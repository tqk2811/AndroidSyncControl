#nullable enable
using AndroidSyncControl.Mcp;
using AndroidSyncControl.Mcp.Models;
using AndroidSyncControl.UI.ViewModels;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AndroidSyncControl.UI;

/// <summary>
/// Keeps the MCP server in step with the settings. Owns <see cref="McpServerHost"/> and stops it in
/// <see cref="Shutdown"/>. Called on the UI thread.
/// </summary>
internal sealed class McpServerController
{
    readonly MainWVM _viewModel;
    readonly McpServerHost _host;

    /// <summary>Toggling quickly must not overlap one start with the previous stop.</summary>
    readonly SemaphoreSlim _applyLock = new SemaphoreSlim(1, 1);

    public McpServerController(MainWVM viewModel, McpServerHost host)
    {
        _viewModel = viewModel;
        _host = host;
    }

    /// <summary>Never throws: a failure such as a busy port is shown as the status text.</summary>
    public async Task ApplyAsync()
    {
        await _applyLock.WaitAsync();
        try
        {
            await StopQuietlyAsync();
            _viewModel.McpStatus = string.Empty;
            if (!_viewModel.McpEnabled)
                return;

            McpHostOptions options = new McpHostOptions
            {
                Port = _viewModel.McpPort,
                AccessToken = _viewModel.EnsureMcpToken(),
            };
            // Building the ASP.NET host is slow enough to freeze the window.
            await Task.Run(() => _host.StartAsync(options));
            _viewModel.McpStatus = _host.EndpointUrl ?? string.Empty;
        }
        catch (Exception ex)
        {
            _viewModel.McpStatus = ex.Message;
        }
        finally
        {
            _applyLock.Release();
        }
    }

    /// <summary>Does not wait: an in-flight tool call may need the UI thread that is calling this.</summary>
    public void Shutdown() => _ = ShutdownAsync();

    async Task ShutdownAsync()
    {
        // The lock lets a start that is still running finish, so this stop cannot miss it.
        await _applyLock.WaitAsync().ConfigureAwait(false);
        await StopQuietlyAsync().ConfigureAwait(false);
    }

    /// <summary>The host has already detached the server when its stop fails, so a restart can still go on.</summary>
    async Task StopQuietlyAsync()
    {
        try { await _host.StopAsync(); }
        catch (Exception) { }
    }
}
