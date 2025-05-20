using HomeStack.Core.Interfaces;
using HomeStack.Core.Models;
using Renci.SshNet;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace HomeStack.Scanner;

/// <summary>
/// Implementation of the pfSense scanner
/// </summary>
public class PfSenseScanner : IPfSenseScanner
{
    private readonly ILogger<PfSenseScanner>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PfSenseScanner"/> class
    /// </summary>
    /// <param name="logger">Optional logger</param>
    public PfSenseScanner(ILogger<PfSenseScanner>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Checks if pfSense is available via SSH
    /// </summary>
    /// <param name="hostname">pfSense hostname or IP address</param>
    /// <param name="username">SSH username</param>
    /// <param name="password">SSH password</param>
    /// <param name="port">SSH port (default: 22)</param>
    /// <returns>True if pfSense is available, false otherwise</returns>
    public async Task<bool> IsPfSenseAvailableAsync(string hostname, string? username = null, string? password = null, int port = 22)
    {
        try
        {
            // Default to admin if username not provided
            username ??= "admin";
            
            // Connect via SSH
            using var client = new SshClient(hostname, port, username, password ?? string.Empty);
            client.Connect();
            
            // Check if it's really pfSense
            var result = client.RunCommand("test -f /etc/version && echo 'true' || echo 'false'");
            var isPfSense = result.Result.Trim() == "true";
            
            client.Disconnect();
            return isPfSense;
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// Gets system information from pfSense
    /// </summary>
    /// <param name="hostname">pfSense hostname or IP address</param>
    /// <param name="username">SSH username</param>
    /// <param name="password">SSH password</param>
    /// <returns>pfSense information</returns>
    public async Task<PfSenseInfo> GetSystemInfoAsync(string hostname, string username, string password)
    {
        var pfSenseInfo = new PfSenseInfo
        {
            Hostname = hostname,
            IpAddress = await ResolveHostnameAsync(hostname)
        };

        try
        {
            // Connect via SSH
            using var client = new SshClient(hostname, username, password);
            client.Connect();

            // Get pfSense version
            var versionOutput = ExecuteCommand(client, "cat /etc/version");
            pfSenseInfo.Version = versionOutput.Trim();

            // Get network interfaces
            pfSenseInfo.Interfaces = GetNetworkInterfaces(client);

            client.Disconnect();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, $"Error getting system info from pfSense: {hostname}");
        }

        return pfSenseInfo;
    }

    /// <inheritdoc/>
    public async Task<List<DnsHostOverride>> GetDnsHostOverridesAsync(string hostname, string username, string password)
    {
        var hostOverrides = new List<DnsHostOverride>();

        try
        {
            // Connect via SSH
            using var client = new SshClient(hostname, username, password);
            client.Connect();

            // Get DNS host overrides from pfSense config
            var configXml = ExecuteCommand(client, "cat /conf/config.xml");
            
            // Parse XML
            var xmlDoc = XDocument.Parse(configXml);
            
            // Extract host overrides
            var overrideElements = xmlDoc.Descendants("dnsmasq")
                .Descendants("hosts");

            foreach (var element in overrideElements)
            {
                var hostElement = element.Element("host");
                if (hostElement != null)
                {
                    var hostOverride = new DnsHostOverride
                    {
                        Hostname = hostElement.Element("host")?.Value ?? string.Empty,
                        Domain = hostElement.Element("domain")?.Value ?? string.Empty,
                        IpAddress = hostElement.Element("ip")?.Value ?? string.Empty,
                        Description = hostElement.Element("descr")?.Value ?? string.Empty
                    };
                    
                    hostOverrides.Add(hostOverride);
                }
            }

            client.Disconnect();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, $"Error getting DNS host overrides from pfSense: {hostname}");
        }

        return hostOverrides;
    }

    /// <inheritdoc/>
    public async Task<List<DhcpLease>> GetDhcpLeasesAsync(string hostname, string username, string password)
    {
        var dhcpLeases = new List<DhcpLease>();

        try
        {
            // Connect via SSH
            using var client = new SshClient(hostname, username, password);
            client.Connect();

            // Get DHCP leases from pfSense
            var leasesOutput = ExecuteCommand(client, "cat /var/dhcpd/var/db/dhcpd.leases");
            
            // Parse DHCP leases
            var leaseRegex = new Regex(@"lease\s+(\d+\.\d+\.\d+\.\d+)\s+{([^}]+)}", RegexOptions.Singleline);
            var matches = leaseRegex.Matches(leasesOutput);

            foreach (Match match in matches)
            {
                var ipAddress = match.Groups[1].Value;
                var leaseContent = match.Groups[2].Value;

                var dhcpLease = new DhcpLease
                {
                    IpAddress = ipAddress
                };

                // Extract MAC address
                var macMatch = Regex.Match(leaseContent, @"hardware\s+ethernet\s+([0-9a-f:]+);");
                if (macMatch.Success)
                {
                    dhcpLease.MacAddress = macMatch.Groups[1].Value;
                }

                // Extract hostname
                var hostnameMatch = Regex.Match(leaseContent, @"client-hostname\s+""([^""]+)"";");
                if (hostnameMatch.Success)
                {
                    dhcpLease.Hostname = hostnameMatch.Groups[1].Value;
                }

                // Extract start time
                var startMatch = Regex.Match(leaseContent, @"starts\s+\d+\s+([^;]+);");
                if (startMatch.Success)
                {
                    if (DateTime.TryParse(startMatch.Groups[1].Value, out var startTime))
                    {
                        dhcpLease.Start = startTime;
                    }
                }

                // Extract end time
                var endMatch = Regex.Match(leaseContent, @"ends\s+\d+\s+([^;]+);");
                if (endMatch.Success)
                {
                    if (DateTime.TryParse(endMatch.Groups[1].Value, out var endTime))
                    {
                        dhcpLease.End = endTime;
                    }
                }

                dhcpLeases.Add(dhcpLease);
            }

            // Get static mappings from config.xml
            var configXml = ExecuteCommand(client, "cat /conf/config.xml");
            var xmlDoc = XDocument.Parse(configXml);
            
            var staticMappings = xmlDoc.Descendants("dhcpd")
                .Descendants("staticmap");

            foreach (var mapping in staticMappings)
            {
                var staticLease = new DhcpLease
                {
                    IpAddress = mapping.Element("ipaddr")?.Value ?? string.Empty,
                    MacAddress = mapping.Element("mac")?.Value ?? string.Empty,
                    Hostname = mapping.Element("hostname")?.Value ?? string.Empty,
                    Description = mapping.Element("descr")?.Value ?? string.Empty,
                    IsStatic = true
                };
                
                dhcpLeases.Add(staticLease);
            }

            // Check which IPs are online
            foreach (var lease in dhcpLeases)
            {
                var pingOutput = ExecuteCommand(client, $"ping -c 1 -W 1 {lease.IpAddress}");
                lease.IsOnline = !pingOutput.Contains("100% packet loss");
            }

            client.Disconnect();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, $"Error getting DHCP leases from pfSense: {hostname}");
        }

        return dhcpLeases;
    }

    /// <inheritdoc/>
    public async Task<bool> UpdateDnsHostOverrideAsync(string hostname, string username, string password, DnsHostOverride hostOverride)
    {
        try
        {
            // Connect via SSH
            using var client = new SshClient(hostname, username, password);
            client.Connect();

            // Get current config
            var configXml = ExecuteCommand(client, "cat /conf/config.xml");
            var xmlDoc = XDocument.Parse(configXml);

            // Find existing host override or create new one
            var dnsmasqElement = xmlDoc.Descendants("dnsmasq").FirstOrDefault();
            if (dnsmasqElement == null)
            {
                _logger?.LogError(null, "dnsmasq element not found in pfSense config");
                return false;
            }

            var hostsElement = dnsmasqElement.Element("hosts");
            if (hostsElement == null)
            {
                hostsElement = new XElement("hosts");
                dnsmasqElement.Add(hostsElement);
            }

            // Find existing host entry with same hostname
            var existingHost = hostsElement.Elements("host")
                .FirstOrDefault(h => h.Element("host")?.Value == hostOverride.Hostname && 
                                     h.Element("domain")?.Value == hostOverride.Domain);

            if (existingHost != null)
            {
                // Update existing host
                existingHost.Element("ip")!.Value = hostOverride.IpAddress;
                existingHost.Element("descr")!.Value = hostOverride.Description;
            }
            else
            {
                // Create new host entry
                var newHost = new XElement("host",
                    new XElement("host", hostOverride.Hostname),
                    new XElement("domain", hostOverride.Domain),
                    new XElement("ip", hostOverride.IpAddress),
                    new XElement("descr", hostOverride.Description)
                );
                
                hostsElement.Add(newHost);
            }

            // Save modified config to a temporary file
            var tempPath = "/tmp/config.xml.tmp";
            client.RunCommand($"echo '{xmlDoc}' > {tempPath}");

            // Backup current config
            client.RunCommand("cp /conf/config.xml /conf/config.xml.bak");

            // Replace config
            client.RunCommand($"cp {tempPath} /conf/config.xml");

            // Reload DNS service
            client.RunCommand("pfSsh.php playback svc restart dnsmasq");

            client.Disconnect();
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, $"Error updating DNS host override on pfSense: {hostname}");
            return false;
        }
    }

    private static List<NetworkInterface> GetNetworkInterfaces(SshClient client)
    {
        var interfaces = new List<NetworkInterface>();

        try
        {
            // Get interface information
            var interfaceOutput = ExecuteCommand(client, "ifconfig -a");
            
            // Parse interfaces
            var interfaceRegex = new Regex(@"^([a-z0-9]+):", RegexOptions.Multiline);
            var matches = interfaceRegex.Matches(interfaceOutput);

            foreach (Match match in matches)
            {
                var interfaceName = match.Groups[1].Value;
                
                // Get details for this interface
                var interfaceDetails = ExecuteCommand(client, $"ifconfig {interfaceName}");
                
                var networkInterface = new NetworkInterface
                {
                    Name = interfaceName,
                    IsUp = interfaceDetails.Contains("UP"),
                    IsEnabled = !interfaceDetails.Contains("DISABLED")
                };

                // Extract IP address
                var ipMatch = Regex.Match(interfaceDetails, @"inet\s+(\d+\.\d+\.\d+\.\d+)");
                if (ipMatch.Success)
                {
                    networkInterface.IpAddress = ipMatch.Groups[1].Value;
                }

                // Extract subnet mask
                var maskMatch = Regex.Match(interfaceDetails, @"netmask\s+(\w+)");
                if (maskMatch.Success)
                {
                    networkInterface.SubnetMask = maskMatch.Groups[1].Value;
                }

                // Extract MAC address
                var macMatch = Regex.Match(interfaceDetails, @"ether\s+([0-9a-f:]+)");
                if (macMatch.Success)
                {
                    networkInterface.MacAddress = macMatch.Groups[1].Value;
                }

                // Extract description from config.xml
                var configOutput = ExecuteCommand(client, "cat /conf/config.xml");
                var descMatch = Regex.Match(configOutput, $@"<{interfaceName}>.*?<descr>(.*?)</descr>", RegexOptions.Singleline);
                if (descMatch.Success)
                {
                    networkInterface.Description = descMatch.Groups[1].Value;
                }

                interfaces.Add(networkInterface);
            }
        }
        catch (Exception ex)
        {
            // Log error but continue with other interfaces
            Console.WriteLine($"Error getting network interfaces: {ex.Message}");
        }

        return interfaces;
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