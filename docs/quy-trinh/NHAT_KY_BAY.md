# NHẬT KÝ BẪY KỸ THUẬT (TRAP LOG & LESSONS LEARNED) — MiniLoyalty

> **Phạm vi**: Sổ tay ghi nhận, phân tích nguyên nhân gốc và biện pháp phòng ngừa các bẫy kỹ thuật thực tế gặp phải khi port 1-1 từ `2022.7.Loyalty` sang `MiniLoyalty`.  
> **Cập nhật**: 2026-10-16 11:15  
> **Nguồn dữ liệu**: Tổng hợp từ `repos\labs-audit\INDEX_CONVERT.md` (§5 gồm 47 bẫy của fleet), nhật ký thực thi `.parity-run\logs\*\write_r*.txt`, và lịch sử commit của `MiniLoyalty`.  
> **Trạng thái**: Nháp — chờ review  

---

## 1. Bảng tra cứu nhanh các bẫy kỹ thuật trọng tâm

| Mã bẫy | Tên bẫy / Triệu chứng | Nhóm | Mức độ | Hạng mục thực tế liên quan |
|---|---|---|---|---|
| **B-01** | `string.Contains()` trên SQLite dịch thành `instr()` phân biệt hoa/thường | Cơ sở dữ liệu | 🔴 Nghiêm trọng | #87 Mst_CardType, #77 Mst_CarBrand, #213 Mst_Province, MiniPIM |
| **B-02** | Toán tử `=` trên SQLite phân biệt hoa/thường trên chuỗi ASCII | Cơ sở dữ liệu | 🔴 Nghiêm trọng | #70 Mst_BirthPolicy, #97 Mst_CarModel, MiniStamp |
| **B-03** | SQLite `LIKE` không hỗ trợ so khớp không phân biệt hoa/thường tiếng Việt có dấu | Cơ sở dữ liệu | ⚠️ Cảnh báo (GAP) | #87 Mst_CardTypeController/Search |
| **B-04** | `EnsureCreated()` không cập nhật schema khi database đã tồn tại | Cơ sở dữ liệu | 🔴 Nghiêm trọng | #143 Mst_District, Data/PortInfra.cs, MiniWMS, MiniDMS |
| **B-05** | Kiểu số tiền tệ `MyValue`/float chuyển thành `decimal` trên SQLite | Cơ sở dữ liệu | 🔴 Nghiêm trọng | Luật chung port (§3 PORT_RULES_MiniLoyalty.md) |
| **B-06** | `DISTINCT` trên câu truy vấn chứa cột `IDENTITY` làm mất tác dụng dedupe | Cơ sở dữ liệu | ⚠️ Cảnh báo | #110 Mst_DealerCPController/Search |
| **B-07** | Đọc `StandardOutput.ReadLine()` chờ "Now listening" gây deadlock vô hạn | Tiến trình & Mạng | 🔴 Nghiêm trọng | Bẫy #44 fleet, toàn bộ pipeline verify runtime MiniLoyalty |
| **B-08** | Cấu hình `UseUrls` trong app ghi đè biến môi trường `ASPNETCORE_URLS` | Tiến trình & Mạng | ⚠️ Cảnh báo | Toàn bộ các lượt chạy verify runtime (phải dùng `$env:PORT`) |
| **B-09** | Minimal API ném ngoại lệ khi có nhiều hơn 1 tham số body phức hợp | Tiến trình & Mạng | 🔴 Nghiêm trọng | Bẫy #43 fleet (MiniHTC-A), quy tắc controller port MiniLoyalty |
| **B-10** | Port nhầm mã chết (`Delete.*`, `*.sync-conflict-*`, hàm cũ `_New2019...`) | Mã nguồn & Dữ liệu | 🔴 Nghiêm trọng | Bẫy #11/#35 fleet, toàn bộ 113 hạng mục đã port |
| **B-11** | Sao chép tài khoản, mật khẩu, token hệ thống hardcode từ nguồn | Bảo mật | 🔴 Cấm tuyệt đối | #112 Mst_DealerCP, #068 Map_QueryDealer_Member, Sự cố #112 |
| **B-12** | Nhầm lẫn giữa các hàm chuẩn hóa chuỗi `StdParam`, `StdStr`, `StdFlag` | Mã nguồn & Dữ liệu | ⚠️ Cảnh báo | Các trường mã (DLCPCode, OrgID, MST) và cờ FlagActive |
| **B-13** | Xóa toàn bộ bảng trong action Save gây mất dữ liệu tương tranh | Nghiệp vụ nguồn | ⚠️ Cảnh báo | #230 Mst_RankPolicyController/Save |
| **B-14** | Truy cập `rows[0]` không kiểm tra danh sách rỗng gây crash | Nghiệp vụ nguồn | ⚠️ Cảnh báo | #229 Mst_RankPolicyController/GetByRankPolicyCode |
| **B-15** | Agent chạm trần số bước an toàn (Tool Step Limit) bị ngắt phiên | Điều phối & Tooling | ⚠️ Cảnh báo | #091 Mst_CardTypeController/Update (bẫy #36) |
| **B-16** | Client PowerShell làm hỏng mã hóa UTF-8 khi gửi request body string | Điều phối & Tooling | ⚠️ Cảnh báo | #87 Mst_CardTypeController/Search |
| **B-17** | Chép số liệu báo cáo từ tài liệu khác mà không đo lại độc lập | Quy trình chất lượng | ⚠️ Cảnh báo | Bẫy #22 fleet, quy tắc kiểm tra của vai trò doc2 |

---

## 2. Phân tích chi tiết từng bẫy kỹ thuật

### Nhóm 1: Cơ sở dữ liệu & Truy vấn

#### B-01: `string.Contains()` trên SQLite dịch thành `instr()` phân biệt hoa/thường
- **Triệu chứng**: Build đạt `0 Error(s)`. Dữ liệu trong database có bản ghi chứa mã `PT-LOC-DAU`, nhưng khi tìm kiếm với từ khóa `loc`, câu truy vấn trả về 0 dòng kết quả.
- **Nguyên nhân gốc**: EF Core khi dịch `string.Contains()` sang SQLite sẽ sinh hàm nội tại `instr()`. Hàm `instr()` của SQLite mặc định phân biệt hoa/thường (Case-Sensitive), trong khi hệ thống nguồn dùng SQL Server với collation `SQL_Latin1_General_CP1_CI_AS` (Case-Insensitive).
- **Cách phòng tránh & Khắc phục**:
  Bắt buộc chuyển toàn bộ các phép tìm kiếm chuỗi sang `EF.Functions.Like()` với pattern đã được hạ chữ (`ToLower()`) và escape các ký tự đại diện (`%`, `_`, `\`):
  ```csharp
  var pat = "%" + PortQuery.EscapeLike(keyword.Trim().ToLower()) + "%";
  query = query.Where(x => EF.Functions.Like(x.BrandName.ToLower(), pat, "\\"));
  ```
- **Hạng mục liên quan**: Toàn bộ các action Search danh mục (#77, #87, #97, #139, #213).

#### B-02: Toán tử `=` trên SQLite phân biệt hoa/thường với chuỗi ASCII
- **Triệu chứng**: So sánh trạng thái `x.Status == "PENDING"` khi dữ liệu lưu `"pending"` trả về 0 dòng mà không báo lỗi.
- **Nguyên nhân gốc**: Toán tử `=` trên SQLite phân biệt hoa/thường đối với mọi chuỗi ký tự. SQL Server thì không phân biệt.
- **Cách phòng tránh & Khắc phục**:
  Khi so khớp chính xác (Equal) không phân biệt hoa/thường, sử dụng `EF.Functions.Like(cột, giá_trị)` **không có ký tự đại diện wildcard**:
  ```csharp
  query = query.Where(x => EF.Functions.Like(x.FlagActive, flagActive));
  ```
- **Hạng mục liên quan**: #70 Mst_BirthPolicy, #97 Mst_CarModel, #132 Mst_Discipline.

#### B-03: SQLite `LIKE` không hỗ trợ tiếng Việt có dấu
- **Triệu chứng**: Tìm kiếm `%van%` khớp `VAN`, nhưng tìm `%Văn%` không khớp `%VĂN%`.
- **Nguyên nhân gốc**: Lệnh `LIKE` của SQLite chỉ xử lý không phân biệt hoa/thường cho 128 ký tự ASCII tiêu chuẩn. Ký tự Unicode mở rộng (như `ă`, `ê`, `ơ`, `ư`) không được xử lý bởi collation mặc định của SQLite.
- **Cách phòng tránh & Khắc phục**:
  Ghi nhận đây là một **GAP không tránh khỏi của SQLite thuần**. Trong môi trường dev/test, chấp nhận hành vi này và kiểm thử với dữ liệu chuẩn. Khi triển khai production, cấu hình PostgreSQL với extension `unaccent` hoặc SQL Server với collation tiếng Việt chuyên dụng.
- **Hạng mục liên quan**: #87 Mst_CardType (kiểm thử `khởi` vs `KHỞI`).

#### B-04: `EnsureCreated()` không cập nhật schema khi DB đã tồn tại
- **Triệu chứng**: Thêm entity mới hoặc thuộc tính mới vào DbContext, chạy app thành công nhưng khi gọi API thì bị lỗi `Microsoft.Data.Sqlite.SqliteException: no such table` hoặc `no such column`.
- **Nguyên nhân gốc**: Phương thức `Database.EnsureCreated()` của EF Core kiểm tra nếu file database SQLite đã có trên đĩa thì sẽ **hoàn toàn không làm gì cả** (no-op), bỏ qua mọi bảng hoặc cột mới phát sinh sau đó.
- **Cách phòng tránh & Khắc phục**:
  Sử dụng cơ chế tự động migration SQLite (`MigrateSqliteAuto` / `Data/PortInfra.cs`):
  1. Trích xuất kịch bản DDL qua `GenerateCreateScript()`.
  2. Bổ sung mệnh đề `IF NOT EXISTS` cho các lệnh `CREATE TABLE`.
  3. Quét thông tin cột qua `PRAGMA table_info(<bảng>)` trước khi chạy `ALTER TABLE <bảng> ADD COLUMN <cột>`.
- **Hạng mục liên quan**: #143 Mst_District, các bảng mới thêm trong `Data/PortInfra.cs`.

#### B-05: Kiểu số tiền tệ `MyValue`/float chuyển thành `decimal` trên SQLite
- **Triệu chứng**: Các hàm tổng hợp `SUM()` trên cột số tiền trả về chuỗi hoặc kết quả sai lệch; so sánh lớn hơn / nhỏ hơn hoạt động theo thứ tự bảng chữ cái thay vì giá trị số học.
- **Nguyên nhân gốc**: SQLite không có kiểu dữ liệu `decimal` thực thụ; EF Core mặc định map `decimal` thành kiểu `TEXT`. Khi lưu trữ chuỗi, số `"100"` sẽ nhỏ hơn `"20"`.
- **Cách phòng tránh & Khắc phục**:
  Chuyển đổi toàn bộ các trường số thực, tiền tệ, điểm thưởng nguồn (`MyValue`) sang kiểu **`double?`** trong entity C#.
- **Hạng mục liên quan**: Mục 3 trong `PORT_RULES_MiniLoyalty.md`.

---

### Nhóm 2: Tiến trình, Mạng & Khởi động Runtime

#### B-07: Đọc `StandardOutput.ReadLine()` chờ "Now listening" gây deadlock
- **Triệu chứng**: Agent thực thi lệnh khởi động ứng dụng và treo bất động 20 - 30 phút, buộc runner phải ngắt tiến trình bằng timeout.
- **Nguyên nhân gốc**: Luồng xuất chuẩn (stdout) của ASP.NET Core trên Windows được đệm luồng (buffered). Chuỗi thông báo "Now listening on..." không được flush ra console ngay lập tức, làm lệnh đọc dòng `ReadLine()` bị khóa vô hạn.
- **Cách phòng tránh & Khắc phục**:
  Khởi động ứng dụng ngầm bằng `Start-Process`:
  ```powershell
  $p = Start-Process dotnet 'bin\Release\net8.0\MiniLoyalty.dll' -PassThru -WindowStyle Hidden
  # Thăm dò endpoint sức khỏe
  for ($i = 0; $i -lt 30; $i++) {
      Start-Sleep 1
      try {
          $res = Invoke-WebRequest "http://127.0.0.1:$env:PORT/healthz" -UseBasicParsing -TimeoutSec 2
          if ($res.StatusCode -eq 200) { break }
      } catch {}
  }
  ```
  Sau khi kiểm thử xong, bắt buộc tắt tiến trình bằng `Stop-Process -Id $p.Id -Force`.
- **Hạng mục liên quan**: Bẫy #44 trong INDEX_CONVERT, toàn bộ quy trình kiểm thử runtime MiniLoyalty.

#### B-08: Cấu hình `UseUrls` ghi đè biến môi trường `ASPNETCORE_URLS`
- **Triệu chứng**: Thiết lập biến môi trường `$env:ASPNETCORE_URLS = "http://127.0.0.1:8200"` nhưng ứng dụng vẫn cố bind vào cổng mặc định `8080`, dẫn đến lỗi `System.IO.IOException: Failed to bind to address ... Address already in use`.
- **Nguyên nhân gốc**: Mã nguồn `Program.cs` có dòng cấu hình:
  ```csharp
  builder.WebHost.UseUrls($"http://0.0.0.0:{Environment.GetEnvironmentVariable("PORT") ?? "8080"}");
  ```
  Phương thức `UseUrls()` có độ ưu tiên cao hơn và ghi đè hoàn toàn giá trị cấu hình của `ASPNETCORE_URLS`.
- **Cách phòng tránh & Khắc phục**:
  Bắt buộc đặt cổng chạy thông qua biến môi trường **`PORT`**:
  ```powershell
  $env:PORT = "8250"
  ```
- **Hạng mục liên quan**: Mọi kịch bản chạy thử nghiệm runtime của các agent.

#### B-09: Minimal API ném ngoại lệ khi có nhiều hơn 1 tham số body phức hợp
- **Triệu chứng**: Ứng dụng biên dịch thành công `0 Error(s)`. Tuy nhiên khi khởi động, ứng dụng lập tức sập với thông báo: `System.InvalidOperationException: Failure to infer one or more parameters`.
- **Nguyên nhân gốc**: ASP.NET Core Minimal API chỉ cho phép suy đoán duy nhất **một** tham số phức hợp gắn với Request Body. Nếu khai báo action có từ hai tham số DTO hoặc List trở lên, bộ định tuyến route table không thể phân giải.
- **Cách phòng tránh & Khắc phục**:
  Bọc tất cả các danh sách hoặc đối tượng tham số vào một DTO duy nhất (Wrapper DTO):
  ```csharp
  public class LineItemsRequest {
      public List<ServiceDto>? Services { get; set; }
      public List<PartDto>? Parts { get; set; }
  }
  ```
- **Hạng mục liên quan**: Bẫy #43 (phát hiện tại MiniHTC-A với 753 action).

---

### Nhóm 3: Mã nguồn, Dữ liệu & Nghiệp vụ

#### B-10: Port nhầm mã chết (`Delete.*`, `*.sync-conflict-*`, hàm cũ)
- **Triệu chứng**: Mã port chạy đúng với code đọc được, nhưng khi đối chiếu với hệ thống thực tế đang vận hành thì logic sai hoàn toàn; khách hàng báo cáo thiếu nghiệp vụ.
- **Nguyên nhân gốc**: Repo nguồn `2022.7.Loyalty` tích lũy nhiều năm phát triển, chứa hàng trăm file sao lưu của công cụ đồng bộ (`*.sync-conflict-*.cs`), thư mục `Delete.*`, hoặc nhiều biến thể hàm nghiệp vụ cùng tên (`WAS_Get`, `WAS_Get_New2022`, `WAS_Get_New2025`).
- **Cách phòng tránh & Khắc phục**:
  1. Luôn đi theo chuỗi gọi thực tế từ Controller: Gate -> WebAPI -> Biz Controller -> Biz LIVE method.
  2. Tuyệt đối không mở các file có tên bắt đầu bằng `Delete.` hoặc chứa cụm `sync-conflict`.
  3. Bản nào được hàm controller gọi trực tiếp mới là bản sống duy nhất.
- **Hạng mục liên quan**: Toàn bộ quy trình trích nguồn của 113 hạng mục.

#### B-11: Sao chép thông tin đăng nhập, token hệ thống hardcode từ nguồn
- **Triệu chứng**: Quét bảo mật phát hiện mật khẩu tài khoản hệ thống (system account), khóa API bí mật hoặc chuỗi kết nối database nằm trong mã nguồn hoặc comment.
- **Nguyên nhân gốc**: Mã nguồn cũ thường hardcode tài khoản quản trị để gọi service nội bộ (như tại `Mst_DealerCPController.cs:340` hoặc `Map_QueryDealer_MemberController.cs:280`). Agent vô tình sao chép nguyên văn cả tài khoản và mật khẩu vào mã mới.
- **Cách phòng tránh & Khắc phục**:
  1. Luật bất di bất dịch: Tuyệt đối không sao chép thông tin nhạy cảm vào bất kỳ đâu.
  2. Thay thế bằng ghi chú: *"tài khoản viết cứng tại file:dòng — không chép giá trị"*.
  3. Đánh dấu điểm này vào sổ GAP để chuyển đổi sang cơ chế cấu hình an toàn qua `appsettings.json` hoặc User Secrets.
- **Hạng mục liên quan**: Sự cố #112 trong nhật ký fleet, hạng mục #112 Mst_DealerCP.

#### B-12: Nhầm lẫn giữa các hàm chuẩn hóa `StdParam`, `StdStr`, `StdFlag`
- **Triệu chứng**: Mã đại lý `dlcp001` bị lưu nguyên dạng chữ thường, làm sai lệch kết quả tìm kiếm mã khóa chính; hoặc cờ `FlagActive` bị gán `null` thay vì `"0"`.
- **Nguyên nhân gốc**: DLL tiện ích nguồn `TUtils.CUtils` định nghĩa các hàm chuẩn hóa khác nhau rõ rệt nhưng lập trình viên dễ nhầm lẫn:
  - `StdParam(x)`: Dùng cho MÃ (Code, ID) -> bắt buộc `Trim().ToUpper()`, rỗng biến thành `null`.
  - `StdStr(x)`: Dùng cho TÊN, MÔ TẢ -> chỉ `Trim().Normalize()`, giữ nguyên chữ hoa/thường.
  - `StdFlag(x)`: Dùng cho CỜ (Flag) -> trả về `"1"` nếu đúng bằng `"1"`, mọi giá trị khác trả về `"0"`.
- **Cách phòng tránh & Khắc phục**:
  Đối chiếu chính xác dòng lệnh nguồn đang gọi hàm nào của `CUtils` để áp dụng hàm tương ứng trong mã port.
- **Hạng mục liên quan**: Toàn bộ các thực thể danh mục của MiniLoyalty.

---

### Nhóm 4: Điều phối, Kiểm thử & Môi trường

#### B-15: Agent chạm trần số bước an toàn (Tool Step Limit) bị ngắt phiên
- **Triệu chứng**: Lượt chạy của agent bị dừng giữa chừng với thông báo: `Paused after 50 consecutive tool steps as a safety limit`. File log bị ngắt, thiếu mục tóm tắt `TOM TAT`.
- **Nguyên nhân gốc**: Agent thực hiện quá nhiều thao tác dò dẫm, kiểm tra file thừa thãi hoặc vòng lặp sửa lỗi kéo dài vượt quá ngưỡng an toàn 50 tool calls của runner.
- **Cách phòng tránh & Khắc phục**:
  1. Đề bài phải cung cấp chính xác đường dẫn file, không để agent tự đi tìm kiếm mò mẫm.
  2. Bắt buộc agent ghi file kết quả ngay từ đầu và cập nhật liên tục thay vì đợi đến bước cuối cùng mới ghi file.
  3. Khi phát hiện phiên bị dừng do an toàn (như tại #091 `Mst_CardTypeController_Update`), reviewer kiểm tra diff nếu đã hoàn tất logic thì có thể phê duyệt commit và ghi nhận vết rõ ràng.
- **Hạng mục liên quan**: #091 Mst_CardTypeController/Update.

#### B-16: Client PowerShell làm hỏng mã hóa UTF-8 khi gửi request body string
- **Triệu chứng**: Gửi chuỗi JSON có chữ tiếng Việt qua `Invoke-RestMethod -Body $json` thì server nhận được ký tự rác (`kh?i` thay vì `khởi`), làm bài test tìm kiếm bị trượt vô lý.
- **Nguyên nhân gốc**: PowerShell console mặc định chuyển đổi chuỗi sang mã hóa ASCII hoặc ANSI của hệ điều hành trước khi truyền qua mạng nếu không chỉ định rõ kiểu dữ liệu.
- **Cách phòng tránh & Khắc phục**:
  Chuyển đổi chuỗi payload sang mảng byte UTF-8 tường minh trước khi gửi:
  ```powershell
  $bodyBytes = [System.Text.Encoding]::UTF8.GetBytes($jsonString)
  $response = Invoke-RestMethod -Uri $url -Method Post -Body $bodyBytes -ContentType "application/json; charset=utf-8"
  ```
- **Hạng mục liên quan**: Kiểm thử runtime tại #87 Mst_CardTypeController/Search.

---

## 3. Lịch sử cập nhật

| Phiên bản | Ngày cập nhật | Người thực hiện | Nội dung thay đổi |
|---|---|---|---|
| 1.0 | 2026-10-16 11:15 | doc2 (Quản lý quy trình) | Khởi tạo sổ tay bẫy kỹ thuật; phân loại 17 bẫy trọng tâm từ INDEX_CONVERT §5 và thực tiễn triển khai MiniLoyalty. |
