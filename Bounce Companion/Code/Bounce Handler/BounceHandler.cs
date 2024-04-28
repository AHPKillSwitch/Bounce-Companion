using Bounce_Companion.Code.Bounce_Companion_Utility;
using Bounce_Companion.Code.Command_Handler;
using Bounce_Companion.Code.Object___Havok_Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bounce_Companion.Code.Bounce_Handler
{

    internal class BounceHandler
    {
        MainWindow main;
        Mem m;
        Utility utility;
        HavokHandler hav;
        CommandHandler commandHandler;
        Annoucements announcements;
        ReplaySystem replaySystem;
        ObjectHandler objectHandler;

        public int bounceCount = 0;
        public int countReset = 0;
        public bool postBounceFilter = false;
        bool Bouncetimer;
        float p_Vel_Prev_Record = -999999;
        float p_Z_Prev_Record = 999999;
        public bool HoochCheck;
        public int hoochCount = 0;
        public bool slantTimer;
        float prev_P_X_Vel;
        float prev_P_Y_Vel;
        float prev_P_Z_Vel;
        public bool freeStyleMode = false;
        
        public bool debug = false;

        public BounceHandler(Mem m, Utility utility, HavokHandler hav, CommandHandler commandHandler, MainWindow main, ReplaySystem replaySystem, ObjectHandler objectHandler)
        {
            this.m = m;
            this.utility = utility;
            this.commandHandler = commandHandler;
            this.main = main;
            this.replaySystem = replaySystem;
            GetLocationData();
            this.objectHandler = objectHandler;
        }

        public Task BounceChecker()
        {
            try
            {
                int tickrate = m.ReadByte("halo2.exe+0x004C06E4,0x2");
                int p_Index = m.ReadInt16("halo2.exe+0x4E8F4C");
                int obj_List_Address = m.ReadInt32("halo2.exe+0x4E461C,0x44");
                int obj_List_Memory_Address = m.ReadInt32(obj_List_Address + p_Index * 0xC + 0x8);

                float p_X = utility.ReadPlayerFloat(obj_List_Memory_Address, 0xC * 0x4);
                float p_Y = utility.ReadPlayerFloat(obj_List_Memory_Address, 0xD * 0x4);
                float p_Z = utility.ReadPlayerFloat(obj_List_Memory_Address, 0xE * 0x4);
                float p_X_Vel = utility.ReadPlayerFloat(obj_List_Memory_Address, 0x22 * 0x4);
                float p_Y_Vel = utility.ReadPlayerFloat(obj_List_Memory_Address, 0x23 * 0x4);
                float p_Z_Vel = utility.ReadPlayerFloat(obj_List_Memory_Address, 0x24 * 0x4);
                int p_Airbourne = utility.ReadPlayerByte(obj_List_Memory_Address, 0xD8 * 0x4);
                int hav_Index_Datum = m.ReadInt32(obj_List_Memory_Address + 0xB4);
                int Obj_Vehi_Index_Datum = m.ReadInt32(obj_List_Memory_Address + 0x14);
                string vehi_salt = Obj_Vehi_Index_Datum.ToString("X");
                if (vehi_salt != "FFFFFFFF")
                {
                    int hav_Salt = hav.GetHavokSaltFromObjectDatum(Obj_Vehi_Index_Datum.ToString("X"));
                    objectHandler.objectHavokAddress = hav.GetHavokAddressFromHavokSalt(hav_Salt.ToString("X"));
                }

                else objectHandler.objectHavokAddress = hav.GetHavokAddressFromHavokSalt(hav_Index_Datum.ToString("X"));
                replaySystem.RecordPlayerPosition(p_X, p_Y, p_Z, p_X_Vel, p_Y_Vel, p_Z_Vel);
                utility.UpdateUI(p_X_Vel, p_Y_Vel, p_Z_Vel, p_X, p_Y, p_Z, tickrate);
                _ = HandleBounces(p_X, p_Y, p_Z, p_X_Vel, p_Y_Vel, p_Z_Vel, p_Airbourne);
                if (freeStyleMode) commandHandler.FreestlyeMode(p_X_Vel, p_Y_Vel, p_Z_Vel);
                p_Vel_Prev_Record = p_Z_Vel;
                p_Z_Prev_Record = p_Z;
                prev_P_X_Vel = p_X_Vel;
                prev_P_Y_Vel = p_Y_Vel;
                prev_P_Z_Vel = p_Z_Vel;
            }
            catch
            {
                utility.PrintToConsole("Read Error in Bounce Checker");
            }
            return null;
        }
        private async Task HandleBounces(float p_X, float p_Y, float p_Z, float p_X_Vel, float p_Y_Vel, float p_Z_Vel, int p_Airbourne)
        {
            if (bounceCount > 0 && p_Airbourne == 0 && !debug)
            {
                bounceCount = 0;
                HoochCheck = false;
            }


            if (slantTimer && (Math.Abs(p_X_Vel) < 2.8 || Math.Abs(p_Y_Vel) < 2.8))
            {
                slantTimer = false;
            }

            if (Bouncetimer)
            {
                if (p_Z_Vel < 0)
                {
                    Bouncetimer = false;
                }
            }
            else
            {
                if ((6.0 <= p_Z_Vel && p_Z_Vel <= 18.0) || (18.0 <= p_Z_Vel && p_Z_Vel <= 30.0))
                {
                    string bouncetype = (p_Z_Vel <= 18.0) ? "standard" : "monster";
                    if (prev_P_Z_Vel < -2) await BounceDetected(p_X, p_Y, p_Z, p_X_Vel, p_Y_Vel, p_Z_Vel, bouncetype);
                }

                if (p_Z_Vel < 5)
                {
                    postBounceFilter = false;
                }
            }

            if (HoochCheck)
            {
                CheckHooch(p_X, p_Y);
            }
        }
        private async Task BounceDetected(float p_X, float p_Y, float p_Z, float p_X_Vel, float p_Y_Vel, float p_Z_Vel, string bouncetype)
        {
            bouncetype = SlantCheckBounceType(p_X_Vel, p_Y_Vel, bouncetype);
            utility.PrintBounceDetails(p_X, p_Y, p_Z, p_X_Vel, p_Y_Vel, p_Z_Vel, bouncetype);
            Bouncetimer = true;
            bounceCount++;
            HoochCheck = true;
            string locationAndType = LocationCheck(p_X, p_Y);
            string location = locationAndType.Split(':')[0];
            string bounceTypeRequirement = locationAndType.Split(':')[1];
            if (bounceTypeRequirement != "null")
            {
                float requiredvelocity = float.Parse(bounceTypeRequirement);
                if (p_Z_Vel! >= requiredvelocity) location = "null"; // Check if player velocity meets the required number, if not they didnt hit the bounce.
            }
            postBounceFilter = true;
            if (main.checkbox_DisableOverlay.IsChecked != true)
            {
                await announcements.Announce(bounceCount, location, bouncetype);
            }


        }
        private string SlantCheckBounceType(float p_X_Vel, float p_Y_Vel, string bouncetype)
        {
            if (!slantTimer)
            {
                if (Math.Abs(p_X_Vel) > 3.5 || Math.Abs(p_Y_Vel) > 3.5)
                {
                    bouncetype = "slant";
                    if (Math.Abs(p_X_Vel) > 16 || Math.Abs(p_Y_Vel) > 16)
                    {
                        bouncetype = "monsterslant";
                    }
                    slantTimer = true;
                }
            }

            return bouncetype;
        }
        public void CheckHooch(float p_X, float p_Y)
        {
            //string locationAndType = LocationCheck(p_X, p_Y);
            //string location = locationAndType.Split(':')[0];
            //string bounceType = locationAndType.Split(':')[1];

            //if (bounceType == "hooch")
            //{
            //    hoochCount++;
            //    if (hoochCount >= 13) // if in hooch for more then 2 seconds
            //    {
            //        _ = Announcements(bounceCount, location);
            //    }
            //}
        }
        public struct LocationData
        {
            public string locationName;
            public string bounceType;
            public string xMin;
            public string xMax;
            public string yMin;
            public string yMax;
            public string zMin;
            public string zMax;

            public LocationData(string locationName, string bounceType, string xMin, string xMax, string yMin, string yMax, string zMin, string zMax)
            {
                this.locationName = locationName;
                this.bounceType = bounceType;
                this.xMin = xMin;
                this.xMax = xMax;
                this.yMin = yMin;
                this.yMax = yMax;
                this.zMin = zMin;
                this.zMax = zMax;

            }
        }
        List<LocationData> LocationDataList = new List<LocationData>();
        public string LocationCheck(float x, float y)
        {
            for (var i = 0; i < LocationDataList.Count; i++)
            {
                float xMin = float.Parse(LocationDataList[i].xMin);
                float xMax = float.Parse(LocationDataList[i].xMax);
                float yMin = float.Parse(LocationDataList[i].yMin);
                float yMax = float.Parse(LocationDataList[i].yMax);
                string locationName = LocationDataList[i].locationName;
                string bounceType = LocationDataList[i].bounceType;
                if (xMin <= x && x <= xMax && yMin <= y && y <= yMax)
                {
                    return locationName + ":" + bounceType;
                }
            }

            return "null:null";
        }

        public void GetLocationData()
        {
            try
            {
                List<string> result = commandHandler.PullCommands("https://pastebin.com/JY9FMDLX");


                foreach (string line in result)
                {
                    string locationNametobefixed = line.Split(':')[0];
                    string locationName = locationNametobefixed.Replace("/", "");
                    string bounceType = line.Split(':')[1];
                    string xMin = line.Split(':')[2];
                    string xMax = line.Split(':')[3];
                    string yMin = line.Split(':')[4];
                    string yMax = line.Split(':')[5];
                    string zMin = line.Split(':')[6];
                    string zMax = line.Split(':')[7];
                    string pVel = line.Split(':')[8];
                    LocationData LocationData = new LocationData(locationName, bounceType, xMin, xMax, yMin, yMax, zMin, zMax);
                    LocationDataList.Add(LocationData);
                }
            }
            catch { }
        }

    }
}
