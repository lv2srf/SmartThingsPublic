# OBD-II Diagnostic Tool - User Guide
## For 2008 GMC Yukon Denali

### Table of Contents
1. [Introduction](#introduction)
2. [Hardware Setup](#hardware-setup)
3. [Software Installation](#software-installation)
4. [Connecting to Your Vehicle](#connecting-to-your-vehicle)
5. [Using the Dashboard](#using-the-dashboard)
6. [Reading Diagnostic Codes](#reading-diagnostic-codes)
7. [Clearing Diagnostic Codes](#clearing-diagnostic-codes)
8. [Freeze Frame Data](#freeze-frame-data)
9. [Vehicle Information](#vehicle-information)
10. [Bidirectional Control Tests](#bidirectional-control-tests)
11. [Troubleshooting](#troubleshooting)

---

## Introduction

The OBD-II Diagnostic Tool is a professional-grade application designed for real-time vehicle diagnostics and monitoring. It provides full bidirectional communication with your 2008 GMC Yukon Denali's Engine Control Unit (ECU) and other control modules.

### Key Features
- **Real-time Data Monitoring**: View live engine parameters
- **Diagnostic Trouble Codes**: Read, decode, and clear DTCs
- **Freeze Frame Data**: Capture snapshots of vehicle state when errors occur
- **Bidirectional Control**: Test actuators and components
- **GM-Specific Support**: Optimized for 2008 GMC Yukon Denali

---

## Hardware Setup

### Required Equipment
1. **OBD-II Adapter** (one of the following):
   - ELM327 USB adapter
   - ELM327 Bluetooth adapter
   - OBDLink MX/MX+
   - Any ELM327-compatible device

2. **Device**:
   - Windows PC (Windows 10 or later) OR
   - Android device (Android 8.0 or later)

### Locating the OBD-II Port

Your 2008 GMC Yukon Denali's OBD-II port is located:
1. Under the driver's side dashboard
2. Above the brake pedal
3. Look for a 16-pin trapezoidal connector

### Connecting the Adapter

#### For USB Adapters:
1. Plug the adapter into the OBD-II port
2. Connect USB cable to your Windows PC
3. Wait for drivers to install (if first time)

#### For Bluetooth Adapters:
1. Plug the adapter into the OBD-II port
2. Turn on vehicle ignition (engine can be off for initial setup)
3. On your device:
   - **Windows**: Go to Settings > Bluetooth > Add Device
   - **Android**: Go to Settings > Bluetooth > Pair New Device
4. Look for device name (usually "OBD-II" or "ELM327")
5. Default PIN is usually `1234` or `0000`

---

## Software Installation

### Windows
1. Download the latest release from the releases page
2. Extract the ZIP file to a folder
3. Run `OBDReader.exe`
4. No installation required - it's portable!

### Android
1. Download the APK file
2. Enable "Install from Unknown Sources" in Settings
3. Tap the APK file to install
4. Grant Bluetooth and Location permissions when prompted

---

## Connecting to Your Vehicle

### Windows Application

1. **Start the engine** (recommended for full functionality)
2. Launch OBDReader
3. Select your COM port from the dropdown:
   - USB adapters: Usually COM3, COM4, etc.
   - Bluetooth: Check Windows Device Manager for the COM port number
4. Click **Connect**
5. Wait for connection (5-10 seconds)
6. You should see:
   - Green connection indicator
   - Battery voltage displayed
   - "Connected" status message

### Android Application

1. **Start the engine** (recommended)
2. Launch OBD Reader app
3. Tap the Bluetooth icon in the top-right
4. Select your paired OBD adapter from the list
5. Wait for connection
6. Connection indicator will turn green

---

## Using the Dashboard

The Dashboard tab displays real-time vehicle data:

### Key Metrics

#### Engine RPM
- Normal idle: 600-800 RPM
- Displays engine revolutions per minute
- Updates every 0.5 seconds

#### Vehicle Speed
- In km/h (multiply by 0.621 for mph)
- Real-time speed from ECU

#### Coolant Temperature
- Normal operating: 85-95°C
- **Warning**: If > 105°C, pull over safely and check coolant

#### Throttle Position
- 0% = Closed (idle)
- 100% = Wide open throttle
- Shows accelerator pedal position

#### Engine Load
- Percentage of maximum engine torque
- Useful for diagnosing performance issues

#### Fuel Level
- Percentage of tank capacity
- May not be extremely accurate

#### Intake Air Temperature
- Temperature of air entering engine
- Affects fuel mixture calculations

#### Battery Voltage
- Normal: 13.5-14.5V (engine running)
- Normal: 12.4-12.8V (engine off)
- **Warning**: If < 12V (engine running), check charging system

### Live Data Grid

Below the key metrics, you'll find a scrollable list of all available parameters from your vehicle.

---

## Reading Diagnostic Codes

### What are Diagnostic Trouble Codes (DTCs)?

DTCs are codes stored by your vehicle's ECU when it detects a problem. They consist of:
- **Letter**: System type (P=Powertrain, C=Chassis, B=Body, U=Network)
- **Number**: Specific fault code (e.g., P0301 = Cylinder 1 Misfire)

### Reading Codes

1. Go to **Trouble Codes** tab
2. Click **Read Codes**
3. Wait 2-5 seconds
4. Codes will appear in a list with:
   - Code number (e.g., P0301)
   - Description (e.g., "Cylinder 1 Misfire Detected")
   - Type (Powertrain, Chassis, etc.)
   - Status (Confirmed, Pending, Permanent)

### Understanding Code Status

- **Confirmed**: Active fault that triggered the Check Engine Light
- **Pending**: Fault detected once, not yet confirmed (light may not be on)
- **Permanent**: Serious fault that cannot be cleared manually

### Common 2008 GMC Yukon Denali Codes

| Code | Description | Common Cause |
|------|-------------|--------------|
| P0300 | Random Misfire | Spark plugs, coils, fuel system |
| P0420 | Catalyst Efficiency Low (Bank 1) | Catalytic converter, O2 sensors |
| P0128 | Coolant Temperature Low | Thermostat stuck open |
| P0171 | System Too Lean (Bank 1) | Vacuum leak, MAF sensor |
| P0401 | EGR Flow Insufficient | EGR valve, carbon buildup |

---

## Clearing Diagnostic Codes

### Important Warnings ⚠️

**DO NOT** clear codes without addressing the underlying problem!
- Clearing codes does NOT fix the issue
- Codes will return if the fault persists
- Clearing codes resets readiness monitors (will fail emissions test)

### When to Clear Codes

✅ **Do clear codes**:
- After completing repairs
- To verify repair was successful
- As instructed by a mechanic

❌ **Don't clear codes**:
- Just to turn off the check engine light
- Before getting an emissions test
- Without knowing what the code means

### How to Clear Codes

1. Go to **Trouble Codes** tab
2. Click **Clear Codes**
3. Confirm the warning dialog
4. Wait for confirmation message
5. Drive the vehicle to verify codes don't return

### After Clearing

Your vehicle will need to complete "drive cycles" to reset readiness monitors:
- Drive normally for 50-100 miles
- Include city and highway driving
- Include cold starts and warm engine operation

---

## Freeze Frame Data

### What is Freeze Frame Data?

When a fault occurs, the ECU saves a "snapshot" of all sensor data at that moment. This helps diagnose intermittent problems.

### Reading Freeze Frame

1. Go to **Freeze Frame** tab
2. Click **Read Freeze Frame**
3. View captured data including:
   - Engine RPM at fault
   - Vehicle speed at fault
   - Coolant temperature
   - Throttle position
   - All other sensor readings

### Using Freeze Frame Data

Compare freeze frame data to current data to understand conditions when the fault occurred.

**Example**: P0301 (Misfire Cylinder 1)
- Freeze frame shows RPM: 2500, Speed: 100 km/h, Load: 75%
- **Analysis**: Misfire occurs under load at highway speed
- **Likely cause**: Spark plug or coil failing under load

---

## Vehicle Information

### Available Information

#### VIN (Vehicle Identification Number)
- 17-character unique identifier
- Useful for parts lookup and registration

#### Calibration ID
- ECU software version
- Needed when updating ECU firmware

#### Protocol
- Should show: "ISO 15765-4 (CAN)"
- Confirms correct communication protocol

### Reading Vehicle Info

1. Go to **Vehicle Info** tab
2. Click **Read Vehicle Info**
3. Wait 5-10 seconds
4. Information will populate

---

## Bidirectional Control Tests

### ⚠️ Safety Warning

**Bidirectional controls actively operate vehicle systems!**

Only use these features:
- ✅ When safely parked
- ✅ In a well-ventilated area
- ✅ With engine running (when required)
- ✅ If you understand what the test does

### EVAP System Test

**Purpose**: Tests for leaks in the evaporative emission system

**How it works**: ECU closes EVAP valves and pressurizes the system to detect leaks

**When to use**:
- Codes P0442 (small leak) or P0455 (large leak)
- Failed emissions test for EVAP

**Procedure**:
1. Engine must be running
2. Fuel tank 1/4 to 3/4 full
3. Go to **Actuator Tests** tab
4. Click **Run EVAP Test**
5. Test takes 2-5 minutes
6. Check for new codes after test

### Catalytic Converter Test

**Purpose**: Tests catalyst efficiency

**How it works**: ECU monitors upstream and downstream O2 sensors

**When to use**:
- Code P0420 or P0430 (catalyst efficiency)
- Before replacing expensive catalytic converter

**Procedure**:
1. Engine must be at operating temperature
2. Go to **Actuator Tests** tab
3. Click **Run Catalyst Test**
4. Drive vehicle normally for 5-10 minutes
5. Check monitoring test results

---

## Troubleshooting

### Cannot Connect to Vehicle

**Problem**: "Unable to connect" or timeout error

**Solutions**:
1. **Check physical connection**
   - Adapter fully seated in OBD port
   - USB cable secure (if USB adapter)
   - Bluetooth paired (if Bluetooth adapter)

2. **Check vehicle**
   - Ignition ON (engine can be off for testing)
   - Battery voltage > 11V
   - Try starting the engine

3. **Check COM port (Windows)**
   - Wrong COM port selected
   - Try each available port
   - Check Device Manager for correct port

4. **Reset adapter**
   - Unplug adapter from OBD port
   - Wait 10 seconds
   - Plug back in
   - Try connecting again

### Data Not Updating

**Problem**: Values stuck at 0 or not changing

**Solutions**:
1. **Engine must be running** for most sensors
2. Click **Disconnect** then **Connect** to reset
3. Check vehicle is in READY mode (not accessory)

### Incorrect Readings

**Problem**: Values seem wrong (e.g., RPM showing 16000)

**Solutions**:
1. Some adapters have scaling issues
2. Try different adapter brand/model
3. Update adapter firmware if possible

### Bluetooth Connection Drops

**Problem**: Connection unstable on Android

**Solutions**:
1. Keep adapter and phone close (< 10 meters)
2. Remove metal objects between devices
3. Ensure phone Bluetooth is not connected to other devices
4. Some cheap adapters have poor Bluetooth range

### No DTCs Found (but check engine light is on)

**Problem**: Light is on but no codes show

**Solutions**:
1. Some codes are manufacturer-specific
2. Try reading **Pending Codes**
3. Try reading **Permanent Codes**
4. Use dealer-level scan tool for GM-specific codes

---

## Technical Specifications

### Supported Protocols
- ISO 15765-4 (CAN bus) - Primary protocol for 2008 GMC Yukon Denali
- Auto-detect other protocols if needed

### Update Rate
- Live data: 2 Hz (500ms refresh)
- Adjustable based on performance

### Supported PIDs
- Over 60 standard OBD-II PIDs
- Optimized for GM vehicles

### Data Logging
- Coming in future update

---

## Tips for Best Results

1. **Always start engine** for accurate real-time data
2. **Warm up vehicle** before running diagnostic tests
3. **Clear codes only after repairs** - don't just silence the light
4. **Document freeze frame data** before clearing codes
5. **Regular monitoring** can catch problems early

---

## Support

### Need Help?

- Check the troubleshooting section above
- Consult your vehicle's service manual
- Visit forums for 2008 GMC Yukon Denali owners

### Reporting Issues

Include:
- Windows or Android version
- OBD adapter model
- Error message or screenshot
- Steps to reproduce the problem

---

## Legal Disclaimer

This software is provided for diagnostic and educational purposes. Always consult a certified mechanic for repairs. Improper use of bidirectional controls can damage vehicle systems. Use at your own risk.

---

**Version 1.0**
**Last Updated**: 2025-01-04
**For**: 2008 GMC Yukon Denali
**Protocol**: ISO 15765-4 (CAN bus)
