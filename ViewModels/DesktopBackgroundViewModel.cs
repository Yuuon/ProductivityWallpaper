namespace ProductivityWallpaper.ViewModels
{
    /// <summary>
    /// ViewModel for the Desktop Background configuration view.
    /// Desktop backgrounds now include audio that plays alongside wallpapers.
    /// </summary>
    public partial class DesktopBackgroundViewModel : MediaConfigurationViewModel
    {
        protected override string DefaultSchemeName => "Desktop Background";
        public override string StorageKeyPrefix => "DesktopBackground";
        public override bool IncludeAudio => true;
    }
}
