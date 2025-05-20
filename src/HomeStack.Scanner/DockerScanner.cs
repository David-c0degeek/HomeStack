using Docker.DotNet;
using Docker.DotNet.Models;
using HomeStack.Core.Interfaces;
using HomeStack.Core.Models;
using System.Net;

namespace HomeStack.Scanner;

/// <summary>
/// Implementation of the Docker scanner
/// </summary>
public class DockerScanner : IDockerScanner
{
    private readonly ILogger<DockerScanner>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DockerScanner"/> class
    /// </summary>
    /// <param name="logger">Optional logger</param>
    public DockerScanner(ILogger<DockerScanner>? logger = null)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<List<ContainerInfo>> ScanContainersAsync(string? endpoint = null)
    {
        var containers = new List<ContainerInfo>();

        try
        {
            // Create Docker client
            var client = CreateDockerClient(endpoint);

            // Get all containers (running and stopped)
            var dockerContainers = await client.Containers.ListContainersAsync(
                new ContainersListParameters
                {
                    All = true
                });

            foreach (var dockerContainer in dockerContainers)
            {
                var containerInfo = new ContainerInfo
                {
                    Id = dockerContainer.ID,
                    Name = GetContainerName(dockerContainer),
                    Image = dockerContainer.Image,
                    IsRunning = dockerContainer.State == "running",
                    Labels = new Dictionary<string, string>(dockerContainer.Labels ?? new Dictionary<string, string>())
                };

                // Get network information
                if (dockerContainer.NetworkSettings?.Networks != null)
                {
                    // Get the first network's IP (typically "bridge" network)
                    var network = dockerContainer.NetworkSettings.Networks.FirstOrDefault();
                    if (network.Value != null)
                    {
                        containerInfo.IpAddress = network.Value.IPAddress ?? string.Empty;
                    }
                }

                // Get port mappings
                if (dockerContainer.Ports != null)
                {
                    foreach (var port in dockerContainer.Ports)
                    {
                        if (port.PublicPort > 0)
                        {
                            containerInfo.Ports.Add(new PortMapping
                            {
                                HostPort = (int)port.PublicPort,
                                ContainerPort = (int)port.PrivatePort,
                                Protocol = port.Type ?? "tcp"
                            });
                        }
                    }
                }

                // Check if container is VPN isolated (based on common patterns)
                containerInfo.IsVpnIsolated = IsVpnIsolated(containerInfo);

                containers.Add(containerInfo);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error scanning Docker containers");
        }

        return containers;
    }

    /// <inheritdoc/>
    public async Task<ServiceHealth> CheckContainerHealthAsync(string containerId)
    {
        var health = new ServiceHealth
        {
            ServiceName = containerId,
            Status = Core.Models.ServiceStatus.Unknown,
            LastCheckTime = DateTime.Now
        };

        try
        {
            // Create Docker client
            var client = CreateDockerClient();

            // Get container information
            var inspectResponse = await client.Containers.InspectContainerAsync(containerId);
            
            // Check if container is running
            if (inspectResponse.State?.Running != true)
            {
                health.Status = Core.Models.ServiceStatus.Unhealthy;
                health.ErrorMessage = "Container is not running";
                return health;
            }

            // If container has health check, use it
            if (!string.IsNullOrEmpty(inspectResponse.State.Health?.Status))
            {
                health.Status = inspectResponse.State.Health.Status?.ToLower() switch
                {
                    "healthy" => Core.Models.ServiceStatus.Healthy,
                    "unhealthy" => Core.Models.ServiceStatus.Unhealthy,
                    "starting" => Core.Models.ServiceStatus.Degraded,
                    _ => Core.Models.ServiceStatus.Unknown
                };
                return health;
            }

            // If container is running but has no health check, consider it healthy
            health.Status = Core.Models.ServiceStatus.Healthy;
        }
        catch (Exception ex)
        {
            health.Status = Core.Models.ServiceStatus.Unhealthy;
            health.ErrorMessage = ex.Message;
            _logger?.LogError(ex, "Error checking container health");
        }

        return health;
    }

    private static bool IsVpnIsolated(ContainerInfo container)
    {
        // Common VPN container name patterns
        var vpnContainerPatterns = new[]
        {
            "vpn", "wireguard", "openvpn", "mullvad", "nordvpn", "expressvpn", "protonvpn", "surfshark", "pia", "privatevpn"
        };

        // Check if the container name contains VPN-related keywords
        if (vpnContainerPatterns.Any(pattern => container.Name.Contains(pattern, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        // Check if the container is using a VPN network interface
        if (container.Labels.TryGetValue("com.docker.compose.network", out var network))
        {
            return vpnContainerPatterns.Any(pattern => network.Contains(pattern, StringComparison.OrdinalIgnoreCase));
        }

        // Check if IP address seems to be in a VPN subnet
        if (!string.IsNullOrEmpty(container.IpAddress))
        {
            return !IPAddress.TryParse(container.IpAddress, out var ipAddress) || 
                   !IsPrivateIpAddress(ipAddress);
        }

        return false;
    }

    private static bool IsPrivateIpAddress(IPAddress ipAddress)
    {
        byte[] bytes = ipAddress.GetAddressBytes();
        
        // Check for private IP ranges (RFC 1918)
        return (bytes[0] == 10) || // 10.0.0.0/8
               (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) || // 172.16.0.0/12
               (bytes[0] == 192 && bytes[1] == 168); // 192.168.0.0/16
    }

    private static string GetContainerName(ContainerListResponse container)
    {
        // Container names in Docker API have a leading slash
        if (container.Names != null && container.Names.Count > 0)
        {
            return container.Names[0].TrimStart('/');
        }

        return container.ID.Substring(0, 12); // Return shortened container ID if no name
    }

    private static DockerClient CreateDockerClient(string? endpoint = null)
    {
        // Use provided endpoint or try common Docker endpoints
        var dockerEndpoints = new List<string>
        {
            endpoint ?? string.Empty,
            "npipe://./pipe/docker_engine", // Windows
            "unix:///var/run/docker.sock",  // Linux
            "tcp://localhost:2375",         // TCP without TLS
            "tcp://localhost:2376"          // TCP with TLS
        };

        // Filter out empty endpoints
        dockerEndpoints = dockerEndpoints.Where(e => !string.IsNullOrEmpty(e)).ToList();

        // Try each endpoint until one works
        Exception? lastException = null;

        foreach (var dockerEndpoint in dockerEndpoints)
        {
            try
            {
                var client = new DockerClientConfiguration(new Uri(dockerEndpoint))
                    .CreateClient();

                // Test connection
                client.System.PingAsync().GetAwaiter().GetResult();

                return client;
            }
            catch (Exception ex)
            {
                lastException = ex;
                // Continue trying other endpoints
            }
        }

        // If no endpoint worked, throw the last exception
        throw lastException ?? new Exception("No valid Docker endpoint found");
    }
}

/// <summary>
/// Simple logger interface for dependency injection
/// </summary>
/// <typeparam name="T">Type to log</typeparam>
public interface ILogger<T>
{
    /// <summary>
    /// Logs an error
    /// </summary>
    /// <param name="ex">Exception</param>
    /// <param name="message">Message</param>
    void LogError(Exception ex, string message);
}