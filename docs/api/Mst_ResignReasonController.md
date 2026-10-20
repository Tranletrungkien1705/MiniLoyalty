# Phạm vi: Mst_ResignReasonControllerPort (Quản lý Lý do nghỉ việc)
# Cập nhật: 2026-10-16
# Phiên bản mã: 11bf46e
# Trạng thái: Nháp — chờ review

## 1. Tóm tắt
Mục đích nghiệp vụ của controller này là quản lý danh mục lý do nghỉ việc của nhân sự theo từng tổ chức (`OrgID`), bao gồm các chức năng tìm kiếm phân trang, tra cứu theo mã, tạo mới (từng dòng hoặc import qua Excel), cập nhật, xóa (đơn lẻ hoặc hàng loạt), xuất dữ liệu ra Excel và tải file mẫu.
- **Bảng nguồn (File gate / API / Biz)**: `Mst_ResignReasonController.cs` (Controllers/Port/Mst_ResignReasonControllerPort.cs)
- **Bảng CSDL (EF Core)**: `Mst_ResignReason` và `Seq_ReasonID`

### Bảng cột thực thể `Mst_ResignReason`:
| Tên cột | Kiểu dữ liệu EF | Khóa / Khác | Ghi chú |
| --- | --- | --- | --- |
| OrgID | `string` | Khóa chính (PK) | Mã tổ chức |
| ReasonID | `string` | Khóa chính (PK) | Mã lý do nghỉ việc |
| NetworkID | `string?` | - | Mã mạng lưới |
| ResignReason | `string?` | - | Tên lý do nghỉ việc |
| ReasonDesc | `string?` | - | Mô tả lý do nghỉ việc |
| FlagActive | `string?` | - | Trạng thái kích hoạt ("1": Hoạt động, "0": Ngừng) |
| Remark | `string?` | - | Ghi chú |
| LogLUDTimeUTC | `DateTime?` | - | Thời gian cập nhật log (UTC) |
| LogLUBy | `string?` | - | Người cập nhật log |
| mnnt_NNTFullName | `string?` | Không ánh xạ (NotMapped) | Tên tổ chức (join từ bảng `Mst_NNT`) |

---

## 2. Các Endpoint

### 2.1. POST /api/mst_resignreason/search
- **Mô tả**: Tìm kiếm và phân trang danh sách lý do nghỉ việc.
- **Request Body (`MstResignReasonSearchReq`)**:
  | Tên tham số | Kiểu dữ liệu | Bắt buộc? | Mặc định | Ý nghĩa |
  | --- | --- | --- | --- | --- |
  | OrgID | `string?` | Không | `null` | Mã tổ chức cần lọc |
  | KeyWord | `string?` | Không | `null` | Từ khóa tìm kiếm theo tên lý do |
  | FlagActive | `string?` | Không | `null` | Trạng thái kích hoạt |
  | Ft_PageIndex | `int` | Không | `0` | Số thứ tự trang (bắt đầu từ 0) |
  | Ft_PageSize | `int` | Không | `10` | Kích thước trang |

- **Quy tắc xử lý**:
  1. Lấy thông tin phân trang `pageIndex` và `pageSize` từ request. Nếu `pageSize == 0`, tự động đổi thành `10`.
  2. Truy vấn dữ liệu qua phương thức nội bộ `GetXAsync` với điều kiện lọc theo `OrgID`, `KeyWord`, và `FlagActive`.
  3. Tính toán tổng số trang (`pageCount`) dựa trên tổng số bản ghi `itemCount` và `pageSize`.
- **Mã lỗi (`ErridnLoyalty.*`)**: Chưa xác minh (Sử dụng cơ chế catch ngoại lệ chung).
- **Ví dụ Request**:
    ```json
    {
      "OrgID": "4020320001",
      "KeyWord": "Nghỉ",
      "FlagActive": "1",
      "Ft_PageIndex": 0,
      "Ft_PageSize": 10
    }
    ```
- **Ví dụ Response**: Chưa có lần chạy mau

---

### 2.2. POST /api/mst_resignreason/getbyreasonid
- **Mô tả**: Lấy chi tiết một lý do nghỉ việc theo mã `ReasonID`.
- **Request Body (`MstResignReasonGetByReasonIDReq`)**:
  | Tên tham số | Kiểu dữ liệu | Bắt buộc? | Mặc định | Ý nghĩa |
  | --- | --- | --- | --- | --- |
  | ReasonID | `string?` | Không | `null` | Mã lý do nghỉ việc |
  | OrgID | `string?` | Không | `null` | Mã tổ chức |

- **Quy tắc xử lý**:
  1. Nếu `ReasonID` rỗng hoặc khoảng trắng, trả về kết quả thành công với dữ liệu là `null`.
  2. Gọi `GetXAsync` để tìm bản ghi phù hợp và trả về phần tử đầu tiên (`rows.FirstOrDefault()`).
- **Mã lỗi**: Chưa xác minh.
- **Ví dụ Request**:
    ```json
    {
      "OrgID": "4020320001",
      "ReasonID": "R01"
    }
    ```
- **Ví dụ Response**: Chưa có lần chạy mau

---

### 2.3. POST /api/mst_resignreason/getallactive
- **Mô tả**: Lấy toàn bộ danh sách lý do nghỉ việc đang hoạt động (`FlagActive = "1"`).
- **Request Body**: Không yêu cầu (hoặc body rỗng).
- **Quy tắc xử lý**:
  1. Gọi `GetXAsync` với `FlagActive = "1"`, không giới hạn phân trang (`recordCount = int.MaxValue`).
  2. Trả về danh sách nếu có dữ liệu, ngược lại trả về `null` nếu danh sách rỗng.
- **Mã lỗi**: Chưa xác minh.
- **Ví dụ Request**: Trống (`{}`) hoặc `null`.
- **Ví dụ Response**: Chưa có lần chạy mau

---

### 2.4. POST /api/mst_resignreason/create
- **Mô tả**: Thêm mới một lý do nghỉ việc.
- **Request Body (`Mst_ResignReason`)**:
  | Tên tham số | Kiểu dữ liệu | Bắt buộc? | Mặc định | Ý nghĩa |
  | --- | --- | --- | --- | --- |
  | OrgID | `string` | Có | - | Mã tổ chức |
  | ResignReason | `string` | Có | - | Tên lý do nghỉ việc |
  | ReasonDesc | `string?` | Không | `null` | Mô tả |
  | Remark | `string?` | Không | `null` | Ghi chú |

- **Quy tắc xử lý**:
  1. Nếu body rỗng (`req == null`), trả về lỗi mặc định thông qua `PortResult.Fail("Mst_ResignReason_Create", "Check.strJson", null)`.
  2. Sinh mã `ReasonID` tự động thông qua sequence `Seq_ReasonID` (`PortSeq.NextAsync`) và format thành chuỗi định dạng (ví dụ: `REASONID...`).
  3. Gọi phương thức kiểm tra và lưu dữ liệu trong transaction riêng (`CreateXAsync`).
  4. Kiểm tra `OrgID` bắt buộc tồn tại và có `FlagActive` hợp lệ trong bảng `Mst_Org`.
  5. Kiểm tra `ReasonID` và tên `ResignReason` không được trùng lặp trong cùng `OrgID`.
  6. Lưu bản ghi mới vào CSDL với `FlagActive = "1"`.
- **Mã lỗi (`ErridnLoyalty.*`)**:
  - `Mst_ResignReason_Create`: Lỗi chung hoặc body rỗng.
  - `Mst_ResignReason_CreateX_InvalidOrgID`: Thiếu hoặc không hợp lệ `OrgID`.
  - `Mst_ResignReason_CreateX_InvalidReasonID`: Thiếu hoặc không hợp lệ `ReasonID`.
  - `Mst_ResignReason_CheckDB_ReasonIDExist`: Mã lý do đã tồn tại.
  - `Mst_Org_CheckDB_OrgNotFound`: Không tìm thấy tổ chức.
  - `Mst_Org_CheckDB_FlagActiveNotMatched`: Tổ chức không ở trạng thái hoạt động.
  - `Mst_ResignReason_CreateX_InvalidResignReason`: Tên lý do nghỉ việc trống.
  - `Mst_ResignReason_CheckResignReason_ResignReasonExist`: Tên lý do nghỉ việc đã tồn tại.
- **Ví dụ Request**:
    ```json
    {
      "OrgID": "4020320001",
      "ResignReason": "Lý do cá nhân",
      "ReasonDesc": "Cá nhân xin nghỉ",
      "Remark": "Không có"
    }
    ```
- **Ví dụ Response**: Chưa có lần chạy mau

---

### 2.5. POST /api/mst_resignreason/import
- **Mô tả**: Import danh sách lý do nghỉ việc từ file Excel (`multipart/form-data`).
- **Request / Form Data**:
  | Tên tham số | Kiểu dữ liệu | Bắt buộc? | Mặc định | Ý nghĩa |
  | --- | --- | --- | --- | --- |
  | file | `IFormFile` | Có | - | File Excel định dạng `.xlsx` |

- **Quy tắc xử lý**:
  1. Kiểm tra file upload hợp lệ (không rỗng, đúng phần mở rộng `.xlsx`).
  2. Đọc dữ liệu từ file Excel sử dụng `PortExcel.ReadSheet` theo template 3 cột: `ResignReason`, `OrgID`, `ReasonDesc`.
  3. Kiểm tra số lượng cột tiêu đề và tính hợp lệ của dữ liệu (dòng dữ liệu không được trống, tên lý do không được trống).
  4. Duyệt qua từng dòng, sinh `ReasonID` tự động bằng `Guid.NewGuid().ToString()` và gọi `CreateXAsync` theo từng transaction riêng biệt (hành vi import một phần: nếu lỗi ở dòng k, các dòng trước đó đã được lưu thành công, dừng lại và trả lỗi của dòng k).
- **Mã lỗi**:
  - `Mst_ResignReason_Import`: File không hợp lệ, không có dữ liệu, hoặc lỗi trong quá trình import.
- **Ví dụ Request**: Gửi qua form-data với key `file` đính kèm file Excel.
- **Ví dụ Response**: Chưa có lần chạy mau

---

### 2.6. POST /api/mst_resignreason/update
- **Mô tả**: Cập nhật thông tin lý do nghỉ việc.
- **Request Body (`Mst_ResignReason`)**:
  | Tên tham số | Kiểu dữ liệu | Bắt buộc? | Mặc định | Ý nghĩa |
  | --- | --- | --- | --- | --- |
  | OrgID | `string` | Có | - | Mã tổ chức |
  | ReasonID | `string` | Có | - | Mã lý do nghỉ việc cần sửa |
  | ResignReason | `string` | Có | - | Tên lý do nghỉ việc mới |
  | ReasonDesc | `string?` | Không | `null` | Mô tả mới |
  | FlagActive | `string?` | Không | `"1"` | Trạng thái mới ("1" hoặc "0") |
  | Remark | `string?` | Không | `null` | Ghi chú mới |

- **Quy tắc xử lý**:
  1. Kiểm tra body rỗng.
  2. Kiểm tra bản ghi tồn tại trong CSDL dựa trên `OrgID` và `ReasonID`.
  3. Kiểm tra tổ chức (`OrgID`) hợp lệ và đang hoạt động.
  4. Kiểm tra tên lý do mới không được trống và không bị trùng lặp với bản ghi khác trong cùng tổ chức.
  5. Cập nhật các trường thông tin được phép (`ResignReason`, `ReasonDesc`, `FlagActive`, `Remark`, `LogLUDTimeUTC`, `LogLUBy`) và lưu vào CSDL.
- **Mã lỗi**:
  - `Mst_ResignReason_Update`: Lỗi chung / body rỗng.
  - `Mst_ResignReason_CheckDB_ReasonIDNotFound`: Không tìm thấy bản ghi cần sửa.
  - `Mst_Org_CheckDB_OrgNotFound`: Không tìm thấy tổ chức.
  - `Mst_Org_CheckDB_FlagActiveNotMatched`: Tổ chức không hoạt động.
  - `Mst_ResignReason_UpdateX_InvalidResignReason`: Tên lý do nghỉ việc trống.
  - `Mst_ResignReason_CheckResignReason_ResignReasonExist`: Tên lý do nghỉ việc bị trùng.
- **Ví dụ Request**:
    ```json
    {
      "OrgID": "4020320001",
      "ReasonID": "R01",
      "ResignReason": "Nghỉ hưu theo chế độ",
      "ReasonDesc": "Cập nhật mô tả",
      "FlagActive": "1"
    }
    ```
- **Ví dụ Response**: Chưa có lần chạy mau

---

### 2.7. POST /api/mst_resignreason/delete
- **Mô tả**: Xóa một lý do nghỉ việc theo `OrgID` và `ReasonID`.
- **Request Body (`Mst_ResignReason`)**:
  | Tên tham số | Kiểu dữ liệu | Bắt buộc? | Mặc định | Ý nghĩa |
  | --- | --- | --- | --- | --- |
  | OrgID | `string?` | Không | `null` | Mã tổ chức |
  | ReasonID | `string` | Có | - | Mã lý do nghỉ việc cần xóa |

- **Quy tắc xử lý**:
  1. Kiểm tra `ReasonID` không được rỗng (nếu rỗng trả về `invalid_ResignReason`).
  2. Kiểm tra tổ chức tồn tại và hoạt động.
  3. Kiểm tra bản ghi cần xóa tồn tại trong CSDL.
  4. Thực hiện xóa bản ghi (`Remove`) và lưu thay đổi.
- **Mã lỗi**:
  - `invalid_ResignReason`: `ReasonID` trống.
  - `Mst_Org_CheckDB_OrgNotFound`: Không tìm thấy tổ chức.
  - `Mst_Org_CheckDB_FlagActiveNotMatched`: Tổ chức không hoạt động.
  - `Mst_ResignReason_CheckDB_ReasonIDNotFound`: Không tìm thấy bản ghi cần xóa.
  - `Mst_ResignReason_Delete`: Lỗi ngoại lệ hệ thống.
- **Ví dụ Request**:
    ```json
    {
      "OrgID": "4020320001",
      "ReasonID": "R03"
    }
    ```
- **Ví dụ Response**: Chưa có lần chạy mau

---

### 2.8. POST /api/mst_resignreason/deletemultiple
- **Mô tả**: Xóa nhiều lý do nghỉ việc hàng loạt.
- **Request Body (`List<Mst_ResignReason>`)**: Mảng các đối tượng chứa `OrgID` và `ReasonID`.
- **Quy tắc xử lý**:
  1. Kiểm tra mảng request không được rỗng.
  2. Duyệt qua từng phần tử trong mảng, thực hiện kiểm tra và xóa tuần tự (hành vi xóa một phần: nếu phần tử thứ k gặp lỗi, dừng lại và trả về lỗi, các phần tử trước đó đã được xóa thành công).
- **Mã lỗi**: Giống các mã lỗi của endpoint `delete`.
- **Ví dụ Request**:
    ```json
    [
      { "OrgID": "4020320001", "ReasonID": "R02" },
      { "OrgID": "4020320001", "ReasonID": "R03" }
    ]
    ```
- **Ví dụ Response**: Chưa có lần chạy mau

---

### 2.9. POST /api/mst_resignreason/export
- **Mô tả**: Xuất danh sách lý do nghỉ việc ra file Excel.
- **Request Body (`MstResignReasonExportReq`)**:
  | Tên tham số | Kiểu dữ liệu | Bắt buộc? | Mặc định | Ý nghĩa |
  | --- | --- | --- | --- | --- |
  | OrgID | `string?` | Không | `null` | Lọc theo mã tổ chức |
  | KeyWord | `string?` | Không | `null` | Từ khóa tìm kiếm |
  | FlagActive | `string?` | Không | `null` | Trạng thái |

- **Quy tắc xử lý**:
  1. Gọi `GetXAsync` để lấy toàn bộ dữ liệu thỏa mãn điều kiện lọc.
  2. Nếu danh sách kết quả rỗng, trả về lỗi `Không có dữ liệu`.
  3. Tạo file Excel qua thư viện `PortExcel` sử dụng từ điển cột xuất chuẩn và trả về đường dẫn tải file (`Url`).
- **Mã lỗi**:
  - `Mst_ResignReason_Export`: Không có dữ liệu hoặc lỗi tạo file.
- **Ví dụ Request**:
    ```json
    {
      "OrgID": "4020320001"
    }
    ```
- **Ví dụ Response**: Chưa có lần chạy mau

---

### 2.10. POST /api/mst_resignreason/exporttemplate
- **Mô tả**: Tải file Excel mẫu để chuẩn bị dữ liệu import.
- **Request Body**: Không yêu cầu.
- **Quy tắc xử lý**:
  1. Khởi tạo danh sách mẫu (danh sách rỗng).
  2. Lấy từ điển cột mẫu (`GetImportDicColumsTemplate`).
  3. Tạo file Excel mẫu và trả về đường dẫn tải file (`Url`).
- **Mã lỗi**:
  - `Mst_ResignReason_ExportTemplate`: Lỗi trong quá trình tạo file mẫu.
- **Ví dụ Request**: Trống (`{}`) hoặc không cần body.
- **Ví dụ Response**: Chưa có lần chạy mau

---

## 3. Khác biệt so với nguồn & GAP
- **Phân quyền**: Hệ thống nguồn sử dụng session (`orgUserUI.OrgId`), hệ đích (`MiniLoyalty`) nhận trực tiếp qua tham số `OrgID` trong body request.
- **Cấu hình chuỗi kết nối và NetworkID**: Lấy cấu hình mặc định từ `IConfiguration` (`Biz_NetworkID` mặc định là `"1001"`).
- **Log & Transaction**: Sử dụng giao dịch độc lập (`BeginTransactionAsync`) cho từng thao tác xử lý dòng đơn hoặc import/xóa hàng loạt để hỗ trợ hành vi thực thi một phần (partial commit/rollback).

---

## 4. Nhật ký thay đổi
- `#174`, `#185`, `#192`, `#236`, `#240`, `#241`: Port 1-1 các chức năng Search, GetByReasonID, Create, Update, Delete, Import, Export và ExportTemplate từ mã nguồn gốc sang MiniLoyalty.
