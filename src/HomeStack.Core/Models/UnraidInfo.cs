namespace HomeStack.Core.Models;

/// <summary>
/// Represents information about an Unraid server
/// </summary>
public class UnraidInfo
{
    /// <summary>
    /// Gets or sets the hostname
    /// </summary>
    public string Hostname { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the IP address
    /// </summary>
    public string IpAddress { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the Unraid version
    /// </summary>
    public string Version { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the CPU model
    /// </summary>
    public string CpuModel { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the total memory
    /// </summary>
    public string TotalMemory { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets a value indicating whether Docker is enabled
    /// </summary>
    public bool DockerEnabled { get; set; }
    
    /// <summary>
    /// Gets or sets the system information
    /// </summary>
    public Dictionary<string, string> SystemInfo { get; set; } = new Dictionary<string, string>();
    
    /// <summary>
    /// Gets or sets the list of containers
    /// </summary>
    public List<ContainerInfo> Containers { get; set; } = new List<ContainerInfo>();
}