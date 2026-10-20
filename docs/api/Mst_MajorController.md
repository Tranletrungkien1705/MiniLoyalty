# Tham chiếu API Mst_MajorController (Port 1-1)

| Thông tin | Giá trị |
|---|---|
| **Phạm vi** | Tài liệu tham chiếu chi tiết 2 endpoint đã port 1-1 của Mst_MajorController (#168, #169) |
| **Cập nhật** | 2026-10-16 |
| **Phiên bản mã** | `79657b7` |
| **Trạng thái** | Nháp — chờ review |

> [!NOTE]
> Toàn bộ các endpoint thuộc controller này áp dụng chuẩn hóa JSON PascalCase tự động qua `PortJsonResultFilter` và xử lý ngoại lệ tập trung qua `PortExceptionFilter` (chuyển đổi ngoại lệ chưa bắt thành HTTP 400 `PortResult.Fail`). Chi tiết xem tại [Kiến trúc Hạ tầng Dùng chung](../kien-truc/HA_TANG_CHUNG.md).

---

## 1. Tóm tắt Nghiệp vụ & Cấu trúc Dữ liệu

### 1.1. Mục đích Nghiệp vụ
API `Mst_MajorController` quản lý danh mục chuyên ngành đào tạo (`Mst_Major`) trong hệ thống MiniLoyalty. Module này cung cấp các chức năng tìm kiếm phân trang theo từ khóa tên chuyên ngành và trạng thái hoạt động (toán tử so sánh bằng tuyệt đối `=`), và lấy toàn bộ danh sách các chuyên ngành đang hoạt động phục vụ quản lý hồ sơ nhân sự, tuyển dụng và phân loại trình độ.

### 1.2. Bảng Đối chiếu Nguồn Tra cứu (Source Code Mapping)

| Thành phần | Đường dẫn tập tin nguồn (`2022.7.Loyalty`) | Tập tin đích tương ứng (`MiniLoyalty`) |
|---|---|---|
| **Gate Controller** | `13.ClientGate\V10\idn.Loyalty.WebAPI\Controllers\Mst_MajorController.cs:25-199` | [`Controllers/Port/Mst_MajorControllerPort.cs`](../../Controllers/Port/Mst_MajorControllerPort.cs#L61) |
| **API Backend** | `11.BackEnd\V10\idn.Loyalty.Biz.Web\Controllers\MstMajorController.cs:17-88` | Tích hợp trực tiếp vào [`Controllers/Port/Mst_MajorControllerPort.cs`](../../Controllers/Port/Mst_MajorControllerPort.cs#L61) |
| **Nghiệp vụ (Biz)** | `11.BackEnd\V10\idn.Loyalty.Biz\MasterData.cs` (Get `:40346-40474`; GetX `:40189-40344`) | Tích hợp trực tiếp vào [`Controllers/Port/Mst_MajorControllerPort.cs`](../../Controllers/Port/Mst_MajorControllerPort.cs#L117-L165) |
| **Entity Model** | `12.Dev.Common\idn.Loyalty.Common\Models\HRM\Mst_Major.cs:9-19` | [`Models/Port/Mst_Major.cs:31-41`](../../Models/Port/Mst_Major.cs#L31-L41) |
| **Cấu hình EF** | Khóa draft `MajorCode` (không có DDL nguồn) | [`Models/Port/Mst_Major.cs:44-52`](../../Models/Port/Mst_Major.cs#L44-L52) |
| **Seeder Demo** | `Order => 42` (danh mục `Mst_*` dùng 10–99) | [`Models/Port/Mst_Major.cs:59-79`](../../Models/Port/Mst_Major.cs#L59-L79) |

### 1.3. Cấu trúc Bảng CSDL (`Mst_Major`)

Bảng dữ liệu vật lý `Mst_Major` gồm đúng 8 cột:

| Tên cột | Kiểu EF Core | Khóa | Cho phép NULL | Ghi chú & Ý nghĩa |
|---|---|---|---|---|
| `MajorCode` | `string` | **PK** | Không | Mã chuyên ngành (ví dụ: `MJ01`, `MJ02`). Khóa chính (khóa draft). |
| `NetworkID` | `string?` | | Có | Mã mạng nghiệp vụ. Ở đích lấy cấu hình `Biz_NetworkID`. |
| `MajorName` | `string?` | | Có | Tên chuyên ngành đào tạo (Công nghệ thông tin, Kế toán...). |
| `MajorDesc` | `string?` | | Có | Mô tả thông tin chuyên ngành. |
| `MajorGroupCode` | `string?` | | Có | Mã nhóm chuyên ngành cha (`Mst_MajorGroup`). |
| `FlagActive` | `string?` | | Có | Cờ trạng thái hoạt động: `"1"` (Đang hoạt động), `"0"` (Ngừng hoạt động). |
| `LogLUDTimeUTC` | `DateTime?` | | Có | Thời điểm cập nhật cuối cùng theo chuẩn UTC. |
| `LogLUBy` | `string?` | | Có | Người thực hiện cập nhật cuối cùng. |

**Dữ liệu mẫu khởi tạo (Seeder - `Mst_MajorSeeder`, Idempotent)**:
- Gồm 6 bản ghi demo: `MJ01` (CNTT, '1'), `MJ02` (Kỹ thuật phần mềm, '1'), `MJ03` (QTKD, '1'), `MJ04` (Kế toán DN, '0'), `MJ05` (Tài chính NH, '1'), `MJ06` (Cơ khí động lực, '1').
- Toàn bộ bản ghi gán `NetworkID = "DEMO"`, `LogLUBy = "seed"`.

---

## 2. Đặc tả Chi tiết Từng Endpoint API

---

### 2.1. Search (#168)

Tìm kiếm danh mục chuyên ngành phân trang theo từ khóa và trạng thái hoạt động.

- **Route**: `POST /api/mst_major/search`
- **Controller Action**: [`Mst_MajorControllerPort.Search`](../../Controllers/Port/Mst_MajorControllerPort.cs#L70)
- **Tham chiếu nguồn**: Gate `Mst_MajorController.cs:25-105` → API `MstMajorController.cs:17-88` → Biz `MasterData.cs:40189-40344`

#### Bảng tham số Request

Payload JSON gửi qua Request Body (`MstMajorSearchReq`), hỗ trợ cả trường hợp body rỗng:

| Tên tham số | Kiểu dữ liệu | Bắt buộc | Mặc định | Ý nghĩa & Quy tắc |
|---|---|---|---|---|
| `OrgID` | `string?` | Không | `null` | Nhận tại tham số nhưng hàm `strWhereClause_Mst_Major` (`:171-186`) **không lọc theo OrgID**. |
| `KeyWord` | `string?` | Không | `null` | Từ khóa tìm kiếm theo tên chuyên ngành (`MajorName LIKE '%kw%'`, `gate :176-179`). **Không tìm trên `MajorCode`, `MajorDesc` hay `MajorGroupCode`**. |
| `FlagActive` | `string?` | Không | `null` | Cờ hoạt động, **áp dụng toán tử so sánh bằng tuyệt đối '='** (`gate :180-183`). Không tách dấu phẩy. |
| `Ft_PageIndex` | `int` | Không | `0` | Chỉ số trang cần lấy (0-based). |
| `Ft_PageSize` | `int` | Không | `10` | Số dòng trên mỗi trang. Nếu `<= 0` trả về mảng rỗng `DataList = []` (`gate :80-88`). |

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Xác định vị trí bản ghi: `start = (long)pageIndex * pageSize`.
2. Mệnh đề lọc qua `GetXAsync`:
   - **KeyWord**: `EF.Functions.Like((x.MajorName ?? "").ToLower(), kw, PortQuery.Esc)`.
   - **FlagActive**: `EF.Functions.Like((x.FlagActive ?? "").ToLower(), fa, PortQuery.Esc)` (toán tử bằng tuyệt đối không wildcard).
3. Draft sắp theo `mm.MajorCode ASC` (`MasterData.cs:40251`), đếm tổng `itemCount = await ordered.CountAsync()` (`:40255`).
4. Trích xuất trang: Nếu `pageSize <= 0` hoặc `recordStart < 0` trả về danh sách rỗng (bài học A.12); nếu `pageSize > 0` gọi `.Skip((int)start).Take((int)pageSize)`.
5. Select cuối theo `t.MyIdxSeq ASC` (`MasterData.cs:40290`), trả về đúng 9 trường: `MyIdxSeq` (0-based) + 8 cột model nguồn.
6. Tính `PageCount`: Nếu `rows.Count == 0`, `PageCount = 0`; ngược lại `PageCount = ceil(itemCount / ps)` (`gate :80-88`).

#### Bảng mã lỗi
Endpoint không có mã lỗi nghiệp vụ riêng; ngoại lệ chưa bắt được chuyển thành HTTP 400 qua `PortExceptionFilter`.

#### Ví dụ Request / Response thực tế (từ lần chạy VERIFY #168 / #169)
- **Tìm kiếm mặc định (`{}`)**:
  - Request: `POST /api/mst_major/search` kèm `{}`
  - Response (HTTP 200): `ItemCount = 6`, `PageCount = 1`, `DataList` đủ 6 dòng (`MJ01` đến `MJ06`).
- **Lọc theo trạng thái `FlagActive = "1"`**:
  - Request: `{"FlagActive": "1"}`
  - Response: `ItemCount = 5`, `DataList` chứa 5 dòng active (`MJ01`, `MJ02`, `MJ03`, `MJ05`, `MJ06`).
- **Lọc theo trạng thái `FlagActive = "0"`**:
  - Request: `{"FlagActive": "0"}`
  - Response: `ItemCount = 1`, `DataList` chứa 1 dòng `MJ04` với `MyIdxSeq = 0`.

---

### 2.2. GetAllActive (#169)

Lấy toàn bộ danh sách các chuyên ngành đang hoạt động (`FlagActive = '1'`).

- **Route**: `POST /api/mst_major/getallactive`
- **Controller Action**: [`Mst_MajorControllerPort.GetAllActive`](../../Controllers/Port/Mst_MajorControllerPort.cs#L102)
- **Tham chiếu nguồn**: Gate `Mst_MajorController.cs:107-167` → API `MstMajorController.cs:17-88` → Biz `MasterData.cs:40346-40474`

#### Bảng tham số Request
Endpoint không nhận tham số Body (`gate :107`).

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Lọc `FlagActive = "1"` cố định với toán tử `=` (`gate :127-130, :195`), không có `KeyWord`, không lọc `OrgID`/`NetworkID`.
2. Tái sử dụng `GetXAsync` với `recordStart = 0`, `recordCount = 123456000` (`gate :142-143`).
3. Sắp xếp theo `MajorCode ASC`, `MyIdxSeq` bắt đầu từ 0.
4. Trả về mảng phẳng trần các thực thể (9 trường gồm `MyIdxSeq` + 8 cột model). Khi không có dữ liệu trả về `Data = null` (`gate :126, :149-154`).

#### Ví dụ Request / Response thực tế (từ lần chạy VERIFY #169)
- Request: `POST /api/mst_major/getallactive`
- Response (HTTP 200):
  ```json
  {
    "Success": true,
    "Data": [
      { "MyIdxSeq": 0, "MajorCode": "MJ01", "NetworkID": "DEMO", "MajorName": "Cong nghe thong tin", "MajorDesc": "Mo ta MJ01", "MajorGroupCode": "MG01", "FlagActive": "1", "LogLUDTimeUTC": "2026-10-10T10:00:00Z", "LogLUBy": "seed" },
      { "MyIdxSeq": 1, "MajorCode": "MJ02", "NetworkID": "DEMO", "MajorName": "Ky thuat phan mem", "MajorDesc": "Mo ta MJ02", "MajorGroupCode": "MG01", "FlagActive": "1", "LogLUDTimeUTC": "2026-10-10T10:00:00Z", "LogLUBy": "seed" },
      { "MyIdxSeq": 2, "MajorCode": "MJ03", "NetworkID": "DEMO", "MajorName": "Quan tri kinh doanh", "MajorDesc": "Mo ta MJ03", "MajorGroupCode": "MG02", "FlagActive": "1", "LogLUDTimeUTC": "2026-10-10T10:00:00Z", "LogLUBy": "seed" },
      { "MyIdxSeq": 3, "MajorCode": "MJ05", "NetworkID": "DEMO", "MajorName": "Tai chinh ngan hang", "MajorDesc": "Mo ta MJ05", "MajorGroupCode": "MG02", "FlagActive": "1", "LogLUDTimeUTC": "2026-10-10T10:00:00Z", "LogLUBy": "seed" },
      { "MyIdxSeq": 4, "MajorCode": "MJ06", "NetworkID": "DEMO", "MajorName": "Co khi dong luc", "MajorDesc": "Mo ta MJ06", "MajorGroupCode": "MG03", "FlagActive": "1", "LogLUDTimeUTC": "2026-10-10T10:00:00Z", "LogLUBy": "seed" }
    ]
  }
  ```
  *(Bản ghi `MJ04` có `FlagActive = '0'` bị loại bỏ đúng như nguồn).*

---

## 3. Khác biệt so với Nguồn & GAP

1. **Khóa chính & DDL CSDL**: Nguồn không có DDL tạo bảng `Mst_Major`. Khóa chính `MajorCode` là khóa draft, kiểu cột cần xác nhận thêm khi có schema production.
2. **KeyWord chỉ tìm trên MajorName**: Nguồn chỉ lọc trên `MajorName`, không lọc `MajorCode`, `MajorDesc` hay `MajorGroupCode`.
3. **Toán tử lọc FlagActive**: Sử dụng toán tử `=` nguyên giá trị (không tách dấu phẩy), khác với một số bảng dùng `IN`.
4. **Phân quyền & Log**: Bỏ qua `CheckGatewayAuthentication`, `CheckAuthorize` và ghi log `RequestLog`/`ReturnLog`.

---

## 4. Nhật ký Thay đổi

| Hạng mục (#) | Action / Endpoint | Commit Hash | Ngày hoàn thành | Ghi chú |
|---|---|---|---|---|
| #168 | `Search` | `c2e0d17` | 2026-10-16 | Khởi tạo bảng `Mst_Major`, seeder demo và endpoint tìm kiếm phân trang |
| #169 | `GetAllActive` | `b9dfe70` | 2026-10-16 | Endpoint lấy danh sách chuyên ngành đang hoạt động (mảng trần) |
