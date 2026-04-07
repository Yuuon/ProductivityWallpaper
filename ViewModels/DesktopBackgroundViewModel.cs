namespace ProductivityWallpaper.ViewModels
{
    /// <summary>
    /// ViewModel for the Desktop Background configuration view.
    /// Desktop backgrounds do not include a separate audio section.
    /// </summary>
    public partial class DesktopBackgroundViewModel : MediaConfigurationViewModel
    {
        protected override string DefaultSchemeName => "Desktop Background";
        public override string StorageKeyPrefix => "DesktopBackground";
        public override bool IncludeAudio => false;
    }
}
