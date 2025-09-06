using AiImageProcessor.Database.Entities;

namespace AiImageProcessor.Services.Abstractions;

public interface IImageAnalysisService
{
    Task<ImageAnalysisResult> AnalyzeImageAsync(Stream imageStream, string fileName);
}
