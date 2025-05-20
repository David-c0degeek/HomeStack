namespace HomeStack.Core.Models;

/// <summary>
/// Represents a proxy configuration for a service
/// </summary>
public class ProxyConfig
{
    /// <summary>
    /// Gets or sets the service name
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the domain
    /// </summary>
    public string Domain { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the target host (internal)
    /// </summary>
    public string TargetHost { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the target port
    /// </summary>
    public int TargetPort { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether SSL is enabled
    /// </summary>
    public bool Ssl { get; set; }
    
    /// <summary>
    /// Gets or sets custom headers
    /// </summary>
    public Dictionary<string, string> CustomHeaders { get; set; } = new Dictionary<string, string>();
    
    /// <summary>
    /// Gets or sets authentication settings
    /// </summary>
    public ProxyAuthentication? ProxyAuthentication { get; set; }
    
    // Backwards compatibility properties
    
    /// <summary>
    /// Gets the service name (for backwards compatibility)
    /// </summary>
    public string ServiceName => Name;
    
    /// <summary>
    /// Gets the public URL (for backwards compatibility)
    /// </summary>
    public string PublicUrl => Domain;
    
    /// <summary>
    /// Gets the target URL (for backwards compatibility)
    /// </summary>
    public string TargetUrl => $"{(Ssl ? "https" : "http")}://{TargetHost}:{TargetPort}";
    
    /// <summary>
    /// Gets or sets a value indicating whether the proxy config is enabled
    /// </summary>
    public bool Enabled { get; set; }
}