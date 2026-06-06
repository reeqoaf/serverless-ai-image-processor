using Xunit;
using AiImageProcessor.Configuration;
using AiImageProcessor.Services;
using AiImageProcessor.Services.Abstractions;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace AiImageProcessor.Tests;

public class ComputerVisionAnalysisServiceTests
{
    private readonly Mock<IComputerVisionWrapper> _wrapperMock = new();

    private static IOptions<ApplicationSettings> Settings(int maxRetries = 3) =>
        Options.Create(new ApplicationSettings { MaxRetries = maxRetries });

    private ComputerVisionAnalysisService CreateService(int maxRetries = 3) =>
        new(_wrapperMock.Object, Settings(maxRetries), NullLogger<ComputerVisionAnalysisService>.Instance);

    private void SetupReadApiSuccess(string? text = null)
    {
        var operationId = Guid.NewGuid();
        var headers = new ReadInStreamHeaders
        {
            OperationLocation = $"https://example.cognitive.microsoft.com/vision/v3.2/read/analyzeResults/{operationId}"
        };

        _wrapperMock.Setup(w => w.ReadInStreamAsync(It.IsAny<Stream>()))
            .ReturnsAsync(headers);

        _wrapperMock.Setup(w => w.GetReadResultAsync(It.IsAny<Guid>()))
            .ReturnsAsync(BuildReadOperationResult(text));
    }

    // AnalyzeResult property type (ReadAnalyzeResult) isn't directly referenceable from the test
    // assembly due to transitive dependency resolution. Use reflection to build the nested object.
    private static ReadOperationResult BuildReadOperationResult(string? text)
    {
        var result = new ReadOperationResult { Status = OperationStatusCodes.Succeeded };

        var arType = typeof(ReadOperationResult).GetProperty("AnalyzeResult")!.PropertyType;
        var analyzeResult = Activator.CreateInstance(arType)!;

        if (!string.IsNullOrEmpty(text))
        {
            var line = new Line { Text = text };
            var readResult = new ReadResult { Lines = [line] };
            arType.GetProperty("ReadResults")!.SetValue(analyzeResult, new List<ReadResult> { readResult });
        }

        typeof(ReadOperationResult).GetProperty("AnalyzeResult")!.SetValue(result, analyzeResult);
        return result;
    }

    private static ImageAnalysis BuildAnalysis(
        IList<ImageTag>? tags = null,
        IList<DetectedObject>? objects = null,
        IList<FaceDescription>? faces = null,
        ImageMetadata? imageMetadata = null,
        AdultInfo? adult = null,
        ColorInfo? color = null)
    {
        return new ImageAnalysis
        {
            Tags = tags ?? [],
            Objects = objects ?? [],
            Faces = faces ?? [],
            Metadata = imageMetadata ?? new ImageMetadata { Width = 800, Height = 600 },
            Adult = adult,
            Color = color,
            Description = new ImageDescriptionDetails
            {
                Captions = [new ImageCaption { Text = "a test image", Confidence = 0.9 }],
            },
        };
    }

    [Fact]
    public async Task Analyze_MapsTags_Correctly()
    {
        var analysis = BuildAnalysis(tags: [new ImageTag { Name = "cat" }, new ImageTag { Name = "animal" }]);
        _wrapperMock.Setup(w => w.AnalyzeImageInStreamAsync(It.IsAny<Stream>(), It.IsAny<IList<VisualFeatureTypes?>>()))
            .ReturnsAsync(analysis);
        SetupReadApiSuccess();

        var result = await CreateService().AnalyzeImageAsync(new MemoryStream([1, 2, 3]), "test.jpg");

        Assert.Contains("cat", result.Tags);
        Assert.Contains("animal", result.Tags);
    }

    [Fact]
    public async Task Analyze_MapsObjects_WithBoundingBox()
    {
        var analysis = BuildAnalysis(objects:
        [
            new DetectedObject(
                rectangle: new BoundingRect(10, 20, 100, 50),
                objectProperty: "car",
                confidence: 0.95,
                parent: null)
        ]);
        _wrapperMock.Setup(w => w.AnalyzeImageInStreamAsync(It.IsAny<Stream>(), It.IsAny<IList<VisualFeatureTypes?>>()))
            .ReturnsAsync(analysis);
        SetupReadApiSuccess();

        var result = await CreateService().AnalyzeImageAsync(new MemoryStream([1, 2, 3]), "test.jpg");

        Assert.Single(result.Objects);
        Assert.Equal("car", result.Objects[0].Name);
        Assert.Equal(0.95, result.Objects[0].Confidence);
        Assert.NotNull(result.Objects[0].BoundingBox);
        Assert.Equal(10, result.Objects[0].BoundingBox!.X);
    }

    [Fact]
    public async Task Analyze_FaceConfidence_IsNull()
    {
        var analysis = BuildAnalysis(faces:
        [
            new FaceDescription { Age = 30, Gender = Gender.Male }
        ]);
        _wrapperMock.Setup(w => w.AnalyzeImageInStreamAsync(It.IsAny<Stream>(), It.IsAny<IList<VisualFeatureTypes?>>()))
            .ReturnsAsync(analysis);
        SetupReadApiSuccess();

        var result = await CreateService().AnalyzeImageAsync(new MemoryStream([1, 2, 3]), "test.jpg");

        Assert.Single(result.Faces);
        Assert.Null(result.Faces[0].Confidence);
    }

    [Fact]
    public async Task Analyze_ExtractsText_ViaReadApi()
    {
        _wrapperMock.Setup(w => w.AnalyzeImageInStreamAsync(It.IsAny<Stream>(), It.IsAny<IList<VisualFeatureTypes?>>()))
            .ReturnsAsync(BuildAnalysis());
        SetupReadApiSuccess("Hello World");

        var result = await CreateService().AnalyzeImageAsync(new MemoryStream([1, 2, 3]), "test.jpg");

        Assert.Contains("Hello World", result.Text);
    }

    [Fact]
    public async Task Analyze_ReturnsEmptyText_WhenReadApiFails()
    {
        _wrapperMock.Setup(w => w.AnalyzeImageInStreamAsync(It.IsAny<Stream>(), It.IsAny<IList<VisualFeatureTypes?>>()))
            .ReturnsAsync(BuildAnalysis());
        _wrapperMock.Setup(w => w.ReadInStreamAsync(It.IsAny<Stream>()))
            .ThrowsAsync(new ComputerVisionErrorResponseException("OCR failed"));

        var result = await CreateService().AnalyzeImageAsync(new MemoryStream([1, 2, 3]), "test.jpg");

        Assert.Empty(result.Text);
    }

    [Fact]
    public async Task Analyze_MapsDimensions_FromMetadata()
    {
        var analysis = BuildAnalysis(imageMetadata: new ImageMetadata { Width = 1920, Height = 1080 });
        _wrapperMock.Setup(w => w.AnalyzeImageInStreamAsync(It.IsAny<Stream>(), It.IsAny<IList<VisualFeatureTypes?>>()))
            .ReturnsAsync(analysis);
        SetupReadApiSuccess();

        var result = await CreateService().AnalyzeImageAsync(new MemoryStream([1, 2, 3]), "test.jpg");

        Assert.Equal("1920x1080", result.Dimensions);
    }

    [Fact]
    public async Task Analyze_RetriesOnTransientFailure_ThenSucceeds()
    {
        var callCount = 0;
        _wrapperMock.Setup(w => w.AnalyzeImageInStreamAsync(It.IsAny<Stream>(), It.IsAny<IList<VisualFeatureTypes?>>()))
            .Returns(() =>
            {
                callCount++;
                if (callCount == 1)
                    throw new ComputerVisionErrorResponseException("transient");
                return Task.FromResult(BuildAnalysis());
            });
        SetupReadApiSuccess();

        var result = await CreateService().AnalyzeImageAsync(new MemoryStream([1, 2, 3]), "test.jpg");

        Assert.NotNull(result);
        Assert.Equal(2, callCount);
    }
}
