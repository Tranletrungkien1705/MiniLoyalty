# Phạm vi: Map_QueryDealer_MemberControllerPort (Tra cứu hội viên theo đại lý)
# Cập nhật: 2026-10-16
# Phiên bản mã: 11bf46e
# Trạng thái: Nháp — chờ review

## 1. Tóm tắt
Mục đích nghiệp vụ của controller này là quản lý và tra cứu thông tin lượt truy vấn hội viên của các đại lý (`Map_QueryDealer_Member`), kết hợp dữ liệu liên kết với thẻ, hội viên, đại lý và tỉnh thành. Hỗ trợ tìm kiếm thông thường, báo cáo thống kê theo điều kiện thời gian truy vấn, và xuất dữ liệu báo cáo ra file Excel.
- **Bảng nguồn (File gate / API / Biz)**: `Map_QueryDealer_MemberController.cs` (Controllers/Port/Map_QueryDealer_MemberControllerPort.cs)
- **Bảng CSDL (EF Core)**: `Map_QueryDealer_Member`, `Crd_Member`, `Crd_Card`, `Mst_DealerCP`, `Mst_Province`

### Bảng cột thực thể (`Map_QueryDealer_Member`):
| Tên cột | Kiểu dữ liệu EF | Khóa / Khác | Ghi chú |
| --- | --- | --- | --- |
| DLCPCode | `string` | Khóa chính (PK) | Mã đại lý truy vấn |
| MemberNo | `string` | Khóa chính (PK) | Mã hội viên |
| NetworkID | `string` | - | Mã mạng |
| QueryDate | `DateTime` | - | Ngày truy vấn |
| Remark | `string?` | - | Ghi chú |
| FlagActive | `string?` | - | Trạng thái |
| LogLUDTimeUTC | `DateTime?` | - | Thời gian cập nhật log (UTC) |
| LogLUBy | `string?` | - | Người cập nhật log |

---

## 2. Các Endpoint

### 2.1. POST /api/map_querydealer_member/search
- **Mô tả**: Tìm kiếm thông tin lượt truy vấn hội viên theo đại lý (phục vụ chức năng Search thông thường).
- **Request Body (`MapQueryDealerMemberSearchReq`)**:
  | Tên tham số | Kiểu dữ liệu | Bắt buộc? | Mặc định | Ý nghĩa |
  | --- | --- | --- | --- | --- |
  | EffDateStartFrom | `string?` | Không | `null` | Ngày hiệu lực từ (nhận nhưng không lọc) |
  | EffDateStartTo | `string?` | Không | `null` | Ngày hiệu lực đến (nhận nhưng không lọc) |
  | CardNo | `string?` | Không | `null` | Lọc theo số thẻ |
  | IDCardNo | `string?` | Không | `null` | Lọc theo số CMT/CCCD |
  | MemberNo | `string?` | Không | `null` | Lọc theo mã hội viên |
  | PhoneNo | `string?` | Không | `null` | Lọc theo số điện thoại |
  | DLCPCode | `string?` | Không | `null` | Mã đại lý (phân tách bởi dấu phẩy) |
  | PointRefFrom | `string?` | Không | `null` | Tham chiếu điểm từ |
  | PointRefTo | `string?` | Không | `null` | Tham chiếu điểm đến |
  | FlagPoint | `string?` | Không | `null` | Cờ điểm |
  | CarNo | `string?` | Không | `null` | Biển số xe |
  | VIN | `string?` | Không | `null` | Số khung |

- **Quy tắc xử lý**:
  1. Xây dựng truy vấn dự thảo (`Draft`) dựa trên các điều kiện lọc kết hợp giữa bảng `Map_QueryDealer_Member`, `Crd_Member`, và `Crd_Card`.
  2. Sắp xếp kết quả giảm dần theo `LogLUDTimeUTC` và lấy tối đa giới hạn bản ghi.
  3. Join lấy thêm thông tin mở rộng từ đại lý (`Mst_DealerCP`) và hội viên.
  4. Trả về cấu trúc JSON chứa `DataList` (nếu rỗng trả về `null`).
- **Mã lỗi (`ErridnLoyalty.*`)**: Chưa xác minh.
- **Ví dụ Request**:
    ```json
    {
      "MemberNo": "HV0000001",
      "DLCPCode": "DL01"
    }
    ```
- **Ví dụ Response**: Chưa có lần chạy mau

---

### 2.2. POST /api/map_querydealer_member/searchforreport
- **Mô tả**: Báo cáo truy vấn hội viên theo đại lý, lọc theo khoảng thời gian truy vấn và các thông tin chi tiết.
- **Request Body (`MapQueryDealerMemberSearchForReportReq`)**:
  | Tên tham số | Kiểu dữ liệu | Bắt buộc? | Mặc định | Ý nghĩa |
  | --- | --- | --- | --- | --- |
  | QueryDateFrom | `string?` | Không | `null` | Ngày truy vấn từ (`>=`) |
  | QueryDateTo | `string?` | Không | `null` | Ngày truy vấn đến (`<=`) |
  | MemberNo | `string?` | Không | `null` | Mã hội viên |
  | PhoneNo | `string?` | Không | `null` | Số điện thoại |
  | DLCPCode | `string?` | Không | `null` | Mã đại lý (phân tách dấu phẩy) |
  | CarNo | `string?` | Không | `null` | Biển số xe |
  | VIN | `string?` | Không | `null` | Số khung |

- **Quy tắc xử lý**:
  1. Parse `QueryDateFrom` và `QueryDateTo` theo `InvariantCulture`. Sai định dạng trả về lỗi 400 `ErridnLoyalty.Rpt_Map_QueryDealer_Member`.
  2. Lọc danh sách draft theo các điều kiện kết hợp.
  3. Áp dụng điều kiện lọc quan trọng: Chỉ giữ lại các lượt truy vấn mà đại lý truy vấn khác với đại lý mở thẻ của hội viên (`mqdm.DLCPCode <> cm.DLCodeRegis`).
  4. Trả về danh sách (`Lst_Map_QueryDealer_Member`), danh sách thống kê số lượng hội viên (`Lst_Rpt_Map_QueryDealer_Member`), và tổng số dòng (`CountRow`).
- **Mã lỗi**:
  - `Rpt_Map_QueryDealer_Member`: Lỗi định dạng ngày tháng `QueryDate`.
- **Ví dụ Request**:
    ```json
    {
      "QueryDateFrom": "2026-10-01",
      "QueryDateTo": "2026-10-31",
      "DLCPCode": "DL01"
    }
    ```
- **Ví dụ Response**: Chưa có lần chạy mau

---

### 2.3. POST /api/map_querydealer_member/exportexcel
- **Mô tả**: Xuất báo cáo truy vấn hội viên theo đại lý ra file Excel.
- **Request Body (`MapQueryDealerMemberExportExcelReq`)**:
  | Tên tham số | Kiểu dữ liệu | Bắt buộc? | Mặc định | Ý nghĩa |
  | --- | --- | --- | --- | --- |
  | QueryDateFrom | `string?` | Không | `null` | Ngày truy vấn từ |
  | QueryDateTo | `string?` | Không | `null` | Ngày truy vấn đến |
  | MemberNo | `string?` | Không | `null` | Mã hội viên |
  | PhoneNo | `string?` | Không | `null` | Số điện thoại |
  | DLCPCode | `string?` | Không | `null` | Mã đại lý |
  | CarNo | `string?` | Không | `null` | Biển số xe |
  | VIN | `string?` | Không | `null` | Số khung |

- **Quy tắc xử lý**:
  1. Thực hiện lấy dữ liệu báo cáo tương tự endpoint `searchforreport`.
  2. Kiểm tra nếu danh sách kết quả rỗng, trả về lỗi `Không có dữ liệu` (`Map_QueryDealer_Member_ExportExcel`).
  3. Tạo file Excel sử dụng từ điển 11 cột chuẩn (`GetImportDicColums`) và trả về đường dẫn tải file (`Url`).
- **Mã lỗi**:
  - `Map_QueryDealer_Member_ExportExcel`: Không có dữ liệu để xuất Excel hoặc lỗi định dạng ngày.
- **Ví dụ Request**:
    ```json
    {
      "QueryDateFrom": "2026-10-01",
      "QueryDateTo": "2026-10-31"
    }
    ```
- **Ví dụ Response**: Chưa có lần chạy mau

---

## 3. Khác biệt so với nguồn & GAP
- **Phân quyền dữ liệu (Ability)**: Hệ thống nguồn lấy thông tin đại lý theo người dùng đăng nhập (nếu khác "HTV" thì lọc theo đại lý của user). Tại hệ đích hiện chưa có cấu trúc session user, tạm thời bỏ qua bước lọc theo user (`coi như HTV`). Đây là điểm GAP cần lưu ý khi tích hợp phân quyền tài khoản hệ thống.
- **So sánh mã đại lý**: Điều kiện `mqdm.DLCPCode <> cm.DLCodeRegis` loại bỏ các lượt tự tra cứu của chính đại lý mở thẻ; nếu `DLCodeRegis` là `null` thì dòng bị loại theo logic SQL tiêu chuẩn.

---

## 4. Nhật ký thay đổi
- `#66`, `#67`, `#69`: Port 1-1 các chức năng tìm kiếm thông thường (`Search`), báo cáo thống kê (`SearchForRePort`) và xuất Excel (`ExportExcel`) từ mã nguồn gốc sang MiniLoyalty (`Map_QueryDealer_MemberControllerPort.cs`).
