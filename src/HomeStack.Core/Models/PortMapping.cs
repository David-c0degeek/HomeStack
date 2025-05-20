namespace HomeStack.Core.Models;

/// <summary>
/// Represents a port mapping for a container
/// </summary>
public class PortMapping
{
    /// <summary>
    /// Gets or sets the host port
    /// </summary>
    public int HostPort { get; set; }
    
    /// <summary>
    /// Gets or sets the container port
    /// </summary>
    public int ContainerPort { get; set; }
    
    /// <summary>
    /// Gets or sets the protocol (tcp, udp)
    /// </summary>
    public string Protocol { get; set; } = "tcp";
}