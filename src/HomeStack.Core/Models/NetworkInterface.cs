namespace HomeStack.Core.Models;

/// <summary>
/// Represents a network interface
/// </summary>
public class NetworkInterface
{
    /// <summary>
    /// Gets or sets the interface name
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the interface description
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the IP address of the interface
    /// </summary>
    public string IpAddress { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the subnet mask
    /// </summary>
    public string SubnetMask { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the MAC address
    /// </summary>
    public string MacAddress { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets a value indicating whether the interface is enabled
    /// </summary>
    public bool IsEnabled { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether the interface is up
    /// </summary>
    public bool IsUp { get; set; }
}