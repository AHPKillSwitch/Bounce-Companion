using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static Bounce_Companion.MainWindow;

namespace Bounce_Companion
{
    public class Mem
    {
        private static Process _process;

        // Constants for process access rights
        private const int PROCESS_VM_READ = 0x0010;
        private const int PROCESS_VM_WRITE = 0x0020;
        private const int PROCESS_VM_OPERATION = 0x0008;
        private IntPtr processHandle = 0;
        public void OpenProcessHandle(Process process)
        {
            _process = process;
            processHandle = process.Handle;
        }



        // Read memory from the target process
        public bool ReadMemory(IntPtr address, byte[] buffer, int bytesToRead, out int bytesRead)
        {
            return ReadProcessMemory(processHandle, address, buffer, bytesToRead, out bytesRead);
        }

        // Write memory to the target process
        public bool WriteMemory(IntPtr address, byte[] buffer, int bytesToWrite, out int bytesWritten)
        {
            return WriteProcessMemory(processHandle, address, buffer, bytesToWrite, out bytesWritten);
        }

        // Read an integer from memory
        public int ReadInt32(object string_address)
        {
            IntPtr address = GetIntPtrAddress(string_address);

            byte[] buffer = new byte[4];
            ReadMemory(address, buffer, buffer.Length, out _);
            return BitConverter.ToInt32(buffer, 0);
        }
        public int ReadInt16(object string_address)
        {
            IntPtr address = GetIntPtrAddress(string_address);

            byte[] buffer = new byte[2];
            ReadMemory(address, buffer, buffer.Length, out _);
            return BitConverter.ToInt16(buffer, 0);
        }
        public int ReadInt8(object string_address)
        {
            IntPtr address = GetIntPtrAddress(string_address);

            byte[] buffer = new byte[1];
            ReadMemory(address, buffer, buffer.Length, out _);
            return BitConverter.ToInt32(buffer, 0);
        }

        // Read a float from memory
        public float ReadFloat(object string_address)
        {
            IntPtr address = GetIntPtrAddress(string_address);

            byte[] buffer = new byte[4];
            ReadMemory(address, buffer, buffer.Length, out _);
            return BitConverter.ToSingle(buffer, 0);
        }

        // Read a string from memory (ASCII)
        public string ReadString(object string_address, int maxLength = 256)
        {
            IntPtr address = GetIntPtrAddress(string_address);

            byte[] buffer = new byte[maxLength];
            ReadMemory(address, buffer, buffer.Length, out int bytesRead);

            // Find the index of the first null character (if any)
            int nullTerminatorIndex = Array.IndexOf(buffer, (byte)0);

            // If no null terminator is found, use the maximum length of bytes read
            if (nullTerminatorIndex == -1)
            {
                nullTerminatorIndex = bytesRead;
            }

            // Decode the byte array up to the null terminator (or maximum bytes read)
            string result = Encoding.ASCII.GetString(buffer, 0, nullTerminatorIndex);

            // Remove any trailing whitespace
            result = result.Trim();

            return result;
        }

        // Read a Unicode string from memory
        public string ReadUnicodeString(object string_address, int maxLength = 256)
        {
            IntPtr address = GetIntPtrAddress(string_address);

            byte[] buffer = new byte[maxLength * 2];
            ReadMemory(address, buffer, buffer.Length, out int bytesRead);
            return Encoding.Unicode.GetString(buffer, 0, bytesRead);
        }

        // Write an integer to memory
        public bool WriteInt32(object string_address, int value)
        {
            IntPtr address = GetIntPtrAddress(string_address);

            byte[] buffer = BitConverter.GetBytes(value);
            return WriteMemory(address, buffer, buffer.Length, out _);
        }

        // Write a float to memory
        public bool WriteFloat(object string_address, float value)
        {
            IntPtr address = GetIntPtrAddress(string_address);

            byte[] buffer = BitConverter.GetBytes(value);
            return WriteMemory(address, buffer, buffer.Length, out _);
        }

        // Write a string to memory (ASCII)
        public bool WriteString(object string_address, string value)
        {
            IntPtr address = GetIntPtrAddress(string_address);

            byte[] buffer = Encoding.ASCII.GetBytes(value);
            return WriteMemory(address, buffer, buffer.Length, out _);
        }

        // Write a Unicode string to memory
        public bool WriteUnicodeString(object string_address, string value)
        {
            IntPtr address = GetIntPtrAddress(string_address);

            byte[] buffer = Encoding.Unicode.GetBytes(value);
            return WriteMemory(address, buffer, buffer.Length, out _);
        }

        public byte ReadByte(object string_address)
        {
            IntPtr address = GetIntPtrAddress(string_address);

            byte[] buffer = new byte[1];
            ReadMemory(address, buffer, buffer.Length, out _);
            return buffer[0];
        }

        public byte[] ReadBytes(object string_address, int length)
        {
            IntPtr address = GetIntPtrAddress(string_address);

            byte[] buffer = new byte[length];
            ReadMemory(address, buffer, buffer.Length, out _);
            return buffer;
        }

        public bool WriteByte(object string_address, byte value)
        {
            IntPtr address = GetIntPtrAddress(string_address);
            byte[] buffer = { value };
            return WriteMemory(address, buffer, buffer.Length, out _);
        }
        public bool WriteBytes(string string_address, byte[] values)
        {
            IntPtr address = GetIntPtrAddress(string_address);

            return WriteMemory(address, values, values.Length, out _);
        }
        public bool WriteToMemory(object stringAddress, string dataType, string value)
        {
            IntPtr address = GetIntPtrAddress(stringAddress);
            byte[] buffer;

            switch (dataType.ToLower())
            {
                case "byte":
                    buffer = new byte[] { Convert.ToByte(value, 16) };
                    break;
                case "bytes":
                    string[] byteStrings = value.Split(' ');
                    buffer = new byte[byteStrings.Length];
                    for (int i = 0; i < byteStrings.Length; i++)
                    {
                        if (!string.IsNullOrEmpty(byteStrings[i]))
                            buffer[i] = Convert.ToByte(byteStrings[i], 16);
                    }
                    break;
                case "int32":
                    buffer = BitConverter.GetBytes(Convert.ToInt32(value));
                    break;
                case "int":
                    buffer = BitConverter.GetBytes(Convert.ToInt32(value));
                    break;
                case "int16":
                    buffer = BitConverter.GetBytes(Convert.ToInt16(value));
                    break;
                case "float":
                    buffer = BitConverter.GetBytes(Convert.ToSingle(value));
                    break;
                case "float32":
                    buffer = BitConverter.GetBytes(Convert.ToSingle(value));
                    break;
                case "string":
                    buffer = Encoding.ASCII.GetBytes(value);
                    break;
                case "unicodestring":
                    buffer = Encoding.Unicode.GetBytes(value);
                    break;
                default:
                    throw new ArgumentException("Invalid data type specified");
            }

            return WriteMemory(address, buffer, buffer.Length, out _);
        }
        public IntPtr GetIntPtrAddress(object address)
        {
            IntPtr ptrAddress;
            if (address is string stringAddress)
            {
                if (stringAddress.Contains('+'))
                {
                    return ptrAddress = GetIntPtrFromAddress(address.ToString());
                }
                return 0;
            }
            else if (address is int intAddress)
            {
                return ptrAddress = new IntPtr(intAddress);
            }
            else if (address is IntPtr intptrAddress)
            {
                return ptrAddress = intptrAddress;
            }
            else
            {
                return 0;
            }
        }


        public IntPtr GetIntPtrFromAddress(string address)
        {
            // Split the address string by commas and plus sign
            string[] parts = address.Split(',', '+');

            if (parts.Length < 2)
                return IntPtr.Zero; // Invalid format

            // Parse base address offset
            string baseString = parts[1].StartsWith("0x") ? parts[1].Substring(2) : parts[1];
            if (!int.TryParse(baseString, System.Globalization.NumberStyles.HexNumber, null, out int baseAddressOffset))
                return IntPtr.Zero; // Invalid base address offset format

            // Calculate base address
            IntPtr baseAddress = _process.MainModule.BaseAddress;
            IntPtr currentAddress = (IntPtr)(baseAddress.ToInt64() + baseAddressOffset);


            // Handle offsets
            if (parts.Length > 2)
            {
                for (int i = 2; i < parts.Length; i++)
                {
                    // Parse offset
                    string offsetString = parts[i].StartsWith("0x") ? parts[i].Substring(2) : parts[i];
                    if (!int.TryParse(offsetString, System.Globalization.NumberStyles.HexNumber, null, out int offset))
                        return IntPtr.Zero;

                    // Read pointer at current address
                    int pointerValue = ReadInt32(currentAddress);
                    if (pointerValue == 0)
                        return IntPtr.Zero; // Failed to read pointer

                    // Calculate next address
                    currentAddress = (IntPtr)(pointerValue + offset);
                }
            }

            return currentAddress;
        }


        // Import the necessary functions from kernel32.dll
        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, out int lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int nSize, out int lpNumberOfBytesWritten);

        // Import CloseHandle function from kernel32.dll
        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr hObject);
    }
}
