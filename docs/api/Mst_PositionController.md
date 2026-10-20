Phạm vi: Controllers/Port/Mst_PositionControllerPort.cs, Models/Port/Mst_Position.cs
Cập nhật: 2026-10-16
Phiên bản mã: 7973495
Trạng thái: Nháp — chờ review

# Tài liệu API: Mst_PositionController

## 1. Tóm tắt
- **Mục đích nghiệp vụ**: Quản lý danh mục chức danh (`Mst_Position`), hỗ trợ tìm kiếm phân trang, lấy danh sách hoạt động, lấy chi tiết theo mã chức danh, thêm mới (sinh mã tự động), cập nhật, xóa đơn, xóa nhiều (hành vi xóa một phần) và xuất tệp Excel.
- **Bảng nguồn**: 
  - Gate controller: `13.ClientGate/V10/idn.Loyalty.WebAPI/Controllers/Mst_PositionController.cs`
  - API: `MstPositionController.cs`
  - Biz: `11.BackEnd/V10/idn.Loyalty.Biz/MasterData.cs` (`WAS_Mst_Position_Get`, `Mst_Position_Get`, `Mst_Position_GetX`, `Mst_Position_CreateX`, `Mst_Position_UpdateX`, `Mst_Position_DeleteX`)
  - DDL: Không có DDL `create table Mst_Position` trong nguồn; cấu trúc xây dựng dựa trên model `idn.Loyalty.Common\Models\HRM\Mst_Position.cs:1-22` và các hàm lưu `MasterData.cs`.
- **Cấu trúc bảng (Mst_Position)**:
  | Tên cột | Kiểu dữ liệu EF | Khóa | Ghi chú |
  | --- | --- | --- | --- |
  | `OrgID` | `string` | PK (Phần 1) | Mã tổ chức (NOT NULL) |
  | `PositionCode` | `string` | PK (Phần 2) | Mã chức danh (NOT NULL) |
  | `NetworkID` | `string?` | - | Mã mạng lưới |
  | `PositionName` | `string?` | - | Tên chức danh |
  | `PositionDesc` | `string?` | - | Mô tả chức danh |
  | `FlagActive` | `string?` | - | Trạng thái ('1'/'0') |
  | `Remark` | `string?` | - | Ghi chú |
  | `LogLUDTimeUTC` | `DateTime?` | - | Thời điểm cập nhật UTC |
  | `LogLUBy` | `string?` | - | Người cập nhật |

---

## 2. Các Endpoint

### 2.1. POST /api/mst_position/search
- **Mô tả**: Tìm kiếm và phân trang danh mục chức danh.
- **Request Body** (`MstPositionSearchReq`):
  | Tên tham số | Kiểu | Bắt buộc? | Mặc định | Ý nghĩa |
  | --- | --- | --- | --- | --- |
  | `OrgID` | `string?` | Không | `null` | Mã tổ chức (so sánh bằng `=`) |
  | `KeyWord` | `string?` | Không | `null` | Từ khóa tìm kiếm theo tên chức danh (`PositionName`, điều kiện `like '%KeyWord%'`) |
  | `FlagActive` | `string?` | Không | `null` | Cờ hoạt động, tách dấu phẩy sang toán tử `IN` |
  | `Ft_PageIndex` | `int` | Không | `0` | Chỉ số trang (0-based) |
  | `Ft_PageSize` | `int` | Không | `10` | Số dòng trên mỗi trang |

- **Quy tắc xử lý**:
  1. Tính toán phân trang từ `Ft_PageIndex` và `Ft_PageSize` (`start = pageIndex * pageSize`).
  2. Lọc theo `OrgID`, `KeyWord` (trên cột `PositionName`), và `FlagActive` (dạng `IN`).
  3. Sắp xếp kết quả theo `OrgID asc`, `PositionCode asc`.
  4. Đếm tổng số bản ghi khớp (`itemCount`).
  5. Cắt trang theo phạm vi `[start, start + pageSize)`. Nếu `PageSize = 0`, trả về danh sách rỗng.
  6. Thực hiện `LEFT JOIN` với bảng `Mst_NNT` theo `OrgID` để lấy trường `mnnt_NNTFullName`.
  7. Trả về cấu trúc phân trang chuẩn `PageInfo<Mst_PositionSearchResultItem>`.

- **Mã lỗi**: Không phát sinh ngoại lệ đặc biệt ngoài lỗi hệ thống.
- **Ví dụ Request**:
    POST /api/mst_position/search
    Content-Type: application/json

    {
      "OrgID": "4020320001",
      "KeyWord": "dev",
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
            "OrgID": "4020320001",
            "PositionCode": "POS_DEV",
            "NetworkID": "1001",
            "PositionName": "Lap trinh vien",
            "PositionDesc": "Phat trien phan mem",
            "FlagActive": "1",
            "Remark": "Demo dev",
            "LogLUDTimeUTC": "2026-10-16T10:00:00Z",
            "LogLUBy": "seed",
            "mnnt_NNTFullName": null
          }
        ],
        "ItemCount": 1,
        "PageCount": 1,
        "PageIndex": 0,
        "PageSize": 10
      }
    }

---

### 2.2. POST /api/mst_position/getbypositioncode
- **Mô tả**: Lấy chi tiết thông tin chức danh theo mã chức danh và mã tổ chức.
- **Request Body** (`MstPositionGetByPositionCodeReq`):
  | Tên tham số | Kiểu | Bắt buộc? | Mặc định | Ý nghĩa |
  | --- | --- | --- | --- | --- |
  | `PositionCode` | `string?` | Có | `null` | Mã chức danh cần tra cứu |
  | `OrgID` | `string?` | Không | `null` | Mã tổ chức tùy chọn |

- **Quy tắc xử lý**:
  1. Nếu `PositionCode` rỗng hoặc `null`, không truy vấn cơ sở dữ liệu và trả về ngay `Data = null`, `Success = true`.
  2. Truy vấn với giới hạn 1 bản ghi (`recordCount = 1`) sắp xếp theo `OrgID asc`, `PositionCode asc`.
  3. Trả về đối tượng đơn (object) nếu tìm thấy, ngược lại trả về `null`.
- **Mã lỗi**: Không có.
- **Ví dụ Request**:
    POST /api/mst_position/getbypositioncode
    Content-Type: application/json

    {
      "OrgID": "4020320001",
      "PositionCode": "POS_DEV"
    }

- **Ví dụ Response**:
    {
      "Success": true,
      "Data": {
        "MyIdxSeq": 0,
        "OrgID": "4020320001",
        "PositionCode": "POS_DEV",
        "NetworkID": "1001",
        "PositionName": "Lap trinh vien",
        "PositionDesc": "Phat trien phan mem",
        "FlagActive": "1",
        "Remark": "Demo dev",
        "LogLUDTimeUTC": "2026-10-16T10:00:00Z",
        "LogLUBy": "seed",
        "mnnt_NNTFullName": null
      }
    }

---

### 2.3. POST /api/mst_position/getallactive
- **Mô tả**: Lấy toàn bộ danh sách chức danh đang hoạt động (`FlagActive = "1"`).
- **Request Body** (`MstPositionGetAllActiveReq`):
  | Tên tham số | Kiểu | Bắt buộc? | Mặc định | Ý nghĩa |
  | --- | --- | --- | --- | --- |
  | `OrgID` | `string?` | Không | `null` | Mã tổ chức tùy chọn để lọc |

- **Quy tắc xử lý**:
  1. Gọi truy vấn ngầm với `FlagActive = "1"`, `recordStart = 0`, `recordCount = int.MaxValue`.
  2. Trả về mảng trần `List<Mst_PositionSearchResultItem>`; nếu danh sách rỗng, trả về `Data = null`.
- **Mã lỗi**: Không có.
- **Ví dụ Request**:
    POST /api/mst_position/getallactive
    Content-Type: application/json

    {
      "OrgID": "4020320001"
    }

- **Ví dụ Response**:
    {
      "Success": true,
      "Data": [
        {
          "MyIdxSeq": 0,
          "OrgID": "4020320001",
          "PositionCode": "POS_DEV",
          "NetworkID": "1001",
          "PositionName": "Lap trinh vien",
          "PositionDesc": "Phat trien phan mem",
          "FlagActive": "1",
          "Remark": "Demo dev",
          "LogLUDTimeUTC": "2026-10-16T10:00:00Z",
          "LogLUBy": "seed",
          "mnnt_NNTFullName": null
        }
      ]
    }

---

### 2.4. POST /api/mst_position/create
- **Mô tả**: Thêm mới chức danh. Hệ thống sinh mã chức danh tự động thông qua bảng sequence trước khi lưu.
- **Request Body** (`MstPositionCreateReq`):
  | Tên tham số | Kiểu | Bắt buộc? | Mặc định | Ý nghĩa |
  | --- | --- | --- | --- | --- |
  | `OrgID` | `string?` | Có | `null` | Mã tổ chức |
  | `PositionCode` | `string?` | Không | `null` | Mã chức danh (sẽ bị ghi đè bởi mã sinh tự động từ hệ thống) |
  | `PositionName` | `string?` | Có | `null` | Tên chức danh |
  | `PositionDesc` | `string?` | Không | `null` | Mô tả chức danh |
  | `Remark` | `string?` | Không | `null` | Ghi chú |

- **Quy tắc xử lý & Kiểm tra dữ liệu**:
  1. Nếu body `null`, trả về lỗi `strJson null` (mã mặc định `Mst_Position_Create`).
  2. Lấy mã sequence tự động (`Seq_PositionCode`) và format dạng `POS{0:00000}`.
  3. Mở transaction, chuẩn hóa và kiểm tra điều kiện:
     - `OrgID` hoặc mã chức danh sinh ra rỗng -> Lỗi `Mst_Position_CreateX_InvalidOrgID` / `Mst_Position_CreateX_InvalidPositionCode`.
     - Kiểm tra chức danh đã tồn tại trong tổ chức chưa (`Mst_Position_CheckDB_PositionCodeExist`).
     - Kiểm tra tổ chức (`OrgID`) phải tồn tại và đang hoạt động (`Mst_Org_CheckDB_OrgNotFound`, `Mst_Org_CheckDB_FlagActiveNotMatched`).
     - Kiểm tra tên chức danh không được rỗng và chưa tồn tại trong tổ chức (`Mst_Position_CreateX_InvalidPositionName`, `Mst_Position_CheckDB_PositionNameExist`).
  4. Lưu dữ liệu mới với `FlagActive = "1"`, `LogLUDTimeUTC = DateTime.UtcNow`, `LogLUBy = "port"` và Commit transaction.

- **Bảng mã lỗi**:
  | Mã lỗi (`ErridnLoyalty.*`) | Điều kiện phát sinh |
  | --- | --- |
  | `Mst_Position_Create` | Body `null` hoặc lỗi ngoại lệ hệ thống trong transaction. |
  | `Mst_Position_CreateX_InvalidOrgID` | `OrgID` rỗng sau khi chuẩn hóa. |
  | `Mst_Position_CreateX_InvalidPositionCode` | Mã chức danh sinh tự động bị rỗng. |
  | `Mst_Position_CheckDB_PositionCodeExist` | Mã chức danh đã tồn tại trong tổ chức. |
  | `Mst_Org_CheckDB_OrgNotFound` | Không tìm thấy tổ chức (`OrgID`) trong hệ thống. |
  | `Mst_Org_CheckDB_FlagActiveNotMatched` | Tổ chức (`OrgID`) không ở trạng thái hoạt động (`FlagActive != "1"`). |
  | `Mst_Position_CreateX_InvalidPositionName` | Tên chức danh rỗng. |
  | `Mst_Position_CheckDB_PositionNameExist` | Tên chức danh đã tồn tại trong cùng một tổ chức. |

- **Ví dụ Request**:
    POST /api/mst_position/create
    Content-Type: application/json

    {
      "OrgID": "4020320001",
      "PositionName": "Chuyen vien kiem thu",
      "PositionDesc": "Kiem thu phan mềm",
      "Remark": "Them moi"
    }

- **Ví dụ Response**:
    {
      "Success": true,
      "Data": null
    }

---

### 2.5. POST /api/mst_position/update
- **Mô tả**: Cập nhật thông tin chức danh.
- **Request Body** (`MstPositionUpdateReq`):
  | Tên tham số | Kiểu | Bắt buộc? | Mặc định | Ý nghĩa |
  | --- | --- | --- | --- | --- |
  | `OrgID` | `string?` | Có | `null` | Mã tổ chức |
  | `PositionCode` | `string?` | Có | `null` | Mã chức danh cần cập nhật |
  | `PositionName` | `string?` | Có | `null` | Tên chức danh mới |
  | `PositionDesc` | `string?` | Không | `null` | Mô tả mới |
  | `FlagActive` | `string?` | Không | `null` | Trạng thái hoạt động mới ('1'/'0') |
  | `Remark` | `string?` | Không | `null` | Ghi chú mới |

- **Quy tắc xử lý & Kiểm tra dữ liệu**:
  1. Kiểm tra body `null` (`strJson null`).
  2. Mở transaction, thực hiện các bước kiểm tra:
     - Kiểm tra chức danh tồn tại (`Mst_Position_CheckDB_PositionCodeNotFound`).
     - Kiểm tra tổ chức tồn tại và đang hoạt động (`Mst_Org_CheckDB_OrgNotFound`, `Mst_Org_CheckDB_FlagActiveNotMatched`).
     - Kiểm tra tên chức danh không rỗng (`Mst_Position_UpdateX_InvalidPositionName`).
     - Nếu thay đổi tên chức danh, kiểm tra xem tên mới đã bị trùng trong cùng tổ chức chưa (`Mst_Position_CheckDB_PositionNameExist`).
  3. Cập nhật các trường được phép (`PositionName`, `PositionDesc`, `FlagActive`, `Remark`), cập nhật `LogLUDTimeUTC` và `LogLUBy`, sau đó Commit transaction.

- **Bảng mã lỗi**:
  | Mã lỗi (`ErridnLoyalty.*`) | Điều kiện phát sinh |
  | --- | --- |
  | `Mst_Position_Update` | Body `null` hoặc ngoại lệ hệ thống trong quá trình cập nhật. |
  | `Mst_Position_CheckDB_PositionCodeNotFound` | Không tìm thấy chức danh cần cập nhật. |
  | `Mst_Org_CheckDB_OrgNotFound` | Không tìm thấy tổ chức (`OrgID`). |
  | `Mst_Org_CheckDB_FlagActiveNotMatched` | Tổ chức không ở trạng thái hoạt động. |
  | `Mst_Position_UpdateX_InvalidPositionName` | Tên chức danh mới rỗng. |
  | `Mst_Position_CheckDB_PositionNameExist` | Tên chức danh mới bị trùng với chức danh khác trong cùng tổ chức. |

- **Ví dụ Request**:
    POST /api/mst_position/update
    Content-Type: application/json

    {
      "OrgID": "4020320001",
      "PositionCode": "POS_DEV",
      "PositionName": "Senior Developer",
      "PositionDesc": "Phat trien cao cap",
      "FlagActive": "1",
      "Remark": "Cap nhat"
    }

- **Ví dụ Response**:
    {
      "Success": true,
      "Data": null
    }

---

### 2.6. POST /api/mst_position/delete
- **Mô tả**: Xóa đơn một chức danh.
- **Request Body** (`MstPositionDeleteReq`):
  | Tên tham số | Kiểu | Bắt buộc? | Mặc định | Ý nghĩa |
  | --- | --- | --- | --- | --- |
  | `OrgID` | `string?` | Có | `null` | Mã tổ chức |
  | `PositionCode` | `string?` | Có | `null` | Mã chức danh cần xóa |

- **Quy tắc xử lý**:
  1. Kiểm tra `PositionCode` rỗng/null, nếu rỗng trả về lỗi `invalid_positioncode`.
  2. Mở transaction riêng, kiểm tra tổ chức và sự tồn tại của chức danh.
  3. Xóa bản ghi khỏi bảng `Mst_Position` và Commit transaction.

- **Bảng mã lỗi**:
  | Mã lỗi (`ErridnLoyalty.*`) | Điều kiện phát sinh |
  | --- | --- |
  | `invalid_positioncode` | Mã chức danh truyền vào bị rỗng hoặc `null`. |
  | `Mst_Org_CheckDB_OrgNotFound` | Không tìm thấy tổ chức tương ứng. |
  | `Mst_Org_CheckDB_FlagActiveNotMatched` | Tổ chức không ở trạng thái hoạt động. |
  | `Mst_Position_CheckDB_PositionCodeNotFound` | Không tìm thấy chức danh cần xóa trong cơ sở dữ liệu. |
  | `Mst_Position_Delete` | Lỗi phát sinh trong quá trình xóa dữ liệu. |

- **Ví dụ Request**:
    POST /api/mst_position/delete
    Content-Type: application/json

    {
      "OrgID": "4020320001",
      "PositionCode": "POS_DEV"
    }

- **Ví dụ Response**:
    {
      "Success": true,
      "Data": null
    }

---

### 2.7. POST /api/mst_position/deletemultiple
- **Mô tả**: Xóa nhiều chức danh theo danh sách. Hỗ trợ hành vi **xóa một phần** (partial delete).
- **Request Body**: Mảng các đối tượng xóa (`List<MstPositionDeleteReq>`).
  | Tên tham số | Kiểu | Bắt buộc? | Mặc định | Ý nghĩa |
  | --- | --- | --- | --- | --- |
  | `OrgID` | `string?` | Có | `null` | Mã tổ chức của từng phần tử |
  | `PositionCode` | `string?` | Có | `null` | Mã chức danh của từng phần tử |

- **Quy tắc xử lý**:
  1. Nếu danh sách body `null`, trả về lỗi `Mst_Position_Delete`.
  2. Lặp qua từng phần tử và gọi thao tác xóa riêng biệt (mỗi phần tử một transaction riêng).
  3. **Hành vi đặc biệt**: Nếu phần tử thứ $k$ gặp lỗi, vòng lặp dừng lại ngay và trả về lỗi của phần tử đó; các phần tử trước $k$ đã xóa thành công vào DB và **không bị rollback**.

- **Bảng mã lỗi**: Các mã lỗi tương tự như endpoint xóa đơn (`Mst_Org_CheckDB_*`, `Mst_Position_CheckDB_*`, `Mst_Position_Delete`).
- **Ví dụ Request**:
    POST /api/mst_position/deletemultiple
    Content-Type: application/json

    [
      {
        "OrgID": "4020320001",
        "PositionCode": "POS_MGR"
      }
    ]

- **Ví dụ Response**:
    {
      "Success": true,
      "Data": null
    }

---

### 2.8. POST /api/mst_position/export
- **Mô tả**: Xuất danh sách chức danh ra tệp Excel dựa trên tiêu chí tìm kiếm.
- **Request Body** (`MstPositionSearchReq`):
  | Tên tham số | Kiểu | Bắt buộc? | Mặc định | Ý nghĩa |
  | --- | --- | --- | --- | --- |
  | `OrgID` | `string?` | Không | `null` | Lọc theo tổ chức |
  | `KeyWord` | `string?` | Không | `null` | Lọc theo từ khóa tên chức danh |
  | `FlagActive` | `string?` | Không | `null` | Lọc theo trạng thái hoạt động |

- **Quy tắc xử lý**:
  1. Truy vấn toàn bộ dữ liệu khớp với điều kiện tìm kiếm (giới hạn `recordCount = int.MaxValue`).
  2. Nếu danh sách kết quả rỗng, trả về lỗi với thông điệp `"Không có dữ liệu"`.
  3. Sử dụng từ điển cột xuất Excel (`GetImportDicColums()`) gồm 3 cột: Tên chức danh (`PositionName`), Mô tả (`PositionDesc`), và Trạng thái (`FlagActive`).
  4. Ghi file Excel và trả về đường dẫn URL tải file.

- **Bảng mã lỗi**:
  | Mã lỗi (`ErridnLoyalty.*`) | Điều kiện phát sinh |
  | --- | --- |
  | `Mst_Position_Export` | Không có dữ liệu để xuất hoặc lỗi phát sinh trong quá trình tạo tệp Excel. |

- **Ví dụ Request**:
    POST /api/mst_position/export
    Content-Type: application/json

    {
      "OrgID": "4020320001",
      "FlagActive": "1"
    }

- **Ví dụ Response**:
    {
      "Success": true,
      "Data": {
        "Url": "http://localhost:5000/temp/excel/Mst_Position_20261016.xlsx"
      }
    }

---

## 3. Khác biệt so với nguồn & GAP
- **Hệ thống phân quyền**: Các phương thức kiểm tra xác thực (`CheckGatewayAuthentication`, `CheckAuthorize`, `Sys_Access_CheckDenyV30`) tại nguồn đã được lược bỏ do hệ thống đích chưa tích hợp phân quyền.
- **Log hệ thống**: Việc ghi nhật ký `RequestLog`/`ReturnLog` tại tầng Biz nguồn không được đưa vào bản port.
- **Bảng `Staff_MapDepartment`**: Kiểm tra liên kết nhân sự - phòng ban trong quá trình xóa chức danh bị bỏ qua do chưa có entity tương ứng trong `DbContext`.
- **Định dạng Sequence**: Chuỗi sinh mã tự động cho `PositionCode` sử dụng format tạm `POS{0:00000}`.

---

## 4. Nhật ký thay đổi
- `#197` → Port chức năng Search.
- `#198` → Port chức năng GetByPositionCode.
- `#199` → Port chức năng GetAllActive.
- `#200` → Port chức năng Create.
- `#201` → Port chức năng Update.
- `#202` → Port chức năng Delete.
- `#203` → Port chức năng DeleteMultiple.
- `#204` → Port chức năng Export.
