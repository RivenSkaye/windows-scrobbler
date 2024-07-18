using System.Xml.Serialization;

namespace Scrobbler.Models.LastFm;

public class LastFmTrackDc
{
    [XmlElement("id")]
    public int Id { get; set; }
    
    [XmlElement("name")]
    public string Name { get; set; }
    
    [XmlElement("duration")]
    public int? Duration { get; set; }
}