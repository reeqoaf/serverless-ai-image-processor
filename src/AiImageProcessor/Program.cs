using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using AiImageProcessor.Extensions;
using AiImageProcessor.Database.Repositories;
using AiImageProcessor.Database.Repositories.Abstractions;
using AiImageProcessor.Services;
using AiImageProcessor.Services.Abstractions;

var builder = FunctionsApplication.CreateBuilder(args);

builder.Services.AddApplicationConfiguration();

builder.Services.AddClientSecretCredential();

builder.Services.AddComputerVisionClient();
builder.Services.AddComputerVisionWrapper();
builder.Services.AddCosmosClient();
builder.Services.AddBlobServiceClient();

builder.Services.AddSingleton<IImageAnalysisRepository, CosmosImageAnalysisRepository>();

builder.Services.AddScoped<IImageAnalysisService, ComputerVisionAnalysisService>();
builder.Services.AddScoped<IImageProcessingService, ImageProcessingService>();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Build().Run();
