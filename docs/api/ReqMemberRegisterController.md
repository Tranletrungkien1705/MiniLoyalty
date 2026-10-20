---
Phạm vi: Controllers/Port/ReqMemberRegisterControllerPort.cs, Models/Port/Req_MemberRegister.cs
Cập nhật: 2026-10-18
Phiên bản mã: d30cdab
Trạng thái: Nháp — chờ review
---

# Tài liệu API: ReqMemberRegisterController

## 1. Tóm tắt

API quản lý yêu cầu đăng ký hội viên (`Req_MemberRegister`), thực hiện các nghiệp vụ tra cứu theo mã hệ thống hoặc mã yêu cầu, phân trang danh sách đăng ký, xuất Excel, cùng các thao tác trạng thái như Phê duyệt (`Approve`, `ApproveOld`), Hủy (`Cancel`) và Từ chối (`Reject`).

- **Bảng nguồn**: 
  - Gate: `ReqMemberRegisterController.cs`
  - API: `11.BackEnd\V10\idn.Loyalty.Biz.Web\Controllers\ReqMemberRegisterController.cs`
  - Biz: `11.BackEnd\V10\idn.Loyalty.Biz\Member.cs`
- **Bảng cơ sở dữ liệu**:
  - `Req_MemberRegister` (Thông tin đăng ký hội viên):
    - `ReqMemberRegisterCodeSys`: Kiểu `string` (EF), Khóa chính, Mã hệ thống lượt đăng ký.
    - `ReqMemberRegisterCode`: Kiểu `string?` (EF), Mã lượt đăng ký.
    - `DLCodeRegis`: Kiểu `string?` (EF), Đại lý đăng ký.
    - `RegisterDate`: Kiểu `string?` (EF), Ngày hội viên đăng ký.
    - `ReqMemberRegisterStatus`: Kiểu `string?` (EF), Trạng thái yêu cầu (`PENDING`, `APPROVE`, `CANCEL`, `REJECT`).
    - `CarNo`: Kiểu `string?` (EF), Biển số xe.
    - `VIN`: Kiểu `string?` (EF), Số khung.
    - `CustomerName`: Kiểu `string?` (EF), Tên khách hàng.
    - `CustomerPhoneNo`: Kiểu `string?` (EF), Số điện thoại khách hàng.
    - `ProvinceCode`, `DistrictCode`: Kiểu `string?` (EF), Mã tỉnh/thành và quận/huyện.
    - `ApproveDTimeUTC`, `CreateDTimeUTC`, `CancelDTimeUTC`, `RejectDTimeUTC`, `LUDTimeUTC`, `LogLUDTimeUTC`: Kiểu `string?` / `DateTime?` (EF), Các mốc thời gian hệ thống.
    - `ApproveBy`, `CreateBy`, `CancelBy`, `RejectBy`, `LUBy`, `LogLUBy`: Kiểu `string?` (EF), Thông tin người thực hiện.
    - `Remark`: Kiểu `string?` (EF), Ghi chú.
  - Các bảng đính kèm (`Req_MemberRegisterIDAttachFile`, `Req_MemberRegisterVehicleAttachFile`, `Req_MemberRegisterAttachFile`): Lưu trữ file chứng từ tùy thân, xe và file chung liên kết qua `ReqMemberRegisterCodeSys`.

---

## 2. Các Endpoint

### 2.1. GetByReqMemberRegisterCodeSys (Lấy chi tiết theo mã hệ thống)
- **Route & Method**: `POST /api/reqmemberregister/getbyreqmemberregistercodesys` (Hỗ trợ alias `/api/req_memberregister/getbyreqmemberregistercodesys`)
- **Mã nguồn đích**: `Controllers/Port/ReqMemberRegisterControllerPort.cs:72`

**Bảng tham số Request (`Req_MemberRegister_GetByReqMemberRegisterCodeSysReq`):**
| Tên tham số | Kiểu dữ liệu | Bắt buộc? | Mặc định | Ý nghĩa |
| :--- | :--- | :--- | :--- | :--- |
| `ReqMemberRegisterCodeSys` | string? | Có | null | Mã hệ thống lượt đăng ký |

**Quy tắc xử lý:**
1. Lấy thông tin `utcOffset` từ header `TimeZone` (mặc định là `7`).
2. Kiểm tra nếu `ReqMemberRegisterCodeSys` rỗng hoặc null, trả về cấu trúc rỗng với `Success = true`.
3. Truy vấn bản ghi `Req_MemberRegister` bằng toán tử `=` (không phân biệt hoa thường).
4. Bổ sung thông tin (`EnrichAsync`) từ các bảng tỉnh/thành (`Mst_Province`), quận/huyện (`Mst_District`), đại lý (`Mst_DealerCP`), hội viên (`CrdMember`), chuyển đổi thời gian sang giờ địa phương.
5. Truy vấn và đính kèm các danh sách file liên quan (`IDAttachFile`, `VehicleAttachFile`, `AttachFile`).
6. Trả về đối tượng kết quả.

**Mã lỗi:**
Chưa xac minh

**Ví dụ Request:**
```json
{
  "ReqMemberRegisterCodeSys": "RMR001"
}
 {
  "ReqMemberRegisterCode": "CODE001"
}
 {
  "ReqMemberRegisterStatus": "PENDING",
  "Ft_PageIndex": 0,
  "Ft_PageSize": 10
}
 {
  "ReqMemberRegisterStatus": "PENDING"
}
 {
  "Req_MemberRegister": {
    "ReqMemberRegisterCodeSys": "RMR001",
    "Remark": "Phê duyệt đăng ký"
  }
}
 {
  "ReqMemberRegisterCodeSys": "RMR001",
  "Remark": "Phê duyệt nhanh"
}
 {
  "Req_MemberRegister": {
    "ReqMemberRegisterCodeSys": "RMR001",
    "Remark": "Hủy yêu cầu do khách hàng đổi ý"
  }
}
 {
  "Req_MemberRegister": {
    "ReqMemberRegisterCodeSys": "RMR001",
    "Remark": "Từ chối sau kiểm tra hồ sơ"
  }
}
