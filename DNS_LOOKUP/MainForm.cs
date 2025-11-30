using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DnsClient;
using Newtonsoft.Json;

namespace DnsLookupTool
{
    public partial class MainForm : Form
    {
        // Configuration
        private IPAddress? CustomDnsServer = null;
        private List<HistoryEntry> QueryHistory = new List<HistoryEntry>();
        private int RetryCount = 3;
        private TimeSpan Timeout = TimeSpan.FromSeconds(5);
        private bool ForceTcpOnly = false;
        private bool EnableDnsSec = false;
        private string SecuritySettingsFile = "security_settings.json";

        // UI Controls
        private TabControl tabControl = null!;
        private Panel headerPanel = null!;
        private Label lblDnsServer = null!;
        private Button btnConfigDns = null!;

        // Tab 1: Domain Lookup (A/AAAA)
        private TextBox txtDomain = null!;
        private Button btnLookupDomain = null!;
        private ListBox lstResults = null!;
        private Label lblStatus = null!;

        // Tab 2: Reverse Lookup (PTR)
        private TextBox txtIpReverse = null!;
        private Button btnReverseIp = null!;
        private RichTextBox rtbReverseResults = null!;

        // Tab 3: Multiple Records
        private TextBox txtQueryMulti = null!;
        private ComboBox cmbRecordType = null!;
        private Button btnQueryMulti = null!;
        private RichTextBox rtbMultiResults = null!;

        // Tab 4: Batch Process
        private TextBox txtBatchFile = null!;
        private Button btnBrowseBatch = null!;
        private Button btnProcessBatch = null!;
        private RichTextBox rtbBatchResults = null!;
        private ProgressBar progressBatch = null!;

        // Tab 5: History
        private DataGridView dgvHistory = null!;

        // Tab 6: Settings
        private CheckBox chkForceTcp = null!;
        private CheckBox chkDnsSec = null!;
        private Button btnResetSecurity = null!;
        private Button btnExportResults = null!;

        public MainForm()
        {
            InitializeComponent();
            LoadHistory();
            LoadSecuritySettings();
            ApplyTheming();
        }

        private void InitializeComponent()
        {
            this.Text = "DNS Lookup Tool v2.0";
            this.Size = new Size(900, 650);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Icon = SystemIcons.Application;

            // Header Panel
            headerPanel = new Panel
            {
                Height = 60,
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(45, 45, 48)
            };

            var lblTitle = new Label
            {
                Text = "🌐 DNS Lookup Tool",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(15, 10)
            };
            headerPanel.Controls.Add(lblTitle);

            lblDnsServer = new Label
            {
                Text = $"DNS Server: {(CustomDnsServer?.ToString() ?? "Default")}",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.LightGray,
                AutoSize = true,
                Location = new Point(15, 35)
            };
            headerPanel.Controls.Add(lblDnsServer);

            btnConfigDns = new Button
            {
                Text = "⚙️ Configure DNS",
                Location = new Point(780, 15),
                Size = new Size(100, 30),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnConfigDns.Click += BtnConfigDns_Click;
            headerPanel.Controls.Add(btnConfigDns);

            this.Controls.Add(headerPanel);

            // Main Tab Control
            tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Location = new Point(0, 60),
                TabIndex = 0
            };

            // Tab 1: Domain Lookup
            CreateDomainLookupTab();

            // Tab 2: Reverse Lookup
            CreateReverseLookupTab();

            // Tab 3: Multiple Records
            CreateMultipleRecordsTab();

            // Tab 4: Batch Process
            CreateBatchProcessTab();

            // Tab 5: History
            CreateHistoryTab();

            // Tab 6: Settings & Export
            CreateSettingsTab();

            this.Controls.Add(tabControl);
        }

        private void CreateDomainLookupTab()
        {
            var tabPage = new TabPage { Text = "A/AAAA Lookup", AutoScroll = true };
            tabPage.BackColor = Color.White;

            var lblInput = new Label
            {
                Text = "Domain Name:",
                Location = new Point(20, 20),
                AutoSize = true
            };
            tabPage.Controls.Add(lblInput);

            txtDomain = new TextBox
            {
                Location = new Point(20, 45),
                Size = new Size(400, 30),
                Font = new Font("Segoe UI", 11)
            };
            tabPage.Controls.Add(txtDomain);

            btnLookupDomain = new Button
            {
                Text = "🔍 Lookup",
                Location = new Point(430, 45),
                Size = new Size(100, 30),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10)
            };
            btnLookupDomain.Click += BtnLookupDomain_Click;
            tabPage.Controls.Add(btnLookupDomain);

            lblStatus = new Label
            {
                Text = "Ready",
                Location = new Point(20, 85),
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
            var tabPage = new TabPage { Text = "PTR Lookup", AutoScroll = true };
            tabPage.BackColor = Color.White;

            var lblInput = new Label
            {
                Text = "IP Address:",
                Location = new Point(20, 20),
                AutoSize = true
            };
            tabPage.Controls.Add(lblInput);

            txtIpReverse = new TextBox
            {
                Location = new Point(20, 45),
                Size = new Size(400, 30),
                Font = new Font("Segoe UI", 11)
            };
            tabPage.Controls.Add(txtIpReverse);

            btnReverseIp = new Button
            {
                Text = "↩️ Reverse",
                Location = new Point(430, 45),
                Size = new Size(100, 30),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10)
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
            var tabPage = new TabPage { Text = "DNS Records", AutoScroll = true };
            tabPage.BackColor = Color.White;

            var lblInput = new Label
            {
                Text = "Domain or IP:",
                Location = new Point(20, 20),
                AutoSize = true
            };
            tabPage.Controls.Add(lblInput);

            txtQueryMulti = new TextBox
            {
                Location = new Point(20, 45),
                Size = new Size(300, 30),
                Font = new Font("Segoe UI", 11)
            };
            tabPage.Controls.Add(txtQueryMulti);

            var lblType = new Label
            {
                Text = "Record Type:",
                Location = new Point(330, 20),
                AutoSize = true
            };
            tabPage.Controls.Add(lblType);

            cmbRecordType = new ComboBox
            {
                Location = new Point(330, 45),
                Size = new Size(100, 30),
                Font = new Font("Segoe UI", 11),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbRecordType.Items.AddRange(new[] { "A", "AAAA", "PTR", "MX", "CNAME", "TXT", "NS", "SOA" });
            cmbRecordType.SelectedIndex = 0;
            tabPage.Controls.Add(cmbRecordType);

            btnQueryMulti = new Button
            {
                Text = "📋 Query",
                Location = new Point(440, 45),
                Size = new Size(90, 30),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10)
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
            var tabPage = new TabPage { Text = "Batch Process", AutoScroll = true };
            tabPage.BackColor = Color.White;

            var lblFile = new Label
            {
                Text = "Batch File (one query per line):",
                Location = new Point(20, 20),
                AutoSize = true
            };
            tabPage.Controls.Add(lblFile);

            txtBatchFile = new TextBox
            {
                Location = new Point(20, 45),
                Size = new Size(350, 30),
                Font = new Font("Segoe UI", 11),
                ReadOnly = true
            };
            tabPage.Controls.Add(txtBatchFile);

            btnBrowseBatch = new Button
            {
                Text = "📁 Browse",
                Location = new Point(380, 45),
                Size = new Size(90, 30),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnBrowseBatch.Click += BtnBrowseBatch_Click;
            tabPage.Controls.Add(btnBrowseBatch);

            btnProcessBatch = new Button
            {
                Text = "▶ Process",
                Location = new Point(480, 45),
                Size = new Size(90, 30),
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnProcessBatch.Click += BtnProcessBatch_Click;
            tabPage.Controls.Add(btnProcessBatch);

            progressBatch = new ProgressBar
            {
                Location = new Point(20, 85),
                Size = new Size(550, 25),
                Style = ProgressBarStyle.Continuous
            };
            tabPage.Controls.Add(progressBatch);

            rtbBatchResults = new RichTextBox
            {
                Location = new Point(20, 120),
                Size = new Size(550, 350),
                Font = new Font("Consolas", 9),
                ReadOnly = true
            };
            tabPage.Controls.Add(rtbBatchResults);

            tabControl.TabPages.Add(tabPage);
        }

        private void CreateHistoryTab()
        {
            var tabPage = new TabPage { Text = "History", AutoScroll = true };
            tabPage.BackColor = Color.White;

            dgvHistory = new DataGridView
            {
                Location = new Point(20, 20),
                Size = new Size(550, 400),
                AllowUserToAddRows = false,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            dgvHistory.Columns.Add("Type", "Type");
            dgvHistory.Columns.Add("Query", "Query");
            dgvHistory.Columns.Add("Timestamp", "Timestamp");
            dgvHistory.Columns.Add("ElapsedMs", "Time (ms)");

            tabPage.Controls.Add(dgvHistory);

            var btnClearHistory = new Button
            {
                Text = "🗑️ Clear History",
                Location = new Point(20, 430),
                Size = new Size(120, 35),
                BackColor = Color.FromArgb(244, 67, 54),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnClearHistory.Click += (s, e) =>
            {
                QueryHistory.Clear();
                dgvHistory.Rows.Clear();
                SaveHistory();
            };
            tabPage.Controls.Add(btnClearHistory);

            tabControl.TabPages.Add(tabPage);
        }

        private void CreateSettingsTab()
        {
            var tabPage = new TabPage { Text = "Settings & Export", AutoScroll = true };
            tabPage.BackColor = Color.White;

            var lblSecurity = new Label
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
                Checked = ForceTcpOnly
            };
            chkForceTcp.CheckedChanged += (s, e) => { ForceTcpOnly = chkForceTcp.Checked; SaveSecuritySettings(); };
            tabPage.Controls.Add(chkForceTcp);

            chkDnsSec = new CheckBox
            {
                Text = "Enable DNSSEC Validation (Stub)",
                Location = new Point(20, 80),
                AutoSize = true,
                Checked = EnableDnsSec
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
                FlatStyle = FlatStyle.Flat
            };
            btnResetSecurity.Click += (s, e) =>
            {
                ForceTcpOnly = false;
                EnableDnsSec = false;
                chkForceTcp.Checked = false;
                chkDnsSec.Checked = false;
                SaveSecuritySettings();
            };
            tabPage.Controls.Add(btnResetSecurity);

            var lblExport = new Label
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
                FlatStyle = FlatStyle.Flat
            };
            btnExportResults.Click += BtnExportResults_Click;
            tabPage.Controls.Add(btnExportResults);

            var btnGenerateReport = new Button
            {
                Text = "📊 Generate Report",
                Location = new Point(180, 210),
                Size = new Size(150, 35),
                BackColor = Color.FromArgb(156, 39, 176),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnGenerateReport.Click += BtnGenerateReport_Click;
            tabPage.Controls.Add(btnGenerateReport);

            tabControl.TabPages.Add(tabPage);
        }

        private void ApplyTheming()
        {
            // Modern dark theme for better UX
            this.BackColor = Color.FromArgb(240, 240, 240);
        }

        // Event Handlers
        private async void BtnLookupDomain_Click(object? sender, EventArgs e)
        {
            var domain = txtDomain.Text.Trim();
            if (string.IsNullOrWhiteSpace(domain))
            {
                MessageBox.Show("Please enter a domain name", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            lblStatus.Text = "Looking up...";
            lstResults.Items.Clear();

            try
            {
                var client = CreateLookupClient();
                var sw = System.Diagnostics.Stopwatch.StartNew();
                var result = await client.QueryAsync(domain, QueryType.A);
                sw.Stop();

                if (result.Answers.Count == 0)
                {
                    lstResults.Items.Add("No records found");
                    lblStatus.Text = "No results";
                    return;
                }

                var addresses = result.Answers.OfType<DnsClient.Protocol.ARecord>().Select(r => r.Address.ToString()).ToArray();
                foreach (var ip in addresses)
                {
                    lstResults.Items.Add($"📌 {ip}");
                }

                lblStatus.Text = $"Found {addresses.Length} result(s) in {sw.ElapsedMilliseconds}ms";
                SaveToHistory("A/AAAA", domain, sw.ElapsedMilliseconds, string.Join(", ", addresses));
                RefreshHistory();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Lookup Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblStatus.Text = "Error";
            }
        }

        private async void BtnReverseIp_Click(object? sender, EventArgs e)
        {
            var ipText = txtIpReverse.Text.Trim();
            if (!IPAddress.TryParse(ipText, out var ip))
            {
                MessageBox.Show("Invalid IP address", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            rtbReverseResults.Clear();
            rtbReverseResults.AppendText("Resolving...\r\n");

            try
            {
                var client = CreateLookupClient();
                var sw = System.Diagnostics.Stopwatch.StartNew();
                var result = await client.QueryReverseAsync(ip);
                sw.Stop();

                rtbReverseResults.Clear();
                var ptr = result.Answers.OfType<DnsClient.Protocol.PtrRecord>().FirstOrDefault();
                var hostName = ptr?.PtrDomainName?.ToString() ?? "Not found";

                rtbReverseResults.AppendText($"IP: {ip}\r\n");
                rtbReverseResults.AppendText($"Hostname: {hostName}\r\n");
                rtbReverseResults.AppendText($"Time: {sw.ElapsedMilliseconds}ms\r\n");

                SaveToHistory("PTR", ip.ToString(), sw.ElapsedMilliseconds, hostName);
                RefreshHistory();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Reverse Lookup Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void BtnQueryMulti_Click(object? sender, EventArgs e)
        {
            var query = txtQueryMulti.Text.Trim();
            if (string.IsNullOrWhiteSpace(query))
            {
                MessageBox.Show("Please enter a query", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var recordType = GetQueryType(cmbRecordType.SelectedItem?.ToString() ?? "A");
            rtbMultiResults.Clear();
            rtbMultiResults.AppendText("Querying...\r\n");

            try
            {
                var client = CreateLookupClient();
                var sw = System.Diagnostics.Stopwatch.StartNew();
                var result = await client.QueryAsync(query, recordType);
                sw.Stop();

                rtbMultiResults.Clear();

                if (result.Answers.Count == 0)
                {
                    rtbMultiResults.AppendText("No records found\r\n");
                    return;
                }

                foreach (var answer in result.Answers)
                {
                    string recordStr = FormatRecord(recordType, answer);
                    rtbMultiResults.AppendText($"• {recordStr}\r\n");
                }

                rtbMultiResults.AppendText($"\r\nTime: {sw.ElapsedMilliseconds}ms\r\n");

                SaveToHistory(recordType.ToString() ?? "Unknown", query, sw.ElapsedMilliseconds, "");
                RefreshHistory();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Query Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void BtnProcessBatch_Click(object? sender, EventArgs e)
        {
            var filePath = txtBatchFile.Text.Trim();
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                MessageBox.Show("Please select a valid file", "File Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            rtbBatchResults.Clear();
            rtbBatchResults.AppendText("Starting batch process...\r\n\r\n");

            try
            {
                var lines = (await File.ReadAllLinesAsync(filePath))
                    .Select(l => l.Trim())
                    .Where(l => !string.IsNullOrWhiteSpace(l))
                    .ToArray();

                progressBatch.Maximum = lines.Length;
                progressBatch.Value = 0;

                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];
                    try
                    {
                        var client = CreateLookupClient();
                        var sw = System.Diagnostics.Stopwatch.StartNew();
                        var res = await client.QueryAsync(line, QueryType.A);
                        sw.Stop();

                        var addrs = res.Answers.OfType<DnsClient.Protocol.ARecord>()
                            .Select(r => r.Address.ToString())
                            .ToArray();

                        string result = addrs.Length > 0 
                            ? string.Join(", ", addrs)
                            : "No results";

                        rtbBatchResults.AppendText($"✓ {line} => {result} ({sw.ElapsedMilliseconds}ms)\r\n");
                        SaveToHistory("A", line, sw.ElapsedMilliseconds, result);
                    }
                    catch (Exception ex)
                    {
                        rtbBatchResults.AppendText($"✗ {line} => Error: {ex.Message}\r\n");
                    }

                    progressBatch.Value = i + 1;
                    Application.DoEvents();
                }

                rtbBatchResults.AppendText("\r\nBatch process completed!");
                RefreshHistory();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Batch Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnConfigDns_Click(object? sender, EventArgs e)
        {
            var form = new DnsConfigForm { DnsServer = CustomDnsServer };
            if (form.ShowDialog() == DialogResult.OK)
            {
                CustomDnsServer = form.DnsServer;
                lblDnsServer.Text = $"DNS Server: {(CustomDnsServer?.ToString() ?? "Default")}";
            }
        }

        private void BtnBrowseBatch_Click(object? sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog { Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*" })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    txtBatchFile.Text = ofd.FileName;
                }
            }
        }

        private void BtnExportResults_Click(object? sender, EventArgs e)
        {
            using (var sfd = new SaveFileDialog { Filter = "Text Files (*.txt)|*.txt|CSV Files (*.csv)|*.csv" })
            {
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var lines = QueryHistory.Select(h => $"{h.Type}\t{h.Query}\t{h.Timestamp}\t{h.ElapsedMs}ms");
                        File.WriteAllLines(sfd.FileName, lines);
                        MessageBox.Show("Export successful!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Export failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void BtnGenerateReport_Click(object? sender, EventArgs e)
        {
            if (QueryHistory.Count == 0)
            {
                MessageBox.Show("No history to generate report", "Empty History", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("DNS Lookup Tool - Report");
                sb.AppendLine($"Generated: {DateTime.Now}");
                sb.AppendLine($"Total Queries: {QueryHistory.Count}\r\n");

                var byType = QueryHistory.GroupBy(h => h.Type)
                    .OrderByDescending(g => g.Count());

                sb.AppendLine("Statistics by Type:");
                foreach (var g in byType)
                {
                    var avg = g.Where(x => x.ElapsedMs >= 0).DefaultIfEmpty().Average(x => x.ElapsedMs);
                    sb.AppendLine($"  {g.Key}: {g.Count()} queries, avg {avg:F1}ms");
                }

                sb.AppendLine("\nTop 10 Queries:");
                var topQueries = QueryHistory.GroupBy(h => h.Query)
                    .OrderByDescending(g => g.Count())
                    .Take(10);
                foreach (var g in topQueries)
                {
                    sb.AppendLine($"  {g.Key}: {g.Count()} times");
                }

                var reportPath = "report.txt";
                File.WriteAllText(reportPath, sb.ToString());
                MessageBox.Show($"Report generated: {Path.GetFullPath(reportPath)}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Report generation failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Helper Methods
        private QueryType GetQueryType(string typeStr)
        {
            return typeStr switch
            {
                "AAAA" => QueryType.AAAA,
                "PTR" => QueryType.PTR,
                "MX" => QueryType.MX,
                "CNAME" => QueryType.CNAME,
                "TXT" => QueryType.TXT,
                "NS" => QueryType.NS,
                "SOA" => QueryType.SOA,
                _ => QueryType.A
            };
        }

        private string FormatRecord(QueryType type, DnsClient.Protocol.DnsResourceRecord answer)
        {
            return type switch
            {
                QueryType.A => $"A: {((DnsClient.Protocol.ARecord)answer).Address}",
                QueryType.AAAA => $"AAAA: {((DnsClient.Protocol.AaaaRecord)answer).Address}",
                QueryType.PTR => $"PTR: {((DnsClient.Protocol.PtrRecord)answer).PtrDomainName}",
                QueryType.MX => $"MX: {((DnsClient.Protocol.MxRecord)answer).Exchange} (Preference: {((DnsClient.Protocol.MxRecord)answer).Preference})",
                QueryType.CNAME => $"CNAME: {((DnsClient.Protocol.CNameRecord)answer).CanonicalName}",
                QueryType.TXT => $"TXT: {string.Join(" ", ((DnsClient.Protocol.TxtRecord)answer).Text)}",
                QueryType.NS => $"NS: {((DnsClient.Protocol.NsRecord)answer).NSDName}",
                QueryType.SOA => $"SOA: {((DnsClient.Protocol.SoaRecord)answer).MName}",
                _ => "Unknown record type"
            };
        }

        private LookupClient CreateLookupClient()
        {
            var options = CustomDnsServer != null
                ? new LookupClientOptions(CustomDnsServer) { Timeout = Timeout, Retries = RetryCount }
                : new LookupClientOptions { Timeout = Timeout, Retries = RetryCount };
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
            foreach (var entry in QueryHistory.OrderByDescending(h => h.Timestamp).Take(100))
            {
                dgvHistory.Rows.Add(entry.Type, entry.Query, entry.Timestamp.ToString("g"), entry.ElapsedMs);
            }
        }

        private void SaveHistory()
        {
            try
            {
                var json = JsonConvert.SerializeObject(QueryHistory);
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
                    var json = File.ReadAllText("history.json");
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
                var obj = new { ForceTcpOnly, EnableDnsSec };
                var json = JsonConvert.SerializeObject(obj, Formatting.Indented);
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
                    var json = File.ReadAllText(SecuritySettingsFile);
                    var node = JsonConvert.DeserializeObject<dynamic>(json);
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
        public string Type { get; set; } = "";
        public string Query { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public long ElapsedMs { get; set; }
        public string Details { get; set; } = "";
    }
}
