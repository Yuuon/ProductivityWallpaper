using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ProductivityWallpaper.Services
{
    /// <summary>
    /// Represents a single navigation log entry.
    /// </summary>
    public class NavigationLogEntry
    {
        /// <summary>
        /// Gets or sets the timestamp of the navigation attempt.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the name of the feature being navigated to.
        /// </summary>
        public string FeatureName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the type name of the ViewModel created (if successful).
        /// </summary>
        public string ViewModelType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the navigation was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the error message if navigation failed.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the stack trace if navigation failed.
        /// </summary>
        public string? StackTrace { get; set; }
    }

    /// <summary>
    /// Singleton service for runtime navigation monitoring and logging.
    /// Tracks navigation attempts from button clicks to ViewModel creation.
    /// </summary>
    public static class NavigationMonitorService
    {
        private static readonly List<NavigationLogEntry> _logs = new();
        private static readonly object _lock = new();

        /// <summary>
        /// Event raised when a navigation fails.
        /// </summary>
        public static event EventHandler<NavigationLogEntry>? OnNavigationFailed;

        /// <summary>
        /// Logs a navigation attempt.
        /// </summary>
        /// <param name="feature">The feature name being navigated to.</param>
        /// <param name="viewModel">The ViewModel instance created (null if failed).</param>
        /// <param name="error">The exception that occurred (null if successful).</param>
        public static void LogNavigation(string feature, object? viewModel, Exception? error = null)
        {
            var entry = new NavigationLogEntry
            {
                Timestamp = DateTime.Now,
                FeatureName = feature,
                ViewModelType = viewModel?.GetType().Name ?? "null",
                Success = error == null && viewModel != null,
                ErrorMessage = error?.Message,
                StackTrace = error?.StackTrace
            };

            lock (_lock)
            {
                _logs.Add(entry);
            }

            // Output to debug window for immediate visibility
            if (entry.Success)
            {
                Debug.WriteLine($"[NavigationMonitor] SUCCESS: {feature} -> {entry.ViewModelType}");
            }
            else
            {
                Debug.WriteLine($"[NavigationMonitor] FAILED: {feature}");
                if (error != null)
                {
                    Debug.WriteLine($"[NavigationMonitor] ERROR: {error.Message}");
                    Debug.WriteLine($"[NavigationMonitor] STACK: {error.StackTrace}");
                }
                else
                {
                    Debug.WriteLine($"[NavigationMonitor] ERROR: ViewModel is null");
                }

                // Raise event for UI notification
                OnNavigationFailed?.Invoke(null, entry);
            }
        }

        /// <summary>
        /// Gets all navigation logs.
        /// </summary>
        /// <returns>An enumerable of all navigation log entries.</returns>
        public static IEnumerable<NavigationLogEntry> GetLogs()
        {
            lock (_lock)
            {
                return _logs.ToList();
            }
        }

        /// <summary>
        /// Gets a formatted navigation report summary.
        /// </summary>
        /// <returns>A formatted string containing navigation statistics.</returns>
        public static string GetNavigationReport()
        {
            lock (_lock)
            {
                if (_logs.Count == 0)
                {
                    return "No navigation attempts recorded.";
                }

                var total = _logs.Count;
                var successful = _logs.Count(l => l.Success);
                var failed = total - successful;
                var successRate = total > 0 ? (successful * 100.0 / total) : 0;

                var report = $"""
                    Navigation Report
                    =================
                    Total Attempts: {total}
                    Successful: {successful}
                    Failed: {failed}
                    Success Rate: {successRate:F1}%

                    Recent Attempts:
                    """;

                foreach (var log in _logs.TakeLast(10))
                {
                    var status = log.Success ? "✓" : "✗";
                    report += $"\n  {status} [{log.Timestamp:HH:mm:ss}] {log.FeatureName}";
                    if (!log.Success)
                    {
                        report += $" - {log.ErrorMessage ?? "Unknown error"}";
                    }
                    else
                    {
                        report += $" -> {log.ViewModelType}";
                    }
                }

                if (_logs.Count > 10)
                {
                    report += $"\n  ... and {_logs.Count - 10} more";
                }

                return report;
            }
        }

        /// <summary>
        /// Checks if all navigation attempts were successful.
        /// </summary>
        /// <returns>True if all attempts succeeded and at least one attempt was made; otherwise, false.</returns>
        public static bool AllNavigationsSuccessful()
        {
            lock (_lock)
            {
                return _logs.Count > 0 && _logs.All(l => l.Success);
            }
        }

        /// <summary>
        /// Clears all navigation logs.
        /// </summary>
        public static void ClearLogs()
        {
            lock (_lock)
            {
                _logs.Clear();
            }

            Debug.WriteLine("[NavigationMonitor] Logs cleared");
        }

        /// <summary>
        /// Gets the count of navigation attempts.
        /// </summary>
        /// <returns>The total number of navigation attempts.</returns>
        public static int GetAttemptCount()
        {
            lock (_lock)
            {
                return _logs.Count;
            }
        }

        /// <summary>
        /// Gets the count of successful navigation attempts.
        /// </summary>
        /// <returns>The number of successful navigation attempts.</returns>
        public static int GetSuccessCount()
        {
            lock (_lock)
            {
                return _logs.Count(l => l.Success);
            }
        }

        /// <summary>
        /// Gets the count of failed navigation attempts.
        /// </summary>
        /// <returns>The number of failed navigation attempts.</returns>
        public static int GetFailureCount()
        {
            lock (_lock)
            {
                return _logs.Count(l => !l.Success);
            }
        }
    }
}
