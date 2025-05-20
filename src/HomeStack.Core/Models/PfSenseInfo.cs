namespace HomeStack.Core.Models;

/// <summary>
/// Represents information about a pfSense router
/// </summary>
public class PfSenseInfo
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
    /// Gets or sets the pfSense version
    /// </summary>
    public string Version { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the list of DNS host overrides
    /// </summary>
    public List<DnsHostOverride> DnsHostOverrides { get; set; } = new List<DnsHostOverride>();
    
    /// <summary>
    /// Gets or sets the list of DHCP leases
    /// </summary>
    public List<DhcpLease> DhcpLeases { get; set; } = new List<DhcpLease>();
    
    /// <summary>
    /// Gets or sets the list of network interfaces
    /// </summary>
    public List<NetworkInterface> Interfaces { get; set; } = new List<NetworkInterface>();
}