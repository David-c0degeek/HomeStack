using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Text.Json;
using HomeStack.Core.Models;
using HomeStack.Scanner.Docker;
using HomeStack.Scanner.Services;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;

namespace HomeStack.Cli;

internal class Program
{
    static async Task<int> Main(string[] args)
    {
        // Setup dependency injection
        var services = ConfigureServices();
        
        // Setup the root command
        var rootCommand = new RootCommand("HomeStack - A tool for homelab discovery and configuration");
        
        // Add the scan command
        var scanCommand = CreateScanCommand();
        rootCommand.AddCommand(scanCommand);
        
        // Add the config command
        var configCommand = CreateConfigCommand();
        rootCommand.AddCommand(configCommand);
        
        // Execute the command pipeline
        return await rootCommand.InvokeAsync(args);
    }

    private static ServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();
        
        // Register scanners
        services.AddSingleton<DockerScanner>();
        services.AddSingleton<Core.Interfaces.IDockerScanner>(sp => sp.GetRequiredService<DockerScanner>());
        
        // Register scan service
        services.AddSingleton<ScanService>();
        services.AddSingleton<Core.Interfaces.IScanService>(sp => sp.GetRequiredService<ScanService>());
        
        return services.BuildServiceProvider();
    }

    private static Command CreateScanCommand()
    {
        var scanCommand = new Command("scan", "Scan network for devices and services");
        
        // Add common options
        var outputOption = new Option<string>(
            aliases: new[] { "--output", "-o" },
            description: "Output file for scan results");
        
        // Add Docker options
        var dockerOption = new Option<bool>(
            aliases: new[] { "--docker", "-d" },
            description: "Scan Docker containers");
        var dockerHostOption = new Option<string?>(
            "--docker-host",
            description: "Docker host URL (default: local Docker socket)");
        var dockerCertOption = new Option<string?>(
            "--docker-cert",
            description: "Path to Docker TLS certificates");
        var dockerApiOption = new Option<string?>(
            "--docker-api",
            description: "Docker API version");
        
        // Add Unraid options
        var unraidOption = new Option<bool>(
            aliases: new[] { "--unraid", "-u" },
            description: "Scan Unraid server");
        var unraidHostOption = new Option<string?>(
            "--unraid-host",
            description: "Unraid host IP or hostname");
        var unraidUserOption = new Option<string>(
            "--unraid-user",
            getDefaultValue: () => "root",
            description: "Unraid SSH username");
        var unraidPassOption = new Option<string?>(
            "--unraid-pass",
            description: "Unraid SSH password");
        var unraidPortOption = new Option<int>(
            "--unraid-port",
            getDefaultValue: () => 22,
            description: "Unraid SSH port");
        
        // Add pfSense options
        var pfSenseOption = new Option<bool>(
            aliases: new[] { "--pfsense", "-p" },
            description: "Scan pfSense router");
        var pfSenseHostOption = new Option<string?>(
            "--pfsense-host",
            description: "pfSense host IP or hostname");
        var pfSenseUserOption = new Option<string?>(
            "--pfsense-user",
            description: "pfSense SSH username");
        var pfSensePassOption = new Option<string?>(
            "--pfsense-pass",
            description: "pfSense SSH password");
        var pfSensePortOption = new Option<int>(
            "--pfsense-port",
            getDefaultValue: () => 22,
            description: "pfSense SSH port");
        
        // Add all options to the command
        scanCommand.AddOption(outputOption);
        scanCommand.AddOption(dockerOption);
        scanCommand.AddOption(dockerHostOption);
        scanCommand.AddOption(dockerCertOption);
        scanCommand.AddOption(dockerApiOption);
        scanCommand.AddOption(unraidOption);
        scanCommand.AddOption(unraidHostOption);
        scanCommand.AddOption(unraidUserOption);
        scanCommand.AddOption(unraidPassOption);
        scanCommand.AddOption(unraidPortOption);
        scanCommand.AddOption(pfSenseOption);
        scanCommand.AddOption(pfSenseHostOption);
        scanCommand.AddOption(pfSenseUserOption);
        scanCommand.AddOption(pfSensePassOption);
        scanCommand.AddOption(pfSensePortOption);
        
        // Add command handler
        scanCommand.Handler = CommandHandler.Create<ScanCommandOptions>(ExecuteScanCommand);
        
        return scanCommand;
    }

    private static Command CreateConfigCommand()
    {
        var configCommand = new Command("config", "Generate configurations based on scan results");
        
        // Add options
        var inputOption = new Option<string>(
            aliases: new[] { "--input", "-i" },
            description: "Input file with scan results")
        { IsRequired = true };
        
        var typesOption = new Option<string[]>(
            aliases: new[] { "--types", "-t" },
            description: "Configuration types to generate (dns, nginx, caddy, flame, homepage)")
        { IsRequired = true, AllowMultipleArgumentsPerToken = true };
        
        var domainOption = new Option<string?>(
            aliases: new[] { "--domain", "-d" },
            description: "Domain suffix for hostnames");
        
        var outputDirOption = new Option<string?>(
            aliases: new[] { "--output-dir", "-o" },
            description: "Output directory for configuration files");
        
        var sslOption = new Option<bool>(
            "--ssl",
            description: "Enable SSL for proxy configurations");
        
        var authOption = new Option<bool>(
            "--auth",
            description: "Enable authentication for proxy configurations");
        
        // Add options to command
        configCommand.AddOption(inputOption);
        configCommand.AddOption(typesOption);
        configCommand.AddOption(domainOption);
        configCommand.AddOption(outputDirOption);
        configCommand.AddOption(sslOption);
        configCommand.AddOption(authOption);
        
        // Add command handler
        configCommand.Handler = CommandHandler.Create<ConfigCommandOptions>(ExecuteConfigCommand);
        
        return configCommand;
    }

    private static async Task<int> ExecuteScanCommand(ScanCommandOptions options)
    {
        try
        {
            // Display banner
            AnsiConsole.Write(
                new FigletText("HomeStack")
                    .Color(Color.Green));
            AnsiConsole.WriteLine("Homelab Scanner Tool");
            AnsiConsole.WriteLine();
            
            // Check if at least one scanner is enabled
            if (!options.Docker && !options.Unraid && !options.PfSense)
            {
                AnsiConsole.MarkupLine("[red]Error:[/] At least one scanner option must be enabled (--docker, --unraid, or --pfsense)");
                return 1;
            }
            
            // Validation for Unraid options
            if (options.Unraid && string.IsNullOrEmpty(options.UnraidHost))
            {
                AnsiConsole.MarkupLine("[red]Error:[/] Unraid host is required when Unraid scanning is enabled");
                return 1;
            }
            
            // Validation for pfSense options
            if (options.PfSense && string.IsNullOrEmpty(options.PfSenseHost))
            {
                AnsiConsole.MarkupLine("[red]Error:[/] pfSense host is required when pfSense scanning is enabled");
                return 1;
            }
            
            // Show scan options
            AnsiConsole.WriteLine("Scan options:");
            if (options.Docker)
                AnsiConsole.MarkupLine("- [green]Docker:[/] Enabled" + (string.IsNullOrEmpty(options.DockerHost) ? " (local socket)" : $" ({options.DockerHost})"));
            if (options.Unraid)
                AnsiConsole.MarkupLine($"- [green]Unraid:[/] {options.UnraidHost}");
            if (options.PfSense)
                AnsiConsole.MarkupLine($"- [green]pfSense:[/] {options.PfSenseHost}");
            AnsiConsole.WriteLine();
            
            // Create Docker scanner
            var dockerScanner = new DockerScanner();
            
            // Check if Docker is available
            if (options.Docker)
            {
                AnsiConsole.Status()
                    .Start("Checking Docker availability...", ctx =>
                    {
                        bool isAvailable = dockerScanner.IsDockerAvailableAsync(
                            options.DockerHost, options.DockerCert, options.DockerApi).GetAwaiter().GetResult();
                        
                        if (!isAvailable)
                        {
                            ctx.Status("Docker is not available");
                            throw new Exception("Docker is not available. Please check the Docker daemon or connection settings.");
                        }
                        
                        ctx.Status("Docker is available");
                    });
            }
            
            // Create scan service
            var scanService = new ScanService(dockerScanner);
            
            // Perform the scan
            var scanResults = await AnsiConsole.Status()
                .StartAsync("Scanning...", async ctx =>
                {
                    var results = await scanService.ScanAllAsync(
                        options.Docker,
                        options.Unraid,
                        options.PfSense,
                        options.UnraidHost,
                        options.UnraidUser,
                        options.UnraidPass,
                        options.UnraidPort,
                        options.PfSenseHost,
                        options.PfSenseUser,
                        options.PfSensePass,
                        options.PfSensePort,
                        options.DockerHost,
                        options.DockerCert,
                        options.DockerApi);
                    
                    ctx.Status("Scan completed");
                    return results;
                });
            
            // Display scan results summary
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[green]Scan completed successfully.[/]");
            
            // Display errors and warnings if any
            if (scanResults.Errors.Count > 0)
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[red]Errors:[/]");
                foreach (var error in scanResults.Errors)
                {
                    AnsiConsole.MarkupLine($"- [red]{error}[/]");
                }
            }
            
            if (scanResults.Warnings.Count > 0)
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[yellow]Warnings:[/]");
                foreach (var warning in scanResults.Warnings)
                {
                    AnsiConsole.MarkupLine($"- [yellow]{warning}[/]");
                }
            }
            
            // Display scan results
            var table = new Table()
                .Title("Scan Results Summary")
                .BorderColor(Color.Green)
                .Border(TableBorder.Rounded);
            
            table.AddColumn("Category");
            table.AddColumn("Count");
            
            table.AddRow("Docker Containers", scanResults.Containers.Count.ToString());
            table.AddRow("DNS Host Overrides", scanResults.DnsHostOverrides.Count.ToString());
            table.AddRow("DHCP Leases", scanResults.DhcpLeases.Count.ToString());
            table.AddRow("Network Interfaces", scanResults.NetworkInterfaces.Count.ToString());
            table.AddRow("Proxy Configurations", scanResults.ProxyConfigs.Count.ToString());
            
            AnsiConsole.Write(table);
            
            // Output results to file if specified
            if (!string.IsNullOrEmpty(options.Output))
            {
                var json = JsonSerializer.Serialize(scanResults, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                
                await File.WriteAllTextAsync(options.Output, json);
                AnsiConsole.MarkupLine($"[green]Scan results saved to:[/] {options.Output}");
            }
            
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }
    }

    private static async Task<int> ExecuteConfigCommand(ConfigCommandOptions options)
    {
        try
        {
            // Display banner
            AnsiConsole.Write(
                new FigletText("HomeStack")
                    .Color(Color.Green));
            AnsiConsole.WriteLine("Homelab Configuration Generator");
            AnsiConsole.WriteLine();
            
            // Check if input file exists
            if (!File.Exists(options.Input))
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] Input file '{options.Input}' not found");
                return 1;
            }
            
            // Load scan results from input file
            var json = await File.ReadAllTextAsync(options.Input);
            var scanResults = JsonSerializer.Deserialize<ScanResults>(json);
            
            if (scanResults == null)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] Failed to parse scan results from '{options.Input}'");
                return 1;
            }
            
            // Create output directory if specified and not exists
            if (!string.IsNullOrEmpty(options.OutputDir) && !Directory.Exists(options.OutputDir))
            {
                Directory.CreateDirectory(options.OutputDir);
            }
            
            // Generate configurations for each specified type
            foreach (var type in options.Types)
            {
                switch (type.ToLower())
                {
                    case "dns":
                        await GenerateDnsConfig(scanResults, options);
                        break;
                    case "nginx":
                        await GenerateNginxConfig(scanResults, options);
                        break;
                    case "caddy":
                        await GenerateCaddyConfig(scanResults, options);
                        break;
                    case "flame":
                        await GenerateFlameConfig(scanResults, options);
                        break;
                    case "homepage":
                        await GenerateHomepageConfig(scanResults, options);
                        break;
                    default:
                        AnsiConsole.MarkupLine($"[yellow]Warning:[/] Unknown configuration type '{type}'");
                        break;
                }
            }
            
            AnsiConsole.MarkupLine("[green]Configuration generation completed.[/]");
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }
    }

    private static async Task GenerateDnsConfig(ScanResults scanResults, ConfigCommandOptions options)
    {
        AnsiConsole.Status()
            .Start("Generating DNS configuration...", ctx =>
            {
                // Mock implementation for demo
                var config = GenerateMockDnsConfig(scanResults, options.Domain);
                
                // Write to file
                var outputPath = GetOutputPath(options.OutputDir, "dns_overrides.conf");
                File.WriteAllText(outputPath, config);
                
                ctx.Status($"DNS configuration saved to: {outputPath}");
                Thread.Sleep(500); // Simulate work
            });
    }

    private static async Task GenerateNginxConfig(ScanResults scanResults, ConfigCommandOptions options)
    {
        AnsiConsole.Status()
            .Start("Generating Nginx configuration...", ctx =>
            {
                // Mock implementation for demo
                var config = GenerateMockNginxConfig(scanResults, options.Domain, options.Ssl);
                
                // Write to file
                var outputPath = GetOutputPath(options.OutputDir, "nginx.conf");
                File.WriteAllText(outputPath, config);
                
                ctx.Status($"Nginx configuration saved to: {outputPath}");
                Thread.Sleep(500); // Simulate work
            });
    }

    private static async Task GenerateCaddyConfig(ScanResults scanResults, ConfigCommandOptions options)
    {
        AnsiConsole.Status()
            .Start("Generating Caddy configuration...", ctx =>
            {
                // Mock implementation for demo
                var config = GenerateMockCaddyConfig(scanResults, options.Domain, options.Ssl);
                
                // Write to file
                var outputPath = GetOutputPath(options.OutputDir, "Caddyfile");
                File.WriteAllText(outputPath, config);
                
                ctx.Status($"Caddy configuration saved to: {outputPath}");
                Thread.Sleep(500); // Simulate work
            });
    }

    private static async Task GenerateFlameConfig(ScanResults scanResults, ConfigCommandOptions options)
    {
        AnsiConsole.Status()
            .Start("Generating Flame dashboard configuration...", ctx =>
            {
                // Mock implementation for demo
                var config = GenerateMockFlameConfig(scanResults, options.Domain);
                
                // Write to file
                var outputPath = GetOutputPath(options.OutputDir, "flame_apps.json");
                File.WriteAllText(outputPath, config);
                
                ctx.Status($"Flame configuration saved to: {outputPath}");
                Thread.Sleep(500); // Simulate work
            });
    }

    private static async Task GenerateHomepageConfig(ScanResults scanResults, ConfigCommandOptions options)
    {
        AnsiConsole.Status()
            .Start("Generating Homepage dashboard configuration...", ctx =>
            {
                // Mock implementation for demo
                var config = GenerateMockHomepageConfig(scanResults, options.Domain);
                
                // Write to file
                var outputPath = GetOutputPath(options.OutputDir, "homepage_services.yaml");
                File.WriteAllText(outputPath, config);
                
                ctx.Status($"Homepage configuration saved to: {outputPath}");
                Thread.Sleep(500); // Simulate work
            });
    }

    private static string GetOutputPath(string? outputDir, string filename)
    {
        return string.IsNullOrEmpty(outputDir) 
            ? filename 
            : Path.Combine(outputDir, filename);
    }

    #region Mock Config Generators
    
    private static string GenerateMockDnsConfig(ScanResults scanResults, string? domain)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("# pfSense DNS Host Overrides");
        sb.AppendLine("# Generated by HomeStack");
        sb.AppendLine("# " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        sb.AppendLine();
        
        foreach (var container in scanResults.Containers)
        {
            var hostname = container.Name.ToLower();
            sb.AppendLine($"{container.IpAddress} {hostname}{(string.IsNullOrEmpty(domain) ? "" : "." + domain)} # {container.Image}");
        }
        
        foreach (var dnsEntry in scanResults.DnsHostOverrides)
        {
            if (!scanResults.Containers.Any(c => c.Name.Equals(dnsEntry.Hostname, StringComparison.OrdinalIgnoreCase)))
            {
                sb.AppendLine($"{dnsEntry.IpAddress} {dnsEntry.Fqdn} # {dnsEntry.Description}");
            }
        }
        
        return sb.ToString();
    }
    
    private static string GenerateMockNginxConfig(ScanResults scanResults, string? domain, bool ssl)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("# Nginx configuration for reverse proxy");
        sb.AppendLine("# Generated by HomeStack");
        sb.AppendLine("# " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        sb.AppendLine();
        
        foreach (var container in scanResults.Containers)
        {
            var hostname = container.Name.ToLower();
            var fqdn = string.IsNullOrEmpty(domain) ? hostname : $"{hostname}.{domain}";
            
            if (container.PortMappings.Count > 0)
            {
                sb.AppendLine($"server {{");
                sb.AppendLine($"    listen 80;");
                
                if (ssl)
                {
                    sb.AppendLine($"    listen 443 ssl;");
                    sb.AppendLine($"    ssl_certificate /etc/ssl/certs/{fqdn}.crt;");
                    sb.AppendLine($"    ssl_certificate_key /etc/ssl/private/{fqdn}.key;");
                }
                
                sb.AppendLine($"    server_name {fqdn};");
                sb.AppendLine();
                
                // Redirect HTTP to HTTPS if SSL is enabled
                if (ssl)
                {
                    sb.AppendLine($"    if ($scheme = http) {{");
                    sb.AppendLine($"        return 301 https://$host$request_uri;");
                    sb.AppendLine($"    }}");
                    sb.AppendLine();
                }
                
                sb.AppendLine($"    location / {{");
                
                var port = container.PortMappings.FirstOrDefault()?.ContainerPort ?? 80;
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
    
    private static string GenerateMockCaddyConfig(ScanResults scanResults, string? domain, bool ssl)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("# Caddy configuration for reverse proxy");
        sb.AppendLine("# Generated by HomeStack");
        sb.AppendLine("# " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        sb.AppendLine();
        
        foreach (var container in scanResults.Containers)
        {
            var hostname = container.Name.ToLower();
            var fqdn = string.IsNullOrEmpty(domain) ? hostname : $"{hostname}.{domain}";
            
            if (container.PortMappings.Count > 0)
            {
                var port = container.PortMappings.FirstOrDefault()?.ContainerPort ?? 80;
                
                sb.AppendLine($"{fqdn} {{");
                
                if (!ssl)
                {
                    sb.AppendLine($"    tls internal");
                }
                
                sb.AppendLine($"    reverse_proxy {container.IpAddress}:{port} {{");
                sb.AppendLine($"        header_up Host {{host}}");
                sb.AppendLine($"        header_up X-Real-IP {{remote}}");
                sb.AppendLine($"        header_up X-Forwarded-For {{remote}}");
                sb.AppendLine($"        header_up X-Forwarded-Proto {{scheme}}");
                sb.AppendLine($"    }}");
                sb.AppendLine($"}}");
                sb.AppendLine();
            }
        }
        
        return sb.ToString();
    }
    
    private static string GenerateMockFlameConfig(ScanResults scanResults, string? domain)
    {
        var apps = new List<object>();
        
        foreach (var container in scanResults.Containers)
        {
            var hostname = container.Name.ToLower();
            var fqdn = string.IsNullOrEmpty(domain) ? hostname : $"{hostname}.{domain}";
            
            if (container.PortMappings.Count > 0)
            {
                apps.Add(new
                {
                    name = container.Name,
                    url = $"http://{fqdn}",
                    icon = "https://raw.githubusercontent.com/pawelmalak/flame/master/public/icons/apps.svg",
                    description = $"{container.Image} container"
                });
            }
        }
        
        return JsonSerializer.Serialize(new { apps }, new JsonSerializerOptions { WriteIndented = true });
    }
    
    private static string GenerateMockHomepageConfig(ScanResults scanResults, string? domain)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("# Homepage dashboard configuration");
        sb.AppendLine("# Generated by HomeStack");
        sb.AppendLine("# " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        sb.AppendLine();
        
        sb.AppendLine("services:");
        
        foreach (var container in scanResults.Containers)
        {
            var hostname = container.Name.ToLower();
            var fqdn = string.IsNullOrEmpty(domain) ? hostname : $"{hostname}.{domain}";
            
            if (container.PortMappings.Count > 0)
            {
                sb.AppendLine($"  - name: {container.Name}");
                sb.AppendLine($"    icon: docker");
                sb.AppendLine($"    url: http://{fqdn}");
                sb.AppendLine($"    healthCheck: http://{fqdn}");
                sb.AppendLine($"    description: {container.Image} container");
                sb.AppendLine();
            }
        }
        
        return sb.ToString();
    }
    
    #endregion
}

/// <summary>
/// Command line options for the scan command
/// </summary>
public class ScanCommandOptions
{
    public string? Output { get; set; }
    
    public bool Docker { get; set; }
    public string? DockerHost { get; set; }
    public string? DockerCert { get; set; }
    public string? DockerApi { get; set; }
    
    public bool Unraid { get; set; }
    public string? UnraidHost { get; set; }
    public string UnraidUser { get; set; } = "root";
    public string? UnraidPass { get; set; }
    public int UnraidPort { get; set; } = 22;
    
    public bool PfSense { get; set; }
    public string? PfSenseHost { get; set; }
    public string? PfSenseUser { get; set; }
    public string? PfSensePass { get; set; }
    public int PfSensePort { get; set; } = 22;
}

/// <summary>
/// Command line options for the config command
/// </summary>
public class ConfigCommandOptions
{
    public string Input { get; set; } = string.Empty;
    public string[] Types { get; set; } = Array.Empty<string>();
    public string? Domain { get; set; }
    public string? OutputDir { get; set; }
    public bool Ssl { get; set; }
    public bool Auth { get; set; }
}