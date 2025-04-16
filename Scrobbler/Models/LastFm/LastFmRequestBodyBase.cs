using System.Text.Json.Serialization;

namespace Scrobbler.Models.LastFm;

/// <summary>
/// Base DC containing authentication properties that are required in all LastFM API calls.
/// </summary>
/// <remarks>See https://www.last.fm/api/authentication for details</remarks>
public abstract class LastFmRequestBodyBase
{
    /// <summary>
    /// A LastFM API key
    /// </summary>
    [JsonPropertyName("api_key")]
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// A LastFM session key
    /// </summary>
    [JsonPropertyName("sk")]
    public string SessionKey { get; set; } = string.Empty;

    /// <summary>
    /// A LastFM method signature
    /// </summary>
    [JsonPropertyName("api_sig")]
    public string ApiSignature { get; protected set; } = string.Empty;

    /// <summary>
    /// The LastFM method to run
    /// </summary>
    [JsonPropertyName("method")]
    public abstract string Method { get; }

    public abstract Dictionary<string, string> ToFormData();
}
