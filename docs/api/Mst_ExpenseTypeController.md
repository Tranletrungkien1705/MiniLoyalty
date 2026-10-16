# Tham chiếu API Mst_ExpenseTypeController (Port 1-1)

| Thông tin | Giá trị |
|---|---|
| **Phạm vi** | Tài liệu tham chiếu chi tiết 1 endpoint đã port 1-1 của Mst_ExpenseTypeController (#151) |
| **Cập nhật** | 2026-10-16 |
| **Phiên bản mã** | `ed28f8f` |
| **Trạng thái** | Nháp — chờ review |

> [!NOTE]
> Toàn bộ các endpoint thuộc controller này áp dụng chuẩn hóa JSON PascalCase tự động qua `PortJsonResultFilter` và xử lý ngoại lệ tập trung qua `PortExceptionFilter` (chuyển đổi ngoại lệ chưa bắt thành HTTP 400 `PortResult.Fail`). Chi tiết xem tại [Kiến trúc Hạ tầng Dùng chung](../kien-truc/HA_TANG_CHUNG.md).

---

## 1. Tóm tắt Nghiệp vụ & Cấu trúc Dữ liệu

### 1.1. Mục đích Nghiệp vụ
API `Mst_ExpenseTypeController` quản lý danh mục các loại chi phí phát sinh (`Mst_ExpenseType`) trong hệ thống MiniLoyalty. Module cung cấp endpoint tra cứu toàn bộ danh sách các loại chi phí đang hoạt động phục vụ cho các phân hệ tính toán chi phí dịch vụ, hóa đơn và báo cáo tài chính.

### 1.2. Bảng Đối chiếu Nguồn Tra cứu (Source Code Mapping)

| Thành phần | Đường dẫn tập tin nguồn (`2022.7.Loyalty`) | Tập tin đích tương ứng (`MiniLoyalty`) |
|---|---|---|
| **Gate Controller** | `13.ClientGate\V10\idn.Loyalty.WebAPI\Controllers\Mst_ExpenseTypeController.cs:23-155` | [`Controllers/Port/Mst_ExpenseTypeControllerPort.cs`](../../Controllers/Port/Mst_ExpenseTypeControllerPort.cs) |
| **API Backend** | `11.BackEnd\V10\idn.Loyalty.Biz.Web\Controllers\MstExpenseTypeController.cs:22-90` | Tích hợp trực tiếp vào [`Controllers/Port/Mst_ExpenseTypeControllerPort.cs`](../../Controllers/Port/Mst_ExpenseTypeControllerPort.cs) |
| **Nghiệp vụ (Biz)** | `11.BackEnd\V10\idn.Loyalty.Biz\Master.Loyalty.cs` (GetX `:9190-9349`; Get `:9036-9189`; WAS `:8933-9035`) | Tích hợp trực tiếp vào [`Controllers/Port/Mst_ExpenseTypeControllerPort.cs`](../../Controllers/Port/Mst_ExpenseTypeControllerPort.cs) |
| **Entity Model** | `20221026.CreateDatabase.sql:183-191` (7 cột) | [`Models/Port/Mst_ExpenseType.cs:27-36`](../../Models/Port/Mst_ExpenseType.cs#L27-L36) |
| **Cấu hình EF** | Khóa gộp `(ExpenseType, NetworkID)` | [`Models/Port/Mst_ExpenseType.cs:39-51`](../../Models/Port/Mst_ExpenseType.cs#L39-L51) |
| **Seeder Demo** | `Order => 35` (danh mục `Mst_*` dùng 10–99) | [`Models/Port/Mst_ExpenseType.cs:57-98`](../../Models/Port/Mst_ExpenseType.cs#L57-L98) |

### 1.3. Cấu trúc Bảng CSDL (`Mst_ExpenseType`)

Bảng dữ liệu vật lý `Mst_ExpenseType` gồm đúng 7 cột theo DDL chuẩn:

| Tên cột | Kiểu EF Core | Khóa | Cho phép NULL | Ghi chú & Ý nghĩa |
|---|---|---|---|---|
| `ExpenseType` | `string` | **PK1** | Không | Mã loại chi phí (Varchar50 NOT NULL). Khóa chính phần 1. |
| `NetworkID` | `string` | **PK2** | Không | Mã mạng nghiệp vụ (Varchar50 NOT NULL). Khóa chính phần 2. |
| `ExpenseTypeName` | `string` | | Không | Tên loại chi phí (Varchar200 NOT NULL). |
| `FlagActive` | `string?` | | Có | Cờ trạng thái hoạt động: `"1"` (Đang hoạt động), `"0"` (Ngừng hoạt động). |
| `Remark` | `string?` | | Có | Ghi chú loại chi phí (Varchar400). |
| `LogLUDTimeUTC` | `DateTime?` | | Có | Thời điểm cập nhật cuối cùng theo chuẩn UTC. |
| `LogLUBy` | `string?` | | Có | Người thực hiện cập nhật cuối cùng. |

**Dữ liệu mẫu khởi tạo (Seeder - `Mst_ExpenseTypeSeeder`, Idempotent)**:
- Gồm 3 bản ghi demo: `EX01` (Chi phí dịch vụ, LogLUDTimeUTC 2026-01-01, FlagActive `'1'`), `EX02` (Chi phí phụ tùng, LogLUDTimeUTC 2026-02-01, FlagActive `'1'`), `EX03` (Chi phí khác, LogLUDTimeUTC 2026-03-01, FlagActive `'0'`).
- Toàn bộ bản ghi gán `NetworkID = "4020320001"`, `LogLUBy = "seed"`.

---

## 2. Đặc tả Chi tiết Từng Endpoint API

---

### 2.1. GetAllActive (#151)

Lấy toàn bộ danh sách các loại chi phí đang hoạt động (`FlagActive = '1'`).

- **Route**: `POST /api/mst_expensetype/getallactive`
- **Controller Action**: [`Mst_ExpenseTypeControllerPort.GetAllActive`](../../Controllers/Port/Mst_ExpenseTypeControllerPort.cs#L63)
- **Tham chiếu nguồn**: Gate `Mst_ExpenseTypeController.cs:23-132` → API `MstExpenseTypeController.cs:22-90` → Biz `Master.Loyalty.cs:9190-9349`

#### Bảng tham số Request
Endpoint không nhận tham số Body (`gate :23`).

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Thiết lập tham số phân trang: `Ft_RecordStart = 0`, `Ft_RecordCount = 123456000` (`gate :65-66`).
2. Mệnh đề lọc qua `strWhereClause_Mst_ExpenseType` (`gate :138-155`):
   - **FlagActive**: Lọc theo toán tử `in` (`in ('1')`, `gate :144-152`). Ở đích so khớp tuyệt đối `Like(cot.ToLower(), "1", "\\")` không wildcard.
   - **OrgID**: Gate nhận tham số nhưng hàm tạo điều kiện không xử lý `OrgID` → Không lọc theo `OrgID`/`NetworkID`.
3. Sắp xếp thứ tự: Sắp xếp theo `mrp.LogLUDTimeUTC DESC` (`Master.Loyalty.cs:9255, :9294`), sắp phụ theo `ExpenseType ASC` để đảm bảo thứ tự xác định khi trùng mốc thời gian.
4. Gán `MyIdxSeq = recordStart + i` (0-based identity).
5. Trả về mảng phẳng trần các thực thể loại chi phí. Nếu danh sách rỗng trả về `Data = null` (`gate :72-74, :112-119`). Khối mở rộng cây phân cấp `Ext/tree` (`:75-107`) bị comment ở nguồn nên không port.

#### Bảng mã lỗi
Endpoint luôn trả về HTTP 200.

#### Ví dụ Request / Response thực tế (từ lần chạy VERIFY #151)
- Request: `POST /api/mst_expensetype/getallactive`
- Response (HTTP 200):
  ```json
  {
    "Success": true,
    "Data": [
      { "MyIdxSeq": 0, "ExpenseType": "EX02", "NetworkID": "4020320001", "ExpenseTypeName": "Chi phi phu tung", "FlagActive": "1", "LogLUDTimeUTC": "2026-02-01T00:00:00" },
      { "MyIdxSeq": 1, "ExpenseType": "EX01", "NetworkID": "4020320001", "ExpenseTypeName": "Chi phi dich vu", "FlagActive": "1", "LogLUDTimeUTC": "2026-01-01T00:00:00" }
    ]
  }
  ```

---

## 3. Khác biệt so với Nguồn & GAP

1. **Khóa chính gộp**: DDL nguồn không khai báo ràng buộc PK tường minh, model EF Core cấu hình khóa gộp `(ExpenseType, NetworkID)` để bảo toàn tính duy nhất và khả năng truy vấn.
2. **Khối mở rộng Ext/tree**: Nguồn có đoạn mã xây dựng cấu trúc cây chi phí `Mst_ExpenseTypeExtension` nhưng đã bị comment vô hiệu hóa (`gate :75-107`). MiniLoyalty không port đoạn mã chết này.
3. **Phân quyền & Kiểm soát truy cập**: Bỏ qua các kiểm tra phân quyền `CheckGatewayAuthentication`, `CheckAuthorize`, `Sys_User_CheckAuthorize` và ghi log `RequestLog`/`ReturnLog`.

---

## 4. Nhật ký Thay đổi

| Hạng mục (#) | Action / Endpoint | Commit Hash | Ngày hoàn thành | Ghi chú |
|---|---|---|---|---|
| #151 | `GetAllActive` | `aecf24a` | 2026-10-16 | Khởi tạo bảng `Mst_ExpenseType`, seeder demo và endpoint lấy danh sách loại chi phí đang hoạt động |
