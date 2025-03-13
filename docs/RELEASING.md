# Release Process for CheckRelease

This document outlines the process for creating new releases of the CheckRelease application.

## Semantic Versioning

CheckRelease follows [Semantic Versioning (SEMVER)](https://semver.org/) for version numbering. Versions follow the format: `MAJOR.MINOR.PATCH` (e.g., `1.2.3`).

For detailed information on when to increment each version component, refer to the [Versioning Guide](versioning-guide.md).

## Release Process

### 1. Prepare for Release

1. Ensure all tests are passing by running:
   ```bash
   dotnet test
   ```

2. Review the changes since the last release to determine the appropriate version increment (MAJOR, MINOR, or PATCH).

3. Update any documentation that needs to be updated for the new release.

### 2. Create and Push the Release Tag

1. Create a tag with the format `release/vX.Y.Z` where X.Y.Z is the semantic version:
   ```bash
   git tag release/v1.2.3
   ```

2. Push the tag to GitHub:
   ```bash
   git push origin release/v1.2.3
   ```

### 3. Automated Release Process

Once the tag is pushed, the GitHub Actions release workflow will automatically:

1. Extract the version number from the tag
2. Update the version in the project file
3. Run tests to ensure everything is working correctly
4. Build the application for all supported architectures
5. Create a GitHub Release with the appropriate version
6. Upload the built executables to the GitHub Release
7. Generate release notes based on commits since the previous release

You can monitor the progress of the release workflow in the "Actions" tab of the GitHub repository.

### 4. Post-Release Verification

After the release is created:

1. Verify that the GitHub Release was created correctly
2. Download and test the executables to ensure they work as expected
3. Check that the version number in the project file was updated correctly

### 5. Announce the Release

Once the release is verified:

1. Announce the release to stakeholders
2. Update any relevant documentation or websites
3. Consider posting about the release on social media or other channels

## Branch Protection and PR Requirements

To ensure code quality and prevent breaking changes, the main branch is protected with the following requirements:

1. Pull requests must be reviewed and approved before merging
2. The "Build and Test" workflow must pass before merging
3. Direct pushes to the main branch are not allowed

These protections help ensure that only high-quality, tested code is merged into the main branch.

## Troubleshooting

If the release workflow fails:

1. Check the workflow logs in the "Actions" tab of the GitHub repository
2. Fix any issues that caused the workflow to fail
3. Delete the tag, fix the issues, and create a new tag:
   ```bash
   git tag -d release/v1.2.3
   git push --delete origin release/v1.2.3
   # Fix issues
   git tag release/v1.2.3
   git push origin release/v1.2.3
   ```

## Pre-release Versions

For pre-release versions, append a hyphen and a series of dot-separated identifiers:

- Alpha: `release/v1.2.3-alpha.1`
- Beta: `release/v1.2.3-beta.1`
- Release Candidate: `release/v1.2.3-rc.1`

The release workflow will automatically mark these as pre-releases in GitHub.
