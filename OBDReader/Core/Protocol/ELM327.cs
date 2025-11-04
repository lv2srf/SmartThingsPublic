using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OBDReader.Core.Protocol
{
    /// <summary>
    /// ELM327 OBD-II Adapter Interface
    /// Provides serial communication and command interface for ELM327-based adapters
    /// </summary>
    public class ELM327 : IDisposable
    {
        private SerialPort serialPort;
        private readonly object lockObject = new object();
        private bool isConnected = false;

        // ELM327 Command Responses
        private const string PROMPT = ">";
        private const string OK = "OK";
        private const string ERROR = "ERROR";
        private const string NO_DATA = "NO DATA";
        private const string UNABLE_TO_CONNECT = "UNABLE TO CONNECT";
        private const string SEARCHING = "SEARCHING...";

        // Timeouts
        private const int READ_TIMEOUT = 2000;
        private const int COMMAND_DELAY = 100;

        public string PortName { get; private set; }
        public int BaudRate { get; private set; }
        public string ProtocolVersion { get; private set; }
        public string DeviceDescription { get; private set; }

        public event EventHandler<string> DataReceived;
        public event EventHandler<string> ErrorOccurred;

        public ELM327(string portName, int baudRate = 38400)
        {
            PortName = portName;
            BaudRate = baudRate;
        }

        /// <summary>
        /// Open connection to ELM327 adapter
        /// </summary>
        public async Task<bool> ConnectAsync()
        {
            try
            {
                serialPort = new SerialPort(PortName, BaudRate)
                {
                    DataBits = 8,
                    Parity = Parity.None,
                    StopBits = StopBits.One,
                    Handshake = Handshake.None,
                    ReadTimeout = READ_TIMEOUT,
                    WriteTimeout = READ_TIMEOUT,
                    DtrEnable = true,
                    RtsEnable = true
                };

                serialPort.Open();
                await Task.Delay(100); // Allow port to stabilize

                // Reset adapter
                if (!await SendCommandAsync("ATZ"))
                    return false;

                await Task.Delay(500); // Wait for reset

                // Disable echo
                if (!await SendCommandAsync("ATE0"))
                    return false;

                // Disable line feeds
                if (!await SendCommandAsync("ATL0"))
                    return false;

                // Disable spaces
                if (!await SendCommandAsync("ATS0"))
                    return false;

                // Enable headers (for CAN analysis)
                if (!await SendCommandAsync("ATH1"))
                    return false;

                // Set protocol to automatic ISO 15765-4 (CAN)
                if (!await SendCommandAsync("ATSP0"))
                    return false;

                // Get device description
                DeviceDescription = await SendCommandAsync("AT@1");

                // Get protocol version
                ProtocolVersion = await SendCommandAsync("ATDPN");

                isConnected = true;
                return true;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"Connection failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Disconnect from adapter
        /// </summary>
        public void Disconnect()
        {
            try
            {
                if (serialPort != null && serialPort.IsOpen)
                {
                    serialPort.Close();
                }
                isConnected = false;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"Disconnect error: {ex.Message}");
            }
        }

        /// <summary>
        /// Send AT command to ELM327
        /// </summary>
        public async Task<string> SendCommandAsync(string command)
        {
            if (!isConnected && serialPort?.IsOpen != true)
                throw new InvalidOperationException("Not connected to adapter");

            lock (lockObject)
            {
                try
                {
                    // Clear buffers
                    serialPort.DiscardInBuffer();
                    serialPort.DiscardOutBuffer();

                    // Send command
                    serialPort.WriteLine(command);

                    // Read response
                    StringBuilder response = new StringBuilder();
                    DateTime startTime = DateTime.Now;

                    while ((DateTime.Now - startTime).TotalMilliseconds < READ_TIMEOUT)
                    {
                        try
                        {
                            string line = serialPort.ReadLine();
                            line = line.Trim('\r', '\n', ' ', '\t');

                            if (string.IsNullOrEmpty(line))
                                continue;

                            // Skip echo of command
                            if (line == command)
                                continue;

                            // Check for prompt (end of response)
                            if (line.EndsWith(PROMPT))
                            {
                                line = line.TrimEnd('>');
                                if (!string.IsNullOrEmpty(line))
                                    response.AppendLine(line);
                                break;
                            }

                            response.AppendLine(line);
                        }
                        catch (TimeoutException)
                        {
                            break;
                        }
                    }

                    string result = response.ToString().Trim();
                    DataReceived?.Invoke(this, result);

                    return result;
                }
                catch (Exception ex)
                {
                    ErrorOccurred?.Invoke(this, $"Command error: {ex.Message}");
                    throw;
                }
            }
        }

        /// <summary>
        /// Send OBD-II request and get response
        /// </summary>
        public async Task<byte[]> SendOBDRequestAsync(byte mode, byte pid, params byte[] additionalData)
        {
            // Build request
            List<byte> request = new List<byte> { mode, pid };
            if (additionalData != null && additionalData.Length > 0)
                request.AddRange(additionalData);

            // Convert to hex string
            string hexCommand = string.Join(" ", request.Select(b => b.ToString("X2")));

            // Send command
            string response = await SendCommandAsync(hexCommand);

            // Parse response
            return ParseHexResponse(response);
        }

        /// <summary>
        /// Parse hex response from ELM327
        /// </summary>
        private byte[] ParseHexResponse(string response)
        {
            if (string.IsNullOrWhiteSpace(response))
                return new byte[0];

            // Handle error responses
            if (response.Contains(NO_DATA) || response.Contains(ERROR) || response.Contains(UNABLE_TO_CONNECT))
                return new byte[0];

            // Remove common non-hex characters
            response = response.Replace(SEARCHING, "")
                             .Replace(PROMPT, "")
                             .Replace(" ", "")
                             .Replace("\r", "")
                             .Replace("\n", "")
                             .Trim();

            // Parse hex bytes
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

        /// <summary>
        /// Get available serial ports
        /// </summary>
        public static string[] GetAvailablePorts()
        {
            return SerialPort.GetPortNames();
        }

        /// <summary>
        /// Set protocol explicitly
        /// </summary>
        public async Task<bool> SetProtocolAsync(Protocol protocol)
        {
            string command = protocol switch
            {
                Protocol.AUTO => "ATSP0",
                Protocol.ISO_15765_4_CAN_11bit_500k => "ATSP6",
                Protocol.ISO_15765_4_CAN_29bit_500k => "ATSP7",
                Protocol.ISO_15765_4_CAN_11bit_250k => "ATSP8",
                Protocol.ISO_15765_4_CAN_29bit_250k => "ATSP9",
                Protocol.SAE_J1939_CAN => "ATSPA",
                _ => "ATSP0"
            };

            string result = await SendCommandAsync(command);
            return result.Contains(OK);
        }

        /// <summary>
        /// Set adaptive timing
        /// </summary>
        public async Task<bool> SetAdaptiveTimingAsync(AdaptiveTiming timing)
        {
            string command = timing switch
            {
                AdaptiveTiming.Off => "ATAT0",
                AdaptiveTiming.Auto1 => "ATAT1",
                AdaptiveTiming.Auto2 => "ATAT2",
                _ => "ATAT1"
            };

            string result = await SendCommandAsync(command);
            return result.Contains(OK);
        }

        /// <summary>
        /// Set timeout value
        /// </summary>
        public async Task<bool> SetTimeoutAsync(int timeout)
        {
            // Timeout in multiples of 4ms (0-255)
            int timeoutValue = Math.Min(255, timeout / 4);
            string result = await SendCommandAsync($"ATST{timeoutValue:X2}");
            return result.Contains(OK);
        }

        /// <summary>
        /// Read voltage
        /// </summary>
        public async Task<double> ReadVoltageAsync()
        {
            string response = await SendCommandAsync("ATRV");
            if (double.TryParse(response.Replace("V", ""), out double voltage))
                return voltage;
            return 0.0;
        }

        public void Dispose()
        {
            Disconnect();
            serialPort?.Dispose();
        }

        /// <summary>
        /// OBD-II Protocol enumeration
        /// </summary>
        public enum Protocol
        {
            AUTO = 0,
            SAE_J1850_PWM = 1,
            SAE_J1850_VPW = 2,
            ISO_9141_2 = 3,
            ISO_14230_4_KWP_5baud = 4,
            ISO_14230_4_KWP_fast = 5,
            ISO_15765_4_CAN_11bit_500k = 6,
            ISO_15765_4_CAN_29bit_500k = 7,
            ISO_15765_4_CAN_11bit_250k = 8,
            ISO_15765_4_CAN_29bit_250k = 9,
            SAE_J1939_CAN = 10
        }

        /// <summary>
        /// Adaptive timing modes
        /// </summary>
        public enum AdaptiveTiming
        {
            Off = 0,
            Auto1 = 1,
            Auto2 = 2
        }
    }
}
