namespace HomeStack.Core.Models;

/// <summary>
/// Represents information about a Docker container
/// </summary>
public class ContainerInfo
{
    /// <summary>
    /// Gets or sets the container ID
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the container name
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the container image
    /// </summary>
    public string Image { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the container IP address
    /// </summary>
    public string IpAddress { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets a value indicating whether the container is running
    /// </summary>
    public bool IsRunning { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether the container is VPN isolated
    /// </summary>
    public bool IsVpnIsolated { get; set; }
    
    /// <summary>
    /// Gets or sets the container labels
    /// </summary>
    public Dictionary<string, string> Labels { get; set; } = new Dictionary<string, string>();
    
    /// <summary>
    /// Gets or sets the port mappings
    /// </summary>
    public List<PortMapping> Ports { get; set; } = new List<PortMapping>();
}