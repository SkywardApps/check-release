using System;

namespace CheckRelease
{
    /// <summary>
    /// Represents the application settings that can be configured via appsettings.json or command line.
    /// </summary>
    public class AppSettings
    {
        /// <summary>
        /// Gets or sets the number of days to look back for tags.
        /// Default: 42 days (6 weeks)
        /// </summary>
        public int SpanDays { get; set; } = 42;

        /// <summary>
        /// Gets or sets the prefix for JIRA tickets in commit messages.
        /// Default: "JIRA"
        /// </summary>
        public string Prefix { get; set; } = "JIRA";

        /// <summary>
        /// Gets or sets the path to the appsettings.json file relative to the repository root.
        /// Default: ""
        /// </summary>
        public string SettingsPath { get; set; } = "";
        
        /// <summary>
        /// Gets or sets the base URL for JIRA tickets.
        /// Default: "https://jira.example.com/browse/"
        /// </summary>
        public string JiraBaseUrl { get; set; } = "https://jira.example.com/browse/";
    }
}
