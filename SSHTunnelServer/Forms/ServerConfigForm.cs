using System;
using System.Windows.Forms;
using System.IO;
using System.Collections.Generic;
using System.Xml.Linq;

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
                    AllowedAuthMethods = new List<AuthMethod>(existingConfig.AllowedAuthMethods),
                    MaxAuthTries = existingConfig.MaxAuthTries,
                    LoginGraceTime = existingConfig.LoginGraceTime,
                    PermitRootLogin = existingConfig.PermitRootLogin,
                    PasswordAuthentication = existingConfig.PasswordAuthentication,
                    PubkeyAuthentication = existingConfig.PubkeyAuthentication,
                    ChallengeResponseAuth = existingConfig.ChallengeResponseAuth,
                    EnableYubiKey = existingConfig.EnableYubiKey,
                    YubiKeyAuthServer = existingConfig.YubiKeyAuthServer,
                    YubiKeyAPIKey = existingConfig.YubiKeyAPIKey,
                    YubiKeyClientID = existingConfig.YubiKeyClientID,
                    ServerKeyPath = existingConfig.ServerKeyPath,
                    ServerCertPath = existingConfig.ServerCertPath,
                    LogLevel = existingConfig.LogLevel,
                    EnableAuditLogging = existingConfig.EnableAuditLogging,
                    LogFilePath = existingConfig.LogFilePath,
                    IsActive = existingConfig.IsActive
                };
            }
            else
            {
                // Create new configuration with defaults
                Configuration = new ServerConfig
                {
                    Name = "New Configuration",
                    ListenPort = 22,
                    AllowedClients = "*",
                    UseEncryption = false,
                    SecurityLevel = SecurityLevel.Standard,
                    AllowedAuthMethods = new List<AuthMethod>
                    {
                        AuthMethod.Password,
                        AuthMethod.PublicKey
                    },
                    IsActive = false
                };
            }

            // Populate form fields
            txtName.Text = Configuration.Name;
            numListenPort.Value = Configuration.ListenPort;
            txtAllowedClients.Text = Configuration.AllowedClients;
            chkUseEncryption.Checked = Configuration.UseEncryption;

            // Set security level
            cmbSecurityLevel.Items.Clear();
            foreach (SecurityLevel level in Enum.GetValues(typeof(SecurityLevel)))
            {
                cmbSecurityLevel.Items.Add(level.ToString());
            }
            cmbSecurityLevel.SelectedIndex = (int)Configuration.SecurityLevel;

            // Authentication methods
            chkPasswordAuth.Checked = Configuration.AllowedAuthMethods.Contains(AuthMethod.Password);
            chkPublicKeyAuth.Checked = Configuration.AllowedAuthMethods.Contains(AuthMethod.PublicKey);
            chkCertificateAuth.Checked = Configuration.AllowedAuthMethods.Contains(AuthMethod.Certificate);
            chkKeyboardInteractiveAuth.Checked = Configuration.AllowedAuthMethods.Contains(AuthMethod.KeyboardInteractive);
            chkYubikeyAuth.Checked = Configuration.AllowedAuthMethods.Contains(AuthMethod.YubiKey);
            chkTwoFactorAuth.Checked = Configuration.AllowedAuthMethods.Contains(AuthMethod.TwoFactor);

            // Advanced settings
            numMaxAuthTries.Value = Configuration.MaxAuthTries;
            numLoginGraceTime.Value = Configuration.LoginGraceTime;
            chkPermitRootLogin.Checked = Configuration.PermitRootLogin;

            // YubiKey settings
            chkEnableYubiKey.Checked = Configuration.EnableYubiKey;
            txtYubiKeyServer.Text = Configuration.YubiKeyAuthServer;
            txtYubiKeyAPIKey.Text = Configuration.YubiKeyAPIKey;
            txtYubiKeyClientID.Text = Configuration.YubiKeyClientID;

            // Server keys and certificates
            PopulateServerKeysDropdown();
            PopulateServerCertificatesDropdown();

            if (!string.IsNullOrEmpty(Configuration.ServerKeyPath))
            {
                string keyName = Path.GetFileNameWithoutExtension(Configuration.ServerKeyPath);
                int index = cmbServerKey.FindStringExact(keyName);
                if (index >= 0)
                    cmbServerKey.SelectedIndex = index;
            }

            if (!string.IsNullOrEmpty(Configuration.ServerCertPath))
            {
                string certName = Path.GetFileNameWithoutExtension(Configuration.ServerCertPath);
                int index = cmbServerCert.FindStringExact(certName);
                if (index >= 0)
                    cmbServerCert.SelectedIndex = index;
            }

            // Logging
            cmbLogLevel.Items.Clear();
            foreach (LogLevel level in Enum.GetValues(typeof(LogLevel)))
            {
                cmbLogLevel.Items.Add(level.ToString());
            }
            cmbLogLevel.SelectedIndex = (int)Configuration.LogLevel;
            chkEnableAuditLogging.Checked = Configuration.EnableAuditLogging;
            txtLogFilePath.Text = Configuration.LogFilePath;

            // Update UI state
            UpdateYubiKeyUIState();
        }

        private void PopulateServerKeysDropdown()
        {
            cmbServerKey.Items.Clear();
            cmbServerKey.Items.Add("(Default)");

            foreach (string key in _securityManager.GetAvailableServerKeys())
            {
                cmbServerKey.Items.Add(key);
            }

            cmbServerKey.SelectedIndex = 0;
        }

        private void PopulateServerCertificatesDropdown()
        {
            cmbServerCert.Items.Clear();
            cmbServerCert.Items.Add("(Default)");

            foreach (string cert in _securityManager.GetAvailableServerCertificates())
            {
                cmbServerCert.Items.Add(cert);
            }

            cmbServerCert.SelectedIndex = 0;
        }

        private void chkEnableYubiKey_CheckedChanged(object sender, EventArgs e)
        {
            UpdateYubiKeyUIState();
        }

        private void UpdateYubiKeyUIState()
        {
            bool yubiKeyEnabled = chkEnableYubiKey.Checked;

            txtYubiKeyServer.Enabled = yubiKeyEnabled;
            txtYubiKeyAPIKey.Enabled = yubiKeyEnabled;
            txtYubiKeyClientID.Enabled = yubiKeyEnabled;
            btnConfigureYubiKey.Enabled = yubiKeyEnabled;
            chkYubikeyAuth.Enabled = yubiKeyEnabled;

            if (yubiKeyEnabled && !chkYubikeyAuth.Checked)
            {
                chkYubikeyAuth.Checked = true;
            }
        }

        private void btnGenerateKey_Click(object sender, EventArgs e)
        {
            using (var keyGenForm = new ServerKeyGenerationForm())
            {
                if (keyGenForm.ShowDialog() == DialogResult.OK)
                {
                    // Refresh the keys dropdown
                    PopulateServerKeysDropdown();

                    // Select the newly generated key
                    int index = cmbServerKey.FindStringExact(keyGenForm.GeneratedKeyName);
                    if (index >= 0)
                        cmbServerKey.SelectedIndex = index;
                }
            }
        }

        private void btnGenerateCert_Click(object sender, EventArgs e)
        {
            using (var certGenForm = new ServerCertificateForm())
            {
                if (certGenForm.ShowDialog() == DialogResult.OK)
                {
                    // Refresh the certificates dropdown
                    PopulateServerCertificatesDropdown();

                    // Select the newly generated certificate
                    int index = cmbServerCert.FindStringExact(certGenForm.GeneratedCertName);
                    if (index >= 0)
                        cmbServerCert.SelectedIndex = index;
                }
            }
        }

        private void btnBrowseLogFile_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Filter = "Log Files|*.log|All Files|*.*";
                dialog.Title = "Select Log File Location";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    txtLogFilePath.Text = dialog.FileName;
                }
            }
        }

        private void btnConfigureYubiKey_Click(object sender, EventArgs e)
        {
            MessageBox.Show("YubiKey configuration requires the YubiKey Manager (ykman) tool to be installed on the server.\n\n" +
                "Please make sure the YubiKey is configured with the OATH or OTP application enabled.",
                "YubiKey Configuration", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Please enter a name for the configuration.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Collect authentication methods
            List<AuthMethod> allowedAuthMethods = new List<AuthMethod>();
            if (chkPasswordAuth.Checked) allowedAuthMethods.Add(AuthMethod.Password);
            if (chkPublicKeyAuth.Checked) allowedAuthMethods.Add(AuthMethod.PublicKey);
            if (chkCertificateAuth.Checked) allowedAuthMethods.Add(AuthMethod.Certificate);
            if (chkKeyboardInteractiveAuth.Checked) allowedAuthMethods.Add(AuthMethod.KeyboardInteractive);
            if (chkYubikeyAuth.Checked) allowedAuthMethods.Add(AuthMethod.YubiKey);
            if (chkTwoFactorAuth.Checked) allowedAuthMethods.Add(AuthMethod.TwoFactor);

            if (allowedAuthMethods.Count == 0)
            {
                MessageBox.Show("Please select at least one authentication method.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // YubiKey validation
            if (chkEnableYubiKey.Checked && chkYubikeyAuth.Checked)
            {
                if (string.IsNullOrEmpty(txtYubiKeyServer.Text))
                {
                    MessageBox.Show("Please enter a YubiKey authentication server.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            // Update configuration
            Configuration.Name = txtName.Text;
            Configuration.ListenPort = (int)numListenPort.Value;
            Configuration.AllowedClients = txtAllowedClients.Text;
            Configuration.UseEncryption = chkUseEncryption.Checked;

            // Security level
            if (cmbSecurityLevel.SelectedIndex >= 0)
            {
                Configuration.SecurityLevel = (SecurityLevel)cmbSecurityLevel.SelectedIndex;
            }

            // Authentication methods
            Configuration.AllowedAuthMethods = allowedAuthMethods;

            // Advanced settings
            Configuration.MaxAuthTries = (int)numMaxAuthTries.Value;
            Configuration.LoginGraceTime = (int)numLoginGraceTime.Value;
            Configuration.PermitRootLogin = chkPermitRootLogin.Checked;
            Configuration.PasswordAuthentication = chkPasswordAuth.Checked;
            Configuration.PubkeyAuthentication = chkPublicKeyAuth.Checked;
            Configuration.ChallengeResponseAuth = chkKeyboardInteractiveAuth.Checked;

            // YubiKey settings
            Configuration.EnableYubiKey = chkEnableYubiKey.Checked;
            Configuration.YubiKeyAuthServer = txtYubiKeyServer.Text;
            Configuration.YubiKeyAPIKey = txtYubiKeyAPIKey.Text;
            Configuration.YubiKeyClientID = txtYubiKeyClientID.Text;

            // Server keys and certificates
            if (cmbServerKey.SelectedIndex > 0)
            {
                string appDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "SSHTunnelServer", "server_keys");
                Configuration.ServerKeyPath = Path.Combine(appDataPath, $"{cmbServerKey.SelectedItem}.key");
            }
            else
            {
                Configuration.ServerKeyPath = "";
            }

            if (cmbServerCert.SelectedIndex > 0)
            {
                string appDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "SSHTunnelServer", "server_certs");
                Configuration.ServerCertPath = Path.Combine(appDataPath, $"{cmbServerCert.SelectedItem}.pfx");
            }
            else
            {
                Configuration.ServerCertPath = "";
            }

            // Logging
            if (cmbLogLevel.SelectedIndex >= 0)
            {
                Configuration.LogLevel = (LogLevel)cmbLogLevel.SelectedIndex;
            }
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

        private void btnViewConfig_Click(object sender, EventArgs e)
        {
            // Generate config preview and show in a dialog
            string configPreview = _securityManager.GenerateSSHConfig(Configuration);

            using (var previewForm = new ConfigPreviewForm(configPreview))
            {
                previewForm.ShowDialog();
            }
        }

        // The InitializeComponent method would be here with the designer code
        private void InitializeComponent()
        {
            // Components would be initialized here
            // We're not showing the complete UI initialization code for brevity
            // In a real implementation, you would have all the control declarations and UI layout here
        }
    }

    // Preview form for SSH config
    public class ConfigPreviewForm : Form
    {
        private TextBox txtConfigPreview;
        private Button btnClose;

        public ConfigPreviewForm(string configText)
        {
            InitializeComponent();
            txtConfigPreview.Text = configText;
        }

        private void InitializeComponent()
        {
            this.txtConfigPreview = new System.Windows.Forms.TextBox();
            this.btnClose = new System.Windows.Forms.Button();
            this.SuspendLayout();

            // txtConfigPreview
            this.txtConfigPreview.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtConfigPreview.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtConfigPreview.Location = new System.Drawing.Point(12, 12);
            this.txtConfigPreview.Multiline = true;
            this.txtConfigPreview.Name = "txtConfigPreview";
            this.txtConfigPreview.ReadOnly = true;
            this.txtConfigPreview.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtConfigPreview.Size = new System.Drawing.Size(560, 398);
            this.txtConfigPreview.TabIndex = 0;

            // btnClose
            this.btnClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnClose.Location = new System.Drawing.Point(497, 416);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(75, 23);
            this.btnClose.TabIndex = 1;
            this.btnClose.Text = "Close";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);

            // ConfigPreviewForm
            this.AcceptButton = this.btnClose;
            this.CancelButton = this.btnClose;
            this.ClientSize = new System.Drawing.Size(584, 451);
            this.Controls.Add(this.btnClose);
            this.Controls.Add(this.txtConfigPreview);
            this.MinimizeBox = false;
            this.Name = "ConfigPreviewForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "SSH Config Preview";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }
    }

    // Server key generation form
    public class ServerKeyGenerationForm : Form
    {
        private System.Windows.Forms.Label lblKeyName;
        private System.Windows.Forms.TextBox txtKeyName;
        private System.Windows.Forms.Label lblKeySize;
        private System.Windows.Forms.NumericUpDown numKeySize;
        private System.Windows.Forms.Button btnGenerate;
        private System.Windows.Forms.Button btnCancel;

        private ServerSecurityManager _securityManager;
        public string GeneratedKeyName { get; private set; }

        public ServerKeyGenerationForm()
        {
            InitializeComponent();
            _securityManager = new ServerSecurityManager();
        }

        private void InitializeComponent()
        {
            this.lblKeyName = new System.Windows.Forms.Label();
            this.txtKeyName = new System.Windows.Forms.TextBox();
            this.lblKeySize = new System.Windows.Forms.Label();
            this.numKeySize = new System.Windows.Forms.NumericUpDown();
            this.btnGenerate = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.numKeySize)).BeginInit();
            this.SuspendLayout();

            // lblKeyName
            this.lblKeyName.AutoSize = true;
            this.lblKeyName.Location = new System.Drawing.Point(12, 15);
            this.lblKeyName.Name = "lblKeyName";
            this.lblKeyName.Size = new System.Drawing.Size(60, 13);
            this.lblKeyName.TabIndex = 0;
            this.lblKeyName.Text = "Key Name:";

            // txtKeyName
            this.txtKeyName.Location = new System.Drawing.Point(78, 12);
            this.txtKeyName.Name = "txtKeyName";
            this.txtKeyName.Size = new System.Drawing.Size(224, 20);
            this.txtKeyName.TabIndex = 1;

            // lblKeySize
            this.lblKeySize.AutoSize = true;
            this.lblKeySize.Location = new System.Drawing.Point(12, 41);
            this.lblKeySize.Name = "lblKeySize";
            this.lblKeySize.Size = new System.Drawing.Size(52, 13);
            this.lblKeySize.TabIndex = 2;
            this.lblKeySize.Text = "Key Size:";

            // numKeySize
            this.numKeySize.Location = new System.Drawing.Point(78, 39);
            this.numKeySize.Maximum = new decimal(new int[] {
            8192,
            0,
            0,
            0});
            this.numKeySize.Minimum = new decimal(new int[] {
            1024,
            0,
            0,
            0});
            this.numKeySize.Name = "numKeySize";
            this.numKeySize.Size = new System.Drawing.Size(120, 20);
            this.numKeySize.TabIndex = 3;
            this.numKeySize.Value = new decimal(new int[] {
            2048,
            0,
            0,
            0});

            // btnGenerate
            this.btnGenerate.Location = new System.Drawing.Point(146, 65);
            this.btnGenerate.Name = "btnGenerate";
            this.btnGenerate.Size = new System.Drawing.Size(75, 23);
            this.btnGenerate.TabIndex = 4;
            this.btnGenerate.Text = "Generate";
            this.btnGenerate.UseVisualStyleBackColor = true;
            this.btnGenerate.Click += new System.EventHandler(this.btnGenerate_Click);

            // btnCancel
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(227, 65);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 5;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);

            // ServerKeyGenerationForm
            this.AcceptButton = this.btnGenerate;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(314, 101);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnGenerate);
            this.Controls.Add(this.numKeySize);
            this.Controls.Add(this.lblKeySize);
            this.Controls.Add(this.txtKeyName);
            this.Controls.Add(this.lblKeyName);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ServerKeyGenerationForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Generate Server Key";
            ((System.ComponentModel.ISupportInitialize)(this.numKeySize)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private void btnGenerate_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtKeyName.Text))
            {
                MessageBox.Show("Please enter a name for the key.", "Validation Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Generate the key
            bool success = _securityManager.GenerateServerKeyPair(
                txtKeyName.Text,
                (int)numKeySize.Value);

            if (success)
            {
                GeneratedKeyName = txtKeyName.Text;
                DialogResult = DialogResult.OK;
                Close();
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }

    // Server certificate form
    public class ServerCertificateForm : Form
    {
        private System.Windows.Forms.Label lblCertName;
        private System.Windows.Forms.TextBox txtCertName;
        private System.Windows.Forms.Label lblSubjectName;
        private System.Windows.Forms.TextBox txtSubjectName;
        private System.Windows.Forms.Label lblValidYears;
        private System.Windows.Forms.NumericUpDown numValidYears;
        private System.Windows.Forms.Button btnGenerate;
        private System.Windows.Forms.Button btnCancel;

        private ServerSecurityManager _securityManager;
        public string GeneratedCertName { get; private set; }

        public ServerCertificateForm()
        {
            InitializeComponent();
            _securityManager = new ServerSecurityManager();
        }

        private void InitializeComponent()
        {
            this.lblCertName = new System.Windows.Forms.Label();
            this.txtCertName = new System.Windows.Forms.TextBox();
            this.lblSubjectName = new System.Windows.Forms.Label();
            this.txtSubjectName = new System.Windows.Forms.TextBox();
            this.lblValidYears = new System.Windows.Forms.Label();
            this.numValidYears = new System.Windows.Forms.NumericUpDown();
            this.btnGenerate = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.numValidYears)).BeginInit();
            this.SuspendLayout();

            // lblCertName
            this.lblCertName.AutoSize = true;
            this.lblCertName.Location = new System.Drawing.Point(12, 15);
            this.lblCertName.Name = "lblCertName";
            this.lblCertName.Size = new System.Drawing.Size(93, 13);
            this.lblCertName.TabIndex = 0;
            this.lblCertName.Text = "Certificate Name:";

            // txtCertName
            this.txtCertName.Location = new System.Drawing.Point(111, 12);
            this.txtCertName.Name = "txtCertName";
            this.txtCertName.Size = new System.Drawing.Size(191, 20);
            this.txtCertName.TabIndex = 1;

            // lblSubjectName
            this.lblSubjectName.AutoSize = true;
            this.lblSubjectName.Location = new System.Drawing.Point(12, 41);
            this.lblSubjectName.Name = "lblSubjectName";
            this.lblSubjectName.Size = new System.Drawing.Size(80, 13);
            this.lblSubjectName.TabIndex = 2;
            this.lblSubjectName.Text = "Subject Name:";

            // txtSubjectName
            this.txtSubjectName.Location = new System.Drawing.Point(111, 38);
            this.txtSubjectName.Name = "txtSubjectName";
            this.txtSubjectName.Size = new System.Drawing.Size(191, 20);
            this.txtSubjectName.TabIndex = 3;
            this.txtSubjectName.Text = "CN=SSHTunnelServer";

            // lblValidYears
            this.lblValidYears.AutoSize = true;
            this.lblValidYears.Location = new System.Drawing.Point(12, 67);
            this.lblValidYears.Name = "lblValidYears";
            this.lblValidYears.Size = new System.Drawing.Size(85, 13);
            this.lblValidYears.TabIndex = 4;
            this.lblValidYears.Text = "Valid for (years):";

            // numValidYears
            this.numValidYears.Location = new System.Drawing.Point(111, 65);
            this.numValidYears.Maximum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.numValidYears.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numValidYears.Name = "numValidYears";
            this.numValidYears.Size = new System.Drawing.Size(60, 20);
            this.numValidYears.TabIndex = 5;
            this.numValidYears.Value = new decimal(new int[] {
            5,
            0,
            0,
            0});

            // btnGenerate
            this.btnGenerate.Location = new System.Drawing.Point(146, 91);
            this.btnGenerate.Name = "btnGenerate";
            this.btnGenerate.Size = new System.Drawing.Size(75, 23);
            this.btnGenerate.TabIndex = 6;
            this.btnGenerate.Text = "Generate";
            this.btnGenerate.UseVisualStyleBackColor = true;
            this.btnGenerate.Click += new System.EventHandler(this.btnGenerate_Click);

            // btnCancel
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(227, 91);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 7;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);

            // ServerCertificateForm
            this.AcceptButton = this.btnGenerate;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(314, 126);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnGenerate);
            this.Controls.Add(this.numValidYears);
            this.Controls.Add(this.lblValidYears);
            this.Controls.Add(this.txtSubjectName);
            this.Controls.Add(this.lblSubjectName);
            this.Controls.Add(this.txtCertName);
            this.Controls.Add(this.lblCertName);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ServerCertificateForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Generate Server Certificate";
            ((System.ComponentModel.ISupportInitialize)(this.numValidYears)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private void btnGenerate_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtCertName.Text))
            {
                MessageBox.Show("Please enter a name for the certificate.", "Validation Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtSubjectName.Text))
            {
                MessageBox.Show("Please enter a subject name.", "Validation Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Generate the certificate
            bool success = _securityManager.GenerateServerCertificate(
                txtCertName.Text,
                txtSubjectName.Text,
                (int)numValidYears.Value);

            if (success)
            {
                GeneratedCertName = txtCertName.Text;
                DialogResult = DialogResult.OK;
                Close();
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}