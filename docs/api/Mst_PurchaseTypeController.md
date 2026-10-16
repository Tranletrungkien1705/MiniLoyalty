# Tham chiếu API Mst_PurchaseTypeController (Port 1-1)

| Thông tin | Giá trị |
|---|---|
| **Phạm vi** | Tài liệu tham chiếu chi tiết 1 endpoint đã port 1-1 của Mst_PurchaseTypeController (#222) |
| **Cập nhật** | 2026-10-16 |
| **Phiên bản mã** | `ed28f8f` |
| **Trạng thái** | Nháp — chờ review |

> [!NOTE]
> Toàn bộ các endpoint thuộc controller này áp dụng chuẩn hóa JSON PascalCase tự động qua `PortJsonResultFilter` và xử lý ngoại lệ tập trung qua `PortExceptionFilter` (chuyển đổi ngoại lệ chưa bắt thành HTTP 400 `PortResult.Fail`). Chi tiết xem tại [Kiến trúc Hạ tầng Dùng chung](../kien-truc/HA_TANG_CHUNG.md).

---

## 1. Tóm tắt Nghiệp vụ & Cấu trúc Dữ liệu

### 1.1. Mục đích Nghiệp vụ
API `Mst_PurchaseTypeController` quản lý danh mục hình thức mua hàng / mua xe (`Mst_PurchaseType`) trong hệ thống MiniLoyalty. Module cung cấp endpoint tra cứu danh sách các hình thức mua hàng (ví dụ: mua thêm, mua mới, mua xe cũ, đổi xe) phục vụ cho quy trình ghi nhận giao dịch khách hàng và tính toán quyền lợi hội viên.

### 1.2. Bảng Đối chiếu Nguồn Tra cứu (Source Code Mapping)

| Thành phần | Đường dẫn tập tin nguồn (`2022.7.Loyalty`) | Tập tin đích tương ứng (`MiniLoyalty`) |
|---|---|---|
| **Gate Controller** | `13.ClientGate\V10\idn.Loyalty.WebAPI\Controllers\Mst_PurchaseTypeController.cs:26-94` | [`Controllers/Port/Mst_PurchaseTypeControllerPort.cs`](../../Controllers/Port/Mst_PurchaseTypeControllerPort.cs) |
| **API Backend** | `11.BackEnd\V10\idn.Loyalty.Biz.Web\Controllers\MstPurchaseTypeController.cs:20-89` | Tích hợp trực tiếp vào [`Controllers/Port/Mst_PurchaseTypeControllerPort.cs`](../../Controllers/Port/Mst_PurchaseTypeControllerPort.cs) |
| **Nghiệp vụ (Biz)** | `11.BackEnd\V10\idn.Loyalty.Biz\Master.Loyalty.cs` (GetX `:22129-22282`; Get `:21984-22128`; WAS `:21886-21983`) | Tích hợp trực tiếp vào [`Controllers/Port/Mst_PurchaseTypeControllerPort.cs`](../../Controllers/Port/Mst_PurchaseTypeControllerPort.cs) |
| **Entity Model** | `12.Dev.Common\idn.Loyalty.Common\Models\Loyalty\Mst_PurchaseType.cs:9-15` | [`Models/Port/Mst_PurchaseType.cs:25-31`](../../Models/Port/Mst_PurchaseType.cs#L25-L31) |
| **Hằng số Hệ thống** | `idn.Loyalty.Constants\Const.Main.BE.cs:1600-1605` (BUYADD, BUYNEW, BUYUSED, EXCHANGE) | Tích hợp vào seeder |
| **Seeder Demo** | `Order => 13` (danh mục `Mst_*` dùng 10–99) | [`Models/Port/Mst_PurchaseType.cs:50-67`](../../Models/Port/Mst_PurchaseType.cs#L50-L67) |

### 1.3. Cấu trúc Bảng CSDL (`Mst_PurchaseType`)

Bảng dữ liệu vật lý `Mst_PurchaseType` gồm đúng 4 cột (đặc biệt: **KHÔNG có các cột `NetworkID`, `LogLUDTimeUTC`, `LogLUBy`** theo đúng Model nguồn):

| Tên cột | Kiểu EF Core | Khóa | Cho phép NULL | Ghi chú & Ý nghĩa |
|---|---|---|---|---|
| `PurchaseType` | `string` | **PK** | Không | Mã loại mua hàng (`BUYADD`, `BUYNEW`, `BUYUSED`, `EXCHANGE`). Khóa chính. |
| `PurchaseTypeName` | `string` | | Không | Tên loại mua hàng (chuẩn hóa qua `StdStr`). |
| `Remark` | `string?` | | Có | Ghi chú thêm. |
| `FlagActive` | `string?` | | Có | Cờ trạng thái hoạt động: `"1"` (Đang hoạt động), `"0"` (Ngừng hoạt động). |

**Dữ liệu mẫu khởi tạo (Seeder - `Mst_PurchaseTypeSeeder`, Idempotent)**:
- Gồm 4 bản ghi theo đúng hằng số nghiệp vụ:
  - `BUYADD`: "Mua thêm", FlagActive `'1'`.
  - `BUYNEW`: "Mua mới", FlagActive `'1'`.
  - `BUYUSED`: "Mua xe cũ", FlagActive `'1'`.
  - `EXCHANGE`: "Đổi xe", FlagActive `'0'`.

---

## 2. Đặc tả Chi tiết Từng Endpoint API

---

### 2.1. GetAllActive (#222)

Lấy danh sách các loại mua hàng trong hệ thống.

- **Route**: `POST /api/mst_purchasetype/getallactive`
- **Controller Action**: [`Mst_PurchaseTypeControllerPort.GetAllActive`](../../Controllers/Port/Mst_PurchaseTypeControllerPort.cs#L37)
- **Tham chiếu nguồn**: Gate `Mst_PurchaseTypeController.cs:26-94` → API `MstPurchaseTypeController.cs:20-89` → Biz `Master.Loyalty.cs:22129-22282`

#### Bảng tham số Request
Endpoint không nhận tham số Body (`gate :26`).

#### Quy tắc xử lý theo thứ tự (Hành vi không lọc cờ của nguồn)
1. Thiết lập tham số phân trang: `Ft_RecordStart = 0`, `Ft_RecordCount = 123456000` (`gate :61-62`).
2. **Hành vi không lọc cờ của nguồn**: Tại Gate nguồn, đoạn gán `//FlagActive = "1"` bị comment out (`:46-50`), và biến `Ft_WhereClause` được gán cứng chuỗi rỗng `""` (`:63`). Do đó, **hệ thống không lọc theo cờ hoạt động**, các bản ghi có `FlagActive = '0'` (như `EXCHANGE`) vẫn được trả về bình thường.
3. Sắp xếp thứ tự: Sắp xếp tăng dần theo `mpt.PurchaseType ASC` (`Master.Loyalty.cs:22189, :22228`).
4. Gán `MyIdxSeq = recordStart + i` (0-based identity).
5. Trả về mảng phẳng trần các thực thể loại mua hàng. Nếu không có dữ liệu trả về `Data = null` (`gate :45, :67-72, :78`).

#### Bảng mã lỗi
Endpoint luôn trả về HTTP 200.

#### Ví dụ Request / Response thực tế (từ lần chạy VERIFY #222)
- Request: `POST /api/mst_purchasetype/getallactive`
- Response (HTTP 200):
  ```json
  {
    "Success": true,
    "Data": [
      { "MyIdxSeq": 0, "PurchaseType": "BUYADD", "PurchaseTypeName": "Mua thêm", "Remark": null, "FlagActive": "1" },
      { "MyIdxSeq": 1, "PurchaseType": "BUYNEW", "PurchaseTypeName": "Mua mới", "Remark": null, "FlagActive": "1" },
      { "MyIdxSeq": 2, "PurchaseType": "BUYUSED", "PurchaseTypeName": "Mua xe cũ", "Remark": null, "FlagActive": "1" },
      { "MyIdxSeq": 3, "PurchaseType": "EXCHANGE", "PurchaseTypeName": "Đổi xe", "Remark": null, "FlagActive": "0" }
    ]
  }
  ```

---

## 3. Khác biệt so với Nguồn & GAP

1. **Không có DDL nguồn**: Bảng `Mst_PurchaseType` không có DDL trong repo; cấu trúc 4 cột được bảo toàn chính xác theo model nguồn và hằng số hệ thống.
2. **Hành vi GetAllActive không lọc FlagActive**: Tên hàm là `GetAllActive` nhưng do mệnh đề lọc bị comment ở nguồn nên trả về cả bản ghi inactive. MiniLoyalty tái hiện trung thực hành vi này.
3. **Phân quyền & Kiểm soát truy cập**: Bỏ qua các kiểm tra phân quyền và ghi log hệ thống.

---

## 4. Nhật ký Thay đổi

| Hạng mục (#) | Action / Endpoint | Commit Hash | Ngày hoàn thành | Ghi chú |
|---|---|---|---|---|
| #222 | `GetAllActive` | `ad8f49a` | 2026-10-16 | Khởi tạo bảng `Mst_PurchaseType`, seeder demo và endpoint lấy danh sách loại mua hàng |
