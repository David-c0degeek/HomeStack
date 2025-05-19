using System.Text.Json.Serialization;

namespace HomeStack.Core.Models;

/// <summary>
/// Represents the combined results of a scan operation
/// </summary>
public class ScanResults
{
    /// <summary>
    /// Time when the scan was performed
    /// </summary>
    public DateTime ScanTime { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Docker containers found during the scan
    /// </summary>
    public List<ContainerInfo> Containers { get; set; } = new();
    
    /// <summary>
    /// Unraid server information if scanned
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public UnraidInfo? UnraidInfo { get; set; }
    
    /// <summary>
    /// pfSense router information if scanned
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public PfSenseInfo? PfSenseInfo { get; set; }
    
    /// <summary>
    /// DNS host overrides found in pfSense
    /// </summary>
    public List<DnsHostOverride> DnsHostOverrides { get; set; } = new();
    
    /// <summary>
    /// DHCP leases found in pfSense
    /// </summary>
    public List<DhcpLease> DhcpLeases { get; set; } = new();
    
    /// <summary>
    /// Network interfaces found in pfSense
    /// </summary>
    public List<NetworkInterface> NetworkInterfaces { get; set; } = new();
    
    /// <summary>
    /// Generated proxy configurations
    /// </summary>
    public List<ProxyConfig> ProxyConfigs { get; set; } = new();
    
    /// <summary>
    /// Docker host used for scanning
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DockerHost { get; set; }
    
    /// <summary>
    /// Unraid host used for scanning
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? UnraidHost { get; set; }
    
    /// <summary>
    /// pfSense host used for scanning
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PfSenseHost { get; set; }
    
    /// <summary>
    /// Any errors encountered during scanning
    /// </summary>
    public List<string> Errors { get; set; } = new();
    
    /// <summary>
    /// Any warnings encountered during scanning
    /// </summary>
    public List<string> Warnings { get; set; } = new();
}