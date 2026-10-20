# Tham chiếu API Mst_LeaveTypeController (Port 1-1)

| Thông tin | Giá trị |
|---|---|
| **Phạm vi** | Tài liệu tham chiếu chi tiết 2 endpoint đã port 1-1 của Mst_LeaveTypeController (#166, #167) |
| **Cập nhật** | 2026-10-16 |
| **Phiên bản mã** | `79657b7` |
| **Trạng thái** | Nháp — chờ review |

> [!NOTE]
> Toàn bộ các endpoint thuộc controller này áp dụng chuẩn hóa JSON PascalCase tự động qua `PortJsonResultFilter` và xử lý ngoại lệ tập trung qua `PortExceptionFilter` (chuyển đổi ngoại lệ chưa bắt thành HTTP 400 `PortResult.Fail`). Chi tiết xem tại [Kiến trúc Hạ tầng Dùng chung](../kien-truc/HA_TANG_CHUNG.md).

---

## 1. Tóm tắt Nghiệp vụ & Cấu trúc Dữ liệu

### 1.1. Mục đích Nghiệp vụ
API `Mst_LeaveTypeController` quản lý danh mục loại nghỉ phép (`Mst_LeaveType`) trong hệ thống MiniLoyalty. Module này cung cấp các chức năng tìm kiếm phân trang theo từ khóa tên loại nghỉ phép và trạng thái hoạt động (toán tử so sánh bằng tuyệt đối `=`), và lấy toàn bộ danh sách các loại nghỉ phép đang hoạt động phục vụ quản lý nhân sự, chấm công và chế độ bảo hiểm.

### 1.2. Bảng Đối chiếu Nguồn Tra cứu (Source Code Mapping)

| Thành phần | Đường dẫn tập tin nguồn (`2022.7.Loyalty`) | Tập tin đích tương ứng (`MiniLoyalty`) |
|---|---|---|
| **Gate Controller** | `13.ClientGate\V10\idn.Loyalty.WebAPI\Controllers\Mst_LeaveTypeController.cs:25-199` | [`Controllers/Port/Mst_LeaveTypeControllerPort.cs`](../../Controllers/Port/Mst_LeaveTypeControllerPort.cs#L69) |
| **API Backend** | `11.BackEnd\V10\idn.Loyalty.Biz.Web\Controllers\MstLeaveTypeController.cs:17-88` | Tích hợp trực tiếp vào [`Controllers/Port/Mst_LeaveTypeControllerPort.cs`](../../Controllers/Port/Mst_LeaveTypeControllerPort.cs#L69) |
| **Nghiệp vụ (Biz)** | `11.BackEnd\V10\idn.Loyalty.Biz\MasterData.cs` (Get `:31748-31876`; GetX `:31591-31746`) | Tích hợp trực tiếp vào [`Controllers/Port/Mst_LeaveTypeControllerPort.cs`](../../Controllers/Port/Mst_LeaveTypeControllerPort.cs#L135-L195) |
| **Entity Model** | `12.Dev.Common\idn.Loyalty.Common\Models\HRM\Mst_LeaveType.cs:11-19` | [`Models/Port/Mst_LeaveType.cs:32-44`](../../Models/Port/Mst_LeaveType.cs#L32-L44) |
| **Cấu hình EF** | Khóa draft `LeaveTypeCode` (không có DDL nguồn) | [`Models/Port/Mst_LeaveType.cs:46-54`](../../Models/Port/Mst_LeaveType.cs#L46-L54) |
| **Seeder Demo** | `Order => 41` (danh mục `Mst_*` dùng 10–99) | [`Models/Port/Mst_LeaveType.cs:60-147`](../../Models/Port/Mst_LeaveType.cs#L60-L147) |

### 1.3. Cấu trúc Bảng CSDL (`Mst_LeaveType`)

Bảng dữ liệu vật lý `Mst_LeaveType` gồm đúng 9 cột:

| Tên cột | Kiểu EF Core | Khóa | Cho phép NULL | Ghi chú & Ý nghĩa |
|---|---|---|---|---|
| `LeaveTypeCode` | `string` | **PK** | Không | Mã loại nghỉ phép (ví dụ: `LT01`, `LT02`). Khóa chính (khóa draft). |
| `NetworkID` | `string?` | | Có | Mã mạng nghiệp vụ. Ở đích lấy cấu hình `Biz_NetworkID`. |
| `LeaveType` | `string?` | | Có | Tên loại nghỉ phép (cột tên là `LeaveType`, không phải `LeaveTypeName`). |
| `LeaveTypeDesc` | `string?` | | Có | Mô tả loại nghỉ phép. |
| `FlagUnPaid` | `string?` | | Có | Cờ nghỉ không hưởng lương (`"1"`: Không lương, `"0"`: Có lương). |
| `FlagInsurance` | `string?` | | Có | Cờ chế độ bảo hiểm xã hội chi trả (`"1"`: Có, `"0"`: Không). |
| `FlagActive` | `string?` | | Có | Cờ trạng thái hoạt động: `"1"` (Đang hoạt động), `"0"` (Ngừng hoạt động). |
| `LogLUDTimeUTC` | `DateTime?` | | Có | Thời điểm cập nhật cuối cùng theo chuẩn UTC. |
| `LogLUBy` | `string?` | | Có | Người thực hiện cập nhật cuối cùng. |

**Dữ liệu mẫu khởi tạo (Seeder - `Mst_LeaveTypeSeeder`, Idempotent)**:
- Gồm 6 bản ghi demo: `LT01` (Nghỉ phép năm, '1'), `LT02` (Nghỉ ốm, '1'), `LT03` (Nghỉ thai sản, '1'), `LT04` (Nghỉ không lương, '1'), `LT05` (Nghỉ phép cưới, '1'), `LT06` (Nghỉ phép cũ ngừng, '0').
- Toàn bộ bản ghi gán `NetworkID = "DEMO"`, `LogLUBy = "seed"`.

---

## 2. Đặc tả Chi tiết Từng Endpoint API

---

### 2.1. Search (#166)

Tìm kiếm danh mục loại nghỉ phép phân trang theo từ khóa và trạng thái hoạt động.

- **Route**: `POST /api/mst_leavetype/search`
- **Controller Action**: [`Mst_LeaveTypeControllerPort.Search`](../../Controllers/Port/Mst_LeaveTypeControllerPort.cs#L81)
- **Tham chiếu nguồn**: Gate `Mst_LeaveTypeController.cs:25-105` → API `MstLeaveTypeController.cs:17-88` → Biz `MasterData.cs:31591-31746`

#### Bảng tham số Request

Payload JSON gửi qua Request Body (`MstLeaveTypeSearchReq`), hỗ trợ cả trường hợp body rỗng:

| Tên tham số | Kiểu dữ liệu | Bắt buộc | Mặc định | Ý nghĩa & Quy tắc |
|---|---|---|---|---|
| `OrgID` | `string?` | Không | `null` | Nhận tại tham số nhưng hàm `strWhereClause_Mst_LeaveType` (`:171-186`) **không lọc theo OrgID**. |
| `KeyWord` | `string?` | Không | `null` | Từ khóa tìm kiếm theo tên loại nghỉ phép (`LeaveType LIKE '%kw%'`, `gate :176-179`). **Không tìm trên `LeaveTypeCode` hay `LeaveTypeDesc`**. |
| `FlagActive` | `string?` | Không | `null` | Cờ hoạt động, **áp dụng toán tử so sánh bằng tuyệt đối '='** (`gate :180-183`). Không tách dấu phẩy, chuỗi `"0,1"` tìm chính xác chuỗi `"0,1"`. |
| `Ft_PageIndex` | `int` | Không | `0` | Chỉ số trang cần lấy (0-based). |
| `Ft_PageSize` | `int` | Không | `10` | Số dòng trên mỗi trang. Nếu `<= 0` trả về mảng rỗng `DataList = []` (`gate :80-88`). |

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Xác định vị trí bản ghi: `start = (long)pageIndex * pageSize`.
2. Mệnh đề lọc qua `GetXAsync`:
   - **KeyWord**: `EF.Functions.Like((x.LeaveType ?? "").ToLower(), kw, PortQuery.Esc)`.
   - **FlagActive**: `EF.Functions.Like((x.FlagActive ?? "").ToLower(), fa, PortQuery.Esc)` (toán tử bằng tuyệt đối không wildcard).
3. Draft sắp theo `mlt.LeaveTypeCode ASC` (`MasterData.cs:31653`), đếm tổng `itemCount = await ordered.CountAsync()` (`:31657`).
4. Trích xuất trang: Nếu `pageSize <= 0` hoặc `recordStart < 0` trả về danh sách rỗng (bài học A.12); nếu `pageSize > 0` gọi `.Skip((int)start).Take((int)pageSize)`.
5. Select cuối theo `t.MyIdxSeq ASC` (`MasterData.cs:31692`), trả về đúng 10 trường: `MyIdxSeq` (0-based) + 9 cột model nguồn.
6. Tính `PageCount`: Nếu `rows.Count == 0`, `PageCount = 0`; ngược lại `PageCount = ceil(itemCount / ps)` (`gate :80-88`).

#### Bảng mã lỗi
Endpoint không có mã lỗi nghiệp vụ riêng; ngoại lệ chưa bắt được chuyển thành HTTP 400 qua `PortExceptionFilter`.

#### Ví dụ Request / Response thực tế (từ lần chạy VERIFY #166 / #167)
- **Tìm kiếm mặc định (`{}`)**:
  - Request: `POST /api/mst_leavetype/search` kèm `{}`
  - Response (HTTP 200): `ItemCount = 6`, `PageCount = 1`, `DataList` đủ 6 dòng (`LT01` đến `LT06`).
- **Lọc theo trạng thái `FlagActive = "1"`**:
  - Request: `{"FlagActive": "1"}`
  - Response: `ItemCount = 5`, `DataList` chứa 5 dòng active (`LT01` đến `LT05`).
- **Lọc theo chuỗi `"0,1"`**:
  - Do toán tử so sánh bằng `=`, chuỗi `"0,1"` tìm kiếm chính xác chuỗi `"0,1"` → `ItemCount = 0`, `PageCount = 0`, `DataList = []`.

---

### 2.2. GetAllActive (#167)

Lấy toàn bộ danh sách các loại nghỉ phép đang hoạt động (`FlagActive = '1'`).

- **Route**: `POST /api/mst_leavetype/getallactive`
- **Controller Action**: [`Mst_LeaveTypeControllerPort.GetAllActive`](../../Controllers/Port/Mst_LeaveTypeControllerPort.cs#L116)
- **Tham chiếu nguồn**: Gate `Mst_LeaveTypeController.cs:107-167` → API `MstLeaveTypeController.cs:17-88` → Biz `MasterData.cs:31748-31876`

#### Bảng tham số Request
Endpoint không nhận tham số Body (`gate :107`).

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Lọc `FlagActive = "1"` cố định với toán tử `=` (`gate :127-130, :195`), không có `KeyWord`, không lọc `OrgID`/`NetworkID`.
2. Tái sử dụng `GetXAsync` với `recordStart = 0`, `recordCount = 123456000` (`gate :142-143`).
3. Sắp xếp theo `LeaveTypeCode ASC`, `MyIdxSeq` bắt đầu từ 0.
4. Trả về mảng phẳng trần các thực thể (10 trường gồm `MyIdxSeq` + 9 cột model). Khi không có dữ liệu trả về `Data = null` (`gate :126, :149-154`).

#### Ví dụ Request / Response thực tế (từ lần chạy VERIFY #167)
- Request: `POST /api/mst_leavetype/getallactive`
- Response (HTTP 200):
  ```json
  {
    "Success": true,
    "Data": [
      { "MyIdxSeq": 0, "LeaveTypeCode": "LT01", "NetworkID": "DEMO", "LeaveType": "Nghi phep nam", "LeaveTypeDesc": "Mo ta LT01", "FlagUnPaid": "0", "FlagInsurance": "0", "FlagActive": "1", "LogLUDTimeUTC": "2026-10-10T10:00:00Z", "LogLUBy": "seed" },
      { "MyIdxSeq": 1, "LeaveTypeCode": "LT02", "NetworkID": "DEMO", "LeaveType": "Nghi om", "LeaveTypeDesc": "Mo ta LT02", "FlagUnPaid": "0", "FlagInsurance": "1", "FlagActive": "1", "LogLUDTimeUTC": "2026-10-10T10:00:00Z", "LogLUBy": "seed" },
      { "MyIdxSeq": 2, "LeaveTypeCode": "LT03", "NetworkID": "DEMO", "LeaveType": "Nghi thai san", "LeaveTypeDesc": "Mo ta LT03", "FlagUnPaid": "0", "FlagInsurance": "1", "FlagActive": "1", "LogLUDTimeUTC": "2026-10-10T10:00:00Z", "LogLUBy": "seed" },
      { "MyIdxSeq": 3, "LeaveTypeCode": "LT04", "NetworkID": "DEMO", "LeaveType": "Nghi khong luong", "LeaveTypeDesc": "Mo ta LT04", "FlagUnPaid": "1", "FlagInsurance": "0", "FlagActive": "1", "LogLUDTimeUTC": "2026-10-10T10:00:00Z", "LogLUBy": "seed" },
      { "MyIdxSeq": 4, "LeaveTypeCode": "LT05", "NetworkID": "DEMO", "LeaveType": "Nghi phep cuoi", "LeaveTypeDesc": "Mo ta LT05", "FlagUnPaid": "0", "FlagInsurance": "0", "FlagActive": "1", "LogLUDTimeUTC": "2026-10-10T10:00:00Z", "LogLUBy": "seed" }
    ]
  }
  ```
  *(Bản ghi `LT06` có `FlagActive = '0'` bị loại bỏ đúng như nguồn).*

---

## 3. Khác biệt so với Nguồn & GAP

1. **Khóa chính & DDL CSDL**: Nguồn không có DDL tạo bảng `Mst_LeaveType`. Khóa chính `LeaveTypeCode` là khóa draft, kiểu cột cần xác nhận thêm khi có schema production.
2. **Tên cột LeaveType**: Cột tên là `LeaveType`, không phải `LeaveTypeName`.
3. **KeyWord chỉ tìm trên LeaveType**: Nguồn chỉ lọc trên `LeaveType`, không lọc `LeaveTypeCode` hay `LeaveTypeDesc`.
4. **Phân quyền & Log**: Bỏ qua `CheckGatewayAuthentication`, `CheckAuthorize` và ghi log `RequestLog`/`ReturnLog`.

---

## 4. Nhật ký Thay đổi

| Hạng mục (#) | Action / Endpoint | Commit Hash | Ngày hoàn thành | Ghi chú |
|---|---|---|---|---|
| #166 | `Search` | `b28baba` | 2026-10-16 | Khởi tạo bảng `Mst_LeaveType`, seeder demo và endpoint tìm kiếm phân trang |
| #167 | `GetAllActive` | `39bc3de` | 2026-10-16 | Endpoint lấy danh sách loại nghỉ phép đang hoạt động (mảng trần) |
