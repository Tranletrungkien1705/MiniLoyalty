# Tham chiếu API Mst_GenderController (Port 1-1)

| Thông tin | Giá trị |
|---|---|
| **Phạm vi** | Tài liệu tham chiếu chi tiết 2 endpoint đã port 1-1 của Mst_GenderController (#154, #155) |
| **Cập nhật** | 2026-10-16 |
| **Phiên bản mã** | `79657b7` |
| **Trạng thái** | Nháp — chờ review |

> [!NOTE]
> Toàn bộ các endpoint thuộc controller này áp dụng chuẩn hóa JSON PascalCase tự động qua `PortJsonResultFilter` và xử lý ngoại lệ tập trung qua `PortExceptionFilter` (chuyển đổi ngoại lệ chưa bắt thành HTTP 400 `PortResult.Fail`). Chi tiết xem tại [Kiến trúc Hạ tầng Dùng chung](../kien-truc/HA_TANG_CHUNG.md).

---

## 1. Tóm tắt Nghiệp vụ & Cấu trúc Dữ liệu

### 1.1. Mục đích Nghiệp vụ
API `Mst_GenderController` quản lý danh mục giới tính (`Mst_Gender`) trong hệ thống MiniLoyalty. Module này cung cấp các chức năng tìm kiếm phân trang theo từ khóa và danh sách cờ hoạt động (toán tử `IN`), và lấy toàn bộ danh sách các giới tính đang hoạt động phục vụ thông tin hồ sơ nhân sự và khách hàng hội viên.

### 1.2. Bảng Đối chiếu Nguồn Tra cứu (Source Code Mapping)

| Thành phần | Đường dẫn tập tin nguồn (`2022.7.Loyalty`) | Tập tin đích tương ứng (`MiniLoyalty`) |
|---|---|---|
| **Gate Controller** | `13.ClientGate\V10\idn.Loyalty.WebAPI\Controllers\Mst_GenderController.cs:25-208` | [`Controllers/Port/Mst_GenderControllerPort.cs`](../../Controllers/Port/Mst_GenderControllerPort.cs#L61) |
| **API Backend** | `11.BackEnd\V10\idn.Loyalty.Biz.Web\Controllers\MstGenderController.cs:20-89` | Tích hợp trực tiếp vào [`Controllers/Port/Mst_GenderControllerPort.cs`](../../Controllers/Port/Mst_GenderControllerPort.cs#L61) |
| **Nghiệp vụ (Biz)** | `11.BackEnd\V10\idn.Loyalty.Biz\MasterData.cs` (Get `:29770-29940`; GetX `:29942-30095`) | Tích hợp trực tiếp vào [`Controllers/Port/Mst_GenderControllerPort.cs`](../../Controllers/Port/Mst_GenderControllerPort.cs#L112-L164) |
| **Entity Model** | `12.Dev.Common\idn.Loyalty.Common\Models\HRM\Mst_Gender.cs:9-18` | [`Models/Port/Mst_Gender.cs:31-40`](../../Models/Port/Mst_Gender.cs#L31-L40) |
| **Cấu hình EF** | Khóa draft `GenderCode` (không có DDL nguồn) | [`Models/Port/Mst_Gender.cs:43-51`](../../Models/Port/Mst_Gender.cs#L43-L51) |
| **Seeder Demo** | `Order => 36` (danh mục `Mst_*` dùng 10–99) | [`Models/Port/Mst_Gender.cs:59-75`](../../Models/Port/Mst_Gender.cs#L59-L75) |

### 1.3. Cấu trúc Bảng CSDL (`Mst_Gender`)

Bảng dữ liệu vật lý `Mst_Gender` gồm đúng 7 cột:

| Tên cột | Kiểu EF Core | Khóa | Cho phép NULL | Ghi chú & Ý nghĩa |
|---|---|---|---|---|
| `GenderCode` | `string` | **PK** | Không | Mã giới tính (ví dụ: `GN01`, `GN02`). Khóa chính (khóa draft). |
| `NetworkID` | `string?` | | Có | Mã mạng nghiệp vụ. Ở đích lấy từ cấu hình `Biz_NetworkID`. |
| `OrgID` | `string?` | | Có | Mã tổ chức. Bảng có cột nhưng hàm lọc nguồn không sử dụng để lọc. |
| `Gender` | `string?` | | Có | Tên giới tính (cột tên là `Gender`, không phải `GenderName`). |
| `FlagActive` | `string?` | | Có | Cờ trạng thái hoạt động: `"1"` (Đang hoạt động), `"0"` (Ngừng hoạt động). |
| `LogLUDTimeUTC` | `DateTime?` | | Có | Thời điểm cập nhật cuối cùng theo chuẩn UTC. |
| `LogLUBy` | `string?` | | Có | Người thực hiện cập nhật cuối cùng. |

**Dữ liệu mẫu khởi tạo (Seeder - `Mst_GenderSeeder`, Idempotent)**:
- Gồm 3 bản ghi demo: `GN01` (Nam, FlagActive `'1'`), `GN02` (Nữ, FlagActive `'1'`), `GN03` (Khác, FlagActive `'0'`).
- Toàn bộ bản ghi gán `NetworkID = "DEMO"`, `OrgID = "4020320001"`, `LogLUBy = "seed"`.

---

## 2. Đặc tả Chi tiết Từng Endpoint API

---

### 2.1. Search (#154)

Tìm kiếm danh mục giới tính phân trang theo từ khóa và trạng thái hoạt động.

- **Route**: `POST /api/mst_gender/search`
- **Controller Action**: [`Mst_GenderControllerPort.Search`](../../Controllers/Port/Mst_GenderControllerPort.cs#L70)
- **Tham chiếu nguồn**: Gate `Mst_GenderController.cs:25-105` → API `MstGenderController.cs:20-89` → Biz `MasterData.cs:29942-30095`

#### Bảng tham số Request

Payload JSON gửi qua Request Body (`MstGenderSearchReq`), hỗ trợ cả trường hợp body rỗng:

| Tên tham số | Kiểu dữ liệu | Bắt buộc | Mặc định | Ý nghĩa & Quy tắc |
|---|---|---|---|---|
| `OrgID` | `string?` | Không | `null` | Nhận tại tham số nhưng hàm `strWhereClause_Mst_Gender` (`:171-195`) không lọc theo `OrgID`. |
| `KeyWord` | `string?` | Không | `null` | Từ khóa tìm kiếm theo tên giới tính (`Gender LIKE '%kw%'`, `gate :178`). |
| `FlagActive` | `string?` | Không | `null` | Danh sách trạng thái phân cách bởi dấu phẩy, áp dụng toán tử `IN ('1','0')` (`gate :184-192`). Nhánh `=` (`:180-183`) bị comment ở nguồn. |
| `Ft_PageIndex` | `int` | Không | `0` | Chỉ số trang cần lấy (0-based). |
| `Ft_PageSize` | `int` | Không | `10` | Số dòng trên mỗi trang. Nếu `<= 0` trả về mảng rỗng `DataList = []` (`gate :81-89`). |

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Xác định vị trí bản ghi: `start = (long)pageIndex * pageSize`.
2. Mệnh đề lọc qua `GetXAsync`:
   - **KeyWord**: `EF.Functions.Like((x.Gender ?? "").ToLower(), PortQuery.Contains(keyWord), PortQuery.Esc)`.
   - **FlagActive**: Tách dấu phẩy, chuẩn hóa chữ thường và lọc qua `IN` (`ds.Contains((x.FlagActive ?? "").ToLower())`).
3. Sắp xếp thứ tự bản ghi: Draft sắp theo `mg.GenderCode ASC` (`MasterData.cs:30002`), select cuối sắp theo `t.MyIdxSeq ASC` (`MasterData.cs:30041`).
4. Đếm tổng số bản ghi: `itemCount = await ordered.CountAsync()` (tương ứng `MyCount` trong SQL nguồn `:30006`).
5. Trích xuất trang: Nếu `pageSize <= 0` hoặc `recordStart < 0` trả về danh sách rỗng (bài học A.12); nếu `pageSize > 0` gọi `.Skip((int)start).Take((int)pageSize)`.
6. Gán `MyIdxSeq = start + i` (0-based identity).
7. Tính `PageCount`: Nếu `rows.Count == 0`, `PageCount = 0`; ngược lại `PageCount = ceil(itemCount / ps)`.

#### Bảng mã lỗi
Endpoint không có mã lỗi nghiệp vụ riêng; ngoại lệ chưa bắt được chuyển thành HTTP 400 qua `PortExceptionFilter`.

#### Ví dụ Request / Response thực tế (từ lần chạy VERIFY #154 / #155)
- **Tìm kiếm mặc định (`{}`)**:
  - Request: `POST /api/mst_gender/search` kèm `{}`
  - Response (HTTP 200):
    ```json
    {
      "Success": true,
      "Data": {
        "DataList": [
          { "MyIdxSeq": 0, "GenderCode": "GN01", "NetworkID": "DEMO", "OrgID": "4020320001", "Gender": "Nam", "FlagActive": "1", "LogLUDTimeUTC": null, "LogLUBy": "seed" },
          { "MyIdxSeq": 1, "GenderCode": "GN02", "NetworkID": "DEMO", "OrgID": "4020320001", "Gender": "Nữ", "FlagActive": "1", "LogLUDTimeUTC": null, "LogLUBy": "seed" },
          { "MyIdxSeq": 2, "GenderCode": "GN03", "NetworkID": "DEMO", "OrgID": "4020320001", "Gender": "Khác", "FlagActive": "0", "LogLUDTimeUTC": null, "LogLUBy": "seed" }
        ],
        "ItemCount": 3,
        "PageCount": 1,
        "PageIndex": 0,
        "PageSize": 10
      }
    }
    ```

---

### 2.2. GetAllActive (#155)

Lấy toàn bộ danh sách các giới tính đang hoạt động (`FlagActive = '1'`).

- **Route**: `POST /api/mst_gender/getallactive`
- **Controller Action**: [`Mst_GenderControllerPort.GetAllActive`](../../Controllers/Port/Mst_GenderControllerPort.cs#L98)
- **Tham chiếu nguồn**: Gate `Mst_GenderController.cs:107-167` → API `MstGenderController.cs:20-89` → Biz `MasterData.cs:29670-30095`

#### Bảng tham số Request
Endpoint không nhận tham số Body (`gate :107`).

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Tái sử dụng `GetXAsync` với tham số cố định: `FlagActive = "1"`, `recordStart = 0`, `recordCount = 123456000` (bài học A.3).
2. Không nhận `KeyWord`, không lọc theo `OrgID` (hàm lọc `strWhereClause_Mst_Gender_Active` `:197-208` không dùng OrgID).
3. Trả về mảng phẳng trần các thực thể kèm `MyIdxSeq`. Nếu không có dữ liệu trả về `Data = null` (nguồn gate `:126, 149-154`).

#### Ví dụ Request / Response thực tế (từ lần chạy VERIFY #155)
- Request: `POST /api/mst_gender/getallactive`
- Response (HTTP 200):
  ```json
  {
    "Success": true,
    "Data": [
      { "MyIdxSeq": 0, "GenderCode": "GN01", "NetworkID": "DEMO", "OrgID": "4020320001", "Gender": "Nam", "FlagActive": "1", "LogLUDTimeUTC": null, "LogLUBy": "seed" },
      { "MyIdxSeq": 1, "GenderCode": "GN02", "NetworkID": "DEMO", "OrgID": "4020320001", "Gender": "Nữ", "FlagActive": "1", "LogLUDTimeUTC": null, "LogLUBy": "seed" }
    ]
  }
  ```

---

## 3. Khác biệt so với Nguồn & GAP

1. **Khóa chính & DDL CSDL**: Nguồn không có DDL tạo bảng `Mst_Gender`. Khóa chính `GenderCode` là khóa draft, kiểu cột cần xác nhận thêm khi có schema production.
2. **Tên cột Gender**: Cột tên là `Gender`, không phải `GenderName`.
3. **Toán tử lọc FlagActive**: Dùng toán tử `IN` tách dấu phẩy (dòng `=` bị comment trong nguồn), khác với các bảng dùng `=` như `Mst_EthnicCategory`.
4. **Phân quyền & Log**: Bỏ qua `CheckGatewayAuthentication`, `CheckAuthorize` và ghi log `RequestLog`/`ReturnLog`.

---

## 4. Nhật ký Thay đổi

| Hạng mục (#) | Action / Endpoint | Commit Hash | Ngày hoàn thành | Ghi chú |
|---|---|---|---|---|
| #154 | `Search` | `b8d304e` | 2026-10-16 | Khởi tạo bảng `Mst_Gender`, seeder demo và endpoint tìm kiếm phân trang |
| #155 | `GetAllActive` | `eec3343` | 2026-10-16 | Endpoint lấy danh sách giới tính đang hoạt động (mảng trần) |
