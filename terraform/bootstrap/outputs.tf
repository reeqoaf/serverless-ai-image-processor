# =============================================================================
# BOOTSTRAP INFRASTRUCTURE OUTPUTS
# =============================================================================

# Resource Group
output "bootstrap_resource_group_name" {
  description = "Name of the bootstrap resource group"
  value       = azurerm_resource_group.bootstrap.name
}

output "bootstrap_resource_group_id" {
  description = "ID of the bootstrap resource group"
  value       = azurerm_resource_group.bootstrap.id
}

# =============================================================================
# STORAGE ACCOUNT OUTPUTS
# =============================================================================

output "terraform_state_storage_account_name" {
  description = "Name of the storage account for Terraform state"
  value       = azurerm_storage_account.terraform_state.name
}

output "terraform_state_storage_account_id" {
  description = "ID of the storage account for Terraform state"
  value       = azurerm_storage_account.terraform_state.id
}

output "terraform_state_container_name" {
  description = "Name of the container for Terraform state"
  value       = azurerm_storage_container.terraform_state.name
}

output "terraform_state_storage_connection_string" {
  description = "Connection string for the Terraform state storage account"
  value       = azurerm_storage_account.terraform_state.primary_connection_string
  sensitive   = true
}

output "terraform_state_storage_access_key" {
  description = "Primary access key for the Terraform state storage account"
  value       = azurerm_storage_account.terraform_state.primary_access_key
  sensitive   = true
}

# =============================================================================
# SERVICE PRINCIPAL OUTPUTS
# =============================================================================

output "github_actions_spn_client_id" {
  description = "Client ID of the GitHub Actions service principal"
  value       = azuread_service_principal.github_actions.client_id
}

output "github_actions_spn_client_secret" {
  description = "Client secret of the GitHub Actions service principal"
  value       = azuread_service_principal_password.github_actions.value
  sensitive   = true
}

output "github_actions_spn_tenant_id" {
  description = "Tenant ID of the GitHub Actions service principal"
  value       = data.azurerm_client_config.current.tenant_id
}

output "github_actions_spn_object_id" {
  description = "Object ID of the GitHub Actions service principal"
  value       = azuread_service_principal.github_actions.object_id
}


# =============================================================================
# TERRAFORM BACKEND CONFIGURATION
# =============================================================================

output "terraform_backend_config" {
  description = "Terraform backend configuration for remote state"
  value = {
    resource_group_name  = azurerm_resource_group.bootstrap.name
    storage_account_name = azurerm_storage_account.terraform_state.name
    container_name       = azurerm_storage_container.terraform_state.name
    key                  = "terraform.tfstate"
  }
}

# =============================================================================
# GITHUB ACTIONS SECRETS
# =============================================================================

output "github_actions_secrets" {
  description = "Secrets to be added to GitHub Actions"
  value = {
    AZURE_CLIENT_ID       = azuread_service_principal.github_actions.client_id
    AZURE_CLIENT_SECRET   = azuread_service_principal_password.github_actions.value
    AZURE_TENANT_ID       = data.azurerm_client_config.current.tenant_id
    AZURE_SUBSCRIPTION_ID = var.subscription_id
  }
  sensitive = true
}
