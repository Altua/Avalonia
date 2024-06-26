using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Avalonia.Win32.Interop;
namespace Avalonia.Win32
{
    internal class OffscreenParentWindow
    {

        private static IntPtr s_handle = IntPtr.Zero;
        private static UnmanagedMethods.WndProc? s_wndProcDelegate;
        public static IntPtr Handle
        {
            get
            {
                if (s_handle == IntPtr.Zero)
                {
                    s_handle = CreateParentWindow();
                }

                return s_handle;
            }
        }

        public static void Destroy()
        {
            if (Handle != IntPtr.Zero)
            {
                UnmanagedMethods.DestroyWindow(Handle);
                s_handle = IntPtr.Zero;
            }
        }

        private static IntPtr CreateParentWindow()
        {
            s_wndProcDelegate = ParentWndProc;

            var wndClassEx = new UnmanagedMethods.WNDCLASSEX
            {
                cbSize = Marshal.SizeOf<UnmanagedMethods.WNDCLASSEX>(),
                hInstance = UnmanagedMethods.GetModuleHandle(null),
                lpfnWndProc = s_wndProcDelegate,
                lpszClassName = "AvaloniaEmbeddedWindow-" + Guid.NewGuid(),
            };

            var atom = UnmanagedMethods.RegisterClassEx(ref wndClassEx);

            if (atom == 0)
            {
                throw new Win32Exception();
            }

            var hwnd = UnmanagedMethods.CreateWindowEx(
                0,
                atom,
                null,
                (int)UnmanagedMethods.WindowStyles.WS_OVERLAPPEDWINDOW,
                UnmanagedMethods.CW_USEDEFAULT,
                UnmanagedMethods.CW_USEDEFAULT,
                UnmanagedMethods.CW_USEDEFAULT,
                UnmanagedMethods.CW_USEDEFAULT,
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero);

            if (hwnd == IntPtr.Zero)
            {
                throw new Win32Exception();
            }

            return hwnd;
        }

        private static IntPtr ParentWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            return UnmanagedMethods.DefWindowProc(hWnd, msg, wParam, lParam);
        }
    }
}
