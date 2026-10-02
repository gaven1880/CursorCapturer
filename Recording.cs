using System.Text.Json.Serialization;

namespace CursorCapturer;

public class Keyframe
{
  public int Frame { get; set; }

  public int X { get; set; }

  public int Y { get; set; }
}

public class Recording
{
  // Machine specific, so it is never written to the saved file.
  [JsonIgnore]
  public string Video { get; set; } = string.Empty;

  public double Fps { get; set; }

  public List<Keyframe> Keyframes { get; set; } = new List<Keyframe>();
}
