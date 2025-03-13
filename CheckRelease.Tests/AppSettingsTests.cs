using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CheckRelease.Tests
{
    public class AppSettingsTests
    {
        private IConfiguration CreateMockConfiguration(Dictionary<string, string> settings)
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(settings)
                .Build();
        }
        
        [Fact]
        public void AppSettings_WithDefaults_SetsDefaultValues()
        {
            // Arrange & Act
            var settings = new AppSettings();
            
            // Assert
            Assert.Equal(42, settings.SpanDays);
            Assert.Equal("JIRA", settings.Prefix);
            Assert.Equal("", settings.SettingsPath);
            Assert.Equal("https://jira.example.com/browse/", settings.JiraBaseUrl);
        }
        
        [Fact]
        public void AppSettings_FromConfiguration_LoadsValues()
        {
            // Arrange
            var config = CreateMockConfiguration(new Dictionary<string, string>
            {
                { "SpanDays", "30" },
                { "Prefix", "CUSTOM" },
                { "SettingsPath", "path/to/settings.json" },
                { "JiraBaseUrl", "https://custom-jira.example.com/browse/" }
            });
            
            // Act
            var settings = new AppSettings();
            config.Bind(settings);
            
            // Assert
            Assert.Equal(30, settings.SpanDays);
            Assert.Equal("CUSTOM", settings.Prefix);
            Assert.Equal("path/to/settings.json", settings.SettingsPath);
            Assert.Equal("https://custom-jira.example.com/browse/", settings.JiraBaseUrl);
        }
        
        [Fact]
        public void AppSettings_WithPartialConfiguration_UsesMixOfDefaultsAndConfigValues()
        {
            // Arrange
            var config = CreateMockConfiguration(new Dictionary<string, string>
            {
                { "SpanDays", "30" },
                { "Prefix", "CUSTOM" }
                // SettingsPath and JiraBaseUrl not provided
            });
            
            // Act
            var settings = new AppSettings();
            config.Bind(settings);
            
            // Assert
            Assert.Equal(30, settings.SpanDays);
            Assert.Equal("CUSTOM", settings.Prefix);
            Assert.Equal("", settings.SettingsPath); // Default value
            Assert.Equal("https://jira.example.com/browse/", settings.JiraBaseUrl); // Default value
        }
        
        [Fact]
        public void AppSettings_WithInvalidValues_ThrowsException()
        {
            // Arrange
            var config = CreateMockConfiguration(new Dictionary<string, string>
            {
                { "SpanDays", "invalid" } // Not a valid integer
            });
            
            // Act & Assert
            var settings = new AppSettings();
            var exception = Assert.Throws<InvalidOperationException>(() => config.Bind(settings));
            
            // Verify the exception message contains information about the invalid value
            Assert.Contains("SpanDays", exception.Message);
            Assert.Contains("Int32", exception.Message);
        }
        
        [Fact]
        public void AppSettings_CommandLineOverridesConfiguration()
        {
            // Arrange
            var configSettings = new Dictionary<string, string>
            {
                { "SpanDays", "30" },
                { "Prefix", "CONFIG" },
                { "SettingsPath", "config/path/settings.json" },
                { "JiraBaseUrl", "https://config-jira.example.com/browse/" }
            };
            
            var config = CreateMockConfiguration(configSettings);
            
            // Create command line options that override some settings
            var commandLineOptions = new CommandLineParser.CommandLineOptions
            {
                SpanDays = 60,
                Prefix = "CMDLINE",
                SettingsPath = "cmdline/path/settings.json",
                // JiraBaseUrl not provided in command line options
            };
            
            // Act
            var settings = new AppSettings();
            config.Bind(settings);
            
            // Apply command line overrides
            settings.SpanDays = commandLineOptions.SpanDays;
            settings.Prefix = commandLineOptions.Prefix;
            settings.SettingsPath = commandLineOptions.SettingsPath;
            
            // Assert
            Assert.Equal(60, settings.SpanDays); // Command line value
            Assert.Equal("CMDLINE", settings.Prefix); // Command line value
            Assert.Equal("cmdline/path/settings.json", settings.SettingsPath); // Command line value
            Assert.Equal("https://config-jira.example.com/browse/", settings.JiraBaseUrl); // Config value (not overridden)
        }
        
        [Fact]
        public void AppSettings_WithEmptyConfiguration_UsesDefaults()
        {
            // Arrange
            var config = CreateMockConfiguration(new Dictionary<string, string>());
            
            // Act
            var settings = new AppSettings();
            config.Bind(settings);
            
            // Assert
            Assert.Equal(42, settings.SpanDays);
            Assert.Equal("JIRA", settings.Prefix);
            Assert.Equal("", settings.SettingsPath);
            Assert.Equal("https://jira.example.com/browse/", settings.JiraBaseUrl);
        }
    }
}
