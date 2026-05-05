using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ProductivityWallpaper.Models
{
    /// <summary>
    /// Represents an ordered list of media references with playback/display settings.
    /// Used for Desktop Background, system events, and click actions.
    /// References media by ID (not embedded objects) for theme resource sharing.
    /// </summary>
    public partial class MediaReferenceList : ObservableObject
    {
        /// <summary>
        /// Ordered list of media resource IDs from the ThemeResourceLibrary.
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<string> _mediaIds = new();

        /// <summary>
        /// Playback mode: Sequential or Random.
        /// </summary>
        [ObservableProperty]
        private PlaybackMode _playbackMode = PlaybackMode.Sequential;

        /// <summary>
        /// Display mode for images/videos: Fill, Center, or Tile.
        /// Used as the default/fallback when per-item modes are not specified.
        /// </summary>
        [ObservableProperty]
        private DisplayMode _displayMode = DisplayMode.Fill;

        /// <summary>
        /// Per-item display modes, aligned by index with <see cref="MediaIds"/>.
        /// When this list is shorter than MediaIds, missing entries fall back to <see cref="DisplayMode"/>.
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<DisplayMode> _itemDisplayModes = new();

        /// <summary>
        /// Whether audio should be muted during playback.
        /// </summary>
        [ObservableProperty]
        private bool _isMuted;

        /// <summary>
        /// Optional duration to show each item (for timed transitions).
        /// Null = use media duration (for video) or indefinite (for images).
        /// </summary>
        [ObservableProperty]
        private TimeSpan? _itemDuration;

        /// <summary>
        /// Validates that all referenced media IDs exist in the library.
        /// </summary>
        public bool ValidateReferences(ThemeResourceLibrary library, out List<string> missingIds)
        {
            missingIds = MediaIds.Where(id => library.GetById(id) == null).ToList();
            return missingIds.Count == 0;
        }
    }

    /// <summary>
    /// Represents a click action: one visual media + up to 5 audio files.
    /// Uses ID-based references to ThemeResourceLibrary.
    /// </summary>
    public partial class ClickAction : ObservableObject
    {
        /// <summary>
        /// Resource ID for visual content (image or video). Null = no visual.
        /// </summary>
        [ObservableProperty]
        private string? _visualMediaId;

        /// <summary>
        /// Display mode for the visual content (image or video).
        /// </summary>
        [ObservableProperty]
        private DisplayMode _visualDisplayMode = DisplayMode.Fill;

        /// <summary>
        /// Resource IDs for audio content (max 5).
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<string> _audioMediaIds = new();

        /// <summary>
        /// Validates the action configuration.
        /// </summary>
        public bool Validate(out string? error)
        {
            if (AudioMediaIds.Count > 5)
            {
                error = "Maximum 5 audio files allowed";
                return false;
            }
            error = null;
            return true;
        }
    }
}
