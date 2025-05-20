using HomeStack.Core.Interfaces;
using HomeStack.Core.Models;
using HomeStack.Configurator;

namespace HomeStack.Scanner;

/// <summary>
/// Implementation of the scan service
/// </summary>
public class ScanService : IScanService
{
    private readonly IDockerScanner _dockerScanner;
    private readonly IUnraidScanner _unraidScanner;
    private readonly IPfSenseScanner _pfSenseScanner;
    private readonly ILogger<ScanService>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScanService"/> class
    /// </summary>
    /// <param name="dockerScanner">Docker scanner</param>
    /// <param name="unraidScanner">Unraid scanner</param>
    /// <param name="pfSenseScanner">pfSense scanner</param>
    /// <param name="logger">Optional logger</param>
    public ScanService(
        IDockerScanner dockerScanner,
        IUnraidScanner unraidScanner,
        IPfSenseScanner pfSenseScanner,
        ILogger<ScanService>? logger = null)
    {
        _dockerScanner = dockerScanner;
        _unraidScanner = unraidScanner;
        _pfSenseScanner = pfSenseScanner;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<ScanResults> ScanNetworkAsync(
        bool scanDocker = true,
        bool scanUnraid = false,
        bool scanPfSense = false,
        (string hostname, string username, string password)? unraidSettings = null,
        (string hostname, string username, string password)? pfSenseSettings = null)
    {
        var results = new ScanResults();

        try
        {
            // Scan Docker containers
            if (scanDocker)
            {
                _logger?.LogInformation("Scanning Docker containers...");
                results.Containers.AddRange(await _dockerScanner.ScanContainersAsync());
            }

            // Scan Unraid
            if (scanUnraid && unraidSettings.HasValue)
            {
                _logger?.LogInformation($"Scanning Unraid server: {unraidSettings.Value.hostname}");
                
                // Check if Unraid is available
                if (await _unraidScanner.IsUnraidAvailableAsync(
                    unraidSettings.Value.hostname,
                    unraidSettings.Value.username,
                    unraidSettings.Value.password))
                {
                    // Get system info
                    results.UnraidInfo = await _unraidScanner.GetSystemInfoAsync(
                        unraidSettings.Value.hostname,
                        unraidSettings.Value.username,
                        unraidSettings.Value.password);
                    
                    // Get containers if Docker is enabled
                    if (results.UnraidInfo.DockerEnabled)
                    {
                        var unraidContainers = await _unraidScanner.GetContainersAsync(
                            unraidSettings.Value.hostname,
                            unraidSettings.Value.username,
                            unraidSettings.Value.password);
                        
                        // Add containers only if they're not already in the list
                        foreach (var container in unraidContainers)
                        {
                            if (!results.Containers.Any(c => c.Id == container.Id))
                            {
                                results.Containers.Add(container);
                            }
                        }
                    }
                }
                else 
                {
                    _logger?.LogInformation($"Unraid server {unraidSettings.Value.hostname} is not available");
                }
            }

            // Scan pfSense
            if (scanPfSense && pfSenseSettings.HasValue)
            {
                _logger?.LogInformation($"Scanning pfSense router: {pfSenseSettings.Value.hostname}");
                
                // Check if pfSense is available
                if (await _pfSenseScanner.IsPfSenseAvailableAsync(
                    pfSenseSettings.Value.hostname,
                    pfSenseSettings.Value.username,
                    pfSenseSettings.Value.password))
                {
                    // Get system info
                    results.PfSenseInfo = await _pfSenseScanner.GetSystemInfoAsync(
                        pfSenseSettings.Value.hostname,
                        pfSenseSettings.Value.username,
                        pfSenseSettings.Value.password);
                    
                    // Get DNS host overrides
                    var dnsHostOverrides = await _pfSenseScanner.GetDnsHostOverridesAsync(
                        pfSenseSettings.Value.hostname,
                        pfSenseSettings.Value.username,
                        pfSenseSettings.Value.password);
                        
                    if (results.PfSenseInfo != null)
                    {
                        results.PfSenseInfo.DnsHostOverrides = dnsHostOverrides;
                    }
                    
                    // Get DHCP leases
                    var dhcpLeases = await _pfSenseScanner.GetDhcpLeasesAsync(
                        pfSenseSettings.Value.hostname,
                        pfSenseSettings.Value.username,
                        pfSenseSettings.Value.password);
                        
                    if (results.PfSenseInfo != null)
                    {
                        results.PfSenseInfo.DhcpLeases = dhcpLeases;
                    }
                }
                else
                {
                    _logger?.LogInformation($"pfSense router {pfSenseSettings.Value.hostname} is not available");
                }
            }

            // Set scan timestamp
            results.ScanTimestamp = DateTime.Now;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during network scan");
        }

        return results;
    }

    /// <inheritdoc/>
    public async Task<Dictionary<string, ServiceHealth>> CheckServiceHealthAsync(ScanResults scanResults)
    {
        var healthResults = new Dictionary<string, ServiceHealth>();

        try
        {
            foreach (var container in scanResults.Containers)
            {
                if (container.IsRunning)
                {
                    _logger?.LogInformation($"Checking health for container: {container.Name}");
                    var health = await _dockerScanner.CheckContainerHealthAsync(container.Id);
                    health.ServiceName = container.Name; // Use container name instead of ID for better readability
                    healthResults[container.Name] = health;
                }
                else
                {
                    // Container is not running
                    healthResults[container.Name] = new ServiceHealth
                    {
                        ServiceName = container.Name,
                        Status = Core.Models.ServiceStatus.Unhealthy,
                        ErrorMessage = "Container is not running",
                        LastCheckTime = DateTime.Now
                    };
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error checking service health");
        }

        return healthResults;
    }

    /// <inheritdoc/>
    public async Task<Dictionary<string, string>> GenerateConfigurationsAsync(
        ScanResults scanResults,
        IEnumerable<string> configTypes,
        string? domain = null)
    {
        var configurations = new Dictionary<string, string>();
        
        try
        {
            // Create a configuration generator
            // In a proper DI setup, this would be injected
            var configGenerator = new ConfigurationGenerator();
            
            foreach (var configType in configTypes)
            {
                switch (configType.ToLowerInvariant())
                {
                    case "dns":
                        // Generate DNS host overrides
                        var hostOverrides = configGenerator.GenerateDnsHostOverrides(scanResults.Containers, domain);
                        configurations["dns"] = await configGenerator.GeneratePfSenseDnsConfigAsync(hostOverrides);
                        break;
                        
                    case "nginx":
                        // Generate Nginx proxy config
                        configurations["nginx"] = await configGenerator.GenerateProxyConfigurationAsync(
                            scanResults.Containers, "nginx", domain);
                        break;
                        
                    case "caddy":
                        // Generate Caddy proxy config
                        configurations["caddy"] = await configGenerator.GenerateProxyConfigurationAsync(
                            scanResults.Containers, "caddy", domain);
                        break;
                        
                    case "flame":
                        // Generate Flame dashboard config
                        configurations["flame"] = await configGenerator.GenerateDashboardConfigurationAsync(
                            scanResults.Containers, "flame", domain);
                        break;
                        
                    case "homepage":
                        // Generate Homepage dashboard config
                        configurations["homepage"] = await configGenerator.GenerateDashboardConfigurationAsync(
                            scanResults.Containers, "homepage", domain);
                        break;
                        
                    default:
                        _logger?.LogError(null, $"Unsupported configuration type: {configType}");
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error generating configurations");
        }
        
        return configurations;
    }
}

/// <summary>
/// Logger interface extension for information level
/// </summary>
public static class LoggerExtensions
{
    /// <summary>
    /// Logs an information message
    /// </summary>
    /// <typeparam name="T">Type to log</typeparam>
    /// <param name="logger">Logger</param>
    /// <param name="message">Message</param>
    public static void LogInformation<T>(this ILogger<T>? logger, string message)
    {
        logger?.LogError(null, message);
    }
}