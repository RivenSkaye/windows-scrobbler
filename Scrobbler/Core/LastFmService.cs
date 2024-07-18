using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Serialization;
using Microsoft.Win32;
using Scrobbler.Models.LastFm;
using Scrobbler.Util;

namespace Scrobbler.Core;

public class LastFmService
{
    private readonly HttpClient _client;
    private readonly AppSettings _appSettings;
    private readonly ILogger<LastFmService> _logger;

    public LastFmService(HttpClient client, AppSettings appSettings, ILogger<LastFmService> logger)
    {
        _client = client;
        _appSettings = appSettings;
        _logger = logger;
    }


    private const string BaseUrl = "https://ws.audioscrobbler.com/2.0";
    private const string RegistryKeyPath = @"Software\Yoeksa\Scrobbler";
    private const string RegistrySessionKeyValueName = "session_key";
    
    
    private AuthenticationToken? _token;
    private SessionDc? _session;
    
    public async Task<bool> TrackExistsAsync(string track, string artist)
    {
        var queryVals = new Dictionary<string, string>()
        {
            {"method", "track.getInfo"},
            {"track", track},
            {"artist", artist},
            {"api_key", _appSettings.ApiKey}
        };

        try
        {
            var result = await SendGetRequestAsync<LastFmGetTrackInfoResponseDc>(queryVals, signRequest: false);
            return result?.Track?.Duration is > 30000;
        }
        catch (LastFmErrorException e)
        {
            if (e.Code == 6) // Track not found error code
                return false;

            throw;
        }
    }
    
    public async Task EnsureAuthenticatedAsync()
    {
        await EnsureHasTokenAsync();
        var gotSessionKey = await EnsureHasSessionKeyAsync();
        
        _logger.LogInformation($"Finished authentication workflow - isAuthenticated: {gotSessionKey}");

        // TODO: Handle the case where authentication failed?
        if (!gotSessionKey)
            throw new Exception();
    }

    /// <summary>
    /// Ensure that the service has a session key
    /// </summary>
    /// <returns>true if a session key was gathered. False if the user has not authorized the application</returns>
    private async Task<bool> EnsureHasSessionKeyAsync()
    {
        // Check if we already have a session key
        if (_session is not null)
            return true;

        // Try to get an existing session key from the Windows registry
        var regKey = Registry.CurrentUser.CreateSubKey(RegistryKeyPath);
        var regSessionKey = regKey.GetValue(RegistrySessionKeyValueName) as string;
        if (!string.IsNullOrWhiteSpace(regSessionKey))
        {
            _logger.LogInformation("Found session key in Windows registry");
            _session = new SessionDc(regSessionKey);
            return true;
        }
        
        var queryVals = new Dictionary<string, string>
        {
            {"method", "auth.getSession"},
            
            {"api_key", _appSettings.ApiKey},
            {"token", _token!.Token},
        };

        try
        {
            var response = await SendGetRequestAsync<LastFmGetSessionKeyResponseDc>(queryVals);
            _session = response?.Session ?? throw new NotImplementedException();

            regKey.SetValue(RegistrySessionKeyValueName, response.Session.Key);
        }
        catch (LastFmErrorException e)
        {
            if (e.Code != 14)
                throw; // Only handle unauthorized token here
            
            var process = new Process();
            process.StartInfo.UseShellExecute = true;
            process.StartInfo.FileName = $"https://www.last.fm/api/auth/?api_key={_appSettings.ApiKey}&token={_token.Token}";
            process.Start();
        }

        return await AsyncHelper.RetryExponentialBackOffAsync(async () =>
        {
            try
            {
                if (_logger.IsEnabled(LogLevel.Information))
                    _logger.LogInformation("Retrying auth.getSession");
                
                var result = await SendGetRequestAsync<LastFmGetSessionKeyResponseDc>(queryVals);
                if (result?.Session?.Key is not null)
                {
                    _session = result.Session;
                    regKey.SetValue(RegistrySessionKeyValueName, result.Session.Key);
                    
                    return true;
                }

                return false;
            }
            catch (LastFmErrorException e)
            {
                if (e.Code == 14)
                    return false;

                throw;
            }

        }, retryTimes: 15, baseBackOff: 5000, 30000);
    }
    
    private async Task EnsureHasTokenAsync()
    {
        if (_token is not null && _token.ValidTo > DateTime.UtcNow)
            return;
        
        var queryVals = new Dictionary<string, string>
        {
            {"method", "auth.gettoken"},
            {"api_key", _appSettings.ApiKey},
        };

        var token = await SendGetRequestAsync<AuthenticationToken>(queryVals);

        _token = token ?? throw new NotImplementedException();
    }

    /// <summary>
    /// Add a LastFM API signature to a query string Dictionary
    /// </summary>
    /// <remarks>The process is explained here: https://www.last.fm/api/desktopauth</remarks>
    private Dictionary<string, string> AddSignature(Dictionary<string, string> queryVals)
    {
        var copy = new Dictionary<string, string>(queryVals);
        
        var signatureString = $"{string.Join(null, queryVals.OrderBy(kv => kv.Key).Select(kv => kv.Key + kv.Value))}{_appSettings.SharedSecret}";

        var inputBytes = Encoding.UTF8.GetBytes(signatureString);
        var hashBytes = MD5.HashData(inputBytes);
        
        copy.Add("api_sig", Convert.ToHexString(hashBytes));
        return copy;
    }

    private async Task<T?> SendGetRequestAsync<T>(Dictionary<string, string> queryVals, bool signRequest = true) where T : class
    {
        if (signRequest)
            queryVals = AddSignature(queryVals);
        
        using var request = new HttpRequestMessage(HttpMethod.Get, BaseUrl.AddQueryString(queryVals));
        var result = await _client.SendAsync(request);
        
        if (!result.IsSuccessStatusCode)
        {
            // TODO: Handle error more gracefully
            var error = await result.Content.ReadFromXmlAsync<LastFmErrorDc>();
            _logger.LogCritical($"Failed to fetch LastFM authentication key! code: {error?.Error.Code} - {error?.Error.Message}");
            throw new LastFmErrorException(error.Error.Code, error.Error.Message);
        }

        return await result.Content.ReadFromXmlAsync<T>();
    }
    
    [XmlRoot("lfm")]
    public class AuthenticationToken
    {
        [XmlElement("token")]
        public string Token { get; set; }
        public DateTime GrantedAt { get; } = DateTime.UtcNow;

        public DateTime ValidTo => GrantedAt.AddHours(1).AddMinutes(-5); // LastFM keys are valid for 1 hour.. So we request a new one every 55 minutes
    }
}