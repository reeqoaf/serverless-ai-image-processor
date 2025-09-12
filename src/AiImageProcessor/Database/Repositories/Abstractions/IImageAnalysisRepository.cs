using AiImageProcessor.Database.Entities;

namespace AiImageProcessor.Database.Repositories.Abstractions;

public interface IImageAnalysisRepository
{
    Task<ImageAnalysisDocument> StoreAnalysisAsync(string fileName, ImageAnalysisResult analysis, ImageMetadata metadata, StorageInfo storage);
    Task<ImageAnalysisDocument?> GetByIdAsync(string id);
}
