# DNS Lookup Tool (Console) - DOAN_LTM

Tiện ích console đơn giản để tra cứu DNS, đo thời gian phản hồi, batch xử lý và lưu lịch sử. Dùng cho mục đích học tập/đồ án.

## Tính năng
- Tra cứu A / AAAA (tên miền → IP).
- Tra cứu ngược PTR (IP → hostname).
- Tra cứu các loại bản ghi: A, AAAA, PTR, MX, CNAME, TXT, NS, SOA.
- Đo thời gian phản hồi (n lần) và thống kê (trung bình / min / max).
- Tra cứu nhiều bản ghi cho một query.
- Batch xử lý từ file (mỗi dòng một query).
- Lưu lịch sử truy vấn vào `history.json`.
- Xuất kết quả sang file (.txt/.csv) và tùy chọn nén ZIP.
- Cấu hình DNS server tùy chỉnh.
- Cài đặt bảo mật cơ bản (Force TCP only, DNSSEC — DNSSEC là stub).
- Tạo báo cáo đơn giản (`report.txt`).

## Cơ chế hoạt động (tóm tắt)
- Ứng dụng dùng thư viện DnsClient để thực hiện truy vấn DNS.
- Tạo LookupClient qua `CreateLookupClient()` với `LookupClientOptions` (có thể sử dụng DNS tùy chỉnh, timeout và số lần thử).
- Các chức năng:
  - ResolveDomain: gọi `client.Query(domain, QueryType.A)` để lấy A records.
  - ReverseLookup: gọi `client.QueryReverse(ip)` và đọc `PtrRecord.PtrDomainName` để lấy hostname.
  - QueryMultipleRecords: gọi `client.QueryAsync(query, queryType)` và xử lý từng kiểu bản ghi theo kiểu record.
  - MeasureResponseTime: lặp gọi Query để đo thời gian (Stopwatch).
  - BatchProcess: đọc file, chạy Query cho từng dòng, lưu kết quả.
- Lịch sử truy vấn được lưu vào danh sách `QueryHistory` và ghi/đọc JSON từ `history.json`.
- Cấu hình bảo mật được lưu/đọc từ `security_settings.json` (ForceTcpOnly, EnableDnsSec). Áp dụng ForceTcpOnly yêu cầu chỉnh thêm `CreateLookupClient()` nếu cần ép dùng TCP.
- Báo cáo: gom thống kê từ lịch sử và ghi `report.txt`.

## Thư viện / phụ thuộc
- .NET SDK 6.0+ (hoặc tương thích)
- NuGet packages:
  - DnsClient
  - Newtonsoft.Json

## Các file quan trọng
- Source: `/workspaces/DOAN_LTM/DNS_LOOKUP/Program.cs`
- Lịch sử: `history.json`
- Cấu hình bảo mật: `security_settings.json`
- Báo cáo: `report.txt`
- README: `/workspaces/DOAN_LTM/README.md`

## Cài đặt & chạy (trong dev container Ubuntu)
1. Mở terminal trong workspace:
   cd /workspaces/DOAN_LTM/DNS_LOOKUP
2. Cài các package nếu chưa có:
   dotnet add package DnsClient
   dotnet add package Newtonsoft.Json
3. Build và chạy:
   dotnet build
   dotnet run

## Kiểm tra PTR ngoài chương trình
- nslookup 8.8.8.8
- dig -x 8.8.8.8 +short
Lưu ý: 8.8.8.8 thường trả về `dns.google` chứ không phải `google.com`.

## Gợi ý mở rộng / debugging
- Muốn ép dùng TCP hoặc bật DNSSEC thực sự, chỉnh `CreateLookupClient()` để thiết lập `LookupClientOptions` tương ứng.
- Để xem chi tiết bản ghi PTR, dùng `result.Answers.OfType<PtrRecord>()` và lấy `PtrDomainName`.
- Batch có thể song song hóa bằng `Parallel.ForEach` hoặc Task-based concurrency nếu cần.
- Kiểm tra quyền ghi file và đường dẫn nếu không tạo được `history.json`/`report.txt`.

## License
Code dùng cho mục đích học tập / đồ án. Tuỳ chỉnh và tái sử dụng tuân theo giấy phép dự án (không kèm giấy phép cụ thể trong repo).
