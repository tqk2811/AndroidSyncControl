#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace AndroidSyncControl.Mcp.Helpers;

public static class NetworkAddressHelper
{
    /// <summary>
    /// Addresses of this PC on its networks, IPv4 first and adapters with a gateway first.
    /// Link-local and temporary IPv6 addresses are left out: a client cannot rely on them.
    /// </summary>
    public static IReadOnlyList<IPAddress> GetLanAddresses()
    {
        return NetworkInterface.GetAllNetworkInterfaces()
            .Where(x => x.OperationalStatus == OperationalStatus.Up
                && x.NetworkInterfaceType != NetworkInterfaceType.Loopback
                && x.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
            .Select(x => x.GetIPProperties())
            .OrderByDescending(x => x.GatewayAddresses.Count > 0)
            .SelectMany(x => x.UnicastAddresses)
            .Where(x => x.SuffixOrigin != SuffixOrigin.Random)
            .Select(x => x.Address)
            .Where(IsReachableFromLan)
            .Distinct()
            .OrderBy(x => x.AddressFamily == AddressFamily.InterNetwork ? 0 : 1)
            .ToList();
    }

    /// <summary>The address a client should use. A wildcard bind address cannot be connected to.</summary>
    public static IPAddress GetClientAddress(IPAddress bindAddress, IReadOnlyList<IPAddress> lanAddresses)
    {
        if (bindAddress.Equals(IPAddress.Any))
            return lanAddresses.FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetwork) ?? IPAddress.Loopback;
        if (bindAddress.Equals(IPAddress.IPv6Any))
            return lanAddresses.FirstOrDefault() ?? IPAddress.IPv6Loopback;
        return bindAddress;
    }

    static bool IsReachableFromLan(IPAddress address)
    {
        if (address.AddressFamily == AddressFamily.InterNetworkV6)
            return !address.IsIPv6LinkLocal;
        if (address.AddressFamily != AddressFamily.InterNetwork)
            return false;
        byte[] bytes = address.GetAddressBytes();
        return !(bytes[0] == 169 && bytes[1] == 254);
    }
}
