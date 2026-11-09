#nullable enable
using AndroidSyncControl.Mcp;
using AndroidSyncControl.Mcp.Helpers;
using AndroidSyncControl.Mcp.Interfaces;
using AndroidSyncControl.Mcp.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace AndroidSyncControl.Tests;

[TestClass]
public class McpBindAddressTests
{
    const string Token = "secret";

    static readonly IPAddress LanV4 = IPAddress.Parse("192.168.1.5");
    static readonly IPAddress LanV6 = IPAddress.Parse("2001:db8::5");

    [DataTestMethod]
    [DataRow("127.0.0.1", false, StatusCodes.Status200OK)]
    [DataRow("[::1]", false, StatusCodes.Status200OK)]
    [DataRow("localhost", false, StatusCodes.Status200OK)]
    [DataRow("192.168.1.5", false, StatusCodes.Status403Forbidden)]
    [DataRow("192.168.1.5", true, StatusCodes.Status200OK)]
    [DataRow("[2001:db8::5]", true, StatusCodes.Status200OK)]
    [DataRow("evil.example.com", true, StatusCodes.Status403Forbidden)]
    public async Task Guard_checks_host_for_the_bind_mode(string host, bool allowRemote, int expected)
    {
        Assert.AreEqual(expected, await InvokeGuardAsync(host, null, allowRemote));
    }

    [DataTestMethod]
    [DataRow(null)]
    [DataRow("wrong")]
    public async Task Guard_requires_the_token_when_open_to_the_network(string? token)
    {
        Assert.AreEqual(StatusCodes.Status401Unauthorized, await InvokeGuardAsync("192.168.1.5", null, true, token));
    }

    [TestMethod]
    public async Task Server_on_ipv6_wildcard_also_accepts_ipv4_clients()
    {
        await using McpServerHost server = new McpServerHost(new Mock<IDeviceHost>().Object, new Mock<IAdbCommandRunner>().Object);
        await server.StartAsync(new McpHostOptions { BindAddress = IPAddress.IPv6Any, Port = 0, AccessToken = Token });

        int port = new Uri(server.EndpointUrl!).Port;
        using HttpClient http = new HttpClient();
        using HttpResponseMessage response = await http.PostAsync($"http://127.0.0.1:{port}{McpServerHost.EndpointPath}", new StringContent("{}"));
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Guard_rejects_a_lan_origin_even_when_open_to_the_network()
    {
        Assert.AreEqual(StatusCodes.Status403Forbidden, await InvokeGuardAsync("192.168.1.5", "http://192.168.1.9", true));
    }

    [TestMethod]
    public void Client_address_replaces_wildcards_only()
    {
        IPAddress[] lan = { LanV4, LanV6 };
        Assert.AreEqual(LanV4, NetworkAddressHelper.GetClientAddress(IPAddress.Any, lan));
        Assert.AreEqual(LanV4, NetworkAddressHelper.GetClientAddress(IPAddress.IPv6Any, lan));
        Assert.AreEqual(LanV6, NetworkAddressHelper.GetClientAddress(LanV6, lan));
        Assert.AreEqual(IPAddress.IPv6Loopback, NetworkAddressHelper.GetClientAddress(IPAddress.IPv6Loopback, lan));
    }

    [TestMethod]
    public void Client_address_without_a_network_is_loopback()
    {
        Assert.AreEqual(IPAddress.Loopback, NetworkAddressHelper.GetClientAddress(IPAddress.Any, new[] { LanV6 }));
        Assert.AreEqual(IPAddress.IPv6Loopback, NetworkAddressHelper.GetClientAddress(IPAddress.IPv6Any, new IPAddress[0]));
    }

    [TestMethod]
    public async Task Server_listens_on_ipv6_loopback()
    {
        await using McpServerHost server = new McpServerHost(new Mock<IDeviceHost>().Object, new Mock<IAdbCommandRunner>().Object);
        await server.StartAsync(new McpHostOptions { BindAddress = IPAddress.IPv6Loopback, Port = 0, AccessToken = Token });

        StringAssert.StartsWith(server.EndpointUrl, "http://[::1]:");
        using HttpClient http = new HttpClient();
        using HttpResponseMessage response = await http.PostAsync(server.EndpointUrl, new StringContent("{}"));
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Wildcard_server_reports_a_connectable_address()
    {
        await using McpServerHost server = new McpServerHost(new Mock<IDeviceHost>().Object, new Mock<IAdbCommandRunner>().Object);
        await server.StartAsync(new McpHostOptions { BindAddress = IPAddress.Any, Port = 0, AccessToken = Token });

        Assert.IsFalse(server.EndpointUrl!.Contains("0.0.0.0"), server.EndpointUrl);
        using HttpClient http = new HttpClient();
        using HttpResponseMessage response = await http.PostAsync(server.EndpointUrl, new StringContent("{}"));
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    static async Task<int> InvokeGuardAsync(string host, string? origin, bool allowRemote, string? token = Token)
    {
        DefaultHttpContext context = new DefaultHttpContext();
        context.Request.Host = new HostString(host);
        if (token is not null)
            context.Request.Headers.Authorization = "Bearer " + token;
        if (origin is not null)
            context.Request.Headers.Origin = origin;
        bool passed = false;
        await new McpAccessGuard(Token, allowRemote).InvokeAsync(context, _ =>
        {
            passed = true;
            return Task.CompletedTask;
        });
        // A rejected request must not reach the endpoint, and 200 is also the default status.
        Assert.AreEqual(context.Response.StatusCode == StatusCodes.Status200OK, passed);
        return context.Response.StatusCode;
    }
}
