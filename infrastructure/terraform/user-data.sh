#!/bin/bash
# Charter Reporter App - EC2 Bootstrap Script
# This script runs on first boot to set up the environment

set -e

# Logging setup
exec > >(tee /var/log/charter-reporter-bootstrap.log)
exec 2>&1

echo "=== Charter Reporter Bootstrap Started: $(date) ==="

# Update system packages
echo "Updating system packages..."
dnf update -y

# Install .NET 8 Runtime
echo "Installing .NET 8 Runtime..."
curl -sSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
bash /tmp/dotnet-install.sh --channel 8.0 --install-dir /usr/share/dotnet --runtime aspnetcore
ln -sf /usr/share/dotnet/dotnet /usr/bin/dotnet

# Install Nginx
echo "Installing and configuring Nginx..."
dnf install -y nginx
systemctl enable nginx

# Install Certbot for Let's Encrypt
echo "Installing Certbot..."
dnf install -y python3-certbot-nginx

# Install CloudWatch Agent
echo "Installing CloudWatch Agent..."
dnf install -y amazon-cloudwatch-agent

# Create application directories
echo "Creating application directories..."
mkdir -p /var/www/charter-reporter/data
mkdir -p /var/app/keys
mkdir -p /var/log/charter-reporter
chown -R ec2-user:ec2-user /var/www/charter-reporter /var/app/keys /var/log/charter-reporter

# Create Nginx configuration
echo "Configuring Nginx..."
cat > /etc/nginx/conf.d/charter-reporter.conf << 'NGINX_EOF'
# Rate limiting zones
limit_req_zone $binary_remote_addr zone=general:10m rate=10r/s;
limit_req_zone $binary_remote_addr zone=auth:10m rate=3r/m;
limit_req_zone $binary_remote_addr zone=export:10m rate=1r/m;

server {
    listen 80;
    server_name ${domain_name} www.${domain_name};
    return 301 https://$host$request_uri;
}

server {
    listen 443 ssl http2;
    server_name ${domain_name} www.${domain_name};

    # SSL Configuration (will be populated by Certbot)
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
NGINX_EOF

# Create proxy params
cat > /etc/nginx/proxy_params << 'PROXY_EOF'
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
PROXY_EOF

# Test Nginx configuration and start
nginx -t
systemctl start nginx

# Create systemd service for the Charter Reporter app
echo "Creating systemd service..."
cat > /etc/systemd/system/charter-reporter.service << 'SERVICE_EOF'
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
Environment=AWS_REGION=${aws_region}
Environment=DOTNET_PrintTelemetryMessage=false
Environment=ConnectionStrings__AppDb=Data Source=/var/www/charter-reporter/data/app.db

[Install]
WantedBy=multi-user.target
SERVICE_EOF

systemctl daemon-reload
systemctl enable charter-reporter

# Configure CloudWatch Agent
echo "Configuring CloudWatch Agent..."
cat > /opt/aws/amazon-cloudwatch-agent/etc/amazon-cloudwatch-agent.json << 'CW_EOF'
{
  "logs": {
    "logs_collected": {
      "files": {
        "collect_list": [
          {
            "file_path": "/var/log/charter-reporter/*.log",
            "log_group_name": "/charter-reporter/app",
            "log_stream_name": "{instance_id}",
            "retention_in_days": 30
          },
          {
            "file_path": "/var/log/nginx/access.log",
            "log_group_name": "/charter-reporter/nginx/access",
            "log_stream_name": "{instance_id}",
            "retention_in_days": 14
          },
          {
            "file_path": "/var/log/nginx/error.log",
            "log_group_name": "/charter-reporter/nginx/error",
            "log_stream_name": "{instance_id}",
            "retention_in_days": 30
          },
          {
            "file_path": "/var/log/messages",
            "log_group_name": "/charter-reporter/system",
            "log_stream_name": "{instance_id}",
            "retention_in_days": 7
          }
        ]
      }
    }
  },
  "metrics": {
    "append_dimensions": {
      "InstanceId": "$${aws:InstanceId}",
      "InstanceType": "$${aws:InstanceType}"
    },
    "metrics_collected": {
      "cpu": {
        "measurement": ["cpu_usage_idle", "cpu_usage_iowait"],
        "metrics_collection_interval": 60
      },
      "mem": {
        "measurement": ["mem_used_percent", "mem_available_percent"],
        "metrics_collection_interval": 60
      },
      "disk": {
        "measurement": ["used_percent", "inodes_free"],
        "resources": ["*"],
        "metrics_collection_interval": 60
      },
      "netstat": {
        "measurement": ["tcp_established", "tcp_time_wait"],
        "metrics_collection_interval": 60
      }
    }
  }
}
CW_EOF

# Start CloudWatch Agent
/opt/aws/amazon-cloudwatch-agent/bin/amazon-cloudwatch-agent-ctl \
  -a start -m ec2 \
  -c file:/opt/aws/amazon-cloudwatch-agent/etc/amazon-cloudwatch-agent.json

# Create backup verification script
echo "Creating backup verification script..."
cat > /usr/local/bin/verify-backup.sh << 'BACKUP_EOF'
#!/bin/bash
echo "=== Charter Reporter Backup Verification ==="
echo "SQLite DB size: $(du -h /var/www/charter-reporter/data/app.db 2>/dev/null || echo 'Not deployed yet')"
echo "Keys directory: $(ls -la /var/app/keys/)"
echo "Latest backup: $(aws backup list-recovery-points --backup-vault-name charter-reporter-vault --region ${aws_region} --query 'RecoveryPoints[0].RecoveryPointArn' 2>/dev/null || echo 'No backups yet')"
echo "Disk usage: $(df -h /)"

# Test SQLite integrity if DB exists
if [ -f /var/www/charter-reporter/data/app.db ]; then
    sqlite3 /var/www/charter-reporter/data/app.db "PRAGMA integrity_check;" || echo "SQLite integrity check FAILED"
fi

echo "Backup verification completed: $(date)"
BACKUP_EOF

chmod +x /usr/local/bin/verify-backup.sh

# Create health check script
echo "Creating health check script..."
cat > /usr/local/bin/health-check.sh << 'HEALTH_EOF'
#!/bin/bash
APP_HEALTH=$(curl -s -o /dev/null -w "%{http_code}" http://127.0.0.1:5000/health)
NGINX_STATUS=$(systemctl is-active nginx)
APP_STATUS=$(systemctl is-active charter-reporter)

if [ "$APP_HEALTH" != "200" ] || [ "$NGINX_STATUS" != "active" ] || [ "$APP_STATUS" != "active" ]; then
    echo "ALERT: Service unhealthy - App:$APP_HEALTH Nginx:$NGINX_STATUS App:$APP_STATUS" | logger -t charter-reporter-health
fi
HEALTH_EOF

chmod +x /usr/local/bin/health-check.sh

# Create SQLite maintenance script
echo "Creating SQLite maintenance script..."
cat > /usr/local/bin/sqlite-maintenance.sh << 'SQLITE_EOF'
#!/bin/bash
if [ -f /var/www/charter-reporter/data/app.db ]; then
    cd /var/www/charter-reporter/data
    echo "Running SQLite maintenance: $(date)"
    sqlite3 app.db "VACUUM;"
    sqlite3 app.db "ANALYZE;"
    echo "SQLite size after maintenance: $(du -h app.db)"
else
    echo "SQLite database not found, skipping maintenance"
fi
SQLITE_EOF

chmod +x /usr/local/bin/sqlite-maintenance.sh

# Set up cron jobs
echo "Setting up cron jobs..."
cat > /tmp/charter-cron << 'CRON_EOF'
# Health check every 5 minutes
*/5 * * * * /usr/local/bin/health-check.sh

# Backup verification daily at 3 AM
0 3 * * * /usr/local/bin/verify-backup.sh

# SQLite maintenance weekly on Sundays at 2 AM
0 2 * * 0 /usr/local/bin/sqlite-maintenance.sh
CRON_EOF

crontab -u ec2-user /tmp/charter-cron
rm /tmp/charter-cron

# Configure logrotate for application logs
cat > /etc/logrotate.d/charter-reporter << 'LOGROTATE_EOF'
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
LOGROTATE_EOF

# Create deployment script for future use
echo "Creating deployment script..."
cat > /usr/local/bin/deploy-charter-reporter.sh << 'DEPLOY_EOF'
#!/bin/bash
# Charter Reporter Deployment Script
# Usage: deploy-charter-reporter.sh <s3-artifact-url>

set -e

if [ $# -ne 1 ]; then
    echo "Usage: $0 <s3-artifact-url>"
    echo "Example: $0 s3://${s3_artifacts_bucket}/charter-reporter-abc123.zip"
    exit 1
fi

ARTIFACT_URL=$1
BACKUP_DIR="/var/www/charter-reporter.backup.$(date +%s)"

echo "Starting deployment: $(date)"
echo "Artifact: $ARTIFACT_URL"

# Download artifact
echo "Downloading artifact..."
aws s3 cp "$ARTIFACT_URL" /tmp/app.zip --region ${aws_region}

# Stop service
echo "Stopping application..."
systemctl stop charter-reporter

# Backup current deployment
echo "Creating backup..."
cp -r /var/www/charter-reporter "$BACKUP_DIR"

# Deploy new version
echo "Deploying new version..."
rm -rf /var/www/charter-reporter/*
unzip -o /tmp/app.zip -d /var/www/charter-reporter
chown -R ec2-user:ec2-user /var/www/charter-reporter

# Start service
echo "Starting application..."
systemctl start charter-reporter

# Health check
echo "Performing health check..."
sleep 10
if curl -f http://127.0.0.1:5000/health; then
    echo "Deployment successful: $(date)"
    rm -rf "$BACKUP_DIR"
    rm /tmp/app.zip
else
    echo "Health check failed, rolling back..."
    systemctl stop charter-reporter
    rm -rf /var/www/charter-reporter
    mv "$BACKUP_DIR" /var/www/charter-reporter
    systemctl start charter-reporter
    echo "Rollback completed"
    exit 1
fi
DEPLOY_EOF

chmod +x /usr/local/bin/deploy-charter-reporter.sh

# Set up automatic security updates
echo "Configuring automatic security updates..."
dnf install -y dnf-automatic
sed -i 's/apply_updates = no/apply_updates = yes/' /etc/dnf/automatic.conf
sed -i 's/upgrade_type = default/upgrade_type = security/' /etc/dnf/automatic.conf
systemctl enable dnf-automatic.timer
systemctl start dnf-automatic.timer

# Wait for Let's Encrypt after DNS is configured
echo "Creating SSL setup script for manual execution after DNS configuration..."
cat > /usr/local/bin/setup-ssl.sh << 'SSL_EOF'
#!/bin/bash
# Run this script after DNS points to this server
# Usage: sudo /usr/local/bin/setup-ssl.sh

set -e

echo "Setting up SSL certificate for ${domain_name}..."

# Obtain certificate
certbot --nginx \
  -d ${domain_name} \
  -d www.${domain_name} \
  --agree-tos \
  -m ${admin_email} \
  --redirect \
  --non-interactive

# Test renewal
certbot renew --dry-run

echo "SSL setup completed successfully"
echo "Your site should now be available at https://${domain_name}"
SSL_EOF

chmod +x /usr/local/bin/setup-ssl.sh

# Create validation script
echo "Creating validation script..."
cat > /usr/local/bin/deployment-validation.sh << 'VALIDATE_EOF'
#!/bin/bash
echo "=== Charter Reporter Deployment Validation ==="

# 1. SSL/TLS and Security Headers (skip if no SSL yet)
echo "1. Testing HTTPS and security headers..."
if curl -k -I https://${domain_name} 2>/dev/null | grep -E "(strict-transport-security|x-content-type|x-frame)"; then
    echo "PASS: Security headers present"
else
    echo "INFO: SSL not configured yet or security headers missing"
fi

# 2. HTTP redirect test
echo "2. Testing HTTP to HTTPS redirect..."
if curl -s -o /dev/null -w "%{http_code}" http://${domain_name} | grep -q "301"; then
    echo "PASS: HTTP redirect working"
else
    echo "INFO: HTTP redirect not working (normal if SSL not set up)"
fi

# 3. Service Status
echo "3. Checking service status..."
systemctl is-active nginx || echo "FAIL: Nginx not active"
systemctl is-active charter-reporter || echo "INFO: Charter Reporter service not active (normal before deployment)"

# 4. Directory Setup
echo "4. Checking directory setup..."
[ -d /var/www/charter-reporter ] && echo "PASS: App directory exists" || echo "FAIL: App directory missing"
[ -d /var/app/keys ] && echo "PASS: Keys directory exists" || echo "FAIL: Keys directory missing"
[ -d /var/log/charter-reporter ] && echo "PASS: Log directory exists" || echo "FAIL: Log directory missing"

# 5. CloudWatch Agent
echo "5. Checking CloudWatch Agent..."
if /opt/aws/amazon-cloudwatch-agent/bin/amazon-cloudwatch-agent-ctl -m ec2 -a query-config; then
    echo "PASS: CloudWatch Agent running"
else
    echo "FAIL: CloudWatch Agent not running"
fi

# 6. Backup script test
echo "6. Testing backup verification..."
/usr/local/bin/verify-backup.sh

echo "Validation completed: $(date)"
VALIDATE_EOF

chmod +x /usr/local/bin/deployment-validation.sh

echo "=== Charter Reporter Bootstrap Completed: $(date) ==="
echo "Next steps:"
echo "1. Configure DNS to point to this server's Elastic IP"
echo "2. Run: sudo /usr/local/bin/setup-ssl.sh"
echo "3. Deploy the application using the CI/CD pipeline"
echo "4. Run: sudo /usr/local/bin/deployment-validation.sh"



