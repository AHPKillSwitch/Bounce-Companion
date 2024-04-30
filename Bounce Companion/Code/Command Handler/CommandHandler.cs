using Bounce_Companion.Code.Addresses___Offsets;
using Bounce_Companion.Code.Bounce_Companion_Utility;
using Bounce_Companion.Code.Bounce_Handler;
using Bounce_Companion.Code.Camera_Tool;
using Bounce_Companion.Code.Object___Havok_Helpers;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Runtime.Remoting;
using System.Text;
using System.Threading.Tasks;
using static Bounce_Companion.MainWindow;

namespace Bounce_Companion.Code.Command_Handler
{
    public class CommandHandler
    {
        MainWindow main;
        Mem m;

        public List<Command> Commands = new List<Command>();

        private bool FreezeVelocity = true;
        private float freezeValue = 0;
        private bool bindPitch = false;
        public CommandHandler(MainWindow main) 
        {

            this.main = main;
            this.m = main.m; 
        }
        public struct Command
        {
            //  /weap.objects\weapons\rifle\assault_rifle.[Barrels].[Firing_effect].Prediction Type=None|
            public string TagType;
            public string TagName;
            public string[] Args;
            public string Method;
            public string value;
        }
        public List<string> PullCommands(string url)
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.3");
            var html = httpClient.GetStringAsync(url).Result;

            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(html);
            var commandboxHtml = htmlDocument.DocumentNode.Descendants("div")
                .Where(node => node.GetAttributeValue("class", "")
                .Contains("source")).ToList();

            var commandlineList = commandboxHtml[0].Descendants("div")
                .Where(node => node.GetAttributeValue("class", "")
                .Contains("de1")).ToList();
            string[] STRList = commandlineList.Select(s => main.Utility.CleanString(s.InnerHtml)).ToArray();
            STRList = STRList.Where(s => s != "").ToArray();
            httpClient.Dispose();
            return STRList.ToList();
        }
        public async Task<bool> GetCommands()
        {
            string url = System.Text.Encoding.Unicode.GetString(m.ReadBytes("halo2.exe+97777C", 127)).Split('\0').FirstOrDefault(part => !string.IsNullOrEmpty(part));

            string consoleinput = "";
            bool link = (!url.Contains(":"));
            if (url is null)
            {
                //ModsEnabled = false;
                //Main.SetPokingStatusLabel(ModsEnabled);
                //Main.PrintToConsole("-------------------- Mods Disabled --------------------" + "\n");
                //Main.PrintToConsole("Make sure the game type description contains TSI:PBLink" + "\n");
            }

            else if (url.Contains("BC:"))
            {
                List<string> result = new List<string>();
                Globals.modsEnabled = true;
                try
                {
                    string pbinurl1 = "https://pastebin.com/" + url.Split(':')[1];
                    result = PullCommands(pbinurl1);
                    if (result.Count == 0)
                    {
                        return true;
                    }
                    //else PrintToConsole("GameType Mods Found. Applying... ");
                    foreach (string command in result)
                    {
                        try
                        {
                            GetCommandsFromString(consoleinput, command);
                        }
                        catch
                        {
                            main.Utility.PrintToConsole("Failed to apply command. " + command);
                        }
                    }
                    return true;
                }
                catch
                {
                    foreach (string line in System.IO.File.ReadLines("Content/Commands/" + url.Split(':')[1] + ".txt"))
                    {
                        string command = main.Utility.CleanString(line);
                        result.Add(command);
                    }
                    foreach (string command in result)
                    {
                        if (command.Contains("//"))
                        {
                            await ApplyGameMod(command, true);
                        }
                        else
                        {
                            GetCommandsFromString("", command);
                            main.Utility.PrintToConsole(main.rm.outPutStrings);
                            main.rm.outPutStrings.Clear();
                        }
                    }
                    foreach (string command in result)
                    {
                        GetCommandsFromString(consoleinput, command);
                    }
                }
                return false;
            }
            return true;
            //else //LabPoking.Content = "Disabled";

        }
        public async void ApplyMods(bool on, string modName)
        {
            string leftBttn = "N/A"; ;
            string rightBttn = "N/A";
            string keyBindBttn = string.Empty;
            string enableModsTextOutput = "";
            string disableModsTextOutput = "";
            int delayTime = 0;

            List<string> result = new List<string>();
            foreach (string line in System.IO.File.ReadLines("Content/Commands/" + modName + ".txt"))
            {
                string command = main.Utility.CleanString(line);
                result.Add(command);
            }
            foreach (string command in result)
            {
                if (command.Contains("EnableModTextOutPut:"))
                {
                    enableModsTextOutput = command.Split(':')[1];
                }
                else if (command.Contains("DisableModTextOutPut:"))
                {
                    disableModsTextOutput = command.Split(':')[1];
                }
                else if (!on)
                {
                    if (command.Contains("Disable:"))
                    {
                        if (command.Contains("//"))
                        {
                            delayTime = await ApplyGameMod(command.Split("able:")[1], on);
                        }
                        else
                        {
                            GetCommandsFromString("", command.Split("able:")[1]);
                            main.Utility.PrintToConsole(main.rm.outPutStrings);
                            main.rm.outPutStrings.Clear();
                        }
                        main.Utility.PrintToConsole(disableModsTextOutput);
                    }
                }
                else
                {
                    if (command.Contains("Enable:"))
                    {
                        if (command.Contains("//"))
                        {
                            delayTime = await ApplyGameMod(command.Split(':')[1], on);
                        }
                        else
                        {
                            GetCommandsFromString("", command.Split("able:")[1]);
                            main.Utility.PrintToConsole(main.rm.outPutStrings);
                            main.rm.outPutStrings.Clear();
                        }
                        main.Utility.PrintToConsole(enableModsTextOutput);
                    }
                }
                await Task.Delay(delayTime);
                delayTime = 0;
            }

        }
        public void GetCommandsFromString(string consoleinput, string command)
        {
            Globals.modsEnabled = true;
            Command com = new Command();
            List<string> args = new List<string>();
            int split = 0;
            string consoleArgs = "";
            string strcommand = command;
            if (string.IsNullOrEmpty(consoleinput) && string.IsNullOrEmpty(command)) return;

            if (consoleinput != "")
            {
                strcommand = consoleinput;
            }
            if (strcommand.Contains("//"))
            {
                _ = ApplyGameMod(strcommand, true);
            }
            else if (Globals.modsEnabled && Globals.modsEnabledMaster)
            {
                for (var i = 0; i < strcommand.Length; i++)
                {
                    string tmp = strcommand.ElementAt(i).ToString();
                    if (tmp == ".")
                    {                                             //------| Check through command string for the first "."
                        string test = strcommand.Split('.')[split];
                        if (split == 0)
                        {
                            com.TagName = test.Replace("/", "");  //------| Setting the string as tagtype
                        }
                        else if (split == 1)
                        {
                            com.TagType = test.Replace("/", "");  //------| Check through command string for the second "." set this as tagname
                        }
                        else if (split == 2 && strcommand.Contains("["))
                        {
                            string tmpArg = test.Replace("/", "") + strcommand.Split('.')[split + 1];
                            //Get final square bracket
                            int bracketindex = tmpArg.LastIndexOf(']');
                            tmpArg = tmpArg.Substring(0, bracketindex + 1);
                            //Parse arguments

                            string arg = "";
                            for (var j = 0; j < tmpArg.Length; j++)
                            {
                                if (tmpArg.ElementAt(j) == ']')
                                {
                                    //end of argument
                                    arg += tmpArg.ElementAt(j);
                                    args.Add(arg);
                                    consoleArgs += arg;
                                    arg = "";
                                }
                                else
                                {
                                    arg += tmpArg.ElementAt(j);
                                }
                            }
                            com.Args = args.ToArray();
                        }
                        else if (split == 2 && !strcommand.Contains("["))
                        {
                            string tmpArg = test;
                            //Get final square bracket
                            //Parse arguments
                            args.Add(tmpArg);
                            com.Args = args.ToArray();
                        }
                        split += 1;
                    }
                    if (tmp == "=")
                    {
                        break;
                    }
                }


                com.Method = strcommand.Split('.')[split].Split('=')[0];
                string finalLine = strcommand.Split('=')[1].Split('\n')[0];
                com.value = finalLine;
                Commands.Add(com);
                var currentcommand = Commands[Commands.Count - 1];
                main.CE.c = currentcommand;
                main.CE.TagBlockProcessing(m);
                string output =
                    "Tag Type: " + "\t" + com.TagType + "\n" +
                    "Tag Name: " + "\t" + com.TagName + "\n" +
                    "Tag Blocks: " + "\t" + consoleArgs + "\n" +
                    "Edit Value: " + "\t" + com.Method + "\n" +
                    "New value: " + "\t" + com.value;


                main.rm.outPutStrings.Add(output);
                main.rm.outPutStrings.Add(strcommand);
            }

        }
        
        public async Task<int> ApplyGameMod(string command, bool on)
        {
            string fullCommand = string.Empty;
            string prefix = string.Empty;
            if (command.Contains(" ")) // Example Enable://WireFrame true
            {
                prefix = command.Split(' ')[1];
                string[] commandParts = command.Split(' ');

                fullCommand = commandParts.Length >= 3 ? commandParts[2] : "";
                command = command.Split(' ')[0];
                if (prefix.ToLower() == "true") on = true;
                if (prefix.ToLower() == "false") on = false;
            }
            switch (command.ToLower())
            {
                case "//addbounce":
                    {
                        main.BounceHandler.debug = true;
                        main.BounceHandler.bounceCount++;
                        main.Utility.PrintToConsole("+1 Added to bounce count.");
                        _ = main.Annoucements.Announce(main.BounceHandler.bounceCount, "null", "standard");
                        break;
                    }
                case "//clearbounce":
                    {
                        main.BounceHandler.debug = false;
                        main.BounceHandler.bounceCount = 0;
                        main.Utility.PrintToConsole("Bounce count reset.");
                        break;
                    }
                case "//wireframe":
                    {
                        if (on)
                        {
                            m.WriteToMemory("halo2.exe+468174", "Byte", "0x01");
                        }
                        else
                        {
                            m.WriteToMemory("halo2.exe+468174", "Byte", "0x00");
                        }
                        break;
                    }
                case "//savestate":
                    {
                        if (on)
                        {
                            m.WriteToMemory("halo2.exe+482250", "Byte", "0x01");
                        }
                        else
                        {
                            m.WriteToMemory("halo2.exe+48224F", "Byte", "0x01");
                        }
                        break;
                    }
                case "//savestatep2":
                    {
                        if (on)
                        {
                            if (main.player2Attched) main.mp2.WriteToMemory("halo2.exe+482250", "Byte", "0x01");
                        }
                        else
                        {
                            if (main.player2Attched) main.mp2.WriteToMemory("halo2.exe+48224F", "Byte", "0x01");
                        }
                        break;
                    }
                case "//nullcharfilter":
                    {
                        if (on)
                        {
                            m.WriteToMemory("halo2.exe+0030D3F8", "Bytes", "0x90 0x90");
                        }
                        else
                        {
                            m.WriteToMemory("halo2.exe+0030D3F8", "Bytes", "0x74 0x2F");
                        }
                        break;
                    }
                case "//customcontrails":
                    {
                        if (on)
                        {
                            main.customContrails = true;
                        }
                        else
                        {
                            main.customContrails = false;
                        }
                        break;
                    }
                case "//warpfix":
                    {
                        if (on)
                        {
                            m.WriteToMemory("halo2.exe+4F958E", "Bytes", "0x80 0x40 0x00 0x00 0x00 0x40 0x40 0x00 0x00 0x20 0x41");
                        }
                        else
                        {
                            m.WriteToMemory("halo2.exe+4F958E", "Bytes", "0x20 0x40 0x00 0x00 0x00 0x40 0x40 0x00 0x00 0xF0 0x40");
                        }
                        break;
                    }
                case "//capturescene":
                    {
                        main.CameraTool.CaptureScene();
                        break;
                    }
                case "//startcamera":
                    {
                        await main.CameraTool.StartCameraRoll();
                        break;
                    }
                case "//stopcamera":
                    {
                        main.CameraTool.rollCamera = false;
                        break;
                    }
                case "//jumptoscene":
                    {
                        int index = 0;
                        if (string.IsNullOrEmpty(prefix)) index = 0;
                        else index = int.Parse(prefix);
                        main.CameraTool.JumpCameraToScene(index);
                        break;
                    }
                case "//debugcamera":
                    {
                        main.CameraTool.ToggleDebugMode();
                        //isCameraToolOpen = true;
                        break;
                    }
                case "//cameracontrol":
                    {
                        if (on)
                        {
                            main.CameraControls.flyCamControl = true;
                        }
                        else
                        {
                            main.CameraControls.flyCamControl = false;
                        }
                        break;
                    }
                case "//9key":
                    {
                        keybd_event(VK_SPACE, 0, 0, (UIntPtr)0);
                        await Task.Delay(100);
                        keybd_event(VK_SPACE, 0, KEYEVENTF_KEYUP, (UIntPtr)0);
                        break;
                    }
                case "//delay":
                    {
                        int time = int.Parse(prefix);
                        return time;
                    }
                case "//lockvelocity":
                    {
                        if (prefix == "true")
                        {
                            FreezeVelocity = true;
                            freezeValue = m.ReadFloat((main.ObjectHandler.objectHavokAddress - 0x28));
                            Task.Run(() => StartAsyncFreezeVelocity());

                        }
                        else if (prefix == "false")
                        {
                            FreezeVelocity = false;
                            bindPitch = false;
                        }
                        else if (prefix == "pitch")
                        {
                            FreezeVelocity = true;
                            bindPitch = true;
                            freezeValue = m.ReadFloat((main.ObjectHandler.objectHavokAddress - 0x28));
                            Task.Run(() => StartAsyncFreezeVelocity());
                        }
                        break;
                    }
                case "//setvelocity":
                    {
                        float velocity = float.Parse(prefix);
                        m.WriteToMemory((main.ObjectHandler.objectHavokAddress - 0x28), "float", (0.6 * velocity).ToString());
                        await Task.Delay(222);
                        m.WriteToMemory((main.ObjectHandler.objectHavokAddress - 0x28), "float", velocity.ToString());
                        break;
                    }
                case "//addvelocity":
                    {
                        freezeValue = m.ReadFloat((main.ObjectHandler.objectHavokAddress - 0x28));
                        freezeValue += float.Parse(prefix);
                        m.WriteToMemory((main.ObjectHandler.objectHavokAddress - 0x28), "float", (0.6 * freezeValue).ToString());
                        await Task.Delay(222);
                        m.WriteToMemory((main.ObjectHandler.objectHavokAddress - 0x28), "float", freezeValue.ToString());
                        break;
                    }
                case "//autocrouch":
                    {
                        float p_z = m.ReadFloat((main.ObjectHandler.objectHavokAddress + 0x8));
                        m.WriteToMemory((main.ObjectHandler.objectHavokAddress + 0x8), "float", (p_z - 0.2).ToString());
                        break;
                    }
                case "//moveplayer_z":
                    {
                        float player_Z = float.Parse(fullCommand);
                        float p_z = m.ReadFloat((main.ObjectHandler.objectHavokAddress + 0x8));
                        if (prefix == "minus") m.WriteToMemory((main.ObjectHandler.objectHavokAddress + 0x8), "float", (p_z - player_Z).ToString());
                        if (prefix == "add") m.WriteToMemory((main.ObjectHandler.objectHavokAddress + 0x8), "float", (p_z + player_Z).ToString());
                        break;
                    }
                case "//freestylemode":
                    {
                        if (on) main.BounceHandler.freeStyleMode = true;
                        else main.BounceHandler.freeStyleMode = false;
                        break;
                    }
                case "//markposition"://MarkPosition Test Standard
                    {
                        string location = prefix.Split(':')[0];
                        string type = prefix.Split(':')[1];
                        string velocity = prefix.Split(':')[2];

                        MarkPlayerCoords(location, type, velocity);
                        break;
                    }
                case "//tp"://tp player KillSwitch:x:y:z or //tp player KillSwitch:location:location1fromjson
                    {
                        string arg = prefix.Split(" ")[0];
                        if (arg.ToLower() == "player")
                        {
                            string playerName = fullCommand.Split(":")[0];
                            float x = float.Parse(fullCommand.Split(":")[1]);
                            float y = float.Parse(fullCommand.Split(':')[2]);
                            float z = float.Parse(fullCommand.Split(':')[3]);

                            break;
                        }
                        else if (arg.ToLower() == "create")
                        {

                            break;
                        }
                        break;
                    }

            }
            return 0;
        }
        public void FreestlyeMode(float p_X_Vel, float p_Y_Vel, float p_Z_Vel)
        {
            float newValue = p_Z_Vel * 10;
            if (p_Z_Vel > 3.1)
            {
                GetCommandsFromString("/globals\\globals.matg.[Player Information:0].Airborne Acceleration=" + newValue.ToString(), "");
                GetCommandsFromString("/globals\\globals.matg.[Player Information:0].Run Forward=" + (p_Z_Vel * 0.4).ToString(), "");
            }
            else
            {
                GetCommandsFromString("/globals\\globals.matg.[Player Information:0].Airborne Acceleration=1.05", "");
                GetCommandsFromString("/globals\\globals.matg.[Player Information:0].Run Forward=2.25", "");
            }
        }
        private void MarkPlayerCoords(string location, string type, string Velocity)
        {
            try
            {
                int p_Index = m.ReadInt16(MemoryAddresses.Addresses.player_Obj_Index);
                IntPtr obj_List_Address = m.ReadInt32("halo2.exe+4E461C,0x44");

                //      p_index object memory address aka player
                IntPtr obj_List_Memory_Address = m.ReadInt32((obj_List_Address + p_Index * 0xC + 0x8)); //0xC = Size of struct      0x8 = offset to memory address in current struct

                float p_X = m.ReadFloat((obj_List_Memory_Address + 0xC * 0x4));
                float p_Y = m.ReadFloat((obj_List_Memory_Address + 0xD * 0x4));
                float p_Z = m.ReadFloat((obj_List_Memory_Address + 0xE * 0x4));

                string bouncelocation = "/" + location + ":" + type + ":" + (Math.Round(p_X - 0.4, 2)).ToString() + ":" + (Math.Round(p_X + 0.4, 2)).ToString() + ":" + (Math.Round(p_Y - 0.4, 2)).ToString() + ":" + (Math.Round(p_Y + 0.4, 2)).ToString() + ":0:0:0:" + Velocity;
                main.Utility.PrintToConsole(bouncelocation);
                main.Utility.WriteBouncePositionToString(bouncelocation);
            }
            catch { main.Utility.PrintToConsole("Error: Failed to get player position."); }
        }
        public async Task StartAsyncFreezeVelocity()
        {
            while (FreezeVelocity)
            {
                if (bindPitch)
                {
                    float pitch = m.ReadFloat(MemoryAddresses.Addresses.player_Camera_Pitch_Address);
                    if (pitch > 0.3 || pitch > -0.3 && main.BounceHandler.p_X_Vel > 1 || main.BounceHandler.p_X_Vel < -1 || main.BounceHandler.p_Y_Vel > 1 || main.BounceHandler.p_Y_Vel < -1) freezeValue = pitch * 12;

                    freezeValue = m.ReadFloat(MemoryAddresses.Addresses.player_Camera_Pitch_Address) * 8;
                }
                m.WriteToMemory((main.ObjectHandler.objectHavokAddress - 0x28), "float", freezeValue.ToString());

                await Task.Delay(29);
            }
        }

    }
}
