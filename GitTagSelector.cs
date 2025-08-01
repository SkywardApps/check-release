using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CheckRelease.Domain;
using CheckRelease.Interfaces;

namespace CheckRelease
{
    /// <summary>
    /// Handles selection of Git tags for comparison.
    /// </summary>
    public class GitTagSelector
    {
        private readonly IGitRepository _repository;
        private readonly bool _debugMode;
        private readonly IConsoleOutput _console;
        
        /// <summary>
        /// Valid tag types for filtering.
        /// </summary>
        public static readonly string[] ValidTypes = { "production", "uat", "qa", "dev" };
        
        /// <summary>
        /// Initializes a new instance of the <see cref="GitTagSelector"/> class.
        /// </summary>
        /// <param name="repository">The Git repository.</param>
        /// <param name="debugMode">Whether debug mode is enabled.</param>
        /// <param name="console">The console output interface.</param>
        public GitTagSelector(IGitRepository repository, bool debugMode = false, IConsoleOutput? console = null)
        {
            _repository = repository;
            _debugMode = debugMode;
            _console = console ?? new Adapters.ConsoleOutput(debugMode);
        }
        
        /// <summary>
        /// Selects tags for comparison based on the provided arguments.
        /// </summary>
        /// <param name="args">The command line arguments.</param>
        /// <returns>A tuple containing the two tags to compare.</returns>
        public (string TagA, string TagB) SelectTags(List<string> args)
        {
            if (_debugMode)
            {
                _console.WriteDebug($"Selecting tags based on arguments: {string.Join(", ", args)}");
            }
            
            // Handle different modes
            if (args.Count == 2 && args[0] != "auto")
            {
                // Direct comparison between two tags
                string tagA = args[0];
                string tagB = args[1];
                
                if (_debugMode)
                {
                    _console.WriteDebug($"Direct comparison between tags: {tagA} and {tagB}");
                }
                
                // Ensure the tags exist
                if (!TagExists(tagA))
                {
                    throw new ArgumentException($"Tag '{tagA}' does not exist.");
                }
                
                if (!TagExists(tagB))
                {
                    throw new ArgumentException($"Tag '{tagB}' does not exist.");
                }
                
                // Sort tags chronologically
                return SortTagsChronologically(tagA, tagB);
            }
            else if (args.Count >= 1 && args[0] == "auto")
            {
                // Auto mode to find recent tags of a specified type
                string tagType = args.Count > 1 ? args[1] : "production";
                
                if (_debugMode)
                {
                    _console.WriteDebug($"Auto mode for tag type: {tagType}");
                }
                
                // Find recent tags of the specified type
                var recentTags = FindRecentTags(tagType, 42); // Default to 6 weeks (42 days)
                
                if (recentTags.Count < 2)
                {
                    throw new InvalidOperationException($"Less than two '{tagType}' tags found in the last 42 days. Found {recentTags.Count} tag(s).");
                }
                
                // Use the two most recent tags
                string tagA = recentTags[1]; // Second most recent (older)
                string tagB = recentTags[0]; // Most recent (newer)
                
                if (_debugMode)
                {
                    _console.WriteDebug($"Selected tags: {tagA} and {tagB}");
                }
                
                return (tagA, tagB);
            }
            else if (args.Count == 1)
            {
                string arg = args[0];
                
                if (ValidTypes.Contains(arg))
                {
                    // Type mode - show all tags of that type from the last month
                    if (_debugMode)
                    {
                        _console.WriteDebug($"Type mode for tag type: {arg}");
                    }
                    
                    // Find tags of the specified type from the last month
                    var tags = FindRecentTags(arg, 4); // 4 weeks = 1 month
                    
                    if (tags.Count < 2)
                    {
                        throw new InvalidOperationException($"Less than two '{arg}' tags found in the last month. Found {tags.Count} tag(s).");
                    }
                    
                    // Use interactive selection to choose two tags
                    var selectedTags = SelectTagsInteractively(tags, "Select two tags for comparison", true);
                    
                    if (selectedTags.Count != 2)
                    {
                        throw new InvalidOperationException("You must select exactly two tags for comparison.");
                    }
                    
                    // Sort tags chronologically
                    return SortTagsChronologically(selectedTags[0], selectedTags[1]);
                }
                else if (ValidTypes.Any(type => arg.Contains(type)))
                {
                    // Single tag mode - find all tags of the same type from the month prior
                    if (_debugMode)
                    {
                        _console.WriteDebug($"Single tag mode for tag: {arg}");
                    }
                    
                    // Ensure the tag exists
                    if (!TagExists(arg))
                    {
                        throw new ArgumentException($"Tag '{arg}' does not exist.");
                    }
                    
                    // Get the tag type
                    string tagType = ValidTypes.First(type => arg.Contains(type));
                    
                    // Get the date of the specified tag
                    DateTimeOffset tagDate = GetTagDate(arg);
                    
                    // Find tags of the same type from the month prior
                    var tags = FindTagsBeforeDate(tagType, tagDate, 4); // 4 weeks = 1 month
                    
                    if (tags.Count == 0)
                    {
                        throw new InvalidOperationException($"No '{tagType}' tags found in the month prior to {arg}.");
                    }
                    
                    // Use interactive selection to choose one tag
                    var selectedTags = SelectTagsInteractively(tags, $"Select a tag to compare with {arg}", false);
                    
                    if (selectedTags.Count != 1)
                    {
                        throw new InvalidOperationException("You must select exactly one tag for comparison.");
                    }
                    
                    // Sort tags chronologically
                    return SortTagsChronologically(selectedTags[0], arg);
                }
            }
            
            throw new ArgumentException("Invalid arguments for tag selection.");
        }
        
        /// <summary>
        /// Selects all tag pairs for auto mode.
        /// </summary>
        /// <param name="tagType">The tag type to filter by.</param>
        /// <param name="spanDays">Number of days to look back.</param>
        /// <returns>A list of tag pairs to compare.</returns>
        public List<(string TagA, string TagB)> SelectTagsAuto(string tagType, int spanDays)
        {
            if (_debugMode)
            {
                _console.WriteDebug($"Auto mode for tag type: {tagType}");
                _console.WriteDebug($"Looking back {spanDays} days");
            }
            
            // Find recent tags of the specified type
            var recentTags = FindRecentTags(tagType, spanDays);
            
            if (recentTags.Count < 2)
            {
                throw new InvalidOperationException($"Less than two '{tagType}' tags found in the last {spanDays} days. Found {recentTags.Count} tag(s).");
            }
            
            // Create pairs of consecutive tags
            var tagPairs = new List<(string TagA, string TagB)>();
            for (int i = 0; i < recentTags.Count - 1; i++)
            {
                // Sort each pair chronologically
                var pair = SortTagsChronologically(recentTags[i+1], recentTags[i]);
                tagPairs.Add(pair);
            }
            
            if (_debugMode)
            {
                _console.WriteDebug($"Created {tagPairs.Count} tag pairs for comparison");
                foreach (var (tagA, tagB) in tagPairs)
                {
                    _console.WriteDebug($"  {tagA} -> {tagB}");
                }
            }
            
            return tagPairs;
        }
        
        /// <summary>
        /// Finds recent tags of a specified type.
        /// </summary>
        /// <param name="tagType">The tag type to filter by.</param>
        /// <param name="days">Number of days to look back.</param>
        /// <returns>A list of recent tags of the specified type.</returns>
        public List<string> FindRecentTags(string tagType, int days = 42)
        {
            if (_debugMode)
            {
                _console.WriteDebug($"Finding tags of type '{tagType}' from the last {days} days");
            }
            
            // Calculate the cutoff date
            DateTimeOffset cutoffDate = DateTimeOffset.Now.AddDays(-days);
            
            if (_debugMode)
            {
                _console.WriteDebug($"Cutoff date: {cutoffDate}");
            }
            
            // Get all tags
            var tags = _repository.GetAllTags()
                .Select(tag => new
                {
                    Name = tag.Name,
                    Date = tag.CreatedAt
                })
                .Where(tag => 
                    // Filter by tag type
                    tag.Name.Contains(tagType) &&
                    // Filter by date
                    tag.Date >= cutoffDate)
                .OrderByDescending(tag => tag.Date) // Sort by date (newest first)
                .Select(tag => tag.Name)
                .ToList();
            
            if (_debugMode)
            {
                _console.WriteDebug($"Found {tags.Count} tags of type '{tagType}' from the last {days} days:");
                foreach (var tag in tags)
                {
                    _console.WriteDebug($"  {tag}");
                }
            }
            
            return tags;
        }
        
        /// <summary>
        /// Finds tags of a specified type before a given date.
        /// </summary>
        /// <param name="tagType">The tag type to filter by.</param>
        /// <param name="date">The date to filter by.</param>
        /// <param name="weeksAgo">Number of weeks to look back from the given date.</param>
        /// <returns>A list of tags of the specified type before the given date.</returns>
        public List<string> FindTagsBeforeDate(string tagType, DateTimeOffset date, int weeksAgo = 4)
        {
            if (_debugMode)
            {
                _console.WriteDebug($"Finding tags of type '{tagType}' from the {weeksAgo} weeks before {date}");
            }
            
            // Calculate the cutoff date
            DateTimeOffset cutoffDate = date.AddDays(-7 * weeksAgo);
            
            if (_debugMode)
            {
                _console.WriteDebug($"Cutoff date: {cutoffDate}");
            }
            
            // Get all tags
            var tags = _repository.GetAllTags()
                .Select(tag => new
                {
                    Name = tag.Name,
                    Date = tag.CreatedAt
                })
                .Where(tag => 
                    // Filter by tag type
                    tag.Name.Contains(tagType) &&
                    // Filter by date (before the given date but after the cutoff date)
                    tag.Date < date && tag.Date >= cutoffDate)
                .OrderByDescending(tag => tag.Date) // Sort by date (newest first)
                .Select(tag => tag.Name)
                .ToList();
            
            if (_debugMode)
            {
                _console.WriteDebug($"Found {tags.Count} tags of type '{tagType}' from the {weeksAgo} weeks before {date}:");
                foreach (var tag in tags)
                {
                    _console.WriteDebug($"  {tag}");
                }
            }
            
            return tags;
        }
        
        /// <summary>
        /// Selects tags interactively from a list of tags.
        /// </summary>
        /// <param name="tags">The list of tags to select from.</param>
        /// <param name="prompt">The prompt to display.</param>
        /// <param name="multiSelect">Whether to allow multiple selections.</param>
        /// <returns>A list of selected tags.</returns>
        private List<string> SelectTagsInteractively(List<string> tags, string prompt, bool multiSelect)
        {
            try
            {
                if (_debugMode)
                {
                    _console.WriteDebug($"Selecting tags interactively from {tags.Count} tags");
                    _console.WriteDebug($"Prompt: {prompt}");
                    _console.WriteDebug($"Multi-select: {multiSelect}");
                }
                
                // Check if we're in an interactive terminal
                if (!Console.IsInputRedirected && !Console.IsOutputRedirected)
                {
                    try
                    {
                        // Get console window size
                        int windowWidth = Console.WindowWidth;
                        int windowHeight = Console.WindowHeight;
                        
                        // Check if window is too small
                        if (windowWidth < 40 || windowHeight < 10)
                        {
                            _console.WriteError("Warning: Terminal window is too small for optimal interactive selection.");
                            _console.WriteError($"Current size: {windowWidth}x{windowHeight}, recommended minimum: 40x10");
                        }
                    }
                    catch (Exception ex)
                    {
                        if (_debugMode)
                        {
                            _console.WriteDebug($"Could not determine terminal window size: {ex.Message}");
                        }
                    }
                    
                    // Display the prompt
                    _console.WriteLine(prompt);
                    _console.WriteLine("Use arrow keys to navigate, Enter to select, Esc to cancel");
                    if (multiSelect)
                    {
                        _console.WriteLine("Use Space to toggle selection, Enter to confirm");
                    }
                    _console.WriteLine(string.Empty);
                    
                    // Display the tags
                    var selectedIndices = new List<int>();
                    int currentIndex = 0;
                    bool done = false;
                    
                    while (!done)
                    {
                        try
                        {
                            // Clear the console
                            Console.Clear();
                            
                            // Display the prompt
                            _console.WriteLine(prompt);
                            _console.WriteLine("Use arrow keys to navigate, Enter to select, Esc to cancel");
                            if (multiSelect)
                            {
                                _console.WriteLine("Use Space to toggle selection, Enter to confirm");
                            }
                            _console.WriteLine(string.Empty);
                            
                            // Display the tags
                            for (int i = 0; i < tags.Count; i++)
                            {
                                string prefix = i == currentIndex ? "> " : "  ";
                                string suffix = selectedIndices.Contains(i) ? " [X]" : "    ";
                                _console.WriteLine($"{prefix}{tags[i]}{suffix}");
                            }
                            
                            // Get user input
                            var key = Console.ReadKey(true);
                            
                            switch (key.Key)
                            {
                                case ConsoleKey.UpArrow:
                                    currentIndex = Math.Max(0, currentIndex - 1);
                                    break;
                                
                                case ConsoleKey.DownArrow:
                                    currentIndex = Math.Min(tags.Count - 1, currentIndex + 1);
                                    break;
                                
                                case ConsoleKey.Spacebar:
                                    if (multiSelect)
                                    {
                                        if (selectedIndices.Contains(currentIndex))
                                        {
                                            selectedIndices.Remove(currentIndex);
                                        }
                                        else
                                        {
                                            selectedIndices.Add(currentIndex);
                                        }
                                    }
                                    break;
                                
                                case ConsoleKey.Enter:
                                    if (multiSelect)
                                    {
                                        if (selectedIndices.Count > 0)
                                        {
                                            done = true;
                                        }
                                        else
                                        {
                                            _console.WriteLine("Please select at least one tag before confirming.");
                                            Thread.Sleep(1500); // Give user time to read the message
                                        }
                                    }
                                    else
                                    {
                                        selectedIndices.Add(currentIndex);
                                        done = true;
                                    }
                                    break;
                                
                                case ConsoleKey.Escape:
                                    // Cancel selection
                                    _console.WriteLine("Selection cancelled by user.");
                                    return new List<string>();
                            }
                        }
                        catch (Exception ex)
                        {
                            if (_debugMode)
                            {
                                _console.WriteDebug($"Error during interactive display: {ex.Message}");
                            }
                            // Continue the loop to try again
                        }
                    }
                    
                    // Return the selected tags
                    return selectedIndices.Select(i => tags[i]).ToList();
                }
                else
                {
                    _console.WriteError("Error: Interactive selection requires an interactive terminal.");
                    _console.WriteError("Please run the application in an interactive terminal or use non-interactive modes.");
                    
                    // Fallback to non-interactive selection
                    if (_debugMode)
                    {
                        _console.WriteDebug("Falling back to non-interactive selection...");
                    }
                    
                    if (multiSelect && tags.Count >= 2)
                    {
                        _console.WriteLine($"Automatically selecting the first two tags: {tags[0]} and {tags[1]}");
                        return new List<string> { tags[0], tags[1] };
                    }
                    else if (!multiSelect && tags.Count >= 1)
                    {
                        _console.WriteLine($"Automatically selecting the first tag: {tags[0]}");
                        return new List<string> { tags[0] };
                    }
                    
                    return new List<string>();
                }
            }
            catch (Exception ex)
            {
                _console.WriteError($"Error during interactive selection: {ex.Message}");
                _console.WriteError("Falling back to non-interactive selection...");
                
                // Fallback to non-interactive selection
                if (multiSelect && tags.Count >= 2)
                {
                    _console.WriteLine($"Automatically selecting the first two tags: {tags[0]} and {tags[1]}");
                    return new List<string> { tags[0], tags[1] };
                }
                else if (!multiSelect && tags.Count >= 1)
                {
                    _console.WriteLine($"Automatically selecting the first tag: {tags[0]}");
                    return new List<string> { tags[0] };
                }
                
                return new List<string>();
            }
        }
        
        /// <summary>
        /// Checks if a tag exists in the repository.
        /// </summary>
        /// <param name="tagName">The tag name.</param>
        /// <returns>True if the tag exists, false otherwise.</returns>
        private bool TagExists(string tagName)
        {
            return _repository.TagExists(tagName);
        }
        
        /// <summary>
        /// Sorts two tags chronologically.
        /// </summary>
        /// <param name="tagA">The first tag.</param>
        /// <param name="tagB">The second tag.</param>
        /// <returns>A tuple containing the two tags sorted chronologically (older first).</returns>
        private (string TagA, string TagB) SortTagsChronologically(string tagA, string tagB)
        {
            // Get the commit dates for both tags
            DateTimeOffset tagADate = GetTagDate(tagA);
            DateTimeOffset tagBDate = GetTagDate(tagB);
            
            // Compare and swap if tagA is newer than tagB
            if (tagADate > tagBDate)
            {
                return (tagB, tagA);
            }
            
            return (tagA, tagB);
        }
        
        /// <summary>
        /// Selects HEAD and a commit from the specified number of days ago for comparison.
        /// </summary>
        /// <param name="spanDays">Number of days to look back.</param>
        /// <param name="tagType">Optional tag type to filter commits by.</param>
        /// <returns>A tuple containing the old commit ID and "HEAD".</returns>
        public (string CommitA, string CommitB) SelectStreamCommits(int spanDays, string? tagType = null)
        {
            if (_debugMode)
            {
                _console.WriteDebug($"Stream mode: comparing HEAD to a commit from {spanDays} days ago");
                if (!string.IsNullOrEmpty(tagType))
                {
                    _console.WriteDebug($"Filtering by type: {tagType}");
                }
            }
            
            // Get the HEAD commit
            var head = _repository.GetHeadGitCommit();
            if (head == null)
            {
                throw new InvalidOperationException("Could not get HEAD commit");
            }
            
            // Calculate the cutoff date
            DateTimeOffset cutoffDate = DateTimeOffset.Now.AddDays(-spanDays);
            
            if (_debugMode)
            {
                _console.WriteDebug($"Cutoff date: {cutoffDate}");
            }
            
            // Find the most recent commit before the cutoff date
            GitCommit? oldCommit = null;
            
            // Get all commits reachable from HEAD
            var commits = _repository.GetCommitsReachableFrom("HEAD");
            
            foreach (var commit in commits)
            {
                // If we have a tag type filter, check if the commit message contains it
                if (!string.IsNullOrEmpty(tagType) && !commit.Message.Contains(tagType))
                {
                    continue;
                }
                
                if (commit.AuthorWhen <= cutoffDate)
                {
                    oldCommit = commit;
                    break;
                }
            }
            
            if (oldCommit == null)
            {
                throw new InvalidOperationException($"No commits found before {cutoffDate}");
            }
            
            if (_debugMode)
            {
                _console.WriteDebug($"Selected commits: {oldCommit.Sha.Substring(0, 7)} -> HEAD");
                _console.WriteDebug($"Old commit date: {oldCommit.AuthorWhen}");
                _console.WriteDebug($"HEAD date: {head.AuthorWhen}");
            }
            
            return (oldCommit.Sha, "HEAD");
        }
        
        /// <summary>
        /// Selects HEAD and the common ancestor with a specified reference for comparison.
        /// </summary>
        /// <param name="targetReference">The target reference (tag, commit, or environment name) to find common ancestor with.</param>
        /// <returns>A tuple containing the common ancestor and "HEAD".</returns>
        public (string CommitA, string CommitB) SelectHeadToCommonAncestor(string targetReference)
        {
            if (_debugMode)
            {
                _console.WriteDebug($"Finding common ancestor between HEAD and {targetReference}");
            }
            
            // Resolve environment name to actual tag if needed
            string resolvedReference = ResolveReferenceToTag(targetReference);
            
            if (_debugMode && resolvedReference != targetReference)
            {
                _console.WriteDebug($"Resolved environment '{targetReference}' to tag '{resolvedReference}'");
            }
            
            // Validate that the target reference exists
            var targetCommit = _repository.LookupGitCommit(resolvedReference);
            if (targetCommit == null)
            {
                throw new ArgumentException($"Error: Reference '{resolvedReference}' does not exist.");
            }
            
            // Get HEAD commit
            var headCommit = _repository.GetHeadGitCommit();
            if (headCommit == null)
            {
                throw new InvalidOperationException("Error: Could not get HEAD commit.");
            }
            
            // Check if HEAD and target are the same commit
            if (headCommit.Sha == targetCommit.Sha)
            {
                throw new InvalidOperationException($"Error: HEAD and '{targetReference}' are the same commit.");
            }
            
            // Find the merge base (common ancestor)
            var mergeBase = _repository.GetMergeBase("HEAD", resolvedReference);
            if (mergeBase == null)
            {
                throw new InvalidOperationException($"Error: No common ancestor found between HEAD and '{resolvedReference}'.");
            }
            
            if (_debugMode)
            {
                _console.WriteDebug($"Common ancestor found: {mergeBase.Sha.Substring(0, 7)}");
                _console.WriteDebug($"Comparison: {mergeBase.Sha.Substring(0, 7)} -> HEAD");
            }
            
            // Return the merge base as the older commit and HEAD as the newer commit
            return (mergeBase.Sha, "HEAD");
        }
        
        /// <summary>
        /// Selects the common ancestor between two references and returns it with the first reference.
        /// </summary>
        /// <param name="reference1">The first reference (tag, commit, or environment name).</param>
        /// <param name="reference2">The second reference (tag, commit, or environment name) to find common ancestor with.</param>
        /// <returns>A tuple containing the common ancestor and the first reference.</returns>
        public (string CommitA, string CommitB) SelectReferencesToCommonAncestor(string reference1, string reference2)
        {
            if (_debugMode)
            {
                _console.WriteDebug($"Finding common ancestor between {reference1} and {reference2}");
            }
            
            // Resolve environment names to actual tags if needed
            string resolvedReference1 = ResolveReferenceToTag(reference1);
            string resolvedReference2 = ResolveReferenceToTag(reference2);
            
            if (_debugMode && resolvedReference1 != reference1)
            {
                _console.WriteDebug($"Resolved environment '{reference1}' to tag '{resolvedReference1}'");
            }
            
            if (_debugMode && resolvedReference2 != reference2)
            {
                _console.WriteDebug($"Resolved environment '{reference2}' to tag '{resolvedReference2}'");
            }
            
            // Validate that both references exist
            var commit1 = _repository.LookupGitCommit(resolvedReference1);
            if (commit1 == null)
            {
                throw new ArgumentException($"Error: Reference '{resolvedReference1}' does not exist.");
            }
            
            var commit2 = _repository.LookupGitCommit(resolvedReference2);
            if (commit2 == null)
            {
                throw new ArgumentException($"Error: Reference '{resolvedReference2}' does not exist.");
            }
            
            // Check if both references are the same commit
            if (commit1.Sha == commit2.Sha)
            {
                throw new InvalidOperationException($"Error: '{resolvedReference1}' and '{resolvedReference2}' are the same commit.");
            }
            
            // Find the merge base (common ancestor)
            var mergeBase = _repository.GetMergeBase(resolvedReference1, resolvedReference2);
            if (mergeBase == null)
            {
                throw new InvalidOperationException($"Error: No common ancestor found between '{resolvedReference1}' and '{resolvedReference2}'.");
            }
            
            if (_debugMode)
            {
                _console.WriteDebug($"Common ancestor found: {mergeBase.Sha.Substring(0, 7)}");
                _console.WriteDebug($"Comparison: {mergeBase.Sha.Substring(0, 7)} -> {resolvedReference1}");
            }
            
            // Return the merge base as the older commit and the first reference as the newer commit
            return (mergeBase.Sha, resolvedReference1);
        }
        
        /// <summary>
        /// Gets the date of a tag.
        /// </summary>
        /// <param name="tagName">The tag name.</param>
        /// <returns>The date of the tag.</returns>
        private DateTimeOffset GetTagDate(string tagName)
        {
            var tag = _repository.GetTag(tagName);
            if (tag == null)
            {
                throw new ArgumentException($"Tag '{tagName}' not found.");
            }
            
            return tag.CreatedAt;
        }
        
        /// <summary>
        /// Resolves a reference to a tag name. If the reference is an environment name,
        /// it finds the most recent tag for that environment. Otherwise, returns the reference unchanged.
        /// </summary>
        /// <param name="reference">The reference to resolve (tag name, commit SHA, or environment name).</param>
        /// <returns>The resolved tag name or the original reference if not an environment name.</returns>
        private string ResolveReferenceToTag(string reference)
        {
            // Check if the reference is a valid environment name
            if (IsValidEnvironment(reference))
            {
                if (_debugMode)
                {
                    _console.WriteDebug($"Reference '{reference}' is an environment name, finding most recent tag");
                }
                
                // Find the most recent tag for this environment
                var recentTags = FindRecentTags(reference, 42); // Look back 42 days (6 weeks)
                
                if (recentTags.Count == 0)
                {
                    throw new InvalidOperationException($"No '{reference}' tags found in the last 42 days.");
                }
                
                string mostRecentTag = recentTags[0];
                
                if (_debugMode)
                {
                    _console.WriteDebug($"Found most recent '{reference}' tag: {mostRecentTag}");
                }
                
                return mostRecentTag;
            }
            
            // Not an environment name, return as-is
            return reference;
        }
        
        /// <summary>
        /// Checks if a reference is a valid environment name.
        /// </summary>
        /// <param name="reference">The reference to check.</param>
        /// <returns>True if the reference is a valid environment name, false otherwise.</returns>
        private bool IsValidEnvironment(string reference)
        {
            return ValidTypes.Contains(reference);
        }
    }
}
