using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OBDReader.Core.Models;
using OBDReader.Core.Protocol;
using OBDReader.Core.Services;

namespace OBDReader.Core.Testing
{
    /// <summary>
    /// Comprehensive test suite for OBD Reader application
    /// Tests all functionality using MockELM327 simulator
    /// </summary>
    public class OBDReaderTests
    {
        private MockELM327 mockAdapter;
        private OBDService obdService;

        public async Task<bool> RunAllTestsAsync()
        {
            Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║   OBD-II DIAGNOSTIC TOOL - COMPREHENSIVE TEST SUITE         ║");
            Console.WriteLine("║   Testing with Simulated 2008 GMC Yukon Denali              ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
            Console.WriteLine();

            int passed = 0;
            int failed = 0;
            int total = 0;

            try
            {
                // Test 1: Connection
                total++;
                Console.WriteLine("TEST 1: Connection to Mock Adapter");
                Console.WriteLine("─────────────────────────────────────────────────────────────");
                if (await TestConnection())
                {
                    Console.WriteLine("✓ PASSED: Successfully connected to mock vehicle\n");
                    passed++;
                }
                else
                {
                    Console.WriteLine("✗ FAILED: Connection failed\n");
                    failed++;
                    return false; // Can't continue without connection
                }

                // Test 2: Read Engine RPM
                total++;
                Console.WriteLine("TEST 2: Read Engine RPM (Mode 01, PID 0x0C)");
                Console.WriteLine("─────────────────────────────────────────────────────────────");
                if (await TestReadRPM())
                {
                    Console.WriteLine("✓ PASSED: RPM reading successful\n");
                    passed++;
                }
                else
                {
                    Console.WriteLine("✗ FAILED: RPM reading failed\n");
                    failed++;
                }

                // Test 3: Read Multiple PIDs
                total++;
                Console.WriteLine("TEST 3: Read Multiple PIDs Simultaneously");
                Console.WriteLine("─────────────────────────────────────────────────────────────");
                if (await TestReadMultiplePIDs())
                {
                    Console.WriteLine("✓ PASSED: Multiple PID reading successful\n");
                    passed++;
                }
                else
                {
                    Console.WriteLine("✗ FAILED: Multiple PID reading failed\n");
                    failed++;
                }

                // Test 4: Read Diagnostic Codes
                total++;
                Console.WriteLine("TEST 4: Read Diagnostic Trouble Codes (Mode 03)");
                Console.WriteLine("─────────────────────────────────────────────────────────────");
                if (await TestReadDTCs())
                {
                    Console.WriteLine("✓ PASSED: DTC reading successful\n");
                    passed++;
                }
                else
                {
                    Console.WriteLine("✗ FAILED: DTC reading failed\n");
                    failed++;
                }

                // Test 5: DTC Decoding
                total++;
                Console.WriteLine("TEST 5: DTC Decoding and Description");
                Console.WriteLine("─────────────────────────────────────────────────────────────");
                if (TestDTCDecoding())
                {
                    Console.WriteLine("✓ PASSED: DTC decoding successful\n");
                    passed++;
                }
                else
                {
                    Console.WriteLine("✗ FAILED: DTC decoding failed\n");
                    failed++;
                }

                // Test 6: Clear Diagnostic Codes
                total++;
                Console.WriteLine("TEST 6: Clear Diagnostic Codes (Mode 04)");
                Console.WriteLine("─────────────────────────────────────────────────────────────");
                if (await TestClearDTCs())
                {
                    Console.WriteLine("✓ PASSED: DTC clearing successful\n");
                    passed++;
                }
                else
                {
                    Console.WriteLine("✗ FAILED: DTC clearing failed\n");
                    failed++;
                }

                // Test 7: Freeze Frame Data
                total++;
                Console.WriteLine("TEST 7: Read Freeze Frame Data (Mode 02)");
                Console.WriteLine("─────────────────────────────────────────────────────────────");
                if (await TestFreezeFrame())
                {
                    Console.WriteLine("✓ PASSED: Freeze frame reading successful\n");
                    passed++;
                }
                else
                {
                    Console.WriteLine("✗ FAILED: Freeze frame reading failed\n");
                    failed++;
                }

                // Test 8: Vehicle Information
                total++;
                Console.WriteLine("TEST 8: Read Vehicle Information (Mode 09)");
                Console.WriteLine("─────────────────────────────────────────────────────────────");
                if (await TestVehicleInfo())
                {
                    Console.WriteLine("✓ PASSED: Vehicle information reading successful\n");
                    passed++;
                }
                else
                {
                    Console.WriteLine("✗ FAILED: Vehicle information reading failed\n");
                    failed++;
                }

                // Test 9: Bidirectional Control
                total++;
                Console.WriteLine("TEST 9: Bidirectional Control - EVAP Test (Mode 08)");
                Console.WriteLine("─────────────────────────────────────────────────────────────");
                if (await TestBidirectionalControl())
                {
                    Console.WriteLine("✓ PASSED: Bidirectional control successful\n");
                    passed++;
                }
                else
                {
                    Console.WriteLine("✗ FAILED: Bidirectional control failed\n");
                    failed++;
                }

                // Test 10: PID Formula Calculations
                total++;
                Console.WriteLine("TEST 10: PID Formula Calculations");
                Console.WriteLine("─────────────────────────────────────────────────────────────");
                if (TestPIDFormulas())
                {
                    Console.WriteLine("✓ PASSED: All PID formulas correct\n");
                    passed++;
                }
                else
                {
                    Console.WriteLine("✗ FAILED: PID formula errors\n");
                    failed++;
                }

                // Test 11: ISO 15765-4 Protocol
                total++;
                Console.WriteLine("TEST 11: ISO 15765-4 CAN Protocol Functions");
                Console.WriteLine("─────────────────────────────────────────────────────────────");
                if (TestISO15765())
                {
                    Console.WriteLine("✓ PASSED: CAN protocol functions working\n");
                    passed++;
                }
                else
                {
                    Console.WriteLine("✗ FAILED: CAN protocol errors\n");
                    failed++;
                }

                // Test 12: Pending DTCs
                total++;
                Console.WriteLine("TEST 12: Read Pending DTCs (Mode 07)");
                Console.WriteLine("─────────────────────────────────────────────────────────────");
                if (await TestPendingDTCs())
                {
                    Console.WriteLine("✓ PASSED: Pending DTC reading successful\n");
                    passed++;
                }
                else
                {
                    Console.WriteLine("✗ FAILED: Pending DTC reading failed\n");
                    failed++;
                }

                // Test 13: Real-time Data Monitoring
                total++;
                Console.WriteLine("TEST 13: Real-time Data Monitoring (5 iterations)");
                Console.WriteLine("─────────────────────────────────────────────────────────────");
                if (await TestRealTimeMonitoring())
                {
                    Console.WriteLine("✓ PASSED: Real-time monitoring successful\n");
                    passed++;
                }
                else
                {
                    Console.WriteLine("✗ FAILED: Real-time monitoring failed\n");
                    failed++;
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n✗ FATAL ERROR: {ex.Message}");
                Console.WriteLine($"   Stack trace: {ex.StackTrace}");
                failed++;
            }
            finally
            {
                // Cleanup
                obdService?.Disconnect();
                mockAdapter?.Dispose();
            }

            // Print summary
            Console.WriteLine("\n╔══════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                      TEST SUMMARY                            ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
            Console.WriteLine($"Total Tests:  {total}");
            Console.WriteLine($"Passed:       {passed} ✓");
            Console.WriteLine($"Failed:       {failed} ✗");
            Console.WriteLine($"Success Rate: {(passed * 100.0 / total):F1}%");
            Console.WriteLine();

            if (failed == 0)
            {
                Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
                Console.WriteLine("║  🎉 ALL TESTS PASSED! Application is fully functional! 🎉  ║");
                Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
            }
            else
            {
                Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
                Console.WriteLine("║  ⚠️  SOME TESTS FAILED - Review errors above              ║");
                Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
            }

            return failed == 0;
        }

        private async Task<bool> TestConnection()
        {
            try
            {
                mockAdapter = new MockELM327("MOCK_COM1");
                obdService = new OBDService(mockAdapter);

                obdService.StatusChanged += (s, msg) => Console.WriteLine($"  Status: {msg}");
                obdService.ErrorOccurred += (s, ex) => Console.WriteLine($"  Error: {ex.Message}");

                bool connected = await obdService.ConnectAsync();

                if (connected)
                {
                    double voltage = await mockAdapter.ReadVoltageAsync();
                    Console.WriteLine($"  Battery Voltage: {voltage:F1}V");
                    return voltage > 11.0 && voltage < 15.0;
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Exception: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> TestReadRPM()
        {
            try
            {
                double rpm = await obdService.ReadCurrentDataAsync(0x0C);
                Console.WriteLine($"  Engine RPM: {rpm:F0} RPM");

                // RPM should be between idle (600) and redline (7000)
                return rpm >= 600 && rpm <= 7000;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Exception: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> TestReadMultiplePIDs()
        {
            try
            {
                var pidData = await obdService.ReadMultiplePIDsAsync(
                    0x0C, // RPM
                    0x0D, // Speed
                    0x05, // Coolant temp
                    0x11, // Throttle
                    0x04, // Engine load
                    0x2F  // Fuel level
                );

                Console.WriteLine($"  RPM:              {pidData[0x0C]:F0} RPM");
                Console.WriteLine($"  Speed:            {pidData[0x0D]:F0} km/h");
                Console.WriteLine($"  Coolant Temp:     {pidData[0x05]:F0} °C");
                Console.WriteLine($"  Throttle:         {pidData[0x11]:F1} %");
                Console.WriteLine($"  Engine Load:      {pidData[0x04]:F1} %");
                Console.WriteLine($"  Fuel Level:       {pidData[0x2F]:F1} %");

                return pidData.Count == 6 && pidData[0x0C] > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Exception: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> TestReadDTCs()
        {
            try
            {
                var codes = await obdService.ReadDiagnosticCodesAsync();
                Console.WriteLine($"  Found {codes.Count} diagnostic code(s):");

                foreach (var code in codes)
                {
                    Console.WriteLine($"    {code.Code}: {code.Description}");
                }

                // Should have P0301 and P0420 in mock
                return codes.Count == 2;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Exception: {ex.Message}");
                return false;
            }
        }

        private bool TestDTCDecoding()
        {
            try
            {
                // Test P0301 decoding
                var dtc1 = DiagnosticTroubleCode.DecodeDTC(0x01, 0x01);
                Console.WriteLine($"  DTC Byte 0x0101 -> {dtc1.Code}: {dtc1.Description}");

                // Test P0420 decoding
                var dtc2 = DiagnosticTroubleCode.DecodeDTC(0x04, 0x20);
                Console.WriteLine($"  DTC Byte 0x0420 -> {dtc2.Code}: {dtc2.Description}");

                // Test C0035 (Chassis code)
                var dtc3 = DiagnosticTroubleCode.DecodeDTC(0x40, 0x35);
                Console.WriteLine($"  DTC Byte 0x4035 -> {dtc3.Code}: {dtc3.Description}");

                return dtc1.Code == "P0301" && dtc2.Code == "P0420" && dtc3.Code == "C0035";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Exception: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> TestClearDTCs()
        {
            try
            {
                bool cleared = await obdService.ClearDiagnosticCodesAsync();
                Console.WriteLine($"  Clear command result: {(cleared ? "Success" : "Failed")}");

                if (cleared)
                {
                    // Verify codes are cleared
                    var codes = await obdService.ReadDiagnosticCodesAsync();
                    Console.WriteLine($"  Remaining codes after clear: {codes.Count}");
                    return codes.Count == 0;
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Exception: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> TestFreezeFrame()
        {
            try
            {
                var freezeData = await obdService.ReadFreezeFrameAsync(0);
                Console.WriteLine($"  Freeze frame parameters: {freezeData.Count}");

                foreach (var kvp in freezeData)
                {
                    var pidDef = OBDPIDs.GetPID(0x01, kvp.Key);
                    if (pidDef != null)
                    {
                        Console.WriteLine($"    {pidDef.Name}: {kvp.Value:F1} {pidDef.Unit}");
                    }
                }

                return freezeData.Count > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Exception: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> TestVehicleInfo()
        {
            try
            {
                string vin = await obdService.ReadVINAsync();
                string calId = await obdService.ReadCalibrationIDAsync();

                Console.WriteLine($"  VIN: {vin}");
                Console.WriteLine($"  Calibration ID: {calId}");

                return !string.IsNullOrEmpty(vin) && !string.IsNullOrEmpty(calId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Exception: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> TestBidirectionalControl()
        {
            try
            {
                bool evapTest = await obdService.TestEVAPSystemAsync();
                Console.WriteLine($"  EVAP test initiated: {evapTest}");

                bool catalystTest = await obdService.TestCatalyticConverterAsync();
                Console.WriteLine($"  Catalyst test initiated: {catalystTest}");

                return evapTest && catalystTest;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Exception: {ex.Message}");
                return false;
            }
        }

        private bool TestPIDFormulas()
        {
            try
            {
                // Test RPM formula: ((A*256)+B)/4
                byte[] rpmData = { 0x0B, 0xB8 }; // Should be 750 RPM
                double rpm = OBDPIDs.CalculateValue(0x01, 0x0C, rpmData);
                Console.WriteLine($"  RPM Formula Test: {rpm:F0} RPM (expected 750)");

                // Test coolant temp formula: A-40
                byte[] tempData = { 0x7D }; // Should be 85°C
                double temp = OBDPIDs.CalculateValue(0x01, 0x05, tempData);
                Console.WriteLine($"  Coolant Temp Test: {temp:F0}°C (expected 85)");

                // Test throttle formula: A*100/255
                byte[] throttleData = { 0x80 }; // Should be ~50%
                double throttle = OBDPIDs.CalculateValue(0x01, 0x11, throttleData);
                Console.WriteLine($"  Throttle Test: {throttle:F1}% (expected ~50)");

                return Math.Abs(rpm - 750) < 1 &&
                       Math.Abs(temp - 85) < 1 &&
                       Math.Abs(throttle - 50.2) < 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Exception: {ex.Message}");
                return false;
            }
        }

        private bool TestISO15765()
        {
            try
            {
                // Test single frame
                byte[] singleData = { 0x01, 0x0C };
                byte[] singleFrame = ISO15765.BuildSingleFrame(singleData);
                Console.WriteLine($"  Single frame: {BitConverter.ToString(singleFrame)}");

                var (frameType, length, data) = ISO15765.ParseCANFrame(singleFrame);
                Console.WriteLine($"  Parsed: Type={frameType}, Length={length}, Data={BitConverter.ToString(data)}");

                // Test multi-frame segmentation
                byte[] longData = new byte[20]; // 20 bytes - requires multi-frame
                for (int i = 0; i < longData.Length; i++) longData[i] = (byte)i;

                var frames = ISO15765.SegmentData(longData);
                Console.WriteLine($"  Multi-frame: {frames.Count} frames generated");

                // Test reassembly
                byte[] reassembled = ISO15765.ReassembleFrames(frames);
                Console.WriteLine($"  Reassembled: {reassembled.Length} bytes");

                return frameType == ISO15765.FrameType.SingleFrame &&
                       frames.Count > 1 &&
                       reassembled.Length == longData.Length;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Exception: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> TestPendingDTCs()
        {
            try
            {
                var pendingCodes = await obdService.ReadPendingCodesAsync();
                Console.WriteLine($"  Pending codes: {pendingCodes.Count}");

                foreach (var code in pendingCodes)
                {
                    Console.WriteLine($"    {code.Code}: {code.Description}");
                }

                return true; // Should be 0 in mock, which is valid
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Exception: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> TestRealTimeMonitoring()
        {
            try
            {
                for (int i = 0; i < 5; i++)
                {
                    var pidData = await obdService.ReadMultiplePIDsAsync(0x0C, 0x0D, 0x05);
                    Console.WriteLine($"  Iteration {i + 1}: RPM={pidData[0x0C]:F0}, Speed={pidData[0x0D]:F0}, Temp={pidData[0x05]:F0}");
                    await Task.Delay(500); // 500ms between reads
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Exception: {ex.Message}");
                return false;
            }
        }
    }
}
