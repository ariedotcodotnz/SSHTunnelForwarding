using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SSHTunnelClient.Forms
{
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
}
