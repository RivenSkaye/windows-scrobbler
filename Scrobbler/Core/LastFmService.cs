using System.Diagnostics;
using System.Xml.Serialization;
using Microsoft.Win32;
using Scrobbler.Models;
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

    private const string BaseUrl = "https://ws.audioscrobbler.com/2.0/";
    private const string RegistryKeyPath = @"Software\Yoeksa\Scrobbler";
    private const string RegistrySessionKeyValueName = "session_key";

    private AuthenticationToken? _token;
    private SessionDc? _session;

    public async Task<bool> ScrobbleTracksAsync(List<TrackMetadata> tracks)
    {
        await EnsureAuthenticatedAsync();

        var formData = new Dictionary<string, string>()
        {
            {"method", "track.scrobble"},
            {"api_key", _appSettings.ApiKey},
            {"sk", _session!.Key}
        };

        for (var i = 0; i < tracks.Count; i++)
        {
            var track = tracks[i];

            formData.Add($"artist[{i}]", track.ArtistName);
            formData.Add($"track[{i}]", track.TrackName);
            formData.Add($"timestamp[{i}]", track.PlayingSince.ToUnixTimestamp().ToString());
            formData.Add($"duration[{i}]", ((int)track.TrackDuration.TotalSeconds).ToString());

            if (track.AlbumName.IsNonEmpty()) formData.Add($"album[{i}]", track.AlbumName!);
            if (track.TrackNumber.HasValue) formData.Add($"trackNumber[{i}]", track.TrackNumber.Value.ToString());
            if (track.AlbumArtistName.IsNonEmpty()) formData.Add($"albumArtist[{i}]", track.AlbumArtistName!);
        }

        try
        {
            await SendFormDataAsync(formData);
        }
        catch (LastFmErrorException e)
        {
            _logger.LogDebug("LastFM Exception {Code}: {Err}", e.Code, e.Message);
            return false;
        }

        if (_logger.IsEnabled(LogLevel.Debug))
            _logger.LogDebug($"Scrobbled {tracks.Count} tracks");
        return true;
    }

    public async Task<LastFmTrackDc?> GetTrackAsync(string track, string artist)
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
            return result?.Track;
        }
        catch (LastFmErrorException e)
        {
            if (e.Code == 6) // Track not found error code
                return null;

            throw;
        }
    }

    public async Task UpdateCurrentlyPlayingAsync(TrackMetadata track)
    {
        await EnsureAuthenticatedAsync();

        await SendFormDataAsync(new LastFmUpdateNowPlayingRequestBody
        {
            ApiKey = _appSettings.ApiKey,
            SessionKey = _session!.Key,

            Track = track.TrackName,
            Artist = track.ArtistName,
            Album = track.AlbumName,
            AlbumArtist = track.AlbumArtistName,
            TrackNumber = track.TrackNumber,
            Duration = (int)track.TrackDuration.TotalSeconds
        });

        if (_logger.IsEnabled(LogLevel.Debug))
            _logger.LogDebug("Updated Playing Now in LastFM");
    }

    public async Task EnsureAuthenticatedAsync()
    {
        await EnsureHasTokenAsync();
        var gotSessionKey = await EnsureHasSessionKeyAsync();

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
            if (_logger.IsEnabled(LogLevel.Information))
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

        copy.Add("api_sig", signatureString.AsMd5HexString());
        return copy;
    }

    private async Task<T?> SendGetRequestAsync<T>(Dictionary<string, string> queryVals, bool signRequest = true) where T : class
    {
        if (signRequest)
            queryVals = AddSignature(queryVals);

        using var request = new HttpRequestMessage(HttpMethod.Get, BaseUrl.AddQueryString(queryVals));
        var result = await _client.SendAsync(request);

        // TODO: Handle error more gracefully
        if (!result.IsSuccessStatusCode && await result.Content.ReadFromXmlAsync<LastFmErrorDc>() is LastFmErrorDc error)
        {
            if (_logger.IsEnabled(LogLevel.Error))
                _logger.LogError($"Got an error response from LastFM! code: {error.Error.Code} - {error.Error.Message}");
            throw new LastFmErrorException(error.Error.Code, error.Error.Message);
        }

        return await result.Content.ReadFromXmlAsync<T>();
    }

    private Task SendFormDataAsync<TBody>(TBody body) where TBody : LastFmRequestBodyBase
    {
        return SendFormDataAsync(body.ToFormData());
    }

    private async Task SendFormDataAsync(Dictionary<string, string> formData)
    {
        var signedFormData = AddSignature(formData);

        using var request = new HttpRequestMessage(HttpMethod.Post, BaseUrl);
        request.Content = new FormUrlEncodedContent(signedFormData);

        var result = await _client.SendAsync(request);
        if (!result.IsSuccessStatusCode && await result.Content.ReadFromXmlAsync<LastFmErrorDc>() is LastFmErrorDc error)
        {
            if (_logger.IsEnabled(LogLevel.Error))
                _logger.LogError($"Got an error response from LastFM: {error.Error.Code} - {error.Error.Message}");
            throw new LastFmErrorException(error.Error.Code, error.Error.Message);
        }

#if DEBUG
        var response = await result.Content.ReadAsStringAsync();
#endif
    }

    [XmlRoot("lfm")]
    public class AuthenticationToken
    {
        [XmlElement("token")]
        public string Token { get; set; } = string.Empty;
        public DateTime GrantedAt { get; } = DateTime.UtcNow;

        public DateTime ValidTo => GrantedAt.AddHours(1).AddMinutes(-5); // LastFM keys are valid for 1 hour.. So we request a new one every 55 minutes
    }
}
