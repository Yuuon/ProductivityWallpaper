using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ProductivityWallpaper.Models
{
    /// <summary>
    /// Represents the type of feature that a scheme can be associated with.
    /// </summary>
    public enum FeatureType
    {
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
        Anniversary
    }

    /// <summary>
    /// Represents a configuration scheme for a specific feature.
    /// A scheme contains settings and media files for a particular behavior or appearance.
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
    }
}
