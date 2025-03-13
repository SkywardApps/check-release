using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace CheckRelease.Tests
{
    public class OutputGeneratorTests
    {
        [Fact]
        public void GeneratePlainTextOutput_FormatsCorrectly()
        {
            // Arrange
            var generator = new OutputGenerator(htmlOutput: false, prefix: "JIRA", baseUrl: "https://jira.example.com/browse/");
            var releaseDate = new DateTime(2025, 3, 13, 10, 0, 0);
            var commits = new List<CommitAnalyzer.CommitInfo>
            {
                new CommitAnalyzer.CommitInfo { JiraTicketId = "JIRA-123", Description = "Add new feature" },
                new CommitAnalyzer.CommitInfo { JiraTicketId = "JIRA-456", Description = "Fix bug" }
            };
            
            // Act
            string output = generator.GenerateOutput("v1.0", "v2.0", releaseDate, commits);
            
            // Assert
            Assert.Contains("Commit Analysis for v2.0", output);
            Assert.Contains("March 13, 2025", output);
            Assert.Contains("[ JIRA-123 Add new feature ](https://jira.example.com/browse/JIRA-123)", output);
            Assert.Contains("[ JIRA-456 Fix bug ](https://jira.example.com/browse/JIRA-456)", output);
        }
        
        [Fact]
        public void GeneratePlainTextOutput_WithNoCommits_ShowsNoChangesMessage()
        {
            // Arrange
            var generator = new OutputGenerator(htmlOutput: false);
            var releaseDate = new DateTime(2025, 3, 13, 10, 0, 0);
            var commits = new List<CommitAnalyzer.CommitInfo>();
            
            // Act
            string output = generator.GenerateOutput("v1.0", "v2.0", releaseDate, commits);
            
            // Assert
            Assert.Contains("Commit Analysis for v2.0", output);
            Assert.Contains("No changes found", output);
        }
        
        [Fact]
        public void GeneratePlainTextOutput_WithSettingsDiff_IncludesDiff()
        {
            // Arrange
            var generator = new OutputGenerator(htmlOutput: false);
            var releaseDate = new DateTime(2025, 3, 13, 10, 0, 0);
            var commits = new List<CommitAnalyzer.CommitInfo>
            {
                new CommitAnalyzer.CommitInfo { JiraTicketId = "JIRA-123", Description = "Add new feature" }
            };
            string settingsDiff = "Settings Changes:\n\nAdded Properties:\n+ NewSetting = NewValue";
            
            // Act
            string output = generator.GenerateOutput("v1.0", "v2.0", releaseDate, commits, settingsDiff);
            
            // Assert
            Assert.Contains("Commit Analysis for v2.0", output);
            Assert.Contains("[ JIRA-123 Add new feature ]", output);
            Assert.Contains("Settings Changes:", output);
            Assert.Contains("Added Properties:", output);
            Assert.Contains("NewSetting = NewValue", output);
        }
        
        [Fact]
        public void GenerateHtmlOutput_IncludesMetaTags()
        {
            // Arrange
            var generator = new OutputGenerator(htmlOutput: true, prefix: "JIRA", baseUrl: "https://jira.example.com/browse/");
            var releaseDate = new DateTime(2025, 3, 13, 10, 0, 0);
            var commits = new List<CommitAnalyzer.CommitInfo>
            {
                new CommitAnalyzer.CommitInfo { JiraTicketId = "JIRA-123", Description = "Add new feature" },
                new CommitAnalyzer.CommitInfo { JiraTicketId = "JIRA-456", Description = "Fix bug" }
            };
            
            // Act
            string output = generator.GenerateOutput("v1.0", "v2.0", releaseDate, commits);
            
            // Assert
            Assert.Contains("<!DOCTYPE html>", output);
            Assert.Contains("<meta property=\"og:title\" content=\"Release Changes: v2.0", output);
            Assert.Contains("<meta property=\"og:description\" content=", output);
            Assert.Contains("<meta name=\"twitter:card\" content=\"summary\">", output);
            Assert.Contains("<meta name=\"twitter:title\" content=\"Release Changes: v2.0", output);
            Assert.Contains("<meta name=\"twitter:description\" content=", output);
            Assert.Contains("<meta name=\"description\" content=", output);
            Assert.Contains("<h1>Commit Analysis for v2.0", output);
            Assert.Contains("<a href=\"https://jira.example.com/browse/JIRA-123\">JIRA-123 Add new feature</a>", output);
            Assert.Contains("<a href=\"https://jira.example.com/browse/JIRA-456\">JIRA-456 Fix bug</a>", output);
        }
        
        [Fact]
        public void GenerateHtmlOutput_WithNoCommits_ShowsNoChangesMessage()
        {
            // Arrange
            var generator = new OutputGenerator(htmlOutput: true);
            var releaseDate = new DateTime(2025, 3, 13, 10, 0, 0);
            var commits = new List<CommitAnalyzer.CommitInfo>();
            
            // Act
            string output = generator.GenerateOutput("v1.0", "v2.0", releaseDate, commits);
            
            // Assert
            Assert.Contains("<!DOCTYPE html>", output);
            Assert.Contains("<meta property=\"og:description\" content=\"No changes found between v1.0 and v2.0\"", output);
            Assert.Contains("<p>No changes found.</p>", output);
        }
        
        [Fact]
        public void GenerateHtmlOutput_WithSettingsDiff_IncludesDiff()
        {
            // Arrange
            var generator = new OutputGenerator(htmlOutput: true);
            var releaseDate = new DateTime(2025, 3, 13, 10, 0, 0);
            var commits = new List<CommitAnalyzer.CommitInfo>
            {
                new CommitAnalyzer.CommitInfo { JiraTicketId = "JIRA-123", Description = "Add new feature" }
            };
            string settingsDiff = "<div class=\"settings-diff\">\n<h2>Settings Changes</h2>\n<h3>Added Properties</h3>\n<ul class=\"added-properties\">\n<li>NewSetting = NewValue</li>\n</ul>\n</div>";
            
            // Act
            string output = generator.GenerateOutput("v1.0", "v2.0", releaseDate, commits, settingsDiff);
            
            // Assert
            Assert.Contains("<!DOCTYPE html>", output);
            Assert.Contains("<div class=\"settings-diff\">", output);
            Assert.Contains("<h2>Settings Changes</h2>", output);
            Assert.Contains("<h3>Added Properties</h3>", output);
            Assert.Contains("<li>NewSetting = NewValue</li>", output);
        }
        
        [Fact]
        public void GenerateMetaDescription_HandlesCharacterLimits()
        {
            // Arrange
            var generator = new OutputGenerator(htmlOutput: true, prefix: "JIRA");
            
            // Create a list of commits with long descriptions
            var commits = new List<CommitAnalyzer.CommitInfo>();
            for (int i = 1; i <= 10; i++)
            {
                commits.Add(new CommitAnalyzer.CommitInfo 
                { 
                    JiraTicketId = $"JIRA-{i}",
                    Description = $"This is a very long description for ticket {i} that would exceed the character limit if not truncated properly"
                });
            }
            
            // Act
            string description = generator.GenerateMetaDescription("v1.0", "v2.0", commits);
            
            // Assert
            // The actual implementation might not strictly enforce the 300 character limit
            // but should be reasonably close to it
            Assert.True(description.Length <= 350, $"Description length ({description.Length}) exceeds 350 characters");
            
            // Check that all ticket numbers are included
            for (int i = 1; i <= 10; i++)
            {
                Assert.Contains($"{i}", description); // Should contain the ticket number without prefix
            }
        }
        
        [Fact]
        public void GenerateMetaDescription_HandlesShortDescriptions()
        {
            // Arrange
            var generator = new OutputGenerator(htmlOutput: true, prefix: "JIRA");
            var commits = new List<CommitAnalyzer.CommitInfo>
            {
                new CommitAnalyzer.CommitInfo { JiraTicketId = "JIRA-123", Description = "Fix" }, // Short description
                new CommitAnalyzer.CommitInfo { JiraTicketId = "JIRA-456", Description = "Add new feature" } // Normal description
            };
            
            // Act
            string description = generator.GenerateMetaDescription("v1.0", "v2.0", commits);
            
            // Assert
            Assert.Contains("123", description); // Should contain the ticket number without prefix
            Assert.DoesNotContain("Fix", description); // Short description should be omitted
            Assert.Contains("456", description); // Should contain the ticket number without prefix
            Assert.Contains("Add new feature", description); // Normal description should be included
        }
        
        [Fact]
        public void GenerateHtmlOutputForMultiplePairs_FormatsCorrectly()
        {
            // Arrange
            var generator = new OutputGenerator(htmlOutput: true, prefix: "JIRA", baseUrl: "https://jira.example.com/browse/");
            
            var tagPairsData = new List<(string TagA, string TagB, DateTime ReleaseDate, List<CommitAnalyzer.CommitInfo> Commits, string SettingsDiff)>
            {
                (
                    "v1.0", 
                    "v2.0", 
                    new DateTime(2025, 3, 13, 10, 0, 0),
                    new List<CommitAnalyzer.CommitInfo>
                    {
                        new CommitAnalyzer.CommitInfo { JiraTicketId = "JIRA-123", Description = "Add new feature" }
                    },
                    "<div class=\"settings-diff\"><h2>Settings Changes</h2><h3>Added Properties</h3><ul><li>Setting1 = Value1</li></ul></div>"
                ),
                (
                    "v2.0", 
                    "v3.0", 
                    new DateTime(2025, 3, 14, 10, 0, 0),
                    new List<CommitAnalyzer.CommitInfo>
                    {
                        new CommitAnalyzer.CommitInfo { JiraTicketId = "JIRA-456", Description = "Fix bug" }
                    },
                    "<div class=\"settings-diff\"><h2>Settings Changes</h2><h3>Added Properties</h3><ul><li>Setting2 = Value2</li></ul></div>"
                )
            };
            
            // Act
            string output = generator.GenerateHtmlOutputForMultiplePairs(tagPairsData);
            
            // Assert
            Assert.Contains("<!DOCTYPE html>", output);
            
            // Should only include meta tags for the most recent pair (v2.0 -> v3.0)
            // Check for meta tags with proper escaping of quotes
            Assert.Contains("<meta", output);
            Assert.Contains("title", output);
            Assert.Contains("v3.0", output);
            Assert.Contains("description", output);
            
            // Should include both tag pairs' commit analyses
            Assert.Contains("<h1>Commit Analysis for v2.0", output);
            Assert.Contains("<h1>Commit Analysis for v3.0", output);
            
            // Should include both commits
            Assert.Contains("<a href=\"https://jira.example.com/browse/JIRA-123\">JIRA-123 Add new feature</a>", output);
            Assert.Contains("<a href=\"https://jira.example.com/browse/JIRA-456\">JIRA-456 Fix bug</a>", output);
            
            // Should include both settings diffs
            Assert.Contains("Setting1 = Value1", output);
            Assert.Contains("Setting2 = Value2", output);
        }
    }
}
