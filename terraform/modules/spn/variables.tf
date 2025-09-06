variable "project_name" {
  description = "Name of the project"
  type        = string
}

variable "environment" {
  description = "Environment name (dev, staging, prod)"
  type        = string
}

variable "storage_account_id" {
  description = "Resource ID of the storage account"
  type        = string
}

variable "cosmosdb_account_id" {
  description = "Resource ID of the CosmosDB account"
  type        = string
}

variable "cognitive_services_id" {
  description = "Resource ID of the Cognitive Services account"
  type        = string
}