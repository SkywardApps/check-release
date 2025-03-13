using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using CheckRelease.Interfaces;

namespace CheckRelease
{
    /// <summary>
    /// Generates a diff of appsettings.json between two Git tags.
    /// </summary>
    public class SettingsDiffGenerator
    {
        private readonly IGitRepository _repository;
        private readonly bool _debugMode;
        private readonly string _settingsPath;
        private readonly IConsoleOutput _console;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsDiffGenerator"/> class.
        /// </summary>
        /// <param name="repository">The Git repository.</param>
        /// <param name="debugMode">Whether debug mode is enabled.</param>
        /// <param name="settingsPath">The path to the appsettings.json file relative to the repository root.</param>
        /// <param name="console">The console output interface.</param>
        public SettingsDiffGenerator(IGitRepository repository, bool debugMode = false, string settingsPath = "<NOT SET>", IConsoleOutput? console = null)
        {
            _repository = repository;
            _debugMode = debugMode;
            _settingsPath = settingsPath ?? throw new ArgumentNullException(nameof(settingsPath), "Settings path must be provided when generating a settings diff.");
            _console = console ?? new Adapters.ConsoleOutput(debugMode);
        }
        
        /// <summary>
        /// Generates a diff of appsettings.json between two tags.
        /// </summary>
        /// <param name="tagA">The first tag.</param>
        /// <param name="tagB">The second tag.</param>
        /// <param name="htmlOutput">Whether to generate HTML output.</param>
        /// <returns>The generated diff output.</returns>
        public string GenerateSettingsDiff(string tagA, string tagB, bool htmlOutput)
        {
            try
            {
                if (_debugMode)
                {
                    _console.WriteDebug($"Generating settings diff between {tagA} and {tagB}");
                    _console.WriteDebug($"Settings path: {_settingsPath}");
                }
                
                // Extract appsettings.json from both tags
                string? oldJson = ExtractAppsettingsFromTag(tagA);
                string? newJson = ExtractAppsettingsFromTag(tagB);
                
                if (string.IsNullOrEmpty(oldJson))
                {
                    return $"Error: Could not extract appsettings.json from tag/commit '{tagA}'. The file may not exist at this path in this commit, or there might be an issue accessing the Git repository.";
                }
                
                if (string.IsNullOrEmpty(newJson))
                {
                    return $"Error: Could not extract appsettings.json from tag/commit '{tagB}'. The file may not exist at this path in this commit, or there might be an issue accessing the Git repository.";
                }
                
                // Parse JSON with System.Text.Json
                var options = new JsonDocumentOptions
                {
                    CommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                };
                
                JsonDocument? oldDoc = null;
                JsonDocument? newDoc = null;
                
                try
                {
                    oldDoc = JsonDocument.Parse(oldJson, options);
                    if (_debugMode)
                    {
                        _console.WriteDebug($"Successfully parsed JSON from {tagA}");
                    }
                }
                catch (JsonException ex)
                {
                    if (_debugMode)
                    {
                        _console.WriteDebug($"Error parsing JSON from {tagA}: {ex.Message}");
                        _console.WriteDebug($"First 100 chars of problematic JSON: {oldJson.Substring(0, Math.Min(100, oldJson.Length))}...");
                    }
                    return $"Error: Invalid JSON format in appsettings.json from '{tagA}': {ex.Message}";
                }
                
                try
                {
                    newDoc = JsonDocument.Parse(newJson, options);
                    if (_debugMode)
                    {
                        _console.WriteDebug($"Successfully parsed JSON from {tagB}");
                    }
                }
                catch (JsonException ex)
                {
                    if (oldDoc != null)
                    {
                        oldDoc.Dispose();
                    }
                    
                    if (_debugMode)
                    {
                        _console.WriteDebug($"Error parsing JSON from {tagB}: {ex.Message}");
                        _console.WriteDebug($"First 100 chars of problematic JSON: {newJson.Substring(0, Math.Min(100, newJson.Length))}...");
                    }
                    return $"Error: Invalid JSON format in appsettings.json from '{tagB}': {ex.Message}";
                }
                
                try
                {
                    using (oldDoc)
                    using (newDoc)
                    {
                        // Flatten JSON objects
                        var oldFlattened = FlattenJson(oldDoc.RootElement);
                        var newFlattened = FlattenJson(newDoc.RootElement);
                        
                        if (_debugMode)
                        {
                            _console.WriteDebug($"Flattened JSON from {tagA}: {oldFlattened.Count} properties");
                            _console.WriteDebug($"Flattened JSON from {tagB}: {newFlattened.Count} properties");
                        }
                        
                        // Compare flattened objects
                        var (addedProperties, removedProperties) = CompareJson(oldFlattened, newFlattened);
                        
                        if (_debugMode)
                        {
                            _console.WriteDebug($"Found {addedProperties.Count} added properties and {removedProperties.Count} removed properties");
                        }
                        
                        // Format the output
                        return FormatDiffOutput(addedProperties, removedProperties, htmlOutput);
                    }
                }
                catch (Exception)
                {
                    if (oldDoc != null)
                    {
                        oldDoc.Dispose();
                    }
                    
                    if (newDoc != null)
                    {
                        newDoc.Dispose();
                    }
                    
                    throw;
                }
            }
            catch (Exception ex)
            {
                var errorMessage = new StringBuilder();
                errorMessage.AppendLine($"Error generating settings diff: {ex.Message}");
                
                if (_debugMode)
                {
                    errorMessage.AppendLine($"Exception type: {ex.GetType().Name}");
                    
                    if (ex.InnerException != null)
                    {
                        errorMessage.AppendLine($"Inner exception: {ex.InnerException.Message}");
                        errorMessage.AppendLine($"Inner exception type: {ex.InnerException.GetType().Name}");
                    }
                    
                    errorMessage.AppendLine($"Stack trace: {ex.StackTrace}");
                }
                
                return errorMessage.ToString();
            }
        }
        
        /// <summary>
        /// Extracts appsettings.json from a specific tag or commit.
        /// </summary>
        /// <param name="tagOrCommit">The tag or commit ID to extract from.</param>
        /// <returns>The content of appsettings.json at that tag or commit.</returns>
        private string? ExtractAppsettingsFromTag(string tagOrCommit)
        {
            try
            {
                if (_debugMode)
                {
                    _console.WriteDebug($"Extracting appsettings.json from {tagOrCommit}");
                }
                
                // Get the file content directly using our abstraction
                string? content = _repository.GetFileContentAtReference(tagOrCommit, _settingsPath);
                
                if (content == null)
                {
                    if (_debugMode)
                    {
                        _console.WriteDebug($"File '{_settingsPath}' not found in reference '{tagOrCommit}'");
                    }
                    return null;
                }
                
                if (_debugMode)
                {
                    _console.WriteDebug($"Successfully extracted appsettings.json from {tagOrCommit}");
                    _console.WriteDebug($"Content size: {content.Length} bytes");
                    if (content.Length > 0)
                    {
                        _console.WriteDebug($"First 100 chars: {content.Substring(0, Math.Min(100, content.Length))}...");
                    }
                }
                
                return content;
            }
            catch (Exception ex)
            {
                if (_debugMode)
                {
                    _console.WriteError($"Error extracting appsettings.json from {tagOrCommit}: {ex.Message}");
                    _console.WriteError($"Exception type: {ex.GetType().Name}");
                    
                    if (ex.InnerException != null)
                    {
                        _console.WriteError($"Inner exception: {ex.InnerException.Message}");
                        _console.WriteError($"Inner exception type: {ex.InnerException.GetType().Name}");
                    }
                    
                    _console.WriteError($"Stack trace: {ex.StackTrace}");
                }
                
                return null;
            }
        }
        
        /// <summary>
        /// Flattens a JSON object into key-value pairs.
        /// </summary>
        /// <param name="element">The JSON element.</param>
        /// <param name="prefix">The prefix for nested properties.</param>
        /// <returns>A dictionary of flattened key-value pairs.</returns>
        private Dictionary<string, string> FlattenJson(JsonElement element, string prefix = "")
        {
            var result = new Dictionary<string, string>();
            
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var property in element.EnumerateObject())
                    {
                        string newPrefix = string.IsNullOrEmpty(prefix) 
                            ? property.Name 
                            : $"{prefix}__{property.Name}";
                        
                        var nestedResult = FlattenJson(property.Value, newPrefix);
                        foreach (var item in nestedResult)
                        {
                            result[item.Key] = item.Value;
                        }
                    }
                    break;
                
                case JsonValueKind.Array:
                    int index = 0;
                    foreach (var item in element.EnumerateArray())
                    {
                        string newPrefix = $"{prefix}__{index}";
                        var nestedResult = FlattenJson(item, newPrefix);
                        foreach (var kvp in nestedResult)
                        {
                            result[kvp.Key] = kvp.Value;
                        }
                        index++;
                    }
                    break;
                
                default:
                    // For primitive values, store the value as a string
                    result[prefix] = GetElementValueAsString(element, prefix);
                    break;
            }
            
            return result;
        }
        
        /// <summary>
        /// Checks if a key contains sensitive words that should have their values censored.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <returns>True if the key contains sensitive words, false otherwise.</returns>
        private bool IsSensitiveKey(string key)
        {
            string[] sensitiveWords = { "password", "secret", "token", "secure" };
            return sensitiveWords.Any(word => key.ToLowerInvariant().Contains(word));
        }
        
        /// <summary>
        /// Censors a sensitive value by keeping the first and last characters and replacing the middle with asterisks.
        /// </summary>
        /// <param name="value">The value to censor.</param>
        /// <returns>The censored value.</returns>
        private string CensorValue(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= 2)
            {
                return value; // Nothing to censor for very short strings
            }
            
            return $"{value[0]}********{value[^1]}";
        }
        
        /// <summary>
        /// Gets the value of a JsonElement as a string.
        /// </summary>
        /// <param name="element">The JSON element.</param>
        /// <param name="key">The key associated with this element.</param>
        /// <returns>The string representation of the element's value.</returns>
        private string GetElementValueAsString(JsonElement element, string key = "")
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                    string? value = element.GetString();
                    // Censor sensitive values
                    if (IsSensitiveKey(key) && value != null)
                    {
                        return CensorValue(value);
                    }
                    return value ?? string.Empty;
                case JsonValueKind.Number:
                    return element.GetRawText();
                case JsonValueKind.True:
                    return "true";
                case JsonValueKind.False:
                    return "false";
                case JsonValueKind.Null:
                    return "null";
                default:
                    return element.GetRawText();
            }
        }
        
        /// <summary>
        /// Compares two flattened JSON objects to find added and removed properties.
        /// </summary>
        /// <param name="oldJson">The old flattened JSON object.</param>
        /// <param name="newJson">The new flattened JSON object.</param>
        /// <returns>A tuple containing lists of added and removed properties.</returns>
        private (List<string> AddedProperties, List<string> RemovedProperties) CompareJson(
            Dictionary<string, string> oldJson, 
            Dictionary<string, string> newJson)
        {
            var addedProperties = new List<string>();
            var removedProperties = new List<string>();
            
            // Find added properties (in new but not in old)
            foreach (var key in newJson.Keys)
            {
                if (!oldJson.ContainsKey(key))
                {
                    addedProperties.Add($"{key} = {newJson[key]}");
                }
            }
            
            // Find removed properties (in old but not in new)
            foreach (var key in oldJson.Keys)
            {
                if (!newJson.ContainsKey(key))
                {
                    removedProperties.Add($"{key} = {oldJson[key]}");
                }
            }
            
            return (addedProperties, removedProperties);
        }
        
        /// <summary>
        /// Formats the diff output in plain text or HTML.
        /// </summary>
        /// <param name="addedProperties">The list of added properties.</param>
        /// <param name="removedProperties">The list of removed properties.</param>
        /// <param name="htmlOutput">Whether to generate HTML output.</param>
        /// <returns>The formatted diff output.</returns>
        private string FormatDiffOutput(
            List<string> addedProperties, 
            List<string> removedProperties, 
            bool htmlOutput)
        {
            var sb = new StringBuilder();
            
            if (htmlOutput)
            {
                sb.AppendLine("<div class=\"settings-diff\">");
                
                if (addedProperties.Count > 0 || removedProperties.Count > 0)
                {
                    sb.AppendLine("<h2>Settings Changes</h2>");
                    
                    if (addedProperties.Count > 0)
                    {
                        sb.AppendLine("<h3>Added Properties</h3>");
                        sb.AppendLine("<ul class=\"added-properties\">");
                        foreach (var property in addedProperties)
                        {
                            sb.AppendLine($"<li>{property}</li>");
                        }
                        sb.AppendLine("</ul>");
                    }
                    
                    if (removedProperties.Count > 0)
                    {
                        sb.AppendLine("<h3>Removed Properties</h3>");
                        sb.AppendLine("<ul class=\"removed-properties\">");
                        foreach (var property in removedProperties)
                        {
                            sb.AppendLine($"<li>{property}</li>");
                        }
                        sb.AppendLine("</ul>");
                    }
                }
                else
                {
                    sb.AppendLine("<p>No settings changes detected.</p>");
                }
                
                sb.AppendLine("</div>");
            }
            else
            {
                sb.AppendLine("Settings Changes:");
                sb.AppendLine();
                
                if (addedProperties.Count > 0 || removedProperties.Count > 0)
                {
                    if (addedProperties.Count > 0)
                    {
                        sb.AppendLine("Added Properties:");
                        foreach (var property in addedProperties)
                        {
                            sb.AppendLine($"+ {property}");
                        }
                        sb.AppendLine();
                    }
                    
                    if (removedProperties.Count > 0)
                    {
                        sb.AppendLine("Removed Properties:");
                        foreach (var property in removedProperties)
                        {
                            sb.AppendLine($"- {property}");
                        }
                        sb.AppendLine();
                    }
                }
                else
                {
                    sb.AppendLine("No settings changes detected.");
                }
            }
            
            return sb.ToString();
        }
    }
}
