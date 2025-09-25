# Charter Reporter App - Terraform Variables

variable "aws_region" {
  description = "AWS region for deployment"
  type        = string
  default     = "af-south-1"
}

variable "environment" {
  description = "Environment name (dev, staging, prod)"
  type        = string
  default     = "prod"
}

variable "vpc_id" {
  description = "VPC ID where existing Moodle/WordPress infrastructure is deployed"
  type        = string
  # Example: vpc-1234567890abcdef0
}

variable "public_subnet_id" {
  description = "Public subnet ID in the existing VPC for the web server"
  type        = string
  # Example: subnet-1234567890abcdef0
}

variable "mariadb_security_group_id" {
  description = "Security Group ID of the existing MariaDB instances"
  type        = string
  # Example: sg-1234567890abcdef0
}

variable "instance_type" {
  description = "EC2 instance type for the web server"
  type        = string
  default     = "t3.micro"
  validation {
    condition = contains([
      "t3.micro", "t3.small", "t3.medium", "t3.large"
    ], var.instance_type)
    error_message = "Instance type must be t3.micro, t3.small, t3.medium, or t3.large."
  }
}

variable "ebs_volume_size" {
  description = "Size of the root EBS volume in GB"
  type        = number
  default     = 20
  validation {
    condition     = var.ebs_volume_size >= 20 && var.ebs_volume_size <= 100
    error_message = "EBS volume size must be between 20 and 100 GB."
  }
}

variable "key_pair_name" {
  description = "EC2 Key Pair name for emergency SSH access (optional)"
  type        = string
  default     = ""
}

variable "domain_name" {
  description = "Domain name for the application (e.g., reporter.charteracademy.co.za)"
  type        = string
  # Example: reporter.charteracademy.co.za
}

variable "admin_email" {
  description = "Administrator email address for SSL certificates and notifications"
  type        = string
  # Example: admin@charteracademy.co.za
}

variable "create_route53_zone" {
  description = "Whether to create a new Route 53 hosted zone for the domain"
  type        = bool
  default     = false
}

# Deployment feature toggles
variable "minimal_deploy" {
  description = "If true, deploys only EC2 + SG + EIP with a minimal bootstrap"
  type        = bool
  default     = true
}

variable "enable_parameter_store" {
  description = "Create and use SSM Parameter Store parameters"
  type        = bool
  default     = false
}

variable "enable_monitoring" {
  description = "Create CloudWatch logs/alarms, backups, dashboard"
  type        = bool
  default     = false
}

variable "enable_artifacts_bucket" {
  description = "Create S3 bucket for deployment artifacts"
  type        = bool
  default     = false
}

variable "enable_db_access" {
  description = "Allow EC2 to access existing MariaDB via SG rule"
  type        = bool
  default     = false
}

variable "enable_https" {
  description = "Open 443 and configure Nginx/Certbot via full bootstrap (non-minimal only)"
  type        = bool
  default     = false
}

# MariaDB Connection Details (existing infrastructure)
variable "mariadb_moodle_host" {
  description = "MariaDB host for Moodle database"
  type        = string
  default     = "charteracademy-moolrn.cbasq8maywa1.af-south-1.rds.amazonaws.com"
}

variable "mariadb_moodle_database" {
  description = "Moodle database name"
  type        = string
  default     = "charteracademy_moolrn"
}

variable "mariadb_moodle_username" {
  description = "Read-only username for Moodle database"
  type        = string
  default     = "readonly_user"
}

variable "mariadb_moodle_table_prefix" {
  description = "Table prefix for Moodle database"
  type        = string
  default     = "moow_"
}

variable "mariadb_woo_host" {
  description = "MariaDB host for WooCommerce database"
  type        = string
  default     = "charteracademy-moolrn.cbasq8maywa1.af-south-1.rds.amazonaws.com"
}

variable "mariadb_woo_database" {
  description = "WooCommerce database name"
  type        = string
  default     = "charteracademy_wp001"
}

variable "mariadb_woo_username" {
  description = "Read-only username for WooCommerce database"
  type        = string
  default     = "readonly_user"
}

variable "mariadb_woo_table_prefix" {
  description = "Table prefix for WooCommerce database"
  type        = string
  default     = "wpbh_"
}

# Secrets (will be stored in Parameter Store)
variable "mariadb_moodle_password" {
  description = "Password for Moodle database read-only user"
  type        = string
  sensitive   = true
}

variable "mariadb_woo_password" {
  description = "Password for WooCommerce database read-only user"
  type        = string
  sensitive   = true
}

variable "smtp_password" {
  description = "SMTP password for email notifications (optional)"
  type        = string
  sensitive   = true
  default     = ""
}

variable "smtp_host" {
  description = "SMTP host for email notifications"
  type        = string
  default     = ""
}

variable "smtp_username" {
  description = "SMTP username for email notifications"
  type        = string
  default     = ""
}

variable "smtp_port" {
  description = "SMTP port for email notifications"
  type        = number
  default     = 587
}

# Monitoring and Alerting
variable "alert_email" {
  description = "Email address for CloudWatch alarms and notifications"
  type        = string
  # Example: alerts@charteracademy.co.za
}

variable "backup_retention_days" {
  description = "Number of days to retain backup snapshots"
  type        = number
  default     = 30
}

variable "log_retention_days" {
  description = "CloudWatch log retention in days"
  type        = number
  default     = 30
  validation {
    condition = contains([
      1, 3, 5, 7, 14, 30, 60, 90, 120, 150, 180, 365, 400, 545, 731, 1096, 1827, 2192, 2557, 2922, 3288, 3653
    ], var.log_retention_days)
    error_message = "Log retention days must be a valid CloudWatch retention period."
  }
}





