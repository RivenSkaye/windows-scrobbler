# Windows Scrobbler
LastFM scrobbler for Windows with minimal dependencies.

# Requirements
* DotNET 8 SDK
* Windows 10/11

# How does it work
The application listens to the Windows System Media Transport Controls for any active media sessions.
Whenever the currently playing media changes, it tries to verify the song that is currently playing.
On a successful lookup, it will notify LastFM that a song is playing. After ~50% of the song has been played, it will be scrobbled.

If the scrobbling fails for any reason, the song is added to a queue and will be sent to LastFM later.

# Setup
To use the application, you need a LastFM key/secret pair. You can apply for one [Here](https://www.last.fm/api/account/create). Leave the Callback URL field empty. Once you have a key and a secret, add them to the respective fields in appsettings.json

The application requires permission to tell LastFM what you're listening to, and to scrobble songs. On the first startup, the you will be redirected to LastFM to log in and allow access. After a successful login, a session key is stored in the windows registry that will be used for subsequent startups

# Future Development:
* Handle all sessions instead of only what Windows considers to be the "most relevant session"
* Support running as a service
* Better/More code documentation
