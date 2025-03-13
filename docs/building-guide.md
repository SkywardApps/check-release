# Building Check Release

This guide provides detailed instructions for building Check Release from source on different platforms.

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) or later
- Git (for cloning the repository)

## Building from Source

### Clone the Repository

```bash
git clone https://github.com/SkywardApps/check-release.git
cd check-release
```

### Building with .NET CLI

The simplest way to build the project is using the .NET CLI:

```bash
dotnet build
```

This will build the project in debug mode. To build in release mode:

```bash
dotnet build -c Release
```

### Running the Application

To run the application directly:

```bash
dotnet run
```

To run with arguments:

```bash
dotnet run -- auto production
```

Note the `--` separator between `dotnet run` and the application arguments.

### Running Tests

To run the tests:

```bash
dotnet test
```

## Building Cross-Platform Binaries

Check Release includes a build script (`build_check_release.sh`) that can generate self-contained executables for multiple platforms.

### Using the Build Script

```bash
./build_check_release.sh
```

This will build self-contained executables for all supported platforms:
- Windows (x64, ARM64)
- Linux (x64, ARM64)
- macOS (x64, ARM64)

### Build Script Options

The build script supports several options:

```bash
./build_check_release.sh --help
```

- `--help`, `-h`: Show usage information
- `--list`, `-l`: List available platforms
- `--clean`, `-c`: Clean build directories before building
- `--force`, `-f`: Force build even if tests fail

You can also specify specific platforms to build for:

```bash
./build_check_release.sh win-x64 linux-x64
```

### Build Output

The build script creates a `check_release_dist` directory with subdirectories for each platform, containing the self-contained executables.

## Platform-Specific Instructions

### Windows

On Windows, you can use either the .NET CLI or Visual Studio:

#### Using Visual Studio

1. Open `CheckRelease.sln` in Visual Studio
2. Select the desired configuration (Debug/Release)
3. Build the solution (F6 or Build > Build Solution)
4. Run the application (F5 or Debug > Start Debugging)

#### Using PowerShell

You can also use the included `run.bat` script:

```powershell
.\run.bat auto
```

### macOS/Linux

On macOS and Linux, ensure the build script has executable permissions:

```bash
chmod +x build_check_release.sh
```

Then run the build script:

```bash
./build_check_release.sh
```

### Docker

You can also build and run Check Release in a Docker container:

```bash
# Build a Docker image
docker build -t check-release .

# Run the container
docker run --rm -v $(pwd):/repo check-release auto
```

## Troubleshooting

### Common Issues

#### Missing .NET SDK

If you get an error about missing .NET SDK, ensure you have installed .NET 9 SDK:

```bash
dotnet --version
```

#### Permission Denied on Linux/macOS

If you get a "permission denied" error when trying to run the executable on Linux or macOS, ensure the file has executable permissions:

```bash
chmod +x check_release_dist/linux-x64/check_release
```

#### LibGit2Sharp Native Library Issues

If you encounter issues with LibGit2Sharp native libraries, ensure you're using a self-contained build or have the correct native libraries installed.

### Getting Help

If you encounter any issues building Check Release, please open an issue on the GitHub repository with:

- Your operating system and version
- .NET SDK version
- The command you ran
- The full error message
