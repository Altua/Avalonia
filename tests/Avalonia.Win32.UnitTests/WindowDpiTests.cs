using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.UnitTests;
using Xunit;
using static Avalonia.Win32.Interop.UnmanagedMethods;

namespace Avalonia.Win32.UnitTests;

public class WindowDpiTests
{
    [SkippableFact]
    public void Native_Windows_Notify_Dpi_Changes_And_Protect_Overlay_Placement()
    {
        Skip.IfNot(OperatingSystem.IsWindows(), "Requires real Win32 windows.");

        Exception failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                VerifyWindows();
            }
            catch (Exception e)
            {
                failure = e;
            }
        }) { IsBackground = true };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(30)), "Win32 DPI test did not complete.");
        if (failure != null)
            ExceptionDispatchInfo.Capture(failure).Throw();
    }

    private static void VerifyWindows()
    {
        using var app = UnitTestApplication.Start(TestServices.StyledWindow);
        using var shutdown = new CancellationTokenSource();
        Win32Platform.Initialize(new Win32PlatformOptions
        {
            RenderingMode = new[] { Win32RenderingMode.Software },
            CompositionMode = new[] { Win32CompositionMode.RedirectionSurface },
            ShutdownCancellationToken = shutdown.Token
        });

        var hostImpl = new WindowImpl();
        var host = new Window(hostImpl);
        try
        {
            var initialScale = hostImpl.RenderScaling;
            var notifications = new List<double>();
            host.ScalingChanged += (_, _) => notifications.Add(host.RenderScaling);

            // Simulate a DPI message without moving to another physical monitor. The next Move
            // must resample the real DPI and notify TopLevel, even without a second WM_DPICHANGED.
            var simulatedDpi = initialScale == 1.5 ? 192u : 144u;
            Assert.True(GetWindowRect(hostImpl.Handle.Handle, out var hostRect));
            var rectPointer = Marshal.AllocHGlobal(Marshal.SizeOf<RECT>());
            try
            {
                Marshal.StructureToPtr(hostRect, rectPointer, false);
                SendMessage(hostImpl.Handle.Handle, (int)WindowsMessage.WM_DPICHANGED,
                    new IntPtr((simulatedDpi << 16) | simulatedDpi), rectPointer);
            }
            finally
            {
                Marshal.FreeHGlobal(rectPointer);
            }

            Assert.Equal(simulatedDpi / WindowImpl.StandardDpi, host.RenderScaling);
            hostImpl.Move(hostImpl.Position);
            Assert.Equal(initialScale, hostImpl.RenderScaling);
            Assert.Equal(initialScale, host.RenderScaling);
            Assert.Equal(new[] { simulatedDpi / WindowImpl.StandardDpi, initialScale }, notifications);

            // Moving again on the same monitor must not publish a duplicate scale notification.
            hostImpl.Move(hostImpl.Position);
            Assert.Equal(2, notifications.Count);

            using var overlay = LegacyOverlay.Create(hostImpl.Handle.Handle);
            var originalRect = GetBounds(overlay.Handle.Handle);
            ((IWindowImpl)overlay).Move(new PixelPoint(2500, 1200));
            Assert.Equal(originalRect, GetBounds(overlay.Handle.Handle));
            ((WindowImpl)overlay).Move(new PixelPoint(-1800, 300));
            Assert.Equal(originalRect, GetBounds(overlay.Handle.Handle));
            ((WindowImpl)overlay).Position = new PixelPoint(900, 600);
            Assert.Equal(originalRect, GetBounds(overlay.Handle.Handle));

            // The native host still controls the child directly, as PowerPoint does.
            SetWindowPos(overlay.Handle.Handle, IntPtr.Zero, 20, 30, 0, 0,
                SetWindowPosFlags.SWP_NOSIZE | SetWindowPosFlags.SWP_NOZORDER | SetWindowPosFlags.SWP_NOACTIVATE);
            Assert.NotEqual(originalRect, GetBounds(overlay.Handle.Handle));
        }
        finally
        {
            host.Close();
            shutdown.Cancel();
        }
    }

    private static (int Left, int Top, int Right, int Bottom) GetBounds(IntPtr hwnd)
    {
        Assert.True(GetWindowRect(hwnd, out var rect));
        return (rect.left, rect.top, rect.right, rect.bottom);
    }

    // Mirror the existing add-in's interface reimplementation and hidden Position property.
    private sealed class LegacyOverlay : WindowsOverlayWindowImpl, IWindowImpl, ITopLevelImpl
    {
        private LegacyOverlay(IntPtr host) : base(host, host) { }

        public static LegacyOverlay Create(IntPtr host)
        {
            InitParentWindow = host;
            try
            {
                return new LegacyOverlay(host);
            }
            finally
            {
                InitParentWindow = IntPtr.Zero;
            }
        }

        public new PixelPoint Position
        {
            get => base.Position.WithX(base.Position.X + 1000);
            set { }
        }
    }
}
