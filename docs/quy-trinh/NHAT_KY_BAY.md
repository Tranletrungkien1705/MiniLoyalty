# NHẬT KÝ BẪY KỸ THUẬT (TRAP LOG & LESSONS LEARNED) — MiniLoyalty

> **Phạm vi**: Sổ tay ghi nhận, phân tích nguyên nhân gốc và biện pháp phòng ngừa các bẫy kỹ thuật thực tế gặp phải khi port 1-1 từ `2022.7.Loyalty` sang `MiniLoyalty`.  
> **Cập nhật**: 2026-10-16 12:00  
> **Nguồn dữ liệu**: Tổng hợp từ `repos\labs-audit\INDEX_CONVERT.md` (§5 gồm 47 bẫy của fleet), `docs\LESSONS_AGENTS.md` (mục A, C, D), nhật ký thực thi `.parity-run\logs\*\write_r*.txt`, và các commit hạ tầng của `MiniLoyalty` (`1752274`, `02911b1`).  
> **Trạng thái**: Nháp — chờ review  

---

## 1. Bảng tra cứu nhanh các bẫy kỹ thuật trọng tâm

| Mã bẫy | Tên bẫy / Triệu chứng | Nhóm | Mức độ | Hạng mục thực tế liên quan |
|---|---|---|---|---|
| **B-01** | `string.Contains()` trên SQLite dịch thành `instr()` phân biệt hoa/thường | Cơ sở dữ liệu | 🔴 Nghiêm trọng | #87 Mst_CardType, #77 Mst_CarBrand, #213 Mst_Province, MiniPIM |
| **B-02** | Toán tử `=` trên SQLite phân biệt hoa/thường trên chuỗi ASCII | Cơ sở dữ liệu | 🔴 Nghiêm trọng | #70 Mst_BirthPolicy, #97 Mst_CarModel, MiniStamp |
| **B-03** | SQLite `lower()` không hạ chữ tiếng Việt có dấu (`Đ` không thành `đ`) | Cơ sở dữ liệu | 🔴 Đã xử lý (02911b1) | #87 Mst_CardType, #152 FamilyStatus, QA-qa1-1, `SqliteUnicodeLowerInterceptor` |
| **B-04** | `EnsureCreated()` không cập nhật schema khi database đã tồn tại | Cơ sở dữ liệu | 🔴 Nghiêm trọng | #143 Mst_District, Data/PortInfra.cs, MiniWMS, MiniDMS |
| **B-05** | Kiểu số tiền tệ `MyValue`/float chuyển thành `decimal` trên SQLite | Cơ sở dữ liệu | 🔴 Nghiêm trọng | Luật chung port (§3 PORT_RULES_MiniLoyalty.md) |
| **B-06** | `DISTINCT` trên câu truy vấn chứa cột `IDENTITY` làm mất tác dụng dedupe | Cơ sở dữ liệu | ⚠️ Cảnh báo | #110 Mst_DealerCPController/Search |
| **B-07** | Đọc `StandardOutput.ReadLine()` chờ "Now listening" gây deadlock vô hạn | Tiến trình & Mạng | 🔴 Nghiêm trọng | Bẫy #44 fleet, toàn bộ pipeline verify runtime MiniLoyalty |
| **B-08** | Cấu hình `UseUrls` trong app ghi đè biến môi trường `ASPNETCORE_URLS` | Tiến trình & Mạng | ⚠️ Cảnh báo | Toàn bộ các lượt chạy verify runtime (phải dùng `$env:PORT`) |
| **B-09** | Minimal API ném ngoại lệ khi có nhiều hơn 1 tham số body phức hợp | Tiến trình & Mạng | 🔴 Nghiêm trọng | Bẫy #43 fleet (MiniHTC-A), quy tắc controller port MiniLoyalty |
| **B-10** | Port nhầm mã chết (`Delete.*`, `*.sync-conflict-*`, hàm cũ `_New2019...`) | Mã nguồn & Dữ liệu | 🔴 Nghiêm trọng | Bẫy #11/#35 fleet, toàn bộ 121 hạng mục đã port |
| **B-11** | Sao chép tài khoản, mật khẩu, token hệ thống hardcode từ nguồn | Bảo mật | 🔴 Cấm tuyệt đối | #112 Mst_DealerCP, #068 Map_QueryDealer_Member, Sự cố #112 |
| **B-12** | Nhầm lẫn giữa các hàm chuẩn hóa chuỗi `StdParam`, `StdStr`, `StdFlag` | Mã nguồn & Dữ liệu | ⚠️ Cảnh báo | Các trường mã (DLCPCode, OrgID, MST) và cờ FlagActive |
| **B-13** | Xóa toàn bộ bảng trong action Save gây mất dữ liệu tương tranh | Nghiệp vụ nguồn | ⚠️ Cảnh báo | #230 Mst_RankPolicyController/Save |
| **B-14** | Truy cập `rows[0]` không kiểm tra danh sách rỗng gây crash | Nghiệp vụ nguồn | ⚠️ Cảnh báo | #229 Mst_RankPolicyController/GetByRankPolicyCode |
| **B-15** | Agent chạm trần số bước an toàn (Tool Step Limit) bị ngắt phiên | Điều phối & Tooling | ⚠️ Cảnh báo | #091 Mst_CardTypeController/Update (bẫy #36) |
| **B-16** | Client PowerShell làm hỏng mã hóa UTF-8 khi gửi request body string | Điều phối & Tooling | ⚠️ Cảnh báo | #87 Mst_CardTypeController/Search |
| **B-17** | Chép số liệu báo cáo từ tài liệu khác mà không đo lại độc lập | Quy trình chất lượng | ⚠️ Cảnh báo | Bẫy #22 fleet, quy tắc kiểm tra của vai trò doc2 |
| **B-18** | Ngoại lệ chưa bắt trong action sập HTTP 500 thay vì trả 400 chuẩn | Kiến trúc & Xử lý lỗi | 🔴 Nghiêm trọng | #129 Mst_Department, QA-qa1-4, QA-qa2-1, `PortExceptionFilter` (02911b1) |
| **B-19** | Đăng ký `CreateFunction("lower")` lặp lại trong từng action/GetXAsync | Cơ sở dữ liệu | ⚠️ Cảnh báo | #152/#153 Mst_FamilyStatus, Luật A.1, `SqliteUnicodeLowerInterceptor` |
| **B-20** | Phân trang: `Ft_PageIndex` âm hoặc tràn int trả nhầm trang đầu | Thuật toán & Dữ liệu | 🔴 Nghiêm trọng | QA-qa1-2/3, Luật A.12, vá 16 vị trí tại 22 file (02911b1) |
| **B-21** | Trùng lặp số thứ tự `Order =>` trong các class `IPortSeeder` | Cơ sở dữ liệu & Seed | ⚠️ Cảnh báo | #153 Mst_FamilyStatus (trùng 32 với Mst_Department), Luật A.4 |
| **B-22** | JSON serialize tự động thành camelCase thay vì PascalCase nguồn | Giao diện API | 🔴 Nghiêm trọng | QA-qa1-5, Luật A.11, `PortJsonResultFilter` (02911b1) |
| **B-23** | Tài liệu tự bịa mã lỗi hoặc route không có thật trong mã nguồn | Quy trình chất lượng | 🔴 Nghiêm trọng | Tài liệu lượt 3 (14 mã lỗi bịa), Luật C.6 |
| **B-24** | Trả nguyên văn `ex.Message` làm lộ thông tin hạ tầng DBMS nội bộ | Bảo mật thông tin | ⚠️ Cảnh báo (GAP) | QA-qa2-4, Quyết định Claude (chấp nhận khớp nguồn, cần bọc nếu công khai) |
| **B-25** | Runner kẹt tiến trình CodeGPT 37 phút do nhầm mã trạng thái BUSY | Điều phối & Tooling | 🔴 Nghiêm trọng | Công cụ `cg-run.ps1`, mã thoát 7 = BUSY, tự hủy run qua API |

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

#### B-03: SQLite `lower()` không hạ chữ tiếng Việt có dấu (`Đ` không thành `đ`)
- **Triệu chứng**: Tìm kiếm từ khóa `ĐÀ NẴNG` hoặc `ĐỘC` trên SQLite trả về 0 dòng kết quả dù dữ liệu trong DB có chứa chính xác tên đó ở dạng chữ hoa hoặc chữ thường (chứng cứ lỗi QA-qa1-1).
- **Nguyên nhân gốc**: Hàm `lower()` tích hợp sẵn của SQLite chỉ xử lý 128 ký tự ASCII tiêu chuẩn. Ký tự Unicode tiếng Việt có dấu (như `Đ`, `Â`, `Ơ`, `Ư`) không được hạ chữ. Khi EF Core dịch `cot.ToLower()` thành `lower("cot")`, giá trị trong SQL vẫn giữ nguyên chữ hoa, trong khi pattern C# đã thành chữ thường, dẫn đến so sánh không khớp.
- **Cách phòng tránh & Khắc phục**:
  - Đã được giải quyết triệt để ở cấp kết nối hạ tầng bằng `SqliteUnicodeLowerInterceptor` (commit `02911b1`).
  - Lớp interceptor tự động đăng ký `sqlite.CreateFunction("lower", value => value?.ToLowerInvariant(), isDeterministic: true)` mỗi khi mở kết nối.
  - Phía ứng dụng: Chỉ cần viết `EF.Functions.Like(cot.ToLower(), pattern, "\\")` như bình thường, **tuyệt đối không đăng ký hàm thủ công trong action** (Bẫy B-19).
- **Hạng mục liên quan**: #87 Mst_CardType, #152 Mst_FamilyStatus, QA-qa1-1, `Data/SqliteUnicodeLower.cs`.

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

#### B-06: `DISTINCT` trên câu truy vấn chứa cột `IDENTITY` làm mất tác dụng dedupe
- **Triệu chứng**: Truy vấn nghiệp vụ mong muốn loại bỏ bản ghi trùng lặp nhưng kết quả trả về vẫn đầy đủ các dòng trùng.
- **Nguyên nhân gốc**: Khi câu lệnh `SELECT DISTINCT` bao gồm cả cột khóa chính tự tăng `IDENTITY` / `Id`, mỗi dòng đều có giá trị Id duy nhất nên từ khóa `DISTINCT` hoàn toàn không có tác dụng.
- **Cách phòng tránh & Khắc phục**:
  Chỉ `SELECT` các cột nghiệp vụ cần distinct, hoặc nhóm theo mã nghiệp vụ (`GroupBy(x => x.DLCPCode).Select(...)`).
- **Hạng mục liên quan**: #110 Mst_DealerCPController/Search.

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
  Bọc tất cả các danh sách hoặc đối tượng tham số vào một DTO duy nhất (Wrapper DTO).
- **Hạng mục liên quan**: Bẫy #43 (phát hiện tại MiniHTC-A với 753 action).

---

### Nhóm 3: Mã nguồn, Dữ liệu & Nghiệp vụ

#### B-10: Port nhầm mã chết (`Delete.*`, `*.sync-conflict-*`, hàm cũ)
- **Triệu chứng**: Mã port chạy đúng với code đọc được, nhưng khi đối chiếu với hệ thống thực tế đang vận hành thì logic sai hoàn toàn; khách hàng báo cáo thiếu nghiệp vụ.
- **Nguyên nhân gốc**: Repo nguồn `2022.7.Loyalty` chứa hàng trăm file sao lưu của công cụ đồng bộ (`*.sync-conflict-*.cs`), thư mục `Delete.*`, hoặc nhiều biến thể hàm nghiệp vụ cùng tên (`WAS_Get`, `WAS_Get_New2022`, `WAS_Get_New2025`).
- **Cách phòng tránh & Khắc phục**:
  1. Luôn đi theo chuỗi gọi thực tế từ Controller: Gate → WebAPI → Biz Controller → Biz LIVE method.
  2. Tuyệt đối không mở các file có tên bắt đầu bằng `Delete.` hoặc chứa cụm `sync-conflict`.
  3. Bản nào được hàm controller gọi trực tiếp mới là bản sống duy nhất.
- **Hạng mục liên quan**: Toàn bộ quy trình trích nguồn của 121 hạng mục.

#### B-11: Sao chép thông tin đăng nhập, token hệ thống hardcode từ nguồn
- **Triệu chứng**: Quét bảo mật phát hiện mật khẩu tài khoản hệ thống (system account), khóa API bí mật hoặc chuỗi kết nối database nằm trong mã nguồn hoặc comment.
- **Nguyên nhân gốc**: Mã nguồn cũ thường hardcode tài khoản quản trị để gọi service nội bộ (như tại `Mst_DealerCPController.cs:340` hoặc `Map_QueryDealer_MemberController.cs:280`).
- **Cách phòng tránh & Khắc phục**:
  1. Luật bất di bất dịch: Tuyệt đối không sao chép thông tin nhạy cảm vào bất kỳ đâu.
  2. Thay thế bằng ghi chú: *"tài khoản viết cứng tại file:dòng — không chép giá trị"*.
- **Hạng mục liên quan**: Sự cố #112 trong nhật ký fleet, hạng mục #112 Mst_DealerCP.

#### B-12: Nhầm lẫn giữa các hàm chuẩn hóa `StdParam`, `StdStr`, `StdFlag`
- **Triệu chứng**: Mã đại lý `dlcp001` bị lưu nguyên dạng chữ thường, làm sai lệch kết quả tìm kiếm mã khóa chính; hoặc cờ `FlagActive` bị gán `null` thay vì `"0"`.
- **Nguyên nhân gốc**: DLL tiện ích nguồn `TUtils.CUtils` định nghĩa các hàm chuẩn hóa khác nhau rõ rệt:
  - `StdParam(x)`: Dùng cho MÃ (Code, ID) → bắt buộc `Trim().ToUpper()`, rỗng biến thành `null`.
  - `StdStr(x)`: Dùng cho TÊN, MÔ TẢ → chỉ `Trim().Normalize()`, giữ nguyên chữ hoa/thường.
  - `StdFlag(x)`: Dùng cho CỜ (Flag) → trả về `"1"` nếu đúng bằng `"1"`, mọi giá trị khác trả về `"0"`.
- **Cách phòng tránh & Khắc phục**:
  Đối chiếu chính xác dòng lệnh nguồn đang gọi hàm nào của `CUtils` để áp dụng hàm tương ứng trong mã port.
- **Hạng mục liên quan**: Toàn bộ các thực thể danh mục của MiniLoyalty.

#### B-13: Xóa toàn bộ bảng trong action Save gây mất dữ liệu tương tranh
- **Triệu chứng**: Khi hai người dùng thực hiện cập nhật chính sách hạng thẻ cùng lúc, toàn bộ dữ liệu bị mất trắng hoặc chỉ còn lại dữ liệu của người bấm sau cùng.
- **Nguyên nhân gốc**: Nghiệp vụ nguồn của `Mst_RankPolicy/Save` thực hiện xóa toàn bộ bảng trước khi chèn lại dữ liệu mới (`DELETE FROM Mst_RankPolicy`).
- **Cách phòng tránh & Khắc phục**:
  Bọc toàn bộ thao tác trong database transaction chặt chẽ; ghi chú cảnh báo hành vi nguồn trong mã và sổ GAP.
- **Hạng mục liên quan**: #230 Mst_RankPolicyController/Save.

#### B-14: Truy cập `rows[0]` không kiểm tra danh sách rỗng gây crash
- **Triệu chứng**: Gửi yêu cầu lấy chi tiết một mã không tồn tại làm server sập với lỗi `System.IndexOutOfRangeException`.
- **Nguyên nhân gốc**: Mã nguồn cũ thường giả định dữ liệu luôn tồn tại và truy cập trực tiếp `dt.Rows[0]`.
- **Cách phòng tránh & Khắc phục**:
  Kiểm tra danh sách rỗng trước khi truy cập phần tử đầu tiên (`FirstOrDefaultAsync()`), nếu không thấy thì trả `Data = null` hoặc mã lỗi tương ứng.
- **Hạng mục liên quan**: #229 Mst_RankPolicyController/GetByRankPolicyCode.

---

### Nhóm 4: Kiến trúc, Xử lý lỗi & Hạ tầng mới

#### B-18: Ngoại lệ chưa bắt trong action sập HTTP 500 thay vì trả lỗi chuẩn 400
- **Triệu chứng**: Khi client gửi dữ liệu sai, hoặc database gặp lỗi cú pháp / khóa chính, API trả về mã phản hồi HTTP 500 Internal Server Error kèm trang lỗi HTML hoặc thông báo sập hệ thống (chứng cứ lỗi QA-qa1-4, QA-qa2-1, QA-qa2-2/3).
- **Nguyên nhân gốc**: Các action port bỏ quên khối `try/catch` hoặc dùng `throw;` để ném lại ngoại lệ (như tại commit `1752274` cho các hàm `Import`, `Delete`, `DeleteMultiple`, `Export` của `Mst_Department`). Trong khi đó, hệ thống nguồn bắt toàn bộ ngoại lệ tại Gate và trả về JSON `Success = false`.
- **Cách phòng tránh & Khắc phục**:
  1. Hạ tầng toàn cục: Kích hoạt `PortExceptionFilter` trong `Program.cs`, tự động bắt mọi ngoại lệ chưa xử lý của action port và định dạng thành HTTP 400 `PortResult.Fail("<Controller>_<Action>", "Exception", ex.Message)` (commit `02911b1`).
  2. Xử lý cục bộ: Tại các hàm ghi có transaction, bắt buộc phải có `catch (Exception ex) { await tx.RollbackAsync(); return PortResult.Fail(...); }`. **CẤM dùng `throw;`**.
- **Hạng mục liên quan**: Toàn bộ các controller port, đặc biệt `Mst_Department` (#127-#130).

#### B-19: Đăng ký `CreateFunction("lower")` lặp lại trong action/GetXAsync
- **Triệu chứng**: Agent viết code tự chèn lệnh `sqliteConn.CreateFunction("lower", ...)` ngay trong action controller hoặc trong phương thức `GetXAsync`. Khi nhiều request gọi đồng thời, sinh lỗi xung đột tài nguyên hoặc làm chậm hiệu năng.
- **Nguyên nhân gốc**: Agent cố gắng giải quyết bẫy tìm kiếm tiếng Việt HOA có dấu (B-03) một cách cục bộ trong từng file thay vì xử lý ở hạ tầng.
- **Cách phòng tránh & Khắc phục**:
  Tuân thủ nghiêm ngặt Bài học A.1 trong `LESSONS_AGENTS.md`: Hàm `lower()` Unicode đã được đăng ký MỘT LẦN duy nhất ở hạ tầng kết nối qua `SqliteUnicodeLowerInterceptor`. Cấm tuyệt đối việc tự ý đăng ký hàm trong action.
- **Hạng mục liên quan**: #152, #153 `Mst_FamilyStatusControllerPort.cs`.

#### B-20: Phân trang: `Ft_PageIndex` âm hoặc tràn int trả nhầm trang đầu
- **Triệu chứng**: Khi client gửi `Ft_PageIndex = -1` hoặc một số cực lớn (như `2147483647`), API trả về dữ liệu của trang đầu tiên thay vì trả về danh sách rỗng như nguồn (chứng cứ QA-qa1-2/3).
- **Nguyên nhân gốc**: Phép tính `recordStart = Ft_PageIndex * Ft_PageSize` khi `Ft_PageIndex` âm sẽ làm `recordStart` âm; hoặc khi `Ft_PageIndex` lớn sẽ làm tràn kiểu số nguyên `int` thành số âm. Lập trình viên thường dùng `Math.Max(0, recordStart)` để clamp về 0, vô tình biến yêu cầu lỗi thành truy vấn trang đầu tiên.
- **Cách phòng tránh & Khắc phục**:
  Áp dụng mẫu chuẩn chống tràn theo Bài học A.12 (đã vá 16 vị trí tại 22 file trong commit `02911b1`):
  ```csharp
  long recordStartLong = (long)req.Ft_PageIndex * req.Ft_PageSize;
  var page = (recordCount <= 0 || recordStartLong < 0 || recordStartLong >= totalCount)
      ? new List<T>()
      : await ordered.Skip((int)Math.Min(recordStartLong, int.MaxValue))
                     .Take((int)Math.Min(req.Ft_PageSize, int.MaxValue))
                     .ToListAsync();
  ```
- **Hạng mục liên quan**: Mọi action Search có phân trang của 22 controller port.

#### B-21: Trùng lặp số thứ tự `Order =>` trong các class `IPortSeeder`
- **Triệu chứng**: Khi chạy seeder khởi tạo dữ liệu ban đầu, thứ tự nạp dữ liệu bị xáo trộn; bảng con được seed trước bảng cha dẫn đến vi phạm ràng buộc dữ liệu hoặc xung đột logic.
- **Nguyên nhân gốc**: Agent tự chọn số `Order` ngẫu nhiên mà không kiểm tra các seeder đã có trước đó (ví dụ: seeder #153 `Mst_FamilyStatus` đặt `Order => 32` trùng với #122 `Mst_Department`).
- **Cách phòng tránh & Khắc phục**:
  Tuân thủ Bài học A.4: Trước khi khai báo seeder mới, bắt buộc chạy lệnh `grep -rn "Order =>" Models/Port/` để rà soát các số đã sử dụng. Chọn số duy nhất trong khoảng 10–99 cho danh mục hệ thống.
- **Hạng mục liên quan**: Toàn bộ các thực thể có seeder trong `Models/Port/`.

#### B-22: JSON serialize tự động thành camelCase thay vì PascalCase nguồn
- **Triệu chứng**: Client ứng dụng cũ hoặc các kịch bản kiểm thử mong đợi phản hồi dạng PascalCase (`{"Success": true, "DataList": [...]}`) nhưng server trả về camelCase (`{"success": true, "dataList": [...]}`), làm client không đọc được dữ liệu (chứng cứ QA-qa1-5).
- **Nguyên nhân gốc**: .NET 8 Web API mặc định sử dụng `JsonNamingPolicy.CamelCase` cho mọi controller.
- **Cách phòng tránh & Khắc phục**:
  Áp dụng `PortJsonResultFilter` trên toàn bộ controller thuộc namespace `MiniLoyalty.Controllers.Port` (commit `02911b1`), thiết lập `PropertyNamingPolicy = null` để giữ nguyên thuộc tính C#. Không đổi tên trường thủ công.
- **Hạng mục liên quan**: Toàn bộ 121 endpoint port đã hoàn thành.

#### B-23: Tài liệu tự bịa mã lỗi hoặc route không có thật trong mã nguồn
- **Triệu chứng**: Tài liệu API mô tả các mã lỗi chi tiết (ví dụ: `Mst_BirthPolicy_Save_InvalidEffDateStart`), nhưng khi kiểm thử thực tế thì hệ thống trả về mã hoàn toàn khác (`Mst_BirthPolicy_SaveX_InvalidEffDateStart`). Đối chiếu máy móc phát hiện 14 mã lỗi bịa đặt trong đợt tài liệu 3.
- **Nguyên nhân gốc**: Kỹ sư tài liệu suy diễn tên mã lỗi dựa trên quy ước đặt tên thông thường thay vì trích xuất nguyên văn từ mã nguồn đang chạy.
- **Cách phòng tránh & Khắc phục**:
  Tuân thủ nghiêm ngặt Bài học C.6: Mọi mã lỗi (`ErridnLoyalty.*`), route API, tên hàm, tên cột nêu trong tài liệu bắt buộc phải được copy nguyên văn bằng lệnh grep (ví dụ: `grep -o 'PortResult.Fail("[^"]*"'`). Cấm phỏng đoán tên.
- **Hạng mục liên quan**: Tài liệu API tại `docs/api/`, vai trò doc1 và doc2.

#### B-24: Trả nguyên văn `ex.Message` làm lộ thông tin hạ tầng DBMS nội bộ
- **Triệu chứng**: Khi gửi một payload gây lỗi SQL syntax hoặc kết nối, phản hồi lỗi chứa toàn bộ chuỗi thông báo lỗi của SQLite hoặc PostgreSQL (ví dụ: `Microsoft.Data.Sqlite.SqliteException: SQLite Error 1: 'no such table: Mst_XYZ'`), làm lộ tên bảng và đường dẫn máy chủ (chứng cứ QA-qa2-4).
- **Nguyên nhân gốc**: Hệ thống nguồn và bộ filter `PortExceptionFilter` trả trực tiếp `context.Exception.Message` vào trường `Params["Exception"]`.
- **Cách phòng tránh & Khắc phục**:
  Claude và nhóm dự án đã ra quyết định: chấp nhận rủi ro này trong giai đoạn chuyển đổi để đảm bảo tính khớp 1-1 với nguồn. Ghi nhận GAP kiến trúc; khi triển khai môi trường công khai, cần có middleware bọc che giấu chuỗi chi tiết này.
- **Hạng mục liên quan**: Toàn bộ hệ thống port 1-1, GAP HT-01.

#### B-25: Runner kẹt tiến trình CodeGPT 37 phút do nhầm mã trạng thái BUSY
- **Triệu chứng**: Pipeline điều phối bị kẹt bất động 37 phút ở một lượt chạy của CodeGPT, không phản hồi và không tự ngắt.
- **Nguyên nhân gốc**: Script bọc `cg-run.ps1` phân tích nhầm mã phản hồi `BUSY` của API thành cạn kiệt hạn ngạch `QUOTA`, dẫn đến vòng lặp chờ vô hạn.
- **Cách phòng tránh & Khắc phục**:
  Cập nhật script `cg-run.ps1` để tự động gửi yêu cầu hủy phiên chạy kẹt qua `/api/runs/{id}/cancel` khi quá thời gian chờ; trả mã thoát tiêu chuẩn 7 cho trạng thái BUSY để runner chuyển sang agent khác.
- **Hạng mục liên quan**: Hệ thống runner tự động, nhật ký cải tiến mục D.

---

## 3. Nhật ký cải tiến thực tế từ `LESSONS_AGENTS.md` (Mục D)

Dưới đây là bảng trích xuất toàn bộ các sự cố và bài học cải tiến đã được Claude phê duyệt và ghi nhận chính thức:

| Ngày | Phát hiện & Triệu chứng | Nguồn phát hiện | Hành động khắc phục |
|---|---|---|---|
| 2026-10-16 | `CreateFunction("lower")` lặp lại trong action (#152/#153) | Claude review | Ban hành Luật A.1; gỡ hàm đăng ký thủ công khỏi #153 |
| 2026-10-16 | `Import`/`Delete`/`DeleteMultiple` dùng `throw;` làm sập HTTP 500 | QA-qa2-1 | Sửa commit `1752274`; ban hành Luật A.2 bọc rollback |
| 2026-10-16 | Seeder `Order` trùng lặp giữa các bảng (trùng 32, 35) | Claude review | Ban hành Luật A.4; bắt buộc grep trước khi chọn số |
| 2026-10-16 | Run CodeGPT kẹt 37 phút, wrapper nhầm BUSY thành QUOTA | Giám sát hệ thống | Cập nhật `cg-run.ps1` tự hủy qua API cancel, mã thoát 7 = BUSY |
| 2026-10-16 | QA-qa1-1: Tìm tiếng Việt HOA có dấu trên SQLite trả 0 dòng | QA-qa1 | Bổ sung `SqliteUnicodeLowerInterceptor` hạ tầng (`02911b1`); cập nhật Luật A.1 |
| 2026-10-16 | QA-qa1-2/3: `PageIndex` tràn int hoặc âm trả nhầm dữ liệu trang đầu | QA-qa1 | Vá mẫu chuẩn tại 16 vị trí thuộc 22 file (`02911b1`); Luật A.12 |
| 2026-10-16 | QA-qa1-4: Action ĐỌC không bắt ngoại lệ làm sập HTTP 500 | QA-qa1 | Bổ sung `PortExceptionFilter` toàn cục (`02911b1`); Luật A.2 |
| 2026-10-16 | Tài liệu lượt 3 ghi 14 mã lỗi không có thật trong mã | Claude (đối chiếu máy) | Ban hành Luật C.6; giao doc1 sửa dứt điểm (doc1-fix3) |
| 2026-10-16 | QA-qa2-4: `ex.Message` trả nguyên văn lộ cấu trúc DBMS/bảng/đường dẫn | QA-qa2 | Quyết định Claude: chấp nhận khớp nguồn, ghi nhận GAP kiến trúc |
| 2026-10-16 | QA-qa2-2/3: Export + 18 endpoint ghi 500 khi DB lỗi (HEAD 1752274) | QA-qa2 | Đã có `PortExceptionFilter` (`02911b1`); giao qa2 xác minh lại trên HEAD mới |
| 2026-10-16 | QA-qa1-5: JSON trả về camelCase thay vì PascalCase như nguồn | QA-qa1 | Bổ sung `PortJsonResultFilter` chỉ cho namespace `Port` (`02911b1`); Luật A.11 |

---

## 4. Lịch sử cập nhật

| Phiên bản | Ngày cập nhật | Người thực hiện | Nội dung thay đổi |
|---|---|---|---|
| 1.0 | 2026-10-16 11:15 | doc2 (Quản lý quy trình) | Khởi tạo sổ tay bẫy kỹ thuật; phân loại 17 bẫy trọng tâm từ INDEX_CONVERT §5 và thực tiễn triển khai MiniLoyalty. |
| 2.0 | 2026-10-16 12:00 | doc2 (Quản lý quy trình) | Cập nhật đợt 2: bổ sung 8 bẫy kỹ thuật mới (B-18 đến B-25) từ LESSONS_AGENTS mục A và D; cập nhật bẫy B-03 (đã giải quyết triệt để bằng SqliteUnicodeLowerInterceptor); bổ sung Bảng đối chiếu sự cố mục D; nâng tổng số bẫy phân tích lên 25 bẫy. |
