# LUẬT PORT 1-1 — `2020.6.QContract` → MiniContract (dùng cho agent viết code)

> Cùng khung với `PORT_RULES_MiniContract.md` (đọc kèm `LESSONS_AGENTS.md`) — chỉ khác đường dẫn nguồn, phong bì và tiền tố mã lỗi.
> Mọi tài liệu nằm trong `C:\Users\Public\Pictures\kientran_private`. Đối chiếu cũ: `repos\labs-audit\AGENT_MiniContract.md`.

## 1. Đường dẫn

| Vai trò | Đường dẫn |
|---|---|
| Repo đích (được sửa) | `C:\Users\Public\Pictures\kientran_private\repos\MiniContract`, nhánh `parity/auto` |
| Nguồn tầng cổng (chỉ đọc) | `D:\idocNet\2020.6.QContract\Dev\V10\idn.Skycic.QContract.Website\Controllers\<X>Controller.cs` |
| Nguồn tầng API (chỉ đọc) | `...\Dev\V10\idn.Skycic.QContract.Biz.Web\Controllers\<X bỏ dấu _>Controller.cs` (action `WA_<X>_<Y>`, kiểu `ServiceResult<RT_...>`) |
| Nguồn nghiệp vụ + SQL (chỉ đọc) | `...\Dev\V10\idn.Skycic.QContract.Biz\*.cs` |
| Model / hằng số (chỉ đọc) | `...\Dev\V10\idn.Skycic.QContract.Common`, `idn.Skycic.QContract.Constants` (mã lỗi `TError.ErridQContract.X` = `"Erridn.Skycic.QContract.X"`) |
| KHÔNG ĐỌC | `Biz.Web.DEV`, `*.sync-conflict-*`, `Delete.*`, `*.bak`, `05.Refs`, `ToolGenNetWork`, `ESign` (thư viện ký số — chỉ tham khảo) |
| Sổ tiến độ | `PARITY_PROGRESS.md` trong repo đích — **agent KHÔNG được sửa** |

Chuỗi gọi của 1 hạng mục `<X>Controller/<Action>`: cổng `Website\Controllers\<X>Controller.<Action>` → `ClientService` (`WA_<X>_<Y>`) →
API `Biz.Web\Controllers\<X>Controller` → hàm biz trong `idn.Skycic.QContract.Biz` → SQL. **Phải đọc TRỌN cả chuỗi.**
Lệnh gọi **dịch vụ ngoài** (ký số ESign, e-mail, SMS, iNOS, VNPay, `RestClient`…) mà đích không có ⇒ KHÔNG bịa: ghi `GAP` + `TRANG_THAI: CAN_XEM_TAY`
(hoặc `BO` nếu hạng mục CHỈ là chuyển tiếp). Mã lỗi trả bằng `PortResult.Fail("<Mã>", ...)` (tiền tố `Erridn.Skycic.QContract.` thêm tự động).

## 2. Definition of Done cho agent viết code (bước 1 → 6; bước commit do điều phối làm)
1. **Grep trước** trong repo đích (`Controllers/`, `Models/`, `Data/`, `Program.cs`, `wwwroot/`): hạng mục đã port chưa?
   Đã có đủ ⇒ không sửa gì, báo `DA_CO` kèm `file:dòng` bằng chứng.
2. **Chọn đúng bản LIVE** của hàm biz: lần theo lời gọi thật từ controller API. Có nhiều bản (`_New2025…`, `_New2026…`)
   thì bản được **gọi** mới là bản sống. **CẤM** đọc `*.sync-conflict-*.cs`, `Delete.*`, `*.bak` — đó là mã chết.
3. **Đọc trọn** action + hàm biz + SQL: tham số, điều kiện lọc, guard (`throw`/mã lỗi), cột trả về, transaction, side-effect.
4. **Bảng đối chiếu** từng tham số/cột/guard: `K` khớp · `T` thiếu (lý do) · `S` khác nguồn (đã khai) · `Đ` đơn giản hoá có chủ đích.
5. **Vá đủ chỗ — MỖI BẢNG/CONTROLLER MỘT FILE RIÊNG** (2 agent làm song song, sửa file dùng chung là xung đột):
   (a) entity: `Models/Port/<TênBảngNguồn>.cs`, `namespace MiniContract.Models.Port`, class + `IEntityTypeConfiguration<T>`
       trong cùng file (`ToTable("<TênBảngNguồn>")`, `HasKey` đúng DDL). **KHÔNG** sửa `AppDbContext.cs`, không thêm `DbSet` —
       dùng `db.Set<T>()`. Bảng + cột thiếu được `Seeder` **tự tạo** (xem `Data/PortInfra.cs`).
   (b) endpoint: `Controllers/Port/<TênControllerNguồn>Port.cs` (vd `Mst_OrgControllerPort.cs`), `[ApiController]`,
       route `/api/<tên-controller-nguồn-viết-thường>/<action>` (vd `/api/mst_org/search`), **POST** như nguồn.
   (c) DTO/request: cùng file controller port.
   (d) seed demo: class `<TênBảng>Seeder : IPortSeeder` trong file entity, idempotent; `Order` danh mục 10–99, nghiệp vụ ≥ 200.
6. **Build + test + verify**:
   - `dotnet build -c Release` phải `0 Error(s)`; `dotnet test MiniContract.Tests -c Release` phải xanh.
   - **Chạy thật** (build xanh CHƯA phải xong): đặt biến `PORT` (app ghi đè `ASPNETCORE_URLS`), DB SQLite tạm, gọi endpoint:
     action GHI ⇒ POST tạo/sửa rồi đọc lại thấy đúng giá trị (round-trip) **và** ít nhất 1 ca guard trả lỗi;
     action ĐỌC ⇒ ít nhất 2 bộ lọc ra số dòng khác nhau, khớp nghiệp vụ nguồn. Ghi lệnh + kết quả thật vào TÓM TẮT.
   - Dọn sạch: tắt app, xoá DB tạm.
   - **CÁCH CHẠY APP ĐÚNG (bẫy #44 đã treo agent 20 phút):** KHÔNG đọc `StandardOutput.ReadLine()` chờ dòng "Now listening" —
     log bị đệm/không in, lệnh đọc treo vô hạn. Dùng: `$p = Start-Process dotnet 'bin\Release\net8.0\MiniContract.dll' -PassThru -WindowStyle Hidden`
     rồi vòng lặp tối đa 30 lần `Start-Sleep 1; try { Invoke-WebRequest http://127.0.0.1:<PORT>/healthz -UseBasicParsing -TimeoutSec 2; break } catch {}`,
     xong `Stop-Process -Id $p.Id -Force`. Biến môi trường `PORT`, `CONNECTION_STRING` đặt bằng `$env:` TRƯỚC khi Start-Process.

## 3. Quy ước bắt buộc (đều từ lỗi đã gặp thật)

- **So chuỗi trên SQLite/Postgres**: CẤM `==`/`.Contains()` trên cột chuỗi khi nguồn là SQL Server (collation không phân biệt
  hoa/thường). Dùng `EF.Functions.Like(cot.ToLower(), pattern, "\\")`, pattern hạ chữ phía C#, escape `% _ \`.
  Nguồn dùng `=` thì Like **không wildcard**; nguồn dùng `like '%x%'` mới thêm `%`. Không tự nới rộng tìm kiếm.
- **Chuẩn hoá input của nguồn (`TUtils.CUtils`, DLL không có mã — lấy theo bản tham chiếu `05.Refs.Biz/**/Utils.cs`, mọi bản giống nhau):**
  `StdParam(x)` = `Trim().ToUpper()`, rỗng → `null` (dùng cho MÃ: DLCPCode, MST, OrgID… ⇒ mã luôn lưu CHỮ HOA);
  `StdStr(x)` = `Trim().Normalize()`, rỗng → `null` (tên, ghi chú); `StdFlag(x)` = `"1"` nếu đúng `"1"`, MỌI giá trị khác kể cả null → `"0"`.
  Port phải áp ĐÚNG hàm mà dòng nguồn gọi — `StdParam` khác `StdStr` ở chỗ viết hoa.
- **Kiểu số**: `MyValue`/float của nguồn ⇒ `double?` (decimal trên SQLite thành TEXT, SUM sai).
- **Minimal API / action**: chỉ MỘT tham số body phức hợp; cần nhiều danh sách thì bọc vào 1 DTO.
- **Hằng số**: chép nguyên văn từ `Const.Main.BE.cs`, kể cả lỗi chính tả của nguồn (vd `TRANSFERBENIFITS`).
- **Bảng master chưa có** ở đích (Mst_Province, Mst_DealerCP…): KHÔNG bịa dữ liệu; khai cột thiếu trong `gapCols`/`gapReason`
  của payload và ghi `T` trong bảng đối chiếu.
- **Nhãn** trang/endpoint: `port 1:1 <TênFileNguồn>.cs — <Bảng>` (tên file nguồn, không phải tên dự án).
- Comment code tiếng Việt **có dấu**, dẫn `file:dòng` nguồn cho mỗi cột/guard quan trọng.
- Không xoá/đổi hành vi endpoint cũ đang chạy (`/api/v1/*`, `/api/earn`, ...). Không đổi tên bảng cũ.

## 4. CẤM (vi phạm = hạng mục bị loại)

- **CẤM chép thông tin đăng nhập của nguồn** (tài khoản/mật khẩu viết cứng, token, connection string) vào BẤT KỲ đâu trong repo —
  kể cả comment. Chỉ ghi "tài khoản viết cứng tại `<file>:<dòng>` — không chép giá trị". (Sự cố thật #112: agent chép mật khẩu vào comment.)
- `git commit`, `git push`, `git checkout`/`switch`, `git reset`, `git stash`, `git clean`, đổi nhánh.
- Sửa `PARITY_PROGRESS.md`. Sửa bất kỳ thứ gì ngoài repo đích. Ghi vào `D:\idocNet`.
- Làm quá 1 hạng mục mỗi lượt. Over-claim: chưa chạy thật thì ghi `verify PENDING`, không ghi "đã xong".
- Đoán: không tìm được nguồn thì ghi `KHÔNG TÌM ĐƯỢC` + đã tìm ở đâu.

## 5. Kết thúc lượt — mục `TOM TAT` (bắt buộc, theo đúng thứ tự)

```
TOM TAT
TRANG_THAI: DONE | DA_CO | BO | CAN_XEM_TAY
NGUON: <file:dòng gate> -> <file:dòng API> -> <file:dòng biz LIVE>
FILE_DA_SUA: <danh sách>
DOI_CHIEU: <bảng K/T/S/Đ>
VERIFY: <lệnh đã chạy + kết quả thật, gồm round-trip và ca lỗi>
GAP: <cột/guard chưa port + lý do>
```

## 6. CHỈ LÀM THEO TÀI LIỆU — XUNG ĐỘT THÌ DỪNG (áp dụng cho MỌI agent)

1. Nguồn sự thật theo thứ tự: **mã nguồn gốc** (`source_excerpt.txt`, `D:\idocNet\2020.6.QContract`) → **đề bài** (`DEBAI.md`) → **luật này** → **bài học tích luỹ** (`docs\LESSONS_AGENTS.md`). Chỉ hành động trong phạm vi các tài liệu đó.
2. **Xung đột** = hai nguồn trên mâu thuẫn nhau (vd đề bài nêu số kỳ vọng mà nguồn cho kết quả khác; luật cấm điều đề bài yêu cầu), hoặc đề bài thiếu thông tin buộc phải đoán, hoặc cách duy nhất để đạt kỳ vọng là vi phạm một luật/bài học.
3. Khi gặp xung đột: **KHÔNG tự chọn, KHÔNG đoán, KHÔNG chế cách vượt.** Làm:
   - ghi file `.parity-run\conflicts\<số>_conflict.md` (tạo thư mục nếu chưa có) gồm: hạng mục, hai (hay các) nguồn mâu thuẫn kèm `file:dòng`, điều đã thử, đề xuất của bạn (chỉ là đề xuất);
   - phần việc KHÔNG bị ảnh hưởng bởi xung đột: vẫn làm xong và build xanh;
   - kết thúc `TOM TAT` với `TRANG_THAI: CAN_XEM_TAY` và dòng `CONFLICT: <mô tả ngắn + đường dẫn file xung đột>`.
4. Claude sẽ đọc các xung đột đã gom khi được đánh thức và chốt một lần. Agent KHÔNG chờ và KHÔNG lặp lại việc để "thử cho qua".
5. Agent phụ (docs/QA/lead) KHÔNG tự sửa tài liệu luật, script hay prompt: chỉ ĐỀ XUẤT vào `.parity-run\proposals\` để Claude duyệt.
