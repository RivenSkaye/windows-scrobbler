namespace Scrobbler.Models.LastFm;

public class LastFmErrorException(int code, string message) : ApplicationException($"LastFm error: {code} - {message}");
