using System;
using System.Windows.Forms;
using System.IO;

namespace SSHTunnelClient
{
    public partial class ConfigurationForm : Form
    {
        public PortForwardingConfig Configuration { get; private set; }

        public ConfigurationForm(PortForwardingConfig existingConfig)
        {
            InitializeComponent();

            if (existingConfig != null)
            {
                // Edit existing configuration
                Configuration = new PortForwardingConfig
                {
                    Name = existingConfig.Name,
                    ServerHost = existingConfig.ServerHost,
                    ServerPort = existingConfig.ServerPort,
                    Username = existingConfig.Username,
                    Password = existingConfig.Password,
                    PrivateKeyPath = existingConfig.PrivateKeyPath,
                    LocalPort = existingConfig.LocalPort,
                    RemoteHost = existingConfig.RemoteHost,
                    RemotePort = existingConfig.RemotePort,
                    UseEncryption = existingConfig.UseEncryption,
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
                    LocalPort = 8080,
                    RemoteHost = "localhost",
                    RemotePort = 80,
                    UseEncryption = false,
                    IsActive = false
                };
            }

            // Populate form fields
            txtName.Text = Configuration.Name;
            txtServerHost.Text = Configuration.ServerHost;
            numServerPort.Value = Configuration.ServerPort;
            txtUsername.Text = Configuration.Username;
            txtPassword.Text = Configuration.Password;
            txtPrivateKeyPath.Text = Configuration.PrivateKeyPath;
            numLocalPort.Value = Configuration.LocalPort;
            txtRemoteHost.Text = Configuration.RemoteHost;
            numRemotePort.Value = Configuration.RemotePort;
            chkUseEncryption.Checked = Configuration.UseEncryption;
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

            if (!string.IsNullOrEmpty(txtPrivateKeyPath.Text) && !File.Exists(txtPrivateKeyPath.Text))
            {
                MessageBox.Show("The specified private key file does not exist.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Update configuration
            Configuration.Name = txtName.Text;
            Configuration.ServerHost = txtServerHost.Text;
            Configuration.ServerPort = (int)numServerPort.Value;
            Configuration.Username = txtUsername.Text;
            Configuration.Password = txtPassword.Text;
            Configuration.PrivateKeyPath = txtPrivateKeyPath.Text;
            Configuration.LocalPort = (int)numLocalPort.Value;
            Configuration.RemoteHost = txtRemoteHost.Text;
            Configuration.RemotePort = (int)numRemotePort.Value;
            Configuration.UseEncryption = chkUseEncryption.Checked;

            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void btnBrowse_Click(object sender, EventArgs e)
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
            this.lblPassword = new System.Windows.Forms.Label();
            this.txtPassword = new System.Windows.Forms.TextBox();
            this.lblPrivateKeyPath = new System.Windows.Forms.Label();
            this.txtPrivateKeyPath = new System.Windows.Forms.TextBox();
            this.btnBrowse = new System.Windows.Forms.Button();
            this.lblLocalPort = new System.Windows.Forms.Label();
            this.numLocalPort = new System.Windows.Forms.NumericUpDown();
            this.lblRemoteHost = new System.Windows.Forms.Label();
            this.txtRemoteHost = new System.Windows.Forms.TextBox();
            this.lblRemotePort = new System.Windows.Forms.Label();
            this.numRemotePort = new System.Windows.Forms.NumericUpDown();
            this.chkUseEncryption = new System.Windows.Forms.CheckBox();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.numServerPort)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numLocalPort)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numRemotePort)).BeginInit();
            this.SuspendLayout();
            // 
            // lblName
            // 
            this.lblName.AutoSize = true;
            this.lblName.Location = new System.Drawing.Point(12, 15);
            this.lblName.Name = "lblName";
            this.lblName.Size = new System.Drawing.Size(38, 13);
            this.lblName.TabIndex = 0;
            this.lblName.Text = "Name:";
            // 
            // txtName
            // 
            this.txtName.Location = new System.Drawing.Point(120, 12);
            this.txtName.Name = "txtName";
            this.txtName.Size = new System.Drawing.Size(252, 20);
            this.txtName.TabIndex = 1;
            // 
            // lblServerHost
            // 
            this.lblServerHost.AutoSize = true;
            this.lblServerHost.Location = new System.Drawing.Point(12, 41);
            this.lblServerHost.Name = "lblServerHost";
            this.lblServerHost.Size = new System.Drawing.Size(69, 13);
            this.lblServerHost.TabIndex = 2;
            this.lblServerHost.Text = "Server Host:";
            // 
            // txtServerHost
            // 
            this.txtServerHost.Location = new System.Drawing.Point(120, 38);
            this.txtServerHost.Name = "txtServerHost";
            this.txtServerHost.Size = new System.Drawing.Size(252, 20);
            this.txtServerHost.TabIndex = 3;
            // 
            // lblServerPort
            // 
            this.lblServerPort.AutoSize = true;
            this.lblServerPort.Location = new System.Drawing.Point(12, 67);
            this.lblServerPort.Name = "lblServerPort";
            this.lblServerPort.Size = new System.Drawing.Size(66, 13);
            this.lblServerPort.TabIndex = 4;
            this.lblServerPort.Text = "Server Port:";
            // 
            // numServerPort
            // 
            this.numServerPort.Location = new System.Drawing.Point(120, 65);
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
            // 
            // lblUsername
            // 
            this.lblUsername.AutoSize = true;
            this.lblUsername.Location = new System.Drawing.Point(12, 93);
            this.lblUsername.Name = "lblUsername";
            this.lblUsername.Size = new System.Drawing.Size(58, 13);
            this.lblUsername.TabIndex = 6;
            this.lblUsername.Text = "Username:";
            // 
            // txtUsername
            // 
            this.txtUsername.Location = new System.Drawing.Point(120, 90);
            this.txtUsername.Name = "txtUsername";
            this.txtUsername.Size = new System.Drawing.Size(252, 20);
            this.txtUsername.TabIndex = 7;
            // 
            // lblPassword
            // 
            this.lblPassword.AutoSize = true;
            this.lblPassword.Location = new System.Drawing.Point(12, 119);
            this.lblPassword.Name = "lblPassword";
            this.lblPassword.Size = new System.Drawing.Size(56, 13);
            this.lblPassword.TabIndex = 8;
            this.lblPassword.Text = "Password:";
            // 
            // txtPassword
            // 
            this.txtPassword.Location = new System.Drawing.Point(120, 116);
            this.txtPassword.Name = "txtPassword";
            this.txtPassword.PasswordChar = '*';
            this.txtPassword.Size = new System.Drawing.Size(252, 20);
            this.txtPassword.TabIndex = 9;
            // 
            // lblPrivateKeyPath
            // 
            this.lblPrivateKeyPath.AutoSize = true;
            this.lblPrivateKeyPath.Location = new System.Drawing.Point(12, 145);
            this.lblPrivateKeyPath.Name = "lblPrivateKeyPath";
            this.lblPrivateKeyPath.Size = new System.Drawing.Size(88, 13);
            this.lblPrivateKeyPath.TabIndex = 10;
            this.lblPrivateKeyPath.Text = "Private Key Path:";
            // 
            // txtPrivateKeyPath
            // 
            this.txtPrivateKeyPath.Location = new System.Drawing.Point(120, 142);
            this.txtPrivateKeyPath.Name = "txtPrivateKeyPath";
            this.txtPrivateKeyPath.Size = new System.Drawing.Size(207, 20);
            this.txtPrivateKeyPath.TabIndex = 11;
            // 
            // btnBrowse
            // 
            this.btnBrowse.Location = new System.Drawing.Point(333, 140);
            this.btnBrowse.Name = "btnBrowse";
            this.btnBrowse.Size = new System.Drawing.Size(39, 23);
            this.btnBrowse.TabIndex = 12;
            this.btnBrowse.Text = "...";
            this.btnBrowse.UseVisualStyleBackColor = true;
            this.btnBrowse.Click += new System.EventHandler(this.btnBrowse_Click);
            // 
            // lblLocalPort
            // 
            this.lblLocalPort.AutoSize = true;
            this.lblLocalPort.Location = new System.Drawing.Point(12, 171);
            this.lblLocalPort.Name = "lblLocalPort";
            this.lblLocalPort.Size = new System.Drawing.Size(58, 13);
            this.lblLocalPort.TabIndex = 13;
            this.lblLocalPort.Text = "Local Port:";
            // 
            // numLocalPort
            // 
            this.numLocalPort.Location = new System.Drawing.Point(120, 169);
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
            this.numLocalPort.TabIndex = 14;
            this.numLocalPort.Value = new decimal(new int[] {
            8080,
            0,
            0,
            0});
            // 
            // lblRemoteHost
            // 
            this.lblRemoteHost.AutoSize = true;
            this.lblRemoteHost.Location = new System.Drawing.Point(12, 197);
            this.lblRemoteHost.Name = "lblRemoteHost";
            this.lblRemoteHost.Size = new System.Drawing.Size(73, 13);
            this.lblRemoteHost.TabIndex = 15;
            this.lblRemoteHost.Text = "Remote Host:";
            // 
            // txtRemoteHost
            // 
            this.txtRemoteHost.Location = new System.Drawing.Point(120, 194);
            this.txtRemoteHost.Name = "txtRemoteHost";
            this.txtRemoteHost.Size = new System.Drawing.Size(252, 20);
            this.txtRemoteHost.TabIndex = 16;
            // 
            // lblRemotePort
            // 
            this.lblRemotePort.AutoSize = true;
            this.lblRemotePort.Location = new System.Drawing.Point(12, 223);
            this.lblRemotePort.Name = "lblRemotePort";
            this.lblRemotePort.Size = new System.Drawing.Size(70, 13);
            this.lblRemotePort.TabIndex = 17;
            this.lblRemotePort.Text = "Remote Port:";
            // 
            // numRemotePort
            // 
            this.numRemotePort.Location = new System.Drawing.Point(120, 221);
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
            this.numRemotePort.TabIndex = 18;
            this.numRemotePort.Value = new decimal(new int[] {
            80,
            0,
            0,
            0});
            // 
            // chkUseEncryption
            // 
            this.chkUseEncryption.AutoSize = true;
            this.chkUseEncryption.Location = new System.Drawing.Point(120, 247);
            this.chkUseEncryption.Name = "chkUseEncryption";
            this.chkUseEncryption.Size = new System.Drawing.Size(161, 17);
            this.chkUseEncryption.TabIndex = 19;
            this.chkUseEncryption.Text = "Use Additional Encryption";
            this.chkUseEncryption.UseVisualStyleBackColor = true;
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(216, 280);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 23);
            this.btnOK.TabIndex = 20;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(297, 280);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 21;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // ConfigurationForm
            // 
            this.AcceptButton = this.btnOK;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(384, 315);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.chkUseEncryption);
            this.Controls.Add(this.numRemotePort);
            this.Controls.Add(this.lblRemotePort);
            this.Controls.Add(this.txtRemoteHost);
            this.Controls.Add(this.lblRemoteHost);
            this.Controls.Add(this.numLocalPort);
            this.Controls.Add(this.lblLocalPort);
            this.Controls.Add(this.btnBrowse);
            this.Controls.Add(this.txtPrivateKeyPath);
            this.Controls.Add(this.lblPrivateKeyPath);
            this.Controls.Add(this.txtPassword);
            this.Controls.Add(this.lblPassword);
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
            ((System.ComponentModel.ISupportInitialize)(this.numServerPort)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numLocalPort)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numRemotePort)).EndInit();
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
        private System.Windows.Forms.Label lblPassword;
        private System.Windows.Forms.TextBox txtPassword;
        private System.Windows.Forms.Label lblPrivateKeyPath;
        private System.Windows.Forms.TextBox txtPrivateKeyPath;
        private System.Windows.Forms.Button btnBrowse;
        private System.Windows.Forms.Label lblLocalPort;
        private System.Windows.Forms.NumericUpDown numLocalPort;
        private System.Windows.Forms.Label lblRemoteHost;
        private System.Windows.Forms.TextBox txtRemoteHost;
        private System.Windows.Forms.Label lblRemotePort;
        private System.Windows.Forms.NumericUpDown numRemotePort;
        private System.Windows.Forms.CheckBox chkUseEncryption;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;
    }
}