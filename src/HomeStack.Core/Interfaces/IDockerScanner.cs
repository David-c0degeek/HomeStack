using HomeStack.Core.Models;

namespace HomeStack.Core.Interfaces;

/// <summary>
/// Interface for scanning Docker containers
/// </summary>
public interface IDockerScanner
{
    /// <summary>
    /// Scans for Docker containers
    /// </summary>
    /// <param name="endpoint">Optional Docker endpoint</param>
    /// <returns>List of container information</returns>
    Task<List<ContainerInfo>> ScanContainersAsync(string? endpoint = null);
    
    /// <summary>
    /// Checks the health of a container
    /// </summary>
    /// <param name="containerId">Container ID</param>
    /// <returns>Service health status</returns>
    Task<ServiceHealth> CheckContainerHealthAsync(string containerId);
}