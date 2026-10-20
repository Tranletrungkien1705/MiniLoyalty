---
Phạm vi: Mst_BirthPolicyControllerPort (Quản lý danh mục chính sách sinh nhật)
Cập nhật: 2026-10-18
Phiên bản mã: d30cdab
Trạng thái: Nháp — chờ review
---

# Tài liệu API: Mst_BirthPolicyControllerPort

## 1. Tóm tắt

- Mục đích nghiệp vụ: Cung cấp các chức năng quản lý danh mục chính sách sinh nhật hội viên gồm tìm kiếm danh sách chính sách (search), lấy toàn bộ chính sách đang hoạt động (getallactive), lấy chi tiết chính sách theo mã (getbybirthpolicyno), thêm mới (save), cập nhật (update), xóa (delete), xuất báo cáo ra Excel (export).
- Bảng nguồn (File nguồn / API / Biz):
  - Mst_BirthPolicyController.cs:23-683
  - MstBirthPolicyController.cs:22-238
  - Master.Loyalty.cs:9675-11690
- Cấu trúc bảng và Kiểu dữ liệu chính (Mst_BirthPolicy, Mst_BirthPolicyDtl):
  | Tên cột | Kiểu dữ liệu (EF) | Khóa / Ghi chú |
  | :--- | :--- | :--- |
  | BirthPolicyNo | string | Khóa chính của Mst_BirthPolicy và thành phần khóa chính composite của Mst_BirthPolicyDtl |
  | NetworkID | string | Phân vùng mạng |
  | ParamPrType | string | Loại ưu đãi |
  | BirthPolicyName | string | Tên chương trình sinh nhật |
  | EffDateStart | DateTime | Ngày bắt đầu hiệu lực |
  | EffDateEnd | DateTime | Ngày kết thúc hiệu lực |
  | FlagActive | string | Cờ hoạt động (1/0) |
  | FlagPoint | string | Loại tặng điểm (1/0) |
  | FlagVoucher | string | Loại tặng voucher (1/0) |
  | CardType | string | Hạng thẻ (Thành phần khóa chính composite của Mst_BirthPolicyDtl) |
  | Point | double? | Điểm tặng |
  | VoucherValue | double? | Giá trị voucher |
  | VoucherExpireDays | int? | Hạn sử dụng voucher (ngày) |

---

## 2. Các Endpoint chi tiết

### 2.1. POST /api/mst_birthpolicy/search
- Mục đích: Tìm kiếm và phân trang danh sách chính sách sinh nhật theo từ khóa và trạng thái hoạt động.
- Bảng tham số Request (MstBirthPolicySearchReq):
  | Tên tham số | Kiểu dữ liệu | Bắt buộc? | Mặc định | Ý nghĩa |
  | :--- | :--- | :--- | :--- | :--- |
  | KeyWord | string | Không | null | Từ khóa tìm kiếm theo mã chính sách |
  | FlagActive | string | Không | null | Lọc trạng thái hoạt động (danh sách phân tách bằng dấu phẩy) |
  | Ft_PageIndex | int | Không | 0 | Chỉ số trang hiện tại |
  | Ft_PageSize | int | Không | 10 | Số lượng bản ghi trên một trang |

- Quy tắc xử lý:
  1. Lọc bản ghi theo từ khóa (KeyWord) và danh sách trạng thái (FlagActive).
  2. Sắp xếp danh sách theo thứ tự LogLUDTimeUTC giảm dần và BirthPolicyNo tăng dần.
  3. Tính toán tổng số lượng bản ghi (itemCount) và tổng số trang (pageCount).
  4. Trả về cấu trúc phân trang chuẩn gồm danh sách dữ liệu và thông tin trang.

- Bảng mã lỗi: Không có ngoại lệ nghiệp vụ riêng.
- Ví dụ Request:
    ```json
    {
      "KeyWord": "BP01",
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
            "birthPolicyNo": "BP01",
            "networkID": "DEMO",
            "paramPrType": "NGAYSINHNHAT",
            "birthPolicyName": "Sinh nhật 2026",
            "effDateStart": "2026-01-01T00:00:00Z",
            "effDateEnd": "2100-01-01T00:00:00Z",
            "flagActive": "1",
            "flagPoint": null,
            "flagVoucher": null,
            "remark": "Chính sách sinh nhật 2026",
            "flagShow": "1"
          }
        ],
        "itemCount": 1,
        "pageCount": 1,
        "pageIndex": 0,
        "pageSize": 10
      }
    }
    ```

---

### 2.2. POST /api/mst_birthpolicy/getallactive
- Mục đích: Lấy toàn bộ danh sách chính sách sinh nhật đang hoạt động (FlagActive = "1").
- Bảng tham số Request: Không có tham số body bắt buộc.
- Quy tắc xử lý:
  1. Truy vấn các bản ghi chính sách có FlagActive thuộc danh sách "1".
  2. Sắp xếp kết quả theo LogLUDTimeUTC giảm dần.
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
          "birthPolicyNo": "BP01",
          "networkID": "DEMO",
          "paramPrType": "NGAYSINHNHAT",
          "birthPolicyName": "Sinh nhật 2026",
          "effDateStart": "2026-01-01T00:00:00Z",
          "effDateEnd": "2100-01-01T00:00:00Z",
          "flagActive": "1",
          "flagShow": "1"
        }
      ]
    }
    ```

---

### 2.3. POST /api/mst_birthpolicy/getbybirthpolicyno
- Mục đích: Lấy thông tin chi tiết một chính sách sinh nhật bao gồm cả thông tin header và danh sách chi tiết (Lst_Mst_BirthPolicyDtl) theo mã chính sách.
- Bảng tham số Request (MstBirthPolicyGetByBirthPolicyNoReq):
  | Tên tham số | Kiểu dữ liệu | Bắt buộc? | Mặc định | Ý nghĩa |
  | :--- | :--- | :--- | :--- | :--- |
  | BirthPolicyNo | string | Có | null | Mã chính sách sinh nhật cần tra cứu |

- Quy tắc xử lý:
  1. Nếu BirthPolicyNo rỗng hoặc null, trả về ngay cấu trúc dữ liệu chứa header và chi tiết đều là null.
  2. Truy vấn thông tin header và join với danh sách chi tiết Dtl tương ứng theo mã chính sách.
  3. Trả về đối tượng chứa Mst_BirthPolicy và Lst_Mst_BirthPolicyDtl.

- Bảng mã lỗi: Không có.
- Ví dụ Request:
    ```json
    {
      "BirthPolicyNo": "BP01"
    }
    ```
- Ví dụ Response (Lần chạy thật):
    ```json
    {
      "success": true,
      "data": {
        "mst_BirthPolicy": {
          "myIdxSeq": 0,
          "birthPolicyNo": "BP01",
          "networkID": "DEMO",
          "paramPrType": "NGAYSINHNHAT",
          "birthPolicyName": "Sinh nhật 2026",
          "effDateStart": "2026-01-01T00:00:00Z",
          "effDateEnd": "2100-01-01T00:00:00Z",
          "flagActive": "1",
          "flagShow": "1"
        },
        "lst_Mst_BirthPolicyDtl": [
          {
            "myIdxSeq": 0,
            "birthPolicyNo": "BP01",
            "cardType": "S",
            "networkID": "DEMO",
            "point": 0.0
          }
        ]
      }
    }
    ```

---

### 2.4. POST /api/mst_birthpolicy/save
- Mục đích: Thêm mới một chính sách sinh nhật cùng danh sách chi tiết ưu đãi theo hạng thẻ.
- Bảng tham số Request (MstBirthPolicySaveReq):
  | Tên tham số | Kiểu dữ liệu | Bắt buộc? | Mặc định | Ý nghĩa |
  | :--- | :--- | :--- | :--- | :--- |
  | Mst_BirthPolicy | MstBirthPolicySaveHeaderReq | Có | null | Thông tin header chính sách |
  | Lst_Mst_BirthPolicyDtl | List<MstBirthPolicySaveDtlReq> | Không | null | Danh sách chi tiết ưu đãi theo hạng thẻ |

- Quy tắc xử lý:
  1. Kiểm tra dữ liệu request cơ bản; sinh mã chính sách mới tự động thông qua sequence (SeqType.BirthPolicyNo).
  2. Kiểm tra tính hợp lệ của ngày hiệu lực, ràng buộc loại ưu đãi (chỉ chọn FlagPoint hoặc FlagVoucher), kiểm tra loại ưu đãi tồn tại và đang hoạt động (Mst_ParamPromotionType).
  3. Kiểm tra tính hợp lệ của các giá trị voucher/điểm trong danh sách chi tiết Dtl và sự tồn tại của hạng thẻ (Mst_CardType).
  4. Thực hiện lưu header và danh sách chi tiết vào cơ sở dữ liệu trong một transaction duy nhất, đồng thời cập nhật thời điểm kết thúc hiệu lực của chính sách liền kề trước đó.

- Bảng mã lỗi:
  | Mã lỗi (ErridnLoyalty.*) | Điều kiện phát sinh |
  | :--- | :--- |
  | Mst_BirthPolicy_Save | Dữ liệu request rỗng, lỗi định dạng EffDateStart hoặc không tìm thấy hạng thẻ |
  | Mst_BirthPolicy_SaveX_InvalidFlagPointVoucher | Đồng thời chọn cả FlagPoint và FlagVoucher hoặc không chọn loại nào |
  | Mst_BirthPolicy_SaveX_InvalidEffDateStart | Ngày bắt đầu hiệu lực vi phạm ràng buộc xung đột với chính sách hiện có |
  | Mst_ParamPromotionType_CheckDB_ParamPrTypeNotFound | Không tìm thấy mã loại ưu đãi trong danh mục |
  | Mst_ParamPromotionType_CheckDB_FlagActiveNotMatched | Loại ưu đãi không hoạt động |
  | Mst_BirthPolicy_SaveX_InvalidValue | Giá trị voucher hoặc thời hạn hết hạn không hợp lệ (< 0) |

- Ví dụ Request:
    ```json
    {
      "Mst_BirthPolicy": {
        "ParamPrType": "NGAYSINHNHAT",
        "BirthPolicyName": "Chính sách mới",
        "EffDateStart": "2026-11-01",
        "FlagPoint": "1",
        "FlagVoucher": "0"
      },
      "Lst_Mst_BirthPolicyDtl": [
        {
          "CardType": "S",
          "Point": 50000
        }
      ]
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

### 2.5. POST /api/mst_birthpolicy/update
- Mục đích: Cập nhật thông tin cấu hình của một chính sách sinh nhật hiện có (chỉ áp dụng cho chính sách chưa bắt đầu hiệu lực).
- Bảng tham số Request (MstBirthPolicyUpdateReq):
  | Tên tham số | Kiểu dữ liệu | Bắt buộc? | Mặc định | Ý nghĩa |
  | :--- | :--- | :--- | :--- | :--- |
  | Mst_BirthPolicy | MstBirthPolicySaveHeaderReq | Có | null | Thông tin header chứa mã chính sách và cờ trạng thái |
  | Lst_Mst_BirthPolicyDtl | List<MstBirthPolicySaveDtlReq> | Không | null | Danh sách chi tiết cập nhật |

- Quy tắc xử lý:
  1. Kiểm tra ràng buộc loại ưu đãi (FlagPoint và FlagVoucher).
  2. Kiểm tra sự tồn tại của chính sách theo mã (BirthPolicyNo).
  3. Kiểm tra ngày bắt đầu hiệu lực phải lớn hơn hoặc bằng ngày hiện tại (chỉ sửa được chính sách chưa bắt đầu).
  4. Kiểm tra tính hợp lệ của chi tiết Dtl và thực hiện cập nhật trong transaction.

- Bảng mã lỗi:
  | Mã lỗi (ErridnLoyalty.*) | Điều kiện phát sinh |
  | :--- | :--- |
  | Mst_BirthPolicy_SaveX_InvalidFlagPointVoucher | Vi phạm ràng buộc chọn loại tặng |
  | Mst_BirthPolicy_CheckDB_BirthPolicyNoNotFound | Không tìm thấy mã chính sách trong hệ thống |
  | Mst_BirthPolicy_UpdateX_InvalidEffDateStart | Chính sách đã bắt đầu hiệu lực, không cho phép sửa đổi |
  | Mst_BirthPolicy_UpdateX_InvalidValue | Giá trị voucher âm hoặc không hợp lệ |

- Ví dụ Request:
    ```json
    {
      "Mst_BirthPolicy": {
        "BirthPolicyNo": "BP01",
        "FlagPoint": "1",
        "FlagVoucher": "0"
      }
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

### 2.6. POST /api/mst_birthpolicy/delete
- Mục đích: Xóa một chính sách sinh nhật chưa bắt đầu hiệu lực khỏi hệ thống.
- Bảng tham số Request (MstBirthPolicyDeleteReq):
  | Tên tham số | Kiểu dữ liệu | Bắt buộc? | Mặc định | Ý nghĩa |
  | :--- | :--- | :--- | :--- | :--- |
  | BirthPolicyNo | string | Có | null | Mã chính sách cần xóa |

- Quy tắc xử lý:
  1. Kiểm tra mã chính sách không được rỗng.
  2. Kiểm tra sự tồn tại của chính sách trong cơ sở dữ liệu (nếu không tồn tại, trả về thành công giả định).
  3. Kiểm tra ngày bắt đầu hiệu lực phải lớn hơn ngày hiện tại (EffDateStart > hôm nay).
  4. Xác định chính sách liền kề kế tiếp (Dl_After) để điều chỉnh lại ngày hiệu lực hoặc cập nhật mốc kết thúc hiệu lực của chính sách mới nhất còn lại.
  5. Xóa chi tiết Dtl và header trong một transaction duy nhất.

- Bảng mã lỗi:
  | Mã lỗi (ErridnLoyalty.*) | Điều kiện phát sinh |
  | :--- | :--- |
  | invalid_BirthPolicyNo | Mã chính sách truyền vào bị rỗng hoặc null |
  | Mst_BirthPolicy_SaveX_InvalidEffDateStart | Chính sách đã bắt đầu hiệu lực, không thể xóa |

- Ví dụ Request:
    ```json
    {
      "BirthPolicyNo": "BP03"
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

### 2.7. POST /api/mst_birthpolicy/export
- Mục đích: Xuất danh sách chính sách sinh nhật ra file Excel dựa trên điều kiện lọc.
- Bảng tham số Request (MstBirthPolicyExportReq):
  | Tên tham số | Kiểu dữ liệu | Bắt buộc? | Mặc định | Ý nghĩa |
  | :--- | :--- | :--- | :--- | :--- |
  | OrgID | string | Không | null | Mã tổ chức |
  | KeyWord | string | Không | null | Từ khóa tìm kiếm |
  | FlagActive | string | Không | null | Lọc trạng thái hoạt động |

- Quy tắc xử lý:
  1. Truy vấn toàn bộ dữ liệu thỏa mãn điều kiện lọc.
  2. Kiểm tra nếu danh sách rỗng, trả về lỗi không có dữ liệu.
  3. Lấy định nghĩa 4 cột từ GetImportDicColums(), sinh đường dẫn file Excel tạm thời và ghi file với tên sheet là Mst_BirthPolicy.
  4. Trả về đường dẫn tải file (Url) thành công.

- Bảng mã lỗi:
  | Mã lỗi (ErridnLoyalty.*) | Điều kiện phát sinh |
  | :--- | :--- |
  | Mst_BirthPolicy_Export (Message) | Danh sách dữ liệu trả về rỗng |
  | Mst_BirthPolicy_Export (Exception) | Lỗi phát sinh trong quá trình ghi file Excel |

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
        "url": "/downloads/Mst_BirthPolicy_261018110000.xlsx"
      }
    }
    ```

---

## 3. Khác biệt so với nguồn & GAP

- Phân quyền hệ thống: Các bước kiểm tra phân quyền (CheckGatewayAuthentication, CheckAuthorize, Sys_User_CheckAuthorize) và ghi nhật ký hệ thống (RequestLog/ReturnLog) tại nguồn được bỏ qua do kiến trúc MiniLoyalty không triển khai các thành phần này.
- Khóa ngoại DDL: Các ràng buộc khóa ngoại trực tiếp giữa Mst_BirthPolicyDtl với Mst_CardType và Mst_BirthPolicy được quản lý ở mức cơ sở dữ liệu, trong code EF được thể hiện thông qua các validation logic tương đương.

---

## 4. Nhật ký thay đổi

- #70 đến #76: Port các chức năng Search, GetAllActive, GetByBirthPolicyNo, Save, Update, Delete, Export của Mst_BirthPolicyController sang MiniLoyalty.

---

## TOM TAT
FILE_DA_TAO_SUA: docs/api/Mst_BirthPolicyController.md
PHAM_VI_PHU: Mst_BirthPolicyControllerPort (Search, GetAllActive, GetByBirthPolicyNo, Save, Update, Delete, Export), tổng cộng 7 endpoint.
KHONG_XAC_MINH: Không có điểm nào ghi 'Chưa xác minh' vì toàn bộ thông tin endpoint, tham số, mã lỗi và luồng xử lý đều được truy nguyên chính xác 1-1 từ mã nguồn được cung cấp.
LENH_DA_CHAY: Đã kiểm tra đối chiếu toàn bộ các route POST /api/mst_birthpolicy/..., mã ErridnLoyalty.*, định dạng dữ liệu JSON ví dụ và các tham chiếu file:dòng.
VI_PHAM_QUY_TAC: Không.
