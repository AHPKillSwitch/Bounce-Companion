using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Newtonsoft.Json;
using System.Threading.Channels;
using static Bounce_Companion.ControllerKeyBinds;
using System.Linq;
using Bounce_Companion.Code.Camera_Tool;

namespace Bounce_Companion
{
    /// <summary>
    /// Interaction logic for ControllerKeyBinds.xaml
    /// </summary>
    public partial class ControllerKeyBinds : Window
    {
        MainWindow main;
        private bool windowLoading = true;
        private List<ComboBox> modComboBoxes = new List<ComboBox>();
        private List<ComboBox> actionComboBoxes = new List<ComboBox>();

        private List<ComboBox> keybindComboBoxes = new List<ComboBox>();
        public List<CheckBox> toggleCheckboxes = new List<CheckBox>();
        private string[] KeyBinds;
        public ControllerKeyBinds(MainWindow Main)
        {
            main = Main;
            InitializeComponent();
            modComboBoxes.Add(ComboBox_RT_Mod);
            modComboBoxes.Add(ComboBox_X_Mod);
            modComboBoxes.Add(ComboBox_RB_Mod);
            modComboBoxes.Add(ComboBox_Start_Mod);
            modComboBoxes.Add(ComboBox_RightStick_Mod);
            modComboBoxes.Add(ComboBox_A_Mod);
            modComboBoxes.Add(ComboBox_Y_Mod);
            modComboBoxes.Add(ComboBox_B_Mod);
            modComboBoxes.Add(ComboBox_LB_Mod);
            modComboBoxes.Add(ComboBox_LT_Mod);
            modComboBoxes.Add(ComboBox_LeftStick_Mod);
            modComboBoxes.Add(ComboBox_DPadUp_Mod);
            modComboBoxes.Add(ComboBox_DPadLeft_Mod);
            modComboBoxes.Add(ComboBox_DPadRight_Mod);
            modComboBoxes.Add(ComboBox_DPadDown_Mod);
            modComboBoxes.Add(ComboBox_Back_Mod);

            actionComboBoxes.Add(ComboBox_RT_EnableState);
            actionComboBoxes.Add(ComboBox_X_EnableState);
            actionComboBoxes.Add(ComboBox_RB_EnableState);
            actionComboBoxes.Add(ComboBox_Start_EnableState);
            actionComboBoxes.Add(ComboBox_RightStick_EnableState);
            actionComboBoxes.Add(ComboBox_A_EnableState);
            actionComboBoxes.Add(ComboBox_Y_EnableState);
            actionComboBoxes.Add(ComboBox_B_EnableState);
            actionComboBoxes.Add(ComboBox_LB_EnableState);
            actionComboBoxes.Add(ComboBox_LT_EnableState);
            actionComboBoxes.Add(ComboBox_LeftStick_EnableState);
            actionComboBoxes.Add(ComboBox_DPadUp_EnableState);
            actionComboBoxes.Add(ComboBox_DPadLeft_EnableState);
            actionComboBoxes.Add(ComboBox_DPadRight_EnableState);
            actionComboBoxes.Add(ComboBox_DPadDown_EnableState);
            actionComboBoxes.Add(ComboBox_Back_EnableState);

            keybindComboBoxes.Add(ComboBox_RT_KeyBind);
            keybindComboBoxes.Add(ComboBox_X_KeyBind);
            keybindComboBoxes.Add(ComboBox_RB_KeyBind);
            keybindComboBoxes.Add(ComboBox_Start_KeyBind);
            keybindComboBoxes.Add(ComboBox_RightStick_KeyBind);
            keybindComboBoxes.Add(ComboBox_A_KeyBind);
            keybindComboBoxes.Add(ComboBox_Y_KeyBind);
            keybindComboBoxes.Add(ComboBox_B_KeyBind);
            keybindComboBoxes.Add(ComboBox_LB_KeyBind);
            keybindComboBoxes.Add(ComboBox_LT_KeyBind);
            keybindComboBoxes.Add(ComboBox_LeftStick_KeyBind);
            keybindComboBoxes.Add(ComboBox_DPadUp_KeyBind);
            keybindComboBoxes.Add(ComboBox_DPadLeft_KeyBind);
            keybindComboBoxes.Add(ComboBox_DPadRight_KeyBind);
            keybindComboBoxes.Add(ComboBox_DPadDown_KeyBind);
            keybindComboBoxes.Add(ComboBox_Back_KeyBind);

            toggleCheckboxes.Add(CheckBox_RT);
            toggleCheckboxes.Add(CheckBox_X);
            toggleCheckboxes.Add(CheckBox_RB);
            toggleCheckboxes.Add(CheckBox_Start);
            toggleCheckboxes.Add(CheckBox_RightStick);
            toggleCheckboxes.Add(CheckBox_A);
            toggleCheckboxes.Add(CheckBox_Y);
            toggleCheckboxes.Add(CheckBox_B);
            toggleCheckboxes.Add(CheckBox_LB);
            toggleCheckboxes.Add(CheckBox_LT);
            toggleCheckboxes.Add(CheckBox_LeftStick);
            toggleCheckboxes.Add(CheckBox_DPadUp);
            toggleCheckboxes.Add(CheckBox_DPadLeft);
            toggleCheckboxes.Add(CheckBox_DPadRight);
            toggleCheckboxes.Add(CheckBox_DPadDown);
            toggleCheckboxes.Add(CheckBox_Back);
            toggleCheckboxes.Add(main.checkbox_AutoWarpFix);

            KeyBinds = new string[]
            {
                "RT",
                "X",
                "RB",
                "Start",
                "RightStick",
                "A",
                "Y",
                "B",
                "LB",
                "LT",
                "LeftStick",
                "DPadUp",
                "DPadLeft",
                "DPadRight",
                "DPadDown",
                "Back",
            };

            PopulateComboBoxFromList();
            LoadUserPreferences();
            windowLoading = false;
        }

        private void PopulateComboBoxFromList()
        {
            foreach (var cb in modComboBoxes)
            {
                PopulateComboBoxWithMods(cb);
            }
            foreach (var cb in keybindComboBoxes)
            {
                PopulateComboBoxWithKeyBinds(cb);
            }
        }
        private void PopulateComboBoxWithKeyBinds(ComboBox cb)
        {
            foreach (string keyBind in KeyBinds)
            {
                cb.Items.Add(keyBind);
            }
        }
        private void PopulateComboBoxWithMods(ComboBox comboBox)
        {
            DirectoryInfo d = new DirectoryInfo("Content/Commands/");

            FileInfo[] Files = d.GetFiles("*.txt"); // Get text files

            foreach (FileInfo file in Files)
            {
                comboBox.Items.Add(file.Name.Split('.')[0]);
            }
        }
        private void ModComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (windowLoading) return;
            if (sender is ComboBox comboBox)
            {
                string comboBoxName = comboBox.Name;
                string selectedValue = comboBox.SelectedItem?.ToString() ?? "";

                string buttonName = comboBoxName.Split('_')[1];

                CheckBox checkBoxEnableState = (CheckBox)FindName("CheckBox_" + buttonName);
                if (checkBoxEnableState == null) return;
                checkBoxEnableState.IsChecked = false;
            }
        }
        private void checkBox_KeyBindEnabled(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox)
            {
                string buttonName = checkBox.Name.Split('_')[1];
                bool checkBoxEnabled = checkBox.IsEnabled;

                ComboBox comboBoxmods = (ComboBox)FindName("ComboBox_" + buttonName + "_Mod");
                ComboBox comboBoxKeyBind = (ComboBox)FindName("ComboBox_" + buttonName + "_KeyBind");
                ComboBox comboBoxEnableState = (ComboBox)FindName("ComboBox_" + buttonName + "_EnableState");

                if (checkBoxEnabled)
                {

                    if (comboBoxKeyBind.SelectionBoxItem.ToString() != "None")
                    {
                        KeyBinds = new string[]
                        {
                            buttonName,
                            comboBoxKeyBind.SelectionBoxItem.ToString()
                        };
                    }
                    else
                    {
                        KeyBinds = new string[]
                        {
                            buttonName
                        };
                    }

                    // Add or remove the item from the list based on checkbox state
                    if (checkBox.IsChecked == true)
                    {
                        main.CameraControls.c_List_Infos_L.Add(new C_List_Info(comboBoxmods.SelectionBoxItem.ToString(), KeyBinds, comboBoxEnableState.SelectionBoxItem.ToString(), false));
                    }
                    else
                    {
                        // Remove the item from the list
                        main.CameraControls.c_List_Infos_L.RemoveAll(item =>
                            item.c_Name == comboBoxmods.SelectionBoxItem.ToString() &&
                            item.c_KeyBind.SequenceEqual(KeyBinds));
                    }

                    SaveUserPreferences();
                }
                else
                {
                    // Remove the item from the list
                    main.CameraControls.c_List_Infos_L.RemoveAll(item =>
                        item.c_Name == comboBoxmods.SelectionBoxItem.ToString() &&
                        item.c_KeyBind.SequenceEqual(KeyBinds));
                    SaveUserPreferences();
                }
            }
        }

        public class C_List_Info
        {
            public string c_Name;
            public string[] c_KeyBind;
            public string c_action;
            public bool c_toggle;

            public C_List_Info(string c_Name, string[] p_keyBind, string c_action, bool c_toggle)
            {
                this.c_Name = c_Name;
                this.c_KeyBind = p_keyBind;
                this.c_action = c_action;
                this.c_toggle = c_toggle;
            }
        }
        [Serializable]
        public class UserPreferences
        {
            public Dictionary<string, string> SelectedMods { get; set; }
            public Dictionary<string, int> ModActionIndexes { get; set; }
            public Dictionary<string, string> SelectedKeyBinds { get; set; }
            public Dictionary<string, bool> CheckboxStates { get; set; }
            public List<C_List_Info> ActivekeyBind { get; set; }

            public UserPreferences()
            {
                SelectedMods = new Dictionary<string, string>();
                ModActionIndexes = new Dictionary<string, int>();
                SelectedKeyBinds = new Dictionary<string, string>();
                CheckboxStates = new Dictionary<string, bool>();
                ActivekeyBind = new List<C_List_Info>();
            }
        }
        private void SaveUserPreferences()
        {
            if (windowLoading) return;
            var userPreferences = new UserPreferences();

            // Store selected items in ComboBoxes
            foreach (var comboBox in modComboBoxes)
            {
               userPreferences.SelectedMods[comboBox.Name] = comboBox.SelectedItem.ToString();
            }
            foreach (var comboBox in actionComboBoxes)
            {
                userPreferences.ModActionIndexes[comboBox.Name] = comboBox.SelectedIndex;
            }

            foreach (var comboBox in keybindComboBoxes)
            {
                userPreferences.SelectedKeyBinds[comboBox.Name] = comboBox.SelectedItem.ToString();
            }
            // Store checkbox states
            foreach (var checkBox in toggleCheckboxes)
            {
                userPreferences.CheckboxStates[checkBox.Name] = checkBox.IsChecked ?? false;
            }
            userPreferences.ActivekeyBind = main.CameraControls.c_List_Infos_L;



            // Serialize the UserPreferences object to JSON and save it to a file
            string json = JsonConvert.SerializeObject(userPreferences);
            File.WriteAllText("user_preferences.json", json);
        }
        private void LoadUserPreferences()
        {
            if (File.Exists("user_preferences.json"))
            {
                string json = File.ReadAllText("user_preferences.json");
                var userPreferences = JsonConvert.DeserializeObject<UserPreferences>(json);

                // Load selected items into ComboBoxes
                foreach (var comboBox in modComboBoxes)
                {
                    if (userPreferences.SelectedMods.ContainsKey(comboBox.Name))
                    {
                        comboBox.SelectedItem = userPreferences.SelectedMods[comboBox.Name];
                    }
                }

                foreach (var comboBox in actionComboBoxes)
                {
                    if (userPreferences.ModActionIndexes.ContainsKey(comboBox.Name))
                    {
                        comboBox.SelectedIndex = userPreferences.ModActionIndexes[comboBox.Name];
                    }
                }

                foreach (var comboBox in keybindComboBoxes)
                {
                    if (userPreferences.SelectedKeyBinds.ContainsKey(comboBox.Name))
                    {
                        comboBox.SelectedItem = userPreferences.SelectedKeyBinds[comboBox.Name];
                    }
                }

                // Load checkbox states
                foreach (var checkBox in toggleCheckboxes)
                {
                    if (userPreferences.CheckboxStates.ContainsKey(checkBox.Name))
                    {
                        checkBox.IsChecked = userPreferences.CheckboxStates[checkBox.Name];
                    }
                }

                main.CameraControls.c_List_Infos_L = userPreferences.ActivekeyBind;

            }
        }





    }
}
