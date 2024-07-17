using Scrobbler;
using Scrobbler.Util;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<Settings>(_ => SettingsFactory.GetSettings());
builder.Services.AddHostedService<ScrobblingService>();

var host = builder.Build();
host.Run();