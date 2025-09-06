# Serverless AI Image Processor

Event-driven image processing pipeline using Azure services with Infrastructure as Code.

## Architecture

```
Image Upload → Blob Storage → Function Trigger → Computer Vision API → CosmosDB
                                                      ↓
                                              Blob Metadata Update
```

## Tech Stack

- **Azure**: Blob Storage, Functions, CosmosDB, Computer Vision
- **Infrastructure**: Terraform (modular)
- **Runtime**: .NET 9 Function App (isolated worker)
- **Security**: Service Principal authentication (where possible)

## Quick Start

### 1. Deploy Infrastructure

```bash
cd terraform
terraform init
terraform apply -var-file="envs/dev/terraform.tfvars"
```

### 2. Deploy Function App

```bash
cd src/AiImageProcessor
func azure functionapp publish <function-app-name>
```

### 3. Test

Upload an image to the `images` container in your storage account.

## Features

- **Event-driven**: Automatic processing on image upload
- **AI Analysis**: Object detection, OCR, face detection, adult content filtering
- **Serverless**: Auto-scaling with consumption-based pricing
- **Secure**: SPN authentication, no hardcoded secrets
- **Cost-optimized**: Serverless CosmosDB, free tiers for dev

## Project Structure

```
├── terraform/              # Infrastructure as Code
│   ├── modules/           # Reusable modules
│   └── envs/             # Environment configs
├── src/AiImageProcessor/  # .NET Function App
│   ├── Services/         # Business logic
│   ├── Database/         # Data access
│   └── Configuration/    # App settings
└── .github/workflows/    # CI/CD
```

## Configuration

All configuration is managed via environment variables set by Terraform:

- `AzureWebJobsStorage` - Function App runtime storage
- `COSMOSDB_CONNECTION_STRING` - Database connection
- `COMPUTER_VISION_ENDPOINT` - AI service endpoint
- `COMPUTER_VISION_API_KEY` - AI service key
- `AZURE_CLIENT_ID/SECRET/TENANT_ID` - SPN credentials

## Development

```bash
# Local development
cd src/AiImageProcessor
func start

# Build
dotnet build

# Deploy
func azure functionapp publish <name>
```

## Security

- ✅ Service Principal authentication
- ✅ RBAC permissions
- ✅ No hardcoded secrets
- ✅ Environment isolation

## Cost

- **Development**: $0-5/month (free tiers)
- **Production**: $100-300/month (premium tiers)

## License

MIT License - see [LICENSE](LICENSE) file.