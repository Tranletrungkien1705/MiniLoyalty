Phạm vi: Controllers/Port/Mst_QualificationFormControllerPort.cs, Models/Port/Mst_QualificationForm.cs
Cập nhật: 2026-10-16
Phiên bản mã: 7973495
Trạng thái: Nháp — chờ review

# Tài liệu API: Mst_QualificationFormController

## 1. Tóm tắt
- **Mục đích nghiệp vụ**: Quản lý danh mục hình thức trình độ (`Mst_QualificationForm`), hỗ trợ tìm kiếm phân trang theo từ khóa và trạng thái hoạt động, và lấy danh sách các hình thức đang hoạt động.
- **Bảng nguồn**: 
  - Gate controller: `13.ClientGate/V10/idn.Loyalty.WebAPI/Controllers/Mst_QualificationFormController.cs`
  - API: `MstQualificationFormController.cs:17-88`
  - Biz: `11.BackEnd/V10/idn.Loyalty.Biz/MasterData.cs` (`Mst_QualificationForm_GetX`)
  - DDL: Không có `create table Mst_QualificationForm` cụ thể trong file migrate SQL; cấu trúc dựa trên model `idn.Loyalty.Common/Models/HRM/Mst_QualificationForm.cs:9-17`.
- **Cấu trúc bảng (Mst_QualificationForm)**:
  | Tên cột | Kiểu dữ liệu EF | Khóa | Ghi chú |
  | --- | --- | --- | --- |
  | `QualificationFormCode` | `string` | PK | Mã hình thức trình độ (NOT NULL) |
  | `NetworkID` | `string?` | - | Mã mạng |
  | `QualificationFormName` | `string?` | - | Tên hình thức trình độ |
  | `FlagActive` | `string?` | - | Trạng thái / Cờ hoạt động ('1'/'0') |
  | `LogLUDTimeUTC` | `DateTime?` | - | Thời điểm cập nhật UTC |
  | `LogLUBy` | `string?` | - | Người cập nhật |

---

## 2. Các Endpoint

### 2.1. POST /api/mst_qualificationform/search
- **Mô tả**: Tìm kiếm và phân trang danh mục hình thức trình độ.
- **Request Body** (`Mst_QualificationFormSearchReq`):
  | Tên tham số | Kiểu | Bắt buộc? | Mặc định | Ý nghĩa |
  | --- | --- | --- | --- | --- |
  | `OrgID` | `string?` | Không | `null` | Mã tổ chức (Mệnh đề lọc nguồn không sử dụng) |
  | `KeyWord` | `string?` | Không | `null` | Từ khóa tìm kiếm theo tên hình thức (`QualificationFormName`, điều kiện `like '%KeyWord%'`) |
  | `FlagActive` | `string?` | Không | `null` | Cờ hoạt động, sử dụng toán tử `=` nguyên chuỗi |
  | `Ft_PageIndex` | `int` | Không | `0` | Chỉ số trang (0-based) |
  | `Ft_PageSize` | `int` | Không | `10` | Số dòng trên mỗi trang |

- **Quy tắc xử lý**:
  1. Tính toán phân trang từ `Ft_PageIndex` và `Ft_PageSize` (`start = pageIndex * pageSize`).
  2. Lọc theo `KeyWord` (trên cột `QualificationFormName`) và `FlagActive` (toán tử `=`). `OrgID` không tham gia vào điều kiện lọc.
  3. Sắp xếp kết quả theo `QualificationFormCode asc`.
  4. Đếm tổng số bản ghi khớp (`itemCount`).
  5. Cắt trang theo phạm vi `[start, start + pageSize)`. Nếu `PageSize = 0`, trả về danh sách rỗng.
  6. Trả về cấu trúc phân trang chuẩn `PageInfo<object>`.

- **Mã lỗi**: Không phát sinh ngoại lệ đặc biệt ngoài lỗi hệ thống.
- **Ví dụ Request**:
    POST /api/mst_qualificationform/search
    Content-Type: application/json

    {
      "KeyWord": "Dao tao",
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
            "QualificationFormCode": "QF1",
            "NetworkID": "DEMO",
            "QualificationFormName": "Dao tao noi bo",
            "FlagActive": "1",
            "LogLUDTimeUTC": "2026-10-16T10:00:00Z",
            "LogLUBy": "seed"
          },
          {
            "MyIdxSeq": 1,
            "QualificationFormCode": "QF2",
            "NetworkID": "DEMO",
            "QualificationFormName": "Dao tao ngoai",
            "FlagActive": "1",
            "LogLUDTimeUTC": "2026-10-16T10:00:00Z",
            "LogLUBy": "seed"
          }
        ],
        "ItemCount": 2,
        "PageCount": 1,
        "PageIndex": 0,
        "PageSize": 10
      }
    }

---

### 2.2. POST /api/mst_qualificationform/getallactive
- **Mô tả**: Lấy toàn bộ danh sách hình thức trình độ đang hoạt động (`FlagActive = "1"`).
- **Request Body**: Không có.
- **Quy tắc xử lý**:
  1. Gọi truy vấn ngầm với `FlagActive = "1"`, `recordStart = 0`, `recordCount = 123456000`.
  2. Trả về mảng trần danh sách kết quả; nếu rỗng, trả về `Data = null`.
- **Mã lỗi**: Không có.
- **Ví dụ Request**:
    POST /api/mst_qualificationform/getallactive
    Content-Type: application/json

    {}

- **Ví dụ Response**:
    {
      "Success": true,
      "Data": [
        {
          "MyIdxSeq": 0,
          "QualificationFormCode": "QF1",
          "NetworkID": "DEMO",
          "QualificationFormName": "Dao tao noi bo",
          "FlagActive": "1",
          "LogLUDTimeUTC": "2026-10-16T10:00:00Z",
          "LogLUBy": "seed"
        },
        {
          "MyIdxSeq": 1,
          "QualificationFormCode": "QF2",
          "NetworkID": "DEMO",
          "QualificationFormName": "Dao tao ngoai",
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
- `#225` → Port chức năng Search.
- `#226` → Port chức năng GetAllActive.
