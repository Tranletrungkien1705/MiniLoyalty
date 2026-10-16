# Tham chiếu API Mst_ProvinceController (Port 1-1)

| Thông tin | Giá trị |
|---|---|
| **Phạm vi** | Tài liệu tham chiếu chi tiết 9 endpoint đã port 1-1 của Mst_ProvinceController (#213 - #221) |
| **Cập nhật** | 2026-10-16 |
| **Phiên bản mã** | `93b6ace` |
| **Trạng thái** | Nháp — chờ review |

---

## 1. Tóm tắt Nghiệp vụ & Cấu trúc Dữ liệu

### 1.1. Mục đích Nghiệp vụ
API `Mst_ProvinceController` quản lý danh mục tỉnh/thành phố (`Mst_Province`) trong hệ thống MiniLoyalty. Phân hệ cung cấp các chức năng tìm kiếm phân trang theo từ khóa và trạng thái hoạt động, tra cứu chi tiết một tỉnh/thành theo mã, lấy toàn bộ danh sách tỉnh/thành đang hoạt động, thêm mới, cập nhật tên và trạng thái, xóa đơn lẻ, nhập danh mục từ bảng tính Excel và xuất dữ liệu/mẫu Excel chuẩn.

### 1.2. Bảng Đối chiếu Nguồn Tra cứu (Source Code Mapping)

| Thành phần | Đường dẫn tập tin nguồn (`2022.7.Loyalty`) | Tập tin đích tương ứng (`MiniLoyalty`) |
|---|---|---|
| **Gate Controller** | `13.ClientGate\V10\idn.Loyalty.WebAPI\Controllers\Mst_ProvinceController.cs:31-710` | [`Controllers/Port/Mst_ProvinceControllerPort.cs`](../../Controllers/Port/Mst_ProvinceControllerPort.cs) |
| **API Backend** | `11.BackEnd\V10\idn.Loyalty.Biz.Web\Controllers\MstProvinceController.cs:20-313` | Tích hợp trực tiếp vào [`Controllers/Port/Mst_ProvinceControllerPort.cs`](../../Controllers/Port/Mst_ProvinceControllerPort.cs) |
| **Nghiệp vụ (Biz)** | `11.BackEnd\V10\idn.Loyalty.Biz\Master.Loyalty.cs` (Select SQL `:442-488`); `11.BackEnd\V10\idn.Loyalty.Biz\Master.cs` (Create `:591-743`, `:1215-1297`; Update `:744-894`, `:1299-1385`; Delete `:895-1006`, `:1387-1468`; CheckDB `:27-91`) | Tích hợp trực tiếp vào [`Controllers/Port/Mst_ProvinceControllerPort.cs`](../../Controllers/Port/Mst_ProvinceControllerPort.cs) |
| **Entity Model** | `12.Dev.Common\idn.Loyalty.Common\Models\Mst_Province.cs:9-22` | [`Models/Port/Mst_Province.cs:29-37`](../../Models/Port/Mst_Province.cs#L29-L37) |
| **Seeder Demo** | `Order => 15` (danh mục `Mst_*` dùng 10–99) | [`Models/Port/Mst_Province.cs:55-73`](../../Models/Port/Mst_Province.cs#L55-L73) |

### 1.3. Cấu trúc Bảng CSDL (`Mst_Province`)

Bảng dữ liệu vật lý `Mst_Province` gồm đúng 6 cột (mã nguồn gốc không có kịch bản DDL `.sql` tạo bảng `Mst_Province`, cấu trúc cột và kiểu dữ liệu được xác nhận qua model và code nghiệp vụ; khóa chính `ProvinceCode` là khóa draft):

| Tên cột | Kiểu EF Core | Khóa | Cho phép NULL | Ghi chú & Ý nghĩa |
|---|---|---|---|---|
| `ProvinceCode` | `string` | **PK** | Không | Mã tỉnh/thành. Khóa chính (draft - không có DDL nguồn, kiểu cột cần xác nhận). |
| `NetworkID` | `string?` | | Có | Mã mạng nghiệp vụ. Ở đích lấy từ cấu hình `Biz_NetworkID` (mặc định `"1001"`). |
| `ProvinceName` | `string?` | | Có | Tên tỉnh/thành phố. |
| `FlagActive` | `string?` | | Có | Cờ trạng thái hoạt động: `"1"` (Đang hoạt động), `"0"` (Ngừng hoạt động). |
| `LogLUDTimeUTC` | `DateTime?` | | Có | Thời điểm cập nhật cuối cùng theo chuẩn UTC. |
| `LogLUBy` | `string?` | | Có | Tài khoản thực hiện cập nhật cuối cùng (mặc định gán `"port"` ở đích). |

**Dữ liệu mẫu khởi tạo (Seeder - `Mst_ProvinceSeeder`, Idempotent)**:
- Gồm 5 bản ghi demo: `HN` (Hà Nội, FlagActive `'1'`), `HCM` (Hồ Chí Minh, `'1'`), `DN` (Đà Nẵng, `'1'`), `HP` (Hải Phòng, `'1'`), `CT` (Cần Thơ, FlagActive `'0'`).
- Toàn bộ bản ghi gán `NetworkID = "6403834000"`, `LogLUBy = "seed"`.

---

## 2. Đặc tả Chi tiết Từng Endpoint API

---

### 2.1. Search (#213)

Tìm kiếm danh mục tỉnh/thành phân trang theo từ khóa và cờ trạng thái hoạt động.

- **Route**: `POST /api/mst_province/search`
- **Controller Action**: [`Mst_ProvinceControllerPort.Search`](../../Controllers/Port/Mst_ProvinceControllerPort.cs#L143-L168)
- **Tham chiếu nguồn**: Gate `Mst_ProvinceController.cs:31-111` → API `MstProvinceController.cs:20-91` → Biz `Master.Loyalty.cs:442-488`

#### Bảng tham số Request

Payload JSON gửi qua Request Body (`MstProvinceSearchReq`), hỗ trợ cả trường hợp body rỗng hoàn toàn:

| Tên tham số | Kiểu dữ liệu | Bắt buộc | Mặc định | Ý nghĩa & Quy tắc |
|---|---|---|---|---|
| `OrgID` | `string?` | Không | `null` | Nhận tại tầng gate (`:31, :61`) nhưng hàm lọc `strWhereClause_Mst_Province` (`:735-759`) **không sử dụng** (bảng không có cột `OrgID`). |
| `KeyWord` | `string?` | Không | `null` | Từ khóa tìm kiếm theo tên tỉnh/thành (`ProvinceName LIKE '%kw%'`, `:740-743`). |
| `FlagActive` | `string?` | Không | `null` | Danh sách trạng thái phân cách bởi dấu phẩy (ví dụ: `"1"` hoặc `"0,1"`). Áp dụng toán tử `IN ('0', '1')` (`:748-756`). |
| `Ft_PageIndex` | `int` | Không | `0` | Chỉ số trang cần lấy (0-based). Nếu `< 0` tự động gán về `0`. |
| `Ft_PageSize` | `int` | Không | `10` | Số dòng trên mỗi trang. Nếu `<= 0` trả về mảng rỗng `DataList = []`. |

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Chuẩn hóa phân trang: `pageIndex = Math.Max(0, r.Ft_PageIndex)`, `pageSize = r.Ft_PageSize`.
2. Vị trí bản ghi bắt đầu: `start = (long)pageIndex * pageSize`.
3. Xây dựng mệnh đề lọc qua hàm `GetXAsync` ([`Mst_ProvinceControllerPort.cs:795-840`](../../Controllers/Port/Mst_ProvinceControllerPort.cs#L795-L840)):
   - **KeyWord**: Nếu có giá trị, lọc `EF.Functions.Like((x.ProvinceName ?? "").ToLower(), PortQuery.Contains(keyWord), PortQuery.Esc)`.
   - **FlagActive**: Tách chuỗi theo dấu phẩy, lọc `ds.Contains((x.FlagActive ?? "").ToLower())`.
   - **OrgID**: Bỏ qua, không lọc.
4. Đếm tổng số bản ghi thỏa mãn điều kiện lọc: `itemCount = await ordered.CountAsync()` (tương ứng `MyCount` trong SQL nguồn `:484`).
5. Sắp xếp thứ tự bản ghi: Bắt buộc sắp theo `mp.ProvinceCode ASC` (`:449`), select cuối sắp theo `t.MyIdxSeq ASC` (`:488`).
6. Trích xuất trang dữ liệu:
   - Nếu `pageSize <= 0`: Trả về danh sách rỗng (nguồn gate `:86-94`).
   - Nếu `pageSize > 0`: Áp dụng `.Skip((int)start).Take((int)pageSize)`.
7. Gán chỉ số `MyIdxSeq = start + i` (0-based) tương ứng số thứ tự định danh trong SQL nguồn (`:443`).
8. Tính toán số trang `PageCount`:
   - Nếu `dataList.Count == 0`, `PageCount = 0`.
   - Ngược lại, `PageCount = ceil(itemCount / ps)` với `ps = pageSize == 0 ? 10 : pageSize` (`:86-94`).

#### Bảng mã lỗi
Endpoint không phát sinh lỗi nghiệp vụ; các tham số không hợp lệ hoặc không tìm thấy dữ liệu đều trả về HTTP 200 kèm danh sách rỗng.

#### Ví dụ Request / Response thực tế (từ lần chạy VERIFY #213)

- **Ví dụ 1: Tìm kiếm mặc định (Empty Body `{}`)**
  - Request: `POST /api/mst_province/search` kèm `{}`
  - Response (HTTP 200):
    ```json
    {
      "success": true,
      "data": {
        "dataList": [
          { "myIdxSeq": 0, "provinceCode": "CT", "networkID": "6403834000", "provinceName": "Cần Thơ", "flagActive": "0", "logLUDTimeUTC": null, "logLUBy": "seed" },
          { "myIdxSeq": 1, "provinceCode": "DN", "networkID": "6403834000", "provinceName": "Đà Nẵng", "flagActive": "1", "logLUDTimeUTC": null, "logLUBy": "seed" },
          { "myIdxSeq": 2, "provinceCode": "HCM", "networkID": "6403834000", "provinceName": "Hồ Chí Minh", "flagActive": "1", "logLUDTimeUTC": null, "logLUBy": "seed" },
          { "myIdxSeq": 3, "provinceCode": "HN", "networkID": "6403834000", "provinceName": "Hà Nội", "flagActive": "1", "logLUDTimeUTC": null, "logLUBy": "seed" },
          { "myIdxSeq": 4, "provinceCode": "HP", "networkID": "6403834000", "provinceName": "Hải Phòng", "flagActive": "1", "logLUDTimeUTC": null, "logLUBy": "seed" }
        ],
        "itemCount": 5,
        "pageCount": 1,
        "pageIndex": 0,
        "pageSize": 10
      }
    }
    ```
- **Ví dụ 2: Lọc theo cờ `FlagActive = "1"`**
  - Request: `{"FlagActive": "1"}`
  - Response (HTTP 200): `itemCount = 4`, `pageCount = 1`, loại bỏ `CT` (FlagActive `'0'`).
- **Ví dụ 3: Đặt `Ft_PageSize = 0`**
  - Request: `{"Ft_PageSize": 0}`
  - Response (HTTP 200): `itemCount = 5`, `pageCount = 0`, `dataList = []`.

---

### 2.2. GetByProvinceCode (#214)

Tra cứu thông tin chi tiết một tỉnh/thành phố theo mã.

- **Route**: `POST /api/mst_province/getbyprovincecode`
- **Controller Action**: [`Mst_ProvinceControllerPort.GetByProvinceCode`](../../Controllers/Port/Mst_ProvinceControllerPort.cs#L170-L194)
- **Tham chiếu nguồn**: Gate `Mst_ProvinceController.cs:113-176` → API `MstProvinceController.cs:20-91` → Biz `Master.Loyalty.cs:442-488`

#### Bảng tham số Request

| Tên tham số | Kiểu dữ liệu | Bắt buộc | Mặc định | Ý nghĩa & Quy tắc |
|---|---|---|---|---|
| `ProvinceCode` | `string?` | Có | `null` | Mã tỉnh/thành cần tra cứu (`:135-138`). Nếu rỗng hoặc `null`, nguồn không truy vấn DB và trả về `Data = null`. |

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Kiểm tra tham số đầu vào: Nếu `string.IsNullOrWhiteSpace(req?.ProvinceCode)`, không thực hiện truy vấn CSDL, trả về ngay `Ok(new { Success = true, Data = (object?)null })` (nguồn gate `:132-133, :163`).
2. Xây dựng truy vấn qua `GetXAsync` với điều kiện `provinceCodeEqual = req.ProvinceCode`:
   - So sánh bằng không phân biệt hoa thường qua `PortQuery.Equal`.
   - Nguồn không truyền `FlagActive` (`:135-138`) nên mệnh đề WHERE chỉ lọc theo `ProvinceCode = '...'`. **Tỉnh/thành có `FlagActive = '0'` vẫn lấy được**.
   - Cố định phân trang: `start = 0`, `count = 1` (`:150-151`).
3. Kết quả: Nếu tìm thấy bản ghi, trả về `Data = rows[0]`; nếu không tìm thấy, trả về `Data = null` (`:157-160`).

#### Bảng mã lỗi
Endpoint luôn trả về HTTP 200.

#### Ví dụ Request / Response thực tế (từ lần chạy VERIFY #214)
- **Tra cứu tỉnh hoạt động (`HN`)**:
  - Request: `{"ProvinceCode": "hn"}`
  - Response (HTTP 200):
    ```json
    {
      "success": true,
      "data": {
        "myIdxSeq": 0,
        "provinceCode": "HN",
        "networkID": "6403834000",
        "provinceName": "Hà Nội",
        "flagActive": "1",
        "logLUDTimeUTC": null,
        "logLUBy": "seed"
      }
    }
    ```
- **Tra cứu tỉnh ngừng hoạt động (`CT`)**:
  - Request: `{"ProvinceCode": "CT"}`
  - Response (HTTP 200): Lấy được bản ghi `CT` với `flagActive = "0"`.
- **Mã không tồn tại hoặc rỗng**:
  - Request: `{"ProvinceCode": "XX"}` hoặc `{"ProvinceCode": ""}` hoặc `{}`
  - Response (HTTP 200): `{"success": true, "data": null}`.

---

### 2.3. GetAllActive (#215)

Lấy danh sách tất cả các tỉnh/thành phố đang hoạt động (`FlagActive = '1'`).

- **Route**: `POST /api/mst_province/getallactive`
- **Controller Action**: [`Mst_ProvinceControllerPort.GetAllActive`](../../Controllers/Port/Mst_ProvinceControllerPort.cs#L197-L202)
- **Tham chiếu nguồn**: Gate `Mst_ProvinceController.cs:178-241` → API `MstProvinceController.cs:20-91` → Biz `Master.Loyalty.cs:442-488`

#### Bảng tham số Request
Endpoint không yêu cầu tham số request body.

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Thiết lập tham số nguồn gate (`:213-214`): `start = 0`, `count = 123456000`, `FlagActive = "1"`.
2. Mệnh đề lọc: Áp dụng `FlagActive = '1'` qua `strWhereClause_Mst_Province` (`:748-756`). Mã `OrgID` được gán vào request tại gate (`:211`) nhưng không tham gia mệnh đề WHERE.
3. Sắp xếp: Theo `mp.ProvinceCode ASC` (`:449`), `MyIdxSeq` đánh số 0-based (`:443`).
4. **Cấu trúc dữ liệu trả về (Đặc thù nguồn)**:
   - Dữ liệu được bọc trong đối tượng `{ Lst_Mst_Province = rows }` (`:197, :220-227`).
   - Nếu không có dòng nào, `Lst_Mst_Province = null` (trả về `Data = { Lst_Mst_Province = null }`).

#### Bảng mã lỗi
Endpoint luôn trả về HTTP 200.

#### Ví dụ Request / Response thực tế (từ lần chạy VERIFY #215)
- Request: `POST /api/mst_province/getallactive`
- Response (HTTP 200):
  ```json
  {
    "success": true,
    "data": {
      "lst_Mst_Province": [
        { "myIdxSeq": 0, "provinceCode": "DN", "networkID": "6403834000", "provinceName": "Đà Nẵng", "flagActive": "1", "logLUDTimeUTC": null, "logLUBy": "seed" },
        { "myIdxSeq": 1, "provinceCode": "HCM", "networkID": "6403834000", "provinceName": "Hồ Chí Minh", "flagActive": "1", "logLUDTimeUTC": null, "logLUBy": "seed" },
        { "myIdxSeq": 2, "provinceCode": "HN", "networkID": "6403834000", "provinceName": "Hà Nội", "flagActive": "1", "logLUDTimeUTC": null, "logLUBy": "seed" },
        { "myIdxSeq": 3, "provinceCode": "HP", "networkID": "6403834000", "provinceName": "Hải Phòng", "flagActive": "1", "logLUDTimeUTC": null, "logLUBy": "seed" }
      ]
    }
  }
  ```

---

### 2.4. Create (#216)

Thêm mới một tỉnh/thành phố vào danh mục.

- **Route**: `POST /api/mst_province/create`
- **Controller Action**: [`Mst_ProvinceControllerPort.Create`](../../Controllers/Port/Mst_ProvinceControllerPort.cs#L225-L251)
- **Tham chiếu nguồn**: Gate `Mst_ProvinceController.cs:245-305` → API `MstProvinceController.cs:94-165` → Biz `Master.cs:1215-1297`, `:591-743`, `:27-91`

#### Bảng tham số Request

Payload JSON gửi qua Request Body (`MstProvinceCreateReq`):

| Tên tham số | Kiểu dữ liệu | Bắt buộc | Mặc định | Ý nghĩa & Quy tắc |
|---|---|---|---|---|
| `ProvinceCode` | `string?` | Có | `null` | Mã tỉnh/thành. Chuẩn hóa qua `StdParam` (`Trim().ToUpper()`, rỗng → `null`). |
| `ProvinceName` | `string?` | Có | `null` | Tên tỉnh/thành. Chuẩn hóa qua `.Trim()` (không Normalize, giữ nguyên nguồn `:641`). |

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Kiểm tra Body rỗng: Nếu body null hoặc rỗng, ném lỗi `Fail("Mst_Province_Create")` (`:254-261`).
2. Mở một transaction duy nhất (`Master.cs:608 _cf.db.BeginTransaction()`).
3. Chuẩn hóa:
   - `strProvinceCode = StdParam(req.ProvinceCode)`.
   - `strProvinceName = string.Format("{0}", req.ProvinceName).Trim()`.
4. Chuỗi kiểm tra Guard theo đúng thứ tự nguồn:
   - **Bước 1**: Nếu `strProvinceCode == null || strProvinceCode.Length < 1` → Lỗi `Mst_Province_Create_InvalidProvinceCode` (`:649-659`).
   - **Bước 2**: Kiểm tra tồn tại trong CSDL qua `Mst_Province_CheckDB` (cờ `"0"`) → Nếu đã tồn tại → Lỗi `Mst_Province_CheckDB_ProvinceExist` (`:660-666, :64-74`).
   - **Bước 3**: Nếu `strProvinceName.Length < 1` → Lỗi `Mst_Province_Create_InvalidProvinceName` (`:668-678`).
5. Ghi dữ liệu 6 cột:
   - `ProvinceCode = strProvinceCode`, `ProvinceName = strProvinceName`.
   - `NetworkID = config["Biz_NetworkID"] ?? "1001"`.
   - `FlagActive = "1"`, `LogLUDTimeUTC = DateTime.UtcNow`, `LogLUBy = "port"`.
6. Commit transaction và trả về `Ok(new { Success = true, Data = (object?)null })` (`:291-292`).

#### Bảng mã lỗi

| Mã lỗi (`ErridnLoyalty.*`) | HTTP Status | Tham số đi kèm | Điều kiện kích hoạt |
|---|---|---|---|
| `Mst_Province_Create` | 400 | `Check.strJson = null` | Request body rỗng hoặc null (`:254-261`). |
| `Mst_Province_Create_InvalidProvinceCode` | 400 | `Check.strProvinceCode` | Mã tỉnh/thành rỗng hoặc null (`:649-659`). |
| `Mst_Province_CheckDB_ProvinceExist` | 400 | `Check.ProvinceCode` | Mã tỉnh/thành đã tồn tại trong CSDL (`:660-666`). |
| `Mst_Province_Create_InvalidProvinceName` | 400 | `Check.strProvinceName` | Tên tỉnh/thành rỗng sau khi Trim (`:668-678`). |

#### Ví dụ Request / Response thực tế (từ lần chạy VERIFY #216)
- **Tạo mới thành công**:
  - Request: `{"ProvinceCode": "qn", "ProvinceName": "Quảng Ninh"}`
  - Response (HTTP 200): `{"success": true, "data": null}`. CSDL lưu `ProvinceCode = "QN"`, `FlagActive = "1"`.
- **Trùng mã đã có**:
  - Request: `{"ProvinceCode": "hn", "ProvinceName": "Hà Nội mới"}`
  - Response (HTTP 400):
    ```json
    {
      "success": false,
      "errorCode": "ErridnLoyalty.Mst_Province_CheckDB_ProvinceExist",
      "params": { "Check.ProvinceCode": "HN" }
    }
    ```

---

### 2.5. Update (#217)

Cập nhật thông tin tên và trạng thái hoạt động của tỉnh/thành phố.

- **Route**: `POST /api/mst_province/update`
- **Controller Action**: [`Mst_ProvinceControllerPort.Update`](../../Controllers/Port/Mst_ProvinceControllerPort.cs#L356-L432)
- **Tham chiếu nguồn**: Gate `Mst_ProvinceController.cs:309-373` → API `MstProvinceController.cs:168-239` → Biz `Master.cs:1299-1385`, `:744-894`, `:27-91`

#### Bảng tham số Request

Payload JSON gửi qua Request Body (`MstProvinceUpdateReq`):

| Tên tham số | Kiểu dữ liệu | Bắt buộc | Mặc định | Ý nghĩa & Quy tắc |
|---|---|---|---|---|
| `ProvinceCode` | `string?` | Có | `null` | Mã tỉnh/thành cần cập nhật. Chuẩn hóa qua `StdParam`. |
| `ProvinceName` | `string?` | Không | `null` | Tên tỉnh/thành mới. Chuẩn hóa qua `.Trim()`. |
| `FlagActive` | `object?` | Không | `"0"` | Trạng thái hoạt động. Chuẩn hóa qua `StdFlag`: Chỉ khi bằng `"1"` mới lưu `"1"`, mọi giá trị khác (kể cả thiếu hoặc null) **đều chuyển về `"0"`** (`Master.cs:804, :843`). |

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Kiểm tra Body rỗng: Nếu null hoặc rỗng → Lỗi `Fail("Mst_Province_Update")` (`:318-325`).
2. Cột cập nhật `Ft_Cols_Upd`: Gate cố định gửi `"Mst_Province.ProvinceName,Mst_Province.FlagActive,"` (`:338-341`), do đó luôn cập nhật cả tên và cờ hoạt động.
3. Mở một transaction duy nhất (`Master.cs:761`).
4. Chuỗi kiểm tra Guard theo thứ tự nguồn:
   - **Bước 1**: Kiểm tra tồn tại trong DB qua `Mst_Province_CheckDB` (cờ `"1"`) → Nếu không tìm thấy bản ghi → Lỗi `Mst_Province_CheckDB_ProvinceNotFound` (`:813-819, :53-63`).
   - **Bước 2**: Vì `bUpd_ProvinceName = true`, kiểm tra `strProvinceName.Length < 1` → Nếu rỗng → Lỗi `Mst_Province_Update_InvalidProvinceName` (`:822-832`).
5. Cập nhật dữ liệu:
   - `entity.ProvinceName = strProvinceName`.
   - `entity.FlagActive = strFlagActive` (thiếu FlagActive trong body sẽ bị đổi về `"0"`).
   - `entity.LogLUDTimeUTC = DateTime.UtcNow`, `entity.LogLUBy = "port"`.
6. Commit transaction và trả về `Ok(new { Success = true, Data = (object?)null })` (`:363-364`).

#### Bảng mã lỗi

| Mã lỗi (`ErridnLoyalty.*`) | HTTP Status | Tham số đi kèm | Điều kiện kích hoạt |
|---|---|---|---|
| `Mst_Province_Update` | 400 | `Check.strJson = null` | Request body rỗng hoặc null (`:318-325`). |
| `Mst_Province_CheckDB_ProvinceNotFound` | 400 | `Check.ProvinceCode` | Mã tỉnh/thành không tồn tại trong CSDL (`:813-819`). |
| `Mst_Province_Update_InvalidProvinceName` | 400 | `Check.strProvinceName` | Tên tỉnh/thành rỗng khi cập nhật (`:822-832`). |

#### Ví dụ Request / Response thực tế (từ lần chạy VERIFY #217)
- **Cập nhật thành công**:
  - Request: `{"ProvinceCode": "hn", "ProvinceName": "Hà Nội mới", "FlagActive": "1"}`
  - Response (HTTP 200): `{"success": true, "data": null}`.
- **Cập nhật thiếu `FlagActive` (bị đổi về `"0"`)**:
  - Request: `{"ProvinceCode": "DN", "ProvinceName": "Đà Nẵng"}`
  - Response (HTTP 200): Thành công, CSDL cập nhật `FlagActive = "0"`.
- **Cập nhật tên rỗng**:
  - Request: `{"ProvinceCode": "HCM", "ProvinceName": "", "FlagActive": "1"}`
  - Response (HTTP 400): `ErridnLoyalty.Mst_Province_Update_InvalidProvinceName`.

---

### 2.6. Delete (#218)

Xóa một tỉnh/thành phố khỏi danh mục.

- **Route**: `POST /api/mst_province/delete`
- **Controller Action**: [`Mst_ProvinceControllerPort.Delete`](../../Controllers/Port/Mst_ProvinceControllerPort.cs#L474-L525)
- **Tham chiếu nguồn**: Gate `Mst_ProvinceController.cs:377-437` → API `MstProvinceController.cs:242-313` → Biz `Master.cs:1387-1468`, `:895-1006`, `:27-91`

#### Bảng tham số Request

Payload JSON gửi qua Request Body (`MstProvinceDeleteReq`):

| Tên tham số | Kiểu dữ liệu | Bắt buộc | Mặc định | Ý nghĩa & Quy tắc |
|---|---|---|---|---|
| `ProvinceCode` | `string?` | Có | `null` | Mã tỉnh/thành cần xóa (`:377, :401`). Chuẩn hóa qua `StdParam`. |

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Kiểm tra mã rỗng tại tầng Gate: Nếu `string.IsNullOrEmpty(ProvinceCode)` → Ném `ServiceException("invalid_provincecode", "invalid_provincecode")` (**chữ thường nguyên văn nguồn**, `:421-423`) → Trả về HTTP 400 `ErridnLoyalty.invalid_provincecode`.
2. Mở một transaction duy nhất (`Master.cs:912`).
3. Chuẩn hóa: `strProvinceCode = StdParam(req.ProvinceCode)`.
4. Kiểm tra tồn tại trong DB qua `Mst_Province_CheckDB` (cờ `"1"`) → Nếu không tìm thấy → Lỗi `Mst_Province_CheckDB_ProvinceNotFound` (`:944-949, :53-63`).
5. Ràng buộc khóa ngoại: Không có bảng nào trong toàn bộ DDL nguồn tham chiếu khóa ngoại tới `Mst_Province` (không chặn xóa bảng con).
6. Xóa bản ghi khỏi bảng `Mst_Province` (`:958-964`).
7. Commit transaction và trả về `Ok(new { Success = true, Data = (object?)null })` (`:418-419`).

#### Bảng mã lỗi

| Mã lỗi (`ErridnLoyalty.*`) | HTTP Status | Tham số đi kèm | Điều kiện kích hoạt |
|---|---|---|---|
| `invalid_provincecode` | 400 | `Check.ProvinceCode` | Mã tỉnh/thành rỗng hoặc khoảng trắng tại gate (`:421-423`). |
| `Mst_Province_CheckDB_ProvinceNotFound` | 400 | `Check.ProvinceCode` | Mã tỉnh/thành không tồn tại trong CSDL (`:944-949`). |

#### Ví dụ Request / Response thực tế (từ lần chạy VERIFY #218)
- **Xóa thành công**:
  - Request: `{"ProvinceCode": "ct"}`
  - Response (HTTP 200): `{"success": true, "data": null}`. Bản ghi `CT` bị xóa, `ItemCount` giảm từ 5 xuống 4.
- **Xóa mã không tồn tại**:
  - Request: `{"ProvinceCode": "XX"}`
  - Response (HTTP 400): `ErridnLoyalty.Mst_Province_CheckDB_ProvinceNotFound`.
- **Mã rỗng**:
  - Request: `{"ProvinceCode": ""}` hoặc `{}`
  - Response (HTTP 400): `ErridnLoyalty.invalid_provincecode`.

---

### 2.7. Import (#219)

Nhập danh mục tỉnh/thành phố hàng loạt từ tập tin bảng tính Excel `.xlsx`.

- **Route**: `POST /api/mst_province/import`
- **Content-Type**: `multipart/form-data`
- **Controller Action**: [`Mst_ProvinceControllerPort.Import`](../../Controllers/Port/Mst_ProvinceControllerPort.cs#L565-L666)
- **Tham chiếu nguồn**: Gate `Mst_ProvinceController.cs:441-592` → API `WA_Mst_Province_Create` (`MstProvinceController.cs:94-165`) → Biz `Mst_Province_Create`

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Kiểm tra tập tin upload (`:465-470`):
   - Không có file, file rỗng, hoặc phần mở rộng khác `.xlsx` → Trả về HTTP 400 `ErridnLoyalty.Mst_Province_Import` kèm `Message = "File excel import không hợp lệ"`.
2. Đọc nội dung qua [`PortExcel.ReadSheet`](../../Data/PortExcel.cs):
   - Nếu bảng rỗng (0 dòng dữ liệu) → Trả về HTTP 400 kèm `Message = "File excel import không có dữ liệu"` (`:482`).
   - Nếu số cột khác 2 (`GetImportDicColumsTemplate().Count != 2`) → Trả về HTTP 400 kèm `Message = "File excel import không hợp lệ"` (`:484-488`).
3. **Kiểm tra Validate toàn bộ dòng TRƯỚC khi tạo** (`:491-510`):
   - Duyệt qua tất cả các dòng:
     - `ProvinceCode` rỗng → Trả về HTTP 400 kèm `Message = "Mã tỉnh/thành không được trống"` (`:496`).
     - `ProvinceName` rỗng → Trả về HTTP 400 kèm `Message = "Tên quận/huyện không được trống"` (`:501`, **nguồn viết nhầm "quận/huyện" cho tỉnh — giữ nguyên văn**).
   - Nếu có bất kỳ dòng nào vi phạm, dừng ngay lập tức và **không có bản ghi nào được lưu vào CSDL**.
4. **Lưu từng dòng (Hành vi Nhập một phần — Partial Import)** (`:539-563`):
   - Duyệt tuần tự từng dòng dữ liệu, mỗi dòng thực thi trong **1 transaction riêng biệt** qua `CreateXAsync`.
   - Nếu dòng thứ `k` bị lỗi (ví dụ: trùng mã `Mst_Province_CheckDB_ProvinceExist`), vòng lặp dừng ngay và trả về lỗi của dòng `k`.
   - **Các dòng trước `k` đã được commit thành công vào CSDL**, trong khi các dòng sau `k` hoàn toàn chưa được xử lý.
5. Thành công: Trả về `Ok(new { Success = true, Data = (object?)null })` (`:572-573`).

#### Bảng mã lỗi

| Mã lỗi (`ErridnLoyalty.*`) | HTTP Status | Tham số đi kèm | Điều kiện kích hoạt |
|---|---|---|---|
| `Mst_Province_Import` | 400 | `Message = "File excel import không hợp lệ"` | Không có file, file rỗng, sai đuôi `.xlsx` hoặc số cột khác 2. |
| `Mst_Province_Import` | 400 | `Message = "File excel import không có dữ liệu"` | File chỉ có tiêu đề, không có dòng dữ liệu. |
| `Mst_Province_Import` | 400 | `Message = "Mã tỉnh/thành không được trống"` | Có dòng bị rỗng mã tỉnh/thành. |
| `Mst_Province_Import` | 400 | `Message = "Tên quận/huyện không được trống"` | Có dòng bị rỗng tên tỉnh/thành (giữ nguyên typo nguồn). |
| `Mst_Province_CheckDB_ProvinceExist` | 400 | `Check.ProvinceCode` | Dòng thứ `k` bị trùng mã đã có trong CSDL (dừng tại dòng `k`). |

#### Minh chứng Kiểm thử Thực tế (từ lần chạy VERIFY #219)
- **Nhập file hợp lệ**: File gồm 2 dòng `[QN, Quảng Ninh]` và `[BN, Bắc Ninh]`. HTTP 200, `ItemCount` tăng từ 5 lên 7.
- **Minh chứng hành vi Partial Import**: File gồm 3 dòng: `[LC, Lào Cai]` (mới), `[hn, Hà Nội]` (trùng seed), `[YB, Yên Bái]` (mới).
  - Kết quả: HTTP 400 `ErridnLoyalty.Mst_Province_CheckDB_ProvinceExist`.
  - CSDL: `LC` **đã được tạo thành công** (`ItemCount` từ 7 lên 8), còn `YB` **hoàn toàn không được tạo**.

---

### 2.8. Export (#220)

Xuất danh sách tỉnh/thành phố ra tập tin Excel `.xlsx`.

- **Route**: `POST /api/mst_province/export`
- **Controller Action**: [`Mst_ProvinceControllerPort.Export`](../../Controllers/Port/Mst_ProvinceControllerPort.cs#L676-L720)
- **Tham chiếu nguồn**: Gate `Mst_ProvinceController.cs:597-675` → API `WA_Mst_Province_Get` → Biz `Master.Loyalty.cs:442-488`

#### Bảng tham số Request

Payload JSON gửi qua Request Body (`MstProvinceExportReq`):

| Tên tham số | Kiểu dữ liệu | Bắt buộc | Mặc định | Ý nghĩa & Quy tắc |
|---|---|---|---|---|
| `OrgID` | `string?` | Không | `null` | Nhận tại gate nhưng không tham gia lọc (`:735-759`). |
| `KeyWord` | `string?` | Không | `null` | Lọc theo từ khóa tên tỉnh/thành (`ProvinceName LIKE '%kw%'`). |
| `FlagActive` | `string?` | Không | `null` | Lọc theo cờ hoạt động (toán tử `IN`). |

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Gọi `GetXAsync(r.KeyWord, r.FlagActive, 0, 123456000, null)` để lấy dữ liệu (`:636-637`).
2. Kiểm tra dữ liệu rỗng: Nếu danh sách trả về rỗng, ném `ServiceException("Không có dữ liệu")` (`:661`) → Trả về HTTP 400 `ErridnLoyalty.Mst_Province_Export` kèm `Message = "Không có dữ liệu"`.
3. Từ điển cột: Sử dụng `GetImportDicColums()` gồm đúng 3 cột (`:647, :714-722`):
   - `ProvinceCode`: `"Mã tỉnh/thành (*)"`
   - `ProvinceName`: `"Tên quận/huyện"` (**typo nguồn — giữ nguyên**)
   - `FlagActive`: `"Trạng thái"`
4. Sinh file qua [`PortExcel.ExportToExcelFromList`](../../Data/PortExcel.cs), đặt tên sheet `"Mst_Province"`.
5. Trả về `Ok(new { Success = true, Data = new { Url = baseAddress + virtualPath } })` (`:652-656`).

#### Bảng mã lỗi

| Mã lỗi (`ErridnLoyalty.*`) | HTTP Status | Tham số đi kèm | Điều kiện kích hoạt |
|---|---|---|---|
| `Mst_Province_Export` | 400 | `Message = "Không có dữ liệu"` | Không có bản ghi nào thỏa mãn điều kiện lọc (`:661`). |

---

### 2.9. ExportTemplate (#221)

Xuất tập tin mẫu Excel dùng cho chức năng Import tỉnh/thành phố.

- **Route**: `POST /api/mst_province/exporttemplate`
- **Controller Action**: [`Mst_ProvinceControllerPort.ExportTemplate`](../../Controllers/Port/Mst_ProvinceControllerPort.cs#L729-L748)
- **Tham chiếu nguồn**: Gate `Mst_ProvinceController.cs:678-710`

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Không nhận body, không kiểm tra quyền, không đọc CSDL (`:686`).
2. Khởi tạo danh sách rỗng `List<Mst_Province>`.
3. Từ điển cột: `GetImportDicColumsTemplate()` gồm đúng 2 cột (`:688, :724-731`):
   - `ProvinceCode`: `"Mã tỉnh/thành (*)"`
   - `ProvinceName`: `"Tên quận/huyện"` (**typo nguồn — giữ nguyên**)
4. Sinh file Excel rỗng chỉ có dòng tiêu đề qua [`PortExcel.ExportToExcelFromList`](../../Data/PortExcel.cs).
5. Trả về `Ok(new { Success = true, Data = new { Url = baseAddress + virtualPath } })` (`:692-696`).

#### Bảng mã lỗi
Endpoint luôn trả về HTTP 200.

---

## 3. Khác biệt So với Nguồn & Danh sách GAP (Known Gaps)

| Hạng mục | Trạng thái ở Nguồn | Triển khai ở Đích | Phân loại & Ghi chú GAP |
|---|---|---|---|
| **Xác thực & Phân quyền** | Gate kiểm tra `CheckGatewayAuthentication`, `CheckAuthorize`; Biz kiểm tra `Sys_User_CheckAuthorize`. | Bỏ qua toàn bộ kiểm tra phân quyền do hệ thống đích chưa có phân hệ User/Role. | Bỏ qua có chủ đích (**Loại Đ**). |
| **Ghi nhật ký hệ thống** | Ghi bản ghi `RequestLog` và `ReturnLog` vào CSDL cho mọi thao tác. | Bỏ qua ghi log DB. | Bỏ qua có chủ đích (**Loại Đ**). |
| **Tài khoản cập nhật (`LogLUBy`)** | Trích xuất từ phiên người dùng đăng nhập. | Gán cố định `"port"` (hoặc `"seed"` trong Seeder). | **GAP**: Chưa có session người dùng. |
| **Mã mạng (`NetworkID`)** | Lấy từ `orgUserUI.NetworkId`. | Đọc từ cấu hình `IConfiguration["Biz_NetworkID"]` (mặc định `"1001"`). | Khớp quy ước hệ thống đích. |
| **Khai báo DDL nguồn** | Không có tập tin `.sql` tạo bảng `Mst_Province` trong repo gốc. | Cấu hình bảng qua EF Core, khóa chính `ProvinceCode` là khóa draft. | **GAP / Cần xác nhận**: Cần đối chiếu với DDL production của nguồn. |
| **Typo tiêu đề cột `ProvinceName`** | Nguồn ghi `"Tên quận/huyện"` cho `ProvinceName` trong Template và Export. | Giữ nguyên văn typo `"Tên quận/huyện"` để bảo đảm tương thích 1-1. | Khớp nguồn 1-1. |
| **Lỗi xóa `invalid_provincecode`** | Ném exception chuỗi chữ thường `"invalid_provincecode"`. | Giữ nguyên mã lỗi chữ thường `ErridnLoyalty.invalid_provincecode`. | Khớp nguồn 1-1. |

---

## 4. Nhật ký Thay đổi & Đối chiếu Tiến độ (Changelog)

| STT Hạng mục | Tên Endpoint & Action | Trạng thái | Mã Commit Git | Mô tả tóm tắt thay đổi |
|---|---|---|---|---|
| **#213** | `Mst_ProvinceController/Search` | Đã hoàn thành | `77773a7` | Khởi tạo model [`Mst_Province.cs`](../../Models/Port/Mst_Province.cs), cấu hình EF Core, Seeder 5 bản ghi và action tìm kiếm phân trang. |
| **#214** | `Mst_ProvinceController/GetByProvinceCode` | Đã hoàn thành | `d1a9bb8` | Tra cứu chi tiết một tỉnh/thành theo mã, không lọc trạng thái hoạt động. |
| **#215** | `Mst_ProvinceController/GetAllActive` | Đã hoàn thành | `578190e` | Lấy toàn bộ danh sách tỉnh/thành đang hoạt động (`FlagActive = '1'`), bọc trong đối tượng `Lst_Mst_Province`. |
| **#216** | `Mst_ProvinceController/Create` | Đã hoàn thành | `cfc0f9c` | Thêm mới tỉnh/thành, kiểm tra mã rỗng, trùng mã và tên rỗng theo thứ tự. |
| **#217** | `Mst_ProvinceController/Update` | Đã hoàn thành | `bff717b` | Cập nhật tên và cờ hoạt động, xử lý tự động hạ cờ về `'0'` nếu thiếu `FlagActive`. |
| **#218** | `Mst_ProvinceController/Delete` | Đã hoàn thành | `901234a` | Xóa đơn lẻ tỉnh/thành, phát sinh mã lỗi chữ thường `invalid_provincecode`. |
| **#219** | `Mst_ProvinceController/Import` | Đã hoàn thành | `1a50d6b` | Nhập danh mục từ Excel, validate toàn bộ dòng trước khi tạo và thực thi cơ chế nhập một phần. |
| **#220** | `Mst_ProvinceController/Export` | Đã hoàn thành | `41f304b` | Xuất danh sách tỉnh/thành ra Excel, sử dụng từ điển 3 cột, báo lỗi khi không có dữ liệu. |
| **#221** | `Mst_ProvinceController/ExportTemplate` | Đã hoàn thành | `47f6ddf` | Xuất file mẫu Excel gồm 2 cột tiêu đề, không truy vấn CSDL. |
