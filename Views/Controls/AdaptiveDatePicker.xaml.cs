using System;
using System.Windows;
using System.Windows.Controls;
using ProductivityWallpaper.Models;

namespace ProductivityWallpaper.Views.Controls
{
    /// <summary>
    /// A date picker control that adapts its input method based on the repeat mode.
    /// Shows different inputs for NoRepeat (full date), Yearly (month/day), Monthly (day), and Weekly (day of week).
    /// </summary>
    public partial class AdaptiveDatePicker : System.Windows.Controls.UserControl
    {
        // --- Dependency Properties ---

        /// <summary>
        /// Identifies the Mode dependency property.
        /// </summary>
        public static readonly DependencyProperty ModeProperty =
            DependencyProperty.Register(
                nameof(Mode),
                typeof(RepeatMode),
                typeof(AdaptiveDatePicker),
                new PropertyMetadata(RepeatMode.NoRepeat, OnModeChanged));

        /// <summary>
        /// Identifies the Date dependency property.
        /// </summary>
        public static readonly DependencyProperty DateProperty =
            DependencyProperty.Register(
                nameof(Date),
                typeof(DateTime),
                typeof(AdaptiveDatePicker),
                new PropertyMetadata(DateTime.Today, OnDateChanged));

        /// <summary>
        /// Identifies the WeeklyDay dependency property.
        /// </summary>
        public static readonly DependencyProperty WeeklyDayProperty =
            DependencyProperty.Register(
                nameof(WeeklyDay),
                typeof(DayOfWeek),
                typeof(AdaptiveDatePicker),
                new PropertyMetadata(DateTime.Today.DayOfWeek, OnWeeklyDayChanged));

        // --- Properties ---

        /// <summary>
        /// Gets or sets the repeat mode that determines the input type.
        /// </summary>
        public RepeatMode Mode
        {
            get => (RepeatMode)GetValue(ModeProperty);
            set => SetValue(ModeProperty, value);
        }

        /// <summary>
        /// Gets or sets the selected date.
        /// </summary>
        public DateTime Date
        {
            get => (DateTime)GetValue(DateProperty);
            set => SetValue(DateProperty, value);
        }

        /// <summary>
        /// Gets or sets the selected day of week for weekly repeat mode.
        /// </summary>
        public DayOfWeek WeeklyDay
        {
            get => (DayOfWeek)GetValue(WeeklyDayProperty);
            set => SetValue(WeeklyDayProperty, value);
        }

        // --- Private Fields ---

        private bool _isUpdating;

        // --- Constructor ---

        /// <summary>
        /// Initializes a new instance of the <see cref="AdaptiveDatePicker"/> class.
        /// </summary>
        public AdaptiveDatePicker()
        {
            InitializeComponent();
            InitializeComboBoxes();
            UpdateControlValues();
        }

        // --- Initialization ---

        /// <summary>
        /// Initializes the combo boxes with their items.
        /// </summary>
        private void InitializeComboBoxes()
        {
            // Initialize months (1-12)
            for (int i = 1; i <= 12; i++)
            {
                YearlyMonthCombo.Items.Add(new ComboBoxItem { Content = i.ToString("D2"), Tag = i });
            }

            // Initialize days (1-31) - will be filtered dynamically
            UpdateDayComboBox(YearlyDayCombo, 31);
            UpdateDayComboBox(MonthlyDayCombo, 31);

            // Initialize days of week
            foreach (DayOfWeek dayOfWeek in Enum.GetValues(typeof(DayOfWeek)))
            {
                WeeklyDayCombo.Items.Add(new ComboBoxItem { Content = dayOfWeek.ToString(), Tag = dayOfWeek });
            }

            // Set initial selections
            YearlyMonthCombo.SelectedIndex = Date.Month - 1;
            YearlyDayCombo.SelectedIndex = Date.Day - 1;
            MonthlyDayCombo.SelectedIndex = Date.Day - 1;
            WeeklyDayCombo.SelectedIndex = (int)WeeklyDay;
        }

        /// <summary>
        /// Updates a day combo box with the specified number of days.
        /// </summary>
        private static void UpdateDayComboBox(System.Windows.Controls.ComboBox comboBox, int daysInMonth)
        {
            comboBox.Items.Clear();
            for (int i = 1; i <= daysInMonth; i++)
            {
                comboBox.Items.Add(new ComboBoxItem { Content = i.ToString("D2"), Tag = i });
            }
        }

        // --- Property Changed Handlers ---

        /// <summary>
        /// Called when the Mode property changes.
        /// </summary>
        private static void OnModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is AdaptiveDatePicker picker)
            {
                picker.UpdateControlValues();
            }
        }

        /// <summary>
        /// Called when the Date property changes.
        /// </summary>
        private static void OnDateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is AdaptiveDatePicker picker && !picker._isUpdating)
            {
                picker.UpdateControlValues();
            }
        }

        /// <summary>
        /// Called when the WeeklyDay property changes.
        /// </summary>
        private static void OnWeeklyDayChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is AdaptiveDatePicker picker && !picker._isUpdating)
            {
                picker.WeeklyDayCombo.SelectedIndex = (int)picker.WeeklyDay;
            }
        }

        // --- Update Methods ---

        /// <summary>
        /// Updates all control values based on the current properties.
        /// </summary>
        private void UpdateControlValues()
        {
            _isUpdating = true;

            try
            {
                // Update full date picker
                FullDatePicker.SelectedDate = Date;

                // Update yearly combos
                int daysInMonth = DateTime.DaysInMonth(2020, Date.Month); // Use non-leap year for consistent day count
                UpdateDayComboBox(YearlyDayCombo, daysInMonth);

                if (Date.Day <= daysInMonth)
                {
                    YearlyMonthCombo.SelectedIndex = Date.Month - 1;
                    YearlyDayCombo.SelectedIndex = Date.Day - 1;
                }

                // Update monthly combo
                UpdateDayComboBox(MonthlyDayCombo, 31);
                if (Date.Day <= 31)
                {
                    MonthlyDayCombo.SelectedIndex = Date.Day - 1;
                }

                // Update weekly combo
                WeeklyDayCombo.SelectedIndex = (int)WeeklyDay;
            }
            finally
            {
                _isUpdating = false;
            }
        }

        // --- Event Handlers ---

        /// <summary>
        /// Handles the SelectedDateChanged event of the full date picker.
        /// </summary>
        private void FullDatePicker_SelectedDateChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_isUpdating || FullDatePicker.SelectedDate == null)
                return;

            _isUpdating = true;
            try
            {
                Date = FullDatePicker.SelectedDate.Value;
                WeeklyDay = Date.DayOfWeek;
            }
            finally
            {
                _isUpdating = false;
            }
        }

        /// <summary>
        /// Handles the SelectionChanged event of the yearly month combo.
        /// </summary>
        private void YearlyMonthCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdating || YearlyMonthCombo.SelectedItem == null)
                return;

            int month = (int)((ComboBoxItem)YearlyMonthCombo.SelectedItem).Tag;
            int daysInMonth = DateTime.DaysInMonth(2020, month);

            // Update day combo box
            int currentDay = Date.Day;
            UpdateDayComboBox(YearlyDayCombo, daysInMonth);

            // Try to preserve the day, or select the last day of month
            int newDay = Math.Min(currentDay, daysInMonth);
            YearlyDayCombo.SelectedIndex = newDay - 1;

            UpdateDateFromYearlySelection(month, newDay);
        }

        /// <summary>
        /// Handles the SelectionChanged event of the yearly day combo.
        /// </summary>
        private void YearlyDayCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdating || YearlyDayCombo.SelectedItem == null)
                return;

            int month = YearlyMonthCombo.SelectedIndex + 1;
            int day = (int)((ComboBoxItem)YearlyDayCombo.SelectedItem).Tag;

            UpdateDateFromYearlySelection(month, day);
        }

        /// <summary>
        /// Updates the Date property from yearly selection.
        /// </summary>
        private void UpdateDateFromYearlySelection(int month, int day)
        {
            _isUpdating = true;
            try
            {
                int year = Date.Year;
                // Adjust for leap year if needed
                if (month == 2 && day == 29)
                {
                    while (!DateTime.IsLeapYear(year))
                    {
                        year++;
                    }
                }

                // Ensure valid date
                int daysInMonth = DateTime.DaysInMonth(year, month);
                day = Math.Min(day, daysInMonth);

                Date = new DateTime(year, month, day);
                WeeklyDay = Date.DayOfWeek;
            }
            finally
            {
                _isUpdating = false;
            }
        }

        /// <summary>
        /// Handles the SelectionChanged event of the monthly day combo.
        /// </summary>
        private void MonthlyDayCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdating || MonthlyDayCombo.SelectedItem == null)
                return;

            int day = (int)((ComboBoxItem)MonthlyDayCombo.SelectedItem).Tag;

            _isUpdating = true;
            try
            {
                // Keep current month/year, just change the day
                int year = Date.Year;
                int month = Date.Month;

                // If the day doesn't exist in this month, adjust
                int daysInMonth = DateTime.DaysInMonth(year, month);
                day = Math.Min(day, daysInMonth);

                Date = new DateTime(year, month, day);
                WeeklyDay = Date.DayOfWeek;
            }
            finally
            {
                _isUpdating = false;
            }
        }

        /// <summary>
        /// Handles the SelectionChanged event of the weekly day combo.
        /// </summary>
        private void WeeklyDayCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdating || WeeklyDayCombo.SelectedItem == null)
                return;

            var dayOfWeek = (DayOfWeek)((ComboBoxItem)WeeklyDayCombo.SelectedItem).Tag;

            _isUpdating = true;
            try
            {
                WeeklyDay = dayOfWeek;

                // Update the date to the next occurrence of this day of week
                var current = Date;
                var daysDiff = ((int)dayOfWeek - (int)current.DayOfWeek + 7) % 7;
                Date = current.AddDays(daysDiff);
            }
            finally
            {
                _isUpdating = false;
            }
        }
    }
}
