using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OBDReader.Core.Models;
using OBDReader.Core.Protocol;

namespace OBDReader.Core.Services
{
    /// <summary>
    /// OBD-II Service implementation
    /// Implements all diagnostic service modes (01-0A) with bidirectional communication
    /// </summary>
    public class OBDService : IDisposable
    {
        private ELM327 adapter;
        private bool isConnected = false;

        // Service Mode Constants
        public const byte MODE_CURRENT_DATA = 0x01;
        public const byte MODE_FREEZE_FRAME = 0x02;
        public const byte MODE_READ_DTC = 0x03;
        public const byte MODE_CLEAR_DTC = 0x04;
        public const byte MODE_O2_SENSOR_RESULTS = 0x05;
        public const byte MODE_MONITORING_RESULTS = 0x06;
        public const byte MODE_PENDING_DTC = 0x07;
        public const byte MODE_CONTROL_OPERATION = 0x08;
        public const byte MODE_VEHICLE_INFO = 0x09;
        public const byte MODE_PERMANENT_DTC = 0x0A;

        // Bidirectional Control (Mode 08) Test IDs
        public const byte TID_EVAP_LEAK_TEST = 0x01;
        public const byte TID_CATALYST_TEST = 0x05;
        public const byte TID_EXHAUST_GAS_SENSOR = 0x0A;

        public event EventHandler<string> StatusChanged;
        public event EventHandler<Exception> ErrorOccurred;

        public OBDService(ELM327 elm327Adapter)
        {
            adapter = elm327Adapter ?? throw new ArgumentNullException(nameof(elm327Adapter));
        }

        /// <summary>
        /// Connect to vehicle
        /// </summary>
        public async Task<bool> ConnectAsync()
        {
            try
            {
                StatusChanged?.Invoke(this, "Connecting to OBD adapter...");
                isConnected = await adapter.ConnectAsync();

                if (isConnected)
                {
                    StatusChanged?.Invoke(this, $"Connected: {adapter.DeviceDescription}");
                    StatusChanged?.Invoke(this, $"Protocol: {adapter.ProtocolVersion}");

                    // Verify vehicle communication
                    var voltage = await adapter.ReadVoltageAsync();
                    StatusChanged?.Invoke(this, $"Vehicle voltage: {voltage:F1}V");
                }

                return isConnected;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, ex);
                return false;
            }
        }

        /// <summary>
        /// Disconnect from vehicle
        /// </summary>
        public void Disconnect()
        {
            adapter?.Disconnect();
            isConnected = false;
            StatusChanged?.Invoke(this, "Disconnected");
        }

        #region Mode 01 - Current Data

        /// <summary>
        /// Read current data PID
        /// </summary>
        public async Task<double> ReadCurrentDataAsync(byte pid)
        {
            ValidateConnection();

            var response = await adapter.SendOBDRequestAsync(MODE_CURRENT_DATA, pid);

            if (response.Length < 2)
                return 0;

            // Verify response mode (should be 0x41 for mode 01 response)
            if (response[0] != 0x41)
                return 0;

            // Extract data bytes (skip mode and PID)
            byte[] data = response.Skip(2).ToArray();

            return OBDPIDs.CalculateValue(MODE_CURRENT_DATA, pid, data);
        }

        /// <summary>
        /// Read multiple PIDs at once
        /// </summary>
        public async Task<Dictionary<byte, double>> ReadMultiplePIDsAsync(params byte[] pids)
        {
            var results = new Dictionary<byte, double>();

            foreach (var pid in pids)
            {
                try
                {
                    double value = await ReadCurrentDataAsync(pid);
                    results[pid] = value;
                }
                catch (Exception ex)
                {
                    ErrorOccurred?.Invoke(this, new Exception($"Error reading PID 0x{pid:X2}: {ex.Message}"));
                }
            }

            return results;
        }

        /// <summary>
        /// Get supported PIDs
        /// </summary>
        public async Task<List<byte>> GetSupportedPIDsAsync()
        {
            var supportedPIDs = new List<byte>();

            // Check PID ranges: 0x00 (01-20), 0x20 (21-40), 0x40 (41-60), etc.
            byte[] checkPIDs = { 0x00, 0x20, 0x40, 0x60, 0x80, 0xA0, 0xC0, 0xE0 };

            foreach (byte checkPID in checkPIDs)
            {
                try
                {
                    var response = await adapter.SendOBDRequestAsync(MODE_CURRENT_DATA, checkPID);

                    if (response.Length >= 6 && response[0] == 0x41 && response[1] == checkPID)
                    {
                        // Parse 4 bytes of bitmap
                        for (int byteIndex = 0; byteIndex < 4; byteIndex++)
                        {
                            byte bitmap = response[2 + byteIndex];

                            for (int bit = 0; bit < 8; bit++)
                            {
                                if ((bitmap & (0x80 >> bit)) != 0)
                                {
                                    byte pidNumber = (byte)(checkPID + byteIndex * 8 + bit + 1);
                                    supportedPIDs.Add(pidNumber);
                                }
                            }
                        }
                    }
                }
                catch
                {
                    break; // Stop checking if we get an error
                }
            }

            return supportedPIDs;
        }

        #endregion

        #region Mode 02 - Freeze Frame Data

        /// <summary>
        /// Read freeze frame data
        /// </summary>
        public async Task<Dictionary<byte, double>> ReadFreezeFrameAsync(byte frameNumber = 0)
        {
            ValidateConnection();

            var freezeData = new Dictionary<byte, double>();

            // Get available freeze frame PIDs
            var supportedPIDs = await GetSupportedPIDsAsync();

            foreach (var pid in supportedPIDs)
            {
                try
                {
                    var response = await adapter.SendOBDRequestAsync(MODE_FREEZE_FRAME, pid, frameNumber);

                    if (response.Length >= 3 && response[0] == 0x42)
                    {
                        byte[] data = response.Skip(3).ToArray();
                        double value = OBDPIDs.CalculateValue(MODE_CURRENT_DATA, pid, data);
                        freezeData[pid] = value;
                    }
                }
                catch (Exception ex)
                {
                    ErrorOccurred?.Invoke(this, new Exception($"Error reading freeze frame PID 0x{pid:X2}: {ex.Message}"));
                }
            }

            return freezeData;
        }

        #endregion

        #region Mode 03 - Read Diagnostic Trouble Codes

        /// <summary>
        /// Read confirmed diagnostic trouble codes
        /// </summary>
        public async Task<List<DiagnosticTroubleCode>> ReadDiagnosticCodesAsync()
        {
            ValidateConnection();

            var response = await adapter.SendOBDRequestAsync(MODE_READ_DTC, 0x00);

            if (response.Length < 2 || response[0] != 0x43)
                return new List<DiagnosticTroubleCode>();

            // First byte after mode is number of codes
            int codeCount = response[1];

            // Extract DTC bytes (skip mode and count)
            byte[] dtcData = response.Skip(2).ToArray();

            return DiagnosticTroubleCode.ParseDTCs(dtcData, DiagnosticTroubleCode.DTCStatus.Confirmed);
        }

        #endregion

        #region Mode 04 - Clear Diagnostic Trouble Codes

        /// <summary>
        /// Clear all diagnostic trouble codes and reset monitoring systems
        /// </summary>
        public async Task<bool> ClearDiagnosticCodesAsync()
        {
            ValidateConnection();

            try
            {
                var response = await adapter.SendOBDRequestAsync(MODE_CLEAR_DTC, 0x00);

                // Successful clear returns 0x44
                if (response.Length > 0 && response[0] == 0x44)
                {
                    StatusChanged?.Invoke(this, "Diagnostic codes cleared successfully");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, new Exception($"Failed to clear codes: {ex.Message}"));
                return false;
            }
        }

        #endregion

        #region Mode 05 - O2 Sensor Test Results

        /// <summary>
        /// Read O2 sensor test results
        /// </summary>
        public async Task<Dictionary<string, double>> ReadO2SensorTestResultsAsync(byte sensorId)
        {
            ValidateConnection();

            var results = new Dictionary<string, double>();

            try
            {
                var response = await adapter.SendOBDRequestAsync(MODE_O2_SENSOR_RESULTS, sensorId);

                if (response.Length >= 2 && response[0] == 0x45)
                {
                    results["Sensor_ID"] = sensorId;

                    // Parse O2 sensor test results
                    // Response format: 45 [TID] [data bytes...]
                    if (response.Length >= 4)
                    {
                        // Voltage (byte 2-3): A*256 + B, scaled to 0-1.275V
                        double voltage = ((response[2] * 256.0 + response[3]) / 1000.0);
                        results["Voltage"] = voltage;
                    }

                    if (response.Length >= 6)
                    {
                        // Current (byte 4-5): A*256 + B, scaled to mA
                        double current = ((response[4] * 256.0 + response[5] - 32768) / 256.0);
                        results["Current"] = current;
                    }

                    if (response.Length >= 8)
                    {
                        // Min/Max values if available
                        results["Min_Voltage"] = response[6] / 200.0;
                        results["Max_Voltage"] = response[7] / 200.0;
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, new Exception($"Error reading O2 sensor {sensorId}: {ex.Message}"));
            }

            return results;
        }

        #endregion

        #region Mode 06 - On-board Monitoring Test Results

        /// <summary>
        /// Read on-board monitoring test results
        /// </summary>
        public async Task<Dictionary<string, object>> ReadMonitoringTestResultsAsync()
        {
            ValidateConnection();

            var results = new Dictionary<string, object>();

            try
            {
                var response = await adapter.SendOBDRequestAsync(MODE_MONITORING_RESULTS, 0x00);

                if (response.Length >= 2 && response[0] == 0x46)
                {
                    // Parse monitoring test results
                    // Response format: 46 [TID] [test data...]
                    // Each test result is typically 4-6 bytes: TID, component ID, min, max, test value

                    int offset = 1; // Skip mode byte
                    int testNumber = 0;

                    while (offset + 4 <= response.Length)
                    {
                        byte testId = response[offset];
                        byte componentId = response[offset + 1];

                        // Test value (bytes 2-3)
                        int testValue = (response[offset + 2] << 8) | response[offset + 3];

                        // Min value (bytes 4-5) if available
                        int minValue = 0;
                        int maxValue = 0;

                        if (offset + 6 <= response.Length)
                        {
                            minValue = (response[offset + 4] << 8) | response[offset + 5];
                        }

                        // Max value (bytes 6-7) if available
                        if (offset + 8 <= response.Length)
                        {
                            maxValue = (response[offset + 6] << 8) | response[offset + 7];
                            offset += 8;
                        }
                        else
                        {
                            offset += 6;
                        }

                        // Store result
                        string testKey = $"Test_{testNumber:D2}_TID_{testId:X2}";
                        results[testKey] = new
                        {
                            TestId = testId,
                            ComponentId = componentId,
                            Value = testValue,
                            Min = minValue,
                            Max = maxValue,
                            Status = (testValue >= minValue && testValue <= maxValue) ? "PASS" : "FAIL"
                        };

                        testNumber++;
                    }

                    StatusChanged?.Invoke(this, $"Retrieved {testNumber} monitoring test results");
                }
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, new Exception($"Error reading monitoring tests: {ex.Message}"));
            }

            return results;
        }

        #endregion

        #region Mode 07 - Read Pending Diagnostic Trouble Codes

        /// <summary>
        /// Read pending diagnostic trouble codes
        /// </summary>
        public async Task<List<DiagnosticTroubleCode>> ReadPendingCodesAsync()
        {
            ValidateConnection();

            var response = await adapter.SendOBDRequestAsync(MODE_PENDING_DTC, 0x00);

            if (response.Length < 2 || response[0] != 0x47)
                return new List<DiagnosticTroubleCode>();

            int codeCount = response[1];
            byte[] dtcData = response.Skip(2).ToArray();

            return DiagnosticTroubleCode.ParseDTCs(dtcData, DiagnosticTroubleCode.DTCStatus.Pending);
        }

        #endregion

        #region Mode 08 - Control of On-board Systems (Bidirectional)

        /// <summary>
        /// Request control of on-board system/component (bidirectional)
        /// </summary>
        public async Task<bool> RequestSystemControlAsync(byte testId, params byte[] parameters)
        {
            ValidateConnection();

            try
            {
                var response = await adapter.SendOBDRequestAsync(MODE_CONTROL_OPERATION, testId, parameters);

                if (response.Length >= 2 && response[0] == 0x48)
                {
                    StatusChanged?.Invoke(this, $"Control operation 0x{testId:X2} executed");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, new Exception($"Control operation failed: {ex.Message}"));
                return false;
            }
        }

        /// <summary>
        /// Test EVAP system
        /// </summary>
        public async Task<bool> TestEVAPSystemAsync()
        {
            return await RequestSystemControlAsync(TID_EVAP_LEAK_TEST);
        }

        /// <summary>
        /// Test catalytic converter
        /// </summary>
        public async Task<bool> TestCatalyticConverterAsync()
        {
            return await RequestSystemControlAsync(TID_CATALYST_TEST);
        }

        #endregion

        #region Mode 09 - Vehicle Information

        /// <summary>
        /// Request vehicle VIN
        /// </summary>
        public async Task<string> ReadVINAsync()
        {
            ValidateConnection();

            try
            {
                var response = await adapter.SendOBDRequestAsync(MODE_VEHICLE_INFO, 0x02);

                if (response.Length >= 2 && response[0] == 0x49)
                {
                    // VIN is in ASCII, skip mode and PID bytes
                    byte[] vinBytes = response.Skip(2).ToArray();
                    return System.Text.Encoding.ASCII.GetString(vinBytes).Trim('\0', ' ');
                }
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, new Exception($"Error reading VIN: {ex.Message}"));
            }

            return string.Empty;
        }

        /// <summary>
        /// Read calibration ID
        /// </summary>
        public async Task<string> ReadCalibrationIDAsync()
        {
            ValidateConnection();

            try
            {
                var response = await adapter.SendOBDRequestAsync(MODE_VEHICLE_INFO, 0x04);

                if (response.Length >= 2 && response[0] == 0x49)
                {
                    byte[] calIdBytes = response.Skip(2).ToArray();
                    return System.Text.Encoding.ASCII.GetString(calIdBytes).Trim('\0', ' ');
                }
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, new Exception($"Error reading Calibration ID: {ex.Message}"));
            }

            return string.Empty;
        }

        #endregion

        #region Mode 0A - Permanent Diagnostic Trouble Codes

        /// <summary>
        /// Read permanent diagnostic trouble codes (cannot be cleared)
        /// </summary>
        public async Task<List<DiagnosticTroubleCode>> ReadPermanentCodesAsync()
        {
            ValidateConnection();

            var response = await adapter.SendOBDRequestAsync(MODE_PERMANENT_DTC, 0x00);

            if (response.Length < 2 || response[0] != 0x4A)
                return new List<DiagnosticTroubleCode>();

            byte[] dtcData = response.Skip(1).ToArray();

            return DiagnosticTroubleCode.ParseDTCs(dtcData, DiagnosticTroubleCode.DTCStatus.Permanent);
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Validate connection before operations
        /// </summary>
        private void ValidateConnection()
        {
            if (!isConnected)
                throw new InvalidOperationException("Not connected to vehicle");
        }

        /// <summary>
        /// Get comprehensive vehicle status
        /// </summary>
        public async Task<VehicleStatus> GetVehicleStatusAsync()
        {
            ValidateConnection();

            var status = new VehicleStatus
            {
                VIN = await ReadVINAsync(),
                ConfirmedCodes = await ReadDiagnosticCodesAsync(),
                PendingCodes = await ReadPendingCodesAsync(),
                PermanentCodes = await ReadPermanentCodesAsync()
            };

            // Read key PIDs
            var pidData = await ReadMultiplePIDsAsync(
                0x04, // Engine Load
                0x05, // Coolant Temperature
                0x0C, // Engine RPM
                0x0D, // Vehicle Speed
                0x11, // Throttle Position
                0x2F  // Fuel Level
            );

            status.CurrentData = pidData;

            return status;
        }

        public void Dispose()
        {
            Disconnect();
            adapter?.Dispose();
        }

        #endregion
    }

    /// <summary>
    /// Comprehensive vehicle status
    /// </summary>
    public class VehicleStatus
    {
        public string VIN { get; set; }
        public List<DiagnosticTroubleCode> ConfirmedCodes { get; set; }
        public List<DiagnosticTroubleCode> PendingCodes { get; set; }
        public List<DiagnosticTroubleCode> PermanentCodes { get; set; }
        public Dictionary<byte, double> CurrentData { get; set; }
        public Dictionary<byte, double> FreezeFrameData { get; set; }

        public bool HasErrors => (ConfirmedCodes?.Count ?? 0) > 0;
        public bool HasPendingErrors => (PendingCodes?.Count ?? 0) > 0;
        public int TotalErrorCount => (ConfirmedCodes?.Count ?? 0) + (PendingCodes?.Count ?? 0) + (PermanentCodes?.Count ?? 0);
    }
}
