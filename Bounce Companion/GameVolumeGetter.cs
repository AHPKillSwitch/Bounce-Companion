using System;
using System.Runtime.InteropServices;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;

namespace Bounce_Companion
{
    public class GameVolumeGetter
    {
        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        public float GetGameVolumeLevel(MainWindow main)
        {
            IntPtr hwnd = FindWindow(null, "Halo 2 - Project Cartographer"); // Replace "Halo 2" with the actual window title of halo2.exe
            if (hwnd == IntPtr.Zero)
            {
                Console.WriteLine("Halo 2 window not found.");
                return -1;
            }

            uint processId;
            GetWindowThreadProcessId(hwnd, out processId);

            var deviceEnumerator = new MMDeviceEnumerator();
            var devices = deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);

            foreach (var device in devices)
            {
                if (device.AudioSessionManager != null)
                {
                    var sessionCount = device.AudioSessionManager.Sessions.Count;

                    for (int i = 0; i < sessionCount; i++)
                    {
                        var session = device.AudioSessionManager.Sessions[i];
                        if (session.GetProcessID == processId)
                        {
                            var audioVolume = session.SimpleAudioVolume;
                            float masterVolume = audioVolume.Volume;
                            Console.WriteLine($"Process {processId}: {session.DisplayName}");
                            Console.WriteLine($"Master Volume: {masterVolume}");
                            return masterVolume;
                        }
                    }
                }
            }

            Console.WriteLine("Halo 2 process not found in audio sessions.");
            return -1;
        }

    }


    
}
