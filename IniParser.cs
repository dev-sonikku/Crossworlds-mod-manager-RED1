using System.Collections.Generic;
using System.IO;

namespace CrossworldsModManager
{
    public static class IniParser
    {
        public static Dictionary<string, Dictionary<string, string>> Parse(string filePath)
        {
            var data = new Dictionary<string, Dictionary<string, string>>();
            if (!File.Exists(filePath)) return data;

            string currentSection = "";

            foreach (var line in File.ReadAllLines(filePath))
            {
                var trimmedLine = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith(";") || trimmedLine.StartsWith("#"))
                    continue;

                if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
                {
                    currentSection = trimmedLine.Substring(1, trimmedLine.Length - 2);
                    if (!data.ContainsKey(currentSection))
                    {
                        data[currentSection] = new Dictionary<string, string>();
                    }
                }
                else if (!string.IsNullOrEmpty(currentSection) && trimmedLine.Contains("="))
                {
                    var parts = trimmedLine.Split(new[] { '=' }, 2);
                    if (parts.Length == 2)
                    {
                        var key = parts[0].Trim();
                        var value = parts[1].Trim().Trim('"'); // Trim quotes
                        data[currentSection][key] = value;
                    }
                }
            }
            return data;
        }
    }
}