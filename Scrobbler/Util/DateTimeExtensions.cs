namespace Scrobbler.Util;

public static class DateTimeExtensions
{
    public static int ToUnixTimestamp(this DateTime dt) => (int) (dt - DateTime.UnixEpoch).TotalSeconds;
}