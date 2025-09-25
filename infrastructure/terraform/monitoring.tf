# Charter Reporter App - Monitoring and Backup Configuration

# SNS Topic for alerts
resource "aws_sns_topic" "charter_reporter_alerts" {
  count = var.enable_monitoring ? 1 : 0
  name = "charter-reporter-alerts"

  tags = {
    Name = "charter-reporter-alerts"
  }
}

resource "aws_sns_topic_subscription" "email_alerts" {
  count     = var.enable_monitoring && var.alert_email != "" ? 1 : 0
  topic_arn = aws_sns_topic.charter_reporter_alerts[0].arn
  protocol  = "email"
  endpoint  = var.alert_email
}

# CloudWatch Log Groups
resource "aws_cloudwatch_log_group" "app_logs" {
  count             = var.enable_monitoring ? 1 : 0
  name              = "/charter-reporter/app"
  retention_in_days = var.log_retention_days

  tags = {
    Name = "charter-reporter-app-logs"
  }
}

resource "aws_cloudwatch_log_group" "nginx_access" {
  count             = var.enable_monitoring ? 1 : 0
  name              = "/charter-reporter/nginx/access"
  retention_in_days = 14

  tags = {
    Name = "charter-reporter-nginx-access"
  }
}

resource "aws_cloudwatch_log_group" "nginx_error" {
  count             = var.enable_monitoring ? 1 : 0
  name              = "/charter-reporter/nginx/error"
  retention_in_days = var.log_retention_days

  tags = {
    Name = "charter-reporter-nginx-error"
  }
}

resource "aws_cloudwatch_log_group" "system_logs" {
  count             = var.enable_monitoring ? 1 : 0
  name              = "/charter-reporter/system"
  retention_in_days = 7

  tags = {
    Name = "charter-reporter-system-logs"
  }
}

# CloudWatch Alarms
resource "aws_cloudwatch_metric_alarm" "high_cpu" {
  count               = var.enable_monitoring ? 1 : 0
  alarm_name          = "Charter-Reporter-HighCPU"
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = "2"
  metric_name         = "CPUUtilization"
  namespace           = "AWS/EC2"
  period              = "300"
  statistic           = "Average"
  threshold           = "80"
  alarm_description   = "This metric monitors ec2 cpu utilization"
  alarm_actions       = [aws_sns_topic.charter_reporter_alerts[0].arn]

  dimensions = {
    InstanceId = aws_instance.charter_reporter.id
  }

  tags = {
    Name = "Charter-Reporter-HighCPU"
  }
}

resource "aws_cloudwatch_metric_alarm" "high_memory" {
  count               = var.enable_monitoring ? 1 : 0
  alarm_name          = "Charter-Reporter-HighMemory"
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = "1"
  metric_name         = "mem_used_percent"
  namespace           = "CWAgent"
  period              = "300"
  statistic           = "Average"
  threshold           = "85"
  alarm_description   = "This metric monitors memory utilization"
  alarm_actions       = [aws_sns_topic.charter_reporter_alerts[0].arn]

  dimensions = {
    InstanceId = aws_instance.charter_reporter.id
  }

  tags = {
    Name = "Charter-Reporter-HighMemory"
  }
}

resource "aws_cloudwatch_metric_alarm" "disk_space" {
  count               = var.enable_monitoring ? 1 : 0
  alarm_name          = "Charter-Reporter-DiskSpace"
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = "1"
  metric_name         = "used_percent"
  namespace           = "CWAgent"
  period              = "300"
  statistic           = "Maximum"
  threshold           = "85"
  alarm_description   = "This metric monitors disk space utilization"
  alarm_actions       = [aws_sns_topic.charter_reporter_alerts[0].arn]

  dimensions = {
    InstanceId = aws_instance.charter_reporter.id
    device     = "xvda1"
    fstype     = "xfs"
    path       = "/"
  }

  tags = {
    Name = "Charter-Reporter-DiskSpace"
  }
}

resource "aws_cloudwatch_metric_alarm" "instance_status_check" {
  count               = var.enable_monitoring ? 1 : 0
  alarm_name          = "Charter-Reporter-InstanceStatusCheck"
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = "2"
  metric_name         = "StatusCheckFailed"
  namespace           = "AWS/EC2"
  period              = "300"
  statistic           = "Maximum"
  threshold           = "0"
  alarm_description   = "This metric monitors instance status check"
  alarm_actions       = [aws_sns_topic.charter_reporter_alerts[0].arn]

  dimensions = {
    InstanceId = aws_instance.charter_reporter.id
  }

  tags = {
    Name = "Charter-Reporter-InstanceStatusCheck"
  }
}

# AWS Backup Configuration
resource "aws_backup_vault" "charter_reporter" {
  count       = var.enable_monitoring ? 1 : 0
  name        = "charter-reporter-vault"
  kms_key_arn = aws_kms_key.backup[0].arn

  tags = {
    Name = "charter-reporter-backup-vault"
  }
}

resource "aws_kms_key" "backup" {
  count       = var.enable_monitoring ? 1 : 0
  description = "KMS key for Charter Reporter backups"
  
  tags = {
    Name = "charter-reporter-backup-key"
  }
}

resource "aws_kms_alias" "backup" {
  count        = var.enable_monitoring ? 1 : 0
  name          = "alias/charter-reporter-backup"
  target_key_id = aws_kms_key.backup[0].key_id
}

# IAM role for AWS Backup
resource "aws_iam_role" "backup" {
  count = var.enable_monitoring ? 1 : 0
  name = "AWSBackupCharterReporterRole"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Action = "sts:AssumeRole"
        Effect = "Allow"
        Principal = {
          Service = "backup.amazonaws.com"
        }
      }
    ]
  })

  tags = {
    Name = "AWSBackupCharterReporterRole"
  }
}

resource "aws_iam_role_policy_attachment" "backup_policy" {
  count      = var.enable_monitoring ? 1 : 0
  role       = aws_iam_role.backup[0].name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AWSBackupServiceRolePolicyForBackup"
}

# Backup Plan
resource "aws_backup_plan" "charter_reporter" {
  count = var.enable_monitoring ? 1 : 0
  name = "charter-reporter-backup-plan"

  rule {
    rule_name         = "daily_backups"
    target_vault_name = aws_backup_vault.charter_reporter[0].name
    schedule          = "cron(0 2 ? * * *)" # 2 AM CAT (0 AM UTC)

    lifecycle {
      cold_storage_after = 30
      delete_after       = var.backup_retention_days
    }

    recovery_point_tags = {
      Project = "Charter-Reporter"
      Type    = "DailyBackup"
    }
  }

  tags = {
    Name = "charter-reporter-backup-plan"
  }
}

# Backup Selection
resource "aws_backup_selection" "charter_reporter" {
  count        = var.enable_monitoring ? 1 : 0
  iam_role_arn = aws_iam_role.backup[0].arn
  name         = "charter-reporter-backup-selection"
  plan_id      = aws_backup_plan.charter_reporter[0].id

  resources = [
    aws_instance.charter_reporter.arn
  ]

  condition {
    string_equals {
      key   = "aws:ResourceTag/Project"
      value = "Charter-Reporter"
    }
  }
}

# CloudWatch Dashboard
resource "aws_cloudwatch_dashboard" "charter_reporter" {
  count          = var.enable_monitoring ? 1 : 0
  dashboard_name = "Charter-Reporter-Dashboard"

  dashboard_body = jsonencode({
    widgets = [
      {
        type   = "metric"
        x      = 0
        y      = 0
        width  = 12
        height = 6

        properties = {
          metrics = [
            ["AWS/EC2", "CPUUtilization", "InstanceId", aws_instance.charter_reporter.id],
            ["CWAgent", "mem_used_percent", "InstanceId", aws_instance.charter_reporter.id],
            ["CWAgent", "used_percent", "InstanceId", aws_instance.charter_reporter.id, "device", "xvda1", "fstype", "xfs", "path", "/"]
          ]
          view    = "timeSeries"
          stacked = false
          region  = var.aws_region
          title   = "System Resources"
          period  = 300
        }
      },
      {
        type   = "log"
        x      = 0
        y      = 6
        width  = 24
        height = 6

        properties = {
          query   = "SOURCE '/charter-reporter/app'\n| fields @timestamp, @message\n| filter @message like /ERROR/\n| sort @timestamp desc\n| limit 50"
          region  = var.aws_region
          title   = "Application Errors"
        }
      }
    ]
  })
}

# EventBridge rule for instance state changes
resource "aws_cloudwatch_event_rule" "instance_state_change" {
  count       = var.enable_monitoring ? 1 : 0
  name        = "charter-reporter-instance-state-change"
  description = "Capture Charter Reporter instance state changes"

  event_pattern = jsonencode({
    source        = ["aws.ec2"]
    detail-type   = ["EC2 Instance State-change Notification"]
    detail = {
      instance-id = [aws_instance.charter_reporter.id]
    }
  })

  tags = {
    Name = "charter-reporter-instance-events"
  }
}

resource "aws_cloudwatch_event_target" "sns" {
  count     = var.enable_monitoring ? 1 : 0
  rule      = aws_cloudwatch_event_rule.instance_state_change[0].name
  target_id = "SendToSNS"
  arn       = aws_sns_topic.charter_reporter_alerts[0].arn
}

# Allow EventBridge to publish to SNS
resource "aws_sns_topic_policy" "charter_reporter_alerts" {
  count = var.enable_monitoring ? 1 : 0
  arn   = aws_sns_topic.charter_reporter_alerts[0].arn

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Principal = {
          Service = "events.amazonaws.com"
        }
        Action   = "SNS:Publish"
        Resource = aws_sns_topic.charter_reporter_alerts[0].arn
      }
    ]
  })
}





