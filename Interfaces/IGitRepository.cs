using System;
using System.Collections.Generic;
using CheckRelease.Domain;

namespace CheckRelease.Interfaces
{
    /// <summary>
    /// Interface for Git repository operations.
    /// </summary>
    public interface IGitRepository : IDisposable
    {
        /// <summary>
        /// Checks if the repository is valid.
        /// </summary>
        /// <returns>True if the repository is valid, false otherwise.</returns>
        bool IsValid();
        
        /// <summary>
        /// Gets all tags in the repository.
        /// </summary>
        /// <returns>A collection of Git tags.</returns>
        IEnumerable<GitTag> GetAllTags();
        
        /// <summary>
        /// Gets a tag by name.
        /// </summary>
        /// <param name="tagName">The name of the tag.</param>
        /// <returns>The Git tag, or null if not found.</returns>
        GitTag? GetTag(string tagName);
        
        /// <summary>
        /// Checks if a tag exists.
        /// </summary>
        /// <param name="tagName">The name of the tag.</param>
        /// <returns>True if the tag exists, false otherwise.</returns>
        bool TagExists(string tagName);
        
        /// <summary>
        /// Gets the HEAD commit.
        /// </summary>
        /// <returns>The HEAD commit, or null if not found.</returns>
        GitCommit? GetHeadCommit();
        
        /// <summary>
        /// Looks up a commit by SHA or reference.
        /// </summary>
        /// <param name="shaOrRef">The SHA or reference to look up.</param>
        /// <returns>The commit, or null if not found.</returns>
        GitCommit? LookupCommit(string shaOrRef);
        
        /// <summary>
        /// Gets commits between two references.
        /// </summary>
        /// <param name="olderRef">The older reference.</param>
        /// <param name="newerRef">The newer reference.</param>
        /// <returns>A collection of commits between the two references.</returns>
        IEnumerable<GitCommit> GetCommitsBetween(string olderRef, string newerRef);
        
        /// <summary>
        /// Gets commits reachable from a reference.
        /// </summary>
        /// <param name="reference">The reference.</param>
        /// <returns>A collection of commits reachable from the reference.</returns>
        IEnumerable<GitCommit> GetCommitsReachableFrom(string reference);
        
        /// <summary>
        /// Gets commits reachable from one reference but not from another.
        /// </summary>
        /// <param name="includeReference">The reference to include commits from.</param>
        /// <param name="excludeReference">The reference to exclude commits from.</param>
        /// <returns>A collection of commits reachable from includeReference but not from excludeReference.</returns>
        IEnumerable<GitCommit> GetCommitsReachableFromButNotFrom(string includeReference, string excludeReference);
        
        /// <summary>
        /// Gets the content of a file at a specific reference.
        /// </summary>
        /// <param name="reference">The reference.</param>
        /// <param name="filePath">The path to the file.</param>
        /// <returns>The content of the file, or null if not found.</returns>
        string? GetFileContentAtReference(string reference, string filePath);
        
        /// <summary>
        /// Gets the merge base (common ancestor) between two references.
        /// </summary>
        /// <param name="reference1">The first reference.</param>
        /// <param name="reference2">The second reference.</param>
        /// <returns>The merge base commit, or null if not found.</returns>
        GitCommit? GetMergeBase(string reference1, string reference2);
    }
}
