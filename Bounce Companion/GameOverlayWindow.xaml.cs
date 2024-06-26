﻿using Bounce_Companion.Code.Camera_Tool;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
//using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;

namespace Bounce_Companion
{
    /// <summary>
    /// Interaction logic for GameOverlayWindow.xaml
    /// </summary>
    public partial class GameOverlayWindow : Window
    {
        static Process p;
        public Slider[] sliders;
        public MainWindow main;
        CameraControls cameraControls;

        private List<Rectangle> rectangles;
        private Rectangle selectedRectangle;
        private Point lastMousePosition;

        public static double canvasWidth = 0;
        public static double canvasHeight = 0;
        public bool activeEmblem = false;

        public GameOverlayWindow(MainWindow Main, CameraControls cameraControls)
        {
            InitializeComponent();

            main = Main;
            p = Process.GetProcessesByName("halo2")[0];
            sliders = new Slider[] { slider_Vert_Vel, slider_HorX_Vel, slider_HorY_Vel };
            slider_Vert_Vel.Value = 0;
            slider_HorX_Vel.Value = 0;
            slider_HorY_Vel.Value = 0;

            // Set label content for sliders
            label_Vert_Vel.Content = "Vertical Velocity: 0";
            label_HorX_Vel.Content = "Horizontal X Velocity: 0";
            label_HorY_Vel.Content = "Horizontal Y Velocity: 0";
            rectangles = new List<Rectangle>();

            Loaded += MainWindow_Loaded;
            this.cameraControls = cameraControls;
        }
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            List<float[]> bounceMarkersList = new List<float[]>();
            float[] bounceLocations = new float[] { 3.51f, -5.3f, 6.8f };
            bounceMarkersList.Add(bounceLocations);

            //CreateRectangles(bounceMarkersList);
            //DrawRectanglesOnScreen();
        }

        static List<Image> emblemImageArray = new List<Image>();
        static List<Label> emblemTextArray = new List<Label>();
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            grid_Overlay.Width = e.NewSize.Width;
            grid_Overlay.Height = e.NewSize.Height;
        }

        public async Task ShowEmblem(string bounceNumber, string bounceName)
        {
            if (activeEmblem) 
            { MoveLastEmblemDown(); }
            double initialScaleX = grid_Overlay.Width / canvasWidth; // Use the initial window width
            double initialScaleY = grid_Overlay.Height / canvasHeight; // Use the initial window height
            double initialScale = Math.Min(initialScaleX, initialScaleY);

            // Calculate the current scaling factor based on the current window size
            double currentScaleX = this.Width / grid_Overlay.Width; // Use the initial window width (800 in your case)
            double currentScaleY = this.Height / grid_Overlay.Height; // Use the initial window height (450 in your case)
            double currentScale = Math.Min(currentScaleX, currentScaleY);

            double scale = currentScale / initialScale; // Calculate the scaling factor

            if (bounceNumber == bounceName) bounceNumber = bounceNumber.ToLower();
            int g_children = grid_Overlay.Children.Count;

            Image cImg = new Image()
            {
                Name = "emblemImage" + bounceNumber,
                Width = scale * 80  ,
                Height = scale * 80,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                //Margin = new Thickness(30 * scale, 30 * scale, 0, 0), // Position at the top-left corner
                Source = new BitmapImage(new Uri("Content/Medals/MultiBounce/" + bounceNumber + ".png", UriKind.Relative))
            };

            Label cLabel = new Label()
            {
                Name = "emblemText" + bounceNumber,
                FontSize = 30 * scale,
                Foreground = new SolidColorBrush(Color.FromRgb(138, 204, 242)),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                //Margin = new Thickness(85 * scale, 30 * scale, 0, 0), // Adjust margin for label
                Content = bounceName,
            };

            StackPanel stackPanel = new StackPanel()
            {
                Name = "stackPanel" + bounceNumber,
                Orientation = Orientation.Horizontal,
                Width = 1000 * scale,
                Height = 80 * scale,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(20 * scale, 110 * scale, 0, 0), // Position at the top-left corner
            };

            DoubleAnimation fadeIn = new DoubleAnimation
            {
                From = 0.7,
                To = 1,
                Duration = new Duration(TimeSpan.FromMilliseconds(500)),
            };
            
            stackPanel.Children.Add(cImg);
            stackPanel.Children.Add(cLabel);
            grid_Overlay.Children.Add(stackPanel);
            activeEmblem = true;

            stackPanel.BeginAnimation(OpacityProperty, fadeIn);

            await Task.Delay(1500);

            DoubleAnimation fadeOut = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = new Duration(TimeSpan.FromSeconds(1.5)),
            };

            stackPanel.BeginAnimation(OpacityProperty, fadeOut);

            await Task.Delay(1500);

            grid_Overlay.Children.Remove(stackPanel);
            activeEmblem = false;
        }

        private void MoveLastEmblemDown()
        {
            int lastIndex = grid_Overlay.Children.Count - 1;
            UIElement lastChild = grid_Overlay.Children[lastIndex];

            if (lastChild is StackPanel stackPanel)
            {
                double moveDownAmount = stackPanel.ActualHeight; // Get the height of the StackPanel

                // Adjust the top margin to move it down by the height of the StackPanel
                Thickness margin = stackPanel.Margin;
                margin.Top += moveDownAmount;
                margin.Left += 35;
                stackPanel.Margin = margin;
                //Image img = (Image)stackPanel.Children[1];
                //img.Height -= 20;
                //img.Width -= 20;
            }
        }

        public void UpdateStatusBar(int BC, float Vol, float X, float Y, float Z, int tr)
        {
            //string SSC = "";
            //if (CS == -3) { SSC = "Set"; }
            //else { SSC = "None"; }

            labelBounceCount.Content = "Bounce Count: " + BC.ToString();
            //labelCrouchState.Content = "Crouch State: " + SSC;
            labelVolocity.Content = "Velocity: " + Vol.ToString();
            labelXLocation.Content = "Position X: " + X.ToString();
            labelYLocation.Content = "Position Y: " + Y.ToString();
            labelZLocation.Content = "Position Z: " + Z.ToString();
            label_Vert_Vel2.Content = Vol.ToString();
            label_Tickrate.Content = "Tickrate: " + tr.ToString();
        }

        public void EnablePlayerInfoTab(bool enabled)
        {
            if (enabled)
            {
                LabelPlayerInfo.Visibility = Visibility.Visible;
                labelBounceCount.Visibility = Visibility.Visible;
                labelCrouchState.Visibility = Visibility.Visible;
                labelVolocity.Visibility = Visibility.Visible;
                labelXLocation.Visibility = Visibility.Visible;
                labelYLocation.Visibility = Visibility.Visible;
                labelZLocation.Visibility = Visibility.Visible;
                stackPanelPlayerInfo.Visibility = Visibility.Visible;
                main.checkbox_EnableBounceXYZStats.Visibility = Visibility.Visible;
                main.checkbox_EnableSliderStats.Visibility = Visibility.Visible;
                label_Vert_Vel2.Visibility = Visibility.Visible;
                label_Tickrate.Visibility = Visibility.Visible;

            }
            else
            {
                LabelPlayerInfo.Visibility = Visibility.Hidden;
                labelBounceCount.Visibility = Visibility.Hidden;
                labelCrouchState.Visibility = Visibility.Hidden;
                labelVolocity.Visibility = Visibility.Hidden;
                labelXLocation.Visibility = Visibility.Hidden;
                labelYLocation.Visibility = Visibility.Hidden;
                labelZLocation.Visibility = Visibility.Hidden;
                stackPanelPlayerInfo.Visibility = Visibility.Hidden;
                main.checkbox_EnableSliderStats.Visibility = Visibility.Hidden;

                main.checkbox_EnableBounceXYZStats.Visibility = Visibility.Hidden;
                label_Vert_Vel2.Visibility = Visibility.Hidden;
                label_Tickrate.Visibility = Visibility.Hidden;


            }

        }
        public void EnableDebugSliders(bool enable)
        {
            if (enable)
            {
                label_Vert_Vel.Visibility = Visibility.Visible;
                label_HorX_Vel.Visibility = Visibility.Visible;
                label_HorY_Vel.Visibility = Visibility.Visible;
                slider_Vert_Vel.Visibility = Visibility.Visible;
                slider_HorX_Vel.Visibility = Visibility.Visible;
                slider_HorY_Vel.Visibility = Visibility.Visible;
            }
            else
            {
                label_Vert_Vel.Visibility = Visibility.Hidden;
                label_HorX_Vel.Visibility = Visibility.Hidden;
                label_HorY_Vel.Visibility = Visibility.Hidden;
                slider_Vert_Vel.Visibility = Visibility.Hidden;
                slider_HorX_Vel.Visibility = Visibility.Hidden;
                slider_HorY_Vel.Visibility = Visibility.Hidden;

            }
        }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void ShowEmblem(object sender, RoutedEventArgs e)
        {

        }

        private object movingObject;
        private double firstXPos, firstYPos;

        private void PreviewDown(object sender, MouseButtonEventArgs e)
        {
            if (sender == stackPanelPlayerInfo)
            {
                firstXPos = e.GetPosition(stackPanelPlayerInfo).X;
                firstYPos = e.GetPosition(stackPanelPlayerInfo).Y;
            }
            else if (sender == AimTextChat)
            {
                firstXPos = e.GetPosition(AimTextChat).X;
                firstYPos = e.GetPosition(AimTextChat).Y;
            }

            movingObject = sender;
        }

        private void PreviewUp(object sender, MouseButtonEventArgs e)
        {
            movingObject = null;
        }

        private void MoveMouse(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && sender == movingObject)
            {
                double newLeft = e.GetPosition(grid_Overlay).X - firstXPos - grid_Overlay.Margin.Left;
                double newTop = e.GetPosition(grid_Overlay).Y - firstYPos - grid_Overlay.Margin.Top;



                if (sender == stackPanelPlayerInfo)
                {
                    stackPanelPlayerInfo.SetValue(Canvas.LeftProperty, newLeft);
                    stackPanelPlayerInfo.SetValue(Canvas.TopProperty, newTop);
                }
                else if (sender == AimTextChat)
                {
                    AimTextChat.SetValue(Canvas.LeftProperty, newLeft);
                    AimTextChat.SetValue(Canvas.TopProperty, newTop);
                }
            }
        }
        public void UpdateSliders(float hX, float hY, float vert)
        {
            // Update labels with current slider values
            slider_Vert_Vel.Value = vert;
            slider_HorX_Vel.Value = hX;
            slider_HorY_Vel.Value = hY;

            label_Vert_Vel.Content = "Z Velocity: " + slider_Vert_Vel.Value.ToString();
            label_HorX_Vel.Content = "X Velocity: " + slider_HorX_Vel.Value.ToString();
            label_HorY_Vel.Content = "Y Velocity: " + slider_HorY_Vel.Value.ToString();
        }
        public bool wtsEnabled = false;
        public async void WTSTest()
        {
            if (!wtsEnabled)
            {
                wtsEnabled = true;
                await Task.Run(() => LoopWTS(wtsEnabled));
            }
            else
            {
                wtsEnabled = false;
            }
        }

        private async Task LoopWTS(bool en)
        {
            while (en)
            {
                float[] CameraPosition = cameraControls.GetCameraData(out byte[] cameraData);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    //DrawBoxOnCanvas(CameraPosition);
                });

                await Task.Delay(100); // Introduce a delay to control the refresh rate
            }
        }

        private void GridOverlay_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            canvasWidth = e.NewSize.Width;
            canvasHeight = e.NewSize.Height;

            // Update or use the new canvas width and height
        }
        public void SetnamePlateName(string p_Name)
        {
            label_P_NamePlate.Content = p_Name;
            label_P_NamePlate.Visibility = Visibility.Visible;
        }
        public class MathUtils
        {
            private const float PI = 3.141592653589f;

            public static float Radians(float degrees)
            {
                return degrees * (PI / 180.0f);
            }

            public static float Degrees(float radians)
            {
                return (180.0f / PI) * radians;
            }

            public static Vector3 WorldToScreen(Vector3 pos, int width, int height, Camera p_Cam, float p_fov)
            {
                float aspect_ratio = (float)width / (float)height;
                Vector3 cam_to_obj = new Vector3();

                float y_fov = p_fov / aspect_ratio;

                cam_to_obj.X = pos.X - p_Cam.Position.X;
                cam_to_obj.Y = pos.Y - p_Cam.Position.Y;
                cam_to_obj.Z = pos.Z - p_Cam.Position.Z;

                float dist_to_obj = (float)Math.Sqrt(cam_to_obj.X * cam_to_obj.X + cam_to_obj.Y * cam_to_obj.Y + cam_to_obj.Z * cam_to_obj.Z);
                Normalize(ref cam_to_obj);

                float obj_yaw = (float)Math.Atan2(cam_to_obj.Y, cam_to_obj.X);
                float camera_yaw = p_Cam.Rotation.X % (2.0f * PI);
                float relative_yaw = obj_yaw - camera_yaw;

                if (relative_yaw > PI)
                    relative_yaw -= 2 * PI;
                if (relative_yaw < -PI)
                    relative_yaw += 2 * PI;

                float obj_pitch = (float)Math.Asin(cam_to_obj.Z);
                float cam_pitch = p_Cam.Rotation.Y;

                float relative_pitch = cam_pitch - obj_pitch;
                float x_pos = -relative_yaw * 2 / Radians(p_fov);
                float y_pos = relative_pitch * 2 / Radians(y_fov);

                Vector3 onscreen = new Vector3();
                onscreen.X = (x_pos + 1.0f) / 2;
                onscreen.Y = (y_pos + 1.0f) / 2;
                onscreen.Z = dist_to_obj;

                onscreen.X = onscreen.X * width;
                onscreen.Y = onscreen.Y * height;

                Vector3 rotation = RotatePointAroundCenter(onscreen, -p_Cam.Rotation.Z, width, height);

                return rotation;
            }
            public static void Normalize(ref Vector3 vector)
            {
                float mag = (float)Math.Sqrt(vector.X * vector.X + vector.Y * vector.Y + vector.Z * vector.Z);
                vector.X /= mag;
                vector.Y /= mag;
                vector.Z /= mag;
            }
            public static Vector3 RotatePointAroundCenter(Vector3 pos, float angle, float width, float height)
            {
                Vector3 vector = new Vector3();

                float ox = width / 2;
                float oy = height / 2;
                float theta = angle;

                vector.X = (float)(Math.Cos(theta) * (pos.X - ox) - Math.Sin(theta) * (pos.Y - oy) + ox);
                vector.Y = (float)(Math.Sin(theta) * (pos.X - ox) + Math.Cos(theta) * (pos.Y - oy) + oy);
                vector.Z = pos.Z;
                return vector;
            }

            // Other methods such as Normalize, CosineInterpolate, CubicInterpolate, etc. can be converted similarly
        }

        public class Camera
        {
            public Vector3 Position { get; set; }
            public Vector3 Rotation { get; set; }
        }

        public struct Vector3
        {
            public float X { get; set; }
            public float Y { get; set; }
            public float Z { get; set; }
        }

    }
}
