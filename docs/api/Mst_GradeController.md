# Tham chiếu API Mst_GradeController (Port 1-1)

| Thông tin | Giá trị |
|---|---|
| **Phạm vi** | Tài liệu tham chiếu chi tiết 2 endpoint đã port 1-1 của Mst_GradeController (#162, #163) |
| **Cập nhật** | 2026-10-16 |
| **Phiên bản mã** | `79657b7` |
| **Trạng thái** | Nháp — chờ review |

> [!NOTE]
> Toàn bộ các endpoint thuộc controller này áp dụng chuẩn hóa JSON PascalCase tự động qua `PortJsonResultFilter` và xử lý ngoại lệ tập trung qua `PortExceptionFilter` (chuyển đổi ngoại lệ chưa bắt thành HTTP 400 `PortResult.Fail`). Chi tiết xem tại [Kiến trúc Hạ tầng Dùng chung](../kien-truc/HA_TANG_CHUNG.md).

---

## 1. Tóm tắt Nghiệp vụ & Cấu trúc Dữ liệu

### 1.1. Mục đích Nghiệp vụ
API `Mst_GradeController` quản lý danh mục trình độ chuyên môn (`Mst_Grade`) trong hệ thống MiniLoyalty. Module này cung cấp các chức năng tìm kiếm phân trang theo từ khóa tên trình độ và trạng thái hoạt động (toán tử so sánh bằng tuyệt đối `=`), và lấy toàn bộ danh sách các trình độ chuyên môn đang hoạt động phục vụ quản lý hồ sơ nhân sự và đào tạo.

### 1.2. Bảng Đối chiếu Nguồn Tra cứu (Source Code Mapping)

| Thành phần | Đường dẫn tập tin nguồn (`2022.7.Loyalty`) | Tập tin đích tương ứng (`MiniLoyalty`) |
|---|---|---|
| **Gate Controller** | `13.ClientGate\V10\idn.Loyalty.WebAPI\Controllers\Mst_GradeController.cs:25-199` | [`Controllers/Port/Mst_GradeControllerPort.cs`](../../Controllers/Port/Mst_GradeControllerPort.cs#L69) |
| **API Backend** | `11.BackEnd\V10\idn.Loyalty.Biz.Web\Controllers\MstGradeController.cs:17-88` | Tích hợp trực tiếp vào [`Controllers/Port/Mst_GradeControllerPort.cs`](../../Controllers/Port/Mst_GradeControllerPort.cs#L69) |
| **Nghiệp vụ (Biz)** | `11.BackEnd\V10\idn.Loyalty.Biz\MasterData.cs` (Get `:35414-35542`; GetX `:35257-35412`) | Tích hợp trực tiếp vào [`Controllers/Port/Mst_GradeControllerPort.cs`](../../Controllers/Port/Mst_GradeControllerPort.cs#L142-L227) |
| **Entity Model** | `12.Dev.Common\idn.Loyalty.Common\Models\HRM\Mst_Grade.cs:9-18` | [`Models/Port/Mst_Grade.cs:30-39`](../../Models/Port/Mst_Grade.cs#L30-L39) |
| **Cấu hình EF** | Khóa draft `GradeCode` (không có DDL nguồn) | [`Models/Port/Mst_Grade.cs:42-50`](../../Models/Port/Mst_Grade.cs#L42-L50) |
| **Seeder Demo** | `Order => 37` (danh mục `Mst_*` dùng 10–99) | [`Models/Port/Mst_Grade.cs:58-79`](../../Models/Port/Mst_Grade.cs#L58-L79) |

### 1.3. Cấu trúc Bảng CSDL (`Mst_Grade`)

Bảng dữ liệu vật lý `Mst_Grade` gồm đúng 7 cột:

| Tên cột | Kiểu EF Core | Khóa | Cho phép NULL | Ghi chú & Ý nghĩa |
|---|---|---|---|---|
| `GradeCode` | `string` | **PK** | Không | Mã trình độ chuyên môn (ví dụ: `G01`, `G02`). Khóa chính (khóa draft). |
| `NetworkID` | `string?` | | Có | Mã mạng nghiệp vụ. Ở đích lấy cấu hình `Biz_NetworkID`. |
| `GradeName` | `string?` | | Có | Tên trình độ chuyên môn (Cử nhân, Kỹ sư, Thạc sĩ...). |
| `GradeDesc` | `string?` | | Có | Mô tả trình độ chuyên môn. |
| `FlagActive` | `string?` | | Có | Cờ trạng thái hoạt động: `"1"` (Đang hoạt động), `"0"` (Ngừng hoạt động). |
| `LogLUDTimeUTC` | `DateTime?` | | Có | Thời điểm cập nhật cuối cùng theo chuẩn UTC. |
| `LogLUBy` | `string?` | | Có | Người thực hiện cập nhật cuối cùng. |

**Dữ liệu mẫu khởi tạo (Seeder - `Mst_GradeSeeder`, Idempotent)**:
- Gồm 6 bản ghi demo: `G01` (Cử nhân, '1'), `G02` (Kỹ sư, '1'), `G03` (Thạc sĩ, '1'), `G04` (Tiến sĩ, '0'), `G05` (Trung cấp, '1'), `G06` (Cao đẳng, '1').
- Toàn bộ bản ghi gán `NetworkID = "DEMO"`, `LogLUBy = "seed"`.

---

## 2. Đặc tả Chi tiết Từng Endpoint API

---

### 2.1. Search (#162)

Tìm kiếm danh mục trình độ phân trang theo từ khóa và trạng thái hoạt động.

- **Route**: `POST /api/mst_grade/search`
- **Controller Action**: [`Mst_GradeControllerPort.Search`](../../Controllers/Port/Mst_GradeControllerPort.cs#L81)
- **Tham chiếu nguồn**: Gate `Mst_GradeController.cs:25-105` → API `MstGradeController.cs:17-88` → Biz `MasterData.cs:35257-35412`

#### Bảng tham số Request

Payload JSON gửi qua Request Body (`MstGradeSearchReq`), hỗ trợ cả trường hợp body rỗng:

| Tên tham số | Kiểu dữ liệu | Bắt buộc | Mặc định | Ý nghĩa & Quy tắc |
|---|---|---|---|---|
| `OrgID` | `string?` | Không | `null` | Nhận tại tham số nhưng hàm `strWhereClause_Mst_Grade` (`:171-186`) **không lọc theo OrgID**. |
| `KeyWord` | `string?` | Không | `null` | Từ khóa tìm kiếm theo tên trình độ (`GradeName LIKE '%kw%'`, `gate :176-179`). **Không lọc trên `GradeCode` hay `GradeDesc`**. |
| `FlagActive` | `string?` | Không | `null` | Cờ hoạt động, **áp dụng toán tử so sánh bằng tuyệt đối '='** (`gate :180-183`). Không tách dấu phẩy, không dùng `IN`. |
| `Ft_PageIndex` | `int` | Không | `0` | Chỉ số trang cần lấy (0-based). |
| `Ft_PageSize` | `int` | Không | `10` | Số dòng trên mỗi trang. Nếu `<= 0` trả về mảng rỗng `DataList = []` (`gate :80-88`). |

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Xác định vị trí bản ghi: `start = (long)pageIndex * pageSize`.
2. Mệnh đề lọc qua `GetXAsync`:
   - **KeyWord**: `EF.Functions.Like((x.GradeName ?? "").ToLower(), kwLower, "\\")`.
   - **FlagActive**: `EF.Functions.Like((x.FlagActive ?? "").ToLower(), faLower, "\\")` (toán tử bằng tuyệt đối không wildcard).
3. Draft truy vấn sắp theo `mg.GradeCode ASC` (`MasterData.cs:35319`), đếm tổng `itemCount = draftKeys.Count` (`:35323`).
4. Trích xuất trang: Nếu `pageSize <= 0` hoặc `recordStart < 0` trả về danh sách rỗng (bài học A.12); nếu `pageSize > 0` cắt trang theo `[start, start + count - 1]`.
5. Thực hiện `INNER JOIN` draft đã cắt trang với bảng `Mst_Grade` theo `GradeCode` và sắp xếp theo `t.MyIdxSeq ASC` (`MasterData.cs:35352-35358`).
6. Trả về đúng 8 trường: `MyIdxSeq` (0-based) + 7 cột model nguồn.
7. Tính `PageCount`: Nếu `rows.Count == 0`, `PageCount = 0`; ngược lại `PageCount = ceil(itemCount / ps)` (`gate :80-88`).

#### Bảng mã lỗi
Endpoint không có mã lỗi nghiệp vụ riêng; ngoại lệ chưa bắt được chuyển thành HTTP 400 qua `PortExceptionFilter`.

#### Ví dụ Request / Response thực tế (từ lần chạy VERIFY #162 / #163)
- **Tìm kiếm mặc định (`{}`)**:
  - Request: `POST /api/mst_grade/search` kèm `{}`
  - Response (HTTP 200): `ItemCount = 6`, `PageCount = 1`, `DataList` đủ 6 dòng (`G01` đến `G06`).
- **Lọc theo trạng thái `FlagActive = "1"`**:
  - Request: `{"FlagActive": "1"}`
  - Response: `ItemCount = 5`, `DataList` chứa 5 dòng active (`G01`, `G02`, `G03`, `G05`, `G06`).
- **Lọc theo chuỗi `"0,1"`**:
  - Do toán tử so sánh bằng `=`, chuỗi `"0,1"` tìm kiếm giá trị bằng `"0,1"` → `ItemCount = 0`, `PageCount = 0`, `DataList = []`.

---

### 2.2. GetAllActive (#163)

Lấy toàn bộ danh sách các trình độ chuyên môn đang hoạt động (`FlagActive = '1'`).

- **Route**: `POST /api/mst_grade/getallactive`
- **Controller Action**: [`Mst_GradeControllerPort.GetAllActive`](../../Controllers/Port/Mst_GradeControllerPort.cs#L114)
- **Tham chiếu nguồn**: Gate `Mst_GradeController.cs:107-167` → API `MstGradeController.cs:17-88` → Biz `MasterData.cs:35414-35542`

#### Bảng tham số Request
Endpoint không nhận tham số Body (`gate :107`).

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Lọc `FlagActive = "1"` cố định với toán tử `=` (`gate :127-130, :195`), không có `KeyWord`, không lọc `OrgID`/`NetworkID`.
2. Tái sử dụng `GetXAsync` với `recordStart = 0`, `recordCount = 123456000` (`gate :142-143`).
3. Sắp xếp theo `GradeCode ASC`, `MyIdxSeq` bắt đầu từ 0.
4. Trả về mảng phẳng trần các thực thể (8 trường gồm `MyIdxSeq` + 7 cột model). Nếu không có dữ liệu trả về `Data = null` (`gate :126, :149-154`).

#### Ví dụ Request / Response thực tế (từ lần chạy VERIFY #163)
- Request: `POST /api/mst_grade/getallactive`
- Response (HTTP 200):
  ```json
  {
    "Success": true,
    "Data": [
      { "MyIdxSeq": 0, "GradeCode": "G01", "NetworkID": "DEMO", "GradeName": "Cử nhân", "GradeDesc": "Mo ta G01", "FlagActive": "1", "LogLUDTimeUTC": "2026-10-10T10:00:00Z", "LogLUBy": "seed" },
      { "MyIdxSeq": 1, "GradeCode": "G02", "NetworkID": "DEMO", "GradeName": "Kỹ sư", "GradeDesc": "Mo ta G02", "FlagActive": "1", "LogLUDTimeUTC": "2026-10-10T10:00:00Z", "LogLUBy": "seed" },
      { "MyIdxSeq": 2, "GradeCode": "G03", "NetworkID": "DEMO", "GradeName": "Thạc sĩ", "GradeDesc": "Mo ta G03", "FlagActive": "1", "LogLUDTimeUTC": "2026-10-10T10:00:00Z", "LogLUBy": "seed" },
      { "MyIdxSeq": 3, "GradeCode": "G05", "NetworkID": "DEMO", "GradeName": "Trung cấp", "GradeDesc": "Mo ta G05", "FlagActive": "1", "LogLUDTimeUTC": "2026-10-10T10:00:00Z", "LogLUBy": "seed" },
      { "MyIdxSeq": 4, "GradeCode": "G06", "NetworkID": "DEMO", "GradeName": "Cao đẳng", "GradeDesc": "Mo ta G06", "FlagActive": "1", "LogLUDTimeUTC": "2026-10-10T10:00:00Z", "LogLUBy": "seed" }
    ]
  }
  ```
  *(Bản ghi `G04` có `FlagActive = '0'` bị loại bỏ đúng như nguồn).*

---

## 3. Khác biệt so với Nguồn & GAP

1. **Khóa chính & DDL CSDL**: Nguồn không có DDL tạo bảng `Mst_Grade`. Khóa chính `GradeCode` là khóa draft, kiểu cột cần xác nhận thêm khi có schema production.
2. **KeyWord chỉ tìm trên GradeName**: Nguồn chỉ lọc trên `GradeName`, không lọc `GradeCode` hay `GradeDesc`.
3. **Toán tử lọc FlagActive**: Sử dụng toán tử `=` nguyên giá trị (không tách dấu phẩy), khác với một số bảng dùng `IN`.
4. **Phân quyền & Log**: Bỏ qua `CheckGatewayAuthentication`, `CheckAuthorize` và ghi log `RequestLog`/`ReturnLog`.

---

## 4. Nhật ký Thay đổi

| Hạng mục (#) | Action / Endpoint | Commit Hash | Ngày hoàn thành | Ghi chú |
|---|---|---|---|---|
| #162 | `Search` | `3b851aa` | 2026-10-16 | Khởi tạo bảng `Mst_Grade`, seeder demo và endpoint tìm kiếm phân trang |
| #163 | `GetAllActive` | `6c2dc39` | 2026-10-16 | Endpoint lấy danh sách trình độ đang hoạt động (mảng trần) |
