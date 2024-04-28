using SharpDX.XInput;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Bounce_Companion.MainWindow;
using System.Windows.Input;
using static Bounce_Companion.ControllerKeyBinds;
using System.Net;
using System.Windows;
using Bounce_Companion.Code.Command_Handler;

namespace Bounce_Companion.Code.Camera_Tool
{
    internal class CameraControls
    {
        private Mem m;
        private Controller xboxController;
        private CameraInterpolation interpolation;
        private CommandHandler c_Handler;
        private CameraTool cameraTool;
        private System.Threading.Timer toggleTimer;

        
        private float[] c_angle;

        // Camera Transition speed
        public float c_GlobalTransitionTime = 0f;
        public int loopDelayTime = 100;

        //camera Addresses
        public int xAddress = 0;
        public int yAddress = 0;
        public int zAddress = 0;
        public int yawAddress = 0;
        public int pitchAddress = 0;
        public int rollAddress = 0;

        //camera flyspeeds
        public float c_moveSpeed = 0f;
        public float c_turnSpeed = 0f;
        public float c_pitchSpeed = 0f;
        public float c_heightSpeed = 0f;
        public float c_rollSpeed = 0f;


        public CameraControls(Mem M, CameraInterpolation Interpolation)
        {
            m = M;
            interpolation = Interpolation;
            toggleTimer = new System.Threading.Timer(ToggleCallback, null, Timeout.Infinite, Timeout.Infinite);

            _ = InitializeXboxController();
            SetCameraAddresses();

            // Initialize key states
            foreach (ControllerKey key in Enum.GetValues(typeof(ControllerKey)))
            {
                keyStates[key] = false;
            }
        }
        public const float DeadbandThreshold = 0.5f;
        public bool flyCamControl = false;
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
        private async Task InitializeXboxController()
        {
            xboxController = new Controller(UserIndex.One);

            while (true)
            {
                await HandleXboxControllerInputAsync();
                await Task.Delay(1); // Delay between controller input handling
            }
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
                                if (!cameraTool.rollCamera && flyCamControl)
                                {
                                    await interpolation.MoveCameraPositionAsync(xAxisInput, yAxisInput, yawAxisInput, pitchAxisInput, leftTrigger, rightTrigger, leftShoulderPressed, rightShoulderPressed);
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
                    c_Handler.ApplyMods(action, command);
                    obj.c_toggle = action;
                    toggleTimer.Change(500, Timeout.Infinite); // Adjust the delay as needed (e.g., 1000 milliseconds)
                    isTimerRunning = true;
                }
            }
        }
        public void MoveCameraPosition(float x, float y, float z, float yaw, float pitch, float roll)
        {
            // Write the camera position and rotation values to memory
            m.WriteToMemory(xAddress, "float", x.ToString());
            m.WriteToMemory(yAddress, "float", y.ToString());
            m.WriteToMemory(zAddress, "float", z.ToString());
            m.WriteToMemory(yawAddress, "float", yaw.ToString());
            m.WriteToMemory(pitchAddress, "float", pitch.ToString());
            m.WriteToMemory(rollAddress, "float", roll.ToString());
        }
        public float[] GetCameraData(out byte[] cameraPosition)
        {
            int c_Address = 0;
            c_Address = m.ReadInt32("halo2.exe+0x004D84EC");

            c_angle = new float[12];
            int j = 6;
            for (int i = 0; i < 36; i++)
            {
                if (i < 6)
                {
                    c_angle[i] = m.ReadFloat(c_Address + i * 0x4);
                }
                if (i == 7) i = 29;
                if (i > 29)
                {
                    c_angle[j] = m.ReadFloat(c_Address + i * 0x4);
                    j++;
                }

            }
            cameraPosition = m.ReadBytes(c_Address, 0x14);
            return c_angle;
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
        public void ResetOrientation(bool postion)
        {
            if (postion)
            {
                m.WriteToMemory(xAddress, "float", 0.ToString());
                m.WriteToMemory(yAddress, "float", 0.ToString());
                m.WriteToMemory(zAddress, "float", 10.ToString());

            }
            else
            {
                m.WriteToMemory(yawAddress, "float", 0.ToString());
                m.WriteToMemory(pitchAddress, "float", 0.ToString());
                m.WriteToMemory(rollAddress, "float", 0.ToString());
            }
        }
        public void SetCameraAddresses()
        {
            int cameraPositionAddress = m.ReadInt32("halo2.exe+0x004D84EC");
            xAddress = cameraPositionAddress;
            yAddress = cameraPositionAddress + 0x4;
            zAddress = cameraPositionAddress + 0x8;
            yawAddress = cameraPositionAddress + 0xC;
            pitchAddress = cameraPositionAddress + 0x10;
            rollAddress = cameraPositionAddress + 0x14;
        }
    }
}
