# SỔ ĐĂNG KÝ GAP (GAP REGISTER) — MiniLoyalty

> **Phạm vi**: Toàn bộ các điểm sai khác, thiếu hụt (GAP) được ghi nhận trong quá trình port 1-1 các hạng mục từ 2022.7.Loyalty sang MiniLoyalty.
> **Cập nhật**: 2026-10-16 11:15
> **Nguồn dữ liệu**: Quét tự động toàn bộ 121 thư mục nhật ký tại .parity-run\logs\*\write_r*.txt (112 file có mục GAP:), kết hợp .parity-run\skipped.json.
> **Trạng thái**: Nháp — chờ review

---

## 1. Bảng thống kê GAP theo loại

| STT | Loại GAP | Số lượng phát hiện | Tác động chính | Hướng xử lý đề xuất | Trạng thái tổng quát |
|---|---|---|---|---|---|
| 1 | Phân quyền (Auth / Ability) | 97 | Chưa kiểm tra quyền chi tiết và ngữ cảnh user đăng nhập | Bổ sung middleware xác thực JWT / Claims khi có hệ thống SSO | Mở (chờ SSO) |
| 2 | Ghi nhật ký (Audit Log) | 98 | Bỏ qua ghi log gọi API (Request/Return Log) | Bổ sung middleware audit log hoặc Serilog tập trung | Mở (chờ audit module) |
| 3 | Định dạng file Excel | 19 | Phụ thuộc DLL nguồn (ExcelExport / ExcelImportNew) | Chuẩn hóa template Excel bằng bộ kiểm thử đối chiếu với file mẫu thực tế của hệ thống nguồn | Mở (chờ xác nhận mẫu) |
| 4 | Dữ liệu demo (Seeder Mock) | 18 | Dữ liệu danh mục tự sinh để phục vụ test round-trip | Import dữ liệu thật từ SQL Server dump khi bàn giao | Mở (dùng trong test) |
| 5 | Phụ thuộc chưa port | 17 | Thiếu bảng liên quan (Mst_NNT, Prm_*, Staff_*) và FK | Hoãn kiểm tra FK; kích hoạt lại sau khi port xong các bảng cha | Mở (chờ thứ tự port) |
| 6 | Thiếu DDL / Schema suy luận | 15 | Bảng thiếu script DDL gốc, kiểu cột phải suy luận | Chuẩn hóa migration chính thức từ DB SQL Server thật | Đã xử lý tạm thời / Mở DDL |
| 7 | Bất thường nghiệp vụ nguồn | 8 | Lỗi logic tiềm ẩn ở nguồn (rows[0], xóa bảng, hardcode) | Phòng vệ mã port, bọc transaction, báo cáo để xác nhận | Đã phòng vệ trong mã port |
| 8 | Dịch vụ ngoài (External Services) | 5 | Phụ thuộc iNOS master server hoặc Hyundai DMS | Cấu hình cờ feature toggle hoặc xây dựng mock adapter | Mở (CAN_XEM_TAY) |

> *Ghi chú thống kê*: Một hạng mục trong file log có thể đồng thời ghi nhận nhiều loại GAP (ví dụ: vừa thiếu phân quyền, vừa thiếu ghi log, vừa thiếu DDL). Tổng số lượt ghi nhận là 277 lượt trên 112 hạng mục có báo cáo GAP.

---

## 2. Phân tích chi tiết từng nhóm GAP & Hướng xử lý

### 2.1. Phân quyền và ngữ cảnh người dùng (Phân quyền)
- **Mô tả**: Hầu hết các action nguồn đều gọi CheckGatewayAuthentication(), CheckAuthorize(ref orgUserUI), Sys_User_CheckAuthorize(), hoặc truy vấn quyền đại lý myCache_Mst_PromotionProgram_ViewAbility_Get. Khi port sang MiniLoyalty, các kiểm tra này được đánh dấu Đ (đơn giản hóa có chủ đích) và tạm thời cho qua.
- **Tác động**: Mọi client gọi tới endpoint đều được thực thi mà không bị chặn phân quyền; các trường dữ liệu audit (`LogLUBy`, `OrgID`, `NetworkID`) tạm thời nhận giá trị cố định hoặc lấy từ cấu hình: `LogLUBy = "port"`, `OrgID` theo `Biz_OrgID`, `NetworkID` theo `Biz_NetworkID` (mặc định `"1001"`) — xem `Controllers/Port/Mst_DepartmentControllerPort.cs`.
- **Hướng xử lý**: Tạo TenantContext và middleware xác thực tập trung. Khi có token hợp lệ, trích xuất OrgID, NetworkID, UserCode đưa vào HttpContext.Items để các controller đọc tự động.

### 2.2. Ghi nhật ký hệ thống (Ghi log)
- **Mô tả**: Nguồn gọi ProcessBizReq_OutSide, ProcessBizReturn_OutSide, ghi vào bảng Sys_RequestLog / Sys_ReturnLog. Trong mã port, các khối lệnh này được lược bỏ để giảm thiểu phụ thuộc hạ tầng.
- **Tác động**: Không ghi lại vết request/response nhị phân tại database. Tuy nhiên, luồng nghiệp vụ cốt lõi không bị ảnh hưởng.
- **Hướng xử lý**: Tích hợp serilog ghi log dạng có cấu trúc (structured logging) ra console hoặc file, sẵn sàng đẩy lên OpenTelemetry/Seq mà không cần bảng DB riêng.

### 2.3. Định dạng và xử lý tệp Excel (Định dạng file Excel)
- **Mô tả**: Nguồn sử dụng các phương thức đóng gói sẵn trong DLL nhị phân (idn.Loyalty.Common\Utils\ExcelExport.cs, ExcelImportNew). Do không tái sử dụng trực tiếp DLL .NET Framework cũ, mã port sử dụng lớp tiện ích `Data/PortExcel.cs` dựa trên thư viện ClosedXML (đọc/ghi `.xlsx`).
- **Tác động**: File xuất ra hoặc file template có thể có khác biệt nhỏ về style hiển thị hoặc tiêu đề cột so với phiên bản cũ.
- **Hướng xử lý**: Thu thập file Excel mẫu xuất ra từ môi trường cũ; viết bộ unit test kiểm tra đối chiếu từng sheet, tên cột và kiểu dữ liệu trong file import.

### 2.4. Dữ liệu giả lập (Dữ liệu demo)
- **Mô tả**: Các bảng danh mục (Mst_Province, Mst_District, Mst_Ward, Mst_CarBrand, Mst_CardType, Mst_BirthPolicy...) sử dụng IPortSeeder để nạp dữ liệu mẫu ban đầu nhằm đảm bảo kiểm thử round-trip chạy được.
- **Tác động**: Dữ liệu chỉ mang tính minh họa, chưa có toàn bộ danh mục thực tế của khách hàng.
- **Hướng xử lý**: Tách bạch seeder môi trường Development và seeder môi trường Production; cung cấp script ETL đồng bộ dữ liệu thật từ SQL Server sang SQLite/PostgreSQL.

### 2.5. Phụ thuộc bảng chưa port và Khóa ngoại (Phụ thuộc chưa port)
- **Mô tả**: Một số endpoint xóa (Delete, DeleteMultiple) ở nguồn có kiểm tra khóa ngoại (FK) sang các bảng khác trước khi cho phép xóa (ví dụ: Mst_Department kiểm tra Staff_MapDepartment, Train_CourseDepartment; Mst_CardType kiểm tra các bảng giao dịch thẻ; Mst_DealerCP kiểm tra Map_QueryDealer_Member, Prm_CarNew). Vì các bảng này chưa port nên mã port chưa thể kích hoạt các câu lệnh kiểm tra này.
- **Tác động**: Nếu xóa danh mục cha, các bản ghi con ở hệ thống cũ có thể bị mất tính toàn vẹn tham chiếu.
- **Hướng xử lý**: Lưu vết danh sách FK; sau khi hoàn thành các nhóm bảng giao dịch và người dùng, quay lại bổ sung các guard kiểm tra FK trước khi xóa.

### 2.6. Thiếu kịch bản DDL chính thức (Thiếu DDL / Schema)
- **Mô tả**: Nhiều bảng danh mục không tìm thấy file .sql tạo bảng trong thư mục Migrate\. Tên cột, kiểu dữ liệu và khóa chính được suy đoán từ thuộc tính C# và mã nguồn gọi SQL.
- **Tác động**: Nguy cơ sai lệch kiểu dữ liệu hoặc thiếu các chỉ mục quan trọng. Đặc biệt, SQLite với CREATE TABLE IF NOT EXISTS không tự động thay đổi khóa chính nếu DB đã tồn tại (như trường hợp #143 Mst_District).
- **Hướng xử lý**: Trích xuất schema trực tiếp từ database SQL Server đang chạy thật (sys.columns, sys.tables) để đối chiếu và chuẩn hóa file migration EF Core cuối cùng.

### 2.7. Bất thường nghiệp vụ nguồn (Bất thường nghiệp vụ)
- **Mô tả & Hướng xử lý**:
  1. #110 Mst_DealerCPController/Search: Nguồn dùng DISTINCT trên câu truy vấn có cột IDENTITY. Hướng xử lý: chuẩn hóa SELECT bỏ identity khi cần distinct.
  2. #112 Mst_DealerCPController/GetByDLCPCode: Nguồn hardcode tài khoản hệ thống. Hướng xử lý: loại bỏ tài khoản hardcode, thay bằng cơ chế cấu hình service account an toàn.
  3. #187 Mst_ParamSysController/GetAllActive: Khi trùng LogLUDTimeUTC, thứ tự bản ghi không xác định. Hướng xử lý: thêm sắp xếp thứ cấp theo khóa chính ParamCode.
  4. #196 Mst_PolicyMoneyToPointServiceController/Save: Logic nguồn chép nhầm kiểm tra BirthPolicy. Hướng xử lý: giữ tương thích nhưng đánh dấu cảnh báo nghiệp vụ.
  5. #209 Mst_PromotionProgramController/Update: Xử lý lệch múi giờ giữa máy chủ và UTC. Hướng xử lý: chuẩn hóa lưu trữ DateTimeOffset / UTC thống nhất.
  6. #229 Mst_RankPolicyController/GetByRankPolicyCode: Nguồn truy cập trực tiếp rows[0]. Hướng xử lý: kiểm tra danh sách rỗng trước khi truy cập phần tử đầu tiên để chống crash.
  7. #230 Mst_RankPolicyController/Save: Nguồn thực hiện xóa toàn bộ bảng rồi chèn lại. Hướng xử lý: bọc trong transaction chặt chẽ và ghi chú cảnh báo tương tranh.

### 2.8. Dịch vụ ngoài (Dịch vụ ngoài / CAN_XEM_TAY)
- **Mô tả**: Các hạng mục phụ thuộc dịch vụ ngoài được ghi nhận tại .parity-run\skipped.json:
  1. #181 Mst_OrgController/GetAllOrg: Phụ thuộc API iNOS Master Server (WA_OS_Inos_Org_GetMyOrgList).
  2. #182 Mst_OrgController/GetOrgsNotInSolution: Phụ thuộc API iNOS Master Server.
  3. #068 Map_QueryDealer_MemberController/Create: Phụ thuộc Web Service Hyundai DMS (WSDMS CarSv_Ser_CustomerCar_Get, Ser_CustomerCar_SalesCreate).
- **Hướng xử lý**: Cần người dùng phê duyệt phương án: (a) Xây dựng bảng mirror đồng bộ định kỳ; (b) Xây dựng mock service; hoặc (c) Loại bỏ endpoint nếu không còn dùng hệ thống ngoài.

---

## 3. Sổ đăng ký chi tiết từng hạng mục (112 mục)

| Hạng mục # | Tên hạng mục | Nhóm GAP phân loại | Nội dung chi tiết được ghi nhận | Trạng thái |
|---|---|---|---|---|
| 65 | Map_DealerCP_OrgController_GetByDLCPCode | Phân quyền, Ghi log, Bất thường nghiệp vụ | thu tu khi trung LogLUDTimeUTC; ability nguoi dung; phan quyen; log | Đã phòng vệ trong code |
| 66 | Map_QueryDealer_MemberController_Search | Phân quyền, Ghi log, Dữ liệu demo, Thiếu DDL / Schema | ability theo dai ly user (chờ có Sys_User); khoa suy luan (DLCPCode, MemberNo); du lieu demo; phan quyen (CheckGatewayAuthentication, CheckAuthorize); log (RequestLog/ReturnLog) | Mở |
| 67 | Map_QueryDealer_MemberController_SearchForRePort | Phân quyền, Ghi log | ability theo dai ly user; dinh dang ngay QueryDate; phan quyen; log | Mở |
| 69 | Map_QueryDealer_MemberController_ExportExcel | Phân quyền, Ghi log, Định dạng file Excel | dinh dang o Excel (DLL nguon) can xac nhan; ability; phan quyen; log | Mở |
| 70 | Mst_BirthPolicyController_Search | Phân quyền, Ghi log, Dữ liệu demo, Phụ thuộc chưa port | FK chua gia lap; du lieu demo; phan quyen; log | Mở |
| 71 | Mst_BirthPolicyController_GetAllActive | Phân quyền, Ghi log | phan quyen; log | Mở |
| 72 | Mst_BirthPolicyController_GetByBirthPolicyNo | Phân quyền, Ghi log | phan quyen; log | Mở |
| 73 | Mst_BirthPolicyController_Save | Phân quyền, Ghi log, Dữ liệu demo | FlagPoint/FlagVoucher khong co script; Mst_ParamPromotionType du lieu demo; NetworkID/LogLUBy tu user; phan quyen; log | Mở |
| 74 | Mst_BirthPolicyController_Update | Phân quyền, Ghi log | UpdateBy tu user; phan quyen; log | Mở |
| 75 | Mst_BirthPolicyController_Delete | Phân quyền, Ghi log | phan quyen; log | Mở |
| 76 | Mst_BirthPolicyController_Export | Định dạng file Excel | dinh dang o Excel (DLL nguon) can xac nhan | Mở |
| 77 | Mst_CarBrandController_Search | Phân quyền, Ghi log, Dữ liệu demo, Thiếu DDL / Schema | khong co DDL; du lieu demo; phan quyen; log | Mở |
| 78 | Mst_CarBrandController_GetAllActive | Phân quyền, Ghi log | phan quyen; log | Mở |
| 79 | Mst_CarBrandController_GetByBrandCode | Phân quyền, Ghi log | phan quyen; log Chi tiết: - Thêm MstCarBrandGetByBrandCodeReq (body { BrandCode }) và action [HttpPost("getbybrandcode")] — mô phỏng y hệt getbycardtype (#89): rỗng → Data = null không truy vấn; ngược lại lấy 1 dòng (RecordStart "0", RecordCount "1") và trả rows[0] hoặc null. - Thêm tham số string? brandCodeEqual = null vào GetXAsync (dùng PortQuery.Equal + EF.Functions.Like), có comment tiếng Việt có dấu giải thích nhánh OrgID chết và FlagActive không chạy. - Build: 0 Error (1 warning EF1002 có sẵn ở Seeder.cs, không liên quan). Test: 5/5 pass. - Chạy thật (PORT=8236, <TEMP>\v079.db): 5 ca P2 + search đều đúng như kỳ vọng; đã tắt app và xoá db (port 8236 đã giải phóng). - Không commit/push/reset/checkout; không sửa PARITY_PROGRESS.md; chỉ sửa 1 file trong workspace. | Mở |
| 80 | Mst_CarBrandController_Create | Phân quyền, Ghi log | phan quyen; log; LogLUBy chua co user | Mở |
| 81 | Mst_CarBrandController_Update | Phân quyền, Ghi log | phan quyen; log; LogLUBy chua co user | Mở |
| 82 | Mst_CarBrandController_Delete | Phân quyền, Ghi log | phan quyen; log | Mở |
| 83 | Mst_CarBrandController_DeleteMultiple | Phân quyền, Ghi log | phan quyen; log | Mở |
| 84 | Mst_CarBrandController_Import | Phân quyền, Ghi log | NetworkID tu user; dinh dang file la gia dinh; phan quyen; log | Mở |
| 85 | Mst_CarBrandController_Export | Định dạng file Excel | dinh dang o Excel (DLL nguon) can xac nhan | Mở |
| 86 | Mst_CarBrandController_ExportTemplate | Định dạng file Excel | dinh dang o Excel (DLL nguon) can xac nhan | Mở |
| 87 | Mst_CardTypeController_Search | Phân quyền, Ghi log | phan quyen (CheckGatewayAuthentication/CheckAuthorize/Sys_User_CheckAuthorize/Sys_Access) va log (RequestLog/ReturnLog) bo qua co chu dich (loai Đ). Collation tieng Viet bay #24: KHONG lech — ca E "khởi" va "KHỞI" deu tra dung 1 dong (N). Luu y: ket qua 0 ban dau la do CLIENT PowerShell ma hoa sai UTF-8 khi gui -Body dang string; gui dung UTF-8 bytes thi dung. Ban than SQLite lower() + LIKE xu ly dung ky tu "ở" (U+1EDF). | Mở |
| 88 | Mst_CardTypeController_GetAllActive | Phân quyền, Ghi log | phần quyền (CheckGatewayAuthentication/CheckAuthorize/Sys_User_CheckAuthorize — :119-120, :5484-5501) chưa port; log request/return (ProcessBizReq_OutSide/ProcessBizReturn_OutSide) chưa port. | Mở |
| 89 | Mst_CardTypeController_GetByCardType | Phân quyền, Ghi log | phân quyền (CheckGatewayAuthentication/CheckAuthorize/Sys_User_CheckAuthorize — nguồn :225-226, :5487-5500) chưa port; log (ProcessBizReq_OutSide/ProcessBizReturn_OutSide) chưa port. | Mở |
| 90 | Mst_CardTypeController_Create | Phân quyền, Ghi log | phan quyen; log; LogLUBy chua co user | Mở |
| 92 | Mst_CardTypeController_Delete | Phân quyền, Ghi log, Phụ thuộc chưa port | FK 4 bang chua port; phan quyen; log | Mở |
| 93 | Mst_CardTypeController_DeleteMultiple | Phân quyền, Ghi log, Phụ thuộc chưa port | FK 4 bang chua port; phan quyen; log | Mở |
| 94 | Mst_CardTypeController_Import | Phân quyền, Ghi log | NetworkID tu user; dinh dang file la gia dinh; phan quyen; log | Mở |
| 95 | Mst_CardTypeController_Export | Định dạng file Excel | dinh dang o Excel (DLL nguon) can xac nhan | Mở |
| 96 | Mst_CardTypeController_ExportTemplate | Định dạng file Excel | dinh dang o Excel (DLL nguon) can xac nhan | Mở |
| 97 | Mst_CarModelController_Search | Phân quyền, Ghi log, Dữ liệu demo, Thiếu DDL / Schema | khong co DDL; du lieu demo; phan quyen; log | Mở |
| 98 | Mst_CarModelController_GetAllActive | Phân quyền, Ghi log | phan quyen; log | Mở |
| 99 | Mst_CarModelController_GetByModelCode | Phân quyền, Ghi log | phan quyen; log | Mở |
| 100 | Mst_CarModelController_Create | Phân quyền, Ghi log | phan quyen; log; LogLUBy chua co user | Mở |
| 101 | Mst_CarModelController_Update | Phân quyền, Ghi log | phan quyen; log; LogLUBy chua co user | Mở |
| 102 | Mst_CarModelController_Delete | Phân quyền, Ghi log, Phụ thuộc chưa port | FK Prm_CarRecommendSpec/Prm_CarNewSpec chua port; phan quyen; log | Mở |
| 103 | Mst_CarModelController_DeleteMultiple | Phân quyền, Ghi log, Phụ thuộc chưa port | FK Prm_* chua port; phan quyen; log | Mở |
| 104 | Mst_CarModelController_Import | Phân quyền, Ghi log | NetworkID tu user; dinh dang file la gia dinh; phan quyen; log | Mở |
| 105 | Mst_CarModelController_Export | Định dạng file Excel | dinh dang o Excel (DLL nguon) can xac nhan | Mở |
| 106 | Mst_CarModelController_ExportTemplate | Định dạng file Excel | dinh dang o Excel (DLL nguon) can xac nhan | Mở |
| 107 | Mst_CertificateGroupController_Search | Phân quyền, Ghi log, Dữ liệu demo, Thiếu DDL / Schema | khong co DDL; du lieu demo; phan quyen; log | Mở |
| 108 | Mst_CertificateGroupController_GetAllActive | Phân quyền, Ghi log | phan quyen; log | Mở |
| 109 | Mst_CountryController_GetAllActive | Phân quyền, Ghi log, Dữ liệu demo, Thiếu DDL / Schema | khong co DDL; du lieu demo; phan quyen; log | Mở |
| 110 | Mst_DealerCPController_Search | Phân quyền, Ghi log, Thiếu DDL / Schema, Bất thường nghiệp vụ | khoa Map_DealerCP_Org suy luan; DISTINCT+IDENTITY can xac nhan; phan quyen; log | Đã phòng vệ trong code |
| 111 | Mst_DealerCPController_GetAllActive | Phân quyền, Ghi log | phan quyen; log | Mở |
| 112 | Mst_DealerCPController_GetByDLCPCode | Phân quyền, Ghi log, Thiếu DDL / Schema, Bất thường nghiệp vụ | tai khoan he thong viet cung khong port; phan quyen; log | Đã phòng vệ trong code |
| 113 | Mst_DealerCPController_GetByOrgID | Phân quyền, Ghi log | phan quyen; log | Mở |
| 114 | Mst_DealerCPController_CreateOld | Phân quyền, Ghi log | phan quyen (Sys_User_CheckAuthorize :3114-3131); RequestLog/ReturnLog; LogLUBy chua co user; Biz_NetworkID cau hinh | Mở |
| 116 | Mst_DealerCPController_Update | Phân quyền, Ghi log | phan quyen; log; LogLUBy chua co user | Mở |
| 117 | Mst_DealerCPController_Delete | Phân quyền, Ghi log, Phụ thuộc chưa port | FK Map_QueryDealer_Member, Prm_CarNew, Prm_CarRecommend, Mst_PromotionProgramSpec chua kiem (bang chua port); phan quyen; log | Mở |
| 118 | Mst_DealerCPController_DeleteMultiple | Phân quyền, Ghi log, Phụ thuộc chưa port | FK 4 bang chua port; phan quyen; log | Mở |
| 120 | Mst_DealerCPController_Export | Phân quyền, Ghi log, Định dạng file Excel | dinh dang o Excel cua ExcelExport (DLL nguon) can xac nhan; phan quyen; log | Mở |
| 121 | Mst_DealerCPController_ExportTemplate | Định dạng file Excel | dinh dang o Excel cua ExcelExport (DLL nguon) can xac nhan | Mở |
| 122 | Mst_DepartmentController_Search | Phân quyền, Ghi log, Dữ liệu demo, Thiếu DDL / Schema, Phụ thuộc chưa port | Staff_MapDepartment chua port (ss_QtyStaff=0); khong co DDL; du lieu demo; phan quyen; log | Mở |
| 123 | Mst_DepartmentController_GetAllActive | Phân quyền, Ghi log | OrgID cua user dang nhap; phan quyen; log | Mở |
| 124 | Mst_DepartmentController_GetByDepartmentCode | Phân quyền, Ghi log, Phụ thuộc chưa port | Staff_MapDepartment; phan quyen; log | Mở |
| 125 | Mst_DepartmentController_Create | Phân quyền, Ghi log | OrgID/MST/LogLUBy tu user; goc 'HO' viet cung; phan quyen; log | Mở |
| 126 | Mst_DepartmentController_Update | Phân quyền, Ghi log | LogLUBy tu user; phan quyen; log | Mở |
| 127 | Mst_DepartmentController_Delete | Phân quyền, Ghi log, Phụ thuộc chưa port | Staff_MapDepartment; Train_CourseDepartment FK; phan quyen; log | Mở |
| 128 | Mst_DepartmentController_DeleteMultiple | Phân quyền, Ghi log, Phụ thuộc chưa port | Staff_MapDepartment; phan quyen; log | Mở |
| 129 | Mst_DepartmentController_Import | Phân quyền, Ghi log, Định dạng file Excel | dinh dang file ExcelImportNew (DLL) la gia dinh; OrgID/MST tu user; phan quyen; log | Mở |
| 132 | Mst_DisciplineController_Search | Phân quyền, Ghi log, Dữ liệu demo, Thiếu DDL / Schema | khong co DDL; du lieu demo; phan quyen; log | Mở |
| 133 | Mst_DisciplineController_GetByDisciplineCode | Phân quyền, Ghi log | phan quyen; log | Mở |
| 134 | Mst_DisciplineController_GetAllActive | Phân quyền, Ghi log | phan quyen; log | Mở |
| 135 | Mst_DisciplineController_Create | Phân quyền, Ghi log | LogLUBy tu user; phan quyen; log | Mở |
| 136 | Mst_DisciplineController_Update | Phân quyền, Ghi log | LogLUBy tu user; phan quyen; log | Mở |
| 137 | Mst_DisciplineController_Delete | Phân quyền, Ghi log | phan quyen; log | Mở |
| 138 | Mst_DisciplineController_DeleteMultiple | Phân quyền, Ghi log | phan quyen; log | Mở |
| 139 | Mst_DistrictController_Search | Phân quyền, Ghi log, Dữ liệu demo, Thiếu DDL / Schema | khong co DDL; du lieu demo; phan quyen; log | Mở |
| 140 | Mst_DistrictController_GetDetail | Phân quyền, Ghi log | phan quyen; log | Mở |
| 141 | Mst_DistrictController_GetByProvinceCode | Phân quyền, Ghi log | phan quyen; log | Mở |
| 142 | Mst_DistrictController_GetAllActive | Phân quyền, Ghi log | phan quyen; log | Mở |
| 143 | Mst_DistrictController_Create | Phân quyền, Ghi log, Thiếu DDL / Schema | DB da tao bang voi khoa cu (DistrictCode) se KHONG tu doi khoa (CREATE TABLE IF NOT EXISTS) — can migrate tay neu da deploy; phan quyen; log | Mở |
| 144 | Mst_DistrictController_Update | Phân quyền, Ghi log | phan quyen; log | Mở |
| 145 | Mst_DistrictController_Delete | Phân quyền, Ghi log, Phụ thuộc chưa port | FK Mst_Agent chua port; phan quyen; log | Mở |
| 146 | Mst_DistrictController_Import | Ghi log, Định dạng file Excel | định dạng file ExcelImportNew (DLL) là giả định; phân quyền; log | Mở |
| 147 | Mst_DistrictController_Export | Định dạng file Excel | dinh dang o Excel (DLL nguon) can xac nhan | Mở |
| 148 | Mst_DistrictController_ExportTemplate | Định dạng file Excel | dinh dang o Excel (DLL nguon) can xac nhan | Mở |
| 149 | Mst_EthnicCategoryController_Search | Phân quyền, Ghi log, Dữ liệu demo, Thiếu DDL / Schema | không có DDL (PK = EthnicCode, kiểu cột cần xác nhận); dữ liệu demo; phân quyền (CheckGatewayAuthentication/CheckAuthorize bỏ qua có chủ đích); log (RequestLog/ReturnLog bỏ qua) Ghi chú: lần chạy trước đã tạo sẵn 2 file đúng chuẩn; tôi đọc lại, xác nhận khớp đề bài và mẫu #132, build + test + chạy thật 6 ca đều xanh nên không làm lại từ đầu. App đã tắt, DB tạm đã xóa, git status chỉ còn 2 file untracked như yêu cầu. | Mở |
| 150 | Mst_EthnicCategoryController_GetAllActive | Phân quyền, Ghi log | phan quyen; log | Mở |
| 151 | Mst_ExpenseTypeController_GetAllActive | Phân quyền, Ghi log | CheckAuthorize / user dang nhap; log RequestLog/ReturnLog; Ext tree (da comment o nguon) | Mở |
| 152 | Mst_FamilyStatusController_Search | Phân quyền, Ghi log, Dữ liệu demo, Thiếu DDL / Schema | khong co DDL; du lieu demo; phan quyen; log | Mở |
| 178 | Mst_OrgController_Search | Phân quyền, Ghi log, Phụ thuộc chưa port | Mst_NNT con 60 cot chua lam (thuoc cac hang muc Mst_NNTController/*); he phan quyen CheckGatewayAuthentication/CheckAuthorize/Sys_User_CheckAuthorize/Sys_Access_CheckDenyV30; ghi RequestLog/ReturnLog. | Mở |
| 179 | Mst_OrgController_GetAllActive | Phân quyền, Ghi log | phan quyen; log | Mở |
| 187 | Mst_ParamSysController_GetAllActive | Phân quyền, Ghi log, Bất thường nghiệp vụ | thu tu khi trung LogLUDTimeUTC; phan quyen; log | Đã phòng vệ trong code |
| 194 | Mst_PolicyMoneyToPointServiceController_GetAllActive | Phân quyền, Ghi log, Dữ liệu demo, Phụ thuộc chưa port | FK chua gia lap; du lieu demo; phan quyen; log | Mở |
| 195 | Mst_PolicyMoneyToPointServiceController_GetByPolicyCode | Phân quyền, Ghi log | phan quyen; log | Mở |
| 196 | Mst_PolicyMoneyToPointServiceController_Save | Phân quyền, Ghi log, Bất thường nghiệp vụ | NetworkID/LogLUBy tu user; BirthPolicy check chep nham o nguon; phan quyen; log | Đã phòng vệ trong code |
| 205 | Mst_PromotionProgramController_Search | Phân quyền, Ghi log, Dữ liệu demo, Phụ thuộc chưa port | ability theo dai ly user; FK chua gia lap; du lieu demo; phan quyen; log | Mở |
| 206 | Mst_PromotionProgramController_GetAllActive | Phân quyền, Ghi log | ability; phan quyen; log | Mở |
| 207 | Mst_PromotionProgramController_GetByPrProgramCode | Phân quyền, Ghi log | ability (myCache_Mst_PromotionProgram_ViewAbility_Get tam coi nhu Root thay toan bo); phan quyen (CheckGatewayAuthentication, CheckAuthorize); log (RequestLog/ReturnLog) | Mở |
| 208 | Mst_PromotionProgramController_Save | Phân quyền, Ghi log, Phụ thuộc chưa port | OrgID/NetworkID/LogLUBy tu user; FK CardType Dtl; phan quyen; log | Mở |
| 209 | Mst_PromotionProgramController_Update | Phân quyền, Ghi log, Bất thường nghiệp vụ | gio may chu vs UTC; OrgID/NetworkID/LogLUBy tu user; phan quyen; log | Đã phòng vệ trong code |
| 210 | Mst_PromotionProgramController_Delete | Phân quyền, Ghi log | phan quyen; log | Mở |
| 211 | Mst_PromotionProgramController_Export | Định dạng file Excel | dinh dang o Excel (DLL nguon) can xac nhan | Mở |
| 212 | Mst_PromotionProgramController_ExportTemplate | Định dạng file Excel | dinh dang o Excel (DLL nguon) can xac nhan | Mở |
| 213 | Mst_ProvinceController_Search | Phân quyền, Ghi log, Dữ liệu demo, Thiếu DDL / Schema | khong co DDL; du lieu tinh la demo; phan quyen; log | Mở |
| 214 | Mst_ProvinceController_GetByProvinceCode | Phân quyền, Ghi log | phan quyen; log | Mở |
| 215 | Mst_ProvinceController_GetAllActive | Phân quyền, Ghi log | phan quyen; log | Mở |
| 216 | Mst_ProvinceController_Create | Phân quyền, Ghi log | phan quyen; log; LogLUBy chua co user | Mở |
| 217 | Mst_ProvinceController_Update | Phân quyền, Ghi log | phan quyen; log | Mở |
| 218 | Mst_ProvinceController_Delete | Phân quyền, Ghi log | phan quyen; log | Mở |
| 219 | Mst_ProvinceController_Import | Phân quyền, Ghi log, Định dạng file Excel | dinh dang file ExcelImportNew (DLL) la gia dinh; phan quyen; log | Mở |
| 220 | Mst_ProvinceController_Export | Định dạng file Excel | dinh dang o Excel (DLL nguon) can xac nhan | Mở |
| 221 | Mst_ProvinceController_ExportTemplate | Định dạng file Excel | dinh dang o Excel (DLL nguon) can xac nhan | Mở |
| 222 | Mst_PurchaseTypeController_GetAllActive | Phân quyền, Ghi log, Dữ liệu demo | ten loai mua la demo; phan quyen; log | Mở |
| 227 | Mst_RankPolicyController_Search | Phân quyền, Ghi log, Dữ liệu demo, Phụ thuộc chưa port | FK CardType (Mst_CardType delete can chan); du lieu demo; phan quyen; log | Mở |
| 228 | Mst_RankPolicyController_GetAllActive | Phân quyền, Ghi log | phan quyen; log | Mở |
| 229 | Mst_RankPolicyController_GetByRankPolicyCode | Phân quyền, Ghi log, Bất thường nghiệp vụ | hanh vi [0] cua nguon co the la loi nguon — can nghiep vu xac nhan; phan quyen; log | Đã phòng vệ trong code |
| 230 | Mst_RankPolicyController_Save | Phân quyền, Ghi log, Bất thường nghiệp vụ | NetworkID tu user; LogLUBy; xoa toan bang la hanh vi nguon; phan quyen; log | Đã phòng vệ trong code |
| 246 | Mst_WardController_GetAllActive | Phân quyền, Ghi log, Dữ liệu demo, Thiếu DDL / Schema | khoa Mst_Ward suy luan; khong co DDL; du lieu demo; phan quyen; log | Mở |
| 247 | Mst_WardController_GetWardDetail | Phân quyền, Ghi log | phan quyen; log | Mở |
| 91 | Mst_CardTypeController_Update | Phân quyền, Ghi log | Lượt chạy chạm giới hạn safety steps (50 steps) nên file log write_r0 bị ngắt trước khi ghi mục TOM TAT; mã nguồn đã được Claude review PASS và commit (43e777c) | Đã commit, Mở auth/log |

---

## 4. Lịch sử cập nhật

| Phiên bản | Ngày cập nhật | Người thực hiện | Nội dung thay đổi |
|---|---|---|---|---|
| 1.0 | 2026-10-16 11:15 | doc2 (Quản lý quy trình) | Khởi tạo sổ đăng ký GAP tổng hợp toàn bộ 112 file nhật ký thực tế và skipped.json; phân loại 8 nhóm nghiệp vụ kèm hướng xử lý. |
