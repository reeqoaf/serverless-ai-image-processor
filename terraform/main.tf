terraform {
  required_version = ">= 1.10"
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = ">= 4.0"
    }
    azuread = {
      source  = "hashicorp/azuread"
      version = ">= 3.5"
    }
    random = {
      source  = "hashicorp/random"
      version = ">= 3.7"
    }
  }
}

provider "azurerm" {
  subscription_id = var.subscription_id
  features {
    resource_group {
      prevent_deletion_if_contains_resources = false
    }
  }
  
  # Use Service Principal authentication when credentials are provided
  # This allows the same configuration to work both locally and in GitHub Actions
  client_id       = var.spn_client_id != "" ? var.spn_client_id : null
  client_secret   = var.spn_client_secret != "" ? var.spn_client_secret : null
  tenant_id       = var.spn_tenant_id != "" ? var.spn_tenant_id : null
}

provider "azuread" {
  tenant_id = data.azurerm_client_config.current.tenant_id
}

data "azurerm_client_config" "current" {}

resource "azurerm_resource_group" "main" {
  name     = "${var.project_name}-${var.environment}-rg"
  location = var.location

  tags = var.tags
}

module "storage" {
  source = "./modules/storage"

  project_name                = var.project_name
  environment                 = var.environment
  resource_group_name         = azurerm_resource_group.main.name
  location                    = azurerm_resource_group.main.location
  account_tier                = var.storage_config.tier
  account_replication_type    = var.storage_config.replication_type
  enable_blob_versioning      = var.storage_config.enable_versioning

  tags = var.tags
}


module "cosmosdb" {
  source = "./modules/cosmosdb"

  project_name        = var.project_name
  environment         = var.environment
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location

  tags = var.tags
}

module "cognitive_services" {
  source = "./modules/cognitive_services"

  project_name        = var.project_name
  environment         = var.environment
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  sku_name            = var.cognitive_services_config.sku_name

  tags = var.tags

  # Ensure the service is created before the function app
  depends_on = [azurerm_resource_group.main]
}

module "spn" {
  source = "./modules/spn"

  project_name        = var.project_name
  environment         = var.environment
  storage_account_id  = module.storage.storage_account_id
  cosmosdb_account_id = module.cosmosdb.cosmosdb_account_id
  cognitive_services_id = module.cognitive_services.cognitive_services_id

  depends_on = [module.storage, module.cosmosdb, module.cognitive_services]
}

module "function_app" {
  source = "./modules/function_app"

  project_name                = var.project_name
  environment                 = var.environment
  resource_group_name         = azurerm_resource_group.main.name
  location                    = azurerm_resource_group.main.location
  storage_account_name        = module.storage.storage_account_name
  storage_connection_string   = module.storage.storage_account_primary_connection_string
  sku_tier                    = var.function_app_config.sku_tier
  sku_size                    = var.function_app_config.sku_size
  cosmosdb_connection_string  = module.cosmosdb.cosmosdb_connection_string
  computer_vision_endpoint    = module.cognitive_services.computer_vision_endpoint
  computer_vision_api_key     = module.cognitive_services.computer_vision_api_key
  max_retries                 = var.max_retries
  
  # SPN credentials from Terraform-created SPN
  spn_client_id               = module.spn.spn_client_id
  spn_client_secret           = module.spn.spn_client_secret
  spn_tenant_id               = module.spn.spn_tenant_id

  depends_on = [module.storage, module.cosmosdb, module.cognitive_services, module.spn]

  tags = var.tags
}