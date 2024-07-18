using System.Xml.Serialization;

namespace Scrobbler.Util;

public static class HttpExtensions
{
    public static string AddQueryString(this string uri, Dictionary<string, string> keyVals) =>
        $"{uri}?{string.Join('&', keyVals.Select(kv => $"{kv.Key}={kv.Value}"))}";

    public static async Task<T?> ReadFromXmlAsync<T>(this HttpContent content) where T : class
    {
        #if DEBUG
        var str = await content.ReadAsStringAsync();
        #endif
        
        var stream = await content.ReadAsStreamAsync();
        return new XmlSerializer(typeof(T)).Deserialize(stream) as T;
    }
}