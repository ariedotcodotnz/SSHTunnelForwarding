using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using Microsoft.Win32;
using System.Security.Cryptography;
using System.Text;
using SSHTunnelClient.Models;

namespace SSHTunnelClient
{
    // Enhanced Port Forwarding Configuration class with YubiKey support
    public class PortForwardingConfig
    {
        public string Name { get; set; }
        public string ServerHost { get; set; }
        public int ServerPort { get; set; }
        public string Username { get; set; }

        // Authentication properties
        public AuthMethod AuthenticationMethod { get; set; }
        public string Password { get; set; }
        public string PrivateKeyPath { get; set; }
        public string PrivateKeyPassphrase { get; set; }
        public string CertificateName { get; set; }
        public bool UseTOTP { get; set; }
        public string TOTPSecretKey { get; set; }

        // YubiKey properties
        public YubiKeyMode YubiKeyMode { get; set; }
        public string YubiKeyPIN { get; set; }
        public string YubiKeyProvider { get; set; }  // Path to PKCS#11 provider

        // Port forwarding properties
        public int LocalPort { get; set; }
        public string RemoteHost { get; set; }
        public int RemotePort { get; set; }
        public bool UseEncryption { get; set; }
        public bool IsActive { get; set; }

        // Connection settings
        public int ConnectionTimeout { get; set; } = 30; // seconds
        public int KeepAliveInterval { get; set; } = 60; // seconds
        public bool EnableCompression { get; set; } = true;

        // Advanced settings
        public string SSHOptions { get; set; } = ""; // Additional SSH command-line options
    }
}
}
