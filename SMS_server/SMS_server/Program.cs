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
using System.Net;
using System.Diagnostics;
using System.Data.SqlClient;
using System.Data;
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

        static string teachersFeedFName;                                                        //nazev souboru s feedem ucitelu

        static string telNum_fname;                                                             //nazev souboru s telCislama

        static string telNumTeacher_fname;                                                      //nazev souboru s telCislama ucitelu

        static string comPort;                                                                  //COM port

        static bool verbose = true;                                                             //vypisuj log do command okna

        static int updateInt = 1000;                                                            //update interval

        static SQLiteConnection sqlite_conn;                                                    //databaze SQLite

        static Dictionary<char, char> charsToReplace = new Dictionary<char, char> {
            { (char)0x13, '-' },
            { (char)0x1C, '"' },
            { (char)0x1D, '"' },
            { (char)0x1E, '"' },
            { (char)0x1F, '"' }
        };

        private static void GetATCOM(Boolean recursive = false)                                 //projdi vsechny comy, posli 'AT' a cekej 500ms na odpoved
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
            Log("AT server is not connected to computer!", false, !recursive, true); // Please connect it and press any key to retry!
            //Console.ReadKey();
            System.Threading.Thread.Sleep(1000);
            GetATCOM(true);
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
                    Log(String.Format("AT server was disconnected or failed, because response is \"{0}\"!", messageReaded.Replace("\r", "")), false, true, true);
                    //Console.ReadKey();
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
                Log("AT was disconnected!", false, true, true);
                //Console.ReadKey();
                GetATCOM();
                //CheckForATonCOM(comPort);
            }
        }

        private static void CreateCacheFileStruct()                                             //funkce prvotni inicializace cache
        {
            SQLiteCommand command = new SQLiteCommand(sqlite_conn)
            {
                CommandText = "CREATE TABLE newArticles (id INT UNIQUE, content TEXT, chcksum VARCHAR(32))"
            };
            command.ExecuteNonQuery();
            command.Dispose();
            Log("Cache file structure created succesfully!");
        }

        private static string GetChecksumForID(int id)                                          //funkce vraci checksum pro zadane ID, nebo null
        {
            SQLiteCommand command = new SQLiteCommand(sqlite_conn)
            {
                CommandText = String.Format("SELECT chcksum FROM newArticles WHERE id={0}", id)
            };
            string chcksum = Convert.ToString(command.ExecuteScalar());
            command.Dispose();
            return (chcksum);
        }

        private static string GetContentForID(int id)                                           //funkce vraci telo zpravy pro zadane ID, nebo null
        {
            SQLiteCommand command = new SQLiteCommand(sqlite_conn)
            {
                CommandText = String.Format("SELECT content FROM newArticles WHERE id={0}", id)
            };
            string content = Convert.ToString(command.ExecuteScalar());
            command.Dispose();
            return (content);
        }

        private static void SetValuesForID(int id, string content, string checksum)             //funkce zapisuje article do tabulky
        {
            Log(String.Format("Setting new values for article {0}! Checksum: \"{1}\".", id, checksum));
            SQLiteCommand command = new SQLiteCommand(sqlite_conn)
            {
                CommandText = "REPLACE INTO newArticles (id, content, chcksum) VALUES (@id, @content, @chcksum);"
            };
            command.Parameters.AddWithValue("id", id);
            command.Parameters.AddWithValue("content", content);
            command.Parameters.AddWithValue("chcksum", checksum);
            command.ExecuteNonQuery();
            command.Dispose();
        }

        static void Log(string message, bool blockDateTime = false, bool forceVerbose = false, bool criticalLog = false)  //logovaci Fce
        {            //[telo zpravy;    blokovani zapisu dateTimu;  nuceny vypis zpravy do konzole]
            DateTime dateTimeNow = DateTime.Now;
            if (File.Exists(@"sms.log"))
            {
                string lastLine = File.ReadLines(@"sms.log").Last();
                if (lastLine.Contains("["))
                {
                    if (dateTimeNow.ToString("[yyyy.MM.dd", CultureInfo.InvariantCulture) != lastLine.Substring(0, 11))
                    {
                        try
                        {
                            System.IO.File.Move(@"sms.log", String.Format(@"sms_{0}.log", lastLine.Substring(1, 10).Replace('.', '_')));
                        }
                        catch
                        {

                        }
                    }
                }
            }
            string dateTimeString = String.Empty;                   //definuj datetime string
            if (!blockDateTime)                                     //pokud neni blokovany zapis datetime
            {              //definuj promennou dateTime jako aktualni cas a zformatuj ji 
                dateTimeString = dateTimeNow.ToString("[yyyy.MM.dd HH:mm:ss.fff]: ", CultureInfo.InvariantCulture);
            }
            using (StreamWriter fs = File.AppendText(@"sms.log"))   //otevri soubor sms.log pro appendovani jako fs
            {
                fs.WriteLine(dateTimeString + message);             //zapis do logu dateTimeString a zpravu
                if (verbose || forceVerbose)                        //pokud je zapnute logovani, a/nebo se zprava loguje vzdy
                {
                    Console.WriteLine(dateTimeString + message);    //zapis ji jeste do konzole
                }
            }
            if (criticalLog)
            {
                using (StreamWriter critical_fs = File.AppendText(@"critical_log.log"))   //otevri soubor sms.log pro appendovani jako fs
                {
                    critical_fs.WriteLine(dateTimeString + message);    //zapis do logu dateTimeString a zpravu
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

        static void SendSMSs(List<string[]> newArticles, Boolean isTeacher)                     //vygeneruje a posle SMS s zadanymi newArticles
        {
            if ((File.Exists(telNum_fname) && ! isTeacher) || (File.Exists(telNumTeacher_fname) && isTeacher))
            {
                Log("List of phone numbers exists! Parsing...");
                string telNum_content;
                if (isTeacher)
                {
                    telNum_content = File.ReadAllText(telNumTeacher_fname);
                }
                else
                {
                    telNum_content = File.ReadAllText(telNum_fname);
                }
                string[] telNum_lines = telNum_content.Split('\n');
                if (telNum_lines.Length > 0 && !string.IsNullOrEmpty(telNum_content))
                {
                    long smsCount = 0;
                    if (File.Exists("sms.count"))
                    {
                        try
                        {
                            smsCount = Convert.ToInt64(File.ReadAllText("sms.count"));
                        } catch { }
                    }

                    long intranetSMSToSend = telNum_lines.LongLength * newArticles.LongCount();
                    long sentSMS = 0;
                    float progress = 0;
                    string progressString = "[                              ]";

                    string whoString;
                    if (isTeacher)
                    {
                        whoString = "teachers";
                    } 
                    else
                    {
                        whoString = "students";
                    }
                    Log(String.Format("List of {0} phone numbers contains {1} phone numbers!", whoString, telNum_lines.Length));
                    SerialPort portHandler = new System.IO.Ports.SerialPort(
                        comPort, 115200, System.IO.Ports.Parity.None, 8, System.IO.Ports.StopBits.One);
                    portHandler.WriteTimeout = 500;
                    portHandler.Open();
                    Log(String.Format("STARTING SMS SENDING JOB FOR {0}!", whoString.ToUpper()));
                    Log("Setting message format!");
                    portHandler.Write("AT+CMGF=1\r");
                    while(portHandler.BytesToRead == 0);
                    string[] response = portHandler.ReadExisting().Replace("\n", "").Split('\r');
                    if (response.Length > 2)
                    {
                        if (response[2] == "OK")
                        {
                            Log("Message format succesfully set!");
                            portHandler.Write("AT+CSCS=\"8859-1\"\r");
                            while (portHandler.BytesToRead == 0) ;
                            response = portHandler.ReadExisting().Replace("\n", "").Split('\r');
                            if (response.Length > 2)
                            {
                                if (response[2] == "OK")
                                {
                                    Log("Encoding succesfully set!");
                                    int currentLine = 1;
                                    foreach (string telNum_line in telNum_lines)
                                    {
                                        if (!string.IsNullOrEmpty(telNum_line) && !string.IsNullOrWhiteSpace(telNum_line) && telNum_line.Length > 1)
                                        {
                                            string recipient_name = telNum_line.Split(';')[1].Replace("\n", "").Replace("\r", "").Trim();
                                            string telNum_asString = telNum_line.Split(';')[0].Replace("\n", "").Replace("\r", "").Trim();
                                            string telNum_asStringTrimmed = telNum_asString.Replace("\n", "").Replace("\r", "").Trim();
                                            if (!string.IsNullOrEmpty(telNum_asStringTrimmed))
                                            {
                                                Int64 telNum = Convert.ToInt64(telNum_asString);
                                                if (telNum > 420000000000 && telNum <= 420999999999)
                                                {
                                                    Log(String.Format("STARTING JOB FOR NUMBER {0} - {1}!", telNum_asStringTrimmed, recipient_name));
                                                    foreach (string[] article in newArticles)
                                                    {
                                                        progress = (float) sentSMS / intranetSMSToSend;
                                                        progressString = "[";
                                                        for (int i = 0; i < Math.Round(progress*50); i++)
                                                        {
                                                            progressString += "#";
                                                        }
                                                        while (progressString.Length < 51)
                                                        {
                                                            progressString += " ";
                                                        }
                                                        progressString += "]";

                                                        Log(String.Format("Setting recipient number to {0} - {1}!", telNum_asStringTrimmed, recipient_name));
                                                        string title;
                                                        if (isTeacher)
                                                        {
                                                            title = "SBOROVNA";
                                                        }
                                                        else
                                                        {
                                                            title = "INTRANET";
                                                        }
                                                        Log(String.Format("Sending message {0} out of {1} for {2}...", sentSMS, intranetSMSToSend, title), false, true);
                                                        Log(String.Format("Actual progress is {0}%.", progress*100));
                                                        Log(progressString);
                                                        Log(String.Format("Total SMS sent: {0}", smsCount));

                                                        string header = String.Format("Nove oznameni {0} od ", title);
                                                        if (article[6] == "EDIT")
                                                        {
                                                            header = String.Format("Upravene oznameni {0} od ", title);
                                                        }
                                                        string teloZpravy = RemoveDiacritics(header + article[2] + ": " + article[1] + "\n" + article[3] + "\n" + article[4]);
                                                        portHandler.Write(String.Format("AT+CMGS=\"{0}\"\r", telNum_asStringTrimmed));
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
                                                        //System.IO.File.WriteAllText(String.Format(@"{0}.txt",article[0]), teloZpravy);
                                                        while (portHandler.BytesToRead < teloZpravy.Length) System.Threading.Thread.Sleep(500); ;
                                                        string response_body = portHandler.ReadExisting().Replace("\n", "");
                                                        //response = response_wp.Split('\r');
                                                        if (response_body.Contains("ERROR"))
                                                        {
                                                            //portHandler.Write("AT+CMEE=2\r");
                                                            //while (portHandler.BytesToRead == 0) ;
                                                            //portHandler.DiscardInBuffer();
                                                            Log(String.Format("AT server returned ERROR state while sending message body! Skipping message!"));
                                                            System.Threading.Thread.Sleep(1000);
                                                            continue;
                                                        }
                                                        Log("Sending message body succesfull! Waiting for SMS to send!");
                                                        while (portHandler.BytesToRead == 0) System.Threading.Thread.Sleep(500); ;
                                                        System.Threading.Thread.Sleep(500);
                                                        response = portHandler.ReadExisting().Replace("\n", "").Split('\r');
                                                        if (response.Length < 2)
                                                        {
                                                            Log(String.Format("SMS was probably not send, because response is unknown: {0}", response[0]));
                                                            sentSMS++;
                                                            File.WriteAllText("sms.count", smsCount.ToString());
                                                        }
                                                        if (response[1].Contains("ME"))
                                                        {
                                                            Log("SMS was sent to server itself! Critical stop!");
                                                            sentSMS++;
                                                            File.WriteAllText("sms.count", smsCount.ToString());
                                                            while (portHandler.BytesToRead == 0) System.Threading.Thread.Sleep(500); ;
                                                            System.Threading.Thread.Sleep(500);
                                                            portHandler.DiscardInBuffer();
                                                        }
                                                        else if (response[3].Contains("OK"))
                                                        {
                                                            Log(String.Format("SMS to number {0} - {1} succesfully sent!", telNum_asStringTrimmed, recipient_name));
                                                            smsCount++;
                                                            sentSMS++;
                                                            File.WriteAllText("sms.count", smsCount.ToString());
                                                        }
                                                        else
                                                        {
                                                            Log(String.Format("Sending SMS to number {0} - {1} failed!", telNum_asStringTrimmed, recipient_name));
                                                            sentSMS++;
                                                            File.WriteAllText("sms.count", smsCount.ToString());
                                                        }
                                                    }
                                                    Log(String.Format("JOB FOR NUMBER {0} - {1} COMPLETED!", telNum_asStringTrimmed, recipient_name));
                                                }
                                                else
                                                {
                                                    Log("\"" + telNum_asStringTrimmed + "\" is not valid phone number in format 00420abcdefghi!");
                                                }
                                            }
                                            else
                                            {
                                                Log("\"" + telNum_asStringTrimmed + "\" is not valid phone number in format 00420abcdefghi!");
                                            }
                                        }
                                        else
                                        {
                                            Log(String.Format("Line {0} does not contain valid number and recipient name!", currentLine));
                                        }
                                        currentLine++;
                                    }
                                }
                                else
                                {
                                    Log(String.Format("Unable to set encoding! Error: {0}!", response[2]));
                                }
                            }
                            else
                            {
                                Log("Unable to set encoding! Invalid AT server response!");
                            }
                        }
                        else
                        {
                            Log(String.Format("Unable to set message format! Error: {0}!", response[2]));
                        }
                        portHandler.Close();
                        Log(String.Format("SMS SENDING JOB FOR {0} FINISHED!", whoString.ToUpper()));
                    }
                    else
                    {
                        Log("Unable to set message format! Invalid AT server response!");
                    }
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

        static bool CheckIsLocalPath(string path)                                               //zkontroluje, jestli se jedna o localpath, nebo URL
        {
            if (path.StartsWith(@"http://"))
            {
                return false;
            }

            return new Uri(path).IsFile;
        }

        static bool CheckURLfileExists(string url)
        {
            HttpWebResponse response = null;
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "HEAD";
            
            try
            {
                response = (HttpWebResponse)request.GetResponse();
            }
            catch 
            {
                /* A WebException will be thrown if the status of the response is not `200 OK` */
                return false;
            }
            if (response != null)
            {
                response.Close();
                return true;
            }
            return false;
        }                                           //zkontroluje, jestli zadane URL opravdu existuje

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

        static void CheckForNewestXML(string fname, Boolean isTeacher = false)                  //kontroluje hashe XML
        {
            if (CheckIsLocalPath(fname))
            {
                if (!File.Exists(fname))
                {
                    Log(String.Format("XML file with RSS feed \"{0}\" is local file and does not exists! Skipping job...", fname));
                    return;
                }
                byte[] nhash = CalcMD5forFile(fname);
                if (File.Exists(fname + ".md5"))
                {
                    byte[] ohash = File.ReadAllBytes(fname + ".md5");
                    if (!MD5.Equals(BitConverter.ToString(nhash), BitConverter.ToString(ohash)))
                    {
                        Log("XML feed changed from last update! Parsing RSS feed...");
                        File.WriteAllBytes(fname + ".md5", nhash);
                    }
                    else
                    {
                        Log("XML feed se od posledniho prubehu nezmenil! Cekam...");
                        return;
                    }
                }
                else
                {
                    //hash nesouhlasi, zpracuj soubor
                    Log("First start of aplication! Control checksum does not exist! Parsing RSS feed...");
                    File.WriteAllBytes(fname + ".md5", nhash);
                }
            }
            else
            {
                if (!CheckURLfileExists(fname))
                {
                    Log(String.Format("XML file with RSS feed \"{0}\" is URL adress and does not appear to exist or to be an XML file! Skipping job...", fname));
                    return;
                }
            }
            List<string[]> newArticles = ParseActualXML(fname, isTeacher);
            if (newArticles.Count > 0)
            {
                SendSMSs(newArticles, isTeacher);
            }
        }

        static List<string[]> ParseActualXML(string feedFName, Boolean isTeacher)
        {
            List<string[]> seznamClanku = new List<string[]>();
            XmlDocument xmlDoc = new XmlDocument(); // Create an empty XML document object

            if (CheckIsLocalPath(feedFName))
            {
                try
                {
                    xmlDoc.Load(feedFName); // Load the XML document from the specified file
                }
                catch
                {
                    Log(String.Format("Failed parsing XML file with RSS feed \"{0}\"! File is not XML, or corrupted! Skipping job...", feedFName));
                    return seznamClanku;
                }
            }
            else
            {
                try
                {
                    string url = feedFName;
                    XmlReader reader = XmlReader.Create(url);
                    xmlDoc.Load(reader); // Load the XML document from the specified url
                }
                catch
                {
                    Log(String.Format("Failed parsing XML file with RSS feed \"{0}\"! File is not XML, or corrupted! Skipping job...", feedFName));
                    return seznamClanku;
                }
            }
        

            Log("Loading XML!");

            XmlNodeList nodeList = xmlDoc.GetElementsByTagName("item");
            Log("XML loaded succesfully!");

            Log("Reading XML content...");
            foreach (XmlNode node in nodeList)
            {
                XmlElement XMLclanek = node as XmlElement;
                string id = XMLclanek.GetElementsByTagName("guid")[0].InnerText;
                id = id.Substring(id.IndexOf("ID=") + 3);
                string nazev = XMLclanek.GetElementsByTagName("title")[0].InnerText.Trim();
                string autor = XMLclanek.GetElementsByTagName("author")[0].InnerText.Trim();
                autor = autor.Substring(0, 3) + autor.Substring(autor.IndexOf(" ") + 1, 3);
                string obsah = XMLclanek.GetElementsByTagName("description")[0].InnerText.Trim();
                obsah = Regex.Replace(obsah, @"\p{C}+", string.Empty);
                nazev = Regex.Replace(nazev, @"\p{C}+", string.Empty);
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
                nazev = HtmlUtilities.ConvertToPlainText(nazev);
                string obsah_bezPriloh = obsah.Substring(0, Math.Max(obsah.IndexOf("Attachments:")-1,0));
                obsah = obsah.Replace("Attachments:", string.Empty).Replace("Body:", string.Empty).Replace((char)0xA0,(char)0x20);
                foreach(KeyValuePair<char, char> pair in charsToReplace)
                {
                    obsah = obsah.Replace(pair.Key, pair.Value);
                }
                nazev = nazev.Replace((char)0xA0, (char)0x20);
                if (obsah.Length > 700)
                {
                    obsah = obsah.Substring(0, 700) + "...\nZprava byla zkracena! Pokracovani:";
                }
                if (nazev.Length > 100)
                {
                    nazev = nazev.Substring(0, 100) + "...";
                }
                obsah = obsah.Trim();
                string link = XMLclanek.GetElementsByTagName("link")[0].InnerText.Trim();
                string datum = XMLclanek.GetElementsByTagName("pubDate")[0].InnerText.Trim();
                if (!string.IsNullOrEmpty(GetChecksumForID(Convert.ToInt32(id))))
                {
                    if (CalcMD5(obsah_bezPriloh) == GetChecksumForID(Convert.ToInt32(id)) || string.IsNullOrWhiteSpace(obsah_bezPriloh))
                    {
                        Log(String.Format("There already is record for article {0} and checksum is same as in RSS feed!", id));
                    }
                    else
                    {
                        Log(String.Format("There already is record for article {0}, but checksums are not equal!", id));
                        int rozdil = CalcLeventhainDist(obsah_bezPriloh, GetContentForID(Convert.ToInt32(id)));
                        float procenta = (float)rozdil / obsah_bezPriloh.Length * 100;
                        Log(String.Format("Article {0} differs by {1} characters, which is {2}% of string! That is probably not imprtant change, skipping!", id, rozdil, procenta));
                        if (procenta > 30)
                        {
                            Log(String.Format("Article {0} differs by {1} characters, which is more than 30% of string! Adding to SMS job as updated article!", id, rozdil));
                            string[] clanekArr = new string[] { id, nazev, autor, obsah, link, datum, "EDIT" };
                            seznamClanku.Add(clanekArr);
                        }
                        SetValuesForID(Convert.ToInt32(id), obsah_bezPriloh, CalcMD5(obsah_bezPriloh));
                    }
                }
                else
                {
                    Log(String.Format("Article {0} does not have record! Adding to SMS job as new article!", id));
                    SetValuesForID(Convert.ToInt32(id), obsah_bezPriloh, CalcMD5(obsah_bezPriloh));
                    string[] clanekArr = new string[] { id, nazev, autor, obsah, link, datum, "NEW" };
                    seznamClanku.Add(clanekArr);
                }
            }
            Log(String.Format("From least update {0} articles was added or changed!", seznamClanku.Count));
            return (seznamClanku);
        }

        static void Main(string[] args)
        {
            Log("--------------------------\r\n-------PROGRAM INIT-------\r\n--------------------------", true);
            try
            {
                var parser = new FileIniDataParser();
                IniData data = parser.ReadFile("conf.ini");
                feedFName = data["GeneralConfiguration"]["feedFile"];
                teachersFeedFName = data["GeneralConfiguration"]["teachersFeedFile"];
                if (data["GeneralConfiguration"]["displayLog"].ToLower() == "true")
                {
                    verbose = true;
                }
                else
                {
                    verbose = false;
                }
                Log(String.Format("Source file with RSS feed is: {0}, ", feedFName) + ((CheckIsLocalPath(feedFName)) ? "which appears to be local file!" : "which appears to be an URL!"));
                Log(String.Format("Source file with teachers RSS feed is: {0}!", teachersFeedFName) + ((CheckIsLocalPath(teachersFeedFName)) ? "which appears to be local file!" : "which appears to be an URL!"));
                telNum_fname = data["GeneralConfiguration"]["numbersList"];
                telNumTeacher_fname = data["GeneralConfiguration"]["teachersNumbersList"];
                Log(String.Format("Source file with phone numbers is: {0}!", telNum_fname));
                Log(String.Format("Source file with techaers phone numbers is: {0}!", telNumTeacher_fname));
                updateInt = Convert.ToInt32(data["GeneralConfiguration"]["updateInterval"]);
            }
            catch
            {
                Log("Parsing ini file failed or ini file was not found!!", false, true, true);
                //Console.ReadKey();
                return;
            }
            Log("Configuration file loaded and parsed sucesfully!");
            try
            {
                Log("Loading cache file!");
                sqlite_conn = new SQLiteConnection("Data Source=newArticles.cf; Version = 3; Compress = True; ");
                sqlite_conn.Open();
                Log("Cache file loaded sucesfully!");

                Log("Checking for cache file consistency!");
                SQLiteCommand command = new SQLiteCommand(sqlite_conn)
                {
                    CommandText = "SELECT name FROM sqlite_master WHERE name='newArticles'"
                };
                var tableName = command.ExecuteScalar();
                command.Dispose();
                if (tableName != null && tableName.ToString() == "newArticles")
                {
                    Log("Checking for cache file consistency succeed!");
                }
                else
                {
                    Log("Checking for cache file consistency failed! Creating new empty cache file!");
                    CreateCacheFileStruct();
                }
            }
            catch
            {
                Log("Opening cache file failed!", false, true, true);
                //Console.ReadKey();
                return;
            }
            GetATCOM();

            Stopwatch stopwatch = new Stopwatch();

            while (true)
            {
                stopwatch.Reset();
                stopwatch.Start();
                CheckForATonCOM(comPort);
                CheckForNewestXML(feedFName);
                CheckForNewestXML(teachersFeedFName, true);
                stopwatch.Stop();
                System.Threading.Thread.Sleep((int) Math.Max(updateInt - stopwatch.ElapsedMilliseconds,0));
            }
        }
    }
}
