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
}

provider "azuread" {
  tenant_id = data.azurerm_client_config.current.tenant_id
}

data "azurerm_client_config" "current" {}

# =============================================================================
# BOOTSTRAP RESOURCE GROUP
# =============================================================================

resource "azurerm_resource_group" "bootstrap" {
  name     = "${var.project_name}-bootstrap-rg"
  location = var.location

  tags = merge(var.tags, {
    Purpose = "Bootstrap Infrastructure"
    Type    = "Bootstrap"
  })
}

# =============================================================================
# STORAGE ACCOUNT FOR TERRAFORM STATE
# =============================================================================

resource "random_string" "storage_suffix" {
  length  = 8
  special = false
  upper   = false
}

resource "azurerm_storage_account" "terraform_state" {
  name                     = "aiimgproctf${random_string.storage_suffix.result}"
  resource_group_name      = azurerm_resource_group.bootstrap.name
  location                 = azurerm_resource_group.bootstrap.location
  account_tier             = "Standard"
  account_replication_type = "LRS"

  # Enable versioning for state file protection
  blob_properties {
    versioning_enabled = true
  }

  tags = merge(var.tags, {
    Purpose = "Terraform State Storage"
    Type    = "Bootstrap"
  })
}

resource "azurerm_storage_container" "terraform_state" {
  name                  = "tfstate"
  storage_account_name  = azurerm_storage_account.terraform_state.name
  container_access_type = "private"
}

# =============================================================================
# SERVICE PRINCIPAL FOR GITHUB ACTIONS
# =============================================================================

resource "azuread_application" "github_actions" {
  display_name = "${var.project_name}-github-actions"

  tags = ["terraform", "github-actions", var.environment]
}

resource "azuread_service_principal" "github_actions" {
  client_id = azuread_application.github_actions.client_id

  tags = ["terraform", "github-actions", var.environment]
}

resource "azuread_service_principal_password" "github_actions" {
  service_principal_id = azuread_service_principal.github_actions.id
  display_name         = "GitHub Actions Secret"

  # Set expiration to 2 years from now
  end_date = timeadd(timestamp(), "17520h")
}

# =============================================================================
# ROLE ASSIGNMENTS FOR SERVICE PRINCIPAL
# =============================================================================

# Contributor role for the entire subscription (for resource management)
resource "azurerm_role_assignment" "github_actions_contributor" {
  scope                = "/subscriptions/${var.subscription_id}"
  role_definition_name = "Contributor"
  principal_id         = azuread_service_principal.github_actions.object_id
}

# User Access Administrator role for RBAC assignments
# Required if the workflow must assign roles (e.g., giving Function access to Cosmos DB)
resource "azurerm_role_assignment" "github_actions_user_access_admin" {
  scope                = "/subscriptions/${var.subscription_id}"
  role_definition_name = "User Access Administrator"
  principal_id         = azuread_service_principal.github_actions.object_id
}

# Storage Blob Data Contributor for state management
resource "azurerm_role_assignment" "github_actions_storage" {
  scope                = azurerm_storage_account.terraform_state.id
  role_definition_name = "Storage Blob Data Contributor"
  principal_id         = azuread_service_principal.github_actions.object_id
}

