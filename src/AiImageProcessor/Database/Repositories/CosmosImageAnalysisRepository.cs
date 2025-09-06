using Microsoft.Azure.Cosmos;
using AiImageProcessor.Database.Entities;
using AiImageProcessor.Database.Repositories.Abstractions;
using Microsoft.Extensions.Logging;

namespace AiImageProcessor.Database.Repositories;

public class CosmosImageAnalysisRepository(CosmosClient cosmosClient, ILogger<IImageAnalysisRepository> logger) : IImageAnalysisRepository
{
    private readonly CosmosClient _cosmosClient = cosmosClient ?? throw new ArgumentNullException(nameof(cosmosClient));
    private readonly ILogger<IImageAnalysisRepository> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly Container _container = cosmosClient.GetDatabase("ImageAnalysis").GetContainer("Images");

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

    public async Task<List<ImageAnalysisDocument>> GetByDateAsync(DateTime date)
    {
        try
        {
            var partitionKey = date.ToString("yyyy-MM-dd");
            var queryDefinition = new QueryDefinition("SELECT * FROM c WHERE c.partitionKey = @partitionKey")
                .WithParameter("@partitionKey", partitionKey);

            var results = new List<ImageAnalysisDocument>();
            var iterator = _container.GetItemQueryIterator<ImageAnalysisDocument>(queryDefinition);
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                foreach (var item in response)
                {
                    results.Add(item);
                }
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get documents by date {Date}", date);
            throw;
        }
    }

    public async Task<List<ImageAnalysisDocument>> GetByTagAsync(string tag)
    {
        try
        {
            var queryDefinition = new QueryDefinition("SELECT * FROM c WHERE ARRAY_CONTAINS(c.analysis.tags, @tag)")
                .WithParameter("@tag", tag);

            var results = new List<ImageAnalysisDocument>();
            var iterator = _container.GetItemQueryIterator<ImageAnalysisDocument>(queryDefinition);
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                foreach (var item in response)
                {
                    results.Add(item);
                }
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get documents by tag {Tag}", tag);
            throw;
        }
    }

    public async Task<List<ImageAnalysisDocument>> GetByStatusAsync(string status)
    {
        try
        {
            var queryDefinition = new QueryDefinition("SELECT * FROM c WHERE c.status = @status")
                .WithParameter("@status", status);

            var results = new List<ImageAnalysisDocument>();
            var iterator = _container.GetItemQueryIterator<ImageAnalysisDocument>(queryDefinition);
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                foreach (var item in response)
                {
                    results.Add(item);
                }
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get documents by status {Status}", status);
            throw;
        }
    }

    public async Task<List<ImageAnalysisDocument>> GetFailedImagesAsync()
    {
        try
        {
            var queryDefinition = new QueryDefinition("SELECT * FROM c WHERE c.status = 'Failed' OR c.status = 'PermanentlyFailed'");

            var results = new List<ImageAnalysisDocument>();
            var iterator = _container.GetItemQueryIterator<ImageAnalysisDocument>(queryDefinition);
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                foreach (var item in response)
                {
                    results.Add(item);
                }
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get failed images");
            throw;
        }
    }

    public async Task UpdateStatusAsync(string id, string status, string? errorMessage = null)
    {
        try
        {
            var document = await GetByIdAsync(id);
            if (document != null)
            {
                document.Status = status;
                // Error information is stored in blob metadata instead
                await _container.ReplaceItemAsync(document, id, new PartitionKey(document.PartitionKey));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update status for document {Id}", id);
            throw;
        }
    }

    public async Task DeleteAsync(string id)
    {
        try
        {
            await _container.DeleteItemAsync<ImageAnalysisDocument>(id, new PartitionKey(GetPartitionKeyFromId(id)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete document {Id}", id);
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