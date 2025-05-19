namespace HomeStack.Core.Models;

/// <summary>
/// Represents authentication configuration for a reverse proxy
/// </summary>
public class ProxyAuthentication
{
    /// <summary>
    /// The type of authentication
    /// </summary>
    public AuthenticationType Type { get; set; } = AuthenticationType.Basic;
    
    /// <summary>
    /// The realm name for authentication
    /// </summary>
    public string Realm { get; set; } = "Restricted";
    
    /// <summary>
    /// List of username and password combinations
    /// </summary>
    public List<UserCredential> Credentials { get; set; } = new();
    
    /// <summary>
    /// Path to an authentication file (e.g., .htpasswd, .htaccess)
    /// </summary>
    public string? AuthFilePath { get; set; }
}

/// <summary>
/// Types of authentication for reverse proxies
/// </summary>
public enum AuthenticationType
{
    /// <summary>
    /// Basic HTTP authentication
    /// </summary>
    Basic = 0,
    
    /// <summary>
    /// Digest HTTP authentication
    /// </summary>
    Digest = 1,
    
    /// <summary>
    /// JWT token authentication
    /// </summary>
    JWT = 2,
    
    /// <summary>
    /// OAuth authentication
    /// </summary>
    OAuth = 3
}

/// <summary>
/// Represents a username and password combination for authentication
/// </summary>
public class UserCredential
{
    /// <summary>
    /// Username
    /// </summary>
    public string Username { get; set; } = string.Empty;
    
    /// <summary>
    /// Password (should be stored securely)
    /// </summary>
    public string Password { get; set; } = string.Empty;
}