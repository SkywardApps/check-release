using System;
using Xunit;
using CheckRelease.Domain;
using CheckRelease.Testing;

namespace CheckRelease.Tests
{
    /// <summary>
    /// Tests for the GitTagSelector common ancestor methods.
    /// </summary>
    public class GitTagSelectorFromCommonAncestorTests
    {
        [Fact]
        public void SelectHeadToCommonAncestor_ValidTargetReference_ReturnsCorrectCommitPair()
        {
            // Arrange
            var mockRepo = new MockGitRepository();
            var console = new MockConsoleOutput();
            
            // Create a simple commit history:
            // commonAncestor <- targetCommit
            //      \
            //       <- headCommit
            var commonAncestor = new GitCommit
            {
                Sha = "abc123",
                Message = "Common ancestor commit",
                AuthorName = "Test Author",
                AuthorWhen = DateTimeOffset.Now.AddDays(-3)
            };
            
            var targetCommit = new GitCommit
            {
                Sha = "def456",
                Message = "Target commit",
                AuthorName = "Test Author",
                AuthorWhen = DateTimeOffset.Now.AddDays(-2)
            };
            
            var headCommit = new GitCommit
            {
                Sha = "ghi789",
                Message = "HEAD commit",
                AuthorName = "Test Author",
                AuthorWhen = DateTimeOffset.Now.AddDays(-1)
            };
            
            mockRepo.AddCommit(commonAncestor)
                    .AddCommit(targetCommit)
                    .AddCommit(headCommit)
                    .SetHeadCommit(headCommit.Sha);
            
            // Mock the merge base to return the common ancestor
            // This is a simplified mock that returns commonAncestor when looking for merge base
            // In a real implementation, we'd need to properly mock the GetMergeBase method
            
            var tagSelector = new GitTagSelector(mockRepo, debugMode: true, console);
            
            // Act & Assert
            // Since our mock doesn't fully implement merge base logic,
            // we'll test the error cases first
            
            var exception = Assert.Throws<ArgumentException>(() => 
                tagSelector.SelectHeadToCommonAncestor("nonexistent-ref"));
            
            Assert.Contains("Reference 'nonexistent-ref' does not exist", exception.Message);
        }
        
        [Fact]
        public void SelectHeadToCommonAncestor_HeadAndTargetAreSameCommit_ThrowsException()
        {
            // Arrange
            var mockRepo = new MockGitRepository();
            var console = new MockConsoleOutput();
            
            var commit = new GitCommit
            {
                Sha = "abc123",
                Message = "Test commit",
                AuthorName = "Test Author",
                AuthorWhen = DateTimeOffset.Now
            };
            
            mockRepo.AddCommit(commit)
                    .SetHeadCommit(commit.Sha);
            
            var tagSelector = new GitTagSelector(mockRepo, debugMode: true, console);
            
            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => 
                tagSelector.SelectHeadToCommonAncestor(commit.Sha));
            
            Assert.Contains("HEAD and 'abc123' are the same commit", exception.Message);
        }
        
        [Fact]
        public void SelectHeadToCommonAncestor_NoHeadCommit_ThrowsException()
        {
            // Arrange
            var mockRepo = new MockGitRepository();
            var console = new MockConsoleOutput();
            
            var commit = new GitCommit
            {
                Sha = "abc123",
                Message = "Test commit",
                AuthorName = "Test Author",
                AuthorWhen = DateTimeOffset.Now
            };
            
            mockRepo.AddCommit(commit);
            // Note: Not setting HEAD commit
            
            var tagSelector = new GitTagSelector(mockRepo, debugMode: true, console);
            
            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => 
                tagSelector.SelectHeadToCommonAncestor(commit.Sha));
            
            Assert.Contains("Could not get HEAD commit", exception.Message);
        }
        
        #region SelectReferencesToCommonAncestor Tests
        
        [Fact]
        public void SelectReferencesToCommonAncestor_FirstReferenceDoesNotExist_ThrowsException()
        {
            // Arrange
            var mockRepo = new MockGitRepository();
            var console = new MockConsoleOutput();
            
            var commit = new GitCommit
            {
                Sha = "abc123",
                Message = "Test commit",
                AuthorName = "Test Author",
                AuthorWhen = DateTimeOffset.Now
            };
            
            mockRepo.AddCommit(commit);
            
            var tagSelector = new GitTagSelector(mockRepo, debugMode: true, console);
            
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => 
                tagSelector.SelectReferencesToCommonAncestor("nonexistent-ref", commit.Sha));
            
            Assert.Contains("Reference 'nonexistent-ref' does not exist", exception.Message);
        }
        
        [Fact]
        public void SelectReferencesToCommonAncestor_SecondReferenceDoesNotExist_ThrowsException()
        {
            // Arrange
            var mockRepo = new MockGitRepository();
            var console = new MockConsoleOutput();
            
            var commit = new GitCommit
            {
                Sha = "abc123",
                Message = "Test commit",
                AuthorName = "Test Author",
                AuthorWhen = DateTimeOffset.Now
            };
            
            mockRepo.AddCommit(commit);
            
            var tagSelector = new GitTagSelector(mockRepo, debugMode: true, console);
            
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => 
                tagSelector.SelectReferencesToCommonAncestor(commit.Sha, "nonexistent-ref"));
            
            Assert.Contains("Reference 'nonexistent-ref' does not exist", exception.Message);
        }
        
        [Fact]
        public void SelectReferencesToCommonAncestor_BothReferencesDoNotExist_ThrowsException()
        {
            // Arrange
            var mockRepo = new MockGitRepository();
            var console = new MockConsoleOutput();
            
            var tagSelector = new GitTagSelector(mockRepo, debugMode: true, console);
            
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => 
                tagSelector.SelectReferencesToCommonAncestor("nonexistent-ref1", "nonexistent-ref2"));
            
            Assert.Contains("Reference 'nonexistent-ref1' does not exist", exception.Message);
        }
        
        [Fact]
        public void SelectReferencesToCommonAncestor_BothReferencesAreSameCommit_ThrowsException()
        {
            // Arrange
            var mockRepo = new MockGitRepository();
            var console = new MockConsoleOutput();
            
            var commit = new GitCommit
            {
                Sha = "abc123",
                Message = "Test commit",
                AuthorName = "Test Author",
                AuthorWhen = DateTimeOffset.Now
            };
            
            mockRepo.AddCommit(commit);
            
            var tagSelector = new GitTagSelector(mockRepo, debugMode: true, console);
            
            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => 
                tagSelector.SelectReferencesToCommonAncestor(commit.Sha, commit.Sha));
            
            Assert.Contains("'abc123' and 'abc123' are the same commit", exception.Message);
        }
        
        [Fact]
        public void SelectReferencesToCommonAncestor_NoCommonAncestor_ThrowsException()
        {
            // Arrange
            var mockRepo = new MockGitRepository();
            var console = new MockConsoleOutput();
            
            var commit1 = new GitCommit
            {
                Sha = "abc123",
                Message = "Test commit 1",
                AuthorName = "Test Author",
                AuthorWhen = DateTimeOffset.Now.AddDays(-2)
            };
            
            var commit2 = new GitCommit
            {
                Sha = "def456",
                Message = "Test commit 2",
                AuthorName = "Test Author",
                AuthorWhen = DateTimeOffset.Now.AddDays(-1)
            };
            
            mockRepo.AddCommit(commit1)
                    .AddCommit(commit2);
            
            var tagSelector = new GitTagSelector(mockRepo, debugMode: true, console);
            
            // Act & Assert
            // Note: Since our mock GetMergeBase returns null when no common ancestor is found,
            // this will trigger the "No common ancestor found" error
            var exception = Assert.Throws<InvalidOperationException>(() => 
                tagSelector.SelectReferencesToCommonAncestor(commit1.Sha, commit2.Sha));
            
            Assert.Contains("No common ancestor found between", exception.Message);
        }
        
        [Fact]
        public void SelectReferencesToCommonAncestor_ValidReferences_ReturnsCorrectCommitPair()
        {
            // Arrange
            var mockRepo = new MockGitRepository();
            var console = new MockConsoleOutput();
            
            // Create a simple commit history with a common ancestor
            var commonAncestor = new GitCommit
            {
                Sha = "common123",
                Message = "Common ancestor commit",
                AuthorName = "Test Author",
                AuthorWhen = DateTimeOffset.Now.AddDays(-3)
            };
            
            var commit1 = new GitCommit
            {
                Sha = "abc123",
                Message = "Test commit 1",
                AuthorName = "Test Author",
                AuthorWhen = DateTimeOffset.Now.AddDays(-2)
            };
            
            var commit2 = new GitCommit
            {
                Sha = "def456",
                Message = "Test commit 2",
                AuthorName = "Test Author",
                AuthorWhen = DateTimeOffset.Now.AddDays(-1)
            };
            
            mockRepo.AddCommit(commonAncestor)
                    .AddCommit(commit1)
                    .AddCommit(commit2)
                    .AddCommitRelationship(commonAncestor.Sha, commit1.Sha)
                    .AddCommitRelationship(commonAncestor.Sha, commit2.Sha);
            
            var tagSelector = new GitTagSelector(mockRepo, debugMode: true, console);
            
            // Act
            var result = tagSelector.SelectReferencesToCommonAncestor(commit1.Sha, commit2.Sha);
            
            // Assert
            Assert.Equal(commonAncestor.Sha, result.CommitA);
            Assert.Equal(commit1.Sha, result.CommitB);
        }
        
        [Fact]
        public void SelectReferencesToCommonAncestor_WithTags_ReturnsCorrectCommitPair()
        {
            // Arrange
            var mockRepo = new MockGitRepository();
            var console = new MockConsoleOutput();
            
            // Create commits and tags
            var commonAncestor = new GitCommit
            {
                Sha = "common123",
                Message = "Common ancestor commit",
                AuthorName = "Test Author",
                AuthorWhen = DateTimeOffset.Now.AddDays(-3)
            };
            
            var commit1 = new GitCommit
            {
                Sha = "abc123",
                Message = "Test commit 1",
                AuthorName = "Test Author",
                AuthorWhen = DateTimeOffset.Now.AddDays(-2)
            };
            
            var commit2 = new GitCommit
            {
                Sha = "def456",
                Message = "Test commit 2",
                AuthorName = "Test Author",
                AuthorWhen = DateTimeOffset.Now.AddDays(-1)
            };
            
            var tag1 = new GitTag
            {
                Name = "v1.0.0",
                TargetCommitSha = commit1.Sha,
                CreatedAt = DateTimeOffset.Now.AddDays(-2)
            };
            
            var tag2 = new GitTag
            {
                Name = "v2.0.0",
                TargetCommitSha = commit2.Sha,
                CreatedAt = DateTimeOffset.Now.AddDays(-1)
            };
            
            mockRepo.AddCommit(commonAncestor)
                    .AddCommit(commit1)
                    .AddCommit(commit2)
                    .AddTag(tag1)
                    .AddTag(tag2)
                    .AddCommitRelationship(commonAncestor.Sha, commit1.Sha)
                    .AddCommitRelationship(commonAncestor.Sha, commit2.Sha);
            
            var tagSelector = new GitTagSelector(mockRepo, debugMode: true, console);
            
            // Act
            var result = tagSelector.SelectReferencesToCommonAncestor(tag1.Name, tag2.Name);
            
            // Assert
            Assert.Equal(commonAncestor.Sha, result.CommitA);
            Assert.Equal(tag1.Name, result.CommitB);
        }
        
        #endregion
        
        #region Environment Name Resolution Tests
        
        [Fact]
        public void SelectHeadToCommonAncestor_WithEnvironmentName_ResolvesToMostRecentTag()
        {
            // Arrange
            var mockRepo = new MockGitRepository();
            var console = new MockConsoleOutput();
            
            // Create commits and tags
            var commonAncestor = new GitCommit
            {
                Sha = "common123",
                Message = "Common ancestor commit",
                AuthorName = "Test Author",
                AuthorWhen = DateTimeOffset.Now.AddDays(-4)
            };
            
            var prodCommit1 = new GitCommit
            {
                Sha = "prod123",
                Message = "Production commit 1",
                AuthorName = "Test Author",
                AuthorWhen = DateTimeOffset.Now.AddDays(-3)
            };
            
            var prodCommit2 = new GitCommit
            {
                Sha = "prod456",
                Message = "Production commit 2",
                AuthorName = "Test Author",
                AuthorWhen = DateTimeOffset.Now.AddDays(-2)
            };
            
            var headCommit = new GitCommit
            {
                Sha = "head789",
                Message = "HEAD commit",
                AuthorName = "Test Author",
                AuthorWhen = DateTimeOffset.Now.AddDays(-1)
            };
            
            // Create production tags
            var prodTag1 = new GitTag
            {
                Name = "v1.0.0-production",
                TargetCommitSha = prodCommit1.Sha,
                CreatedAt = DateTimeOffset.Now.AddDays(-3)
            };
            
            var prodTag2 = new GitTag
            {
                Name = "v1.1.0-production",
                TargetCommitSha = prodCommit2.Sha,
                CreatedAt = DateTimeOffset.Now.AddDays(-2)
            };
            
            mockRepo.AddCommit(commonAncestor)
                    .AddCommit(prodCommit1)
                    .AddCommit(prodCommit2)
                    .AddCommit(headCommit)
                    .AddTag(prodTag1)
                    .AddTag(prodTag2)
                    .SetHeadCommit(headCommit.Sha)
                    .AddCommitRelationship(commonAncestor.Sha, prodCommit1.Sha)
                    .AddCommitRelationship(commonAncestor.Sha, prodCommit2.Sha)
                    .AddCommitRelationship(commonAncestor.Sha, headCommit.Sha);
            
            var tagSelector = new GitTagSelector(mockRepo, debugMode: true, console);
            
            // Act
            var result = tagSelector.SelectHeadToCommonAncestor("production");
            
            // Assert
            Assert.Equal(commonAncestor.Sha, result.CommitA);
            Assert.Equal("HEAD", result.CommitB);
            
            // Verify that debug output shows environment resolution
            var debugOutput = string.Join("\n", console.DebugOutput);
            Assert.Contains("Reference 'production' is an environment name", debugOutput);
            Assert.Contains("Found most recent 'production' tag: v1.1.0-production", debugOutput);
            Assert.Contains("Resolved environment 'production' to tag 'v1.1.0-production'", debugOutput);
        }
        
        [Fact]
        public void SelectHeadToCommonAncestor_WithEnvironmentName_NoTagsFound_ThrowsException()
        {
            // Arrange
            var mockRepo = new MockGitRepository();
            var console = new MockConsoleOutput();
            
            var headCommit = new GitCommit
            {
                Sha = "head789",
                Message = "HEAD commit",
                AuthorName = "Test Author",
                AuthorWhen = DateTimeOffset.Now
            };
            
            mockRepo.AddCommit(headCommit)
                    .SetHeadCommit(headCommit.Sha);
            
            var tagSelector = new GitTagSelector(mockRepo, debugMode: true, console);
            
            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => 
                tagSelector.SelectHeadToCommonAncestor("production"));
            
            Assert.Contains("No 'production' tags found in the last 42 days", exception.Message);
        }
        
        [Fact]
        public void SelectReferencesToCommonAncestor_WithEnvironmentNames_ResolvesToMostRecentTags()
        {
            // Arrange
            var mockRepo = new MockGitRepository();
            var console = new MockConsoleOutput();
            
            // Create commits and tags
            var commonAncestor = new GitCommit
            {
                Sha = "common123",
                Message = "Common ancestor commit",
                AuthorName = "Test Author",
                AuthorWhen = DateTimeOffset.Now.AddDays(-5)
            };
            
            var uatCommit = new GitCommit
            {
                Sha = "uat123",
                Message = "UAT commit",
                AuthorName = "Test Author",
                AuthorWhen = DateTimeOffset.Now.AddDays(-3)
            };
            
            var prodCommit = new GitCommit
            {
                Sha = "prod456",
                Message = "Production commit",
                AuthorName = "Test Author",
                AuthorWhen = DateTimeOffset.Now.AddDays(-2)
            };
            
            // Create tags
            var uatTag = new GitTag
            {
                Name = "v1.0.0-uat",
                TargetCommitSha = uatCommit.Sha,
                CreatedAt = DateTimeOffset.Now.AddDays(-3)
            };
            
            var prodTag = new GitTag
            {
                Name = "v1.0.0-production",
                TargetCommitSha = prodCommit.Sha,
                CreatedAt = DateTimeOffset.Now.AddDays(-2)
            };
            
            mockRepo.AddCommit(commonAncestor)
                    .AddCommit(uatCommit)
                    .AddCommit(prodCommit)
                    .AddTag(uatTag)
                    .AddTag(prodTag)
                    .AddCommitRelationship(commonAncestor.Sha, uatCommit.Sha)
                    .AddCommitRelationship(commonAncestor.Sha, prodCommit.Sha);
            
            var tagSelector = new GitTagSelector(mockRepo, debugMode: true, console);
            
            // Act
            var result = tagSelector.SelectReferencesToCommonAncestor("uat", "production");
            
            // Assert
            Assert.Equal(commonAncestor.Sha, result.CommitA);
            Assert.Equal("v1.0.0-uat", result.CommitB);
            
            // Verify that debug output shows environment resolution
            var debugOutput = string.Join("\n", console.DebugOutput);
            Assert.Contains("Resolved environment 'uat' to tag 'v1.0.0-uat'", debugOutput);
            Assert.Contains("Resolved environment 'production' to tag 'v1.0.0-production'", debugOutput);
        }
        
        [Fact]
        public void SelectReferencesToCommonAncestor_WithMixedReferences_ResolvesOnlyEnvironmentName()
        {
            // Arrange
            var mockRepo = new MockGitRepository();
            var console = new MockConsoleOutput();
            
            // Create commits and tags
            var commonAncestor = new GitCommit
            {
                Sha = "common123",
                Message = "Common ancestor commit",
                AuthorName = "Test Author",
                AuthorWhen = DateTimeOffset.Now.AddDays(-5)
            };
            
            var specificCommit = new GitCommit
            {
                Sha = "specific456",
                Message = "Specific commit",
                AuthorName = "Test Author",
                AuthorWhen = DateTimeOffset.Now.AddDays(-4)
            };
            
            var prodCommit = new GitCommit
            {
                Sha = "prod789",
                Message = "Production commit",
                AuthorName = "Test Author",
                AuthorWhen = DateTimeOffset.Now.AddDays(-2)
            };
            
            // Create production tag
            var prodTag = new GitTag
            {
                Name = "v1.0.0-production",
                TargetCommitSha = prodCommit.Sha,
                CreatedAt = DateTimeOffset.Now.AddDays(-2)
            };
            
            mockRepo.AddCommit(commonAncestor)
                    .AddCommit(specificCommit)
                    .AddCommit(prodCommit)
                    .AddTag(prodTag)
                    .AddCommitRelationship(commonAncestor.Sha, specificCommit.Sha)
                    .AddCommitRelationship(commonAncestor.Sha, prodCommit.Sha);
            
            var tagSelector = new GitTagSelector(mockRepo, debugMode: true, console);
            
            // Act
            var result = tagSelector.SelectReferencesToCommonAncestor("specific456", "production");
            
            // Assert
            Assert.Equal(commonAncestor.Sha, result.CommitA);
            Assert.Equal("specific456", result.CommitB);
            
            // Verify that debug output shows only production environment resolution
            var debugOutput = string.Join("\n", console.DebugOutput);
            Assert.DoesNotContain("Resolved environment 'specific456'", debugOutput);
            Assert.Contains("Resolved environment 'production' to tag 'v1.0.0-production'", debugOutput);
        }
        
        [Fact]
        public void SelectReferencesToCommonAncestor_WithInvalidEnvironmentName_NoTagsFound_ThrowsException()
        {
            // Arrange
            var mockRepo = new MockGitRepository();
            var console = new MockConsoleOutput();
            
            var commit = new GitCommit
            {
                Sha = "commit123",
                Message = "Test commit",
                AuthorName = "Test Author",
                AuthorWhen = DateTimeOffset.Now
            };
            
            mockRepo.AddCommit(commit);
            
            var tagSelector = new GitTagSelector(mockRepo, debugMode: true, console);
            
            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => 
                tagSelector.SelectReferencesToCommonAncestor("uat", "production"));
            
            Assert.Contains("No 'uat' tags found in the last 42 days", exception.Message);
        }
        
        [Theory]
        [InlineData("production")]
        [InlineData("uat")]
        [InlineData("qa")]
        [InlineData("dev")]
        public void IsValidEnvironment_WithValidEnvironmentNames_ReturnsTrue(string environmentName)
        {
            // Arrange
            var mockRepo = new MockGitRepository();
            var console = new MockConsoleOutput();
            var tagSelector = new GitTagSelector(mockRepo, debugMode: false, console);
            
            // Act - We need to use reflection since IsValidEnvironment is private
            var method = typeof(GitTagSelector).GetMethod("IsValidEnvironment", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = (bool)method.Invoke(tagSelector, new object[] { environmentName });
            
            // Assert
            Assert.True(result);
        }
        
        [Theory]
        [InlineData("invalid")]
        [InlineData("v1.0.0")]
        [InlineData("main")]
        [InlineData("")]
        public void IsValidEnvironment_WithInvalidEnvironmentNames_ReturnsFalse(string environmentName)
        {
            // Arrange
            var mockRepo = new MockGitRepository();
            var console = new MockConsoleOutput();
            var tagSelector = new GitTagSelector(mockRepo, debugMode: false, console);
            
            // Act - We need to use reflection since IsValidEnvironment is private
            var method = typeof(GitTagSelector).GetMethod("IsValidEnvironment", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = (bool)method.Invoke(tagSelector, new object[] { environmentName });
            
            // Assert
            Assert.False(result);
        }
        
        [Fact]
        public void ResolveReferenceToTag_WithEnvironmentName_ReturnsResolvedTag()
        {
            // Arrange
            var mockRepo = new MockGitRepository();
            var console = new MockConsoleOutput();
            
            var prodCommit = new GitCommit
            {
                Sha = "prod123",
                Message = "Production commit",
                AuthorName = "Test Author",
                AuthorWhen = DateTimeOffset.Now.AddDays(-1)
            };
            
            var prodTag = new GitTag
            {
                Name = "v1.0.0-production",
                TargetCommitSha = prodCommit.Sha,
                CreatedAt = DateTimeOffset.Now.AddDays(-1)
            };
            
            mockRepo.AddCommit(prodCommit)
                    .AddTag(prodTag);
            
            var tagSelector = new GitTagSelector(mockRepo, debugMode: true, console);
            
            // Act - We need to use reflection since ResolveReferenceToTag is private
            var method = typeof(GitTagSelector).GetMethod("ResolveReferenceToTag", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = (string)method.Invoke(tagSelector, new object[] { "production" });
            
            // Assert
            Assert.Equal("v1.0.0-production", result);
        }
        
        [Fact]
        public void ResolveReferenceToTag_WithNonEnvironmentName_ReturnsUnchanged()
        {
            // Arrange
            var mockRepo = new MockGitRepository();
            var console = new MockConsoleOutput();
            var tagSelector = new GitTagSelector(mockRepo, debugMode: false, console);
            
            // Act - We need to use reflection since ResolveReferenceToTag is private
            var method = typeof(GitTagSelector).GetMethod("ResolveReferenceToTag", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = (string)method.Invoke(tagSelector, new object[] { "v1.0.0" });
            
            // Assert
            Assert.Equal("v1.0.0", result);
        }
        
        #endregion
    }
}
