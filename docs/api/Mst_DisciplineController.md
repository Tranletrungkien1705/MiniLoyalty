# Tham chiếu API Mst_DisciplineController (Port 1-1)

| Thông tin | Giá trị |
|---|---|
| **Phạm vi** | Tài liệu tham chiếu chi tiết 7 endpoint đã port 1-1 của Mst_DisciplineController (#132 - #138) |
| **Cập nhật** | 2026-10-16 |
| **Phiên bản mã** | `014ab70` |
| **Trạng thái** | Nháp — chờ review |

---

## 1. Tóm tắt Nghiệp vụ & Cấu trúc Dữ liệu

### 1.1. Mục đích Nghiệp vụ
API `Mst_DisciplineController` quản lý danh mục các hình thức kỷ luật nhân sự (`Mst_Discipline`) trong hệ thống MiniLoyalty. Module cung cấp các chức năng tìm kiếm phân trang theo từ khóa và trạng thái hoạt động (toán tử so sánh bằng tuyệt đối), lấy danh sách tất cả hình thức kỷ luật đang hoạt động, tra cứu chi tiết theo mã, thêm mới kèm sinh sequence tự động `DISCIPLINECODE...`, cập nhật thông tin và trạng thái (tự động hạ cờ nếu thiếu), xóa đơn lẻ (tái hiện lỗi typo sao chép từ Department của nguồn) và xóa hàng loạt theo cơ chế xóa một phần (partial delete).

### 1.2. Bảng Đối chiếu Nguồn Tra cứu (Source Code Mapping)

| Thành phần | Đường dẫn tập tin nguồn (`2022.7.Loyalty`) | Tập tin đích tương ứng (`MiniLoyalty`) |
|---|---|---|
| **Gate Controller** | `13.ClientGate\V10\idn.Loyalty.WebAPI\Controllers\Mst_DisciplineController.cs:26-555` | [`Controllers/Port/Mst_DisciplineControllerPort.cs`](../../Controllers/Port/Mst_DisciplineControllerPort.cs) |
| **API Backend** | `11.BackEnd\V10\idn.Loyalty.Biz.Web\Controllers\MstDisciplineController.cs:17-310` | Tích hợp trực tiếp vào [`Controllers/Port/Mst_DisciplineControllerPort.cs`](../../Controllers/Port/Mst_DisciplineControllerPort.cs) |
| **Nghiệp vụ (Biz)** | `11.BackEnd\V10\idn.Loyalty.Biz\MasterData.cs` (Get `:33954-34331`; Create `:34333-34630`; Update `:34632-34939`; Delete `:34941-35186`) | Tích hợp trực tiếp vào [`Controllers/Port/Mst_DisciplineControllerPort.cs`](../../Controllers/Port/Mst_DisciplineControllerPort.cs) |
| **Entity Model** | `12.Dev.Common\idn.Loyalty.Common\Models\HRM\Mst_Discipline.cs:9-18` | [`Models/Port/Mst_Discipline.cs:29-38`](../../Models/Port/Mst_Discipline.cs#L29-L38) |
| **Sequence Entity** | `Seq.cs:28-41 (SeqType.DisciplineCode)` | [`Models/Port/Mst_Discipline.cs:78-83`](../../Models/Port/Mst_Discipline.cs#L78-L83) |
| **Seeder Demo** | `Order => 33` (danh mục `Mst_*` dùng 10–99) | [`Models/Port/Mst_Discipline.cs:56-72`](../../Models/Port/Mst_Discipline.cs#L56-L72) |

### 1.3. Cấu trúc Bảng CSDL (`Mst_Discipline` & `Seq_DisciplineCode`)

Bảng dữ liệu vật lý `Mst_Discipline` gồm đúng 7 cột (nguồn không có tập tin DDL tạo bảng trong repo, cấu trúc cột được xác nhận qua model và code nghiệp vụ; khóa chính `DisciplineCode` là khóa draft):

| Tên cột | Kiểu EF Core | Khóa | Cho phép NULL | Ghi chú & Ý nghĩa |
|---|---|---|---|---|
| `DisciplineCode` | `string` | **PK** | Không | Mã hình thức kỷ luật. Khóa chính (draft - không có DDL nguồn, kiểu cột cần xác nhận). |
| `NetworkID` | `string?` | | Có | Mã mạng nghiệp vụ. Ở đích lấy từ cấu hình `Biz_NetworkID`. |
| `DisciplineName` | `string?` | | Có | Tên hình thức kỷ luật (chuẩn hóa qua `StdStr`). |
| `DisciplineDesc` | `string?` | | Có | Mô tả chi tiết hình thức kỷ luật. |
| `FlagActive` | `string?` | | Có | Cờ trạng thái hoạt động: `"1"` (Đang hoạt động), `"0"` (Ngừng hoạt động). |
| `LogLUDTimeUTC` | `DateTime?` | | Có | Thời điểm cập nhật cuối cùng theo chuẩn UTC. |
| `LogLUBy` | `string?` | | Có | Tài khoản thực hiện cập nhật cuối cùng (mặc định gán `"port"` ở đích). |

**Bảng Sequence `Seq_DisciplineCode`**:
- `AutoId`: `long`, Khóa chính tự tăng (`IDENTITY(1,1)`), sinh số thứ tự sequence cho mã hình thức kỷ luật.
- `Remark`: `string?`, Nullable, Ghi chú dòng sequence.

**Dữ liệu mẫu khởi tạo (Seeder - `Mst_DisciplineSeeder`, Idempotent)**:
- Gồm 3 bản ghi demo: `KL01` (Khiển trách, FlagActive `'1'`), `KL02` (Cảnh cáo, FlagActive `'1'`), `KL03` (Sa thải, FlagActive `'0'`).
- Toàn bộ bản ghi gán `NetworkID = "DEMO"`, `LogLUBy = "seed"`.

---

## 2. Đặc tả Chi tiết Từng Endpoint API

---

### 2.1. Search (#132)

Tìm kiếm danh mục hình thức kỷ luật phân trang theo từ khóa và trạng thái hoạt động.

- **Route**: `POST /api/mst_discipline/search`
- **Controller Action**: [`Mst_DisciplineControllerPort.Search`](../../Controllers/Port/Mst_DisciplineControllerPort.cs#L162-L186)
- **Tham chiếu nguồn**: Gate `Mst_DisciplineController.cs:26-106` → API `MstDisciplineController.cs:17-88` → Biz `MasterData.cs:33954-34331`

#### Bảng tham số Request

Payload JSON gửi qua Request Body (`MstDisciplineSearchReq`), hỗ trợ cả trường hợp body rỗng hoàn toàn:

| Tên tham số | Kiểu dữ liệu | Bắt buộc | Mặc định | Ý nghĩa & Quy tắc |
|---|---|---|---|---|
| `OrgID` | `string?` | Không | `null` | Nhận tại tham số hàm (`gate :26, :56`) nhưng hàm `strWhereClause_Mst_Discipline` (`:523-538`) **không sử dụng OrgID** trong mệnh đề WHERE → Không lọc. |
| `KeyWord` | `string?` | Không | `null` | Từ khóa tìm kiếm theo tên kỷ luật (`DisciplineName LIKE '%kw%'`, `gate :530`). |
| `FlagActive` | `string?` | Không | `null` | Trạng thái hoạt động, **áp dụng toán tử bằng tuyệt đối '='** (`gate :534`). Không tách dấu phẩy (nếu gửi `"0,1"` sẽ so sánh chuỗi nguyên bản dẫn đến 0 kết quả). |
| `Ft_PageIndex` | `int` | Không | `0` | Chỉ số trang cần lấy (0-based). Nếu `< 0` tự động gán về `0`. |
| `Ft_PageSize` | `int` | Không | `10` | Số dòng trên mỗi trang. Nếu `<= 0` trả về mảng rỗng `DataList = []` (`gate :81-89`). |

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Chuẩn hóa phân trang: `pageIndex = Math.Max(0, req.Ft_PageIndex)`, `pageSize = req.Ft_PageSize`.
2. Vị trí bản ghi bắt đầu: `start = (long)pageIndex * pageSize`.
3. Mệnh đề lọc qua `GetXAsync` ([`Mst_DisciplineControllerPort.cs:640-672`](../../Controllers/Port/Mst_DisciplineControllerPort.cs#L640-L672)):
   - **KeyWord**: `EF.Functions.Like((x.DisciplineName ?? "").ToLower(), PortQuery.Contains(keyWord), PortQuery.Esc)`.
   - **FlagActive**: So sánh bằng `(x.FlagActive ?? "").ToLower() == flagActive.ToLower()`.
4. Sắp xếp thứ tự bản ghi: Phân trang theo `md.DisciplineCode ASC` (`MasterData.cs:34016`), select cuối sắp theo `t.MyIdxSeq ASC` (`MasterData.cs:34055`).
5. Đếm tổng số bản ghi: `itemCount = await ordered.CountAsync()`.
6. Trích xuất trang: Nếu `pageSize <= 0` trả về danh sách rỗng; nếu `pageSize > 0` gọi `.Skip((int)start).Take((int)pageSize)`.
7. Gán `MyIdxSeq = start + i` (0-based, `MasterData.cs:34010`).
8. Tính `PageCount`: Nếu `dataList.Count == 0`, `PageCount = 0`; ngược lại `PageCount = ceil(itemCount / ps)`.

#### Bảng mã lỗi
Endpoint luôn trả về HTTP 200.

#### Ví dụ Request / Response thực tế (từ lần chạy VERIFY #132)
- **Tìm kiếm mặc định (`{}`)**:
  - Request: `POST /api/mst_discipline/search` kèm `{}`
  - Response (HTTP 200):
    ```json
    {
      "Success": true,
      "Data": {
        "DataList": [
          { "MyIdxSeq": 0, "DisciplineCode": "KL01", "DisciplineName": "Khiển trách", "FlagActive": "1" },
          { "MyIdxSeq": 1, "DisciplineCode": "KL02", "DisciplineName": "Cảnh cáo", "FlagActive": "1" },
          { "MyIdxSeq": 2, "DisciplineCode": "KL03", "DisciplineName": "Sa thải", "FlagActive": "0" }
        ],
        "ItemCount": 3,
        "PageCount": 1,
        "PageIndex": 0,
        "PageSize": 10
      }
    }
    ```

---

### 2.2. GetByDisciplineCode (#133)

Tra cứu thông tin chi tiết một hình thức kỷ luật theo mã.

- **Route**: `POST /api/mst_discipline/getbydisciplinecode`
- **Controller Action**: [`Mst_DisciplineControllerPort.GetByDisciplineCode`](../../Controllers/Port/Mst_DisciplineControllerPort.cs#L206-L242)
- **Tham chiếu nguồn**: Gate `Mst_DisciplineController.cs:108-171` → API `MstDisciplineController.cs:17-88` → Biz `MasterData.cs:33954-34331`

#### Bảng tham số Request

Payload JSON gửi qua Request Body (`MstDisciplineGetByDisciplineCodeReq`):

| Tên tham số | Kiểu dữ liệu | Bắt buộc | Mặc định | Ý nghĩa & Quy tắc |
|---|---|---|---|---|
| `DisciplineCode` | `string?` | Có | `null` | Mã hình thức kỷ luật cần tra cứu (`gate :108`). |

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. **Kiểm tra mã rỗng** (`gate :128, :158`):
   - Nếu `DisciplineCode` rỗng hoặc null: **Tuyệt đối không truy vấn CSDL**, trả về ngay HTTP 200 `{ Success = true, Data = null }`.
2. Chuẩn hóa mã qua `StdParam(req.DisciplineCode)`.
3. Lọc trong CSDL theo `DisciplineCode =` không phân biệt hoa thường (`gate :547`). Không lọc `FlagActive` (`gate :130-133`), do đó bản ghi ngừng hoạt động như `KL03` vẫn tra cứu thành công.
4. Trả về đối tượng bản ghi đầu tiên `Lst_Mst_Discipline[0]` hoặc `null` nếu không tìm thấy (`gate :152-155`).

#### Bảng mã lỗi
Endpoint luôn trả về HTTP 200.

#### Ví dụ Request / Response thực tế (từ lần chạy VERIFY #133)
- Request `{"DisciplineCode": "kl01"}` → HTTP 200 `{ "Success": true, "Data": { "DisciplineCode": "KL01", "DisciplineName": "Khiển trách", "FlagActive": "1" } }`.
- Request `{"DisciplineCode": "KL0"}` → HTTP 200 `{ "Success": true, "Data": null }`.
- Request `{}` hoặc `{"DisciplineCode": ""}` → HTTP 200 `{ "Success": true, "Data": null }` (không gọi DB).

---

### 2.3. GetAllActive (#134)

Lấy danh sách tất cả các hình thức kỷ luật đang hoạt động (`FlagActive = '1'`).

- **Route**: `POST /api/mst_discipline/getallactive`
- **Controller Action**: [`Mst_DisciplineControllerPort.GetAllActive`](../../Controllers/Port/Mst_DisciplineControllerPort.cs#L188-L204)
- **Tham chiếu nguồn**: Gate `Mst_DisciplineController.cs:173-233` → API `MstDisciplineController.cs:17-88` → Biz `MasterData.cs:33954-34331`

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Không nhận body, phân trang toàn bộ (`Ft_RecordStart = 0`, `Ft_RecordCount = 123456000`, `gate :208-209`).
2. Lọc `FlagActive = "1"` qua toán tử bằng `=` (`gate :195, :551`).
3. Sắp xếp danh sách theo `md.DisciplineCode ASC`.
4. Trả về trực tiếp mảng phẳng `List<Mst_Discipline>` hoặc `null` nếu danh sách rỗng (`gate :192, :215-220`).

#### Bảng mã lỗi
Endpoint luôn trả về HTTP 200.

#### Ví dụ Request / Response thực tế (từ lần chạy VERIFY #134)
- Request: `POST /api/mst_discipline/getallactive`
- Response (HTTP 200):
  ```json
  {
    "Success": true,
    "Data": [
      { "MyIdxSeq": 0, "DisciplineCode": "KL01", "DisciplineName": "Khiển trách", "FlagActive": "1" },
      { "MyIdxSeq": 1, "DisciplineCode": "KL02", "DisciplineName": "Cảnh cáo", "FlagActive": "1" }
    ]
  }
  ```

---

### 2.4. Create (#135)

Thêm mới hình thức kỷ luật, tự động sinh mã sequence `DISCIPLINECODE...` ghi đè mã client gửi.

- **Route**: `POST /api/mst_discipline/create`
- **Controller Action**: [`Mst_DisciplineControllerPort.Create`](../../Controllers/Port/Mst_DisciplineControllerPort.cs#L244-L373)
- **Tham chiếu nguồn**: Gate `Mst_DisciplineController.cs:237-320` → API `MstDisciplineController.cs:91-162` → Biz `MasterData.cs:34333-34630`

#### Bảng tham số Request

Payload JSON gửi qua Request Body (`MstDisciplineCreateReq`):

| Tên tham số | Kiểu dữ liệu | Bắt buộc | Mặc định | Ý nghĩa & Quy tắc |
|---|---|---|---|---|
| `DisciplineCode` | `string?` | Không | | Mã client gửi lên, **bị ghi đè hoàn toàn bởi sequence hệ thống** (`gate :287`). |
| `DisciplineName` | `string?` | **Có** | | Tên hình thức kỷ luật (chuẩn hóa qua `StdStr`). |
| `DisciplineDesc` | `string?` | Không | | Mô tả chi tiết hình thức kỷ luật. |

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. **Kiểm tra body rỗng / null**: Nếu `req == null` → Trả về HTTP 400 `ErridnLoyalty.Mst_Discipline_Create` (`Check.strJson = null`, `gate :246-253`). Ở bước này **chưa sinh sequence**.
2. **Sinh mã Sequence tự động** (`gate :267-288, Seq.cs:28-41`):
   - Mở Transaction riêng biệt lấy `AutoId` từ bảng `Seq_DisciplineCode`, commit ngay.
   - Định dạng mã: `PortSeq.Format("DISCIPLINECODE", autoId, 5, 100000)` → Tạo chuỗi dạng `DISCIPLINECODE.00001`.
   - **Mã này ghi đè hoàn toàn giá trị `DisciplineCode` client gửi**.
   - Sequence đã tăng sẽ không bị rollback kể cả khi bước lưu sau đó gặp lỗi.
3. Mở Transaction CSDL để lưu bản ghi (`MasterData.cs:34467`).
4. Chuẩn hóa chuỗi: `DisciplineName` và `DisciplineDesc` qua `StdStr` (`Trim().Normalize()`).
5. **Kiểm tra tên rỗng**: Nếu `DisciplineName` rỗng sau chuẩn hóa → Rollback, trả về HTTP 400 kèm mã lỗi `ErridnLoyalty.Mst_Discipline_CreateX_InvalidDisciplineName` (`MasterData.cs:34394-34404`).
6. Lưu 7 cột dữ liệu:
   - `DisciplineCode`: Mã sinh từ sequence.
   - `NetworkID`: Lấy từ cấu hình `Biz_NetworkID` (mặc định `"1001"`).
   - `DisciplineName`, `DisciplineDesc`.
   - `FlagActive = "1"`, `LogLUDTimeUTC = DateTime.UtcNow`, `LogLUBy = "port"`.
7. Commit Transaction, trả về HTTP 200 `{ Success = true, Data = null }` (`gate :306-307`).

#### Bảng mã lỗi `ErridnLoyalty.*`

| Mã lỗi | Điều kiện kích hoạt | Các tham số trả về trong `Params` |
|---|---|---|
| `ErridnLoyalty.Mst_Discipline_Create` | Request body rỗng hoặc null | `Check.strJson = null` |
| `ErridnLoyalty.Mst_Discipline_CreateX_InvalidDisciplineName` | `DisciplineName` rỗng hoặc chỉ có khoảng trắng | `Check.strDisciplineName = null` |

---

### 2.5. Update (#136)

Cập nhật thông tin hình thức kỷ luật, tự động hạ cờ về `'0'` nếu thiếu `FlagActive`.

- **Route**: `POST /api/mst_discipline/update`
- **Controller Action**: [`Mst_DisciplineControllerPort.Update`](../../Controllers/Port/Mst_DisciplineControllerPort.cs#L375-L488)
- **Tham chiếu nguồn**: Gate `Mst_DisciplineController.cs:324-389` → API `MstDisciplineController.cs:165-236` → Biz `MasterData.cs:34632-34939`

#### Bảng tham số Request

Payload JSON gửi qua Request Body (`MstDisciplineUpdateReq`):

| Tên tham số | Kiểu dữ liệu | Bắt buộc | Mặc định | Ý nghĩa & Quy tắc |
|---|---|---|---|---|
| `DisciplineCode` | `string?` | **Có** | | Mã hình thức kỷ luật cần cập nhật (`gate :324`). |
| `DisciplineName` | `string?` | **Có** | | Tên hình thức kỷ luật mới (chuẩn hóa qua `StdStr`). |
| `DisciplineDesc` | `string?` | Không | | Mô tả chi tiết hình thức kỷ luật. |
| `FlagActive` | `string?` | Không | `"0"` | Cờ trạng thái: Chuẩn hóa qua `StdFlag` (chỉ nhận `"1"`, còn lại kể cả null/thiếu đều hạ về `"0"`). |

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Kiểm tra body rỗng / null: Trả về HTTP 400 `ErridnLoyalty.Mst_Discipline_Update` (`gate :333-340`).
2. Mở Transaction CSDL độc lập (`MasterData.cs:34770`).
3. Chuẩn hóa: `DisciplineCode` (StdParam), `DisciplineName` (StdStr), `DisciplineDesc` (StdStr), `FlagActive` (StdFlag).
4. **Kiểm tra mã tồn tại trong CSDL** (`MasterData.cs:34684-34690`): Nếu không tìm thấy → Rollback, trả về HTTP 400 `ErridnLoyalty.Mst_Discipline_CheckDB_DisciplineCodeNotFound`.
5. **Kiểm tra tên rỗng**: Nếu `DisciplineName` rỗng → Rollback, trả về HTTP 400 `ErridnLoyalty.Mst_Discipline_UpdateX_InvalidDisciplineName`.
6. Cập nhật cố định 3 cột theo `Ft_Cols_Upd` nguồn (`gate :353-357`): `DisciplineName`, `DisciplineDesc`, `FlagActive` cùng với `LogLUDTimeUTC = DateTime.UtcNow`, `LogLUBy = "port"`. Không thay đổi `DisciplineCode` và `NetworkID`.
7. Commit Transaction, trả về HTTP 200 `{ Success = true, Data = null }` (`gate :375-376`).

#### Bảng mã lỗi `ErridnLoyalty.*`

| Mã lỗi | Điều kiện kích hoạt | Các tham số trả về trong `Params` |
|---|---|---|
| `ErridnLoyalty.Mst_Discipline_Update` | Request body rỗng hoặc null | `Check.strJson = null` |
| `ErridnLoyalty.Mst_Discipline_CheckDB_DisciplineCodeNotFound` | Bản ghi không tồn tại trong CSDL | `Check.DisciplineCode = <value>` |
| `ErridnLoyalty.Mst_Discipline_UpdateX_InvalidDisciplineName` | Tên hình thức kỷ luật rỗng | `Check.strDisciplineName = null` |

---

### 2.6. Delete (#137)

Xóa một bản ghi hình thức kỷ luật đơn lẻ khỏi cơ sở dữ liệu.

- **Route**: `POST /api/mst_discipline/delete`
- **Controller Action**: [`Mst_DisciplineControllerPort.Delete`](../../Controllers/Port/Mst_DisciplineControllerPort.cs#L490-L544)
- **Tham chiếu nguồn**: Gate `Mst_DisciplineController.cs:393-454` → API `MstDisciplineController.cs:239-310` → Biz `MasterData.cs:34941-35186`

#### Bảng tham số Request

Payload JSON gửi qua Request Body (`MstDisciplineDeleteReq`):

| Tên tham số | Kiểu dữ liệu | Bắt buộc | Mặc định | Ý nghĩa & Quy tắc |
|---|---|---|---|---|
| `DisciplineCode` | `string?` | **Có** | | Mã hình thức kỷ luật cần xóa (`gate :393`). |
| `OrgID` | `string?` | Không | | Nhận tại gate nhưng biz không sử dụng (`gate :393, :427`). |

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. **Kiểm tra mã rỗng tại tầng Gate** (`gate :439`):
   - Nếu `DisciplineCode` null hoặc rỗng: Tầng gate nguồn ném `ServiceException("invalid_departmentcode", "invalid_departmentcode")` do lập trình viên sao chép mã từ `Mst_DepartmentController` mà không sửa lại tên lỗi.
   - **Hệ thống đích bảo lưu trung thực 1-1 mã lỗi này**: Trả về HTTP 400 kèm mã lỗi `ErridnLoyalty.invalid_departmentcode`.
2. Mở Transaction CSDL độc lập qua hàm `DeleteXAsync`.
3. Chuẩn hóa mã qua `StdParam`.
4. **Kiểm tra tồn tại trong CSDL**: Nếu không tìm thấy bản ghi → Rollback, trả về HTTP 400 `ErridnLoyalty.Mst_Discipline_CheckDB_DisciplineCodeNotFound`.
5. Thực hiện xóa dòng khỏi bảng `Mst_Discipline`.
6. Commit Transaction, trả về HTTP 200 `{ Success = true, Data = null }` (`gate :434-435`).

#### Bảng mã lỗi `ErridnLoyalty.*`

| Mã lỗi | Điều kiện kích hoạt | Các tham số trả về trong `Params` |
|---|---|---|
| `ErridnLoyalty.invalid_departmentcode` | `DisciplineCode` rỗng hoặc body null (lỗi typo đặc thù nguồn) | `Check.DisciplineCode = null` |
| `ErridnLoyalty.Mst_Discipline_CheckDB_DisciplineCodeNotFound` | Bản ghi không tồn tại trong CSDL | `Check.DisciplineCode = <value>` |

---

### 2.7. DeleteMultiple (#138)

Xóa nhiều hình thức kỷ luật theo danh sách mã, lặp tuần tự từng Transaction riêng biệt (xóa một phần).

- **Route**: `POST /api/mst_discipline/deletemultiple`
- **Controller Action**: [`Mst_DisciplineControllerPort.DeleteMultiple`](../../Controllers/Port/Mst_DisciplineControllerPort.cs#L546-L587)
- **Tham chiếu nguồn**: Gate `Mst_DisciplineController.cs:455-520` → API `MstDisciplineController.cs:239-310` → Biz `MasterData.cs:34941-35186`

#### Bảng tham số Request

Payload JSON gửi qua Request Body là một mảng danh sách các đối tượng: `List<MstDisciplineDeleteReq>`:

| Tên tham số | Kiểu dữ liệu | Bắt buộc | Mặc định | Ý nghĩa & Quy tắc |
|---|---|---|---|---|
| `[].DisciplineCode` | `string?` | Có | | Mã hình thức kỷ luật cho từng phần tử cần xóa. |

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. **Kiểm tra body rỗng / null**: Nếu `req == null` → Trả về HTTP 400 `ErridnLoyalty.Mst_Discipline_Delete` (`Check.strJson = null`, `gate :465-472`).
2. **Xử lý mảng rỗng `[]`**: Vòng lặp không chạy, trả về ngay HTTP 200 `{ Success = true, Data = null }` (`gate :505-506`).
3. **Lặp tuần tự qua từng phần tử** (`gate :486-503`):
   - **Khác biệt quan trọng so với Delete đơn**: Gate của `DeleteMultiple` **không kiểm tra rỗng đối với `DisciplineCode`**. Nếu phần tử có mã rỗng, mã rỗng chuyển thẳng xuống `DeleteXAsync` và gây ra lỗi `ErridnLoyalty.Mst_Discipline_CheckDB_DisciplineCodeNotFound` (thay vì `invalid_departmentcode`).
   - Mỗi phần tử được thực thi xóa trong một Transaction độc lập đã commit.
4. **Hành vi Xóa một phần (Partial Delete)**:
   - Nếu phần tử thứ k gặp lỗi: Dừng vòng lặp ngay, trả về mã lỗi của phần tử thứ k.
   - Các phần tử đứng trước k đã được xóa vĩnh viễn khỏi CSDL và không bị rollback.
   - Các phần tử đứng sau k hoàn toàn không được duyệt tới.
5. Nếu toàn bộ phần tử hợp lệ: Trả về HTTP 200 `{ Success = true, Data = null }`.

#### Bảng mã lỗi `ErridnLoyalty.*`

| Mã lỗi | Điều kiện kích hoạt | Các tham số trả về trong `Params` |
|---|---|---|
| `ErridnLoyalty.Mst_Discipline_Delete` | Request body rỗng hoặc null | `Check.strJson = null` |
| `ErridnLoyalty.Mst_Discipline_CheckDB_DisciplineCodeNotFound` | Phần tử thứ k có mã rỗng hoặc không tồn tại trong CSDL | `Check.DisciplineCode = <value>` |

#### Minh chứng Kiểm thử Thực tế (từ lần chạy VERIFY #138)
- Danh sách ban đầu: CSDL có `KL01`, `KL02`, `KL03`.
- Request: `[{"DisciplineCode":"KL01"}, {"DisciplineCode":"XX"}, {"DisciplineCode":"KL02"}]`.
- Kết quả: HTTP 400 `ErridnLoyalty.Mst_Discipline_CheckDB_DisciplineCodeNotFound` với `Check.DisciplineCode = "XX"`.
- CSDL sau lệnh: Bản ghi `KL01` **đã bị xóa thành công**, bản ghi `KL02` **vẫn còn nguyên**.

---

## 3. Khác biệt So với Nguồn & Danh sách GAP (Known Gaps)

| Hạng mục | Trạng thái ở Nguồn | Triển khai ở Đích | Phân loại & Ghi chú GAP |
|---|---|---|---|
| **Xác thực & Phân quyền** | Gate kiểm tra `CheckGatewayAuthentication`, `CheckAuthorize`; Biz kiểm tra `Sys_User_CheckAuthorize`, `Sys_Access_CheckDenyV30`. | Bỏ qua toàn bộ kiểm tra phân quyền. | Bỏ qua có chủ đích (**Loại Đ**). |
| **Ghi nhật ký hệ thống** | Ghi bản ghi `RequestLog` và `ReturnLog` vào CSDL cho mọi thao tác. | Bỏ qua ghi log DB. | Bỏ qua có chủ đích (**Loại Đ**). |
| **Lỗi typo nguồn khi Delete** | Ném exception với mã lỗi sao chép từ Department: `"invalid_departmentcode"`. | Giữ nguyên mã lỗi `ErridnLoyalty.invalid_departmentcode` khi Delete đơn rỗng mã. | Khớp nguồn 1-1. |
| **Khai báo DDL nguồn** | Không có tập tin `.sql` tạo bảng `Mst_Discipline` trong repo nguồn. | Tự suy diễn kiểu cột từ model và code biz. Khóa chính `DisciplineCode` là khóa draft. | **GAP / Cần xác nhận**: Cần đối chiếu với DDL thực tế trên môi trường production. |
| **Không có khóa ngoại chặn xóa** | DDL nguồn không có FK nào tham chiếu tới `Mst_Discipline`. | Không giả lập khóa ngoại chặn xóa. | Khớp nguồn 1-1. |

---

## 4. Nhật ký Thay đổi & Đối chiếu Tiến độ (Changelog)

| STT Hạng mục | Tên Endpoint & Action | Trạng thái | Mã Commit Git | Mô tả tóm tắt thay đổi |
|---|---|---|---|---|
| **#132** | `Mst_DisciplineController/Search` | Đã hoàn thành | `408c2b7` | Khởi tạo model [`Mst_Discipline.cs`](../../Models/Port/Mst_Discipline.cs), cấu hình EF Core, Seeder 3 bản ghi demo và action tìm kiếm phân trang (toán tử '=' cho FlagActive). |
| **#133** | `Mst_DisciplineController/GetByDisciplineCode` | Đã hoàn thành | `70bd1db` | Tra cứu chi tiết một hình thức kỷ luật theo mã, trả về `null` khi mã rỗng không gọi DB. |
| **#134** | `Mst_DisciplineController/GetAllActive` | Đã hoàn thành | `7a9dcb4` | Lấy danh sách các hình thức kỷ luật đang hoạt động, trả về mảng phẳng trực tiếp. |
| **#135** | `Mst_DisciplineController/Create` | Đã hoàn thành | `08defd5` | Thêm mới hình thức kỷ luật, tích hợp sinh sequence `DISCIPLINECODE...` độc lập ghi đè mã client, kiểm tra tên rỗng. |
| **#136** | `Mst_DisciplineController/Update` | Đã hoàn thành | `60ea7cc` | Cập nhật thông tin hình thức kỷ luật, tự động hạ cờ về `'0'` nếu thiếu `FlagActive`, kiểm tra tên rỗng. |
| **#137** | `Mst_DisciplineController/Delete` | Đã hoàn thành | `bf2220f` | Xóa đơn lẻ hình thức kỷ luật, bảo lưu trung thực mã lỗi typo của nguồn `invalid_departmentcode`. |
| **#138** | `Mst_DisciplineController/DeleteMultiple` | Đã hoàn thành | `c71e69b` | Xóa danh sách hình thức kỷ luật theo từng transaction riêng biệt, thực thi cơ chế xóa một phần khi gặp lỗi. |
