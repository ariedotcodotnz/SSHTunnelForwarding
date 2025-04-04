using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using Microsoft.Win32;
using System.Security.Cryptography;
using System.Text;

namespace SSHTunnelServer
{
    // Server Configuration class
    public class ServerConfig
    {
        public string Name { get; set; }
        public int ListenPort { get; set; }
        public string AllowedClients { get; set; }
        public bool UseEncryption { get; set; }
        public bool IsActive { get; set; }
    }
}
