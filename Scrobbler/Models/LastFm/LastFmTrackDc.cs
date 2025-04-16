using System.Xml.Serialization;

namespace Scrobbler.Models.LastFm;

public class LastFmTrackDc
{
    [XmlElement("id")]
    public int Id { get; set; }

    [XmlElement("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Track duration, in milliseconds
    /// </summary>
    [XmlElement("duration")]
    public int? Duration { get; set; }

    [XmlElement("album")]
    public LastFmAlbumDc? Album { get; set; }

    [XmlElement("artist")]
    public LastFmArtistDc? Artist { get; set; }
}
