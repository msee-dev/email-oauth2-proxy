using EmailOAuth2Proxy.Service;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "Email OAuth 2.0 Proxy Service";
});

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
