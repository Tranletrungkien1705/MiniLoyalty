# Tài liệu API InvF_TempPrintController

Phạm vi: InvF_TempPrintController (GetByTempPrintType)
Cập nhật: 2026-10-17
Phiên bản mã: fd23012
Trạng thái: Nháp — chờ review

---

## 1. Tóm tắt

- Mục đích nghiệp vụ: Quản lý tra cứu mẫu in hoá đơn/chứng từ (InvF_TempPrint) dựa theo loại mẫu in (TempPrintType).
- Bảng nguồn: Chưa xác minh (mã nguồn gate/API/biz chưa được cung cấp trong gói mã tham chiếu hiện tại).
- Bảng cơ sở dữ liệu: InvF_TempPrint (Models/Port/InvF_TempPrint.cs:10-23)

| Tên cột | Kiểu trong EF | Khóa | Ghi chú |
|---|---|---|---|
| IF_TempPrintNo | string | PK, Required | Mã định danh mẫu in |
| NetworkID | string? | Nullable | Mã định danh mạng |
| TempPrintType | string? | Nullable | Loại mẫu in |
| TempPrintName | string? | Nullable | Tên hiển thị của mẫu in |
| TempPrintDesc | string? | Nullable | Diễn giải/mô tả mẫu in |
| PathFile | string? | Nullable | Đường dẫn file mẫu in |
| IsDefault | string? | Nullable | Cờ đánh dấu mặc định (1: mặc định, 0: không) |
| FlagActive | string? | Nullable | Cờ trạng thái hoạt động (1: kích hoạt, 0: dừng) |
| LogLUDTimeUTC | DateTime? | Nullable | Thời điểm cập nhật cuối (UTC) |
| LogLUBy | string? | Nullable | Người dùng cập nhật cuối |

---

## 2. Endpoint: POST /api/invf_tempprint/getbytempprinttype

- Route: POST /api/invf_tempprint/getbytempprinttype (Controllers/Port/InvF_TempPrintControllerPort.cs:28)
- HTTP method: POST

### Bảng tham số request (InvF_TempPrint_GetByTempPrintTypeReq)

| Tên tham số | Kiểu | Bắt buộc? | Mặc định | Ý nghĩa |
|---|---|---|---|---|
| TempPrintType | string? | Không | null | Loại mẫu in cần tra cứu (so khớp chính xác, không phân biệt hoa thường) |

### Quy tắc xử lý theo thứ tự

1. Nhận payload từ body request, cho phép body rỗng (Controllers/Port/InvF_TempPrintControllerPort.cs:28-30).
2. Khởi tạo đối tượng kết quả objInvF_TempPrint = null (Controllers/Port/InvF_TempPrintControllerPort.cs:31).
3. Kiểm tra biến tempPrintType: Nếu tempPrintType khác null và không rỗng (!string.IsNullOrEmpty(tempPrintType)), thực hiện truy vấn bảng InvF_TempPrint với điều kiện TempPrintType khác null và giá trị trùng khớp chính xác (sử dụng PortQuery.Equal không phân biệt hoa thường), sắp xếp tăng dần theo IF_TempPrintNo và lấy bản ghi đầu tiên (Controllers/Port/InvF_TempPrintControllerPort.cs:33-40).
4. Nếu tempPrintType là null hoặc chuỗi rỗng, bỏ qua truy vấn và giữ nguyên objInvF_TempPrint là null (Controllers/Port/InvF_TempPrintControllerPort.cs:33).
5. Trả về kết quả HTTP 200 kèm đối tượng JSON có cấu trúc bọc: Success = true, Data = { InvF_TempPrint = objInvF_TempPrint } (Controllers/Port/InvF_TempPrintControllerPort.cs:42-49).

### Bảng mã lỗi

| Mã lỗi | Điều kiện |
|---|---|
| Không có | Controller không khai báo mã lỗi ErridnLoyalty; trả về HTTP 200 và Data.InvF_TempPrint = null khi không tìm thấy hoặc tham số rỗng |

### Ví dụ request suy ra từ mã

    {
        "TempPrintType": "TP01"
    }

Ví dụ với body rỗng:

    {}

### Ví dụ response

Chưa có lần chạy mẫu

### Hành vi đặc biệt

- Cho phép request body rỗng: Thuộc tính EmptyBodyBehavior.Allow cho phép gửi body rỗng hoặc null mà không phát sinh lỗi parse JSON (Controllers/Port/InvF_TempPrintControllerPort.cs:28).
- Trả về null trong Data: Khi TempPrintType không được truyền hoặc chuỗi rỗng hoặc không có dữ liệu trùng khớp trong cơ sở dữ liệu, trường Data.InvF_TempPrint trả về null thay vì ném lỗi hoặc trả danh sách rỗng (Controllers/Port/InvF_TempPrintControllerPort.cs:31, :46).
- Lấy bản ghi đầu tiên: Trường hợp có nhiều hơn một bản ghi cùng loại mẫu in, kết quả được sắp xếp tăng dần theo IF_TempPrintNo và chỉ lấy bản ghi đầu tiên qua FirstOrDefaultAsync (Controllers/Port/InvF_TempPrintControllerPort.cs:38-39).

---

## 3. Khác biệt so với nguồn & GAP

- Vị trí mã nguồn nguồn: Chưa xác minh file/dòng của gate, API và biz ở nguồn do không có trong tệp thông tin tham chiếu hiện tại.
- Phân quyền & phiên đăng nhập: Controller chưa áp dụng middleware xác thực người dùng hoặc phân quyền mạng/tổ chức.
- Lọc theo mạng/đơn vị: Truy vấn chỉ tìm kiếm theo TempPrintType mà không ràng buộc điều kiện NetworkID hoặc OrgID từ ngữ cảnh đăng nhập (Controllers/Port/InvF_TempPrintControllerPort.cs:36-39).
- Dữ liệu mẫu (Seed): Tích hợp InvF_TempPrintSeeder (Order 63) nạp sẵn 2 bản ghi mẫu (TP01, TP02) khi cơ sở dữ liệu trống (Models/Port/InvF_TempPrint.cs:35-50).

---

## 4. Nhật ký thay đổi

- Hạng mục port InvF_TempPrintController/GetByTempPrintType: Hoàn thành triển khai và cấu hình entity (commit fd23012).
