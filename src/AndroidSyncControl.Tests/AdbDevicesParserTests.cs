using AndroidSyncControl.Mcp.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace AndroidSyncControl.Tests;

[TestClass]
public class AdbDevicesParserTests
{
    [TestMethod]
    public void Parse_KeepsDevicesInStatesTheLibraryEnumRejects()
    {
        const string output = "* daemon started successfully\r\nList of devices attached\r\nabc\tdevice\r\n10.0.0.2:5555\tconnecting\r\nxyz\tno permissions (udev)\r\n\r\n";

        var devices = AdbDevicesParser.Parse(output);

        CollectionAssert.AreEqual(new[] { "abc", "10.0.0.2:5555", "xyz" }, devices.Select(x => x.DeviceId).ToArray());
        CollectionAssert.AreEqual(new[] { "device", "connecting", "no permissions (udev)" }, devices.Select(x => x.State).ToArray());
    }

    [TestMethod]
    public void Parse_KeepsSerialWithSpaces()
    {
        var devices = AdbDevicesParser.Parse("List of devices attached\nadb-1 (2)._adb-tls-connect._tcp\tdevice\n");

        Assert.AreEqual("adb-1 (2)._adb-tls-connect._tcp", devices.Single().DeviceId);
    }
}
