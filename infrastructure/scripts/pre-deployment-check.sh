#!/bin/bash
# Charter Reporter App - Pre-Deployment Validation Script
# Run this script before deploying to validate your configuration

set -e

echo "=== Charter Reporter Pre-Deployment Validation ==="
echo "Date: $(date)"
echo ""

# Color codes for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Check functions
check_aws_cli() {
    echo "🔍 Checking AWS CLI configuration..."
    
    if ! command -v aws &> /dev/null; then
        echo -e "${RED}❌ AWS CLI not found. Please install AWS CLI v2.${NC}"
        return 1
    fi
    
    if ! aws sts get-caller-identity --region af-south-1 &> /dev/null; then
        echo -e "${RED}❌ AWS CLI not configured or no access to af-south-1.${NC}"
        return 1
    fi
    
    ACCOUNT_ID=$(aws sts get-caller-identity --query 'Account' --output text)
    USER_ARN=$(aws sts get-caller-identity --query 'Arn' --output text)
    echo -e "${GREEN}✅ AWS CLI configured${NC}"
    echo "   Account: $ACCOUNT_ID"
    echo "   User: $USER_ARN"
    echo ""
}

check_terraform() {
    echo "🔍 Checking Terraform..."
    
    if ! command -v terraform &> /dev/null; then
        echo -e "${RED}❌ Terraform not found. Please install Terraform >= 1.6.${NC}"
        return 1
    fi
    
    TERRAFORM_VERSION=$(terraform version -json | jq -r '.terraform_version')
    echo -e "${GREEN}✅ Terraform installed: $TERRAFORM_VERSION${NC}"
    echo ""
}

check_terraform_config() {
    echo "🔍 Checking Terraform configuration..."
    
    if [ ! -f "terraform.tfvars" ]; then
        echo -e "${RED}❌ terraform.tfvars not found.${NC}"
        echo "   Copy terraform.tfvars.example to terraform.tfvars and update with your values."
        return 1
    fi
    
    # Check required variables
    REQUIRED_VARS=("vpc_id" "public_subnet_id" "mariadb_security_group_id" "domain_name" "admin_email" "mariadb_moodle_password" "mariadb_woo_password")
    
    for var in "${REQUIRED_VARS[@]}"; do
        if ! grep -q "^$var\s*=" terraform.tfvars || grep -q "^$var\s*=\s*\"\"" terraform.tfvars; then
            echo -e "${RED}❌ Required variable '$var' not set in terraform.tfvars${NC}"
            return 1
        fi
    done
    
    echo -e "${GREEN}✅ terraform.tfvars configuration looks good${NC}"
    echo ""
}

check_aws_permissions() {
    echo "🔍 Checking AWS permissions..."
    
    # Test EC2 permissions
    if ! aws ec2 describe-vpcs --region af-south-1 --max-items 1 &> /dev/null; then
        echo -e "${RED}❌ No EC2 permissions or access to af-south-1${NC}"
        return 1
    fi
    
    # Test IAM permissions
    if ! aws iam list-roles --max-items 1 &> /dev/null; then
        echo -e "${RED}❌ No IAM permissions${NC}"
        return 1
    fi
    
    # Test Parameter Store permissions
    if ! aws ssm describe-parameters --region af-south-1 --max-items 1 &> /dev/null; then
        echo -e "${RED}❌ No SSM Parameter Store permissions${NC}"
        return 1
    fi
    
    echo -e "${GREEN}✅ AWS permissions look sufficient${NC}"
    echo ""
}

check_existing_infrastructure() {
    echo "🔍 Validating existing infrastructure..."
    
    # Extract values from terraform.tfvars
    VPC_ID=$(grep "^vpc_id" terraform.tfvars | cut -d'"' -f2)
    SUBNET_ID=$(grep "^public_subnet_id" terraform.tfvars | cut -d'"' -f2)
    SG_ID=$(grep "^mariadb_security_group_id" terraform.tfvars | cut -d'"' -f2)
    
    # Check VPC exists
    if ! aws ec2 describe-vpcs --vpc-ids "$VPC_ID" --region af-south-1 &> /dev/null; then
        echo -e "${RED}❌ VPC $VPC_ID not found or no access${NC}"
        return 1
    fi
    
    # Check subnet exists and is in the VPC
    SUBNET_VPC=$(aws ec2 describe-subnets --subnet-ids "$SUBNET_ID" --region af-south-1 --query 'Subnets[0].VpcId' --output text 2>/dev/null || echo "null")
    if [ "$SUBNET_VPC" != "$VPC_ID" ]; then
        echo -e "${RED}❌ Subnet $SUBNET_ID not found or not in VPC $VPC_ID${NC}"
        return 1
    fi
    
    # Check security group exists and is in the VPC
    SG_VPC=$(aws ec2 describe-security-groups --group-ids "$SG_ID" --region af-south-1 --query 'SecurityGroups[0].VpcId' --output text 2>/dev/null || echo "null")
    if [ "$SG_VPC" != "$VPC_ID" ]; then
        echo -e "${RED}❌ Security Group $SG_ID not found or not in VPC $VPC_ID${NC}"
        return 1
    fi
    
    echo -e "${GREEN}✅ Existing infrastructure validated${NC}"
    echo "   VPC: $VPC_ID"
    echo "   Subnet: $SUBNET_ID"
    echo "   MariaDB SG: $SG_ID"
    echo ""
}

check_domain() {
    echo "🔍 Checking domain configuration..."
    
    DOMAIN=$(grep "^domain_name" terraform.tfvars | cut -d'"' -f2)
    
    # Check if domain resolves (might not point to our server yet)
    if dig +short "$DOMAIN" &> /dev/null; then
        CURRENT_IP=$(dig +short "$DOMAIN" | head -n1)
        echo -e "${YELLOW}⚠️ Domain $DOMAIN currently points to: $CURRENT_IP${NC}"
        echo "   This will need to be updated after deployment."
    else
        echo -e "${YELLOW}⚠️ Domain $DOMAIN does not resolve yet${NC}"
        echo "   This is normal - you'll configure DNS after deployment."
    fi
    echo ""
}

check_mariadb_connectivity() {
    echo "🔍 Testing MariaDB connectivity..."
    
    # Extract database details
    MOODLE_HOST=$(grep "^mariadb_moodle_host" terraform.tfvars | cut -d'"' -f2)
    MOODLE_USER=$(grep "^mariadb_moodle_username" terraform.tfvars | cut -d'"' -f2)
    WOO_HOST=$(grep "^mariadb_woo_host" terraform.tfvars | cut -d'"' -f2)
    WOO_USER=$(grep "^mariadb_woo_username" terraform.tfvars | cut -d'"' -f2)
    
    echo "   Moodle: $MOODLE_USER@$MOODLE_HOST"
    echo "   WooCommerce: $WOO_USER@$WOO_HOST"
    echo -e "${YELLOW}   Note: Actual connectivity will be tested from the EC2 instance after deployment.${NC}"
    echo ""
}

calculate_costs() {
    echo "💰 Estimated monthly costs (af-south-1):"
    
    INSTANCE_TYPE=$(grep "^instance_type" terraform.tfvars | cut -d'"' -f2 || echo "t3.small")
    EBS_SIZE=$(grep "^ebs_volume_size" terraform.tfvars | cut -d'=' -f2 | tr -d ' ' || echo "30")
    
    case $INSTANCE_TYPE in
        "t3.micro")  EC2_COST="~\$9" ;;
        "t3.small")  EC2_COST="~\$15" ;;
        "t3.medium") EC2_COST="~\$30" ;;
        *) EC2_COST="~\$15" ;;
    esac
    
    EBS_COST=$(echo "scale=2; $EBS_SIZE * 0.10" | bc 2>/dev/null || echo "~\$3")
    
    echo "   - EC2 $INSTANCE_TYPE: $EC2_COST/month"
    echo "   - EBS ${EBS_SIZE}GB: ~\$${EBS_COST}/month"
    echo "   - Route 53: ~\$0.50/month"
    echo "   - CloudWatch: ~\$2/month"
    echo "   - Data transfer: ~\$1/month"
    echo "   - Total: ~\$20-25/month"
    echo ""
}

# Main validation
main() {
    local errors=0
    
    # Change to terraform directory if not already there
    if [ -f "terraform/main.tf" ]; then
        cd terraform
    elif [ ! -f "main.tf" ]; then
        echo -e "${RED}❌ Run this script from the infrastructure/ or infrastructure/terraform/ directory${NC}"
        exit 1
    fi
    
    # Run all checks
    check_aws_cli || ((errors++))
    check_terraform || ((errors++))
    check_terraform_config || ((errors++))
    check_aws_permissions || ((errors++))
    check_existing_infrastructure || ((errors++))
    check_domain
    check_mariadb_connectivity
    calculate_costs
    
    # Summary
    if [ $errors -eq 0 ]; then
        echo -e "${GREEN}🎉 All validation checks passed!${NC}"
        echo ""
        echo "Next steps:"
        echo "1. Run: terraform init"
        echo "2. Run: terraform plan"
        echo "3. Run: terraform apply"
        echo "4. Configure DNS to point to the Elastic IP"
        echo "5. Set up SSL certificate"
        echo "6. Deploy the application"
        echo ""
    else
        echo -e "${RED}❌ $errors validation check(s) failed.${NC}"
        echo "Please fix the issues above before proceeding with deployment."
        exit 1
    fi
}

# Run main function
main "$@"



