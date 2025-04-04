using System;
using System.Collections.Generic;

namespace SSHTunnelServer
{
    // Server Configuration class with enhanced security features
    public class ServerConfig
    {
        // Basic properties
        public string Name { get; set; }
        public int ListenPort { get; set; }
        public string AllowedClients { get; set; }
        public bool IsActive { get; set; }

        // Security features
        public bool UseEncryption { get; set; }
        public SecurityLevel SecurityLevel { get; set; } = SecurityLevel.Standard;

        // Authentication options
        public List<AuthMethod> AllowedAuthMethods { get; set; } = new List<AuthMethod>
        {
            AuthMethod.Password,
            AuthMethod.PublicKey
        };

        // Advanced settings
        public int MaxAuthTries { get; set; } = 6;
        public int LoginGraceTime { get; set; } = 120; // seconds
        public bool PermitRootLogin { get; set; } = false;
        public bool PasswordAuthentication { get; set; } = true;
        public bool PubkeyAuthentication { get; set; } = true;
        public bool ChallengeResponseAuth { get; set; } = false;

        // YubiKey settings
        public bool EnableYubiKey { get; set; } = false;
        public string YubiKeyAuthServer { get; set; } = "api.yubico.com";
        public string YubiKeyAPIKey { get; set; } = "";
        public string YubiKeyClientID { get; set; } = "";

        // Server identification
        public string ServerKeyPath { get; set; } = "";
        public string ServerCertPath { get; set; } = "";

        // Logging options
        public LogLevel LogLevel { get; set; } = LogLevel.Info;
        public bool EnableAuditLogging { get; set; } = false;
        public string LogFilePath { get; set; } = "";
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