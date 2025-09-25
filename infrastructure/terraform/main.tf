# Charter Reporter App - Infrastructure as Code
# Region: af-south-1 (Cape Town) for SA users

terraform {
  required_version = ">= 1.6"
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
    random = {
      source  = "hashicorp/random"
      version = "~> 3.1"
    }
  }
}

provider "aws" {
  region = var.aws_region
  
  default_tags {
    tags = {
      Project     = "Charter-Reporter"
      Environment = var.environment
      ManagedBy   = "Terraform"
    }
  }
}

# Current account identity (for scoping IAM policies)
data "aws_caller_identity" "current" {}

# Data sources to reference existing infrastructure
data "aws_vpc" "existing" {
  id = var.vpc_id
}

data "aws_subnet" "existing_public" {
  id = var.public_subnet_id
}

data "aws_security_group" "mariadb" {
  id = var.mariadb_security_group_id
}

# Random password for initial admin user
resource "random_password" "admin_password" {
  length  = 16
  special = true
}

# Elastic IP for the EC2 instance
resource "aws_eip" "charter_reporter" {
  domain = "vpc"
  
  tags = {
    Name = "charter-reporter-eip"
  }
}

# Security Group for the Charter Reporter web application
resource "aws_security_group" "charter_reporter_web" {
  name_prefix = "sg-charter-reporter-web"
  description = "Security group for Charter Reporter web application"
  vpc_id      = data.aws_vpc.existing.id

    description = "HTTPS"
    from_port   = 443
    to_port     = 443
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }
  }

  ingress {
    description = "HTTP"
    from_port   = 80
    to_port     = 80
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  # Minimal, reliable outbound: allow all egress
  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = {
    Name = "sg-charter-reporter-web"
  }
}

# Add ingress rule to existing MariaDB security group to allow Charter Reporter access
resource "aws_security_group_rule" "mariadb_from_charter_reporter" {
  source_security_group_id = aws_security_group.charter_reporter_web.id
}

# IAM Instance Profile for the EC2 instance
resource "aws_iam_role" "charter_reporter_ec2" {
  name = "EC2-CharterReporter-InstanceRole"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Action = "sts:AssumeRole"
        Effect = "Allow"
        Principal = {
          Service = "ec2.amazonaws.com"
        }
      }
    ]
  })

  tags = {
    Name = "EC2-CharterReporter-InstanceRole"
  }
}

# Attach AWS managed policies
resource "aws_iam_role_policy_attachment" "ssm_managed_instance_core" {
  role       = aws_iam_role.charter_reporter_ec2.name
  policy_arn = "arn:aws:iam::aws:policy/AmazonSSMManagedInstanceCore"
}

resource "aws_iam_role_policy_attachment" "cloudwatch_agent_server_policy" {
  count      = var.enable_monitoring ? 1 : 0
  role       = aws_iam_role.charter_reporter_ec2.name
  policy_arn = "arn:aws:iam::aws:policy/CloudWatchAgentServerPolicy"
}

# Custom policy for Parameter Store access
resource "aws_iam_role_policy" "parameter_store_access" {

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Sid    = "ReadSSMParameters"
        Effect = "Allow"
        Action = [
          "ssm:GetParameter",
          "ssm:GetParameters"
        ]
        Resource = [
          "arn:aws:ssm:${var.aws_region}:${data.aws_caller_identity.current.account_id}:parameter/charter-reporter/*"
        ]
      {
        Sid    = "ReadArtifacts"
        Effect = "Allow"
        Action = [
          "s3:GetObject"
        ]
        Resource = [
        ]
      }
    ]
  })
}

resource "aws_iam_instance_profile" "charter_reporter_ec2" {
  name = "charter-reporter-ec2-profile"
  role = aws_iam_role.charter_reporter_ec2.name
}

# S3 bucket for deployment artifacts
resource "aws_s3_bucket" "artifacts" {
  count         = var.enable_artifacts_bucket ? 1 : 0
  bucket        = "charter-reporter-artifacts-${random_password.bucket_suffix.result}"
  force_destroy = false

  tags = {
    Name = "charter-reporter-artifacts"
  }
}

resource "random_password" "bucket_suffix" {
  length  = 8
  upper   = false
  special = false
}

resource "aws_s3_bucket_versioning" "artifacts" {
  versioning_configuration {
    status = "Enabled"
  }
}

resource "aws_s3_bucket_lifecycle_configuration" "artifacts" {

  rule {
    id     = "DeleteOldArtifacts"
    status = "Enabled"

    expiration {
      days = 90
    }

    noncurrent_version_expiration {
      noncurrent_days = 30
    }
  }
}

resource "aws_s3_bucket_public_access_block" "artifacts" {

  block_public_acls       = true
  block_public_policy     = true
  ignore_public_acls      = true
  restrict_public_buckets = true
}

# Get latest Amazon Linux 2023 AMI
data "aws_ami" "amazon_linux_2023" {
  most_recent = true
  owners      = ["amazon"]

  filter {
    name   = "name"
    values = ["al2023-ami-*-x86_64"]
  }

  filter {
    name   = "virtualization-type"
    values = ["hvm"]
  }
}

# User data script for initial setup
locals {
    aws_region          = var.aws_region
    domain_name         = var.domain_name
    admin_email         = var.admin_email
  }))

  user_data = var.minimal_deploy ? local.user_data_min : local.user_data_full
}

# EC2 Instance
resource "aws_instance" "charter_reporter" {
  ami                     = data.aws_ami.amazon_linux_2023.id
  instance_type           = var.instance_type
  key_name                = var.key_pair_name # Optional, for emergency access
  vpc_security_group_ids  = [aws_security_group.charter_reporter_web.id]
  subnet_id               = data.aws_subnet.existing_public.id
  iam_instance_profile    = aws_iam_instance_profile.charter_reporter_ec2.name
  user_data               = local.user_data
  
  root_block_device {
    volume_type = "gp3"
    volume_size = var.ebs_volume_size
    encrypted   = true
    tags = {
      Name = "charter-reporter-root-volume"
    }
  }

  tags = {
    Name = "charter-reporter-web"
    Type = "WebServer"
  }

  lifecycle {
    create_before_destroy = true
  }
}

# Associate Elastic IP with instance
resource "aws_eip_association" "charter_reporter" {
  instance_id   = aws_instance.charter_reporter.id
  allocation_id = aws_eip.charter_reporter.id
}

# Route 53 DNS records (optional - only if managing DNS in this account)
resource "aws_route53_zone" "main" {
  count = var.create_route53_zone ? 1 : 0
  name  = var.domain_name

  tags = {
    Name = "${var.domain_name}-zone"
  }
}

resource "aws_route53_record" "main" {
  count   = var.create_route53_zone ? 1 : 0
  zone_id = aws_route53_zone.main[0].zone_id
  name    = var.domain_name
  type    = "A"
  ttl     = 300
  records = [aws_eip.charter_reporter.public_ip]
}

resource "aws_route53_record" "www" {
  count   = var.create_route53_zone ? 1 : 0
  zone_id = aws_route53_zone.main[0].zone_id
  name    = "www.${var.domain_name}"
  type    = "A"
  ttl     = 300
  records = [aws_eip.charter_reporter.public_ip]
}



