namespace Scrobbler.Models;

public record TrackMetadata()
{
    public TrackMetadata(string trackName, string artistName, string? albumName, string? albumArtistName, int? trackNumber, TimeSpan trackDuration) : this()
    {
        TrackName = trackName;
        TrackDuration = trackDuration;
        ArtistName = artistName;
        AlbumName = albumName;
        AlbumArtistName = albumArtistName;
        TrackNumber = trackNumber;
        PlayingSince = DateTime.UtcNow;
    }

    /// <summary>
    /// The name of the track
    /// </summary>
    public string TrackName { get; } = string.Empty;

    /// <summary>
    /// The duration of the track
    /// </summary>
    public TimeSpan TrackDuration { get; set; }

    /// <summary>
    /// The name of the track's artist
    /// </summary>
    public string ArtistName { get; } = string.Empty;

    /// <summary>
    /// The name of the track's album
    /// </summary>
    public string? AlbumName { get; }

    /// <summary>
    /// The name of the track's album's artist. Can be null if the same as the track artist name
    /// </summary>
    public string? AlbumArtistName { get; }

    /// <summary>
    /// the track number of the track
    /// </summary>
    public int? TrackNumber { get; }

    /// <summary>
    /// The time the track started playing, in UTC
    /// </summary>
    public DateTime PlayingSince { get; }

    public bool IsQueuedForScrobble { get; set; }

    public bool IsSameTrackAs(TrackMetadata? other) => other is not null
                                                     && other.TrackName == TrackName
                                                     && other.ArtistName == ArtistName;
};
