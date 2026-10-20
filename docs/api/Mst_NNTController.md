---
Phạm vi: Controllers/Port/Mst_NNTControllerPort.cs, Models/Port/Mst_NNT.cs
Cập nhật: 2026-10-18
Phiên bản mã: d30cdab
Trạng thái: Nháp — chờ review
---

# Tài liệu API: Mst_NNTController

## 1. Tóm tắt

API quản lý danh mục Người nộp thuế (`Mst_NNT`), thực hiện các nghiệp vụ tra cứu danh sách phân trang, lấy toàn bộ danh sách hoạt động, truy vấn theo mã số thuế, tạo mới, cập nhật và xóa người nộp thuế.

- **Bảng nguồn**: 
  - Gate: `13.ClientGate\V10\idn.Loyalty.WebAPI\Controllers\Mst_NNTController.cs:26-662`
  - API: `11.BackEnd\V10\idn.Loyalty.Biz.Web\Controllers\MstNNTController.cs:20-89, 604-673, 820-889, 1254-1324`
  - Biz: `11.BackEnd\V10\idn.Loyalty.Biz\Master.cs:7933-8295, 20940-21524, 25240-26885`, `Master.1.cs:3699-4545`
- **Bảng cơ sở dữ liệu (`Mst_NNT`)**:
  - `MST`: Kiểu `string` (EF), Khóa chính, Mã số thuế.
  - `OrgID`: Kiểu `string?` (EF), Mã tổ chức.
  - `NNTFullName`: Kiểu `string?` (EF), Tên doanh nghiệp / NNT.
  - `NNTShortName`: Kiểu `string?` (EF), Tên viết tắt doanh nghiệp.
  - `NetworkID`: Kiểu `string?` (EF), Mã mạng lưới.
  - `MSTParent`: Kiểu `string?` (EF), MST cha / đơn vị cấp trên.
  - `MSTBUCode`, `MSTBUPattern`, `MSTLevel`: Kiểu `string?` (EF), Thông tin BU và cấp bậc.
  - `ProvinceCode`, `DistrictCode`, `DLCode`: Kiểu `string?` (EF), Mã tỉnh/thành, quận/huyện, đại lý.
  - `NNTAddress`, `NNTMobile`, `NNTPhone`, `NNTFax`: Kiểu `string?` (EF), Địa chỉ và số liên lạc.
  - `PresentBy`, `BusinessRegNo`, `NNTPosition`, `PresentIDNo`, `PresentIDType`: Kiểu `string?` (EF), Thông tin đại diện pháp luật.
  - `GovTaxID`, `ContactName`, `ContactPhone`, `ContactEmail`, `Website`: Kiểu `string?` (EF), Cơ quan thuế, thông tin liên hệ và website.
  - `CANumber`, `CAOrg`, `CAEffDTimeUTCStart`, `CAEffDTimeUTCEnd`: Kiểu `string?` (EF), Thông tin chữ ký số CA.
  - `PackageCode`, `CreatedDate`, `AccNo`, `AccHolder`, `BankName`: Kiểu `string?` (EF), Gói dịch vụ, ngày tạo và tài khoản ngân hàng.
  - `BizType`, `BizFieldCode`, `BizSizeCode`, `DealerType`, `AreaCode`: Kiểu `string?` (EF), Loại hình, lĩnh vực, quy mô, loại đại lý và vùng.
  - `RegisterStatus`, `TCTStatus`, `FlagActive`: Kiểu `string?` (EF), Trạng thái đăng ký, trạng thái TCT và cờ hoạt động ('1'=hoạt động, '0'=ngừng).
  - `Remark`: Kiểu `string?` (EF), Ghi chú.
  - `LogLUDTimeUTC`: Kiểu `DateTime?` (EF), Thời gian cập nhật cuối UTC.
  - `LogLUBy`: Kiểu `string?` (EF), Người cập nhật cuối.

---

## 2. Các Endpoint

### 2.1. Search (Tra cứu phân trang)
- **Route & Method**: `POST /api/mst_nnt/search`
- **Mã nguồn đích**: `Controllers/Port/Mst_NNTControllerPort.cs:52`

**Bảng tham số Request (`Mst_NNTSearchRequest`):**
| Tên tham số | Kiểu dữ liệu | Bắt buộc? | Mặc định | Ý nghĩa |
| :--- | :--- | :--- | :--- | :--- |
| `OrgID` | string? | Không | null | Lọc theo mã tổ chức |
| `KeyWord` | string? | Không | null | Từ khóa tìm kiếm theo tên NNT (`NNTFullName`) |
| `FlagActive` | string? | Không | null | Lọc theo trạng thái hoạt động (phân tách bằng dấu phẩy) |
| `Ft_PageIndex` | int | Không | 0 | Chỉ số trang (0-based) |
| `Ft_PageSize` | int | Không | 10 | Kích thước trang |

**Quy tắc xử lý:**
1. Chuẩn hóa và áp dụng các điều kiện lọc (nếu có): `MST` (exact match), `OrgID` (exact match), `NNTFullName` (like `%KeyWord%`), `FlagActive` (IN clause theo danh sách truyền vào).
2. Thực hiện đếm tổng số bản ghi (`itemCount`) trên query nháp.
3. Phân trang bằng `Skip` và `Take` theo `Ft_PageIndex` và `Ft_PageSize`, sắp xếp theo `MST asc`.
4. Join lấy thông tin tỉnh/thành (`Mst_Province`) và quận/huyện (`Mst_District`) tương ứng.
5. Trả về kết quả bọc trong `PageInfo<Mst_NNTSearchResultItem>`.

**Mã lỗi (`ErridnLoyalty.*` + điều kiện):**
Chưa xac minh

**Ví dụ Request:**
```json
{
  "OrgID": "4020320001",
  "KeyWord": "Công ty",
  "FlagActive": "1",
  "Ft_PageIndex": 0,
  "Ft_PageSize": 10
}
 {}
 {
  "MST": "0100000001"
}
 {
  "MST": "0100000010",
  "OrgID": "4020320001",
  "NNTFullName": "Công ty Cổ phần Thương mại Mới",
  "NNTShortName": "TM Mới",
  "NNTAddress": "123 Đường Láng, Hà Nội",
  "ContactName": "Nguyễn Văn A",
  "ContactPhone": "0987654321",
  "ContactEmail": "contact@tmmoi.vn"
}
 {
  "MST": "0100000001",
  "NNTFullName": "Công ty Cổ phần Tập đoàn 01 - Đã cập nhật",
  "NNTShortName": "Tập đoàn 01",
  "NNTAddress": "456 Phố Huế, Hà Nội",
  "ContactName": "Trần Văn B",
  "ContactPhone": "0912345678",
  "ContactEmail": "update01@demo.vn",
  "BizType": "1",
  "FlagActive": "1"
}
 {
  "MST": "0100000006"
}
