using AiImageProcessor.Configuration;
using AiImageProcessor.Services;
using AiImageProcessor.Services.Abstractions;
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
            options.ComputerVisionEndpoint    = Environment.GetEnvironmentVariable("COMPUTER_VISION_ENDPOINT") ?? string.Empty;
            options.ComputerVisionApiKey      = Environment.GetEnvironmentVariable("COMPUTER_VISION_API_KEY") ?? string.Empty;
            options.CosmosDbConnectionString  = Environment.GetEnvironmentVariable("COSMOSDB_CONNECTION_STRING") ?? string.Empty;
            options.StorageAccountName        = Environment.GetEnvironmentVariable("STORAGE_ACCOUNT_NAME") ?? string.Empty;
            options.AzureClientId             = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID") ?? string.Empty;
            options.AzureClientSecret         = Environment.GetEnvironmentVariable("AZURE_CLIENT_SECRET") ?? string.Empty;
            options.AzureTenantId             = Environment.GetEnvironmentVariable("AZURE_TENANT_ID") ?? string.Empty;

            var maxRetriesStr = Environment.GetEnvironmentVariable("MAX_RETRIES");
            if (int.TryParse(maxRetriesStr, out var maxRetries) && maxRetries > 0)
                options.MaxRetries = maxRetries;
        });
    }

    public static void AddCosmosClient(this IServiceCollection services)
    {
        services.AddSingleton<CosmosClient>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<ApplicationSettings>>().Value;

            if (string.IsNullOrEmpty(options.CosmosDbConnectionString))
                throw new InvalidOperationException("CosmosDB connection string not configured");

            return new CosmosClient(options.CosmosDbConnectionString);
        });
    }

    public static void AddClientSecretCredential(this IServiceCollection services)
    {
        services.AddSingleton<ClientSecretCredential>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<ApplicationSettings>>().Value;

            if (string.IsNullOrEmpty(options.AzureClientId) ||
                string.IsNullOrEmpty(options.AzureClientSecret) ||
                string.IsNullOrEmpty(options.AzureTenantId))
            {
                throw new InvalidOperationException(
                    "SPN credentials not configured. Set AZURE_CLIENT_ID, AZURE_CLIENT_SECRET, and AZURE_TENANT_ID environment variables.");
            }

            return new ClientSecretCredential(options.AzureTenantId, options.AzureClientId, options.AzureClientSecret);
        });
    }

    public static void AddComputerVisionClient(this IServiceCollection services)
    {
        services.AddSingleton<ComputerVisionClient>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<ApplicationSettings>>().Value;

            if (string.IsNullOrEmpty(options.ComputerVisionEndpoint))
                throw new InvalidOperationException("Computer Vision endpoint not configured");

            if (string.IsNullOrEmpty(options.ComputerVisionApiKey))
                throw new InvalidOperationException("Computer Vision API key not configured");

            var endpoint = options.ComputerVisionEndpoint.EndsWith("/")
                ? options.ComputerVisionEndpoint
                : options.ComputerVisionEndpoint + "/";

            return new ComputerVisionClient(new ApiKeyServiceClientCredentials(options.ComputerVisionApiKey))
            {
                Endpoint = endpoint
            };
        });
    }

    public static void AddComputerVisionWrapper(this IServiceCollection services)
    {
        services.AddSingleton<IComputerVisionWrapper, ComputerVisionClientWrapper>();
    }

    public static void AddBlobServiceClient(this IServiceCollection services)
    {
        services.AddSingleton<BlobServiceClient>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<ApplicationSettings>>().Value;
            var credential = provider.GetRequiredService<ClientSecretCredential>();

            if (string.IsNullOrEmpty(options.StorageAccountName))
                throw new InvalidOperationException("Storage account name not configured");

            var blobUri = new Uri($"https://{options.StorageAccountName}.blob.core.windows.net");
            return new BlobServiceClient(blobUri, credential);
        });
    }
}
