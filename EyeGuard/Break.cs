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
        private int secondsInBreak = 20;
        private Timer countdownTimer;
        private MediaPlayer mediaPlayer = new MediaPlayer()
        {
            Source = MediaSource.CreateFromUri(new Uri("ms-appx:///Assets/timer-done.wav"))
        };
        private DispatcherQueue dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        internal Break()
        {
            restWindows = new List<RestWindow>();

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
                    window.CountdownTextBlock.Text = secondsInBreak.ToString();
                });
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
                window.FullscreenOnMonitor(hMonitor);

                restWindows.Add(window);
                window.SkipBreakPressed += SkipBreak;
            });

            return true;
        }
    }
}
