namespace Scrobbler.Models;

/// <summary>
/// Base DC containing authentication properties that are required in all LastFM API calls.
/// </summary>
/// <remarks>See https://www.last.fm/api/authentication for details</remarks>
public abstract class LastFmBaseDc
{
    /// <summary>
    /// LastFM API Key
    /// </summary>
    public string ApiKey { get; set; }
    
    /// <summary>
    /// LastFM API Method Signature
    /// </summary>
    public string ApiSignature { get; set; }
    
    /// <summary>
    /// LastFM Session Key
    /// </summary>
    public string SessionKey { get; set; }
}