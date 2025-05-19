namespace HomeStack.Core.Models;

/// <summary>
/// Represents information about a Docker container
/// </summary>
public class ContainerInfo
{
    /// <summary>
    /// Container ID
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Container name
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Container image name
    /// </summary>
    public string Image { get; set; } = string.Empty;
    
    /// <summary>
    /// Container IP address
    /// </summary>
    public string IpAddress { get; set; } = string.Empty;
    
    /// <summary>
    /// Container status
    /// </summary>
    public string Status { get; set; } = string.Empty;
    
    /// <summary>
    /// Port mappings for the container
    /// </summary>
    public List<PortMapping> PortMappings { get; set; } = new();
    
    /// <summary>
    /// Whether the container is running behind a VPN
    /// </summary>
    public bool IsVpnIsolated { get; set; }
    
    /// <summary>
    /// Container health status
    /// </summary>
    public ServiceHealth Health { get; set; } = ServiceHealth.Unknown;
    
    /// <summary>
    /// Container labels
    /// </summary>
    public Dictionary<string, string> Labels { get; set; } = new();
    
    /// <summary>
    /// Container environment variables
    /// </summary>
    public Dictionary<string, string> EnvironmentVariables { get; set; } = new();
}