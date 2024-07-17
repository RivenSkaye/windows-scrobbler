using ConfigurationManager = System.Configuration.ConfigurationManager;

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
    /// The amount of time, in milliseconds, to wait after polling
    /// </summary>
    public int PollTime { get; set; }
}

public class SettingsFactory
{
    public static AppSettings GetSettings() => new()
    {
        UseLogging = ConfigurationManager.AppSettings["useLogging"] == "true",
        PollTime = int.Parse(ConfigurationManager.AppSettings["pollTime"] ?? "10000"),
        ApiKey = Environment.GetEnvironmentVariable("LASTFM_API_KEY") ?? string.Empty
    };
}