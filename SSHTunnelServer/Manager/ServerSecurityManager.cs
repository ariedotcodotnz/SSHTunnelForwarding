using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Windows.Forms;
using System.Net;

namespace SSHTunnelServer
{
    // Server Security Manager class for handling server security features
    public class ServerSecurityManager
    {
        private const string CERT_STORE_PATH = "server_certs";
        private const string KEY_STORE_PATH = "server_keys";

        public ServerSecurityManager()
        {
            // Create storage directories if they don't exist
            string appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SSHTunnelServer");

            string certStorePath = Path.Combine(appDataPath, CERT_STORE_PATH);
            string keyStorePath = Path.Combine(appDataPath, KEY_STORE_PATH);

            if (!Directory.Exists(certStorePath))
                Directory.CreateDirectory(certStorePath);

            if (!Directory.Exists(keyStorePath))
                Directory.CreateDirectory(keyStorePath);
        }

        // Generate server key pair
        public bool GenerateServerKeyPair(string keyName, int keySize = 2048)
        {
            try
            {
                string appDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "SSHTunnelServer");
                string keyStorePath = Path.Combine(appDataPath, KEY_STORE_PATH);

                // Generate RSA key pair
                using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(keySize))
                {
                    // Export private key
                    string privateKey = rsa.ToXmlString(true);
                    string privateKeyPath = Path.Combine(keyStorePath, $"{keyName}.key");
                    File.WriteAllText(privateKeyPath, privateKey);

                    // Export public key
                    string publicKey = rsa.ToXmlString(false);
                    string publicKeyPath = Path.Combine(keyStorePath, $"{keyName}.pub");
                    File.WriteAllText(publicKeyPath, publicKey);

                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating server key pair: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        // Generate a self-signed certificate for the server
        public bool GenerateServerCertificate(string certificateName, string subjectName, int validityInYears = 5)
        {
            try
            {
                string appDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "SSHTunnelServer");
                string certStorePath = Path.Combine(appDataPath, CERT_STORE_PATH);

                // For .NET Framework, we need to use a more complex approach to generate self-signed certificates
                // This is a simplified version - in production, use a proper certificate library

                // Create certificate properties
                CspParameters csp = new CspParameters
                {
                    KeyContainerName = $"SSHTunnelServer-{certificateName}",
                    Flags = CspProviderFlags.UseMachineKeyStore
                };

                // Create the certificate
                using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(2048, csp))
                {
                    // Generate a random serial number
                    byte[] serialNumber = new byte[16];
                    using (var rng = new RNGCryptoServiceProvider())
                    {
                        rng.GetBytes(serialNumber);
                    }

                    // Create certificate request
                    string certificateRequest = CreateCertificateRequest(rsa, subjectName);

                    // In a real implementation, we would use the certificate request to generate 
                    // a self-signed certificate. For simplicity, we're just saving the certificate data.

                    // Save the certificate data
                    string certificatePath = Path.Combine(certStorePath, $"{certificateName}.cer");
                    File.WriteAllText(certificatePath, certificateRequest);

                    // Save the private key
                    string privateKeyPath = Path.Combine(certStorePath, $"{certificateName}.key");
                    File.WriteAllText(privateKeyPath, rsa.ToXmlString(true));

                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating server certificate: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        // Create a basic certificate request (simplified for the example)
        private string CreateCertificateRequest(RSACryptoServiceProvider rsa, string subjectName)
        {
            // In a real implementation, we would create a proper PKCS#10 certificate request
            // For simplicity, we're just creating a basic structure

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("-----BEGIN CERTIFICATE REQUEST-----");
            sb.AppendLine(Convert.ToBase64String(Encoding.UTF8.GetBytes($"Subject={subjectName}"),
                Base64FormattingOptions.InsertLineBreaks));
            sb.AppendLine("-----END CERTIFICATE REQUEST-----");

            return sb.ToString();
        }

        public bool ChallengeResponseAuth { get; set; } = false;

        // Import an existing certificate
        public bool ImportCertificate(string certificatePath, string password = null)
        {
            try
            {
                string appDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "SSHTunnelServer");
                string certStorePath = Path.Combine(appDataPath, CERT_STORE_PATH);

                // Load the certificate
                X509Certificate2 cert;
                if (string.IsNullOrEmpty(password))
                {
                    cert = new X509Certificate2(certificatePath);
                }
                else
                {
                    cert = new X509Certificate2(certificatePath, password);
                }

                // Save to certificate store
                string certName = $"{cert.SubjectName.Name.Replace(",", "_").Replace(" ", "_")}.pfx";
                string savePath = Path.Combine(certStorePath, certName);

                // Export with private key if possible
                byte[] certBytes;
                if (cert.HasPrivateKey)
                {
                    // Generate a random password for the PFX
                    string randomPwd = GenerateRandomPassword(16);
                    certBytes = cert.Export(X509ContentType.Pfx, randomPwd);

                    // Store the password securely (simplified here)
                    File.WriteAllText(savePath + ".pwd", EncryptText(randomPwd));
                }
                else
                {
                    certBytes = cert.Export(X509ContentType.Cert);
                }

                File.WriteAllBytes(savePath, certBytes);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error importing certificate: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        // Generate SSH configuration based on ServerConfig
        public string GenerateSSHConfig(ServerConfig config)
        {
            StringBuilder sb = new StringBuilder();

            // Basic settings
            sb.AppendLine($"# SSH Server Configuration for {config.Name}");
            sb.AppendLine($"Port {config.ListenPort}");
            sb.AppendLine("Protocol 2");
            sb.AppendLine("HostKey /etc/ssh/ssh_host_rsa_key");
            sb.AppendLine("HostKey /etc/ssh/ssh_host_ecdsa_key");
            sb.AppendLine("HostKey /etc/ssh/ssh_host_ed25519_key");

            // If custom server key is provided
            if (!string.IsNullOrEmpty(config.ServerKeyPath))
            {
                sb.AppendLine($"HostKey {config.ServerKeyPath}");
            }

            // Logging
            sb.AppendLine("SyslogFacility AUTH");
            switch (config.LogLevel)
            {
                case LogLevel.Error:
                    sb.AppendLine("LogLevel ERROR");
                    break;
                case LogLevel.Info:
                    sb.AppendLine("LogLevel INFO");
                    break;
                case LogLevel.Verbose:
                    sb.AppendLine("LogLevel VERBOSE");
                    break;
                case LogLevel.Debug:
                    sb.AppendLine("LogLevel DEBUG");
                    break;
            }

            // Authentication settings
            sb.AppendLine($"LoginGraceTime {config.LoginGraceTime}s");
            sb.AppendLine($"PermitRootLogin {(config.PermitRootLogin ? "yes" : "no")}");
            sb.AppendLine("StrictModes yes");
            sb.AppendLine($"MaxAuthTries {config.MaxAuthTries}");
            sb.AppendLine("MaxSessions 10");
            sb.AppendLine($"PubkeyAuthentication {(config.PubkeyAuthentication ? "yes" : "no")}");
            sb.AppendLine($"PasswordAuthentication {(config.PasswordAuthentication ? "yes" : "no")}");
            sb.AppendLine("PermitEmptyPasswords no");
            sb.AppendLine($"ChallengeResponseAuthenication {(config.ChallengeResponseAuth ? "yes" : "no")}");


            // Add allowed clients configuration
            if (config.AllowedClients != "*")
            {
                string[] allowedIPs = config.AllowedClients.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string ip in allowedIPs)
                {
                    sb.AppendLine($"AllowUsers *@{ip.Trim()}");
                }
            }

            // SSH forwarding settings
            sb.AppendLine("X11Forwarding yes");
            sb.AppendLine("PrintMotd no");
            sb.AppendLine("AcceptEnv LANG LC_*");
            sb.AppendLine("Subsystem sftp /usr/lib/openssh/sftp-server");
            sb.AppendLine("GatewayPorts yes");  // Important for port forwarding
            sb.AppendLine("PermitTunnel yes");  // Enable tunneling
            sb.AppendLine("AllowTcpForwarding yes");  // Allow TCP forwarding
            sb.AppendLine("AllowStreamLocalForwarding yes");

            // YubiKey settings if enabled
            if (config.EnableYubiKey)
            {
                sb.AppendLine("# YubiKey Configuration");
                sb.AppendLine("ChallengeResponseAuthentication yes");
                sb.AppendLine($"AuthenticationMethods publickey,keyboard-interactive:pam");
                if (!string.IsNullOrEmpty(config.YubiKeyAuthServer))
                {
                    sb.AppendLine($"# YubiKey validation server: {config.YubiKeyAuthServer}");
                }
            }

            // Add security level specific settings
            switch (config.SecurityLevel)
            {
                case SecurityLevel.High:
                    sb.AppendLine("\n# High Security Settings");
                    sb.AppendLine("Ciphers aes256-ctr,aes192-ctr,aes128-ctr");
                    sb.AppendLine("MACs hmac-sha2-512,hmac-sha2-256");
                    sb.AppendLine("KexAlgorithms curve25519-sha256,diffie-hellman-group-exchange-sha256");
                    break;

                case SecurityLevel.Basic:
                    sb.AppendLine("\n# Basic Security Settings (Compatibility Mode)");
                    sb.AppendLine("Ciphers aes128-ctr,aes192-ctr,aes256-ctr,aes128-cbc,3des-cbc");
                    sb.AppendLine("MACs hmac-sha1,hmac-sha2-256,hmac-sha2-512");
                    break;

                    // Standard security is default, no additional settings needed
            }

            return sb.ToString();
        }

        // Verify YubiKey OTP (simplified implementation)
        public bool VerifyYubiKeyOTP(string otp, string clientID, string apiKey)
        {
            try
            {
                // In a real implementation, this would make an API call to the YubiKey validation server
                // This is a simplified version for demonstration purposes

                if (string.IsNullOrEmpty(otp) || otp.Length < 32 || otp.Length > 48)
                {
                    return false;  // Invalid OTP format
                }

                string url = $"https://api.yubico.com/wsapi/2.0/verify?otp={otp}&id={clientID}&nonce={GenerateNonce()}";

                // In a real implementation, we would make an HTTPS request to the YubiKey validation server
                // And parse the response to verify the OTP

                // For this example, we'll just simulate a successful verification
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // Check if an IP is allowed
        public bool IsIPAllowed(string ip, string allowedClients)
        {
            if (allowedClients == "*")
                return true;

            string[] allowedIPs = allowedClients.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string allowedIP in allowedIPs)
            {
                string trimmedIP = allowedIP.Trim();

                // Check for exact match
                if (trimmedIP == ip)
                    return true;

                // Check for CIDR notation (e.g., 192.168.1.0/24)
                if (trimmedIP.Contains("/"))
                {
                    if (IsIPInCIDRRange(ip, trimmedIP))
                        return true;
                }

                // Check for wildcard (e.g., 192.168.1.*)
                if (trimmedIP.EndsWith("*"))
                {
                    string prefix = trimmedIP.TrimEnd('*');
                    if (ip.StartsWith(prefix))
                        return true;
                }
            }

            return false;
        }

        // Check if an IP is in a CIDR range
        private bool IsIPInCIDRRange(string ip, string cidr)
        {
            try
            {
                string[] parts = cidr.Split('/');
                if (parts.Length != 2)
                    return false;

                // Parse CIDR notation
                IPAddress networkAddress = IPAddress.Parse(parts[0]);
                int prefixLength = int.Parse(parts[1]);

                // Convert IP addresses to bytes
                byte[] networkBytes = networkAddress.GetAddressBytes();
                byte[] ipBytes = IPAddress.Parse(ip).GetAddressBytes();

                // Compare the common prefix
                int prefixFullBytes = prefixLength / 8;
                int prefixRemainingBits = prefixLength % 8;

                // Check full bytes
                for (int i = 0; i < prefixFullBytes; i++)
                {
                    if (networkBytes[i] != ipBytes[i])
                        return false;
                }

                // Check remaining bits if any
                if (prefixRemainingBits > 0)
                {
                    int mask = 0xFF << (8 - prefixRemainingBits);
                    return (networkBytes[prefixFullBytes] & mask) == (ipBytes[prefixFullBytes] & mask);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        // Generate a random nonce for YubiKey validation
        private string GenerateNonce()
        {
            byte[] nonceBytes = new byte[16];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(nonceBytes);
            }

            return BitConverter.ToString(nonceBytes).Replace("-", "").ToLower();
        }

        // Generate a random password
        private string GenerateRandomPassword(int length)
        {
            const string validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%^&*()-_=+";
            StringBuilder sb = new StringBuilder();

            using (var rng = new RNGCryptoServiceProvider())
            {
                byte[] randomBytes = new byte[length];
                rng.GetBytes(randomBytes);

                for (int i = 0; i < length; i++)
                {
                    sb.Append(validChars[randomBytes[i] % validChars.Length]);
                }
            }

            return sb.ToString();
        }

        // Simple encryption for storing sensitive data
        private string EncryptText(string plainText)
        {
            // This is a simplified implementation - in a production app, use Windows Data Protection API
            // or a proper secure storage solution

            // Create a simple XOR encryption with a fixed key
            byte[] keyBytes = Encoding.UTF8.GetBytes("SSHTunnelServerSecretKey123456789");
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] encryptedBytes = new byte[plainBytes.Length];

            for (int i = 0; i < plainBytes.Length; i++)
            {
                encryptedBytes[i] = (byte)(plainBytes[i] ^ keyBytes[i % keyBytes.Length]);
            }

            return Convert.ToBase64String(encryptedBytes);
        }

        // List all available server keys
        public List<string> GetAvailableServerKeys()
        {
            string appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SSHTunnelServer");
            string keyStorePath = Path.Combine(appDataPath, KEY_STORE_PATH);

            List<string> keys = new List<string>();

            if (Directory.Exists(keyStorePath))
            {
                foreach (string file in Directory.GetFiles(keyStorePath, "*.key"))
                {
                    keys.Add(Path.GetFileNameWithoutExtension(file));
                }
            }

            return keys;
        }

        // List all available server certificates
        public List<string> GetAvailableServerCertificates()
        {
            string appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SSHTunnelServer");
            string certStorePath = Path.Combine(appDataPath, CERT_STORE_PATH);

            List<string> certs = new List<string>();

            if (Directory.Exists(certStorePath))
            {
                foreach (string file in Directory.GetFiles(certStorePath, "*.pfx"))
                {
                    certs.Add(Path.GetFileNameWithoutExtension(file));
                }
            }

            return certs;
        }
    }
}