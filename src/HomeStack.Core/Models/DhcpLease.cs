namespace HomeStack.Core.Models;

/// <summary>
/// Represents a DHCP lease in pfSense
/// </summary>
public class DhcpLease
{
    /// <summary>
    /// Gets or sets the IP address
    /// </summary>
    public string IpAddress { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the MAC address
    /// </summary>
    public string MacAddress { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the hostname
    /// </summary>
    public string Hostname { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the start time of the lease
    /// </summary>
    public DateTime? Start { get; set; }
    
    /// <summary>
    /// Gets or sets the end time of the lease
    /// </summary>
    public DateTime? End { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether this is a static mapping
    /// </summary>
    public bool IsStatic { get; set; }
    
    /// <summary>
    /// Gets or sets the online status
    /// </summary>
    public bool IsOnline { get; set; }
    
    /// <summary>
    /// Gets or sets the description
    /// </summary>
    public string Description { get; set; } = string.Empty;
}