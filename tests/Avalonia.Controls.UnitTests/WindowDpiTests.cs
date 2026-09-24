using System;
using Avalonia.Platform;
using Avalonia.UnitTests;
using Moq;
using Xunit;

namespace Avalonia.Controls.UnitTests;

public class WindowDpiTests
{
    [Theory]
    [InlineData(1.0, 1.5)]
    [InlineData(1.5, 1.0)]
    [InlineData(1.25, 2.0)]
    [InlineData(2.0, 1.25)]
    public void Manual_Window_Uses_Native_Monitor_Instead_Of_Adjusted_Position(double nativeScale, double pointScale)
    {
        using var app = UnitTestApplication.Start(TestServices.StyledWindow);
        var impl = MockWindowingPlatform.CreateWindowMock();
        impl.Setup(x => x.RenderScaling).Returns(nativeScale);
        impl.Setup(x => x.DesktopScaling).Returns(nativeScale);
        impl.Setup(x => x.Position).Returns(new PixelPoint(2200, 100));

        var nativeScreen = new Screen(nativeScale, new PixelRect(0, 0, 1920, 1080), new PixelRect(0, 0, 1920, 1080), true);
        var pointScreen = new Screen(pointScale, new PixelRect(1920, 0, 2560, 1440), new PixelRect(1920, 0, 2560, 1440), false);
        var screens = new Mock<IScreenImpl>();
        screens.Setup(x => x.AllScreens).Returns(new[] { nativeScreen, pointScreen });
        screens.Setup(x => x.ScreenFromWindow(impl.Object)).Returns(nativeScreen);
        screens.Setup(x => x.ScreenFromPoint(It.IsAny<PixelPoint>())).Returns(pointScreen);
        impl.Setup(x => x.TryGetFeature(typeof(IScreenImpl))).Returns(screens.Object);

        var window = new Window(impl.Object) { WindowState = WindowState.Maximized };
        try
        {
            window.Show();

            impl.Verify(x => x.Move(It.IsAny<PixelPoint>()), Times.Never);
            screens.Verify(x => x.ScreenFromWindow(impl.Object), Times.Once);
            screens.Verify(x => x.ScreenFromPoint(It.IsAny<PixelPoint>()), Times.Never);
            Assert.Equal(nativeScale, window.RenderScaling);
        }
        finally
        {
            window.Close();
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Manual_Window_Still_Corrects_Scaling_With_Native_Monitor_Or_Point_Fallback(bool nativeMonitorAvailable)
    {
        using var app = UnitTestApplication.Start(TestServices.StyledWindow);
        var impl = MockWindowingPlatform.CreateWindowMock();
        var position = new PixelPoint(2200, 100);
        impl.Setup(x => x.Position).Returns(position);

        var screen = new Screen(1.5, new PixelRect(1920, 0, 2560, 1440), new PixelRect(1920, 0, 2560, 1440), false);
        var screens = new Mock<IScreenImpl>();
        screens.Setup(x => x.AllScreens).Returns(new[] { screen });
        screens.Setup(x => x.ScreenFromWindow(impl.Object)).Returns(nativeMonitorAvailable ? screen : null);
        screens.Setup(x => x.ScreenFromPoint(position)).Returns(screen);
        impl.Setup(x => x.TryGetFeature(typeof(IScreenImpl))).Returns(screens.Object);

        var window = new Window(impl.Object);
        try
        {
            window.Show();

            impl.Verify(x => x.Move(position), Times.Once);
            screens.Verify(x => x.ScreenFromPoint(position), nativeMonitorAvailable ? Times.Never() : Times.Once());
        }
        finally
        {
            window.Close();
        }
    }
}
