# Tham chiếu API Mst_CountryController (Port 1-1)

| Thông tin | Giá trị |
|---|---|
| **Phạm vi** | Tài liệu tham chiếu chi tiết 1 endpoint đã port 1-1 của Mst_CountryController (#109) |
| **Cập nhật** | 2026-10-16 |
| **Phiên bản mã** | `ed28f8f` |
| **Trạng thái** | Nháp — chờ review |

> [!NOTE]
> Toàn bộ các endpoint thuộc controller này áp dụng chuẩn hóa JSON PascalCase tự động qua `PortJsonResultFilter` và xử lý ngoại lệ tập trung qua `PortExceptionFilter` (chuyển đổi ngoại lệ chưa bắt thành HTTP 400 `PortResult.Fail`). Chi tiết xem tại [Kiến trúc Hạ tầng Dùng chung](../kien-truc/HA_TANG_CHUNG.md).

---

## 1. Tóm tắt Nghiệp vụ & Cấu trúc Dữ liệu

### 1.1. Mục đích Nghiệp vụ
API `Mst_CountryController` quản lý danh mục các quốc gia (`Mst_Country`) trong hệ thống MiniLoyalty. Module cung cấp endpoint tra cứu toàn bộ danh sách quốc gia đang hoạt động phục vụ cho các thông tin địa lý, địa chỉ khách hàng và đối tác.

### 1.2. Bảng Đối chiếu Nguồn Tra cứu (Source Code Mapping)

| Thành phần | Đường dẫn tập tin nguồn (`2022.7.Loyalty`) | Tập tin đích tương ứng (`MiniLoyalty`) |
|---|---|---|
| **Gate Controller** | `13.ClientGate\V10\idn.Loyalty.WebAPI\Controllers\Mst_CountryController.cs:25-100` | [`Controllers/Port/Mst_CountryControllerPort.cs`](../../Controllers/Port/Mst_CountryControllerPort.cs) |
| **API Backend** | `11.BackEnd\V10\idn.Loyalty.Biz.Web\Controllers\MstCountryController.cs:20-91` | Tích hợp trực tiếp vào [`Controllers/Port/Mst_CountryControllerPort.cs`](../../Controllers/Port/Mst_CountryControllerPort.cs) |
| **Nghiệp vụ (Biz)** | `11.BackEnd\V10\idn.Loyalty.Biz\Master.cs` (Get `:3298-3526`; WAS `:3987-4086`) | Tích hợp trực tiếp vào [`Controllers/Port/Mst_CountryControllerPort.cs`](../../Controllers/Port/Mst_CountryControllerPort.cs) |
| **Entity Model** | `12.Dev.Common\idn.Loyalty.Common\Models\Mst_Country.cs` (5 cột) | [`Models/Port/Mst_Country.cs:29-36`](../../Models/Port/Mst_Country.cs#L29-L36) |
| **Cấu hình EF** | Khóa chính `CountryCode` (không có DDL nguồn) | [`Models/Port/Mst_Country.cs:39-47`](../../Models/Port/Mst_Country.cs#L39-L47) |
| **Seeder Demo** | `Order => 18` (danh mục `Mst_*` dùng 10–99) | [`Models/Port/Mst_Country.cs:55-72`](../../Models/Port/Mst_Country.cs#L55-L72) |

### 1.3. Cấu trúc Bảng CSDL (`Mst_Country`)

Bảng dữ liệu vật lý `Mst_Country` gồm đúng 5 cột (đặc biệt: **KHÔNG có cột `NetworkID`** theo đúng Model nguồn):

| Tên cột | Kiểu EF Core | Khóa | Cho phép NULL | Ghi chú & Ý nghĩa |
|---|---|---|---|---|
| `CountryCode` | `string` | **PK** | Không | Mã quốc gia (ví dụ: `VN`, `KR`, `JP`). Khóa chính. |
| `CountryName` | `string?` | | Có | Tên quốc gia (chuẩn hóa qua `StdStr`). |
| `FlagActive` | `string?` | | Có | Cờ trạng thái hoạt động: `"1"` (Đang hoạt động), `"0"` (Ngừng hoạt động). |
| `LogLUDTimeUTC` | `DateTime?` | | Có | Thời điểm cập nhật cuối cùng theo chuẩn UTC. |
| `LogLUBy` | `string?` | | Có | Người thực hiện cập nhật cuối cùng (mặc định gán `"seed"` hoặc `"port"`). |

**Dữ liệu mẫu khởi tạo (Seeder - `Mst_CountrySeeder`, Idempotent)**:
- Gồm 4 bản ghi demo: `VN` (Việt Nam, FlagActive `'1'`), `KR` (Hàn Quốc, FlagActive `'1'`), `JP` (Nhật Bản, FlagActive `'1'`), `US` (Hoa Kỳ, FlagActive `'0'`).
- Toàn bộ bản ghi gán `LogLUBy = "seed"`.

---

## 2. Đặc tả Chi tiết Từng Endpoint API

---

### 2.1. GetAllActive (#109)

Lấy toàn bộ danh sách các quốc gia đang hoạt động (`FlagActive = '1'`).

- **Route**: `POST /api/mst_country/getallactive`
- **Controller Action**: [`Mst_CountryControllerPort.GetAllActive`](../../Controllers/Port/Mst_CountryControllerPort.cs#L56)
- **Tham chiếu nguồn**: Gate `Mst_CountryController.cs:25-85` → API `MstCountryController.cs:20-91` → Biz `Master.cs:3298-3526`

#### Bảng tham số Request
Endpoint không nhận tham số Body (`gate :25`).

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Thiết lập tham số phân trang: `Ft_RecordStart = 0`, `Ft_RecordCount = 123456000` (`gate :41-42, :60-61`).
2. Mệnh đề lọc qua `strWhereClause_Mst_Country_Active` (`gate :89-100`):
   - **FlagActive**: Toán tử so sánh bằng tuyệt đối `=` với giá trị `"1"` (`gate :47, :96`).
   - **OrgID**: Gate nhận tham số nhưng hàm lọc không sử dụng → Không lọc theo `OrgID`.
3. Sắp xếp thứ tự: Sắp xếp theo `mc.CountryCode ASC` (`Master.cs:3393`), select cuối sắp theo `t.MyIdxSeq ASC` (`Master.cs:3432`).
4. Gán `MyIdxSeq = recordStart + i` (0-based identity).
5. Trả về mảng phẳng trần các thực thể quốc gia. Nếu danh sách rỗng trả về `Data = null` (`gate :44, :67-72`).

#### Bảng mã lỗi
Endpoint luôn trả về HTTP 200.

#### Ví dụ Request / Response thực tế (từ lần chạy VERIFY #109)
- Request: `POST /api/mst_country/getallactive`
- Response (HTTP 200):
  ```json
  {
    "Success": true,
    "Data": [
      { "MyIdxSeq": 0, "CountryCode": "JP", "CountryName": "Nhật Bản", "FlagActive": "1", "LogLUDTimeUTC": null, "LogLUBy": "seed" },
      { "MyIdxSeq": 1, "CountryCode": "KR", "CountryName": "Hàn Quốc", "FlagActive": "1", "LogLUDTimeUTC": null, "LogLUBy": "seed" },
      { "MyIdxSeq": 2, "CountryCode": "VN", "CountryName": "Việt Nam", "FlagActive": "1", "LogLUDTimeUTC": null, "LogLUBy": "seed" }
    ]
  }
  ```

---

## 3. Khác biệt so với Nguồn & GAP

1. **Không có DDL nguồn**: Bảng `Mst_Country` không có tập tin DDL tạo bảng trong repository; cấu trúc 5 cột và khóa chính `CountryCode` được xác thực qua model `Mst_Country.cs` và câu lệnh truy vấn SQL của `Master.cs`.
2. **Không có NetworkID**: Đây là một trong số ít các bảng danh mục hệ thống không chứa trường `NetworkID`.
3. **Phân quyền & Kiểm soát truy cập**: Bỏ qua các kiểm tra phân quyền `CheckGatewayAuthentication`, `CheckAuthorize`, `Sys_Access_CheckDenyV30` và ghi log `RequestLog`/`ReturnLog`.

---

## 4. Nhật ký Thay đổi

| Hạng mục (#) | Action / Endpoint | Commit Hash | Ngày hoàn thành | Ghi chú |
|---|---|---|---|---|
| #109 | `GetAllActive` | `4d4e166` | 2026-10-16 | Khởi tạo bảng `Mst_Country`, seeder demo và endpoint lấy danh sách quốc gia đang hoạt động |
