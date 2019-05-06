using System;
using System.IO.Ports;
using System.Threading;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using System.Security.Cryptography;
using System.Timers;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;     // DLL support
using IniParser;
using IniParser.Model;
using System.Text;
using System.Data.SQLite;
using HtmlAgilityPack;
/*using System.ServiceModel;
using System.ServiceModel.Syndication;*/

namespace SMS_server
{
    public class HtmlUtilities
    {
        /// <summary>
        /// Converts HTML to plain text / strips tags.
        /// </summary>
        /// <param name="html">The HTML.</param>
        /// <returns></returns>
        public static string ConvertToPlainText(string html)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);

            StringWriter sw = new StringWriter();
            ConvertTo(doc.DocumentNode, sw);
            sw.Flush();
            return sw.ToString();
        }


        /// <summary>
        /// Count the words.
        /// The content has to be converted to plain text before (using ConvertToPlainText).
        /// </summary>
        /// <param name="plainText">The plain text.</param>
        /// <returns></returns>
        public static int CountWords(string plainText)
        {
            return !String.IsNullOrEmpty(plainText) ? plainText.Split(' ', '\n').Length : 0;
        }


        public static string Cut(string text, int length)
        {
            if (!String.IsNullOrEmpty(text) && text.Length > length)
            {
                text = text.Substring(0, length - 4) + " ...";
            }
            return text;
        }


        private static void ConvertContentTo(HtmlNode node, TextWriter outText)
        {
            foreach (HtmlNode subnode in node.ChildNodes)
            {
                ConvertTo(subnode, outText);
            }
        }


        private static void ConvertTo(HtmlNode node, TextWriter outText)
        {
            string html;
            switch (node.NodeType)
            {
                case HtmlNodeType.Comment:
                    // don't output comments
                    break;

                case HtmlNodeType.Document:
                    ConvertContentTo(node, outText);
                    break;

                case HtmlNodeType.Text:
                    // script and style must not be output
                    string parentName = node.ParentNode.Name;
                    if ((parentName == "script") || (parentName == "style"))
                        break;

                    // get text
                    html = ((HtmlTextNode)node).Text;

                    // is it in fact a special closing node output as text?
                    if (HtmlNode.IsOverlappedClosingElement(html))
                        break;

                    // check the text is meaningful and not a bunch of whitespaces
                    if (html.Trim().Length > 0)
                    {
                        outText.Write(HtmlEntity.DeEntitize(html));
                    }
                    break;

                case HtmlNodeType.Element:
                    switch (node.Name)
                    {
                        case "p":
                            // treat paragraphs as crlf
                            outText.Write("\r\n");
                            break;
                        case "br":
                            outText.Write("\r\n");
                            break;
                    }

                    if (node.HasChildNodes)
                    {
                        ConvertContentTo(node, outText);
                    }
                    break;
            }
        }
    }

    class Program
    {
        static string feedFName;                                                                //nazev souboru s feedem

        static string telNum;                                                                   //nazev souboru s telCislama

        static string comPort;                                                                  //COM port

        static bool verbose = true;                                                             //vypisuj log do command okna

        static int updateInt = 1000;                                                            //update interval

        static SQLiteConnection sqlite_conn;                                                    //databaze SQLite

        private static void GetATCOM()                                                          //projdi vsechny comy, posli 'AT' a cekej 500ms na odpoved
        {
            string[] portsList = SerialPort.GetPortNames();             //nacti seznam vsech comPortu

            foreach (string port in portsList)                          //projdi seznam comPortu s portem jako port
            {
                try
                {
                    //definuj handler pro seriovy port port
                    SerialPort portHandler = new System.IO.Ports.SerialPort(
                        port, 115200, System.IO.Ports.Parity.None, 8, System.IO.Ports.StopBits.One);
                    portHandler.WriteTimeout = 500;
                    portHandler.Open();                                 //otevri port
                    portHandler.Write("AT\r");                          //posli 'AT'
                    System.Threading.Thread.Sleep(500);                 //cekej 500ms
                    string messageReaded = portHandler.ReadExisting();  //precti odpoved
                    portHandler.Close();                                //zavri COM
                    if (messageReaded.Contains("AT\r\r\nOK\r\n"))       //pokud je odpoved 'AT OK', na COMu je AT server a je ok
                    {
                        comPort = port;                                 //tj zapis port do staticke promenne comPort
                        Log(String.Format("AT server is on {0}!", port));
                        return;
                    }
                    else                                                //pokud odpoved neni 'AT OK'
                    {
                        if (!string.IsNullOrEmpty(messageReaded))       //pak pokud zarizeni odpovedelo, neni to AT server
                        {
                            Log(String.Format("On {0} is not AT server, because response was \"{1}\"!", port, messageReaded.Replace("\r", "")));
                        }
                        else                                            //jinak pokud neodpovedelo vubec, rovnez to neni AT srv
                        {
                            Log(String.Format("On {0} is not AT server, because no response was received in time limit!", port));
                        }
                    }
                }
                catch                                                   //pokud se nepovede nejaky z kroku vyse, preskoc COM
                {
                    Log(String.Format("{0} can´t be opened!", port));
                    continue;
                }
            }
            //pokud dojde az sem na zadnem COM neni AT, pockej na user response a rekurzivne zavolej sam sebe
            Log("AT server is not connected to computer! Please connect it and press any key to retry!", false, true);
            Console.ReadKey();
            GetATCOM();
        }

        private static void CheckForATonCOM(string comPort)                                     //zkontroluj, jestli AT server zije [comPort]
        {
            try
            {
                //definuj handler pro seriovy port port
                SerialPort portHandler = new System.IO.Ports.SerialPort(
                    comPort, 115200, System.IO.Ports.Parity.None, 8, System.IO.Ports.StopBits.One);
                portHandler.WriteTimeout = 500;
                portHandler.Open();                                 //otevri port
                portHandler.Write("AT\r");                          //posli 'AT'
                System.Threading.Thread.Sleep(500);                 //cekej 500ms
                string messageReaded = portHandler.ReadExisting();  //precti odpoved
                portHandler.Close();                                //zavri COM
                if (!messageReaded.Contains("AT\r\r\nOK\r\n"))      //pokud neni odpoved 'AT OK', neco je spatne, pockej na user response a rekurzivne se zavolej
                {
                    Log(String.Format("AT server was disconnected or failed, because response is \"{0}\"! Please connect it and press any key to continue!", messageReaded.Replace("\r", "")), false, true);
                    Console.ReadKey();
                    GetATCOM();
                    CheckForATonCOM(comPort);
                }
                else                                                //pokud je odpoved 'AT OK', na COMu je AT server a je ok
                {
                    Log(String.Format("AT server lives on {0}!", comPort));
                }
            }
            catch                                                   //pokud selze nektery z kroku vyse, COM se nepovedlo otevrit,
            {                                                       //pockej na user response a zavolej GetATCOM()
                Log("AT was disconnected! Please connect it and press any key to continue!", false, true);
                Console.ReadKey();
                GetATCOM();
                //CheckForATonCOM(comPort);
            }
        }

        private static void CreateCacheFileStruct()                                             //funkce prvotni inicializace cache
        {
            SQLiteCommand command = new SQLiteCommand(sqlite_conn)
            {
                CommandText = "CREATE TABLE clanky (id INT UNIQUE, content TEXT, chcksum VARCHAR(32))"
            };
            command.ExecuteNonQuery();
            command.Dispose();
            Log("Cache file structure created succesfully!");
        }

        private static string GetChecksumForID(int id)                                          //funkce vraci checksum pro zadane ID, nebo null
        {
            SQLiteCommand command = new SQLiteCommand(sqlite_conn)
            {
                CommandText = String.Format("SELECT chcksum FROM clanky WHERE id={0}", id)
            };
            string chcksum = Convert.ToString(command.ExecuteScalar());
            command.Dispose();
            return (chcksum);
        }

        private static string GetContentForID(int id)                                           //funkce vraci telo zpravy pro zadane ID, nebo null
        {
            SQLiteCommand command = new SQLiteCommand(sqlite_conn)
            {
                CommandText = String.Format("SELECT content FROM clanky WHERE id={0}", id)
            };
            string content = Convert.ToString(command.ExecuteScalar());
            command.Dispose();
            return (content);
        }

        private static void SetValuesForID(int id, string content, string checksum)             //funkce zapisuje clanek do tabulky
        {
            Log(String.Format("Setting new values for article {0}! Checksum: \"{1}\".", id, checksum));
            SQLiteCommand command = new SQLiteCommand(sqlite_conn)
            {
                CommandText = String.Format("REPLACE INTO clanky (id, content, chcksum) VALUES ({0}, '{1}', '{2}'); ", id, content, checksum)
            };
            command.ExecuteNonQuery();
            command.Dispose();
        }

        static void Log(string message, bool blockDateTime = false, bool forceVerbose = false)  //logovaci Fce
        {            //[telo zpravy;    blokovani zapisu dateTimu;  nuceny vypis zpravy do konzole]
            string dateTimeString = String.Empty;                   //definuj datetime string
            if (!blockDateTime)                                     //pokud neni blokovany zapis datetime
            {
                DateTime dateTimeNow = DateTime.Now;                //definuj promennou dateTime jako aktualni cas a zformatuj ji 
                dateTimeString = dateTimeNow.ToString("[MM.dd.yyyy HH:mm:ss.fff]:", CultureInfo.InvariantCulture);
            }
            using (StreamWriter fs = File.AppendText(@"sms.log"))   //otevri soubor sms.log pro appendovani jako fs
            {
                fs.WriteLine(dateTimeString + message);             //zapis do logu dateTimeString a zpravu
                if (verbose || forceVerbose)                        //pokud je zapnute logovani, a/nebo se zprava loguje vzdy
                {
                    Console.WriteLine(dateTimeString + message);    //zapis ji jeste do konzole
                }
            }
        }

        static byte[] CalcMD5forFile(string fname)                                              //spocita MD5 zadaneho souboru a vrati jej jako pole bytu
        {
            using (var md5 = MD5.Create())
            {
                using (var fs = File.OpenRead(fname))
                {
                    return md5.ComputeHash(fs);
                }
            }
        }

        static string CalcMD5(string input)
        {
            using (var md5 = MD5.Create())
            {
                return BitConverter.ToString(md5.ComputeHash(System.Text.Encoding.ASCII.GetBytes(input)));
            }
        }

        static string RemoveDiacritics(string text)
        {
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }

        static void SendSMSs(List<string[]> noveClanky)                                         //vygeneruje a posle SMS s zadanymi clanky
        {
            if (File.Exists(telNum))
            {
                Log("List of phone numbers exists! Parsing...");
                string teleText = File.ReadAllText(telNum);
                string[] cisla = teleText.Split('\n');
                if (cisla.Length > 0 && !string.IsNullOrEmpty(teleText))
                {
                    Log("List of phone numbers contains " + cisla.Length + " phone numbers!");
                    SerialPort portHandler = new System.IO.Ports.SerialPort(
                        comPort, 115200, System.IO.Ports.Parity.None, 8, System.IO.Ports.StopBits.One);
                    portHandler.WriteTimeout = 500;
                    portHandler.Open();
                    Log("STARTING SMS SENDING JOB!");
                    Log("Setting message format!");
                    portHandler.Write("AT+CMGF=1\r");
                    while (portHandler.BytesToRead == 0) ;
                    string[] response = portHandler.ReadExisting().Replace("\n", "").Split('\r');
                    if (response[2] == "OK")
                    {
                        Log("Message format succesfully set!");
                        portHandler.Write("AT+CSCS=\"8859-1\"\r");
                        while (portHandler.BytesToRead == 0) ;
                        response = portHandler.ReadExisting().Replace("\n", "").Split('\r');
                        if (response[2] == "OK")
                        {
                            Log("Encoding succesfully set!");
                            foreach (string cislo in cisla)
                            {
                                string cislo_trimmed = cislo.Replace("\n", "").Replace("\r", "");
                                if (!string.IsNullOrEmpty(cislo_trimmed))
                                {
                                    Int64 telNum = Convert.ToInt64(cislo);
                                    if (telNum > 420000000000 && telNum <= 420999999999)
                                    {
                                        Log(String.Format("STARTING JOB FOR NUMBER {0}!", cislo_trimmed));
                                        foreach (string[] clanek in noveClanky)
                                        {
                                            Log(String.Format("Setting recipient number to {0}!", cislo_trimmed));
                                            string header = "Nove oznameni od ";
                                            if (clanek[6] == "edit")
                                            {
                                                header = "Upravene oznameni ";
                                            }
                                            string teloZpravy = RemoveDiacritics(header + clanek[2] + ": " + clanek[1] + "\n" + clanek[3] + "\n" + clanek[4]);
                                            portHandler.Write(String.Format("AT+CMGS=\"{0}\"\r", cislo_trimmed));
                                            int ch = 0;
                                            string buff = "";
                                            Log("Request sent! Waiting for confirming 0x3E char!");
                                            while (ch != 0x3E)
                                            {
                                                ch = portHandler.ReadChar();
                                                buff += ch.ToString();
                                                if (buff.Contains("ERROR"))
                                                {
                                                    break;
                                                }
                                            }
                                            if (buff.Contains("ERROR"))
                                            {
                                                Log(String.Format("Unable to initialise carrier!"));
                                                System.Threading.Thread.Sleep(1000);
                                                continue;
                                            }
                                            Log("Setting recipient number succesfull!");
                                            Log("Sending message body!");
                                            portHandler.Write(String.Format("{0}\x1A\r", teloZpravy));
                                            while (portHandler.BytesToRead < teloZpravy.Length) ;
                                            response = portHandler.ReadExisting().Replace("\n", "").Split('\r');
                                            if (response[1] == "ERROR")
                                            {
                                                Log(String.Format("Unable to initialise carrier!"));
                                                System.Threading.Thread.Sleep(1000);
                                                continue;
                                            }
                                            Log("Sending message body succesfull! Waiting for SMS to send!");
                                            while (portHandler.BytesToRead == 0) ;
                                            System.Threading.Thread.Sleep(500);
                                            response = portHandler.ReadExisting().Replace("\n", "").Split('\r');
                                            if (response[3] == "OK")
                                            {
                                                Log(String.Format("SMS to number {0} succesfully sent!", cislo_trimmed));
                                            }
                                            else
                                            {
                                                Log(String.Format("Sending SMS to number {0} failed!", cislo_trimmed));
                                            }
                                        }
                                        Log(String.Format("JOB FOR NUMBER {0} COMPLETED!", cislo_trimmed));
                                    }
                                    else
                                    {
                                        Log("\"" + cislo_trimmed + "\" is not valid phone number in format 00420abcdefghi!");
                                    }
                                }
                                else
                                {
                                    Log("\"" + cislo_trimmed + "\" is not valid phone number in format 00420abcdefghi!");
                                }
                            }
                        }
                        else
                        {
                            Log(String.Format("Unable to set encoding! Error: {0}!", response[2]));
                        }
                    }
                    else
                    {
                        Log(String.Format("Unable to set message format! Error: {0}!", response[2]));
                    }
                    portHandler.Close();
                    Log("SMS SENDING JOB FINISHED!");
                }
                else
                {
                    Log("List of phone numbers does not contain any numbers! Skipping job!");
                }
            }
            else
            {
                Log("List of phone numbers does not exists! Skipping job!");
            }
        }

        static int CalcLeventhainDist(string s, string t)                                       //spocita rozdil mezi dvemi stringy
        {
            if (string.IsNullOrEmpty(s))                        //pokud je s prazdny
            {
                if (string.IsNullOrEmpty(t))                    //a t taky
                    return 0;                                   //pak jsou shodne
                return t.Length;                                //jinak je rozdil delka t
            }

            if (string.IsNullOrEmpty(t))                        //pokud je t prazdny
            {
                return s.Length;                                //pak je rozdil delka s
            }

            int delkaS = s.Length;                              //zapis delku stringu s
            int delkaT = t.Length;                              //zapis delku stringu t
            int[,] d = new int[delkaS + 1, delkaT + 1];         //definuj novou tabulku o velikosti t+1 a s+1

            //vypln tabulku hodnotami 0, 1, 2, ...
            for (int i = 0; i <= delkaS; d[i, 0] = i++) ;
            for (int j = 1; j <= delkaT; d[0, j] = j++) ;

            for (int i = 1; i <= delkaS; i++)                   //projdi sloupce
            {
                for (int j = 1; j <= delkaT; j++)               //projdi radky
                {
                    int rozd = (t[j - 1] == s[i - 1]) ? 0 : 1;  //pokud jsou shodne rozdil je 0, pokud ne, rozdil je 1
                    int min1 = d[i - 1, j] + 1;
                    int min2 = d[i, j - 1] + 1;
                    int min3 = d[i - 1, j - 1] + rozd;
                    d[i, j] = Math.Min(Math.Min(min1, min2), min3);
                }
            }
            return d[delkaS, delkaT];                           //vrat rozdil
        }

        static void CheckForNewestXML(string fname)                                             //kontroluje hashe XML
        {
            if (File.Exists(fname))
            {
                byte[] nhash = CalcMD5forFile(fname);
                if (File.Exists(fname + ".md5"))
                {
                    byte[] ohash = File.ReadAllBytes(fname + ".md5");
                    if (!MD5.Equals(BitConverter.ToString(nhash), BitConverter.ToString(ohash)))
                    {
                        //hash nesohlasi, zpracuj soubor
                        Log("XML feed changed from last update! Parsing RSS feed...");
                        List<string[]> clanky = ParseActualXML();
                        File.WriteAllBytes(fname + ".md5", nhash);
                        if (clanky.Count > 0)
                        {
                            SendSMSs(clanky);
                        }
                    }
                    else
                    {
                        //Log("XML feed se od posledniho prubehu nezmenil! Cekam...");
                    }
                }
                else
                {
                    //hash nesouhlasi, zpracuj soubor
                    Log("First start of aplication! Control checksum does not exist! Parsing RSS feed...");
                    List<string[]> clanky = ParseActualXML();
                    File.WriteAllBytes(fname + ".md5", nhash);
                    if (clanky.Count > 0)
                    {
                        SendSMSs(clanky);
                    }
                }
            }
            else
            {
                Log(String.Format("XML file with RSS feed \"{0}\" does not exists! Skipping job...", fname));
            }
        }

        static List<string[]> ParseActualXML()
        {
            /*string url = "https://spsdmasna.sharepoint.com/_layouts/15/listfeed.aspx?List=%7B268E2C52-F89E-4C25-B2F5-5170C29B6A19%7D&Source=https%3A%2F%2Fspsdmasna%2Esharepoint%2Ecom%2FSitePages%2FHome%2Easpx";
            XmlReader reader = XmlReader.Create(url);*/

            List<string[]> seznamClanku = new List<string[]>();

            Log("Loading XML!");
            XmlDocument xmlDoc = new XmlDocument(); // Create an empty XML document object
            //xmlDoc.Load(reader); // Load the XML document from the specified file
            xmlDoc.Load(feedFName); // Load the XML document from the specified file

            XmlNodeList nodeList = xmlDoc.GetElementsByTagName("item");
            Log("XML loaded succesfully!");

            Log("Reading XML content...");
            foreach (XmlNode node in nodeList)
            {
                XmlElement XMLclanek = node as XmlElement;
                string id = XMLclanek.GetElementsByTagName("guid")[0].InnerText;
                id = id.Substring(id.IndexOf("ID=") + 3);
                string nazev = XMLclanek.GetElementsByTagName("title")[0].InnerText;
                string autor = XMLclanek.GetElementsByTagName("author")[0].InnerText;
                autor = autor.Substring(autor.IndexOf(" ") + 1, 2) + autor.Substring(0, 2);
                string obsah = XMLclanek.GetElementsByTagName("description")[0].InnerText.Trim();
                obsah = Regex.Replace(obsah, @"\p{C}+", string.Empty);
                while (obsah.IndexOf("<a href") != -1)
                {
                    obsah.Replace("https://", string.Empty);
                    int linkZac = obsah.IndexOf(">", obsah.IndexOf("<a href"));
                    int linkKon = obsah.IndexOf("</a>");
                    if (linkKon - linkZac > 35)
                    {
                        obsah = obsah.Substring(0, linkZac + 15) + "..." + obsah.Substring(linkKon - 15);
                    }
                    obsah = obsah.Substring(0, obsah.IndexOf("<a href") - 1) + obsah.Substring(obsah.IndexOf(">", obsah.IndexOf("<a href")) + 1);
                }
                //Regex.Replace(obsah, "<[^>]*(>|$)", string.Empty)
                obsah = HtmlUtilities.ConvertToPlainText(obsah);
                obsah = obsah.Replace("Attachments:", string.Empty).Replace("Body:", string.Empty);
                string link = XMLclanek.GetElementsByTagName("link")[0].InnerText;
                string datum = XMLclanek.GetElementsByTagName("pubDate")[0].InnerText;
                if (!string.IsNullOrEmpty(GetChecksumForID(Convert.ToInt32(id))))
                {
                    if (CalcMD5(obsah) == GetChecksumForID(Convert.ToInt32(id)))
                    {
                        Log(String.Format("There already is record for article {0} and checksum is same as in RSS feed!", id));
                    }
                    else
                    {
                        int rozdil = CalcLeventhainDist(obsah, GetContentForID(Convert.ToInt32(id)));
                        float procenta = (float)rozdil / obsah.Length * 100;
                        Log(String.Format("There already is record for article {0} and it does differ by {1} characters, which is {2}% of string!", id, rozdil, procenta));
                        if (procenta > 30)
                        {
                            Log(String.Format("Article {0} differs by more than 30% of string! Adding to SMS job as updated article!", id));
                            string[] clanekArr = new string[] { id, nazev, autor, obsah, link, datum, "edit" };
                            seznamClanku.Add(clanekArr);
                        }
                        SetValuesForID(Convert.ToInt32(id), obsah, CalcMD5(obsah));
                    }
                }
                else
                {
                    Log(String.Format("Article {0} does not have record! Adding to SMS job as new article!", id));
                    SetValuesForID(Convert.ToInt32(id), obsah, CalcMD5(obsah));
                    string[] clanekArr = new string[] { id, nazev, autor, obsah, link, datum, "new" };
                    seznamClanku.Add(clanekArr);
                }
            }
            Log(String.Format("From least update {0} articles was added or changed!", seznamClanku.Count));
            return (seznamClanku);
        }

        static void Main(string[] args)
        {
            Log("--------------------------\n-------PROGRAM INIT-------\n--------------------------", true);
            try
            {
                var parser = new FileIniDataParser();
                IniData data = parser.ReadFile("conf.ini");
                feedFName = data["GeneralConfiguration"]["feedFile"];
                if (data["GeneralConfiguration"]["displayLog"].ToLower() == "true")
                {
                    verbose = true;
                }
                else
                {
                    Console.WriteLine(data["GeneralConfiguration"]["displayLog"].ToLower());
                    verbose = false;
                }
                Log(String.Format("Source file with RSS feed is: {0}!", feedFName));
                telNum = data["GeneralConfiguration"]["numbersList"];
                Log(String.Format("Source file with phone numbers is: {0}!", telNum));
                updateInt = Convert.ToInt32(data["GeneralConfiguration"]["updateInterval"]);
            }
            catch
            {
                Log("Parsing ini file failed or ini file was not found! Press any key to exit!", false, true);
                Console.ReadKey();
                return;
            }
            Log("Configuration file loaded and parsed sucesfully!");
            try
            {
                Log("Loading cache file!");
                sqlite_conn = new SQLiteConnection("Data Source=clanky.cf; Version = 3; Compress = True; ");
                sqlite_conn.Open();
                Log("Cache file loaded sucesfully!");

                Log("Cheching for cache file consistency!");
                SQLiteCommand command = new SQLiteCommand(sqlite_conn)
                {
                    CommandText = "SELECT name FROM sqlite_master WHERE name='clanky'"
                };
                var tableName = command.ExecuteScalar();
                command.Dispose();
                if (tableName != null && tableName.ToString() == "clanky")
                {
                    Log("Cheching for cache file consistency succeed!");
                }
                else
                {
                    Log("Cheching for cache file consistency failed! Creating new empty cache file!");
                    CreateCacheFileStruct();
                }
            }
            catch
            {
                Log("Opening cache file failed! Press any key to exit!", false, true);
                Console.ReadKey();
                return;
            }
            GetATCOM();

            while (true)
            {
                CheckForATonCOM(comPort);
                CheckForNewestXML(feedFName);
                System.Threading.Thread.Sleep(updateInt - 500 - 150);
            }
        }
    }
}
