# Phạm vi: api/sys_user
# Cập nhật: 2026-10-18
# Phiên bản mã: d30cdab
# Trạng thái: Nháp — chờ review

## 1. Tóm tắt
- **Mục đích nghiệp vụ**: Quản lý thông tin người dùng trong hệ thống (MiniLoyalty), bao gồm tìm kiếm, tra cứu theo mã/MST, lấy thông tin người dùng hiện tại, tạo mới, cập nhật, xóa (đơn lẻ và hàng loạt), import từ Excel và export danh sách/template Excel.
- **Bảng nguồn**: 
  - Gate: `idn.Loyalty.WebAPI\Controllers\Sys_UserController.cs:498-561, 565-683, 685-837, 841-912, 916-976, 978-1042, 1046-1235, 1240-1318`
  - API Biz: `idn.Loyalty.Biz.Web\Controllers\SysUserController.cs:199-270, 421-492, 569-640, 643-714`
  - Biz LIVE: `idn.Loyalty.Biz\System.cs:8365-8478, 7987-8363, 11222-11323, 9218-9391, 10160-10828, 12524-12617, 12243-12523, 13092-13171, 12619-12732`
- **Bảng bảng-cột (Sys_User)**:
  - `UserCode`: string (EF: PK, Required) — Mã người dùng.
  - `NetworkID`: string? (EF: string) — Mã Network.
  - `UserName`: string? (EF: string) — Tên người dùng.
  - `UserPassword`: string? (EF: string) — Mật khẩu (truy vấn trả về mask `*********`).
  - `PhoneNo`: string? (EF: string) — Số điện thoại.
  - `EMail`: string? (EF: string) — Địa chỉ email.
  - `MST`: string? (EF: string) — Mã số thuế.
  - `DepartmentCode`: string? (EF: string) — Mã phòng ban.
  - `Position`: string? (EF: string) — Vị trí công tác.
  - `OrgID`: string? (EF: string) — Mã tổ chức.
  - `CustomerCodeSys`: string? (EF: string) — Mã hệ thống khách hàng.
  - `FlagActive`: string? (EF: string) — Trạng thái kích hoạt ('1' = hoạt động, '0' = dừng).
  - `FlagDLAdmin`, `FlagSysAdmin`, `FlagNNTAdmin`: string? (EF: string) — Các cờ quản trị đại lý, hệ thống, NNT.
  - `ACId`, `ACAvatar`, `ACEmail`, `ACLanguage`, `ACName`, `ACPhone`, `ACTimeZone`: string? (EF: string) — Thông tin tài khoản tích hợp Inos.

---

## 2. Các Endpoint

### 2.1. POST /api/sys_user/search
- **Mục đích**: Tìm kiếm và phân trang danh sách người dùng theo từ khóa, tổ chức và trạng thái.
- **Request Parameters (JSON Body - `Sys_UserSearchReq`)**:
  - `OrgID`: string? (Không bắt buộc, mặc định: null) — Mã tổ chức lọc chính xác.
  - `KeyWord`: string? (Không bắt buộc, mặc định: null) — Từ khóa tìm kiếm theo %UserName%, %PhoneNo%, %EMail%, %UserCode%.
  - `FlagActive`: string? (Không bắt buộc, mặc định: null) — Trạng thái kích hoạt lọc chính xác.
  - `Ft_PageIndex`: int (Không bắt buộc, mặc định: 0) — Chỉ số trang (0-based).
  - `Ft_PageSize`: int (Không bắt buộc, mặc định: 10) — Kích thước trang.
- **Quy tắc xử lý**:
  1. Khởi tạo truy vấn bảng `Sys_User` (AsNoTracking).
  2. Lọc theo `KeyWord` trên 4 cột (`UserName`, `PhoneNo`, `EMail`, `UserCode`) bằng điều kiện `LIKE`.
  3. Lọc theo `OrgID` và `FlagActive` nếu được cung cấp.
  4. Sắp xếp kết quả theo `UserCode` tăng dần.
  5. Tính tổng số bản ghi (`itemCount`) và thực hiện phân trang (`Skip`, `Take`).
  6. Left join sang các bảng `Mst_Department`, `Mst_Customer`, `Mst_Org`, `Map_DealerCP_Org`, `Mst_DealerCP`.
  7. Ẩn mật khẩu thành `*********` và định dạng `ACAvatar`.
- **Mã lỗi (`ErridnLoyalty.*`)**: Chưa có mã lỗi ngoại lệ chuyên biệt; lỗi hệ thống trả về HTTP 400 qua `PortResult.Fail`.
- **Ví dụ Request**:
    {
      "OrgID": "ORG01",
      "KeyWord": "Admin",
      "FlagActive": "1",
      "Ft_PageIndex": 0,
      "Ft_PageSize": 10
    }
- **Ví dụ Response (Lấy từ lần chạy thật / Dữ liệu mẫu)**:
    {
      "success": true,
      "data": {
        "dataList": [
          {
            "myIdxSeq": 0,
            "userCode": "U01",
            "networkID": "DEMO",
            "userName": "Admin System",
            "userPassword": "********_",
            "phoneNo": "0901000001",
            "eMail": "admin@example.com",
            "mst": "0100000001",
            "departmentCode": "KD",
            "position": "Quản trị viên",
            "flagDLAdmin": "1",
            "flagSysAdmin": "1",
            "flagNNTAdmin": "0",
            "orgID": "ORG01",
            "customerCodeSys": "CUST01",
            "customerCode": "KH01",
            "customerName": "Khách hàng Doanh Nghiệp 01",
            "flagActive": "1",
            "acId": "AC01",
            "acAvatar": "/avatars/u01.png"
          }
        ],
        "itemCount": 1,
        "pageCount": 1,
        "pageIndex": 0,
        "pageSize": 10
      }
    }

---

### 2.2. POST /api/sys_user/getbyusercode
- **Mục đích**: Lấy chi tiết thông tin một người dùng theo mã `UserCode`.
- **Request Parameters (JSON Body - `Sys_UserGetByUserCodeReq`)**:
  - `UserCode`: string? (Bắt buộc) — Mã người dùng cần tra cứu.
- **Quy tắc xử lý**:
  1. Nếu `UserCode` rỗng hoặc không tìm thấy, trả về `Success = true, Data = null`.
  2. Truy vấn `Sys_User` theo `UserCode` (khớp chính xác).
  3. Left join các bảng liên quan (`Mst_Department`, `Mst_Customer`, `Mst_Org`, `Map_DealerCP_Org`, `Mst_DealerCP`).
  4. Che mật khẩu `UserPassword = "*********"`.
  5. Xử lý đường dẫn `ACAvatar`.
- **Mã lỗi**: Trả về 400 nếu xảy ra ngoại lệ hệ thống.
- **Ví dụ Request**:
    {
      "UserCode": "U01"
    }
- **Ví dụ Response**:
    {
      "success": true,
      "data": {
        "userCode": "U01",
        "userName": "Admin System",
        "userPassword": "*********",
        "phoneNo": "0901000001",
        "eMail": "admin@example.com",
        "mst": "0100000001",
        "flagActive": "1"
      }
    }

---

### 2.3. POST /api/sys_user/getbymst
- **Mục đích**: Lấy danh sách người dùng theo Mã số thuế (`MST`).
- **Request Parameters (JSON Body - `Sys_UserGetByMSTReq`)**:
  - `MST`: string? (Bắt buộc) — Mã số thuế cần tra cứu.
- **Quy tắc xử lý**:
  1. Nếu `MST` rỗng/null, trả về `Success = true, Data = null`.
  2. Lọc chính xác theo cột `MST`.
  3. Sắp xếp theo `UserCode` tăng dần và left join phòng ban, khách hàng.
  4. Che mật khẩu thành `*********`.
- **Ví dụ Request**:
    {
      "MST": "0100000001"
    }
- **Ví dụ Response**:
    {
      "success": true,
      "data": [
        {
          "userCode": "U01",
          "userName": "Admin System",
          "userPassword": "*********",
          "mst": "0100000001",
          "flagActive": "1"
        }
      ]
    }

---

### 2.4. POST /api/sys_user/getforcurrentuser
- **Mục đích**: Lấy thông tin cấu hình/quyền cơ bản của người dùng hiện tại phục vụ định danh phiên.
- **Request Parameters (JSON Body - `Sys_UserGetForCurrentUserReq`)**:
  - `UserCode`: string? (Bắt buộc) — Mã người dùng.
- **Quy tắc xử lý**:
  1. Nếu `UserCode` rỗng, trả về `Data = null`.
  2. Truy vấn `Sys_User` và chỉ chọn 6 cột cốt lõi (`UserCode`, `UserName`, `MST`, `FlagDLAdmin`, `FlagSysAdmin`, `FlagActive`).
- **Ví dụ Request**:
    {
      "UserCode": "U01"
    }
- **Ví dụ Response**:
    {
      "success": true,
      "data": {
        "userCode": "U01",
        "userName": "Admin System",
        "mst": "0100000001",
        "flagDLAdmin": "1",
        "flagSysAdmin": "1",
        "flagActive": "1"
      }
    }

---

### 2.5. POST /api/sys_user/getallactive
- **Mục đích**: Lấy danh sách toàn bộ người dùng đang hoạt động (`FlagActive = "1"`).
- **Request Parameters**: Không yêu cầu tham số bắt buộc.
- **Quy tắc xử lý**:
  1. Lọc `FlagActive = "1"`, sắp xếp theo `UserCode` asc.
  2. Left join các bảng liên quan. Trả về cấu trúc chứa `Lst_Sys_User`.
- **Ví dụ Request**:
    {}
- **Ví dụ Response**:
    {
      "success": true,
      "data": {
        "lst_Sys_User": [
          {
            "userCode": "U01",
            "userName": "Admin System",
            "flagActive": "1"
          }
        ]
      }
    }

---

### 2.6. POST /api/sys_user/create
- **Mục đích**: Tạo mới người dùng hệ thống.
- **Request Parameters (JSON Body - `Sys_UserCreateReq`)**:
  - `UserCode`: string? (Không bắt buộc; hệ thống tự gán bằng `EMail` nếu trống).
  - `UserName`: string? (Bắt buộc) — Tên người dùng.
  - `UserPassword`: string? (Bắt buộc) — Mật khẩu thô (sẽ được hash MD5 + muối).
  - `EMail`: string? (Bắt buộc) — Địa chỉ email.
  - `MST`: string? (Bắt buộc) — Mã số thuế thuộc tổ chức NNT hợp lệ.
  - `DepartmentCode`: string? (Không bắt buộc) — Mã phòng ban.
  - `Position`: string? (Không bắt buộc) — Chức vụ.
  - `FlagDLAdmin`, `FlagSysAdmin`, `FlagNNTAdmin`: string? (Không bắt buộc) — Các cờ quản trị.
  - `flagExist`: string? (Không bắt buộc) — Nếu bằng "1", dùng mật khẩu mặc định cấu hình (`PassDefault` hoặc `idocNet@123`).
- **Quy tắc xử lý & Guards**:
  1. `UserCode = EMail`.
  2. Kiểm tra `UserCode` đã tồn tại chưa (`Sys_User_CheckDB`).
  3. Kiểm tra độ dài `UserName` >= 1.
  4. Kiểm tra mật khẩu hợp lệ (hoặc gán mặc định nếu `flagExist = "1"`).
  5. Kiểm tra `MST` tồn tại trong bảng `Mst_NNT` và có `FlagActive = "1"`.
  6. Kiểm tra quyền `FlagSysAdmin` nếu có header `WAUserCode`.
  7. Hash mật khẩu bằng MD5 + muối `idocNet`, lưu DB trong transaction.
- **Mã lỗi (`ErridnLoyalty.*`)**:
  - `Sys_User_Create`: Lỗi chung hoặc body null.
  - `Sys_User_Create_InvalidUserCode`: Mã người dùng không hợp lệ/rỗng.
  - `Sys_User_CheckDB_UserCodeExist`: Tài khoản đã tồn tại.
  - `Sys_User_Create_InvalidUserName`: Tên người dùng trống.
  - `Sys_User_Create_InvalidUserPassword`: Mật khẩu trống.
  - `Mst_NNT_CheckDB_NNTNotFound`: Không tìm thấy MST hoặc NNT không hoạt động.
  - `Sys_User_Create_NNTNotAdmin`: Không đủ quyền bật cờ quản trị hệ thống.
- **Ví dụ Request**:
    {
      "UserName": "New User",
      "UserPassword": "Password123",
      "EMail": "newuser@example.com",
      "MST": "0100000001",
      "FlagSysAdmin": "0"
    }
- **Ví dụ Response**:
    {
      "success": true,
      "data": null
    }

---

### 2.7. POST /api/sys_user/createcp
- **Mục đích**: Tạo mới người dùng cho đại lý (CP).
- **Request Parameters**: Tương tự `Sys_UserCreateReq`, kết hợp thêm `mdcpo_DLCPCode`.
- **Quy tắc xử lý**:
  1. Gán `EMail = UserCode`.
  2. Tra cứu tự động `MST` thông qua mã đại lý (`mdcpo_DLCPCode`) liên kết với `Map_DealerCP_Org`, `Mst_DealerCP` và `Mst_NNT`.
  3. Thực hiện các bước tạo tương tự action `create`.
- **Mã lỗi**: Các mã lỗi giống `create` kết hợp lỗi tra cứu đại lý.
- **Ví dụ Request**:
    {
      "UserCode": "NEWCP",
      "UserName": "Dealer User",
      "UserPassword": "Password123",
      "mdcpo_DLCPCode": "DL01"
    }
- **Ví dụ Response**:
    {
      "success": true,
      "data": null
    }

---

### 2.8. POST /api/sys_user/update
- **Mục đích**: Cập nhật thông tin người dùng.
- **Request Parameters (JSON Body - `Sys_UserUpdateReq`)**:
  - `UserCode`: string? (Bắt buộc) — Mã tài khoản cần sửa.
  - `UserName`: string? — Tên mới.
  - `PhoneNo`: string? — Số điện thoại mới.
  - `EMail`: string? — Email mới.
  - `Position`: string? — Chức vụ mới.
  - `Avatar`: string? — Đường dẫn ảnh đại diện.
  - `FlagDLAdmin`, `FlagSysAdmin`, `FlagNNTAdmin`, `FlagActive`: string? — Các cờ trạng thái/quyền.
- **Quy tắc xử lý**:
  1. Kiểm tra tồn tại `UserCode` trong CSDL (`Sys_User_CheckDB`).
  2. Kiểm tra tính hợp lệ của `UserName` nếu được cập nhật.
  3. Cập nhật các trường cấu hình trong danh sách cột cho phép (`UserName`, `PhoneNo`, `FlagSysAdmin`, `FlagNNTAdmin`, `FlagActive`), ghi nhận `LogLUDTimeUTC` và `LogLUBy`.
- **Mã lỗi**:
  - `Sys_User_Update`: Lỗi tham số / body null.
  - `Sys_User_CheckDB_UserCodeNotFound`: Không tìm thấy tài khoản.
  - `Sys_User_Update_InvalidUserName`: Tên người dùng không hợp lệ.
- **Ví dụ Request**:
    {
      "UserCode": "U01",
      "UserName": "Admin Updated",
      "PhoneNo": "0909999999",
      "FlagActive": "1"
    }
- **Ví dụ Response**:
    {
      "success": true,
      "data": null
    }

---

### 2.9. POST /api/sys_user/delete
- **Mục đích**: Xóa một người dùng theo `UserCode`.
- **Request Parameters (JSON Body - `Sys_UserDeleteReq`)**:
  - `UserCode`: string? (Bắt buộc) — Mã người dùng cần xóa.
- **Quy tắc xử lý**:
  1. Kiểm tra `UserCode` không được rỗng.
  2. Kiểm tra sự tồn tại trong DB (`Sys_User_CheckDB`).
  3. Xóa các bản ghi liên quan trong bảng phụ `Sys_UserInGroup`.
  4. Xóa bản ghi trong bảng `Sys_User` trong transaction.
- **Mã lỗi**:
  - `invalid_usercode`: `UserCode` rỗng hoặc null.
  - `Sys_User_CheckDB_UserCodeNotFound`: Tài khoản không tồn tại.
- **Ví dụ Request**:
    {
      "UserCode": "U03"
    }
- **Ví dụ Response**:
    {
      "success": true,
      "data": null
    }

---

### 2.10. POST /api/sys_user/deletemultiple
- **Mục đích**: Xóa nhiều người dùng cùng lúc.
- **Request Parameters**: Danh sách JSON Body (`List<Sys_UserDeleteReq>`).
- **Quy tắc xử lý**:
  1. Lặp qua từng phần tử và gọi hàm xóa độc lập (`DeleteXAsync`) trong các transaction riêng biệt (hành vi xóa một phần: nếu lỗi ở phần tử thứ k, các phần tử trước đã commit thành công).
- **Mã lỗi**: Trả về lỗi của phần tử đầu tiên gặp sự cố.
- **Ví dụ Request**:
    [
      { "UserCode": "U02" },
      { "UserCode": "U03" }
    ]
- **Ví dụ Response**:
    {
      "success": true,
      "data": null
    }

---

### 2.11. POST /api/sys_user/import
- **Mục đích**: Nhập khẩu danh sách người dùng từ tệp Excel (.xlsx).
- **Request Parameters (Multipart Form-Data)**:
  - `file`: IFormFile (Bắt buộc) — Tệp Excel định dạng `.xlsx`.
  - `orgId`: string? (Không bắt buộc) — Mã tổ chức.
- **Quy tắc xử lý**:
  1. Kiểm tra file upload hợp lệ (không rỗng, đuôi `.xlsx`).
  2. Đọc sheet Excel với 4 cột yêu cầu: `UserCode`, `UserName`, `UserPassword`, `mdcpo_DLCPCode`.
  3. Kiểm tra tính hợp lệ toàn bộ các dòng (Kiểm tra trước - Check null). Nếu có lỗi, hủy bỏ toàn bộ.
  4. Lặp qua từng dòng, tra cứu MST từ mã đại lý và gọi hàm tạo mới độc lập cho từng dòng (Import một phần).
- **Mã lỗi**:
  - `Sys_User_Import`: Lỗi file không hợp lệ / thiếu dữ liệu / sai số cột.
  - `invalid_DLCPCode`: Không tìm thấy mã đại lý tương ứng.
- **Ví dụ Request (Multipart form-data)**:
  - `file`: (Binary .xlsx file)
- **Ví dụ Response**:
    {
      "success": true,
      "data": null
    }

---

### 2.12. POST /api/sys_user/export
- **Mục đích**: Xuất danh sách người dùng ra tệp Excel theo điều kiện tìm kiếm.
- **Request Parameters (JSON Body - `Sys_UserSearchReq`)**: Giống action `search`.
- **Quy tắc xử lý**:
  1. Truy vấn dữ liệu theo bộ lọc `KeyWord`, `OrgID`, `FlagActive`, sắp xếp theo `UserCode` asc.
  2. Nếu không có dữ liệu, báo lỗi không có dữ liệu.
  3. Xuất dữ liệu ra file Excel với 7 cột chuẩn (`UserCode`, `UserName`, `PhoneNo`, `EMail`, `MST`, `mdcpo_DLCPCode`, `FlagActive`).
  4. Trả về URL tải tệp.
- **Mã lỗi**:
  - `Sys_User_Export`: Không có dữ liệu hoặc lỗi ngoại lệ hệ thống.
- **Ví dụ Request**:
    {
      "OrgID": "ORG01",
      "FlagActive": "1"
    }
- **Ví dụ Response**:
    {
      "success": true,
      "data": {
        "url": "/uploads/excel/Sys_User_20261018.xlsx"
      }
    }

---

### 2.13. POST /api/sys_user/exporttemplate
- **Mục đích**: Tải tệp Excel mẫu dùng để import người dùng.
- **Request Parameters**: Không yêu cầu tham số.
- **Quy tắc xử lý**:
  1. Tạo danh sách rỗng và xuất Excel với 4 cột template (`UserCode`, `UserName`, `UserPassword`, `mdcpo_DLCPCode`).
  2. Trả về URL tải tệp mẫu.
- **Mã lỗi**: `Sys_User_ExportTemplate` khi có lỗi phát sinh.
- **Ví dụ Request**:
    {}
- **Ví dụ Response**:
    {
      "success": true,
      "data": {
        "url": "/uploads/excel/Sys_User_Template_20261018.xlsx"
      }
    }

---

## 3. Khác biệt so với nguồn & GAP
- **Phân quyền session**: Nguồn sử dụng session UI của hệ thống cũ (`orgUserUI.Email`, `orgUserUI.OrgId`), bản port sang MiniLoyalty nhận qua header `WAUserCode` hoặc tham số truyền vào, với mặc định cấu hình an toàn (`nFlagSysAdminRoot = 0`).
- **Tích hợp dịch vụ Inos**: Các dịch vụ bên ngoài của Inos (`Mst_ManageNotify_CreateX`, `Inos_AccountService_*`, `Inos_OrgService_*`, `Inos_LicService_*`) được bỏ qua có chủ đích do không có sẵn môi trường backend Inos tại đích.
- **Xử lý ngoại lệ / Lỗi định dạng**: Giữ nguyên hành vi nguồn, bao gồm cả các ngoại lệ tiềm ẩn như `IndexOutOfRangeException` khi phân tách chuỗi `ACAvatar` nếu không chứa ký tự `~`.

---

## 4. Nhật ký thay đổi
- Hạng mục `#323`, `#324`, `#325`, `#327`, `#328`, `#329`, `#330`, `#331`, `#332`, `#333`, `#334`, `#335`, `#336`: Port hoàn thiện Controller `Sys_UserControllerPort`, Entity `Sys_User`, `Mst_Customer` và `Sys_UserSeeder`.

---

## TOM TAT
FILE_DA_TAO_SUA: docs/api/Sys_UserController.md, docs/README.md, docs/kien-truc/TONG_QUAN.md
PHAM_VI_PHU: Sys_UserControllerPort (13 endpoint: Search, GetByUserCode, GetByMST, GetForCurrentUser, GetAllActive, Create, CreateCP, Update, Delete, DeleteMultiple, Import, Export, ExportTemplate)
KHONG_XAC_MINH: Không có điểm chưa xác minh trọng yếu; các dịch vụ Inos bên ngoài được ghi nhận GAP đúng quy định.
LENH_DA_CHAY: Đã rà soát mã nguồn C# thực tế trong worktree, kiểm tra chính xác các dòng lệnh truy vấn LINQ, EF Core và quy tắc mã lỗi `ErridnLoyalty.*`.
VI_PHAM_QUY_TAC: Không có.
