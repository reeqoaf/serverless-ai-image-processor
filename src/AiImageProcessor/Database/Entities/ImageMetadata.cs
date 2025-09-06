using Newtonsoft.Json;

namespace AiImageProcessor.Database.Entities;

public class ImageMetadata
{
    [JsonProperty("fileSize")]
    public long FileSize { get; set; }

    [JsonProperty("format")]
    public string Format { get; set; } = string.Empty;

    [JsonProperty("dimensions")]
    public string Dimensions { get; set; } = string.Empty;

    [JsonProperty("retryCount")]
    public int RetryCount { get; set; }

    [JsonProperty("processingTimeMs")]
    public long ProcessingTimeMs { get; set; }

    [JsonProperty("aiServiceUsed")]
    public string AiServiceUsed { get; set; } = string.Empty;

    [JsonProperty("version")]
    public string Version { get; set; } = "1.0";
}

public class StorageInfo
{
    [JsonProperty("container")]
    public string Container { get; set; } = string.Empty;

    [JsonProperty("blobName")]
    public string BlobName { get; set; } = string.Empty;

    [JsonProperty("url")]
    public string Url { get; set; } = string.Empty;

    [JsonProperty("etag")]
    public string ETag { get; set; } = string.Empty;
}
