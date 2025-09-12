using AiImageProcessor.Configuration;
using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AiImageProcessor.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddApplicationConfiguration(this IServiceCollection services)
    {
        services.Configure<ApplicationSettings>(options =>
        {
            options.ComputerVisionEndpoint = Environment.GetEnvironmentVariable("COMPUTER_VISION_ENDPOINT") ?? string.Empty;
            options.CosmosDbConnectionString = Environment.GetEnvironmentVariable("COSMOSDB_CONNECTION_STRING") ?? string.Empty;
            options.StorageAccountName = Environment.GetEnvironmentVariable("STORAGE_ACCOUNT_NAME") ?? string.Empty;

            var maxRetriesStr = Environment.GetEnvironmentVariable("MAX_RETRIES");
            if (int.TryParse(maxRetriesStr, out var maxRetries) && maxRetries > 0)
            {
                options.MaxRetries = maxRetries;
            }
        });
    }

    public static void AddCosmosClient(this IServiceCollection services)
    {
        services.AddSingleton<CosmosClient>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<ApplicationSettings>>().Value;

            if (string.IsNullOrEmpty(options.CosmosDbConnectionString))
            {
                throw new InvalidOperationException("CosmosDB connection string not configured");
            }

            return new CosmosClient(options.CosmosDbConnectionString);
        });
    }

    public static void AddClientSecretCredential(this IServiceCollection services)
    {
        services.AddSingleton<ClientSecretCredential>(provider =>
        {
            var clientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID");
            var clientSecret = Environment.GetEnvironmentVariable("AZURE_CLIENT_SECRET");
            var tenantId = Environment.GetEnvironmentVariable("AZURE_TENANT_ID");

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret) || string.IsNullOrEmpty(tenantId))
            {
                throw new InvalidOperationException("SPN credentials not configured. Set AZURE_CLIENT_ID, AZURE_CLIENT_SECRET, and AZURE_TENANT_ID environment variables.");
            }

            return new ClientSecretCredential(tenantId, clientId, clientSecret);
        });
    }

    public static void AddComputerVisionClient(this IServiceCollection services)
    {
        services.AddSingleton<ComputerVisionClient>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<ApplicationSettings>>().Value;

            if (string.IsNullOrEmpty(options.ComputerVisionEndpoint))
            {
                throw new InvalidOperationException("Computer Vision endpoint not configured");
            }

            var endpoint = options.ComputerVisionEndpoint.EndsWith("/")
                ? options.ComputerVisionEndpoint
                : options.ComputerVisionEndpoint + "/";

            var apiKey = Environment.GetEnvironmentVariable("COMPUTER_VISION_API_KEY");
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("Computer Vision API key not configured");
            }

            return new ComputerVisionClient(new ApiKeyServiceClientCredentials(apiKey))
            {
                Endpoint = endpoint
            };
        });
    }

    public static void AddBlobServiceClient(this IServiceCollection services)
    {
        services.AddSingleton<BlobServiceClient>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<ApplicationSettings>>().Value;
            var credential = provider.GetRequiredService<ClientSecretCredential>();

            if (string.IsNullOrEmpty(options.StorageAccountName))
            {
                throw new InvalidOperationException("Storage account name not configured");
            }

            var blobUri = new Uri($"https://{options.StorageAccountName}.blob.core.windows.net");
            return new BlobServiceClient(blobUri, credential);
        });
    }
}
