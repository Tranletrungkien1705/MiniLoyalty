# Tài liệu Hệ thống MiniLoyalty — Tham chiếu Kiến trúc & API Port 1-1

| Thông tin | Giá trị |
|---|---|
| **Phạm vi** | Toàn bộ tài liệu tham chiếu kiến trúc và API port 1-1 MiniLoyalty |
| **Cập nhật** | 2026-10-16 |
| **Phiên bản mã** | `93b6ace` |
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

- [docs/api/Mst_ProvinceController.md](api/Mst_ProvinceController.md): Danh mục tỉnh/thành phố (`Mst_Province`) — Gồm 9 endpoint đã hoàn thành (#213 - #221):
  - `POST /api/mst_province/search`: Tìm kiếm tỉnh/thành phân trang, lọc theo từ khóa và trạng thái hoạt động (toán tử `IN`).
  - `POST /api/mst_province/getbyprovincecode`: Tra cứu chi tiết tỉnh/thành theo mã, không lọc trạng thái hoạt động.
  - `POST /api/mst_province/getallactive`: Lấy toàn bộ danh sách tỉnh/thành đang hoạt động, bọc trong đối tượng `{ Lst_Mst_Province }`.
  - `POST /api/mst_province/create`: Thêm mới tỉnh/thành, kiểm tra mã rỗng, trùng mã và tên rỗng theo thứ tự.
  - `POST /api/mst_province/update`: Cập nhật tên và trạng thái tỉnh/thành, tự động hạ cờ về `'0'` nếu thiếu `FlagActive`.
  - `POST /api/mst_province/delete`: Xóa đơn lẻ tỉnh/thành, phát sinh mã lỗi chữ thường `invalid_provincecode`.
  - `POST /api/mst_province/import`: Nhập danh mục từ Excel `.xlsx`, validate toàn bộ dòng trước khi tạo, hỗ trợ nhập một phần.
  - `POST /api/mst_province/export`: Xuất danh sách tỉnh/thành ra Excel, sử dụng từ điển 3 cột, báo lỗi khi không có dữ liệu.
  - `POST /api/mst_province/exporttemplate`: Xuất file mẫu Excel gồm 2 cột tiêu đề, không truy vấn CSDL.

- [docs/api/Mst_DistrictController.md](api/Mst_DistrictController.md): Danh mục quận/huyện (`Mst_District`) — Gồm 10 endpoint đã hoàn thành (#139 - #148):
  - `POST /api/mst_district/search`: Tìm kiếm quận/huyện phân trang, lọc từ khóa và trạng thái hoạt động (toán tử `=`).
  - `POST /api/mst_district/getdetail`: Tra cứu chi tiết quận/huyện theo cặp khóa ghép `(ProvinceCode, DistrictCode)`.
  - `POST /api/mst_district/getbyprovincecode`: Lấy danh sách quận/huyện theo tỉnh/thành, bọc trong đối tượng `{ Lst_Mst_District }`.
  - `POST /api/mst_district/getallactive`: Lấy tất cả quận/huyện đang hoạt động, trả về danh sách mảng phẳng trực tiếp.
  - `POST /api/mst_district/create`: Thêm mới quận/huyện theo khóa ghép, kiểm tra tồn tại và trạng thái của tỉnh cha.
  - `POST /api/mst_district/update`: Cập nhật tên và trạng thái quận/huyện, không thay đổi tỉnh cha, kiểm tra trạng thái tỉnh cha.
  - `POST /api/mst_district/delete`: Xóa quận/huyện theo khóa ghép, tái hiện hành vi if lồng đặc thù (thiếu tỉnh trả về HTTP 200 `Success = false`).
  - `POST /api/mst_district/import`: Nhập danh mục từ Excel `.xlsx`, validate toàn bộ dòng trước khi tạo, hỗ trợ nhập một phần.
  - `POST /api/mst_district/export`: Xuất danh sách quận/huyện ra Excel, sử dụng từ điển 4 cột, báo lỗi khi không có dữ liệu.
  - `POST /api/mst_district/exporttemplate`: Xuất file mẫu Excel gồm 3 cột tiêu đề, không truy vấn CSDL.

- [docs/api/Mst_CarBrandController.md](api/Mst_CarBrandController.md): Danh mục hãng xe ô tô (`Mst_CarBrand`) — Gồm 10 endpoint đã hoàn thành (#77 - #86):
  - `POST /api/mst_carbrand/search`: Tìm kiếm danh mục hãng xe phân trang, lọc theo từ khóa và trạng thái hoạt động (toán tử `IN`).
  - `POST /api/mst_carbrand/getallactive`: Lấy danh sách tất cả hãng xe đang hoạt động, trả về mảng phẳng trực tiếp.
  - `POST /api/mst_carbrand/getbybrandcode`: Tra cứu chi tiết hãng xe theo mã hiệu xe, không lọc trạng thái hoạt động.
  - `POST /api/mst_carbrand/create`: Thêm mới hãng xe, tự động viết hoa tên hãng theo `StdParam`, kiểm tra trùng mã.
  - `POST /api/mst_carbrand/update`: Cập nhật tên, ghi chú và trạng thái hãng xe, bảo lưu hoa thường của tên theo `StdStr`.
  - `POST /api/mst_carbrand/delete`: Xóa đơn lẻ hãng xe, phát sinh mã lỗi chữ thường `invalid_Brandcode`, không chặn khóa ngoại.
  - `POST /api/mst_carbrand/deletemultiple`: Xóa nhiều hãng xe theo danh sách mã trong từng transaction riêng (xóa một phần).
  - `POST /api/mst_carbrand/import`: Nhập danh mục từ Excel `.xlsx`, validate toàn bộ dòng trước khi tạo, hỗ trợ nhập một phần.
  - `POST /api/mst_carbrand/export`: Xuất danh sách hãng xe ra Excel, sử dụng từ điển 3 cột, báo lỗi khi không có dữ liệu.
  - `POST /api/mst_carbrand/exporttemplate`: Xuất file mẫu Excel gồm 2 cột tiêu đề, không truy vấn CSDL.

- [docs/api/Mst_CardTypeController.md](api/Mst_CardTypeController.md): Danh mục loại thẻ / hạng thẻ (`Mst_CardType`) — Gồm 10 endpoint đã hoàn thành (#87 - #96):
  - `POST /api/mst_cardtype/search`: Tìm kiếm danh mục loại thẻ phân trang, sắp xếp trang theo ngưỡng giá trị `Value ASC`.
  - `POST /api/mst_cardtype/getallactive`: Lấy danh sách loại thẻ đang hoạt động sắp xếp tăng dần theo `Value`, trả về mảng phẳng.
  - `POST /api/mst_cardtype/getbycardtype`: Tra cứu chi tiết loại thẻ theo mã, không lọc trạng thái hoạt động.
  - `POST /api/mst_cardtype/create`: Thêm mới loại thẻ, kiểm tra chặt chẽ `Value > 0`, không trùng `Value` và ràng buộc NOT NULL.
  - `POST /api/mst_cardtype/update`: Cập nhật thông tin loại thẻ, không kiểm tra trùng `Value` khi update theo đúng nguồn.
  - `POST /api/mst_cardtype/delete`: Xóa đơn lẻ loại thẻ, phát sinh lỗi typo `invalid_CartType`, tích hợp giả lập khóa ngoại con.
  - `POST /api/mst_cardtype/deletemultiple`: Xóa nhiều loại thẻ theo danh sách mã trong từng transaction riêng (xóa một phần).
  - `POST /api/mst_cardtype/import`: Nhập danh mục từ Excel `.xlsx`, validate toàn bộ dòng trước khi tạo, hỗ trợ nhập một phần.
  - `POST /api/mst_cardtype/export`: Xuất danh sách loại thẻ ra Excel, sử dụng từ điển 4 cột, sắp xếp tăng dần theo `Value`.
  - `POST /api/mst_cardtype/exporttemplate`: Xuất file mẫu Excel gồm 3 cột tiêu đề, không truy vấn CSDL.

*(Các controller khác sẽ được bổ sung vào mục lục này trong các lượt tài liệu hóa tiếp theo).*

### 2.3. Quy trình & Vận hành (`docs/quy-trinh/`)

- Thư mục do phân hệ tài liệu quy trình (doc2) quản lý độc lập, bao gồm các kịch bản kiểm thử nghiệp vụ, hướng dẫn vận hành và đối soát.

---

## 3. Quy ước Kỹ thuật Cốt lõi

Mọi module port 1-1 và tài liệu tham chiếu đều tuân thủ các quy tắc bất biến sau:

1. **Chuẩn hóa chuỗi đầu vào theo nguồn**:
   - `StdParam(x)` = `x.Trim().ToUpper()`, chuỗi rỗng chuyển thành `null` (áp dụng cho các trường mã: `DepartmentCode`, `ProvinceCode`, `DistrictCode`, `BrandCode`, `CardType`, `OrgID`, `MST`...).
   - `StdStr(x)` = `x.Trim().Normalize()`, chuỗi rỗng chuyển thành `null` (áp dụng cho tên, mô tả, ghi chú).
   - `StdFlag(x)` = `"1"` nếu bằng `"1"`, mọi giá trị khác chuyển thành `"0"`.
2. **Xử lý so sánh chuỗi trên SQLite / PostgreSQL**:
   - SQL Server nguồn sử dụng collation không phân biệt hoa thường (`CI`).
   - SQLite và PostgreSQL phân biệt hoa thường với toán tử so sánh chuỗi mặc định. Do đó, toàn bộ truy vấn lọc chuỗi bắt buộc phải hạ chữ cả hai vế qua [`PortQuery`](../Data/PortQuery.cs) kết hợp với `EF.Functions.Like(...)`.
3. **Chuẩn hóa phản hồi mã lỗi**:
   - Lỗi nghiệp vụ trả về HTTP 400 kèm định dạng chuẩn `{ Success = false, ErrorCode = "ErridnLoyalty.<MãLỗi>", Params = { ... } }` thông qua helper [`PortResult.Fail`](../Data/PortResult.cs#L17-L23).
4. **Phân vùng dữ liệu và cô lập module**:
   - Mỗi bảng và controller port nằm trong từng cặp file riêng biệt (`Models/Port/<Table>.cs` và `Controllers/Port/<Controller>Port.cs`).
   - Bảng được cấu hình bằng `IEntityTypeConfiguration<T>` tự động quét bởi assembly, không sửa đổi trực tiếp [`AppDbContext.cs`](../Data/AppDbContext.cs).
