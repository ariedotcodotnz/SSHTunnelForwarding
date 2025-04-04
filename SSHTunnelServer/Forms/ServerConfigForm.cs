using System;
using System.Windows.Forms;

namespace SSHTunnelServer
{
    public partial class ServerConfigForm : Form
    {
        public ServerConfig Configuration { get; private set; }

        public ServerConfigForm(ServerConfig existingConfig)
        {
            InitializeComponent();

            if (existingConfig != null)
            {
                // Edit existing configuration
                Configuration = new ServerConfig
                {
                    Name = existingConfig.Name,
                    ListenPort = existingConfig.ListenPort,
                    AllowedClients = existingConfig.AllowedClients,
                    UseEncryption = existingConfig.UseEncryption,
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
                    IsActive = false
                };
            }

            // Populate form fields
            txtName.Text = Configuration.Name;
            numListenPort.Value = Configuration.ListenPort;
            txtAllowedClients.Text = Configuration.AllowedClients;
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

            // Update configuration
            Configuration.Name = txtName.Text;
            Configuration.ListenPort = (int)numListenPort.Value;
            Configuration.AllowedClients = txtAllowedClients.Text;
            Configuration.UseEncryption = chkUseEncryption.Checked;

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
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.numListenPort)).BeginInit();
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
            // lblListenPort
            // 
            this.lblListenPort.AutoSize = true;
            this.lblListenPort.Location = new System.Drawing.Point(12, 41);
            this.lblListenPort.Name = "lblListenPort";
            this.lblListenPort.Size = new System.Drawing.Size(63, 13);
            this.lblListenPort.TabIndex = 2;
            this.lblListenPort.Text = "Listen Port:";
            // 
            // numListenPort
            // 
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
            // 
            // lblAllowedClients
            // 
            this.lblAllowedClients.AutoSize = true;
            this.lblAllowedClients.Location = new System.Drawing.Point(12, 67);
            this.lblAllowedClients.Name = "lblAllowedClients";
            this.lblAllowedClients.Size = new System.Drawing.Size(85, 13);
            this.lblAllowedClients.TabIndex = 4;
            this.lblAllowedClients.Text = "Allowed Clients:";
            // 
            // txtAllowedClients
            // 
            this.txtAllowedClients.Location = new System.Drawing.Point(120, 64);
            this.txtAllowedClients.Name = "txtAllowedClients";
            this.txtAllowedClients.Size = new System.Drawing.Size(252, 20);
            this.txtAllowedClients.TabIndex = 5;
            // 
            // lblAllowedClientsHelp
            // 
            this.lblAllowedClientsHelp.Location = new System.Drawing.Point(117, 87);
            this.lblAllowedClientsHelp.Name = "lblAllowedClientsHelp";
            this.lblAllowedClientsHelp.Size = new System.Drawing.Size(255, 30);
            this.lblAllowedClientsHelp.TabIndex = 6;
            this.lblAllowedClientsHelp.Text = "IP addresses separated by commas. Use * to allow all clients.";
            // 
            // chkUseEncryption
            // 
            this.chkUseEncryption.AutoSize = true;
            this.chkUseEncryption.Location = new System.Drawing.Point(120, 120);
            this.chkUseEncryption.Name = "chkUseEncryption";
            this.chkUseEncryption.Size = new System.Drawing.Size(207, 17);
            this.chkUseEncryption.TabIndex = 7;
            this.chkUseEncryption.Text = "Handle Additional Encrypted Tunnels";
            this.chkUseEncryption.UseVisualStyleBackColor = true;
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(216, 160);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 23);
            this.btnOK.TabIndex = 8;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(297, 160);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 9;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // ServerConfigForm
            // 
            this.AcceptButton = this.btnOK;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(384, 195);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
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
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;
    }
}