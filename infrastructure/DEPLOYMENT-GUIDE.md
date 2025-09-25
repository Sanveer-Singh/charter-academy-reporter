# Charter Reporter App - Complete Deployment Guide

## 🎯 Overview

This guide walks you through deploying the Charter Reporter App using the provided Infrastructure as Code (Terraform) solution. The infrastructure will be deployed in the same VPC as your existing Moodle and WordPress databases.

## ⚡ Quick Deployment (Experienced Users)

```bash
cd infrastructure/terraform
cp terraform.tfvars.example terraform.tfvars
# Edit terraform.tfvars with your values (see checklist below)

# Optional: run pre-flight checks
bash ../scripts/pre-deployment-check.sh

make init
make plan
make apply

# After apply: set DNS to the Elastic IP, then
make connect
sudo /usr/local/bin/setup-ssl.sh
```

## 📝 Detailed Step-by-Step Guide

### Prerequisites Checklist

- [ ] AWS CLI configured with `af-south-1` region (Cape Town)
- [ ] Terraform >= 1.6 installed
- [ ] Domain name ready for configuration
- [ ] Read-only credentials for existing MariaDB databases
- [ ] Existing VPC, subnet, and security group IDs

### Step 1: Gather Existing Infrastructure Information

You'll need these IDs from your existing AWS infrastructure:

```bash
# Find your VPC ID
aws ec2 describe-vpcs --region af-south-1 --query 'Vpcs[*].[VpcId,Tags[?Key==`Name`].Value|[0]]' --output table

# Find public subnet ID in your VPC
aws ec2 describe-subnets --region af-south-1 --filters "Name=vpc-id,Values=YOUR_VPC_ID" --query 'Subnets[?MapPublicIpOnLaunch==`true`].[SubnetId,AvailabilityZone,CidrBlock]' --output table

# Find MariaDB security group ID
aws ec2 describe-security-groups --region af-south-1 --filters "Name=vpc-id,Values=YOUR_VPC_ID" --query 'SecurityGroups[?contains(Description,`mariadb`) || contains(GroupName,`mariadb`)].[GroupId,GroupName,Description]' --output table
```

### Step 2: Configure Terraform Variables (Minimal Cost Defaults)

```bash
cd infrastructure/terraform
cp terraform.tfvars.example terraform.tfvars
```

Edit `terraform.tfvars` with your actual values (t3.micro, 20GB defaults):

```hcl
# === REQUIRED: Update these with your actual infrastructure ===
vpc_id                    = "vpc-xxxxxxxxxxxxxxx"    # From Step 1
public_subnet_id          = "subnet-xxxxxxxxxxxxxxx" # From Step 1  
mariadb_security_group_id = "sg-xxxxxxxxxxxxxxx"     # From Step 1

# === REQUIRED: Domain and contact information ===
domain_name = "reporter.charteracademy.co.za"       # Your domain
admin_email = "admin@charteracademy.co.za"          # For SSL certs
alert_email = "alerts@charteracademy.co.za"         # For monitoring

# === REQUIRED: Database credentials (read-only users) ===
mariadb_moodle_password = "your_moodle_readonly_password"
mariadb_woo_password    = "your_woo_readonly_password"

# === OPTIONAL: Customize as needed ===
instance_type       = "t3.micro"  # start as small as possible
ebs_volume_size     = 20          # GB
create_route53_zone = false       # leave false unless hosting DNS in this account
```

### Step 3: Deploy Infrastructure

```bash
# Initialize and validate
make init
make validate

# Review the plan
make plan

# Deploy (creates all AWS resources)
make apply
```

### Step 4: Configure DNS

After deployment, configure your domain to point to the new server:

```bash
# Get the Elastic IP
make output

# Configure DNS A record
# If using Route 53 (and create_route53_zone = false):
aws route53 change-resource-record-sets --hosted-zone-id YOUR_ZONE_ID --change-batch '{
  "Changes": [{
    "Action": "CREATE",
    "ResourceRecordSet": {
      "Name": "reporter.charteracademy.co.za",
      "Type": "A",
      "TTL": 300,
      "ResourceRecords": [{"Value": "YOUR_ELASTIC_IP"}]
    }
  }]
}' --region af-south-1

# Wait for DNS propagation (2-10 minutes)
dig reporter.charteracademy.co.za
```

### Step 5: Set Up SSL Certificate

```bash
# Connect to the instance
make connect

# Once DNS is working, set up SSL
sudo /usr/local/bin/setup-ssl.sh

# Verify SSL is working
curl -I https://reporter.charteracademy.co.za
```

### Step 6: Deploy the Application

#### Option A: Manual Deployment (First Time)

```bash
# Build and package the application locally
cd /path/to/charter-reporter-source
dotnet publish ./src/Web/Charter.Reporter.Web.csproj -c Release -o ./publish
cd ./publish && zip -r ../charter-reporter-initial.zip . && cd ..

# Resolve S3 artifacts bucket and instance id from SSM/tags
S3_BUCKET=$(aws ssm get-parameter --name "/charter-reporter/deployment/artifacts_bucket" --region af-south-1 --query 'Parameter.Value' --output text)
INSTANCE_ID=$(aws ec2 describe-instances \
  --region af-south-1 \
  --filters Name=tag:Project,Values=Charter-Reporter Name=instance-state-name,Values=running \
  --query 'Reservations[0].Instances[0].InstanceId' --output text)

# Upload to S3
aws s3 cp charter-reporter-initial.zip s3://$S3_BUCKET/charter-reporter-initial.zip --region af-south-1

# Deploy via SSM
aws ssm send-command \
  --instance-ids "$INSTANCE_ID" \
  --document-name "AWS-RunShellScript" \
  --parameters 'commands=["sudo /usr/local/bin/deploy-charter-reporter.sh s3://'$S3_BUCKET'/charter-reporter-initial.zip"]' \
  --region af-south-1
```

#### Option B: GitHub Actions (Automated)

1. Copy `infrastructure/github-actions/deploy.yml` to `.github/workflows/deploy.yml`
2. Add GitHub secrets:
   - `AWS_ACCESS_KEY_ID`
   - `AWS_SECRET_ACCESS_KEY`
3. Push to main branch to trigger deployment

### Step 7: Verification

```bash
# Run comprehensive validation
make health

# Or connect and run manually
make connect
sudo /usr/local/bin/deployment-validation.sh

# Check application is working
curl -f https://reporter.charteracademy.co.za/health
```

## 🔧 Post-Deployment Configuration

### Application Configuration

The app will need these configuration files created or updated:

1. **appsettings.Production.json** - Already configured via Parameter Store
2. **Program.cs** - Ensure SSM Parameter Store config binding is enabled in `Production`.
3. **Database migrations** - Run on first deployment

### Initial Admin User

The infrastructure creates a random admin password. Retrieve it:

```bash
# Get the generated admin password
terraform output -raw generated_admin_password

# Or view in Parameter Store
aws ssm get-parameter --name "/charter-reporter/admin/password" --with-decryption --region af-south-1 --query 'Parameter.Value' --output text
```

## 📊 Monitoring and Operations

### CloudWatch Dashboard

Access your monitoring dashboard:
- Go to AWS Console → CloudWatch → Dashboards → "Charter-Reporter-Dashboard"
- Monitor CPU, memory, disk usage, and application logs

### Log Access

```bash
# View application logs
aws logs tail /charter-reporter/app --since 1h --region af-south-1

# View Nginx logs  
aws logs tail /charter-reporter/nginx/error --since 1h --region af-south-1
```

### Backup Management

```bash
# Check backup status
make backup-status

# Manual backup verification
aws ssm send-command \
  --instance-ids "$(terraform output -raw instance_id)" \
  --document-name "AWS-RunShellScript" \
  --parameters 'commands=["/usr/local/bin/verify-backup.sh"]' \
  --region af-south-1
```

## 🚨 Troubleshooting

### Common Issues

**502 Bad Gateway after deployment:**
```bash
# Check application service
make connect
sudo systemctl status charter-reporter
sudo journalctl -u charter-reporter --since "10 minutes ago"

# Check Parameter Store access
aws ssm get-parameter --name "/charter-reporter/admin/email" --region af-south-1
```

**SSL Certificate Issues:**
```bash
# Check Certbot logs
sudo journalctl -u certbot --since "1 hour ago"

# Manual certificate renewal test
sudo certbot renew --dry-run

# Verify DNS is pointing to correct IP
dig +short reporter.charteracademy.co.za
```

**MariaDB Connection Issues:**
```bash
# Test connectivity from EC2
mysql -h charteracademy-moolrn.cbasq8maywa1.af-south-1.rds.amazonaws.com -u readonly_user -p -e "SELECT 1;"

# Check security group rules
aws ec2 describe-security-groups --group-ids sg-your-mariadb-sg --region af-south-1
```

### Emergency Procedures

**Rollback Deployment:**
```bash
# Emergency rollback
aws ssm send-command \
  --instance-ids "YOUR_INSTANCE_ID" \
  --document-name "AWS-RunShellScript" \
  --parameters 'commands=[
    "sudo systemctl stop charter-reporter",
    "sudo mv /var/www/charter-reporter /var/www/charter-reporter.failed",
    "sudo mv /var/www/charter-reporter.backup.* /var/www/charter-reporter",
    "sudo systemctl start charter-reporter"
  ]' --region af-south-1
```

**Disaster Recovery:**
```bash
# Restore from backup (creates new instance)
terraform import aws_instance.charter_reporter i-new-instance-id
terraform apply -replace="aws_instance.charter_reporter"
```

## 🧪 Testing Checklist

After deployment, verify these items:

- [ ] HTTPS certificate is valid (A+ rating on SSL Labs)
- [ ] Application health endpoint returns 200
- [ ] Authentication flow works (login/logout)
- [ ] Dashboard loads with data from MariaDB
- [ ] Export functionality works
- [ ] Rate limiting is active (test with multiple requests)
- [ ] Security headers are present
- [ ] CloudWatch alarms are active
- [ ] Backup plan is working

## 💰 Cost Management

**Monthly Cost Tracking:**
```bash
# View current costs
aws ce get-cost-and-usage \
  --time-period Start=2024-01-01,End=2024-01-31 \
  --granularity MONTHLY \
  --metrics BlendedCost \
  --group-by Type=DIMENSION,Key=SERVICE \
  --region af-south-1
```

**Cost Optimization:**
- Monitor CloudWatch costs and adjust log retention
- Consider Reserved Instances after 3 months of stable usage
- Review backup retention based on actual needs
- Scale instance size based on CloudWatch metrics

Minimal profile (af-south-1 typical):
- EC2 t3.micro: ~$9/mo
- EBS 20GB gp3: ~$2/mo
- CloudWatch logs/metrics: ~$2/mo
- Optional Route 53: ~$0.50/mo
- Total: ~$13.50/mo

## 🔄 Updates and Maintenance

### Infrastructure Updates
```bash
# Update Terraform configuration
terraform plan
terraform apply

# Update system packages (automated via dnf-automatic)
make connect
sudo dnf update -y
```

### Application Updates
- Use GitHub Actions for automated deployment
- Monitor CloudWatch alarms after updates
- Test health endpoints before promoting

### Certificate Renewal
- Automated via Certbot systemd timer
- Manual renewal: `sudo certbot renew`

---

## 📞 Support

For infrastructure issues:
1. Check CloudWatch logs and metrics
2. Review Terraform state and outputs
3. Validate AWS permissions and network connectivity
4. Use the troubleshooting commands in this guide

For application issues:
1. Check application logs: `sudo journalctl -u charter-reporter`
2. Verify Parameter Store configuration
3. Test MariaDB connectivity
4. Review the main project README.md for application-specific guidance


## ✅ Values You Must Update (Checklist)

Update these in `infrastructure/terraform/terraform.tfvars` before `make apply`:

- VPC: `vpc_id` (must be the same VPC as the existing MariaDBs)
- Subnet: `public_subnet_id` (public subnet in that VPC)
- MariaDB SG: `mariadb_security_group_id` (SG attached to your RDS/MariaDB)
- Domain: `domain_name` (e.g., reporter.charteracademy.co.za)
- Emails: `admin_email`, `alert_email`
- DB passwords: `mariadb_moodle_password`, `mariadb_woo_password`
- Optional SMTP: `smtp_host`, `smtp_username`, `smtp_password`, `smtp_port`
- Instance size: `instance_type` (default t3.micro), `ebs_volume_size` (default 20)
- DNS management: `create_route53_zone` (keep false unless hosting DNS in AWS)
- Retention: `log_retention_days`, `backup_retention_days`

Other places automatically aligned:
- Security Groups: Terraform creates a web SG and adds an ingress rule to your MariaDB SG allowing port 3306 from the app SG.
- IAM: Scoped to current account; app reads only `/charter-reporter/*` SSM params, and S3 artifacts bucket.
- Region: Provider and scripts default to `af-south-1` (Cape Town).




