# Charter Reporter App - Parameter Store Configuration

# Admin user configuration
resource "aws_ssm_parameter" "admin_email" {
  name  = "/charter-reporter/admin/email"
  type  = "String"
  value = var.admin_email

  tags = {
    Name = "charter-reporter-admin-email"
  }
}

resource "aws_ssm_parameter" "admin_password" {
  name  = "/charter-reporter/admin/password"
  type  = "SecureString"
  value = random_password.admin_password.result

  tags = {
    Name = "charter-reporter-admin-password"
  }
}

# MariaDB Moodle connection parameters
resource "aws_ssm_parameter" "mariadb_moodle_host" {
  name  = "/charter-reporter/mariadb/moodle/host"
  type  = "String"
  value = var.mariadb_moodle_host

  tags = {
    Name = "charter-reporter-mariadb-moodle-host"
  }
}

resource "aws_ssm_parameter" "mariadb_moodle_database" {
  name  = "/charter-reporter/mariadb/moodle/database"
  type  = "String"
  value = var.mariadb_moodle_database

  tags = {
    Name = "charter-reporter-mariadb-moodle-database"
  }
}

resource "aws_ssm_parameter" "mariadb_moodle_username" {
  name  = "/charter-reporter/mariadb/moodle/username"
  type  = "String"
  value = var.mariadb_moodle_username

  tags = {
    Name = "charter-reporter-mariadb-moodle-username"
  }
}

resource "aws_ssm_parameter" "mariadb_moodle_password" {
  name  = "/charter-reporter/mariadb/moodle/password"
  type  = "SecureString"
  value = var.mariadb_moodle_password

  tags = {
    Name = "charter-reporter-mariadb-moodle-password"
  }
}

resource "aws_ssm_parameter" "mariadb_moodle_table_prefix" {
  name  = "/charter-reporter/mariadb/moodle/table_prefix"
  type  = "String"
  value = var.mariadb_moodle_table_prefix

  tags = {
    Name = "charter-reporter-mariadb-moodle-table-prefix"
  }
}

# MariaDB WooCommerce connection parameters
resource "aws_ssm_parameter" "mariadb_woo_host" {
  name  = "/charter-reporter/mariadb/woo/host"
  type  = "String"
  value = var.mariadb_woo_host

  tags = {
    Name = "charter-reporter-mariadb-woo-host"
  }
}

resource "aws_ssm_parameter" "mariadb_woo_database" {
  name  = "/charter-reporter/mariadb/woo/database"
  type  = "String"
  value = var.mariadb_woo_database

  tags = {
    Name = "charter-reporter-mariadb-woo-database"
  }
}

resource "aws_ssm_parameter" "mariadb_woo_username" {
  name  = "/charter-reporter/mariadb/woo/username"
  type  = "String"
  value = var.mariadb_woo_username

  tags = {
    Name = "charter-reporter-mariadb-woo-username"
  }
}

resource "aws_ssm_parameter" "mariadb_woo_password" {
  name  = "/charter-reporter/mariadb/woo/password"
  type  = "SecureString"
  value = var.mariadb_woo_password

  tags = {
    Name = "charter-reporter-mariadb-woo-password"
  }
}

resource "aws_ssm_parameter" "mariadb_woo_table_prefix" {
  name  = "/charter-reporter/mariadb/woo/table_prefix"
  type  = "String"
  value = var.mariadb_woo_table_prefix

  tags = {
    Name = "charter-reporter-mariadb-woo-table-prefix"
  }
}

# SMTP configuration (optional)
resource "aws_ssm_parameter" "smtp_host" {
  count = var.smtp_host != "" ? 1 : 0
  name  = "/charter-reporter/email/smtp/host"
  type  = "String"
  value = var.smtp_host

  tags = {
    Name = "charter-reporter-smtp-host"
  }
}

resource "aws_ssm_parameter" "smtp_port" {
  count = var.smtp_host != "" ? 1 : 0
  name  = "/charter-reporter/email/smtp/port"
  type  = "String"
  value = tostring(var.smtp_port)

  tags = {
    Name = "charter-reporter-smtp-port"
  }
}

resource "aws_ssm_parameter" "smtp_username" {
  count = var.smtp_username != "" ? 1 : 0
  name  = "/charter-reporter/email/smtp/username"
  type  = "String"
  value = var.smtp_username

  tags = {
    Name = "charter-reporter-smtp-username"
  }
}

resource "aws_ssm_parameter" "smtp_password" {
  count = var.smtp_password != "" ? 1 : 0
  name  = "/charter-reporter/email/smtp/password"
  type  = "SecureString"
  value = var.smtp_password

  tags = {
    Name = "charter-reporter-smtp-password"
  }
}

# Application configuration parameters
resource "aws_ssm_parameter" "app_environment" {
  name  = "/charter-reporter/app/environment"
  type  = "String"
  value = var.environment

  tags = {
    Name = "charter-reporter-app-environment"
  }
}

resource "aws_ssm_parameter" "app_domain" {
  name  = "/charter-reporter/app/domain"
  type  = "String"
  value = var.domain_name

  tags = {
    Name = "charter-reporter-app-domain"
  }
}

# Backup configuration
resource "aws_ssm_parameter" "backup_vault_name" {
  name  = "/charter-reporter/backup/vault_name"
  type  = "String"
  value = aws_backup_vault.charter_reporter.name

  tags = {
    Name = "charter-reporter-backup-vault-name"
  }
}

# S3 artifacts bucket
resource "aws_ssm_parameter" "artifacts_bucket" {
  name  = "/charter-reporter/deployment/artifacts_bucket"
  type  = "String"
  value = aws_s3_bucket.artifacts.bucket

  tags = {
    Name = "charter-reporter-artifacts-bucket"
  }
}



