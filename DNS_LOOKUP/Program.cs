using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace DnsLookupTool
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            while (true)
            {
                PrintBanner();
                PrintMenu();

                Console.Write("Chọn chức năng (1-5): ");
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
                        ExportResults();
                        Pause();
                        break;
                    case "5":
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
            Console.WriteLine("║        🌐 DNS Lookup Tool v1.0         ║");
            Console.WriteLine("║        .NET Console Application        ║");
            Console.WriteLine("╚════════════════════════════════════════╝");
            Console.ResetColor();
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
            Console.WriteLine(" 4) 💾 Xuất kết quả ra file");
            Console.WriteLine(" 5) ❌ Thoát");
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

        // 1) Resolve domain to IPs (A/AAAA)
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

                var sw = Stopwatch.StartNew();
                IPAddress[] addresses = Dns.GetHostAddresses(domain);
                sw.Stop();

                if (addresses.Length == 0)
                {
                    Console.WriteLine("Không tìm thấy địa chỉ IP nào.");
                    return;
                }

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Kết quả (A/AAAA):");
                Console.ResetColor();

                foreach (var ip in addresses)
                {
                    var family = ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork
                        ? "IPv4 (A)"
                        : "IPv6 (AAAA)";
                    Console.WriteLine($" - {ip} [{family}]");
                }

                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Thời gian truy vấn: {sw.ElapsedMilliseconds} ms");
                Console.ResetColor();

                // Lưu vào bộ nhớ tạm (để xuất file nếu cần)
                LastResults = new ResultSet
                {
                    Domain = domain,
                    Timestamp = DateTime.Now,
                    ElapsedMs = sw.ElapsedMilliseconds,
                    Addresses = addresses
                };
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Lỗi tra cứu DNS: {ex.Message}");
                Console.ResetColor();
            }
        }

        // 2) Reverse lookup (PTR)
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

                var sw = Stopwatch.StartNew();
                IPHostEntry entry = Dns.GetHostEntry(ip);
                sw.Stop();

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Kết quả Reverse DNS (PTR):");
                Console.ResetColor();

                Console.WriteLine($" - Hostname: {entry.HostName}");

                if (entry.Aliases != null && entry.Aliases.Length > 0)
                {
                    Console.WriteLine(" - Aliases:");
                    foreach (var alias in entry.Aliases)
                        Console.WriteLine($"   • {alias}");
                }

                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Thời gian truy vấn: {sw.ElapsedMilliseconds} ms");
                Console.ResetColor();

                // Lưu vào bộ nhớ tạm
                LastReverseResult = new ReverseResult
                {
                    Ip = ip.ToString(),
                    HostName = entry.HostName,
                    Timestamp = DateTime.Now,
                    ElapsedMs = sw.ElapsedMilliseconds,
                    Aliases = entry.Aliases ?? Array.Empty<string>()
                };
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Lỗi tra cứu Reverse DNS: {ex.Message}");
                Console.ResetColor();
            }
        }

        // 3) Measure response time for domain resolution
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
                    _ = Dns.GetHostAddresses(domain);
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

            // Thống kê cơ bản
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

        // 4) Export last result to file
        static void ExportResults()
        {
            Console.WriteLine("Chọn loại dữ liệu để xuất:");
            Console.WriteLine("1) Kết quả IP từ tên miền (A/AAAA)");
            Console.WriteLine("2) Kết quả Reverse DNS (PTR)");
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
                    default:
                        Console.WriteLine("Lựa chọn không hợp lệ.");
                        return;
                }

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Xuất dữ liệu thành công: {Path.GetFullPath(path)}");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Lỗi khi xuất file: {ex.Message}");
                Console.ResetColor();
            }
        }

        // ======= Data writing helpers =======
        static void WriteDomainResults(ResultSet rs, string path)
        {
            bool isCsv = Path.GetExtension(path).Equals(".csv", StringComparison.OrdinalIgnoreCase);
            using var sw = new StreamWriter(path, false, new UTF8Encoding(true));

            if (isCsv)
            {
                sw.WriteLine("Domain,Timestamp,ElapsedMs,IP,Family");
                foreach (var ip in rs.Addresses)
                {
                    var family = ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork ? "IPv4" : "IPv6";
                    sw.WriteLine($"{rs.Domain},{rs.Timestamp:yyyy-MM-dd HH:mm:ss},{rs.ElapsedMs},{ip},{family}");
                }
            }
            else
            {
                sw.WriteLine($"Domain: {rs.Domain}");
                sw.WriteLine($"Timestamp: {rs.Timestamp:yyyy-MM-dd HH:mm:ss}");
                sw.WriteLine($"Elapsed: {rs.ElapsedMs} ms");
                sw.WriteLine("Addresses:");
                foreach (var ip in rs.Addresses)
                {
                    var family = ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork ? "IPv4" : "IPv6";
                    sw.WriteLine($" - {ip} [{family}]");
                }
            }
        }

        static void WriteReverseResults(ReverseResult rr, string path)
        {
            bool isCsv = Path.GetExtension(path).Equals(".csv", StringComparison.OrdinalIgnoreCase);
            using var sw = new StreamWriter(path, false, new UTF8Encoding(true));

            if (isCsv)
            {
                sw.WriteLine("IP,Timestamp,ElapsedMs,HostName,Aliases");
                string aliases = rr.Aliases != null && rr.Aliases.Length > 0
                    ? string.Join('|', rr.Aliases)
                    : "";
                sw.WriteLine($"{rr.Ip},{rr.Timestamp:yyyy-MM-dd HH:mm:ss},{rr.ElapsedMs},{rr.HostName},{aliases}");
            }
            else
            {
                sw.WriteLine($"IP: {rr.Ip}");
                sw.WriteLine($"Timestamp: {rr.Timestamp:yyyy-MM-dd HH:mm:ss}");
                sw.WriteLine($"Elapsed: {rr.ElapsedMs} ms");
                sw.WriteLine($"HostName: {rr.HostName}");
                if (rr.Aliases != null && rr.Aliases.Length > 0)
                {
                    sw.WriteLine("Aliases:");
                    foreach (var a in rr.Aliases) sw.WriteLine($" - {a}");
                }
            }
        }

        // ======= Simple stats helpers =======
        static double Average(long[] values)
        {
            long sum = 0;
            foreach (var v in values) sum += v;
            return values.Length == 0 ? 0 : (double)sum / values.Length;
        }

        static long Min(long[] values)
        {
            long min = long.MaxValue;
            foreach (var v in values) if (v < min) min = v;
            return values.Length == 0 ? 0 : min;
        }

        static long Max(long[] values)
        {
            long max = long.MinValue;
            foreach (var v in values) if (v > max) max = v;
            return values.Length == 0 ? 0 : max;
        }

        // ======= In-memory last results =======
        static ResultSet? LastResults { get; set; }
        static ReverseResult? LastReverseResult { get; set; }
    }

    // ======= Models =======
    class ResultSet
    {
        public string Domain { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public long ElapsedMs { get; set; }
        public IPAddress[] Addresses { get; set; } = Array.Empty<IPAddress>();
    }

    class ReverseResult
    {
        public string Ip { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public long ElapsedMs { get; set; }
        public string HostName { get; set; } = "";
        public string[] Aliases { get; set; } = Array.Empty<string>();
    }
}

