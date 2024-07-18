namespace Scrobbler.Models.LastFm;

public class LastFmRequestUpdateNowPlayingDc : LastFmRequestBaseDc
{
    /// <summary>
    /// The name of the currently playing track
    /// </summary>
    public string Track { get; set; }
    
    /// <summary>
    /// the name of the track's artist
    /// </summary>
    public string Artist { get; set; }
    
    /// <summary>
    /// The name of the track's album
    /// </summary>
    public string Album { get; set; }
    
    /// <summary>
    /// The name of the track's album's artist
    /// </summary>
    public string AlbumArtist { get; set; }
    
    /// <summary>
    /// The track's number (on the album)
    /// </summary>
    public int TrackNumber { get; set; }
    
    /// <summary>
    /// The duration of the track, in seconds
    /// </summary>
    public int Duration { get; set; }
}