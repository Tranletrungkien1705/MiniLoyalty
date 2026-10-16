# Tham chiếu API Mst_DistrictController (Port 1-1)

| Thông tin | Giá trị |
|---|---|
| **Phạm vi** | Tài liệu tham chiếu chi tiết 10 endpoint đã port 1-1 của Mst_DistrictController (#139 - #148) |
| **Cập nhật** | 2026-10-16 |
| **Phiên bản mã** | `93b6ace` |
| **Trạng thái** | Nháp — chờ review |

---

## 1. Tóm tắt Nghiệp vụ & Cấu trúc Dữ liệu

### 1.1. Mục đích Nghiệp vụ
API `Mst_DistrictController` quản lý danh mục quận/huyện (`Mst_District`) trực thuộc các tỉnh/thành phố trong hệ thống MiniLoyalty. Module hỗ trợ tìm kiếm phân trang theo từ khóa và trạng thái, tra cứu chi tiết quận/huyện theo khóa ghép (tỉnh, huyện), lấy danh sách quận/huyện theo tỉnh/thành, lấy tất cả quận/huyện đang hoạt động, thêm mới (có kiểm tra ràng buộc tỉnh cha), cập nhật tên/trạng thái, xóa đơn lẻ theo cặp khóa ghép, nhập dữ liệu từ Excel và xuất file dữ liệu/mẫu chuẩn.

### 1.2. Bảng Đối chiếu Nguồn Tra cứu (Source Code Mapping)

| Thành phần | Đường dẫn tập tin nguồn (`2022.7.Loyalty`) | Tập tin đích tương ứng (`MiniLoyalty`) |
|---|---|---|
| **Gate Controller** | `13.ClientGate\V10\idn.Loyalty.WebAPI\Controllers\Mst_DistrictController.cs:31-791` | [`Controllers/Port/Mst_DistrictControllerPort.cs`](../../Controllers/Port/Mst_DistrictControllerPort.cs) |
| **API Backend** | `11.BackEnd\V10\idn.Loyalty.Biz.Web\Controllers\MstDistrictController.cs:20-313` | Tích hợp trực tiếp vào [`Controllers/Port/Mst_DistrictControllerPort.cs`](../../Controllers/Port/Mst_DistrictControllerPort.cs) |
| **Nghiệp vụ (Biz)** | `11.BackEnd\V10\idn.Loyalty.Biz\Master.cs` (Select SQL `:1801-2023`, `:2584-2708`; Create `:2024-2193`, `:2710-2794`; Update `:2194-2359`, `:2796-2883`; Delete `:2360-2475`, `:2885-2968`; CheckDB `:1729-1799`; Province CheckDB `:27-91`) | Tích hợp trực tiếp vào [`Controllers/Port/Mst_DistrictControllerPort.cs`](../../Controllers/Port/Mst_DistrictControllerPort.cs) |
| **Entity Model** | `12.Dev.Common\idn.Loyalty.Common\Models\Mst_District.cs:9-24` | [`Models/Port/Mst_District.cs:29-38`](../../Models/Port/Mst_District.cs#L29-L38) |
| **Seeder Demo** | `Order => 16` (danh mục `Mst_*` dùng 10–99) | [`Models/Port/Mst_District.cs:58-76`](../../Models/Port/Mst_District.cs#L58-L76) |

### 1.3. Cấu trúc Bảng CSDL (`Mst_District`)

Bảng dữ liệu vật lý `Mst_District` gồm đúng 7 cột. Khóa chính thực tế của bảng là **Khóa ghép (Composite Key)** gồm hai cột `(ProvinceCode, DistrictCode)` (xác nhận qua `Master.cs:1744-1745`, cho phép cùng một mã quận/huyện tồn tại độc lập ở hai tỉnh khác nhau):

| Tên cột | Kiểu EF Core | Khóa | Cho phép NULL | Ghi chú & Ý nghĩa |
|---|---|---|---|---|
| `ProvinceCode` | `string` | **PK (ghép)** | Không | Mã tỉnh/thành phố trực thuộc. Khóa chính ghép phần 1. |
| `DistrictCode` | `string` | **PK (ghép)** | Không | Mã quận/huyện. Khóa chính ghép phần 2. |
| `NetworkID` | `string?` | | Có | Mã mạng nghiệp vụ. Ở đích lấy từ cấu hình `Biz_NetworkID` (mặc định `"1001"`). |
| `DistrictName` | `string?` | | Có | Tên quận/huyện. |
| `FlagActive` | `string?` | | Có | Cờ trạng thái hoạt động: `"1"` (Đang hoạt động), `"0"` (Ngừng hoạt động). |
| `LogLUDTimeUTC` | `DateTime?` | | Có | Thời điểm cập nhật cuối cùng theo chuẩn UTC. |
| `LogLUBy` | `string?` | | Có | Tài khoản thực hiện cập nhật cuối cùng (mặc định gán `"port"` ở đích). |

**Dữ liệu mẫu khởi tạo (Seeder - `Mst_DistrictSeeder`, Idempotent)**:
- Gồm 5 bản ghi demo: `HN01` (Ba Đình, HN, `'1'`), `HN02` (Hoàn Kiếm, HN, `'1'`), `HCM01` (Quận 1, HCM, `'1'`), `DN01` (Hải Châu, DN, `'1'`), `CT01` (Ninh Kiều, CT, `'0'`).
- Toàn bộ bản ghi gán `NetworkID = "6403834000"`, `LogLUBy = "seed"`.

---

## 2. Đặc tả Chi tiết Từng Endpoint API

---

### 2.1. Search (#139)

Tìm kiếm danh mục quận/huyện phân trang theo từ khóa và trạng thái hoạt động.

- **Route**: `POST /api/mst_district/search`
- **Controller Action**: [`Mst_DistrictControllerPort.Search`](../../Controllers/Port/Mst_DistrictControllerPort.cs#L163-L188)
- **Tham chiếu nguồn**: Gate `Mst_DistrictController.cs:31-111` → API `MstDistrictController.cs:20-91` → Biz `Master.cs:1883-1929`

#### Bảng tham số Request

Payload JSON gửi qua Request Body (`MstDistrictSearchReq`), hỗ trợ body rỗng hoàn toàn:

| Tên tham số | Kiểu dữ liệu | Bắt buộc | Mặc định | Ý nghĩa & Quy tắc |
|---|---|---|---|---|
| `OrgID` | `string?` | Không | `null` | Nhận tại gate (`:31, :61`) nhưng mệnh đề lọc `strWhereClause_Mst_District` (`:818-833`) **không sử dụng**. |
| `KeyWord` | `string?` | Không | `null` | Từ khóa tìm kiếm theo tên quận/huyện (`DistrictName LIKE '%kw%'`, `:823-826`). |
| `FlagActive` | `string?` | Không | `null` | Cờ trạng thái hoạt động. **Áp dụng toán tử `=`** (`:827-830`) — KHÁC `Mst_Province` dùng `IN` (chuỗi `"0,1"` không khớp dòng nào). |
| `Ft_PageIndex` | `int` | Không | `0` | Chỉ số trang cần lấy (0-based). Nếu `< 0` tự động gán về `0`. |
| `Ft_PageSize` | `int` | Không | `10` | Số dòng trên mỗi trang. Nếu `<= 0` trả về mảng rỗng `DataList = []`. |

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Chuẩn hóa phân trang: `pageIndex = Math.Max(0, r.Ft_PageIndex)`, `pageSize = r.Ft_PageSize`.
2. Tính vị trí bản ghi: `start = (long)pageIndex * pageSize`.
3. Xây dựng truy vấn qua `GetXAsync` ([`Mst_DistrictControllerPort.cs:961-1006`](../../Controllers/Port/Mst_DistrictControllerPort.cs#L961-L1006)):
   - **KeyWord**: Lọc `EF.Functions.Like((x.DistrictName ?? "").ToLower(), PortQuery.Contains(keyWord), PortQuery.Esc)`.
   - **FlagActive**: Lọc `EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal(flagActive), PortQuery.Esc)` (toán tử `=` của nguồn `:827-830`).
   - **OrgID**: Bỏ qua.
4. Đếm tổng số bản ghi: `itemCount = await ordered.CountAsync()` (`Master.cs:1925`).
5. Sắp xếp thứ tự: Phân trang theo `md.DistrictCode ASC` (`:1890`), select cuối sắp theo `t.MyIdxSeq ASC` (`:1929`). Không join bảng khác (`:1923-1929`).
6. Trích xuất trang: Nếu `pageSize <= 0` trả về `DataList = []`; nếu `pageSize > 0` gọi `.Skip((int)start).Take((int)pageSize)`.
7. Gán `MyIdxSeq = start + i` (0-based).
8. Tính `PageCount`: Nếu `dataList.Count == 0`, `PageCount = 0`; ngược lại `PageCount = ceil(itemCount / ps)`.

#### Bảng mã lỗi
Endpoint không phát sinh lỗi nghiệp vụ; luôn trả về HTTP 200.

#### Ví dụ Request / Response thực tế (từ lần chạy VERIFY #139)
- **Tìm kiếm mặc định (`{}`)**:
  - Response (HTTP 200): `itemCount = 5`, `pageCount = 1`, `dataList = [CT01#0, DN01#1, HCM01#2, HN01#3, HN02#4]`.
- **Lọc theo `FlagActive = "1"`**:
  - Request: `{"FlagActive": "1"}`
  - Response (HTTP 200): `itemCount = 4`, `pageCount = 1`, `dataList = [DN01, HCM01, HN01, HN02]` (loại bỏ `CT01`).
- **Lọc chuỗi `"0,1"` (toán tử `=`)**:
  - Request: `{"FlagActive": "0,1"}`
  - Response (HTTP 200): `itemCount = 0`, `pageCount = 0`, `dataList = []` (đúng hành vi nguồn, khác Province).

---

### 2.2. GetDetail (#140)

Tra cứu chi tiết một quận/huyện theo cặp khóa ghép `(ProvinceCode, DistrictCode)`.

- **Route**: `POST /api/mst_district/getdetail`
- **Controller Action**: [`Mst_DistrictControllerPort.GetDetail`](../../Controllers/Port/Mst_DistrictControllerPort.cs#L196-L220)
- **Tham chiếu nguồn**: Gate `Mst_DistrictController.cs:113-177` → API `MstDistrictController.cs:20-91` → Biz `Master.cs:1883-1929`

#### Bảng tham số Request

Payload JSON gửi qua Request Body (`MstDistrictGetDetailReq`):

| Tên tham số | Kiểu dữ liệu | Bắt buộc | Mặc định | Ý nghĩa & Quy tắc |
|---|---|---|---|---|
| `ProvinceCode` | `string?` | Có | `null` | Mã tỉnh/thành (`:113, :137`). |
| `DistrictCode` | `string?` | Có | `null` | Mã quận/huyện (`:113, :138`). |

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. **Kiểm tra điều kiện tiên quyết nguồn (`:133-134`)**: Chỉ truy vấn khi **CẢ HAI** `ProvinceCode` và `DistrictCode` đều có giá trị khác rỗng. Nếu thiếu một trong hai hoặc cả hai rỗng → Trả về ngay `Ok(new { Success = true, Data = (object?)null })` (`:132, :164`).
2. Mệnh đề lọc: `strWhereClause_Mst_District_Detail` (`:835-863`) lọc `ProvinceCode =` và `DistrictCode =`.
3. **Cờ hoạt động không lọc**: Dòng lọc `FlagActive` bị comment trong mã nguồn gốc (`:848-851`) nên quận/huyện có `FlagActive = '0'` vẫn tra cứu được bình thường.
4. Lấy 1 dòng (`Ft_RecordStart = "0"`, `Ft_RecordCount = "1"`, `:151-152`). Trả về `Data = rows[0]` hoặc `null` nếu không tìm thấy (`:158-161`).

#### Bảng mã lỗi
Endpoint luôn trả về HTTP 200.

#### Ví dụ Request / Response thực tế (từ lần chạy VERIFY #140)
- **Tra cứu hợp lệ**:
  - Request: `{"ProvinceCode": "hn", "DistrictCode": "hn02"}`
  - Response (HTTP 200): `{"success": true, "data": {"myIdxSeq": 0, "districtCode": "HN02", "provinceCode": "HN", "districtName": "Hoàn Kiếm", "flagActive": "1", ...}}`.
- **Tra cứu quận có `FlagActive = "0"`**:
  - Request: `{"ProvinceCode": "CT", "DistrictCode": "CT01"}`
  - Response (HTTP 200): Trả về bản ghi `CT01` với `flagActive = "0"`.
- **Thiếu tỉnh hoặc mã không tồn tại**:
  - Request: `{"DistrictCode": "HN01"}` hoặc `{"ProvinceCode": "DN", "DistrictCode": "HN01"}`
  - Response (HTTP 200): `{"success": true, "data": null}`.

---

### 2.3. GetByProvinceCode (#141)

Lấy danh sách tất cả quận/huyện thuộc một tỉnh/thành phố.

- **Route**: `POST /api/mst_district/getbyprovincecode`
- **Controller Action**: [`Mst_DistrictControllerPort.GetByProvinceCode`](../../Controllers/Port/Mst_DistrictControllerPort.cs#L227-L245)
- **Tham chiếu nguồn**: Gate `Mst_DistrictController.cs:179-250` → API `MstDistrictController.cs:20-91` → Biz `Master.cs:1883-1929`

#### Bảng tham số Request

Payload JSON gửi qua Request Body (`MstDistrictGetByProvinceCodeReq`):

| Tên tham số | Kiểu dữ liệu | Bắt buộc | Mặc định | Ý nghĩa & Quy tắc |
|---|---|---|---|---|
| `ProvinceCode` | `string?` | Có | `null` | Mã tỉnh/thành cần lấy danh sách quận/huyện (`:179, :199`). |
| `FlagActive` | `string?` | Không | `null` | Cờ hoạt động (`:179, :201-208`). Chú ý: Gate gán `"1"` nếu rỗng nhưng hàm lọc nguồn comment dòng lọc cờ nên tham số này **không có tác dụng**. |

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Kiểm tra mã tỉnh: Nếu `string.IsNullOrWhiteSpace(req?.ProvinceCode)`, trả về ngay `Ok(new { Success = true, Data = new { Lst_Mst_District = (object?)null } })` (`:198, :234-237`).
2. Mệnh đề lọc: Gọi `strWhereClause_Mst_District_Detail` (`:835-863`) chỉ với `ProvinceCode =`. Dòng `FlagActive` bị comment nên quận/huyện ngừng hoạt động vẫn được trả về.
3. Phân trang nguồn: `Ft_RecordStart = "0"`, `Ft_RecordCount = "123456000"` (`:221-222`).
4. **Cấu trúc dữ liệu trả về**: Bọc trong đối tượng `{ Lst_Mst_District = rows.Count > 0 ? rows : null }` (`:228-237`).

#### Bảng mã lỗi
Endpoint luôn trả về HTTP 200.

#### Ví dụ Request / Response thực tế (từ lần chạy VERIFY #141)
- **Lấy danh sách quận của Hà Nội**:
  - Request: `{"ProvinceCode": "hn"}`
  - Response (HTTP 200): `{"success": true, "data": {"lst_Mst_District": [HN01#0, HN02#1]}}`.
- **Lấy danh sách tỉnh Cần Thơ (chứa quận ngừng hoạt động `CT01`)**:
  - Request: `{"ProvinceCode": "CT"}` hoặc `{"ProvinceCode": "CT", "FlagActive": "1"}`
  - Response (HTTP 200): Vẫn trả về `CT01` (FlagActive `'0'`) do nguồn không lọc cờ.
- **Mã tỉnh rỗng**:
  - Request: `{}` hoặc `{"ProvinceCode": ""}`
  - Response (HTTP 200): `{"success": true, "data": {"lst_Mst_District": null}}`.

---

### 2.4. GetAllActive (#142)

Lấy danh sách tất cả quận/huyện đang hoạt động (`FlagActive = '1'`) trên toàn hệ thống.

- **Route**: `POST /api/mst_district/getallactive`
- **Controller Action**: [`Mst_DistrictControllerPort.GetAllActive`](../../Controllers/Port/Mst_DistrictControllerPort.cs#L257-L267)
- **Tham chiếu nguồn**: Gate `Mst_DistrictController.cs:252-315` → API `MstDistrictController.cs:20-91` → Biz `Master.cs:1883-1929`

#### Bảng tham số Request
Endpoint không nhận request body (`:257`).

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Tham số mặc định nguồn: `Ft_RecordStart = "0"`, `Ft_RecordCount = "123456000"` (`:287-288`).
2. Mệnh đề lọc: Gọi `strWhereClause_Mst_District` với `FlagActive = "1"` (`:272-275`). **Lọc `FlagActive = '1'` có tác dụng** (khác `GetByProvinceCode`).
3. **Cấu trúc dữ liệu trả về (Đặc thù nguồn)**:
   - Trả về **mảng phẳng (flat array)** `List<Mst_District>` trực tiếp trong thuộc tính `Data` (`:298-299`).
   - **KHÁC** `Mst_Province` (bọc `{ Lst_Mst_Province }`) và **KHÁC** `GetByProvinceCode` (bọc `{ Lst_Mst_District }`).
   - Nếu rỗng, `Data = null`.

#### Bảng mã lỗi
Endpoint luôn trả về HTTP 200.

#### Ví dụ Request / Response thực tế (từ lần chạy VERIFY #142)
- Request: `POST /api/mst_district/getallactive`
- Response (HTTP 200):
  ```json
  {
    "success": true,
    "data": [
      { "myIdxSeq": 0, "districtCode": "DN01", "provinceCode": "DN", "districtName": "Hải Châu", "flagActive": "1", ... },
      { "myIdxSeq": 1, "districtCode": "HCM01", "provinceCode": "HCM", "districtName": "Quận 1", "flagActive": "1", ... },
      { "myIdxSeq": 2, "districtCode": "HN01", "provinceCode": "HN", "districtName": "Ba Đình", "flagActive": "1", ... },
      { "myIdxSeq": 3, "districtCode": "HN02", "provinceCode": "HN", "districtName": "Hoàn Kiếm", "flagActive": "1", ... }
    ]
  }
  ```

---

### 2.5. Create (#143)

Thêm mới một quận/huyện vào danh mục, kèm kiểm tra ràng buộc tỉnh cha.

- **Route**: `POST /api/mst_district/create`
- **Controller Action**: [`Mst_DistrictControllerPort.Create`](../../Controllers/Port/Mst_DistrictControllerPort.cs#L298-L324)
- **Tham chiếu nguồn**: Gate `Mst_DistrictController.cs:316-376` → API `MstDistrictController.cs:94-165` → Biz `Master.cs:2710-2794`, `:2024-2193`, `:1729-1799`, `:27-91`

#### Bảng tham số Request

Payload JSON gửi qua Request Body (`MstDistrictCreateReq`):

| Tên tham số | Kiểu dữ liệu | Bắt buộc | Mặc định | Ý nghĩa & Quy tắc |
|---|---|---|---|---|
| `DistrictCode` | `string?` | Có | `null` | Mã quận/huyện. Chuẩn hóa qua `StdParam`. |
| `ProvinceCode` | `string?` | Có | `null` | Mã tỉnh/thành phố trực thuộc. Chuẩn hóa qua `StdParam`. |
| `DistrictName` | `string?` | Có | `null` | Tên quận/huyện. Chuẩn hóa qua `.Trim()`. |

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Kiểm tra Body rỗng: Nếu null hoặc rỗng → Lỗi `Fail("Mst_District_Create")` (`:325-332`).
2. Mở một transaction duy nhất (`Master.cs:2041 _cf.db.BeginTransaction()`).
3. Chuẩn hóa chuỗi: `strDistrictCode = StdParam(req.DistrictCode)`, `strProvinceCode = StdParam(req.ProvinceCode)`, `strDistrictName = string.Format("{0}", req.DistrictName).Trim()`.
4. **Chuỗi kiểm tra Guard 4 bước theo đúng thứ tự nguồn**:
   - **Bước 1**: Nếu `strDistrictCode == null || strDistrictCode.Length < 1` → Lỗi `Mst_District_Create_InvalidDistrictCode` (`:2086-2095`).
   - **Bước 2**: Kiểm tra tồn tại trong DB theo **CẶP KHÓA GHÉP `(ProvinceCode, DistrictCode)`** qua `Mst_District_CheckDB(strProvinceCode, strDistrictCode, Flag "0")` → Nếu đã có bản ghi → Lỗi `Mst_District_CheckDB_DistrictExist` (`:2097-2104, :1770-1781`).
   - **Bước 3**: Kiểm tra tỉnh cha qua `Mst_Province_CheckDB(strProvinceCode, Flag "1", ActiveList "1")`:
     - Nếu không tìm thấy tỉnh cha trong CSDL → Lỗi `Mst_Province_CheckDB_ProvinceNotFound` (`:2108-2114, :53-63`).
     - Nếu tỉnh cha có `FlagActive != '1'` (tỉnh đang ngừng hoạt động) → Lỗi `Mst_Province_CheckDB_FlagActiveNotMatched` (`:78-89`).
   - **Bước 4**: Nếu `strDistrictName.Length < 1` → Lỗi `Mst_District_Create_InvalidDistrictName` (`:2116-2126`).
5. Ghi dữ liệu 7 cột: `DistrictCode`, `ProvinceCode`, `DistrictName`, `NetworkID = config["Biz_NetworkID"] ?? "1001"`, `FlagActive = "1"`, `LogLUDTimeUTC = DateTime.UtcNow`, `LogLUBy = "port"`.
6. Commit transaction và trả về `Ok(new { Success = true, Data = (object?)null })` (`:362-363`).

#### Bảng mã lỗi

| Mã lỗi (`ErridnLoyalty.*`) | HTTP Status | Tham số đi kèm | Điều kiện kích hoạt |
|---|---|---|---|
| `Mst_District_Create` | 400 | `Check.strJson = null` | Request body rỗng hoặc null (`:325-332`). |
| `Mst_District_Create_InvalidDistrictCode` | 400 | `Check.strDistrictCode` | Mã quận/huyện rỗng hoặc null (`:2086-2095`). |
| `Mst_District_CheckDB_DistrictExist` | 400 | `Check.ProvinceCode`, `Check.DistrictCode` | Trùng cặp (tỉnh, huyện) trong CSDL (`:2097-2104`). |
| `Mst_Province_CheckDB_ProvinceNotFound` | 400 | `Check.ProvinceCode` | Tỉnh/thành cha không tồn tại (`:2108-2114`). |
| `Mst_Province_CheckDB_FlagActiveNotMatched` | 400 | `Check.ProvinceCode` | Tỉnh/thành cha đang ngừng hoạt động (`FlagActive = '0'`). |
| `Mst_District_Create_InvalidDistrictName` | 400 | `Check.strDistrictName` | Tên quận/huyện rỗng sau khi Trim (`:2116-2126`). |

#### Ví dụ Request / Response thực tế (từ lần chạy VERIFY #143)
- **Tạo hợp lệ**:
  - Request: `{"DistrictCode": "hn03", "ProvinceCode": "hn", "DistrictName": "  Đống Đa  "}`
  - Response (HTTP 200): `{"success": true, "data": null}`.
- **Trùng mã huyện nhưng khác tỉnh (Hợp lệ nhờ Khóa ghép)**:
  - Request: `{"DistrictCode": "HN01", "ProvinceCode": "DN", "DistrictName": "Huyện trùng mã"}`
  - Response (HTTP 200): Thành công tạo mới, do cặp `(DN, HN01)` chưa tồn tại.
- **Tỉnh cha ngừng hoạt động (`CT`)**:
  - Request: `{"DistrictCode": "CT02", "ProvinceCode": "CT", "DistrictName": "Cái Răng"}`
  - Response (HTTP 400): `ErridnLoyalty.Mst_Province_CheckDB_FlagActiveNotMatched`.

---

### 2.6. Update (#144)

Cập nhật tên và trạng thái hoạt động của quận/huyện.

- **Route**: `POST /api/mst_district/update`
- **Controller Action**: [`Mst_DistrictControllerPort.Update`](../../Controllers/Port/Mst_DistrictControllerPort.cs#L462-L541)
- **Tham chiếu nguồn**: Gate `Mst_DistrictController.cs:380-447` → API `MstDistrictController.cs:168-239` → Biz `Master.cs:2796-2883`, `:2194-2359`, `:1729-1799`

#### Bảng tham số Request

Payload JSON gửi qua Request Body (`MstDistrictUpdateReq`):

| Tên tham số | Kiểu dữ liệu | Bắt buộc | Mặc định | Ý nghĩa & Quy tắc |
|---|---|---|---|---|
| `ProvinceCode` | `string?` | Có | `null` | Mã tỉnh/thành (khóa ghép). **Không thể thay đổi tỉnh** vì dòng cập nhật tỉnh bị comment tại gate (`:411`). |
| `DistrictCode` | `string?` | Có | `null` | Mã quận/huyện (khóa ghép). Chuẩn hóa qua `StdParam`. |
| `DistrictName` | `string?` | Không | `null` | Tên quận/huyện mới. Chuẩn hóa qua `.Trim()`. |
| `FlagActive` | `object?` | Không | `"0"` | Trạng thái hoạt động. Chuẩn hóa qua `StdFlag` (thiếu/khác `"1"` đổi về `"0"`). |

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Kiểm tra Body rỗng: Nếu null hoặc rỗng → Lỗi `Fail("Mst_District_Update")` (`:389-396`).
2. Cột cập nhật `Ft_Cols_Upd`: Gate chỉ gửi `"Mst_District.DistrictName,Mst_District.FlagActive,"` (`:409-413`), không cho phép đổi tỉnh cha.
3. Mở một transaction duy nhất (`Master.cs:2212`).
4. **Chuỗi kiểm tra Guard theo thứ tự nguồn**:
   - **Bước 1**: Kiểm tra tồn tại quận/huyện theo **CẶP KHÓA GHÉP `(ProvinceCode, DistrictCode)`** qua `Mst_District_CheckDB` (cờ `"1"`) → Nếu không tìm thấy → Lỗi `Mst_District_CheckDB_DistrictNotFound` (`:2264-2270, :1729-1799`).
   - **Bước 2**: Kiểm tra tỉnh cha qua `Mst_Province_CheckDB(strProvinceCode, Flag "1", ActiveList "1")`:
     - Không thấy tỉnh cha → Lỗi `Mst_Province_CheckDB_ProvinceNotFound`.
     - Tỉnh cha có `FlagActive != '1'` → Lỗi `Mst_Province_CheckDB_FlagActiveNotMatched` (không cho phép sửa huyện thuộc tỉnh đã ngừng hoạt động).
   - **Bước 3**: Nếu `strDistrictName.Length < 1` → Lỗi `Mst_District_Update_InvalidDistrictName` (`:2276-2286`).
5. Cập nhật dữ liệu: `entity.DistrictName = strDistrictName`, `entity.FlagActive = strFlagActive`, `entity.LogLUDTimeUTC = DateTime.UtcNow`, `entity.LogLUBy = "port"`.
6. Commit transaction và trả về `Ok(new { Success = true, Data = (object?)null })` (`:431-432`).

#### Bảng mã lỗi

| Mã lỗi (`ErridnLoyalty.*`) | HTTP Status | Tham số đi kèm | Điều kiện kích hoạt |
|---|---|---|---|
| `Mst_District_Update` | 400 | `Check.strJson = null` | Request body rỗng hoặc null (`:389-396`). |
| `Mst_District_CheckDB_DistrictNotFound` | 400 | `Check.ProvinceCode`, `Check.DistrictCode` | Không tìm thấy quận/huyện theo cặp khóa ghép (`:2264-2270`). |
| `Mst_Province_CheckDB_ProvinceNotFound` | 400 | `Check.ProvinceCode` | Tỉnh cha không tồn tại. |
| `Mst_Province_CheckDB_FlagActiveNotMatched` | 400 | `Check.ProvinceCode` | Tỉnh cha đang ngừng hoạt động (`FlagActive = '0'`). |
| `Mst_District_Update_InvalidDistrictName` | 400 | `Check.strDistrictName` | Tên quận/huyện rỗng khi cập nhật (`:2276-2286`). |

#### Ví dụ Request / Response thực tế (từ lần chạy VERIFY #144)
- **Cập nhật thành công**:
  - Request: `{"ProvinceCode": "hn", "DistrictCode": "hn01", "DistrictName": "  Ba Đình mới  ", "FlagActive": "1"}`
  - Response (HTTP 200): `{"success": true, "data": null}`.
- **Cập nhật quận của tỉnh ngừng hoạt động (`CT01`)**:
  - Request: `{"ProvinceCode": "CT", "DistrictCode": "CT01", "DistrictName": "Ninh Kiều mới", "FlagActive": "1"}`
  - Response (HTTP 400): `ErridnLoyalty.Mst_Province_CheckDB_FlagActiveNotMatched`.
- **Sai cặp khóa ghép**:
  - Request: `{"ProvinceCode": "DN", "DistrictCode": "HN01", "DistrictName": "x"}`
  - Response (HTTP 400): `ErridnLoyalty.Mst_District_CheckDB_DistrictNotFound`.

---

### 2.7. Delete (#145)

Xóa một quận/huyện khỏi danh mục theo cặp khóa ghép `(ProvinceCode, DistrictCode)`.

- **Route**: `POST /api/mst_district/delete`
- **Controller Action**: [`Mst_DistrictControllerPort.Delete`](../../Controllers/Port/Mst_DistrictControllerPort.cs#L610-L658)
- **Tham chiếu nguồn**: Gate `Mst_DistrictController.cs:449-513` → API `MstDistrictController.cs:242-313` → Biz `Master.cs:2885-2968`, `:2360-2475`, `:1729-1799`

#### Bảng tham số Request

Payload JSON gửi qua Request Body (`MstDistrictDeleteReq`):

| Tên tham số | Kiểu dữ liệu | Bắt buộc | Mặc định | Ý nghĩa & Quy tắc |
|---|---|---|---|---|
| `DistrictCode` | `string?` | Có | `null` | Mã quận/huyện cần xóa (`:449`). Chuẩn hóa qua `StdParam`. |
| `ProvinceCode` | `string?` | Có | `null` | Mã tỉnh/thành cần xóa (`:449`). Chuẩn hóa qua `StdParam`. |

#### Quy tắc xử lý theo thứ tự (Hành vi If lồng đặc thù nguồn)
Cấu trúc kiểm tra tại tầng gate của nguồn (`:468-496`):
```csharp
if (!string.IsNullOrEmpty(DistrictCode)) {
    if (!string.IsNullOrEmpty(ProvinceCode)) {
        // Thực hiện xóa...
        objJsonResultEntry.SetSuccess();
    }
} else {
    throw new ServiceException("invalid_districtcode_provincecode", ...);
}
```
Từ đó suy ra các trường hợp xử lý:
1. **Thiếu `DistrictCode`** (kể cả body rỗng `{}`): Ném `ServiceException("invalid_districtcode_provincecode")` → Trả về **HTTP 400 `ErridnLoyalty.invalid_districtcode_provincecode`**.
2. **Có `DistrictCode` nhưng thiếu `ProvinceCode`**: Không rơi vào nhánh else, không gọi hàm xóa, không gọi `SetSuccess()`. Kết quả: Trả về **HTTP 200 kèm `{"Success": false, "Data": null}`** (bản ghi không bị xóa).
3. **Đủ cả `DistrictCode` và `ProvinceCode`**:
   - Mở 1 transaction (`Master.cs:2391`).
   - Chuẩn hóa: `strProvinceCode = StdParam(req.ProvinceCode)`, `strDistrictCode = StdParam(req.DistrictCode)`.
   - Kiểm tra tồn tại theo cặp `(ProvinceCode, DistrictCode)` qua `Mst_District_CheckDB` (cờ `"1"`) → Nếu không thấy → Ném lỗi **HTTP 400 `ErridnLoyalty.Mst_District_CheckDB_DistrictNotFound`** (`:2412-2421`).
   - Khóa ngoại: Bảng `Mst_Agent` có FK tới `Mst_District` nhưng chưa port (GAP).
   - Xóa bản ghi khỏi bảng `Mst_District` (`:2427-2434`).
   - Commit transaction và trả về **HTTP 200 kèm `{"Success": true, "Data": null}`** (`:493-494`).

#### Bảng mã lỗi

| Mã lỗi (`ErridnLoyalty.*`) | HTTP Status | Tham số đi kèm | Điều kiện kích hoạt |
|---|---|---|---|
| `invalid_districtcode_provincecode` | 400 | `Check.DistrictCode`, `Check.ProvinceCode` | Thiếu `DistrictCode` tại tầng gate (`:497-500`). |
| `Mst_District_CheckDB_DistrictNotFound` | 400 | `Check.ProvinceCode`, `Check.DistrictCode` | Không tìm thấy quận/huyện theo cặp khóa ghép (`:2412-2421`). |

#### Ví dụ Request / Response thực tế (từ lần chạy VERIFY #145)
- **Xóa thành công**:
  - Request: `{"ProvinceCode": "hn", "DistrictCode": "hn02"}`
  - Response (HTTP 200): `{"success": true, "data": null}`. Bản ghi `HN02` bị xóa, `ItemCount` giảm 5 → 4.
- **Có huyện nhưng thiếu tỉnh (HTTP 200 nhưng Success = false)**:
  - Request: `{"DistrictCode": "HN01"}`
  - Response (HTTP 200): `{"success": false, "data": null}`. Bản ghi `HN01` vẫn còn nguyên trong CSDL.
- **Thiếu cả hai hoặc body rỗng**:
  - Request: `{}`
  - Response (HTTP 400): `ErridnLoyalty.invalid_districtcode_provincecode`.

---

### 2.8. Import (#146)

Nhập danh mục quận/huyện từ tập tin Excel `.xlsx`.

- **Route**: `POST /api/mst_district/import`
- **Content-Type**: `multipart/form-data`
- **Controller Action**: [`Mst_DistrictControllerPort.Import`](../../Controllers/Port/Mst_DistrictControllerPort.cs#L722-L829)
- **Tham chiếu nguồn**: Gate `Mst_DistrictController.cs:517-673` → API `WA_Mst_District_Create` → Biz `Mst_District_Create`

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Kiểm tra file upload (`:541-546`): File rỗng, thiếu file hoặc sai đuôi `.xlsx` → Lỗi `Mst_District_Import` ("File excel import không hợp lệ").
2. Đọc file qua [`PortExcel.ReadSheet`](../../Data/PortExcel.cs):
   - 0 dòng dữ liệu → Lỗi `Mst_District_Import` ("File excel import không có dữ liệu").
   - Số cột tiêu đề khác 3 (`GetImportDicColumsTemplate().Count != 3`) → Lỗi `Mst_District_Import` ("File excel import không hợp lệ").
3. **Kiểm tra Validate toàn bộ dòng TRƯỚC khi tạo** (`:567-590`):
   - Dòng kiểm tra `DistrictCode` rỗng → Lỗi `Mst_District_Import` kèm `Message = "Mã quận/huyện không được trống"`.
   - Dòng kiểm tra `ProvinceCode` rỗng → Lỗi `Mst_District_Import` kèm `Message = "Mã tỉnh/thành không được trống"`.
   - Dòng kiểm tra `DistrictName` rỗng → Lỗi `Mst_District_Import` kèm `Message = "Tên quận/huyện không được trống"`.
   - Có bất kỳ dòng nào vi phạm, dừng ngay và không có dòng nào được tạo vào CSDL.
4. **Lưu từng dòng (Partial Import)**:
   - Duyệt tuần tự từng dòng, gọi `CreateXAsync` trong **1 transaction riêng biệt cho mỗi dòng**.
   - Dòng thứ `k` bị lỗi (ví dụ trùng cặp mã hoặc tỉnh cha ngừng hoạt động), ném mã lỗi 400 của dòng `k` và dừng vòng lặp.
   - **Các dòng trước `k` đã được commit vào CSDL**, các dòng sau `k` không được duyệt.
5. Thành công: Trả về `Ok(new { Success = true, Data = (object?)null })` (`:653-655`).

#### Bảng mã lỗi

| Mã lỗi (`ErridnLoyalty.*`) | HTTP Status | Tham số đi kèm | Điều kiện kích hoạt |
|---|---|---|---|
| `Mst_District_Import` | 400 | `Message = "File excel import không hợp lệ"` | Không có file, file rỗng, sai đuôi hoặc số cột khác 3. |
| `Mst_District_Import` | 400 | `Message = "File excel import không có dữ liệu"` | File chỉ có tiêu đề, không có dữ liệu. |
| `Mst_District_Import` | 400 | `Message = "Mã quận/huyện không được trống"` | Dòng có `DistrictCode` rỗng. |
| `Mst_District_Import` | 400 | `Message = "Mã tỉnh/thành không được trống"` | Dòng có `ProvinceCode` rỗng. |
| `Mst_District_Import` | 400 | `Message = "Tên quận/huyện không được trống"` | Dòng có `DistrictName` rỗng. |
| `Mst_District_CheckDB_DistrictExist` | 400 | `Check.ProvinceCode`, `Check.DistrictCode` | Dòng thứ `k` bị trùng cặp khóa ghép trong CSDL. |
| `Mst_Province_CheckDB_FlagActiveNotMatched` | 400 | `Check.ProvinceCode` | Dòng thứ `k` có tỉnh cha đang ngừng hoạt động. |

#### Minh chứng Kiểm thử Thực tế (từ lần chạy VERIFY #146)
- File gồm 3 dòng: `[DN02, DN, Thanh Khê]` (hợp lệ), `[HN01, HN, x]` (trùng seed), `[HN04, HN, Cầu Giấy]` (hợp lệ).
- Kết quả: HTTP 400 `ErridnLoyalty.Mst_District_CheckDB_DistrictExist` với `Check.ProvinceCode = "HN", Check.DistrictCode = "HN01"`.
- CSDL: `DN02` **đã được tạo thành công** (`ItemCount` tăng từ 7 lên 8), `HN04` **không được tạo**.

---

### 2.9. Export (#147)

Xuất danh sách quận/huyện ra tập tin Excel `.xlsx`.

- **Route**: `POST /api/mst_district/export`
- **Controller Action**: [`Mst_DistrictControllerPort.Export`](../../Controllers/Port/Mst_DistrictControllerPort.cs#L841-L885)
- **Tham chiếu nguồn**: Gate `Mst_DistrictController.cs:678-756` → API `WA_Mst_District_Get` → Biz `Master.cs:1883-1929`

#### Bảng tham số Request

Payload JSON gửi qua Request Body (`MstDistrictExportReq`):

| Tên tham số | Kiểu dữ liệu | Bắt buộc | Mặc định | Ý nghĩa & Quy tắc |
|---|---|---|---|---|
| `OrgID` | `string?` | Không | `null` | Nhận tại gate nhưng không lọc (`:818-833`). |
| `KeyWord` | `string?` | Không | `null` | Lọc theo từ khóa tên quận/huyện (`DistrictName LIKE '%kw%'`). |
| `FlagActive` | `string?` | Không | `null` | Lọc theo trạng thái hoạt động (**toán tử `=`**, `:827-830`). |

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Gọi `GetXAsync(r.KeyWord, r.FlagActive, 0, 123456000)` để trích xuất dữ liệu (`:717-718`).
2. Kiểm tra dữ liệu rỗng: Nếu không có bản ghi nào, ném `ServiceException("Không có dữ liệu")` (`:742`) → Trả về HTTP 400 `ErridnLoyalty.Mst_District_Export` kèm `Message = "Không có dữ liệu"`.
3. Từ điển cột: `GetImportDicColums()` gồm đúng 4 cột (`:729, :795-804`):
   - `DistrictCode`: `"Mã quận/huyện (*)"`
   - `ProvinceCode`: `"Mã tỉnh/thành (*)"`
   - `DistrictName`: `"Tên quận/huyện"`
   - `FlagActive`: `"Trạng thái"`
4. Sinh file Excel sheet `"Mst_District"` qua [`PortExcel.ExportToExcelFromList`](../../Data/PortExcel.cs).
5. Trả về `Ok(new { Success = true, Data = new { Url = baseAddress + virtualPath } })` (`:733-737`).

#### Bảng mã lỗi

| Mã lỗi (`ErridnLoyalty.*`) | HTTP Status | Tham số đi kèm | Điều kiện kích hoạt |
|---|---|---|---|
| `Mst_District_Export` | 400 | `Message = "Không có dữ liệu"` | Không có bản ghi nào thỏa mãn điều kiện lọc (`:742`). |

---

### 2.10. ExportTemplate (#148)

Xuất tập tin mẫu Excel dùng cho chức năng Import danh mục quận/huyện.

- **Route**: `POST /api/mst_district/exporttemplate`
- **Controller Action**: [`Mst_DistrictControllerPort.ExportTemplate`](../../Controllers/Port/Mst_DistrictControllerPort.cs#L897-L916)
- **Tham chiếu nguồn**: Gate `Mst_DistrictController.cs:759-791`

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Không nhận body, không kiểm tra quyền, không đọc CSDL (`:767`).
2. Khởi tạo danh sách rỗng `List<Mst_District>`.
3. Từ điển cột: `GetImportDicColumsTemplate()` gồm đúng 3 cột (`:769, :806-815`):
   - `DistrictCode`: `"Mã quận/huyện (*)"`
   - `ProvinceCode`: `"Mã tỉnh/thành (*)"`
   - `DistrictName`: `"Tên quận/huyện"`
4. Sinh file Excel rỗng có dòng tiêu đề sheet `"Mst_District"`.
5. Trả về `Ok(new { Success = true, Data = new { Url = baseAddress + virtualPath } })` (`:773-777`).

#### Bảng mã lỗi
Endpoint luôn trả về HTTP 200.

---

## 3. Khác biệt So với Nguồn & Danh sách GAP (Known Gaps)

| Hạng mục | Trạng thái ở Nguồn | Triển khai ở Đích | Phân loại & Ghi chú GAP |
|---|---|---|---|
| **Xác thực & Phân quyền** | Gate kiểm tra `CheckGatewayAuthentication`, `CheckAuthorize`; Biz kiểm tra `Sys_User_CheckAuthorize`. | Bỏ qua toàn bộ kiểm tra phân quyền. | Bỏ qua có chủ đích (**Loại Đ**). |
| **Ghi nhật ký hệ thống** | Ghi bản ghi `RequestLog` và `ReturnLog` vào CSDL cho mọi thao tác. | Bỏ qua ghi log DB. | Bỏ qua có chủ đích (**Loại Đ**). |
| **Khóa chính ghép** | Nguồn không có DDL, kiểm tra CheckDB theo cặp `(ProvinceCode, DistrictCode)`. | Khai báo `HasKey(x => new { x.ProvinceCode, x.DistrictCode })` trong EF Core. | Khớp logic nghiệp vụ nguồn 1-1. |
| **Khóa ngoại `Mst_Agent`** | DDL nguồn có FK từ `Mst_Agent` tới `Mst_District` (`20191203.z11.CreateTable.Mst_Agent.sql:27`). | Bảng `Mst_Agent` chưa được port nên không kiểm tra chặn xóa bảng con. | **GAP**: Chưa có bảng `Mst_Agent` ở đích. |
| **Toán tử lọc `FlagActive`** | Màn `Mst_District` dùng toán tử `=` (`:827-830`) thay vì `IN`. | Giữ nguyên toán tử `=` như nguồn. | Khớp nguồn 1-1. |
| **Hành vi xóa thiếu tỉnh** | Có `DistrictCode` nhưng thiếu `ProvinceCode` trả về HTTP 200 `Success = false` không xóa. | Tái hiện chính xác cấu trúc if lồng của nguồn. | Khớp nguồn 1-1. |
| **Không đổi tỉnh khi Update** | Dòng gán `ProvinceCode` vào `Ft_Cols_Upd` bị comment tại gate (`:411`). | Không cập nhật cột `ProvinceCode` khi gọi Update. | Khớp nguồn 1-1. |

---

## 4. Nhật ký Thay đổi & Đối chiếu Tiến độ (Changelog)

| STT Hạng mục | Tên Endpoint & Action | Trạng thái | Mã Commit Git | Mô tả tóm tắt thay đổi |
|---|---|---|---|---|
| **#139** | `Mst_DistrictController/Search` | Đã hoàn thành | `926454e` | Khởi tạo model [`Mst_District.cs`](../../Models/Port/Mst_District.cs), cấu hình EF Core, Seeder 5 bản ghi và action tìm kiếm phân trang. |
| **#140** | `Mst_DistrictController/GetDetail` | Đã hoàn thành | `638e1da` | Tra cứu chi tiết quận/huyện theo cặp khóa ghép `(ProvinceCode, DistrictCode)`, không lọc cờ hoạt động. |
| **#141** | `Mst_DistrictController/GetByProvinceCode` | Đã hoàn thành | `4c27394` | Lấy danh sách quận/huyện theo tỉnh/thành, bọc trong đối tượng `Lst_Mst_District`. |
| **#142** | `Mst_DistrictController/GetAllActive` | Đã hoàn thành | `1ec6984` | Lấy toàn bộ danh sách quận/huyện đang hoạt động, trả về mảng phẳng trực tiếp. |
| **#143** | `Mst_DistrictController/Create` | Đã hoàn thành | `bcb2a6d` | Thêm mới quận/huyện theo cặp khóa ghép, kiểm tra tồn tại và trạng thái tỉnh cha. |
| **#144** | `Mst_DistrictController/Update` | Đã hoàn thành | `b956397` | Cập nhật tên và trạng thái quận/huyện, bảo lưu tỉnh cha, kiểm tra trạng thái tỉnh cha. |
| **#145** | `Mst_DistrictController/Delete` | Đã hoàn thành | `6a0da23` | Xóa đơn lẻ quận/huyện theo cặp khóa ghép, tái hiện hành vi if lồng đặc thù của nguồn. |
| **#146** | `Mst_DistrictController/Import` | Đã hoàn thành | `02b9e95` | Nhập danh mục từ Excel, validate toàn bộ dòng trước khi tạo và thực thi cơ chế nhập một phần. |
| **#147** | `Mst_DistrictController/Export` | Đã hoàn thành | `e6b27ff` | Xuất danh sách quận/huyện ra Excel, sử dụng từ điển 4 cột, báo lỗi khi không có dữ liệu. |
| **#148** | `Mst_DistrictController/ExportTemplate` | Đã hoàn thành | `733b31b` | Xuất file mẫu Excel gồm 3 cột tiêu đề, không truy vấn CSDL. |
