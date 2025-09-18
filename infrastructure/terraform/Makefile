# Charter Reporter App - Terraform Makefile
# Simplified commands for infrastructure management

.PHONY: help init plan apply destroy validate clean check-vars output

# Default target
help:
	@echo "Charter Reporter App - Infrastructure Commands"
	@echo ""
	@echo "Setup Commands:"
	@echo "  make init          - Initialize Terraform"
	@echo "  make validate      - Validate Terraform configuration"
	@echo "  make plan          - Preview infrastructure changes"
	@echo "  make apply         - Apply infrastructure changes"
	@echo ""
	@echo "Management Commands:"
	@echo "  make output        - Show important deployment information"
	@echo "  make connect       - Start SSM session to the instance"
	@echo "  make logs          - View application logs"
	@echo "  make health        - Check service health"
	@echo ""
	@echo "Cleanup Commands:"
	@echo "  make destroy       - Destroy all infrastructure (WARNING: Data loss)"
	@echo "  make clean         - Clean local Terraform state"
	@echo ""
	@echo "Prerequisites:"
	@echo "  1. Copy terraform.tfvars.example to terraform.tfvars"
	@echo "  2. Update terraform.tfvars with your actual values"
	@echo "  3. Ensure AWS CLI is configured for af-south-1"

# Check if terraform.tfvars exists
check-vars:
	@if [ ! -f terraform.tfvars ]; then \
		echo "ERROR: terraform.tfvars not found!"; \
		echo "Copy terraform.tfvars.example to terraform.tfvars and update with your values."; \
		exit 1; \
	fi

# Initialize Terraform
init:
	@echo "Initializing Terraform..."
	terraform init

# Validate configuration
validate: check-vars
	@echo "Validating Terraform configuration..."
	terraform validate
	terraform fmt -check

# Preview changes
plan: check-vars validate
	@echo "Planning infrastructure changes..."
	terraform plan -out=tfplan

# Apply changes
apply: check-vars validate
	@echo "Applying infrastructure changes..."
	terraform apply -auto-approve tfplan
	@echo ""
	@echo "=== Deployment Complete ==="
	@echo "Next Steps:"
	@echo "1. Configure DNS to point to: $$(terraform output -raw public_ip)"
	@echo "2. Connect to instance: make connect"
	@echo "3. Set up SSL: sudo /usr/local/bin/setup-ssl.sh"
	@echo "4. Deploy application via CI/CD or manually"

# Show important outputs
output:
	@echo "=== Charter Reporter Infrastructure Information ==="
	@echo "Instance ID: $$(terraform output -raw instance_id)"
	@echo "Public IP: $$(terraform output -raw public_ip)"
	@echo "S3 Bucket: $$(terraform output -raw s3_artifacts_bucket)"
	@echo ""
	@echo "Connect to instance:"
	@echo "  $$(terraform output -raw ssm_session_command)"
	@echo ""
	@echo "DNS Configuration Needed:"
	@echo "  Point your domain to: $$(terraform output -raw public_ip)"

# Connect to instance via SSM
connect:
	@echo "Starting SSM session..."
	aws ssm start-session --target $$(terraform output -raw instance_id) --region af-south-1

# View application logs
logs:
	@echo "Fetching recent application logs..."
	aws logs tail /charter-reporter/app --since 1h --region af-south-1

# Health check
health:
	@echo "Checking service health..."
	@INSTANCE_ID=$$(terraform output -raw instance_id); \
	aws ssm send-command \
		--instance-ids "$$INSTANCE_ID" \
		--document-name "AWS-RunShellScript" \
		--parameters 'commands=["/usr/local/bin/deployment-validation.sh"]' \
		--region af-south-1 \
		--query 'Command.CommandId' --output text | \
	while read COMMAND_ID; do \
		echo "Command ID: $$COMMAND_ID"; \
		aws ssm wait command-executed --command-id "$$COMMAND_ID" --instance-id "$$INSTANCE_ID" --region af-south-1; \
		aws ssm get-command-invocation --command-id "$$COMMAND_ID" --instance-id "$$INSTANCE_ID" --region af-south-1 --query 'StandardOutputContent' --output text; \
	done

# Destroy infrastructure
destroy: check-vars
	@echo "WARNING: This will destroy all infrastructure and data!"
	@echo "Press Ctrl+C within 10 seconds to cancel..."
	@sleep 10
	terraform destroy -auto-approve

# Clean local state
clean:
	rm -f tfplan
	rm -rf .terraform/
	rm -f .terraform.lock.hcl

# Development helpers
fmt:
	terraform fmt -recursive

lint: validate
	@echo "Running additional checks..."
	@# Add tflint or other tools here if available

# Backup verification
backup-status:
	@echo "Checking backup status..."
	aws backup list-recovery-points \
		--backup-vault-name charter-reporter-vault \
		--region af-south-1 \
		--query 'RecoveryPoints[0:5].[RecoveryPointArn,CreationDate,Status]' \
		--output table

# Show costs
costs:
	@echo "Estimated monthly costs for af-south-1:"
	@echo "- EC2 t3.micro: ~$$9/month"
	@echo "- EBS 20GB gp3: ~$$2/month"
	@echo "- Route 53: ~$$0.50/month"
	@echo "- CloudWatch: ~$$2/month"
	@echo "- Total: ~$$13.50/month"





