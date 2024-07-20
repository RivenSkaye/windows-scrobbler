using System.Xml.Serialization;

namespace Scrobbler.Models.LastFm;

public class LastFmArtistDc
{
    [XmlElement("mbid")]
    public string MusicBrainzId { get; set; }
    
    [XmlElement("name")]
    public string Name { get; set; }
}