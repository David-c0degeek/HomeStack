using HomeStack.Core.Interfaces;
using HomeStack.Core.Models;

namespace HomeStack.Scanner.Services;

/// <summary>
/// Implementation of the scan service
/// </summary>
public class ScanService : IScanService
{
    private readonly IDockerScanner _dockerScanner;
    private readonly IUnraidScanner? _unraidScanner;
    private readonly IPfSenseScanner? _pfSenseScanner;
    
    /// <summary>
    /// Initializes a new instance of the ScanService class
    /// </summary>
    /// <param name="dockerScanner">Docker scanner</param>
    /// <param name="unraidScanner">Unraid scanner (optional)</param>
    /// <param name="pfSenseScanner">pfSense scanner (optional)</param>
    public ScanService(
        IDockerScanner dockerScanner,
        IUnraidScanner? unraidScanner = null,
        IPfSenseScanner? pfSenseScanner = null)
    {
        _dockerScanner = dockerScanner ?? throw new ArgumentNullException(nameof(dockerScanner));
        _unraidScanner = unraidScanner;
        _pfSenseScanner = pfSenseScanner;
    }
    
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
    public async Task<ScanResults> ScanAllAsync(
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
        string? dockerApiVersion = null)
    {
        var results = new ScanResults
        {
            DockerHost = dockerHost,
            UnraidHost = unraidHost,
            PfSenseHost = pfSenseHost
        };
        
        // Scan Docker containers
        if (scanDocker)
        {
            try
            {
                results.Containers.AddRange(await _dockerScanner.ScanContainersAsync(
                    dockerHost, dockerCertPath, dockerApiVersion));
            }
            catch (Exception ex)
            {
                results.Errors.Add($"Error scanning Docker containers: {ex.Message}");
            }
        }
        
        // Scan Unraid server
        if (scanUnraid && _unraidScanner != null)
        {
            if (string.IsNullOrEmpty(unraidHost))
            {
                results.Errors.Add("Unraid host is required for Unraid scanning");
            }
            else
            {
                try
                {
                    // Check if Unraid is available
                    if (await _unraidScanner.IsUnraidAvailableAsync(
                        unraidHost, unraidUsername, unraidPassword, unraidPort))
                    {
                        // Get system info
                        results.UnraidInfo = await _unraidScanner.GetSystemInfoAsync(
                            unraidHost, unraidUsername, unraidPassword, unraidPort);
                            
                        // Get containers if Docker is enabled
                        if (results.UnraidInfo.DockerEnabled)
                        {
                            var unraidContainers = await _unraidScanner.GetContainersAsync(
                                unraidHost, unraidUsername, unraidPassword, unraidPort);
                                
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
                        results.Warnings.Add($"Unraid server {unraidHost} is not available");
                    }
                }
                catch (Exception ex)
                {
                    results.Errors.Add($"Error scanning Unraid server: {ex.Message}");
                }
            }
        }
        
        // Scan pfSense router
        if (scanPfSense && _pfSenseScanner != null)
        {
            if (string.IsNullOrEmpty(pfSenseHost))
            {
                results.Errors.Add("pfSense host is required for pfSense scanning");
            }
            else if (string.IsNullOrEmpty(pfSenseUsername) || string.IsNullOrEmpty(pfSensePassword))
            {
                results.Errors.Add("pfSense username and password are required for pfSense scanning");
            }
            else
            {
                try
                {
                    // Get system info
                    results.PfSenseInfo = await _pfSenseScanner.GetSystemInfoAsync(
                        pfSenseHost, pfSenseUsername, pfSensePassword, pfSensePort);
                        
                    // Get DNS overrides
                    results.DnsHostOverrides.AddRange(await _pfSenseScanner.GetDnsHostOverridesAsync(
                        pfSenseHost, pfSenseUsername, pfSensePassword, pfSensePort));
                        
                    // Get DHCP leases
                    results.DhcpLeases.AddRange(await _pfSenseScanner.GetDhcpLeasesAsync(
                        pfSenseHost, pfSenseUsername, pfSensePassword, pfSensePort));
                        
                    // Get network interfaces
                    results.NetworkInterfaces.AddRange(await _pfSenseScanner.GetNetworkInterfacesAsync(
                        pfSenseHost, pfSenseUsername, pfSensePassword, pfSensePort));
                }
                catch (Exception ex)
                {
                    results.Errors.Add($"Error scanning pfSense router: {ex.Message}");
                }
            }
        }
        
        // Generate proxy configs for discovered containers
        foreach (var container in results.Containers)
        {
            if (container.PortMappings.Count > 0)
            {
                var port = container.PortMappings.FirstOrDefault(p => p.IsPublic)?.ContainerPort 
                    ?? container.PortMappings.First().ContainerPort;
                    
                results.ProxyConfigs.Add(new ProxyConfig
                {
                    Hostname = container.Name,
                    TargetIp = container.IpAddress,
                    TargetPort = port,
                    IsEnabled = true
                });
            }
        }
        
        return results;
    }
}