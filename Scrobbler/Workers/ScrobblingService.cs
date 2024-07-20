using System.Collections.Concurrent;
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
    private const int ScrobblingBatchSize = 50;
    
    private DateTime _lastScrobbledTime = DateTime.UtcNow;
    private ConcurrentQueue<TrackMetadata> _scrobbleQueue = new();
    
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
            
            // if (logger.IsEnabled(LogLevel.Information)) 
            //     logger.LogInformation($"Playback status: {playbackInfo.PlaybackStatus}. Playing {_currentlyPlaying?.ArtistName} - {_currentlyPlaying?.TrackName} (Playtime: {DateTime.UtcNow - _currentlyPlaying?.PlayingSince})");

            // If we haven't scrobbled for 15 minutes, do so
            if (_lastScrobbledTime < DateTime.UtcNow.AddMinutes(-15))
                await ProcessScrobbleQueueAsync();
            
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
               && track.PlayingSince < DateTime.UtcNow - (track.TrackDuration / 2);
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
            // There is some weirdness here, which is caused by the browser pretty much always being classified as music.
            // To work around this, I try to query LastFM to see if the track exists.
            
            var timeline = session.GetTimelineProperties();
            var track = new TrackMetadata(
                mediaProperties.Title,
                mediaProperties.Artist,
                mediaProperties.AlbumTitle.IsNonEmpty() ? mediaProperties.AlbumTitle : null,
                mediaProperties.AlbumArtist.IsNonEmpty() ? mediaProperties.AlbumArtist : null,
                mediaProperties.TrackNumber > 0 ? mediaProperties.TrackNumber : null,
                timeline.EndTime
            );

            // If the track hasn't actually changed, just reset the timestamp and return
            if (track.IsSameTrackAs(_currentlyPlaying))
            {
                _currentlyPlaying = track;
                return;
            }
            
            var exists = await lastFmService.TrackExistsAsync(track.TrackName, track.ArtistName);
            if (exists)
            {
                newTrack = track;
                if (logger.IsEnabled(LogLevel.Information)) 
                    logger.LogInformation($"Now playing: {track?.ArtistName} - {track?.TrackName}");
                
                if (track is not null)
                    await lastFmService.UpdateCurrentlyPlayingAsync(track);
            }
            else
            {
                logger.LogWarning($"Got non-existent track {track?.ArtistName} - {track?.TrackName}");
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
            if (track.IsQueuedForScrobble)
                return;

            track.IsQueuedForScrobble = true;
        }
        
        _scrobbleQueue.Enqueue(track);
        
        logger.LogInformation($"Added track to scrobbling queue: {track.ArtistName} - {track.TrackName}");
    }

    private async Task ProcessScrobbleQueueAsync()
    {
        var scrobbleBatch = new List<TrackMetadata>();
        for (var i = 0; i < ScrobblingBatchSize; i++)
        {
            if (!_scrobbleQueue.TryDequeue(out var track))
                break;
            
            scrobbleBatch.Add(track);
        }

        if (!scrobbleBatch.Any())
            return;
        
        var success = await lastFmService.ScrobbleTracksAsync(scrobbleBatch);
        if (!success)
        {
            scrobbleBatch.ForEach(_scrobbleQueue.Enqueue);
        }
    }
    
    private void OnCurrentSessionChanged(GlobalSystemMediaTransportControlsSessionManager sender, CurrentSessionChangedEventArgs args)
    {
        if (_session is not null)
            CloseSession(_session);

        var session = sender.GetCurrentSession();
        if (session is null)
            return;
        
        logger.LogInformation($"New session: {session.SourceAppUserModelId}");
        
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

        await ProcessScrobbleQueueAsync();
    }
}