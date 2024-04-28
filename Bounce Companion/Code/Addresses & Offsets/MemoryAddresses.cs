using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bounce_Companion.Code.Addresses___Offsets
{
    internal class MemoryAddresses
    {
        public static string OffsetAddress(string address, string offset)
        {
            string[] parts = address.Split('+');
            if (parts.Length != 2)
            {
                Console.WriteLine("Invalid base address format");
                return "invalid";
            }
            string moduleName = parts[0];
            string hexAddressStr = parts[1];

            if (!int.TryParse(hexAddressStr, System.Globalization.NumberStyles.HexNumber, null, out int baseAddress))
            {
                Console.WriteLine("Invalid hex address format");
                return "invalid";
            }
            // Calculate the new address
            int newAddress = baseAddress + int.Parse(offset.Replace(",", ""), NumberStyles.HexNumber);

            // Convert the new address back to string
            string newAddressStr = moduleName + "+" + newAddress.ToString("X");
            return newAddressStr;
        }
        public static class Addresses
        {
            public static string tickRate = "halo2.exe+0x004C06E4,0x02";
            public static string player_Obj_Index = "halo2.exe+4E7C88";
        }
        public static class Offsets
        {
            //mapsHeader
            public static string scnr_Name = ",1A4";
            //g_cache_file_globals
            public static int tag_index_absolute_mapping = 0x14;
            public static int absolute_index_tag_mapping = 0x18;
            public static int tag_loaded_count = 0x1C;
            public static int tag_total_count = 0x20;
            public static int tag_instances = 0x10;
        }
    }
}
