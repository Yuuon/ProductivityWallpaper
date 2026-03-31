namespace ProductivityWallpaper.ViewModels
{
    /// <summary>
    /// ViewModel for the Boot/Restart configuration view.
    /// </summary>
    public partial class BootRestartViewModel : MediaConfigurationViewModel
    {
        protected override string DefaultSchemeName => "Boot & Restart";
        public override string StorageKeyPrefix => "BootRestart";
    }
}
