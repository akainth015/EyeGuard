using Microsoft.UI.Xaml;
using System;

namespace EyeGuard
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Title = "EyeGuard";
            AppWindow.SetIcon("Assets/EyeGuard.ico");

            // Make window borderless
            AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
            // Using a 48 px tall title bar because a back button will be present
            AppWindow.TitleBar.PreferredHeightOption = Microsoft.UI.Windowing.TitleBarHeightOption.Tall;

            // Navigate to home page by default
            ContentFrame.Navigate(typeof(HomePage));
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
