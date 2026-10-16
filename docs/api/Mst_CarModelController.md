# Tham chiếu API Mst_CarModelController (Port 1-1)

| Thông tin | Giá trị |
|---|---|
| **Phạm vi** | Tài liệu tham chiếu chi tiết 10 endpoint đã port 1-1 của Mst_CarModelController (#97 - #106) |
| **Cập nhật** | 2026-10-16 |
| **Phiên bản mã** | `ed28f8f` |
| **Trạng thái** | Nháp — chờ review |

> [!NOTE]
> Toàn bộ các endpoint thuộc controller này áp dụng chuẩn hóa JSON PascalCase tự động qua `PortJsonResultFilter` và xử lý ngoại lệ tập trung qua `PortExceptionFilter` (chuyển đổi ngoại lệ chưa bắt thành HTTP 400 `PortResult.Fail`). Chi tiết xem tại [Kiến trúc Hạ tầng Dùng chung](../kien-truc/HA_TANG_CHUNG.md).

---

## 1. Tóm tắt Nghiệp vụ & Cấu trúc Dữ liệu

### 1.1. Mục đích Nghiệp vụ
API `Mst_CarModelController` quản lý danh mục dòng xe ô tô (`Mst_CarModel`) trong hệ thống MiniLoyalty. Module cung cấp đầy đủ các chức năng tra cứu danh mục phân trang theo từ khóa và trạng thái hoạt động (toán tử so sánh bằng tuyệt đối `=`), lấy danh sách dòng xe đang hoạt động, tra cứu chi tiết theo mã, thêm mới dòng xe (kiểm tra tồn tại dòng xe và kiểm tra tính hợp lệ/trạng thái hoạt động của hãng xe cha `Mst_CarBrand`), cập nhật thông tin dòng xe, xóa đơn lẻ, xóa nhiều dòng xe theo cơ chế xóa một phần (partial delete), nhập dữ liệu hàng loạt từ file Excel `.xlsx`, xuất danh sách dòng xe và xuất file Excel mẫu template.

### 1.2. Bảng Đối chiếu Nguồn Tra cứu (Source Code Mapping)

| Thành phần | Đường dẫn tập tin nguồn (`2022.7.Loyalty`) | Tập tin đích tương ứng (`MiniLoyalty`) |
|---|---|---|
| **Gate Controller** | `13.ClientGate\V10\idn.Loyalty.WebAPI\Controllers\Mst_CarModelController.cs:25-906` | [`Controllers/Port/Mst_CarModelControllerPort.cs`](../../Controllers/Port/Mst_CarModelControllerPort.cs) |
| **API Backend** | `11.BackEnd\V10\idn.Loyalty.Biz.Web\Controllers\MstCarModelController.cs:17-310` | Tích hợp trực tiếp vào [`Controllers/Port/Mst_CarModelControllerPort.cs`](../../Controllers/Port/Mst_CarModelControllerPort.cs) |
| **Nghiệp vụ (Biz)** | `11.BackEnd\V10\idn.Loyalty.Biz\Master.Loyalty.cs` (Get `:1739-1791`; Create `:1908-2250`; Update `:2251-2592`; Delete `:2593-2866`; CheckDB `:1843-1906`) | Tích hợp trực tiếp vào [`Controllers/Port/Mst_CarModelControllerPort.cs`](../../Controllers/Port/Mst_CarModelControllerPort.cs) |
| **Entity Model** | `12.Dev.Common\idn.Loyalty.Common\Models\Loyalty\Mst_CarModel.cs:9-23` | [`Models/Port/Mst_CarModel.cs:30-38`](../../Models/Port/Mst_CarModel.cs#L30-L38) |
| **Cấu hình EF** | Khóa chính suy từ FK trong `20221026.CreateDatabase.sql:91` | [`Models/Port/Mst_CarModel.cs:43-51`](../../Models/Port/Mst_CarModel.cs#L43-L51) |
| **Seeder Demo** | `Order => 20` (danh mục `Mst_*` dùng 10–99) | [`Models/Port/Mst_CarModel.cs:58-75`](../../Models/Port/Mst_CarModel.cs#L58-L75) |

### 1.3. Cấu trúc Bảng CSDL (`Mst_CarModel`)

Bảng dữ liệu vật lý `Mst_CarModel` gồm đúng 8 cột (nguồn không có tập tin DDL tạo bảng trực tiếp, khóa chính `ModelCode` được suy luận từ ràng buộc khóa ngoại `REFERENCES Mst_CarModel (ModelCode)` tại `20221026.CreateDatabase.sql:91`):

| Tên cột | Kiểu EF Core | Khóa | Cho phép NULL | Ghi chú & Ý nghĩa |
|---|---|---|---|---|
| `ModelCode` | `string` | **PK** | Không | Mã dòng xe (chuẩn hóa qua `StdParam`: viết hoa, không khoảng trắng thừa). Khóa chính. |
| `NetworkID` | `string?` | | Có | Mã mạng nghiệp vụ. Ở đích lấy từ cấu hình `Biz_NetworkID`. |
| `BrandCode` | `string?` | | Có | Mã hãng xe trực thuộc (`Mst_CarBrand`). Không tạo quan hệ FK cứng trên EF do nguồn không có DDL chứng minh. |
| `ModelName` | `string?` | | Có | Tên dòng xe (chuẩn hóa qua `StdStr`). |
| `FlagActive` | `string?` | | Có | Cờ trạng thái hoạt động: `"1"` (Đang hoạt động), `"0"` (Ngừng hoạt động). |
| `Remark` | `string?` | | Có | Ghi chú dòng xe. |
| `LogLUDTimeUTC` | `DateTime?` | | Có | Thời điểm cập nhật cuối cùng theo chuẩn UTC. |
| `LogLUBy` | `string?` | | Có | Tài khoản cập nhật cuối cùng (mặc định gán `"seed"` hoặc `"port"` ở đích). |

**Dữ liệu mẫu khởi tạo (Seeder - `Mst_CarModelSeeder`, Idempotent)**:
- Gồm 4 bản ghi demo: `ACC` (Accent, BrandCode `HYU`, FlagActive `'1'`), `MOR` (Morning, BrandCode `KIA`, FlagActive `'1'`), `VIO` (Vios, BrandCode `TOY`, FlagActive `'1'`), `VF8` (VF 8, BrandCode `VIN`, FlagActive `'0'`).
- Toàn bộ bản ghi gán `NetworkID = "6403834000"`, `LogLUBy = "seed"`.

---

## 2. Đặc tả Chi tiết Từng Endpoint API

---

### 2.1. Search (#97)

Tìm kiếm danh mục dòng xe phân trang theo từ khóa tên dòng xe và trạng thái hoạt động.

- **Route**: `POST /api/mst_carmodel/search`
- **Controller Action**: [`Mst_CarModelControllerPort.Search`](../../Controllers/Port/Mst_CarModelControllerPort.cs#L183)
- **Tham chiếu nguồn**: Gate `Mst_CarModelController.cs:25-108` → API `MstCarModelController.cs:17-88` → Biz `Master.Loyalty.cs:1739-1791`

#### Bảng tham số Request

Payload JSON gửi qua Request Body (`MstCarModelSearchReq`), hỗ trợ cả trường hợp body rỗng:

| Tên tham số | Kiểu dữ liệu | Bắt buộc | Mặc định | Ý nghĩa & Quy tắc |
|---|---|---|---|---|
| `KeyWord` | `string?` | Không | `null` | Từ khóa tìm kiếm theo tên dòng xe (`ModelName LIKE '%kw%'`, `gate :291`). |
| `FlagActive` | `string?` | Không | `null` | Trạng thái hoạt động, **áp dụng toán tử so sánh bằng tuyệt đối '='** (`gate :299-302`). Không tách dấu phẩy (nếu gửi `"0,1"` sẽ so khớp chuỗi nguyên bản dẫn đến 0 kết quả). |
| `Ft_PageIndex` | `int` | Không | `0` | Chỉ số trang cần lấy (0-based). Nếu `< 0` giữ nguyên theo chuẩn bảo vệ phân trang. |
| `Ft_PageSize` | `int` | Không | `10` | Số dòng trên mỗi trang. Nếu `<= 0` trả về mảng rỗng `DataList = []` (`gate :83-94`). |

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Xác định vị trí bản ghi: `start = (long)pageIndex * pageSize`.
2. Mệnh đề lọc qua `GetXAsync`:
   - **KeyWord**: `EF.Functions.Like((x.ModelName ?? "").ToLower(), PortQuery.Contains(keyWord), PortQuery.Esc)`.
   - **FlagActive**: `(x.FlagActive ?? "").ToLower() == flagActive.ToLower()`.
3. Sắp xếp thứ tự: Phân trang theo `md.ModelCode ASC` (`Master.Loyalty.cs:1748`), select cuối sắp theo `t.MyIdxSeq ASC` (`Master.Loyalty.cs:1788`).
4. Đếm tổng số bản ghi: `itemCount = await ordered.CountAsync()`.
5. Trích xuất trang: Nếu `pageSize <= 0` trả về danh sách rỗng; nếu `pageSize > 0` gọi `.Skip((int)start).Take((int)pageSize)`.
6. Gán `MyIdxSeq = start + i` (0-based identity).
7. Tính `PageCount`: Nếu `rows.Count == 0`, `PageCount = 0`; ngược lại `PageCount = ceil(itemCount / ps)` (nếu `pageSize == 0` thì lấy `ps = 10`).

#### Bảng mã lỗi
Endpoint luôn trả về HTTP 200 kèm đối tượng `PageInfo`.

#### Ví dụ Request / Response thực tế (từ lần chạy VERIFY #97)
- **Tìm kiếm mặc định (`{}`)**:
  - Request: `POST /api/mst_carmodel/search` kèm `{}`
  - Response (HTTP 200):
    ```json
    {
      "Success": true,
      "Data": {
        "DataList": [
          { "MyIdxSeq": 0, "ModelCode": "ACC", "BrandCode": "HYU", "ModelName": "Accent", "FlagActive": "1" },
          { "MyIdxSeq": 1, "ModelCode": "MOR", "BrandCode": "KIA", "ModelName": "Morning", "FlagActive": "1" },
          { "MyIdxSeq": 2, "ModelCode": "VF8", "BrandCode": "VIN", "ModelName": "VF 8", "FlagActive": "0" },
          { "MyIdxSeq": 3, "ModelCode": "VIO", "BrandCode": "TOY", "ModelName": "Vios", "FlagActive": "1" }
        ],
        "ItemCount": 4,
        "PageCount": 1,
        "PageIndex": 0,
        "PageSize": 10
      }
    }
    ```

---

### 2.2. GetAllActive (#98)

Lấy toàn bộ danh sách các dòng xe đang hoạt động (`FlagActive = '1'`).

- **Route**: `POST /api/mst_carmodel/getallactive`
- **Controller Action**: [`Mst_CarModelControllerPort.GetAllActive`](../../Controllers/Port/Mst_CarModelControllerPort.cs#L211)
- **Tham chiếu nguồn**: Gate `Mst_CarModelController.cs:110-215` → API `MstCarModelController.cs:91-164` → Biz `Master.Loyalty.cs:1739-1791`

#### Bảng tham số Request
Endpoint không nhận tham số Body (`gate :110`).

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Thiết lập tham số phân trang nguồn: `Ft_RecordStart = 0`, `Ft_RecordCount = 123456000` (`gate :148-149`).
2. Mệnh đề lọc: `FlagActive = "1"` (toán tử `=`, `gate :135, :301`). Khối `Ext/tree` (`:159-191`) bị comment trong nguồn nên không xử lý.
3. Trả về mảng phẳng trần các thực thể `Mst_CarModel` kèm `MyIdxSeq`. Nếu không có dữ liệu trả về `Data = null` (`gate :129, :156-158, :196-203`).

#### Ví dụ Request / Response thực tế (từ lần chạy VERIFY #98)
- Request: `POST /api/mst_carmodel/getallactive`
- Response (HTTP 200):
  ```json
  {
    "Success": true,
    "Data": [
      { "MyIdxSeq": 0, "ModelCode": "ACC", "BrandCode": "HYU", "ModelName": "Accent", "FlagActive": "1" },
      { "MyIdxSeq": 1, "ModelCode": "MOR", "BrandCode": "KIA", "ModelName": "Morning", "FlagActive": "1" },
      { "MyIdxSeq": 2, "ModelCode": "VIO", "BrandCode": "TOY", "ModelName": "Vios", "FlagActive": "1" }
    ]
  }
  ```

---

### 2.3. GetByModelCode (#99)

Tra cứu thông tin chi tiết một dòng xe theo mã `ModelCode`.

- **Route**: `POST /api/mst_carmodel/getbymodelcode`
- **Controller Action**: [`Mst_CarModelControllerPort.GetByModelCode`](../../Controllers/Port/Mst_CarModelControllerPort.cs#L229)
- **Tham chiếu nguồn**: Gate `Mst_CarModelController.cs:217-280` → API `MstCarModelController.cs:166-237` → Biz `Master.Loyalty.cs:1739-1791`

#### Bảng tham số Request

Payload JSON gửi qua Request Body (`MstCarModelGetByModelCodeReq`):

| Tên tham số | Kiểu dữ liệu | Bắt buộc | Mặc định | Ý nghĩa & Quy tắc |
|---|---|---|---|---|
| `ModelCode` | `string?` | Có | `null` | Mã dòng xe cần tra cứu (`gate :237-241`). |

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Kiểm tra tham số: Nếu `ModelCode` rỗng/null, không truy vấn CSDL, trả về ngay HTTP 200 với `Data = null` (`gate :237, :267`).
2. Truy vấn dữ liệu: Lọc theo `ModelCode` chính xác (chuẩn hóa `StdParam`), không lọc cờ `FlagActive` (dòng xe ngừng hoạt động `FlagActive = '0'` vẫn được lấy).
3. Trả về đối tượng đơn lẻ đầu tiên (`Lst_Mst_CarModel[0]`) hoặc `null` nếu không tìm thấy (`gate :261-264`).

#### Ví dụ Request / Response thực tế (từ lần chạy VERIFY #99)
- Request: `POST /api/mst_carmodel/getbymodelcode` kèm `{"ModelCode": "ACC"}`
- Response (HTTP 200):
  ```json
  {
    "Success": true,
    "Data": {
      "MyIdxSeq": 0,
      "ModelCode": "ACC",
      "NetworkID": "6403834000",
      "BrandCode": "HYU",
      "ModelName": "Accent",
      "FlagActive": "1",
      "Remark": null,
      "LogLUBy": "seed"
    }
  }
  ```

---

### 2.4. Create (#100)

Thêm mới một dòng xe vào hệ thống MiniLoyalty.

- **Route**: `POST /api/mst_carmodel/create`
- **Controller Action**: [`Mst_CarModelControllerPort.Create`](../../Controllers/Port/Mst_CarModelControllerPort.cs#L278)
- **Tham chiếu nguồn**: Gate `Mst_CarModelController.cs:492-566` → API `MstCarModelController.cs:239-310` → Biz `Master.Loyalty.cs:1908-2250`

#### Bảng tham số Request

Payload JSON gửi qua Request Body (`MstCarModelCreateReq`):

| Tên tham số | Kiểu dữ liệu | Bắt buộc | Mặc định | Ý nghĩa & Quy tắc |
|---|---|---|---|---|
| `strJson` | `string?` | Có | `null` | Chuỗi JSON chứa đối tượng dòng xe cần tạo (theo đúng giao thức Gate nguồn). |
| `obj` | `Mst_CarModel?`| Không | `null` | Đối tượng bind trực tiếp nếu không gửi qua `strJson`. |

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Kiểm tra payload: Nếu `strJson` và `obj` đều rỗng → trả về lỗi `Mst_CarModel_Create`.
2. Chuẩn hóa dữ liệu: `ModelCode = StdParam(ModelCode)`, `ModelName = StdStr(ModelName)`, `BrandCode = StdParam(BrandCode)`.
3. Kiểm tra mã rỗng: Nếu `ModelCode` rỗng → trả về lỗi `Mst_CarModel_Create_InvalidModelCode`.
4. Kiểm tra trùng mã: Gọi `Mst_CarModel_CheckDB(ModelCode, FlagExist = "1")`. Nếu đã tồn tại → trả về lỗi `Mst_CarModel_CheckDB_CarModelExist`.
5. Kiểm tra hãng xe cha: Gọi `Mst_CarBrand_CheckDB(BrandCode, FlagExist = "1")`.
   - Nếu không tìm thấy hãng xe trong bảng `Mst_CarBrand` → trả về lỗi `Mst_CarBrand_CheckDB_BrandNotFound`.
   - Nếu hãng xe có `FlagActive != "1"` → trả về lỗi `Mst_CarBrand_CheckDB_FlagActiveNotMatched`.
6. Thực hiện lưu vào CSDL trong transaction: Gán `FlagActive = "1"`, `LogLUDTimeUTC = DateTime.UtcNow`, `LogLUBy = "port"`.

#### Bảng mã lỗi (Copy nguyên văn từ mã nguồn)

| Mã lỗi (`ErrorCode`) | Tham số `Params` | Điều kiện kích hoạt |
|---|---|---|
| `ErridnLoyalty.Mst_CarModel_Create` | `{"Check.strJson": null}` | Payload request body rỗng hoặc không phân tích được JSON. |
| `ErridnLoyalty.Mst_CarModel_Create_InvalidModelCode` | `{"Check.strModelCode": strModelCode}` | `ModelCode` sau chuẩn hóa bị rỗng hoặc null. |
| `ErridnLoyalty.Mst_CarModel_CheckDB_CarModelExist` | `{"Check.ModelCode": strModelCode}` | Dòng xe đã tồn tại trong CSDL. |
| `ErridnLoyalty.Mst_CarBrand_CheckDB_BrandNotFound` | `{"Check.BrandCode": strBrandCode}` | Hãng xe `BrandCode` không tồn tại trong danh mục `Mst_CarBrand`. |
| `ErridnLoyalty.Mst_CarBrand_CheckDB_FlagActiveNotMatched` | `{"Check.BrandCode": strBrandCode, "Check.strFlagActiveListToCheck": "1", "DB.FlagActive": dbFlag}` | Hãng xe tồn tại nhưng trạng thái hoạt động không phải là `'1'`. |

#### Ví dụ Request / Response thực tế (từ lần chạy VERIFY #100)
- **Tạo thành công**:
  - Request: `POST /api/mst_carmodel/create` kèm `{"obj": {"ModelCode": "CR", "BrandCode": "HYU", "ModelName": "Creta"}}`
  - Response (HTTP 200): `{"Success": true, "Data": null}`
- **Báo lỗi trùng mã**:
  - Request: `POST /api/mst_carmodel/create` kèm `{"obj": {"ModelCode": "ACC", "BrandCode": "HYU", "ModelName": "Accent Mới"}}`
  - Response (HTTP 400):
    ```json
    {
      "Success": false,
      "ErrorCode": "ErridnLoyalty.Mst_CarModel_CheckDB_CarModelExist",
      "Params": { "Check.ModelCode": "ACC" }
    }
    ```

---

### 2.5. Update (#101)

Cập nhật thông tin và trạng thái hoạt động của dòng xe.

- **Route**: `POST /api/mst_carmodel/update`
- **Controller Action**: [`Mst_CarModelControllerPort.Update`](../../Controllers/Port/Mst_CarModelControllerPort.cs#L418)
- **Tham chiếu nguồn**: Gate `Mst_CarModelController.cs:568-642` → API `MstCarModelController.cs:239-310` → Biz `Master.Loyalty.cs:2251-2592`

#### Bảng tham số Request

Payload JSON gửi qua Request Body (`MstCarModelUpdateReq`):

| Tên tham số | Kiểu dữ liệu | Bắt buộc | Mặc định | Ý nghĩa & Quy tắc |
|---|---|---|---|---|
| `strJson` | `string?` | Có | `null` | Chuỗi JSON chứa thông tin cập nhật dòng xe. |
| `obj` | `Mst_CarModel?`| Không | `null` | Đối tượng bind trực tiếp nếu không gửi qua `strJson`. |

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Kiểm tra payload: Nếu rỗng → trả về lỗi `Mst_CarModel_Update`.
2. Kiểm tra tồn tại: Gọi `Mst_CarModel_CheckDB(ModelCode, FlagExist = "1")`. Nếu không tồn tại → trả về lỗi `Mst_CarModel_CheckDB_CarModelNotFound`.
3. Kiểm tra hãng xe cha: Nếu có cập nhật `BrandCode`, kiểm tra sự tồn tại trong `Mst_CarBrand`. Nếu không có → trả về lỗi `Mst_CarBrand_CheckDB_BrandNotFound`. (Lưu ý: Nguồn không kiểm tra `FlagActive` của hãng xe khi Update).
4. Cập nhật dữ liệu: Gán các trường `ModelName`, `BrandCode`, `Remark`, `FlagActive` (hạ về `'0'` nếu null hoặc khác `'1'`), `LogLUDTimeUTC = DateTime.UtcNow`, `LogLUBy = "port"`.

#### Bảng mã lỗi (Copy nguyên văn từ mã nguồn)

| Mã lỗi (`ErrorCode`) | Tham số `Params` | Điều kiện kích hoạt |
|---|---|---|
| `ErridnLoyalty.Mst_CarModel_Update` | `{"Check.strJson": null}` | Payload request body rỗng hoặc không hợp lệ. |
| `ErridnLoyalty.Mst_CarModel_CheckDB_CarModelNotFound` | `{"Check.ModelCode": strModelCode}` | `ModelCode` cần cập nhật không tồn tại trong CSDL. |
| `ErridnLoyalty.Mst_CarBrand_CheckDB_BrandNotFound` | `{"Check.BrandCode": strBrandCode}` | Hãng xe cập nhật không tồn tại trong hệ thống. |

---

### 2.6. Delete (#102)

Xóa một dòng xe đơn lẻ khỏi CSDL.

- **Route**: `POST /api/mst_carmodel/delete`
- **Controller Action**: [`Mst_CarModelControllerPort.Delete`](../../Controllers/Port/Mst_CarModelControllerPort.cs#L516)
- **Tham chiếu nguồn**: Gate `Mst_CarModelController.cs:644-706` → API `MstCarModelController.cs:312-383` → Biz `Master.Loyalty.cs:2593-2866`

#### Bảng tham số Request

Payload JSON gửi qua Request Body (`MstCarModelDeleteReq`):

| Tên tham số | Kiểu dữ liệu | Bắt buộc | Mặc định | Ý nghĩa & Quy tắc |
|---|---|---|---|---|
| `ModelCode` | `string?` | Có | `null` | Mã dòng xe cần xóa (`gate :644`). |

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Kiểm tra mã rỗng tại Gate: Nếu `ModelCode` rỗng hoặc null → trả về lỗi `invalid_ModelCode` (`gate :665`).
2. Kiểm tra tồn tại: Gọi `Mst_CarModel_CheckDB(ModelCode, FlagExist = "1")`. Nếu không tồn tại → trả về lỗi `Mst_CarModel_CheckDB_CarModelNotFound`.
3. Thực hiện xóa bản ghi khỏi CSDL trong transaction và trả về HTTP 200 `{ "Success": true, "Data": null }`.

#### Bảng mã lỗi (Copy nguyên văn từ mã nguồn)

| Mã lỗi (`ErrorCode`) | Tham số `Params` | Điều kiện kích hoạt |
|---|---|---|
| `ErridnLoyalty.invalid_ModelCode` | `{"Check.ModelCode": req?.ModelCode}` | `ModelCode` gửi lên bị rỗng hoặc null. |
| `ErridnLoyalty.Mst_CarModel_CheckDB_CarModelNotFound` | `{"Check.ModelCode": strModelCode}` | Dòng xe cần xóa không tìm thấy trong CSDL. |

---

### 2.7. DeleteMultiple (#103)

Xóa nhiều dòng xe theo danh sách theo cơ chế xóa một phần (partial delete).

- **Route**: `POST /api/mst_carmodel/deletemultiple`
- **Controller Action**: [`Mst_CarModelControllerPort.DeleteMultiple`](../../Controllers/Port/Mst_CarModelControllerPort.cs#L571)
- **Tham chiếu nguồn**: Gate `Mst_CarModelController.cs:708-769` → API `MstCarModelController.cs:312-383` → Biz `Master.Loyalty.cs:2593-2866`

#### Bảng tham số Request

Payload JSON nhận mảng danh sách các đối tượng chứa `ModelCode` (`List<MstCarModelDeleteReq>`):

| Tên tham số | Kiểu dữ liệu | Bắt buộc | Mặc định | Ý nghĩa & Quy tắc |
|---|---|---|---|---|
| `[].ModelCode` | `string` | Có | | Mã dòng xe cần xóa trong từng phần tử. |

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Kiểm tra body rỗng: Nếu body null → trả về lỗi `Mst_CarModel_Delete`. Nếu body là mảng rỗng `[]` → trả về HTTP 200 `{ "Success": true, "Data": null }`.
2. Lặp tuần tự từng phần tử trong danh sách:
   - Mỗi phần tử được thực thi trong **một transaction độc lập**.
   - Nếu gặp lỗi ở phần tử thứ `k` (ví dụ không tìm thấy `ModelCode`), transaction của phần tử `k` bị hủy và hệ thống dừng ngay vòng lặp, trả về mã lỗi của phần tử đó.
   - **Hành vi xóa một phần (Partial Delete)**: Toàn bộ các phần tử từ `1` đến `k-1` đã được commit thành công trước đó vẫn được lưu vĩnh viễn trong CSDL.

#### Ví dụ Request / Response thực tế (từ lần chạy VERIFY #103)
- Request: `POST /api/mst_carmodel/deletemultiple` kèm `[{"ModelCode": "ACC"}, {"ModelCode": "MA_KHONG_TON_TAI"}, {"ModelCode": "VIO"}]`
- Kết quả: Mã lỗi `ErridnLoyalty.Mst_CarModel_CheckDB_CarModelNotFound` trả về cho phần tử thứ 2. Dòng xe `ACC` bị xóa thành công; dòng xe `VIO` chưa được xử lý và vẫn còn nguyên trong CSDL.

---

### 2.8. Import (#104)

Nhập danh mục dòng xe hàng loạt từ tập tin Excel `.xlsx`.

- **Route**: `POST /api/mst_carmodel/import`
- **HTTP Method**: `POST` (`multipart/form-data`)
- **Controller Action**: [`Mst_CarModelControllerPort.Import`](../../Controllers/Port/Mst_CarModelControllerPort.cs#L697)
- **Tham chiếu nguồn**: Gate `Mst_CarModelController.cs:771-849`

#### Bảng tham số Request

| Tên tham số | Kiểu dữ liệu | Bắt buộc | Vị trí | Ý nghĩa & Quy tắc |
|---|---|---|---|---|
| `file` | `IFormFile` | Có | Form Data | File Excel `.xlsx` chứa 3 cột: `Mã Model`, `Tên Model`, `Mã hiệu xe`. |

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Kiểm tra file: File thiếu hoặc đuôi file khác `.xlsx` → trả về lỗi `File excel import không hợp lệ`.
2. Đọc dữ liệu qua `PortExcel.ReadSheet`:
   - Nếu sheet không có dòng dữ liệu → trả về lỗi `File excel import không có dữ liệu`.
   - Nếu số cột khác 3 → trả về lỗi `File excel import không hợp lệ`.
3. **Kiểm tra TOÀN BỘ các dòng trước khi lưu**:
   - Nếu có dòng thiếu `ModelCode` → báo lỗi `Mã Model không được trống`.
   - Nếu có dòng thiếu `ModelName` → báo lỗi `Tên Model không được trống`.
   - Nếu có dòng thiếu `BrandCode` → báo lỗi `Mã hiệu xe không được trống`.
4. Thực hiện ghi dữ liệu: Lặp từng dòng và gọi `CreateXAsync` trong từng transaction riêng biệt. Nếu phát sinh lỗi ở dòng `k` (trùng mã, hãng xe cha không hoạt động...), dừng ngay và trả về mã lỗi tương ứng (hỗ trợ Import một phần).

#### Bảng mã lỗi (Copy nguyên văn từ mã nguồn)

| Mã lỗi (`ErrorCode`) | Tham số `Params` | Điều kiện kích hoạt |
|---|---|---|
| `ErridnLoyalty.Mst_CarModel_Import` | `{"Message": "File excel import không hợp lệ"}` | File rỗng, không đúng định dạng hoặc không đủ 3 cột tiêu chuẩn. |
| `ErridnLoyalty.Mst_CarModel_Import` | `{"Message": "File excel import không có dữ liệu"}` | Tập tin Excel không có dòng dữ liệu nào. |
| `ErridnLoyalty.Mst_CarModel_Import` | `{"Message": "Mã Model không được trống"}` | Có ít nhất một dòng trong Excel bị trống cột mã dòng xe. |
| `ErridnLoyalty.Mst_CarModel_Import` | `{"Message": "Tên Model không được trống"}` | Có ít nhất một dòng trong Excel bị trống cột tên dòng xe. |
| `ErridnLoyalty.Mst_CarModel_Import` | `{"Message": "Mã hiệu xe không được trống"}` | Có ít nhất một dòng trong Excel bị trống cột mã hãng xe. |

---

### 2.9. Export (#105)

Xuất danh sách dòng xe ra tập tin Excel `.xlsx`.

- **Route**: `POST /api/mst_carmodel/export`
- **Controller Action**: [`Mst_CarModelControllerPort.Export`](../../Controllers/Port/Mst_CarModelControllerPort.cs#L817)
- **Tham chiếu nguồn**: Gate `Mst_CarModelController.cs:771-849`

#### Bảng tham số Request

Payload JSON gửi qua Request Body (`MstCarModelExportReq`):

| Tên tham số | Kiểu dữ liệu | Bắt buộc | Mặc định | Ý nghĩa & Quy tắc |
|---|---|---|---|---|
| `KeyWord` | `string?` | Không | `null` | Lọc theo tên dòng xe (`ModelName LIKE '%kw%'`). |
| `FlagActive` | `string?` | Không | `null` | Lọc theo trạng thái hoạt động (toán tử `=`). |

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Truy vấn danh sách dòng xe: `Ft_RecordStart = 0`, `Ft_RecordCount = 123456000`, sắp xếp theo `ModelCode ASC`.
2. Kiểm tra dữ liệu: Nếu danh sách trả về rỗng → trả về HTTP 400 kèm lỗi `Mst_CarModel_Export` (`Message = "Không có dữ liệu"`).
3. Xuất file Excel: Sử dụng từ điển 4 cột tiêu đề chuẩn (`GetImportDicColums()`):
   - `ModelCode` → `"Mã Model (*)"`
   - `ModelName` → `"Tên Model(*)"`
   - `BrandCode` → `"Mã hiệu xe (*)"`
   - `FlagActive` → `"Trạng thái"`
4. Lưu file vào thư mục web root qua `PortExcel.ExportToExcelFromList` và trả về URL tải file.

#### Bảng mã lỗi (Copy nguyên văn từ mã nguồn)

| Mã lỗi (`ErrorCode`) | Tham số `Params` | Điều kiện kích hoạt |
|---|---|---|
| `ErridnLoyalty.Mst_CarModel_Export` | `{"Message": "Không có dữ liệu"}` | Không tìm thấy bất kỳ dòng xe nào thỏa mãn điều kiện lọc. |

---

### 2.10. ExportTemplate (#106)

Xuất file mẫu Excel template phục vụ cho việc nhập dữ liệu danh mục dòng xe.

- **Route**: `POST /api/mst_carmodel/exporttemplate`
- **Controller Action**: [`Mst_CarModelControllerPort.ExportTemplate`](../../Controllers/Port/Mst_CarModelControllerPort.cs#L872)
- **Tham chiếu nguồn**: Gate `Mst_CarModelController.cs:852-884`

#### Bảng tham số Request
Endpoint không nhận tham số Body và không truy vấn cơ sở dữ liệu.

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Khởi tạo danh sách dòng xe rỗng `List<Mst_CarModel>`.
2. Ánh xạ từ điển 3 cột tiêu đề mẫu (`GetImportDicColumsTemplate()`):
   - `ModelCode` → `"Mã Model"`
   - `ModelName` → `"Tên Model(*)"`
   - `BrandCode` → `"Mã hiệu xe"`
3. Sinh file mẫu qua `PortExcel.ExportToExcelFromList` và trả về đường dẫn URL tải file: `{ "Success": true, "Data": { "Url": "..." } }`.

---

## 3. Khác biệt so với Nguồn & GAP

1. **Khóa chính và DDL**: Nguồn không có tập tin DDL tạo bảng trực tiếp trong repository. Khóa chính `ModelCode` được suy luận chắc chắn từ ràng buộc khóa ngoại trong DDL khởi tạo (`20221026.CreateDatabase.sql:91`).
2. **Khóa ngoại giả lập con (Prm_CarRecommendSpec / Prm_CarNewSpec)**: Nguồn có kiểm tra ràng buộc không cho xóa nếu dòng xe đã được dùng trong các bảng nghiệp vụ khuyến mại/giới thiệu xe. Vì các module này chưa port ở đích, MiniLoyalty tạm thời chỉ ghi nhận comment GAP và không chặn xóa.
3. **Phân quyền & Tài khoản hệ thống**: Bỏ qua các lời gọi `CheckGatewayAuthentication`, `CheckAuthorize`, `Sys_User_CheckAuthorize`, và không chép các tài khoản hệ thống viết cứng từ nguồn.
4. **Định dạng file Excel**: Sử dụng thư viện `PortExcel` (dựa trên ClosedXML/OpenXML) thay thế cho DLL nguồn độc quyền `idn.ExportToExcel`.

---

## 4. Nhật ký Thay đổi

| Hạng mục (#) | Action / Endpoint | Commit Hash | Ngày hoàn thành | Ghi chú |
|---|---|---|---|---|
| #97 | `Search` | `7f59b63` | 2026-10-16 | Khởi tạo bảng `Mst_CarModel`, seeder demo và action tìm kiếm phân trang |
| #98 | `GetAllActive` | `7a89b1e` | 2026-10-16 | Lấy toàn bộ dòng xe có FlagActive = '1' |
| #99 | `GetByModelCode` | `fcd66ce` | 2026-10-16 | Tra cứu chi tiết theo mã |
| #100 | `Create` | `52137d6` | 2026-10-16 | Thêm mới dòng xe kèm kiểm tra hợp lệ hãng xe cha |
| #101 | `Update` | `5685496` | 2026-10-16 | Cập nhật thông tin và trạng thái dòng xe |
| #102 | `Delete` | `79ced82` | 2026-10-16 | Xóa đơn lẻ dòng xe theo mã |
| #103 | `DeleteMultiple` | `5b61bc9` | 2026-10-16 | Xóa hàng loạt theo cơ chế xóa một phần (partial delete) |
| #104 | `Import` | `c1ef11a` | 2026-10-16 | Nhập dữ liệu từ Excel .xlsx (kiểm tra rỗng toàn bộ trước khi lưu) |
| #105 | `Export` | `847a191` | 2026-10-16 | Xuất danh sách dòng xe ra Excel 4 cột từ điển |
| #106 | `ExportTemplate` | `1548bea` | 2026-10-16 | Xuất file mẫu template 3 cột từ điển |
