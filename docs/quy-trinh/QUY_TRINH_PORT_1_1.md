# QUY TRÌNH CHUẨN PORT 1-1 TỪ `2022.7.Loyalty` SANG MiniLoyalty

> **Phạm vi**: Toàn bộ chu trình port 1-1 các hạng mục Controller/Action từ giải pháp nguồn `2022.7.Loyalty` sang ứng dụng đích `MiniLoyalty`.  
> **Cập nhật**: 2026-10-16 13:35  
> **Nguồn dữ liệu**: `PORT_RULES_MiniLoyalty.md` (gồm mục 6), `repos\labs-audit\SOP_CONVERT_1TO1.md`, `repos\labs-audit\INDEX_CONVERT.md` (§5 bẫy), `docs\LESSONS_AGENTS.md`, nhật ký thực tế tại `.parity-run\logs\`.  
> **Trạng thái**: Nháp — chờ review  

---

## 1. Mục đích và nguyên tắc cốt lõi

Tài liệu này chuẩn hóa toàn bộ các bước thực thi việc chuyển đổi (port) 1-1 từng endpoint, action nghiệp vụ từ hệ thống cũ (`2022.7.Loyalty`) sang kiến trúc hiện đại của `MiniLoyalty` (.NET 8, EF Core, SQLite/PostgreSQL).

### Nguyên tắc bất biến
1. **Một hạng mục mỗi lượt**: Mỗi phiên làm việc chỉ xử lý duy nhất 1 hạng mục. Cấm làm đồng thời nhiều hạng mục trong một commit code.
2. **Cô lập theo file riêng**: Mỗi bảng/controller port phải nằm trên một file riêng biệt (`Models/Port/<Bảng>.cs` và `Controllers/Port/<TênController>Port.cs`). Không sửa file dùng chung (`AppDbContext.cs`, `Program.cs`) khi không được giao rõ, tránh xung đột giữa các agent chạy song song.
3. **Đọc trọn vẹn chuỗi gọi LIVE**: Bắt buộc lần dấu vết đủ 4 tầng: `ClientGate` → `WebAPI Service` → `Biz API Controller` → `Biz LIVE Method` → `SQL thật`. Cấm dừng ở tầng Gate hoặc đọc nhầm mã chết (`Delete.*`, `*.sync-conflict-*.cs`, `*.bak`).
4. **Kiểm chứng bằng chạy thật (Runtime Verification)**: Build xanh (`0 Error(s)`) chỉ là điều kiện cần. Bắt buộc phải chạy tiến trình app thật, kiểm tra round-trip ghi dữ liệu, kiểm tra các ca chặn lỗi (guard), và đối chiếu bộ lọc truy vấn.
5. **Tuyệt đối an toàn thông tin**: Nghiêm cấm sao chép tài khoản, mật khẩu, token, chuỗi kết nối hardcode từ nguồn vào mã nguồn hoặc tài liệu dưới bất kỳ hình thức nào.
6. **Chỉ làm theo tài liệu — Xung đột thì dừng**: Tuyệt đối không suy diễn ngoài tài liệu, không tự ý giải quyết mâu thuẫn.

---

## 2. Sơ đồ luồng thực thi (Workflow)

```mermaid
flowchart TD
    A["1. Chọn hạng mục<br/>(Duyệt PARITY_PROGRESS.md)"] --> B["2. Trích nguồn & Lần chuỗi<br/>(Gate -> API -> Biz LIVE -> SQL)"]
    B --> C{"Kiểm tra khả thi & Xung đột?"}
    C -- "Phụ thuộc dịch vụ ngoài" --> C1["Gán nhãn CAN_XEM_TAY<br/>Ghi skipped.json"]
    C -- "Thiếu bảng nền tảng" --> C2["Gán nhãn HOAN_PHU_THUOC<br/>Ghi skipped.json"]
    C -- "Mã rác / scaffold" --> C3["Gán nhãn BO<br/>Ghi vào sổ"]
    C -- "Mâu thuẫn tài liệu / thiếu thông tin" --> C4["Ghi .parity-run/conflicts/<br/>Gán nhãn CAN_XEM_TAY"]
    C -- "Đủ điều kiện port" --> D["3. Soạn đề bài DEBAI.md<br/>(Hợp đồng, schema, kịch bản test)"]
    D --> E["4. Agent viết code<br/>(Grep -> Entity -> Controller -> DTO -> Seeder)"]
    E --> F["5. Build & Test<br/>(dotnet build, dotnet test)"]
    F -- "Lỗi biên dịch" --> E
    F -- "Build xanh" --> G["6. Chạy thật & Xác minh<br/>(Start-Process PORT, test round-trip, guard)"]
    G -- "Lỗi runtime / sai logic" --> E
    G -- "Đạt toàn bộ tiêu chí" --> H["7. Review & Chốt<br/>(Kiểm tra bảng K/T/S/Đ, commit feat + docs)"]
    H --> I["8. QA & Tài liệu độc lập<br/>(Test hồi quy, cập nhật SO_GAP, TRUY_VET)"]
```

---

## 3. Phân định vai trò & trách nhiệm

Quy trình port 1-1 phân định 6 vai trò chuyên biệt nhằm đảm bảo tính khách quan và kiểm soát chất lượng nhiều lớp:

| Vai trò | Thành phần đảm nhiệm | Trách nhiệm chính |
|---|---|---|
| **Điều phối (Orchestrator)** | Claude / Quản trị viên | Chọn hạng mục tiếp theo từ sổ tiến độ; kiểm tra quan hệ phụ thuộc; phân bổ luồng cho các agent viết code; giám sát tiến độ toàn cục. |
| **Kỹ sư trưởng (Lead)** | Agent Lead (`lead.ps1`, Claude Sonnet 5.5 qua `agy`) | Định hướng kiến trúc; phân tích chuỗi gọi phức tạp; soạn thảo đề bài `DEBAI.md` (chuẩn hóa schema, route, con số kỳ vọng, ca test); xử lý vướng mắc kỹ thuật và đề xuất tối ưu. |
| **Agent viết mã (Worker)** | `agy` (Gemini Flash), `CodeGPT` (DeepSeek) | Đọc hiểu trọn vẹn chuỗi nghiệp vụ; kiểm tra trùng lặp (`grep`); sinh mã entity, controller port, seeder; biên dịch `dotnet build`; chạy kiểm thử runtime cục bộ; xuất nhật ký `write_r*.txt`. |
| **Người duyệt (Reviewer)** | Claude Opus 5.5 | Soát xét độc lập toàn bộ diff; kiểm tra tính trung thực của bảng đối chiếu (K/T/S/Đ); rà soát an toàn dữ liệu; phê duyệt đạt chuẩn (`Review: PASS`) trước khi tạo commit. |
| **Đảm bảo chất lượng (QA)** | Agent QA (`qa1`, `qa2`) | Kiểm thử độc lập hộp đen / hộp xám trên môi trường runtime; dò tìm lỗi hồi quy, bẫy chữ hoa/thường, phân trang âm/tràn int, và rò rỉ dữ liệu `ex.Message`; đề xuất vá hạ tầng dùng chung. |
| **Tài liệu & Quy trình (Docs)** | Agent Docs (`doc1`, `doc2` / Antigravity) | `doc1`: Soạn thảo và cập nhật tài liệu API chi tiết theo controller (`docs/api/`); đối chiếu nguyên văn mã lỗi và route với mã nguồn.<br/>`doc2`: Quản lý quy trình, sổ đăng ký GAP (`SO_GAP.md`), ma trận truy vết (`TRUY_VET.md`), nhật ký bẫy kỹ thuật (`NHAT_KY_BAY.md`), báo cáo tiến độ độc lập (`BAO_CAO_TIEN_DO.md`) và kiến trúc hạ tầng chung. |

---

## 4. Bảy giai đoạn thực hiện chi tiết

### Giai đoạn 1: Lựa chọn hạng mục & Lập kế hoạch
1. Đọc sổ tiến độ `PARITY_PROGRESS.md`, tìm các hạng mục đang ở trạng thái `Chưa làm`.
2. Ưu tiên theo nhóm nghiệp vụ logic: hoàn thành dứt điểm các controller danh mục cơ sở (`Mst_*`) trước khi chuyển sang bảng trung gian (`Map_*`), giao dịch thẻ (`Crd_*`), khuyến mại (`Prm_*`), hoặc báo cáo (`Rpt_*`).
3. Kiểm tra các điều kiện tiên quyết: bảng phụ thuộc đã có entity và migration chưa. Nếu chưa có bảng cha, chuyển sang tiêu chí tạm hoãn (`HOAN_PHU_THUOC`).

### Giai đoạn 2: Trích nguồn & Lần chuỗi gọi
1. Bắt đầu từ file Gate: `D:\idocNet\2022.7.Loyalty\13.ClientGate\V10\idn.Loyalty.WebAPI\Controllers\<Tên>Controller.cs`.
2. Xác định phương thức API backend tương ứng: qua lời gọi `WA_<Tên>_<Action>`.
3. Lần tới file Controller Backend: `11.BackEnd\V10\idn.Loyalty.Biz.Web\Controllers\<Tên>Controller.cs`.
4. Tìm hàm nghiệp vụ xử lý tại `idn.Loyalty.Biz\*.cs`:
   - Phải xác định đúng **bản LIVE** đang được hàm controller gọi thực tế (ví dụ: `_New2025...`, `_New2026...`).
   - Tuyệt đối loại bỏ các bản chết: file `Delete.*`, file xung đột đồng bộ `*.sync-conflict-*.cs`, file sao lưu `*.bak`.
5. Đọc SQL thực thi: kiểm tra danh sách bảng nguồn, các cột được chọn hoặc cập nhật, điều kiện lọc `WHERE`, các phép kiểm tra logic (`throw`, mã lỗi nghiệp vụ).

### Giai đoạn 3: Soạn đề bài chi tiết (`DEBAI.md`)
Đề bài giao cho agent viết code do Claude hoặc Agent Lead soạn thảo, bắt buộc gồm 5 phần cấu trúc:
1. **Đường dẫn tuyệt đối**: Vị trí file nguồn (Gate, API, Biz, DDL) và file đích được phép ghi trong repo.
2. **Đặc tả hợp đồng API**: Route URL (dạng `/api/<tên-viết-thường>/<action>`), phương thức HTTP (chuẩn POST), cấu trúc Request Body / DTO.
3. **Mô hình thực thể & Cơ sở dữ liệu**: Tên bảng nguồn, danh sách cột, kiểu dữ liệu, khóa chính (`HasKey`), quan hệ khóa ngoại (nếu có).
4. **Danh sách Guard & Validation**: Điều kiện kiểm tra dữ liệu đầu vào, mã lỗi trả về (`Success = false`, `ErrorCode`).
5. **Kịch bản kiểm thử Runtime**: Tối thiểu 1 ca round-trip ghi-đọc thành công và 1 ca vi phạm guard trả lỗi đúng chuẩn.

### Giai đoạn 4: Viết mã nguồn (DoD bước 1 - 5)
1. **Bước 1 - Grep trước**: Quét toàn bộ repo đích xem endpoint/bảng đã tồn tại chưa (`grep -rn "<Tên>"`). Nếu đã có, không sửa mã, báo cáo `DA_CO`.
2. **Bước 2 - Mô hình thực thể (`Models/Port/<Bảng>.cs`)**:
   - Khai báo lớp entity trong `namespace MiniLoyalty.Models.Port`.
   - Cấu hình Fluent API qua `IEntityTypeConfiguration<T>` trong cùng file (`ToTable("<TênBảng>")`, `HasKey(...)`).
   - Không khai báo `DbSet` trong `AppDbContext.cs`; sử dụng `db.Set<T>()`.
   - Tạo class Seeder `<TênBảng>Seeder : IPortSeeder` sinh dữ liệu mẫu tối thiểu (idempotent); số `Order =>` từ 10–99 cho danh mục, grep kiểm tra trước để tránh trùng.
3. **Bước 3 - Controller Port (`Controllers/Port/<Tên>Port.cs`)**:
   - Đặt attribute `[ApiController]`, route `/api/<tên-controller-thường>/<action>`.
   - Action nhận 1 body DTO duy nhất (Minimal API pattern). Với action nhận body rỗng hoặc nullable, dùng `[FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)]`.
   - Namespace bắt buộc là `MiniLoyalty.Controllers.Port` để hưởng hạ tầng JSON PascalCase tự động.
4. **Bước 4 - Chuẩn hóa dữ liệu nguồn**:
   - Áp dụng đúng hàm: `StdParam(x)` (Trim + ToUpper), `StdStr(x)` (Trim + Normalize), `StdFlag(x)` ("1" nếu "1", ngược lại "0").
   - Kiểu số: dùng `double?` cho các trường tính toán hoặc tiền tệ nguồn (`MyValue`), tránh lỗi `SUM()` trên kiểu TEXT của SQLite.
   - So khớp chuỗi: dùng `EF.Functions.Like(cột.ToLower(), pattern, "\\")` với pattern đã xử lý `ToLower()` và escape ký tự đặc biệt, đảm bảo tương thích không phân biệt hoa/thường.
5. **Bước 5 - Lập bảng đối chiếu K/T/S/Đ**:
   - `K`: Khớp hoàn toàn với nguồn.
   - `T`: Thiếu (nêu rõ lý do thiếu cột/bảng liên quan).
   - `S`: Khác nguồn (đã khai báo lý do điều chỉnh kỹ thuật).
   - `Đ`: Đơn giản hóa có chủ đích (ví dụ: lược bỏ ghi log ngoài hoặc bypass phân quyền chưa port).

### Giai đoạn 5: Build, Test & Kiểm thử thực tế Runtime (DoD bước 6)
1. **Biên dịch**: `dotnet build -c Release` đạt `0 Error(s)`.
2. **Kiểm thử đơn vị**: `dotnet test MiniLoyalty.Tests -c Release` toàn bộ pass.
3. **Chạy ứng dụng thật**:
   - Thiết lập cổng chạy riêng biệt qua biến `$env:PORT` (vì app cấu hình `UseUrls` ghi đè `ASPNETCORE_URLS`).
   - Tạo database SQLite tạm thời tại thư mục Temp.
   - Khởi động ứng dụng độc lập qua PowerShell:
     ```powershell
     $p = Start-Process dotnet 'bin\Release\net8.0\MiniLoyalty.dll' -PassThru -WindowStyle Hidden
     ```
   - Chờ ứng dụng sẵn sàng bằng vòng lặp kiểm tra endpoint `/healthz` (tối đa 30 lần, mỗi lần nghỉ 1 giây). Tuyệt đối không đọc `StandardOutput.ReadLine()` tránh treo vô hạn do đệm luồng stdout (Bẫy #44).
4. **Thực thi các ca kiểm thử**:
   - Với action GHI: Gửi payload hợp lệ → kiểm tra kết quả trả về `Success = true` → truy vấn lại dữ liệu đảm bảo khớp giá trị (round-trip); gửi payload vi phạm guard → kiểm tra hệ thống trả lỗi đúng mã.
   - Với action ĐỌC: Gửi tối thiểu 2 bộ lọc khác nhau → xác nhận số lượng bản ghi trả về phân hóa đúng logic nghiệp vụ.
5. **Dọn dẹp tài nguyên**: Tắt tiến trình ứng dụng (`Stop-Process -Id $p.Id -Force`), đóng cổng mạng, và xóa tệp database tạm.

### Giai đoạn 6: Review & Đánh giá an toàn
1. Reviewer độc lập (Claude) kiểm tra diff git (`git diff`).
2. Xác minh không có thông tin đăng nhập, token hệ thống bị lọt vào code hoặc comment.
3. Đánh giá tính xác thực của kết quả kiểm thử runtime trong file log `write_r*.txt`.
4. Nếu phát hiện sai sót, yêu cầu agent viết lại round tiếp theo (`write_r1`, `write_r2`). Khi đạt toàn bộ tiêu chí, cấp cờ `Review: PASS`.

### Giai đoạn 7: Chốt kết quả & Ghi nhận sổ tiến độ
1. Tạo commit mã nguồn: `feat(parity): port 1-1 <Controller>/<Action> ...` kèm thông tin định danh engine thực hiện.
2. Tạo commit tài liệu: `docs(parity): <Controller>/<Action> -> Đã làm (X/337 (P%))`.
3. Cập nhật dòng tương ứng trong `PARITY_PROGRESS.md` từ `Chưa làm` sang `Đã làm` kèm hash commit.
4. Lưu trữ toàn bộ nhật ký phiên chạy tại `.parity-run\logs\<số>_<hạng-mục>\`.

---

## 5. Quy tắc cốt lõi: Chỉ làm theo tài liệu — Xung đột thì dừng

Quy định này áp dụng nghiêm ngặt cho **MỌI agent** (Worker, Lead, QA, Docs) theo mục 6 của `PORT_RULES_MiniLoyalty.md`:

### 5.1. Thứ tự ưu tiên nguồn sự thật (Hierarchy of Truth)
Khi có sự khác biệt giữa các nguồn thông tin, mức độ ưu tiên tuân thủ theo thứ tự sau:
1. **Mã nguồn gốc**: `source_excerpt.txt` và mã nguồn trong `D:\idocNet\2022.7.Loyalty`.
2. **Đề bài**: `DEBAI.md` của hạng mục cụ thể.
3. **Luật chung port**: `PORT_RULES_MiniLoyalty.md`.
4. **Bài học tích lũy**: `docs\LESSONS_AGENTS.md`.

Agent chỉ hành động trong phạm vi các tài liệu trên, tuyệt đối không tự bổ sung hành vi ngoài đặc tả.

### 5.2. Định nghĩa xung đột kỹ thuật (Conflict)
Xung đột xảy ra trong các tình huống sau:
- Hai nguồn trong danh sách trên mâu thuẫn trực tiếp với nhau (ví dụ: đề bài nêu số kỳ vọng mà mã nguồn cho kết quả khác; luật chung cấm điều đề bài yêu cầu).
- Đề bài thiếu thông tin quan trọng buộc agent phải phỏng đoán.
- Cách duy nhất để vượt qua kiểm thử là vi phạm một luật hoặc bài học đã ban hành.

### 5.3. Quy trình xử lý xung đột
Khi gặp xung đột: **KHÔNG tự chọn, KHÔNG đoán, KHÔNG chế cách vượt.** Agent bắt buộc thực hiện:
1. Ghi nhận chi tiết vào tệp: `.parity-run\conflicts\<số>_conflict.md` (tạo thư mục nếu chưa có) gồm:
   - Tên hạng mục.
   - Các nguồn mâu thuẫn kèm `file:dòng` cụ thể.
   - Các bước/thử nghiệm đã thực hiện.
   - Đề xuất kỹ thuật của agent (chỉ đóng vai trò đề xuất tham khảo).
2. Hoàn tất các phần việc KHÔNG bị ảnh hưởng bởi xung đột để đảm bảo `dotnet build -c Release` vẫn đạt `0 Error(s)`.
3. Kết thúc mục `TOM TAT` với:
   - `TRANG_THAI: CAN_XEM_TAY`
   - `CONFLICT: <mô tả ngắn gọn + đường dẫn file xung đột>`
4. Claude sẽ đọc các xung đột đã gom khi được đánh thức và ra quyết định chính thức một lần. Agent không chờ và không lặp lại việc để "thử cho qua".
5. Agent phụ (Docs/QA/Lead) KHÔNG tự sửa luật, script hay prompt: chỉ đề xuất vào `.parity-run\proposals\<tên>.md` để Claude duyệt.

---

## 6. Hạ tầng dùng chung cốt lõi (Shared Infrastructure)

Để tránh tình trạng mỗi agent tự viết logic xử lý cục bộ gây phân mảnh hoặc xung đột, hệ thống chuẩn hóa hai thành phần hạ tầng dùng chung tại commit `02911b1`:

### 6.1. `Data/PortFilters.cs` — Chuẩn hóa JSON & Bắt ngoại lệ tập trung

- **PascalCase JSON tự động (`PortJsonResultFilter`)**:
  - Gate nguồn sử dụng `JavaScriptSerializer` / Newtonsoft.Json mặc định PascalCase (`Success`, `Data`, `DataList`, `ItemCount`, `ErrorCode`, `Params`).
  - .NET 8 mặc định camelCase. Bộ filter `PortJsonResultFilter` can thiệp vào `OnResultExecuting`, chỉ áp dụng `PropertyNamingPolicy = null` cho controller thuộc namespace `MiniLoyalty.Controllers.Port`.
  - Các endpoint cũ của `MiniLoyalty` (`/api/v1/*`, `/api/earn`, v.v.) và SPA không bị ảnh hưởng.
  - Vị trí mã nguồn: [`Data/PortFilters.cs:19-51`](../../Data/PortFilters.cs#L19-L51).

- **Chuyển ngoại lệ thành HTTP 400 (`PortExceptionFilter`)**:
  - Nguồn bắt mọi ngoại lệ tại Gate và trả về `Success = false` (không bao giờ trả HTTP 500 ra client).
  - Filter `PortExceptionFilter` bắt mọi ngoại lệ chưa xử lý trong controller port, tự động định dạng thành HTTP 400 `PortResult.Fail("<Controller>_<Action>", "Exception", ex.Message)`.
  - Vị trí mã nguồn: [`Data/PortFilters.cs:54-67`](../../Data/PortFilters.cs#L54-L67).
  - **Quy tắc bắt buộc đối với Transaction**: Filter chỉ là chốt chặn cuối cùng cho các action đọc hoặc lỗi không lường trước. Tại các hàm ghi có transaction, lập trình viên bắt buộc phải `catch (Exception ex) { await tx.RollbackAsync(); return PortResult.Fail(...); }` cục bộ để đảm bảo rollback cơ sở dữ liệu có kiểm soát; **tuyệt đối không dùng `throw;`**.

### 6.2. `Data/SqliteUnicodeLower.cs` — Hỗ trợ Unicode cho SQLite

- **Đăng ký hàm `lower()` Unicode một lần ở cấp kết nối**:
  - Lệnh `LIKE` và hàm `lower()` mặc định của SQLite chỉ xử lý các ký tự ASCII tiêu chuẩn. Ký tự tiếng Việt có dấu (như `Đ`, `Ơ`, `Ư`, `Ă`) không được hạ chữ, dẫn đến các truy vấn tìm kiếm chữ hoa như `ĐÀ NẴNG` trên SQLite trả về 0 dòng kết quả (chứng cứ lỗi QA-qa1-1).
  - Lớp `SqliteUnicodeLowerInterceptor` kế thừa `DbConnectionInterceptor`, tự động đăng ký delegate `value?.ToLowerInvariant()` cho hàm `lower` mỗi khi một `SqliteConnection` được mở.
  - Vị trí mã nguồn: [`Data/SqliteUnicodeLower.cs:14-30`](../../Data/SqliteUnicodeLower.cs#L14-L30).
  - **Quy tắc bắt buộc**: **CẤM** đăng ký `CreateFunction("lower", ...)` lặp lại trong action hoặc trong các phương thức `GetXAsync` (đã vi phạm tại #152, #153). Mọi truy vấn chỉ cần viết `EF.Functions.Like(cột.ToLower(), pattern, "\\")` là hoạt động chính xác trên cả SQLite và PostgreSQL.

---

## 7. Tiêu chí hoàn thành (Definition of Done - DoD)

Một hạng mục chỉ được công nhận là `Đã làm` khi thỏa mãn đầy đủ 7 tiêu chí:
- [x] **DoD 1 - Không trùng lặp**: Đã grep xác nhận chưa từng được port trước đó trong repo đích.
- [x] **DoD 2 - Đúng nguồn sống**: Lần đúng chuỗi gọi tới hàm Biz LIVE đang chạy, không tham chiếu mã chết.
- [x] **DoD 3 - Đầy đủ cấu trúc**: Entity + Fluent API + Controller Port + DTO + Seeder được tạo đúng quy ước thư mục riêng biệt.
- [x] **DoD 4 - Chuẩn hóa chuẩn mực**: Áp dụng đúng các quy ước kỹ thuật (`StdParam`/`StdStr`/`StdFlag`, `double?`, `EF.Functions.Like`, Minimal API 1 DTO, `Order` Seeder không trùng).
- [x] **DoD 5 - Bảng đối chiếu minh bạch**: Có bảng phân loại K/T/S/Đ chi tiết cho từng tham số đầu vào, cột dữ liệu và guard nghiệp vụ.
- [x] **DoD 6 - Chạy thật đạt 100%**: Ứng dụng đã chạy thực tế trên cổng mạng riêng; có bằng chứng lệnh gọi HTTP curl/Invoke-RestMethod; đạt round-trip thành công và bắt lỗi guard đúng chuẩn; đã dọn dẹp tiến trình và DB tạm.
- [x] **DoD 7 - Review độc lập chấp thuận**: Được reviewer kiểm duyệt diff, xác nhận không lộ credentials, và phê duyệt commit.

---

## 8. Tiêu chí phân loại bỏ qua & tạm hoãn

Khi một hạng mục không thể hoàn tất theo quy trình thông thường, điều phối viên phân loại vào 1 trong 3 trạng thái sau và lưu vết tại `.parity-run\skipped.json`:

```mermaid
flowchart TD
    Issue["Phát hiện rào cản khi phân tích nguồn"] --> Check1{"Phụ thuộc hệ thống / dịch vụ ngoài?"}
    Check1 -- "Có" --> Action1["Gán CAN_XEM_TAY<br/>Lưu lý do vào skipped.json<br/>Chờ phê duyệt kiến trúc stub/mock"]
    Check1 -- "Không" --> Check2{"Thiếu entity / bảng DB nền tảng?"}
    Check2 -- "Có" --> Action2["Gán HOAN_PHU_THUOC<br/>Lưu thứ tự ưu tiên vào skipped.json<br/>Chờ port xong bảng gốc"]
    Check2 -- "Không" --> Check3{"Là scaffold / backup / mã trùng lặp?"}
    Check3 -- "Có" --> Action3["Gán nhãn BO<br/>Cập nhật PARITY_PROGRESS.md<br/>Nêu rõ lý do loại trừ"]
    Check3 -- "Không" --> Check4{"Mâu thuẫn tài liệu / thiếu thông tin?"}
    Check4 -- "Có" --> Action5["Gán CAN_XEM_TAY<br/>Tạo file .parity-run/conflicts/<br/>Chờ Claude duyệt"]
    Check4 -- "Không" --> Action6["Tiếp tục quy trình port bình thường"]
```

### 1. `CAN_XEM_TAY` (Cần con người quyết định kiến trúc)
Áp dụng khi logic nguồn phụ thuộc vào dịch vụ bên thứ ba, hệ thống ngoại vi, hoặc phát sinh xung đột tài liệu:
- Gọi API Master Server ngoại vi (ví dụ: `WA_OS_Inos_Org_GetMyOrgList` của iNOS).
- Gọi Web Service ghi dữ liệu sang hệ thống DMS khác (ví dụ: `WSDMS CarSv_Ser_CustomerCar_Get` ghi dữ liệu xe sang DMS Hyundai làm ảnh hưởng giao dịch).
- Phát hiện xung đột tài liệu theo mục 5 của quy trình này.

### 2. `HOAN_PHU_THUOC` (Hoãn do phụ thuộc thứ tự)
Áp dụng khi endpoint yêu cầu ghi hoặc kiểm tra khóa ngoại trên các bảng lớn phức tạp chưa được chuyển đổi:
- Yêu cầu bảng `Mst_NNT` với hơn 60 cột thông tin doanh nghiệp mạng lưới.
- Yêu cầu hệ thống phân quyền chi tiết người dùng `Sys_User` (hàm kiểm tra `Sys_User_GetAbilityViewOfUser`).
- Xử lý: Hoãn lại và quay lại thực hiện sau khi các bảng nền tảng đã được port và kiểm thử hoàn chỉnh.

### 3. `BO` (Loại bỏ khỏi phạm vi chuyển đổi)
Áp dụng cho các thành phần không mang giá trị nghiệp vụ vận hành hoặc không thể port 1-1 do thiếu nguồn backend:
- Các endpoint scaffold sinh tự động của framework (`HelpPage`, `/Shared/`).
- Các action sao lưu cũ của lập trình viên còn sót lại trong mã nguồn (ví dụ: `Mst_OrgController/GetAllOrg_bk`, #180).
- **Thiếu nguồn backend tại tầng Biz (Bài học B.7)**: Các action chỉ tồn tại ở tầng ClientGate gọi qua ClientService sang dịch vụ ngoài (như `MstSv`) mà trong toàn bộ `idn.Loyalty.Biz` và `idn.Loyalty.Biz.Web` không có hàm `WA_` hoặc hàm biz tương ứng, không có DDL và không có mã lỗi (đã áp dụng cho #159–#161 `Mst_GovIDTypeController/Create, Update, Delete`).
- Xử lý: Đánh dấu trạng thái `Bỏ` trong sổ tiến độ hoặc `skipped.json` kèm lý do minh bạch, không tính vào khối lượng công việc tồn đọng.

---

## 9. Các điều cấm tuyệt đối (Zero Tolerance)

1. **Cấm rò rỉ thông tin bảo mật**: Tuyệt đối không chép tài khoản, mật khẩu, private key, token, connection string vào code, comment hay tài liệu. Vi phạm sẽ bị hủy bỏ toàn bộ kết quả lượt làm việc.
2. **Cấm tự ý thao tác Git**: Agent viết code không được tự ý chạy các lệnh `git commit`, `git push`, `git checkout`, `git switch`, `git reset`, `git clean` hoặc chuyển đổi nhánh.
3. **Cấm can thiệp sổ tiến độ**: Không tự ý sửa đổi file `PARITY_PROGRESS.md`. Quyền cập nhật sổ tiến độ thuộc về bước điều phối sau khi đã có commit hợp lệ.
4. **Cấm sửa ngoài phạm vi cho phép**: Không ghi đè các file dùng chung (`AppDbContext.cs`, `Program.cs`) trừ khi có chỉ thị rõ ràng. Không ghi bất kỳ dữ liệu nào vào thư mục nguồn `D:\idocNet`.
5. **Cấm báo cáo khống (Over-claim)**: Chưa chạy kiểm thử runtime thực tế thì chỉ được ghi nhận `verify PENDING`, không được tự nhận là hoàn thành.
6. **Cấm đăng ký SQLite function trong action**: Không gọi `sqlite.CreateFunction("lower", ...)` trong action port; chức năng này đã được đảm nhiệm tập trung bởi `SqliteUnicodeLowerInterceptor`.
7. **Cấm tự bịa mã lỗi hoặc route (Bài học C.6)**: Mọi mã lỗi (`ErridnLoyalty.*`), route API, tên hàm, tên cột nêu trong tài liệu bắt buộc phải được copy nguyên văn bằng lệnh grep từ mã nguồn. Cấm suy diễn tên mã lỗi.
8. **Thiếu nguồn backend = không port (Bài học B.7)**: Nếu action ở gate chỉ gọi dịch vụ ngoài mà backend không có hàm tương ứng thì không tự đoán mã lỗi hay quy tắc; bắt buộc lập hồ sơ xung đột `conflicts/<số>_conflict.md` và dừng lại với `CAN_XEM_TAY`.

---

## 10. Lịch sử cập nhật

| Phiên bản | Ngày cập nhật | Người thực hiện | Nội dung thay đổi |
|---|---|---|---|
| 1.0 | 2026-10-16 11:15 | doc2 (Quản lý quy trình) | Khởi tạo tài liệu quy trình chuẩn port 1-1 cho MiniLoyalty; tích hợp các bài học từ SOP_CONVERT_1TO1 và INDEX_CONVERT §5. |
| 2.0 | 2026-10-16 12:00 | doc2 (Quản lý quy trình) | Bổ sung phân định vai trò chi tiết (Lead, QA, Docs, Orchestrator, Reviewer, Worker); bổ sung Mục 5 Quy tắc cốt lõi 'Chỉ làm theo tài liệu — Xung đột thì dừng' từ PORT_RULES mục 6; bổ sung Mục 6 mô tả hạ tầng dùng chung (PortFilters.cs, SqliteUnicodeLower.cs). |
| 3.0 | 2026-10-16 13:35 | doc2 (Quản lý quy trình) | Cập nhật đợt 3: bổ sung tiêu chí BỎ do thiếu nguồn backend tại tầng Biz (Bài học B.7); bổ sung điều cấm số 7 (cấm bịa mã lỗi theo C.6) và số 8 (thiếu backend thì dừng); đồng bộ với các quyết định phê duyệt mới nhất của Claude. |
