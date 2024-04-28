using Bounce_Companion.Code.Bounce_Companion_Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static Bounce_Companion.ControllerKeyBinds;

namespace Bounce_Companion.Code.Object___Havok_Helpers
{
    internal class ObjectHandler
    {
        Mem m;
        Utility utility;
        public Dictionary<string, p_List_Info> p_List = new Dictionary<string, p_List_Info>();
        public int obj_List_Address = 0;
        public int objectHavokAddress = 0;
        public ObjectHandler(Mem M, Utility utility) 
        { 
            m = M;
            this.utility = utility;
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
        public string GetPlayerNameSalt(string playerName)
        {
            UpdatePlayerList();
            foreach (KeyValuePair<string, p_List_Info> key in p_List)
            {
                p_List_Info info = key.Value;
                if (playerName == info.p_Name)
                {
                    return info.p_Salt;
                }
            }
            return "null";
        }//List<p_List_Info> p_List = new List<p_List_Info>();
        
        int check = 0;
        public Task UpdatePlayerList()
        {
            for (int i = 0; i < 15; i++)
            {
                string player_Info = PopulatePlayersList(i); // return Name:Salt
                if (player_Info.Split(':')[1] != "0" && player_Info.Split(':')[0] != "" && player_Info.Split(':')[1] != "FFFFFFFF") // if player name isnt null and player salt isnt null
                {
                    if (!p_List.ContainsKey(player_Info.Split(':')[0])) // check if dict doesnt contain key before adding
                    {
                        p_List.Add(player_Info.Split(':')[0], new p_List_Info(player_Info.Split(':')[0], player_Info.Split(':')[1], i));
                    }
                }
            }
            for (int p = 0; p < 15; p++)
            {
                string player_Info = PopulatePlayersList(p);
                if (p_List.ContainsKey(player_Info.Split(':')[0])) // check if dict has player stored
                {
                    p_List_Info p_info = p_List[player_Info.Split(':')[0]];
                    if (player_Info.Split(':')[1] != p_info.p_Salt) // check if salt has changeg
                    {
                        if (player_Info.Split(':')[1] == "FFFFFFFF") // has player died or left the game
                        {
                            //SetContrails(player_Info.Split(':')[0], p_info.p_number);
                            utility.PrintToConsole("Player Died");
                            check = 0;
                            p_info.p_Salt = player_Info.Split(':')[1];
                        }
                        else
                        {
                            p_info.p_Salt = player_Info.Split(':')[1];
                        }

                    }
                }
            }
            check = 0;
            return null;
        }

        public string PopulatePlayersList(int i)
        {
            int playersAddress = m.ReadInt32("halo2.exe+47CD48");
            playersAddress += 0x17D8 + 0x74;

            int salt = m.ReadInt32((playersAddress + (0x204 * i)));
            byte[] nameBytes = m.ReadBytes((playersAddress + (0x204 * i) + 0x18), 32);
            int count = 0;
            for (int x = 0; x < nameBytes.Length; x++)
            {
                if (nameBytes[x] == 00) count++;
                else count = 0;
                if (count >= 2)
                {
                    nameBytes[x] = 0x20;
                }
            }
            string name = System.Text.Encoding.Unicode.GetString(nameBytes);
            name = StripUnicodeCharactersFromString(name);
            string saltToString = salt.ToString("X");
            return name + ":" + saltToString;
        }
        public static String StripUnicodeCharactersFromString(string inputValue)
        {
            return Regex.Replace(inputValue, @"[^\u0000-\u007F]", String.Empty);
        }
        public struct p_List_Info
        {
            public string p_Name;
            public string p_Salt;
            public int p_number;

            public p_List_Info(string p_Name, string p_Salt, int p_number)
            {
                this.p_Name = p_Name;
                this.p_Salt = p_Salt;
                this.p_number = p_number;
            }
        }
    }
}
