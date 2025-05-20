using System.Runtime.InteropServices;
using Docker.DotNet;
using Docker.DotNet.Models;
using HomeStack.Core.Interfaces;
using HomeStack.Core.Models;

namespace HomeStack.Scanner.Docker;

/// <summary>
/// Implementation of the Docker scanner service
/// </summary>
public class DockerScanner : IDockerScanner
{
    private readonly ILogger<DockerScanner> _logger;

    public DockerScanner(ILogger<DockerScanner> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Checks if the Docker daemon is available
    /// </summary>
    /// <param name="host">Optional Docker host (default: local Docker socket)</param>
    /// <param name="certPath">Optional certificate path for TLS authentication</param>
    /// <param name="apiVersion">Optional Docker API version</param>
    /// <returns>True if the Docker daemon is available, false otherwise</returns>
    public async Task<bool> IsDockerAvailableAsync(string? host = null, string? certPath = null, string? apiVersion = null)
    {
        try
        {
            using var client = CreateDockerClient(host, certPath, apiVersion);
            var info = await client.System.GetSystemInfoAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Scans Docker containers and returns information about them
    /// </summary>
    /// <param name="host">Optional Docker host (default: local Docker socket)</param>
    /// <param name="certPath">Optional certificate path for TLS authentication</param>
    /// <param name="apiVersion">Optional Docker API version</param>
    /// <returns>List of container information objects</returns>
    public async Task<List<Core.Models.ContainerInfo>> ScanContainersAsync(string? endpoint = null)
    {
        return (await ScanContainersAsync(endpoint, null, null)).ToList();
    }
    
    public async Task<IEnumerable<Core.Models.ContainerInfo>> ScanContainersAsync(
        string? host = null, 
        string? certPath = null, 
        string? apiVersion = null)
    {
        var result = new List<Core.Models.ContainerInfo>();
        
        try
        {
            using var client = CreateDockerClient(host, certPath, apiVersion);
            
            // Get containers
            var containers = await client.Containers.ListContainersAsync(new ContainersListParameters
            {
                All = true // Include stopped containers
            });
            
            // Process each container
            foreach (var container in containers)
            {
                var info = new Core.Models.ContainerInfo
                {
                    Id = container.ID,
                    Name = GetContainerName(container),
                    Image = container.Image,
                    IsRunning = container.State == "running",
                    IsVpnIsolated = IsContainerVpnIsolated(container)
                };
                
                // Get container IP address
                try
                {
                    var inspect = await client.Containers.InspectContainerAsync(container.ID);
                    if (inspect.NetworkSettings?.Networks != null)
                    {
                        // Get first available IP address from any network
                        foreach (var network in inspect.NetworkSettings.Networks)
                        {
                            if (!string.IsNullOrEmpty(network.Value.IPAddress))
                            {
                                info.IpAddress = network.Value.IPAddress;
                                break;
                            }
                        }
                    }
                    
                    // Get port mappings
                    info.Ports = GetPortMappings(container, inspect);
                    
                    // Get environment variables (not used in current ContainerInfo model)
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error inspecting container {info.Name}");
                }
                
                // Get labels
                info.Labels = new Dictionary<string, string>(container.Labels ?? new Dictionary<string, string>());
                
                result.Add(info);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scanning Docker containers");
            throw;
        }
        
        return result;
    }
    
    /// <summary>
    /// Creates a Docker client
    /// </summary>
    private DockerClient CreateDockerClient(string? host, string? certPath, string? apiVersion)
    {
        var dockerHost = string.IsNullOrEmpty(host) ? GetDefaultDockerHost() : host;
        
        // Configure Docker client with appropriate options
        var clientConfig = new DockerClientConfiguration(new Uri(dockerHost));
        
        // Create client
        var client = clientConfig.CreateClient();
        
        return client;
    }
    
    /// <summary>
    /// Gets the default Docker host URI based on the operating system
    /// </summary>
    private string GetDefaultDockerHost()
    {
        // Use appropriate Docker socket path for the platform
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return "npipe://./pipe/docker_engine"; // Windows named pipe
        }
        else
        {
            return "unix:///var/run/docker.sock"; // Unix socket
        }
    }
    
    /// <summary>
    /// Gets the container name without leading slash
    /// </summary>
    private string GetContainerName(ContainerListResponse container)
    {
        // Get first name (without leading slash)
        if (container.Names != null && container.Names.Any())
        {
            var name = container.Names[0];
            return name.StartsWith('/') ? name.Substring(1) : name;
        }
        
        return container.ID.Substring(0, 12); // Use shortened ID if no name available
    }
    
    /// <summary>
    /// Gets the container health status
    /// </summary>
    public async Task<ServiceHealth> CheckContainerHealthAsync(string containerId)
    {
        try
        {
            using var client = CreateDockerClient(null, null, null);
            
            // Get container details
            var inspect = await client.Containers.InspectContainerAsync(containerId);
            
            var health = new ServiceHealth
            {
                ServiceName = inspect.Name.TrimStart('/'),
                LastCheckTime = DateTime.Now
            };
            
            // Check container state
            if (inspect.State.Running)
            {
                if (inspect.State.Health != null)
                {
                    health.Status = inspect.State.Health.Status switch
                    {
                        "healthy" => Core.Models.ServiceStatus.Healthy,
                        "unhealthy" => Core.Models.ServiceStatus.Unhealthy,
                        _ => Core.Models.ServiceStatus.Unknown
                    };
                }
                else
                {
                    health.Status = Core.Models.ServiceStatus.Healthy; // Running but no health check
                }
            }
            else if (inspect.State.Status == "exited" || inspect.State.Status == "dead")
            {
                health.Status = Core.Models.ServiceStatus.Unhealthy;
                health.ErrorMessage = $"Container is {inspect.State.Status}";
            }
            else
            {
                health.Status = Core.Models.ServiceStatus.Unknown;
                health.ErrorMessage = $"Container state: {inspect.State.Status}";
            }
            
            return health;
        }
        catch (Exception ex)
        {
            return new ServiceHealth
            {
                ServiceName = containerId,
                Status = Core.Models.ServiceStatus.Unknown,
                ErrorMessage = ex.Message
            };
        }
    }
    
    private ServiceHealth GetContainerHealth(ContainerListResponse container)
    {
        if (container.Status?.Contains("(healthy)") == true)
        {
            return new ServiceHealth 
            { 
                ServiceName = GetContainerName(container),
                Status = Core.Models.ServiceStatus.Healthy,
                LastCheckTime = DateTime.Now
            };
        }
        else if (container.Status?.Contains("(unhealthy)") == true)
        {
            return new ServiceHealth 
            { 
                ServiceName = GetContainerName(container),
                Status = Core.Models.ServiceStatus.Unhealthy,
                LastCheckTime = DateTime.Now,
                ErrorMessage = "Container reported as unhealthy"
            };
        }
        else if (container.Status?.Contains("Up") == true)
        {
            return new ServiceHealth 
            { 
                ServiceName = GetContainerName(container),
                Status = Core.Models.ServiceStatus.Healthy, // Assume healthy if up but no health check
                LastCheckTime = DateTime.Now
            };
        }
        else if (container.Status?.Contains("Exited") == true || container.Status?.Contains("Dead") == true)
        {
            return new ServiceHealth 
            { 
                ServiceName = GetContainerName(container),
                Status = Core.Models.ServiceStatus.Unhealthy,
                LastCheckTime = DateTime.Now,
                ErrorMessage = container.Status
            };
        }
        else if (container.Status?.Contains("Created") == true || container.Status?.Contains("Restarting") == true)
        {
            return new ServiceHealth 
            { 
                ServiceName = GetContainerName(container),
                Status = Core.Models.ServiceStatus.Unknown,
                LastCheckTime = DateTime.Now,
                ErrorMessage = container.Status
            };
        }
        
        return new ServiceHealth 
        { 
            ServiceName = GetContainerName(container),
            Status = Core.Models.ServiceStatus.Unknown,
            LastCheckTime = DateTime.Now
        };
    }
    
    /// <summary>
    /// Checks if the container is running behind a VPN
    /// </summary>
    private bool IsContainerVpnIsolated(ContainerListResponse container)
    {
        // Check for common VPN container labels or names
        if (container.Labels != null)
        {
            // Check for gluetun, vpn, or wireguard label
            if (container.Labels.Any(l => 
                l.Key.Contains("vpn", StringComparison.OrdinalIgnoreCase) ||
                l.Value.Contains("vpn", StringComparison.OrdinalIgnoreCase) ||
                l.Key.Contains("wireguard", StringComparison.OrdinalIgnoreCase) ||
                l.Value.Contains("wireguard", StringComparison.OrdinalIgnoreCase) ||
                l.Key.Contains("gluetun", StringComparison.OrdinalIgnoreCase) ||
                l.Value.Contains("gluetun", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
        }
        
        // Check for VPN in container name or image
        return container.Names?.Any(n => 
            n.Contains("vpn", StringComparison.OrdinalIgnoreCase) ||
            n.Contains("wireguard", StringComparison.OrdinalIgnoreCase) ||
            n.Contains("gluetun", StringComparison.OrdinalIgnoreCase)) == true
            || container.Image.Contains("vpn", StringComparison.OrdinalIgnoreCase)
            || container.Image.Contains("wireguard", StringComparison.OrdinalIgnoreCase)
            || container.Image.Contains("gluetun", StringComparison.OrdinalIgnoreCase);
    }
    
    /// <summary>
    /// Gets port mappings for the container
    /// </summary>
    private List<PortMapping> GetPortMappings(ContainerListResponse container, ContainerInspectResponse inspect)
    {
        var portMappings = new List<PortMapping>();
        
        // Process port mappings
        if (container.Ports != null)
        {
            foreach (var port in container.Ports)
            {
                if (port.PrivatePort > 0) // Skip ports without a private port
                {
                    var mapping = new PortMapping
                    {
                        ContainerPort = (int)port.PrivatePort,
                        Protocol = port.Type ?? "tcp"
                    };
                    
                    if (port.PublicPort > 0)
                    {
                        mapping.HostPort = (int)port.PublicPort;
                    }
                    
                    portMappings.Add(mapping);
                }
            }
        }
        
        // If no ports found in container list, try to get them from inspect
        if (portMappings.Count == 0 && inspect.Config?.ExposedPorts != null)
        {
            foreach (var port in inspect.Config.ExposedPorts)
            {
                var parts = port.Key.Split('/');
                if (parts.Length >= 1 && int.TryParse(parts[0], out var containerPort))
                {
                    var mapping = new PortMapping
                    {
                        ContainerPort = containerPort,
                        Protocol = parts.Length > 1 ? parts[1] : "tcp"
                    };
                    
                    portMappings.Add(mapping);
                }
            }
        }
        
        return portMappings;
    }
    
    /// <summary>
    /// Gets environment variables for the container
    /// </summary>
    private Dictionary<string, string> GetEnvironmentVariables(ContainerInspectResponse inspect)
    {
        var envVars = new Dictionary<string, string>();
        
        if (inspect.Config?.Env != null)
        {
            foreach (var env in inspect.Config.Env)
            {
                var parts = env.Split('=', 2);
                if (parts.Length == 2)
                {
                    var key = parts[0];
                    var value = parts[1];
                    
                    // Skip sensitive environment variables
                    if (IsSensitiveEnvVar(key))
                    {
                        value = "********";
                    }
                    
                    envVars[key] = value;
                }
            }
        }
        
        return envVars;
    }
    
    /// <summary>
    /// Checks if the environment variable is sensitive (passwords, tokens, keys)
    /// </summary>
    private bool IsSensitiveEnvVar(string key)
    {
        var sensitiveKeywords = new[]
        {
            "password", "pass", "pwd", "secret", "token", "key", "auth", "cred", "apikey", "api_key",
            "jwt", "access", "login", "user"
        };
        
        return sensitiveKeywords.Any(k => key.Contains(k, StringComparison.OrdinalIgnoreCase));
    }
}