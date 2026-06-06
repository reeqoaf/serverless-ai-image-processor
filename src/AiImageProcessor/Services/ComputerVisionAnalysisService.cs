using AiImageProcessor.Configuration;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using AiImageProcessor.Database.Entities;
using AiImageProcessor.Services.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;

namespace AiImageProcessor.Services;

public class ComputerVisionAnalysisService(
    IComputerVisionWrapper wrapper,
    IOptions<ApplicationSettings> settings,
    ILogger<ComputerVisionAnalysisService> logger) : IImageAnalysisService
{
    private readonly IComputerVisionWrapper _wrapper = wrapper ?? throw new ArgumentNullException(nameof(wrapper));
    private readonly ILogger<ComputerVisionAnalysisService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly AsyncRetryPolicy _retryPolicy = Policy
        .Handle<ComputerVisionErrorResponseException>()
        .WaitAndRetryAsync(
            settings.Value.MaxRetries,
            attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));

    private static readonly IList<VisualFeatureTypes?> Features =
    [
        VisualFeatureTypes.Objects,
        VisualFeatureTypes.Tags,
        VisualFeatureTypes.Description,
        VisualFeatureTypes.Faces,
        VisualFeatureTypes.Color,
        VisualFeatureTypes.Adult,
    ];

    public async Task<ImageAnalysisResult> AnalyzeImageAsync(Stream imageStream, string fileName)
    {
        try
        {
            _logger.LogInformation("Starting image analysis for {FileName}", fileName);

            imageStream.Position = 0;
            var analysis = await _retryPolicy.ExecuteAsync(() =>
                _wrapper.AnalyzeImageInStreamAsync(imageStream, Features));

            imageStream.Position = 0;
            var text = await ExtractTextViaReadApiAsync(imageStream, fileName);

            var result = new ImageAnalysisResult
            {
                Description  = analysis.Description?.Captions?.FirstOrDefault()?.Text ?? "No description available",
                Tags         = analysis.Tags?.Select(t => t.Name).ToList() ?? [],
                Objects      = MapDetectedObjects(analysis.Objects),
                Faces        = MapDetectedFaces(analysis.Faces),
                Text         = text,
                Colors       = MapColorAnalysis(analysis.Color),
                AdultContent = MapAdultContent(analysis.Adult),
                ImageType    = DetermineImageType(analysis),
                Dimensions   = $"{analysis.Metadata?.Width ?? 0}x{analysis.Metadata?.Height ?? 0}",
            };

            _logger.LogInformation("Image analysis completed for {FileName}", fileName);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing image {FileName}", fileName);
            throw;
        }
    }

    private async Task<List<string>> ExtractTextViaReadApiAsync(Stream imageStream, string fileName)
    {
        try
        {
            var headers = await _wrapper.ReadInStreamAsync(imageStream);

            // Operation-Location header format: .../read/analyzeResults/{operationId}
            var operationUrl = headers.OperationLocation;
            if (string.IsNullOrEmpty(operationUrl) || !Guid.TryParse(operationUrl.Split('/').Last(), out var operationId))
            {
                _logger.LogWarning("Could not parse Read API operation ID from URL for {FileName}", fileName);
                return [];
            }

            const int maxAttempts = 60;
            const int delayMs = 500;

            for (var attempt = 0; attempt < maxAttempts; attempt++)
            {
                await Task.Delay(delayMs);
                var result = await _wrapper.GetReadResultAsync(operationId);

                if (result.Status == OperationStatusCodes.Succeeded)
                    return ExtractLinesFromReadResult(result);

                if (result.Status == OperationStatusCodes.Failed)
                {
                    _logger.LogWarning("Read API operation failed for {FileName}", fileName);
                    return [];
                }
            }

            _logger.LogWarning("Read API timed out for {FileName}", fileName);
            return [];
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OCR (Read API) failed for {FileName}, continuing without text extraction", fileName);
            return [];
        }
    }

    private static List<string> ExtractLinesFromReadResult(ReadOperationResult result)
    {
        if (result.AnalyzeResult?.ReadResults == null) return [];

        return result.AnalyzeResult.ReadResults
            .SelectMany(r => r.Lines ?? [])
            .Select(l => l.Text)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .ToList();
    }

    private static List<Database.Entities.DetectedObject> MapDetectedObjects(IList<Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models.DetectedObject>? objects)
    {
        if (objects == null) return [];

        return objects.Select(obj => new Database.Entities.DetectedObject
        {
            Name       = obj.ObjectProperty ?? "Unknown",
            Confidence = obj.Confidence,
            BoundingBox = obj.Rectangle != null ? new BoundingBox
            {
                X      = obj.Rectangle.X,
                Y      = obj.Rectangle.Y,
                Width  = obj.Rectangle.W,
                Height = obj.Rectangle.H,
            } : null,
        }).ToList();
    }

    private static List<DetectedFace> MapDetectedFaces(IList<FaceDescription>? faces)
    {
        if (faces == null) return [];

        return faces.Select(face => new DetectedFace
        {
            Age    = face.Age,
            Gender = face.Gender?.ToString() ?? "Unknown",
            Emotion = "Unknown",
            BoundingBox = face.FaceRectangle != null ? new BoundingBox
            {
                X      = face.FaceRectangle.Left,
                Y      = face.FaceRectangle.Top,
                Width  = face.FaceRectangle.Width,
                Height = face.FaceRectangle.Height,
            } : null,
        }).ToList();
    }

    private static ColorAnalysis MapColorAnalysis(ColorInfo? colorInfo)
    {
        if (colorInfo == null) return new ColorAnalysis();

        return new ColorAnalysis
        {
            Dominant        = colorInfo.DominantColorForeground ?? "#000000",
            Accent          = colorInfo.DominantColorBackground ?? "#FFFFFF",
            IsBlackAndWhite = colorInfo.IsBWImg,
        };
    }

    private static AdultContentAnalysis MapAdultContent(AdultInfo? adultInfo)
    {
        if (adultInfo == null) return new AdultContentAnalysis();

        return new AdultContentAnalysis
        {
            IsAdultContent = adultInfo.IsAdultContent,
            AdultScore     = adultInfo.AdultScore,
            RacyScore      = adultInfo.RacyScore,
        };
    }

    private static string DetermineImageType(ImageAnalysis analysis)
    {
        if (analysis.Adult?.IsAdultContent == true) return "Adult";
        if (analysis.Faces?.Any() == true) return "Portrait";
        if (analysis.Objects?.Any() == true) return "Object";
        return "General";
    }
}
