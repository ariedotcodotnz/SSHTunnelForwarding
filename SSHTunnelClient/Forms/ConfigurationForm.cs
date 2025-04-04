using System;
using System.Windows.Forms;
using System.IO;
using SSHTunnelClient.Models;

namespace SSHTunnelClient
{
    public partial class ConfigurationForm : Form
    {
        public PortForwardingConfig Configuration { get; private set; }
        private AuthenticationManager _authManager;

        public ConfigurationForm(PortForwardingConfig existingConfig)
        {
            InitializeComponent();
            _authManager = new AuthenticationManager();

            if (existingConfig != null)
            {
                // Edit existing configuration
                Configuration = new PortForwardingConfig
                {
                    Name = existingConfig.Name,
                    ServerHost = existingConfig.ServerHost,
                    ServerPort = existingConfig.ServerPort,
                    Username = existingConfig.Username,
                    AuthenticationMethod = existingConfig.AuthenticationMethod,
                    Password = existingConfig.Password,
                    PrivateKeyPath = existingConfig.PrivateKeyPath,
                    PrivateKeyPassphrase = existingConfig.PrivateKeyPassphrase,
                    CertificateName = existingConfig.CertificateName,
                    UseTOTP = existingConfig.UseTOTP,
                    TOTPSecretKey = existingConfig.TOTPSecretKey,
                    LocalPort = existingConfig.LocalPort,
                    RemoteHost = existingConfig.RemoteHost,
                    RemotePort = existingConfig.RemotePort,
                    UseEncryption = existingConfig.UseEncryption,
                    ConnectionTimeout = existingConfig.ConnectionTimeout,
                    KeepAliveInterval = existingConfig.KeepAliveInterval,
                    EnableCompression = existingConfig.EnableCompression,
                    SSHOptions = existingConfig.SSHOptions,
                    IsActive = existingConfig.IsActive
                };
            }
            else
            {
                // Create new configuration
                Configuration = new PortForwardingConfig
                {
                    Name = "New Configuration",
                    ServerHost = "example.com",
                    ServerPort = 22,
                    Username = "username",
                    AuthenticationMethod = AuthMethod.Password,
                    Password = "",
                    PrivateKeyPath = "",
                    PrivateKeyPassphrase = "",
                    CertificateName = "",
                    UseTOTP = false,
                    TOTPSecretKey = "",
                    LocalPort = 8080,
                    RemoteHost = "localhost",
                    RemotePort = 80,
                    UseEncryption = false,
                    ConnectionTimeout = 30,
                    KeepAliveInterval = 60,
                    EnableCompression = true,
                    SSHOptions = "",
                    IsActive = false
                };
            }

            // Populate form fields
            txtName.Text = Configuration.Name;
            txtServerHost.Text = Configuration.ServerHost;
            numServerPort.Value = Configuration.ServerPort;
            txtUsername.Text = Configuration.Username;

            // Authentication settings
            cmbAuthMethod.SelectedIndex = (int)Configuration.AuthenticationMethod;
            txtPassword.Text = Configuration.Password;
            txtPrivateKeyPath.Text = Configuration.PrivateKeyPath;
            txtPrivateKeyPassphrase.Text = Configuration.PrivateKeyPassphrase;

            // Certificate selection
            PopulateCertificateDropdown();
            if (!string.IsNullOrEmpty(Configuration.CertificateName))
            {
                int index = cmbCertificate.FindStringExact(Configuration.CertificateName);
                if (index >= 0)
                    cmbCertificate.SelectedIndex = index;
            }

            // TOTP settings
            chkUseTOTP.Checked = Configuration.UseTOTP;
            txtTOTPSecret.Text = Configuration.TOTPSecretKey;

            // Forwarding settings
            numLocalPort.Value = Configuration.LocalPort;
            txtRemoteHost.Text = Configuration.RemoteHost;
            numRemotePort.Value = Configuration.RemotePort;
            chkUseEncryption.Checked = Configuration.UseEncryption;

            // Advanced settings
            numConnectionTimeout.Value = Configuration.ConnectionTimeout;
            numKeepAliveInterval.Value = Configuration.KeepAliveInterval;
            chkEnableCompression.Checked = Configuration.EnableCompression;
            txtSSHOptions.Text = Configuration.SSHOptions;

            // Update UI based on selected authentication method
            UpdateAuthenticationUIState();
        }

        private void PopulateCertificateDropdown()
        {
            cmbCertificate.Items.Clear();

            foreach (string cert in _authManager.GetAvailableCertificates())
            {
                cmbCertificate.Items.Add(cert);
            }
        }

        private void cmbAuthMethod_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateAuthenticationUIState();
        }

        private void UpdateAuthenticationUIState()
        {
            // Hide all authentication panels first
            pnlPasswordAuth.Visible = false;
            pnlPrivateKeyAuth.Visible = false;
            pnlCertificateAuth.Visible = false;

            // Show the appropriate panel based on selected authentication method
            switch (cmbAuthMethod.SelectedIndex)
            {
                case (int)AuthMethod.Password:
                    pnlPasswordAuth.Visible = true;
                    break;

                case (int)AuthMethod.PrivateKey:
                    pnlPrivateKeyAuth.Visible = true;
                    break;

                case (int)AuthMethod.Certificate:
                    pnlCertificateAuth.Visible = true;
                    break;

                case (int)AuthMethod.KeyboardInteractive:
                    pnlPasswordAuth.Visible = true;
                    break;
            }

            // TOTP is available for all auth methods
            pnlTOTP.Visible = true;
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Please enter a name for the configuration.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtServerHost.Text))
            {
                MessageBox.Show("Please enter a server host.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtUsername.Text))
            {
                MessageBox.Show("Please enter a username.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtRemoteHost.Text))
            {
                MessageBox.Show("Please enter a remote host.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Validate authentication inputs based on selected method
            AuthMethod selectedAuthMethod = (AuthMethod)cmbAuthMethod.SelectedIndex;

            if (selectedAuthMethod == AuthMethod.PrivateKey)
            {
                if (string.IsNullOrEmpty(txtPrivateKeyPath.Text))
                {
                    MessageBox.Show("Please specify a private key file.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (!File.Exists(txtPrivateKeyPath.Text))
                {
                    MessageBox.Show("The specified private key file does not exist.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            else if (selectedAuthMethod == AuthMethod.Certificate)
            {
                if (cmbCertificate.SelectedIndex < 0)
                {
                    MessageBox.Show("Please select a certificate.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            if (chkUseTOTP.Checked && string.IsNullOrWhiteSpace(txtTOTPSecret.Text))
            {
                MessageBox.Show("Please enter a TOTP secret key or uncheck the TOTP option.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Update configuration
            Configuration.Name = txtName.Text;
            Configuration.ServerHost = txtServerHost.Text;
            Configuration.ServerPort = (int)numServerPort.Value;
            Configuration.Username = txtUsername.Text;

            // Authentication settings
            Configuration.AuthenticationMethod = (AuthMethod)cmbAuthMethod.SelectedIndex;
            Configuration.Password = txtPassword.Text;
            Configuration.PrivateKeyPath = txtPrivateKeyPath.Text;
            Configuration.PrivateKeyPassphrase = txtPrivateKeyPassphrase.Text;

            // Certificate
            if (cmbCertificate.SelectedIndex >= 0)
            {
                Configuration.CertificateName = cmbCertificate.SelectedItem.ToString();
            }

            // TOTP settings
            Configuration.UseTOTP = chkUseTOTP.Checked;
            Configuration.TOTPSecretKey = txtTOTPSecret.Text;

            // Forwarding settings
            Configuration.LocalPort = (int)numLocalPort.Value;
            Configuration.RemoteHost = txtRemoteHost.Text;
            Configuration.RemotePort = (int)numRemotePort.Value;
            Configuration.UseEncryption = chkUseEncryption.Checked;

            // Advanced settings
            Configuration.ConnectionTimeout = (int)numConnectionTimeout.Value;
            Configuration.KeepAliveInterval = (int)numKeepAliveInterval.Value;
            Configuration.EnableCompression = chkEnableCompression.Checked;
            Configuration.SSHOptions = txtSSHOptions.Text;

            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void btnBrowsePrivateKey_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter = "Private Key Files|*.ppk;*.pem;*.*|All Files|*.*";
                dialog.Title = "Select Private Key File";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    txtPrivateKeyPath.Text = dialog.FileName;
                }
            }
        }

        private void btnGenerateKeyPair_Click(object sender, EventArgs e)
        {
            using (var keyGenForm = new KeyGenerationForm())
            {
                if (keyGenForm.ShowDialog() == DialogResult.OK)
                {
                    // Update the key path if a key was generated
                    if (!string.IsNullOrEmpty(keyGenForm.GeneratedKeyPath))
                    {
                        txtPrivateKeyPath.Text = keyGenForm.GeneratedKeyPath;
                        cmbAuthMethod.SelectedIndex = (int)AuthMethod.PrivateKey;
                        UpdateAuthenticationUIState();
                    }
                }
            }
        }

        private void btnImportCertificate_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter = "Certificate Files|*.pfx;*.p12;*.cer;*.crt|All Files|*.*";
                dialog.Title = "Select Certificate File";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    string password = null;

                    // Ask for password if it's a PKCS#12/PFX file
                    if (Path.GetExtension(dialog.FileName).ToLower() == ".pfx" ||
                        Path.GetExtension(dialog.FileName).ToLower() == ".p12")
                    {
                        using (var pwdForm = new PasswordPromptForm("Enter Certificate Password"))
                        {
                            if (pwdForm.ShowDialog() == DialogResult.OK)
                            {
                                password = pwdForm.Password;
                            }
                            else
                            {
                                return; // User cancelled
                            }
                        }
                    }

                    if (_authManager.ImportCertificate(dialog.FileName, password))
                    {
                        MessageBox.Show("Certificate imported successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        PopulateCertificateDropdown();
                        cmbAuthMethod.SelectedIndex = (int)AuthMethod.Certificate;
                        UpdateAuthenticationUIState();
                    }
                }
            }
        }

        private void chkUseTOTP_CheckedChanged(object sender, EventArgs e)
        {
            txtTOTPSecret.Enabled = chkUseTOTP.Checked;
            lblTOTPSecret.Enabled = chkUseTOTP.Checked;
        }

        private void btnAdvanced_Click(object sender, EventArgs e)
        {
            if (pnlAdvanced.Visible)
            {
                pnlAdvanced.Visible = false;
                btnAdvanced.Text = "Advanced >>";
            }
            else
            {
                pnlAdvanced.Visible = true;
                btnAdvanced.Text = "Advanced <<";
            }
        }

        private void btnGenerateTOTP_Click(object sender, EventArgs e)
        {
            // Generate a random Base32 secret
            Random random = new Random();
            const string base32Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
            char[] secretChars = new char[32]; // 32 characters = 160 bits

            for (int i = 0; i < secretChars.Length; i++)
            {
                secretChars[i] = base32Chars[random.Next(base32Chars.Length)];
            }

            txtTOTPSecret.Text = new string(secretChars);
            chkUseTOTP.Checked = true;

            MessageBox.Show("A new TOTP secret has been generated. You'll need to configure this in your authenticator app.",
                "TOTP Secret Generated", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // Note: InitializeComponent method would be here with the designer code
        // It would create all the UI controls, including the new authentication options
        private void InitializeComponent()
        {
            this.lblName = new System.Windows.Forms.Label();
            this.txtName = new System.Windows.Forms.TextBox();
            this.lblServerHost = new System.Windows.Forms.Label();
            this.txtServerHost = new System.Windows.Forms.TextBox();
            this.lblServerPort = new System.Windows.Forms.Label();
            this.numServerPort = new System.Windows.Forms.NumericUpDown();
            this.lblUsername = new System.Windows.Forms.Label();
            this.txtUsername = new System.Windows.Forms.TextBox();

            // Authentication method selector
            this.lblAuthMethod = new System.Windows.Forms.Label();
            this.cmbAuthMethod = new System.Windows.Forms.ComboBox();

            // Password authentication panel
            this.pnlPasswordAuth = new System.Windows.Forms.Panel();
            this.lblPassword = new System.Windows.Forms.Label();
            this.txtPassword = new System.Windows.Forms.TextBox();

            // Private key authentication panel
            this.pnlPrivateKeyAuth = new System.Windows.Forms.Panel();
            this.lblPrivateKeyPath = new System.Windows.Forms.Label();
            this.txtPrivateKeyPath = new System.Windows.Forms.TextBox();
            this.btnBrowsePrivateKey = new System.Windows.Forms.Button();
            this.lblPrivateKeyPassphrase = new System.Windows.Forms.Label();
            this.txtPrivateKeyPassphrase = new System.Windows.Forms.TextBox();
            this.btnGenerateKeyPair = new System.Windows.Forms.Button();

            // Certificate authentication panel
            this.pnlCertificateAuth = new System.Windows.Forms.Panel();
            this.lblCertificate = new System.Windows.Forms.Label();
            this.cmbCertificate = new System.Windows.Forms.ComboBox();
            this.btnImportCertificate = new System.Windows.Forms.Button();

            // TOTP panel
            this.pnlTOTP = new System.Windows.Forms.Panel();
            this.chkUseTOTP = new System.Windows.Forms.CheckBox();
            this.lblTOTPSecret = new System.Windows.Forms.Label();
            this.txtTOTPSecret = new System.Windows.Forms.TextBox();
            this.btnGenerateTOTP = new System.Windows.Forms.Button();

            // Port forwarding settings
            this.lblLocalPort = new System.Windows.Forms.Label();
            this.numLocalPort = new System.Windows.Forms.NumericUpDown();
            this.lblRemoteHost = new System.Windows.Forms.Label();
            this.txtRemoteHost = new System.Windows.Forms.TextBox();
            this.lblRemotePort = new System.Windows.Forms.Label();
            this.numRemotePort = new System.Windows.Forms.NumericUpDown();
            this.chkUseEncryption = new System.Windows.Forms.CheckBox();

            // Advanced settings
            this.btnAdvanced = new System.Windows.Forms.Button();
            this.pnlAdvanced = new System.Windows.Forms.Panel();
            this.lblConnectionTimeout = new System.Windows.Forms.Label();
            this.numConnectionTimeout = new System.Windows.Forms.NumericUpDown();
            this.lblKeepAliveInterval = new System.Windows.Forms.Label();
            this.numKeepAliveInterval = new System.Windows.Forms.NumericUpDown();
            this.chkEnableCompression = new System.Windows.Forms.CheckBox();
            this.lblSSHOptions = new System.Windows.Forms.Label();
            this.txtSSHOptions = new System.Windows.Forms.TextBox();

            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();

            // Initialize panels
            this.pnlPasswordAuth.SuspendLayout();
            this.pnlPrivateKeyAuth.SuspendLayout();
            this.pnlCertificateAuth.SuspendLayout();
            this.pnlTOTP.SuspendLayout();
            this.pnlAdvanced.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numServerPort)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numLocalPort)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numRemotePort)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numConnectionTimeout)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numKeepAliveInterval)).BeginInit();
            this.SuspendLayout();

            // lblName
            this.lblName.AutoSize = true;
            this.lblName.Location = new System.Drawing.Point(12, 15);
            this.lblName.Name = "lblName";
            this.lblName.Size = new System.Drawing.Size(38, 13);
            this.lblName.TabIndex = 0;
            this.lblName.Text = "Name:";

            // txtName
            this.txtName.Location = new System.Drawing.Point(140, 12);
            this.txtName.Name = "txtName";
            this.txtName.Size = new System.Drawing.Size(252, 20);
            this.txtName.TabIndex = 1;

            // lblServerHost
            this.lblServerHost.AutoSize = true;
            this.lblServerHost.Location = new System.Drawing.Point(12, 41);
            this.lblServerHost.Name = "lblServerHost";
            this.lblServerHost.Size = new System.Drawing.Size(69, 13);
            this.lblServerHost.TabIndex = 2;
            this.lblServerHost.Text = "Server Host:";

            // txtServerHost
            this.txtServerHost.Location = new System.Drawing.Point(140, 38);
            this.txtServerHost.Name = "txtServerHost";
            this.txtServerHost.Size = new System.Drawing.Size(252, 20);
            this.txtServerHost.TabIndex = 3;

            // lblServerPort
            this.lblServerPort.AutoSize = true;
            this.lblServerPort.Location = new System.Drawing.Point(12, 67);
            this.lblServerPort.Name = "lblServerPort";
            this.lblServerPort.Size = new System.Drawing.Size(66, 13);
            this.lblServerPort.TabIndex = 4;
            this.lblServerPort.Text = "Server Port:";

            // numServerPort
            this.numServerPort.Location = new System.Drawing.Point(140, 65);
            this.numServerPort.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this.numServerPort.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numServerPort.Name = "numServerPort";
            this.numServerPort.Size = new System.Drawing.Size(80, 20);
            this.numServerPort.TabIndex = 5;
            this.numServerPort.Value = new decimal(new int[] {
            22,
            0,
            0,
            0});

            // lblUsername
            this.lblUsername.AutoSize = true;
            this.lblUsername.Location = new System.Drawing.Point(12, 93);
            this.lblUsername.Name = "lblUsername";
            this.lblUsername.Size = new System.Drawing.Size(58, 13);
            this.lblUsername.TabIndex = 6;
            this.lblUsername.Text = "Username:";

            // txtUsername
            this.txtUsername.Location = new System.Drawing.Point(140, 90);
            this.txtUsername.Name = "txtUsername";
            this.txtUsername.Size = new System.Drawing.Size(252, 20);
            this.txtUsername.TabIndex = 7;

            // lblAuthMethod
            this.lblAuthMethod.AutoSize = true;
            this.lblAuthMethod.Location = new System.Drawing.Point(12, 119);
            this.lblAuthMethod.Name = "lblAuthMethod";
            this.lblAuthMethod.Size = new System.Drawing.Size(122, 13);
            this.lblAuthMethod.TabIndex = 8;
            this.lblAuthMethod.Text = "Authentication Method:";

            // cmbAuthMethod
            this.cmbAuthMethod.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbAuthMethod.FormattingEnabled = true;
            this.cmbAuthMethod.Items.AddRange(new object[] {
            "Password",
            "Private Key",
            "Certificate",
            "Keyboard Interactive"});
            this.cmbAuthMethod.Location = new System.Drawing.Point(140, 116);
            this.cmbAuthMethod.Name = "cmbAuthMethod";
            this.cmbAuthMethod.Size = new System.Drawing.Size(252, 21);
            this.cmbAuthMethod.TabIndex = 9;
            this.cmbAuthMethod.SelectedIndexChanged += new System.EventHandler(this.cmbAuthMethod_SelectedIndexChanged);

            // Password Authentication Panel
            this.pnlPasswordAuth.Controls.Add(this.lblPassword);
            this.pnlPasswordAuth.Controls.Add(this.txtPassword);
            this.pnlPasswordAuth.Location = new System.Drawing.Point(15, 143);
            this.pnlPasswordAuth.Name = "pnlPasswordAuth";
            this.pnlPasswordAuth.Size = new System.Drawing.Size(377, 35);
            this.pnlPasswordAuth.TabIndex = 10;

            // lblPassword
            this.lblPassword.AutoSize = true;
            this.lblPassword.Location = new System.Drawing.Point(0, 7);
            this.lblPassword.Name = "lblPassword";
            this.lblPassword.Size = new System.Drawing.Size(56, 13);
            this.lblPassword.TabIndex = 0;
            this.lblPassword.Text = "Password:";

            // txtPassword
            this.txtPassword.Location = new System.Drawing.Point(125, 4);
            this.txtPassword.Name = "txtPassword";
            this.txtPassword.PasswordChar = '*';
            this.txtPassword.Size = new System.Drawing.Size(252, 20);
            this.txtPassword.TabIndex = 1;

            // Private Key Authentication Panel
            this.pnlPrivateKeyAuth.Controls.Add(this.lblPrivateKeyPath);
            this.pnlPrivateKeyAuth.Controls.Add(this.txtPrivateKeyPath);
            this.pnlPrivateKeyAuth.Controls.Add(this.btnBrowsePrivateKey);
            this.pnlPrivateKeyAuth.Controls.Add(this.lblPrivateKeyPassphrase);
            this.pnlPrivateKeyAuth.Controls.Add(this.txtPrivateKeyPassphrase);
            this.pnlPrivateKeyAuth.Controls.Add(this.btnGenerateKeyPair);
            this.pnlPrivateKeyAuth.Location = new System.Drawing.Point(15, 143);
            this.pnlPrivateKeyAuth.Name = "pnlPrivateKeyAuth";
            this.pnlPrivateKeyAuth.Size = new System.Drawing.Size(377, 70);
            this.pnlPrivateKeyAuth.TabIndex = 11;
            this.pnlPrivateKeyAuth.Visible = false;

            // lblPrivateKeyPath
            this.lblPrivateKeyPath.AutoSize = true;
            this.lblPrivateKeyPath.Location = new System.Drawing.Point(0, 7);
            this.lblPrivateKeyPath.Name = "lblPrivateKeyPath";
            this.lblPrivateKeyPath.Size = new System.Drawing.Size(88, 13);
            this.lblPrivateKeyPath.TabIndex = 0;
            this.lblPrivateKeyPath.Text = "Private Key Path:";

            // txtPrivateKeyPath
            this.txtPrivateKeyPath.Location = new System.Drawing.Point(125, 4);
            this.txtPrivateKeyPath.Name = "txtPrivateKeyPath";
            this.txtPrivateKeyPath.Size = new System.Drawing.Size(207, 20);
            this.txtPrivateKeyPath.TabIndex = 1;

            // btnBrowsePrivateKey
            this.btnBrowsePrivateKey.Location = new System.Drawing.Point(338, 3);
            this.btnBrowsePrivateKey.Name = "btnBrowsePrivateKey";
            this.btnBrowsePrivateKey.Size = new System.Drawing.Size(39, 23);
            this.btnBrowsePrivateKey.TabIndex = 2;
            this.btnBrowsePrivateKey.Text = "...";
            this.btnBrowsePrivateKey.UseVisualStyleBackColor = true;
            this.btnBrowsePrivateKey.Click += new System.EventHandler(this.btnBrowsePrivateKey_Click);

            // lblPrivateKeyPassphrase
            this.lblPrivateKeyPassphrase.AutoSize = true;
            this.lblPrivateKeyPassphrase.Location = new System.Drawing.Point(0, 36);
            this.lblPrivateKeyPassphrase.Name = "lblPrivateKeyPassphrase";
            this.lblPrivateKeyPassphrase.Size = new System.Drawing.Size(118, 13);
            this.lblPrivateKeyPassphrase.TabIndex = 3;
            this.lblPrivateKeyPassphrase.Text = "Private Key Passphrase:";

            // txtPrivateKeyPassphrase
            this.txtPrivateKeyPassphrase.Location = new System.Drawing.Point(125, 33);
            this.txtPrivateKeyPassphrase.Name = "txtPrivateKeyPassphrase";
            this.txtPrivateKeyPassphrase.PasswordChar = '*';
            this.txtPrivateKeyPassphrase.Size = new System.Drawing.Size(207, 20);
            this.txtPrivateKeyPassphrase.TabIndex = 4;

            // btnGenerateKeyPair
            this.btnGenerateKeyPair.Location = new System.Drawing.Point(338, 32);
            this.btnGenerateKeyPair.Name = "btnGenerateKeyPair";
            this.btnGenerateKeyPair.Size = new System.Drawing.Size(39, 23);
            this.btnGenerateKeyPair.TabIndex = 5;
            this.btnGenerateKeyPair.Text = "New";
            this.btnGenerateKeyPair.UseVisualStyleBackColor = true;
            this.btnGenerateKeyPair.Click += new System.EventHandler(this.btnGenerateKeyPair_Click);

            // Certificate Authentication Panel
            this.pnlCertificateAuth.Controls.Add(this.lblCertificate);
            this.pnlCertificateAuth.Controls.Add(this.cmbCertificate);
            this.pnlCertificateAuth.Controls.Add(this.btnImportCertificate);
            this.pnlCertificateAuth.Location = new System.Drawing.Point(15, 143);
            this.pnlCertificateAuth.Name = "pnlCertificateAuth";
            this.pnlCertificateAuth.Size = new System.Drawing.Size(377, 35);
            this.pnlCertificateAuth.TabIndex = 12;
            this.pnlCertificateAuth.Visible = false;

            // lblCertificate
            this.lblCertificate.AutoSize = true;
            this.lblCertificate.Location = new System.Drawing.Point(0, 7);
            this.lblCertificate.Name = "lblCertificate";
            this.lblCertificate.Size = new System.Drawing.Size(60, 13);
            this.lblCertificate.TabIndex = 0;
            this.lblCertificate.Text = "Certificate:";

            // cmbCertificate
            this.cmbCertificate.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbCertificate.FormattingEnabled = true;
            this.cmbCertificate.Location = new System.Drawing.Point(125, 4);
            this.cmbCertificate.Name = "cmbCertificate";
            this.cmbCertificate.Size = new System.Drawing.Size(207, 21);
            this.cmbCertificate.TabIndex = 1;

            // btnImportCertificate
            this.btnImportCertificate.Location = new System.Drawing.Point(338, 3);
            this.btnImportCertificate.Name = "btnImportCertificate";
            this.btnImportCertificate.Size = new System.Drawing.Size(39, 23);
            this.btnImportCertificate.TabIndex = 2;
            this.btnImportCertificate.Text = "...";
            this.btnImportCertificate.UseVisualStyleBackColor = true;
            this.btnImportCertificate.Click += new System.EventHandler(this.btnImportCertificate_Click);

            // TOTP Panel
            this.pnlTOTP.Controls.Add(this.chkUseTOTP);
            this.pnlTOTP.Controls.Add(this.lblTOTPSecret);
            this.pnlTOTP.Controls.Add(this.txtTOTPSecret);
            this.pnlTOTP.Controls.Add(this.btnGenerateTOTP);
            this.pnlTOTP.Location = new System.Drawing.Point(15, 220);
            this.pnlTOTP.Name = "pnlTOTP";
            this.pnlTOTP.Size = new System.Drawing.Size(377, 60);
            this.pnlTOTP.TabIndex = 13;

            // chkUseTOTP
            this.chkUseTOTP.AutoSize = true;
            this.chkUseTOTP.Location = new System.Drawing.Point(3, 3);
            this.chkUseTOTP.Name = "chkUseTOTP";
            this.chkUseTOTP.Size = new System.Drawing.Size(202, 17);
            this.chkUseTOTP.TabIndex = 0;
            this.chkUseTOTP.Text = "Use Two-Factor Authentication (TOTP)";
            this.chkUseTOTP.UseVisualStyleBackColor = true;
            this.chkUseTOTP.CheckedChanged += new System.EventHandler(this.chkUseTOTP_CheckedChanged);

            // lblTOTPSecret
            this.lblTOTPSecret.AutoSize = true;
            this.lblTOTPSecret.Enabled = false;
            this.lblTOTPSecret.Location = new System.Drawing.Point(0, 30);
            this.lblTOTPSecret.Name = "lblTOTPSecret";
            this.lblTOTPSecret.Size = new System.Drawing.Size(73, 13);
            this.lblTOTPSecret.TabIndex = 1;
            this.lblTOTPSecret.Text = "TOTP Secret:";

            // txtTOTPSecret
            this.txtTOTPSecret.Enabled = false;
            this.txtTOTPSecret.Location = new System.Drawing.Point(125, 27);
            this.txtTOTPSecret.Name = "txtTOTPSecret";
            this.txtTOTPSecret.Size = new System.Drawing.Size(207, 20);
            this.txtTOTPSecret.TabIndex = 2;

            // btnGenerateTOTP
            this.btnGenerateTOTP.Location = new System.Drawing.Point(338, 26);
            this.btnGenerateTOTP.Name = "btnGenerateTOTP";
            this.btnGenerateTOTP.Size = new System.Drawing.Size(39, 23);
            this.btnGenerateTOTP.TabIndex = 3;
            this.btnGenerateTOTP.Text = "Gen";
            this.btnGenerateTOTP.UseVisualStyleBackColor = true;
            this.btnGenerateTOTP.Click += new System.EventHandler(this.btnGenerateTOTP_Click);

            // Port Forwarding Settings
            // lblLocalPort
            this.lblLocalPort.AutoSize = true;
            this.lblLocalPort.Location = new System.Drawing.Point(12, 290);
            this.lblLocalPort.Name = "lblLocalPort";
            this.lblLocalPort.Size = new System.Drawing.Size(58, 13);
            this.lblLocalPort.TabIndex = 14;
            this.lblLocalPort.Text = "Local Port:";

            // numLocalPort
            this.numLocalPort.Location = new System.Drawing.Point(140, 288);
            this.numLocalPort.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this.numLocalPort.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numLocalPort.Name = "numLocalPort";
            this.numLocalPort.Size = new System.Drawing.Size(80, 20);
            this.numLocalPort.TabIndex = 15;
            this.numLocalPort.Value = new decimal(new int[] {
            8080,
            0,
            0,
            0});

            // lblRemoteHost
            this.lblRemoteHost.AutoSize = true;
            this.lblRemoteHost.Location = new System.Drawing.Point(12, 316);
            this.lblRemoteHost.Name = "lblRemoteHost";
            this.lblRemoteHost.Size = new System.Drawing.Size(73, 13);
            this.lblRemoteHost.TabIndex = 16;
            this.lblRemoteHost.Text = "Remote Host:";

            // txtRemoteHost
            this.txtRemoteHost.Location = new System.Drawing.Point(140, 313);
            this.txtRemoteHost.Name = "txtRemoteHost";
            this.txtRemoteHost.Size = new System.Drawing.Size(252, 20);
            this.txtRemoteHost.TabIndex = 17;

            // lblRemotePort
            this.lblRemotePort.AutoSize = true;
            this.lblRemotePort.Location = new System.Drawing.Point(12, 342);
            this.lblRemotePort.Name = "lblRemotePort";
            this.lblRemotePort.Size = new System.Drawing.Size(70, 13);
            this.lblRemotePort.TabIndex = 18;
            this.lblRemotePort.Text = "Remote Port:";

            // numRemotePort
            this.numRemotePort.Location = new System.Drawing.Point(140, 340);
            this.numRemotePort.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this.numRemotePort.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numRemotePort.Name = "numRemotePort";
            this.numRemotePort.Size = new System.Drawing.Size(80, 20);
            this.numRemotePort.TabIndex = 19;
            this.numRemotePort.Value = new decimal(new int[] {
            80,
            0,
            0,
            0});

            // chkUseEncryption
            this.chkUseEncryption.AutoSize = true;
            this.chkUseEncryption.Location = new System.Drawing.Point(140, 366);
            this.chkUseEncryption.Name = "chkUseEncryption";
            this.chkUseEncryption.Size = new System.Drawing.Size(161, 17);
            this.chkUseEncryption.TabIndex = 20;
            this.chkUseEncryption.Text = "Use Additional Encryption";
            this.chkUseEncryption.UseVisualStyleBackColor = true;

            // Advanced Button
            this.btnAdvanced.Location = new System.Drawing.Point(15, 390);
            this.btnAdvanced.Name = "btnAdvanced";
            this.btnAdvanced.Size = new System.Drawing.Size(110, 23);
            this.btnAdvanced.TabIndex = 21;
            this.btnAdvanced.Text = "Advanced >>";
            this.btnAdvanced.UseVisualStyleBackColor = true;
            this.btnAdvanced.Click += new System.EventHandler(this.btnAdvanced_Click);

            // Advanced Panel
            this.pnlAdvanced.Controls.Add(this.lblConnectionTimeout);
            this.pnlAdvanced.Controls.Add(this.numConnectionTimeout);
            this.pnlAdvanced.Controls.Add(this.lblKeepAliveInterval);
            this.pnlAdvanced.Controls.Add(this.numKeepAliveInterval);
            this.pnlAdvanced.Controls.Add(this.chkEnableCompression);
            this.pnlAdvanced.Controls.Add(this.lblSSHOptions);
            this.pnlAdvanced.Controls.Add(this.txtSSHOptions);
            this.pnlAdvanced.Location = new System.Drawing.Point(15, 419);
            this.pnlAdvanced.Name = "pnlAdvanced";
            this.pnlAdvanced.Size = new System.Drawing.Size(377, 105);
            this.pnlAdvanced.TabIndex = 22;
            this.pnlAdvanced.Visible = false;

            // lblConnectionTimeout
            this.lblConnectionTimeout.AutoSize = true;
            this.lblConnectionTimeout.Location = new System.Drawing.Point(0, 7);
            this.lblConnectionTimeout.Name = "lblConnectionTimeout";
            this.lblConnectionTimeout.Size = new System.Drawing.Size(108, 13);
            this.lblConnectionTimeout.TabIndex = 0;
            this.lblConnectionTimeout.Text = "Connection Timeout:";

            // numConnectionTimeout
            this.numConnectionTimeout.Location = new System.Drawing.Point(125, 5);
            this.numConnectionTimeout.Maximum = new decimal(new int[] {
            300,
            0,
            0,
            0});
            this.numConnectionTimeout.Minimum = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.numConnectionTimeout.Name = "numConnectionTimeout";
            this.numConnectionTimeout.Size = new System.Drawing.Size(60, 20);
            this.numConnectionTimeout.TabIndex = 1;
            this.numConnectionTimeout.Value = new decimal(new int[] {
            30,
            0,
            0,
            0});

            // lblKeepAliveInterval
            this.lblKeepAliveInterval.AutoSize = true;
            this.lblKeepAliveInterval.Location = new System.Drawing.Point(0, 33);
            this.lblKeepAliveInterval.Name = "lblKeepAliveInterval";
            this.lblKeepAliveInterval.Size = new System.Drawing.Size(97, 13);
            this.lblKeepAliveInterval.TabIndex = 2;
            this.lblKeepAliveInterval.Text = "Keep Alive Interval:";

            // numKeepAliveInterval
            this.numKeepAliveInterval.Location = new System.Drawing.Point(125, 31);
            this.numKeepAliveInterval.Maximum = new decimal(new int[] {
            3600,
            0,
            0,
            0});
            this.numKeepAliveInterval.Minimum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.numKeepAliveInterval.Name = "numKeepAliveInterval";
            this.numKeepAliveInterval.Size = new System.Drawing.Size(60, 20);
            this.numKeepAliveInterval.TabIndex = 3;
            this.numKeepAliveInterval.Value = new decimal(new int[] {
            60,
            0,
            0,
            0});

            // chkEnableCompression
            this.chkEnableCompression.AutoSize = true;
            this.chkEnableCompression.Location = new System.Drawing.Point(125, 57);
            this.chkEnableCompression.Name = "chkEnableCompression";
            this.chkEnableCompression.Size = new System.Drawing.Size(124, 17);
            this.chkEnableCompression.TabIndex = 4;
            this.chkEnableCompression.Text = "Enable Compression";
            this.chkEnableCompression.UseVisualStyleBackColor = true;

            // lblSSHOptions
            this.lblSSHOptions.AutoSize = true;
            this.lblSSHOptions.Location = new System.Drawing.Point(0, 82);
            this.lblSSHOptions.Name = "lblSSHOptions";
            this.lblSSHOptions.Size = new System.Drawing.Size(107, 13);
            this.lblSSHOptions.TabIndex = 5;
            this.lblSSHOptions.Text = "SSH Command Line:";

            // txtSSHOptions
            this.txtSSHOptions.Location = new System.Drawing.Point(125, 79);
            this.txtSSHOptions.Name = "txtSSHOptions";
            this.txtSSHOptions.Size = new System.Drawing.Size(252, 20);
            this.txtSSHOptions.TabIndex = 6;

            // OK/Cancel Buttons
            this.btnOK.Location = new System.Drawing.Point(216, 530);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 23);
            this.btnOK.TabIndex = 23;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);

            // btnCancel
            this.btnCancel.Location = new System.Drawing.Point(297, 530);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 24;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);

            // ConfigurationForm
            this.AcceptButton = this.btnOK;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(404, 565);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.pnlAdvanced);
            this.Controls.Add(this.btnAdvanced);
            this.Controls.Add(this.chkUseEncryption);
            this.Controls.Add(this.numRemotePort);
            this.Controls.Add(this.lblRemotePort);
            this.Controls.Add(this.txtRemoteHost);
            this.Controls.Add(this.lblRemoteHost);
            this.Controls.Add(this.numLocalPort);
            this.Controls.Add(this.lblLocalPort);
            this.Controls.Add(this.pnlTOTP);
            this.Controls.Add(this.pnlCertificateAuth);
            this.Controls.Add(this.pnlPrivateKeyAuth);
            this.Controls.Add(this.pnlPasswordAuth);
            this.Controls.Add(this.cmbAuthMethod);
            this.Controls.Add(this.lblAuthMethod);
            this.Controls.Add(this.txtUsername);
            this.Controls.Add(this.lblUsername);
            this.Controls.Add(this.numServerPort);
            this.Controls.Add(this.lblServerPort);
            this.Controls.Add(this.txtServerHost);
            this.Controls.Add(this.lblServerHost);
            this.Controls.Add(this.txtName);
            this.Controls.Add(this.lblName);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ConfigurationForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "SSH Tunnel Configuration";
            this.pnlPasswordAuth.ResumeLayout(false);
            this.pnlPasswordAuth.PerformLayout();
            this.pnlPrivateKeyAuth.ResumeLayout(false);
            this.pnlPrivateKeyAuth.PerformLayout();
            this.pnlCertificateAuth.ResumeLayout(false);
            this.pnlCertificateAuth.PerformLayout();
            this.pnlTOTP.ResumeLayout(false);
            this.pnlTOTP.PerformLayout();
            this.pnlAdvanced.ResumeLayout(false);
            this.pnlAdvanced.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numServerPort)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numLocalPort)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numRemotePort)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numConnectionTimeout)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numKeepAliveInterval)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.Label lblName;
        private System.Windows.Forms.TextBox txtName;
        private System.Windows.Forms.Label lblServerHost;
        private System.Windows.Forms.TextBox txtServerHost;
        private System.Windows.Forms.Label lblServerPort;
        private System.Windows.Forms.NumericUpDown numServerPort;
        private System.Windows.Forms.Label lblUsername;
        private System.Windows.Forms.TextBox txtUsername;
        private System.Windows.Forms.Label lblAuthMethod;
        private System.Windows.Forms.ComboBox cmbAuthMethod;

        // Password Auth Panel
        private System.Windows.Forms.Panel pnlPasswordAuth;
        private System.Windows.Forms.Label lblPassword;
        private System.Windows.Forms.TextBox txtPassword;

        // Private Key Auth Panel
        private System.Windows.Forms.Panel pnlPrivateKeyAuth;
        private System.Windows.Forms.Label lblPrivateKeyPath;
        private System.Windows.Forms.TextBox txtPrivateKeyPath;
        private System.Windows.Forms.Button btnBrowsePrivateKey;
        private System.Windows.Forms.Label lblPrivateKeyPassphrase;
        private System.Windows.Forms.TextBox txtPrivateKeyPassphrase;
        private System.Windows.Forms.Button btnGenerateKeyPair;

        // Certificate Auth Panel
        private System.Windows.Forms.Panel pnlCertificateAuth;
        private System.Windows.Forms.Label lblCertificate;
        private System.Windows.Forms.ComboBox cmbCertificate;
        private System.Windows.Forms.Button btnImportCertificate;

        // TOTP Panel
        private System.Windows.Forms.Panel pnlTOTP;
        private System.Windows.Forms.CheckBox chkUseTOTP;
        private System.Windows.Forms.Label lblTOTPSecret;
        private System.Windows.Forms.TextBox txtTOTPSecret;
        private System.Windows.Forms.Button btnGenerateTOTP;

        // Port Forwarding Settings
        private System.Windows.Forms.Label lblLocalPort;
        private System.Windows.Forms.NumericUpDown numLocalPort;
        private System.Windows.Forms.Label lblRemoteHost;
        private System.Windows.Forms.TextBox txtRemoteHost;
        private System.Windows.Forms.Label lblRemotePort;
        private System.Windows.Forms.NumericUpDown numRemotePort;
        private System.Windows.Forms.CheckBox chkUseEncryption;

        // Advanced Settings
        private System.Windows.Forms.Button btnAdvanced;
        private System.Windows.Forms.Panel pnlAdvanced;
        private System.Windows.Forms.Label lblConnectionTimeout;
        private System.Windows.Forms.NumericUpDown numConnectionTimeout;
        private System.Windows.Forms.Label lblKeepAliveInterval;
        private System.Windows.Forms.NumericUpDown numKeepAliveInterval;
        private System.Windows.Forms.CheckBox chkEnableCompression;
        private System.Windows.Forms.Label lblSSHOptions;
        private System.Windows.Forms.TextBox txtSSHOptions;

        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;
    }
}

// Password Prompt Form for Certificate Import
public class PasswordPromptForm : Form
{
    public string Password { get; private set; }

    public PasswordPromptForm(string title)
    {
        InitializeComponent();
        this.Text = title;
    }

    private void btnOK_Click(object sender, EventArgs e)
    {
        Password = txtPassword.Text;
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
        this.lblPrompt = new System.Windows.Forms.Label();
        this.txtPassword = new System.Windows.Forms.TextBox();
        this.btnOK = new System.Windows.Forms.Button();
        this.btnCancel = new System.Windows.Forms.Button();
        this.SuspendLayout();

        // lblPrompt
        this.lblPrompt.AutoSize = true;
        this.lblPrompt.Location = new System.Drawing.Point(12, 15);
        this.lblPrompt.Name = "lblPrompt";
        this.lblPrompt.Size = new System.Drawing.Size(56, 13);
        this.lblPrompt.TabIndex = 0;
        this.lblPrompt.Text = "Password:";

        // txtPassword
        this.txtPassword.Location = new System.Drawing.Point(74, 12);
        this.txtPassword.Name = "txtPassword";
        this.txtPassword.PasswordChar = '*';
        this.txtPassword.Size = new System.Drawing.Size(198, 20);
        this.txtPassword.TabIndex = 1;

        // btnOK
        this.btnOK.Location = new System.Drawing.Point(116, 45);
        this.btnOK.Name = "btnOK";
        this.btnOK.Size = new System.Drawing.Size(75, 23);
        this.btnOK.TabIndex = 2;
        this.btnOK.Text = "OK";
        this.btnOK.UseVisualStyleBackColor = true;
        this.btnOK.Click += new System.EventHandler(this.btnOK_Click);

        // btnCancel
        this.btnCancel.Location = new System.Drawing.Point(197, 45);
        this.btnCancel.Name = "btnCancel";
        this.btnCancel.Size = new System.Drawing.Size(75, 23);
        this.btnCancel.TabIndex = 3;
        this.btnCancel.Text = "Cancel";
        this.btnCancel.UseVisualStyleBackColor = true;
        this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);

        // PasswordPromptForm
        this.AcceptButton = this.btnOK;
        this.CancelButton = this.btnCancel;
        this.ClientSize = new System.Drawing.Size(284, 80);
        this.Controls.Add(this.btnCancel);
        this.Controls.Add(this.btnOK);
        this.Controls.Add(this.txtPassword);
        this.Controls.Add(this.lblPrompt);
        this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.Name = "PasswordPromptForm";
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    private System.Windows.Forms.Label lblPrompt;
    private System.Windows.Forms.TextBox txtPassword;
    private System.Windows.Forms.Button btnOK;
    private System.Windows.Forms.Button btnCancel;
}

// Key Generation Form
public class KeyGenerationForm : Form
{
    public string GeneratedKeyPath { get; private set; }
    private AuthenticationManager _authManager;

    public KeyGenerationForm()
    {
        InitializeComponent();
        _authManager = new AuthenticationManager();
    }

    private void btnGenerate_Click(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtKeyName.Text))
        {
            MessageBox.Show("Please enter a name for the key pair.", "Validation Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        // Generate key pair
        bool success = _authManager.GenerateSSHKeyPair(
            txtKeyName.Text,
            txtPassphrase.Text,
            (int)numKeySize.Value);

        if (success)
        {
            string appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SSHTunnelClient", "key_store");

            GeneratedKeyPath = Path.Combine(appDataPath, $"{txtKeyName.Text}.pem");

            MessageBox.Show("Key pair generated successfully.\r\n\r\n" +
                "Public key has been saved as:\r\n" +
                $"{Path.Combine(appDataPath, $"{txtKeyName.Text}.pub")}\r\n\r\n" +
                "Copy this to your server's authorized_keys file.",
                "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

            DialogResult = DialogResult.OK;
            Close();
        }
    }

    private void btnCancel_Click(object sender, EventArgs e)
    {
        DialogResult = DialogResult.Cancel;
        Close();
    }

    private void InitializeComponent()
    {
        this.lblKeyName = new System.Windows.Forms.Label();
        this.txtKeyName = new System.Windows.Forms.TextBox();
        this.lblKeySize = new System.Windows.Forms.Label();
        this.numKeySize = new System.Windows.Forms.NumericUpDown();
        this.lblPassphrase = new System.Windows.Forms.Label();
        this.txtPassphrase = new System.Windows.Forms.TextBox();
        this.btnGenerate = new System.Windows.Forms.Button();
        this.btnCancel = new System.Windows.Forms.Button();
        ((System.ComponentModel.ISupportInitialize)(this.numKeySize)).BeginInit();
        this.SuspendLayout();

        // lblKeyName
        this.lblKeyName.AutoSize = true;
        this.lblKeyName.Location = new System.Drawing.Point(12, 15);
        this.lblKeyName.Name = "lblKeyName";
        this.lblKeyName.Size = new System.Drawing.Size(63, 13);
        this.lblKeyName.TabIndex = 0;
        this.lblKeyName.Text = "Key Name:";

        // txtKeyName
        this.txtKeyName.Location = new System.Drawing.Point(120, 12);
        this.txtKeyName.Name = "txtKeyName";
        this.txtKeyName.Size = new System.Drawing.Size(252, 20);
        this.txtKeyName.TabIndex = 1;

        // lblKeySize
        this.lblKeySize.AutoSize = true;
        this.lblKeySize.Location = new System.Drawing.Point(12, 41);
        this.lblKeySize.Name = "lblKeySize";
        this.lblKeySize.Size = new System.Drawing.Size(54, 13);
        this.lblKeySize.TabIndex = 2;
        this.lblKeySize.Text = "Key Size:";

        // numKeySize
        this.numKeySize.Location = new System.Drawing.Point(120, 39);
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

        // lblPassphrase
        this.lblPassphrase.AutoSize = true;
        this.lblPassphrase.Location = new System.Drawing.Point(12, 67);
        this.lblPassphrase.Name = "lblPassphrase";
        this.lblPassphrase.Size = new System.Drawing.Size(67, 13);
        this.lblPassphrase.TabIndex = 4;
        this.lblPassphrase.Text = "Passphrase:";

        // txtPassphrase
        this.txtPassphrase.Location = new System.Drawing.Point(120, 64);
        this.txtPassphrase.Name = "txtPassphrase";
        this.txtPassphrase.PasswordChar = '*';
        this.txtPassphrase.Size = new System.Drawing.Size(252, 20);
        this.txtPassphrase.TabIndex = 5;

        // btnGenerate
        this.btnGenerate.Location = new System.Drawing.Point(216, 100);
        this.btnGenerate.Name = "btnGenerate";
        this.btnGenerate.Size = new System.Drawing.Size(75, 23);
        this.btnGenerate.TabIndex = 6;
        this.btnGenerate.Text = "Generate";
        this.btnGenerate.UseVisualStyleBackColor = true;
        this.btnGenerate.Click += new System.EventHandler(this.btnGenerate_Click);

        // btnCancel
        this.btnCancel.Location = new System.Drawing.Point(297, 100);
        this.btnCancel.Name = "btnCancel";
        this.btnCancel.Size = new System.Drawing.Size(75, 23);
        this.btnCancel.TabIndex = 7;
        this.btnCancel.Text = "Cancel";
        this.btnCancel.UseVisualStyleBackColor = true;
        this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);

        // KeyGenerationForm
        this.AcceptButton = this.btnGenerate;
        this.CancelButton = this.btnCancel;
        this.ClientSize = new System.Drawing.Size(384, 135);
        this.Controls.Add(this.btnCancel);
        this.Controls.Add(this.btnGenerate);
        this.Controls.Add(this.txtPassphrase);
        this.Controls.Add(this.lblPassphrase);
        this.Controls.Add(this.numKeySize);
        this.Controls.Add(this.lblKeySize);
        this.Controls.Add(this.txtKeyName);
        this.Controls.Add(this.lblKeyName);
        this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.Name = "KeyGenerationForm";
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        this.Text = "Generate SSH Key Pair";
        ((System.ComponentModel.ISupportInitialize)(this.numKeySize)).EndInit();
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    private System.Windows.Forms.Label lblKeyName;
    private System.Windows.Forms.TextBox txtKeyName;
    private System.Windows.Forms.Label lblKeySize;
    private System.Windows.Forms.NumericUpDown numKeySize;
    private System.Windows.Forms.Label lblPassphrase;
    private System.Windows.Forms.TextBox txtPassphrase;
    private System.Windows.Forms.Button btnGenerate;
    private System.Windows.Forms.Button btnCancel;
}