# Check Release

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4)](https://dotnet.microsoft.com/download/dotnet/9.0)
[![Version](https://img.shields.io/badge/Version-1.0.0-blue)](https://github.com/SkywardApps/check-release/releases)
[![Build Status](https://github.com/SkywardApps/check-release/actions/workflows/build.yml/badge.svg)](https://github.com/SkywardApps/check-release/actions/workflows/build.yml)
[![Release Status](https://github.com/SkywardApps/check-release/actions/workflows/release.yml/badge.svg)](https://github.com/SkywardApps/check-release/actions/workflows/release.yml)

A C# tool for analyzing and displaying changes between Git tags with JIRA ticket extraction and Slack unfurling support.

## Overview

Check Release is a command-line utility that analyzes commits between Git tags or specific commits, extracts JIRA tickets from commit messages, and outputs the results in plain text or HTML format. The HTML output includes meta tags for Slack unfurling, allowing changes to be visible directly in Slack without requiring users to click through to the page.

Originally developed as a bash script, Check Release has been ported to C# using .NET 9 and LibGit2Sharp, providing cross-platform compatibility and additional features.

## Features

- **Git Tag Analysis**: Analyze commits between Git tags or specific commits
- **JIRA Ticket Extraction**: Extract JIRA tickets (with configurable prefix) from commit messages
- **Multiple Output Formats**: Generate output in plain text or HTML
- **Slack Unfurling**: Include meta tags in HTML output for Slack unfurling
- **Multiple Selection Modes**:
  - Direct comparison between two tags
  - Automatic selection of recent tags
  - Stream mode for comparing HEAD to a historical commit
- **Settings Diff**: Compare settings files (appsettings.json) between tags
- **Cross-Platform Support**: Works on Windows, Linux, and macOS with both x64 and ARM64 architectures

## Installation

### Pre-built Binaries

Download the latest release for your platform from the [Releases](https://github.com/SkywardApps/check-release/releases) page.

### Building from Source

1. Ensure you have [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) installed
2. Clone the repository:
   ```
   git clone https://github.com/SkywardApps/check-release.git
   cd check-release
   ```
3. Build the project:
   ```
   dotnet build
   ```
4. Run the application:
   ```
   dotnet run
   ```

### Building Cross-Platform Binaries

The repository includes a build script that can generate self-contained executables for multiple platforms:

```bash
./build_check_release.sh
```

Run `./build_check_release.sh --help` for more options.

## Usage

```
Usage:
  CheckRelease [--html] [--settings-diff=<path>] [--debug] <tag1> <tag2>
    - Use these two tags directly.

  CheckRelease [--html] [--settings-diff=<path>] [--debug] auto [type]
    - Automatically find the last two weeks tags of the specified type and use them.
    - If [type] is not provided, defaults to 'production'

  CheckRelease [--html] [--settings-diff=<path>] [--debug] stream [type]
    - Compare HEAD to a commit from --span days ago [default: 42 days[]
    - If [type] is provided, only consider commits containing that type

  CheckRelease [--html] [--settings-diff=<path>] [--debug] <type>
    - Must be one of: production, uat, qa, dev
    - Shows all tags of that type from the last month in an interactive menu.
    - User must select exactly two.

  CheckRelease [--html] [--settings-diff=<path>] [--debug] <tag>
    - Tag must contain one of the valid types.
    - Finds all tags of the same type from the month prior to <tag>'s creation date.
    - Shows them in a menu, user picks exactly one to pair with <tag>.

Options:
  --html                   Generate HTML output instead of plain text
  --settings-diff <path>   Include a diff of appsettings.json between tags and specify the path
  --debug                  Enable debug mode with verbose output
  --trace                  Enable trace mode
  --span <days>            Number of days to look back for tags [default: 42 days]
  --prefix <text>          Prefix for JIRA tickets in commit messages
  --jira-base-url <url>    Base URL for JIRA tickets

Examples:
  CheckRelease --html --settings-diff="project-dir/appsettings.json" auto > releases.html
  CheckRelease --settings-diff="path/to/appsettings.json" auto uat
  CheckRelease production --settings-diff
  CheckRelease --settings-diff="project-dir/appsettings.json" release-1.2.3-uat
  CheckRelease --html --settings-diff="project-dir/appsettings.json" release-1.2.3-uat release-1.2.4-uat
```

## Configuration

The application uses a flexible configuration system that supports both command-line arguments and appsettings.json, with a clear priority order:

1. Command-line arguments
2. appsettings.json values
3. Default values

Example appsettings.json:

```json
{
  "SpanDays": 42,
  "Prefix": "JIRA-",
  "JiraBaseUrl": "https://your-jira-instance.atlassian.net/browse/"
}
```

## Cross-Platform Support

Check Release is designed to be cross-platform, with support for:

- Windows (x64, ARM64)
- Linux (x64, ARM64)
- macOS (x64, ARM64)

## Versioning

This project uses [Semantic Versioning (SEMVER)](https://semver.org/) with the following guidelines:

### Major Version (X.0.0)
- Breaking changes to command-line options
- Changes to appsettings.json format that are not backward compatible
- Modifications to output format that would break existing integrations

### Minor Version (0.Y.0)
- New features and enhancements that maintain backward compatibility
- Additional command-line options that don't affect existing behavior
- New output formats or templates

### Patch Version (0.0.Z)
- Bug fixes and refinements that don't change functionality
- Performance improvements
- Documentation updates

## Contributing

Contributions are welcome! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for details on how to contribute to this project.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- Originally developed as a bash script by Skyward App Company LLC
- Ported to C# using .NET 9 and LibGit2Sharp
