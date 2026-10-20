# Tài liệu API Crd_MemberDiscountTransactionController

Phạm vi: Crd_MemberDiscountTransactionController (Search)
Cập nhật: 2026-10-17
Phiên bản mã: 5d5aba5
Trạng thái: Nháp — chờ review

---

## 1. Tóm tắt

- Mục đích nghiệp vụ: Quản lý tra cứu và tìm kiếm lịch sử các giao dịch chiết khấu dịch vụ của hội viên (CrdMemberDiscountTransaction) với loại giao dịch cố định DISCOUNTRO. Hỗ trợ lọc theo khoảng ngày thực hiện, mã hội viên, khoảng giá trị điểm chiết khấu và kết hợp thông tin hội viên từ bảng CrdMember.
- Bảng nguồn:
  * Nguồn Gate:
    - 13.ClientGate/V10/idn.Loyalty.WebAPI/Controllers/Crd_MemberDiscountTransactionController.cs:31-138 (Search)
    - Crd_MemberDiscountTransactionController.cs:142-179 (strWhereClause_Crd_MemberDiscountTransaction)
  * Nguồn API:
    - 11.BackEnd/V10/idn.Loyalty.Biz.Web/Controllers/CrdMemberDiscountTransactionController.cs:22-89 (WA_Crd_MemberDiscountTransaction_Get)
  * Nguồn Biz:
    - 11.BackEnd/V10/idn.Loyalty.Biz/Transaction.cs:5177-5270 (WAS_Crd_MemberDiscountTransaction_Get)
    - Transaction.cs:5271-5423 (Crd_MemberDiscountTransaction_Get)
    - Transaction.cs:5424-5629 (Crd_MemberDiscountTransaction_GetX)
- Bảng cơ sở dữ liệu: Crd_MemberDiscountTransaction (Models/PortLoyalty.cs:273-307, alias trong Models/Port/Crd_MemberDiscountTransaction.cs:8-10)

| Tên cột | Kiểu trong EF | Khóa | Ghi chú |
|---|---|---|---|
| AutoId | long | PK, Identity | Khóa chính tự tăng (DDL 20260817.CreTb.Crd_MemberDiscountTransaction.sql) |
| RefNo | string | Required | Mã giao dịch tham chiếu / số báo giá RO |
| NetworkID | string? | Nullable | Mã định danh mạng |
| OrgID | string? | Nullable | Mã định danh đơn vị/tổ chức |
| DLCode | string? | Nullable | Mã đại lý giao dịch |
| MemberNo | string | Required | Mã hội viên |
| CardNo | string? | Nullable | Mã thẻ hội viên |
| CardTypeUse | string? | Nullable | Hạng thẻ sử dụng |
| CardTypeInit | string? | Nullable | Hạng thẻ thực |
| DealPointType | string? | Nullable | Loại giao dịch (cố định lọc DISCOUNTRO) |
| AmountForDC | double? | Nullable | Số tiền làm căn cứ chiết khấu |
| PointChTotal | double? | Nullable | Điểm/giá trị chiết khấu giao dịch |
| CreateDate | DateTime? | Nullable | Ngày thực hiện giao dịch |
| CreateDTimeUTC | DateTime? | Nullable | Thời điểm tạo bản ghi (UTC) |
| CreateBy | string? | Nullable | Người tạo bản ghi |
| LUDTimeUTC | DateTime? | Nullable | Thời điểm cập nhật cuối (UTC) |
| LUBy | string? | Nullable | Người cập nhật cuối |
| LogLUDTimeUTC | DateTime? | Nullable | Thời điểm ghi log cập nhật cuối (UTC) |
| LogLUBy | string? | Nullable | Người ghi log |

---

## 2. Endpoint: POST /api/crd_memberdiscounttransaction/search

- Route: POST /api/crd_memberdiscounttransaction/search (Controllers/Port/Crd_MemberDiscountTransactionControllerPort.cs:58)
- HTTP method: POST

### Bảng tham số request (Crd_MemberDiscountTransactionSearchReq)

| Tên tham số | Kiểu | Bắt buộc? | Mặc định | Ý nghĩa |
|---|---|---|---|---|
| MemberNo | string? | Có | null | Mã hội viên cần tra cứu (gate :36, :52, :60-63) |
| CreateDateFrom | string? | Có | null | Ngày thực hiện giao dịch từ (gate :32, :53, :56-59) |
| CreateDateTo | string? | Không | null | Ngày thực hiện giao dịch đến (gate :33, :54) |
| PointChTotalFrom | string? | Không | null | Điểm/giá trị chiết khấu từ (gate :34) |
| PointChTotalTo | string? | Không | null | Điểm/giá trị chiết khấu đến (gate :35) |

### Quy tắc xử lý theo thứ tự

1. Chuẩn hóa chuỗi đầu vào theo TUtils.CUtils.StdStr của gate: Loại bỏ khoảng trắng hai đầu (.Trim()) của MemberNo, CreateDateFrom, CreateDateTo; nếu là chuỗi rỗng hoặc chỉ có khoảng trắng thì chuyển thành null (Controllers/Port/Crd_MemberDiscountTransactionControllerPort.cs:61-63).
2. Kiểm tra validation theo đúng thứ tự tầng Gate:
   - Kiểm tra CreateDateFrom: Nếu null hoặc rỗng, trả về mã lỗi invalid_CreateDateFrom qua PortResult.Fail (Controllers/Port/Crd_MemberDiscountTransactionControllerPort.cs:67-70).
   - Kiểm tra MemberNo: Nếu null hoặc rỗng, trả về mã lỗi invalid_MemberNo qua PortResult.Fail (Controllers/Port/Crd_MemberDiscountTransactionControllerPort.cs:73-76).
3. Thiết lập truy vấn kết hợp: Thực hiện inner join giữa bảng CrdMemberDiscountTransaction và bảng CrdMember theo điều kiện mvt.MemberNo == cm.MemberNo (Controllers/Port/Crd_MemberDiscountTransactionControllerPort.cs:79-81).
4. Áp dụng các điều kiện lọc trong mệnh đề WHERE:
   - Cố định loại giao dịch DealPointType: So khớp không phân biệt hoa/thường với chuỗi discountro (Controllers/Port/Crd_MemberDiscountTransactionControllerPort.cs:84).
   - Lọc theo MemberNo: So khớp chính xác không phân biệt hoa/thường bằng PortQuery.Equal (Controllers/Port/Crd_MemberDiscountTransactionControllerPort.cs:87).
   - Lọc theo CreateDateFrom: Phân tích cú pháp ngày (DateTime.TryParse) và lọc CreateDate >= dFrom (Controllers/Port/Crd_MemberDiscountTransactionControllerPort.cs:90-94).
   - Lọc theo CreateDateTo: Nếu có giá trị, phân tích cú pháp ngày và lọc CreateDate <= dTo (Controllers/Port/Crd_MemberDiscountTransactionControllerPort.cs:97-104).
   - Lọc theo PointChTotalFrom: Nếu có giá trị, thay dấu phẩy bằng dấu chấm, phân tích số thực và lọc PointChTotal >= ptFrom (Controllers/Port/Crd_MemberDiscountTransactionControllerPort.cs:107-111).
   - Lọc theo PointChTotalTo: Nếu có giá trị, thay dấu phẩy bằng dấu chấm, phân tích số thực và lọc PointChTotal <= ptTo (Controllers/Port/Crd_MemberDiscountTransactionControllerPort.cs:114-118).
5. Sắp xếp kết quả: Sắp xếp giảm dần theo LogLUDTimeUTC, sau đó giảm dần theo AutoId (Controllers/Port/Crd_MemberDiscountTransactionControllerPort.cs:123).
6. Phân trang và đóng gói:
   - Lấy toàn bộ danh sách kết quả phù hợp (Controllers/Port/Crd_MemberDiscountTransactionControllerPort.cs:127).
   - Tính tổng số dòng ItemCount. Cấu hình mặc định pageSizeConfig = 10 dùng để tính số lượng trang PageCount (Controllers/Port/Crd_MemberDiscountTransactionControllerPort.cs:128-130).
   - Trả về đối tượng PageInfo<CrdMemberDiscountTransaction> với PageIndex = 0 và PageSize = 0 (khớp hành vi gate :74-78, :113-125) (Controllers/Port/Crd_MemberDiscountTransactionControllerPort.cs:132-138).
7. Trả về kết quả HTTP 200 dạng Ok({ Success = true, Data = pageInfo }) (Controllers/Port/Crd_MemberDiscountTransactionControllerPort.cs:140-144).

### Bảng mã lỗi

| Mã lỗi | Điều kiện |
|---|---|
| invalid_CreateDateFrom | Tham số CreateDateFrom là null hoặc rỗng |
| invalid_MemberNo | Tham số MemberNo là null hoặc rỗng |

*Lưu ý: Các mã lỗi trên được trả về qua PortResult.Fail.*

### Ví dụ request suy ra từ mã

    {
        "MemberNo": "MB5501",
        "CreateDateFrom": "2026-03-01",
        "CreateDateTo": "2026-03-31",
        "PointChTotalFrom": "100000",
        "PointChTotalTo": "500000"
    }

### Ví dụ response

Chưa có lần chạy mẫu

### Hành vi đặc biệt

- Ràng buộc tồn tại hội viên (Inner Join): Giao dịch chỉ được trả về nếu mã MemberNo tồn tại đồng thời trong bảng CrdMember. Nếu có giao dịch mang MemberNo không có trong CrdMember (như dữ liệu seed MB_GHOST), bản ghi đó sẽ bị loại khỏi kết quả truy vấn (Controllers/Port/Crd_MemberDiscountTransactionControllerPort.cs:79-81).
- Cố định DealPointType: DTO không cho phép truyền DealPointType mà controller tự động ép điều kiện DealPointType là DISCOUNTRO (Controllers/Port/Crd_MemberDiscountTransactionControllerPort.cs:84).
- Cấu hình phân trang cố định: PageIndex và PageSize trong kết quả trả về luôn bằng 0 do gate nguồn không nhận 2 tham số này từ client, nhưng PageCount được tính dựa trên số lượng 10 bản ghi/trang (Controllers/Port/Crd_MemberDiscountTransactionControllerPort.cs:129-138).
- Bỏ qua OrgID và KeyWord: Các tham số tổ chức và từ khóa không được sinh mệnh đề WHERE trong SQL khớp với hành vi gốc tại gate :142-179 và Biz :5473-5500.

---

## 3. Khác biệt so với nguồn & GAP

- Tái sử dụng Entity có sẵn: Tái sử dụng entity CrdMemberDiscountTransaction đã được định nghĩa tại Models/PortLoyalty.cs:273-307 thay vì tạo mới entity nhằm tránh xung đột mapping EF Core.
- Xác thực và phân quyền: Chưa tích hợp các kiểm tra CheckGatewayAuthentication và phân quyền người dùng như hệ thống gate nguồn.
- Ghi nhật ký: Bỏ qua việc ghi RequestLog và ReturnLog có trong tầng Biz nguồn.
- Dữ liệu mẫu (Seed): Tích hợp Crd_MemberDiscountTransactionSeeder (Order 77) nạp sẵn 2 hội viên (MB5501, MB5502), 2 thẻ (CD5501, CD5502) và 6 giao dịch mẫu (AutoId từ 55001 đến 55006) để phục vụ kiểm thử (Models/Port/Crd_MemberDiscountTransaction.cs:33-181).

---

## 4. Nhật ký thay đổi

- Hạng mục #055: Port 1:1 action Crd_MemberDiscountTransactionController/Search từ gate sang MiniLoyalty (commit 5d5aba5).
