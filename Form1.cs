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
        private ListView listView;
        private Button backButton;
        private ImageList imageList;

        // SHGetFileInfo constants.
        private const uint SHGFI_ICON = 0x100;
        private const uint SHGFI_SMALLICON = 0x1;
        private const uint SHGFI_USEFILEATTRIBUTES = 0x10;
        private const uint FILE_ATTRIBUTE_DIRECTORY = 0x10;

        public MainForm(string initialDirectory)
        {
            // Enable DPI scaling.
            this.AutoScaleMode = AutoScaleMode.Dpi;
            
            navigationStack.Push(initialDirectory);
            InitializeComponents();
            LoadDirectory(initialDirectory);
        }

        private void InitializeComponents()
        {
            this.Text = "Directory Viewer";
            this.Width = 800;
            this.Height = 600;

            // Back button.
            backButton = new Button
            {
                Text = "Back",
                Location = new Point(10, 10)
            };
            backButton.Click += BackButton_Click;
            this.Controls.Add(backButton);

            // ListView in List view mode (similar to Explorer's list view).
            listView = new ListView
            {
                Location = new Point(10, 50),
                Width = this.ClientSize.Width - 20,
                Height = this.ClientSize.Height - 60,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                View = View.List
            };
            listView.DoubleClick += ListView_DoubleClick;
            this.Controls.Add(listView);

            // ImageList for small icons (16x16).
            imageList = new ImageList { ImageSize = new Size(16, 16) };
            listView.SmallImageList = imageList;
        }

        private void LoadDirectory(string path)
        {
            // Show only the current directory's name in the title.
            this.Text = "Directory Viewer - " + GetDirectoryDisplayName(path);
            listView.Items.Clear();
            imageList.Images.Clear();

            int imageIndex = 0;
            try
            {
                // Add subdirectories.
                foreach (var dir in Directory.GetDirectories(path))
                {
                    Icon icon = GetFolderIcon();
                    imageList.Images.Add(icon);
                    var item = new ListViewItem(Path.GetFileName(dir), imageIndex)
                    {
                        Tag = dir
                    };
                    listView.Items.Add(item);
                    imageIndex++;
                }

                // Add files.
                foreach (var file in Directory.GetFiles(path))
                {
                    Icon icon = GetIconForFile(file);
                    imageList.Images.Add(icon);
                    var item = new ListViewItem(Path.GetFileName(file), imageIndex)
                    {
                        Tag = file
                    };
                    listView.Items.Add(item);
                    imageIndex++;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading directory: " + ex.Message);
            }
        }

        // Updated helper: use DirectoryInfo to return the folder name.
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

        private void ListView_DoubleClick(object sender, EventArgs e)
        {
            if (listView.SelectedItems.Count == 0)
                return;

            string path = listView.SelectedItems[0].Tag as string;
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
