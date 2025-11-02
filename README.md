# 🌐 Ứng dụng phân giải tên miền thành địa chỉ IP

### 📖 Giới thiệu
Đây là đồ án môn **Lập trình mạng máy tính** với mục tiêu xây dựng một công cụ đơn giản giúp người dùng tra cứu DNS.  
Người dùng chỉ cần nhập một tên miền (ví dụ: `google.com`), chương trình sẽ hiển thị danh sách các địa chỉ IP tương ứng.

---

### 📝 Tóm tắt đồ án
- Người dùng nhập vào một **tên miền**.
- Ứng dụng sử dụng lớp **`Dns` của .NET** để phân giải tên miền.
- Kết quả trả về là danh sách các địa chỉ **IPv4/IPv6** tương ứng.

---

### 🎯 Kết quả đạt được
- ✅ Giải thích được **khái niệm và vai trò của hệ thống DNS** trong mạng (CLO1.1).  
- ✅ Sử dụng lớp **`Dns` của .NET** để thực hiện các truy vấn DNS (CLO2.1, CLO3.1).  
- ✅ Thiết kế một **công cụ mạng đơn giản, hữu ích** (CLO3.2).  

---

### 🔧 Các hướng mở rộng chức năng
1. **Hỗ trợ nhiều loại bản ghi DNS**  
   - MX (Mail Exchange) – máy chủ email  
   - NS (Name Server) – máy chủ tên miền  
   - CNAME – bí danh tên miền  
   - TXT – bản ghi văn bản (SPF, DKIM)  

2. **Kiểm tra tốc độ phản hồi DNS**  
   - Đo thời gian truy vấn từ nhiều DNS server (Google DNS, Cloudflare, OpenDNS).  
   - So sánh hiệu suất giữa các máy chủ DNS.  

3. **Phát hiện lỗi cấu hình DNS**  
   - Kiểm tra bản ghi MX hợp lệ.  
   - Kiểm tra sự trùng lặp hoặc thiếu bản ghi NS.  
   - Cảnh báo nếu thiếu bản ghi A/AAAA.  

4. **Xuất báo cáo kết quả**  
   - Lưu kết quả ra file `.txt`, `.csv`, hoặc `.html`.  

5. **Giao diện nâng cao**  
   - Ngoài console, có thể phát triển GUI bằng **WinForms hoặc WPF**.  
   - Hiển thị kết quả dạng bảng, có màu sắc phân biệt từng loại bản ghi.  
   - Thêm biểu đồ nhỏ để hiển thị tốc độ phản hồi DNS.  

6. **Tích hợp tra cứu ngược (Reverse DNS)**  
   - Nhập địa chỉ IP → trả về tên miền tương ứng.  

7. **So sánh kết quả từ nhiều DNS server**  
   - Ví dụ: so sánh Google DNS và Cloudflare DNS.  
   - Phát hiện tình trạng **DNS poisoning** hoặc sự khác biệt do caching.  

---

### 🚀 Cách chạy chương trình
1. Clone repo về máy:
   ```bash
   git clone [https://github.com/your-username/dns-lookup-tool.git](https://github.com/SuyMeoz/DOAN_LTM.git)
   cd DNS_LOOKUP
