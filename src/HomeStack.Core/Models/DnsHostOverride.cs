namespace HomeStack.Core.Models;

/// <summary>
/// Represents a DNS host override in pfSense
/// </summary>
public class DnsHostOverride
{
    /// <summary>
    /// Gets or sets the hostname
    /// </summary>
    public string Hostname { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the domain
    /// </summary>
    public string Domain { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the IP address
    /// </summary>
    public string IpAddress { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the description
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets the fully qualified domain name (FQDN)
    /// </summary>
    public string FullyQualifiedDomainName => string.IsNullOrEmpty(Domain) ? Hostname : $"{Hostname}.{Domain}";
}