using NAudio.Gui;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Xml;

namespace Bounce_Companion.Code.Bounce_Companion_Utility
{
    internal class Annoucements
    {
        MainWindow main;
        Utility utility;
        GameVolumeGetter volumeGetter;
        Settings settingsWindow;
        GameOverlayWindow GOW;


        private MediaPlayer audio_Player_Main = new MediaPlayer();
        private MediaPlayer audio_Player_Main_2 = new MediaPlayer();
        private MediaPlayer audio_Player_Effect_1 = new MediaPlayer();
        private MediaPlayer audio_Player_Effect_2 = new MediaPlayer();
        private MediaPlayer audio_Player_Effect_3 = new MediaPlayer();

        private string sfx2 = string.Empty;
        private string sfx3 = string.Empty;
        private string sfx4 = string.Empty;
        private string sfx5 = string.Empty;
        private string sfx6 = string.Empty;
        private string sfx7 = string.Empty;
        private string sfx8 = string.Empty;
        private string sfx9 = string.Empty;
        private string sfx10 = string.Empty;

        public Annoucements(MainWindow main, Utility utility, GameVolumeGetter volumeGetter, Settings settingsWindow, GameOverlayWindow gOW)
        {
            this.main = main;
            this.utility = utility;
            this.volumeGetter = volumeGetter;
            this.settingsWindow = settingsWindow;
            GOW = gOW;

            
            LoadSFXFromXml();
        }
        public async Task Announce(int count, string location, string type)
        {

            if (main.checkbox_DisableOverlay.IsChecked == false)
            {
                string bounceSFX = string.Empty;
                if (location != "null")
                {
                    utility.PrintToConsole(location + " Bounce Hit");
                    bounceSFX = GetBounceSFX(11);
                    //await PlayLocationalAudioShowEmblem(location, location);
                    await PlayMultiAudioShowEmblem("null", location, bounceSFX);
                    //await Task.Delay(1000);
                }
                foreach (int number in bouncenumber)
                {
                    if (number == count)
                    {
                        string bounceText = string.Empty;
                        if (type.Contains("slant")) location = type;
                        else
                        {
                            bounceText = GetBounceText(number);
                            if (bounceSFX != "null") bounceSFX = GetBounceSFX(number);
                        }

                        utility.PrintToConsole(bounceText);
                        await PlayMultiAudioShowEmblem(number.ToString(), bounceText, bounceSFX);
                        break;
                    }
                }
                if (count > 10)
                {
                    bounceSFX = GetBounceSFX(2);
                    await PlayMultiAudioShowEmblem(count.ToString(), count.ToString(), bounceSFX);
                }

            }


        }
        List<int> bouncenumber = new List<int> { 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 }; // need to remove the bounce number list ts stupid
        private async Task PlayMultiAudioShowEmblem(string bounceNumber, string bounceText, string bounceSFX)
        {
            _ = GOW.ShowEmblem(bounceNumber, bounceText);
            if (!string.IsNullOrEmpty(bounceSFX))
            {
                PlaySFX(audio_Player_Effect_1, "Content/Audio/SFX/", bounceSFX, volumeGetter.GetGameVolumeLevel(main));
                await Task.Delay((int)settingsWindow.Slider_DelayAudio.Value);
            }
            PlaySFX(audio_Player_Main, "Content/Audio/MultiBounces/", bounceNumber, volumeGetter.GetGameVolumeLevel(main));
        }

        private void PlaySFX(MediaPlayer audio_Player, string filePath, string bounceNumber, float audioLevel)
        {
            audio_Player.Open(new Uri(filePath + bounceNumber + ".wav", UriKind.Relative));

            audio_Player.Volume = audioLevel;
            audio_Player.Play();
        }

        private async Task PlayLocationalAudioShowEmblem(string bounceNumber, string bounceText)
        {
            audio_Player_Main = new MediaPlayer();
            audio_Player_Main.Open(new Uri("Content/Audio/Locational/" + bounceText.ToLower() + ".wav", UriKind.Relative));
            audio_Player_Main.Volume = 0.4;
            //PlaySniperAudio();
            audio_Player_Main.Play();
            //await GOW.ShowEmblem(bounceText, bounceText);
        }
        private void PlaySniperAudio()
        {
            audio_Player_Main_2 = new MediaPlayer();
            audio_Player_Main_2.Open(new Uri("Content/Audio/Locational/sniperfire.wav", UriKind.Relative));
            audio_Player_Main_2.Volume = 0.4;
            audio_Player_Main_2.Play();
        }
        private string GetBounceSFX(int number)
        {
            switch (number)
            {
                case 2: return sfx2;
                case 3: return sfx3;
                case 4: return sfx4;
                case 5: return sfx5;
                case 6: return sfx6;
                case 7: return sfx7;
                case 8: return sfx8;
                case 9: return sfx9;
                case 10: return sfx10;
                default: return "";
            }
        }
        public void LoadSFXFromXml()
        {
            string xmlFileName = "sfxnames.xml"; // Hardcoded XML file name
            string xmlFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Content", "XMLs", xmlFileName);
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(xmlFilePath);

            XmlNodeList sfxNodes = xmlDoc.SelectNodes("//sfxnames/sfx");
            foreach (XmlNode sfxNode in sfxNodes)
            {
                if (int.TryParse(sfxNode.Attributes["number"].Value, out int sfxNumber))
                {
                    string sfxValue = sfxNode.InnerText;

                    switch (sfxNumber)
                    {
                        case 2: sfx2 = sfxValue; break;
                        case 3: sfx3 = sfxValue; break;
                        case 4: sfx4 = sfxValue; break;
                        case 5: sfx5 = sfxValue; break;
                        case 6: sfx6 = sfxValue; break;
                        case 7: sfx7 = sfxValue; break;
                        case 8: sfx8 = sfxValue; break;
                        case 9: sfx9 = sfxValue; break;
                        case 10: sfx10 = sfxValue; break;
                    }
                }
            }
        }
        private string GetBounceText(int number)
        {
            switch (number)
            {
                case 2: return "Double Bounce!";
                case 3: return "Triple Bounce!";
                case 4: return "Quad Bounce!";
                case 5: return "Bouncetacular!";
                case 6: return "Bouncetrocity!";
                case 7: return "Bungee Jumper!";
                case 8: return "Octabounce!";
                case 9: return "Bouncepocalypse!";
                case 10: return "Bounce Revival!";
                default: return "";
            }
        }
    }
}
