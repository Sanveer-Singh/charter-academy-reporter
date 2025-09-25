## Charter Reporter App — AWS Lightsail Deployment (Simplest and Cheapest Path)

Audience: Anyone. Copy/paste friendly with the right defaults. Optimized for South Africa users.

Region: Choose the closest Lightsail location to your users (Cape Town if/when available, else nearest region). Use the same domain/DNS patterns.


### What You’ll Get

- 1× Lightsail Linux instance (Ubuntu LTS), static IP, Nginx reverse proxy, free HTTPS (Let’s Encrypt), and your ASP.NET Core app running as a systemd service. Snapshots for backups. Optional minimal CI/CD.

Trade-offs vs EC2 plan:
- Simpler and cheapest to start, but fewer AWS-native integrations (IAM roles, SSM, AWS Backup). Accept SSH for admin and use file/env-based secrets.


### 1) Create the Instance

1. Go to Lightsail console and click Create instance.
2. Platform: Linux/Unix.
3. Blueprint: OS Only → Ubuntu 22.04 LTS.
4. Choose the smallest plan that fits (start with the lowest tier; you can upgrade later).
5. Name the instance (e.g., charter-reporter).
6. Create instance.


### 2) Attach a Static IP

1. In Lightsail → Networking → Create static IP.
2. Attach to your instance. Note the IP.


### 3) DNS Setup

Option A: Use Lightsail DNS (simplest)
- Create DNS zone in Lightsail (Networking → Create DNS zone).
- Add an A record for your domain (e.g., `example.com`) pointing to the static IP.
- Update your registrar to use Lightsail nameservers.

Option B: Keep your current DNS/Route 53
- Create an A record in your DNS to the static IP.

Wait for DNS to propagate before requesting HTTPS.


### 4) Connect via SSH (browser-based)

- In the Lightsail console, click the terminal icon next to your instance to open a browser SSH session.


### 5) Install Dependencies (Nginx, .NET, Certbot)

Run these commands:
```bash
sudo apt-get update -y
sudo apt-get upgrade -y

# .NET 8 ASP.NET runtime
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt-get update -y
sudo apt-get install -y aspnetcore-runtime-8.0

# Nginx and Certbot
sudo apt-get install -y nginx certbot python3-certbot-nginx unzip

# App directories
sudo mkdir -p /var/www/charter-reporter
sudo mkdir -p /var/app/keys
sudo mkdir -p /var/log/charter-reporter
sudo chown -R ubuntu:ubuntu /var/www/charter-reporter /var/app/keys /var/log/charter-reporter
```


### 6) Configure Nginx (HTTP→HTTPS and Reverse Proxy)

Replace `example.com` with your domain:
```bash
sudo tee /etc/nginx/sites-available/charter-reporter >/dev/null <<'NGINX'
server {
    listen 80;
    server_name example.com www.example.com;
    location /.well-known/acme-challenge/ {
        root /var/www/html;
    }
    location / {
        return 301 https://$host$request_uri;
    }
}

server {
    listen 443 ssl http2;
    server_name example.com www.example.com;

    ssl_certificate     /etc/letsencrypt/live/example.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/example.com/privkey.pem;

    client_max_body_size 25m;

    location / {
        proxy_pass         http://127.0.0.1:5000;
        proxy_http_version 1.1;
        proxy_set_header   Upgrade $http_upgrade;
        proxy_set_header   Connection keep-alive;
        proxy_set_header   Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header   X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto $scheme;
    }
}
NGINX

sudo ln -s /etc/nginx/sites-available/charter-reporter /etc/nginx/sites-enabled/charter-reporter
sudo nginx -t && sudo systemctl reload nginx
```


### 7) HTTPS (Let’s Encrypt)

Run after DNS resolves to your static IP:
```bash
sudo certbot --nginx -d example.com -d www.example.com --agree-tos -m admin@example.com --redirect --non-interactive
```

Auto-renewal is installed by default. Test renewal:
```bash
sudo certbot renew --dry-run
```


### 8) Deploy the App (Manual, simplest)

On your dev machine:
```bash
dotnet publish ./src/Web/Charter.Reporter.Web.csproj -c Release -o out
zip -r charter-reporter.zip out
```

Upload to server (from your machine):
```bash
scp charter-reporter.zip ubuntu@YOUR_STATIC_IP:/home/ubuntu/
```

On the server:
```bash
unzip -o /home/ubuntu/charter-reporter.zip -d /var/www/charter-reporter
sudo chown -R ubuntu:ubuntu /var/www/charter-reporter
```


### 9) Run as a Service (systemd)

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
User=ubuntu
Environment=ASPNETCORE_URLS=http://127.0.0.1:5000
Environment=DOTNET_PrintTelemetryMessage=false

[Install]
WantedBy=multi-user.target
UNIT

sudo systemctl daemon-reload
sudo systemctl enable charter-reporter
sudo systemctl start charter-reporter
sudo systemctl status charter-reporter --no-pager
```


### 10) App Settings and Secrets

Simplest path:
- Use environment variables in the systemd service (add more `Environment=` lines) for SMTP, MariaDB creds, etc.
- Or create `/var/www/charter-reporter/.env` and source it in `ExecStartPre` with a wrapper script.

Example to add an environment variable:
```ini
Environment=ConnectionStrings__AppDb=Data Source=/var/www/charter-reporter/data/app.db
```


### 11) Open Ports in Lightsail Networking

- Ensure ports 80 and 443 are open in the instance’s Networking tab.


### 12) Backups

- Use Lightsail snapshots (manual or automatic) for the instance. Schedule daily snapshots.
- To restore: create a new instance from snapshot and reattach the static IP.


### 13) Minimal CI/CD (Optional)

Option A: GitHub Actions + SCP (simple)
```yaml
name: deploy-lightsail
on:
  workflow_dispatch:

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      - name: Publish
        run: |
          dotnet restore ./src/Web/Charter.Reporter.Web.csproj
          dotnet publish ./src/Web/Charter.Reporter.Web.csproj -c Release -o out
          cd out && zip -r ../charter-reporter.zip . && cd ..
      - name: Copy to server
        uses: appleboy/scp-action@v0.1.7
        with:
          host: ${{ secrets.LS_HOST }}
          username: ubuntu
          key: ${{ secrets.LS_SSH_KEY }}
          source: "charter-reporter.zip"
          target: "/home/ubuntu"
      - name: Restart service
        uses: appleboy/ssh-action@v1.2.0
        with:
          host: ${{ secrets.LS_HOST }}
          username: ubuntu
          key: ${{ secrets.LS_SSH_KEY }}
          script: |
            sudo systemctl stop charter-reporter || true
            sudo unzip -o /home/ubuntu/charter-reporter.zip -d /var/www/charter-reporter
            sudo chown -R ubuntu:ubuntu /var/www/charter-reporter
            sudo systemctl start charter-reporter
            sleep 3
            curl -f http://127.0.0.1:5000/ || (sudo journalctl -u charter-reporter --no-pager | tail -n 200; exit 1)
```

Option B: S3 pull (no inbound SCP)
- Upload artifact to S3, then SSH and `aws s3 cp` from the server (requires AWS CLI configured on server with access keys).


### 14) Security Notes

- Keep SSH key safe; rotate if compromised.
- Enable automatic security updates on Ubuntu.
- Use `ufw` if you want an extra host firewall (allow 80/443, deny others).
- Set secure cookies and HSTS headers (see EC2 guide Appendix A for Nginx headers).


### 15) When to Move to EC2

- Need SSM (no SSH), IAM roles, AWS Backup, CloudWatch Agent, or HA with ALB/ASG.
- Expecting higher traffic or stricter uptime SLAs.


— End —


