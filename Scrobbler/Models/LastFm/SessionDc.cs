using System.Xml.Serialization;

namespace Scrobbler.Models.LastFm;

public class SessionDc
{
    public SessionDc()
    {
        Key = string.Empty;
    }

    public SessionDc(string key)
    {
        Key = key;
    }

    [XmlElement("key")]
    public string Key { get; set; }
}