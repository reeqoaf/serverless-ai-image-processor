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
- `envs/dev/terraform.tfvars` - Development configuration

### Key Variables
```hcl
project_name = "ai-image-processor"
environment = "dev"
location = "West Europe"
subscription_id = "your-subscription-id"
```

## Module Structure

```
terraform/
├── modules/
│   ├── storage/           # Storage account + containers
│   ├── cosmosdb/          # CosmosDB database
│   ├── cognitive_services/ # Computer Vision API
│   ├── function_app/      # Function App + service plan
│   └── spn/              # Service Principal
├── envs/dev/             # Environment-specific configs
└── main.tf              # Root module
```

## GitHub Actions

- **Terraform Deploy** - Deploy infrastructure
- **Terraform Destroy** - Remove infrastructure

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