# Tham chiếu API Mst_BirthPolicyController (Port 1-1)

| Thông tin | Giá trị |
|---|---|
| **Phạm vi** | Tài liệu tham chiếu chi tiết 7 endpoint đã port 1-1 của Mst_BirthPolicyController (#70 - #76) |
| **Cập nhật** | 2026-10-16 |
| **Phiên bản mã** | `ed28f8f` |
| **Trạng thái** | Nháp — chờ review |

---

## 1. Tóm tắt Nghiệp vụ & Cấu trúc Dữ liệu

### 1.1. Mục đích Nghiệp vụ
API `Mst_BirthPolicyController` quản lý chính sách tặng quà sinh nhật (`Mst_BirthPolicy`) và cấu hình chi tiết mức ưu đãi theo từng hạng thẻ (`Mst_BirthPolicyDtl`) trong hệ thống MiniLoyalty. Module cung cấp các chức năng tìm kiếm phân trang theo từ khóa và trạng thái hoạt động (kèm tính toán cờ hiệu lực `FlagShow`), lấy danh sách các chính sách đang hoạt động, tra cứu chi tiết chính sách kèm danh sách hạng thẻ theo mã chính sách, thêm mới chính sách kèm cấu hình chi tiết theo transaction, cập nhật thông tin chính sách, xóa chính sách theo cờ xóa và xuất danh sách chính sách ra tập tin Excel chuẩn.

### 1.2. Bảng Đối chiếu Nguồn Tra cứu (Source Code Mapping)

| Thành phần | Đường dẫn tập tin nguồn (`2022.7.Loyalty`) | Tập tin đích tương ứng (`MiniLoyalty`) |
|---|---|---|
| **Gate Controller** | `13.ClientGate\V10\idn.Loyalty.WebAPI\Controllers\Mst_BirthPolicyController.cs:23-683` | [`Controllers/Port/Mst_BirthPolicyControllerPort.cs`](../../Controllers/Port/Mst_BirthPolicyControllerPort.cs) |
| **API Backend** | `11.BackEnd\V10\idn.Loyalty.Biz.Web\Controllers\MstBirthPolicyController.cs:22-238` | Tích hợp trực tiếp vào [`Controllers/Port/Mst_BirthPolicyControllerPort.cs`](../../Controllers/Port/Mst_BirthPolicyControllerPort.cs) |
| **Nghiệp vụ (Biz)** | `11.BackEnd\V10\idn.Loyalty.Biz\Master.Loyalty.cs` (Get `:10606-11085`; Save/Create/Delete `:9675-10604`; Update `:11093-11690`) | Tích hợp trực tiếp vào [`Controllers/Port/Mst_BirthPolicyControllerPort.cs`](../../Controllers/Port/Mst_BirthPolicyControllerPort.cs) |
| **Entity Header** | `12.Dev.Common\idn.Loyalty.Common\Models\Loyalty\Mst_BirthPolicy.cs:9-25` | [`Models/Port/Mst_BirthPolicy.cs:38-54`](../../Models/Port/Mst_BirthPolicy.cs#L38-L54) |
| **Entity Detail** | `20221026.CreateDatabase.sql:378-394` (DDL 7 cột) + migration 2026 (3 cột) | [`Models/Port/Mst_BirthPolicy.cs:81-95`](../../Models/Port/Mst_BirthPolicy.cs#L81-L95) |
| **Seeder Demo** | `Order => 27` (danh mục `Mst_*` dùng 10–99) | [`Models/Port/Mst_BirthPolicy.cs:130-220`](../../Models/Port/Mst_BirthPolicy.cs#L130-L220) |

### 1.3. Cấu trúc Bảng CSDL (`Mst_BirthPolicy` & `Mst_BirthPolicyDtl`)

Hệ thống quản lý chính sách sinh nhật gồm 2 bảng quan hệ master - detail:

#### Bảng Header `Mst_BirthPolicy` (14 cột trong Model, 12 cột DDL chuẩn + 2 cột DB thực tế):

| Tên cột | Kiểu EF Core | Khóa | Cho phép NULL | Ghi chú & Ý nghĩa |
|---|---|---|---|---|
| `BirthPolicyNo` | `string` | **PK** | Không | Mã chính sách sinh nhật (Varchar50). Khóa chính (DDL `20221026.CreateDatabase.sql:416-419`). |
| `NetworkID` | `string` | | Không | Mã mạng nghiệp vụ (Varchar50 NOT NULL). Ở đích lấy từ cấu hình `Biz_NetworkID`. |
| `ParamPrType` | `string?` | | Có | Loại ưu đãi (Varchar50). Mặc định là `"NGAYSINHNHAT"`. |
| `BirthPolicyName` | `string?` | | Có | Tên chương trình chính sách sinh nhật (Varchar200). |
| `EffDateStart` | `DateTime` | | Không | Ngày bắt đầu hiệu lực (MyDateTime NOT NULL). |
| `EffDateEnd` | `DateTime` | | Không | Ngày kết thúc hiệu lực (MyDateTime NOT NULL). |
| `FlagActive` | `string?` | | Có | Cờ trạng thái kích hoạt: `"1"` (Kích hoạt), `"0"` (Không kích hoạt). |
| `FlagPoint` | `string?` | | Có | Cờ tặng điểm (`"1"`/`"0"`). Cột có trong DB thực tế, không có DDL trong repo (GAP ghi nhận). |
| `FlagVoucher` | `string?` | | Có | Cờ tặng voucher (`"1"`/`"0"`). Cột có trong DB thực tế, không có DDL trong repo (GAP ghi nhận). |
| `Remark` | `string?` | | Có | Ghi chú thêm về chính sách sinh nhật (Varchar400). |
| `UpdateDTime` | `DateTime?` | | Có | Thời điểm cập nhật nghiệp vụ (MyDateTime NULL). |
| `UpdateBy` | `string?` | | Có | Tài khoản thực hiện cập nhật nghiệp vụ (Varchar50 NULL). |
| `LogLUDTimeUTC` | `DateTime?` | | Có | Thời điểm cập nhật cuối cùng theo chuẩn UTC. |
| `LogLUBy` | `string?` | | Có | Tài khoản thực hiện cập nhật cuối cùng (mặc định gán `"port"` ở đích). |

*(Lưu ý: Thuộc tính `FlagShow` là cột tính toán runtime lúc truy vấn theo ngày hệ thống UTC+7, không lưu trong bảng CSDL)*.

#### Bảng Detail `Mst_BirthPolicyDtl` (10 cột, Khóa chính ghép `(BirthPolicyNo, CardType)`):

| Tên cột | Kiểu EF Core | Khóa | Cho phép NULL | Ghi chú & Ý nghĩa |
|---|---|---|---|---|
| `BirthPolicyNo` | `string` | **PK1** | Không | Mã chính sách sinh nhật (Varchar50 NOT NULL, liên kết tới header). |
| `CardType` | `string` | **PK2** | Không | Mã hạng thẻ thành viên áp dụng (Varchar50 NOT NULL). |
| `NetworkID` | `string` | | Không | Mã mạng nghiệp vụ (Varchar50 NOT NULL). |
| `Point` | `double?` | | Có | Số điểm thưởng tặng vào dịp sinh nhật (MyValue). |
| `Remark` | `string?` | | Có | Ghi chú cho mức ưu đãi của hạng thẻ. |
| `LogLUDTimeUTC` | `DateTime?` | | Có | Thời điểm cập nhật cuối cùng theo chuẩn UTC. |
| `LogLUBy` | `string?` | | Có | Tài khoản cập nhật cuối cùng. |
| `VoucherValue` | `double?` | | Có | Giá trị voucher tặng kèm (decimal(28,8) từ migration 2026). |
| `VoucherPointVCLimit` | `double?` | | Có | Giới hạn điểm quy đổi voucher (decimal(28,8) từ migration 2026). |
| `VoucherExpireDays` | `int?` | | Có | Hạn sử dụng của voucher tính theo số ngày (int từ migration 2026). |

**Dữ liệu mẫu khởi tạo (Seeder - `Mst_BirthPolicySeeder`, Idempotent)**:
- Header gồm 3 bản ghi: `BP01` (Sinh nhật 2026, FlagActive `'1'`), `BP02` (Sinh nhật 2025, FlagActive `'0'`), `BP03` (Sinh nhật 2027, FlagActive `'1'`).
- Detail gồm 4 bản ghi: `BP01` x `S` (0 điểm), `BP01` x `G` (100.000 điểm, Voucher 500.000), `BP01` x `P` (200.000 điểm), `BP03` x `G` (150.000 điểm).

---

## 2. Đặc tả Chi tiết Từng Endpoint API

---

### 2.1. Search (#70)

Tìm kiếm danh mục chính sách sinh nhật phân trang theo từ khóa và trạng thái hoạt động, tự động tính toán cờ hiệu lực `FlagShow`.

- **Route**: `POST /api/mst_birthpolicy/search`
- **Controller Action**: [`Mst_BirthPolicyControllerPort.Search`](../../Controllers/Port/Mst_BirthPolicyControllerPort.cs#L183-L216)
- **Tham chiếu nguồn**: Gate `Mst_BirthPolicyController.cs:23-103` → API `MstBirthPolicyController.cs:168-238` → Biz `Master.Loyalty.cs:10606-11085`

#### Bảng tham số Request

Payload JSON gửi qua Request Body (`MstBirthPolicySearchReq`), hỗ trợ cả trường hợp body rỗng hoàn toàn:

| Tên tham số | Kiểu dữ liệu | Bắt buộc | Mặc định | Ý nghĩa & Quy tắc |
|---|---|---|---|---|
| `KeyWord` | `string?` | Không | `null` | Từ khóa tìm kiếm theo mã chính sách (`BirthPolicyNo LIKE '%kw%'`, `gate :299-302`). |
| `FlagActive` | `string?` | Không | `null` | Danh sách trạng thái phân cách bởi dấu phẩy (toán tử `IN ('1','0')`, `gate :308-316`). |
| `Ft_PageIndex` | `int` | Không | `0` | Chỉ số trang cần lấy (0-based). Nếu `< 0` tự động gán về `0`. |
| `Ft_PageSize` | `int` | Không | `10` | Số dòng trên mỗi trang. Nếu `<= 0` trả về mảng rỗng `DataList = []` (`gate :78-86`). |

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Chuẩn hóa phân trang: `pageIndex = Math.Max(0, req.Ft_PageIndex)`, `pageSize = req.Ft_PageSize`.
2. Vị trí bản ghi bắt đầu: `start = (long)pageIndex * pageSize` (`gate :66-67`).
3. Mệnh đề lọc qua `GetXAsync` ([`Mst_BirthPolicyControllerPort.cs:1150-1185`](../../Controllers/Port/Mst_BirthPolicyControllerPort.cs#L1150-L1185)):
   - **KeyWord**: `EF.Functions.Like((x.BirthPolicyNo ?? "").ToLower(), PortQuery.Contains(keyWord), PortQuery.Esc)`.
   - **FlagActive**: Tách chuỗi theo dấu phẩy, lọc `ds.Contains((x.FlagActive ?? "").ToLower())`.
4. Sắp xếp thứ tự bản ghi: Sắp xếp theo `LogLUDTimeUTC DESC` (`Master.Loyalty.cs:10944`), hòa trùng sắp phụ theo `BirthPolicyNo ASC`.
5. Đếm tổng số bản ghi: `itemCount = await ordered.CountAsync()`.
6. Trích xuất trang: Nếu `pageSize <= 0` trả về danh sách rỗng (`nFilterRecordEnd = -1`); nếu `pageSize > 0` gọi `.Skip((int)start).Take((int)pageSize)`.
7. Gán `MyIdxSeq = start + i` (0-based) và tính toán thuộc tính `FlagShow` (`Master.Loyalty.cs:10984-10996`):
   - Tính ngày hiện tại theo giờ Việt Nam UTC+7: `today = DateTime.UtcNow.AddHours(7).Date`.
   - `FlagShow = "1"` nếu `EffDateStart.Date <= today && today <= EffDateEnd.Date`; ngược lại `FlagShow = "0"`.
8. Tính `PageCount`: Nếu `dataList.Count == 0`, `PageCount = 0`; ngược lại `PageCount = ceil(itemCount / ps)`.

#### Bảng mã lỗi
Endpoint không phát sinh lỗi nghiệp vụ; luôn trả về HTTP 200.

#### Ví dụ Request / Response thực tế (từ lần chạy VERIFY #70)
- **Tìm kiếm mặc định (`{}`)**:
  - Request: `POST /api/mst_birthpolicy/search` kèm `{}`
  - Response (HTTP 200):
    ```json
    {
      "Success": true,
      "Data": {
        "DataList": [
          { "MyIdxSeq": 0, "BirthPolicyNo": "BP03", "BirthPolicyName": "Sinh nhật 2027", "FlagActive": "1", "FlagShow": "0" },
          { "MyIdxSeq": 1, "BirthPolicyNo": "BP01", "BirthPolicyName": "Sinh nhật 2026", "FlagActive": "1", "FlagShow": "1" },
          { "MyIdxSeq": 2, "BirthPolicyNo": "BP02", "BirthPolicyName": "Sinh nhật 2025", "FlagActive": "0", "FlagShow": "0" }
        ],
        "ItemCount": 3,
        "PageCount": 1,
        "PageIndex": 0,
        "PageSize": 10
      }
    }
    ```

---

### 2.2. GetAllActive (#71)

Lấy danh sách tất cả chính sách sinh nhật đang hoạt động (`FlagActive = '1'`).

- **Route**: `POST /api/mst_birthpolicy/getallactive`
- **Controller Action**: [`Mst_BirthPolicyControllerPort.GetAllActive`](../../Controllers/Port/Mst_BirthPolicyControllerPort.cs#L218-L241)
- **Tham chiếu nguồn**: Gate `Mst_BirthPolicyController.cs:104-213` → API `MstBirthPolicyController.cs:168-238` → Biz `Master.Loyalty.cs:10606-11085`

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Không nhận body, không phân trang (`Ft_RecordStart = 0`, `Ft_RecordCount = 123456000`, `gate :128-129`).
2. Lọc chính sách có `FlagActive = "1"`.
3. Sắp xếp danh sách theo `LogLUDTimeUTC DESC`, `BirthPolicyNo ASC`.
4. Tính toán thuộc tính `FlagShow` và gán chỉ số `MyIdxSeq` tuần tự bắt đầu từ 0.
5. Định dạng dữ liệu trả về: Bọc trong đối tượng `{ Lst_Mst_BirthPolicy: list }` (`gate :197-200`). Nếu danh sách rỗng, trường `Lst_Mst_BirthPolicy` nhận giá trị `null`.

#### Bảng mã lỗi
Endpoint luôn trả về HTTP 200.

#### Ví dụ Request / Response thực tế (từ lần chạy VERIFY #71)
- Request: `POST /api/mst_birthpolicy/getallactive` kèm `{}`
- Response (HTTP 200):
  ```json
  {
    "Success": true,
    "Data": {
      "Lst_Mst_BirthPolicy": [
        { "MyIdxSeq": 0, "BirthPolicyNo": "BP03", "BirthPolicyName": "Sinh nhật 2027", "FlagActive": "1" },
        { "MyIdxSeq": 1, "BirthPolicyNo": "BP01", "BirthPolicyName": "Sinh nhật 2026", "FlagActive": "1" }
      ]
    }
  }
  ```

---

### 2.3. GetByBirthPolicyNo (#72)

Tra cứu chi tiết một chính sách sinh nhật và toàn bộ cấu hình chi tiết theo hạng thẻ (`Lst_Mst_BirthPolicyDtl`).

- **Route**: `POST /api/mst_birthpolicy/getbybirthpolicyno`
- **Controller Action**: [`Mst_BirthPolicyControllerPort.GetByBirthPolicyNo`](../../Controllers/Port/Mst_BirthPolicyControllerPort.cs#L243-L333)
- **Tham chiếu nguồn**: Gate `Mst_BirthPolicyController.cs:214-290` → API `MstBirthPolicyController.cs:168-238` → Biz `Master.Loyalty.cs:10606-11085`

#### Bảng tham số Request

Payload JSON gửi qua Request Body (`MstBirthPolicyGetByBirthPolicyNoReq`):

| Tên tham số | Kiểu dữ liệu | Bắt buộc | Mặc định | Ý nghĩa & Quy tắc |
|---|---|---|---|---|
| `BirthPolicyNo` | `string?` | Có | `null` | Mã chính sách sinh nhật cần tra cứu (`gate :214`). |

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. **Kiểm tra mã rỗng** (`gate :233-234, :273-277`):
   - Nếu `BirthPolicyNo` rỗng hoặc null: **Tuyệt đối không truy vấn CSDL**, trả về ngay HTTP 200 `{ Success = true, Data = { Mst_BirthPolicy = null, Lst_Mst_BirthPolicyDtl = null } }`.
2. Chuẩn hóa mã qua `StdParam(req.BirthPolicyNo)`.
3. Tra cứu Header (`gate :257`): Lọc theo `BirthPolicyNo` (không phân biệt hoa thường, không lọc `FlagActive` nên chính sách ngừng hoạt động như `BP02` vẫn tra cứu được).
4. Tra cứu Detail (`gate :258`): Truy vấn bảng `Mst_BirthPolicyDtl` theo `BirthPolicyNo`, sắp xếp theo `LogLUDTimeUTC DESC`, `CardType ASC` (`Master.Loyalty.cs:11008-11016`).
5. Kết quả trả về: `{ Mst_BirthPolicy = header, Lst_Mst_BirthPolicyDtl = dtlList.Count > 0 ? dtlList : null }`.

#### Bảng mã lỗi
Endpoint luôn trả về HTTP 200 (không tìm thấy trả về Data null).

#### Ví dụ Request / Response thực tế (từ lần chạy VERIFY #72)
- **Tra cứu chính sách hợp lệ (`"BP01"`)**:
  - Request: `{"BirthPolicyNo": "BP01"}`
  - Response (HTTP 200):
    ```json
    {
      "Success": true,
      "Data": {
        "Mst_BirthPolicy": { "BirthPolicyNo": "BP01", "BirthPolicyName": "Sinh nhật 2026", "FlagActive": "1" },
        "Lst_Mst_BirthPolicyDtl": [
          { "BirthPolicyNo": "BP01", "CardType": "G", "Point": 100000.0, "VoucherValue": 500000.0 },
          { "BirthPolicyNo": "BP01", "CardType": "P", "Point": 200000.0 },
          { "BirthPolicyNo": "BP01", "CardType": "S", "Point": 0.0 }
        ]
      }
    }
    ```
- **Tra cứu mã rỗng / không truyền (`{}`)**:
  - Response: `{"Success": true, "Data": { "Mst_BirthPolicy": null, "Lst_Mst_BirthPolicyDtl": null }}`.

---

### 2.4. Save (#73)

Tạo mới chính sách sinh nhật kèm danh sách cấu hình điểm và voucher theo hạng thẻ trong 1 Transaction duy nhất.

- **Route**: `POST /api/mst_birthpolicy/save`
- **Controller Action**: [`Mst_BirthPolicyControllerPort.Save`](../../Controllers/Port/Mst_BirthPolicyControllerPort.cs#L335-L679)
- **Tham chiếu nguồn**: Gate `Mst_BirthPolicyController.cs:339-434` → API `MstBirthPolicyController.cs:22-92` → Biz `Master.Loyalty.cs:9675-10505`

#### Bảng tham số Request

Payload JSON gửi qua Request Body (`MstBirthPolicySaveReq`):

| Tên tham số | Kiểu dữ liệu | Bắt buộc | Vị trí | Ý nghĩa & Quy tắc |
|---|---|---|---|---|
| `Mst_BirthPolicy` | `object` | **Có** | Body | Thông tin header chính sách (`BirthPolicyNo`, `BirthPolicyName`, `EffDateStart`, `FlagActive`, `FlagPoint`, `FlagVoucher`, `Remark`). |
| `Lst_Mst_BirthPolicyDtl` | `array` | Không | Body | Danh sách đối tượng detail hạng thẻ (`CardType`, `Point`, `VoucherValue`, `VoucherExpireDays`, `Remark`). |

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. **Kiểm tra body rỗng / null**: Nếu `req == null` → Trả về HTTP 400 kèm mã lỗi `ErridnLoyalty.Mst_BirthPolicy_Save` (`Check.strJson = null`, `gate :347-354`).
2. **Sinh sequence mã chính sách**: Lấy sequence độc lập `PortSeq.NextAsync<Seq_BirthPolicyNo>(_db)` trong transaction riêng (tự tăng ngay cả khi các bước sau lỗi; format `{0}BIRTHPOLICYNO.{3}.{1:00000}{2}`, max 100000, `gate :373-397`). Client không truyền `BirthPolicyNo`.
3. **Kiểm tra Header null**: Nếu `req.Mst_BirthPolicy == null` (ví dụ gửi `{}`) → Trả về HTTP 400 `ErridnLoyalty.Mst_BirthPolicy_Save` (`Check.Mst_BirthPolicy = null`, `gate :395`).
4. Chuẩn hóa dữ liệu: `StdParam` (mã), `StdStr` (tên, ghi chú, `ParamPrType` — không viết hoa theo `Master.Loyalty.cs:9737`), `StdFlag` (`FlagPoint`, `FlagVoucher`). Cờ `FlagActive` được gate ép cứng bằng `"1"` (`:415`).
5. **Kiểm tra ngày bắt đầu hiệu lực**:
   - Nếu `EffDateStart` rỗng, null hoặc không parse được ngày hợp lệ (`Convert.ToDateTime`) → Trả về HTTP 400 `ErridnLoyalty.Mst_BirthPolicy_Save` (`Check.EffDateStart = <value>`, `Master.Loyalty.cs:9745-9746`).
6. **Kiểm tra cờ ưu đãi (FlagPoint / FlagVoucher)** (`:9797-9809`):
   - Cả hai cùng bằng `"1"` hoặc cùng khác `"1"` (chọn cả hai hoặc không chọn cái nào) → Trả về HTTP 400 `ErridnLoyalty.Mst_BirthPolicy_SaveX_InvalidFlagPointVoucher` (`Check.FlagPoint`, `Check.FlagVoucher`, `Expect = "Chi duoc chon 1 loai tang: FlagPoint hoac FlagVoucher = '1' (khong ca hai, khong ca khong)"`).
7. **Kiểm tra hiệu lực không chồng lấn** (`:9836-9863`):
   - Nếu đã tồn tại chính sách cùng `ParamPrType` có `EffDateStart >= dtEffDateStart.Date` → Trả về HTTP 400 `ErridnLoyalty.Mst_BirthPolicy_SaveX_InvalidEffDateStart` (`Check.BirthPolicyNo`, `Check.EffDateStart`).
8. **Kiểm tra danh mục loại ưu đãi** (`:9867-9873` tra cứu `Mst_ParamPromotionType`):
   - Không tìm thấy `ParamPrType` trong CSDL → Trả về HTTP 400 `ErridnLoyalty.Mst_ParamPromotionType_CheckDB_ParamPrTypeNotFound` (`Check.ParamPrType`).
   - Cờ hoạt động của loại ưu đãi không khớp `"1"` → Trả về HTTP 400 `ErridnLoyalty.Mst_ParamPromotionType_CheckDB_FlagActiveNotMatched` (`Check.ParamPrType`, `Check.strFlagActiveListToCheck = "1"`, `DB.FlagActive`).
9. **Kiểm tra danh sách Detail theo hạng thẻ** (`Master.Loyalty.cs:10021-10087`):
   - Duyệt từng phần tử trong `Lst_Mst_BirthPolicyDtl`:
     - `VoucherValue` không hợp lệ (sai định dạng số) → Trả về HTTP 400 `ErridnLoyalty.Mst_BirthPolicy_Save` (`Check.VoucherValue`).
     - `VoucherExpireDays` không hợp lệ (sai định dạng số nguyên) → Trả về HTTP 400 `ErridnLoyalty.Mst_BirthPolicy_Save` (`Check.VoucherExpireDays`).
     - Nếu `VoucherValue < 0 || VoucherExpireDays < 0` → Trả về HTTP 400 `ErridnLoyalty.Mst_BirthPolicy_SaveX_InvalidValue` (`Check.CardType`, `Check.VoucherValue`, `Check.VoucherExpireDays`, `Check.ErrCoditional = "VoucherValue < 0 or VoucherExpireDays < 0"`).
     - Nếu `VoucherValue > 0 && VoucherExpireDays <= 0` → Trả về HTTP 400 `ErridnLoyalty.Mst_BirthPolicy_SaveX_InvalidValue` (`Check.CardType`, `Check.VoucherValue`, `Check.VoucherExpireDays`, `Check.ErrCoditional = "VoucherValue > 0 but VoucherExpireDays <= 0"`).
     - Giả lập kiểm tra khóa ngoại: `CardType` không tồn tại trong `Mst_CardType` → Trả về HTTP 400 `ErridnLoyalty.Mst_BirthPolicy_Save` (`Check.CardType = <strCardType>`).
   - Kiểm tra trùng lặp `CardType` trong cùng danh sách detail → Trả về HTTP 400 `ErridnLoyalty.Mst_BirthPolicy_Save` (`Check.DuplicateCardType = null`).
10. **Lưu dữ liệu trong 1 Transaction duy nhất** (`Master.Loyalty.cs:10389`, commit `:10468`):
    - Xóa các bản ghi Detail và Header cũ cùng `BirthPolicyNo` nếu có.
    - Lưu bản ghi Header và các bản ghi Detail mới vào CSDL.
    - Bắt ngoại lệ CSDL: Rollback transaction, trả về HTTP 400 `ErridnLoyalty.Mst_BirthPolicy_Save` (`Params.Exception = ex.Message`).
11. Commit Transaction, trả về HTTP 200 `{ Success = true, Data = null }` (`gate :424-425`).

#### Bảng mã lỗi `ErridnLoyalty.*`

| Mã lỗi | Điều kiện kích hoạt | Các tham số trả về trong `Params` |
|---|---|---|
| `ErridnLoyalty.Mst_BirthPolicy_Save` | Request body rỗng (`Check.strJson = null`), Header null (`Check.Mst_BirthPolicy = null`), `EffDateStart` rỗng/sai định dạng (`Check.EffDateStart`), lỗi định dạng số voucher detail (`Check.VoucherValue`, `Check.VoucherExpireDays`), `CardType` không tồn tại trong `Mst_CardType` (`Check.CardType`), trùng lặp `CardType` trong detail (`Check.DuplicateCardType = null`), hoặc ngoại lệ CSDL (`Params.Exception = ex.Message`) | `Check.strJson`, `Check.Mst_BirthPolicy`, `Check.EffDateStart`, `Check.VoucherValue`, `Check.VoucherExpireDays`, `Check.CardType`, `Check.DuplicateCardType`, `Exception` |
| `ErridnLoyalty.Mst_BirthPolicy_SaveX_InvalidFlagPointVoucher` | Cả 2 cờ `FlagPoint` và `FlagVoucher` cùng bằng `"1"` hoặc cùng khác `"1"` | `Check.FlagPoint`, `Check.FlagVoucher`, `Expect` |
| `ErridnLoyalty.Mst_BirthPolicy_SaveX_InvalidEffDateStart` | Tồn tại chính sách khác cùng `ParamPrType` có `EffDateStart >= dtEffDateStart.Date` | `Check.BirthPolicyNo`, `Check.EffDateStart` |
| `ErridnLoyalty.Mst_ParamPromotionType_CheckDB_ParamPrTypeNotFound` | Loại ưu đãi `ParamPrType` không tồn tại trong bảng `Mst_ParamPromotionType` | `Check.ParamPrType` |
| `ErridnLoyalty.Mst_ParamPromotionType_CheckDB_FlagActiveNotMatched` | Trạng thái hoạt động `FlagActive` của `Mst_ParamPromotionType` không khớp `"1"` | `Check.ParamPrType`, `Check.strFlagActiveListToCheck = "1"`, `DB.FlagActive` |
| `ErridnLoyalty.Mst_BirthPolicy_SaveX_InvalidValue` | `VoucherValue < 0`, `VoucherExpireDays < 0`, hoặc `VoucherValue > 0` nhưng `VoucherExpireDays <= 0` | `Check.CardType`, `Check.VoucherValue`, `Check.VoucherExpireDays`, `Check.ErrCoditional` |

---

### 2.5. Update (#74)

Cập nhật thông tin chính sách sinh nhật và cấu hình chi tiết mức ưu đãi theo hạng thẻ.

- **Route**: `POST /api/mst_birthpolicy/update`
- **Controller Action**: [`Mst_BirthPolicyControllerPort.Update`](../../Controllers/Port/Mst_BirthPolicyControllerPort.cs#L681-L918)
- **Tham chiếu nguồn**: Gate `Mst_BirthPolicyController.cs:438-530` → API `MstBirthPolicyController.cs:95-165` → Biz `Master.Loyalty.cs:11093-11690`

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. **Kiểm tra body rỗng / null**: Nếu `req == null` → Trả về HTTP 400 `ErridnLoyalty.Mst_BirthPolicy_Update` (`Check.strJson = null`, `gate :448-455`).
2. **Kiểm tra Header null**: Nếu `req.Mst_BirthPolicy == null` (ví dụ gửi `{}`) → Trả về HTTP 400 `ErridnLoyalty.WAS_Mst_BirthPolicy_Update` (`Check.Mst_BirthPolicy = null`, `WAS :11655-11658`).
3. Chuẩn hóa dữ liệu input: `StdParam(header.BirthPolicyNo)`, `StdFlag(header.FlagActive)`, `StdFlag(header.FlagPoint)`, `StdFlag(header.FlagVoucher)`.
4. **Kiểm tra cờ ưu đãi (FlagPoint / FlagVoucher)** (`:11143-11155`):
   - Cả 2 cờ cùng bằng `"1"` hoặc cùng khác `"1"` → Trả về HTTP 400 `ErridnLoyalty.Mst_BirthPolicy_SaveX_InvalidFlagPointVoucher` (`Check.FlagPoint`, `Check.FlagVoucher`, `Expect`).
5. **Kiểm tra tồn tại chính sách trong CSDL** (`:11160-11167`):
   - Tra cứu `BirthPolicyNo` trong `Mst_BirthPolicy`: Nếu không tìm thấy → Trả về HTTP 400 `ErridnLoyalty.Mst_BirthPolicy_CheckDB_BirthPolicyNoNotFound` (`Check.BirthPolicyNo = <value>`).
6. **Kiểm tra ngày hiệu lực chính sách** (`:11171-11197`):
   - Nếu `existingPolicy.EffDateStart < today` (DateTime.UtcNow.Date): Chính sách đã bắt đầu hiệu lực trong quá khứ → Trả về HTTP 400 `ErridnLoyalty.Mst_BirthPolicy_UpdateX_InvalidEffDateStart` (`Check.BirthPolicyNo`, `Check.EffDateEnd`). CHỈ sửa được chính sách chưa bắt đầu hiệu lực (`EffDateStart >= today`).
7. **Kiểm tra danh sách Detail theo hạng thẻ** (`:11264-11329`):
   - Duyệt từng phần tử trong `Lst_Mst_BirthPolicyDtl`:
     - Parse `VoucherValue` lỗi → Trả về HTTP 400 `ErridnLoyalty.Mst_BirthPolicy_Update` (`Check.VoucherValue`).
     - Parse `VoucherExpireDays` lỗi → Trả về HTTP 400 `ErridnLoyalty.Mst_BirthPolicy_Update` (`Check.VoucherExpireDays`).
     - Nếu `VoucherValue < 0 || VoucherExpireDays < 0` → Trả về HTTP 400 `ErridnLoyalty.Mst_BirthPolicy_UpdateX_InvalidValue` (`Check.ErrCoditional = "VoucherValue < 0 or VoucherExpireDays < 0"`).
     - Nếu `VoucherValue > 0 && VoucherExpireDays <= 0` → Trả về HTTP 400 `ErridnLoyalty.Mst_BirthPolicy_UpdateX_InvalidValue` (`Check.ErrCoditional = "VoucherValue > 0 but VoucherExpireDays <= 0"`).
8. **Cập nhật dữ liệu trong 1 Transaction duy nhất** (`Master.Loyalty.cs:11489`, commit `:11561`):
   - Cập nhật Header (`:11372-11386`): Gán `FlagActive`, `FlagPoint`, `FlagVoucher`, `UpdateDTime = DateTime.UtcNow`, `UpdateBy = "port"`. Các trường tên, ngày, `ParamPrType`, `Remark` do client gửi bị bỏ qua theo đúng mã nguồn (`:11655-11658`).
   - Cập nhật Detail (Upsert): Xóa toàn bộ các bản ghi detail cũ của chính sách trong `Mst_BirthPolicyDtl` và thêm mới danh sách detail.
   - Bắt ngoại lệ CSDL: Rollback transaction, trả về HTTP 400 `ErridnLoyalty.Mst_BirthPolicy_Update` (`Params.Exception = ex.Message`).
9. Commit Transaction, trả về HTTP 200 `{ Success = true, Data = null }`.

#### Bảng mã lỗi `ErridnLoyalty.*`

| Mã lỗi | Điều kiện kích hoạt | Các tham số trả về trong `Params` |
|---|---|---|
| `ErridnLoyalty.Mst_BirthPolicy_Update` | Request body rỗng (`Check.strJson = null`), lỗi định dạng số voucher detail (`Check.VoucherValue`, `Check.VoucherExpireDays`), hoặc ngoại lệ CSDL (`Params.Exception = ex.Message`) | `Check.strJson`, `Check.VoucherValue`, `Check.VoucherExpireDays`, `Exception` |
| `ErridnLoyalty.WAS_Mst_BirthPolicy_Update` | Khối `Mst_BirthPolicy` null trong body | `Check.Mst_BirthPolicy = null` |
| `ErridnLoyalty.Mst_BirthPolicy_SaveX_InvalidFlagPointVoucher` | Cả 2 cờ `FlagPoint` và `FlagVoucher` cùng bằng `"1"` hoặc cùng khác `"1"` | `Check.FlagPoint`, `Check.FlagVoucher`, `Expect` |
| `ErridnLoyalty.Mst_BirthPolicy_CheckDB_BirthPolicyNoNotFound` | Chính sách không tồn tại trong CSDL | `Check.BirthPolicyNo = <value>` |
| `ErridnLoyalty.Mst_BirthPolicy_UpdateX_InvalidEffDateStart` | Chính sách đã bắt đầu hiệu lực trong quá khứ (`EffDateStart < today`) | `Check.BirthPolicyNo`, `Check.EffDateEnd` |
| `ErridnLoyalty.Mst_BirthPolicy_UpdateX_InvalidValue` | `VoucherValue < 0`, `VoucherExpireDays < 0`, hoặc `VoucherValue > 0` nhưng `VoucherExpireDays <= 0` | `Check.CardType`, `Check.VoucherValue`, `Check.VoucherExpireDays`, `Check.ErrCoditional` |

---

### 2.6. Delete (#75)

Xóa một chính sách sinh nhật và toàn bộ cấu hình chi tiết trực thuộc khỏi cơ sở dữ liệu.

- **Route**: `POST /api/mst_birthpolicy/delete`
- **Controller Action**: [`Mst_BirthPolicyControllerPort.Delete`](../../Controllers/Port/Mst_BirthPolicyControllerPort.cs#L920-L931) và [`DeleteXAsync`](../../Controllers/Port/Mst_BirthPolicyControllerPort.cs#L951-L1048)
- **Tham chiếu nguồn**: Gate `Mst_BirthPolicyController.cs:535-600` → API `WA_Mst_BirthPolicy_Save` (`FlagIsDelete = "1"`) → Biz `Master.Loyalty.cs:9675-10346` (`bIsDelete = true`)

#### Bảng tham số Request

Payload JSON gửi qua Request Body (`MstBirthPolicyDeleteReq`):

| Tên tham số | Kiểu dữ liệu | Bắt buộc | Mặc định | Ý nghĩa & Quy tắc |
|---|---|---|---|---|
| `BirthPolicyNo` | `string?` | **Có** | | Mã chính sách cần xóa (`gate :535`). |

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. **Kiểm tra mã rỗng tại Gate** (`gate :554, :585`):
   - Nếu `req == null` hoặc `string.IsNullOrWhiteSpace(req.BirthPolicyNo)` → Trả về HTTP 400 kèm mã lỗi `ErridnLoyalty.invalid_BirthPolicyNo` (`Check.BirthPolicyNo = <value>`).
2. Mở Transaction CSDL độc lập (`DeleteXAsync`, `Master.Loyalty.cs:9675-10346, bIsDelete = true`).
3. Chuẩn hóa mã qua `StdParam`.
4. **Kiểm tra tồn tại trong CSDL** (`Master.Loyalty.cs:9757-9770`):
   - Nếu không tìm thấy chính sách trong CSDL (`dl == null`): **Không báo lỗi**, commit transaction và trả về ngay HTTP 200 `{ Success = true, Data = null }` theo đúng hành vi nguồn (`bIsDelete -> goto Done`, `gate :579-580`).
5. **Kiểm tra ngày bắt đầu hiệu lực** (`Master.Loyalty.cs:9879-9906`):
   - Nếu `dl.EffDateStart <= today` (DateTime.UtcNow.Date): Chính sách đã bắt đầu hiệu lực → Rollback transaction, trả về HTTP 400 `ErridnLoyalty.Mst_BirthPolicy_SaveX_InvalidEffDateStart` (`Check.BirthPolicyNo`, `Check.EffDateStart`). CHỈ xóa được chính sách chưa bắt đầu hiệu lực (`EffDateStart > today`).
6. **Xác định chính sách kế tiếp trước khi xóa** (`:9908-9941`):
   - Tìm `dlAfter`: chính sách cùng `ParamPrType` có `LogLUDTimeUTC > dl.LogLUDTimeUTC` (top 1 order by `LogLUDTimeUTC ASC`).
7. **Xóa dữ liệu** (`:10131-10151`):
   - Xóa toàn bộ các bản ghi con trong `Mst_BirthPolicyDtl` có `BirthPolicyNo` tương ứng.
   - Xóa bản ghi chính trong `Mst_BirthPolicy`.
8. **Điều chỉnh chính sách liền kề** (`:10272-10318`):
   - Nếu có `dlAfter`: lùi `dlAfter.EffDateStart` về `dlEffDateStart` của dòng vừa bị xóa.
   - Nếu không có `dlAfter`: chính sách mới nhất còn lại cùng `ParamPrType` được gán `EffDateEnd = new DateTime(2100, 1, 1, 0, 0, 0, DateTimeKind.Utc)`.
9. Bắt ngoại lệ CSDL: Rollback transaction, trả về HTTP 400 `ErridnLoyalty.Mst_BirthPolicy_Save` (`Params.Exception = ex.Message`).
10. Commit Transaction và trả về HTTP 200 `{ Success = true, Data = null }` (`gate :591-592`).

#### Bảng mã lỗi `ErridnLoyalty.*`

| Mã lỗi | Điều kiện kích hoạt | Các tham số trả về trong `Params` |
|---|---|---|
| `ErridnLoyalty.invalid_BirthPolicyNo` | `BirthPolicyNo` rỗng, null hoặc chỉ chứa khoảng trắng | `Check.BirthPolicyNo = <value>` |
| `ErridnLoyalty.Mst_BirthPolicy_SaveX_InvalidEffDateStart` | Chính sách đã bắt đầu có hiệu lực (`EffDateStart <= today`) | `Check.BirthPolicyNo = <no>`, `Check.EffDateStart = <date>` |
| `ErridnLoyalty.Mst_BirthPolicy_Save` | Lỗi ngoại lệ CSDL trong transaction | `Params.Exception = ex.Message` |

---

### 2.7. Export (#76)

Xuất danh sách chính sách sinh nhật ra tập tin Excel `.xlsx`.

- **Route**: `POST /api/mst_birthpolicy/export`
- **Controller Action**: [`Mst_BirthPolicyControllerPort.Export`](../../Controllers/Port/Mst_BirthPolicyControllerPort.cs#L1082-L1145)
- **Tham chiếu nguồn**: Gate `Mst_BirthPolicyController.cs:604-683` → API `WA_Mst_BirthPolicy_Get` → Biz `Master.Loyalty.cs:10606-11085`

#### Bảng tham số Request

Payload JSON gửi qua Request Body (`MstBirthPolicySearchReq`), hỗ trợ body rỗng:

| Tên tham số | Kiểu dữ liệu | Bắt buộc | Mặc định | Ý nghĩa & Quy tắc |
|---|---|---|---|---|
| `KeyWord` | `string?` | Không | `null` | Lọc theo mã chính sách (`BirthPolicyNo LIKE '%kw%'`). |
| `FlagActive` | `string?` | Không | `null` | Lọc theo cờ hoạt động (toán tử `IN`). |

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Gọi `GetXAsync` với phân trang `recordStart = 0`, `recordCount = 123456000` (`gate :646-647`).
2. Kiểm tra dữ liệu rỗng: Nếu không có bản ghi nào → Trả về HTTP 400 kèm mã lỗi `ErridnLoyalty.Mst_BirthPolicy_Export` và `Message = "Không có dữ liệu"` (`gate :654, :668-672`).
3. Từ điển cột `GetImportDicColums()` gồm đúng 4 cột (`gate :658, :724-733`):
   - `BirthPolicyNo`: `"Mã chính sách (*)"`
   - `BirthPolicyName`: `"Tên chính sách (*)"`
   - `EffDateStart`: `"Thời gian áp dụng"`
   - `EffDateEnd`: `"Thời gian kết thúc"`
4. Sinh file Excel sheet `"Mst_BirthPolicy"` qua [`PortExcel.ExportToExcelFromList`](../../Data/PortExcel.cs).
5. Trả về HTTP 200 `{ Success = true, Data = { Url = baseAddress + virtualPath } }` (`gate :662-665`).

#### Bảng mã lỗi `ErridnLoyalty.*`

| Mã lỗi | Điều kiện kích hoạt | Các tham số trả về trong `Params` |
|---|---|---|
| `ErridnLoyalty.Mst_BirthPolicy_Export` | Không có bản ghi nào thỏa mãn điều kiện lọc | `Message = "Không có dữ liệu"` |

---

## 3. Khác biệt So với Nguồn & Danh sách GAP (Known Gaps)

| Hạng mục | Trạng thái ở Nguồn | Triển khai ở Đích | Phân loại & Ghi chú GAP |
|---|---|---|---|
| **Xác thực & Phân quyền** | Gate kiểm tra `CheckGatewayAuthentication`, `CheckAuthorize`; Biz kiểm tra `Sys_User_CheckAuthorize`. | Bỏ qua toàn bộ kiểm tra phân quyền. | Bỏ qua có chủ đích (**Loại Đ**). |
| **Ghi nhật ký hệ thống** | Ghi bản ghi `RequestLog` và `ReturnLog` vào CSDL cho mọi thao tác. | Bỏ qua ghi log DB. | Bỏ qua có chủ đích (**Loại Đ**). |
| **Quyền drAbilityOfUser** | Biz gọi `drAbilityOfUser` nhưng dòng sử dụng bị comment ở nguồn (`:10932`). | Bỏ qua kiểm tra `drAbilityOfUser`. | Khớp nguồn 1-1. |
| **Cột FlagPoint / FlagVoucher** | Câu truy vấn INSERT trong mã nguồn gốc ghi 2 cột này vào DB thực tế nhưng không có trong DDL `.sql` của repo. | Bổ sung 2 cột vào Model EF và tự sinh cột khi khởi chạy qua `AddMissingColumnsAsync`. | **GAP**: Thiếu DDL gốc trong repo. |
| **Phiên người dùng đăng nhập** | Trích xuất `NetworkID`, `UpdateBy` từ session người dùng `orgUserUI`. | Lấy `Biz_NetworkID` từ cấu hình `IConfiguration`, gán `LogLUBy = "port"`. | **GAP**: Chưa có session người dùng. |
| **Khóa ngoại Navigation EF** | DDL nguồn có khai báo FK từ Detail sang Header và CardType. | Chỉ thực hiện kiểm tra logic tồn tại mã khi Save/Update, không cấu hình navigation EF Core. | **GAP**: Độc lập hóa cấu trúc entity. |

---

## 4. Nhật ký Thay đổi & Đối chiếu Tiến độ (Changelog)

| STT Hạng mục | Tên Endpoint & Action | Trạng thái | Mã Commit Git | Mô tả tóm tắt thay đổi |
|---|---|---|---|---|
| **#70** | `Mst_BirthPolicyController/Search` | Đã hoàn thành | `87e7bc8` | Khởi tạo model [`Mst_BirthPolicy.cs`](../../Models/Port/Mst_BirthPolicy.cs), cấu hình EF Core cho Header và Detail, Seeder demo và action tìm kiếm phân trang kèm tính `FlagShow`. |
| **#71** | `Mst_BirthPolicyController/GetAllActive` | Đã hoàn thành | `1f45cdd` | Bổ sung endpoint lấy danh sách chính sách đang hoạt động, trả về bọc trong `{ Lst_Mst_BirthPolicy }`. |
| **#72** | `Mst_BirthPolicyController/GetByBirthPolicyNo` | Đã hoàn thành | `1177dce` | Bổ sung endpoint tra cứu chi tiết chính sách và danh sách mức ưu đãi theo hạng thẻ, trả về `null` khi mã rỗng. |
| **#73** | `Mst_BirthPolicyController/Save` | Đã hoàn thành | `41cfecb` | Thêm mới chính sách sinh nhật, tự gán ngày kết thúc 2100-01-01, kiểm tra trùng mã và lưu header + detail trong 1 transaction. |
| **#74** | `Mst_BirthPolicyController/Update` | Đã hoàn thành | `127bc2b` | Cập nhật thông tin chính sách, thực hiện upsert xóa-thêm lại toàn bộ danh sách detail theo hạng thẻ trong 1 transaction. |
| **#75** | `Mst_BirthPolicyController/Delete` | Đã hoàn thành | `1eee9a3` | Xóa chính sách theo cơ chế SaveX `bIsDelete = true`, xóa đồng thời header và detail trong 1 transaction. |
| **#76** | `Mst_BirthPolicyController/Export` | Đã hoàn thành | `7c5d72f` | Xuất danh sách chính sách ra file Excel 4 cột từ điển, báo lỗi khi không có dữ liệu. |
