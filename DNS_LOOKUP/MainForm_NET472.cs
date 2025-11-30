using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Forms;
using DnsClient;
using Newtonsoft.Json;

namespace DnsLookupTool
{
    public class MainForm_NET472 : Form
    {
        // Configuration
        private IPAddress CustomDnsServer;
        private List<HistoryEntry> QueryHistory = new List<HistoryEntry>();
        private int RetryCount = 3;
        private TimeSpan Timeout = TimeSpan.FromSeconds(5);
        private bool ForceTcpOnly = false;
        private bool EnableDnsSec = false;
        private string SecuritySettingsFile = "security_settings.json";

        // UI Controls
        private TabControl tabControl;
        private Label lblDnsServer;
        private Button btnConfigDns;

        // Tab 1: Domain Lookup (A/AAAA)
        private TextBox txtDomain;
        private Button btnLookupDomain;
        private ListBox lstResults;
        private Label lblStatus;

        // Tab 2: Reverse Lookup (PTR)
        private TextBox txtIpReverse;
        private Button btnReverseIp;
        private RichTextBox rtbReverseResults;

        // Tab 3: Multiple Records
        private TextBox txtQueryMulti;
        private ComboBox cmbRecordType;
        private Button btnQueryMulti;
        private RichTextBox rtbMultiResults;

        // Tab 4: Batch Process
        private TextBox txtBatchFile;
        private Button btnBrowseBatch;
        private Button btnProcessBatch;
        private RichTextBox rtbBatchResults;
        private ProgressBar progressBatch;

        // Tab 5: History
        private DataGridView dgvHistory;

        // Tab 6: Settings
        private CheckBox chkForceTcp;
        private CheckBox chkDnsSec;
        private Button btnResetSecurity;
        private Button btnExportResults;

        public MainForm_NET472()
        {
            Text = "DNS Lookup Tool v2.0";
            Size = new Size(950, 700);
            StartPosition = FormStartPosition.CenterScreen;
            Icon = SystemIcons.Application;
            BackColor = Color.FromArgb(240, 240, 240);

            LoadHistory();
            LoadSecuritySettings();
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            // KHÔNG CÓ HEADER - TAB CONTROL CHIẾM TOÀN BỘ FORM
            tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10)
            };

            CreateDomainLookupTab();
            CreateReverseLookupTab();
            CreateMultipleRecordsTab();
            CreateBatchProcessTab();
            CreateHistoryTab();
            CreateSettingsTab();

            Controls.Add(tabControl);
        }

        private void CreateDomainLookupTab()
        {
            TabPage tabPage = new TabPage { Text = "🔍 A/AAAA Lookup", AutoScroll = true };
            tabPage.BackColor = Color.White;
            tabPage.Padding = new Padding(10);

            Label lblInput = new Label
            {
                Text = "Domain Name:",
                Location = new Point(20, 20),
                AutoSize = true,
                Font = new Font("Segoe UI", 10)
            };
            tabPage.Controls.Add(lblInput);

            txtDomain = new TextBox
            {
                Location = new Point(20, 45),
                Size = new Size(400, 35),
                Font = new Font("Segoe UI", 11),
                Text = ""
            };
            tabPage.Controls.Add(txtDomain);

            btnLookupDomain = new Button
            {
                Text = "🔍 Lookup",
                Location = new Point(430, 45),
                Size = new Size(100, 35),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            btnLookupDomain.Click += BtnLookupDomain_Click;
            tabPage.Controls.Add(btnLookupDomain);

            lblStatus = new Label
            {
                Text = "Ready",
                Location = new Point(20, 90),
                Size = new Size(500, 25),
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.Gray
            };
            tabPage.Controls.Add(lblStatus);

            lstResults = new ListBox
            {
                Location = new Point(20, 120),
                Size = new Size(550, 350),
                Font = new Font("Segoe UI", 11),
                ItemHeight = 25
            };
            tabPage.Controls.Add(lstResults);

            tabControl.TabPages.Add(tabPage);
        }

        private void CreateReverseLookupTab()
        {
            TabPage tabPage = new TabPage { Text = "↩️ PTR Lookup", AutoScroll = true };
            tabPage.BackColor = Color.White;
            tabPage.Padding = new Padding(10);

            Label lblInput = new Label
            {
                Text = "IP Address:",
                Location = new Point(20, 20),
                AutoSize = true,
                Font = new Font("Segoe UI", 10)
            };
            tabPage.Controls.Add(lblInput);

            txtIpReverse = new TextBox
            {
                Location = new Point(20, 45),
                Size = new Size(400, 35),
                Font = new Font("Segoe UI", 11),
                Text = ""
            };
            tabPage.Controls.Add(txtIpReverse);

            btnReverseIp = new Button
            {
                Text = "↩️ Reverse",
                Location = new Point(430, 45),
                Size = new Size(100, 35),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            btnReverseIp.Click += BtnReverseIp_Click;
            tabPage.Controls.Add(btnReverseIp);

            rtbReverseResults = new RichTextBox
            {
                Location = new Point(20, 90),
                Size = new Size(550, 380),
                Font = new Font("Segoe UI", 10),
                ReadOnly = true
            };
            tabPage.Controls.Add(rtbReverseResults);

            tabControl.TabPages.Add(tabPage);
        }

        private void CreateMultipleRecordsTab()
        {
            TabPage tabPage = new TabPage { Text = "📋 DNS Records", AutoScroll = true };
            tabPage.BackColor = Color.White;
            tabPage.Padding = new Padding(10);

            Label lblInput = new Label
            {
                Text = "Domain or IP:",
                Location = new Point(20, 20),
                AutoSize = true,
                Font = new Font("Segoe UI", 10)
            };
            tabPage.Controls.Add(lblInput);

            txtQueryMulti = new TextBox
            {
                Location = new Point(20, 45),
                Size = new Size(300, 35),
                Font = new Font("Segoe UI", 11),
                Text = ""
            };
            tabPage.Controls.Add(txtQueryMulti);

            Label lblType = new Label
            {
                Text = "Record Type:",
                Location = new Point(330, 20),
                AutoSize = true,
                Font = new Font("Segoe UI", 10)
            };
            tabPage.Controls.Add(lblType);

            cmbRecordType = new ComboBox
            {
                Location = new Point(330, 45),
                Size = new Size(100, 35),
                Font = new Font("Segoe UI", 10),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbRecordType.Items.AddRange(new[] { "A", "AAAA", "PTR", "MX", "CNAME", "TXT", "NS", "SOA" });
            cmbRecordType.SelectedIndex = 0;
            tabPage.Controls.Add(cmbRecordType);

            btnQueryMulti = new Button
            {
                Text = "📋 Query",
                Location = new Point(440, 45),
                Size = new Size(90, 35),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            btnQueryMulti.Click += BtnQueryMulti_Click;
            tabPage.Controls.Add(btnQueryMulti);

            rtbMultiResults = new RichTextBox
            {
                Location = new Point(20, 90),
                Size = new Size(510, 380),
                Font = new Font("Segoe UI", 10),
                ReadOnly = true
            };
            tabPage.Controls.Add(rtbMultiResults);

            tabControl.TabPages.Add(tabPage);
        }

        private void CreateBatchProcessTab()
        {
            TabPage tabPage = new TabPage { Text = "📦 Batch Process", AutoScroll = true };
            tabPage.BackColor = Color.White;
            tabPage.Padding = new Padding(10);

            Label lblFile = new Label
            {
                Text = "Batch File (one query per line):",
                Location = new Point(20, 20),
                AutoSize = true,
                Font = new Font("Segoe UI", 10)
            };
            tabPage.Controls.Add(lblFile);

            txtBatchFile = new TextBox
            {
                Location = new Point(20, 45),
                Size = new Size(350, 35),
                Font = new Font("Segoe UI", 11),
                ReadOnly = true,
                Text = ""
            };
            tabPage.Controls.Add(txtBatchFile);

            btnBrowseBatch = new Button
            {
                Text = "📁 Browse",
                Location = new Point(380, 45),
                Size = new Size(90, 35),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            btnBrowseBatch.Click += BtnBrowseBatch_Click;
            tabPage.Controls.Add(btnBrowseBatch);

            btnProcessBatch = new Button
            {
                Text = "▶ Process",
                Location = new Point(480, 45),
                Size = new Size(90, 35),
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            btnProcessBatch.Click += BtnProcessBatch_Click;
            tabPage.Controls.Add(btnProcessBatch);

            progressBatch = new ProgressBar
            {
                Location = new Point(20, 90),
                Size = new Size(550, 25),
                Style = ProgressBarStyle.Continuous
            };
            tabPage.Controls.Add(progressBatch);

            rtbBatchResults = new RichTextBox
            {
                Location = new Point(20, 125),
                Size = new Size(550, 345),
                Font = new Font("Consolas", 9),
                ReadOnly = true
            };
            tabPage.Controls.Add(rtbBatchResults);

            tabControl.TabPages.Add(tabPage);
        }

        private void CreateHistoryTab()
        {
            TabPage tabPage = new TabPage { Text = "📜 History", AutoScroll = true };
            tabPage.BackColor = Color.White;
            tabPage.Padding = new Padding(10);

            dgvHistory = new DataGridView
            {
                Location = new Point(20, 20),
                Size = new Size(550, 400),
                AllowUserToAddRows = false,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BorderStyle = BorderStyle.Fixed3D
            };

            dgvHistory.Columns.Add("Type", "Type");
            dgvHistory.Columns.Add("Query", "Query");
            dgvHistory.Columns.Add("Timestamp", "Timestamp");
            dgvHistory.Columns.Add("ElapsedMs", "Time (ms)");

            tabPage.Controls.Add(dgvHistory);

            Button btnClearHistory = new Button
            {
                Text = "🗑️ Clear",
                Location = new Point(20, 430),
                Size = new Size(100, 35),
                BackColor = Color.FromArgb(244, 67, 54),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            btnClearHistory.Click += (s, e) =>
            {
                QueryHistory.Clear();
                dgvHistory.Rows.Clear();
                SaveHistory();
                MessageBox.Show("History cleared!", "Success");
            };
            tabPage.Controls.Add(btnClearHistory);

            tabControl.TabPages.Add(tabPage);
        }

        private void CreateSettingsTab()
        {
            TabPage tabPage = new TabPage { Text = "⚙️ Settings", AutoScroll = true };
            tabPage.BackColor = Color.White;
            tabPage.Padding = new Padding(20);

            Label lblSecurity = new Label
            {
                Text = "Security Settings:",
                Location = new Point(20, 20),
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                AutoSize = true
            };
            tabPage.Controls.Add(lblSecurity);

            chkForceTcp = new CheckBox
            {
                Text = "Force TCP Only",
                Location = new Point(20, 50),
                AutoSize = true,
                Checked = ForceTcpOnly,
                Font = new Font("Segoe UI", 10)
            };
            chkForceTcp.CheckedChanged += (s, e) => { ForceTcpOnly = chkForceTcp.Checked; SaveSecuritySettings(); };
            tabPage.Controls.Add(chkForceTcp);

            chkDnsSec = new CheckBox
            {
                Text = "Enable DNSSEC Validation (Stub)",
                Location = new Point(20, 80),
                AutoSize = true,
                Checked = EnableDnsSec,
                Font = new Font("Segoe UI", 10)
            };
            chkDnsSec.CheckedChanged += (s, e) => { EnableDnsSec = chkDnsSec.Checked; SaveSecuritySettings(); };
            tabPage.Controls.Add(chkDnsSec);

            btnResetSecurity = new Button
            {
                Text = "Reset to Defaults",
                Location = new Point(20, 120),
                Size = new Size(150, 35),
                BackColor = Color.FromArgb(255, 152, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            btnResetSecurity.Click += (s, e) =>
            {
                ForceTcpOnly = false;
                EnableDnsSec = false;
                chkForceTcp.Checked = false;
                chkDnsSec.Checked = false;
                SaveSecuritySettings();
                MessageBox.Show("Settings reset!", "Info");
            };
            tabPage.Controls.Add(btnResetSecurity);

            Label lblExport = new Label
            {
                Text = "Export & Report:",
                Location = new Point(20, 180),
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                AutoSize = true
            };
            tabPage.Controls.Add(lblExport);

            btnExportResults = new Button
            {
                Text = "💾 Export History",
                Location = new Point(20, 210),
                Size = new Size(150, 35),
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            btnExportResults.Click += BtnExportResults_Click;
            tabPage.Controls.Add(btnExportResults);

            Button btnGenerateReport = new Button
            {
                Text = "📊 Generate Report",
                Location = new Point(180, 210),
                Size = new Size(150, 35),
                BackColor = Color.FromArgb(156, 39, 176),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            btnGenerateReport.Click += BtnGenerateReport_Click;
            tabPage.Controls.Add(btnGenerateReport);

            tabControl.TabPages.Add(tabPage);
        }

        // Event Handlers
        private void BtnLookupDomain_Click(object sender, EventArgs e)
        {
            string domain = txtDomain.Text.Trim();
            if (string.IsNullOrWhiteSpace(domain))
            {
                MessageBox.Show("Please enter a domain name", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            lblStatus.Text = "Looking up...";
            lstResults.Items.Clear();

            try
            {
                LookupClient client = CreateLookupClient();
                System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
                DnsClient.Protocol.IDnsQueryResponse result = client.Query(domain, QueryType.A);
                sw.Stop();

                if (result.Answers.Count == 0)
                {
                    lstResults.Items.Add("No records found");
                    lblStatus.Text = "No results";
                    return;
                }

                string[] addresses = result.Answers.OfType<DnsClient.Protocol.ARecord>().Select(r => r.Address.ToString()).ToArray();
                foreach (string ip in addresses)
                {
                    lstResults.Items.Add("📌 " + ip);
                }

                lblStatus.Text = "Found " + addresses.Length + " result(s) in " + sw.ElapsedMilliseconds + "ms";
                SaveToHistory("A/AAAA", domain, sw.ElapsedMilliseconds, string.Join(", ", addresses));
                RefreshHistory();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Lookup Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblStatus.Text = "Error";
            }
        }

        private void BtnReverseIp_Click(object sender, EventArgs e)
        {
            string ipText = txtIpReverse.Text.Trim();
            IPAddress ip;
            if (!IPAddress.TryParse(ipText, out ip))
            {
                MessageBox.Show("Invalid IP address", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            rtbReverseResults.Clear();
            rtbReverseResults.AppendText("Resolving...\r\n");

            try
            {
                LookupClient client = CreateLookupClient();
                System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
                DnsClient.Protocol.IDnsQueryResponse result = client.QueryReverse(ip);
                sw.Stop();

                rtbReverseResults.Clear();
                DnsClient.Protocol.PtrRecord ptr = result.Answers.OfType<DnsClient.Protocol.PtrRecord>().FirstOrDefault();
                string hostName = ptr != null ? ptr.PtrDomainName.ToString() : "Not found";

                rtbReverseResults.AppendText("IP: " + ip + "\r\n");
                rtbReverseResults.AppendText("Hostname: " + hostName + "\r\n");
                rtbReverseResults.AppendText("Time: " + sw.ElapsedMilliseconds + "ms\r\n");

                SaveToHistory("PTR", ip.ToString(), sw.ElapsedMilliseconds, hostName);
                RefreshHistory();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Reverse Lookup Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnQueryMulti_Click(object sender, EventArgs e)
        {
            string query = txtQueryMulti.Text.Trim();
            if (string.IsNullOrWhiteSpace(query))
            {
                MessageBox.Show("Please enter a query", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            QueryType recordType = GetQueryType(cmbRecordType.SelectedItem.ToString());
            rtbMultiResults.Clear();
            rtbMultiResults.AppendText("Querying...\r\n");

            try
            {
                LookupClient client = CreateLookupClient();
                System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
                DnsClient.Protocol.IDnsQueryResponse result = client.Query(query, recordType);
                sw.Stop();

                rtbMultiResults.Clear();

                if (result.Answers.Count == 0)
                {
                    rtbMultiResults.AppendText("No records found\r\n");
                    return;
                }

                foreach (DnsClient.Protocol.DnsResourceRecord answer in result.Answers)
                {
                    string recordStr = FormatRecord(recordType, answer);
                    rtbMultiResults.AppendText("• " + recordStr + "\r\n");
                }

                rtbMultiResults.AppendText("\r\nTime: " + sw.ElapsedMilliseconds + "ms\r\n");

                SaveToHistory(recordType.ToString(), query, sw.ElapsedMilliseconds, "");
                RefreshHistory();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Query Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnProcessBatch_Click(object sender, EventArgs e)
        {
            string filePath = txtBatchFile.Text.Trim();
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                MessageBox.Show("Please select a valid file", "File Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            rtbBatchResults.Clear();
            rtbBatchResults.AppendText("Starting batch process...\r\n\r\n");

            try
            {
                string[] lines = File.ReadAllLines(filePath);
                lines = lines.Select(l => l.Trim()).Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();

                progressBatch.Maximum = lines.Length;
                progressBatch.Value = 0;

                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];
                    try
                    {
                        LookupClient client = CreateLookupClient();
                        System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
                        DnsClient.Protocol.IDnsQueryResponse res = client.Query(line, QueryType.A);
                        sw.Stop();

                        string[] addrs = res.Answers.OfType<DnsClient.Protocol.ARecord>()
                            .Select(r => r.Address.ToString())
                            .ToArray();

                        string result = addrs.Length > 0 
                            ? string.Join(", ", addrs)
                            : "No results";

                        rtbBatchResults.AppendText("✓ " + line + " => " + result + " (" + sw.ElapsedMilliseconds + "ms)\r\n");
                        SaveToHistory("A", line, sw.ElapsedMilliseconds, result);
                    }
                    catch (Exception ex)
                    {
                        rtbBatchResults.AppendText("✗ " + line + " => Error: " + ex.Message + "\r\n");
                    }

                    progressBatch.Value = i + 1;
                    Application.DoEvents();
                }

                rtbBatchResults.AppendText("\r\nBatch process completed!");
                RefreshHistory();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Batch Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnConfigDns_Click(object sender, EventArgs e)
        {
            DnsConfigForm form = new DnsConfigForm { DnsServer = CustomDnsServer };
            if (form.ShowDialog() == DialogResult.OK)
            {
                CustomDnsServer = form.DnsServer;
                lblDnsServer.Text = "DNS Server: " + (CustomDnsServer != null ? CustomDnsServer.ToString() : "Default");
            }
        }

        private void BtnBrowseBatch_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog { Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*" };
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                txtBatchFile.Text = ofd.FileName;
            }
            ofd.Dispose();
        }

        private void BtnExportResults_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog { Filter = "Text Files (*.txt)|*.txt|CSV Files (*.csv)|*.csv" };
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    string[] lines = QueryHistory.Select(h => h.Type + "\t" + h.Query + "\t" + h.Timestamp + "\t" + h.ElapsedMs + "ms").ToArray();
                    File.WriteAllLines(sfd.FileName, lines);
                    MessageBox.Show("Export successful!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Export failed: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            sfd.Dispose();
        }

        private void BtnGenerateReport_Click(object sender, EventArgs e)
        {
            if (QueryHistory.Count == 0)
            {
                MessageBox.Show("No history to generate report", "Empty History", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("DNS Lookup Tool - Report");
                sb.AppendLine("Generated: " + DateTime.Now);
                sb.AppendLine("Total Queries: " + QueryHistory.Count + "\r\n");

                var byType = QueryHistory.GroupBy(h => h.Type).OrderByDescending(g => g.Count());

                sb.AppendLine("Statistics by Type:");
                foreach (var g in byType)
                {
                    double[] times = g.Where(x => x.ElapsedMs >= 0).Select(x => (double)x.ElapsedMs).ToArray();
                    double avg = times.Length > 0 ? times.Average() : 0;
                    sb.AppendLine("  " + g.Key + ": " + g.Count() + " queries, avg " + avg.ToString("F1") + "ms");
                }

                sb.AppendLine("\nTop 10 Queries:");
                var topQueries = QueryHistory.GroupBy(h => h.Query)
                    .OrderByDescending(g => g.Count())
                    .Take(10);
                foreach (var g in topQueries)
                {
                    sb.AppendLine("  " + g.Key + ": " + g.Count() + " times");
                }

                string reportPath = "report.txt";
                File.WriteAllText(reportPath, sb.ToString());
                MessageBox.Show("Report generated: " + Path.GetFullPath(reportPath), "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Report generation failed: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Helper Methods
        private QueryType GetQueryType(string typeStr)
        {
            if (typeStr == "AAAA") return QueryType.AAAA;
            if (typeStr == "PTR") return QueryType.PTR;
            if (typeStr == "MX") return QueryType.MX;
            if (typeStr == "CNAME") return QueryType.CNAME;
            if (typeStr == "TXT") return QueryType.TXT;
            if (typeStr == "NS") return QueryType.NS;
            if (typeStr == "SOA") return QueryType.SOA;
            return QueryType.A;
        }

        private string FormatRecord(QueryType type, DnsClient.Protocol.DnsResourceRecord answer)
        {
            if (type == QueryType.A)
                return "A: " + ((DnsClient.Protocol.ARecord)answer).Address.ToString();
            if (type == QueryType.AAAA)
                return "AAAA: " + ((DnsClient.Protocol.AaaaRecord)answer).Address.ToString();
            if (type == QueryType.PTR)
                return "PTR: " + ((DnsClient.Protocol.PtrRecord)answer).PtrDomainName.ToString();
            if (type == QueryType.MX)
                return "MX: " + ((DnsClient.Protocol.MxRecord)answer).Exchange + " (Preference: " + ((DnsClient.Protocol.MxRecord)answer).Preference + ")";
            if (type == QueryType.CNAME)
                return "CNAME: " + ((DnsClient.Protocol.CNameRecord)answer).CanonicalName.ToString();
            if (type == QueryType.TXT)
                return "TXT: " + string.Join(" ", ((DnsClient.Protocol.TxtRecord)answer).Text);
            if (type == QueryType.NS)
                return "NS: " + ((DnsClient.Protocol.NsRecord)answer).NSDName.ToString();
            if (type == QueryType.SOA)
                return "SOA: " + ((DnsClient.Protocol.SoaRecord)answer).MName.ToString();
            return "Unknown record type";
        }

        private LookupClient CreateLookupClient()
        {
            LookupClientOptions options;
            if (CustomDnsServer != null)
                options = new LookupClientOptions(CustomDnsServer) { Timeout = Timeout, Retries = RetryCount };
            else
                options = new LookupClientOptions { Timeout = Timeout, Retries = RetryCount };
            return new LookupClient(options);
        }

        private void SaveToHistory(string type, string query, long elapsedMs, string details)
        {
            QueryHistory.Add(new HistoryEntry
            {
                Type = type,
                Query = query,
                Timestamp = DateTime.Now,
                ElapsedMs = elapsedMs,
                Details = details
            });
        }

        private void RefreshHistory()
        {
            dgvHistory.Rows.Clear();
            foreach (HistoryEntry entry in QueryHistory.OrderByDescending(h => h.Timestamp).Take(100))
            {
                dgvHistory.Rows.Add(entry.Type, entry.Query, entry.Timestamp.ToString("g"), entry.ElapsedMs);
            }
        }

        private void SaveHistory()
        {
            try
            {
                string json = JsonConvert.SerializeObject(QueryHistory);
                File.WriteAllText("history.json", json);
            }
            catch { }
        }

        private void LoadHistory()
        {
            try
            {
                if (File.Exists("history.json"))
                {
                    string json = File.ReadAllText("history.json");
                    QueryHistory = JsonConvert.DeserializeObject<List<HistoryEntry>>(json) ?? new List<HistoryEntry>();
                }
            }
            catch
            {
                QueryHistory = new List<HistoryEntry>();
            }
        }

        private void SaveSecuritySettings()
        {
            try
            {
                object obj = new { ForceTcpOnly, EnableDnsSec };
                string json = JsonConvert.SerializeObject(obj, Formatting.Indented);
                File.WriteAllText(SecuritySettingsFile, json);
            }
            catch { }
        }

        private void LoadSecuritySettings()
        {
            try
            {
                if (File.Exists(SecuritySettingsFile))
                {
                    string json = File.ReadAllText(SecuritySettingsFile);
                    dynamic node = JsonConvert.DeserializeObject(json);
                    if (node != null)
                    {
                        ForceTcpOnly = node.ForceTcpOnly == true;
                        EnableDnsSec = node.EnableDnsSec == true;
                    }
                }
            }
            catch
            {
                ForceTcpOnly = false;
                EnableDnsSec = false;
            }
        }

        protected override void Dispose(bool disposing)
        {
            SaveHistory();
            SaveSecuritySettings();
            base.Dispose(disposing);
        }
    }

    // Support Classes
    public class HistoryEntry
    {
        public string Type { get; set; }
        public string Query { get; set; }
        public DateTime Timestamp { get; set; }
        public long ElapsedMs { get; set; }
        public string Details { get; set; }

        public HistoryEntry()
        {
            Type = "";
            Query = "";
            Details = "";
        }
    }
}
