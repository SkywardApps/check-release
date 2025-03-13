using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using CheckRelease.Domain;
using CheckRelease.Testing;
using Xunit;

namespace CheckRelease.Tests
{
    public class SettingsDiffGeneratorTests
    {
        private const string SettingsPath = "appsettings.json";
        
        [Fact]
        public void GenerateSettingsDiff_DetectsAddedProperties()
        {
            // Arrange
            var mockRepo = new MockGitRepository();
            
            // Create old settings JSON
            string oldJson = @"{
                ""Logging"": {
                    ""LogLevel"": {
                        ""Default"": ""Information"",
                        ""Microsoft"": ""Warning""
                    }
                },
                ""AllowedHosts"": ""*"",
                ""AppSettings"": {
                    ""Prefix"": ""JIRA"",
                    ""SpanDays"": 42
                }
            }";
            
            // Create new settings JSON with added property
            string newJson = @"{
                ""Logging"": {
                    ""LogLevel"": {
                        ""Default"": ""Information"",
                        ""Microsoft"": ""Warning""
                    }
                },
                ""AllowedHosts"": ""*"",
                ""AppSettings"": {
                    ""Prefix"": ""JIRA"",
                    ""SpanDays"": 42,
                    ""NewSetting"": ""NewValue""
                }
            }";
            
            // Add tags and file content
            mockRepo.AddTag(new GitTag 
            { 
                Name = "v1.0", 
                TargetCommitSha = "abc1234",
                CreatedAt = DateTimeOffset.Now.AddDays(-10)
            });
            
            mockRepo.AddTag(new GitTag 
            { 
                Name = "v2.0", 
                TargetCommitSha = "def5678",
                CreatedAt = DateTimeOffset.Now.AddDays(-5)
            });
            
            mockRepo.AddFileContent("v1.0", SettingsPath, oldJson);
            mockRepo.AddFileContent("v2.0", SettingsPath, newJson);
            
            var diffGenerator = new SettingsDiffGenerator(mockRepo, false, SettingsPath);
            
            // Act
            string diff = diffGenerator.GenerateSettingsDiff("v1.0", "v2.0", false);
            
            // Assert
            Assert.Contains("Added Properties:", diff);
            Assert.Contains("AppSettings__NewSetting = NewValue", diff);
            Assert.DoesNotContain("Removed Properties:", diff);
        }
        
        [Fact]
        public void GenerateSettingsDiff_DetectsRemovedProperties()
        {
            // Arrange
            var mockRepo = new MockGitRepository();
            
            // Create old settings JSON
            string oldJson = @"{
                ""Logging"": {
                    ""LogLevel"": {
                        ""Default"": ""Information"",
                        ""Microsoft"": ""Warning"",
                        ""System"": ""Error""
                    }
                },
                ""AllowedHosts"": ""*"",
                ""AppSettings"": {
                    ""Prefix"": ""JIRA"",
                    ""SpanDays"": 42,
                    ""OldSetting"": ""OldValue""
                }
            }";
            
            // Create new settings JSON with removed property
            string newJson = @"{
                ""Logging"": {
                    ""LogLevel"": {
                        ""Default"": ""Information"",
                        ""Microsoft"": ""Warning""
                    }
                },
                ""AllowedHosts"": ""*"",
                ""AppSettings"": {
                    ""Prefix"": ""JIRA"",
                    ""SpanDays"": 42
                }
            }";
            
            // Add tags and file content
            mockRepo.AddTag(new GitTag 
            { 
                Name = "v1.0", 
                TargetCommitSha = "abc1234",
                CreatedAt = DateTimeOffset.Now.AddDays(-10)
            });
            
            mockRepo.AddTag(new GitTag 
            { 
                Name = "v2.0", 
                TargetCommitSha = "def5678",
                CreatedAt = DateTimeOffset.Now.AddDays(-5)
            });
            
            mockRepo.AddFileContent("v1.0", SettingsPath, oldJson);
            mockRepo.AddFileContent("v2.0", SettingsPath, newJson);
            
            var diffGenerator = new SettingsDiffGenerator(mockRepo, false, SettingsPath);
            
            // Act
            string diff = diffGenerator.GenerateSettingsDiff("v1.0", "v2.0", false);
            
            // Assert
            Assert.Contains("Removed Properties:", diff);
            Assert.Contains("AppSettings__OldSetting = OldValue", diff);
            Assert.Contains("Logging__LogLevel__System = Error", diff);
            Assert.DoesNotContain("Added Properties:", diff);
        }
        
        [Fact]
        public void GenerateSettingsDiff_HandlesNestedObjects()
        {
            // Arrange
            var mockRepo = new MockGitRepository();
            
            // Create old settings JSON
            string oldJson = @"{
                ""NestedObject"": {
                    ""Level1"": {
                        ""Level2"": {
                            ""Value"": ""OldValue""
                        }
                    }
                }
            }";
            
            // Create new settings JSON with changed nested property
            string newJson = @"{
                ""NestedObject"": {
                    ""Level1"": {
                        ""Level2"": {
                            ""Value"": ""NewValue"",
                            ""NewProperty"": true
                        }
                    }
                }
            }";
            
            // Add tags and file content
            mockRepo.AddTag(new GitTag 
            { 
                Name = "v1.0", 
                TargetCommitSha = "abc1234",
                CreatedAt = DateTimeOffset.Now.AddDays(-10)
            });
            
            mockRepo.AddTag(new GitTag 
            { 
                Name = "v2.0", 
                TargetCommitSha = "def5678",
                CreatedAt = DateTimeOffset.Now.AddDays(-5)
            });
            
            mockRepo.AddFileContent("v1.0", SettingsPath, oldJson);
            mockRepo.AddFileContent("v2.0", SettingsPath, newJson);
            
            var diffGenerator = new SettingsDiffGenerator(mockRepo, false, SettingsPath);
            
            // Act
            string diff = diffGenerator.GenerateSettingsDiff("v1.0", "v2.0", false);
            
            // Assert
            Assert.Contains("Added Properties:", diff);
            Assert.Contains("NestedObject__Level1__Level2__NewProperty = true", diff);
        }
        
        [Fact]
        public void GenerateSettingsDiff_HandlesArrays()
        {
            // Arrange
            var mockRepo = new MockGitRepository();
            
            // Create old settings JSON
            string oldJson = @"{
                ""ArrayProperty"": [
                    {
                        ""Name"": ""Item1"",
                        ""Value"": 1
                    },
                    {
                        ""Name"": ""Item2"",
                        ""Value"": 2
                    }
                ]
            }";
            
            // Create new settings JSON with added array item
            string newJson = @"{
                ""ArrayProperty"": [
                    {
                        ""Name"": ""Item1"",
                        ""Value"": 1
                    },
                    {
                        ""Name"": ""Item2"",
                        ""Value"": 2
                    },
                    {
                        ""Name"": ""Item3"",
                        ""Value"": 3
                    }
                ]
            }";
            
            // Add tags and file content
            mockRepo.AddTag(new GitTag 
            { 
                Name = "v1.0", 
                TargetCommitSha = "abc1234",
                CreatedAt = DateTimeOffset.Now.AddDays(-10)
            });
            
            mockRepo.AddTag(new GitTag 
            { 
                Name = "v2.0", 
                TargetCommitSha = "def5678",
                CreatedAt = DateTimeOffset.Now.AddDays(-5)
            });
            
            mockRepo.AddFileContent("v1.0", SettingsPath, oldJson);
            mockRepo.AddFileContent("v2.0", SettingsPath, newJson);
            
            var diffGenerator = new SettingsDiffGenerator(mockRepo, false, SettingsPath);
            
            // Act
            string diff = diffGenerator.GenerateSettingsDiff("v1.0", "v2.0", false);
            
            // Assert
            Assert.Contains("Added Properties:", diff);
            Assert.Contains("ArrayProperty__2__Name = Item3", diff);
            Assert.Contains("ArrayProperty__2__Value = 3", diff);
        }
        
        [Fact]
        public void GenerateSettingsDiff_CensorsSensitiveValues()
        {
            // Arrange
            var mockRepo = new MockGitRepository();
            
            // Create old settings JSON
            string oldJson = @"{
                ""RegularValue"": ""NotSensitive"",
                ""Password"": ""SuperSecret123"",
                ""ApiToken"": ""abcdef123456"",
                ""SecretKey"": ""xyz789"",
                ""Secure"": ""sensitive-data""
            }";
            
            // Create new settings JSON with added sensitive property
            string newJson = @"{
                ""RegularValue"": ""NotSensitive"",
                ""Password"": ""SuperSecret123"",
                ""ApiToken"": ""abcdef123456"",
                ""SecretKey"": ""xyz789"",
                ""Secure"": ""sensitive-data"",
                ""NewPassword"": ""NewSecret456""
            }";
            
            // Add tags and file content
            mockRepo.AddTag(new GitTag 
            { 
                Name = "v1.0", 
                TargetCommitSha = "abc1234",
                CreatedAt = DateTimeOffset.Now.AddDays(-10)
            });
            
            mockRepo.AddTag(new GitTag 
            { 
                Name = "v2.0", 
                TargetCommitSha = "def5678",
                CreatedAt = DateTimeOffset.Now.AddDays(-5)
            });
            
            mockRepo.AddFileContent("v1.0", SettingsPath, oldJson);
            mockRepo.AddFileContent("v2.0", SettingsPath, newJson);
            
            var diffGenerator = new SettingsDiffGenerator(mockRepo, false, SettingsPath);
            
            // Act
            string diff = diffGenerator.GenerateSettingsDiff("v1.0", "v2.0", false);
            
            // Assert
            Assert.Contains("Added Properties:", diff);
            Assert.Contains("NewPassword = ", diff);
            
            // Check that sensitive values are censored
            Assert.DoesNotContain("SuperSecret123", diff);
            Assert.DoesNotContain("abcdef123456", diff);
            Assert.DoesNotContain("xyz789", diff);
            Assert.DoesNotContain("sensitive-data", diff);
            Assert.DoesNotContain("NewSecret456", diff);
            
            // But regular values are not censored (if they're in the diff)
            // Note: Regular values will only appear in the diff if they're added or removed
            Assert.Contains("NewPassword =", diff); // Just check that the key is there
            Assert.DoesNotContain("NewSecret456", diff); // But the sensitive value is censored
        }
        
        [Fact]
        public void GenerateSettingsDiff_HandlesHtmlOutput()
        {
            // Arrange
            var mockRepo = new MockGitRepository();
            
            // Create old settings JSON
            string oldJson = @"{
                ""AppSettings"": {
                    ""Prefix"": ""JIRA"",
                    ""SpanDays"": 42
                }
            }";
            
            // Create new settings JSON with added property
            string newJson = @"{
                ""AppSettings"": {
                    ""Prefix"": ""JIRA"",
                    ""SpanDays"": 42,
                    ""NewSetting"": ""NewValue""
                }
            }";
            
            // Add tags and file content
            mockRepo.AddTag(new GitTag 
            { 
                Name = "v1.0", 
                TargetCommitSha = "abc1234",
                CreatedAt = DateTimeOffset.Now.AddDays(-10)
            });
            
            mockRepo.AddTag(new GitTag 
            { 
                Name = "v2.0", 
                TargetCommitSha = "def5678",
                CreatedAt = DateTimeOffset.Now.AddDays(-5)
            });
            
            mockRepo.AddFileContent("v1.0", SettingsPath, oldJson);
            mockRepo.AddFileContent("v2.0", SettingsPath, newJson);
            
            var diffGenerator = new SettingsDiffGenerator(mockRepo, false, SettingsPath);
            
            // Act
            string diff = diffGenerator.GenerateSettingsDiff("v1.0", "v2.0", true);
            
            // Assert
            Assert.Contains("<div class=\"settings-diff\">", diff);
            Assert.Contains("<h2>Settings Changes</h2>", diff);
            Assert.Contains("<h3>Added Properties</h3>", diff);
            Assert.Contains("<ul class=\"added-properties\">", diff);
            Assert.Contains("<li>AppSettings__NewSetting = NewValue</li>", diff);
            Assert.Contains("</ul>", diff);
            Assert.Contains("</div>", diff);
        }
        
        [Fact]
        public void GenerateSettingsDiff_HandlesNoChanges()
        {
            // Arrange
            var mockRepo = new MockGitRepository();
            
            // Create identical settings JSON for both tags
            string json = @"{
                ""AppSettings"": {
                    ""Prefix"": ""JIRA"",
                    ""SpanDays"": 42
                }
            }";
            
            // Add tags and file content
            mockRepo.AddTag(new GitTag 
            { 
                Name = "v1.0", 
                TargetCommitSha = "abc1234",
                CreatedAt = DateTimeOffset.Now.AddDays(-10)
            });
            
            mockRepo.AddTag(new GitTag 
            { 
                Name = "v2.0", 
                TargetCommitSha = "def5678",
                CreatedAt = DateTimeOffset.Now.AddDays(-5)
            });
            
            mockRepo.AddFileContent("v1.0", SettingsPath, json);
            mockRepo.AddFileContent("v2.0", SettingsPath, json);
            
            var diffGenerator = new SettingsDiffGenerator(mockRepo, false, SettingsPath);
            
            // Act
            string diff = diffGenerator.GenerateSettingsDiff("v1.0", "v2.0", false);
            
            // Assert
            Assert.Contains("No settings changes detected.", diff);
            Assert.DoesNotContain("Added Properties:", diff);
            Assert.DoesNotContain("Removed Properties:", diff);
        }
        
        [Fact]
        public void GenerateSettingsDiff_HandlesFileNotFound()
        {
            // Arrange
            var mockRepo = new MockGitRepository();
            
            // Add tags but no file content
            mockRepo.AddTag(new GitTag 
            { 
                Name = "v1.0", 
                TargetCommitSha = "abc1234",
                CreatedAt = DateTimeOffset.Now.AddDays(-10)
            });
            
            mockRepo.AddTag(new GitTag 
            { 
                Name = "v2.0", 
                TargetCommitSha = "def5678",
                CreatedAt = DateTimeOffset.Now.AddDays(-5)
            });
            
            var diffGenerator = new SettingsDiffGenerator(mockRepo, false, SettingsPath);
            
            // Act
            string diff = diffGenerator.GenerateSettingsDiff("v1.0", "v2.0", false);
            
            // Assert
            Assert.Contains("Error: Could not extract appsettings.json from", diff);
        }
        
        [Fact]
        public void GenerateSettingsDiff_HandlesInvalidJson()
        {
            // Arrange
            var mockRepo = new MockGitRepository();
            
            // Create valid old settings JSON
            string oldJson = @"{
                ""AppSettings"": {
                    ""Prefix"": ""JIRA"",
                    ""SpanDays"": 42
                }
            }";
            
            // Create invalid new settings JSON
            string newJson = @"{
                ""AppSettings"": {
                    ""Prefix"": ""JIRA"",
                    ""SpanDays"": 42,
                    ""InvalidProperty"": // Missing value
                }
            }";
            
            // Add tags and file content
            mockRepo.AddTag(new GitTag 
            { 
                Name = "v1.0", 
                TargetCommitSha = "abc1234",
                CreatedAt = DateTimeOffset.Now.AddDays(-10)
            });
            
            mockRepo.AddTag(new GitTag 
            { 
                Name = "v2.0", 
                TargetCommitSha = "def5678",
                CreatedAt = DateTimeOffset.Now.AddDays(-5)
            });
            
            mockRepo.AddFileContent("v1.0", SettingsPath, oldJson);
            mockRepo.AddFileContent("v2.0", SettingsPath, newJson);
            
            var diffGenerator = new SettingsDiffGenerator(mockRepo, false, SettingsPath);
            
            // Act
            string diff = diffGenerator.GenerateSettingsDiff("v1.0", "v2.0", false);
            
            // Assert
            Assert.Contains("Error: Invalid JSON format in appsettings.json from", diff);
        }
    }
}
