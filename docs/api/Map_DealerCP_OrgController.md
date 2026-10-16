# Tham chiếu API Map_DealerCP_OrgController (Port 1-1)

| Thông tin | Giá trị |
|---|---|
| **Phạm vi** | Tài liệu tham chiếu chi tiết 1 endpoint đã port 1-1 của Map_DealerCP_OrgController (#65) |
| **Cập nhật** | 2026-10-16 |
| **Phiên bản mã** | `ed28f8f` |
| **Trạng thái** | Nháp — chờ review |

> [!NOTE]
> Toàn bộ các endpoint thuộc controller này áp dụng chuẩn hóa JSON PascalCase tự động qua `PortJsonResultFilter` và xử lý ngoại lệ tập trung qua `PortExceptionFilter` (chuyển đổi ngoại lệ chưa bắt thành HTTP 400 `PortResult.Fail`). Chi tiết xem tại [Kiến trúc Hạ tầng Dùng chung](../kien-truc/HA_TANG_CHUNG.md).

---

## 1. Tóm tắt Nghiệp vụ & Cấu trúc Dữ liệu

### 1.1. Mục đích Nghiệp vụ
API `Map_DealerCP_OrgController` quản lý quan hệ liên kết phân cấp và phân quyền giữa đại lý phân phối (`Mst_DealerCP`) và tổ chức chi nhánh trực thuộc (`Mst_Org`) trong hệ thống MiniLoyalty. Module cung cấp endpoint tra cứu thông tin liên kết tổ chức của đại lý theo mã đại lý (`DLCPCode`), đồng thời kết nối thông tin mã số thuế từ bảng người nộp thuế (`Mst_NNT`).

### 1.2. Bảng Đối chiếu Nguồn Tra cứu (Source Code Mapping)

| Thành phần | Đường dẫn tập tin nguồn (`2022.7.Loyalty`) | Tập tin đích tương ứng (`MiniLoyalty`) |
|---|---|---|
| **Gate Controller** | `13.ClientGate\V10\idn.Loyalty.WebAPI\Controllers\Map_DealerCP_OrgController.cs:26-111` | [`Controllers/Port/Map_DealerCP_OrgControllerPort.cs`](../../Controllers/Port/Map_DealerCP_OrgControllerPort.cs) |
| **API Backend** | `11.BackEnd\V10\idn.Loyalty.Biz.Web\Controllers\MapDealerCPOrgController.cs:92-161` | Tích hợp trực tiếp vào [`Controllers/Port/Map_DealerCP_OrgControllerPort.cs`](../../Controllers/Port/Map_DealerCP_OrgControllerPort.cs) |
| **Nghiệp vụ (Biz)** | `11.BackEnd\V10\idn.Loyalty.Biz\Master.Loyalty.cs` (GetX `:5086-5260`; Get `:4932-5085`; WAS `:4832-4931`) | Tích hợp trực tiếp vào [`Controllers/Port/Map_DealerCP_OrgControllerPort.cs`](../../Controllers/Port/Map_DealerCP_OrgControllerPort.cs) |
| **Entity Model** | `12.Dev.Common\idn.Loyalty.Common\Models\Loyalty\Map_DealerCP_Org.cs:9-18` | [`Models/Port/Mst_DealerCP.cs:66-84`](../../Models/Port/Mst_DealerCP.cs#L66-L84) |
| **Cấu hình EF** | Khóa ghép `(DLCPCode, OrgID)` | [`Models/Port/Mst_DealerCP.cs:98-106`](../../Models/Port/Mst_DealerCP.cs#L98-L106) |
| **Seeder Demo** | `Mst_DealerCPSeeder` (khởi tạo liên kết đại lý - tổ chức) | [`Models/Port/Mst_DealerCP.cs:198-233`](../../Models/Port/Mst_DealerCP.cs#L198-L233) |

### 1.3. Cấu trúc Bảng CSDL (`Map_DealerCP_Org`)

Bảng dữ liệu vật lý `Map_DealerCP_Org` gồm đúng 6 cột:

| Tên cột | Kiểu EF Core | Khóa | Cho phép NULL | Ghi chú & Ý nghĩa |
|---|---|---|---|---|
| `DLCPCode` | `string` | **PK1** | Không | Mã đại lý phân phối (`Mst_DealerCP`). Khóa chính phần 1. |
| `OrgID` | `string` | **PK2** | Không | Mã tổ chức chi nhánh (`Mst_Org`). Khóa chính phần 2. |
| `NetworkID` | `string?` | | Có | Mã mạng nghiệp vụ. Ở đích gán theo cấu hình hoặc seed (`"1001"`). |
| `FlagActive` | `string?` | | Có | Cờ trạng thái hoạt động: `"1"` (Kích hoạt), `"0"` (Ngừng). |
| `LogLUDTimeUTC` | `DateTime?` | | Có | Thời điểm cập nhật cuối cùng theo chuẩn UTC. |
| `LogLUBy` | `string?` | | Có | Người thực hiện cập nhật cuối cùng. |

**Dữ liệu mẫu khởi tạo (Seeder - `Mst_DealerCPSeeder`, Idempotent)**:
- Gồm 3 bản ghi liên kết demo:
  - `DL01` liên kết với tổ chức `4020320001` (FlagActive `'1'`).
  - `DL02` liên kết với tổ chức `4020320001` (FlagActive `'1'`).
  - `DL03` liên kết với tổ chức `4020320002` (FlagActive `'1'`).
- Toàn bộ bản ghi gán `NetworkID = "1001"`, `LogLUBy = "seed"`.

---

## 2. Đặc tả Chi tiết Từng Endpoint API

---

### 2.1. GetByDLCPCode (#65)

Tra cứu thông tin liên kết tổ chức và mã số thuế chi nhánh của đại lý theo `DLCPCode`.

- **Route**: `POST /api/map_dealercp_org/getbydlcpcode`
- **Controller Action**: [`Map_DealerCP_OrgControllerPort.GetByDLCPCode`](../../Controllers/Port/Map_DealerCP_OrgControllerPort.cs#L92)
- **Tham chiếu nguồn**: Gate `Map_DealerCP_OrgController.cs:26-91` → API `MapDealerCPOrgController.cs:92-161` → Biz `Master.Loyalty.cs:5086-5260`

#### Bảng tham số Request

Payload JSON gửi qua Request Body (`MapDealerCPOrgGetByDLCPCodeReq`), hỗ trợ cả trường hợp body rỗng:

| Tên tham số | Kiểu dữ liệu | Bắt buộc | Mặc định | Ý nghĩa & Quy tắc |
|---|---|---|---|---|
| `DLCPCode` | `string?` | Có | `null` | Mã đại lý phân phối cần tra cứu (`gate :26, :47, :51`). |

#### Quy tắc xử lý theo thứ tự (Nghiệp vụ 2 bước của nguồn)
1. **Kiểm tra mã đại lý rỗng**: Nếu `DLCPCode` rỗng, null hoặc chỉ chứa khoảng trắng → không truy vấn CSDL (`gate :47`), trả về ngay HTTP 200 với `Success = true, Data = null` (`gate :78`).
2. **Nghiệp vụ 2 bước trong `Master.Loyalty.cs:5086-5260`**:
   - **Bước 1 - DRAFT (`#tbl_Map_DealerCP_Org_Filter_Draft`)**:
     - Lọc `mdcpo.DLCPCode == dlcpCode` kết hợp inner join với `Mst_DealerCP mdcp`.
     - Dòng lọc `FlagActive` bị comment trong nguồn (`gate :105-108`) → Không lọc theo `FlagActive`.
     - Sắp xếp theo `mdcpo.LogLUDTimeUTC DESC`. Cắt trang 1 dòng (`Ft_RecordStart = 0`, `Ft_RecordCount = 1`).
   - **Bước 2 - SELECT CUỐI**:
     - Inner join bảng draft với `Map_DealerCP_Org`, `Mst_Org`, và `Mst_NNT` trên `OrgID` (`mo.OrgID == mnnt.OrgID`).
     - **Lưu ý nghiệp vụ**: Nếu tổ chức liên kết không có bản ghi tương ứng trong bảng người nộp thuế `Mst_NNT`, dòng dữ liệu sẽ bị loại bỏ do điều kiện inner join này.
     - Cột trả về bổ sung: `mnnt_MST` (mã số thuế lấy từ bảng `Mst_NNT`).
3. Gán kết quả: Trả về phần tử đầu tiên `rows[0]` hoặc `null` nếu không có dữ liệu thỏa mãn.

#### Bảng mã lỗi
Endpoint luôn trả về HTTP 200.

#### Ví dụ Request / Response thực tế (từ lần chạy VERIFY #65)
- **Tra cứu đại lý có liên kết tổ chức (`DL01`)**:
  - Request: `POST /api/map_dealercp_org/getbydlcpcode` kèm `{"DLCPCode": "DL01"}`
  - Response (HTTP 200):
    ```json
    {
      "Success": true,
      "Data": {
        "MyIdxSeq": 0,
        "DLCPCode": "DL01",
        "OrgID": "4020320001",
        "NetworkID": "1001",
        "FlagActive": "1",
        "LogLUDTimeUTC": "2026-01-01T00:00:00",
        "LogLUBy": "seed",
        "mnnt_MST": "0100000001"
      }
    }
    ```
- **Tra cứu đại lý không tồn tại hoặc rỗng**:
  - Request: `POST /api/map_dealercp_org/getbydlcpcode` kèm `{"DLCPCode": "KHONG_CO"}` hoặc `{}`
  - Response (HTTP 200):
    ```json
    {
      "Success": true,
      "Data": null
    }
    ```

---

## 3. Khác biệt so với Nguồn & GAP

1. **Ràng buộc Inner Join với Mst_NNT**: Theo đúng SQL nguồn, các liên kết giữa đại lý và tổ chức chỉ hiển thị khi tổ chức đó đã được khai báo mã số thuế trong bảng `Mst_NNT`. MiniLoyalty bảo toàn hoàn toàn logic này.
2. **Hòa trùng khi sắp xếp**: Trường hợp các liên kết có cùng mốc `LogLUDTimeUTC`, hệ thống sắp phụ theo `OrgID ASC` để đảm bảo kết quả nhất quán.
3. **Phân quyền & Tài khoản hệ thống**: Nguồn gọi API backend bằng tài khoản hệ thống viết cứng `WAUserCode_BG`/`WAUserPassword_BG`. Đích không chép thông tin đăng nhập và bỏ qua kiểm tra gateway authentication.

---

## 4. Nhật ký Thay đổi

| Hạng mục (#) | Action / Endpoint | Commit Hash | Ngày hoàn thành | Ghi chú |
|---|---|---|---|---|
| #65 | `GetByDLCPCode` | `b6bc9a4` | 2026-10-16 | Khởi tạo bảng `Map_DealerCP_Org` và endpoint tra cứu liên kết đại lý - tổ chức kèm mã số thuế |
