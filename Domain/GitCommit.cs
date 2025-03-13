using System;

namespace CheckRelease.Domain
{
    /// <summary>
    /// Represents a Git commit.
    /// </summary>
    public class GitCommit
    {
        /// <summary>
        /// Gets or sets the SHA hash of the commit.
        /// </summary>
        public string Sha { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the commit message.
        /// </summary>
        public string Message { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the name of the author.
        /// </summary>
        public string AuthorName { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the date and time when the commit was authored.
        /// </summary>
        public DateTimeOffset AuthorWhen { get; set; }
        
        /// <summary>
        /// Gets or sets the number of parent commits.
        /// </summary>
        public int ParentCount { get; set; }
    }
}
