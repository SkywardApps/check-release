# Check Release Versioning Guide

Check Release follows [Semantic Versioning (SEMVER)](https://semver.org/) for version numbering. This guide explains how we apply SEMVER principles to this project.

## Version Format

Versions follow the format: `MAJOR.MINOR.PATCH` (e.g., `1.2.3`)

- **MAJOR**: Incremented for incompatible changes that require users to modify how they use the tool
- **MINOR**: Incremented for new features that are backward compatible
- **PATCH**: Incremented for backward compatible bug fixes

## When to Increment Each Version Component

### Major Version (X.0.0)

Increment the major version when you make incompatible changes that would break existing usage patterns. For Check Release, this includes:

- **Command-line Interface Changes**:
  - Removing or renaming existing command-line options
  - Changing the behavior of existing options in a way that breaks existing scripts
  - Changing the required format of arguments

- **Configuration Format Changes**:
  - Modifying the structure of appsettings.json in a non-backward compatible way
  - Removing configuration options without fallbacks

- **Output Format Changes**:
  - Altering the HTML or text output format in ways that would break existing integrations
  - Changing the meta tag structure for Slack unfurling
  - Modifying the JSON structure of any machine-readable outputs

- **Core Functionality Changes**:
  - Changing the Git tag naming convention requirements
  - Modifying how JIRA tickets are extracted from commit messages
  - Changing the behavior of tag selection modes

### Minor Version (0.Y.0)

Increment the minor version when you add functionality in a backward compatible manner. For Check Release, this includes:

- **New Features**:
  - Adding new command-line options that don't affect existing behavior
  - Introducing new tag selection modes
  - Adding new output formats or templates
  - Implementing new ways to extract information from commits

- **Enhancements**:
  - Improving existing features without changing their core behavior
  - Adding new settings to appsettings.json (with sensible defaults)
  - Enhancing the HTML output with additional information
  - Adding support for new platforms

- **Deprecations**:
  - Marking features as deprecated (but still functional)
  - Adding warnings about future breaking changes

### Patch Version (0.0.Z)

Increment the patch version when you make backward compatible bug fixes. For Check Release, this includes:

- **Bug Fixes**:
  - Fixing incorrect behavior in existing functionality
  - Addressing edge cases in tag selection
  - Correcting issues in JIRA ticket extraction
  - Fixing formatting problems in output

- **Performance Improvements**:
  - Optimizing algorithms without changing behavior
  - Reducing memory usage
  - Improving execution speed

- **Documentation Updates**:
  - Correcting or enhancing documentation
  - Improving error messages
  - Adding code comments

- **Internal Refactoring**:
  - Code cleanup that doesn't affect external behavior
  - Dependency updates that don't change functionality

## Pre-release Versions

For pre-release versions, append a hyphen and a series of dot-separated identifiers:

- Alpha: `1.2.3-alpha.1`
- Beta: `1.2.3-beta.1`
- Release Candidate: `1.2.3-rc.1`

## Examples

### Major Version Increment

- Changing `--settings-diff` to require a different format for the path
- Removing the `auto` mode without a replacement
- Changing how tag types are specified

### Minor Version Increment

- Adding a new `--format` option for additional output formats
- Implementing a new tag selection mode
- Adding support for custom JIRA ticket formats
- Enhancing HTML output with additional metadata

### Patch Version Increment

- Fixing a bug in the JIRA ticket extraction logic
- Correcting an issue with date handling in tag selection
- Improving error messages for invalid inputs
- Optimizing the performance of settings diff generation

## Version Management in the Codebase

### Where Versions are Defined

The version number is defined in the following locations:

1. `CheckRelease.csproj` file
2. Release tags in the GitHub repository

### How to Update the Version

When making changes, follow these steps:

1. Determine the type of change (major, minor, patch)
2. Update the version in the `CheckRelease.csproj` file
3. Document the changes in the release notes
4. Create a tag with the new version when releasing

## For Contributors

When submitting pull requests:

1. Indicate in your PR description whether your changes would require a major, minor, or patch version increment
2. Explain why you believe your changes fall into that category
3. If your changes would require a major version increment, provide migration guidance for users

## Version History

Check the [Releases](https://github.com/SkywardApps/check-release/releases) page for a complete version history and release notes.
