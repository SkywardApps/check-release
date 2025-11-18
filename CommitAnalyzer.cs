using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CheckRelease.Domain;
using CheckRelease.Interfaces;

namespace CheckRelease
{
    /// <summary>
    /// Analyzes commits between two Git tags.
    /// </summary>
    public class CommitAnalyzer
    {
        private readonly IGitRepository _repository;
        private readonly bool _debugMode;
        private readonly string _prefix;
        private readonly IConsoleOutput _console;
        
        // Regular expression to match JIRA tickets in commit messages
        private readonly Regex _jiraTicketRegex;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="CommitAnalyzer"/> class.
        /// </summary>
        /// <param name="repository">The Git repository.</param>
        /// <param name="prefix">The prefix for JIRA tickets in commit messages.</param>
        /// <param name="debugMode">Whether debug mode is enabled.</param>
        public CommitAnalyzer(IGitRepository repository, string prefix, bool debugMode = false, IConsoleOutput? console = null)
        {
            _repository = repository;
            _debugMode = debugMode;
            _prefix = prefix;
            _console = console ?? new Adapters.ConsoleOutput(debugMode);
            
            // Create the regex pattern with the configurable prefix
            // This pattern supports both formats:
            // 1. Simple: JIRA-123 (just the ticket ID)
            // 2. With title: JIRA-123__Add_Feature (ticket ID followed by underscores and title)
            _jiraTicketRegex = new Regex($@"({_prefix}-[0-9]+)(?:_+([A-Za-z0-9_]+))?", RegexOptions.Compiled);
        }
        
        /// <summary>
        /// Analyzes commits between two tags.
        /// </summary>
        /// <param name="tagA">The first tag.</param>
        /// <param name="tagB">The second tag.</param>
        /// <returns>A list of commit information.</returns>
        public List<CommitInfo> AnalyzeCommits(string tagA, string tagB)
        {
            try
            {
                if (_debugMode)
                {
                    _console.WriteDebug($"Analyzing commits between tags: {tagA} -> {tagB}");
                }
                
                // Get all commits between the tags
                var allCommits = _repository.GetCommitsBetween(tagA, tagB).ToList();
                
                if (_debugMode)
                {
                    _console.WriteDebug($"Found {allCommits.Count} total commits between {tagA} and {tagB}");
                    
                    // If we found commits, show the first few
                    if (allCommits.Count > 0)
                    {
                        _console.WriteDebug("First few commits:");
                        foreach (var commit in allCommits.Take(Math.Min(5, allCommits.Count)))
                        {
                            _console.WriteDebug($"  {commit.Sha.Substring(0, 7)} - {commit.Message}");
                        }
                    }
                }
                
                // Filter for merge commits
                var mergeCommits = allCommits.Where(c => c.ParentCount > 1).ToList();
                
                if (_debugMode)
                {
                    _console.WriteDebug($"Found {mergeCommits.Count} merge commits");
                }
                
                // If no merge commits found but there are regular commits, use all commits
                var commits = mergeCommits.Count == 0 && allCommits.Count > 0 ? allCommits : mergeCommits;
                
                if (_debugMode)
                {
                    _console.WriteDebug($"Using {commits.Count} commits for analysis");
                }
                
                // Extract JIRA tickets from commit messages
                var result = new List<CommitInfo>();
                var processedTickets = new HashSet<string>(); // To avoid duplicates
                
                foreach (var commit in commits)
                {
                    var message = commit.Message;
                    
                    // Skip commits containing "_F_"
                    if (message.Contains("_F_"))
                    {
                        if (_debugMode)
                        {
                            _console.WriteDebug($"Skipping commit {commit.Sha.Substring(0, 7)} (contains _F_): {message}");
                        }
                        continue;
                    }
                    
                    // Extract JIRA tickets
                    var matches = _jiraTicketRegex.Matches(message);
                    foreach (Match match in matches)
                    {
                        var jiraId = match.Groups[1].Value;
                        var extraText = match.Groups[2].Value;
                        
                        // Format the description:
                        // If there's extra text (title), format it
                        // If there's no extra text (simple branch name), use "Ticket {ID}"
                        var formattedText = string.IsNullOrEmpty(extraText) 
                            ? $"Ticket {jiraId}" 
                            : FormatExtraText(extraText);
                        
                        // Skip if we've already processed this ticket
                        if (processedTickets.Contains(jiraId))
                        {
                            continue;
                        }
                        
                        processedTickets.Add(jiraId);
                        
                        var commitInfo = new CommitInfo
                        {
                            JiraTicketId = jiraId,
                            Description = formattedText,
                            CommitHash = commit.Sha
                        };
                        
                        result.Add(commitInfo);
                        
                        if (_debugMode)
                        {
                            _console.WriteDebug($"Found ticket: {jiraId} - {formattedText}");
                        }
                    }
                }
                
                // Sort by JIRA ticket ID
                result.Sort((a, b) => string.Compare(a.JiraTicketId, b.JiraTicketId, StringComparison.Ordinal));
                
                if (_debugMode)
                {
                    _console.WriteDebug($"Extracted {result.Count} unique JIRA tickets");
                }
                
                return result;
            }
            catch (ArgumentException ex)
            {
                _console.WriteError($"Invalid argument: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                _console.WriteError($"Unexpected error analyzing commits: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Formats extra text by replacing underscores with spaces and inserting spaces before uppercase letters.
        /// </summary>
        /// <param name="text">The text to format.</param>
        /// <returns>The formatted text.</returns>
        private string FormatExtraText(string text)
        {
            // Replace underscores with spaces
            string result = text.Replace('_', ' ');
            
            // Insert space before uppercase letters following lowercase letters
            result = Regex.Replace(result, @"([a-z0-9])([A-Z])", "$1 $2");
            
            return result;
        }
        
        /// <summary>
        /// Represents information about a commit.
        /// </summary>
        public class CommitInfo
        {
            /// <summary>
            /// Gets or sets the JIRA ticket ID.
            /// </summary>
            public string JiraTicketId { get; set; } = string.Empty;
            
            /// <summary>
            /// Gets or sets the description.
            /// </summary>
            public string Description { get; set; } = string.Empty;
            
            /// <summary>
            /// Gets or sets the commit hash.
            /// </summary>
            public string CommitHash { get; set; } = string.Empty;
        }
    }
}
