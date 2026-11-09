using AndroidSyncControl.Mcp;
using AndroidSyncControl.Mcp.Interfaces;
using AndroidSyncControl.Mcp.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Moq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TqkLibrary.Scrcpy.Enums;
using TqkLibrary.Scrcpy.Interfaces;

namespace AndroidSyncControl.Tests;

/// <summary>Drives the real HTTP endpoint with the MCP client; only the devices are faked.</summary>
[TestClass]
public class McpServerEndToEndTests
{
    const string Token = "test-token";
    const string DeviceId = "device-1";
    const string InitializeBody = "{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"ping\"}";

    readonly Mock<IDeviceHost> _deviceHost = new();
    readonly Mock<IAdbCommandRunner> _adb = new();
    readonly Mock<IDeviceSession> _session = new();
    readonly Mock<IControl> _control = new();
    McpServerHost _server = null!;
    McpClient _client = null!;

    [TestInitialize]
    public async Task Initialize()
    {
        _adb.Setup(x => x.ListDevicesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new AdbDeviceInfo { DeviceId = DeviceId, State = "Device" } });
        _adb.Setup(x => x.ExecuteAsync(DeviceId, It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AdbCommandResult { ExitCode = 0, Stdout = "out", Stderr = "", TimedOut = false });

        _control.Setup(x => x.InjectTouchEvent(It.IsAny<AndroidMotionEventAction>(), It.IsAny<long>(), It.IsAny<Rectangle>(), It.IsAny<float>(), It.IsAny<AndroidMotionEventButton>(), It.IsAny<AndroidMotionEventButton>()))
            .Returns(true);
        _session.SetupGet(x => x.DeviceId).Returns(DeviceId);
        _session.SetupGet(x => x.IsConnected).Returns(true);
        _session.SetupGet(x => x.ScreenSize).Returns(new Size(400, 800));
        _session.SetupGet(x => x.InputControl).Returns(_control.Object);
        _session.Setup(x => x.CaptureScreen()).Returns(() => FilledBitmap(400, 800));
        _deviceHost.Setup(x => x.FindAsync(DeviceId, It.IsAny<CancellationToken>())).ReturnsAsync(_session.Object);
        _deviceHost.Setup(x => x.GetSessionsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new[] { _session.Object });

        _server = new McpServerHost(_deviceHost.Object, _adb.Object);
        await _server.StartAsync(new McpHostOptions { Port = 0, AccessToken = Token });

        _client = await McpClient.CreateAsync(new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = new Uri(_server.EndpointUrl!),
            TransportMode = HttpTransportMode.StreamableHttp,
            AdditionalHeaders = new Dictionary<string, string> { ["Authorization"] = "Bearer " + Token },
        }));
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        await _client.DisposeAsync();
        await _server.DisposeAsync();
    }

    [TestMethod]
    public async Task ListTools_ExposesEveryTool()
    {
        var tools = await _client.ListToolsAsync();

        CollectionAssert.AreEquivalent(
            new[] { "device_list", "device_connect", "device_disconnect", "adb_execute", "screenshot", "tap", "long_press", "swipe", "scroll", "key", "text", "start_app" },
            tools.Select(x => x.Name).ToArray());
    }

    [TestMethod]
    public async Task Request_WithoutToken_IsUnauthorized()
    {
        using HttpResponseMessage response = await SendAsync(null, null, null);
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Request_WithWrongToken_IsUnauthorized()
    {
        using HttpResponseMessage response = await SendAsync("other-token", null, null);
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Request_WithForeignHost_IsForbiddenEvenWithToken()
    {
        using HttpResponseMessage response = await SendAsync(Token, "attacker.example", null);
        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [TestMethod]
    public async Task Request_WithForeignOrigin_IsForbiddenEvenWithToken()
    {
        using HttpResponseMessage response = await SendAsync(Token, null, "https://attacker.example");
        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [TestMethod]
    public async Task DeviceList_ReportsConnectedDevice()
    {
        CallToolResult result = await CallAsync("device_list");

        Assert.AreNotEqual(true, result.IsError);
        string text = ((TextContentBlock)result.Content[0]).Text;
        StringAssert.Contains(text, DeviceId);
        StringAssert.Contains(text, "400");
    }

    [TestMethod]
    public async Task DeviceConnect_UnknownDevice_NeverReachesTheHost()
    {
        CallToolResult result = await CallAsync("device_connect", ("deviceId", "bad id --flag"));

        Assert.IsTrue(result.IsError);
        _deviceHost.Verify(x => x.ConnectAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task AdbExecute_QuotesArguments()
    {
        CallToolResult result = await CallAsync("adb_execute", ("deviceId", DeviceId), ("arguments", new[] { "shell", "echo a b" }));

        Assert.AreNotEqual(true, result.IsError);
        _adb.Verify(x => x.ExecuteAsync(DeviceId, "shell \"echo a b\"", It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [DataTestMethod]
    [DataRow("-s")]
    [DataRow("kill-server")]
    [DataRow("disconnect")]
    public async Task AdbExecute_RejectsCommandsOutsideTheDevice(string firstArgument)
    {
        CallToolResult result = await CallAsync("adb_execute", ("deviceId", DeviceId), ("arguments", new[] { firstArgument, "other" }));

        Assert.IsTrue(result.IsError);
        VerifyAdbNeverExecuted();
    }

    [TestMethod]
    public async Task AdbExecute_UnknownDevice_IsRejected()
    {
        CallToolResult result = await CallAsync("adb_execute", ("deviceId", "bad id"), ("arguments", new[] { "shell", "ls" }));

        Assert.IsTrue(result.IsError);
        VerifyAdbNeverExecuted();
    }

    [TestMethod]
    public async Task Screenshot_ReturnsShrunkImageAndSizes()
    {
        CallToolResult result = await CallAsync("screenshot", ("deviceId", DeviceId), ("maxSize", 200));

        Assert.AreNotEqual(true, result.IsError);
        ImageContentBlock image = result.Content.OfType<ImageContentBlock>().Single();
        Assert.AreEqual("image/jpeg", image.MimeType);
        using (Bitmap decoded = new Bitmap(new System.IO.MemoryStream(image.DecodedData.ToArray())))
            Assert.AreEqual(new Size(100, 200), decoded.Size);
        string description = result.Content.OfType<TextContentBlock>().Single().Text;
        StringAssert.Contains(description, "\"imageWidth\":100");
        StringAssert.Contains(description, "\"screenWidth\":400");
    }

    [TestMethod]
    public async Task Tap_FromShrunkImage_TouchesTheMappedPoint()
    {
        CallToolResult result = await CallAsync("tap", ("deviceId", DeviceId), ("x", 50), ("y", 100), ("imageWidth", 100), ("imageHeight", 200));

        Assert.AreNotEqual(true, result.IsError);
        _control.Verify(x => x.InjectTouchEvent(AndroidMotionEventAction.ACTION_DOWN, It.IsAny<long>(), new Rectangle(200, 400, 400, 800), 1f, It.IsAny<AndroidMotionEventButton>(), It.IsAny<AndroidMotionEventButton>()), Times.Once);
    }

    [TestMethod]
    public async Task Tap_WithoutSession_IsRejected()
    {
        CallToolResult result = await CallAsync("tap", ("deviceId", "not-open"), ("x", 1), ("y", 1));

        Assert.IsTrue(result.IsError);
        StringAssert.Contains(((TextContentBlock)result.Content[0]).Text, "device_connect");
    }

    [TestMethod]
    public async Task Text_NonAscii_IsPastedThroughTheClipboard()
    {
        _control.Setup(x => x.SetClipboard("Xin chào", true)).Returns(true);

        CallToolResult result = await CallAsync("text", ("deviceId", DeviceId), ("text", "Xin chào"));

        Assert.AreNotEqual(true, result.IsError);
        _control.Verify(x => x.InjectText(It.IsAny<string>()), Times.Never);
    }

    void VerifyAdbNeverExecuted()
        => _adb.Verify(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);

    Task<CallToolResult> CallAsync(string tool, params (string Name, object Value)[] arguments)
        => _client.CallToolAsync(tool, arguments.ToDictionary(x => x.Name, x => (object?)x.Value)).AsTask();

    async Task<HttpResponseMessage> SendAsync(string? token, string? host, string? origin)
    {
        using HttpClient http = new HttpClient();
        using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, _server.EndpointUrl)
        {
            Content = new StringContent(InitializeBody, Encoding.UTF8, "application/json"),
        };
        request.Headers.Accept.ParseAdd("application/json");
        request.Headers.Accept.ParseAdd("text/event-stream");
        if (token is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (host is not null)
            request.Headers.Host = host;
        if (origin is not null)
            request.Headers.Add("Origin", origin);
        return await http.SendAsync(request);
    }

    static Bitmap FilledBitmap(int width, int height)
    {
        Bitmap bitmap = new Bitmap(width, height);
        using Graphics graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.SteelBlue);
        return bitmap;
    }
}
