using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using OBDReader.Core.Models;
using OBDReader.Core.Protocol;
using OBDReader.Core.Services;

namespace OBDReader.Windows
{
    public partial class MainWindow : Window
    {
        private ELM327 adapter;
        private OBDService obdService;
        private DispatcherTimer dataUpdateTimer;
        private CancellationTokenSource cancellationTokenSource;
        private bool isMonitoring = false;

        private ObservableCollection<LiveDataItem> liveDataItems = new ObservableCollection<LiveDataItem>();
        private ObservableCollection<DiagnosticTroubleCode> dtcItems = new ObservableCollection<DiagnosticTroubleCode>();
        private ObservableCollection<LiveDataItem> freezeFrameItems = new ObservableCollection<LiveDataItem>();

        public MainWindow()
        {
            InitializeComponent();
            InitializeApplication();
        }

        private void InitializeApplication()
        {
            // Populate COM ports
            RefreshPorts();

            // Setup data grids
            LiveDataGrid.ItemsSource = liveDataItems;
            DTCDataGrid.ItemsSource = dtcItems;
            FreezeFrameGrid.ItemsSource = freezeFrameItems;

            // Setup timer for live data updates
            dataUpdateTimer = new DispatcherTimer();
            dataUpdateTimer.Interval = TimeSpan.FromMilliseconds(500); // Update every 500ms
            dataUpdateTimer.Tick += DataUpdateTimer_Tick;

            StatusText.Text = "Ready - Select COM port and connect";
        }

        private void RefreshPorts()
        {
            string[] ports = SerialPort.GetPortNames();
            PortComboBox.ItemsSource = ports;
            if (ports.Length > 0)
                PortComboBox.SelectedIndex = 0;
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (PortComboBox.SelectedItem == null)
            {
                MessageBox.Show("Please select a COM port", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string portName = PortComboBox.SelectedItem.ToString();

            try
            {
                ConnectButton.IsEnabled = false;
                StatusText.Text = "Connecting...";

                // Create adapter and service
                adapter = new ELM327(portName);
                obdService = new OBDService(adapter);

                // Subscribe to events
                obdService.StatusChanged += (s, msg) => Dispatcher.Invoke(() => StatusText.Text = msg);
                obdService.ErrorOccurred += (s, ex) => Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Error: {ex.Message}", "OBD Error", MessageBoxButton.OK, MessageBoxImage.Error);
                });

                // Connect
                bool connected = await obdService.ConnectAsync();

                if (connected)
                {
                    ConnectionStatusIndicator.Fill = new SolidColorBrush(Color.FromRgb(76, 175, 80)); // Green
                    ConnectButton.IsEnabled = false;
                    DisconnectButton.IsEnabled = true;
                    PortComboBox.IsEnabled = false;

                    StatusText.Text = "Connected - Ready for diagnostics";

                    // Read vehicle voltage
                    double voltage = await adapter.ReadVoltageAsync();
                    VoltageText.Text = voltage.ToString("F1");

                    // Start monitoring
                    StartMonitoring();
                }
                else
                {
                    MessageBox.Show("Failed to connect to vehicle. Please check adapter connection and try again.",
                        "Connection Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                    ConnectButton.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Connection error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                ConnectButton.IsEnabled = true;
            }
        }

        private void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            StopMonitoring();

            obdService?.Disconnect();
            adapter?.Dispose();

            ConnectionStatusIndicator.Fill = new SolidColorBrush(Color.FromRgb(211, 47, 47)); // Red
            ConnectButton.IsEnabled = true;
            DisconnectButton.IsEnabled = false;
            PortComboBox.IsEnabled = true;

            StatusText.Text = "Disconnected";
        }

        private void StartMonitoring()
        {
            if (!isMonitoring)
            {
                isMonitoring = true;
                cancellationTokenSource = new CancellationTokenSource();
                dataUpdateTimer.Start();
            }
        }

        private void StopMonitoring()
        {
            if (isMonitoring)
            {
                isMonitoring = false;
                dataUpdateTimer.Stop();
                cancellationTokenSource?.Cancel();
            }
        }

        private async void DataUpdateTimer_Tick(object sender, EventArgs e)
        {
            if (obdService == null || !isMonitoring)
                return;

            try
            {
                // Read key PIDs
                var pidData = await obdService.ReadMultiplePIDsAsync(
                    0x0C, // Engine RPM
                    0x0D, // Vehicle Speed
                    0x05, // Coolant Temperature
                    0x11, // Throttle Position
                    0x04, // Engine Load
                    0x2F, // Fuel Level
                    0x0F  // Intake Air Temp
                );

                // Update dashboard
                if (pidData.ContainsKey(0x0C))
                    RPMText.Text = pidData[0x0C].ToString("F0");

                if (pidData.ContainsKey(0x0D))
                    SpeedText.Text = pidData[0x0D].ToString("F0");

                if (pidData.ContainsKey(0x05))
                    CoolantTempText.Text = pidData[0x05].ToString("F0");

                if (pidData.ContainsKey(0x11))
                    ThrottleText.Text = pidData[0x11].ToString("F1");

                if (pidData.ContainsKey(0x04))
                    EngineLoadText.Text = pidData[0x04].ToString("F1");

                if (pidData.ContainsKey(0x2F))
                    FuelLevelText.Text = pidData[0x2F].ToString("F1");

                if (pidData.ContainsKey(0x0F))
                    IntakeAirTempText.Text = pidData[0x0F].ToString("F0");

                LastUpdateText.Text = $"Last Update: {DateTime.Now:HH:mm:ss}";

                // Update live data grid
                UpdateLiveDataGrid(pidData);
            }
            catch (Exception ex)
            {
                // Log error but don't stop monitoring
                System.Diagnostics.Debug.WriteLine($"Data update error: {ex.Message}");
            }
        }

        private void UpdateLiveDataGrid(Dictionary<byte, double> pidData)
        {
            liveDataItems.Clear();

            foreach (var kvp in pidData)
            {
                var pidDef = OBDPIDs.GetPID(0x01, kvp.Key);
                if (pidDef != null)
                {
                    liveDataItems.Add(new LiveDataItem
                    {
                        Name = pidDef.Name,
                        Value = kvp.Value.ToString("F2"),
                        Unit = pidDef.Unit
                    });
                }
            }
        }

        private async void ReadCodesButton_Click(object sender, RoutedEventArgs e)
        {
            if (obdService == null)
                return;

            try
            {
                StatusText.Text = "Reading diagnostic codes...";

                var codes = await obdService.ReadDiagnosticCodesAsync();

                dtcItems.Clear();
                foreach (var code in codes)
                {
                    dtcItems.Add(code);
                }

                UpdateDTCSummary();

                StatusText.Text = $"Read {codes.Count} diagnostic code(s)";

                if (codes.Count == 0)
                {
                    MessageBox.Show("No diagnostic trouble codes found!", "Codes Read", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading codes: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ClearCodesButton_Click(object sender, RoutedEventArgs e)
        {
            if (obdService == null)
                return;

            var result = MessageBox.Show(
                "This will clear all diagnostic trouble codes and reset readiness monitors.\n\nAre you sure you want to continue?",
                "Clear Codes",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                StatusText.Text = "Clearing diagnostic codes...";

                bool success = await obdService.ClearDiagnosticCodesAsync();

                if (success)
                {
                    dtcItems.Clear();
                    UpdateDTCSummary();
                    StatusText.Text = "Diagnostic codes cleared successfully";
                    MessageBox.Show("Diagnostic codes cleared successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Failed to clear codes. Please try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error clearing codes: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ReadPendingButton_Click(object sender, RoutedEventArgs e)
        {
            if (obdService == null)
                return;

            try
            {
                StatusText.Text = "Reading pending codes...";

                var codes = await obdService.ReadPendingCodesAsync();

                dtcItems.Clear();
                foreach (var code in codes)
                {
                    dtcItems.Add(code);
                }

                UpdateDTCSummary();

                StatusText.Text = $"Read {codes.Count} pending code(s)";

                if (codes.Count == 0)
                {
                    MessageBox.Show("No pending trouble codes found!", "Pending Codes", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading pending codes: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateDTCSummary()
        {
            TotalCodesText.Text = dtcItems.Count.ToString();
            ConfirmedCodesText.Text = dtcItems.Count(c => c.Status == DiagnosticTroubleCode.DTCStatus.Confirmed).ToString();
            PendingCodesText.Text = dtcItems.Count(c => c.Status == DiagnosticTroubleCode.DTCStatus.Pending).ToString();
        }

        private async void ReadFreezeFrameButton_Click(object sender, RoutedEventArgs e)
        {
            if (obdService == null)
                return;

            try
            {
                StatusText.Text = "Reading freeze frame data...";

                var freezeData = await obdService.ReadFreezeFrameAsync();

                freezeFrameItems.Clear();
                foreach (var kvp in freezeData)
                {
                    var pidDef = OBDPIDs.GetPID(0x01, kvp.Key);
                    if (pidDef != null)
                    {
                        freezeFrameItems.Add(new LiveDataItem
                        {
                            Name = pidDef.Name,
                            Value = kvp.Value.ToString("F2"),
                            Unit = pidDef.Unit
                        });
                    }
                }

                StatusText.Text = "Freeze frame data retrieved";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading freeze frame: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ReadVehicleInfoButton_Click(object sender, RoutedEventArgs e)
        {
            if (obdService == null)
                return;

            try
            {
                StatusText.Text = "Reading vehicle information...";

                string vin = await obdService.ReadVINAsync();
                string calId = await obdService.ReadCalibrationIDAsync();

                VINText.Text = string.IsNullOrEmpty(vin) ? "Not available" : vin;
                CalibrationIDText.Text = string.IsNullOrEmpty(calId) ? "Not available" : calId;
                ProtocolText.Text = adapter.ProtocolVersion ?? "Unknown";

                StatusText.Text = "Vehicle information retrieved";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading vehicle info: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void TestEVAPButton_Click(object sender, RoutedEventArgs e)
        {
            if (obdService == null)
                return;

            var result = MessageBox.Show(
                "This will initiate the EVAP system test. Ensure the vehicle is in a safe condition.\n\nContinue?",
                "EVAP Test",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                StatusText.Text = "Running EVAP system test...";

                bool success = await obdService.TestEVAPSystemAsync();

                if (success)
                {
                    MessageBox.Show("EVAP system test completed. Check for results in monitoring data.",
                        "Test Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                    StatusText.Text = "EVAP test completed";
                }
                else
                {
                    MessageBox.Show("EVAP test failed or not supported.", "Test Failed",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error running EVAP test: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void TestCatalystButton_Click(object sender, RoutedEventArgs e)
        {
            if (obdService == null)
                return;

            var result = MessageBox.Show(
                "This will initiate the catalytic converter test. Ensure the vehicle is in a safe condition.\n\nContinue?",
                "Catalyst Test",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                StatusText.Text = "Running catalyst test...";

                bool success = await obdService.TestCatalyticConverterAsync();

                if (success)
                {
                    MessageBox.Show("Catalytic converter test completed. Check for results in monitoring data.",
                        "Test Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                    StatusText.Text = "Catalyst test completed";
                }
                else
                {
                    MessageBox.Show("Catalyst test failed or not supported.", "Test Failed",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error running catalyst test: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            StopMonitoring();
            obdService?.Dispose();
            adapter?.Dispose();
            base.OnClosed(e);
        }
    }

    // Data models for UI binding
    public class LiveDataItem
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public string Unit { get; set; }
    }
}
