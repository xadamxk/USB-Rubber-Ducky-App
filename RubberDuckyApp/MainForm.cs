using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using System.Windows.Forms;

/* To-Do List
 * TODO: Clean up variables
 * TODO: Clean up flow of methods
 * TODO: If >1 removable drive, check for drivename(?) for ducky and select it
 * TODO: Implement 'unmount/eject' button/code
 * TODO: Re-Code 'CheckDirectoryExists' method to say it created directory, not ask. or close program if say no
 * TODO: Add payload description tooltips
 * Firmware options?
 * Other features?
 */

namespace RubberDuckyApp
{
    public partial class MainForm : Form
    {
        private string selectedDrive = ""; // ex C:\
        private string selectedRemovableDrive = ""; // ex E:\
        private string javaEXELocation = ""; 
        private string duckyDirectory = ""; //C:\RubberDucky
        private string javaHome = ""; // C:\RubberDucky\java.exe
        private string encoderLocation = ""; // C:\RubberDucky\encoder.jar
        bool microSDCheck;
        private string[] defaultScriptsNameArray = new string[100]; // Cap default limit to 100 scripts
        private string[] customScriptsNameArray = new string[50]; // Cap custom limit to 50 scripts
        const string payloadHeader1 = "Select a Payload...";
        const string payloadHeader2 = "--- Custom Scripts ---";
        const string payloadHeader3 = "--- Default Scripts ---";

        readonly string[] keyboardStrings = 
        {
            "be", "br", "ca", "ch", "de", "dk",
            "es", "fi", "fr", "gb", "hr", "it",
            "pt", "ru", "si", "sv", "tr", "us"
        };

        const string encoderURL =
            "https://github.com/midnitesnake/USB-Rubber-Ducky/blob/master/Encoder/encoder.jar?raw=true";

        const string payloadsURL =
            "https://github.com/hak5darren/USB-Rubber-Ducky/wiki/Payloads";

        private readonly WebClient _client;

        public MainForm()
        {
            _client = new WebClient();
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Assign default language to English
            comboBox2.SelectedIndex = 17;

            // Refresh ComboBox with default drive
            RefreshHdComboBox();

            // Assign variable to selected drive (1)
            selectedDrive = comboBox1.SelectedItem.ToString();
            duckyDirectory = selectedDrive + "RubberDucky";

            // Check selected drive for Rubber Ducky Directory
            CheckDirectoryExists();

            // Check RubberDucky Directory for java
            CheckHomeDirectoryJava();

            // Check RubberDucky Directory for encoder
            CheckDirectoryEncoder();

            // Check RubberDucky Directory for input.txt
            CheckDirectoryInputFile();

            // Check for Scripts Directory
            CheckDirectoryScriptsExists();

            // Check for Default Scripts Directory
            CheckDirectoryScriptsDefaultExists();

            // Check for Custom Scripts Directory
            CheckDirectoryScriptsCustomExists();

            // Display MicroSD Options
            DisplayMicroSd();

            // Display Encoder Options
            RefreshEncoderElements();

            // Check Payload Count
            CheckPayloadCount();

            // Load Payload ComboBox
            LoadPayloadComboBox();

        }

        private void DisplayMicroSd() // Auto
        {
            if (comboBox1.SelectedIndex != -1)
            RefreshMicroSdComboBox();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // Make .bin Button
            label5.Text = "Creating .bin";
            string inputFile = "input.txt";
            string outputFile = "inject.bin";
            string languageProperty = keyboardStrings[comboBox2.SelectedIndex];
            string encodeCommand = "cmd /c \"cd " + duckyDirectory + " & java -jar encoder.jar -i " + 
                inputFile + " -o " + outputFile + " -l " + languageProperty + "\""; // cmd /c "cd c"\RubberDucky & java -jar encoder.jar -i input.txt -o inject.bin -l en"
        
                if (File.Exists(duckyDirectory + "\\" + outputFile))
                    File.Delete(duckyDirectory + "\\" + outputFile);

                Process process = new Process();
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                startInfo.FileName = "cmd.exe";
                startInfo.Arguments = encodeCommand;
                process.StartInfo = startInfo;
                process.Start();
            
            // .bin created in label
            label5.Text = ".bin Created In: " + duckyDirectory;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            // Copy .bin Button
            RefreshMicroSdComboBox();
            if (comboBox3.SelectedIndex != -1) // Check title array for removable disks
                    microSDCheck = true;

            if ( microSDCheck)
                File.Copy(duckyDirectory+"\\inject.bin",selectedRemovableDrive+"\\inject.bin", true);

            // .bin copied to label
            label5.Text = ".bin Copied To: " + comboBox3.SelectedItem;
        }

        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox3.SelectedIndex != -1)
                selectedRemovableDrive = comboBox3.SelectedItem.ToString();
        }

        private void RefreshHdComboBox()
        {
            // Assign drives to combobox1
            comboBox1.Items.Clear();
            // Credit to: http://stackoverflow.com/questions/623182/c-sharp-dropbox-of-drives
            foreach (var Drives in Environment.GetLogicalDrives())
            {
                DriveInfo DriveInf = new DriveInfo(Drives);

                if (DriveInf.IsReady)
                {
                    comboBox1.Items.Add(DriveInf.Name);
                }
            }

            if (comboBox1.Items.Count > 0)
                comboBox1.SelectedIndex = 0;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            // Refresh Button
            RefreshHdComboBox();
            RefreshEncoderElements();
            RefreshMicroSdComboBox();

            // Drives Refreshed label
            label5.Text = "Drives Refreshed.";
        }

        private void button5_Click(object sender, EventArgs e)
        {
            // Save Payload Button
            File.WriteAllText(duckyDirectory + "\\input.txt", textBox1.Text);

            // Payload Saved Label
            label5.Text = "Payload Saved.";
        }

        private void RefreshMicroSdComboBox()
        {
            // Assign removable drives to combobox3
            comboBox3.Items.Clear();
            foreach (var Drives in Environment.GetLogicalDrives())
            {
                DriveInfo driveInf = new DriveInfo(Drives);

                if (driveInf.IsReady)
                    if (driveInf.DriveType.ToString().Contains("Removable"))
                        comboBox3.Items.Add(driveInf.Name);
            }

            // Remove copies due to loop
            object[] distinctItems = (from Object o in comboBox3.Items select o).Distinct().ToArray();
            comboBox3.Items.Clear();
            comboBox3.Items.AddRange(distinctItems);

            if (comboBox3.Items.Count > 0)
                comboBox3.SelectedIndex = 0;
            // Add specifc drive name check here

            if (comboBox3.Items.Count == 0)
                MessageBox.Show(
                    "No removable storage device was found.\n" +
                    "Please insert the MicroSD card that will be used for your USB Rubber Ducky.\n" +
                    "Click \"Refresh\" when ready.");
        }

        private void RefreshEncoderElements()
        {
            if (comboBox1.SelectedIndex == -1)
            {
                button1.Hide();
                button2.Hide();
            }
            else if (comboBox1.SelectedIndex != -1)
            {
                button1.Show();
                button2.Show();
            }
        }

        private void CheckDirectoryExists()
        {
            if (!Directory.Exists(duckyDirectory))
            {
                DialogResult result = MessageBox.Show("The directory: \"" + duckyDirectory + "\"" + " does not exist.\nWould you like to create it for this application?",
                    "NOTICE!",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.None,
                    MessageBoxDefaultButton.Button3);

                if (result == DialogResult.Yes) // If user said yes
                {
                    Directory.CreateDirectory(duckyDirectory);
                    MessageBox.Show(duckyDirectory + "has been created!");
                }
            }
        }

        private void CheckHomeDirectoryJava()
        {
            string java32 = selectedDrive + "Program Files (x86)\\Java";
            string java64 = selectedDrive + "Program Files\\Java";
            javaHome = duckyDirectory + "\\java.exe";

            if (!File.Exists(javaHome)) // If java.exe is NOT in \RubberDucky
            {
                if (Directory.Exists(java32)) // Then Java32 is installed
                {
                string[] java32subdirectories = Directory.GetDirectories(java32);
                javaEXELocation = java32subdirectories.Last() + "\\bin\\java.exe"; // Selects most current version
                File.Copy(javaEXELocation, javaHome);
                }
                else if (Directory.Exists(java64)) // Then Java64 is installed
                {
                    string[] java64subdirectories = Directory.GetDirectories(java64);
                    javaEXELocation = java64subdirectories.Last() + "\\bin\\java.exe"; // Selects most current version
                    File.Copy(javaEXELocation, javaHome);
                }
                else
                {
                    MessageBox.Show("No java.exe file was found.\n\n" +
                                    "If you do not have Java installed, please download it from Oracle and re-run this program.\n\n" +
                                    "If you installed Java to a non-default directory, " +
                                    "please copy your java.exe file to:\n" + duckyDirectory + " and re-run this program.",
                        "ERROR: No Java File Found!",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Exclamation);
                }
            }
        }

        private void CheckDirectoryEncoder()
        {
            encoderLocation = duckyDirectory + "\\encoder.jar";
            if (!File.Exists(encoderLocation))
            {
                // Download encoder.jar if not found
                // Credits to: http://stackoverflow.com/questions/32223706/download-zipball-from-github-in-c-sharp
                using (var client = new WebClient())
                {
                    client.Headers.Add("user-agent", "Anything");
                    client.DownloadFile(encoderURL, encoderLocation);
                }
            }
        }

        private void CheckDirectoryInputFile()
        {
            if (!File.Exists(duckyDirectory + "\\input.txt"))
                File.CreateText(duckyDirectory + "\\input.txt");
        }

        private void CheckDirectoryScriptsExists()
        {
            DirectoriesCreate(duckyDirectory + "\\Scripts");
        }

        private void CheckDirectoryScriptsDefaultExists()
        {
            DirectoriesCreate(duckyDirectory + "\\Scripts\\Default");
        }

        private void CheckDirectoryScriptsCustomExists()
        {
            DirectoriesCreate(duckyDirectory + "\\Scripts\\Custom");
        }

        private static void DirectoriesCreate(string directory)
        {
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
        }

        private void LoadPayloadComboBox()
        {
            //Variables
            string defaultDirectory = duckyDirectory + "\\Scripts\\Default";
            string customDirectory = duckyDirectory + "\\Scripts\\Custom";
            var customNameList = new List<string>();
            var defaultNameList = new List<string>();

            comboBox4.Items.Add(payloadHeader1);
            comboBox4.Items.Add(payloadHeader2);
            // Add each custom payload to combobox4
            foreach (var t in Directory.GetFiles(customDirectory, "*.txt").Select(Path.GetFileNameWithoutExtension))
            {
                customNameList.Add(t);
                comboBox4.Items.Add(t);
            }
            // Add each default payload to combobox4
            comboBox4.Items.Add(payloadHeader3);
            foreach (var s in Directory.GetFiles(defaultDirectory, "*.txt").Select(Path.GetFileNameWithoutExtension))
            {
                defaultNameList.Add(s);
                comboBox4.Items.Add(s);
            }
            // Lists to Arrays
            customScriptsNameArray = customNameList.ToArray();
            defaultScriptsNameArray = defaultNameList.ToArray();
            // Set default selected item
            comboBox4.SelectedIndex = 0;
        }

        private void CheckPayloadCount()
        {
            bool internetConnection = NetworkInterface.GetIsNetworkAvailable();

            if (internetConnection)
            {
                var source = _client.DownloadString(payloadsURL);
                DirectoryInfo dir = new DirectoryInfo(duckyDirectory + "\\Scripts\\Default\\");

                // Break Payloads Page into list
                string tempPage = GetBetween(source, "<ul>", "</ul>");
                // Count number of items in list
                int actualScriptCount = Regex.Matches(tempPage, "<li>").Cast<Match>().Count();

                int downloadedScriptCount = dir.GetFiles().Length;

                // Debug Actual & Download Count
                //MessageBox.Show(actualScriptCount + " " + downloadedScriptCount);

                if (actualScriptCount != downloadedScriptCount)
                    SetPayloads();
            }
            else
                MessageBox.Show(
                    "ERROR: No Internet Connection!\nPlease connect to the Internet to download Rubber Ducky Payloads.");

        }

        private void SetPayloads()
        {
            // Thanks to Mr.Trvp for helping with this method.
            //Alert User
            MessageBox.Show("Downloading default Ducky Scripts from github.com/hak5darren/USB-Rubber-Ducky.\n" +
                            "The program will load once they are finished downloading.");
            //Load Wiki-Payload Page
            var source = _client.DownloadString(payloadsURL);
            //var payloadsCodes = new List<Payload>();
            //var payloadsName = new List<string>();

            // Each link on Wiki-Payload Page
            foreach (var payload in PayloadParser.Parse(source))
            {
                var tempPayload = payload;
                // Only save /wiki/Payload--- links
                if (!payload.Link.Contains("hak5darren/USB-Rubber-Ducky/wiki/Payload---"))
                    continue;
                // Clean up payload name
                var santizedName = "";
                if (santizedName.Contains("Payload - ")) // Remove Payload - from title
                    santizedName = santizedName.Replace("Payload - ", "");
                santizedName = payload.Name.Replace("/", " ").Replace("-", " ");
                santizedName = santizedName.SanitizeForFile().Replace("Payload   ", "");
                if (santizedName.Contains("&#39;")) // Fix ' in title
                    santizedName = santizedName.Replace("&#39;", "'");
                santizedName = Regex.Replace(santizedName, @"(^\w)|(\s\w)", m => m.Value.ToUpper()); // Capitalize each word in title
                if (santizedName.StartsWith(" ")) // Remove first char if string starts with a space
                    santizedName = santizedName.Remove(0, 1);

                // Assign path
                try
                {
                    var path = Path.Combine(duckyDirectory + "\\Scripts\\Default\\", santizedName + ".txt");
                    if (File.Exists(path))
                        tempPayload.Code = File.ReadAllText(path);
                    else
                    {
                        tempPayload.Code = GetCodeFromPayload(payload);
                        File.WriteAllText(path, tempPayload.Code);
                    }
                }
                catch (Exception error)
                {
                    MessageBox.Show("One of the downloaded files was not able to save." +
                                    "Check if an Antivirus software/ Firewall prevented this and try again.");
                }
            }
        }

        public string GetCodeFromPayload(Payload payload)
        {
            var source = _client.DownloadString(payload.Link);
            if (source.Contains("<code>"))
                return GetBetween(source, "<code>", "</code>");

            if (source.Contains("<pre>"))
                return GetBetween(source, "<pre>", "</pre>");

            return string.Empty;
        }

        private static string GetBetween(string source, string begin, string end)
        {
            var start = source.IndexOf(begin, StringComparison.Ordinal) + begin.Length;
            var last = source.IndexOf(end, start, StringComparison.Ordinal) - start;

            return source.Substring(start, last);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            // Load Selected Payload Button
            bool scriptFound = false;

            if (comboBox4.SelectedItem == payloadHeader1)
                PromptInvalidSelection(payloadHeader1);  
            else if (comboBox4.SelectedItem == payloadHeader2)
                PromptInvalidSelection(payloadHeader2);
            else if (comboBox4.SelectedItem == payloadHeader3)
                PromptInvalidSelection(payloadHeader3);
            else
            {
                // Check if selected script is in custom folder
                foreach (string t in customScriptsNameArray)
                {
                    if (comboBox4.SelectedItem == t)
                    {
                        string scriptPath = duckyDirectory + "\\Scripts\\Custom\\" + t + ".txt";
                        textBox1.Text = File.ReadAllText(scriptPath);
                        scriptFound = true;
                        break;
                    }
                }
                // Check if selected script is in default folder
                if (!scriptFound)
                {
                    foreach (var s in defaultScriptsNameArray)
                    {
                        if (comboBox4.SelectedItem == s)
                        {
                            string scriptPath = duckyDirectory + "\\Scripts\\Default\\" + s + ".txt";
                            textBox1.Text = File.ReadAllText(scriptPath);
                        }
                    }
                }
            }
        }

        private static void PromptInvalidSelection(string item)
        {
            MessageBox.Show(
                    "ERROR! Invalid option selected!\n" + item + " is a seperator, not a payload.\n" +
                    "Please select a valid payload from the Payloads dropdown.");
        }

        private void comboBox4_MouseHover(object sender, EventArgs e)
        {
            toolTip1.SetToolTip(comboBox4, "Selected Payload:\n" + comboBox4.SelectedItem);
        }

        private void button6_Click(object sender, EventArgs e)
        {
            // Eject selected MicroSD
            selectedRemovableDrive = selectedRemovableDrive.Trim('\\');
            selectedRemovableDrive = selectedRemovableDrive.Trim(':');
            char drive = Char.Parse(selectedRemovableDrive);

            try
            {
                EjectDrive.EjectDriveMethod(drive);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

    }
}
