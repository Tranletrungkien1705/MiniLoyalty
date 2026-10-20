# Tham chiếu API Mst_FamilyStatusController (Port 1-1)

| Thông tin | Giá trị |
|---|---|
| **Phạm vi** | Tài liệu tham chiếu chi tiết 2 endpoint đã port 1-1 của Mst_FamilyStatusController (#152, #153) |
| **Cập nhật** | 2026-10-16 |
| **Phiên bản mã** | `79657b7` |
| **Trạng thái** | Nháp — chờ review |

> [!NOTE]
> Toàn bộ các endpoint thuộc controller này áp dụng chuẩn hóa JSON PascalCase tự động qua `PortJsonResultFilter` và xử lý ngoại lệ tập trung qua `PortExceptionFilter` (chuyển đổi ngoại lệ chưa bắt thành HTTP 400 `PortResult.Fail`). Chi tiết xem tại [Kiến trúc Hạ tầng Dùng chung](../kien-truc/HA_TANG_CHUNG.md).

---

## 1. Tóm tắt Nghiệp vụ & Cấu trúc Dữ liệu

### 1.1. Mục đích Nghiệp vụ
API `Mst_FamilyStatusController` quản lý danh mục tình trạng gia đình (`Mst_FamilyStatus`) trong hệ thống MiniLoyalty (ví dụ: Độc thân, Đã kết hôn, Ly hôn). Module này phục vụ cấu hình hồ sơ lý lịch nhân sự và khách hàng hội viên, cung cấp chức năng tìm kiếm phân trang theo từ khóa tên tình trạng gia đình và trạng thái hoạt động, cùng endpoint lấy toàn bộ danh mục đang hoạt động.

### 1.2. Bảng Đối chiếu Nguồn Tra cứu (Source Code Mapping)

| Thành phần | Đường dẫn tập tin nguồn (`2022.7.Loyalty`) | Tập tin đích tương ứng (`MiniLoyalty`) |
|---|---|---|
| **Gate Controller** | `13.ClientGate\V10\idn.Loyalty.WebAPI\Controllers\Mst_FamilyStatusController.cs:25-199` | [`Controllers/Port/Mst_FamilyStatusControllerPort.cs`](../../Controllers/Port/Mst_FamilyStatusControllerPort.cs#L64) |
| **API Backend** | `11.BackEnd\V10\idn.Loyalty.Biz.Web\Controllers\MstFamilyStatusController.cs:17-88` | Tích hợp trực tiếp vào [`Controllers/Port/Mst_FamilyStatusControllerPort.cs`](../../Controllers/Port/Mst_FamilyStatusControllerPort.cs#L64) |
| **Nghiệp vụ (Biz)** | `11.BackEnd\V10\idn.Loyalty.Biz\MasterData.cs` (GetX `:42415-42570`; Get `:42572-42700`; WAS `:42702-42792`) | Tích hợp trực tiếp vào [`Controllers/Port/Mst_FamilyStatusControllerPort.cs`](../../Controllers/Port/Mst_FamilyStatusControllerPort.cs#L122-L169) |
| **Entity Model** | `12.Dev.Common\idn.Loyalty.Common\Models\HRM\Mst_FamilyStatus.cs:9-17` | [`Models/Port/Mst_FamilyStatus.cs:29-38`](../../Models/Port/Mst_FamilyStatus.cs#L29-L38) |
| **Cấu hình EF** | Khóa draft `FamilyStatusCode` (không có DDL nguồn) | [`Models/Port/Mst_FamilyStatus.cs:41-49`](../../Models/Port/Mst_FamilyStatus.cs#L41-L49) |
| **Seeder Demo** | `Order => 39` (danh mục `Mst_*` dùng 10–99) | [`Models/Port/Mst_FamilyStatus.cs:57-73`](../../Models/Port/Mst_FamilyStatus.cs#L57-L73) |

### 1.3. Cấu trúc Bảng CSDL (`Mst_FamilyStatus`)

Bảng dữ liệu vật lý `Mst_FamilyStatus` gồm đúng 7 cột:

| Tên cột | Kiểu EF Core | Khóa | Cho phép NULL | Ghi chú & Ý nghĩa |
|---|---|---|---|---|
| `FamilyStatusCode` | `string` | **PK** | Không | Mã tình trạng gia đình (ví dụ: `FS01`, `FS02`, `FS03`). Khóa chính (khóa draft). |
| `NetworkID` | `string?` | | Có | Mã mạng nghiệp vụ. Ở đích lấy từ cấu hình `Biz_NetworkID`. |
| `FamilyStatusName` | `string?` | | Có | Tên tình trạng gia đình (ví dụ: `Độc thân`, `Đã kết hôn`). |
| `FamilyStatusDesc` | `string?` | | Có | Mô tả tình trạng gia đình. |
| `FlagActive` | `string?` | | Có | Cờ trạng thái hoạt động: `"1"` (Đang hoạt động), `"0"` (Ngừng hoạt động). |
| `LogLUDTimeUTC` | `DateTime?` | | Có | Thời điểm cập nhật cuối cùng theo chuẩn UTC. |
| `LogLUBy` | `string?` | | Có | Người thực hiện cập nhật cuối cùng. |

**Dữ liệu mẫu khởi tạo (Seeder - `Mst_FamilyStatusSeeder`, Idempotent)**:
- Gồm 3 bản ghi demo: `FS01` (Độc thân, FlagActive `'1'`, Desc `'Chưa kết hôn'`), `FS02` (Đã kết hôn, FlagActive `'1'`, Desc `'Có vợ/chồng'`), `FS03` (Ly hôn, FlagActive `'0'`, Desc `null`).
- Toàn bộ bản ghi gán `NetworkID = "DEMO"`, `LogLUBy = "seed"`.

---

## 2. Đặc tả Chi tiết Từng Endpoint API

---

### 2.1. Search (#152)

Tìm kiếm danh mục tình trạng gia đình phân trang theo từ khóa và trạng thái hoạt động.

- **Route**: `POST /api/mst_familystatus/search`
- **Controller Action**: [`Mst_FamilyStatusControllerPort.Search`](../../Controllers/Port/Mst_FamilyStatusControllerPort.cs#L73)
- **Tham chiếu nguồn**: Gate `Mst_FamilyStatusController.cs:25-105` → API `MstFamilyStatusController.cs:17-88` → Biz `MasterData.cs:42415-42570`

#### Bảng tham số Request

Payload JSON gửi qua Request Body (`MstFamilyStatusSearchReq`), hỗ trợ cả trường hợp body rỗng:

| Tên tham số | Kiểu dữ liệu | Bắt buộc | Mặc định | Ý nghĩa & Quy tắc |
|---|---|---|---|---|
| `OrgID` | `string?` | Không | `null` | Nhận tại tham số (`gate :25, :55`) nhưng hàm `strWhereClause_Mst_FamilyStatus` (`:171-186`) không dùng `OrgID` để lọc. |
| `KeyWord` | `string?` | Không | `null` | Từ khóa tìm kiếm theo tên tình trạng gia đình (`FamilyStatusName LIKE '%kw%'`, `gate :178`). |
| `FlagActive` | `string?` | Không | `null` | Cờ trạng thái hoạt động, toán tử `=` (`gate :182`, `:195`) — **KHÔNG IN**. Không tách dấu phẩy; nếu truyền `"0,1"` sẽ so sánh chuỗi bằng `"0,1"` và trả về 0 dòng. |
| `Ft_PageIndex` | `int` | Không | `0` | Chỉ số trang cần lấy (0-based). |
| `Ft_PageSize` | `int` | Không | `10` | Số dòng trên mỗi trang. Nếu `<= 0` trả về mảng rỗng `DataList = []` (`gate :83-90`). |

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Xác định vị trí bản ghi: `start = (long)pageIndex * pageSize`.
2. Mệnh đề lọc qua `GetXAsync`:
   - **KeyWord**: `EF.Functions.Like((x.FamilyStatusName ?? "").ToLower(), PortQuery.Contains(keyWord), PortQuery.Esc)`.
   - **FlagActive**: Toán tử `=` qua `EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal(flagActive), PortQuery.Esc)`.
3. Sắp xếp thứ tự bản ghi: Theo `mfs.FamilyStatusCode ASC` (`MasterData.cs:42477`), select cuối sắp theo `t.MyIdxSeq ASC` (`MasterData.cs:42516`).
4. Đếm tổng số bản ghi: `itemCount = await ordered.CountAsync()` (tương ứng `MyCount` trong SQL nguồn `:42481`).
5. Trích xuất trang: Nếu `pageSize <= 0` hoặc `recordStart < 0` trả về danh sách rỗng; nếu `pageSize > 0` gọi `.Skip((int)start).Take((int)pageSize)`.
6. Gán `MyIdxSeq = start + i` (0-based identity).
7. Tính `PageCount`: Nếu `rows.Count == 0`, `PageCount = 0`; ngược lại `PageCount = ceil(itemCount / ps)` (với `ps = pageSize == 0 ? 10 : pageSize`).

#### Bảng mã lỗi
Endpoint không có mã lỗi nghiệp vụ riêng; ngoại lệ chưa bắt được chuyển thành HTTP 400 qua `PortExceptionFilter`.

#### Ví dụ Request / Response thực tế (từ lần chạy VERIFY #152)
- **Tìm kiếm mặc định (`{}`)**:
  - Request: `POST /api/mst_familystatus/search` kèm `{}`
  - Response (HTTP 200):
    ```json
    {
      "Success": true,
      "Data": {
        "DataList": [
          { "MyIdxSeq": 0, "FamilyStatusCode": "FS01", "NetworkID": "DEMO", "FamilyStatusName": "Độc thân", "FamilyStatusDesc": "Chưa kết hôn", "FlagActive": "1", "LogLUDTimeUTC": null, "LogLUBy": "seed" },
          { "MyIdxSeq": 1, "FamilyStatusCode": "FS02", "NetworkID": "DEMO", "FamilyStatusName": "Đã kết hôn", "FamilyStatusDesc": "Có vợ/chồng", "FlagActive": "1", "LogLUDTimeUTC": null, "LogLUBy": "seed" },
          { "MyIdxSeq": 2, "FamilyStatusCode": "FS03", "NetworkID": "DEMO", "FamilyStatusName": "Ly hôn", "FamilyStatusDesc": null, "FlagActive": "0", "LogLUDTimeUTC": null, "LogLUBy": "seed" }
        ],
        "ItemCount": 3,
        "PageCount": 1,
        "PageIndex": 0,
        "PageSize": 10
      }
    }
    ```

---

### 2.2. GetAllActive (#153)

Lấy toàn bộ danh sách các tình trạng gia đình đang hoạt động (`FlagActive = '1'`).

- **Route**: `POST /api/mst_familystatus/getallactive`
- **Controller Action**: [`Mst_FamilyStatusControllerPort.GetAllActive`](../../Controllers/Port/Mst_FamilyStatusControllerPort.cs#L105)
- **Tham chiếu nguồn**: Gate `Mst_FamilyStatusController.cs:107-167` → API `MstFamilyStatusController.cs:17-88` → Biz `MasterData.cs:42415-42792`

#### Bảng tham số Request
Endpoint không nhận tham số Body (`gate :107`).

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Tái sử dụng `GetXAsync` với tham số cố định: `FlagActive = "1"` (toán tử `=`, `gate :127-130, :195`), `recordStart = 0`, `recordCount = 123456000` (`gate :142-143`, quy ước port).
2. Không nhận `KeyWord`, không lọc theo `OrgID` (hàm lọc `strWhereClause_Mst_FamilyStatus_Active` `:188-199` không dùng OrgID).
3. Nguồn chạy trong Transaction nhưng luôn `Rollback` (:42610, :42663) vì chỉ đọc dữ liệu; phía port dùng truy vấn `AsNoTracking()` không ghi dữ liệu.
4. Trả về mảng phẳng trần các thực thể kèm `MyIdxSeq`. Nếu không có dữ liệu trả về `Data = null` (nguồn gate `:126, 149-154`).

#### Ví dụ Request / Response thực tế (từ lần chạy VERIFY #153)
- Request: `POST /api/mst_familystatus/getallactive`
- Response (HTTP 200):
  ```json
  {
    "Success": true,
    "Data": [
      { "MyIdxSeq": 0, "FamilyStatusCode": "FS01", "NetworkID": "DEMO", "FamilyStatusName": "Độc thân", "FamilyStatusDesc": "Chưa kết hôn", "FlagActive": "1", "LogLUDTimeUTC": null, "LogLUBy": "seed" },
      { "MyIdxSeq": 1, "FamilyStatusCode": "FS02", "NetworkID": "DEMO", "FamilyStatusName": "Đã kết hôn", "FamilyStatusDesc": "Có vợ/chồng", "FlagActive": "1", "LogLUDTimeUTC": null, "LogLUBy": "seed" }
    ]
  }
  ```

---

## 3. Khác biệt so với Nguồn & GAP

1. **Khóa chính & DDL CSDL**: Nguồn không có DDL tạo bảng `Mst_FamilyStatus`. Khóa chính `FamilyStatusCode` là khóa draft, kiểu cột cần xác nhận thêm khi có schema production.
2. **Toán tử lọc FlagActive**: Sử dụng toán tử `=` (`strWhereClause_Mst_FamilyStatus` `:182`, `strWhereClause_Mst_FamilyStatus_Active` `:195`), không hỗ trợ toán tử `IN` dạng chuỗi phân cách bởi dấu phẩy.
3. **Hình dạng dòng GetAllActive**: Trả về kèm `MyIdxSeq` (0-based) tương tự cấu trúc `Search` do dùng chung `GetXAsync`.
4. **Phân quyền & Log**: Bỏ qua `CheckGatewayAuthentication`, `CheckAuthorize` và ghi log `RequestLog`/`ReturnLog`.

---

## 4. Nhật ký Thay đổi

| Hạng mục (#) | Action / Endpoint | Commit Hash | Ngày hoàn thành | Ghi chú |
|---|---|---|---|---|
| #152 | `Search` | `b759701` | 2026-10-16 | Khởi tạo bảng `Mst_FamilyStatus`, seeder demo và endpoint tìm kiếm phân trang |
| #153 | `GetAllActive` | `b22f364` | 2026-10-16 | Endpoint lấy danh sách tình trạng gia đình đang hoạt động (mảng trần) |
