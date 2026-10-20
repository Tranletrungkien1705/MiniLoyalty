# Tham chiếu API Mst_MajorGroupController (Port 1-1)

| Thông tin | Giá trị |
|---|---|
| **Phạm vi** | Tài liệu tham chiếu chi tiết 2 endpoint đã port 1-1 của Mst_MajorGroupController (#170, #171) |
| **Cập nhật** | 2026-10-16 |
| **Phiên bản mã** | `79657b7` |
| **Trạng thái** | Nháp — chờ review |

> [!NOTE]
> Toàn bộ các endpoint thuộc controller này áp dụng chuẩn hóa JSON PascalCase tự động qua `PortJsonResultFilter` và xử lý ngoại lệ tập trung qua `PortExceptionFilter` (chuyển đổi ngoại lệ chưa bắt thành HTTP 400 `PortResult.Fail`). Chi tiết xem tại [Kiến trúc Hạ tầng Dùng chung](../kien-truc/HA_TANG_CHUNG.md).

---

## 1. Tóm tắt Nghiệp vụ & Cấu trúc Dữ liệu

### 1.1. Mục đích Nghiệp vụ
API `Mst_MajorGroupController` quản lý danh mục nhóm chuyên ngành đào tạo (`Mst_MajorGroup`) trong hệ thống MiniLoyalty. Module này cung cấp các chức năng tìm kiếm phân trang theo từ khóa tên nhóm chuyên ngành và trạng thái hoạt động (toán tử so sánh bằng tuyệt đối `=`), và lấy toàn bộ danh sách các nhóm chuyên ngành đang hoạt động phục vụ phân cấp trình độ và hồ sơ nhân sự.

### 1.2. Bảng Đối chiếu Nguồn Tra cứu (Source Code Mapping)

| Thành phần | Đường dẫn tập tin nguồn (`2022.7.Loyalty`) | Tập tin đích tương ứng (`MiniLoyalty`) |
|---|---|---|
| **Gate Controller** | `13.ClientGate\V10\idn.Loyalty.WebAPI\Controllers\Mst_MajorGroupController.cs:25-199` | [`Controllers/Port/Mst_MajorGroupControllerPort.cs`](../../Controllers/Port/Mst_MajorGroupControllerPort.cs#L65) |
| **API Backend** | `11.BackEnd\V10\idn.Loyalty.Biz.Web\Controllers\MstMajorGroupController.cs:17-88` | Tích hợp trực tiếp vào [`Controllers/Port/Mst_MajorGroupControllerPort.cs`](../../Controllers/Port/Mst_MajorGroupControllerPort.cs#L65) |
| **Nghiệp vụ (Biz)** | `11.BackEnd\V10\idn.Loyalty.Biz\MasterData.cs` (Get `:39044-39172`; GetX `:38887-39042`) | Tích hợp trực tiếp vào [`Controllers/Port/Mst_MajorGroupControllerPort.cs`](../../Controllers/Port/Mst_MajorGroupControllerPort.cs#L120-L166) |
| **Entity Model** | `12.Dev.Common\idn.Loyalty.Common\Models\HRM\Mst_MajorGroup.cs:9-18` | [`Models/Port/Mst_MajorGroup.cs:29-38`](../../Models/Port/Mst_MajorGroup.cs#L29-L38) |
| **Cấu hình EF** | Khóa draft `MajorGroupCode` (không có DDL nguồn) | [`Models/Port/Mst_MajorGroup.cs:41-49`](../../Models/Port/Mst_MajorGroup.cs#L41-L49) |
| **Seeder Demo** | `Order => 43` (danh mục `Mst_*` dùng 10–99) | [`Models/Port/Mst_MajorGroup.cs:56-76`](../../Models/Port/Mst_MajorGroup.cs#L56-L76) |

### 1.3. Cấu trúc Bảng CSDL (`Mst_MajorGroup`)

Bảng dữ liệu vật lý `Mst_MajorGroup` gồm đúng 7 cột:

| Tên cột | Kiểu EF Core | Khóa | Cho phép NULL | Ghi chú & Ý nghĩa |
|---|---|---|---|---|
| `MajorGroupCode` | `string` | **PK** | Không | Mã nhóm chuyên ngành (ví dụ: `MG01`, `MG02`). Khóa chính (khóa draft). |
| `NetworkID` | `string?` | | Có | Mã mạng nghiệp vụ. Ở đích lấy cấu hình `Biz_NetworkID`. |
| `MajorGroupName` | `string?` | | Có | Tên nhóm chuyên ngành đào tạo (Công nghệ thông tin, Cơ khí...). |
| `MajorGroupDesc` | `string?` | | Có | Mô tả thông tin nhóm chuyên ngành. |
| `FlagActive` | `string?` | | Có | Cờ trạng thái hoạt động: `"1"` (Đang hoạt động), `"0"` (Ngừng hoạt động). |
| `LogLUDTimeUTC` | `DateTime?` | | Có | Thời điểm cập nhật cuối cùng theo chuẩn UTC. |
| `LogLUBy` | `string?` | | Có | Người thực hiện cập nhật cuối cùng. |

**Dữ liệu mẫu khởi tạo (Seeder - `Mst_MajorGroupSeeder`, Idempotent)**:
- Gồm 6 bản ghi demo: `MG01` (CNTT, '1'), `MG02` (Cơ khí, '1'), `MG03` (ĐTVT, '1'), `MG04` (KTĐN, '1'), `MG05` (QTKD, '1'), `MG06` (Ngành cũ ngừng, '0').
- Toàn bộ bản ghi gán `NetworkID = "DEMO"`, `LogLUBy = "seed"`.

---

## 2. Đặc tả Chi tiết Từng Endpoint API

---

### 2.1. Search (#170)

Tìm kiếm danh mục nhóm chuyên ngành phân trang theo từ khóa và trạng thái hoạt động.

- **Route**: `POST /api/mst_majorgroup/search`
- **Controller Action**: [`Mst_MajorGroupControllerPort.Search`](../../Controllers/Port/Mst_MajorGroupControllerPort.cs#L74)
- **Tham chiếu nguồn**: Gate `Mst_MajorGroupController.cs:25-105` → API `MstMajorGroupController.cs:17-88` → Biz `MasterData.cs:38887-39042`

#### Bảng tham số Request

Payload JSON gửi qua Request Body (`MstMajorGroupSearchReq`), hỗ trợ cả trường hợp body rỗng:

| Tên tham số | Kiểu dữ liệu | Bắt buộc | Mặc định | Ý nghĩa & Quy tắc |
|---|---|---|---|---|
| `OrgID` | `string?` | Không | `null` | Nhận tại tham số nhưng hàm `strWhereClause_Mst_MajorGroup` (`:171-186`) **không lọc theo OrgID**. |
| `KeyWord` | `string?` | Không | `null` | Từ khóa tìm kiếm theo tên nhóm chuyên ngành (`MajorGroupName LIKE '%kw%'`, `gate :178`). **Không tìm trên `MajorGroupCode` hay `MajorGroupDesc`**. |
| `FlagActive` | `string?` | Không | `null` | Cờ hoạt động, **áp dụng toán tử so sánh bằng tuyệt đối '='** (`gate :182`). Không tách dấu phẩy. |
| `Ft_PageIndex` | `int` | Không | `0` | Chỉ số trang cần lấy (0-based). |
| `Ft_PageSize` | `int` | Không | `10` | Số dòng trên mỗi trang. Nếu `<= 0` trả về mảng rỗng `DataList = []` (`gate :80-90`). |

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Xác định vị trí bản ghi: `start = (long)pageIndex * pageSize`.
2. Mệnh đề lọc qua `GetXAsync`:
   - **KeyWord**: `EF.Functions.Like((x.MajorGroupName ?? "").ToLower(), kw, PortQuery.Esc)`.
   - **FlagActive**: `EF.Functions.Like((x.FlagActive ?? "").ToLower(), fa, PortQuery.Esc)` (toán tử bằng tuyệt đối không wildcard).
3. Draft sắp theo `mmg.MajorGroupCode ASC` (`MasterData.cs:38949`), đếm tổng `itemCount = await ordered.CountAsync()` (`:38952`).
4. Trích xuất trang: Nếu `pageSize <= 0` hoặc `recordStart < 0` trả về danh sách rỗng (bài học A.12); nếu `pageSize > 0` gọi `.Skip((int)start).Take((int)pageSize)`.
5. Select cuối theo `t.MyIdxSeq ASC` (`MasterData.cs:38988`), trả về đúng 8 trường: `MyIdxSeq` (0-based) + 7 cột model nguồn.
6. Tính `PageCount`: Nếu `rows.Count == 0`, `PageCount = 0`; ngược lại `PageCount = ceil(itemCount / ps)` (`gate :83-87`).

#### Bảng mã lỗi
Endpoint không có mã lỗi nghiệp vụ riêng; ngoại lệ chưa bắt được chuyển thành HTTP 400 qua `PortExceptionFilter`.

#### Ví dụ Request / Response thực tế (từ lần chạy VERIFY #170 / #171)
- **Tìm kiếm mặc định (`{}`)**:
  - Request: `POST /api/mst_majorgroup/search` kèm `{}`
  - Response (HTTP 200): `ItemCount = 6`, `PageCount = 1`, `DataList` đủ 6 dòng (`MG01` đến `MG06`).
- **Lọc theo trạng thái `FlagActive = "1"`**:
  - Request: `{"FlagActive": "1"}`
  - Response: `ItemCount = 5`, `DataList` chứa 5 dòng active (`MG01` đến `MG05`).
- **Lọc theo trạng thái `FlagActive = "0"`**:
  - Request: `{"FlagActive": "0"}`
  - Response: `ItemCount = 1`, `DataList` chứa 1 dòng `MG06` với `MyIdxSeq = 0`.

---

### 2.2. GetAllActive (#171)

Lấy toàn bộ danh sách các nhóm chuyên ngành đang hoạt động (`FlagActive = '1'`).

- **Route**: `POST /api/mst_majorgroup/getallactive`
- **Controller Action**: [`Mst_MajorGroupControllerPort.GetAllActive`](../../Controllers/Port/Mst_MajorGroupControllerPort.cs#L100)
- **Tham chiếu nguồn**: Gate `Mst_MajorGroupController.cs:107-167` → API `MstMajorGroupController.cs:17-88` → Biz `MasterData.cs:39044-39172`

#### Bảng tham số Request
Endpoint không nhận tham số Body (`gate :107`).

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Lọc `FlagActive = "1"` cố định với toán tử `=` (`gate :127-130, :195`), không có `KeyWord`, không lọc `OrgID`/`NetworkID`.
2. Tái sử dụng `GetXAsync` với `recordStart = 0`, `recordCount = 123456000` (`gate :142-143`).
3. Sắp xếp theo `MajorGroupCode ASC`, `MyIdxSeq` bắt đầu từ 0.
4. Trả về mảng phẳng trần các thực thể (8 trường gồm `MyIdxSeq` + 7 cột model). Khi không có dữ liệu trả về `Data = null` (`gate :126, :149-154`).

#### Ví dụ Request / Response thực tế (từ lần chạy VERIFY #171)
- Request: `POST /api/mst_majorgroup/getallactive`
- Response (HTTP 200):
  ```json
  {
    "Success": true,
    "Data": [
      { "MyIdxSeq": 0, "MajorGroupCode": "MG01", "NetworkID": "DEMO", "MajorGroupName": "Cong nghe thong tin", "MajorGroupDesc": "Mo ta MG01", "FlagActive": "1", "LogLUDTimeUTC": "2026-10-10T10:00:00Z", "LogLUBy": "seed" },
      { "MyIdxSeq": 1, "MajorGroupCode": "MG02", "NetworkID": "DEMO", "MajorGroupName": "Co khi dong luc", "MajorGroupDesc": "Mo ta MG02", "FlagActive": "1", "LogLUDTimeUTC": "2026-10-10T10:00:00Z", "LogLUBy": "seed" },
      { "MyIdxSeq": 2, "MajorGroupCode": "MG03", "NetworkID": "DEMO", "MajorGroupName": "Dien tu vien thong", "MajorGroupDesc": "Mo ta MG03", "FlagActive": "1", "LogLUDTimeUTC": "2026-10-10T10:00:00Z", "LogLUBy": "seed" },
      { "MyIdxSeq": 3, "MajorGroupCode": "MG04", "NetworkID": "DEMO", "MajorGroupName": "Kinh te doi ngoai", "MajorGroupDesc": "Mo ta MG04", "FlagActive": "1", "LogLUDTimeUTC": "2026-10-10T10:00:00Z", "LogLUBy": "seed" },
      { "MyIdxSeq": 4, "MajorGroupCode": "MG05", "NetworkID": "DEMO", "MajorGroupName": "Quan tri kinh doanh", "MajorGroupDesc": "Mo ta MG05", "FlagActive": "1", "LogLUDTimeUTC": "2026-10-10T10:00:00Z", "LogLUBy": "seed" }
    ]
  }
  ```
  *(Bản ghi `MG06` có `FlagActive = '0'` bị loại bỏ đúng như nguồn).*

---

## 3. Khác biệt so với Nguồn & GAP

1. **Khóa chính & DDL CSDL**: Nguồn không có DDL tạo bảng `Mst_MajorGroup`. Khóa chính `MajorGroupCode` là khóa draft, kiểu cột cần xác nhận thêm khi có schema production.
2. **KeyWord chỉ tìm trên MajorGroupName**: Nguồn chỉ lọc trên `MajorGroupName`, không lọc `MajorGroupCode` hay `MajorGroupDesc`.
3. **Toán tử lọc FlagActive**: Sử dụng toán tử `=` nguyên giá trị (không tách dấu phẩy), khác với một số bảng dùng `IN`.
4. **Phân quyền & Log**: Bỏ qua `CheckGatewayAuthentication`, `CheckAuthorize` và ghi log `RequestLog`/`ReturnLog`.

---

## 4. Nhật ký Thay đổi

| Hạng mục (#) | Action / Endpoint | Commit Hash | Ngày hoàn thành | Ghi chú |
|---|---|---|---|---|
| #170 | `Search` | `2421a22` | 2026-10-16 | Khởi tạo bảng `Mst_MajorGroup`, seeder demo và endpoint tìm kiếm phân trang |
| #171 | `GetAllActive` | `523031b` | 2026-10-16 | Endpoint lấy danh sách nhóm chuyên ngành đang hoạt động (mảng trần) |
