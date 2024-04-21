﻿using ABI.Windows.Foundation;
using Haruka.Arcade.SEGA835Lib.Debugging;
using Haruka.Arcade.SEGA835Lib.Devices;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;

namespace Haruka.Arcade.SEGA835Lib.Serial {
    public class SProtSerial : SerialComm {

        public const byte SYNC_BYTE = 0xE0;
        private const byte ESCAPE_BYTE = 0xD0;

        public SProtSerial(int port_no, int baudrate = 115200, int timeout = 1000, bool dtr = false, bool rts = false, Parity parity = Parity.None, int dataBits = 8, StopBits stopBits = StopBits.One, Handshake flowControl = Handshake.None) : base(port_no, baudrate, timeout, dtr, rts, parity, dataBits, stopBits, flowControl) {
        }

        public override DeviceStatus Read(int len, out byte[] data) {
            if (LOG_RW) {
                Log.Write("SProtSerial Port " + Port + ", Read Len=" + len);
            }
            int pos = 0;
            List<byte> bytes = new List<byte>();
            data = null;
            int checksum = 0;
            bool escape_flag = false;
            DeviceStatus ret = DeviceStatus.OK;
            while (pos < len) {
                ret = base.ReadByte(out byte b);
                if (ret != DeviceStatus.OK) {
                    return ret;
                }
                if (pos == 0 && b != SYNC_BYTE) {
                    Log.WriteError("SProtSerial Read failed, expected sync byte, got " + b);
                    return DeviceStatus.ERR_CHECKSUM;
                }
                if (b == ESCAPE_BYTE) {
                    escape_flag = true;
                } else {
                    if (escape_flag) {
                        bytes.Add((byte)(b + 1));
                        checksum += 1;
                        escape_flag = false;
                    } else {
                        bytes.Add(b);
                    }
                    pos++;
                }
                if (pos > 1 && pos < len && !escape_flag) { // don't add sync and checksum byte
                    checksum += b;
                }
            }

            data = bytes.ToArray();

            if (DUMP_BYTES) {
                Log.Dump(data, "SProtSerial Read:");
            }

            checksum %= 0x100;
            byte data_checksum = data[data.Length - 1];
            if (checksum != data_checksum) {
                Log.WriteError("SProtSerial Read failed, checksum mismatch, expected " + data_checksum + ", got " + checksum);
                ret = DeviceStatus.ERR_CHECKSUM;
            }

            return ret;
        }


        public DeviceStatus ReadLenByOffset(int lenByteOffset, out byte[] data, bool lenIncludesSelf = false, bool lenIncludesChecksumByte = false) {
            if (LOG_RW) {
                Log.Write("SProtSerial Port " + Port + ", Read Len By Offset=" + lenByteOffset);
            }
            int pos = 0;
            int? len = null;
            List<byte> bytes = new List<byte>();
            data = null;
            int checksum = 0;
            bool escape_flag = false;
            DeviceStatus ret = DeviceStatus.OK;
            while (len == null) {
                ret = base.ReadByte(out byte b);
                if (ret != DeviceStatus.OK) {
                    return ret;
                }
                if (pos == 0 && b != SYNC_BYTE) {
                    Log.WriteError("SProtSerial ReadLenByOffset failed, expected sync byte, got " + b);
                    return DeviceStatus.ERR_CHECKSUM;
                }
                if (b == ESCAPE_BYTE) {
                    escape_flag = true;
                } else {
                    if (escape_flag) {
                        bytes.Add((byte)(b + 1));
                        checksum += 1;
                        escape_flag = false;
                    } else {
                        bytes.Add(b);
                    }
                    if (pos++ == lenByteOffset) {
                        len = b; // checksum byte
                    }
                }
                if (pos > 1 && !escape_flag) { // don't add sync byte
                    checksum += b;
                }
            }
            pos = lenIncludesSelf ? 1 : 0;
            len += lenIncludesChecksumByte ? 0 : 1;
            if (LOG_RW) {
                Log.Write("SProtSerial Port " + Port + ", Read Len Remaining=" + (len - pos));
            }
            if (len - pos < 0) {
                throw new ArgumentException("Bytes to read from stream are negative (len: " + len + ", pos: " + pos + ")");
            }
            while (pos < len) {
                ret = base.ReadByte(out byte b);
                if (ret != DeviceStatus.OK) {
                    return ret;
                }
                if (b == ESCAPE_BYTE) {
                    escape_flag = true;
                } else {
                    if (escape_flag) {
                        bytes.Add((byte)(b + 1));
                        checksum += 1;
                        escape_flag = false;
                    } else {
                        bytes.Add(b);
                    }
                    pos++;
                }
                if (pos < len && !escape_flag) { // don't add sync and checksum byte
                    checksum += b;
                }
            }

            checksum %= 0x100;

            data = bytes.ToArray();
            if (DUMP_BYTES) {
                Log.Dump(data, "SProtSerial Read:");
            }

            byte data_checksum = data[data.Length - 1];
            if (checksum != data_checksum) {
                Log.WriteError("SProtSerial ReadLenByOffset failed, checksum mismatch, expected " + data_checksum + ", got " + checksum);
                ret = DeviceStatus.ERR_CHECKSUM;
            }

            return ret;
        }

        public override DeviceStatus Write(byte[] data) {
            List<byte> bytes = new List<byte>();
            bytes.Add(SYNC_BYTE);
            int checksum = 0;
            foreach (byte b in data) {
                if (b == ESCAPE_BYTE || b == SYNC_BYTE) {
                    bytes.Add(ESCAPE_BYTE);
                    bytes.Add((byte)(b - 1));
                } else {
                    bytes.Add(b);
                }
                checksum += b;
            }
            bytes.Add((byte)(checksum % 0x100));
            byte[] encoded = bytes.ToArray();
            if (DUMP_BYTES) {
                Log.Dump(encoded, "SProtSerial Write:");
            }
            return base.Write(encoded);
        }

        public DeviceStatus WriteAndRead(byte[] send, int recv_len, out byte[] recv) {
            DeviceStatus ret = Write(send);
            if (ret != DeviceStatus.OK) {
                recv = null;
                return ret;
            }
            return Read(recv_len, out recv);
        }

        public DeviceStatus WriteAndReadByOffset(byte[] send, int recv_len_offset, out byte[] recv) {
            DeviceStatus ret = Write(send);
            if (ret != DeviceStatus.OK) {
                recv = null;
                return ret;
            }
            return ReadLenByOffset(recv_len_offset, out recv);
        }
    }
}