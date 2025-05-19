namespace HomeStack.Core.Interfaces;

/// <summary>
/// Interface for the pfSense scanner service
/// </summary>
public interface IPfSenseScanner
{
    /// <summary>
    /// Gets pfSense system information
    /// </summary>
    /// <param name="host">pfSense hostname or IP address</param>
    /// <param name="username">SSH username</param>
    /// <param name="password">SSH password</param>
    /// <param name="port">SSH port (default: 22)</param>
    /// <returns>pfSense system information</returns>
    Task<Models.PfSenseInfo> GetSystemInfoAsync(
        string host, 
        string username, 
        string password, 
        int port = 22);

    /// <summary>
    /// Gets pfSense DNS host overrides
    /// </summary>
    /// <param name="host">pfSense hostname or IP address</param>
    /// <param name="username">SSH username</param>
    /// <param name="password">SSH password</param>
    /// <param name="port">SSH port (default: 22)</param>
    /// <returns>List of DNS host overrides</returns>
    Task<IEnumerable<Models.DnsHostOverride>> GetDnsHostOverridesAsync(
        string host, 
        string username, 
        string password, 
        int port = 22);

    /// <summary>
    /// Gets pfSense DHCP leases
    /// </summary>
    /// <param name="host">pfSense hostname or IP address</param>
    /// <param name="username">SSH username</param>
    /// <param name="password">SSH password</param>
    /// <param name="port">SSH port (default: 22)</param>
    /// <returns>List of DHCP leases</returns>
    Task<IEnumerable<Models.DhcpLease>> GetDhcpLeasesAsync(
        string host, 
        string username, 
        string password, 
        int port = 22);

    /// <summary>
    /// Gets pfSense network interface information
    /// </summary>
    /// <param name="host">pfSense hostname or IP address</param>
    /// <param name="username">SSH username</param>
    /// <param name="password">SSH password</param>
    /// <param name="port">SSH port (default: 22)</param>
    /// <returns>List of network interfaces</returns>
    Task<IEnumerable<Models.NetworkInterface>> GetNetworkInterfacesAsync(
        string host, 
        string username, 
        string password, 
        int port = 22);
}