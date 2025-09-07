# =============================================================================
# BOOTSTRAP INFRASTRUCTURE VARIABLES
# =============================================================================

variable "subscription_id" {
  description = "Azure subscription ID"
  type        = string
  validation {
    condition     = can(regex("^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$", var.subscription_id))
    error_message = "Subscription ID must be a valid UUID format."
  }
}

variable "project_name" {
  description = "Name of the project (used for resource naming)"
  type        = string
  validation {
    condition     = can(regex("^[a-z0-9-]+$", var.project_name))
    error_message = "Project name must contain only lowercase letters, numbers, and hyphens."
  }
}

variable "environment" {
  description = "Environment name (dev, staging, prod)"
  type        = string
  validation {
    condition     = contains(["dev", "staging", "prod"], var.environment)
    error_message = "Environment must be one of: dev, staging, prod."
  }
}

variable "location" {
  description = "Azure region for resources"
  type        = string
  validation {
    condition     = contains([
      "West Europe", "North Europe", "East US", "West US 2", 
      "Central US", "East US 2", "West US", "Southeast Asia"
    ], var.location)
    error_message = "Location must be a supported Azure region."
  }
}


# =============================================================================
# TAGS CONFIGURATION
# =============================================================================

variable "tags" {
  description = "Tags to apply to all resources"
  type        = map(string)
  default = {
    Project     = "AI Image Processor"
    Environment = "Bootstrap"
    ManagedBy   = "Terraform"
    Owner       = "DevOps Team"
    CostCenter  = "Infrastructure"
  }
}
