using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Windows.Forms;
using System.Threading.Tasks;

namespace SSHTunnelClient
{
    // YubiKey Manager class for integrating YubiKey authentication
    public class YubiKeyManager
    {
        private const string YUBIKEY_DIR = "yubikey";
        private string _appDataPath;

        // Native methods for YubiKey communication (Windows only)
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateFile(
            string lpFileName, uint dwDesiredAccess, uint dwShareMode,
            IntPtr lpSecurityAttributes, uint dwCreationDisposition,
            uint dwFlagsAndAttributes, IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("hid.dll", SetLastError = true)]
        private static extern bool HidD_GetAttributes(
            IntPtr HidDeviceObject, ref HIDD_ATTRIBUTES Attributes);

        [StructLayout(LayoutKind.Sequential)]
        private struct HIDD_ATTRIBUTES
        {
            public int Size;
            public ushort VendorID;
            public ushort ProductID;
            public ushort VersionNumber;
        }

        // YubiKey vendor ID
        private const ushort YUBIKEY_VID = 0x1050;

        public YubiKeyManager()
        {
            _appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SSHTunnelClient");

            string yubiKeyPath = Path.Combine(_appDataPath, YUBIKEY_DIR);
            if (!Directory.Exists(yubiKeyPath))
            {
                Directory.CreateDirectory(yubiKeyPath);
            }
        }

        // Check if YubiKey is present on the system
        public bool IsYubiKeyPresent()
        {
            // First, check if the YubiKey app is installed
            if (IsYubiKeyManagerInstalled())
            {
                // Then check for connected devices
                return DetectYubiKeyDevice();
            }

            return false;
        }

        // Detect if YubiKey Manager application is installed
        private bool IsYubiKeyManagerInstalled()
        {
            try
            {
                string[] commonPaths = {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Yubico", "YubiKey Manager", "ykman.exe"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Yubico", "YubiKey Manager", "ykman.exe")
                };

                foreach (string path in commonPaths)
                {
                    if (File.Exists(path))
                    {
                        return true;
                    }
                }

                // Check if ykman is in PATH
                Process process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "where",
                        Arguments = "ykman",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                return !string.IsNullOrEmpty(output) && output.Contains("ykman");
            }
            catch
            {
                return false;
            }
        }

        // Detect physically connected YubiKey device
        private bool DetectYubiKeyDevice()
        {
            try
            {
                // Use ykman to check for devices
                Process process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "ykman",
                        Arguments = "list",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                return !string.IsNullOrEmpty(output) && !output.Contains("No YubiKey detected");
            }
            catch
            {
                // Fallback to direct HID detection if ykman fails
                try
                {
                    return DetectYubiKeyHID();
                }
                catch
                {
                    return false;
                }
            }
        }

        // Direct HID detection of YubiKey
        private bool DetectYubiKeyHID()
        {
            // Use Windows HID API to detect YubiKey
            IntPtr deviceHandle = CreateFile(
                "\\\\.\\HID#VID_1050&PID_0407",
                0, // No access needed, just checking existence
                3, // FILE_SHARE_READ | FILE_SHARE_WRITE
                IntPtr.Zero,
                3, // OPEN_EXISTING
                0, // No special attributes
                IntPtr.Zero);

            if (deviceHandle != new IntPtr(-1))
            {
                // Device found
                CloseHandle(deviceHandle);
                return true;
            }

            return false;
        }

        // Get YubiKey device info
        public YubiKeyInfo GetYubiKeyInfo()
        {
            YubiKeyInfo info = new YubiKeyInfo();

            try
            {
                Process process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "ykman",
                        Arguments = "info",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                if (!string.IsNullOrEmpty(output))
                {
                    // Parse the output to extract YubiKey info
                    if (output.Contains("Device type:"))
                    {
                        string[] lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (string line in lines)
                        {
                            if (line.StartsWith("Device type:"))
                                info.DeviceType = line.Replace("Device type:", "").Trim();
                            else if (line.StartsWith("Serial number:"))
                                info.SerialNumber = line.Replace("Serial number:", "").Trim();
                            else if (line.StartsWith("Firmware version:"))
                                info.FirmwareVersion = line.Replace("Firmware version:", "").Trim();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting YubiKey info: {ex.Message}");
            }

            return info;
        }

        // Check if YubiKey has SSH support configured
        public bool HasSSHSupport()
        {
            try
            {
                // Check for PIV or OpenPGP support
                Process process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "ykman",
                        Arguments = "piv info",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                return !string.IsNullOrEmpty(output) && output.Contains("Authentication") && !error.Contains("not supported");
            }
            catch
            {
                return false;
            }
        }

        // Configure YubiKey for SSH authentication using PIV
        public bool ConfigureForSSH(string pinCode)
        {
            if (string.IsNullOrEmpty(pinCode) || pinCode.Length < 6)
            {
                MessageBox.Show("PIN must be at least 6 characters long.", "Invalid PIN",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            try
            {
                // Generate a new key in slot 9a (PIV Authentication)
                Process process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "ykman",
                        Arguments = "piv generate-key -a RSA2048 9a ssh_cert.pub",
                        UseShellExecute = false,
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();

                // Supply PIN when prompted
                process.StandardInput.WriteLine(pinCode);
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0 || !string.IsNullOrEmpty(error))
                {
                    MessageBox.Show($"Error configuring YubiKey: {error}", "Configuration Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                // Now create certificate for the key
                process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "ykman",
                        Arguments = "piv generate-certificate -s \"SSH Authentication\" 9a ssh_cert.pub",
                        UseShellExecute = false,
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                process.StandardInput.WriteLine(pinCode);
                output = process.StandardOutput.ReadToEnd();
                error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0 || !string.IsNullOrEmpty(error))
                {
                    MessageBox.Show($"Error creating certificate: {error}", "Configuration Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                // Save the public key
                string yubiKeyPath = Path.Combine(_appDataPath, YUBIKEY_DIR);
                string pubKeyPath = Path.Combine(yubiKeyPath, "id_rsa_yubikey.pub");

                if (File.Exists("ssh_cert.pub"))
                {
                    File.Copy("ssh_cert.pub", pubKeyPath, true);
                    File.Delete("ssh_cert.pub");
                }

                MessageBox.Show("YubiKey configured successfully for SSH authentication. " +
                    $"The public key has been saved to: {pubKeyPath}",
                    "YubiKey Configured", MessageBoxButtons.OK, MessageBoxIcon.Information);

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error configuring YubiKey: {ex.Message}", "Configuration Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        // Configure SSH to use YubiKey via PKCS#11
        public string ConfigureSSHAgent(string pinCode)
        {
            try
            {
                // Create SSH config file to use YubiKey
                string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                string sshConfigDir = Path.Combine(userProfile, ".ssh");
                string configPath = Path.Combine(sshConfigDir, "config");
                string pkcs11Provider = string.Empty;

                // Determine the PKCS#11 provider path based on OS
                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    // Windows
                    pkcs11Provider = @"C:\Program Files\Yubico\Yubico PIV Tool\bin\libykcs11.dll";
                    if (!File.Exists(pkcs11Provider))
                    {
                        pkcs11Provider = @"C:\Program Files (x86)\Yubico\Yubico PIV Tool\bin\libykcs11.dll";
                    }
                }
                else
                {
                    // Assume Linux/macOS format
                    pkcs11Provider = "/usr/local/lib/libykcs11.so";
                }

                if (!Directory.Exists(sshConfigDir))
                {
                    Directory.CreateDirectory(sshConfigDir);
                }

                // Create or update SSH config
                string configContent = string.Empty;
                if (File.Exists(configPath))
                {
                    configContent = File.ReadAllText(configPath);
                }

                if (!configContent.Contains("PKCS11Provider"))
                {
                    string newConfig = $@"
# YubiKey SSH configuration
Host *
    PKCS11Provider ""{pkcs11Provider}""";

                    File.AppendAllText(configPath, newConfig);
                }

                // Test the configuration
                Process process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "ssh-add",
                        Arguments = "-s \"" + pkcs11Provider + "\"",
                        UseShellExecute = false,
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                process.StandardInput.WriteLine(pinCode);
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                return pkcs11Provider;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error configuring SSH agent: {ex.Message}", "Configuration Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return string.Empty;
            }
        }

        // Get the path to the YubiKey public key
        public string GetPublicKeyPath()
        {
            string yubiKeyPath = Path.Combine(_appDataPath, YUBIKEY_DIR);
            string pubKeyPath = Path.Combine(yubiKeyPath, "id_rsa_yubikey.pub");

            if (File.Exists(pubKeyPath))
            {
                return pubKeyPath;
            }

            return string.Empty;
        }

        // Get the YubiKey challenge-response
        public async Task<string> GetChallengeResponseAsync(string challenge)
        {
            try
            {
                // Create a temporary challenge file
                string challengeFile = Path.Combine(Path.GetTempPath(), "yubikey_challenge.txt");
                File.WriteAllText(challengeFile, challenge);

                // Get response using YubiKey HMAC-SHA1 challenge-response
                Process process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "ykman",
                        Arguments = $"oath code",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();

                // Read response asynchronously
                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                await Task.Run(() => process.WaitForExit());

                // Clean up
                if (File.Exists(challengeFile))
                {
                    File.Delete(challengeFile);
                }

                if (process.ExitCode != 0 || !string.IsNullOrEmpty(error))
                {
                    return string.Empty;
                }

                // Parse the response
                string response = string.Empty;
                if (!string.IsNullOrEmpty(output))
                {
                    string[] lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    if (lines.Length > 0)
                    {
                        string[] parts = lines[0].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 2)
                        {
                            response = parts[1].Trim();
                        }
                    }
                }

                return response;
            }
            catch
            {
                return string.Empty;
            }
        }
    }

    // YubiKey information class
    public class YubiKeyInfo
    {
        public string DeviceType { get; set; } = "Unknown";
        public string SerialNumber { get; set; } = "Unknown";
        public string FirmwareVersion { get; set; } = "Unknown";
    }
}