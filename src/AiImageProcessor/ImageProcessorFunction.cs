using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using AiImageProcessor.Services.Abstractions;

namespace AiImageProcessor;

public class ImageProcessorFunction(
    ILogger<ImageProcessorFunction> logger,
    IImageProcessingService imageProcessingService)
{
    private readonly ILogger<ImageProcessorFunction> _logger = logger;
    private readonly IImageProcessingService _imageProcessingService = imageProcessingService;

    [Function(nameof(ImageProcessorFunction))]
    public async Task Run([BlobTrigger("images/{name}", Connection = "AzureWebJobsStorage")] Stream stream, string name)
    {
        _logger.LogInformation("Blob trigger activated! Processing image: {ImageName}", name);

        try
        {
            // Process the image
            await _imageProcessingService.ProcessImageAsync(stream, name);

            _logger.LogInformation("Image processing completed for: {ImageName}", name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing image: {ImageName}", name);
            throw;
        }
    }
}