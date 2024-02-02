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
    /// Interaction logic for NewProjectWindow.xaml
    /// </summary>
    public partial class NewProjectWindow : Window
    {
        public string ProjectName { get; private set; }
        MainWindow main;

        public NewProjectWindow( MainWindow Main)
        {
            main = Main;
            InitializeComponent();
            WindowState = WindowState.Normal;
        }

        private void CreateProjectButton_Click(object sender, RoutedEventArgs e)
        {
            ProjectName = ProjectNameTextBox.Text.Trim();
            main.textBox_ProjectName.Text = ProjectName;
            DialogResult = true;
            main.staackPanel_CameraTool.Visibility = Visibility.Visible;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
