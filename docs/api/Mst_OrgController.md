# Phạm vi: Mst_OrgControllerPort (Quản lý Tổ chức)
# Cập nhật: 2026-10-16
# Phiên bản mã: 7973495
# Trạng thái: Nháp — chờ review

## 1. Tóm tắt
Mục đích nghiệp vụ của controller này là quản lý và tra cứu thông tin tổ chức (`Mst_Org`), kết hợp liên kết thông tin người nộp thuế từ bảng `Mst_NNT`. Hỗ trợ các chức năng tìm kiếm phân trang theo từ khóa, lọc theo trạng thái hoạt động và lấy toàn bộ danh sách các tổ chức đang hoạt động.
- **Bảng nguồn (File gate / API / Biz)**: `Mst_OrgController.cs` (Controllers/Port/Mst_OrgControllerPort.cs)
- **Bảng CSDL (EF Core)**: `Mst_Org`, `Mst_NNT`

### Bảng cột thực thể (`Mst_Org` và dữ liệu trả về):
| Tên cột | Kiểu dữ liệu EF | Khóa / Khác | Ghi chú |
| --- | --- | --- | --- |
| OrgID | `string` | Khóa chính (PK) | Mã tổ chức |
| NetworkID | `string?` | - | Mã mạng lưới |
| OrgParent | `string?` | - | Mã tổ chức cha |
| OrgBUCode | `string?` | - | Mã đơn vị kinh doanh BU |
| OrgBUPattern | `string?` | - | Mẫu đơn vị kinh doanh BU |
| OrgLevel | `string?` | - | Cấp bậc tổ chức |
| Remark | `string?` | - | Ghi chú |
| FlagActive | `string?` | - | Trạng thái kích hoạt ('1' = hoạt động, '0' = tạm ngừng) |
| LogLUDTimeUTC | `DateTime?` | - | Thời gian cập nhật cuối cùng (UTC) |
| LogLUBy | `string?` | - | Người thực hiện cập nhật cuối |
| MST | `string?` | - | Mã số thuế liên kết |
| mnnt_NNTFullName | `string?` | Kết hợp (Join) | Tên người nộp thuế từ bảng `Mst_NNT` |

---

## 2. Các Endpoint

### 2.1. POST /api/mst_org/search
- **Mô tả**: Tìm kiếm và phân trang danh sách tổ chức theo từ khóa và trạng thái hoạt động.
- **Request Body (`Mst_OrgSearchRequest`)**:
  | Tên tham số | Kiểu dữ liệu | Bắt buộc? | Mặc định | Ý nghĩa |
  | --- | --- | --- | --- | --- |
  | OrgID | `string?` | Không | `null` | Mã tổ chức (không sử dụng làm điều kiện lọc theo mã nguồn gốc) |
  | KeyWord | `string?` | Không | `null` | Từ khóa tìm kiếm (lọc theo `OrgID`) |
  | FlagActive | `string?` | Không | `null` | Lọc theo trạng thái kích hoạt |
  | Ft_PageIndex | `int` | Không | `0` | Số thứ tự trang (bắt đầu từ 0) |
  | Ft_PageSize | `int` | Không | `10` | Kích thước trang |

- **Quy tắc xử lý**:
  1. Xây dựng truy vấn lọc trên bảng `Mst_Org` với `KeyWord` (toán tử `like` trên `OrgID`) và `FlagActive`.
  2. Thực hiện đếm tổng số bản ghi (`itemCount`) trên bảng draft trước khi phân trang và join.
  3. Phân trang và thực hiện `inner join` với bảng `Mst_NNT` theo `OrgID` để lấy tên người nộp thuế (`mnnt_NNTFullName`). Các tổ chức không có bản ghi tương ứng trong `Mst_NNT` sẽ bị loại khỏi kết quả trả về.
  4. Tính toán `PageInfo` và trả về kết quả cấu trúc phân trang.
- **Mã lỗi (`ErridnLoyalty.*`)**: Chưa xác minh.
- **Ví dụ Request**:
    ```json
    {
      "KeyWord": "402032",
      "FlagActive": "1",
      "Ft_PageIndex": 0,
      "Ft_PageSize": 10
    }
    ```
- **Ví dụ Response**: Chưa có lần chạy mau

---

### 2.2. POST /api/mst_org/getallactive
- **Mô tả**: Lấy toàn bộ danh sách các tổ chức đang hoạt động (`FlagActive = "1"`), không phân trang.
- **Request Body**: Không yêu cầu (hoặc rỗng).
- **Quy tắc xử lý**:
  1. Gọi hàm lõi `GetXAsync` với điều kiện lọc cố định `FlagActive = "1"` và giới hạn phân trang mặc định lớn (`recordStart = 0`, `recordCount = 123456000`).
  2. Nếu danh sách kết quả không rỗng, trả về mảng dữ liệu; ngược lại nếu rỗng, trả về `null`.
- **Mã lỗi**: Chưa xác minh.
- **Ví dụ Request**: Trống (`{}`) hoặc không cần body.
- **Ví dụ Response**: Chưa có lần chạy mau

---

## 3. Khác biệt so với nguồn & GAP
- **Phân quyền và xác thực**: Các cơ chế kiểm tra quyền truy cập (`CheckGatewayAuthentication`, `CheckAuthorize`, `Sys_User_CheckAuthorize`, `Sys_Access_CheckDenyV30`) từ mã nguồn gốc đã được lược bỏ do hệ thống đích chưa có hệ thống phân quyền tài khoản.
- **Log hệ thống**: Các thao tác ghi nhật ký `RequestLog`/`ReturnLog` ở tầng biz nguồn không được thực hiện tại controller port.

---

## 4. Nhật ký thay đổi
- Port 1-1 các chức năng tìm kiếm phân trang (`Search`) và lấy danh sách hoạt động (`GetAllActive`) từ mã nguồn gốc sang `Mst_OrgControllerPort.cs`.
