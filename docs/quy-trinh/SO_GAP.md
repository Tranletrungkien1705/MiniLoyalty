# SỔ ĐĂNG KÝ GAP (GAP REGISTER) — MiniLoyalty

> **Phạm vi**: Toàn bộ các điểm sai khác, thiếu hụt (GAP) được ghi nhận trong quá trình port 1-1 các hạng mục từ `2022.7.Loyalty` sang `MiniLoyalty`.  
> **Cập nhật**: 2026-10-16 13:44  
> **Nguồn dữ liệu**: Quét tự động toàn bộ 145 thư mục nhật ký tại `.parity-run\logs\*\write_r*.txt` (135 file có mục `GAP:`), kết hợp `.parity-run\skipped.json`, mã nguồn hạ tầng `Data/PortFilters.cs`, `Data/PortResult.cs`, và `PARITY_PROGRESS.md` (136 mục Đã làm, 1 Bỏ, 200 Chưa làm).  
> **Trạng thái**: Nháp — chờ review  

---

## 1. Bảng thống kê GAP theo loại

| STT | Loại GAP | Số lượng phát hiện | Tác động chính | Hướng xử lý đề xuất | Trạng thái tổng quát |
|---|---|---|---|---|---|
| 1 | Phân quyền (Auth / Ability) | 116 | Chưa kiểm tra quyền chi tiết và ngữ cảnh user đăng nhập | Bổ sung middleware xác thực JWT / Claims khi có hệ thống SSO | Mở (chờ SSO) |
| 2 | Ghi nhật ký (Audit Log) | 117 | Bỏ qua ghi log gọi API (Request/Return Log) | Bổ sung middleware audit log hoặc Serilog tập trung | Mở (chờ audit module) |
| 3 | Thiếu DDL / Schema suy luận | 31 | Bảng thiếu script DDL gốc, kiểu cột và PK phải suy luận từ SQL | Chuẩn hóa migration chính thức từ DB SQL Server thật | Mở DDL |
| 4 | Định dạng file Excel | 21 | Phụ thuộc DLL nguồn cũ (ExcelExport / ExcelImportNew) | Chuẩn hóa template Excel bằng bộ kiểm thử đối chiếu với file mẫu thực tế | Mở (chờ xác nhận mẫu) |
| 5 | Dữ liệu demo (Seeder Mock) | 31 | Dữ liệu danh mục tự sinh để phục vụ kiểm thử round-trip | Import dữ liệu thật từ SQL Server dump khi bàn giao | Mở (dùng trong test) |
| 6 | Phụ thuộc chưa port & Khóa ngoại | 30 | Thiếu bảng liên quan (Mst_NNT, Mst_Area, Mst_GovTaxID, Staff_*, Train_*) và ràng buộc FK | Hoãn kiểm tra FK; kích hoạt lại sau khi port xong các bảng cha | Mở (chờ thứ tự port) |
| 7 | Bất thường nghiệp vụ nguồn | 8 | Lỗi logic tiềm ẩn ở nguồn (rows[0], xóa bảng, hardcode, timezone) | Phòng vệ mã port, bọc transaction, ghi chú cảnh báo | Đã phòng vệ trong mã port |
| 8 | Dịch vụ ngoài & Thiếu nguồn backend | 8 | Phụ thuộc iNOS Master Server, Hyundai DMS, hoặc thiếu hẳn hàm Biz backend | Áp dụng cơ chế xung đột: BỎ các action thiếu backend; hoãn các action phụ thuộc hạ tầng ngoài | 3 Đã bỏ, 5 Tạm hoãn |
| 9 | Bảo mật & Xử lý ngoại lệ (PORT_SANITIZE_ERRORS) | 1 | Thông báo lỗi mặc định trả nguyên `ex.Message` lộ chi tiết kỹ thuật | Đã tích hợp công tắc môi trường `PORT_SANITIZE_ERRORS=1` che giấu chi tiết lỗi khi triển khai production | Đã có công tắc hạ tầng |
| 10 | Định dạng API & Serialization | 1 | JSON PascalCase chỉ áp dụng cho namespace `Controllers.Port` | Duy trì `PortJsonResultFilter` để bảo toàn tính tương thích 1-1 cho API port và bảo vệ API cũ/SPA | Đã xử lý bằng hạ tầng (02911b1) |
| 11 | Thuộc tính mở rộng vô hại (`MyIdxSeq`) | 1 | Một số action đọc trả thêm `MyIdxSeq` mà model nguồn không có | Quyết định Claude: chấp nhận là GAP vô hại, tay cầm kiểm chứng thứ tự của agent, không cần sửa lại | Rủi ro đã chấp nhận |
| 12 | Hiệu năng xử lý: Import Department O(N²) | 1 | Import 5.000 dòng mất 57s do mỗi dòng gọi UpdBU toàn bảng | Quyết định Claude: khớp hành vi nguồn cả về hiệu năng; ghi nhận tối ưu hóa về sau (UpdBU 1 lần sau vòng lặp) | Ghi nhận tối ưu sau |

> *Ghi chú thống kê*: Một hạng mục trong file log có thể đồng thời ghi nhận nhiều loại GAP (ví dụ: vừa thiếu phân quyền, vừa thiếu ghi log, vừa thiếu DDL). Tổng số lượt ghi nhận là 366 lượt trên 133 hạng mục có báo cáo GAP và 4 mục hạ tầng / quyết định toàn cục.

---

## 2. Phân tích chi tiết từng nhóm GAP & Hướng xử lý

### 2.1. Phân quyền và ngữ cảnh người dùng (Phân quyền)
- **Mô tả**: Hầu hết các action nguồn đều gọi `CheckGatewayAuthentication()`, `CheckAuthorize(ref orgUserUI)`, `Sys_User_CheckAuthorize()`, hoặc truy vấn quyền đại lý `myCache_Mst_PromotionProgram_ViewAbility_Get`. Khi port sang MiniLoyalty, các kiểm tra này được đánh dấu `Đ` (đơn giản hóa có chủ đích) và tạm thời cho qua.
- **Tác động**: Mọi client gọi tới endpoint đều được thực thi mà không bị chặn phân quyền; các trường dữ liệu audit (`LogLUBy`, `OrgID`, `NetworkID`) tạm thời nhận giá trị cố định hoặc lấy từ cấu hình: `LogLUBy = "port"`, `OrgID` theo `Biz_OrgID`, `NetworkID` theo `Biz_NetworkID` (mặc định `"1001"`).
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
- **Mô tả**: Các bảng danh mục (`Mst_Province`, `Mst_District`, `Mst_Ward`, `Mst_CarBrand`, `Mst_CardType`, `Mst_BirthPolicy`, `Mst_GovIDType`, `Mst_Grade`, `Mst_Gender`, `Mst_LeaveDay`, `Mst_LeaveType`, `Mst_Major`, `Mst_MajorGroup`, `Mst_NNT`, `Mst_PersonalBackground`, `Mst_PolicyExpenseType`...) sử dụng `IPortSeeder` để nạp dữ liệu mẫu ban đầu nhằm đảm bảo kiểm thử round-trip chạy được.
- **Tác động**: Dữ liệu chỉ mang tính minh họa, chưa có toàn bộ danh mục thực tế của khách hàng.
- **Hướng xử lý**: Tách bạch seeder môi trường Development và seeder môi trường Production; cung cấp script ETL đồng bộ dữ liệu thật từ SQL Server sang SQLite/PostgreSQL.

### 2.5. Phụ thuộc bảng chưa port và Khóa ngoại (Phụ thuộc chưa port)
- **Mô tả**: Một số endpoint xóa (`Delete`, `DeleteMultiple`) hoặc thêm mới ở nguồn có kiểm tra khóa ngoại (FK) sang các bảng khác trước khi cho phép xử lý (ví dụ: `Mst_Department` kiểm tra `Staff_MapDepartment`, `Train_CourseDepartment`; `Mst_CardType` kiểm tra các bảng giao dịch thẻ; `Mst_DealerCP` kiểm tra `Map_QueryDealer_Member`, `Prm_CarNew`; `Mst_NNT` cần các bảng `Mst_GovTaxID`, `Mst_Area`; `Mst_PolicyExpenseType` kiểm tra `Mst_ExpenseType`). Vì các bảng này chưa port nên mã port chưa thể kích hoạt các câu lệnh kiểm tra này.
- **Tác động**: Nếu thao tác dữ liệu cha, các bản ghi con ở hệ thống cũ có thể bị mất tính toàn vẹn tham chiếu.
- **Hướng xử lý**: Lưu vết danh sách FK; sau khi hoàn thành các nhóm bảng giao dịch và người dùng, quay lại bổ sung các guard kiểm tra FK trước khi xóa.

### 2.6. Thiếu kịch bản DDL chính thức (Thiếu DDL / Schema)
- **Mô tả**: Nhiều bảng danh mục không tìm thấy file `.sql` tạo bảng trong thư mục `Migrate\`. Tên cột, kiểu dữ liệu và khóa chính được suy đoán từ thuộc tính C# và mã nguồn gọi SQL (như `Mst_FamilyStatus`, `Mst_GovIDType`, `Mst_Grade`, `Mst_Gender`, `Mst_LeaveDay`, `Mst_LeaveType`, `Mst_Major`, `Mst_MajorGroup`, `Mst_PersonalBackground`).
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

### 2.8. Dịch vụ ngoài & Thiếu nguồn backend (CAN_XEM_TAY & Bỏ)
- **Bỏ do thiếu nguồn backend (#159, #160, #161)**:
  - Tại hạng mục #159 (`Mst_GovIDTypeController/Create`), #160 (`Update`), và #161 (`Delete`), tầng ClientGate gọi qua ClientService tới endpoint dịch vụ bên ngoài `MstSv` (`WA_Mst_GovIDType_Create/Update/Delete`).
  - Trong toàn bộ codebase backend (`11.BackEnd\...\idn.Loyalty.Biz` và `idn.Loyalty.Biz.Web`), hoàn toàn **không tồn tại** các hàm `WA_Mst_GovIDType_*` hay hàm nghiệp vụ tương ứng; không có DDL và không có mã lỗi (chi tiết tại báo cáo xung đột `../../.parity-run/conflicts/159_conflict.md`).
  - **Quyết định Claude (16/10)**: BỎ cả 3 hạng mục #159, #160, #161, đưa vào `skipped.json` và ban hành Bài học B.7 trong `LESSONS_AGENTS.md`: *Thiếu nguồn backend = không port; cấm phỏng đoán quy tắc/mã lỗi*.
- **Danh sách toàn bộ 8 hạng mục trong `skipped.json`**:
  1. `#159 Mst_GovIDTypeController/Create` (`BO`): Thiếu nguồn backend trong `idn.Loyalty.Biz`, gate gọi dịch vụ ngoài `MstSv`.
  2. `#160 Mst_GovIDTypeController/Update` (`BO`): Cùng lý do #159, thiếu backend trong Biz.
  3. `#161 Mst_GovIDTypeController/Delete` (`BO`): Cùng lý do #159, thiếu backend trong Biz.
  4. `#115 Mst_DealerCPController/Create` (`HOAN_PHU_THUOC`): Gọi `Mst_NNT_CreateForNetworkCP` ghi ~45 cột `Mst_NNT` và kiểm tra quyền `Sys_User`. Bản `CreateOld` (#114, 03e9623) đã port xong; bản Create chính thức hoãn sau `Mst_NNT` và `Sys_User`.
  5. `#119 Mst_DealerCPController/Import` (`HOAN_PHU_THUOC`): Tương tự #115, phụ thuộc `WA_Mst_NNT_CreateForNetworkCP` và `Sys_User`.
  6. `#068 Map_QueryDealer_MemberController/Create` (`CAN_XEM_TAY`): Phần DB nội bộ port được, nhưng sau khi lưu có gọi Web Service ngoài của Hyundai DMS (`WSDMS CarSv_Ser_CustomerCar_Get`, `Ser_CustomerCar_SalesCreate`). Chờ quyết định stub WS / port DB + GAP / bỏ.
  7. `#181 Mst_OrgController/GetAllOrg` (`CAN_XEM_TAY`): Gọi API ngoài iNOS Master Server (`WA_OS_Inos_Org_GetMyOrgList`) để lấy Org chưa có trong `Mst_Org`. MiniLoyalty chưa có hạ tầng master server.
  8. `#182 Mst_OrgController/GetOrgsNotInSolution` (`CAN_XEM_TAY`): Tương tự #181, phụ thuộc API iNOS Master Server.

### 2.9. Bảo mật & Xử lý ngoại lệ: Công tắc `PORT_SANITIZE_ERRORS`
- **Mô tả**: Tại hạ tầng xử lý lỗi `Data/PortResult.cs` và `Data/PortFilters.cs`, các ngoại lệ chưa bắt được chuyển đổi thành HTTP 400 `PortResult.Fail("<Controller>_<Action>", "Exception", ex.Message)`.
- **Hành vi mặc định**: Mặc định công tắc TẮT (biến môi trường không đặt hoặc khác `"1"`). Hệ thống trả nguyên văn chuỗi `ex.Message` ra đối tượng JSON phản hồi. Điều này hoàn toàn khớp với hành vi nguồn (`AddException` tại Gate), cho phép agent và đội ngũ kiểm thử đối chiếu chính xác từng mã lỗi SQL và nguyên nhân lỗi.
- **Công tắc Production**: Khi cấu hình `$env:PORT_SANITIZE_ERRORS = "1"`:
  ```csharp
  var sanitize = Environment.GetEnvironmentVariable("PORT_SANITIZE_ERRORS") == "1";
  p[key] = sanitize && key == "Exception" 
      ? "Lỗi xử lý dữ liệu (chi tiết đã ghi nhật ký máy chủ)." 
      : paramCouples[i + 1];
  ```
  Thông điệp kỹ thuật lộ cấu trúc DBMS, tên bảng, tên cột hoặc đường dẫn tệp tin trên máy chủ sẽ được thay thế bằng thông báo thân thiện. Chi tiết lỗi gốc vẫn được `PortExceptionFilter` ghi đầy đủ vào nhật ký máy chủ (`ILogger.LogError`).
- **Trạng thái**: Đã sẵn sàng tại hạ tầng; rủi ro được chấp nhận trong môi trường phát triển và kiểm thử nội bộ.

### 2.10. Định dạng API & Serialization: JSON PascalCase theo namespace
- **Mô tả**: Hệ thống nguồn sử dụng serializer trả về JSON định dạng PascalCase (`Success`, `Data`, `DataList`, `ItemCount`, `ErrorCode`, `Params`). .NET 8 mặc định camelCase.
- **Giải pháp**: Lớp `PortJsonResultFilter` (commit `02911b1`) chỉ áp dụng cấu hình `PropertyNamingPolicy = null` cho các controller thuộc namespace `MiniLoyalty.Controllers.Port`. Các controller cũ ngoài namespace này (`/api/v1/*`, `/api/earn`, SPA UI) vẫn giữ camelCase tiêu chuẩn của .NET 8.
- **Trạng thái**: Đã xử lý triệt để tại hạ tầng.

### 2.11. Thuộc tính mở rộng vô hại: `MyIdxSeq` trong các action đọc
- **Mô tả**: QA-qa1-6 phát hiện các action đọc (`GetAllActive`, `GetBy*`, `Search`) của một số controller danh mục (như #170, #171 `Mst_MajorGroup`) trả thêm trường `MyIdxSeq` trong đối tượng JSON phản hồi, trong khi model nguồn không có trường này.
- **Đánh giá & Quyết định**: Claude đã phê duyệt chính thức (Bài học D trong `LESSONS_AGENTS.md`): CHẤP NHẬN là sai khác đã khai báo (GAP) vô hại. Đây là tay cầm nội bộ được agent viết code sử dụng để kiểm chứng thứ tự sắp xếp; không phá vỡ hợp đồng dữ liệu phía client và không cần sửa lại mã đã commit.

### 2.12. Hiệu năng xử lý: Import Department O(N²) do UpdBU toàn bảng
- **Mô tả**: QA-qa2-8 ghi nhận khi import 5.000 dòng phòng ban tại #129 (`Mst_DepartmentController/Import`), thời gian thực thi mất 57 giây. Nguyên nhân do mỗi dòng được import đều gọi hàm cập nhật toàn bộ bảng `UpdBU` (dẫn đến độ phức tạp O(N²)).
- **Đánh giá & Quyết định**: Claude quyết định: GHI NHẬN, KHÔNG ĐỔI mã trong giai đoạn chuyển đổi. Lý do: mã nguồn gốc cũng gọi UpdBU toàn bảng ở mỗi lời gọi `Create`, do đó mã đích khớp hoàn toàn với nguồn cả về mặt hiệu năng. Tối ưu hóa (thực hiện UpdBU một lần duy nhất sau vòng lặp) sẽ được thực hiện ở giai đoạn tối ưu hóa hệ thống sau này, vì việc này can thiệp vào ranh giới transaction dữ liệu.

---

## 3. Sổ đăng ký chi tiết từng hạng mục (133 hạng mục + 4 mục hạ tầng)

| Hạng mục # | Tên hạng mục | Nhóm GAP phân loại | Nội dung chi tiết được ghi nhận | Trạng thái |
|---|---|---|---|---|
| HT-01 | Hạ tầng toàn cục (PortResult / PortFilters) | Bảo mật & Xử lý ngoại lệ | Thông báo lỗi mặc định trả nguyên `ex.Message` để đối chiếu 1-1 với nguồn; công tắc `PORT_SANITIZE_ERRORS=1` che giấu chi tiết lỗi lộ tên DBMS/bảng/đường dẫn máy chủ khi chạy production | Đã có công tắc tại Data/PortResult.cs |
| HT-02 | Hạ tầng toàn cục (PortJsonResultFilter) | Định dạng API & Serialization | JSON PascalCase chỉ áp cho namespace `Controllers.Port` (commit `02911b1`); các endpoint cũ (/api/v1, /api/earn) và SPA giữ nguyên camelCase | Đã xử lý bằng hạ tầng |
| HT-03 | Hạ tầng & Action đọc (QA-qa1-6) | Thuộc tính mở rộng vô hại | Một số action đọc (GetAllActive, GetBy*, Search) trả thêm `MyIdxSeq` mà model nguồn không có; Claude chốt: chấp nhận là GAP vô hại, tay cầm kiểm chứng thứ tự | Rủi ro đã chấp nhận (Quyết định Claude) |
| HT-04 | Mst_DepartmentController_Import (QA-qa2-8) | Hiệu năng xử lý | Import 5.000 dòng mất 57 giây do mỗi dòng gọi UpdBU toàn bảng (O(N²)); khớp hành vi nguồn cả về hiệu năng; ghi nhận tối ưu hóa về sau | Ghi nhận tối ưu sau |
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
| 110 | Mst_DealerCPController_Search | Phân quyền, Ghi log, Dữ liệu demo, Bất thường nghiệp vụ | DISTINCT trên IDENTITY; dữ liệu demo; phân quyền; log | Đã phòng vệ trong code |
| 111 | Mst_DealerCPController_GetAllActive | Phân quyền, Ghi log | phân quyền; log | Mở |
| 112 | Mst_DealerCPController_GetByDLCPCode | Phân quyền, Ghi log, Bất thường nghiệp vụ | tài khoản viết cứng tại file:dòng; phân quyền; log | Đã phòng vệ trong code |
| 113 | Mst_DealerCPController_GetByOrgID | Phân quyền, Ghi log | phân quyền; log | Mở |
| 114 | Mst_DealerCPController_CreateOld | Phân quyền, Ghi log | phân quyền; log; LogLUBy chưa có user | Mở |
| 115 | Mst_DealerCPController_Create | Phụ thuộc chưa port, Phân quyền | Gọi Mst_NNT_CreateForNetworkCP ghi ~45 cột Mst_NNT và kiểm tra Sys_User | HOAN_PHU_THUOC |
| 116 | Mst_DealerCPController_Update | Phân quyền, Ghi log | phân quyền; log; LogLUBy chưa có user | Mở |
| 117 | Mst_DealerCPController_Delete | Phân quyền, Ghi log, Phụ thuộc chưa port | FK Map_QueryDealer_Member/Prm_CarNew; phân quyền; log | Mở |
| 118 | Mst_DealerCPController_DeleteMultiple | Phân quyền, Ghi log, Phụ thuộc chưa port | FK Map_QueryDealer_Member/Prm_CarNew; phân quyền; log | Mở |
| 119 | Mst_DealerCPController_Import | Phụ thuộc chưa port, Phân quyền | Gọi WA_Mst_NNT_CreateForNetworkCP ghi Mst_NNT và kiểm tra Sys_User | HOAN_PHU_THUOC |
| 120 | Mst_DealerCPController_Export | Định dạng file Excel | định dạng ô Excel (DLL nguồn) cần xác nhận | Mở |
| 121 | Mst_DealerCPController_ExportTemplate | Định dạng file Excel | định dạng ô Excel (DLL nguồn) cần xác nhận | Mở |
| 122 | Mst_DepartmentController_Search | Phân quyền, Ghi log, Dữ liệu demo, Thiếu DDL / Schema | không có DDL; dữ liệu demo; phân quyền; log | Mở |
| 123 | Mst_DepartmentController_GetAllActive | Phân quyền, Ghi log | phân quyền; log | Mở |
| 124 | Mst_DepartmentController_GetByDepartmentCode | Phân quyền, Ghi log | phân quyền; log | Mở |
| 125 | Mst_DepartmentController_Create | Phân quyền, Ghi log | phân quyền; log; LogLUBy chưa có user | Mở |
| 126 | Mst_DepartmentController_Update | Phân quyền, Ghi log | phân quyền; log; LogLUBy chưa có user | Mở |
| 127 | Mst_DepartmentController_Delete | Phân quyền, Ghi log, Phụ thuộc chưa port | FK Staff_MapDepartment/Train_CourseDepartment; phân quyền; log | Mở |
| 128 | Mst_DepartmentController_DeleteMultiple | Phân quyền, Ghi log, Phụ thuộc chưa port | FK Staff_MapDepartment/Train_CourseDepartment; phân quyền; log | Mở |
| 129 | Mst_DepartmentController_Import | Phân quyền, Ghi log, Hiệu năng xử lý | Hiệu năng O(N²) do UpdBU toàn bảng mỗi dòng; phân quyền; log | Ghi nhận tối ưu sau |
| 130 | Mst_DepartmentController_Export | Định dạng file Excel | cột Phòng ban cấp trên (mã hay tên) của DLL nguồn cần đối chiếu mẫu thật | Mở |
| 131 | Mst_DepartmentController_ExportTemplate | Định dạng file Excel | định dạng ô Excel (DLL nguồn) cần xác nhận | Mở |
| 132 | Mst_DisciplineController_Search | Phân quyền, Ghi log, Dữ liệu demo, Thiếu DDL / Schema | không có DDL; dữ liệu demo; phân quyền; log | Mở |
| 133 | Mst_DisciplineController_GetByDisciplineCode | Phân quyền, Ghi log | phân quyền; log | Mở |
| 134 | Mst_DisciplineController_GetAllActive | Phân quyền, Ghi log | phân quyền; log | Mở |
| 135 | Mst_DisciplineController_Create | Phân quyền, Ghi log | phân quyền; log; LogLUBy chưa có user | Mở |
| 136 | Mst_DisciplineController_Update | Phân quyền, Ghi log | phân quyền; log; LogLUBy chưa có user | Mở |
| 137 | Mst_DisciplineController_Delete | Phân quyền, Ghi log | phân quyền; log | Mở |
| 138 | Mst_DisciplineController_DeleteMultiple | Phân quyền, Ghi log | phân quyền; log | Mở |
| 139 | Mst_DistrictController_Search | Phân quyền, Ghi log, Dữ liệu demo, Thiếu DDL / Schema | không có DDL; dữ liệu demo; phân quyền; log | Mở |
| 140 | Mst_DistrictController_GetDetail | Phân quyền, Ghi log | phân quyền; log | Mở |
| 141 | Mst_DistrictController_GetByProvinceCode | Phân quyền, Ghi log | phân quyền; log | Mở |
| 142 | Mst_DistrictController_GetAllActive | Phân quyền, Ghi log | phân quyền; log | Mở |
| 143 | Mst_DistrictController_Create | Phân quyền, Ghi log | phân quyền; log; LogLUBy chưa có user | Mở |
| 144 | Mst_DistrictController_Update | Phân quyền, Ghi log | phân quyền; log; LogLUBy chưa có user | Mở |
| 145 | Mst_DistrictController_Delete | Phân quyền, Ghi log, Phụ thuộc chưa port | FK Mst_Ward; phân quyền; log | Mở |
| 146 | Mst_DistrictController_Import | Phân quyền, Ghi log | NetworkID từ user; định dạng file là giả định; phân quyền; log | Mở |
| 147 | Mst_DistrictController_Export | Định dạng file Excel | định dạng ô Excel (DLL nguồn) cần xác nhận | Mở |
| 148 | Mst_DistrictController_ExportTemplate | Định dạng file Excel | định dạng ô Excel (DLL nguồn) cần xác nhận | Mở |
| 149 | Mst_EthnicCategoryController_Search | Phân quyền, Ghi log, Dữ liệu demo, Thiếu DDL / Schema | không có DDL; dữ liệu demo; phân quyền; log | Mở |
| 150 | Mst_EthnicCategoryController_GetAllActive | Phân quyền, Ghi log | phân quyền; log | Mở |
| 151 | Mst_ExpenseTypeController_GetAllActive | Phân quyền, Ghi log | CheckAuthorize / user đăng nhập; log RequestLog/ReturnLog; Ext tree | Mở |
| 152 | Mst_FamilyStatusController_Search | Phân quyền, Ghi log, Dữ liệu demo, Thiếu DDL / Schema | không có DDL; dữ liệu demo; phân quyền; log | Mở |
| 153 | Mst_FamilyStatusController_GetAllActive | Thiếu DDL / Schema, Phân quyền, Ghi log | không có DDL/kiểu cột; Ft_RecordStart/Count nguồn chưa xác minh; mã lỗi chuỗi chưa xác minh; auth/log bỏ (Đ); Order 32 trùng Mst_Department (đã xử lý ở A.4) | Mở |
| 154 | Mst_GenderController_Search | Phân quyền, Ghi log, Dữ liệu demo, Thiếu DDL / Schema | không có DDL; dữ liệu demo; phân quyền; log | Mở |
| 155 | Mst_GenderController_GetAllActive | Phân quyền, Ghi log | phân quyền; log | Mở |
| 156 | Mst_GovIDTypeController_Search | Phân quyền, Ghi log, Dữ liệu demo, Thiếu DDL / Schema | auth (CheckAuthorize); NetworkID không lọc; PK/kiểu cột chưa xác minh (DDL không có); dữ liệu demo; log | Mở |
| 157 | Mst_GovIDTypeController_GetByGovIDType | Thiếu DDL / Schema, Phân quyền, Ghi log | không DDL; verb gate chưa xác minh; Trim/IsNullOrEmpty nguồn chưa xác minh; phân quyền/log bỏ (Đ) | Mở |
| 158 | Mst_GovIDTypeController_GetAllActive | Thiếu DDL / Schema, Dữ liệu demo, Phân quyền, Ghi log | Ft_RecordStart/Count chưa xác minh; không DDL; dữ liệu demo; phân quyền/log bỏ (Đ) | Mở |
| 159 | Mst_GovIDTypeController_Create | Dịch vụ ngoài, Thiếu nguồn backend | Thiếu nguồn backend: gate gọi dịch vụ ngoài MstSv, idn.Loyalty.Biz không có hàm Create/mã lỗi (xem conflicts/159_conflict.md); không thể port 1-1 | BỎ (Claude chốt 16/10) |
| 160 | Mst_GovIDTypeController_Update | Dịch vụ ngoài, Thiếu nguồn backend | Thiếu nguồn backend: gate gọi dịch vụ ngoài MstSv, idn.Loyalty.Biz không có hàm Update/mã lỗi | BỎ (Claude chốt 16/10) |
| 161 | Mst_GovIDTypeController_Delete | Dịch vụ ngoài, Thiếu nguồn backend | Thiếu nguồn backend: gate gọi dịch vụ ngoài MstSv, idn.Loyalty.Biz không có hàm Delete/mã lỗi | BỎ (Claude chốt 16/10) |
| 162 | Mst_GradeController_Search | Thiếu DDL / Schema, Dữ liệu demo, Phân quyền, Ghi log | DDL Mst_Grade chưa xác minh (kiểu/PK suy ra); dữ liệu demo; phân quyền/auth bỏ; log; hình dáng phản hồi lỗi | Mở |
| 163 | Mst_GradeController_GetAllActive | Thiếu DDL / Schema, Phân quyền, Ghi log | Ft_RecordStart/Count chưa xác minh (dùng tạm 0/123456000); verb chưa xác minh (POST); không có DDL; phân quyền; log | Mở |
| 164 | Mst_LeaveDayController_Search | Phân quyền, Ghi log, Dữ liệu demo, Thiếu DDL / Schema | DDL Mst_LeaveDay chưa có trong repo (.sql); dữ liệu demo; phân quyền; log | Mở |
| 165 | Mst_LeaveDayController_GetAllActive | Phân quyền, Ghi log | phân quyền; log | Mở |
| 166 | Mst_LeaveTypeController_Search | Thiếu DDL / Schema, Dữ liệu demo, Phân quyền, Ghi log | không có DDL; dữ liệu demo; phân quyền; log | Mở |
| 167 | Mst_LeaveTypeController_GetAllActive | Phân quyền, Ghi log | phân quyền; log | Mở |
| 168 | Mst_MajorController_Search | Thiếu DDL / Schema, Dữ liệu demo, Phân quyền, Ghi log | không có DDL (.sql) — kiểu cột và PK suy từ SQL GetX; dữ liệu demo; phân quyền; log | Mở |
| 169 | Mst_MajorController_GetAllActive | Phân quyền, Ghi log | phân quyền; log | Mở |
| 170 | Mst_MajorGroupController_Search | Thiếu DDL / Schema, Dữ liệu demo, Phân quyền, Ghi log, MyIdxSeq | không có DDL (.sql); dữ liệu demo; phân quyền; log; thêm MyIdxSeq vô hại | Mở |
| 171 | Mst_MajorGroupController_GetAllActive | Thiếu DDL / Schema, Dữ liệu demo, Phân quyền, Ghi log, MyIdxSeq | không có DDL; dữ liệu demo; phân quyền; log; thêm MyIdxSeq vô hại | Mở |
| 172 | Mst_NNTController_Search | Phụ thuộc chưa port, Phân quyền, Ghi log | Mst_GovTaxID, Mst_Area chưa có ở repo đích (các cột mgt_*, ma_* null); phân quyền; log | Mở |
| 178 | Mst_OrgController_Search | Phân quyền, Ghi log, Phụ thuộc chưa port | Mst_NNT còn 60 cột chưa làm; hệ phân quyền; ghi log | Mở |
| 179 | Mst_OrgController_GetAllActive | Phân quyền, Ghi log | phân quyền; log | Mở |
| 181 | Mst_OrgController_GetAllOrg | Dịch vụ ngoài | Gọi API iNOS Master Server WA_OS_Inos_Org_GetMyOrgList; chưa có hạ tầng đăng ký | CAN_XEM_TAY |
| 182 | Mst_OrgController_GetOrgsNotInSolution | Dịch vụ ngoài | Phụ thuộc API iNOS Master Server WA_OS_Inos_Org_GetMyOrgList | CAN_XEM_TAY |
| 187 | Mst_ParamSysController_GetAllActive | Phân quyền, Ghi log, Bất thường nghiệp vụ | thứ tự khi trùng LogLUDTimeUTC; phân quyền; log | Đã phòng vệ trong code |
| 188 | Mst_PersonalBackgroundController_Search | Thiếu DDL / Schema, Phân quyền, Ghi log | không có DDL trong repo — PK = PersonalBGCode (khóa draft), kiểu cột cần xác nhận; bỏ qua CheckGatewayAuthentication, CheckAuthorize, RequestLog/ReturnLog, Sys_Access_CheckDenyV30 | Mở |
| 189 | Mst_PersonalBackgroundController_GetAllActive | Thiếu DDL / Schema, Dữ liệu demo, Phân quyền, Ghi log, Thuộc tính mở rộng vô hại | PK suy luận PersonalBGCode; không có DDL; dữ liệu demo; bỏ qua CheckGatewayAuthentication, CheckAuthorize; trả thêm MyIdxSeq (GAP đã chấp nhận) | Mở |
| 190 | Mst_PolicyExpenseTypeController_Search | Phụ thuộc chưa port, Dữ liệu demo, Phân quyền, Ghi log | FK ExpenseType (Mst_ExpenseType); dữ liệu demo; phân quyền; log | Mở |
| 191 | Mst_PolicyExpenseTypeController_GetAllActive | Phụ thuộc chưa port, Dữ liệu demo, Phân quyền, Ghi log | FK ExpenseType (Mst_ExpenseType); dữ liệu demo; phân quyền; log | Mở |
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

## 4. Phương pháp đối chiếu máy móc & Ngày giờ đối chiếu

- **Thời điểm đo lường đối chiếu**: **2026-10-16 13:35** (giờ địa phương).
- **Lệnh quét tự động toàn bộ GAP từ các file nhật ký**:
  ```powershell
  $logDir = "C:\Users\Public\Pictures\kientran_private\repos\MiniLoyalty\.parity-run\logs"
  $folders = Get-ChildItem -Path $logDir -Directory
  $gaps = @()
  foreach ($f in $folders) {
      $wlogs = Get-ChildItem -Path $f.FullName -Filter "write_r*.txt" | Where-Object { $_.Name -notmatch 'prompt' }
      foreach ($w in $wlogs) {
          $c = Get-Content $w.FullName -Raw
          if ($c -match 'GAP:\s*([^\r\n]+)') {
              $gaps += [PSCustomObject]@{ Folder = $f.Name; Gap = $Matches[1].Trim() }
          }
      }
  }
  Write-Host "Tổng số nhật ký có GAP: $($gaps.Count)"
  ```
- **Lệnh đối chiếu trạng thái loại bỏ và tạm hoãn trong skipped.json**:
  ```powershell
  Get-Content "C:\Users\Public\Pictures\kientran_private\repos\MiniLoyalty\.parity-run\skipped.json" -Raw -Encoding utf8 | ConvertFrom-Json
  ```

---

## 5. Lịch sử cập nhật

| Phiên bản | Ngày cập nhật | Người thực hiện | Nội dung thay đổi |
|---|---|---|---|
| 1.0 | 2026-10-16 11:15 | doc2 (Quản lý quy trình) | Khởi tạo sổ đăng ký GAP tổng hợp toàn bộ 112 file nhật ký thực tế và skipped.json; phân loại 8 nhóm nghiệp vụ kèm hướng xử lý. |
| 2.0 | 2026-10-16 12:00 | doc2 (Quản lý quy trình) | Cập nhật đợt 2: bổ sung 8 hạng mục mới hoàn thành (#130, #131, #153, #156, #157, #158, #162, #163); bổ sung 2 GAP hạ tầng toàn cục (thông báo lỗi trả nguyên ex.Message và JSON PascalCase theo namespace). |
| 2.1 | 2026-10-16 12:12 | doc2 (Quản lý quy trình) | Cập nhật realtime bổ sung #154 (Mst_GenderController/Search) và #164 (Mst_LeaveDayController/Search); nâng số lượng nhật ký rà soát lên 122 file; tính toán lại toàn bộ số liệu theo PARITY_PROGRESS.md (123 Đã làm, 1 Bỏ, 213 Chưa làm). |
| 3.0 | 2026-10-16 13:35 | doc2 (Quản lý quy trình) | Cập nhật đợt 3: quét 145 thư mục nhật ký (133 nhật ký có GAP); bổ sung 5 nhóm GAP và quyết định mới từ LESSONS mục D: (1) `MyIdxSeq` trong kết quả đọc là GAP vô hại đã chấp nhận; (2) Import Department O(N²) do UpdBU toàn bảng mỗi dòng khớp nguồn, ghi nhận tối ưu sau; (3) Công tắc `PORT_SANITIZE_ERRORS=1` tại `Data/PortResult.cs` che giấu chi tiết lỗi khi deploy production; (4) GovIDType Create/Update/Delete (#159-#161) BỎ do thiếu nguồn backend (dẫn `conflicts/159_conflict.md` và liệt kê toàn bộ 8 mục trong `skipped.json`); (5) Bổ sung thời điểm và lệnh đối chiếu máy móc. |
