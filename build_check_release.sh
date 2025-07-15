#!/usr/bin/env bash

# build_check_release.sh - Build script for CheckRelease
# Creates self-contained executables for multiple platforms

set -euo pipefail

# Configuration
PROJECT_DIR="."
OUTPUT_NAME="check_release"
CONFIGURATION="Release"
BUILD_DIR="$PROJECT_DIR/publish"
DIST_DIR="check_release_dist"

# Define target platforms
ALL_PLATFORMS=(
  "win-x64:check_release.win-x64.exe"
  "win-arm64:check_release.win-arm64.exe"
  "linux-x64:check_release.linux-x64"
  "linux-arm64:check_release.linux-arm64"
  "osx-x64:check_release.osx-x64"  
  "osx-arm64:check_release.osx-arm64"
)

# Function to run tests
run_tests() {
  echo "Running tests..."
  dotnet test
  return $?
}

# Function to display usage information
show_usage() {
  echo "Usage: $0 [options] [platform1 platform2 ...]"
  echo
  echo "Options:"
  echo "  --help, -h       Show this help message"
  echo "  --list, -l       List available platforms"
  echo "  --clean, -c      Clean build directories before building"
  echo "  --force, -f      Force build even if tests fail"
  echo
  echo "If no platforms are specified, builds for all supported platforms."
  echo "Available platforms:"
  for platform_info in "${ALL_PLATFORMS[@]}"; do
    RID="${platform_info%%:*}"
    echo "  - $RID"
  done
  echo
  echo "Examples:"
  echo "  $0                     # Build for all platforms"
  echo "  $0 win-x64 linux-x64   # Build only for Windows and Linux (x64)"
  echo "  $0 --clean             # Clean and build for all platforms"
  exit 0
}

# Function to list available platforms
list_platforms() {
  echo "Available platforms:"
  for platform_info in "${ALL_PLATFORMS[@]}"; do
    RID="${platform_info%%:*}"
    PLATFORM_OUTPUT="${platform_info#*:}"
    echo "  - $RID ($PLATFORM_OUTPUT)"
  done
  exit 0
}

# Function to clean build directories
clean_build() {
  echo "Cleaning build directories..."
  rm -rf "$BUILD_DIR"
  rm -rf "$DIST_DIR"
  echo "Clean complete."
}

# Parse command line arguments
CLEAN=false
FORCE=false
PLATFORMS=()

while [[ $# -gt 0 ]]; do
  case "$1" in
    --help|-h)
      show_usage
      ;;
    --list|-l)
      list_platforms
      ;;
    --clean|-c)
      CLEAN=true
      shift
      ;;
    --force|-f)
      FORCE=true
      shift
      ;;
    -*)
      echo "Unknown option: $1"
      show_usage
      ;;
    *)
      # Check if the specified platform is valid
      VALID=false
      for platform_info in "${ALL_PLATFORMS[@]}"; do
        RID="${platform_info%%:*}"
        if [[ "$1" == "$RID" ]]; then
          PLATFORMS+=("$platform_info")
          VALID=true
          break
        fi
      done
      
      if [[ "$VALID" == "false" ]]; then
        echo "Error: Unknown platform '$1'"
        echo "Use --list to see available platforms"
        exit 1
      fi
      
      shift
      ;;
  esac
done

# If no platforms specified, use all platforms
if [[ ${#PLATFORMS[@]} -eq 0 ]]; then
  PLATFORMS=("${ALL_PLATFORMS[@]}")
fi

# Clean if requested
if [[ "$CLEAN" == "true" ]]; then
  clean_build
fi

# Run tests first
run_tests
TEST_RESULT=$?

if [[ $TEST_RESULT -ne 0 ]]; then
  if [[ "$FORCE" == "true" ]]; then
    echo "WARNING: Tests failed, but continuing with build due to --force option"
  else
    echo "ERROR: Tests failed. Build aborted."
    echo "Use --force option to build anyway."
    exit 1
  fi
fi

echo "Building CheckRelease as self-contained executables for ${#PLATFORMS[@]} platforms..."

# Create output directory
mkdir -p "$BUILD_DIR"

# Build for each platform
for platform_info in "${PLATFORMS[@]}"; do
  # Split the platform info into RID and output name
  RID="${platform_info%%:*}"
  PLATFORM_OUTPUT="${platform_info#*:}"
  
  echo "Building for $RID..."
  
  # Create platform-specific output directory
  PLATFORM_DIR="$BUILD_DIR/$RID"
  mkdir -p "$PLATFORM_DIR"
  
  # Build the project
  dotnet publish "$PROJECT_DIR/CheckRelease.csproj" \
    -c "$CONFIGURATION" \
    -r "$RID" \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:PublishTrimmed=true \
    -p:EnableCompressionInSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -p:DebugType=None \
    -p:DebugSymbols=false \
    -o "$PLATFORM_DIR"
  
  # Copy and rename the executable to the platform directory
  if [ -f "$PLATFORM_DIR/CheckRelease" ]; then
    mv "$PLATFORM_DIR/CheckRelease" "$PLATFORM_DIR/$PLATFORM_OUTPUT"
  elif [ -f "$PLATFORM_DIR/CheckRelease.exe" ]; then
    mv "$PLATFORM_DIR/CheckRelease.exe" "$PLATFORM_DIR/$PLATFORM_OUTPUT"
  fi
  
  # Set executable permissions for non-Windows platforms
  if [[ "$RID" != win-* ]]; then
    chmod +x "$PLATFORM_DIR/$PLATFORM_OUTPUT"
  fi
  
  echo "✓ Build for $RID completed"
done

# Create a directory structure for easy distribution
echo "Creating distribution directory..."
DIST_DIR="check_release_dist"
mkdir -p "$DIST_DIR"

# Copy executables to distribution directory
for platform_info in "${PLATFORMS[@]}"; do
  RID="${platform_info%%:*}"
  PLATFORM_OUTPUT="${platform_info#*:}"
  
  mkdir -p "$DIST_DIR/$RID"
  cp "$BUILD_DIR/$RID/$PLATFORM_OUTPUT" "$DIST_DIR/$RID/"
  
  echo "✓ Copied $PLATFORM_OUTPUT to $DIST_DIR/$RID/"
done

echo "Build complete! Executables are available in the '$DIST_DIR' directory"
echo "Platform-specific executables can be found in their respective subdirectories:"

# List available platforms
for platform_info in "${PLATFORMS[@]}"; do
  RID="${platform_info%%:*}"
  PLATFORM_OUTPUT="${platform_info#*:}"
  echo "  - $RID/$PLATFORM_OUTPUT"
done
