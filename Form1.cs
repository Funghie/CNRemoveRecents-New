using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Xml.Linq;

namespace CNRemoveRecents
{
    public partial class Form1 : Form
    {
        private readonly string iniPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.ini");

        private void SaveLastSelected(string value)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(iniPath));
            File.WriteAllText(iniPath, value ?? "");
        }

        private string LoadLastSelected()
        {
            if (File.Exists(iniPath))
            {
                return File.ReadAllText(iniPath).Trim();
            }
            return null;
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
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd.HH-mm-ss");
            string backupFileName = $"Defaults.xml.{timestamp}.{selectedFolder}";
            string backupPath = Path.Combine(desktopPath, backupFileName);
            File.Copy(defaultsPath, backupPath, true);
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
                XDocument doc = XDocument.Load(defaultsPath);
                var autoSavers = doc.Descendants("member").FirstOrDefault(x => (string)x.Attribute("name") == "AutoSavers");
                var gRecent = autoSavers?.Elements("member").FirstOrDefault(x => (string)x.Attribute("name") == "GRecentDocumentPaths");
                var pathsList = gRecent?.Elements("list").FirstOrDefault(x => (string)x.Attribute("name") == "Paths");
                if (pathsList == null)
                {
                    MessageBox.Show("Error!\nNo <list name=\"Paths\"> section found in this file", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
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
                doc.Save(defaultsPath);
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
                XDocument doc = XDocument.Load(defaultsPath);
                var autoSavers = doc.Descendants("member").FirstOrDefault(x => (string)x.Attribute("name") == "AutoSavers");
                var gRecent = autoSavers?.Elements("member").FirstOrDefault(x => (string)x.Attribute("name") == "GRecentDocumentPaths");
                var pathsList = gRecent?.Elements("list").FirstOrDefault(x => (string)x.Attribute("name") == "Paths");
                if (pathsList == null)
                {
                    MessageBox.Show("Error!\nNo <list name=\"Paths\"> section found in this file", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
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
                doc.Save(defaultsPath);
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
                "Cubase Nuendo Remove Recents v2 by Phil Pendlebury\r\n" +
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
                "The Defaults.xml will be first backed up to your desktop and then the file will be processed\r\n" +
                "\r\n" +
                "To remove only selected references, first select as many items as you like from the list, then click the Remove Selected button\r\n" +
                "The Defaults.xml will be first backed up to your desktop and then the file will be processed\r\n" +
                "\r\n" +
                "You can sort the grid by any of the columns, this will not affect order when the Defaults.xml file is processed\r\n";

            MessageBox.Show(this, instructions, "Instructions", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

        private void label2_Click(object sender, EventArgs e)
        {

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

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {

        }
    }
}
