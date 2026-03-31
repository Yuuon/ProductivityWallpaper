namespace ProductivityWallpaper.ViewModels
{
    /// <summary>
    /// ViewModel for the Shutdown configuration view.
    /// </summary>
    public partial class ShutdownViewModel : MediaConfigurationViewModel
    {
        protected override string DefaultSchemeName => "Shutdown";
        public override string StorageKeyPrefix => "Shutdown";
    }
}
