Phạm vi: Controller phân quyền truy cập hệ thống (Sys_AccessController)
Cập nhật: 2026-10-16
Phiên bản mã: 7973495
Trạng thái: Nháp - chờ review

## 1. Tóm tắt

- Mục đích nghiệp vụ: Quản lý và tra cứu phân quyền truy cập chức năng hệ thống theo nhóm người dùng, bao gồm các chức năng tìm kiếm, lấy theo mã nhóm, lấy theo mã đối tượng, lấy toàn bộ chức năng theo đối tượng, lưu phân quyền menu/button trong nhóm và lưu chức năng trong menu.
- Bảng nguồn:
  - Gate: Sys_AccessController.cs:27-109 (Search), Sys_AccessController.cs:110-217 (Get), Sys_AccessController.cs:220-285 (GetByGroupCode), Sys_AccessController.cs:288-402 (GetAllFunctionBy_ObjectCode), Sys_AccessController.cs:405-472 (GetByObjectCode), Sys_AccessController.cs:474-576 (SaveMenuButtonInGroup), Sys_AccessController.cs:580-756 (SaveFuncInMenu)
  - API: SysAccessController.cs:20-91 (WA_Sys_Access_Get), SysAccessController.cs:94-165 (WA_Sys_Access_Save)
  - Biz: System.cs:757-850 (WAS_Sys_Access_Get), System.cs:1284-1366 (WAS_Sys_Access_Save), System.cs:852-1055 (Sys_Access_Save), System.cs:290-512 (Sys_Access_Get)
- Bảng cấu trúc cột (Sys_Access):
  - GroupCode | string | Khóa chính (PK) | Mã nhóm người dùng (Sys_Access.cs:11)
  - ObjectCode | string | Khóa chính (PK) | Mã đối tượng chức năng (Sys_Access.cs:12)
  - LogLUDTimeUTC | DateTime? | Nullable | Thời gian cập nhật cuối UTC (Sys_Access.cs:13)
  - LogLUBy | string? | Nullable | Người cập nhật cuối (Sys_Access.cs:14)

## 2. Từng endpoint

### Endpoint 1: Tìm kiếm phân quyền truy cập (Search)
- Route & HTTP Method: POST /api/sys_access/search
- Tham số Request (SysAccessSearchReq):
  - OrgID | string? | Không | null | Mã đơn vị tổ chức
  - KeyWord | string? | Không | null | Từ khóa tìm kiếm
  - FlagActive | string? | Không | null | Trạng thái hoạt động
  - Ft_PageIndex | int | Không | 0 | Chỉ số trang hiện tại
  - Ft_PageSize | int | Không | 10 | Số bản ghi trên mỗi trang
- Quy tắc xử lý:
  1. Tính toán vị trí bắt đầu recordStart = pageIndex * pageSize.
  2. Gọi phương thức nội bộ GetXAsync để lấy dữ liệu phân quyền kết hợp thông tin đối tượng từ bảng Sys_Object.
  3. Tính toán tổng số trang pageCount dựa trên tổng số bản ghi và kích thước trang.
  4. Trả về kết quả bọc trong cấu trúc PageInfo.
- Bảng mã lỗi: Chua xac minh.
- Ví dụ Request:
    {
      "OrgID": "DEMO",
      "KeyWord": "G01",
      "FlagActive": "1",
      "Ft_PageIndex": 0,
      "Ft_PageSize": 10
    }
- Ví dụ Response: Chua co lan chay mau.

### Endpoint 2: Lấy toàn bộ phân quyền (Get)
- Route & HTTP Method: POST /api/sys_access/get
- Tham số Request: Không có.
- Quy tắc xử lý:
  1. Gọi GetXAsync với các tham số mặc định (recordStart = 0, recordCount = 123456000).
  2. Trả về danh sách phân quyền; nếu rỗng, trả về Data = null.
- Bảng mã lỗi: Chua xac minh.
- Ví dụ Request:
    {}
- Ví dụ Response: Chua co lan chay mau.

### Endpoint 3: Lấy phân quyền theo mã nhóm (GetByGroupCode)
- Route & HTTP Method: POST /api/sys_access/getbygroupcode
- Tham số Request (SysAccessGetByGroupCodeReq):
  - GroupCode | string? | Có | null | Mã nhóm người dùng cần tra cứu
- Quy tắc xử lý:
  1. Kiểm tra nếu GroupCode rỗng hoặc null, trả về ngay Success = true, Data = null.
  2. Gọi GetXAsync lọc theo GroupCode được chỉ định.
  3. Trả về danh sách kết quả hoặc null nếu rỗng.
- Bảng mã lỗi: Chua xac minh.
- Ví dụ Request:
    {
      "GroupCode": "G01"
    }
- Ví dụ Response: Chua co lan chay mau.

### Endpoint 4: Lấy phân quyền theo mã đối tượng (GetByObjectCode)
- Route & HTTP Method: POST /api/sys_access/getbyobjectcode
- Tham số Request (SysAccessGetByObjectCodeReq):
  - ObjectCode | string? | Có | null | Mã đối tượng chức năng
- Quy tắc xử lý:
  1. Kiểm tra nếu ObjectCode rỗng hoặc null, trả về ngay Success = true, Data = null.
  2. Gọi GetXAsync lọc theo ObjectCode được chỉ định.
  3. Trả về danh sách kết quả hoặc null nếu rỗng.
- Bảng mã lỗi: Chua xac minh.
- Ví dụ Request:
    {
      "ObjectCode": "MNU01"
    }
- Ví dụ Response: Chua co lan chay mau.

### Endpoint 5: Lấy tất cả chức năng theo mã đối tượng (GetAllFunctionBy_ObjectCode)
- Route & HTTP Method: POST /api/sys_access/getallfunctionby_objectcode
- Tham số Request (SysAccessGetAllFunctionByObjectCodeReq):
  - GroupCode | string? | Không | null | Mã nhóm người dùng
  - OrgID | string? | Không | null | Mã đơn vị
  - NetworkID | string? | Không | null | Mã mạng
  - Tid | string? | Không | null | Mã giao dịch
- Quy tắc xử lý:
  1. Lấy danh sách Sys_Object có ObjectType thuộc nhóm ['MENU', 'BUTTON'] và FlagActive = '1', sắp xếp theo ObjectCode asc.
  2. Nếu danh sách Sys_Object rỗng, trả về Data = null.
  3. Nếu GroupCode không trống, truy vấn danh sách phân quyền Sys_Access của nhóm để xác định FlagSelected.
- Bảng mã lỗi:
  - Ngoại lệ NullReferenceException khi groupCode không được cung cấp hoặc chưa có dữ liệu truy cập (được bắt và trả về lỗi qua PortExceptionFilter).
- Ví dụ Request:
    {
      "GroupCode": "G01",
      "OrgID": "DEMO",
      "NetworkID": "DEMO",
      "Tid": "1"
    }
- Ví dụ Response: Chua co lan chay mau.

### Endpoint 6: Lưu phân quyền menu và nút trong nhóm (SaveMenuButtonInGroup)
- Route & HTTP Method: POST /api/sys_access/savemenubuttoningroup
- Tham số Request (SysAccessSaveMenuButtonInGroupReq):
  - Sys_Group | Sys_Group_Input? | Có | null | Thông tin nhóm chứa GroupCode
  - Lst_Sys_Access | List<Sys_Access_Input>? | Không | null | Danh sách đối tượng phân quyền gồm ObjectCode và so_ObjectType
- Quy tắc xử lý:
  1. Kiểm tra request body không được null.
  2. Phân loại các đối tượng Menu và Button, xử lý độ phân giải menu qua cấu hình SysObjectSetting.config.
  3. Thực hiện giao dịch (Transaction): kiểm tra tồn tại của nhóm và các đối tượng, xóa sạch phân quyền cũ của nhóm và thêm mới danh sách phân quyền.
- Bảng mã lỗi:
  - Sys_Access_Save: Lỗi khi dữ liệu đầu vào null hoặc lỗi phát sinh trong giao dịch.
  - Sys_Group_CheckDB_GroupCodeNotFound: Không tìm thấy mã nhóm.
  - Sys_Object_CheckDB_ObjectCodeNotFound: Không tìm thấy mã đối tượng.
  - Sys_Object_CheckDB_FlagActiveNotMatched: Đối tượng không ở trạng thái hoạt động.
- Ví dụ Request:
    {
      "Sys_Group": {
        "GroupCode": "G01"
      },
      "Lst_Sys_Access": [
        {
          "ObjectCode": "MNU01",
          "so_ObjectType": "MENU"
        }
      ]
    }
- Ví dụ Response: Chua co lan chay mau.

### Endpoint 7: Lưu chức năng trong menu (SaveFuncInMenu)
- Route & HTTP Method: POST /api/sys_access/savefuncinmenu
- Tham số Request (SysAccessSaveFuncInMenuReq):
  - MenuCode | string? | Không | null | Mã menu
  - Lst_FuncCode | List<string>? | Không | null | Danh sách mã chức năng
- Quy tắc xử lý:
  1. Kiểm tra request không được null, cập nhật cấu hình danh sách chức năng đối tượng vào tệp cấu hình SysObjectSetting.config.
  2. Truy vấn danh sách quyền liên quan đến menu, xử lý lại danh sách quyền của từng nhóm và thực hiện lưu trữ giao dịch lưu phân quyền.
- Bảng mã lỗi:
  - Sys_Access_Save: Lỗi phát sinh khi đối tượng request null hoặc lỗi giao dịch cập nhật.
- Ví dụ Request:
    {
      "MenuCode": "MNU01",
      "Lst_FuncCode": ["FUNC1", "FUNC2"]
    }
- Ví dụ Response: Chua co lan chay mau.

## 3. Khác biệt so với nguồn & GAP
- Các tham số OrgID, KeyWord, FlagActive truyền vào phương thức Search thực tế không được áp dụng trong câu lệnh lọc của hàm strWhereClause tương ứng tại nguồn gốc (luôn mặc định (1=1)).
- Bảng Sys_Object được xử lý qua left join mô phỏng kết hợp dữ liệu ánh xạ trong bộ nhớ, một số cơ chế kiểm tra phân quyền như Sys_Access_CheckDeny được coi là GAP ở hệ thống MiniLoyalty.

## 4. Nhật ký thay đổi
- Hạng mục #300: Port Sys_AccessController/Search.
- Hạng mục #301: Port Sys_AccessController/Get.
- Hạng mục #302: Port Sys_AccessController/GetByGroupCode.
- Hạng mục #303: Port Sys_AccessController/GetAllFunctionBy_ObjectCode.
- Hạng mục #304: Port Sys_AccessController/GetByObjectCode.
- Hạng mục #305: Port Sys_AccessController/SaveMenuButtonInGroup.
- Hạng mục #306: Port Sys_AccessController/SaveFuncInMenu.

TOM TAT
FILE_DA_TAO_SUA: docs/api/Sys_AccessController.md
PHAM_VI_PHU: Sys_AccessControllerPort (7 endpoint: search, get, getbygroupcode, getbyobjectcode, getallfunctionby_objectcode, savemenubuttoningroup, savefuncinmenu)
KHONG_XAC_MINH: Mã lỗi ErridnLoyalty chi tiết phát sinh từ tầng Biz gốc ngoài các mã được ánh xạ trong PortResult; response thực tế chưa có do chưa chạy app.
LENH_DA_CHAY: Chua chay lenh (che do WEB).
VI_PHAM_QUY_TAC: Khong.
