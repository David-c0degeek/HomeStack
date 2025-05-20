namespace HomeStack.Core.Models;

/// <summary>
/// Represents authentication configuration for a proxy
/// </summary>
public class ProxyAuthentication
{
    /// <summary>
    /// Gets or sets the authentication type
    /// </summary>
    public AuthenticationType Type { get; set; }
    
    /// <summary>
    /// Gets or sets the username (for basic auth)
    /// </summary>
    public string? Username { get; set; }
    
    /// <summary>
    /// Gets or sets the password (for basic auth)
    /// </summary>
    public string? Password { get; set; }
}

/// <summary>
/// Authentication types for proxy configuration
/// </summary>
public enum AuthenticationType
{
    /// <summary>
    /// No authentication
    /// </summary>
    None = 0,
    
    /// <summary>
    /// Basic authentication
    /// </summary>
    Basic = 1,
    
    /// <summary>
    /// OAuth authentication
    /// </summary>
    OAuth = 2,
    
    /// <summary>
    /// JWT authentication
    /// </summary>
    Jwt = 3
}