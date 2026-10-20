# Tài liệu API Crd_CardTransactionController

Phạm vi: Crd_CardTransactionController (Search, SearchDetailCrdDealUsePromotion, Export)
Cập nhật: 2026-10-18
Phiên bản mã: d30cdab
Trạng thái: Nháp — chờ review

---

## 1. Tóm tắt

- Mục đích nghiệp vụ: Quản lý tra cứu lịch sử giao dịch thẻ hội viên (CrdCardTransaction) bao gồm tích điểm, tiêu điểm, sử dụng ưu đãi chương trình (PRPROGRAM) và chiết khấu dịch vụ (DISCOUNTRO); hỗ trợ tra cứu chi tiết giao dịch ưu đãi và xuất báo cáo danh sách giao dịch ra file Excel.
- Bảng nguồn:
  * Nguồn Gate:
    - Crd_CardTransactionController.cs:32-113 (Search)
    - Crd_CardTransactionController.cs:114-187 (SearchDetailCrdDealUsePromotion)
    - Crd_CardTransactionController.cs:191-308 (strWhereClause_Crd_CardTransaction)
    - Crd_CardTransactionController.cs:312-395 (Export)
    - Crd_CardTransactionController.cs:399-418 (GetImportDicColums)
  * Nguồn API:
    - CrdCardTransactionController.cs:24-91 (WA_Crd_CardTransaction_Get)
  * Nguồn Biz:
    - Master.Loyalty.cs:21144-21253 (WAS_Crd_CardTransaction_Get)
    - Master.Loyalty.cs:21254-21408 (Crd_CardTransaction_Get)
    - Master.Loyalty.cs:21409-21817 (Crd_CardTransaction_GetX — bản LIVE)
- Bảng cơ sở dữ liệu:
  * Bảng chính: Crd_CardTransaction (entity CrdCardTransaction tại Models/PortLoyalty.cs, DDL 20221108.z11.Crd_Member.Crd_Card.sql:215-280)
  * Bảng kết hợp: CrdMember (Crd_Member), CrdCard (Crd_Card), Mst_PromotionProgram, Mst_PromotionProgramDtl, CrdMemberDiscountTransaction (Crd_MemberDiscountTransaction)

### Cấu trúc dữ liệu kết quả (Crd_CardTransactionSearchResult - 66 thuộc tính)

| Tên trường | Kiểu | Ghi chú |
|---|---|---|
| AutoId | long | Khóa chính tự tăng |
| RefNo | string? | Mã giao dịch tham chiếu / số RO |
| ProjectCode | string? | Mã dự án |
| NetworkID | string? | Mã mạng |
| OrgID | string? | Mã tổ chức |
| DLCode | string? | Mã đại lý thực hiện giao dịch |
| MemberNo | string? | Mã hội viên |
| CardNo | string? | Mã thẻ hội viên |
| CardSourceCode | string? | Mã nguồn phát hành thẻ |
| CardTypeUse | string? | Hạng thẻ sử dụng |
| CardTypeInit | string? | Hạng thẻ thực tế ban đầu |
| DealPointType | string? | Loại giao dịch điểm (CONSUMPTION, POINTINCREASE, PRPROGRAM, DISCOUNTRO...) |
| PrProgramCode | string? | Mã chương trình ưu đãi |
| EffDateStart | DateTime? | Ngày hiệu lực bắt đầu |
| EffDateEnd | DateTime? | Ngày hiệu lực kết thúc |
| UsePrmDTime | DateTime? | Thời gian sử dụng ưu đãi |
| PointChTotal | double? | Tổng điểm giao dịch |
| PointChBlock | double? | Điểm đóng băng / giữ |
| AmountChTotal | double? | Tổng tiền giao dịch |
| AmountChBlock | double? | Tiền đóng băng |
| QtyVisitChTotal | double? | Tổng lượt vào xưởng/giao dịch |
| QtyVisitChBlock | double? | Lượt đóng băng |
| QtyPrChTotal | double? | Tổng số lượng ưu đãi áp dụng |
| QtyPrChBlock | double? | Số lượng ưu đãi đóng băng |
| QtyPrUsed | double? | Số lượng ưu đãi đã dùng |
| PointRef | double? | Điểm tham chiếu |
| AmountRef | double? | Tiền tham chiếu |
| PolicyPointUnit | double? | Đơn vị điểm chính sách |
| PolicyAmountUnit | double? | Đơn vị tiền chính sách |
| PolicyCode | string? | Mã chính sách |
| ParamPointUnit | double? | Tham số đơn vị điểm |
| ParamAmountUnit | double? | Tham số đơn vị tiền |
| ParamCode | string? | Mã tham số |
| FlagViewUI | string? | Cờ hiển thị giao diện |
| FlagPoint | string? | Cờ tính điểm |
| FlagAmount | string? | Cờ tiền giao dịch |
| FlagPr | string? | Cờ ưu đãi |
| FunctionName | string? | Tên hàm xử lý |
| FunctionActionType | string? | Loại hành động |
| FunctionRemark | string? | Ghi chú hàm |
| RefType | string? | Loại tham chiếu (RO, PR...) |
| PointChRankTotal | double? | Điểm xét hạng tích lũy |
| RefCode00 - RefCode09 | string? | Các mã trường tham chiếu mở rộng |
| CreateDTimeUTC | string? | Thời điểm thực hiện (chuyển sang giờ Việt Nam UTC+7, định dạng yyyy-MM-dd) |
| CreateBy | string? | Người tạo |
| LUDTimeUTC | DateTime? | Thời điểm cập nhật cuối (UTC) |
| LUBy | string? | Người cập nhật cuối |
| LogLUDTimeUTC | DateTime? | Thời điểm ghi log cập nhật cuối (UTC) |
| LogLUBy | string? | Người ghi log |
| PointExpiryDTime | string? | Thời điểm hết hạn điểm (định dạng yyyy-MM-dd) |
| AmountChTotalInit | double? | Tiền giao dịch ban đầu |
| AmountChTotalDiffer | double? | Tiền giao dịch chênh lệch |
| AmountAvailPrevious | double? | Số tiền khả dụng trước đó |
| PointExpiryDTimeSync | DateTime? | Ngày hết hạn điểm đồng bộ |
| PointChTotalInit | double? | Điểm giao dịch ban đầu |
| PointChTotalDiffer | double? | Điểm giao dịch chênh lệch |
| PrProgramName | string? | Tên chương trình ưu đãi (lấy từ Mst_PromotionProgram) |
| Remark | string? | Mô tả chương trình (lấy từ Mst_PromotionProgram) |
| Unit | string? | Đơn vị tính ưu đãi (lấy từ Mst_PromotionProgramDtl) |
| CarNo | string? | Biển số xe (lấy từ CrdMember) |
| PhoneNo | string? | Số điện thoại (lấy từ CrdMember) |
| MemberName | string? | Tên hội viên (lấy từ CrdMember) |
| DLCodeExceptionally | string? | Mã đại lý ngoại lệ (lấy từ CrdCard hoặc CrdMemberDiscountTransaction) |
| DiscountRate | double | Tỷ lệ chiết khấu (%) |
| DiscountAmount | double | Số tiền / điểm chiết khấu |
| DiscountRevenue | double | Doanh thu làm căn cứ chiết khấu |

---

## 2. Các Endpoint

### 2.1. Endpoint: POST /api/crd_cardtransaction/search

- Route: POST /api/crd_cardtransaction/search (Controllers/Port/Crd_CardTransactionControllerPort.cs:170)
- HTTP method: POST

#### Bảng tham số request (Crd_CardTransactionSearchReq)

| Tên tham số | Kiểu | Bắt buộc? | Mặc định | Ý nghĩa |
|---|---|---|---|---|
| CreateDTimeUTCFrom | string? | Không | null | Thời gian thực hiện từ (chuyển đổi sang UTC - 7h) |
| CreateDTimeUTCTo | string? | Không | null | Thời gian thực hiện đến (cuối ngày - 7h chuyển đổi sang UTC) |
| EffDateStartFrom | string? | Không | null | Ngày bắt đầu hiệu lực từ |
| EffDateStartTo | string? | Không | null | Ngày bắt đầu hiệu lực đến |
| CardNo | string? | Không | null | Số thẻ hội viên (so khớp chính xác) |
| IDCardNo | string? | Không | null | Số CMT/CCCD của hội viên (so khớp chính xác trên CrdMember) |
| MemberNo | string? | Không | null | Mã hội viên (so khớp chính xác) |
| PointRefFrom | string? | Không | null | Điểm tham chiếu từ |
| PointRefTo | string? | Không | null | Điểm tham chiếu đến |
| DealPointType | string? | Không | null | Loại giao dịch (chuỗi phân cách dấu phẩy, vd: CONSUMPTION,DISCOUNTRO) |
| DLCode | string? | Không | null | Mã đại lý thực hiện giao dịch |
| PointChRankTotalFrom | string? | Không | null | Điểm xét hạng từ |
| PointChRankTotalTo | string? | Không | null | Điểm xét hạng đến |
| AmountChTotalFrom | string? | Không | null | Tiền giao dịch từ |
| AmountChTotalTo | string? | Không | null | Tiền giao dịch đến |
| PrProgramCode | string? | Không | null | Mã chương trình ưu đãi (so khớp chính xác) |
| FlagPoint | string? | Không | null | Cờ điểm |
| PhoneNo | string? | Không | null | Số điện thoại hội viên (so khớp chính xác trên CrdMember) |
| CardType | string? | Không | null | Hạng thẻ (tự động ánh xạ sang CardTypeUse nếu CardTypeUse rỗng) |
| CardTypeUse | string? | Không | null | Hạng thẻ sử dụng (so khớp chính xác) |
| UsePrmDTimeFrom | string? | Không | null | Ngày sử dụng ưu đãi từ |
| UsePrmDTimeTo | string? | Không | null | Ngày sử dụng ưu đãi đến |
| OrgID | string? | Không | null | Mã tổ chức |
| NetworkID | string? | Không | null | Mã mạng |
| Ft_PageIndex | int | Không | 0 | Chỉ số trang (bắt đầu từ 0) |
| Ft_PageSize | int | Không | 10 | Số lượng bản ghi trên một trang |
| Ft_RecordStart | string? | Không | null | Tham số phụ vị trí bắt đầu bản ghi |
| Ft_RecordCount | string? | Không | null | Tham số phụ số lượng bản ghi |

#### Quy tắc xử lý theo thứ tự

1. Ánh xạ tham số: Nếu có truyền CardType và CardTypeUse đang rỗng, gán CardTypeUse = CardType (Controllers/Port/Crd_CardTransactionControllerPort.cs:173-176).
2. Xây dựng truy vấn cơ sở:
   - Join bảng CrdCardTransaction với CrdMember (inner join trên MemberNo).
   - Left join với CrdCard trên { MemberNo, CardNo }.
   - Left join với Mst_PromotionProgram trên PrProgramCode.
   - Left join với Mst_PromotionProgramDtl trên { PrProgramCode, CardType = CardTypeUse }.
3. Lọc theo các tiêu chí từ strWhereClause (Controllers/Port/Crd_CardTransactionControllerPort.cs:282-402):
   - PrProgramCode, CardNo, DLCode, CardTypeUse, MemberNo, OrgID, NetworkID: So khớp chính xác không phân biệt hoa thường bằng PortQuery.Equal.
   - IDCardNo, PhoneNo: So khớp chính xác trên bảng liên kết CrdMember.
   - EffDateStartFrom/To: So sánh >= / <= trên cột EffDateStart.
   - PointRefFrom/To: So sánh >= / <= trên cột PointRef (mặc định 0 nếu null).
   - AmountChTotalFrom/To: So sánh >= / <= trên cột AmountChTotal (mặc định 0 nếu null).
   - PointChRankTotalFrom/To: So sánh >= / <= trên cột PointChRankTotal (mặc định 0 nếu null).
   - UsePrmDTimeFrom/To: So sánh >= / <= trên cột UsePrmDTime.
   - CreateDTimeUTCFrom/To: Phân tích cú pháp ngày, chuyển sang giờ UTC bằng cách trừ 7 tiếng so với đầu ngày (CreateDTimeUTCFrom) hoặc cuối ngày (CreateDTimeUTCTo 23:59:59).
   - DealPointType: Tách mảng theo dấu phẩy, lọc IN (chuyển về chữ thường). Đồng thời kiểm tra nếu có chứa loại DISCOUNTRO thì đánh dấu cờ isDiscountSearch = true.
4. Xử lý phân nhánh theo DealPointType:
   - Nếu không chứa DISCOUNTRO (isDiscountSearch == false):
     * Sắp xếp giảm dần theo LogLUDTimeUTC.
     * Đếm tổng số lượng bản ghi khớp (itemCount).
     * Áp dụng Skip/Take theo pageIndex và pageSize.
     * Ánh xạ kết quả sang Crd_CardTransactionSearchResult qua MapToResult (CreateDTimeUTC đổi sang giờ VN yyyy-MM-dd, gán các trường chiết khấu bằng 0).
     * Đóng gói vào PageInfo<Crd_CardTransactionSearchResult> và trả về HTTP 200.
   - Nếu có chứa DISCOUNTRO (isDiscountSearch == true):
     * Thực hiện thêm truy vấn trên bảng CrdMemberDiscountTransaction join CrdMember với các điều kiện lọc tương ứng (CardNo, MemberNo, DLCode, CardTypeUse, IDCardNo, PhoneNo, EffDateStartFrom/To, CreateDTimeUTCFrom/To, OrgID, NetworkID).
     * Ánh xạ danh sách chiết khấu sang Crd_CardTransactionSearchResult qua MapDiscountToResult (gán DealPointType = DISCOUNTRO, DiscountRate = PolicyDiscountRate, DiscountAmount = PointChTotal, DiscountRevenue = AmountForDC).
     * Hợp nhất (Union/Concat) hai danh sách và sắp xếp giảm dần theo LogLUDTimeUTC.
     * Thực hiện phân trang trong bộ nhớ theo pageIndex và pageSize, đóng gói vào PageInfo và trả về HTTP 200.

#### Bảng mã lỗi

| Mã lỗi | Điều kiện |
|---|---|
| Không có | Controller không khai báo lỗi ErridnLoyalty; trả về PageInfo trong Data |

---

### 2.2. Endpoint: POST /api/crd_cardtransaction/searchdetailcrddealusepromotion

- Route: POST /api/crd_cardtransaction/searchdetailcrddealusepromotion (Controllers/Port/Crd_CardTransactionControllerPort.cs:188)
- HTTP method: POST

#### Bảng tham số request (Crd_CardTransactionDetailPromotionReq)

| Tên tham số | Kiểu | Bắt buộc? | Mặc định | Ý nghĩa |
|---|---|---|---|---|
| CreateDTimeUTCFrom | string? | Không | null | Thời gian thực hiện từ |
| CreateDTimeUTCTo | string? | Không | null | Thời gian thực hiện đến |
| CardType | string? | Không | null | Hạng thẻ (ánh xạ sang CardTypeUse) |
| MemberNo | string? | Không | null | Mã hội viên |
| PhoneNo | string? | Không | null | Số điện thoại hội viên |
| PrProgramCode | string? | Không | null | Mã chương trình ưu đãi |
| DLCode | string? | Không | null | Mã đại lý thực hiện |
| UsePrmDTimeFrom | string? | Không | null | Ngày sử dụng ưu đãi từ |
| UsePrmDTimeTo | string? | Không | null | Ngày sử dụng ưu đãi đến |

#### Quy tắc xử lý theo thứ tự

1. Nhận DTO gồm 9 tham số, khởi tạo đối tượng mới nếu payload null (Controllers/Port/Crd_CardTransactionControllerPort.cs:190).
2. Chuẩn bị yêu cầu tìm kiếm lõi:
   - Ghi đè cố định DealPointType = "PRPROGRAM" (Controllers/Port/Crd_CardTransactionControllerPort.cs:200).
   - Thiết lập không phân trang: Ft_PageIndex = 0, Ft_PageSize = 123456000, Ft_RecordStart = "0", Ft_RecordCount = "123456000" (Controllers/Port/Crd_CardTransactionControllerPort.cs:204-207).
3. Gọi SearchCoreAsync để thực thi truy vấn tìm kiếm dữ liệu.
4. Trích xuất danh sách DataList từ kết quả PageInfo:
   - Nếu danh sách có dữ liệu: Trả về HTTP 200 với Success = true, Data = { DataList = list } (Controllers/Port/Crd_CardTransactionControllerPort.cs:215).
   - Nếu danh sách null hoặc rỗng: Trả về HTTP 200 với Success = true, Data = { DataList = null } (Controllers/Port/Crd_CardTransactionControllerPort.cs:215).

#### Bảng mã lỗi

| Mã lỗi | Điều kiện |
|---|---|
| Không có | Controller không khai báo mã lỗi; trả về DataList null khi không có kết quả |

---

### 2.3. Endpoint: POST /api/crd_cardtransaction/export

- Route: POST /api/crd_cardtransaction/export (Controllers/Port/Crd_CardTransactionControllerPort.cs:240)
- HTTP method: POST

#### Bảng tham số request (Crd_CardTransactionExportReq)

| Tên tham số | Kiểu | Bắt buộc? | Mặc định | Ý nghĩa |
|---|---|---|---|---|
| CreateDTimeUTCFrom | string? | Không | null | Thời gian thực hiện từ (nếu chưa có giờ, tự động ghép thêm " 00:00:00") |
| CreateDTimeUTCTo | string? | Không | null | Thời gian thực hiện đến (nếu chưa có giờ, tự động ghép thêm " 23:59:59") |
| CardType | string? | Không | null | Hạng thẻ (ưu tiên dùng thay cho CardTypeUse) |
| CardTypeUse | string? | Không | null | Hạng thẻ sử dụng |
| MemberNo | string? | Không | null | Mã thẻ hội viên |
| PhoneNo | string? | Không | null | Số điện thoại hội viên |
| PrProgramCode | string? | Không | null | Mã chương trình (nhận vào nhưng bỏ qua trong strWhereClause theo nguồn gate :333-342) |
| DLCode | string? | Không | null | Mã đại lý thực hiện |
| OrgID | string? | Không | null | Mã tổ chức |
| NetworkID | string? | Không | null | Mã mạng |

#### Quy tắc xử lý theo thứ tự

1. Xác định hạng thẻ sử dụng: Lấy CardType nếu có giá trị, ngược lại dùng CardTypeUse (Controllers/Port/Crd_CardTransactionControllerPort.cs:243).
2. Chuẩn hóa chuỗi thời gian: Bổ sung " 00:00:00" cho CreateDTimeUTCFrom và " 23:59:59" cho CreateDTimeUTCTo nếu chưa có thành phần giờ phút giây (Controllers/Port/Crd_CardTransactionControllerPort.cs:247-248).
3. Thiết lập truy vấn tìm kiếm dữ liệu:
   - Cố định DealPointType = "PRPROGRAM" (Controllers/Port/Crd_CardTransactionControllerPort.cs:250).
   - Thiết lập số lượng bản ghi tối đa 123456000 (không phân trang).
   - Lưu ý: Tham số PrProgramCode được nhận vào DTO nhưng không đưa vào điều kiện lọc, khớp với mã nguồn gate :333-342.
4. Thực thi truy vấn bằng SearchCoreAsync.
5. Kiểm tra dữ liệu: Nếu kết quả trả về null hoặc DataList rỗng (Count == 0), trả về HTTP 400 kèm mã lỗi ErridnLoyalty.Crd_CardTransaction_Export với thông điệp "Không có dữ liệu" (Controllers/Port/Crd_CardTransactionControllerPort.cs:272-275).
6. Khởi tạo đường dẫn và thư mục xuất file Excel thông qua PortExcel.GenExcelExportFilePath với tiền tố "Crd_CardTransaction" (Controllers/Port/Crd_CardTransactionControllerPort.cs:278-290).
7. Lấy danh sách 14 cột Excel xuất theo định nghĩa từ GetImportDicColums() (Controllers/Port/Crd_CardTransactionControllerPort.cs:286, :542-559):
   - RefNo: "Số giao dịch"
   - DLCode: "Đại lý thực hiện"
   - CreateDTimeUTC: "Ngày thực hiện"
   - MemberNo: "Mã thẻ hội viên"
   - MemberName: "Tên hội viên"
   - CardTypeUse: "Hạng sử dụng"
   - PhoneNo: "Số điện thoại"
   - CarNo: "Biển số xe"
   - CardNo: "Mã kỳ xét hạng"
   - PrProgramCode: "Mã chương trình"
   - PrProgramName: "Tên chương trình"
   - Remark: "Mô tả"
   - QtyPrChTotal: "Số lượng sử dụng"
   - Unit: "Đơn vị tính"
8. Xuất file bằng PortExcel.ExportToExcelFromList vào sheet tên "Crd_CardTransaction" (Controllers/Port/Crd_CardTransactionControllerPort.cs:293).
9. Trả về kết quả HTTP 200 dạng Ok({ Success = true, Data = { Url = baseUrl + virtualPath } }) (Controllers/Port/Crd_CardTransactionControllerPort.cs:302-309). Nếu phát sinh ngoại lệ, trả về lỗi ErridnLoyalty.Crd_CardTransaction_Export kèm Exception message.

#### Bảng mã lỗi

| Mã lỗi | Điều kiện |
|---|---|
| Crd_CardTransaction_Export | Không có dòng dữ liệu nào thỏa mãn điều kiện lọc (Message: "Không có dữ liệu") hoặc xảy ra lỗi trong quá trình ghi file Excel |

---

## 3. Ví dụ Request / Response

### 3.1. Ví dụ Request suy ra từ mã

Ví dụ cho Search:

    {
        "MemberNo": "MEM01",
        "DealPointType": "CONSUMPTION",
        "CreateDTimeUTCFrom": "2026-10-01",
        "CreateDTimeUTCTo": "2026-10-31",
        "Ft_PageIndex": 0,
        "Ft_PageSize": 10
    }

Ví dụ cho SearchDetailCrdDealUsePromotion:

    {
        "MemberNo": "MEM01",
        "CreateDTimeUTCFrom": "2026-10-01",
        "CreateDTimeUTCTo": "2026-10-31",
        "PrProgramCode": "PR01"
    }

Ví dụ cho Export:

    {
        "MemberNo": "MEM01",
        "CreateDTimeUTCFrom": "2026-10-01",
        "CreateDTimeUTCTo": "2026-10-31"
    }

### 3.2. Ví dụ Response

Chưa có lần chạy mẫu

---

## 4. Hành vi đặc biệt

- Cố định DealPointType ở SearchDetailCrdDealUsePromotion và Export: Cả 2 action này tự động ghi đè loại giao dịch là "PRPROGRAM", không phụ thuộc vào giá trị client gửi lên (Controllers/Port/Crd_CardTransactionControllerPort.cs:200, :250).
- DataList trả về null: Tại endpoint SearchDetailCrdDealUsePromotion, khi không có dòng kết quả nào, trường Data.DataList trả về giá trị null thay vì trả mảng rỗng [] hay đối tượng PageInfo (Controllers/Port/Crd_CardTransactionControllerPort.cs:215).
- Lỗi xuất rỗng: Tại endpoint Export, nếu không có dữ liệu để xuất, hệ thống trả về mã lỗi Crd_CardTransaction_Export với Message = "Không có dữ liệu" thay vì sinh file Excel rỗng (Controllers/Port/Crd_CardTransactionControllerPort.cs:272-275).
- Ghép bảng chiết khấu khi có DISCOUNTRO: Action Search tự động phát hiện tham số DealPointType có chứa chuỗi DISCOUNTRO để thực hiện UNION danh sách giao dịch chiết khấu từ bảng CrdMemberDiscountTransaction và tự động tính toán các cột DiscountRate, DiscountAmount, DiscountRevenue (Controllers/Port/Crd_CardTransactionControllerPort.cs:440-505).
- Bỏ qua tham số PrProgramCode khi Export: Dù tham số PrProgramCode có mặt trong DTO của Export, controller không lọc tham số này trong strWhereClause nhằm bảo toàn tính tương thích với mã nguồn gốc tầng Gate :333-342.

---

## 5. Khác biệt so với nguồn & GAP

- Chuyển đổi ngày giờ UTC: Trường CreateDTimeUTC trong DTO kết quả được cộng 7 giờ (AddHours(7)) để hiển thị theo giờ Việt Nam dạng chuỗi yyyy-MM-dd theo quy tắc tầng Biz nguồn :21221.
- Bỏ qua xác thực và phân quyền: Bỏ qua các bước kiểm tra cổng và người dùng gồm CheckGatewayAuthentication và CheckAuthorize có trong mã nguồn gate.
- Dữ liệu mẫu (Seed): Tích hợp Crd_CardTransactionSeeder (Order 50) tự động nạp dữ liệu liên kết gồm 2 hội viên CrdMember (MEM01, MEM02), 2 thẻ CrdCard (CARD01, CARD02), 5 giao dịch thẻ CrdCardTransaction (tiêu dùng, tích điểm, ưu đãi) và 1 giao dịch CrdMemberDiscountTransaction để kiểm thử các luồng tìm kiếm và union chiết khấu (Models/Port/Crd_CardTransaction.cs:33-400).

---

## 6. Nhật ký thay đổi

- Hạng mục #012: Port 1:1 action Crd_CardTransactionController/Search (commit d30cdab).
- Hạng mục #013: Port 1:1 action Crd_CardTransactionController/SearchDetailCrdDealUsePromotion (commit d30cdab).
- Hạng mục #014: Port 1:1 action Crd_CardTransactionController/Export (commit d30cdab).
