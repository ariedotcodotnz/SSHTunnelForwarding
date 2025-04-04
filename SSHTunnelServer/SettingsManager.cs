using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using Microsoft.Win32;
using System.Security.Cryptography;
using System.Text;

namespace SSHTunnelServer
{
    // Settings Manager class
    public class SettingsManager
    {
        private const string SETTINGS_FILE_NAME = "settings.xml";
        private const string CONFIG_FILE_NAME = "configurations.xml";
        private const string AUTO_START_KEY = "AutoStartOnStartup";

        public SettingsManager()
        {
            // Create the application data directory if it doesn't exist
            string appDataPath = GetAppDataPath();
            if (!Directory.Exists(appDataPath))
            {
                Directory.CreateDirectory(appDataPath);
            }
        }

        public string GetAppDataPath()
        {
            string appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SSHTunnelServer");
            return appDataPath;
        }

        public List<ServerConfig> LoadConfigurations()
        {
            string configPath = Path.Combine(GetAppDataPath(), CONFIG_FILE_NAME);
            if (File.Exists(configPath))
            {
                try
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(List<ServerConfig>));
                    using (FileStream stream = new FileStream(configPath, FileMode.Open))
                    {
                        return (List<ServerConfig>)serializer.Deserialize(stream);
                    }
                }
                catch (Exception)
                {
                    // If there's an error, return an empty list
                    return new List<ServerConfig>();
                }
            }
            else
            {
                return new List<ServerConfig>();
            }
        }

        public void SaveConfigurations(List<ServerConfig> configs)
        {
            string configPath = Path.Combine(GetAppDataPath(), CONFIG_FILE_NAME);
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(List<ServerConfig>));
                using (FileStream stream = new FileStream(configPath, FileMode.Create))
                {
                    serializer.Serialize(stream, configs);
                }
            }
            catch (Exception)
            {
                // Handle serialization errors
            }
        }

        public bool GetAutoStartOnStartup()
        {
            string settingsPath = Path.Combine(GetAppDataPath(), SETTINGS_FILE_NAME);
            if (File.Exists(settingsPath))
            {
                try
                {
                    Dictionary<string, string> settings = LoadSettings();
                    if (settings.TryGetValue(AUTO_START_KEY, out string value))
                    {
                        return bool.Parse(value);
                    }
                }
                catch (Exception)
                {
                    // If there's an error, return false
                }
            }
            return false;
        }

        public void SetAutoStartOnStartup(bool autoStart)
        {
            Dictionary<string, string> settings = LoadSettings();
            settings[AUTO_START_KEY] = autoStart.ToString();
            SaveSettings(settings);
        }

        private Dictionary<string, string> LoadSettings()
        {
            string settingsPath = Path.Combine(GetAppDataPath(), SETTINGS_FILE_NAME);
            Dictionary<string, string> settings = new Dictionary<string, string>();

            if (File.Exists(settingsPath))
            {
                try
                {
                    string[] lines = File.ReadAllLines(settingsPath);
                    foreach (string line in lines)
                    {
                        int separatorIndex = line.IndexOf('=');
                        if (separatorIndex > 0)
                        {
                            string key = line.Substring(0, separatorIndex).Trim();
                            string value = line.Substring(separatorIndex + 1).Trim();
                            settings[key] = value;
                        }
                    }
                }
                catch (Exception)
                {
                    // If there's an error, return an empty dictionary
                }
            }

            return settings;
        }

        private void SaveSettings(Dictionary<string, string> settings)
        {
            string settingsPath = Path.Combine(GetAppDataPath(), SETTINGS_FILE_NAME);
            try
            {
                List<string> lines = new List<string>();
                foreach (var setting in settings)
                {
                    lines.Add($"{setting.Key}={setting.Value}");
                }
                File.WriteAllLines(settingsPath, lines);
            }
            catch (Exception)
            {
                // Handle file errors
            }
        }
    }
}
