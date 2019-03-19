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
/*using System.ServiceModel;
using System.ServiceModel.Syndication;*/

namespace SMS_server
{ 
    class Program
    {
        //nazev souboru s feedem
        static string feedFName = @"H:\VS2017\SMS_server\SMS_server\rss.xml";

        //vypisuj log do command okna
        static bool verbose = true;


        static System.IO.Ports.SerialPort _serialPort;

        //logovaci Fce
        static void Log(string message, bool wd = false)
        {
            string dt = String.Empty;
            if (!wd)
            {
                DateTime dateTime = DateTime.Now;
                dt = dateTime.ToString("[MM.dd.yyyy HH:mm:ss.fff]:", CultureInfo.InvariantCulture);
            }
            using (StreamWriter fs = File.AppendText(@"sms.log"))
            {
                fs.WriteLine(dt + message);
                if (Program.verbose)
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
                        ParseActualXML();
                        File.WriteAllBytes(fname+".md5", nhash);
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
                    ParseActualXML();
                    File.WriteAllBytes(fname+".md5", nhash);
                    SerialPort _serialPort = new SerialPort("COM1", 19200, Parity.None, 8, StopBits.One);
                }
            }
            else
            {
                Log("XML feed neexistuje! Cekam...");
            }
        }

        static void ParseActualXML()
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
                /*while (obsah.IndexOf("<a href") != -1)
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
                obsah = obsah.Replace("Attachments:", string.Empty).Replace("Body:", string.Empty).Trim();*/
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
            //return (obsahXML);
        }

        static void MinTimerTick(object sender, EventArgs e)
        {
            CheckForNewestXML(feedFName);
        }

        static void Main(string[] args)
        {
            Log("--------------------------\n-------PROGRAM INIT-------\n--------------------------", true);
            Log("Program start");
            Timer timer1 = new Timer
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
