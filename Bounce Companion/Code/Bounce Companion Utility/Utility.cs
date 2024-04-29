using Bounce_Companion.Code.Addresses___Offsets;
using Bounce_Companion.Code.Bounce_Handler;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static Bounce_Companion.MainWindow;

namespace Bounce_Companion.Code.Bounce_Companion_Utility
{
    public class Utility
    {
        Mem m;
        MainWindow main;
        GameOverlayWindow GOW;
        BounceHandler bounceHandler;
        public Utility(Mem m, MainWindow main)
        {
            this.m = m;
            this.main = main;
        }
        
        public void PrintToConsole(string input)
        {
            main.ConsoleOut.AppendText(input + "\n");
            main.ConsoleOut.ScrollToEnd();
        }
        public void PrintToConsole(List<string> outPutStrings)
        {
            foreach (string txt in outPutStrings)
            {
                main.ConsoleOut.AppendText(txt + "\n");
            }
            main.ConsoleOut.ScrollToEnd();
        }
        public float[] ParseFloatArray(string input)
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
        public void WriteBouncePositionToString(string position)
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
        public void UpdateUI(float p_X_Vel, float p_Y_Vel, float p_Z_Vel, float p_X, float p_Y, float p_Z, int tr)
        {
            if (main.checkbox_EnableBounceStats.IsChecked == true && main.checkbox_EnableBounceXYZStats.IsChecked == true)
            {
                PrintToConsole($"Player XYZ Velocity: {p_X_Vel}, {p_Y_Vel}, {p_Z_Vel}    Player XYZ Position: {p_X}, {p_Y}, {p_Z}");
            }
            main.GOW.UpdateStatusBar(main.BounceHandler.bounceCount, p_Z_Vel, p_X, p_Y, p_Z, tr);
            main.GOW.UpdateSliders(p_X_Vel, p_Y_Vel, p_Z_Vel);
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
        public void PrintToConsole_ContinueNextText(string input)
        {
            main.ConsoleOut.AppendText(input);
            main.ConsoleOut.ScrollToEnd();
        }
        public void PrintBounceDetails(float p_X, float p_Y, float p_Z, float p_X_Vel, float p_Y_Vel, float p_Z_Vel, string bouncetype)
        {
            string details = $" - - - - - - - - Bounce Detected - - - - - - - -\n" +
                $"Triggered at location X: {p_X} Y: {p_Y} Z: {p_Z} \n" +
                $"Trigged Velocity - X: {p_X_Vel} Y: {p_Y_Vel} Z: {p_Z_Vel}\n" +
                $"Pre Bounce Velocity - X: {Globals.prev_P_X_Vel} Y: {Globals.prev_P_Y_Vel} Z: {Globals.prev_P_Z_Vel} \n" +
                $"Bounce Type: {bouncetype} \n";

            PrintToConsole(details);
        }
        public float ReadPlayerFloat(int baseAddress, int offset)
        {
            return m.ReadFloat(baseAddress + offset);
        }
        public int ReadPlayerByte(int baseAddress, int offset)
        {
            return m.ReadByte(baseAddress + offset);
        }
    }
}
