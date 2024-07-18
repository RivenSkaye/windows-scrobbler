using System.Xml.Serialization;

namespace Scrobbler.Models.LastFm;

[XmlRoot("lfm")]
public class LastFmErrorDc
{
    [XmlElement("error")]
    public LastFmError Error { get; set; }
}

public class LastFmError
{
    /// <summary>
    /// Error message
    /// </summary>
    [XmlText]
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// LastFM Error Code. Consult the lastfm documentation for the failing endpoint
    /// </summary>
    [XmlAttribute("code")]
    public int Code { get; set; }
}