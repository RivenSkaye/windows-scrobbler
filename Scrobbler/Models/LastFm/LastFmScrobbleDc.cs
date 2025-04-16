﻿namespace Scrobbler.Models.LastFm;

public class LastFmScrobbleDc
{
    /// <summary>
    /// The name of the currently playing track
    /// </summary>
    public string Track { get; set; } = string.Empty;

    /// <summary>
    /// the name of the track's artist
    /// </summary>
    public string Artist { get; set; } = string.Empty;

    /// <summary>
    /// The name of the track's album
    /// </summary>
    public string Album { get; set; } = string.Empty;

    /// <summary>
    /// The name of the track's album's artist
    /// </summary>
    public string AlbumArtist { get; set; } = string.Empty;

    /// <summary>
    /// The track's number (on the album)
    /// </summary>
    public int TrackNumber { get; set; }

    /// <summary>
    /// The duration of the track, in seconds
    /// </summary>
    public int Duration { get; set; }

    /// <summary>
    /// The time the track started playing, in UNIX timestamp format.
    /// </summary>
    public int Timestamp { get; set; }
}
