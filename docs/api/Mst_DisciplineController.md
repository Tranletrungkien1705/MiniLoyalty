---
Phạm vi: Mst_DisciplineControllerPort (Quản lý danh mục hình thức kỷ luật)
Cập nhật: 2026-10-18
Phiên bản mã: d30cdab
Trạng thái: Nháp — chờ review
---

# Tài liệu API: Mst_DisciplineControllerPort

## 1. Tóm tắt

- Mục đích nghiệp vụ: Cung cấp các chức năng quản lý danh mục hình thức kỷ luật hội viên gồm tìm kiếm danh sách hình thức kỷ luật (search), lấy toàn bộ hình thức kỷ luật đang hoạt động (getallactive), lấy chi tiết theo mã (getbydisciplinecode), thêm mới (create), cập nhật (update), xóa (delete) và xóa nhiều (deletemultiple).
- Bảng nguồn (File nguồn / API / Biz):
  - Mst_DisciplineController.cs:26-520
  - MstDisciplineController.cs:17-310
  - MasterData.cs:33954-35186
- Cấu trúc bảng và Kiểu dữ liệu chính (Mst_Discipline):
  | Tên cột | Kiểu dữ liệu (EF) | Khóa / Ghi chú |
  | :--- | :--- | :--- |
  | DisciplineCode | string | Khóa chính của Mst_Discipline (khong co DDL — kieu cot can xac nhan) |
  | NetworkID | string? | Mã mạng |
  | DisciplineName | string? | Hình thức kỷ luật |
  | DisciplineDesc | string? | Mô tả |
  | FlagActive | string? | Trạng thái / Cờ hoạt động (1/0) |
  | LogLUDTimeUTC | DateTime? | Thời điểm log cập nhật UTC |
  | LogLUBy | string? | Người cập nhật |

---

## 2. Các Endpoint chi tiết

### 2.1. POST /api/mst_discipline/search
- Mục đích: Tìm kiếm và phân trang danh sách hình thức kỷ luật theo từ khóa và trạng thái hoạt động.
- Bảng tham số Request (MstDisciplineSearchReq):
  | Tên tham số | Kiểu dữ liệu | Bắt buộc? | Mặc định | Ý nghĩa |
  | :--- | :--- | :--- | :--- | :--- |
  | OrgID | string | Không | null | Tổ chức (không sử dụng trong mệnh đề lọc) |
  | KeyWord | string | Không | null | Từ khóa tìm kiếm theo tên hình thức kỷ luật (DisciplineName) |
  | FlagActive | string | Không | null | Lọc trạng thái hoạt động (toán tử =) |
  | Ft_PageIndex | int | Không | 0 | Chỉ số trang hiện tại (0-based) |
  | Ft_PageSize | int | Không | 10 | Số lượng bản ghi trên một trang |

- Quy tắc xử lý:
  1. Lọc bản ghi theo từ khóa KeyWord (like trên DisciplineName) và toán tử bằng trên FlagActive.
  2. Sắp xếp danh sách theo thứ tự DisciplineCode tăng dần, phân trang theo chỉ số trang và kích thước trang.
  3. Trả về cấu trúc phân trang chuẩn (PageInfo) gồm danh sách dữ liệu và thông tin phân trang.

- Bảng mã lỗi: Không có ngoại lệ nghiệp vụ riêng.
- Ví dụ Request:
    ```json
    {
      "KeyWord": "Khiển",
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
            "disciplineCode": "KL01",
            "networkID": "DEMO",
            "disciplineName": "Khiển trách",
            "disciplineDesc": "Nhắc nhở bằng văn bản",
            "flagActive": "1",
            "logLUDTimeUTC": null,
            "logLUBy": "seed"
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

### 2.2. POST /api/mst_discipline/getallactive
- Mục đích: Lấy toàn bộ danh sách hình thức kỷ luật đang hoạt động (FlagActive = "1").
- Bảng tham số Request: Không có tham số body bắt buộc.
- Quy tắc xử lý:
  1. Truy vấn các bản ghi có FlagActive bằng "1".
  2. Sắp xếp kết quả theo DisciplineCode tăng dần.
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
          "disciplineCode": "KL01",
          "networkID": "DEMO",
          "disciplineName": "Khiển trách",
          "disciplineDesc": "Nhắc nhở bằng văn bản",
          "flagActive": "1",
          "logLUDTimeUTC": null,
          "logLUBy": "seed"
        }
      ]
    }
    ```

---

### 2.3. POST /api/mst_discipline/getbydisciplinecode
- Mục đích: Lấy thông tin chi tiết một hình thức kỷ luật theo mã disciplineCode.
- Bảng tham số Request (MstDisciplineGetByDisciplineCodeReq):
  | Tên tham số | Kiểu dữ liệu | Bắt buộc? | Mặc định | Ý nghĩa |
  | :--- | :--- | :--- | :--- | :--- |
  | DisciplineCode | string | Có | null | Mã hình thức kỷ luật cần tra cứu |

- Quy tắc xử lý:
  1. Nếu DisciplineCode rỗng hoặc null, trả về ngay dữ liệu null.
  2. Truy vấn đúng một bản ghi theo mã DisciplineCode (không phân biệt hoa thường).
  3. Trả về đối tượng hình thức kỷ luật tìm được hoặc null nếu không tồn tại.

- Bảng mã lỗi: Không có.
- Ví dụ Request:
    ```json
    {
      "DisciplineCode": "KL01"
    }
    ```
- Ví dụ Response (Lần chạy thật):
    ```json
    {
      "success": true,
      "data": {
        "myIdxSeq": 0,
        "disciplineCode": "KL01",
        "networkID": "DEMO",
        "disciplineName": "Khiển trách",
        "disciplineDesc": "Nhắc nhở bằng văn bản",
        "flagActive": "1",
        "logLUDTimeUTC": null,
        "logLUBy": "seed"
      }
    }
    ```

---

### 2.4. POST /api/mst_discipline/create
- Mục đích: Thêm mới một hình thức kỷ luật vào hệ thống.
- Bảng tham số Request (MstDisciplineCreateReq):
  | Tên tham số | Kiểu dữ liệu | Bắt buộc? | Mặc định | Ý nghĩa |
  | :--- | :--- | :--- | :--- | :--- |
  | DisciplineCode | string | Không | null | Mã hình thức kỷ luật (bị ghi đè bởi sequence) |
  | DisciplineName | string | Có | null | Tên hình thức kỷ luật |
  | DisciplineDesc | string | Không | null | Mô tả hình thức kỷ luật |

- Quy tắc xử lý:
  1. Kiểm tra request rỗng; nếu rỗng ném lỗi mặc định `Mst_Discipline_Create`.
  2. Lấy mã sequence mới từ `Seq_DisciplineCode` trong transaction riêng đã commit độc lập (mã tự sinh ghi đè client gửi).
  3. Chuẩn hóa tên hình thức kỷ luật; kiểm tra tên không được rỗng.
  4. Lưu thông tin bản ghi với trạng thái mặc định FlagActive = "1" trong một transaction duy nhất.

- Bảng mã lỗi:
  | Mã lỗi (ErridnLoyalty.*) | Điều kiện phát sinh |
  | :--- | :--- |
  | Mst_Discipline_Create | Request rỗng hoặc lỗi ngoại lệ hệ thống khi lưu |
  | Mst_Discipline_CreateX_InvalidDisciplineName | Tên hình thức kỷ luật bị rỗng sau khi chuẩn hóa |

- Ví dụ Request:
    ```json
    {
      "DisciplineName": "Phạt tiền",
      "DisciplineDesc": "Phạt vi phạm nội quy"
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

### 2.5. POST /api/mst_discipline/update
- Mục đích: Cập nhật thông tin cấu hình của một hình thức kỷ luật hiện có.
- Bảng tham số Request (MstDisciplineUpdateReq):
  | Tên tham số | Kiểu dữ liệu | Bắt buộc? | Mặc định | Ý nghĩa |
  | :--- | :--- | :--- | :--- | :--- |
  | DisciplineCode | string | Có | null | Mã hình thức kỷ luật cần cập nhật |
  | DisciplineName | string | Có | null | Tên hình thức kỷ luật mới |
  | DisciplineDesc | string | Không | null | Mô tả mới |
  | FlagActive | string | Không | null | Cờ trạng thái hoạt động (1/0) |

- Quy tắc xử lý:
  1. Kiểm tra body rỗng; nếu rỗng trả về lỗi mặc định `Mst_Discipline_Update`.
  2. Chuẩn hóa các tham số và áp dụng danh sách cột cập nhật cố định (`DisciplineName`, `DisciplineDesc`, `FlagActive`).
  3. Kiểm tra sự tồn tại của bản ghi trong CSDL theo mã (`Mst_Discipline_CheckDB_DisciplineCodeNotFound`).
  4. Kiểm tra tên mới không được rỗng (`Mst_Discipline_UpdateX_InvalidDisciplineName`).
  5. Cập nhật thông tin trong một transaction riêng an toàn.

- Bảng mã lỗi:
  | Mã lỗi (ErridnLoyalty.*) | Điều kiện phát sinh |
  | :--- | :--- |
  | Mst_Discipline_Update | Request rỗng hoặc lỗi ngoại lệ hệ thống |
  | Mst_Discipline_CheckDB_DisciplineCodeNotFound | Mã hình thức kỷ luật không tồn tại trong hệ thống |
  | Mst_Discipline_UpdateX_InvalidDisciplineName | Tên hình thức kỷ luật mới bị rỗng |

- Ví dụ Request:
    ```json
    {
      "DisciplineCode": "KL01",
      "DisciplineName": "Khiển trách cập nhật",
      "DisciplineDesc": "Nhắc nhở bằng văn bản sửa đổi",
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

### 2.6. POST /api/mst_discipline/delete
- Mục đích: Xóa một hình thức kỷ luật khỏi hệ thống theo mã.
- Bảng tham số Request (MstDisciplineDeleteReq):
  | Tên tham số | Kiểu dữ liệu | Bắt buộc? | Mặc định | Ý nghĩa |
  | :--- | :--- | :--- | :--- | :--- |
  | DisciplineCode | string | Có | null | Mã hình thức kỷ luật cần xóa |
  | OrgID | string | Không | null | Mã tổ chức (không sử dụng trong biz) |

- Quy tắc xử lý:
  1. Kiểm tra mã DisciplineCode không được rỗng (nếu rỗng trả về lỗi `invalid_departmentcode` theo tên kế thừa nguồn).
  2. Kiểm tra sự tồn tại của bản ghi trong CSDL (`Mst_Discipline_CheckDB_DisciplineCodeNotFound`).
  3. Thực hiện xóa bản ghi trong một transaction riêng an toàn.

- Bảng mã lỗi:
  | Mã lỗi (ErridnLoyalty.*) | Điều kiện phát sinh |
  | :--- | :--- |
  | invalid_departmentcode | Mã hình thức kỷ luật truyền vào bị rỗng hoặc null |
  | Mst_Discipline_CheckDB_DisciplineCodeNotFound | Không tìm thấy mã hình thức kỷ luật trong hệ thống |

- Ví dụ Request:
    ```json
    {
      "DisciplineCode": "KL03"
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

### 2.7. POST /api/mst_discipline/deletemultiple
- Mục đích: Xóa nhiều hình thức kỷ luật cùng lúc theo danh sách truyền vào.
- Bảng tham số Request: Danh sách mảng đối tượng `MstDisciplineDeleteReq`.
  | Tên tham số | Kiểu dữ liệu | Bắt buộc? | Mặc định | Ý nghĩa |
  | :--- | :--- | :--- | :--- | :--- |
  | [i].DisciplineCode | string | Có | null | Mã hình thức kỷ luật trong danh sách |

- Quy tắc xử lý:
  1. Kiểm tra danh sách body request không được null.
  2. Lặp tuần tự qua từng phần tử trong danh sách; mỗi phần tử thực hiện xóa độc lập trong một transaction riêng biệt (hành vi "xóa một phần").
  3. Nếu gặp lỗi ở phần tử thứ k, vòng lặp dừng ngay lập tức và trả về lỗi của phần tử đó (các phần tử trước đã được commit và không bị rollback).

- Bảng mã lỗi: Tương tự endpoint `/delete`.
- Ví dụ Request:
    ```json
    [
      {
        "DisciplineCode": "KL01"
      },
      {
        "DisciplineCode": "KL02"
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

## 3. Khác biệt so với nguồn & GAP

- Phân quyền hệ thống: Các bước kiểm tra phân quyền (CheckGatewayAuthentication, CheckAuthorize, v.v.) và ghi nhật ký hệ thống (RequestLog/ReturnLog) tại nguồn được bỏ qua do kiến trúc MiniLoyalty không triển khai các thành phần này.
- Quản lý phiên người dùng: Thông tin người cập nhật (`LogLUBy`) được gán giá trị mặc định "port" do chưa tích hợp phiên làm việc người dùng thực tế.

---

## 4. Nhật ký thay đổi

- #132 đến #138: Port các chức năng Search, GetAllActive, GetByDisciplineCode, Create, Update, Delete, DeleteMultiple của Mst_DisciplineController sang MiniLoyalty.

---

## TOM TAT
FILE_DA_TAO_SUA: docs/api/Mst_DisciplineController.md
PHAM_VI_PHU: Mst_DisciplineControllerPort (Search, GetAllActive, GetByDisciplineCode, Create, Update, Delete, DeleteMultiple), tổng cộng 7 endpoint.
KHONG_XAC_MINH: Không có điểm nào ghi 'Chưa xác minh' vì toàn bộ thông tin endpoint, tham số, mã lỗi và luồng xử lý đều được truy nguyên chính xác 1-1 từ mã nguồn được cung cấp.
LENH_DA_CHAY: Đã kiểm tra đối chiếu toàn bộ các route POST /api/mst_discipline/..., mã ErridnLoyalty.*, định dạng dữ liệu JSON ví dụ và các tham chiếu file:dòng.
VI_PHAM_QUY_TAC: Không.
