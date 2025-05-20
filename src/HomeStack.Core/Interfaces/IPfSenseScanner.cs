using HomeStack.Core.Models;

namespace HomeStack.Core.Interfaces;

/// <summary>
/// Interface for scanning pfSense router
/// </summary>
public interface IPfSenseScanner
{
    /// <summary>
    /// Checks if pfSense is available via SSH
    /// </summary>
    /// <param name="hostname">pfSense hostname or IP address</param>
    /// <param name="username">SSH username</param>
    /// <param name="password">SSH password</param>
    /// <param name="port">SSH port (default: 22)</param>
    /// <returns>True if pfSense is available, false otherwise</returns>
    Task<bool> IsPfSenseAvailableAsync(string hostname, string? username = null, string? password = null, int port = 22);
    
    /// <summary>
    /// Gets system information from pfSense
    /// </summary>
    /// <param name="hostname">pfSense hostname or IP address</param>
    /// <param name="username">SSH username</param>
    /// <param name="password">SSH password</param>
    /// <returns>pfSense information</returns>
    Task<PfSenseInfo> GetSystemInfoAsync(string hostname, string username, string password);
    
    /// <summary>
    /// Gets DNS host overrides from pfSense
    /// </summary>
    /// <param name="hostname">pfSense hostname or IP address</param>
    /// <param name="username">SSH username</param>
    /// <param name="password">SSH password</param>
    /// <returns>List of DNS host overrides</returns>
    Task<List<DnsHostOverride>> GetDnsHostOverridesAsync(string hostname, string username, string password);
    
    /// <summary>
    /// Gets DHCP leases from pfSense
    /// </summary>
    /// <param name="hostname">pfSense hostname or IP address</param>
    /// <param name="username">SSH username</param>
    /// <param name="password">SSH password</param>
    /// <returns>List of DHCP leases</returns>
    Task<List<DhcpLease>> GetDhcpLeasesAsync(string hostname, string username, string password);
    
    /// <summary>
    /// Updates a DNS host override in pfSense
    /// </summary>
    /// <param name="hostname">pfSense hostname or IP address</param>
    /// <param name="username">SSH username</param>
    /// <param name="password">SSH password</param>
    /// <param name="hostOverride">DNS host override to update</param>
    /// <returns>True if successful, false otherwise</returns>
    Task<bool> UpdateDnsHostOverrideAsync(string hostname, string username, string password, DnsHostOverride hostOverride);
}