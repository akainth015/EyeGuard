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

            AppInstance.GetCurrent().Activated += NotifyAlreadyRunningInBackground;

            var clArgs = Environment.GetCommandLineArgs();
            if (activatedEventArgs.Kind == ExtendedActivationKind.Launch && clArgs.Length == 1)
            {
                NotifyStartingInBackground();
            }

            mainWindow = new MainWindow();

            while (true)
            {
                await Task.Delay(TimeSpan.FromMinutes(1));
                minutesBeforeNextBreak -= 1;

                // If the user is not at their computer, the next break should be 20 minutes after they
                // return and are using the computer for 20 minutes straight.
                PInvokeUtils.SHQueryUserNotificationState(out var notificationState);
                if (notificationState == 1)
                {
                    minutesBeforeNextBreak = BREAK_INTERVAL; // FOR TESTING
                    Debug.WriteLine("Setting the next break to " + BREAK_INTERVAL + " minutes from now because the user is away");
                    continue;
                }

                if (minutesBeforeNextBreak == 0)
                {
                    minutesBeforeNextBreak = BREAK_INTERVAL;

                    // Is it a focus session? Skip this break.
                    if (FocusSessionManager.IsSupported && FocusSessionManager.GetDefault().IsFocusActive)
                    {
                        Debug.WriteLine("Skipping break because of a focus session");
                        // the break should trigger immediately after the user is done with their focus session
                        minutesBeforeNextBreak = 1;
                        continue;
                    }

                    // Is the user doing a presentation, or playing a game? Skip this break.
                    if (notificationState != 5)
                    {
                        Debug.WriteLine("Skipping break because user is not accepting notifications");
                        // the break should trigger immediately after the user is done with their activity
                        minutesBeforeNextBreak = 1;
                        continue;
                    }

                    // Is the window the user is using full-screened? Skip this break.
                    IntPtr foregroundWindow = PInvokeUtils.GetForegroundWindow();
                    if (foregroundWindow != IntPtr.Zero && PInvokeUtils.isFullscreen(foregroundWindow))
                    {
                        Debug.WriteLine("Skipping break because the users's application is in full-screen");
                        // the break should trigger immediately after the user exits full screen
                        minutesBeforeNextBreak = 1;
                        continue;
                    }

                    breakInstance = new Break();
                    await Task.Delay(TimeSpan.FromSeconds(21));
                    AppInstance.Restart("suppressNotification");
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

        private MainWindow mainWindow;
        private int minutesBeforeNextBreak = BREAK_INTERVAL;
    }
}
