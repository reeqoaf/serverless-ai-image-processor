using Xunit;
using AiImageProcessor.Configuration;
using AiImageProcessor.Database.Entities;
using AiImageProcessor.Database.Repositories;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using System.Net;

namespace AiImageProcessor.Tests;

public class CosmosImageAnalysisRepositoryTests
{
    private static IOptions<ApplicationSettings> DefaultSettings => Options.Create(new ApplicationSettings { MaxRetries = 1 });

    private static CosmosImageAnalysisRepository CreateRepository(Mock<Container> containerMock)
        => new(containerMock.Object, DefaultSettings, NullLogger<CosmosImageAnalysisRepository>.Instance);

    private static (ImageAnalysisResult, ImageMetadata, StorageInfo) BuildTestData() =>
    (
        new ImageAnalysisResult { Description = "test" },
        new ImageMetadata { FileSize = 100 },
        new StorageInfo { Container = "images", BlobName = "test.jpg" }
    );

    private static Mock<ItemResponse<ImageAnalysisDocument>> BuildItemResponse(ImageAnalysisDocument doc)
    {
        var responseMock = new Mock<ItemResponse<ImageAnalysisDocument>>();
        responseMock.Setup(r => r.Resource).Returns(doc);
        return responseMock;
    }

    [Fact]
    public async Task StoreAnalysis_CallsUpsertItemAsync_NotCreate()
    {
        var containerMock = new Mock<Container>();
        var (analysis, metadata, storage) = BuildTestData();

        containerMock
            .Setup(c => c.UpsertItemAsync(
                It.IsAny<ImageAnalysisDocument>(),
                It.IsAny<PartitionKey>(),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ImageAnalysisDocument doc, PartitionKey _, ItemRequestOptions _, CancellationToken _) =>
                BuildItemResponse(doc).Object);

        var repo = CreateRepository(containerMock);
        await repo.StoreAnalysisAsync("photo.jpg", analysis, metadata, storage);

        containerMock.Verify(
            c => c.UpsertItemAsync(
                It.IsAny<ImageAnalysisDocument>(),
                It.IsAny<PartitionKey>(),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        containerMock.Verify(
            c => c.CreateItemAsync(
                It.IsAny<ImageAnalysisDocument>(),
                It.IsAny<PartitionKey>(),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task StoreAnalysis_SameFileName_ProducesSameDocumentId()
    {
        var containerMock = new Mock<Container>();
        var capturedIds = new List<string>();

        containerMock
            .Setup(c => c.UpsertItemAsync(
                It.IsAny<ImageAnalysisDocument>(),
                It.IsAny<PartitionKey>(),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback<ImageAnalysisDocument, PartitionKey?, ItemRequestOptions, CancellationToken>(
                (doc, _, _, _) => capturedIds.Add(doc.Id))
            .ReturnsAsync((ImageAnalysisDocument doc, PartitionKey _, ItemRequestOptions _, CancellationToken _) =>
                BuildItemResponse(doc).Object);

        var repo = CreateRepository(containerMock);
        var (analysis, metadata, storage) = BuildTestData();

        await repo.StoreAnalysisAsync("photo.jpg", analysis, metadata, storage);
        await repo.StoreAnalysisAsync("photo.jpg", analysis, metadata, storage);

        Assert.Equal(2, capturedIds.Count);
        Assert.Equal(capturedIds[0], capturedIds[1]);
    }

    [Fact]
    public async Task StoreAnalysis_DifferentFileNames_ProduceDifferentIds()
    {
        var containerMock = new Mock<Container>();
        var capturedIds = new List<string>();

        containerMock
            .Setup(c => c.UpsertItemAsync(
                It.IsAny<ImageAnalysisDocument>(),
                It.IsAny<PartitionKey>(),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback<ImageAnalysisDocument, PartitionKey?, ItemRequestOptions, CancellationToken>(
                (doc, _, _, _) => capturedIds.Add(doc.Id))
            .ReturnsAsync((ImageAnalysisDocument doc, PartitionKey _, ItemRequestOptions _, CancellationToken _) =>
                BuildItemResponse(doc).Object);

        var repo = CreateRepository(containerMock);
        var (analysis, metadata, storage) = BuildTestData();

        await repo.StoreAnalysisAsync("photo1.jpg", analysis, metadata, storage);
        await repo.StoreAnalysisAsync("photo2.jpg", analysis, metadata, storage);

        Assert.NotEqual(capturedIds[0], capturedIds[1]);
    }

    [Fact]
    public async Task GetById_ReturnsNull_On404()
    {
        var containerMock = new Mock<Container>();
        containerMock
            .Setup(c => c.ReadItemAsync<ImageAnalysisDocument>(
                It.IsAny<string>(),
                It.IsAny<PartitionKey>(),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new CosmosException("Not Found", HttpStatusCode.NotFound, 0, string.Empty, 0));

        var repo = CreateRepository(containerMock);
        var result = await repo.GetByIdAsync("nonexistent-id");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetById_ReturnsDocument_WhenFound()
    {
        var containerMock = new Mock<Container>();
        var expected = new ImageAnalysisDocument { Id = "abc123", FileName = "photo.jpg" };

        var responseMock = BuildItemResponse(expected);
        containerMock
            .Setup(c => c.ReadItemAsync<ImageAnalysisDocument>(
                It.IsAny<string>(),
                It.IsAny<PartitionKey>(),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseMock.Object);

        var repo = CreateRepository(containerMock);
        var result = await repo.GetByIdAsync("abc123");

        Assert.NotNull(result);
        Assert.Equal("photo.jpg", result.FileName);
    }

    [Fact]
    public void GenerateDocumentId_IsDeterministic()
    {
        var id1 = CosmosImageAnalysisRepository.GenerateDocumentId("Photo.JPG");
        var id2 = CosmosImageAnalysisRepository.GenerateDocumentId("Photo.JPG");
        Assert.Equal(id1, id2);
    }

    [Fact]
    public void GenerateDocumentId_IsCaseInsensitive()
    {
        var id1 = CosmosImageAnalysisRepository.GenerateDocumentId("photo.jpg");
        var id2 = CosmosImageAnalysisRepository.GenerateDocumentId("PHOTO.JPG");
        Assert.Equal(id1, id2);
    }
}
