using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using AiImageProcessor.Services.Abstractions;

namespace AiImageProcessor.Services;

public class ComputerVisionClientWrapper(ComputerVisionClient client) : IComputerVisionWrapper
{
    private readonly ComputerVisionClient _client = client ?? throw new ArgumentNullException(nameof(client));

    public Task<ImageAnalysis> AnalyzeImageInStreamAsync(Stream image, IList<VisualFeatureTypes?> features)
        => _client.AnalyzeImageInStreamAsync(image, features);

    public Task<ReadInStreamHeaders> ReadInStreamAsync(Stream image)
        => _client.ReadInStreamAsync(image);

    public async Task<ReadOperationResult> GetReadResultAsync(Guid operationId)
        => await _client.GetReadResultAsync(operationId);
}
