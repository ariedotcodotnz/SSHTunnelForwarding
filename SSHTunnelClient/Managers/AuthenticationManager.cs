using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Windows.Forms;
using SSHTunnelClient.Models;
using SSHTunnelClient;

namespace SSHTunnelClient
{
    // Enhanced Authentication Manager
    public class AuthenticationManager
    {
        private const string CERT_STORE_PATH = "cert_store";
        private const string KEY_STORE_PATH = "key_store";

        public AuthenticationManager()
        {
            // Create storage directories if they don't exist
            string appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SSHTunnelClient");

            string certStorePath = Path.Combine(appDataPath, CERT_STORE_PATH);
            string keyStorePath = Path.Combine(appDataPath, KEY_STORE_PATH);

            if (!Directory.Exists(certStorePath))
                Directory.CreateDirectory(certStorePath);

            if (!Directory.Exists(keyStorePath))
                Directory.CreateDirectory(keyStorePath);
        }

        // Generate a new SSH key pair
        public bool GenerateSSHKeyPair(string keyName, string passphrase, int keySize = 2048)
        {
            try
            {
                string appDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "SSHTunnelClient");
                string keyStorePath = Path.Combine(appDataPath, KEY_STORE_PATH);

                // Generate RSA key pair
                using (RSA rsa = RSA.Create())
                {
                    rsa.KeySize = keySize;

                    // Export private key using parameters instead
                    RSAParameters parameters = rsa.ExportParameters(true);
                    byte[] privateKeyBytes = ExportPrivateKeyToDER(parameters);
                    string privateKeyPath = Path.Combine(keyStorePath, $"{keyName}.pem");

                    // If passphrase is provided, encrypt the private key
                    if (!string.IsNullOrEmpty(passphrase))
                    {
                        privateKeyBytes = EncryptPrivateKey(privateKeyBytes, passphrase);
                    }

                    File.WriteAllBytes(privateKeyPath, privateKeyBytes);

                    // Export public key
                    string publicKeyPath = Path.Combine(keyStorePath, $"{keyName}.pub");
                    string publicKeyOpenSSH = ConvertToOpenSSHPublicKey(parameters, keyName);
                    File.WriteAllText(publicKeyPath, publicKeyOpenSSH);

                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating SSH key pair: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        // Add these helper methods:
        private byte[] ExportPrivateKeyToDER(RSAParameters parameters)
        {
            // Simple implementation - in production you would use proper ASN.1 DER encoding
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter bw = new BinaryWriter(ms);

                // Write private key components
                WriteParameter(bw, parameters.Modulus);
                WriteParameter(bw, parameters.Exponent);
                WriteParameter(bw, parameters.D);
                WriteParameter(bw, parameters.P);
                WriteParameter(bw, parameters.Q);
                WriteParameter(bw, parameters.DP);
                WriteParameter(bw, parameters.DQ);
                WriteParameter(bw, parameters.InverseQ);

                return ms.ToArray();
            }
        }

        private void WriteParameter(BinaryWriter bw, byte[] value)
        {
            if (value != null)
            {
                bw.Write(value.Length);
                bw.Write(value);
            }
            else
            {
                bw.Write(0);
            }
        }
        private byte[] ReverseBytes(byte[] bytes)
        {
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
            return bytes;
        }

        // Convert RSA public key bytes to OpenSSH format
        private string ConvertToOpenSSHPublicKey(RSAParameters parameters, string comment)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("ssh-rsa ");

            // Add the base64 encoded portion
            byte[] keyTypeBytes = Encoding.ASCII.GetBytes("ssh-rsa");
            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    // Key type length and value
                    bw.Write(ReverseBytes(BitConverter.GetBytes(keyTypeBytes.Length)));
                    bw.Write(keyTypeBytes);

                    // Public exponent length and value
                    bw.Write(ReverseBytes(BitConverter.GetBytes(parameters.Exponent.Length)));
                    bw.Write(parameters.Exponent);

                    // Modulus length and value
                    bw.Write(ReverseBytes(BitConverter.GetBytes(parameters.Modulus.Length)));
                    bw.Write(parameters.Modulus);
                }

                sb.Append(Convert.ToBase64String(ms.ToArray()));
            }

            // Add the comment
            sb.Append(" ");
            sb.Append(comment);

            return sb.ToString();
        }

        // Encrypt private key with passphrase
        private byte[] EncryptPrivateKey(byte[] privateKeyBytes, string passphrase)
        {
            byte[] salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            byte[] key;
            using (var deriveBytes = new Rfc2898DeriveBytes(passphrase, salt, 10000))
            {
                key = deriveBytes.GetBytes(32); // 256 bits for AES-256
            }

            byte[] iv = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(iv);
            }

            byte[] encryptedKey;
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;

                using (var encryptor = aes.CreateEncryptor())
                using (var ms = new MemoryStream())
                {
                    ms.Write(salt, 0, salt.Length);
                    ms.Write(iv, 0, iv.Length);

                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        cs.Write(privateKeyBytes, 0, privateKeyBytes.Length);
                    }

                    encryptedKey = ms.ToArray();
                }
            }

            return encryptedKey;
        }

        // Import a certificate for certificate-based authentication
        public bool ImportCertificate(string certificatePath, string password = null)
        {
            try
            {
                string appDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "SSHTunnelClient");
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

        // Generate a random password
        private string GenerateRandomPassword(int length)
        {
            const string validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%^&*()-_=+";
            StringBuilder sb = new StringBuilder();

            using (var rng = RandomNumberGenerator.Create())
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

        // Generate a TOTP code for 2FA
        public string GenerateTOTPCode(string secretKey)
        {
            try
            {
                // Decode the Base32 encoded secret
                byte[] key = Base32Decode(secretKey);

                // Get the current Unix timestamp divided by 30 (30-second intervals)
                long counter = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30;

                // Convert counter to bytes
                byte[] counterBytes = BitConverter.GetBytes(counter);
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(counterBytes);
                }

                // Pad the counter to 8 bytes
                byte[] paddedCounter = new byte[8];
                counterBytes.CopyTo(paddedCounter, 8 - counterBytes.Length);

                // Compute HMAC-SHA-1
                HMACSHA1 hmac = new HMACSHA1(key);
                byte[] hash = hmac.ComputeHash(paddedCounter);

                // Get the offset into the hash
                int offset = hash[hash.Length - 1] & 0x0F;

                // Get 4 bytes starting at the offset
                int truncatedHash = ((hash[offset] & 0x7F) << 24) |
                                   ((hash[offset + 1] & 0xFF) << 16) |
                                   ((hash[offset + 2] & 0xFF) << 8) |
                                   (hash[offset + 3] & 0xFF);

                // Compute the TOTP code (6 digits)
                int totp = truncatedHash % 1000000;

                // Format as 6 digits
                return totp.ToString("D6");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating TOTP code: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        // Base32 decoding (used for TOTP)
        private byte[] Base32Decode(string input)
        {
            input = input.ToUpperInvariant().TrimEnd('=');

            // Initialize the dictionary
            Dictionary<char, byte> base32Dict = new Dictionary<char, byte>
            {
                { 'A', 0 }, { 'B', 1 }, { 'C', 2 }, { 'D', 3 }, { 'E', 4 }, { 'F', 5 }, { 'G', 6 }, { 'H', 7 },
                { 'I', 8 }, { 'J', 9 }, { 'K', 10 }, { 'L', 11 }, { 'M', 12 }, { 'N', 13 }, { 'O', 14 }, { 'P', 15 },
                { 'Q', 16 }, { 'R', 17 }, { 'S', 18 }, { 'T', 19 }, { 'U', 20 }, { 'V', 21 }, { 'W', 22 }, { 'X', 23 },
                { 'Y', 24 }, { 'Z', 25 }, { '2', 26 }, { '3', 27 }, { '4', 28 }, { '5', 29 }, { '6', 30 }, { '7', 31 }
            };

            // Calculate the output length
            int outputLength = input.Length * 5 / 8;
            byte[] result = new byte[outputLength];

            // Process the input in chunks of 8 characters (40 bits) to produce 5 bytes
            int buffer = 0;
            int bitsRemaining = 0;
            int resultIndex = 0;

            foreach (char c in input)
            {
                if (!base32Dict.TryGetValue(c, out byte value))
                {
                    throw new FormatException($"Invalid Base32 character: {c}");
                }

                buffer = (buffer << 5) | value;
                bitsRemaining += 5;

                if (bitsRemaining >= 8)
                {
                    bitsRemaining -= 8;
                    result[resultIndex++] = (byte)(buffer >> bitsRemaining);
                }
            }

            return result;
        }

        // Simple encryption for storing sensitive data
        private string EncryptText(string plainText)
        {
            // This is a simplified implementation - in a production app, use Windows Data Protection API
            // or a proper secure storage solution

            // Create a simple XOR encryption with a fixed key
            byte[] keyBytes = Encoding.UTF8.GetBytes("SSHTunnelClientSecretKey123456789");
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] encryptedBytes = new byte[plainBytes.Length];

            for (int i = 0; i < plainBytes.Length; i++)
            {
                encryptedBytes[i] = (byte)(plainBytes[i] ^ keyBytes[i % keyBytes.Length]);
            }

            return Convert.ToBase64String(encryptedBytes);
        }

        // List all available keys
        public List<string> GetAvailableSSHKeys()
        {
            string appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SSHTunnelClient");
            string keyStorePath = Path.Combine(appDataPath, KEY_STORE_PATH);

            List<string> keys = new List<string>();

            if (Directory.Exists(keyStorePath))
            {
                foreach (string file in Directory.GetFiles(keyStorePath, "*.pem"))
                {
                    keys.Add(Path.GetFileNameWithoutExtension(file));
                }
            }

            return keys;
        }

        // List all available certificates
        public List<string> GetAvailableCertificates()
        {
            string appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SSHTunnelClient");
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