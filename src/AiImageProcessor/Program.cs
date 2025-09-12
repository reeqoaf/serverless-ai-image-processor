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

// Register SPN credentials
builder.Services.AddClientSecretCredential();

builder.Services.AddComputerVisionClient();
builder.Services.AddCosmosClient();
builder.Services.AddBlobServiceClient();

// Register repositories
builder.Services.AddSingleton<IImageAnalysisRepository, CosmosImageAnalysisRepository>();

// Register services
builder.Services.AddScoped<IImageAnalysisService, ComputerVisionAnalysisService>();
builder.Services.AddScoped<IImageProcessingService, ImageProcessingService>();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Build().Run();
