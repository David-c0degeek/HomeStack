using HomeStack.Core.Interfaces;
using HomeStack.Core.Models;
using Scriban;

namespace HomeStack.Scanner.Services;

/// <summary>
/// Implementation of the scan service that orchestrates scanning operations
/// </summary>
public class ScanService : IScanService
{
    private readonly IDockerScanner _dockerScanner;
    private readonly IUnraidScanner? _unraidScanner;
    private readonly IPfSenseScanner? _pfSenseScanner;
    private readonly ILogger<ScanService>? _logger;
    
    /// <summary>
    /// Initializes a new instance of the ScanService class
    /// </summary>
    /// <param name="dockerScanner">Docker scanner</param>
    /// <param name="unraidScanner">Unraid scanner (optional)</param>
    /// <param name="pfSenseScanner">pfSense scanner (optional)</param>
    /// <param name="logger">Logger (optional)</param>
    public ScanService(
        IDockerScanner dockerScanner,
        IUnraidScanner? unraidScanner = null,
        IPfSenseScanner? pfSenseScanner = null,
        ILogger<ScanService>? logger = null)
    {
        _dockerScanner = dockerScanner ?? throw new ArgumentNullException(nameof(dockerScanner));
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
        var results = new ScanResults
        {
            ScanTimestamp = DateTime.Now,
            Containers = new List<ContainerInfo>(),
            UnraidInfo = null,
            PfSenseInfo = null
        };
        
        // Scan Docker containers
        if (scanDocker)
        {
            try
            {
                results.Containers.AddRange(await _dockerScanner.ScanContainersAsync());
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error scanning Docker containers");
                Console.WriteLine($"Error scanning Docker containers: {ex.Message}");
            }
        }
        
        // Scan Unraid server
        if (scanUnraid && _unraidScanner != null && unraidSettings.HasValue)
        {
            var (hostname, username, password) = unraidSettings.Value;
            
            if (string.IsNullOrEmpty(hostname))
            {
                Console.WriteLine("Unraid host is required for Unraid scanning");
            }
            else
            {
                try
                {
                    // Check if Unraid is available
                    if (await _unraidScanner.IsUnraidAvailableAsync(hostname, username, password))
                    {
                        // Get system info
                        results.UnraidInfo = await _unraidScanner.GetSystemInfoAsync(hostname, username, password);
                        
                        // Get containers if Docker is enabled
                        if (results.UnraidInfo.DockerEnabled)
                        {
                            var unraidContainers = await _unraidScanner.GetContainersAsync(hostname, username, password);
                            
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
                        Console.WriteLine($"Unraid server {hostname} is not available");
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, $"Error scanning Unraid server: {hostname}");
                    Console.WriteLine($"Error scanning Unraid server: {ex.Message}");
                }
            }
        }
        
        // Scan pfSense router
        if (scanPfSense && _pfSenseScanner != null && pfSenseSettings.HasValue)
        {
            var (hostname, username, password) = pfSenseSettings.Value;
            
            if (string.IsNullOrEmpty(hostname))
            {
                Console.WriteLine("pfSense host is required for pfSense scanning");
            }
            else if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                Console.WriteLine("pfSense username and password are required for pfSense scanning");
            }
            else
            {
                try
                {
                    // Check if pfSense is available
                    if (await _pfSenseScanner.IsPfSenseAvailableAsync(hostname, username, password))
                    {
                        // Get system info
                        results.PfSenseInfo = await _pfSenseScanner.GetSystemInfoAsync(hostname, username, password);
                        
                        // Get DNS overrides
                        var dnsHostOverrides = await _pfSenseScanner.GetDnsHostOverridesAsync(hostname, username, password);
                        if (results.PfSenseInfo != null)
                        {
                            results.PfSenseInfo.DnsHostOverrides = dnsHostOverrides;
                        }
                        
                        // Get DHCP leases
                        var dhcpLeases = await _pfSenseScanner.GetDhcpLeasesAsync(hostname, username, password);
                        if (results.PfSenseInfo != null)
                        {
                            results.PfSenseInfo.DhcpLeases = dhcpLeases;
                        }
                    }
                    else
                    {
                        Console.WriteLine($"pfSense router {hostname} is not available");
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, $"Error scanning pfSense router: {hostname}");
                    Console.WriteLine($"Error scanning pfSense router: {ex.Message}");
                }
            }
        }
        
        // Generate proxy configs for discovered containers
        foreach (var container in results.Containers)
        {
            if (!string.IsNullOrEmpty(container.IpAddress) && container.Ports.Count > 0)
            {
                var port = container.Ports.FirstOrDefault()?.ContainerPort ?? 80;
                
                results.ProxyConfigs.Add(new ProxyConfig
                {
                    Name = container.Name,
                    TargetHost = container.IpAddress,
                    TargetPort = port,
                    Enabled = true
                });
            }
        }
        
        return results;
    }

    /// <inheritdoc/>
    public async Task<Dictionary<string, ServiceHealth>> CheckServiceHealthAsync(ScanResults scanResults)
    {
        var results = new Dictionary<string, ServiceHealth>();
        
        // Check container health
        foreach (var container in scanResults.Containers)
        {
            try 
            {
                if (!string.IsNullOrEmpty(container.Id))
                {
                    var health = await _dockerScanner.CheckContainerHealthAsync(container.Id);
                    results[container.Name] = health;
                }
                else
                {
                    results[container.Name] = new ServiceHealth
                    {
                        ServiceName = container.Name,
                        Status = ServiceStatus.Unknown,
                        ErrorMessage = "Container ID is missing"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error checking container health: {container.Name}");
                results[container.Name] = new ServiceHealth
                {
                    ServiceName = container.Name,
                    Status = ServiceStatus.Unknown,
                    ErrorMessage = ex.Message
                };
            }
        }
        
        return results;
    }
    
    /// <inheritdoc/>
    public async Task<Dictionary<string, string>> GenerateConfigurationsAsync(
        ScanResults scanResults, 
        IEnumerable<string> configTypes, 
        string? domain = null)
    {
        var results = new Dictionary<string, string>();
        
        foreach (var configType in configTypes)
        {
            switch (configType.ToLowerInvariant())
            {
                case "dns":
                    results["dns"] = GenerateDnsConfig(scanResults, domain);
                    break;
                    
                case "nginx":
                    results["nginx"] = GenerateNginxConfig(scanResults, domain);
                    break;
                    
                case "caddy":
                    results["caddy"] = GenerateCaddyConfig(scanResults, domain);
                    break;
                    
                case "flame":
                    results["flame"] = GenerateFlameConfig(scanResults, domain);
                    break;
                    
                case "homepage":
                    results["homepage"] = GenerateHomepageConfig(scanResults, domain);
                    break;
            }
        }
        
        return results;
    }
    
    private string GenerateDnsConfig(ScanResults scanResults, string? domain)
    {
        // Simple DNS config generation
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("# Generated DNS host overrides");
        sb.AppendLine("# Format: hostname IP");
        
        foreach (var container in scanResults.Containers)
        {
            if (!string.IsNullOrEmpty(container.IpAddress))
            {
                string hostname = container.Name;
                if (!string.IsNullOrEmpty(domain) && !domain.StartsWith("."))
                {
                    hostname += "." + domain;
                }
                else if (!string.IsNullOrEmpty(domain))
                {
                    hostname += domain;
                }
                
                sb.AppendLine($"{hostname} {container.IpAddress}");
            }
        }
        
        return sb.ToString();
    }
    
    private string GenerateNginxConfig(ScanResults scanResults, string? domain)
    {
        // Simple Nginx config generation
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("# Generated Nginx configuration");
        
        foreach (var container in scanResults.Containers)
        {
            if (!string.IsNullOrEmpty(container.IpAddress) && container.Ports.Count > 0)
            {
                var port = container.Ports.FirstOrDefault()?.ContainerPort ?? 80;
                
                string hostname = container.Name;
                if (!string.IsNullOrEmpty(domain) && !domain.StartsWith("."))
                {
                    hostname += "." + domain;
                }
                else if (!string.IsNullOrEmpty(domain))
                {
                    hostname += domain;
                }
                
                sb.AppendLine($"server {{");
                sb.AppendLine($"    listen 80;");
                sb.AppendLine($"    listen [::]:80;");
                sb.AppendLine($"    server_name {hostname};");
                sb.AppendLine();
                sb.AppendLine($"    location / {{");
                sb.AppendLine($"        proxy_pass http://{container.IpAddress}:{port};");
                sb.AppendLine($"        proxy_set_header Host $host;");
                sb.AppendLine($"        proxy_set_header X-Real-IP $remote_addr;");
                sb.AppendLine($"        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;");
                sb.AppendLine($"        proxy_set_header X-Forwarded-Proto $scheme;");
                sb.AppendLine($"    }}");
                sb.AppendLine($"}}");
                sb.AppendLine();
            }
        }
        
        return sb.ToString();
    }
    
    private string GenerateCaddyConfig(ScanResults scanResults, string? domain)
    {
        // Simple Caddy config generation
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("# Generated Caddyfile");
        
        foreach (var container in scanResults.Containers)
        {
            if (!string.IsNullOrEmpty(container.IpAddress) && container.Ports.Count > 0)
            {
                var port = container.Ports.FirstOrDefault()?.ContainerPort ?? 80;
                
                string hostname = container.Name;
                if (!string.IsNullOrEmpty(domain) && !domain.StartsWith("."))
                {
                    hostname += "." + domain;
                }
                else if (!string.IsNullOrEmpty(domain))
                {
                    hostname += domain;
                }
                
                sb.AppendLine($"{hostname} {{");
                sb.AppendLine($"    reverse_proxy {container.IpAddress}:{port}");
                sb.AppendLine($"}}");
                sb.AppendLine();
            }
        }
        
        return sb.ToString();
    }
    
    private string GenerateFlameConfig(ScanResults scanResults, string? domain)
    {
        // Simple Flame bookmarks JSON
        var bookmarks = new List<object>();
        
        foreach (var container in scanResults.Containers)
        {
            if (!string.IsNullOrEmpty(container.IpAddress) && container.Ports.Count > 0)
            {
                var port = container.Ports.FirstOrDefault()?.ContainerPort ?? 80;
                
                string hostname = container.Name;
                if (!string.IsNullOrEmpty(domain) && !domain.StartsWith("."))
                {
                    hostname += "." + domain;
                }
                else if (!string.IsNullOrEmpty(domain))
                {
                    hostname += domain;
                }
                
                bookmarks.Add(new
                {
                    name = container.Name,
                    url = $"http://{hostname}",
                    icon = "mdi-docker",
                    description = $"Container: {container.Image}"
                });
            }
        }
        
        return System.Text.Json.JsonSerializer.Serialize(new { bookmarks }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
    }
    
    private string GenerateHomepageConfig(ScanResults scanResults, string? domain)
    {
        // Simple Homepage services YAML
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("# Generated Homepage services configuration");
        sb.AppendLine("---");
        sb.AppendLine("services:");
        sb.AppendLine("  - Services:");
        
        foreach (var container in scanResults.Containers)
        {
            if (!string.IsNullOrEmpty(container.IpAddress) && container.Ports.Count > 0)
            {
                string hostname = container.Name;
                if (!string.IsNullOrEmpty(domain) && !domain.StartsWith("."))
                {
                    hostname += "." + domain;
                }
                else if (!string.IsNullOrEmpty(domain))
                {
                    hostname += domain;
                }
                
                sb.AppendLine($"    - {container.Name}:");
                sb.AppendLine($"        icon: docker");
                sb.AppendLine($"        href: http://{hostname}");
                sb.AppendLine($"        description: {container.Image}");
                sb.AppendLine($"        ping: {container.IpAddress}");
            }
        }
        
        return sb.ToString();
    }
}
