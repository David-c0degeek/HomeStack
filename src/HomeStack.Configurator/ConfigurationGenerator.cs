using HomeStack.Core.Interfaces;
using HomeStack.Core.Models;
using Scriban;

namespace HomeStack.Configurator;

/// <summary>
/// Implementation of the configuration generator
/// </summary>
public class ConfigurationGenerator : IConfigurationGenerator
{
    private readonly string _templatesPath;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationGenerator"/> class
    /// </summary>
    /// <param name="templatesPath">Path to template files</param>
    public ConfigurationGenerator(string? templatesPath = null)
    {
        // Default to the Templates directory in the current assembly location
        _templatesPath = templatesPath ?? 
            Path.Combine(Path.GetDirectoryName(typeof(ConfigurationGenerator).Assembly.Location) ?? string.Empty, "Templates");
    }
    
    /// <inheritdoc/>
    public List<DnsHostOverride> GenerateDnsHostOverrides(List<ContainerInfo> containers, string? domain = null)
    {
        var hostOverrides = new List<DnsHostOverride>();
        
        foreach (var container in containers.Where(c => c.IsRunning))
        {
            var hostname = container.Name.ToLowerInvariant();
            
            // Skip if IP address is empty
            if (string.IsNullOrEmpty(container.IpAddress))
            {
                continue;
            }
            
            var hostOverride = new DnsHostOverride
            {
                Hostname = hostname,
                Domain = domain ?? "local",
                IpAddress = container.IpAddress,
                Description = $"HomeStack auto-generated DNS entry for container: {container.Name}"
            };
            
            hostOverrides.Add(hostOverride);
        }
        
        return hostOverrides;
    }

    /// <inheritdoc/>
    public async Task<string> GenerateProxyConfigurationAsync(List<ContainerInfo> containers, string proxyType, string? domain = null)
    {
        domain ??= "local";
        
        // Create proxy configs for containers with web ports
        var proxyConfigs = new List<ProxyConfig>();
        foreach (var container in containers.Where(c => c.IsRunning))
        {
            // Check if container has web ports (80, 443, 8080, etc.)
            var webPorts = container.Ports
                .Where(p => p.ContainerPort == 80 || p.ContainerPort == 443 || p.ContainerPort == 8080)
                .ToList();
            
            if (webPorts.Count > 0)
            {
                foreach (var port in webPorts)
                {
                    var subdomain = container.Name.ToLowerInvariant();
                    var isHttps = port.ContainerPort == 443;
                    
                    var proxyConfig = new ProxyConfig
                    {
                        Name = container.Name,
                        Domain = $"{subdomain}.{domain}",
                        TargetHost = container.IpAddress,
                        TargetPort = port.ContainerPort,
                        Ssl = isHttps,
                        ProxyAuthentication = null // No auth by default
                    };
                    
                    proxyConfigs.Add(proxyConfig);
                }
            }
        }
        
        // Load appropriate template
        var templateFile = proxyType.ToLowerInvariant() switch
        {
            "nginx" => "NginxProxy.scriban",
            "caddy" => "CaddyProxy.scriban",
            _ => throw new ArgumentException($"Unsupported proxy type: {proxyType}")
        };
        
        var templatePath = Path.Combine(_templatesPath, templateFile);
        
        // Check if template file exists
        if (!File.Exists(templatePath))
        {
            throw new FileNotFoundException($"Template file not found: {templatePath}");
        }
        
        // Load template
        var templateContent = await File.ReadAllTextAsync(templatePath);
        var template = Template.Parse(templateContent);
        
        // Execute template with proxy configs
        var result = await template.RenderAsync(new
        {
            ProxyConfigs = proxyConfigs,
            Domain = domain,
            GeneratedAt = DateTime.Now
        });
        
        return result;
    }

    /// <inheritdoc/>
    public async Task<string> GenerateDashboardConfigurationAsync(List<ContainerInfo> containers, string dashboardType, string? domain = null)
    {
        domain ??= "local";
        
        // Filter containers that should be included in the dashboard
        var dashboardContainers = containers
            .Where(c => c.IsRunning)
            .Where(c => !c.Labels.ContainsKey("homestack.dashboard.exclude") || 
                        !bool.TryParse(c.Labels["homestack.dashboard.exclude"], out var exclude) || !exclude)
            .ToList();
        
        // Load appropriate template
        var templateFile = dashboardType.ToLowerInvariant() switch
        {
            "flame" => "FlameBookmarks.scriban",
            "homepage" => "HomepageServices.scriban",
            _ => throw new ArgumentException($"Unsupported dashboard type: {dashboardType}")
        };
        
        var templatePath = Path.Combine(_templatesPath, templateFile);
        
        // Check if template file exists
        if (!File.Exists(templatePath))
        {
            throw new FileNotFoundException($"Template file not found: {templatePath}");
        }
        
        // Load template
        var templateContent = await File.ReadAllTextAsync(templatePath);
        var template = Template.Parse(templateContent);
        
        // Execute template with container data
        var result = await template.RenderAsync(new
        {
            Containers = dashboardContainers,
            Domain = domain,
            GeneratedAt = DateTime.Now
        });
        
        return result;
    }
    
    /// <summary>
    /// Generates pfSense DNS host override configuration file
    /// </summary>
    /// <param name="hostOverrides">List of host overrides</param>
    /// <returns>Generated configuration content</returns>
    public async Task<string> GeneratePfSenseDnsConfigAsync(List<DnsHostOverride> hostOverrides)
    {
        var templatePath = Path.Combine(_templatesPath, "PfSenseDns.scriban");
        
        // Check if template file exists
        if (!File.Exists(templatePath))
        {
            throw new FileNotFoundException($"Template file not found: {templatePath}");
        }
        
        // Load template
        var templateContent = await File.ReadAllTextAsync(templatePath);
        var template = Template.Parse(templateContent);
        
        // Execute template with host overrides
        var result = await template.RenderAsync(new
        {
            Overrides = hostOverrides,
            GeneratedAt = DateTime.Now
        });
        
        return result;
    }
}