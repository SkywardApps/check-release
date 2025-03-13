using System;
using System.Collections.Generic;
using System.Linq;
using CheckRelease.Domain;
using CheckRelease.Interfaces;

namespace CheckRelease.Testing
{
    /// <summary>
    /// Mock implementation of IGitRepository for testing.
    /// </summary>
    public class MockGitRepository : IGitRepository
    {
        private readonly Dictionary<string, GitTag> _tags = new();
        private readonly Dictionary<string, GitCommit> _commits = new();
        private readonly Dictionary<string, List<string>> _commitRelationships = new();
        private readonly Dictionary<(string Reference, string FilePath), string> _fileContents = new();
        private GitCommit? _headCommit;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="MockGitRepository"/> class.
        /// </summary>
        public MockGitRepository() { }
        
        /// <summary>
        /// Adds a tag to the repository.
        /// </summary>
        /// <param name="tag">The tag to add.</param>
        /// <returns>This instance for method chaining.</returns>
        public MockGitRepository AddTag(GitTag tag)
        {
            _tags[tag.Name] = tag;
            return this;
        }
        
        /// <summary>
        /// Adds a commit to the repository.
        /// </summary>
        /// <param name="commit">The commit to add.</param>
        /// <returns>This instance for method chaining.</returns>
        public MockGitRepository AddCommit(GitCommit commit)
        {
            _commits[commit.Sha] = commit;
            return this;
        }
        
        /// <summary>
        /// Sets the HEAD commit.
        /// </summary>
        /// <param name="sha">The SHA of the commit to set as HEAD.</param>
        /// <returns>This instance for method chaining.</returns>
        public MockGitRepository SetHeadCommit(string sha)
        {
            _headCommit = _commits.GetValueOrDefault(sha);
            return this;
        }
        
        /// <summary>
        /// Adds a relationship between two commits.
        /// </summary>
        /// <param name="parentSha">The SHA of the parent commit.</param>
        /// <param name="childSha">The SHA of the child commit.</param>
        /// <returns>This instance for method chaining.</returns>
        public MockGitRepository AddCommitRelationship(string parentSha, string childSha)
        {
            if (!_commitRelationships.ContainsKey(childSha))
                _commitRelationships[childSha] = new List<string>();
                
            _commitRelationships[childSha].Add(parentSha);
            return this;
        }
        
        /// <summary>
        /// Adds file content at a specific reference.
        /// </summary>
        /// <param name="reference">The reference.</param>
        /// <param name="filePath">The path to the file.</param>
        /// <param name="content">The content of the file.</param>
        /// <returns>This instance for method chaining.</returns>
        public MockGitRepository AddFileContent(string reference, string filePath, string content)
        {
            _fileContents[(reference, filePath)] = content;
            return this;
        }
        
        /// <inheritdoc/>
        public bool IsValid() => true;
        
        /// <inheritdoc/>
        public IEnumerable<GitTag> GetAllTags() => _tags.Values;
        
        /// <inheritdoc/>
        public GitTag? GetTag(string tagName) => _tags.GetValueOrDefault(tagName);
        
        /// <inheritdoc/>
        public bool TagExists(string tagName) => _tags.ContainsKey(tagName);
        
        /// <inheritdoc/>
        public GitCommit? GetHeadCommit() => _headCommit;
        
        /// <inheritdoc/>
        public GitCommit? LookupCommit(string shaOrRef)
        {
            if (shaOrRef == "HEAD")
                return _headCommit;
                
            if (_commits.TryGetValue(shaOrRef, out var commit))
                return commit;
                
            if (_tags.TryGetValue(shaOrRef, out var tag))
                return _commits.GetValueOrDefault(tag.TargetCommitSha);
                
            return null;
        }
        
        /// <inheritdoc/>
        public IEnumerable<GitCommit> GetCommitsBetween(string olderRef, string newerRef)
        {
            // For testing purposes, we'll just return all commits
            // This is a simplified implementation that works for our test cases
            return _commits.Values;
        }
        
        /// <inheritdoc/>
        public IEnumerable<GitCommit> GetCommitsReachableFrom(string reference)
        {
            var result = new HashSet<string>();
            var visited = new HashSet<string>();
            var commit = LookupCommit(reference);
            
            if (commit == null)
                return Enumerable.Empty<GitCommit>();
                
            CollectReachableCommits(commit.Sha, result, visited);
            
            return result.Select(sha => _commits[sha]);
        }
        
        /// <inheritdoc/>
        public IEnumerable<GitCommit> GetCommitsReachableFromButNotFrom(string includeReference, string excludeReference)
        {
            var includeCommits = GetCommitsReachableFrom(includeReference).Select(c => c.Sha).ToHashSet();
            var excludeCommits = GetCommitsReachableFrom(excludeReference).Select(c => c.Sha).ToHashSet();
            
            return includeCommits.Except(excludeCommits).Select(sha => _commits[sha]);
        }
        
        private void CollectReachableCommits(string sha, HashSet<string> result, HashSet<string> visited)
        {
            if (visited.Contains(sha))
                return;
                
            visited.Add(sha);
            result.Add(sha);
            
            if (_commitRelationships.TryGetValue(sha, out var children))
            {
                foreach (var child in children)
                {
                    CollectReachableCommits(child, result, visited);
                }
            }
        }
        
        /// <inheritdoc/>
        public string? GetFileContentAtReference(string reference, string filePath)
        {
            return _fileContents.GetValueOrDefault((reference, filePath));
        }
        
        /// <inheritdoc/>
        public void Dispose() { }
    }
}
