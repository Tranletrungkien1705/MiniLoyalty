# Tài liệu API Prm_VoucherNewCarController

Phạm vi: Prm_VoucherNewCarController (Search)
Cập nhật: 2026-10-17
Phiên bản mã: 60bdafd
Trạng thái: Nháp — chờ review

---

## 1. Tóm tắt

- Mục đích nghiệp vụ: Quản lý tra cứu và tìm kiếm thông tin các chương trình voucher dành cho xe mới (Prm_VoucherNewCar). Hỗ trợ lọc theo mã, tên, khoảng thời gian hiệu lực, thời điểm tạo và cờ trạng thái hiển thị hiệu lực (FlagShow) kết hợp phân trang dữ liệu.
- Bảng nguồn:
  * Nguồn gate:
    - Prm_VoucherNewCarController.cs:32-146 (Search -> WA_Prm_VoucherNewCar_Get)
    - Prm_VoucherNewCarController.cs:723-760 (strWhereClause_Prm_VoucherNewCar)
    - Prm_VoucherNewCarController.cs:775-811 (strWhereClause_Prm_VoucherNewCarFlagShow)
  * Nguồn API:
    - PrmVoucherNewCarController.cs:21-89 (WA_Prm_VoucherNewCar_Get)
  * Nguồn biz:
    - Promotion.cs:5746-5867 (WAS_Prm_VoucherNewCar_Get)
    - Promotion.cs:5869-6026 (Prm_VoucherNewCar_Get)
    - Promotion.cs:6028-6273 (Prm_VoucherNewCar_GetX)
- Bảng cơ sở dữ liệu: Prm_VoucherNewCar (Models/Port/Prm_VoucherNewCar.cs:12-48)

| Tên cột | Kiểu trong EF | Khóa | Ghi chú |
|---|---|---|---|
| PrmVoucherCode | string | PK, Required | Mã chương trình voucher |
| NetworkID | string | Required | Định danh mạng |
| OrgID | string | Required | Định danh đơn vị/tổ chức |
| PrmVoucherName | string | Required | Tên chương trình voucher |
| EffDateStart | string | Required | Ngày hiệu lực bắt đầu (định dạng yyyy-MM-dd) |
| EffDateEnd | string | Required | Ngày hiệu lực kết thúc (định dạng yyyy-MM-dd) |
| QtyDayLimitFDlvDate | double? | Nullable | Số ngày giới hạn từ ngày giao xe |
| ValidityPeriod | double? | Nullable | Thời hạn hiệu lực của voucher (ngày) |
| FlagAllModel | string? | Nullable | Cờ áp dụng cho tất cả model xe |
| PointVoucherAllModel | double? | Nullable | Điểm tặng xe mới áp dụng cho tất cả model |
| PointUseLimitAllModel | double? | Nullable | Điểm sử dụng tối đa trên mỗi lần dịch vụ cho tất cả model |
| CreateDTimeUTC | DateTime | Required | Thời điểm tạo bản ghi (UTC) |
| CreateBy | string | Required | Người tạo bản ghi |
| LUDTimeUTC | DateTime? | Nullable | Thời điểm cập nhật cuối (UTC) |
| LUBy | string? | Nullable | Người cập nhật cuối |
| ApprDTimeUTC | DateTime? | Nullable | Thời điểm phê duyệt A (UTC) |
| ApprBy | string? | Nullable | Người phê duyệt A |
| FinishDTimeUTC | DateTime? | Nullable | Thời điểm phê duyệt hoàn tất F (UTC) |
| FinishBy | string? | Nullable | Người phê duyệt F |
| CancelDTimeUTC | DateTime? | Nullable | Thời điểm từ chối hủy (UTC) |
| CancelBy | string? | Nullable | Người từ chối |
| PrmVoucherStatus | string? | Nullable | Trạng thái phê duyệt chương trình |
| Remark | string? | Nullable | Ghi chú diễn giải |
| LogLUDTimeUTC | DateTime? | Nullable | Thời điểm ghi log cập nhật cuối |
| LogLUBy | string? | Nullable | Người ghi log cập nhật cuối |
| MyIdxSeq | long? | NotMapped | Số thứ tự dòng bản ghi phân trang |
| FlagShow | string? | NotMapped | Trạng thái hiển thị chương trình hiệu lực (1/0) |
| PointActualVCAllModel | double? | NotMapped | Điểm thực tế voucher cho tất cả model |
| QtyUseLimitAllModel | double? | NotMapped | Giới hạn số lượng sử dụng cho tất cả model |
| PointVoucherCalc | double? | NotMapped | Điểm voucher sau tính toán |

---

## 2. Endpoint: POST /api/prm_vouchernewcar/search

- Route: POST /api/prm_vouchernewcar/search (Controllers/Port/Prm_VoucherNewCarControllerPort.cs:68)
- HTTP method: POST

### Bảng tham số request (Prm_VoucherNewCarSearchReq)

| Tên tham số | Kiểu | Bắt buộc? | Mặc định | Ý nghĩa |
|---|---|---|---|---|
| OrgID | string? | Không | null | Mã tổ chức/đơn vị (nhận vào nhưng không sinh điều kiện lọc theo gate :723-760) |
| KeyWord | string? | Không | null | Từ khóa tìm kiếm (nhận vào nhưng không sinh điều kiện lọc theo gate :723-760) |
| PrmVoucherCode | string? | Không | null | Mã chương trình voucher (so khớp chính xác, không phân biệt hoa thường) |
| PrmVoucherName | string? | Không | null | Tên chương trình voucher (so khớp chính xác, không phân biệt hoa thường) |
| QtyDayLimitFDlvDate | string? | Không | null | Số ngày giới hạn từ ngày giao xe (không sinh điều kiện lọc) |
| EffDateStart | string? | Không | null | Ngày hiệu lực bắt đầu (so khớp chính xác, không phân biệt hoa thường) |
| EffDateEnd | string? | Không | null | Ngày hiệu lực kết thúc (so khớp chính xác, không phân biệt hoa thường) |
| ValidityPeriod | string? | Không | null | Thời hạn hiệu lực (không sinh điều kiện lọc) |
| PrmVoucherStatus | string? | Không | null | Trạng thái voucher (không sinh điều kiện lọc) |
| CreateDTimeUTCFrom | string? | Không | null | Khoảng ngày tạo từ (ghép thêm 00:00:00, lọc CreateDTimeUTC >= dtFrom) |
| CreateDTimeUTCTo | string? | Không | null | Khoảng ngày tạo đến (ghép thêm 23:59:59, lọc CreateDTimeUTC <= dtTo) |
| FlagShow | string? | Không | null | Cờ lọc trạng thái: "1" (chương trình FINISH có hiệu lực hôm nay) hoặc "0" (ngược lại) |
| Ft_PageIndex | int | Không | 0 | Chỉ số trang cần lấy (bắt đầu từ 0) |
| Ft_PageSize | int | Không | 10 | Số lượng bản ghi trên một trang |

### Quy tắc xử lý theo thứ tự

1. Khởi tạo đối tượng request mặc định nếu payload truyền vào là null.
2. Thiết lập truy vấn cơ sở dữ liệu trên bảng Prm_VoucherNewCar.
3. Lọc theo PrmVoucherCode: Nếu có giá trị, so khớp chính xác không phân biệt hoa thường sử dụng hàm PortQuery.Equal (Controllers/Port/Prm_VoucherNewCarControllerPort.cs:78-82).
4. Lọc theo PrmVoucherName: Nếu có giá trị, so khớp chính xác không phân biệt hoa thường sử dụng hàm PortQuery.Equal (Controllers/Port/Prm_VoucherNewCarControllerPort.cs:85-89).
5. Lọc theo EffDateStart: Nếu có giá trị, so khớp chính xác chuỗi ngày EffDateStart (Controllers/Port/Prm_VoucherNewCarControllerPort.cs:92-96).
6. Lọc theo EffDateEnd: Nếu có giá trị, so khớp chính xác chuỗi ngày EffDateEnd (Controllers/Port/Prm_VoucherNewCarControllerPort.cs:99-103).
7. Lọc theo CreateDTimeUTCFrom / To: Nếu có giá trị, phân tích chuỗi ngày sang DateTime và lọc CreateDTimeUTC nằm trong khoảng từ CreateDTimeUTCFrom 00:00:00 đến CreateDTimeUTCTo 23:59:59 (Controllers/Port/Prm_VoucherNewCarControllerPort.cs:106-119).
8. Lọc theo FlagShow: Lấy ngày hiện tại todayFilterStr = DateTime.UtcNow.ToString("yyyy-MM-dd") (Controllers/Port/Prm_VoucherNewCarControllerPort.cs:122-133):
   - Nếu FlagShow == "1": Điều kiện PrmVoucherStatus == "FINISH" và EffDateStart <= todayFilterStr và todayFilterStr <= EffDateEnd.
   - Nếu FlagShow == "0": Điều kiện PrmVoucherStatus != "FINISH" hoặc (PrmVoucherStatus == "FINISH" và (EffDateStart > todayFilterStr hoặc todayFilterStr > EffDateEnd)).
9. Đếm tổng số bản ghi phù hợp và sắp xếp giảm dần theo CreateDTimeUTC (Controllers/Port/Prm_VoucherNewCarControllerPort.cs:136-137).
10. Phân trang: Bỏ qua (pageIndex * pageSize) bản ghi và lấy pageSize bản ghi. Trường hợp pageSize == 0, trả về danh sách rỗng (Controllers/Port/Prm_VoucherNewCarControllerPort.cs:140-148).
11. Xác định chương trình hiển thị hiệu lực (PrmVoucherCodeForFlagShow): Tìm bản ghi đầu tiên có PrmVoucherStatus == "FINISH" và EffDateStart <= dateSys <= EffDateEnd với dateSys = DateTime.UtcNow.AddHours(7).ToString("yyyy-MM-dd"), sắp xếp theo EffDateStart giảm dần (Controllers/Port/Prm_VoucherNewCarControllerPort.cs:151-156).
12. Duyệt từng bản ghi trong danh sách phân trang:
    - Gán MyIdxSeq tuần tự tăng dần từ vị trí bắt đầu recordStart.
    - Gán FlagShow = "1" nếu mã chương trình trùng với PrmVoucherCodeForFlagShow, ngược lại gán "0".
    - Chuẩn hóa ngày kết thúc: Nếu EffDateEnd mang giá trị "2100-01-01" thì gán lại thành chuỗi rỗng "".
13. Tính tổng số trang PageCount và trả về HTTP 200 kèm đối tượng PageInfo<Prm_VoucherNewCar> (Controllers/Port/Prm_VoucherNewCarControllerPort.cs:171-179).

### Bảng mã lỗi

| Mã lỗi | Điều kiện |
|---|---|
| Không có | Controller không sử dụng mã ErridnLoyalty; mọi kết quả trả về bọc trong đối tượng Ok({ Success = true, Data = pageInfo }) |

### Ví dụ request suy ra từ mã

    {
        "PrmVoucherCode": "PVNC01",
        "PrmVoucherName": null,
        "EffDateStart": null,
        "EffDateEnd": null,
        "CreateDTimeUTCFrom": "2026-10-01",
        "CreateDTimeUTCTo": "2026-10-31",
        "FlagShow": "1",
        "Ft_PageIndex": 0,
        "Ft_PageSize": 10
    }

### Ví dụ response

Chưa có lần chạy mẫu

### Hành vi đặc biệt

- Khi Ft_PageSize = 0: Kết quả pagedList trả về danh sách rỗng và PageCount được đặt về 0.
- Chuẩn hóa EffDateEnd: Bản ghi có EffDateEnd bằng "2100-01-01" sẽ tự động được chuyển đổi thành chuỗi rỗng "" trước khi trả về.
- Cơ chế tính FlagShow của từng dòng: Phụ thuộc vào ngày hệ thống theo giờ địa phương (UTC+7). Chỉ duy nhất chương trình FINISH có khoảng hiệu lực chứa ngày hiện tại và có EffDateStart mới nhất mới được gán FlagShow = "1".
- Các tham số không sinh điều kiện lọc: OrgID, KeyWord, QtyDayLimitFDlvDate, ValidityPeriod, PrmVoucherStatus vẫn được giữ trong DTO để tương thích giao tiếp nhưng không tạo điều kiện where (khớp hoàn toàn hành vi của gate nguồn tại Prm_VoucherNewCarController.cs:723-760).

---

## 3. Khác biệt so với nguồn & GAP

- Định dạng ngày giờ: Tại gate nguồn (Prm_VoucherNewCarController.cs:91-94), ApprDTimeUTC và CreateDTimeUTC được chuyển đổi sang chuỗi ngày giờ theo múi giờ địa phương (utcOffset +7) dạng yyyy-MM-dd HH:mm:ss khi hiển thị lên giao diện. Ở controller port, hai trường này trả về trực tiếp đối tượng DateTime kiểu UTC thông qua JSON serializer.
- Múi giờ tính toán FlagShow: Việc lọc FlagShow ở bước truy vấn dùng UtcNow (Controllers/Port/Prm_VoucherNewCarControllerPort.cs:121), trong khi việc tính PrmVoucherCodeForFlagShow để hiển thị gán cờ lại dùng UtcNow + 7 giờ (Controllers/Port/Prm_VoucherNewCarControllerPort.cs:151), đúng theo logic nghiệp vụ đối chiếu từ nguồn gate và biz Promotion.cs:6093-6100.
- Xác thực và phân quyền: Chưa tích hợp kiểm tra token đăng nhập và phân quyền người dùng như hệ thống gate nguồn.
- Dữ liệu mẫu (Seed): Tích hợp Prm_VoucherNewCarSeeder (Order 62) tự động nạp 3 bản ghi mẫu (PVNC01, PVNC02, PVNC03) khi cơ sở dữ liệu trống.

---

## 4. Nhật ký thay đổi

- Hạng mục #264: Port 1:1 action Prm_VoucherNewCarController/Search từ gate sang MiniLoyalty (commit 60bdafd).
