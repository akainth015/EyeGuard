using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Diagnostics;

namespace EyeGuard
{
    public sealed partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            InitializeComponent();
            LoadSettings();

            // Attach event handlers after loading settings to avoid triggering during initialization
            BreakIntervalSlider.ValueChanged += BreakIntervalSlider_ValueChanged;
            BreakDurationSlider.ValueChanged += BreakDurationSlider_ValueChanged;
        }

        private void LoadSettings()
        {
            var settingsService = SettingsService.Instance;
            BreakIntervalSlider.Value = settingsService.BreakInterval;
            BreakIntervalValueText.Text = settingsService.BreakInterval.ToString();

            BreakDurationSlider.Value = settingsService.BreakDuration;
            UpdateBreakDurationDisplay(settingsService.BreakDuration);

            LoadPauseSettings();
        }

        private void LoadPauseSettings()
        {
            var settingsService = SettingsService.Instance;
            var pauseUntil = settingsService.PauseUntil;

            if (pauseUntil.HasValue)
            {
                // Convert UTC to local time for display
                var localTime = pauseUntil.Value.ToLocalTime();
                PauseDatePicker.Date = new DateTimeOffset(localTime.Date);
                PauseTimePicker.Time = localTime.TimeOfDay;
            }
            else
            {
                // Set to current time + 1 hour as default
                var defaultTime = DateTime.Now.AddHours(1);
                PauseDatePicker.Date = new DateTimeOffset(defaultTime.Date);
                PauseTimePicker.Time = defaultTime.TimeOfDay;
            }

            UpdatePauseStatus();
        }

        private void BreakIntervalSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            int value = (int)e.NewValue;
            BreakIntervalValueText.Text = value.ToString();
            Debug.WriteLine("Break interval was updated to " + value + " minutes");

            var settingsService = SettingsService.Instance;
            settingsService.BreakInterval = value;
        }

        private void BreakDurationSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            int value = (int)e.NewValue;
            UpdateBreakDurationDisplay(value);
            Debug.WriteLine("Break duration was updated to " + value + " seconds");

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

        private void PauseDatePicker_DateChanged(object sender, DatePickerValueChangedEventArgs args)
        {
            UpdatePauseStatus();
        }

        private void PauseTimePicker_TimeChanged(object sender, TimePickerValueChangedEventArgs e)
        {
            UpdatePauseStatus();
        }

        private void SetPauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (PauseDatePicker.Date == null)
            {
                PauseStatusText.Text = "Please select a date.";
                return;
            }

            // Combine date and time, treating as local time
            var localDateTime = PauseDatePicker.Date + PauseTimePicker.Time;
            var localDateTimeObj = localDateTime.DateTime;

            // Check if the selected time is in the future
            if (localDateTimeObj <= DateTime.Now)
            {
                PauseStatusText.Text = "Please select a time in the future.";
                return;
            }

            // Convert to UTC and store
            var utcDateTime = localDateTimeObj.ToUniversalTime();
            SettingsService.Instance.PauseUntil = utcDateTime;

            UpdatePauseStatus();
        }

        private void ClearPauseButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsService.Instance.PauseUntil = null;
            UpdatePauseStatus();
        }

        private void UpdatePauseStatus()
        {
            var settingsService = SettingsService.Instance;
            var pauseUntil = settingsService.PauseUntil;

            if (pauseUntil.HasValue)
            {
                var localTime = pauseUntil.Value.ToLocalTime();
                PauseStatusText.Text = $"Breaks are paused until {localTime:g}";
                SetPauseButton.IsEnabled = true;
                ClearPauseButton.IsEnabled = true;
            }
            else
            {
                PauseStatusText.Text = "Breaks are active.";
                SetPauseButton.IsEnabled = true;
                ClearPauseButton.IsEnabled = false;
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            // Navigate back to home page
            Frame.Navigate(typeof(HomePage));
        }
    }
}
