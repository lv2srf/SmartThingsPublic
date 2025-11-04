using System;
using System.Collections.Generic;

namespace OBDReader.Core.Models
{
    /// <summary>
    /// OBD-II Parameter IDs (PIDs) definitions
    /// Comprehensive list of standard and GM-specific PIDs for 2008 GMC Yukon Denali
    /// </summary>
    public static class OBDPIDs
    {
        /// <summary>
        /// PID definition with formula and unit
        /// </summary>
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

        /// <summary>
        /// Mode 01 - Current Data PIDs
        /// </summary>
        public static readonly Dictionary<byte, PIDDefinition> Mode01PIDs = new Dictionary<byte, PIDDefinition>
        {
            // Standard PIDs
            [0x00] = new PIDDefinition
            {
                PID = 0x00,
                Name = "PIDs Supported [01-20]",
                Description = "Bitmap of supported PIDs 01-20",
                Unit = "",
                ByteCount = 4,
                Category = "System"
            },

            [0x01] = new PIDDefinition
            {
                PID = 0x01,
                Name = "Monitor Status",
                Description = "MIL status and supported monitors",
                Unit = "",
                ByteCount = 4,
                Category = "System"
            },

            [0x03] = new PIDDefinition
            {
                PID = 0x03,
                Name = "Fuel System Status",
                Description = "Current fuel system status",
                Unit = "",
                ByteCount = 2,
                Category = "Fuel"
            },

            [0x04] = new PIDDefinition
            {
                PID = 0x04,
                Name = "Engine Load",
                Description = "Calculated engine load value",
                Unit = "%",
                ByteCount = 1,
                Formula = data => (data[0] * 100.0 / 255.0),
                MinValue = 0,
                MaxValue = 100,
                Category = "Engine"
            },

            [0x05] = new PIDDefinition
            {
                PID = 0x05,
                Name = "Coolant Temperature",
                Description = "Engine coolant temperature",
                Unit = "°C",
                ByteCount = 1,
                Formula = data => (data[0] - 40),
                MinValue = -40,
                MaxValue = 215,
                Category = "Temperature"
            },

            [0x06] = new PIDDefinition
            {
                PID = 0x06,
                Name = "Short Fuel Trim Bank 1",
                Description = "Short term fuel trim - Bank 1",
                Unit = "%",
                ByteCount = 1,
                Formula = data => ((data[0] - 128) * 100.0 / 128.0),
                MinValue = -100,
                MaxValue = 99.2,
                Category = "Fuel"
            },

            [0x07] = new PIDDefinition
            {
                PID = 0x07,
                Name = "Long Fuel Trim Bank 1",
                Description = "Long term fuel trim - Bank 1",
                Unit = "%",
                ByteCount = 1,
                Formula = data => ((data[0] - 128) * 100.0 / 128.0),
                MinValue = -100,
                MaxValue = 99.2,
                Category = "Fuel"
            },

            [0x08] = new PIDDefinition
            {
                PID = 0x08,
                Name = "Short Fuel Trim Bank 2",
                Description = "Short term fuel trim - Bank 2",
                Unit = "%",
                ByteCount = 1,
                Formula = data => ((data[0] - 128) * 100.0 / 128.0),
                MinValue = -100,
                MaxValue = 99.2,
                Category = "Fuel"
            },

            [0x09] = new PIDDefinition
            {
                PID = 0x09,
                Name = "Long Fuel Trim Bank 2",
                Description = "Long term fuel trim - Bank 2",
                Unit = "%",
                ByteCount = 1,
                Formula = data => ((data[0] - 128) * 100.0 / 128.0),
                MinValue = -100,
                MaxValue = 99.2,
                Category = "Fuel"
            },

            [0x0A] = new PIDDefinition
            {
                PID = 0x0A,
                Name = "Fuel Pressure",
                Description = "Fuel rail pressure (gauge)",
                Unit = "kPa",
                ByteCount = 1,
                Formula = data => (data[0] * 3),
                MinValue = 0,
                MaxValue = 765,
                Category = "Fuel"
            },

            [0x0B] = new PIDDefinition
            {
                PID = 0x0B,
                Name = "Intake MAP",
                Description = "Intake manifold absolute pressure",
                Unit = "kPa",
                ByteCount = 1,
                Formula = data => data[0],
                MinValue = 0,
                MaxValue = 255,
                Category = "Engine"
            },

            [0x0C] = new PIDDefinition
            {
                PID = 0x0C,
                Name = "Engine RPM",
                Description = "Engine revolutions per minute",
                Unit = "RPM",
                ByteCount = 2,
                Formula = data => ((data[0] * 256 + data[1]) / 4.0),
                MinValue = 0,
                MaxValue = 16383.75,
                Category = "Engine"
            },

            [0x0D] = new PIDDefinition
            {
                PID = 0x0D,
                Name = "Vehicle Speed",
                Description = "Vehicle speed",
                Unit = "km/h",
                ByteCount = 1,
                Formula = data => data[0],
                MinValue = 0,
                MaxValue = 255,
                Category = "Vehicle"
            },

            [0x0E] = new PIDDefinition
            {
                PID = 0x0E,
                Name = "Timing Advance",
                Description = "Timing advance before TDC",
                Unit = "°",
                ByteCount = 1,
                Formula = data => ((data[0] / 2.0) - 64),
                MinValue = -64,
                MaxValue = 63.5,
                Category = "Engine"
            },

            [0x0F] = new PIDDefinition
            {
                PID = 0x0F,
                Name = "Intake Air Temp",
                Description = "Intake air temperature",
                Unit = "°C",
                ByteCount = 1,
                Formula = data => (data[0] - 40),
                MinValue = -40,
                MaxValue = 215,
                Category = "Temperature"
            },

            [0x10] = new PIDDefinition
            {
                PID = 0x10,
                Name = "MAF Air Flow",
                Description = "Mass air flow sensor rate",
                Unit = "g/s",
                ByteCount = 2,
                Formula = data => ((data[0] * 256 + data[1]) / 100.0),
                MinValue = 0,
                MaxValue = 655.35,
                Category = "Engine"
            },

            [0x11] = new PIDDefinition
            {
                PID = 0x11,
                Name = "Throttle Position",
                Description = "Absolute throttle position",
                Unit = "%",
                ByteCount = 1,
                Formula = data => (data[0] * 100.0 / 255.0),
                MinValue = 0,
                MaxValue = 100,
                Category = "Engine"
            },

            [0x13] = new PIDDefinition
            {
                PID = 0x13,
                Name = "O2 Sensors Present",
                Description = "Oxygen sensors present",
                Unit = "",
                ByteCount = 1,
                Category = "Sensors"
            },

            [0x14] = new PIDDefinition
            {
                PID = 0x14,
                Name = "O2 Bank 1 Sensor 1",
                Description = "Oxygen sensor voltage and fuel trim - Bank 1, Sensor 1",
                Unit = "V",
                ByteCount = 2,
                Formula = data => (data[0] / 200.0),
                MinValue = 0,
                MaxValue = 1.275,
                Category = "Sensors"
            },

            [0x15] = new PIDDefinition
            {
                PID = 0x15,
                Name = "O2 Bank 1 Sensor 2",
                Description = "Oxygen sensor voltage and fuel trim - Bank 1, Sensor 2",
                Unit = "V",
                ByteCount = 2,
                Formula = data => (data[0] / 200.0),
                MinValue = 0,
                MaxValue = 1.275,
                Category = "Sensors"
            },

            [0x1C] = new PIDDefinition
            {
                PID = 0x1C,
                Name = "OBD Standard",
                Description = "OBD standards this vehicle conforms to",
                Unit = "",
                ByteCount = 1,
                Category = "System"
            },

            [0x1F] = new PIDDefinition
            {
                PID = 0x1F,
                Name = "Runtime Since Start",
                Description = "Run time since engine start",
                Unit = "s",
                ByteCount = 2,
                Formula = data => (data[0] * 256 + data[1]),
                MinValue = 0,
                MaxValue = 65535,
                Category = "Engine"
            },

            [0x21] = new PIDDefinition
            {
                PID = 0x21,
                Name = "Distance w/ MIL",
                Description = "Distance traveled with MIL on",
                Unit = "km",
                ByteCount = 2,
                Formula = data => (data[0] * 256 + data[1]),
                MinValue = 0,
                MaxValue = 65535,
                Category = "System"
            },

            [0x23] = new PIDDefinition
            {
                PID = 0x23,
                Name = "Fuel Rail Pressure",
                Description = "Fuel rail gauge pressure (diesel or gasoline direct injection)",
                Unit = "kPa",
                ByteCount = 2,
                Formula = data => ((data[0] * 256 + data[1]) * 10),
                MinValue = 0,
                MaxValue = 655350,
                Category = "Fuel"
            },

            [0x2F] = new PIDDefinition
            {
                PID = 0x2F,
                Name = "Fuel Level",
                Description = "Fuel tank level input",
                Unit = "%",
                ByteCount = 1,
                Formula = data => (data[0] * 100.0 / 255.0),
                MinValue = 0,
                MaxValue = 100,
                Category = "Fuel"
            },

            [0x31] = new PIDDefinition
            {
                PID = 0x31,
                Name = "Distance Since Clear",
                Description = "Distance traveled since codes cleared",
                Unit = "km",
                ByteCount = 2,
                Formula = data => (data[0] * 256 + data[1]),
                MinValue = 0,
                MaxValue = 65535,
                Category = "System"
            },

            [0x33] = new PIDDefinition
            {
                PID = 0x33,
                Name = "Barometric Pressure",
                Description = "Absolute barometric pressure",
                Unit = "kPa",
                ByteCount = 1,
                Formula = data => data[0],
                MinValue = 0,
                MaxValue = 255,
                Category = "Environment"
            },

            [0x42] = new PIDDefinition
            {
                PID = 0x42,
                Name = "Control Module Voltage",
                Description = "Control module voltage",
                Unit = "V",
                ByteCount = 2,
                Formula = data => ((data[0] * 256 + data[1]) / 1000.0),
                MinValue = 0,
                MaxValue = 65.535,
                Category = "System"
            },

            [0x43] = new PIDDefinition
            {
                PID = 0x43,
                Name = "Absolute Load Value",
                Description = "Absolute load value",
                Unit = "%",
                ByteCount = 2,
                Formula = data => ((data[0] * 256 + data[1]) * 100.0 / 255.0),
                MinValue = 0,
                MaxValue = 25700,
                Category = "Engine"
            },

            [0x45] = new PIDDefinition
            {
                PID = 0x45,
                Name = "Relative Throttle",
                Description = "Relative throttle position",
                Unit = "%",
                ByteCount = 1,
                Formula = data => (data[0] * 100.0 / 255.0),
                MinValue = 0,
                MaxValue = 100,
                Category = "Engine"
            },

            [0x46] = new PIDDefinition
            {
                PID = 0x46,
                Name = "Ambient Air Temp",
                Description = "Ambient air temperature",
                Unit = "°C",
                ByteCount = 1,
                Formula = data => (data[0] - 40),
                MinValue = -40,
                MaxValue = 215,
                Category = "Temperature"
            },

            [0x49] = new PIDDefinition
            {
                PID = 0x49,
                Name = "Accelerator Position D",
                Description = "Accelerator pedal position D",
                Unit = "%",
                ByteCount = 1,
                Formula = data => (data[0] * 100.0 / 255.0),
                MinValue = 0,
                MaxValue = 100,
                Category = "Engine"
            },

            [0x4C] = new PIDDefinition
            {
                PID = 0x4C,
                Name = "Commanded Throttle",
                Description = "Commanded throttle actuator",
                Unit = "%",
                ByteCount = 1,
                Formula = data => (data[0] * 100.0 / 255.0),
                MinValue = 0,
                MaxValue = 100,
                Category = "Engine"
            },

            [0x51] = new PIDDefinition
            {
                PID = 0x51,
                Name = "Fuel Type",
                Description = "Fuel type coding",
                Unit = "",
                ByteCount = 1,
                Category = "Fuel"
            },

            [0x5C] = new PIDDefinition
            {
                PID = 0x5C,
                Name = "Engine Oil Temp",
                Description = "Engine oil temperature",
                Unit = "°C",
                ByteCount = 1,
                Formula = data => (data[0] - 40),
                MinValue = -40,
                MaxValue = 215,
                Category = "Temperature"
            }
        };

        /// <summary>
        /// Get PID definition by mode and PID
        /// </summary>
        public static PIDDefinition GetPID(byte mode, byte pid)
        {
            return mode switch
            {
                0x01 => Mode01PIDs.ContainsKey(pid) ? Mode01PIDs[pid] : null,
                _ => null
            };
        }

        /// <summary>
        /// Calculate value from response data
        /// </summary>
        public static double CalculateValue(byte mode, byte pid, byte[] data)
        {
            var definition = GetPID(mode, pid);
            if (definition?.Formula != null && data != null && data.Length >= definition.ByteCount)
            {
                return definition.Formula(data);
            }
            return 0;
        }
    }
}
