using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Diagnostics;

namespace DirectoryViewer
{
    public class MainForm : Form
    {
        private readonly Stack<string> navigationStack = new Stack<string>();
        private FlowLayoutPanel flowPanel;
        private Button backButton;

        // SHGetFileInfo constants.
        private const uint SHGFI_ICON = 0x100;
        private const uint SHGFI_SMALLICON = 0x1;
        private const uint SHGFI_USEFILEATTRIBUTES = 0x10;
        private const uint FILE_ATTRIBUTE_DIRECTORY = 0x10;

        public MainForm(string initialDirectory)
        {
            // Enable DPI scaling.
            this.AutoScaleMode = AutoScaleMode.Dpi;
            // Modern look.
            this.BackColor = Color.White;
            this.Font = new Font("Segoe UI", 11);
            // Start full screen.
            this.WindowState = FormWindowState.Maximized;

            navigationStack.Push(initialDirectory);
            InitializeComponents();
            LoadDirectory(initialDirectory);
        }

        private void InitializeComponents()
        {
            this.Text = "Directory Viewer";

            // Create a Back button at the top.
            backButton = new Button
            {
                Text = "Back",
                Location = new Point(10, 10),
                Size = new Size(100, 40),
                Font = new Font("Segoe UI", 11),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White
            };
            backButton.FlatAppearance.BorderSize = 0;
            backButton.Click += BackButton_Click;
            this.Controls.Add(backButton);

            // Create a FlowLayoutPanel to host item buttons.
            flowPanel = new FlowLayoutPanel
            {
                Location = new Point(10, 60),
                Size = new Size(this.ClientSize.Width - 20, this.ClientSize.Height - 70),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                BackColor = Color.White
            };
            flowPanel.Resize += FlowPanel_Resize;
            this.Controls.Add(flowPanel);
        }

        private void FlowPanel_Resize(object sender, EventArgs e)
        {
            // Update the width of each button so they span the full width.
            foreach (Control ctrl in flowPanel.Controls)
            {
                ctrl.Width = flowPanel.ClientSize.Width - 20; // 20-pixel margin.
            }
        }

        private void LoadDirectory(string path)
        {
            // Set the title to show only the folder's name.
            this.Text = "Directory Viewer - " + GetDirectoryDisplayName(path);
            flowPanel.Controls.Clear();

            try
            {
                // Add subdirectory buttons.
                foreach (var dir in Directory.GetDirectories(path))
                {
                    Button btn = CreateItemButton(Path.GetFileName(dir), dir, isDirectory: true);
                    flowPanel.Controls.Add(btn);
                }

                // Add file buttons.
                foreach (var file in Directory.GetFiles(path))
                {
                    Button btn = CreateItemButton(Path.GetFileName(file), file, isDirectory: false);
                    flowPanel.Controls.Add(btn);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading directory: " + ex.Message);
            }
        }

        // Creates a button representing a file or folder.
        private Button CreateItemButton(string text, string fullPath, bool isDirectory)
        {
            Button btn = new Button();
            // Extra spaces added between icon and text.
            btn.Text = "    " + text; // Four spaces before text.
            btn.Tag = fullPath;
            btn.Font = new Font("Segoe UI", 11);
            btn.TextAlign = ContentAlignment.MiddleLeft;
            btn.ImageAlign = ContentAlignment.MiddleLeft;
            btn.Padding = new Padding(10, 0, 0, 0);
            btn.Height = 50;
            btn.FlatStyle = FlatStyle.Flat;
            // Set borders for a modern look.
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = Color.LightGray;
            btn.BackColor = Color.White;
            btn.ForeColor = Color.Black;

            // Get icon for the item.
            Icon icon = isDirectory ? GetFolderIcon() : GetIconForFile(fullPath);
            if (icon != null)
                btn.Image = icon.ToBitmap();

            btn.Click += ItemButton_Click;
            // Set the button width to span the container.
            btn.Width = flowPanel.ClientSize.Width - 20;
            return btn;
        }

        private void ItemButton_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            string path = btn.Tag as string;
            if (Directory.Exists(path))
            {
                navigationStack.Push(path);
                LoadDirectory(path);
            }
            else if (File.Exists(path))
            {
                try
                {
                    Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error opening file: " + ex.Message);
                }
            }
        }

        // Returns only the folder's name.
        private string GetDirectoryDisplayName(string path)
        {
            return new DirectoryInfo(path).Name;
        }

        private void BackButton_Click(object sender, EventArgs e)
        {
            if (navigationStack.Count > 1)
            {
                navigationStack.Pop();
                string previous = navigationStack.Peek();
                LoadDirectory(previous);
            }
        }

        // Retrieve the standard folder icon using a dummy file with the directory attribute.
        private Icon GetFolderIcon()
        {
            SHFILEINFO shinfo = new SHFILEINFO();
            SHGetFileInfo("dummy", FILE_ATTRIBUTE_DIRECTORY, ref shinfo, (uint)Marshal.SizeOf(shinfo),
                SHGFI_ICON | SHGFI_SMALLICON | SHGFI_USEFILEATTRIBUTES);
            Icon icon = (Icon)Icon.FromHandle(shinfo.hIcon).Clone();
            DestroyIcon(shinfo.hIcon);
            return icon;
        }

        // Retrieve the icon associated with a file.
        private Icon GetIconForFile(string file)
        {
            SHFILEINFO shinfo = new SHFILEINFO();
            SHGetFileInfo(file, 0, ref shinfo, (uint)Marshal.SizeOf(shinfo),
                SHGFI_ICON | SHGFI_SMALLICON);
            Icon icon = (Icon)Icon.FromHandle(shinfo.hIcon).Clone();
            DestroyIcon(shinfo.hIcon);
            return icon;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SHGetFileInfo(
            string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }
    }
}
