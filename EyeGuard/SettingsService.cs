using Microsoft.Windows.Storage;
using System;

namespace EyeGuard
{
    /// <summary>
    /// Service for managing application settings using ApplicationData.LocalSettings
    /// </summary>
    public class SettingsService
    {
        private const string BREAK_INTERVAL_KEY = "BreakInterval";
        private const int DEFAULT_BREAK_INTERVAL = 20; // minutes
        private const int MIN_BREAK_INTERVAL = 10; // minutes
        private const int MAX_BREAK_INTERVAL = 30; // minutes

        private const string BREAK_DURATION_KEY = "BreakDuration";
        private const int DEFAULT_BREAK_DURATION = 20; // seconds
        private const int MIN_BREAK_DURATION = 5; // seconds
        private const int MAX_BREAK_DURATION = 300; // seconds (5 minutes)

        private static SettingsService _instance;
        public static SettingsService Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new SettingsService();
                }
                return _instance;
            }
        }

        public event EventHandler<int> BreakIntervalChanged;
        public event EventHandler<int> BreakDurationChanged;

        private SettingsService()
        {
            // Initialize settings if they don't exist
            if (!ApplicationData.GetDefault().LocalSettings.Values.ContainsKey(BREAK_INTERVAL_KEY))
            {
                ApplicationData.GetDefault().LocalSettings.Values[BREAK_INTERVAL_KEY] = DEFAULT_BREAK_INTERVAL;
            }
            if (!ApplicationData.GetDefault().LocalSettings.Values.ContainsKey(BREAK_DURATION_KEY))
            {
                ApplicationData.GetDefault().LocalSettings.Values[BREAK_DURATION_KEY] = DEFAULT_BREAK_DURATION;
            }
        }

        /// <summary>
        /// Gets or sets the break interval in minutes (between 10 and 30)
        /// </summary>
        public int BreakInterval
        {
            get
            {
                if (ApplicationData.GetDefault().LocalSettings.Values.TryGetValue(BREAK_INTERVAL_KEY, out var value))
                {
                    if (value is int interval)
                    {
                        // Ensure the value is within valid range
                        return Math.Clamp(interval, MIN_BREAK_INTERVAL, MAX_BREAK_INTERVAL);
                    }
                }
                return DEFAULT_BREAK_INTERVAL;
            }
            set
            {
                // Clamp the value to the valid range
                int clampedValue = Math.Clamp(value, MIN_BREAK_INTERVAL, MAX_BREAK_INTERVAL);
                ApplicationData.GetDefault().LocalSettings.Values[BREAK_INTERVAL_KEY] = clampedValue;
                BreakIntervalChanged?.Invoke(this, clampedValue);
            }
        }

        public int MinBreakInterval => MIN_BREAK_INTERVAL;
        public int MaxBreakInterval => MAX_BREAK_INTERVAL;

        /// <summary>
        /// Gets or sets the break duration in seconds (between 5 and 300)
        /// </summary>
        public int BreakDuration
        {
            get
            {
                if (ApplicationData.GetDefault().LocalSettings.Values.TryGetValue(BREAK_DURATION_KEY, out var value))
                {
                    if (value is int duration)
                    {
                        // Ensure the value is within valid range
                        return Math.Clamp(duration, MIN_BREAK_DURATION, MAX_BREAK_DURATION);
                    }
                }
                return DEFAULT_BREAK_DURATION;
            }
            set
            {
                // Clamp the value to the valid range
                int clampedValue = Math.Clamp(value, MIN_BREAK_DURATION, MAX_BREAK_DURATION);
                ApplicationData.GetDefault().LocalSettings.Values[BREAK_DURATION_KEY] = clampedValue;
                BreakDurationChanged?.Invoke(this, clampedValue);
            }
        }

        public int MinBreakDuration => MIN_BREAK_DURATION;
        public int MaxBreakDuration => MAX_BREAK_DURATION;
    }
}
