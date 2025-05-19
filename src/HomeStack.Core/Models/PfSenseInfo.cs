namespace HomeStack.Core.Models;

/// <summary>
/// Represents system information for a pfSense router
/// </summary>
public class PfSenseInfo
{
    /// <summary>
    /// Hostname of the pfSense router
    /// </summary>
    public string Hostname { get; set; } = string.Empty;
    
    /// <summary>
    /// Version of pfSense
    /// </summary>
    public string Version { get; set; } = string.Empty;
    
    /// <summary>
    /// Platform architecture (e.g., amd64)
    /// </summary>
    public string? Platform { get; set; }
    
    /// <summary>
    /// Domain name configured in pfSense
    /// </summary>
    public string? Domain { get; set; }
    
    /// <summary>
    /// DNS servers configured in pfSense
    /// </summary>
    public List<string> DnsServers { get; set; } = new();
    
    /// <summary>
    /// Time when the pfSense system was last started
    /// </summary>
    public DateTime? BootTime { get; set; }
    
    /// <summary>
    /// Current system uptime
    /// </summary>
    public TimeSpan? Uptime { get; set; }
    
    /// <summary>
    /// Current system load averages (1, 5, 15 minutes)
    /// </summary>
    public string? LoadAverages { get; set; }
    
    /// <summary>
    /// CPU temperature (if available)
    /// </summary>
    public string? CpuTemperature { get; set; }
    
    /// <summary>
    /// Memory usage information
    /// </summary>
    public string? MemoryUsage { get; set; }
    
    /// <summary>
    /// Host IP address (used for connection)
    /// </summary>
    public string? HostIpAddress { get; set; }
}