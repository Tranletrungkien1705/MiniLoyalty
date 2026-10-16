# Tham chiếu API Mst_CarBrandController (Port 1-1)

| Thông tin | Giá trị |
|---|---|
| **Phạm vi** | Tài liệu tham chiếu chi tiết 10 endpoint đã port 1-1 của Mst_CarBrandController (#77 - #86) |
| **Cập nhật** | 2026-10-16 |
| **Phiên bản mã** | `93b6ace` |
| **Trạng thái** | Nháp — chờ review |

---

## 1. Tóm tắt Nghiệp vụ & Cấu trúc Dữ liệu

### 1.1. Mục đích Nghiệp vụ
API `Mst_CarBrandController` quản lý danh mục các hãng xe ô tô (`Mst_CarBrand`) trong hệ thống MiniLoyalty. Module cung cấp các chức năng tìm kiếm phân trang theo từ khóa và trạng thái hoạt động, lấy danh sách các hãng xe đang hoạt động, tra cứu chi tiết hãng xe theo mã, thêm mới (chuẩn hóa viết hoa tên hãng theo nguồn), cập nhật thông tin/ghi chú/trạng thái, xóa đơn lẻ và xóa hàng loạt (hỗ trợ xóa một phần), nhập dữ liệu danh mục từ Excel và xuất file danh sách/file mẫu chuẩn.

### 1.2. Bảng Đối chiếu Nguồn Tra cứu (Source Code Mapping)

| Thành phần | Đường dẫn tập tin nguồn (`2022.7.Loyalty`) | Tập tin đích tương ứng (`MiniLoyalty`) |
|---|---|---|
| **Gate Controller** | `13.ClientGate\V10\idn.Loyalty.WebAPI\Controllers\Mst_CarBrandController.cs:25-887` | [`Controllers/Port/Mst_CarBrandControllerPort.cs`](../../Controllers/Port/Mst_CarBrandControllerPort.cs) |
| **API Backend** | `11.BackEnd\V10\idn.Loyalty.Biz.Web\Controllers\MstCarBrandController.cs:20-307` | Tích hợp trực tiếp vào [`Controllers/Port/Mst_CarBrandControllerPort.cs`](../../Controllers/Port/Mst_CarBrandControllerPort.cs) |
| **Nghiệp vụ (Biz)** | `11.BackEnd\V10\idn.Loyalty.Biz\Master.Loyalty.cs` (Select SQL `:400-447`, `:93-346`; Create `:502-828`; Update `:829-1156`; Delete `:1157-1430`; CheckDB `:28-91`) | Tích hợp trực tiếp vào [`Controllers/Port/Mst_CarBrandControllerPort.cs`](../../Controllers/Port/Mst_CarBrandControllerPort.cs) |
| **Entity Model** | `12.Dev.Common\idn.Loyalty.Common\Models\Loyalty\Mst_CarBrand.cs:9-22` | [`Models/Port/Mst_CarBrand.cs:29-38`](../../Models/Port/Mst_CarBrand.cs#L29-L38) |
| **Seeder Demo** | `Order => 19` (danh mục `Mst_*` dùng 10–99) | [`Models/Port/Mst_CarBrand.cs:56-73`](../../Models/Port/Mst_CarBrand.cs#L56-L73) |

### 1.3. Cấu trúc Bảng CSDL (`Mst_CarBrand`)

Bảng dữ liệu vật lý `Mst_CarBrand` gồm đúng 7 cột (nguồn không có script DDL tạo bảng trong repo, cấu trúc cột được xác nhận qua model và code nghiệp vụ; khóa chính `BrandCode` là khóa draft):

| Tên cột | Kiểu EF Core | Khóa | Cho phép NULL | Ghi chú & Ý nghĩa |
|---|---|---|---|---|
| `BrandCode` | `string` | **PK** | Không | Mã hãng xe. Khóa chính (draft - không có DDL nguồn, kiểu cột cần xác nhận). |
| `NetworkID` | `string?` | | Có | Mã mạng nghiệp vụ. Ở đích lấy từ request hoặc cấu hình `Biz_NetworkID`. |
| `BrandName` | `string?` | | Có | Tên hãng xe (ở nghiệp vụ Create bị viết hoa qua `StdParam` theo hành vi nguồn). |
| `FlagActive` | `string?` | | Có | Cờ trạng thái hoạt động: `"1"` (Đang hoạt động), `"0"` (Ngừng hoạt động). |
| `Remark` | `string?` | | Có | Ghi chú thêm về hãng xe. |
| `LogLUDTimeUTC` | `DateTime?` | | Có | Thời điểm cập nhật cuối cùng theo chuẩn UTC. |
| `LogLUBy` | `string?` | | Có | Tài khoản thực hiện cập nhật cuối cùng (mặc định gán `"port"` ở đích). |

**Dữ liệu mẫu khởi tạo (Seeder - `Mst_CarBrandSeeder`, Idempotent)**:
- Gồm 4 bản ghi demo: `HYU` (Hyundai, FlagActive `'1'`), `KIA` (Kia, `'1'`), `TOY` (Toyota, `'1'`), `VIN` (VinFast, FlagActive `'0'`).
- Toàn bộ bản ghi gán `NetworkID = "6403834000"`, `LogLUBy = "seed"`.

---

## 2. Đặc tả Chi tiết Từng Endpoint API

---

### 2.1. Search (#77)

Tìm kiếm danh mục hãng xe phân trang theo từ khóa và trạng thái hoạt động.

- **Route**: `POST /api/mst_carbrand/search`
- **Controller Action**: [`Mst_CarBrandControllerPort.Search`](../../Controllers/Port/Mst_CarBrandControllerPort.cs#L170-L195)
- **Tham chiếu nguồn**: Gate `Mst_CarBrandController.cs:25-105` → API `MstCarBrandController.cs:238-307` → Biz `Master.Loyalty.cs:400-447`

#### Bảng tham số Request

Payload JSON gửi qua Request Body (`MstCarBrandSearchReq`), hỗ trợ cả trường hợp body rỗng hoàn toàn:

| Tên tham số | Kiểu dữ liệu | Bắt buộc | Mặc định | Ý nghĩa & Quy tắc |
|---|---|---|---|---|
| `KeyWord` | `string?` | Không | `null` | Từ khóa tìm kiếm theo tên hãng xe (`BrandName LIKE '%kw%'`, `:290`). |
| `FlagActive` | `string?` | Không | `null` | Danh sách trạng thái phân cách dấu phẩy. Áp dụng toán tử `IN ('a','b')` (`:297-305`); dòng `=` (`:293-296`) bị comment nên không thực thi. |
| `Ft_PageIndex` | `int` | Không | `0` | Chỉ số trang cần lấy (0-based). Nếu `< 0` tự động gán về `0`. |
| `Ft_PageSize` | `int` | Không | `10` | Số dòng trên mỗi trang. Nếu `<= 0` trả về mảng rỗng `DataList = []`. |

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Chuẩn hóa phân trang: `pageIndex = Math.Max(0, r.Ft_PageIndex)`, `pageSize = r.Ft_PageSize`.
2. Vị trí bản ghi bắt đầu: `start = (long)pageIndex * pageSize`.
3. Mệnh đề lọc qua `GetXAsync` ([`Mst_CarBrandControllerPort.cs:987-1013`](../../Controllers/Port/Mst_CarBrandControllerPort.cs#L987-L1013)):
   - **KeyWord**: `EF.Functions.Like((x.BrandName ?? "").ToLower(), PortQuery.Contains(keyWord), PortQuery.Esc)`.
   - **FlagActive**: Tách chuỗi dấu phẩy, lọc `ds.Contains((x.FlagActive ?? "").ToLower())`.
4. Đếm tổng số bản ghi: `itemCount = await ordered.CountAsync()` (`Master.Loyalty.cs:441`).
5. Sắp xếp thứ tự: Phân trang theo `mcb.BrandCode ASC` (`:407`), select cuối sắp theo `t.MyIdxSeq ASC` (`:447`).
6. Trích xuất trang: Nếu `pageSize <= 0` trả về danh sách rỗng (`:83-94`); nếu `pageSize > 0` gọi `.Skip((int)start).Take((int)pageSize)`.
7. Gán `MyIdxSeq = start + i` (0-based, `:401`).
8. Tính `PageCount`: Nếu `dataList.Count == 0`, `PageCount = 0`; ngược lại `PageCount = ceil(itemCount / ps)`.

#### Bảng mã lỗi
Endpoint không phát sinh lỗi nghiệp vụ; luôn trả về HTTP 200.

#### Ví dụ Request / Response thực tế (từ lần chạy VERIFY #77)
- **Tìm kiếm mặc định (`{}`)**:
  - Request: `POST /api/mst_carbrand/search` kèm `{}`
  - Response (HTTP 200):
    ```json
    {
      "success": true,
      "data": {
        "dataList": [
          { "myIdxSeq": 0, "brandCode": "HYU", "networkID": "6403834000", "brandName": "Hyundai", "flagActive": "1", "remark": null, "logLUDTimeUTC": null, "logLUBy": "seed" },
          { "myIdxSeq": 1, "brandCode": "KIA", "networkID": "6403834000", "brandName": "Kia", "flagActive": "1", "remark": null, "logLUDTimeUTC": null, "logLUBy": "seed" },
          { "myIdxSeq": 2, "brandCode": "TOY", "networkID": "6403834000", "brandName": "Toyota", "flagActive": "1", "remark": null, "logLUDTimeUTC": null, "logLUBy": "seed" },
          { "myIdxSeq": 3, "brandCode": "VIN", "networkID": "6403834000", "brandName": "VinFast", "flagActive": "0", "remark": null, "logLUDTimeUTC": null, "logLUBy": "seed" }
        ],
        "itemCount": 4,
        "pageCount": 1,
        "pageIndex": 0,
        "pageSize": 10
      }
    }
    ```
- **Lọc `FlagActive = "1"`**: `itemCount = 3`, loại bỏ `VIN`.

---

### 2.2. GetAllActive (#78)

Lấy danh sách tất cả các hãng xe đang hoạt động (`FlagActive = '1'`).

- **Route**: `POST /api/mst_carbrand/getallactive`
- **Controller Action**: [`Mst_CarBrandControllerPort.GetAllActive`](../../Controllers/Port/Mst_CarBrandControllerPort.cs#L198-L208)
- **Tham chiếu nguồn**: Gate `Mst_CarBrandController.cs:106-215` → API `MstCarBrandController.cs:238-307` → Biz `Master.Loyalty.cs:400-447`

#### Bảng tham số Request
Endpoint không yêu cầu tham số request body.

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Tham số mặc định nguồn: `Ft_RecordStart = "0"`, `Ft_RecordCount = "123456000"`, `FlagActive = "1"` (`:132-136`).
2. Mệnh đề lọc: Áp dụng `FlagActive = '1'` qua toán tử `IN ('1')` (`:297-305`). Mã `OrgID` được gán vào request tại gate (`:134, :146`) nhưng không được tham chiếu trong hàm lọc.
3. Sắp xếp: Phân trang theo `mcb.BrandCode ASC` (`:407`), `MyIdxSeq` 0-based (`:401`).
4. **Cấu trúc dữ liệu trả về**: Trả về **mảng phẳng (flat array)** `List<Mst_CarBrand>` trực tiếp trong thuộc tính `Data` (`:195-199`). Nếu không có dòng nào, `Data = null` (`:129, :155-157`).

#### Bảng mã lỗi
Endpoint luôn trả về HTTP 200.

#### Ví dụ Request / Response thực tế (từ lần chạy VERIFY #78)
- Request: `POST /api/mst_carbrand/getallactive`
- Response (HTTP 200):
  ```json
  {
    "success": true,
    "data": [
      { "myIdxSeq": 0, "brandCode": "HYU", "networkID": "6403834000", "brandName": "Hyundai", "flagActive": "1", ... },
      { "myIdxSeq": 1, "brandCode": "KIA", "networkID": "6403834000", "brandName": "Kia", "flagActive": "1", ... },
      { "myIdxSeq": 2, "brandCode": "TOY", "networkID": "6403834000", "brandName": "Toyota", "flagActive": "1", ... }
    ]
  }
  ```

---

### 2.3. GetByBrandCode (#79)

Tra cứu chi tiết một hãng xe theo mã hiệu xe (`BrandCode`).

- **Route**: `POST /api/mst_carbrand/getbybrandcode`
- **Controller Action**: [`Mst_CarBrandControllerPort.GetByBrandCode`](../../Controllers/Port/Mst_CarBrandControllerPort.cs#L216-L240)
- **Tham chiếu nguồn**: Gate `Mst_CarBrandController.cs:216-279` → API `MstCarBrandController.cs:238-307` → Biz `Master.Loyalty.cs:400-447`

#### Bảng tham số Request

Payload JSON gửi qua Request Body (`MstCarBrandGetByBrandCodeReq`):

| Tên tham số | Kiểu dữ liệu | Bắt buộc | Mặc định | Ý nghĩa & Quy tắc |
|---|---|---|---|---|
| `BrandCode` | `string?` | Có | `null` | Mã hãng xe cần tra cứu (`:238-241`). Nếu rỗng hoặc null, nguồn không truy vấn và trả về `Data = null`. |

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Kiểm tra mã: Nếu `string.IsNullOrWhiteSpace(req?.BrandCode)`, trả về ngay `Ok(new { Success = true, Data = (object?)null })` (`:235-236, :266`).
2. Mệnh đề lọc: Gọi `GetXAsync` với `brandCodeEqual = req.BrandCode` (so sánh bằng không phân biệt hoa thường).
3. **Cờ hoạt động không lọc**: Nhánh `FlagActive` không được truyền ở gate nên không lọc cờ; **hãng xe ngừng hoạt động (`VIN`, FlagActive `'0'`) vẫn tra cứu được**.
4. Phân trang nguồn: `Ft_RecordStart = "0"`, `Ft_RecordCount = "1"` (`:253-254`). Trả về `Data = rows[0]` hoặc `null` nếu không tìm thấy (`:260-263`).

#### Bảng mã lỗi
Endpoint luôn trả về HTTP 200.

#### Ví dụ Request / Response thực tế (từ lần chạy VERIFY #79)
- **Tra cứu hợp lệ**:
  - Request: `{"BrandCode": "kia"}`
  - Response (HTTP 200): Trả về bản ghi `KIA`.
- **Tra cứu hãng xe ngừng hoạt động (`VIN`)**:
  - Request: `{"BrandCode": "VIN"}`
  - Response (HTTP 200): Trả về bản ghi `VIN` với `flagActive = "0"`.
- **Mã không tồn tại hoặc rỗng**:
  - Request: `{"BrandCode": "KI"}` hoặc `{"BrandCode": ""}` hoặc `{}`
  - Response (HTTP 200): `{"success": true, "data": null}`.

---

### 2.4. Create (#80)

Thêm mới một hãng xe vào danh mục.

- **Route**: `POST /api/mst_carbrand/create`
- **Controller Action**: [`Mst_CarBrandControllerPort.Create`](../../Controllers/Port/Mst_CarBrandControllerPort.cs#L327-L344)
- **Tham chiếu nguồn**: Gate `Mst_CarBrandController.cs:332-419` → API `MstCarBrandController.cs:20-90` → Biz `Master.Loyalty.cs:502-828`, `:28-91`

#### Bảng tham số Request

Payload JSON gửi qua Request Body (`MstCarBrandCreateReq`):

| Tên tham số | Kiểu dữ liệu | Bắt buộc | Mặc định | Ý nghĩa & Quy tắc |
|---|---|---|---|---|
| `BrandCode` | `string?` | Có | `null` | Mã hãng xe. Chuẩn hóa qua `StdParam` (`Trim().ToUpper()`). |
| `NetworkID` | `string?` | Không | `null` | Lấy trực tiếp từ body request (`:552`), chuẩn hóa qua `StdParam`. |
| `BrandName` | `string?` | Không | `null` | Tên hãng xe. **Bị viết hoa toàn bộ qua `StdParam` (`ToUpper()`) theo đúng hành vi nguồn (`Master.Loyalty.cs:764`)**. |
| `Remark` | `string?` | Không | `null` | Ghi chú thêm. Chuẩn hóa qua `StdStr` (`Trim().Normalize()`). |

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Kiểm tra Body rỗng: Nếu null hoặc rỗng → Lỗi `Fail("Mst_CarBrand_Create")` (`:340-347`).
2. Mở một transaction duy nhất (`Master.Loyalty.cs:610 _cf.db.BeginTransaction()`).
3. Chuẩn hóa dữ liệu: `strBrandCode = StdParam(req.BrandCode)`, `strNetworkID = StdParam(req.NetworkID)`, `strBrandName = StdParam(req.BrandName)`, `strRemark = StdStr(req.Remark)`.
4. **Chuỗi kiểm tra Guard của nguồn**:
   - **Bước 1**: Nếu `string.IsNullOrEmpty(strBrandCode)` → Lỗi `Mst_CarBrand_Create_InvalidBrandCode` (`:755-763`).
   - **Bước 2**: Kiểm tra tồn tại trong DB qua `Mst_CarBrand_CheckDB(strBrandCode, Flag "0")` → Nếu đã có → Lỗi `Mst_CarBrand_CheckDB_BrandExist` (`:767-773, :64-74`).
   - **Đặc thù nguồn**: Nguồn **KHÔNG** kiểm tra rỗng cho `BrandName` và **KHÔNG** kiểm tra `NetworkID`.
5. Ghi dữ liệu 7 cột: `BrandCode`, `NetworkID`, `BrandName`, `Remark`, `FlagActive = "1"`, `LogLUDTimeUTC = DateTime.UtcNow`, `LogLUBy = "port"`.
6. Commit transaction và trả về `Ok(new { Success = true, Data = (object?)null })` (`:405-406`).

#### Bảng mã lỗi

| Mã lỗi (`ErridnLoyalty.*`) | HTTP Status | Tham số đi kèm | Điều kiện kích hoạt |
|---|---|---|---|
| `Mst_CarBrand_Create` | 400 | `Check.strJson = null` | Request body rỗng hoặc null (`:340-347`). |
| `Mst_CarBrand_Create_InvalidBrandCode` | 400 | `Check.strBrandCode` | Mã hãng xe rỗng hoặc null (`:755-763`). |
| `Mst_CarBrand_CheckDB_BrandExist` | 400 | `Check.BrandCode` | Mã hãng xe đã tồn tại trong CSDL (`:767-773`). |

#### Ví dụ Request / Response thực tế (từ lần chạy VERIFY #80)
- **Tạo mới hợp lệ**:
  - Request: `{"BrandCode": "mzd", "BrandName": "Mazda"}`
  - Response (HTTP 200): `{"success": true, "data": null}`.
  - CSDL lưu: `BrandCode = "MZD"`, `BrandName = "MAZDA"` (bị viết hoa bởi `StdParam`), `FlagActive = "1"`.
- **Trùng mã đã có (`KIA`)**:
  - Request: `{"BrandCode": "kia", "BrandName": "Kia Motors"}`
  - Response (HTTP 400): `ErridnLoyalty.Mst_CarBrand_CheckDB_BrandExist`.
- **Mã rỗng**:
  - Request: `{"BrandCode": "", "BrandName": "Test"}`
  - Response (HTTP 400): `ErridnLoyalty.Mst_CarBrand_Create_InvalidBrandCode`.

---

### 2.5. Update (#81)

Cập nhật thông tin tên, ghi chú và trạng thái hoạt động của hãng xe.

- **Route**: `POST /api/mst_carbrand/update`
- **Controller Action**: [`Mst_CarBrandControllerPort.Update`](../../Controllers/Port/Mst_CarBrandControllerPort.cs#L458-L541)
- **Tham chiếu nguồn**: Gate `Mst_CarBrandController.cs:421-488` → API `MstCarBrandController.cs:92-162` → Biz `Master.Loyalty.cs:829-1156`, `:28-91`

#### Bảng tham số Request

Payload JSON gửi qua Request Body (`MstCarBrandUpdateReq`):

| Tên tham số | Kiểu dữ liệu | Bắt buộc | Mặc định | Ý nghĩa & Quy tắc |
|---|---|---|---|---|
| `BrandCode` | `string?` | Có | `null` | Mã hãng xe cần cập nhật. Chuẩn hóa qua `StdParam`. |
| `NetworkID` | `string?` | Không | `null` | Nguồn nhận nhưng **không lưu khi update** (`:878, :1111-1113`). |
| `BrandName` | `string?` | Không | `null` | Tên hãng xe. Chuẩn hóa qua `StdStr` (**không bị viết hoa, khác Create #80**). |
| `Remark` | `string?` | Không | `null` | Ghi chú thêm. Chuẩn hóa qua `StdStr`. |
| `FlagActive` | `string?` | Không | `"0"` | Cờ hoạt động. Chuẩn hóa qua `StdFlag` (thiếu/khác `"1"` tự động đổi về `"0"`). |

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Kiểm tra Body rỗng: Nếu null hoặc rỗng → Lỗi `Fail("Mst_CarBrand_Update")` (`:431-438`).
2. Cột cập nhật `Ft_Cols_Upd`: Gate cố định gửi 3 cột: `"Mst_CarBrand.BrandCode,Mst_CarBrand.BrandName,Mst_CarBrand.Remark,Mst_CarBrand.FlagActive,"` (`:453-456`), luôn cập nhật tên, ghi chú và trạng thái. Cột `NetworkID` và `BrandCode` không đổi.
3. Mở một transaction duy nhất (`Master.Loyalty.cs:942`).
4. Chuỗi kiểm tra Guard:
   - Kiểm tra tồn tại qua `Mst_CarBrand_CheckDB(strBrandCode, Flag "1")` (`:1095-1101, :53-63`): Nếu không tìm thấy bản ghi hoặc mã rỗng → Lỗi `Mst_CarBrand_CheckDB_BrandNotFound`.
5. Cập nhật dữ liệu:
   - `entity.BrandName = strBrandName` (giữ nguyên chữ hoa/thường theo `StdStr`).
   - `entity.Remark = strRemark`.
   - `entity.FlagActive = strFlagActive` (thiếu trường này sẽ cập nhật về `"0"`).
   - `entity.LogLUDTimeUTC = DateTime.UtcNow`, `entity.LogLUBy = "port"`.
6. Commit transaction và trả về `Ok(new { Success = true, Data = (object?)null })` (`:474-475`).

#### Bảng mã lỗi

| Mã lỗi (`ErridnLoyalty.*`) | HTTP Status | Tham số đi kèm | Điều kiện kích hoạt |
|---|---|---|---|
| `Mst_CarBrand_Update` | 400 | `Check.strJson = null` | Request body rỗng hoặc null (`:431-438`). |
| `Mst_CarBrand_CheckDB_BrandNotFound` | 400 | `Check.BrandCode` | Mã hãng xe không tồn tại trong CSDL (`:1095-1101`). |

#### Ví dụ Request / Response thực tế (từ lần chạy VERIFY #81)
- **Cập nhật đầy đủ thành công**:
  - Request: `{"BrandCode": "kia", "BrandName": "  Kia Motors  ", "Remark": " ghi chú ", "FlagActive": "1"}`
  - Response (HTTP 200): `{"success": true, "data": null}`.
  - CSDL lưu: `BrandName = "Kia Motors"` (không bị viết hoa), `Remark = "ghi chú"`, `FlagActive = "1"`.
- **Cập nhật thiếu `FlagActive` (hạ cờ về `"0"`)**:
  - Request: `{"BrandCode": "TOY", "BrandName": "Toyota"}`
  - Response (HTTP 200): Thành công, `TOY` nhận `FlagActive = "0"`.
- **Mã không tồn tại**:
  - Request: `{"BrandCode": "BMW", "BrandName": "x"}`
  - Response (HTTP 400): `ErridnLoyalty.Mst_CarBrand_CheckDB_BrandNotFound`.

---

### 2.6. Delete (#82)

Xóa một hãng xe đơn lẻ khỏi danh mục.

- **Route**: `POST /api/mst_carbrand/delete`
- **Controller Action**: [`Mst_CarBrandControllerPort.Delete`](../../Controllers/Port/Mst_CarBrandControllerPort.cs#L561-L604)
- **Tham chiếu nguồn**: Gate `Mst_CarBrandController.cs:492-555` → API `MstCarBrandController.cs:165-235` → Biz `Master.Loyalty.cs:1157-1430`, `:28-91`

#### Bảng tham số Request

Payload JSON gửi qua Request Body (`MstCarBrandDeleteReq`):

| Tên tham số | Kiểu dữ liệu | Bắt buộc | Mặc định | Ý nghĩa & Quy tắc |
|---|---|---|---|---|
| `BrandCode` | `string?` | Có | `null` | Mã hãng xe cần xóa (`:492, :516`). Chuẩn hóa qua `StdParam`. |

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Kiểm tra mã rỗng tại Gate: Nếu `string.IsNullOrEmpty(BrandCode)` → Ném `ServiceException("invalid_Brandcode", "invalid_Brandcode")` (**chữ 'c' viết thường nguyên văn nguồn**, `:541`) → Trả về **HTTP 400 `ErridnLoyalty.invalid_Brandcode`**.
2. Mở một transaction duy nhất (`Master.Loyalty.cs:1255`).
3. Chuẩn hóa: `strBrandCode = StdParam(req.BrandCode)`.
4. Kiểm tra tồn tại trong DB qua `Mst_CarBrand_CheckDB(strBrandCode, Flag "1")` (`:1400-1405`) → Không thấy → Lỗi **HTTP 400 `ErridnLoyalty.Mst_CarBrand_CheckDB_BrandNotFound`**.
5. **Ràng buộc khóa ngoại**: Không có bảng nào trong toàn bộ DDL nguồn có khóa ngoại `REFERENCES Mst_CarBrand`. Bảng `Mst_CarModel.BrandCode` chỉ tham chiếu theo quy ước mã, không có FK cứng. **Xóa hãng xe đang được dòng xe sử dụng vẫn thành công** (dòng xe giữ mã hãng mồ côi — đúng hành vi nguồn).
6. Xóa bản ghi khỏi bảng `Mst_CarBrand` (`:1414-1417`).
7. Commit transaction và trả về `Ok(new { Success = true, Data = (object?)null })` (`:536-537`).

#### Bảng mã lỗi

| Mã lỗi (`ErridnLoyalty.*`) | HTTP Status | Tham số đi kèm | Điều kiện kích hoạt |
|---|---|---|---|
| `invalid_Brandcode` | 400 | `Check.BrandCode` | Mã hãng xe rỗng hoặc null tại tầng gate (`:541`). |
| `Mst_CarBrand_CheckDB_BrandNotFound` | 400 | `Check.BrandCode` | Mã hãng xe không tồn tại trong CSDL (`:1400-1405`). |

#### Ví dụ Request / Response thực tế (từ lần chạy VERIFY #82)
- **Xóa thành công**:
  - Request: `{"BrandCode": "vin"}`
  - Response (HTTP 200): `{"success": true, "data": null}`. Bản ghi `VIN` bị xóa.
- **Xóa hãng đang có dòng xe tham chiếu (`KIA`)**:
  - Request: `{"BrandCode": "KIA"}`
  - Response (HTTP 200): Thành công xóa `KIA`. Kiểm tra `/api/mst_carmodel/getbymodelcode` thì dòng xe `MOR` vẫn tồn tại và giữ `BrandCode = "KIA"`.
- **Mã rỗng**:
  - Request: `{"BrandCode": ""}` hoặc `{}`
  - Response (HTTP 400): `ErridnLoyalty.invalid_Brandcode`.

---

### 2.7. DeleteMultiple (#83)

Xóa danh sách nhiều hãng xe, thực thi theo cơ chế từng transaction độc lập (xóa một phần).

- **Route**: `POST /api/mst_carbrand/deletemultiple`
- **Controller Action**: [`Mst_CarBrandControllerPort.DeleteMultiple`](../../Controllers/Port/Mst_CarBrandControllerPort.cs#L616-L666)
- **Tham chiếu nguồn**: Gate `Mst_CarBrandController.cs:557-621` → API `WA_Mst_CarBrand_Delete` → Biz `Mst_CarBrand_Delete`

#### Bảng tham số Request

Payload JSON gửi qua Request Body: Mảng đối tượng `List<MstCarBrandDeleteReq>`.

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Kiểm tra Body rỗng: Nếu body null hoặc rỗng → Lỗi `Mst_CarBrand_Delete` (`:567-574`).
2. Mảng rỗng `[]`: Không thực thi vòng lặp, trả về `Ok(new { Success = true, Data = (object?)null })`.
3. Duyệt tuần tự từng phần tử trong danh sách:
   - Mỗi phần tử thực thi trong **1 transaction riêng biệt** (`Master.Loyalty.cs:1255`).
   - Khác với Delete đơn, DeleteMultiple không kiểm tra rỗng ở tầng gate; nếu phần tử có mã rỗng, lỗi phát sinh tại tầng CheckDB → Lỗi `ErridnLoyalty.Mst_CarBrand_CheckDB_BrandNotFound`.
   - **Hành vi Xóa một phần (Partial Delete)**: Nếu phần tử thứ `k` bị lỗi, ném exception và thoát vòng lặp ngay lập tức. **Các phần tử trước `k` đã được commit vào DB và không bị rollback**; các phần tử sau `k` không được xử lý (`:588-614`).
4. Thành công: Trả về `Ok(new { Success = true, Data = (object?)null })` (`:607-608`).

#### Bảng mã lỗi

| Mã lỗi (`ErridnLoyalty.*`) | HTTP Status | Tham số đi kèm | Điều kiện kích hoạt |
|---|---|---|---|
| `Mst_CarBrand_Delete` | 400 | `Check.strJson = null` | Body request null hoặc rỗng (`:567-574`). |
| `Mst_CarBrand_CheckDB_BrandNotFound` | 400 | `Check.BrandCode` | Phần tử thứ `k` có mã không tồn tại hoặc mã rỗng (`:1400-1405`). |

#### Minh chứng Kiểm thử Thực tế (từ lần chạy VERIFY #83)
- Request mảng 3 phần tử: `[{"BrandCode": "HYU"}, {"BrandCode": "XX"}, {"BrandCode": "TOY"}]`.
- Response (HTTP 400): `ErridnLoyalty.Mst_CarBrand_CheckDB_BrandNotFound` với `Check.BrandCode = "XX"`.
- CSDL: `HYU` **đã bị xóa thành công**, trong khi `TOY` **vẫn còn nguyên vẹn trong CSDL**.

---

### 2.8. Import (#84)

Nhập danh mục hãng xe từ tập tin Excel `.xlsx`.

- **Route**: `POST /api/mst_carbrand/import`
- **Content-Type**: `multipart/form-data`
- **Controller Action**: [`Mst_CarBrandControllerPort.Import`](../../Controllers/Port/Mst_CarBrandControllerPort.cs#L744-L853)
- **Tham chiếu nguồn**: Gate `Mst_CarBrandController.cs:625-769` → API `WA_Mst_CarBrand_Create` → Biz `Mst_CarBrand_Create`

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Kiểm tra file upload (`:649-654`): File rỗng, thiếu file hoặc sai đuôi `.xlsx` → Lỗi `Mst_CarBrand_Import` ("File excel import không hợp lệ").
2. Đọc file qua [`PortExcel.ReadSheet`](../../Data/PortExcel.cs):
   - 0 dòng dữ liệu → Lỗi `Mst_CarBrand_Import` ("File excel import không có dữ liệu").
   - Số cột khác 2 (`GetImportDicColumsTemplate().Count != 2`) → Lỗi `Mst_CarBrand_Import` ("File excel import không hợp lệ").
3. **Kiểm tra Validate toàn bộ dòng TRƯỚC khi tạo** (`:675-695`):
   - `BrandCode` rỗng → Lỗi `Mst_CarBrand_Import` kèm `Message = "Mã hiệu xe không được trống"` (`:680`).
   - `BrandName` rỗng → Lỗi `Mst_CarBrand_Import` kèm `Message = "Tên hiệu xe không được trống"` (`:685`).
   - Có bất kỳ dòng nào vi phạm, dừng ngay và không có dòng nào được tạo vào CSDL.
4. **Lưu từng dòng (Partial Import)**:
   - `NetworkID`: Lấy từ cấu hình `IConfiguration["Biz_NetworkID"] ?? "1001"` (GAP: nguồn lấy từ `orgUserUI.NetworkId`).
   - Duyệt từng dòng, gọi `CreateXAsync` trong **1 transaction riêng biệt cho mỗi dòng**.
   - Dòng thứ `k` bị lỗi (ví dụ: trùng mã `Mst_CarBrand_CheckDB_BrandExist`), vòng lặp dừng ngay và trả về lỗi của dòng `k`.
   - **Các dòng trước `k` đã được commit vào CSDL**, các dòng sau `k` không được duyệt.
5. Thành công: Trả về `Ok(new { Success = true, Data = (object?)null })` (`:749-750`).

#### Bảng mã lỗi

| Mã lỗi (`ErridnLoyalty.*`) | HTTP Status | Tham số đi kèm | Điều kiện kích hoạt |
|---|---|---|---|
| `Mst_CarBrand_Import` | 400 | `Message = "File excel import không hợp lệ"` | Không có file, file rỗng, sai đuôi hoặc số cột khác 2. |
| `Mst_CarBrand_Import` | 400 | `Message = "File excel import không có dữ liệu"` | File chỉ có tiêu đề, không có dữ liệu. |
| `Mst_CarBrand_Import` | 400 | `Message = "Mã hiệu xe không được trống"` | Dòng có `BrandCode` rỗng. |
| `Mst_CarBrand_Import` | 400 | `Message = "Tên hiệu xe không được trống"` | Dòng có `BrandName` rỗng. |
| `Mst_CarBrand_CheckDB_BrandExist` | 400 | `Check.BrandCode` | Dòng thứ `k` bị trùng mã trong CSDL. |

#### Minh chứng Kiểm thử Thực tế (từ lần chạy VERIFY #84)
- File gồm 3 dòng: `[FRD, Ford]` (hợp lệ), `[kia, x]` (trùng seed), `[HON, Honda]` (hợp lệ).
- Kết quả: HTTP 400 `ErridnLoyalty.Mst_CarBrand_CheckDB_BrandExist`.
- CSDL: `FRD` **đã được tạo thành công** (`BrandName = "FORD"`, `NetworkID = "1001"`), còn `HON` **không được tạo**.

---

### 2.9. Export (#85)

Xuất danh sách hãng xe ra tập tin Excel `.xlsx`.

- **Route**: `POST /api/mst_carbrand/export`
- **Controller Action**: [`Mst_CarBrandControllerPort.Export`](../../Controllers/Port/Mst_CarBrandControllerPort.cs#L867-L911)
- **Tham chiếu nguồn**: Gate `Mst_CarBrandController.cs:774-852` → API `WA_Mst_CarBrand_Get` → Biz `Master.Loyalty.cs:400-447`

#### Bảng tham số Request

Payload JSON gửi qua Request Body (`MstCarBrandExportReq`):

| Tên tham số | Kiểu dữ liệu | Bắt buộc | Mặc định | Ý nghĩa & Quy tắc |
|---|---|---|---|---|
| `OrgID` | `string?` | Không | `null` | Nhận tại gate nhưng không lọc (`:283-308`). |
| `KeyWord` | `string?` | Không | `null` | Lọc theo từ khóa tên hãng xe (`BrandName LIKE '%kw%'`). |
| `FlagActive` | `string?` | Không | `null` | Lọc theo trạng thái hoạt động (toán tử `IN`). |

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Gọi `GetXAsync(r.KeyWord, r.FlagActive, 0, 123456000, null)` để lấy dữ liệu (`:813-814`).
2. Kiểm tra dữ liệu rỗng: Nếu danh sách rỗng, ném `ServiceException("Không có dữ liệu")` (`:838`) → Trả về HTTP 400 `ErridnLoyalty.Mst_CarBrand_Export` kèm `Message = "Không có dữ liệu"`.
3. Từ điển cột: `GetImportDicColums()` gồm đúng 3 cột (`:824, :891-901`):
   - `BrandCode`: `"Mã hiệu xe (*)"`
   - `BrandName`: `"Tên hiệu xe (*)"`
   - `FlagActive`: `"Trạng thái"`
4. Sinh file Excel sheet `"Mst_CarBrand"` qua [`PortExcel.ExportToExcelFromList`](../../Data/PortExcel.cs).
5. Trả về `Ok(new { Success = true, Data = new { Url = baseAddress + virtualPath } })` (`:830-833`).

#### Bảng mã lỗi

| Mã lỗi (`ErridnLoyalty.*`) | HTTP Status | Tham số đi kèm | Điều kiện kích hoạt |
|---|---|---|---|
| `Mst_CarBrand_Export` | 400 | `Message = "Không có dữ liệu"` | Không có bản ghi nào thỏa mãn điều kiện lọc (`:838`). |

---

### 2.10. ExportTemplate (#86)

Xuất tập tin mẫu Excel dùng cho chức năng Import hãng xe.

- **Route**: `POST /api/mst_carbrand/exporttemplate`
- **Controller Action**: [`Mst_CarBrandControllerPort.ExportTemplate`](../../Controllers/Port/Mst_CarBrandControllerPort.cs#L922-L941)
- **Tham chiếu nguồn**: Gate `Mst_CarBrandController.cs:855-887`

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Không nhận body, không kiểm tra quyền, không đọc CSDL (`:863`).
2. Khởi tạo danh sách rỗng `List<Mst_CarBrand>`.
3. Từ điển cột: `GetImportDicColumsTemplate()` gồm đúng 2 cột (`:865, :903-911`):
   - `BrandCode`: `"Mã hiệu xe (*)"`
   - `BrandName`: `"Tên hiệu xe (*)"`
4. Sinh file Excel rỗng có dòng tiêu đề sheet `"Mst_CarBrand"`.
5. Trả về `Ok(new { Success = true, Data = new { Url = baseAddress + virtualPath } })` (`:869-873`).

#### Bảng mã lỗi
Endpoint luôn trả về HTTP 200.

---

## 3. Khác biệt So với Nguồn & Danh sách GAP (Known Gaps)

| Hạng mục | Trạng thái ở Nguồn | Triển khai ở Đích | Phân loại & Ghi chú GAP |
|---|---|---|---|
| **Xác thực & Phân quyền** | Gate kiểm tra `CheckGatewayAuthentication`, `CheckAuthorize`; Biz kiểm tra `Sys_User_CheckAuthorize`. | Bỏ qua toàn bộ kiểm tra phân quyền. | Bỏ qua có chủ đích (**Loại Đ**). |
| **Ghi nhật ký hệ thống** | Ghi bản ghi `RequestLog` và `ReturnLog` vào CSDL cho mọi thao tác. | Bỏ qua ghi log DB. | Bỏ qua có chủ đích (**Loại Đ**). |
| **Viết hoa tên khi Create** | `BrandName` được chuẩn hóa qua `StdParam` (`Trim().ToUpper()`) tại `Master.Loyalty.cs:764`. | Giữ nguyên hành vi viết hoa `BrandName` khi Create. | Khớp nguồn 1-1. |
| **Bảo lưu tên khi Update** | `BrandName` khi Update chuẩn hóa qua `StdStr` (`Trim().Normalize()`), không bị viết hoa. | Giữ nguyên phân biệt hoa thường khi Update. | Khớp nguồn 1-1. |
| **Lỗi xóa `invalid_Brandcode`** | Ném exception với chữ `'c'` viết thường `"invalid_Brandcode"`. | Giữ nguyên mã lỗi `ErridnLoyalty.invalid_Brandcode`. | Khớp nguồn 1-1. |
| **Không có khóa ngoại chặn xóa** | DDL nguồn không có `REFERENCES Mst_CarBrand`. Dòng xe (`Mst_CarModel`) chỉ lưu mã theo quy ước. | Xóa hãng xe thành công dù đang có dòng xe tham chiếu. | Khớp nguồn 1-1. |
| **Mã mạng trong Import** | Nguồn lấy `NetworkID` từ phiên đăng nhập `orgUserUI.NetworkId`. | Đích lấy từ cấu hình `IConfiguration["Biz_NetworkID"] ?? "1001"`. | **GAP**: Chưa có session người dùng. |

---

## 4. Nhật ký Thay đổi & Đối chiếu Tiến độ (Changelog)

| STT Hạng mục | Tên Endpoint & Action | Trạng thái | Mã Commit Git | Mô tả tóm tắt thay đổi |
|---|---|---|---|---|
| **#77** | `Mst_CarBrandController/Search` | Đã hoàn thành | `58254a7` | Khởi tạo model [`Mst_CarBrand.cs`](../../Models/Port/Mst_CarBrand.cs), cấu hình EF Core, Seeder 4 bản ghi và action tìm kiếm phân trang. |
| **#78** | `Mst_CarBrandController/GetAllActive` | Đã hoàn thành | `42bec14` | Lấy danh sách các hãng xe đang hoạt động, trả về mảng phẳng trực tiếp. |
| **#79** | `Mst_CarBrandController/GetByBrandCode` | Đã hoàn thành | `9f12551` | Tra cứu chi tiết một hãng xe theo mã hiệu xe, không lọc cờ hoạt động. |
| **#80** | `Mst_CarBrandController/Create` | Đã hoàn thành | `aac1677` | Thêm mới hãng xe, tự động viết hoa tên hãng theo `StdParam`, kiểm tra trùng mã. |
| **#81** | `Mst_CarBrandController/Update` | Đã hoàn thành | `8c9d925` | Cập nhật thông tin hãng xe, bảo lưu hoa thường tên hãng theo `StdStr`, tự động hạ cờ về `'0'` nếu thiếu `FlagActive`. |
| **#82** | `Mst_CarBrandController/Delete` | Đã hoàn thành | `e2b928d` | Xóa đơn lẻ hãng xe, phát sinh mã lỗi chữ thường `invalid_Brandcode`, không chặn khóa ngoại. |
| **#83** | `Mst_CarBrandController/DeleteMultiple` | Đã hoàn thành | `3be4019` | Xóa danh sách hãng xe theo từng transaction độc lập, thực thi cơ chế xóa một phần. |
| **#84** | `Mst_CarBrandController/Import` | Đã hoàn thành | `f00ecc2` | Nhập danh mục từ Excel, validate toàn bộ dòng trước khi tạo và thực thi cơ chế nhập một phần. |
| **#85** | `Mst_CarBrandController/Export` | Đã hoàn thành | `451e2e1` | Xuất danh sách hãng xe ra Excel, sử dụng từ điển 3 cột, báo lỗi khi không có dữ liệu. |
| **#86** | `Mst_CarBrandController/ExportTemplate` | Đã hoàn thành | `a75af45` | Xuất file mẫu Excel gồm 2 cột tiêu đề, không truy vấn CSDL. |
