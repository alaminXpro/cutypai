namespace cutypai.Models;

public class LipsyncData
{
    public List<MouthCue> MouthCues { get; set; } = new();
}

public class MouthCue
{
    public double Start { get; set; }
    public double End { get; set; }
    public string Value { get; set; } = string.Empty;
}

public class PhonemeTiming
{
    public string Phoneme { get; set; } = string.Empty;
    public double Start { get; set; }
    public double End { get; set; }
}
