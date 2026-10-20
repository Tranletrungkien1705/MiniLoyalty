# Tài liệu tham chiếu API: Crd_DealSerROController

- Phạm vi: Controller Crd_DealSerROControllerPort (các endpoint Search, Calc, Add của báo giá dịch vụ sửa chữa/bảo dưỡng Crd_DealSerRO)
- Cập nhật: 2026-10-18
- Phiên bản mã: d30cdab
- Trạng thái: Nháp — chờ review

---

## 1. Tóm tắt

### 1.1 Mục đích nghiệp vụ
Quản lý vòng đời nghiệp vụ báo giá dịch vụ sửa chữa/bảo dưỡng (Repair Order - RO) cho hội viên và thẻ khách hàng, bao gồm:
- Tra cứu danh sách báo giá dịch vụ theo hội viên, thẻ và phân trang kết quả.
- Tính toán thử điểm thưởng, quyền lợi và kiểm tra hợp lệ trước khi thực hiện giao dịch chính thức.
- Thêm mới báo giá dịch vụ, tích điểm xét hạng (POINTINCREASE), tích điểm tiêu dùng (CONSUMPTION), trừ tiền/điểm sử dụng (POINTUSE), ghi nhận lượt dịch vụ (SERVICETURN), thưởng khuyến mại (CONSUMPTIONPRM) và tự động xét duyệt nâng hạng thẻ.

### 1.2 Bảng nguồn và tệp đối chiếu
- Gate Search: 13.ClientGate/V10/idn.Loyalty.WebAPI/Controllers/Crd_DealSerROController.cs:29-109
- Gate Calc: 13.ClientGate/V10/idn.Loyalty.WebAPI/Controllers/Crd_DealSerROController.cs:113-184
- Gate Add: 13.ClientGate/V10/idn.Loyalty.WebAPI/Controllers/Crd_DealSerROController.cs:188-251
- Filter: 13.ClientGate/V10/idn.Loyalty.WebAPI/Controllers/Crd_DealSerROController.cs:255-270 (strWhereClause_Crd_DealSerRO)
- API Add: 11.BackEnd/V10/idn.Loyalty.Biz.Web/Controllers/CrdDealSerROController.cs:96-166
- Biz Add/Calc: 11.BackEnd/V10/idn.Loyalty.Biz/Card.Deal.cs:14810-15174 (Crd_DealSerRO_Add_New20260813) và Card.Deal.cs:12009-14441 (Crd_DealSerRO_AddX_New20260813)
- Controller Port: Controllers/Port/Crd_DealSerROControllerPort.cs:1-558
- Model Entity: Models/Port/Crd_DealSerRO.cs:1-125

### 1.3 Cấu trúc dữ liệu bảng Crd_DealSerRO
Bảng Crd_DealSerRO gồm 45 thuộc tính ánh xạ 1:1 theo model nguồn:

| Tên cột | Kiểu EF | Khóa | Ghi chú |
|---|---|---|---|
| DealSerRONo | string | PK | Số báo giá dịch vụ sửa chữa (bắt buộc) |
| NetworkID | string? | | Mã mạng |
| OrgID | string? | | Mã tổ chức |
| DLCode | string? | | Mã đại lý |
| MemberNo | string? | | Mã hội viên |
| CardNo | string? | | Số thẻ |
| DealPointType | string? | | Loại giao dịch (mặc định SERVICE) |
| CardType | string? | | Hạng thẻ hiện tại |
| CardTypeExpect | string? | | Hạng thẻ dự kiến |
| ParamCode | string? | | Mã tham số hệ thống (mặc định UNITPOINTTOMONEY) |
| PolicyCode | string? | | Mã chính sách quy đổi điểm dịch vụ |
| TotalAmountRO | double? | | Tổng giá trị báo giá sửa chữa |
| AmountRO | double? | | Giá trị báo giá dùng tích điểm / tiền xét hạng |
| AmountUse | double? | | Số tiền sử dụng từ tích lũy của hội viên |
| PointTotal | double? | | Tổng điểm được tặng khi lập báo giá |
| AmountTotal | double? | | Tổng tiền được tặng khi lập báo giá |
| PointEnd | double? | | Điểm tích lũy sau giao dịch |
| AmountEnd | double? | | Giá trị tiền tích lũy sau giao dịch |
| EffDateEnd | DateTime? | | Ngày hết hạn |
| PointAvail | double? | | Điểm khả dụng trong kỳ của thẻ |
| PolicyPointUnit | double? | | Điểm đơn vị theo chính sách |
| PolicyAmountUnit | double? | | Tiền đơn vị theo chính sách |
| ParamPointUnit | double? | | Điểm đơn vị theo tham số hệ thống |
| ParamAmountUnit | double? | | Tiền đơn vị theo tham số hệ thống |
| DealSerROStatus | string? | | Trạng thái báo giá (mặc định PENDING) |
| FlagOnlyPoint | string? | | Cờ chỉ tích điểm |
| Remark | string? | | Ghi chú |
| CreateDTimeUTC | DateTime? | | Thời điểm tạo bản ghi (UTC) |
| CreateBy | string? | | Người tạo bản ghi |
| LUDTimeUTC | DateTime? | | Thời điểm cập nhật cuối (UTC) |
| LUBy | string? | | Người cập nhật cuối |
| ApprBy | string? | | Người duyệt |
| ApprDTimeUTC | DateTime? | | Thời điểm duyệt (UTC) |
| UpdateDTimeUTC | DateTime? | | Thời điểm đồng bộ/cập nhật |
| UpdateBy | string? | | Người đồng bộ/cập nhật |
| LogLUDTimeUTC | DateTime? | | Thời điểm log cập nhật cuối (UTC) |
| LogLUBy | string? | | Người log cập nhật cuối |
| PlateNo | string? | | Biển số xe |
| VIN | string? | | Số khung xe (VIN) |
| PointRankTotal | double? | | Điểm tích xét hạng |
| PointConsumptionPrm | double? | | Điểm tích tiêu dùng khuyến mại |
| PointVoucher | double? | | Tổng điểm voucher sử dụng theo hạng |
| PointActualVoucher | double? | | Tổng điểm voucher sử dụng thực tế |
| TotalDiscount | double? | | Tổng giá trị chiết khấu |
| TotalAmountForDC | double? | | Tổng giá trị RO dùng tính chiết khấu |

---

## 2. Danh sách Endpoint

### 2.1 Endpoint: POST /api/crd_dealserro/search

#### a. Mô tả
Tìm kiếm và phân trang danh sách báo giá dịch vụ Crd_DealSerRO theo mã hội viên và số thẻ.

#### b. Tham số Request (Crd_DealSerROSearchReq)
Mã nguồn: Controllers/Port/Crd_DealSerROControllerPort.cs:25-34

| Tên tham số | Kiểu dữ liệu | Bắt buộc | Mặc định | Ý nghĩa |
|---|---|---|---|---|
| MemberNo | string? | Không | null | Mã hội viên cần lọc (so sánh bằng chính xác, không phân biệt hoa thường) |
| CardNo | string? | Không | null | Số thẻ cần lọc (so sánh bằng chính xác, không phân biệt hoa thường) |
| DLCPCode | string? | Không | null | Mã đại lý (gate nhận nhưng không lọc trong strWhereClause) |
| OrgID | string? | Không | null | Mã tổ chức (gate nhận nhưng không lọc trong SQL WHERE) |
| NetworkID | string? | Không | null | Mã mạng (gate nhận nhưng không lọc trong SQL WHERE) |
| Ft_PageIndex | int | Không | 0 | Vị trí trang (chỉ mục bắt đầu từ 0) |
| Ft_PageSize | int | Không | 10 | Số lượng bản ghi trên một trang |

#### c. Quy tắc xử lý
Theo Controllers/Port/Crd_DealSerROControllerPort.cs:100-143:
1. Nhận payload, gán giá trị mặc định nếu body null.
2. Lọc MemberNo: Sử dụng PortQuery.Equal so khớp chính xác chuỗi ký tự, không phân biệt hoa thường (gate dòng 260-263).
3. Lọc CardNo: Sử dụng PortQuery.Equal so khớp chính xác chuỗi ký tự, không phân biệt hoa thường (gate dòng 264-267).
4. Các tham số DLCPCode, OrgID, NetworkID được giữ nguyên theo mã nguồn: không đưa vào điều kiện WHERE (gate dòng 255-270; Card.Deal.cs:166-168).
5. Sắp xếp kết quả tăng dần theo LogLUDTimeUTC, thứ cấp theo DealSerRONo (Card.Deal.cs:168).
6. Phân trang dữ liệu:
   - Tính toán chỉ mục bắt đầu: recordStart = Ft_PageIndex * Ft_PageSize.
   - Nếu Ft_PageSize <= 0 hoặc recordStart < 0: trả về danh sách rỗng.
   - Ngược lại: Skip(recordStart).Take(Ft_PageSize).
7. Tính toán PageCount:
   - Nếu danh sách kết quả rỗng (kể cả khi Ft_PageSize = 0): PageCount = 0.
   - Nếu danh sách có dữ liệu: tính theo trần số trang với kích thước trang tối thiểu bằng 10 khi Ft_PageSize = 0.
8. Trả về đối tượng PageInfo bọc trong cấu trúc Success = true, Data = pageInfo.

#### d. Bảng mã lỗi
Endpoint không trả lỗi nghiệp vụ qua ErridnLoyalty mà luôn trả về HTTP 200 kèm PageInfo.

#### e. Ví dụ Request
    {
        "MemberNo": "MB01",
        "CardNo": "CARD01",
        "Ft_PageIndex": 0,
        "Ft_PageSize": 10
    }

#### f. Ví dụ Response
Chưa có lần chạy mẫu.

#### g. Hành vi đặc biệt
- Khi danh sách kết quả rỗng hoặc Ft_PageSize bằng 0, thuộc tính PageCount luôn trả về 0 theo đúng hành vi gate nguồn (gate dòng 62-70, 84-92).
- Các tham số DLCPCode, OrgID, NetworkID trong request bị bỏ qua tại tầng lọc điều kiện để khớp 1:1 với gate gốc.

---

### 2.2 Endpoint: POST /api/crd_dealserro/calc

#### a. Mô tả
Tính toán thử nghiệm nghiệp vụ báo giá dịch vụ (Repair Order). Thực hiện toàn bộ quy tắc kiểm tra ràng buộc và xử lý luồng tính toán nhưng không truyền danh sách chi tiết chi phí và voucher sang phía hạch toán chi tiết.

#### b. Tham số Request (Crd_DealSerROCalcReq)
Mã nguồn: Controllers/Port/Crd_DealSerROControllerPort.cs:45-103
Kế thừa cấu trúc từ RQ_Crd_DealSerRO:

| Thuộc tính | Kiểu dữ liệu | Bắt buộc | Mặc định | Ý nghĩa |
|---|---|---|---|---|
| Crd_DealSerRO | Crd_DealSerROInput? | Có | null | Đối tượng thông tin tổng quan báo giá dịch vụ |
| Lst_Crd_DealSerRODtl | List<Crd_DealSerRODtlInput>? | Không | null | Danh sách chi tiết chi phí dịch vụ (bị bỏ qua khi tính toán) |
| Lst_Crd_DealSerROVoucher | List<Crd_DealSerROVoucherInput>? | Không | null | Danh sách voucher áp dụng (bị bỏ qua khi tính toán) |

Chi tiết thuộc tính bên trong Crd_DealSerROInput:
- DealSerRONo (string, bắt buộc): Số hiệu báo giá dịch vụ.
- DLCode (string): Mã đại lý thực hiện dịch vụ.
- CardNo (string): Số thẻ thành viên.
- MemberNo (string): Mã hội viên.
- DealPointType (string): Loại giao dịch (mặc định SERVICE nếu bỏ trống).
- CardType (string): Hạng thẻ.
- CardTypeExpect (string): Hạng thẻ dự kiến.
- PolicyCode (string): Mã chính sách tích điểm dịch vụ.
- ParamCode (string): Mã tham số quy đổi (mặc định UNITPOINTTOMONEY).
- TotalAmountRO (double?): Tổng tiền báo giá sửa chữa.
- AmountRO (double?): Tiền báo giá tính điểm.
- AmountUse (double?): Tiền sử dụng tích lũy của khách hàng.
- PointTotal (double?): Tổng điểm thưởng.
- PolicyPointUnit (double?): Điểm đơn vị theo chính sách.
- PolicyAmountUnit (double?): Tiền đơn vị theo chính sách.
- ParamPointUnit (double?): Điểm đơn vị theo tham số.
- ParamAmountUnit (double?): Tiền đơn vị theo tham số.
- Remark (string?): Ghi chú giao dịch.
- FlagOnlyPoint (string?): Cờ chỉ tích điểm ("1" hoặc "0").
- PlateNo (string?): Biển số xe mang vào làm dịch vụ.
- VIN (string?): Số khung của xe.
- PointTotalRank (double?): Điểm tích xét hạng.
- PointConsumptionPrm (double?): Điểm thưởng tiêu dùng khuyến mại.
- PointVoucher (double?): Điểm voucher theo hạng sử dụng.
- PointActualVoucher (double?): Điểm voucher thực tế.
- TotalDiscount (double?): Tổng tiền chiết khấu.
- TotalAmountForDC (double?): Tiền dùng tính chiết khấu.

#### c. Quy tắc xử lý
Theo Controllers/Port/Crd_DealSerROControllerPort.cs:156-324:
1. Đọc mã người thực hiện từ HTTP Header WAUserCode (mặc định "SYS").
2. Kiểm tra dữ liệu đầu vào Crd_DealSerRO và DealSerRONo: Nếu null hoặc rỗng trả lỗi Crd_DealSerRO_Calc với tham số Check.strJson.
3. Kiểm tra mã báo giá trùng lặp: Tìm trong bảng Crd_DealSerRO theo DealSerRONo. Nếu đã tồn tại trả lỗi Crd_DealSerRO_CheckDB_Crd_DealSerROExist.
4. Kiểm tra đại lý: Tìm đại lý trong Mst_DealerCP theo DLCode. Nếu không tìm thấy trả lỗi Mst_DealerCP_CheckDB_DLNotFound; nếu cờ FlagActive != "1" trả lỗi Mst_DealerCP_CheckDB_FlagActiveNotMatched.
5. Kiểm tra hội viên: Tìm trong CrdMembers theo MemberNo. Nếu không tìm thấy trả lỗi Crd_Member_CheckDB_MemberNotFound; nếu RegisStatus != "FINISH" trả lỗi Crd_Member_CheckDB_RegisStatusNotMatched; nếu MemberStatus != "APPROVE" trả lỗi Crd_Member_CheckDB_MemberStatusNotMatched.
6. So khớp thông tin xe: So sánh PlateNo và VIN giữa request với CarNo và VIN của hội viên. Nếu không trùng khớp trả lỗi Crd_DealSerRO_Add_PlateNoOrVINNotMatchMember.
7. Kiểm tra thẻ: Tìm trong CrdCards theo CardNo. Nếu không tìm thấy trả lỗi Crd_Card_CheckDB_CardNotFound; nếu CardStatus != "APPROVE" trả lỗi Crd_Card_CheckDB_CardStatusNotMatched.
8. Kiểm tra số tiền sử dụng: Nếu AmountUse > 0 và AmountUse > Member.AmountEnd trả lỗi Crd_DealSerRO_Add_Input_InvalidAmountUse.
9. Tra cứu chính sách Mst_PolicyMoneyToPointService và chi tiết Mst_PolicyMoneyToPointServiceDtl theo hạng thẻ của khách và thời điểm hiện tại còn hiệu lực (EffDateStart <= now <= EffDateEnd).
10. Tại action Calc, danh sách chi tiết dtlList và voucherList được gán null (khớp với gate dòng 159).
11. Mở Database Transaction, cập nhật thông tin hội viên, thêm mới bản ghi Crd_DealSerRO với trạng thái PENDING.
12. Tra cứu tham số quy đổi UNITPOINTTOMONEY. Nếu có, tạo các giao dịch thẻ tương ứng (POINTUSE, POINTINCREASE, CONSUMPTION, SERVICETURN, CONSUMPTIONPRM) và kích hoạt xét duyệt nâng hạng thẻ qua PerformRankPolicyCheckAsync.
13. Commit Transaction và trả về cấu trúc: { Success = true, Data = { Crd_DealSerRO = null } }.

#### d. Bảng mã lỗi
Mã nguồn: Controllers/Port/Crd_DealSerROControllerPort.cs:176-321

| Mã lỗi | Điều kiện kích hoạt | Tham số trả về |
|---|---|---|
| Crd_DealSerRO_Calc | req.Crd_DealSerRO null hoặc DealSerRONo rỗng | Check.strJson |
| Crd_DealSerRO_CheckDB_Crd_DealSerROExist | DealSerRONo đã tồn tại trong bảng Crd_DealSerRO | Check.DealSerRONo |
| Mst_DealerCP_CheckDB_DLNotFound | Không tìm thấy DLCode trong Mst_DealerCP | Check.DLCPCode |
| Mst_DealerCP_CheckDB_FlagActiveNotMatched | Đại lý có FlagActive != "1" | Check.DLCPCode, Check.strFlagActiveListToCheck, DB.FlagActive |
| Crd_Member_CheckDB_MemberNotFound | Không tìm thấy MemberNo trong CrdMembers | Check.MemberNo |
| Crd_Member_CheckDB_RegisStatusNotMatched | Hội viên có RegisStatus != "FINISH" | Check.MemberNo, Check.strRegisStatusListToCheck, DB.RegisStatus |
| Crd_Member_CheckDB_MemberStatusNotMatched | Hội viên có MemberStatus != "APPROVE" | Check.MemberNo, Check.strMemberStatusListToCheck, DB.MemberStatus |
| Crd_DealSerRO_Add_PlateNoOrVINNotMatchMember | PlateNo hoặc VIN không khớp với CarNo hoặc VIN của hội viên | Check.MemberNo, Check.PlateNo_Input, Check.VIN_Input, Check.PlateNo_Member, Check.VIN_Member |
| Crd_Card_CheckDB_CardNotFound | Không tìm thấy CardNo trong CrdCards | Check.CardNo |
| Crd_Card_CheckDB_CardStatusNotMatched | Thẻ có CardStatus != "APPROVE" | Check.CardNo, Check.strFlagActiveListToCheck, DB.CardStatus |
| Crd_DealSerRO_Add_Input_InvalidAmountUse | AmountUse > 0 và lớn hơn số tiền tích lũy của hội viên (AmountEnd) | Check.AmountUse, Check.AmountEnd |
| Crd_CardTransaction_Perform_QtyChangeOverThreshold | Biến động số lượng hoặc điểm vượt ngưỡng giới hạn quy định | pte.Params |
| Crd_DealSerRO_Calc | Bắt ngoại lệ phát sinh không mong muốn trong quá trình thực thi | Exception |

#### e. Ví dụ Request
    {
        "Crd_DealSerRO": {
            "DealSerRONo": "RO_CALC_001",
            "DLCode": "DL01",
            "MemberNo": "MB01",
            "CardNo": "CARD01",
            "DealPointType": "SERVICE",
            "CardType": "STANDARD",
            "TotalAmountRO": 500000,
            "AmountRO": 500000,
            "AmountUse": 0,
            "PlateNo": "29A-12345",
            "VIN": "VN1234567890"
        }
    }

#### f. Ví dụ Response
Chưa có lần chạy mẫu.

#### g. Hành vi đặc biệt
- Tại action Calc, Lst_Crd_DealSerRODtl và Lst_Crd_DealSerROVoucher bị bỏ qua hoàn toàn và gán null (khớp hành vi gate dòng 159).
- Đối tượng trả về khi thành công là { Success = true, Data = { Crd_DealSerRO = null } } khớp với đặc tính nguồn.

---

### 2.3 Endpoint: POST /api/crd_dealserro/add

#### a. Mô tả
Thêm mới báo giá dịch vụ sửa chữa (Repair Order), ghi nhận chi tiết khoản chi phí, đối soát voucher, hạch toán giao dịch thẻ và tự động xét duyệt thăng hạng cho thẻ hội viên.

#### b. Tham số Request (RQ_Crd_DealSerRO)
Mã nguồn: Controllers/Port/Crd_DealSerROControllerPort.cs:36-103

| Thuộc tính | Kiểu dữ liệu | Bắt buộc | Mặc định | Ý nghĩa |
|---|---|---|---|---|
| Crd_DealSerRO | Crd_DealSerROInput? | Có | null | Đối tượng thông tin báo giá dịch vụ (xem chi tiết ở mục 2.2.b) |
| Lst_Crd_DealSerRODtl | List<Crd_DealSerRODtlInput>? | Không | null | Danh sách chi tiết các khoản mục chi phí dịch vụ |
| Lst_Crd_DealSerROVoucher | List<Crd_DealSerROVoucherInput>? | Không | null | Danh sách voucher áp dụng trong báo giá |

Chi tiết thuộc tính bên trong Crd_DealSerRODtlInput:
- ExpenseType (string, bắt buộc nếu có chi tiết): Loại chi phí dịch vụ (tra cứu trong Mst_ExpenseType).
- PolicyExpenseTypeNo (string?): Số chính sách chi phí.
- Amount (double?): Số tiền chi phí.
- FlagPoint (string?): Cờ áp dụng tính điểm ("1" hoặc "0").
- AmountRate (double?): Tỷ lệ tiền áp dụng.
- FlagDiscount (double?): Cờ áp dụng chiết khấu.
- DiscountRate (double?): Tỷ lệ chiết khấu.
- AmountForDC (double?): Số tiền dùng tính chiết khấu.
- RefCode00 đến RefCode05 (string?): Các trường tham chiếu mở rộng.

Chi tiết thuộc tính bên trong Crd_DealSerROVoucherInput:
- VoucherCode (string?): Mã voucher.
- PointVoucher (double?): Điểm voucher áp dụng theo hạng sử dụng.
- PointActualVoucher (double?): Điểm voucher áp dụng theo hạng thực tế.

#### c. Quy tắc xử lý
Theo Controllers/Port/Crd_DealSerROControllerPort.cs:156-485:
1. Đọc mã người dùng từ header WAUserCode (mặc định "SYS").
2. Thực hiện toàn bộ các bước kiểm tra hợp lệ tương tự mục 2.2.c:
   - Kiểm tra Crd_DealSerRO và DealSerRONo không null/rỗng (actionName là "Crd_DealSerRO_Add").
   - Kiểm tra DealSerRONo không trùng lặp trong bảng Crd_DealSerRO.
   - Kiểm tra đại lý tồn tại và FlagActive == "1".
   - Kiểm tra hội viên tồn tại, RegisStatus == "FINISH", MemberStatus == "APPROVE".
   - So khớp biển số xe (PlateNo) và số khung (VIN) của input với thông tin hội viên (CarNo, VIN).
   - Kiểm tra thẻ tồn tại và CardStatus == "APPROVE".
   - Kiểm tra AmountUse không vượt quá số tiền tích lũy Member.AmountEnd.
3. Kiểm tra danh sách chi tiết Lst_Crd_DealSerRODtl:
   - Duyệt từng dòng chi tiết, tìm kiếm ExpenseType trong bảng Mst_ExpenseType.
   - Nếu không tìm thấy trả lỗi Mst_ExpenseType_CheckDB_ExpenseTypeNotFound.
   - Nếu tồn tại nhưng FlagActive != "1" trả lỗi Mst_ExpenseType_CheckDB_FlagActiveNotMatched.
4. Kiểm tra danh sách voucher Lst_Crd_DealSerROVoucher:
   - Tính tổng PointVoucher và tổng PointActualVoucher từ danh sách voucher.
   - So sánh với giá trị PointVoucher và PointActualVoucher ở header Crd_DealSerRO. Nếu sai lệch trả lỗi Crd_DealSerRO_Add_InvalidPointVoucher.
5. Mở Transaction cơ sở dữ liệu:
   - Khóa hội viên và cập nhật LogLUDTimeUTC, LogLUBy.
   - Tra cứu bảng Mst_PolicyExpenseType tương ứng các ExpenseType được chỉ định.
   - Lưu bản ghi chính vào Crd_DealSerRO với DealSerROStatus = "PENDING".
   - Lưu các bản ghi chi tiết vào Crd_DealSerRODtl với RefType = "CRD_DEALSERRO" và DealSerROStatusDtl = "PENDING".
6. Tính toán và tạo giao dịch thẻ qua PortCardTransactionEngine:
   - Tra cứu tham số hệ thống UNITPOINTTOMONEY. Nếu có, tiến hành sinh danh sách CrdCardTransaction.
   - Giao dịch POINTUSE: Giảm tiền AmountUse của hội viên (AmountChTotal âm).
   - Duyệt chi tiết tính toán điểm xét hạng (PointRankTotal) và điểm tiêu dùng (PointChTotal):
     + PointRank: Tính theo tỷ lệ ConvertPoint/ConvertValue từ chính sách.
     + Nếu FlagPointRank == "1": Tính tổng tiền và điểm xét hạng.
     + Nếu FlagPoint == "1": Tính tổng tiền và điểm tiêu dùng.
     + Nếu FlagCountService == "1": Tính tổng tiền tích lũy lượt dịch vụ.
   - Giao dịch POINTINCREASE: Nếu tổng điểm xét hạng > 0, phát sinh giao dịch tăng điểm xét hạng.
   - Giao dịch CONSUMPTION: Nếu điểm tiêu dùng > 0, phát sinh giao dịch tăng điểm tiêu dùng kèm hạn sử dụng (cuối tháng của năm kế tiếp).
   - Giao dịch SERVICETURN: Nếu tổng tiền dịch vụ đạt ngưỡng quy định trong chính sách (ValueRankCardType), ghi nhận tăng 1 lượt dịch vụ.
   - Giao dịch CONSUMPTIONPRM: Nếu có điểm khuyến mại PointConsumptionPrm > 0 và có điểm tiêu dùng, phát sinh giao dịch điểm khuyến mại với DLCode là "HTV".
   - Thực thi hạch toán danh sách giao dịch qua PortCardTransactionEngine.Crd_CardTransaction_PerformAsync.
7. Xét duyệt nâng hạng thẻ qua hàm PerformRankPolicyCheckAsync:
   - Tra cứu chính sách Mst_RankPolicy của hạng thẻ hiện tại.
   - Nếu QtyVisitAvail >= QtyVisitUpBegin và PointCardRank >= PointUpBegin: tìm hạng thẻ kế tiếp có Value lớn hơn.
   - Cập nhật CardTypeUse của thẻ và hội viên sang hạng mới, đặt lại PointCardRank về 0, ghi nhận lịch sử vào bảng Crd_CardRank với CardSourceCode = "UP".
8. Hoàn tất giao dịch cơ sở dữ liệu (SaveChangesAsync và CommitAsync).
9. Trả kết quả thành công: { Success = true, Data = null }.

#### d. Bảng mã lỗi
Mã nguồn: Controllers/Port/Crd_DealSerROControllerPort.cs:176-321

| Mã lỗi | Điều kiện kích hoạt | Tham số trả về |
|---|---|---|
| Crd_DealSerRO_Add | req.Crd_DealSerRO null hoặc DealSerRONo rỗng | Check.strJson |
| Crd_DealSerRO_CheckDB_Crd_DealSerROExist | DealSerRONo đã tồn tại trong cơ sở dữ liệu | Check.DealSerRONo |
| Mst_DealerCP_CheckDB_DLNotFound | DLCode không tìm thấy trong Mst_DealerCP | Check.DLCPCode |
| Mst_DealerCP_CheckDB_FlagActiveNotMatched | Đại lý có FlagActive != "1" | Check.DLCPCode, Check.strFlagActiveListToCheck, DB.FlagActive |
| Crd_Member_CheckDB_MemberNotFound | MemberNo không tìm thấy trong CrdMembers | Check.MemberNo |
| Crd_Member_CheckDB_RegisStatusNotMatched | Hội viên có RegisStatus != "FINISH" | Check.MemberNo, Check.strRegisStatusListToCheck, DB.RegisStatus |
| Crd_Member_CheckDB_MemberStatusNotMatched | Hội viên có MemberStatus != "APPROVE" | Check.MemberNo, Check.strMemberStatusListToCheck, DB.MemberStatus |
| Crd_DealSerRO_Add_PlateNoOrVINNotMatchMember | Biển số xe hoặc VIN không khớp thông tin hội viên | Check.MemberNo, Check.PlateNo_Input, Check.VIN_Input, Check.PlateNo_Member, Check.VIN_Member |
| Crd_Card_CheckDB_CardNotFound | CardNo không tìm thấy trong CrdCards | Check.CardNo |
| Crd_Card_CheckDB_CardStatusNotMatched | Thẻ có CardStatus != "APPROVE" | Check.CardNo, Check.strFlagActiveListToCheck, DB.CardStatus |
| Crd_DealSerRO_Add_Input_InvalidAmountUse | AmountUse > 0 và lớn hơn số tiền tích lũy của hội viên | Check.AmountUse, Check.AmountEnd |
| Mst_ExpenseType_CheckDB_ExpenseTypeNotFound | ExpenseType trong chi tiết không tồn tại trong Mst_ExpenseType | Check.ExpenseType |
| Mst_ExpenseType_CheckDB_FlagActiveNotMatched | Khoản chi phí ExpenseType có FlagActive != "1" | Check.ExpenseType, Check.strFlagActiveListToCheck, DB.FlagActive |
| Crd_DealSerRO_Add_InvalidPointVoucher | Tổng điểm voucher trong danh sách không khớp header | Check.PointVoucher, Check.SumPointVoucher, Check.PointActualVoucher, Check.SumPointActualVoucher |
| Crd_CardTransaction_Perform_QtyChangeOverThreshold | Biến động số lượng hoặc điểm vượt hạn mức cho phép | pte.Params |
| Crd_DealSerRO_Add | Lỗi ngoại lệ hệ thống trong quá trình thực thi | Exception |

#### e. Ví dụ Request
    {
        "Crd_DealSerRO": {
            "DealSerRONo": "RO20261018001",
            "DLCode": "DL01",
            "MemberNo": "MB01",
            "CardNo": "CARD01",
            "DealPointType": "SERVICE",
            "CardType": "STANDARD",
            "TotalAmountRO": 1500000,
            "AmountRO": 1500000,
            "AmountUse": 100000,
            "PlateNo": "29A-12345",
            "VIN": "VN1234567890",
            "PointVoucher": 0,
            "PointActualVoucher": 0
        },
        "Lst_Crd_DealSerRODtl": [
            {
                "ExpenseType": "MAINTENANCE",
                "Amount": 1500000,
                "FlagPoint": "1",
                "AmountRate": 1.0
            }
        ],
        "Lst_Crd_DealSerROVoucher": []
    }

#### f. Ví dụ Response
Chưa có lần chạy mẫu.

#### g. Hành vi đặc biệt
- Toàn bộ các thao tác tạo báo giá, lưu dòng chi tiết, hạch toán giao dịch thẻ và nâng hạng thẻ được thực thi nguyên tử trong một Transaction (nếu có lỗi sẽ Rollback hoàn toàn).
- Khác với action Calc, action Add trả về Data là null khi thành công ({ Success = true, Data = null }).
- Nếu thiếu tham số hệ thống UNITPOINTTOMONEY, hệ thống chỉ lưu báo giá mà không sinh các giao dịch thẻ tương ứng (khớp 1:1 hành vi INNER JOIN trong SQL nguồn).

---

## 3. Khác biệt so với nguồn & GAP

Theo nhật ký triển khai và đối chiếu mã nguồn:
1. Bỏ qua phân quyền Gate: Mã nguồn gốc kiểm tra phân quyền truy cập và vai trò người dùng tại tầng Gateway; phiên bản port tập trung vào logic nghiệp vụ và lấy người dùng qua Header WAUserCode (mặc định "SYS").
2. Lọc tham số tìm kiếm tại Gate: Các tham số DLCPCode, OrgID, NetworkID được định nghĩa trong request DTO nhưng logic nguồn (strWhereClause_Crd_DealSerRO) không đưa vào mệnh đề WHERE; bản port giữ nguyên hành vi này để đảm bảo tính tương thích 1:1.
3. Thuộc tính RankPolicyCode: Trong model nguồn Crd_DealSerRO.cs:18 thuộc tính này bị chú thích (comment out), do đó bản port không cấu hình trường này trên bảng cơ sở dữ liệu.
4. Cơ chế tính toán trong action Calc: Tại action Calc, danh sách chi tiết (Lst_Crd_DealSerRODtl) và danh sách voucher (Lst_Crd_DealSerROVoucher) bị gán null theo đúng dòng mã chú thích tại Gate nguồn (gate dòng 159).

---

## 4. Nhật ký thay đổi

- Hạng mục #019: Port model Crd_DealSerRO và action Search tìm kiếm báo giá dịch vụ (commit d30cdab).
- Hạng mục #020: Port action Calc tính toán thử nghiệm báo giá dịch vụ (commit d30cdab).
- Hạng mục #021: Port action Add thêm mới báo giá dịch vụ, hạch toán giao dịch thẻ và kiểm tra chính sách nâng hạng (commit d30cdab).

---

TOM TAT
FILE_DA_TAO_SUA: docs/api/Crd_DealSerROController.md
PHAM_VI_PHU: Crd_DealSerROControllerPort (POST /api/crd_dealserro/search, POST /api/crd_dealserro/calc, POST /api/crd_dealserro/add; 3 endpoints)
KHONG_XAC_MINH: khong
LENH_DA_CHAY: Chua co lan chay mau (moi truong WEB chi doc ma)
VI_PHAM_QUY_TAC: khong
