namespace AiImageProcessor.Configuration;

public class ApplicationSettings
{
    public string ComputerVisionEndpoint { get; set; } = string.Empty;
    public string ComputerVisionApiKey { get; set; } = string.Empty;
    public string CosmosDbConnectionString { get; set; } = string.Empty;
    public string StorageAccountName { get; set; } = string.Empty;
    public string AzureClientId { get; set; } = string.Empty;
    public string AzureClientSecret { get; set; } = string.Empty;
    public string AzureTenantId { get; set; } = string.Empty;
    public int MaxRetries { get; set; } = 3;
}
