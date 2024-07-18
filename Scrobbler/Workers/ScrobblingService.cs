using Windows.Media;
using Windows.Media.Control;
using Scrobbler.Core;
using Scrobbler.Models;
using Scrobbler.Util;

namespace Scrobbler.Workers;

public class ScrobblingService(ILogger<ScrobblingService> logger, AppSettings appSettings, LastFmService lastFmService) : BackgroundService
{
    private GlobalSystemMediaTransportControlsSessionManager? _manager;
    private GlobalSystemMediaTransportControlsSession? _session;

    private TrackMetadata? _currentlyPlaying;
    
    private const int MinTrackLengthInSeconds = 1;
    
    private Queue<TrackMetadata> _scrobbleQueue = new();
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await InitializeAsync(stoppingToken);

        await lastFmService.EnsureAuthenticatedAsync();
        
        while (!stoppingToken.IsCancellationRequested)
        {
            var playbackInfo = _session?.GetPlaybackInfo();
            if (playbackInfo is {PlaybackStatus: GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing} && _currentlyPlaying is not null)
            {
                if (TrackCanBeScrobbled(_currentlyPlaying))
                    EnqueueForScrobbling(_currentlyPlaying);
            }
            
            if (logger.IsEnabled(LogLevel.Information))
            {
                // var playbackStatus  = _session.GetPlaybackInfo().PlaybackStatus;
                // var media = await _session.TryGetMediaPropertiesAsync();
                
                logger.LogInformation($"Playback status: {playbackInfo.PlaybackStatus}. Playing {_currentlyPlaying?.ArtistName} - {_currentlyPlaying?.TrackName} (Playtime: {DateTime.UtcNow - _currentlyPlaying?.PlayingSince})");
            }
            
            await Task.Delay(appSettings.PollTime, stoppingToken);
        }
    }

    /// <summary>
    /// <para>Checks whether a given track can be scrobbled.</para>
    /// 
    /// LastFM has some rules regarding when a track can be scrobbled:
    /// 
    /// <list type="bullet">
    ///     <item>Must be at least 30 seconds long</item>
    ///     <item>Must have been playing for at least half its duration, or for 4 minutes, whichever comes first</item>
    /// </list>
    /// 
    /// </summary>
    /// <remarks>See the LastFM docs for more information: https://www.last.fm/api/scrobbling#when-is-a-scrobble-a-scrobble</remarks>
    /// <remarks>The currently playing track</remarks>
    private bool TrackCanBeScrobbled(TrackMetadata? track)
    {
        return track is not null
               && track.TrackDuration.TotalSeconds > MinTrackLengthInSeconds
               && track.PlayingSince < DateTime.UtcNow - TimeSpan.FromSeconds(5); //(track.TrackDuration / 2);
    }
    
    private async Task InitializeAsync(CancellationToken stoppingToken)
    {
        // Get manager and current session
        _manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
        _session = _manager.GetCurrentSession();
        
        // Listen to changes to Current Session
        _manager.CurrentSessionChanged += OnCurrentSessionChanged;
        InitializeSession(_session);

        await UpdateCurrentTrackAsync(_session);
        
        // Register shutdown method
        stoppingToken.Register(ShutDown);
    }

    private async Task UpdateCurrentTrackAsync(GlobalSystemMediaTransportControlsSession session)
    {
        TrackMetadata? newTrack = null;
        
        var mediaProperties = await session.TryGetMediaPropertiesAsync();
        if (mediaProperties.PlaybackType == MediaPlaybackType.Music)
        {
            // There is some weirdness here, which is caused by Youtube (or the browser, haven't checked other pages) always being classified as music.
            // To work around this, I try to query LastFM to see if the track exists.
            
            var timeline = session.GetTimelineProperties();
            var track = new TrackMetadata(
                mediaProperties.Title,
                mediaProperties.Artist,
                mediaProperties.AlbumTitle,
                mediaProperties.AlbumArtist,
                mediaProperties.TrackNumber,
                timeline.EndTime
            );
            
            var exists = await lastFmService.TrackExistsAsync(track.TrackName, track.ArtistName);
            if (exists)
            {
                newTrack = track;
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation($"Now playing: " +
                                          $"\n\t - Track: {track?.TrackName}" +
                                          $"\n\t - Artist: {track?.ArtistName} " +
                                          $"\n\t - Album: {track?.AlbumName} " +
                                          $"\n\t - TrackNum: {track?.TrackNumber} " +
                                          $"\n\t - AlbumArtist: {track?.AlbumArtistName}" +
                                          $"\n\t - duration: {track?.TrackDuration})");
                }
            }
            else
            {
                logger.LogWarning("Got non-existant track");
            }
        }

        if (_currentlyPlaying is not null && TrackCanBeScrobbled(_currentlyPlaying))
            EnqueueForScrobbling(_currentlyPlaying);
        _currentlyPlaying = newTrack;
    }
    
    private void InitializeSession(GlobalSystemMediaTransportControlsSession session)
    {
        session.MediaPropertiesChanged += OnMediaPropertiesChangedAsync;
    }

    private void CloseSession(GlobalSystemMediaTransportControlsSession session)
    {
        session.MediaPropertiesChanged -= OnMediaPropertiesChangedAsync;
    }
    
    private void ShutDown()
    {
        logger.LogInformation("Cleaning up resources..");
        
        if (_manager is null)
            return;

        if (_session is not null)
            CloseSession(_session);
        
        _manager.CurrentSessionChanged -= OnCurrentSessionChanged;
    }

    private void EnqueueForScrobbling(TrackMetadata track)
    {
        lock (track)
        {
            if (track.IsScrobbled)
                return;

            track.IsScrobbled = true;
            _scrobbleQueue.Enqueue(track);
        }
        
        logger.LogInformation($"Added track to scrobbling queue: {track.ArtistName} - {track.TrackName}");
    }

    private void OnCurrentSessionChanged(GlobalSystemMediaTransportControlsSessionManager sender, CurrentSessionChangedEventArgs args)
    {
        if (_session is not null)
            CloseSession(_session);

        var session = sender.GetCurrentSession();
        if (session is null)
            return;
        
        InitializeSession(session);
        _session = session;
        
    }
    
    private async void OnMediaPropertiesChangedAsync(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args)
    {
        if (_currentlyPlaying is not null)
        {
            var oldTrackFormat = $"{_currentlyPlaying.ArtistName} - {_currentlyPlaying.TrackName}";
            if (TrackCanBeScrobbled(_currentlyPlaying))
                logger.LogDebug($"Track should be scrobbled: {oldTrackFormat}");
            else logger.LogDebug($"Finished track {oldTrackFormat} - ineligible for scrobble");
        }

        await UpdateCurrentTrackAsync(sender);

        // TODO: Notify LastFM
    }
}