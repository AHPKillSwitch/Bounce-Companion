using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Memory;


namespace Bounce_Companion
{
    public class ReplaySystem
    {
        MainWindow main;
        private List<PlayerInput> recordedInputs;
        private bool recording;
        Mem m;
        public ReplaySystem(Mem Memory, MainWindow Main)
        {
            m = Memory;
            main = Main;
            recordedInputs = new List<PlayerInput>();
            recording = false;
        }
        private struct PlayerInput
        {
            public float X;
            public float Y;
            public float Z;
            public float X_Vel;
            public float Y_Vel;
            public float Z_Vel;
            public long Timestamp;
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

        public void RecordPlayerPosition(float x, float y, float z, float xVel, float yVel, float zVel)
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
                    Timestamp = Stopwatch.GetTimestamp()
                };
                recordedInputs.Add(input);
            }
        }
        private float ReadPlayerFloat(int baseAddress, int offset)
        {
            return m.ReadFloat((baseAddress + offset).ToString("X"), "");
        }
        public async Task ReplayPlayerMovements()
        {
            if (recordedInputs.Count == 0)
            {
                Console.WriteLine("No recorded inputs to replay.");
            }
            else
            {
                int p_Index = m.Read2Byte("halo2.exe+4E7C88");
                int obj_List_Address = m.ReadInt("halo2.exe+4E461C,0x44");
                int obj_List_Memory_Address = m.ReadInt((obj_List_Address + p_Index * 0xC + 0x8).ToString("X"));

                float currentX = ReadPlayerFloat(obj_List_Memory_Address, 0xC * 0x4);
                float currentY = ReadPlayerFloat(obj_List_Memory_Address, 0xD * 0x4);
                float currentZ = ReadPlayerFloat(obj_List_Memory_Address, 0xE * 0x4);

                float adjustedXVel = 0;
                float adjustedYVel = 0;
                float adjustedZVel = 0;

                long targetFrameTime = 30;
                long startTime = Stopwatch.GetTimestamp();
                 // Initialize with the player's starting position

                foreach (var input in recordedInputs)
                {
                    long elapsedTime = input.Timestamp - startTime;
                    long targetTime = elapsedTime + startTime + (targetFrameTime * Stopwatch.Frequency / 1000);

                    // Simulate the desired playback speed using a basic delay loop
                    while (Stopwatch.GetTimestamp() < targetTime)
                    {
                        // Sleep for a short time to simulate a frame delay
                        await Task.Delay(29);
                    }

                    // Calculate the normalized direction vector from the player's current position to the recorded position
                    float deltaX = input.X - currentX;
                    float deltaY = input.Y - currentY;
                    float deltaZ = input.Z - currentZ;
                    float magnitude = (float)Math.Sqrt(deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ);
                    float normalizedDeltaX = deltaX / magnitude;
                    float normalizedDeltaY = deltaY / magnitude;
                    float normalizedDeltaZ = deltaZ / magnitude;

                    // Apply the correction factor only if the recorded velocity is non-zero
                    if (input.X_Vel != 0 || input.Y_Vel != 0 || input.Z_Vel != 0)
                    {
                        float correctionX = normalizedDeltaX * 0.01f;
                        float correctionY = normalizedDeltaY * 0.01f;
                        float correctionZ = normalizedDeltaZ * 0.01f;

                        // Adjust the player's velocity components to apply the correction
                         adjustedXVel = input.X_Vel + correctionX;
                         adjustedYVel = input.Y_Vel + correctionY;
                         adjustedZVel = input.Z_Vel + correctionZ;

                        // Update the game's memory with the adjusted velocity values
                        // Example memory write code (replace with actual memory writing method):
                        // memoryWriter.WriteObjectVelocity("player_salt", objMemAddress, adjustedXVel, adjustedYVel, adjustedZVel);
                    }
                    else
                    {
                        // If recorded velocity is zero, simply use the recorded velocity values
                         adjustedXVel = input.X_Vel;
                         adjustedYVel = input.Y_Vel;
                         adjustedZVel = input.Z_Vel;

                        // Update the game's memory with the original velocity values
                        // Example memory write code (replace with actual memory writing method):
                        // memoryWriter.WriteObjectVelocity("player_salt", objMemAddress, adjustedXVel, adjustedYVel, adjustedZVel);
                    }

                    // Update the current player position based on the adjusted velocity
                    currentX += adjustedXVel;
                    currentY += adjustedYVel;
                    currentZ += adjustedZVel;

                    string hexString = main.settingsWindow.Textbox_P_Address_replay.Text;
                    int xAddress = Convert.ToInt32(hexString, 16);
                    m.WriteMemory(xAddress.ToString("X"), "float", adjustedXVel.ToString());
                    m.WriteMemory((xAddress += 0x04).ToString("X"), "float", adjustedYVel.ToString());
                    m.WriteMemory((xAddress += 0x04).ToString("X"), "float", adjustedZVel.ToString());
                    await Task.Delay(29);
                }
            }
        }
    }
}

