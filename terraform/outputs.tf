output "resource_group_name" {
  description = "Name of the resource group"
  value       = azurerm_resource_group.main.name
}

output "storage_account_name" {
  description = "Name of the storage account"
  value       = module.storage.storage_account_name
}

output "storage_account_primary_connection_string" {
  description = "Primary connection string for the storage account"
  value       = module.storage.storage_account_primary_connection_string
  sensitive   = true
}

output "storage_account_primary_access_key" {
  description = "Primary access key for the storage account"
  value       = module.storage.storage_account_primary_access_key
  sensitive   = true
}

output "images_container_name" {
  description = "Name of the images container"
  value       = module.storage.images_container_name
}


output "function_app_name" {
  description = "Name of the Function App"
  value       = module.function_app.function_app_name
}

output "function_app_identity_principal_id" {
  description = "Principal ID of the Function App's managed identity"
  value       = module.function_app.function_app_identity_principal_id
}

output "computer_vision_endpoint" {
  description = "Computer Vision endpoint URL"
  value       = module.cognitive_services.computer_vision_endpoint
}

output "computer_vision_api_key" {
  description = "Computer Vision API key"
  value       = module.cognitive_services.computer_vision_api_key
  sensitive   = true
}

output "cosmosdb_connection_string" {
  description = "CosmosDB connection string"
  value       = module.cosmosdb.cosmosdb_connection_string
  sensitive   = true
}

# SPN outputs for Function App
output "spn_client_id" {
  description = "Service Principal Client ID"
  value       = module.spn.spn_client_id
}

output "spn_client_secret" {
  description = "Service Principal Client Secret"
  value       = module.spn.spn_client_secret
  sensitive   = true
}

output "spn_tenant_id" {
  description = "Service Principal Tenant ID"
  value       = module.spn.spn_tenant_id
}