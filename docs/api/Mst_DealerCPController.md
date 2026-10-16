# Tham chiếu API Mst_DealerCPController (Port 1-1)

| Thông tin | Giá trị |
|---|---|
| **Phạm vi** | Tài liệu tham chiếu chi tiết 10 endpoint đã port 1-1 của Mst_DealerCPController (#110 - #114, #116 - #118, #120 - #121) và phân tích 2 endpoint chưa port (#115, #119) |
| **Cập nhật** | 2026-10-16 |
| **Phiên bản mã** | `ed28f8f` |
| **Trạng thái** | Nháp — chờ review |

---

## 1. Tóm tắt Nghiệp vụ & Cấu trúc Dữ liệu

### 1.1. Mục đích Nghiệp vụ
API `Mst_DealerCPController` quản lý danh mục đại lý phân phối (`Mst_DealerCP`) và mối quan hệ liên kết giữa đại lý với tổ chức sở hữu (`Map_DealerCP_Org`) trong hệ thống MiniLoyalty. Module cung cấp các chức năng tìm kiếm phân trang theo từ khóa và trạng thái hoạt động, lấy danh sách tất cả đại lý đang hoạt động kèm liên kết tổ chức, tra cứu chi tiết đại lý theo mã hoặc theo tổ chức, thêm mới đại lý trực tiếp qua `CreateOld` kèm tự động phân cấp đơn vị kinh doanh BU toàn bảng, cập nhật thông tin đại lý, xóa đơn lẻ/xóa hàng loạt (xóa một phần), xuất danh sách ra tập tin Excel và xuất file mẫu chuẩn.

> [!IMPORTANT]
> **Hiện trạng Port Endpoint**: Trong tổng số 12 endpoint của controller nguồn:
> - **10 endpoint ĐÃ hoàn thành port 1-1**: `Search` (#110), `GetAllActive` (#111), `GetByDLCPCode` (#112), `GetByOrgID` (#113), `CreateOld` (#114), `Update` (#116), `Delete` (#117), `DeleteMultiple` (#118), `Export` (#120), `ExportTemplate` (#121).
> - **2 endpoint CHƯA port do phụ thuộc module ngoài**: `Create` (#115) và `Import` (#119). Hai endpoint này ở tầng gate nguồn bắt buộc phải gọi dịch vụ tạo người nộp thuế `Mst_NNT` (`WA_Mst_NNT_Create`) trước khi tạo bản ghi đại lý. Do phân hệ `Mst_NNT` chưa được triển khai trong MiniLoyalty nên 2 endpoint này được tạm hoãn chờ phụ thuộc. Thay vào đó, việc tạo mới đại lý được thực thi qua `CreateOld` (#114).

### 1.2. Bảng Đối chiếu Nguồn Tra cứu (Source Code Mapping)

| Thành phần | Đường dẫn tập tin nguồn (`2022.7.Loyalty`) | Tập tin đích tương ứng (`MiniLoyalty`) |
|---|---|---|
| **Gate Controller** | `13.ClientGate\V10\idn.Loyalty.WebAPI\Controllers\Mst_DealerCPController.cs:26-1288` | [`Controllers/Port/Mst_DealerCPControllerPort.cs`](../../Controllers/Port/Mst_DealerCPControllerPort.cs) |
| **API Backend** | `11.BackEnd\V10\idn.Loyalty.Biz.Web\Controllers\MstDealerCPController.cs:21-305` | Tích hợp trực tiếp vào [`Controllers/Port/Mst_DealerCPControllerPort.cs`](../../Controllers/Port/Mst_DealerCPControllerPort.cs) |
| **Nghiệp vụ (Biz)** | `11.BackEnd\V10\idn.Loyalty.Biz\Master.Loyalty.cs` (Get `:3937-4433`; Create `:2973-3322`; Update `:3323-3660`; Delete `:3662-3935`; CheckDB `:2870-2934`; UpdBU `:2935-2971`) | Tích hợp trực tiếp vào [`Controllers/Port/Mst_DealerCPControllerPort.cs`](../../Controllers/Port/Mst_DealerCPControllerPort.cs) |
| **Entity Đại lý** | `12.Dev.Common\idn.Loyalty.Common\Models\Loyalty\Mst_DealerCP.cs:11-24` | [`Models/Port/Mst_DealerCP.cs:15-58`](../../Models/Port/Mst_DealerCP.cs#L15-L58) |
| **Entity Liên kết** | `12.Dev.Common\idn.Loyalty.Common\Models\Loyalty\Map_DealerCP_Org.cs:11-16` | [`Models/Port/Mst_DealerCP.cs:66-85`](../../Models/Port/Mst_DealerCP.cs#L66-L85) |
| **Seeder Demo** | `Order => 12` (chạy sau Mst_OrgSeeder có Order 10) | [`Models/Port/Mst_DealerCP.cs:111-235`](../../Models/Port/Mst_DealerCP.cs#L111-L235) |

### 1.3. Cấu trúc Bảng CSDL (`Mst_DealerCP` & `Map_DealerCP_Org`)

Bảng dữ liệu quản lý đại lý gồm 2 bảng (nguồn không có script DDL tạo bảng trong repo, cấu trúc cột được xác nhận qua model và code nghiệp vụ; khóa chính là khóa draft):

#### 1. Bảng `Mst_DealerCP` (14 cột, Khóa chính `DLCPCode`):

| Tên cột | Kiểu EF Core | Khóa | Cho phép NULL | Ghi chú & Ý nghĩa |
|---|---|---|---|---|
| `DLCPCode` | `string` | **PK** | Không | Mã đại lý (Khóa chính draft - không có DDL nguồn). |
| `NetworkID` | `string?` | | Có | Mã mạng lưới. Ở đích lấy từ cấu hình `Biz_NetworkID` (mặc định `"1001"`). |
| `DLCPCodeParent` | `string?` | | Có | Mã đại lý cấp trên trong cây phân cấp. |
| `DLCPBUCode` | `string?` | | Có | Mã đơn vị nghiệp vụ phân cấp (BU Code). Tự động sinh bởi `UpdBU`. |
| `DLCPBUPattern` | `string?` | | Có | Mẫu tìm kiếm phân cấp BU (Pattern). Tự động sinh bởi `UpdBU`. |
| `DLCPLevel` | `string?` | | Có | Cấp bậc đại lý (1, 2, 3...). Tự động tính bởi `UpdBU`. |
| `DLCPName` | `string?` | | Có | Tên đầy đủ của đại lý. |
| `DLCPPhoneNo` | `string?` | | Có | Số điện thoại liên hệ của đại lý. |
| `FlagRoot` | `string?` | | Có | Cờ đại lý gốc: `"1"` (Đại lý cấp 1 không có cha), `"0"` (Đại lý con). |
| `FlagActive` | `string?` | | Có | Cờ trạng thái: `"1"` (Hoạt động), `"0"` (Tạm ngừng). |
| `FlagBirthPoints` | `string?` | | Có | Cờ đánh dấu đại lý được phép tặng điểm sinh nhật: `"1"`/`"0"`. |
| `Remark` | `string?` | | Có | Ghi chú thêm về đại lý. |
| `LogLUDTimeUTC` | `DateTime?` | | Có | Thời điểm cập nhật cuối cùng theo chuẩn UTC. |
| `LogLUBy` | `string?` | | Có | Tài khoản cập nhật cuối cùng (mặc định gán `"port"` ở đích). |

#### 2. Bảng Liên kết `Map_DealerCP_Org` (6 cột, Khóa chính ghép `(DLCPCode, OrgID)`):

| Tên cột | Kiểu EF Core | Khóa | Cho phép NULL | Ghi chú & Ý nghĩa |
|---|---|---|---|---|
| `DLCPCode` | `string` | **PK1** | Không | Mã đại lý liên kết. |
| `OrgID` | `string` | **PK2** | Không | Mã tổ chức liên kết. |
| `NetworkID` | `string?` | | Có | Mã mạng lưới nghiệp vụ. |
| `FlagActive` | `string?` | | Có | Cờ trạng thái liên kết: `"1"`/`"0"`. |
| `LogLUDTimeUTC` | `DateTime?` | | Có | Thời điểm cập nhật cuối cùng UTC. |
| `LogLUBy` | `string?` | | Có | Tài khoản cập nhật cuối cùng. |

**Dữ liệu mẫu khởi tạo (Seeder - `Mst_DealerCPSeeder`, Idempotent)**:
- Gồm 4 đại lý: `HTV` (Tổng công ty, parent null, FlagRoot `'1'`, FlagActive `'1'`), `DL01` (Hà Nội, parent HTV, FlagRoot `'0'`, FlagActive `'1'`), `DL02` (Hải Phòng, parent HTV, FlagRoot `'0'`, FlagActive `'0'`), `DL03` (Đà Nẵng, parent HTV, FlagRoot `'0'`, FlagActive `'1'`).
- Bảng liên kết: `(DL01, 4020320001)`, `(DL02, 4020320001)`, `(DL02, 4020320003)`.

---

## 2. Đặc tả Chi tiết Từng Endpoint API

---

### 2.1. Search (#110)

Tìm kiếm danh mục đại lý phân trang theo từ khóa và trạng thái hoạt động.

- **Route**: `POST /api/mst_dealercp/search`
- **Controller Action**: [`Mst_DealerCPControllerPort.Search`](../../Controllers/Port/Mst_DealerCPControllerPort.cs#L190-L235)
- **Tham chiếu nguồn**: Gate `Mst_DealerCPController.cs:26-106` → API `MstDealerCPController.cs:21-90` → Biz `Master.Loyalty.cs:3937-4433`

#### Bảng tham số Request

Payload JSON gửi qua Request Body (`Mst_DealerCPSearchRequest`):

| Tên tham số | Kiểu dữ liệu | Bắt buộc | Mặc định | Ý nghĩa & Quy tắc |
|---|---|---|---|---|
| `KeyWord` | `string?` | Không | `null` | Từ khóa tìm kiếm theo tên đại lý hoặc mã đại lý (`gate :372-377`). |
| `FlagActive` | `string?` | Không | `null` | Danh sách trạng thái phân cách bởi dấu phẩy (toán tử `IN ('a','b')`, `gate :380-388`). |
| `Ft_PageIndex` | `int` | Không | `0` | Chỉ số trang cần lấy (0-based). Mặc định là `0`. |
| `Ft_PageSize` | `int` | Không | `10` | Số dòng trên mỗi trang. Nếu `<= 0` trả về mảng rỗng `DataList = []` (`gate :79-90`). |

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Chuẩn hóa phân trang: `pageIndex = Math.Max(0, req.Ft_PageIndex)`, `pageSize = req.Ft_PageSize`.
2. Lọc dữ liệu qua `GetXAsync` ([`Mst_DealerCPControllerPort.cs:1162-1215`](../../Controllers/Port/Mst_DealerCPControllerPort.cs#L1162-L1215)):
   - **KeyWord**: `EF.Functions.Like((x.DLCPName ?? "").ToLower(), PortQuery.Contains(keyWord), PortQuery.Esc) || EF.Functions.Like((x.DLCPCode ?? "").ToLower(), PortQuery.Contains(keyWord), PortQuery.Esc)`.
   - **FlagActive**: Tách chuỗi theo dấu phẩy, lọc `ds.Contains((x.FlagActive ?? "").ToLower())`.
3. Sắp xếp thứ tự bản ghi: Phân trang theo `mcp.DLCPCode ASC` (`Master.Loyalty.cs:4243`), select cuối sắp theo `t.MyIdxSeq ASC` (`:4314`).
4. Đếm tổng số bản ghi: `itemCount = await ordered.CountAsync()`.
5. Trích xuất trang: Nếu `pageSize <= 0` trả về danh sách rỗng; nếu `pageSize > 0` gọi `.Skip((int)start).Take((int)pageSize)`.
6. Enrich dữ liệu: Truy vấn các bản ghi `Map_DealerCP_Org` tương ứng và gán vào thuộc tính `Lst_Map_DealerCP_Org`.
7. Gán `MyIdxSeq = start + i` (0-based) và tính toán `PageCount`.

#### Bảng mã lỗi
Endpoint luôn trả về HTTP 200.

---

### 2.2. GetAllActive (#111)

Lấy danh sách tất cả đại lý đang hoạt động (`FlagActive = '1'`) kèm danh sách các tổ chức liên kết.

- **Route**: `POST /api/mst_dealercp/getallactive`
- **Controller Action**: [`Mst_DealerCPControllerPort.GetAllActive`](../../Controllers/Port/Mst_DealerCPControllerPort.cs#L60-L96)
- **Tham chiếu nguồn**: Gate `Mst_DealerCPController.cs:107-230` → API `MstDealerCPController.cs:21-90` → Biz `Master.Loyalty.cs:3937-4433`

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Không nhận body, phân trang toàn bộ (`Ft_RecordStart = 0`, `Ft_RecordCount = 123456000`).
2. **Không lọc OrgID**: Tầng gate truyền `OrgID = orgUserUI.OrgId` (`gate :135`), nhưng hàm `strWhereClause_Mst_DealerCP` (`:364-389`) **không dùng OrgID trong mệnh đề WHERE**, do đó chỉ lọc `FlagActive = "1"` (`gate :53-59`).
3. Sắp xếp danh sách theo `DLCPCode ASC`.
4. Enrich danh sách tổ chức liên kết từ `Map_DealerCP_Org` cho từng đại lý.
5. Trả về mảng đối tượng `List<Mst_DealerCPUI>` hoặc `null` nếu danh sách rỗng (`gate :214-217`).

#### Bảng mã lỗi
Endpoint luôn trả về HTTP 200.

---

### 2.3. GetByDLCPCode (#112)

Tra cứu thông tin chi tiết một đại lý theo mã và danh sách tổ chức liên kết.

- **Route**: `POST /api/mst_dealercp/getbydlcpcode`
- **Controller Action**: [`Mst_DealerCPControllerPort.GetByDLCPCode`](../../Controllers/Port/Mst_DealerCPControllerPort.cs#L98-L150)
- **Tham chiếu nguồn**: Gate `Mst_DealerCPController.cs:231-296` → API `MstDealerCPController.cs:21-90` → Biz `Master.Loyalty.cs:3937-4433`

#### Bảng tham số Request

Payload JSON gửi qua Request Body (`Mst_DealerCPGetByDLCPCodeRequest`):

| Tên tham số | Kiểu dữ liệu | Bắt buộc | Mặc định | Ý nghĩa & Quy tắc |
|---|---|---|---|---|
| `DLCPCode` | `string?` | Có | `null` | Mã đại lý cần tra cứu (`gate :231`). |

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. **Kiểm tra mã rỗng** (`gate :250, :283-288`):
   - Nếu `DLCPCode` rỗng hoặc null: Không truy vấn CSDL, trả về ngay HTTP 200 `{ Success = true, Data = { Mst_DealerCP = null, Lst_Map_DealerCP_Org = null } }`.
2. Chuẩn hóa mã qua `StdParam`.
3. Tra cứu Header: Lấy đại lý theo `DLCPCode =` (không phân biệt hoa thường, không lọc `FlagActive` nên đại lý ngừng hoạt động như `DL02` vẫn lấy được).
4. Tra cứu Detail: Lấy danh sách tổ chức liên kết từ `Map_DealerCP_Org` theo `DLCPCode`.
5. Trả về đối tượng `{ Mst_DealerCP = dealer, Lst_Map_DealerCP_Org = maps.Count > 0 ? maps : null }`.

#### Bảng mã lỗi
Endpoint luôn trả về HTTP 200.

---

### 2.4. GetByOrgID (#113)

Lấy danh sách các đại lý đang hoạt động thuộc về một tổ chức cụ thể.

- **Route**: `POST /api/mst_dealercp/getbyorgid`
- **Controller Action**: [`Mst_DealerCPControllerPort.GetByOrgID`](../../Controllers/Port/Mst_DealerCPControllerPort.cs#L152-L188)
- **Tham chiếu nguồn**: Gate `Mst_DealerCPController.cs:297-360` → API `MstDealerCPController.cs:21-90` → Biz `Master.Loyalty.cs:3937-4433`

#### Bảng tham số Request

Payload JSON gửi qua Request Body (`Mst_DealerCPGetByOrgIDRequest`):

| Tên tham số | Kiểu dữ liệu | Bắt buộc | Mặc định | Ý nghĩa & Quy tắc |
|---|---|---|---|---|
| `OrgID` | `string?` | Có | `null` | Mã tổ chức cần lấy danh sách đại lý (`gate :297`). |

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Kiểm tra mã rỗng: Nếu `OrgID` null hoặc rỗng → Trả về ngay HTTP 200 `{ Success = true, Data = null }`.
2. Chuẩn hóa `OrgID` qua `StdParam`.
3. Lọc danh sách qua quan hệ `INNER JOIN` giữa `Mst_DealerCP` và `Map_DealerCP_Org` theo `OrgID` và `Mst_DealerCP.FlagActive = '1'` (`gate :407-419`).
4. Sắp xếp danh sách theo `DLCPCode ASC`.
5. Trả về mảng danh sách đại lý hoặc `null` nếu không tìm thấy (`gate :344-347`).

#### Bảng mã lỗi
Endpoint luôn trả về HTTP 200.

---

### 2.5. CreateOld (#114)

Thêm mới đại lý phân phối trực tiếp (không thông qua tạo tổ chức nộp thuế Mst_NNT) kèm tự động cập nhật phân cấp BU toàn bảng.

- **Route**: `POST /api/mst_dealercp/createold`
- **Controller Action**: [`Mst_DealerCPControllerPort.CreateOld`](../../Controllers/Port/Mst_DealerCPControllerPort.cs#L374-L842)
- **Tham chiếu nguồn**: Gate `Mst_DealerCPController.cs:436-521` → API `WA_Mst_DealerCP_Create` (`:93-162`) → Biz `Master.Loyalty.cs:2973-3322`

#### Bảng tham số Request

Payload JSON gửi qua Request Body (`Mst_DealerCPCreateOldRequest`):

| Tên tham số | Kiểu dữ liệu | Bắt buộc | Mặc định | Ý nghĩa & Quy tắc |
|---|---|---|---|---|
| `DLCPCode` | `string?` | **Có** | | Mã đại lý mới (chuẩn hóa qua `StdParam`). |
| `DLCPCodeParent` | `string?` | Không | `null` | Mã đại lý cấp trên (nếu có). |
| `DLCPName` | `string?` | **Có** | | Tên đại lý (chuẩn hóa qua `StdStr`). |
| `DLCPPhoneNo` | `string?` | **Có** | | Số điện thoại đại lý (chuẩn hóa qua `StdStr`). |
| `Remark` | `string?` | Không | `null` | Ghi chú thêm. |

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. **Kiểm tra body rỗng / null**: Nếu `req == null` → Trả về HTTP 400 `ErridnLoyalty.Mst_DealerCP_Create` (`Check.strJson = null`, `gate :446-453`).
2. Mở Transaction CSDL cho toàn bộ quá trình tạo và tính toán BU (`Master.Loyalty.cs:3095`).
3. Chuẩn hóa chuỗi: `DLCPCode` (StdParam), `DLCPCodeParent` (StdParam), `DLCPName` (StdStr), `DLCPPhoneNo` (StdStr), `Remark` (StdStr).
4. **Kiểm tra mã đại lý**: Nếu `DLCPCode` rỗng hoặc null → Rollback, trả về HTTP 400 `ErridnLoyalty.Mst_DealerCP_Create_InvalidDLCPCode` (`Check.strDLCPCode = null`, `Check.DLCPCode = null`, `Master.Loyalty.cs:3248-3258`).
5. **Kiểm tra trùng mã trong CSDL** (`Master.Loyalty.cs:3260-3266, :2907-2917`): Nếu mã đã tồn tại → Rollback, trả về HTTP 400 `ErridnLoyalty.Mst_DealerCP_CheckDB_DLExist` (`Check.DLCPCode = <value>`).
6. **Kiểm tra đại lý cha (nếu có `DLCPCodeParent`)** (`:3270-3281`):
   - Nếu không tìm thấy đại lý cha trong CSDL → Rollback, trả về HTTP 400 `ErridnLoyalty.Mst_DealerCP_CheckDB_DLNotFound` (`Check.DLCPCode = <parentCode>`, `:2896-2906`).
   - Nếu đại lý cha có `FlagActive != "1"` → Rollback, trả về HTTP 400 `ErridnLoyalty.Mst_DealerCP_CheckDB_FlagActiveNotMatched` (`Check.DLCPCode = <parentCode>`, `Check.strFlagActiveListToCheck = "1"`, `DB.FlagActive = <parent.FlagActive>`, `:2920-2933`).
7. Khởi tạo bản ghi `Mst_DealerCP` (14 cột, `Master.Loyalty.cs:3292-3305`):
   - `DLCPCode = strDLCPCode`, `DLCPCodeParent = strDLCPCodeParent`.
   - `FlagRoot = string.IsNullOrEmpty(DLCPCodeParent) ? "1" : "0"`.
   - `FlagActive = "1"`, `FlagBirthPoints = "1"`.
   - `NetworkID` lấy từ cấu hình `Biz_NetworkID` (mặc định `"1001"`).
   - `LogLUDTimeUTC = DateTime.UtcNow`, `LogLUBy = "port"`.
   - *(Lưu ý: Không kiểm tra tên đại lý rỗng hay số điện thoại rỗng theo đúng mã nguồn)*.
8. Lưu bản ghi vào CSDL và gọi `Mst_DealerCP_UpdBUAsync` để tính toán lại toàn bộ `DLCPBUCode`, `DLCPBUPattern`, `DLCPLevel` trong cây phân cấp (`Master.Loyalty.cs:2935-2971`).
9. Commit Transaction, trả về HTTP 200 `{ Success = true, Data = null }` (`gate :505-506`).

#### Bảng mã lỗi `ErridnLoyalty.*`

| Mã lỗi | Điều kiện kích hoạt | Các tham số trả về trong `Params` |
|---|---|---|
| `ErridnLoyalty.Mst_DealerCP_Create` | Request body rỗng hoặc null | `Check.strJson = null` |
| `ErridnLoyalty.Mst_DealerCP_Create_InvalidDLCPCode` | `DLCPCode` rỗng hoặc chỉ có khoảng trắng | `Check.strDLCPCode = null`, `Check.DLCPCode = null` |
| `ErridnLoyalty.Mst_DealerCP_CheckDB_DLExist` | Mã đại lý đã tồn tại trong CSDL | `Check.DLCPCode = <value>` |
| `ErridnLoyalty.Mst_DealerCP_CheckDB_DLNotFound` | Đại lý cha `DLCPCodeParent` không tồn tại trong CSDL | `Check.DLCPCode = <parentCode>` |
| `ErridnLoyalty.Mst_DealerCP_CheckDB_FlagActiveNotMatched` | Đại lý cha đang ở trạng thái ngừng hoạt động (`FlagActive != '1'`) | `Check.DLCPCode = <parentCode>`, `Check.strFlagActiveListToCheck = "1"`, `DB.FlagActive = <parent.FlagActive>` |

---

### 2.6. Update (#116)

Cập nhật thông tin đại lý phân phối (không cập nhật đại lý cha và không tính lại BU).

- **Route**: `POST /api/mst_dealercp/update`
- **Controller Action**: [`Mst_DealerCPControllerPort.Update`](../../Controllers/Port/Mst_DealerCPControllerPort.cs#L844-L963)
- **Tham chiếu nguồn**: Gate `Mst_DealerCPController.cs:700-770` → API `WA_Mst_DealerCP_Update` (`:165-234`) → Biz `Master.Loyalty.cs:3323-3660`

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. **Kiểm tra body rỗng / null**: Trả về HTTP 400 `ErridnLoyalty.Mst_DealerCP_Update` (`Check.strJson = null`, `gate :710-717`).
2. Mở Transaction CSDL độc lập (`Master.Loyalty.cs:3452`).
3. Chuẩn hóa: `DLCPCode` (StdParam), `DLCPName` (StdStr), `DLCPPhoneNo` (StdStr), `Remark` (StdStr), `FlagActive` (StdFlag), `FlagBirthPoints` (StdFlag).
   *(Lưu ý: Nếu không gửi `FlagActive` hoặc `FlagBirthPoints`, `StdFlag` sẽ hạ về `"0"` theo đúng mã nguồn `Master.Loyalty.cs:3611-3612`)*.
4. **Kiểm tra mã tồn tại trong CSDL**: Nếu `string.IsNullOrEmpty(strDLCPCode)` hoặc không tìm thấy đại lý trong CSDL → Rollback, trả về HTTP 400 `ErridnLoyalty.Mst_DealerCP_CheckDB_DLNotFound` (`Check.DLCPCode = <value>`, `Master.Loyalty.cs:3587-3592`).
5. Cập nhật các trường: `DLCPName`, `DLCPPhoneNo`, `FlagActive`, `FlagBirthPoints`, `Remark`, `LogLUDTimeUTC = DateTime.UtcNow`, `LogLUBy = "port"`.
   *(Lưu ý: Không kiểm tra tên hay số điện thoại rỗng; không cập nhật `DLCPCodeParent` và không gọi lại `Mst_DealerCP_UpdBU` theo đúng mã nguồn `Master.Loyalty.cs:3565-3660`)*.
6. Commit Transaction, trả về HTTP 200 `{ Success = true, Data = null }` (`gate :754-755`).

#### Bảng mã lỗi `ErridnLoyalty.*`

| Mã lỗi | Điều kiện kích hoạt | Các tham số trả về trong `Params` |
|---|---|---|
| `ErridnLoyalty.Mst_DealerCP_Update` | Request body rỗng hoặc null | `Check.strJson = null` |
| `ErridnLoyalty.Mst_DealerCP_CheckDB_DLNotFound` | `DLCPCode` rỗng hoặc đại lý không tồn tại trong CSDL | `Check.DLCPCode = <value>` |

---

### 2.7. Delete (#117)

Xóa một bản ghi đại lý khỏi cơ sở dữ liệu (nguồn giữ nguyên bản ghi trong `Map_DealerCP_Org`).

- **Route**: `POST /api/mst_dealercp/delete`
- **Controller Action**: [`Mst_DealerCPControllerPort.Delete`](../../Controllers/Port/Mst_DealerCPControllerPort.cs#L965-L988) và [`DeleteXAsync`](../../Controllers/Port/Mst_DealerCPControllerPort.cs#L1065-L1160)
- **Tham chiếu nguồn**: Gate `Mst_DealerCPController.cs:774-837` → API `WA_Mst_DealerCP_Delete` (`:236-305`) → Biz `Master.Loyalty.cs:3662-3935`

#### Bảng tham số Request

Payload JSON gửi qua Request Body (`Mst_DealerCPDeleteRequest`):

| Tên tham số | Kiểu dữ liệu | Bắt buộc | Mặc định | Ý nghĩa & Quy tắc |
|---|---|---|---|---|
| `DLCPCode` | `string?` | **Có** | | Mã đại lý cần xóa (`gate :774`). |

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. **Kiểm tra mã rỗng tại Gate** (`gate :793, :823`): Nếu `string.IsNullOrEmpty(strDLCPCode)` → Trả về HTTP 400 kèm mã lỗi `ErridnLoyalty.invalid_DLCPCode` (`Check.DLCPCode = req?.DLCPCode`).
2. Mở Transaction CSDL độc lập qua `DeleteXAsync` (`Master.Loyalty.cs:3773`).
3. Chuẩn hóa mã qua `StdParam`.
4. **Kiểm tra tồn tại trong CSDL**: Nếu không tìm thấy đại lý trong CSDL → Rollback, trả về HTTP 400 `ErridnLoyalty.Mst_DealerCP_CheckDB_DLNotFound` (`Check.DLCPCode = <value>`, `Master.Loyalty.cs:2896-2906`).
5. **Giả lập ràng buộc khóa ngoại (FK)**:
   - Nếu đại lý được tham chiếu bởi `Crd_Member.DLCodeRegis`: Rollback, trả về HTTP 400 `ErridnLoyalty.Mst_DealerCP_Delete` (`Check.DLCPCode = <value>`, `FK = "Crd_Member.DLCodeRegis"`).
   - Nếu đại lý được tham chiếu bởi `Map_QueryDealer_Member.DLCPCode`: Rollback, trả về HTTP 400 `ErridnLoyalty.Mst_DealerCP_Delete` (`Check.DLCPCode = <value>`, `FK = "Map_QueryDealer_Member.DLCPCode"`).
   - Nếu đại lý được tham chiếu bởi `Mst_PromotionProgramSpec.DLCode`: Rollback, trả về HTTP 400 `ErridnLoyalty.Mst_DealerCP_Delete` (`Check.DLCPCode = <value>`, `FK = "Mst_PromotionProgramSpec.DLCode"`).
6. **Xóa dòng đại lý**: Xóa bản ghi trong `Mst_DealerCP` (`Master.Loyalty.cs:3926-3932`). Nguồn giữ nguyên bản ghi trong `Map_DealerCP_Org`, không xóa map; không chạy lại `UpdBU`.
7. Commit Transaction, trả về HTTP 200 `{ Success = true, Data = null }` (`gate :817-818`).

#### Bảng mã lỗi `ErridnLoyalty.*`

| Mã lỗi | Điều kiện kích hoạt | Các tham số trả về trong `Params` |
|---|---|---|
| `ErridnLoyalty.invalid_DLCPCode` | `DLCPCode` rỗng, null hoặc chỉ chứa khoảng trắng | `Check.DLCPCode = null` |
| `ErridnLoyalty.Mst_DealerCP_CheckDB_DLNotFound` | Đại lý không tồn tại trong CSDL | `Check.DLCPCode = <value>` |
| `ErridnLoyalty.Mst_DealerCP_Delete` | Vi phạm ràng buộc khóa ngoại tham chiếu từ `Crd_Member`, `Map_QueryDealer_Member` hoặc `Mst_PromotionProgramSpec` | `Check.DLCPCode = <value>`, `FK = <tên-bảng.cột>` |

---

### 2.8. DeleteMultiple (#118)

Xóa danh sách nhiều đại lý theo cơ chế lặp tuần tự từng Transaction riêng biệt (xóa một phần).

- **Route**: `POST /api/mst_dealercp/deletemultiple`
- **Controller Action**: [`Mst_DealerCPControllerPort.DeleteMultiple`](../../Controllers/Port/Mst_DealerCPControllerPort.cs#L1021-L1051)
- **Tham chiếu nguồn**: Gate `Mst_DealerCPController.cs:839-903` → API `WA_Mst_DealerCP_Delete` → Biz `Master.Loyalty.cs:3662-3935`

#### Bảng tham số Request

Payload JSON gửi qua Request Body là mảng danh sách: `List<Mst_DealerCPDeleteRequest>`:

| Tên tham số | Kiểu dữ liệu | Bắt buộc | Mặc định | Ý nghĩa & Quy tắc |
|---|---|---|---|---|
| `[].DLCPCode` | `string?` | Có | | Mã đại lý cho từng phần tử cần xóa. |

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. **Kiểm tra body rỗng / null**: Trả về HTTP 400 `ErridnLoyalty.Mst_DealerCP_Delete` (`Check.strJson = null`, `gate :849-856`).
2. **Mảng rỗng `[]`**: Trả về ngay HTTP 200 `{ Success = true, Data = null }` (`gate :889-890`).
3. **Lặp tuần tự qua từng phần tử trong danh sách** (`gate :870-887`):
   - Thực thi xóa qua hàm `DeleteXAsync` trong một Transaction độc lập đã commit.
   - **Hành vi Xóa một phần**: Nếu phần tử thứ k gặp lỗi, dừng xử lý ngay và trả về mã lỗi của phần tử thứ k; các phần tử trước k đã xóa thành công khỏi CSDL và không bị rollback.
   - Nếu phần tử thứ k có `DLCPCode` rỗng hoặc không tồn tại trong CSDL: Trả về HTTP 400 `ErridnLoyalty.Mst_DealerCP_CheckDB_DLNotFound` (tầng gate DeleteMultiple không kiểm tra rỗng mã như Delete đơn; lỗi rỗng mã phát sinh từ `Mst_DealerCP_CheckDB` :2896-2906).
   - Nếu phần tử thứ k vi phạm khóa ngoại FK: Trả về HTTP 400 `ErridnLoyalty.Mst_DealerCP_Delete`.
4. Nếu tất cả phần tử hợp lệ: Trả về HTTP 200 `{ Success = true, Data = null }`.

#### Bảng mã lỗi `ErridnLoyalty.*`

| Mã lỗi | Điều kiện kích hoạt | Các tham số trả về trong `Params` |
|---|---|---|
| `ErridnLoyalty.Mst_DealerCP_Delete` | Request body rỗng (`Check.strJson = null`) hoặc phần tử thứ k vi phạm khóa ngoại FK | `Check.strJson` hoặc `FK` |
| `ErridnLoyalty.Mst_DealerCP_CheckDB_DLNotFound` | Phần tử thứ k có mã rỗng hoặc không tồn tại trong CSDL | `Check.DLCPCode = <value>` |

---

### 2.9. Export (#120)

Xuất danh sách đại lý phân phối ra tập tin Excel `.xlsx`.

- **Route**: `POST /api/mst_dealercp/export`
- **Controller Action**: [`Mst_DealerCPControllerPort.Export`](../../Controllers/Port/Mst_DealerCPControllerPort.cs#L237-L315)
- **Tham chiếu nguồn**: Gate `Mst_DealerCPController.cs:1175-1253` → API `WA_Mst_DealerCP_Get` → Biz `Master.Loyalty.cs:3937-4433`

#### Bảng tham số Request

Payload JSON gửi qua Request Body (`Mst_DealerCPExportRequest`), hỗ trợ body rỗng:

| Tên tham số | Kiểu dữ liệu | Bắt buộc | Mặc định | Ý nghĩa & Quy tắc |
|---|---|---|---|---|
| `KeyWord` | `string?` | Không | `null` | Lọc theo tên hoặc mã đại lý. |
| `FlagActive` | `string?` | Không | `null` | Lọc theo cờ hoạt động (toán tử `IN`). |

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Gọi `GetXAsync` với phân trang `recordStart = 0`, `recordCount = 123456000` (`gate :1216-1217`).
2. Kiểm tra dữ liệu rỗng: Nếu không có bản ghi nào → Trả về HTTP 400 kèm mã lỗi `ErridnLoyalty.Mst_DealerCP_Export` và `Message = "Không có dữ liệu"` (`gate :1224, :1238-1242`).
3. Từ điển cột `GetImportDicColums()` gồm đúng 5 cột (`gate :1228, :1296-1308`):
   - `DLCPCode`: `"Mã đại lý (*)"`
   - `DLCPCodeParent`: `"Đại lý cấp trên"`
   - `DLCPName`: `"Tên đại lý (*)"`
   - `DLCPPhoneNo`: `"Điện thoại (*)"`
   - `FlagActive`: `"Trạng thái"`
4. Sinh file Excel sheet `"Mst_DealerCP"` qua [`PortExcel.ExportToExcelFromList`](../../Data/PortExcel.cs).
5. Trả về HTTP 200 `{ Success = true, Data = { Url = baseAddress + virtualPath } }` (`gate :1232-1235`).

#### Bảng mã lỗi `ErridnLoyalty.*`

| Mã lỗi | Điều kiện kích hoạt | Các tham số trả về trong `Params` |
|---|---|---|
| `ErridnLoyalty.Mst_DealerCP_Export` | Không có bản ghi nào thỏa mãn điều kiện lọc | `Message = "Không có dữ liệu"` |

---

### 2.10. ExportTemplate (#121)

Xuất tập tin mẫu Excel dùng cho chức năng nhập dữ liệu đại lý.

- **Route**: `POST /api/mst_dealercp/exporttemplate`
- **Controller Action**: [`Mst_DealerCPControllerPort.ExportTemplate`](../../Controllers/Port/Mst_DealerCPControllerPort.cs#L317-L372)
- **Tham chiếu nguồn**: Gate `Mst_DealerCPController.cs:1256-1288`

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Không nhận body, không kiểm tra quyền, không đọc CSDL (`gate :1264`).
2. Khởi tạo danh sách rỗng 0 dòng `List<Mst_DealerCP>`.
3. Từ điển cột `GetImportDicColumsTemplate()` gồm đúng 6 cột (`gate :1266, :1310-1324`):
   - `DLCPCode`: `"Mã đại lý (*)"`
   - `DLCPCodeParent`: `"Đại lý cấp trên"`
   - `DLCPName`: `"Tên đại lý (*)"`
   - `DLCPPhoneNo`: `"Điện thoại (*)"`
   - `mnnt_MST`: `"Mã số thuế (*)"`
   - `mnnt_ContactEmail`: `"Email (*)"`
4. Sinh file Excel rỗng có dòng tiêu đề sheet `"Mst_DealerCP"`.
5. Trả về HTTP 200 `{ Success = true, Data = { Url = baseAddress + virtualPath } }` (`gate :1270-1274`).

#### Bảng mã lỗi
Endpoint luôn trả về HTTP 200.

---

## 3. Các Endpoint CHƯA PORT (Chờ Phụ thuộc Mst_NNT)

> [!WARNING]
> Hai endpoint dưới đây **CHƯA PORT** do phụ thuộc phân hệ `Mst_NNT`. Tài liệu ghi nhận lý do kỹ thuật và **KHÔNG** liệt kê mã lỗi hay hành vi như thể đã chạy.

| STT Hạng mục | Tên Endpoint & Tuyến | Trạng thái | Lý do Chưa Port & Ràng buộc Phụ thuộc |
|---|---|---|---|
| **#115** | `Mst_DealerCPController/Create`<br>`POST /api/mst_dealercp/create` | **CHƯA PORT** | Tầng gate nguồn (`Mst_DealerCPController.cs:523-698`) nhận thông tin đại lý kèm mã số thuế (`MST`), tên tổ chức (`NNTFullName`) và email (`ContactEmail`). Trước khi tạo đại lý `Mst_DealerCP`, gate gọi sang API người nộp thuế `WA_Mst_NNT_Create` (:554-629) để tạo và sinh mã `OrgID`. Do phân hệ `Mst_NNT` chưa được port sang MiniLoyalty, endpoint này tạm dừng chờ phụ thuộc. Nghiệp vụ tạo đại lý hiện tại được thực thi qua `CreateOld` (#114). |
| **#119** | `Mst_DealerCPController/Import`<br>`POST /api/mst_dealercp/import` | **CHƯA PORT** | Tầng gate nguồn (`Mst_DealerCPController.cs:906-1172`) đọc tập tin Excel chứa 6 cột tiêu đề (gồm cả MST và Email). Trong vòng lặp từng dòng dữ liệu, gate cũng gọi `WA_Mst_NNT_Create` (:1025-1077) để tạo tổ chức nộp thuế trước khi tạo đại lý. Chưa thể triển khai độc lập khi thiếu phân hệ `Mst_NNT`. |

---

## 4. Khác biệt So với Nguồn & Danh sách GAP (Known Gaps)

| Hạng mục | Trạng thái ở Nguồn | Triển khai ở Đích | Phân loại & Ghi chú GAP |
|---|---|---|---|
| **Xác thực & Phân quyền** | Gate kiểm tra `CheckGatewayAuthentication`, `CheckAuthorize`; Biz kiểm tra `Sys_User_CheckAuthorize`. | Bỏ qua toàn bộ kiểm tra phân quyền. | Bỏ qua có chủ đích (**Loại Đ**). |
| **Ghi nhật ký hệ thống** | Ghi bản ghi `RequestLog` và `ReturnLog` vào CSDL cho mọi thao tác. | Bỏ qua ghi log DB. | Bỏ qua có chủ đích (**Loại Đ**). |
| **Tài khoản hệ thống nguồn** | Nguồn viết cứng tài khoản/mật khẩu hệ thống tại `:264-265`. | Tuyệt đối không chép thông tin đăng nhập nguồn vào mã nguồn hay tài liệu. | Khớp quy tắc bảo mật. |
| **Khai báo DDL nguồn** | Không có tập tin `.sql` tạo bảng `Mst_DealerCP` và `Map_DealerCP_Org` trong repo. | Tự suy diễn kiểu cột từ model và code biz. Khóa chính `DLCPCode` và khóa ghép `(DLCPCode, OrgID)` là khóa draft. | **GAP / Cần xác nhận**: Cần đối chiếu với DDL thực tế trên môi trường production. |
| **Ràng buộc khóa ngoại khi Xóa** | Nguồn có các bảng tham chiếu tới `DLCPCode`: `Crd_Member`, `Map_QueryDealer_Member`, `Mst_PromotionProgramSpec`, `Prm_CarNew`, `Prm_CarRecommend`. | Đích đã kiểm tra giả lập FK với 3 bảng đã port (`Crd_Member`, `Map_QueryDealer_Member`, `Mst_PromotionProgramSpec`), vi phạm trả về `ErridnLoyalty.Mst_DealerCP_Delete`. Bảng `Map_DealerCP_Org` không có FK nên nguồn và đích đều giữ lại dòng map khi xóa đại lý. | **GAP**: Cần bổ sung kiểm tra FK cho `Prm_CarNew` và `Prm_CarRecommend` khi port 2 bảng này. |

---

## 5. Nhật ký Thay đổi & Đối chiếu Tiến độ (Changelog)

| STT Hạng mục | Tên Endpoint & Action | Trạng thái | Mã Commit Git | Mô tả tóm tắt thay đổi |
|---|---|---|---|---|
| **#110** | `Mst_DealerCPController/Search` | Đã hoàn thành | `e57e314` | Khởi tạo model [`Mst_DealerCP.cs`](../../Models/Port/Mst_DealerCP.cs) và [`Map_DealerCP_Org.cs`](../../Models/Port/Mst_DealerCP.cs), cấu hình EF Core, Seeder demo và action tìm kiếm phân trang. |
| **#111** | `Mst_DealerCPController/GetAllActive` | Đã hoàn thành | `3f0c8f9` | Lấy danh sách đại lý đang hoạt động, không lọc OrgID theo nguồn, enrich liên kết tổ chức. |
| **#112** | `Mst_DealerCPController/GetByDLCPCode` | Đã hoàn thành | `2dc6a66` | Tra cứu chi tiết đại lý theo mã và danh sách tổ chức liên kết, trả về `null` khi mã rỗng. |
| **#113** | `Mst_DealerCPController/GetByOrgID` | Đã hoàn thành | `35489ef` | Lấy danh sách đại lý đang hoạt động thuộc về một tổ chức qua bảng liên kết `Map_DealerCP_Org`. |
| **#114** | `Mst_DealerCPController/CreateOld` | Đã hoàn thành | `03e9623` | Thêm mới đại lý trực tiếp, kiểm tra cha và trạng thái cha, tự động tính toán phân cấp BU toàn bảng qua `UpdBU`. |
| **#115** | `Mst_DealerCPController/Create` | Chưa làm | - | Chưa port do phụ thuộc gọi tạo tổ chức nộp thuế qua `WA_Mst_NNT_Create`. |
| **#116** | `Mst_DealerCPController/Update` | Đã hoàn thành | `578ef34` | Cập nhật thông tin đại lý, không thay đổi đại lý cha, không tính lại BU. |
| **#117** | `Mst_DealerCPController/Delete` | Đã hoàn thành | `f46f3d7` | Xóa đơn lẻ đại lý, xóa liên kết `Map_DealerCP_Org`, kiểm tra mã rỗng tại gate. |
| **#118** | `Mst_DealerCPController/DeleteMultiple` | Đã hoàn thành | `ccda090` | Xóa danh sách đại lý theo từng transaction độc lập, thực thi cơ chế xóa một phần khi có lỗi. |
| **#119** | `Mst_DealerCPController/Import` | Chưa làm | - | Chưa port do phụ thuộc gọi tạo tổ chức nộp thuế qua `WA_Mst_NNT_Create`. |
| **#120** | `Mst_DealerCPController/Export` | Đã hoàn thành | `0b1c1c8` | Xuất danh sách đại lý ra file Excel 5 cột từ điển, báo lỗi khi không có dữ liệu. |
| **#121** | `Mst_DealerCPController/ExportTemplate` | Đã hoàn thành | `1884d03` | Xuất file mẫu Excel gồm 6 cột tiêu đề (kèm cột MST và Email), không truy vấn CSDL. |
