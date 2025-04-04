using System;
using System.Collections.Generic;

namespace SSHTunnelServer
{
    // Server Configuration class with enhanced security features
    public class ServerConfig
    {
        // Existing properties
        public string Name { get; set; }
        public int ListenPort { get; set; }
        public string AllowedClients { get; set; }
        public bool UseEncryption { get; set; }
        public bool IsActive { get; set; }

        // Advanced server properties
        public SecurityLevel SecurityLevel { get; set; } = SecurityLevel.Standard;
        public bool PasswordAuthentication { get; set; } = true;
        public bool PubkeyAuthentication { get; set; } = true;
        public bool CertificateAuthentication { get; set; } = false;
        public bool KeyboardInteractiveAuth { get; set; } = false;
        public bool YubikeyAuthentication { get; set; } = false;
        public bool TwoFactorAuthentication { get; set; } = false;
        public int MaxAuthTries { get; set; } = 6;
        public int LoginGraceTime { get; set; } = 120;
        public bool PermitRootLogin { get; set; } = false;

        // YubiKey settings
        public bool EnableYubiKey { get; set; } = false;
        public string YubiKeyAuthServer { get; set; } = "api.yubico.com";
        public string YubiKeyAPIKey { get; set; } = "";
        public string YubiKeyClientID { get; set; } = "";

        // Server key and certificate
        public string ServerKeyName { get; set; } = "";
        public string ServerCertName { get; set; } = "";
        public string ServerKeyPath { get; set; } = "";
        public string ServerCertPath { get; set; } = "";

        // Logging settings
        public LogLevel LogLevel { get; set; } = LogLevel.Info;
        public bool EnableAuditLogging { get; set; } = false;
        public string LogFilePath { get; set; } = "";
        public bool ChallengeResponseAuth { get; set; } = false; // This is the property name you need

    }


    // Security level enum
    public enum SecurityLevel
    {
        Basic,
        Standard,
        High,
        Custom
    }

    // Authentication methods supported by server
    public enum AuthMethod
    {
        Password,
        PublicKey,
        Certificate,
        KeyboardInteractive,
        YubiKey,
        TwoFactor
    }

    // Log level enum
    public enum LogLevel
    {
        Error,
        Info,
        Verbose,
        Debug
    }
}