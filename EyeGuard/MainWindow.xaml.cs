using Microsoft.UI.Xaml;

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
            AppWindow.SetIcon("Assets/EyeGuard.ico");
        }

        // TODO: Keep the countdown timer in the main window in sync with the app
    }
}
