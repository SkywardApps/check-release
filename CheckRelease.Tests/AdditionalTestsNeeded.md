# Unit Test Coverage for CheckRelease

## Implemented Tests

We have successfully implemented unit tests for the following components:

### 1. CommitAnalyzer Tests ✅
- Extracting JIRA tickets from merge commits
- Skipping commits with F flag

### 2. GitTagSelector Tests ✅
- Direct comparison mode (two specific tags)
- Auto mode (recent tags of specified type)
- Stream mode (comparing HEAD to historical commit)
- Finding recent tags
- Finding tags before a specific date
- Sorting tags chronologically
- Multiple tag pair selection

### 3. SettingsDiffGenerator Tests ✅
- Detecting added properties
- Detecting removed properties
- Handling nested objects
- Handling arrays
- Censoring sensitive values
- HTML output formatting
- Error handling (file not found, invalid JSON)

### 4. CommandLineParser Tests ✅
- Parsing direct comparison mode
- Parsing auto mode
- Parsing stream mode
- Handling optional flags
- Using default values
- Validating arguments
- Handling invalid inputs

### 5. OutputGenerator Tests ✅
- Plain text output generation
- HTML output generation
- Meta tag generation for Slack unfurling
- Character limit handling for descriptions
- Multiple tag pair output
- Settings diff inclusion in output

### 6. AppSettings Tests ✅
- Default values
- Loading from configuration
- Partial configuration
- Command line overrides
- Error handling for invalid values

## Tests Still Needed for Complete Coverage

### 1. Integration Tests

Integration tests should test the end-to-end workflow of the application. These tests would use the MockGitRepository but would test the entire workflow from command-line parsing to output generation.

Example test methods:
```csharp
public void EndToEnd_DirectMode_GeneratesCorrectOutput()
public void EndToEnd_AutoMode_GeneratesCorrectOutput()
public void EndToEnd_StreamMode_GeneratesCorrectOutput()
public void EndToEnd_WithSettingsDiff_IncludesDiffInOutput()
```

### 2. LibGit2SharpRepository Tests

While we have a MockGitRepository for testing, we should also test the actual LibGit2SharpRepository implementation to ensure it correctly interacts with Git repositories.

Example test methods:
```csharp
public void GetAllTags_ReturnsCorrectTags()
public void GetCommitsBetween_ReturnsCorrectCommits()
public void GetFileContentAtReference_ReturnsCorrectContent()
```

These tests would require a real Git repository for testing, which could be set up in a temporary directory during the test.

## Test Implementation Patterns

All implemented tests follow these patterns:
- Use the Arrange-Act-Assert pattern
- Use descriptive test names that indicate what is being tested
- Test both happy paths and error conditions
- Use the MockGitRepository where appropriate

## Current Test Coverage

The current test suite includes 53 tests covering all the core functionality of the application. This provides a solid foundation for ensuring the reliability of the application as it continues to evolve.

### JIRA Base URL Tests

We've added specific tests to verify that the JIRA base URL specified on the command line is correctly used in the HTML output:

1. `JiraBaseUrl_FromCommandLine_IsUsedInHtmlOutput` - Tests that the OutputGenerator correctly uses the provided base URL
2. `JiraBaseUrl_FromCommandLine_IsPassedToOutputGenerator` - Tests that the mock repository setup works correctly with the OutputGenerator
3. `Parse_JiraBaseUrlOption_SetsCorrectProperty` - Tests that the command line parser correctly parses the --jira-base-url option

These tests ensure that when a user specifies a custom JIRA base URL using the `--jira-base-url` option, it is correctly passed through to the HTML output.
