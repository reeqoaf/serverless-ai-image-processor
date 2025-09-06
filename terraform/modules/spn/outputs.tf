output "spn_client_id" {
  description = "Service Principal Client ID"
  value       = azuread_application.main.client_id
}

output "spn_client_secret" {
  description = "Service Principal Client Secret"
  value       = azuread_service_principal_password.main.value
  sensitive   = true
}

output "spn_tenant_id" {
  description = "Service Principal Tenant ID"
  value       = azuread_service_principal.main.application_tenant_id
}

output "spn_object_id" {
  description = "Service Principal Object ID"
  value       = azuread_service_principal.main.object_id
}