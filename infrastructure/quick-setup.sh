#!/bin/bash
# Charter Reporter App - Quick Setup Script
# This script automates the initial setup process

set -e

# Color codes
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

echo -e "${BLUE}=== Charter Reporter App - Quick Setup ===${NC}"
echo ""

# Check if running from correct directory
if [ ! -f "terraform/main.tf" ]; then
    echo -e "${RED}❌ Please run this script from the infrastructure/ directory${NC}"
    exit 1
fi

# Step 1: Prerequisites check
echo -e "${YELLOW}Step 1: Running prerequisites check...${NC}"
if [ -f "scripts/pre-deployment-check.sh" ]; then
    chmod +x scripts/pre-deployment-check.sh
    ./scripts/pre-deployment-check.sh
else
    echo -e "${YELLOW}⚠️ Pre-deployment check script not found, continuing...${NC}"
fi

# Step 2: Copy terraform.tfvars if it doesn't exist
if [ ! -f "terraform/terraform.tfvars" ]; then
    echo -e "${YELLOW}Step 2: Creating terraform.tfvars from example...${NC}"
    cp terraform/terraform.tfvars.example terraform/terraform.tfvars
    echo -e "${RED}❗ IMPORTANT: Edit terraform/terraform.tfvars with your actual values before proceeding!${NC}"
    echo ""
    echo "Required updates:"
    echo "- vpc_id: Your existing VPC ID"
    echo "- public_subnet_id: Public subnet in your VPC"
    echo "- mariadb_security_group_id: Security group for MariaDB"
    echo "- domain_name: Your domain name"
    echo "- admin_email: Your admin email"
    echo "- mariadb_moodle_password: Read-only password for Moodle DB"
    echo "- mariadb_woo_password: Read-only password for WooCommerce DB"
    echo ""
    read -p "Press Enter after updating terraform.tfvars..."
else
    echo -e "${GREEN}✅ terraform.tfvars already exists${NC}"
fi

# Step 3: Terraform initialization
echo -e "${YELLOW}Step 3: Initializing Terraform...${NC}"
cd terraform
terraform init

# Step 4: Validate configuration
echo -e "${YELLOW}Step 4: Validating Terraform configuration...${NC}"
terraform validate
terraform fmt -check

# Step 5: Plan infrastructure
echo -e "${YELLOW}Step 5: Planning infrastructure changes...${NC}"
terraform plan -out=tfplan

# Step 6: Confirm deployment
echo ""
echo -e "${BLUE}=== Ready to Deploy Infrastructure ===${NC}"
echo ""
echo "This will create:"
echo "- EC2 instance (t3.small) with Elastic IP"
echo "- Security groups for web traffic and MariaDB access"
echo "- IAM roles and policies"
echo "- CloudWatch monitoring and alarms"
echo "- S3 bucket for deployments"
echo "- AWS Backup configuration"
echo "- Parameter Store entries for configuration"
echo ""
echo -e "${YELLOW}Estimated cost: ~$20-25/month${NC}"
echo ""
read -p "Continue with deployment? (y/N): " -n 1 -r
echo ""

if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    echo "Deployment cancelled."
    exit 0
fi

# Step 7: Apply infrastructure
echo -e "${YELLOW}Step 7: Applying infrastructure changes...${NC}"
terraform apply -auto-approve tfplan

# Step 8: Display next steps
echo ""
echo -e "${GREEN}🎉 Infrastructure deployment completed!${NC}"
echo ""
echo -e "${BLUE}=== Next Steps ===${NC}"
echo ""

INSTANCE_ID=$(terraform output -raw instance_id)
PUBLIC_IP=$(terraform output -raw public_ip)
DOMAIN=$(terraform output -raw deployment_info | jq -r '.domain_name')
S3_BUCKET=$(terraform output -raw s3_artifacts_bucket)

echo "1. Configure DNS:"
echo "   Point $DOMAIN to $PUBLIC_IP"
echo ""

echo "2. Wait for DNS propagation (2-10 minutes):"
echo "   dig +short $DOMAIN"
echo ""

echo "3. Set up SSL certificate:"
echo "   aws ssm start-session --target $INSTANCE_ID --region af-south-1"
echo "   sudo /usr/local/bin/setup-ssl.sh"
echo ""

echo "4. Deploy the application:"
echo "   # Build your ASP.NET Core app and upload to S3"
echo "   # Then run deployment script on the instance"
echo ""

echo "5. Validate deployment:"
echo "   sudo /usr/local/bin/deployment-validation.sh"
echo ""

echo -e "${GREEN}Infrastructure is ready for application deployment!${NC}"
echo ""
echo "Useful commands:"
echo "  make output        - Show infrastructure details"
echo "  make connect       - Connect to instance via SSM"
echo "  make health        - Check service health"
echo "  make logs          - View application logs"
echo ""
echo "Documentation:"
echo "  - infrastructure/README.md - Infrastructure overview"
echo "  - infrastructure/DEPLOYMENT-GUIDE.md - Complete deployment guide"
echo "  - infrastructure/terraform/terraform.tfvars.example - Configuration reference"
echo ""

# Make scripts executable
chmod +x ../scripts/pre-deployment-check.sh 2>/dev/null || true

echo -e "${BLUE}Setup completed successfully!${NC}"



