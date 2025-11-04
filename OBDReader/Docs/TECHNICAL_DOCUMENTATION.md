# OBD-II Diagnostic Tool - Technical Documentation

## Architecture Overview

### System Design

The OBD-II Diagnostic Tool follows a layered architecture pattern:

```
┌─────────────────────────────────────────┐
│         Presentation Layer              │
│    (Windows WPF / Android Compose)      │
├─────────────────────────────────────────┤
│         Business Logic Layer            │
│        (OBDService, Models)             │
├─────────────────────────────────────────┤
│         Protocol Layer                  │
│     (ISO15765, ELM327 Interface)        │
├─────────────────────────────────────────┤
│         Transport Layer                 │
│    (Serial Port / Bluetooth)            │
└─────────────────────────────────────────┘
```

---

## Core Components

### 1. Protocol Layer

#### ISO15765.cs
Implements ISO 15765-4 (CAN bus) protocol specification.

**Key Features**:
- CAN frame parsing and construction
- Multi-frame message segmentation
- Flow control handling
- Single frame optimization

**Frame Types**:
```csharp
public enum FrameType
{
    SingleFrame,      // 0x0X - Up to 7 bytes
    FirstFrame,       // 0x1X - Start of multi-frame
    ConsecutiveFrame, // 0x2X - Continuation
    FlowControl       // 0x3X - Flow control
}
```

**Example Usage**:
```csharp
// Build single frame request
byte[] data = { 0x01, 0x0C }; // Mode 01, PID 0x0C (RPM)
byte[] frame = ISO15765.BuildSingleFrame(data);

// Parse response
var (frameType, dataLength, responseData) = ISO15765.ParseCANFrame(frame);
```

#### ELM327.cs
ELM327 adapter communication interface.

**Key Features**:
- Serial port management
- AT command processing
- Protocol auto-detection
- Voltage monitoring

**Connection Flow**:
1. Open serial port
2. Reset adapter (ATZ)
3. Disable echo (ATE0)
4. Set protocol (ATSP0 for auto)
5. Enable headers (ATH1)
6. Verify communication

**Example Usage**:
```csharp
var adapter = new ELM327("COM3", 38400);
await adapter.ConnectAsync();

// Send OBD request
byte[] response = await adapter.SendOBDRequestAsync(0x01, 0x0C);

// Read voltage
double voltage = await adapter.ReadVoltageAsync();
```

---

### 2. Service Layer

#### OBDService.cs
High-level OBD-II diagnostic service implementation.

**Supported Service Modes**:

| Mode | Description | Implementation |
|------|-------------|----------------|
| 0x01 | Current Data | `ReadCurrentDataAsync()` |
| 0x02 | Freeze Frame | `ReadFreezeFrameAsync()` |
| 0x03 | Read DTCs | `ReadDiagnosticCodesAsync()` |
| 0x04 | Clear DTCs | `ClearDiagnosticCodesAsync()` |
| 0x05 | O2 Sensor Tests | `ReadO2SensorTestResultsAsync()` |
| 0x06 | Monitoring Tests | `ReadMonitoringTestResultsAsync()` |
| 0x07 | Pending DTCs | `ReadPendingCodesAsync()` |
| 0x08 | Control Operation | `RequestSystemControlAsync()` |
| 0x09 | Vehicle Info | `ReadVINAsync()`, `ReadCalibrationIDAsync()` |
| 0x0A | Permanent DTCs | `ReadPermanentCodesAsync()` |

**Example Usage**:
```csharp
var service = new OBDService(adapter);
await service.ConnectAsync();

// Read current RPM
double rpm = await service.ReadCurrentDataAsync(0x0C);

// Read all DTCs
var codes = await service.ReadDiagnosticCodesAsync();

// Clear codes
bool success = await service.ClearDiagnosticCodesAsync();
```

---

### 3. Data Models

#### OBDPIDs.cs
Comprehensive PID definitions with formulas.

**PID Definition Structure**:
```csharp
public class PIDDefinition
{
    public byte PID { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Unit { get; set; }
    public Func<byte[], double> Formula { get; set; }
    public int ByteCount { get; set; }
    public double MinValue { get; set; }
    public double MaxValue { get; set; }
    public string Category { get; set; }
}
```

**Formula Examples**:

```csharp
// Engine RPM (PID 0x0C)
// Formula: ((A * 256) + B) / 4
Formula = data => ((data[0] * 256 + data[1]) / 4.0)

// Coolant Temperature (PID 0x05)
// Formula: A - 40
Formula = data => (data[0] - 40)

// Engine Load (PID 0x04)
// Formula: (A * 100) / 255
Formula = data => (data[0] * 100.0 / 255.0)
```

#### DiagnosticTroubleCode.cs
DTC decoding and management.

**DTC Format**:
```
P 0 3 0 1
│ │ │ │ └─ Specific fault code
│ │ │ └─── Subsystem
│ │ └───── Component
│ └─────── Generic (0) or Manufacturer (1)
└───────── System: P=Powertrain, C=Chassis, B=Body, U=Network
```

**Decoding Logic**:
```csharp
// First byte: [Type:2][Digit1:2][Digit2:4]
// Second byte: [Digit3:4][Digit4:4]

int typeBits = (firstByte >> 6) & 0x03;
int firstDigit = (firstByte >> 4) & 0x03;
int secondDigit = firstByte & 0x0F;
int thirdDigit = (secondByte >> 4) & 0x0F;
int fourthDigit = secondByte & 0x0F;
```

---

## Communication Protocol

### Request/Response Flow

```
Application                  ELM327                   ECU
    │                           │                      │
    ├──► "01 0C\r"             │                      │
    │   (Request Engine RPM)    │                      │
    │                           ├──► CAN Frame         │
    │                           │    [7E0 02 01 0C]    │
    │                           │                      │
    │                           │    ◄──── CAN Frame   │
    │                           │         [7E8 04 41 0C 1A F8]
    │   ◄──── "41 0C 1A F8"    │                      │
    │   (Response: 1726 RPM)    │                      │
```

### CAN Message Format

**Standard 11-bit CAN ID**:
```
Request:  0x7E0 (Engine ECU)
Response: 0x7E8 (Engine ECU reply)

Format: [CAN_ID] [Length] [Data...]
Example: 7E0 02 01 0C
         │   │  │  └─ PID (0x0C = RPM)
         │   │  └──── Mode (0x01 = Current Data)
         │   └─────── Length (2 bytes)
         └─────────── CAN ID (ECU address)
```

**Response Format**:
```
Response: 7E8 04 41 0C 1A F8
          │   │  │  │  └──┴─ Data bytes (RPM value)
          │   │  │  └─────── PID echo
          │   │  └────────── Mode + 0x40 (0x41)
          │   └───────────── Length (4 bytes)
          └───────────────── CAN ID (ECU response)
```

### Multi-Frame Messages

For messages > 7 bytes:

**First Frame**:
```
10 13 49 02 01 XX XX XX XX
│  │  │  │  │
│  │  │  │  └─ Data byte 1
│  │  │  └──── PID
│  │  └─────── Mode
│  └────────── Total length (0x13 = 19 bytes)
└───────────── First Frame (0x10)
```

**Flow Control** (from application):
```
30 00 00 XX XX XX XX XX
│  │  │
│  │  └─ Separation time (0 = no delay)
│  └──── Block size (0 = send all)
└─────── Flow Control - Continue to Send
```

**Consecutive Frames**:
```
21 XX XX XX XX XX XX XX
│  └──┴──┴──┴──┴──┴──┴─ 7 data bytes
└──────────────────────── Consecutive frame, sequence 1

22 XX XX XX XX XX XX XX
│  └──┴──┴──┴──┴──┴──┴─ 7 data bytes
└──────────────────────── Consecutive frame, sequence 2
```

---

## Windows Application (WPF)

### Architecture

**MVVM Pattern**:
- **Model**: Core protocol and service classes
- **View**: XAML UI definitions
- **ViewModel**: Data binding and UI logic (implicitly in code-behind for simplicity)

### Key Components

#### MainWindow.xaml
Material Design themed interface with:
- TabControl for different views
- Real-time data dashboard
- DTC management
- Freeze frame viewer
- Vehicle information
- Actuator tests

#### MainWindow.xaml.cs
Event handlers and data management:
```csharp
private async void DataUpdateTimer_Tick(object sender, EventArgs e)
{
    // Read multiple PIDs in parallel
    var pidData = await obdService.ReadMultiplePIDsAsync(
        0x0C, 0x0D, 0x05, 0x11, 0x04, 0x2F, 0x0F
    );

    // Update UI on dispatcher thread
    Dispatcher.Invoke(() => {
        RPMText.Text = pidData[0x0C].ToString("F0");
        SpeedText.Text = pidData[0x0D].ToString("F0");
        // ... update other fields
    });
}
```

### Threading Model

- **UI Thread**: WPF main thread, handles rendering and user interaction
- **Background Tasks**: Async/await pattern for OBD communication
- **Timer**: DispatcherTimer for periodic data updates (500ms)

### Data Binding

```csharp
public class LiveDataItem
{
    public string Name { get; set; }
    public string Value { get; set; }
    public string Unit { get; set; }
}

// In XAML:
<DataGrid ItemsSource="{Binding LiveDataItems}">
    <DataGrid.Columns>
        <DataGridTextColumn Binding="{Binding Name}" />
        <DataGridTextColumn Binding="{Binding Value}" />
        <DataGridTextColumn Binding="{Binding Unit}" />
    </DataGrid.Columns>
</DataGrid>
```

---

## Android Application

### Architecture

**Jetpack Compose** - Modern declarative UI framework

### Key Components

#### MainActivity.kt
Main activity with Bluetooth management:
```kotlin
private fun connectToDevice(device: BluetoothDevice) {
    lifecycleScope.launch(Dispatchers.IO) {
        // Create RFCOMM socket
        val uuid = UUID.fromString("00001101-0000-1000-8000-00805F9B34FB")
        bluetoothSocket = device.createRfcommSocketToServiceRecord(uuid)
        bluetoothSocket?.connect()

        // Initialize ELM327
        sendCommand("ATZ\r")
        sendCommand("ATE0\r")
        // ...
    }
}
```

#### Composable UI Components

**Dashboard Screen**:
```kotlin
@Composable
fun DashboardScreen(rpm: Int, speed: Int, coolantTemp: Int) {
    Column {
        Row {
            MetricCard("RPM", rpm.toString(), "RPM", Color.Cyan)
            MetricCard("Speed", speed.toString(), "km/h", Color.Green)
        }
        // ... more metrics
    }
}
```

**State Management**:
```kotlin
var isConnected by remember { mutableStateOf(false) }
var rpm by remember { mutableStateOf(0) }

// Update state triggers recomposition
LaunchedEffect(key1 = isConnected) {
    while (isConnected) {
        rpm = readRPM()
        delay(500)
    }
}
```

### Bluetooth Communication

**Serial Port Protocol (SPP)**:
- UUID: `00001101-0000-1000-8000-00805F9B34FB`
- RFCOMM channel
- 8 data bits, 1 stop bit, no parity

**Permissions Required**:
- `BLUETOOTH_CONNECT`
- `BLUETOOTH_SCAN`
- `ACCESS_FINE_LOCATION` (for Bluetooth device discovery)

---

## Extending the Application

### Adding New PIDs

1. **Add definition to OBDPIDs.cs**:
```csharp
[0x5C] = new PIDDefinition
{
    PID = 0x5C,
    Name = "Engine Oil Temperature",
    Description = "Engine oil temperature",
    Unit = "°C",
    ByteCount = 1,
    Formula = data => (data[0] - 40),
    MinValue = -40,
    MaxValue = 215,
    Category = "Temperature"
}
```

2. **Add to UI**:
```csharp
// In DataUpdateTimer_Tick:
var pidData = await obdService.ReadMultiplePIDsAsync(
    0x0C, 0x0D, 0x05, 0x5C  // Added 0x5C
);

if (pidData.ContainsKey(0x5C))
    OilTempText.Text = pidData[0x5C].ToString("F0");
```

### Adding GM-Specific Codes

GM uses manufacturer-specific modes and PIDs beyond standard OBD-II.

**Example - Mode 0x22 (Enhanced Diagnostics)**:
```csharp
public async Task<byte[]> ReadGMPIDAsync(ushort pid)
{
    byte pidHigh = (byte)(pid >> 8);
    byte pidLow = (byte)(pid & 0xFF);

    return await adapter.SendOBDRequestAsync(0x22, pidHigh, pidLow);
}
```

### Implementing Data Logging

```csharp
public class DataLogger
{
    private StreamWriter logFile;

    public void StartLogging(string filename)
    {
        logFile = new StreamWriter(filename);
        logFile.WriteLine("Timestamp,RPM,Speed,Coolant,Throttle");
    }

    public void LogData(Dictionary<byte, double> pidData)
    {
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        logFile.WriteLine($"{timestamp},{pidData[0x0C]},{pidData[0x0D]},{pidData[0x05]},{pidData[0x11]}");
    }

    public void StopLogging()
    {
        logFile?.Close();
    }
}
```

---

## Performance Optimization

### Request Batching

Instead of individual requests:
```csharp
// Slow - individual requests
double rpm = await ReadCurrentDataAsync(0x0C);
double speed = await ReadCurrentDataAsync(0x0D);
double temp = await ReadCurrentDataAsync(0x05);
```

Use batch reading:
```csharp
// Fast - batch request
var data = await ReadMultiplePIDsAsync(0x0C, 0x0D, 0x05);
```

### Caching

Cache static data (VIN, calibration ID):
```csharp
private string cachedVIN = null;

public async Task<string> GetVINAsync()
{
    if (cachedVIN != null)
        return cachedVIN;

    cachedVIN = await ReadVINAsync();
    return cachedVIN;
}
```

### Adaptive Polling

Adjust update rate based on vehicle state:
```csharp
private int GetUpdateInterval()
{
    if (rpm > 1000)
        return 250;  // Fast update when driving
    else
        return 1000; // Slow update at idle
}
```

---

## Testing

### Unit Tests

```csharp
[Test]
public void TestISO15765_SingleFrame()
{
    byte[] data = { 0x01, 0x0C };
    byte[] frame = ISO15765.BuildSingleFrame(data);

    Assert.AreEqual(8, frame.Length);
    Assert.AreEqual(0x02, frame[0]); // Length = 2
    Assert.AreEqual(0x01, frame[1]); // Mode
    Assert.AreEqual(0x0C, frame[2]); // PID
}

[Test]
public void TestDTCDecoding()
{
    var dtc = DiagnosticTroubleCode.DecodeDTC(0x01, 0x08);
    Assert.AreEqual("P0108", dtc.Code);
    Assert.AreEqual(DTCType.Powertrain, dtc.Type);
}
```

### Integration Tests

Test with ELM327 simulator or real vehicle:
```csharp
[Test]
public async Task TestConnection()
{
    var adapter = new ELM327("COM3");
    bool connected = await adapter.ConnectAsync();
    Assert.IsTrue(connected);

    double voltage = await adapter.ReadVoltageAsync();
    Assert.IsTrue(voltage > 11.0 && voltage < 15.0);
}
```

---

## Error Handling

### Connection Errors

```csharp
try
{
    await obdService.ConnectAsync();
}
catch (IOException ex)
{
    // Serial port errors
    MessageBox.Show("Cannot open COM port. Check connection.");
}
catch (TimeoutException ex)
{
    // No response from vehicle
    MessageBox.Show("Vehicle not responding. Check ignition.");
}
```

### Protocol Errors

```csharp
public async Task<double> SafeReadPID(byte pid)
{
    try
    {
        return await ReadCurrentDataAsync(pid);
    }
    catch (Exception)
    {
        // PID not supported
        return 0;
    }
}
```

---

## Debugging

### Enable Verbose Logging

```csharp
public class ELM327
{
    public bool VerboseLogging { get; set; } = false;

    private void Log(string message)
    {
        if (VerboseLogging)
            Debug.WriteLine($"[ELM327] {message}");
    }
}
```

### Packet Inspection

Log raw communication:
```csharp
private string SendCommand(string command)
{
    Log($"TX: {command}");
    string response = SendCommandInternal(command);
    Log($"RX: {response}");
    return response;
}
```

---

## Security Considerations

### Safe Commands Only

Restrict bidirectional control access:
```csharp
private readonly byte[] SAFE_TEST_IDS = { 0x01, 0x05, 0x0A };

public async Task<bool> RequestSystemControlAsync(byte testId)
{
    if (!SAFE_TEST_IDS.Contains(testId))
        throw new SecurityException("Test ID not allowed");

    // ... execute test
}
```

### User Confirmation

Always require confirmation for destructive operations:
```csharp
if (MessageBox.Show(
    "This will clear all codes. Continue?",
    "Confirm",
    MessageBoxButton.YesNo) == MessageBoxResult.Yes)
{
    await ClearDiagnosticCodesAsync();
}
```

---

## Future Enhancements

### Planned Features
1. Data logging to CSV/SQLite
2. Real-time graphing (live charts)
3. Custom dashboard layouts
4. Multiple vehicle profiles
5. Cloud sync for diagnostic history
6. Advanced GM-specific diagnostics
7. Bi-directional control expansion
8. Performance metrics (0-60, 1/4 mile)

### API Extensions

```csharp
// Future: Data export
public interface IDataExporter
{
    Task ExportToCSV(string filename);
    Task ExportToPDF(string filename);
}

// Future: Cloud sync
public interface ICloudSync
{
    Task UploadDiagnosticSession();
    Task<VehicleHistory> GetVehicleHistory(string vin);
}
```

---

## References

- ISO 15765-4:2016 - Road vehicles — Diagnostic communication over Controller Area Network (DoCAN)
- SAE J1979 - E/E Diagnostic Test Modes
- ELM327 Datasheet - Elm Electronics
- OBD-II PIDs - Wikipedia
- GM Service Manual - 2008 GMC Yukon Denali

---

**Version 1.0**
**Last Updated**: 2025-01-04
**Maintainer**: OBD Reader Development Team
