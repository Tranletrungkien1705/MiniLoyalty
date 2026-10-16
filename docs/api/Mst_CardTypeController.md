# Tham chiếu API Mst_CardTypeController (Port 1-1)

| Thông tin | Giá trị |
|---|---|
| **Phạm vi** | Tài liệu tham chiếu chi tiết 10 endpoint đã port 1-1 của Mst_CardTypeController (#87 - #96) |
| **Cập nhật** | 2026-10-16 |
| **Phiên bản mã** | `93b6ace` |
| **Trạng thái** | Nháp — chờ review |

---

## 1. Tóm tắt Nghiệp vụ & Cấu trúc Dữ liệu

### 1.1. Mục đích Nghiệp vụ
API `Mst_CardTypeController` quản lý danh mục các loại thẻ và hạng thẻ thành viên (`Mst_CardType`) trong hệ thống MiniLoyalty. Phân hệ cung cấp các chức năng tìm kiếm phân trang theo từ khóa và trạng thái hoạt động (sắp xếp theo giá trị/ngưỡng hạng), lấy toàn bộ danh sách hạng thẻ đang hoạt động, tra cứu chi tiết một hạng thẻ theo mã, thêm mới (kiểm tra chặt chẽ ngưỡng giá trị dương và không trùng lặp), cập nhật thông tin/giá trị, xóa đơn lẻ và xóa hàng loạt (tích hợp cơ chế kiểm tra ràng buộc khóa ngoại tới thẻ, hội viên và các chính sách ưu đãi), nhập dữ liệu danh mục từ bảng tính Excel và xuất dữ liệu/mẫu Excel chuẩn.

### 1.2. Bảng Đối chiếu Nguồn Tra cứu (Source Code Mapping)

| Thành phần | Đường dẫn tập tin nguồn (`2022.7.Loyalty`) | Tập tin đích tương ứng (`MiniLoyalty`) |
|---|---|---|
| **Gate Controller** | `13.ClientGate\V10\idn.Loyalty.WebAPI\Controllers\Mst_CardTypeController.cs:25-886` | [`Controllers/Port/Mst_CardTypeControllerPort.cs`](../../Controllers/Port/Mst_CardTypeControllerPort.cs) |
| **API Backend** | `11.BackEnd\V10\idn.Loyalty.Biz.Web\Controllers\MstCardTypeController.cs:21-308` | Tích hợp trực tiếp vào [`Controllers/Port/Mst_CardTypeControllerPort.cs`](../../Controllers/Port/Mst_CardTypeControllerPort.cs) |
| **Nghiệp vụ (Biz)** | `11.BackEnd\V10\idn.Loyalty.Biz\Master.Loyalty.cs` (Select SQL `:5583-5737`, `:5329-5430`; Create `:5738-6099`; Update `:6100-6339`; Delete `:6428-6701`; CheckDB `:5264-5325`) | Tích hợp trực tiếp vào [`Controllers/Port/Mst_CardTypeControllerPort.cs`](../../Controllers/Port/Mst_CardTypeControllerPort.cs) |
| **Entity Model** | `12.Dev.Common\idn.Loyalty.Common\Models\Loyalty\Mst_CardType.cs:9-18` | [`Models/Port/Mst_CardType.cs:23-32`](../../Models/Port/Mst_CardType.cs#L23-L32) |
| **DDL Nguồn Thật** | `11.BackEnd\V10\05.Refs.Biz\Migrate\20221026.z11.UpdDB.02.MasterData.Loyalty.sql:82-90` | [`Models/Port/Mst_CardType.cs:35-45`](../../Models/Port/Mst_CardType.cs#L35-L45) |
| **Seeder Nguồn & Demo**| `Order => 11` (Dữ liệu gốc `20260518.z11.RankRN_BirthVoucher.sql:50-57`) | [`Models/Port/Mst_CardType.cs:51-71`](../../Models/Port/Mst_CardType.cs#L51-L71) |

### 1.3. Cấu trúc Bảng CSDL (`Mst_CardType`)

Bảng dữ liệu vật lý `Mst_CardType` gồm đúng 7 cột, được định nghĩa chính xác từ kịch bản DDL nguồn:

| Tên cột | Kiểu EF Core | Khóa | Cho phép NULL | Ghi chú & Ý nghĩa theo DDL nguồn |
|---|---|---|---|---|
| `CardType` | `string` | **PK** | Không | Mã loại thẻ/hạng thẻ. Khóa chính (`Varchar50 NOT NULL`, DDL `:83`). |
| `NetworkID` | `string` | | Không | Mã mạng nghiệp vụ (`Varchar50 NOT NULL`, DDL `:84`). |
| `CardTypeName` | `string` | | Không | Tên loại thẻ/hạng thẻ (`Varchar200 NOT NULL`, DDL `:85`). |
| `Value` | `double?` | | Có | Giá trị hoặc ngưỡng điểm của hạng thẻ (`MyValue`, DDL `:86`). |
| `FlagActive` | `string?` | | Có | Cờ trạng thái hoạt động: `"1"` (Đang hoạt động), `"0"` (Ngừng hoạt động) (`MyStatus`, DDL `:87`). |
| `LogLUDTimeUTC` | `DateTime?` | | Có | Thời điểm cập nhật cuối cùng theo chuẩn UTC (`MyDateTime`, DDL `:88`). |
| `LogLUBy` | `string?` | | Có | Tài khoản thực hiện cập nhật cuối cùng (`Varchar50`, DDL `:89`). |

**Dữ liệu mẫu khởi tạo (Seeder - `Mst_CardTypeSeeder`, Idempotent)**:
- 2 bản ghi nguồn thật: `R` (Hạng R cơ sở, Value = `0`, FlagActive `'1'`) và `N` (Hạng N khởi đầu, Value = `5`, FlagActive `'1'`) lấy nguyên văn từ script nguồn `20260518.z11.RankRN_BirthVoucher.sql:50-57`.
- 4 bản ghi demo: `I` (Hạng I, Value = `10`, `'1'`), `S` (Hạng S, Value = `20`, `'1'`), `G` (Hạng G, Value = `30`, `'1'`), `P` (Hạng P, Value = `40`, FlagActive `'0'`).

---

## 2. Đặc tả Chi tiết Từng Endpoint API

---

### 2.1. Search (#87)

Tìm kiếm danh mục loại thẻ phân trang theo từ khóa và trạng thái hoạt động.

- **Route**: `POST /api/mst_cardtype/search`
- **Controller Action**: [`Mst_CardTypeControllerPort.Search`](../../Controllers/Port/Mst_CardTypeControllerPort.cs#L129-L146)
- **Tham chiếu nguồn**: Gate `Mst_CardTypeController.cs:25-105` → API `MstCardTypeController.cs:239-308` → Biz `Master.Loyalty.cs:5583-5737`

#### Bảng tham số Request

Payload JSON gửi qua Request Body (`MstCardTypeSearchReq`), hỗ trợ body rỗng hoàn toàn:

| Tên tham số | Kiểu dữ liệu | Bắt buộc | Mặc định | Ý nghĩa & Quy tắc |
|---|---|---|---|---|
| `KeyWord` | `string?` | Không | `null` | Từ khóa tìm kiếm theo tên loại thẻ (`CardTypeName LIKE '%kw%'`, `:290`). |
| `FlagActive` | `string?` | Không | `null` | Danh sách trạng thái phân cách dấu phẩy. Áp dụng toán tử `IN ('a','b')` (`:298-305`). |
| `Ft_PageIndex` | `int` | Không | `0` | Chỉ số trang cần lấy (0-based). Nếu `< 0` tự động gán về `0`. |
| `Ft_PageSize` | `int` | Không | `10` | Số dòng trên mỗi trang. Nếu `<= 0` trả về mảng rỗng `DataList = []`. |

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Chuẩn hóa phân trang: `pageIndex = Math.Max(0, r.Ft_PageIndex)`, `pageSize = r.Ft_PageSize`.
2. Vị trí bản ghi bắt đầu: `start = (long)pageIndex * pageSize`.
3. Mệnh đề lọc qua `GetXAsync` ([`Mst_CardTypeControllerPort.cs:1037-1090`](../../Controllers/Port/Mst_CardTypeControllerPort.cs#L1037-L1090)):
   - **KeyWord**: `EF.Functions.Like((x.CardTypeName ?? "").ToLower(), PortQuery.Contains(keyWord), PortQuery.Esc)`.
   - **FlagActive**: Tách chuỗi dấu phẩy, lọc `ds.Contains((x.FlagActive ?? "").ToLower())`.
4. Đếm tổng số bản ghi: `itemCount = await ordered.CountAsync()` (`Master.Loyalty.cs:5636`).
5. **Thứ tự sắp xếp đặc thù của nguồn (`:5643, :5683`)**:
   - Phân trang trong CSDL sắp xếp theo `mcb.CardType ASC` (`:5643`).
   - Sau khi trích xuất trang dữ liệu, toàn bộ danh sách của trang được sắp xếp lại theo `mcb.Value ASC` (`:5683`).
6. Trích xuất trang: Nếu `pageSize <= 0` trả về danh sách rỗng (`:80-88`); nếu `pageSize > 0` gọi `.Skip((int)start).Take((int)pageSize)`.
7. Gán `MyIdxSeq = start + i` (0-based).
8. Tính `PageCount`: Nếu `dataList.Count == 0`, `PageCount = 0`; ngược lại `PageCount = ceil(itemCount / ps)`.

#### Bảng mã lỗi
Endpoint không phát sinh lỗi nghiệp vụ; luôn trả về HTTP 200.

#### Ví dụ Request / Response thực tế (từ lần chạy VERIFY #87)
- **Tìm kiếm mặc định (`{}`)**:
  - Request: `POST /api/mst_cardtype/search` kèm `{}`
  - Response (HTTP 200):
    ```json
    {
      "success": true,
      "data": {
        "dataList": [
          { "myIdxSeq": 3, "cardType": "R", "networkID": "DEMO", "cardTypeName": "Hạng R (cơ sở)", "value": 0, "flagActive": "1", "logLUDTimeUTC": null, "logLUBy": "seed" },
          { "myIdxSeq": 2, "cardType": "N", "networkID": "DEMO", "cardTypeName": "Hạng N (khởi đầu)", "value": 5, "flagActive": "1", "logLUDTimeUTC": null, "logLUBy": "seed" },
          { "myIdxSeq": 1, "cardType": "I", "networkID": "DEMO", "cardTypeName": "Hạng I", "value": 10, "flagActive": "1", "logLUDTimeUTC": null, "logLUBy": "seed" },
          { "myIdxSeq": 4, "cardType": "S", "networkID": "DEMO", "cardTypeName": "Hạng S", "value": 20, "flagActive": "1", "logLUDTimeUTC": null, "logLUBy": "seed" },
          { "myIdxSeq": 0, "cardType": "G", "networkID": "DEMO", "cardTypeName": "Hạng G", "value": 30, "flagActive": "1", "logLUDTimeUTC": null, "logLUBy": "seed" },
          { "myIdxSeq": 5, "cardType": "P", "networkID": "DEMO", "cardTypeName": "Hạng P", "value": 40, "flagActive": "0", "logLUDTimeUTC": null, "logLUBy": "seed" }
        ],
        "itemCount": 6,
        "pageCount": 1,
        "pageIndex": 0,
        "pageSize": 10
      }
    }
    ```
- **Lọc từ khóa tiếng Việt (`KeyWord = "khởi"`)**:
  - Request: `{"KeyWord": "khởi"}`
  - Response (HTTP 200): `itemCount = 1`, `dataList = [Hạng N (khởi đầu)]`.

---

### 2.2. GetAllActive (#88)

Lấy danh sách tất cả các loại thẻ đang hoạt động (`FlagActive = '1'`), sắp xếp tăng dần theo ngưỡng giá trị (`Value`).

- **Route**: `POST /api/mst_cardtype/getallactive`
- **Controller Action**: [`Mst_CardTypeControllerPort.GetAllActive`](../../Controllers/Port/Mst_CardTypeControllerPort.cs#L156-L166)
- **Tham chiếu nguồn**: Gate `Mst_CardTypeController.cs:106-215` → API `MstCardTypeController.cs:239-308` → Biz `Master.Loyalty.cs:5583-5737`

#### Bảng tham số Request
Endpoint không yêu cầu tham số request body.

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Tham số mặc định nguồn: `Ft_RecordStart = "0"`, `Ft_RecordCount = "123456000"`, `FlagActive = "1"` (`:134-135`). Mã `OrgID` được gán vào request tại gate nhưng không tham gia vào mệnh đề WHERE (`:283-308`).
2. Mệnh đề lọc: Áp dụng `FlagActive = '1'` qua toán tử `IN ('1')` (`:298-305`).
3. Sắp xếp: Theo thứ tự `CardType ASC` rồi sắp xếp trang theo `Value ASC` (`:5683`).
4. **Cấu trúc dữ liệu trả về**: Trả về **mảng phẳng (flat array)** `List<Mst_CardType>` trực tiếp trong thuộc tính `Data` (`:193-202`). Nếu không có dòng nào, trả về `Data = null` (`:129, :155-157`).

#### Bảng mã lỗi
Endpoint luôn trả về HTTP 200.

#### Ví dụ Request / Response thực tế (từ lần chạy VERIFY #88)
- Request: `POST /api/mst_cardtype/getallactive`
- Response (HTTP 200):
  ```json
  {
    "success": true,
    "data": [
      { "myIdxSeq": 3, "cardType": "R", "networkID": "DEMO", "cardTypeName": "Hạng R (cơ sở)", "value": 0, "flagActive": "1", ... },
      { "myIdxSeq": 2, "cardType": "N", "networkID": "DEMO", "cardTypeName": "Hạng N (khởi đầu)", "value": 5, "flagActive": "1", ... },
      { "myIdxSeq": 1, "cardType": "I", "networkID": "DEMO", "cardTypeName": "Hạng I", "value": 10, "flagActive": "1", ... },
      { "myIdxSeq": 4, "cardType": "S", "networkID": "DEMO", "cardTypeName": "Hạng S", "value": 20, "flagActive": "1", ... },
      { "myIdxSeq": 0, "cardType": "G", "networkID": "DEMO", "cardTypeName": "Hạng G", "value": 30, "flagActive": "1", ... }
    ]
  }
  ```
  *(Hạng `P` có FlagActive `'0'` bị loại bỏ; kết quả sắp xếp đúng theo thứ tự `Value` 0, 5, 10, 20, 30).*

---

### 2.3. GetByCardType (#89)

Tra cứu chi tiết một loại thẻ theo mã (`CardType`).

- **Route**: `POST /api/mst_cardtype/getbycardtype`
- **Controller Action**: [`Mst_CardTypeControllerPort.GetByCardType`](../../Controllers/Port/Mst_CardTypeControllerPort.cs#L173-L197)
- **Tham chiếu nguồn**: Gate `Mst_CardTypeController.cs:216-279` → API `MstCardTypeController.cs:239-308` → Biz `Master.Loyalty.cs:5583-5737`

#### Bảng tham số Request

Payload JSON gửi qua Request Body (`MstCardTypeGetByCardTypeReq`):

| Tên tham số | Kiểu dữ liệu | Bắt buộc | Mặc định | Ý nghĩa & Quy tắc |
|---|---|---|---|---|
| `CardType` | `string?` | Có | `null` | Mã loại thẻ cần tra cứu (`:238-241`). Nếu rỗng hoặc null, nguồn không truy vấn và trả về `Data = null`. |

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Kiểm tra mã: Nếu `string.IsNullOrWhiteSpace(req?.CardType)`, trả về ngay `Ok(new { Success = true, Data = (object?)null })` (`:235-236, :266`).
2. Mệnh đề lọc: Gọi `GetXAsync` với điều kiện lọc chính xác `cardTypeEqual = req.CardType`.
3. **Cờ hoạt động không lọc**: Nhánh `FlagActive` không được truyền ở gate nên không lọc cờ; **loại thẻ ngừng hoạt động (`P`, FlagActive `'0'`) vẫn tra cứu được**.
4. Phân trang nguồn: `Ft_RecordStart = "0"`, `Ft_RecordCount = "1"` (`:253-254`). Trả về `Data = rows[0]` hoặc `null` nếu không tìm thấy (`:260-263`).

#### Bảng mã lỗi
Endpoint luôn trả về HTTP 200.

#### Ví dụ Request / Response thực tế (từ lần chạy VERIFY #89)
- **Tra cứu hợp lệ**:
  - Request: `{"CardType": "n"}`
  - Response (HTTP 200): `{"success": true, "data": {"cardType": "N", "cardTypeName": "Hạng N (khởi đầu)", "value": 5, "myIdxSeq": 0, "flagActive": "1", ...}}`.
- **Tra cứu thẻ ngừng hoạt động (`P`)**:
  - Request: `{"CardType": "p"}`
  - Response (HTTP 200): Trả về bản ghi `P` với `flagActive = "0"`.
- **Mã không tồn tại hoặc rỗng**:
  - Request: `{"CardType": "X"}` hoặc `{"CardType": ""}` hoặc `{}`
  - Response (HTTP 200): `{"success": true, "data": null}`.

---

### 2.4. Create (#90)

Thêm mới một loại thẻ/hạng thẻ vào danh mục, kiểm tra chặt chẽ giá trị ngưỡng và ràng buộc NOT NULL.

- **Route**: `POST /api/mst_cardtype/create`
- **Controller Action**: [`Mst_CardTypeControllerPort.Create`](../../Controllers/Port/Mst_CardTypeControllerPort.cs#L282-L300)
- **Tham chiếu nguồn**: Gate `Mst_CardTypeController.cs:329-414` → API `MstCardTypeController.cs:21-91` → Biz `Master.Loyalty.cs:5738-6099`, `:5264-5325`

#### Bảng tham số Request

Payload JSON gửi qua Request Body (`MstCardTypeCreateReq`):

| Tên tham số | Kiểu dữ liệu | Bắt buộc | Mặc định | Ý nghĩa & Quy tắc |
|---|---|---|---|---|
| `CardType` | `string?` | Có | `null` | Mã loại thẻ (`:5787`). Chuẩn hóa qua `StdParam` (`Trim().ToUpper()`). |
| `NetworkID` | `string?` | Có | `null` | Mã mạng (`:5788`). Lấy từ JSON request, chuẩn hóa qua `StdParam`. Cột NOT NULL theo DDL nguồn. |
| `CardTypeName` | `string?` | Có | `null` | Tên loại thẻ (`:5789`). Chuẩn hóa qua `StdStr`. Cột NOT NULL theo DDL nguồn. |
| `Value` | `JsonElement?` | Có | `0` | Giá trị hoặc ngưỡng điểm (`:5790`). Chuyển đổi số qua `Convert.ToDouble(objValue)`. |

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Kiểm tra Body rỗng: Nếu null hoặc rỗng → Ném `ServiceException("strJson null")` (`:339-346`) → Trả về HTTP 400 `ErridnLoyalty.Mst_CardType_Create`.
2. Mở một transaction duy nhất (`Master.Loyalty.cs:5861 _cf.db.BeginTransaction()`).
3. Chuẩn hóa chuỗi và chuyển đổi kiểu:
   - `strCardType = StdParam(req.CardType)`, `strCardTypeName = StdStr(req.CardTypeName)`, `strNetworkID = StdParam(req.NetworkID)`.
   - Parse `dblValue = ConvertToDouble(req.Value)`: Nếu giá trị chuỗi không phải dạng số → Ném lỗi `Mst_CardType_Create` với `Check.objValue`.
4. **Chuỗi kiểm tra Guard nghiêm ngặt của nguồn**:
   - **Bước 1**: Nếu `string.IsNullOrEmpty(strCardType)` → Lỗi `Mst_CardType_Create_InvalidCardType` (`:6011-6021`).
   - **Bước 2**: Kiểm tra tồn tại trong DB qua `Mst_CardType_CheckDB(strCardType, Flag "0")` → Nếu đã có → Lỗi `Mst_CardType_CheckDB_CardTypeExist` (`:6023-6029`).
   - **Bước 3**: Nếu `dblValue <= 0` → Lỗi `Mst_CardType_Create_InvalidValue` (`:6031-6041`).
   - **Bước 4**: Kiểm tra trùng lặp `Value`: Nếu đã có bản ghi khác trong DB có `Value == dblValue` (so sánh số) → Lỗi `Mst_CardType_Create_InvalidValue` (`:6043-6066`).
   - **Bước 5**: Giả lập ràng buộc NOT NULL của DDL (`:6086-6090`): Nếu `strNetworkID` hoặc `strCardTypeName` rỗng → Rollback và ném lỗi `Mst_CardType_Create` với `Check.NotNull`.
5. Ghi dữ liệu 7 cột: `CardType`, `NetworkID`, `CardTypeName`, `Value = dblValue`, `FlagActive = "1"`, `LogLUDTimeUTC = DateTime.UtcNow`, `LogLUBy = "port"`.
6. Commit transaction và trả về `Ok(new { Success = true, Data = (object?)null })` (`:400-401`).

#### Bảng mã lỗi

| Mã lỗi (`ErridnLoyalty.*`) | HTTP Status | Tham số đi kèm | Điều kiện kích hoạt |
|---|---|---|---|
| `Mst_CardType_Create` | 400 | `Check.strJson = null` hoặc `Check.NotNull` | Body rỗng hoặc thiếu trường NOT NULL (`NetworkID`, `CardTypeName`). |
| `Mst_CardType_Create_InvalidCardType` | 400 | `Check.strCardType` | Mã loại thẻ rỗng hoặc null (`:6011-6021`). |
| `Mst_CardType_CheckDB_CardTypeExist` | 400 | `Check.CardType` | Mã loại thẻ đã tồn tại trong CSDL (`:6023-6029`). |
| `Mst_CardType_Create_InvalidValue` | 400 | `Check.Value` | Giá trị `Value <= 0` hoặc bị trùng `Value` với hạng thẻ đã có (`:6031-6066`). |

#### Ví dụ Request / Response thực tế (từ lần chạy VERIFY #90)
- **Tạo mới thành công**:
  - Request: `{"CardType": "d", "NetworkID": "DEMO", "CardTypeName": "Hạng D", "Value": 50}`
  - Response (HTTP 200): `{"success": true, "data": null}`. CSDL lưu `CardType = "D"`, `Value = 50`, `ItemCount` tăng từ 6 lên 7.
- **Trùng giá trị `Value` (đã có ở hạng `S` với Value = 20)**:
  - Request: `{"CardType": "K", "NetworkID": "DEMO", "CardTypeName": "Hạng K", "Value": 20}`
  - Response (HTTP 400):
    ```json
    {
      "success": false,
      "errorCode": "ErridnLoyalty.Mst_CardType_Create_InvalidValue",
      "params": { "Check.Value": 20 }
    }
    ```
- **Giá trị `Value <= 0`**:
  - Request: `{"CardType": "K", "NetworkID": "DEMO", "CardTypeName": "Hạng K", "Value": 0}`
  - Response (HTTP 400): `ErridnLoyalty.Mst_CardType_Create_InvalidValue`.

---

### 2.5. Update (#91)

Cập nhật tên, giá trị ngưỡng và trạng thái hoạt động của loại thẻ.

- **Route**: `POST /api/mst_cardtype/update`
- **Controller Action**: [`Mst_CardTypeControllerPort.Update`](../../Controllers/Port/Mst_CardTypeControllerPort.cs#L442-L529)
- **Tham chiếu nguồn**: Gate `Mst_CardTypeController.cs:418-485` → API `MstCardTypeController.cs:93-163` → Biz `Master.Loyalty.cs:6100-6339`, `:5264-5325`

#### Bảng tham số Request

Payload JSON gửi qua Request Body (`MstCardTypeUpdateReq`):

| Tên tham số | Kiểu dữ liệu | Bắt buộc | Mặc định | Ý nghĩa & Quy tắc |
|---|---|---|---|---|
| `CardType` | `string?` | Có | `null` | Mã loại thẻ cần cập nhật. Chuẩn hóa qua `StdParam`. |
| `NetworkID` | `string?` | Không | `null` | Nhận tại gate nhưng không cập nhật vào CSDL. |
| `CardTypeName` | `string?` | Có | `null` | Tên loại thẻ mới. Cột NOT NULL theo DDL nguồn. Chuẩn hóa qua `StdStr`. |
| `Value` | `JsonElement?` | Không | `0` | Giá trị hoặc ngưỡng điểm mới. Chuyển đổi qua `Convert.ToDouble`. |
| `FlagActive` | `string?` | Không | `"0"` | Trạng thái hoạt động. Chuẩn hóa qua `StdFlag` (thiếu đổi về `"0"`). |

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Kiểm tra Body rỗng: Nếu null hoặc rỗng → Lỗi `Mst_CardType_Update` (`:428-435`).
2. Mở một transaction duy nhất (`Master.Loyalty.cs:6228`).
3. Chuẩn hóa dữ liệu: `strCardType = StdParam(req.CardType)`, `strCardTypeName = StdStr(req.CardTypeName)`, `strFlagActive = StdFlag(req.FlagActive)`.
4. Chuỗi kiểm tra Guard nguồn:
   - **Bước 1**: Kiểm tra tồn tại qua `Mst_CardType_CheckDB(strCardType, Flag "1")` (`:6395-6401`): Nếu không tìm thấy bản ghi → Lỗi `Mst_CardType_CheckDB_CardTypeNotFound`.
   - **Bước 2 (Đặc thù Update nguồn)**: Nguồn **CỐ Ý KHÔNG KIỂM TRA `Value <= 0`** và **KHÔNG KIỂM TRA TRÙNG `Value`** khi Update (`:6377-6427`, khác Create #90).
   - **Bước 3**: Vì `CardTypeName` là cột NOT NULL (`DDL :85`), nếu cập nhật tên rỗng → Lỗi `Mst_CardType_Update` với `Check.NotNull = "CardTypeName"` (`:6211`).
5. Cập nhật dữ liệu: `entity.CardTypeName = strCardTypeName`, `entity.Value = dblValue`, `entity.FlagActive = strFlagActive`, `entity.LogLUDTimeUTC = DateTime.UtcNow`, `entity.LogLUBy = "port"`.
6. Commit transaction và trả về `Ok(new { Success = true, Data = (object?)null })` (`:471-472`).

#### Bảng mã lỗi

| Mã lỗi (`ErridnLoyalty.*`) | HTTP Status | Tham số đi kèm | Điều kiện kích hoạt |
|---|---|---|---|
| `Mst_CardType_Update` | 400 | `Check.strJson = null` hoặc `Check.NotNull` | Body rỗng hoặc cập nhật `CardTypeName` rỗng. |
| `Mst_CardType_CheckDB_CardTypeNotFound` | 400 | `Check.CardType` | Mã loại thẻ không tồn tại trong CSDL (`:6395-6401`). |

---

### 2.6. Delete (#92)

Xóa một loại thẻ khỏi danh mục, kiểm tra chặt chẽ các ràng buộc khóa ngoại tới hội viên, thẻ và chính sách.

- **Route**: `POST /api/mst_cardtype/delete`
- **Controller Action**: [`Mst_CardTypeControllerPort.Delete`](../../Controllers/Port/Mst_CardTypeControllerPort.cs#L564-L583)
- **Tham chiếu nguồn**: Gate `Mst_CardTypeController.cs:489-552` → API `MstCardTypeController.cs:166-236` → Biz `Master.Loyalty.cs:6428-6701`, `:5264-5325`

#### Bảng tham số Request

Payload JSON gửi qua Request Body (`MstCardTypeDeleteReq`):

| Tên tham số | Kiểu dữ liệu | Bắt buộc | Mặc định | Ý nghĩa & Quy tắc |
|---|---|---|---|---|
| `CardType` | `string?` | Có | `null` | Mã loại thẻ cần xóa (`:489, :513`). Chuẩn hóa qua `StdParam`. |

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Kiểm tra mã rỗng tại Gate: Nếu `string.IsNullOrWhiteSpace(req?.CardType)` → Ném `ServiceException("invalid_CartType", "invalid_CartType")` (**giữ nguyên typo nguồn chữ "CartType"**, `:508, :538`) → Trả về **HTTP 400 `ErridnLoyalty.invalid_CartType`**.
2. Mở một transaction duy nhất (`Master.Loyalty.cs:6540`).
3. Chuẩn hóa: `strCardType = StdParam(req?.CardType)`.
4. Kiểm tra tồn tại qua `Mst_CardType_CheckDB(strCardType, Flag "1")` (`:6677-6682`) → Không thấy → Lỗi `Mst_CardType_CheckDB_CardTypeNotFound`.
5. **Kiểm tra ràng buộc Khóa ngoại (FK) đối với các bảng con trong CSDL**:
   - `Crd_Card.CardTypeInit` (`FK` = `"Crd_Card.CardTypeInit"`)
   - `Crd_Card.CardTypeUse` (`FK` = `"Crd_Card.CardTypeUse"`)
   - `Crd_Member.CardTypeInit` (`FK` = `"Crd_Member.CardTypeInit"`)
   - `Crd_Member.CardTypeUse` (`FK` = `"Crd_Member.CardTypeUse"`)
   - `Mst_RankPolicy.CardType` (`FK` = `"Mst_RankPolicy.CardType"`, bảng port ở #227)
   - `Mst_PolicyMoneyToPointServiceDtl.CardType` (`FK` = `"Mst_PolicyMoneyToPointServiceDtl.CardType"`, bảng port ở #194)
   - `Mst_BirthPolicyDtl.CardType` (`FK` = `"Mst_BirthPolicyDtl.CardType"`, bảng port ở #70)
   - Nếu tồn tại bản ghi con ở bất kỳ bảng nào trên → Rollback và trả về HTTP 400 `ErridnLoyalty.Mst_CardType_Delete` kèm thông tin bảng/cột vi phạm.
6. Xóa bản ghi khỏi bảng `Mst_CardType` (`:6692-6695`).
7. Commit transaction và trả về `Ok(new { Success = true, Data = (object?)null })` (`:532-533`).

#### Bảng mã lỗi

| Mã lỗi (`ErridnLoyalty.*`) | HTTP Status | Tham số đi kèm | Điều kiện kích hoạt |
|---|---|---|---|
| `invalid_CartType` | 400 | `Check.CardType` | Mã loại thẻ rỗng hoặc null tại tầng gate (`:538`). |
| `Mst_CardType_CheckDB_CardTypeNotFound` | 400 | `Check.CardType` | Mã loại thẻ không tồn tại trong CSDL (`:6677-6682`). |
| `Mst_CardType_Delete` | 400 | `Check.CardType`, `FK` | Vướng ràng buộc khóa ngoại tới các bảng thẻ/hội viên/chính sách. |

#### Ví dụ Request / Response thực tế (từ lần chạy VERIFY #92)
- **Xóa vướng khóa ngoại thẻ (`G`)**:
  - Request: `{"CardType": "G"}`
  - Response (HTTP 400):
    ```json
    {
      "success": false,
      "errorCode": "ErridnLoyalty.Mst_CardType_Delete",
      "params": {
        "Check.CardType": "G",
        "FK": "Crd_Card.CardTypeUse"
      }
    }
    ```
- **Xóa thành công loại thẻ không có bản ghi con (`R`)**:
  - Request: `{"CardType": "r"}`
  - Response (HTTP 200): `{"success": true, "data": null}`. Bản ghi `R` bị xóa.

---

### 2.7. DeleteMultiple (#93)

Xóa danh sách nhiều loại thẻ, thực thi theo cơ chế từng transaction độc lập (xóa một phần).

- **Route**: `POST /api/mst_cardtype/deletemultiple`
- **Controller Action**: [`Mst_CardTypeControllerPort.DeleteMultiple`](../../Controllers/Port/Mst_CardTypeControllerPort.cs#L615-L665)
- **Tham chiếu nguồn**: Gate `Mst_CardTypeController.cs:554-618` → API `WA_Mst_CardType_Delete` → Biz `Mst_CardType_Delete`

#### Bảng tham số Request

Payload JSON gửi qua Request Body: Mảng đối tượng `List<MstCardTypeDeleteReq>`.

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Kiểm tra Body rỗng: Nếu null hoặc rỗng → Ném `ServiceException("strJson null")` (`:564-571`) → Lỗi `Mst_CardType_Delete`.
2. Mảng rỗng `[]`: Không thực thi vòng lặp, trả về `Ok(new { Success = true, Data = (object?)null })`.
3. Duyệt tuần tự từng phần tử trong danh sách:
   - Mỗi phần tử thực thi trong **1 transaction riêng biệt** (`Master.Loyalty.cs:6540`).
   - Nếu phần tử có mã rỗng, lỗi phát sinh tại `Mst_CardType_CheckDB` → Lỗi `ErridnLoyalty.Mst_CardType_CheckDB_CardTypeNotFound` (khác Delete đơn kiểm tra tại gate).
   - **Hành vi Xóa một phần (Partial Delete)**: Nếu phần tử thứ `k` bị lỗi (ví dụ vướng FK hoặc không tìm thấy), vòng lặp dừng ngay lập tức. **Các phần tử trước `k` đã bị xóa và commit thành công vào CSDL không bị rollback**; các phần tử sau `k` hoàn toàn chưa được xử lý (`:585-611`).
4. Thành công: Trả về `Ok(new { Success = true, Data = (object?)null })` (`:604-605`).

#### Bảng mã lỗi

| Mã lỗi (`ErridnLoyalty.*`) | HTTP Status | Tham số đi kèm | Điều kiện kích hoạt |
|---|---|---|---|
| `Mst_CardType_Delete` | 400 | `Check.strJson = null` hoặc `FK` | Body rỗng hoặc phần tử vướng ràng buộc khóa ngoại. |
| `Mst_CardType_CheckDB_CardTypeNotFound` | 400 | `Check.CardType` | Phần tử thứ `k` có mã không tồn tại hoặc mã rỗng. |

#### Minh chứng Kiểm thử Thực tế (từ lần chạy VERIFY #93)
- Request mảng 3 phần tử: `[{"CardType": "R"}, {"CardType": "G"}, {"CardType": "D"}]` (trong đó `G` vướng FK `Crd_Card`).
- Response (HTTP 400): `ErridnLoyalty.Mst_CardType_Delete` kèm `FK = "Crd_Card.CardTypeUse"`.
- CSDL: `R` **đã bị xóa vĩnh viễn**, trong khi `G` và `D` **vẫn còn nguyên vẹn trong CSDL**.

---

### 2.8. Import (#94)

Nhập danh mục loại thẻ từ tập tin Excel `.xlsx`.

- **Route**: `POST /api/mst_cardtype/import`
- **Content-Type**: `multipart/form-data`
- **Controller Action**: [`Mst_CardTypeControllerPort.Import`](../../Controllers/Port/Mst_CardTypeControllerPort.cs#L803-L910)
- **Tham chiếu nguồn**: Gate `Mst_CardTypeController.cs:622-768` → API `WA_Mst_CardType_Create` → Biz `Mst_CardType_Create`

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Kiểm tra file upload (`:646-651`): File rỗng, thiếu file hoặc sai đuôi `.xlsx` → Lỗi `Mst_CardType_Import` ("File excel import không hợp lệ").
2. Đọc file qua [`PortExcel.ReadSheet`](../../Data/PortExcel.cs):
   - 0 dòng dữ liệu → Lỗi `Mst_CardType_Import` ("File excel import không có dữ liệu").
   - Số cột tiêu đề khác 3 (`GetImportDicColumsTemplate().Count != 3`) → Lỗi `Mst_CardType_Import` ("File excel import không hợp lệ").
3. **Kiểm tra Validate toàn bộ dòng TRƯỚC khi tạo** (`:672-699`):
   - `CardType` rỗng → Lỗi `Mst_CardType_Import` kèm `Message = "Mã hạng thẻ không được trống"` (`:675-679`).
   - `CardTypeName` rỗng → Lỗi `Mst_CardType_Import` kèm `Message = "Tên hạng thẻ không được trống"` (`:680-684`).
   - `Value` rỗng → Lỗi `Mst_CardType_Import` kèm `Message = "Giá trị không được trống"` (`:685-689`).
   - Nếu có bất kỳ dòng nào vi phạm, dừng ngay và **không có dòng nào được tạo vào CSDL**.
4. **Lưu từng dòng (Partial Import)**:
   - `NetworkID`: Lấy từ cấu hình `IConfiguration["Biz_NetworkID"] ?? "1001"` (GAP: nguồn lấy từ `orgUserUI.NetworkId`).
   - Duyệt từng dòng, gọi `CreateXAsync` trong **1 transaction riêng biệt cho mỗi dòng**.
   - Dòng thứ `k` bị lỗi (ví dụ trùng mã `Mst_CardType_CheckDB_CardTypeExist`, sai hoặc trùng `Value`), vòng lặp dừng ngay và trả về lỗi của dòng `k`.
   - **Các dòng trước `k` đã được commit vào CSDL**, các dòng sau `k` không được duyệt.
5. Thành công: Trả về `Ok(new { Success = true, Data = (object?)null })` (`:748-750`).

#### Bảng mã lỗi

| Mã lỗi (`ErridnLoyalty.*`) | HTTP Status | Tham số đi kèm | Điều kiện kích hoạt |
|---|---|---|---|
| `Mst_CardType_Import` | 400 | `Message = "File excel import không hợp lệ"` | File rỗng, sai đuôi hoặc số cột khác 3. |
| `Mst_CardType_Import` | 400 | `Message = "File excel import không có dữ liệu"` | File chỉ có tiêu đề, không có dữ liệu. |
| `Mst_CardType_Import` | 400 | `Message = "Mã hạng thẻ không được trống"` | Dòng có `CardType` rỗng. |
| `Mst_CardType_Import` | 400 | `Message = "Tên hạng thẻ không được trống"` | Dòng có `CardTypeName` rỗng. |
| `Mst_CardType_Import` | 400 | `Message = "Giá trị không được trống"` | Dòng có `Value` rỗng. |
| `Mst_CardType_CheckDB_CardTypeExist` | 400 | `Check.CardType` | Dòng thứ `k` bị trùng mã trong CSDL. |
| `Mst_CardType_Create_InvalidValue` | 400 | `Check.Value` | Dòng thứ `k` có `Value <= 0` hoặc trùng `Value` đã có. |

#### Minh chứng Kiểm thử Thực tế (từ lần chạy VERIFY #94)
- File gồm 3 dòng: `[F, Hạng F, 70]`, `[n, x, 80]` (trùng mã seed `N`), `[H, Hạng H, 90]`.
- Kết quả: HTTP 400 `ErridnLoyalty.Mst_CardType_CheckDB_CardTypeExist`.
- CSDL: `F` **đã được tạo thành công** (`Value = 70`), còn `H` **không được tạo**.

---

### 2.9. Export (#95)

Xuất danh sách loại thẻ ra tập tin Excel `.xlsx`.

- **Route**: `POST /api/mst_cardtype/export`
- **Controller Action**: [`Mst_CardTypeControllerPort.Export`](../../Controllers/Port/Mst_CardTypeControllerPort.cs#L924-L968)
- **Tham chiếu nguồn**: Gate `Mst_CardTypeController.cs:773-851` → API `WA_Mst_CardType_Get` → Biz `Master.Loyalty.cs:5583-5737`

#### Bảng tham số Request

Payload JSON gửi qua Request Body (`MstCardTypeExportReq`):

| Tên tham số | Kiểu dữ liệu | Bắt buộc | Mặc định | Ý nghĩa & Quy tắc |
|---|---|---|---|---|
| `OrgID` | `string?` | Không | `null` | Nhận tại gate nhưng không lọc (`:283-308`). |
| `KeyWord` | `string?` | Không | `null` | Lọc theo từ khóa tên loại thẻ (`CardTypeName LIKE '%kw%'`). |
| `FlagActive` | `string?` | Không | `null` | Lọc theo cờ hoạt động (toán tử `IN`). |

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Gọi `GetXAsync(r.KeyWord, r.FlagActive, 0, 123456000, null)` để trích xuất dữ liệu (`:812-813`).
2. Sắp xếp thứ tự: Toàn bộ danh sách xuất ra được sắp xếp tăng dần theo `Value ASC` (`:5683`).
3. Kiểm tra dữ liệu rỗng: Nếu không có bản ghi nào, ném `ServiceException("Không có dữ liệu")` (`:837`) → Trả về HTTP 400 `ErridnLoyalty.Mst_CardType_Export` kèm `Message = "Không có dữ liệu"`.
4. Từ điển cột: `GetImportDicColums()` gồm đúng 4 cột (`:823, :993-1001`):
   - `CardType`: `"Mã hạng thẻ (*)"`
   - `CardTypeName`: `"Tên hạng thẻ"`
   - `Value`: `"Thứ tự"`
   - `FlagActive`: `"Trạng thái"`
5. Sinh file Excel sheet `"Mst_CardType"` qua [`PortExcel.ExportToExcelFromList`](../../Data/PortExcel.cs).
6. Trả về `Ok(new { Success = true, Data = new { Url = baseAddress + virtualPath } })` (`:829-832`).

#### Bảng mã lỗi

| Mã lỗi (`ErridnLoyalty.*`) | HTTP Status | Tham số đi kèm | Điều kiện kích hoạt |
|---|---|---|---|
| `Mst_CardType_Export` | 400 | `Message = "Không có dữ liệu"` | Không có bản ghi nào thỏa mãn điều kiện lọc (`:837`). |

---

### 2.10. ExportTemplate (#96)

Xuất tập tin mẫu Excel dùng cho chức năng Import loại thẻ/hạng thẻ.

- **Route**: `POST /api/mst_cardtype/exporttemplate`
- **Controller Action**: [`Mst_CardTypeControllerPort.ExportTemplate`](../../Controllers/Port/Mst_CardTypeControllerPort.cs#L979-L998)
- **Tham chiếu nguồn**: Gate `Mst_CardTypeController.cs:854-886`

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Không nhận body, không kiểm tra quyền, không đọc CSDL (`:862`).
2. Khởi tạo danh sách rỗng `List<Mst_CardType>`.
3. Từ điển cột: `GetImportDicColumsTemplate()` gồm đúng 3 cột (`:864, :1003-1011`):
   - `CardType`: `"Mã hạng thẻ (*)"`
   - `CardTypeName`: `"Tên hạng thẻ"`
   - `Value`: `"Thứ tự"`
4. Sinh file Excel rỗng có dòng tiêu đề sheet `"Mst_CardType"`.
5. Trả về `Ok(new { Success = true, Data = new { Url = baseAddress + virtualPath } })` (`:868-872`).

#### Bảng mã lỗi
Endpoint luôn trả về HTTP 200.

---

## 3. Khác biệt So với Nguồn & Danh sách GAP (Known Gaps)

| Hạng mục | Trạng thái ở Nguồn | Triển khai ở Đích | Phân loại & Ghi chú GAP |
|---|---|---|---|
| **Xác thực & Phân quyền** | Gate kiểm tra `CheckGatewayAuthentication`, `CheckAuthorize`; Biz kiểm tra `Sys_User_CheckAuthorize`. | Bỏ qua toàn bộ kiểm tra phân quyền. | Bỏ qua có chủ đích (**Loại Đ**). |
| **Ghi nhật ký hệ thống** | Ghi bản ghi `RequestLog` và `ReturnLog` vào CSDL cho mọi thao tác. | Bỏ qua ghi log DB. | Bỏ qua có chủ đích (**Loại Đ**). |
| **Typo lỗi xóa `invalid_CartType`** | Gate nguồn ném `ServiceException("invalid_CartType", "invalid_CartType")`. | Giữ nguyên văn typo chữ `"CartType"` thành `ErridnLoyalty.invalid_CartType`. | Khớp nguồn 1-1. |
| **Ràng buộc khóa ngoại khi xóa** | SQL Server tự động chặn xóa nếu còn FK trong CSDL. | Giả lập kiểm tra FK trong mã nguồn tới các bảng thẻ/hội viên/chính sách đã port. | Khớp logic nghiệp vụ nguồn 1-1. |
| **Bảng FK chưa port** | `Mst_PromotionProgramSpec.CardType` (DDL nguồn khai FK nhưng bảng Spec không có cột này — nghi vấn DDL lỗi của nguồn). | Chưa kiểm tra bảng này do chưa port bảng. | **GAP**: Cần xác nhận với CSDL thật. |
| **Mã mạng trong Import** | Nguồn lấy `NetworkID` từ phiên đăng nhập `orgUserUI.NetworkId`. | Đích lấy từ cấu hình `IConfiguration["Biz_NetworkID"] ?? "1001"`. | **GAP**: Chưa có session người dùng. |

---

## 4. Nhật ký Thay đổi & Đối chiếu Tiến độ (Changelog)

| STT Hạng mục | Tên Endpoint & Action | Trạng thái | Mã Commit Git | Mô tả tóm tắt thay đổi |
|---|---|---|---|---|
| **#87** | `Mst_CardTypeController/Search` | Đã hoàn thành | `f9f91c1` | Khởi tạo model [`Mst_CardType.cs`](../../Models/Port/Mst_CardType.cs) từ DDL thật, cấu hình EF Core, Seeder 6 bản ghi và action tìm kiếm phân trang. |
| **#88** | `Mst_CardTypeController/GetAllActive` | Đã hoàn thành | `737757f` | Lấy danh sách loại thẻ đang hoạt động sắp theo `Value ASC`, trả về mảng phẳng trực tiếp. |
| **#89** | `Mst_CardTypeController/GetByCardType` | Đã hoàn thành | `8569655` | Tra cứu chi tiết một loại thẻ theo mã, không lọc trạng thái hoạt động. |
| **#90** | `Mst_CardTypeController/Create` | Đã hoàn thành | `262abcd` | Thêm mới loại thẻ, kiểm tra trùng mã, kiểm tra `Value > 0` và không trùng `Value`. |
| **#91** | `Mst_CardTypeController/Update` | Đã hoàn thành | `43e777c` | Cập nhật thông tin loại thẻ, không kiểm tra trùng `Value` khi update theo đúng nguồn. |
| **#92** | `Mst_CardTypeController/Delete` | Đã hoàn thành | `08a9140` | Xóa đơn lẻ loại thẻ, phát sinh lỗi typo `invalid_CartType`, tích hợp giả lập khóa ngoại con. |
| **#93** | `Mst_CardTypeController/DeleteMultiple` | Đã hoàn thành | `5b4de6e` | Xóa danh sách loại thẻ theo từng transaction độc lập, thực thi cơ chế xóa một phần. |
| **#94** | `Mst_CardTypeController/Import` | Đã hoàn thành | `7d595b1` | Nhập danh mục từ Excel, validate toàn bộ dòng trước khi tạo và thực thi cơ chế nhập một phần. |
| **#95** | `Mst_CardTypeController/Export` | Đã hoàn thành | `934cdcd` | Xuất danh sách loại thẻ ra Excel, sử dụng từ điển 4 cột, sắp xếp tăng dần theo `Value`. |
| **#96** | `Mst_CardTypeController/ExportTemplate` | Đã hoàn thành | `0bd575c` | Xuất file mẫu Excel gồm 3 cột tiêu đề, không truy vấn CSDL. |
