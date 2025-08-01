﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LibGit2Sharp;
using Microsoft.Extensions.Configuration;
using CheckRelease.Adapters;
using CheckRelease.Interfaces;

namespace CheckRelease
{
    class Program
    {
        static int Main(string[] args)
        {            
            try
            {
                // Set up configuration
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                    .AddCommandLine(args)
                    .Build();
                
                // Create console output
                var console = new ConsoleOutput();
                
                // Parse command line arguments
                var parser = new CommandLineParser(configuration, console);
                var options = parser.Parse(args);
                
                if (options == null)
                {
                    return 1;
                }
                
                if (!parser.Validate(options))
                {
                    return 1;
                }
                
                // Update console with debug mode
                console = new ConsoleOutput(options.DebugMode);
                
                // Add validation for settings path when settings diff is requested
                if (options.SettingsDiff && string.IsNullOrWhiteSpace(options.SettingsPath))
                {
                    console.WriteError("Error: Settings path must be provided when using --settings-diff.");
                    console.WriteError("Example: --settings-diff=\"project-dir/appsettings.json\"");
                    return 1;
                }
                
                // Enable debug mode if requested
                if (options.DebugMode)
                {
                    console.WriteDebug($"Debug mode enabled");
                    console.WriteDebug($"Current directory: {Directory.GetCurrentDirectory()}");
                }
                
                // Enable trace mode if requested
                if (options.TraceMode)
                {
                    console.WriteDebug($"Trace mode enabled");
                    // In bash, this would set -x to enable command tracing
                    // In C#, we'll just log more verbose information
                }
                
                // Find a valid Git repository by traversing up the directory tree
                string currentDir = Directory.GetCurrentDirectory();
                string? repoPath = FindGitRepository(currentDir, options.DebugMode, console);
                
                if (string.IsNullOrEmpty(repoPath))
                {
                    console.WriteError("Error: Could not find a valid Git repository in the current directory or any parent directory.");
                    return 1;
                }
                
                if (options.DebugMode)
                {
                    console.WriteDebug($"Successfully found Git repository at: {repoPath}");
                }
                
                // Open the repository using our abstraction
                using (var gitRepo = new LibGit2SharpRepository(repoPath, options.DebugMode, console))
                {
                    // Create a tag selector
                    var tagSelector = new GitTagSelector(gitRepo, options.DebugMode, console);
                    
                    try
                    {
                        // Check if we're using --from-common-ancestor option
                        if (options.FromCommonAncestor)
                        {
                            if (options.DebugMode)
                            {
                                console.WriteDebug($"Using --from-common-ancestor mode with {options.Arguments.Count} argument(s)");
                            }
                            
                            // Get commits for common ancestor mode
                            (string commitA, string commitB) commitPair;
                            
                            if (options.Arguments.Count == 1)
                            {
                                // Single argument: compare HEAD to common ancestor with the specified reference
                                commitPair = tagSelector.SelectHeadToCommonAncestor(options.Arguments[0]);
                            }
                            else
                            {
                                // Two arguments: compare first argument to common ancestor with second argument
                                commitPair = tagSelector.SelectReferencesToCommonAncestor(options.Arguments[0], options.Arguments[1]);
                            }
                            
                            var (commitA, commitB) = commitPair;
                            
                            // Process the commit pair
                            var commitAnalyzer = new CommitAnalyzer(gitRepo, options.Prefix, options.DebugMode, console);
                            var commits = commitAnalyzer.AnalyzeCommits(commitA, commitB);
                            var releaseDate = DateTime.Now; // Use current date for HEAD
                            
                            // Generate output
                            var outputGenerator = new OutputGenerator(options.HtmlOutput, options.DebugMode, options.Prefix, options.JiraBaseUrl, console);
                            string output = outputGenerator.GenerateOutput(commitA, commitB, releaseDate, commits);
                            
                            // Generate settings diff if requested
                            if (options.SettingsDiff)
                            {
                                try
                                {
                                    if (options.DebugMode)
                                    {
                                        console.WriteDebug($"Generating settings diff for {commitA} -> {commitB}...");
                                    }
                                    
                                    var settingsDiffGenerator = new SettingsDiffGenerator(gitRepo, options.DebugMode, options.SettingsPath, console);
                                    string settingsDiff = settingsDiffGenerator.GenerateSettingsDiff(commitA, commitB, options.HtmlOutput);
                                    
                                    // Add settings diff to output
                                    output += Environment.NewLine + Environment.NewLine + settingsDiff;
                                }
                                catch (Exception ex)
                                {
                                    console.WriteError($"Error generating settings diff: {ex.Message}");
                                    if (options.DebugMode && ex.InnerException != null)
                                    {
                                        console.WriteError($"Inner Exception: {ex.InnerException.Message}");
                                    }
                                }
                            }
                            
                            // Write output to console
                            console.WriteLine(string.Empty);
                            console.WriteLine(output);
                        }
                        // Check if we're in auto or stream mode
                        else if (options.Arguments.Count >= 1 && options.Arguments[0] == "auto")
                        {
                            // Get the tag type
                            string tagType = options.Arguments.Count > 1 ? options.Arguments[1] : "production";
                            
                            // Get all tag pairs for auto mode
                            var tagPairs = tagSelector.SelectTagsAuto(tagType, options.SpanDays);
                            
                            // For HTML output, we need to collect all data first
                            if (options.HtmlOutput)
                            {
                                var allPairsData = new List<(string TagA, string TagB, DateTime ReleaseDate, List<CommitAnalyzer.CommitInfo> Commits, string SettingsDiff)>();
                                
                                // Process each pair to collect data
                                foreach (var (tagA, tagB) in tagPairs)
                                {
                                    var commitAnalyzer = new CommitAnalyzer(gitRepo, options.Prefix, options.DebugMode, console);
                                    var commits = commitAnalyzer.AnalyzeCommits(tagA, tagB);
                                    var releaseDate = GetTagDate(gitRepo, tagB);
                                    
                                    // Generate settings diff if requested
                                    string settingsDiff = "";
                                    if (options.SettingsDiff)
                                    {
                                        try
                                        {
                                            var settingsDiffGenerator = new SettingsDiffGenerator(gitRepo, options.DebugMode, options.SettingsPath, console);
                                            settingsDiff = settingsDiffGenerator.GenerateSettingsDiff(tagA, tagB, options.HtmlOutput);
                                        }
                                        catch (Exception ex)
                                        {
                                            if (options.DebugMode)
                                            {
                                                console.WriteError($"Error generating settings diff for {tagA} -> {tagB}: {ex.Message}");
                                            }
                                        }
                                    }
                                    
                                    allPairsData.Add((tagA, tagB, releaseDate, commits, settingsDiff));
                                }
                                
                                // Generate HTML output with all pairs, but meta tags only from the most recent pair
                                var outputGenerator = new OutputGenerator(options.HtmlOutput, options.DebugMode, options.Prefix, options.JiraBaseUrl, console);
                                string output = outputGenerator.GenerateHtmlOutputForMultiplePairs(allPairsData);
                                
                                // Write output to console
                                console.WriteLine(string.Empty);
                                console.WriteLine(output);
                            }
                            else
                            {
                                // For plain text output, process each pair sequentially
                                foreach (var (tagA, tagB) in tagPairs)
                                {
                                    var commitAnalyzer = new CommitAnalyzer(gitRepo, options.Prefix, options.DebugMode, console);
                                    var commits = commitAnalyzer.AnalyzeCommits(tagA, tagB);
                                    var releaseDate = GetTagDate(gitRepo, tagB);
                                    
                                    // Generate output
                                    var outputGenerator = new OutputGenerator(options.HtmlOutput, options.DebugMode, options.Prefix, options.JiraBaseUrl, console);
                                    string output = outputGenerator.GenerateOutput(tagA, tagB, releaseDate, commits);
                                    
                                    // Generate settings diff if requested
                                    if (options.SettingsDiff)
                                    {
                                        try
                                        {
                                            if (options.DebugMode)
                                            {
                                                console.WriteDebug($"Generating settings diff for {tagA} -> {tagB}...");
                                            }
                                            
                                            var settingsDiffGenerator = new SettingsDiffGenerator(gitRepo, options.DebugMode, options.SettingsPath, console);
                                            string settingsDiff = settingsDiffGenerator.GenerateSettingsDiff(tagA, tagB, options.HtmlOutput);
                                            
                                            // Add settings diff to output
                                            output += Environment.NewLine + Environment.NewLine + settingsDiff;
                                        }
                                        catch (Exception ex)
                                        {
                                            console.WriteError($"Error generating settings diff: {ex.Message}");
                                            if (options.DebugMode && ex.InnerException != null)
                                            {
                                                console.WriteError($"Inner Exception: {ex.InnerException.Message}");
                                            }
                                        }
                                    }
                                    
                                    // Write output to console
                                    console.WriteLine(string.Empty);
                                    console.WriteLine(output);
                                }
                            }
                        }
                        else if (options.Arguments.Count >= 1 && options.Arguments[0] == "stream")
                        {
                            // Get the tag type (optional)
                            string? tagType = options.Arguments.Count > 1 ? options.Arguments[1] : null;
                            
                            // Get commits for stream mode
                            var (commitA, commitB) = tagSelector.SelectStreamCommits(options.SpanDays, tagType);
                            
                            // Process the commit pair
                            var commitAnalyzer = new CommitAnalyzer(gitRepo, options.Prefix, options.DebugMode, console);
                            var commits = commitAnalyzer.AnalyzeCommits(commitA, commitB);
                            var releaseDate = DateTime.Now; // Use current date for HEAD
                            
                            // Generate output
                            var outputGenerator = new OutputGenerator(options.HtmlOutput, options.DebugMode, options.Prefix, options.JiraBaseUrl, console);
                            string output = outputGenerator.GenerateOutput(commitA, commitB, releaseDate, commits);
                            
                            // Generate settings diff if requested
                            if (options.SettingsDiff)
                            {
                                try
                                {
                                    if (options.DebugMode)
                                    {
                                        console.WriteDebug($"Generating settings diff for {commitA} -> {commitB}...");
                                    }
                                    
                                    var settingsDiffGenerator = new SettingsDiffGenerator(gitRepo, options.DebugMode, options.SettingsPath, console);
                                    string settingsDiff = settingsDiffGenerator.GenerateSettingsDiff(commitA, commitB, options.HtmlOutput);
                                    
                                    // Add settings diff to output
                                    output += Environment.NewLine + Environment.NewLine + settingsDiff;
                                }
                                catch (Exception ex)
                                {
                                    console.WriteError($"Error generating settings diff: {ex.Message}");
                                    if (options.DebugMode && ex.InnerException != null)
                                    {
                                        console.WriteError($"Inner Exception: {ex.InnerException.Message}");
                                    }
                                }
                            }
                            
                            // Write output to console
                            console.WriteLine(string.Empty);
                            console.WriteLine(output);
                        }
                        else
                        {
                            // Handle other modes (direct, type, single tag) as before
                            var (tagA, tagB) = tagSelector.SelectTags(options.Arguments);
                            
                            if (options.DebugMode)
                            {
                                console.WriteDebug($"Selected tags for comparison: {tagA} -> {tagB}");
                            }
                            
                            // Analyze commits between the tags
                            var commitAnalyzer = new CommitAnalyzer(gitRepo, options.Prefix, options.DebugMode, console);
                            var commits = commitAnalyzer.AnalyzeCommits(tagA, tagB);
                            
                            // Get the release date from the tag
                            var releaseDate = GetTagDate(gitRepo, tagB);
                            
                            // Generate output
                            if (options.DebugMode)
                            {
                                console.WriteDebug($"Found {commits.Count} commits with JIRA tickets");
                                foreach (var commit in commits)
                                {
                                    console.WriteDebug($"  {commit.JiraTicketId} - {commit.Description}");
                                }
                            }
                            
                            // Create output generator with configured values
                            var outputGenerator = new OutputGenerator(options.HtmlOutput, options.DebugMode, options.Prefix, options.JiraBaseUrl, console);
                            
                            // Generate output
                            string output = outputGenerator.GenerateOutput(tagA, tagB, releaseDate, commits);
                            
                            // Generate settings diff if requested
                            if (options.SettingsDiff)
                            {
                                try
                                {
                                    if (options.DebugMode)
                                    {
                                        console.WriteDebug("Generating settings diff...");
                                    }
                                    
                                    var settingsDiffGenerator = new SettingsDiffGenerator(gitRepo, options.DebugMode, options.SettingsPath, console);
                                    string settingsDiff = settingsDiffGenerator.GenerateSettingsDiff(tagA, tagB, options.HtmlOutput);
                                    
                                    // Add settings diff to output
                                    output += Environment.NewLine + Environment.NewLine + settingsDiff;
                                }
                                catch (Exception ex)
                                {
                                    console.WriteError($"Error generating settings diff: {ex.Message}");
                                    if (options.DebugMode && ex.InnerException != null)
                                    {
                                        console.WriteError($"Inner Exception: {ex.InnerException.Message}");
                                    }
                                }
                            }
                            
                            // Write output to console
                            console.WriteLine(string.Empty);
                            console.WriteLine(output);
                        }
                    }
                    catch (NotImplementedException ex)
                    {
                        console.WriteError($"Feature not yet implemented: {ex.Message}");
                        return 1;
                    }
                    catch (Exception ex)
                    {
                        console.WriteError($"Error selecting tags: {ex.Message}");
                        return 1;
                    }
                    
                    if (options.DebugMode)
                    {
                        console.WriteDebug($"Arguments: {string.Join(", ", options.Arguments)}");
                        console.WriteDebug($"HTML Output: {options.HtmlOutput}");
                        console.WriteDebug($"Settings Diff: {options.SettingsDiff}");
                    }
                }
                
                return 0;
            }
            catch (Exception ex)
            {
                var console = new ConsoleOutput();
                console.WriteError($"Error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    console.WriteError($"Inner Exception: {ex.InnerException.Message}");
                }
                return 1;
            }
        }
        
        /// <summary>
        /// Finds a valid Git repository by traversing up the directory tree.
        /// </summary>
        /// <param name="startPath">The starting directory path.</param>
        /// <param name="debug">Whether to enable debug output.</param>
        /// <param name="console">The console output interface.</param>
        /// <returns>The path to a valid Git repository, or null if none is found.</returns>
        private static string? FindGitRepository(string startPath, bool debug = false, IConsoleOutput? console = null)
        {
            string currentPath = startPath;
            console = console ?? new ConsoleOutput(debug);
            
            while (!string.IsNullOrEmpty(currentPath))
            {
                if (debug)
                {
                    console.WriteDebug($"Checking if {currentPath} is a Git repository...");
                }
                
                if (Repository.IsValid(currentPath))
                {
                    return currentPath;
                }
                
                // Move up to the parent directory
                DirectoryInfo? parentDir = Directory.GetParent(currentPath);
                if (parentDir == null)
                {
                    break;
                }
                
                currentPath = parentDir.FullName;
            }
            
            return null;
        }
        
        /// <summary>
        /// Gets the date of a tag.
        /// </summary>
        /// <param name="repo">The Git repository.</param>
        /// <param name="tagName">The tag name.</param>
        /// <returns>The date of the tag.</returns>
        private static DateTime GetTagDate(IGitRepository repo, string tagName)
        {
            if (tagName == "HEAD")
            {
                var headCommit = repo.GetHeadGitCommit();
                if (headCommit != null)
                {
                    return headCommit.AuthorWhen.DateTime;
                }
                return DateTime.Now;
            }
            
            var commit = repo.LookupGitCommit(tagName);
            if (commit != null)
            {
                return commit.AuthorWhen.DateTime;
            }
            
            var tag = repo.GetTag(tagName);
            if (tag != null)
            {
                return tag.CreatedAt.DateTime;
            }
            
            throw new ArgumentException($"Tag or commit '{tagName}' not found.");
        }
    }
}
