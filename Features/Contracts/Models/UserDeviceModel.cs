using System.ComponentModel.DataAnnotations;

namespace OptimusPrime.Features.Contracts.Models;

public class UserDeviceModel {
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }

    public string DeviceId { get; set; } = string.Empty;
    public string PublicKey { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
}