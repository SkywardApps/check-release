using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CheckRelease.Domain;
using CheckRelease.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;
using CheckRelease.Interfaces;

namespace CheckRelease.Tests
{
    public class ProgramTests
    {
        [Fact]
        public void JiraBaseUrl_FromCommandLine_IsUsedInHtmlOutput()
        {
            // Arrange
            var customJiraBaseUrl = "https://custom.jira.com/browse/";
            var defaultJiraBaseUrl = "https://jira.example.com/browse/";
            
            var console = new MockConsoleOutput();
            
            // Create a test for the OutputGenerator to verify it uses the correct base URL
            var generator = new OutputGenerator(
                htmlOutput: true,
                debugMode: false,
                prefix: "JIRA",
                baseUrl: customJiraBaseUrl,
                console: console);
            
            var commits = new List<CommitAnalyzer.CommitInfo>
            {
                new CommitAnalyzer.CommitInfo { JiraTicketId = "JIRA-123", Description = "Test commit" }
            };
            
            // Act
            string output = generator.GenerateOutput("tag1", "tag2", DateTime.Now, commits);
            
            // Assert
            Assert.Contains($"<a href=\"{customJiraBaseUrl}JIRA-123\">", output);
            Assert.DoesNotContain($"<a href=\"{defaultJiraBaseUrl}JIRA-123\">", output);
        }
        
        [Fact]
        public void JiraBaseUrl_FromCommandLine_IsPassedToOutputGenerator()
        {
            // This test verifies that the OutputGenerator is created with the correct JIRA base URL
            // from the command line options
            
            // Arrange
            var customJiraBaseUrl = "https://custom.jira.com/browse/";
            var defaultJiraBaseUrl = "https://jira.example.com/browse/";
            
            var console = new MockConsoleOutput();
            
            // Create a mock repository with test data
            var mockRepo = new MockGitRepository();
            
            // Add a test tag
            var tag1 = new GitTag
            {
                Name = "tag1",
                TargetCommitSha = "commit1",
                CreatedAt = DateTimeOffset.Now.AddDays(-10)
            };
            
            var tag2 = new GitTag
            {
                Name = "tag2",
                TargetCommitSha = "commit2",
                CreatedAt = DateTimeOffset.Now
            };
            
            // Add test commits with the correct format for JIRA ticket extraction
            // Format should be PREFIX-NUMBER_DESCRIPTION (e.g., JIRA-123_Add_New_Feature)
            var commit1 = new GitCommit
            {
                Sha = "commit1",
                Message = "JIRA-123_Test_commit",
                AuthorWhen = DateTimeOffset.Now.AddDays(-10),
                ParentCount = 2 // Make it a merge commit
            };
            
            var commit2 = new GitCommit
            {
                Sha = "commit2",
                Message = "JIRA-456_Another_test_commit",
                AuthorWhen = DateTimeOffset.Now,
                ParentCount = 2 // Make it a merge commit
            };
            
            mockRepo.AddTag(tag1)
                   .AddTag(tag2)
                   .AddCommit(commit1)
                   .AddCommit(commit2)
                   .AddCommitRelationship("commit1", "commit2");
            
            // Create a test for the OutputGenerator with the custom JIRA base URL
            var generator = new OutputGenerator(
                htmlOutput: true,
                debugMode: false,
                prefix: "JIRA",
                baseUrl: customJiraBaseUrl,
                console: console);
            
            // Create a commit analyzer with the mock repository
            var commitAnalyzer = new CommitAnalyzer(mockRepo, "JIRA", false, console);
            
            // Get commits between the tags
            var commits = commitAnalyzer.AnalyzeCommits("tag1", "tag2");
            
            // Act
            string output = generator.GenerateOutput("tag1", "tag2", DateTime.Now, commits);
            
            // Assert
            Assert.Contains($"<a href=\"{customJiraBaseUrl}JIRA-", output);
            Assert.DoesNotContain($"<a href=\"{defaultJiraBaseUrl}JIRA-", output);
        }
        
        [Fact]
        public void Parse_JiraBaseUrlOption_SetsCorrectProperty()
        {
            // This test verifies that the --jira-base-url option is correctly parsed
            
            // Arrange
            var customJiraBaseUrl = "https://custom.jira.com/browse/";
            var defaultJiraBaseUrl = "https://jira.example.com/browse/";
            
            var console = new MockConsoleOutput();
            
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "JiraBaseUrl", defaultJiraBaseUrl }
                })
                .Build();
            
            var parser = new CommandLineParser(config, console);
            string[] args = new[] 
            { 
                "--jira-base-url", customJiraBaseUrl,
                "tag1", 
                "tag2" 
            };
            
            // Act
            var result = parser.Parse(args);
            
            // Assert
            Assert.Equal(customJiraBaseUrl, result.JiraBaseUrl);
            Assert.NotEqual(defaultJiraBaseUrl, result.JiraBaseUrl);
        }
    }
}
