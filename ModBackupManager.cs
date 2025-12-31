using System;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;

namespace CrossworldsModManager
{
    public static class ModBackupManager
    {
        public static void BackupMods(string sourceModDirectory)
        {
            ProgressForm progressForm = new ProgressForm("Backing up Mods");
            
            progressForm.Shown += (sender, e) => 
            {
                Task.Run(() => PerformBackup(sourceModDirectory, progressForm));
            };

            progressForm.ShowDialog();
        }

        private static void PerformBackup(string sourceModDirectory, ProgressForm form)
        {
            try
            {
                string appDir = AppDomain.CurrentDomain.BaseDirectory;
                string backupDir = Path.Combine(appDir, "ModsTemp");

                if (!Directory.Exists(backupDir))
                {
                    Directory.CreateDirectory(backupDir);
                }

                if (!Directory.Exists(sourceModDirectory))
                {
                    form.UpdateStatus("Mod directory not found!");
                    form.ShowCompletion("Backup failed.");
                    return;
                }

                string[] files = Directory.GetFiles(sourceModDirectory, "*.*", SearchOption.AllDirectories);
                int totalFiles = files.Length;
                int processed = 0;

                var token = form.TokenSource?.Token ?? CancellationToken.None;

                foreach (string file in files)
                {
                    if (token.IsCancellationRequested)
                    {
                        form.ShowCompletion("Backup skipped.");
                        return;
                    }
                    string relativePath = Path.GetRelativePath(sourceModDirectory, file);
                    
                    // Safety check: if drives differ, GetRelativePath returns absolute path.
                    // In that case, we fallback to filename to avoid writing outside ModsTemp.
                    if (Path.IsPathRooted(relativePath))
                    {
                        relativePath = Path.GetFileName(file);
                    }

                    string destFile = Path.Combine(backupDir, relativePath);
                    string? destDir = Path.GetDirectoryName(destFile);

                    if (destDir != null && !Directory.Exists(destDir))
                    {
                        Directory.CreateDirectory(destDir);
                    }

                    // Copy and overwrite existing files to update backup.
                    // We do not delete anything from ModsTemp, ensuring safety.
                    File.Copy(file, destFile, true);

                    if (token.IsCancellationRequested)
                    {
                        form.ShowCompletion("Backup skipped.");
                        return;
                    }

                    processed++;
                    int percentage = (int)((processed / (float)totalFiles) * 100);
                    
                    form.UpdateStatus($"Backing up: {Path.GetFileName(file)}");
                    form.UpdateProgress(percentage);
                }

                form.ShowCompletion("Backup completed successfully!");
            }
            catch (Exception ex)
            {
                form.ShowCompletion($"Error: {ex.Message}");
            }
        }

        public static void RestoreModsFromBackup(string destinationModDirectory)
        {
            ProgressForm progressForm = new ProgressForm("Restoring Mods");
            
            progressForm.Shown += (sender, e) => 
            {
                Task.Run(() => PerformRestore(destinationModDirectory, progressForm));
            };

            progressForm.ShowDialog();
        }

        private static void PerformRestore(string destinationModDirectory, ProgressForm form)
        {
            try
            {
                string appDir = AppDomain.CurrentDomain.BaseDirectory;
                string backupDir = Path.Combine(appDir, "ModsTemp");

                if (!Directory.Exists(backupDir))
                {
                    form.UpdateStatus("Backup directory not found!");
                    form.ShowCompletion("Restore failed: No backup exists.");
                    return;
                }

                if (!Directory.Exists(destinationModDirectory))
                {
                    Directory.CreateDirectory(destinationModDirectory);
                }

                string[] files = Directory.GetFiles(backupDir, "*.*", SearchOption.AllDirectories);
                int totalFiles = files.Length;
                int processed = 0;

                foreach (string file in files)
                {
                    string relativePath = Path.GetRelativePath(backupDir, file);
                    
                    if (Path.IsPathRooted(relativePath))
                    {
                        relativePath = Path.GetFileName(file);
                    }

                    string destFile = Path.Combine(destinationModDirectory, relativePath);
                    string? destDir = Path.GetDirectoryName(destFile);

                    if (destDir != null && !Directory.Exists(destDir))
                    {
                        Directory.CreateDirectory(destDir);
                    }

                    File.Copy(file, destFile, true);

                    processed++;
                    int percentage = (int)((processed / (float)totalFiles) * 100);
                    
                    form.UpdateStatus($"Restoring: {Path.GetFileName(file)}");
                    form.UpdateProgress(percentage);
                }

                form.ShowCompletion("Restore completed successfully!");
            }
            catch (Exception ex)
            {
                form.ShowCompletion($"Error: {ex.Message}");
            }
        }
    }
}