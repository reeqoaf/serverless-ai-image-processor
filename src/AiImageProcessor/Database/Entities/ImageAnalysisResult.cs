using Newtonsoft.Json;

namespace AiImageProcessor.Database.Entities;

public class ImageAnalysisResult
{
    [JsonProperty("description")]
    public string Description { get; set; } = string.Empty;

    [JsonProperty("tags")]
    public List<string> Tags { get; set; } = new();

    [JsonProperty("objects")]
    public List<DetectedObject> Objects { get; set; } = new();

    [JsonProperty("faces")]
    public List<DetectedFace> Faces { get; set; } = new();

    [JsonProperty("text")]
    public List<string> Text { get; set; } = new();

    [JsonProperty("colors")]
    public ColorAnalysis Colors { get; set; } = new();

    [JsonProperty("adultContent")]
    public AdultContentAnalysis AdultContent { get; set; } = new();

    [JsonProperty("imageType")]
    public string ImageType { get; set; } = string.Empty;

    [JsonProperty("dimensions")]
    public string Dimensions { get; set; } = string.Empty;
}

public class DetectedObject
{
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("confidence")]
    public double Confidence { get; set; }

    [JsonProperty("boundingBox")]
    public BoundingBox? BoundingBox { get; set; }
}

public class DetectedFace
{
    [JsonProperty("age")]
    public int? Age { get; set; }

    [JsonProperty("gender")]
    public string Gender { get; set; } = string.Empty;

    [JsonProperty("emotion")]
    public string Emotion { get; set; } = string.Empty;

    [JsonProperty("confidence", NullValueHandling = NullValueHandling.Ignore)]
    public double? Confidence { get; set; }

    [JsonProperty("boundingBox")]
    public BoundingBox? BoundingBox { get; set; }
}

public class BoundingBox
{
    [JsonProperty("x")]
    public int X { get; set; }

    [JsonProperty("y")]
    public int Y { get; set; }

    [JsonProperty("width")]
    public int Width { get; set; }

    [JsonProperty("height")]
    public int Height { get; set; }
}

public class ColorAnalysis
{
    [JsonProperty("dominant")]
    public string Dominant { get; set; } = string.Empty;

    [JsonProperty("accent")]
    public string Accent { get; set; } = string.Empty;

    [JsonProperty("isBWImg")]
    public bool IsBlackAndWhite { get; set; }
}

public class AdultContentAnalysis
{
    [JsonProperty("isAdultContent")]
    public bool IsAdultContent { get; set; }

    [JsonProperty("adultScore")]
    public double AdultScore { get; set; }

    [JsonProperty("racyScore")]
    public double RacyScore { get; set; }
}
