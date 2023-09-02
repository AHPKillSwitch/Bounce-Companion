using System;
using System.Collections.Generic;
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

        }

        private void UpdateCameraSpeed(object sender, RoutedEventArgs e)
        {
            main.UpdateCameraSpeed();
            _ = main.UpdateConfigWithNewOptions();
        }

        private void ResetOrientation_click(object sender, RoutedEventArgs e)
        {
            main.ResetOrientation(false);
        }

        private void ResetPosition_click(object sender, RoutedEventArgs e)
        {
            main.ResetOrientation(true);
        }

        private void SetTickrate_click(object sender, RoutedEventArgs e)
        {
            int tickRateInt = int.Parse(Textbox_Tickrate.Text);
            main.SetTickrate(tickRateInt);
        }

        private void CallFunction_click(object sender, RoutedEventArgs e)
        {
            if (!main.Injected) main.InjectDLL();
            main.CallFunction();
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
            main.replaySystem.ReplayPlayerMovements();
        }
    }
}
