using Newtonsoft.Json;

namespace AiImageProcessor.Database.Entities;

public class ImageAnalysisDocument
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("fileName")]
    public string FileName { get; set; } = string.Empty;

    [JsonProperty("processedAt")]
    public DateTime ProcessedAt { get; set; }

    [JsonProperty("status")]
    public string Status { get; set; } = string.Empty;

    [JsonProperty("analysis")]
    public ImageAnalysisResult Analysis { get; set; } = new();

    [JsonProperty("metadata")]
    public ImageMetadata Metadata { get; set; } = new();

    [JsonProperty("storage")]
    public StorageInfo Storage { get; set; } = new();

    [JsonProperty("partitionKey")]
    public string PartitionKey { get; set; } = string.Empty;
}
