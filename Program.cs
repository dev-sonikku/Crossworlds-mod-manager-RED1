using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CrossworldsModManager
{
    static class Program
    {
        /// <summary>
        /// Allocates a new console for the calling process.
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            AllocConsole(); // Create a console window
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}