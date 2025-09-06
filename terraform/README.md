# Terraform Infrastructure

Infrastructure as Code for the AI Image Processor using Azure services.

## Quick Deploy

```bash
# Development
terraform init
terraform plan -var-file="envs/dev/terraform.tfvars"
terraform apply -var-file="envs/dev/terraform.tfvars"

# Destroy
terraform destroy -var-file="envs/dev/terraform.tfvars"
```

## Resources Created

- **Resource Group** - Container for all resources
- **Storage Account** - Blob storage for images  
- **CosmosDB** - NoSQL database (serverless)
- **Computer Vision** - AI service for image analysis
- **Function App** - Serverless compute (.NET 9)
- **Service Principal** - Authentication for Function App

## Configuration

### Environment Files
- `envs/dev/terraform.tfvars` - Development (only supported environment)

### Key Variables
```hcl
project_name = "ai-image-processor"
environment = "dev"
location = "West Europe"
subscription_id = "your-subscription-id"
```

## Cost Estimates

| Environment | Monthly Cost | Notes |
|-------------|--------------|-------|
| **Development** | $0-5 | Free tiers, minimal usage |
| **Additional** | Variable | Based on configuration |

## Security

- ✅ No hardcoded secrets
- ✅ Service Principal authentication
- ✅ RBAC permissions
- ✅ Environment isolation

## Troubleshooting

```bash
# Validate configuration
terraform validate

# Check outputs
terraform output

# Azure login
az login
az account set --subscription "your-subscription-id"
```