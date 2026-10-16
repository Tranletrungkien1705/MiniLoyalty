# Tham chiếu API Mst_EthnicCategoryController (Port 1-1)

| Thông tin | Giá trị |
|---|---|
| **Phạm vi** | Tài liệu tham chiếu chi tiết 2 endpoint đã port 1-1 của Mst_EthnicCategoryController (#149, #150) |
| **Cập nhật** | 2026-10-16 |
| **Phiên bản mã** | `ed28f8f` |
| **Trạng thái** | Nháp — chờ review |

> [!NOTE]
> Toàn bộ các endpoint thuộc controller này áp dụng chuẩn hóa JSON PascalCase tự động qua `PortJsonResultFilter` và xử lý ngoại lệ tập trung qua `PortExceptionFilter` (chuyển đổi ngoại lệ chưa bắt thành HTTP 400 `PortResult.Fail`). Chi tiết xem tại [Kiến trúc Hạ tầng Dùng chung](../kien-truc/HA_TANG_CHUNG.md).

---

## 1. Tóm tắt Nghiệp vụ & Cấu trúc Dữ liệu

### 1.1. Mục đích Nghiệp vụ
API `Mst_EthnicCategoryController` quản lý danh mục thành phần dân tộc (`Mst_EthnicCategory`) trong hệ thống MiniLoyalty. Module cung cấp các chức năng tìm kiếm phân trang theo từ khóa và trạng thái hoạt động (toán tử so sánh bằng tuyệt đối `=`), và lấy toàn bộ danh sách các dân tộc đang hoạt động phục vụ cho thông tin hồ sơ khách hàng và nhân sự.

### 1.2. Bảng Đối chiếu Nguồn Tra cứu (Source Code Mapping)

| Thành phần | Đường dẫn tập tin nguồn (`2022.7.Loyalty`) | Tập tin đích tương ứng (`MiniLoyalty`) |
|---|---|---|
| **Gate Controller** | `13.ClientGate\V10\idn.Loyalty.WebAPI\Controllers\Mst_EthnicCategoryController.cs:25-199` | [`Controllers/Port/Mst_EthnicCategoryControllerPort.cs`](../../Controllers/Port/Mst_EthnicCategoryControllerPort.cs) |
| **API Backend** | `11.BackEnd\V10\idn.Loyalty.Biz.Web\Controllers\MstEthnicCategoryController.cs:17-88` | Tích hợp trực tiếp vào [`Controllers/Port/Mst_EthnicCategoryControllerPort.cs`](../../Controllers/Port/Mst_EthnicCategoryControllerPort.cs) |
| **Nghiệp vụ (Biz)** | `11.BackEnd\V10\idn.Loyalty.Biz\MasterData.cs` (Get `:41678-41806`; GetX `:41521-41676`) | Tích hợp trực tiếp vào [`Controllers/Port/Mst_EthnicCategoryControllerPort.cs`](../../Controllers/Port/Mst_EthnicCategoryControllerPort.cs) |
| **Entity Model** | `12.Dev.Common\idn.Loyalty.Common\Models\HRM\Mst_EthnicCategory.cs:9-18` | [`Models/Port/Mst_EthnicCategory.cs:29-38`](../../Models/Port/Mst_EthnicCategory.cs#L29-L38) |
| **Cấu hình EF** | Khóa draft `EthnicCode` (không có DDL nguồn) | [`Models/Port/Mst_EthnicCategory.cs:41-49`](../../Models/Port/Mst_EthnicCategory.cs#L41-L49) |
| **Seeder Demo** | `Order => 34` (danh mục `Mst_*` dùng 10–99) | [`Models/Port/Mst_EthnicCategory.cs:56-72`](../../Models/Port/Mst_EthnicCategory.cs#L56-L72) |

### 1.3. Cấu trúc Bảng CSDL (`Mst_EthnicCategory`)

Bảng dữ liệu vật lý `Mst_EthnicCategory` gồm đúng 7 cột:

| Tên cột | Kiểu EF Core | Khóa | Cho phép NULL | Ghi chú & Ý nghĩa |
|---|---|---|---|---|
| `EthnicCode` | `string` | **PK** | Không | Mã dân tộc (ví dụ: `DT01`, `DT02`). Khóa chính (khóa draft). |
| `NetworkID` | `string?` | | Có | Mã mạng nghiệp vụ. Ở đích lấy cấu hình `Biz_NetworkID`. |
| `EthnicName` | `string?` | | Có | Tên dân tộc (chuẩn hóa qua `StdStr`). |
| `EthnicDesc` | `string?` | | Có | Mô tả thông tin dân tộc. |
| `FlagActive` | `string?` | | Có | Cờ trạng thái hoạt động: `"1"` (Đang hoạt động), `"0"` (Ngừng hoạt động). |
| `LogLUDTimeUTC` | `DateTime?` | | Có | Thời điểm cập nhật cuối cùng theo chuẩn UTC. |
| `LogLUBy` | `string?` | | Có | Người thực hiện cập nhật cuối cùng. |

**Dữ liệu mẫu khởi tạo (Seeder - `Mst_EthnicCategorySeeder`, Idempotent)**:
- Gồm 3 bản ghi demo: `DT01` (Kinh, FlagActive `'1'`), `DT02` (Tày, FlagActive `'1'`), `DT03` (Khác, FlagActive `'0'`).
- Toàn bộ bản ghi gán `NetworkID = "DEMO"`, `LogLUBy = "seed"`.

---

## 2. Đặc tả Chi tiết Từng Endpoint API

---

### 2.1. Search (#149)

Tìm kiếm danh mục dân tộc phân trang theo từ khóa tên dân tộc và trạng thái hoạt động.

- **Route**: `POST /api/mst_ethniccategory/search`
- **Controller Action**: [`Mst_EthnicCategoryControllerPort.Search`](../../Controllers/Port/Mst_EthnicCategoryControllerPort.cs#L71)
- **Tham chiếu nguồn**: Gate `Mst_EthnicCategoryController.cs:25-105` → API `MstEthnicCategoryController.cs:17-88` → Biz `MasterData.cs:41521-41676`

#### Bảng tham số Request

Payload JSON gửi qua Request Body (`MstEthnicCategorySearchReq`), hỗ trợ cả trường hợp body rỗng:

| Tên tham số | Kiểu dữ liệu | Bắt buộc | Mặc định | Ý nghĩa & Quy tắc |
|---|---|---|---|---|
| `OrgID` | `string?` | Không | `null` | Nhận tại tham số hàm nhưng hàm `strWhereClause_Mst_EthnicCategory` (`:171-186`) **không sử dụng OrgID** trong mệnh đề WHERE → Không lọc. |
| `KeyWord` | `string?` | Không | `null` | Từ khóa tìm kiếm theo tên dân tộc (`EthnicName LIKE '%kw%'`, `gate :178`). |
| `FlagActive` | `string?` | Không | `null` | Trạng thái hoạt động, **áp dụng toán tử so sánh bằng tuyệt đối '='** (`gate :182`). Không tách dấu phẩy. |
| `Ft_PageIndex` | `int` | Không | `0` | Chỉ số trang cần lấy (0-based). |
| `Ft_PageSize` | `int` | Không | `10` | Số dòng trên mỗi trang. Nếu `<= 0` trả về mảng rỗng `DataList = []` (`gate :80-90`). |

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Xác định vị trí bản ghi: `start = (long)pageIndex * pageSize`.
2. Mệnh đề lọc qua `GetXAsync`:
   - **KeyWord**: `EF.Functions.Like((x.EthnicName ?? "").ToLower(), PortQuery.Contains(keyWord), PortQuery.Esc)`.
   - **FlagActive**: `(x.FlagActive ?? "").ToLower() == flagActive.ToLower()`.
3. Sắp xếp thứ tự bản ghi: Phân trang theo `mec.EthnicCode ASC` (`MasterData.cs:41583`), select cuối sắp theo `t.MyIdxSeq ASC` (`MasterData.cs:41622`).
4. Đếm tổng số bản ghi: `itemCount = await ordered.CountAsync()`.
5. Trích xuất trang: Nếu `pageSize <= 0` trả về danh sách rỗng; nếu `pageSize > 0` gọi `.Skip((int)start).Take((int)pageSize)`.
6. Gán `MyIdxSeq = start + i` (0-based identity).
7. Tính `PageCount`: Nếu `rows.Count == 0`, `PageCount = 0`; ngược lại `PageCount = ceil(itemCount / ps)`.

#### Bảng mã lỗi
Endpoint luôn trả về HTTP 200.

#### Ví dụ Request / Response thực tế (từ lần chạy VERIFY #149)
- **Tìm kiếm mặc định (`{}`)**:
  - Request: `POST /api/mst_ethniccategory/search` kèm `{}`
  - Response (HTTP 200):
    ```json
    {
      "Success": true,
      "Data": {
        "DataList": [
          { "MyIdxSeq": 0, "EthnicCode": "DT01", "EthnicName": "Kinh", "FlagActive": "1" },
          { "MyIdxSeq": 1, "EthnicCode": "DT02", "EthnicName": "Tày", "FlagActive": "1" },
          { "MyIdxSeq": 2, "EthnicCode": "DT03", "EthnicName": "Khác", "FlagActive": "0" }
        ],
        "ItemCount": 3,
        "PageCount": 1,
        "PageIndex": 0,
        "PageSize": 10
      }
    }
    ```

---

### 2.2. GetAllActive (#150)

Lấy toàn bộ danh sách các dân tộc đang hoạt động (`FlagActive = '1'`).

- **Route**: `POST /api/mst_ethniccategory/getallactive`
- **Controller Action**: [`Mst_EthnicCategoryControllerPort.GetAllActive`](../../Controllers/Port/Mst_EthnicCategoryControllerPort.cs#L98)
- **Tham chiếu nguồn**: Gate `Mst_EthnicCategoryController.cs:107-167` → API `MstEthnicCategoryController.cs:17-88` → Biz `MasterData.cs:41521-41676`

#### Bảng tham số Request
Endpoint không nhận tham số Body (`gate :107`).

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Thiết lập tham số phân trang: `Ft_RecordStart = 0`, `Ft_RecordCount = 123456000` (`gate :142-143`).
2. Mệnh đề lọc: `FlagActive = "1"` (toán tử `=`, `gate :127-130, :195`), không lọc `OrgID`.
3. Trả về mảng phẳng trần các thực thể `Mst_EthnicCategory` kèm `MyIdxSeq`. Nếu không có dữ liệu trả về `Data = null` (`gate :126, :149-154`).

#### Ví dụ Request / Response thực tế (từ lần chạy VERIFY #150)
- Request: `POST /api/mst_ethniccategory/getallactive`
- Response (HTTP 200):
  ```json
  {
    "Success": true,
    "Data": [
      { "MyIdxSeq": 0, "EthnicCode": "DT01", "EthnicName": "Kinh", "FlagActive": "1" },
      { "MyIdxSeq": 1, "EthnicCode": "DT02", "EthnicName": "Tày", "FlagActive": "1" }
    ]
  }
  ```

---

## 3. Khác biệt so với Nguồn & GAP

1. **Khóa chính & DDL CSDL**: Nguồn không có tập tin DDL tạo bảng trực tiếp. Khóa chính `EthnicCode` là khóa draft, kiểu dữ liệu cần tiếp tục đối chiếu khi có thêm thông tin từ hệ thống nghiệp vụ nhân sự.
2. **Tham số OrgID không lọc**: Cả hai action `Search` và `GetAllActive` đều nhận tham số `OrgID` ở tầng Gate nhưng mệnh đề `strWhereClause` của nguồn hoàn toàn không sử dụng `OrgID`. MiniLoyalty giữ đúng hành vi nguồn (không lọc theo OrgID).
3. **Phân quyền & Kiểm soát truy cập**: Bỏ qua các kiểm tra phân quyền `CheckGatewayAuthentication`, `CheckAuthorize` và ghi log `RequestLog`/`ReturnLog`.

---

## 4. Nhật ký Thay đổi

| Hạng mục (#) | Action / Endpoint | Commit Hash | Ngày hoàn thành | Ghi chú |
|---|---|---|---|---|
| #149 | `Search` | `5b9504a` | 2026-10-16 | Khởi tạo bảng `Mst_EthnicCategory`, seeder demo và endpoint tìm kiếm phân trang |
| #150 | `GetAllActive` | `cae436a` | 2026-10-16 | Endpoint lấy danh sách dân tộc đang hoạt động (mảng trần) |
