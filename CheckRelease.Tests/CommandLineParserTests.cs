using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Xunit;
using CheckRelease.Interfaces;
using CheckRelease.Testing;

namespace CheckRelease.Tests
{
    public class CommandLineParserTests
    {
        private IConfiguration CreateMockConfiguration(Dictionary<string, string> settings)
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(settings)
                .Build();
        }
        
        [Fact]
        public void Parse_DirectMode_ReturnsParsedArgs()
        {
            // Arrange
            var config = CreateMockConfiguration(new Dictionary<string, string>
            {
                { "Prefix", "JIRA" },
                { "SpanDays", "42" },
                { "SettingsPath", "" }
            });
            
            var console = new MockConsoleOutput();
            var parser = new CommandLineParser(config, console);
            string[] args = new[] { "release-1.0.0-production", "release-2.0.0-production" };
            
            // Act
            var result = parser.Parse(args);
            
            // Assert
            Assert.Equal(2, result.Arguments.Count);
            Assert.Equal("release-1.0.0-production", result.Arguments[0]);
            Assert.Equal("release-2.0.0-production", result.Arguments[1]);
            Assert.False(result.HtmlOutput);
            Assert.False(result.SettingsDiff);
            Assert.Equal(string.Empty, result.SettingsPath);
            Assert.False(result.DebugMode);
            Assert.Equal(42, result.SpanDays);
            Assert.Equal("JIRA", result.Prefix);
        }
        
        [Fact]
        public void Parse_AutoMode_ReturnsParsedArgs()
        {
            // Arrange
            var config = CreateMockConfiguration(new Dictionary<string, string>
            {
                { "Prefix", "JIRA" },
                { "SpanDays", "42" },
                { "SettingsPath", "" }
            });
            
            var console = new MockConsoleOutput();
            var parser = new CommandLineParser(config, console);
            string[] args = new[] { "auto", "production" };
            
            // Act
            var result = parser.Parse(args);
            
            // Assert
            Assert.Equal(2, result.Arguments.Count);
            Assert.Equal("auto", result.Arguments[0]);
            Assert.Equal("production", result.Arguments[1]);
            Assert.False(result.HtmlOutput);
            Assert.False(result.SettingsDiff);
            Assert.Equal(string.Empty, result.SettingsPath);
            Assert.False(result.DebugMode);
            Assert.Equal(42, result.SpanDays);
            Assert.Equal("JIRA", result.Prefix);
        }
        
        [Fact]
        public void Parse_StreamMode_ReturnsParsedArgs()
        {
            // Arrange
            var config = CreateMockConfiguration(new Dictionary<string, string>
            {
                { "Prefix", "JIRA" },
                { "SpanDays", "42" },
                { "SettingsPath", "" }
            });
            
            var console = new MockConsoleOutput();
            var parser = new CommandLineParser(config, console);
            string[] args = new[] { "stream" };
            
            // Act
            var result = parser.Parse(args);
            
            // Assert
            Assert.Single(result.Arguments);
            Assert.Equal("stream", result.Arguments[0]);
            Assert.False(result.HtmlOutput);
            Assert.False(result.SettingsDiff);
            Assert.Equal(string.Empty, result.SettingsPath);
            Assert.False(result.DebugMode);
            Assert.Equal(42, result.SpanDays);
            Assert.Equal("JIRA", result.Prefix);
        }
        
        [Fact]
        public void Parse_OptionalFlags_SetsCorrectProperties()
        {
            // Arrange
            var config = CreateMockConfiguration(new Dictionary<string, string>
            {
                { "Prefix", "JIRA" },
                { "SpanDays", "42" },
                { "SettingsPath", "" }
            });
            
            var console = new MockConsoleOutput();
            var parser = new CommandLineParser(config, console);
            string[] args = new[] 
            { 
                "--html", 
                "--settings-diff=path/to/appsettings.json", 
                "--debug", 
                "--span", "30", 
                "--prefix", "CUSTOM", 
                "release-1.0.0-production", 
                "release-2.0.0-production" 
            };
            
            // Act
            var result = parser.Parse(args);
            
            // Assert
            Assert.Equal(2, result.Arguments.Count);
            Assert.Equal("release-1.0.0-production", result.Arguments[0]);
            Assert.Equal("release-2.0.0-production", result.Arguments[1]);
            Assert.True(result.HtmlOutput);
            Assert.True(result.SettingsDiff);
            Assert.Equal("path/to/appsettings.json", result.SettingsPath);
            Assert.True(result.DebugMode);
            Assert.Equal(30, result.SpanDays);
            Assert.Equal("CUSTOM", result.Prefix);
        }
        
        [Fact]
        public void Parse_NoOptionalArgs_UsesDefaults()
        {
            // Arrange
            var config = CreateMockConfiguration(new Dictionary<string, string>
            {
                { "Prefix", "JIRA" },
                { "SpanDays", "42" },
                { "SettingsPath", "default/path/appsettings.json" }
            });
            
            var console = new MockConsoleOutput();
            var parser = new CommandLineParser(config, console);
            string[] args = new[] { "release-1.0.0-production", "release-2.0.0-production" };
            
            // Act
            var result = parser.Parse(args);
            
            // Assert
            Assert.Equal(2, result.Arguments.Count);
            Assert.False(result.HtmlOutput);
            // The SettingsDiff property might be true if a default path is provided
            Assert.Equal("default/path/appsettings.json", result.SettingsPath);
            Assert.False(result.DebugMode);
            Assert.Equal(42, result.SpanDays);
            Assert.Equal("JIRA", result.Prefix);
        }
        
        [Fact]
        public void Validate_DirectMode_ReturnsTrue()
        {
            // Arrange
            var config = CreateMockConfiguration(new Dictionary<string, string>());
            var console = new MockConsoleOutput();
            var parser = new CommandLineParser(config, console);
            var options = new CommandLineParser.CommandLineOptions
            {
                Arguments = new List<string> { "release-1.0.0-production", "release-2.0.0-production" }
            };
            
            // Act
            bool isValid = parser.Validate(options);
            
            // Assert
            Assert.True(isValid);
        }
        
        [Fact]
        public void Validate_AutoMode_ReturnsTrue()
        {
            // Arrange
            var config = CreateMockConfiguration(new Dictionary<string, string>());
            var console = new MockConsoleOutput();
            var parser = new CommandLineParser(config, console);
            var options = new CommandLineParser.CommandLineOptions
            {
                Arguments = new List<string> { "auto", "production" }
            };
            
            // Act
            bool isValid = parser.Validate(options);
            
            // Assert
            Assert.True(isValid);
        }
        
        [Fact]
        public void Validate_StreamMode_ReturnsTrue()
        {
            // Arrange
            var config = CreateMockConfiguration(new Dictionary<string, string>());
            var console = new MockConsoleOutput();
            var parser = new CommandLineParser(config, console);
            var options = new CommandLineParser.CommandLineOptions
            {
                Arguments = new List<string> { "stream" }
            };
            
            // Act
            bool isValid = parser.Validate(options);
            
            // Assert
            Assert.True(isValid);
        }
        
        [Fact]
        public void Validate_TypeMode_ReturnsTrue()
        {
            // Arrange
            var config = CreateMockConfiguration(new Dictionary<string, string>());
            var console = new MockConsoleOutput();
            var parser = new CommandLineParser(config, console);
            var options = new CommandLineParser.CommandLineOptions
            {
                Arguments = new List<string> { "production" }
            };
            
            // Act
            bool isValid = parser.Validate(options);
            
            // Assert
            Assert.True(isValid);
        }
        
        [Fact]
        public void Validate_SingleTagMode_ReturnsTrue()
        {
            // Arrange
            var config = CreateMockConfiguration(new Dictionary<string, string>());
            var console = new MockConsoleOutput();
            var parser = new CommandLineParser(config, console);
            var options = new CommandLineParser.CommandLineOptions
            {
                Arguments = new List<string> { "release-1.0.0-production" }
            };
            
            // Act
            bool isValid = parser.Validate(options);
            
            // Assert
            Assert.True(isValid);
        }
        
        [Fact]
        public void Validate_NoArguments_ReturnsFalse()
        {
            // Arrange
            var config = CreateMockConfiguration(new Dictionary<string, string>());
            var console = new MockConsoleOutput();
            var parser = new CommandLineParser(config, console);
            var options = new CommandLineParser.CommandLineOptions
            {
                Arguments = new List<string>()
            };
            
            // Act
            bool isValid = parser.Validate(options);
            
            // Assert
            Assert.False(isValid);
            Assert.Contains(console.ErrorOutput, msg => msg.Contains("No tags or commands specified"));
        }
        
        [Fact]
        public void Validate_InvalidSingleArgument_ReturnsFalse()
        {
            // Arrange
            var config = CreateMockConfiguration(new Dictionary<string, string>());
            var console = new MockConsoleOutput();
            var parser = new CommandLineParser(config, console);
            var options = new CommandLineParser.CommandLineOptions
            {
                Arguments = new List<string> { "invalid-argument" }
            };
            
            // Act
            bool isValid = parser.Validate(options);
            
            // Assert
            Assert.False(isValid);
            Assert.Contains(console.ErrorOutput, msg => msg.Contains("Invalid argument"));
        }
        
        [Fact]
        public void Validate_InvalidAutoModeType_ReturnsFalse()
        {
            // Arrange
            var config = CreateMockConfiguration(new Dictionary<string, string>());
            var console = new MockConsoleOutput();
            var parser = new CommandLineParser(config, console);
            var options = new CommandLineParser.CommandLineOptions
            {
                Arguments = new List<string> { "auto", "invalid-type" }
            };
            
            // Act
            bool isValid = parser.Validate(options);
            
            // Assert
            Assert.False(isValid);
            Assert.Contains(console.ErrorOutput, msg => msg.Contains("Invalid type"));
        }
        
        [Fact]
        public void Validate_TooManyArguments_ReturnsFalse()
        {
            // Arrange
            var config = CreateMockConfiguration(new Dictionary<string, string>());
            var console = new MockConsoleOutput();
            var parser = new CommandLineParser(config, console);
            var options = new CommandLineParser.CommandLineOptions
            {
                Arguments = new List<string> { "arg1", "arg2", "arg3" }
            };
            
            // Act
            bool isValid = parser.Validate(options);
            
            // Assert
            Assert.False(isValid);
            Assert.Contains(console.ErrorOutput, msg => msg.Contains("Too many arguments"));
        }
    }
}
