namespace DirectoryViewer;

static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            // Ensure a valid directory parameter is passed.
            if (args.Length == 0 || !Directory.Exists(args[0]))
            {
                MessageBox.Show("Please provide a valid directory as a command-line argument.");
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm(args[0]));
        }
    }