// Copyright (c) 2025 Phil Pendlebury
// Everything Creative
// Licensed under MIT

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using Microsoft.VisualBasic.FileIO; // Added for Recycle Bin support

namespace CNRemoveRecents
{
    public partial class Form1 : Form
    {
        private readonly string iniPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.ini");
        private const string AppVersion = "v2.2.7";

        // INI settings
        private string lastFolder = null;
        private string backupLocation = null;

        // Helper: Load INI settings
        private void LoadIniSettings()
        {
            lastFolder = null;
            backupLocation = null;
            if (!File.Exists(iniPath)) return;
            string[] lines = File.ReadAllLines(iniPath);
            bool inSettings = false;
            foreach (var line in lines)
            {
                string trimmed = line.Trim();
                if (trimmed.StartsWith("[Settings]", StringComparison.OrdinalIgnoreCase))
                {
                    inSettings = true;
                    continue;
                }
                if (!inSettings || string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#")) continue;
                int eq = trimmed.IndexOf('=');
                if (eq < 0) continue;
                string key = trimmed.Substring(0, eq).Trim();
                string val = trimmed.Substring(eq + 1).Trim();
                if (key.Equals("Last Folder", StringComparison.OrdinalIgnoreCase))
                    lastFolder = val;
                else if (key.Equals("Backup Location", StringComparison.OrdinalIgnoreCase))
                    backupLocation = val;
            }
        }

        // Helper: Save INI settings
        private void SaveIniSettings()
        {
            var lines = new List<string>
            {
                "[Settings]",
                $"Last Folder={lastFolder ?? ""}",
                $"Backup Location={backupLocation ?? ""}"
            };
            Directory.CreateDirectory(Path.GetDirectoryName(iniPath));
            File.WriteAllLines(iniPath, lines);
        }

        private void SaveLastSelected(string value)
        {
            LoadIniSettings();
            lastFolder = value;
            SaveIniSettings();
        }

        private string LoadLastSelected()
        {
            LoadIniSettings();
            return lastFolder;
        }

        private string GetBackupLocation()
        {
            LoadIniSettings();
            string loc = backupLocation;
            if (string.IsNullOrWhiteSpace(loc))
            {
                // Default: Desktop\CNRRBackups
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                loc = Path.Combine(desktop, "CNRRBackups");
            }
            if (loc.Equals("NONE", StringComparison.OrdinalIgnoreCase))
                return null;
            return loc;
        }

        private void AddRow(string name, string folderPath)
        {
            int rowIndex = dataGridView1.Rows.Add();
            var row = dataGridView1.Rows[rowIndex];
            row.Cells["nameColumn"].Value = name;
            // Check if the file exists in the folder
            string filePath = Path.Combine(folderPath, name);
            row.Cells["statusColumn"].Value = File.Exists(filePath) ? "✓" : "X";
            row.Cells["pathColumn"].Value = folderPath; // Show folder path
            row.Cells["pathColumn"].Tag = filePath;   // Store full file path for later use
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            string steinbergPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Steinberg");
            if (Directory.Exists(steinbergPath))
            {
                string[] dirs = Directory.GetDirectories(steinbergPath);
                foreach (string dir in dirs)
                {
                    string folderName = Path.GetFileName(dir);
                    string defaultsPath = Path.Combine(dir, "Defaults.xml");
                    if (File.Exists(defaultsPath) && (folderName.StartsWith("Cubase") || folderName.StartsWith("Nuendo")))
                    {
                        comboBox1.Items.Add(folderName);
                    }
                }
            }

            // Load last selected item from ini
            string lastSelected = LoadLastSelected();
            if (!string.IsNullOrEmpty(lastSelected) && comboBox1.Items.Contains(lastSelected))
            {
                comboBox1.SelectedItem = lastSelected;
            }

            dataGridView1.RowHeadersVisible = false;
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox1.SelectedItem != null)
            {
                SaveLastSelected(comboBox1.SelectedItem.ToString());
                string selectedFolder = comboBox1.SelectedItem.ToString();
                string steinbergPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Steinberg");
                string defaultsPath = Path.Combine(steinbergPath, selectedFolder, "Defaults.xml");

                if (File.Exists(defaultsPath))
                {
                    try
                    {
                        string xmlText = File.ReadAllText(defaultsPath);
                        int startIdx, endIdx;
                        string pathsListSection = ExtractPathsListSection(xmlText, out startIdx, out endIdx);
                        if (pathsListSection == null)
                        {
                            MessageBox.Show("Error!\nNo <list name=\"Paths\"> section found in this file", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            dataGridView1.Rows.Clear();
                            return;
                        }
                        XElement pathsList;
                        try
                        {
                            pathsList = XElement.Parse(pathsListSection);
                        }
                        catch (Exception)
                        {
                            // Try to wrap in a dummy root if parsing fails
                            pathsList = XElement.Parse("<root>" + pathsListSection + "</root>").Element("list");
                        }
                        dataGridView1.Rows.Clear();
                        // Gather all items first
                        var items = new List<(string name, string path, string folder, string filePath)>();
                        foreach (var item in pathsList.Elements("item"))
                        {
                            var nameElement = item.Elements("string").FirstOrDefault(x => (string)x.Attribute("name") == "Name");
                            var pathElement = item.Elements("string").FirstOrDefault(x => (string)x.Attribute("name") == "Path");
                            if (nameElement != null && pathElement != null)
                            {
                                string name = (string)nameElement.Attribute("value");
                                string path = (string)pathElement.Attribute("value");
                                string filePath = Path.Combine(path, name);
                                items.Add((name, path, path, filePath));
                            }
                        }
                        // Group by folder, find latest per folder
                        var latestPerFolder = new Dictionary<string, int>(); // folder -> index in items
                        var folderGroups = items.Select((item, idx) => new { item, idx })
                            .GroupBy(x => x.item.folder);
                        foreach (var group in folderGroups)
                        {
                            int? latestIdx = null;
                            DateTime latestTime = DateTime.MinValue;
                            foreach (var x in group)
                            {
                                string filePath = x.item.filePath;
                                if (File.Exists(filePath))
                                {
                                    DateTime writeTime = File.GetLastWriteTime(filePath);
                                    if (latestIdx == null || writeTime > latestTime)
                                    {
                                        latestTime = writeTime;
                                        latestIdx = x.idx;
                                    }
                                }
                            }
                            if (latestIdx != null)
                            {
                                latestPerFolder[group.Key] = latestIdx.Value;
                            }
                        }
                        // Add rows, set '*' in latestColumn if latest in folder and file exists
                        for (int i = 0; i < items.Count; i++)
                        {
                            var (name, path, folder, filePath) = items[i];
                            int rowIndex = dataGridView1.Rows.Add();
                            var row = dataGridView1.Rows[rowIndex];
                            bool fileExists = File.Exists(filePath);
                            row.Cells["nameColumn"].Value = name;
                            row.Cells["statusColumn"].Value = fileExists ? "✓" : "X";
                            row.Cells["pathColumn"].Value = path;
                            row.Cells["pathColumn"].Tag = filePath;
                            row.Cells["latestColumn"].Value = (fileExists && latestPerFolder.ContainsKey(folder) && latestPerFolder[folder] == i) ? "*" : string.Empty;
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error parsing <list name=\"Paths\"> in '{defaultsPath}':\n{ex.Message}\n\nThe file may be corrupt or contain invalid XML in the Paths section.", "XML Parse Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        dataGridView1.Rows.Clear();
                    }
                }
            }
        }

        private void BackupDefaultsXml()
        {
            if (comboBox1.SelectedItem == null) return;
            string selectedFolder = comboBox1.SelectedItem.ToString();
            string steinbergPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Steinberg");
            string defaultsPath = Path.Combine(steinbergPath, selectedFolder, "Defaults.xml");
            if (!File.Exists(defaultsPath)) return;
            string backupDir = GetBackupLocation();
            if (string.IsNullOrWhiteSpace(backupDir)) return; // No backup if NONE

            if (backupDir.Equals("RECYCLEBIN", StringComparison.OrdinalIgnoreCase))
            {
                // Create backup in temp, then send to Recycle Bin
                string tempDir = Path.GetTempPath();
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd.HH-mm-ss");
                string backupFileName = $"Defaults.xml.{timestamp}.{selectedFolder}";
                string tempBackupPath = Path.Combine(tempDir, backupFileName);
                File.Copy(defaultsPath, tempBackupPath, true);
                try
                {
                    FileSystem.DeleteFile(tempBackupPath, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Could not send backup to Recycle Bin:\n{ex.Message}", "Backup Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                Directory.CreateDirectory(backupDir);
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd.HH-mm-ss");
                string backupFileName = $"Defaults.xml.{timestamp}.{selectedFolder}";
                string backupPath = Path.Combine(backupDir, backupFileName);
                File.Copy(defaultsPath, backupPath, true);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            BackupDefaultsXml();
            if (comboBox1.SelectedItem == null) return;
            string selectedFolder = comboBox1.SelectedItem.ToString();
            string steinbergPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Steinberg");
            string defaultsPath = Path.Combine(steinbergPath, selectedFolder, "Defaults.xml");
            if (!File.Exists(defaultsPath)) return;

            try
            {
                string xmlText = File.ReadAllText(defaultsPath);
                int startIdx, endIdx;
                string pathsListSection = ExtractPathsListSection(xmlText, out startIdx, out endIdx);
                if (pathsListSection == null)
                {
                    MessageBox.Show("Error!\nNo <list name=\"Paths\"> section found in this file", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                // Sanitize ampersands before parsing
                string sanitizedSection = SanitizeAmpersandsInAttributes(pathsListSection);
                XElement pathsList;
                try
                {
                    pathsList = XElement.Parse(sanitizedSection);
                }
                catch (Exception)
                {
                    pathsList = XElement.Parse("<root>" + sanitizedSection + "</root>").Element("list");
                }
                // Remove relevant <item> elements (missing files)
                var toRemove = new HashSet<(string name, string path)>();
                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    if (row.IsNewRow) continue;
                    var status = row.Cells["statusColumn"].Value?.ToString();
                    if (status == "X")
                    {
                        var name = row.Cells["nameColumn"].Value?.ToString();
                        var path = row.Cells["pathColumn"].Value?.ToString();
                        if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(path))
                            toRemove.Add((name, path));
                    }
                }
                var items = pathsList.Elements("item").ToList();
                foreach (var item in items)
                {
                    var nameElement = item.Elements("string").FirstOrDefault(x => (string)x.Attribute("name") == "Name");
                    var pathElement = item.Elements("string").FirstOrDefault(x => (string)x.Attribute("name") == "Path");
                    string name = nameElement != null ? (string)nameElement.Attribute("value") : null;
                    string path = pathElement != null ? (string)pathElement.Attribute("value") : null;
                    if (name != null && path != null && toRemove.Contains((name, path)))
                    {
                        item.Remove();
                    }
                }
                // Serialize edited section
                string newSection = pathsList.ToString();
                // Replace section in original text
                string newXmlText = ReplacePathsListSection(xmlText, newSection, startIdx, endIdx);
                File.WriteAllText(defaultsPath, newXmlText);
                comboBox1_SelectedIndexChanged(null, null);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error editing Defaults.xml: " + ex.Message);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            BackupDefaultsXml();
            if (comboBox1.SelectedItem == null) return;
            string selectedFolder = comboBox1.SelectedItem.ToString();
            string steinbergPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Steinberg");
            string defaultsPath = Path.Combine(steinbergPath, selectedFolder, "Defaults.xml");
            if (!File.Exists(defaultsPath)) return;

            try
            {
                string xmlText = File.ReadAllText(defaultsPath);
                int startIdx, endIdx;
                string pathsListSection = ExtractPathsListSection(xmlText, out startIdx, out endIdx);
                if (pathsListSection == null)
                {
                    MessageBox.Show("Error!\nNo <list name=\"Paths\"> section found in this file", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                // Sanitize ampersands before parsing
                string sanitizedSection = SanitizeAmpersandsInAttributes(pathsListSection);
                XElement pathsList;
                try
                {
                    pathsList = XElement.Parse(sanitizedSection);
                }
                catch (Exception)
                {
                    pathsList = XElement.Parse("<root>" + sanitizedSection + "</root>").Element("list");
                }
                // Remove relevant <item> elements (selected rows)
                var toRemove = new HashSet<(string name, string path)>();
                foreach (DataGridViewRow row in dataGridView1.SelectedRows)
                {
                    if (row.IsNewRow) continue;
                    var name = row.Cells["nameColumn"].Value?.ToString();
                    var path = row.Cells["pathColumn"].Value?.ToString();
                    if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(path))
                        toRemove.Add((name, path));
                }
                var items = pathsList.Elements("item").ToList();
                foreach (var item in items)
                {
                    var nameElement = item.Elements("string").FirstOrDefault(x => (string)x.Attribute("name") == "Name");
                    var pathElement = item.Elements("string").FirstOrDefault(x => (string)x.Attribute("name") == "Path");
                    string name = nameElement != null ? (string)nameElement.Attribute("value") : null;
                    string path = pathElement != null ? (string)pathElement.Attribute("value") : null;
                    if (name != null && path != null && toRemove.Contains((name, path)))
                    {
                        item.Remove();
                    }
                }
                // Serialize edited section
                string newSection = pathsList.ToString();
                // Replace section in original text
                string newXmlText = ReplacePathsListSection(xmlText, newSection, startIdx, endIdx);
                File.WriteAllText(defaultsPath, newXmlText);
                comboBox1_SelectedIndexChanged(null, null);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error editing Defaults.xml: " + ex.Message);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            string instructions =
                $"Cubase Nuendo Remove Recents {AppVersion} by Phil Pendlebury\r\n" +
                "\r\n" +
                "Select your application from the dropdown list\r\n" +
                "The grid will then be populated with all projects that are in the recent projects area\r\n" +
                "\r\n" +
                "Projects that are in recent projects but no longer exist will be marked by an X and highlighted in red\r\n" +
                "\r\n" +
                "Projects that are in recent projects and do exist will be marked by a tick ✓ in the status column\r\n" +
                "\r\n" +
                "The latest project that exists in each folder is marked in the right hand column with an asterisk\r\n" +
                "\r\n" +
                "To remove all project references that no longer exist, click the Remove Missing button\r\n" +
                "The Defaults.xml will be first backed up (see Backup Location Options below) and then the file will be processed\r\n" +
                "\r\n" +
                "To remove only selected references, first select as many items as you like from the list, then click the Remove Selected button\r\n" +
                "The Defaults.xml will be first backed up (see Backup Location Options below) and then the file will be processed\r\n" +
                "\r\n" +
                "You can sort the grid by any of the columns, this will not affect order when the Defaults.xml file is processed\r\n" +
                "\r\n" +
                "To edit the ini file which contains the backup location you can click on the << Select Application label " +
                "\r\n\r\n" +
                "Backup Location Options:\r\n" +
                "Empty (Default): Backups go to Desktop\\CNRRBackups\r\n" +
                "Path: Backups go to that folder (e.g., D:\\MyBackups)\r\n" +
                "NONE: No backup is made\r\n" +
                "RECYCLEBIN: Backup is sent directly to the Recycle Bin\r\n";

            MessageBox.Show(this, instructions, "Instructions", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void label2_Click(object sender, EventArgs e)
        {
            try
            {
                if (!File.Exists(iniPath))
                {
                    // Create a default INI file if it doesn't exist
                    SaveIniSettings();
                }
                Process.Start(new ProcessStartInfo(iniPath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not open INI file:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public Form1()
        {
            InitializeComponent();
            // Set the form icon
            string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cnrr.ico");
            if (File.Exists(iconPath))
            {
                this.Icon = new Icon(iconPath);
            }
            button1.Click += button1_Click;
            button2.Click += button2_Click;
            button3.Click += button3_Click;
            // Enable multi-row selection
            dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView1.MultiSelect = true;
            // Prevent row height adjustment
            dataGridView1.AllowUserToResizeRows = false;
            // Set selection color to slightly darker green
            dataGridView1.DefaultCellStyle.SelectionBackColor = Color.PaleGreen;
            dataGridView1.DefaultCellStyle.SelectionForeColor = Color.Black;
            // Highlight rows with "X" in statusColumn
            dataGridView1.RowPrePaint += dataGridView1_RowPrePaint;
            dataGridView1.CellToolTipTextNeeded += dataGridView1_CellToolTipTextNeeded;
            dataGridView1.ShowCellToolTips = true; // Ensure tooltips are enabled
            button1.BackColor = Color.MistyRose;    // Light red
            button1.ForeColor = Color.Black;
            button2.BackColor = Color.PaleGreen;    // Slightly darker green
            button2.ForeColor = Color.Black;
            // Prevent form resize
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            // Set label1 text with AppVersion
            label1.Text = $"Remove Cubase && Nuendo Recent Files {AppVersion} by Phil Pendlebury ";
        }

        // Highlight rows with "X" in statusColumn as light red
        private void dataGridView1_RowPrePaint(object sender, DataGridViewRowPrePaintEventArgs e)
        {
            var row = dataGridView1.Rows[e.RowIndex];
            var statusCell = row.Cells["statusColumn"];
            if (statusCell.Value != null && statusCell.Value.ToString() == "X")
            {
                row.DefaultCellStyle.BackColor = Color.MistyRose; // Light red
            }
            else
            {
                row.DefaultCellStyle.BackColor = Color.White;
            }
        }

        // Provide tooltips for DataGridView column headers
        private void dataGridView1_CellToolTipTextNeeded(object sender, DataGridViewCellToolTipTextNeededEventArgs e)
        {
            if (e.RowIndex == -1) // Header row
            {
                switch (e.ColumnIndex)
                {
                    case 0:
                        e.ToolTipText = "Project Name";
                        break;
                    case 1:
                        e.ToolTipText = "Ticked if the project exists";
                        break;
                    case 2:
                        e.ToolTipText = "Full project path";
                        break;
                    case 3:
                        e.ToolTipText = "An asterisk (*) shows the latest project in each folder";
                        break;
                }
            }
        }

        // Helper: Extract <list name="Paths">...</list> section from XML text
        private static string ExtractPathsListSection(string xmlText, out int startIdx, out int endIdx)
        {
            startIdx = -1;
            endIdx = -1;
            string startTag = "<list name=\"Paths\"";
            int listStart = xmlText.IndexOf(startTag, StringComparison.OrdinalIgnoreCase);
            if (listStart == -1) return null;
            int openTagEnd = xmlText.IndexOf('>', listStart);
            if (openTagEnd == -1) return null;
            int closeTag = xmlText.IndexOf("</list>", openTagEnd, StringComparison.OrdinalIgnoreCase);
            if (closeTag == -1) return null;
            startIdx = listStart;
            endIdx = closeTag + "</list>".Length;
            return xmlText.Substring(listStart, endIdx - listStart);
        }

        // Helper: Replace <list name="Paths">...</list> section in XML text
        private static string ReplacePathsListSection(string xmlText, string newSection, int startIdx, int endIdx)
        {
            return xmlText.Substring(0, startIdx) + newSection + xmlText.Substring(endIdx);
        }

        // Helper: Sanitize ampersands in attributes for XML parsing
        private static string SanitizeAmpersandsInAttributes(string xmlSection)
        {
            // Replace & with &amp; in attribute values only
            return System.Text.RegularExpressions.Regex.Replace(xmlSection,
                "(&[^;]*)(?=>)",               // Look for & not followed by ; and before >
                m => m.Value.Replace("&", "&amp;")
            );
        }
    }
}
