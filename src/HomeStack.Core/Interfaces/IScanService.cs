namespace HomeStack.Core.Interfaces;

/// <summary>
/// Interface for the main scan service
/// </summary>
public interface IScanService
{
    /// <summary>
    /// Performs a comprehensive scan of all enabled services
    /// </summary>
    /// <param name="scanDocker">Whether to scan Docker containers</param>
    /// <param name="scanUnraid">Whether to scan Unraid servers</param>
    /// <param name="scanPfSense">Whether to scan pfSense routers</param>
    /// <param name="unraidHost">Unraid hostname or IP address</param>
    /// <param name="unraidUsername">Unraid SSH username</param>
    /// <param name="unraidPassword">Unraid SSH password</param>
    /// <param name="unraidPort">Unraid SSH port</param>
    /// <param name="pfSenseHost">pfSense hostname or IP address</param>
    /// <param name="pfSenseUsername">pfSense SSH username</param>
    /// <param name="pfSensePassword">pfSense SSH password</param>
    /// <param name="pfSensePort">pfSense SSH port</param>
    /// <param name="dockerHost">Docker host</param>
    /// <param name="dockerCertPath">Docker certificate path</param>
    /// <param name="dockerApiVersion">Docker API version</param>
    /// <returns>Scan results containing all discovered services and information</returns>
    Task<Models.ScanResults> ScanAllAsync(
        bool scanDocker = false,
        bool scanUnraid = false,
        bool scanPfSense = false,
        string? unraidHost = null,
        string? unraidUsername = null,
        string? unraidPassword = null,
        int unraidPort = 22,
        string? pfSenseHost = null,
        string? pfSenseUsername = null,
        string? pfSensePassword = null,
        int pfSensePort = 22,
        string? dockerHost = null,
        string? dockerCertPath = null,
        string? dockerApiVersion = null);
}