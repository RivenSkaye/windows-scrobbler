using Scrobbler.Core;
using Scrobbler.Util;
using Scrobbler.Workers;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton(services => SettingsFactory.GetSettings(services.GetService<IConfiguration>()!));
builder.Services.AddSingleton<HttpClient>();
builder.Services.AddSingleton<LastFmService>();
builder.Services.AddHostedService<ScrobblingService>();

var host = builder.Build();
host.Run();
