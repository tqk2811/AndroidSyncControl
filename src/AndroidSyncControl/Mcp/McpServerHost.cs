#nullable enable
using AndroidSyncControl.Mcp.Interfaces;
using AndroidSyncControl.Mcp.Models;
using AndroidSyncControl.Mcp.Tools;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace AndroidSyncControl.Mcp;

/// <summary>
/// Runs the HTTP MCP endpoint on the loopback interface.
/// <see cref="IDeviceHost"/> and <see cref="IAdbCommandRunner"/> are borrowed and never disposed here.
/// Not thread-safe: callers serialize Start and Stop.
/// </summary>
public sealed class McpServerHost : IAsyncDisposable
{
    public const string EndpointPath = "/mcp";

    readonly IDeviceHost _deviceHost;
    readonly IAdbCommandRunner _adbCommandRunner;
    WebApplication? _app;

    public McpServerHost(IDeviceHost deviceHost, IAdbCommandRunner adbCommandRunner)
    {
        _deviceHost = deviceHost;
        _adbCommandRunner = adbCommandRunner;
    }

    /// <summary>Full endpoint address while running, otherwise null.</summary>
    public string? EndpointUrl { get; private set; }

    public async Task StartAsync(McpHostOptions options, CancellationToken cancellationToken = default)
    {
        if (_app is not null)
            throw new InvalidOperationException("The MCP server is already running.");

        McpAccessGuard guard = new McpAccessGuard(options.AccessToken);

        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions
        {
            ContentRootPath = AppContext.BaseDirectory,
        });
        builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(kestrel => kestrel.Listen(IPAddress.Loopback, options.Port));

        builder.Services.AddSingleton(_deviceHost);
        builder.Services.AddSingleton(_adbCommandRunner);
        builder.Services.AddSingleton<TouchGesturePlayer>();
        builder.Services
            .AddMcpServer(server => server.ServerInfo = new Implementation { Name = "AndroidSyncControl", Version = "1.0" })
            // Sessions live in IDeviceHost, so an agent that reconnects still finds its devices.
            .WithHttpTransport(transport => transport.Stateless = true)
            .WithTools<DeviceTools>()
            .WithTools<AdbTools>()
            .WithTools<ScreenTools>()
            .WithTools<InputTools>();

        WebApplication app = builder.Build();
        app.Use(guard.InvokeAsync);
        app.MapMcp(EndpointPath);

        try
        {
            await app.StartAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await app.DisposeAsync().ConfigureAwait(false);
            throw;
        }
        _app = app;
        EndpointUrl = app.Urls.First().TrimEnd('/') + EndpointPath;
    }

    public async Task StopAsync()
    {
        WebApplication? app = _app;
        if (app is null)
            return;
        _app = null;
        EndpointUrl = null;
        try
        {
            using CancellationTokenSource timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            await app.StopAsync(timeout.Token).ConfigureAwait(false);
        }
        finally
        {
            await app.DisposeAsync().ConfigureAwait(false);
        }
    }

    public ValueTask DisposeAsync() => new ValueTask(StopAsync());
}
