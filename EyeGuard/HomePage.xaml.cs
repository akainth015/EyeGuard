using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using System;
using System.Threading.Tasks;

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

            // Load pause settings
            LoadPauseSettings();

            // Check and display add-on promotion after page is loaded
            this.Loaded += HomePage_Loaded;
        }

        private void HomePage_Loaded(object sender, RoutedEventArgs e)
        {
            // Defer the license check to avoid reentrancy issues
            DispatcherQueue.TryEnqueue(async () =>
            {
                await CheckAndShowAddonPromotion();
            });
        }

        private async Task CheckAndShowAddonPromotion()
        {
            bool hasPurchased;

            try
            {
                hasPurchased = await StoreUtils.Instance.IsSettingsAddonPurchasedAsync();
            }
            catch (Exception ex)
            {
                // Log the error
                System.Diagnostics.Debug.WriteLine($"Error checking add-on purchase status in HomePage: {ex.Message}");

                // Fall back to cached status from Settings
                var cachedStatus = SettingsService.Instance.CachedSettingsAddonPurchased;
                hasPurchased = cachedStatus ?? false; // Default to false if never verified
            }

            if (hasPurchased)
            {
                // User has the add-on - hide promotion, show controls
                SettingsAddonPromotion.Visibility = Visibility.Collapsed;
                SettingsButton.Visibility = Visibility.Visible;
                // Pause controls visibility is handled by UpdatePauseStatus()
            }
            else
            {
                // User doesn't have the add-on - show promotion, hide controls
                SettingsAddonPromotion.Visibility = Visibility.Visible;
                SettingsButton.Visibility = Visibility.Collapsed;

                // Hide all pause controls
                NotPausedControls.Visibility = Visibility.Collapsed;
                PausedControls.Visibility = Visibility.Collapsed;
                DateTimePickerControls.Visibility = Visibility.Collapsed;
            }
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
            if (SettingsService.Instance.IsPaused)
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

        private void SetPauseButton_Click(object sender, RoutedEventArgs e)
        {
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

            // Return to paused state view
            UpdatePauseStatus();
        }

        private void UpdatePauseStatus()
        {
            var settingsService = SettingsService.Instance;

            if (settingsService.IsPaused)
            {
                // Breaks are paused - show paused controls
                NotPausedControls.Visibility = Visibility.Collapsed;
                PausedControls.Visibility = Visibility.Visible;
                DateTimePickerControls.Visibility = Visibility.Collapsed;
            }
            else
            {
                // Breaks are not paused - show "Pause Breaks" button
                NotPausedControls.Visibility = Visibility.Visible;
                PausedControls.Visibility = Visibility.Collapsed;
                DateTimePickerControls.Visibility = Visibility.Collapsed;
            }
        }

        private void QuickPause30Minutes_Click(object sender, RoutedEventArgs e)
        {
            SetPauseForDuration(TimeSpan.FromMinutes(30));
        }

        private void QuickPause1Hour_Click(object sender, RoutedEventArgs e)
        {
            SetPauseForDuration(TimeSpan.FromHours(1));
        }

        private void QuickPause8Hours_Click(object sender, RoutedEventArgs e)
        {
            SetPauseForDuration(TimeSpan.FromHours(8));
        }

        private void QuickPauseCustom_Click(object sender, RoutedEventArgs e)
        {
            // Set default time to current time + 1 hour
            var defaultTime = DateTime.Now.AddHours(1);
            PauseDatePicker.Date = new DateTimeOffset(defaultTime.Date);
            PauseTimePicker.Time = defaultTime.TimeOfDay;
            PauseStatusText.Text = "";

            // Show date/time pickers to set pause time
            NotPausedControls.Visibility = Visibility.Collapsed;
            DateTimePickerControls.Visibility = Visibility.Visible;
        }

        private void SetPauseForDuration(TimeSpan duration)
        {
            // Calculate pause end time
            var pauseUntil = DateTime.Now.Add(duration).ToUniversalTime();
            SettingsService.Instance.PauseUntil = pauseUntil;

            // Update to paused state
            UpdatePauseStatus();
        }

        private void ResumeBreaksButton_Click(object sender, RoutedEventArgs e)
        {
            // Clear the pause and return to not paused state
            SettingsService.Instance.PauseUntil = null;
            UpdatePauseStatus();
        }

        private void ChangeTimeButton_Click(object sender, RoutedEventArgs e)
        {
            // Load current pause time into pickers
            var pauseUntil = SettingsService.Instance.PauseUntil;
            if (pauseUntil.HasValue)
            {
                var localTime = pauseUntil.Value.ToLocalTime();
                PauseDatePicker.Date = new DateTimeOffset(localTime.Date);
                PauseTimePicker.Time = localTime.TimeOfDay;
            }
            PauseStatusText.Text = "";

            // Show date/time pickers to change the pause time
            PausedControls.Visibility = Visibility.Collapsed;
            DateTimePickerControls.Visibility = Visibility.Visible;
        }

        private void CancelPauseButton_Click(object sender, RoutedEventArgs e)
        {
            // Return to the appropriate state without changing settings
            UpdatePauseStatus();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(SettingsPage));
        }

        private async void PurchaseAddonButton_Click(object sender, RoutedEventArgs e)
        {
            // Attempt to purchase the add-on
            bool purchaseSuccessful = await StoreUtils.Instance.PurchaseSettingsAddonAsync();

            // If purchase was successful, refresh the UI
            if (purchaseSuccessful)
            {
                // Hide promotion, show settings button and pause controls
                SettingsAddonPromotion.Visibility = Visibility.Collapsed;
                SettingsButton.Visibility = Visibility.Visible;
                UpdatePauseStatus(); // This will show the appropriate pause controls
            }
        }
    }
}
