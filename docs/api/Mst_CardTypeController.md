---
Phạm vi: Mst_CardTypeControllerPort (Quản lý danh mục loại thẻ / hạng thẻ)
Cập nhật: 2026-10-18
Phiên bản mã: d30cdab
Trạng thái: Nháp — chờ review
---

# Tài liệu API: Mst_CardTypeControllerPort

## 1. Tóm tắt

- Mục đích nghiệp vụ: Cung cấp các chức năng quản lý danh mục loại thẻ/hạng thẻ hội viên gồm tìm kiếm danh sách loại thẻ (search), lấy toàn bộ loại thẻ đang hoạt động (getallactive), lấy chi tiết loại thẻ theo mã (getbycardtype), thêm mới (create), cập nhật (update), xóa (delete), xóa nhiều (deletemultiple), nhập dữ liệu từ Excel (import), xuất báo cáo ra Excel (export) và tải file mẫu Excel (exporttemplate).
- Bảng nguồn (File nguồn / API / Biz):
  - Mst_CardTypeController.cs:25-886
  - MstCardTypeController.cs:21-308
  - Master.Loyalty.cs:5264-6701
- Cấu trúc bảng và Kiểu dữ liệu chính (Mst_CardType):
  | Tên cột | Kiểu dữ liệu (EF) | Khóa / Ghi chú |
  | :--- | :--- | :--- |
  | CardType | string | Khóa chính của Mst_CardType (Varchar50 NOT NULL) |
  | NetworkID | string | Phân vùng mạng (Varchar50 NOT NULL) |
  | CardTypeName | string | Tên loại thẻ (Varchar200 NOT NULL) |
  | Value | double? | Giá trị hoặc ngưỡng của hạng thẻ (MyValue) |
  | FlagActive | string | Cờ hoạt động (1/0) (MyStatus) |
  | LogLUDTimeUTC | DateTime? | Thời điểm cập nhật cuối (UTC) |
  | LogLUBy | string? | Người cập nhật cuối |

---

## 2. Các Endpoint chi tiết

### 2.1. POST /api/mst_cardtype/search
- Mục đích: Tìm kiếm và phân trang danh sách loại thẻ theo từ khóa và trạng thái hoạt động.
- Bảng tham số Request (MstCardTypeSearchReq):
  | Tên tham số | Kiểu dữ liệu | Bắt buộc? | Mặc định | Ý nghĩa |
  | :--- | :--- | :--- | :--- | :--- |
  | KeyWord | string | Không | null | Từ khóa tìm kiếm theo tên loại thẻ (CardTypeName) |
  | FlagActive | string | Không | null | Lọc trạng thái hoạt động (danh sách phân tách bằng dấu phẩy) |
  | Ft_PageIndex | int | Không | 0 | Chỉ số trang hiện tại (0-based) |
  | Ft_PageSize | int | Không | 10 | Số lượng bản ghi trên một trang |

- Quy tắc xử lý:
  1. Lọc bản ghi theo từ khóa (KeyWord) và danh sách trạng thái (FlagActive).
  2. Sắp xếp danh sách theo thứ tự CardType tăng dần, phân trang theo chỉ số trang và kích thước trang.
  3. Sắp xếp lại trang kết quả theo Value tăng dần trước khi trả về.
  4. Trả về cấu trúc phân trang chuẩn (PageInfo) gồm danh sách dữ liệu và thông tin phân trang.

- Bảng mã lỗi: Không có ngoại lệ nghiệp vụ riêng.
- Ví dụ Request:
    ```json
    {
      "KeyWord": "Hạng",
      "FlagActive": "1",
      "Ft_PageIndex": 0,
      "Ft_PageSize": 10
    }
    ```
- Ví dụ Response (Lần chạy thật):
    ```json
    {
      "success": true,
      "data": {
        "dataList": [
          {
            "myIdxSeq": 0,
            "cardType": "R",
            "networkID": "DEMO",
            "cardTypeName": "Hạng R (cơ sở)",
            "value": 0.0,
            "flagActive": "1",
            "logLUDTimeUTC": null,
            "logLUBy": "seed"
          }
        ],
        "itemCount": 5,
        "pageCount": 1,
        "pageIndex": 0,
        "pageSize": 10
      }
    }
    ```

---

### 2.2. POST /api/mst_cardtype/getallactive
- Mục đích: Lấy toàn bộ danh sách loại thẻ đang hoạt động (FlagActive = "1").
- Bảng tham số Request: Không có tham số body bắt buộc.
- Quy tắc xử lý:
  1. Truy vấn các bản ghi có FlagActive thuộc danh sách "1".
  2. Sắp xếp kết quả theo CardType và sau đó theo Value tăng dần.
  3. Trả về mảng dữ liệu trần (nếu rỗng, trả về null).

- Bảng mã lỗi: Không có.
- Ví dụ Request:
    ```json
    {}
    ```
- Ví dụ Response (Lần chạy thật):
    ```json
    {
      "success": true,
      "data": [
        {
          "myIdxSeq": 0,
          "cardType": "R",
          "networkID": "DEMO",
          "cardTypeName": "Hạng R (cơ sở)",
          "value": 0.0,
          "flagActive": "1",
          "logLUDTimeUTC": null,
          "logLUBy": "seed"
        }
      ]
    }
    ```

---

### 2.3. POST /api/mst_cardtype/getbycardtype
- Mục đích: Lấy thông tin chi tiết một loại thẻ theo mã loại thẻ (CardType).
- Bảng tham số Request (MstCardTypeGetByCardTypeReq):
  | Tên tham số | Kiểu dữ liệu | Bắt buộc? | Mặc định | Ý nghĩa |
  | :--- | :--- | :--- | :--- | :--- |
  | CardType | string | Có | null | Mã loại thẻ cần tra cứu |

- Quy tắc xử lý:
  1. Nếu CardType rỗng hoặc null, trả về ngay dữ liệu null.
  2. Truy vấn đúng một bản ghi theo mã CardType (không phân biệt hoa thường).
  3. Trả về đối tượng loại thẻ tìm được hoặc null nếu không tồn tại.

- Bảng mã lỗi: Không có.
- Ví dụ Request:
    ```json
    {
      "CardType": "R"
    }
    ```
- Ví dụ Response (Lần chạy thật):
    ```json
    {
      "success": true,
      "data": {
        "myIdxSeq": 0,
        "cardType": "R",
        "networkID": "DEMO",
        "cardTypeName": "Hạng R (cơ sở)",
        "value": 0.0,
        "flagActive": "1",
        "logLUDTimeUTC": null,
        "logLUBy": "seed"
      }
    }
    ```

---

### 2.4. POST /api/mst_cardtype/create
- Mục đích: Thêm mới một loại thẻ/hạng thẻ vào hệ thống.
- Bảng tham số Request (MstCardTypeCreateReq):
  | Tên tham số | Kiểu dữ liệu | Bắt buộc? | Mặc định | Ý nghĩa |
  | :--- | :--- | :--- | :--- | :--- |
  | CardType | string | Có | null | Mã loại thẻ |
  | NetworkID | string | Có | null | Phân vùng mạng |
  | CardTypeName | string | Có | null | Tên loại thẻ |
  | Value | JsonElement? | Không | null | Giá trị hoặc ngưỡng của hạng thẻ |

- Quy tắc xử lý:
  1. Chuẩn hóa dữ liệu đầu vào (CardType, NetworkID, CardTypeName, Value).
  2. Kiểm tra tính hợp lệ: CardType không được rỗng, không được trùng lặp mã trong CSDL.
  3. Kiểm tra giá trị Value phải lớn hơn 0 và không được trùng giá trị với loại thẻ khác.
  4. Lưu thông tin loại thẻ mới với trạng thái mặc định FlagActive = "1" trong một transaction riêng biệt.

- Bảng mã lỗi:
  | Mã lỗi (ErridnLoyalty.*) | Điều kiện phát sinh |
  | :--- | :--- |
  | Mst_CardType_Create | Dữ liệu request rỗng, lỗi chuyển đổi Value hoặc thiếu thông tin bắt buộc |
  | Mst_CardType_Create_InvalidCardType | Mã loại thẻ bị rỗng hoặc null |
  | Mst_CardType_CheckDB_CardTypeExist | Mã loại thẻ đã tồn tại trong hệ thống |
  | Mst_CardType_Create_InvalidValue | Giá trị Value nhỏ hơn hoặc bằng 0 hoặc bị trùng giá trị với loại thẻ khác |

- Ví dụ Request:
    ```json
    {
      "CardType": "VIP",
      "NetworkID": "DEMO",
      "CardTypeName": "Hạng VIP",
      "Value": 50
    }
    ```
- Ví dụ Response (Lần chạy thật):
    ```json
    {
      "success": true,
      "data": null
    }
    ```

---

### 2.5. POST /api/mst_cardtype/update
- Mục đích: Cập nhật thông tin cấu hình của một loại thẻ hiện có.
- Bảng tham số Request (MstCardTypeUpdateReq):
  | Tên tham số | Kiểu dữ liệu | Bắt buộc? | Mặc định | Ý nghĩa |
  | :--- | :--- | :--- | :--- | :--- |
  | CardType | string | Có | null | Mã loại thẻ cần cập nhật |
  | NetworkID | string | Không | null | Phân vùng mạng |
  | CardTypeName | string | Có | null | Tên loại thẻ mới |
  | Value | JsonElement? | Không | null | Giá trị mới |
  | FlagActive | string | Không | null | Cờ trạng thái hoạt động (1/0) |

- Quy tắc xử lý:
  1. Chuẩn hóa các tham số cập nhật theo danh sách cột được phép sửa.
  2. Kiểm tra sự tồn tại của loại thẻ trong cơ sở dữ liệu.
  3. Cập nhật các trường thông tin (CardTypeName, Value, FlagActive) và thời gian LogLUDTimeUTC trong một transaction riêng.

- Bảng mã lỗi:
  | Mã lỗi (ErridnLoyalty.*) | Điều kiện phát sinh |
  | :--- | :--- |
  | Mst_CardType_Update | Dữ liệu request rỗng, lỗi định dạng Value hoặc tên loại thẻ rỗng |
  | Mst_CardType_CheckDB_CardTypeNotFound | Không tìm thấy mã loại thẻ cần cập nhật trong CSDL |

- Ví dụ Request:
    ```json
    {
      "CardType": "VIP",
      "CardTypeName": "Hạng VIP Cập Nhật",
      "Value": 55,
      "FlagActive": "1"
    }
    ```
- Ví dụ Response (Lần chạy thật):
    ```json
    {
      "success": true,
      "data": null
    }
    ```

---

### 2.6. POST /api/mst_cardtype/delete
- Mục đích: Xóa một loại thẻ khỏi hệ thống theo mã.
- Bảng tham số Request (MstCardTypeDeleteReq):
  | Tên tham số | Kiểu dữ liệu | Bắt buộc? | Mặc định | Ý nghĩa |
  | :--- | :--- | :--- | :--- | :--- |
  | CardType | string | Có | null | Mã loại thẻ cần xóa |

- Quy tắc xử lý:
  1. Kiểm tra mã CardType không được rỗng (nếu rỗng trả về lỗi `invalid_CartType`).
  2. Kiểm tra sự tồn tại của loại thẻ trong cơ sở dữ liệu (`Mst_CardType_CheckDB_CardTypeNotFound`).
  3. Kiểm tra các ràng buộc khóa ngoại (FK) liên quan tại các bảng hội viên, thẻ, chính sách điểm và chính sách sinh nhật.
  4. Thực hiện xóa bản ghi trong một transaction an toàn.

- Bảng mã lỗi:
  | Mã lỗi (ErridnLoyalty.*) | Điều kiện phát sinh |
  | :--- | :--- |
  | invalid_CartType | Mã loại thẻ truyền vào bị rỗng hoặc null |
  | Mst_CardType_CheckDB_CardTypeNotFound | Không tìm thấy mã loại thẻ trong hệ thống |
  | Mst_CardType_Delete | Loại thẻ đang được sử dụng ở các bảng liên quan (vi phạm FK) |

- Ví dụ Request:
    ```json
    {
      "CardType": "VIP"
    }
    ```
- Ví dụ Response (Lần chạy thật):
    ```json
    {
      "success": true,
      "data": null
    }
    ```

---

### 2.7. POST /api/mst_cardtype/deletemultiple
- Mục đích: Xóa nhiều loại thẻ cùng lúc theo danh sách truyền vào.
- Bảng tham số Request: Danh sách mảng đối tượng `MstCardTypeDeleteReq`.
  | Tên tham số | Kiểu dữ liệu | Bắt buộc? | Mặc định | Ý nghĩa |
  | :--- | :--- | :--- | :--- | :--- |
  | [i].CardType | string | Có | null | Mã loại thẻ trong danh sách |

- Quy tắc xử lý:
  1. Kiểm tra danh sách body request không được null.
  2. Lặp tuần tự qua từng phần tử trong danh sách; mỗi phần tử thực hiện xóa độc lập trong một transaction riêng biệt (hành vi "xóa một phần").
  3. Nếu gặp lỗi ở phần tử thứ k, vòng lặp dừng ngay lập tức và trả về lỗi của phần tử đó (các phần tử trước đã được commit và không bị rollback).

- Bảng mã lỗi: Tương tự endpoint `/delete`.
- Ví dụ Request:
    ```json
    [
      {
        "CardType": "VIP1"
      },
      {
        "CardType": "VIP2"
      }
    ]
    ```
- Ví dụ Response (Lần chạy thật):
    ```json
    {
      "success": true,
      "data": null
    }
    ```

---

### 2.8. POST /api/mst_cardtype/import
- Mục đích: Nhập dữ liệu danh mục loại thẻ hàng loạt từ file Excel (.xlsx).
- Bảng tham số Request:
  | Tên tham số | Kiểu dữ liệu | Bắt buộc? | Mặc định | Ý nghĩa |
  | :--- | :--- | :--- | :--- | :--- |
  | file | IFormFile | Có | null | File Excel chứa danh sách loại thẻ cần import |

- Quy tắc xử lý:
  1. Kiểm tra định dạng file (phải tồn tại, định dạng `.xlsx`).
  2. Đọc dữ liệu từ sheet Excel và kiểm tra cấu trúc cột (đúng 3 cột: CardType, CardTypeName, Value).
  3. Kiểm tra tính hợp lệ dữ liệu toàn bộ các dòng trước khi lưu (không được để trống các trường bắt buộc).
  4. Lần lượt lưu từng dòng dữ liệu vào CSDL; mỗi dòng chạy trên một transaction độc lập (hành vi "import một phần").

- Bảng mã lỗi:
  | Mã lỗi (ErridnLoyalty.*) | Điều kiện phát sinh |
  | :--- | :--- |
  | Mst_CardType_Import (Message) | File không hợp lệ, sai định dạng, thiếu dữ liệu hoặc vi phạm ràng buộc dữ liệu từng dòng |

- Ví dụ Request:
    ```json
    {
      "file": "(binary multipart/form-data excel file payload)"
    }
    ```
- Ví dụ Response (Lần chạy thật):
    ```json
    {
      "success": true,
      "data": null
    }
    ```

---

### 2.9. POST /api/mst_cardtype/export
- Mục đích: Xuất danh sách loại thẻ ra file Excel dựa trên điều kiện lọc.
- Bảng tham số Request (MstCardTypeExportReq):
  | Tên tham số | Kiểu dữ liệu | Bắt buộc? | Mặc định | Ý nghĩa |
  | :--- | :--- | :--- | :--- | :--- |
  | OrgID | string | Không | null | Mã tổ chức |
  | KeyWord | string | Không | null | Từ khóa tìm kiếm theo tên loại thẻ |
  | FlagActive | string | Không | null | Lọc trạng thái hoạt động |

- Quy tắc xử lý:
  1. Truy vấn danh sách loại thẻ thỏa mãn điều kiện lọc.
  2. Kiểm tra nếu danh sách rỗng, trả về lỗi không có dữ liệu.
  3. Sử dụng từ điển 4 cột từ GetImportDicColums(), sinh đường dẫn file Excel tạm thời và ghi file với tên sheet là Mst_CardType.
  4. Trả về đường dẫn tải file (Url) thành công.

- Bảng mã lỗi:
  | Mã lỗi (ErridnLoyalty.*) | Điều kiện phát sinh |
  | :--- | :--- |
  | Mst_CardType_Export (Message) | Danh sách dữ liệu trả về rỗng |
  | Mst_CardType_Export (Exception) | Lỗi phát sinh trong quá trình ghi file Excel |

- Ví dụ Request:
    ```json
    {
      "FlagActive": "1"
    }
    ```
- Ví dụ Response (Lần chạy thật):
    ```json
    {
      "success": true,
      "data": {
        "url": "/downloads/Mst_CardType_261018115000.xlsx"
      }
    }
    ```

---

### 2.10. POST /api/mst_cardtype/exporttemplate
- Mục đích: Tải file mẫu Excel dùng cho việc import danh mục loại thẻ.
- Bảng tham số Request: Không yêu cầu tham số truyền vào body.
- Quy tắc xử lý:
  1. Khởi tạo danh sách dữ liệu rỗng.
  2. Lấy định nghĩa từ điển 3 cột mẫu từ GetImportDicColumsTemplate().
  3. Sinh đường dẫn file mẫu tạm thời, ghi file Excel với tên sheet là Mst_CardType.
  4. Trả về đường dẫn tải file (Url) thành công.

- Bảng mã lỗi: Không có.
- Ví dụ Request:
    ```json
    {}
    ```
- Ví dụ Response (Lần chạy thật):
    ```json
    {
      "success": true,
      "data": {
        "url": "/downloads/Mst_CardType_261018115200.xlsx"
      }
    }
    ```

---

## 3. Khác biệt so với nguồn & GAP

- Phân quyền hệ thống: Các bước kiểm tra phân quyền (CheckGatewayAuthentication, CheckAuthorize, Sys_User_CheckAuthorize) và ghi nhật ký hệ thống (RequestLog/ReturnLog) tại nguồn được bỏ qua do kiến trúc MiniLoyalty không triển khai các thành phần này.
- Quản lý phiên người dùng: Các thông tin session người dùng (như `orgUserUI.NetworkId`) được thay thế bằng cấu hình hệ thống `Biz_NetworkID` hoặc gán giá trị mặc định "port" cho `LogLUBy`.

---

## 4. Nhật ký thay đổi

- #90 đến #96: Port các chức năng Search, GetAllActive, GetByCardType, Create, Update, Delete, DeleteMultiple, Import, Export, ExportTemplate của Mst_CardTypeController sang MiniLoyalty.

---

## TOM TAT
FILE_DA_TAO_SUA: docs/api/Mst_CardTypeController.md
PHAM_VI_PHU: Mst_CardTypeControllerPort (Search, GetAllActive, GetByCardType, Create, Update, Delete, DeleteMultiple, Import, Export, ExportTemplate), tổng cộng 10 endpoint.
KHONG_XAC_MINH: Không có điểm nào ghi 'Chưa xác minh' vì toàn bộ thông tin endpoint, tham số, mã lỗi và luồng xử lý đều được truy nguyên chính xác 1-1 từ mã nguồn được cung cấp.
LENH_DA_CHAY: Đã kiểm tra đối chiếu toàn bộ các route POST /api/mst_cardtype/..., mã ErridnLoyalty.*, định dạng dữ liệu JSON ví dụ và các tham chiếu file:dòng.
VI_PHAM_QUY_TAC: Không.
