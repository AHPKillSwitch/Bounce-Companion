using System;
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
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Text;
using Newtonsoft.Json;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using SharpDX.XInput;
using System.Windows.Controls;
using MathNet.Numerics.Interpolation;
using Microsoft.Win32;
using System.Xml;
using static Bounce_Companion.ControllerKeyBinds;
using static Bounce_Companion.RuntimeMemory;
using Bounce_Companion.Code.Camera_Tool;
using Bounce_Companion.Code.Object___Havok_Helpers;
using Bounce_Companion.Code.Bounce_Companion_Utility;
using static Bounce_Companion.Code.Camera_Tool.CameraTool;
using Octokit;
using Bounce_Companion.Code.Command_Handler;
using Bounce_Companion.Code.Bounce_Handler;

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
        public string currentVersion = "1.0.24 "; // Change this to your current application version
        public string newVersion = string.Empty;
        public Mem m;
        public Mem mp2;
        public Process p;
        public RuntimeMemory rm;
        private GameChatWindow gameChatWindow;
        CommandExecution CE;

        CameraInterpolation cam_interp;
        CameraControls cameraControls;
        CameraTool cameraTool;
        CommandHandler commandHandler;
        ObjectHandler objectHandler;
        Utility utility;
        BounceHandler BH;


        HandleChallenges CH;
        
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
        
        // Camera Transition speed
        public float c_GlobalTransitionTime = 0f;
        public int loopDelayTime = 100;

        public bool tags_Loaded_Status;
        public bool enableDebug = false;
        public bool customContrails = false;
        private bool userTextChanged = true;
        public bool isAppLoading = true;
        
        //public WebSocket ws;
        
        public DispatcherTimer UpdateTimer = new DispatcherTimer();
        public DispatcherTimer CheckerTimer = new DispatcherTimer();
        public DispatcherTimer PlayerMonitorTimer = new DispatcherTimer();
        public GameVolumeGetter volumeGetter = new GameVolumeGetter();
        

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr FindWindow(string strClassName, string strWindowName);

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hwnd, ref Rect rectangle);

        public MainWindow()
        {
            InitializeComponent();
            
            
            
            staackPanel_CameraTool.Visibility = Visibility.Hidden;
            controllerKeyBindsWindow = new ControllerKeyBinds(this);
            controllerKeyBindsWindow.Closed += Window_Closed;
            SizeChanged += MainWindow_SizeChanged;
            m = new Mem();
        }
        public static class ToolSetting
        {
            public static string currentGame = string.Empty;
            public static Process p = null;
            public static string currentGameDLL = string.Empty;
        }
        public static class Globals
        {
            public static Process myProcess;
            public static Process myProcessPlayer2;
            public static IntPtr[] assignedMemory = null;
            public static Dictionary<string, string> tagList = new Dictionary<string, string>();
            public static Dictionary<string, StringID> StringIDs = new Dictionary<string, StringID>();
            public static int pokedCommandCount = 0;
            public static bool t_loaded = false;
            public static string puginpath = "Plugins/";

            public static bool modsEnabled = true;
            public static bool modsEnabledMaster = true;

            //camera Addresses
            public static int xAddress = 0;
            public static int yAddress = 0;
            public static int zAddress = 0;
            public static int yawAddress = 0;
            public static int pitchAddress = 0;
            public static int rollAddress = 0;

            //camera flyspeeds
            public static float c_moveSpeed = 0f;
            public static float c_turnSpeed = 0f;
            public static float c_pitchSpeed = 0f;
            public static float c_heightSpeed = 0f;
            public static float c_rollSpeed = 0f;

            //Camera Tool Parameters
            public static string currentProjectName = string.Empty;
            public static bool rollCamera = false;
            public static int jumpToIndex = 0;

            //Bounce Tracker Stats
            

            public static List<ImageData> imageDataList; // Store the image data
            
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

                    
                    SetCameraFlySpeeds();
                    ParseCommandsFromFile();
                    StartUpdateTask();
                    
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
                    utility.PrintToConsole("Download Starting!");
                    CompanionUpdater.UpdateChecker.DownloadLatestZipRelease(owner, repo);
                    utility.PrintToConsole("Download Complete: \n" +
                        "New Version can be found in your current Bounce Companion root folder, Its called Bounce Companion " + newVersion + ".zip");
                }
                else
                {
                    utility.PrintToConsole("Update Found: User Denied!");
                    return false;

                }
            }
            else
            {
                utility.PrintToConsole("Current Version is up-to date!");
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
                utility.PrintToConsole("Error in bounce checker.");
            }
        }
        private void SetCameraFlySpeeds()
        {
            settingsWindow.TextBox_FlySpeed.Text = Globals.c_moveSpeed.ToString();
            settingsWindow.TextBox_Turnspeed.Text = Globals.c_turnSpeed.ToString();
            settingsWindow.TextBox_Pitchspeed.Text = Globals.c_pitchSpeed.ToString();
            settingsWindow.TextBox_Heightspeed.Text = Globals.c_heightSpeed.ToString();
            settingsWindow.TextBox_Rollspeed.Text = Globals.c_rollSpeed.ToString();
            GlobalTransitionTimeTextBox.Text = c_GlobalTransitionTime.ToString();
            int tickrate = m.ReadByte("halo2.exe+0x004C06E4,0x02");
            settingsWindow.Textbox_Tickrate.Text = tickrate.ToString();
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

            Globals.c_moveSpeed = float.Parse(configJson.MoveSpeed);
            Globals.c_turnSpeed = float.Parse(configJson.TurnSpeed);
            Globals.c_pitchSpeed = float.Parse(configJson.PitchSpeed);
            Globals.c_heightSpeed = float.Parse(configJson.HeightSpeed);
            Globals.c_rollSpeed = float.Parse(configJson.RollSpeed);
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
                utility.PrintToConsole("Invalid Config Found.");

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
                            utility.PrintToConsole("Maps Folder Set");

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
                            utility.PrintToConsole("Invalid Maps Folder. Please select the correct game folder.");
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

            utility.PrintToConsole("Config Is Valid.");
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
                    utility.PrintToConsole("Success.");
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
                        m.OpenProcessHandle(p);
                        selectedProcessindex = selectedIndex;
                        utility.PrintToConsole("Main Process Success.");
                        attached = true;
                        if (selectedIndex < 0)
                        {
                            p = processes[1];
                            mp2.OpenProcessHandle(p);
                            player2Attched = true;
                            utility.PrintToConsole("Second Process Success.");
                        }
                    }
                    else
                    {
                        p = processes[0];
                        mp2.OpenProcessHandle(p);
                        selectedProcessindex = selectedIndex;
                        utility.PrintToConsole("Second Process Success.");
                        p = processes[1];
                        m.OpenProcessHandle(p);
                        utility.PrintToConsole("Main Process Success.");
                        attached = true;
                        player2Attched = true;
                    }

                }
                else
                {
                    utility.PrintToConsole("Failed, the selected process does not exist.");
                }

            }
            catch
            {
                utility.PrintToConsole("Failed, make sure game is running");
            }
        }
        public bool player2Attched = false;
        bool tagscurrentlyloaded;
        public bool UpdateTagStatus()
        {
            string tags_Loaded = m.ReadString("halo2.exe+47CF0C");
            bool tags_Loaded_Status = tags_Loaded != "mainmenu";

            bool newTagsLoaded = tagscurrentlyloaded;

            if (!tagscurrentlyloaded && tags_Loaded_Status) //Check if tags are loaded if not load them
            {
                tagscurrentlyloaded = true;
                PrintToConsole_ContinueNextText("Checking Game Session . . . ");
                if (GameTypeValidCheck())
                {
                    utility.PrintToConsole("Valid Session Found!");
                    HideMods(false);
                    rm = new RuntimeMemory(m, p, this, mapspath, customMapsPath);
                    CE = new CommandExecution(rm, m, this);
                    CH = new HandleChallenges(this); //challenge handler
                    if (auto_warpFix) _ = commandHandler.ApplyGameMod("warpfix", true);
                }
                else
                {
                    utility.PrintToConsole("Invalid Session Found! " +
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
                _ = commandHandler.ApplyGameMod("//debugcamera", false);
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
        public int mapType = 0;
        private bool GameTypeValidCheck()
        {
            string gameTypeName = System.Text.Encoding.Unicode.GetString(m.ReadBytes("halo2.exe+97777C", 127)).Split('\0').FirstOrDefault(part => !string.IsNullOrEmpty(part));
            mapType = m.ReadInt32("halo2.exe+0x47CD88");



            //string gameTypeName = m.ReadString("halo2.exe+97777C", "", 127, true);
            if (gameTypeName.ToLower().Contains("ogh2") || gameTypeName.ToLower().Contains("glitch") || mapType == 0) return true;
            else return false; 

        }
        private async Task UpdateChecker()
        {
            while (true)
            {
                if (!Globals.rollCamera || !debugCameraToggle && attached)
                {
                    if (System.Windows.Application.Current == null)
                    {
                        Environment.Exit(1); // You can choose an appropriate exit code
                    }
                    bool tags_Loaded_Status = false;
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        tags_Loaded_Status = UpdateTagStatus();
                    });
                    if (customContrails)
                    {
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            _ = UpdatePlayerList();
                        });
                    }

                    if (tags_Loaded_Status)
                    {
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                           BH.BounceChecker();
                        });
                    }

                }
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    float[] cameraPosition = cameraControls.GetCameraData(out byte[] cameraPositionArray);
                    UpdateUICameraCoordinates(cameraPosition[0], cameraPosition[1], cameraPosition[2], cameraPosition[3], cameraPosition[4], cameraPosition[5]);
                });
                await Task.Delay(33); // Milliseconds
            }
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
                        utility.PrintToConsole(output);
                    }
                }
                else
                {
                    utility.PrintToConsole("No bounce locations found.");
                }
            }
            catch (Exception ex)
            {
                utility.PrintToConsole($"Error: {ex.Message}");
            }
        }
        
        bool follow;

        private void playerInfoTabClick(object sender, RoutedEventArgs e)
        {
            if (checkbox_DisableOverlay.IsChecked == true) utility.PrintToConsole("Screen Overlay is Disabled - Please Enable it to see Player Info Bar.");
            else if (checkbox_EnableBounceStats.IsChecked == true)
            {
                follow = true;
                GOW.EnablePlayerInfoTab(true);
                utility.PrintToConsole("Player Info Bar: Enabled \nNote: Click and drag the Player Info Bar to place it in a new position. ");
                //FollowCam();
            }
            else
            {
                follow = false;
                GOW.EnablePlayerInfoTab(false);
                utility.PrintToConsole("Player Info Bar: Disabled");
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
            System.Windows.Application.Current.MainWindow = GOW;
            System.Windows.Application.Current.MainWindow.Left = halo2WindowRect.Left;
            System.Windows.Application.Current.MainWindow.Top = halo2WindowRect.Top;
            System.Windows.Application.Current.MainWindow.Width = halo2WindowRect.Right - halo2WindowRect.Left;
            System.Windows.Application.Current.MainWindow.Height = halo2WindowRect.Bottom - halo2WindowRect.Top;

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
                    utility.PrintToConsole("Overlay: Disabled");
                }
                else
                {
                    GOW.Show();
                    utility.PrintToConsole("Overlay: Enabled");
                }
            }
            catch
            {
                GOW.Activate();
                utility.PrintToConsole("Overlay: Enabled");
            }
        }

        private void ShowCredits(object sender, RoutedEventArgs e)
        {
            if (checkbox_Credits.IsChecked == true)
            {
                utility.PrintToConsole("" +
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

        private string PopulatePlayersList(int i)
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
                        commandHandler.GetCommandsFromString("", command);
                        utility.PrintToConsole(rm.outPutStrings);
                        rm.outPutStrings.Clear();

                    }
                }
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
            commandHandler.GetCommandsFromString(consoleInputString, "");
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
            commandHandler.ApplyMods(true, ComboBox_Mods.SelectionBoxItem.ToString());
        }
        private void ApplyDDLModOff(object sender, RoutedEventArgs e)
        {
            commandHandler.ApplyMods(false, ComboBox_Mods.SelectionBoxItem.ToString());
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
                    string command = utility.CleanString(commandLine);
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
                    utility.PrintToConsole("Failed to Register Hotkey. Key: " + keyBindBttn);
                }

            }
        }
        List<Task> tasks = new List<Task>();
        
        [DllImport("user32.dll")]
        public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        public const int KEYEVENTF_KEYDOWN = 0x0001; // Key down
        public const int KEYEVENTF_KEYUP = 0x0002;   // Key up

        public const byte VK_SPACE = 0x20; // The virtual key code for the spacebar
        
        
        
        

        

        private void Disable_Mods(object sender, RoutedEventArgs e)
        {
            if (checkbox_DisableAutoPoking.IsChecked == true)
            {
                modsEnabledMaster = false;
                utility.PrintToConsole("Auto-Poking: Disabled");
            }
            else
            {
                modsEnabledMaster = true;
                utility.PrintToConsole("Auto-Poking: Enabled");
            }
        }
        public static List<string> p_colour = new List<string>();
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
                    string command = utility.CleanString(line);
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
       
        public bool debugCameraToggle = false;
        int debugCameraTogglebytes = 0;
        private void ToggleDebugCamera(object sender, RoutedEventArgs e)
        {
            ToggleDebugMode();
        }

        private void ToggleDebugMode()
        {
            debugCameraTogglebytes = m.ReadInt32("halo2.exe+0x4A8870");
            if (m.ReadInt32("halo2.exe+0x4A84B0") == debugCameraTogglebytes)
            {
                m.WriteToMemory("halo2.exe+0x4A849C", "int", "2");
                debugCameraToggle = true;
            }
            else
            {
                m.WriteToMemory("halo2.exe+0x4A849C", "int", "0");
                m.WriteToMemory("halo2.exe+0x4A84B0", "int", debugCameraTogglebytes.ToString());
                debugCameraToggle = false;
            }
        }

        
        
        private void JumpToSceneButton_Click(object sender, RoutedEventArgs e)
        {
            cameraTool.JumpCameraToScene(-1);
        }
        
        private void ClearTimelineButton_Click(object sender, RoutedEventArgs e)
        {
            cameraTool.ClearTimeline();
        }
        
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            cameraTool.CaptureScene();
        }
        private void NewProjectButton_Click(object sender, RoutedEventArgs e)
        {
            cameraTool.CreateNewProject();
        }
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
       

        
        

        private void SaveSceneDataToFile_click(object sender, RoutedEventArgs e)
        {
            SaveSceneDataToFile(imageDataList);
        }

        private void LoadScenarioDataFromFile_click(object sender, RoutedEventArgs e)
        {
            cameraTool.ClearTimeline();
            Globals.imageDataList = cameraTool.LoadSceneData();
            cameraTool.LoadSceneFromFile();
        }

        private void InsertSceneBeforeSelected_Click(object sender, RoutedEventArgs e)
        {
            cameraTool.InsertSceneAtSelected(jumpToIndex, true);
        }

        private void InsertSceneAfterSelected_Click(object sender, RoutedEventArgs e)
        {
            cameraTool.InsertSceneAtSelected(jumpToIndex, false);
        }
        private async void StartCamera(object sender, RoutedEventArgs e)
        {
            await cameraTool.StartCameraRoll();
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

        

        private void UpdateScene_Click(object sender, RoutedEventArgs e)
        {
            cameraTool.UpdateImageDataIndex(false);
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
            cameraTool.UpdateImageDataIndex(false);

        }

        private void ResetScene_Click(object sender, RoutedEventArgs e)
        {
            cameraTool.UpdateImageDataIndex(true);
        }

        private void StopCamera(object sender, RoutedEventArgs e)
        {
            cameraTool.rollCamera = false;
        }


        

        private static float ToDegrees(float radians)
        {
            return radians * (180f / (float)Math.PI);
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
        

        public void TestWTS()
        {
            GOW.WTSTest();
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

            m.WriteToMemory("halo2.exe+004C06E4, 2", "byte", tickRate.ToString("X"));
            m.WriteToMemory("halo2.exe+004C06E4, 4", "float", tickLength.ToString());
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

            
            
        }

        private void EnableDebugSliders(object sender, RoutedEventArgs e)
        {
            if (checkbox_EnableSliderStats.IsChecked == true) GOW.EnableDebugSliders(true);
            else GOW.EnableDebugSliders(false);
        }
        public bool loopCamera = false;
        private void LoopCamera(object sender, RoutedEventArgs e)
        {
            cameraTool.LoopCamerapath();
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
                _ = commandHandler.ApplyGameMod("//warpfix", true);
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
            keyboardKeyBindsWindow = null; // Set the window to null when it's closed
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
            Globals.imageDataList.ForEach(imageData => imageData.TransitionTime = float.Parse(GlobalTransitionTimeTextBox.Text));
        }

        private void ResetPositionButton(object sender, RoutedEventArgs e)
        {
            cameraTool.ResetOrientation(true);
        }

        private void ResetorientationButto(object sender, RoutedEventArgs e)
        {
            cameraTool.ResetOrientation(false);
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
            if (!keyboardKeyBindsWindow.IsActive)
            {
                keyboardKeyBindsWindow.Activate();
                keyboardKeyBindsWindow.Show();
            }

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




