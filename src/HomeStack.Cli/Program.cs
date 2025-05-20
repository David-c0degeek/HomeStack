using System.CommandLine;
using System.Text.Json;
using System.Text.Json.Serialization;
using HomeStack.Core.Interfaces;
using HomeStack.Core.Models;
using HomeStack.Scanner;
using Microsoft.Extensions.DependencyInjection;

namespace HomeStack.Cli;

/// <summary>
/// Main entry point for the HomeStack CLI application
/// </summary>
internal static class Program
{
    /// <summary>
    /// Application entry point
    /// </summary>
    /// <param name="args">Command line arguments</param>
    /// <returns>Exit code</returns>
    private static async Task<int> Main(string[] args)
    {
        // Create root command
        var rootCommand = new RootCommand("HomeStack - Homelab service management tool");
        
        // Add scan command
        rootCommand.AddCommand(BuildScanCommand());
        
        // Add config command
        rootCommand.AddCommand(BuildConfigCommand());

        // Parse and execute
        return await rootCommand.InvokeAsync(args);
    }
    
    /// <summary>
    /// Builds the scan command
    /// </summary>
    /// <returns>Configured scan command</returns>
    private static Command BuildScanCommand()
    {
        // Create scan command
        var scanCommand = new Command("scan", "Scan local network and services");
        
        // Add options
        var dockerOption = new Option<bool>("--docker", "Scan Docker containers");
        dockerOption.AddAlias("-d");
        scanCommand.AddOption(dockerOption);
        
        var unraidOption = new Option<bool>("--unraid", "Scan Unraid server");
        unraidOption.AddAlias("-u");
        scanCommand.AddOption(unraidOption);
        
        var pfsenseOption = new Option<bool>("--pfsense", "Scan pfSense router");
        pfsenseOption.AddAlias("-p");
        scanCommand.AddOption(pfsenseOption);
        
        var unraidHostOption = new Option<string?>("--unraid-host", "Unraid hostname or IP");
        scanCommand.AddOption(unraidHostOption);
        
        var unraidUserOption = new Option<string?>("--unraid-user", "Unraid SSH username");
        scanCommand.AddOption(unraidUserOption);
        
        var unraidPassOption = new Option<string?>("--unraid-pass", "Unraid SSH password");
        scanCommand.AddOption(unraidPassOption);
        
        var pfsenseHostOption = new Option<string?>("--pfsense-host", "pfSense hostname or IP");
        scanCommand.AddOption(pfsenseHostOption);
        
        var pfsenseUserOption = new Option<string?>("--pfsense-user", "pfSense SSH username");
        scanCommand.AddOption(pfsenseUserOption);
        
        var pfsensePassOption = new Option<string?>("--pfsense-pass", "pfSense SSH password");
        scanCommand.AddOption(pfsensePassOption);
        
        var outputOption = new Option<FileInfo?>("--output", "Output JSON file");
        outputOption.AddAlias("-o");
        scanCommand.AddOption(outputOption);
        
        var prettyOption = new Option<bool>("--pretty", "Pretty-print JSON output");
        scanCommand.AddOption(prettyOption);

        scanCommand.SetHandler(async (context) =>
        {
            var docker = context.ParseResult.GetValueForOption(dockerOption);
            var unraid = context.ParseResult.GetValueForOption(unraidOption);
            var pfsense = context.ParseResult.GetValueForOption(pfsenseOption);
            var unraidHost = context.ParseResult.GetValueForOption(unraidHostOption);
            var unraidUser = context.ParseResult.GetValueForOption(unraidUserOption);
            var unraidPass = context.ParseResult.GetValueForOption(unraidPassOption);
            var pfsenseHost = context.ParseResult.GetValueForOption(pfsenseHostOption);
            var pfsenseUser = context.ParseResult.GetValueForOption(pfsenseUserOption);
            var pfsensePass = context.ParseResult.GetValueForOption(pfsensePassOption);
            var output = context.ParseResult.GetValueForOption(outputOption);
            var pretty = context.ParseResult.GetValueForOption(prettyOption);
            
            await ExecuteScanCommand(
                docker, unraid, pfsense,
                unraidHost, unraidUser, unraidPass,
                pfsenseHost, pfsenseUser, pfsensePass,
                output, pretty);
        });
        
        return scanCommand;
    }
    
    /// <summary>
    /// Builds the config command
    /// </summary>
    /// <returns>Configured config command</returns>
    private static Command BuildConfigCommand()
    {
        // Create a config command
        var configCommand = new Command("config", "Generate configuration files from scan results");
        
        var inputOption = new Option<FileInfo>("--input", "Input JSON file from a previous scan") { IsRequired = true };
        inputOption.AddAlias("-i");
        configCommand.AddOption(inputOption);
        
        var typesOption = new Option<string[]>("--types", "Configuration types to generate (dns, nginx, caddy, flame, homepage)") { IsRequired = true };
        typesOption.AddAlias("-t");
        configCommand.AddOption(typesOption);
        
        var domainOption = new Option<string?>("--domain", "Domain suffix for DNS entries");
        domainOption.AddAlias("-d");
        configCommand.AddOption(domainOption);
        
        var outputDirOption = new Option<DirectoryInfo?>("--output-dir", "Output directory for configuration files");
        outputDirOption.AddAlias("-o");
        configCommand.AddOption(outputDirOption);
        
        configCommand.SetHandler(async (context) =>
        {
            var input = context.ParseResult.GetValueForOption(inputOption);
            var types = context.ParseResult.GetValueForOption(typesOption);
            var domain = context.ParseResult.GetValueForOption(domainOption);
            var outputDir = context.ParseResult.GetValueForOption(outputDirOption);
            
            await ExecuteConfigCommand(input, types, domain, outputDir);
        });
        
        return configCommand;
    }
    
    /// <summary>
    /// Executes the scan command
    /// </summary>
    private static async Task ExecuteScanCommand(
        bool docker, bool unraid, bool pfsense,
        string? unraidHost, string? unraidUser, string? unraidPass,
        string? pfsenseHost, string? pfsenseUser, string? pfsensePass,
        FileInfo? output, bool pretty)
    {
        // Add local services to container
        var serviceProvider = BuildServiceProvider();

        Console.WriteLine("HomeStack - Network Scanner");
        Console.WriteLine("---------------------------");

        var scanService = serviceProvider.GetRequiredService<IScanService>();
        
        (string hostname, string username, string password)? unraidSettings = null;
        if (unraid && !string.IsNullOrEmpty(unraidHost))
        {
            unraidSettings = (unraidHost, unraidUser ?? "root", unraidPass ?? string.Empty);
            Console.WriteLine($"Unraid settings: {unraidSettings.Value.hostname}@{unraidSettings.Value.username}");
        }
        
        (string hostname, string username, string password)? pfsenseSettings = null;
        if (pfsense && !string.IsNullOrEmpty(pfsenseHost))
        {
            pfsenseSettings = (pfsenseHost, pfsenseUser ?? "admin", pfsensePass ?? string.Empty);
            Console.WriteLine($"pfSense settings: {pfsenseSettings.Value.hostname}@{pfsenseSettings.Value.username}");
        }

        Console.WriteLine("Starting scan...");
        var results = await scanService.ScanNetworkAsync(
            docker,
            unraid && unraidSettings.HasValue,
            pfsense && pfsenseSettings.HasValue,
            unraidSettings,
            pfsenseSettings);

        Console.WriteLine("Scan complete!");
        Console.WriteLine($"Found {results.Containers.Count} containers");
        if (results.UnraidInfo != null)
        {
            Console.WriteLine($"Unraid: {results.UnraidInfo.Hostname} ({results.UnraidInfo.Version})");
        }
        if (results.PfSenseInfo != null)
        {
            Console.WriteLine($"pfSense: {results.PfSenseInfo.Hostname} ({results.PfSenseInfo.Version})");
            Console.WriteLine($"DNS overrides: {results.PfSenseInfo.DnsHostOverrides.Count}");
            Console.WriteLine($"DHCP leases: {results.PfSenseInfo.DhcpLeases.Count}");
        }

        // Check service health
        Console.WriteLine("Checking service health...");
        var healthResults = await scanService.CheckServiceHealthAsync(results);
        
        var healthySummary = healthResults.Count(h => h.Value.Status == ServiceStatus.Healthy);
        var unhealthySummary = healthResults.Count(h => h.Value.Status == ServiceStatus.Unhealthy);
        Console.WriteLine($"Health check: {healthySummary} healthy, {unhealthySummary} unhealthy");
        
        // Output results
        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = pretty,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };
        
        var json = JsonSerializer.Serialize(results, jsonOptions);
        
        if (output != null)
        {
            await File.WriteAllTextAsync(output.FullName, json);
            Console.WriteLine($"Results saved to {output.FullName}");
        }
        else
        {
            Console.WriteLine(json);
        }
    }
    
    /// <summary>
    /// Executes the config command
    /// </summary>
    private static async Task ExecuteConfigCommand(
        FileInfo input, string[]? types, string? domain, DirectoryInfo? outputDir)
    {
        // Add local services to container
        var serviceProvider = BuildServiceProvider();
        
        Console.WriteLine("HomeStack - Configuration Generator");
        Console.WriteLine("----------------------------------");
        
        if (!input.Exists)
        {
            Console.WriteLine($"Error: Input file not found: {input?.FullName}");
            return;
        }
        
        try
        {
            // Read scan results from file
            var json = await File.ReadAllTextAsync(input.FullName);
            var results = JsonSerializer.Deserialize<ScanResults>(json);
            
            if (results == null)
            {
                Console.WriteLine("Error: Failed to parse scan results");
                return;
            }
            
            Console.WriteLine($"Loaded scan results from {input.FullName}");
            Console.WriteLine($"Scan timestamp: {results.ScanTimestamp}");
            Console.WriteLine($"Containers: {results.Containers.Count}");
            
            // Generate configurations
            var scanService = serviceProvider.GetRequiredService<IScanService>();
            var configs = await scanService.GenerateConfigurationsAsync(results, types ?? Array.Empty<string>(), domain);
            
            Console.WriteLine($"Generated {configs.Count} configuration files");
            
            // Save or output configurations
            foreach (var (configType, content) in configs)
            {
                if (outputDir != null)
                {
                    if (!outputDir.Exists)
                    {
                        outputDir.Create();
                    }
                    
                    var fileName = configType switch
                    {
                        "dns" => "dns_host_overrides.conf",
                        "nginx" => "nginx_proxy.conf",
                        "caddy" => "Caddyfile",
                        "flame" => "flame_bookmarks.json",
                        "homepage" => "homepage_services.yaml",
                        _ => $"{configType}.conf"
                    };
                    
                    var filePath = Path.Combine(outputDir.FullName, fileName);
                    await File.WriteAllTextAsync(filePath, content);
                    Console.WriteLine($"Saved {configType} configuration to {filePath}");
                }
                else
                {
                    Console.WriteLine($"--- {configType} ---");
                    Console.WriteLine(content);
                    Console.WriteLine();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Builds the service provider
    /// </summary>
    /// <returns>Configured service provider</returns>
    private static ServiceProvider BuildServiceProvider()
    {
        return new ServiceCollection()
            .AddSingleton<Scanner.ILogger<DockerScanner>, ConsoleLogger<DockerScanner>>()
            .AddSingleton<Scanner.ILogger<UnraidScanner>, ConsoleLogger<UnraidScanner>>()
            .AddSingleton<Scanner.ILogger<PfSenseScanner>, ConsoleLogger<PfSenseScanner>>()
            .AddSingleton<Scanner.ILogger<ScanService>, ConsoleLogger<ScanService>>()
            .AddSingleton<IDockerScanner, DockerScanner>()
            .AddSingleton<IUnraidScanner, UnraidScanner>()
            .AddSingleton<IPfSenseScanner, PfSenseScanner>()
            .AddSingleton<IScanService, ScanService>()
            .BuildServiceProvider();
    }
}

/// <summary>
/// Simple console logger implementation
/// </summary>
/// <typeparam name="T">Type to log</typeparam>
public class ConsoleLogger<T> : Scanner.ILogger<T>
{
    public void LogError(Exception? ex, string message)
    {
        Console.WriteLine($"ERROR: {message}");
        if (ex != null)
        {
            Console.WriteLine($"Exception: {ex.Message}");
        }
    }
    
    public void LogInformation(string message)
    {
        Console.WriteLine($"INFO: {message}");
    }
}