# Check Release Usage Guide

This guide provides detailed examples and explanations for using the Check Release tool in various scenarios.

## Table of Contents

- [Basic Usage](#basic-usage)
- [Selection Modes](#selection-modes)
  - [Direct Comparison](#direct-comparison)
  - [Auto Mode](#auto-mode)
  - [Stream Mode](#stream-mode)
  - [Type Mode](#type-mode)
  - [Single Tag Mode](#single-tag-mode)
- [Output Options](#output-options)
  - [Plain Text Output](#plain-text-output)
  - [HTML Output](#html-output)
- [Settings Diff](#settings-diff)
- [Additional Options](#additional-options)
- [Examples](#examples)

## Basic Usage

Check Release is a command-line tool that analyzes commits between Git tags or specific commits, extracts JIRA tickets from commit messages, and outputs the results in plain text or HTML format.

The basic syntax is:

```
CheckRelease [options] <selection mode and arguments>
```

## Selection Modes

Check Release offers several modes for selecting which commits to analyze:

### Direct Comparison

Compare two specific tags directly:

```
CheckRelease <tag1> <tag2>
```

Example:
```
CheckRelease release-1.2.3-production release-1.3.0-production
```

This will analyze all commits between `release-1.2.3-production` and `release-1.3.0-production`.

### Auto Mode

Automatically find and use the most recent tags of a specified type:

```
CheckRelease auto [type]
```

If `[type]` is not provided, it defaults to `production`.

Example:
```
CheckRelease auto uat
```

This will find the two most recent UAT tags and analyze the commits between them.

### Stream Mode

Compare the current HEAD to a commit from a specified number of days ago:

```
CheckRelease stream [type]
```

If `[type]` is provided, only commits containing that type will be considered.

Example:
```
CheckRelease stream --span=30
```

This will compare HEAD to a commit from 30 days ago.

### Type Mode

Show all tags of a specific type from the last month in an interactive menu:

```
CheckRelease <type>
```

The type must be one of: production, uat, qa, dev.

Example:
```
CheckRelease qa
```

This will show all QA tags from the last month in an interactive menu, allowing you to select exactly two for comparison.

### Single Tag Mode

Find all tags of the same type from the month prior to a specific tag's creation date:

```
CheckRelease <tag>
```

The tag must contain one of the valid types.

Example:
```
CheckRelease release-1.2.3-uat
```

This will find all UAT tags from the month prior to the creation date of `release-1.2.3-uat` and show them in a menu, allowing you to pick exactly one to pair with the specified tag.

## Output Options

### Plain Text Output

By default, Check Release outputs in plain text format, which is suitable for console viewing or redirecting to a text file.

Example:
```
CheckRelease auto > release-notes.txt
```

### HTML Output

Use the `--html` flag to generate HTML output, which includes meta tags for Slack unfurling:

```
CheckRelease --html auto > release-notes.html
```

The HTML output includes:
- A title with the tag names
- Meta tags for Slack unfurling
- A list of JIRA tickets with descriptions
- Settings diff (if requested)

## Settings Diff

Use the `--settings-diff` option to include a diff of appsettings.json between tags:

```
CheckRelease --settings-diff="path/to/appsettings.json" auto
```

This will analyze the differences in the specified settings file between the selected tags and include them in the output.

## Additional Options

- `--debug`: Enable debug mode with verbose output
- `--trace`: Enable trace mode
- `--span <days>`: Number of days to look back for tags (default: 42 days)
- `--prefix <text>`: Prefix for JIRA tickets in commit messages
- `--jira-base-url <url>`: Base URL for JIRA tickets

## Examples

### Generate HTML Release Notes for Production

```
CheckRelease --html auto production > production-release.html
```

### Compare UAT Tags with Settings Diff

```
CheckRelease --html --settings-diff="project-dir/appsettings.json" auto uat > uat-release.html
```

### Stream Mode for Recent Changes

```
CheckRelease --html stream --span=14 > recent-changes.html
```

### Direct Comparison with Debug Output

```
CheckRelease --debug --html release-1.2.3-production release-1.3.0-production > release-notes.html
```

### Interactive Selection of QA Tags

```
CheckRelease qa
```

### Custom JIRA Prefix and Base URL

```
CheckRelease --prefix="ABC-" --jira-base-url="https://company.atlassian.net/browse/" auto
```
