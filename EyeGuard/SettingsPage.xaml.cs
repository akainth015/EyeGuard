using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Diagnostics;

namespace EyeGuard
{
    public sealed partial class SettingsPage : Page
    {
        private Windows.Media.Playback.MediaPlayer previewMediaPlayer;

        public SettingsPage()
        {
            InitializeComponent();
            LoadSettings();

            // Attach event handlers after loading settings to avoid triggering during initialization
            BreakIntervalSlider.ValueChanged += BreakIntervalSlider_ValueChanged;
            BreakIntervalSlider.ManipulationCompleted += BreakIntervalSlider_ManipulationCompleted;
            BreakDurationSlider.ValueChanged += BreakDurationSlider_ValueChanged;
            BreakDurationSlider.ManipulationCompleted += BreakDurationSlider_ManipulationCompleted;
        }

        private void LoadSettings()
        {
            var settingsService = SettingsService.Instance;
            BreakIntervalSlider.Value = settingsService.BreakInterval;
            BreakIntervalValueText.Text = settingsService.BreakInterval.ToString();

            BreakDurationSlider.Value = settingsService.BreakDuration;
            UpdateBreakDurationDisplay(settingsService.BreakDuration);

            BreakSoundPathTextBox.Text = settingsService.BreakSound;
            UpdateSoundDuration(settingsService.BreakSound);
        }

        private void BreakIntervalSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            // Only update the display text while dragging
            int value = (int)e.NewValue;
            BreakIntervalValueText.Text = value.ToString();
        }

        private void BreakIntervalSlider_ManipulationCompleted(object sender, Microsoft.UI.Xaml.Input.ManipulationCompletedRoutedEventArgs e)
        {
            // Save the setting when the user releases the slider
            int value = (int)BreakIntervalSlider.Value;
            Debug.WriteLine("Break interval was updated to " + value + " minutes");

            var settingsService = SettingsService.Instance;
            settingsService.BreakInterval = value;
        }

        private void BreakDurationSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            // Only update the display while dragging
            int value = (int)e.NewValue;
            UpdateBreakDurationDisplay(value);
        }

        private void BreakDurationSlider_ManipulationCompleted(object sender, Microsoft.UI.Xaml.Input.ManipulationCompletedRoutedEventArgs e)
        {
            // Save the setting when the user releases the slider
            int value = (int)BreakDurationSlider.Value;
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

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            // Navigate back to home page
            Frame.Navigate(typeof(HomePage));
        }

        private async void BrowseSoundButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.FileTypeFilter.Add(".wav");
            picker.FileTypeFilter.Add(".mp3");
            picker.FileTypeFilter.Add(".m4a");
            picker.FileTypeFilter.Add(".wma");

            // Initialize the picker with the window handle
            var app = Application.Current as App;
            if (app != null && app.SecretWindow != null)
            {
                IntPtr windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(app.SecretWindow);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, windowHandle);
            }

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                var settingsService = SettingsService.Instance;
                settingsService.BreakSound = file.Path;
                BreakSoundPathTextBox.Text = file.Path;
                UpdateSoundDuration(file.Path);
            }
        }

        private void ResetSoundButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsService = SettingsService.Instance;
            settingsService.BreakSound = SettingsService.DEFAULT_BREAK_SOUND;
            BreakSoundPathTextBox.Text = settingsService.BreakSound;
            UpdateSoundDuration(settingsService.BreakSound);
        }

        private async void UpdateSoundDuration(string soundPath)
        {
            try
            {
                var tempPlayer = new Windows.Media.Playback.MediaPlayer()
                {
                    Source = Windows.Media.Core.MediaSource.CreateFromUri(new Uri(soundPath))
                };

                // Wait for the media to open to get duration
                var tcs = new System.Threading.Tasks.TaskCompletionSource<bool>();
                tempPlayer.MediaOpened += (s, e) => tcs.TrySetResult(true);
                tempPlayer.MediaFailed += (s, e) => tcs.TrySetResult(false);

                await tcs.Task;

                var duration = tempPlayer.PlaybackSession.NaturalDuration;
                int durationSeconds = (int)Math.Ceiling(duration.TotalSeconds);

                SoundDurationText.Text = $"Duration: {durationSeconds} second{(durationSeconds == 1 ? "" : "s")}";

                // Show warning if longer than 2 seconds
                SoundDurationWarning.Visibility = durationSeconds > 2 ? Visibility.Visible : Visibility.Collapsed;
                tempPlayer.Dispose();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting sound duration: {ex.Message}");
                SoundDurationText.Text = "Duration: Unknown";
                SoundDurationWarning.Visibility = Visibility.Collapsed;
            }
        }

        private void PlaySoundButton_Click(object sender, RoutedEventArgs e)
        {
            // If currently playing, stop it
            if (previewMediaPlayer != null && previewMediaPlayer.PlaybackSession.PlaybackState == Windows.Media.Playback.MediaPlaybackState.Playing)
            {
                previewMediaPlayer.Pause();
                previewMediaPlayer.Dispose();
                previewMediaPlayer = null;
                PlaySoundButton.Content = "Play";
                return;
            }

            // Start playing
            var settingsService = SettingsService.Instance;
            string soundPath = settingsService.BreakSound;

            try
            {
                previewMediaPlayer = new Windows.Media.Playback.MediaPlayer()
                {
                    Source = Windows.Media.Core.MediaSource.CreateFromUri(new Uri(soundPath))
                };

                // Reset button when audio finishes
                previewMediaPlayer.MediaEnded += (s, args) =>
                {
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        PlaySoundButton.Content = "Play";
                        previewMediaPlayer?.Dispose();
                        previewMediaPlayer = null;
                    });
                };

                previewMediaPlayer.Play();
                PlaySoundButton.Content = "Stop";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error playing sound: {ex.Message}");
                PlaySoundButton.Content = "Play";
            }
        }
    }
}
