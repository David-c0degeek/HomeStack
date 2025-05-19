namespace HomeStack.Core.Models;

/// <summary>
/// Represents the health status of a service
/// </summary>
public enum ServiceHealth
{
    /// <summary>
    /// The service health status is unknown
    /// </summary>
    Unknown = 0,
    
    /// <summary>
    /// The service is healthy
    /// </summary>
    Healthy = 1,
    
    /// <summary>
    /// The service is unhealthy
    /// </summary>
    Unhealthy = 2,
    
    /// <summary>
    /// The service is starting
    /// </summary>
    Starting = 3,
    
    /// <summary>
    /// The service is stopped
    /// </summary>
    Stopped = 4
}