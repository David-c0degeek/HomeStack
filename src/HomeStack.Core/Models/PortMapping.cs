namespace HomeStack.Core.Models;

/// <summary>
/// Represents a port mapping for a container
/// </summary>
public class PortMapping
{
    /// <summary>
    /// The container's internal port
    /// </summary>
    public int ContainerPort { get; set; }
    
    /// <summary>
    /// The host port that maps to the container port
    /// </summary>
    public int? HostPort { get; set; }
    
    /// <summary>
    /// The protocol (TCP, UDP)
    /// </summary>
    public string Protocol { get; set; } = "tcp";
    
    /// <summary>
    /// The host IP that is used for this mapping
    /// </summary>
    public string? HostIp { get; set; }
    
    /// <summary>
    /// Description or identifier for this port (e.g., "web", "api")
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Whether this port is publicly exposed
    /// </summary>
    public bool IsPublic { get; set; }
}