using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FileToolLib.Services
{
    public static class MemoryBank
    {
        private const string MemoryBankDir = "memory-bank";
        private const string SettingsFileName = "settings.txt";
        private static readonly string SettingsFilePath;

        static MemoryBank()
        {
            // Always place the memory-bank directory relative to the executing application's base directory.
            string appBaseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            SettingsFilePath = Path.Combine(appBaseDirectory, MemoryBankDir, SettingsFileName);

            // Ensure the directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsFilePath));
        }

        private static Dictionary<string, string> ReadSettings()
        {
            var settings = new Dictionary<string, string>();
            if (!File.Exists(SettingsFilePath))
            {
                return settings;
            }

            try
            {
                var lines = File.ReadAllLines(SettingsFilePath);
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue; // Skip empty lines or comments
                    var parts = line.Split(new[] { '=' }, 2);
                    if (parts.Length == 2)
                    {
                        settings[parts[0].Trim()] = parts[1].Trim();
                    }
                }
            }
            catch (Exception ex)
            {
                // Log or handle exception (e.g., file corruption)
                System.Diagnostics.Debug.WriteLine($"Error reading MemoryBank settings: {ex.Message}");
            }
            return settings;
        }

        private static void WriteSettings(Dictionary<string, string> settings)
        {
            try
            {
                var lines = settings.Select(kvp => $"{kvp.Key}={kvp.Value}").ToList();
                File.WriteAllLines(SettingsFilePath, lines);
            }
            catch (Exception ex)
            {
                // Log or handle exception
                System.Diagnostics.Debug.WriteLine($"Error writing MemoryBank settings: {ex.Message}");
            }
        }

        public static void SavePatterns(List<string> patterns)
        {
            var settings = ReadSettings();
            settings["Patterns"] = string.Join(";", patterns ?? new List<string>());
            WriteSettings(settings);
        }

        public static List<string> LoadPatterns()
        {
            var settings = ReadSettings();
            if (settings.TryGetValue("Patterns", out var value) && !string.IsNullOrEmpty(value))
            {
                return new List<string>(value.Split(';'));
            }
            return new List<string> { "*.cs" }; // Default patterns
        }

        public static void SavePathSetting(string key, string pathValue)
        {
            if (string.IsNullOrWhiteSpace(key)) return;
            var settings = ReadSettings();
            settings[key] = pathValue ?? string.Empty;
            WriteSettings(settings);
        }

        public static string LoadPathSetting(string key, string defaultValue = null)
        {
            if (string.IsNullOrWhiteSpace(key)) return defaultValue;
            var settings = ReadSettings();
            if (settings.TryGetValue(key, out var value))
            {
                return value;
            }
            return defaultValue;
        }
    }
}