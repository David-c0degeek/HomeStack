using HomeStack.Core.Models;

namespace HomeStack.Core.Interfaces;

/// <summary>
/// Interface for scanning Unraid server
/// </summary>
public interface IUnraidScanner
{
    /// <summary>
    /// Checks if Unraid is available via SSH
    /// </summary>
    /// <param name="hostname">Unraid hostname or IP address</param>
    /// <param name="username">SSH username</param>
    /// <param name="password">SSH password</param>
    /// <param name="port">SSH port (default: 22)</param>
    /// <returns>True if Unraid is available, false otherwise</returns>
    Task<bool> IsUnraidAvailableAsync(string hostname, string? username = null, string? password = null, int port = 22);
    
    /// <summary>
    /// Scans an Unraid server for information
    /// </summary>
    /// <param name="hostname">Unraid hostname or IP address</param>
    /// <param name="username">SSH username</param>
    /// <param name="password">SSH password</param>
    /// <returns>Unraid server information</returns>
    Task<UnraidInfo> GetSystemInfoAsync(string hostname, string username, string password);
    
    /// <summary>
    /// Gets Docker containers running on Unraid
    /// </summary>
    /// <param name="hostname">Unraid hostname or IP address</param>
    /// <param name="username">SSH username</param>
    /// <param name="password">SSH password</param>
    /// <returns>List of container information</returns>
    Task<List<ContainerInfo>> GetContainersAsync(string hostname, string username, string password);
}