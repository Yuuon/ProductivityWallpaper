using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ProductivityWallpaper.Models
{
    /// <summary>
    /// Represents a configuration scheme for a specific feature.
    /// A scheme contains settings and media references for a particular behavior or appearance.
    /// Media is referenced by ID (not embedded objects) for theme resource sharing.
    /// </summary>
    public partial class SchemeModel : ObservableObject
    {
        /// <summary>
        /// Gets or sets the unique identifier for this scheme.
        /// </summary>
        private string _id = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets or sets the unique identifier for this scheme.
        /// </summary>
        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        /// <summary>
        /// Gets or sets the display name of the scheme.
        /// </summary>
        private string _name = string.Empty;

        /// <summary>
        /// Gets or sets the display name of the scheme.
        /// </summary>
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        /// <summary>
        /// Gets or sets the type of feature this scheme belongs to.
        /// </summary>
        private FeatureType _featureType;

        /// <summary>
        /// Gets or sets the type of feature this scheme belongs to.
        /// </summary>
        public FeatureType FeatureType
        {
            get => _featureType;
            set => SetProperty(ref _featureType, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether this scheme is currently active.
        /// Only one scheme can be active per feature at a time.
        /// </summary>
        private bool _isActive;

        /// <summary>
        /// Gets or sets a value indicating whether this scheme is currently active.
        /// Only one scheme can be active per feature at a time.
        /// </summary>
        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether this scheme is currently selected in the UI.
        /// The selected scheme is the one being edited, different from the active scheme.
        /// </summary>
        private bool _isSelected;

        /// <summary>
        /// Gets or sets a value indicating whether this scheme is currently selected in the UI.
        /// The selected scheme is the one being edited, different from the active scheme.
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        /// <summary>
        /// Gets or sets the date and time when this scheme was created.
        /// </summary>
        private DateTime _createdAt = DateTime.Now;

        /// <summary>
        /// Gets or sets the date and time when this scheme was created.
        /// </summary>
        public DateTime CreatedAt
        {
            get => _createdAt;
            set => SetProperty(ref _createdAt, value);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SchemeModel"/> class.
        /// </summary>
        public SchemeModel()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SchemeModel"/> class with the specified parameters.
        /// </summary>
        /// <param name="name">The display name of the scheme.</param>
        /// <param name="featureType">The type of feature this scheme belongs to.</param>
        public SchemeModel(string name, FeatureType featureType)
        {
            _name = name;
            _featureType = featureType;
        }

        // ==================== Media References (ID-based) ====================

        /// <summary>
        /// Desktop background media (ordered list with playback settings).
        /// Used for DesktopBackground feature type.
        /// </summary>
        [ObservableProperty]
        private MediaReferenceList _desktopBackgroundMedia = new();

        /// <summary>
        /// Media for system events (shutdown, boot, wake, lock, unlock, etc.).
        /// Same structure as desktop background but typically shorter clips.
        /// </summary>
        [ObservableProperty]
        private MediaReferenceList _eventMedia = new();

        /// <summary>
        /// Click regions for MouseClick feature type.
        /// Each region has position, size, and associated click actions.
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<ClickRegionModel> _clickRegions = new();

        /// <summary>
        /// Hover text displayed when mouse hovers over character.
        /// </summary>
        [ObservableProperty]
        private string _hoverText = string.Empty;

        /// <summary>
        /// Delay in milliseconds before showing hover text.
        /// </summary>
        [ObservableProperty]
        private int _hoverDelayMs = 500;
    }
}
