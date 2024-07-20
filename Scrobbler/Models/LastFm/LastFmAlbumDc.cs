using System.Xml.Serialization;

namespace Scrobbler.Models.LastFm;

public class LastFmAlbumDc
{
    [XmlElement("title")]
    public string? Title { get; set; }
    
    [XmlElement("artist")]
    public string? Artist { get; set; }
    
    [XmlElement("url")]
    public string? Url { get; set; }
}