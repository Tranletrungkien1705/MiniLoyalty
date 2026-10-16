# Tham chiếu API Mst_CertificateGroupController (Port 1-1)

| Thông tin | Giá trị |
|---|---|
| **Phạm vi** | Tài liệu tham chiếu chi tiết 2 endpoint đã port 1-1 của Mst_CertificateGroupController (#107, #108) |
| **Cập nhật** | 2026-10-16 |
| **Phiên bản mã** | `ed28f8f` |
| **Trạng thái** | Nháp — chờ review |

> [!NOTE]
> Toàn bộ các endpoint thuộc controller này áp dụng chuẩn hóa JSON PascalCase tự động qua `PortJsonResultFilter` và xử lý ngoại lệ tập trung qua `PortExceptionFilter` (chuyển đổi ngoại lệ chưa bắt thành HTTP 400 `PortResult.Fail`). Chi tiết xem tại [Kiến trúc Hạ tầng Dùng chung](../kien-truc/HA_TANG_CHUNG.md).

---

## 1. Tóm tắt Nghiệp vụ & Cấu trúc Dữ liệu

### 1.1. Mục đích Nghiệp vụ
API `Mst_CertificateGroupController` quản lý danh mục nhóm chứng chỉ nhân sự (`Mst_CertificateGroup`) trong hệ thống MiniLoyalty. Module cung cấp các chức năng tìm kiếm phân trang theo từ khóa và trạng thái hoạt động (toán tử so sánh bằng tuyệt đối `=`), và lấy toàn bộ danh sách các nhóm chứng chỉ đang hoạt động phục vụ cho các phân hệ nghiệp vụ nhân sự và đào tạo.

### 1.2. Bảng Đối chiếu Nguồn Tra cứu (Source Code Mapping)

| Thành phần | Đường dẫn tập tin nguồn (`2022.7.Loyalty`) | Tập tin đích tương ứng (`MiniLoyalty`) |
|---|---|---|
| **Gate Controller** | `13.ClientGate\V10\idn.Loyalty.WebAPI\Controllers\Mst_CertificateGroupController.cs:25-199` | [`Controllers/Port/Mst_CertificateGroupControllerPort.cs`](../../Controllers/Port/Mst_CertificateGroupControllerPort.cs) |
| **API Backend** | `11.BackEnd\V10\idn.Loyalty.Biz.Web\Controllers\MstCertificateGroupController.cs:17-88` | Tích hợp trực tiếp vào [`Controllers/Port/Mst_CertificateGroupControllerPort.cs`](../../Controllers/Port/Mst_CertificateGroupControllerPort.cs) |
| **Nghiệp vụ (Biz)** | `11.BackEnd\V10\idn.Loyalty.Biz\MasterData.cs` (Get `:44635-44763`; GetX `:44478-44633`) | Tích hợp trực tiếp vào [`Controllers/Port/Mst_CertificateGroupControllerPort.cs`](../../Controllers/Port/Mst_CertificateGroupControllerPort.cs) |
| **Entity Model** | `12.Dev.Common\idn.Loyalty.Common\Models\HRM\Mst_CertificateGroup.cs:9-17` | [`Models/Port/Mst_CertificateGroup.cs:29-37`](../../Models/Port/Mst_CertificateGroup.cs#L29-L37) |
| **Cấu hình EF** | Khóa draft `CertificateGroupCode` (không có DDL nguồn) | [`Models/Port/Mst_CertificateGroup.cs:40-48`](../../Models/Port/Mst_CertificateGroup.cs#L40-L48) |
| **Seeder Demo** | `Order => 31` (danh mục `Mst_*` dùng 10–99) | [`Models/Port/Mst_CertificateGroup.cs:55-72`](../../Models/Port/Mst_CertificateGroup.cs#L55-L72) |

### 1.3. Cấu trúc Bảng CSDL (`Mst_CertificateGroup`)

Bảng dữ liệu vật lý `Mst_CertificateGroup` gồm đúng 6 cột:

| Tên cột | Kiểu EF Core | Khóa | Cho phép NULL | Ghi chú & Ý nghĩa |
|---|---|---|---|---|
| `CertificateGroupCode` | `string` | **PK** | Không | Mã nhóm chứng chỉ. Khóa chính (khóa draft do nguồn không có tập tin DDL, kiểu cột cần xác nhận). |
| `NetworkID` | `string?` | | Có | Mã mạng nghiệp vụ. Ở đích lấy từ cấu hình `Biz_NetworkID`. |
| `CertificateGroupName` | `string?` | | Có | Tên nhóm chứng chỉ (chuẩn hóa qua `StdStr`). |
| `FlagActive` | `string?` | | Có | Cờ trạng thái hoạt động: `"1"` (Đang hoạt động), `"0"` (Ngừng hoạt động). |
| `LogLUDTimeUTC` | `DateTime?` | | Có | Thời điểm cập nhật cuối cùng theo chuẩn UTC. |
| `LogLUBy` | `string?` | | Có | Người thực hiện cập nhật cuối cùng (mặc định gán `"seed"` hoặc `"port"`). |

**Dữ liệu mẫu khởi tạo (Seeder - `Mst_CertificateGroupSeeder`, Idempotent)**:
- Gồm 3 bản ghi demo: `CG01` (Nhóm chứng chỉ A, FlagActive `'1'`), `CG02` (Nhóm chứng chỉ B, FlagActive `'1'`), `CG03` (Kỹ thuật viên, FlagActive `'0'`).
- Toàn bộ bản ghi gán `NetworkID = "DEMO"`, `LogLUBy = "seed"`.

---

## 2. Đặc tả Chi tiết Từng Endpoint API

---

### 2.1. Search (#107)

Tìm kiếm danh mục nhóm chứng chỉ phân trang theo từ khóa và trạng thái hoạt động.

- **Route**: `POST /api/mst_certificategroup/search`
- **Controller Action**: [`Mst_CertificateGroupControllerPort.Search`](../../Controllers/Port/Mst_CertificateGroupControllerPort.cs#L68)
- **Tham chiếu nguồn**: Gate `Mst_CertificateGroupController.cs:25-105` → API `MstCertificateGroupController.cs:17-88` → Biz `MasterData.cs:44478-44633`

#### Bảng tham số Request

Payload JSON gửi qua Request Body (`MstCertificateGroupSearchReq`), hỗ trợ cả trường hợp body rỗng:

| Tên tham số | Kiểu dữ liệu | Bắt buộc | Mặc định | Ý nghĩa & Quy tắc |
|---|---|---|---|---|
| `OrgID` | `string?` | Không | `null` | Nhận tại tham số hàm (`gate :25, :55`) nhưng hàm `strWhereClause_Mst_CertificateGroup` (`:171-186`) **không sử dụng OrgID** trong mệnh đề WHERE → Không lọc theo tổ chức. |
| `KeyWord` | `string?` | Không | `null` | Từ khóa tìm kiếm theo tên nhóm chứng chỉ (`CertificateGroupName LIKE '%kw%'`, `gate :178`). |
| `FlagActive` | `string?` | Không | `null` | Trạng thái hoạt động, **áp dụng toán tử bằng tuyệt đối '='** (`gate :182`). Không tách dấu phẩy. |
| `Ft_PageIndex` | `int` | Không | `0` | Chỉ số trang cần lấy (0-based). |
| `Ft_PageSize` | `int` | Không | `10` | Số dòng trên mỗi trang. Nếu `<= 0` trả về mảng rỗng `DataList = []` (`gate :83-90`). |

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Xác định vị trí bản ghi: `start = (long)pageIndex * pageSize`.
2. Mệnh đề lọc qua `GetXAsync`:
   - **KeyWord**: `EF.Functions.Like((x.CertificateGroupName ?? "").ToLower(), PortQuery.Contains(keyWord), PortQuery.Esc)`.
   - **FlagActive**: `(x.FlagActive ?? "").ToLower() == flagActive.ToLower()`.
3. Sắp xếp thứ tự bản ghi: Phân trang theo `mcg.CertificateGroupCode ASC` (`MasterData.cs:44540`), select cuối sắp theo `t.MyIdxSeq ASC` (`MasterData.cs:44579`).
4. Đếm tổng số bản ghi: `itemCount = await ordered.CountAsync()`.
5. Trích xuất trang: Nếu `pageSize <= 0` trả về danh sách rỗng; nếu `pageSize > 0` gọi `.Skip((int)start).Take((int)pageSize)`.
6. Gán `MyIdxSeq = start + i` (0-based identity).
7. Tính `PageCount`: Nếu `rows.Count == 0`, `PageCount = 0`; ngược lại `PageCount = ceil(itemCount / ps)`.

#### Bảng mã lỗi
Endpoint luôn trả về HTTP 200.

#### Ví dụ Request / Response thực tế (từ lần chạy VERIFY #107)
- **Tìm kiếm mặc định (`{}`)**:
  - Request: `POST /api/mst_certificategroup/search` kèm `{}`
  - Response (HTTP 200):
    ```json
    {
      "Success": true,
      "Data": {
        "DataList": [
          { "MyIdxSeq": 0, "CertificateGroupCode": "CG01", "CertificateGroupName": "Nhóm chứng chỉ A", "FlagActive": "1" },
          { "MyIdxSeq": 1, "CertificateGroupCode": "CG02", "CertificateGroupName": "Nhóm chứng chỉ B", "FlagActive": "1" },
          { "MyIdxSeq": 2, "CertificateGroupCode": "CG03", "CertificateGroupName": "Kỹ thuật viên", "FlagActive": "0" }
        ],
        "ItemCount": 3,
        "PageCount": 1,
        "PageIndex": 0,
        "PageSize": 10
      }
    }
    ```

---

### 2.2. GetAllActive (#108)

Lấy toàn bộ danh sách các nhóm chứng chỉ đang hoạt động (`FlagActive = '1'`).

- **Route**: `POST /api/mst_certificategroup/getallactive`
- **Controller Action**: [`Mst_CertificateGroupControllerPort.GetAllActive`](../../Controllers/Port/Mst_CertificateGroupControllerPort.cs#L95)
- **Tham chiếu nguồn**: Gate `Mst_CertificateGroupController.cs:107-167` → API `MstCertificateGroupController.cs:17-88` → Biz `MasterData.cs:44478-44633`

#### Bảng tham số Request
Endpoint không nhận tham số Body (`gate :107`).

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Thiết lập tham số phân trang nguồn: `Ft_RecordStart = 0`, `Ft_RecordCount = 123456000` (`gate :142-143`).
2. Mệnh đề lọc: `FlagActive = "1"` (toán tử `=`, `gate :129, :195`), không lọc `OrgID`.
3. Trả về mảng phẳng trần các thực thể `Mst_CertificateGroup` kèm `MyIdxSeq`. Nếu không có dữ liệu trả về `Data = null` (`gate :126, :149-154`).

#### Ví dụ Request / Response thực tế (từ lần chạy VERIFY #108)
- Request: `POST /api/mst_certificategroup/getallactive`
- Response (HTTP 200):
  ```json
  {
    "Success": true,
    "Data": [
      { "MyIdxSeq": 0, "CertificateGroupCode": "CG01", "CertificateGroupName": "Nhóm chứng chỉ A", "FlagActive": "1" },
      { "MyIdxSeq": 1, "CertificateGroupCode": "CG02", "CertificateGroupName": "Nhóm chứng chỉ B", "FlagActive": "1" }
    ]
  }
  ```

---

## 3. Khác biệt so với Nguồn & GAP

1. **Khóa chính & DDL CSDL**: Nguồn không có tập tin DDL tạo bảng trực tiếp. Khóa chính `CertificateGroupCode` là khóa draft, kiểu dữ liệu cần tiếp tục đối chiếu khi có thêm thông tin từ hệ thống nghiệp vụ nhân sự.
2. **Tham số OrgID không lọc**: Cả hai action `Search` và `GetAllActive` đều nhận tham số `OrgID` ở tầng Gate nhưng mệnh đề `strWhereClause` của nguồn hoàn toàn không sử dụng `OrgID`. MiniLoyalty giữ đúng hành vi nguồn (không lọc theo OrgID).
3. **Phân quyền & Kiểm soát truy cập**: Bỏ qua các kiểm tra phân quyền `CheckGatewayAuthentication`, `CheckAuthorize` và ghi log `RequestLog`/`ReturnLog`.

---

## 4. Nhật ký Thay đổi

| Hạng mục (#) | Action / Endpoint | Commit Hash | Ngày hoàn thành | Ghi chú |
|---|---|---|---|---|
| #107 | `Search` | `d3b52bf` | 2026-10-16 | Khởi tạo bảng `Mst_CertificateGroup`, seeder demo và endpoint tìm kiếm phân trang |
| #108 | `GetAllActive` | `2b9ee3d` | 2026-10-16 | Endpoint lấy danh sách nhóm chứng chỉ đang hoạt động (mảng trần) |
