resource "random_string" "suffix" {
  length  = 6
  special = false
  upper   = false
}

resource "azurerm_storage_account" "main" {
  name                     = "aiimgproc${var.environment}${random_string.suffix.result}"
  resource_group_name      = var.resource_group_name
  location                 = var.location
  account_tier             = var.account_tier
  account_replication_type = var.account_replication_type

  blob_properties {
    versioning_enabled = var.enable_blob_versioning
  }

  tags = var.tags
}

resource "azurerm_storage_container" "images" {
  name                  = "images"
  storage_account_id    = azurerm_storage_account.main.id
  container_access_type = "private"
}

# Only images container needed - processed/failed status handled via metadata

resource "azurerm_storage_container" "function_app" {
  name                  = "azure-webjobs-storage"
  storage_account_id    = azurerm_storage_account.main.id
  container_access_type = "private"
}
