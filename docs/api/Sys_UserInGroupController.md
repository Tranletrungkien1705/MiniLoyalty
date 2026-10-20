Phạm vi: Controllers/Port/Sys_UserInGroupControllerPort.cs
Cập nhật: 2026-10-16
Phiên bản mã: c2cccaf
Trạng thái: Nháp — chờ review

# Sys_UserInGroupController API

## 1. Tóm tắt
- **Mục đích nghiệp vụ:** Quản lý việc phân bổ danh sách người dùng (`Sys_UserInGroup`) vào một nhóm người dùng (`Sys_Group`) theo cơ chế xóa toàn bộ danh sách cũ của nhóm và ghi nhận lại danh sách mới được cung cấp trong payload.
- **Bảng nguồn:** Gate `Sys_UserInGroupController.cs:27-112`, Biz `WA_Sys_UserInGroup_Save`, `Sys_UserInGroup_Save_New20191102` (`zTemp.cs:1255-1450`).
- **Bảng cấu trúc/thực thể liên quan:**
  - `Sys_Group`: Quản lý thông tin nhóm (`GroupCode`, `MST`).
  - `Sys_UserInGroup`: Bảng trung gian liên kết nhóm và người dùng (`GroupCode`, `UserCode`, `NetworkID`, `MST`, `LogLUDTimeUTC`, `LogLUBy`).

## 2. Các Endpoint

### POST /api/sys_useringroup/save
Lưu danh sách người dùng thuộc nhóm (xóa danh sách cũ và thêm mới).

#### Tham số Request
Body kiểu JSON (`SysUserInGroupSaveReq`):
- `Sys_Group` (object, không bắt buộc): Đối tượng tham chiếu nhóm.
  - `GroupCode` (string, bắt buộc): Mã nhóm cần cập nhật người dùng.
- `Lst_Sys_UserInGroup` (array, bắt buộc): Danh sách chi tiết người dùng thuộc nhóm.
  - `UserCode` (string, bắt buộc): Mã người dùng.

#### Quy tắc xử lý theo thứ tự
1. Kiểm tra nếu `req` rỗng (`null`), trả về lỗi xác thực với mã `Sys_UserInGroup_Save` và thông báo `Check.strJson`.
2. Bắt đầu transaction cơ sở dữ liệu (`BeginTransactionAsync`).
3. Chuẩn hóa `GroupCode` (trim, viết hoa), kiểm tra sự tồn tại trong bảng `Sys_Group`. Nếu không tìm thấy, rollback transaction và trả về lỗi `Sys_Group_CheckDB_GroupCodeNotFound` (`Check.GroupCodeNotFound`).
4. Kiểm tra `Lst_Sys_UserInGroup` không được để trống (`null`). Nếu rỗng, rollback transaction và trả về lỗi `Sys_UserInGroup_Save_InputTblDtlNotFound` (`Check.TableName`, `Sys_UserInGroup`).
5. Trích xuất danh sách `UserCode` từ yêu cầu.
6. Xóa toàn bộ các bản ghi `Sys_UserInGroup` hiện có gắn với `GroupCode` của nhóm.
7. Lấy `NetworkID` từ cấu hình hệ thống (mặc định là `1001` nếu không cấu hình) và thời gian hiện tại (`DateTime.UtcNow`).
8. Thêm mới từng bản ghi `Sys_UserInGroup` vào cơ sở dữ liệu với `LogLUBy = "port"`.
9. Commit transaction và trả về kết quả thành công. Nếu xảy ra ngoại lệ, rollback và trả về lỗi `Sys_UserInGroup_Save`.

#### Bảng Mã Lỗi
| Mã lỗi | Điều kiện phát sinh | Chi tiết / Tham số trả về |
| :--- | :--- | :--- |
| `Sys_UserInGroup_Save` | Payload truyền lên là `null` hoặc xảy ra ngoại lệ bất kỳ trong khối `try-catch`. | `Check.strJson` hoặc `Exception` |
| `Sys_Group_CheckDB_GroupCodeNotFound` | Không tìm thấy mã nhóm (`GroupCode`) trong cơ sở dữ liệu. | `Check.GroupCodeNotFound` |
| `Sys_UserInGroup_Save_InputTblDtlNotFound` | Thuộc tính `Lst_Sys_UserInGroup` trong request bị thiếu (`null`). | `Check.TableName`, `Sys_UserInGroup` |

#### Ví dụ Request / Response
- **Request:**
    POST /api/sys_useringroup/save
    Content-Type: application/json

    {
      "Sys_Group": {
        "GroupCode": "ADMIN_GROUP"
      },
      "Lst_Sys_UserInGroup": [
        {
          "UserCode": "USER01"
        },
        {
          "UserCode": "USER02"
        }
      ]
    }

- **Response (Thành công):**
    Chưa có lần chạy mau

- **Response (Lỗi - Không tìm thấy nhóm):**
    Chưa có lần chạy mau

## 3. Khác biệt so với nguồn & GAP
- Nguồn gốc (`Sys_User_CheckDB(strWAUserCode)` tại dòng `:1337-1347`) gán `t_sg.MST` theo MST của người dùng đang đăng nhập (session). Ở bản port sang MiniLoyalty chưa có session, code hiện tại tạm dùng MST của nhóm (`dbGroup.MST`). Đây là một GAP cần lưu ý về phân quyền/kiểm tra phiên đăng nhập.

## 4. Nhật ký thay đổi
- Hạng mục `#337` — Port 1:1 `Sys_UserInGroupController.Save` sang `Sys_UserInGroupControllerPort.cs`.

TOM TAT
FILE_DA_TAO_SUA: docs/api/Sys_UserInGroupController.md
PHAM_VI_PHU: Sys_UserInGroupControllerPort (1 endpoint: POST /api/sys_useringroup/save)
KHONG_XAC_MINH: Ví dụ response thực tế (Chưa có lần chạy mau do môi trường web không thực thi lệnh/chạy app trực tiếp).
LENH_DA_CHAY: Không chạy lệnh (Theo quy định môi trường WEB: KHÔNG chạy lệnh).
VI_PHAM_QUY_TAC: Khong
