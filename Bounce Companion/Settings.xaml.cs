using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Bounce_Companion
{
    /// <summary>
    /// Interaction logic for CameraSettings.xaml
    /// </summary>
    public partial class Settings : Window
    {
        private MainWindow main;
        public Settings(MainWindow Main)
        {
            main = Main;
            InitializeComponent();
            PopulateProcessComboBox();
            //UpdateTextBoxesFromJson();
        }

        private void UpdateCameraSpeed(object sender, RoutedEventArgs e)
        {
            main.UpdateCameraSpeed();
            _ = main.UpdateConfigWithNewOptions();
        }

        private void SetTickrate_click(object sender, RoutedEventArgs e)
        {
            int tickRateInt = int.Parse(Textbox_Tickrate.Text);
            main.SetTickrate(tickRateInt);
        }

        private void TestWTS_click(object sender, RoutedEventArgs e)
        {
            main.TestWTS();
        }

        private void ReplaySystemRecord_Click(object sender, RoutedEventArgs e)
        {
            main.replaySystem.StartRecording();
        }

        private void ReplaySystemRecordingStop_Click(object sender, RoutedEventArgs e)
        {
            main.replaySystem.StopRecording();
        }

        private void ReplaySystemReplay_Click(object sender, RoutedEventArgs e)
        {
            _ = main.replaySystem.ReplayPlayerMovements();
        }

        private void SliderSFXChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            textbox_SliderSFX.Text = (Math.Round(Slider_SFXAudio.Value, 2).ToString());
        }

        private void SliderDelayChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            textbox_SliderDelay.Text = (Math.Round(Slider_DelayAudio.Value).ToString() + "ms");
        }
        public void PopulateProcessComboBox()
        {
            ComboBox_DPadDown_EnableState.Items.Clear();

            Process[] processes = Process.GetProcessesByName("halo2");
            for (int i = 0; i < processes.Length; i++)
            {
                ComboBox_DPadDown_EnableState.Items.Add($"Halo 2 - Process {i + 1}");
            }
        }

        private void UpdateLoopDelayTime(object sender, RoutedEventArgs e)
        {
            main.loopDelayTime = int.Parse(Textbox_LoopDelay.Text);
        }

        private void ProcessSelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int selectedIndex = ComboBox_DPadDown_EnableState.SelectedIndex;
            if (selectedIndex >= 0)
            {
                main.AttachToProcess(selectedIndex);
            }
        }
    }
}
