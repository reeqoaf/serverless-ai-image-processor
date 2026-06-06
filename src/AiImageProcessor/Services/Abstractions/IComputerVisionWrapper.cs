using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;

namespace AiImageProcessor.Services.Abstractions;

public interface IComputerVisionWrapper
{
    Task<ImageAnalysis> AnalyzeImageInStreamAsync(Stream image, IList<VisualFeatureTypes?> features);
    Task<ReadInStreamHeaders> ReadInStreamAsync(Stream image);
    Task<ReadOperationResult> GetReadResultAsync(Guid operationId);
}
