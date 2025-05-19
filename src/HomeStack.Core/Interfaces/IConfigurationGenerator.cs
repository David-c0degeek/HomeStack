namespace HomeStack.Core.Interfaces;

/// <summary>
/// Interface for configuration generator services
/// </summary>
public interface IConfigurationGenerator
{
    /// <summary>
    /// Generates pfSense DNS host overrides configuration
    /// </summary>
    /// <param name="scanResults">Scan results containing service information</param>
    /// <param name="domainSuffix">Optional domain suffix to append to hostnames (e.g., .local)</param>
    /// <returns>Configuration text for pfSense DNS host overrides</returns>
    Task<string> GenerateDnsConfigAsync(
        Models.ScanResults scanResults, 
        string? domainSuffix = null);

    /// <summary>
    /// Generates Nginx reverse proxy configuration
    /// </summary>
    /// <param name="scanResults">Scan results containing service information</param>
    /// <param name="domainSuffix">Optional domain suffix to append to hostnames (e.g., .local)</param>
    /// <param name="sslEnabled">Whether SSL should be enabled for the proxy</param>
    /// <param name="authEnabled">Whether authentication should be enabled</param>
    /// <param name="authConfig">Authentication configuration if auth is enabled</param>
    /// <returns>Configuration text for Nginx reverse proxy</returns>
    Task<string> GenerateNginxConfigAsync(
        Models.ScanResults scanResults, 
        string? domainSuffix = null, 
        bool sslEnabled = false, 
        bool authEnabled = false, 
        Models.ProxyAuthentication? authConfig = null);

    /// <summary>
    /// Generates Caddy reverse proxy configuration
    /// </summary>
    /// <param name="scanResults">Scan results containing service information</param>
    /// <param name="domainSuffix">Optional domain suffix to append to hostnames (e.g., .local)</param>
    /// <param name="sslEnabled">Whether SSL should be enabled for the proxy</param>
    /// <param name="authEnabled">Whether authentication should be enabled</param>
    /// <param name="authConfig">Authentication configuration if auth is enabled</param>
    /// <returns>Configuration text for Caddy reverse proxy</returns>
    Task<string> GenerateCaddyConfigAsync(
        Models.ScanResults scanResults, 
        string? domainSuffix = null, 
        bool sslEnabled = false, 
        bool authEnabled = false, 
        Models.ProxyAuthentication? authConfig = null);

    /// <summary>
    /// Generates Flame dashboard configuration
    /// </summary>
    /// <param name="scanResults">Scan results containing service information</param>
    /// <param name="domainSuffix">Optional domain suffix to append to hostnames (e.g., .local)</param>
    /// <returns>Configuration text for Flame dashboard</returns>
    Task<string> GenerateFlameConfigAsync(
        Models.ScanResults scanResults, 
        string? domainSuffix = null);

    /// <summary>
    /// Generates Homepage dashboard configuration
    /// </summary>
    /// <param name="scanResults">Scan results containing service information</param>
    /// <param name="domainSuffix">Optional domain suffix to append to hostnames (e.g., .local)</param>
    /// <returns>Configuration text for Homepage dashboard</returns>
    Task<string> GenerateHomepageConfigAsync(
        Models.ScanResults scanResults, 
        string? domainSuffix = null);
}