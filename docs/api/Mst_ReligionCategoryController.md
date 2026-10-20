Phạm vi: Controllers/Port/Mst_ReligionCategoryControllerPort.cs, Models/Port/Mst_ReligionCategory.cs
Cập nhật: 2026-10-16
Phiên bản mã: 7973495
Trạng thái: Nháp — chờ review

# Tài liệu API: Mst_ReligionCategoryController

## 1. Tóm tắt
- **Mục đích nghiệp vụ**: Quản lý danh mục tôn giáo (`Mst_ReligionCategory`), hỗ trợ tìm kiếm phân trang theo từ khóa và trạng thái hoạt động, và lấy danh sách các tôn giáo đang hoạt động.
- **Bảng nguồn**: 
  - Gate controller: `13.ClientGate/V10/idn.Loyalty.WebAPI/Controllers/Mst_ReligionCategoryController.cs`
  - Biz: `11.BackEnd/V10/idn.Loyalty.Biz/MasterData.cs` (`Mst_ReligionCategory_GetX`)
  - DDL: Không có `create table Mst_ReligionCategory` cụ thể trong file migrate SQL; cấu trúc dựa trên model.
- **Cấu trúc bảng (Mst_ReligionCategory)**:
  | Tên cột | Kiểu dữ liệu EF | Khóa | Ghi chú |
  | --- | --- | --- | --- |
  | `ReligionCode` | `string` | PK | Mã tôn giáo (NOT NULL) |
  | `NetworkID` | `string?` | - | Mã mạng |
  | `ReligionName` | `string?` | - | Tên tôn giáo |
  | `ReligionDesc` | `string?` | - | Mô tả tôn giáo |
  | `FlagActive` | `string?` | - | Trạng thái / Cờ hoạt động ('1'/'0') |
  | `LogLUDTimeUTC` | `DateTime?` | - | Thời điểm cập nhật UTC |
  | `LogLUBy` | `string?` | - | Người cập nhật |

---

## 2. Các Endpoint

### 2.1. POST /api/mst_religioncategory/search
- **Mô tả**: Tìm kiếm và phân trang danh mục tôn giáo.
- **Request Body** (`Mst_ReligionCategorySearchReq`):
  | Tên tham số | Kiểu | Bắt buộc? | Mặc định | Ý nghĩa |
  | --- | --- | --- | --- | --- |
  | `OrgID` | `string?` | Không | `null` | Mã tổ chức (Mệnh đề lọc nguồn không sử dụng) |
  | `KeyWord` | `string?` | Không | `null` | Từ khóa tìm kiếm theo tên tôn giáo (`ReligionName`, điều kiện `like '%KeyWord%'`) |
  | `FlagActive` | `string?` | Không | `null` | Cờ hoạt động, sử dụng so sánh bằng `=` |
  | `Ft_PageIndex` | `int` | Không | `0` | Chỉ số trang (0-based) |
  | `Ft_PageSize` | `int` | Không | `10` | Số dòng trên mỗi trang |

- **Quy tắc xử lý**:
  1. Tính toán phân trang từ `Ft_PageIndex` và `Ft_PageSize` (`start = pageIndex * pageSize`).
  2. Lọc theo `KeyWord` (trên cột `ReligionName`) và `FlagActive` (toán tử `=`). `OrgID` không tham gia vào điều kiện lọc.
  3. Sắp xếp kết quả theo `ReligionCode asc`.
  4. Đếm tổng số bản ghi khớp (`itemCount`).
  5. Cắt trang theo phạm vi `[start, start + pageSize)`. Nếu `PageSize = 0`, trả về danh sách rỗng.
  6. Trả về cấu trúc phân trang chuẩn `PageInfo<object>`.

- **Mã lỗi**: Không phát sinh ngoại lệ đặc biệt ngoài lỗi hệ thống.
- **Ví dụ Request**:
    POST /api/mst_religioncategory/search
    Content-Type: application/json

    {
      "KeyWord": "Phật",
      "FlagActive": "1",
      "Ft_PageIndex": 0,
      "Ft_PageSize": 10
    }

- **Ví dụ Response**:
    {
      "Success": true,
      "Data": {
        "DataList": [
          {
            "MyIdxSeq": 0,
            "ReligionCode": "BUD",
            "NetworkID": "DEMO",
            "ReligionName": "Phật giáo",
            "ReligionDesc": null,
            "FlagActive": "1",
            "LogLUDTimeUTC": "2026-10-16T10:00:00Z",
            "LogLUBy": "seed"
          }
        ],
        "ItemCount": 1,
        "PageCount": 1,
        "PageIndex": 0,
        "PageSize": 10
      }
    }

---

### 2.2. POST /api/mst_religioncategory/getallactive
- **Mô tả**: Lấy toàn bộ danh sách tôn giáo đang hoạt động (`FlagActive = "1"`).
- **Request Body**: Không có.
- **Quy tắc xử lý**:
  1. Gọi truy vấn ngầm với `FlagActive = "1"`, `recordStart = 0`, `recordCount = 123456000`.
  2. Trả về mảng trần danh sách kết quả; nếu rỗng, trả về `Data = null`.
- **Mã lỗi**: Không có.
- **Ví dụ Request**:
    POST /api/mst_religioncategory/getallactive
    Content-Type: application/json

    {}

- **Ví dụ Response**:
    {
      "Success": true,
      "Data": [
        {
          "MyIdxSeq": 0,
          "ReligionCode": "BUD",
          "NetworkID": "DEMO",
          "ReligionName": "Phật giáo",
          "ReligionDesc": null,
          "FlagActive": "1",
          "LogLUDTimeUTC": "2026-10-16T10:00:00Z",
          "LogLUBy": "seed"
        },
        {
          "MyIdxSeq": 1,
          "ReligionCode": "CAT",
          "NetworkID": "DEMO",
          "ReligionName": "Công giáo",
          "ReligionDesc": null,
          "FlagActive": "1",
          "LogLUDTimeUTC": "2026-10-16T10:00:00Z",
          "LogLUBy": "seed"
        },
        {
          "MyIdxSeq": 2,
          "ReligionCode": "ISL",
          "NetworkID": "DEMO",
          "ReligionName": "Hồi giáo",
          "ReligionDesc": null,
          "FlagActive": "1",
          "LogLUDTimeUTC": "2026-10-16T10:00:00Z",
          "LogLUBy": "seed"
        }
      ]
    }

---

## 3. Khác biệt so với nguồn & GAP
- **Phân quyền & Xác thực**: Các phương thức kiểm tra xác thực và phân quyền tại nguồn được bỏ qua do hệ thống đích chưa tích hợp.
- **Log hệ thống**: Việc ghi nhật ký tại tầng Biz nguồn không đưa vào bản port.
- **DDL Cơ sở dữ liệu**: Cấu trúc bảng được suy luận từ Model trong mã nguồn chung.

---

## 4. Nhật ký thay đổi
- `#231` → Port chức năng Search và GetAllActive.
