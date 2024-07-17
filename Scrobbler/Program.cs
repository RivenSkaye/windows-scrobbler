using Scrobbler;
using Scrobbler.Util;
using Scrobbler.Workers;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<AppSettings>(_ => SettingsFactory.GetSettings());
builder.Services.AddSingleton<HttpClient>();
builder.Services.AddHostedService<ScrobblingService>();

var host = builder.Build();
host.Run();