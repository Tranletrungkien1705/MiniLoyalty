Phạm vi: Controller quản lý đối tượng hệ thống (Sys_ObjectController)
Cập nhật: 2026-10-16
Phiên bản mã: 7973495
Trạng thái: Nháp - chờ review

## 1. Tóm tắt

- Mục đích nghiệp vụ: Quản lý và tra cứu các danh mục đối tượng trong hệ thống bao gồm menu active, menu kết hợp button active, danh sách chức năng (FUNC) active, tra cứu chức năng theo mã đối tượng từ tệp cấu hình XML, lấy toàn bộ chức năng theo mã đối tượng kèm cờ đánh dấu phân quyền, và lấy danh sách toàn bộ đối tượng hệ thống.
- Bảng nguồn:
  - Gate: Sys_ObjectController.cs:29-98 (GetAllMenuActive), Sys_ObjectController.cs:101-172 (GetAllMenuButtonActive), Sys_ObjectController.cs:175-243 (GetFunctionActive), Sys_ObjectController.cs:246-302 (GetFunctionByObjectCode), Sys_ObjectController.cs:305-406 (GetAllFunctionBy_ObjectCode), Sys_ObjectController.cs:408-515 (Get)
  - API: SysObjectController.cs:20-91 (WA_Sys_Object_Get)
  - Biz: System.cs:15325-15424 (WAS_Sys_Object_Get), System.cs:14892-15098 (Sys_Object_Get)
- Bảng cấu trúc cột (Sys_Object):
  - ObjectCode | string | Khóa chính (PK), Bắt buộc | Mã đối tượng
  - ObjectName | string? | Nullable | Tên đối tượng
  - ServiceCode | string? | Nullable | Mã dịch vụ
  - ObjectType | string? | Nullable | Loại đối tượng (MENU, BUTTON, FUNC, ...)
  - FlagExecModal | string? | Nullable | Cờ thực thi modal
  - FlagActive | string? | Nullable | Trạng thái hoạt động ('1': active, '0': inactive)
  - LogLUDTimeUTC | string? | Nullable | Thời gian cập nhật cuối UTC
  - LogLUBy | string? | Nullable | Người cập nhật cuối

## 2. Từng endpoint

### Endpoint 1: Lấy danh sách menu đang hoạt động (GetAllMenuActive)
- Route & HTTP Method: POST /api/sys_object/getallmenuactive
- Tham số Request: Không bắt buộc (nhận object rỗng hoặc null).
- Quy tắc xử lý:
  1. Lọc các bản ghi có ObjectType == "MENU" và FlagActive == "1".
  2. Sắp xếp theo ObjectCode tăng dần.
  3. Trả về danh sách đối tượng; nếu rỗng, trả về Data = null.
- Bảng mã lỗi: Chua xac minh.
- Ví dụ Request:
    {}
- Ví dụ Response: Chua co lan chay mau.

### Endpoint 2: Lấy danh sách menu và nút đang hoạt động (GetAllMenuButtonActive)
- Route & HTTP Method: POST /api/sys_object/getallmenubuttonactive
- Tham số Request (Sys_Object_GetAllMenuButtonActiveReq):
  - ObjectType | string? | Không | null | Loại đối tượng
  - ObjectName | string? | Không | null | Tên đối tượng (hỗ trợ tìm kiếm gần đúng với LIKE)
- Quy tắc xử lý:
  1. Lọc cứng các bản ghi có ObjectType thuộc nhóm ['MENU', 'BUTTON'] và FlagActive == "1".
  2. Nếu có truyền ObjectName, áp dụng điều kiện lọc LIKE %ObjectName%.
  3. Sắp xếp kết quả theo ObjectCode tăng dần.
  4. Trả về danh sách bản ghi hoặc null nếu rỗng.
- Bảng mã lỗi: Chua xac minh.
- Ví dụ Request:
    {
      "ObjectName": "Menu"
    }
- Ví dụ Response: Chua co lan chay mau.

### Endpoint 3: Lấy danh sách chức năng đang hoạt động (GetFunctionActive)
- Route & HTTP Method: POST /api/sys_object/getfunctionactive
- Tham số Request: Không bắt buộc.
- Quy tắc xử lý:
  1. Lọc các bản ghi có ObjectType == "FUNC" và FlagActive == "1".
  2. Sắp xếp theo ObjectCode tăng dần.
  3. Trả về danh sách chức năng hoặc null nếu rỗng.
- Bảng mã lỗi: Chua xac minh.
- Ví dụ Request:
    {}
- Ví dụ Response: Chua co lan chay mau.

### Endpoint 4: Lấy danh sách chức năng theo mã đối tượng (GetFunctionByObjectCode)
- Route & HTTP Method: POST /api/sys_object/getfunctionbyobjectcode
- Tham số Request (Sys_Object_GetFunctionByObjectCodeReq):
  - ObjectCode | string? | Có | null | Mã đối tượng menu cần tra cứu cấu hình chức năng
- Quy tắc xử lý:
  1. Đọc tệp cấu hình SysObjectSetting.config từ thư mục App_Data.
  2. Tìm kiếm theo ObjectCode không phân biệt hoa/thường, lấy danh sách các FunctionCodes liên quan.
  3. Trả về mảng danh sách chuỗi mã chức năng (nếu rỗng trả về mảng rỗng []).
- Bảng mã lỗi: Chua xac minh.
- Ví dụ Request:
    {
      "ObjectCode": "MNU01"
    }
- Ví dụ Response: Chua co lan chay mau.

### Endpoint 5: Lấy toàn bộ chức năng kèm cờ chọn theo mã đối tượng (GetAllFunctionBy_ObjectCode)
- Route & HTTP Method: POST /api/sys_object/getallfunctionby_objectcode
- Tham số Request (Sys_Object_GetAllFunctionBy_ObjectCodeReq):
  - ObjectCode | string? | Có | null | Mã đối tượng để so sánh cấu hình
- Quy tắc xử lý:
  1. Lấy danh sách Sys_Object có ObjectType == "FUNC" và FlagActive == "1", sắp xếp theo ObjectCode asc.
  2. Đọc tệp cấu hình SysObjectSetting.config để trích xuất danh sách FunctionCodes gắn với ObjectCode truyền vào.
  3. Duyệt danh sách chức năng, gán FlagSelected = "1" nếu mã chức năng tồn tại trong danh sách cấu hình, ngược lại "0".
  4. Trả về danh sách đối tượng kèm cờ FlagSelected, hoặc null nếu không có chức năng nào.
- Bảng mã lỗi: Chua xac minh.
- Ví dụ Request:
    {
      "ObjectCode": "MNU01"
    }
- Ví dụ Response: Chua co lan chay mau.

### Endpoint 6: Lấy danh sách toàn bộ đối tượng hệ thống (Get)
- Route & HTTP Method: POST /api/sys_object/get
- Tham số Request: Không nhận tham số.
- Quy tắc xử lý:
  1. Truy vấn toàn bộ bảng Sys_Object, sắp xếp theo ObjectCode tăng dần (không lọc trạng thái FlagActive).
  2. Trả về danh sách toàn bộ đối tượng; nếu rỗng trả về null.
- Bảng mã lỗi: Chua xac minh.
- Ví dụ Request:
    {}
- Ví dụ Response: Chua co lan chay mau.

## 3. Khác biệt so với nguồn & GAP
- Các endpoint xử lý cấu hình dựa trên tệp XML (SysObjectSetting.config) yêu cầu tệp tồn tại trong thư mục App_Data của ứng dụng; nếu không có, hệ thống sẽ trả về danh sách rỗng thay vì ném lỗi ngoại lệ nghiêm trọng.
- Một số phương thức truy vấn như Get không áp dụng bộ lọc trạng thái động từ session giống như hệ thống gate gốc mà lấy trực tiếp toàn bộ dữ liệu sắp xếp theo thứ tự mã.

## 4. Nhật ký thay đổi
- Hạng mục #317: Port Sys_ObjectController/GetAllMenuActive.
- Hạng mục #318: Port Sys_ObjectController/GetAllMenuButtonActive.
- Hạng mục #319: Port Sys_ObjectController/GetFunctionActive.
- Hạng mục #320: Port Sys_ObjectController/GetFunctionByObjectCode.
- Hạng mục #321: Port Sys_ObjectController/GetAllFunctionBy_ObjectCode.
- Hạng mục #322: Port Sys_ObjectController/Get.

TOM TAT
FILE_DA_TAO_SUA: docs/api/Sys_ObjectController.md
PHAM_VI_PHU: Sys_ObjectControllerPort (6 endpoint: getallmenuactive, getallmenubuttonactive, getfunctionactive, getfunctionbyobjectcode, getallfunctionby_objectcode, get)
KHONG_XAC_MINH: Mã lỗi ErridnLoyalty chi tiết do các action chủ yếu trả về dữ liệu thuần hoặc bắt ngoại lệ chung; response thực tế chưa có do chưa chạy app.
LENH_DA_CHAY: Chua chay lenh (che do WEB).
VI_PHAM_QUY_TAC: Khong.
