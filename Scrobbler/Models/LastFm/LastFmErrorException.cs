namespace Scrobbler.Models.LastFm;

public class LastFmErrorException : ApplicationException
{
    public LastFmErrorException(int code, string message) : base($"LastFm error: {code} - {message}")
    {
        Code = code;
        Message = message;
    }
    
    public int Code { get; }
    public string Message { get; }
}