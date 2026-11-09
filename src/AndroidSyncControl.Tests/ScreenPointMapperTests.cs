using AndroidSyncControl.Mcp.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ModelContextProtocol;
using System.Drawing;

namespace AndroidSyncControl.Tests;

[TestClass]
public class ScreenPointMapperTests
{
    static readonly Size Screen = new Size(1080, 2400);

    [TestMethod]
    public void Map_WithoutImageSize_KeepsScreenCoordinates()
    {
        Assert.AreEqual(new Point(100, 200), ScreenPointMapper.Map(100, 200, null, null, Screen));
    }

    [TestMethod]
    public void Map_FromShrunkImage_ScalesToTheScreen()
    {
        Assert.AreEqual(new Point(540, 1200), ScreenPointMapper.Map(270, 600, 540, 1200, Screen));
    }

    [TestMethod]
    public void Map_LastPixelOfImage_StaysOnTheScreen()
    {
        Assert.AreEqual(new Point(1078, 2398), ScreenPointMapper.Map(539, 1199, 540, 1200, Screen));
    }

    [DataTestMethod]
    [DataRow(-1, 0)]
    [DataRow(0, -1)]
    [DataRow(1080, 0)]
    [DataRow(0, 2400)]
    public void Map_OutsideTheScreen_IsRejected(int x, int y)
    {
        Assert.ThrowsException<McpException>(() => ScreenPointMapper.Map(x, y, null, null, Screen));
    }

    [TestMethod]
    public void Map_WithOnlyOneImageDimension_IsRejected()
    {
        Assert.ThrowsException<McpException>(() => ScreenPointMapper.Map(1, 1, 100, null, Screen));
    }

    [TestMethod]
    public void Map_BeforeTheScreenSizeIsKnown_IsRejected()
    {
        Assert.ThrowsException<McpException>(() => ScreenPointMapper.Map(1, 1, null, null, Size.Empty));
    }
}
