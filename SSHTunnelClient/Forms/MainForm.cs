﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using SSHTunnelClient.Models;
using System.Threading.Tasks;

namespace SSHTunnelClient
{
    public partial class MainForm : Form
    {
        private BindingList<PortForwardingConfig> _configurations;
        private SettingsManager _settingsManager;
        private StartupManager _startupManager;
        private Dictionary<int, Process> _activeTunnels;
        private EncryptionManager _encryptionManager;

        public MainForm()
        {
            InitializeComponent();

            _settingsManager = new SettingsManager();
            _startupManager = new StartupManager("SSHTunnelClient");
            _activeTunnels = new Dictionary<int, Process>();
            _encryptionManager = new EncryptionManager();

            LoadConfigurations();
            InitializeUI();

            // Auto-connect if set in settings
            if (_settingsManager.GetAutoConnectOnStartup())
            {
                ConnectAllTunnels();
            }
        }

        private void InitializeUI()
        {
            // Set up the DataGridView for port forwarding configurations
            dataGridConfigurations.DataSource = _configurations;
            dataGridConfigurations.AutoGenerateColumns = false;
            dataGridConfigurations.Columns.Clear();

            // Add columns to the DataGridView
            dataGridConfigurations.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ConfigName",
                HeaderText = "Name",
                DataPropertyName = "Name",
                Width = 100
            });

            dataGridConfigurations.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ServerHost",
                HeaderText = "Server Host",
                DataPropertyName = "ServerHost",
                Width = 120
            });

            dataGridConfigurations.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ServerPort",
                HeaderText = "Server Port",
                DataPropertyName = "ServerPort",
                Width = 80
            });

            dataGridConfigurations.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "LocalPort",
                HeaderText = "Local Port",
                DataPropertyName = "LocalPort",
                Width = 80
            });

            dataGridConfigurations.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "RemoteHost",
                HeaderText = "Remote Host",
                DataPropertyName = "RemoteHost",
                Width = 120
            });

            dataGridConfigurations.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "RemotePort",
                HeaderText = "Remote Port",
                DataPropertyName = "RemotePort",
                Width = 80
            });

            dataGridConfigurations.Columns.Add(new DataGridViewCheckBoxColumn
            {
                Name = "UseEncryption",
                HeaderText = "Encrypt",
                DataPropertyName = "UseEncryption",
                Width = 60
            });

            dataGridConfigurations.Columns.Add(new DataGridViewCheckBoxColumn
            {
                Name = "IsActive",
                HeaderText = "Active",
                DataPropertyName = "IsActive",
                Width = 60,
                ReadOnly = true
            });

            // Set up checkboxes and buttons
            chkRunAtStartup.Checked = _startupManager.IsSetToRunAtStartup();
            chkAutoConnectOnStartup.Checked = _settingsManager.GetAutoConnectOnStartup();
        }

        private void LoadConfigurations()
        {
            _configurations = new BindingList<PortForwardingConfig>(_settingsManager.LoadConfigurations());
            if (_configurations.Count == 0)
            {
                // Add a default configuration if none exists
                _configurations.Add(new PortForwardingConfig
                {
                    Name = "Default",
                    ServerHost = "example.com",
                    ServerPort = 22,
                    Username = "username",
                    LocalPort = 8080,
                    RemoteHost = "localhost",
                    RemotePort = 80,
                    UseEncryption = false,
                    IsActive = false
                });
            }
        }

        private void SaveConfigurations()
        {
            _settingsManager.SaveConfigurations(_configurations.ToList());
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            using (var configForm = new ConfigurationForm(null))
            {
                if (configForm.ShowDialog() == DialogResult.OK)
                {
                    _configurations.Add(configForm.Configuration);
                    SaveConfigurations();
                }
            }
        }

        private void btnEdit_Click(object sender, EventArgs e)
        {
            if (dataGridConfigurations.SelectedRows.Count > 0)
            {
                var selectedConfig = (PortForwardingConfig)dataGridConfigurations.SelectedRows[0].DataBoundItem;
                using (var configForm = new ConfigurationForm(selectedConfig))
                {
                    if (configForm.ShowDialog() == DialogResult.OK)
                    {
                        int index = _configurations.IndexOf(selectedConfig);
                        _configurations[index] = configForm.Configuration;
                        SaveConfigurations();
                    }
                }
            }
        }

        private void btnRemove_Click(object sender, EventArgs e)
        {
            if (dataGridConfigurations.SelectedRows.Count > 0)
            {
                var selectedConfig = (PortForwardingConfig)dataGridConfigurations.SelectedRows[0].DataBoundItem;

                // Stop the tunnel if it's active
                if (selectedConfig.IsActive)
                {
                    DisconnectTunnel(selectedConfig);
                }

                _configurations.Remove(selectedConfig);
                SaveConfigurations();
            }
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            if (dataGridConfigurations.SelectedRows.Count > 0)
            {
                var selectedConfig = (PortForwardingConfig)dataGridConfigurations.SelectedRows[0].DataBoundItem;
                ConnectTunnel(selectedConfig);
            }
        }

        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            if (dataGridConfigurations.SelectedRows.Count > 0)
            {
                var selectedConfig = (PortForwardingConfig)dataGridConfigurations.SelectedRows[0].DataBoundItem;
                DisconnectTunnel(selectedConfig);
            }
        }

        private void btnConnectAll_Click(object sender, EventArgs e)
        {
            ConnectAllTunnels();
        }

        private void btnDisconnectAll_Click(object sender, EventArgs e)
        {
            DisconnectAllTunnels();
        }

        private void ConnectAllTunnels()
        {
            foreach (var config in _configurations)
            {
                if (!config.IsActive)
                {
                    ConnectTunnel(config);
                }
            }
        }

        private void DisconnectAllTunnels()
        {
            foreach (var config in _configurations.ToList())
            {
                if (config.IsActive)
                {
                    DisconnectTunnel(config);
                }
            }
        }

        private void ConnectTunnel(PortForwardingConfig config)
        {
            try
            {
                // If encryption is enabled, encrypt the remote data
                string remoteHost = config.RemoteHost;
                int remotePort = config.RemotePort;

                if (config.UseEncryption)
                {
                    remoteHost = _encryptionManager.EncryptText(remoteHost);
                    // For demonstration, we don't actually encrypt the port
                }

                // Base SSH command
                string sshCommand = $"ssh -L {config.LocalPort}:{remoteHost}:{remotePort} {config.Username}@{config.ServerHost} -p {config.ServerPort} -N";

                // Create the process FIRST
                Process sshProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/c {sshCommand}",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    }
                };

                // Handle different authentication methods
                switch (config.AuthenticationMethod)
                {
                    case AuthMethod.PrivateKey:
                        if (!string.IsNullOrEmpty(config.PrivateKeyPath) && File.Exists(config.PrivateKeyPath))
                        {
                            sshCommand += $" -i \"{config.PrivateKeyPath}\"";
                        }
                        break;

                    case AuthMethod.Certificate:
                        // Get path to the certificate
                        string appDataPath = Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                            "SSHTunnelClient");
                        string certPath = Path.Combine(appDataPath, "cert_store", $"{config.CertificateName}.pfx");

                        if (File.Exists(certPath))
                        {
                            sshCommand += $" -o \"PKCS11Provider={certPath}\"";
                        }
                        break;

                    case AuthMethod.KeyboardInteractive:
                        sshCommand += " -o \"PreferredAuthentications=keyboard-interactive\"";
                        break;

                    // Password authentication is the default with OpenSSH
                    case AuthMethod.YubiKey:
                        LogMessage($"Using YubiKey authentication for {config.Name}");

                        // Create YubiKey manager
                        YubiKeyManager yubiKeyManager = new YubiKeyManager();

                        // Check if YubiKey is present
                        if (!yubiKeyManager.IsYubiKeyPresent())
                        {
                            MessageBox.Show("YubiKey not detected. Please insert your YubiKey and try again.",
                                "YubiKey Required", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }

                        // Use the appropriate YubiKey mode
                        switch (config.YubiKeyMode)
                        {
                            case YubiKeyMode.PIV:
                                // PIV mode uses the PKCS#11 provider
                                if (!string.IsNullOrEmpty(config.YubiKeyProvider))
                                {
                                    sshCommand += $" -o \"PKCS11Provider={config.YubiKeyProvider}\"";
                                }

                                // Add PIN handling if needed
                                if (!string.IsNullOrEmpty(config.YubiKeyPIN))
                                {
                                    // Create a temporary PIN file (in a real app, you'd use ssh-askpass or a more secure method)
                                    string tempPinFile = Path.Combine(Path.GetTempPath(), $"yubikey_pin_{Guid.NewGuid()}.txt");
                                    File.WriteAllText(tempPinFile, config.YubiKeyPIN);

                                    // Use PIN file
                                    sshCommand = sshCommand.Replace("-N", $"-N < \"{tempPinFile}\"");

                                    // Schedule cleanup of the PIN file
                                    Timer pinCleanupTimer = new Timer();
                                    pinCleanupTimer.Interval = 5000; // 5 seconds
                                    pinCleanupTimer.Tick += (s, e) =>
                                    {
                                        try
                                        {
                                            if (File.Exists(tempPinFile))
                                                File.Delete(tempPinFile);

                                            ((Timer)s).Stop();
                                            ((Timer)s).Dispose();
                                        }
                                        catch { /* Ignore cleanup errors */ }
                                    };
                                    pinCleanupTimer.Start();
                                }
                                break;

                            case YubiKeyMode.OATH:
                                // For OATH mode, we need to get the current TOTP code
                                Task.Run(async () =>
                                {
                                    try
                                    {
                                        LogMessage("Waiting for YubiKey OATH TOTP code...");

                                        // Get YubiKey OATH code (this will prompt the user to touch the YubiKey if needed)
                                        string response = await yubiKeyManager.GetChallengeResponseAsync("");

                                        if (!string.IsNullOrEmpty(response))
                                        {
                                            // Send the OATH code to the SSH process stdin when prompted
                                            sshProcess.OutputDataReceived += (sender, e) =>
                                            {
                                                if (e.Data != null && e.Data.Contains("Verification code"))
                                                {
                                                    sshProcess.StandardInput.WriteLine(response);
                                                    sshProcess.StandardInput.Flush();
                                                    LogMessage("Sent YubiKey OATH TOTP code");
                                                }
                                            };
                                        }
                                        else
                                        {
                                            LogMessage("Failed to get YubiKey OATH TOTP code");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        LogMessage($"YubiKey OATH error: {ex.Message}");
                                    }
                                });
                                break;

                            case YubiKeyMode.ChallengeResponse:
                                // For Challenge-Response mode, we need to set up keyboard-interactive authentication
                                sshCommand += " -o \"PreferredAuthentications=keyboard-interactive\"";

                                // Handle challenge-response in the output handler
                                sshProcess.OutputDataReceived += async (sender, e) =>
                                {
                                    if (e.Data != null && e.Data.Contains("challenge"))
                                    {
                                        try
                                        {
                                            // Extract challenge (this is simplified - real implementations would parse properly)
                                            string challenge = "challenge";

                                            // Get response from YubiKey
                                            string response = await yubiKeyManager.GetChallengeResponseAsync(challenge);

                                            if (!string.IsNullOrEmpty(response))
                                            {
                                                sshProcess.StandardInput.WriteLine(response);
                                                sshProcess.StandardInput.Flush();
                                                LogMessage("Sent YubiKey challenge-response");
                                            }
                                            else
                                            {
                                                LogMessage("Failed to get YubiKey challenge-response");
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            LogMessage($"YubiKey challenge-response error: {ex.Message}");
                                        }
                                    }
                                };
                                break;
                        }
                        break;

                    case AuthMethod.YubiKeyOTP:
                        LogMessage($"Using YubiKey OTP authentication for {config.Name}");

                        // For YubiKey OTP, we need to handle keyboard-interactive authentication
                        sshCommand += " -o \"PreferredAuthentications=keyboard-interactive\"";

                        // Set up a handler to prompt for YubiKey touch when needed
                        sshProcess.OutputDataReceived += (sender, e) =>
                        {
                            if (e.Data != null && (e.Data.Contains("password") || e.Data.Contains("code")))
                            {
                                // Show message to prompt user to touch YubiKey
                                MessageBox.Show("Please touch your YubiKey button to generate an OTP code.",
                                    "YubiKey Touch Required", MessageBoxButtons.OK, MessageBoxIcon.Information);

                                // The YubiKey will send the OTP as keystrokes automatically
                                // No need to manually enter anything in this case
                            }
                        };
                        break;
                }

                // Add connection timeout
                if (config.ConnectionTimeout > 0)
                {
                    sshCommand += $" -o \"ConnectTimeout={config.ConnectionTimeout}\"";
                }

                // Add keep-alive interval
                if (config.KeepAliveInterval > 0)
                {
                    sshCommand += $" -o \"ServerAliveInterval={config.KeepAliveInterval}\"";
                }

                // Add compression if enabled
                if (config.EnableCompression)
                {
                    sshCommand += " -C";
                }

                // Add any custom SSH options
                if (!string.IsNullOrEmpty(config.SSHOptions))
                {
                    sshCommand += $" {config.SSHOptions}";
                }

                // Handle TOTP if enabled
                if (config.UseTOTP && !string.IsNullOrEmpty(config.TOTPSecretKey))
                {
                    // Generate TOTP code
                    AuthenticationManager authManager = new AuthenticationManager();
                    string totpCode = authManager.GenerateTOTPCode(config.TOTPSecretKey);

                    if (!string.IsNullOrEmpty(totpCode))
                    {
                        // For handling TOTP, we need to use plink with SSH_ASKPASS or similar
                        // This is a simplified approach - in production, you'd need a more robust solution

                        // Create a temporary file with the TOTP code
                        string tempTotpFile = Path.Combine(Path.GetTempPath(), $"totp_{Guid.NewGuid()}.txt");
                        File.WriteAllText(tempTotpFile, totpCode);

                        // Modify command to use the TOTP file
                        sshCommand = sshCommand.Replace("-N", $"-N < \"{tempTotpFile}\"");

                        // Schedule cleanup of the temporary file
                        Timer cleanupTimer = new Timer();
                        cleanupTimer.Interval = 30000; // 30 seconds
                        cleanupTimer.Tick += (sender, e) =>
                        {
                            try
                            {
                                if (File.Exists(tempTotpFile))
                                    File.Delete(tempTotpFile);

                                ((Timer)sender).Stop();
                                ((Timer)sender).Dispose();
                            }
                            catch { /* Ignore cleanup errors */ }
                        };
                        cleanupTimer.Start();
                    }
                }

                // For password authentication, we need to handle stdin
                if (config.AuthenticationMethod == AuthMethod.Password && !string.IsNullOrEmpty(config.Password))
                {
                    sshProcess.OutputDataReceived += (sender, e) =>
                    {
                        if (e.Data != null && e.Data.Contains("password"))
                        {
                            sshProcess.StandardInput.WriteLine(config.Password);
                            sshProcess.StandardInput.Flush();
                        }
                    };
                }

                sshProcess.Start();
                sshProcess.BeginOutputReadLine();
                sshProcess.BeginErrorReadLine();

                // Store the process and update the configuration
                _activeTunnels[config.LocalPort] = sshProcess;
                config.IsActive = true;

                // Refresh the UI
                dataGridConfigurations.Refresh();

                LogMessage($"Connected tunnel: {config.Name} (Local:{config.LocalPort} -> {config.RemoteHost}:{config.RemotePort})");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error connecting tunnel: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                LogMessage($"Error connecting tunnel: {ex.Message}");
            }
        }

        private void DisconnectTunnel(PortForwardingConfig config)
        {
            try
            {
                if (_activeTunnels.TryGetValue(config.LocalPort, out Process sshProcess))
                {
                    // Kill the SSH process
                    if (!sshProcess.HasExited)
                    {
                        sshProcess.Kill();
                    }

                    _activeTunnels.Remove(config.LocalPort);
                    config.IsActive = false;

                    // Refresh the UI
                    dataGridConfigurations.Refresh();

                    LogMessage($"Disconnected tunnel: {config.Name}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error disconnecting tunnel: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                LogMessage($"Error disconnecting tunnel: {ex.Message}");
            }
        }

        private void chkRunAtStartup_CheckedChanged(object sender, EventArgs e)
        {
            if (chkRunAtStartup.Checked)
            {
                _startupManager.SetRunAtStartup();
            }
            else
            {
                _startupManager.RemoveRunAtStartup();
            }
        }

        private void chkAutoConnectOnStartup_CheckedChanged(object sender, EventArgs e)
        {
            _settingsManager.SetAutoConnectOnStartup(chkAutoConnectOnStartup.Checked);
        }

        private void LogMessage(string message)
        {
            txtLog.AppendText($"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}] {message}{Environment.NewLine}");
            // Scroll to the end
            txtLog.SelectionStart = txtLog.Text.Length;
            txtLog.ScrollToCaret();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Disconnect all tunnels when the application is closing
            DisconnectAllTunnels();
        }

        private void InitializeComponent()
        {
            this.dataGridConfigurations = new System.Windows.Forms.DataGridView();
            this.btnAdd = new System.Windows.Forms.Button();
            this.btnEdit = new System.Windows.Forms.Button();
            this.btnRemove = new System.Windows.Forms.Button();
            this.btnConnect = new System.Windows.Forms.Button();
            this.btnDisconnect = new System.Windows.Forms.Button();
            this.btnConnectAll = new System.Windows.Forms.Button();
            this.btnDisconnectAll = new System.Windows.Forms.Button();
            this.chkRunAtStartup = new System.Windows.Forms.CheckBox();
            this.chkAutoConnectOnStartup = new System.Windows.Forms.CheckBox();
            this.txtLog = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridConfigurations)).BeginInit();
            this.SuspendLayout();
            // 
            // dataGridConfigurations
            // 
            this.dataGridConfigurations.AllowUserToAddRows = false;
            this.dataGridConfigurations.AllowUserToDeleteRows = false;
            this.dataGridConfigurations.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridConfigurations.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridConfigurations.Location = new System.Drawing.Point(12, 12);
            this.dataGridConfigurations.MultiSelect = false;
            this.dataGridConfigurations.Name = "dataGridConfigurations";
            this.dataGridConfigurations.ReadOnly = true;
            this.dataGridConfigurations.RowHeadersWidth = 62;
            this.dataGridConfigurations.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dataGridConfigurations.Size = new System.Drawing.Size(650, 200);
            this.dataGridConfigurations.TabIndex = 0;
            // 
            // btnAdd
            // 
            this.btnAdd.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnAdd.Location = new System.Drawing.Point(12, 218);
            this.btnAdd.Name = "btnAdd";
            this.btnAdd.Size = new System.Drawing.Size(75, 23);
            this.btnAdd.TabIndex = 1;
            this.btnAdd.Text = "Add";
            this.btnAdd.UseVisualStyleBackColor = true;
            this.btnAdd.Click += new System.EventHandler(this.btnAdd_Click);
            // 
            // btnEdit
            // 
            this.btnEdit.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnEdit.Location = new System.Drawing.Point(93, 218);
            this.btnEdit.Name = "btnEdit";
            this.btnEdit.Size = new System.Drawing.Size(75, 23);
            this.btnEdit.TabIndex = 2;
            this.btnEdit.Text = "Edit";
            this.btnEdit.UseVisualStyleBackColor = true;
            this.btnEdit.Click += new System.EventHandler(this.btnEdit_Click);
            // 
            // btnRemove
            // 
            this.btnRemove.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnRemove.Location = new System.Drawing.Point(174, 218);
            this.btnRemove.Name = "btnRemove";
            this.btnRemove.Size = new System.Drawing.Size(75, 23);
            this.btnRemove.TabIndex = 3;
            this.btnRemove.Text = "Remove";
            this.btnRemove.UseVisualStyleBackColor = true;
            this.btnRemove.Click += new System.EventHandler(this.btnRemove_Click);
            // 
            // btnConnect
            // 
            this.btnConnect.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnConnect.Location = new System.Drawing.Point(406, 218);
            this.btnConnect.Name = "btnConnect";
            this.btnConnect.Size = new System.Drawing.Size(75, 23);
            this.btnConnect.TabIndex = 4;
            this.btnConnect.Text = "Connect";
            this.btnConnect.UseVisualStyleBackColor = true;
            this.btnConnect.Click += new System.EventHandler(this.btnConnect_Click);
            // 
            // btnDisconnect
            // 
            this.btnDisconnect.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnDisconnect.Location = new System.Drawing.Point(487, 218);
            this.btnDisconnect.Name = "btnDisconnect";
            this.btnDisconnect.Size = new System.Drawing.Size(75, 23);
            this.btnDisconnect.TabIndex = 5;
            this.btnDisconnect.Text = "Disconnect";
            this.btnDisconnect.UseVisualStyleBackColor = true;
            this.btnDisconnect.Click += new System.EventHandler(this.btnDisconnect_Click);
            // 
            // btnConnectAll
            // 
            this.btnConnectAll.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnConnectAll.Location = new System.Drawing.Point(406, 247);
            this.btnConnectAll.Name = "btnConnectAll";
            this.btnConnectAll.Size = new System.Drawing.Size(75, 23);
            this.btnConnectAll.TabIndex = 6;
            this.btnConnectAll.Text = "Connect All";
            this.btnConnectAll.UseVisualStyleBackColor = true;
            this.btnConnectAll.Click += new System.EventHandler(this.btnConnectAll_Click);
            // 
            // btnDisconnectAll
            // 
            this.btnDisconnectAll.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnDisconnectAll.Location = new System.Drawing.Point(487, 247);
            this.btnDisconnectAll.Name = "btnDisconnectAll";
            this.btnDisconnectAll.Size = new System.Drawing.Size(75, 23);
            this.btnDisconnectAll.TabIndex = 7;
            this.btnDisconnectAll.Text = "Disconnect All";
            this.btnDisconnectAll.UseVisualStyleBackColor = true;
            this.btnDisconnectAll.Click += new System.EventHandler(this.btnDisconnectAll_Click);
            // 
            // chkRunAtStartup
            // 
            this.chkRunAtStartup.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.chkRunAtStartup.AutoSize = true;
            this.chkRunAtStartup.Location = new System.Drawing.Point(12, 240);
            this.chkRunAtStartup.Name = "chkRunAtStartup";
            this.chkRunAtStartup.Size = new System.Drawing.Size(140, 24);
            this.chkRunAtStartup.TabIndex = 8;
            this.chkRunAtStartup.Text = "Run at Startup";
            this.chkRunAtStartup.UseVisualStyleBackColor = true;
            this.chkRunAtStartup.CheckedChanged += new System.EventHandler(this.chkRunAtStartup_CheckedChanged);
            // 
            // chkAutoConnectOnStartup
            // 
            this.chkAutoConnectOnStartup.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.chkAutoConnectOnStartup.AutoSize = true;
            this.chkAutoConnectOnStartup.Location = new System.Drawing.Point(116, 240);
            this.chkAutoConnectOnStartup.Name = "chkAutoConnectOnStartup";
            this.chkAutoConnectOnStartup.Size = new System.Drawing.Size(212, 24);
            this.chkAutoConnectOnStartup.TabIndex = 9;
            this.chkAutoConnectOnStartup.Text = "Auto Connect on Startup";
            this.chkAutoConnectOnStartup.UseVisualStyleBackColor = true;
            this.chkAutoConnectOnStartup.CheckedChanged += new System.EventHandler(this.chkAutoConnectOnStartup_CheckedChanged);
            // 
            // txtLog
            // 
            this.txtLog.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtLog.Location = new System.Drawing.Point(12, 295);
            this.txtLog.Multiline = true;
            this.txtLog.Name = "txtLog";
            this.txtLog.ReadOnly = true;
            this.txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtLog.Size = new System.Drawing.Size(650, 120);
            this.txtLog.TabIndex = 10;
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 279);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(40, 20);
            this.label1.TabIndex = 11;
            this.label1.Text = "Log:";
            // 
            // MainForm
            // 
            this.ClientSize = new System.Drawing.Size(674, 427);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.txtLog);
            this.Controls.Add(this.chkAutoConnectOnStartup);
            this.Controls.Add(this.chkRunAtStartup);
            this.Controls.Add(this.btnDisconnectAll);
            this.Controls.Add(this.btnConnectAll);
            this.Controls.Add(this.btnDisconnect);
            this.Controls.Add(this.btnConnect);
            this.Controls.Add(this.btnRemove);
            this.Controls.Add(this.btnEdit);
            this.Controls.Add(this.btnAdd);
            this.Controls.Add(this.dataGridConfigurations);
            this.MinimumSize = new System.Drawing.Size(690, 466);
            this.Name = "MainForm";
            this.Text = "SSH Tunnel Client";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.Load += new System.EventHandler(this.MainForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridConfigurations)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private System.Windows.Forms.DataGridView dataGridConfigurations;
        private System.Windows.Forms.Button btnAdd;
        private System.Windows.Forms.Button btnEdit;
        private System.Windows.Forms.Button btnRemove;
        private System.Windows.Forms.Button btnConnect;
        private System.Windows.Forms.Button btnDisconnect;
        private System.Windows.Forms.Button btnConnectAll;
        private System.Windows.Forms.Button btnDisconnectAll;
        private System.Windows.Forms.CheckBox chkRunAtStartup;
        private System.Windows.Forms.CheckBox chkAutoConnectOnStartup;
        private System.Windows.Forms.TextBox txtLog;
        private System.Windows.Forms.Label label1;

        private void MainForm_Load(object sender, EventArgs e)
        {

        }
    }
}