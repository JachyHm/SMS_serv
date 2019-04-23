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
/*using System.ServiceModel;
using System.ServiceModel.Syndication;*/

namespace SMS_server
{
    class Program
    {
        //nazev souboru s feedem
        static string feedFName;

        //nazev souboru s telCislama
        static string telNum;

        //COM port
        static string comPort;

        //vypisuj log do command okna
        static bool verbose = true;

        //blokovani noveho update, pokud neskoncil stary
        static bool blokujUpd = false;

        /*//import message handlingu
        [DllImport("H:\\VS2017\\SMS_server_CPP\\Release\\SMS_server_CPP.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void sendSMS_CsHandler(char[] com, char[] number, char[] message);*/

        private static void GetATCOM()
        {
            string[] portsList = SerialPort.GetPortNames();
            foreach (string port in portsList)
            {
                try
                {
                    SerialPort portHandler = new System.IO.Ports.SerialPort(
                        port, 115200, System.IO.Ports.Parity.None, 8, System.IO.Ports.StopBits.One);
                    portHandler.Open();
                    portHandler.Write("AT\r");
                    System.Threading.Thread.Sleep(500);
                    string messageReaded = portHandler.ReadExisting();
                    portHandler.Close();
                    if (messageReaded.Contains("AT\r\r\nOK\r\n"))
                    {
                        comPort = port;
                        Log(String.Format("AT server je na {0}!", port));
                        return;
                    }
                    else
                    {
                        Log(String.Format("Na {0} není AT server, neboť odpověď je \"{1}\"!", port, messageReaded));
                    }
                }
                catch
                {
                    Log(String.Format("{0} se nepodařilo otevřít!", port));
                    continue;
                }
            }
            Log("AT server není připojen! Please connect it and press any key to retry!", false, true);
            Console.ReadKey();
            GetATCOM();
        }

        private static void CheckForATonCOM(string comPort)
        {
            try
            {
                SerialPort portHandler = new System.IO.Ports.SerialPort(
                    comPort, 115200, System.IO.Ports.Parity.None, 8, System.IO.Ports.StopBits.One);
                portHandler.Open();
                portHandler.Write("AT\r");
                System.Threading.Thread.Sleep(500);
                string messageReaded = portHandler.ReadExisting();
                portHandler.Close();
                if (!messageReaded.Contains("AT\r\r\nOK\r\n"))
                {
                    Log(String.Format("AT server byl odpojen, neboť odpověď je \"{0}\"! Please connect it and press any key to continue!", messageReaded), false, true);
                    Console.ReadKey();
                    GetATCOM();
                    CheckForATonCOM(comPort);
                }
                else
                {
                    Log(String.Format("AT server žije a je na {0}!",comPort));
                }
            }
            catch
            {
                Log("AT server byl odpojen! Please connect it and press any key to continue!", false, true);
                Console.ReadKey();
                GetATCOM();
                CheckForATonCOM(comPort);
            }
        }

        //logovaci Fce
        static void Log(string message, bool wdt = false, bool forceVerbose = false)
        {
            string dt = String.Empty;
            if (!wdt)
            {
                DateTime dateTime = DateTime.Now;
                dt = dateTime.ToString("[MM.dd.yyyy HH:mm:ss.fff]:", CultureInfo.InvariantCulture);
            }
            using (StreamWriter fs = File.AppendText(@"sms.log"))
            {
                fs.WriteLine(dt + message);
                if (verbose || forceVerbose)
                {
                    Console.WriteLine(dt + message);
                }
            }
        }

        //spocita MD5 zadaneho souboru a vrati jej jako pole bytu
        static byte[] CalcMD5(string fname)
        {
            using (var md5 = MD5.Create()) {
                using (var fs = File.OpenRead(fname))
                {
                    return md5.ComputeHash(fs);
                }
            }
        }

        //volane periodicky co 1 min, zkontroluje ci souhlasi hash a pripadne zahaji parsovani noveho xml
        static void CheckForNewestXML(string fname)
        {
            if (File.Exists(fname))
            {
                byte[] nhash = CalcMD5(fname);
                if (File.Exists(fname+".md5"))
                {
                    byte[] ohash = File.ReadAllBytes(fname+".md5");
                    if (!MD5.Equals(BitConverter.ToString(nhash), BitConverter.ToString(ohash)))
                    {
                        //hash nesohlasi, zpracuj soubor
                        Log("XML feed se zmenil! Parsuji...");
                        List<string[]> clanky = ParseActualXML();
                        File.WriteAllBytes(fname + ".md5", nhash);
                        if (File.Exists(telNum))
                        {
                            Log("Seznam telefonních čísel existuje! Parsuji...");
                            string teleText = File.ReadAllText(telNum);
                            string[] cisla = teleText.Split("\n");
                            if (cisla.Length > 0 && teleText != String.Empty)
                            {
                                Log("Seznam telefonních obsahuje " + cisla.Length + " telefonních čísel! Posílám SMS...");
                                SerialPort portHandler = new System.IO.Ports.SerialPort(
                                    comPort, 115200, System.IO.Ports.Parity.None, 8, System.IO.Ports.StopBits.One);
                                portHandler.Open();
                                portHandler.Write("AT+CMGF=1\r");
                                Log(portHandler.ReadLine());
                                portHandler.Write("AT+CSCS=\"8859-1\"\r");
                                Log(portHandler.ReadLine());
                                foreach (string cislo in cisla)
                                {
                                    if (cislo != String.Empty)
                                    {
                                        Int64 telNum = Convert.ToInt64(cislo);
                                        if (telNum > 420000000000 && telNum <= 420999999999)
                                        {
                                            foreach (string[] clanek in clanky)
                                            {
                                                string teloZpravy = Encoding.GetEncoding("ISO-8859-1").GetString(Encoding.Convert(Encoding.UTF8, Encoding.GetEncoding("ISO-8859-1"), Encoding.Default.GetBytes("Nowa wiadomosc od " + clanek[1] + ": " + clanek[0] + "\n" + clanek[2] + "\n" + clanek[3])));
                                                //sendSMS_CsHandler(comPort, cisloChar, teloZpravy);
                                                portHandler.Write(String.Format("AT+CMGS=\"{0}\"\r", cislo));
                                                Log(portHandler.ReadLine());
                                                portHandler.Write(String.Format("{0}\x1A\r", teloZpravy));
                                                Log(portHandler.ReadLine());
                                                portHandler.DiscardOutBuffer();
                                                portHandler.DiscardInBuffer();
                                                string messageReaded = portHandler.ReadExisting();
                                                if (!messageReaded.Contains("OK\r\n"))
                                                {
                                                    Log(String.Format("SMS na číslo {0} úspěšně odeslaná!", cislo));
                                                }
                                                else
                                                {
                                                    Log(String.Format("SMS na číslo {0} nebyla úspěšně odeslaná!", cislo));
                                                }
                                            }
                                        }
                                        else
                                        {
                                            Log("\"" + cislo + "\" není platné telefonní číslo!");
                                        }
                                    }
                                    else
                                    {
                                        Log("\"" + cislo + "\" není platné telefonní číslo!");
                                    }
                                }
                                portHandler.Close();
                            }
                            else
                            {
                                Log("Seznam telefonních čísel neobsahuje ani jedno číslo! Končím...");
                            }
                        }
                        else
                        {
                            Log("Seznam telefonních čísel neexistuje! Končím...");
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
                    Log("Prvni start aplikace! Parsuji...");
                    List<string[]> clanky = ParseActualXML();
                    File.WriteAllBytes(fname + ".md5", nhash);
                    if (File.Exists(telNum))
                    {
                        Log("Seznam telefonních čísel existuje! Parsuji...");
                        string teleText = File.ReadAllText(telNum);
                        string[] cisla = teleText.Split("\n");
                        if (cisla.Length > 0 && teleText != String.Empty)
                        {
                            Log("Seznam telefonních obsahuje " + cisla.Length + " telefonních čísel! Posílám SMS...");
                            foreach (string cislo in cisla)
                            {
                                if (cislo != String.Empty)
                                {
                                    Int64 telNum = Convert.ToInt64(cislo);
                                    if (telNum > 420000000000 && telNum <= 420999999999)
                                    {
                                        foreach (string[] clanek in clanky)
                                        {
                                            char[] teloZpravy = ("Nowa wiadomosc od " + clanek[1] + ": " + clanek[0] + "\n" + clanek[2] + "\n" + clanek[3]).ToCharArray();
                                            char[] cisloChar = cislo.ToCharArray();
                                            //sendSMS_CsHandler(comPort, cisloChar, teloZpravy);
                                            Log("SMS na číslo " + cislo + " úspěšně odeslaná!");
                                        }
                                    }
                                    else
                                    {
                                        Log("\"" + cislo + "\" není platné telefonní číslo!");
                                    }
                                }
                                else
                                {
                                    Log("\"" + cislo + "\" není platné telefonní číslo!");
                                }
                            }
                        }
                        else
                        {
                            Log("Seznam telefonních čísel neobsahuje ani jedno číslo! Končím...");
                        }
                    }
                    else
                    {
                        Log("Seznam telefonních čísel neexistuje! Končím...");
                    }
                }
            }
            else
            {
                Log("XML feed neexistuje! Cekam...");
            }
        }

        static List<string[]> ParseActualXML()
        {
            /*string url = "https://spsdmasna.sharepoint.com/_layouts/15/listfeed.aspx?List=%7B268E2C52-F89E-4C25-B2F5-5170C29B6A19%7D&Source=https%3A%2F%2Fspsdmasna%2Esharepoint%2Ecom%2FSitePages%2FHome%2Easpx";
            XmlReader reader = XmlReader.Create(url);*/

            List<string[]> seznamClanku = new List<string[]>();

            Log("Nacitam XML!");
            XmlDocument xmlDoc = new XmlDocument(); // Create an empty XML document object
            //xmlDoc.Load(reader); // Load the XML document from the specified file
            xmlDoc.Load(feedFName); // Load the XML document from the specified file
            
            XmlNodeList nodeList = xmlDoc.GetElementsByTagName("item");
            Log("XML nactene uspesne!");


            Log("Ctu obsah XML");
            foreach (XmlNode node in nodeList)
            {
                XmlElement XMLclanek = node as XmlElement;
                string nazev = XMLclanek.GetElementsByTagName("title")[0].InnerText;
                string autor = XMLclanek.GetElementsByTagName("author")[0].InnerText;
                autor = autor.Substring(autor.IndexOf(" ") + 1, 2) + autor.Substring(0, 2);
                string obsah = XMLclanek.GetElementsByTagName("description")[0].InnerText;
                while (obsah.IndexOf("<a href") != -1)
                {
                    obsah.Replace("https://", string.Empty);
                    int linkZac = obsah.IndexOf(">", obsah.IndexOf("<a href"));
                    int linkKon = obsah.IndexOf("</a>");
                    if (linkKon - linkZac > 35)
                    {
                        obsah = obsah.Substring(0, linkZac + 15) + "..." + obsah.Substring(linkKon - 15);
                    }
                    obsah = obsah.Substring(0, obsah.IndexOf("<a href")-1) + obsah.Substring(obsah.IndexOf(">", obsah.IndexOf("<a href"))+1);
                }
                obsah = obsah.Replace("<br", " <br");
                obsah = obsah.Replace("<p", " <p");
                obsah = HttpUtility.HtmlDecode(Regex.Replace(obsah, "<[^>]*(>|$)", string.Empty));
                obsah = obsah.Replace("Attachments:", string.Empty).Replace("Body:", string.Empty).Trim();
                string link = XMLclanek.GetElementsByTagName("link")[0].InnerText;
                string datum = XMLclanek.GetElementsByTagName("pubDate")[0].InnerText;
                /*Console.WriteLine("*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*--*-*");
                Console.WriteLine("Název: " + nazev);
                Console.WriteLine("Autor: " + autor);
                Console.WriteLine("Obsah: " + obsah);
                Console.WriteLine("Odkaz: " + link);
                Console.WriteLine("Datum: " + datum);
                Console.WriteLine("");*/
                string[] clanekArr = new string[] { nazev, autor, obsah, link, datum };
                seznamClanku.Add(clanekArr);
            }
            Log("Uspesne precteno "+seznamClanku.Count+" clanku!");
            return (seznamClanku);
        }

        static void MinTimerTick(object sender, EventArgs e)
        {
            if (!blokujUpd)
            {
                blokujUpd = true;
                CheckForATonCOM(comPort);
                CheckForNewestXML(feedFName);
                blokujUpd = false;
            }
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
                Log(String.Format("Zdrojový soubor s RSS feedem je: {0}!", feedFName));
                telNum = data["GeneralConfiguration"]["numbersList"];
                Log(String.Format("Zdrojový soubor se seznamem telefonních čísel je: {0}!", telNum));
            }
            catch
            {
                Log("Parsing ini file failed! Press any key to exit!", false, true);
                Console.ReadKey();
                return;
            }
            Log("Configuration file loaded sucesfully!");
            GetATCOM();
            System.Timers.Timer timer1 = new System.Timers.Timer
            {
                Interval = 1000
            };
            timer1.Enabled = true;
            timer1.Elapsed += MinTimerTick;
            while (true)
            {
            }
        }

    }
}
