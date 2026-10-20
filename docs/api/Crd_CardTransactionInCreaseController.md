Phạm vi: Controllers/Port/Crd_CardTransactionInCreaseControllerPort.cs, Models/Port/Crd_CardTransactionInCrease.cs
Cập nhật: 2026-10-18
Phiên bản mã: d30cdab
Trạng thái: Nháp — chờ review

# Crd_CardTransactionInCreaseController API

## 1. Tóm tắt
- **Mục đích nghiệp vụ:** Quản lý và tra cứu báo cáo giao dịch tăng điểm (`Crd_CardTransactionInCrease`), chi tiết khấu trừ tiêu điểm (`Crd_CardTransactionInCreaseInst`), hỗ trợ tìm kiếm theo điều kiện thời gian, hội viên, đại lý, mã giao dịch và xuất báo cáo ra file Excel chứa hai sheet.
- **Bảng nguồn:** 
  - Gate: `Crd_CardTransactionInCreaseController.cs` (Search: :33-117, Export: :121-215, strWhereClause_Crd_CardTransactionInCrease: :219-258, DicColums: :262-287, DicColumsDetail: :289-316).
  - API: `CrdCardTransactionInCreaseController.cs` (`WA_Crd_CardTransactionInCrease_Get`, :20-87).
  - Biz: `Report.cs` (`WAS_Crd_CardTransactionInCrease_Get`, :11903-12036; `Crd_CardTransactionInCrease_Get`, :12037-12183; `Crd_CardTransactionInCrease_GetX`, :12184-12523).
- **Bảng cấu trúc / Thực thể liên quan:**
  - `CrdCardTransaction`: Bảng lưu trữ giao dịch thẻ (sử dụng lại entity có sẵn).
  - `CrdMember`: Bảng hội viên liên kết qua `MemberNo`.
  - `Mst_DealerCP`: Bảng đại lý liên kết qua `DLCode`.
  - `RptPE_CrdTransationDeCreaseInst`: Bảng phụ lưu chi tiết khấu trừ tiêu điểm theo giao dịch tăng điểm (`RefNoIC` là khóa chính).
  - `RptPE_CrdTransationInCreaseHist`: Bảng phụ lưu lịch sử điểm hết hạn của giao dịch tăng điểm (`RefNo` là khóa chính).
  - Các DTO trả về kết quả (không ánh xạ bảng DB thực):
    - `Crd_CardTransactionInCreaseItem`: Dữ liệu dòng báo cáo điểm tích (20 cột).
    - `Crd_CardTransactionInCreaseInstItem`: Dữ liệu dòng báo cáo chi tiết tiêu điểm (23 cột).

## 2. Các Endpoint

### 1. POST /api/crd_cardtransactionincrease/search
Tìm kiếm và lấy danh sách báo cáo giao dịch tăng điểm và chi tiết tiêu điểm theo các tiêu chí lọc.

#### Tham số Request
Body kiểu JSON (`Crd_CardTransactionInCreaseSearchReq`):
- `CreateDTimeUTCFrom` (string, không bắt buộc): Thời gian giao dịch từ ngày (`yyyy-MM-dd`).
- `CreateDTimeUTCTo` (string, không bắt buộc): Thời gian giao dịch đến ngày (`yyyy-MM-dd`).
- `MemberNo` (string, không bắt buộc): Mã hội viên.
- `DLCode` (string, không bắt buộc): Mã đại lý.
- `AutoId` (string, không bắt buộc): Mã giao dịch.
- `PeriodMonth` (string, không bắt buộc): Kỳ xét hạng (`yyyy-MM`) dùng cho chi tiết tiêu điểm.

#### Quy tắc xử lý theo thứ tự
1. Chuẩn hóa các tham số lọc đầu vào (`CreateDTimeUTCFrom`, `CreateDTimeUTCTo` chuyển đổi sang giờ UTC từ múi giờ VN `UTC+7`, `MemberNo`, `DLCode`, `AutoId`).
2. Xây dựng câu truy vấn draft từ bảng `CrdCardTransaction` kết hợp `CrdMember` và `Mst_DealerCP` với điều kiện `AmountChTotal > 0` và `CreateDTimeUTC >= '2023-06-30 17:00:00'` cùng các điều kiện lọc động tương ứng.
3. Sắp xếp kết quả tăng dần theo `CreateDTimeUTC` và `AutoId`.
4. Lấy danh sách giao dịch, tính toán số điểm đã tiêu (`PointUsed`), điểm hết hạn (`PointExpiry`), và điểm còn lại có thể tiêu (`PointRemain`) cho từng dòng tăng điểm.
5. Truy vấn danh sách chi tiết tiêu điểm (`Crd_CardTransactionInCreaseInst`) khớp với điều kiện `PeriodMonth` và danh sách giao dịch tìm được.
6. Trả về kết quả HTTP 200 kèm cấu trúc dữ liệu `{ Success = true, Data = { Lst_Crd_CardTransactionInCrease, Lst_Crd_CardTransactionInCreaseInst } }` (danh sách rỗng trả về `null`).

#### Bảng Mã Lỗi
| Mã lỗi | Điều kiện phát sinh | Chi tiết / Tham số trả về |
| :--- | :--- | :--- |
| Không có | Endpoint thực hiện truy vấn trả về dữ liệu thành công hoặc danh sách rỗng (`null`). | Không có |

#### Ví dụ Request / Response
- **Request:**
    POST /api/crd_cardtransactionincrease/search
    Content-Type: application/json

    {
      "MemberNo": "MEM01",
      "PeriodMonth": "2026-03"
    }

- **Response (Thành công):**
    Chưa có lần chạy mau

---

### 2. POST /api/crd_cardtransactionincrease/export
Xuất file Excel chứa báo cáo giao dịch tăng điểm và chi tiết tiêu điểm.

#### Tham số Request
Body kiểu JSON (`Crd_CardTransactionInCreaseSearchReq`):
- Sử dụng tương tự các tham số lọc của endpoint `search`.

#### Quy tắc xử lý theo thứ tự
1. Thực hiện truy vấn dữ liệu giao dịch tăng điểm và chi tiết tiêu điểm tương tự endpoint `search`.
2. Kiểm tra nếu danh sách tăng điểm rỗng hoặc `null`, ném lỗi và trả về phản hồi Fail với mã `Crd_CardTransactionInCrease_Export` và thông báo `Không có dữ liệu`.
3. Tạo file Excel qua thư viện ClosedXML chứa 2 sheet:
   - Sheet 1: `"Điểm tích"` với 20 cột theo từ điển `DicColNames`.
   - Sheet 2: `"Chi tiết tiêu điểm"` với 23 cột theo từ điển `DicColNamesDetail`.
4. Lưu file Excel xuống thư mục tạm và trả về đường dẫn URL tải file qua thuộc tính `Data = { Url = ... }`.

#### Bảng Mã Lỗi
| Mã lỗi | Điều kiện phát sinh | Chi tiết / Tham số trả về |
| :--- | :--- | :--- |
| `Crd_CardTransactionInCrease_Export` | Danh sách dữ liệu tăng điểm rỗng (`Không có dữ liệu`) hoặc xảy ra ngoại lệ khi xuất file. | `Message` hoặc `Exception` |

#### Ví dụ Request / Response
- **Request:**
    POST /api/crd_cardtransactionincrease/export
    Content-Type: application/json

    {
      "MemberNo": "MEM01"
    }

- **Response (Thành công):**
    Chưa có lần chạy mau

## 3. Khác biệt so với nguồn & GAP
- Các bảng tạm `#tbl_...` của nguồn được chuyển đổi thành các truy vấn LINQ và xử lý phân trang/lọc trong bộ nhớ EF Core (do không có bảng tạm SQLite trực tiếp).
- Tính năng phân quyền xem dữ liệu theo tài khoản đăng nhập (`Sys_User_ForCP_GetAbilityViewOfUser`) được bỏ qua do môi trường port hiện tại chưa có phiên đăng nhập; các trường liên quan đến quyền như `DLCode_DC` mặc định gán giá trị `"ĐL khác"`.

## 4. Nhật ký thay đổi
- Hạng mục `#015`, `#016` — Port 1:1 các endpoint `search` và `export` của `Crd_CardTransactionInCreaseController` sang MiniLoyalty (phiên bản mã `d30cdab`).

TOM TAT
FILE_DA_TAO_SUA: docs/api/Crd_CardTransactionInCreaseController.md
PHAM_VI_PHU: Crd_CardTransactionInCreaseControllerPort (2 endpoint: search, export)
KHONG_XAC_MINH: Ví dụ response thực tế (do môi trường WEB không chạy lệnh/chạy app trực tiếp).
LENH_DA_CHAY: Không chạy lệnh (Theo quy định môi trường WEB: KHÔNG chạy lệnh).
VI_PHAM_QUY_TAC: Khong
