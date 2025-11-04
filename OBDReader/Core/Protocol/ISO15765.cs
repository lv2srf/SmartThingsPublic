using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OBDReader.Core.Protocol
{
    /// <summary>
    /// ISO 15765-4 (CAN bus) Protocol Implementation
    /// Handles CAN message framing, flow control, and multi-frame messages
    /// </summary>
    public class ISO15765
    {
        // CAN Protocol Control Information
        private const byte SINGLE_FRAME = 0x00;
        private const byte FIRST_FRAME = 0x10;
        private const byte CONSECUTIVE_FRAME = 0x20;
        private const byte FLOW_CONTROL = 0x30;

        // Flow Control Status
        private const byte FC_CONTINUE_TO_SEND = 0x00;
        private const byte FC_WAIT = 0x01;
        private const byte FC_OVERFLOW = 0x02;

        // Default timing parameters (milliseconds)
        public int P2_TIMEOUT = 50;      // Default response timeout
        public int P2_STAR_TIMEOUT = 5000; // Extended response timeout
        public int SEPARATION_TIME = 0;   // Minimum time between consecutive frames

        /// <summary>
        /// Frame type enumeration
        /// </summary>
        public enum FrameType
        {
            SingleFrame,
            FirstFrame,
            ConsecutiveFrame,
            FlowControl
        }

        /// <summary>
        /// Parse a CAN message and extract the frame type and data
        /// </summary>
        public static (FrameType frameType, int dataLength, byte[] data) ParseCANFrame(byte[] frame)
        {
            if (frame == null || frame.Length == 0)
                throw new ArgumentException("Invalid frame data");

            byte pci = frame[0];
            byte frameTypeBits = (byte)((pci & 0xF0) >> 4);

            FrameType frameType;
            int dataLength = 0;
            byte[] data = null;

            switch (frameTypeBits)
            {
                case 0x0: // Single Frame
                    frameType = FrameType.SingleFrame;
                    dataLength = pci & 0x0F;
                    data = frame.Skip(1).Take(dataLength).ToArray();
                    break;

                case 0x1: // First Frame
                    frameType = FrameType.FirstFrame;
                    dataLength = ((pci & 0x0F) << 8) | frame[1];
                    data = frame.Skip(2).ToArray();
                    break;

                case 0x2: // Consecutive Frame
                    frameType = FrameType.ConsecutiveFrame;
                    int sequenceNumber = pci & 0x0F;
                    data = frame.Skip(1).ToArray();
                    break;

                case 0x3: // Flow Control
                    frameType = FrameType.FlowControl;
                    byte flowStatus = (byte)(pci & 0x0F);
                    data = frame.Skip(1).ToArray();
                    break;

                default:
                    throw new InvalidOperationException($"Unknown frame type: {frameTypeBits}");
            }

            return (frameType, dataLength, data);
        }

        /// <summary>
        /// Build a single frame CAN message (up to 7 bytes of data)
        /// </summary>
        public static byte[] BuildSingleFrame(byte[] data)
        {
            if (data.Length > 7)
                throw new ArgumentException("Single frame can contain maximum 7 bytes");

            byte[] frame = new byte[8];
            frame[0] = (byte)(SINGLE_FRAME | data.Length);
            Array.Copy(data, 0, frame, 1, data.Length);

            // Pad with 0xAA (or 0x00, depends on ECU preference)
            for (int i = data.Length + 1; i < 8; i++)
                frame[i] = 0xAA;

            return frame;
        }

        /// <summary>
        /// Build first frame for multi-frame message
        /// </summary>
        public static byte[] BuildFirstFrame(byte[] data, int totalLength)
        {
            byte[] frame = new byte[8];
            frame[0] = (byte)(FIRST_FRAME | ((totalLength >> 8) & 0x0F));
            frame[1] = (byte)(totalLength & 0xFF);
            Array.Copy(data, 0, frame, 2, Math.Min(6, data.Length));
            return frame;
        }

        /// <summary>
        /// Build consecutive frame
        /// </summary>
        public static byte[] BuildConsecutiveFrame(byte[] data, int sequenceNumber)
        {
            byte[] frame = new byte[8];
            frame[0] = (byte)(CONSECUTIVE_FRAME | (sequenceNumber & 0x0F));
            Array.Copy(data, 0, frame, 1, Math.Min(7, data.Length));

            // Pad if necessary
            for (int i = data.Length + 1; i < 8; i++)
                frame[i] = 0xAA;

            return frame;
        }

        /// <summary>
        /// Build flow control frame
        /// </summary>
        public static byte[] BuildFlowControlFrame(byte flowStatus, byte blockSize, byte separationTime)
        {
            byte[] frame = new byte[8];
            frame[0] = (byte)(FLOW_CONTROL | (flowStatus & 0x0F));
            frame[1] = blockSize;
            frame[2] = separationTime;

            // Pad with 0xAA
            for (int i = 3; i < 8; i++)
                frame[i] = 0xAA;

            return frame;
        }

        /// <summary>
        /// Segment data into multiple CAN frames
        /// </summary>
        public static List<byte[]> SegmentData(byte[] data)
        {
            var frames = new List<byte[]>();

            // If data fits in single frame (7 bytes or less)
            if (data.Length <= 7)
            {
                frames.Add(BuildSingleFrame(data));
                return frames;
            }

            // Multi-frame transmission
            // First frame contains 6 bytes
            frames.Add(BuildFirstFrame(data.Take(6).ToArray(), data.Length));

            // Consecutive frames contain 7 bytes each
            int offset = 6;
            int sequenceNumber = 1;

            while (offset < data.Length)
            {
                int chunkSize = Math.Min(7, data.Length - offset);
                byte[] chunk = data.Skip(offset).Take(chunkSize).ToArray();
                frames.Add(BuildConsecutiveFrame(chunk, sequenceNumber));

                sequenceNumber = (sequenceNumber + 1) & 0x0F; // Wrap at 15
                offset += chunkSize;
            }

            return frames;
        }

        /// <summary>
        /// Reassemble multi-frame message
        /// </summary>
        public static byte[] ReassembleFrames(List<byte[]> frames)
        {
            if (frames.Count == 0)
                throw new ArgumentException("No frames to reassemble");

            var (frameType, dataLength, firstData) = ParseCANFrame(frames[0]);

            if (frameType == FrameType.SingleFrame)
            {
                return firstData;
            }

            if (frameType != FrameType.FirstFrame)
                throw new InvalidOperationException("First frame must be FirstFrame type");

            var completeData = new List<byte>(firstData);
            int expectedLength = dataLength;

            for (int i = 1; i < frames.Count; i++)
            {
                var (cfType, cfLen, cfData) = ParseCANFrame(frames[i]);

                if (cfType != FrameType.ConsecutiveFrame)
                    throw new InvalidOperationException($"Expected ConsecutiveFrame at position {i}");

                completeData.AddRange(cfData);
            }

            // Trim to expected length
            return completeData.Take(expectedLength).ToArray();
        }

        /// <summary>
        /// Calculate CAN ID for diagnostic communication
        /// </summary>
        public static class CANID
        {
            // Standard 11-bit CAN IDs for OBD-II
            public const uint FUNCTIONAL_REQUEST = 0x7DF;  // Broadcast to all ECUs
            public const uint PHYSICAL_REQUEST_ENGINE = 0x7E0;  // Engine ECU
            public const uint PHYSICAL_RESPONSE_ENGINE = 0x7E8; // Engine ECU response

            public const uint PHYSICAL_REQUEST_TRANSMISSION = 0x7E1; // Transmission ECU
            public const uint PHYSICAL_RESPONSE_TRANSMISSION = 0x7E9; // Transmission ECU response

            // Extended 29-bit CAN IDs (if needed)
            public const uint EXTENDED_FUNCTIONAL = 0x18DB33F1;
            public const uint EXTENDED_PHYSICAL = 0x18DA00F1;

            /// <summary>
            /// Get response ID for a given request ID
            /// </summary>
            public static uint GetResponseID(uint requestID)
            {
                // For standard 11-bit IDs, response is request + 8
                if (requestID >= 0x7E0 && requestID <= 0x7E7)
                    return requestID + 8;

                // Default to engine response
                return PHYSICAL_RESPONSE_ENGINE;
            }
        }
    }
}
