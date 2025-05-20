namespace HomeStack.Core.Models;

/// <summary>
/// Represents the health status of a service
/// </summary>
public class ServiceHealth
{
    /// <summary>
    /// Gets or sets the service name
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the service status
    /// </summary>
    public ServiceStatus Status { get; set; }
    
    /// <summary>
    /// Gets or sets the error message if the service is not healthy
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Gets or sets the last check time
    /// </summary>
    public DateTime LastCheckTime { get; set; } = DateTime.Now;
}

/// <summary>
/// Represents the status of a service
/// </summary>
public enum ServiceStatus
{
    /// <summary>
    /// Unknown status
    /// </summary>
    Unknown = 0,
    
    /// <summary>
    /// Healthy status
    /// </summary>
    Healthy = 1,
    
    /// <summary>
    /// Unhealthy status
    /// </summary>
    Unhealthy = 2,
    
    /// <summary>
    /// Degraded status
    /// </summary>
    Degraded = 3
}