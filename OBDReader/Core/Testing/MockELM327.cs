using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OBDReader.Core.Protocol;

namespace OBDReader.Core.Testing
{
    /// <summary>
    /// Mock ELM327 adapter for testing without physical hardware
    /// Simulates realistic OBD-II responses for a 2008 GMC Yukon Denali
    /// </summary>
    public class MockELM327 : IDisposable
    {
        private bool isConnected = false;
        private Random random = new Random();
        private DateTime engineStartTime = DateTime.Now;

        // Simulated vehicle state
        private double simulatedRPM = 0;
        private double simulatedSpeed = 0;
        private double simulatedCoolantTemp = 20;
        private double simulatedThrottle = 0;
        private double simulatedEngineLoad = 0;
        private double simulatedFuelLevel = 75;
        private double simulatedIntakeTemp = 25;
        private double simulatedVoltage = 12.6;

        // Simulated DTCs
        private List<(byte, byte)> storedDTCs = new List<(byte, byte)>
        {
            // P0301 - Cylinder 1 Misfire
            (0x01, 0x01),
            // P0420 - Catalyst System Efficiency Below Threshold (Bank 1)
            (0x04, 0x20)
        };

        public string PortName { get; private set; }
        public int BaudRate { get; private set; }
        public string ProtocolVersion { get; private set; }
        public string DeviceDescription { get; private set; }

        public event EventHandler<string> DataReceived;
        public event EventHandler<string> ErrorOccurred;

        public MockELM327(string portName, int baudRate = 38400)
        {
            PortName = portName;
            BaudRate = baudRate;
            ProtocolVersion = "A6"; // ISO 15765-4 (CAN)
            DeviceDescription = "MOCK ELM327 v1.5";
        }

        /// <summary>
        /// Simulate connection to vehicle
        /// </summary>
        public async Task<bool> ConnectAsync()
        {
            await Task.Delay(100); // Simulate connection delay

            Console.WriteLine("[MOCK] Connecting to simulated 2008 GMC Yukon Denali...");

            // Simulate engine warming up
            simulatedCoolantTemp = 85; // Normal operating temp
            simulatedRPM = 750; // Idle RPM
            simulatedVoltage = 14.2; // Charging system active

            isConnected = true;
            Console.WriteLine("[MOCK] Connected successfully!");
            return true;
        }

        /// <summary>
        /// Simulate disconnection
        /// </summary>
        public void Disconnect()
        {
            isConnected = false;
            Console.WriteLine("[MOCK] Disconnected from vehicle");
        }

        /// <summary>
        /// Process AT commands
        /// </summary>
        public async Task<string> SendCommandAsync(string command)
        {
            if (!isConnected)
                throw new InvalidOperationException("Not connected to mock adapter");

            await Task.Delay(10); // Simulate communication delay

            command = command.Trim().ToUpper();
            Console.WriteLine($"[MOCK] RX: {command}");

            string response = command switch
            {
                "ATZ" => "ELM327 v1.5\r\n>",
                "ATE0" => "OK\r\n>",
                "ATL0" => "OK\r\n>",
                "ATS0" => "OK\r\n>",
                "ATH1" => "OK\r\n>",
                "ATSP0" => "OK\r\n>",
                "ATSP6" => "OK\r\n>",
                "AT@1" => "MOCK ELM327 v1.5\r\n>",
                "ATDPN" => "A6\r\n>", // ISO 15765-4 CAN
                "ATRV" => $"{simulatedVoltage:F1}V\r\n>",
                _ => ProcessOBDCommand(command)
            };

            Console.WriteLine($"[MOCK] TX: {response.Replace("\r\n", " ").Replace(">", "").Trim()}");
            DataReceived?.Invoke(this, response);
            return response;
        }

        /// <summary>
        /// Process OBD-II commands
        /// </summary>
        private string ProcessOBDCommand(string command)
        {
            // Remove spaces and parse hex bytes
            string[] parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
                return "?\r\n>";

            if (!byte.TryParse(parts[0], System.Globalization.NumberStyles.HexNumber, null, out byte mode))
                return "?\r\n>";

            if (!byte.TryParse(parts[1], System.Globalization.NumberStyles.HexNumber, null, out byte pid))
                return "?\r\n>";

            // Update simulated vehicle state (simulate driving)
            UpdateVehicleState();

            return mode switch
            {
                0x01 => ProcessMode01(pid),
                0x02 => ProcessMode02(pid),
                0x03 => ProcessMode03(),
                0x04 => ProcessMode04(),
                0x05 => ProcessMode05(pid),
                0x06 => ProcessMode06(pid),
                0x07 => ProcessMode07(),
                0x08 => ProcessMode08(pid),
                0x09 => ProcessMode09(pid),
                0x0A => ProcessMode0A(),
                _ => "?\r\n>"
            };
        }

        /// <summary>
        /// Simulate vehicle state changes over time
        /// </summary>
        private void UpdateVehicleState()
        {
            // Simulate engine running
            double time = (DateTime.Now - engineStartTime).TotalSeconds;

            // Simulate RPM fluctuation (idle: 750, cruising: 2000-3000)
            simulatedRPM = 750 + Math.Sin(time * 0.5) * 100 + random.Next(-20, 20);

            // Simulate speed (0-60 km/h)
            simulatedSpeed = Math.Max(0, 30 + Math.Sin(time * 0.3) * 30);

            // Simulate throttle position
            simulatedThrottle = Math.Max(0, Math.Min(100, 10 + Math.Sin(time * 0.4) * 15));

            // Simulate engine load
            simulatedEngineLoad = Math.Max(0, Math.Min(100, 25 + Math.Sin(time * 0.35) * 20));

            // Coolant temp stays stable
            simulatedCoolantTemp = 85 + random.Next(-2, 3);

            // Intake air temp
            simulatedIntakeTemp = 30 + random.Next(-3, 3);

            // Fuel level slowly decreases
            simulatedFuelLevel = Math.Max(10, 75 - (time / 360.0)); // 1% per 6 minutes

            // Voltage fluctuates slightly
            simulatedVoltage = 14.2 + random.Next(-2, 2) * 0.1;
        }

        /// <summary>
        /// Mode 01 - Current Data
        /// </summary>
        private string ProcessMode01(byte pid)
        {
            string response = pid switch
            {
                0x00 => "41 00 BE 3F A8 13\r\n>", // Supported PIDs 01-20
                0x01 => "41 01 00 07 E5 00\r\n>", // Monitor status
                0x03 => "41 03 01 00\r\n>", // Fuel system status (closed loop)
                0x04 => FormatResponse(0x41, 0x04, (byte)(simulatedEngineLoad * 255 / 100)),
                0x05 => FormatResponse(0x41, 0x05, (byte)(simulatedCoolantTemp + 40)),
                0x06 => FormatResponse(0x41, 0x06, (byte)((0 + 128) * 128 / 100)), // STFT Bank 1
                0x07 => FormatResponse(0x41, 0x07, (byte)((0 + 128) * 128 / 100)), // LTFT Bank 1
                0x0B => FormatResponse(0x41, 0x0B, (byte)(30)), // MAP
                0x0C => FormatResponse(0x41, 0x0C,
                    (byte)((int)(simulatedRPM * 4) >> 8),
                    (byte)((int)(simulatedRPM * 4) & 0xFF)),
                0x0D => FormatResponse(0x41, 0x0D, (byte)simulatedSpeed),
                0x0E => FormatResponse(0x41, 0x0E, (byte)((10 + 64) * 2)), // Timing advance
                0x0F => FormatResponse(0x41, 0x0F, (byte)(simulatedIntakeTemp + 40)),
                0x10 => FormatResponse(0x41, 0x10, 0x00, 0x64), // MAF
                0x11 => FormatResponse(0x41, 0x11, (byte)(simulatedThrottle * 255 / 100)),
                0x1F => FormatResponse(0x41, 0x1F,
                    (byte)((int)(DateTime.Now - engineStartTime).TotalSeconds >> 8),
                    (byte)((int)(DateTime.Now - engineStartTime).TotalSeconds & 0xFF)),
                0x2F => FormatResponse(0x41, 0x2F, (byte)(simulatedFuelLevel * 255 / 100)),
                0x33 => FormatResponse(0x41, 0x33, 0x65), // Barometric pressure
                0x42 => FormatResponse(0x41, 0x42,
                    (byte)((int)(simulatedVoltage * 1000) >> 8),
                    (byte)((int)(simulatedVoltage * 1000) & 0xFF)),
                0x46 => FormatResponse(0x41, 0x46, 0x28), // Ambient air temp
                0x5C => FormatResponse(0x41, 0x5C, 0x5A), // Oil temp (90°C)
                _ => "NO DATA\r\n>"
            };

            return response;
        }

        /// <summary>
        /// Mode 02 - Freeze Frame Data
        /// </summary>
        private string ProcessMode02(byte pid)
        {
            // Simulate freeze frame from when P0301 occurred
            return pid switch
            {
                0x02 => "42 02 00 01 01\r\n>", // Frame 0, DTC P0301
                0x0C => "42 0C 00 0B B8\r\n>", // RPM was 750 at fault
                0x0D => "42 0D 00 3C\r\n>", // Speed was 60 km/h
                0x05 => "42 05 00 7D\r\n>", // Coolant was 85°C
                0x11 => "42 11 00 33\r\n>", // Throttle was 20%
                _ => "NO DATA\r\n>"
            };
        }

        /// <summary>
        /// Mode 03 - Read DTCs
        /// </summary>
        private string ProcessMode03()
        {
            if (storedDTCs.Count == 0)
            {
                return "43 00\r\n>"; // No DTCs
            }

            // Build response: 43 [count] [DTC bytes...]
            StringBuilder sb = new StringBuilder("43 ");
            sb.Append($"{storedDTCs.Count:X2} ");

            foreach (var (byte1, byte2) in storedDTCs)
            {
                sb.Append($"{byte1:X2} {byte2:X2} ");
            }

            sb.Append("\r\n>");
            return sb.ToString();
        }

        /// <summary>
        /// Mode 04 - Clear DTCs
        /// </summary>
        private string ProcessMode04()
        {
            Console.WriteLine("[MOCK] Clearing diagnostic codes...");
            storedDTCs.Clear();
            return "44\r\n>";
        }

        /// <summary>
        /// Mode 05 - O2 Sensor Test Results
        /// </summary>
        private string ProcessMode05(byte sensorId)
        {
            // Simulate O2 sensor on Bank 1 Sensor 1
            if (sensorId == 0x01)
            {
                return "45 01 03 E8 00 64 32 4B\r\n>"; // Voltage 1.0V, current 100mA
            }
            return "NO DATA\r\n>";
        }

        /// <summary>
        /// Mode 06 - Monitoring Test Results
        /// </summary>
        private string ProcessMode06(byte testId)
        {
            // Simulate catalyst monitor test
            return "46 00 01 85 0B 0A 00 FF FF\r\n>"; // Test ID 1, value in range
        }

        /// <summary>
        /// Mode 07 - Pending DTCs
        /// </summary>
        private string ProcessMode07()
        {
            // No pending codes in this simulation
            return "47 00\r\n>";
        }

        /// <summary>
        /// Mode 08 - Control Operation (Bidirectional)
        /// </summary>
        private string ProcessMode08(byte testId)
        {
            Console.WriteLine($"[MOCK] Executing bidirectional test: 0x{testId:X2}");

            return testId switch
            {
                0x01 => "48 01 00\r\n>", // EVAP leak test initiated
                0x05 => "48 05 00\r\n>", // Catalyst test initiated
                _ => "NO DATA\r\n>"
            };
        }

        /// <summary>
        /// Mode 09 - Vehicle Information
        /// </summary>
        private string ProcessMode09(byte infoType)
        {
            return infoType switch
            {
                0x02 => "49 02 01 31 47 33 45 4B 32 35 53 31 38 37 36 35 34 33 32 31\r\n>", // VIN: 1G3EK25S187654321
                0x04 => "49 04 47 4D 20 43 41 4C 49 42\r\n>", // Calibration ID: GM CALIB
                0x06 => "49 06 01 07\r\n>", // CVN
                0x0A => "49 0A 01 45 43 4D 2D 31\r\n>", // ECU Name: ECM-1
                _ => "NO DATA\r\n>"
            };
        }

        /// <summary>
        /// Mode 0A - Permanent DTCs
        /// </summary>
        private string ProcessMode0A()
        {
            // No permanent codes
            return "4A 00\r\n>";
        }

        /// <summary>
        /// Helper to format OBD response
        /// </summary>
        private string FormatResponse(params byte[] bytes)
        {
            return string.Join(" ", bytes.Select(b => b.ToString("X2"))) + "\r\n>";
        }

        /// <summary>
        /// Send OBD request (compatible with ELM327 interface)
        /// </summary>
        public async Task<byte[]> SendOBDRequestAsync(byte mode, byte pid, params byte[] additionalData)
        {
            List<byte> request = new List<byte> { mode, pid };
            if (additionalData != null && additionalData.Length > 0)
                request.AddRange(additionalData);

            string hexCommand = string.Join(" ", request.Select(b => b.ToString("X2")));
            string response = await SendCommandAsync(hexCommand);

            return ParseHexResponse(response);
        }

        /// <summary>
        /// Parse hex response
        /// </summary>
        private byte[] ParseHexResponse(string response)
        {
            if (string.IsNullOrWhiteSpace(response))
                return new byte[0];

            if (response.Contains("NO DATA") || response.Contains("ERROR"))
                return new byte[0];

            response = response.Replace("SEARCHING...", "")
                             .Replace(">", "")
                             .Replace(" ", "")
                             .Replace("\r", "")
                             .Replace("\n", "")
                             .Trim();

            List<byte> bytes = new List<byte>();

            for (int i = 0; i < response.Length; i += 2)
            {
                if (i + 1 < response.Length)
                {
                    string hexByte = response.Substring(i, 2);
                    if (byte.TryParse(hexByte, System.Globalization.NumberStyles.HexNumber, null, out byte b))
                        bytes.Add(b);
                }
            }

            return bytes.ToArray();
        }

        public async Task<double> ReadVoltageAsync()
        {
            string response = await SendCommandAsync("ATRV");
            return simulatedVoltage;
        }

        public static string[] GetAvailablePorts()
        {
            return new[] { "MOCK_COM1", "MOCK_COM2" };
        }

        public void Dispose()
        {
            Disconnect();
        }
    }
}
