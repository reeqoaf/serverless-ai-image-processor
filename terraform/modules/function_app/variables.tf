variable "project_name" {
  description = "Name of the project"
  type        = string
}

variable "environment" {
  description = "Environment name (dev)"
  type        = string
}

variable "resource_group_name" {
  description = "Name of the resource group"
  type        = string
}

variable "location" {
  description = "Azure region for resources"
  type        = string
}

variable "storage_account_name" {
  description = "Name of the storage account"
  type        = string
}

variable "storage_connection_string" {
  description = "Storage account connection string for Function App secrets"
  type        = string
  sensitive   = true
}

variable "storage_account_access_key" {
  description = "Storage account access key for Function App storage"
  type        = string
  sensitive   = true
}

variable "sku_tier" {
  description = "SKU tier for the Function App"
  type        = string
  default     = "Consumption"
}

variable "sku_size" {
  description = "SKU size for the Function App"
  type        = string
  default     = "Y1"
}

variable "computer_vision_endpoint" {
  description = "Computer Vision endpoint URL"
  type        = string
}

variable "computer_vision_api_key" {
  description = "Computer Vision API key"
  type        = string
  sensitive   = true
}

variable "max_retries" {
  description = "Maximum number of retries for failed operations"
  type        = number
  default     = 3
}

variable "cosmosdb_connection_string" {
  description = "CosmosDB connection string"
  type        = string
  sensitive   = true
}


variable "spn_client_id" {
  description = "Service Principal Client ID"
  type        = string
}

variable "spn_client_secret" {
  description = "Service Principal Client Secret"
  type        = string
  sensitive   = true
}

variable "spn_tenant_id" {
  description = "Service Principal Tenant ID"
  type        = string
}

variable "tags" {
  description = "Tags to apply to all resources"
  type        = map(string)
  default     = {}
}
