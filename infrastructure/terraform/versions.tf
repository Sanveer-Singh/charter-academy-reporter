# Charter Reporter App - Terraform Version Constraints

terraform {
  required_version = ">= 1.6.0"
  
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
  
  # Optional: Configure remote state storage
  # Uncomment and configure for team environments
  # backend "s3" {
  #   bucket         = "your-terraform-state-bucket"
  #   key            = "charter-reporter/terraform.tfstate"
  #   region         = "af-south-1"
  #   encrypt        = true
  #   dynamodb_table = "terraform-locks"
  # }
}





