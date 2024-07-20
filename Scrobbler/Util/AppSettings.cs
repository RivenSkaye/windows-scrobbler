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

    /// <summary>
    /// Whether a track needs to have an album in the response from LastFM to count it as an existing track
    /// </summary>
    /// <remarks>
    /// <p>
    /// There's some weirdness where non-music media, such as YouTube videos,
    /// are reported as Music by the Windows API, and are found in the LastFM database.
    /// </p>
    /// <p>
    /// This means we can scrobble YouTube videos, which generally isn't what we want.
    /// The videos usually don't get an album tag, and all songs I've tried so far had an album tag. So to try to reduce
    /// video scrobbling, we can only count media that has an Album in the LastFM database as music.
    /// </p>
    /// </remarks>
    public bool RequireTrackHasAlbum { get; set; } = true;
}

public class SettingsFactory
{
    public static AppSettings GetSettings(IConfiguration configuration) => new()
    {
        UseLogging = configuration["useLogging"] == "true",
        PollTime = int.Parse(configuration["pollTime"] ?? "1000"),
        ApiKey = configuration["apiKey"] ?? string.Empty,
        SharedSecret = configuration["sharedSecret"] ?? string.Empty,
        RequireTrackHasAlbum = configuration["requireTrackHasAlbum"] is null or "true"
    };
}