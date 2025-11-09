using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using DnsClient;
using Newtonsoft.Json;

namespace DnsLookupTool
{
    class Program
    {
        // Cấu hình toàn cục
        static IPAddress? CustomDnsServer = null;
        static List<HistoryEntry> QueryHistory = new List<HistoryEntry>();
        static int RetryCount = 3;
        static TimeSpan Timeout = TimeSpan.FromSeconds(5);

        // Security settings
        static bool ForceTcpOnly = false;
        static bool EnableDnsSec = false; // note: DNSSEC handling is a stub (depends on resolver support)
        static string SecuritySettingsFile = "security_settings.json";

        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            LoadHistory();
            LoadSecuritySettings();

            while (true)
            {
                PrintBanner();
                PrintMenu();

                Console.Write("Chọn chức năng (1-11): ");
                var choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        ResolveDomain();
                        Pause();
                        break;
                    case "2":
                        ReverseLookup();
                        Pause();
                        break;
                    case "3":
                        MeasureResponseTime();
                        Pause();
                        break;
                    case "4":
                        QueryMultipleRecords().Wait();
                        Pause();
                        break;
                    case "5":
                        ExportResults();
                        Pause();
                        break;
                    case "6":
                        ConfigureDnsServer();
                        Pause();
                        break;
                    case "7":
                        ViewHistory();
                        Pause();
                        break;
                    case "8":
                        BatchProcess().Wait();
                        Pause();
                        break;
                    case "9":
                        GenerateReport();
                        Pause();
                        break;
                    case "10":
                        ConfigureSecurity();
                        Pause();
                        break;
                    case "11":
                        SaveHistory();
                        Console.WriteLine("Thoát chương trình. Tạm biệt!");
                        return;
                    default:
                        Console.WriteLine("Lựa chọn không hợp lệ. Vui lòng thử lại.");
                        Pause();
                        break;
                }
            }
        }

        // ======= UI helpers =======
        static void PrintBanner()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╔════════════════════════════════════════╗");
            Console.WriteLine("║        🌐 DNS Lookup Tool v1.2         ║");
            Console.WriteLine("║        .NET Console Application        ║");
            Console.WriteLine("╚════════════════════════════════════════╝");
            Console.ResetColor();
            Console.WriteLine($"DNS Server hiện tại: {(CustomDnsServer?.ToString() ?? "Mặc định")}");
            Console.WriteLine($"Retry: {RetryCount}, Timeout: {Timeout.TotalSeconds}s");
            Console.WriteLine();
        }

        static void PrintMenu()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Chọn chức năng:");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(" 1) 🔎 Tra cứu IP từ tên miền (A/AAAA)");
            Console.WriteLine(" 2) ↩️  Tra cứu ngược từ IP → tên miền (PTR)");
            Console.WriteLine(" 3) ⏱️  Đo thời gian phản hồi DNS");
            Console.WriteLine(" 4) 📋 Tra cứu nhiều loại bản ghi DNS");
            Console.WriteLine(" 5) 💾 Xuất kết quả ra file");
            Console.WriteLine(" 6) ⚙️  Cấu hình DNS Server tùy chỉnh");
            Console.WriteLine(" 7) 📜 Xem lịch sử truy vấn");
            Console.WriteLine(" 8) 📦 Batch tra cứu hàng loạt");
            Console.WriteLine(" 9) 📊 Thống kê và báo cáo");
            Console.WriteLine("10) 🔒 Cài đặt bảo mật");
            Console.WriteLine("11) ❌ Thoát");
            Console.ResetColor();
            Console.WriteLine();
        }

        static void Pause()
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("👉 Nhấn Enter để tiếp tục...");
            Console.ResetColor();
            Console.ReadLine();
        }

        // ======= Core features =======

        static void ResolveDomain()
        {
            Console.Write("Nhập tên miền (ví dụ: google.com): ");
            var domain = Console.ReadLine()?.Trim();

            if (string.IsNullOrWhiteSpace(domain))
            {
                Console.WriteLine("Tên miền không hợp lệ.");
                return;
            }

            try
            {
                Console.WriteLine();
                Console.WriteLine($"Đang tra cứu địa chỉ IP cho: {domain}");

                var client = CreateLookupClient();
                var sw = Stopwatch.StartNew();
                var result = client.Query(domain, QueryType.A);
                sw.Stop();

                if (result.Answers.Count == 0)
                {
                    Console.WriteLine("Không tìm thấy địa chỉ IP.");
                    return;
                }

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Kết quả (A/AAAA):");
                Console.ResetColor();

                var addresses = result.Answers.OfType<DnsClient.Protocol.ARecord>().Select(r => r.Address).ToArray();
                foreach (var ip in addresses)
                {
                    Console.WriteLine($" - {ip}");
                }

                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Thời gian truy vấn: {sw.ElapsedMilliseconds} ms");
                Console.ResetColor();

                LastResults = new ResultSet
                {
                    Domain = domain,
                    Timestamp = DateTime.Now,
                    ElapsedMs = sw.ElapsedMilliseconds,
                    Addresses = addresses
                };
                SaveToHistory("A/AAAA", domain ?? "", sw.ElapsedMilliseconds,
                    string.Join(", ", addresses.Select(a => a.ToString())));
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Lỗi tra cứu DNS: {ex.Message}");
                Console.ResetColor();
            }
        }

        static void ReverseLookup()
        {
            Console.Write("Nhập địa chỉ IP (ví dụ: 8.8.8.8): ");
            var ipText = Console.ReadLine()?.Trim();

            if (!IPAddress.TryParse(ipText, out var ip))
            {
                Console.WriteLine("Địa chỉ IP không hợp lệ.");
                return;
            }

            try
            {
                Console.WriteLine();
                Console.WriteLine($"Đang tra cứu tên miền cho IP: {ip}");

                var client = CreateLookupClient();
                var sw = Stopwatch.StartNew();
                var result = client.QueryReverse(ip);
                sw.Stop();

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Kết quả Reverse DNS (PTR):");
                Console.ResetColor();

                var ptr = result.Answers.OfType<DnsClient.Protocol.PtrRecord>().FirstOrDefault();
                var hostName = ptr?.PtrDomainName?.ToString() ?? "Không tìm thấy";
                Console.WriteLine($" - Hostname: {hostName}");

                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Thời gian truy vấn: {sw.ElapsedMilliseconds} ms");
                Console.ResetColor();

                LastReverseResult = new ReverseResult
                {
                    Ip = ip.ToString(),
                    HostName = hostName,
                    Timestamp = DateTime.Now,
                    ElapsedMs = sw.ElapsedMilliseconds,
                    Aliases = Array.Empty<string>()
                };
                SaveToHistory("PTR", ip.ToString(), sw.ElapsedMilliseconds, hostName);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Lỗi tra cứu Reverse DNS: {ex.Message}");
                Console.ResetColor();
            }
        }

        static void MeasureResponseTime()
        {
            Console.Write("Nhập tên miền để đo thời gian phản hồi: ");
            var domain = Console.ReadLine()?.Trim();
            if (string.IsNullOrWhiteSpace(domain))
            {
                Console.WriteLine("Tên miền không hợp lệ.");
                return;
            }

            Console.Write("Số lần đo (mặc định 5): ");
            var timesText = Console.ReadLine();
            int times = 5;
            if (!string.IsNullOrWhiteSpace(timesText))
            {
                int.TryParse(timesText, out times);
                times = Math.Clamp(times, 1, 50);
            }

            var measurements = new long[times];
            Console.WriteLine();

            for (int i = 0; i < times; i++)
            {
                var sw = Stopwatch.StartNew();
                try
                {
                    var client = CreateLookupClient();
                    _ = client.Query(domain, QueryType.A);
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Lần {i + 1}: lỗi - {ex.Message}");
                    Console.ResetColor();
                    measurements[i] = -1;
                    continue;
                }
                finally
                {
                    sw.Stop();
                }

                measurements[i] = sw.ElapsedMilliseconds;
                Console.WriteLine($"Lần {i + 1}: {measurements[i]} ms");
            }

            var valid = Array.FindAll(measurements, m => m >= 0);
            if (valid.Length > 0)
            {
                double avg = Average(valid);
                long min = Min(valid);
                long max = Max(valid);

                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Tổng kết cho {domain}:");
                Console.ResetColor();
                Console.WriteLine($" - Trung bình: {avg:F1} ms");
                Console.WriteLine($" - Nhanh nhất: {min} ms");
                Console.WriteLine($" - Chậm nhất: {max} ms");
            }
            else
            {
                Console.WriteLine("Không có mẫu hợp lệ để thống kê.");
            }
        }

        static async Task QueryMultipleRecords()
        {
            Console.Write("Nhập tên miền hoặc IP (ví dụ: google.com hoặc 8.8.8.8): ");
            var query = Console.ReadLine()?.Trim();

            if (string.IsNullOrWhiteSpace(query))
            {
                Console.WriteLine("Đầu vào không hợp lệ.");
                return;
            }

            Console.WriteLine("Chọn loại bản ghi (nhập số):");
            Console.WriteLine("1) A (IPv4)");
            Console.WriteLine("2) AAAA (IPv6)");
            Console.WriteLine("3) PTR (Reverse)");
            Console.WriteLine("4) MX (Mail Exchange)");
            Console.WriteLine("5) CNAME (Canonical Name)");
            Console.WriteLine("6) TXT (Text)");
            Console.WriteLine("7) NS (Name Server)");
            Console.WriteLine("8) SOA (Start of Authority)");
            Console.Write("Lựa chọn: ");
            var recordChoice = Console.ReadLine();

            QueryType queryType = GetQueryTypeFromChoice(recordChoice);

            try
            {
                Console.WriteLine();
                Console.WriteLine($"Đang tra cứu bản ghi {queryType} cho: {query}");

                var client = CreateLookupClient();
                var sw = Stopwatch.StartNew();
                var result = await client.QueryAsync(query, queryType);
                sw.Stop();

                if (result.Answers.Count == 0)
                {
                    Console.WriteLine("Không tìm thấy bản ghi nào.");
                    return;
                }

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Kết quả ({queryType}):");
                Console.ResetColor();

                var records = new List<string>();
                foreach (var answer in result.Answers)
                {
                    string recordStr = "";
                    switch (queryType)
                    {
                        case QueryType.A:
                            recordStr = $"A: {((DnsClient.Protocol.ARecord)answer).Address}";
                            break;
                        case QueryType.AAAA:
                            recordStr = $"AAAA: {((DnsClient.Protocol.AaaaRecord)answer).Address}";
                            break;
                        case QueryType.PTR:
                            recordStr = $"PTR: {((DnsClient.Protocol.PtrRecord)answer).PtrDomainName}";
                            break;
                        case QueryType.MX:
                            var mx = (DnsClient.Protocol.MxRecord)answer;
                            recordStr = $"MX: {mx.Exchange} (Priority: {mx.Preference})";
                            break;
                        case QueryType.CNAME:
                            recordStr = $"CNAME: {((DnsClient.Protocol.CNameRecord)answer).CanonicalName}";
                            break;
                        case QueryType.TXT:
                            recordStr = $"TXT: {string.Join(" ", ((DnsClient.Protocol.TxtRecord)answer).Text)}";
                            break;
                        case QueryType.NS:
                            recordStr = $"NS: {((DnsClient.Protocol.NsRecord)answer).NSDName}";
                            break;
                        case QueryType.SOA:
                            var soa = (DnsClient.Protocol.SoaRecord)answer;
                            recordStr = $"SOA: Primary: {soa.MName}, Responsible: {soa.RName}, Serial: {soa.Serial}";
                            break;
                    }
                    Console.WriteLine($" - {recordStr}");
                    records.Add(recordStr);
                }

                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Thời gian truy vấn: {sw.ElapsedMilliseconds} ms");
                Console.ResetColor();

                LastMultiResult = new MultiRecordResult
                {
                    Query = query,
                    RecordType = queryType.ToString(),
                    Timestamp = DateTime.Now,
                    ElapsedMs = sw.ElapsedMilliseconds,
                    Records = records.ToArray()
                };
                SaveToHistory(queryType.ToString(), query, sw.ElapsedMilliseconds, string.Join("; ", records));
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Lỗi tra cứu DNS: {ex.Message}");
                Console.ResetColor();
            }
        }

        static void ExportResults()
        {
            Console.WriteLine("Chọn loại dữ liệu để xuất:");
            Console.WriteLine("1) Kết quả IP từ tên miền (A/AAAA)");
            Console.WriteLine("2) Kết quả Reverse DNS (PTR)");
            Console.WriteLine("3) Kết quả nhiều bản ghi DNS");
            Console.Write("Lựa chọn: ");
            var choice = Console.ReadLine();

            Console.Write("Đường dẫn file (ví dụ: results.csv hoặc results.txt): ");
            var path = Console.ReadLine()?.Trim();

            if (string.IsNullOrWhiteSpace(path))
            {
                Console.WriteLine("Đường dẫn file không hợp lệ.");
                return;
            }

            try
            {
                switch (choice)
                {
                    case "1":
                        if (LastResults == null)
                        {
                            Console.WriteLine("Chưa có dữ liệu tra cứu tên miền để xuất.");
                            return;
                        }
                        WriteDomainResults(LastResults, path);
                        break;
                    case "2":
                        if (LastReverseResult == null)
                        {
                            Console.WriteLine("Chưa có dữ liệu reverse DNS để xuất.");
                            return;
                        }
                        WriteReverseResults(LastReverseResult, path);
                        break;
                    case "3":
                        if (LastMultiResult == null)
                        {
                            Console.WriteLine("Chưa có dữ liệu nhiều bản ghi để xuất.");
                            return;
                        }
                        WriteMultiResults(LastMultiResult, path);
                        break;
                    default:
                        Console.WriteLine("Lựa chọn không hợp lệ.");
                        return;
                }

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Xuất dữ liệu thành công: {Path.GetFullPath(path)}");
                Console.ResetColor();

                // Mã hóa cơ bản (tạo ZIP)
                Console.Write("Mã hóa file xuất (tạo ZIP)? (y/n): ");
                if (Console.ReadLine()?.ToLower() == "y")
                {
                    var zipPath = path + ".zip";
                    using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
                    {
                        zip.CreateEntryFromFile(path, Path.GetFileName(path), CompressionLevel.Optimal);
                    }
                    Console.WriteLine($"Đã tạo ZIP: {zipPath}");
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Lỗi khi xuất file: {ex.Message}");
                Console.ResetColor();
            }
        }

        static void ConfigureDnsServer()
        {
            Console.Write("Nhập IP DNS Server (ví dụ: 8.8.8.8) hoặc 'default' để reset: ");
            var input = Console.ReadLine()?.Trim();
            if (input == "default")
            {
                CustomDnsServer = null;
                Console.WriteLine("Đã reset về DNS mặc định.");
            }
            else if (IPAddress.TryParse(input, out var ip))
            {
                CustomDnsServer = ip;
                Console.WriteLine($"Đã đặt DNS Server: {ip}");
            }
            else
            {
                Console.WriteLine("IP không hợp lệ.");
            }
        }

        static void ViewHistory()
        {
            if (QueryHistory.Count == 0)
            {
                Console.WriteLine("Lịch sử trống.");
                return;
            }

            Console.WriteLine("Lịch sử truy vấn (gần nhất trước):");
            for (int i = QueryHistory.Count - 1; i >= 0; i--)
            {
                var entry = QueryHistory[i];
                Console.WriteLine($"{i + 1}) {entry.Type} - {entry.Query} - {entry.Timestamp} - {entry.ElapsedMs}ms");
            }

            Console.Write("Nhập số để xóa (hoặc Enter để bỏ qua): ");
            var delInput = Console.ReadLine();
            if (int.TryParse(delInput, out var index) && index > 0 && index <= QueryHistory.Count)
            {
                QueryHistory.RemoveAt(QueryHistory.Count - index);
                Console.WriteLine("Đã xóa.");
            }
        }

        static async Task BatchProcess()
        {
            Console.Write("Nhập đường dẫn file danh sách (mỗi dòng một query, ví dụ: domains.txt): ");
            var filePath = Console.ReadLine()?.Trim();

            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                Console.WriteLine("File không tồn tại hoặc đường dẫn không hợp lệ.");
                return;
            }

            var lines = await File.ReadAllLinesAsync(filePath);
            foreach (var line in lines.Select(l => l.Trim()).Where(l => !string.IsNullOrWhiteSpace(l)))
            {
                try
                {
                    var client = CreateLookupClient();
                    var sw = Stopwatch.StartNew();
                    var res = client.Query(line, QueryType.A);
                    sw.Stop();

                    var addrs = res.Answers.OfType<DnsClient.Protocol.ARecord>().Select(r => r.Address.ToString()).ToArray();
                    SaveToHistory("A", line, sw.ElapsedMilliseconds, string.Join(", ", addrs));
                    Console.WriteLine($"{line} => {string.Join(", ", addrs)} ({sw.ElapsedMilliseconds} ms)");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{line} => Lỗi: {ex.Message}");
                }
            }
        }

        // ======= Stubs / types / helpers để biên dịch =======
        static ResultSet? LastResults = null;
        static ReverseResult? LastReverseResult = null;
        static MultiRecordResult? LastMultiResult = null;

        class HistoryEntry
        {
            public string Type { get; set; } = "";
            public string Query { get; set; } = "";
            public DateTime Timestamp { get; set; }
            public long ElapsedMs { get; set; }
            public string Details { get; set; } = "";
        }

        class ResultSet
        {
            public string Domain { get; set; } = "";
            public DateTime Timestamp { get; set; }
            public long ElapsedMs { get; set; }
            public System.Net.IPAddress[] Addresses { get; set; } = Array.Empty<System.Net.IPAddress>();
        }

        class ReverseResult
        {
            public string Ip { get; set; } = "";
            public string HostName { get; set; } = "";
            public DateTime Timestamp { get; set; }
            public long ElapsedMs { get; set; }
            public string[] Aliases { get; set; } = Array.Empty<string>();
        }

        class MultiRecordResult
        {
            public string Query { get; set; } = "";
            public string RecordType { get; set; } = "";
            public DateTime Timestamp { get; set; }
            public long ElapsedMs { get; set; }
            public string[] Records { get; set; } = Array.Empty<string>();
        }

        static LookupClient CreateLookupClient()
        {
            var options = new LookupClientOptions();
            if (CustomDnsServer != null)
            {
                options = new LookupClientOptions(CustomDnsServer) { Timeout = Timeout, Retries = RetryCount };
            }
            else
            {
                options = new LookupClientOptions { Timeout = Timeout, Retries = RetryCount };
            }
            return new LookupClient(options);
        }

        static QueryType GetQueryTypeFromChoice(string? choice)
        {
            return choice switch
            {
                "1" => QueryType.A,
                "2" => QueryType.AAAA,
                "3" => QueryType.PTR,
                "4" => QueryType.MX,
                "5" => QueryType.CNAME,
                "6" => QueryType.TXT,
                "7" => QueryType.NS,
                "8" => QueryType.SOA,
                _ => QueryType.A
            };
        }

        static void SaveToHistory(string type, string query, long elapsedMs, string details)
        {
            QueryHistory.Add(new HistoryEntry { Type = type, Query = query, Timestamp = DateTime.Now, ElapsedMs = elapsedMs, Details = details });
        }

        static void SaveHistory()
        {
            try
            {
                var json = JsonConvert.SerializeObject(QueryHistory);
                File.WriteAllText("history.json", json);
            }
            catch { /* ignore errors for stub */ }
        }

        static void LoadHistory()
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

        static void WriteDomainResults(ResultSet res, string path)
        {
            var lines = new List<string> { $"Domain: {res.Domain}", $"Timestamp: {res.Timestamp}", $"ElapsedMs: {res.ElapsedMs}" };
            lines.AddRange(res.Addresses.Select(a => a.ToString()));
            File.WriteAllLines(path, lines);
        }

        static void WriteReverseResults(ReverseResult res, string path)
        {
            var lines = new[] { $"IP: {res.Ip}", $"Hostname: {res.HostName}", $"Timestamp: {res.Timestamp}", $"ElapsedMs: {res.ElapsedMs}" };
            File.WriteAllLines(path, lines);
        }

        static void WriteMultiResults(MultiRecordResult res, string path)
        {
            var lines = new List<string> { $"Query: {res.Query}", $"Type: {res.RecordType}", $"Timestamp: {res.Timestamp}", $"ElapsedMs: {res.ElapsedMs}" };
            lines.AddRange(res.Records);
            File.WriteAllLines(path, lines);
        }

        static double Average(long[] arr) => arr.Length == 0 ? 0 : arr.Average();
        static long Min(long[] arr) => arr.Length == 0 ? 0 : arr.Min();
        static long Max(long[] arr) => arr.Length == 0 ? 0 : arr.Max();

        static void ConfigureSecurity()
        {
            // Load current settings (no-op if already loaded)
            LoadSecuritySettings();

            Console.WriteLine("Cấu hình bảo mật DNS:");
            Console.WriteLine($"1) Force TCP only: {(ForceTcpOnly ? "Bật" : "Tắt")}");
            Console.WriteLine($"2) Enable DNSSEC validation (stub): {(EnableDnsSec ? "Bật" : "Tắt")}");
            Console.WriteLine("3) Reset về mặc định");
            Console.WriteLine("Enter để thoát mà không thay đổi.");
            Console.Write("Chọn mục để chuyển trạng thái (1/2/3): ");
            var input = Console.ReadLine()?.Trim();

            switch (input)
            {
                case "1":
                    ForceTcpOnly = !ForceTcpOnly;
                    Console.WriteLine($"Force TCP only đã {(ForceTcpOnly ? "Bật" : "Tắt")}");
                    break;
                case "2":
                    EnableDnsSec = !EnableDnsSec;
                    Console.WriteLine($"DNSSEC validation (stub) đã {(EnableDnsSec ? "Bật" : "Tắt")}");
                    break;
                case "3":
                    ForceTcpOnly = false;
                    EnableDnsSec = false;
                    Console.WriteLine("Đã reset về mặc định.");
                    break;
                default:
                    Console.WriteLine("Không thay đổi.");
                    break;
            }

            SaveSecuritySettings();
            Console.WriteLine("Lưu cấu hình xong. Lưu ý: một số thay đổi cần khởi động lại hoặc phụ thuộc vào resolver được sử dụng.");
        }

        static void GenerateReport()
        {
            if (QueryHistory.Count == 0)
            {
                Console.WriteLine("Không có dữ liệu lịch sử để tạo báo cáo.");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("DNS Lookup Tool - Báo cáo");
            sb.AppendLine($"Thời gian tạo: {DateTime.Now}");
            sb.AppendLine($"Tổng truy vấn: {QueryHistory.Count}");
            sb.AppendLine();

            var byType = QueryHistory.GroupBy(h => h.Type)
                                     .Select(g => new { Type = g.Key, Count = g.Count(), AvgMs = g.Where(x => x.ElapsedMs >= 0).DefaultIfEmpty().Average(x => x.ElapsedMs) })
                                     .OrderByDescending(x => x.Count)
                                     .ToList();
            sb.AppendLine("Thống kê theo loại:");
            foreach (var t in byType)
            {
                sb.AppendLine($" - {t.Type}: {t.Count} truy vấn, trung bình {(t.AvgMs):F1} ms");
            }
            sb.AppendLine();

            var topQueries = QueryHistory.GroupBy(h => h.Query)
                                         .Select(g => new { Query = g.Key, Count = g.Count() })
                                         .OrderByDescending(x => x.Count)
                                         .Take(10);
            sb.AppendLine("Top 10 truy vấn nhiều nhất:");
            foreach (var q in topQueries)
            {
                sb.AppendLine($" - {q.Query} ({q.Count} lần)");
            }
            sb.AppendLine();

            var reportPath = "report.txt";
            File.WriteAllText(reportPath, sb.ToString());
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Báo cáo tạo xong: {Path.GetFullPath(reportPath)}");
            Console.ResetColor();
        }

        static void SaveSecuritySettings()
        {
            try
            {
                var obj = new { ForceTcpOnly, EnableDnsSec };
                var json = JsonConvert.SerializeObject(obj, Formatting.Indented);
                File.WriteAllText(SecuritySettingsFile, json);
            }
            catch { /* ignore errors */ }
        }

        static void LoadSecuritySettings()
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
    }
}