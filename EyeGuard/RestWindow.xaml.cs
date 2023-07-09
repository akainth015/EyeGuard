using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using System.Runtime.InteropServices;
using Windows.Foundation;
using static EyeGuard.PInvokeUtils;

namespace EyeGuard
{
    public sealed partial class RestWindow : Window
    {
        public TypedEventHandler<RestWindow, object> SkipBreakPressed = (RestWindow sender, object e) => { };

        public RestWindow()
        {
            InitializeComponent();
            AppWindow.IsShownInSwitchers = false;
        }

        private void ForceFocus()
        {
            // Get window handle
            IntPtr windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(this);
            ShowWindow(windowHandle, 6);
            SetForegroundWindow(windowHandle);
            ShowWindow(windowHandle, 3);
        }

        public void FullscreenOnMonitor(IntPtr hMonitor)
        {
            MonitorInfo monitorInfo = new MonitorInfo();
            GetMonitorInfo(hMonitor, ref monitorInfo);

            int x = monitorInfo.WorkArea.Left;
            int y = monitorInfo.WorkArea.Top;
            var pointOnMonitor = new Windows.Graphics.PointInt32(x, y);

            DispatcherQueue.TryEnqueue(() =>
            {
                AppWindow.Move(pointOnMonitor);
                ForceFocus();
                AppWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
            });
        }

        private void SkipBreak(object sender, RoutedEventArgs e)
        {
            SkipBreakPressed(this, null);
        }

        /// <summary>
        ///     Brings the thread that created the specified window into the foreground and activates the window. Keyboard input is
        ///     directed to the window, and various visual cues are changed for the user. The system assigns a slightly higher
        ///     priority to the thread that created the foreground window than it does to other threads.
        ///     <para>See for https://msdn.microsoft.com/en-us/library/windows/desktop/ms633539%28v=vs.85%29.aspx more information.</para>
        /// </summary>
        /// <param name="hWnd">
        ///     C++ ( hWnd [in]. Type: HWND )<br />A handle to the window that should be activated and brought to the foreground.
        /// </param>
        /// <returns>
        ///     <c>true</c> or nonzero if the window was brought to the foreground, <c>false</c> or zero If the window was not
        ///     brought to the foreground.
        /// </returns>
        /// <remarks>
        ///     The system restricts which processes can set the foreground window. A process can set the foreground window only if
        ///     one of the following conditions is true:
        ///     <list type="bullet">
        ///     <listheader>
        ///         <term>Conditions</term><description></description>
        ///     </listheader>
        ///     <item>The process is the foreground process.</item>
        ///     <item>The process was started by the foreground process.</item>
        ///     <item>The process received the last input event.</item>
        ///     <item>There is no foreground process.</item>
        ///     <item>The process is being debugged.</item>
        ///     <item>The foreground process is not a Modern Application or the Start Screen.</item>
        ///     <item>The foreground is not locked (see LockSetForegroundWindow).</item>
        ///     <item>The foreground lock time-out has expired (see SPI_GETFOREGROUNDLOCKTIMEOUT in SystemParametersInfo).</item>
        ///     <item>No menus are active.</item>
        ///     </list>
        ///     <para>
        ///     An application cannot force a window to the foreground while the user is working with another window.
        ///     Instead, Windows flashes the taskbar button of the window to notify the user.
        ///     </para>
        ///     <para>
        ///     A process that can set the foreground window can enable another process to set the foreground window by
        ///     calling the AllowSetForegroundWindow function. The process specified by dwProcessId loses the ability to set
        ///     the foreground window the next time the user generates input, unless the input is directed at that process, or
        ///     the next time a process calls AllowSetForegroundWindow, unless that process is specified.
        ///     </para>
        ///     <para>
        ///     The foreground process can disable calls to SetForegroundWindow by calling the LockSetForegroundWindow
        ///     function.
        ///     </para>
        /// </remarks>
        // For Windows Mobile, replace user32.dll with coredll.dll
        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool SetForegroundWindow(IntPtr hWnd);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool DestroyWindow(IntPtr hWnd);
    }
}
