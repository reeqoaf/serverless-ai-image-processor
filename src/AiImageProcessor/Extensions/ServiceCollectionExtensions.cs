using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using AiImageProcessor.Configuration;

namespace AiImageProcessor.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationConfiguration(this IServiceCollection services)
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

        return services;
    }
}
