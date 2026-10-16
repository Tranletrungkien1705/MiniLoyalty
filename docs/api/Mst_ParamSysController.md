# Tham chiếu API Mst_ParamSysController (Port 1-1)

| Thông tin | Giá trị |
|---|---|
| **Phạm vi** | Tài liệu tham chiếu chi tiết 1 endpoint đã port 1-1 của Mst_ParamSysController (#187) |
| **Cập nhật** | 2026-10-16 |
| **Phiên bản mã** | `ed28f8f` |
| **Trạng thái** | Nháp — chờ review |

> [!NOTE]
> Toàn bộ các endpoint thuộc controller này áp dụng chuẩn hóa JSON PascalCase tự động qua `PortJsonResultFilter` và xử lý ngoại lệ tập trung qua `PortExceptionFilter` (chuyển đổi ngoại lệ chưa bắt thành HTTP 400 `PortResult.Fail`). Chi tiết xem tại [Kiến trúc Hạ tầng Dùng chung](../kien-truc/HA_TANG_CHUNG.md).

---

## 1. Tóm tắt Nghiệp vụ & Cấu trúc Dữ liệu

### 1.1. Mục đích Nghiệp vụ
API `Mst_ParamSysController` quản lý danh mục các tham số cấu hình hệ thống (`Mst_ParamSys`) trong MiniLoyalty. Module cung cấp endpoint tra cứu toàn bộ danh sách các tham số hệ thống đang hoạt động phục vụ cho các cài đặt nghiệp vụ toàn cục (như tỷ lệ quy đổi điểm đơn vị sang tiền `UNITPOINTTOMONEY` hay các cờ cấu hình chung).

### 1.2. Bảng Đối chiếu Nguồn Tra cứu (Source Code Mapping)

| Thành phần | Đường dẫn tập tin nguồn (`2022.7.Loyalty`) | Tập tin đích tương ứng (`MiniLoyalty`) |
|---|---|---|
| **Gate Controller** | `13.ClientGate\V10\idn.Loyalty.WebAPI\Controllers\Mst_ParamSysController.cs:26-163` | [`Controllers/Port/Mst_ParamSysControllerPort.cs`](../../Controllers/Port/Mst_ParamSysControllerPort.cs) |
| **API Backend** | `11.BackEnd\V10\idn.Loyalty.Biz.Web\Controllers\MstParamSysController.cs:19-90` | Tích hợp trực tiếp vào [`Controllers/Port/Mst_ParamSysControllerPort.cs`](../../Controllers/Port/Mst_ParamSysControllerPort.cs) |
| **Nghiệp vụ (Biz)** | `11.BackEnd\V10\idn.Loyalty.Biz\Master.Loyalty.cs` (GetX `:12861-13020`; Get `:12707-12860`; WAS `:12603-12706`) | Tích hợp trực tiếp vào [`Controllers/Port/Mst_ParamSysControllerPort.cs`](../../Controllers/Port/Mst_ParamSysControllerPort.cs) |
| **Entity Model** | `12.Dev.Common\idn.Loyalty.Common\Models\Loyalty\Mst_ParamSys.cs:9-17` | [`Models/Port/Mst_ParamSys.cs:28-36`](../../Models/Port/Mst_ParamSys.cs#L28-L36) |
| **Cấu hình EF & DDL**| DDL `Mst_Param.sql:4-11` (6 cột) | [`Models/Port/Mst_ParamSys.cs:39-48`](../../Models/Port/Mst_ParamSys.cs#L39-L48) |
| **Seeder Demo** | `Order => 14` (danh mục `Mst_*` dùng 10–99) | [`Models/Port/Mst_ParamSys.cs:55-97`](../../Models/Port/Mst_ParamSys.cs#L55-L97) |

### 1.3. Cấu trúc Bảng CSDL (`Mst_ParamSys`)

Bảng dữ liệu vật lý `Mst_ParamSys` gồm đúng 6 cột theo DDL chuẩn:

| Tên cột | Kiểu EF Core | Khóa | Cho phép NULL | Ghi chú & Ý nghĩa |
|---|---|---|---|---|
| `ParamCode` | `string` | **PK** | Không | Mã tham số hệ thống (Varchar200 NOT NULL). Khóa chính. |
| `NetworkID` | `string` | | Không | Mã mạng nghiệp vụ (Varchar50 NOT NULL). |
| `ParamValue` | `string?` | | Có | Giá trị tham số hệ thống (Varchar400). |
| `FlagActive` | `string?` | | Có | Cờ trạng thái hoạt động: `"1"` (Đang hoạt động), `"0"` (Ngừng hoạt động). |
| `LogLUDTimeUTC` | `DateTime?` | | Có | Thời điểm cập nhật cuối cùng theo chuẩn UTC. |
| `LogLUBy` | `string?` | | Có | Người thực hiện cập nhật cuối cùng. |

**Dữ liệu mẫu khởi tạo (Seeder - `Mst_ParamSysSeeder`, Idempotent)**:
- Gồm các bản ghi nguyên văn từ script khởi tạo:
  - `TS01`: NetworkID `6403834000`, ParamValue `"1"`, FlagActive `'1'`.
  - `UNITPOINTTOMONEY`: NetworkID `6403834000`, ParamValue `"1"`, FlagActive `'1'`.
  - `DEMO_OFF`: ParamValue `"0"`, FlagActive `'0'`.
- Toàn bộ bản ghi gán `LogLUBy = "SYS"` hoặc `"seed"`.

---

## 2. Đặc tả Chi tiết Từng Endpoint API

---

### 2.1. GetAllActive (#187)

Lấy toàn bộ danh sách các tham số hệ thống đang hoạt động (`FlagActive = '1'`).

- **Route**: `POST /api/mst_paramsys/getallactive`
- **Controller Action**: [`Mst_ParamSysControllerPort.GetAllActive`](../../Controllers/Port/Mst_ParamSysControllerPort.cs#L47)
- **Tham chiếu nguồn**: Gate `Mst_ParamSysController.cs:26-135` → API `MstParamSysController.cs:19-90` → Biz `Master.Loyalty.cs:12861-13020`

#### Bảng tham số Request
Endpoint không nhận tham số Body (`gate :26`).

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Thiết lập tham số phân trang: `Ft_RecordStart = 0`, `Ft_RecordCount = 123456000` (`gate :68-69`).
2. Mệnh đề lọc qua `strWhereClause_Mst_ParamSys` (`gate :138-163`):
   - **FlagActive**: Lọc theo giá trị `"1"` (`gate :55, :152-160`).
   - **OrgID**: Gate nhận tham số nhưng hàm lọc không sử dụng → Không lọc theo `OrgID`.
3. Sắp xếp thứ tự: Sắp xếp theo `LogLUDTimeUTC DESC` (`Master.Loyalty.cs:12926, :12965`), sắp phụ theo `ParamCode ASC` để kết quả xác định khi trùng mốc thời gian.
4. Gán `MyIdxSeq = recordStart + i` (0-based identity).
5. Trả về mảng phẳng trần các thực thể tham số hệ thống. Nếu danh sách rỗng trả về `Data = null` (`gate :49, :75-77, :113-119`).

#### Bảng mã lỗi
Endpoint luôn trả về HTTP 200.

#### Ví dụ Request / Response thực tế (từ lần chạy VERIFY #187)
- Request: `POST /api/mst_paramsys/getallactive`
- Response (HTTP 200):
  ```json
  {
    "Success": true,
    "Data": [
      { "MyIdxSeq": 0, "ParamCode": "TS01", "NetworkID": "6403834000", "ParamValue": "1", "FlagActive": "1", "LogLUDTimeUTC": "2022-11-08T00:00:00", "LogLUBy": "SYS" },
      { "MyIdxSeq": 1, "ParamCode": "UNITPOINTTOMONEY", "NetworkID": "6403834000", "ParamValue": "1", "FlagActive": "1", "LogLUDTimeUTC": "2022-11-08T00:00:00", "LogLUBy": "SYS" }
    ]
  }
  ```

---

## 3. Khác biệt so với Nguồn & GAP

1. **Hòa trùng sắp xếp**: Nguồn không quy định thứ tự sắp xếp khi có nhiều tham số có cùng mốc `LogLUDTimeUTC`. MiniLoyalty bổ sung sắp phụ theo `ParamCode ASC` để đảm bảo tính xác định tuyệt đối giữa các lần chạy.
2. **Phân quyền & Kiểm soát truy cập**: Bỏ qua các kiểm tra phân quyền `CheckGatewayAuthentication`, `CheckAuthorize` và ghi log `RequestLog`/`ReturnLog`.

---

## 4. Nhật ký Thay đổi

| Hạng mục (#) | Action / Endpoint | Commit Hash | Ngày hoàn thành | Ghi chú |
|---|---|---|---|---|
| #187 | `GetAllActive` | `2bb249c` | 2026-10-16 | Khởi tạo bảng `Mst_ParamSys`, seeder demo và endpoint lấy danh sách tham số hệ thống đang hoạt động |
