using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.UI.Shell;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace EyeGuard
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override async void OnLaunched(LaunchActivatedEventArgs args)
        {
            AppInstance currentInstance = AppInstance.GetCurrent();
            AppActivationArguments activationArguments = AppInstance.GetCurrent().GetActivatedEventArgs();

            currentInstance.Activated += OnActivated;

            secretWindow = new MainWindow();
            secretWindow.AppWindow.Show();
            secretWindow.AppWindow.Hide();


            // What caused the app to be launched - the user, or being registered as a startup app?

            if (activationArguments.Kind == ExtendedActivationKind.Launch)
            {
                // Since Program.cs guarantees that this IS the main instance, if it was launched by the user
                // and NOT because the app was registered as a startup application, we will send a notification 
                // letting the user know that EyeGuard is running in the background.
                var appNotification = new AppNotificationBuilder()
                    .AddText("EyeGuard")
                    .AddText("EyeGuard is running in the background, and will show itself when its time to rest.")
                    .BuildNotification();

                AppNotificationManager.Default.Show(appNotification);
            }
            while (true)
            {
                await Task.Delay(TimeSpan.FromMinutes(20));

                bool isFocusMode = false;
                if (FocusSessionManager.IsSupported)
                {
                    isFocusMode = FocusSessionManager.GetDefault().IsFocusActive;
                }

                IntPtr foregroundWindowHandle = GetForegroundWindow();
                isFocusMode = isFocusMode || isFullscreen(foregroundWindowHandle);
                
                
                if (!isFocusMode)
                {
                    EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, MonitorEnumCallBack, IntPtr.Zero);
                }
            }
        }

        protected void OnActivated(object sender, AppActivationArguments activationArguments)
        {
            var appNotification = new AppNotificationBuilder()
                .AddText("EyeGuard")
                .AddText("EyeGuard is already running in the background, and will show itself when its time to rest.")
                .BuildNotification();

            AppNotificationManager.Default.Show(appNotification);
        }

        private bool MonitorEnumCallBack(IntPtr hMonitor, IntPtr hdcMonitor, ref RectStruct lprcMonitor, IntPtr dwData)
        {
            Debug.WriteLine("hMonitor: " + hMonitor);
            Debug.WriteLine("hdcMonitor: " + hdcMonitor);
            Debug.WriteLine("lprcMonitor: " + lprcMonitor);
            Debug.WriteLine("dwData: " + dwData);
            MonitorInfo mon_info = new MonitorInfo();
            bool isSuccess = GetMonitorInfo(hMonitor, ref mon_info);
            if (!isSuccess)
            {
                Debug.WriteLine("GetMonitorInfo indicated failure");
            }

            int x = mon_info.WorkArea.Left;
            int y = mon_info.WorkArea.Top;

            Debug.WriteLine("(" + mon_info.WorkArea.Left + ", " + mon_info.WorkArea.Top + ")");

            DispatcherQueue.GetForCurrentThread().TryEnqueue(() =>
            {
                var window = new RestWindow();
                window.AppWindow.Move(new Windows.Graphics.PointInt32(x, y));
                window.AppWindow.SetPresenter(Microsoft.UI.Windowing.AppWindowPresenterKind.FullScreen);
                window.Activate();

                restWindows.Add(window);
                window.Skipping += (RestWindow window, object e) =>
                {
                    foreach (var restWindow in restWindows)
                    {
                        restWindow.Close();
                    }
                };
            });

            return true;
        }

        private Window secretWindow;
        private List<RestWindow> restWindows = new();

        bool isFullscreen(IntPtr windowHandle)
        {
            MonitorInfo monitorInfo = new MonitorInfo();
            GetMonitorInfo(MonitorFromWindow(windowHandle, MONITOR_DEFAULTTOPRIMARY), ref monitorInfo);

            RectStruct windowRect;
            GetWindowRect(windowHandle, out windowRect);

            return windowRect.Left == monitorInfo.Monitor.Left
                && windowRect.Right == monitorInfo.Monitor.Right
                && windowRect.Top == monitorInfo.Monitor.Top
                && windowRect.Bottom == monitorInfo.Monitor.Bottom;
        }

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool GetWindowRect(IntPtr hwnd, out RectStruct lpRect);


        const int MONITOR_DEFAULTTONULL = 0;
        const int MONITOR_DEFAULTTOPRIMARY = 1;
        const int MONITOR_DEFAULTTONEAREST = 2;

        [DllImport("user32.dll")]
        static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        /// <summary>
        ///     Retrieves a handle to the foreground window (the window with which the user is currently working). The system
        ///     assigns a slightly higher priority to the thread that creates the foreground window than it does to other threads.
        ///     <para>See https://msdn.microsoft.com/en-us/library/windows/desktop/ms633505%28v=vs.85%29.aspx for more information.</para>
        /// </summary>
        /// <returns>
        ///     C++ ( Type: Type: HWND )<br /> The return value is a handle to the foreground window. The foreground window
        ///     can be NULL in certain circumstances, such as when a window is losing activation.
        /// </returns>
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("shell32.dll")]
        static extern int SHQueryUserNotificationState(out int pquns);

        [DllImport("user32.dll")]
        internal static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

        internal delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref RectStruct lprcMonitor, IntPtr dwData);

        [DllImport("User32.dll")]
        static extern bool GetMonitorInfo(IntPtr hMonitor, [In, Out] ref MonitorInfo lpmi);

        /// <summary>
        /// The MONITORINFOEX structure contains information about a display monitor.
        /// The GetMonitorInfo function stores information into a MONITORINFOEX structure or a MONITORINFO structure.
        /// The MONITORINFOEX structure is a superset of the MONITORINFO structure. The MONITORINFOEX structure adds a string member to contain a name
        /// for the display monitor.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        internal struct MonitorInfo
        {
            /// <summary>
            /// The size, in bytes, of the structure. Set this member to sizeof(MONITORINFOEX) (72) before calling the GetMonitorInfo function.
            /// Doing so lets the function determine the type of structure you are passing to it.
            /// </summary>
            public int Size;

            /// <summary>
            /// A RECT structure that specifies the display monitor rectangle, expressed in virtual-screen coordinates.
            /// Note that if the monitor is not the primary display monitor, some of the rectangle's coordinates may be negative values.
            /// </summary>
            public RectStruct Monitor;

            /// <summary>
            /// A RECT structure that specifies the work area rectangle of the display monitor that can be used by applications,
            /// expressed in virtual-screen coordinates. Windows uses this rectangle to maximize an application on the monitor.
            /// The rest of the area in rcMonitor contains system windows such as the task bar and side bars.
            /// Note that if the monitor is not the primary display monitor, some of the rectangle's coordinates may be negative values.
            /// </summary>
            public RectStruct WorkArea;

            /// <summary>
            /// The attributes of the display monitor.
            ///
            /// This member can be the following value:
            ///   1 : MONITORINFOF_PRIMARY
            /// </summary>
            public uint Flags;

            public MonitorInfo()
            {
                Size = Marshal.SizeOf(typeof(MonitorInfo));
                Debug.WriteLine("MI size: " + Size);
                Monitor = new RectStruct();
                WorkArea = new RectStruct();
                Flags = 0;
            }
        }

        /// <summary>
        /// The RECT structure defines the coordinates of the upper-left and lower-right corners of a rectangle.
        /// </summary>
        /// <see cref="http://msdn.microsoft.com/en-us/library/dd162897%28VS.85%29.aspx"/>
        /// <remarks>
        /// By convention, the right and bottom edges of the rectangle are normally considered exclusive.
        /// In other words, the pixel whose coordinates are ( right, bottom ) lies immediately outside of the the rectangle.
        /// For example, when RECT is passed to the FillRect function, the rectangle is filled up to, but not including,
        /// the right column and bottom row of pixels. This structure is identical to the RECTL structure.
        /// </remarks>
        [StructLayout(LayoutKind.Sequential)]
        public struct RectStruct
        {
            /// <summary>
            /// The x-coordinate of the upper-left corner of the rectangle.
            /// </summary>
            public int Left;

            /// <summary>
            /// The y-coordinate of the upper-left corner of the rectangle.
            /// </summary>
            public int Top;

            /// <summary>
            /// The x-coordinate of the lower-right corner of the rectangle.
            /// </summary>
            public int Right;

            /// <summary>
            /// The y-coordinate of the lower-right corner of the rectangle.
            /// </summary>
            public int Bottom;
        }
    }
}
