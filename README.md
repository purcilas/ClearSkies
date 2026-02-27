# Shader Cache Cleaner

A native Windows application to clean NVIDIA and DirectX shader caches, perfect for preparing your system for games like Microsoft Flight Simulator 2024.

## Features

- **Multi-Cache Support**: Cleans multiple shader cache types:
  - NVIDIA DirectX Shader Cache
  - NVIDIA OpenGL Shader Cache
  - NVIDIA GPU Cache
  - DirectX Shader Cache (D3DSCache)
  - AMD Shader Caches (DX11 & DX12)

- **Cache Size Display**: Shows the size of each cache before cleaning

- **Selective Cleaning**: Choose which caches to clean with checkboxes

- **Live Deletion Log**: Real-time log showing exactly which files are being deleted
  - See every file removed during cleanup
  - Track skipped files (locked/in-use)
  - View summary statistics

- **Automatic Scheduling**: Set up automatic cache cleaning using Windows Task Scheduler
  - Daily, Weekly, or Monthly schedules
  - Custom time selection

- **Safe Cleaning**:
  - Files in use by applications are automatically skipped
  - No data loss - only shader cache files are removed

## Requirements

- Windows 10/11
- .NET 8.0 Runtime
- Administrator privileges (for some caches and scheduling)

## Installation

1. Download the latest release
2. Extract to a folder of your choice
3. Run `ShaderCacheCleaner.exe`

## Usage

### Manual Cleaning

1. Launch the application
2. Click **"Scan Caches"** to detect all shader caches
3. Select the caches you want to clean (or use **All**/**None** buttons)
4. Click **"Clean Selected"** to remove the cached files
5. Confirm the deletion when prompted
6. Watch the live log at the bottom to see which files are being deleted in real-time
7. Use **"Clear Log"** to clear the deletion log

### Scheduled Cleaning

1. Click the **"Schedule..."** button
2. Enable **"Enable Automatic Cache Cleaning"**
3. Choose frequency (Daily, Weekly, or Monthly)
4. Set the time for automatic cleaning
5. Click **"Apply"**

**Note**: Creating scheduled tasks requires Administrator privileges. Right-click the application and select "Run as Administrator" if needed.

### Command Line

The application supports silent cleaning via command line:

```bash
ShaderCacheCleaner.exe /clean
```

This is used by the Task Scheduler for automatic cleaning.

## Why Clean Shader Caches?

Shader caches can accumulate over time and consume significant disk space. Cleaning them can:

- Free up disk space (often several GB)
- Resolve graphics issues or corruption
- Improve game performance in some cases
- Prepare for major updates (like MSFS 2024)

Games will automatically rebuild shader caches as needed, so it's safe to delete them.

## Building from Source

### Prerequisites

- Visual Studio 2022 or later
- .NET 8.0 SDK

### Build Steps

```bash
cd ShaderCacheCleaner
dotnet build
```

Or open the solution in Visual Studio and build normally.

## Technical Details

- Built with C# and Windows Forms
- Uses .NET 8.0
- Integrates with Windows Task Scheduler via `schtasks.exe`
- Calculates directory sizes recursively
- Handles in-use files gracefully

## Troubleshooting

**"Not running as Administrator" warning**:
- Some caches (especially NVIDIA NV_Cache in ProgramData) require admin rights
- Right-click the app and select "Run as Administrator"

**Scheduled task creation fails**:
- Ensure you're running as Administrator
- Check Windows Event Viewer for Task Scheduler errors

**Some files won't delete**:
- This is normal - files in use by running applications are skipped
- Close graphics-intensive applications before cleaning
- Some system files may be locked by Windows

## License

This project is open source and free to use.

## Author

Created for cleaning shader caches before MSFS 2024 installations.

## Version

1.0.0 - Initial Release
