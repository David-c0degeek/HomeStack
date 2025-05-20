using HomeStack.Core.Interfaces;
using HomeStack.Core.Models;
using Renci.SshNet;
using System.Net;
using System.Text.RegularExpressions;

namespace HomeStack.Scanner;

/// <summary>
/// Implementation of the Unraid scanner
/// </summary>
public class UnraidScanner : IUnraidScanner
{
    private readonly ILogger<UnraidScanner>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnraidScanner"/> class
    /// </summary>
    /// <param name="logger">Optional logger</param>
    public UnraidScanner(ILogger<UnraidScanner>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Checks if Unraid is available via SSH
    /// </summary>
    /// <param name="hostname">Unraid hostname or IP address</param>
    /// <param name="username">SSH username</param>
    /// <param name="password">SSH password</param>
    /// <param name="port">SSH port (default: 22)</param>
    /// <returns>True if Unraid is available, false otherwise</returns>
    public async Task<bool> IsUnraidAvailableAsync(string hostname, string? username = null, string? password = null, int port = 22)
    {
        try
        {
            // Default to root if username not provided
            username ??= "root";
            
            // Connect via SSH
            using var client = new SshClient(hostname, port, username, password ?? string.Empty);
            client.Connect();
            
            // Check if it's really Unraid
            var result = client.RunCommand("test -f /etc/unraid-version && echo 'true' || echo 'false'");
            var isUnraid = result.Result.Trim() == "true";
            
            client.Disconnect();
            return isUnraid;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<List<ContainerInfo>> GetContainersAsync(string hostname, string username, string password)
    {
        var containers = new List<ContainerInfo>();

        try
        {
            // Connect via SSH
            using var client = new SshClient(hostname, username, password);
            client.Connect();

            // Unraid uses Docker, so we can use Docker commands
            var dockerPs = ExecuteCommand(client, "docker ps -a --format '{{.ID}}|{{.Names}}|{{.Image}}|{{.Status}}|{{.Ports}}'");
            
            // Parse output
            var lines = dockerPs.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var parts = line.Split('|');
                if (parts.Length < 5)
                {
                    continue;
                }

                var container = new ContainerInfo
                {
                    Id = parts[0].Trim(),
                    Name = parts[1].Trim(),
                    Image = parts[2].Trim(),
                    IsRunning = parts[3].Trim().StartsWith("Up", StringComparison.OrdinalIgnoreCase)
                };

                // Parse ports
                if (!string.IsNullOrEmpty(parts[4]))
                {
                    var portMappings = parts[4].Split(',', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var portMapping in portMappings)
                    {
                        var portMatch = Regex.Match(portMapping.Trim(), @"(\d+(?:\.\d+)?):(\d+)(?:/(\w+))?");
                        if (portMatch.Success)
                        {
                            var hostPort = int.Parse(portMatch.Groups[1].Value);
                            var containerPort = int.Parse(portMatch.Groups[2].Value);
                            var protocol = portMatch.Groups[3].Success ? portMatch.Groups[3].Value : "tcp";

                            container.Ports.Add(new PortMapping
                            {
                                HostPort = hostPort,
                                ContainerPort = containerPort,
                                Protocol = protocol
                            });
                        }
                    }
                }

                // Get container IP address
                var inspectOutput = ExecuteCommand(client, $"docker inspect --format '{{{{.NetworkSettings.IPAddress}}}}' {container.Id}");
                container.IpAddress = inspectOutput.Trim();

                // Get container labels
                var labelsOutput = ExecuteCommand(client, $"docker inspect --format '{{{{json .Config.Labels}}}}' {container.Id}");
                if (!string.IsNullOrEmpty(labelsOutput) && labelsOutput != "<no value>")
                {
                    try
                    {
                        // Basic parsing of JSON labels (without using System.Text.Json to keep it simple)
                        var labelsMatch = Regex.Match(labelsOutput, @"\""([^""]+)\"":\""([^""]+)\""");
                        while (labelsMatch.Success)
                        {
                            var key = labelsMatch.Groups[1].Value;
                            var value = labelsMatch.Groups[2].Value;
                            container.Labels[key] = value;
                            labelsMatch = labelsMatch.NextMatch();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, $"Error parsing container labels: {container.Id}");
                    }
                }

                // Check if VPN isolated based on network settings or labels
                var networkOutput = ExecuteCommand(client, $"docker inspect --format '{{{{json .NetworkSettings.Networks}}}}' {container.Id}");
                container.IsVpnIsolated = networkOutput.Contains("vpn", StringComparison.OrdinalIgnoreCase) ||
                                         container.Labels.Any(l => l.Key.Contains("vpn", StringComparison.OrdinalIgnoreCase) || 
                                                                 l.Value.Contains("vpn", StringComparison.OrdinalIgnoreCase));

                containers.Add(container);
            }

            client.Disconnect();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, $"Error getting Docker containers from Unraid: {hostname}");
        }

        return containers;
    }

    /// <inheritdoc/>
    public async Task<UnraidInfo> GetSystemInfoAsync(string hostname, string username, string password)
    {
        var unraidInfo = new UnraidInfo
        {
            Hostname = hostname,
            IpAddress = await ResolveHostnameAsync(hostname)
        };

        try
        {
            // Connect via SSH
            using var client = new SshClient(hostname, username, password);
            client.Connect();

            // Get Unraid version
            var versionOutput = ExecuteCommand(client, "cat /etc/unraid-version");
            unraidInfo.Version = versionOutput.Trim();

            // Get CPU info
            var cpuInfo = ExecuteCommand(client, "cat /proc/cpuinfo | grep 'model name' | head -1");
            var cpuMatch = Regex.Match(cpuInfo, @"model name\s+:\s+(.+)");
            if (cpuMatch.Success)
            {
                unraidInfo.CpuModel = cpuMatch.Groups[1].Value.Trim();
            }

            // Get memory info
            var memInfo = ExecuteCommand(client, "free -h | grep 'Mem:'");
            var memParts = memInfo.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (memParts.Length >= 2)
            {
                unraidInfo.TotalMemory = memParts[1];
            }

            // Get Docker status
            var dockerStatus = ExecuteCommand(client, "systemctl is-active docker");
            unraidInfo.DockerEnabled = dockerStatus.Trim() == "active";

            client.Disconnect();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, $"Error getting system info from Unraid: {hostname}");
        }

        return unraidInfo;
    }

    private static string ExecuteCommand(SshClient client, string command)
    {
        var result = client.RunCommand(command);
        return result.Result;
    }

    private static async Task<string> ResolveHostnameAsync(string hostname)
    {
        try
        {
            // Check if input is already an IP address
            if (IPAddress.TryParse(hostname, out _))
            {
                return hostname;
            }

            // Try to resolve hostname
            var hostEntry = await Dns.GetHostEntryAsync(hostname);
            return hostEntry.AddressList.FirstOrDefault()?.ToString() ?? hostname;
        }
        catch
        {
            // If resolution fails, return the original hostname
            return hostname;
        }
    }
}
