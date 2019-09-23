using IniParser;
using IniParser.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SMS_srv_editor
{
    public partial class Form1 : Form
    {
        static string wdir;
        static bool verbose;
        static string telnumlist;
        static string xmlpath;
        static string oldinput;
        static int updateint;
        static bool confOpened = false;

        public Form1()
        {
            InitializeComponent();
        }

        private void folderBrowserDialog1_HelpRequest(object sender, EventArgs e)
        {

        }

        private void select_wdir_but_Click(object sender, EventArgs e)
        {
            using (var wd_dialog = new FolderBrowserDialog())
            {
                DialogResult result = wd_dialog.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(wd_dialog.SelectedPath))
                {
                    if (File.Exists(wd_dialog.SelectedPath + "\\conf.ini"))
                    {
                        wdir = wd_dialog.SelectedPath;
                        confOpened = true;
                        wdir_combobox.Text = wd_dialog.SelectedPath;
                        loadConfig(wdir);
                        loadPhoneNumbers();
                    }
                    else
                    {
                        DialogResult dialogResult = MessageBox.Show("No config file found in selected folder. Create new one?", "No config file found", MessageBoxButtons.YesNo);
                        if (dialogResult == DialogResult.Yes)
                        {
                            wdir = wd_dialog.SelectedPath;
                            confOpened = true;
                            wdir_combobox.Text = wd_dialog.SelectedPath;
                            loadConfig(wdir);
                            loadPhoneNumbers();
                        }
                    }
                }
            }
        }

        private void createConfig(string working_directory)
        {
            string inifile_template = String.Format(@";Main configuration file of SPSD Masna's SMS server
[GeneralConfiguration]

;Update interval in msecs
updateInterval = 1000

;Telephone number list file
numbersList = {0}\cisla.num

;Feed file
;feedFile = {0}\rss.xml
feedFile = http://mujweb/admin/kiosek/feed.xml

;If log messages should be displayed in console window
displayLog = true", working_directory);
            File.WriteAllText(wdir + "\\conf.ini", inifile_template);
        }

        private void flushConfig()
        {
            if (!confOpened)
            {
                DialogResult dialogResult = MessageBox.Show("No config file is opened. Do you want to open one?", "No config file opened", MessageBoxButtons.YesNo);
                if (dialogResult == DialogResult.Yes)
                {
                    using (var wd_dialog = new FolderBrowserDialog())
                    {
                        DialogResult result = wd_dialog.ShowDialog();

                        if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(wd_dialog.SelectedPath))
                        {
                            if (File.Exists(wd_dialog.SelectedPath + "\\conf.ini"))
                            {
                                wdir = wd_dialog.SelectedPath;
                                confOpened = true;
                                wdir_combobox.Text = wd_dialog.SelectedPath;
                                loadConfig(wdir);
                                loadPhoneNumbers();
                            }
                            else
                            {
                                DialogResult wd_dialogResult = MessageBox.Show("No config file found in selected folder. Create new one?", "No config file found", MessageBoxButtons.YesNo);
                                if (wd_dialogResult == DialogResult.Yes)
                                {
                                    wdir = wd_dialog.SelectedPath;
                                    confOpened = true;
                                    wdir_combobox.Text = wd_dialog.SelectedPath;
                                    createConfig(wdir);
                                    loadConfig(wdir);
                                    loadPhoneNumbers();
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                var parser = new FileIniDataParser();
                IniData data = parser.ReadFile(wdir + "\\conf.ini");
                data["GeneralConfiguration"]["feedFile"] = xmlpath;
                if (verbose)
                {
                    data["GeneralConfiguration"]["displayLog"] = "true";
                }
                else
                {
                    data["GeneralConfiguration"]["displayLog"] = "false";
                }
                data["GeneralConfiguration"]["numbersList"] = telnumlist;
                data["GeneralConfiguration"]["updateInterval"] = Convert.ToString(updateint);
                parser.WriteFile(wdir + "\\conf.ini", data);
            }
            Console.WriteLine("Flush conf!");
        }

        private void loadConfig(string wdir)
        {
            this.Text = "Grafický editor SMS serveru: " + wdir;
            var parser = new FileIniDataParser();
            IniData data = parser.ReadFile(wdir+"\\conf.ini");
            xmlpath = data["GeneralConfiguration"]["feedFile"];
            xmlpat_combobox.Text = xmlpath;
            if (data["GeneralConfiguration"]["displayLog"].ToLower() == "true")
            {
                verbose = true;
                verbose_checkbox.Checked = true;
            }
            else
            {
                verbose = false;
                verbose_checkbox.Checked = false;
            }
            telnumlist = data["GeneralConfiguration"]["numbersList"];
            numpat_combobox.Text = telnumlist;
            updateint = Math.Max(Convert.ToInt32(data["GeneralConfiguration"]["updateInterval"]), 500);
            update_interval_updown.Value = updateint;
        }

        private void loadPhoneNumbers()
        {
            if (File.Exists(telnumlist))
            {
                string teleText = File.ReadAllText(telnumlist);
                string[] cisla = teleText.Split('\n');
                if (cisla.Length > 0 && !string.IsNullOrEmpty(teleText))
                {
                    phonenumber_table.Rows.Clear();
                    foreach (string cislo in cisla)
                    {
                        try
                        {
                            phonenumber_table.Rows.Add(cislo.Split(';')[1], cislo.Split(';')[0]);
                        }
                        catch
                        {
                            phonenumber_table.Rows.Add(String.Empty, cislo.Split(';')[0]);
                        }
                    }
                }
            }
        }

        private void flushPhoneNumbers()
        {
            string buffer = String.Empty;
            for (int i = 0; i < phonenumber_table.Rows.Count-1; i++)
            {
                string name = String.Empty;
                try
                {
                    name = phonenumber_table.Rows[i].Cells[0].Value.ToString();
                }
                catch { }
                string number = String.Empty;
                try
                {
                    number = phonenumber_table.Rows[i].Cells[1].Value.ToString();
                }
                catch { }
                string radek = number + ";" + name + "\r\n";
                buffer += radek;
            }
            File.WriteAllText(telnumlist, buffer.TrimEnd());
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void update_interval_updown_ValueChanged(object sender, EventArgs e)
        {
            updateint = Convert.ToInt32(update_interval_updown.Value);
            flushConfig();
        }

        private void verbose_checkbox_CheckedChanged(object sender, EventArgs e)
        {
            verbose = verbose_checkbox.Checked;
            flushConfig();
        }

        private void wdir_combobox_Leave(object sender, EventArgs e)
        {
            if (File.Exists(wdir_combobox.Text.Trim() + "\\conf.ini"))
            {
                wdir = wdir_combobox.Text;
                confOpened = true;
                wdir_combobox.Text = wdir_combobox.Text;
                loadConfig(wdir);
                loadPhoneNumbers();
            }
            else if (!string.IsNullOrWhiteSpace(wdir_combobox.Text))
            {
                DialogResult dialogResult = MessageBox.Show("No config file found in selected folder. Create new one?", "No config file found", MessageBoxButtons.YesNo);
                if (dialogResult == DialogResult.Yes)
                {
                    wdir = wdir_combobox.Text;
                    confOpened = true;
                    wdir_combobox.Text = wdir_combobox.Text;
                    createConfig(wdir);
                    loadConfig(wdir);
                    loadPhoneNumbers();
                }
            }
        }

        private void wdir_combobox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                wdir_combobox.Text = oldinput;
            }
            else if (e.KeyCode == Keys.Enter)
            {
                if (File.Exists(wdir_combobox.Text.Trim() + "\\conf.ini"))
                {
                    wdir = wdir_combobox.Text;
                    confOpened = true;
                    wdir_combobox.Text = wdir_combobox.Text;
                    loadConfig(wdir);
                    loadPhoneNumbers();
                }
                else if (!string.IsNullOrWhiteSpace(wdir_combobox.Text))
                {
                    DialogResult dialogResult = MessageBox.Show("No config file found in selected folder. Create new one?", "No config file found", MessageBoxButtons.YesNo);
                    if (dialogResult == DialogResult.Yes)
                    {
                        wdir = wdir_combobox.Text;
                        confOpened = true;
                        wdir_combobox.Text = wdir_combobox.Text;
                        createConfig(wdir);
                        loadConfig(wdir);
                        loadPhoneNumbers();
                    }
                }
            }

        }

        private void wdir_combobox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (File.Exists(wdir_combobox.Text.Trim() + "\\conf.ini"))
            {
                wdir = wdir_combobox.Text;
                confOpened = true;
                wdir_combobox.Text = wdir_combobox.Text;
                loadConfig(wdir);
                loadPhoneNumbers();
            }
            else if (!string.IsNullOrWhiteSpace(wdir_combobox.Text))
            {
                DialogResult dialogResult = MessageBox.Show("No config file found in selected folder. Create new one?", "No config file found", MessageBoxButtons.YesNo);
                if (dialogResult == DialogResult.Yes)
                {
                    wdir = wdir_combobox.Text;
                    confOpened = true;
                    wdir_combobox.Text = wdir_combobox.Text;
                    createConfig(wdir);
                    loadConfig(wdir);
                    loadPhoneNumbers();
                }
            }
        }

        private void wdir_combobox_Enter(object sender, EventArgs e)
        {
            oldinput = wdir_combobox.Text;
        }

        private void select_xmlpat_button_Click(object sender, EventArgs e)
        {
            using (var xmlpat_dialog = new OpenFileDialog())
            {
                xmlpat_dialog.DefaultExt = ".xml";
                xmlpat_dialog.InitialDirectory = "C:/";
                xmlpat_dialog.Multiselect = false;

                DialogResult result = xmlpat_dialog.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(xmlpat_dialog.FileName))
                {
                    if (File.Exists(xmlpat_dialog.FileName))
                    {
                        xmlpath = xmlpat_dialog.FileName;
                        xmlpat_combobox.Text = xmlpat_dialog.FileName;
                        flushConfig();
                    }
                    else
                    {
                        DialogResult dialogResult = MessageBox.Show("{xmlpat_dialog.FileName} is not a file!", "Is not a file", MessageBoxButtons.YesNo);
                        if (dialogResult == DialogResult.Yes)
                        {
                            xmlpath = xmlpat_dialog.FileName;
                            xmlpat_combobox.Text = xmlpat_dialog.FileName;
                            flushConfig();
                        }
                        else if (dialogResult == DialogResult.No)
                        {
                        }
                    }
                }
            }

        }

        private void select_numpat_button_Click(object sender, EventArgs e)
        {
            using (var numpat_dialog = new OpenFileDialog())
            {
                numpat_dialog.DefaultExt = ".xml";
                numpat_dialog.InitialDirectory = "C:/";
                numpat_dialog.Multiselect = false;

                DialogResult result = numpat_dialog.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(numpat_dialog.FileName))
                {
                    if (File.Exists(numpat_dialog.FileName))
                    {
                        telnumlist = numpat_dialog.FileName;
                        numpat_combobox.Text = numpat_dialog.FileName;
                        flushConfig();
                    }
                    else
                    {
                        DialogResult dialogResult = MessageBox.Show("{numpat_dialog.FileName} is not a file!", "Is not a file", MessageBoxButtons.YesNo);
                        if (dialogResult == DialogResult.Yes)
                        {
                            telnumlist = numpat_dialog.FileName;
                            numpat_combobox.Text = numpat_dialog.FileName;
                            flushConfig();
                        }
                        else if (dialogResult == DialogResult.No)
                        {
                        }
                    }
                }
            }

        }

        private void phonenumber_table_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            flushPhoneNumbers();
        }

        private void xmlpat_combobox_Leave(object sender, EventArgs e)
        {
            xmlpath = xmlpat_combobox.Text;
            flushConfig();
        }

        private void xmlpat_combobox_Validated(object sender, EventArgs e)
        {
            xmlpath = xmlpat_combobox.Text;
            flushConfig();
        }

        private void xmlpat_combobox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                xmlpath = xmlpat_combobox.Text;
                flushConfig();
            }
            else if (e.KeyCode == Keys.Escape)
            {
                xmlpath = String.Empty;
                xmlpat_combobox.Text = String.Empty;
            }
        }
    }
}
