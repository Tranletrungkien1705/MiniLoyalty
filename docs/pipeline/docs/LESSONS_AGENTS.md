# BÀI HỌC TÍCH LUỸ CHO AGENT (đọc TRƯỚC khi làm; áp dụng hết)

> Tài liệu này được cập nhật sau mỗi đợt review (Claude hoặc agent QA đề xuất, Claude duyệt). Mỗi bài học đều đến từ một lỗi/sai sót có thật.
> Cập nhật gần nhất: 2026-10-16. Nếu một bài học mâu thuẫn với đề bài hoặc luật: KHÔNG tự quyết — ghi xung đột theo `PORT_RULES_MiniLoyalty.md` mục 6.

## A. Lỗi mã đã gặp thật
1. **CẤM đăng ký/ghi đè hàm SQLite (`CreateFunction(...)`) trong action hay `GetXAsync`.** `lower()` Unicode trên SQLite đã được đăng ký MỘT LẦN ở hạ tầng (`Data/SqliteUnicodeLower.cs`, commit 02911b1) nên tìm kiếm tiếng Việt HOA/thường có dấu (vd `ĐÀ NẴNG`, `ĐỘC`) chạy đúng — cứ viết `EF.Functions.Like(cot.ToLower(), pattern, ...)` như các port khác, không thêm gì.
2. **Ngoại lệ**: `Data/PortFilters.cs` (`PortExceptionFilter`) đã biến mọi ngoại lệ chưa bắt trong action port thành 400 `PortResult.Fail("<Controller>_<Action>", "Exception", ex.Message)`. Ở nơi có transaction, có HAI mẫu được phép (qa2 đã xác minh 24/24 endpoint trả 400 khi DB lỗi): (a) `catch (Exception) { await tx.RollbackAsync(); throw; }` — rollback rồi ném lại, filter đặt mã lỗi theo ĐÚNG tên action (nên dùng cho hàm dùng chung nhiều action như `DeleteXAsync`, `SaveCoreAsync`, `CreateXAsync`); (b) `catch (Exception ex) { await tx.RollbackAsync(); return PortResult.Fail("<Controller>_<Action>", "Exception", ex.Message); }` — chỉ khi tên hard-code là TÊN ĐÚNG của action đang chạy. CẤM hard-code tên action khác trong hàm dùng chung (lỗi QA-qa2-5/6: Delete trả mã `..._Save`). CẤM bỏ qua rollback. Phần ghi file Excel của Export vẫn bọc try/catch riêng.
11. **JSON PascalCase tự động** cho mọi controller trong namespace `MiniLoyalty.Controllers.Port` (`PortJsonResultFilter`): KHÔNG tự cấu hình naming policy, KHÔNG đổi tên trường; controller port bắt buộc nằm đúng namespace đó. Ví dụ phản hồi dùng `Success`, `Data`, `DataList`, `ItemCount`, `ErrorCode`, `Params` (PascalCase).
12. **Phân trang**: KHÔNG clamp `Ft_PageIndex` âm về 0 (nguồn trả danh sách rỗng, `PageCount=0`, giữ nguyên `PageIndex` đã gửi). Dùng đúng mẫu trong các port hiện có: `var page = (recordCount <= 0 || recordStart < 0) ? <rỗng> : await ordered.Skip((int)Math.Min(recordStart, int.MaxValue)).Take((int)Math.Min(recordCount, int.MaxValue)).ToListAsync();` (chống tràn int khi PageIndex lớn).
3. **Dùng lại hàm có sẵn** (`GetXAsync`, `CreateXAsync`, `DeleteXAsync`) thay vì chép lại truy vấn trong action mới. Đọc controller port hiện có TRƯỚC khi viết.
4. **`Order =>` của `IPortSeeder`**: grep `Order =>` trong `Models/Port` và chọn số CHƯA dùng (10–99). Không đặt trùng.
5. **`Data` khi rỗng**: GetAllActive/GetBy* trả `null` (không phải `[]`) khi không có dòng; Search trả `PageInfo` với `DataList` rỗng. Theo đúng gate nguồn của TỪNG action.
6. **Toán tử lọc phải theo nguồn, đừng đoán**: `FlagActive` có nơi dùng `=` (nguyên chuỗi, `"0,1"` → 0 dòng), có nơi `IN` tách dấu phẩy. Đọc hàm lọc `strWhereClause_*` của đúng action. KeyWord lọc theo cột TÊN, không phải cột mã.
7. **`Ft_PageSize = 0`** → `DataList` rỗng, `PageCount = 0`, `ItemCount` vẫn là tổng. Âm → xử lý như đề bài nêu, không tự clamp khác nguồn.
8. **Body rỗng**: dùng `[FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)]` với tham số nullable cho mọi action nhận body.
9. **Import nhiều dòng**: kiểm TẤT CẢ dòng trước khi tạo (nếu nguồn làm vậy), rồi mỗi dòng một transaction; lỗi dòng k → các dòng trước k đã commit (import một phần), trả đúng mã lỗi của `CreateXAsync`.
10. **Không chép thông tin đăng nhập** (GwUserCode/GwPassword, token, connection string) của nguồn — kể cả trong comment.

## B. Cách làm việc của agent
1. **Chỉ báo điều bạn đã CHẠY.** Chưa chạy thì ghi `verify PENDING`. Không viết "đã xong" khi thiếu bằng chứng (lệnh + kết quả thật).
2. **Không sao chép/tham khảo file của agent khác hay đề bài của hạng mục khác** (đã có agent chép nguyên file của agent khác rồi báo là tự làm).
3. **Chỉ làm theo tài liệu** (đề bài + luật + bài học này + nguồn). Không tự thêm tính năng, không "cải thiện" ngoài phạm vi. Thiếu/mâu thuẫn → ghi xung đột, dừng hạng mục đó.
4. **Cổng chạy thử**: dùng đúng cổng được giao; tắt app và xoá DB tạm khi xong; không đọc `StandardOutput.ReadLine()` chờ "Now listening" (treo vô hạn) — dùng vòng lặp `/healthz`.
5. **Tối đa 3 lần thử** cho cùng một lỗi build/test; sau đó dừng và báo `CAN_XEM_TAY` kèm nhật ký, KHÔNG lặp vô hạn.
6. Báo cáo cuối luôn có mục `TOM TAT` đúng khuôn của đề bài.
7. **Thiếu nguồn backend = không port.** Nếu action ở gate chỉ gọi `Service.WA_X_*` sang dịch vụ ngoài và trong `idn.Loyalty.Biz`/`Biz.Web` KHÔNG có hàm `WA_X_*`/`X_*`/`X_*X` tương ứng (grep toàn nguồn) thì KHÔNG đoán quy tắc/mã lỗi: ghi `conflicts\<số>_conflict.md` (như #159) và `TRANG_THAI: CAN_XEM_TAY`. Claude quyết: bỏ (ghi skipped.json). Đã áp dụng cho Mst_GovIDType Create/Update/Delete (#159–#161).

## C. Môi trường Windows/PowerShell
1. Không đặt tên thư mục/file là `aux`, `con`, `nul`, `prn`, `com1`... (tên thiết bị dành riêng của Windows).
2. Khi ghi file từ chuỗi có `\a`, `\r`, `\n`, `\t`, `\b`: dùng nháy đơn / here-string nguyên văn; escape sẽ làm mất ký tự (đã xảy ra: `admin`→`dmin`, `rows`→`ows`).
3. PowerShell 5.1: `New-Item` không nhận mảng đường dẫn như tham số vị trí — dùng vòng lặp.
4. Link trong tài liệu phải là link TƯƠNG ĐỐI (`../../Controllers/...`), không dùng `file:///` hay đường dẫn tuyệt đối của máy; không dùng ký hiệu LaTeX (`$...$`) — dùng ký tự `→`.
5. Chỉ nhắc tên script/công cụ nếu đã kiểm tra nó tồn tại trong repo.
6. **Mã lỗi (`ErridnLoyalty.*`), route, tên hàm, tên cột nêu trong tài liệu PHẢI copy nguyên văn từ mã bằng grep** (vd `grep -o 'PortResult.Fail("[^"]*"'`), KHÔNG tự đặt/suy tên. Lượt tài liệu 3 đã ghi 14 mã lỗi không tồn tại (vd `Mst_BirthPolicy_Save_InvalidEffDateStart` trong khi mã thật là `Mst_BirthPolicy_SaveX_InvalidEffDateStart`). Endpoint chưa port (CHƯA PORT) không được mô tả hành vi/mã lỗi như thể đã chạy.

## D. Nhật ký cải tiến (thêm dòng mới ở cuối, KHÔNG xoá dòng cũ)
| Ngày | Phát hiện | Nguồn | Hành động |
|---|---|---|---|
| 2026-10-16 | `CreateFunction("lower")` trong action (#152/#153) | Claude review | Cấm (A.1); gỡ khỏi #153 |
| 2026-10-16 | Import/Delete/DeleteMultiple `throw;` → HTTP 500 | QA-qa2-1 | Sửa commit 1752274; luật A.2 |
| 2026-10-16 | Seeder `Order` trùng (32, 35) | Claude review | Luật A.4 |
| 2026-10-16 | Run CodeGPT kẹt 37 phút, wrapper nhầm BUSY thành QUOTA | Giám sát | `cg-run.ps1` tự huỷ run kẹt qua `/api/runs/{id}/cancel`, mã thoát 7 = BUSY |
| 2026-10-16 | QA-qa1-1: tìm tiếng Việt HOA có dấu trên SQLite trả 0 dòng | qa1 | `SqliteUnicodeLowerInterceptor` ở hạ tầng (02911b1); luật A.1 cập nhật |
| 2026-10-16 | QA-qa1-2/3: PageIndex tràn int hoặc âm trả nhầm trang đầu | qa1 | Vá 16 chỗ ở 22 file (02911b1); luật A.12 |
| 2026-10-16 | QA-qa1-4: action ĐỌC không bắt ngoại lệ → 500 | qa1 | `PortExceptionFilter` toàn cục (02911b1); luật A.2 |
| 2026-10-16 | QA-qa2-8: Import 5.000 dòng Department mất 57 giây (UpdBU toàn bảng mỗi dòng, O(N²)) | qa2 | GHI NHẬN, KHÔNG ĐỔI: nguồn cũng gọi UpdBU toàn bảng ở mỗi Create nên mã đích khớp cả hiệu năng. Tối ưu (UpdBU một lần sau vòng lặp, kể cả khi dừng giữa chừng) là việc sau, cần kiểm thử riêng vì đụng ranh giới transaction |
| 2026-10-16 | QA-qa2-9: cảnh báo CS0168 sau 39eaeed | qa2 | Sửa (`catch (Exception)` khi không dùng ex) |
| 2026-10-16 | Thay entity stub Mst_NNT trong Mst_Org.cs bằng entity đầy đủ (#172) | lead + agent | Hồi quy PASS (Department Create phụ thuộc NNT, Org search). Mẫu tốt: đề bài nêu rõ xung đột tên lớp CS0101 và cách di chuyển |
| 2026-10-16 | QA-qa1-6: GetAllActive/GetBy*/Search trả thêm `MyIdxSeq` (model nguồn không có) | qa1 | QUYẾT ĐỊNH (Claude): CHẤP NHẬN là sai khác đã khai báo (GAP) — thuộc tính thừa vô hại, là tay cầm kiểm chứng thứ tự của agent; KHÔNG báo lại. Port mới theo đề bài của lead (có thể không kèm) |
| 2026-10-16 | QA-qa1-7: PromotionProgram Search mặc định `Ft_PageSize = 0` | qa1 | BÁC BỎ (báo cáo sai): chữ ký nguồn `Search(string OrgID, string KeyWord, int Ft_PageIndex, int Ft_PageSize)` không có giá trị mặc định → `int` = 0 là hành vi nguồn. Bài học: QA phải đối chiếu CHỮ KÝ nguồn trước khi gọi là "không nhất quán giữa các port" |
| 2026-10-16 | QA-qa1-8: seeder `Order` trùng (32, 12) | qa1 | Sửa (ba9f44b); `parity-step commit` tự đổi sang số trống nếu trùng |
| 2026-10-16 | QA-qa2-5/6/7: mã lỗi sai tên action trong hàm dùng chung; còn `throw;` | qa2 | Sửa 39eaeed; luật A.2 viết lại: cho phép `rollback + throw;` (filter đặt tên đúng) hoặc Fail với tên ĐÚNG action; qa2 đã xác minh 24/24 endpoint trả 400 |
| 2026-10-16 | #159 GovIDType Create: gate gọi dịch vụ ngoài, backend không có hàm | agent (xung đột) → Claude chốt | BỎ #159–#161 (skipped.json); luật B.7; cơ chế xung đột hoạt động đúng |
| 2026-10-16 | #152/#154/#162 lặp lỗi: CreateFunction, clamp PageIndex, thiếu chặn tràn | Claude review | `parity-step commit` tự chuẩn hoá phân trang + build lại, chặn commit nếu còn CreateFunction |
| 2026-10-16 | Tài liệu lượt 3 ghi 14 mã lỗi không có trong mã | Claude (đối chiếu máy móc) | Luật C.6; giao doc1 sửa (doc1-fix3) |
| 2026-10-16 | QA-qa2-4: `ex.Message` trả nguyên văn lộ tên DBMS/bảng/đường dẫn | qa2 | QUYẾT ĐỊNH (Claude): chấp nhận vì khớp nguồn (`AddException`); ghi GAP, cần bọc nếu triển khai công khai |
| 2026-10-16 | QA-qa2-2/3: Export + 18 endpoint ghi 500 khi DB lỗi (HEAD 1752274) | qa2 | Đã có PortExceptionFilter (02911b1); giao qa2 xác minh lại trên HEAD mới (qa2-r3) |
| 2026-10-16 | QA-qa1-5: JSON camelCase thay vì PascalCase nguồn | qa1 | `PortJsonResultFilter` chỉ cho namespace Port (02911b1); luật A.11. Tài liệu API (docs/api) dùng ví dụ PascalCase — giờ đã đúng thực tế |


### A.13 (Claude, 16/10, từ #186 ParamPromotion/Save do gweb viết) — hàm *_CheckDB và giá trị gán mặc định
- Khi nguồn gọi `X_CheckDB(code, flagExist, flagActiveList, flagShowList)` phải port ĐỦ các bước theo thứ tự: tồn tại → FlagActive thuộc danh sách → FlagShow (EffDateStart <= hôm nay(UTC+7).Date <= EffDateEnd). Không được chỉ kiểm "tồn tại". Mã lỗi lấy NGUYÊN VĂN từ hàm (kể cả tên copy-paste sai của dev nguồn, vd `Mst_BirthPolicy_CheckDB_FlagShowNotMatched` trong `Mst_PromotionProgram_CheckDB`) và trả bằng `PortResult.Fail("<TênLỗiKhôngTiềnTố>", "Check.xxx", giá trị, ...)` như `Mst_BirthPolicyControllerPort.cs:415-424`.
- NetworkID khi nguồn gán `strNetworkID`: `_config["Biz_NetworkID"] ?? "1001"` (inject `IConfiguration`), KHÔNG hard-code chuỗi "PORT". `LogLUBy = "port"`, `FlagActive = "1"`, `LogLUDTimeUTC = DateTime.UtcNow`.
- Nguồn "xoá cả bảng rồi insert lại" (delete where 1=1) phải port đúng (transaction: ExecuteDelete + AddRange + SaveChanges; lỗi giữa chừng → rollback, bảng giữ nguyên).
- Agent web (gweb) không chạy được app: Claude/QA phải chạy thật các ca P2 trước khi chốt.

### A.14 (Claude, 16/10, từ #234/#233 do gweb viết)
- Tham số action GetBy*/Search luôn nhận qua JSON body (`[FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] XxxReq? req`), KHÔNG dùng `[FromQuery]`/`[FromHeader]`. Nguồn lấy OrgID/NetworkID từ session/header (orgUserUI) mà hàm lọc có dùng: thêm `OrgID` TUỲ CHỌN vào body, không truyền thì không lọc.
- Join `left join Mst_NNT on OrgID` có thể ra nhiều dòng NNT cho 1 OrgID: lấy dòng đầu theo MST (OrderBy MST), KHÔNG dùng ToDictionary (ném lỗi khoá trùng).
- So sánh toán tử `=` trên chuỗi: `EF.Functions.Like(x.Col.ToLower(), PortQuery.Equal(v), PortQuery.Esc)`, không dùng `==` (phân biệt hoa/thường).

### A.15 (Claude, 16/10, từ #236 ResignReason/Create do gweb viết) — Create: mã do SEQ, kiểm tra Org, mã lỗi mặc định
- Gate Create có đoạn `Get Seq` (`WA_Seq_Common_Get`, `SeqType.<X>`) thì mã (ReasonID, DisciplineCode...) do SERVER sinh và GHI ĐÈ giá trị client gửi: thêm entity `Seq_<X>` + `PortSeq.NextAsync<Seq_<X>>` + `PortSeq.Build(<format từ Seq.cs>, ...)` như `Mst_DisciplineControllerPort.cs:282-283`; lấy seq TRƯỚC transaction lưu (seq tăng cả khi lưu lỗi). Format/bảng tra trong `11.BackEnd\...\Seq.cs` (htMap).
- `Mst_Org_CheckDB(OrgID, "1", "1")` KHÔNG được bỏ qua với lý do GAP: Mst_Org đã có entity -> kiểm OrgID tồn tại (`Mst_Org_CheckDB_OrgNotFound`) và FlagActive thuộc "1" (`Mst_Org_CheckDB_FlagActiveNotMatched`).
- Mã lỗi mặc định/body rỗng của Create là `Mst_<X>_Create` (KHÔNG phải `Mst_<X>Controller_Create`): body rỗng -> `PortResult.Fail("Mst_<X>_Create", "Check.strJson", null)`; catch -> `Fail("Mst_<X>_Create", "Exception", ex.Message)`. Mỗi nhánh lỗi sau khi mở transaction phải `await tx.RollbackAsync()` trước khi return.

### A.16 (Claude, 16/10, từ #175 NNT/Create do agy3 viết) — KHÔNG tự đặt mã lỗi của hàm *_CheckDB
- Hàm `X_CheckDB(code, flagExist, flagActiveList)` chưa được trích thì phải TỰ TRA (grep `void X_CheckDB` trong `11.BackEnd\V10\idn.Loyalty.Biz\*.cs`, mã lỗi trong `12.Dev.Common\idn.Loyalty.Constants\Error.*.cs`), KHÔNG đặt tên mã tạm. Mẫu chung: flagExist "1" không thấy -> `X_CheckDB_<Tên>NotFound` (với NNT: `Mst_NNT_CheckDB_NNTNotFound`); flagExist "0" mà đã có -> `X_CheckDB_<Tên>Exist` (`Mst_NNT_CheckDB_NNTExist`); FlagActive không thuộc danh sách -> `X_CheckDB_FlagActiveNotMatched`. Tên giữa `_CheckDB_` KHÔNG phải tên cột (NNT chứ không phải MST). Tham số đầu của Params là `Check.<Cột>`.
- Luồng gọi dịch vụ ngoài (`Inos_OrgService_*`, `OS_MstSv_*`) hoặc ghi bảng phụ không có ở đích: bỏ qua có chủ đích và ghi GAP rõ; không đoán thay thế.

### A.17 (Claude, 16/10, từ xung đột #201 và #237) — StringEqual phân biệt hoa/thường; lead KHÔNG tự đặt mã lỗi
- `CmUtils.StringUtils.StringEqual(a, b)` của nguồn là so sánh PHÂN BIỆT hoa/thường (có hàm riêng `StringEqualIgnoreCase`). Trong Update, `if (bUpd_X && !StringEqual(new, db)) CheckX(...)` port bằng `string.Equals(new, db, StringComparison.Ordinal)`: đổi tên chỉ khác hoa/thường sẽ vấp chính dòng đó ở bước kiểm trùng (Like không phân biệt hoa/thường) -> báo `..._Exist`. Giống `Mst_DepartmentControllerPort` và `Mst_PositionControllerPort` (Update).
- Lead khi soạn đề bài: mã lỗi phải GREP thật trong `12.Dev.Common\idn.Loyalty.Constants\Error.*.cs` rồi mới ghi (vd `Mst_Position_CheckDB_PositionCodeNotFound`, KHÔNG phải `..._PositionNotFound`). Không chắc thì ghi "Chưa xác minh", đừng suy ra từ tên cột.
- Khi đề bài sai so với nguồn, agent làm đúng quy trình: theo NGUỒN, ghi `conflicts\<no>_conflict.md` + CAN_XEM_TAY (xem #201) - Claude chốt: theo nguồn.

### A.18 (Claude, 16/10, từ #300 / #307 nhóm Sys_*)
- Giữ NGUYÊN lỗi của nguồn (parity 1-1) và ghi chú rõ `LỖI NGUỒN GIỮ NGUYÊN` kèm file:dòng: ví dụ `Sys_GroupController.cs:87` `Qty = x.FirstOrDefault().GroupCode.Count()` là đếm KÝ TỰ của mã nhóm (không phải số user) và chỉ khác 0 khi nhóm có >= 1 dòng Sys_UserInGroup. Đừng "sửa cho đúng ý".
- `left join`/`inner join` sang bảng khác: nếu bảng đó ĐÃ có entity trên nhánh `parity/auto` (vd Sys_Object từ #317) thì PHẢI join thật, không gán null với lý do GAP. Bảng chưa có thì mới ghi GAP. Dữ liệu seed của các bảng join phải dùng mã khớp nhau (vd Sys_Access.ObjectCode = Sys_Object.ObjectCode).
- Tham số của gate (OrgID/KeyWord/FlagActive) mà hàm `strWhereClause_*` KHÔNG dùng thì bỏ qua, nhưng vẫn nhận trong body để không lỗi.

### A.19 (Claude, 16/10, từ #240 ResignReason/Import)
- Import: danh sách cột Excel lấy từ `GetImportDicColumsTemplate()` TRONG controller nguồn (grep tên hàm trong chính file gate, không nằm ở excerpt) — KHÔNG đoán cột. Thứ tự cột = thứ tự khai báo trong Dictionary (vd ResignReason, OrgID, ReasonDesc; dòng comment `//` là KHÔNG có). Dùng `PortExcel.ReadSheet(stream, keysInOrder)`.
- OrgID của từng dòng lấy từ ô Excel khi template có cột OrgID (nguồn: `Mst_ResignReason = item`), không lấy từ session/form/header. Mã sinh bằng Guid ở gate (`item.ReasonID = Guid.NewGuid().ToString()`) thì dùng đúng Guid, KHÔNG dùng Seq (chỉ Create mới dùng Seq).
- Khi model CodeGPT treo (`model stream stalled`) và diff rỗng: chạy lại bằng `parity-step -Action fix -Worker <slot> -Engine agy -FixFile <note>`.

### A.20 (Claude, 16/10, từ #320 Sys_Object/GetFunctionByObjectCode do gweb viết)
- Hàm phụ của gate/BaseController (vd `GetSysObjectSetting()` đọc `~/App_Data/SysObjectSetting.config`) KHÔNG được "giả lập" bằng dữ liệu bịa trong controller. Phải grep định nghĩa thật (`BaseController.cs`) và port đúng nguồn dữ liệu: tệp cấu hình -> copy tệp mẫu của nguồn vào `App_Data/` của dự án và đọc từ `IWebHostEnvironment.ContentRootPath` (tệp thiếu -> null như nguồn). Web agent (gweb) không grep được nên lead phải dẫn định nghĩa hàm phụ vào đề bài.
- `Data = <List rỗng>` của nguồn là mảng rỗng `[]`, không phải null (khác với `null` khi biến gán null).

### A.21 (Claude, 16/10, từ #305 SaveMenuButtonInGroup, #311, #321)
- KHÔNG bịa thêm tham số/định dạng body mà nguồn không có (vd `strJson` lồng bên trong body, cờ `Deny`/`FlagCheckDeny`, nhận `GroupCode` ở cả 2 nơi). Body của action nhận `string strJson` ở nguồn chính LÀ đối tượng JSON đó ở đích (vd RQ_Sys_Access = {Sys_Group, Lst_Sys_Access}). Ngoại lệ gate ("strJson null") -> `PortResult.Fail("<Mã mặc định của biz>", "Check.strJson", null)`; lỗi bất ngờ (NullReference...) cứ để ném, PortExceptionFilter trả 400.
- Giá trị mặc định chỉ có khi nguồn có: `StdFlag(null)` = "0" (không tự đặt "1"). Nhánh "rỗng -> giữ nguyên danh sách của client" của gate là lỗi nguồn cần giữ nguyên (xem gate Sys_AccessController :540-559).
- `StdParam` ở biz làm Trim+ToUpper rồi LƯU mã viết hoa (SQL Server không phân biệt hoa/thường nên vẫn khớp); so sánh C# `x.Equals(y)` thì phân biệt hoa/thường. Dữ liệu seed cho các hàm Save phải khớp với bảng liên quan (Sys_Object...).

### A.22 (Claude, 16/10, từ #7 Crd_CardRank/Search, #327)
- `source_excerpt.txt` có thể CHỈ chứa tầng WAS_ (wrapper) mà KHÔNG chứa thân hàm biz thật (vd Crd_Card_CalcRank/CalcRankX ở `CardRank.cs`). Khi excerpt thiếu thân hàm: grep `DataSet X(` / `void XX(` trong `11.BackEnd\V10\idn.Loyalty.Biz\*.cs` và ĐỌC TRỌN hàm trước khi viết. Tên "Search" ở gate KHÔNG đảm bảo là tìm kiếm đơn giản: đó có thể là phép tính (xếp hạng, tổng hợp). Nếu hàm biz là phép tính, phải port ĐÚNG công thức/SQL (join bảng chính sách, case-when, bảng tạm), không thay bằng bộ lọc.
- Tham số do gate GHI ĐÈ cố định (vd `CardStatus = "APPROVE"`) KHÔNG nhận từ client. Bộ lọc nào nằm trên bảng nào (Crd_Member hay Crd_Card) phải theo đúng SQL nguồn.
- Phép chiếu của biz chỉ chọn một số cột (vd Sys_User_GetForCurrentUser: 6 cột) thì các cột còn lại trong DTO là null; KHÔNG tự tra cứu thêm bảng để điền.

### A.23 (Claude, 16/10) — Lead soạn đề bài: số cột/tên cột Excel phải GREP NGUỒN
Lead đã sai 3 lần liên tiếp (#241: 7 cột thay vì 5; #314: 3 cột thay vì 2; #315: 5 cột thay vì 3) vì bịa số cột. Lead (kể cả bản web) KHÔNG được ghi số cột/tên cột Import/Export/Template nếu chưa grep `GetImportDicColums`/`GetImportDicColumsTemplate` trong chính file gate; không chắc thì ghi "Chưa xác minh - agent phải tự tra". Agent: luôn tự tra, nguồn thắng đề bài (§6.1).

### A.24 (Claude, 16/10, từ #329 Sys_User/Create, #13, #7 — xung đột khi gộp)
- GỘP NHIỀU HẠNG MỤC CÙNG NHÓM: trước khi tạo class/file mới, grep trong `Controllers/Port` + `Models/Port` xem tên đã có chưa (vd `Crd_Card`, `Crd_CardSearchReq` do hạng mục khác đã chốt). Đặt tên DUY NHẤT theo hạng mục (`<Ctrl>_<Action>Req`, `<Ctrl>_<Action>Item`). Entity đã có trong `Models/PortLoyalty.cs`/AppDbContext (CrdCard, CrdMember...) thì DÙNG LẠI, không tạo entity thứ hai cùng bảng. Seeder KHÔNG sửa dữ liệu của seeder khác (không `foreach ... p.Value = ...`); chỉ THÊM dòng mới.
- Mật khẩu: nguồn lưu `GetEncodedHash(pw)` = base64(MD5(pw + "idocNet")) bỏ 2 ký tự cuối (`idN.TVAN.Utils/Utils.cs`). Không có phiên đăng nhập ở đích: người thực hiện nhận qua header `WAUserCode` TUỲ CHỌN; thiếu header thì coi KHÔNG phải quản trị (không cho tự cấp FlagSysAdmin = 1) — KHÔNG mặc định một tài khoản admin ("U01").
- Tham số ngoài body của gate (`flagExist`, `flagChangeAvatar`) đưa vào body JSON như trường phẳng; KHÔNG bọc thêm `strJson` lồng.
