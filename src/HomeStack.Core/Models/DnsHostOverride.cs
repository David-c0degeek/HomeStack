namespace HomeStack.Core.Models;

/// <summary>
/// Represents a DNS host override entry in pfSense
/// </summary>
public class DnsHostOverride
{
    /// <summary>
    /// The hostname (without domain suffix)
    /// </summary>
    public string Hostname { get; set; } = string.Empty;
    
    /// <summary>
    /// The domain suffix
    /// </summary>
    public string? Domain { get; set; }
    
    /// <summary>
    /// The full FQDN (hostname + domain)
    /// </summary>
    public string Fqdn 
    { 
        get 
        {
            if (string.IsNullOrEmpty(Domain))
                return Hostname;
                
            return $"{Hostname}.{Domain}";
        } 
    }
    
    /// <summary>
    /// The IP address
    /// </summary>
    public string IpAddress { get; set; } = string.Empty;
    
    /// <summary>
    /// Description for this DNS entry
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Whether this entry is enabled
    /// </summary>
    public bool IsEnabled { get; set; } = true;
}