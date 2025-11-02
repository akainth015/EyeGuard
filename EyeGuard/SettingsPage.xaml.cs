using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace EyeGuard
{
    public sealed partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            InitializeComponent();
            LoadSettings();
        }

        private void LoadSettings()
        {
            var settingsService = SettingsService.Instance;
            BreakIntervalSlider.Value = settingsService.BreakInterval;
            BreakIntervalValueText.Text = settingsService.BreakInterval.ToString();

            BreakDurationSlider.Value = settingsService.BreakDuration;
            UpdateBreakDurationDisplay(settingsService.BreakDuration);
        }

        private void BreakIntervalSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (BreakIntervalValueText == null) return; // During initialization

            int value = (int)e.NewValue;
            BreakIntervalValueText.Text = value.ToString();

            var settingsService = SettingsService.Instance;
            settingsService.BreakInterval = value;
        }

        private void BreakDurationSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (BreakDurationValueText == null) return; // During initialization

            int value = (int)e.NewValue;
            UpdateBreakDurationDisplay(value);

            var settingsService = SettingsService.Instance;
            settingsService.BreakDuration = value;
        }

        private void UpdateBreakDurationDisplay(int seconds)
        {
            if (seconds >= 60)
            {
                // Display in minutes if 60 seconds or more
                int minutes = seconds / 60;
                int remainingSeconds = seconds % 60;

                if (remainingSeconds == 0)
                {
                    BreakDurationValueText.Text = minutes.ToString();
                    BreakDurationUnitText.Text = minutes == 1 ? "minute" : "minutes";
                }
                else
                {
                    BreakDurationValueText.Text = $"{minutes}:{remainingSeconds:D2}";
                    BreakDurationUnitText.Text = "min";
                }
            }
            else
            {
                // Display in seconds
                BreakDurationValueText.Text = seconds.ToString();
                BreakDurationUnitText.Text = seconds == 1 ? "second" : "seconds";
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            // Navigate back to home page
            Frame.Navigate(typeof(HomePage));
        }
    }
}
