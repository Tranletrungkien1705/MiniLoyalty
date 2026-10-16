# SỔ ĐĂNG KÝ GAP (GAP REGISTER) — MiniLoyalty

> **Phạm vi**: Toàn bộ các điểm sai khác, thiếu hụt (GAP) được ghi nhận trong quá trình port 1-1 các hạng mục từ `2022.7.Loyalty` sang `MiniLoyalty`.  
> **Cập nhật**: 2026-10-16 12:12  
> **Nguồn dữ liệu**: Quét tự động toàn bộ 131 thư mục nhật ký tại `.parity-run\logs\*\write_r*.txt` (122 file có mục `GAP:`), kết hợp `.parity-run\skipped.json`, mã nguồn hạ tầng `Data/PortFilters.cs`, và `PARITY_PROGRESS.md` (123 mục Đã làm, 1 Bỏ, 213 Chưa làm).  
> **Trạng thái**: Nháp — chờ review  

---

## 1. Bảng thống kê GAP theo loại

| STT | Loại GAP | Số lượng phát hiện | Tác động chính | Hướng xử lý đề xuất | Trạng thái tổng quát |
|---|---|---|---|---|---|
| 1 | Phân quyền (Auth / Ability) | 107 | Chưa kiểm tra quyền chi tiết và ngữ cảnh user đăng nhập | Bổ sung middleware xác thực JWT / Claims khi có hệ thống SSO | Mở (chờ SSO) |
| 2 | Ghi nhật ký (Audit Log) | 107 | Bỏ qua ghi log gọi API (Request/Return Log) | Bổ sung middleware audit log hoặc Serilog tập trung | Mở (chờ audit module) |
| 3 | Thiếu DDL / Schema suy luận | 23 | Bảng thiếu script DDL gốc, kiểu cột phải suy luận | Chuẩn hóa migration chính thức từ DB SQL Server thật | Đã xử lý tạm thời / Mở DDL |
| 4 | Định dạng file Excel | 21 | Phụ thuộc DLL nguồn (ExcelExport / ExcelImportNew) | Chuẩn hóa template Excel bằng bộ kiểm thử đối chiếu với file mẫu thực tế của hệ thống nguồn | Mở (chờ xác nhận mẫu) |
| 5 | Dữ liệu demo (Seeder Mock) | 24 | Dữ liệu danh mục tự sinh để phục vụ test round-trip | Import dữ liệu thật từ SQL Server dump khi bàn giao | Mở (dùng trong test) |
| 6 | Phụ thuộc chưa port | 17 | Thiếu bảng liên quan (Mst_NNT, Prm_*, Staff_*) và FK | Hoãn kiểm tra FK; kích hoạt lại sau khi port xong các bảng cha | Mở (chờ thứ tự port) |
| 7 | Bất thường nghiệp vụ nguồn | 8 | Lỗi logic tiềm ẩn ở nguồn (rows[0], xóa bảng, hardcode) | Phòng vệ mã port, bọc transaction, báo cáo để xác nhận | Đã phòng vệ trong mã port |
| 8 | Dịch vụ ngoài (External Services) | 5 | Phụ thuộc iNOS master server hoặc Hyundai DMS | Cấu hình cờ feature toggle hoặc xây dựng mock adapter | Mở (CAN_XEM_TAY) |
| 9 | Bảo mật & Xử lý ngoại lệ | 1 | Thông báo lỗi trả nguyên `ex.Message` lộ chi tiết kỹ thuật | Bọc filter chung ẩn chi tiết lỗi khi triển khai môi trường công khai | Rủi ro đã chấp nhận |
| 10 | Định dạng API & Serialization | 1 | JSON PascalCase chỉ áp dụng cho namespace `Controllers.Port` | Duy trì filter tách biệt để bảo toàn tính toàn vẹn cho API cũ và SPA | Đã xử lý bằng hạ tầng |

> *Ghi chú thống kê*: Một hạng mục trong file log có thể đồng thời ghi nhận nhiều loại GAP (ví dụ: vừa thiếu phân quyền, vừa thiếu ghi log, vừa thiếu DDL). Tổng số lượt ghi nhận là 314 lượt trên 122 hạng mục có báo cáo GAP và 2 hạng mục hạ tầng toàn cục.

---

## 2. Phân tích chi tiết từng nhóm GAP & Hướng xử lý

### 2.1. Phân quyền và ngữ cảnh người dùng (Phân quyền)
- **Mô tả**: Hầu hết các action nguồn đều gọi `CheckGatewayAuthentication()`, `CheckAuthorize(ref orgUserUI)`, `Sys_User_CheckAuthorize()`, hoặc truy vấn quyền đại lý `myCache_Mst_PromotionProgram_ViewAbility_Get`. Khi port sang MiniLoyalty, các kiểm tra này được đánh dấu `Đ` (đơn giản hóa có chủ đích) và tạm thời cho qua.
- **Tác động**: Mọi client gọi tới endpoint đều được thực thi mà không bị chặn phân quyền; các trường dữ liệu audit (`LogLUBy`, `OrgID`, `NetworkID`) tạm thời nhận giá trị cố định hoặc lấy từ cấu hình: `LogLUBy = "port"`, `OrgID` theo `Biz_OrgID`, `NetworkID` theo `Biz_NetworkID` (mặc định `"1001"`) — xem `Controllers/Port/Mst_DepartmentControllerPort.cs`.
- **Hướng xử lý**: Tạo `TenantContext` và middleware xác thực tập trung. Khi có token hợp lệ, trích xuất `OrgID`, `NetworkID`, `UserCode` đưa vào `HttpContext.Items` để các controller đọc tự động.

### 2.2. Ghi nhật ký hệ thống (Ghi log)
- **Mô tả**: Nguồn gọi `ProcessBizReq_OutSide`, `ProcessBizReturn_OutSide`, ghi vào bảng `Sys_RequestLog` / `Sys_ReturnLog`. Trong mã port, các khối lệnh này được lược bỏ để giảm thiểu phụ thuộc hạ tầng.
- **Tác động**: Không ghi lại vết request/response nhị phân tại database. Tuy nhiên, luồng nghiệp vụ cốt lõi không bị ảnh hưởng.
- **Hướng xử lý**: Tích hợp Serilog ghi log dạng có cấu trúc (structured logging) ra console hoặc file, sẵn sàng đẩy lên OpenTelemetry/Seq mà không cần bảng DB riêng.

### 2.3. Định dạng và xử lý tệp Excel (Định dạng file Excel)
- **Mô tả**: Nguồn sử dụng các phương thức đóng gói sẵn trong DLL nhị phân (`idn.Loyalty.Common\Utils\ExcelExport.cs`, `ExcelImportNew`). Do không tái sử dụng trực tiếp DLL .NET Framework cũ, mã port sử dụng lớp tiện ích `Data/PortExcel.cs` dựa trên thư viện ClosedXML (đọc/ghi `.xlsx`).
- **Tác động**: File xuất ra hoặc file template có thể có khác biệt nhỏ về style hiển thị hoặc tiêu đề cột so với phiên bản cũ. Tại #130 (`Mst_Department/Export`), giá trị cột "Phòng ban cấp trên" (mã hay tên) của DLL nguồn cần đối chiếu mẫu thật.
- **Hướng xử lý**: Thu thập file Excel mẫu xuất ra từ môi trường cũ; viết bộ unit test kiểm tra đối chiếu từng sheet, tên cột và kiểu dữ liệu trong file import.

### 2.4. Dữ liệu giả lập (Dữ liệu demo)
- **Mô tả**: Các bảng danh mục (`Mst_Province`, `Mst_District`, `Mst_Ward`, `Mst_CarBrand`, `Mst_CardType`, `Mst_BirthPolicy`, `Mst_GovIDType`, `Mst_Grade`, `Mst_Gender`, `Mst_LeaveDay`...) sử dụng `IPortSeeder` để nạp dữ liệu mẫu ban đầu nhằm đảm bảo kiểm thử round-trip chạy được.
- **Tác động**: Dữ liệu chỉ mang tính minh họa, chưa có toàn bộ danh mục thực tế của khách hàng.
- **Hướng xử lý**: Tách bạch seeder môi trường Development và seeder môi trường Production; cung cấp script ETL đồng bộ dữ liệu thật từ SQL Server sang SQLite/PostgreSQL.

### 2.5. Phụ thuộc bảng chưa port và Khóa ngoại (Phụ thuộc chưa port)
- **Mô tả**: Một số endpoint xóa (`Delete`, `DeleteMultiple`) ở nguồn có kiểm tra khóa ngoại (FK) sang các bảng khác trước khi cho phép xóa (ví dụ: `Mst_Department` kiểm tra `Staff_MapDepartment`, `Train_CourseDepartment`; `Mst_CardType` kiểm tra các bảng giao dịch thẻ; `Mst_DealerCP` kiểm tra `Map_QueryDealer_Member`, `Prm_CarNew`). Vì các bảng này chưa port nên mã port chưa thể kích hoạt các câu lệnh kiểm tra này.
- **Tác động**: Nếu xóa danh mục cha, các bản ghi con ở hệ thống cũ có thể bị mất tính toàn vẹn tham chiếu.
- **Hướng xử lý**: Lưu vết danh sách FK; sau khi hoàn thành các nhóm bảng giao dịch và người dùng, quay lại bổ sung các guard kiểm tra FK trước khi xóa.

### 2.6. Thiếu kịch bản DDL chính thức (Thiếu DDL / Schema)
- **Mô tả**: Nhiều bảng danh mục không tìm thấy file `.sql` tạo bảng trong thư mục `Migrate\`. Tên cột, kiểu dữ liệu và khóa chính được suy đoán từ thuộc tính C# và mã nguồn gọi SQL (như `Mst_FamilyStatus`, `Mst_GovIDType`, `Mst_Grade`, `Mst_Gender`, `Mst_LeaveDay`).
- **Tác động**: Nguy cơ sai lệch kiểu dữ liệu hoặc thiếu các chỉ mục quan trọng. Đặc biệt, SQLite với `CREATE TABLE IF NOT EXISTS` không tự động thay đổi khóa chính nếu DB đã tồn tại (như trường hợp #143 `Mst_District`).
- **Hướng xử lý**: Trích xuất schema trực tiếp từ database SQL Server đang chạy thật (`sys.columns`, `sys.tables`) để đối chiếu và chuẩn hóa file migration EF Core cuối cùng.

### 2.7. Bất thường nghiệp vụ nguồn (Bất thường nghiệp vụ)
- **Mô tả & Hướng xử lý**:
  1. #110 `Mst_DealerCPController/Search`: Nguồn dùng `DISTINCT` trên câu truy vấn có cột `IDENTITY`. Hướng xử lý: chuẩn hóa SELECT bỏ identity khi cần distinct.
  2. #112 `Mst_DealerCPController/GetByDLCPCode`: Nguồn hardcode tài khoản hệ thống. Hướng xử lý: loại bỏ tài khoản hardcode, thay bằng cơ chế cấu hình service account an toàn.
  3. #187 `Mst_ParamSysController/GetAllActive`: Khi trùng `LogLUDTimeUTC`, thứ tự bản ghi không xác định. Hướng xử lý: thêm sắp xếp thứ cấp theo khóa chính `ParamCode`.
  4. #196 `Mst_PolicyMoneyToPointServiceController/Save`: Logic nguồn chép nhầm kiểm tra `BirthPolicy`. Hướng xử lý: giữ tương thích nhưng đánh dấu cảnh báo nghiệp vụ.
  5. #209 `Mst_PromotionProgramController/Update`: Xử lý lệch múi giờ giữa máy chủ và UTC. Hướng xử lý: chuẩn hóa lưu trữ `DateTimeOffset` / UTC thống nhất.
  6. #229 `Mst_RankPolicyController/GetByRankPolicyCode`: Nguồn truy cập trực tiếp `rows[0]`. Hướng xử lý: kiểm tra danh sách rỗng trước khi truy cập phần tử đầu tiên để chống crash.
  7. #230 `Mst_RankPolicyController/Save`: Nguồn thực hiện xóa toàn bộ bảng rồi chèn lại. Hướng xử lý: bọc trong transaction chặt chẽ và ghi chú cảnh báo tương tranh.

### 2.8. Dịch vụ ngoài (Dịch vụ ngoài / CAN_XEM_TAY)
- **Mô tả**: Các hạng mục phụ thuộc dịch vụ ngoài được ghi nhận tại `.parity-run\skipped.json`:
  1. #181 `Mst_OrgController/GetAllOrg`: Phụ thuộc API iNOS Master Server (`WA_OS_Inos_Org_GetMyOrgList`).
  2. #182 `Mst_OrgController/GetOrgsNotInSolution`: Phụ thuộc API iNOS Master Server.
  3. #068 `Map_QueryDealer_MemberController/Create`: Phụ thuộc Web Service Hyundai DMS (`WSDMS CarSv_Ser_CustomerCar_Get`, `Ser_CustomerCar_SalesCreate`).
- **Hướng xử lý**: Cần người dùng phê duyệt phương án: (a) Xây dựng bảng mirror đồng bộ định kỳ; (b) Xây dựng mock service; hoặc (c) Loại bỏ endpoint nếu không còn dùng hệ thống ngoài.

### 2.9. Bảo mật & Xử lý ngoại lệ: Thông báo lỗi trả nguyên `ex.Message`
- **Mô tả**: Cả ở tầng lọc ngoại lệ toàn cục (`PortExceptionFilter`) và các khối `catch (Exception ex)` cục bộ trong controller port đều trả nguyên văn chuỗi `ex.Message` ra đối tượng phản hồi HTTP 400 (`PortResult.Fail("<Controller>_<Action>", "Exception", ex.Message)`). Điều này hoàn toàn khớp với hành vi của hệ thống nguồn (`CmUtils.CMyException` và `AddException` tại Gate).
- **Tác động**: Tiềm ẩn rủi ro lộ cấu trúc hạ tầng bên trong (Information Disclosure) như tên bảng, tên cột, đường dẫn tệp tin trên máy chủ, hoặc cú pháp câu lệnh DBMS (SQLite/PostgreSQL) khi gặp lỗi kết nối hay lỗi truy vấn.
- **Đánh giá & Quyết định**: Claude và nhóm dự án đã phê duyệt chấp nhận rủi ro này trong giai đoạn chuyển đổi để đảm bảo tính khớp 1-1 với `2022.7.Loyalty` và phục vụ kiểm thử đối chiếu chính xác.
- **Hướng xử lý đề xuất**: Nếu giải pháp được triển khai ra môi trường công khai (Public Internet), bắt buộc cấu hình một middleware/filter bọc bên ngoài để thay thế chuỗi chi tiết `ex.Message` bằng thông báo lỗi chung (ví dụ: *"Đã xảy ra lỗi trong quá trình xử lý"*), đồng thời ghi vết chi tiết vào hệ thống log nội bộ.

### 2.10. Định dạng API & Serialization: JSON PascalCase chỉ áp cho namespace `Controllers.Port`
- **Mô tả**: Hệ thống nguồn `2022.7.Loyalty` sử dụng thư viện `JavaScriptSerializer` và Newtonsoft.Json, trả về các trường JSON theo định dạng **PascalCase** (`Success`, `Data`, `DataList`, `ItemCount`, `ErrorCode`, `Params`). Trong khi đó, .NET 8 Web API mặc định định dạng JSON theo chuẩn **camelCase**.
- **Giải pháp hạ tầng**: Để bảo toàn hợp đồng API 1-1 với các client nguồn mà không gây ảnh hưởng tới các endpoint cũ đang chạy của MiniLoyalty (`/api/v1/*`, `/api/earn`, v.v.) cũng như giao diện người dùng SPA frontend, lớp `PortJsonResultFilter` được cấu hình để chỉ áp dụng `PropertyNamingPolicy = null` cho các Controller thuộc đúng namespace `MiniLoyalty.Controllers.Port` (commit `02911b1`).
- **Tác động**: Các controller cũ và controller tiện ích ngoài namespace `Port` vẫn trả về camelCase chuẩn của .NET 8. Đảm bảo tính độc lập và an toàn tuyệt đối.
- **Trạng thái**: Đã xử lý hoàn tất tại hạ tầng dùng chung (`Data/PortFilters.cs`).

---

## 3. Sổ đăng ký chi tiết từng hạng mục (122 hạng mục + 2 mục hạ tầng)

| Hạng mục # | Tên hạng mục | Nhóm GAP phân loại | Nội dung chi tiết được ghi nhận | Trạng thái |
|---|---|---|---|---|
| HT-01 | Hạ tầng toàn cục (PortExceptionFilter) | Bảo mật & Xử lý ngoại lệ | Thông báo lỗi trả nguyên `ex.Message` — rủi ro đã chấp nhận vì khớp nguồn (`AddException`), cần bọc nếu triển khai công khai | Rủi ro đã chấp nhận (Cần bọc khi deploy production) |
| HT-02 | Hạ tầng toàn cục (PortJsonResultFilter) | Định dạng API & Serialization | JSON PascalCase chỉ áp cho namespace `Controllers.Port`; các endpoint cũ (/api/v1, /api/earn) và SPA giữ camelCase | Đã xử lý bằng hạ tầng (02911b1) |
| 65 | Map_DealerCP_OrgController_GetByDLCPCode | Phân quyền, Ghi log, Bất thường nghiệp vụ | thứ tự khi trùng LogLUDTimeUTC; ability người dùng; phân quyền; log | Đã phòng vệ trong code |
| 66 | Map_QueryDealer_MemberController_Search | Phân quyền, Ghi log, Dữ liệu demo, Thiếu DDL / Schema | ability theo đại lý user (chờ có Sys_User); khóa suy luận (DLCPCode, MemberNo); dữ liệu demo; phân quyền; log | Mở |
| 67 | Map_QueryDealer_MemberController_SearchForRePort | Phân quyền, Ghi log | ability theo đại lý user; định dạng ngày QueryDate; phân quyền; log | Mở |
| 69 | Map_QueryDealer_MemberController_ExportExcel | Phân quyền, Ghi log, Định dạng file Excel | định dạng ô Excel (DLL nguồn) cần xác nhận; ability; phân quyền; log | Mở |
| 70 | Mst_BirthPolicyController_Search | Phân quyền, Ghi log, Dữ liệu demo, Phụ thuộc chưa port | FK chưa giả lập; dữ liệu demo; phân quyền; log | Mở |
| 71 | Mst_BirthPolicyController_GetAllActive | Phân quyền, Ghi log | phân quyền; log | Mở |
| 72 | Mst_BirthPolicyController_GetByBirthPolicyNo | Phân quyền, Ghi log | phân quyền; log | Mở |
| 73 | Mst_BirthPolicyController_Save | Phân quyền, Ghi log, Dữ liệu demo | FlagPoint/FlagVoucher không có script; Mst_ParamPromotionType dữ liệu demo; NetworkID/LogLUBy từ user; phân quyền; log | Mở |
| 74 | Mst_BirthPolicyController_Update | Phân quyền, Ghi log | UpdateBy từ user; phân quyền; log | Mở |
| 75 | Mst_BirthPolicyController_Delete | Phân quyền, Ghi log | phân quyền; log | Mở |
| 76 | Mst_BirthPolicyController_Export | Định dạng file Excel | định dạng ô Excel (DLL nguồn) cần xác nhận | Mở |
| 77 | Mst_CarBrandController_Search | Phân quyền, Ghi log, Dữ liệu demo, Thiếu DDL / Schema | không có DDL; dữ liệu demo; phân quyền; log | Mở |
| 78 | Mst_CarBrandController_GetAllActive | Phân quyền, Ghi log | phân quyền; log | Mở |
| 79 | Mst_CarBrandController_GetByBrandCode | Phân quyền, Ghi log | phân quyền; log | Mở |
| 80 | Mst_CarBrandController_Create | Phân quyền, Ghi log | phân quyền; log; LogLUBy chưa có user | Mở |
| 81 | Mst_CarBrandController_Update | Phân quyền, Ghi log | phân quyền; log; LogLUBy chưa có user | Mở |
| 82 | Mst_CarBrandController_Delete | Phân quyền, Ghi log | phân quyền; log | Mở |
| 83 | Mst_CarBrandController_DeleteMultiple | Phân quyền, Ghi log | phân quyền; log | Mở |
| 84 | Mst_CarBrandController_Import | Phân quyền, Ghi log | NetworkID từ user; định dạng file là giả định; phân quyền; log | Mở |
| 85 | Mst_CarBrandController_Export | Định dạng file Excel | định dạng ô Excel (DLL nguồn) cần xác nhận | Mở |
| 86 | Mst_CarBrandController_ExportTemplate | Định dạng file Excel | định dạng ô Excel (DLL nguồn) cần xác nhận | Mở |
| 87 | Mst_CardTypeController_Search | Phân quyền, Ghi log | phân quyền và log bỏ qua có chủ đích (loại Đ) | Mở |
| 88 | Mst_CardTypeController_GetAllActive | Phân quyền, Ghi log | phân quyền; log | Mở |
| 89 | Mst_CardTypeController_GetByCardType | Phân quyền, Ghi log | phân quyền; log | Mở |
| 90 | Mst_CardTypeController_Create | Phân quyền, Ghi log | phân quyền; log; LogLUBy chưa có user | Mở |
| 91 | Mst_CardTypeController_Update | Phân quyền, Ghi log | Lượt chạy chạm trần 50 bước an toàn; mã đã Claude review PASS và commit (43e777c); phân quyền; log | Đã commit, Mở auth/log |
| 92 | Mst_CardTypeController_Delete | Phân quyền, Ghi log, Phụ thuộc chưa port | FK 4 bảng chưa port; phân quyền; log | Mở |
| 93 | Mst_CardTypeController_DeleteMultiple | Phân quyền, Ghi log, Phụ thuộc chưa port | FK 4 bảng chưa port; phân quyền; log | Mở |
| 94 | Mst_CardTypeController_Import | Phân quyền, Ghi log | NetworkID từ user; định dạng file là giả định; phân quyền; log | Mở |
| 95 | Mst_CardTypeController_Export | Định dạng file Excel | định dạng ô Excel (DLL nguồn) cần xác nhận | Mở |
| 96 | Mst_CardTypeController_ExportTemplate | Định dạng file Excel | định dạng ô Excel (DLL nguồn) cần xác nhận | Mở |
| 97 | Mst_CarModelController_Search | Phân quyền, Ghi log, Dữ liệu demo, Thiếu DDL / Schema | không có DDL; dữ liệu demo; phân quyền; log | Mở |
| 98 | Mst_CarModelController_GetAllActive | Phân quyền, Ghi log | phân quyền; log | Mở |
| 99 | Mst_CarModelController_GetByModelCode | Phân quyền, Ghi log | phân quyền; log | Mở |
| 100 | Mst_CarModelController_Create | Phân quyền, Ghi log | phân quyền; log; LogLUBy chưa có user | Mở |
| 101 | Mst_CarModelController_Update | Phân quyền, Ghi log | phân quyền; log; LogLUBy chưa có user | Mở |
| 102 | Mst_CarModelController_Delete | Phân quyền, Ghi log, Phụ thuộc chưa port | FK Prm_CarRecommendSpec/Prm_CarNewSpec chưa port; phân quyền; log | Mở |
| 103 | Mst_CarModelController_DeleteMultiple | Phân quyền, Ghi log, Phụ thuộc chưa port | FK Prm_* chưa port; phân quyền; log | Mở |
| 104 | Mst_CarModelController_Import | Phân quyền, Ghi log | NetworkID từ user; định dạng file là giả định; phân quyền; log | Mở |
| 105 | Mst_CarModelController_Export | Định dạng file Excel | định dạng ô Excel (DLL nguồn) cần xác nhận | Mở |
| 106 | Mst_CarModelController_ExportTemplate | Định dạng file Excel | định dạng ô Excel (DLL nguồn) cần xác nhận | Mở |
| 107 | Mst_CertificateGroupController_Search | Phân quyền, Ghi log, Dữ liệu demo, Thiếu DDL / Schema | không có DDL; dữ liệu demo; phân quyền; log | Mở |
| 108 | Mst_CertificateGroupController_GetAllActive | Phân quyền, Ghi log | phân quyền; log | Mở |
| 109 | Mst_CountryController_GetAllActive | Phân quyền, Ghi log, Dữ liệu demo, Thiếu DDL / Schema | không có DDL; dữ liệu demo; phân quyền; log | Mở |
| 110 | Mst_DealerCPController_Search | Phân quyền, Ghi log, Thiếu DDL / Schema, Bất thường nghiệp vụ | khóa Map_DealerCP_Org suy luận; DISTINCT+IDENTITY cần xác nhận; phân quyền; log | Đã phòng vệ trong code |
| 111 | Mst_DealerCPController_GetAllActive | Phân quyền, Ghi log | phân quyền; log | Mở |
| 112 | Mst_DealerCPController_GetByDLCPCode | Phân quyền, Ghi log, Thiếu DDL / Schema, Bất thường nghiệp vụ | tài khoản hệ thống viết cứng không port; phân quyền; log | Đã phòng vệ trong code |
| 113 | Mst_DealerCPController_GetByOrgID | Phân quyền, Ghi log | phân quyền; log | Mở |
| 114 | Mst_DealerCPController_CreateOld | Phân quyền, Ghi log | phân quyền; log; LogLUBy chưa có user; Biz_NetworkID cấu hình | Mở |
| 116 | Mst_DealerCPController_Update | Phân quyền, Ghi log | phân quyền; log; LogLUBy chưa có user | Mở |
| 117 | Mst_DealerCPController_Delete | Phân quyền, Ghi log, Phụ thuộc chưa port | FK Map_QueryDealer_Member, Prm_CarNew, Prm_CarRecommend, Mst_PromotionProgramSpec chưa kiểm; phân quyền; log | Mở |
| 118 | Mst_DealerCPController_DeleteMultiple | Phân quyền, Ghi log, Phụ thuộc chưa port | FK 4 bảng chưa port; phân quyền; log | Mở |
| 120 | Mst_DealerCPController_Export | Phân quyền, Ghi log, Định dạng file Excel | định dạng ô Excel của ExcelExport (DLL nguồn) cần xác nhận; phân quyền; log | Mở |
| 121 | Mst_DealerCPController_ExportTemplate | Định dạng file Excel | định dạng ô Excel của ExcelExport (DLL nguồn) cần xác nhận | Mở |
| 122 | Mst_DepartmentController_Search | Phân quyền, Ghi log, Dữ liệu demo, Thiếu DDL / Schema, Phụ thuộc chưa port | Staff_MapDepartment chưa port (ss_QtyStaff=0); không có DDL; dữ liệu demo; phân quyền; log | Mở |
| 123 | Mst_DepartmentController_GetAllActive | Phân quyền, Ghi log | OrgID của user đăng nhập; phân quyền; log | Mở |
| 124 | Mst_DepartmentController_GetByDepartmentCode | Phân quyền, Ghi log, Phụ thuộc chưa port | Staff_MapDepartment; phân quyền; log | Mở |
| 125 | Mst_DepartmentController_Create | Phân quyền, Ghi log | OrgID/MST/LogLUBy từ user; gốc 'HO' viết cứng; phân quyền; log | Mở |
| 126 | Mst_DepartmentController_Update | Phân quyền, Ghi log | LogLUBy từ user; phân quyền; log | Mở |
| 127 | Mst_DepartmentController_Delete | Phân quyền, Ghi log, Phụ thuộc chưa port | Staff_MapDepartment; Train_CourseDepartment FK; phân quyền; log | Mở |
| 128 | Mst_DepartmentController_DeleteMultiple | Phân quyền, Ghi log, Phụ thuộc chưa port | Staff_MapDepartment; phân quyền; log | Mở |
| 129 | Mst_DepartmentController_Import | Phân quyền, Ghi log, Định dạng file Excel | định dạng file ExcelImportNew (DLL) là giả định; OrgID/MST từ user; phân quyền; log | Mở |
| 130 | Mst_DepartmentController_Export | Phân quyền, Ghi log, Định dạng file Excel | định dạng ô Excel / giá trị cột "Phòng ban cấp trên" (mã hay tên) của DLL nguồn cần xác nhận; phân quyền bỏ qua | Mở |
| 131 | Mst_DepartmentController_ExportTemplate | Định dạng file Excel | định dạng ô Excel (DLL nguồn ExcelExport) và nội dung gate :864-873 chưa xác minh trực tiếp từ nguồn | Mở |
| 132 | Mst_DisciplineController_Search | Phân quyền, Ghi log, Dữ liệu demo, Thiếu DDL / Schema | không có DDL; dữ liệu demo; phân quyền; log | Mở |
| 133 | Mst_DisciplineController_GetByDisciplineCode | Phân quyền, Ghi log | phân quyền; log | Mở |
| 134 | Mst_DisciplineController_GetAllActive | Phân quyền, Ghi log | phân quyền; log | Mở |
| 135 | Mst_DisciplineController_Create | Phân quyền, Ghi log | LogLUBy từ user; phân quyền; log | Mở |
| 136 | Mst_DisciplineController_Update | Phân quyền, Ghi log | LogLUBy từ user; phân quyền; log | Mở |
| 137 | Mst_DisciplineController_Delete | Phân quyền, Ghi log | phân quyền; log | Mở |
| 138 | Mst_DisciplineController_DeleteMultiple | Phân quyền, Ghi log | phân quyền; log | Mở |
| 139 | Mst_DistrictController_Search | Phân quyền, Ghi log, Dữ liệu demo, Thiếu DDL / Schema | không có DDL; dữ liệu demo; phân quyền; log | Mở |
| 140 | Mst_DistrictController_GetDetail | Phân quyền, Ghi log | phân quyền; log | Mở |
| 141 | Mst_DistrictController_GetByProvinceCode | Phân quyền, Ghi log | phân quyền; log | Mở |
| 142 | Mst_DistrictController_GetAllActive | Phân quyền, Ghi log | phân quyền; log | Mở |
| 143 | Mst_DistrictController_Create | Phân quyền, Ghi log, Thiếu DDL / Schema | DB đã tạo bảng với khóa cũ (DistrictCode) sẽ KHÔNG tự đổi khóa (CREATE TABLE IF NOT EXISTS); phân quyền; log | Mở |
| 144 | Mst_DistrictController_Update | Phân quyền, Ghi log | phân quyền; log | Mở |
| 145 | Mst_DistrictController_Delete | Phân quyền, Ghi log, Phụ thuộc chưa port | FK Mst_Agent chưa port; phân quyền; log | Mở |
| 146 | Mst_DistrictController_Import | Ghi log, Định dạng file Excel | định dạng file ExcelImportNew (DLL) là giả định; phân quyền; log | Mở |
| 147 | Mst_DistrictController_Export | Định dạng file Excel | định dạng ô Excel (DLL nguồn) cần xác nhận | Mở |
| 148 | Mst_DistrictController_ExportTemplate | Định dạng file Excel | định dạng ô Excel (DLL nguồn) cần xác nhận | Mở |
| 149 | Mst_EthnicCategoryController_Search | Phân quyền, Ghi log, Dữ liệu demo, Thiếu DDL / Schema | không có DDL (PK = EthnicCode, kiểu cột cần xác nhận); dữ liệu demo; phân quyền; log | Mở |
| 150 | Mst_EthnicCategoryController_GetAllActive | Phân quyền, Ghi log | phân quyền; log | Mở |
| 151 | Mst_ExpenseTypeController_GetAllActive | Phân quyền, Ghi log | CheckAuthorize / user đăng nhập; log RequestLog/ReturnLog; Ext tree | Mở |
| 152 | Mst_FamilyStatusController_Search | Phân quyền, Ghi log, Dữ liệu demo, Thiếu DDL / Schema | không có DDL; dữ liệu demo; phân quyền; log | Mở |
| 153 | Mst_FamilyStatusController_GetAllActive | Thiếu DDL / Schema, Phân quyền, Ghi log | không có DDL/kiểu cột; Ft_RecordStart/Count nguồn chưa xác minh; mã lỗi chuỗi chưa xác minh; auth/log bỏ (Đ); Order 32 trùng Mst_Department (đã xử lý ở A.4) | Mở |
| 154 | Mst_GenderController_Search | Phân quyền, Ghi log, Dữ liệu demo, Thiếu DDL / Schema | không có DDL; dữ liệu demo; phân quyền; log | Mở |
| 156 | Mst_GovIDTypeController_Search | Phân quyền, Ghi log, Dữ liệu demo, Thiếu DDL / Schema | auth (CheckAuthorize); NetworkID không lọc; PK/kiểu cột chưa xác minh (DDL không có); dữ liệu demo; log | Mở |
| 157 | Mst_GovIDTypeController_GetByGovIDType | Thiếu DDL / Schema, Phân quyền, Ghi log | không DDL; verb gate chưa xác minh; Trim/IsNullOrEmpty nguồn chưa xác minh; phân quyền/log bỏ (Đ) | Mở |
| 158 | Mst_GovIDTypeController_GetAllActive | Thiếu DDL / Schema, Dữ liệu demo, Phân quyền, Ghi log | Ft_RecordStart/Count chưa xác minh; không DDL; dữ liệu demo; phân quyền/log bỏ (Đ) | Mở |
| 162 | Mst_GradeController_Search | Thiếu DDL / Schema, Dữ liệu demo, Phân quyền, Ghi log | DDL Mst_Grade chưa xác minh (kiểu/PK suy ra); dữ liệu demo; phân quyền/auth bỏ; log; hình dáng phản hồi lỗi | Mở |
| 163 | Mst_GradeController_GetAllActive | Thiếu DDL / Schema, Phân quyền, Ghi log | Ft_RecordStart/Count chưa xác minh (dùng tạm 0/123456000); verb chưa xác minh (POST); không có DDL; phân quyền; log | Mở |
| 164 | Mst_LeaveDayController_Search | Phân quyền, Ghi log, Dữ liệu demo, Thiếu DDL / Schema | DDL Mst_LeaveDay chưa có trong repo (.sql); dữ liệu demo; phân quyền; log | Mở |
| 178 | Mst_OrgController_Search | Phân quyền, Ghi log, Phụ thuộc chưa port | Mst_NNT còn 60 cột chưa làm; hệ phân quyền; ghi log | Mở |
| 179 | Mst_OrgController_GetAllActive | Phân quyền, Ghi log | phân quyền; log | Mở |
| 187 | Mst_ParamSysController_GetAllActive | Phân quyền, Ghi log, Bất thường nghiệp vụ | thứ tự khi trùng LogLUDTimeUTC; phân quyền; log | Đã phòng vệ trong code |
| 194 | Mst_PolicyMoneyToPointServiceController_GetAllActive | Phân quyền, Ghi log, Dữ liệu demo, Phụ thuộc chưa port | FK chưa giả lập; dữ liệu demo; phân quyền; log | Mở |
| 195 | Mst_PolicyMoneyToPointServiceController_GetByPolicyCode | Phân quyền, Ghi log | phân quyền; log | Mở |
| 196 | Mst_PolicyMoneyToPointServiceController_Save | Phân quyền, Ghi log, Bất thường nghiệp vụ | NetworkID/LogLUBy từ user; BirthPolicy check chép nhầm ở nguồn; phân quyền; log | Đã phòng vệ trong code |
| 205 | Mst_PromotionProgramController_Search | Phân quyền, Ghi log, Dữ liệu demo, Phụ thuộc chưa port | ability theo đại lý user; FK chưa giả lập; dữ liệu demo; phân quyền; log | Mở |
| 206 | Mst_PromotionProgramController_GetAllActive | Phân quyền, Ghi log | ability; phân quyền; log | Mở |
| 207 | Mst_PromotionProgramController_GetByPrProgramCode | Phân quyền, Ghi log | ability; phân quyền; log | Mở |
| 208 | Mst_PromotionProgramController_Save | Phân quyền, Ghi log, Phụ thuộc chưa port | OrgID/NetworkID/LogLUBy từ user; FK CardType Dtl; phân quyền; log | Mở |
| 209 | Mst_PromotionProgramController_Update | Phân quyền, Ghi log, Bất thường nghiệp vụ | giờ máy chủ vs UTC; OrgID/NetworkID/LogLUBy từ user; phân quyền; log | Đã phòng vệ trong code |
| 210 | Mst_PromotionProgramController_Delete | Phân quyền, Ghi log | phân quyền; log | Mở |
| 211 | Mst_PromotionProgramController_Export | Định dạng file Excel | định dạng ô Excel (DLL nguồn) cần xác nhận | Mở |
| 212 | Mst_PromotionProgramController_ExportTemplate | Định dạng file Excel | định dạng ô Excel (DLL nguồn) cần xác nhận | Mở |
| 213 | Mst_ProvinceController_Search | Phân quyền, Ghi log, Dữ liệu demo, Thiếu DDL / Schema | không có DDL; dữ liệu tỉnh là demo; phân quyền; log | Mở |
| 214 | Mst_ProvinceController_GetByProvinceCode | Phân quyền, Ghi log | phân quyền; log | Mở |
| 215 | Mst_ProvinceController_GetAllActive | Phân quyền, Ghi log | phân quyền; log | Mở |
| 216 | Mst_ProvinceController_Create | Phân quyền, Ghi log | phân quyền; log; LogLUBy chưa có user | Mở |
| 217 | Mst_ProvinceController_Update | Phân quyền, Ghi log | phân quyền; log | Mở |
| 218 | Mst_ProvinceController_Delete | Phân quyền, Ghi log | phân quyền; log | Mở |
| 219 | Mst_ProvinceController_Import | Phân quyền, Ghi log, Định dạng file Excel | định dạng file ExcelImportNew (DLL) là giả định; phân quyền; log | Mở |
| 220 | Mst_ProvinceController_Export | Định dạng file Excel | định dạng ô Excel (DLL nguồn) cần xác nhận | Mở |
| 221 | Mst_ProvinceController_ExportTemplate | Định dạng file Excel | định dạng ô Excel (DLL nguồn) cần xác nhận | Mở |
| 222 | Mst_PurchaseTypeController_GetAllActive | Phân quyền, Ghi log, Dữ liệu demo | tên loại mua là demo; phân quyền; log | Mở |
| 227 | Mst_RankPolicyController_Search | Phân quyền, Ghi log, Dữ liệu demo, Phụ thuộc chưa port | FK CardType (Mst_CardType delete cần chặn); dữ liệu demo; phân quyền; log | Mở |
| 228 | Mst_RankPolicyController_GetAllActive | Phân quyền, Ghi log | phân quyền; log | Mở |
| 229 | Mst_RankPolicyController_GetByRankPolicyCode | Phân quyền, Ghi log, Bất thường nghiệp vụ | hành vi [0] của nguồn có thể là lỗi nguồn — cần nghiệp vụ xác nhận; phân quyền; log | Đã phòng vệ trong code |
| 230 | Mst_RankPolicyController_Save | Phân quyền, Ghi log, Bất thường nghiệp vụ | NetworkID từ user; LogLUBy; xóa toàn bảng là hành vi nguồn; phân quyền; log | Đã phòng vệ trong code |
| 246 | Mst_WardController_GetAllActive | Phân quyền, Ghi log, Dữ liệu demo, Thiếu DDL / Schema | khóa Mst_Ward suy luận; không có DDL; dữ liệu demo; phân quyền; log | Mở |
| 247 | Mst_WardController_GetWardDetail | Phân quyền, Ghi log | phân quyền; log | Mở |

---

## 4. Lịch sử cập nhật

| Phiên bản | Ngày cập nhật | Người thực hiện | Nội dung thay đổi |
|---|---|---|---|
| 1.0 | 2026-10-16 11:15 | doc2 (Quản lý quy trình) | Khởi tạo sổ đăng ký GAP tổng hợp toàn bộ 112 file nhật ký thực tế và skipped.json; phân loại 8 nhóm nghiệp vụ kèm hướng xử lý. |
| 2.0 | 2026-10-16 12:00 | doc2 (Quản lý quy trình) | Cập nhật đợt 2: bổ sung 8 hạng mục mới hoàn thành (#130, #131, #153, #156, #157, #158, #162, #163); bổ sung 2 GAP hạ tầng toàn cục (thông báo lỗi trả nguyên ex.Message và JSON PascalCase theo namespace). |
| 2.1 | 2026-10-16 12:12 | doc2 (Quản lý quy trình) | Cập nhật realtime bổ sung #154 (Mst_GenderController/Search) và #164 (Mst_LeaveDayController/Search); nâng số lượng nhật ký rà soát lên 122 file; tính toán lại toàn bộ số liệu theo PARITY_PROGRESS.md (123 Đã làm, 1 Bỏ, 213 Chưa làm). |
