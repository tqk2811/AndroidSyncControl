#nullable enable
using AndroidSyncControl.Mcp.Interfaces;
using AndroidSyncControl.UI.ViewModels;
using ModelContextProtocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace AndroidSyncControl.UI;

/// <summary>
/// Exposes the devices shown in the window to MCP. Every access to the view models runs on the
/// UI thread. The views stay owned by <see cref="MainWVM"/> and the window.
/// </summary>
internal sealed class McpDeviceHost : IDeviceHost
{
    readonly MainWVM _viewModel;
    readonly Dispatcher _dispatcher;
    readonly Action _removeMainDeviceView;

    static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(30);

    /// <summary>Serializes the connect and disconnect requests coming from MCP.</summary>
    readonly SemaphoreSlim _sessionChangeLock = new SemaphoreSlim(1, 1);

    bool _isClosed;

    public McpDeviceHost(MainWVM viewModel, Dispatcher dispatcher, Action removeMainDeviceView)
    {
        _viewModel = viewModel;
        _dispatcher = dispatcher;
        _removeMainDeviceView = removeMainDeviceView;
    }

    /// <summary>Must run on the UI thread, before the views are disposed.</summary>
    public void Close() => _isClosed = true;

    public Task<IReadOnlyList<IDeviceSession>> GetSessionsAsync(CancellationToken cancellationToken)
        => OnUiThreadAsync<IReadOnlyList<IDeviceSession>>(() => AllViews().ToList(), cancellationToken);

    public async Task<IDeviceSession?> FindAsync(string deviceId, CancellationToken cancellationToken)
        => await OnUiThreadAsync(() => FindView(deviceId), cancellationToken).ConfigureAwait(false);

    public async Task<IDeviceSession> ConnectAsync(string deviceId, CancellationToken cancellationToken)
    {
        await _sessionChangeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            DeviceView? existing = await OnUiThreadAsync(() => FindView(deviceId), cancellationToken).ConfigureAwait(false);
            if (existing is not null)
            {
                if (!existing.IsConnected)
                    throw new McpException($"Device '{deviceId}' is open but its scrcpy session is not live. Retry shortly, or call device_disconnect and connect again.");
                return existing;
            }

            Task<DeviceView> start = await OnUiThreadAsync(() => _viewModel.ShowInListAsync(deviceId), cancellationToken).ConfigureAwait(false);
            // scrcpy's Connect can block forever when the server never calls back, so the lock must not wait on it unbounded.
            Task finished = await Task.WhenAny(start, Task.Delay(ConnectTimeout, cancellationToken)).ConfigureAwait(false);
            if (finished != start)
            {
                cancellationToken.ThrowIfCancellationRequested();
                throw new McpException($"scrcpy did not connect to device '{deviceId}' in time, it is still starting in the app window.");
            }

            DeviceView view = await start.ConfigureAwait(false);
            if (!view.IsConnected)
            {
                await OnUiThreadAsync(() => _viewModel.RemoveFromList(view), CancellationToken.None).ConfigureAwait(false);
                throw new McpException($"scrcpy could not connect to device '{deviceId}'.");
            }
            return view;
        }
        finally
        {
            _sessionChangeLock.Release();
        }
    }

    public async Task<bool> DisconnectAsync(string deviceId, CancellationToken cancellationToken)
    {
        await _sessionChangeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await OnUiThreadAsync(() => RemoveView(deviceId), cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _sessionChangeLock.Release();
        }
    }

    bool RemoveView(string deviceId)
    {
        List<DeviceView> views = AllViews().Where(x => x.DeviceId == deviceId).ToList();
        // Disposing a view whose Connect is still blocked would freeze the UI thread.
        if (views.Any(x => x.IsStarting))
            throw new McpException($"Device '{deviceId}' is still connecting, retry later.");

        foreach (DeviceView view in views)
        {
            if (ReferenceEquals(view, _viewModel.DeviceView))
                _removeMainDeviceView();
            else
                _viewModel.RemoveFromList(view);
        }
        return views.Count > 0;
    }

    DeviceView? FindView(string deviceId) => AllViews().FirstOrDefault(x => x.DeviceId == deviceId);

    IEnumerable<DeviceView> AllViews()
        => _viewModel.DeviceView is null
            ? _viewModel.DeviceViews
            : _viewModel.DeviceViews.Prepend(_viewModel.DeviceView);

    Task OnUiThreadAsync(Action action, CancellationToken cancellationToken)
        => OnUiThreadAsync<object?>(() => { action(); return null; }, cancellationToken);

    Task<T> OnUiThreadAsync<T>(Func<T> function, CancellationToken cancellationToken)
        => _dispatcher.InvokeAsync(() =>
        {
            if (_isClosed)
                throw new McpException("The application is closing.");
            return function();
        }, DispatcherPriority.Normal, cancellationToken).Task;
}
