#nullable enable
using Microsoft.AspNetCore.Http;
using System;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AndroidSyncControl.Mcp;

/// <summary>
/// Middleware protecting the endpoint, which can run any command on the attached devices.
/// Host and Origin stop web pages reaching it through DNS rebinding; the token stops everything else.
/// </summary>
public sealed class McpAccessGuard
{
    const string BearerPrefix = "Bearer ";

    readonly byte[] _expectedToken;

    public McpAccessGuard(string accessToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
            throw new ArgumentException("An access token is required.", nameof(accessToken));
        _expectedToken = Encoding.UTF8.GetBytes(accessToken);
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (!IsLoopbackHost(context.Request.Host.Host) || !IsAllowedOrigin(context.Request.Headers.Origin.ToString()))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }
        if (!HasValidToken(context.Request.Headers.Authorization.ToString()))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.Headers.WWWAuthenticate = "Bearer";
            return;
        }
        await next(context);
    }

    bool HasValidToken(string authorization)
    {
        if (!authorization.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
            return false;
        byte[] actual = Encoding.UTF8.GetBytes(authorization.Substring(BearerPrefix.Length).Trim());
        return CryptographicOperations.FixedTimeEquals(actual, _expectedToken);
    }

    static bool IsAllowedOrigin(string origin)
    {
        // Native MCP clients send no Origin, browsers always do on these requests.
        if (string.IsNullOrEmpty(origin))
            return true;
        return Uri.TryCreate(origin, UriKind.Absolute, out Uri? uri) && IsLoopbackHost(uri.Host);
    }

    static bool IsLoopbackHost(string host)
    {
        if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase))
            return true;
        return IPAddress.TryParse(host, out IPAddress? address) && IPAddress.IsLoopback(address);
    }
}
