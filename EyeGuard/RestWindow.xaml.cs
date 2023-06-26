using Microsoft.UI.Xaml;
using System;
using System.Diagnostics.Tracing;
using System.Runtime.InteropServices;
using Windows.Foundation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace EyeGuard
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class RestWindow : Window
    {
        public TypedEventHandler<RestWindow, object> Skipping = (RestWindow sender, object e) => { };

        public RestWindow()
        {
            InitializeComponent();
            AppWindow.SetPresenter(Microsoft.UI.Windowing.AppWindowPresenterKind.FullScreen);

            timer = new DispatcherTimer();
            timer.Tick += ReduceTimeRemaining;
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Start();
        }

        public void ForceFocus()
        {
            AppWindow.Hide();
            AppWindow.Show();
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            SetForegroundWindow(hWnd);
        }

        private void ReduceTimeRemaining(object sender, object e)
        {
            secondsRemaining -= 1;
            if (secondsRemaining <= 0)
            {
                timer.Stop();
                Close();
            }
            else
            {
                CountdownTextBlock.Text = secondsRemaining.ToString();
            }
        }

        private void SkipBreak(object sender, RoutedEventArgs e)
        {
            Skipping(this, null);
        }

        private int secondsRemaining = 20;
        private DispatcherTimer timer;

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
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetForegroundWindow(IntPtr hWnd);
    }
}
