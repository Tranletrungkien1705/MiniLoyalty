Phạm vi: Controller quản lý loại mẫu in (Mst_TempPrintType)
Cập nhật: 2026-10-16
Phiên bản mã: 7973495
Trạng thái: Nháp - chờ review

## 1. Tóm tắt

- Mục đích nghiệp vụ: Cung cấp chức năng lấy danh sách toàn bộ loại mẫu in đang quản lý trong hệ thống MiniLoyalty theo logic port 1:1 từ nguồn.
- Bảng nguồn:
  - Gate: Mst_TempPrintTypeController.cs:31-95 (WA_Mst_TempPrintType_Get)
  - API: MstTempPrintTypeController.cs:19-89
  - Biz: MasterData.cs:46057-46161 (WAS_Mst_TempPrintType_Get), MasterData.cs:45891-46055, MasterData.cs:45736-45889
- Bảng cấu trúc cột (Mst_TempPrintType):
  - TempPrintType | string | Khóa chính (PK), Bắt buộc | Mã loại mẫu in
  - NetworkID | string? | Nullable | NetworkID
  - TempPrintName | string? | Nullable | Tên loại mẫu in
  - Remark | string? | Nullable | Ghi chú
  - FlagActive | string? | Nullable | Trạng thái hoạt động ('0' hoặc '1')
  - LogLUDTimeUTC | DateTime? | Nullable | Thời gian cập nhật cuối UTC
  - LogLUBy | string? | Nullable | Người cập nhật cuối

## 2. Từng endpoint

### Endpoint 1: Lấy danh sách tất cả loại mẫu in (GetAllActive)
- Route & HTTP Method: POST /api/mst_tempprinttype/getallactive
- Tham số Request: Không có (dùng body rỗng hoặc không bắt buộc).
- Quy tắc xử lý:
  1. Gọi hàm nội bộ GetXAsync với keyWord = null, flagActive = null, recordStart = 0, recordCount = 123456000 (do gate không thiết lập Ft_WhereClause nên lấy toàn bộ).
  2. Sắp xếp danh sách theo TempPrintType giảm dần (desc).
  3. Bọc kết quả vào đối tượng phản hồi có thuộc tính Lst_Mst_TempPrintType. Nếu danh sách trống, trả về giá trị null cho thuộc tính này.
- Bảng mã lỗi: Chua xac minh (Không có khai báo ngoại lệ cụ thể trong mã port).
- Ví dụ Request:
    {}
- Ví dụ Response: Chua co lan chay mau.

## 3. Khác biệt so với nguồn & GAP
- Mặc dù endpoint có tên là GetAllActive, gate gốc không thiết lập điều kiện lọc FlagActive = '1', do đó hành vi thực tế là lấy toàn bộ danh sách bản ghi.
- Kết quả trả về được bọc trong cấu trúc đối tượng chứa Lst_Mst_TempPrintType thay vì mảng trần thuần túy.

## 4. Nhật ký thay đổi
- Hạng mục #245: Port Mst_TempPrintTypeController/GetAllActive.

TOM TAT
FILE_DA_TAO_SUA: docs/api/Mst_TempPrintTypeController.md
PHAM_VI_PHU: Mst_TempPrintTypeControllerPort (1 endpoint: getallactive)
KHONG_XAC_MINH: Mã lỗi ErridnLoyalty do controller port không có try/catch tường minh; response thực tế chưa có do chưa chạy app.
LENH_DA_CHAY: Chua chay lenh (che do WEB).
VI_PHAM_QUY_TAC: Khong.
