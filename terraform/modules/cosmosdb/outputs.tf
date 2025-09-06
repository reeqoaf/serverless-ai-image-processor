output "cosmosdb_account_name" {
  description = "Name of the CosmosDB account"
  value       = azurerm_cosmosdb_account.main.name
}

output "cosmosdb_account_id" {
  description = "ID of the CosmosDB account"
  value       = azurerm_cosmosdb_account.main.id
}

output "cosmosdb_endpoint" {
  description = "Endpoint of the CosmosDB account"
  value       = azurerm_cosmosdb_account.main.endpoint
}

output "cosmosdb_primary_key" {
  description = "Primary key of the CosmosDB account"
  value       = azurerm_cosmosdb_account.main.primary_key
  sensitive   = true
}

output "cosmosdb_connection_string" {
  description = "Connection string for the CosmosDB account"
  value       = "AccountEndpoint=${azurerm_cosmosdb_account.main.endpoint};AccountKey=${azurerm_cosmosdb_account.main.primary_key};"
  sensitive   = true
}

output "database_name" {
  description = "Name of the SQL database"
  value       = azurerm_cosmosdb_sql_database.main.name
}

output "container_name" {
  description = "Name of the SQL container"
  value       = azurerm_cosmosdb_sql_container.images.name
}
