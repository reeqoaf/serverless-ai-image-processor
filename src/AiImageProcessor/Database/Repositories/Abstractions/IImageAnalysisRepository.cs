using AiImageProcessor.Database.Entities;

namespace AiImageProcessor.Database.Repositories.Abstractions;

public interface IImageAnalysisRepository
{
    Task<ImageAnalysisDocument> StoreAnalysisAsync(string fileName, ImageAnalysisResult analysis, ImageMetadata metadata, StorageInfo storage);
    Task<ImageAnalysisDocument?> GetByIdAsync(string id);
    Task<List<ImageAnalysisDocument>> GetByDateAsync(DateTime date);
    Task<List<ImageAnalysisDocument>> GetByTagAsync(string tag);
    Task<List<ImageAnalysisDocument>> GetByStatusAsync(string status);
    Task<List<ImageAnalysisDocument>> GetFailedImagesAsync();
    Task UpdateStatusAsync(string id, string status, string? errorMessage = null);
    Task DeleteAsync(string id);
}
