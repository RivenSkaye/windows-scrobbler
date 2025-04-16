using System.Text;
using Scrobbler.Util;

namespace Scrobbler.Models.LastFm;

public class LastFmUpdateNowPlayingRequestBody : LastFmRequestBodyBase
{
    public override string Method => "track.updateNowPlaying";

    /// <summary>
    /// The name of the currently playing track
    /// </summary>
    public string? Track { get; set; }

    /// <summary>
    /// the name of the track's artist
    /// </summary>
    public string? Artist { get; set; }

    /// <summary>
    /// The name of the track's album
    /// </summary>
    public string? Album { get; set; }

    /// <summary>
    /// The name of the track's album's artist
    /// </summary>
    public string? AlbumArtist { get; set; }

    /// <summary>
    /// The track's number (on the album)
    /// </summary>
    public int? TrackNumber { get; set; }

    /// <summary>
    /// The duration of the track, in seconds
    /// </summary>
    public int Duration { get; set; }


    public override Dictionary<string, string> ToFormData()
    {
        var form = new Dictionary<string, string>();

        if (Album.IsNonEmpty())
            form.Add("album", Album!);

        if (AlbumArtist.IsNonEmpty())
            form.Add("albumArtist", AlbumArtist!);

        form.Add("api_key", ApiKey);

        form.Add("artist", Artist ?? string.Empty);

        form.Add("duration", Duration.ToString());

        form.Add("method", Method);

        form.Add("sk", SessionKey);

        form.Add("track", Track ?? string.Empty);

        if (TrackNumber > 0)
            form.Add("trackNumber", TrackNumber.Value.ToString());

        return form;
    }
}
