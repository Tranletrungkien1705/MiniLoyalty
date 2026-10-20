Phạm vi: Controllers/Port/Mst_PolicyExpenseTypeControllerPort.cs, Models/Port/Mst_PolicyExpenseType.cs
Cập nhật: 2026-10-16
Phiên bản mã: 7973495
Trạng thái: Nháp — chờ review

# Tài liệu API: Mst_PolicyExpenseTypeController

## 1. Tóm tắt
- **Mục đích nghiệp vụ**: Quản lý danh mục chính sách loại chi phí (`Mst_PolicyExpenseType`), hỗ trợ tìm kiếm phân trang, lấy danh sách hoạt động, lấy chi tiết theo mã chính sách và lưu dữ liệu (xóa toàn bộ, ghi mới và lưu lịch sử vào `Mst_PolicyExpenseTypeHist`).
- **Bảng nguồn**: 
  - Gate controller: `13.ClientGate/V10/idn.Loyalty.WebAPI/Controllers/Mst_PolicyExpenseTypeController.cs`
  - API: `MstPolicyExpenseTypeController.cs:96-164`
  - Biz: `idn.Loyalty.Biz/Master.Loyalty.cs` (`WAS_Mst_PolicyExpenseType_Get`, `Mst_PolicyExpenseType_Get`, `Mst_PolicyExpenseType_GetX`, `WAS_Mst_PolicyExpenseType_Save`, `Mst_PolicyExpenseType_Save`, `Mst_PolicyExpenseType_SaveX`)
  - DDL: `11.BackEnd/.../Migrate/20221006.QUYNHNTN/20221026.CreateDatabase.sql:156-170` (cùng các script bổ sung cột `FlagDiscount`, `DiscountRate`)
- **Cấu trúc bảng (Mst_PolicyExpenseType)**:
  | Tên cột | Kiểu dữ liệu EF | Khóa | Ghi chú |
  | --- | --- | --- | --- |
  | `PolicyExpenseTypeNo` | `string` | PK (Phần 1) | Mã chính sách chi phí (Varchar50, NOT NULL) |
  | `ExpenseType` | `string` | PK (Phần 2) | Mã loại chi phí (Varchar50, NOT NULL) |
  | `NetworkID` | `string` | - | Mã mạng (Varchar50, NOT NULL) |
  | `ExpenseTypeNameActual` | `string` | - | Tên loại đối tượng thực tế (Varchar200, NOT NULL) |
  | `FlagPoint` | `string?` | - | Đối tượng áp dụng (MyStatus) |
  | `FlagPointRank` | `string?` | - | Cờ tính điểm dịch vụ (MyStatus) |
  | `AmountRate` | `double?` | - | Tỉ lệ tích điểm |
  | `MaxRankReviewPoint` | `double?` | - | Điểm xét hạng tối đa |
  | `MaxAccumulationPoint` | `double?` | - | Điểm tích lũy tối đa |
  | `FlagCountService` | `string?` | - | Cờ tính điểm dịch vụ (MyStatus) |
  | `FlagDiscount` | `string?` | - | Có chiết khấu dịch vụ (MyStatus) |
  | `DiscountRate` | `double?` | - | Tỉ lệ chiết khấu dịch vụ (0..100) |
  | `FlagActive` | `string?` | - | Cờ hoạt động '1'/'0' (MyStatus) |
  | `Remark` | `string?` | - | Ghi chú (Varchar400) |
  | `LogLUDTimeUTC` | `DateTime?` | - | Thời điểm cập nhật UTC |
  | `LogLUBy` | `string?` | - | Người cập nhật (Varchar50) |

---

## 2. Các Endpoint

### 2.1. POST /api/mst_policyexpensetype/search
- **Mô tả**: Tìm kiếm và phân trang danh mục chính sách loại chi phí.
- **Request Body** (`MstPolicyExpenseTypeSearchReq`):
  | Tên tham số | Kiểu | Bắt buộc? | Mặc định | Ý nghĩa |
  | --- | --- | --- | --- | --- |
  | `KeyWord` | `string?` | Không | `null` | Từ khóa tìm kiếm (Gate nguồn không thực hiện lọc từ khóa) |
  | `FlagActive` | `string?` | Không | `null` | Cờ hoạt động, lọc theo toán tử IN (phân cách bằng dấu phẩy) |
  | `Ft_PageIndex` | `int` | Không | `0` | Chỉ số trang (0-based) |
  | `Ft_PageSize` | `int` | Không | `10` | Số dòng trên mỗi trang |

- **Quy tắc xử lý**:
  1. Tính toán phân trang từ `Ft_PageIndex` và `Ft_PageSize` (`start = pageIndex * pageSize`).
  2. Lọc `FlagActive` theo kiểu IN (tách dấu phẩy, trim, không phân biệt hoa/thường). `KeyWord` không lọc.
  3. Sắp xếp giảm dần theo `LogLUDTimeUTC desc`.
  4. Đếm tổng bản ghi khớp (`itemCount`).
  5. Cắt trang theo phạm vi `[start, start + pageSize)`. Nếu `pageSize = 0`, trả về danh sách rỗng.
  6. LEFT JOIN với bảng `Mst_ExpenseType` để lấy thêm cột `ExpenseTypeName`.
  7. Trả về đối tượng phân trang chuẩn `PageInfo<object>`.

- **Mã lỗi**: Không phát sinh ngoại lệ đặc biệt ngoài lỗi hệ thống.
- **Ví dụ Request**:
    POST /api/mst_policyexpensetype/search
    Content-Type: application/json

    {
      "KeyWord": "",
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
            "PolicyExpenseTypeNo": "PET01",
            "ExpenseType": "EX01",
            "NetworkID": "DEMO",
            "ExpenseTypeNameActual": "Thuc te DV 01",
            "ExpenseTypeName": "Chi phi dich vu",
            "FlagPoint": "1",
            "FlagPointRank": "1",
            "AmountRate": 0.05,
            "MaxRankReviewPoint": 1000.0,
            "MaxAccumulationPoint": 5000.0,
            "FlagCountService": "1",
            "FlagDiscount": "1",
            "DiscountRate": 10.0,
            "FlagActive": "1",
            "Remark": "Seed demo",
            "LogLUDTimeUTC": "2026-10-10T15:00:00Z",
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

### 2.2. POST /api/mst_policyexpensetype/getallactive
- **Mô tả**: Lấy toàn bộ các chính sách loại chi phí đang hoạt động (`FlagActive = "1"`).
- **Request Body**: Không có.
- **Quy tắc xử lý**:
  1. Gọi truy vấn ngầm với `flagActive = "1"`, `recordStart = 0`, `recordCount = 123456000`.
  2. Trả về mảng trần `List<object>`; nếu rỗng trả về `Data = null`.
- **Mã lỗi**: Không có.
- **Ví dụ Request**:
    POST /api/mst_policyexpensetype/getallactive
    Content-Type: application/json

    {}

- **Ví dụ Response**:
    {
      "Success": true,
      "Data": [
        {
          "MyIdxSeq": 0,
          "PolicyExpenseTypeNo": "PET01",
          "ExpenseType": "EX01",
          "NetworkID": "DEMO",
          "ExpenseTypeNameActual": "Thuc te DV 01",
          "ExpenseTypeName": "Chi phi dich vu",
          "FlagPoint": "1",
          "FlagPointRank": "1",
          "AmountRate": 0.05,
          "MaxRankReviewPoint": 1000.0,
          "MaxAccumulationPoint": 5000.0,
          "FlagCountService": "1",
          "FlagDiscount": "1",
          "DiscountRate": 10.0,
          "FlagActive": "1",
          "Remark": "Seed demo",
          "LogLUDTimeUTC": "2026-10-10T15:00:00Z",
          "LogLUBy": "seed"
        }
      ]
    }

---

### 2.3. POST /api/mst_policyexpensetype/getbypolicyexpensetypeno
- **Mô tả**: Lấy chi tiết chính sách loại chi phí theo mã chính sách (`PolicyExpenseTypeNo`).
- **Request Body** (`MstPolicyExpenseTypeGetByPolicyExpenseTypeNoReq`):
  | Tên tham số | Kiểu | Bắt buộc? | Mặc định | Ý nghĩa |
  | --- | --- | --- | --- | --- |
  | `PolicyExpenseTypeNo` | `string?` | Có | `null` | Mã chính sách chi phí cần tra cứu |

- **Quy tắc xử lý**:
  1. Nếu `PolicyExpenseTypeNo` rỗng hoặc `null`, trả về ngay `Data = null` (không thực hiện truy vấn DB).
  2. Truy vấn với giới hạn `recordCount = 1`.
  3. Không lọc theo `FlagActive` (bản ghi có `FlagActive = "0"` vẫn có thể được lấy).
  4. Trả về đối tượng đơn (object) nếu tìm thấy, ngược lại trả về `null`.
- **Mã lỗi**: Không có.
- **Ví dụ Request**:
    POST /api/mst_policyexpensetype/getbypolicyexpensetypeno
    Content-Type: application/json

    {
      "PolicyExpenseTypeNo": "PET01"
    }

- **Ví dụ Response**:
    {
      "Success": true,
      "Data": {
        "MyIdxSeq": 0,
        "PolicyExpenseTypeNo": "PET01",
        "ExpenseType": "EX01",
        "NetworkID": "DEMO",
        "ExpenseTypeNameActual": "Thuc te DV 01",
        "ExpenseTypeName": "Chi phi dich vu",
        "FlagPoint": "1",
        "FlagPointRank": "1",
        "AmountRate": 0.05,
        "MaxRankReviewPoint": 1000.0,
        "MaxAccumulationPoint": 5000.0,
        "FlagCountService": "1",
        "FlagDiscount": "1",
        "DiscountRate": 10.0,
        "FlagActive": "1",
        "Remark": "Seed demo",
        "LogLUDTimeUTC": "2026-10-10T15:00:00Z",
        "LogLUBy": "seed"
      }
    }

---

### 2.4. POST /api/mst_policyexpensetype/save
- **Mô tả**: Lưu danh sách chính sách loại chi phí. Hành vi nguồn thực hiện lấy mã sequence, kiểm tra ràng buộc từng dòng, **XÓA TOÀN BỘ** dữ liệu cũ trong bảng `Mst_PolicyExpenseType`, insert danh sách mới và ghi lưu vết lịch sử vào bảng `Mst_PolicyExpenseTypeHist`.
- **Request Body** (`MstPolicyExpenseTypeSaveReq`):
  | Tên tham số | Kiểu | Bắt buộc? | Mặc định | Ý nghĩa |
  | --- | --- | --- | --- | --- |
  | `Lst_Mst_PolicyExpenseType` | `List<MstPolicyExpenseTypeSaveRowReq>?` | Có | `null` | Danh sách các dòng chi phí cần lưu |

- **Quy tắc xử lý & Kiểm tra dữ liệu**:
  1. Nếu request hoặc `Lst_Mst_PolicyExpenseType` bằng `null`, trả về lỗi `Mst_PolicyExpenseType_Save` (hoặc `strJson null`).
  2. Lấy mã sequence mới từ bảng sequence (tăng độc lập kể cả khi lưu thất bại sau đó).
  3. Nếu mảng đầu vào rỗng `[]`, bảng `Mst_PolicyExpenseType` sẽ bị xóa sạch hoàn toàn (trở về 0 dòng).
  4. Duyệt kiểm tra từng dòng:
     - Kiểm tra tồn tại và trạng thái hoạt động của `ExpenseType` trong bảng `Mst_ExpenseType`.
     - Kiểm tra `DiscountRate` nằm trong khoảng từ `0` đến `100`.
     - Kiểm tra chéo: nếu `FlagDiscount != "1"` thì `DiscountRate` phải bằng `0`.
  5. Mở Transaction, thực hiện xóa bảng `Mst_PolicyExpenseType`, thêm mới danh sách chính sách và thêm lịch sử vào `Mst_PolicyExpenseTypeHist`.

- **Bảng mã lỗi**:
  | Mã lỗi (`ErridnLoyalty.*`) | Điều kiện phát sinh |
  | --- | --- |
  | `Mst_PolicyExpenseType_Save` | Request `null`, danh sách thiếu, hoặc ngoại lệ phát sinh trong quá trình thực thi Transaction cơ sở dữ liệu. |
  | `Mst_ExpenseType_CheckDB_ExpenseTypeNotFound` | `ExpenseType` không tồn tại trong bảng `Mst_ExpenseType`. |
  | `Mst_ExpenseType_CheckDB_FlagActiveNotMatched` | `ExpenseType` có `FlagActive` khác `"1"`. |
  | `Mst_PolicyExpenseType_Save_InvalidValue` | Vi phạm quy tắc validate `DiscountRate` (< 0 hoặc > 100, hoặc `FlagDiscount` khác `"1"` mà `DiscountRate <> 0`). |

- **Ví dụ Request**:
    POST /api/mst_policyexpensetype/save
    Content-Type: application/json

    {
      "Lst_Mst_PolicyExpenseType": [
        {
          "ExpenseType": "EX01",
          "ExpenseTypeNameActual": "Thuc te DV 01",
          "FlagPoint": "1",
          "FlagPointRank": "1",
          "AmountRate": 0.05,
          "MaxRankReviewPoint": 1000.0,
          "MaxAccumulationPoint": 5000.0,
          "FlagCountService": "1",
          "FlagDiscount": "1",
          "DiscountRate": 10.0,
          "Remark": "Ghi chu cap nhat"
        }
      ]
    }

- **Ví dụ Response**:
    {
      "Success": true,
      "Data": null
    }

---

## 3. Khác biệt so với nguồn & GAP
- **Phân quyền & Check Auth**: Hệ thống đích chưa tích hợp hệ thống phân quyền (bỏ qua `CheckGatewayAuthentication`, `CheckAuthorize`, `Sys_User_CheckAuthorize`).
- **OrgID / NetworkID**: Nguồn lấy qua header/session, tại bản port cấu hình lấy từ `Biz_NetworkID` hoặc mặc định `"1001"`, và không tham gia lọc trực tiếp vào WHERE clause của các lệnh tìm kiếm.
- **drAbilityOfUser**: Nguồn lấy nhưng bị comment không sử dụng, bản port đã loại bỏ.

---

## 4. Nhật ký thay đổi
- `#190` → Port chức năng Search.
- `#191` → Port chức năng GetAllActive.
- `#192` → Port chức năng GetByPolicyExpenseTypeNo và Save.
