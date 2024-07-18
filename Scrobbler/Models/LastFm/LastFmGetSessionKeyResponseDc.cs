using System.Xml.Serialization;

namespace Scrobbler.Models.LastFm;

[XmlRoot("lfm")]
public class LastFmGetSessionKeyResponseDc
{
    [XmlElement("session")]
    public SessionDc? Session { get; set; }
}