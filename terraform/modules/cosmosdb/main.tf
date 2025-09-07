resource "random_string" "suffix" {
  length  = 6
  special = false
  upper   = false
}

resource "azurerm_cosmosdb_account" "main" {
  name                = "cosmos${var.environment}${random_string.suffix.result}"
  location            = var.location
  resource_group_name = var.resource_group_name
  offer_type          = "Standard"
  kind                = "GlobalDocumentDB"

  consistency_policy {
    consistency_level       = "Session"
    max_interval_in_seconds = 5
    max_staleness_prefix    = 100
  }

  geo_location {
    location          = var.location
    failover_priority = 0
  }

  capabilities {
    name = "EnableServerless"
  }

  tags = var.tags
}

resource "azurerm_cosmosdb_sql_database" "main" {
  name                = "ImageAnalysis"
  resource_group_name = azurerm_cosmosdb_account.main.resource_group_name
  account_name        = azurerm_cosmosdb_account.main.name
}

resource "azurerm_cosmosdb_sql_container" "images" {
  name                = "Images"
  resource_group_name = azurerm_cosmosdb_account.main.resource_group_name
  account_name        = azurerm_cosmosdb_account.main.name
  database_name       = azurerm_cosmosdb_sql_database.main.name
  partition_key_paths = ["/partitionKey"]

  indexing_policy {
    indexing_mode = "consistent"

    included_path {
      path = "/*"
    }

    included_path {
      path = "/analysis/tags/?"
    }

    included_path {
      path = "/analysis/objects/name/?"
    }

    included_path {
      path = "/processedAt/?"
    }

    included_path {
      path = "/status/?"
    }

    excluded_path {
      path = "/analysis/rawData/?"
    }
  }

}
