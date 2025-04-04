using System;
using System.Windows.Forms;
using System.Threading.Tasks;

namespace SSHTunnelClient
{
    // Form for configuring YubiKey
    public partial class YubiKeyConfigForm : Form
    {
        private YubiKeyManager _yubiKeyManager;
        public string YubiKeyPIN { get; private set; }
        public string YubiKeyProvider { get; private set; }
        public YubiKeyMode SelectedMode { get; private set; }

        public YubiKeyConfigForm()
        {
            InitializeComponent();
            _yubiKeyManager = new YubiKeyManager();

            // Initialize mode selection
            cmbYubiKeyMode.Items.Add("PIV (Smart Card)");
            cmbYubiKeyMode.Items.Add("OATH-TOTP");
            cmbYubiKeyMode.Items.Add("Challenge-Response");
            cmbYubiKeyMode.SelectedIndex = 0; // Default to PIV

            // Check if YubiKey is present
            CheckYubiKeyStatus();
        }

        private async void CheckYubiKeyStatus()
        {
            btnConfigure.Enabled = false;
            lblStatus.Text = "Checking for YubiKey...";

            // Use Task.Run to avoid UI freezing
            bool isPresent = await Task.Run(() => _yubiKeyManager.IsYubiKeyPresent());

            if (isPresent)
            {
                lblStatus.Text = "YubiKey detected";

                // Get YubiKey info
                YubiKeyInfo info = await Task.Run(() => _yubiKeyManager.GetYubiKeyInfo());
                txtDeviceInfo.Text = $"Device: {info.DeviceType}\r\nSerial: {info.SerialNumber}\r\nFirmware: {info.FirmwareVersion}";

                // Check for SSH support
                bool hasSSH = await Task.Run(() => _yubiKeyManager.HasSSHSupport());
                lblSSHSupport.Text = hasSSH ? "SSH Support: Yes" : "SSH Support: No (will be configured)";

                btnConfigure.Enabled = true;
            }
            else
            {
                lblStatus.Text = "No YubiKey detected. Please insert your YubiKey.";
                txtDeviceInfo.Text = "No device detected";
                lblSSHSupport.Text = "SSH Support: N/A";
            }
        }

        private void cmbYubiKeyMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Enable different options based on selected mode
            switch (cmbYubiKeyMode.SelectedIndex)
            {
                case 0: // PIV
                    lblPIN.Visible = true;
                    txtPIN.Visible = true;
                    lblPIN.Text = "PIV PIN:";
                    break;

                case 1: // OATH-TOTP
                    lblPIN.Visible = true;
                    txtPIN.Visible = true;
                    lblPIN.Text = "OATH PIN:";
                    break;

                case 2: // Challenge-Response
                    lblPIN.Visible = false;
                    txtPIN.Visible = false;
                    break;
            }
        }

        private async void btnConfigure_Click(object sender, EventArgs e)
        {
            btnConfigure.Enabled = false;
            lblStatus.Text = "Configuring YubiKey...";

            try
            {
                switch (cmbYubiKeyMode.SelectedIndex)
                {
                    case 0: // PIV
                        if (string.IsNullOrEmpty(txtPIN.Text) || txtPIN.Text.Length < 6)
                        {
                            MessageBox.Show("PIV PIN must be at least 6 characters.", "Invalid PIN",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                            btnConfigure.Enabled = true;
                            lblStatus.Text = "Configuration failed. Invalid PIN.";
                            return;
                        }

                        // Configure YubiKey for SSH authentication
                        bool success = await Task.Run(() => _yubiKeyManager.ConfigureForSSH(txtPIN.Text));

                        if (success)
                        {
                            // Configure SSH agent
                            string provider = await Task.Run(() => _yubiKeyManager.ConfigureSSHAgent(txtPIN.Text));

                            if (!string.IsNullOrEmpty(provider))
                            {
                                YubiKeyPIN = txtPIN.Text;
                                YubiKeyProvider = provider;
                                SelectedMode = YubiKeyMode.PIV;

                                DialogResult = DialogResult.OK;
                                Close();
                            }
                            else
                            {
                                lblStatus.Text = "YubiKey configured but SSH agent setup failed.";
                                btnConfigure.Enabled = true;
                            }
                        }
                        else
                        {
                            lblStatus.Text = "YubiKey configuration failed.";
                            btnConfigure.Enabled = true;
                        }
                        break;

                    case 1: // OATH-TOTP
                        MessageBox.Show("OATH-TOTP is configured using the YubiKey Manager app.\n\n" +
                            "Please use the YubiKey Manager to add OATH credentials, then restart this application.",
                            "OATH-TOTP Configuration", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        SelectedMode = YubiKeyMode.OATH;
                        DialogResult = DialogResult.OK;
                        Close();
                        break;

                    case 2: // Challenge-Response
                        MessageBox.Show("Challenge-Response mode requires configuration with the YubiKey Personalization Tool.\n\n" +
                            "Please use the YubiKey Personalization Tool to configure HMAC-SHA1 Challenge-Response in slot 2, then restart this application.",
                            "Challenge-Response Configuration", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        SelectedMode = YubiKeyMode.ChallengeResponse;
                        DialogResult = DialogResult.OK;
                        Close();
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error configuring YubiKey: {ex.Message}", "Configuration Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblStatus.Text = "Configuration error occurred.";
                btnConfigure.Enabled = true;
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            CheckYubiKeyStatus();
        }

        private void InitializeComponent()
        {
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblStatus = new System.Windows.Forms.Label();
            this.txtDeviceInfo = new System.Windows.Forms.TextBox();
            this.lblSSHSupport = new System.Windows.Forms.Label();
            this.lblMode = new System.Windows.Forms.Label();
            this.cmbYubiKeyMode = new System.Windows.Forms.ComboBox();
            this.lblPIN = new System.Windows.Forms.Label();
            this.txtPIN = new System.Windows.Forms.TextBox();
            this.btnConfigure = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnRefresh = new System.Windows.Forms.Button();
            this.SuspendLayout();

            // lblTitle
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblTitle.Location = new System.Drawing.Point(12, 9);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(180, 16);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "YubiKey SSH Configuration";

            // lblStatus
            this.lblStatus.AutoSize = true;
            this.lblStatus.Location = new System.Drawing.Point(12, 35);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(123, 13);
            this.lblStatus.TabIndex = 1;
            this.lblStatus.Text = "Checking for YubiKey...";

            // txtDeviceInfo
            this.txtDeviceInfo.Location = new System.Drawing.Point(15, 60);
            this.txtDeviceInfo.Multiline = true;
            this.txtDeviceInfo.Name = "txtDeviceInfo";
            this.txtDeviceInfo.ReadOnly = true;
            this.txtDeviceInfo.Size = new System.Drawing.Size(300, 70);
            this.txtDeviceInfo.TabIndex = 2;

            // lblSSHSupport
            this.lblSSHSupport.AutoSize = true;
            this.lblSSHSupport.Location = new System.Drawing.Point(12, 140);
            this.lblSSHSupport.Name = "lblSSHSupport";
            this.lblSSHSupport.Size = new System.Drawing.Size(93, 13);
            this.lblSSHSupport.TabIndex = 3;
            this.lblSSHSupport.Text = "SSH Support: N/A";

            // lblMode
            this.lblMode.AutoSize = true;
            this.lblMode.Location = new System.Drawing.Point(12, 170);
            this.lblMode.Name = "lblMode";
            this.lblMode.Size = new System.Drawing.Size(86, 13);
            this.lblMode.TabIndex = 4;
            this.lblMode.Text = "YubiKey Mode:";

            // cmbYubiKeyMode
            this.cmbYubiKeyMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbYubiKeyMode.FormattingEnabled = true;
            this.cmbYubiKeyMode.Location = new System.Drawing.Point(105, 167);
            this.cmbYubiKeyMode.Name = "cmbYubiKeyMode";
            this.cmbYubiKeyMode.Size = new System.Drawing.Size(210, 21);
            this.cmbYubiKeyMode.TabIndex = 5;
            this.cmbYubiKeyMode.SelectedIndexChanged += new System.EventHandler(this.cmbYubiKeyMode_SelectedIndexChanged);

            // lblPIN
            this.lblPIN.AutoSize = true;
            this.lblPIN.Location = new System.Drawing.Point(12, 205);
            this.lblPIN.Name = "lblPIN";
            this.lblPIN.Size = new System.Drawing.Size(53, 13);
            this.lblPIN.TabIndex = 6;
            this.lblPIN.Text = "PIV PIN:";

            // txtPIN
            this.txtPIN.Location = new System.Drawing.Point(105, 202);
            this.txtPIN.Name = "txtPIN";
            this.txtPIN.PasswordChar = '*';
            this.txtPIN.Size = new System.Drawing.Size(210, 20);
            this.txtPIN.TabIndex = 7;

            // btnConfigure
            this.btnConfigure.Enabled = false;
            this.btnConfigure.Location = new System.Drawing.Point(140, 240);
            this.btnConfigure.Name = "btnConfigure";
            this.btnConfigure.Size = new System.Drawing.Size(90, 23);
            this.btnConfigure.TabIndex = 8;
            this.btnConfigure.Text = "Configure";
            this.btnConfigure.UseVisualStyleBackColor = true;
            this.btnConfigure.Click += new System.EventHandler(this.btnConfigure_Click);

            // btnCancel
            this.btnCancel.Location = new System.Drawing.Point(240, 240);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 9;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);

            // btnRefresh
            this.btnRefresh.Location = new System.Drawing.Point(15, 240);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(75, 23);
            this.btnRefresh.TabIndex = 10;
            this.btnRefresh.Text = "Refresh";
            this.btnRefresh.UseVisualStyleBackColor = true;
            this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);

            // YubiKeyConfigForm
            this.AcceptButton = this.btnConfigure;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(334, 281);
            this.Controls.Add(this.btnRefresh);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnConfigure);
            this.Controls.Add(this.txtPIN);
            this.Controls.Add(this.lblPIN);
            this.Controls.Add(this.cmbYubiKeyMode);
            this.Controls.Add(this.lblMode);
            this.Controls.Add(this.lblSSHSupport);
            this.Controls.Add(this.txtDeviceInfo);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.lblTitle);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "YubiKeyConfigForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "YubiKey Configuration";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.TextBox txtDeviceInfo;
        private System.Windows.Forms.Label lblSSHSupport;
        private System.Windows.Forms.Label lblMode;
        private System.Windows.Forms.ComboBox cmbYubiKeyMode;
        private System.Windows.Forms.Label lblPIN;
        private System.Windows.Forms.TextBox txtPIN;
        private System.Windows.Forms.Button btnConfigure;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnRefresh;
    }

    // YubiKey mode enum
    public enum YubiKeyMode
    {
        PIV,
        OATH,
        ChallengeResponse
    }
}