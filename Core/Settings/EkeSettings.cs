namespace OptimusPrime.Core.Settings;

public class EkeSettings {
    public string Password { get; set; } = string.Empty;
    public int Iterations { get; set; } = 0;
    public int KeyLength { get; set; } = 0;
    public string ServerKeyB64 { get; set; } = string.Empty;
}