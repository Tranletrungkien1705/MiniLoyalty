# Tham chiếu API Mst_RankPolicyController (Port 1-1)

| Thông tin | Giá trị |
|---|---|
| **Phạm vi** | Tài liệu tham chiếu chi tiết 4 endpoint đã port 1-1 của Mst_RankPolicyController (#227 - #230) |
| **Cập nhật** | 2026-10-16 |
| **Phiên bản mã** | `ed28f8f` |
| **Trạng thái** | Nháp — chờ review |

> [!NOTE]
> Toàn bộ các endpoint thuộc controller này áp dụng chuẩn hóa JSON PascalCase tự động qua `PortJsonResultFilter` và xử lý ngoại lệ tập trung qua `PortExceptionFilter` (chuyển đổi ngoại lệ chưa bắt thành HTTP 400 `PortResult.Fail`). Chi tiết xem tại [Kiến trúc Hạ tầng Dùng chung](../kien-truc/HA_TANG_CHUNG.md).

---

## 1. Tóm tắt Nghiệp vụ & Cấu trúc Dữ liệu

### 1.1. Mục đích Nghiệp vụ
API `Mst_RankPolicyController` quản lý danh mục chính sách xếp hạng và nâng/duy trì hạng thẻ thành viên (`Mst_RankPolicy`) trong MiniLoyalty. Module cung cấp các chức năng tìm kiếm phân trang cấu hình ngưỡng tích lũy điểm/số lần ghé thăm theo từng hạng thẻ, lấy danh sách chính sách đang hoạt động, tra cứu chi tiết chính sách, và lưu toàn bộ cấu hình ma trận nâng hạng thẻ trong 1 transaction.

### 1.2. Bảng Đối chiếu Nguồn Tra cứu (Source Code Mapping)

| Thành phần | Đường dẫn tập tin nguồn (`2022.7.Loyalty`) | Tập tin đích tương ứng (`MiniLoyalty`) |
|---|---|---|
| **Gate Controller** | `13.ClientGate\V10\idn.Loyalty.WebAPI\Controllers\Mst_RankPolicyController.cs:23-411` | [`Controllers/Port/Mst_RankPolicyControllerPort.cs`](../../Controllers/Port/Mst_RankPolicyControllerPort.cs) |
| **API Backend** | `11.BackEnd\V10\idn.Loyalty.Biz.Web\Controllers\MstRankPolicyController.cs:95-230` | Tích hợp trực tiếp vào [`Controllers/Port/Mst_RankPolicyControllerPort.cs`](../../Controllers/Port/Mst_RankPolicyControllerPort.cs) |
| **Nghiệp vụ (Biz)** | `11.BackEnd\V10\idn.Loyalty.Biz\Master.Loyalty.cs` (GetX `:7697-7872`; Save `:6987-7441`; WAS `:7442-7542`) | Tích hợp trực tiếp vào [`Controllers/Port/Mst_RankPolicyControllerPort.cs`](../../Controllers/Port/Mst_RankPolicyControllerPort.cs) |
| **Entity Model** | `20221026.CreateDatabase.sql:204-222` (17 cột) | [`Models/Port/Mst_RankPolicy.cs:30-49`](../../Models/Port/Mst_RankPolicy.cs#L30-L49) |
| **Cấu hình EF** | Khóa ghép `(CardType, RankPolicyCode)` tại `:226-228` | [`Models/Port/Mst_RankPolicy.cs:55-67`](../../Models/Port/Mst_RankPolicy.cs#L55-L67) |
| **Lịch sử & Sequence**| `Mst_RankPolicyHist` (`:223-248`), `Seq_RankPolicyCode` (`:266-284`) | [`Models/Port/Mst_RankPolicy.cs:223-284`](../../Models/Port/Mst_RankPolicy.cs#L223-L284) |
| **Seeder Demo** | `Order => 25` (danh mục `Mst_*` dùng 10–99) | [`Models/Port/Mst_RankPolicy.cs:76-220`](../../Models/Port/Mst_RankPolicy.cs#L76-L220) |

### 1.3. Cấu trúc Bảng CSDL (`Mst_RankPolicy`)

Bảng dữ liệu vật lý `Mst_RankPolicy` gồm đúng 17 cột theo DDL chuẩn:

| Tên cột | Kiểu EF Core | Khóa | Cho phép NULL | Ghi chú & Ý nghĩa |
|---|---|---|---|---|
| `CardType` | `string` | **PK1** | Không | Mã hạng thẻ (`Mst_CardType`). Khóa chính phần 1. |
| `RankPolicyCode` | `string` | **PK2** | Không | Mã chính sách xếp hạng. Khóa chính phần 2. |
| `NetworkID` | `string` | | Không | Mã mạng nghiệp vụ. Ở đích lấy cấu hình `Biz_NetworkID`. |
| `Value` | `double?` | | Có | Giá trị ngưỡng chính sách (MyValue). *(Chú ý: màn Search trả Value của `Mst_CardType`)*. |
| `PointUpBegin` | `double?` | | Có | Điểm tích lũy nâng hạng từ (MyValue). |
| `PointUpEnd` | `double?` | | Có | Điểm tích lũy nâng hạng đến (MyValue). |
| `QtyVisitUpBegin` | `double?` | | Có | Số lượt ghé thăm nâng hạng từ (MyValue). |
| `QtyVisitUpEnd` | `double?` | | Có | Số lượt ghé thăm nâng hạng đến (MyValue). |
| `PointKeepBegin` | `double?` | | Có | Điểm tích lũy duy trì hạng từ (MyValue). |
| `PointKeepEnd` | `double?` | | Có | Điểm tích lũy duy trì hạng đến (MyValue). |
| `QtyVisitKeepBegin` | `double?` | | Có | Số lượt ghé thăm duy trì hạng từ (MyValue). |
| `QtyVisitKeepEnd` | `double?` | | Có | Số lượt ghé thăm duy trì hạng đến (MyValue). |
| `QtyMonth` | `double?` | | Có | Thời gian duy trì hạng (số tháng, MyValue). |
| `FlagActive` | `string?` | | Có | Cờ trạng thái hoạt động: `"1"` (Kích hoạt), `"0"` (Ngừng). |
| `Remark` | `string?` | | Có | Ghi chú chính sách. |
| `LogLUDTimeUTC` | `DateTime?` | | Có | Thời điểm cập nhật cuối cùng theo chuẩn UTC. |
| `LogLUBy` | `string?` | | Có | Người thực hiện cập nhật cuối cùng. |

**Dữ liệu mẫu khởi tạo (Seeder - `Mst_RankPolicySeeder`, Idempotent)**:
- Gồm 6 bản ghi demo:
  - `RP2026`: cấu hình cho các hạng `N`, `I`, `S`, `G`, `P` (FlagActive `'1'`).
  - `RP2025`: cấu hình cho hạng `S` (FlagActive `'0'`).
- Toàn bộ bản ghi gán `NetworkID = "DEMO"`, `LogLUBy = "seed"`.

---

## 2. Đặc tả Chi tiết Từng Endpoint API

---

### 2.1. Search (#227)

Tìm kiếm danh mục chính sách xếp hạng phân trang theo trạng thái hoạt động.

- **Route**: `POST /api/mst_rankpolicy/search`
- **Controller Action**: [`Mst_RankPolicyControllerPort.Search`](../../Controllers/Port/Mst_RankPolicyControllerPort.cs#L135)
- **Tham chiếu nguồn**: Gate `Mst_RankPolicyController.cs:23-103` → API `MstRankPolicyController.cs:95-163` → Biz `Master.Loyalty.cs:7697-7872`

#### Bảng tham số Request

Payload JSON gửi qua Request Body (`MstRankPolicySearchReq`), hỗ trợ cả trường hợp body rỗng:

| Tên tham số | Kiểu dữ liệu | Bắt buộc | Mặc định | Ý nghĩa & Quy tắc |
|---|---|---|---|---|
| `KeyWord` | `string?` | Không | `null` | Nhận tại tham số hàm nhưng hàm `strWhereClause_Mst_RankPolicy` (`:281-298`) **hoàn toàn không xử lý KeyWord** → Không lọc. |
| `FlagActive` | `string?` | Không | `null` | Danh sách trạng thái hoạt động phân cách dấu phẩy, **áp dụng toán tử IN** (`gate :288-295`). |
| `Ft_PageIndex` | `int` | Không | `0` | Chỉ số trang cần lấy (0-based). |
| `Ft_PageSize` | `int` | Không | `10` | Số dòng trên mỗi trang. |

#### Quy tắc xử lý theo thứ tự (Nghiệp vụ 2 bước đặc thù của nguồn)
1. **Bước 1 - DRAFT (`#tbl_Mst_RankPolicy_Filter_Draft`)**:
   - Truy vấn các bản ghi `Mst_RankPolicy` thỏa mãn `FlagActive` (toán tử `IN`).
   - Sắp xếp theo `LogLUDTimeUTC DESC`.
   - Đếm `ItemCount` là tổng số dòng draft, cắt trang `[recordStart, recordStart + recordCount - 1]`.
2. **Bước 2 - SELECT CUỐI (Outer Join từ bảng Hạng thẻ)**:
   - Truy vấn bắt đầu từ bảng `Mst_CardType mct` có `FlagActive = '1'`.
   - Thực hiện `LEFT JOIN` sang trang draft trên `CardType`, và `LEFT JOIN` sang `Mst_RankPolicy` trên `(RankPolicyCode, CardType)`.
   - Sắp xếp kết quả cuối cùng theo `mct.Value ASC` (giá trị của bảng Hạng thẻ, không phải của RankPolicy).
   - **Hành vi đặc biệt**: `DataList` luôn chứa đầy đủ tất cả các hạng thẻ đang hoạt động trong hệ thống. Hạng thẻ nào chưa có trong trang draft thì các cột chỉ số sẽ mang giá trị `null` và `MyIdxSeq = null`.
3. Trả về đúng 12 cột chuẩn: `MyIdxSeq`, `CardType`, `Value` (của `Mst_CardType`), `PointUpBegin`, `PointUpEnd`, `QtyVisitUpBegin`, `QtyVisitUpEnd`, `PointKeepBegin`, `PointKeepEnd`, `QtyVisitKeepBegin`, `QtyVisitKeepEnd`, `QtyMonth`.

#### Bảng mã lỗi
Endpoint luôn trả về HTTP 200.

#### Ví dụ Request / Response thực tế (từ lần chạy VERIFY #227)
- **Tìm kiếm mặc định (`{}`)**:
  - Request: `POST /api/mst_rankpolicy/search` kèm `{}`
  - Response (HTTP 200):
    ```json
    {
      "Success": true,
      "Data": {
        "DataList": [
          { "MyIdxSeq": null, "CardType": "R", "Value": 0.0, "PointUpBegin": null, "PointUpEnd": null },
          { "MyIdxSeq": 0, "CardType": "N", "Value": 5.0, "PointUpBegin": 500.0, "PointUpEnd": 599.0 },
          { "MyIdxSeq": 1, "CardType": "I", "Value": 10.0, "PointUpBegin": 1000.0, "PointUpEnd": 1099.0 },
          { "MyIdxSeq": 2, "CardType": "S", "Value": 20.0, "PointUpBegin": 2000.0, "PointUpEnd": 2099.0 },
          { "MyIdxSeq": 3, "CardType": "G", "Value": 30.0, "PointUpBegin": 3000.0, "PointUpEnd": 3099.0 }
        ],
        "ItemCount": 6,
        "PageCount": 1,
        "PageIndex": 0,
        "PageSize": 10
      }
    }
    ```

---

### 2.2. GetAllActive (#228)

Lấy toàn bộ danh sách các chính sách xếp hạng đang hoạt động (`FlagActive = '1'`).

- **Route**: `POST /api/mst_rankpolicy/getallactive`
- **Controller Action**: [`Mst_RankPolicyControllerPort.GetAllActive`](../../Controllers/Port/Mst_RankPolicyControllerPort.cs#L170)
- **Tham chiếu nguồn**: Gate `Mst_RankPolicyController.cs:104-213` → API `MstRankPolicyController.cs:95-163` → Biz `Master.Loyalty.cs:7697-7872`

#### Bảng tham số Request
Endpoint không nhận tham số Body (`gate :104`).

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Gọi hàm nghiệp vụ 2 bước với `FlagActive = "1"`, `Ft_RecordStart = 0`, `Ft_RecordCount = 123456000`.
2. Trả về mảng phẳng trần các dòng ma trận xếp hạng sắp xếp theo `mct.Value ASC`.

#### Ví dụ Request / Response thực tế (từ lần chạy VERIFY #228)
- Request: `POST /api/mst_rankpolicy/getallactive`
- Response (HTTP 200):
  ```json
  {
    "Success": true,
    "Data": [
      { "MyIdxSeq": null, "CardType": "R", "Value": 0.0, "PointUpBegin": null },
      { "MyIdxSeq": 0, "CardType": "N", "Value": 5.0, "PointUpBegin": 500.0 },
      { "MyIdxSeq": 1, "CardType": "I", "Value": 10.0, "PointUpBegin": 1000.0 },
      { "MyIdxSeq": 2, "CardType": "S", "Value": 20.0, "PointUpBegin": 2000.0 },
      { "MyIdxSeq": 3, "CardType": "G", "Value": 30.0, "PointUpBegin": 3000.0 }
    ]
  }
  ```

---

### 2.3. GetByRankPolicyCode (#229)

Tra cứu chi tiết chính sách xếp hạng theo mã `RankPolicyCode`.

- **Route**: `POST /api/mst_rankpolicy/getbyrankpolicycode`
- **Controller Action**: [`Mst_RankPolicyControllerPort.GetByRankPolicyCode`](../../Controllers/Port/Mst_RankPolicyControllerPort.cs#L197)
- **Tham chiếu nguồn**: Gate `Mst_RankPolicyController.cs:214-277` → API `MstRankPolicyController.cs:95-163` → Biz `Master.Loyalty.cs:7697-7872`

#### Bảng tham số Request

Payload JSON gửi qua Request Body (`MstRankPolicyGetByRankPolicyCodeReq`):

| Tên tham số | Kiểu dữ liệu | Bắt buộc | Mặc định | Ý nghĩa & Quy tắc |
|---|---|---|---|---|
| `RankPolicyCode` | `string?` | Có | `null` | Mã chính sách xếp hạng cần tra cứu (`gate :214, :234-238`). |

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Kiểm tra tham số: Nếu `RankPolicyCode` rỗng/null → không truy vấn CSDL, trả về ngay HTTP 200 với `Data = null` (`gate :234, :264`).
2. Mệnh đề lọc: Gọi `strWhereClause_Mst_RankPolicy_By_RankPolicyCode` lọc theo `RankPolicyCode = req.RankPolicyCode`.
3. **Cảnh báo hành vi nguồn**: Vì truy vấn cuối cùng luôn thực hiện outer join từ `Mst_CardType (FlagActive = '1')` sắp xếp theo `mct.Value ASC`, nên phần tử đầu tiên `[0]` được trả về là hạng thẻ active có giá trị nhỏ nhất trong hệ thống (thường là hạng R), bảo lưu chính xác hành vi nguồn.

#### Bảng mã lỗi
Endpoint luôn trả về HTTP 200.

#### Ví dụ Request / Response thực tế (từ lần chạy VERIFY #229)
- Request: `POST /api/mst_rankpolicy/getbyrankpolicycode` kèm `{"RankPolicyCode": "RP2026"}`
- Response (HTTP 200):
  ```json
  {
    "Success": true,
    "Data": {
      "MyIdxSeq": null,
      "CardType": "R",
      "Value": 0.0,
      "PointUpBegin": null,
      "PointUpEnd": null,
      "QtyVisitUpBegin": null,
      "QtyVisitUpEnd": null
    }
  }
  ```

---

### 2.4. Save (#230)

Lưu mới hoặc cập nhật toàn bộ cấu hình chính sách xếp hạng cho các hạng thẻ trong 1 transaction.

- **Route**: `POST /api/mst_rankpolicy/save`
- **Controller Action**: [`Mst_RankPolicyControllerPort.Save`](../../Controllers/Port/Mst_RankPolicyControllerPort.cs#L248)
- **Tham chiếu nguồn**: Gate `Mst_RankPolicyController.cs:319-411` → API `MstRankPolicyController.cs:165-230` → Biz `Master.Loyalty.cs:6987-7441`

#### Bảng tham số Request

Hỗ trợ gửi qua chuỗi `strJson` hoặc danh sách đối tượng trực tiếp `Lst_Mst_RankPolicy`:

| Tên tham số | Kiểu dữ liệu | Bắt buộc | Mặc định | Ý nghĩa & Quy tắc |
|---|---|---|---|---|
| `[].CardType` | `string` | Có | | Mã hạng thẻ áp dụng (`Mst_CardType`). |
| `[].PointUpBegin` | `double?` | Có | | Điểm tích lũy nâng hạng từ. |
| `[].PointUpEnd` | `double?` | Không | | Điểm tích lũy nâng hạng đến. |
| `[].PointKeepBegin` | `double?` | Có | | Điểm tích lũy duy trì hạng từ. |
| `[].PointKeepEnd` | `double?` | Không | | Điểm tích lũy duy trì hạng đến. |
| `[].QtyMonth` | `double?` | Không | `12` | Số tháng duy trì hiệu lực hạng thẻ. |

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Kiểm tra payload: Danh sách cấu hình không được rỗng.
2. Kiểm tra tính hợp lệ từng dòng:
   - `CardType` phải tồn tại trong `Mst_CardType` (`ErridnLoyalty.Mst_CardType_CheckDB_CardTypeNotFound`).
   - Hạng thẻ phải đang hoạt động (`FlagActive = '1'`, `ErridnLoyalty.Mst_CardType_CheckDB_FlagActiveNotMatched`).
   - Ràng buộc điểm: `PointUpBegin` không được nhỏ hơn `PointKeepBegin` (`ErridnLoyalty.Mst_RankPolicy_Save_InvalidValue`).
   - Không được trùng lặp `CardType` trong cùng một request (`ErridnLoyalty.Mst_RankPolicy_Save`).
3. Thực thi transaction:
   - Hệ thống tự sinh mã chính sách mới `RANKPOLICYCODE...` qua bảng sequence `Seq_RankPolicyCode`.
   - **Hành vi nguồn**: Xóa sạch toàn bộ dữ liệu cấu hình cũ trong bảng `Mst_RankPolicy` và lưu lại bộ cấu hình mới vừa gửi.
   - Commit transaction và trả về HTTP 200 `{ "Success": true, "Data": null }`.

#### Bảng mã lỗi (Copy nguyên văn từ mã nguồn)

| Mã lỗi (`ErrorCode`) | Tham số `Params` | Điều kiện kích hoạt |
|---|---|---|
| `ErridnLoyalty.Mst_RankPolicy_Save` | `{"Check.strJson": null}` | Request body rỗng hoàn toàn. |
| `ErridnLoyalty.Mst_RankPolicy_Save` | `{"Check.Lst_Mst_RankPolicy": null}` | Danh sách hạng thẻ rỗng. |
| `ErridnLoyalty.Mst_CardType_CheckDB_CardTypeNotFound` | `{"Check.CardType": strCardType}` | Mã hạng thẻ không tồn tại trong CSDL. |
| `ErridnLoyalty.Mst_CardType_CheckDB_FlagActiveNotMatched` | `{"Check.CardType": strCardType, "Check.strFlagActiveListToCheck": "1", "DB.FlagActive": dbFlag}` | Hạng thẻ có cờ trạng thái khác `'1'`. |
| `ErridnLoyalty.Mst_RankPolicy_Save` | `{"Check.DB.PointUpBegin": val, "Check.DB.PointKeepBegin": val2}` | Giá trị điểm nâng hạng hoặc duy trì bị null. |
| `ErridnLoyalty.Mst_RankPolicy_Save_InvalidValue` | `{"Check.DB.RankPolicyCode": code, "Check.DB.CardType": type, "Check.ErrCoditional": "PointUpBegin < PointKeepBegin"}` | Điểm nâng hạng nhỏ hơn điểm duy trì hạng. |
| `ErridnLoyalty.Mst_RankPolicy_Save` | `{"Check.NextRowValues": null}` | Thứ tự các dòng cấu hình không liên tục. |
| `ErridnLoyalty.Mst_RankPolicy_Save` | `{"Check.CardTypeDuplicate": type}` | Xuất hiện 2 dòng cùng cấu hình cho một `CardType`. |
| `ErridnLoyalty.Mst_RankPolicy_Save` | `{"Exception": ex.Message}` | Lỗi ngoại lệ trong quá trình thực thi transaction. |

---

## 3. Khác biệt so với Nguồn & GAP

1. **Truy vấn 2 bước đặc thù**: Màn hình Search và GetAllActive thực hiện outer join từ bảng `Mst_CardType`, do đó trả về đầy đủ mọi hạng thẻ và lấy trường `Value` của hạng thẻ thay vì của bảng RankPolicy.
2. **Hành vi GetByRankPolicyCode trả phần tử đầu**: Do sắp xếp theo `mct.Value ASC` của bảng ngoài, `[0]` luôn là hạng thẻ có giá trị nhỏ nhất thay vì chính sách cần tìm nếu có nhiều dòng.
3. **Save xóa trắng bảng**: Khi lưu cấu hình mới, nguồn xóa toàn bộ các bản ghi xếp hạng hiện có trong bảng `Mst_RankPolicy`. Hành vi này được giữ nguyên ở đích.
4. **Phân quyền & Ghi log**: Bỏ qua các kiểm tra phân quyền và ghi log hệ thống.

---

## 4. Nhật ký Thay đổi

| Hạng mục (#) | Action / Endpoint | Commit Hash | Ngày hoàn thành | Ghi chú |
|---|---|---|---|---|
| #227 | `Search` | `b98d2d5` | 2026-10-16 | Khởi tạo bảng `Mst_RankPolicy`, seeder demo và truy vấn 2 bước outer join |
| #228 | `GetAllActive` | `a20a029` | 2026-10-16 | Endpoint lấy danh sách chính sách xếp hạng đang hoạt động |
| #229 | `GetByRankPolicyCode` | `cb67c47` | 2026-10-16 | Endpoint tra cứu chính sách theo mã |
| #230 | `Save` | `ddbf66a` | 2026-10-16 | Endpoint lưu/cập nhật toàn bộ chính sách xếp hạng trong transaction |
