namespace ProductivityWallpaper.ViewModels
{
    /// <summary>
    /// Common interface for all feature ViewModels displayed in the Creator view.
    /// Provides the minimum contract for scheme identity, naming, and activation state.
    /// </summary>
    public interface IFeatureViewModel
    {
        /// <summary>
        /// Gets or sets the display name of the current scheme.
        /// </summary>
        string SchemeName { get; set; }

        /// <summary>
        /// Gets or sets whether the scheme name is being inline-edited.
        /// </summary>
        bool IsEditingName { get; set; }

        /// <summary>
        /// Gets or sets whether this scheme is currently active.
        /// </summary>
        bool IsActive { get; set; }
    }
}
