namespace HomeStack.Core.Interfaces;

/// <summary>
/// Interface for the Docker scanner service
/// </summary>
public interface IDockerScanner
{
    /// <summary>
    /// Scans Docker containers and returns information about them
    /// </summary>
    /// <param name="host">Optional Docker host (default: local Docker socket)</param>
    /// <param name="certPath">Optional certificate path for TLS authentication</param>
    /// <param name="apiVersion">Optional Docker API version</param>
    /// <returns>List of container information objects</returns>
    Task<IEnumerable<Models.ContainerInfo>> ScanContainersAsync(
        string? host = null, 
        string? certPath = null, 
        string? apiVersion = null);
        
    /// <summary>
    /// Checks if the Docker daemon is available
    /// </summary>
    /// <param name="host">Optional Docker host (default: local Docker socket)</param>
    /// <param name="certPath">Optional certificate path for TLS authentication</param>
    /// <param name="apiVersion">Optional Docker API version</param>
    /// <returns>True if the Docker daemon is available, false otherwise</returns>
    Task<bool> IsDockerAvailableAsync(
        string? host = null, 
        string? certPath = null, 
        string? apiVersion = null);
}