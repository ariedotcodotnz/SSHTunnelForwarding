using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using Microsoft.Win32;
using System.Security.Cryptography;
using System.Text;

namespace SSHTunnelClient
{
    // Port Forwarding Configuration class
    public class PortForwardingConfig
    {
        public string Name { get; set; }
        public string ServerHost { get; set; }
        public int ServerPort { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string PrivateKeyPath { get; set; }
        public int LocalPort { get; set; }
        public string RemoteHost { get; set; }
        public int RemotePort { get; set; }
        public bool UseEncryption { get; set; }
        public bool IsActive { get; set; }
    }
}
