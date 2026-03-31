namespace ProductivityWallpaper.Models
{
    /// <summary>
    /// Represents the availability status of a resource file.
    /// </summary>
    public enum ResourceStatus
    {
        /// <summary>
        /// File exists and is accessible at SourcePath.
        /// </summary>
        OK = 0,

        /// <summary>
        /// File was not found at the expected SourcePath.
        /// </summary>
        Missing = 1,

        /// <summary>
        /// Hash matches another resource already in the library (duplicate detected).
        /// </summary>
        Duplicate = 2,

        /// <summary>
        /// An error occurred while accessing or validating the file.
        /// </summary>
        Error = 3
    }
}
