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
            } catch (e: IOException) {
                e.printStackTrace()
                withContext(Dispatchers.Main) {
                    isConnected.value = false
                }
            }
        }
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
        // Simple DTC parsing (actual implementation would be more complex)
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
    onConnect: (BluetoothDevice) -> Unit,
    onDisconnect: () -> Unit,
    onReadCodes: () -> List<String>,
    onClearCodes: () -> Boolean,
    getPairedDevices: () -> List<BluetoothDevice>
) {
    var selectedTab by remember { mutableStateOf(0) }
    var showDeviceDialog by remember { mutableStateOf(false) }
    var rpm by remember { mutableStateOf(0) }
    var speed by remember { mutableStateOf(0) }
    var coolantTemp by remember { mutableStateOf(0) }
    var throttle by remember { mutableStateOf(0f) }

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
                0 -> DashboardScreen(rpm, speed, coolantTemp, throttle)
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
fun DashboardScreen(rpm: Int, speed: Int, coolantTemp: Int, throttle: Float) {
    Column(
        modifier = Modifier
            .fillMaxSize()
            .background(Color(0xFF1E1E1E))
            .padding(16.dp)
    ) {
        // Metrics Grid
        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.SpaceEvenly
        ) {
            MetricCard("RPM", rpm.toString(), "RPM", Color(0xFF00BCD4))
            MetricCard("Speed", speed.toString(), "km/h", Color(0xFF4CAF50))
        }

        Spacer(Modifier.height(16.dp))

        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.SpaceEvenly
        ) {
            MetricCard("Coolant", coolantTemp.toString(), "°C", Color(0xFFFF9800))
            MetricCard("Throttle", String.format("%.1f", throttle), "%", Color(0xFF9C27B0))
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
            onClick = { /* Run test */ }
        )

        Spacer(Modifier.height(16.dp))

        ActuatorTestCard(
            "Catalytic Converter Test",
            "Tests catalytic converter efficiency",
            onClick = { /* Run test */ }
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
