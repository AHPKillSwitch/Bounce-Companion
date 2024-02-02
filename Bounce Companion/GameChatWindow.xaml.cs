using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using System.Windows;
using Memory;

namespace Bounce_Companion
{
    public partial class GameChatWindow : Window
    {
        private List<string> messageLog = new List<string>();
        private Mem m; // Assuming you have a Mem instance
        MainWindow main;
        private int bufferSize = 100; // Adjust the buffer size as needed
        private int currentIndex = 0;

        public GameChatWindow(Mem M, MainWindow Main)
        {
            InitializeComponent();
            m = M;
            main = Main;
            Timer timer = new Timer(1000); // Adjust the interval as needed (1 second in this example)
            timer.Elapsed += Timer_Elapsed;
            timer.Start();
            // Adjust the Left property to make the window stick to the right side of the MainWindow
            Left = Main.Left + Main.ActualWidth;
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            // Read chat bytes from memory
            byte[] chatBytes = m.ReadBytes("halo2.exe+009758C8,0x0", 1005, "");

            // Convert bytes to string
            string newMessages = Encoding.Unicode.GetString(chatBytes).Split('\0').FirstOrDefault(part => !string.IsNullOrEmpty(part));

            // Process only new messages
            if (!string.IsNullOrEmpty(newMessages))
            {
                // Split new messages into lines
                string[] newLines = newMessages.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

                // Add only new lines to the message log
                foreach (string line in newLines)
                {
                    if (!messageLog.Contains(line))
                    {
                        messageLog.Add(line);
                        currentIndex = (currentIndex + 1) % bufferSize;
                    }
                }

                // Update the window on the UI thread
                Dispatcher.Invoke(() =>
                {
                    // Display the latest messages from the circular buffer
                    int start = (currentIndex - messageLog.Count + bufferSize) % bufferSize;
                    List<string> displayedMessages = new List<string>();
                    for (int i = 0; i < messageLog.Count; i++)
                    {
                        displayedMessages.Add(messageLog[(start + i) % bufferSize]);
                    }

                    // Update the TextBox with the displayed messages
                    txtChat.Text = string.Join(Environment.NewLine, displayedMessages);

                    // Scroll to the latest message
                    txtChat.ScrollToEnd();
                });
            }
        }
    }





}
