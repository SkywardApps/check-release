using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CheckRelease.Domain;
using CheckRelease.Interfaces;
using LibGit2Sharp;

namespace CheckRelease.Adapters
{
    /// <summary>
    /// Implementation of IGitRepository using LibGit2Sharp.
    /// </summary>
    public class LibGit2SharpRepository : IGitRepository
    {
        private readonly Repository _repository;
        private readonly bool _debugMode;
        private readonly IConsoleOutput _console;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="LibGit2SharpRepository"/> class.
        /// </summary>
        /// <param name="repositoryPath">The path to the Git repository.</param>
        /// <param name="debugMode">Whether debug mode is enabled.</param>
        /// <param name="console">The console output interface.</param>
        public LibGit2SharpRepository(string repositoryPath, bool debugMode = false, IConsoleOutput? console = null)
        {
            _repository = new Repository(repositoryPath);
            _debugMode = debugMode;
            _console = console ?? new ConsoleOutput(debugMode);
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="LibGit2SharpRepository"/> class.
        /// </summary>
        /// <param name="repository">The LibGit2Sharp repository.</param>
        /// <param name="debugMode">Whether debug mode is enabled.</param>
        /// <param name="console">The console output interface.</param>
        public LibGit2SharpRepository(Repository repository, bool debugMode = false, IConsoleOutput? console = null)
        {
            _repository = repository;
            _debugMode = debugMode;
            _console = console ?? new ConsoleOutput(debugMode);
        }
        
        /// <inheritdoc/>
        public bool IsValid()
        {
            return Repository.IsValid(_repository.Info.WorkingDirectory);
        }
        
        /// <inheritdoc/>
        public IEnumerable<GitTag> GetAllTags()
        {
            return _repository.Tags.Select(tag => new GitTag
            {
                Name = tag.FriendlyName,
                TargetCommitSha = (tag.PeeledTarget as Commit)?.Sha ?? string.Empty,
                CreatedAt = (tag.PeeledTarget as Commit)?.Author.When ?? DateTimeOffset.MinValue
            });
        }
        
        /// <inheritdoc/>
        public GitTag? GetTag(string tagName)
        {
            var tag = _repository.Tags[tagName];
            if (tag == null) return null;
            
            return new GitTag
            {
                Name = tag.FriendlyName,
                TargetCommitSha = (tag.PeeledTarget as Commit)?.Sha ?? string.Empty,
                CreatedAt = (tag.PeeledTarget as Commit)?.Author.When ?? DateTimeOffset.MinValue
            };
        }
        
        /// <inheritdoc/>
        public bool TagExists(string tagName)
        {
            return _repository.Tags[tagName] != null;
        }
        
        /// <inheritdoc/>
        public GitCommit? GetHeadCommit()
        {
            var headCommit = _repository.Head.Tip;
            if (headCommit == null) return null;
            
            return MapCommit(headCommit);
        }
        
        /// <inheritdoc/>
        public GitCommit? LookupCommit(string shaOrRef)
        {
            // Check if it's HEAD
            if (shaOrRef == "HEAD")
            {
                return GetHeadCommit();
            }
            
            // Try to look up as a commit
            var commit = _repository.Lookup<Commit>(shaOrRef);
            if (commit != null)
            {
                return MapCommit(commit);
            }
            
            // Try to look up as a tag
            var tag = _repository.Tags[shaOrRef];
            if (tag?.PeeledTarget is Commit tagCommit)
            {
                return MapCommit(tagCommit);
            }
            
            return null;
        }
        
        /// <inheritdoc/>
        public IEnumerable<GitCommit> GetCommitsBetween(string olderRef, string newerRef)
        {
            try
            {
                if (_debugMode)
                {
                    _console.WriteDebug($"Getting commits between: {olderRef} -> {newerRef}");
                }
                
                var olderCommit = _repository.Lookup<Commit>(olderRef);
                var newerCommit = _repository.Lookup<Commit>(newerRef);
                
                if (olderCommit == null || newerCommit == null)
                {
                    if (_debugMode)
                    {
                        _console.WriteDebug($"Could not find commits for {olderRef} or {newerRef}");
                    }
                    return Enumerable.Empty<GitCommit>();
                }
                
                // Try both directions to see which one works
                var filter1 = new CommitFilter
                {
                    IncludeReachableFrom = newerCommit,
                    ExcludeReachableFrom = olderCommit,
                    SortBy = CommitSortStrategies.Time
                };
                
                var filter2 = new CommitFilter
                {
                    IncludeReachableFrom = olderCommit,
                    ExcludeReachableFrom = newerCommit,
                    SortBy = CommitSortStrategies.Time
                };
                
                var commits1 = _repository.Commits.QueryBy(filter1).ToList();
                var commits2 = _repository.Commits.QueryBy(filter2).ToList();
                
                if (_debugMode)
                {
                    _console.WriteDebug($"Filter 1 (newer reachable, not older): Found {commits1.Count} commits");
                    _console.WriteDebug($"Filter 2 (older reachable, not newer): Found {commits2.Count} commits");
                }
                
                // Use the filter that found commits
                var commits = commits1.Count > 0 ? commits1 : commits2;
                
                return commits.Select(MapCommit);
            }
            catch (Exception ex)
            {
                if (_debugMode)
                {
                    _console.WriteError($"Error getting commits between {olderRef} and {newerRef}: {ex.Message}");
                }
                return Enumerable.Empty<GitCommit>();
            }
        }
        
        /// <inheritdoc/>
        public IEnumerable<GitCommit> GetCommitsReachableFrom(string reference)
        {
            try
            {
                var commit = _repository.Lookup<Commit>(reference);
                if (commit == null)
                {
                    return Enumerable.Empty<GitCommit>();
                }
                
                var filter = new CommitFilter
                {
                    IncludeReachableFrom = commit,
                    SortBy = CommitSortStrategies.Time
                };
                
                return _repository.Commits.QueryBy(filter).Select(MapCommit);
            }
            catch (Exception ex)
            {
                if (_debugMode)
                {
                    _console.WriteError($"Error getting commits reachable from {reference}: {ex.Message}");
                }
                return Enumerable.Empty<GitCommit>();
            }
        }
        
        /// <inheritdoc/>
        public IEnumerable<GitCommit> GetCommitsReachableFromButNotFrom(string includeReference, string excludeReference)
        {
            try
            {
                var includeCommit = _repository.Lookup<Commit>(includeReference);
                var excludeCommit = _repository.Lookup<Commit>(excludeReference);
                
                if (includeCommit == null || excludeCommit == null)
                {
                    return Enumerable.Empty<GitCommit>();
                }
                
                var filter = new CommitFilter
                {
                    IncludeReachableFrom = includeCommit,
                    ExcludeReachableFrom = excludeCommit,
                    SortBy = CommitSortStrategies.Time
                };
                
                return _repository.Commits.QueryBy(filter).Select(MapCommit);
            }
            catch (Exception ex)
            {
                if (_debugMode)
                {
                    _console.WriteError($"Error getting commits reachable from {includeReference} but not from {excludeReference}: {ex.Message}");
                }
                return Enumerable.Empty<GitCommit>();
            }
        }
        
        /// <inheritdoc/>
        public string? GetFileContentAtReference(string reference, string filePath)
        {
            try
            {
                var commit = _repository.Lookup<Commit>(reference);
                if (commit == null) return null;
                
                var tree = commit.Tree;
                var treeEntry = tree[filePath];
                if (treeEntry == null) return null;
                
                var blob = (Blob)treeEntry.Target;
                using var contentStream = blob.GetContentStream();
                using var reader = new StreamReader(contentStream);
                return reader.ReadToEnd();
            }
            catch (Exception ex)
            {
                if (_debugMode)
                {
                    _console.WriteError($"Error getting file content at {reference} for {filePath}: {ex.Message}");
                }
                return null;
            }
        }
        
        /// <summary>
        /// Maps a LibGit2Sharp Commit to a GitCommit.
        /// </summary>
        /// <param name="commit">The LibGit2Sharp Commit.</param>
        /// <returns>A GitCommit.</returns>
        private GitCommit MapCommit(Commit commit)
        {
            return new GitCommit
            {
                Sha = commit.Sha,
                Message = commit.Message,
                AuthorName = commit.Author.Name,
                AuthorWhen = commit.Author.When,
                ParentCount = commit.Parents.Count()
            };
        }
        
        /// <inheritdoc/>
        public void Dispose()
        {
            _repository.Dispose();
        }
    }
}
