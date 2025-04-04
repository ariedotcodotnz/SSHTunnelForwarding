using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SSHTunnelClient
{
    // Encryption Manager class
    public class EncryptionManager
    {
        private byte[] _key;
        private byte[] _iv;

        public EncryptionManager()
        {
            // In a real implementation, you would want to store these securely
            // and/or generate them dynamically
            _key = Encoding.UTF8.GetBytes("0123456789ABCDEF0123456789ABCDEF"); // 32 bytes for AES-256
            _iv = Encoding.UTF8.GetBytes("0123456789ABCDEF"); // 16 bytes for AES
        }

        public string EncryptText(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return plainText;

            try
            {
                byte[] encrypted;
                using (Aes aes = Aes.Create())
                {
                    aes.Key = _key;
                    aes.IV = _iv;

                    ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                    using (MemoryStream ms = new MemoryStream())
                    {
                        using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                        {
                            using (StreamWriter sw = new StreamWriter(cs))
                            {
                                sw.Write(plainText);
                            }
                            encrypted = ms.ToArray();
                        }
                    }
                }

                return Convert.ToBase64String(encrypted);
            }
            catch (Exception)
            {
                // In case of encryption failure, return the original text
                return plainText;
            }
        }

        public string DecryptText(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
                return cipherText;

            try
            {
                byte[] cipherBytes = Convert.FromBase64String(cipherText);
                string plaintext = null;

                using (Aes aes = Aes.Create())
                {
                    aes.Key = _key;
                    aes.IV = _iv;

                    ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                    using (MemoryStream ms = new MemoryStream(cipherBytes))
                    {
                        using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                        {
                            using (StreamReader reader = new StreamReader(cs))
                            {
                                plaintext = reader.ReadToEnd();
                            }
                        }
                    }
                }

                return plaintext;
            }
            catch (Exception)
            {
                // In case of decryption failure, return the original text
                return cipherText;
            }
        }
    }
}
