# =============================================================================
# CORE INFRASTRUCTURE VARIABLES
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
  description = "Environment name (dev)"
  type        = string
  validation {
    condition     = contains(["dev"], var.environment)
    error_message = "Environment must be: dev."
  }
}

variable "location" {
  description = "Azure region for resources"
  type        = string
  validation {
    condition = contains([
      "West Europe", "North Europe", "East US", "West US 2",
      "Central US", "East US 2", "West US", "Southeast Asia"
    ], var.location)
    error_message = "Location must be a supported Azure region."
  }
}

# =============================================================================
# STORAGE CONFIGURATION
# =============================================================================

variable "storage_config" {
  description = "Storage account configuration"
  type = object({
    tier              = string
    replication_type  = string
    enable_versioning = bool
  })
  validation {
    condition     = contains(["Standard", "Premium"], var.storage_config.tier)
    error_message = "Storage tier must be either Standard or Premium."
  }
  validation {
    condition     = contains(["LRS", "GRS", "RAGRS", "ZRS"], var.storage_config.replication_type)
    error_message = "Replication type must be one of: LRS, GRS, RAGRS, ZRS."
  }
}

# =============================================================================
# COGNITIVE SERVICES CONFIGURATION
# =============================================================================

variable "cognitive_services_config" {
  description = "Cognitive Services configuration"
  type = object({
    sku_name = string
  })
  validation {
    condition     = contains(["F0", "S0", "S1", "S2", "S3", "S4", "S5", "S6"], var.cognitive_services_config.sku_name)
    error_message = "Cognitive Services SKU must be one of: F0, S0, S1, S2, S3, S4, S5, S6."
  }
}

# =============================================================================
# FUNCTION APP CONFIGURATION
# =============================================================================

variable "function_app_config" {
  description = "Function App configuration"
  type = object({
    sku_tier = string
    sku_size = string
  })
  validation {
    condition     = contains(["Consumption", "Basic", "Standard", "Premium"], var.function_app_config.sku_tier)
    error_message = "SKU tier must be one of: Consumption, Basic, Standard, Premium."
  }
  validation {
    condition     = (var.function_app_config.sku_tier == "Consumption" && var.function_app_config.sku_size == "Y1") || (var.function_app_config.sku_tier != "Consumption" && can(regex("^[A-Z][0-9]$", var.function_app_config.sku_size)))
    error_message = "For Consumption tier, sku_size must be Y1. For other tiers, use format like B1, S1, etc."
  }
}


# =============================================================================
# APPLICATION CONFIGURATION
# =============================================================================

variable "max_retries" {
  description = "Maximum number of retries for failed operations"
  type        = number
  validation {
    condition     = var.max_retries >= 1 && var.max_retries <= 10
    error_message = "Max retries must be between 1 and 10."
  }
}


# =============================================================================
# SERVICE PRINCIPAL CONFIGURATION (FROM BOOTSTRAP)
# =============================================================================

variable "spn_client_id" {
  description = "Service Principal Client ID from bootstrap"
  type        = string
  default     = ""
}

variable "spn_client_secret" {
  description = "Service Principal Client Secret from bootstrap"
  type        = string
  default     = ""
  sensitive   = true
}

variable "spn_tenant_id" {
  description = "Service Principal Tenant ID from bootstrap"
  type        = string
  default     = ""
}

# =============================================================================
# BACKEND CONFIGURATION
# =============================================================================

variable "backend_config" {
  description = "Backend configuration for remote state storage"
  type = object({
    resource_group_name  = string
    storage_account_name = string
    container_name       = string
    key                  = string
  })
  default = {
    resource_group_name  = ""
    storage_account_name = ""
    container_name       = ""
    key                  = ""
  }
}

# =============================================================================
# TAGS CONFIGURATION
# =============================================================================

variable "tags" {
  description = "Tags to apply to all resources"
  type        = map(string)
}