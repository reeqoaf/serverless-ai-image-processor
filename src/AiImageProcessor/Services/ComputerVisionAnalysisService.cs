using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using AiImageProcessor.Database.Entities;
using AiImageProcessor.Services.Abstractions;
using Microsoft.Extensions.Logging;

namespace AiImageProcessor.Services;

public class ComputerVisionAnalysisService(ComputerVisionClient computerVisionClient, ILogger<ComputerVisionAnalysisService> logger) : IImageAnalysisService
{
    private readonly ComputerVisionClient _computerVisionClient = computerVisionClient ?? throw new ArgumentNullException(nameof(computerVisionClient));
    private readonly ILogger<ComputerVisionAnalysisService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<ImageAnalysisResult> AnalyzeImageAsync(Stream imageStream, string fileName)
    {
        try
        {
            _logger.LogInformation("Starting image analysis for {FileName}", fileName);
            _logger.LogInformation("Computer Vision Endpoint: {Endpoint}", _computerVisionClient.Endpoint);

            // Reset stream position
            imageStream.Position = 0;

            // Analyze image with multiple features
            var features = new List<VisualFeatureTypes?>
            {
                VisualFeatureTypes.Objects,
                VisualFeatureTypes.Tags,
                VisualFeatureTypes.Description,
                VisualFeatureTypes.Faces,
                VisualFeatureTypes.Color,
                VisualFeatureTypes.Adult
            };

            var analysis = await _computerVisionClient.AnalyzeImageInStreamAsync(imageStream, features);

            // Extract text using OCR
            OcrResult? ocrResult = null;
            if (imageStream.CanSeek)
            {
                imageStream.Position = 0;
                try
                {
                    ocrResult = await _computerVisionClient.RecognizePrintedTextInStreamAsync(true, imageStream);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "OCR failed for {FileName}, continuing without text extraction", fileName);
                }
            }
            else
            {
                _logger.LogWarning("Stream does not support seeking, skipping OCR for {FileName}", fileName);
            }

            var result = new ImageAnalysisResult
            {
                Description = analysis.Description?.Captions?.FirstOrDefault()?.Text ?? "No description available",
                Tags = analysis.Tags?.Select(t => t.Name).ToList() ?? new List<string>(),
                Objects = MapDetectedObjects(analysis.Objects),
                Faces = MapDetectedFaces(analysis.Faces),
                Text = ocrResult != null ? ExtractTextFromOcr(ocrResult) : new List<string>(), // Handle null OCR result
                Colors = MapColorAnalysis(analysis.Color),
                AdultContent = MapAdultContent(analysis.Adult),
                ImageType = DetermineImageType(analysis)
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

    private static List<Database.Entities.DetectedObject> MapDetectedObjects(IList<Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models.DetectedObject>? objects)
    {
        if (objects == null) return new List<Database.Entities.DetectedObject>();

        return objects.Select(obj => new Database.Entities.DetectedObject
        {
            Name = obj.ObjectProperty ?? "Unknown",
            Confidence = obj.Confidence,
            BoundingBox = obj.Rectangle != null ? new BoundingBox
            {
                X = obj.Rectangle.X,
                Y = obj.Rectangle.Y,
                Width = obj.Rectangle.W,
                Height = obj.Rectangle.H
            } : null
        }).ToList();
    }

    private static List<DetectedFace> MapDetectedFaces(IList<FaceDescription>? faces)
    {
        if (faces == null) return new List<DetectedFace>();

        return faces.Select(face => new DetectedFace
        {
            Age = face.Age,
            Gender = face.Gender?.ToString() ?? "Unknown",
            Emotion = "Unknown", // Emotion detection not available in current Computer Vision API
            Confidence = 0.9, // Default confidence
            BoundingBox = face.FaceRectangle != null ? new BoundingBox
            {
                X = face.FaceRectangle.Left,
                Y = face.FaceRectangle.Top,
                Width = face.FaceRectangle.Width,
                Height = face.FaceRectangle.Height
            } : null
        }).ToList();
    }

    private static List<string> ExtractTextFromOcr(OcrResult? ocrResult)
    {
        if (ocrResult?.Regions == null) return new List<string>();

        var textLines = new List<string>();
        foreach (var region in ocrResult.Regions)
        {
            if (region.Lines != null)
            {
                foreach (var line in region.Lines)
                {
                    if (line.Words != null)
                    {
                        var lineText = string.Join(" ", line.Words.Select(w => w.Text));
                        if (!string.IsNullOrWhiteSpace(lineText))
                        {
                            textLines.Add(lineText);
                        }
                    }
                }
            }
        }

        return textLines;
    }

    private static ColorAnalysis MapColorAnalysis(ColorInfo? colorInfo)
    {
        if (colorInfo == null) return new ColorAnalysis();

        return new ColorAnalysis
        {
            Dominant = colorInfo.DominantColorForeground ?? "#000000",
            Accent = colorInfo.DominantColorBackground ?? "#FFFFFF",
            IsBlackAndWhite = colorInfo.IsBWImg
        };
    }

    private static AdultContentAnalysis MapAdultContent(AdultInfo? adultInfo)
    {
        if (adultInfo == null) return new AdultContentAnalysis();

        return new AdultContentAnalysis
        {
            IsAdultContent = adultInfo.IsAdultContent,
            AdultScore = adultInfo.AdultScore,
            RacyScore = adultInfo.RacyScore
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
