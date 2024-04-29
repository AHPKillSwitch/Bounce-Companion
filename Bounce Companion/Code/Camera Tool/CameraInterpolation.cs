using Bounce_Companion.Code.Bounce_Companion_Utility;
using Bounce_Companion.Code.Object___Havok_Helpers;
using MathNet.Numerics.Interpolation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using static Bounce_Companion.ControllerKeyBinds;
using static Bounce_Companion.MainWindow;

namespace Bounce_Companion.Code.Camera_Tool
{
    public class CameraInterpolation
    {
        private Mem m;
        private MainWindow main;
        private CameraTool.ImageData selectedImageData = new CameraTool.ImageData();



        public bool Offset_Selected_Player = false;
        public bool CTO_OffsetPlayer = false;
        public bool noClip = false;
        public int jumpToIndex = 0;

        public CameraInterpolation(MainWindow main, CameraTool cameraTool)
        {
            this.main = main;
            this.m = main.m;
        }

            
    
        public async Task MoveCameraSmoothly(List<float[]> positions, List<float> fixedSpeeds)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            main.CameraControls.SetCameraAddresses();
            main.CameraTool.rollCamera = true;
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
                if (!main.CameraTool.rollCamera) return;
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
                            salt = main.ObjectHandler.GetPlayerNameSalt(main.ComboBox_SceneData_Playernames.Text);
                        }
                        else
                        {
                            salt = main.ObjectHandler.GetPlayerNameSalt(main.ComboBox_Playernames.Text);
                        }
                    });
                    int obj_List_Memory_Address;
                    float p_X, p_Y, p_Z, p_X_Vel, p_Y_Vel, p_Z_Vel, p_pitch, p_Yaw, p_Shields;
                    main.ObjectHandler.ReadObjectXYZ(salt, out obj_List_Memory_Address, out p_X, out p_Y, out p_Z, out p_X_Vel, out p_Y_Vel, out p_Z_Vel, out p_pitch, out p_Yaw, out p_Shields);
                    float[] blah = main.CameraControls.GetCameraData(out byte[] cameraPositionArray1);
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
            main.CameraTool.rollCamera = false;
            if (main.CameraTool.loopCamera)
            {
                //MoveCameraAsyncinternal((float)positionsX[0], (float)positionsX[0], (float)positionsX[0], (float)yawValues[0], (float)pitchValues[0], (float)rollValues[0]);
                //await Task.Delay(loopDelayTime);
                await Application.Current.Dispatcher.Invoke(async () =>
                {
                    await main.CameraTool.StartCameraRoll();
                });
            }
            void MoveCameraAsyncinternal(float x, float y, float z, float yaw, float pitch, float roll)
            {
                if (selectedImageData.FacePlayer) face_Selected_Player = true;
                // Write the camera position and rotation values to memory
                m.WriteToMemory(main.CameraControls.xAddress, "float", x.ToString());
                m.WriteToMemory(main.CameraControls.yAddress, "float", y.ToString());
                m.WriteToMemory(main.CameraControls.zAddress, "float", z.ToString());

                if (!face_Selected_Player) // if we are not facing the player, Set the camera anlge
                {
                    m.WriteToMemory(main.CameraControls.yawAddress, "float", yaw.ToString());
                    m.WriteToMemory(main.CameraControls.pitchAddress, "float", pitch.ToString());
                    m.WriteToMemory(main.CameraControls.rollAddress, "float", roll.ToString());
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
                m.WriteToMemory(main.CameraControls.yawAddress, "float", yawRad.ToString());
                m.WriteToMemory(main.CameraControls.pitchAddress, "float", pitchRad.ToString());

            }
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
        public async Task MoveCameraPositionAsync(float yAxisInput, float xAxisInput, float controllerXInput, float controllerYInput, float leftTrigger, float rightTrigger, bool leftShoulderPressed, bool rightShoulderPressed)
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
                    salt = main.ComboBox_Playernames.Text.Split(':')[1];
                });


                int obj_List_Memory_Address;
                float p_X, p_Y, p_Z, p_X_Vel, p_Y_Vel, p_Z_Vel, p_pitch, p_Yaw, p_Shields;
                main.ObjectHandler.ReadObjectXYZ(salt, out obj_List_Memory_Address, out p_X, out p_Y, out p_Z, out p_X_Vel, out p_Y_Vel, out p_Z_Vel, out p_pitch, out p_Yaw, out p_Shields);

                cameraX = p_X;
                cameraY = p_Y;
                cameraZ = p_Z;
                cameraYaw = p_Yaw;
                cameraPitch = p_pitch;
            }
            else
            {
                main.CameraControls.SetCameraAddresses();
                float[] cameraPosition = main.CameraControls.GetCameraData(out byte[] cameraPositionArray1);
                cameraX = cameraPosition[0];
                cameraY = cameraPosition[1];
                cameraZ = cameraPosition[2];
                cameraYaw = cameraPosition[3];
                cameraPitch = cameraPosition[4];
                cameraRoll = cameraPosition[5];
            }

            // Apply the controller input to the camera yaw, pitch, and roll
            cameraYaw -= controllerXInput * (main.CameraControls.c_turnSpeed / 100);
            cameraPitch -= controllerYInput * (main.CameraControls.c_pitchSpeed / 100);
            cameraRoll += (rightShoulderPressed ? 1 : 0) * (main.CameraControls.c_rollSpeed / 100) - (leftShoulderPressed ? 1 : 0) * (main.CameraControls.c_rollSpeed / 100);

            //// Normalize the yaw value to stay within the range of -π to π
            //if (cameraYaw < -Math.PI)
            //    cameraYaw += 2 * (float)Math.PI;
            //else if (cameraYaw > Math.PI)
            //    cameraYaw -= 2 * (float)Math.PI;

            // Limit the pitch value to stay within a desired range (e.g., -π/2 to π/2 for looking up and down)
            //cameraPitch = (float)Math.Clamp(cameraPitch, -Math.PI / 2, Math.PI / 2);

            // Calculate movement based on left joystick inputs
            float moveX = xAxisInput * (main.CameraControls.c_moveSpeed / 100);
            float moveY = yAxisInput * (main.CameraControls.c_moveSpeed / 100);

            // Calculate the forward vector based on the camera's yaw
            float forwardX = (float)Math.Sin(cameraYaw);
            float forwardY = (float)Math.Cos(cameraYaw);

            // Update the camera position, yaw, pitch, and height based on input
            cameraX += forwardX * moveY - forwardY * moveX;
            cameraY -= forwardY * moveY + forwardX * moveX;
            cameraZ -= (leftTrigger - rightTrigger) * (main.CameraControls.c_heightSpeed / 100);

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
                        baseCoordinate = int.Parse(main.settingsWindow.Textbox_P_Address_replay.Text);
                    });
                }
                else baseCoordinate = main.CameraControls.xAddress;
                // Write the camera position and rotation values to memory
                m.WriteToMemory(baseCoordinate, "float", x.ToString());
                m.WriteToMemory(baseCoordinate + 4, "float", y.ToString());
                m.WriteToMemory(baseCoordinate + 8, "float", z.ToString());

                if (!face_Selected_Player || !selectedImageData.FacePlayer) // if we are not facing the player, Set the camera anlge
                {
                    if (noClip) return;
                    m.WriteToMemory(main.CameraControls.yawAddress, "float", yaw.ToString());
                    m.WriteToMemory(main.CameraControls.pitchAddress, "float", pitch.ToString());
                    m.WriteToMemory(main.CameraControls.rollAddress, "float", roll.ToString());
                }
            }

        }
        
        public async Task OffsetObjectPositionContinuous(string salt)
        {
            while (Offset_Selected_Player)
            {
                try
                {
                    int obj_List_Memory_Address;
                    float p_X, p_Y, p_Z, p_X_Vel, p_Y_Vel, p_Z_Vel, p_Yaw, p_pitch, p_Shields;
                    main.ObjectHandler.ReadObjectXYZ(salt, out obj_List_Memory_Address, out p_X, out p_Y, out p_Z, out p_X_Vel, out p_Y_Vel, out p_Z_Vel, out p_Yaw, out p_pitch, out p_Shields);
                    float[] cameraPosition = main.CameraControls.GetCameraData(out byte[] cameraPositionArray1);
                    float[] objectPosition = { p_X, p_Y, p_Z };
                    if (face_Selected_Player)
                    {
                        UpdateCameraAngle(objectPosition, cameraPosition, 1f);
                    }

                    await OffsetObjectPosition(objectPosition, cameraPosition);
                    await Task.Delay(32); // Delay between offset calculations (adjust as needed)
                }
                catch
                { MessageBox.Show("Error in OffsetObjectPositionContinuous"); Offset_Selected_Player = false; main.CheckBox_OffsetPlayer.IsChecked = false; }
            }
        }

        private async Task OffsetObjectPosition(float[] objectPosition, float[] cameraPosition)
        {
            float[] offsetPosition = new float[3];
            // Calculate the offset position
            if (!CTO_OffsetPlayer)
            {
                selectedImageData = main.CameraTool.imageDataList[jumpToIndex];
                if (selectedImageData.SpectatePlayer)
                {
                    offsetPosition[0] = objectPosition[0] + RemoveNonDigitChars(selectedImageData.CameraOffsetsArray[0]);
                    offsetPosition[1] = objectPosition[1] + RemoveNonDigitChars(selectedImageData.CameraOffsetsArray[1]);
                    offsetPosition[2] = objectPosition[2] + RemoveNonDigitChars(selectedImageData.CameraOffsetsArray[2]);
                }
            }
            else
            {
                offsetPosition[0] = objectPosition[0] + RemoveNonDigitChars(main.TextBox_Offset_X.Text);
                offsetPosition[1] = objectPosition[1] + RemoveNonDigitChars(main.TextBox_Offset_Y.Text);
                offsetPosition[2] = objectPosition[2] + RemoveNonDigitChars(main.TextBox_Offset_Z.Text);
            }

            // Use memory.dll or any other appropriate library to write the offset position to memory
            await WriteOffsetPositionToMemory(offsetPosition);
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
            m.WriteToMemory(main.CameraControls.yawAddress, "float", yawRad.ToString());
            m.WriteToMemory(main.CameraControls.pitchAddress, "float", pitchRad.ToString());
        }
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
            m.WriteToMemory(main.CameraControls.xAddress, "float", offsetPosition[0].ToString());
            m.WriteToMemory(main.CameraControls.yAddress, "float", offsetPosition[1].ToString());
            m.WriteToMemory(main.CameraControls.zAddress, "float", offsetPosition[2].ToString());

            return Task.CompletedTask;
        }
    }
}
