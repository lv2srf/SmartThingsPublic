# OBD-II Diagnostic Tool - Setup Guide
## Complete Installation and Configuration

---

## Table of Contents
1. [System Requirements](#system-requirements)
2. [Hardware Setup](#hardware-setup)
3. [Windows Installation](#windows-installation)
4. [Android Installation](#android-installation)
5. [Building from Source](#building-from-source)
6. [Troubleshooting](#troubleshooting)
7. [FAQ](#faq)

---

## System Requirements

### Windows
- **Operating System**: Windows 10 or later (64-bit)
- **RAM**: 4 GB minimum, 8 GB recommended
- **Storage**: 100 MB free space
- **.NET Runtime**: .NET 6.0 or later (included in installer)
- **USB Port**: For USB OBD adapters
- **Bluetooth**: For Bluetooth OBD adapters

### Android
- **Operating System**: Android 8.0 (Oreo) or later
- **RAM**: 2 GB minimum
- **Storage**: 50 MB free space
- **Bluetooth**: Required for wireless adapters
- **USB OTG**: Optional, for USB adapters

### OBD-II Adapter
**Compatible Adapters**:
- ✅ ELM327 v1.5 or later (USB, Bluetooth, WiFi)
- ✅ OBDLink MX/MX+
- ✅ Veepeak OBD Check
- ✅ MUCAR-compatible adapters
- ✅ Any ELM327-based adapter

**Not Recommended**:
- ❌ Cheap counterfeit ELM327 (v1.3 or lower)
- ❌ WiFi adapters (more complex setup)
- ❌ Non-ELM327 adapters

---

## Hardware Setup

### Step 1: Purchase Compatible Adapter

**Recommended Adapters** (in order of preference):

1. **OBDLink MX+** ($99)
   - Professional grade
   - Excellent Bluetooth range
   - Fast response time
   - Best compatibility

2. **Veepeak OBD Check BLE+** ($29)
   - Good value
   - Reliable Bluetooth
   - Works with most vehicles

3. **Generic ELM327 v1.5 USB** ($15)
   - Budget option
   - Requires USB cable
   - Reliable for basic diagnostics

### Step 2: Locate OBD-II Port

**2008 GMC Yukon Denali**:
- Port location: Under driver's side dashboard
- Position: Above brake pedal, to the left
- Look for: 16-pin trapezoidal connector

**Finding the port**:
1. Sit in driver's seat
2. Look under dashboard on left side
3. May have a cover that pops off
4. Port will be facing down

### Step 3: Connect Adapter

**For all adapter types**:
1. Ensure vehicle is OFF
2. Plug adapter firmly into OBD-II port
3. Adapter LED should light up
4. Turn ignition to ON position (engine can stay off)

**LED Indicators**:
- Solid red/green: Power on
- Blinking blue: Bluetooth discoverable
- Rapid blinking: Attempting connection

---

## Windows Installation

### Method 1: Installer (Recommended)

1. **Download**
   - Visit the releases page
   - Download `OBDReader-Setup.exe`
   - File size: ~25 MB

2. **Install**
   - Run `OBDReader-Setup.exe`
   - Follow installation wizard
   - Choose installation directory
   - Create desktop shortcut (recommended)
   - Click "Install"

3. **Launch**
   - Desktop shortcut OR
   - Start Menu → OBD Reader

### Method 2: Portable Version

1. **Download**
   - Download `OBDReader-Portable.zip`
   - Extract to any folder

2. **Run**
   - Open extracted folder
   - Double-click `OBDReader.exe`
   - No installation required!

### First Launch Setup

1. **Grant Permissions**
   - Windows Firewall: Allow access (for future features)
   - USB device access: Automatic

2. **Select COM Port**
   - For USB adapters:
     - Open Device Manager
     - Expand "Ports (COM & LPT)"
     - Note the COM port (e.g., COM3)

   - For Bluetooth adapters:
     - Settings → Bluetooth → Pair device
     - Default PIN: `1234` or `0000`
     - Check Device Manager for COM port
     - You may see two ports - use the "Outgoing" port

3. **Connect**
   - Turn vehicle ignition ON
   - Select COM port in dropdown
   - Click "Connect"
   - Wait 5-10 seconds
   - Green indicator = Connected!

### Updating

**Auto-Update** (coming soon):
- App will check for updates on startup
- Click "Download Update" when prompted

**Manual Update**:
1. Download latest release
2. Install over existing version
3. Settings and preferences preserved

---

## Android Installation

### Method 1: Google Play Store (Coming Soon)

Will be available in Google Play Store after beta testing.

### Method 2: Direct APK Install

1. **Enable Unknown Sources**
   - Settings → Security
   - Enable "Install unknown apps"
   - Or per-app: Settings → Apps → Your Browser → Install unknown apps

2. **Download APK**
   - On your Android device
   - Visit the releases page
   - Download `OBDReader.apk`
   - File size: ~15 MB

3. **Install**
   - Open Downloads folder
   - Tap `OBDReader.apk`
   - Review permissions
   - Tap "Install"
   - Tap "Open" when complete

### Permissions Setup

**Required Permissions**:

1. **Bluetooth** - To connect to OBD adapter
   - Tap "Allow" when prompted

2. **Location** - Required by Android for Bluetooth scanning
   - Tap "Allow" when prompted
   - Note: App does NOT track your location

3. **Nearby Devices** (Android 12+)
   - Tap "Allow" when prompted

### Connecting Bluetooth Adapter

1. **Pair Adapter**
   - Settings → Bluetooth
   - Turn on Bluetooth
   - Adapter should appear (e.g., "OBD-II", "ELM327")
   - Tap to pair
   - Enter PIN: `1234` or `0000`
   - Pairing successful!

2. **Connect in App**
   - Open OBD Reader app
   - Tap Bluetooth icon (top right)
   - Select your paired adapter
   - Wait for connection
   - Green indicator = Connected!

### Android-Specific Tips

- Keep phone close to adapter (<10 meters)
- Close other Bluetooth apps
- Disable battery optimization for OBD Reader:
  - Settings → Apps → OBD Reader → Battery → Unrestricted

---

## Building from Source

### Prerequisites

**For Windows**:
- Visual Studio 2022
- .NET 6.0 SDK
- NuGet Package Manager

**For Android**:
- Android Studio
- JDK 17
- Android SDK (API 26+)
- Gradle 8.0+

### Windows Build

1. **Clone Repository**
```bash
git clone https://github.com/yourusername/OBDReader.git
cd OBDReader/Windows
```

2. **Restore Packages**
```bash
dotnet restore
```

3. **Build**
```bash
dotnet build --configuration Release
```

4. **Run**
```bash
dotnet run
```

**Output**: `bin/Release/net6.0-windows/OBDReader.exe`

### Android Build

1. **Open Project**
   - Launch Android Studio
   - File → Open
   - Select `OBDReader/Android`

2. **Sync Gradle**
   - Android Studio will auto-sync
   - Wait for dependencies to download

3. **Build APK**
   - Build → Build Bundle(s) / APK(s) → Build APK(s)
   - Wait for build to complete
   - Click "locate" to find APK

**Output**: `app/build/outputs/apk/release/app-release.apk`

### Development Tips

**Hot Reload (Windows)**:
```bash
dotnet watch run
```

**Debug Mode**:
- Set breakpoints in Visual Studio
- F5 to start debugging
- Monitor Output window for logs

**Android Debugging**:
- Enable USB Debugging on phone
- Connect via USB
- Click "Run" in Android Studio
- Select your device

---

## Troubleshooting

### Windows Issues

#### "Cannot find COM port"
**Problem**: COM port not visible in dropdown

**Solutions**:
1. Check Device Manager for port number
2. Unplug and replug adapter
3. Try different USB port
4. Update drivers:
   - Device Manager → Right-click adapter → Update driver
   - Search automatically for drivers

#### "Connection timeout"
**Problem**: App cannot connect to adapter

**Solutions**:
1. Turn vehicle ignition ON
2. Wait 5 seconds, try again
3. Reset adapter (unplug and replug)
4. Check COM port is correct
5. For Bluetooth: Check pairing in Windows settings

#### "Access denied to COM port"
**Problem**: Port already in use

**Solutions**:
1. Close other OBD apps
2. Close apps that might use serial ports
3. Restart computer if issue persists

#### ".NET Runtime not found"
**Problem**: .NET 6.0 not installed

**Solutions**:
1. Download .NET 6.0 Desktop Runtime
2. Install from: https://dotnet.microsoft.com/download
3. Choose "Desktop Runtime" (not SDK)
4. Restart computer

### Android Issues

#### "Cannot pair Bluetooth adapter"
**Problem**: Pairing fails or requests PIN

**Solutions**:
1. Common PINs to try:
   - `1234`
   - `0000`
   - `6789`
2. Reset adapter by unplugging for 10 seconds
3. Clear Bluetooth cache:
   - Settings → Apps → Bluetooth → Storage → Clear Cache
4. Restart phone

#### "Permission denied"
**Problem**: App cannot access Bluetooth

**Solutions**:
1. Grant all permissions:
   - Settings → Apps → OBD Reader → Permissions
   - Enable Bluetooth, Location, Nearby Devices
2. Enable Location services (required for Bluetooth on Android)
3. Reinstall app if issue persists

#### "Connection unstable / keeps dropping"
**Problem**: Bluetooth disconnects randomly

**Solutions**:
1. Move phone closer to adapter
2. Disable battery optimization:
   - Settings → Apps → OBD Reader → Battery
   - Select "Unrestricted"
3. Keep screen on while using
4. Some cheap adapters have poor Bluetooth chips - consider upgrading

#### "App crashes on launch"
**Problem**: Immediate crash

**Solutions**:
1. Clear app data:
   - Settings → Apps → OBD Reader → Storage → Clear Data
2. Check Android version (8.0+ required)
3. Reinstall app
4. Report crash with error log

### Vehicle-Specific Issues

#### "No data from vehicle"
**Problem**: Connected but no live data

**Solutions**:
1. **Start the engine** - Most PIDs require running engine
2. Check adapter compatibility with 2008 GMC Yukon
3. Try reading DTCs (works with engine off)

#### "Some parameters show 0"
**Problem**: Certain values always zero

**Solutions**:
1. That PID may not be supported by your vehicle
2. Start engine if showing 0
3. Some values only active under certain conditions

#### "VIN not reading"
**Problem**: Vehicle info shows "Not available"

**Solutions**:
1. Try multiple times (can be slow)
2. Some ECUs don't support Mode 09
3. This is normal for some vehicles

---

## FAQ

### General Questions

**Q: Will this void my vehicle warranty?**
A: No. Reading diagnostics through OBD-II port is legal and does not affect warranty. However, clearing codes or using bidirectional controls should be done carefully.

**Q: Can I leave the adapter plugged in?**
A: Not recommended. Some adapters drain battery if left plugged in when vehicle is off. Unplug when not in use.

**Q: Does this work on other vehicles?**
A: Yes! While optimized for 2008 GMC Yukon Denali, it works on any vehicle with CAN bus (most 2008+ vehicles).

**Q: What's the difference between USB and Bluetooth adapters?**
A:
- **USB**: More reliable, faster, requires cable
- **Bluetooth**: Wireless, convenient, potential connection issues with cheap adapters

**Q: Is an internet connection required?**
A: No. App works completely offline. All diagnostics done locally.

### Technical Questions

**Q: What protocol does my vehicle use?**
A: 2008 GMC Yukon Denali uses ISO 15765-4 (CAN bus). The adapter will auto-detect this.

**Q: What's the update rate for live data?**
A: 2 Hz (500ms) by default. Some PIDs may be slower depending on ECU response time.

**Q: Can I log data?**
A: Coming in future update. Currently, no built-in data logging.

**Q: Does this support J2534 pass-through programming?**
A: No. This is a diagnostic tool, not a programming interface.

**Q: Can I read airbag, ABS, or transmission codes?**
A: Currently focuses on engine/powertrain codes. Support for other modules coming in future update.

### Safety Questions

**Q: Is it safe to use while driving?**
A: You can monitor data while driving, but do not interact with the app. Have a passenger operate it for safety.

**Q: What happens if I clear codes?**
A: Clears Check Engine Light and resets readiness monitors. If the problem persists, codes will return. Only clear after repairs.

**Q: Can bidirectional tests damage my vehicle?**
A: When used properly, no. Only use tests you understand, and follow on-screen warnings.

---

## Additional Resources

### Video Tutorials
- Coming soon: YouTube channel with setup guides

### Community Support
- Forum: [Coming soon]
- Discord: [Coming soon]

### Documentation
- User Guide: See USER_GUIDE.md
- Technical Docs: See TECHNICAL_DOCUMENTATION.md
- API Reference: See source code comments

---

## Getting Help

### Before Contacting Support

1. Check this guide's Troubleshooting section
2. Review the User Guide
3. Search existing issues on GitHub

### Reporting Issues

Include:
- Operating system and version
- OBD adapter model
- Vehicle year, make, model
- Error message or screenshot
- Steps to reproduce

### Contact

- GitHub Issues: [Preferred method]
- Email: [Coming soon]
- Forum: [Coming soon]

---

**Version 1.0**
**Last Updated**: 2025-01-04
**License**: Apache 2.0
