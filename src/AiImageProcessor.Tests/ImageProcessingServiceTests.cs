using Xunit;
using AiImageProcessor.Database.Entities;
using AiImageProcessor.Database.Repositories.Abstractions;
using AiImageProcessor.Services;
using AiImageProcessor.Services.Abstractions;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace AiImageProcessor.Tests;

public class ImageProcessingServiceTests
{
    private readonly Mock<IImageAnalysisService> _analysisMock = new();
    private readonly Mock<IImageAnalysisRepository> _repoMock = new();
    private readonly Mock<BlobServiceClient> _blobServiceMock = new();
    private readonly Mock<BlobContainerClient> _containerMock = new();
    private readonly Mock<BlobClient> _blobClientMock = new();

    private ImageProcessingService CreateService() =>
        new(_analysisMock.Object, _repoMock.Object, _blobServiceMock.Object,
            NullLogger<ImageProcessingService>.Instance);

    private void SetupBlobChain()
    {
        _blobServiceMock.Setup(b => b.GetBlobContainerClient("images")).Returns(_containerMock.Object);
        _containerMock.Setup(c => c.GetBlobClient(It.IsAny<string>())).Returns(_blobClientMock.Object);

        var blobPropertiesMock = BlobsModelFactory.BlobProperties(eTag: new ETag("\"etag123\""));
        var responseMock = Response.FromValue(blobPropertiesMock, Mock.Of<Response>());
        _blobClientMock.Setup(b => b.GetPropertiesAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseMock);
        _blobClientMock.Setup(b => b.Uri).Returns(new Uri("https://example.blob.core.windows.net/images/test.jpg"));
        _blobClientMock.Setup(b => b.Name).Returns("test.jpg");
        _blobClientMock.Setup(b => b.SetMetadataAsync(
                It.IsAny<IDictionary<string, string>>(),
                It.IsAny<BlobRequestConditions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(BlobsModelFactory.BlobInfo(new ETag("\"etag123\""), DateTimeOffset.UtcNow), Mock.Of<Response>()));
    }

    private void SetupHappyPath(ImageAnalysisResult? result = null)
    {
        result ??= new ImageAnalysisResult { Description = "a cat", Dimensions = "100x100" };
        _analysisMock.Setup(a => a.AnalyzeImageAsync(It.IsAny<Stream>(), It.IsAny<string>()))
            .ReturnsAsync(result);
        _repoMock.Setup(r => r.StoreAnalysisAsync(
                It.IsAny<string>(), It.IsAny<ImageAnalysisResult>(),
                It.IsAny<ImageMetadata>(), It.IsAny<StorageInfo>()))
            .ReturnsAsync(new ImageAnalysisDocument { Id = "docid", FileName = "test.jpg" });
    }

    [Fact]
    public async Task ProcessImage_StoresDocumentInDatabase_OnSuccess()
    {
        SetupBlobChain();
        SetupHappyPath();
        var service = CreateService();

        await service.ProcessImageAsync(new MemoryStream([1, 2, 3]), "test.jpg");

        _repoMock.Verify(r => r.StoreAnalysisAsync(
            "test.jpg",
            It.IsAny<ImageAnalysisResult>(),
            It.IsAny<ImageMetadata>(),
            It.IsAny<StorageInfo>()), Times.Once);
    }

    [Fact]
    public async Task ProcessImage_UpdatesBlobMetadata_WithCompletedStatus_OnSuccess()
    {
        SetupBlobChain();
        SetupHappyPath();
        var service = CreateService();

        await service.ProcessImageAsync(new MemoryStream([1, 2, 3]), "test.jpg");

        _blobClientMock.Verify(b => b.SetMetadataAsync(
            It.Is<IDictionary<string, string>>(m => m["processing_status"] == "completed"),
            It.IsAny<BlobRequestConditions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessImage_UpdatesBlobMetadata_WithFailedStatus_AndRethrows_OnException()
    {
        SetupBlobChain();
        _analysisMock.Setup(a => a.AnalyzeImageAsync(It.IsAny<Stream>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("CV error"));

        var service = CreateService();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ProcessImageAsync(new MemoryStream([1, 2, 3]), "test.jpg"));

        _blobClientMock.Verify(b => b.SetMetadataAsync(
            It.Is<IDictionary<string, string>>(m => m["processing_status"] == "failed"),
            It.IsAny<BlobRequestConditions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessImage_ProcessingTimeMs_IsNonZero()
    {
        SetupBlobChain();
        ImageMetadata? capturedMetadata = null;

        _analysisMock.Setup(a => a.AnalyzeImageAsync(It.IsAny<Stream>(), It.IsAny<string>()))
            .Returns(async () =>
            {
                await Task.Delay(20);
                return new ImageAnalysisResult { Dimensions = "100x100" };
            });

        _repoMock.Setup(r => r.StoreAnalysisAsync(
                It.IsAny<string>(), It.IsAny<ImageAnalysisResult>(),
                It.IsAny<ImageMetadata>(), It.IsAny<StorageInfo>()))
            .Callback<string, ImageAnalysisResult, ImageMetadata, StorageInfo>((_, _, m, _) => capturedMetadata = m)
            .ReturnsAsync(new ImageAnalysisDocument { Id = "docid" });

        var service = CreateService();
        await service.ProcessImageAsync(new MemoryStream([1, 2, 3]), "test.jpg");

        Assert.NotNull(capturedMetadata);
        Assert.True(capturedMetadata.ProcessingTimeMs >= 0);
    }

    [Fact]
    public async Task ProcessImage_FileSize_CapturedBeforeStreamConsumption()
    {
        SetupBlobChain();
        ImageMetadata? capturedMetadata = null;

        _analysisMock.Setup(a => a.AnalyzeImageAsync(It.IsAny<Stream>(), It.IsAny<string>()))
            .Returns(async (Stream s, string _) =>
            {
                // Simulate consuming the stream
                await s.CopyToAsync(Stream.Null);
                return new ImageAnalysisResult { Dimensions = "100x100" };
            });

        _repoMock.Setup(r => r.StoreAnalysisAsync(
                It.IsAny<string>(), It.IsAny<ImageAnalysisResult>(),
                It.IsAny<ImageMetadata>(), It.IsAny<StorageInfo>()))
            .Callback<string, ImageAnalysisResult, ImageMetadata, StorageInfo>((_, _, m, _) => capturedMetadata = m)
            .ReturnsAsync(new ImageAnalysisDocument { Id = "docid" });

        var stream = new MemoryStream([10, 20, 30, 40, 50]);
        var service = CreateService();
        await service.ProcessImageAsync(stream, "test.jpg");

        Assert.NotNull(capturedMetadata);
        Assert.Equal(5, capturedMetadata.FileSize);
    }

    [Fact]
    public async Task ProcessImage_Dimensions_MappedFromAnalysisResult()
    {
        SetupBlobChain();
        ImageMetadata? capturedMetadata = null;

        _analysisMock.Setup(a => a.AnalyzeImageAsync(It.IsAny<Stream>(), It.IsAny<string>()))
            .ReturnsAsync(new ImageAnalysisResult { Dimensions = "1920x1080" });

        _repoMock.Setup(r => r.StoreAnalysisAsync(
                It.IsAny<string>(), It.IsAny<ImageAnalysisResult>(),
                It.IsAny<ImageMetadata>(), It.IsAny<StorageInfo>()))
            .Callback<string, ImageAnalysisResult, ImageMetadata, StorageInfo>((_, _, m, _) => capturedMetadata = m)
            .ReturnsAsync(new ImageAnalysisDocument { Id = "docid" });

        var service = CreateService();
        await service.ProcessImageAsync(new MemoryStream([1, 2, 3]), "test.jpg");

        Assert.Equal("1920x1080", capturedMetadata?.Dimensions);
    }
}
