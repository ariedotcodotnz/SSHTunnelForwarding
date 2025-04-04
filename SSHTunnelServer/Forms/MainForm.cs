using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;

namespace SSHTunnelServer
{
    public partial class MainForm : Form
    {
        private BindingList<ServerConfig> _configurations;
        private SettingsManager _settingsManager;
        private StartupManager _startupManager;
        private Dictionary<int, Process> _activeListeners;
        private EncryptionManager _encryptionManager;

        public MainForm()
        {
            InitializeComponent();

            _settingsManager = new SettingsManager();
            _startupManager = new StartupManager("SSHTunnelServer");
            _activeListeners = new Dictionary<int, Process>();
            _encryptionManager = new EncryptionManager();

            LoadConfigurations();
            InitializeUI();

            // Auto-start if set in settings
            if (_settingsManager.GetAutoStartOnStartup())
            {
                StartAllListeners();
            }
        }

        private void InitializeUI()
        {
            // Set up the DataGridView for server configurations
            dataGridConfigurations.DataSource = _configurations;
            dataGridConfigurations.AutoGenerateColumns = false;
            dataGridConfigurations.Columns.Clear();

            // Add columns to the DataGridView
            dataGridConfigurations.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Name",
                HeaderText = "Name",
                DataPropertyName = "Name",
                Width = 100
            });

            dataGridConfigurations.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ListenPort",
                HeaderText = "Listen Port",
                DataPropertyName = "ListenPort",
                Width = 80
            });

            dataGridConfigurations.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "AllowedClients",
                HeaderText = "Allowed Clients",
                DataPropertyName = "AllowedClients",
                Width = 120
            });

            dataGridConfigurations.Columns.Add(new DataGridViewCheckBoxColumn
            {
                Name = "UseEncryption",
                HeaderText = "Decrypt",
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
            chkAutoStartOnStartup.Checked = _settingsManager.GetAutoStartOnStartup();
        }

        private void LoadConfigurations()
        {
            _configurations = new BindingList<ServerConfig>(_settingsManager.LoadConfigurations());
            if (_configurations.Count == 0)
            {
                // Add a default configuration if none exists
                _configurations.Add(new ServerConfig
                {
                    Name = "Default",
                    ListenPort = 22,
                    AllowedClients = "*",
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
            using (var configForm = new ServerConfigForm(null))
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
                var selectedConfig = (ServerConfig)dataGridConfigurations.SelectedRows[0].DataBoundItem;
                using (var configForm = new ServerConfigForm(selectedConfig))
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
                var selectedConfig = (ServerConfig)dataGridConfigurations.SelectedRows[0].DataBoundItem;

                // Stop the listener if it's active
                if (selectedConfig.IsActive)
                {
                    StopListener(selectedConfig);
                }

                _configurations.Remove(selectedConfig);
                SaveConfigurations();
            }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            if (dataGridConfigurations.SelectedRows.Count > 0)
            {
                var selectedConfig = (ServerConfig)dataGridConfigurations.SelectedRows[0].DataBoundItem;
                StartListener(selectedConfig);
            }
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            if (dataGridConfigurations.SelectedRows.Count > 0)
            {
                var selectedConfig = (ServerConfig)dataGridConfigurations.SelectedRows[0].DataBoundItem;
                StopListener(selectedConfig);
            }
        }

        private void btnStartAll_Click(object sender, EventArgs e)
        {
            StartAllListeners();
        }

        private void btnStopAll_Click(object sender, EventArgs e)
        {
            StopAllListeners();
        }

        private void StartAllListeners()
        {
            foreach (var config in _configurations)
            {
                if (!config.IsActive)
                {
                    StartListener(config);
                }
            }
        }

        private void StopAllListeners()
        {
            foreach (var config in _configurations.ToList())
            {
                if (config.IsActive)
                {
                    StopListener(config);
                }
            }
        }

        private void StartListener(ServerConfig config)
        {
            try
            {
                // Check if port is already in use
                if (IsPortInUse(config.ListenPort))
                {
                    MessageBox.Show($"Port {config.ListenPort} is already in use.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // For Windows, we'll use sshd_config to set up the SSH server
                string sshdConfigPath = CreateSshdConfig(config);

                // Start the SSH server process
                Process sshProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "sshd.exe",
                        Arguments = $"-f \"{sshdConfigPath}\" -D",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    }
                };

                sshProcess.OutputDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        LogMessage(e.Data);
                    }
                };

                sshProcess.ErrorDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        LogMessage($"ERROR: {e.Data}");
                    }
                };

                sshProcess.Start();
                sshProcess.BeginOutputReadLine();
                sshProcess.BeginErrorReadLine();

                // Store the process and update the configuration
                _activeListeners[config.ListenPort] = sshProcess;
                config.IsActive = true;

                // Refresh the UI
                dataGridConfigurations.Refresh();

                LogMessage($"Started listener: {config.Name} on port {config.ListenPort}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error starting listener: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                LogMessage($"Error starting listener: {ex.Message}");
            }
        }

        private bool IsPortInUse(int port)
        {
            bool inUse = false;

            try
            {
                TcpListener tcpListener = new TcpListener(IPAddress.Any, port);
                tcpListener.Start();
                tcpListener.Stop();
            }
            catch
            {
                inUse = true;
            }

            return inUse;
        }

        private string CreateSshdConfig(ServerConfig config)
        {
            string appDataPath = _settingsManager.GetAppDataPath();
            string configDir = Path.Combine(appDataPath, "configs");

            if (!Directory.Exists(configDir))
            {
                Directory.CreateDirectory(configDir);
            }

            string configPath = Path.Combine(configDir, $"sshd_config_{config.ListenPort}");

            // Create sshd_config content
            List<string> configLines = new List<string>
            {
                $"Port {config.ListenPort}",
                "Protocol 2",
                $"ListenAddress 0.0.0.0:{config.ListenPort}",
                "HostKey /etc/ssh/ssh_host_rsa_key",
                "HostKey /etc/ssh/ssh_host_ecdsa_key",
                "HostKey /etc/ssh/ssh_host_ed25519_key",
                "SyslogFacility AUTH",
                "LogLevel INFO",
                "LoginGraceTime 2m",
                "PermitRootLogin no",
                "StrictModes yes",
                "MaxAuthTries 6",
                "MaxSessions 10",
                "PubkeyAuthentication yes",
                "AuthorizedKeysFile .ssh/authorized_keys",
                "PasswordAuthentication yes",
                "PermitEmptyPasswords no",
                "ChallengeResponseAuthentication no",
                "UsePAM yes",
                "X11Forwarding yes",
                "PrintMotd no",
                "AcceptEnv LANG LC_*",
                "Subsystem sftp /usr/lib/openssh/sftp-server",
                "GatewayPorts yes",  // Important for port forwarding
                "PermitTunnel yes",  // Enable tunneling
                "AllowTcpForwarding yes",  // Allow TCP forwarding
                "AllowStreamLocalForwarding yes",
                "X11UseLocalhost yes"
            };

            // Add allowed clients configuration
            if (config.AllowedClients != "*")
            {
                string[] allowedIPs = config.AllowedClients.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string ip in allowedIPs)
                {
                    configLines.Add($"AllowUsers *@{ip.Trim()}");
                }
            }

            // Write the config file
            File.WriteAllLines(configPath, configLines);

            return configPath;
        }

        private void StopListener(ServerConfig config)
        {
            try
            {
                if (_activeListeners.TryGetValue(config.ListenPort, out Process sshProcess))
                {
                    // Kill the SSH process
                    if (!sshProcess.HasExited)
                    {
                        sshProcess.Kill();
                    }

                    _activeListeners.Remove(config.ListenPort);
                    config.IsActive = false;

                    // Refresh the UI
                    dataGridConfigurations.Refresh();

                    LogMessage($"Stopped listener: {config.Name} on port {config.ListenPort}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error stopping listener: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                LogMessage($"Error stopping listener: {ex.Message}");
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

        private void chkAutoStartOnStartup_CheckedChanged(object sender, EventArgs e)
        {
            _settingsManager.SetAutoStartOnStartup(chkAutoStartOnStartup.Checked);
        }

        private void LogMessage(string message)
        {
            if (txtLog.InvokeRequired)
            {
                txtLog.Invoke(new Action<string>(LogMessage), message);
                return;
            }

            txtLog.AppendText($"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}] {message}{Environment.NewLine}");
            // Scroll to the end
            txtLog.SelectionStart = txtLog.Text.Length;
            txtLog.ScrollToCaret();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Stop all listeners when the application is closing
            StopAllListeners();
        }

        private void InitializeComponent()
        {
            this.dataGridConfigurations = new System.Windows.Forms.DataGridView();
            this.btnAdd = new System.Windows.Forms.Button();
            this.btnEdit = new System.Windows.Forms.Button();
            this.btnRemove = new System.Windows.Forms.Button();
            this.btnStart = new System.Windows.Forms.Button();
            this.btnStop = new System.Windows.Forms.Button();
            this.btnStartAll = new System.Windows.Forms.Button();
            this.btnStopAll = new System.Windows.Forms.Button();
            this.chkRunAtStartup = new System.Windows.Forms.CheckBox();
            this.chkAutoStartOnStartup = new System.Windows.Forms.CheckBox();
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
            // btnStart
            // 
            this.btnStart.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnStart.Location = new System.Drawing.Point(406, 218);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(75, 23);
            this.btnStart.TabIndex = 4;
            this.btnStart.Text = "Start";
            this.btnStart.UseVisualStyleBackColor = true;
            this.btnStart.Click += new System.EventHandler(this.btnStart_Click);
            // 
            // btnStop
            // 
            this.btnStop.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnStop.Location = new System.Drawing.Point(487, 218);
            this.btnStop.Name = "btnStop";
            this.btnStop.Size = new System.Drawing.Size(75, 23);
            this.btnStop.TabIndex = 5;
            this.btnStop.Text = "Stop";
            this.btnStop.UseVisualStyleBackColor = true;
            this.btnStop.Click += new System.EventHandler(this.btnStop_Click);
            // 
            // btnStartAll
            // 
            this.btnStartAll.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnStartAll.Location = new System.Drawing.Point(406, 247);
            this.btnStartAll.Name = "btnStartAll";
            this.btnStartAll.Size = new System.Drawing.Size(75, 23);
            this.btnStartAll.TabIndex = 6;
            this.btnStartAll.Text = "Start All";
            this.btnStartAll.UseVisualStyleBackColor = true;
            this.btnStartAll.Click += new System.EventHandler(this.btnStartAll_Click);
            // 
            // btnStopAll
            // 
            this.btnStopAll.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnStopAll.Location = new System.Drawing.Point(487, 247);
            this.btnStopAll.Name = "btnStopAll";
            this.btnStopAll.Size = new System.Drawing.Size(75, 23);
            this.btnStopAll.TabIndex = 7;
            this.btnStopAll.Text = "Stop All";
            this.btnStopAll.UseVisualStyleBackColor = true;
            this.btnStopAll.Click += new System.EventHandler(this.btnStopAll_Click);
            // 
            // chkRunAtStartup
            // 
            this.chkRunAtStartup.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.chkRunAtStartup.AutoSize = true;
            this.chkRunAtStartup.Location = new System.Drawing.Point(12, 247);
            this.chkRunAtStartup.Name = "chkRunAtStartup";
            this.chkRunAtStartup.Size = new System.Drawing.Size(98, 17);
            this.chkRunAtStartup.TabIndex = 8;
            this.chkRunAtStartup.Text = "Run at Startup";
            this.chkRunAtStartup.UseVisualStyleBackColor = true;
            this.chkRunAtStartup.CheckedChanged += new System.EventHandler(this.chkRunAtStartup_CheckedChanged);
            // 
            // chkAutoStartOnStartup
            // 
            this.chkAutoStartOnStartup.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.chkAutoStartOnStartup.AutoSize = true;
            this.chkAutoStartOnStartup.Location = new System.Drawing.Point(116, 247);
            this.chkAutoStartOnStartup.Name = "chkAutoStartOnStartup";
            this.chkAutoStartOnStartup.Size = new System.Drawing.Size(133, 17);
            this.chkAutoStartOnStartup.TabIndex = 9;
            this.chkAutoStartOnStartup.Text = "Auto Start on Startup";
            this.chkAutoStartOnStartup.UseVisualStyleBackColor = true;
            this.chkAutoStartOnStartup.CheckedChanged += new System.EventHandler(this.chkAutoStartOnStartup_CheckedChanged);
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
            this.label1.Size = new System.Drawing.Size(28, 13);
            this.label1.TabIndex = 11;
            this.label1.Text = "Log:";
            // 
            // MainForm
            // 
            this.ClientSize = new System.Drawing.Size(674, 427);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.txtLog);
            this.Controls.Add(this.chkAutoStartOnStartup);
            this.Controls.Add(this.chkRunAtStartup);
            this.Controls.Add(this.btnStopAll);
            this.Controls.Add(this.btnStartAll);
            this.Controls.Add(this.btnStop);
            this.Controls.Add(this.btnStart);
            this.Controls.Add(this.btnRemove);
            this.Controls.Add(this.btnEdit);
            this.Controls.Add(this.btnAdd);
            this.Controls.Add(this.dataGridConfigurations);
            this.MinimumSize = new System.Drawing.Size(690, 466);
            this.Name = "MainForm";
            this.Text = "SSH Tunnel Server";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridConfigurations)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.DataGridView dataGridConfigurations;
        private System.Windows.Forms.Button btnAdd;
        private System.Windows.Forms.Button btnEdit;
        private System.Windows.Forms.Button btnRemove;
        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.Button btnStop;
        private System.Windows.Forms.Button btnStartAll;
        private System.Windows.Forms.Button btnStopAll;
        private System.Windows.Forms.CheckBox chkRunAtStartup;
        private System.Windows.Forms.CheckBox chkAutoStartOnStartup;
        private System.Windows.Forms.TextBox txtLog;
        private System.Windows.Forms.Label label1;
    }
}