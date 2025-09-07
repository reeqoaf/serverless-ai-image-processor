terraform {
  backend "azurerm" {
    # Configure via -backend-config or environment variables
    # Example: terraform init -backend-config="resource_group_name=your-rg" -backend-config="storage_account_name=your-storage"
  }
}
