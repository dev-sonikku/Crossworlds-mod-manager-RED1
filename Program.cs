using System;
using System.Windows.Forms;

namespace CrossworldsModManager
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                // Fallback for unhandled exceptions
                MessageBox.Show($"A fatal error occurred:\n{ex.Message}\n\n{ex.StackTrace}", 
                    "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}