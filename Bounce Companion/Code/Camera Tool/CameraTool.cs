using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Bounce_Companion.MainWindow;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;
using System.Windows.Controls;
using Bounce_Companion.Code.Bounce_Companion_Utility;
using System.IO;
using Newtonsoft.Json;
using Microsoft.Win32;

namespace Bounce_Companion.Code.Camera_Tool
{
    internal class CameraTool
    {
        CameraControls cameraControls;
        MainWindow main;
        Settings settingsWindow;
        Utility utility;

        List<float[]> CameraPositionArrayList = new List<float[]>();

        public int jumpToIndex = 0;

        public CameraTool(CameraControls cameraControls, MainWindow main, Settings settingsWindow, Utility utility) 
        {
            this.main = main;
            this.cameraControls = cameraControls;
            Globals.imageDataList = new List<ImageData>();
            this.settingsWindow = settingsWindow;
            this.utility = utility;

        }
        public class ImageData
        {
            public float[] CameraPosition { get; set; }
            public float TransitionTime { get; set; }
            public string Notes { get; set; }
            public byte[] CameraPositionArray { get; set; }
            public bool FacePlayer { get; set; }
            public bool SpectatePlayer { get; set; }
            public string[] CameraOffsetsArray { get; set; }
            public string SelctedPlayerString { get; set; }
            public string ImageFileName { get; set; }
        }
        public class TimeLine
        {
            public string sceneImage { get; set; }
            public byte[] CameraData { get; set; }
            public int transitionTime { get; set; }
        }
        public void JumpCameraToScene(int index)
        {
            if (index == -1)
            {
                index = Globals.jumpToIndex;
            }
            if (Globals.imageDataList.Count == 0) return;
            // Retrieve and display the selected image data
            if (index > -1)
            {
                Globals.selectedImageData = Globals.imageDataList[index];
            }
            cameraControls.MoveCameraPosition(Globals.selectedImageData.CameraPosition[0], selectedImageData.CameraPosition[1], selectedImageData.CameraPosition[2], selectedImageData.CameraPosition[3], selectedImageData.CameraPosition[4], selectedImageData.CameraPosition[5]);
        }
        private void InsertSceneAtSelected(int selectedIndex, bool insertBefore)
        {
            if (selectedIndex >= 0 && selectedIndex <= Globals.imageDataList.Count)
            {
                // Retrieve data from text boxes
                float[] cameraPosition = cameraControls.GetCameraData(out byte[] cameraPositionArray);
                float transitionTime = float.Parse(main.GlobalTransitionTimeTextBox.Text);
                CameraPositionArrayList.Add(cameraPosition);

                // Generate a unique filename for the image
                string imageFileName = Guid.NewGuid().ToString() + ".png";

                // Create new image data
                var newImageData = new ImageData
                {
                    CameraPosition = cameraPosition,
                    TransitionTime = transitionTime,
                    CameraPositionArray = cameraPositionArray,
                    FacePlayer = false,
                    SpectatePlayer = false,
                    CameraOffsetsArray = new string[3] { "0.00", "0.00", "0.00" },
                    SelctedPlayerString = "",
                    ImageFileName = imageFileName
                };

                // Calculate the index for insertion
                int insertIndex = insertBefore ? selectedIndex : selectedIndex + 1;

                // Insert the new image data at the calculated index
                Globals.imageDataList.Insert(insertIndex, newImageData);

                // Update the UI - Add a new stack panel at the calculated index
                InsertStackPanel(insertIndex, imageFileName);

                // Update the text blocks in the UI
                UpdateTextBlocks();

                // Save the new image
                var bitmapSource = ScreenshotHelper.GetBitmapThumbnailAsync(320, 240);
                if (bitmapSource != null)
                {
                    // Use the Documents folder and specific path
                    string imagePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", "Halo 2", "Bounce Companion", "Camera Paths", Globals.currentProjectName, imageFileName);

                    // Save the bitmap source to the specified path
                    SaveBitmapSourceToFile((BitmapSource)bitmapSource, imagePath);
                    // Update the image source in the UI
                    UpdateImageSource(insertIndex, bitmapSource);
                }
            }
        }
        public void SaveSceneDataToFile(List<ImageData> imageDataList)
        {
            var dialog = new SaveFileDialog();
            dialog.InitialDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", "Halo 2", "Bounce Companion", "Camera Paths", Globals.currentProjectName);
            dialog.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";

            if (dialog.ShowDialog() == true)
            {
                string filePath = dialog.FileName;
                string jsonData = JsonConvert.SerializeObject(imageDataList, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(filePath, jsonData);
            }
        }
        private void SaveBitmapSourceToFile(BitmapSource bitmapSource, string fileName)
        {
            // Get the Documents folder
            string documentsFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            // Combine the Documents folder with the additional path
            string folderPath = Path.Combine(documentsFolder, "My Games", "Halo 2", "Bounce Companion", "Camera Paths");

            // Ensure the directory exists
            Directory.CreateDirectory(folderPath);

            // Combine the folder path and file name to get the full file path
            string filePath = Path.Combine(folderPath, fileName);


            // Create the file and save the bitmap
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                BitmapEncoder encoder = new PngBitmapEncoder(); // Change to the appropriate encoder if needed
                encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                encoder.Save(stream);
            }
        }
        private void ClearTimelineData()
        {
            // Clear the stack panel and data list
            main.ImageStackPanel.Children.Clear();
            Globals.imageDataList.Clear();

            // Hide the large image and data panel
            main.LargeImage.Source = null;
            //LargeImageData.Visibility = Visibility.Collapsed;

            CameraPositionArrayList.Clear();
        }
        public void CaptureScene()
        {
            try
            {
                // Retrieve data from text boxes
                float[] cameraPosition = cameraControls.GetCameraData(out byte[] cameraPositionArray);
                float transitionTime = float.Parse(main.GlobalTransitionTimeTextBox.Text);
                CameraPositionArrayList.Add(cameraPosition);

                // Create a new stack panel for each image
                StackPanel stackPanel = new StackPanel();
                stackPanel.Orientation = Orientation.Vertical;
                stackPanel.Width = 200;

                // Create a text block to display the index of the image
                TextBlock textBlock = new TextBlock();
                textBlock.Text = main.ImageStackPanel.Children.Count.ToString(); // Index starts from 0
                textBlock.HorizontalAlignment = HorizontalAlignment.Center;
                textBlock.Margin = new Thickness(-2);

                // Generate a unique filename for the image
                string imageFileName = Guid.NewGuid().ToString() + ".png";
                // Create an image control
                System.Windows.Controls.Image image = new System.Windows.Controls.Image();
                image.Stretch = Stretch.Uniform;
                image.Margin = new Thickness(5);
                image.MouseLeftButtonUp += Image_MouseLeftButtonUp; // Handle click event

                // Capture screenshot of the game process and use it as the image source
                // Capture screenshot of the game process and use it as the image source
                var bitmapSource = ScreenshotHelper.GetBitmapThumbnailAsync(320, 240);
                if (bitmapSource != null)
                {
                    string imagePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", "Halo 2", "Bounce Companion", "Camera Paths", Globals.currentProjectName, imageFileName);

                    // Save the bitmap source to the specified path
                    SaveBitmapSourceToFile((BitmapSource)bitmapSource, imagePath);

                    // Set the image source
                    image.Source = bitmapSource;
                }

                // Add the text block and image to the stack panel
                stackPanel.Children.Add(textBlock);
                stackPanel.Children.Add(image);

                // Add the stack panel to the parent ImageStackPanel
                main.ImageStackPanel.Children.Add(stackPanel);

                string[] floats = new string[3];
                floats[0] = "0.00";
                floats[1] = "0.00";
                floats[2] = "0.00";

                // Store image data
                Globals.imageDataList.Add(new ImageData
                {
                    CameraPosition = cameraPosition,
                    TransitionTime = transitionTime,
                    CameraPositionArray = cameraPositionArray,
                    FacePlayer = false,
                    SpectatePlayer = false,
                    CameraOffsetsArray = floats,
                    SelctedPlayerString = "",
                    ImageFileName = imageFileName

                });
            }
            catch (Exception ex)
            {
                utility.PrintToConsole(ex.ToString());
            }
        }
        private void InsertStackPanel(int index, string imageFileName)
        {
            // Create a new stack panel for each image
            StackPanel stackPanel = new StackPanel();
            stackPanel.Orientation = Orientation.Vertical;
            stackPanel.Width = 200;

            // Create a text block to display the index of the image
            TextBlock textBlock = new TextBlock();
            textBlock.Text = index.ToString(); // Index starts from 0
            textBlock.HorizontalAlignment = HorizontalAlignment.Center;
            textBlock.Margin = new Thickness(-2);

            // Create an image control
            System.Windows.Controls.Image image = new System.Windows.Controls.Image();
            image.Stretch = Stretch.Uniform;
            image.Margin = new Thickness(5);
            image.MouseLeftButtonUp += Image_MouseLeftButtonUp; // Handle click event

            // Set the image source if available
            string imagePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", "Halo 2", "Bounce Companion", "Camera Paths");
            if (File.Exists(imagePath))
            {
                BitmapImage bitmapImage = new BitmapImage(new Uri(imagePath));
                image.Source = bitmapImage;
            }

            // Add the text block and image to the stack panel
            stackPanel.Children.Add(textBlock);
            stackPanel.Children.Add(image);

            // Insert the stack panel at the selected index
            main.ImageStackPanel.Children.Insert(index, stackPanel);
        }
        private void UpdateImageSource(int index, ImageSource source)
        {
            if (index >= 0 && index < main.ImageStackPanel.Children.Count)
            {
                var stackPanel = main.ImageStackPanel.Children[index] as StackPanel;
                var image = stackPanel.Children[1] as System.Windows.Controls.Image;
                image.Source = source;
            }
        }
        private void UpdateTextBlocks()
        {
            for (int i = 0; i < main.ImageStackPanel.Children.Count; i++)
            {
                var stackPanel = main.ImageStackPanel.Children[i] as StackPanel;
                var textBlock = stackPanel.Children[0] as TextBlock;
                textBlock.Text = i.ToString();
            }
        }
        public System.Windows.Controls.Image clickedImage;
        private void Image_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            clickedImage = (System.Windows.Controls.Image)sender;
            int selectedIndex = -1;

            // Find the index of the clicked image within the ImageStackPanel
            for (int i = 0; i < main.ImageStackPanel.Children.Count; i++)
            {
                if (main.ImageStackPanel.Children[i] is StackPanel stackPanel)
                {
                    if (stackPanel.Children.Contains(clickedImage))
                    {
                        selectedIndex = i;
                        Globals.jumpToIndex = i;
                        break;
                    }
                }
            }

            if (selectedIndex != -1)
            {
                // Retrieve and display the selected image data
                ImageData selectedImageData = Globals.imageDataList[selectedIndex];
                // Use the selectedImageData as needed

                // Display the larger image and data panel
                main.LargeImage.Source = clickedImage.Source;
                main.LargeImage.Visibility = Visibility.Visible;
                userTextChanged = false;
                main.TransitionTimeTextBox.Text = selectedImageData.TransitionTime.ToString();
                main.CheckBox_OffsetAfterScene.IsChecked = selectedImageData.SpectatePlayer;
                main.CheckBox_TrackAfterScene.IsChecked = selectedImageData.FacePlayer;

                main.SceneDataTextBox_X.Text = selectedImageData.CameraPosition[0].ToString();
                main.SceneDataTextBox_Y.Text = selectedImageData.CameraPosition[1].ToString();
                main.SceneDataTextBox_Z.Text = selectedImageData.CameraPosition[2].ToString();
                main.SceneDataTextBox_Yaw.Text = selectedImageData.CameraPosition[3].ToString();
                main.SceneDataTextBox_Pitch.Text = selectedImageData.CameraPosition[4].ToString();
                main.SceneDataTextBox_Roll.Text = selectedImageData.CameraPosition[5].ToString();

                main.TextBox_SceneData_Offset_X.Text = selectedImageData.CameraOffsetsArray[0];
                main.TextBox_SceneData_Offset_Y.Text = selectedImageData.CameraOffsetsArray[1];
                main.TextBox_SceneData_Offset_Z.Text = selectedImageData.CameraOffsetsArray[2];
                main.TextBox_SelectedSceneData.Text = "[" + selectedIndex + "] - Selected Scene Data - ";
                main.ComboBox_SceneData_Playernames.SelectedItem = selectedImageData.SelctedPlayerString;

                //LargeImageData.Visibility = Visibility.Visible;
            }
        }
        // Modify LoadSceneData to open an open file dialog in the specified folder
        List<ImageData> LoadSceneData()
        {
            var dialog = new OpenFileDialog();
            dialog.InitialDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", "Halo 2", "Bounce Companion", "Camera Paths");
            dialog.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";

            if (dialog.ShowDialog() == true)
            {
                string filePath = dialog.FileName;

                // Get the directory path of the file
                string directoryPath = Path.GetDirectoryName(filePath);

                // Get the name of the last folder in the directory path
                Globals.currentProjectName = Path.GetFileName(directoryPath);

                if (File.Exists(filePath))
                {
                    string jsonData = File.ReadAllText(filePath);
                    main.staackPanel_CameraTool.Visibility = Visibility.Visible;
                    main.textBox_ProjectName.Text = Globals.currentProjectName;  // Set the text to the new currentProjectName
                    return JsonConvert.DeserializeObject<List<ImageData>>(jsonData);
                }
            }

            return new List<ImageData>();
        }

        private void LoadSceneFromFile()
        {
            int i = 2;
            foreach (var imageData in Globals.imageDataList)
            {
                if (i >= 10) i = 2;
                // Create a new stack panel
                StackPanel stackPanel = new StackPanel();
                stackPanel.Orientation = Orientation.Vertical;
                stackPanel.Width = 200;

                // Create a text block
                TextBlock textBlock = new TextBlock();
                textBlock.Text = main.ImageStackPanel.Children.Count.ToString();
                textBlock.HorizontalAlignment = HorizontalAlignment.Center;
                textBlock.Margin = new Thickness(-2);

                // Create an image control
                System.Windows.Controls.Image image = new System.Windows.Controls.Image();
                image.Stretch = Stretch.Uniform;
                image.Margin = new Thickness(5);
                image.MouseLeftButtonUp += Image_MouseLeftButtonUp;
                // Get the Documents folder
                string documentsFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

                // Combine the Documents folder with the additional path


                try
                {
                    string imagePath = Path.Combine(documentsFolder, "My Games", "Halo 2", "Bounce Companion", "Camera Paths", Globals.currentProjectName, imageData.ImageFileName);
                    //string imagePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", "Halo 2", "Bounce Companion", "Camera Paths", currentProjectName, imageData.ImageFileName);
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new Uri(imagePath);
                    bitmap.EndInit();

                    image.Source = bitmap;

                    i++;
                }
                catch (Exception ex)
                {
                    // Log or display the exception details
                    string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

                    // Combine the base directory with the image file name
                    string imagePath = Path.Combine(baseDirectory, "BrokenImage.png");
                    image.Source = new BitmapImage(new Uri(imagePath));
                    i++;
                }

                // Add text block and image to stack panel
                stackPanel.Children.Add(textBlock);
                stackPanel.Children.Add(image);

                // Add stack panel to parent ImageStackPanel
                main.ImageStackPanel.Children.Add(stackPanel);
            }
        }
        private void UpdateImageDataIndex(bool fullreset)
        {
            if (Globals.imageDataList == null || Globals.imageDataList.Count == 0) return;
            float transitionTime = float.Parse(main.TransitionTimeTextBox.Text);
            float[] cameraPosition = cameraControls.GetCameraData(out byte[] cameraPositionArray);
            bool Face = false;
            bool Spectate = false;
            if (Globals.jumpToIndex > -1)
            {
                if (main.CheckBox_TrackAfterScene.IsChecked == true) Face = true;
                if (main.CheckBox_OffsetAfterScene.IsChecked == true) Spectate = true;
                selectedImageData = Globals.imageDataList[Globals.jumpToIndex];

                Globals.imageDataList[Globals.jumpToIndex].TransitionTime = transitionTime;
                if (fullreset) Globals.imageDataList[Globals.jumpToIndex].CameraPosition = cameraPosition;
                if (fullreset) Globals.imageDataList[Globals.jumpToIndex].CameraPositionArray = cameraPositionArray;
                Globals.imageDataList[Globals.jumpToIndex].FacePlayer = Face;
                Globals.imageDataList[Globals.jumpToIndex].SpectatePlayer = Spectate;
                Globals.imageDataList[Globals.jumpToIndex].CameraPosition[0] = float.Parse(main.SceneDataTextBox_X.Text);
                Globals.imageDataList[Globals.jumpToIndex].CameraPosition[1] = float.Parse(main.SceneDataTextBox_Y.Text);
                Globals.imageDataList[Globals.jumpToIndex].CameraPosition[2] = float.Parse(main.SceneDataTextBox_Z.Text);
                Globals.imageDataList[Globals.jumpToIndex].CameraPosition[3] = float.Parse(main.SceneDataTextBox_Yaw.Text);
                Globals.imageDataList[Globals.jumpToIndex].CameraPosition[4] = float.Parse(main.SceneDataTextBox_Pitch.Text);
                Globals.imageDataList[Globals.jumpToIndex].CameraPosition[5] = float.Parse(main.SceneDataTextBox_Roll.Text);
                Globals.imageDataList[Globals.jumpToIndex].CameraOffsetsArray[0] = main.TextBox_SceneData_Offset_X.Text;
                Globals.imageDataList[Globals.jumpToIndex].CameraOffsetsArray[1] = main.TextBox_SceneData_Offset_Y.Text;
                Globals.imageDataList[Globals.jumpToIndex].CameraOffsetsArray[2] = main.TextBox_SceneData_Offset_Z.Text;
                Globals.imageDataList[Globals.jumpToIndex].SelctedPlayerString = main.ComboBox_SceneData_Playernames.Text;
                if (fullreset)
                {
                    var bitmapSource = ScreenshotHelper.GetBitmapThumbnailAsync(320, 240);
                    if (bitmapSource != null)
                    {
                        main.LargeImage.Source = null;
                        clickedImage.Source = null;
                        main.LargeImage.Source = bitmapSource;
                        clickedImage.Source = bitmapSource;
                        // Use the Documents folder and specific path
                        string imagePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", "Halo 2", "Bounce Companion", "Camera Paths", Globals.currentProjectName, Globals.imageDataList[Globals.jumpToIndex].ImageFileName);

                        // Save the bitmap source to the specified path
                        SaveBitmapSourceToFile((BitmapSource)bitmapSource, imagePath);
                        // Update the image source in the UI
                        UpdateImageSource(Globals.jumpToIndex, bitmapSource);
                    }
                }

            }
        }
        
        private async Task StartCameraRoll()
        {
            if (!rollCamera)
            {
                try
                {
                    main.CheckBox_OffsetPlayer.IsChecked = false;
                    Offset_Selected_Player = false;
                    List<float[]> cameraPathList = new List<float[]>();
                    List<float> cameraTransitionTImeList = new List<float>();
                    for (int i = 0; i < Globals.imageDataList.Count; i++)
                    {
                        var currentImageData = Globals.imageDataList[i];
                        cameraPathList.Add(currentImageData.CameraPosition);
                        cameraTransitionTImeList.Add(currentImageData.TransitionTime);
                    }
                    await Task.Run(async () =>
                    {
                        // This code will run on a separate thread.
                        await cameraInterpolation.MoveCameraSmoothly(cameraPathList, cameraTransitionTImeList);
                    });
                }
                catch (Exception ex) { MessageBox.Show("Error:" + ex.Message); rollCamera = false; }
            }
        }

        public List<float> transitionTimeList = new List<float>();
        public void LoopCamerapath()
        {
            if (!loopCamera) loopCamera = true;
            else loopCamera = false;
        }
        public void CreateNewProject()
        {
            // Open the NewProjectWindow
            NewProjectWindow newProjectWindow = new NewProjectWindow(main);
            newProjectWindow.Owner = main;

            if (newProjectWindow.ShowDialog() == true)
            {
                // Retrieve the project name from the window
                string projectName = newProjectWindow.ProjectName;

                if (!string.IsNullOrEmpty(projectName))
                {
                    // Set the current project name
                    Globals.currentProjectName = projectName;

                    // Calculate the position below the button
                    double buttonBottom = main.NewProjectButton.PointToScreen(new Point(0, main.NewProjectButton.ActualHeight)).Y;
                    double buttonCenterX = main.NewProjectButton.PointToScreen(new Point(main.NewProjectButton.ActualWidth / 2, 0)).X;
                    double windowWidth = newProjectWindow.Width;
                    double windowHeight = newProjectWindow.Height;

                    // Set the position of the window
                    newProjectWindow.Left = buttonCenterX - (windowWidth / 2);
                    newProjectWindow.Top = buttonBottom;

                    // Create a folder for the project in the specified path
                    string projectFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", "Halo 2", "Bounce Companion", "Camera Paths", Globals.currentProjectName);
                    Directory.CreateDirectory(projectFolderPath);

                    // Now you can use projectFolderPath to save screenshots for this project
                }
            }
        }
        public void UpdateCameraSpeed()
        {
            if (main.isAppLoading)
                return;

            if (!string.IsNullOrEmpty(settingsWindow.TextBox_FlySpeed.Text) && utility.ContainsOnlyNumbersOrDecimals(settingsWindow.TextBox_FlySpeed.Text)) Globals.c_moveSpeed = float.Parse(settingsWindow.TextBox_FlySpeed.Text);
            if (!string.IsNullOrEmpty(settingsWindow.TextBox_Turnspeed.Text) && utility.ContainsOnlyNumbersOrDecimals(settingsWindow.TextBox_Turnspeed.Text)) Globals.c_turnSpeed = float.Parse(settingsWindow.TextBox_Turnspeed.Text);
            if (!string.IsNullOrEmpty(settingsWindow.TextBox_Pitchspeed.Text) && utility.ContainsOnlyNumbersOrDecimals(settingsWindow.TextBox_Pitchspeed.Text)) Globals.c_pitchSpeed = float.Parse(settingsWindow.TextBox_Pitchspeed.Text);
            if (!string.IsNullOrEmpty(settingsWindow.TextBox_Heightspeed.Text) && utility.ContainsOnlyNumbersOrDecimals(settingsWindow.TextBox_Heightspeed.Text)) Globals.c_heightSpeed = float.Parse(settingsWindow.TextBox_Heightspeed.Text);
            if (!string.IsNullOrEmpty(settingsWindow.TextBox_Rollspeed.Text) && utility.ContainsOnlyNumbersOrDecimals(settingsWindow.TextBox_Rollspeed.Text)) Globals.c_rollSpeed = float.Parse(settingsWindow.TextBox_Rollspeed.Text);
        }
        public void ClearTimeline()
        {
            MessageBoxResult result = MessageBox.Show("Would you like to permanently delete the timeline and its saved images? Select No to only clear the timeline", "Confirmation", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // User chose to clear the timeline
                ClearTimelineData();
                ClearAllImages();
            }
            else if (result == MessageBoxResult.No)
            {
                ClearTimelineData();
            }
            Globals.rollCamera = false;
        }
        private void ClearAllImages()
        {
            string imagePathToDelete = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", "Halo 2", "Bounce Companion", "Camera Paths", Globals.currentProjectName);
            DeleteAllFilesInFolder(imagePathToDelete);
        }
        public void DeleteAllFilesInFolder(string folderPath)
        {
            try
            {
                // Get all files in the folder
                string[] pngFiles = Directory.GetFiles(folderPath, "*.png");

                // Delete each PNG file
                foreach (string pngFile in pngFiles)
                {
                    File.Delete(pngFile);
                }

                MessageBox.Show("All files deleted successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        public void CaptureCameraScene(object sender, RoutedEventArgs e)
        {
            TimeLine cameraScene = new TimeLine();
            _ = cameraControls.GetCameraData(out byte[] cameraPosition);
            cameraScene.CameraData = cameraPosition;
            cameraScene.transitionTime = 1000;
        }
        public void DeleteImage(string imagePath)
        {
            try
            {
                // Check if the file exists before attempting to delete
                if (File.Exists(imagePath))
                {
                    File.Delete(imagePath);
                    MessageBox.Show("Image deleted successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Image not found at the specified path.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void DeleteScene(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("This will delete your selected scene, do you want to continue?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                if (main.ImageStackPanel.Children.Count == 0) return;
                ImageData imageData = imageDataList[jumpToIndex];
                // Remove the image from the stack panel
                main.ImageStackPanel.Children.RemoveAt(jumpToIndex);
                // Remove the corresponding data from the list
                imageDataList.RemoveAt(jumpToIndex);
                main.LargeImage.Visibility = Visibility.Collapsed;
                string imagePathToDelete = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", "Halo 2", "Bounce Companion", "Camera Paths", currentProjectName, imageData.ImageFileName);
                DeleteImage(imagePathToDelete);

            }
        }
        public static class ScreenshotHelper
        {
            public static ImageSource GetBitmapThumbnailAsync(int thumbnailWidth, int thumbnailHeight)
            {
                IntPtr handle = ToolSetting.p.MainWindowHandle;
                IntPtr myhandle = Process.GetCurrentProcess().MainWindowHandle;

                // Store the current mouse position
                NativeMethods.POINT originalMousePosition;
                NativeMethods.GetCursorPos(out originalMousePosition);

                // Check if the window is minimized and restore it if needed
                if (User32.IsIconic(handle))
                    User32.ShowWindowAsync(handle, User32.SHOWNORMAL);

                User32.SetForegroundWindow(handle);

                // Simulate pressing Alt+PrintScreen
                Key altKey = (Key)KeyInterop.VirtualKeyFromKey(Key.LeftAlt);
                Key printScreenKey = (Key)KeyInterop.VirtualKeyFromKey(Key.PrintScreen);

                NativeMethods.keybd_event((byte)altKey, 0, NativeMethods.KEYEVENTF_EXTENDEDKEY, 0);
                NativeMethods.keybd_event((byte)printScreenKey, 0, NativeMethods.KEYEVENTF_EXTENDEDKEY, 0);
                Thread.Sleep(200);
                NativeMethods.keybd_event((byte)printScreenKey, 0, NativeMethods.KEYEVENTF_EXTENDEDKEY | NativeMethods.KEYEVENTF_KEYUP, 0);
                NativeMethods.keybd_event((byte)altKey, 0, NativeMethods.KEYEVENTF_EXTENDEDKEY | NativeMethods.KEYEVENTF_KEYUP, 0);

                Thread.Sleep(100);

                // Get the screenshot from the clipboard
                BitmapSource fullScreenshot = Clipboard.GetImage() as BitmapSource;

                // Calculate the scaling factors for thumbnail size
                double scaleX = (double)thumbnailWidth / fullScreenshot.PixelWidth;
                double scaleY = (double)thumbnailHeight / fullScreenshot.PixelHeight;

                // Create a scaled thumbnail bitmap
                TransformedBitmap thumbnailBitmap = new TransformedBitmap(fullScreenshot, new ScaleTransform(scaleX, scaleY));

                IntPtr clipWindow = User32.GetOpenClipboardWindow();
                User32.OpenClipboard(clipWindow);
                User32.EmptyClipboard();
                User32.CloseClipboard();

                // Bring the application window to the front again
                User32.SetForegroundWindow(myhandle);

                // Restore the mouse position
                NativeMethods.SetCursorPos(originalMousePosition.X, originalMousePosition.Y);

                return thumbnailBitmap;
            }


            private static IntPtr GetWindowHandle(string name)
            {
                var process = Process.GetProcessesByName(name).FirstOrDefault();
                if (process != null && process.MainWindowHandle != IntPtr.Zero)
                    return process.MainWindowHandle;

                return IntPtr.Zero;
            }

            //Functions utilizing the user32.dll 
            //Documentation on user32.dll - http://www.pinvoke.net/index.aspx
            public class User32
            {
                public const int SHOWNORMAL = 1;
                public const int SHOWMINIMIZED = 2;
                public const int SHOWMAXIMIZED = 3;

                [DllImport("user32.dll")]
                public static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

                [DllImport("user32.dll")]
                [return: MarshalAs(UnmanagedType.Bool)]
                public static extern bool SetForegroundWindow(IntPtr hWnd);

                [DllImport("user32.dll")]
                [return: MarshalAs(UnmanagedType.Bool)]
                public static extern bool IsIconic(IntPtr hWnd);

                [DllImport("user32.dll", SetLastError = true)]
                public static extern bool CloseClipboard();

                [DllImport("user32.dll", SetLastError = true)]
                public static extern bool OpenClipboard(IntPtr hWndNewOwner);

                [DllImport("user32.dll")]
                public static extern bool EmptyClipboard();

                [DllImport("user32.dll")]
                public static extern IntPtr GetOpenClipboardWindow();
            }
            public class NativeMethods
            {
                public const int KEYEVENTF_EXTENDEDKEY = 0x0001;
                public const int KEYEVENTF_KEYUP = 0x0002;

                [DllImport("user32.dll")]
                public static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);
                [DllImport("user32.dll")]
                public static extern bool SetCursorPos(int X, int Y);

                [DllImport("user32.dll")]
                public static extern bool GetCursorPos(out POINT lpPoint);

                [StructLayout(LayoutKind.Sequential)]
                public struct POINT
                {
                    public int X;
                    public int Y;
                }
            }
            
        }

        
    }
}
