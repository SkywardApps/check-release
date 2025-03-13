using System.CommandLine;
using System.CommandLine.Parsing;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using CheckRelease.Interfaces;

namespace CheckRelease
{
    /// <summary>
    /// Handles parsing of command line arguments for the CheckRelease application.
    /// </summary>
    public class CommandLineParser
    {
        private readonly RootCommand _rootCommand;
        private readonly IConfiguration _configuration;
        private readonly IConsoleOutput _console;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="CommandLineParser"/> class.
        /// </summary>
        /// <param name="configuration">The configuration to use for default values.</param>
        [UnconditionalSuppressMessage("Trimming", "IL2026:RequiresUnreferencedCode", 
            Justification = "AppSettings properties are preserved")]
        private static void BindConfiguration(IConfiguration configuration, AppSettings settings)
        {
            configuration.Bind(settings);
        }

        public CommandLineParser(IConfiguration configuration, IConsoleOutput console)
        {
            _configuration = configuration;
            _console = console;
            _rootCommand = new RootCommand("C# port of check_release.sh - Analyzes commits between Git tags");
            
            // Get default values from configuration
            var appSettings = new AppSettings();
            BindConfiguration(_configuration, appSettings);
            
            // Add options
            var htmlOption = new Option<bool>("--html", "Generate HTML output instead of plain text");
            var settingsDiffOption = new Option<string>("--settings-diff", "Include a diff of appsettings.json between tags and specify the path to the settings file");
            settingsDiffOption.SetDefaultValue(appSettings.SettingsPath);
            var debugOption = new Option<bool>("--debug", "Enable debug mode with verbose output");
            var traceOption = new Option<bool>("--trace", "Enable command tracing");
            var spanOption = new Option<int>("--span", () => appSettings.SpanDays, $"Number of days to look back for tags");
            var prefixOption = new Option<string>("--prefix", () => appSettings.Prefix, $"Prefix for JIRA tickets in commit messages");
            var jiraBaseUrlOption = new Option<string>("--jira-base-url", () => appSettings.JiraBaseUrl, $"Base URL for JIRA tickets");
            
            _rootCommand.AddOption(htmlOption);
            _rootCommand.AddOption(settingsDiffOption);
            _rootCommand.AddOption(debugOption);
            _rootCommand.AddOption(traceOption);
            _rootCommand.AddOption(spanOption);
            _rootCommand.AddOption(prefixOption);
            _rootCommand.AddOption(jiraBaseUrlOption);
            
            // Add argument for tags or commands
            var tagsArgument = new Argument<string[]>("tags", "Tags or commands to use")
            {
                Arity = ArgumentArity.ZeroOrMore
            };
            
            _rootCommand.AddArgument(tagsArgument);
            
            // Set the handler
            _rootCommand.SetHandler((bool html, string settingsDiffPath, bool debug, bool trace, int span, string prefix, string jiraBaseUrl, string[] args) =>
            {
                var options = new CommandLineOptions
                {
                    HtmlOutput = html,
                    SettingsDiff = !string.IsNullOrEmpty(settingsDiffPath),
                    SettingsPath = settingsDiffPath,
                    DebugMode = debug,
                    TraceMode = trace,
                    SpanDays = span,
                    Prefix = prefix,
                    JiraBaseUrl = jiraBaseUrl,
                    Arguments = args.ToList()
                };
                
                ParseResult = options;
            }, htmlOption, settingsDiffOption, debugOption, traceOption, spanOption, prefixOption, jiraBaseUrlOption, tagsArgument);
        }
        
        /// <summary>
        /// Gets the parse result.
        /// </summary>
        public CommandLineOptions ParseResult { get; private set; } = new CommandLineOptions();
        
        /// <summary>
        /// Parses the command line arguments.
        /// </summary>
        /// <param name="args">The command line arguments.</param>
        /// <returns>The parse result.</returns>
        public CommandLineOptions Parse(string[] args)
        {
            _rootCommand.Invoke(args);
            return ParseResult;
        }
        
        /// <summary>
        /// Validates the command line options.
        /// </summary>
        /// <param name="options">The command line options.</param>
        /// <returns>True if the options are valid, false otherwise.</returns>
        public bool Validate(CommandLineOptions options)
        {
            if (options.Arguments.Count == 0)
            {
                _console.WriteError("Error: No tags or commands specified.");
                ShowUsage();
                return false;
            }
            
            if (options.Arguments.Count == 1)
            {
                string arg = options.Arguments[0];
                
                // Check if it's the "auto" or "stream" command
                if (arg == "auto" || arg == "stream")
                {
                    return true;
                }
                
                // Check if it's a valid type
                if (GitTagSelector.ValidTypes.Contains(arg))
                {
                    return true;
                }
                
                // Check if it's a tag that contains a valid type
                if (GitTagSelector.ValidTypes.Any(type => arg.Contains(type)))
                {
                    return true;
                }
                
                _console.WriteError($"Error: Invalid argument '{arg}'. Must be 'auto', a valid type ({string.Join(", ", GitTagSelector.ValidTypes)}), or a tag containing a valid type.");
                ShowUsage();
                return false;
            }
            
            if (options.Arguments.Count == 2)
            {
                // If first argument is "auto", second argument must be a valid type or not provided
                if (options.Arguments[0] == "auto")
                {
                    string type = options.Arguments[1];
                    if (!GitTagSelector.ValidTypes.Contains(type))
                    {
                        _console.WriteError($"Error: Invalid type '{type}'. Must be one of: {string.Join(", ", GitTagSelector.ValidTypes)}.");
                        ShowUsage();
                        return false;
                    }
                }
                
                return true;
            }
            
            if (options.Arguments.Count > 2)
            {
                _console.WriteError("Error: Too many arguments. Expected at most 2 arguments.");
                ShowUsage();
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Shows the usage information.
        /// </summary>
        public void ShowUsage()
        {
            // Get default values from configuration
            var appSettings = new AppSettings();
            BindConfiguration(_configuration, appSettings);
            
            _console.WriteLine("Usage:");
            _console.WriteLine("  CheckRelease [--html] [--settings-diff=<path>] [--debug] <tag1> <tag2>");
            _console.WriteLine("    - Use these two tags directly.");
            _console.WriteLine(string.Empty);
            _console.WriteLine("  CheckRelease [--html] [--settings-diff=<path>] [--debug] auto [type]");
            _console.WriteLine("    - Automatically find the last two weeks tags of the specified type and use them.");
            _console.WriteLine("    - If [type] is not provided, defaults to 'production'");
            _console.WriteLine(string.Empty);
            _console.WriteLine("  CheckRelease [--html] [--settings-diff=<path>] [--debug] stream [type]");
            _console.WriteLine($"    - Compare HEAD to a commit from --span days ago [default: {appSettings.SpanDays} days[]");
            _console.WriteLine("    - If [type] is provided, only consider commits containing that type");
            _console.WriteLine(string.Empty);
            _console.WriteLine("  CheckRelease [--html] [--settings-diff=<path>] [--debug] <type>");
            _console.WriteLine("    - Must be one of: production, uat, qa, dev");
            _console.WriteLine("    - Shows all tags of that type from the last month in an interactive menu.");
            _console.WriteLine("    - User must select exactly two.");
            _console.WriteLine(string.Empty);
            _console.WriteLine("  CheckRelease [--html] [--settings-diff=<path>] [--debug] <tag>");
            _console.WriteLine("    - Tag must contain one of the valid types.");
            _console.WriteLine("    - Finds all tags of the same type from the month prior to <tag>'s creation date.");
            _console.WriteLine("    - Shows them in a menu, user picks exactly one to pair with <tag>.");
            _console.WriteLine(string.Empty);
            _console.WriteLine("Options:");
            _console.WriteLine("  --html                   Generate HTML output instead of plain text");
            _console.WriteLine($"  --settings-diff <path>   Include a diff of appsettings.json between tags and specify the path [default: {appSettings.SettingsPath}]");
            _console.WriteLine("  --debug                  Enable debug mode with verbose output");
            _console.WriteLine("  --trace                  Enable trace mode");
            _console.WriteLine($"  --span <days>            Number of days to look back for tags [default: {appSettings.SpanDays} days]");
            _console.WriteLine($"  --prefix <text>          Prefix for JIRA tickets in commit messages [default: {appSettings.Prefix}]");
            _console.WriteLine($"  --jira-base-url <url>    Base URL for JIRA tickets [default: {appSettings.JiraBaseUrl}]");
            _console.WriteLine(string.Empty);
            _console.WriteLine("Examples:");
            _console.WriteLine("  CheckRelease --html --settings-diff=\"project-dir/appsettings.json\" auto > releases.html");
            _console.WriteLine("  CheckRelease --settings-diff=\"path/to/appsettings.json\" auto uat");
            _console.WriteLine("  CheckRelease production --settings-diff");
            _console.WriteLine("  CheckRelease --settings-diff=\"project-dir/appsettings.json\" release-1.2.3-uat");
            _console.WriteLine("  CheckRelease --html --settings-diff=\"project-dir/appsettings.json\" release-1.2.3-uat release-1.2.4-uat");
        }
        
        /// <summary>
        /// Represents the command line options for the CheckRelease application.
        /// </summary>
        public class CommandLineOptions
        {
            /// <summary>
            /// Gets or sets a value indicating whether to generate HTML output.
            /// </summary>
            public bool HtmlOutput { get; set; }
            
            /// <summary>
            /// Gets or sets a value indicating whether to include settings diff.
            /// </summary>
            public bool SettingsDiff { get; set; }
            
            /// <summary>
            /// Gets or sets the path to the appsettings.json file relative to the repository root.
            /// </summary>
            public string SettingsPath { get; set; } = string.Empty;
            
            /// <summary>
            /// Gets or sets a value indicating whether to enable debug mode.
            /// </summary>
            public bool DebugMode { get; set; }
            
            /// <summary>
            /// Gets or sets a value indicating whether to enable trace mode.
            /// </summary>
            public bool TraceMode { get; set; }
            
            /// <summary>
            /// Gets or sets the number of days to look back for tags.
            /// </summary>
            public int SpanDays { get; set; }
            
            /// <summary>
            /// Gets or sets the prefix for JIRA tickets in commit messages.
            /// </summary>
            public string Prefix { get; set; } = string.Empty;
            
            /// <summary>
            /// Gets or sets the base URL for JIRA tickets.
            /// </summary>
            public string JiraBaseUrl { get; set; } = string.Empty;
            
            /// <summary>
            /// Gets or sets the non-option arguments (tags or commands).
            /// </summary>
            public List<string> Arguments { get; set; } = new List<string>();
        }
    }
}
