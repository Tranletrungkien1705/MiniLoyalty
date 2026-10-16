# Tài liệu Hệ thống MiniLoyalty — Tham chiếu Kiến trúc & API Port 1-1

| Thông tin | Giá trị |
|---|---|
| **Phạm vi** | Toàn bộ tài liệu tham chiếu kiến trúc và API port 1-1 MiniLoyalty |
| **Cập nhật** | 2026-10-16 |
| **Phiên bản mã** | `ed28f8f` |
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
- [docs/kien-truc/HA_TANG_CHUNG.md](kien-truc/HA_TANG_CHUNG.md): Kiến trúc hạ tầng dùng chung cho controller port 1-1:
  - Chuẩn hóa JSON PascalCase qua `PortJson` và `PortJsonResultFilter` (chỉ áp dụng cho namespace `MiniLoyalty.Controllers.Port`).
  - Xử lý ngoại lệ tập trung qua `PortExceptionFilter` chuyển đổi ngoại lệ thành HTTP 400 `PortResult.Fail`.
  - Hỗ trợ hạ chữ Unicode tiếng Việt trên SQLite qua `SqliteUnicodeLowerInterceptor`.

### 2.2. Tham chiếu API (`docs/api/`)

Tài liệu chi tiết từng controller API được port 1-1 từ tầng gate của hệ thống nguồn:

- [docs/api/Mst_DepartmentController.md](api/Mst_DepartmentController.md): Danh mục phòng ban (`Mst_Department`) — Gồm 9 endpoint đã hoàn thành (#122 - #130):
  - `POST /api/mst_department/search`: Tìm kiếm danh mục phòng ban phân trang, lọc theo từ khóa, tổ chức và cờ hoạt động.
  - `POST /api/mst_department/getallactive`: Lấy danh sách tất cả phòng ban đang hoạt động (`FlagActive = '1'`) theo tổ chức.
  - `POST /api/mst_department/getbydepartmentcode`: Tra cứu chi tiết một phòng ban theo mã (`DepartmentCode`).
  - `POST /api/mst_department/create`: Thêm mới phòng ban, tự sinh sequence mã `DPMCODE...`, tự động cập nhật phân cấp BU toàn bảng.
  - `POST /api/mst_department/update`: Cập nhật tên, mô tả và trạng thái phòng ban (không thay đổi phòng ban cấp trên).
  - `POST /api/mst_department/delete`: Xóa một phòng ban đơn lẻ theo mã phòng ban và mã tổ chức.
  - `POST /api/mst_department/deletemultiple`: Xóa nhiều phòng ban theo danh sách mã, hỗ trợ hành vi xóa một phần (partial delete).
  - `POST /api/mst_department/import`: Nhập dữ liệu danh mục phòng ban từ file Excel `.xlsx`, sinh mã GUID, hỗ trợ nhập một phần (partial import).
  - `POST /api/mst_department/export`: Xuất danh sách phòng ban ra tập tin Excel `.xlsx` với từ điển 4 cột theo thứ tự nguồn.

- [docs/api/Mst_BirthPolicyController.md](api/Mst_BirthPolicyController.md): Chính sách sinh nhật (`Mst_BirthPolicy`, `Mst_BirthPolicyDtl`) — Gồm 7 endpoint đã hoàn thành (#70 - #76):
  - `POST /api/mst_birthpolicy/search`: Tìm kiếm danh mục chính sách sinh nhật phân trang, tự động tính toán cờ hiệu lực `FlagShow` theo giờ VN (UTC+7).
  - `POST /api/mst_birthpolicy/getallactive`: Lấy danh sách tất cả chính sách đang hoạt động, bọc trong `{ Lst_Mst_BirthPolicy }`.
  - `POST /api/mst_birthpolicy/getbybirthpolicyno`: Tra cứu chi tiết chính sách kèm danh sách mức ưu đãi theo từng hạng thẻ, trả về null khi mã rỗng.
  - `POST /api/mst_birthpolicy/save`: Thêm mới chính sách sinh nhật, tự gán ngày kết thúc 2100-01-01, lưu header và detail trong 1 transaction.
  - `POST /api/mst_birthpolicy/update`: Cập nhật thông tin chính sách, upsert xóa-thêm lại toàn bộ danh sách detail theo hạng thẻ trong 1 transaction.
  - `POST /api/mst_birthpolicy/delete`: Xóa chính sách theo cơ chế SaveX `bIsDelete = true`, xóa đồng thời header và detail trong 1 transaction.
  - `POST /api/mst_birthpolicy/export`: Xuất danh sách chính sách ra file Excel 4 cột từ điển, báo lỗi khi không có dữ liệu.

- [docs/api/Mst_DisciplineController.md](api/Mst_DisciplineController.md): Danh mục hình thức kỷ luật (`Mst_Discipline`) — Gồm 7 endpoint đã hoàn thành (#132 - #138):
  - `POST /api/mst_discipline/search`: Tìm kiếm hình thức kỷ luật phân trang, áp dụng toán tử bằng tuyệt đối `=` cho `FlagActive`.
  - `POST /api/mst_discipline/getbydisciplinecode`: Tra cứu chi tiết một hình thức kỷ luật theo mã, trả về `null` khi mã rỗng.
  - `POST /api/mst_discipline/getallactive`: Lấy danh sách tất cả hình thức kỷ luật đang hoạt động, trả về mảng phẳng trực tiếp.
  - `POST /api/mst_discipline/create`: Thêm mới hình thức kỷ luật, tự sinh sequence `DISCIPLINECODE...` độc lập ghi đè mã client.
  - `POST /api/mst_discipline/update`: Cập nhật thông tin hình thức kỷ luật, tự động hạ cờ về `'0'` nếu thiếu `FlagActive`.
  - `POST /api/mst_discipline/delete`: Xóa đơn lẻ hình thức kỷ luật, bảo lưu trung thực mã lỗi typo của nguồn `invalid_departmentcode`.
  - `POST /api/mst_discipline/deletemultiple`: Xóa nhiều hình thức kỷ luật theo từng transaction riêng biệt (xóa một phần khi có lỗi).

- [docs/api/Mst_PromotionProgramController.md](api/Mst_PromotionProgramController.md): Danh mục chương trình khuyến mại (`Mst_PromotionProgram`, `Dtl`, `Spec`) — Gồm 8 endpoint đã hoàn thành (#205 - #212):
  - `POST /api/mst_promotionprogram/search`: Tìm kiếm chương trình khuyến mại phân trang, mặc định `Ft_PageSize = 0` trả về mảng rỗng theo nguồn.
  - `POST /api/mst_promotionprogram/getallactive`: Lấy danh sách tất cả chương trình khuyến mại, tái hiện hành vi bỏ qua lọc cờ của nguồn.
  - `POST /api/mst_promotionprogram/getbyprprogramcode`: Tra cứu chi tiết chương trình cùng 2 danh sách bảng con Dtl và Spec, trả về null khi mã rỗng.
  - `POST /api/mst_promotionprogram/save`: Thêm mới chương trình theo upsert xóa-ghi lại trong 1 transaction, ép cờ kích hoạt `FlagActive = '1'`, kiểm tra đại lý hợp lệ.
  - `POST /api/mst_promotionprogram/update`: Cập nhật chương trình, tự động chốt ngày kết thúc bằng giờ máy chủ khi ngừng kích hoạt.
  - `POST /api/mst_promotionprogram/delete`: Xóa chương trình, chặn xóa khi chương trình chưa bắt đầu hiệu lực (EffDateStart > DateTime.UtcNow), bảo lưu mã lỗi typo nguồn `invalid_BirthPolicyNo`.
  - `POST /api/mst_promotionprogram/export`: Xuất danh sách chương trình ra Excel 7 cột từ điển, bỏ qua lọc FlagActive theo nguồn.
  - `POST /api/mst_promotionprogram/exporttemplate`: Xuất file mẫu Excel gồm 4 cột tiêu đề, không truy vấn CSDL.

- [docs/api/Mst_DealerCPController.md](api/Mst_DealerCPController.md): Danh mục đại lý phân phối (`Mst_DealerCP`, `Map_DealerCP_Org`) — Gồm 10 endpoint đã hoàn thành (#110 - #114, #116 - #118, #120 - #121):
  - `POST /api/mst_dealercp/search`: Tìm kiếm danh mục đại lý phân trang theo từ khóa và trạng thái hoạt động (toán tử `IN`).
  - `POST /api/mst_dealercp/getallactive`: Lấy danh sách tất cả đại lý đang hoạt động, không lọc OrgID theo nguồn, enrich liên kết tổ chức.
  - `POST /api/mst_dealercp/getbydlcpcode`: Tra cứu chi tiết đại lý theo mã và danh sách tổ chức liên kết.
  - `POST /api/mst_dealercp/getbyorgid`: Lấy danh sách đại lý đang hoạt động thuộc về một tổ chức qua bảng liên kết `Map_DealerCP_Org`.
  - `POST /api/mst_dealercp/createold`: Thêm mới đại lý trực tiếp, kiểm tra đại lý cha, tự động tính toán phân cấp BU toàn bảng qua `UpdBU`.
  - `POST /api/mst_dealercp/update`: Cập nhật thông tin đại lý, không thay đổi đại lý cha, không tính lại BU.
  - `POST /api/mst_dealercp/delete`: Xóa đơn lẻ đại lý, kiểm tra giả lập khóa ngoại, kiểm tra mã rỗng tại gate.
  - `POST /api/mst_dealercp/deletemultiple`: Xóa nhiều đại lý theo từng transaction riêng biệt (xóa một phần khi có lỗi).
  - `POST /api/mst_dealercp/export`: Xuất danh sách đại lý ra file Excel 5 cột từ điển, báo lỗi khi không có dữ liệu.
  - `POST /api/mst_dealercp/exporttemplate`: Xuất file mẫu Excel gồm 6 cột tiêu đề (kèm cột MST và Email), không truy vấn CSDL.
  - *(Ghi chú: 2 endpoint `Create` #115 và `Import` #119 CHƯA PORT do phụ thuộc gọi sang module `Mst_NNT`)*.

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

- [docs/api/Mst_CarModelController.md](api/Mst_CarModelController.md): Danh mục dòng xe ô tô (`Mst_CarModel`) — Gồm 10 endpoint đã hoàn thành (#97 - #106):
  - `POST /api/mst_carmodel/search`: Tìm kiếm danh mục dòng xe phân trang theo từ khóa và trạng thái hoạt động (toán tử `=`).
  - `POST /api/mst_carmodel/getallactive`: Lấy danh sách tất cả dòng xe đang hoạt động, trả về mảng phẳng trực tiếp.
  - `POST /api/mst_carmodel/getbymodelcode`: Tra cứu chi tiết dòng xe theo mã, không lọc trạng thái hoạt động.
  - `POST /api/mst_carmodel/create`: Thêm mới dòng xe, kiểm tra tồn tại mã và kiểm tra trạng thái hoạt động của hãng xe cha `Mst_CarBrand`.
  - `POST /api/mst_carmodel/update`: Cập nhật thông tin dòng xe, kiểm tra tồn tại và kiểm tra hãng xe cha.
  - `POST /api/mst_carmodel/delete`: Xóa đơn lẻ dòng xe theo mã, kiểm tra mã rỗng tại gate.
  - `POST /api/mst_carmodel/deletemultiple`: Xóa nhiều dòng xe theo danh sách trong từng transaction riêng (xóa một phần).
  - `POST /api/mst_carmodel/import`: Nhập danh mục từ Excel `.xlsx`, validate toàn bộ dòng trước khi tạo, hỗ trợ nhập một phần.
  - `POST /api/mst_carmodel/export`: Xuất danh sách dòng xe ra Excel, sử dụng từ điển 4 cột, báo lỗi khi không có dữ liệu.
  - `POST /api/mst_carmodel/exporttemplate`: Xuất file mẫu Excel gồm 3 cột tiêu đề, không truy vấn CSDL.

- [docs/api/Mst_CertificateGroupController.md](api/Mst_CertificateGroupController.md): Danh mục nhóm chứng chỉ (`Mst_CertificateGroup`) — Gồm 2 endpoint đã hoàn thành (#107, #108):
  - `POST /api/mst_certificategroup/search`: Tìm kiếm nhóm chứng chỉ phân trang theo từ khóa và trạng thái hoạt động (toán tử `=`).
  - `POST /api/mst_certificategroup/getallactive`: Lấy toàn bộ nhóm chứng chỉ đang hoạt động, trả về mảng phẳng trực tiếp.

- [docs/api/Mst_CountryController.md](api/Mst_CountryController.md): Danh mục quốc gia (`Mst_Country`) — Gồm 1 endpoint đã hoàn thành (#109):
  - `POST /api/mst_country/getallactive`: Lấy toàn bộ danh sách quốc gia đang hoạt động, không chứa cột `NetworkID`.

- [docs/api/Mst_WardController.md](api/Mst_WardController.md): Danh mục phường/xã (`Mst_Ward`) — Gồm 2 endpoint đã hoàn thành (#246, #247):
  - `POST /api/mst_ward/getallactive`: Lấy toàn bộ phường/xã đang hoạt động theo khóa ghép 3 cấp `(ProvinceCode, DistrictCode, WardCode)`.
  - `POST /api/mst_ward/getwarddetail`: Tra cứu chi tiết danh sách phường/xã theo tỉnh và huyện, bảo lưu hành vi không lọc `FlagActive` theo nguồn.

- [docs/api/Mst_PolicyMoneyToPointServiceController.md](api/Mst_PolicyMoneyToPointServiceController.md): Chính sách quy đổi tiền sang điểm dịch vụ (`Mst_PolicyMoneyToPointService`, `Dtl`) — Gồm 3 endpoint đã hoàn thành (#194 - #196):
  - `POST /api/mst_policymoneytopointservice/getallactive`: Lấy toàn bộ chính sách quy đổi, không lọc cờ hoạt động theo nguồn.
  - `POST /api/mst_policymoneytopointservice/getbypolicycode`: Tra cứu chi tiết chính sách kèm danh sách cấu hình từng hạng thẻ (`Lst_Mst_PolicyMoneyToPointService`).
  - `POST /api/mst_policymoneytopointservice/save`: Thêm mới hoặc cập nhật chính sách và chi tiết trong 1 transaction, tự sinh sequence mã chính sách.

- [docs/api/Mst_RankPolicyController.md](api/Mst_RankPolicyController.md): Chính sách xếp hạng thẻ hội viên (`Mst_RankPolicy`) — Gồm 4 endpoint đã hoàn thành (#227 - #230):
  - `POST /api/mst_rankpolicy/search`: Tìm kiếm ma trận xếp hạng phân trang theo nghiệp vụ 2 bước outer join từ `Mst_CardType`, không lọc KeyWord theo nguồn.
  - `POST /api/mst_rankpolicy/getallactive`: Lấy toàn bộ ma trận xếp hạng đang hoạt động sắp xếp theo `mct.Value ASC`.
  - `POST /api/mst_rankpolicy/getbyrankpolicycode`: Tra cứu chi tiết chính sách, bảo lưu hành vi trả về phần tử đầu tiên của bảng outer join.
  - `POST /api/mst_rankpolicy/save`: Lưu mới bộ chính sách xếp hạng trong 1 transaction, xóa trắng cấu hình cũ toàn bảng theo nguồn.

- [docs/api/Mst_EthnicCategoryController.md](api/Mst_EthnicCategoryController.md): Danh mục dân tộc (`Mst_EthnicCategory`) — Gồm 2 endpoint đã hoàn thành (#149, #150):
  - `POST /api/mst_ethniccategory/search`: Tìm kiếm danh mục dân tộc phân trang theo từ khóa và trạng thái hoạt động (toán tử `=`).
  - `POST /api/mst_ethniccategory/getallactive`: Lấy toàn bộ danh sách dân tộc đang hoạt động, trả về mảng phẳng trực tiếp.

- [docs/api/Mst_ExpenseTypeController.md](api/Mst_ExpenseTypeController.md): Danh mục loại chi phí (`Mst_ExpenseType`) — Gồm 1 endpoint đã hoàn thành (#151):
  - `POST /api/mst_expensetype/getallactive`: Lấy toàn bộ loại chi phí đang hoạt động, sắp xếp theo thời điểm cập nhật `LogLUDTimeUTC DESC`.

- [docs/api/Mst_ParamSysController.md](api/Mst_ParamSysController.md): Danh mục tham số hệ thống (`Mst_ParamSys`) — Gồm 1 endpoint đã hoàn thành (#187):
  - `POST /api/mst_paramsys/getallactive`: Lấy toàn bộ danh sách tham số hệ thống đang hoạt động, sắp xếp theo `LogLUDTimeUTC DESC`.

- [docs/api/Mst_PurchaseTypeController.md](api/Mst_PurchaseTypeController.md): Danh mục loại mua hàng (`Mst_PurchaseType`) — Gồm 1 endpoint đã hoàn thành (#222):
  - `POST /api/mst_purchasetype/getallactive`: Lấy toàn bộ loại mua hàng theo 4 hằng số hệ thống, bảo lưu hành vi không lọc cờ theo nguồn.

- [docs/api/Map_DealerCP_OrgController.md](api/Map_DealerCP_OrgController.md): Liên kết đại lý - tổ chức chi nhánh (`Map_DealerCP_Org`) — Gồm 1 endpoint đã hoàn thành (#65):
  - `POST /api/map_dealercp_org/getbydlcpcode`: Tra cứu thông tin liên kết tổ chức và mã số thuế chi nhánh của đại lý theo `DLCPCode` (nghiệp vụ 2 bước inner join `Mst_NNT`).

*(Toàn bộ 20 controller API port 1-1 đã hoàn tất 100% đều đã được tài liệu hóa chi tiết).*

### 2.3. Quy trình & Vận hành (`docs/quy-trinh/`)

Thư mục do phân hệ tài liệu quy trình (`doc2`) quản lý độc lập:
- [docs/quy-trinh/QUY_TRINH_PORT_1_1.md](quy-trinh/QUY_TRINH_PORT_1_1.md): Quy trình chuẩn port 1-1, phân định vai trò (Lead, QA, Docs, Orchestrator, Reviewer, Worker), quy tắc "chỉ làm theo tài liệu — xung đột thì dừng", hạ tầng dùng chung và Definition of Done.
- [docs/quy-trinh/SO_GAP.md](quy-trinh/SO_GAP.md): Sổ đăng ký và phân loại toàn bộ GAP kỹ thuật, phân quyền, dữ liệu demo, định dạng Excel, bảo mật `ex.Message` và JSON PascalCase.
- [docs/quy-trinh/TRUY_VET.md](quy-trinh/TRUY_VET.md): Ma trận truy vết toàn bộ 121 hạng mục hoàn thành đối chiếu về nguồn Gate, API, Biz LIVE và commit Git (xác nhận 100% trong `git log`).
- [docs/quy-trinh/NHAT_KY_BAY.md](quy-trinh/NHAT_KY_BAY.md): Sổ tay ghi nhận 25 bẫy kỹ thuật thực tế và bảng nhật ký cải tiến tích lũy từ môi trường Windows/PowerShell/SQLite.
- [docs/quy-trinh/BAO_CAO_TIEN_DO.md](quy-trinh/BAO_CAO_TIEN_DO.md): Báo cáo tiến độ đo lường độc lập theo 8 nhóm tiền tố và 67 controller (121/337, đạt 35.91%).

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
