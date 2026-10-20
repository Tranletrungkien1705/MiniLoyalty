# Tham chiếu API Mst_LeaveDayController (Port 1-1)

| Thông tin | Giá trị |
|---|---|
| **Phạm vi** | Tài liệu tham chiếu chi tiết 2 endpoint đã port 1-1 của Mst_LeaveDayController (#164, #165) |
| **Cập nhật** | 2026-10-16 |
| **Phiên bản mã** | `79657b7` |
| **Trạng thái** | Nháp — chờ review |

> [!NOTE]
> Toàn bộ các endpoint thuộc controller này áp dụng chuẩn hóa JSON PascalCase tự động qua `PortJsonResultFilter` và xử lý ngoại lệ tập trung qua `PortExceptionFilter` (chuyển đổi ngoại lệ chưa bắt thành HTTP 400 `PortResult.Fail`). Chi tiết xem tại [Kiến trúc Hạ tầng Dùng chung](../kien-truc/HA_TANG_CHUNG.md).

---

## 1. Tóm tắt Nghiệp vụ & Cấu trúc Dữ liệu

### 1.1. Mục đích Nghiệp vụ
API `Mst_LeaveDayController` quản lý danh mục cấu hình số ngày nghỉ phép theo thâm niên và đối tượng nghề nghiệp (`Mst_LeaveDay`) trong hệ thống MiniLoyalty. Module này cung cấp các chức năng tìm kiếm phân trang theo từ khóa số ngày phép thông thường và trạng thái hoạt động (toán tử so sánh bằng tuyệt đối `=`), và lấy toàn bộ danh sách các chính sách ngày phép đang hoạt động phục vụ tính toán quyền lợi nhân sự.

### 1.2. Bảng Đối chiếu Nguồn Tra cứu (Source Code Mapping)

| Thành phần | Đường dẫn tập tin nguồn (`2022.7.Loyalty`) | Tập tin đích tương ứng (`MiniLoyalty`) |
|---|---|---|
| **Gate Controller** | `13.ClientGate\V10\idn.Loyalty.WebAPI\Controllers\Mst_LeaveDayController.cs:25-199` | [`Controllers/Port/Mst_LeaveDayControllerPort.cs`](../../Controllers/Port/Mst_LeaveDayControllerPort.cs#L72) |
| **API Backend** | `11.BackEnd\V10\idn.Loyalty.Biz.Web\Controllers\MstLeaveDayController.cs:17-88` | Tích hợp trực tiếp vào [`Controllers/Port/Mst_LeaveDayControllerPort.cs`](../../Controllers/Port/Mst_LeaveDayControllerPort.cs#L72) |
| **Nghiệp vụ (Biz)** | `11.BackEnd\V10\idn.Loyalty.Biz\MasterData.cs` (Get `:30323-30451`; GetX `:30166-30321`) | Tích hợp trực tiếp vào [`Controllers/Port/Mst_LeaveDayControllerPort.cs`](../../Controllers/Port/Mst_LeaveDayControllerPort.cs#L144-L232) |
| **Entity Model** | `12.Dev.Common\idn.Loyalty.Common\Models\HRM\Mst_LeaveDay.cs:9-21` | [`Models/Port/Mst_LeaveDay.cs:34-46`](../../Models/Port/Mst_LeaveDay.cs#L34-L46) |
| **Cấu hình EF** | Khóa draft `LeaveDayCodeSys` (không có DDL nguồn) | [`Models/Port/Mst_LeaveDay.cs:49-57`](../../Models/Port/Mst_LeaveDay.cs#L49-L57) |
| **Seeder Demo** | `Order => 38` (danh mục `Mst_*` dùng 10–99) | [`Models/Port/Mst_LeaveDay.cs:65-86`](../../Models/Port/Mst_LeaveDay.cs#L65-L86) |

### 1.3. Cấu trúc Bảng CSDL (`Mst_LeaveDay`)

Bảng dữ liệu vật lý `Mst_LeaveDay` gồm đúng 10 cột:

| Tên cột | Kiểu EF Core | Khóa | Cho phép NULL | Ghi chú & Ý nghĩa |
|---|---|---|---|---|
| `LeaveDayCodeSys` | `string` | **PK** | Không | Mã hệ thống cấu hình ngày phép (ví dụ: `LD01`, `LD02`). Khóa chính (khóa draft). |
| `NetworkID` | `string?` | | Có | Mã mạng nghiệp vụ. Ở đích lấy từ cấu hình `Biz_NetworkID`. |
| `NormalLeaveDay` | `double?` | | Có | Số ngày nghỉ phép tiêu chuẩn trong năm (kiểu số thực `double?`). |
| `MinDevotedYear` | `double?` | | Có | Số năm cống hiến tối thiểu để áp dụng mức phép. |
| `LeaveDaySpecial` | `double?` | | Có | Số ngày nghỉ phép đặc biệt theo đối tượng. |
| `LeaveDayJob` | `double?` | | Có | Số ngày nghỉ phép cộng thêm theo đặc thù công việc. |
| `FlagActive` | `string?` | | Có | Cờ trạng thái hoạt động: `"1"` (Đang hoạt động), `"0"` (Ngừng hoạt động). |
| `Remark` | `string?` | | Có | Ghi chú chính sách ngày phép. |
| `LogLUDTimeUTC` | `DateTime?` | | Có | Thời điểm cập nhật cuối cùng theo chuẩn UTC. |
| `LogLUBy` | `string?` | | Có | Người thực hiện cập nhật cuối cùng. |

**Dữ liệu mẫu khởi tạo (Seeder - `Mst_LeaveDaySeeder`, Idempotent)**:
- Gồm 6 bản ghi demo: `LD01` (12 ngày tiêu chuẩn, '1'), `LD02` (14 ngày, '1'), `LD03` (15 ngày, '1'), `LD04` (16 ngày, '0'), `LD05` (18 ngày, '1'), `LD06` (24 ngày, '1').
- Toàn bộ bản ghi gán `NetworkID = "DEMO"`, `LogLUBy = "seed"`.

---

## 2. Đặc tả Chi tiết Từng Endpoint API

---

### 2.1. Search (#164)

Tìm kiếm danh mục ngày nghỉ phép phân trang theo từ khóa và trạng thái hoạt động.

- **Route**: `POST /api/mst_leaveday/search`
- **Controller Action**: [`Mst_LeaveDayControllerPort.Search`](../../Controllers/Port/Mst_LeaveDayControllerPort.cs#L84)
- **Tham chiếu nguồn**: Gate `Mst_LeaveDayController.cs:25-105` → API `MstLeaveDayController.cs:17-88` → Biz `MasterData.cs:30166-30321`

#### Bảng tham số Request

Payload JSON gửi qua Request Body (`MstLeaveDaySearchReq`), hỗ trợ cả trường hợp body rỗng:

| Tên tham số | Kiểu dữ liệu | Bắt buộc | Mặc định | Ý nghĩa & Quy tắc |
|---|---|---|---|---|
| `OrgID` | `string?` | Không | `null` | Nhận tại tham số nhưng hàm `strWhereClause_Mst_LeaveDay` (`:171-186`) **không lọc theo OrgID**. |
| `KeyWord` | `string?` | Không | `null` | Từ khóa tìm kiếm theo số ngày phép thông thường (`NormalLeaveDay LIKE '%kw%'`, `gate :176-179`). **Không tìm trên `LeaveDayCodeSys` hay `Remark`**. |
| `FlagActive` | `string?` | Không | `null` | Cờ hoạt động, **áp dụng toán tử so sánh bằng tuyệt đối '='** (`gate :180-183`). Không tách dấu phẩy. |
| `Ft_PageIndex` | `int` | Không | `0` | Chỉ số trang cần lấy (0-based). |
| `Ft_PageSize` | `int` | Không | `10` | Số dòng trên mỗi trang. Nếu `<= 0` trả về mảng rỗng `DataList = []` (`gate :80-88`). |

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Xác định vị trí bản ghi: `start = (long)pageIndex * pageSize`.
2. Mệnh đề lọc qua `GetXAsync`:
   - **KeyWord**: Chuyển đổi `NormalLeaveDay` sang chuỗi và lọc `EF.Functions.Like((x.NormalLeaveDay.ToString()).ToLower(), kwLower, "\\")`.
   - **FlagActive**: `EF.Functions.Like((x.FlagActive ?? "").ToLower(), faLower, "\\")` (toán tử bằng tuyệt đối không wildcard).
3. Draft truy vấn sắp theo `mld.LeaveDayCodeSys ASC` (`MasterData.cs:30229`), đếm tổng `itemCount = draftKeys.Count` (`:30232`).
4. Trích xuất trang: Nếu `pageSize <= 0` hoặc `recordStart < 0` trả về danh sách rỗng (bài học A.12); nếu `pageSize > 0` cắt trang theo `[start, start + count - 1]`.
5. Thực hiện `INNER JOIN` draft đã cắt trang với bảng `Mst_LeaveDay` theo `LeaveDayCodeSys` và sắp xếp theo `t.MyIdxSeq ASC` (`MasterData.cs:30259-30269`).
6. Trả về đúng 11 trường: `MyIdxSeq` (0-based) + 10 cột model nguồn.
7. Tính `PageCount`: Nếu `rows.Count == 0`, `PageCount = 0`; ngược lại `PageCount = ceil(itemCount / ps)` (`gate :80-88`).

#### Bảng mã lỗi
Endpoint không có mã lỗi nghiệp vụ riêng; ngoại lệ chưa bắt được chuyển thành HTTP 400 qua `PortExceptionFilter`.

#### Ví dụ Request / Response thực tế (từ lần chạy VERIFY #164 / #165)
- **Tìm kiếm mặc định (`{}`)**:
  - Request: `POST /api/mst_leaveday/search` kèm `{}`
  - Response (HTTP 200): `ItemCount = 6`, `PageCount = 1`, `DataList` đủ 6 dòng (`LD01` đến `LD06`).
- **Lọc theo trạng thái `FlagActive = "0"`**:
  - Request: `{"FlagActive": "0"}`
  - Response: `ItemCount = 1`, `DataList` chứa 1 dòng `LD04` với `MyIdxSeq = 0`.

---

### 2.2. GetAllActive (#165)

Lấy toàn bộ danh sách các chính sách ngày phép đang hoạt động (`FlagActive = '1'`).

- **Route**: `POST /api/mst_leaveday/getallactive`
- **Controller Action**: [`Mst_LeaveDayControllerPort.GetAllActive`](../../Controllers/Port/Mst_LeaveDayControllerPort.cs#L116)
- **Tham chiếu nguồn**: Gate `Mst_LeaveDayController.cs:107-167` → API `MstLeaveDayController.cs:17-88` → Biz `MasterData.cs:30453-30543`

#### Bảng tham số Request
Endpoint không nhận tham số Body (`gate :107`).

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Lọc `FlagActive = "1"` cố định với toán tử `=` (`gate :127-130, :188-199`), không có `KeyWord`, không lọc `OrgID`/`NetworkID`.
2. Tái sử dụng `GetXAsync` với `recordStart = 0`, `recordCount = 123456000` (`gate :142-143`).
3. Sắp xếp theo `LeaveDayCodeSys ASC`, `MyIdxSeq` bắt đầu từ 0.
4. Trả về mảng phẳng trần các thực thể (11 trường gồm `MyIdxSeq` + 10 cột model). Khi không có dữ liệu trả về `Data = null` (`gate :126, :149-154`).

#### Ví dụ Request / Response thực tế (từ lần chạy VERIFY #165)
- Request: `POST /api/mst_leaveday/getallactive`
- Response (HTTP 200):
  ```json
  {
    "Success": true,
    "Data": [
      { "MyIdxSeq": 0, "LeaveDayCodeSys": "LD01", "NetworkID": "DEMO", "NormalLeaveDay": 12.0, "MinDevotedYear": 1.0, "LeaveDaySpecial": 2.0, "LeaveDayJob": 0.0, "FlagActive": "1", "Remark": "Phep tieu chuan 1 nam", "LogLUDTimeUTC": "2026-10-10T10:00:00Z", "LogLUBy": "seed" },
      { "MyIdxSeq": 1, "LeaveDayCodeSys": "LD02", "NetworkID": "DEMO", "NormalLeaveDay": 14.0, "MinDevotedYear": 5.0, "LeaveDaySpecial": 3.0, "LeaveDayJob": 1.0, "FlagActive": "1", "Remark": "Phep tham nien 5 nam", "LogLUDTimeUTC": "2026-10-10T10:00:00Z", "LogLUBy": "seed" },
      { "MyIdxSeq": 2, "LeaveDayCodeSys": "LD03", "NetworkID": "DEMO", "NormalLeaveDay": 15.0, "MinDevotedYear": 10.0, "LeaveDaySpecial": 4.0, "LeaveDayJob": 2.0, "FlagActive": "1", "Remark": "Phep tham nien 10 nam", "LogLUDTimeUTC": "2026-10-10T10:00:00Z", "LogLUBy": "seed" },
      { "MyIdxSeq": 3, "LeaveDayCodeSys": "LD05", "NetworkID": "DEMO", "NormalLeaveDay": 18.0, "MinDevotedYear": 20.0, "LeaveDaySpecial": 6.0, "LeaveDayJob": 3.0, "FlagActive": "1", "Remark": "Phep tham nien 20 nam", "LogLUDTimeUTC": "2026-10-10T10:00:00Z", "LogLUBy": "seed" },
      { "MyIdxSeq": 4, "LeaveDayCodeSys": "LD06", "NetworkID": "DEMO", "NormalLeaveDay": 24.0, "MinDevotedYear": 25.0, "LeaveDaySpecial": 8.0, "LeaveDayJob": 4.0, "FlagActive": "1", "Remark": "Phep dac thu 14", "LogLUDTimeUTC": "2026-10-10T10:00:00Z", "LogLUBy": "seed" }
    ]
  }
  ```
  *(Bản ghi `LD04` có `FlagActive = '0'` bị loại bỏ đúng như nguồn).*

---

## 3. Khác biệt so với Nguồn & GAP

1. **Khóa chính & DDL CSDL**: Nguồn không có DDL tạo bảng `Mst_LeaveDay`. Khóa chính `LeaveDayCodeSys` là khóa draft, kiểu cột cần xác nhận thêm khi có schema production.
2. **Kiểu số thực double?**: Các cột số ngày và năm cống hiến sử dụng kiểu `double?` để tránh lỗi lưu trữ TEXT trên SQLite.
3. **KeyWord chỉ tìm trên NormalLeaveDay**: Nguồn chuyển đổi số ngày sang chuỗi và tìm kiếm LIKE, không tìm trên mã hay ghi chú.
4. **Phân quyền & Log**: Bỏ qua `CheckGatewayAuthentication`, `CheckAuthorize` và ghi log `RequestLog`/`ReturnLog`.

---

## 4. Nhật ký Thay đổi

| Hạng mục (#) | Action / Endpoint | Commit Hash | Ngày hoàn thành | Ghi chú |
|---|---|---|---|---|
| #164 | `Search` | `c6b0984` | 2026-10-16 | Khởi tạo bảng `Mst_LeaveDay`, seeder demo và endpoint tìm kiếm phân trang |
| #165 | `GetAllActive` | `03b59f5` | 2026-10-16 | Endpoint lấy danh sách ngày phép đang hoạt động (mảng trần) |
