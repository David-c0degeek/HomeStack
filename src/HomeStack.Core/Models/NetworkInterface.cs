namespace HomeStack.Core.Models;

/// <summary>
/// Represents a network interface
/// </summary>
public class NetworkInterface
{
    /// <summary>
    /// Interface name (e.g., eth0, em0, LAN, WAN)
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Interface description
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Interface MAC address
    /// </summary>
    public string? MacAddress { get; set; }
    
    /// <summary>
    /// Interface IP address
    /// </summary>
    public string? IpAddress { get; set; }
    
    /// <summary>
    /// Interface subnet mask
    /// </summary>
    public string? SubnetMask { get; set; }
    
    /// <summary>
    /// Interface gateway address
    /// </summary>
    public string? Gateway { get; set; }
    
    /// <summary>
    /// Whether this interface has DHCP server enabled
    /// </summary>
    public bool HasDhcpServer { get; set; }
    
    /// <summary>
    /// Whether this interface is enabled
    /// </summary>
    public bool IsEnabled { get; set; }
    
    /// <summary>
    /// Whether this interface is a WAN (Internet-facing) interface
    /// </summary>
    public bool IsWan { get; set; }
    
    /// <summary>
    /// Maximum transmission unit (MTU) for this interface
    /// </summary>
    public int? Mtu { get; set; }
    
    /// <summary>
    /// Media type (e.g., "1000baseT full-duplex")
    /// </summary>
    public string? Media { get; set; }
    
    /// <summary>
    /// Status of the interface (up, down)
    /// </summary>
    public string? Status { get; set; }
}