using Microsoft.Azure.Cosmos;
using AiImageProcessor.Database.Entities;
using AiImageProcessor.Database.Repositories.Abstractions;
using Microsoft.Extensions.Logging;

namespace AiImageProcessor.Database.Repositories;

public class CosmosImageAnalysisRepository(CosmosClient cosmosClient, ILogger<IImageAnalysisRepository> logger) : IImageAnalysisRepository
{
    private const string DatabaseName = "ImageAnalysis";
    private const string ContainerName = "Images";

    private readonly ILogger<IImageAnalysisRepository> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly Container _container = cosmosClient.GetDatabase(DatabaseName).GetContainer(ContainerName);

    public async Task<ImageAnalysisDocument> StoreAnalysisAsync(string fileName, ImageAnalysisResult analysis, ImageMetadata metadata, StorageInfo storage)
    {
        try
        {
            var documentId = GenerateDocumentId(fileName);
            var partitionKey = GetPartitionKeyFromId(documentId);

            var document = new ImageAnalysisDocument
            {
                Id = documentId,
                PartitionKey = partitionKey,
                FileName = fileName,
                Analysis = analysis,
                Metadata = metadata,
                Storage = storage,
                ProcessedAt = DateTime.UtcNow,
                Status = "Completed"
            };

            var response = await _container.CreateItemAsync(document, new PartitionKey(document.PartitionKey));

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
            var response = await _container.ReadItemAsync<ImageAnalysisDocument>(id, new PartitionKey(GetPartitionKeyFromId(id)));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get document by ID {Id}", id);
            throw;
        }
    }

    private static string GenerateDocumentId(string fileName)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var sanitizedFileName = Path.GetFileNameWithoutExtension(fileName).Replace(" ", "_");
        return $"{sanitizedFileName}_{timestamp}";
    }

    private static string GetPartitionKeyFromId(string id)
    {
        // Extract date from ID for partitioning
        var datePart = id.Split('_').LastOrDefault()?.Substring(0, 8) ?? DateTime.UtcNow.ToString("yyyyMMdd");
        return DateTime.ParseExact(datePart, "yyyyMMdd", null).ToString("yyyy-MM-dd");
    }
}