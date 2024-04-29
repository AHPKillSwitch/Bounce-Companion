using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bounce_Companion.Code.Object___Havok_Helpers
{
    public class HavokHandler
    {
        Mem m;
        public HavokHandler(Mem M) 
        {
            m = M;
        }
        public int GetHavokAddressFromHavokSalt(string salt)
        {
            if (salt == "0") return 0;
            int p_Index = int.Parse(salt.Remove(0, 4), System.Globalization.NumberStyles.HexNumber);
            int hav_List_Address = m.ReadInt32("halo2.exe+004D83B8,0x44");
            int hav_List_Memory_Address = m.ReadInt32((hav_List_Address + (p_Index * 0xA0) + 0x70));

            hav_List_Memory_Address += 0x40;
            hav_List_Memory_Address = m.ReadInt32(hav_List_Memory_Address);
            hav_List_Memory_Address += 0x14;
            hav_List_Memory_Address = m.ReadInt32(hav_List_Memory_Address);
            return hav_List_Memory_Address += 0x10;
        }
        public int GetHavokSaltFromObjectDatum(string obj_Salt)
        {
            int p_Index = int.Parse(obj_Salt.Remove(0, 4), System.Globalization.NumberStyles.HexNumber);
            int obj_List_Address = m.ReadInt32("halo2.exe+4E461C,0x44");
            int offset = p_Index * 0xC + 0x8;
            obj_List_Address += offset;
            int obj_List_Memory_Address = m.ReadInt32((obj_List_Address));
            obj_List_Memory_Address += 0xb4;
            int hav_Salt = m.ReadInt32((obj_List_Memory_Address));

            return hav_Salt;
        }
        public void WriteObjectVelocity(int address, float X_Vel, float Y_Vel, float Z_Vel)
        {
            m.WriteToMemory((address - 0x8), "float", X_Vel.ToString());
            m.WriteToMemory((address - 0x4), "float", Y_Vel.ToString());
            m.WriteToMemory(address, "float", Z_Vel.ToString());
        }
    }
}
