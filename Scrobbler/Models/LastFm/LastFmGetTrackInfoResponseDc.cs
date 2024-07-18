using System.Xml.Serialization;

namespace Scrobbler.Models.LastFm;

[XmlRoot("lfm")]
public class LastFmGetTrackInfoResponseDc
{
    [XmlElement("track")]
    public LastFmTrackDc? Track { get; set; }
}