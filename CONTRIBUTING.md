# Contributing to Check Release

Thank you for your interest in contributing to Check Release! We welcome contributions from everyone and are grateful for any help you can offer.

## Getting Started

### Development Environment Setup

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
4. Run the tests:
   ```
   dotnet test
   ```

### Project Structure

- `Program.cs` - Entry point and main workflow orchestration
- `CommandLineParser.cs` - Command-line argument parsing and validation
- `GitTagSelector.cs` - Git repository interaction and tag/commit selection
- `CommitAnalyzer.cs` - Commit message analysis and JIRA ticket extraction
- `OutputGenerator.cs` - Output generation in plain text and HTML formats
- `SettingsDiffGenerator.cs` - JSON configuration comparison
- `AppSettings.cs` - Configuration management
- `Adapters/` - Implementation of interfaces
- `Domain/` - Domain objects
- `Interfaces/` - Interface definitions
- `Testing/` - Mock implementations for testing

## How to Contribute

### Reporting Bugs

If you find a bug, please open an issue with:
- A clear description of the problem
- Steps to reproduce
- Expected vs. actual behavior
- Any relevant logs or screenshots

### Suggesting Enhancements

We welcome suggestions for new features or improvements:
- Describe the enhancement clearly
- Explain why it would be valuable
- Suggest an implementation approach if possible

### Pull Requests

1. Fork the repository
2. Create a branch for your changes
3. Make your changes
4. Add or update tests as needed
5. Ensure all tests pass
6. Submit a pull request

### Pull Request Guidelines

- Keep changes focused and atomic
- Follow the existing code style
- Include tests for new functionality
- Update documentation as needed
- Add a clear description of your changes

### Continuous Integration

All pull requests are automatically built and tested using GitHub Actions. The following checks will run:

1. **Build and Test**: Runs the `build_check_release.sh` script to:
   - Execute all tests
   - Build the application for all supported architectures
   - Ensure everything compiles correctly

Pull requests cannot be merged until all checks pass. This helps maintain code quality and ensures that changes don't break existing functionality.

### Branch Protection

The main branch is protected with the following rules:

1. Pull requests must be reviewed and approved before merging
2. The "Build and Test" workflow must pass before merging
3. Direct pushes to the main branch are not allowed

These protections help ensure that only high-quality, tested code is merged into the main branch.

## Versioning and Release Guidelines

This project follows [Semantic Versioning (SEMVER)](https://semver.org/). When contributing, please indicate the type of change:

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

For detailed information about the release process, please see [RELEASING.md](docs/RELEASING.md).

## Coding Standards

We value clean, readable code but don't want to burden contributors with overly strict guidelines. Here are some basic principles:

- Use meaningful names for variables, methods, and classes
- Write self-documenting code where possible
- Add comments to explain WHY, not WHAT
- Follow the existing patterns in the codebase
- Write tests for your code

## Testing

- Run existing tests to ensure your changes don't break anything:
  ```
  dotnet test
  ```
- Add new tests for new functionality
- Follow the existing test patterns (Arrange-Act-Assert)

## Communication

- Use GitHub issues for bug reports and feature requests
- Be respectful and considerate in all communications
- Ask questions if you're unsure about anything

## License

By contributing to this project, you agree that your contributions will be licensed under the project's MIT License.

Thank you for contributing to Check Release!
