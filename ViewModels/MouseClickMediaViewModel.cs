namespace ProductivityWallpaper.ViewModels
{
    /// <summary>
    /// ViewModel for the media configuration sub-section within MouseClick view.
    /// This extends MediaConfigurationViewModel to provide the same media management UI
    /// (import, preview, playback mode) as other feature pages, but embedded as a sub-section.
    /// Includes audio support.
    /// </summary>
    public partial class MouseClickMediaViewModel : MediaConfigurationViewModel
    {
        protected override string DefaultSchemeName => "Mouse Click Media";
        public override string StorageKeyPrefix => "MouseClickMedia";
        public override bool IncludeAudio => true;
    }
}
