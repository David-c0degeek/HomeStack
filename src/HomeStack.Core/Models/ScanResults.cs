namespace HomeStack.Core.Models;

/// <summary>
/// Represents the results of a network scan
/// </summary>
public class ScanResults
{
    /// <summary>
    /// Gets or sets the list of discovered containers
    /// </summary>
    public List<ContainerInfo> Containers { get; set; } = new List<ContainerInfo>();
    
    /// <summary>
    /// Gets or sets the Unraid server information
    /// </summary>
    public UnraidInfo? UnraidInfo { get; set; }
    
    /// <summary>
    /// Gets or sets the pfSense router information
    /// </summary>
    public PfSenseInfo? PfSenseInfo { get; set; }
    
    /// <summary>
    /// Gets or sets the generated proxy configurations
    /// </summary>
    public List<ProxyConfig> ProxyConfigs { get; set; } = new List<ProxyConfig>();
    
    /// <summary>
    /// Gets or sets the timestamp of the scan
    /// </summary>
    public DateTime ScanTimestamp { get; set; } = DateTime.Now;
}