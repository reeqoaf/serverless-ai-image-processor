resource "azurerm_cognitive_account" "computer_vision" {
  name                = "${var.project_name}-${var.environment}-cv-${random_string.suffix.result}"
  location            = var.location
  resource_group_name = var.resource_group_name
  kind                = "ComputerVision"
  sku_name            = var.sku_name

  # Ensure proper configuration
  public_network_access_enabled = true
  local_auth_enabled           = true

  tags = var.tags

  # Prevent accidental deletion in production
  lifecycle {
    prevent_destroy = false
  }
}

resource "random_string" "suffix" {
  length  = 6
  special = false
  upper   = false
}
