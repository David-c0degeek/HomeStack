using HomeStack.Core.Models;

namespace HomeStack.Core.Interfaces;

/// <summary>
/// Interface for generating configuration files based on scan results
/// </summary>
public interface IConfigurationGenerator
{
    /// <summary>
    /// Generates DNS host override configuration for pfSense
    /// </summary>
    /// <param name="containers">Container information</param>
    /// <param name="domain">Optional domain suffix</param>
    /// <returns>List of DNS host overrides</returns>
    List<DnsHostOverride> GenerateDnsHostOverrides(List<ContainerInfo> containers, string? domain = null);
    
    /// <summary>
    /// Generates proxy configuration (Nginx, Caddy, etc.)
    /// </summary>
    /// <param name="containers">Container information</param>
    /// <param name="proxyType">Type of proxy (nginx, caddy)</param>
    /// <param name="domain">Optional domain suffix</param>
    /// <returns>Generated configuration file content</returns>
    Task<string> GenerateProxyConfigurationAsync(List<ContainerInfo> containers, string proxyType, string? domain = null);
    
    /// <summary>
    /// Generates dashboard configuration (Flame, Homepage, etc.)
    /// </summary>
    /// <param name="containers">Container information</param>
    /// <param name="dashboardType">Type of dashboard (flame, homepage)</param>
    /// <param name="domain">Optional domain suffix</param>
    /// <returns>Generated configuration file content</returns>
    Task<string> GenerateDashboardConfigurationAsync(List<ContainerInfo> containers, string dashboardType, string? domain = null);
}