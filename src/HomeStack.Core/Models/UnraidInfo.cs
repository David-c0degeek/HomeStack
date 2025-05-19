namespace HomeStack.Core.Models;

/// <summary>
/// Represents system information for an Unraid server
/// </summary>
public class UnraidInfo
{
    /// <summary>
    /// Hostname of the Unraid server
    /// </summary>
    public string Hostname { get; set; } = string.Empty;
    
    /// <summary>
    /// Version of Unraid
    /// </summary>
    public string Version { get; set; } = string.Empty;
    
    /// <summary>
    /// Kernel version
    /// </summary>
    public string? KernelVersion { get; set; }
    
    /// <summary>
    /// Time when the Unraid system was last started
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
    /// CPU model
    /// </summary>
    public string? CpuModel { get; set; }
    
    /// <summary>
    /// CPU temperature
    /// </summary>
    public string? CpuTemperature { get; set; }
    
    /// <summary>
    /// Memory usage information
    /// </summary>
    public string? MemoryUsage { get; set; }
    
    /// <summary>
    /// Array status (Started, Stopped, etc.)
    /// </summary>
    public string? ArrayStatus { get; set; }
    
    /// <summary>
    /// Whether parity check is in progress
    /// </summary>
    public bool ParityCheckInProgress { get; set; }
    
    /// <summary>
    /// Whether Docker is enabled
    /// </summary>
    public bool DockerEnabled { get; set; }
    
    /// <summary>
    /// Whether VM (Virtual Machine) support is enabled
    /// </summary>
    public bool VmEnabled { get; set; }
    
    /// <summary>
    /// Host IP address (used for connection)
    /// </summary>
    public string? HostIpAddress { get; set; }
}