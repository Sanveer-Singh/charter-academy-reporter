# Charter Reporter App - Terraform Outputs

output "instance_id" {
  description = "ID of the Charter Reporter EC2 instance"
  value       = aws_instance.charter_reporter.id
}

output "public_ip" {
  description = "Elastic IP address of the Charter Reporter instance"
  value       = aws_eip.charter_reporter.public_ip
}

output "instance_dns" {
  description = "Public DNS name of the Charter Reporter instance"
  value       = aws_instance.charter_reporter.public_dns
}

output "security_group_id" {
  description = "Security Group ID for the Charter Reporter web application"
  value       = aws_security_group.charter_reporter_web.id
}

output "iam_role_arn" {
  description = "ARN of the IAM role for the EC2 instance"
  value       = aws_iam_role.charter_reporter_ec2.arn
}

output "s3_artifacts_bucket" {
  description = "S3 bucket name for deployment artifacts"
  value       = aws_s3_bucket.artifacts.bucket
}

output "route53_zone_id" {
  description = "Route 53 hosted zone ID (if created)"
  value       = var.create_route53_zone ? aws_route53_zone.main[0].zone_id : null
}

output "route53_nameservers" {
  description = "Route 53 nameservers (if zone created)"
  value       = var.create_route53_zone ? aws_route53_zone.main[0].name_servers : null
}

# Connection information for application configuration
output "deployment_info" {
  description = "Key deployment information for application setup"
  value = {
    domain_name           = var.domain_name
    elastic_ip           = aws_eip.charter_reporter.public_ip
    instance_id          = aws_instance.charter_reporter.id
    s3_artifacts_bucket  = aws_s3_bucket.artifacts.bucket
    parameter_store_path = "/charter-reporter"
  }
}

# SSH connection command (if key pair is used)
output "ssh_connection_command" {
  description = "SSH command to connect to the instance (if key pair is configured)"
  value       = var.key_pair_name != "" ? "ssh -i ~/.ssh/${var.key_pair_name}.pem ec2-user@${aws_eip.charter_reporter.public_ip}" : "Use AWS SSM Session Manager for secure access"
}

# SSM Session Manager connection
output "ssm_session_command" {
  description = "AWS CLI command to start SSM session"
  value       = "aws ssm start-session --target ${aws_instance.charter_reporter.id} --region ${var.aws_region}"
}

# Parameter Store setup commands
output "parameter_store_setup_commands" {
  description = "Commands to set up secrets in Parameter Store"
  value = [
    "aws ssm put-parameter --name '/charter-reporter/mariadb/moodle/password' --type SecureString --value 'YOUR_MOODLE_PASSWORD' --overwrite --region ${var.aws_region}",
    "aws ssm put-parameter --name '/charter-reporter/mariadb/woo/password' --type SecureString --value 'YOUR_WOO_PASSWORD' --overwrite --region ${var.aws_region}",
    "aws ssm put-parameter --name '/charter-reporter/admin/email' --type String --value '${var.admin_email}' --overwrite --region ${var.aws_region}",
    "aws ssm put-parameter --name '/charter-reporter/admin/password' --type SecureString --value '${random_password.admin_password.result}' --overwrite --region ${var.aws_region}"
  ]
  sensitive = true
}

output "generated_admin_password" {
  description = "Generated admin password for initial setup"
  value       = random_password.admin_password.result
  sensitive   = true
}





