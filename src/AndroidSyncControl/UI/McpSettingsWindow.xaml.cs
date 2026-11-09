#nullable enable
using AndroidSyncControl.Localization;
using AndroidSyncControl.Mcp.Helpers;
using AndroidSyncControl.UI.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;

namespace AndroidSyncControl.UI;

/// <summary>Settings of the MCP server. Binds to the view model of the main window.</summary>
public partial class McpSettingsWindow : Window
{
    readonly MainWVM _viewModel;

    internal McpSettingsWindow(MainWVM viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        // The token field must not be blank before the server has been enabled once.
        _viewModel.EnsureMcpToken();
        // Must be filled before the binding of SelectedItem is evaluated.
        cbo_host.ItemsSource = BuildHostList();
        DataContext = viewModel;
    }

    List<string> BuildHostList()
    {
        List<string> hosts = new List<string> { IPAddress.Loopback.ToString(), IPAddress.IPv6Loopback.ToString() };
        hosts.AddRange(NetworkAddressHelper.GetLanAddresses().Select(x => x.ToString()));
        hosts.Add(IPAddress.Any.ToString());
        hosts.Add(IPAddress.IPv6Any.ToString());
        // A saved LAN address may be gone after the network changed.
        if (!hosts.Contains(_viewModel.McpHost))
            hosts.Add(_viewModel.McpHost);
        return hosts;
    }

    private void btn_copy_Click(object sender, RoutedEventArgs e)
    {
        // McpStatus holds an error message instead of the address when the server failed to start.
        if (!_viewModel.McpStatus.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            return;

        string config = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            ["mcpServers"] = new Dictionary<string, object>
            {
                ["android-sync-control"] = new Dictionary<string, object>
                {
                    ["type"] = "http",
                    ["url"] = _viewModel.McpStatus,
                    ["headers"] = new Dictionary<string, string>
                    {
                        ["Authorization"] = "Bearer " + _viewModel.McpToken,
                    },
                },
            },
        }, new JsonSerializerOptions { WriteIndented = true });
        try { Clipboard.SetText(config); }
        catch (COMException) { /* clipboard is locked by another process */ }
    }

    private void btn_newToken_Click(object sender, RoutedEventArgs e)
    {
        MessageBoxResult answer = MessageBox.Show(
            this,
            LanguageManager.GetString("Str.Mcp.NewTokenConfirm"),
            LanguageManager.GetString("Str.Mcp.Title"),
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (answer == MessageBoxResult.Yes)
            _viewModel.RegenerateMcpToken();
    }
}
