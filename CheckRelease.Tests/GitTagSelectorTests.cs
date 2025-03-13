using System;
using System.Collections.Generic;
using System.Linq;
using CheckRelease.Domain;
using CheckRelease.Testing;
using Xunit;

namespace CheckRelease.Tests
{
    public class GitTagSelectorTests
    {
        [Fact]
        public void SelectTags_DirectMode_ReturnsSortedTags()
        {
            // Arrange
            var mockRepo = new MockGitRepository();
            
            // Add test tags
            mockRepo.AddTag(new GitTag 
            { 
                Name = "release-1.0.0-production", 
                TargetCommitSha = "abc1234",
                CreatedAt = DateTimeOffset.Now.AddDays(-10)
            });
            
            mockRepo.AddTag(new GitTag 
            { 
                Name = "release-2.0.0-production", 
                TargetCommitSha = "def5678",
                CreatedAt = DateTimeOffset.Now.AddDays(-5)
            });
            
            var selector = new GitTagSelector(mockRepo);
            
            // Act
            var (tagA, tagB) = selector.SelectTags(new List<string> { "release-2.0.0-production", "release-1.0.0-production" });
            
            // Assert
            Assert.Equal("release-1.0.0-production", tagA); // Older tag should be first
            Assert.Equal("release-2.0.0-production", tagB); // Newer tag should be second
        }
        
        [Fact]
        public void SelectTags_DirectMode_ThrowsWhenTagDoesNotExist()
        {
            // Arrange
            var mockRepo = new MockGitRepository();
            
            // Add test tag
            mockRepo.AddTag(new GitTag 
            { 
                Name = "release-1.0.0-production", 
                TargetCommitSha = "abc1234",
                CreatedAt = DateTimeOffset.Now.AddDays(-10)
            });
            
            var selector = new GitTagSelector(mockRepo);
            
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => 
                selector.SelectTags(new List<string> { "release-1.0.0-production", "non-existent-tag" }));
            
            Assert.Contains("non-existent-tag", exception.Message);
        }
        
        [Fact]
        public void SelectTags_AutoMode_ReturnsRecentTags()
        {
            // Arrange
            var mockRepo = new MockGitRepository();
            
            // Add test tags
            mockRepo.AddTag(new GitTag 
            { 
                Name = "release-1.0.0-production", 
                TargetCommitSha = "abc1234",
                CreatedAt = DateTimeOffset.Now.AddDays(-30)
            });
            
            mockRepo.AddTag(new GitTag 
            { 
                Name = "release-2.0.0-production", 
                TargetCommitSha = "def5678",
                CreatedAt = DateTimeOffset.Now.AddDays(-20)
            });
            
            mockRepo.AddTag(new GitTag 
            { 
                Name = "release-3.0.0-production", 
                TargetCommitSha = "ghi9012",
                CreatedAt = DateTimeOffset.Now.AddDays(-10)
            });
            
            var selector = new GitTagSelector(mockRepo);
            
            // Act
            var (tagA, tagB) = selector.SelectTags(new List<string> { "auto", "production" });
            
            // Assert
            Assert.Equal("release-2.0.0-production", tagA); // Second most recent
            Assert.Equal("release-3.0.0-production", tagB); // Most recent
        }
        
        [Fact]
        public void SelectTags_AutoMode_ThrowsWhenLessThanTwoTagsFound()
        {
            // Arrange
            var mockRepo = new MockGitRepository();
            
            // Add only one test tag
            mockRepo.AddTag(new GitTag 
            { 
                Name = "release-1.0.0-production", 
                TargetCommitSha = "abc1234",
                CreatedAt = DateTimeOffset.Now.AddDays(-10)
            });
            
            var selector = new GitTagSelector(mockRepo);
            
            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => 
                selector.SelectTags(new List<string> { "auto", "production" }));
            
            Assert.Contains("Less than two 'production' tags found", exception.Message);
        }
        
        [Fact]
        public void FindRecentTags_ReturnsTagsInDescendingOrder()
        {
            // Arrange
            var mockRepo = new MockGitRepository();
            
            // Add test tags
            mockRepo.AddTag(new GitTag 
            { 
                Name = "release-1.0.0-production", 
                TargetCommitSha = "abc1234",
                CreatedAt = DateTimeOffset.Now.AddDays(-30)
            });
            
            mockRepo.AddTag(new GitTag 
            { 
                Name = "release-2.0.0-production", 
                TargetCommitSha = "def5678",
                CreatedAt = DateTimeOffset.Now.AddDays(-20)
            });
            
            mockRepo.AddTag(new GitTag 
            { 
                Name = "release-3.0.0-production", 
                TargetCommitSha = "ghi9012",
                CreatedAt = DateTimeOffset.Now.AddDays(-10)
            });
            
            // Add a tag that's too old
            mockRepo.AddTag(new GitTag 
            { 
                Name = "release-0.5.0-production", 
                TargetCommitSha = "xyz9876",
                CreatedAt = DateTimeOffset.Now.AddDays(-50)
            });
            
            // Add a tag of different type
            mockRepo.AddTag(new GitTag 
            { 
                Name = "release-1.5.0-qa", 
                TargetCommitSha = "mno3456",
                CreatedAt = DateTimeOffset.Now.AddDays(-15)
            });
            
            var selector = new GitTagSelector(mockRepo);
            
            // Act
            var recentTags = selector.FindRecentTags("production", 40);
            
            // Assert
            Assert.Equal(3, recentTags.Count);
            Assert.Equal("release-3.0.0-production", recentTags[0]); // Most recent first
            Assert.Equal("release-2.0.0-production", recentTags[1]);
            Assert.Equal("release-1.0.0-production", recentTags[2]);
            Assert.DoesNotContain("release-0.5.0-production", recentTags); // Too old
            Assert.DoesNotContain("release-1.5.0-qa", recentTags); // Wrong type
        }
        
        [Fact]
        public void FindTagsBeforeDate_ReturnsTagsBeforeSpecifiedDate()
        {
            // Arrange
            var mockRepo = new MockGitRepository();
            var referenceDate = DateTimeOffset.Now.AddDays(-10);
            
            // Add test tags
            mockRepo.AddTag(new GitTag 
            { 
                Name = "release-1.0.0-production", 
                TargetCommitSha = "abc1234",
                CreatedAt = DateTimeOffset.Now.AddDays(-30)
            });
            
            mockRepo.AddTag(new GitTag 
            { 
                Name = "release-2.0.0-production", 
                TargetCommitSha = "def5678",
                CreatedAt = DateTimeOffset.Now.AddDays(-20)
            });
            
            // Add a tag after the reference date
            mockRepo.AddTag(new GitTag 
            { 
                Name = "release-3.0.0-production", 
                TargetCommitSha = "ghi9012",
                CreatedAt = DateTimeOffset.Now.AddDays(-5)
            });
            
            // Add a tag that's too old
            mockRepo.AddTag(new GitTag 
            { 
                Name = "release-0.5.0-production", 
                TargetCommitSha = "xyz9876",
                CreatedAt = DateTimeOffset.Now.AddDays(-50)
            });
            
            var selector = new GitTagSelector(mockRepo);
            
            // Act
            var tags = selector.FindTagsBeforeDate("production", referenceDate, 4);
            
            // Assert
            Assert.Equal(2, tags.Count);
            Assert.Equal("release-2.0.0-production", tags[0]); // Most recent first
            Assert.Equal("release-1.0.0-production", tags[1]);
            Assert.DoesNotContain("release-3.0.0-production", tags); // After reference date
            Assert.DoesNotContain("release-0.5.0-production", tags); // Too old
        }
        
        [Fact]
        public void SelectTagsAuto_ReturnsMultipleTagPairs()
        {
            // Arrange
            var mockRepo = new MockGitRepository();
            
            // Add test tags
            mockRepo.AddTag(new GitTag 
            { 
                Name = "release-1.0.0-production", 
                TargetCommitSha = "abc1234",
                CreatedAt = DateTimeOffset.Now.AddDays(-30)
            });
            
            mockRepo.AddTag(new GitTag 
            { 
                Name = "release-2.0.0-production", 
                TargetCommitSha = "def5678",
                CreatedAt = DateTimeOffset.Now.AddDays(-20)
            });
            
            mockRepo.AddTag(new GitTag 
            { 
                Name = "release-3.0.0-production", 
                TargetCommitSha = "ghi9012",
                CreatedAt = DateTimeOffset.Now.AddDays(-10)
            });
            
            var selector = new GitTagSelector(mockRepo);
            
            // Act
            var tagPairs = selector.SelectTagsAuto("production", 40);
            
            // Assert
            Assert.Equal(2, tagPairs.Count);
            
            // The order of pairs depends on how SelectTagsAuto is implemented
            // We just need to verify that we have the correct pairs, regardless of order
            Assert.Equal(2, tagPairs.Count);
            
            // Verify that we have both pairs (1.0.0 -> 2.0.0 and 2.0.0 -> 3.0.0)
            bool hasPair1 = tagPairs.Any(p => p.TagA == "release-1.0.0-production" && p.TagB == "release-2.0.0-production");
            bool hasPair2 = tagPairs.Any(p => p.TagA == "release-2.0.0-production" && p.TagB == "release-3.0.0-production");
            
            Assert.True(hasPair1, "Should contain pair release-1.0.0-production -> release-2.0.0-production");
            Assert.True(hasPair2, "Should contain pair release-2.0.0-production -> release-3.0.0-production");
        }
        
        [Fact]
        public void SelectStreamCommits_ReturnsHeadAndOldCommit()
        {
            // Arrange
            var mockRepo = new MockGitRepository();
            
            // Add test commits
            var oldCommit = new GitCommit 
            { 
                Sha = "abc1234", 
                Message = "Old commit",
                AuthorName = "Test User",
                AuthorWhen = DateTimeOffset.Now.AddDays(-30)
            };
            
            var midCommit = new GitCommit 
            { 
                Sha = "def5678", 
                Message = "Mid commit",
                AuthorName = "Test User",
                AuthorWhen = DateTimeOffset.Now.AddDays(-20)
            };
            
            var headCommit = new GitCommit 
            { 
                Sha = "ghi9012", 
                Message = "Head commit",
                AuthorName = "Test User",
                AuthorWhen = DateTimeOffset.Now.AddDays(-10)
            };
            
            mockRepo.AddCommit(oldCommit)
                   .AddCommit(midCommit)
                   .AddCommit(headCommit)
                   .SetHeadCommit("ghi9012");
            
            // Setup commit relationships
            mockRepo.AddCommitRelationship("abc1234", "def5678");
            mockRepo.AddCommitRelationship("def5678", "ghi9012");
            
            var selector = new GitTagSelector(mockRepo);
            
            // Act
            var (commitA, commitB) = selector.SelectStreamCommits(25); // Look back 25 days
            
            // Assert
            Assert.Equal("abc1234", commitA); // Old commit
            Assert.Equal("HEAD", commitB); // HEAD
        }
        
        [Fact]
        public void SelectStreamCommits_WithTagType_FiltersCommitsByType()
        {
            // Arrange
            var mockRepo = new MockGitRepository();
            
            // Add test commits
            var oldCommit = new GitCommit 
            { 
                Sha = "abc1234", 
                Message = "Old commit with production",
                AuthorName = "Test User",
                AuthorWhen = DateTimeOffset.Now.AddDays(-30)
            };
            
            var midCommit = new GitCommit 
            { 
                Sha = "def5678", 
                Message = "Mid commit with qa",
                AuthorName = "Test User",
                AuthorWhen = DateTimeOffset.Now.AddDays(-20)
            };
            
            var headCommit = new GitCommit 
            { 
                Sha = "ghi9012", 
                Message = "Head commit",
                AuthorName = "Test User",
                AuthorWhen = DateTimeOffset.Now.AddDays(-10)
            };
            
            mockRepo.AddCommit(oldCommit)
                   .AddCommit(midCommit)
                   .AddCommit(headCommit)
                   .SetHeadCommit("ghi9012");
            
            // Setup commit relationships
            mockRepo.AddCommitRelationship("abc1234", "def5678");
            mockRepo.AddCommitRelationship("def5678", "ghi9012");
            
            var selector = new GitTagSelector(mockRepo);
            
            // Act
            var (commitA, commitB) = selector.SelectStreamCommits(25, "production"); // Look back 25 days, filter by "production"
            
            // Assert
            Assert.Equal("abc1234", commitA); // Old commit with "production" in message
            Assert.Equal("HEAD", commitB); // HEAD
        }
    }
}
