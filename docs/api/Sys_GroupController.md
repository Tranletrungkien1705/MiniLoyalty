Phạm vi: Controller quản lý nhóm người dùng hệ thống (Sys_GroupController)
Cập nhật: 2026-10-16
Phiên bản mã: 7973495
Trạng thái: Nháp - chờ review

## 1. Tóm tắt

- Mục đích nghiệp vụ: Quản lý nhóm người dùng trong hệ thống (tra cứu phân trang, lấy toàn bộ danh sách active, lấy chi tiết theo mã nhóm, tạo mới, cập nhật, xóa đơn, xóa nhiều, import từ file Excel, export danh sách và export file mẫu).
- Bảng nguồn:
  - Gate: Sys_GroupController.cs:27-135 (Search), Sys_GroupController.cs:136-243 (GetAllActive), Sys_GroupController.cs:244-310 (GetByGroupCode), Sys_GroupController.cs:314-398 (Create), Sys_GroupController.cs:402-471 (Update), Sys_GroupController.cs:475-542 (Delete), Sys_GroupController.cs:544-608 (DeleteMultiple), Sys_GroupController.cs:612-753 (Import), Sys_GroupController.cs:758-836 (Export), Sys_GroupController.cs:839-871 (ExportTemplate)
  - API: SysGroupController.cs:20-91 (Get), SysGroupController.cs:94-165 (Create), SysGroupController.cs:168-239 (Update), SysGroupController.cs:242-313 (Delete)
  - Biz: System.cs:2510-2625 (WAS_Sys_Group_Get), System.cs:2938-3018 (WAS_Sys_Group_Create), System.cs:3368-3451 (WAS_Sys_Group_Update), System.cs:3019-3163 (Sys_Group_Update), System.cs:3726-3805 (WAS_Sys_Group_Delete), System.cs:3453-3566 (Sys_Group_Delete)
- Bảng cấu trúc cột (Sys_Group):
  - GroupCode | string | Khóa chính (PK), Bắt buộc | Mã nhóm người dùng (Sys_Group.cs:11)
  - GroupName | string? | Nullable | Tên nhóm người dùng (Sys_Group.cs:13)
  - FlagActive | string? | Nullable | Trạng thái hoạt động ('1': hoạt động, '0': không hoạt động) (Sys_Group.cs:15)
  - LogLUDTimeUTC | DateTime? | Nullable | Thời gian cập nhật cuối UTC (Sys_Group.cs:17)
  - LogLUBy | string? | Nullable | Người cập nhật cuối (Sys_Group.cs:19)
  - MST | string? | Nullable | Mã số thuế (Sys_Group.cs:21)

## 2. Từng endpoint

### Endpoint 1: Tìm kiếm nhóm người dùng (Search)
- Route & HTTP Method: POST /api/sys_group/search
- Tham số Request (Sys_GroupSearchReq):
  - OrgID | string? | Không | null | Mã đơn vị tổ chức (bị bỏ qua theo logic nguồn)
  - KeyWord | string? | Không | null | Từ khóa tìm kiếm (bị bỏ qua theo logic nguồn)
  - FlagActive | string? | Không | null | Lọc theo trạng thái hoạt động (so sánh bằng tuyệt đối)
  - Ft_PageIndex | int | Không | 0 | Chỉ số trang hiện tại
  - Ft_PageSize | int | Không | 10 | Số bản ghi trên mỗi trang
- Quy tắc xử lý:
  1. Lọc theo FlagActive nếu có giá trị.
  2. Sắp xếp danh sách theo GroupCode tăng dần, đếm tổng số bản ghi và phân trang lấy dữ liệu.
  3. Thống kê số lượng user thuộc nhóm từ bảng Sys_UserInGroup để trả về QtyUserInGroup.
  4. Trả về đối tượng PageInfo chứa danh sách kết quả và thông tin phân trang.
- Bảng mã lỗi:
  - Chua xac minh (Không khai báo mã lỗi tường minh ngoại trừ bắt ngoại lệ chung).
- Ví dụ Request:
    {
      "OrgID": "DEMO",
      "KeyWord": "Admin",
      "FlagActive": "1",
      "Ft_PageIndex": 0,
      "Ft_PageSize": 10
    }
- Ví dụ Response: Chua co lan chay mau.

### Endpoint 2: Lấy danh sách nhóm hoạt động (GetAllActive)
- Route & HTTP Method: POST /api/sys_group/getallactive
- Tham số Request: Không bắt buộc (nhận object rỗng hoặc null).
- Quy tắc xử lý:
  1. Lọc các bản ghi có FlagActive = "1".
  2. Sắp xếp theo GroupCode tăng dần, giới hạn số lượng từ 0 đến 123456000.
  3. Trả về danh sách trần (dạng mảng các đối tượng gồm MyIdxSeq và các cột của Sys_Group); nếu rỗng trả về null.
- Bảng mã lỗi: Chua xac minh.
- Ví dụ Request:
    {}
- Ví dụ Response: Chua co lan chay mau.

### Endpoint 3: Lấy chi tiết nhóm theo mã (GetByGroupCode)
- Route & HTTP Method: POST /api/sys_group/getbygroupcode
- Tham số Request (Sys_GroupGetByGroupCodeReq):
  - GroupCode | string? | Có | null | Mã nhóm người dùng cần tra cứu
  - OrgID | string? | Không | null | Mã đơn vị tổ chức
- Quy tắc xử lý:
  1. Nếu GroupCode rỗng hoặc null, trả về ngay Success = true, Data = null.
  2. Truy vấn tìm kiếm Sys_Group theo GroupCode; nếu không tìm thấy trả về null.
  3. Truy vấn danh sách người dùng trong nhóm từ bảng Sys_UserInGroup kết hợp thông tin liên quan.
  4. Trả về cấu trúc chứa objSys_Group và danh sách Lst_Sys_UserInGroup.
- Bảng mã lỗi: Chua xac minh.
- Ví dụ Request:
    {
      "GroupCode": "G01"
    }
- Ví dụ Response: Chua co lan chay mau.

### Endpoint 4: Tạo mới nhóm người dùng (Create)
- Route & HTTP Method: POST /api/sys_group/create
- Tham số Request (Sys_GroupCreateReq):
  - GroupCode | string? | Có | null | Mã nhóm người dùng
  - GroupName | string? | Có | null | Tên nhóm người dùng
  - MST | string? | Không | null | Mã số thuế
- Quy tắc xử lý:
  1. Kiểm tra request body không được null (nếu null trả lỗi ErridnLoyalty.Sys_Group_Create).
  2. Mở giao dịch (Transaction), chuẩn hoá dữ liệu đầu vào.
  3. Kiểm tra tính hợp lệ: GroupCode không được rỗng, GroupName có độ dài tối thiểu 1 ký tự, GroupCode chưa tồn tại trong hệ thống.
  4. Lưu thông tin mới vào bảng Sys_Group với FlagActive mặc định là "1", commit giao dịch.
- Bảng mã lỗi:
  - Sys_Group_Create: Lỗi khi thiếu dữ liệu body hoặc lỗi ngoại lệ hệ thống.
  - Sys_Group_Create_InvalidGroupCode: Mã nhóm không hợp lệ hoặc rỗng.
  - Sys_Group_Create_InvalidGroupName: Tên nhóm quá ngắn (dưới 1 ký tự).
  - Sys_Group_CheckDB_GroupCodeExist: Mã nhóm đã tồn tại trong cơ sở dữ liệu.
- Ví dụ Request:
    {
      "GroupCode": "G05",
      "GroupName": "Manager",
      "MST": "0101"
    }
- Ví dụ Response: Chua co lan chay mau.

### Endpoint 5: Cập nhật thông tin nhóm người dùng (Update)
- Route & HTTP Method: POST /api/sys_group/update
- Tham số Request (Sys_GroupUpdateReq):
  - GroupCode | string? | Có | null | Mã nhóm người dùng cần cập nhật
  - GroupName | string? | Không | null | Tên nhóm người dùng mới
  - FlagActive | string? | Không | null | Trạng thái hoạt động mới
  - MST | string? | Không | null | Mã số thuế mới
- Quy tắc xử lý:
  1. Kiểm tra request body không được null.
  2. Mở giao dịch, xác định các cột cần cập nhật (GroupName, FlagActive).
  3. Kiểm tra sự tồn tại của nhóm trong cơ sở dữ liệu theo GroupCode; nếu không thấy trả lỗi.
  4. Kiểm tra độ dài tên nhóm mới (nếu cập nhật).
  5. Cập nhật thông tin, thời gian LogLUDTimeUTC và commit giao dịch.
- Bảng mã lỗi:
  - Sys_Group_Update: Lỗi khi body rỗng hoặc lỗi ngoại lệ.
  - Sys_Group_CheckDB_GroupCodeNotFound: Không tìm thấy mã nhóm cần cập nhật.
  - Sys_Group_Update_InvalidGroupName: Tên nhóm cập nhật không hợp lệ.
- Ví dụ Request:
    {
      "GroupCode": "G01",
      "GroupName": "Admin Updated",
      "FlagActive": "1"
    }
- Ví dụ Response: Chua co lan chay mau.

### Endpoint 6: Xóa nhóm người dùng (Delete)
- Route & HTTP Method: POST /api/sys_group/delete
- Tham số Request (Sys_GroupDeleteReq):
  - GroupCode | string? | Có | null | Mã nhóm cần xóa
- Quy tắc xử lý:
  1. Kiểm tra GroupCode không được rỗng (nếu rỗng trả lỗi invalid_GroupCode).
  2. Mở giao dịch kiểm tra sự tồn tại của nhóm trong cơ sở dữ liệu.
  3. Xóa các bản ghi liên quan trong bảng phụ Sys_UserInGroup.
  4. Xóa bản ghi Sys_Group và commit giao dịch.
- Bảng mã lỗi:
  - invalid_GroupCode: Mã nhóm không được cung cấp hoặc rỗng.
  - Sys_Group_CheckDB_GroupCodeNotFound: Không tìm thấy nhóm cần xóa.
- Ví dụ Request:
    {
      "GroupCode": "G04"
    }
- Ví dụ Response: Chua co lan chay mau.

### Endpoint 7: Xóa nhiều nhóm người dùng (DeleteMultiple)
- Route & HTTP Method: POST /api/sys_group/deletemultiple
- Tham số Request: Danh sách đối tượng (List<Sys_GroupDeleteReq>)
  - GroupCode | string? | Có | null | Mã nhóm trong danh sách cần xóa
- Quy tắc xử lý:
  1. Kiểm tra request body không được null.
  2. Lặp tuần tự qua danh sách các nhóm yêu cầu xóa, mỗi phần tử thực hiện trong một giao dịch độc lập.
  3. Áp dụng hành vi "xóa một phần": nếu một phần tử gặp lỗi thì dừng lại và trả lỗi của phần tử đó (các phần tử trước đã commit thành công không bị rollback).
- Bảng mã lỗi:
  - Sys_Group_Delete: Lỗi khi body rỗng hoặc lỗi thực hiện giao dịch của phần tử.
  - Sys_Group_CheckDB_GroupCodeNotFound: Không tìm thấy mã nhóm tương ứng trong quá trình xóa.
- Ví dụ Request:
    [
      {
        "GroupCode": "G03"
      }
    ]
- Ví dụ Response: Chua co lan chay mau.

### Endpoint 8: Import dữ liệu nhóm từ Excel (Import)
- Route & HTTP Method: POST /api/sys_group/import (multipart/form-data)
- Tham số Request:
  - orgId | string? | Không | null | Mã đơn vị
  - file | IFormFile | Có | null | Tệp Excel định dạng .xlsx
- Quy tắc xử lý:
  1. Kiểm tra định dạng tệp tải lên phải là .xlsx và không được rỗng.
  2. Đọc dữ liệu tệp Excel theo template chuẩn (2 cột: GroupCode, GroupName); kiểm tra dữ liệu không được rỗng.
  3. Vòng lặp kiểm tra: xác thực toàn bộ các dòng không bị trống mã hoặc tên nhóm trước khi tiến hành tạo.
  4. Vòng lặp tạo mới: gọi tiến trình tạo tuần tự cho từng dòng (mỗi dòng một giao dịch độc lập, hỗ trợ import một phần).
- Bảng mã lỗi:
  - Sys_Group_Import: Lỗi file không hợp lệ, thiếu file, hoặc tệp không có dữ liệu.
  - Các lỗi phát sinh từ hàm tạo tương tự nghiệp vụ Create.
- Ví dụ Request: (FormData gồm file excel)
- Ví dụ Response: Chua co lan chay mau.

### Endpoint 9: Xuất dữ liệu danh sách nhóm ra Excel (Export)
- Route & HTTP Method: POST /api/sys_group/export
- Tham số Request (Sys_GroupExportReq):
  - OrgID | string? | Không | null | Mã đơn vị tổ chức
  - KeyWord | string? | Không | null | Từ khóa tìm kiếm
  - FlagActive | string? | Không | null | Lọc theo trạng thái hoạt động
- Quy tắc xử lý:
  1. Lọc danh sách nhóm theo trạng thái FlagActive (các tham số khác bỏ qua theo logic nguồn).
  2. Kiểm tra nếu danh sách rỗng thì trả về lỗi "Không có dữ liệu".
  3. Sử dụng từ điển cột chuẩn để tạo tệp Excel và lưu vào thư mục hệ thống.
  4. Trả về đường dẫn Url tải file.
- Bảng mã lỗi:
  - Sys_Group_Export: Lỗi khi không có dữ liệu xuất báo cáo hoặc lỗi phát sinh trong quá trình ghi file.
- Ví dụ Request:
    {
      "FlagActive": "1"
    }
- Ví dụ Response: Chua co lan chay mau.

### Endpoint 10: Tải tệp Excel mẫu ExportTemplate (ExportTemplate)
- Route & HTTP Method: POST /api/sys_group/exporttemplate
- Tham số Request: Không có.
- Quy tắc xử lý:
  1. Khởi tạo danh sách rỗng, lấy từ điển cấu trúc mẫu gồm 2 cột (GroupCode, GroupName).
  2. Tạo tệp Excel mẫu và lưu trữ vào thư mục hệ thống.
  3. Trả về đường dẫn Url tải tệp mẫu.
- Bảng mã lỗi:
  - Sys_Group_ExportTemplate: Lỗi phát sinh trong quá trình tạo tệp mẫu.
- Ví dụ Request:
    {}
- Ví dụ Response: Chua co lan chay mau.

## 3. Khác biệt so với nguồn & GAP
- Các tham số KeyWord và OrgID trong hàm Search và Export thực tế bị bỏ qua do hàm strWhereClause tương ứng tại mã nguồn gốc không áp dụng điều kiện lọc này.
- Các liên kết liên quan đến bảng Sys_User được giả lập hoặc bỏ qua do entity tương ứng chưa được port vào hệ thống đích MiniLoyalty.

## 4. Nhật ký thay đổi
- Hạng mục #307: Port Sys_GroupController/Search.
- Hạng mục #308: Port Sys_GroupController/GetAllActive.
- Hạng mục #309: Port Sys_GroupController/GetByGroupCode.
- Hạng mục #310: Port Sys_GroupController/Create.
- Hạng mục #311: Port Sys_GroupController/Update.
- Hạng mục #312: Port Sys_GroupController/Delete.
- Hạng mục #313: Port Sys_GroupController/DeleteMultiple.
- Hạng mục #314: Port Sys_GroupController/Import.
- Hạng mục #315: Port Sys_GroupController/Export.
- Hạng mục #316: Port Sys_GroupController/ExportTemplate.

TOM TAT
FILE_DA_TAO_SUA: docs/api/Sys_GroupController.md
PHAM_VI_PHU: Sys_GroupControllerPort (10 endpoint: search, getallactive, getbygroupcode, create, update, delete, deletemultiple, import, export, exporttemplate)
KHONG_XAC_MINH: Mã lỗi chi tiết ErridnLoyalty tầng Biz ngoài các mã cơ bản được ánh xạ PortResult; phản hồi response thực tế chưa có do chưa chạy app.
LENH_DA_CHAY: Chua chay lenh (che do WEB).
VI_PHAM_QUY_TAC: Khong.
