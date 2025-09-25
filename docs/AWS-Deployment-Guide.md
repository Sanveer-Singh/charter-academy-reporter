## Charter Reporter App — AWS Deployment Guide (Cost-Optimized for South Africa)

Audience: Absolute beginners who can follow copy/paste steps, and experts who want the why and alternatives.

Region: Africa (Cape Town) `af-south-1` — lowest latency for SA users and keeps data in-country.

Scope: Plan only. This document explains how to provision and deploy the ASP.NET Core MVC app described in `README.md` with strong security defaults, minimal running cost, and clear upgrade paths.


### 1) Architecture Overview (Recommended Baseline)

Goal: Reliable, low-cost production setup for moderate traffic with straightforward operations.

- Compute: 1× EC2 `t3.micro` or `t3.small` in `af-south-1` running Amazon Linux 2023, Kestrel + Nginx reverse proxy.
- TLS: Let’s Encrypt via Certbot on Nginx (free, automated renewals). Alternative: ACM + ALB (simpler TLS but adds cost).
- Storage: Single EBS volume (gp3, 20–40 GB) for app, `wwwroot`, SQLite DB, DataProtection keys.
- Network: Default VPC, public subnet, Security Group allowing 443 from the Internet, SSM for management (no SSH open).
- Secrets: AWS Systems Manager Parameter Store (SecureString) for DB credentials and app secrets (free tier friendly).
- Logs/Monitoring: CloudWatch Logs + CloudWatch Agent for system/app metrics, alarms + AWS Budgets.
- Backups: AWS Backup daily EBS snapshot for SQLite and keys.
- DNS: Route 53 hosted zone + Elastic IP for the EC2 instance.

Why this is best for cost in SA:
- Avoids the fixed monthly cost of an ALB and other managed layers until you need scale or HA.
- Uses SSM Session Manager for secure access (no bastion, no SSH open ports).
- Free TLS via Let’s Encrypt instead of ACM+ALB.
- Parameter Store over Secrets Manager to avoid per-secret monthly cost.

When to upgrade:
- Increasing traffic or strict uptime → Add ALB + Auto Scaling Group across 2 AZs.
- Static assets heavy → S3 + CloudFront (bandwidth offload) once data patterns justify spend.


### 2) Alternative Architectures (Quick Compare)

- Lightsail: Easiest/cheapest managed VM + static IP + basic monitoring. Good for small MVPs. Limited IAM/SSM features and fewer knobs; still viable in SA.
- Windows + IIS (per README): Works well with ASP.NET Core Module, but Windows licensing increases cost. Prefer Linux for cost unless you require IIS features.
- ALB + Auto Scaling: Adds high-availability and zero-downtime deployments, but ALB has a fixed cost; use once traffic needs justify it.


### 3) Cost Pointers (Order of magnitude, af-south-1)

- EC2 `t3.micro`/`t3.small`: low monthly on‑demand. Consider 1‑year no‑upfront Savings Plan later.
- EBS gp3 20–40 GB: a few USD/month.
- Route 53: ~$0.50/hosted zone/month + $0.40 per million queries.
- CloudWatch: a few USD/month depending on logs/metrics/alarms. Keep retention modest (e.g., 14–30 days).
- Let’s Encrypt: free.
- Parameter Store (Standard): free.


### 4) Prerequisites and Getting Started

**Required Resources:**
- **Domain**: Registered domain name (can be Route 53 or external registrar)
- **AWS Account**: With AdministratorAccess IAM user (not root), MFA enabled
- **Local Tools**: AWS CLI v2, Git, .NET 8 SDK (optional for local testing)

**Step-by-Step Prerequisites Setup:**

1. **AWS Account Setup:**
```bash
# Install AWS CLI v2 (if not installed)
# Windows: Download from https://awscli.amazonaws.com/AWSCLIV2.msi
# Mac: curl "https://awscli.amazonaws.com/AWSCLIV2.pkg" -o "AWSCLIV2.pkg"
# Linux: curl "https://awscli.amazonaws.com/awscli-exe-linux-x86_64.zip" -o "awscliv2.zip"

# Configure AWS CLI with your IAM user credentials
aws configure
# Enter: Access Key ID, Secret Access Key, Region: af-south-1, Output: json

# Verify access
aws sts get-caller-identity
```

2. **Domain DNS Setup:**
```bash
# If using Route 53 (recommended for AWS integration)
aws route53 create-hosted-zone --name yourdomain.com --caller-reference $(date +%s) --region af-south-1

# If using external registrar, you'll need to:
# 1. Point nameservers to Route 53 (get NS records from hosted zone)
# 2. Or create A record pointing to Elastic IP after EC2 creation
```

3. **Cost Estimation:**
```
Monthly cost estimate for af-south-1:
- t3.small EC2: ~$15/month
- EBS gp3 30GB: ~$3/month  
- Route 53 hosted zone: $0.50/month
- CloudWatch logs (modest): ~$2/month
- Data transfer: ~$1/month (typical usage)
Total: ~$21.50/month

Compare to managed alternatives:
- Lightsail 2GB: ~$10/month (but less features)
- Elastic Beanstalk: ~$25/month (adds ALB cost)
```

**Absolute Beginner Checklist:**
- [ ] AWS account created and MFA enabled
- [ ] IAM user with AdministratorAccess (not root user)
- [ ] AWS CLI installed and configured (`aws sts get-caller-identity` works)
- [ ] Domain registered and DNS delegation ready
- [ ] Budget alert configured to avoid surprise costs


### 5) One-Time AWS Account Setup (Security + Budgets)

1. Create an IAM admin user for yourself (not root), enable MFA.
2. Create an IAM group `Administrators` and attach `AdministratorAccess`.
3. Add an AWS Budget (monthly) with email alerts at 50%/80%/100%.
4. Enable default VPC in `af-south-1` (it exists by default in most accounts).

CLI (optional):
```bash
aws budgets create-budget --account-id YOUR_ACCOUNT_ID --budget '...JSON...' | cat
```
Tip: Use the Console first; budgets JSON is verbose.


### 6) VPC, Security Groups, and IAM Roles

- Use default VPC and a public subnet for the single EC2.
- Security Group `sg-charter-reporter-web`:
  - Inbound: 443 from 0.0.0.0/0
  - Inbound: 80 from 0.0.0.0/0 (optional, only for HTTP→HTTPS redirect)
  - No inbound SSH. Use SSM Session Manager.
  - Outbound: 443 to 0.0.0.0/0 (HTTPS, Let's Encrypt, AWS APIs)
  - Outbound: 80 to 0.0.0.0/0 (package updates, HTTP redirects)
  - Outbound: 3306 to RDS security group (MariaDB access)

**MariaDB RDS Security Group** `sg-charter-reporter-mariadb`:
- Inbound: 3306 from `sg-charter-reporter-web` only
- Outbound: none required

**Network Validation:**
Your `appsettings.json` shows RDS endpoints in `af-south-1`. Ensure:
1. RDS instances are in same VPC as EC2 (or VPC peering configured)
2. Route tables allow connectivity between subnets
3. RDS parameter groups have correct timezone settings for Africa/Johannesburg

Instance Role (IAM): `EC2-CharterReporter-InstanceRole` with policies:
- `AmazonSSMManagedInstanceCore` (required for Session Manager)
- `CloudWatchAgentServerPolicy` (metrics/logs)
- `AmazonSSMReadOnlyAccess` (to read Parameter Store standard parameters; or attach inline least-privilege policy)


### 7) EC2 Provisioning (Amazon Linux 2023)

1. Launch EC2 in `af-south-1`:
   - AMI: Amazon Linux 2023 (x86_64)
   - Type: `t3.small` (recommended) or `t3.micro` (if very small traffic)
   - Storage: gp3 30 GB (increase if logs/artifacts grow)
   - Network: default VPC, public subnet
   - Security Group: `sg-charter-reporter-web`
   - IAM Role: `EC2-CharterReporter-InstanceRole`
   - User data (optional hardening): system updates on first boot
2. Allocate an Elastic IP and associate to the instance.

Session access (no SSH):
- Use AWS Console → Systems Manager → Session Manager → Start session (works after SSM agent/role are attached; AL2023 has SSM agent by default).


### 8) OS Bootstrap: .NET, Nginx, Certbot, Directories

Connect via Session Manager shell, then run:
```bash
sudo dnf update -y
curl -sSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
sudo bash /tmp/dotnet-install.sh --channel 8.0 --install-dir /usr/share/dotnet
sudo ln -s /usr/share/dotnet/dotnet /usr/bin/dotnet

sudo dnf install -y nginx
sudo systemctl enable nginx
sudo systemctl start nginx

sudo dnf install -y python3-certbot-nginx

sudo mkdir -p /var/www/charter-reporter
sudo mkdir -p /var/app/keys
sudo mkdir -p /var/log/charter-reporter
sudo chown -R ec2-user:ec2-user /var/www/charter-reporter /var/app/keys /var/log/charter-reporter
```

Reasoning:
- Installing the .NET runtime only (cheaper/fewer packages than full SDK on server).
- Nginx handles TLS and reverse proxy; Kestrel focuses on app logic.
- DataProtection keys persisted in `/var/app/keys` for stable cookie encryption.


### 9) Nginx Reverse Proxy with Performance and Security

**Create optimized Nginx configuration** (replace `example.com` with your domain):
```bash
sudo tee /etc/nginx/conf.d/charter-reporter.conf >/dev/null <<'NGINX'
# Rate limiting zones
limit_req_zone $binary_remote_addr zone=general:10m rate=10r/s;
limit_req_zone $binary_remote_addr zone=auth:10m rate=3r/m;
limit_req_zone $binary_remote_addr zone=export:10m rate=1r/m;

server {
    listen 80;
    server_name example.com www.example.com;
    return 301 https://$host$request_uri;
}

server {
    listen 443 ssl http2;
    server_name example.com www.example.com;

    # SSL Configuration
    ssl_certificate     /etc/letsencrypt/live/example.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/example.com/privkey.pem;
    ssl_protocols       TLSv1.2 TLSv1.3;
    ssl_ciphers         ECDHE-RSA-AES256-GCM-SHA512:DHE-RSA-AES256-GCM-SHA512:ECDHE-RSA-AES256-GCM-SHA384;
    ssl_prefer_server_ciphers off;
    ssl_session_cache   shared:SSL:10m;
    ssl_session_timeout 1d;

    # Security Headers
    add_header Strict-Transport-Security "max-age=63072000; includeSubDomains; preload" always;
    add_header X-Content-Type-Options nosniff always;
    add_header X-Frame-Options DENY always;
    add_header Referrer-Policy no-referrer-when-downgrade always;
    add_header X-XSS-Protection "1; mode=block" always;

    # Performance Settings
    client_max_body_size 25m;
    gzip on;
    gzip_vary on;
    gzip_min_length 1024;
    gzip_types text/plain text/css text/xml text/javascript application/javascript application/xml+rss application/json;

    # Static Assets with Caching
    location ~* \.(js|css|png|jpg|jpeg|gif|ico|svg|woff|woff2|ttf)$ {
        proxy_pass http://127.0.0.1:5000;
        expires 1y;
        add_header Cache-Control "public, immutable";
        add_header Vary Accept-Encoding;
    }

    # Rate Limited Endpoints
    location /Account/Login {
        limit_req zone=auth burst=5 nodelay;
        proxy_pass http://127.0.0.1:5000;
        include /etc/nginx/proxy_params;
    }

    location /Export {
        limit_req zone=export burst=2 nodelay;
        proxy_pass http://127.0.0.1:5000;
        include /etc/nginx/proxy_params;
    }

    # General Application
    location / {
        limit_req zone=general burst=20 nodelay;
        proxy_pass http://127.0.0.1:5000;
        include /etc/nginx/proxy_params;
    }
}
NGINX

# Create proxy params file
sudo tee /etc/nginx/proxy_params >/dev/null <<'PROXY'
proxy_http_version 1.1;
proxy_set_header   Upgrade $http_upgrade;
proxy_set_header   Connection keep-alive;
proxy_set_header   Host $host;
proxy_cache_bypass $http_upgrade;
proxy_set_header   X-Forwarded-For $proxy_add_x_forwarded_for;
proxy_set_header   X-Forwarded-Proto $scheme;
proxy_set_header   X-Real-IP $remote_addr;
proxy_connect_timeout 30s;
proxy_send_timeout 30s;
proxy_read_timeout 30s;
PROXY

sudo nginx -t && sudo systemctl reload nginx
```

Obtain TLS cert via Let’s Encrypt (DNS must point to Elastic IP first):
```bash
sudo certbot --nginx -d example.com -d www.example.com --agree-tos -m admin@example.com --redirect --non-interactive
```

Renewal runs automatically via system timer. Use ACM+ALB instead if you prefer managed TLS (adds fixed monthly cost).


### 10) App Service (systemd) and DataProtection Setup

**Create directories and set permissions:**
```bash
sudo mkdir -p /var/www/charter-reporter/data
sudo mkdir -p /var/app/keys
sudo mkdir -p /var/log/charter-reporter
sudo chown -R ec2-user:ec2-user /var/www/charter-reporter /var/app/keys /var/log/charter-reporter
```

**Create systemd unit to run Kestrel:**
```bash
sudo tee /etc/systemd/system/charter-reporter.service >/dev/null <<'UNIT'
[Unit]
Description=Charter Reporter ASP.NET Core App
After=network.target

[Service]
WorkingDirectory=/var/www/charter-reporter
ExecStart=/usr/bin/dotnet /var/www/charter-reporter/Charter.Reporter.Web.dll
Restart=always
RestartSec=5
SyslogIdentifier=charter-reporter
User=ec2-user
Environment=ASPNETCORE_URLS=http://127.0.0.1:5000
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=AWS_REGION=af-south-1
Environment=DOTNET_PrintTelemetryMessage=false
# SQLite path on persistent storage
Environment=ConnectionStrings__AppDb=Data Source=/var/www/charter-reporter/data/app.db

[Install]
WantedBy=multi-user.target
UNIT

sudo systemctl daemon-reload
sudo systemctl enable charter-reporter
```

**DataProtection Keys Configuration** (add to `Program.cs` after service registration):
```csharp
// Configure DataProtection to persist keys across restarts
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("/var/app/keys"))
    .SetApplicationName("Charter.Reporter")
    .SetDefaultKeyLifetime(TimeSpan.FromDays(90));
```

**Production Configuration Validation:**
```bash
# Test service after deployment
sudo systemctl start charter-reporter
sudo systemctl status charter-reporter
curl -f http://127.0.0.1:5000/health || echo "Health check failed"
sudo journalctl -u charter-reporter --no-pager | tail -n 50
```


### 11) Secrets and Configuration (Parameter Store)

**Store All Secrets in Parameter Store:**
```bash
# MariaDB credentials (replace REDACTED with actual values)
aws ssm put-parameter --name "/charter-reporter/mariadb/moodle/password" \
  --type SecureString --value "REDACTED" --overwrite --region af-south-1

aws ssm put-parameter --name "/charter-reporter/mariadb/woo/password" \
  --type SecureString --value "REDACTED" --overwrite --region af-south-1

# SMTP settings (if using external SMTP)
aws ssm put-parameter --name "/charter-reporter/email/smtp/password" \
  --type SecureString --value "REDACTED" --overwrite --region af-south-1

# Admin user seeding
aws ssm put-parameter --name "/charter-reporter/admin/email" \
  --type String --value "admin@charteracademy.co.za" --overwrite --region af-south-1
  
aws ssm put-parameter --name "/charter-reporter/admin/password" \
  --type SecureString --value "STRONG_ADMIN_PASSWORD" --overwrite --region af-south-1
```

**Update IAM Role** (add to existing policies):
```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Sid": "ReadSSMParameters",
      "Effect": "Allow",
      "Action": [
        "ssm:GetParameter",
        "ssm:GetParameters"
      ],
      "Resource": [
        "arn:aws:ssm:af-south-1:*:parameter/charter-reporter/*"
      ]
    }
  ]
}
```

**App Configuration Changes Required:**
Add AWS SDK dependency to `src/Web/Charter.Reporter.Web.csproj`:
```xml
<PackageReference Include="AWS.Extensions.NETCore.Setup" Version="3.7.7" />
<PackageReference Include="AWSSDK.SimpleSystemsManagement" Version="3.7.305.23" />
```

**Environment Variables in systemd service** (update section 10):
```bash
Environment=AWS_REGION=af-south-1
Environment=ASPNETCORE_ENVIRONMENT=Production
```

**Production appsettings.Production.json** (create this file):
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning",
      "Charter.Reporter": "Information"
    }
  },
  "MariaDb": {
    "Moodle": {
      "Host": "charteracademy-moolrn.cbasq8maywa1.af-south-1.rds.amazonaws.com",
      "Port": 3306,
      "Database": "charteracademy_moolrn",
      "Username": "readonly_user",
      "Password": "PARAMETER_STORE_PLACEHOLDER",
      "TablePrefix": "moow_"
    },
    "Woo": {
      "Host": "charteracademy-moolrn.cbasq8maywa1.af-south-1.rds.amazonaws.com",
      "Port": 3306,
      "Database": "charteracademy_wp001",
      "Username": "readonly_user",
      "Password": "PARAMETER_STORE_PLACEHOLDER",
      "TablePrefix": "wpbh_"
    }
  },
  "Admin": {
    "Email": "PARAMETER_STORE_PLACEHOLDER",
    "Password": "PARAMETER_STORE_PLACEHOLDER"
  }
}
```

**Parameter Store Integration Code** (add to `Program.cs`):
```csharp
// Add before builder.Build()
if (!builder.Environment.IsDevelopment())
{
    builder.Configuration.AddSystemsManager("/charter-reporter", TimeSpan.FromMinutes(5));
    
    // Override with Parameter Store values
    var ssmClient = new Amazon.SimpleSystemsManagement.AmazonSimpleSystemsManagementClient(Amazon.RegionEndpoint.AFSouth1);
    
    // Load MariaDB passwords
    var moodlePassword = await GetParameterValue(ssmClient, "/charter-reporter/mariadb/moodle/password");
    var wooPassword = await GetParameterValue(ssmClient, "/charter-reporter/mariadb/woo/password");
    var adminEmail = await GetParameterValue(ssmClient, "/charter-reporter/admin/email");
    var adminPassword = await GetParameterValue(ssmClient, "/charter-reporter/admin/password");
    
    builder.Configuration["MariaDb:Moodle:Password"] = moodlePassword;
    builder.Configuration["MariaDb:Woo:Password"] = wooPassword;
    builder.Configuration["Admin:Email"] = adminEmail;
    builder.Configuration["Admin:Password"] = adminPassword;
}

static async Task<string> GetParameterValue(Amazon.SimpleSystemsManagement.IAmazonSimpleSystemsManagement client, string paramName)
{
    var request = new Amazon.SimpleSystemsManagement.Model.GetParameterRequest
    {
        Name = paramName,
        WithDecryption = true
    };
    var response = await client.GetParameterAsync(request);
    return response.Parameter.Value;
}
```


### 13) Comprehensive Monitoring and Alerting

**Install CloudWatch Agent with application-specific config:**
```bash
sudo dnf install -y amazon-cloudwatch-agent
sudo tee /opt/aws/amazon-cloudwatch-agent/etc/amazon-cloudwatch-agent.json >/dev/null <<'CW'
{
  "logs": {
    "logs_collected": {
      "files": {
        "collect_list": [
          {"file_path": "/var/log/charter-reporter/*.log", "log_group_name": "/charter-reporter/app", "log_stream_name": "{instance_id}", "retention_in_days": 30},
          {"file_path": "/var/log/nginx/access.log", "log_group_name": "/charter-reporter/nginx/access", "log_stream_name": "{instance_id}", "retention_in_days": 14},
          {"file_path": "/var/log/nginx/error.log", "log_group_name": "/charter-reporter/nginx/error", "log_stream_name": "{instance_id}", "retention_in_days": 30},
          {"file_path": "/var/log/messages", "log_group_name": "/charter-reporter/system", "log_stream_name": "{instance_id}", "retention_in_days": 7}
        ]
      }
    }
  },
  "metrics": {
    "append_dimensions": {"InstanceId": "${aws:InstanceId}", "InstanceType": "${aws:InstanceType}"},
    "metrics_collected": {
      "cpu": {"measurement": ["cpu_usage_idle", "cpu_usage_iowait"], "metrics_collection_interval": 60},
      "mem": {"measurement": ["mem_used_percent", "mem_available_percent"], "metrics_collection_interval": 60},
      "disk": {"measurement": ["used_percent", "inodes_free"], "resources": ["*"], "metrics_collection_interval": 60},
      "netstat": {"measurement": ["tcp_established", "tcp_time_wait"], "metrics_collection_interval": 60}
    }
  }
}
CW

sudo /opt/aws/amazon-cloudwatch-agent/bin/amazon-cloudwatch-agent-ctl -a start -m ec2 -c file:/opt/aws/amazon-cloudwatch-agent/etc/amazon-cloudwatch-agent.json
```

**Create Critical Alarms with SNS:**
```bash
# Create SNS topic for alerts
aws sns create-topic --name charter-reporter-alerts --region af-south-1
aws sns subscribe --topic-arn arn:aws:sns:af-south-1:ACCOUNT:charter-reporter-alerts \
  --protocol email --notification-endpoint admin@charteracademy.co.za --region af-south-1

# System Health Alarms
aws cloudwatch put-metric-alarm --alarm-name "Charter-HighCPU" \
  --alarm-description "High CPU usage" --metric-name CPUUtilization \
  --namespace AWS/EC2 --statistic Average --period 300 --threshold 80 \
  --comparison-operator GreaterThanThreshold --evaluation-periods 2 \
  --alarm-actions arn:aws:sns:af-south-1:ACCOUNT:charter-reporter-alerts \
  --dimensions Name=InstanceId,Value=i-INSTANCEID --region af-south-1

aws cloudwatch put-metric-alarm --alarm-name "Charter-HighMemory" \
  --alarm-description "High memory usage" --metric-name mem_used_percent \
  --namespace CWAgent --statistic Average --period 300 --threshold 85 \
  --comparison-operator GreaterThanThreshold --evaluation-periods 1 \
  --alarm-actions arn:aws:sns:af-south-1:ACCOUNT:charter-reporter-alerts \
  --dimensions Name=InstanceId,Value=i-INSTANCEID --region af-south-1

aws cloudwatch put-metric-alarm --alarm-name "Charter-DiskSpace" \
  --alarm-description "Low disk space" --metric-name used_percent \
  --namespace CWAgent --statistic Maximum --period 300 --threshold 85 \
  --comparison-operator GreaterThanThreshold --evaluation-periods 1 \
  --alarm-actions arn:aws:sns:af-south-1:ACCOUNT:charter-reporter-alerts \
  --dimensions Name=InstanceId,Value=i-INSTANCEID,Name=device,Value=xvda1 --region af-south-1
```

**Application Health Monitoring:**
Set up custom CloudWatch dashboard:
```bash
# Create custom metrics dashboard (use AWS Console for visual setup)
# Key metrics to track:
# - ASP.NET request duration P95
# - MariaDB connection pool usage
# - Export operation frequency and size
# - Failed authentication attempts
# - Health check status (/health endpoint)
```

**Log Analysis Queries:**
```bash
# CloudWatch Insights queries for common issues:
# Failed logins: fields @timestamp, @message | filter @message like /login.*failed/i
# Slow queries: fields @timestamp, @message | filter @message like /slow.*query/i
# Export operations: fields @timestamp, @message | filter @message like /export.*completed/i
```


### 14) Backups and Disaster Recovery

**Create AWS Backup Plan:**
```bash
# Create backup vault
aws backup create-backup-vault --backup-vault-name charter-reporter-vault --region af-south-1

# Create IAM role for AWS Backup
aws iam create-role --role-name AWSBackupDefaultServiceRole --assume-role-policy-document '{
  "Version": "2012-10-17",
  "Statement": [{
    "Effect": "Allow",
    "Principal": {"Service": "backup.amazonaws.com"},
    "Action": "sts:AssumeRole"
  }]
}'

aws iam attach-role-policy --role-name AWSBackupDefaultServiceRole \
  --policy-arn arn:aws:iam::aws:policy/service-role/AWSBackupServiceRolePolicyForBackup
```

**Backup Plan Configuration:**
- Daily backups at 02:00 CAT (00:00 UTC)
- Retention: 30 days (balance cost vs recovery needs)
- Includes: EBS volume, SQLite database, DataProtection keys
- Recovery Point Objective (RPO): 24 hours
- Recovery Time Objective (RTO): 4 hours

**Backup Verification Script:**
```bash
#!/bin/bash
# /usr/local/bin/verify-backup.sh
echo "=== Charter Reporter Backup Verification ==="
echo "SQLite DB size: $(du -h /var/www/charter-reporter/data/app.db)"
echo "Keys directory: $(ls -la /var/app/keys/)"
echo "Latest backup: $(aws backup list-recovery-points --backup-vault-name charter-reporter-vault --region af-south-1 --query 'RecoveryPoints[0].RecoveryPointArn')"
echo "Disk usage: $(df -h /)"

# Test SQLite integrity
sqlite3 /var/www/charter-reporter/data/app.db "PRAGMA integrity_check;" || echo "SQLite integrity check FAILED"
echo "Backup verification completed: $(date)"
```

**Disaster Recovery Procedure:**
1. Launch new EC2 instance with same configuration
2. Restore EBS volume from latest backup
3. Attach restored volume to new instance
4. Update DNS A record to new Elastic IP
5. Verify application health and MariaDB connectivity
6. Test authentication and basic functionality

**Monthly DR Drill:** Test restore to staging environment to validate RTO targets.


### 15) DNS and HTTPS

Route 53 hosted zone:
- Create an `A` record for `example.com` pointing to the Elastic IP.
- Wait for propagation, then run Certbot (section 9).


### 16) CI/CD with Zero-Downtime Deployments

**S3 Artifact Storage Setup:**
```bash
# Create deployment bucket
aws s3 mb s3://charter-reporter-artifacts-afs1 --region af-south-1
aws s3api put-bucket-versioning --bucket charter-reporter-artifacts-afs1 \
  --versioning-configuration Status=Enabled --region af-south-1

# Lifecycle policy for artifact cleanup
aws s3api put-bucket-lifecycle-configuration --bucket charter-reporter-artifacts-afs1 \
  --lifecycle-configuration '{
    "Rules": [{
      "ID": "DeleteOldArtifacts",
      "Status": "Enabled",
      "Expiration": {"Days": 90}
    }]
  }' --region af-south-1
```

**Enhanced GitHub Actions Workflow:**
```yaml
name: Deploy Charter Reporter
on:
  workflow_dispatch:
  push:
    branches: [ main ]

env:
  AWS_REGION: af-south-1
  S3_BUCKET: charter-reporter-artifacts-afs1
  INSTANCE_ID: i-xxxxxxxxxxxx

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
          
      - name: Build and Publish
        run: |
          # Restore and build all projects
          dotnet restore Charter.Reporter.sln
          dotnet build Charter.Reporter.sln -c Release --no-restore
          
          # Publish web project with production config
          dotnet publish ./src/Web/Charter.Reporter.Web.csproj -c Release -o out \
            --no-build --verbosity minimal
          
          # Create deployment package
          cd out && zip -r ../charter-reporter-${{ github.sha }}.zip . && cd ..
          
      - name: Upload Artifact to S3
        run: |
          aws s3 cp charter-reporter-${{ github.sha }}.zip \
            s3://${{ env.S3_BUCKET }}/charter-reporter-${{ github.sha }}.zip \
            --region ${{ env.AWS_REGION }}
        env:
          AWS_ACCESS_KEY_ID: ${{ secrets.AWS_ACCESS_KEY_ID }}
          AWS_SECRET_ACCESS_KEY: ${{ secrets.AWS_SECRET_ACCESS_KEY }}
          
      - name: Deploy with Health Checks
        run: |
          COMMAND_ID=$(aws ssm send-command \
            --instance-ids "${{ env.INSTANCE_ID }}" \
            --document-name "AWS-RunShellScript" \
            --parameters commands='[
              "echo \"Starting deployment: $(date)\"",
              "aws s3 cp s3://${{ env.S3_BUCKET }}/charter-reporter-${{ github.sha }}.zip /tmp/app.zip --region ${{ env.AWS_REGION }}",
              "sudo systemctl stop charter-reporter",
              "sudo cp -r /var/www/charter-reporter /var/www/charter-reporter.backup.$(date +%s)",
              "sudo rm -rf /var/www/charter-reporter/*",
              "sudo unzip -o /tmp/app.zip -d /var/www/charter-reporter",
              "sudo chown -R ec2-user:ec2-user /var/www/charter-reporter",
              "sudo systemctl start charter-reporter",
              "sleep 10",
              "curl -f http://127.0.0.1:5000/health || (echo \"Health check failed\"; sudo journalctl -u charter-reporter --no-pager | tail -n 50; exit 1)",
              "curl -f -s http://127.0.0.1:5000/ | grep -q \"Charter\" || (echo \"App content check failed\"; exit 1)",
              "echo \"Deployment successful: $(date)\"",
              "sudo rm -rf /var/www/charter-reporter.backup.*",
              "rm /tmp/app.zip"
            ]' \
            --comment "Charter Reporter deploy ${{ github.sha }}" \
            --region ${{ env.AWS_REGION }} \
            --query 'Command.CommandId' --output text)
          
          # Wait for deployment to complete
          aws ssm wait command-executed --command-id $COMMAND_ID --instance-id ${{ env.INSTANCE_ID }} --region ${{ env.AWS_REGION }}
          
          # Check deployment status
          aws ssm get-command-invocation --command-id $COMMAND_ID --instance-id ${{ env.INSTANCE_ID }} --region ${{ env.AWS_REGION }} --query 'Status'
        env:
          AWS_ACCESS_KEY_ID: ${{ secrets.AWS_ACCESS_KEY_ID }}
          AWS_SECRET_ACCESS_KEY: ${{ secrets.AWS_SECRET_ACCESS_KEY }}
```

**Rollback Procedure:**
```bash
# Emergency rollback to previous version
aws ssm send-command \
  --instance-ids "i-xxxxxxxxxxxx" \
  --document-name "AWS-RunShellScript" \
  --parameters commands='[
    "sudo systemctl stop charter-reporter",
    "sudo rm -rf /var/www/charter-reporter.current",
    "sudo mv /var/www/charter-reporter /var/www/charter-reporter.failed",
    "sudo mv /var/www/charter-reporter.backup.* /var/www/charter-reporter",
    "sudo systemctl start charter-reporter",
    "curl -f http://127.0.0.1:5000/health || echo \"Rollback failed\""
  ]' --region af-south-1
```

**Blue-Green Deployment Option:**
For zero-downtime deployments, consider ALB with target groups for true blue-green deployments once traffic justifies the ALB cost (~$20/month).


### 17) Production Configuration and Database Setup

**Application Configuration Files Required:**
1. **appsettings.Production.json** (created in section 11)
2. **Database migration on first deployment:**

```bash
# Run database setup on first deployment (add to SSM deploy script)
"sudo -u ec2-user dotnet /var/www/charter-reporter/Charter.Reporter.Web.dll --migrate-only || echo 'Migration completed or already applied'"
```

**SQLite Configuration:**
- **Path**: `/var/www/charter-reporter/data/app.db` (on EBS, included in backups)
- **Connection pooling**: Enabled by default in EF Core
- **WAL mode**: Configured automatically for better concurrent access
- **Backup strategy**: File-level backup via EBS snapshots + integrity checks

**DataProtection Keys:**
- **Location**: `/var/app/keys` (persistent across deployments)
- **Lifecycle**: 90-day default rotation
- **Backup**: Included in EBS snapshots

**Environment-Specific Overrides:**
```bash
# systemd service environment variables (already configured in section 10)
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=AWS_REGION=af-south-1
Environment=ConnectionStrings__AppDb=Data Source=/var/www/charter-reporter/data/app.db
```

**Configuration Validation on Startup:**
Add to `Program.cs` (startup validation):
```csharp
// Validate critical configuration at startup
var requiredSettings = new[] {
    "MariaDb:Moodle:Host",
    "MariaDb:Woo:Host",
    "Admin:Email"
};

foreach (var setting in requiredSettings)
{
    if (string.IsNullOrEmpty(builder.Configuration[setting]))
    {
        throw new InvalidOperationException($"Required configuration missing: {setting}");
    }
}
```

**Email Configuration:**
- **Development**: Uses `DevNoopEmailSender` (no-op implementation)
- **Production**: SMTP via Parameter Store or SES (recommended for reliability)
- **SES Setup** (if chosen): Verify domain, configure DKIM, store creds in Parameter Store


### 18) Security Hardening

- Use SSM only (no SSH). If you must use SSH for emergencies, allow your office IP on port 22 temporarily.
- Keep system updated: create SSM Patch Manager baseline/maintenance window.
- Principle of least privilege in IAM roles and S3 bucket policies.
- Restrict outbound if compliance requires; otherwise keep default for Let’s Encrypt renewals and package repos.
- Enable `HttpOnly`, `Secure`, `SameSite=Strict` cookies in app (per README). Enforce HSTS via Nginx.


### 19) Day-2 Operations and Maintenance

**Log Rotation Setup:**
```bash
# Configure logrotate for application logs
sudo tee /etc/logrotate.d/charter-reporter >/dev/null <<'LOGROTATE'
/var/log/charter-reporter/*.log {
    daily
    rotate 30
    compress
    delaycompress
    missingok
    create 0644 ec2-user ec2-user
    postrotate
        systemctl reload charter-reporter
    endscript
}
LOGROTATE

# Test logrotate configuration
sudo logrotate -d /etc/logrotate.d/charter-reporter
```

**Performance Monitoring and Scaling Triggers:**
Monitor these metrics and scale when thresholds are reached:
- **CPU > 70% sustained 1+ hours**: Scale to next instance size (`t3.small` → `t3.medium`)
- **Memory > 85%**: Investigate export operations; consider memory-optimized instance
- **Disk > 80%**: Implement log rotation; expand EBS volume
- **Response time > 2s P95**: Add ALB + second instance for load distribution
- **MariaDB connections > 80 concurrent**: Investigate connection pooling; optimize queries

**Database Maintenance:**
```bash
# SQLite optimization (run weekly via cron)
#!/bin/bash
# /usr/local/bin/sqlite-maintenance.sh
cd /var/www/charter-reporter/data
echo "Running SQLite maintenance: $(date)"
sqlite3 app.db "VACUUM;"
sqlite3 app.db "ANALYZE;"
echo "SQLite size after maintenance: $(du -h app.db)"
```

**Security Updates Automation:**
```bash
# Create SSM Patch Manager maintenance window
aws ssm create-maintenance-window \
  --name "charter-reporter-patching" \
  --schedule "cron(0 3 ? * SUN *)" \
  --duration 4 \
  --cutoff 1 \
  --allow-unassociated-targets \
  --region af-south-1

# Register EC2 instance for patching
aws ssm register-target-with-maintenance-window \
  --window-id mw-XXXXXXXXX \
  --targets Key=InstanceIds,Values=i-INSTANCEID \
  --resource-type INSTANCE \
  --region af-south-1
```

**Health Check Automation:**
```bash
# /usr/local/bin/health-check.sh - run every 5 minutes via cron
#!/bin/bash
APP_HEALTH=$(curl -s -o /dev/null -w "%{http_code}" http://127.0.0.1:5000/health)
NGINX_STATUS=$(systemctl is-active nginx)
APP_STATUS=$(systemctl is-active charter-reporter)

if [ "$APP_HEALTH" != "200" ] || [ "$NGINX_STATUS" != "active" ] || [ "$APP_STATUS" != "active" ]; then
    echo "ALERT: Service unhealthy - App:$APP_HEALTH Nginx:$NGINX_STATUS App:$APP_STATUS" | logger -t charter-reporter-health
    # Optionally restart services or send SNS alert
fi
```

**Cost Optimization Reviews:**
- **Weekly**: Review CloudWatch costs and adjust log retention if needed
- **Monthly**: Analyze EC2 utilization for rightsizing opportunities
- **Quarterly**: Evaluate Reserved Instance vs On-Demand savings


### 20) Scaling and HA Options (Trade-offs)

- Add ALB + Auto Scaling Group across 2 AZs for HA. Use ACM cert on ALB (managed TLS), protect instances in private subnets with NAT (adds cost) or keep public with SG locked down.
- Use S3 + CloudFront for `wwwroot` offload if bandwidth grows (saves EC2 CPU). Adds per-GB transfer costs, but offloads spikes.
- Move to `t3.medium`/`t3.large` as CPU/memory requires; adopt a 1‑year Savings Plan once usage stabilizes.
- Keep SQLite unless concurrency/size forces a change; if you need a managed DB for app data, migrate to RDS PostgreSQL or MySQL (adds cost and ops weight). Source MariaDB stays read‑only as per design.


### 20) Windows + IIS Path (If You Must Match README Exactly)

Trade-off: Higher licensing cost; simpler for teams already standardized on IIS.

Key differences:
- AMI: Windows Server 2022 Base; instance type should be at least `t3.small`/`t3.medium` for RAM.
- Install ASP.NET Core Hosting Bundle, configure IIS site binding (443), URL Rewrite for HTTPS redirect.
- TLS: win-acme (Let’s Encrypt) or ACM+ALB in front. win-acme automates renewals.
- Service model: ASP.NET Core Module hosts the app via IIS; no systemd.
- SSM: use Session Manager with the Windows SSM agent; disable RDP ingress.
- Backups, logs, S3/SSM deploy flow: conceptually identical to Linux path.


### 21) Runbooks (Ops)

- Deploy new version: trigger GitHub Action → S3 upload → SSM command runs → service restart → smoke check on localhost.
- Rollback: re-deploy prior artifact. If instance fails, restore from latest EBS snapshot and swap Elastic IP.
- Certificate renewals: Certbot auto-renews. Confirm monthly via logs `sudo journalctl -u nginx --since "-35 days" | grep -i certbot`.
- Capacity issue: scale instance up one size; later, introduce ALB + ASG for HA.


### 22) Step-by-Step Validation and Troubleshooting

**Pre-Deployment Validation:**
```bash
# 1. Verify AWS CLI access and region
aws sts get-caller-identity --region af-south-1
aws ec2 describe-regions --region af-south-1

# 2. Test Parameter Store access
aws ssm get-parameter --name "/charter-reporter/admin/email" --region af-south-1

# 3. Verify RDS connectivity from local machine (if accessible)
mysql -h charteracademy-moolrn.cbasq8maywa1.af-south-1.rds.amazonaws.com -u admin -p -e "SELECT 1;"
```

**Post-Deployment Validation Checklist:**
```bash
#!/bin/bash
# /usr/local/bin/deployment-validation.sh
echo "=== Charter Reporter Deployment Validation ==="

# 1. SSL/TLS and Security Headers
echo "1. Testing HTTPS and security headers..."
curl -I https://yourdomain.com | grep -E "(strict-transport-security|x-content-type|x-frame)"

# 2. Health Endpoints
echo "2. Testing health endpoints..."
curl -f https://yourdomain.com/health || echo "FAIL: Health endpoint"

# 3. Authentication Flow
echo "3. Testing authentication redirect..."
curl -s -o /dev/null -w "%{http_code}" https://yourdomain.com/Dashboard | grep -q "302\|401" && echo "PASS: Auth redirect" || echo "FAIL: Auth redirect"

# 4. Rate Limiting
echo "4. Testing rate limiting..."
for i in {1..15}; do curl -s -o /dev/null -w "%{http_code}" https://yourdomain.com/ & done; wait
echo "Check for 429 responses above"

# 5. Static Assets
echo "5. Testing static asset caching..."
curl -I https://yourdomain.com/css/sb-admin-2.min.css | grep -q "cache-control" && echo "PASS: Static caching" || echo "FAIL: Static caching"

# 6. Service Status
echo "6. Checking service status..."
systemctl is-active charter-reporter nginx || echo "FAIL: Services not active"

# 7. Log Writing
echo "7. Testing log generation..."
sudo journalctl -u charter-reporter --since "1 minute ago" --no-pager | grep -q "." && echo "PASS: App logging" || echo "FAIL: App logging"

# 8. MariaDB Connectivity
echo "8. Testing MariaDB from app perspective..."
curl -s https://yourdomain.com/health | jq '.status' | grep -q "Healthy" && echo "PASS: DB connectivity" || echo "FAIL: DB connectivity"

echo "Validation completed: $(date)"
```

**Common Issues and Solutions:**

| Issue | Symptoms | Solution |
|-------|----------|----------|
| 502 Bad Gateway | Nginx shows 502, app logs show startup errors | Check Parameter Store access, verify AWS_REGION environment |
| Authentication loops | Users can't login, redirects to login repeatedly | Verify DataProtection keys directory permissions |
| MariaDB timeouts | Dashboard loads slowly, health check fails | Check security groups, RDS parameter groups |
| High memory usage | App OOM kills, slow export operations | Implement streaming for large exports, add swap file |
| Certificate renewal fails | HTTPS warnings, browser security errors | Check Certbot logs, verify Route 53 DNS propagation |

**Performance Baseline Verification:**
```bash
# Load testing (run from external machine)
# Install: apt-get install apache2-utils
ab -n 100 -c 5 https://yourdomain.com/
# Target: >50 req/sec, <2s average response time

# Memory usage check
free -h && echo "App memory:" && ps aux | grep dotnet | grep -v grep
```

**Security Validation:**
```bash
# SSL Labs rating should be A+ or A
# Test at: https://www.ssllabs.com/ssltest/

# Port scan validation (should show only 80, 443 open)
nmap -p 1-1000 yourdomain.com

# Headers check
curl -I https://yourdomain.com | grep -E "(strict-transport|x-frame|x-content)"
```


### 23) Scaling and HA Options (Trade-offs)





### Appendix B — IAM Inline Policy (S3 Read for Artifacts)

Attach to the EC2 instance role (edit bucket name):
```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Sid": "ReadArtifacts",
      "Effect": "Allow",
      "Action": ["s3:GetObject"],
      "Resource": ["arn:aws:s3:::charter-reporter-artifacts-afs1/*"]
    }
  ]
}
```


### Appendix C — Complete Program.cs Production Setup

**Required changes to `src/Web/Program.cs` for production:**

```csharp
// Add to top of file
using Amazon.SimpleSystemsManagement;

// After builder creation, before builder.Build()
if (!builder.Environment.IsDevelopment())
{
    // Parameter Store integration
    builder.Configuration.AddSystemsManager("/charter-reporter", TimeSpan.FromMinutes(5));
    
    // Load secrets from Parameter Store
    var ssmClient = new AmazonSimpleSystemsManagementClient(Amazon.RegionEndpoint.AFSouth1);
    
    var moodlePassword = await GetParameterValue(ssmClient, "/charter-reporter/mariadb/moodle/password");
    var wooPassword = await GetParameterValue(ssmClient, "/charter-reporter/mariadb/woo/password");
    var adminEmail = await GetParameterValue(ssmClient, "/charter-reporter/admin/email");
    var adminPassword = await GetParameterValue(ssmClient, "/charter-reporter/admin/password");
    
    builder.Configuration["MariaDb:Moodle:Password"] = moodlePassword;
    builder.Configuration["MariaDb:Woo:Password"] = wooPassword;
    builder.Configuration["Admin:Email"] = adminEmail;
    builder.Configuration["Admin:Password"] = adminPassword;
}

// DataProtection configuration (add after service registration)
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("/var/app/keys"))
    .SetApplicationName("Charter.Reporter")
    .SetDefaultKeyLifetime(TimeSpan.FromDays(90));

// Configuration validation
var requiredSettings = new[] {
    "MariaDb:Moodle:Host",
    "MariaDb:Woo:Host",
    "Admin:Email"
};

foreach (var setting in requiredSettings)
{
    if (string.IsNullOrEmpty(builder.Configuration[setting]))
    {
        throw new InvalidOperationException($"Required configuration missing: {setting}");
    }
}

// Helper method (add at bottom of file)
static async Task<string> GetParameterValue(IAmazonSimpleSystemsManagement client, string paramName)
{
    var request = new GetParameterRequest
    {
        Name = paramName,
        WithDecryption = true
    };
    var response = await client.GetParameterAsync(request);
    return response.Parameter.Value;
}
```


### Appendix D — Lightsail Notes (Cheapest Path)

- Create a Linux instance in Cape Town with a static IP.
- Install .NET, Nginx, Certbot the same as EC2.
- Use Lightsail snapshots for backups.
- Trade-offs: less granular IAM/SSM; migration to EC2 later is straightforward (export snapshot).


### Final Summary: From Zero to Production

**Complete Deployment Sequence for Beginners:**
1. **Setup (Sections 4-5)**: AWS account, CLI, budget alerts
2. **Infrastructure (Sections 6-7)**: VPC, security groups, EC2 launch
3. **System Setup (Section 8)**: Install .NET, Nginx, Certbot
4. **Application (Sections 9-11)**: Nginx config, systemd service, secrets
5. **Data Access (Section 12)**: MariaDB connectivity validation
6. **Monitoring (Sections 13-14)**: CloudWatch, alarms, backups
7. **DNS & SSL (Section 15)**: Route 53, Let's Encrypt certificates
8. **Automation (Section 16)**: CI/CD pipeline setup
9. **Validation (Section 22)**: End-to-end testing and verification

**Time Estimates:**
- First-time setup: 4-6 hours (including DNS propagation waits)
- Experienced AWS users: 2-3 hours
- Subsequent deployments: 5-10 minutes via CI/CD

**Success Indicators:**
- [ ] SSL Labs rating A or A+
- [ ] Health endpoint returns 200 with all checks passing
- [ ] Authentication flow works (login/logout)
- [ ] MariaDB queries return data on dashboard
- [ ] Export functionality works without errors
- [ ] Monitoring alerts are active and tested
- [ ] Backup verification script runs successfully

**Cost Reality Check:**
- **Month 1**: ~$25 (including setup time and testing)
- **Steady state**: ~$21.50/month
- **With 1-year Savings Plan**: ~$15/month (after usage patterns stabilize)

**When to Scale Up:**
- Traffic > 1000 users/day → consider `t3.medium`
- Need HA → add ALB + Auto Scaling Group (+$20/month)
- Heavy exports → add S3 + CloudFront for static assets
- Regulatory requirements → private subnets + NAT Gateway

**Expert Notes:**
This architecture prioritizes operational simplicity and cost efficiency over maximum scalability. It's designed to grow with your needs rather than over-engineer for Day 1. The monitoring and automation setup provides the data needed to make informed scaling decisions based on actual usage patterns rather than assumptions.

— End —


