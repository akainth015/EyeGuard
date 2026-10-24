using Microsoft.UI.Xaml;
using System;
using System.Diagnostics;
using Windows.Storage;

namespace EyeGuard
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private const string WindowIconUri = "ms-appx:///Assets/EyeGuard.ico";

        public MainWindow()
        {
            InitializeComponent();
            Title = "EyeGuard";
            SetWindowIcon();

            // Make window borderless
            AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
            // Using a 48 px tall title bar because a back button will be present
            AppWindow.TitleBar.PreferredHeightOption = Microsoft.UI.Windowing.TitleBarHeightOption.Tall;

            // Navigate to home page by default
            ContentFrame.Navigate(typeof(HomePage));
        }

        /// <summary>
        /// Sets the window icon. AppWindow.SetIcon only accepts a fully qualified path, so the
        /// packaged asset is resolved through ms-appx (the same scheme the break sound uses)
        /// instead of passing a working-directory-relative path.
        /// </summary>
        private async void SetWindowIcon()
        {
            try
            {
                StorageFile iconFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri(WindowIconUri));
                AppWindow.SetIcon(iconFile.Path);
            }
            catch (Exception ex)
            {
                // A missing or unreadable icon must not take the window down; the default stays.
                Debug.WriteLine("Could not set the window icon: " + ex.Message);
            }
        }

        public void ForceFocus()
        {
            // Get window handle
            IntPtr windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(this);
            RestWindow.ShowWindow(windowHandle, 6);
            RestWindow.SetForegroundWindow(windowHandle);
            RestWindow.ShowWindow(windowHandle, 1);
        }

        public void NavigateToHome()
        {
            ContentFrame.Navigate(typeof(HomePage));
        }
    }
}
