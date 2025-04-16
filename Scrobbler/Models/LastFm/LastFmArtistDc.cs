using System.Xml.Serialization;

namespace Scrobbler.Models.LastFm;

public class LastFmArtistDc
{
    [XmlElement("mbid")]
    public string MusicBrainzId { get; set; } = string.Empty;

    [XmlElement("name")]
    public string Name { get; set; } = string.Empty;
}
