# Tham chiếu API Mst_GovIDTypeController (Port 1-1)

| Thông tin | Giá trị |
|---|---|
| **Phạm vi** | Tài liệu tham chiếu chi tiết 3 endpoint đọc của Mst_GovIDTypeController (#156, #157, #158; Create/Update/Delete đã BỎ) |
| **Cập nhật** | 2026-10-16 |
| **Phiên bản mã** | `79657b7` |
| **Trạng thái** | Nháp — chờ review |

> [!NOTE]
> Toàn bộ các endpoint thuộc controller này áp dụng chuẩn hóa JSON PascalCase tự động qua `PortJsonResultFilter` và xử lý ngoại lệ tập trung qua `PortExceptionFilter` (chuyển đổi ngoại lệ chưa bắt thành HTTP 400 `PortResult.Fail`). Chi tiết xem tại [Kiến trúc Hạ tầng Dùng chung](../kien-truc/HA_TANG_CHUNG.md).

---

## 1. Tóm tắt Nghiệp vụ & Cấu trúc Dữ liệu

### 1.1. Mục đích Nghiệp vụ
API `Mst_GovIDTypeController` quản lý danh mục loại giấy tờ tùy thân (`Mst_GovIDType`) trong hệ thống MiniLoyalty (bao gồm Căn cước công dân, Chứng minh nhân dân, Giấy phép lái xe, Hộ chiếu...). Module cung cấp 3 chức năng đọc: tìm kiếm phân trang theo từ khóa tên loại giấy tờ và trạng thái hoạt động (toán tử `IN`), tra cứu chi tiết 1 loại giấy tờ theo mã, và lấy toàn bộ danh sách các loại giấy tờ đang hoạt động.

> [!IMPORTANT]
> **Quyết định Bỏ các Action Ghi (`Create`, `Update`, `Delete` — #159, #160, #161)**:  
> Tại tầng gate của hệ thống nguồn (`13.ClientGate\V10\idn.Loyalty.WebAPI\Controllers\Mst_GovIDTypeController.cs:237-424`), các action `Create`, `Update`, `Delete` đều ủy nhiệm lời gọi sang dịch vụ bên ngoài `MstSv` (`API_Get(MstSv_Login)`). Trong toàn bộ codebase backend của dự án `2022.7.Loyalty` (`11.BackEnd\...\idn.Loyalty.Biz.Web` và `idn.Loyalty.Biz`), **hoàn toàn không tồn tại** các hàm `WA_Mst_GovIDType_Create`, `WAS_Mst_GovIDType_Create`, hay `Mst_GovIDType_Create` tương ứng, và file hằng số `Error.idN.TVAN.cs` không có mã lỗi `ErridnLoyalty.Mst_GovIDType_Create*`.  
> Tuân thủ nguyên tắc cốt lõi **"Chỉ làm theo tài liệu — Xung đột thì dừng"** (`PORT_RULES_MiniLoyalty.md` mục 6) và bài học kinh nghiệm **"Thiếu nguồn backend = không port"** (`LESSONS_AGENTS.md` B.7), agent đã lập báo cáo xung đột chi tiết tại [`.parity-run/conflicts/159_conflict.md`](../../.parity-run/conflicts/159_conflict.md) và Orchestrator đã chính thức phê duyệt **BỎ** các hạng mục #159, #160, #161.

### 1.2. Bảng Đối chiếu Nguồn Tra cứu (Source Code Mapping)

| Thành phần | Đường dẫn tập tin nguồn (`2022.7.Loyalty`) | Tập tin đích tương ứng (`MiniLoyalty`) |
|---|---|---|
| **Gate Controller** | `13.ClientGate\V10\idn.Loyalty.WebAPI\Controllers\Mst_GovIDTypeController.cs:26-233`, `:435-476` | [`Controllers/Port/Mst_GovIDTypeControllerPort.cs`](../../Controllers/Port/Mst_GovIDTypeControllerPort.cs#L85) |
| **API Backend** | `11.BackEnd\V10\idn.Loyalty.Biz.Web\Controllers\MstGovIDTypeController.cs:21-90` | Tích hợp trực tiếp vào [`Controllers/Port/Mst_GovIDTypeControllerPort.cs`](../../Controllers/Port/Mst_GovIDTypeControllerPort.cs#L85) |
| **Nghiệp vụ (Biz)** | `11.BackEnd\V10\idn.Loyalty.Biz\Master.cs` (Get `:32284-32454`; GetX `:32456-32609`; CheckDB `:31891-31955`) | Tích hợp trực tiếp vào [`Controllers/Port/Mst_GovIDTypeControllerPort.cs`](../../Controllers/Port/Mst_GovIDTypeControllerPort.cs#L178-L284) |
| **Entity Model** | `12.Dev.Common\idn.Loyalty.Common\Models\Mst_GovIDType.cs:9-24` | [`Models/Port/Mst_GovIDType.cs:28-37`](../../Models/Port/Mst_GovIDType.cs#L28-L37) |
| **Cấu hình EF** | Khóa draft `GovIDType` (không có DDL nguồn) | [`Models/Port/Mst_GovIDType.cs:40-48`](../../Models/Port/Mst_GovIDType.cs#L40-L48) |
| **Seeder Demo** | `Order => 40` (danh mục `Mst_*` dùng 10–99) | [`Models/Port/Mst_GovIDType.cs:54-74`](../../Models/Port/Mst_GovIDType.cs#L54-L74) |

### 1.3. Cấu trúc Bảng CSDL (`Mst_GovIDType`)

Bảng dữ liệu vật lý `Mst_GovIDType` gồm đúng 7 cột:

| Tên cột | Kiểu EF Core | Khóa | Cho phép NULL | Ghi chú & Ý nghĩa |
|---|---|---|---|---|
| `GovIDType` | `string` | **PK** | Không | Mã loại giấy tờ tùy thân (ví dụ: `CCCD`, `CMND`, `HC`). Khóa chính (khóa draft). |
| `NetworkID` | `string?` | | Có | Mã mạng nghiệp vụ. Ở đích lấy cấu hình `Biz_NetworkID`. |
| `GovIDTypeName` | `string?` | | Có | Tên loại giấy tờ (Căn cước công dân, Hộ chiếu...). |
| `Remark` | `string?` | | Có | Ghi chú loại giấy tờ. |
| `FlagActive` | `string?` | | Có | Cờ trạng thái hoạt động: `"1"` (Đang hoạt động), `"0"` (Ngừng hoạt động). |
| `LogLUDTimeUTC` | `DateTime?` | | Có | Thời điểm cập nhật cuối cùng theo chuẩn UTC. |
| `LogLUBy` | `string?` | | Có | Người thực hiện cập nhật cuối cùng. |

**Dữ liệu mẫu khởi tạo (Seeder - `Mst_GovIDTypeSeeder`, Idempotent)**:
- Gồm 5 bản ghi demo: `CCCD` ('1'), `CMND` ('1'), `GPLX` ('1'), `GTK` ('0'), `HC` ('1').
- Toàn bộ bản ghi gán `NetworkID = "DEMO"`, `LogLUBy = "seed"`.

---

## 2. Đặc tả Chi tiết Từng Endpoint API

---

### 2.1. Search (#156)

Tìm kiếm danh mục loại giấy tờ phân trang theo từ khóa tên loại giấy tờ và trạng thái hoạt động.

- **Route**: `POST /api/mst_govidtype/search`
- **Controller Action**: [`Mst_GovIDTypeControllerPort.Search`](../../Controllers/Port/Mst_GovIDTypeControllerPort.cs#L97)
- **Tham chiếu nguồn**: Gate `Mst_GovIDTypeController.cs:26-106` → API `MstGovIDTypeController.cs:21-90` → Biz `Master.cs:32456-32609`

#### Bảng tham số Request

Payload JSON gửi qua Request Body (`MstGovIDTypeSearchReq`), hỗ trợ cả trường hợp body rỗng:

| Tên tham số | Kiểu dữ liệu | Bắt buộc | Mặc định | Ý nghĩa & Quy tắc |
|---|---|---|---|---|
| `OrgID` | `string?` | Không | `null` | Nhận tại tham số nhưng hàm `strWhereClause_Mst_GovIDType` (`:435-459`) **không lọc theo OrgID**. |
| `KeyWord` | `string?` | Không | `null` | Từ khóa tìm kiếm theo tên loại giấy tờ (`GovIDTypeName LIKE '%kw%'`, `:440-443`). **Không tìm trên mã `GovIDType`**. |
| `FlagActive` | `string?` | Không | `null` | Danh sách trạng thái phân cách bởi dấu phẩy, **áp dụng toán tử `IN`** (`:448-456`). Nhánh `=` bị comment trong nguồn. |
| `Ft_PageIndex` | `int` | Không | `0` | Chỉ số trang cần lấy (0-based). |
| `Ft_PageSize` | `int` | Không | `10` | Số dòng trên mỗi trang. Nếu `<= 0` trả về mảng rỗng `DataList = []` (`:81-89`). |

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Xác định vị trí bản ghi: `start = (long)pageIndex * pageSize`.
2. Mệnh đề lọc qua `GetXAsync`:
   - **KeyWord**: `EF.Functions.Like((x.GovIDTypeName ?? "").ToLower(), kw, PortQuery.Esc)`.
   - **FlagActive**: Tách chuỗi theo dấu phẩy, hạ chữ thường và lọc qua toán tử `IN`.
3. Draft sắp theo `mgi.GovIDType ASC` (`Master.cs:32516`), đếm tổng `itemCount = draftKeys.Count` (`:32520`).
4. Trích xuất trang: Nếu `pageSize <= 0` trả về danh sách rỗng; nếu `pageSize > 0` cắt trang theo `[start, start + count - 1]`.
5. Thực hiện `INNER JOIN` draft đã cắt trang với bảng `Mst_GovIDType` theo `GovIDType` và sắp xếp theo `t.MyIdxSeq ASC` (`Master.cs:32549-32556`).
6. **Đặc thù dữ liệu trả về**: Khác với các controller khác, dữ liệu trả về của `Mst_GovIDType` **không chứa trường `MyIdxSeq`**, map đúng 7 thuộc tính model nguồn `TModels.Mst_GovIDType` (`Mst_GovIDType.cs:9-24`).
7. Tính `PageCount`: Nếu `rows.Count == 0`, `PageCount = 0`; ngược lại `PageCount = ceil(itemCount / ps)` (`gate :81-89`).

#### Bảng mã lỗi
Endpoint không có mã lỗi nghiệp vụ riêng; ngoại lệ chưa bắt được chuyển thành HTTP 400 qua `PortExceptionFilter`.

#### Ví dụ Request / Response thực tế (từ lần chạy VERIFY #156)
- **Tìm kiếm mặc định (`{}`)**:
  - Request: `POST /api/mst_govidtype/search` kèm `{}`
  - Response (HTTP 200): `ItemCount = 5`, `PageCount = 1`, `DataList` đủ 5 dòng (`CCCD`, `CMND`, `GPLX`, `GTK`, `HC`).
- **Tìm kiếm theo từ khóa `KeyWord = "căn cước"`**:
  - Request: `{"KeyWord": "căn cước", "FlagActive": "1"}`
  - Response: `ItemCount = 1`, `PageCount = 1`, `DataList` chỉ chứa bản ghi `"CCCD"`.
- **Tìm kiếm từ khóa bằng mã `KeyWord = "CCCD"`**:
  - Request: `{"KeyWord": "CCCD"}`
  - Response: `ItemCount = 0`, `PageCount = 0`, `DataList = []` (chứng minh KeyWord chỉ lọc trên tên, không lọc mã).

---

### 2.2. GetByGovIDType (#157)

Tra cứu chi tiết một loại giấy tờ tùy thân theo mã hiệu.

- **Route**: `POST /api/mst_govidtype/getbygovidtype`
- **Controller Action**: [`Mst_GovIDTypeControllerPort.GetByGovIDType`](../../Controllers/Port/Mst_GovIDTypeControllerPort.cs#L124)
- **Tham chiếu nguồn**: Gate `Mst_GovIDTypeController.cs:108-171` → API `MstGovIDTypeController.cs:21-90` → Biz `Master.cs:32456-32609`

#### Bảng tham số Request

Payload JSON gửi qua Request Body (`MstGovIDTypeGetByReq`):

| Tên tham số | Kiểu dữ liệu | Bắt buộc | Mặc định | Ý nghĩa & Quy tắc |
|---|---|---|---|---|
| `GovIDType` | `string?` | Không | `null` | Mã loại giấy tờ tùy thân cần tra cứu (ví dụ: `"CCCD"`, `"GTK"`). |

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. **Kiểm tra mã rỗng tại Gate** (`Mst_GovIDTypeController.cs:128`):
   - Nếu `string.IsNullOrEmpty(req?.GovIDType)`: Không truy vấn CSDL, trả về ngay HTTP 200 kèm `Data = null` (`:157-158`).
2. **Truy vấn CSDL**:
   - Gọi `GetXAsync` với `start = 0`, `count = 1` (`gate :145-146`).
   - Lọc chính xác theo mã: `GovIDType =` (toán tử `=`, `:466-469`), không phân biệt hoa thường.
   - **Không lọc FlagActive**: Bản ghi có `FlagActive = "0"` vẫn được trả về bình thường.
   - Không lọc `OrgID`/`NetworkID`.
3. **Kết quả trả về**:
   - Trả về **MỘT ĐỐI TƯỢNG DUY NHẤT** (không phải mảng, không bọc `PageInfo`) gồm 7 trường model nguồn.
   - Nếu không tìm thấy: Trả về `Data = null`.

#### Ví dụ Request / Response thực tế (từ lần chạy VERIFY #157)
- **Tra cứu thành công mã `"gtk"` (giấy tờ ngừng hoạt động)**:
  - Request: `{"GovIDType": "gtk"}`
  - Response (HTTP 200):
    ```json
    {
      "Success": true,
      "Data": {
        "GovIDType": "GTK",
        "NetworkID": "DEMO",
        "GovIDTypeName": "Giấy tờ khác",
        "Remark": "Seed demo",
        "FlagActive": "0",
        "LogLUDTimeUTC": "2026-10-10T10:00:00Z",
        "LogLUBy": "seed"
      }
    }
    ```
- **Tra cứu mã rỗng `{"GovIDType": ""}`**: Trả về `{"Success": true, "Data": null}`.

---

### 2.3. GetAllActive (#158)

Lấy toàn bộ danh sách các loại giấy tờ tùy thân đang hoạt động (`FlagActive = '1'`).

- **Route**: `POST /api/mst_govidtype/getallactive`
- **Controller Action**: [`Mst_GovIDTypeControllerPort.GetAllActive`](../../Controllers/Port/Mst_GovIDTypeControllerPort.cs#L157)
- **Tham chiếu nguồn**: Gate `Mst_GovIDTypeController.cs:173-233` → API `MstGovIDTypeController.cs:21-90` → Biz `Master.cs:32456-32609`

#### Bảng tham số Request
Endpoint không nhận tham số Body (`gate :173`).

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Lọc `FlagActive = "1"` qua toán tử `IN ('1')` (`gate :193-196, :448-456`), không nhận `KeyWord`, không lọc `OrgID`/`NetworkID`.
2. Tái sử dụng `GetXAsync` với `recordStart = 0`, `recordCount = 123456000` (`gate :208-209`).
3. Sắp xếp theo `GovIDType ASC`.
4. Trả về **mảng phẳng trần** các đối tượng 7 trường (không có `MyIdxSeq`, không bọc `PageInfo`). Khi rỗng trả về `Data = null` (`gate :215-220`).

#### Ví dụ Request / Response thực tế (từ lần chạy VERIFY #158)
- Request: `POST /api/mst_govidtype/getallactive`
- Response (HTTP 200):
  ```json
  {
    "Success": true,
    "Data": [
      { "GovIDType": "CCCD", "NetworkID": "DEMO", "GovIDTypeName": "Căn cước công dân", "Remark": "Seed demo", "FlagActive": "1", "LogLUDTimeUTC": "2026-10-10T10:00:00Z", "LogLUBy": "seed" },
      { "GovIDType": "CMND", "NetworkID": "DEMO", "GovIDTypeName": "Chứng minh nhân dân", "Remark": "Seed demo", "FlagActive": "1", "LogLUDTimeUTC": "2026-10-10T10:00:00Z", "LogLUBy": "seed" },
      { "GovIDType": "GPLX", "NetworkID": "DEMO", "GovIDTypeName": "Giấy phép lái xe", "Remark": "Seed demo", "FlagActive": "1", "LogLUDTimeUTC": "2026-10-10T10:00:00Z", "LogLUBy": "seed" },
      { "GovIDType": "HC", "NetworkID": "DEMO", "GovIDTypeName": "Hộ chiếu", "Remark": "Seed demo", "FlagActive": "1", "LogLUDTimeUTC": "2026-10-10T10:00:00Z", "LogLUBy": "seed" }
    ]
  }
  ```
  *(Bản ghi `GTK` có `FlagActive = '0'` bị loại bỏ).*

---

## 3. Khác biệt so với Nguồn & GAP

1. **Khóa chính & DDL CSDL**: Nguồn không có DDL tạo bảng `Mst_GovIDType`. Khóa chính `GovIDType` là khóa draft, kiểu cột cần xác nhận thêm khi có schema production.
2. **Không trả MyIdxSeq**: Khác với phần lớn các controller khác, dữ liệu trả về của `Mst_GovIDType` map chính xác 7 trường theo model nguồn, không chứa thuộc tính phụ `MyIdxSeq`.
3. **Các Action Ghi Bị Bỏ**: Các chức năng Create/Update/Delete đã bị loại khỏi phạm vi port do phụ thuộc dịch vụ ngoài `MstSv` không có mã backend trong codebase Loyalty (tham chiếu [`.parity-run/conflicts/159_conflict.md`](../../.parity-run/conflicts/159_conflict.md)).
4. **Phân quyền & Log**: Bỏ qua `CheckGatewayAuthentication`, `CheckAuthorize` và ghi log `RequestLog`/`ReturnLog`.

---

## 4. Nhật ký Thay đổi

| Hạng mục (#) | Action / Endpoint | Commit Hash | Ngày hoàn thành | Ghi chú |
|---|---|---|---|---|
| #156 | `Search` | `0cab8a8` | 2026-10-16 | Khởi tạo bảng `Mst_GovIDType`, seeder demo và endpoint tìm kiếm phân trang |
| #157 | `GetByGovIDType` | `e05732a` | 2026-10-16 | Endpoint tra cứu chi tiết loại giấy tờ theo mã, trả về 1 đối tượng |
| #158 | `GetAllActive` | `fbe0e15` | 2026-10-16 | Endpoint lấy danh sách loại giấy tờ đang hoạt động (mảng trần) |
| #159–#161 | `Create, Update, Delete` | *Đã bỏ* | 2026-10-16 | Đã BỎ do thiếu backend source; xem `.parity-run/conflicts/159_conflict.md` |
