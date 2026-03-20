# MidiServiceRestarter

*(Please note: This is an unofficial tool and is not affiliated with or endorsed by Microsoft.)*

A simple, lightweight Windows WPF application to restart the modern Windows MIDI Service (`MidiSrv`) and view connected MIDI devices.

When working with DAWs or MIDI hardware, the Windows MIDI service can sometimes become unresponsive or fail to detect new devices. This utility provides a quick and easy way to check the current status of the service, monitor connected MIDI devices, and restart the service with administrator privileges—all without navigating through the Windows Services management console.

## Features

- **Service Management:** Displays the real-time status of the Windows MIDI Service and restarts it with a single click.
- **Device Enumeration:** Automatically lists all connected MIDI IN and OUT devices.
- **Auto-Update:** Optional 5-second auto-refresh toggle to keep the device list up-to-date.
- **Standalone Support:** Can be built as a self-contained executable that runs without requiring the .NET SDK installed on the target machine.

## Requirements

- Windows 11 .
- Administrator privileges (The app will automatically request elevation via UAC when starting).
- *Note: Please ensure all DAWs and MIDI applications are closed before restarting the service to prevent crashes or lockups in those applications.*

## Build and Publish

To build a standalone `.exe` executable that you can distribute to others:

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
