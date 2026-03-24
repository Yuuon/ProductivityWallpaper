namespace ProductivityWallpaper.Models
{
    /// <summary>
    /// Represents the type of feature that a scheme can be associated with.
    /// </summary>
    public enum FeatureType
    {
        // ==================== Existing Types ====================

        /// <summary>
        /// Desktop background wallpaper feature.
        /// </summary>
        DesktopBackground,

        /// <summary>
        /// Mouse click interaction feature.
        /// </summary>
        MouseClick,

        /// <summary>
        /// System shutdown event feature.
        /// </summary>
        Shutdown,

        /// <summary>
        /// System boot or restart event feature.
        /// </summary>
        BootRestart,

        /// <summary>
        /// Screen wake from sleep event feature.
        /// </summary>
        ScreenWake,

        /// <summary>
        /// Application opening feature.
        /// </summary>
        OpenApp,

        /// <summary>
        /// Desktop clock widget feature.
        /// </summary>
        DesktopClock,

        /// <summary>
        /// Pomodoro timer feature.
        /// </summary>
        Pomodoro,

        /// <summary>
        /// Anniversary/important date reminder feature.
        /// </summary>
        Anniversary,

        // ==================== New System Event Types ====================

        /// <summary>
        /// Windows session locked event.
        /// </summary>
        SessionLock,

        /// <summary>
        /// Windows session unlocked event.
        /// </summary>
        SessionUnlock,

        /// <summary>
        /// Network adapter disconnected event.
        /// </summary>
        NetworkDisconnect,

        /// <summary>
        /// Network adapter reconnected event.
        /// </summary>
        NetworkReconnect,

        /// <summary>
        /// Battery below threshold event.
        /// </summary>
        PowerLow,

        /// <summary>
        /// AC power connected event.
        /// </summary>
        PowerCharging
    }

    /// <summary>
    /// Extension methods for FeatureType enum.
    /// </summary>
    public static class FeatureTypeExtensions
    {
        /// <summary>
        /// Checks if the feature type supports multiple schemes (expandable navigation).
        /// </summary>
        public static bool SupportsMultipleSchemes(this FeatureType type)
        {
            return type is FeatureType.DesktopBackground
                or FeatureType.MouseClick
                or FeatureType.Shutdown
                or FeatureType.BootRestart
                or FeatureType.ScreenWake
                or FeatureType.SessionLock
                or FeatureType.SessionUnlock
                or FeatureType.NetworkDisconnect
                or FeatureType.NetworkReconnect
                or FeatureType.PowerLow
                or FeatureType.PowerCharging;
        }

        /// <summary>
        /// Checks if the feature type is a system event (including existing ones).
        /// </summary>
        public static bool IsSystemEvent(this FeatureType type)
        {
            return type is FeatureType.SessionLock
                or FeatureType.SessionUnlock
                or FeatureType.NetworkDisconnect
                or FeatureType.NetworkReconnect
                or FeatureType.PowerLow
                or FeatureType.PowerCharging
                or FeatureType.Shutdown
                or FeatureType.BootRestart
                or FeatureType.ScreenWake;
        }

        /// <summary>
        /// Gets the Chinese display name for UI rendering.
        /// </summary>
        public static string GetDisplayName(this FeatureType type)
        {
            return type switch
            {
                FeatureType.DesktopBackground => "桌面背景",
                FeatureType.MouseClick => "鼠标点击",
                FeatureType.Shutdown => "关机",
                FeatureType.BootRestart => "开机/重启",
                FeatureType.ScreenWake => "屏幕唤醒",
                FeatureType.OpenApp => "打开应用",
                FeatureType.DesktopClock => "桌面时钟",
                FeatureType.Pomodoro => "番茄钟",
                FeatureType.Anniversary => "纪念日",
                FeatureType.SessionLock => "锁屏",
                FeatureType.SessionUnlock => "解锁",
                FeatureType.NetworkDisconnect => "网络断开",
                FeatureType.NetworkReconnect => "网络重连",
                FeatureType.PowerLow => "电量低",
                FeatureType.PowerCharging => "充电中",
                _ => type.ToString()
            };
        }
    }
}
