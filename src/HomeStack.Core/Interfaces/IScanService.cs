using HomeStack.Core.Models;

namespace HomeStack.Core.Interfaces;

/// <summary>
/// Interface for the scan service that orchestrates scanning operations
/// </summary>
public interface IScanService
{
    /// <summary>
    /// Performs a full scan of the network
    /// </summary>
    /// <param name="scanDocker">Whether to scan Docker</param>
    /// <param name="scanUnraid">Whether to scan Unraid</param>
    /// <param name="scanPfSense">Whether to scan pfSense</param>
    /// <param name="unraidSettings">Unraid connection settings</param>
    /// <param name="pfSenseSettings">pfSense connection settings</param>
    /// <returns>Scan results</returns>
    Task<ScanResults> ScanNetworkAsync(
        bool scanDocker = true, 
        bool scanUnraid = false, 
        bool scanPfSense = false,
        (string hostname, string username, string password)? unraidSettings = null,
        (string hostname, string username, string password)? pfSenseSettings = null);
    
    /// <summary>
    /// Checks the health of all discovered services
    /// </summary>
    /// <param name="scanResults">Previous scan results</param>
    /// <returns>Dictionary of service health statuses</returns>
    Task<Dictionary<string, ServiceHealth>> CheckServiceHealthAsync(ScanResults scanResults);
    
    /// <summary>
    /// Generates configurations based on scan results
    /// </summary>
    /// <param name="scanResults">Scan results</param>
    /// <param name="configTypes">Types of configurations to generate (dns, proxy, dashboard)</param>
    /// <param name="domain">Optional domain suffix</param>
    /// <returns>Dictionary of configuration type to generated content</returns>
    Task<Dictionary<string, string>> GenerateConfigurationsAsync(
        ScanResults scanResults, 
        IEnumerable<string> configTypes, 
        string? domain = null);
}