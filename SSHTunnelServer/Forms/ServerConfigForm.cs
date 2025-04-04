using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace SSHTunnelServer
{
    public partial class ServerConfigForm : Form
    {
        public ServerConfig Configuration { get; private set; }
        private ServerSecurityManager _securityManager;

        public ServerConfigForm(ServerConfig existingConfig)
        {
            InitializeComponent();
            _securityManager = new ServerSecurityManager();

            // Populate security level dropdown
            cmbSecurityLevel.Items.Add("Basic");
            cmbSecurityLevel.Items.Add("Standard");
            cmbSecurityLevel.Items.Add("High");

            // Populate log level dropdown
            cmbLogLevel.Items.Add("Error");
            cmbLogLevel.Items.Add("Info");
            cmbLogLevel.Items.Add("Verbose");
            cmbLogLevel.Items.Add("Debug");

            // Populate server key and certificate dropdowns
            PopulateKeyAndCertificateDropdowns();

            if (existingConfig != null)
            {
                // Edit existing configuration
                Configuration = new ServerConfig
                {
                    Name = existingConfig.Name,
                    ListenPort = existingConfig.ListenPort,
                    AllowedClients = existingConfig.AllowedClients,
                    UseEncryption = existingConfig.UseEncryption,
                    SecurityLevel = existingConfig.SecurityLevel,
                    PasswordAuthentication = existingConfig.PasswordAuthentication,
                    PubkeyAuthentication = existingConfig.PubkeyAuthentication,
                    CertificateAuthentication = existingConfig.CertificateAuthentication,
                    KeyboardInteractiveAuth = existingConfig.KeyboardInteractiveAuth,
                    YubikeyAuthentication = existingConfig.YubikeyAuthentication,
                    TwoFactorAuthentication = existingConfig.TwoFactorAuthentication,
                    MaxAuthTries = existingConfig.MaxAuthTries,
                    LoginGraceTime = existingConfig.LoginGraceTime,
                    PermitRootLogin = existingConfig.PermitRootLogin,
                    EnableYubiKey = existingConfig.EnableYubiKey,
                    YubiKeyAuthServer = existingConfig.YubiKeyAuthServer,
                    YubiKeyAPIKey = existingConfig.YubiKeyAPIKey,
                    YubiKeyClientID = existingConfig.YubiKeyClientID,
                    ServerKeyName = existingConfig.ServerKeyName,
                    ServerCertName = existingConfig.ServerCertName,
                    LogLevel = existingConfig.LogLevel,
                    EnableAuditLogging = existingConfig.EnableAuditLogging,
                    LogFilePath = existingConfig.LogFilePath,
                    IsActive = existingConfig.IsActive
                };
            }
            else
            {
                // Create new configuration
                Configuration = new ServerConfig
                {
                    Name = "New Configuration",
                    ListenPort = 22,
                    AllowedClients = "*",
                    UseEncryption = false,
                    SecurityLevel = SecurityLevel.Standard,
                    PasswordAuthentication = true,
                    PubkeyAuthentication = true,
                    CertificateAuthentication = false,
                    KeyboardInteractiveAuth = false,
                    YubikeyAuthentication = false,
                    TwoFactorAuthentication = false,
                    MaxAuthTries = 6,
                    LoginGraceTime = 120,
                    PermitRootLogin = false,
                    EnableYubiKey = false,
                    YubiKeyAuthServer = "api.yubico.com",
                    YubiKeyAPIKey = "",
                    YubiKeyClientID = "",
                    ServerKeyName = "",
                    ServerCertName = "",
                    LogLevel = LogLevel.Info,
                    EnableAuditLogging = false,
                    LogFilePath = "",
                    IsActive = false
                };
            }

            // Populate form fields
            txtName.Text = Configuration.Name;
            numListenPort.Value = Configuration.ListenPort;
            txtAllowedClients.Text = Configuration.AllowedClients;
            chkUseEncryption.Checked = Configuration.UseEncryption;

            // Security level
            cmbSecurityLevel.SelectedIndex = (int)Configuration.SecurityLevel;

            // Authentication settings
            chkPasswordAuth.Checked = Configuration.PasswordAuthentication;
            chkPublicKeyAuth.Checked = Configuration.PubkeyAuthentication;
            chkCertificateAuth.Checked = Configuration.CertificateAuthentication;
            chkKeyboardInteractiveAuth.Checked = Configuration.KeyboardInteractiveAuth;
            chkYubikeyAuth.Checked = Configuration.YubikeyAuthentication;
            chkTwoFactorAuth.Checked = Configuration.TwoFactorAuthentication;

            // Advanced settings
            numMaxAuthTries.Value = Configuration.MaxAuthTries;
            numLoginGraceTime.Value = Configuration.LoginGraceTime;
            chkPermitRootLogin.Checked = Configuration.PermitRootLogin;

            // YubiKey settings
            chkEnableYubiKey.Checked = Configuration.EnableYubiKey;
            txtYubiKeyServer.Text = Configuration.YubiKeyAuthServer;
            txtYubiKeyAPIKey.Text = Configuration.YubiKeyAPIKey;
            txtYubiKeyClientID.Text = Configuration.YubiKeyClientID;

            // Server key and certificate
            if (!string.IsNullOrEmpty(Configuration.ServerKeyName))
            {
                int keyIndex = cmbServerKey.FindStringExact(Configuration.ServerKeyName);
                if (keyIndex >= 0)
                    cmbServerKey.SelectedIndex = keyIndex;
            }

            if (!string.IsNullOrEmpty(Configuration.ServerCertName))
            {
                int certIndex = cmbServerCert.FindStringExact(Configuration.ServerCertName);
                if (certIndex >= 0)
                    cmbServerCert.SelectedIndex = certIndex;
            }

            // Logging settings
            cmbLogLevel.SelectedIndex = (int)Configuration.LogLevel;
            chkEnableAuditLogging.Checked = Configuration.EnableAuditLogging;
            txtLogFilePath.Text = Configuration.LogFilePath;

            // Update UI state based on current settings
            UpdateUIState();
        }

        private void PopulateKeyAndCertificateDropdowns()
        {
            // Clear existing items
            cmbServerKey.Items.Clear();
            cmbServerCert.Items.Clear();

            // Add empty option
            cmbServerKey.Items.Add("(None)");
            cmbServerCert.Items.Add("(None)");

            // Get available keys and certificates
            List<string> keys = _securityManager.GetAvailableServerKeys();
            List<string> certs = _securityManager.GetAvailableServerCertificates();

            // Add to dropdowns
            foreach (string key in keys)
            {
                cmbServerKey.Items.Add(key);
            }

            foreach (string cert in certs)
            {
                cmbServerCert.Items.Add(cert);
            }

            // Select first items
            cmbServerKey.SelectedIndex = 0;
            cmbServerCert.SelectedIndex = 0;
        }

        private void UpdateUIState()
        {
            // Enable/disable YubiKey settings based on YubiKey checkbox
            bool yubiKeyEnabled = chkEnableYubiKey.Checked;
            txtYubiKeyServer.Enabled = yubiKeyEnabled;
            txtYubiKeyAPIKey.Enabled = yubiKeyEnabled;
            txtYubiKeyClientID.Enabled = yubiKeyEnabled;
            btnConfigureYubiKey.Enabled = yubiKeyEnabled;
            chkYubikeyAuth.Enabled = yubiKeyEnabled;
        }

        private void cmbSecurityLevel_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Update security-related settings based on selected security level
            switch (cmbSecurityLevel.SelectedIndex)
            {
                case 0: // Basic
                    // Less secure but more compatible settings
                    break;
                case 1: // Standard
                    // Default balanced settings
                    break;
                case 2: // High
                    // Most secure settings
                    break;
            }
        }

        private void chkEnableYubiKey_CheckedChanged(object sender, EventArgs e)
        {
            UpdateUIState();
        }

        private void btnBrowseLogFile_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Filter = "Log Files|*.log|All Files|*.*";
                dialog.Title = "Select Log File";
                dialog.FileName = "ssh_audit.log";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    txtLogFilePath.Text = dialog.FileName;
                }
            }
        }

        private void btnGenerateServerKey_Click(object sender, EventArgs e)
        {
            string keyName = "server_key_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
            if (_securityManager.GenerateServerKeyPair(keyName))
            {
                MessageBox.Show($"Server key pair '{keyName}' generated successfully.", "Key Generated", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Refresh the key dropdown
                PopulateKeyAndCertificateDropdowns();

                // Select the newly generated key
                int index = cmbServerKey.FindStringExact(keyName);
                if (index >= 0)
                    cmbServerKey.SelectedIndex = index;
            }
        }

        private void btnGenerateServerCert_Click(object sender, EventArgs e)
        {
            string certName = "server_cert_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string subject = "CN=SSHTunnelServer, O=YourOrganization";

            if (_securityManager.GenerateServerCertificate(certName, subject))
            {
                MessageBox.Show($"Server certificate '{certName}' generated successfully.", "Certificate Generated", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Refresh the certificate dropdown
                PopulateKeyAndCertificateDropdowns();

                // Select the newly generated certificate
                int index = cmbServerCert.FindStringExact(certName);
                if (index >= 0)
                    cmbServerCert.SelectedIndex = index;
            }
        }

        private void btnConfigureYubiKey_Click(object sender, EventArgs e)
        {
            MessageBox.Show("YubiKey configuration will launch an external tool. Please make sure your YubiKey is connected.", "YubiKey Configuration", MessageBoxButtons.OK, MessageBoxIcon.Information);

            // Here you would typically launch an external YubiKey configuration tool
            // or show a custom YubiKey configuration dialog
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Please enter a name for the configuration.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Update configuration
            Configuration.Name = txtName.Text;
            Configuration.ListenPort = (int)numListenPort.Value;
            Configuration.AllowedClients = txtAllowedClients.Text;
            Configuration.UseEncryption = chkUseEncryption.Checked;

            // Security level
            Configuration.SecurityLevel = (SecurityLevel)cmbSecurityLevel.SelectedIndex;

            // Authentication settings
            Configuration.PasswordAuthentication = chkPasswordAuth.Checked;
            Configuration.PubkeyAuthentication = chkPublicKeyAuth.Checked;
            Configuration.CertificateAuthentication = chkCertificateAuth.Checked;
            Configuration.KeyboardInteractiveAuth = chkKeyboardInteractiveAuth.Checked;
            Configuration.YubikeyAuthentication = chkYubikeyAuth.Checked;
            Configuration.TwoFactorAuthentication = chkTwoFactorAuth.Checked;

            // Advanced settings
            Configuration.MaxAuthTries = (int)numMaxAuthTries.Value;
            Configuration.LoginGraceTime = (int)numLoginGraceTime.Value;
            Configuration.PermitRootLogin = chkPermitRootLogin.Checked;

            // YubiKey settings
            Configuration.EnableYubiKey = chkEnableYubiKey.Checked;
            if (Configuration.EnableYubiKey)
            {
                Configuration.YubiKeyAuthServer = txtYubiKeyServer.Text;
                Configuration.YubiKeyAPIKey = txtYubiKeyAPIKey.Text;
                Configuration.YubiKeyClientID = txtYubiKeyClientID.Text;
            }

            // Server key and certificate
            if (cmbServerKey.SelectedIndex > 0) // Skip "(None)" option
                Configuration.ServerKeyName = cmbServerKey.SelectedItem.ToString();
            else
                Configuration.ServerKeyName = "";

            if (cmbServerCert.SelectedIndex > 0) // Skip "(None)" option
                Configuration.ServerCertName = cmbServerCert.SelectedItem.ToString();
            else
                Configuration.ServerCertName = "";

            // Logging settings
            Configuration.LogLevel = (LogLevel)cmbLogLevel.SelectedIndex;
            Configuration.EnableAuditLogging = chkEnableAuditLogging.Checked;
            Configuration.LogFilePath = txtLogFilePath.Text;

            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void InitializeComponent()
        {
            this.lblName = new System.Windows.Forms.Label();
            this.txtName = new System.Windows.Forms.TextBox();
            this.lblListenPort = new System.Windows.Forms.Label();
            this.numListenPort = new System.Windows.Forms.NumericUpDown();
            this.lblAllowedClients = new System.Windows.Forms.Label();
            this.txtAllowedClients = new System.Windows.Forms.TextBox();
            this.lblAllowedClientsHelp = new System.Windows.Forms.Label();
            this.chkUseEncryption = new System.Windows.Forms.CheckBox();

            // Security Level controls
            this.lblSecurityLevel = new System.Windows.Forms.Label();
            this.cmbSecurityLevel = new System.Windows.Forms.ComboBox();

            // Authentication controls
            this.grpAuthentication = new System.Windows.Forms.GroupBox();
            this.chkPasswordAuth = new System.Windows.Forms.CheckBox();
            this.chkPublicKeyAuth = new System.Windows.Forms.CheckBox();
            this.chkCertificateAuth = new System.Windows.Forms.CheckBox();
            this.chkKeyboardInteractiveAuth = new System.Windows.Forms.CheckBox();
            this.chkYubikeyAuth = new System.Windows.Forms.CheckBox();
            this.chkTwoFactorAuth = new System.Windows.Forms.CheckBox();

            // Advanced auth settings
            this.lblMaxAuthTries = new System.Windows.Forms.Label();
            this.numMaxAuthTries = new System.Windows.Forms.NumericUpDown();
            this.lblLoginGraceTime = new System.Windows.Forms.Label();
            this.numLoginGraceTime = new System.Windows.Forms.NumericUpDown();
            this.chkPermitRootLogin = new System.Windows.Forms.CheckBox();

            // YubiKey settings
            this.grpYubiKey = new System.Windows.Forms.GroupBox();
            this.chkEnableYubiKey = new System.Windows.Forms.CheckBox();
            this.lblYubiKeyServer = new System.Windows.Forms.Label();
            this.txtYubiKeyServer = new System.Windows.Forms.TextBox();
            this.lblYubiKeyAPIKey = new System.Windows.Forms.Label();
            this.txtYubiKeyAPIKey = new System.Windows.Forms.TextBox();
            this.lblYubiKeyClientID = new System.Windows.Forms.Label();
            this.txtYubiKeyClientID = new System.Windows.Forms.TextBox();
            this.btnConfigureYubiKey = new System.Windows.Forms.Button();

            // Server key and certificate
            this.grpServerCrypto = new System.Windows.Forms.GroupBox();
            this.lblServerKey = new System.Windows.Forms.Label();
            this.cmbServerKey = new System.Windows.Forms.ComboBox();
            this.btnGenerateServerKey = new System.Windows.Forms.Button();
            this.lblServerCert = new System.Windows.Forms.Label();
            this.cmbServerCert = new System.Windows.Forms.ComboBox();
            this.btnGenerateServerCert = new System.Windows.Forms.Button();

            // Logging settings
            this.grpLogging = new System.Windows.Forms.GroupBox();
            this.lblLogLevel = new System.Windows.Forms.Label();
            this.cmbLogLevel = new System.Windows.Forms.ComboBox();
            this.chkEnableAuditLogging = new System.Windows.Forms.CheckBox();
            this.lblLogFilePath = new System.Windows.Forms.Label();
            this.txtLogFilePath = new System.Windows.Forms.TextBox();
            this.btnBrowseLogFile = new System.Windows.Forms.Button();

            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();

            ((System.ComponentModel.ISupportInitialize)(this.numListenPort)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numMaxAuthTries)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numLoginGraceTime)).BeginInit();
            this.grpAuthentication.SuspendLayout();
            this.grpYubiKey.SuspendLayout();
            this.grpServerCrypto.SuspendLayout();
            this.grpLogging.SuspendLayout();
            this.SuspendLayout();

            // lblName
            this.lblName.AutoSize = true;
            this.lblName.Location = new System.Drawing.Point(12, 15);
            this.lblName.Name = "lblName";
            this.lblName.Size = new System.Drawing.Size(38, 13);
            this.lblName.TabIndex = 0;
            this.lblName.Text = "Name:";

            // txtName
            this.txtName.Location = new System.Drawing.Point(120, 12);
            this.txtName.Name = "txtName";
            this.txtName.Size = new System.Drawing.Size(252, 20);
            this.txtName.TabIndex = 1;

            // lblListenPort
            this.lblListenPort.AutoSize = true;
            this.lblListenPort.Location = new System.Drawing.Point(12, 41);
            this.lblListenPort.Name = "lblListenPort";
            this.lblListenPort.Size = new System.Drawing.Size(63, 13);
            this.lblListenPort.TabIndex = 2;
            this.lblListenPort.Text = "Listen Port:";

            // numListenPort
            this.numListenPort.Location = new System.Drawing.Point(120, 39);
            this.numListenPort.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this.numListenPort.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numListenPort.Name = "numListenPort";
            this.numListenPort.Size = new System.Drawing.Size(80, 20);
            this.numListenPort.TabIndex = 3;
            this.numListenPort.Value = new decimal(new int[] {
            22,
            0,
            0,
            0});

            // lblAllowedClients
            this.lblAllowedClients.AutoSize = true;
            this.lblAllowedClients.Location = new System.Drawing.Point(12, 67);
            this.lblAllowedClients.Name = "lblAllowedClients";
            this.lblAllowedClients.Size = new System.Drawing.Size(85, 13);
            this.lblAllowedClients.TabIndex = 4;
            this.lblAllowedClients.Text = "Allowed Clients:";

            // txtAllowedClients
            this.txtAllowedClients.Location = new System.Drawing.Point(120, 64);
            this.txtAllowedClients.Name = "txtAllowedClients";
            this.txtAllowedClients.Size = new System.Drawing.Size(252, 20);
            this.txtAllowedClients.TabIndex = 5;

            // lblAllowedClientsHelp
            this.lblAllowedClientsHelp.Location = new System.Drawing.Point(117, 87);
            this.lblAllowedClientsHelp.Name = "lblAllowedClientsHelp";
            this.lblAllowedClientsHelp.Size = new System.Drawing.Size(255, 30);
            this.lblAllowedClientsHelp.TabIndex = 6;
            this.lblAllowedClientsHelp.Text = "IP addresses separated by commas. Use * to allow all clients.";

            // chkUseEncryption
            this.chkUseEncryption.AutoSize = true;
            this.chkUseEncryption.Location = new System.Drawing.Point(120, 120);
            this.chkUseEncryption.Name = "chkUseEncryption";
            this.chkUseEncryption.Size = new System.Drawing.Size(207, 17);
            this.chkUseEncryption.TabIndex = 7;
            this.chkUseEncryption.Text = "Handle Additional Encrypted Tunnels";
            this.chkUseEncryption.UseVisualStyleBackColor = true;

            // lblSecurityLevel
            this.lblSecurityLevel.AutoSize = true;
            this.lblSecurityLevel.Location = new System.Drawing.Point(12, 150);
            this.lblSecurityLevel.Name = "lblSecurityLevel";
            this.lblSecurityLevel.Size = new System.Drawing.Size(80, 13);
            this.lblSecurityLevel.TabIndex = 8;
            this.lblSecurityLevel.Text = "Security Level:";

            // cmbSecurityLevel
            this.cmbSecurityLevel.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbSecurityLevel.FormattingEnabled = true;
            this.cmbSecurityLevel.Location = new System.Drawing.Point(120, 147);
            this.cmbSecurityLevel.Name = "cmbSecurityLevel";
            this.cmbSecurityLevel.Size = new System.Drawing.Size(121, 21);
            this.cmbSecurityLevel.TabIndex = 9;
            this.cmbSecurityLevel.SelectedIndexChanged += new System.EventHandler(this.cmbSecurityLevel_SelectedIndexChanged);

            // Authentication group
            this.grpAuthentication.Location = new System.Drawing.Point(12, 180);
            this.grpAuthentication.Name = "grpAuthentication";
            this.grpAuthentication.Size = new System.Drawing.Size(360, 120);
            this.grpAuthentication.TabIndex = 10;
            this.grpAuthentication.TabStop = false;
            this.grpAuthentication.Text = "Authentication Methods";

            // Authentication checkboxes
            this.chkPasswordAuth.AutoSize = true;
            this.chkPasswordAuth.Location = new System.Drawing.Point(20, 20);
            this.chkPasswordAuth.Name = "chkPasswordAuth";
            this.chkPasswordAuth.Size = new System.Drawing.Size(120, 17);
            this.chkPasswordAuth.TabIndex = 0;
            this.chkPasswordAuth.Text = "Password Authentication";
            this.chkPasswordAuth.UseVisualStyleBackColor = true;

            this.chkPublicKeyAuth.AutoSize = true;
            this.chkPublicKeyAuth.Location = new System.Drawing.Point(20, 43);
            this.chkPublicKeyAuth.Name = "chkPublicKeyAuth";
            this.chkPublicKeyAuth.Size = new System.Drawing.Size(135, 17);
            this.chkPublicKeyAuth.TabIndex = 1;
            this.chkPublicKeyAuth.Text = "Public Key Authentication";
            this.chkPublicKeyAuth.UseVisualStyleBackColor = true;

            this.chkCertificateAuth.AutoSize = true;
            this.chkCertificateAuth.Location = new System.Drawing.Point(20, 66);
            this.chkCertificateAuth.Name = "chkCertificateAuth";
            this.chkCertificateAuth.Size = new System.Drawing.Size(141, 17);
            this.chkCertificateAuth.TabIndex = 2;
            this.chkCertificateAuth.Text = "Certificate Authentication";
            this.chkCertificateAuth.UseVisualStyleBackColor = true;

            this.chkKeyboardInteractiveAuth.AutoSize = true;
            this.chkKeyboardInteractiveAuth.Location = new System.Drawing.Point(20, 89);
            this.chkKeyboardInteractiveAuth.Name = "chkKeyboardInteractiveAuth";
            this.chkKeyboardInteractiveAuth.Size = new System.Drawing.Size(176, 17);
            this.chkKeyboardInteractiveAuth.TabIndex = 3;
            this.chkKeyboardInteractiveAuth.Text = "Keyboard Interactive Authentication";
            this.chkKeyboardInteractiveAuth.UseVisualStyleBackColor = true;

            this.chkYubikeyAuth.AutoSize = true;
            this.chkYubikeyAuth.Location = new System.Drawing.Point(200, 20);
            this.chkYubikeyAuth.Name = "chkYubikeyAuth";
            this.chkYubikeyAuth.Size = new System.Drawing.Size(131, 17);
            this.chkYubikeyAuth.TabIndex = 4;
            this.chkYubikeyAuth.Text = "YubiKey Authentication";
            this.chkYubikeyAuth.UseVisualStyleBackColor = true;

            this.chkTwoFactorAuth.AutoSize = true;
            this.chkTwoFactorAuth.Location = new System.Drawing.Point(200, 43);
            this.chkTwoFactorAuth.Name = "chkTwoFactorAuth";
            this.chkTwoFactorAuth.Size = new System.Drawing.Size(147, 17);
            this.chkTwoFactorAuth.TabIndex = 5;
            this.chkTwoFactorAuth.Text = "Two-Factor Authentication";
            this.chkTwoFactorAuth.UseVisualStyleBackColor = true;

            // Authentication settings
            this.lblMaxAuthTries.AutoSize = true;
            this.lblMaxAuthTries.Location = new System.Drawing.Point(12, 320);
            this.lblMaxAuthTries.Name = "lblMaxAuthTries";
            this.lblMaxAuthTries.Size = new System.Drawing.Size(83, 13);
            this.lblMaxAuthTries.TabIndex = 11;
            this.lblMaxAuthTries.Text = "Max Auth Tries:";

            this.numMaxAuthTries.Location = new System.Drawing.Point(120, 318);
            this.numMaxAuthTries.Maximum = new decimal(new int[] {
            20,
            0,
            0,
            0});
            this.numMaxAuthTries.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numMaxAuthTries.Name = "numMaxAuthTries";
            this.numMaxAuthTries.Size = new System.Drawing.Size(60, 20);
            this.numMaxAuthTries.TabIndex = 12;
            this.numMaxAuthTries.Value = new decimal(new int[] {
            6,
            0,
            0,
            0});

            this.lblLoginGraceTime.AutoSize = true;
            this.lblLoginGraceTime.Location = new System.Drawing.Point(12, 346);
            this.lblLoginGraceTime.Name = "lblLoginGraceTime";
            this.lblLoginGraceTime.Size = new System.Drawing.Size(101, 13);
            this.lblLoginGraceTime.TabIndex = 13;
            this.lblLoginGraceTime.Text = "Login Grace Time (s):";

            this.numLoginGraceTime.Location = new System.Drawing.Point(120, 344);
            this.numLoginGraceTime.Maximum = new decimal(new int[] {
            600,
            0,
            0,
            0});
            this.numLoginGraceTime.Minimum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.numLoginGraceTime.Name = "numLoginGraceTime";
            this.numLoginGraceTime.Size = new System.Drawing.Size(60, 20);
            this.numLoginGraceTime.TabIndex = 14;
            this.numLoginGraceTime.Value = new decimal(new int[] {
            120,
            0,
            0,
            0});

            this.chkPermitRootLogin.AutoSize = true;
            this.chkPermitRootLogin.Location = new System.Drawing.Point(120, 370);
            this.chkPermitRootLogin.Name = "chkPermitRootLogin";
            this.chkPermitRootLogin.Size = new System.Drawing.Size(105, 17);
            this.chkPermitRootLogin.TabIndex = 15;
            this.chkPermitRootLogin.Text = "Permit Root Login";
            this.chkPermitRootLogin.UseVisualStyleBackColor = true;

            // YubiKey settings group
            this.grpYubiKey.Location = new System.Drawing.Point(12, 400);
            this.grpYubiKey.Name = "grpYubiKey";
            this.grpYubiKey.Size = new System.Drawing.Size(360, 130);
            this.grpYubiKey.TabIndex = 16;
            this.grpYubiKey.TabStop = false;
            this.grpYubiKey.Text = "YubiKey Settings";

            // YubiKey controls
            this.chkEnableYubiKey.AutoSize = true;
            this.chkEnableYubiKey.Location = new System.Drawing.Point(20, 20);
            this.chkEnableYubiKey.Name = "chkEnableYubiKey";
            this.chkEnableYubiKey.Size = new System.Drawing.Size(100, 17);
            this.chkEnableYubiKey.TabIndex = 0;
            this.chkEnableYubiKey.Text = "Enable YubiKey";
            this.chkEnableYubiKey.UseVisualStyleBackColor = true;
            this.chkEnableYubiKey.CheckedChanged += new System.EventHandler(this.chkEnableYubiKey_CheckedChanged);

            this.lblYubiKeyServer.AutoSize = true;
            this.lblYubiKeyServer.Location = new System.Drawing.Point(20, 46);
            this.lblYubiKeyServer.Name = "lblYubiKeyServer";
            this.lblYubiKeyServer.Size = new System.Drawing.Size(87, 13);
            this.lblYubiKeyServer.TabIndex = 1;
            this.lblYubiKeyServer.Text = "YubiKey Server:";

            this.txtYubiKeyServer.Location = new System.Drawing.Point(140, 43);
            this.txtYubiKeyServer.Name = "txtYubiKeyServer";
            this.txtYubiKeyServer.Size = new System.Drawing.Size(200, 20);
            this.txtYubiKeyServer.TabIndex = 2;

            this.lblYubiKeyAPIKey.AutoSize = true;
            this.lblYubiKeyAPIKey.Location = new System.Drawing.Point(20, 72);
            this.lblYubiKeyAPIKey.Name = "lblYubiKeyAPIKey";
            this.lblYubiKeyAPIKey.Size = new System.Drawing.Size(91, 13);
            this.lblYubiKeyAPIKey.TabIndex = 3;
            this.lblYubiKeyAPIKey.Text = "YubiKey API Key:";

            this.txtYubiKeyAPIKey.Location = new System.Drawing.Point(140, 69);
            this.txtYubiKeyAPIKey.Name = "txtYubiKeyAPIKey";
            this.txtYubiKeyAPIKey.Size = new System.Drawing.Size(200, 20);
            this.txtYubiKeyAPIKey.TabIndex = 4;

            this.lblYubiKeyClientID.AutoSize = true;
            this.lblYubiKeyClientID.Location = new System.Drawing.Point(20, 98);
            this.lblYubiKeyClientID.Name = "lblYubiKeyClientID";
            this.lblYubiKeyClientID.Size = new System.Drawing.Size(96, 13);
            this.lblYubiKeyClientID.TabIndex = 5;
            this.lblYubiKeyClientID.Text = "YubiKey Client ID:";

            this.txtYubiKeyClientID.Location = new System.Drawing.Point(140, 95);
            this.txtYubiKeyClientID.Name = "txtYubiKeyClientID";
            this.txtYubiKeyClientID.Size = new System.Drawing.Size(200, 20);
            this.txtYubiKeyClientID.TabIndex = 6;

            this.btnConfigureYubiKey.Location = new System.Drawing.Point(270, 15);
            this.btnConfigureYubiKey.Name = "btnConfigureYubiKey";
            this.btnConfigureYubiKey.Size = new System.Drawing.Size(70, 23);
            this.btnConfigureYubiKey.TabIndex = 7;
            this.btnConfigureYubiKey.Text = "Configure";
            this.btnConfigureYubiKey.UseVisualStyleBackColor = true;
            this.btnConfigureYubiKey.Click += new System.EventHandler(this.btnConfigureYubiKey_Click);

            // Server crypto group
            this.grpServerCrypto.Location = new System.Drawing.Point(12, 540);
            this.grpServerCrypto.Name = "grpServerCrypto";
            this.grpServerCrypto.Size = new System.Drawing.Size(360, 100);
            this.grpServerCrypto.TabIndex = 17;
            this.grpServerCrypto.TabStop = false;
            this.grpServerCrypto.Text = "Server Key and Certificate";

            // Server key and certificate controls
            this.lblServerKey.AutoSize = true;
            this.lblServerKey.Location = new System.Drawing.Point(20, 25);
            this.lblServerKey.Name = "lblServerKey";
            this.lblServerKey.Size = new System.Drawing.Size(63, 13);
            this.lblServerKey.TabIndex = 0;
            this.lblServerKey.Text = "Server Key:";

            this.cmbServerKey.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbServerKey.FormattingEnabled = true;
            this.cmbServerKey.Location = new System.Drawing.Point(100, 22);
            this.cmbServerKey.Name = "cmbServerKey";
            this.cmbServerKey.Size = new System.Drawing.Size(180, 21);
            this.cmbServerKey.TabIndex = 1;

            this.btnGenerateServerKey.Location = new System.Drawing.Point(286, 20);
            this.btnGenerateServerKey.Name = "btnGenerateServerKey";
            this.btnGenerateServerKey.Size = new System.Drawing.Size(60, 23);
            this.btnGenerateServerKey.TabIndex = 2;
            this.btnGenerateServerKey.Text = "Generate";
            this.btnGenerateServerKey.UseVisualStyleBackColor = true;
            this.btnGenerateServerKey.Click += new System.EventHandler(this.btnGenerateServerKey_Click);

            this.lblServerCert.AutoSize = true;
            this.lblServerCert.Location = new System.Drawing.Point(20, 60);
            this.lblServerCert.Name = "lblServerCert";
            this.lblServerCert.Size = new System.Drawing.Size(70, 13);
            this.lblServerCert.TabIndex = 3;
            this.lblServerCert.Text = "Server Cert:";

            this.cmbServerCert.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbServerCert.FormattingEnabled = true;
            this.cmbServerCert.Location = new System.Drawing.Point(100, 57);
            this.cmbServerCert.Name = "cmbServerCert";
            this.cmbServerCert.Size = new System.Drawing.Size(180, 21);
            this.cmbServerCert.TabIndex = 4;

            this.btnGenerateServerCert.Location = new System.Drawing.Point(286, 55);
            this.btnGenerateServerCert.Name = "btnGenerateServerCert";
            this.btnGenerateServerCert.Size = new System.Drawing.Size(60, 23);
            this.btnGenerateServerCert.TabIndex = 5;
            this.btnGenerateServerCert.Text = "Generate";
            this.btnGenerateServerCert.UseVisualStyleBackColor = true;
            this.btnGenerateServerCert.Click += new System.EventHandler(this.btnGenerateServerCert_Click);

            // Logging group
            this.grpLogging.Location = new System.Drawing.Point(12, 650);
            this.grpLogging.Name = "grpLogging";
            this.grpLogging.Size = new System.Drawing.Size(360, 100);
            this.grpLogging.TabIndex = 18;
            this.grpLogging.TabStop = false;
            this.grpLogging.Text = "Logging";

            // Logging controls
            this.lblLogLevel.AutoSize = true;
            this.lblLogLevel.Location = new System.Drawing.Point(20, 25);
            this.lblLogLevel.Name = "lblLogLevel";
            this.lblLogLevel.Size = new System.Drawing.Size(61, 13);
            this.lblLogLevel.TabIndex = 0;
            this.lblLogLevel.Text = "Log Level:";

            this.cmbLogLevel.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbLogLevel.FormattingEnabled = true;
            this.cmbLogLevel.Location = new System.Drawing.Point(100, 22);
            this.cmbLogLevel.Name = "cmbLogLevel";
            this.cmbLogLevel.Size = new System.Drawing.Size(121, 21);
            this.cmbLogLevel.TabIndex = 1;

            this.chkEnableAuditLogging.AutoSize = true;
            this.chkEnableAuditLogging.Location = new System.Drawing.Point(20, 50);
            this.chkEnableAuditLogging.Name = "chkEnableAuditLogging";
            this.chkEnableAuditLogging.Size = new System.Drawing.Size(124, 17);
            this.chkEnableAuditLogging.TabIndex = 2;
            this.chkEnableAuditLogging.Text = "Enable Audit Logging";
            this.chkEnableAuditLogging.UseVisualStyleBackColor = true;

            this.lblLogFilePath.AutoSize = true;
            this.lblLogFilePath.Location = new System.Drawing.Point(20, 75);
            this.lblLogFilePath.Name = "lblLogFilePath";
            this.lblLogFilePath.Size = new System.Drawing.Size(73, 13);
            this.lblLogFilePath.TabIndex = 3;
            this.lblLogFilePath.Text = "Log File Path:";

            this.txtLogFilePath.Location = new System.Drawing.Point(100, 72);
            this.txtLogFilePath.Name = "txtLogFilePath";
            this.txtLogFilePath.Size = new System.Drawing.Size(200, 20);
            this.txtLogFilePath.TabIndex = 4;

            this.btnBrowseLogFile.Location = new System.Drawing.Point(306, 70);
            this.btnBrowseLogFile.Name = "btnBrowseLogFile";
            this.btnBrowseLogFile.Size = new System.Drawing.Size(40, 23);
            this.btnBrowseLogFile.TabIndex = 5;
            this.btnBrowseLogFile.Text = "...";
            this.btnBrowseLogFile.UseVisualStyleBackColor = true;
            this.btnBrowseLogFile.Click += new System.EventHandler(this.btnBrowseLogFile_Click);

            // OK and Cancel buttons
            this.btnOK.Location = new System.Drawing.Point(216, 760);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 23);
            this.btnOK.TabIndex = 19;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);

            this.btnCancel.Location = new System.Drawing.Point(297, 760);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 20;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);

            // Add controls to groups
            this.grpAuthentication.Controls.Add(this.chkPasswordAuth);
            this.grpAuthentication.Controls.Add(this.chkPublicKeyAuth);
            this.grpAuthentication.Controls.Add(this.chkCertificateAuth);
            this.grpAuthentication.Controls.Add(this.chkKeyboardInteractiveAuth);
            this.grpAuthentication.Controls.Add(this.chkYubikeyAuth);
            this.grpAuthentication.Controls.Add(this.chkTwoFactorAuth);

            this.grpYubiKey.Controls.Add(this.chkEnableYubiKey);
            this.grpYubiKey.Controls.Add(this.lblYubiKeyServer);
            this.grpYubiKey.Controls.Add(this.txtYubiKeyServer);
            this.grpYubiKey.Controls.Add(this.lblYubiKeyAPIKey);
            this.grpYubiKey.Controls.Add(this.txtYubiKeyAPIKey);
            this.grpYubiKey.Controls.Add(this.lblYubiKeyClientID);
            this.grpYubiKey.Controls.Add(this.txtYubiKeyClientID);
            this.grpYubiKey.Controls.Add(this.btnConfigureYubiKey);

            this.grpServerCrypto.Controls.Add(this.lblServerKey);
            this.grpServerCrypto.Controls.Add(this.cmbServerKey);
            this.grpServerCrypto.Controls.Add(this.btnGenerateServerKey);
            this.grpServerCrypto.Controls.Add(this.lblServerCert);
            this.grpServerCrypto.Controls.Add(this.cmbServerCert);
            this.grpServerCrypto.Controls.Add(this.btnGenerateServerCert);

            this.grpLogging.Controls.Add(this.lblLogLevel);
            this.grpLogging.Controls.Add(this.cmbLogLevel);
            this.grpLogging.Controls.Add(this.chkEnableAuditLogging);
            this.grpLogging.Controls.Add(this.lblLogFilePath);
            this.grpLogging.Controls.Add(this.txtLogFilePath);
            this.grpLogging.Controls.Add(this.btnBrowseLogFile);

            // ServerConfigForm
            this.AcceptButton = this.btnOK;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(384, 795);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.grpLogging);
            this.Controls.Add(this.grpServerCrypto);
            this.Controls.Add(this.grpYubiKey);
            this.Controls.Add(this.chkPermitRootLogin);
            this.Controls.Add(this.numLoginGraceTime);
            this.Controls.Add(this.lblLoginGraceTime);
            this.Controls.Add(this.numMaxAuthTries);
            this.Controls.Add(this.lblMaxAuthTries);
            this.Controls.Add(this.grpAuthentication);
            this.Controls.Add(this.cmbSecurityLevel);
            this.Controls.Add(this.lblSecurityLevel);
            this.Controls.Add(this.chkUseEncryption);
            this.Controls.Add(this.lblAllowedClientsHelp);
            this.Controls.Add(this.txtAllowedClients);
            this.Controls.Add(this.lblAllowedClients);
            this.Controls.Add(this.numListenPort);
            this.Controls.Add(this.lblListenPort);
            this.Controls.Add(this.txtName);
            this.Controls.Add(this.lblName);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ServerConfigForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "SSH Server Configuration";

            ((System.ComponentModel.ISupportInitialize)(this.numListenPort)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numMaxAuthTries)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numLoginGraceTime)).EndInit();
            this.grpAuthentication.ResumeLayout(false);
            this.grpAuthentication.PerformLayout();
            this.grpYubiKey.ResumeLayout(false);
            this.grpYubiKey.PerformLayout();
            this.grpServerCrypto.ResumeLayout(false);
            this.grpServerCrypto.PerformLayout();
            this.grpLogging.ResumeLayout(false);
            this.grpLogging.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.Label lblName;
        private System.Windows.Forms.TextBox txtName;
        private System.Windows.Forms.Label lblListenPort;
        private System.Windows.Forms.NumericUpDown numListenPort;
        private System.Windows.Forms.Label lblAllowedClients;
        private System.Windows.Forms.TextBox txtAllowedClients;
        private System.Windows.Forms.Label lblAllowedClientsHelp;
        private System.Windows.Forms.CheckBox chkUseEncryption;
        private System.Windows.Forms.Label lblSecurityLevel;
        private System.Windows.Forms.ComboBox cmbSecurityLevel;
        private System.Windows.Forms.GroupBox grpAuthentication;
        private System.Windows.Forms.CheckBox chkPasswordAuth;
        private System.Windows.Forms.CheckBox chkPublicKeyAuth;
        private System.Windows.Forms.CheckBox chkCertificateAuth;
        private System.Windows.Forms.CheckBox chkKeyboardInteractiveAuth;
        private System.Windows.Forms.CheckBox chkYubikeyAuth;
        private System.Windows.Forms.CheckBox chkTwoFactorAuth;
        private System.Windows.Forms.Label lblMaxAuthTries;
        private System.Windows.Forms.NumericUpDown numMaxAuthTries;
        private System.Windows.Forms.Label lblLoginGraceTime;
        private System.Windows.Forms.NumericUpDown numLoginGraceTime;
        private System.Windows.Forms.CheckBox chkPermitRootLogin;
        private System.Windows.Forms.GroupBox grpYubiKey;
        private System.Windows.Forms.CheckBox chkEnableYubiKey;
        private System.Windows.Forms.Label lblYubiKeyServer;
        private System.Windows.Forms.TextBox txtYubiKeyServer;
        private System.Windows.Forms.Label lblYubiKeyAPIKey;
        private System.Windows.Forms.TextBox txtYubiKeyAPIKey;
        private System.Windows.Forms.Label lblYubiKeyClientID;
        private System.Windows.Forms.TextBox txtYubiKeyClientID;
        private System.Windows.Forms.Button btnConfigureYubiKey;
        private System.Windows.Forms.GroupBox grpServerCrypto;
        private System.Windows.Forms.Label lblServerKey;
        private System.Windows.Forms.ComboBox cmbServerKey;
        private System.Windows.Forms.Button btnGenerateServerKey;
        private System.Windows.Forms.Label lblServerCert;
        private System.Windows.Forms.ComboBox cmbServerCert;
        private System.Windows.Forms.Button btnGenerateServerCert;
        private System.Windows.Forms.GroupBox grpLogging;
        private System.Windows.Forms.Label lblLogLevel;
        private System.Windows.Forms.ComboBox cmbLogLevel;
        private System.Windows.Forms.CheckBox chkEnableAuditLogging;
        private System.Windows.Forms.Label lblLogFilePath;
        private System.Windows.Forms.TextBox txtLogFilePath;
        private System.Windows.Forms.Button btnBrowseLogFile;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;
    }
}