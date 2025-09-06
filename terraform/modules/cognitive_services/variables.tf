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

variable "sku_name" {
  description = "SKU name for the Cognitive Services account"
  type        = string
  default     = "S0"
  validation {
    condition     = contains(["F0", "S0", "S1", "S2", "S3", "S4", "S5", "S6"], var.sku_name)
    error_message = "SKU name must be one of: F0, S0, S1, S2, S3, S4, S5, S6."
  }
}

variable "tags" {
  description = "Tags to apply to all resources"
  type        = map(string)
  default     = {}
}
