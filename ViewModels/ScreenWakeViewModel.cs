namespace ProductivityWallpaper.ViewModels
{
    /// <summary>
    /// ViewModel for the Screen Wake configuration view.
    /// </summary>
    public partial class ScreenWakeViewModel : MediaConfigurationViewModel
    {
        protected override string DefaultSchemeName => "Screen Wake";
        public override string StorageKeyPrefix => "ScreenWake";
    }
}
