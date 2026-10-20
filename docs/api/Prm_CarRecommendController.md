---
Phạm vi: Controllers/Port/Prm_CarRecommendControllerPort.cs, Models/Port/Prm_CarRecommend.cs
Cập nhật: 2026-10-18
Phiên bản mã: d30cdab
Trạng thái: Nháp — chờ review
---

# Tài liệu API: Prm_CarRecommendController

## 1. Tóm tắt

API quản lý chương trình giới thiệu/khuyến mại xe mới (`Prm_CarRecommend`), phục vụ các nghiệp vụ tra cứu phân trang, lấy chi tiết, lưu trữ (thêm mới/sửa), phê duyệt (Approve), hoàn thành (Finish), hủy (Cancel), xóa bản ghi, và tính toán khuyến mại xe mới (`CalcPrm`).

- **Bảng nguồn**: 
  - Gate Controller: `Prm_CarRecommendController.cs` (và tương đương `Prm_CarNewController.cs`)
  - API: `11.BackEnd\V10\idn.Loyalty.Biz.Web\Controllers`
  - Biz: `11.BackEnd\V10\idn.Loyalty.Biz\Promotion.cs`
- **Bảng cơ sở dữ liệu**:
  - `Prm_CarRecommend` (Header):
    - `PRMCRCodeSys`: Kiểu `string` (EF), Khóa chính hệ thống.
    - `NetworkID`: Kiểu `string` (EF), Mã mạng lưới.
    - `OrgID`: Kiểu `string` (EF), Mã tổ chức.
    - `DLCPCode`: Kiểu `string` (EF), Mã đại lý.
    - `PRMCRCode`: Kiểu `string` (EF), Mã chương trình.
    - `PRMCRName`: Kiểu `string?` (EF), Tên chương trình giới thiệu xe.
    - `EffDateStart`, `EffDateEnd`: Kiểu `string?` (EF), Ngày bắt đầu và kết thúc hiệu lực.
    - `FlagAllModel`: Kiểu `string?` (EF), Cờ áp dụng tất cả model xe.
    - `PointValAllModel`: Kiểu `double?` (EF), Giá trị điểm áp dụng tất cả model.
    - `PRMCRStatus`: Kiểu `string?` (EF), Trạng thái (`PENDING`, `APPROVE`, `FINISH`, `CANCEL`).
    - `CreateDTimeUTC`, `LUDTimeUTC`, `ApprDTimeUTC`, `FinishDTimeUTC`, `CancelDTimeUTC`, `LogLUDTimeUTC`: Kiểu `DateTime?` (EF), Các mốc thời gian UTC.
    - `CreateBy`, `LUBy`, `ApprBy`, `FinishBy`, `CancelBy`, `LogLUBy`, `Remark`: Kiểu `string?` (EF), Thông tin người tác động và ghi chú.
  - `Prm_CarRecommendDtl` (Chi tiết giá trị điểm theo dòng):
    - `PRMCNCodeSys`, `Idx`: Kiểu `string`, `long` (EF), Khóa chính composite.
    - `NetworkID`, `OrgID`: Kiểu `string` (EF).
    - `PointVal`: Kiểu `double?` (EF), Giá trị điểm.
    - `PRMCNStatusDtl`: Kiểu `string?` (EF), Trạng thái chi tiết.
    - `Remark`: Kiểu `string?` (EF), Ghi chú.
    - `LogLUDTimeUTC`, `LogLUBy`: Kiểu `DateTime?`, `string?` (EF).
  - `Prm_CarRecommendSpec` (Chi tiết model xe áp dụng):
    - `PRMCNCodeSys`, `Idx`: Kiểu `string`, `long` (EF), Khóa chính composite.
    - `ModelCode`: Kiểu `string` (EF), Mã model xe.
    - `NetworkID`, `OrgID`: Kiểu `string` (EF).
    - `PRMCNStatusSpec`: Kiểu `string?` (EF), Trạng thái.
    - `LogLUDTimeUTC`, `LogLUBy`: Kiểu `DateTime?`, `string?` (EF).

---

## 2. Các Endpoint

### 2.1. Search (Tra cứu phân trang chương trình khuyến mại xe mới)
- **Route & Method**: `POST /api/prm_carrecommend/search`
- **Mã nguồn đích**: `Controllers/Port/Prm_CarRecommendControllerPort.cs:25`

**Bảng tham số Request (`Prm_CarRecommendSearchReq`):**
| Tên tham số | Kiểu dữ liệu | Bắt buộc? | Mặc định | Ý nghĩa |
| :--- | :--- | :--- | :--- | :--- |
| `OrgID` | string? | Không | null | Lọc theo mã tổ chức |
| `KeyWord` | string? | Không | null | Từ khóa tìm kiếm |
| `PRMCRCodeSys` | string? | Không | null | Lọc theo mã hệ thống chương trình |
| `PRMCRName` | string? | Không | null | Lọc theo tên chương trình |
| `PRMCRCode` | string? | Không | null | Lọc theo mã chương trình |
| `DLCPCode` | string? | Không | null | Lọc theo mã đại lý |
| `PRMCRStatus` | string? | Không | null | Lọc theo trạng thái chương trình |
| `EffDateStart` | string? | Không | null | Lọc theo ngày bắt đầu hiệu lực |
| `CreateDTimeUTCFrom` | string? | Không | null | Lọc thời gian tạo từ (UTC) |
| `CreateDTimeUTCTo` | string? | Không | null | Lọc thời gian tạo đến (UTC) |
| `FlagShow` | string? | Không | null | Lọc theo cờ hiển thị (`1` hoặc `0`) |
| `Ft_PageIndex` | int | Không | 0 | Chỉ số trang (0-based) |
| `Ft_PageSize` | int | Không | 10 | Kích thước trang |

**Quy tắc xử lý:**
1. Áp dụng các điều kiện lọc dựa trên tham số truyền vào (OrgID, PRMCRCodeSys, PRMCRName, PRMCRCode, DLCPCode, EffDateStart, khoảng thời gian tạo, FlagShow).
2. Sắp xếp kết quả giảm dần theo `CreateDTimeUTC`, tính toán phân trang `Skip` và `Take`.
3. Gán thứ tự `MyIdxSeq`, tính toán lại `FlagShow` và chuẩn hóa ngày kết thúc (`EffDateEnd == "2100-01-01"` thành chuỗi rỗng).
4. Trả về kết quả bọc trong đối tượng phân trang `PageInfo<Prm_CarRecommend>`.

**Mã lỗi:**
Chưa xac minh

**Ví dụ Request:**
```json
{
  "OrgID": "ORG01",
  "PRMCRStatus": "FINISH",
  "Ft_PageIndex": 0,
  "Ft_PageSize": 10
}
 {
  "PRMCRCodeSys": "PCR01"
}
 {
  "Prm_CarRecommend": {
    "PRMCRName": "Chương trình giới thiệu xe mới tháng 11",
    "EffDateStart": "2026-11-01",
    "FlagAllModel": "1",
    "PointValAllModel": 150,
    "Remark": "Tạo mới"
  },
  "Lst_Prm_CarRecommendDtl": [
    {
      "PointVal": 150
    }
  ],
  "Lst_Prm_CarRecommendSpec": [
    {
      "ModelCode": "MODEL01"
    }
  ]
}
 {
  "PRMCRCodeSys": "PCR_P1",
  "Remark": "Phê duyệt chương trình"
}
 {
  "PRMCRCodeSys": "PCR_A1",
  "Remark": "Hoàn thành chương trình"
}
 {
  "PRMCRCodeSys": "PCR_P2",
  "Remark": "Hủy chương trình"
}
 {
  "Prm_CarRecommend": {
    "PRMCRCodeSys": "PCR_P1"
  }
}
 {
  "DLCPCode": "DL01",
  "ModelCode": "MD01"
}
