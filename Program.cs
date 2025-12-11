using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace CrossworldsModManager
{
    static class Program
    {
        // Unique GUID for the application to identify the mutex and messages.
        private const string AppGuid = "c1a2b3d4-e5f6-7890-1234-567890abcdef";
        private const string ProtocolName = "bluestar";

        // P/Invoke for sending messages to the existing instance
        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, ref COPYDATASTRUCT lParam);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        // Struct for WM_COPYDATA
        [StructLayout(LayoutKind.Sequential)]
        private struct COPYDATASTRUCT
        {
            public IntPtr dwData;
            public int cbData;
            public IntPtr lpData;
        }

        private const int WM_COPYDATA = 0x004A;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            using (Mutex mutex = new Mutex(true, AppGuid, out bool createdNew))
            {
                string? oneClickUrl = args.Length > 0 && args[0].StartsWith($"{ProtocolName}:", StringComparison.OrdinalIgnoreCase)
                    ? args[0]
                    : null;

                if (createdNew)
                {
                    // This is the first instance.
                    // Always register the protocol on startup to ensure it's up-to-date.
                    RegisterProtocol();
                    try
                    {
                        Application.EnableVisualStyles();
                        Application.SetCompatibleTextRenderingDefault(false);
                        Application.Run(new MainForm(oneClickUrl));
                    }
                    catch (Exception ex)
                    {
                        // Fallback for unhandled exceptions
                        MessageBox.Show($"A fatal error occurred:\n{ex.Message}\n\n{ex.StackTrace}",
                            "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    // Another instance is already running. Send the URL to it.
                    if (oneClickUrl != null)
                    {
                        var currentProcess = Process.GetCurrentProcess();
                        var otherProcess = Process.GetProcessesByName(currentProcess.ProcessName)
                                                  .FirstOrDefault(p => p.Id != currentProcess.Id);

                        if (otherProcess != null)
                        {
                            // Send the URL via WM_COPYDATA
                            byte[] data = System.Text.Encoding.UTF8.GetBytes(oneClickUrl);
                            var cds = new COPYDATASTRUCT
                            {
                                dwData = IntPtr.Zero,
                                cbData = data.Length + 1,
                                lpData = Marshal.StringToHGlobalAnsi(oneClickUrl)
                            };
                            SendMessage(otherProcess.MainWindowHandle, WM_COPYDATA, IntPtr.Zero, ref cds);
                            SetForegroundWindow(otherProcess.MainWindowHandle); // Bring the existing window to the front
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Registers the custom URL protocol in the Windows Registry for the current user.
        /// </summary>
        private static void RegisterProtocol()
        {
            try
            {
                // Use HKEY_CURRENT_USER to avoid requiring admin privileges.
                using (var key = Registry.CurrentUser.CreateSubKey($"Software\\Classes\\{ProtocolName}"))
                {
                    if (key == null) return;

                    string? exePath = Process.GetCurrentProcess().MainModule?.FileName;
                    if (string.IsNullOrEmpty(exePath))
                    {
                        Debug.WriteLine("Could not determine executable path for protocol registration.");
                        return;
                    }

                    key.SetValue("", $"URL:{ProtocolName} Protocol");
                    key.SetValue("URL Protocol", "");

                    using (var commandKey = key.CreateSubKey(@"shell\open\command"))
                    {
                        commandKey?.SetValue("", $"\"{exePath}\" \"%1\"");
                    }
                }
            }
            catch (Exception ex)
            {
                // Log or show a non-fatal error if registration fails.
                Debug.WriteLine($"Failed to register URL protocol: {ex.Message}");
            }
        }
    }
}