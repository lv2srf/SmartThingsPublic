# OBD-II CAN Bus Diagnostic Tool

A professional, full-featured bidirectional OBD-II diagnostic application for 2008 GMC Yukon Denali with ISO 15765-4 (CAN bus) protocol support.

## Features

### Core Capabilities
- **Bidirectional Communication**: Full read/write support for all OBD-II service modes
- **ISO 15765-4 (CAN bus)**: Native CAN protocol implementation
- **Real-time Monitoring**: Live sensor data with customizable dashboards
- **Diagnostic Trouble Codes**: Read, decode, and clear DTCs with detailed descriptions
- **Freeze Frame Data**: Capture and analyze freeze frame data
- **Actuator Tests**: Bidirectional control for component testing
- **GM-Specific Support**: Enhanced support for 2008 GMC Yukon Denali

### Supported OBD-II Modes
- **Mode 01**: Current powertrain diagnostic data (live data)
- **Mode 02**: Freeze frame data
- **Mode 03**: Read diagnostic trouble codes
- **Mode 04**: Clear diagnostic trouble codes
- **Mode 05**: Oxygen sensor test results
- **Mode 06**: On-board monitoring test results
- **Mode 07**: Pending diagnostic trouble codes
- **Mode 08**: Control of on-board systems
- **Mode 09**: Vehicle information (VIN, calibration IDs)
- **Mode 0A**: Permanent diagnostic trouble codes

### Platform Support
- **Windows**: Native WPF application with modern UI
- **Android**: Native Android application
- **Serial Communication**: USB, Bluetooth, and WiFi OBD adapters

## Hardware Requirements

### Compatible OBD-II Adapters
- ELM327-based adapters (USB, Bluetooth, WiFi)
- OBDLink MX/MX+
- Veepeak OBD adapters
- MUCAR-compatible interfaces

### Vehicle Requirements
- 2008 GMC Yukon Denali (optimized)
- Any CAN bus compatible vehicle (2008+)
- OBD-II port (standard on all US vehicles 1996+)

## Architecture

```
OBDReader/
├── Core/               # Core protocol implementation
│   ├── Protocol/       # ISO 15765-4, ELM327
│   ├── Services/       # OBD service modes
│   ├── Models/         # Data models
│   └── Utilities/      # Helpers
├── Windows/            # Windows WPF application
├── Android/            # Android application
├── Shared/             # Shared UI components
└── Docs/              # Documentation
```

## Quick Start

### Windows
1. Connect OBD-II adapter to vehicle
2. Connect adapter to PC via USB/Bluetooth
3. Launch OBDReader.exe
4. Select COM port and connect
5. Start diagnostics

### Android
1. Connect Bluetooth OBD-II adapter
2. Pair adapter with Android device
3. Launch OBD Reader app
4. Select adapter and connect
5. Start diagnostics

## Development

### Prerequisites
- .NET 6.0+ SDK
- Visual Studio 2022 (Windows)
- Android Studio (Android development)

### Build
```bash
# Windows
cd Windows
dotnet build

# Android
cd Android
./gradlew build
```

## License
Apache 2.0

## Author
Created for 2008 GMC Yukon Denali diagnostic and monitoring
