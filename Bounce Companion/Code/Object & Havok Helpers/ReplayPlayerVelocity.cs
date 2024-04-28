using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bounce_Companion.Code.Object___Havok_Helpers
{
    internal class ReplayPlayerVelocity
    {
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
    }
}
