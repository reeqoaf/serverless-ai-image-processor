using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.Cosmos;
using Azure.Identity;
using Azure.Storage.Blobs;
using AiImageProcessor.Extensions;
using AiImageProcessor.Configuration;
using AiImageProcessor.Database.Repositories;
using AiImageProcessor.Database.Repositories.Abstractions;
using AiImageProcessor.Services;
using AiImageProcessor.Services.Abstractions;

var builder = FunctionsApplication.CreateBuilder(args);

// Configure application settings
builder.Services.AddApplicationConfiguration();

// Register repositories
builder.Services.AddSingleton<IImageAnalysisRepository, CosmosImageAnalysisRepository>();

// Register SPN credentials
builder.Services.AddSingleton<ClientSecretCredential>(provider =>
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

// Register Computer Vision client with API key
builder.Services.AddSingleton<ComputerVisionClient>(provider =>
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

// Register CosmosDB client with connection string
builder.Services.AddSingleton<CosmosClient>(provider =>
{
    var options = provider.GetRequiredService<IOptions<ApplicationSettings>>().Value;

    if (string.IsNullOrEmpty(options.CosmosDbConnectionString))
    {
        throw new InvalidOperationException("CosmosDB connection string not configured");
    }

    return new CosmosClient(options.CosmosDbConnectionString);
});

// Register Blob Service client with SPN authentication
builder.Services.AddSingleton<BlobServiceClient>(provider =>
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

// Register services
builder.Services.AddScoped<IImageAnalysisService, ComputerVisionAnalysisService>();
builder.Services.AddScoped<IImageProcessingService, ImageProcessingService>();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Build().Run();
