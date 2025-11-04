using System;
using System.Collections.Generic;

namespace OBDReader.Core.Models
{
    /// <summary>
    /// Diagnostic Trouble Code (DTC) representation and decoder
    /// </summary>
    public class DiagnosticTroubleCode
    {
        public string Code { get; set; }
        public string Description { get; set; }
        public DTCType Type { get; set; }
        public DTCStatus Status { get; set; }
        public byte[] RawData { get; set; }

        /// <summary>
        /// DTC Type enumeration
        /// </summary>
        public enum DTCType
        {
            Powertrain,      // P codes
            Chassis,         // C codes
            Body,            // B codes
            Network          // U codes
        }

        /// <summary>
        /// DTC Status
        /// </summary>
        public enum DTCStatus
        {
            Confirmed,       // Mode 03 - Confirmed codes
            Pending,         // Mode 07 - Pending codes
            Permanent        // Mode 0A - Permanent codes
        }

        /// <summary>
        /// Decode DTC from raw bytes
        /// </summary>
        public static DiagnosticTroubleCode DecodeDTC(byte firstByte, byte secondByte, DTCStatus status = DTCStatus.Confirmed)
        {
            // Extract type bits (first 2 bits of first byte)
            int typeBits = (firstByte >> 6) & 0x03;
            DTCType type = typeBits switch
            {
                0x00 => DTCType.Powertrain,
                0x01 => DTCType.Chassis,
                0x02 => DTCType.Body,
                0x03 => DTCType.Network,
                _ => DTCType.Powertrain
            };

            // Extract first character digit (next 2 bits)
            int firstDigit = (firstByte >> 4) & 0x03;

            // Extract remaining digits
            int secondDigit = firstByte & 0x0F;
            int thirdDigit = (secondByte >> 4) & 0x0F;
            int fourthDigit = secondByte & 0x0F;

            // Build code string
            char typeChar = type switch
            {
                DTCType.Powertrain => 'P',
                DTCType.Chassis => 'C',
                DTCType.Body => 'B',
                DTCType.Network => 'U',
                _ => 'P'
            };

            string code = $"{typeChar}{firstDigit}{secondDigit:X}{thirdDigit:X}{fourthDigit:X}";

            return new DiagnosticTroubleCode
            {
                Code = code,
                Type = type,
                Status = status,
                RawData = new byte[] { firstByte, secondByte },
                Description = GetDTCDescription(code)
            };
        }

        /// <summary>
        /// Parse multiple DTCs from response data
        /// </summary>
        public static List<DiagnosticTroubleCode> ParseDTCs(byte[] data, DTCStatus status = DTCStatus.Confirmed)
        {
            var codes = new List<DiagnosticTroubleCode>();

            // DTCs come in pairs of bytes
            for (int i = 0; i < data.Length - 1; i += 2)
            {
                // Skip padding (0x00 0x00)
                if (data[i] == 0x00 && data[i + 1] == 0x00)
                    continue;

                var dtc = DecodeDTC(data[i], data[i + 1], status);
                codes.Add(dtc);
            }

            return codes;
        }

        /// <summary>
        /// Get DTC description (comprehensive database for common codes)
        /// </summary>
        public static string GetDTCDescription(string code)
        {
            // Comprehensive DTC database (common codes for 2008 GMC Yukon Denali)
            var descriptions = new Dictionary<string, string>
            {
                // P0xxx - Generic Powertrain Codes
                ["P0000"] = "No fault detected",
                ["P0010"] = "Camshaft Position Actuator Circuit (Bank 1)",
                ["P0011"] = "Camshaft Position Timing Over-Advanced (Bank 1)",
                ["P0013"] = "Exhaust Camshaft Position Actuator Circuit (Bank 1)",
                ["P0014"] = "Exhaust Camshaft Position Timing Over-Advanced (Bank 1)",
                ["P0016"] = "Crankshaft/Camshaft Correlation (Bank 1 Sensor A)",
                ["P0017"] = "Crankshaft/Camshaft Correlation (Bank 1 Sensor B)",
                ["P0030"] = "HO2S Heater Control Circuit (Bank 1 Sensor 1)",
                ["P0036"] = "HO2S Heater Control Circuit (Bank 1 Sensor 2)",
                ["P0068"] = "MAP/MAF - Throttle Position Correlation",
                ["P0087"] = "Fuel Rail/System Pressure - Too Low",
                ["P0088"] = "Fuel Rail/System Pressure - Too High",
                ["P0089"] = "Fuel Pressure Regulator Performance",

                ["P0100"] = "Mass or Volume Air Flow Circuit Malfunction",
                ["P0101"] = "Mass or Volume Air Flow Circuit Range/Performance Problem",
                ["P0102"] = "Mass or Volume Air Flow Circuit Low Input",
                ["P0103"] = "Mass or Volume Air Flow Circuit High Input",
                ["P0106"] = "Manifold Absolute Pressure/Barometric Pressure Circuit Range/Performance Problem",
                ["P0107"] = "Manifold Absolute Pressure/Barometric Pressure Circuit Low Input",
                ["P0108"] = "Manifold Absolute Pressure/Barometric Pressure Circuit High Input",
                ["P0112"] = "Intake Air Temperature Circuit Low Input",
                ["P0113"] = "Intake Air Temperature Circuit High Input",
                ["P0116"] = "Engine Coolant Temperature Circuit Range/Performance Problem",
                ["P0117"] = "Engine Coolant Temperature Circuit Low Input",
                ["P0118"] = "Engine Coolant Temperature Circuit High Input",
                ["P0121"] = "Throttle/Pedal Position Sensor/Switch A Circuit Range/Performance Problem",
                ["P0122"] = "Throttle/Pedal Position Sensor/Switch A Circuit Low Input",
                ["P0123"] = "Throttle/Pedal Position Sensor/Switch A Circuit High Input",
                ["P0128"] = "Coolant Thermostat (Coolant Temperature Below Thermostat Regulating Temperature)",
                ["P0130"] = "O2 Sensor Circuit Malfunction (Bank 1 Sensor 1)",
                ["P0131"] = "O2 Sensor Circuit Low Voltage (Bank 1 Sensor 1)",
                ["P0132"] = "O2 Sensor Circuit High Voltage (Bank 1 Sensor 1)",
                ["P0133"] = "O2 Sensor Circuit Slow Response (Bank 1 Sensor 1)",
                ["P0134"] = "O2 Sensor Circuit No Activity Detected (Bank 1 Sensor 1)",
                ["P0135"] = "O2 Sensor Heater Circuit Malfunction (Bank 1 Sensor 1)",
                ["P0137"] = "O2 Sensor Circuit Low Voltage (Bank 1 Sensor 2)",
                ["P0138"] = "O2 Sensor Circuit High Voltage (Bank 1 Sensor 2)",
                ["P0140"] = "O2 Sensor Circuit No Activity Detected (Bank 1 Sensor 2)",
                ["P0141"] = "O2 Sensor Heater Circuit Malfunction (Bank 1 Sensor 2)",
                ["P0150"] = "O2 Sensor Circuit Malfunction (Bank 2 Sensor 1)",
                ["P0151"] = "O2 Sensor Circuit Low Voltage (Bank 2 Sensor 1)",
                ["P0152"] = "O2 Sensor Circuit High Voltage (Bank 2 Sensor 1)",
                ["P0153"] = "O2 Sensor Circuit Slow Response (Bank 2 Sensor 1)",
                ["P0154"] = "O2 Sensor Circuit No Activity Detected (Bank 2 Sensor 1)",
                ["P0155"] = "O2 Sensor Heater Circuit Malfunction (Bank 2 Sensor 1)",
                ["P0157"] = "O2 Sensor Circuit Low Voltage (Bank 2 Sensor 2)",
                ["P0158"] = "O2 Sensor Circuit High Voltage (Bank 2 Sensor 2)",
                ["P0160"] = "O2 Sensor Circuit No Activity Detected (Bank 2 Sensor 2)",
                ["P0161"] = "O2 Sensor Heater Circuit Malfunction (Bank 2 Sensor 2)",
                ["P0171"] = "System Too Lean (Bank 1)",
                ["P0172"] = "System Too Rich (Bank 1)",
                ["P0174"] = "System Too Lean (Bank 2)",
                ["P0175"] = "System Too Rich (Bank 2)",

                ["P0200"] = "Injector Circuit Malfunction",
                ["P0201"] = "Injector Circuit Malfunction - Cylinder 1",
                ["P0202"] = "Injector Circuit Malfunction - Cylinder 2",
                ["P0203"] = "Injector Circuit Malfunction - Cylinder 3",
                ["P0204"] = "Injector Circuit Malfunction - Cylinder 4",
                ["P0205"] = "Injector Circuit Malfunction - Cylinder 5",
                ["P0206"] = "Injector Circuit Malfunction - Cylinder 6",
                ["P0207"] = "Injector Circuit Malfunction - Cylinder 7",
                ["P0208"] = "Injector Circuit Malfunction - Cylinder 8",
                ["P0220"] = "Throttle/Pedal Position Sensor/Switch B Circuit Malfunction",
                ["P0300"] = "Random/Multiple Cylinder Misfire Detected",
                ["P0301"] = "Cylinder 1 Misfire Detected",
                ["P0302"] = "Cylinder 2 Misfire Detected",
                ["P0303"] = "Cylinder 3 Misfire Detected",
                ["P0304"] = "Cylinder 4 Misfire Detected",
                ["P0305"] = "Cylinder 5 Misfire Detected",
                ["P0306"] = "Cylinder 6 Misfire Detected",
                ["P0307"] = "Cylinder 7 Misfire Detected",
                ["P0308"] = "Cylinder 8 Misfire Detected",
                ["P0315"] = "Crankshaft Position System Variation Not Learned",
                ["P0325"] = "Knock Sensor 1 Circuit Malfunction (Bank 1 or Single Sensor)",
                ["P0327"] = "Knock Sensor 1 Circuit Low Input (Bank 1 or Single Sensor)",
                ["P0328"] = "Knock Sensor 1 Circuit High Input (Bank 1 or Single Sensor)",
                ["P0335"] = "Crankshaft Position Sensor A Circuit Malfunction",
                ["P0336"] = "Crankshaft Position Sensor A Circuit Range/Performance",
                ["P0340"] = "Camshaft Position Sensor Circuit Malfunction",
                ["P0341"] = "Camshaft Position Sensor Circuit Range/Performance",
                ["P0351"] = "Ignition Coil A Primary/Secondary Circuit Malfunction",
                ["P0352"] = "Ignition Coil B Primary/Secondary Circuit Malfunction",
                ["P0353"] = "Ignition Coil C Primary/Secondary Circuit Malfunction",
                ["P0354"] = "Ignition Coil D Primary/Secondary Circuit Malfunction",
                ["P0355"] = "Ignition Coil E Primary/Secondary Circuit Malfunction",
                ["P0356"] = "Ignition Coil F Primary/Secondary Circuit Malfunction",
                ["P0357"] = "Ignition Coil G Primary/Secondary Circuit Malfunction",
                ["P0358"] = "Ignition Coil H Primary/Secondary Circuit Malfunction",

                ["P0401"] = "Exhaust Gas Recirculation Flow Insufficient Detected",
                ["P0402"] = "Exhaust Gas Recirculation Flow Excessive Detected",
                ["P0403"] = "Exhaust Gas Recirculation Circuit Malfunction",
                ["P0404"] = "Exhaust Gas Recirculation Circuit Range/Performance",
                ["P0405"] = "Exhaust Gas Recirculation Sensor A Circuit Low",
                ["P0420"] = "Catalyst System Efficiency Below Threshold (Bank 1)",
                ["P0430"] = "Catalyst System Efficiency Below Threshold (Bank 2)",
                ["P0442"] = "Evaporative Emission Control System Leak Detected (small leak)",
                ["P0443"] = "Evaporative Emission Control System Purge Control Valve Circuit Malfunction",
                ["P0446"] = "Evaporative Emission Control System Vent Control Circuit Malfunction",
                ["P0449"] = "Evaporative Emission Control System Vent Valve/Solenoid Circuit Malfunction",
                ["P0455"] = "Evaporative Emission Control System Leak Detected (gross leak)",
                ["P0461"] = "Fuel Level Sensor Circuit Range/Performance",
                ["P0462"] = "Fuel Level Sensor Circuit Low Input",
                ["P0463"] = "Fuel Level Sensor Circuit High Input",
                ["P0500"] = "Vehicle Speed Sensor Malfunction",
                ["P0506"] = "Idle Control System RPM Lower Than Expected",
                ["P0507"] = "Idle Control System RPM Higher Than Expected",

                ["P0601"] = "Internal Control Module Memory Check Sum Error",
                ["P0602"] = "Control Module Programming Error",
                ["P0603"] = "Internal Control Module Keep Alive Memory (KAM) Error",
                ["P0604"] = "Internal Control Module Random Access Memory (RAM) Error",
                ["P0605"] = "Internal Control Module Read Only Memory (ROM) Error",
                ["P0606"] = "PCM Processor Fault",
                ["P0700"] = "Transmission Control System Malfunction",
                ["P0701"] = "Transmission Control System Range/Performance",
                ["P0705"] = "Transmission Range Sensor Circuit Malfunction (PRNDL Input)",
                ["P0711"] = "Transmission Fluid Temperature Sensor Circuit Range/Performance",
                ["P0712"] = "Transmission Fluid Temperature Sensor Circuit Low Input",
                ["P0713"] = "Transmission Fluid Temperature Sensor Circuit High Input",
                ["P0716"] = "Input/Turbine Speed Sensor Circuit Range/Performance",
                ["P0717"] = "Input/Turbine Speed Sensor Circuit No Signal",
                ["P0720"] = "Output Speed Sensor Circuit Malfunction",
                ["P0741"] = "Torque Converter Clutch Circuit Performance or Stuck Off",
                ["P0742"] = "Torque Converter Clutch Circuit Stuck On",
                ["P0748"] = "Pressure Control Solenoid Electrical",
                ["P0751"] = "Shift Solenoid A Performance or Stuck Off",
                ["P0756"] = "Shift Solenoid B Performance or Stuck Off",

                // U codes - Network Communication
                ["U0001"] = "High Speed CAN Communication Bus",
                ["U0100"] = "Lost Communication with ECM/PCM",
                ["U0101"] = "Lost Communication with TCM",
                ["U0151"] = "Lost Communication with ABS Control Module",

                // C codes - Chassis
                ["C0035"] = "Left Front Wheel Speed Sensor Circuit",
                ["C0040"] = "Right Front Wheel Speed Sensor Circuit",
                ["C0045"] = "Left Rear Wheel Speed Sensor Circuit",
                ["C0050"] = "Right Rear Wheel Speed Sensor Circuit"
            };

            return descriptions.ContainsKey(code) ? descriptions[code] : "Unknown DTC - Please refer to service manual";
        }

        public override string ToString()
        {
            return $"{Code}: {Description}";
        }
    }
}
