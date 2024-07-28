# Windows Scrobbler
LastFM scrobbler for Windows with minimal dependencies.

# Requirements
* DotNET 8 SDK
* Windows 10/11

# How does it work
The application listens to the Windows System Media Transport Controls for any active media sessions.
Whenever the currently playing media changes, it tries to verify the song that is currently playing.
On a successful lookup, it will notify LastFM that a song is playing. After ~50% of the song has been played, it will be scrobbled. <br>
If the scrobbling fails for any reason, the song is added to a queue and will be sent to LastFM later.

# Future Development:
* Handle all sessions instead of only what Windows considers to be the "most relevant session"
* Support running as a service
