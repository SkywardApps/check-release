using System;

namespace CheckRelease.Domain
{
    /// <summary>
    /// Represents a Git tag.
    /// </summary>
    public class GitTag
    {
        /// <summary>
        /// Gets or sets the name of the tag.
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the SHA hash of the target commit.
        /// </summary>
        public string TargetCommitSha { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the date and time when the tag was created.
        /// </summary>
        public DateTimeOffset CreatedAt { get; set; }
    }
}
