Phạm vi: Controller quản lý danh mục nhóm kỹ năng (Mst_SkillGroup)
Cập nhật: 2026-10-16
Phiên bản mã: 7973495
Trạng thái: Nháp - chờ review

## 1. Tóm tắt

- Mục đích nghiệp vụ: Cung cấp các chức năng tìm kiếm phân trang và lấy toàn bộ danh sách nhóm kỹ năng đang hoạt động trong hệ thống MiniLoyalty.
- Bảng nguồn:
  - Gate: Mst_SkillGroupController.cs:25-105 (Search), Mst_SkillGroupController.cs:107-167 (GetAllActive), Mst_SkillGroupController.cs:171-199 (strWhereClause)
  - API: MstSkillGroupController.cs:17-88 (WA_Mst_SkillGroup_Get)
  - Biz: MasterData.cs:43871-43961 (WAS_Mst_SkillGroup_Get), MasterData.cs:43741-43869 (Mst_SkillGroup_Get), MasterData.cs:43584-43739 (Mst_SkillGroup_GetX)
- Bảng cấu trúc cột (Mst_SkillGroup):
  - SkillGroupCode | string | Khóa chính (PK), Bắt buộc | Mã nhóm kỹ năng (Mst_SkillGroup.cs:11)
  - NetworkID | string? | Nullable | NetworkID (Mst_SkillGroup.cs:12)
  - SkillGroupName | string? | Nullable | Tên nhóm kỹ năng (Mst_SkillGroup.cs:13)
  - FlagActive | string? | Nullable | Trạng thái hoạt động '0' hoặc '1' (Mst_SkillGroup.cs:14)
  - LogLUDTimeUTC | DateTime? | Nullable | Thời gian cập nhật cuối UTC (Mst_SkillGroup.cs:15)
  - LogLUBy | string? | Nullable | Người cập nhật cuối (Mst_SkillGroup.cs:16)

## 2. Từng endpoint

### Endpoint 1: Tìm kiếm nhóm kỹ năng (Search)
- Route & HTTP Method: POST /api/mst_skillgroup/search
- Tham số Request (Mst_SkillGroupSearchReq):
  - OrgID | string? | Không | null | Nhận vào request nhưng strWhereClause không sử dụng (không lọc)
  - KeyWord | string? | Không | null | Từ khóa tìm kiếm theo tên nhóm kỹ năng (SkillGroupName)
  - FlagActive | string? | Không | null | Lọc theo trạng thái hoạt động (so sánh bằng tuyệt đối)
  - Ft_PageIndex | int | Không | 0 | Chỉ số trang hiện tại
  - Ft_PageSize | int | Không | 10 | Số bản ghi trên mỗi trang
- Quy tắc xử lý:
  1. Tính toán vị trí bắt đầu recordStart = pageIndex * pageSize.
  2. Xây dựng query với điều kiện LIKE trên SkillGroupName nếu KeyWord không trống.
  3. Xây dựng điều kiện lọc bằng FlagActive nếu không trống.
  4. Sắp xếp theo SkillGroupCode, đếm tổng số bản ghi và phân trang lấy dữ liệu.
  5. Trả về kết quả dạng PageInfo chứa danh sách bản ghi và thông tin phân trang.
- Bảng mã lỗi: Chua xac minh (Không có khai báo ngoại lệ ErridnLoyalty cụ thể trong mã port).
- Ví dụ Request:
    {
      "OrgID": "DEMO",
      "KeyWord": "Bán hàng",
      "FlagActive": "1",
      "Ft_PageIndex": 0,
      "Ft_PageSize": 10
    }
- Ví dụ Response: Chua co lan chay mau.

### Endpoint 2: Lấy tất cả nhóm kỹ năng đang hoạt động (GetAllActive)
- Route & HTTP Method: POST /api/mst_skillgroup/getallactive
- Tham số Request: Không có (dùng body rỗng hoặc không bắt buộc).
- Quy tắc xử lý:
  1. Gọi phương thức nội bộ GetXAsync với tham số KeyWord = null, FlagActive = "1", recordStart = 0, recordCount = 123456000.
  2. Trả về đối tượng chứa Data là danh sách nhóm kỹ năng active nếu số lượng lớn hơn 0, ngược lại trả về null.
- Bảng mã lỗi: Chua xac minh.
- Ví dụ Request:
    {}
- Ví dụ Response: Chua co lan chay mau.

## 3. Khác biệt so với nguồn & GAP
- Tham số OrgID được nhận ở tầng gate trong request Search nhưng không được sử dụng trong strWhereClause gốc cũng như mã port.
- Dữ liệu phản hồi chuẩn hóa theo định dạng JSON của cổng Port 1:1, chưa tích hợp đầy đủ kiểm thử chạy thực tế trên môi trường live.

## 4. Nhật ký thay đổi
- Hạng mục #243: Port Mst_SkillGroupController/Search.
- Hạng mục #244: Port Mst_SkillGroupController/GetAllActive.

TOM TAT
FILE_DA_TAO_SUA: docs/api/Mst_SkillGroupController.md
PHAM_VI_PHU: Mst_SkillGroupControllerPort (2 endpoint: search, getallactive)
KHONG_XAC_MINH: Mã lỗi cụ thể ErridnLoyalty do controller port không chứa try/catch hoặc mã lỗi tường minh; response thực tế chưa có do chưa chạy app.
LENH_DA_CHAY: Chua chay lenh (che do WEB).
VI_PHAM_QUY_TAC: Khong.
