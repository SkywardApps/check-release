using System;
using System.Collections.Generic;
using System.Linq;
using CheckRelease.Domain;
using CheckRelease.Testing;
using Xunit;

namespace CheckRelease.Tests
{
    public class CommitAnalyzerTests
    {
        [Fact]
        public void SimpleTest()
        {
            // A very simple test to verify xUnit is working
            Assert.True(true);
        }
        
        [Fact]
        public void AnalyzeCommits_ExtractsJiraTickets_FromMergeCommits()
        {
            // Arrange
            var mockRepo = new MockGitRepository();
            
            // Add test commits
            var commit1 = new GitCommit 
            { 
                Sha = "abc1234", 
                Message = "Merge branch 'feature/JIRA-123_Add_New_Feature'",
                AuthorName = "Test User",
                AuthorWhen = DateTimeOffset.Now.AddDays(-2),
                ParentCount = 2 // Merge commit
            };
            
            var commit2 = new GitCommit 
            { 
                Sha = "def5678", 
                Message = "Merge branch 'feature/JIRA-456_Fix_Bug'",
                AuthorName = "Test User",
                AuthorWhen = DateTimeOffset.Now.AddDays(-1),
                ParentCount = 2 // Merge commit
            };
            
            mockRepo.AddCommit(commit1).AddCommit(commit2);
            
            // Setup commit relationships
            mockRepo.AddCommitRelationship("abc1234", "def5678");
            
            // Add tags
            mockRepo.AddTag(new GitTag 
            { 
                Name = "v1.0", 
                TargetCommitSha = "abc1234",
                CreatedAt = DateTimeOffset.Now.AddDays(-2)
            });
            
            mockRepo.AddTag(new GitTag 
            { 
                Name = "v2.0", 
                TargetCommitSha = "def5678",
                CreatedAt = DateTimeOffset.Now.AddDays(-1)
            });
            
            var analyzer = new CommitAnalyzer(mockRepo, "JIRA");
            
            // Act
            var result = analyzer.AnalyzeCommits("v1.0", "v2.0");
            
            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains(result, c => c.JiraTicketId == "JIRA-123" && c.Description == "Add New Feature");
            Assert.Contains(result, c => c.JiraTicketId == "JIRA-456" && c.Description == "Fix Bug");
        }
        
        [Fact]
        public void AnalyzeCommits_SkipsCommits_WithFFlag()
        {
            // Arrange
            var mockRepo = new MockGitRepository();
            
            // Add test commits
            var commit1 = new GitCommit 
            { 
                Sha = "abc1234", 
                Message = "Merge branch 'feature/JIRA-123_Add_New_Feature'",
                AuthorName = "Test User",
                AuthorWhen = DateTimeOffset.Now.AddDays(-3),
                ParentCount = 2 // Merge commit
            };
            
            var commit2 = new GitCommit 
            { 
                Sha = "def5678", 
                Message = "Merge branch 'feature/JIRA-456_F_Fix_Bug'", // Contains _F_ flag
                AuthorName = "Test User",
                AuthorWhen = DateTimeOffset.Now.AddDays(-2),
                ParentCount = 2 // Merge commit
            };
            
            var commit3 = new GitCommit 
            { 
                Sha = "ghi9012", 
                Message = "Merge branch 'feature/JIRA-789_Add_Another_Feature'",
                AuthorName = "Test User",
                AuthorWhen = DateTimeOffset.Now.AddDays(-1),
                ParentCount = 2 // Merge commit
            };
            
            mockRepo.AddCommit(commit1).AddCommit(commit2).AddCommit(commit3);
            
            // Setup commit relationships
            mockRepo.AddCommitRelationship("abc1234", "def5678");
            mockRepo.AddCommitRelationship("def5678", "ghi9012");
            
            // Add tags
            mockRepo.AddTag(new GitTag 
            { 
                Name = "v1.0", 
                TargetCommitSha = "abc1234",
                CreatedAt = DateTimeOffset.Now.AddDays(-3)
            });
            
            mockRepo.AddTag(new GitTag 
            { 
                Name = "v2.0", 
                TargetCommitSha = "ghi9012",
                CreatedAt = DateTimeOffset.Now.AddDays(-1)
            });
            
            var analyzer = new CommitAnalyzer(mockRepo, "JIRA");
            
            // Act
            var result = analyzer.AnalyzeCommits("v1.0", "v2.0");
            
            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains(result, c => c.JiraTicketId == "JIRA-123" && c.Description == "Add New Feature");
            Assert.Contains(result, c => c.JiraTicketId == "JIRA-789" && c.Description == "Add Another Feature");
            Assert.DoesNotContain(result, c => c.JiraTicketId == "JIRA-456"); // Should be skipped due to _F_ flag
        }
        
        [Fact]
        public void AnalyzeCommits_HandlesSimpleBranchNames_WithoutTitle()
        {
            // Arrange
            var mockRepo = new MockGitRepository();
            
            // Add test commits with simple branch names (no title after ticket ID)
            var commit1 = new GitCommit 
            { 
                Sha = "abc1234", 
                Message = "Merge branch 'JIRA-4789'", // Simple branch name
                AuthorName = "Test User",
                AuthorWhen = DateTimeOffset.Now.AddDays(-2),
                ParentCount = 2 // Merge commit
            };
            
            var commit2 = new GitCommit 
            { 
                Sha = "def5678", 
                Message = "Merge branch 'JIRA-5678'", // Another simple branch name
                AuthorName = "Test User",
                AuthorWhen = DateTimeOffset.Now.AddDays(-1),
                ParentCount = 2 // Merge commit
            };
            
            mockRepo.AddCommit(commit1).AddCommit(commit2);
            
            // Setup commit relationships
            mockRepo.AddCommitRelationship("abc1234", "def5678");
            
            // Add tags
            mockRepo.AddTag(new GitTag 
            { 
                Name = "v1.0", 
                TargetCommitSha = "abc1234",
                CreatedAt = DateTimeOffset.Now.AddDays(-2)
            });
            
            mockRepo.AddTag(new GitTag 
            { 
                Name = "v2.0", 
                TargetCommitSha = "def5678",
                CreatedAt = DateTimeOffset.Now.AddDays(-1)
            });
            
            var analyzer = new CommitAnalyzer(mockRepo, "JIRA");
            
            // Act
            var result = analyzer.AnalyzeCommits("v1.0", "v2.0");
            
            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains(result, c => c.JiraTicketId == "JIRA-4789" && c.Description == "Ticket JIRA-4789");
            Assert.Contains(result, c => c.JiraTicketId == "JIRA-5678" && c.Description == "Ticket JIRA-5678");
        }
        
        [Fact]
        public void AnalyzeCommits_HandlesMixedBranchNames_SimpleAndWithTitle()
        {
            // Arrange
            var mockRepo = new MockGitRepository();
            
            // Add test commits with a mix of simple and titled branch names
            var commit1 = new GitCommit 
            { 
                Sha = "abc1234", 
                Message = "Merge branch 'ELUM-4789'", // Simple branch name
                AuthorName = "Test User",
                AuthorWhen = DateTimeOffset.Now.AddDays(-3),
                ParentCount = 2 // Merge commit
            };
            
            var commit2 = new GitCommit 
            { 
                Sha = "def5678", 
                Message = "Merge branch 'ELUM-5000__BUG_Fix_the_overflow'", // Branch with title
                AuthorName = "Test User",
                AuthorWhen = DateTimeOffset.Now.AddDays(-2),
                ParentCount = 2 // Merge commit
            };
            
            var commit3 = new GitCommit 
            { 
                Sha = "ghi9012", 
                Message = "Merge branch 'ELUM-5100'", // Another simple branch name
                AuthorName = "Test User",
                AuthorWhen = DateTimeOffset.Now.AddDays(-1),
                ParentCount = 2 // Merge commit
            };
            
            mockRepo.AddCommit(commit1).AddCommit(commit2).AddCommit(commit3);
            
            // Setup commit relationships
            mockRepo.AddCommitRelationship("abc1234", "def5678");
            mockRepo.AddCommitRelationship("def5678", "ghi9012");
            
            // Add tags
            mockRepo.AddTag(new GitTag 
            { 
                Name = "v1.0", 
                TargetCommitSha = "abc1234",
                CreatedAt = DateTimeOffset.Now.AddDays(-3)
            });
            
            mockRepo.AddTag(new GitTag 
            { 
                Name = "v2.0", 
                TargetCommitSha = "ghi9012",
                CreatedAt = DateTimeOffset.Now.AddDays(-1)
            });
            
            var analyzer = new CommitAnalyzer(mockRepo, "ELUM");
            
            // Act
            var result = analyzer.AnalyzeCommits("v1.0", "v2.0");
            
            // Assert
            Assert.Equal(3, result.Count);
            Assert.Contains(result, c => c.JiraTicketId == "ELUM-4789" && c.Description == "Ticket ELUM-4789");
            Assert.Contains(result, c => c.JiraTicketId == "ELUM-5000" && c.Description == "BUG Fix the overflow");
            Assert.Contains(result, c => c.JiraTicketId == "ELUM-5100" && c.Description == "Ticket ELUM-5100");
        }
    }
}
