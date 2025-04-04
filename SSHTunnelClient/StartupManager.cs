using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using Microsoft.Win32;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;


namespace SSHTunnelClient
{
    // Startup Manager class
    public class StartupManager
    {
        private const string RUN_KEY = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private string _appName;

        public StartupManager(string appName)
        {
            _appName = appName;
        }

        public bool IsSetToRunAtStartup()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RUN_KEY))
            {
                return key.GetValue(_appName) != null;
            }
        }

        public void SetRunAtStartup()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RUN_KEY, true))
            {
                key.SetValue(_appName, $"\"{Application.ExecutablePath}\"");
            }
        }

        public void RemoveRunAtStartup()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RUN_KEY, true))
            {
                if (key.GetValue(_appName) != null)
                {
                    key.DeleteValue(_appName);
                }
            }
        }
    }

}
