namespace HomeStack.Core.Models;

/// <summary>
/// Represents a reverse proxy configuration
/// </summary>
public class ProxyConfig
{
    /// <summary>
    /// The hostname to use for the proxy
    /// </summary>
    public string Hostname { get; set; } = string.Empty;
    
    /// <summary>
    /// The domain to use for the proxy
    /// </summary>
    public string? Domain { get; set; }
    
    /// <summary>
    /// The fully qualified domain name (hostname + domain)
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
    /// The target IP address
    /// </summary>
    public string TargetIp { get; set; } = string.Empty;
    
    /// <summary>
    /// The target port
    /// </summary>
    public int TargetPort { get; set; }
    
    /// <summary>
    /// The target URL (http://ip:port)
    /// </summary>
    public string TargetUrl 
    { 
        get => $"http://{TargetIp}:{TargetPort}"; 
    }
    
    /// <summary>
    /// Whether SSL is enabled for this proxy
    /// </summary>
    public bool SslEnabled { get; set; }
    
    /// <summary>
    /// The SSL certificate path (if SSL is enabled)
    /// </summary>
    public string? SslCertPath { get; set; }
    
    /// <summary>
    /// The SSL key path (if SSL is enabled)
    /// </summary>
    public string? SslKeyPath { get; set; }
    
    /// <summary>
    /// Whether authentication is enabled for this proxy
    /// </summary>
    public bool AuthEnabled { get; set; }
    
    /// <summary>
    /// Authentication configuration (if auth is enabled)
    /// </summary>
    public ProxyAuthentication? AuthConfig { get; set; }
    
    /// <summary>
    /// Additional headers to set
    /// </summary>
    public Dictionary<string, string> AdditionalHeaders { get; set; } = new();
    
    /// <summary>
    /// Path to a custom configuration file
    /// </summary>
    public string? CustomConfigPath { get; set; }
    
    /// <summary>
    /// Whether this proxy config is enabled
    /// </summary>
    public bool IsEnabled { get; set; } = true;
}