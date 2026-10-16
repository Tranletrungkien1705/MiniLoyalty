# Tham chiếu API Mst_PromotionProgramController (Port 1-1)

| Thông tin | Giá trị |
|---|---|
| **Phạm vi** | Tài liệu tham chiếu chi tiết 8 endpoint đã port 1-1 của Mst_PromotionProgramController (#205 - #212) |
| **Cập nhật** | 2026-10-16 |
| **Phiên bản mã** | `ed28f8f` |
| **Trạng thái** | Nháp — chờ review |

---

## 1. Tóm tắt Nghiệp vụ & Cấu trúc Dữ liệu

### 1.1. Mục đích Nghiệp vụ
API `Mst_PromotionProgramController` quản lý danh mục các chương trình khuyến mại (`Mst_PromotionProgram`), chính sách chi tiết cho từng hạng thẻ (`Mst_PromotionProgramDtl`) và danh sách các đại lý được chỉ định áp dụng chương trình (`Mst_PromotionProgramSpec`) trong hệ thống MiniLoyalty. Module cung cấp các chức năng tìm kiếm phân trang theo từ khóa, lấy danh sách tất cả chương trình đang hoạt động, tra cứu chi tiết chương trình cùng 2 danh sách bảng con (Dtl và Spec), lưu mới theo cơ chế upsert xóa-ghi lại trong 1 transaction (tự động kích hoạt cờ hoạt động), cập nhật chương trình (tự động chốt ngày kết thúc khi ngừng kích hoạt), xóa chương trình (chặn xóa chương trình chưa bắt đầu hiệu lực ở tương lai, bảo lưu mã lỗi typo nguồn), xuất danh sách ra tập tin Excel và xuất file mẫu chuẩn.

### 1.2. Bảng Đối chiếu Nguồn Tra cứu (Source Code Mapping)

| Thành phần | Đường dẫn tập tin nguồn (`2022.7.Loyalty`) | Tập tin đích tương ứng (`MiniLoyalty`) |
|---|---|---|
| **Gate Controller** | `13.ClientGate\V10\idn.Loyalty.WebAPI\Controllers\Mst_PromotionProgramController.cs:32-743` | [`Controllers/Port/Mst_PromotionProgramControllerPort.cs`](../../Controllers/Port/Mst_PromotionProgramControllerPort.cs) |
| **API Backend** | `11.BackEnd\V10\idn.Loyalty.Biz.Web\Controllers\MstPromotionProgramController.cs:22-163` | Tích hợp trực tiếp vào [`Controllers/Port/Mst_PromotionProgramControllerPort.cs`](../../Controllers/Port/Mst_PromotionProgramControllerPort.cs) |
| **Nghiệp vụ (Biz)** | `11.BackEnd\V10\idn.Loyalty.Biz\Master.Loyalty.cs` (Get `:13997-14543`; Save/SaveX `:13179-13995`) | Tích hợp trực tiếp vào [`Controllers/Port/Mst_PromotionProgramControllerPort.cs`](../../Controllers/Port/Mst_PromotionProgramControllerPort.cs) |
| **Entity Header** | `20221026.CreateDatabase.sql:490-509` (DDL 11 cột) | [`Models/Port/Mst_PromotionProgram.cs:33-46`](../../Models/Port/Mst_PromotionProgram.cs#L33-L46) |
| **Entity Detail** | `20221026.CreateDatabase.sql:466-484` (DDL 10 cột) | [`Models/Port/Mst_PromotionProgram.cs:73-86`](../../Models/Port/Mst_PromotionProgram.cs#L73-L86) |
| **Entity Spec** | `20221026.CreateDatabase.sql:444-460` (DDL 8 cột) | [`Models/Port/Mst_PromotionProgram.cs:111-122`](../../Models/Port/Mst_PromotionProgram.cs#L111-L122) |
| **Seeder Demo** | `Order => 30` (danh mục `Mst_*` dùng 10–99) | [`Models/Port/Mst_PromotionProgram.cs:145-236`](../../Models/Port/Mst_PromotionProgram.cs#L145-L236) |

### 1.3. Cấu trúc Bảng CSDL

Hệ thống quản lý chương trình khuyến mại gồm 3 bảng vật lý chuẩn theo DDL `20221026.CreateDatabase.sql`:

#### 1. Bảng Header `Mst_PromotionProgram` (Đúng 11 cột, Khóa chính `PrProgramCode`):

| Tên cột | Kiểu EF Core | Khóa | Cho phép NULL | Ghi chú & Ý nghĩa |
|---|---|---|---|---|
| `PrProgramCode` | `string` | **PK** | Không | Mã chương trình khuyến mãi (Varchar50 NOT NULL, PK :506-509). |
| `NetworkID` | `string` | | Không | Mã mạng nghiệp vụ (Varchar50 NOT NULL). |
| `OrgID` | `string?` | | Có | Mã tổ chức (Varchar50). |
| `PrProgramName` | `string` | | Không | Tên chương trình khuyến mãi (Varchar200 NOT NULL). |
| `EffDateStart` | `DateTime` | | Không | Ngày bắt đầu hiệu lực (MyDateTime NOT NULL). |
| `EffDateEnd` | `DateTime` | | Không | Ngày kết thúc hiệu lực (MyDateTime NOT NULL). |
| `FlagAllDL` | `string?` | | Có | Cờ áp dụng tất cả đại lý: `"1"` (Tất cả), `"0"` (Theo bảng chỉ định Spec). |
| `FlagActive` | `string?` | | Có | Cờ trạng thái: `"1"` (Kích hoạt), `"0"` (Không kích hoạt). |
| `Remark` | `string?` | | Có | Ghi chú thêm (Varchar400). |
| `LogLUDTimeUTC` | `DateTime?` | | Có | Thời điểm cập nhật cuối cùng UTC. |
| `LogLUBy` | `string?` | | Có | Tài khoản cập nhật cuối cùng (mặc định gán `"port"`). |

#### 2. Bảng Detail `Mst_PromotionProgramDtl` (Đúng 10 cột, Khóa chính ghép `(PrProgramCode, CardType)`):

| Tên cột | Kiểu EF Core | Khóa | Cho phép NULL | Ghi chú & Ý nghĩa |
|---|---|---|---|---|
| `PrProgramCode` | `string` | **PK1** | Không | Mã chương trình (Varchar50 NOT NULL, liên kết tới Header). |
| `CardType` | `string` | **PK2** | Không | Mã hạng thẻ thành viên (Varchar50 NOT NULL). |
| `NetworkID` | `string?` | | Có | Mã mạng nghiệp vụ (Varchar50). |
| `OrgID` | `string?` | | Có | Mã tổ chức (nvarchar(10)). |
| `Qty` | `double?` | | Có | Số lượng ưu đãi (MyValue). |
| `Unit` | `string?` | | Có | Đơn vị tính (Varchar50). |
| `FlagActive` | `string?` | | Có | Cờ hoạt động: `"1"`/`"0"`. |
| `Remark` | `string?` | | Có | Ghi chú mức ưu đãi. |
| `LogLUDTimeUTC` | `DateTime?` | | Có | Thời điểm cập nhật cuối cùng UTC. |
| `LogLUBy` | `string?` | | Có | Tài khoản cập nhật cuối cùng. |

#### 3. Bảng Chỉ định Đại lý `Mst_PromotionProgramSpec` (Đúng 8 cột, Khóa chính ghép `(DLCode, PrProgramCode)`):

| Tên cột | Kiểu EF Core | Khóa | Cho phép NULL | Ghi chú & Ý nghĩa |
|---|---|---|---|---|
| `DLCode` | `string` | **PK1** | Không | Mã đại lý được chỉ định (Varchar50 NOT NULL, FK tới `Mst_DealerCP.DLCPCode`). |
| `PrProgramCode` | `string` | **PK2** | Không | Mã chương trình (Varchar50 NOT NULL, FK tới Header). |
| `NetworkID` | `string?` | | Có | Mã mạng nghiệp vụ (Varchar50). |
| `OrgID` | `string?` | | Có | Mã tổ chức (Varchar50). |
| `FlagActive` | `string?` | | Có | Cờ trạng thái: Khi lưu nguồn gán cố định `"PENDING"`. |
| `Remark` | `string?` | | Có | Ghi chú. |
| `LogLUDTimeUTC` | `DateTime?` | | Có | Thời điểm cập nhật cuối cùng UTC. |
| `LogLUBy` | `string?` | | Có | Tài khoản cập nhật cuối cùng. |

**Dữ liệu mẫu khởi tạo (Seeder - `Mst_PromotionProgramSeeder`, Idempotent)**:
- Gồm 3 bản ghi Header: `PR01` (Khuyến mãi tháng 10, FlagAllDL `'1'`, FlagActive `'1'`), `PR02` (Ưu đãi đại lý, FlagAllDL `'0'`, FlagActive `'1'`), `PR03` (Chương trình cũ, FlagAllDL `'0'`, FlagActive `'0'`).
- Detail: `PR01` x `S` (Qty 10), `PR02` x `G` (Qty 5). Spec: `PR02` x `DL01`, `PR02` x `DL03`.

---

## 2. Đặc tả Chi tiết Từng Endpoint API

---

### 2.1. Search (#205)

Tìm kiếm danh mục chương trình khuyến mại phân trang theo từ khóa mã chương trình.

- **Route**: `POST /api/mst_promotionprogram/search`
- **Controller Action**: [`Mst_PromotionProgramControllerPort.Search`](../../Controllers/Port/Mst_PromotionProgramControllerPort.cs#L177-L208)
- **Tham chiếu nguồn**: Gate `Mst_PromotionProgramController.cs:32-113` → API `MstPromotionProgramController.cs:22-90` → Biz `Master.Loyalty.cs:13997-14543`

#### Bảng tham số Request

Payload JSON gửi qua Request Body (`MstPromotionProgramSearchReq`):

| Tên tham số | Kiểu dữ liệu | Bắt buộc | Mặc định | Ý nghĩa & Quy tắc |
|---|---|---|---|---|
| `OrgID` | `string?` | Không | `null` | Nhận tại signature gate (`:32`) nhưng hàm `strWhereClause_Mst_PromotionProgram` (`:718-730`) **không dùng OrgID** → Không lọc. |
| `KeyWord` | `string?` | Không | `null` | Từ khóa tìm kiếm theo mã chương trình (`PrProgramCode LIKE '%kw%'`, `gate :724-727`). |
| `Ft_PageIndex` | `int` | Không | `0` | Chỉ số trang cần lấy (0-based). Mặc định là `0`. |
| `Ft_PageSize` | `int` | Không | `0` | Số dòng trên mỗi trang. **Nguồn không có giá trị mặc định cho `Ft_PageSize` nên mặc định kiểu int là `0`**. Nếu gửi body `{}` hoặc `Ft_PageSize = 0` → Trả về mảng rỗng `DataList = []` (`gate :84-95`). |

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Chuẩn hóa phân trang: `pageIndex = Math.Max(0, req.Ft_PageIndex)`, `pageSize = req.Ft_PageSize`.
2. Vị trí bản ghi bắt đầu: `start = (long)pageIndex * pageSize`.
3. Mệnh đề lọc qua `GetXAsync`: Lọc theo từ khóa `KeyWord` trên trường `PrProgramCode`.
4. Sắp xếp thứ tự bản ghi: Phân trang theo `mrpl.LogLUDTimeUTC DESC` (`Master.Loyalty.cs:14349`), hòa trùng sắp phụ theo `PrProgramCode ASC`.
5. Đếm tổng số bản ghi: `itemCount = await ordered.CountAsync()`.
6. Trích xuất trang: Nếu `pageSize <= 0` trả về danh sách rỗng `DataList = []`; nếu `pageSize > 0` gọi `.Skip((int)start).Take((int)pageSize)`.
7. Gán `MyIdxSeq = start + i` (0-based) và tính `PageCount`.

#### Bảng mã lỗi
Endpoint luôn trả về HTTP 200.

#### Ví dụ Request / Response thực tế (từ lần chạy VERIFY #205)
- **Tìm kiếm mặc định (`{}`)**:
  - Request: `POST /api/mst_promotionprogram/search` kèm `{}`
  - Response (HTTP 200):
    ```json
    {
      "Success": true,
      "Data": {
        "DataList": [],
        "ItemCount": 3,
        "PageCount": 0,
        "PageIndex": 0,
        "PageSize": 0
      }
    }
    ```
- **Tìm kiếm có phân trang (`{"Ft_PageSize": 10}`)**: Trả về đủ 3 bản ghi demo (`PR03`, `PR01`, `PR02`).

---

### 2.2. GetAllActive (#206)

Lấy danh sách tất cả các chương trình khuyến mại.

- **Route**: `POST /api/mst_promotionprogram/getallactive`
- **Controller Action**: [`Mst_PromotionProgramControllerPort.GetAllActive`](../../Controllers/Port/Mst_PromotionProgramControllerPort.cs#L210-L231)
- **Tham chiếu nguồn**: Gate `Mst_PromotionProgramController.cs:114-223` → API `MstPromotionProgramController.cs:22-90` → Biz `Master.Loyalty.cs:13997-14543`

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Không nhận body, phân trang toàn bộ (`Ft_RecordStart = 0`, `Ft_RecordCount = 123456000`, `gate :138-139`).
2. **Hành vi đặc thù nguồn**: Tầng gate gọi `strWhereClause` với `FlagActive = "1"` (`gate :121`), tuy nhiên hàm `strWhereClause_Mst_PromotionProgram` (`:718-730`) **chỉ xử lý tham số `KeyWord` và hoàn toàn bỏ qua `FlagActive`**. Do đó, endpoint trả về tất cả các chương trình khuyến mại trong hệ thống.
3. Sắp xếp danh sách theo `LogLUDTimeUTC DESC`, `PrProgramCode ASC`.
4. Trả về mảng phẳng `List<Mst_PromotionProgram>` hoặc `null` nếu rỗng (`gate :207-210`).

#### Bảng mã lỗi
Endpoint luôn trả về HTTP 200.

---

### 2.3. GetByPrProgramCode (#207)

Tra cứu chi tiết một chương trình khuyến mại cùng toàn bộ danh sách mức ưu đãi hạng thẻ (`Lst_Mst_PromotionProgramDtl`) và danh sách đại lý chỉ định (`Lst_Mst_PromotionProgramSpec`).

- **Route**: `POST /api/mst_promotionprogram/getbyprprogramcode`
- **Controller Action**: [`Mst_PromotionProgramControllerPort.GetByPrProgramCode`](../../Controllers/Port/Mst_PromotionProgramControllerPort.cs#L233-L285)
- **Tham chiếu nguồn**: Gate `Mst_PromotionProgramController.cs:224-307` → API `MstPromotionProgramController.cs:22-90` → Biz `Master.Loyalty.cs:13997-14543`

#### Bảng tham số Request

Payload JSON gửi qua Request Body (`MstPromotionProgramGetByPrProgramCodeReq`):

| Tên tham số | Kiểu dữ liệu | Bắt buộc | Mặc định | Ý nghĩa & Quy tắc |
|---|---|---|---|---|
| `PrProgramCode` | `string?` | Có | `null` | Mã chương trình khuyến mãi cần tra cứu (`gate :224`). |

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. **Kiểm tra mã rỗng** (`gate :243-244, :290-295`):
   - Nếu `PrProgramCode` null hoặc rỗng: Không truy vấn CSDL, trả về ngay HTTP 200 `{ Success = true, Data = { Mst_PromotionProgram = null, Lst_Mst_PromotionProgramDtl = null, Lst_Mst_PromotionProgramSpec = null } }`.
2. Chuẩn hóa mã qua `StdParam`.
3. Tra cứu Header (`gate :267`): Lấy bản ghi từ `Mst_PromotionProgram` theo `PrProgramCode` (không lọc `FlagActive`).
4. Tra cứu Detail (`gate :268`): Lấy danh sách từ `Mst_PromotionProgramDtl` theo `PrProgramCode`, sắp xếp theo `LogLUDTimeUTC DESC`.
5. Tra cứu Spec (`gate :269`): Lấy danh sách từ `Mst_PromotionProgramSpec` theo `PrProgramCode`, sắp xếp theo `LogLUDTimeUTC DESC`.
6. Trả về cấu trúc 3 bảng: Nếu danh sách con rỗng thì gán `null`.

#### Bảng mã lỗi
Endpoint luôn trả về HTTP 200.

#### Ví dụ Request / Response thực tế (từ lần chạy VERIFY #207)
- Request: `{"PrProgramCode": "pr02"}`
- Response (HTTP 200):
  ```json
  {
    "Success": true,
    "Data": {
      "Mst_PromotionProgram": { "PrProgramCode": "PR02", "PrProgramName": "Ưu đãi đại lý", "FlagAllDL": "0", "FlagActive": "1" },
      "Lst_Mst_PromotionProgramDtl": [
        { "PrProgramCode": "PR02", "CardType": "G", "Qty": 5.0 }
      ],
      "Lst_Mst_PromotionProgramSpec": [
        { "DLCode": "DL01", "PrProgramCode": "PR02", "FlagActive": "PENDING" },
        { "DLCode": "DL03", "PrProgramCode": "PR02", "FlagActive": "PENDING" }
      ]
    }
  }
  ```

---

### 2.4. Save (#208)

Tạo mới chương trình khuyến mại kèm danh sách Dtl và Spec theo cơ chế upsert xóa-ghi lại trong 1 Transaction duy nhất.

- **Route**: `POST /api/mst_promotionprogram/save`
- **Controller Action**: [`Mst_PromotionProgramControllerPort.Save`](../../Controllers/Port/Mst_PromotionProgramControllerPort.cs#L287-L306)
- **Tham chiếu nguồn**: Gate `Mst_PromotionProgramController.cs:311-402` → API `MstPromotionProgramController.cs:93-163` → Biz `Master.Loyalty.cs:13179-13995`

#### Bảng tham số Request

Payload JSON gửi qua Request Body (`MstPromotionProgramSaveReq`):

| Tên tham số | Kiểu dữ liệu | Bắt buộc | Vị trí | Ý nghĩa & Quy tắc |
|---|---|---|---|---|
| `Mst_PromotionProgram` | `object` | **Có** | Body | Thông tin header (`PrProgramCode`, `PrProgramName`, `EffDateStart`, `EffDateEnd`, `FlagAllDL`, `Remark`). |
| `Lst_Mst_PromotionProgramDtl` | `array` | Không | Body | Danh sách ưu đãi theo hạng thẻ (`CardType`, `Qty`, `Unit`, `FlagActive`, `Remark`). |
| `Lst_Mst_PromotionProgramSpec` | `array` | Không | Body | Danh sách đại lý áp dụng (`DLCode`, `Remark`). |

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. **Kiểm tra body rỗng / null**: Nếu body null hoặc thiếu khối `Mst_PromotionProgram` → Ném ngoại lệ, trả về HTTP 400 kèm mã lỗi `ErridnLoyalty.Mst_PromotionProgram_Save` (`Check.strJson = null`, `gate :315, :387-393`).
2. **Không sinh sequence**: Đoạn code lấy sequence tại gate nguồn (`:345-366`) bị comment out → `PrProgramCode` lấy trực tiếp từ client gửi lên qua `StdParam`. Nếu mã rỗng → Báo lỗi `ErridnLoyalty.Mst_PromotionProgram_Save`.
3. **Ép cờ kích hoạt tại Gate**: Tầng gate ép cứng `FlagActive = "1"` (`gate :384`), bất kể client truyền giá trị nào.
4. Mở Transaction CSDL độc lập (`Master.Loyalty.cs:13768`).
5. Chuẩn hóa: `PrProgramCode` (StdParam), `PrProgramName` (StdStr).
6. **Kiểm tra tên rỗng**: Nếu `PrProgramName` rỗng → Rollback, trả về HTTP 400 kèm mã lỗi `ErridnLoyalty.Mst_PromotionProgram_SaveX_InvalidPrProgramName` (`Master.Loyalty.cs:13285-13295`).
7. **Kiểm tra ngày hiệu lực**: `EffDateStart` và `EffDateEnd` bắt buộc hợp lệ theo chuẩn ngày giờ.
8. **Kiểm tra danh sách Detail**: Chuẩn hóa `CardType` qua `StdParam`. Không được trùng lặp `CardType` trong cùng chương trình.
9. **Kiểm tra danh sách Spec**:
   - Chuẩn hóa `DLCode` qua `StdParam`.
   - Giả lập ràng buộc khóa ngoại: Kiểm tra tồn tại `DLCode` trong bảng đại lý `Mst_DealerCP`. Nếu đại lý không tồn tại → Ném lỗi `ErridnLoyalty.Mst_PromotionProgram_Save`.
   - Gán cố định `FlagActive = "PENDING"` cho mọi bản ghi spec (`Master.Loyalty.cs:13516`).
10. **Cơ chế Upsert Xóa - Ghi lại (`Master.Loyalty.cs:13562-13706`)**:
    - Xóa các bản ghi con cũ trong `Mst_PromotionProgramSpec`.
    - Xóa các bản ghi con cũ trong `Mst_PromotionProgramDtl`.
    - Xóa bản ghi cũ trong `Mst_PromotionProgram`.
    - Thêm mới Header, các bản ghi Dtl và các bản ghi Spec.
11. Commit Transaction, trả về HTTP 200 `{ Success = true, Data = null }` (`gate :389`).

#### Bảng mã lỗi `ErridnLoyalty.*`

| Mã lỗi | Điều kiện kích hoạt | Các tham số trả về trong `Params` |
|---|---|---|
| `ErridnLoyalty.Mst_PromotionProgram_Save` | Request body rỗng, mã rỗng, ngày không hợp lệ, trùng khóa con hoặc mã đại lý không tồn tại | `Check.strJson = null` hoặc tham số lỗi |
| `ErridnLoyalty.Mst_PromotionProgram_SaveX_InvalidPrProgramName` | `PrProgramName` rỗng hoặc chỉ có khoảng trắng | `Check.strPrProgramName = null` |

---

### 2.5. Update (#209)

Cập nhật chương trình khuyến mại (dùng chung logic lưu lõi `SaveCoreAsync` với Save).

- **Route**: `POST /api/mst_promotionprogram/update`
- **Controller Action**: [`Mst_PromotionProgramControllerPort.Update`](../../Controllers/Port/Mst_PromotionProgramControllerPort.cs#L308-L326)
- **Tham chiếu nguồn**: Gate `Mst_PromotionProgramController.cs:406-502` → API `MstPromotionProgramController.cs:93-163` → Biz `Master.Loyalty.cs:13179-13995`

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Thực hiện toàn bộ quy trình kiểm tra và upsert giống Save, ngoại trừ 2 điểm khác biệt cốt lõi:
2. **Không ép cờ `FlagActive = '1'`**: Sử dụng `FlagActive` client gửi lên, chuẩn hóa qua `StdFlag` (chỉ nhận `"1"`, còn lại kể cả null/thiếu đều hạ về `"0"`).
3. **Tự động chốt ngày kết thúc khi ngừng kích hoạt (`gate :480-484`)**:
   - Nếu `StdFlag(FlagActive) == "0"`: Hệ thống **tự động ghi đè `EffDateEnd` bằng thời điểm hiện tại của máy chủ**: `EffDateEnd = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")`.
4. Thực hiện xóa và ghi lại bản ghi trong Transaction CSDL, trả về HTTP 200 `{ Success = true, Data = null }`.

---

### 2.6. Delete (#210)

Xóa một chương trình khuyến mại và toàn bộ chi tiết con trực thuộc khỏi cơ sở dữ liệu.

- **Route**: `POST /api/mst_promotionprogram/delete`
- **Controller Action**: [`Mst_PromotionProgramControllerPort.Delete`](../../Controllers/Port/Mst_PromotionProgramControllerPort.cs#L328-L387)
- **Tham chiếu nguồn**: Gate `Mst_PromotionProgramController.cs:506-569` → API `WA_Mst_PromotionProgram_Save` (`FlagIsDelete = "1"`) → Biz `Master.Loyalty.cs:13179-13734` (`bIsDelete = true`)

#### Bảng tham số Request

Payload JSON gửi qua Request Body (`MstPromotionProgramDeleteReq`):

| Tên tham số | Kiểu dữ liệu | Bắt buộc | Mặc định | Ý nghĩa & Quy tắc |
|---|---|---|---|---|
| `PrProgramCode` | `string?` | **Có** | | Mã chương trình khuyến mãi cần xóa (`gate :506`). |

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. **Kiểm tra mã rỗng tại Gate** (`gate :555`):
   - Nếu `req == null` hoặc `string.IsNullOrWhiteSpace(req.PrProgramCode)`: Tầng gate nguồn ném `ServiceException("invalid_BirthPolicyNo", "invalid_BirthPolicyNo")` do lỗi sao chép từ module chính sách sinh nhật.
   - **Hệ thống đích bảo lưu trung thực 1-1 mã lỗi này**: Trả về HTTP 400 kèm mã lỗi `ErridnLoyalty.invalid_BirthPolicyNo` (`Check.PrProgramCode = req?.PrProgramCode`).
2. Mở Transaction CSDL độc lập qua `DeleteXAsync` (`Master.Loyalty.cs:13768`).
3. Chuẩn hóa mã qua `StdParam`.
4. **Kiểm tra tồn tại trong CSDL** (`Master.Loyalty.cs:13255-13258`):
   - Nếu chương trình không tồn tại trong CSDL (`program == null`): **Thành công, không làm gì cả**, commit transaction và trả về ngay HTTP 200 `{ Success = true, Data = null }` theo đúng hành vi nguồn (`goto MyCodeLabel_Done`).
5. **Chặn xóa chương trình chưa bắt đầu hiệu lực (`Master.Loyalty.cs:13298-13331`)**:
   - Nguồn kiểm tra nếu chương trình có `program.EffDateStart > nowUtc` (DateTime.UtcNow) → Rollback transaction, trả về HTTP 400 kèm mã lỗi `ErridnLoyalty.Mst_PromotionProgram_SaveX_InvalidEffDateStart` (`Check.PrProgramCode`, `Check.EffDateStart`, `Check.strToday`). Nghĩa là: CHỈ xóa được chương trình ĐÃ BẮT ĐẦU HIỆU LỰC (`EffDateStart <= nowUtc`); chương trình ở tương lai chưa có hiệu lực thì không xóa được.
6. **Xóa dữ liệu** (`Master.Loyalty.cs:13562-13588`):
   - Xóa lần lượt các bản ghi con trong `Mst_PromotionProgramSpec`.
   - Xóa các bản ghi con trong `Mst_PromotionProgramDtl`.
   - Xóa bản ghi cha trong `Mst_PromotionProgram`.
7. Bắt ngoại lệ CSDL: Rollback transaction, trả về HTTP 400 `ErridnLoyalty.Mst_PromotionProgram_Save` (`Params.Exception = ex.Message`).
8. Commit Transaction, trả về HTTP 200 `{ Success = true, Data = null }` (`gate :560`).

#### Bảng mã lỗi `ErridnLoyalty.*`

| Mã lỗi | Điều kiện kích hoạt | Các tham số trả về trong `Params` |
|---|---|---|
| `ErridnLoyalty.invalid_BirthPolicyNo` | `PrProgramCode` rỗng, null hoặc chỉ chứa khoảng trắng (lỗi typo đặc thù nguồn) | `Check.PrProgramCode = null` |
| `ErridnLoyalty.Mst_PromotionProgram_SaveX_InvalidEffDateStart` | Chương trình chưa bắt đầu hiệu lực ở tương lai (`EffDateStart > nowUtc`) | `Check.PrProgramCode = <codeParam>`, `Check.EffDateStart = <date>`, `Check.strToday = <today>` |
| `ErridnLoyalty.Mst_PromotionProgram_Save` | Lỗi ngoại lệ CSDL trong transaction | `Params.Exception = ex.Message` |

---

### 2.7. Export (#211)

Xuất danh sách chương trình khuyến mại ra tập tin Excel `.xlsx`.

- **Route**: `POST /api/mst_promotionprogram/export`
- **Controller Action**: [`Mst_PromotionProgramControllerPort.Export`](../../Controllers/Port/Mst_PromotionProgramControllerPort.cs#L389-L446)
- **Tham chiếu nguồn**: Gate `Mst_PromotionProgramController.cs:573-656` → API `WA_Mst_PromotionProgram_Get` → Biz `Master.Loyalty.cs:13997-14543`

#### Bảng tham số Request

Payload JSON gửi qua Request Body (`MstPromotionProgramExportReq`), hỗ trợ body rỗng:

| Tên tham số | Kiểu dữ liệu | Bắt buộc | Mặc định | Ý nghĩa & Quy tắc |
|---|---|---|---|---|
| `OrgID` | `string?` | Không | `null` | Nhận tại signature gate nhưng không lọc (`gate :573, :718-730`). |
| `KeyWord` | `string?` | Không | `null` | Lọc theo mã chương trình (`PrProgramCode LIKE '%kw%'`). |
| `FlagActive` | `string?` | Không | `null` | Nhận tại signature gate nhưng **bị bỏ qua trong `strWhereClause`** → Không lọc. |

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Gọi `GetXAsync` với phân trang `recordStart = 0`, `recordCount = 123456000` (`gate :619-620`).
2. Kiểm tra dữ liệu rỗng: Nếu không có bản ghi nào → Trả về HTTP 400 kèm mã lỗi `ErridnLoyalty.Mst_PromotionProgram_Export` và `Message = "Không có dữ liệu"` (`gate :627, :641-645`).
3. Từ điển cột `GetImportDicColums()` gồm đúng 7 cột (`gate :631, :691-702`):
   - `PrProgramCode`: `"Mã CTKM (*)"`
   - `PrProgramName`: `"Tên CTKM (*)"`
   - `EffDateStart`: `"Thời gian bắt đầu (*)"`
   - `EffDateEnd`: `"Thời gian kết thúc (*)"`
   - `FlagAllDL`: `"Áp dụng tất cả đại lý"`
   - `FlagActive`: `"Trạng thái"`
   - `Remark`: `"Ghi chú"`
4. Sinh file Excel sheet `"Mst_PromotionProgram"` qua [`PortExcel.ExportToExcelFromList`](../../Data/PortExcel.cs).
5. Trả về HTTP 200 `{ Success = true, Data = { Url = baseAddress + virtualPath } }` (`gate :635-638`).

#### Bảng mã lỗi `ErridnLoyalty.*`

| Mã lỗi | Điều kiện kích hoạt | Các tham số trả về trong `Params` |
|---|---|---|
| `ErridnLoyalty.Mst_PromotionProgram_Export` | Không có bản ghi nào thỏa mãn điều kiện lọc | `Message = "Không có dữ liệu"` |

---

### 2.8. ExportTemplate (#212)

Xuất tập tin mẫu Excel dùng cho chức năng nhập dữ liệu chương trình khuyến mại.

- **Route**: `POST /api/mst_promotionprogram/exporttemplate`
- **Controller Action**: [`Mst_PromotionProgramControllerPort.ExportTemplate`](../../Controllers/Port/Mst_PromotionProgramControllerPort.cs#L448-L474)
- **Tham chiếu nguồn**: Gate `Mst_PromotionProgramController.cs:658-690`

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Không nhận body, không kiểm tra quyền, không đọc CSDL (`gate :666`).
2. Khởi tạo danh sách rỗng 0 dòng `List<Mst_PromotionProgram>`.
3. Từ điển cột `GetImportDicColumsTemplate()` gồm đúng 4 cột (`gate :668, :704-713`):
   - `PrProgramCode`: `"Mã CTKM (*)"`
   - `PrProgramName`: `"Tên CTKM (*)"`
   - `EffDateStart`: `"Thời gian bắt đầu (*)"`
   - `EffDateEnd`: `"Thời gian kết thúc (*)"`
4. Sinh file Excel rỗng có dòng tiêu đề sheet `"Mst_PromotionProgram"`.
5. Trả về HTTP 200 `{ Success = true, Data = { Url = baseAddress + virtualPath } }` (`gate :672-676`).

#### Bảng mã lỗi
Endpoint luôn trả về HTTP 200.

---

## 3. Khác biệt So với Nguồn & Danh sách GAP (Known Gaps)

| Hạng mục | Trạng thái ở Nguồn | Triển khai ở Đích | Phân loại & Ghi chú GAP |
|---|---|---|---|
| **Xác thực & Phân quyền** | Gate kiểm tra `CheckGatewayAuthentication`, `CheckAuthorize`; Biz kiểm tra `Sys_User_CheckAuthorize`, `drAbilityOfUser`. | Bỏ qua toàn bộ kiểm tra phân quyền do chưa có phân hệ User/Role. | Bỏ qua có chủ đích (**Loại Đ**). |
| **Ghi nhật ký hệ thống** | Ghi bản ghi `RequestLog` và `ReturnLog` vào CSDL cho mọi thao tác. | Bỏ qua ghi log DB. | Bỏ qua có chủ đích (**Loại Đ**). |
| **Lỗi typo nguồn khi Delete** | Ném exception với mã lỗi sao chép từ BirthPolicy: `"invalid_BirthPolicyNo"`. | Giữ nguyên mã lỗi `ErridnLoyalty.invalid_BirthPolicyNo` khi Delete rỗng mã. | Khớp nguồn 1-1. |
| **Khóa ngoại DDL Dtl** | DDL nguồn khai báo FK CardType trỏ nhầm bảng `REFERENCES Mst_CardTypeDtl`. | Bỏ qua kiểm tra ràng buộc FK CardType trong Dtl. | **GAP**: DDL nguồn sai bảng tham chiếu. |
| **Mặc định phân trang Search** | Signature nguồn không khai báo default cho `Ft_PageSize` nên C# gán `0` dẫn đến `DataList = []`. | Giữ nguyên hành vi gán `Ft_PageSize = 0` mặc định của nguồn. | Khớp nguồn 1-1. |

---

## 4. Nhật ký Thay đổi & Đối chiếu Tiến độ (Changelog)

| STT Hạng mục | Tên Endpoint & Action | Trạng thái | Mã Commit Git | Mô tả tóm tắt thay đổi |
|---|---|---|---|---|
| **#205** | `Mst_PromotionProgramController/Search` | Đã hoàn thành | `dba2d40` | Khởi tạo model [`Mst_PromotionProgram.cs`](../../Models/Port/Mst_PromotionProgram.cs) (Header, Dtl, Spec), cấu hình EF Core, Seeder demo và action tìm kiếm phân trang. |
| **#206** | `Mst_PromotionProgramController/GetAllActive` | Đã hoàn thành | `045e8ef` | Lấy danh sách tất cả chương trình khuyến mại, tái hiện hành vi bỏ qua lọc cờ của nguồn. |
| **#207** | `Mst_PromotionProgramController/GetByPrProgramCode` | Đã hoàn thành | `2b9f2f0` | Tra cứu chi tiết chương trình cùng 2 danh sách bảng con Dtl và Spec, trả về `null` khi mã rỗng. |
| **#208** | `Mst_PromotionProgramController/Save` | Đã hoàn thành | `c9070f3` | Thêm mới chương trình theo cơ chế upsert xóa-ghi lại trong 1 transaction, ép cờ kích hoạt `FlagActive = '1'`, kiểm tra đại lý hợp lệ. |
| **#209** | `Mst_PromotionProgramController/Update` | Đã hoàn thành | `26fc5aa` | Cập nhật chương trình, tự động chốt ngày kết thúc bằng giờ máy chủ khi hạ cờ hoạt động. |
| **#210** | `Mst_PromotionProgramController/Delete` | Đã hoàn thành | `09b4b3d` | Xóa chương trình, chặn xóa khi chương trình chưa bắt đầu hiệu lực (EffDateStart > DateTime.UtcNow), bảo lưu mã lỗi typo `invalid_BirthPolicyNo`. |
| **#211** | `Mst_PromotionProgramController/Export` | Đã hoàn thành | `e7ebfb9` | Xuất danh sách chương trình ra Excel 7 cột từ điển, báo lỗi khi không có dữ liệu. |
| **#212** | `Mst_PromotionProgramController/ExportTemplate` | Đã hoàn thành | `6999fdd` | Xuất file mẫu Excel gồm 4 cột tiêu đề, không truy vấn CSDL. |
