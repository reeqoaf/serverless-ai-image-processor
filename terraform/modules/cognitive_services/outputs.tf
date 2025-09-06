output "computer_vision_endpoint" {
  description = "Computer Vision endpoint URL"
  value       = azurerm_cognitive_account.computer_vision.endpoint
}

output "computer_vision_api_key" {
  description = "Computer Vision API key"
  value       = azurerm_cognitive_account.computer_vision.primary_access_key
  sensitive   = true
}

output "computer_vision_secondary_key" {
  description = "Computer Vision secondary API key"
  value       = azurerm_cognitive_account.computer_vision.secondary_access_key
  sensitive   = true
}

output "computer_vision_id" {
  description = "Computer Vision resource ID"
  value       = azurerm_cognitive_account.computer_vision.id
}

output "cognitive_services_id" {
  description = "Cognitive Services account ID"
  value       = azurerm_cognitive_account.computer_vision.id
}
