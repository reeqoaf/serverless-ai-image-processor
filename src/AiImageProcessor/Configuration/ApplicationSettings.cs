namespace AiImageProcessor.Configuration;

public class ApplicationSettings
{
    public string ComputerVisionEndpoint { get; set; } = string.Empty;
    public string CosmosDbConnectionString { get; set; } = string.Empty;
    public string StorageAccountName { get; set; } = string.Empty;
    public int MaxRetries { get; set; } = 3;
}
