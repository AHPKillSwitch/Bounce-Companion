using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bounce_Companion.Code.Object___Havok_Helpers
{
    internal class ObjectHandler
    {
        Mem m;
        public int obj_List_Address = 0;
        public ObjectHandler(Mem M) 
        { 
            m = M;
        }
        public void ReadObjectXYZ(string p_Salt, out int obj_List_Memory_Address, out float p_X, out float p_Y, out float p_Z, out float p_X_Vel, out float p_Y_Vel, out float p_Z_Vel, out float p_Yaw, out float p_pitch, out float p_Shields)
        {

            obj_List_Address = m.ReadInt32("halo2.exe+4E461C,0x44");
            int p_Index = int.Parse(p_Salt.Remove(0, 4), System.Globalization.NumberStyles.HexNumber);

            obj_List_Memory_Address = m.ReadInt32(obj_List_Address + p_Index * 0xC + 0x8);
            p_X = m.ReadFloat(obj_List_Memory_Address + 0x64);
            p_Y = m.ReadFloat(obj_List_Memory_Address + 0x68);
            p_Z = m.ReadFloat(obj_List_Memory_Address + 0x6C);
            p_X_Vel = m.ReadFloat(obj_List_Memory_Address + 0x22 * 0x4);
            p_Y_Vel = m.ReadFloat(obj_List_Memory_Address + 0x23 * 0x4);
            p_Z_Vel = m.ReadFloat(obj_List_Memory_Address + 0x24 * 0x4);
            p_Yaw = m.ReadFloat(obj_List_Memory_Address + 0x170 * 0x4);
            p_pitch = m.ReadFloat(obj_List_Memory_Address + 0x15C * 0x4);
            p_Shields = m.ReadFloat(obj_List_Memory_Address + 0xF0 * 0x4);
        }
    }
}
