using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using System;

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

            // Subscribe to pause status changes
            SettingsService.Instance.PauseUntilChanged += OnPauseStatusChanged;
        }

        private void OnCountdownChanged(object sender, int minutesLeft)
        {
            // Update UI on the UI thread
            DispatcherQueue.TryEnqueue(() =>
            {
                UpdateCountdownText(minutesLeft);
            });
        }

        private void OnPauseStatusChanged(object sender, DateTime? pauseUntil)
        {
            // Update UI on the UI thread
            DispatcherQueue.TryEnqueue(() =>
            {
                var app = Application.Current as App;
                if (app != null)
                {
                    UpdateCountdownText(app.MinutesBeforeNextBreak);
                }
            });
        }

        private void UpdateCountdownText(int minutes)
        {
            CountdownTextBlock.Inlines.Clear();

            // Check if breaks are paused
            var pauseUntil = SettingsService.Instance.PauseUntil;
            if (pauseUntil.HasValue)
            {
                var localTime = pauseUntil.Value.ToLocalTime();
                CountdownTextBlock.Inlines.Add(new Run { Text = "Breaks are paused until " });
                CountdownTextBlock.Inlines.Add(new Bold { Inlines = { new Run { Text = localTime.ToString("g") } } });
                CountdownTextBlock.Inlines.Add(new Run { Text = "." });
            }
            else
            {
                CountdownTextBlock.Inlines.Add(new Run { Text = "Your next break is in " });
                CountdownTextBlock.Inlines.Add(new Bold { Inlines = { new Run { Text = minutes.ToString() } } });
                CountdownTextBlock.Inlines.Add(new Run { Text = minutes == 1 ? " minute." : " minutes." });
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(SettingsPage));
        }
    }
}
