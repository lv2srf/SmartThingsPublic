package com.obdreader.android

import android.Manifest
import android.bluetooth.BluetoothAdapter
import android.bluetooth.BluetoothDevice
import android.bluetooth.BluetoothSocket
import android.content.pm.PackageManager
import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.core.app.ActivityCompat
import androidx.lifecycle.lifecycleScope
import com.obdreader.android.ui.theme.OBDReaderTheme
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.delay
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import java.io.IOException
import java.io.InputStream
import java.io.OutputStream
import java.util.*

class MainActivity : ComponentActivity() {

    private var bluetoothAdapter: BluetoothAdapter? = null
    private var bluetoothSocket: BluetoothSocket? = null
    private var inputStream: InputStream? = null
    private var outputStream: OutputStream? = null
    private var isConnected = mutableStateOf(false)

    // Real-time data state
    private var rpm = mutableStateOf(0)
    private var speed = mutableStateOf(0)
    private var coolantTemp = mutableStateOf(0)
    private var throttle = mutableStateOf(0f)
    private var engineLoad = mutableStateOf(0f)
    private var fuelLevel = mutableStateOf(0f)
    private var intakeAirTemp = mutableStateOf(0)
    private var voltage = mutableStateOf(0.0)

    private val requestPermissionLauncher = registerForActivityResult(
        ActivityResultContracts.RequestMultiplePermissions()
    ) { permissions ->
        if (permissions[Manifest.permission.BLUETOOTH_CONNECT] == true) {
            // Permission granted
        }
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        // Request Bluetooth permissions
        if (ActivityCompat.checkSelfPermission(
                this,
                Manifest.permission.BLUETOOTH_CONNECT
            ) != PackageManager.PERMISSION_GRANTED
        ) {
            requestPermissionLauncher.launch(
                arrayOf(
                    Manifest.permission.BLUETOOTH_CONNECT,
                    Manifest.permission.BLUETOOTH_SCAN,
                    Manifest.permission.ACCESS_FINE_LOCATION
                )
            )
        }

        bluetoothAdapter = BluetoothAdapter.getDefaultAdapter()

        setContent {
            OBDReaderTheme {
                Surface(
                    modifier = Modifier.fillMaxSize(),
                    color = MaterialTheme.colorScheme.background
                ) {
                    OBDReaderApp(
                        isConnected = isConnected.value,
                        rpm = rpm.value,
                        speed = speed.value,
                        coolantTemp = coolantTemp.value,
                        throttle = throttle.value,
                        engineLoad = engineLoad.value,
                        fuelLevel = fuelLevel.value,
                        intakeAirTemp = intakeAirTemp.value,
                        voltage = voltage.value,
                        onConnect = { device -> connectToDevice(device) },
                        onDisconnect = { disconnectFromDevice() },
                        onReadCodes = { readDiagnosticCodes() },
                        onClearCodes = { clearDiagnosticCodes() },
                        getPairedDevices = { getPairedBluetoothDevices() }
                    )
                }
            }
        }
    }

    private fun getPairedBluetoothDevices(): List<BluetoothDevice> {
        if (ActivityCompat.checkSelfPermission(
                this,
                Manifest.permission.BLUETOOTH_CONNECT
            ) != PackageManager.PERMISSION_GRANTED
        ) {
            return emptyList()
        }

        return bluetoothAdapter?.bondedDevices?.toList() ?: emptyList()
    }

    private fun connectToDevice(device: BluetoothDevice) {
        lifecycleScope.launch(Dispatchers.IO) {
            try {
                if (ActivityCompat.checkSelfPermission(
                        this@MainActivity,
                        Manifest.permission.BLUETOOTH_CONNECT
                    ) != PackageManager.PERMISSION_GRANTED
                ) {
                    return@launch
                }

                // Standard SerialPortService UUID
                val uuid = UUID.fromString("00001101-0000-1000-8000-00805F9B34FB")
                bluetoothSocket = device.createRfcommSocketToServiceRecord(uuid)
                bluetoothSocket?.connect()

                inputStream = bluetoothSocket?.inputStream
                outputStream = bluetoothSocket?.outputStream

                // Initialize ELM327
                sendCommand("ATZ\r")
                delay(1000)
                sendCommand("ATE0\r")
                sendCommand("ATL0\r")
                sendCommand("ATS0\r")
                sendCommand("ATH1\r")
                sendCommand("ATSP0\r")

                withContext(Dispatchers.Main) {
                    isConnected.value = true
                }

                // Start real-time data polling
                startDataPolling()
            } catch (e: IOException) {
                e.printStackTrace()
                withContext(Dispatchers.Main) {
                    isConnected.value = false
                }
            }
        }
    }

    private fun startDataPolling() {
        lifecycleScope.launch(Dispatchers.IO) {
            while (isConnected.value) {
                try {
                    // Read Engine RPM (PID 0x0C)
                    val rpmResponse = sendCommand("01 0C\r")
                    rpm.value = parseRPM(rpmResponse)

                    // Read Vehicle Speed (PID 0x0D)
                    val speedResponse = sendCommand("01 0D\r")
                    speed.value = parseSpeed(speedResponse)

                    // Read Coolant Temperature (PID 0x05)
                    val coolantResponse = sendCommand("01 05\r")
                    coolantTemp.value = parseCoolantTemp(coolantResponse)

                    // Read Throttle Position (PID 0x11)
                    val throttleResponse = sendCommand("01 11\r")
                    throttle.value = parseThrottle(throttleResponse)

                    // Read Engine Load (PID 0x04)
                    val loadResponse = sendCommand("01 04\r")
                    engineLoad.value = parseEngineLoad(loadResponse)

                    // Read Fuel Level (PID 0x2F)
                    val fuelResponse = sendCommand("01 2F\r")
                    fuelLevel.value = parseFuelLevel(fuelResponse)

                    // Read Intake Air Temp (PID 0x0F)
                    val intakeResponse = sendCommand("01 0F\r")
                    intakeAirTemp.value = parseIntakeAirTemp(intakeResponse)

                    delay(500) // 500ms polling interval (2 Hz)
                } catch (e: Exception) {
                    e.printStackTrace()
                    delay(1000) // Longer delay on error
                }
            }
        }
    }

    private fun parseRPM(response: String): Int {
        try {
            val hex = response.replace(Regex("[^0-9A-Fa-f]"), "")
            if (hex.length >= 8) {
                val a = hex.substring(4, 6).toInt(16)
                val b = hex.substring(6, 8).toInt(16)
                return ((a * 256 + b) / 4)
            }
        } catch (e: Exception) {
            e.printStackTrace()
        }
        return 0
    }

    private fun parseSpeed(response: String): Int {
        try {
            val hex = response.replace(Regex("[^0-9A-Fa-f]"), "")
            if (hex.length >= 6) {
                return hex.substring(4, 6).toInt(16)
            }
        } catch (e: Exception) {
            e.printStackTrace()
        }
        return 0
    }

    private fun parseCoolantTemp(response: String): Int {
        try {
            val hex = response.replace(Regex("[^0-9A-Fa-f]"), "")
            if (hex.length >= 6) {
                return hex.substring(4, 6).toInt(16) - 40
            }
        } catch (e: Exception) {
            e.printStackTrace()
        }
        return 0
    }

    private fun parseThrottle(response: String): Float {
        try {
            val hex = response.replace(Regex("[^0-9A-Fa-f]"), "")
            if (hex.length >= 6) {
                val value = hex.substring(4, 6).toInt(16)
                return (value * 100f / 255f)
            }
        } catch (e: Exception) {
            e.printStackTrace()
        }
        return 0f
    }

    private fun parseEngineLoad(response: String): Float {
        try {
            val hex = response.replace(Regex("[^0-9A-Fa-f]"), "")
            if (hex.length >= 6) {
                val value = hex.substring(4, 6).toInt(16)
                return (value * 100f / 255f)
            }
        } catch (e: Exception) {
            e.printStackTrace()
        }
        return 0f
    }

    private fun parseFuelLevel(response: String): Float {
        try {
            val hex = response.replace(Regex("[^0-9A-Fa-f]"), "")
            if (hex.length >= 6) {
                val value = hex.substring(4, 6).toInt(16)
                return (value * 100f / 255f)
            }
        } catch (e: Exception) {
            e.printStackTrace()
        }
        return 0f
    }

    private fun parseIntakeAirTemp(response: String): Int {
        try {
            val hex = response.replace(Regex("[^0-9A-Fa-f]"), "")
            if (hex.length >= 6) {
                return hex.substring(4, 6).toInt(16) - 40
            }
        } catch (e: Exception) {
            e.printStackTrace()
        }
        return 0
    }

    private fun disconnectFromDevice() {
        try {
            bluetoothSocket?.close()
            inputStream?.close()
            outputStream?.close()
            isConnected.value = false
        } catch (e: IOException) {
            e.printStackTrace()
        }
    }

    private fun sendCommand(command: String): String {
        try {
            outputStream?.write(command.toByteArray())
            outputStream?.flush()

            val buffer = ByteArray(1024)
            val bytes = inputStream?.read(buffer) ?: 0
            return String(buffer, 0, bytes)
        } catch (e: IOException) {
            e.printStackTrace()
            return ""
        }
    }

    private fun readDiagnosticCodes(): List<String> {
        val response = sendCommand("03\r")
        // Parse DTC codes from response
        return parseDTCs(response)
    }

    private fun clearDiagnosticCodes(): Boolean {
        val response = sendCommand("04\r")
        return response.contains("44")
    }

    private fun parseDTCs(response: String): List<String> {
        val codes = mutableListOf<String>()

        try {
            // Remove non-hex characters
            val cleanResponse = response.replace(Regex("[^0-9A-Fa-f]"), "")

            // Response format: 43 [count] [DTC bytes...]
            // Each DTC is 2 bytes
            if (cleanResponse.length < 4) return codes

            // Skip mode byte (43) and count byte, start at position 4
            var i = 4
            while (i + 3 < cleanResponse.length) {
                val byte1 = cleanResponse.substring(i, i + 2).toIntOrNull(16) ?: break
                val byte2 = cleanResponse.substring(i + 2, i + 4).toIntOrNull(16) ?: break

                // Decode DTC
                val typeChar = when ((byte1 shr 6) and 0x03) {
                    0x00 -> 'P'  // Powertrain
                    0x01 -> 'C'  // Chassis
                    0x02 -> 'B'  // Body
                    0x03 -> 'U'  // Network
                    else -> 'P'
                }

                val firstDigit = (byte1 shr 4) and 0x03
                val secondDigit = byte1 and 0x0F
                val thirdDigit = (byte2 shr 4) and 0x0F
                val fourthDigit = byte2 and 0x0F

                val dtcCode = "$typeChar$firstDigit${secondDigit.toString(16).uppercase()}" +
                             "${thirdDigit.toString(16).uppercase()}${fourthDigit.toString(16).uppercase()}"

                // Skip padding codes (0000)
                if (dtcCode != "P0000") {
                    codes.add(dtcCode)
                }

                i += 4
            }
        } catch (e: Exception) {
            e.printStackTrace()
        }

        return codes
    }

    override fun onDestroy() {
        super.onDestroy()
        disconnectFromDevice()
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun OBDReaderApp(
    isConnected: Boolean,
    rpm: Int,
    speed: Int,
    coolantTemp: Int,
    throttle: Float,
    engineLoad: Float,
    fuelLevel: Float,
    intakeAirTemp: Int,
    voltage: Double,
    onConnect: (BluetoothDevice) -> Unit,
    onDisconnect: () -> Unit,
    onReadCodes: () -> List<String>,
    onClearCodes: () -> Boolean,
    getPairedDevices: () -> List<BluetoothDevice>
) {
    var selectedTab by remember { mutableStateOf(0) }
    var showDeviceDialog by remember { mutableStateOf(false) }

    Scaffold(
        topBar = {
            TopAppBar(
                title = {
                    Row(verticalAlignment = Alignment.CenterVertically) {
                        Icon(
                            Icons.Default.DirectionsCar,
                            contentDescription = null,
                            tint = Color(0xFF00BCD4),
                            modifier = Modifier.size(32.dp)
                        )
                        Spacer(Modifier.width(8.dp))
                        Text("OBD-II Diagnostic Tool")
                    }
                },
                actions = {
                    if (!isConnected) {
                        IconButton(onClick = { showDeviceDialog = true }) {
                            Icon(Icons.Default.Bluetooth, "Connect")
                        }
                    } else {
                        IconButton(onClick = onDisconnect) {
                            Icon(Icons.Default.BluetoothDisabled, "Disconnect")
                        }
                    }
                },
                colors = TopAppBarDefaults.topAppBarColors(
                    containerColor = Color(0xFF1E1E1E),
                    titleContentColor = Color.White
                )
            )
        },
        bottomBar = {
            NavigationBar(containerColor = Color(0xFF2D2D30)) {
                NavigationBarItem(
                    icon = { Icon(Icons.Default.Dashboard, "Dashboard") },
                    label = { Text("Dashboard") },
                    selected = selectedTab == 0,
                    onClick = { selectedTab = 0 }
                )
                NavigationBarItem(
                    icon = { Icon(Icons.Default.Warning, "Codes") },
                    label = { Text("Codes") },
                    selected = selectedTab == 1,
                    onClick = { selectedTab = 1 }
                )
                NavigationBarItem(
                    icon = { Icon(Icons.Default.Info, "Vehicle") },
                    label = { Text("Info") },
                    selected = selectedTab == 2,
                    onClick = { selectedTab = 2 }
                )
                NavigationBarItem(
                    icon = { Icon(Icons.Default.Build, "Tests") },
                    label = { Text("Tests") },
                    selected = selectedTab == 3,
                    onClick = { selectedTab = 3 }
                )
            }
        }
    ) { padding ->
        Box(modifier = Modifier.padding(padding)) {
            when (selectedTab) {
                0 -> DashboardScreen(rpm, speed, coolantTemp, throttle, engineLoad, fuelLevel, intakeAirTemp, voltage)
                1 -> TroubleCodesScreen(onReadCodes, onClearCodes)
                2 -> VehicleInfoScreen()
                3 -> ActuatorTestsScreen()
            }
        }
    }

    if (showDeviceDialog) {
        DeviceSelectionDialog(
            devices = getPairedDevices(),
            onDeviceSelected = { device ->
                onConnect(device)
                showDeviceDialog = false
            },
            onDismiss = { showDeviceDialog = false }
        )
    }
}

@Composable
fun DashboardScreen(
    rpm: Int,
    speed: Int,
    coolantTemp: Int,
    throttle: Float,
    engineLoad: Float,
    fuelLevel: Float,
    intakeAirTemp: Int,
    voltage: Double
) {
    Column(
        modifier = Modifier
            .fillMaxSize()
            .background(Color(0xFF1E1E1E))
            .padding(16.dp)
    ) {
        // Metrics Grid Row 1
        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.SpaceEvenly
        ) {
            MetricCard("RPM", rpm.toString(), "RPM", Color(0xFF00BCD4))
            MetricCard("Speed", speed.toString(), "km/h", Color(0xFF4CAF50))
        }

        Spacer(Modifier.height(16.dp))

        // Metrics Grid Row 2
        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.SpaceEvenly
        ) {
            MetricCard("Coolant", coolantTemp.toString(), "°C", Color(0xFFFF9800))
            MetricCard("Throttle", String.format("%.1f", throttle), "%", Color(0xFF9C27B0))
        }

        Spacer(Modifier.height(16.dp))

        // Metrics Grid Row 3
        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.SpaceEvenly
        ) {
            MetricCard("Load", String.format("%.1f", engineLoad), "%", Color(0xFFE91E63))
            MetricCard("Fuel", String.format("%.1f", fuelLevel), "%", Color(0xFFFFC107))
        }

        Spacer(Modifier.height(16.dp))

        // Metrics Grid Row 4
        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.SpaceEvenly
        ) {
            MetricCard("Intake", intakeAirTemp.toString(), "°C", Color(0xFF00BCD4))
            MetricCard("Battery", String.format("%.1f", voltage), "V", Color(0xFF8BC34A))
        }
    }
}

@Composable
fun MetricCard(label: String, value: String, unit: String, color: Color) {
    Card(
        modifier = Modifier
            .width(160.dp)
            .height(120.dp),
        colors = CardDefaults.cardColors(containerColor = Color(0xFF2D2D30)),
        shape = RoundedCornerShape(8.dp)
    ) {
        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(16.dp),
            verticalArrangement = Arrangement.SpaceBetween
        ) {
            Text(
                text = label,
                fontSize = 12.sp,
                color = Color.Gray
            )
            Text(
                text = value,
                fontSize = 32.sp,
                fontWeight = FontWeight.Bold,
                color = color
            )
            Text(
                text = unit,
                fontSize = 10.sp,
                color = Color.Gray
            )
        }
    }
}

@Composable
fun TroubleCodesScreen(onReadCodes: () -> List<String>, onClearCodes: () -> Boolean) {
    var codes by remember { mutableStateOf<List<String>>(emptyList()) }

    Column(
        modifier = Modifier
            .fillMaxSize()
            .background(Color(0xFF1E1E1E))
            .padding(16.dp)
    ) {
        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.spacedBy(8.dp)
        ) {
            Button(
                onClick = { codes = onReadCodes() },
                modifier = Modifier.weight(1f),
                colors = ButtonDefaults.buttonColors(containerColor = Color(0xFF00BCD4))
            ) {
                Text("Read Codes")
            }
            Button(
                onClick = {
                    if (onClearCodes()) {
                        codes = emptyList()
                    }
                },
                modifier = Modifier.weight(1f),
                colors = ButtonDefaults.buttonColors(containerColor = Color(0xFFD32F2F))
            ) {
                Text("Clear Codes")
            }
        }

        Spacer(Modifier.height(16.dp))

        if (codes.isEmpty()) {
            Text(
                "No diagnostic codes found",
                color = Color.White,
                modifier = Modifier.align(Alignment.CenterHorizontally)
            )
        } else {
            LazyColumn {
                items(codes) { code ->
                    Card(
                        modifier = Modifier
                            .fillMaxWidth()
                            .padding(vertical = 4.dp),
                        colors = CardDefaults.cardColors(containerColor = Color(0xFF2D2D30))
                    ) {
                        Text(
                            text = code,
                            color = Color.White,
                            modifier = Modifier.padding(16.dp)
                        )
                    }
                }
            }
        }
    }
}

@Composable
fun VehicleInfoScreen() {
    Column(
        modifier = Modifier
            .fillMaxSize()
            .background(Color(0xFF1E1E1E))
            .padding(16.dp)
    ) {
        InfoCard("VIN", "Not available")
        InfoCard("Protocol", "ISO 15765-4 (CAN)")
        InfoCard("Calibration ID", "Not available")
    }
}

@Composable
fun InfoCard(label: String, value: String) {
    Card(
        modifier = Modifier
            .fillMaxWidth()
            .padding(vertical = 8.dp),
        colors = CardDefaults.cardColors(containerColor = Color(0xFF2D2D30))
    ) {
        Column(modifier = Modifier.padding(16.dp)) {
            Text(text = label, fontSize = 12.sp, color = Color.Gray)
            Spacer(Modifier.height(4.dp))
            Text(text = value, fontSize = 16.sp, color = Color(0xFF00BCD4), fontWeight = FontWeight.Bold)
        }
    }
}

@Composable
fun ActuatorTestsScreen() {
    Column(
        modifier = Modifier
            .fillMaxSize()
            .background(Color(0xFF1E1E1E))
            .padding(16.dp)
    ) {
        Text(
            "Bidirectional Control Tests",
            fontSize = 18.sp,
            fontWeight = FontWeight.Bold,
            color = Color.White,
            modifier = Modifier.padding(bottom = 16.dp)
        )

        ActuatorTestCard(
            "EVAP System Test",
            "Tests the evaporative emission control system for leaks",
            onClick = {
                // Send Mode 08 command for EVAP leak test (TID 0x01)
                lifecycleScope.launch(Dispatchers.IO) {
                    try {
                        val response = sendCommand("08 01\r")
                        withContext(Dispatchers.Main) {
                            if (response.contains("48")) {
                                // Test initiated successfully
                                android.widget.Toast.makeText(
                                    this@MainActivity,
                                    "EVAP test initiated. Check results after 2-5 minutes.",
                                    android.widget.Toast.LENGTH_LONG
                                ).show()
                            } else {
                                android.widget.Toast.makeText(
                                    this@MainActivity,
                                    "EVAP test not supported or failed",
                                    android.widget.Toast.LENGTH_SHORT
                                ).show()
                            }
                        }
                    } catch (e: Exception) {
                        e.printStackTrace()
                    }
                }
            }
        )

        Spacer(Modifier.height(16.dp))

        ActuatorTestCard(
            "Catalytic Converter Test",
            "Tests catalytic converter efficiency",
            onClick = {
                // Send Mode 08 command for catalyst test (TID 0x05)
                lifecycleScope.launch(Dispatchers.IO) {
                    try {
                        val response = sendCommand("08 05\r")
                        withContext(Dispatchers.Main) {
                            if (response.contains("48")) {
                                // Test initiated successfully
                                android.widget.Toast.makeText(
                                    this@MainActivity,
                                    "Catalyst test initiated. Drive normally for 5-10 minutes.",
                                    android.widget.Toast.LENGTH_LONG
                                ).show()
                            } else {
                                android.widget.Toast.makeText(
                                    this@MainActivity,
                                    "Catalyst test not supported or failed",
                                    android.widget.Toast.LENGTH_SHORT
                                ).show()
                            }
                        }
                    } catch (e: Exception) {
                        e.printStackTrace()
                    }
                }
            }
        )
    }
}

@Composable
fun ActuatorTestCard(title: String, description: String, onClick: () -> Unit) {
    Card(
        modifier = Modifier.fillMaxWidth(),
        colors = CardDefaults.cardColors(containerColor = Color(0xFF2D2D30))
    ) {
        Column(modifier = Modifier.padding(16.dp)) {
            Text(title, fontSize = 16.sp, fontWeight = FontWeight.Bold, color = Color.White)
            Spacer(Modifier.height(8.dp))
            Text(description, fontSize = 12.sp, color = Color.Gray)
            Spacer(Modifier.height(12.dp))
            Button(
                onClick = onClick,
                colors = ButtonDefaults.buttonColors(containerColor = Color(0xFF00BCD4))
            ) {
                Text("Run Test")
            }
        }
    }
}

@Composable
fun DeviceSelectionDialog(
    devices: List<BluetoothDevice>,
    onDeviceSelected: (BluetoothDevice) -> Unit,
    onDismiss: () -> Unit
) {
    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text("Select OBD Adapter") },
        text = {
            LazyColumn {
                items(devices) { device ->
                    TextButton(
                        onClick = { onDeviceSelected(device) },
                        modifier = Modifier.fillMaxWidth()
                    ) {
                        if (ActivityCompat.checkSelfPermission(
                                androidx.compose.ui.platform.LocalContext.current,
                                Manifest.permission.BLUETOOTH_CONNECT
                            ) == PackageManager.PERMISSION_GRANTED
                        ) {
                            Text(device.name ?: "Unknown Device")
                        }
                    }
                }
            }
        },
        confirmButton = {
            TextButton(onClick = onDismiss) {
                Text("Cancel")
            }
        }
    )
}
