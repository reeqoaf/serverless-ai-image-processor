using Azure.Storage.Blobs;
using AiImageProcessor.Database.Entities;
using AiImageProcessor.Database.Repositories.Abstractions;
using AiImageProcessor.Services.Abstractions;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace AiImageProcessor.Services;

public class ImageProcessingService(
    IImageAnalysisService imageAnalysisService,
    IImageAnalysisRepository imageAnalysisRepository,
    BlobServiceClient blobServiceClient,
    ILogger<ImageProcessingService> logger) : IImageProcessingService
{
    private readonly IImageAnalysisService _imageAnalysisService = imageAnalysisService ?? throw new ArgumentNullException(nameof(imageAnalysisService));
    private readonly IImageAnalysisRepository _imageAnalysisRepository = imageAnalysisRepository ?? throw new ArgumentNullException(nameof(imageAnalysisRepository));
    private readonly BlobServiceClient _blobServiceClient = blobServiceClient ?? throw new ArgumentNullException(nameof(blobServiceClient));
    private readonly ILogger<ImageProcessingService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task ProcessImageAsync(Stream imageStream, string fileName)
    {
        var startTime = DateTime.UtcNow;
        var imagesContainer = _blobServiceClient.GetBlobContainerClient("images");
        var blobClient = imagesContainer.GetBlobClient(fileName);

        try
        {
            _logger.LogInformation("Starting image processing for {FileName}", fileName);

            // Analyze image
            var analysisResult = await _imageAnalysisService.AnalyzeImageAsync(imageStream, fileName);

            // Create metadata
            var metadata = new ImageMetadata
            {
                FileSize = imageStream.Length,
                Format = Path.GetExtension(fileName).TrimStart('.'),
                Dimensions = "Unknown", // Image dimensions not available from current analysis
                RetryCount = 0,
                ProcessingTimeMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds,
                AiServiceUsed = "ComputerVision",
                Version = "1.0"
            };

            // Create storage info
            var storageInfo = new StorageInfo
            {
                Container = "images",
                BlobName = fileName,
                Url = blobClient.Uri.ToString(),
                ETag = (await blobClient.GetPropertiesAsync()).Value.ETag.ToString()
            };

            // Store in database
            var document = await _imageAnalysisRepository.StoreAnalysisAsync(fileName, analysisResult, metadata, storageInfo);
            _logger.LogInformation("Stored analysis in database with ID: {DocumentId}", document.Id);

            // Update blob metadata
            await UpdateBlobMetadataAsync(blobClient, "completed");

            _logger.LogInformation("Image processing completed for: {FileName}", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing image {FileName}", fileName);
            await UpdateBlobMetadataAsync(blobClient, "failed", ex.Message);
            throw;
        }
    }

    private async Task UpdateBlobMetadataAsync(BlobClient blobClient, string status, string? errorMessage = null)
    {
        try
        {
            var metadata = new Dictionary<string, string>
            {
                ["processed"] = status == "completed" ? "true" : "false",
                ["processed_at"] = DateTime.UtcNow.ToString("O"),
                ["processed_by"] = "AI-Image-Processor",
                ["processing_status"] = status
            };

            if (!string.IsNullOrEmpty(errorMessage))
            {
                // Sanitize error message for metadata (remove invalid characters)
                var sanitizedError = SanitizeMetadataValue(errorMessage);
                metadata["error_message"] = sanitizedError;
            }

            await blobClient.SetMetadataAsync(metadata);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update blob metadata for {BlobName}", blobClient.Name);
        }
    }

    private static string SanitizeMetadataValue(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        // Remove control characters and other invalid characters for Azure Storage metadata
        return new string(value.Where(c => !char.IsControl(c) && c != '\0').ToArray());
    }
}
