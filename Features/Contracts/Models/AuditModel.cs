using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace OptimusPrime.Features.Contracts.Models;

public class AuditModel {
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public Guid? UserId { get; set; } = null;
    public string UserEmail { get; set; } = string.Empty;


    public string ActionType { get; set; } = string.Empty; 
    public string EntityName { get; set; } = string.Empty; 
    public string EntityId { get; set; } = string.Empty;  


    [Column(TypeName = "jsonb")]
    public string OldValues { get; set; } = "{}";

    [Column(TypeName = "jsonb")]
    public string NewValues { get; set; } = "{}";

    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;

    public bool IsSuccess { get; set; } = false;
    public string Message { get; set; } = string.Empty;
}