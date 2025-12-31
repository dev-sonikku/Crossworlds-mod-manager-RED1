using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace CrossworldsModManager
{
    static class Program
    {
        // Current application version.
        public const string AppVersion = "1.0.6";

        // Unique GUID for the application to identify the mutex and messages.
        private const string AppGuid = "c1a2b3d4-e5f6-7890-1234-567890abcdef"; // Please generate a new GUID for your app
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
                        Application.Run(new MainForm(oneClickUrl, AppVersion));
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

        /// <summary>
        /// Checks GitHub for a new release and prompts the user to update if one is found.
        /// </summary>
        public static async void CheckForUpdates()
        {
            try
            {
                string owner = "Red1Fouad";
                string repo = "Crossworlds-mod-manager-RED1";
                string latestVersionTag;
                string downloadUrl = string.Empty;

                using (var client = new HttpClient())
                {
                    // GitHub API requires a User-Agent header.
                    client.DefaultRequestHeaders.Add("User-Agent", "CrossworldsModManager-Update-Check");

                    // Get the latest release information
                    var response = await client.GetStringAsync($"https://api.github.com/repos/{owner}/{repo}/releases/latest");
                    
                    // A simple JSON parser to avoid adding a full library dependency.
                    // This finds the "tag_name" and the first "browser_download_url".
                    latestVersionTag = ParseJsonValue(response, "tag_name");
                    downloadUrl = ParseJsonValue(response, "browser_download_url");
                }

                if (string.IsNullOrEmpty(latestVersionTag) || string.IsNullOrEmpty(downloadUrl))
                {
                    Debug.WriteLine("Could not determine latest version or download URL from GitHub API response.");
                    return;
                }

                // Normalize version strings (e.g., "v1.2.3" -> "1.2.3")
                var latestVersion = new Version(latestVersionTag.TrimStart('v'));
                var currentVersion = new Version(AppVersion);

                if (latestVersion > currentVersion)
                {
                    var result = MessageBox.Show(
                        $"A new version ({latestVersionTag}) is available!\nWould you like to update now?",
                        "Update Available",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Information);

                    if (result == DialogResult.Yes)
                    {
                        // Launch the external updater
                        string updaterPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "updater.exe");
                        if (File.Exists(updaterPath))
                        {
                            var currentProcess = Process.GetCurrentProcess();
                            string? appPath = currentProcess.MainModule?.FileName;

                            if (string.IsNullOrEmpty(appPath)) {
                                MessageBox.Show("Could not determine the application path. Update cannot proceed.", "Update Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return;
                            }

                            string arguments = $"--pid {currentProcess.Id} --appPath \"{appPath}\" --downloadUrl \"{downloadUrl}\"";
                            Process.Start(updaterPath, arguments);
                            Application.Exit();
                        }
                        else
                        {
                            MessageBox.Show($"Updater executable not found at:\n{updaterPath}", "Update Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error checking for updates: {ex.Message}");
                // Silently fail, as this is not a critical function.
            }
        }

        // A very basic helper to extract a value from a JSON string.
        private static string ParseJsonValue(string json, string key) =>
            System.Text.RegularExpressions.Regex.Match(json, $"\"{key}\"\\s*:\\s*\"(.*?)\"").Groups[1].Value;

        /// <summary>
        /// Checks for a mod.ini in all subdirectories of the given mod path.
        /// If found, it makes that directory the root of the mod.
        /// </summary>
        /// <param name="modPath">The current root directory of the mod.</param>
        public static void CheckAndSetModRoot(string modPath)
        {
            try
            {
                if (!Directory.Exists(modPath)) return;

                // Search for mod.ini in subdirectories
                var iniFiles = Directory.GetFiles(modPath, "mod.ini", SearchOption.AllDirectories);

                if (iniFiles.Length > 0)
                {
                    string? foundIniPath = null;
                    string fullModPath = Path.GetFullPath(modPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                    // Find the first mod.ini that is NOT in the root directory
                    foreach (var file in iniFiles)
                    {
                        var dir = Path.GetDirectoryName(file);
                        if (dir == null) continue;
                        var fullDir = Path.GetFullPath(dir).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                        if (!fullDir.Equals(fullModPath, StringComparison.OrdinalIgnoreCase))
                        {
                            foundIniPath = file;
                            break;
                        }
                    }

                    if (foundIniPath == null) return;

                    string? newRoot = Path.GetDirectoryName(foundIniPath);

                    // If the found path is somehow the same as modPath, return
                    if (string.IsNullOrEmpty(newRoot) || newRoot.Equals(modPath, StringComparison.OrdinalIgnoreCase)) return;

                    Debug.WriteLine($"Found mod.ini in subdirectory: {newRoot}. Promoting to root.");
                    
                    // Safety check: Ensure newRoot is actually inside modPath
                    if (!Path.GetFullPath(newRoot).StartsWith(fullModPath, StringComparison.OrdinalIgnoreCase))
                    {
                        Debug.WriteLine($"Safety check failed: {newRoot} is not inside {modPath}");
                        return;
                    }

                    // Move the new root to a temporary location outside the mods directory
                    // to avoid performing destructive operations inside the user's mods folder.
                    string tempRoot = Path.Combine(Path.GetTempPath(), "CrossworldsModManager", Guid.NewGuid().ToString());
                    try
                    {
                        Directory.CreateDirectory(tempRoot);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Failed to create temp root '{tempRoot}': {ex.Message}");
                        return;
                    }

                    string tempPath = Path.Combine(tempRoot, Path.GetFileName(newRoot) ?? ("mod_temp_" + Guid.NewGuid().ToString()));

                    // Try to move the new root into the temp area; if that fails (cross-volume),
                    // fall back to copying then deleting the source.
                    try
                    {
                        SafeMoveDirectory(newRoot, tempPath);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Failed to move or copy new root to temp: {ex.Message}");
                        Log($"Failed to move or copy new root '{newRoot}' to temp '{tempPath}': {ex.Message}");
                        return; // Abort
                    }

                    // Instead of moving the original folder to a sibling trash inside the mods folder,
                    // move it to the temp root outside the mods directory. This prevents accidental
                    // deletion of unrelated folders inside the mods directory.
                    string trashPath = Path.Combine(tempRoot, Path.GetFileName(modPath) + "_trash_" + Guid.NewGuid().ToString());
                    bool trashCreated = false;

                    try
                    {
                        SafeMoveDirectory(modPath, trashPath);
                        trashCreated = true;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Failed to move original folder to trash: {ex.Message}. Reverting.");
                        Log($"Failed to move or copy original '{modPath}' to trash '{trashPath}': {ex.Message}");
                        try { SafeMoveDirectory(tempPath, newRoot); } catch { }
                        return;
                    }

                    // Rename the temp folder to the original mod path name
                    try
                    {
                        SafeMoveDirectory(tempPath, modPath);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Failed to rename temp folder to mod path: {ex.Message}. Reverting.");
                        Log($"Failed to move or copy temp '{tempPath}' to mod path '{modPath}': {ex.Message}");
                        // Restore from trash
                        if (trashCreated && Directory.Exists(trashPath))
                        {
                            try { SafeMoveDirectory(trashPath, modPath); SafeMoveDirectory(tempPath, newRoot); } catch { }
                        }
                        return;
                    }

                    // Success! Do NOT automatically delete the trash folder. Retain it in the system temp
                    // for manual inspection/cleanup to avoid accidental mass deletion.
                    Debug.WriteLine($"Mod root normalized. Original folder moved to: {trashPath}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error normalizing mod directory: {ex.Message}");
            }
        }

        // Log file path for operations that may affect user data.
        private static readonly string OperationLogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mod_ops.log");

        private static void Log(string message)
        {
            try
            {
                var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}";
                File.AppendAllText(OperationLogPath, line);
                Debug.WriteLine(message);
            }
            catch
            {
                // Best effort only; don't let logging interfere with operations.
            }
        }

        // Attempts to move a directory. If a simple move fails (e.g., across volumes),
        // falls back to a recursive copy followed by deletion of the source.
        private static void SafeMoveDirectory(string sourceDir, string destDir)
        {
            if (string.Equals(Path.GetFullPath(sourceDir).TrimEnd(Path.DirectorySeparatorChar), Path.GetFullPath(destDir).TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase))
                return; // same path

            try
            {
                // Try a standard move first (fast, preserves metadata when possible).
                Directory.Move(sourceDir, destDir);
                Log($"Moved directory '{sourceDir}' -> '{destDir}'");
                return;
            }
            catch (IOException)
            {
                // Fall through to copy fallback.
            }
            catch (UnauthorizedAccessException)
            {
                // Fall through to copy fallback in case of permission oddities.
            }

            // Copy fallback
            CopyDirectory(sourceDir, destDir);

            // Verify destination exists, then remove source.
            if (Directory.Exists(destDir))
            {
                try
                {
                    Directory.Delete(sourceDir, true);
                    Log($"Copied directory '{sourceDir}' -> '{destDir}' and deleted source.");
                }
                catch (Exception ex)
                {
                    Log($"Copied directory '{sourceDir}' -> '{destDir}', but failed to delete source: {ex.Message}");
                    throw;
                }
            }
            else
            {
                throw new IOException($"Destination directory '{destDir}' does not exist after copy fallback.");
            }
        }

        private static void CopyDirectory(string sourceDir, string destDir)
        {
            var sourceInfo = new DirectoryInfo(sourceDir);
            if (!sourceInfo.Exists) throw new DirectoryNotFoundException($"Source directory not found: {sourceDir}");

            Directory.CreateDirectory(destDir);

            // Copy files
            foreach (var file in sourceInfo.GetFiles("*", SearchOption.TopDirectoryOnly))
            {
                var destFile = Path.Combine(destDir, file.Name);
                file.CopyTo(destFile, true);
            }

            // Recursively copy subdirectories
            foreach (var dir in sourceInfo.GetDirectories())
            {
                var destSubDir = Path.Combine(destDir, dir.Name);
                CopyDirectory(dir.FullName, destSubDir);
            }
        }
    }
}