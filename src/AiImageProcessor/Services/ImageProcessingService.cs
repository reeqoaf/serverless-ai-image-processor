using System.Diagnostics;
using Azure.Storage.Blobs;
using AiImageProcessor.Database.Entities;
using AiImageProcessor.Database.Repositories.Abstractions;
using AiImageProcessor.Services.Abstractions;
using Microsoft.Extensions.Logging;

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
        var stopwatch = Stopwatch.StartNew();
        var fileSize = imageStream.Length;

        var imagesContainer = _blobServiceClient.GetBlobContainerClient("images");
        var blobClient = imagesContainer.GetBlobClient(fileName);

        try
        {
            _logger.LogInformation("Starting image processing for {FileName}", fileName);

            var analysisResult = await _imageAnalysisService.AnalyzeImageAsync(imageStream, fileName);

            var blobProperties = await blobClient.GetPropertiesAsync();

            var metadata = new ImageMetadata
            {
                FileSize        = fileSize,
                Format          = Path.GetExtension(fileName).TrimStart('.'),
                Dimensions      = analysisResult.Dimensions,
                RetryCount      = 0,
                AiServiceUsed   = "ComputerVision",
                Version         = "1.0",
            };

            var storageInfo = new StorageInfo
            {
                Container = "images",
                BlobName  = fileName,
                Url       = blobClient.Uri.ToString(),
                ETag      = blobProperties.Value.ETag.ToString(),
            };

            var document = await _imageAnalysisRepository.StoreAnalysisAsync(fileName, analysisResult, metadata, storageInfo);

            await UpdateBlobMetadataAsync(blobClient, "completed");

            stopwatch.Stop();
            metadata.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;

            _logger.LogInformation(
                "Image processing completed for {FileName} in {ElapsedMs}ms (doc: {DocumentId})",
                fileName, stopwatch.ElapsedMilliseconds, document.Id);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error processing image {FileName} after {ElapsedMs}ms", fileName, stopwatch.ElapsedMilliseconds);
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
                ["processed"]          = status == "completed" ? "true" : "false",
                ["processed_at"]       = DateTime.UtcNow.ToString("O"),
                ["processed_by"]       = "AI-Image-Processor",
                ["processing_status"]  = status,
            };

            if (!string.IsNullOrEmpty(errorMessage))
                metadata["error_message"] = SanitizeMetadataValue(errorMessage);

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

        return new string(value.Where(c => !char.IsControl(c) && c != '\0').ToArray());
    }
}
