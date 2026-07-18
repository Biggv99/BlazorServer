namespace OptimusPrime.Features.Contracts.Models;

// ==========================
// Health model
// ==========================
public class HealthModel {
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = default;
}

// ==========================
// Unhealthy model 
// ==========================
public static class HealthUnhealthyModel {

    public static List<HealthModel> GetUnhealthyList () {

        return [
            new() { Name = HealthServiceName.firebase, IsActive = false },
            new() { Name = HealthServiceName.postgres, IsActive = false },
            new() { Name = HealthServiceName.prometheus, IsActive = false },
            new() { Name = HealthServiceName.grafana, IsActive = false },
            new() { Name = HealthServiceName.redis, IsActive = false }
        ];
    }
}

// ==========================
// Health service names
// ==========================
public static class HealthServiceName {
    public const string firebase = "firebase";
    public const string postgres = "postgres";
    public const string prometheus = "prometheus";
    public const string grafana = "grafana";
    public const string redis = "redis";
}