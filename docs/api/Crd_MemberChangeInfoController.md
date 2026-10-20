# Tài liệu API: Crd_MemberChangeInfoController

Phạm vi: Crd_MemberChangeInfoController (POST /api/crd_memberchangeinfo/search)
Cập nhật: 2026-10-17
Phiên bản mã: 5b0e111
Trạng thái: Nháp — chờ review

## 1. Tóm tắt

Cung cấp chức năng tra cứu danh sách đề nghị thay đổi thông tin hội viên (Crd_MemberChangeInfo) kết hợp cùng thông tin chi tiết của hội viên tương ứng (Crd_Member). Dữ liệu hỗ trợ lọc theo đại lý đề nghị, mã hội viên, số đề nghị, biển số xe, trạng thái đề nghị và khoảng thời gian tạo hoặc hoàn thành đề nghị.

### Tệp nguồn và tệp đích
- Nguồn Gate: 13.ClientGate/V10/idn.Loyalty.WebAPI/Controllers/Crd_MemberChangeInfoController.cs:33-144, 974-1025
- Nguồn API: 11.BackEnd/V10/idn.Loyalty.Biz.Web/Controllers/CrdMemberChangeInfoController.cs:23-93
- Nguồn Biz / SQL: 11.BackEnd/V10/idn.Loyalty.Biz/Card.cs:27978-28280 (GetX), 28282-28441 (Get), 28443-28557 (WAS_), 27911 (Crd_MemberChangeInfo_CheckDB)
- Đích Controller: Controllers/Port/Crd_MemberChangeInfoControllerPort.cs:1-203
- Đích Model: Models/Port/Crd_MemberChangeInfo.cs:1-177

### Bảng dữ liệu Crd_MemberChangeInfo

Bảng Crd_MemberChangeInfo gồm 27 trường lưu trong cơ sở dữ liệu và các trường ánh xạ/mở rộng khi hiển thị:
- Khóa chính: RequestNo (kiểu string, không rỗng).

| Tên cột | Kiểu EF | Khóa | Cho phép null | Ghi chú |
| :--- | :--- | :--- | :--- | :--- |
| RequestNo | string | PK | Không | Số đề nghị thay đổi thông tin |
| NetworkID | string? | | Có | Mã mạng lưới |
| OrgID | string? | | Có | Mã tổ chức |
| MemberNo | string? | | Có | Mã thẻ hội viên |
| DLCodeRequest | string? | | Có | Đại lý tạo đề nghị |
| RequestType | string? | | Có | Loại đề nghị |
| RequestStatus | string? | | Có | Trạng thái đề nghị (PENDING, APPROVE, FINISH, REJECT...) |
| Remark | string? | | Có | Ghi chú nội bộ |
| RemarkHTV | string? | | Có | Ghi chú từ phía HTV |
| AttachFilePath | string? | | Có | Đường dẫn tệp đính kèm |
| AttachFileName | string? | | Có | Tên tệp đính kèm |
| AttachFileSize | string? | | Có | Kích thước tệp đính kèm |
| AttachFileBase64 | string? | | Có | Dữ liệu Base64 tệp |
| CreateDTimeUTC | string? | | Có | Thời gian tạo (UTC chuỗi) |
| CreateBy | string? | | Có | Người tạo |
| LUDTimeUTC | string? | | Có | Thời gian cập nhật gần nhất |
| LUBy | string? | | Có | Người cập nhật gần nhất |
| ApproveDTimeUTC | string? | | Có | Thời gian duyệt cấp đại lý (A) |
| ApproveBy | string? | | Có | Người duyệt cấp đại lý |
| FinishDTimeUTC | string? | | Có | Thời gian duyệt hoàn tất (F) |
| FinishBy | string? | | Có | Người duyệt hoàn tất |
| RejectDTimeUTC | string? | | Có | Thời gian từ chối |
| RejectBy | string? | | Có | Người từ chối |
| LogLUDTimeUTC | string? | | Có | Thời gian ghi log cập nhật cuối |
| LogLUBy | string? | | Có | Người ghi log cập nhật cuối |
| FlagFileUpload | string? | | Có | Cờ tệp tải lên (1: có upload, 0: không) |
| AttFileId | string? | | Có | Định danh file upload |
| ApproveDLCode | string? | | Có | Đơn vị duyệt (lưu trong DB) |
| cm_MemberName | string? | | Có | [NotMapped] Tên hội viên từ Crd_Member |
| cm_CarNo | string? | | Có | [NotMapped] Biển số xe từ Crd_Member |
| cm_PhoneNo | string? | | Có | [NotMapped] Số điện thoại từ Crd_Member |
| cm_VIN | string? | | Có | [NotMapped] Số khung VIN từ Crd_Member |
| cm_AmountEnd | double? | | Có | [NotMapped] Số dư tiền cuối kỳ từ Crd_Member |
| cm_CardTypeUse | string? | | Có | [NotMapped] Hạng thẻ sử dụng từ Crd_Member |
| FlagApproveAF | string? | | Có | [NotMapped] Cờ quyền duyệt (1: được duyệt, 0: không) |

## 2. Endpoint chi tiết

### POST /api/crd_memberchangeinfo/search

- Route: /api/crd_memberchangeinfo/search
- Method: POST
- Mục đích: Tìm kiếm danh sách đề nghị thay đổi thông tin hội viên theo điều kiện lọc kết hợp thông tin hội viên.

#### Tham số request (Crd_MemberChangeInfoSearchReq)

| Tên tham số | Kiểu dữ liệu | Bắt buộc | Mặc định | Ý nghĩa |
| :--- | :--- | :--- | :--- | :--- |
| DLCPCode | string? | Không | null | Mã đại lý đề nghị lọc trên Crd_MemberChangeInfo.DLCodeRequest (Gate:33) |
| MemberNo | string? | Không | null | Mã hội viên lọc trên Crd_MemberChangeInfo.MemberNo (Gate:33) |
| CreateDTimeUTCFrom | string? | Không | null | Thời gian tạo từ ngày (định dạng yyyy-MM-dd) (Gate:33) |
| CreateDTimeUTCTo | string? | Không | null | Thời gian tạo đến ngày (định dạng yyyy-MM-dd) (Gate:33) |
| RequestNo | string? | Không | null | Số đề nghị lọc trên Crd_MemberChangeInfo.RequestNo (Gate:33) |
| RequestStatus | string? | Không | null | Danh sách trạng thái cách nhau bằng dấu phẩy (Gate:33) |
| CarNo | string? | Không | null | Biển số xe lọc trên Crd_Member.CarNo (Gate:33) |
| FinishDTimeUTCFrom | string? | Không | null | Thời gian hoàn tất từ ngày (định dạng yyyy-MM-dd) (Gate:33) |
| FinishDTimeUTCTo | string? | Không | null | Thời gian hoàn tất đến ngày (định dạng yyyy-MM-dd) (Gate:33) |
| OrgID | string? | Không | null | Mã tổ chức (tham số mở rộng, không tham gia lọc) (Gate:33) |

#### Quy tắc xử lý theo thứ tự
1. Khởi tạo đối tượng request mặc định nếu payload gửi lên rỗng hoặc null (Controllers/Port/Crd_MemberChangeInfoControllerPort.cs:51).
2. Chuẩn hoá các chuỗi mốc thời gian lọc (Gate:54-69, Controllers/Port/Crd_MemberChangeInfoControllerPort.cs:54-76):
    - CreateDTimeUTCFrom: nối thêm chuỗi " 00:00:00" nếu có giá trị.
    - CreateDTimeUTCTo: nối thêm chuỗi " 23:59:59" nếu có giá trị.
    - FinishDTimeUTCFrom: nối thêm chuỗi " 00:00:00" nếu có giá trị.
    - FinishDTimeUTCTo: nối thêm chuỗi " 23:59:59" nếu có giá trị.
3. Thực hiện INNER JOIN giữa Crd_MemberChangeInfo và Crd_Member theo khóa MemberNo (Card.cs:28051-28066, Controllers/Port/Crd_MemberChangeInfoControllerPort.cs:81-83). Các bản ghi đề nghị có MemberNo không tồn tại trong Crd_Member sẽ bị loại bỏ.
4. Áp dụng các điều kiện lọc (WhereClause theo Gate:974-1025, Controllers/Port/Crd_MemberChangeInfoControllerPort.cs:87-142):
    - DLCPCode: so khớp bằng chính xác không phân biệt hoa thường với cmci.DLCodeRequest qua PortQuery.Equal.
    - MemberNo: so khớp bằng chính xác không phân biệt hoa thường với cmci.MemberNo qua PortQuery.Equal.
    - RequestNo: so khớp bằng chính xác không phân biệt hoa thường với cmci.RequestNo qua PortQuery.Equal.
    - CreateDTimeUTCFrom: cmci.CreateDTimeUTC >= createFrom (so sánh chuỗi).
    - CreateDTimeUTCTo: cmci.CreateDTimeUTC <= createTo (so sánh chuỗi).
    - FinishDTimeUTCFrom: cmci.FinishDTimeUTC >= finishFrom (so sánh chuỗi).
    - FinishDTimeUTCTo: cmci.FinishDTimeUTC <= finishTo (so sánh chuỗi).
    - CarNo: so khớp bằng chính xác không phân biệt hoa thường với cm.CarNo của bảng hội viên qua PortQuery.Equal.
    - RequestStatus: tách chuỗi theo dấu phẩy, đưa về chữ thường và kiểm tra toán tử IN trên cmci.RequestStatus.
    - OrgID: không tham gia vào mệnh đề lọc.
5. Sắp xếp danh sách kết quả theo cmci.LogLUDTimeUTC tăng dần (asc) (Card.cs:28065, Controllers/Port/Crd_MemberChangeInfoControllerPort.cs:147).
6. Ánh xạ dữ liệu và tính toán các cờ hiển thị (Card.cs:28100-28164, Controllers/Port/Crd_MemberChangeInfoControllerPort.cs:151-209):
    - Xác định mã đại lý người dùng userDLCPCode: lấy req.DLCPCode viết hoa, mặc định là "HTV" nếu rỗng.
    - ApproveDLCode: gắn thêm tiền tố "HTV," vào giá trị gốc cmci.ApproveDLCode ("HTV," + cmci.ApproveDLCode).
    - FlagApproveAF: gán "1" nếu RequestStatus là "PENDING" hoặc "APPROVE", đồng thời userDLCPCode là "HTV" hoặc trùng với cmci.ApproveDLCode; ngược lại gán "0".
    - Sao chép các trường mở rộng từ Crd_Member: cm_MemberName, cm_CarNo, cm_PhoneNo, cm_VIN, cm_AmountEnd, cm_CardTypeUse.
7. Chuyển đổi múi giờ sang giờ địa phương (+7 tiếng, định dạng yyyy-MM-dd HH:mm:ss) cho 4 trường (Gate:105-125, Controllers/Port/Crd_MemberChangeInfoControllerPort.cs:202-205, 222-230):
    - LUDTimeUTC
    - ApproveDTimeUTC
    - FinishDTimeUTC
    - CreateDTimeUTC
8. Đóng gói kết quả: Trả về HTTP 200 kèm payload Success = true và Data = { Lst_Crd_MemberChangeInfo = resultList }. Nếu danh sách kết quả rỗng, thuộc tính Lst_Crd_MemberChangeInfo được gán giá trị null (Gate:70, 101-104, Controllers/Port/Crd_MemberChangeInfoControllerPort.cs:211-219).

#### Bảng mã lỗi
Endpoint không ném mã lỗi ErridnLoyalty.* trong luồng xử lý chuẩn; dữ liệu luôn trả về HTTP 200 kèm cờ Success = true.

| Mã lỗi | Điều kiện phát sinh |
| :--- | :--- |
| Không có | Controller không định nghĩa mã ErridnLoyalty.* cụ thể |

#### Ví dụ Request
Tìm kiếm đề nghị theo mã đại lý và khoảng trạng thái:

    POST /api/crd_memberchangeinfo/search
    Content-Type: application/json

    {
        "DLCPCode": "VN040",
        "MemberNo": "MB001",
        "CreateDTimeUTCFrom": "2026-08-01",
        "CreateDTimeUTCTo": "2026-08-31",
        "RequestNo": null,
        "RequestStatus": "PENDING,APPROVE",
        "CarNo": null,
        "FinishDTimeUTCFrom": null,
        "FinishDTimeUTCTo": null,
        "OrgID": null
    }

Tìm kiếm toàn bộ không điều kiện:

    POST /api/crd_memberchangeinfo/search
    Content-Type: application/json

    {}

#### Ví dụ Response
Chưa có lần chạy mẫu

#### Hành vi đặc biệt
- Khi danh sách kết quả rỗng (không tìm thấy bản ghi thỏa mãn), thuộc tính Data.Lst_Crd_MemberChangeInfo trả về giá trị null thay vì mảng rỗng [] (Gate:70, Controllers/Port/Crd_MemberChangeInfoControllerPort.cs:217).
- Cơ chế INNER JOIN với Crd_Member khiến những bản ghi Crd_MemberChangeInfo có MemberNo không tồn tại trong danh mục hội viên sẽ tự động bị loại khỏi danh sách kết quả (Card.cs:28051-28066, Controllers/Port/Crd_MemberChangeInfoControllerPort.cs:81-83).
- Bốn trường thời gian CreateDTimeUTC, LUDTimeUTC, ApproveDTimeUTC, FinishDTimeUTC được tự động cộng thêm 7 giờ (UTC sang giờ VN) và định dạng chuẩn chuỗi yyyy-MM-dd HH:mm:ss trước khi trả về (Gate:105-125, Controllers/Port/Crd_MemberChangeInfoControllerPort.cs:202-205).
- Cột ApproveDLCode luôn được nối thêm tiền tố "HTV," ở đầu chuỗi (Card.cs:28138, Controllers/Port/Crd_MemberChangeInfoControllerPort.cs:161).

## 3. Khác biệt so với nguồn & GAP

- Tham số OrgID: Nguồn gate nhận OrgID từ session/header nhưng không tham gia lọc trong mệnh đề WHERE của thủ tục SQL cũ; controller port giữ nguyên tính tương thích không lọc trường này.
- Phân quyền người dùng: Nguồn cũ đọc thông tin đơn vị của user từ token đăng nhập để tính cờ FlagApproveAF; controller port suy luận đơn vị từ tham số DLCPCode truyền vào (hoặc mặc định là HTV).
- Nhật ký hệ thống: Controller port chưa tích hợp hạ tầng ghi log cuộc gọi API như bản gốc idn.Loyalty.

## 4. Nhật ký thay đổi

- Hạng mục #027 (commit 5b0e111): Port 1:1 action Search của Crd_MemberChangeInfoController, mô hình dữ liệu Crd_MemberChangeInfo và seeder khởi tạo dữ liệu mẫu kiểm chứng INNER JOIN.
