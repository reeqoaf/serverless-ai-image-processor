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

### Option 1: GitHub Actions (Recommended)

1. **Configure GitHub Repository**:
   - Add repository secrets: `AZURE_CREDENTIALS` and `TF_BACKEND_CONFIG`
   - Create `dev` environment with environment variables
   - See [GitHub Actions Setup](#github-actions-setup) for detailed instructions

2. **Deploy via GitHub Actions**:
   - Go to Actions > Terraform Deploy
   - Click "Run workflow"
   - Select environment: `dev`
   - Select action: `plan` (first) then `apply`

### Option 2: Local Deployment

1. **Deploy Infrastructure**:
   ```bash
   cd terraform
   terraform init
   terraform apply -var-file="envs/dev/terraform.tfvars"
   ```

2. **Deploy Function App**:
   ```bash
   cd src/AiImageProcessor
   func azure functionapp publish <function-app-name>
   ```

3. **Test**:
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

## GitHub Actions Setup

### Required Secrets

Configure these in your GitHub repository settings under Settings > Secrets and variables > Actions > Repository secrets:

#### AZURE_CREDENTIALS
Azure Service Principal credentials in JSON format:
```json
{
  "clientId": "your-client-id",
  "clientSecret": "your-client-secret", 
  "tenantId": "your-tenant-id",
  "subscriptionId": "your-subscription-id"
}
```

#### TF_BACKEND_CONFIG
Terraform backend configuration:
```
resource_group_name=your-rg,storage_account_name=your-storage,container_name=tfstate,key=terraform-dev.tfstate
```

### Environment Variables

Configure these in your GitHub repository settings under Settings > Secrets and variables > Actions > Environment variables for each environment:

#### Dev Environment Variables
| Variable Name | Description | Example Value |
|---------------|-------------|---------------|
| `DEV_SUBSCRIPTION_ID` | Azure Subscription ID | `d505b2ea-ccd0-42e1-830c-3e61bf61bd2b` |
| `DEV_PROJECT_NAME` | Project name for resource naming | `ai-image-processor` |
| `DEV_ENVIRONMENT` | Environment name | `dev` |
| `DEV_LOCATION` | Azure region | `West Europe` |
| `DEV_STORAGE_TIER` | Storage account tier | `Standard` |
| `DEV_STORAGE_REPLICATION_TYPE` | Storage replication type | `LRS` |
| `DEV_STORAGE_ENABLE_VERSIONING` | Enable blob versioning | `false` |
| `DEV_COGNITIVE_SERVICES_SKU` | Cognitive Services SKU | `F0` |
| `DEV_FUNCTION_APP_SKU_TIER` | Function App SKU tier | `Consumption` |
| `DEV_FUNCTION_APP_SKU_SIZE` | Function App SKU size | `Y1` |
| `DEV_MAX_RETRIES` | Maximum retries for operations | `3` |
| `DEV_TAGS_PROJECT` | Project tag value | `AI Image Processor` |
| `DEV_TAGS_ENVIRONMENT` | Environment tag value | `Development` |
| `DEV_TAGS_MANAGED_BY` | ManagedBy tag value | `Terraform` |
| `DEV_TAGS_OWNER` | Owner tag value | `Dev Team` |
| `DEV_TAGS_COST_CENTER` | CostCenter tag value | `R&D` |

### Workflow Usage

#### Deploy Infrastructure
1. Go to the Actions tab in your GitHub repository
2. Select "Terraform Deploy" workflow
3. Click "Run workflow"
4. Select:
   - **Environment**: Choose the target environment (dev)
   - **Action**: Choose the action to perform:
     - `plan`: Create and display a Terraform plan (safe, read-only)
     - `apply`: Apply the Terraform plan (creates/modifies resources)

#### Destroy Infrastructure
1. Go to the Actions tab in your GitHub repository
2. Select "Terraform Destroy" workflow
3. Click "Run workflow"
4. Select:
   - **Environment**: Choose the target environment (dev)
   - **Confirm Destroy**: Type "DESTROY" to confirm destruction of all resources

## License

MIT License - see [LICENSE](LICENSE) file.