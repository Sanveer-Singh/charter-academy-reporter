# Charter Reporter App - Infrastructure as Code

This folder contains Terraform configuration to provision the Charter Reporter App infrastructure on AWS, designed to integrate with your existing Moodle and WordPress MariaDB infrastructure.

## 🏗️ Architecture Overview

- **Region**: `af-south-1` (Cape Town) for optimal SA latency
- **Compute**: EC2 `t3.small` with Amazon Linux 2023, Nginx reverse proxy
- **Storage**: EBS gp3 volume with encrypted root volume and SQLite database
- **Security**: Security Groups, IAM roles, Parameter Store for secrets
- **Monitoring**: CloudWatch metrics, logs, alarms, and AWS Backup
- **TLS**: Let's Encrypt via Certbot (automated renewals)
- **Access**: SSM Session Manager (no SSH required)

## 📋 Prerequisites

1. **AWS CLI** configured with appropriate permissions
2. **Terraform** >= 1.6 installed
3. **Existing VPC** with MariaDB instances for Moodle/WordPress
4. **Domain name** pointing to the infrastructure (can be configured after deployment)
5. **Database credentials** for read-only access to existing MariaDB instances

## 🚀 Quick Start

### Step 1: Configure Variables

```bash
cd infrastructure/terraform
cp terraform.tfvars.example terraform.tfvars
```

Edit `terraform.tfvars` with your actual values:

```hcl
# Required: Your existing infrastructure
vpc_id                    = "vpc-xxxxxxxx"
public_subnet_id          = "subnet-xxxxxxxx"  
mariadb_security_group_id = "sg-xxxxxxxx"

# Required: Domain and emails
domain_name = "reporter.charteracademy.co.za"
admin_email = "admin@charteracademy.co.za"
alert_email = "alerts@charteracademy.co.za"

# Required: Database passwords
mariadb_moodle_password = "your_actual_readonly_password"
mariadb_woo_password    = "your_actual_readonly_password"
```

### Step 2: Deploy Infrastructure

```bash
# Initialize Terraform
terraform init

# Review the plan
terraform plan

# Apply the configuration
terraform apply
```

### Step 3: Post-Deployment Setup

After Terraform completes, you'll need to:

1. **Configure DNS** to point to the Elastic IP
2. **Set up SSL** certificate
3. **Deploy the application**

```bash
# Get the instance details
terraform output deployment_info

# Connect to the instance
aws ssm start-session --target $(terraform output -raw instance_id) --region af-south-1

# Once DNS is configured, run SSL setup
sudo /usr/local/bin/setup-ssl.sh

# Validate the deployment
sudo /usr/local/bin/deployment-validation.sh
```

## 📊 Infrastructure Components

### Compute & Networking
- **EC2 Instance**: `t3.small` with Amazon Linux 2023
- **Elastic IP**: Static public IP address
- **Security Groups**: Web traffic (80, 443) + MariaDB access
- **IAM Role**: SSM, CloudWatch, Parameter Store, S3 access

### Storage & Backup
- **EBS Volume**: 30GB gp3 encrypted root volume
- **S3 Bucket**: Deployment artifacts with lifecycle management
- **AWS Backup**: Daily snapshots with 30-day retention
- **SQLite Database**: Stored on EBS at `/var/www/charter-reporter/data/`

### Monitoring & Alerting
- **CloudWatch Logs**: Application, Nginx, and system logs
- **CloudWatch Metrics**: CPU, memory, disk, network monitoring
- **CloudWatch Alarms**: High resource usage, instance status
- **SNS Notifications**: Email alerts for critical events
- **Dashboard**: Pre-configured CloudWatch dashboard

### Security & Configuration
- **Parameter Store**: Secure storage for passwords and configuration
- **TLS/SSL**: Let's Encrypt certificates with auto-renewal
- **Security Headers**: HSTS, CSP, anti-XSS headers via Nginx
- **Rate Limiting**: Nginx-based rate limiting for different endpoints

## 🛡️ Security Features

- **No SSH Access**: SSM Session Manager for secure management
- **Encrypted Storage**: EBS volumes encrypted at rest
- **Secure Parameters**: Database passwords stored in Parameter Store
- **Network Isolation**: Security groups restrict access appropriately
- **Security Headers**: Comprehensive HTTP security headers
- **Rate Limiting**: Protection against abuse and DoS

## 💰 Cost Optimization

Monthly cost estimate for `af-south-1`:
- **EC2 t3.small**: ~$15/month
- **EBS 30GB**: ~$3/month
- **Route 53**: ~$0.50/month (if managed by Terraform)
- **CloudWatch**: ~$2/month
- **Total**: ~$20.50/month

Cost-saving tips:
- Use Reserved Instances after usage patterns stabilize
- Adjust log retention periods based on needs
- Scale instance size based on actual usage metrics

## 🔧 Operational Tasks

### SSL Certificate Setup
```bash
# After DNS configuration, run on the EC2 instance:
sudo /usr/local/bin/setup-ssl.sh
```

### Application Deployment
```bash
# Deploy from S3 artifact:
sudo /usr/local/bin/deploy-charter-reporter.sh s3://bucket-name/artifact.zip
```

### Health Monitoring
```bash
# Manual health check:
sudo /usr/local/bin/deployment-validation.sh

# View application logs:
sudo journalctl -u charter-reporter --follow

# View Nginx logs:
sudo tail -f /var/log/nginx/access.log
sudo tail -f /var/log/nginx/error.log
```

### Backup Operations
```bash
# Verify backup status:
sudo /usr/local/bin/verify-backup.sh

# List available backups:
aws backup list-recovery-points --backup-vault-name charter-reporter-vault --region af-south-1
```

## 🔄 CI/CD Integration

The infrastructure creates an S3 bucket for deployment artifacts. Use this GitHub Actions workflow:

```yaml
name: Deploy Charter Reporter
on:
  push:
    branches: [ main ]

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      
      - name: Build and Deploy
        run: |
          # Build application
          dotnet publish ./src/Web/Charter.Reporter.Web.csproj -c Release -o out
          
          # Create deployment package
          cd out && zip -r ../charter-reporter-${{ github.sha }}.zip . && cd ..
          
          # Upload to S3
          aws s3 cp charter-reporter-${{ github.sha }}.zip \
            s3://$(terraform output -raw s3_artifacts_bucket)/charter-reporter-${{ github.sha }}.zip
          
          # Deploy via SSM
          aws ssm send-command \
            --instance-ids "$(terraform output -raw instance_id)" \
            --document-name "AWS-RunShellScript" \
            --parameters commands="[\"sudo /usr/local/bin/deploy-charter-reporter.sh s3://$(terraform output -raw s3_artifacts_bucket)/charter-reporter-${{ github.sha }}.zip\"]" \
            --region af-south-1
```

## 🚨 Troubleshooting

### Common Issues

| Issue | Solution |
|-------|----------|
| 502 Bad Gateway | Check Parameter Store access and app service status |
| SSL Certificate Fails | Ensure DNS points to Elastic IP before running setup-ssl.sh |
| MariaDB Connection Issues | Verify security group rules and credentials |
| High Memory Usage | Monitor export operations, consider larger instance |

### Debug Commands

```bash
# Check service status
sudo systemctl status charter-reporter nginx

# View recent application logs
sudo journalctl -u charter-reporter --since "10 minutes ago"

# Test MariaDB connectivity
mysql -h your-mariadb-host -u readonly_user -p -e "SELECT 1;"

# Verify Parameter Store access
aws ssm get-parameter --name "/charter-reporter/admin/email" --region af-south-1
```

## 📝 Terraform Outputs

After deployment, Terraform provides these useful outputs:

- `instance_id`: EC2 instance ID for SSM access
- `public_ip`: Elastic IP for DNS configuration
- `s3_artifacts_bucket`: S3 bucket for CI/CD deployments
- `parameter_store_setup_commands`: Commands to verify Parameter Store
- `ssm_session_command`: Command to connect via SSM

## 🧹 Cleanup

To destroy the infrastructure:

```bash
# WARNING: This will delete all resources and data
terraform destroy
```

**Note**: This will permanently delete the SQLite database and all application data. Ensure you have backups if needed.

## 🔗 Integration with Existing Infrastructure

This Terraform configuration is designed to integrate seamlessly with your existing Charter Academy infrastructure:

- **VPC Integration**: Deploys into your existing VPC alongside Moodle/WordPress
- **Database Access**: Configures security group rules for MariaDB access
- **No Changes**: Makes zero modifications to existing MariaDB databases
- **Network Isolation**: Maintains security boundaries while enabling necessary connectivity

The infrastructure follows the security and architectural principles outlined in the main project README and AWS deployment guide.





