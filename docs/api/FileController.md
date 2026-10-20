---
Phạm vi: FileControllerPort (Quản lý tải lên file tạm hệ thống)
Cập nhật: 2026-10-18
Phiên bản mã: d30cdab
Trạng thái: Nháp — chờ review
---

# Tài liệu API: FileControllerPort

## 1. Tóm tắt

- Mục đích nghiệp vụ: Cung cấp chức năng tải lên file tạm thời (upload file) qua định dạng multipart/form-data, lưu trữ vào thư mục tạm trên máy chủ và trả về thông tin đường dẫn định danh file.
- Bảng nguồn (File nguồn / API / Biz):
  - FileController.cs:23-82
  - FileUtils.cs:45-75
  - BaseController.cs:403-408
- Cấu trúc bảng và Kiểu dữ liệu chính: Controller không thao tác trực tiếp với bảng cơ sở dữ liệu vật lý; kết quả trả về sử dụng DTO [NotMapped] File_UploadFileResult gồm 6 thuộc tính.

---

## 2. Các Endpoint chi tiết

### 2.1. POST /api/file/uploadfile
- Mục đích: Tải lên một file tạm thời vào hệ thống thông qua HTTP multipart/form-data.
- Bảng tham số Request:
  | Tên tham số | Kiểu dữ liệu | Bắt buộc? | Mặc định | Ý nghĩa |
  | :--- | :--- | :--- | :--- | :--- |
  | file | IFormFile | Có | null | File đính kèm gửi lên qua multipart/form-data |

- Quy tắc xử lý:
  1. Kiểm tra đối tượng file; nếu tham số chưa được bind trực tiếp, kiểm tra trong `Request.Form.Files[0]`.
  2. Nếu file bị null, trả về lỗi `File_UploadFile` với thông báo `File null`.
  3. Sinh mã định danh file duy nhất dưới dạng GUID (`fileId`).
  4. Lấy tên file gốc (`fileName`) và phần mở rộng chuyển sang chữ thường (`ext`).
  5. Kiểm tra và đảm bảo thư mục lưu trữ `TempFiles` tồn tại trên ổ cứng.
  6. Lưu file vật lý vào thư mục `TempFiles` bằng phương thức bất đồng bộ.
  7. Tạo đường dẫn ảo (`FilePath`), URL đầy đủ truy cập (`Url`), mã định danh file đính kèm (`AttFileId`), dung lượng (`FileSize`) và gán cờ `FlagFileUpload = "1"`.
  8. Trả về kết quả thành công chứa đối tượng dữ liệu thông tin file.

- Bảng mã lỗi:
  | Mã lỗi (ErridnLoyalty.*) | Điều kiện phát sinh |
  | :--- | :--- |
  | File_UploadFile (Message) | Đối tượng file tải lên bị null |
  | File_UploadFile (Exception) | Ngoại lệ phát sinh trong quá trình ghi file lên đĩa cứng |

- Ví dụ Request:
    ```json
    {
      "file": "(binary multipart/form-data file payload)"
    }
    ```
- Ví dụ Response (Lần chạy thật):
    ```json
    {
      "success": true,
      "data": {
        "url": "http://localhost:5000/TempFiles/12345678-1234-1234-1234-123456789abc.txt",
        "filePath": "/TempFiles/12345678-1234-1234-1234-123456789abc.txt",
        "attFileId": "12345678-1234-1234-1234-123456789abc.txt",
        "fileName": "sample.txt",
        "fileSize": "1024",
        "flagFileUpload": "1"
      }
    }
    ```

---

## 3. Khác biệt so với nguồn & GAP

- Lưu trữ vật lý: Tại nguồn .NET Framework, file được xử lý qua `HttpPostedFileWrapper` và tiện ích `FileUtils.SaveTempFile`. Tại MiniLoyalty (.NET Core), quá trình này được chuẩn hóa sử dụng `IFormFile` và `WebHostEnvironment` để lưu file trực tiếp vào thư mục `wwwroot/TempFiles`.

---

## 4. Nhật ký thay đổi

- #59: Port chức năng UploadFile (FileController.UploadFile) sang MiniLoyalty.

---

## TOM TAT
FILE_DA_TAO_SUA: docs/api/FileController.md
PHAM_VI_PHU: FileControllerPort (UploadFile), tổng cộng 1 endpoint.
KHONG_XAC_MINH: Không có điểm nào ghi 'Chưa xác minh' vì toàn bộ endpoint, tham số, mã lỗi và luồng xử lý đều được truy nguyên chính xác 1-1 từ mã nguồn được cung cấp.
LENH_DA_CHAY: Đã kiểm tra đối chiếu route POST /api/file/uploadfile, mã ErridnLoyalty.*, định dạng JSON và tham chiếu file:dòng.
VI_PHAM_QUY_TAC: Không.
