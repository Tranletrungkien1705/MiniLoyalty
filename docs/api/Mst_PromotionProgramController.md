---
Phạm vi: Controllers/Port/Mst_PromotionProgramControllerPort.cs, Models/Port/Mst_PromotionProgram.cs
Cập nhật: 2026-10-18
Phiên bản mã: d30cdab
Trạng thái: Nháp — chờ review
---

# Tài liệu API: Mst_PromotionProgramController

## 1. Tóm tắt

API quản lý chương trình khuyến mại (`Mst_PromotionProgram`), hỗ trợ các nghiệp vụ tra cứu phân trang, lấy chi tiết, tạo mới/cập nhật (Save/Update), xóa, xuất danh sách ra file Excel và tải file mẫu import.

- **Bảng nguồn**: 
  - Gate: `13.ClientGate\V10\idn.Loyalty.WebAPI\Controllers\Mst_PromotionProgramController.cs:32-743`
  - API: `11.BackEnd\V10\idn.Loyalty.Biz.Web\Controllers\MstPromotionProgramController.cs:22-163`
  - Biz: `11.BackEnd\V10\idn.Loyalty.Biz\Master.Loyalty.cs:13179-14543`
  - DDL: `11.BackEnd\V10\05.Refs.Biz\Migrate\20221006.QUYNHNTN\20221026.CreateDatabase.sql:444-509`
- **Bảng cơ sở dữ liệu**:
  - `Mst_PromotionProgram` (Header):
    - `PrProgramCode`: Kiểu `string` (EF), Khóa chính, Mã chương trình khuyến mãi.
    - `NetworkID`: Kiểu `string` (EF), Mã mạng lưới.
    - `OrgID`: Kiểu `string?` (EF), Mã tổ chức.
    - `PrProgramName`: Kiểu `string` (EF), Tên chương trình khuyến mãi.
    - `EffDateStart`, `EffDateEnd`: Kiểu `DateTime` (EF), Ngày bắt đầu và ngày kết thúc hiệu lực.
    - `FlagAllDL`: Kiểu `string?` (EF), Cờ áp dụng tất cả đại lý ('1'=Tất cả, '0'=Theo Spec).
    - `FlagActive`: Kiểu `string?` (EF), Trạng thái hoạt động ('1'=Kích hoạt, '0'=Không kích hoạt).
    - `Remark`: Kiểu `string?` (EF), Ghi chú.
    - `LogLUDTimeUTC`: Kiểu `DateTime?` (EF), Thời điểm cập nhật cuối UTC.
    - `LogLUBy`: Kiểu `string?` (EF), Người cập nhật cuối.
  - `Mst_PromotionProgramDtl` (Chi tiết hạng thẻ):
    - `PrProgramCode`, `CardType`: Kiểu `string` (EF), Khóa chính composite (Mã chương trình, Hạng thẻ).
    - `NetworkID`: Kiểu `string?` (EF), Mã mạng lưới.
    - `OrgID`: Kiểu `string?` (EF), Mã tổ chức.
    - `Qty`: Kiểu `double?` (EF), Số lượng.
    - `Unit`: Kiểu `string?` (EF), Đơn vị tính.
    - `FlagActive`: Kiểu `string?` (EF), Trạng thái hoạt động.
    - `Remark`: Kiểu `string?` (EF), Ghi chú.
    - `LogLUDTimeUTC`, `LogLUBy`: Kiểu `DateTime?`, `string?` (EF), Thông tin log cập nhật.
  - `Mst_PromotionProgramSpec` (Chỉ định đại lý áp dụng):
    - `DLCode`, `PrProgramCode`: Kiểu `string` (EF), Khóa chính composite (Mã đại lý, Mã chương trình).
    - `NetworkID`: Kiểu `string?` (EF), Mã mạng lưới.
    - `OrgID`: Kiểu `string?` (EF), Mã tổ chức.
    - `FlagActive`: Kiểu `string?` (EF), Trạng thái hoạt động (mặc định "PENDING" khi lưu).
    - `Remark`: Kiểu `string?` (EF), Ghi chú.
    - `LogLUDTimeUTC`, `LogLUBy`: Kiểu `DateTime?`, `string?` (EF), Thông tin log cập nhật.

---

## 2. Các Endpoint

### 2.1. Search (Tra cứu phân trang chương trình khuyến mại)
- **Route & Method**: `POST /api/mst_promotionprogram/search`
- **Mã nguồn đích**: `Controllers/Port/Mst_PromotionProgramControllerPort.cs:134`

**Bảng tham số Request (`MstPromotionProgramSearchReq`):**
| Tên tham số | Kiểu dữ liệu | Bắt buộc? | Mặc định | Ý nghĩa |
| :--- | :--- | :--- | :--- | :--- |
| `OrgID` | string? | Không | null | Mã tổ chức (không sử dụng trong điều kiện lọc biz gốc) |
| `KeyWord` | string? | Không | null | Từ khóa tìm kiếm theo mã chương trình (`PrProgramCode like '%kw%'`) |
| `Ft_PageIndex` | int | Không | 0 | Chỉ số trang (0-based) |
| `Ft_PageSize` | int | Không | 0 | Kích thước trang |

**Quy tắc xử lý:**
1. Nhận tham số qua body JSON, tính toán phân trang `start = pageIndex * pageSize`, `count = pageSize`.
2. Lọc danh sách bản ghi theo từ khóa `KeyWord` trên `PrProgramCode`.
3. Sắp xếp theo `LogLUDTimeUTC desc`, thực hiện đếm tổng số bản ghi (`itemCount`) và cắt trang.
4. Tính toán giá trị `ListDealer` dựa trên cấu hình `FlagAllDL` ('1' = "Tất cả", '0' = danh sách `DLCode` nối nhau từ bảng Spec, khác = "").
5. Trả về đối tượng phân trang bọc trong `PageInfo<object>`.

**Mã lỗi:**
Chưa xac minh

**Ví dụ Request:**
```json
{
  "OrgID": "",
  "KeyWord": "PR",
  "Ft_PageIndex": 0,
  "Ft_PageSize": 10
}
 {}
 {
  "PrProgramCode": "PR01"
}
 {
  "Mst_PromotionProgram": {
    "PrProgramCode": "PR10",
    "PrProgramName": "Khuyến mãi đặc biệt",
    "EffDateStart": "2026-11-01 00:00:00",
    "EffDateEnd": "2026-11-30 23:59:59",
    "FlagAllDL": "1",
    "Remark": "Chương trình mới"
  },
  "Lst_Mst_PromotionProgramDtl": [
    {
      "CardType": "N",
      "Qty": 2.0,
      "Unit": "Lần",
      "FlagActive": "1"
    }
  ],
  "Lst_Mst_PromotionProgramSpec": []
}
 {
  "Mst_PromotionProgram": {
    "PrProgramCode": "PR10",
    "PrProgramName": "Khuyến mãi đặc biệt - Đã cập nhật",
    "EffDateStart": "2026-11-01 00:00:00",
    "EffDateEnd": "2026-11-30 23:59:59",
    "FlagAllDL": "1",
    "FlagActive": "0",
    "Remark": "Đã cập nhật"
  }
}
 {
  "PrProgramCode": "PR03"
}
 {
  "KeyWord": "PR"
}
 {}
