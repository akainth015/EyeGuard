using System;
using System.Collections.Generic;
using static EyeGuard.PInvokeUtils;
using Microsoft.UI.Dispatching;
using System.Timers;
using Windows.Media.Core;
using Windows.Media.Playback;

namespace EyeGuard
{
    internal class Break
    {
        private List<RestWindow> restWindows;
        private int secondsInBreak;
        private bool useMinutesFormat;
        private Timer countdownTimer;
        private MediaPlayer mediaPlayer = new MediaPlayer()
        {
            Source = MediaSource.CreateFromUri(new Uri("ms-appx:///Assets/timer-done.wav"))
        };
        private DispatcherQueue dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        internal Break()
        {
            restWindows = new List<RestWindow>();

            // Get break duration from settings
            secondsInBreak = SettingsService.Instance.BreakDuration;

            // Determine format based on initial duration
            useMinutesFormat = secondsInBreak >= 60;

            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, OpenRestWindowOnMonitor, IntPtr.Zero);

            countdownTimer = new Timer();
            countdownTimer.Interval = 1000;
            countdownTimer.Elapsed += Tick;
            countdownTimer.Start();

            mediaPlayer.Play();
        }

        private void Tick(object sender, EventArgs e)
        {
            secondsInBreak--;

            if (secondsInBreak == 0)
            {
                mediaPlayer.Play();
                EndBreak();
                return;
            }

            foreach (var window in restWindows)
            {
                dispatcherQueue.TryEnqueue(() =>
                {
                    window.CountdownTextBlock.Text = FormatTimeRemaining(secondsInBreak);
                });
            }
        }

        private string FormatTimeRemaining(int totalSeconds)
        {
            if (useMinutesFormat)
            {
                // Break started with 60+ seconds: always show minutes:seconds format
                int minutes = totalSeconds / 60;
                int seconds = totalSeconds % 60;
                return $"{minutes}:{seconds:D2}";
            }
            else
            {
                // Break started with less than 60 seconds: show seconds only
                return totalSeconds.ToString();
            }
        }

        private void SkipBreak(RestWindow _, object e)
        {
            EndBreak();
        }

        private void EndBreak()
        {
            countdownTimer.Stop();
            foreach (var window in restWindows)
            {
                dispatcherQueue.TryEnqueue(window.Close);
            }
            restWindows.Clear();
        }

        private bool OpenRestWindowOnMonitor(IntPtr hMonitor, IntPtr hdcMonitor, ref RectStruct lprcMonitor, IntPtr dwData)
        {
            dispatcherQueue.TryEnqueue(() =>
            {
                var window = new RestWindow();
                window.CountdownTextBlock.Text = FormatTimeRemaining(secondsInBreak);
                window.FullscreenOnMonitor(hMonitor);

                restWindows.Add(window);
                window.SkipBreakPressed += SkipBreak;
            });

            return true;
        }
    }
}
