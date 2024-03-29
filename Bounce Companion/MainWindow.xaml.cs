﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using HtmlAgilityPack;
using Memory;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
//using Google.Apis.Auth.OAuth2;
//using Google.Apis.Sheets.v4;
using System.Text;
using Newtonsoft.Json;
using System.Windows.Interop;
//using WebSocketSharp;
using System.Windows.Media.Imaging;
using SharpDX.XInput;
using System.Windows.Controls;
using MathNet.Numerics.Interpolation;
using Microsoft.Win32;
using System.Xml;
using static Bounce_Companion.MainWindow;
using static Bounce_Companion.ControllerKeyBinds;
using System.Numerics;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
//using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrackBar;

namespace Bounce_Companion
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 


    //Null Char spawn filter - halo2.exe+0030D3F8
    public partial class MainWindow : Window
    {
        //GameOverlayWindow overlayWindow = new GameOverlayWindow();
        public string currentVersion = "0.36.24 "; // Change this to your current application version
        public string newVersion = string.Empty;
        public Mem m = new Mem();
        public Mem mp2 = new Mem();
        public Process p;
        public RuntimeMemory rm;
        private GameChatWindow gameChatWindow;
        CommandExecution CE;
        HandleChallenges CH;
        public List<Command> Commands = new List<Command>();
        GameOverlayWindow GOW;
        public ReplaySystem replaySystem;
        public Settings settingsWindow;
        public ControllerKeyBinds controllerKeyBindsWindow;
        public KeyBoardBinds keyboardKeyBindsWindow;
        public string currentGameMainWindow = string.Empty;
        public bool attached = false;
        bool modsEnabled;
        bool modsEnabledMaster = true;
        /// <summary>
        /// Config Options
        /// </summary>
        public string mapspath = string.Empty;
        public string customMapsPath = string.Empty;
        public string serverPlaylistPath = string.Empty;
        //camera flyspeeds
        public float c_moveSpeed = 0f;
        public float c_turnSpeed = 0f;
        public float c_pitchSpeed = 0f;
        public float c_heightSpeed = 0f;
        public float c_rollSpeed = 0f;
        // Camera Transition speed
        public float c_GlobalTransitionTime = 0f;
        public int loopDelayTime = 100;

        public bool tags_Loaded_Status;
        public bool enableDebug = false;
        public bool customContrails = false;
        private bool userTextChanged = true;
        public bool isAppLoading = true;
        public int obj_List_Address = 0;
        //public WebSocket ws;
        private MediaPlayer audio_Player_Main = new MediaPlayer();
        private MediaPlayer audio_Player_Main_2 = new MediaPlayer();
        private MediaPlayer audio_Player_Effect_1 = new MediaPlayer();
        private MediaPlayer audio_Player_Effect_2 = new MediaPlayer();
        private MediaPlayer audio_Player_Effect_3 = new MediaPlayer();
        public DispatcherTimer UpdateTimer = new DispatcherTimer();
        public DispatcherTimer CheckerTimer = new DispatcherTimer();
        public DispatcherTimer PlayerMonitorTimer = new DispatcherTimer();
        public GameVolumeGetter volumeGetter = new GameVolumeGetter();
        private List<ImageData> imageDataList; // Store the image data
        private Controller xboxController;
        private System.Threading.Timer toggleTimer;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr FindWindow(string strClassName, string strWindowName);

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hwnd, ref Rect rectangle);

        public MainWindow()
        {
            InitializeComponent();
            imageDataList = new List<ImageData>();
            toggleTimer = new System.Threading.Timer(ToggleCallback, null, Timeout.Infinite, Timeout.Infinite);
            _ = InitializeXboxController();
            staackPanel_CameraTool.Visibility = Visibility.Hidden;
            controllerKeyBindsWindow = new ControllerKeyBinds(this);
            controllerKeyBindsWindow.Closed += Window_Closed;
            SizeChanged += MainWindow_SizeChanged;
        }
        public static class ToolSetting
        {
            public static string currentGame = string.Empty;
            public static Process p = null;
            public static string currentGameDLL = string.Empty;
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
            string[] STRList = commandlineList.Select(s => CleanString(s.InnerHtml)).ToArray();
            STRList = STRList.Where(s => s != "").ToArray();
            httpClient.Dispose();
            return STRList.ToList();
        }

        public string CleanString(string str)
        {
            if (str.Contains("/"))
            {
                return str.Replace("\r", "");
            }
            else
            {
                return str;
            }
        }



        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LabStatus.Foreground = Brushes.Red;

            if (await CheckForNewVersion())
            {

            }
            else
            {
                //attempt to attach
                AttachToProcess(0);
                GetCommansFromFile();
                PrintToConsole_ContinueNextText("Checking Config . . . ");
                await CheckAndSetMapFiles();
                SetupConfig();


                if (attached)
                {
                    LabStatus.Foreground = Brushes.Green;
                    LabStatus.Content = "Process: Attached";
                    GOW = new GameOverlayWindow(this);
                    GOW.EnablePlayerInfoTab(false);
                    GOW.Show();
                    if (settingsWindow == null)
                    {
                        settingsWindow = new Settings(this);
                        settingsWindow.Closed += SettingsWindow_Closed; // Handle window closed event
                    }
                    replaySystem = new ReplaySystem(m, this);
                    SetWindowPos();
                    // Attach an event handler to the GotFocus event of the "Camera Tool" tab
                    TabItem_CameraTool.GotFocus += TabItem_CameraTool_GotFocus;

                    // Attach an event handler to the LostFocus event of the "Camera Tool" tab
                    TabItem_CameraTool.LostFocus += TabItem_CameraTool_LostFocus;

                    GetLocationData();
                    LoadSFXFromXml();
                    SetCameraFlySpeeds();
                    ParseCommandsFromFile();
                    SetCameraAddresses();
                    StartUpdateTask();
                    // Initialize key states
                    foreach (ControllerKey key in Enum.GetValues(typeof(ControllerKey)))
                    {
                        keyStates[key] = false;
                    }
                }
                else
                {
                    LabStatus.Foreground = Brushes.Red;
                    LabStatus.Content = "Process: Not Attached";
                }
                UpdateTimer.Interval = TimeSpan.FromMilliseconds(150);
                CheckerTimer.Interval = TimeSpan.FromMilliseconds(31);

                isAppLoading = false;
            }

        }

        private async Task<bool> CheckForNewVersion()
        {
            PrintToConsole_ContinueNextText("Checking for newer Versions . . .  ");
            string owner = "AHPKillSwitch";
            string repo = "Bounce-Companion";

            bool isNewVersionAvailable = false; // await CompanionUpdater.UpdateChecker.IsNewVersionAvailable(owner, repo, currentVersion, this);
            if (isNewVersionAvailable)
            {
                MessageBoxResult result = MessageBox.Show("A new version is available. Do you want to download it now?", "Update Available", MessageBoxButton.YesNo);

                if (result == MessageBoxResult.Yes)
                {
                    PrintToConsole("Download Starting!");
                    CompanionUpdater.UpdateChecker.DownloadLatestZipRelease(owner, repo);
                    PrintToConsole("Download Complete: \n" +
                        "New Version can be found in your current Bounce Companion root folder, Its called Bounce Companion " + newVersion + ".zip");
                }
                else
                {
                    PrintToConsole("Update Found: User Denied!");
                    return false;

                }
            }
            else
            {
                PrintToConsole("Current Version is up-to date!");
            }
            return isNewVersionAvailable;
        }

        private async void StartUpdateTask()
        {
            try
            {
                await Task.Run(() => UpdateChecker());
            }
            catch
            {
                PrintToConsole("Error in bounce checker.");
            }
        }
        private void SetCameraFlySpeeds()
        {
            settingsWindow.TextBox_FlySpeed.Text = c_moveSpeed.ToString();
            settingsWindow.TextBox_Turnspeed.Text = c_turnSpeed.ToString();
            settingsWindow.TextBox_Pitchspeed.Text = c_pitchSpeed.ToString();
            settingsWindow.TextBox_Heightspeed.Text = c_heightSpeed.ToString();
            settingsWindow.TextBox_Rollspeed.Text = c_rollSpeed.ToString();
            GlobalTransitionTimeTextBox.Text = c_GlobalTransitionTime.ToString();
            int tickrate = m.ReadByte("halo2.exe+0x004C06E4,0x02");
            settingsWindow.Textbox_Tickrate.Text = tickrate.ToString();
        }

        private void SetCameraAddresses()
        {
            cameraPositionAddress = m.ReadInt("halo2.exe+0x004D84EC");
            xAddress = cameraPositionAddress;
            yAddress = cameraPositionAddress + 0x4;
            zAddress = cameraPositionAddress + 0x8;
            yawAddress = cameraPositionAddress + 0xC;
            pitchAddress = cameraPositionAddress + 0x10;
            rollAddress = cameraPositionAddress + 0x14;
        }

        private async void SetupConfig()
        {
            ConfigJson configJson = await SetConfig().ConfigureAwait(false);
        }

        private async Task<ConfigJson> SetConfig()
        {
            var json = string.Empty;

            using (var fs = File.OpenRead("Config.json"))
            using (var sr = new StreamReader(fs, new UTF8Encoding(false)))
                json = await sr.ReadToEndAsync().ConfigureAwait(false);


            var configJson = JsonConvert.DeserializeObject<ConfigJson>(json);
            mapspath = configJson.MapsPath;
            customMapsPath = configJson.CustomMapsPath;
            serverPlaylistPath = configJson.ServerPlaylistPath;

            c_moveSpeed = float.Parse(configJson.MoveSpeed);
            c_turnSpeed = float.Parse(configJson.TurnSpeed);
            c_pitchSpeed = float.Parse(configJson.PitchSpeed);
            c_heightSpeed = float.Parse(configJson.HeightSpeed);
            c_rollSpeed = float.Parse(configJson.RollSpeed);
            c_GlobalTransitionTime = float.Parse(configJson.globalTransitionTime);
            return configJson;
        }
        private async Task UpdateConfig(ConfigJson config)
        {
            var json = JsonConvert.SerializeObject(config, Newtonsoft.Json.Formatting.Indented);

            await File.WriteAllTextAsync("Config.json", json).ConfigureAwait(false);
        }

        public async Task UpdateConfigWithNewOptions()
        {
            var config = await SetConfig();

            // Add new options
            config.MoveSpeed = settingsWindow.TextBox_FlySpeed.Text;
            config.TurnSpeed = settingsWindow.TextBox_Turnspeed.Text;
            config.PitchSpeed = settingsWindow.TextBox_Pitchspeed.Text;
            config.HeightSpeed = settingsWindow.TextBox_Heightspeed.Text;
            config.RollSpeed = settingsWindow.TextBox_Rollspeed.Text;

            await UpdateConfig(config);
        }
        public async Task UpdateCameraSpeedUI()
        {
            var config = await SetConfig();

            // Add new options
            settingsWindow.TextBox_FlySpeed.Text = config.MoveSpeed;
            settingsWindow.TextBox_Turnspeed.Text = config.TurnSpeed;
            settingsWindow.TextBox_Pitchspeed.Text = config.PitchSpeed;
            settingsWindow.TextBox_Heightspeed.Text = config.HeightSpeed;
            settingsWindow.TextBox_Rollspeed.Text = config.RollSpeed;

            await UpdateConfig(config);
        }

        public class ConfigJson
        {
            public string MapsPath { get; set; }
            public string CustomMapsPath { get; set; }
            public string ServerPlaylistPath { get; set; }
            public string MoveSpeed { get; set; }
            public string TurnSpeed { get; set; }
            public string PitchSpeed { get; set; }
            public string HeightSpeed { get; set; }
            public string RollSpeed { get; set; }
            public string globalTransitionTime { get; set; }
        }
        private async Task CheckAndSetMapFiles()
        {
            ConfigJson config = await SetConfig();

            if (string.IsNullOrWhiteSpace(config.MapsPath) || string.IsNullOrWhiteSpace(config.CustomMapsPath) || !config.MapsPath.Contains("Halo 2 Project Cartographer\\maps") || !config.CustomMapsPath.Contains("Documents\\My Games\\Halo 2\\Maps"))
            {
                PrintToConsole("Invalid Config Found.");

                // Show a custom dialog with a message and "Select Halo 2 Executable" button
                MessageBoxResult result = MessageBox.Show(
                    "Invalid game or custom maps path found. Please select the Halo 2 executable file (halo2.exe).",
                    "Configuration Invalid",
                    MessageBoxButton.OKCancel);

                if (result == MessageBoxResult.OK)
                {
                    var halo2ExeDialog = new Microsoft.Win32.OpenFileDialog
                    {
                        Title = "Select Halo 2 Executable",
                        Filter = "Halo 2 Executable|halo2.exe",
                        CheckFileExists = true,
                    };

                    if (halo2ExeDialog.ShowDialog() == true)
                    {
                        string halo2ExePath = halo2ExeDialog.FileName;
                        string gameFolderPath = System.IO.Path.GetDirectoryName(halo2ExePath);
                        string mapsFolderPath = System.IO.Path.Combine(gameFolderPath, "maps");

                        // Check if the maps folder exists within the selected game folder
                        if (System.IO.Directory.Exists(mapsFolderPath))
                        {
                            PrintToConsole("Maps Folder Set");

                            config.MapsPath = mapsFolderPath;

                            // Check and create the custom maps folder in Documents\My Games\Halo 2\Maps
                            string customMapsFolderPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", "Halo 2", "Maps");
                            if (!System.IO.Directory.Exists(customMapsFolderPath))
                            {
                                System.IO.Directory.CreateDirectory(customMapsFolderPath);
                            }

                            config.CustomMapsPath = customMapsFolderPath;

                            await UpdateConfig(config);


                        }
                        else
                        {
                            PrintToConsole("Invalid Maps Folder. Please select the correct game folder.");
                            Environment.Exit(0);
                        }
                    }
                    else
                    {
                        //Environment.Exit(0);
                    }
                }
                else
                {
                    //Environment.Exit(0);
                }
            }

            PrintToConsole("Config Is Valid.");
        }



        public int selectedProcessindex = -1;
        public void AttachToProcess(int selectedIndex)
        {
            if (attached && selectedIndex == selectedProcessindex) return;
            if (attached)
            {
                //m.CloseProcess(); 
                //mp2.CloseProcess(); 
                attached = false;
            }


            try
            {
                PrintToConsole_ContinueNextText("Attempting to attach to the Halo 2 game process . . .  ");
                Process[] processes = Process.GetProcessesByName("halo2");
                if (selectedIndex < processes.Length)
                {
                    p = processes[selectedIndex];
                    PrintToConsole("Success.");
                    ToolSetting.currentGame = "halo2";
                    currentGameMainWindow = "halo2";
                    ToolSetting.currentGameDLL = "halo2";
                    ToolSetting.p = p;
                    CheckerTimer.Start();
                    UpdateTimer.Start();
                    PlayerMonitorTimer.Start();
                    if (selectedIndex == 0)
                    {
                        p = processes[0];
                        m.OpenProcess(p.Id);
                        selectedProcessindex = selectedIndex;
                        PrintToConsole("Main Process Success.");
                        attached = true;
                        if (selectedIndex < 0)
                        {
                            p = processes[1];
                            mp2.OpenProcess(p.Id);
                            player2Attched = true;
                            PrintToConsole("Second Process Success.");
                        }
                    }
                    else
                    {
                        p = processes[0];
                        mp2.OpenProcess(p.Id);
                        selectedProcessindex = selectedIndex;
                        PrintToConsole("Second Process Success.");
                        p = processes[1];
                        m.OpenProcess(p.Id);
                        PrintToConsole("Main Process Success.");
                        attached = true;
                        player2Attched = true;
                    }

                }
                else
                {
                    PrintToConsole("Failed, the selected process does not exist.");
                }

            }
            catch
            {
                PrintToConsole("Failed, make sure game is running");
            }
        }
        public bool player2Attched = false;
        bool tagscurrentlyloaded;
        public bool UpdateTagStatus()
        {
            string tags_Loaded = m.ReadString("halo2.exe+47CF0C", "");
            bool tags_Loaded_Status = tags_Loaded != "mainmenu";

            bool newTagsLoaded = tagscurrentlyloaded;

            if (!tagscurrentlyloaded && tags_Loaded_Status) //Check if tags are loaded if not load them
            {
                tagscurrentlyloaded = true;
                PrintToConsole_ContinueNextText("Checking Game Session . . . ");
                if (GameTypeValidCheck())
                {
                    PrintToConsole("Valid Session Found!");
                    HideMods(false);
                    rm = new RuntimeMemory(m, p, this, mapspath, customMapsPath);
                    CE = new CommandExecution(rm, m, this);
                    CH = new HandleChallenges(this); //challenge handler
                    if (auto_warpFix) _ = ApplyGameMod("warpfix", true);
                }
                else
                {
                    PrintToConsole("Invalid Session Found! " +
                        "Mods Disabled." +
                        "This tool will only work in OGH2 or Glitch Lobbies!");

                    HideMods(true);
                }


            }
            else if (tagscurrentlyloaded && !tags_Loaded_Status)
            {
                tagscurrentlyloaded = false;
                progressBar_TagsProgress.Value = 0;
            }
            SetWindowPos();



            return tags_Loaded_Status;
        }
        private void HideMods(bool hide)
        {
            if (hide)
            {
                ComboBox_Mods.Visibility = Visibility.Hidden;
                Bttn_on.Visibility = Visibility.Hidden;
                Bttn_off.Visibility = Visibility.Hidden;
                modsEnabledMaster = false;
                _ = ApplyGameMod("//debugcamera", false);
                button_DebugCamera.Visibility = Visibility.Hidden;
            }
            else
            {
                ComboBox_Mods.Visibility = Visibility.Visible;
                Bttn_on.Visibility = Visibility.Visible;
                Bttn_off.Visibility = Visibility.Visible;
                modsEnabledMaster = true;
                button_DebugCamera.Visibility = Visibility.Visible;
            }
        }

        private bool GameTypeValidCheck()
        {
            string gameTypeName = System.Text.Encoding.Unicode.GetString(m.ReadBytes("halo2.exe+97777C", 127, "")).Split('\0').FirstOrDefault(part => !string.IsNullOrEmpty(part));




            //string gameTypeName = m.ReadString("halo2.exe+97777C", "", 127, true);
            if (gameTypeName.ToLower().Contains("ogh2") || gameTypeName.ToLower().Contains("glitch"))
                return true;
            else { return false; }

        }
        private async Task UpdateChecker()
        {
            while (true)
            {
                if (!rollCamera || !debugCameraToggle && attached)
                {
                    if (Application.Current == null)
                    {
                        Environment.Exit(1); // You can choose an appropriate exit code
                    }
                    bool tags_Loaded_Status = false;
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        tags_Loaded_Status = UpdateTagStatus();
                    });
                    if (customContrails)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            _ = UpdatePlayerList();
                        });
                    }

                    if (tags_Loaded_Status)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            BounceChecker();
                        });
                    }

                }
                Application.Current.Dispatcher.Invoke(() =>
                {
                    float[] cameraPosition = GetCameraData(out byte[] cameraPositionArray);
                    UpdateUICameraCoordinates(cameraPosition[0], cameraPosition[1], cameraPosition[2], cameraPosition[3], cameraPosition[4], cameraPosition[5]);
                });
                await Task.Delay(33); // Milliseconds
            }
        }
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
        public int havokaddress = 0;
        public Task BounceChecker()
        {
            try
            {
                int tickrate = m.ReadByte("halo2.exe+004C06E4,0x2");
                int p_Index = m.Read2Byte("halo2.exe+4E7C88");
                int obj_List_Address = m.ReadInt("halo2.exe+4E461C,0x44");
                int obj_List_Memory_Address = m.ReadInt((obj_List_Address + p_Index * 0xC + 0x8).ToString("X"));

                float p_X = ReadPlayerFloat(obj_List_Memory_Address, 0xC * 0x4);
                float p_Y = ReadPlayerFloat(obj_List_Memory_Address, 0xD * 0x4);
                float p_Z = ReadPlayerFloat(obj_List_Memory_Address, 0xE * 0x4);
                float p_X_Vel = ReadPlayerFloat(obj_List_Memory_Address, 0x22 * 0x4);
                float p_Y_Vel = ReadPlayerFloat(obj_List_Memory_Address, 0x23 * 0x4);
                float p_Z_Vel = ReadPlayerFloat(obj_List_Memory_Address, 0x24 * 0x4);
                int p_Airbourne = ReadPlayerByte(obj_List_Memory_Address, 0xD8 * 0x4);
                int hav_Index_Datum = m.ReadInt((obj_List_Memory_Address + 0xB4).ToString("X"));
                havokaddress = GetHavokAddressFromHavokSalt(hav_Index_Datum.ToString("X"));
                replaySystem.RecordPlayerPosition(p_X, p_Y, p_Z, p_X_Vel, p_Y_Vel, p_Z_Vel);
                UpdateUI(p_X_Vel, p_Y_Vel, p_Z_Vel, p_X, p_Y, p_Z, tickrate);
                _ = HandleBounces(p_X, p_Y, p_Z, p_X_Vel, p_Y_Vel, p_Z_Vel, p_Airbourne);
                if (freeStyleMode) FreestlyeMode(p_X_Vel, p_Y_Vel, p_Z_Vel);
                p_Vel_Prev_Record = p_Z_Vel;
                p_Z_Prev_Record = p_Z;
                prev_P_X_Vel = p_X_Vel;
                prev_P_Y_Vel = p_Y_Vel;
                prev_P_Z_Vel = p_Z_Vel;
            }
            catch
            {
                PrintToConsole("Read Error in Bounce Checker");
            }
            return null;
        }
        public int GetHavokAddressFromHavokSalt(string salt)
        {
            if (salt == "0") return 0;
            int p_Index = int.Parse(salt.Remove(0, 4), System.Globalization.NumberStyles.HexNumber);
            int hav_List_Address = m.ReadInt("halo2.exe+004D83B8,0x44", "");
            int hav_List_Memory_Address = m.ReadInt((hav_List_Address + (p_Index * 0xA0) + 0x70).ToString("X"), "");

            hav_List_Memory_Address += 0x40;
            hav_List_Memory_Address = m.ReadInt(hav_List_Memory_Address.ToString("X"), "");
            hav_List_Memory_Address += 0x14;
            hav_List_Memory_Address = m.ReadInt(hav_List_Memory_Address.ToString("X"), "");
            return hav_List_Memory_Address += 0x10;
        }
        private void FreestlyeMode(float p_X_Vel, float p_Y_Vel, float p_Z_Vel)
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

        private float ReadPlayerFloat(int baseAddress, int offset)
        {
            return m.ReadFloat((baseAddress + offset).ToString("X"), "");
        }
        private int ReadPlayerByte(int baseAddress, int offset)
        {
            return m.ReadByte((baseAddress + offset).ToString("X"), "");
        }

        private void UpdateUI(float p_X_Vel, float p_Y_Vel, float p_Z_Vel, float p_X, float p_Y, float p_Z, int tr)
        {
            if (checkbox_EnableBounceStats.IsChecked == true && checkbox_EnableBounceXYZStats.IsChecked == true)
            {
                PrintToConsole($"Player XYZ Velocity: {p_X_Vel}, {p_Y_Vel}, {p_Z_Vel}    Player XYZ Position: {p_X}, {p_Y}, {p_Z}");
            }
            GOW.UpdateStatusBar(bounceCount, p_Z_Vel, p_X, p_Y, p_Z, tr);
            GOW.UpdateSliders(p_X_Vel, p_Y_Vel, p_Z_Vel);
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

        private void MarkPlayerCoords(string location, string type, string Velocity)
        {
            try
            {
                int p_Index = m.Read2Byte("halo2.exe+4E7C88", "");
                int obj_List_Address = m.ReadInt("halo2.exe+4E461C,0x44", "");

                //      p_index object memory address aka player
                int obj_List_Memory_Address = m.ReadInt((obj_List_Address + p_Index * 0xC + 0x8).ToString("X"), ""); //0xC = Size of struct      0x8 = offset to memory address in current struct

                float p_X = m.ReadFloat((obj_List_Memory_Address + 0xC * 0x4).ToString("X"), "");
                float p_Y = m.ReadFloat((obj_List_Memory_Address + 0xD * 0x4).ToString("X"), "");
                float p_Z = m.ReadFloat((obj_List_Memory_Address + 0xE * 0x4).ToString("X"), "");

                string bouncelocation = "/" + location + ":" + type + ":" + (Math.Round(p_X - 0.4, 2)).ToString() + ":" + (Math.Round(p_X + 0.4, 2)).ToString() + ":" + (Math.Round(p_Y - 0.4, 2)).ToString() + ":" + (Math.Round(p_Y + 0.4, 2)).ToString() + ":0:0:0:" + Velocity;
                PrintToConsole(bouncelocation);
                WriteBouncePositionToString(bouncelocation);
            }
            catch { PrintToConsole("Error: Failed to get player position."); }
        }
        private void ListAllLocations()
        {
            string path = @"Content\BounceLocations.txt";

            try
            {
                if (File.Exists(path))
                {
                    string[] lines = File.ReadAllLines(path);

                    foreach (string line in lines)
                    {
                        string[] parts = line.Split(':');
                        string locationName = parts[1];
                        float originalX = float.Parse(parts[2]);
                        float originalY = float.Parse(parts[4]);
                        float originalZ = float.Parse(parts[6]);

                        string output = $"{locationName}: ({originalX}, {originalY}, {originalZ})";
                        PrintToConsole(output);
                    }
                }
                else
                {
                    PrintToConsole("No bounce locations found.");
                }
            }
            catch (Exception ex)
            {
                PrintToConsole($"Error: {ex.Message}");
            }
        }
        //private List<LocationData> bounceLocations = new List<LocationData>();

        //public struct LocationData
        //{
        //    public string Name { get; set; }
        //    public float OriginalX { get; set; }
        //    public float OriginalY { get; set; }
        //    public float OriginalZ { get; set; }
        //}
        private void WriteBouncePositionToString(string position)
        {
            string path = @"Content\BounceLocations.txt";

            if (!File.Exists(path))
            {
                File.Create(path).Dispose();
                WritePosition(path);
            }
            else if (File.Exists(path))
            {
                WritePosition(path);
            }

            void WritePosition(string path)
            {
                using (TextWriter tw = new StreamWriter(path))
                {
                    tw.WriteLine(position);
                }
            }
        }


        private async Task BounceDetected(float p_X, float p_Y, float p_Z, float p_X_Vel, float p_Y_Vel, float p_Z_Vel, string bouncetype)
        {
            bouncetype = SlantCheckBounceType(p_X_Vel, p_Y_Vel, bouncetype);
            PrintBounceDetails(p_X, p_Y, p_Z, p_X_Vel, p_Y_Vel, p_Z_Vel, bouncetype);
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
            if (checkbox_DisableOverlay.IsChecked != true)
            {
                await Announcements(bounceCount, location, bouncetype);
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

        private void PrintBounceDetails(float p_X, float p_Y, float p_Z, float p_X_Vel, float p_Y_Vel, float p_Z_Vel, string bouncetype)
        {
            string details = $" - - - - - - - - Bounce Detected - - - - - - - -\n" +
                $"Triggered at location X: {p_X} Y: {p_Y} Z: {p_Z} \n" +
                $"Trigged Velocity - X: {p_X_Vel} Y: {p_Y_Vel} Z: {p_Z_Vel}\n" +
                $"Pre Bounce Velocity - X: {prev_P_X_Vel} Y: {prev_P_Y_Vel} Z: {prev_P_Z_Vel} \n" +
                $"Bounce Type: {bouncetype} \n";

            PrintToConsole(details);
        }
        private string GetBounceText(int number)
        {
            switch (number)
            {
                case 2: return "Double Bounce!";
                case 3: return "Triple Bounce!";
                case 4: return "Quad Bounce!";
                case 5: return "Bouncetacular!";
                case 6: return "Bouncetrocity!";
                case 7: return "Bungee Jumper!";
                case 8: return "Octabounce!";
                case 9: return "Bouncepocalypse!";
                case 10: return "Bounce Revival!";
                default: return "";
            }
        }
        public string sfx2 = string.Empty;
        public string sfx3 = string.Empty;
        public string sfx4 = string.Empty;
        public string sfx5 = string.Empty;
        public string sfx6 = string.Empty;
        public string sfx7 = string.Empty;
        public string sfx8 = string.Empty;
        public string sfx9 = string.Empty;
        public string sfx10 = string.Empty;

        public void LoadSFXFromXml()
        {
            string xmlFileName = "sfxnames.xml"; // Hardcoded XML file name
            string xmlFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Content", "XMLs", xmlFileName);
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(xmlFilePath);

            XmlNodeList sfxNodes = xmlDoc.SelectNodes("//sfxnames/sfx");
            foreach (XmlNode sfxNode in sfxNodes)
            {
                if (int.TryParse(sfxNode.Attributes["number"].Value, out int sfxNumber))
                {
                    string sfxValue = sfxNode.InnerText;

                    switch (sfxNumber)
                    {
                        case 2: sfx2 = sfxValue; break;
                        case 3: sfx3 = sfxValue; break;
                        case 4: sfx4 = sfxValue; break;
                        case 5: sfx5 = sfxValue; break;
                        case 6: sfx6 = sfxValue; break;
                        case 7: sfx7 = sfxValue; break;
                        case 8: sfx8 = sfxValue; break;
                        case 9: sfx9 = sfxValue; break;
                        case 10: sfx10 = sfxValue; break;
                    }
                }
            }
        }

        private string GetBounceSFX(int number)
        {
            switch (number)
            {
                case 2: return sfx2;
                case 3: return sfx3;
                case 4: return sfx4;
                case 5: return sfx5;
                case 6: return sfx6;
                case 7: return sfx7;
                case 8: return sfx8;
                case 9: return sfx9;
                case 10: return sfx10;
                default: return "";
            }
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
        private void ReadObjectXYZ(string p_Salt, out int obj_List_Memory_Address, out float p_X, out float p_Y, out float p_Z, out float p_X_Vel, out float p_Y_Vel, out float p_Z_Vel, out float p_Yaw, out float p_pitch, out float p_Shields)
        {

            obj_List_Address = m.ReadInt("halo2.exe+4E461C,0x44", "");
            int p_Index = int.Parse(p_Salt.Remove(0, 4), System.Globalization.NumberStyles.HexNumber);

            obj_List_Memory_Address = m.ReadInt((obj_List_Address + p_Index * 0xC + 0x8).ToString("X"), "");
            p_X = m.ReadFloat((obj_List_Memory_Address + 0x64).ToString("X"), "");
            p_Y = m.ReadFloat((obj_List_Memory_Address + 0x68).ToString("X"), "");
            p_Z = m.ReadFloat((obj_List_Memory_Address + 0x6C).ToString("X"), "");
            p_X_Vel = m.ReadFloat((obj_List_Memory_Address + 0x22 * 0x4).ToString("X"), "");
            p_Y_Vel = m.ReadFloat((obj_List_Memory_Address + 0x23 * 0x4).ToString("X"), "");
            p_Z_Vel = m.ReadFloat((obj_List_Memory_Address + 0x24 * 0x4).ToString("X"), "");
            p_Yaw = m.ReadFloat((obj_List_Memory_Address + 0x170 * 0x4).ToString("X"), "");
            p_pitch = m.ReadFloat((obj_List_Memory_Address + 0x15C * 0x4).ToString("X"), "");
            p_Shields = m.ReadFloat((obj_List_Memory_Address + 0xF0 * 0x4).ToString("X"), "");
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
                List<string> result = PullCommands("https://pastebin.com/JY9FMDLX");


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

        List<int> bouncenumber = new List<int> { 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 };

        public async Task Announcements(int count, string location, string type)
        {

            if (checkbox_DisableOverlay.IsChecked == false)
            {
                string bounceSFX = string.Empty;
                if (location != "null")
                {
                    PrintToConsole(location + " Bounce Hit");
                    bounceSFX = GetBounceSFX(11);
                    //await PlayLocationalAudioShowEmblem(location, location);
                    await PlayMultiAudioShowEmblem("null", location, bounceSFX);
                    //await Task.Delay(1000);
                }
                foreach (int number in bouncenumber)
                {
                    if (number == count)
                    {
                        string bounceText = string.Empty;
                        if (type.Contains("slant")) location = type;
                        else
                        {
                            bounceText = GetBounceText(number);
                            if (bounceSFX != "null") bounceSFX = GetBounceSFX(number);
                        }

                        PrintToConsole(bounceText);
                        await PlayMultiAudioShowEmblem(number.ToString(), bounceText, bounceSFX);
                        break;
                    }
                }
                if (count > 10)
                {
                    bounceSFX = GetBounceSFX(2);
                    await PlayMultiAudioShowEmblem(count.ToString(), count.ToString(), bounceSFX);
                }

            }


        }
        private async Task PlayMultiAudioShowEmblem(string bounceNumber, string bounceText, string bounceSFX)
        {
            _ = GOW.ShowEmblem(bounceNumber, bounceText);
            if (!string.IsNullOrEmpty(bounceSFX))
            {
                PlaySFX(audio_Player_Effect_1, "Content/Audio/SFX/", bounceSFX, volumeGetter.GetGameVolumeLevel(this));
                await Task.Delay((int)settingsWindow.Slider_DelayAudio.Value);
            }
            PlaySFX(audio_Player_Main, "Content/Audio/MultiBounces/", bounceNumber, volumeGetter.GetGameVolumeLevel(this));
        }

        private void PlaySFX(MediaPlayer audio_Player, string filePath, string bounceNumber, float audioLevel)
        {
            audio_Player.Open(new Uri(filePath + bounceNumber + ".wav", UriKind.Relative));

            audio_Player.Volume = audioLevel;
            audio_Player.Play();
        }

        private async Task PlayLocationalAudioShowEmblem(string bounceNumber, string bounceText)
        {
            audio_Player_Main = new MediaPlayer();
            audio_Player_Main.Open(new Uri("Content/Audio/Locational/" + bounceText.ToLower() + ".wav", UriKind.Relative));
            audio_Player_Main.Volume = 0.4;
            //PlaySniperAudio();
            audio_Player_Main.Play();
            //await GOW.ShowEmblem(bounceText, bounceText);
        }
        private void PlaySniperAudio()
        {
            audio_Player_Main_2 = new MediaPlayer();
            audio_Player_Main_2.Open(new Uri("Content/Audio/Locational/sniperfire.wav", UriKind.Relative));
            audio_Player_Main_2.Volume = 0.4;
            audio_Player_Main_2.Play();
        }

        public void PrintToConsole(string input)
        {
            ConsoleOut.AppendText(input + "\n");
            ConsoleOut.ScrollToEnd();
        }
        public void PrintToConsole_ContinueNextText(string input)
        {
            ConsoleOut.AppendText(input);
            ConsoleOut.ScrollToEnd();
        }
        bool follow;

        private void playerInfoTabClick(object sender, RoutedEventArgs e)
        {
            if (checkbox_DisableOverlay.IsChecked == true) PrintToConsole("Screen Overlay is Disabled - Please Enable it to see Player Info Bar.");
            else if (checkbox_EnableBounceStats.IsChecked == true)
            {
                follow = true;
                GOW.EnablePlayerInfoTab(true);
                PrintToConsole("Player Info Bar: Enabled \nNote: Click and drag the Player Info Bar to place it in a new position. ");
                //FollowCam();
            }
            else
            {
                follow = false;
                GOW.EnablePlayerInfoTab(false);
                PrintToConsole("Player Info Bar: Disabled");
            }
        }
        private Task FollowCam()
        {
            while (true)
            {
                if (follow)
                {
                    float playerz = m.ReadFloat("30077420", "");
                    m.WriteMemory("20CC8428", "float", playerz.ToString(), "");
                }
            }
        }

        private void Application_Exit(object sender, EventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        public void SetWindowPos()
        {
            IntPtr ptr = p.MainWindowHandle;
            Rect halo2WindowRect = new Rect();
            GetWindowRect(ptr, ref halo2WindowRect);
            Application.Current.MainWindow = GOW;
            Application.Current.MainWindow.Left = halo2WindowRect.Left;
            Application.Current.MainWindow.Top = halo2WindowRect.Top;
            Application.Current.MainWindow.Width = halo2WindowRect.Right - halo2WindowRect.Left;
            Application.Current.MainWindow.Height = halo2WindowRect.Bottom - halo2WindowRect.Top;

        }
        public struct Rect
        {
            public int Left { get; set; }
            public int Top { get; set; }
            public int Right { get; set; }
            public int Bottom { get; set; }
        }

        private void Disable_Medals(object sender, RoutedEventArgs e)
        {
            try
            {
                if (checkbox_DisableOverlay.IsChecked == true)
                {
                    GOW.Hide();
                    PrintToConsole("Overlay: Disabled");
                }
                else
                {
                    GOW.Show();
                    PrintToConsole("Overlay: Enabled");
                }
            }
            catch
            {
                GOW.Activate();
                PrintToConsole("Overlay: Enabled");
            }
        }

        private void ShowCredits(object sender, RoutedEventArgs e)
        {
            if (checkbox_Credits.IsChecked == true)
            {
                PrintToConsole("" +
                    "Credits: \n" +
                    "Luke: Farther of the Project, Creative Vision, Emblem Design and Concept, Audio Design \n" +
                    "KillSwitch: UI Design, Emblem Concept, Bounce Detection Code, Tag Editing Code, Camera Tool, General Programming\n" +
                    "Bestiole: Audio Engineering\n" +
                    "nhoj: Graphics/Emblem Designer\n" +
                    "Special Thanks:\n" +
                    "Harc, Lookenpeepers, Lord Zedd, Berthalamew, Glitchy Scripts, Jagged");

                checkbox_Credits.IsChecked = false;

            }
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

        public async Task<bool> GetCommands()
        {
            string url = System.Text.Encoding.Unicode.GetString(m.ReadBytes("halo2.exe+97777C", 127, "")).Split('\0').FirstOrDefault(part => !string.IsNullOrEmpty(part));

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
                modsEnabled = true;
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
                            PrintToConsole("Failed to apply command. " + command);
                        }
                    }
                    return true;
                }
                catch
                {
                    foreach (string line in System.IO.File.ReadLines("Content/Commands/" + url.Split(':')[1] + ".txt"))
                    {
                        string command = CleanString(line);
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
                            PrintToConsole(rm.outPutStrings);
                            rm.outPutStrings.Clear();
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
        private struct p_List_Info
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
        //List<p_List_Info> p_List = new List<p_List_Info>();
        Dictionary<string, p_List_Info> p_List = new Dictionary<string, p_List_Info>();
        int check = 0;
        private Task UpdatePlayerList()
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
                            PrintToConsole("Player Died");
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

        private string PopulatePlayersList(int i)
        {
            int playersAddress = m.ReadInt("halo2.exe+47CD48", "");
            playersAddress += 0x17D8 + 0x74;

            uint salt = m.ReadUInt((playersAddress + (0x204 * i)).ToString("X"), "");
            byte[] nameBytes = m.ReadBytes((playersAddress + (0x204 * i) + 0x18).ToString("X"), 32, "");
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

        private void SetContrails(string name, int index)
        {
            foreach (string playerData in p_colour)
            {
                List<string> result = new List<string>();
                string p_Name = playerData.Split(':')[1];
                if (p_Name == name)
                {
                    result.Add("/objects\\characters\\masterchief\\masterchief_mp.bipd.[Attachments:1].Attachment Type=?." + index.ToString() + ".cont");
                    result.Add("/objects\\characters\\masterchief\\masterchief_mp.bipd.[Attachments:1].Marker=");
                    result.Add("/objects\\characters\\masterchief\\masterchief_mp.bipd.[Attachments:1].Primary Scale=");
                    result.Add("/?" + index.ToString() + ".cont.Bitmap=effects\\bitmaps\\contrails\\covenant_beam.bitm");
                    result.Add("/?" + index.ToString() + ".cont.[Point States:0].Duration=1:1");
                    result.Add("/?" + index.ToString() + ".cont.[Point States:0].Transition Duration=0.6:0.6");
                    result.Add("/?" + index.ToString() + ".cont.[Point States:0].Physics=effects\\point_physics\\vacuum_particle.pph");
                    result.Add("/?" + index.ToString() + ".cont.[Point States:0].Color Lower Bound=" + playerData.Split(':')[2]);
                    result.Add("/?" + index.ToString() + ".cont.[Point States:0].Color Upper Bound=" + playerData.Split(':')[3]);
                    foreach (string command in result)
                    {
                        GetCommandsFromString("", command);
                        PrintToConsole(rm.outPutStrings);
                        rm.outPutStrings.Clear();

                    }
                }
            }
        }

        public void GetCommandsFromString(string consoleinput, string command)
        {
            modsEnabled = true;
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
            else if (modsEnabled && modsEnabledMaster)
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
                CE.c = currentcommand;
                CE.TagBlockProcessing(m);
                string output =
                    "Tag Type: " + "\t" + com.TagType + "\n" +
                    "Tag Name: " + "\t" + com.TagName + "\n" +
                    "Tag Blocks: " + "\t" + consoleArgs + "\n" +
                    "Edit Value: " + "\t" + com.Method + "\n" +
                    "New value: " + "\t" + com.value;


                rm.outPutStrings.Add(output);
                rm.outPutStrings.Add(strcommand);
            }

        }

        //private void GetVolume()
        //{
        //    // Identify the game's process (replace "GameProcessName" with the actual process name).
        //    var gameProcess = Process.GetProcessesByName("halo2").FirstOrDefault();

        //    if (gameProcess != null)
        //    {
        //        // Access the audio device.
        //        var enumerator = new MMDeviceEnumerator();
        //        var defaultDevice = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

        //        // Get the game's volume level.
        //        var volume = defaultDevice.AudioEndpointVolume.MasterVolumeLevelScalar;

        //        // Set your application's volume to match the game's volume.
        //        var yourAppVolume = 0.5f; // Set this to your desired volume level.
        //        defaultDevice.AudioEndpointVolume.MasterVolumeLevelScalar = yourAppVolume;
        //    }

        //}

        public static string ConvertByteToHex(byte[] byteData)
        {
            string hexValues = BitConverter.ToString(byteData).Replace("-", "");

            return hexValues;
        }
        public static string ConvertHexToUnicode(byte[] Hex)
        {
            return System.Text.Encoding.UTF8.GetString(Hex);
        }
        public static string ConvertHexToUnicode(string Hex)
        {
            byte[] textBytes = ConvertHexToByteArray(Hex);
            Hex = System.Text.Encoding.Unicode.GetString(textBytes);
            return Hex;
        }
        public static byte[] ConvertHexToByteArray(string hexString)
        {
            byte[] byteArray = new byte[hexString.Length / 2];

            for (int index = 0; index < byteArray.Length; index++)
            {
                string byteValue = hexString.Substring(index * 2, 2);
                byteArray[index] = byte.Parse(byteValue, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            }

            return byteArray;
        }

        private void ConsoleIn_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SendCommands();
            }
        }
        private void SendCommands()
        {
            string consoleInputString = txtbox_ConsoleIn.Text;
            PokeCommands(consoleInputString);
            txtbox_ConsoleIn.Clear();
        }
        public void PokeCommands(string consoleInputString)
        {
            GetCommandsFromString(consoleInputString, "");
        }
        private void GetCommansFromFile()
        {
            ComboBox_Mods.Items.Clear();
            DirectoryInfo d = new DirectoryInfo("Content/Commands/"); //Assuming Test is your Folder

            FileInfo[] Files = d.GetFiles("*.txt"); //Getting Text files
            string str = "";

            foreach (FileInfo file in Files)
            {
                ComboBox_Mods.Items.Add(file.Name.Split('.')[0]);
            }
        }

        private void ApplyDDLModOn(object sender, RoutedEventArgs e)
        {
            ApplyMods(true, ComboBox_Mods.SelectionBoxItem.ToString());
        }
        private void ApplyDDLModOff(object sender, RoutedEventArgs e)
        {
            ApplyMods(false, ComboBox_Mods.SelectionBoxItem.ToString());
        }


        public Dictionary<string, List<string>> hotKeyEnabledCommandDict = new Dictionary<string, List<string>>();
        public Dictionary<string, List<string>> hotKeyDisableCommandDict = new Dictionary<string, List<string>>();

        public void ParseCommandsFromFile()
        {
            List<string> enableCommand = new List<string>();
            List<string> disableCommand = new List<string>();
            IntPtr handle = new WindowInteropHelper(this).Handle;
            string keyBindBttn = string.Empty;
            string enableModsTextOutput = "";
            string disableModsTextOutput = "";

            List<string> result = new List<string>();
            DirectoryInfo d = new DirectoryInfo(@"Content/Commands/");
            FileInfo[] Files = d.GetFiles("*.txt");
            foreach (var filename in Files)
            {
                foreach (string commandLine in System.IO.File.ReadLines(filename.ToString()))
                {
                    string command = CleanString(commandLine);
                    result.Add(command);

                    foreach (string line in result)
                    {
                        if (line.Contains("KeyBindButton:"))
                        {
                            keyBindBttn = line.Split(':')[1];
                        }
                        if (line.Contains("Enable:"))
                        {
                            enableCommand.Add(line.Split(':')[1]);
                        }
                        if (line.Contains("Disable:"))
                        {
                            disableCommand.Add(line.Split(':')[1]);
                        }
                    }
                }
            }
            if (keyBindBttn != string.Empty)
            {
                try
                {
                    RegisterHotKey(handle, HOTKEY_ID, VK_SHIFT, Convert.ToUInt32(keyBindBttn, 16));
                    hotKeyEnabledCommandDict.Add(keyBindBttn, enableCommand);
                }
                catch
                {
                    PrintToConsole("Failed to Register Hotkey. Key: " + keyBindBttn);
                }

            }
        }
        List<Task> tasks = new List<Task>();
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
                string command = CleanString(line);
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
                            PrintToConsole(rm.outPutStrings);
                            rm.outPutStrings.Clear();
                        }
                        PrintToConsole(disableModsTextOutput);
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
                            PrintToConsole(rm.outPutStrings);
                            rm.outPutStrings.Clear();
                        }
                        PrintToConsole(enableModsTextOutput);
                    }
                }
                await Task.Delay(delayTime);
                delayTime = 0;
            }

        }
        [DllImport("user32.dll")]
        public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        public const int KEYEVENTF_KEYDOWN = 0x0001; // Key down
        public const int KEYEVENTF_KEYUP = 0x0002;   // Key up

        public const byte VK_SPACE = 0x20; // The virtual key code for the spacebar
        public bool debug = false;
        private async Task<int> ApplyGameMod(string command, bool on)
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
                        debug = true;
                        bounceCount++;
                        PrintToConsole("+1 Added to bounce count.");
                        _ = Announcements(bounceCount, "null", "standard");
                        break;
                    }
                case "//clearbounce":
                    {
                        debug = false;
                        bounceCount = 0;
                        PrintToConsole("Bounce count reset.");
                        break;
                    }
                case "//wireframe":
                    {
                        if (on)
                        {
                            m.WriteMemory("halo2.exe+468174", "Byte", "0x01", "");
                        }
                        else
                        {
                            m.WriteMemory("halo2.exe+468174", "Byte", "0x00", "");
                        }
                        break;
                    }
                case "//savestate":
                    {
                        if (on)
                        {
                            m.WriteMemory("halo2.exe+482250", "Byte", "0x01", "");
                        }
                        else
                        {
                            m.WriteMemory("halo2.exe+48224F", "Byte", "0x01", "");
                        }
                        break;
                    }
                case "//savestatep2":
                    {
                        if (on)
                        {
                            if (player2Attched) mp2.WriteMemory("halo2.exe+482250", "Byte", "0x01", "");
                        }
                        else
                        {
                            if (player2Attched) mp2.WriteMemory("halo2.exe+48224F", "Byte", "0x01", "");
                        }
                        break;
                    }
                case "//nullcharfilter":
                    {
                        if (on)
                        {
                            m.WriteMemory("halo2.exe+0030D3F8", "Bytes", "0x90 0x90", "");
                        }
                        else
                        {
                            m.WriteMemory("halo2.exe+0030D3F8", "Bytes", "0x74 0x2F", "");
                        }
                        break;
                    }
                case "//customcontrails":
                    {
                        if (on)
                        {
                            customContrails = true;
                        }
                        else
                        {
                            customContrails = false;
                        }
                        break;
                    }
                case "//warpfix":
                    {
                        if (on)
                        {
                            m.WriteMemory("halo2.exe+4F958E", "Bytes", "0x80 0x40 0x00 0x00 0x00 0x40 0x40 0x00 0x00 0x20 0x41", "");
                        }
                        else
                        {
                            m.WriteMemory("halo2.exe+4F958E", "Bytes", "0x20 0x40 0x00 0x00 0x00 0x40 0x40 0x00 0x00 0xF0 0x40", "");
                        }
                        break;
                    }
                case "//capturescene":
                    {
                        CaptureScene();
                        break;
                    }
                case "//startcamera":
                    {
                        await StartCameraRoll();
                        break;
                    }
                case "//stopcamera":
                    {
                        rollCamera = false;
                        break;
                    }
                case "//jumptoscene":
                    {
                        int index = 0;
                        if (string.IsNullOrEmpty(prefix)) index = 0;
                        else index = int.Parse(prefix);
                        JumpCameraToScene(index);
                        break;
                    }
                case "//debugcamera":
                    {
                        ToggleDebugMode();
                        //isCameraToolOpen = true;
                        break;
                    }
                case "//cameracontrol":
                    {
                        if (on)
                        {
                            flyCamControl = true;
                        }
                        else
                        {
                            flyCamControl = false;
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
                            freezeValue = m.ReadFloat((havokaddress - 0x28).ToString("X"));
                            Task.Run(() => StartAsyncFreezeVelocity());

                        }
                        else
                        {
                            FreezeVelocity = false;
                        }
                        break;
                    }
                case "//setvelocity":
                    {
                        float velocity = float.Parse(prefix);
                        m.WriteMemory((havokaddress - 0x28).ToString("X"), "float", (0.6 * velocity).ToString());
                        await Task.Delay(222);
                        m.WriteMemory((havokaddress - 0x28).ToString("X"), "float", velocity.ToString());
                        break;
                    }
                case "//addvelocity":
                    {
                        freezeValue = m.ReadFloat((havokaddress - 0x28).ToString("X"));
                        freezeValue += float.Parse(prefix);
                        m.WriteMemory((havokaddress - 0x28).ToString("X"), "float", (0.6 * freezeValue).ToString());
                        await Task.Delay(222);
                        m.WriteMemory((havokaddress - 0x28).ToString("X"), "float", freezeValue.ToString());
                        break;
                    }
                case "//autocrouch":
                    {
                        float p_z = m.ReadFloat((havokaddress + 0x8).ToString("X"));
                        m.WriteMemory((havokaddress + 0x8).ToString("X"), "float", (p_z - 0.2).ToString());
                        break;
                    }
                case "//freestylemode":
                    {
                        if (on) freeStyleMode = true;
                        else freeStyleMode = false;
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
        private bool FreezeVelocity = true;
        private float freezeValue = 0;
        public async Task StartAsyncFreezeVelocity()
        {
            while (FreezeVelocity)
            {
                m.WriteMemory((havokaddress - 0x28).ToString("X"), "float", freezeValue.ToString());

                await Task.Delay(29);
            }
        }

        private void PrintToConsole(List<string> outPutStrings)
        {
            foreach (string txt in outPutStrings)
            {
                ConsoleOut.AppendText(txt + "\n");
            }
            ConsoleOut.ScrollToEnd();
        }

        private void Disable_Mods(object sender, RoutedEventArgs e)
        {
            if (checkbox_DisableAutoPoking.IsChecked == true)
            {
                modsEnabledMaster = false;
                PrintToConsole("Auto-Poking: Disabled");
            }
            else
            {
                modsEnabledMaster = true;
                PrintToConsole("Auto-Poking: Enabled");
            }
        }
        public static List<string> p_colour = new List<string>();
        //static readonly string[] Scopes = { SheetsService.Scope.Spreadsheets };
        //static readonly string ApplicationName = "Bounce Companion";
        //static readonly string SpreadSheetId = "173PbslIHbVRguvREpgESeHO2e2MKK-haaH7_4xnUdIM";
        //static readonly string sheet = "PlayerData";

        //static SheetsService service;

        //static void GetPlayerData()
        //{
        //    GoogleCredential credential;
        //    using (var stream = new FileStream("pdata.json", FileMode.Open, FileAccess.Read))
        //    {
        //        credential = GoogleCredential.FromStream(stream)
        //            .CreateScoped(Scopes);
        //    }

        //    service = new SheetsService(new Google.Apis.Services.BaseClientService.Initializer()
        //    {
        //        HttpClientInitializer = credential,
        //        ApplicationName = ApplicationName,
        //    });
        //    ReadEntries();
        //}

        //static void ReadEntries()
        //{
        //    string p_colour_data = string.Empty;
        //    var range = $"{sheet}!A1:D30";
        //    var request = service.Spreadsheets.Values.Get(SpreadSheetId, range);

        //    var response = request.Execute();
        //    var values = response.Values;
        //    if (values != null && values.Count > 0)
        //    {
        //        foreach (var row in values)
        //        {
        //            p_colour_data = (string)row[0] + ":";
        //            p_colour_data += (string)row[1] + ":";
        //            p_colour_data += (string)row[2] + ":";
        //            p_colour_data += (string)row[3];
        //            p_colour.Add(p_colour_data);
        //        }
        //    }
        //}
        public string prev_Command = string.Empty;
        private void SendCommands(object sender, RoutedEventArgs e)
        {
            prev_Command = txtbox_ConsoleIn.Text;
            SendCommands();
        }
        private void UIElement_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if ((sender is TextBlock textBlock && textBlock.IsFocused) ||
                (sender is TextBox textBox && textBox.IsFocused))
            {
                if (e.Key == Key.Up)
                {
                    txtbox_ConsoleIn.Text = prev_Command;
                }
            }
        }

        private void UpdateModBttns(object sender, EventArgs e)
        {
            string leftBttn = "N/A"; ;
            string rightBttn = "N/A";

            List<string> result = new List<string>();
            string modName = ComboBox_Mods.SelectionBoxItem.ToString();
            if (modName != "")
            {
                foreach (string line in System.IO.File.ReadLines("Content/Commands/" + modName + ".txt"))
                {
                    string command = CleanString(line);
                    result.Add(command);
                }
                foreach (string command in result)
                {
                    if (command.Contains("LeftButtonText:"))
                    {
                        leftBttn = command.Split(':')[1];
                        if (leftBttn != string.Empty)
                        {
                            Bttn_on.Content = leftBttn;
                        }
                    }
                    if (command.Contains("RightButtonText:"))
                    {
                        rightBttn = command.Split(':')[1];
                        if (rightBttn != string.Empty)
                        {
                            Bttn_off.Content = rightBttn;
                        }
                    }
                    if (command.Contains("SelectedText:"))
                    {
                        rightBttn = command.Split(':')[1];
                        if (rightBttn != string.Empty)
                        {
                            Bttn_off.Content = rightBttn;
                        }
                    }
                }
            }
        }
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int HOTKEY_ID = 9000;
        private const uint VK_NUMLOCK = 0x90;
        private const uint MOD_CTRL = 0x0002;
        private const uint VK_CAPITAL = 0x14;
        private const uint VK_SHIFT = 0x10;
        private const uint VK_F = 0x70;

        HwndSource source;
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            IntPtr handle = new WindowInteropHelper(this).Handle;
            source = HwndSource.FromHwnd(handle);
            source.AddHook(HwndHook);

            RegisterHotKey(handle, HOTKEY_ID, MOD_CTRL, VK_NUMLOCK); //num lock
            RegisterHotKey(handle, HOTKEY_ID, MOD_CTRL, VK_CAPITAL); //caps lock
            RegisterHotKey(handle, HOTKEY_ID, VK_SHIFT, VK_SHIFT); //caps lock
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;
            switch (msg)
            {
                case WM_HOTKEY:
                    switch (wParam.ToInt32())
                    {
                        case HOTKEY_ID:
                            int vkey = (((int)lParam >> 16) & 0xFFFF);
                            if (vkey == VK_CAPITAL)
                            {
                                MessageBox.Show("CapsLock was pressed");
                            }
                            else if (vkey == VK_NUMLOCK)
                            {
                                MessageBox.Show("NumLock was pressed");
                            }
                            else if (vkey == VK_SHIFT)
                            {
                                MessageBox.Show("Shift was pressed");
                            }

                            handled = true;
                            break;
                    }
                    break;
            }
            return IntPtr.Zero;
        }

        protected override void OnClosed(EventArgs e)
        {
            source.RemoveHook(HwndHook);
            source = null;
            UnregisterHotKey(new WindowInteropHelper(this).Handle, HOTKEY_ID);
            base.OnClosed(e);
        }
        //public void WebSocketServer()
        //{
        //    try
        //    {
        //        using (ws = new WebSocket("ws://52.14.59.129:1171/MessageSocket")) // using the "using" clause to ensure the connection does correctly close in the case of a crash or error
        //        {
        //            ws.Connect();
        //            ws.OnMessage += Ws_OnMessageReceived;
        //            PrintToConsole("Successfully connected to BounceBot websocket.");
        //            ws.Send("sm:Test Message");
        //        }
        //    }
        //    catch
        //    {
        //        PrintToConsole("An Error occured while trying to connect to Bouncebot.");
        //    }

        //}


        //private void Ws_OnMessageReceived(object sender, MessageEventArgs e)
        //{
        //    throw new NotImplementedException(); // e.Data = message info
        //}

        public class TimeLine
        {
            public string sceneImage { get; set; }
            public byte[] CameraData { get; set; }
            public int transitionTime { get; set; }
        }

        private void CaptureCameraScene(object sender, RoutedEventArgs e)
        {
            TimeLine cameraScene = new TimeLine();
            _ = GetCameraData(out byte[] cameraPosition);
            cameraScene.CameraData = cameraPosition;
            cameraScene.transitionTime = 1000;
        }
        private float[] c_angle;
        public float[] GetCameraData(out byte[] cameraPosition)
        {
            int c_Address = 0;
            c_Address = m.ReadInt("halo2.exe+0x004D84EC");

            c_angle = new float[12];
            int j = 6;
            for (int i = 0; i < 36; i++)
            {
                if (i < 6)
                {
                    c_angle[i] = m.ReadFloat((c_Address + i * 0x4).ToString("X"));
                }
                if (i == 7) i = 29;
                if (i > 29)
                {
                    c_angle[j] = m.ReadFloat((c_Address + i * 0x4).ToString("X"));
                    j++;
                }

            }
            cameraPosition = m.ReadBytes(c_Address.ToString("X"), 0x14);
            return c_angle;
        }
        public bool debugCameraToggle = false;
        int debugCameraTogglebytes = 0;
        private void ToggleDebugCamera(object sender, RoutedEventArgs e)
        {
            ToggleDebugMode();
        }

        private void ToggleDebugMode()
        {
            debugCameraTogglebytes = m.ReadInt("halo2.exe+0x4A8870");
            if (m.ReadInt("halo2.exe+0x4A84B0") == debugCameraTogglebytes)
            {
                m.WriteMemory("halo2.exe+0x4A849C", "int", "2");
                debugCameraToggle = true;
            }
            else
            {
                m.WriteMemory("halo2.exe+0x4A849C", "int", "0");
                m.WriteMemory("halo2.exe+0x4A84B0", "int", debugCameraTogglebytes.ToString());
                debugCameraToggle = false;
            }
        }

        public int jumpToIndex = 0;
        ImageData selectedImageData = new ImageData();
        private void JumpToSceneButton_Click(object sender, RoutedEventArgs e)
        {
            JumpCameraToScene(-1);
        }

        private void JumpCameraToScene(int index)
        {
            if (index == -1)
            {
                index = jumpToIndex;
            }
            if (imageDataList.Count == 0) return;
            // Retrieve and display the selected image data
            if (index > -1)
            {
                selectedImageData = imageDataList[index];
            }
            MoveCameraPosition(selectedImageData.CameraPosition[0], selectedImageData.CameraPosition[1], selectedImageData.CameraPosition[2], selectedImageData.CameraPosition[3], selectedImageData.CameraPosition[4], selectedImageData.CameraPosition[5]);
        }
        public void MoveCameraPosition(float x, float y, float z, float yaw, float pitch, float roll)
        {
            // Write the camera position and rotation values to memory
            m.WriteMemory(xAddress.ToString("X"), "float", x.ToString());
            m.WriteMemory(yAddress.ToString("X"), "float", y.ToString());
            m.WriteMemory(zAddress.ToString("X"), "float", z.ToString());
            m.WriteMemory(yawAddress.ToString("X"), "float", yaw.ToString());
            m.WriteMemory(pitchAddress.ToString("X"), "float", pitch.ToString());
            m.WriteMemory(rollAddress.ToString("X"), "float", roll.ToString());
        }
        private void ClearTimelineButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Would you like to permanently delete the timeline and its saved images? Select No to only clear the timeline", "Confirmation", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // User chose to clear the timeline
                ClearTimeline();
                ClearAllImages();
            }
            else if (result == MessageBoxResult.No)
            {
                ClearTimeline();
            }
            rollCamera = false;
        }
        private string currentProjectName;
        List<float[]> CameraPositionArrayList = new List<float[]>();
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            CaptureScene();
        }
        private void NewProjectButton_Click(object sender, RoutedEventArgs e)
        {
            // Open the NewProjectWindow
            NewProjectWindow newProjectWindow = new NewProjectWindow(this);
            newProjectWindow.Owner = this;

            if (newProjectWindow.ShowDialog() == true)
            {
                // Retrieve the project name from the window
                string projectName = newProjectWindow.ProjectName;

                if (!string.IsNullOrEmpty(projectName))
                {
                    // Set the current project name
                    currentProjectName = projectName;

                    // Calculate the position below the button
                    double buttonBottom = NewProjectButton.PointToScreen(new Point(0, NewProjectButton.ActualHeight)).Y;
                    double buttonCenterX = NewProjectButton.PointToScreen(new Point(NewProjectButton.ActualWidth / 2, 0)).X;
                    double windowWidth = newProjectWindow.Width;
                    double windowHeight = newProjectWindow.Height;

                    // Set the position of the window
                    newProjectWindow.Left = buttonCenterX - (windowWidth / 2);
                    newProjectWindow.Top = buttonBottom;

                    // Create a folder for the project in the specified path
                    string projectFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", "Halo 2", "Bounce Companion", "Camera Paths", currentProjectName);
                    Directory.CreateDirectory(projectFolderPath);

                    // Now you can use projectFolderPath to save screenshots for this project
                }
            }
        }



        private void CaptureScene()
        {
            try
            {
                // Retrieve data from text boxes
                float[] cameraPosition = GetCameraData(out byte[] cameraPositionArray);
                float transitionTime = float.Parse(GlobalTransitionTimeTextBox.Text);
                CameraPositionArrayList.Add(cameraPosition);

                // Create a new stack panel for each image
                StackPanel stackPanel = new StackPanel();
                stackPanel.Orientation = Orientation.Vertical;
                stackPanel.Width = 200;

                // Create a text block to display the index of the image
                TextBlock textBlock = new TextBlock();
                textBlock.Text = ImageStackPanel.Children.Count.ToString(); // Index starts from 0
                textBlock.HorizontalAlignment = HorizontalAlignment.Center;
                textBlock.Margin = new Thickness(-2);

                // Generate a unique filename for the image
                string imageFileName = Guid.NewGuid().ToString() + ".png";
                // Create an image control
                System.Windows.Controls.Image image = new System.Windows.Controls.Image();
                image.Stretch = Stretch.Uniform;
                image.Margin = new Thickness(5);
                image.MouseLeftButtonUp += Image_MouseLeftButtonUp; // Handle click event

                // Capture screenshot of the game process and use it as the image source
                // Capture screenshot of the game process and use it as the image source
                var bitmapSource = ScreenshotHelper.GetBitmapThumbnailAsync(320, 240);
                if (bitmapSource != null)
                {
                    string imagePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", "Halo 2", "Bounce Companion", "Camera Paths", currentProjectName, imageFileName);

                    // Save the bitmap source to the specified path
                    SaveBitmapSourceToFile((BitmapSource)bitmapSource, imagePath);

                    // Set the image source
                    image.Source = bitmapSource;
                }

                // Add the text block and image to the stack panel
                stackPanel.Children.Add(textBlock);
                stackPanel.Children.Add(image);

                // Add the stack panel to the parent ImageStackPanel
                ImageStackPanel.Children.Add(stackPanel);

                string[] floats = new string[3];
                floats[0] = "0.00";
                floats[1] = "0.00";
                floats[2] = "0.00";

                // Store image data
                imageDataList.Add(new ImageData
                {
                    CameraPosition = cameraPosition,
                    TransitionTime = transitionTime,
                    CameraPositionArray = cameraPositionArray,
                    FacePlayer = false,
                    SpectatePlayer = false,
                    CameraOffsetsArray = floats,
                    SelctedPlayerString = "",
                    ImageFileName = imageFileName

                });
            }
            catch (Exception ex)
            {
                PrintToConsole(ex.ToString());
            }
        }

        private void InsertSceneAtSelected(int selectedIndex, bool insertBefore)
        {
            if (selectedIndex >= 0 && selectedIndex <= imageDataList.Count)
            {
                // Retrieve data from text boxes
                float[] cameraPosition = GetCameraData(out byte[] cameraPositionArray);
                float transitionTime = float.Parse(GlobalTransitionTimeTextBox.Text);
                CameraPositionArrayList.Add(cameraPosition);

                // Generate a unique filename for the image
                string imageFileName = Guid.NewGuid().ToString() + ".png";

                // Create new image data
                var newImageData = new ImageData
                {
                    CameraPosition = cameraPosition,
                    TransitionTime = transitionTime,
                    CameraPositionArray = cameraPositionArray,
                    FacePlayer = false,
                    SpectatePlayer = false,
                    CameraOffsetsArray = new string[3] { "0.00", "0.00", "0.00" },
                    SelctedPlayerString = "",
                    ImageFileName = imageFileName
                };

                // Calculate the index for insertion
                int insertIndex = insertBefore ? selectedIndex : selectedIndex + 1;

                // Insert the new image data at the calculated index
                imageDataList.Insert(insertIndex, newImageData);

                // Update the UI - Add a new stack panel at the calculated index
                InsertStackPanel(insertIndex, imageFileName);

                // Update the text blocks in the UI
                UpdateTextBlocks();

                // Save the new image
                var bitmapSource = ScreenshotHelper.GetBitmapThumbnailAsync(320, 240);
                if (bitmapSource != null)
                {
                    // Use the Documents folder and specific path
                    string imagePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", "Halo 2", "Bounce Companion", "Camera Paths", currentProjectName, imageFileName);

                    // Save the bitmap source to the specified path
                    SaveBitmapSourceToFile((BitmapSource)bitmapSource, imagePath);
                    // Update the image source in the UI
                    UpdateImageSource(insertIndex, bitmapSource);
                }
            }
        }

        private void InsertStackPanel(int index, string imageFileName)
        {
            // Create a new stack panel for each image
            StackPanel stackPanel = new StackPanel();
            stackPanel.Orientation = Orientation.Vertical;
            stackPanel.Width = 200;

            // Create a text block to display the index of the image
            TextBlock textBlock = new TextBlock();
            textBlock.Text = index.ToString(); // Index starts from 0
            textBlock.HorizontalAlignment = HorizontalAlignment.Center;
            textBlock.Margin = new Thickness(-2);

            // Create an image control
            System.Windows.Controls.Image image = new System.Windows.Controls.Image();
            image.Stretch = Stretch.Uniform;
            image.Margin = new Thickness(5);
            image.MouseLeftButtonUp += Image_MouseLeftButtonUp; // Handle click event

            // Set the image source if available
            string imagePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", "Halo 2", "Bounce Companion", "Camera Paths");
            if (File.Exists(imagePath))
            {
                BitmapImage bitmapImage = new BitmapImage(new Uri(imagePath));
                image.Source = bitmapImage;
            }

            // Add the text block and image to the stack panel
            stackPanel.Children.Add(textBlock);
            stackPanel.Children.Add(image);

            // Insert the stack panel at the selected index
            ImageStackPanel.Children.Insert(index, stackPanel);
        }
        private void UpdateImageSource(int index, ImageSource source)
        {
            if (index >= 0 && index < ImageStackPanel.Children.Count)
            {
                var stackPanel = ImageStackPanel.Children[index] as StackPanel;
                var image = stackPanel.Children[1] as System.Windows.Controls.Image;
                image.Source = source;
            }
        }
        private void UpdateTextBlocks()
        {
            for (int i = 0; i < ImageStackPanel.Children.Count; i++)
            {
                var stackPanel = ImageStackPanel.Children[i] as StackPanel;
                var textBlock = stackPanel.Children[0] as TextBlock;
                textBlock.Text = i.ToString();
            }
        }
        // ...
        private void OpenSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            if (settingsWindow == null)
            {
                settingsWindow = new Settings(this);
                settingsWindow.Closed += SettingsWindow_Closed; // Handle window closed event
            }
            if (!settingsWindow.IsActive) settingsWindow.Activate();
            settingsWindow.Show();
        }
        private void SettingsWindow_Closed(object sender, EventArgs e)
        {
            // Clean up and reset the settings window instance
            settingsWindow.Closed -= SettingsWindow_Closed;
            settingsWindow = null;
        }
        public void SaveSceneDataToFile(List<ImageData> imageDataList)
        {
            var dialog = new SaveFileDialog();
            dialog.InitialDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", "Halo 2", "Bounce Companion", "Camera Paths", currentProjectName);
            dialog.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";

            if (dialog.ShowDialog() == true)
            {
                string filePath = dialog.FileName;
                string jsonData = JsonConvert.SerializeObject(imageDataList, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(filePath, jsonData);
            }
        }

        // Modify LoadSceneData to open an open file dialog in the specified folder
        List<ImageData> LoadSceneData()
        {
            var dialog = new OpenFileDialog();
            dialog.InitialDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", "Halo 2", "Bounce Companion", "Camera Paths");
            dialog.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";

            if (dialog.ShowDialog() == true)
            {
                string filePath = dialog.FileName;

                // Get the directory path of the file
                string directoryPath = Path.GetDirectoryName(filePath);

                // Get the name of the last folder in the directory path
                currentProjectName = Path.GetFileName(directoryPath);

                if (File.Exists(filePath))
                {
                    string jsonData = File.ReadAllText(filePath);
                    staackPanel_CameraTool.Visibility = Visibility.Visible;
                    textBox_ProjectName.Text = currentProjectName;  // Set the text to the new currentProjectName
                    return JsonConvert.DeserializeObject<List<ImageData>>(jsonData);
                }
            }

            return new List<ImageData>();
        }

        private void LoadSceneFromFile()
        {
            int i = 2;
            foreach (var imageData in imageDataList)
            {
                if (i >= 10) i = 2;
                // Create a new stack panel
                StackPanel stackPanel = new StackPanel();
                stackPanel.Orientation = Orientation.Vertical;
                stackPanel.Width = 200;

                // Create a text block
                TextBlock textBlock = new TextBlock();
                textBlock.Text = ImageStackPanel.Children.Count.ToString();
                textBlock.HorizontalAlignment = HorizontalAlignment.Center;
                textBlock.Margin = new Thickness(-2);

                // Create an image control
                System.Windows.Controls.Image image = new System.Windows.Controls.Image();
                image.Stretch = Stretch.Uniform;
                image.Margin = new Thickness(5);
                image.MouseLeftButtonUp += Image_MouseLeftButtonUp;
                // Get the Documents folder
                string documentsFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

                // Combine the Documents folder with the additional path


                try
                {
                    string imagePath = Path.Combine(documentsFolder, "My Games", "Halo 2", "Bounce Companion", "Camera Paths", currentProjectName, imageData.ImageFileName);
                    //string imagePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", "Halo 2", "Bounce Companion", "Camera Paths", currentProjectName, imageData.ImageFileName);
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new Uri(imagePath);
                    bitmap.EndInit();

                    image.Source = bitmap;

                    i++;
                }
                catch (Exception ex)
                {
                    // Log or display the exception details
                    string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

                    // Combine the base directory with the image file name
                    string imagePath = Path.Combine(baseDirectory, "BrokenImage.png");
                    image.Source = new BitmapImage(new Uri(imagePath));
                    i++;
                }

                // Add text block and image to stack panel
                stackPanel.Children.Add(textBlock);
                stackPanel.Children.Add(image);

                // Add stack panel to parent ImageStackPanel
                ImageStackPanel.Children.Add(stackPanel);
            }
        }
        private void SaveBitmapSourceToFile(BitmapSource bitmapSource, string fileName)
        {
            // Get the Documents folder
            string documentsFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            // Combine the Documents folder with the additional path
            string folderPath = Path.Combine(documentsFolder, "My Games", "Halo 2", "Bounce Companion", "Camera Paths");

            // Ensure the directory exists
            Directory.CreateDirectory(folderPath);

            // Combine the folder path and file name to get the full file path
            string filePath = Path.Combine(folderPath, fileName);


            // Create the file and save the bitmap
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                BitmapEncoder encoder = new PngBitmapEncoder(); // Change to the appropriate encoder if needed
                encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                encoder.Save(stream);
            }
        }

        private void SaveSceneDataToFile_click(object sender, RoutedEventArgs e)
        {
            SaveSceneDataToFile(imageDataList);
        }

        private void LoadScenarioDataFromFile_click(object sender, RoutedEventArgs e)
        {
            ClearTimeline();
            imageDataList = LoadSceneData();
            LoadSceneFromFile();
        }

        private void InsertSceneBeforeSelected_Click(object sender, RoutedEventArgs e)
        {
            InsertSceneAtSelected(jumpToIndex, true);
        }

        private void InsertSceneAfterSelected_Click(object sender, RoutedEventArgs e)
        {
            InsertSceneAtSelected(jumpToIndex, false);
        }
        private void ClearTimeline()
        {
            // Clear the stack panel and data list
            ImageStackPanel.Children.Clear();
            imageDataList.Clear();

            // Hide the large image and data panel
            LargeImage.Source = null;
            //LargeImageData.Visibility = Visibility.Collapsed;

            CameraPositionArrayList.Clear();
        }

        private void ClearAllImages()
        {
            string imagePathToDelete = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", "Halo 2", "Bounce Companion", "Camera Paths", currentProjectName);
            DeleteAllFilesInFolder(imagePathToDelete);
        }

        public void DeleteAllFilesInFolder(string folderPath)
        {
            try
            {
                // Get all files in the folder
                string[] pngFiles = Directory.GetFiles(folderPath, "*.png");

                // Delete each PNG file
                foreach (string pngFile in pngFiles)
                {
                    File.Delete(pngFile);
                }

                MessageBox.Show("All files deleted successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private float[] ParseFloatArray(string input)
        {
            string realValue = input.Split(':')[1];
            string[] values = realValue.Split(',');
            float[] result = new float[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                result[i] = float.Parse(values[i]);
            }
            return result;
        }
        public System.Windows.Controls.Image clickedImage;
        private void Image_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            clickedImage = (System.Windows.Controls.Image)sender;
            int selectedIndex = -1;

            // Find the index of the clicked image within the ImageStackPanel
            for (int i = 0; i < ImageStackPanel.Children.Count; i++)
            {
                if (ImageStackPanel.Children[i] is StackPanel stackPanel)
                {
                    if (stackPanel.Children.Contains(clickedImage))
                    {
                        selectedIndex = i;
                        jumpToIndex = i;
                        break;
                    }
                }
            }

            if (selectedIndex != -1)
            {
                // Retrieve and display the selected image data
                ImageData selectedImageData = imageDataList[selectedIndex];
                // Use the selectedImageData as needed

                // Display the larger image and data panel
                LargeImage.Source = clickedImage.Source;
                LargeImage.Visibility = Visibility.Visible;
                userTextChanged = false;
                TransitionTimeTextBox.Text = selectedImageData.TransitionTime.ToString();
                CheckBox_OffsetAfterScene.IsChecked = selectedImageData.SpectatePlayer;
                CheckBox_TrackAfterScene.IsChecked = selectedImageData.FacePlayer;

                SceneDataTextBox_X.Text = selectedImageData.CameraPosition[0].ToString();
                SceneDataTextBox_Y.Text = selectedImageData.CameraPosition[1].ToString();
                SceneDataTextBox_Z.Text = selectedImageData.CameraPosition[2].ToString();
                SceneDataTextBox_Yaw.Text = selectedImageData.CameraPosition[3].ToString();
                SceneDataTextBox_Pitch.Text = selectedImageData.CameraPosition[4].ToString();
                SceneDataTextBox_Roll.Text = selectedImageData.CameraPosition[5].ToString();

                TextBox_SceneData_Offset_X.Text = selectedImageData.CameraOffsetsArray[0];
                TextBox_SceneData_Offset_Y.Text = selectedImageData.CameraOffsetsArray[1];
                TextBox_SceneData_Offset_Z.Text = selectedImageData.CameraOffsetsArray[2];
                TextBox_SelectedSceneData.Text = "[" + selectedIndex + "] - Selected Scene Data - ";
                ComboBox_SceneData_Playernames.SelectedItem = selectedImageData.SelctedPlayerString;

                //LargeImageData.Visibility = Visibility.Visible;
            }
        }
        private void UpdateImageDataIndex(bool fullreset)
        {
            if (imageDataList == null || imageDataList.Count == 0) return;
            float transitionTime = float.Parse(TransitionTimeTextBox.Text);
            float[] cameraPosition = GetCameraData(out byte[] cameraPositionArray);
            bool Face = false;
            bool Spectate = false;
            if (jumpToIndex > -1)
            {
                if (CheckBox_TrackAfterScene.IsChecked == true) Face = true;
                if (CheckBox_OffsetAfterScene.IsChecked == true) Spectate = true;
                selectedImageData = imageDataList[jumpToIndex];

                imageDataList[jumpToIndex].TransitionTime = transitionTime;
                if (fullreset) imageDataList[jumpToIndex].CameraPosition = cameraPosition;
                if (fullreset) imageDataList[jumpToIndex].CameraPositionArray = cameraPositionArray;
                imageDataList[jumpToIndex].FacePlayer = Face;
                imageDataList[jumpToIndex].SpectatePlayer = Spectate;
                imageDataList[jumpToIndex].CameraPosition[0] = float.Parse(SceneDataTextBox_X.Text);
                imageDataList[jumpToIndex].CameraPosition[1] = float.Parse(SceneDataTextBox_Y.Text);
                imageDataList[jumpToIndex].CameraPosition[2] = float.Parse(SceneDataTextBox_Z.Text);
                imageDataList[jumpToIndex].CameraPosition[3] = float.Parse(SceneDataTextBox_Yaw.Text);
                imageDataList[jumpToIndex].CameraPosition[4] = float.Parse(SceneDataTextBox_Pitch.Text);
                imageDataList[jumpToIndex].CameraPosition[5] = float.Parse(SceneDataTextBox_Roll.Text);
                imageDataList[jumpToIndex].CameraOffsetsArray[0] = TextBox_SceneData_Offset_X.Text;
                imageDataList[jumpToIndex].CameraOffsetsArray[1] = TextBox_SceneData_Offset_Y.Text;
                imageDataList[jumpToIndex].CameraOffsetsArray[2] = TextBox_SceneData_Offset_Z.Text;
                imageDataList[jumpToIndex].SelctedPlayerString = ComboBox_SceneData_Playernames.Text;
                if (fullreset)
                {
                    var bitmapSource = ScreenshotHelper.GetBitmapThumbnailAsync(320, 240);
                    if (bitmapSource != null)
                    {
                        LargeImage.Source = null;
                        clickedImage.Source = null;
                        LargeImage.Source = bitmapSource;
                        clickedImage.Source = bitmapSource;
                        // Use the Documents folder and specific path
                        string imagePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", "Halo 2", "Bounce Companion", "Camera Paths", currentProjectName, imageDataList[jumpToIndex].ImageFileName);

                        // Save the bitmap source to the specified path
                        SaveBitmapSourceToFile((BitmapSource)bitmapSource, imagePath);
                        // Update the image source in the UI
                        UpdateImageSource(jumpToIndex, bitmapSource);
                    }
                }

            }
        }
        public bool rollCamera = false;
        private async void StartCamera(object sender, RoutedEventArgs e)
        {
            await StartCameraRoll();
        }

        private async Task StartCameraRoll()
        {
            if (!rollCamera)
            {
                try
                {
                    CheckBox_OffsetPlayer.IsChecked = false;
                    Offset_Selected_Player = false;
                    List<float[]> cameraPathList = new List<float[]>();
                    List<float> cameraTransitionTImeList = new List<float>();
                    for (int i = 0; i < imageDataList.Count; i++)
                    {
                        var currentImageData = imageDataList[i];
                        cameraPathList.Add(currentImageData.CameraPosition);
                        cameraTransitionTImeList.Add(currentImageData.TransitionTime);
                    }
                    await Task.Run(async () =>
                    {
                        // This code will run on a separate thread.
                        await MoveCameraSmoothly(cameraPathList, cameraTransitionTImeList);
                    });
                }
                catch (Exception ex) { MessageBox.Show("Error:" + ex.Message); rollCamera = false; }
            }
        }

        public List<float> transitionTimeList = new List<float>();
        private int cameraPositionAddress; // Memory address to write camera position

        private int xAddress;
        private int yAddress;
        private int zAddress;
        private int rollAddress;
        private int pitchAddress;
        private int yawAddress;
        private float currentYaw;
        private float currentPitch;
        private float currentRoll;

        public async Task MoveCameraSmoothly(List<float[]> positions, List<float> fixedSpeeds)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            SetCameraAddresses();
            rollCamera = true;
            double total_time = 0;
            double[] transition_mappings = new double[fixedSpeeds.Count];
            int numPoints = positions.Count;

            int transitionTimes = fixedSpeeds.Count;
            double[] times = new double[transitionTimes];
            double[] positionsX = new double[numPoints];
            double[] positionsY = new double[numPoints];
            double[] positionsZ = new double[numPoints];
            double[] yawValues = new double[numPoints];
            double[] yawValues2 = new double[numPoints];
            double[] pitchValues = new double[numPoints];
            double[] rollValues = new double[numPoints];

            for (int i = 0; i < numPoints; i++)
            {
                times[i] = fixedSpeeds[i]; // Use the provided transition time for each point
                positionsX[i] = positions[i][0];
                positionsY[i] = positions[i][1];
                positionsZ[i] = positions[i][2];
                yawValues[i] = positions[i][3];
                yawValues2[i] = positions[i][3];
                pitchValues[i] = positions[i][4];
                rollValues[i] = positions[i][5];
            }
            for (int i = 0; i < fixedSpeeds.Count; i++)
            {
                transition_mappings[i] = total_time;
                if (i < fixedSpeeds.Count - 1) total_time += GetTransitionTime(positions[i], positions[i + 1], fixedSpeeds[i]);
            }

            IInterpolation SplineX = CubicSpline.InterpolateNatural(transition_mappings, positionsX);
            IInterpolation SplineY = CubicSpline.InterpolateNatural(transition_mappings, positionsY);
            IInterpolation SplineZ = CubicSpline.InterpolateNatural(transition_mappings, positionsZ);

            IInterpolation SplineYaw = CubicSpline.InterpolateNatural(transition_mappings, yawValues);
            IInterpolation SplinePitch = CubicSpline.InterpolateNatural(transition_mappings, pitchValues);
            IInterpolation SplineRoll = CubicSpline.InterpolateNatural(transition_mappings, rollValues);

            double currentTime = 0;
            int c = 0;
            while (currentTime < total_time)
            {
                if (!rollCamera) return;
                double t = currentTime;

                stopwatch.Restart();

                float currentX = (float)SplineX.Interpolate(t);
                float currentY = (float)SplineY.Interpolate(t);
                float currentZ = (float)SplineZ.Interpolate(t);

                float currentYaw = (float)SplineYaw.Interpolate(t);
                float currentPitch = (float)SplinePitch.Interpolate(t);
                float currentRoll = (float)SplineRoll.Interpolate(t);

                if (face_Selected_Player || selectedImageData.FacePlayer)
                {
                    string salt = string.Empty;
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (selectedImageData.FacePlayer)
                        {
                            salt = GetPlayerNameSalt(ComboBox_SceneData_Playernames.Text);
                        }
                        else
                        {
                            salt = GetPlayerNameSalt(ComboBox_Playernames.Text);
                        }
                    });
                    int obj_List_Memory_Address;
                    float p_X, p_Y, p_Z, p_X_Vel, p_Y_Vel, p_Z_Vel, p_pitch, p_Yaw, p_Shields;
                    ReadObjectXYZ(salt, out obj_List_Memory_Address, out p_X, out p_Y, out p_Z, out p_X_Vel, out p_Y_Vel, out p_Z_Vel, out p_pitch, out p_Yaw, out p_Shields);
                    float[] blah = GetCameraData(out byte[] cameraPositionArray1);
                    float[] objectPosition = { p_X, p_Y, p_Z };
                    UpdateCameraAngleinternal(objectPosition, blah, 1f);
                }
                MoveCameraAsyncinternal(currentX, currentY, currentZ, currentYaw, currentPitch, currentRoll);

                await Task.Delay(33);
                currentTime += 16.0;
                c++;
            }

            if (positions.Count > 0)
            {
                float[] finalPosition = positions[positions.Count - 1];

                MoveCameraAsyncinternal(finalPosition[0], finalPosition[1], finalPosition[2], finalPosition[3], finalPosition[4], finalPosition[5]);
            }
            rollCamera = false;
            if (loopCamera)
            {
                //MoveCameraAsyncinternal((float)positionsX[0], (float)positionsX[0], (float)positionsX[0], (float)yawValues[0], (float)pitchValues[0], (float)rollValues[0]);
                //await Task.Delay(loopDelayTime);
                await Application.Current.Dispatcher.Invoke(async () =>
                {
                    await StartCameraRoll();
                });
            }
            void MoveCameraAsyncinternal(float x, float y, float z, float yaw, float pitch, float roll)
            {
                if (selectedImageData.FacePlayer) face_Selected_Player = true;
                // Write the camera position and rotation values to memory
                m.WriteMemory(xAddress.ToString("X"), "float", x.ToString());
                m.WriteMemory(yAddress.ToString("X"), "float", y.ToString());
                m.WriteMemory(zAddress.ToString("X"), "float", z.ToString());

                if (!face_Selected_Player) // if we are not facing the player, Set the camera anlge
                {
                    m.WriteMemory(yawAddress.ToString("X"), "float", yaw.ToString());
                    m.WriteMemory(pitchAddress.ToString("X"), "float", pitch.ToString());
                    m.WriteMemory(rollAddress.ToString("X"), "float", roll.ToString());
                }
            }
            void UpdateCameraAngleinternal(float[] objectPosition, float[] a_CameraPosition, float t)
            {
                // Calculate the direction vector from the camera to the object
                float dx = objectPosition[0] - a_CameraPosition[0];
                float dy = objectPosition[1] - a_CameraPosition[1];
                float dz = objectPosition[2] - a_CameraPosition[2];

                // Calculate the target yaw angle (in radians)
                float targetYaw = (float)Math.Atan2(dy, dx);

                // Calculate the shortest angular distance between the current and target yaw angles
                float yawDifference = targetYaw - a_CameraPosition[3];
                if (yawDifference > Math.PI)
                    yawDifference -= 2 * (float)Math.PI;
                else if (yawDifference < -Math.PI)
                    yawDifference += 2 * (float)Math.PI;

                // Smoothly interpolate the yaw angle
                float smoothedYaw = a_CameraPosition[3] + yawDifference * t; // Adjust the interpolation factor (0.1f) as desired

                // Calculate the target pitch angle (in radians)
                float targetPitch = (float)Math.Atan2(dz, Math.Sqrt(dx * dx + dy * dy));

                // Calculate the shortest angular distance between the current and target pitch angles
                float pitchDifference = targetPitch - a_CameraPosition[4];
                if (pitchDifference > Math.PI)
                    pitchDifference -= 2 * (float)Math.PI;
                else if (pitchDifference < -Math.PI)
                    pitchDifference += 2 * (float)Math.PI;

                // Smoothly interpolate the pitch angle
                float smoothedPitch = a_CameraPosition[4] + pitchDifference * t; // Adjust the interpolation factor (0.1f) as desired

                // Convert the yaw, pitch, and roll angles to radians
                float yawRad = smoothedYaw;
                float pitchRad = smoothedPitch;

                // Write the new camera angle to the memory address
                m.WriteMemory(yawAddress.ToString("X"), "float", yawRad.ToString());
                m.WriteMemory(pitchAddress.ToString("X"), "float", pitchRad.ToString());

            }
        }
        public void DeleteImage(string imagePath)
        {
            try
            {
                // Check if the file exists before attempting to delete
                if (File.Exists(imagePath))
                {
                    File.Delete(imagePath);
                    MessageBox.Show("Image deleted successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Image not found at the specified path.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void UpdateUICameraCoordinates(float currentX, float currentY, float currentZ, float currentYaw, float currentPitch, float currentRoll)
        {
            SceneDataTextBox_C_X.Text = Math.Round(currentX, 2).ToString();
            SceneDataTextBox_C_Y.Text = Math.Round(currentY, 2).ToString();
            SceneDataTextBox_C_Z.Text = Math.Round(currentZ, 2).ToString();
            SceneDataTextBox_C_Yaw.Text = Math.Round(currentYaw, 2).ToString();
            SceneDataTextBox_C_Pitch.Text = Math.Round(currentPitch, 2).ToString();
            SceneDataTextBox_C_Roll.Text = Math.Round(currentRoll, 2).ToString();
        }

        private static double GetTransitionTime(float[] currentPosition, float[] nextPosition, float TransitionTime)
        {
            float maxDelta = Math.Max(Math.Abs(currentPosition[0] - nextPosition[0]),
                                             Math.Max(Math.Abs(currentPosition[1] - nextPosition[1]),
                                                      Math.Abs(currentPosition[2] - nextPosition[2])));
            float fixedSpeed = TransitionTime; // Set your desired fixed speed here (units per second)
            return maxDelta / fixedSpeed * 1000;
        }
        private float InterpolateAngle(float startAngle, float endAngle, double t)
        {
            float diff = endAngle - startAngle;
            if (Math.Abs(diff) > Math.PI) // If the difference is larger than 180 degrees, take the shortest path
            {
                if (diff > 0)
                    endAngle -= 2 * (float)Math.PI;
                else
                    endAngle += 2 * (float)Math.PI;
            }
            return startAngle + (float)t * (endAngle - startAngle);
        }
        public bool face_Selected_Player = false;

        private void UpdateScene_Click(object sender, RoutedEventArgs e)
        {
            UpdateImageDataIndex(false);
        }
        public static class Mathf
        {
            public static float Cos(float radians)
            {
                return (float)Math.Cos(radians);
            }

            public static float Sin(float radians)
            {
                return (float)Math.Sin(radians);
            }

            public static float Acos(float value)
            {
                return (float)Math.Acos(value);
            }
        }



        private void UpdateScene_Click(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            UpdateImageDataIndex(false);

        }

        private void ResetScene_Click(object sender, RoutedEventArgs e)
        {
            UpdateImageDataIndex(true);
        }

        private void StopCamera(object sender, RoutedEventArgs e)
        {
            rollCamera = false;
        }


        private void DeleteScene(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("This will delete your selected scene, do you want to continue?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                if (ImageStackPanel.Children.Count == 0) return;
                ImageData imageData = imageDataList[jumpToIndex];
                // Remove the image from the stack panel
                ImageStackPanel.Children.RemoveAt(jumpToIndex);
                // Remove the corresponding data from the list
                imageDataList.RemoveAt(jumpToIndex);
                LargeImage.Visibility = Visibility.Collapsed;
                string imagePathToDelete = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", "Halo 2", "Bounce Companion", "Camera Paths", currentProjectName, imageData.ImageFileName);
                DeleteImage(imagePathToDelete);

            }
        }

        private static float ToDegrees(float radians)
        {
            return radians * (180f / (float)Math.PI);
        }
        public void UpdateCameraAngle(float[] objectPosition, float[] a_CameraPosition, float t)
        {
            // Calculate the direction vector from the camera to the object
            float dx = objectPosition[0] - a_CameraPosition[0];
            float dy = objectPosition[1] - a_CameraPosition[1];
            float dz = objectPosition[2] - a_CameraPosition[2] + 0.8f;

            // Calculate the target yaw angle (in radians)
            float targetYaw = (float)Math.Atan2(dy, dx);

            // Calculate the shortest angular distance between the current and target yaw angles
            float yawDifference = targetYaw - a_CameraPosition[3];
            if (yawDifference > Math.PI)
                yawDifference -= 2 * (float)Math.PI;
            else if (yawDifference < -Math.PI)
                yawDifference += 2 * (float)Math.PI;

            // Smoothly interpolate the yaw angle
            float smoothedYaw = a_CameraPosition[3] + yawDifference * t; // Adjust the interpolation factor (0.1f) as desired

            // Calculate the target pitch angle (in radians)
            float targetPitch = (float)Math.Atan2(dz, Math.Sqrt(dx * dx + dy * dy));

            // Calculate the shortest angular distance between the current and target pitch angles
            float pitchDifference = targetPitch - a_CameraPosition[4];
            if (pitchDifference > Math.PI)
                pitchDifference -= 2 * (float)Math.PI;
            else if (pitchDifference < -Math.PI)
                pitchDifference += 2 * (float)Math.PI;

            // Smoothly interpolate the pitch angle
            float smoothedPitch = a_CameraPosition[4] + pitchDifference * t; // Adjust the interpolation factor (0.1f) as desired

            // Convert the yaw, pitch, and roll angles to radians
            float yawRad = smoothedYaw;
            float pitchRad = smoothedPitch;

            // Write the new camera angle to the memory address
            m.WriteMemory(yawAddress.ToString("X"), "float", yawRad.ToString());
            m.WriteMemory(pitchAddress.ToString("X"), "float", pitchRad.ToString());

        }


        //public void UpdateCameraAngle(float[] objectPosition, float[] cameraPositionOrientation)
        //{
        //    // Calculate the direction vector from the camera to the object
        //    float dx = objectPosition[0] - cameraPositionOrientation[0];
        //    float dy = objectPosition[1] - cameraPositionOrientation[1];
        //    float dz = objectPosition[2] - cameraPositionOrientation[2];

        //    // Calculate the yaw angle (in radians)
        //    float yaw = (float)Math.Atan2(dy, dx);

        //    // Calculate the pitch angle (in radians)
        //    float pitch = (float)Math.Atan2(dz, Math.Sqrt(dx * dx + dy * dy));

        //    // Convert the yaw, pitch, and roll angles to radians
        //    float yawRad = cameraPositionOrientation[3] * (float)Math.PI / 180.0f;
        //    float pitchRad = pitch;
        //    float rollRad = cameraPositionOrientation[5] * (float)Math.PI / 180.0f;


        //    // Calculate the new camera angle (in radians)
        //    float newCameraYaw = yawRad + yaw;
        //    float newCameraPitch = pitchRad;

        //    // Write the new camera angle to the memory address
        //    IntPtr cameraAngleAddress = new IntPtr(0x12345678); // Replace with your actual memory address
        //    m.WriteMemory(rollAddress.ToString("X"), "float", newCameraYaw.ToString());
        //    m.WriteMemory((rollAddress + 0x4).ToString("X"), "float", newCameraPitch.ToString());
        //    m.WriteMemory((rollAddress + 0x8).ToString("X"), "float", rollRad.ToString());
        //}



        public bool ContainsOnlyNumbersOrDecimals(string input)
        {
            if (!string.IsNullOrEmpty(input))
            {
                string pattern = @"^[+-]?\d+(\.\d+)?$";
                Regex regex = new Regex(pattern);
                return regex.IsMatch(input);
            }
            return false;
        }
        private async Task MoveCameraPositionAsync(float yAxisInput, float xAxisInput, float controllerXInput, float controllerYInput, float leftTrigger, float rightTrigger, bool leftShoulderPressed, bool rightShoulderPressed)
        {
            float cameraX;
            float cameraY;
            float cameraZ;
            float cameraYaw;
            float cameraPitch;
            float cameraRoll = 0;
            if (noClip)
            {
                string salt = string.Empty;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    salt = ComboBox_Playernames.Text.Split(':')[1];
                });


                int obj_List_Memory_Address;
                float p_X, p_Y, p_Z, p_X_Vel, p_Y_Vel, p_Z_Vel, p_pitch, p_Yaw, p_Shields;
                ReadObjectXYZ(salt, out obj_List_Memory_Address, out p_X, out p_Y, out p_Z, out p_X_Vel, out p_Y_Vel, out p_Z_Vel, out p_pitch, out p_Yaw, out p_Shields);

                cameraX = p_X;
                cameraY = p_Y;
                cameraZ = p_Z;
                cameraYaw = p_Yaw;
                cameraPitch = p_pitch;
            }
            else
            {
                SetCameraAddresses();
                float[] cameraPosition = GetCameraData(out byte[] cameraPositionArray1);
                cameraX = cameraPosition[0];
                cameraY = cameraPosition[1];
                cameraZ = cameraPosition[2];
                cameraYaw = cameraPosition[3];
                cameraPitch = cameraPosition[4];
                cameraRoll = cameraPosition[5];
            }

            // Apply the controller input to the camera yaw, pitch, and roll
            cameraYaw -= controllerXInput * (c_turnSpeed / 100);
            cameraPitch -= controllerYInput * (c_pitchSpeed / 100);
            cameraRoll += (rightShoulderPressed ? 1 : 0) * (c_rollSpeed / 100) - (leftShoulderPressed ? 1 : 0) * (c_rollSpeed / 100);

            //// Normalize the yaw value to stay within the range of -π to π
            //if (cameraYaw < -Math.PI)
            //    cameraYaw += 2 * (float)Math.PI;
            //else if (cameraYaw > Math.PI)
            //    cameraYaw -= 2 * (float)Math.PI;

            // Limit the pitch value to stay within a desired range (e.g., -π/2 to π/2 for looking up and down)
            //cameraPitch = (float)Math.Clamp(cameraPitch, -Math.PI / 2, Math.PI / 2);

            // Calculate movement based on left joystick inputs
            float moveX = xAxisInput * (c_moveSpeed / 100);
            float moveY = yAxisInput * (c_moveSpeed / 100);

            // Calculate the forward vector based on the camera's yaw
            float forwardX = (float)Math.Sin(cameraYaw);
            float forwardY = (float)Math.Cos(cameraYaw);

            // Update the camera position, yaw, pitch, and height based on input
            cameraX += forwardX * moveY - forwardY * moveX;
            cameraY -= forwardY * moveY + forwardX * moveX;
            cameraZ -= (leftTrigger - rightTrigger) * (c_heightSpeed / 100);

            // Write the updated camera position to memory

            MoveCameraAsync(cameraX, cameraY, cameraZ, cameraYaw, cameraPitch, cameraRoll);

            void MoveCameraAsync(float x, float y, float z, float yaw, float pitch, float roll)
            {
                int baseCoordinate = 0;
                // Calculate the offsets for XYZ position and rotation values based on your game's memory structure
                if (noClip)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        baseCoordinate = int.Parse(settingsWindow.Textbox_P_Address_replay.Text);
                    });
                }
                else baseCoordinate = xAddress;
                // Write the camera position and rotation values to memory
                m.WriteMemory((baseCoordinate).ToString("X"), "float", x.ToString());
                m.WriteMemory((baseCoordinate + 4).ToString("X"), "float", y.ToString());
                m.WriteMemory((baseCoordinate + 8).ToString("X"), "float", z.ToString());

                if (!face_Selected_Player || !selectedImageData.FacePlayer) // if we are not facing the player, Set the camera anlge
                {
                    if (noClip) return;
                    m.WriteMemory(yawAddress.ToString("X"), "float", yaw.ToString());
                    m.WriteMemory(pitchAddress.ToString("X"), "float", pitch.ToString());
                    m.WriteMemory(rollAddress.ToString("X"), "float", roll.ToString());
                }
            }

        }
        public bool noClip = false;


        public void HandleKeyboardInput(Key key)
        {
            float xAxisInput = 0;
            float yAxisInput = 0;

            switch (key)
            {
                case Key.W:
                    yAxisInput = 1;
                    break;
                case Key.S:
                    yAxisInput = -1;
                    break;
                case Key.A:
                    xAxisInput = -1;
                    break;
                case Key.D:
                    xAxisInput = 1;
                    break;
            }

            //MoveCamera(xAxisInput, yAxisInput);
        }

        public void HandleMouseInput(float deltaX, float deltaY)
        {
            float xAxisInput = deltaX;
            float yAxisInput = deltaY;

            //MoveCamera(xAxisInput, yAxisInput);
        }
        private void TabItem_CameraTool_GotFocus(object sender, RoutedEventArgs e)
        {
            // The "Camera Tool" tab has been selected
            flyCamControl = true;
        }

        private void TabItem_CameraTool_LostFocus(object sender, RoutedEventArgs e)
        {
            // The "Camera Tool" tab has been unselected
            flyCamControl = false;
        }
        private const float DeadbandThreshold = 0.5f;
        private bool flyCamControl = false;
        Dictionary<ControllerKey, bool> keyStates = new Dictionary<ControllerKey, bool>();


        public enum ControllerKey
        {
            RT,
            X,
            RB,
            Start,
            RightStick,
            A,
            Y,
            B,
            LB,
            LT,
            LeftStick,
            DPadUp,
            DPadLeft,
            DPadRight,
            DPadDown,
            Back
        }
        private async Task HandleXboxControllerInputAsync()
        {
            await Task.Run(async () =>
            {
                while (true)
                {
                    if (xboxController.IsConnected)
                    {
                        State controllerState = xboxController.GetState();

                        if (controllerState.PacketNumber != 0)
                        {
                            float xAxisInput = ApplyDeadzone(controllerState.Gamepad.LeftThumbX / (float)short.MaxValue, 0.2f);
                            float yAxisInput = -ApplyDeadzone(controllerState.Gamepad.LeftThumbY / (float)short.MaxValue, 0.2f); // Invert Y-axis for camera movement
                            float yawAxisInput = ApplyDeadzone(controllerState.Gamepad.RightThumbX / (float)short.MaxValue, 0.2f);
                            float pitchAxisInput = -ApplyDeadzone(controllerState.Gamepad.RightThumbY / (float)short.MaxValue, 0.2f);
                            float leftTrigger = ApplyDeadzone(controllerState.Gamepad.LeftTrigger / (float)byte.MaxValue, 0.1f);
                            float rightTrigger = ApplyDeadzone(controllerState.Gamepad.RightTrigger / (float)byte.MaxValue, 0.1f);

                            bool leftShoulderPressed = controllerState.Gamepad.Buttons.HasFlag(GamepadButtonFlags.LeftShoulder);
                            bool rightShoulderPressed = controllerState.Gamepad.Buttons.HasFlag(GamepadButtonFlags.RightShoulder);
                            if (xAxisInput != 0 || yAxisInput != 0 || yawAxisInput != 0 || pitchAxisInput != 0 || leftTrigger != 0 || rightTrigger != 0 || leftShoulderPressed || rightShoulderPressed)
                            {
                                if (!rollCamera && flyCamControl)
                                {
                                    await MoveCameraPositionAsync(xAxisInput, yAxisInput, yawAxisInput, pitchAxisInput, leftTrigger, rightTrigger, leftShoulderPressed, rightShoulderPressed);
                                }
                            }
                            // Check D-pad and button presses
                            if (controllerState.Gamepad.Buttons.HasFlag(GamepadButtonFlags.DPadUp))
                            {
                                //Application.Current.Dispatcher.Invoke(() =>
                                //{
                                //    CallDllFunction();
                                //});
                                keyStates[ControllerKey.DPadUp] = true;
                            }
                            else keyStates[ControllerKey.DPadUp] = false;

                            if (controllerState.Gamepad.Buttons.HasFlag(GamepadButtonFlags.DPadDown))
                            {
                                // D-pad Down button is pressed
                                // Perform your action here
                                keyStates[ControllerKey.DPadDown] = true;
                            }
                            else keyStates[ControllerKey.DPadDown] = false;

                            if (controllerState.Gamepad.Buttons.HasFlag(GamepadButtonFlags.DPadLeft))
                            {
                                // D-pad Left button is pressed
                                // Perform your action here
                                keyStates[ControllerKey.DPadLeft] = true;
                            }
                            else keyStates[ControllerKey.DPadLeft] = false;

                            if (controllerState.Gamepad.Buttons.HasFlag(GamepadButtonFlags.DPadRight))
                            {
                                // D-pad Right button is pressed
                                // Perform your action here
                                keyStates[ControllerKey.DPadRight] = true;
                            }
                            else keyStates[ControllerKey.DPadRight] = false;

                            if (controllerState.Gamepad.Buttons.HasFlag(GamepadButtonFlags.Start))
                            {
                                //Application.Current.Dispatcher.Invoke(() =>
                                //{
                                //    _ = StartCameraRoll();
                                //});
                                keyStates[ControllerKey.Start] = true;
                            }
                            else keyStates[ControllerKey.Start] = false;

                            if (controllerState.Gamepad.Buttons.HasFlag(GamepadButtonFlags.Back))
                            {
                                //Application.Current.Dispatcher.Invoke(() =>
                                //{
                                //    rollCamera = false;
                                //});
                                keyStates[ControllerKey.Back] = true;
                            }
                            else keyStates[ControllerKey.Back] = false;

                            if (controllerState.Gamepad.Buttons.HasFlag(GamepadButtonFlags.A))
                            {
                                // A button is pressed
                                // Perform your action here
                                keyStates[ControllerKey.A] = true;
                            }
                            else keyStates[ControllerKey.A] = false;

                            if (controllerState.Gamepad.Buttons.HasFlag(GamepadButtonFlags.X))
                            {
                                //Application.Current.Dispatcher.Invoke(() =>
                                //{
                                //    CaptureScene();
                                //});
                                keyStates[ControllerKey.X] = true;
                            }
                            else keyStates[ControllerKey.X] = false;

                            if (controllerState.Gamepad.Buttons.HasFlag(GamepadButtonFlags.B))
                            {
                                keyStates[ControllerKey.B] = true;
                            }
                            else keyStates[ControllerKey.B] = false;

                            if (controllerState.Gamepad.Buttons.HasFlag(GamepadButtonFlags.Y))
                            {
                                //Application.Current.Dispatcher.Invoke(() =>
                                //{
                                //    replaySystem.StartRecording();
                                //});
                                keyStates[ControllerKey.Y] = true;
                            }
                            else keyStates[ControllerKey.Y] = false;

                            if (controllerState.Gamepad.Buttons.HasFlag(GamepadButtonFlags.LeftShoulder))
                            {
                                keyStates[ControllerKey.LB] = true;
                            }
                            else keyStates[ControllerKey.LB] = false;

                            if (controllerState.Gamepad.Buttons.HasFlag(GamepadButtonFlags.RightShoulder))
                            {
                                // D-pad Down button is pressed
                                // Perform your action here
                                keyStates[ControllerKey.RB] = true;
                            }
                            else keyStates[ControllerKey.RB] = false;
                            if (controllerState.Gamepad.Buttons.HasFlag(GamepadButtonFlags.LeftThumb))
                            {
                                // D-pad Down button is pressed
                                // Perform your action here
                                keyStates[ControllerKey.LeftStick] = true;
                            }
                            else keyStates[ControllerKey.LeftStick] = false;

                            if (controllerState.Gamepad.Buttons.HasFlag(GamepadButtonFlags.RightThumb))
                            {
                                // D-pad Down button is pressed
                                // Perform your action here
                                keyStates[ControllerKey.RightStick] = true;
                            }
                            else keyStates[ControllerKey.RightStick] = false;
                            ControllerKey[] activeKeys = keyStates.Where(kv => kv.Value).Select(kv => kv.Key).ToArray();

                            if (activeKeys.Length > 0) // Check if any element in trueBoolsArray is true
                            {
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    CheckForKeyBindActivation(activeKeys);
                                });
                            }
                        }

                    }
                    Thread.Sleep(33); // Delay between controller state checks
                }
            });
        }
        private Dictionary<string, bool> toggleStateDictionary = new Dictionary<string, bool>();
        public List<C_List_Info> c_List_Infos_L = new List<C_List_Info>();
        private bool isTimerRunning = false;
        private void CheckForKeyBindActivation(ControllerKey[] activeKeys)
        {
            foreach (var obj in c_List_Infos_L)
            {
                var c_keybinds = obj.c_KeyBind;
                string command = obj.c_Name;
                string actionState = obj.c_action;
                bool togglestate = obj.c_toggle;
                bool action = false;

                // Check if any keybind contains a pair of active keys
                bool keybindActive = c_keybinds.Any(keybind =>
                {
                    if (c_keybinds.Length == 1)
                    {
                        return activeKeys.Contains(ParseControllerKey(c_keybinds[0]));
                    }
                    else if (c_keybinds.Length == 2)
                    {
                        return activeKeys.Contains(ParseControllerKey(c_keybinds[0])) &&
                            activeKeys.Contains(ParseControllerKey(c_keybinds[1]));
                    }
                    return false; // No active keys in this keybind
                });

                if (actionState == "Enable Mod") action = true;
                if (actionState == "Disable Mod") action = false;
                if (actionState == "Toggle Mod") action = !togglestate; // Toggle the state

                if (keybindActive && !isTimerRunning)
                {
                    ApplyMods(action, command);
                    obj.c_toggle = action;
                    toggleTimer.Change(500, Timeout.Infinite); // Adjust the delay as needed (e.g., 1000 milliseconds)
                    isTimerRunning = true;
                }
            }
        }
        private void ToggleCallback(object state)
        {
            // Reset all toggle states
            toggleStateDictionary.Clear();
            isTimerRunning = false;
        }
        // Define a method to parse a string into a ControllerKey enum
        private ControllerKey ParseControllerKey(string key)
        {
            if (Enum.TryParse<ControllerKey>(key, out ControllerKey controllerKey))
            {
                return controllerKey;
            }
            return ControllerKey.RT; // Default to a specific key if parsing fails
        }

        private float ApplyDeadzone(float input, float deadbandThreshold)
        {
            if (Math.Abs(input) < deadbandThreshold)
                input = 0f;

            return input;
        }


        private void LoadNamesToComboBox(object sender, EventArgs e)
        {
            ComboBox_Playernames.Items.Clear();
            ComboBox_SceneData_Playernames.Items.Clear();
            p_List.Clear();
            UpdatePlayerList();
            foreach (KeyValuePair<string, p_List_Info> key in p_List)
            {
                p_List_Info info = key.Value;
                ComboBox_Playernames.Items.Add(info.p_Name.ToString());
                ComboBox_SceneData_Playernames.Items.Add(info.p_Name.ToString());
            }
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
        }
        private void CheckBoxChecked_TrackPlayer(object sender, RoutedEventArgs e)
        {
            if (CheckBox_TrackPlayer.IsChecked == true)
            {
                //CheckBox_OffsetPlayer.IsChecked = false;
                //Offset_Selected_Player = false;
                face_Selected_Player = true;
            }
            else
            {
                face_Selected_Player = false;
            }
        }
        public bool Offset_Selected_Player = false;
        public bool CTO_OffsetPlayer = false;
        private async void CheckBoxChecked_OffsetPlayer(object sender, RoutedEventArgs e)
        {
            if (CheckBox_OffsetPlayer.IsChecked == true)
            {
                if (ComboBox_Playernames.SelectedItem == null)
                {
                    MessageBox.Show("Please select a player from the Select Player Dropdown Box.");
                    //CheckBox_OffsetPlayer.IsChecked = false;
                    return;
                }
                //CheckBox_TrackPlayer.IsChecked = false;
                CTO_OffsetPlayer = true;
                Offset_Selected_Player = true;
                string salt = GetPlayerNameSalt(ComboBox_Playernames.Text);
                await OffsetObjectPositionContinuous(salt);
            }
            else
            {
                Offset_Selected_Player = false;
            }
        }
        //public async Task OffsetObjectPositionContinuous(string salt)
        //{
        //    while (Offset_Selected_Player)
        //    {
        //        try
        //        {
        //            int obj_List_Memory_Address;
        //            float p_X, p_Y, p_Z, p_X_Vel, p_Y_Vel, p_Z_Vel, p_pitch, p_Yaw, p_Shields;
        //            ReadObjectXYZ(salt, out obj_List_Memory_Address, out p_X, out p_Y, out p_Z, out p_X_Vel, out p_Y_Vel, out p_Z_Vel, out p_pitch,out p_Yaw, out p_Shields);
        //            float[] cameraPosition = GetCameraData(out byte[] cameraPositionArray1);
        //            float[] objectPosition = { p_X, p_Y, p_Z };
        //            if (face_Selected_Player)
        //            {
        //                UpdateCameraAngle(objectPosition, cameraPosition, 1f);
        //            }

        //            await OffsetObjectPosition(objectPosition, cameraPosition);
        //            await Task.Delay(32); // Delay between offset calculations (adjust as needed)
        //        }
        //        catch
        //        { MessageBox.Show("Error in OffsetObjectPositionContinuous"); Offset_Selected_Player = false; CheckBox_OffsetPlayer.IsChecked = false; }
        //    }
        //}

        public bool SpectateCamera = false;
        public async Task OffsetObjectPositionContinuous(string salt)
        {
            while (Offset_Selected_Player)
            {
                try
                {
                    int obj_List_Memory_Address;
                    float p_X, p_Y, p_Z, p_X_Vel, p_Y_Vel, p_Z_Vel, p_Yaw, p_pitch, p_Shields;
                    ReadObjectXYZ(salt, out obj_List_Memory_Address, out p_X, out p_Y, out p_Z, out p_X_Vel, out p_Y_Vel, out p_Z_Vel, out p_Yaw, out p_pitch, out p_Shields);
                    float[] cameraPosition = GetCameraData(out byte[] cameraPositionArray1);
                    float[] objectPosition = { p_X, p_Y, p_Z };
                    if (face_Selected_Player)
                    {
                        UpdateCameraAngle(objectPosition, cameraPosition, 1f);
                    }

                    await OffsetObjectPosition(objectPosition, cameraPosition);
                    await Task.Delay(32); // Delay between offset calculations (adjust as needed)
                }
                catch
                { MessageBox.Show("Error in OffsetObjectPositionContinuous"); Offset_Selected_Player = false; CheckBox_OffsetPlayer.IsChecked = false; }
            }
        }

        private async Task OffsetObjectPosition(float[] objectPosition, float[] cameraPosition)
        {
            float[] offsetPosition = new float[3];
            // Calculate the offset position
            if (!CTO_OffsetPlayer)
            {
                selectedImageData = imageDataList[jumpToIndex];
                if (selectedImageData.SpectatePlayer)
                {
                    offsetPosition[0] = objectPosition[0] + RemoveNonDigitChars(selectedImageData.CameraOffsetsArray[0]);
                    offsetPosition[1] = objectPosition[1] + RemoveNonDigitChars(selectedImageData.CameraOffsetsArray[1]);
                    offsetPosition[2] = objectPosition[2] + RemoveNonDigitChars(selectedImageData.CameraOffsetsArray[2]);
                }
            }
            else
            {
                offsetPosition[0] = objectPosition[0] + RemoveNonDigitChars(TextBox_Offset_X.Text);
                offsetPosition[1] = objectPosition[1] + RemoveNonDigitChars(TextBox_Offset_Y.Text);
                offsetPosition[2] = objectPosition[2] + RemoveNonDigitChars(TextBox_Offset_Z.Text);
            }

            // Use memory.dll or any other appropriate library to write the offset position to memory
            await WriteOffsetPositionToMemory(offsetPosition);
        }
        //private async Task OffsetObjectPosition(float[] objectPosition, float[] cameraPosition)
        //{
        //    float[] offsetPosition = new float[3];
        //    // Calculate the offset position
        //    if (!CTO_OffsetPlayer)
        //    {
        //        selectedImageData = imageDataList[jumpToIndex];
        //        if (selectedImageData.SpectatePlayer)
        //        {
        //            offsetPosition[0] = objectPosition[0] + RemoveNonDigitChars(selectedImageData.CameraOffsetsArray[0]);
        //            offsetPosition[1] = objectPosition[1] + RemoveNonDigitChars(selectedImageData.CameraOffsetsArray[1]);
        //            offsetPosition[2] = objectPosition[2] + RemoveNonDigitChars(selectedImageData.CameraOffsetsArray[2]);
        //        }
        //    }
        //    else
        //    {
        //        offsetPosition[0] = objectPosition[0] + RemoveNonDigitChars(TextBox_Offset_X.Text);
        //        offsetPosition[1] = objectPosition[1] + RemoveNonDigitChars(TextBox_Offset_Y.Text);
        //        offsetPosition[2] = objectPosition[2] + RemoveNonDigitChars(TextBox_Offset_Z.Text);
        //    }

        //    // Use memory.dll or any other appropriate library to write the offset position to memory
        //    await WriteOffsetPositionToMemory(offsetPosition);
        //}
        public float RemoveNonDigitChars(string input)
        {
            // Remove any non-digit characters from the input string using regular expressions
            string cleanedString = Regex.Replace(input, "[^0-9.-]", "");

            // Ensure the cleaned string is a valid float format
            float floatValue;
            if (!float.TryParse(cleanedString, out floatValue))
            {
                // Handle invalid float format if necessary
                throw new ArgumentException("Invalid float format");
            }
            if (string.IsNullOrEmpty(cleanedString)) cleanedString = "1";
            return float.Parse(cleanedString);
        }


        private Task WriteOffsetPositionToMemory(float[] offsetPosition)
        {
            m.WriteMemory(xAddress.ToString("X"), "float", offsetPosition[0].ToString());
            m.WriteMemory(yAddress.ToString("X"), "float", offsetPosition[1].ToString());
            m.WriteMemory(zAddress.ToString("X"), "float", offsetPosition[2].ToString());

            return Task.CompletedTask;
        }
        private async Task InitializeXboxController()
        {
            xboxController = new Controller(UserIndex.One);

            while (true)
            {
                await HandleXboxControllerInputAsync();
                await Task.Delay(1); // Delay between controller input handling
            }
        }



        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            HandleKeyboardInput(e.Key);
        }

        private void MainWindow_MouseMove(object sender, MouseEventArgs e)
        {
            float deltaX = (float)e.GetPosition(this).X - (float)this.ActualWidth / 2;
            float deltaY = (float)e.GetPosition(this).Y - (float)this.ActualHeight / 2;

            HandleMouseInput(deltaX, deltaY);

            // Reset mouse position to the center of the window
            NativeMethods.GetCursorPos(out POINT cursorPos);
            NativeMethods.SetCursorPos((int)(this.Left + this.Width / 2), (int)(this.Top + this.Height / 2));
        }
        private struct POINT
        {
            public int X;
            public int Y;
        }
        private static class NativeMethods
        {
            [System.Runtime.InteropServices.DllImport("user32.dll")]
            public static extern bool GetCursorPos(out POINT lpPoint);

            [System.Runtime.InteropServices.DllImport("user32.dll")]
            public static extern bool SetCursorPos(int X, int Y);
        }




        // Class to store image data
        public class ImageData
        {
            public float[] CameraPosition { get; set; }
            public float TransitionTime { get; set; }
            public string Notes { get; set; }
            public byte[] CameraPositionArray { get; set; }
            public bool FacePlayer { get; set; }
            public bool SpectatePlayer { get; set; }
            public string[] CameraOffsetsArray { get; set; }
            public string SelctedPlayerString { get; set; }
            public string ImageFileName { get; set; }
        }
        public static class ScreenshotHelper
        {
            public static ImageSource GetBitmapThumbnailAsync(int thumbnailWidth, int thumbnailHeight)
            {
                IntPtr handle = ToolSetting.p.MainWindowHandle;
                IntPtr myhandle = Process.GetCurrentProcess().MainWindowHandle;

                // Store the current mouse position
                NativeMethods.POINT originalMousePosition;
                NativeMethods.GetCursorPos(out originalMousePosition);

                // Check if the window is minimized and restore it if needed
                if (User32.IsIconic(handle))
                    User32.ShowWindowAsync(handle, User32.SHOWNORMAL);

                User32.SetForegroundWindow(handle);

                // Simulate pressing Alt+PrintScreen
                Key altKey = (Key)KeyInterop.VirtualKeyFromKey(Key.LeftAlt);
                Key printScreenKey = (Key)KeyInterop.VirtualKeyFromKey(Key.PrintScreen);

                NativeMethods.keybd_event((byte)altKey, 0, NativeMethods.KEYEVENTF_EXTENDEDKEY, 0);
                NativeMethods.keybd_event((byte)printScreenKey, 0, NativeMethods.KEYEVENTF_EXTENDEDKEY, 0);
                Thread.Sleep(200);
                NativeMethods.keybd_event((byte)printScreenKey, 0, NativeMethods.KEYEVENTF_EXTENDEDKEY | NativeMethods.KEYEVENTF_KEYUP, 0);
                NativeMethods.keybd_event((byte)altKey, 0, NativeMethods.KEYEVENTF_EXTENDEDKEY | NativeMethods.KEYEVENTF_KEYUP, 0);

                Thread.Sleep(100);

                // Get the screenshot from the clipboard
                BitmapSource fullScreenshot = Clipboard.GetImage() as BitmapSource;

                // Calculate the scaling factors for thumbnail size
                double scaleX = (double)thumbnailWidth / fullScreenshot.PixelWidth;
                double scaleY = (double)thumbnailHeight / fullScreenshot.PixelHeight;

                // Create a scaled thumbnail bitmap
                TransformedBitmap thumbnailBitmap = new TransformedBitmap(fullScreenshot, new ScaleTransform(scaleX, scaleY));

                IntPtr clipWindow = User32.GetOpenClipboardWindow();
                User32.OpenClipboard(clipWindow);
                User32.EmptyClipboard();
                User32.CloseClipboard();

                // Bring the application window to the front again
                User32.SetForegroundWindow(myhandle);

                // Restore the mouse position
                NativeMethods.SetCursorPos(originalMousePosition.X, originalMousePosition.Y);

                return thumbnailBitmap;
            }


            private static IntPtr GetWindowHandle(string name)
            {
                var process = Process.GetProcessesByName(name).FirstOrDefault();
                if (process != null && process.MainWindowHandle != IntPtr.Zero)
                    return process.MainWindowHandle;

                return IntPtr.Zero;
            }

            //Functions utilizing the user32.dll 
            //Documentation on user32.dll - http://www.pinvoke.net/index.aspx
            public class User32
            {
                public const int SHOWNORMAL = 1;
                public const int SHOWMINIMIZED = 2;
                public const int SHOWMAXIMIZED = 3;

                [DllImport("user32.dll")]
                public static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

                [DllImport("user32.dll")]
                [return: MarshalAs(UnmanagedType.Bool)]
                public static extern bool SetForegroundWindow(IntPtr hWnd);

                [DllImport("user32.dll")]
                [return: MarshalAs(UnmanagedType.Bool)]
                public static extern bool IsIconic(IntPtr hWnd);

                [DllImport("user32.dll", SetLastError = true)]
                public static extern bool CloseClipboard();

                [DllImport("user32.dll", SetLastError = true)]
                public static extern bool OpenClipboard(IntPtr hWndNewOwner);

                [DllImport("user32.dll")]
                public static extern bool EmptyClipboard();

                [DllImport("user32.dll")]
                public static extern IntPtr GetOpenClipboardWindow();
            }
            public class NativeMethods
            {
                public const int KEYEVENTF_EXTENDEDKEY = 0x0001;
                public const int KEYEVENTF_KEYUP = 0x0002;

                [DllImport("user32.dll")]
                public static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);
                [DllImport("user32.dll")]
                public static extern bool SetCursorPos(int X, int Y);

                [DllImport("user32.dll")]
                public static extern bool GetCursorPos(out POINT lpPoint);

                [StructLayout(LayoutKind.Sequential)]
                public struct POINT
                {
                    public int X;
                    public int Y;
                }
            }


        }

        public void ResetOrientation(bool postion)
        {
            if (postion)
            {
                m.WriteMemory(xAddress.ToString("X"), "float", 0.ToString());
                m.WriteMemory(yAddress.ToString("X"), "float", 0.ToString());
                m.WriteMemory(zAddress.ToString("X"), "float", 10.ToString());

            }
            else
            {
                m.WriteMemory(yawAddress.ToString("X"), "float", 0.ToString());
                m.WriteMemory(pitchAddress.ToString("X"), "float", 0.ToString());
                m.WriteMemory(rollAddress.ToString("X"), "float", 0.ToString());
            }
        }

        public void TestWTS()
        {
            GOW.WTSTest();
        }

        //private void TeleportPlayerName(string playerName, float x, float y, float z)
        //{
        //    if (!Injected)
        //    {
        //        PrintToConsole("Error: DLL not injected!!");
        //        return;
        //    }
        //    S_TP_Player playerCoordinates = new S_TP_Player();
        //    if (jumpToIndex < 0) jumpToIndex = 0;
        //    float[] cameraPos = { x, y, z };
        //    if (x == 999 && y == 999 && z == 999)
        //    {
        //        ImageData selectedImageData = imageDataList[jumpToIndex];
        //        cameraPos = selectedImageData.CameraPosition;
        //    }


        //    string salt = "0x" + GetPlayerNameSalt(playerName);
        //    if (salt == "null") return;
        //    playerCoordinates.player_datum = Convert.ToUInt32(salt, 16);

        //    playerCoordinates.coord_x = cameraPos[0]; playerCoordinates.coord_y = cameraPos[1]; playerCoordinates.coord_z = cameraPos[2];
        //    uint playerDatum = (uint)injector.CallFunction("H2V-API.dll", "GetPlayerDatumFromPlayerInfo", playerCoordinates.player_datum);
        //    if (playerDatum == 0xFFFFFFFF) return;

        //    playerCoordinates.player_datum = playerDatum;
        //    long sucessful = injector.CallFunction("H2V-API.dll", "SetPlayerCoordinates", playerCoordinates);
        //}

        //[StructLayout(LayoutKind.Sequential, Pack = 1)] // Set the struct alignment to 1 byte
        //struct S_TP_Player
        //{
        //    public UInt32 player_datum;
        //    public float coord_x;
        //    public float coord_y;
        //    public float coord_z;
        //}
        //public bool Injected = false;
        //public void InjectDLL()
        //{
        //    string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        //    string dllFolderPath = Path.Combine(baseDirectory, "Content", "DLLs");
        //    string dllPath = Path.Combine(dllFolderPath, "H2V-API.dll");
        //    long result = injector.Inject(dllPath);

        //    if (result == 0)
        //    {
        //        MessageBox.Show("Injection Failed");
        //        injector.Dispose();
        //        return;
        //    }
        //    else Injected = true;
        //}

        public void UpdateCameraSpeed()
        {
            if (isAppLoading)
                return;

            if (!string.IsNullOrEmpty(settingsWindow.TextBox_FlySpeed.Text) && ContainsOnlyNumbersOrDecimals(settingsWindow.TextBox_FlySpeed.Text)) c_moveSpeed = float.Parse(settingsWindow.TextBox_FlySpeed.Text);
            if (!string.IsNullOrEmpty(settingsWindow.TextBox_Turnspeed.Text) && ContainsOnlyNumbersOrDecimals(settingsWindow.TextBox_Turnspeed.Text)) c_turnSpeed = float.Parse(settingsWindow.TextBox_Turnspeed.Text);
            if (!string.IsNullOrEmpty(settingsWindow.TextBox_Pitchspeed.Text) && ContainsOnlyNumbersOrDecimals(settingsWindow.TextBox_Pitchspeed.Text)) c_pitchSpeed = float.Parse(settingsWindow.TextBox_Pitchspeed.Text);
            if (!string.IsNullOrEmpty(settingsWindow.TextBox_Heightspeed.Text) && ContainsOnlyNumbersOrDecimals(settingsWindow.TextBox_Heightspeed.Text)) c_heightSpeed = float.Parse(settingsWindow.TextBox_Heightspeed.Text);
            if (!string.IsNullOrEmpty(settingsWindow.TextBox_Rollspeed.Text) && ContainsOnlyNumbersOrDecimals(settingsWindow.TextBox_Rollspeed.Text)) c_rollSpeed = float.Parse(settingsWindow.TextBox_Rollspeed.Text);
        }
        private void Button_Up_Click(object sender, RoutedEventArgs e)
        {
            Button clickedButton = (Button)sender;
            string textBoxName = "TextBox_Offset_" + clickedButton.Name.Split('_')[1]; // Extract X, Y, Z

            TextBox textBox = FindName(textBoxName) as TextBox;
            if (textBox != null)
            {
                string n = textBox.Text;
                float x = float.Parse(n);
                float y = (float)Math.Round((x + 0.2), 1);
                textBox.Text = (y).ToString();
                UpdateImageDataIndex(false);
            }
        }

        private void Button_Down_Click(object sender, RoutedEventArgs e)
        {
            Button clickedButton = (Button)sender;
            string textBoxName = "TextBox_Offset_" + clickedButton.Name.Split('_')[1]; // Extract X, Y, Z

            TextBox textBox = FindName(textBoxName) as TextBox;
            if (textBox != null)
            {
                string n = textBox.Text;
                float x = float.Parse(n);
                float y = (float)Math.Round((x - 0.2), 1);
                textBox.Text = (y).ToString();
                UpdateImageDataIndex(false);
            }
        }

        public void SetTickrate(int tickRateInt)
        {
            byte tickRate = (byte)tickRateInt;
            float tickLength = 1.0f / tickRateInt;

            m.WriteMemory("halo2.exe+004C06E4, 2", "byte", tickRate.ToString("X"));
            m.WriteMemory("halo2.exe+004C06E4, 4", "float", tickLength.ToString());
        }
        class PlayerMovementRecorder : MainWindow
        {
            private struct PlayerInput
            {
                public float X;
                public float Y;
                public float Z;
                public float X_Vel;
                public float Y_Vel;
                public float Z_Vel;
                public float Shields;
                public long Timestamp; // You can use Stopwatch.GetTimestamp() for high-resolution timing
            }

            private List<PlayerInput> recordedInputs;
            private bool recording;

            public PlayerMovementRecorder()
            {
                recordedInputs = new List<PlayerInput>();
                recording = false;
            }

            public void StartRecording()
            {
                recordedInputs.Clear();
                recording = true;
            }

            public void StopRecording()
            {
                recording = false;
            }

            public void RecordPlayerPosition(float x, float y, float z, float xVel, float yVel, float zVel, float shields)
            {
                if (recording)
                {
                    PlayerInput input = new PlayerInput
                    {
                        X = x,
                        Y = y,
                        Z = z,
                        X_Vel = xVel,
                        Y_Vel = yVel,
                        Z_Vel = zVel,
                        Shields = shields,
                        Timestamp = Stopwatch.GetTimestamp()
                    };
                    recordedInputs.Add(input);
                }
            }

            public void ReplayPlayerMovements()
            {
                if (recordedInputs.Count == 0)
                {
                    Console.WriteLine("No recorded inputs to replay.");
                    return;
                }

                long startTime = Stopwatch.GetTimestamp();
                foreach (var input in recordedInputs)
                {
                    long elapsedTime = input.Timestamp - startTime;
                    long targetTime = elapsedTime + startTime;

                    while (Stopwatch.GetTimestamp() < targetTime)
                    {
                        // You may need to adjust this loop depending on the game's update rate
                        // and the desired playback speed
                    }

                    // Adjust the player's velocity to guide them toward the recorded position
                    // This simulates pushing the player using the velocity
                    string salt = string.Empty;
                    if (selectedImageData.FacePlayer)
                    {
                        salt = GetPlayerNameSalt(ComboBox_SceneData_Playernames.Text);
                    }
                    else
                    {
                        salt = GetPlayerNameSalt(ComboBox_Playernames.Text);
                    }
                    int obj_List_Memory_Address;
                    float p_X, p_Y, p_Z, p_X_Vel, p_Y_Vel, p_Z_Vel, p_pitch, p_Yaw, p_Shields;
                    ReadObjectXYZ(salt, out obj_List_Memory_Address, out p_X, out p_Y, out p_Z, out p_X_Vel, out p_Y_Vel, out p_Z_Vel, out p_pitch, out p_Yaw, out p_Shields);

                    float deltaX = input.X - p_X;
                    float deltaY = input.Y - p_Y;
                    float deltaZ = input.Z - p_Z;

                    // Calculate the normalized direction vector
                    float magnitude = (float)Math.Sqrt(deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ);
                    float normalizedDeltaX = deltaX / magnitude;
                    float normalizedDeltaY = deltaY / magnitude;
                    float normalizedDeltaZ = deltaZ / magnitude;

                    // Adjust the player's velocity to follow the direction vector
                    float adjustedXVel = normalizedDeltaX * input.X_Vel;
                    float adjustedYVel = normalizedDeltaY * input.Y_Vel;
                    float adjustedZVel = normalizedDeltaZ * input.Z_Vel;

                    // Update the game's memory with the adjusted velocity values
                    // Example memory write code (replace with actual memory writing method):
                    // memoryWriter.WriteObjectVelocity("player_salt", objMemAddress, adjustedXVel, adjustedYVel, adjustedZVel);
                    int p_Address = int.Parse(settingsWindow.Textbox_P_Address_replay.Text);
                    WriteObjectVelocity(p_Address, adjustedXVel, adjustedYVel, adjustedZVel);
                }
            }
            public void WriteObjectVelocity(int address, float X_Vel, float Y_Vel, float Z_Vel)
            {
                m.WriteMemory((address - 0x8).ToString("X"), "float", X_Vel.ToString());
                m.WriteMemory((address - 0x4).ToString("X"), "float", Y_Vel.ToString());
                m.WriteMemory(address.ToString("X"), "float", Z_Vel.ToString());
            }
        }

        private void EnableDebugSliders(object sender, RoutedEventArgs e)
        {
            if (checkbox_EnableSliderStats.IsChecked == true) GOW.EnableDebugSliders(true);
            else GOW.EnableDebugSliders(false);
        }
        public bool loopCamera = false;
        private void LoopCamera(object sender, RoutedEventArgs e)
        {
            if (!loopCamera) loopCamera = true;
            else loopCamera = false;
        }

        private void ShowNamePlates(object sender, RoutedEventArgs e)
        {
            noClip = true;
            //GOW.SetnamePlateName(ComboBox_Playernames.Text.Split(':')[0]);
        }

        private void UpdateSelectedSceneTextBox_Event(object sender, TextChangedEventArgs e)
        {
            if (isAppLoading) return;
            TextBox_SelectedScene.Text = "Selected Scene";
            TextBox_SelectedScene.Foreground = Brushes.White;
        }
        public bool auto_warpFix = false;

        private void AutoWarpfix()
        {
            if ((bool)checkbox_AutoWarpFix.IsChecked)
            {
                auto_warpFix = true;
                _ = ApplyGameMod("//warpfix", true);
            }
            else
            {
                auto_warpFix = false;
            }
        }

        private void OpenBindsButton_Click(object sender, RoutedEventArgs e)
        {
            if (controllerKeyBindsWindow == null)
            {
                controllerKeyBindsWindow = new ControllerKeyBinds(this);
                controllerKeyBindsWindow.Closed += Window_Closed;
            }
            else
            {
                controllerKeyBindsWindow.Activate(); // Bring the existing window to the front
            }
            if (!controllerKeyBindsWindow.IsActive) controllerKeyBindsWindow.Activate();
            controllerKeyBindsWindow.Show();
        }
        private void Window_Closed(object sender, EventArgs e)
        {
            controllerKeyBindsWindow = null; // Set the window to null when it's closed
        }

        private void CheckBox_AutoApplyWarpfix(object sender, DependencyPropertyChangedEventArgs e)
        {
            AutoWarpfix();
        }

        private async void EditFolderPaths_Click(object sender, RoutedEventArgs e)
        {
            await CheckAndSetMapFiles();
        }

        private void CheckBoxChecked_SpectatePlayer(object sender, RoutedEventArgs e)
        {
            if (CheckBox_SpectatePlayer.IsChecked == true) SpectateCamera = true;
            else SpectateCamera = false;
        }

        private void UpdateTransitionTimes(object sender, RoutedEventArgs e)
        {
            imageDataList.ForEach(imageData => imageData.TransitionTime = float.Parse(GlobalTransitionTimeTextBox.Text));
        }

        private void ResetPositionButton(object sender, RoutedEventArgs e)
        {
            ResetOrientation(true);
        }

        private void ResetorientationButto(object sender, RoutedEventArgs e)
        {
            ResetOrientation(false);
        }

        private void UpdateCommands(object sender, EventArgs e)
        {
            GetCommansFromFile();
        }

        private void OpenKBBindsButton_Click(object sender, RoutedEventArgs e)
        {

            if (keyboardKeyBindsWindow == null)
            {
                keyboardKeyBindsWindow = new KeyBoardBinds(this);
                keyboardKeyBindsWindow.Closed += Window_Closed;
            }
            else
            {
                keyboardKeyBindsWindow.Activate(); // Bring the existing window to the front
            }
            if (!keyboardKeyBindsWindow.IsActive) keyboardKeyBindsWindow.Activate();
            keyboardKeyBindsWindow.Show();
        }
        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Update the position of the GameChatWindow when the main window is resized
            UpdateGameChatWindowPosition();
        }
        private void checkbox_GameChatWindow_Click(object sender, RoutedEventArgs e)
        {
            // Create and show the GameChatWindow
            gameChatWindow = new GameChatWindow(m, this);

            // Set the owner to the main window
            gameChatWindow.Owner = this;

            // Update the position of the GameChatWindow
            UpdateGameChatWindowPosition();

            gameChatWindow.Show();
        }
        private void UpdateGameChatWindowPosition()
        {
            if (gameChatWindow != null && gameChatWindow.IsVisible)
            {
                // Set the position to the right side of the main window
                gameChatWindow.Left = this.Left + this.Width;
                gameChatWindow.Top = this.Top;
            }
        }
    }




}




