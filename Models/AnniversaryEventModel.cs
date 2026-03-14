using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ProductivityWallpaper.Models
{
    /// <summary>
    /// Represents the repeat mode for an anniversary event.
    /// </summary>
    public enum RepeatMode
    {
        /// <summary>
        /// No repeat - show full date: 2024-12-25
        /// </summary>
        NoRepeat,

        /// <summary>
        /// Yearly repeat - show month/day only: 12-25
        /// </summary>
        Yearly,

        /// <summary>
        /// Monthly repeat - show day only: 25
        /// </summary>
        Monthly,

        /// <summary>
        /// Weekly repeat - show day of week: Monday
        /// </summary>
        Weekly
    }

    /// <summary>
    /// Represents a display style for the anniversary widget.
    /// </summary>
    public partial class AnniversaryStyleModel : ObservableObject
    {
        /// <summary>
        /// Gets or sets the unique identifier for this style.
        /// </summary>
        [ObservableProperty]
        private string _id = string.Empty;

        /// <summary>
        /// Gets or sets the display name of this style.
        /// </summary>
        [ObservableProperty]
        private string _name = string.Empty;

        /// <summary>
        /// Gets or sets the path to the preview image for this style.
        /// </summary>
        [ObservableProperty]
        private string _previewImagePath = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether this style is currently active.
        /// </summary>
        [ObservableProperty]
        private bool _isActive;
    }

    /// <summary>
    /// Represents an anniversary event with countdown functionality.
    /// </summary>
    public partial class AnniversaryEventModel : ObservableObject
    {
        /// <summary>
        /// Gets or sets the unique identifier for this event.
        /// </summary>
        [ObservableProperty]
        private string _id = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets or sets the name of the event.
        /// </summary>
        [ObservableProperty]
        private string _name = "New Event";

        /// <summary>
        /// Gets or sets the repeat mode for this event.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DisplayDate))]
        [NotifyPropertyChangedFor(nameof(DaysRemaining))]
        private RepeatMode _repeatMode = RepeatMode.NoRepeat;

        /// <summary>
        /// Gets or sets the target date for this event.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DisplayDate))]
        [NotifyPropertyChangedFor(nameof(DaysRemaining))]
        private DateTime _targetDate = DateTime.Today;

        /// <summary>
        /// Gets or sets the day of week for weekly repeat mode.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DisplayDate))]
        [NotifyPropertyChangedFor(nameof(DaysRemaining))]
        private DayOfWeek _weeklyDay = DateTime.Today.DayOfWeek;

        /// <summary>
        /// Gets or sets a value indicating whether the event name is being edited.
        /// </summary>
        [ObservableProperty]
        private bool _isNameEditing;

        /// <summary>
        /// Gets the number of days remaining until the next occurrence.
        /// </summary>
        public int DaysRemaining
        {
            get
            {
                var next = GetNextOccurrence();
                return (next - DateTime.Today).Days;
            }
        }

        /// <summary>
        /// Gets the formatted display date based on the repeat mode.
        /// </summary>
        public string DisplayDate
        {
            get
            {
                return RepeatMode switch
                {
                    RepeatMode.NoRepeat => TargetDate.ToString("yyyy-MM-dd"),
                    RepeatMode.Yearly => TargetDate.ToString("MM-dd"),
                    RepeatMode.Monthly => TargetDate.ToString("dd"),
                    RepeatMode.Weekly => WeeklyDay.ToString(),
                    _ => TargetDate.ToString("yyyy-MM-dd")
                };
            }
        }

        /// <summary>
        /// Gets the next occurrence of this anniversary based on the repeat mode.
        /// </summary>
        /// <returns>The next occurrence date.</returns>
        private DateTime GetNextOccurrence()
        {
            var today = DateTime.Today;

            switch (RepeatMode)
            {
                case RepeatMode.NoRepeat:
                    return TargetDate;

                case RepeatMode.Yearly:
                    // Same month and day every year
                    var nextYearly = new DateTime(today.Year, TargetDate.Month, TargetDate.Day);
                    if (nextYearly < today)
                    {
                        nextYearly = nextYearly.AddYears(1);
                    }
                    return nextYearly;

                case RepeatMode.Monthly:
                    // Same day every month
                    int day = Math.Min(TargetDate.Day, DateTime.DaysInMonth(today.Year, today.Month));
                    var nextMonthly = new DateTime(today.Year, today.Month, day);
                    if (nextMonthly < today)
                    {
                        // Move to next month
                        var nextMonth = today.AddMonths(1);
                        day = Math.Min(TargetDate.Day, DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month));
                        nextMonthly = new DateTime(nextMonth.Year, nextMonth.Month, day);
                    }
                    return nextMonthly;

                case RepeatMode.Weekly:
                    // Same day of week every week
                    var nextWeekly = today.AddDays(((int)WeeklyDay - (int)today.DayOfWeek + 7) % 7);
                    if (nextWeekly < today)
                    {
                        nextWeekly = nextWeekly.AddDays(7);
                    }
                    return nextWeekly;

                default:
                    return TargetDate;
            }
        }

        /// <summary>
        /// Called when TargetDate changes to update WeeklyDay if needed.
        /// </summary>
        partial void OnTargetDateChanged(DateTime value)
        {
            if (WeeklyDay != value.DayOfWeek)
            {
                WeeklyDay = value.DayOfWeek;
            }
        }

        /// <summary>
        /// Called when WeeklyDay changes to update TargetDate if needed.
        /// </summary>
        partial void OnWeeklyDayChanged(DayOfWeek value)
        {
            var current = TargetDate;
            var daysDiff = ((int)value - (int)current.DayOfWeek + 7) % 7;
            if (daysDiff != 0)
            {
                TargetDate = current.AddDays(daysDiff);
            }
        }
    }
}
