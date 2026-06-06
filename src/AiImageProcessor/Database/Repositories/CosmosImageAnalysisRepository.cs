using System.Net;
using System.Security.Cryptography;
using System.Text;
using AiImageProcessor.Configuration;
using AiImageProcessor.Database.Entities;
using AiImageProcessor.Database.Repositories.Abstractions;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;

namespace AiImageProcessor.Database.Repositories;

public class CosmosImageAnalysisRepository : IImageAnalysisRepository
{
    private const string DatabaseName = "ImageAnalysis";
    private const string ContainerName = "Images";

    private readonly Container _container;
    private readonly ILogger<CosmosImageAnalysisRepository> _logger;
    private readonly AsyncRetryPolicy _retryPolicy;

    public CosmosImageAnalysisRepository(
        CosmosClient cosmosClient,
        IOptions<ApplicationSettings> settings,
        ILogger<CosmosImageAnalysisRepository> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _container = cosmosClient.GetDatabase(DatabaseName).GetContainer(ContainerName);
        _retryPolicy = BuildRetryPolicy(settings.Value.MaxRetries);
    }

    internal CosmosImageAnalysisRepository(
        Container container,
        IOptions<ApplicationSettings> settings,
        ILogger<CosmosImageAnalysisRepository> logger)
    {
        _container = container ?? throw new ArgumentNullException(nameof(container));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _retryPolicy = BuildRetryPolicy(settings.Value.MaxRetries);
    }

    public async Task<ImageAnalysisDocument> StoreAnalysisAsync(
        string fileName,
        ImageAnalysisResult analysis,
        ImageMetadata metadata,
        StorageInfo storage)
    {
        try
        {
            var documentId   = GenerateDocumentId(fileName);
            var partitionKey = GetPartitionKey(documentId);

            var document = new ImageAnalysisDocument
            {
                Id           = documentId,
                PartitionKey = partitionKey,
                FileName     = fileName,
                Analysis     = analysis,
                Metadata     = metadata,
                Storage      = storage,
                ProcessedAt  = DateTime.UtcNow,
                Status       = "Completed",
            };

            var response = await _retryPolicy.ExecuteAsync(() =>
                _container.UpsertItemAsync(document, new PartitionKey(partitionKey)));

            _logger.LogInformation("Stored analysis for {FileName} with ID {DocumentId}", fileName, document.Id);

            return response.Resource;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store analysis for {FileName}", fileName);
            throw;
        }
    }

    public async Task<ImageAnalysisDocument?> GetByIdAsync(string id)
    {
        try
        {
            var response = await _container.ReadItemAsync<ImageAnalysisDocument>(
                id, new PartitionKey(GetPartitionKey(id)));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get document by ID {Id}", id);
            throw;
        }
    }

    internal static string GenerateDocumentId(string fileName)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(fileName.ToLowerInvariant())));
        return hash[..32];
    }

    internal static string GetPartitionKey(string documentId)
        => documentId[..2];

    private static AsyncRetryPolicy BuildRetryPolicy(int maxRetries) =>
        Policy
            .Handle<CosmosException>(e => (int)e.StatusCode >= 500)
            .WaitAndRetryAsync(
                maxRetries,
                attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));
}
