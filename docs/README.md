# Tài liệu Hệ thống MiniLoyalty — Tham chiếu Kiến trúc & API Port 1-1

| Thông tin | Giá trị |
|---|---|
| **Phạm vi** | Toàn bộ tài liệu tham chiếu kiến trúc và API port 1-1 MiniLoyalty |
| **Cập nhật** | 2026-10-16 |
| **Phiên bản mã** | 8b25ecf |
| **Trạng thái** | Nháp — chờ review |

---

## 1. Giới thiệu

Tài liệu này cung cấp toàn bộ thông số kỹ thuật, kiến trúc phân tầng, thành phần dùng chung và đặc tả tham chiếu chi tiết các API đã được chuyển đổi (port) 1-1 từ hệ thống gốc `2022.7.Loyalty` sang hệ thống mới `MiniLoyalty` (thư mục gốc của repository).

Mục tiêu cốt lõi của quá trình port:
- Đảm bảo tương thích hoàn toàn 1-1 về nghiệp vụ, tham số đầu vào, quy tắc lọc, chuẩn hóa chuỗi, chuỗi kiểm tra guard và mã lỗi (`ErridnLoyalty.*`).
- Độc lập hóa cấu trúc mã nguồn giữa các module để hỗ trợ phát triển song song nhiều luồng mà không xung đột merge.
- Cung cấp tài liệu kỹ thuật chuẩn xác, kiểm chứng được dựa trên mã nguồn thực tế và kết quả chạy thử nghiệm.

---

## 2. Mục lục Tài liệu

### 2.1. Kiến trúc Hệ thống (`docs/kien-truc/`)

- [docs/kien-truc/TONG_QUAN.md](kien-truc/TONG_QUAN.md): Tổng quan kiến trúc hệ thống port 1-1:
  - So sánh mô hình kiến trúc nguồn 4 tầng (ClientGate → WCF/WebAPI → Biz → SQL Server) và kiến trúc đích rút gọn (Controller Port → EF Core → SQLite / PostgreSQL).
  - Đặc tả các thành phần hạ tầng chia sẻ: [`PortQuery`](../Data/PortQuery.cs), [`PortResult`](../Data/PortResult.cs), [`PortSeq`](../Data/PortSeq.cs), [`PortExcel`](../Data/PortExcel.cs), [`PortInfra`](../Data/PortInfra.cs), [`IPortSeeder`](../Data/PortInfra.cs#L14-L20).
  - Cơ chế tự sinh DDL bảng và bổ sung cột tự động (`EnsureNewTablesAsync`, `AddMissingColumnsAsync`).
  - Thiết lập tham số môi trường và cấu hình hệ thống (`PORT`, `CONNECTION_STRING`, `Biz_OrgID`, `Biz_NetworkID`).

### 2.2. Tham chiếu API (`docs/api/`)

Tài liệu chi tiết từng controller API được port 1-1 từ tầng gate của hệ thống nguồn:

- [docs/api/Mst_DepartmentController.md](api/Mst_DepartmentController.md): Danh mục phòng ban (`Mst_Department`) — Gồm 8 endpoint đã hoàn thành (#122 - #129):
  - `POST /api/mst_department/search`: Tìm kiếm danh mục phòng ban phân trang, lọc theo từ khóa, tổ chức và cờ hoạt động.
  - `POST /api/mst_department/getallactive`: Lấy danh sách tất cả phòng ban đang hoạt động (`FlagActive = '1'`) theo tổ chức.
  - `POST /api/mst_department/getbydepartmentcode`: Tra cứu chi tiết một phòng ban theo mã (`DepartmentCode`).
  - `POST /api/mst_department/create`: Thêm mới phòng ban, tự sinh sequence mã `DPMCODE...`, tự động cập nhật phân cấp BU toàn bảng.
  - `POST /api/mst_department/update`: Cập nhật tên, mô tả và trạng thái phòng ban (không thay đổi phòng ban cấp trên).
  - `POST /api/mst_department/delete`: Xóa một phòng ban đơn lẻ theo mã phòng ban và mã tổ chức.
  - `POST /api/mst_department/deletemultiple`: Xóa nhiều phòng ban theo danh sách mã, hỗ trợ hành vi xóa một phần (partial delete).
  - `POST /api/mst_department/import`: Nhập dữ liệu danh mục phòng ban từ file Excel `.xlsx`, sinh mã GUID, hỗ trợ nhập một phần (partial import).

*(Các controller khác sẽ được bổ sung vào mục lục này trong các lượt tài liệu hóa tiếp theo).*

### 2.3. Quy trình & Vận hành (`docs/quy-trinh/`)

- Thư mục do phân hệ tài liệu quy trình (doc2) quản lý độc lập, bao gồm các kịch bản kiểm thử nghiệp vụ, hướng dẫn vận hành và đối soát.

---

## 3. Quy ước Kỹ thuật Cốt lõi

Mọi module port 1-1 và tài liệu tham chiếu đều tuân thủ các quy tắc bất biến sau:

1. **Chuẩn hóa chuỗi đầu vào theo nguồn**:
   - `StdParam(x)` = `x.Trim().ToUpper()`, chuỗi rỗng chuyển thành `null` (áp dụng cho các trường mã: `DepartmentCode`, `OrgID`, `MST`...).
   - `StdStr(x)` = `x.Trim().Normalize()`, chuỗi rỗng chuyển thành `null` (áp dụng cho tên, mô tả).
   - `StdFlag(x)` = `"1"` nếu bằng `"1"`, mọi giá trị khác chuyển thành `"0"`.
2. **Xử lý so sánh chuỗi trên SQLite / PostgreSQL**:
   - SQL Server nguồn sử dụng collation không phân biệt hoa thường (`CI`).
   - SQLite và PostgreSQL phân biệt hoa thường với toán tử so sánh chuỗi mặc định. Do đó, toàn bộ truy vấn lọc chuỗi bắt buộc phải hạ chữ cả hai vế qua [`PortQuery`](../Data/PortQuery.cs) kết hợp với `EF.Functions.Like(...)`.
3. **Chuẩn hóa phản hồi mã lỗi**:
   - Lỗi nghiệp vụ trả về HTTP 400 kèm định dạng chuẩn `{ Success = false, ErrorCode = "ErridnLoyalty.<MãLỗi>", Params = { ... } }` thông qua helper [`PortResult.Fail`](../Data/PortResult.cs#L17-L23).
4. **Phân vùng dữ liệu và cô lập module**:
   - Mỗi bảng và controller port nằm trong từng cặp file riêng biệt (`Models/Port/<Table>.cs` và `Controllers/Port/<Controller>Port.cs`).
   - Bảng được cấu hình bằng `IEntityTypeConfiguration<T>` tự động quét bởi assembly, không sửa đổi trực tiếp [`AppDbContext.cs`](../Data/AppDbContext.cs).
