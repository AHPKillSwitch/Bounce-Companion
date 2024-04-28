using Bounce_Companion.Code.Command_Handler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Bounce_Companion
{
    public class CommandExecution
    {
        List<string> valueTypes = new List<string> { "/point2", "/point3", "/degree", "/degree2", "/degree3", "/flags8", "/flags16", "/flags32", "/int8", "/int16", "/int32", "/enum8", "/enum16", "/enum32", "/tagRef", "/float32", "/rangef", "/ranged", "/vector3", "/vector4", "/stringId", "/colorf" };
        List<RuntimeMemory.TagData> AllTagData = new List<RuntimeMemory.TagData>();
        RuntimeMemory.TagData TD = new RuntimeMemory.TagData();
        XmlDocument doc;
        RuntimeMemory rm;
        RuntimeMemory.TagHolder TH;
        RuntimeMemory.StringID stringID = new RuntimeMemory.StringID();
        MainWindow Main;
        Mem m;
        public CommandHandler.Command c;
        public int i = 0;


        public CommandExecution(RuntimeMemory RM, Mem memory, MainWindow main)
        {
            TH = RM.TH;
            m = memory;
            rm = RM;
            Main = main;
            stringID = RM.SI;
        }
        public void TagBlockProcessing(Mem memory)
        {
            doc = new XmlDocument();
            doc.Load(rm.puginpath + c.TagType + ".xml");
            if (c.value.Contains("?*"))
            {
                int i = int.Parse(c.value.Split('.')[1]);
                TD = TH.GetPlayerColourTag(c.TagType, i);
            }
            else if (c.TagName.Contains("?*"))
            {
                int i = int.Parse(c.TagName.Split('*')[1]);
                TD = TH.GetPlayerColourTag(c.TagType, i);
            }
            else
            {
                TD = TH.GetTag(c.TagType, c.TagName);
            }
            TD.ToString();
            if (TD.tagRef != "null") // tag not found in dict
            {
                CommandParsing(c.Args, memory, c.Method, c.value);
                rm.outPutStrings.Add("----------------- Processing Command " + i + " ----------------");
                i++;
            }
            else
            {
                ErrorOutput_TagNotFound(c.TagName, c.TagType);
            }
        }
        public int RunTimeAddress;
        public string offset;
        public string tagblock = "/tagblock";
        public string basexml;
        public int CopyPasteBlockData;
        public string method1;

        private void CommandParsing(string[] args, Mem memory, string method, string value)
        {
            basexml = "/plugin";
            RunTimeAddress = TD.runTimeAddress;
            string test = TD.tagRef;
            if (args != null)
            {
                string argument1;
                for (int i = 0; i < args.Length; i++)
                {
                    if (i + 1 == args.Length) method1 = method;
                    else method1 = "blank";
                    string argument = args.ElementAt(i);
                    if (argument.Contains("["))
                    {
                        basexml = basexml + tagblock;
                        argument1 = argument.Replace("[", "").Replace("]", "");
                        RunTimeAddress = GetTagBlockData(memory, basexml, argument1, method1, RunTimeAddress, value);
                    }
                }
                RunTimeAddress = GetTagData(memory, basexml, method, value);
            }
            else
            {
                RunTimeAddress = GetTagData(memory, basexml, method, value);
            }
        }


        private int GetTagBlockData(Mem memory, string xmlpath, string argument, string method1, int RunTimeAddress, string value)
        {
            XmlNodeList nodeList = doc.SelectNodes(xmlpath);
            for (var nc = 0; nc < nodeList.Count; nc++)
            {
                string argument1 = argument.Split(':')[0];
                int CmdBlockIndex = int.Parse(argument.Split(':')[1]);
                if (nodeList[nc].Attributes[0].Value == argument1)
                {
                    offset = nodeList[nc].Attributes[1].InnerText; //get off set from xml
                    int offs = Convert.ToInt32(offset, 16); // convert to int
                    string BlockIndex = nodeList[nc].Attributes[3].InnerText; //get index size from xml
                    int Bindex = Convert.ToInt32(BlockIndex, 16);
                    if (method1 == "copy")
                    {
                        int addrToRead1 = (RunTimeAddress + offs + 4);
                        CopyPasteBlockData = (memory.ReadInt32(addrToRead1));
                        rm.outPutStrings.Add("copied Data: " + CopyPasteBlockData.ToString() + "\n");

                    }
                    else if (method1 == "paste")
                    {
                        int addrToRead1 = (RunTimeAddress + offs + 4);
                        WriteMemory(addrToRead1, "int", CopyPasteBlockData.ToString());
                        rm.outPutStrings.Add("Pasted Data: " + CopyPasteBlockData.ToString() + "\n");
                    }
                    else if (method1 == "delete" || method1 == "add" || method1 == "clear" || method1 == "set")
                    {
                        int blockindexcount = memory.ReadInt32(RunTimeAddress + offs);
                        if (method1 == "add")
                        {
                            int addrToRead1 = (RunTimeAddress + offs); //
                            string mm1 = addrToRead1.ToString("X");
                            blockindexcount += 1;
                            WriteMemory(mm1, "int", blockindexcount.ToString());
                            rm.outPutStrings.Add("Added +1 to tag index, Current index = : " + blockindexcount.ToString() + "\n");
                        }
                        if (method1 == "delete")
                        {
                            int addrToRead1 = (RunTimeAddress + offs); //
                            string mm1 = addrToRead1.ToString("X");
                            blockindexcount -= 1;
                            WriteMemory(mm1, "int", blockindexcount.ToString());
                            rm.outPutStrings.Add("Added -1 to tag index, Current index = : " + blockindexcount.ToString() + "\n");
                        }
                        if (method1 == "clear")
                        {
                            int addrToRead1 = (RunTimeAddress + offs); //
                            string mm1 = addrToRead1.ToString("X");
                            WriteMemory(mm1, "int", "0");
                            rm.outPutStrings.Add("Tag Block Index`s Cleared " + "\n");
                        }
                        if (method1 == "set")
                        {
                            int addrToRead1 = (RunTimeAddress + offs); //
                            string mm1 = addrToRead1.ToString("X");
                            WriteMemory(mm1, "int", value);
                            rm.outPutStrings.Add("Tag Block Index`s Cleared " + "\n");
                        }
                    }
                    else
                    {
                        int addrToRead = (RunTimeAddress + offs + 4); // runtime tag address + offset to Selected [block] + 4 to get to block address
                        int tagbase = rm.ReturnMapBase(TD.map);
                        RunTimeAddress = (memory.ReadInt32(addrToRead)) + tagbase;
                        Bindex = CmdBlockIndex * Bindex;
                        RunTimeAddress += Bindex;
                        string mk = RunTimeAddress.ToString("X");
                        return RunTimeAddress;
                    }
                }
            }
            rm.outPutStrings.Add("Command Error!!");
            return 0;
        }

        private int GetTagData(Mem memory, string xmlpath, string method, string value)
        {
            int RunTimeAddress1 = RunTimeAddress;
            foreach (string type in valueTypes)
            {
                XmlNodeList innerNodes = doc.SelectNodes(xmlpath + type);
                foreach (XmlNode _node in innerNodes)
                {

                    if (_node.Attributes[0].Value == method)
                    {
                        //in the right node
                        string hexOffset = _node.Attributes[1].Value;
                        int off = Convert.ToInt32(hexOffset, 16);
                        int Tagruntimeaddress = RunTimeAddress + off;
                        string mk2 = Tagruntimeaddress.ToString("X");
                        //MessageBox.Show(node.InnerXml);
                        switch (type)
                        {
                            case "/rangef":
                                { // Code flow ----->
                                    float value1 = float.Parse(value.Split(':')[0]); WriteMemory(Tagruntimeaddress, "float", value1.ToString());
                                    float value2 = float.Parse(value.Split(':')[1]); Tagruntimeaddress += 4; WriteMemory(Tagruntimeaddress, "float", value2.ToString());
                                    break;
                                }
                            case "/ranged":
                                { // Code flow ----->
                                    float value1 = float.Parse(value.Split(':')[0]); WriteMemory(Tagruntimeaddress, "float", value1.ToString());
                                    float value2 = float.Parse(value.Split(':')[1]); Tagruntimeaddress += 4; WriteMemory(Tagruntimeaddress, "float", value2.ToString());
                                    break;
                                }
                            case "/colorf":
                                { // Code flow ----->
                                    int runtimeaddress = Tagruntimeaddress;
                                    WriteMemory(runtimeaddress, "float", GetColorF(value.Substring(0, 2))); //alpha
                                    WriteMemory(runtimeaddress += 0x4, "float", GetColorF(value.Substring(2, 2))); //R
                                    WriteMemory(runtimeaddress += 0x4, "float", GetColorF(value.Substring(4, 2))); //G
                                    WriteMemory(runtimeaddress += 0x4, "float", GetColorF(value.Substring(6, 2))); //B
                                    break;
                                }
                            case "/tagRef":
                                {
                                    string tagname = string.Empty;
                                    string tagtype = string.Empty;
                                    if (value != "null")
                                    {

                                        if (c.value.Contains("?*"))
                                        {
                                            int i = int.Parse(c.value.Split('.')[1]);
                                            TD = TH.GetPlayerColourTag(c.TagType, i);
                                        }
                                        else if (c.TagName.Contains("?*"))
                                        {
                                            int i = int.Parse(c.TagName.Split('?')[1]);
                                            TD = TH.GetPlayerColourTag(c.TagType, i);
                                        }
                                        else
                                        {
                                            tagname = value.Split('.')[0];
                                            tagtype = value.Split('.')[1];
                                            TD = TH.GetTag(tagtype, tagname);
                                        }
                                        TD.ToString();
                                        string game = rm.CurGame;
                                        string datumindex = TD.tagRef;
                                        if (datumindex != "null")
                                        {
                                            RTETagRef(memory, tagtype, Tagruntimeaddress, datumindex, game);
                                        }
                                        else
                                        {
                                            ErrorOutput_TagNotFound(tagname, tagtype);
                                        }
                                    }
                                    else
                                    {
                                        int runtimeaddress = Tagruntimeaddress;
                                        WriteMemory(runtimeaddress, "int", Convert.ToInt32("0xFFFFFFFF", 16).ToString());
                                        runtimeaddress += 4;
                                        WriteMemory(runtimeaddress, "int", Convert.ToInt32("0xFFFFFFFF", 16).ToString());
                                    }
                                    break;
                                }
                            case "/int8":
                                {

                                    WriteMemory(Tagruntimeaddress, "byte", sbyte.Parse(value).ToString());

                                    //WriteMemory(Tagruntimeaddress.ToString("X"), "int", value);
                                    break;
                                }
                            case "/int16":
                                {
                                    string output = string.Empty;
                                    byte[] bytes = BitConverter.GetBytes(Convert.ToInt16(value));
                                    for (int i = 0; i < bytes.Length; i++)
                                    {
                                        output += "0x" + bytes[i].ToString("X");
                                        if (i != bytes.Length - 1) output += ",";
                                    }
                                    WriteMemory(Tagruntimeaddress, "bytes", output);

                                    //WriteMemory(Tagruntimeaddress.ToString("X"), "int", value);
                                    break;
                                }
                            case "/int32":
                                {
                                    WriteMemory(Tagruntimeaddress, "int", value);
                                    break;
                                }
                            case "/enum8":
                                {
                                    foreach (var off1 in from XmlNode node in innerNodes
                                                         where node.Attributes[0].Value == method
                                                         from XmlNode option in node
                                                         where _node.Attributes[0].Value == method && option.Attributes[0].Value == value
                                                         let tmpflagindex = option.Attributes[1].Value
                                                         let off1 = Convert.ToInt32(tmpflagindex, 16)
                                                         select off1)
                                    {
                                        WriteMemory(Tagruntimeaddress, "int", off1.ToString());
                                        return 0;
                                    }

                                    break;
                                }
                            case "/enum16":
                                {
                                    foreach (var off1 in from XmlNode node in innerNodes
                                                         where node.Attributes[0].Value == method
                                                         from XmlNode option in node
                                                         where _node.Attributes[0].Value == method && option.Attributes[0].Value == value
                                                         let tmpflagindex = option.Attributes[1].Value
                                                         let off1 = Convert.ToInt32(tmpflagindex, 16)
                                                         select off1)
                                    {
                                        WriteMemory(Tagruntimeaddress, "int", off1.ToString());
                                        return 0;
                                    }

                                    break;
                                }
                            case "/enum32":
                                {
                                    foreach (var off1 in from XmlNode node in innerNodes
                                                         where node.Attributes[0].Value == method
                                                         from XmlNode option in node
                                                         where _node.Attributes[0].Value == method && option.Attributes[0].Value == value
                                                         let tmpflagindex = option.Attributes[1].Value
                                                         let off1 = Convert.ToInt32(tmpflagindex, 16)
                                                         select off1)
                                    {
                                        WriteMemory(Tagruntimeaddress, "int", off1.ToString());
                                        return 0;
                                    }

                                    break;
                                }
                            case "/float32":
                                {
                                    float v1 = float.Parse(value);
                                    WriteMemory(Tagruntimeaddress, "float", v1.ToString());
                                    break;
                                }
                            case "/point2":
                                { // Code flow ----->
                                    float v1 = float.Parse(value.Split(':')[0]); WriteMemory(Tagruntimeaddress, "float", v1.ToString());
                                    float v2 = float.Parse(value.Split(':')[1]); Tagruntimeaddress += 4; WriteMemory(Tagruntimeaddress, "float", v2.ToString());
                                    break;
                                }
                            case "/point3":
                                { // Code flow ----->
                                    float v1 = float.Parse(value.Split(':')[0]); WriteMemory(Tagruntimeaddress, "float", v1.ToString());
                                    float v2 = float.Parse(value.Split(':')[1]); Tagruntimeaddress += 4; WriteMemory(Tagruntimeaddress, "float", v2.ToString());
                                    float v3 = float.Parse(value.Split(':')[1]); Tagruntimeaddress += 4; WriteMemory(Tagruntimeaddress, "float", v3.ToString());
                                    break;
                                }
                            case "/degree":
                                { // Code flow ----->
                                    int v1 = int.Parse(value.Split(':')[0]); float v1_2 = ConvertDegreesToRadians(v1); WriteMemory(Tagruntimeaddress, "float", v1_2.ToString());
                                    break;
                                }
                            case "/degree2":
                                { // Code flow ----->
                                    int v1 = int.Parse(value.Split(':')[0]); float v1_2 = ConvertDegreesToRadians(v1); WriteMemory(Tagruntimeaddress, "float", v1_2.ToString());
                                    int v2 = int.Parse(value.Split(':')[1]); float v2_2 = ConvertDegreesToRadians(v2); Tagruntimeaddress += 4; WriteMemory(Tagruntimeaddress, "float", v2_2.ToString());
                                    break;
                                }
                            case "/vector3":
                                { // Code flow ----->
                                    float v1 = float.Parse(value.Split(':')[0]); WriteMemory(Tagruntimeaddress, "float", v1.ToString());
                                    float v2 = float.Parse(value.Split(':')[1]); Tagruntimeaddress += 4; WriteMemory(Tagruntimeaddress, "float", v2.ToString());
                                    float v3 = float.Parse(value.Split(':')[2]); Tagruntimeaddress += 4; WriteMemory(Tagruntimeaddress, "float", v3.ToString());
                                    break;
                                }
                            case "/vector4":
                                { // Code flow ----->
                                    float v1 = float.Parse(value.Split(':')[0]); WriteMemory(Tagruntimeaddress, "float", v1.ToString());
                                    float v2 = float.Parse(value.Split(':')[1]); Tagruntimeaddress += 4; WriteMemory(Tagruntimeaddress, "float", v2.ToString());
                                    float v3 = float.Parse(value.Split(':')[2]); Tagruntimeaddress += 4; WriteMemory(Tagruntimeaddress, "float", v3.ToString());
                                    float v4 = float.Parse(value.Split(':')[3]); Tagruntimeaddress += 4; WriteMemory(Tagruntimeaddress, "float", v4.ToString());
                                    break;
                                }
                            case "/stringId":
                                { // Code flow ----->

                                    stringID = rm.GetStringID(value);
                                    if (stringID.index == "")
                                    {
                                        WriteMemory(Tagruntimeaddress, "int", "0");
                                    }
                                    else
                                    {
                                        WriteMemory(Tagruntimeaddress, "int", stringID.index.Length.ToString("X") + stringID.index);
                                    }
                                    break;
                                }
                            case "/flags8": //
                                {
                                    string myOption = value.Split(':')[0];
                                    bool settrue = bool.Parse(value.Split(':')[1]);

                                    int flagindex = 0;
                                    foreach (var (Tagruntimeaddress1, tmpflagindex) in from XmlNode node in innerNodes
                                                                                       where node.Attributes[0].Value == method
                                                                                       from XmlNode option in node
                                                                                       where _node.Attributes[0].Value == method && option.Attributes[0].Value == myOption
                                                                                       let hexOffset1 = node.Attributes[1].Value
                                                                                       let off1 = Convert.ToInt32(hexOffset1, 16)
                                                                                       let Tagruntimeaddress1 = RunTimeAddress1 + off1
                                                                                       let tmpflagindex = option.Attributes[1].Value
                                                                                       select (Tagruntimeaddress1, tmpflagindex))
                                    {
                                        flagindex = int.Parse(tmpflagindex);
                                        int readFlags = memory.ReadByte(Tagruntimeaddress1);
                                        RTEFlags(memory, Tagruntimeaddress1, readFlags, flagindex, settrue);
                                        return 0;
                                    }

                                    break;
                                }
                            case "/flags16": //
                                {
                                    string myOption = value.Split(':')[0];
                                    bool settrue = bool.Parse(value.Split(':')[1]);

                                    int flagindex = 0;
                                    foreach (var (Tagruntimeaddress1, tmpflagindex) in from XmlNode node in innerNodes
                                                                                       where node.Attributes[0].Value == method
                                                                                       from XmlNode option in node
                                                                                       where _node.Attributes[0].Value == method && option.Attributes[0].Value == myOption
                                                                                       let hexOffset1 = node.Attributes[1].Value
                                                                                       let off1 = Convert.ToInt32(hexOffset1, 16)
                                                                                       let Tagruntimeaddress1 = RunTimeAddress1 + off1
                                                                                       let tmpflagindex = option.Attributes[1].Value
                                                                                       select (Tagruntimeaddress1, tmpflagindex))
                                    {
                                        flagindex = int.Parse(tmpflagindex);
                                        int readFlags = memory.ReadInt16(Tagruntimeaddress1);
                                        RTEFlags(memory, Tagruntimeaddress1, readFlags, flagindex, settrue);
                                        return 0;
                                    }

                                    break;
                                }
                            case "/flags32": //
                                {
                                    string myOption = value.Split(':')[0];
                                    bool settrue = bool.Parse(value.Split(':')[1]);

                                    int flagindex = 0;
                                    foreach (var (Tagruntimeaddress1, tmpflagindex) in from XmlNode node in innerNodes
                                                                                       where node.Attributes[0].Value == method
                                                                                       from XmlNode option in node
                                                                                       where _node.Attributes[0].Value == method && option.Attributes[0].Value == myOption
                                                                                       let hexOffset1 = node.Attributes[1].Value
                                                                                       let off1 = Convert.ToInt32(hexOffset1, 16)
                                                                                       let Tagruntimeaddress1 = RunTimeAddress1 + off1
                                                                                       let tmpflagindex = option.Attributes[1].Value
                                                                                       select (Tagruntimeaddress1, tmpflagindex))
                                    {
                                        flagindex = int.Parse(tmpflagindex);
                                        int readFlags = memory.ReadInt32(Tagruntimeaddress1);
                                        RTEFlags(memory, Tagruntimeaddress1, readFlags, flagindex, settrue);
                                        return 0;
                                    }

                                    break;
                                }
                        }
                    }
                }
            }
            return 0;
        }

        public void WriteMemory(object Tagruntimeaddress,string type, string value)
        {
            rm.outPutStrings.Add("Memory: " + Tagruntimeaddress + " Type: " + type + " Value: " + value);
            m.WriteToMemory(Tagruntimeaddress, type, value);
        }

        private void ErrorOutput_TagNotFound(string tagname, string tagtype)
        {
            rm.outPutStrings.Add("=================================== COMMAND ERROR ===================================");
            rm.outPutStrings.Add("Failed to find Tagname: " + tagname);
            rm.outPutStrings.Add("Tag Dictinary: " + tagtype);
            rm.outPutStrings.Add("Check the below command for spelling or formatting errors!!");
            rm.outPutStrings.Add("Note: Shared.map tags are not loaded on all maps, check if tag is loaded on the .map");
            rm.outPutStrings.Add("===================================================================================");
        }

        public float ConvertDegreesToRadians(int degrees)
        {
            float radians = (float)((Math.PI / 180) * degrees);
            return (radians);
        }

        public void RTETagRef(Mem memory, string tagtype, int runtimeaddress, string datumindex, string game)
        {

            string fixedvalue = "";
            for (int idx = 0; idx < datumindex.Length - 1; idx += 2)
            {
                var value = datumindex.Substring(idx, 2);
                fixedvalue += "0x" + value;
                if (idx != datumindex.Length - 2) { fixedvalue += " "; }
            }
            byte[] bytes = Encoding.ASCII.GetBytes(tagtype);
            Array.Reverse(bytes);
            string Rtagtype = Encoding.ASCII.GetString(bytes);
            string intstr = (int.Parse(datumindex, System.Globalization.NumberStyles.HexNumber)).ToString();
            WriteMemory(runtimeaddress, "string", Rtagtype);
            runtimeaddress += 4;
            WriteMemory(runtimeaddress, "int", intstr);
        }
        public void RTEFlags(Mem memory, int runtimeaddress, int currentFlags, int option, bool setTrue)
        {
            string input = Convert.ToString(currentFlags, 2);
            int output = Convert.ToInt32(input, 2);
            if (setTrue == true)
            {
                int intValue = output | (1 << option);

                WriteMemory(runtimeaddress, "int", intValue.ToString());
            }
            else
            {
                int intValue = output &= ~(1 << option);

                WriteMemory(runtimeaddress, "int", intValue.ToString());
            }
        }
        private string GetColorF(string hex)
        {
            int x = int.Parse(hex, System.Globalization.NumberStyles.HexNumber);
            float f = x / 255;

            return f.ToString();
        }
    }
}
