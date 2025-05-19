namespace HomeStack.Core.Models;

/// <summary>
/// Represents a DHCP lease in pfSense
/// </summary>
public class DhcpLease
{
    /// <summary>
    /// Lease IP address
    /// </summary>
    public string IpAddress { get; set; } = string.Empty;
    
    /// <summary>
    /// MAC address of the device
    /// </summary>
    public string MacAddress { get; set; } = string.Empty;
    
    /// <summary>
    /// Hostname of the device
    /// </summary>
    public string? Hostname { get; set; }
    
    /// <summary>
    /// Description for this lease
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Start time of the lease
    /// </summary>
    public DateTime? StartTime { get; set; }
    
    /// <summary>
    /// End time of the lease
    /// </summary>
    public DateTime? EndTime { get; set; }
    
    /// <summary>
    /// The online status of the device
    /// </summary>
    public bool IsOnline { get; set; }
    
    /// <summary>
    /// Whether the lease is static (reserved)
    /// </summary>
    public bool IsStatic { get; set; }
    
    /// <summary>
    /// The network interface this lease is associated with
    /// </summary>
    public string? Interface { get; set; }
}