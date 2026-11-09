using AndroidSyncControl.Mcp;
using AndroidSyncControl.Mcp.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TqkLibrary.Scrcpy.Enums;
using TqkLibrary.Scrcpy.Interfaces;

namespace AndroidSyncControl.Tests;

[TestClass]
public class TouchGesturePlayerTests
{
    readonly List<(AndroidMotionEventAction Action, Point Point)> _events = new();
    CancellationTokenSource? _cancelOnFirstMove;
    Mock<IDeviceSession> _session = null!;

    [TestInitialize]
    public void Initialize()
    {
        Mock<IControl> control = new Mock<IControl>();
        control
            .Setup(x => x.InjectTouchEvent(It.IsAny<AndroidMotionEventAction>(), It.IsAny<long>(), It.IsAny<Rectangle>(), It.IsAny<float>(), It.IsAny<AndroidMotionEventButton>(), It.IsAny<AndroidMotionEventButton>()))
            .Returns((AndroidMotionEventAction action, long _, Rectangle position, float _, AndroidMotionEventButton _, AndroidMotionEventButton _) =>
            {
                _events.Add((action, position.Location));
                if (action == AndroidMotionEventAction.ACTION_MOVE)
                    _cancelOnFirstMove?.Cancel();
                return true;
            });
        _session = new Mock<IDeviceSession>();
        _session.SetupGet(x => x.DeviceId).Returns("device");
        _session.SetupGet(x => x.ScreenSize).Returns(new Size(100, 100));
        _session.SetupGet(x => x.InputControl).Returns(control.Object);
    }

    [TestMethod]
    public async Task Tap_SendsDownThenUpAtThePoint()
    {
        await new TouchGesturePlayer().TapAsync(_session.Object, new Point(5, 6), 0, CancellationToken.None);

        CollectionAssert.AreEqual(
            new[] { (AndroidMotionEventAction.ACTION_DOWN, new Point(5, 6)), (AndroidMotionEventAction.ACTION_UP, new Point(5, 6)) },
            _events);
    }

    [TestMethod]
    public async Task Swipe_LiftsAtTheDestination()
    {
        await new TouchGesturePlayer().SwipeAsync(_session.Object, new Point(0, 0), new Point(50, 80), 64, CancellationToken.None);

        Assert.AreEqual((AndroidMotionEventAction.ACTION_UP, new Point(50, 80)), _events.Last());
        Assert.IsTrue(_events.Any(x => x.Action == AndroidMotionEventAction.ACTION_MOVE));
    }

    [TestMethod]
    public async Task Swipe_WithoutDuration_StillEndsAtTheDestination()
    {
        await new TouchGesturePlayer().SwipeAsync(_session.Object, new Point(0, 0), new Point(50, 80), 0, CancellationToken.None);

        Assert.AreEqual((AndroidMotionEventAction.ACTION_UP, new Point(50, 80)), _events.Last());
    }

    [TestMethod]
    public async Task Swipe_ShorterThanOneStep_StillMovesToTheDestination()
    {
        await new TouchGesturePlayer().SwipeAsync(_session.Object, new Point(0, 0), new Point(50, 80), 0, CancellationToken.None);

        CollectionAssert.Contains(_events, (AndroidMotionEventAction.ACTION_MOVE, new Point(50, 80)));
    }

    [TestMethod]
    public async Task Swipe_WhenCancelled_StillLiftsTheFinger()
    {
        _cancelOnFirstMove = new CancellationTokenSource();

        await Assert.ThrowsExceptionAsync<TaskCanceledException>(() =>
            new TouchGesturePlayer().SwipeAsync(_session.Object, new Point(0, 0), new Point(90, 90), 5000, _cancelOnFirstMove.Token));

        Assert.AreEqual(AndroidMotionEventAction.ACTION_UP, _events.Last().Action);
        Assert.IsTrue(_events.Count < 10);
    }
}
