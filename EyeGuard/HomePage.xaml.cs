using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;

namespace EyeGuard
{
    public sealed partial class HomePage : Page
    {
        public HomePage()
        {
            InitializeComponent();

            // Subscribe to countdown changes from the App
            var app = Application.Current as App;
            if (app != null)
            {
                app.CountdownChanged += OnCountdownChanged;
                // Initialize with actual current value
                UpdateCountdownText(app.MinutesBeforeNextBreak);
            }
        }

        private void OnCountdownChanged(object sender, int minutesLeft)
        {
            // Update UI on the UI thread
            DispatcherQueue.TryEnqueue(() =>
            {
                UpdateCountdownText(minutesLeft);
            });
        }

        private void UpdateCountdownText(int minutes)
        {
            CountdownTextBlock.Inlines.Clear();
            CountdownTextBlock.Inlines.Add(new Run { Text = "Your next break is in " });
            CountdownTextBlock.Inlines.Add(new Bold { Inlines = { new Run { Text = minutes.ToString() } } });
            CountdownTextBlock.Inlines.Add(new Run { Text = minutes == 1 ? " minute." : " minutes." });
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(SettingsPage));
        }
    }
}
