using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static Bounce_Companion.KeyBoardBinds;
using static Bounce_Companion.MainWindow;
using System.Windows.Automation;
using Bounce_Companion.Code.Command_Handler;
//using System.Windows.Forms;

namespace Bounce_Companion
{
    /// <summary>
    /// Interaction logic for KeyBoardBinds.xaml
    /// </summary>
    public partial class KeyBoardBinds : Window, INotifyPropertyChanged
    {
        private ObservableCollection<Keybind> keybinds = new ObservableCollection<Keybind>();
        MainWindow main;
        private bool isRecording = false;
        private static HashSet<Key> currentlyPressedKeys = new HashSet<Key>();
        public KeyBoardBinds(MainWindow Main)
        {
            main = Main;
            InitializeComponent();
            // Populate combo boxes
            KeyboardHook.Start();
            MouseHook.Start(); // Add this line to start the mouse hook
            PopulateComboBoxWithMods(modListComboBox);
            KeyboardHook.KeyIntercepted += KeyboardHook_KeyIntercepted;
            MouseHook.MouseWheelScrolled += MouseHook_MouseWheelScrolled; // Add this line to handle mouse wheel events
            // Set DataContext to the current instance of KeyBoardBinds
            DataContext = this;
        }
        private void MouseHook_MouseWheelScrolled(object sender, MouseHook.MouseWheelEventArgs e)
        {
            // Handle the mouse wheel scroll event
            int delta = e.Delta;
            //AdjustWalkSpeed(delta);
        }
        public ObservableCollection<Keybind> Keybinds
        {
            get { return keybinds; }
            set
            {
                if (value != keybinds)
                {
                    keybinds = value;
                }
            }
        }
        public event PropertyChangedEventHandler PropertyChanged
        {
            add
            {
                ((INotifyPropertyChanged)keybinds).PropertyChanged += value;
            }

            remove
            {
                ((INotifyPropertyChanged)keybinds).PropertyChanged -= value;
            }
        }

        private void PopulateComboBoxWithMods(ComboBox comboBox)
        {
            DirectoryInfo d = new DirectoryInfo("Content/Commands/");

            FileInfo[] Files = d.GetFiles("*.txt");

            foreach (FileInfo file in Files)
            {
                comboBox.Items.Add(file.Name.Split('.')[0]);
            }
        }
        private void RecordButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isRecording)
            {
                keys.Clear();
                // Start recording
                RecordButton.Content = "Stop";
                isRecording = true;

                // Clear the recorded keys label
                recordedKeysLabel.Content = "Recorded Keys: ";

                // Clear the recorded keys collection
                recordedKeys.Clear();

                
            }
            else
            {
                // Stop recording
                RecordButton.Content = "Record";
                isRecording = false;

            }
        }
        // Handle ToggleButton Click event to update enable/disable state
        private void ToggleButton_Click(object sender, RoutedEventArgs e)
        {
            var toggleButton = (ToggleButton)sender;
            var keybind = (Keybind)toggleButton.DataContext;
            keybind.IsEnabled = toggleButton.IsChecked ?? false;
        }
        // Handle Remove Button Click event to remove the item from keybinds collection
        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var keybind = (Keybind)button.DataContext;
            keybinds.Remove(keybind);
        }

        private HashSet<Key> recordedKeys = new HashSet<Key>();
        public Dictionary<Key, bool> keyStates = new Dictionary<Key, bool>();
        private List<string> keys = new List<string>();
        private void KeyboardHook_KeyIntercepted(object sender, KeyInterceptedEventArgs e)
        {
            if (isRecording && recordedKeys.Add(e.Key))
            {
                // Add the recorded key to the ModAction ComboBox
                Dispatcher.Invoke(() =>
                {
                    // Update the recorded keys label
                    recordedKeysLabel.Content += $"{e.Key} ";
                    keys.Add(e.Key.ToString());
                });
            }
            else
            {
                // Update the key state
                if (e.IsPressed)
                {
                    // Handle the key press event
                    CheckGlobalHotkeys();
                }
            }
        }

        private void CheckGlobalHotkeys()
        {
            foreach (var keybind in keybinds)
            {
                // Convert the keybind strings to a HashSet<Key> for easy comparison
                HashSet<Key> requiredKeys = new HashSet<Key>(keybind.Keys.Select(keyStr => (Key)Enum.Parse(typeof(Key), keyStr)));

                // Check if the currently pressed keys contain all required keys
                bool allRequiredKeysPressed = requiredKeys.All(key => currentlyPressedKeys.Contains(key));

                // Check if the currently pressed keys match the key pair and all required keys are pressed
                if (allRequiredKeysPressed && keybind.IsEnabled)
                {
                    // Execute the action associated with the keybind
                    ExecuteKeybindAction(keybind);
                }
            }
        }






        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            // Add the selected keybind to the list
            string selectedMod = modListComboBox.SelectionBoxItem?.ToString();
            string selectedAction = modActionComboBox.SelectionBoxItem?.ToString();
            List<string> Keybind_Keys = keys.ToList();

            if (!string.IsNullOrEmpty(selectedMod) && !string.IsNullOrEmpty(selectedAction))
            {
                keybinds.Add(new Keybind { Mod = selectedMod, Action = selectedAction, Keys = Keybind_Keys });
                keys.Clear();
                // Clear the recorded keys label
                recordedKeysLabel.Content = "Recorded Keys: ";
            }
        }

        private void ExecuteKeybindAction(Keybind keybind)
        {
            bool togglestate = keybind.Toggle;
            bool action = false;
            if (keybind.Action == "Enable Mod") action = true;
            if (keybind.Action == "Disable Mod") action = false;
            if (keybind.Action == "Toggle Mod") action = !togglestate; // Toggle the state
            keybind.Toggle = action;
            main.CommandHandler.ApplyMods(action, keybind.Mod);
        }
        public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        public static class KeyboardHook
        {
            private static IntPtr keyboardHookId = IntPtr.Zero;
            private static LowLevelKeyboardProc keyboardProc;

            public static event EventHandler<KeyInterceptedEventArgs> KeyIntercepted;

            public static void Start()
            {
                keyboardProc = KeyboardHookCallback;
                keyboardHookId = SetHook(keyboardProc, 13); // 13 corresponds to WH_KEYBOARD_LL
                MouseHook.Start();
            }

            public static void Stop()
            {
                UnhookWindowsHookEx(keyboardHookId);
                MouseHook.Stop();
            }

            private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

            private static IntPtr SetHook(LowLevelKeyboardProc proc, int hookType)
            {
                using (var curProcess = System.Diagnostics.Process.GetCurrentProcess())
                using (var curModule = curProcess.MainModule)
                {
                    return SetWindowsHookEx(hookType, proc, GetModuleHandle(curModule.ModuleName), 0);
                }
            }

            private static IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
            {
                if (nCode >= 0 && (wParam == (IntPtr)0x0100 || wParam == (IntPtr)0x0101)) // WM_KEYDOWN or WM_KEYUP
                {
                    int vkCode = Marshal.ReadInt32(lParam);
                    Key key = KeyInterop.KeyFromVirtualKey(vkCode);

                    if (wParam == (IntPtr)0x0100) // KeyDown
                    {
                        currentlyPressedKeys.Add(key);
                    }
                    else if (wParam == (IntPtr)0x0101) // KeyUp
                    {
                        currentlyPressedKeys.Remove(key);
                    }

                    // Call your custom event handler
                    KeyIntercepted?.Invoke(null, new KeyInterceptedEventArgs(key, wParam == (IntPtr)0x0100));
                }

                return CallNextHookEx(keyboardHookId, nCode, wParam, lParam);
            }
            #region DLL Imports

            [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

            [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool UnhookWindowsHookEx(IntPtr hhk);

            [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

            [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            private static extern IntPtr GetModuleHandle(string lpModuleName);

            #endregion
        }
        public static class MouseHook
        {
            private static IntPtr mouseHookId = IntPtr.Zero;
            private static MouseLowLevelMouseProc mouseProc;

            public static event EventHandler<MouseWheelEventArgs> MouseWheelScrolled;

            public static void Start()
            {
                mouseProc = MouseHookCallback;
                mouseHookId = SetHook(mouseProc, 14); // 14 corresponds to WH_MOUSE_LL
            }

            public static void Stop()
            {
                UnhookWindowsHookEx(mouseHookId);
            }
            public delegate IntPtr MouseLowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

            private static IntPtr SetHook(MouseLowLevelMouseProc proc, int hookType)
            {
                using (var curProcess = System.Diagnostics.Process.GetCurrentProcess())
                using (var curModule = curProcess.MainModule)
                {
                    return SetWindowsHookEx(hookType, proc, GetModuleHandle(curModule.ModuleName), 0);
                }
            }

            private static IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
            {
                if (nCode >= 0 && wParam == (IntPtr)0x020A) // WM_MOUSEWHEEL
                {
                    long rawDelta = lParam.ToInt64(); // Use ToInt64 instead of ToInt32
                    int delta = (int)((rawDelta >> 16) & 0xFFFF); // Cast to int

                    MouseWheelScrolled?.Invoke(null, new MouseWheelEventArgs(delta));
                }

                return CallNextHookEx(mouseHookId, nCode, wParam, lParam);
            }


            public class MouseWheelEventArgs : EventArgs
            {
                public int Delta { get; }

                public MouseWheelEventArgs(int delta)
                {
                    Delta = delta;
                }
            }
            #region DLL Imports

            [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            private static extern IntPtr SetWindowsHookEx(int idHook, MouseLowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

            [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool UnhookWindowsHookEx(IntPtr hhk);

            [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

            [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            private static extern IntPtr GetModuleHandle(string lpModuleName);

            #endregion
        }



        public class KeyInterceptedEventArgs : EventArgs
        {
            public Key Key { get; }
            public bool IsPressed { get; }

            public KeyInterceptedEventArgs(Key key, bool isPressed)
            {
                Key = key;
                IsPressed = isPressed;
            }
        }
        // Add the following property to your Keybind class to represent the enable/disable state
        public class Keybind : INotifyPropertyChanged
        {
            private bool isEnabled;
            private string enabledIndicator = "false";
            private Brush enabledIndicatorColor;

            public string Mod { get; set; }
            public string Action { get; set; }
            public List<string> Keys { get; set; }

            public bool Toggle {  get; set; }

            public bool IsEnabled
            {
                get { return isEnabled; }
                set
                {
                    if (value != isEnabled)
                    {
                        isEnabled = value;
                        UpdateProperty(nameof(IsEnabled));
                    }
                }
            }

            public string EnabledIndicator
            {
                get { return enabledIndicator; }
                set
                {
                    if (value != enabledIndicator)
                    {
                        enabledIndicator = value;
                        OnPropertyChanged(nameof(EnabledIndicator));
                    }
                }
            }
            public string DisplayKeys
            {
                get
                {
                    // Convert the key strings to Key objects
                    IEnumerable<Key> keys = Keys.Select(keyStr => (Key)Enum.Parse(typeof(Key), keyStr));

                    // Format the keys for display
                    return string.Join(" + ", keys.Select(key => key.ToString()));
                }
            }
            public Brush EnabledIndicatorColor
            {
                get { return enabledIndicatorColor; }
                set
                {
                    if (value != enabledIndicatorColor)
                    {
                        enabledIndicatorColor = value;
                        OnPropertyChanged(nameof(EnabledIndicatorColor));
                    }
                }
            }

            private void UpdateProperty(string propertyName)
            {
                // PropertyChanged?.Invoke(propertyName);
                OnPropertyChanged(propertyName);

                if (propertyName == nameof(IsEnabled))
                {
                    EnabledIndicator = isEnabled ? "Yes" : "No";
                    EnabledIndicatorColor = isEnabled ? Brushes.Green : Brushes.Red;
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;

            protected virtual void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }



    }
}

