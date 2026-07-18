namespace OptimusPrime.Features.Contracts.Commands.Shared;

public class BaseTrackedCommand {
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public long? DeviceTimestamp { get; set; } = null;
    public string DeviceSignature { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
}