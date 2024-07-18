namespace Scrobbler.Util;

public class AppSettings
{
    /// <summary>
    /// If true, log output to X
    /// </summary>
    public bool UseLogging { get; set; }
    
    /// <summary>
    /// The LastFM API Key
    /// </summary>
    public string ApiKey { get; set; }
    
    /// <summary>
    /// LastFM Shared Secret
    /// </summary>
    public string SharedSecret { get; set; }
    
    /// <summary>
    /// The amount of time, in milliseconds, to wait after polling
    /// </summary>
    public int PollTime { get; set; }
}

public class SettingsFactory
{
    public static AppSettings GetSettings(IConfiguration configuration) => new()
    {
        UseLogging = configuration["useLogging"] == "true",
        PollTime = int.Parse(configuration["pollTime"] ?? "10000"),
        ApiKey = configuration["apiKey"] ?? string.Empty,
        SharedSecret = configuration["sharedSecret"] ?? string.Empty
    };
}