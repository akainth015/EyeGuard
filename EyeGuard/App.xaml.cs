using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.UI.Shell;

namespace EyeGuard
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        public const int BREAK_INTERVAL = 20; // this is in minutes

        private Break breakInstance;

        // Event to notify when the countdown changes
        public event EventHandler<int> CountdownChanged;

        // Property to expose the current countdown value
        public int MinutesBeforeNextBreak => minutesBeforeNextBreak;

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
            var mainInstance = AppInstance.FindOrRegisterForKey("MainInstance");
            var activatedEventArgs = AppInstance.GetCurrent().GetActivatedEventArgs();

            if (!mainInstance.IsCurrent)
            {
                mainInstance.RedirectActivationToAsync(activatedEventArgs).AsTask().Wait();
                Process.GetCurrentProcess().Kill();
                return;
            }

            AppInstance.GetCurrent().Activated += ShowMainWindow;

            // If "suppressNotifications" is a command-line argument, then a notification that EyeGuard
            // is starting should be skipped. Reminder that the first command-line argument is the
            // path to the EyeGuard executable (hence the length check is 1 instead of 0)
            var clArgs = Environment.GetCommandLineArgs();
            if (activatedEventArgs.Kind == ExtendedActivationKind.Launch && clArgs.Length == 1)
            {
                NotifyStartingInBackground();
            }

            secretWindow = new MainWindow();

            while (true)
            {
                await Task.Delay(TimeSpan.FromMinutes(1));
                UpdateMinutesBeforeNextBreak(minutesBeforeNextBreak - 1);

                // If the user is not at their computer, the next break should be 20 minutes after they
                // return and are using the computer for 20 minutes straight.
                PInvokeUtils.SHQueryUserNotificationState(out var notificationState);
                if (notificationState == 1)
                {
                    UpdateMinutesBeforeNextBreak(BREAK_INTERVAL);
                    Debug.WriteLine("Setting the next break to " + BREAK_INTERVAL + " minutes from now because the user is away");
                    continue;
                }

                if (minutesBeforeNextBreak == 0)
                {
                    UpdateMinutesBeforeNextBreak(BREAK_INTERVAL);

                    // Is it a focus session? Skip this break.
                    if (FocusSessionManager.IsSupported && FocusSessionManager.GetDefault().IsFocusActive)
                    {
                        Debug.WriteLine("Skipping break because of a focus session");
                        // the break should trigger immediately after the user is done with their focus session
                        UpdateMinutesBeforeNextBreak(1);
                        continue;
                    }

                    // Is the user doing a presentation, or playing a game? Skip this break.
                    if (notificationState != 5)
                    {
                        Debug.WriteLine("Skipping break because user is not accepting notifications");
                        // the break should trigger immediately after the user is done with their activity
                        UpdateMinutesBeforeNextBreak(1);
                        continue;
                    }

                    // Is the window the user is using full-screened? Skip this break.
                    IntPtr foregroundWindow = PInvokeUtils.GetForegroundWindow();
                    if (foregroundWindow != IntPtr.Zero && PInvokeUtils.isFullscreen(foregroundWindow))
                    {
                        Debug.WriteLine("Skipping break because the users's application is in full-screen");
                        // the break should trigger immediately after the user exits full screen
                        UpdateMinutesBeforeNextBreak(1);
                        continue;
                    }

                    breakInstance = new Break();
                    await Task.Delay(TimeSpan.FromSeconds(21));

                    // https://github.com/microsoft/microsoft-ui-xaml/issues/7282#issuecomment-1717060648
                    // The code below can be safely deleted once resolved.
                    if (mainWindow == null)
                    {
                        Debug.WriteLine("Restarting EyeGuard to mitigate a memory leak in the WinUI framework");
                        AppInstance.Restart("suppressNotification");
                    } else
                    {
                        Debug.WriteLine("Skipping app restart because the main window is open");
                    }
                }
            }
        }

        private static void NotifyStartingInBackground()
        {
            var notification = new AppNotificationBuilder()
                .AddText("Successfully started")
                .AddText("EyeGuard is running in the background, and will open a window when it's time for a break.")
                .BuildNotification();

            AppNotificationManager.Default.Show(notification);
        }

        private void ShowMainWindow(object sender, AppActivationArguments e)
        {
            secretWindow.DispatcherQueue.TryEnqueue(() =>
            {
                if (mainWindow == null)
                {
                    mainWindow = new MainWindow();
                    mainWindow.Closed += ClearMainWindow;
                }
                mainWindow.Activate();
            });
        }

        private void ClearMainWindow(object sender, WindowEventArgs args)
        {
            // Create a new main window the next time EyeGuard is launched
            mainWindow = null;
        }

        private void NotifyAlreadyRunningInBackground(object sender, AppActivationArguments e)
        {
            string minutesLeftText;
            if (minutesBeforeNextBreak == 1)
            {
                minutesLeftText = "Your next break will be in 1 minute.";
            }
            else
            {
                minutesLeftText = "Your next break will be in " + minutesBeforeNextBreak + " minutes.";
            }
            var notification = new AppNotificationBuilder()
                    .AddText("Already running")
                    .AddText(minutesLeftText)
                    .BuildNotification();
            
            AppNotificationManager.Default.Show(notification);
        }

        private void UpdateMinutesBeforeNextBreak(int value)
        {
            minutesBeforeNextBreak = value;
            CountdownChanged?.Invoke(this, minutesBeforeNextBreak);
        }

        private MainWindow mainWindow;
        private MainWindow secretWindow;
        private int minutesBeforeNextBreak = BREAK_INTERVAL;
    }
}
