---
Phạm vi: Controllers/Port/Mst_PersonalBackgroundControllerPort.cs, Models/Port/Mst_PersonalBackground.cs
Cập nhật: 2026-10-18
Phiên bản mã: d30cdab
Trạng thái: Nháp — chờ review
---

# Tài liệu API: Mst_PersonalBackgroundController

## 1. Tóm tắt

API quản lý danh mục thành phần bản thân (`Mst_PersonalBackground`), phục vụ tra cứu phân trang và lấy toàn bộ danh sách thành phần bản thân đang hoạt động trong hệ thống MiniLoyalty.

- **Bảng nguồn**: 
  - Gate: `13.ClientGate\V10\idn.Loyalty.WebAPI\Controllers\Mst_PersonalBackgroundController.cs:25-167`
  - API: `11.BackEnd\V10\idn.Loyalty.Biz.Web\Controllers\MstPersonalBackgroundController.cs:17-88`
  - Biz: `11.BackEnd\V10\idn.Loyalty.Biz\MasterData.cs:44925-45302`
- **Bảng cơ sở dữ liệu (`Mst_PersonalBackground`)**:
  - `PersonalBGCode`: Kiểu `string` (EF), Khóa chính (Khoảng dữ liệu: không có DDL — kiểu cột cần xác确认).
  - `NetworkID`: Kiểu `string?` (EF), Mã mạng.
  - `PersonalBGName`: Kiểu `string?` (EF), Tên thành phần bản thân.
  - `PersonalBGDesc`: Kiểu `string?` (EF), Mô tả thành phần bản thân.
  - `FlagActive`: Kiểu `string?` (EF), Trạng thái hoạt động ('1' = hoạt động, '0' = ngừng).
  - `LogLUDTimeUTC`: Kiểu `DateTime?` (EF), Thời điểm cập nhật UTC.
  - `LogLUBy`: Kiểu `string?` (EF), Người cập nhật.

---

## 2. Các Endpoint

### 2.1. Search (Tra cứu phân trang danh mục thành phần bản thân)
- **Route & Method**: `POST /api/mst_personalbackground/search`
- **Mã nguồn đích**: `Controllers/Port/Mst_PersonalBackgroundControllerPort.cs:48`

**Bảng tham số Request (`MstPersonalBackgroundSearchReq`):**
| Tên tham số | Kiểu dữ liệu | Bắt buộc? | Mặc định | Ý nghĩa |
| :--- | :--- | :--- | :--- | :--- |
| `OrgID` | string? | Không | null | Mã tổ chức (gate nhận nhưng không lọc theo logic nguồn) |
| `KeyWord` | string? | Không | null | Từ khóa tìm kiếm theo tên thành phần bản thân (`PersonalBGName`) |
| `FlagActive` | string? | Không | null | Lọc theo trạng thái hoạt động (toán tử `=`) |
| `Ft_PageIndex` | int | Không | 0 | Chỉ số trang (0-based) |
| `Ft_PageSize` | int | Không | 10 | Kích thước trang |

**Quy tắc xử lý:**
1. Nhận tham số qua body JSON, nếu null khởi tạo mặc định.
2. Tính toán phân trang: `start = pageIndex * pageSize`, `count = pageSize`.
3. Lọc dữ liệu:
   - `KeyWord`: Tìm kiếm không phân biệt hoa/thường theo dạng `like '%KeyWord%'` trên cột `PersonalBGName`.
   - `FlagActive`: So sánh bằng chính xác (`=`) với giá trị truyền vào trên cột `FlagActive`.
4. Sắp xếp kết quả theo `PersonalBGCode asc`, thực hiện đếm tổng bản ghi (`itemCount`).
5. Cắt trang theo giới hạn `Skip` và `Take`.
6. Trả về đối tượng `PageInfo<object>` bọc trong `Success = true, Data = ...`.

**Mã lỗi:**
Chưa xac minh

**Ví dụ Request:**
```json
{
  "OrgID": "4020320001",
  "KeyWord": "Nong dan",
  "FlagActive": "1",
  "Ft_PageIndex": 0,
  "Ft_PageSize": 10
}
 {}
