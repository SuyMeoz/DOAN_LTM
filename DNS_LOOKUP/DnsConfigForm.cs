using System;
using System.Net;
using System.Windows.Forms;

namespace DnsLookupTool
{
    public partial class DnsConfigForm : Form
    {
        public IPAddress? DnsServer { get; set; }

        private TextBox txtDnsIp = null!;
        private Button btnOk = null!;
        private Button btnCancel = null!;
        private Button btnDefault = null!;

        public DnsConfigForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Configure DNS Server";
            this.Size = new Size(400, 200);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            var lblInstruction = new Label
            {
                Text = "Enter DNS Server IP or leave blank for default:",
                Location = new Point(20, 20),
                AutoSize = true
            };
            this.Controls.Add(lblInstruction);

            txtDnsIp = new TextBox
            {
                Location = new Point(20, 50),
                Size = new Size(350, 30),
                Font = new Font("Segoe UI", 11),
                Text = DnsServer?.ToString() ?? ""
            };
            this.Controls.Add(txtDnsIp);

            var lblExample = new Label
            {
                Text = "Example: 8.8.8.8 (Google DNS) or 1.1.1.1 (Cloudflare DNS)",
                Location = new Point(20, 90),
                Font = new Font("Segoe UI", 9, FontStyle.Italic),
                ForeColor = System.Drawing.Color.Gray,
                AutoSize = true
            };
            this.Controls.Add(lblExample);

            btnDefault = new Button
            {
                Text = "Default",
                Location = new Point(20, 130),
                Size = new Size(80, 35),
                BackColor = System.Drawing.Color.FromArgb(255, 152, 0),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnDefault.Click += (s, e) => { txtDnsIp.Text = ""; };
            this.Controls.Add(btnDefault);

            btnOk = new Button
            {
                Text = "OK",
                Location = new Point(220, 130),
                Size = new Size(75, 35),
                BackColor = System.Drawing.Color.FromArgb(0, 120, 215),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.OK
            };
            btnOk.Click += BtnOk_Click;
            this.Controls.Add(btnOk);

            btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(295, 130),
                Size = new Size(75, 35),
                BackColor = System.Drawing.Color.FromArgb(200, 200, 200),
                ForeColor = System.Drawing.Color.Black,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.Cancel
            };
            this.Controls.Add(btnCancel);

            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;
        }

        private void BtnOk_Click(object? sender, EventArgs e)
        {
            var input = txtDnsIp.Text.Trim();
            if (string.IsNullOrWhiteSpace(input))
            {
                DnsServer = null;
                this.DialogResult = DialogResult.OK;
                this.Close();
                return;
            }

            if (IPAddress.TryParse(input, out var ip))
            {
                DnsServer = ip;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                MessageBox.Show("Invalid IP address. Please enter a valid IP.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
