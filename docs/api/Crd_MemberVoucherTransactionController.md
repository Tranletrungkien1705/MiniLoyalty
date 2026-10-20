# Tài liệu API Crd_MemberVoucherTransactionController

Phạm vi: Crd_MemberVoucherTransactionController (Search)
Cập nhật: 2026-10-17
Phiên bản mã: f0877ac
Trạng thái: Nháp — chờ review

---

## 1. Tóm tắt

- Mục đích nghiệp vụ: Quản lý tra cứu và tìm kiếm lịch sử giao dịch voucher của hội viên (Crd_MemberVoucherTransaction). Hỗ trợ lọc theo khoảng ngày thực hiện, mã hội viên, loại giao dịch điểm và khoảng điểm giao dịch; kết hợp thông tin điểm voucher còn lại thực tế từ bảng Crd_MemberVoucher.
- Bảng nguồn:
  * Nguồn Gate:
    - Crd_MemberVoucherTransactionController.cs:31-134 (Search)
    - Crd_MemberVoucherTransactionController.cs:396-433 (strWhereClause_Crd_MemberVoucherTransaction)
  * Nguồn API:
    - CrdMemberVoucherTransactionController.cs:23-90 (WA_Crd_MemberVoucherTransaction_Get)
  * Nguồn Biz:
    - Transaction.cs:4149-4242 (WAS_Crd_MemberVoucherTransaction_Get)
    - Transaction.cs:4243-4395 (Crd_MemberVoucherTransaction_Get)
    - Transaction.cs:4396-4614 (Crd_MemberVoucherTransaction_GetX)
- Bảng cơ sở dữ liệu:
  * Bảng chính: Crd_MemberVoucherTransaction (Models/Port/Crd_MemberVoucherTransaction.cs:23-64)
  * Bảng phụ đối chiếu: Crd_MemberVoucher (Models/Port/Crd_MemberVoucherTransaction.cs:81-98)
  * Bảng hội viên liên kết: CrdMember (Models/Port/Crd_MemberVoucherTransaction.cs:122-152)

### Bảng cấu trúc Crd_MemberVoucherTransaction

| Tên cột | Kiểu trong EF | Khóa | Ghi chú |
|---|---|---|---|
| AutoId | long | PK, Identity | Khóa chính tự tăng |
| RefNo | string | Required | Mã giao dịch / số tham chiếu voucher |
| NetworkID | string? | Nullable | Mã định danh mạng |
| OrgID | string? | Nullable | Mã định danh đơn vị/tổ chức |
| DLCode | string? | Nullable | Mã đại lý giao dịch |
| MemberNo | string | Required | Mã hội viên |
| CardNo | string? | Nullable | Mã thẻ hội viên |
| CardTypeUse | string? | Nullable | Hạng thẻ sử dụng |
| CardTypeInit | string? | Nullable | Hạng thẻ thực |
| DealPointType | string? | Nullable | Loại giao dịch tích/tiêu điểm voucher |
| PrmVoucherCode | string? | Nullable | Mã chương trình voucher |
| EffDateStart | DateTime? | Nullable | Ngày hiệu lực chương trình từ |
| EffDateEnd | DateTime? | Nullable | Ngày hiệu lực chương trình đến |
| PointChTotal | double? | Nullable | Điểm voucher tích/tiêu (hạng sử dụng) |
| PointActualChTotal | double? | Nullable | Điểm voucher tích/tiêu (hạng thực) |
| PointVCRemain | double? | Nullable | Điểm còn lại của voucher (được override từ Crd_MemberVoucher nếu có) |
| PointActualVCRemain | double? | Nullable | Điểm còn lại của voucher (hạng thực) |
| PointVCLimit | double? | Nullable | Điểm sử dụng tối đa của voucher |
| PointExpiryDate | DateTime? | Nullable | Ngày hết hạn sử dụng điểm voucher |
| PointVCMemberRemain | double? | Nullable | Tổng điểm voucher còn lại của hội viên (hạng sử dụng) |
| PointActualVCMemberRM | double? | Nullable | Tổng điểm voucher còn lại của hội viên (hạng thực) |
| PointVCRO | double? | Nullable | Tổng điểm voucher sử dụng của báo giá (hạng sử dụng) |
| PointActualVCRO | double? | Nullable | Tổng điểm voucher sử dụng của báo giá (hạng thực) |
| QtyUseVCLimit | double? | Nullable | Số lần sử dụng voucher tối đa |
| QtyUseVCRemain | double? | Nullable | Số lần sử dụng voucher còn lại |
| RefCode00 | string? | Nullable | Số tham chiếu 00 |
| RefCode01 | string? | Nullable | Số tham chiếu 01 |
| RefCode02 | string? | Nullable | Số tham chiếu 02 |
| RefCode03 | string? | Nullable | Số tham chiếu 03 |
| RefCode04 | string? | Nullable | Số tham chiếu 04 |
| RefCode05 | string? | Nullable | Số tham chiếu 05 |
| CreateDate | DateTime? | Nullable | Ngày thực hiện giao dịch |
| CreateDTimeUTC | DateTime? | Nullable | Thời điểm tạo bản ghi (UTC) |
| CreateBy | string? | Nullable | Người tạo |
| LUDTimeUTC | DateTime? | Nullable | Thời điểm cập nhật cuối (UTC) |
| LUBy | string? | Nullable | Người cập nhật cuối |
| LogLUDTimeUTC | DateTime? | Nullable | Thời điểm ghi log cập nhật cuối (UTC) |
| LogLUBy | string? | Nullable | Người ghi log cập nhật cuối |
| MyIdxSeq | long? | NotMapped | Số thứ tự dòng bản ghi |
| cm_MemberName | string? | NotMapped | Tên hội viên |
| cm_CarNo | string? | NotMapped | Biển số xe |
| ic_VoucherNo | string? | NotMapped | Mã voucher sử dụng |

### Bảng cấu trúc Crd_MemberVoucher

| Tên cột | Kiểu trong EF | Khóa | Ghi chú |
|---|---|---|---|
| MemberNo | string | PK (composite) | Mã hội viên |
| VoucherNo | string | PK (composite) | Mã voucher |
| NetworkID | string? | Nullable | Mã định danh mạng |
| PointVCTotal | double? | Nullable | Tổng điểm voucher ban đầu |
| PointVCRemain | double? | Nullable | Điểm voucher còn lại hiện tại |
| PointVCLimit | double? | Nullable | Điểm sử dụng tối đa |
| PointExpireDate | DateTime? | Nullable | Ngày hết hạn voucher |
| FlagActive | string? | Nullable | Trạng thái kích hoạt (1: hoạt động, 0: dừng) |
| CreateDTimeUTC | DateTime? | Nullable | Thời điểm tạo bản ghi (UTC) |
| CreateBy | string? | Nullable | Người tạo |
| LUDTimeUTC | DateTime? | Nullable | Thời điểm cập nhật cuối (UTC) |
| LUBy | string? | Nullable | Người cập nhật cuối |
| LogLUDTimeUTC | DateTime? | Nullable | Thời điểm ghi log UTC |
| LogLUBy | string? | Nullable | Người ghi log |

---

## 2. Endpoint: POST /api/crd_membervouchertransaction/search

- Route: POST /api/crd_membervouchertransaction/search (Controllers/Port/Crd_MemberVoucherTransactionControllerPort.cs:41)
- HTTP method: POST

### Bảng tham số request (Crd_MemberVoucherTransactionSearchReq)

| Tên tham số | Kiểu | Bắt buộc? | Mặc định | Ý nghĩa |
|---|---|---|---|---|
| CreateDateFrom | string? | Có | null | Ngày thực hiện giao dịch từ (chuỗi ngày tháng, định dạng chuẩn) |
| CreateDateTo | string? | Không | null | Ngày thực hiện giao dịch đến (chuỗi ngày tháng) |
| PointChTotalFrom | string? | Không | null | Điểm giao dịch từ (chuyển dấu phẩy thành chấm nếu có) |
| PointChTotalTo | string? | Không | null | Điểm giao dịch đến (chuyển dấu phẩy thành chấm nếu có) |
| DealPointType | string? | Có | null | Danh sách loại giao dịch (chuỗi phân cách dấu phẩy, vd: VOUCHERSD,VOUCHERXM) |
| MemberNo | string? | Có | null | Mã hội viên cần tìm kiếm (so khớp chính xác) |

### Quy tắc xử lý theo thứ tự

1. Chuẩn hóa chuỗi đầu vào: Cắt khoảng trắng hai đầu (.Trim()) của MemberNo, CreateDateFrom, CreateDateTo, DealPointType; nếu rỗng hoặc toàn khoảng trắng thì chuyển về null (Controllers/Port/Crd_MemberVoucherTransactionControllerPort.cs:44-48).
2. Kiểm tra tính hợp lệ của tham số bắt buộc theo đúng thứ tự tầng Gate:
   - Nếu CreateDateFrom rỗng: Trả về lỗi validation invalid_CreateDateFrom (Controllers/Port/Crd_MemberVoucherTransactionControllerPort.cs:51-54).
   - CreateDateTo không kiểm tra bắt buộc (khớp mã gate nguồn bị comment out :61-64).
   - Nếu DealPointType rỗng: Trả về lỗi validation invalid_DealPointType (Controllers/Port/Crd_MemberVoucherTransactionControllerPort.cs:59-62).
   - Nếu MemberNo rỗng: Trả về lỗi validation invalid_MemberNo (Controllers/Port/Crd_MemberVoucherTransactionControllerPort.cs:65-68).
3. Thiết lập truy vấn kết hợp: Thực hiện inner join giữa bảng Crd_MemberVoucherTransaction và bảng CrdMember theo điều kiện mvt.MemberNo == cm.MemberNo (Controllers/Port/Crd_MemberVoucherTransactionControllerPort.cs:72-74).
4. Áp dụng các điều kiện lọc:
   - Lọc theo MemberNo: So khớp chính xác không phân biệt hoa thường bằng PortQuery.Equal (Controllers/Port/Crd_MemberVoucherTransactionControllerPort.cs:77).
   - Lọc theo CreateDateFrom: Phân tích chuỗi sang DateTime (theo chuẩn InvariantCulture hoặc mặc định) và lọc điều kiện CreateDate >= dFrom (Controllers/Port/Crd_MemberVoucherTransactionControllerPort.cs:80-84).
   - Lọc theo CreateDateTo: Nếu có giá trị, phân tích sang DateTime và lọc điều kiện CreateDate <= dTo (Controllers/Port/Crd_MemberVoucherTransactionControllerPort.cs:87-94).
   - Lọc theo PointChTotalFrom: Nếu có giá trị, chuyển dấu phẩy thành chấm, phân tích số thực và lọc điều kiện PointChTotal >= ptFrom (Controllers/Port/Crd_MemberVoucherTransactionControllerPort.cs:97-101).
   - Lọc theo PointChTotalTo: Nếu có giá trị, chuyển dấu phẩy thành chấm, phân tích số thực và lọc điều kiện PointChTotal <= ptTo (Controllers/Port/Crd_MemberVoucherTransactionControllerPort.cs:104-108).
   - Lọc theo DealPointType: Tách chuỗi theo dấu phẩy thành mảng loại bỏ phần tử rỗng và khoảng trắng thừa, chuyển về chữ thường và lọc bằng toán tử Contains (IN trong SQL) với DealPointType (Controllers/Port/Crd_MemberVoucherTransactionControllerPort.cs:111-114).
5. Sắp xếp kết quả: Sắp xếp giảm dần theo mvt.LogLUDTimeUTC (Controllers/Port/Crd_MemberVoucherTransactionControllerPort.cs:117).
6. Left join với bảng Crd_MemberVoucher: Kết hợp theo cặp khóa { VoucherNo = mvt.RefNo, mvt.MemberNo } và { VoucherNo = mvc.VoucherNo, mvc.MemberNo } để lấy giá trị PointVCRemain hiện thời của voucher (Controllers/Port/Crd_MemberVoucherTransactionControllerPort.cs:120-128).
7. Xử lý kết quả trả về:
   - Nếu không có dòng dữ liệu nào khớp: Trả về HTTP 200 kèm JSON có Data.DataList = null (Controllers/Port/Crd_MemberVoucherTransactionControllerPort.cs:134-143).
   - Nếu có dữ liệu: Duyệt danh sách bản ghi, gán số thứ tự MyIdxSeq tăng dần từ 0; ghi đè (override) PointVCRemain của giao dịch bằng giá trị PointVCRemain từ Crd_MemberVoucher nếu có giá trị (Controllers/Port/Crd_MemberVoucherTransactionControllerPort.cs:145-156).
8. Trả về HTTP 200 bọc trong cấu trúc { Success = true, Data = { DataList = resultList } } (Controllers/Port/Crd_MemberVoucherTransactionControllerPort.cs:158-165).

### Bảng mã lỗi

| Mã lỗi | Điều kiện |
|---|---|
| invalid_CreateDateFrom | Tham số CreateDateFrom là null hoặc rỗng |
| invalid_DealPointType | Tham số DealPointType là null hoặc rỗng |
| invalid_MemberNo | Tham số MemberNo là null hoặc rỗng |

*Lưu ý: Các mã lỗi trên được trả về qua PortResult.Fail với cấu trúc JSON chuẩn của hệ thống.*

### Ví dụ request suy ra từ mã

    {
        "MemberNo": "MB5601",
        "CreateDateFrom": "2026-05-01",
        "CreateDateTo": "2026-05-31",
        "PointChTotalFrom": "50",
        "PointChTotalTo": "500",
        "DealPointType": "VOUCHERSD,VOUCHERXM"
    }

### Ví dụ response

Chưa có lần chạy mẫu

### Hành vi đặc biệt

- DataList trả về null khi không có dữ liệu: Khác với các endpoint trả về mảng rỗng [] hoặc cấu trúc PageInfo, khi kết quả truy vấn rỗng, endpoint trả về thuộc tính Data.DataList = null theo đúng hành vi tầng Gate nguồn (Crd_MemberVoucherTransactionController.cs:111-121).
- Ghi đè PointVCRemain: Điểm còn lại của voucher lưu tại bảng giao dịch Crd_MemberVoucherTransaction sẽ được ghi đè bằng giá trị thực tế hiện tại từ bảng Crd_MemberVoucher nếu tìm thấy bản ghi tương ứng với cùng MemberNo và RefNo (Controllers/Port/Crd_MemberVoucherTransactionControllerPort.cs:150-153).
- Ràng buộc hội viên (Inner Join): Giao dịch chỉ được trả về nếu mã hội viên MemberNo tồn tại đồng thời trong bảng danh mục hội viên CrdMember.
- Kiểm tra CreateDateTo: Tầng Gate nguồn có mã kiểm tra bắt buộc CreateDateTo nhưng đã bị đóng comment (:61-64), do đó controller port cho phép CreateDateTo rỗng.

---

## 3. Khác biệt so với nguồn & GAP

- Bỏ qua xác thực và phân quyền (Loại Đ): Bỏ qua các bước kiểm tra cổng và người dùng gồm CheckGatewayAuthentication (Gate :47, API :42), CheckAuthorize (Gate :48), Sys_User_CheckAuthorize (Transaction.cs:4307-4320).
- Bỏ qua ghi nhật ký (Loại Đ): Bỏ qua ghi RequestLog và ReturnLog (Transaction.cs:4286-4293, 4385-4392).
- Không phân trang: Endpoint trả về toàn bộ danh sách kết quả thỏa mãn điều kiện lọc mà không áp dụng phân trang Ft_PageIndex / Ft_PageSize.
- Dữ liệu mẫu (Seed): Tích hợp Crd_MemberVoucherTransactionSeeder (Order 58) nạp sẵn 2 hội viên (MB5601, MB5602), 1 bản ghi Crd_MemberVoucher và 4 bản ghi Crd_MemberVoucherTransaction phục vụ kiểm thử đối chiếu (Models/Port/Crd_MemberVoucherTransaction.cs:117-251).

---

## 4. Nhật ký thay đổi

- Hạng mục #056: Port 1:1 action Crd_MemberVoucherTransactionController/Search từ gate sang MiniLoyalty (commit f0877ac).
