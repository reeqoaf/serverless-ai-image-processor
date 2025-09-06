using AiImageProcessor.Database.Entities;

namespace AiImageProcessor.Services.Abstractions;

public interface IImageProcessingService
{
    Task ProcessImageAsync(Stream imageStream, string fileName);
}
