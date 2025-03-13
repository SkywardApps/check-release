using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CheckRelease.Interfaces;

namespace CheckRelease
{
    /// <summary>
    /// Generates output in plain text or HTML format.
    /// </summary>
    public class OutputGenerator
    {
        private readonly bool _htmlOutput;
        private readonly bool _debugMode;
        private readonly string _baseUrl;
        private readonly string _prefix;
        private readonly IConsoleOutput _console;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="OutputGenerator"/> class.
        /// </summary>
        /// <param name="htmlOutput">Whether to generate HTML output.</param>
        /// <param name="debugMode">Whether debug mode is enabled.</param>
        /// <param name="prefix">The prefix for JIRA tickets in commit messages.</param>
        /// <param name="baseUrl">The base URL for JIRA tickets.</param>
        /// <param name="console">The console output interface.</param>
        public OutputGenerator(bool htmlOutput, bool debugMode = false, string prefix = "JIRA", string baseUrl = "https://jira.example.com/browse/", IConsoleOutput? console = null)
        {
            _htmlOutput = htmlOutput;
            _debugMode = debugMode;
            _prefix = prefix;
            _baseUrl = baseUrl;
            _console = console ?? new Adapters.ConsoleOutput(debugMode);
        }
        
        /// <summary>
        /// Generates output for the commit analysis.
        /// </summary>
        /// <param name="tagA">The older tag.</param>
        /// <param name="tagB">The newer tag.</param>
        /// <param name="releaseDate">The release date.</param>
        /// <param name="commits">The list of commits.</param>
        /// <param name="settingsDiff">The settings diff output, if any.</param>
        /// <returns>The generated output.</returns>
        public string GenerateOutput(string tagA, string tagB, DateTime releaseDate, List<CommitAnalyzer.CommitInfo> commits, string? settingsDiff = null)
        {
            if (_debugMode)
            {
                _console.WriteDebug($"Generating output for {tagA} -> {tagB}");
                _console.WriteDebug($"HTML output: {_htmlOutput}");
                _console.WriteDebug($"Found {commits.Count} commits");
            }
            
            if (_htmlOutput)
            {
                return GenerateHtmlOutput(tagA, tagB, releaseDate, commits, settingsDiff);
            }
            else
            {
                return GeneratePlainTextOutput(tagA, tagB, releaseDate, commits, settingsDiff);
            }
        }
        
        /// <summary>
        /// Generates plain text output.
        /// </summary>
        /// <param name="tagA">The older tag.</param>
        /// <param name="tagB">The newer tag.</param>
        /// <param name="releaseDate">The release date.</param>
        /// <param name="commits">The list of commits.</param>
        /// <param name="settingsDiff">The settings diff output, if any.</param>
        /// <returns>The generated plain text output.</returns>
        private string GeneratePlainTextOutput(string tagA, string tagB, DateTime releaseDate, List<CommitAnalyzer.CommitInfo> commits, string? settingsDiff = null)
        {
            var sb = new StringBuilder();
            
            // Add header
            sb.AppendLine($"Commit Analysis for {tagB} ({releaseDate:MMMM dd, yyyy hh:mm tt}):");
            
            // Add commits
            if (commits.Count > 0)
            {
                foreach (var commit in commits)
                {
                    // Format: [ JIRA-XXXX Description ](URL)
                    sb.AppendLine($"[ {commit.JiraTicketId} {commit.Description} ]({_baseUrl}{commit.JiraTicketId})");
                }
            }
            else
            {
                sb.AppendLine("No changes found.");
            }
            
            // Add settings diff if provided
            if (!string.IsNullOrEmpty(settingsDiff))
            {
                sb.AppendLine();
                sb.AppendLine(settingsDiff);
            }
            
            return sb.ToString();
        }
        
        /// <summary>
        /// Generates meta description for Slack unfurling.
        /// </summary>
        /// <param name="tagA">The older tag.</param>
        /// <param name="tagB">The newer tag.</param>
        /// <param name="commits">The list of commits.</param>
        /// <returns>The generated meta description.</returns>
        public string GenerateMetaDescription(string tagA, string tagB, List<CommitAnalyzer.CommitInfo> commits)
        {
            if (commits.Count == 0)
            {
                return $"No changes found between {tagA} and {tagB}";
            }
            
            // Calculate character limit per change based on total number of changes
            // Slack typically truncates at ~300 characters
            int totalLimit = 300;
            int numChanges = commits.Count;
            int separatorLength = 2; // ", " between changes
            
            // Calculate per-change limit, accounting for separators
            int perChangeLimit = (totalLimit - (separatorLength * (numChanges - 1))) / numChanges;
            
            // Format each change and combine into a single string
            var description = new StringBuilder();
            bool first = true;
            
            foreach (var commit in commits)
            {
                // Strip the prefix (e.g., "JIRA-") from the ticket ID
                string ticketNum = commit.JiraTicketId.Replace($"{_prefix}-", "");
                
                // Format the description
                string formattedText = commit.Description;
                
                // Truncate if necessary to fit within perChangeLimit
                if (formattedText.Length > perChangeLimit)
                {
                    formattedText = formattedText.Substring(0, perChangeLimit);
                }
                
                // Add to description - if description would be less than 12 chars, only show ticket number
                if (formattedText.Length < 12)
                {
                    // Description too short, only show ticket number
                    if (first)
                    {
                        description.Append(ticketNum);
                        first = false;
                    }
                    else
                    {
                        description.Append(", ").Append(ticketNum);
                    }
                }
                else
                {
                    // Description is 12+ chars, show ticket number and description
                    if (first)
                    {
                        description.Append(ticketNum).Append(" ").Append(formattedText);
                        first = false;
                    }
                    else
                    {
                        description.Append(", ").Append(ticketNum).Append(" ").Append(formattedText);
                    }
                }
            }
            
            return description.ToString();
        }
        
        /// <summary>
        /// Generates HTML output for multiple tag pairs with meta tags only from the most recent pair.
        /// </summary>
        /// <param name="tagPairsData">The list of tag pairs data.</param>
        /// <returns>The generated HTML output.</returns>
        public string GenerateHtmlOutputForMultiplePairs(List<(string TagA, string TagB, DateTime ReleaseDate, List<CommitAnalyzer.CommitInfo> Commits, string SettingsDiff)> tagPairsData)
        {
            if (_debugMode)
            {
                _console.WriteDebug($"Generating HTML output for {tagPairsData.Count} tag pairs");
            }
            
            var sb = new StringBuilder();
            
            // Get the most recent tag pair for meta tags (first in the list since tags are sorted newest first)
            var mostRecentPair = tagPairsData.First();
            string metaDescription = GenerateMetaDescription(mostRecentPair.TagA, mostRecentPair.TagB, mostRecentPair.Commits);
            
            // Start HTML document
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html>");
            sb.AppendLine("<head>");
            sb.AppendLine("    <meta charset=\"UTF-8\">");
            sb.AppendLine($"    <title>Release Changes: {mostRecentPair.TagB}</title>");
            
            // Add meta tags ONLY for the most recent tag pair
            sb.AppendLine("    <!-- Open Graph meta tags for Slack unfurling -->");
            sb.AppendLine($"    <meta property=\"og:title\" content=\"Release Changes: {mostRecentPair.TagB} ({mostRecentPair.ReleaseDate:MMMM dd, yyyy})\">");
            sb.AppendLine($"    <meta property=\"og:description\" content=\"{metaDescription}\">");
            sb.AppendLine("    <meta property=\"og:type\" content=\"website\">");
            
            // Add Twitter Card meta tags
            sb.AppendLine("    <!-- Twitter Card meta tags -->");
            sb.AppendLine("    <meta name=\"twitter:card\" content=\"summary\">");
            sb.AppendLine($"    <meta name=\"twitter:title\" content=\"Release Changes: {mostRecentPair.TagB} ({mostRecentPair.ReleaseDate:MMMM dd, yyyy})\">");
            sb.AppendLine($"    <meta name=\"twitter:description\" content=\"{metaDescription}\">");
            
            // Add standard description meta tag
            sb.AppendLine($"    <meta name=\"description\" content=\"{metaDescription}\">");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            
            // Add each tag pair's commit analysis
            foreach (var (tagA, tagB, releaseDate, commits, settingsDiff) in tagPairsData)
            {
                sb.AppendLine($"    <h1>Commit Analysis for {tagB} ({releaseDate:MMMM dd, yyyy hh:mm tt})</h1>");
                
                if (commits.Count > 0)
                {
                    sb.AppendLine("    <ul>");
                    foreach (var commit in commits)
                    {
                        sb.AppendLine($"        <li><a href=\"{_baseUrl}{commit.JiraTicketId}\">{commit.JiraTicketId} {commit.Description}</a></li>");
                    }
                    sb.AppendLine("    </ul>");
                }
                else
                {
                    sb.AppendLine("    <p>No changes found.</p>");
                }
                
                // Add settings diff for this tag pair if available
                if (!string.IsNullOrEmpty(settingsDiff))
                {
                    sb.AppendLine(settingsDiff);
                }
            }
            
            // End HTML document
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");
            
            return sb.ToString();
        }
        
        /// <summary>
        /// Generates HTML output with appropriate meta tags for Slack unfurling.
        /// </summary>
        /// <param name="tagA">The older tag.</param>
        /// <param name="tagB">The newer tag.</param>
        /// <param name="releaseDate">The release date.</param>
        /// <param name="commits">The list of commits.</param>
        /// <param name="settingsDiff">The settings diff output, if any.</param>
        /// <returns>The generated HTML output.</returns>
        public string GenerateHtmlOutput(string tagA, string tagB, DateTime releaseDate, List<CommitAnalyzer.CommitInfo> commits, string? settingsDiff = null)
        {
            if (_debugMode)
            {
                _console.WriteDebug($"Generating HTML output for {tagA} -> {tagB}");
            }
            
            var sb = new StringBuilder();
            
            // Generate meta description for Slack unfurling
            string metaDescription = GenerateMetaDescription(tagA, tagB, commits);
            
            // Start HTML document
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html>");
            sb.AppendLine("<head>");
            sb.AppendLine("    <meta charset=\"UTF-8\">");
            sb.AppendLine($"    <title>Release Changes: {tagB}</title>");
            
            // Add meta tags for Slack unfurling
            sb.AppendLine("    <!-- Open Graph meta tags for Slack unfurling -->");
            sb.AppendLine($"    <meta property=\"og:title\" content=\"Release Changes: {tagB} ({releaseDate:MMMM dd, yyyy})\">");
            sb.AppendLine($"    <meta property=\"og:description\" content=\"{metaDescription}\">");
            sb.AppendLine("    <meta property=\"og:type\" content=\"website\">");
            
            // Add Twitter Card meta tags for additional compatibility
            sb.AppendLine("    <!-- Twitter Card meta tags -->");
            sb.AppendLine("    <meta name=\"twitter:card\" content=\"summary\">");
            sb.AppendLine($"    <meta name=\"twitter:title\" content=\"Release Changes: {tagB} ({releaseDate:MMMM dd, yyyy})\">");
            sb.AppendLine($"    <meta name=\"twitter:description\" content=\"{metaDescription}\">");
            
            // Add standard description meta tag
            sb.AppendLine($"    <meta name=\"description\" content=\"{metaDescription}\">");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            
            // Add commit analysis
            sb.AppendLine($"    <h1>Commit Analysis for {tagB} ({releaseDate:MMMM dd, yyyy hh:mm tt})</h1>");
            
            if (commits.Count > 0)
            {
                sb.AppendLine("    <ul>");
                foreach (var commit in commits)
                {
                    sb.AppendLine($"        <li><a href=\"{_baseUrl}{commit.JiraTicketId}\">{commit.JiraTicketId} {commit.Description}</a></li>");
                }
                sb.AppendLine("    </ul>");
            }
            else
            {
                sb.AppendLine("    <p>No changes found.</p>");
            }
            
            // Add settings diff if provided
            if (!string.IsNullOrEmpty(settingsDiff))
            {
                sb.AppendLine(settingsDiff);
            }
            
            // End HTML document
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");
            
            return sb.ToString();
        }
    }
}
