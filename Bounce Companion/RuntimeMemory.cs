using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Linq;
using System.ComponentModel;
using System.Threading.Tasks;
using static Bounce_Companion.RuntimeMemory;
using Bounce_Companion.Code.Bounce_Companion_Utility;
using Bounce_Companion.Code.Command_Handler;

namespace Bounce_Companion
{
    public class RuntimeMemory
    {
        Mem m;
        Process P;
        MainWindow Main;
        Utility Utility;
        CommandHandler CommandHandler;
        public BackgroundWorker bGW = new BackgroundWorker();
        public TagHolder TH = new TagHolder();
        public StringID SI = new StringID();
        public string mapsPath = string.Empty;
        public string customMapsPath = string.Empty;



        public RuntimeMemory(Mem M, Process p, MainWindow main, string mapspath, string CustomMapsPath, Utility utility, CommandHandler commandHandler)
        {
            mapsPath = mapspath;
            customMapsPath = CustomMapsPath;
            m = M;
            P = p;
            Main = main;
            Utility = utility;
            CommandHandler = commandHandler;
            SetAddresses();
            tagCount = m.ReadInt32(tag_instance_count);
            Main.progressBar_TagsProgress.Maximum = 100;
            bGW.WorkerReportsProgress = true;
            bGW.ProgressChanged += BGW_ProgressChanged;
            bGW.DoWork += BackgroundWorker_DoWork;
            bGW.RunWorkerCompleted += BackgroundWorker_RunWorkerCompleted;
            bGW.RunWorkerAsync();
            
        }
        public string CurGame;
        public int tagsloadedstatus;
        string RawHex = string.Empty;
        public int tagCount;
        public string tags_loaded_address;
        string tag_instances_address;
        public string map_Magic_Address;
        public int map_Magic;
        string shared_Magic_address;
        public int shared_Magic;
        string tag_instance_count;
        public string virtual_to_runtime_magic;
        public string mapTagBase;
        public string SharedTagbase;
        public string puginpath;
        public string game = "halo2.exe+";
        private void BGW_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            Main.progressBar_TagsProgress.Value = e.ProgressPercentage;
        }

        private void BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //throw new NotImplementedException();
            if (TH.tagTypeDict.Count > 0) Utility.PrintToConsole("Tag Reading Complete.\n" +
                                TH.tagTypeDict.Count + " Parent tags found "+ tagCount + " tags read and stored!");
            foreach (string line in outPutStrings)
            {
                Utility.PrintToConsole(line);
            }
            outPutStrings.Clear();
        }
        public List<string> outPutStrings = new List<string>();

        private void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            mapname = m.ReadString("halo2.exe+0x47CF0C");
            Task.Run(() => ParseStringIDs(mapname));
            if (ParseTagInstanceList()) CommandHandler.GetCommands();
        }


        public async void SetAddresses()
        {
            string tagsHeader = "47CD68";

            int tagsHeaderInt = Convert.ToInt32(tagsHeader, 16);


            //mapName = game + (tagsHeaderInt + 0x1A4).ToString("X");
            tags_loaded_address = game + (tagsHeaderInt - 0x8).ToString("X");
            map_Magic_Address = game + (tagsHeaderInt + 0x800).ToString("X");
            shared_Magic_address = game + (tagsHeaderInt - 0x4).ToString("X");
            tag_instances_address = game + (tagsHeaderInt + 0x800).ToString("X") + ",0x8";
            //shared_tag_instances_address = game + (tagsHeaderInt - 0x4).ToString("X") + ",0x0";
            //tagNameIndexTable = game + "+0x14B68C8,0x0";
            //tagNameStringTable = "xlive.dll+0x7E9424,0x0";
            tag_instance_count = game + (tagsHeaderInt + 0x800).ToString("X") + ",0x18";
            virtual_to_runtime_magic = game + "0x14B68B8,0x0";
            mapTagBase = game + "0x14B68B8";
            SharedTagbase = game + "0x14B68A8";
            puginpath = "Content/Plugins/Halo2/";
            Utility.PrintToConsole_ContinueNextText("Tag Reading In Progress . . .  ");
            await Task.Delay(500);
            //ParseTagInstanceList();

        }


        //public string virtual_to_runtime_magic;
        string mapname;

        private bool ParseTagInstanceList()
        {
            //try
            //{
                int mapBaseTagAddress = m.ReadInt32(mapTagBase);
                string tagDatumIndexstring;

                TagHolder newHolder = new TagHolder(Main, Utility);
                TH.tagTypeDict.Clear();
                int progressBarCount = 0; // used to count progress bar in lots of 100
                for (var i = 0x0; i < tagCount; i++)
                {
                    if (progressBarCount < 100) progressBarCount += 1;
                    else
                    {
                        progressBarCount = 0;
                        bGW.ReportProgress((int)Math.Round((i / (decimal)tagCount) * 100));
                    }

                    int tagInstancesMemory = m.ReadInt32(tag_instances_address);
                    //Address Setting
                    string tagtype = m.ReadString(tagInstancesMemory + i * 0x10, 4);
                    int test = m.ReadInt32((tagInstancesMemory + (i * 0x10) + 0x4));
                    int datum_Index_value = m.ReadInt32(tagInstancesMemory + (i * 0x10) + 0x4);
                    int tag_instance_value = m.ReadInt32(tagInstancesMemory + (i * 0x10) + 0x8);
                    int tag_size_value = m.ReadInt32(tagInstancesMemory + (i * 0x10) + 0x12);

                    if (test != 0xFFFFFFFF && test != 0)
                    {
                        shared_Magic = m.ReadInt32(shared_Magic_address);
                        map_Magic = m.ReadInt32(map_Magic_Address);
                        //Get Group Name
                        byte[] bytes = Encoding.ASCII.GetBytes(tagtype);
                        Array.Reverse(bytes);
                        tagtype = Encoding.ASCII.GetString(bytes);
                        //Get Runtime Address
                        int tag_runtime_address = (shared_Magic + tag_instance_value);
                        //Get Tag Name
                        string tagname = GetTagName(mapname, i);
                        //Get Datum Index
                        string j = datum_Index_value.ToString("X");
                        string j2 = j.Substring(0, j.Length - 4);
                        string k = i.ToString("X4");
                        tagDatumIndexstring = j2 + k;

                        TH.AddElement(tagtype, tagname, tag_runtime_address, tag_instance_value, tagDatumIndexstring, "shared");
                    }
                }
                TH.PrintTagTypes();
                return true;
            //}
            //catch
            //{
            //    return false;
            //}
        }

        public int sharedTagAddressBase;
        public int loadedMapAddressBase;
        public int ReturnMapBase(string basemap)
        {
            if (basemap == "shared")
            {
                return shared_Magic;
            }
            else if (basemap == "current")
            {
                return map_Magic;
            }
            else
            {
                return map_Magic;
            }
        }


        public class TagHolder
        {
            MainWindow Main;
            Utility Utility;
            public TagHolder(MainWindow main, Utility utility)
            {
                Main = main;
                Utility = utility;
            }

            public TagHolder()
            {
            }

            // dict of Keys to Tag Types
            public Dictionary<string, TagType> tagTypeDict = new Dictionary<string, TagType>();

            public void GetChild(string tagTypeName, string tagName)
            {
                TagType tagType = tagTypeDict[tagTypeName];
                TagData tag = tagType.childTags[tagName];

                tag.Print();
            }

            // for writing a new tag
            public void AddElement(string tagTypeName, string tagName, TagData tagData)
            {
                if (!tagTypeDict.ContainsKey(tagTypeName))
                {
                    tagTypeDict.Add(tagTypeName, new TagType(tagTypeName));
                }

                TagType tagType = tagTypeDict[tagTypeName];

                if (!tagType.childTags.ContainsKey(tagName))
                {
                    tagType.childTags.Add(tagName, tagData);
                }
                else
                {
                    tagType.childTags[tagTypeName] = tagData;
                }
            }

            public void AddElement(string tagTypeName, string tagName, int tag_runtime_address, int runtimeAddress, string datumAddress, string map)
            {
                TagData tagData = new TagData(tagName, tag_runtime_address, runtimeAddress, datumAddress, map);
                AddElement(tagTypeName, tagName, tagData);
            }
            public TagData GetTag(string tagTypeName, string tagName)
            {
                List<string> keys = new List<string>();
                
                if (tagName.Contains("?"))
                {
                    TagType sectetedtagType = tagTypeDict[tagTypeName];
                    foreach (KeyValuePair<string, TagData> typePair in sectetedtagType.childTags)
                    {
                        keys.Add(typePair.Key.ToString());
                    }
                    try
                    {
                        int i = 0;
                        if (tagName == "?+") i = keys.Count - 1;
                        else i = int.Parse(tagName.Split('?')[1]);
                        TagData tag = sectetedtagType.childTags[keys[i]];
                        return tag;
                    }
                    catch
                    {
                        TagData nullTagData = new TagData("null", 0, 0, "null", "null");
                        Utility.PrintToConsole("Cound not find Key " + tagTypeName + "in Dictinary - GetPlayerColourTag");
                        return nullTagData;
                    }
                }
                foreach (KeyValuePair<string, TagType> typePair in tagTypeDict)
                {
                    TagType tagType = typePair.Value;

                    tagType.PrintChildren();
                }
                try
                {
                    
                    TagType tagType = tagTypeDict[tagTypeName];
                    try
                    {
                        TagData tag = tagType.childTags[tagName];
                        return tag;
                    }
                    catch
                    {
                        TagData nullTagData = new TagData("null", 0, 0, "null", "null");
                        //Main.PrintToConsole("Cound not find Key: " + tagName + "in Dictinary group: " + tagTypeName);
                        return nullTagData;
                    }
                }
                catch
                {
                    TagData nullTagData = new TagData("null", 0, 0, "null", "null");
                    //Main.PrintToConsole("Cound not find Key " + tagTypeName + "in Dictinary");
                    return nullTagData;
                }
            }
            public TagData GetPlayerColourTag(string tagTypeName, int i)
            {
                List<string> keys = new List<string>();
                TagType tagType = tagTypeDict["cont"];
                //TagData tagData = tagType.childTags["cont"];
                foreach (KeyValuePair<string, TagData> typePair in tagType.childTags)
                {
                    keys.Add(typePair.Key.ToString());
                }
                try
                {
                    TagData tag = tagType.childTags[keys[i]];
                    return tag;
                }
                catch
                {
                    TagData nullTagData = new TagData("null", 0, 0, "null", "null");
                    Utility.PrintToConsole("Cound not find Key " + tagTypeName + "in Dictinary - GetPlayerColourTag");
                    return nullTagData;
                }
            }


            public TagType GetTagType(string tagTypeName)
            {
                TagType tagType = tagTypeDict[tagTypeName];

                return tagType;
            }

            public void PrintTagTypes()
            {
                foreach (KeyValuePair<string, TagType> typePair in tagTypeDict)
                {
                    TagType tagType = typePair.Value;
                }
            }
            [System.Diagnostics.Conditional("DEBUG")]
            public void PrintTags()
            {
                foreach (KeyValuePair<string, TagType> typePair in tagTypeDict)
                {
                    TagType tagType = typePair.Value;

                    tagType.PrintChildren();
                }
            }
        }
        public class TagType
        {
            public string displayName = "Weapon";


            public void PrintChildren()
            {
                foreach (KeyValuePair<string, TagData> tagPair in childTags)
                {
                    tagPair.Value.Print();
                }
            }
            public Dictionary<string, TagData> childTags = new Dictionary<string, TagData>();

            public TagType(string displayName)
            {
                this.displayName = displayName;
            }
        }
        public struct TagData
        {
            public string tagname;
            public int runTimeAddress;
            public int runtimeValue;
            public string tagRef;
            public string map;

            public TagData(string tagname, int runTimeAddress, int runtimeValue, string datumAddress, string map)
            {
                this.tagname = tagname;
                this.runTimeAddress = runTimeAddress;
                this.runtimeValue = runtimeValue;
                this.tagRef = datumAddress;
                this.map = map;
            }

            public void Print()
            {
                Console.WriteLine("         Runtime Address:" + Convert.ToString(runTimeAddress, 16));
            }


        }

        private string GetTagName(string mapName, int i) // open .map file, read tagname string table, read tagname table. Dont read the whole file
        {
            string filePath = string.Empty;
            if (CheckFileExists(mapsPath, mapName + ".map"))
            {
                filePath = mapsPath + "/" + mapName + ".map";
            }
            else if (CheckFileExists(customMapsPath, mapName + ".map"))
            {
                filePath = customMapsPath + "/" + mapName + ".map";
            }
            else
            { 
                outPutStrings.Add("Failed to find " + mapName + " in map and custom map folder.");
                return null;
            }
            
            int i_readFileT_IndexTableOffset = ReadSizeFromHex(ReadBytesAtOffset(filePath, 0x2D8, 4)); // offset of tag name index table
            int i_readFileT_NameTableOffset = ReadSizeFromHex(ReadBytesAtOffset(filePath, 0x2D0, 4)); // offset of tag name table Array.IndexOf(byteArray,byteToFind)

            int T_NameInstanceOffsett = ReadSizeFromHex(ReadBytesAtOffset(filePath, i_readFileT_IndexTableOffset + (i * 4), 4)); // offset to instance index tag name

            byte[] i_readFileT_NameTableOffsett = (ReadBytesAtOffset(filePath, i_readFileT_NameTableOffset + T_NameInstanceOffsett)); //offset to tag name string
            byte b = 00;

            byte[] c = i_readFileT_NameTableOffsett.Take(Array.IndexOf(i_readFileT_NameTableOffsett, b)).ToArray();

            string output = ConvertHexToUnicode(c);

            return output;
        }
        public static bool CheckFileExists(string folderPath, string fileName)
        {
            string filePath = Path.Combine(folderPath, fileName);

            if (File.Exists(filePath))
            {
                // File exists
                return true;
            }
            else
            {
                // File does not exist
                return false;
            }
        }
        public Dictionary<string, StringID> StringIDs = new Dictionary<string, StringID>();
        public void ParseStringIDs(string mapName) // open .map file, read StringID string table, read StringIndex table. Dont read the whole file
        {
            string filePath = string.Empty;
            if (CheckFileExists(mapsPath, mapName + ".map"))
            {
                filePath = mapsPath + "/" + mapName + ".map";
            }
            else if (CheckFileExists(customMapsPath, mapName + ".map"))
            {
                filePath = customMapsPath + "/" + mapName + ".map";
            }
            else
            {
                outPutStrings.Add("Failed to find " + mapName + " in map and custom map folder.");
            }
            int i_readFileT_CountOffset = ReadSizeFromHex(ReadBytesAtOffset(filePath, 0x170, 4)); // offset of tag name index table
            for (int ct = 0; ct < i_readFileT_CountOffset; ct++)
            {
                int i_readFileT_IndexTableOffset = ReadSizeFromHex(ReadBytesAtOffset(filePath, 0x178, 4)); // offset of tag name index table
                int i_readFileT_NameTableOffset = ReadSizeFromHex(ReadBytesAtOffset(filePath, 0x17C, 4)); // offset of tag name table Array.IndexOf(byteArray,byteToFind)

                int T_NameInstanceOffsett = ReadSizeFromHex(ReadBytesAtOffset(filePath, i_readFileT_IndexTableOffset + (ct * 4), 4)); // offset to instance index tag name

                byte[] i_readFileT_NameTableOffsett = (ReadBytesAtOffset(filePath, i_readFileT_NameTableOffset + T_NameInstanceOffsett)); //offset to tag name string

                byte b = 00;
                byte[] c = i_readFileT_NameTableOffsett.Take(Array.IndexOf(i_readFileT_NameTableOffsett, b)).ToArray();

                string output = ConvertHexToUnicode(c);
                if (output != "")
                {
                    try
                    {
                        StringIDs.Add(output, new StringID(output, output.Length.ToString("X")));
                    }
                    catch { }
                }
            }
        }
        public StringID GetStringID(string sIDName)
        {
            try
            {
                StringID sID = StringIDs[sIDName];
                return sID;
            }
            catch
            {
                StringID nullTagData = new StringID("", "");
                return nullTagData;
            }
        }


        public struct StringID
        {
            public string stringID;
            public string index;

            public StringID(string stringID, string count)
            {
                this.stringID = stringID;
                this.index = count;
            }
        }

        public byte[] ReadBytesAtOffset(string filePath, long offset, int length)
        {
            byte[] test = new byte[length];
            try
            {
                using (BinaryReader reader = new BinaryReader(new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read)))
                {
                    reader.BaseStream.Seek(offset, SeekOrigin.Begin);
                    reader.Read(test, 0, length);
                }
            }
            catch
            {
                outPutStrings.Add("Cant find maps folder, Please check your maps path in the config file!");
            }
            return test;
        }
        public byte[] ReadBytesAtOffset(string filePath, long offset)
        {
            byte[] test = new byte[128];
            using (BinaryReader reader = new BinaryReader(new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read)))
            {
                reader.BaseStream.Seek(offset, SeekOrigin.Begin);
                reader.Read(test, 0, 128);
            }
            return test;
        }


        #region Byte Conversion
        // convert byte array from file to hex values
        public static string ConvertByteToHex(byte[] byteData)
        {
            string hexValues = BitConverter.ToString(byteData).Replace("-", "");

            return hexValues;
        }
        // convert hex values of file back to bytes
        public static byte[] ConvertHexToByteArray(string hexString)
        {
            byte[] byteArray = new byte[hexString.Length / 2];

            for (int index = 0; index < byteArray.Length; index++)
            {
                string byteValue = hexString.Substring(index * 2, 2);
                byteArray[index] = byte.Parse(byteValue, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            }

            return byteArray;
        }
        //convert hex to Unicode string
        public static string ConvertHexToUnicode(string Hex)
        {
            byte[] textBytes = ConvertHexToByteArray(Hex);
            Hex = System.Text.Encoding.UTF8.GetString(textBytes);
            return Hex;
        }
        //convert bytes to unicode string
        public static string ConvertHexToUnicode(byte[] Hex)
        {
            return System.Text.Encoding.UTF8.GetString(Hex);
        }
        public int ReadSizeFromHex(string hex)
        {
            byte[] tmp = ConvertHexToByteArray(hex);
            return BitConverter.ToInt32(tmp, 0);
        }
        public int ReadSizeFromHex(byte[] b)
        {
            //byte[] tmp = ConvertHexToByteArray(b);
            return BitConverter.ToInt32(b, 0);
        }
        public Int16 ReadSizeFrom2ByteHex(string hex)
        {
            byte[] tmp = ConvertHexToByteArray(hex);
            return BitConverter.ToInt16(tmp, 0);
        }
        public int ReadSizeFrom3ByteHex(string hex)
        {
            byte[] tmp = ConvertHexToByteArray(hex);
            int value = tmp[0] + (tmp[1] << 8) + (tmp[2] << 16);
            return value;
        }
        public int ReadSizeFrom1ByteHex(string hex)
        {
            byte[] tmp = ConvertHexToByteArray(hex);
            int value = tmp[0];
            return value;
        }
        #endregion

    }
}
