Phạm vi: Controllers/Port/Mst_ParamPromotionControllerPort.cs, Models/Port/Mst_ParamPromotion.cs
Cập nhật: 2026-10-16
Phiên bản mã: 7973495
Trạng thái: Nháp — chờ review

# Tài liệu API: Mst_ParamPromotionController

## 1. Tóm tắt
- **Mục đích nghiệp vụ**: Quản lý tham số chương trình khuyến mại (`Mst_ParamPromotion`), hỗ trợ tìm kiếm phân trang, lấy danh sách hoạt động, lấy chi tiết theo mã chương trình và lưu dữ liệu (xóa toàn bộ và ghi mới).
- **Bảng nguồn**: 
  - Gate controller: `13.ClientGate/V10/idn.Loyalty.WebAPI/Controllers/Mst_ParamPromotionController.cs`
  - Service: `11.BackEnd/V10/idn.Loyalty.Biz.Web/Controllers/MstParamPromotionController.cs`
  - Biz LIVE: `idn.Loyalty.Biz/Master.Loyalty.cs` (`WAS_Mst_ParamPromotion_Get`, `Mst_ParamPromotion_Get`, `Mst_ParamPromotion_GetX`, `WAS_Mst_ParamPromotion_Save`, `Mst_ParamPromotion_Save`, `Mst_ParamPromotion_SaveX`)
  - DDL: `11.BackEnd/V10/05.Refs.Biz/Migrate/20221006.QUYNHNTN/20221026.CreateDatabase.sql:322-333`
- **Cấu trúc bảng (Mst_ParamPromotion)**:
  | Tên cột | Kiểu dữ liệu EF | Khóa | Ghi chú |
  | --- | --- | --- | --- |
  | `PrProgramCode` | `string` | PK (Phần 1) | Mã chương trình khuyến mại (Varchar50, NOT NULL) |
  | `ParamPrType` | `string` | PK (Phần 2) | Loại áp dụng (Varchar50, NOT NULL) |
  | `NetworkID` | `string` | - | NetworkID (Varchar50, NOT NULL) |
  | `PrProgramName` | `string?` | - | Tên chương trình (Varchar200, lấy từ `Mst_PromotionProgram` qua LEFT JOIN khi select) |
  | `QtyDateBefore` | `double?` | - | Số ngày trước |
  | `QtyDateAfter` | `double?` | - | Số ngày sau |
  | `FlagActive` | `string?` | - | Cờ hoạt động ('1' / '0') |
  | `Remark` | `string?` | - | Ghi chú (Varchar400) |
  | `LogLUDTimeUTC` | `DateTime?` | - | Thời điểm cập nhật UTC |
  | `LogLUBy` | `string?` | - | Người cập nhật (Varchar50) |

---

## 2. Các Endpoint

### 2.1. POST /api/mst_parampromotion/search
- **Mô tả**: Tìm kiếm và phân trang danh sách tham số chương trình khuyến mại.
- **Request Body** (`MstParamPromotionSearchReq`):
  | Tên tham số | Kiểu | Bắt buộc? | Mặc định | Ý nghĩa |
  | --- | --- | --- | --- | --- |
  | `KeyWord` | `string?` | Không | `null` | Từ khóa tìm kiếm (Nhưng gate nguồn KHÔNG xử lý lọc từ khóa) |
  | `FlagActive` | `string?` | Không | `null` | Cờ hoạt động, lọc theo toán tử IN (tách bằng dấu phẩy) |
  | `Ft_PageIndex` | `int` | Không | `0` | Chỉ số trang (0-based) |
  | `Ft_PageSize` | `int` | Không | `10` | Số dòng trên mỗi trang |

- **Quy tắc xử lý**:
  1. Đọc `Ft_PageIndex` và `Ft_PageSize` để tính `start = pageIndex * pageSize`.
  2. Lọc `FlagActive` theo dạng IN (tách dấu phẩy, trim, không phân biệt hoa/thường). `KeyWord` và `OrgID` không tham gia điều kiện lọc.
  3. Sắp xếp kết quả giảm dần theo `LogLUDTimeUTC desc`, sắp xếp phụ `PrProgramCode asc`, `ParamPrType asc`.
  4. Đếm tổng số bản ghi khớp (`itemCount`).
  5. Cắt trang theo phạm vi `[start, start + pageSize)`. Nếu `PageSize = 0`, trả về danh sách rỗng.
  6. LEFT JOIN với bảng `Mst_PromotionProgram` để lấy `PrProgramName`.
  7. Trả về cấu trúc phân trang chuẩn `PageInfo<object>`.

- **Mã lỗi**: Không phát sinh lỗi đặc biệt ngoài ngoại lệ hệ thống.
- **Ví dụ Request**:
    POST /api/mst_parampromotion/search
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
            "PrProgramCode": "PR01",
            "ParamPrType": "TYPE_A",
            "QtyDateAfter": 10.0,
            "QtyDateBefore": 5.0,
            "FlagActive": "1",
            "Remark": "Ghi chu 1",
            "LogLUBy": "seed",
            "LogLUDTimeUTC": "2026-10-10T15:00:00Z",
            "PrProgramName": "Ten param 1"
          }
        ],
        "ItemCount": 1,
        "PageCount": 1,
        "PageIndex": 0,
        "PageSize": 10
      }
    }

---

### 2.2. POST /api/mst_parampromotion/getallactive
- **Mô tả**: Lấy toàn bộ các bản ghi đang hoạt động (`FlagActive = "1"`).
- **Request Body**: Không có.
- **Quy tắc xử lý**:
  1. Gọi hàm truy vấn ngầm với `flagActive = "1"`, `recordStart = 0`, `recordCount = 123456000`.
  2. Nếu danh sách không rỗng, trả về mảng trần; nếu rỗng, trả về `Data = null`.
- **Mã lỗi**: Không có.
- **Ví dụ Request**:
    POST /api/mst_parampromotion/getallactive
    Content-Type: application/json

    {}

- **Ví dụ Response**:
    {
      "Success": true,
      "Data": [
        {
          "MyIdxSeq": 0,
          "PrProgramCode": "PR01",
          "ParamPrType": "TYPE_A",
          "QtyDateAfter": 10.0,
          "QtyDateBefore": 5.0,
          "FlagActive": "1",
          "Remark": "Ghi chu 1",
          "LogLUBy": "seed",
          "LogLUDTimeUTC": "2026-10-10T15:00:00Z",
          "PrProgramName": "Ten param 1"
        }
      ]
    }

---

### 2.3. POST /api/mst_parampromotion/getbyprprogramcode
- **Mô tả**: Lấy chi tiết thông tin tham số theo mã chương trình khuyến mại.
- **Request Body** (`MstParamPromotionGetByPrProgramCodeReq`):
  | Tên tham số | Kiểu | Bắt buộc? | Mặc định | Ý nghĩa |
  | --- | --- | --- | --- | --- |
  | `PrProgramCode` | `string?` | Có | `null` | Mã chương trình khuyến mại cần tra cứu |

- **Quy tắc xử lý**:
  1. Nếu `PrProgramCode` rỗng hoặc `null`, trả về `Data = null`.
  2. Truy vấn theo `PrProgramCode` với giới hạn 1 bản ghi (`recordCount = 1`).
  3. Trả về đối tượng đơn (object) nếu tìm thấy, ngược lại trả về `null`.
- **Mã lỗi**: Không có.
- **Ví dụ Request**:
    POST /api/mst_parampromotion/getbyprprogramcode
    Content-Type: application/json

    {
      "PrProgramCode": "PR01"
    }

- **Ví dụ Response**:
    {
      "Success": true,
      "Data": {
        "MyIdxSeq": 0,
        "PrProgramCode": "PR01",
        "ParamPrType": "TYPE_A",
        "QtyDateAfter": 10.0,
        "QtyDateBefore": 5.0,
        "FlagActive": "1",
        "Remark": "Ghi chu 1",
        "LogLUBy": "seed",
        "LogLUDTimeUTC": "2026-10-10T15:00:00Z",
        "PrProgramName": "Ten param 1"
      }
    }

---

### 2.4. POST /api/mst_parampromotion/save
- **Mô tả**: Lưu danh sách tham số chương trình khuyến mại. Hành vi nghiệp vụ thực hiện **XÓA TOÀN BỘ** dữ liệu hiện tại trong bảng rồi ghi lại danh sách mới trong một Transaction.
- **Request Body** (`MstParamPromotionSaveReq`):
  | Tên tham số | Kiểu | Bắt buộc? | Mặc định | Ý nghĩa |
  | --- | --- | --- | --- | --- |
  | `Lst_Mst_ParamPromotion` | `List<Mst_ParamPromotion>?` | Có | `null` | Danh sách các tham số cần lưu |

- **Quy tắc xử lý & Kiểm tra dữ liệu**:
  1. Kiểm tra `req` hoặc `Lst_Mst_ParamPromotion` nếu `null` trả về lỗi `strJson null`.
  2. Chuẩn hoá `PrProgramCode` (trim và chuyển thành chữ hoa).
  3. Kiểm tra từng chương trình trong `Mst_PromotionProgram`:
     - Nếu không tồn tại chương trình -> Lỗi `Mst_PromotionProgram_CheckDB_PrProgramCodeNotFound`.
     - Nếu `FlagActive` không chứa `"1"` -> Lỗi `Mst_PromotionProgram_CheckDB_FlagActiveNotMatched`.
     - Nếu nằm ngoài khoảng hiệu lực (`EffDateStart <= ngày hiện tại (UTC+7) <= EffDateEnd`) -> Lỗi `Mst_BirthPolicy_CheckDB_FlagShowNotMatched` (giữ nguyên tên lỗi theo mã nguồn gốc).
  4. Gán giá trị bắt buộc: `NetworkID` (lấy từ cấu hình `Biz_NetworkID` hoặc mặc định `"1001"`), `FlagActive = "1"`, `LogLUDTimeUTC = DateTime.UtcNow`, `LogLUBy = "port"`.
  5. Mở Transaction, thực hiện xóa toàn bộ bảng (`ExecuteDeleteAsync()`), thêm mới toàn bộ danh sách hợp lệ và Commit.

- **Bảng mã lỗi**:
  | Mã lỗi (`ErridnLoyalty.*`) | Điều kiện phát sinh |
  | --- | --- |
  | `Mst_ParamPromotion_Save` | Request hoặc danh sách truyền vào bị `null`, hoặc lỗi phát sinh trong quá trình thực thi Transaction cơ sở dữ liệu. |
  | `Mst_PromotionProgram_CheckDB_PrProgramCodeNotFound` | `PrProgramCode` không tồn tại trong bảng `Mst_PromotionProgram`. |
  | `Mst_PromotionProgram_CheckDB_FlagActiveNotMatched` | `FlagActive` của chương trình khuyến mại không hợp lệ (không chứa `"1"`). |
  | `Mst_BirthPolicy_CheckDB_FlagShowNotMatched` | Ngày hiện tại nằm ngoài khoảng hiệu lực (`EffDateStart` - `EffDateEnd`) của chương trình. |

- **Ví dụ Request**:
    POST /api/mst_parampromotion/save
    Content-Type: application/json

    {
      "Lst_Mst_ParamPromotion": [
        {
          "PrProgramCode": "PR01",
          "ParamPrType": "TYPE_A",
          "QtyDateBefore": 5.0,
          "QtyDateAfter": 10.0,
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
- **Phân quyền**: `drAbilityOfUser` tại nguồn bị comment, không port (hệ thống coi như người dùng có toàn quyền xem).
- **Log / Sequence**: Đoạn mã xử lý Sequence trong hàm Save bị comment tại nguồn nên không đưa vào phiên bản port.
- **Tên mã lỗi**: Giữ nguyên tên hàm lỗi nguồn `Mst_BirthPolicy_CheckDB_FlagShowNotMatched` mặc dù đây là Controller kiểm tra chương trình khuyến mại (`Mst_PromotionProgram`).

---

## 4. Nhật ký thay đổi
- `#183` → Port chức năng Search.
- `#184` → Port chức năng GetAllActive.
- `#185` → Port chức năng GetByPrProgramCode.
- `#186` → Port chức năng Save.
