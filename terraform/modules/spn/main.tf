resource "azuread_application" "main" {
  display_name = "${var.project_name}-${var.environment}-spn"
  
  required_resource_access {
    resource_app_id = "00000003-0000-0000-c000-000000000000" # Microsoft Graph
    
    resource_access {
      id   = "e1fe6dd8-ba31-4d61-89e7-88639da4683d" # User.Read
      type = "Scope"
    }
  }
}

resource "azuread_service_principal" "main" {
  client_id = azuread_application.main.client_id
}

resource "azuread_service_principal_password" "main" {
  service_principal_id = azuread_service_principal.main.id
}

# Grant SPN access to Storage Account
resource "azurerm_role_assignment" "spn_storage_blob_data_contributor" {
  scope                = var.storage_account_id
  role_definition_name = "Storage Blob Data Contributor"
  principal_id         = azuread_service_principal.main.object_id
}

# Note: CosmosDB will use connection string instead of RBAC for simplicity

# Grant SPN access to Computer Vision
resource "azurerm_role_assignment" "spn_cognitive_services_user" {
  scope                = var.cognitive_services_id
  role_definition_name = "Cognitive Services User"
  principal_id         = azuread_service_principal.main.object_id
}