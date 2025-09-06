resource "random_string" "suffix" {
  length  = 6
  special = false
  upper   = false
}

resource "azurerm_service_plan" "main" {
  name                = "${var.project_name}-${var.environment}-plan-${random_string.suffix.result}"
  resource_group_name = var.resource_group_name
  location            = var.location
  os_type             = "Windows"
  sku_name            = var.sku_tier == "Consumption" ? "Y1" : "${var.sku_tier}_${var.sku_size}"

  tags = var.tags
}

resource "azurerm_windows_function_app" "main" {
  name                = "${var.project_name}-${var.environment}-func-${random_string.suffix.result}"
  resource_group_name = var.resource_group_name
  location            = var.location

  storage_account_name       = var.storage_account_name
  service_plan_id            = azurerm_service_plan.main.id

  site_config {
    application_stack {
      dotnet_version = "v9.0"
    }
  }

  app_settings = {
    "FUNCTIONS_WORKER_RUNTIME" = "dotnet-isolated"
    "WEBSITE_RUN_FROM_PACKAGE" = "1"
    "AzureWebJobsStorage" = var.storage_connection_string
    "COMPUTER_VISION_ENDPOINT" = var.computer_vision_endpoint
    "COMPUTER_VISION_API_KEY" = var.computer_vision_api_key
    "COSMOSDB_CONNECTION_STRING" = var.cosmosdb_connection_string
    "STORAGE_ACCOUNT_NAME" = var.storage_account_name
    "MAX_RETRIES" = var.max_retries
    "AZURE_CLIENT_ID" = var.spn_client_id
    "AZURE_CLIENT_SECRET" = var.spn_client_secret
    "AZURE_TENANT_ID" = var.spn_tenant_id
  }

  identity {
    type = "SystemAssigned"
  }

  tags = var.tags
}


