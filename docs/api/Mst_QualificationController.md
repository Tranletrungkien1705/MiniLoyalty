Phạm vi: Controllers/Port/Mst_QualificationControllerPort.cs, Models/Port/Mst_Qualification.cs
Cập nhật: 2026-10-16
Phiên bản mã: 7973495
Trạng thái: Nháp — chờ review

# Tài liệu API: Mst_QualificationController

## 1. Tóm tắt
- **Mục đích nghiệp vụ**: Quản lý danh mục trình độ học vấn (`Mst_Qualification`), hỗ trợ tìm kiếm phân trang theo từ khóa và trạng thái hoạt động, và lấy danh sách các trình độ đang hoạt động.
- **Bảng nguồn**: 
  - Gate controller: `13.ClientGate/V10/idn.Loyalty.WebAPI/Controllers/Mst_QualificationController.cs`
  - API: `MstQualificationController.cs:17-88`
  - Biz: `11.BackEnd/V10/idn.Loyalty.Biz/MasterData.cs` (`WAS_Mst_Qualification_Get`, `Mst_Qualification_Get`, `Mst_Qualification_GetX`)
  - DDL: Không có `create table Mst_Qualification` trong mã nguồn (sử dụng model để ánh xạ cấu trúc bảng).
- **Cấu trúc bảng (Mst_Qualification)**:
  | Tên cột | Kiểu dữ liệu EF | Khóa | Ghi chú |
  | --- | --- | --- | --- |
  | `QualificationCode` | `string` | PK | Mã trình độ học vấn (NOT NULL) |
  | `NetworkID` | `string?` | - | Mã mạng |
  | `QualificationName` | `string?` | - | Tên trình độ học vấn |
  | `FlagActive` | `string?` | - | Trạng thái / Cờ hoạt động ('1'/'0') |
  | `Remark` | `string?` | - | Ghi chú |
  | `LogLUDTimeUTC` | `DateTime?` | - | Thời điểm cập nhật UTC |
  | `LogLUBy` | `string?` | - | Người cập nhật |

---

## 2. Các Endpoint

### 2.1. POST /api/mst_qualification/search
- **Mô tả**: Tìm kiếm và phân trang danh mục trình độ học vấn.
- **Request Body** (`MstQualificationSearchReq`):
  | Tên tham số | Kiểu | Bắt buộc? | Mặc định | Ý nghĩa |
  | --- | --- | --- | --- | --- |
  | `OrgID` | `string?` | Không | `null` | Mã tổ chức (Nhưng mệnh đề lọc nguồn không sử dụng) |
  | `KeyWord` | `string?` | Không | `null` | Từ khóa tìm kiếm theo tên trình độ (`QualificationName`, điều kiện `like '%KeyWord%'`) |
  | `FlagActive` | `string?` | Không | `null` | Cờ hoạt động, sử dụng toán tử `=` chính xác (không dùng IN) |
  | `Ft_PageIndex` | `int` | Không | `0` | Chỉ số trang (0-based) |
  | `Ft_PageSize` | `int` | Không | `10` | Số dòng trên mỗi trang |

- **Quy tắc xử lý**:
  1. Tính toán phân trang từ `Ft_PageIndex` và `Ft_PageSize` (`start = pageIndex * pageSize`).
  2. Lọc theo `KeyWord` (trên cột `QualificationName`) và `FlagActive` (toán tử `=`). `OrgID` không tham gia vào điều kiện lọc.
  3. Sắp xếp kết quả theo `QualificationCode asc`.
  4. Đếm tổng số bản ghi khớp (`itemCount`).
  5. Cắt trang theo phạm vi `[start, start + pageSize)`. Nếu `PageSize = 0`, trả về danh sách rỗng.
  6. Trả về cấu trúc phân trang chuẩn `PageInfo<object>`.

- **Mã lỗi**: Không phát sinh ngoại lệ đặc biệt ngoài lỗi hệ thống.
- **Ví dụ Request**:
    POST /api/mst_qualification/search
    Content-Type: application/json

    {
      "KeyWord": "Đại",
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
            "QualificationCode": "UNI",
            "NetworkID": "DEMO",
            "QualificationName": "Đại học",
            "FlagActive": "1",
            "Remark": null,
            "LogLUDTimeUTC": "2026-10-10T10:00:00Z",
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

### 2.2. POST /api/mst_qualification/getallactive
- **Mô tả**: Lấy toàn bộ danh sách trình độ học vấn đang hoạt động (`FlagActive = "1"`).
- **Request Body**: Không có.
- **Quy tắc xử lý**:
  1. Gọi truy vấn ngầm với `FlagActive = "1"`, `recordStart = 0`, `recordCount = 123456000`.
  2. Trả về mảng trần danh sách kết quả; nếu rỗng, trả về `Data = null`.
- **Mã lỗi**: Không có.
- **Ví dụ Request**:
    POST /api/mst_qualification/getallactive
    Content-Type: application/json

    {}

- **Ví dụ Response**:
    {
      "Success": true,
      "Data": [
        {
          "MyIdxSeq": 0,
          "QualificationCode": "COL",
          "NetworkID": "DEMO",
          "QualificationName": "Cao đẳng",
          "FlagActive": "1",
          "Remark": null,
          "LogLUDTimeUTC": "2026-10-10T10:00:00Z",
          "LogLUBy": "seed"
        },
        {
          "MyIdxSeq": 1,
          "QualificationCode": "DOC",
          "NetworkID": "DEMO",
          "QualificationName": "Tiến sĩ",
          "FlagActive": "1",
          "Remark": null,
          "LogLUDTimeUTC": "2026-10-10T10:00:00Z",
          "LogLUBy": "seed"
        },
        {
          "MyIdxSeq": 2,
          "QualificationCode": "UNI",
          "NetworkID": "DEMO",
          "QualificationName": "Đại học",
          "FlagActive": "1",
          "Remark": null,
          "LogLUDTimeUTC": "2026-10-10T10:00:00Z",
          "LogLUBy": "seed"
        }
      ]
    }

---

## 3. Khác biệt so với nguồn & GAP
- **Phân quyền & Xác thực**: Các phương thức kiểm tra xác thực và phân quyền (`CheckGatewayAuthentication`, `CheckAuthorize`, `Sys_User_CheckAuthentication`, `Sys_Access_CheckDenyV30`) tại nguồn được bỏ qua do hệ thống đích chưa tích hợp.
- **Thông tin đăng nhập**: Tài khoản viết cứng `GwUserCode`/`GwPassword` tại gate nguồn không được sao chép vào mã port.
- **Log hệ thống**: Việc ghi nhật ký `RequestLog`/`ReturnLog` tại tầng Biz nguồn không đưa vào bản port.
- **DDL Cơ sở dữ liệu**: Không có script DDL `create table Mst_Qualification` trong mã nguồn; cấu trúc bảng được suy luận từ Model.

---

## 4. Nhật ký thay đổi
- `#223` → Port chức năng Search.
- `#224` → Port chức năng GetAllActive.
