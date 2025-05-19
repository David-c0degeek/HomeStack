namespace HomeStack.Core.Interfaces;

/// <summary>
/// Interface for the Unraid scanner service
/// </summary>
public interface IUnraidScanner
{
    /// <summary>
    /// Gets Unraid system information
    /// </summary>
    /// <param name="host">Unraid hostname or IP address</param>
    /// <param name="username">SSH username (default: root)</param>
    /// <param name="password">SSH password</param>
    /// <param name="port">SSH port (default: 22)</param>
    /// <returns>Unraid system information</returns>
    Task<Models.UnraidInfo> GetSystemInfoAsync(
        string host, 
        string username = "root", 
        string password = "", 
        int port = 22);

    /// <summary>
    /// Gets Docker containers running on the Unraid server
    /// </summary>
    /// <param name="host">Unraid hostname or IP address</param>
    /// <param name="username">SSH username (default: root)</param>
    /// <param name="password">SSH password</param>
    /// <param name="port">SSH port (default: 22)</param>
    /// <returns>List of container information objects</returns>
    Task<IEnumerable<Models.ContainerInfo>> GetContainersAsync(
        string host, 
        string username = "root", 
        string password = "", 
        int port = 22);

    /// <summary>
    /// Checks if an Unraid server is available
    /// </summary>
    /// <param name="host">Unraid hostname or IP address</param>
    /// <param name="username">SSH username (default: root)</param>
    /// <param name="password">SSH password</param>
    /// <param name="port">SSH port (default: 22)</param>
    /// <returns>True if the Unraid server is available, false otherwise</returns>
    Task<bool> IsUnraidAvailableAsync(
        string host, 
        string username = "root", 
        string password = "", 
        int port = 22);
}