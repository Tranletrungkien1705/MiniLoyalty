# Tham chiếu API Mst_PolicyMoneyToPointServiceController (Port 1-1)

| Thông tin | Giá trị |
|---|---|
| **Phạm vi** | Tài liệu tham chiếu chi tiết 3 endpoint đã port 1-1 của Mst_PolicyMoneyToPointServiceController (#194 - #196) |
| **Cập nhật** | 2026-10-16 |
| **Phiên bản mã** | `ed28f8f` |
| **Trạng thái** | Nháp — chờ review |

> [!NOTE]
> Toàn bộ các endpoint thuộc controller này áp dụng chuẩn hóa JSON PascalCase tự động qua `PortJsonResultFilter` và xử lý ngoại lệ tập trung qua `PortExceptionFilter` (chuyển đổi ngoại lệ chưa bắt thành HTTP 400 `PortResult.Fail`). Chi tiết xem tại [Kiến trúc Hạ tầng Dùng chung](../kien-truc/HA_TANG_CHUNG.md).

---

## 1. Tóm tắt Nghiệp vụ & Cấu trúc Dữ liệu

### 1.1. Mục đích Nghiệp vụ
API `Mst_PolicyMoneyToPointServiceController` quản lý danh mục chính sách quy đổi tiền chi tiêu sang điểm tích lũy dịch vụ (`Mst_PolicyMoneyToPointService`) và chi tiết tỉ lệ quy đổi cùng mức chiết khấu theo từng hạng thẻ (`Mst_PolicyMoneyToPointServiceDtl`). Module cung cấp các chức năng lấy toàn bộ danh sách chính sách đang có, tra cứu chi tiết chính sách kèm danh sách hạng thẻ theo mã chính sách, và lưu mới hoặc cập nhật chính sách theo cơ chế transaction bảo vệ tính toàn vẹn dữ liệu.

### 1.2. Bảng Đối chiếu Nguồn Tra cứu (Source Code Mapping)

| Thành phần | Đường dẫn tập tin nguồn (`2022.7.Loyalty`) | Tập tin đích tương ứng (`MiniLoyalty`) |
|---|---|---|
| **Gate Controller** | `13.ClientGate\V10\idn.Loyalty.WebAPI\Controllers\Mst_PolicyMoneyToPointServiceController.cs:24-352` | [`Controllers/Port/Mst_PolicyMoneyToPointServiceControllerPort.cs`](../../Controllers/Port/Mst_PolicyMoneyToPointServiceControllerPort.cs) |
| **API Backend** | `11.BackEnd\V10\idn.Loyalty.Biz.Web\Controllers\MstPolicyMoneyToPointServiceController.cs:94-230` | Tích hợp trực tiếp vào [`Controllers/Port/Mst_PolicyMoneyToPointServiceControllerPort.cs`](../../Controllers/Port/Mst_PolicyMoneyToPointServiceControllerPort.cs) |
| **Nghiệp vụ (Biz)** | `11.BackEnd\V10\idn.Loyalty.Biz\Master.Loyalty.cs` (GetX `:18618-18820`; Save `:17399-18346`) | Tích hợp trực tiếp vào [`Controllers/Port/Mst_PolicyMoneyToPointServiceControllerPort.cs`](../../Controllers/Port/Mst_PolicyMoneyToPointServiceControllerPort.cs) |
| **Entity Header** | `20221026.CreateDatabase.sql:634-651` (8 cột) | [`Models/Port/Mst_PolicyMoneyToPointService.cs:31-41`](../../Models/Port/Mst_PolicyMoneyToPointService.cs#L31-L41) |
| **Entity Detail** | `20221026.CreateDatabase.sql:610-629` (10 cột) | [`Models/Port/Mst_PolicyMoneyToPointService.cs:68-81`](../../Models/Port/Mst_PolicyMoneyToPointService.cs#L68-L81) |
| **Sequence Entity** | `SeqType.PolicyCode` | [`Models/Port/Mst_PolicyMoneyToPointService.cs:235-249`](../../Models/Port/Mst_PolicyMoneyToPointService.cs#L235-L249) |
| **Seeder Demo** | `Order => 26` (danh mục `Mst_*` dùng 10–99) | [`Models/Port/Mst_PolicyMoneyToPointService.cs:116-233`](../../Models/Port/Mst_PolicyMoneyToPointService.cs#L116-L233) |

### 1.3. Cấu trúc Bảng CSDL (`Mst_PolicyMoneyToPointService` & `Dtl`)

Hệ thống quản lý quy đổi tiền sang điểm gồm 2 bảng quan hệ Master - Detail với DDL nguồn chuẩn xác:

#### Bảng Header `Mst_PolicyMoneyToPointService` (Đúng 8 cột theo DDL `:634-651`):

| Tên cột | Kiểu EF Core | Khóa | Cho phép NULL | Ghi chú & Ý nghĩa |
|---|---|---|---|---|
| `PolicyCode` | `string` | **PK** | Không | Mã chính sách quy đổi. Khóa chính (DDL `:647-650`). |
| `NetworkID` | `string` | | Không | Mã mạng nghiệp vụ. Ở đích lấy cấu hình `Biz_NetworkID`. |
| `EffDateStart` | `DateTime` | | Không | Ngày bắt đầu hiệu lực (MyDateTime NOT NULL). |
| `EffDateEnd` | `DateTime` | | Không | Ngày kết thúc hiệu lực (MyDateTime NOT NULL). |
| `FlagActive` | `string?` | | Có | Cờ trạng thái hoạt động: `"1"` (Kích hoạt), `"0"` (Ngừng). |
| `Remark` | `string?` | | Có | Ghi chú chính sách. |
| `LogLUBy` | `string?` | | Có | Người thực hiện cập nhật cuối cùng. |
| `LogLUDTimeUTC` | `DateTime?` | | Có | Thời điểm cập nhật cuối cùng theo chuẩn UTC. |

#### Bảng Chi tiết `Mst_PolicyMoneyToPointServiceDtl` (Đúng 10 cột theo DDL `:610-629`):

| Tên cột | Kiểu EF Core | Khóa | Cho phép NULL | Ghi chú & Ý nghĩa |
|---|---|---|---|---|
| `PolicyCode` | `string` | **PK1** | Không | Mã chính sách quy đổi. Khóa chính phần 1. |
| `CardType` | `string` | **PK2** | Không | Mã hạng thẻ áp dụng (`Mst_CardType`). Khóa chính phần 2. |
| `NetworkID` | `string` | | Không | Mã mạng nghiệp vụ. |
| `ConvertValue` | `double?` | | Có | Giá trị tiền quy đổi (MyValue). |
| `ConvertPoint` | `double?` | | Có | Điểm tích lũy quy đổi tương ứng (MyValue). |
| `ValueRankCardType` | `double?` | | Có | Mốc tính lượt hạng thẻ (MyValue). |
| `DiscountRate` | `double?` | | Có | Tỉ lệ % chiết khấu dịch vụ (decimal/float). |
| `FlagActive` | `string?` | | Có | Cờ trạng thái hoạt động: `"1"`/`"0"`. |
| `Remark` | `string?` | | Có | Ghi chú chi tiết theo hạng thẻ. |
| `LogLUBy` | `string?` | | Có | Người cập nhật cuối cùng. |
| `LogLUDTimeUTC` | `DateTime?` | | Có | Thời điểm cập nhật UTC. |

**Dữ liệu mẫu khởi tạo (Seeder - `Mst_PolicyMoneyToPointServiceSeeder`, Idempotent)**:
- Gồm 3 chính sách demo:
  - `P01` (Hiệu lực 2026-01-01 đến 2026-12-31, FlagActive `'1'`, gồm chi tiết cho hạng N, S, G).
  - `P02` (Hiệu lực 2025-01-01 đến 2025-12-31, FlagActive `'0'`, không có detail).
  - `P03` (Hiệu lực 2026-06-01 đến 2100-01-01, FlagActive `'1'`, gồm chi tiết cho hạng P).
- Toàn bộ bản ghi gán `NetworkID = "6403834000"`, `LogLUBy = "seed"`.

---

## 2. Đặc tả Chi tiết Từng Endpoint API

---

### 2.1. GetAllActive (#194)

Lấy toàn bộ danh sách các chính sách quy đổi tiền sang điểm.

- **Route**: `POST /api/mst_policymoneytopointservice/getallactive`
- **Controller Action**: [`Mst_PolicyMoneyToPointServiceControllerPort.GetAllActive`](../../Controllers/Port/Mst_PolicyMoneyToPointServiceControllerPort.cs#L115)
- **Tham chiếu nguồn**: Gate `Mst_PolicyMoneyToPointServiceController.cs:24-133` → API `MstPolicyMoneyToPointServiceController.cs:94-164` → Biz `Master.Loyalty.cs:18618-18820`

#### Bảng tham số Request
Endpoint không nhận tham số Body (`gate :24`).

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Thiết lập tham số phân trang: `Ft_RecordStart = 0`, `Ft_RecordCount = 123456000` (`gate :66-67`).
2. **Đặc điểm lọc cờ**: Dòng gán `//FlagActive = "1"` bị comment tại Gate nguồn (`gate :53`), và hàm `strWhereClause_Mst_PolicyMoneyToPointService` không sử dụng `OrgID` → **Không có điều kiện lọc nào được áp dụng** (trả về toàn bộ chính sách kể cả chính sách đã hết hạn hoặc ngừng hoạt động `FlagActive = '0'`).
3. Sắp xếp thứ tự: Sắp xếp theo `EffDateStart DESC` kết hợp `PolicyCode ASC`.
4. Trả về mảng phẳng trần các thực thể header kèm `MyIdxSeq`. Nếu rỗng trả về `Data = null` (`gate :113-120`).

#### Bảng mã lỗi
Endpoint luôn trả về HTTP 200.

#### Ví dụ Request / Response thực tế (từ lần chạy VERIFY #194)
- Request: `POST /api/mst_policymoneytopointservice/getallactive`
- Response (HTTP 200):
  ```json
  {
    "Success": true,
    "Data": [
      { "MyIdxSeq": 0, "PolicyCode": "P03", "EffDateStart": "2026-06-01T00:00:00", "EffDateEnd": "2100-01-01T00:00:00", "FlagActive": "1" },
      { "MyIdxSeq": 1, "PolicyCode": "P01", "EffDateStart": "2026-01-01T00:00:00", "EffDateEnd": "2026-12-31T00:00:00", "FlagActive": "1" },
      { "MyIdxSeq": 2, "PolicyCode": "P02", "EffDateStart": "2025-01-01T00:00:00", "EffDateEnd": "2025-12-31T00:00:00", "FlagActive": "0" }
    ]
  }
  ```

---

### 2.2. GetByPolicyCode (#195)

Tra cứu chi tiết chính sách và toàn bộ danh sách cấu hình theo từng hạng thẻ theo mã chính sách.

- **Route**: `POST /api/mst_policymoneytopointservice/getbypolicycode`
- **Controller Action**: [`Mst_PolicyMoneyToPointServiceControllerPort.GetByPolicyCode`](../../Controllers/Port/Mst_PolicyMoneyToPointServiceControllerPort.cs#L137)
- **Tham chiếu nguồn**: Gate `Mst_PolicyMoneyToPointServiceController.cs:134-210` → API `MstPolicyMoneyToPointServiceController.cs:94-164` → Biz `Master.Loyalty.cs:18618-18820`

#### Bảng tham số Request

Payload JSON gửi qua Request Body (`MstPolicyMoneyToPointServiceGetByPolicyCodeReq`):

| Tên tham số | Kiểu dữ liệu | Bắt buộc | Mặc định | Ý nghĩa & Quy tắc |
|---|---|---|---|---|
| `PolicyCode` | `string?` | Có | `null` | Mã chính sách quy đổi cần tra cứu (`gate :134`). |

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Kiểm tra tham số: Nếu `PolicyCode` rỗng/null → không truy vấn CSDL, trả về ngay HTTP 200 với `{ "Mst_PolicyMoneyToPointService": null, "Lst_Mst_PolicyMoneyToPointService": null }` (`gate :153-154, :193-197`).
2. Mệnh đề lọc: Lọc chính xác theo `PolicyCode = req.PolicyCode`. Không lọc `FlagActive` (chính sách `FlagActive = '0'` như `P02` vẫn tra cứu được).
3. Truy vấn Header và Detail:
   - Header lấy phần tử đầu tiên thỏa mãn mã chính sách.
   - Detail lấy toàn bộ các dòng chi tiết thuộc mã chính sách, sắp xếp theo `CardType ASC`.
4. **Bảo lưu tên trường phản hồi nguồn**: Danh sách chi tiết được gán vào trường `Lst_Mst_PolicyMoneyToPointService` (không có chữ `Dtl` do nguồn đặt tên biến như vậy).

#### Bảng mã lỗi
Endpoint luôn trả về HTTP 200.

#### Ví dụ Request / Response thực tế (từ lần chạy VERIFY #195)
- Request: `POST /api/mst_policymoneytopointservice/getbypolicycode` kèm `{"PolicyCode": "P01"}`
- Response (HTTP 200):
  ```json
  {
    "Success": true,
    "Data": {
      "Mst_PolicyMoneyToPointService": {
        "PolicyCode": "P01",
        "NetworkID": "6403834000",
        "EffDateStart": "2026-01-01T00:00:00",
        "EffDateEnd": "2026-12-31T00:00:00",
        "FlagActive": "1",
        "Remark": "Chính sách quy đổi 2026"
      },
      "Lst_Mst_PolicyMoneyToPointService": [
        { "PolicyCode": "P01", "CardType": "G", "ConvertValue": 10000.0, "ConvertPoint": 1.0, "DiscountRate": 10.0 },
        { "PolicyCode": "P01", "CardType": "N", "ConvertValue": 10000.0, "ConvertPoint": 1.0, "DiscountRate": 0.0 },
        { "PolicyCode": "P01", "CardType": "S", "ConvertValue": 10000.0, "ConvertPoint": 1.0, "DiscountRate": 5.0 }
      ]
    }
  }
  ```

---

### 2.3. Save (#196)

Thêm mới hoặc cập nhật toàn bộ thông tin chính sách quy đổi kèm danh sách chi tiết từng hạng thẻ trong 1 transaction.

- **Route**: `POST /api/mst_policymoneytopointservice/save`
- **Controller Action**: [`Mst_PolicyMoneyToPointServiceControllerPort.Save`](../../Controllers/Port/Mst_PolicyMoneyToPointServiceControllerPort.cs#L215)
- **Tham chiếu nguồn**: Gate `Mst_PolicyMoneyToPointServiceController.cs:259-352` → API `MstPolicyMoneyToPointServiceController.cs:166-230` → Biz `Master.Loyalty.cs:17399-18346`

#### Bảng tham số Request

Payload JSON gửi qua Request Body (`MstPolicyMoneyToPointServiceSaveReq`):

| Tên tham số | Kiểu dữ liệu | Bắt buộc | Mặc định | Ý nghĩa & Quy tắc |
|---|---|---|---|---|
| `Mst_PolicyMoneyToPointService` | `HeaderReq` | Có | `null` | Đối tượng thông tin chung của chính sách. |
| `..PolicyCode` | `string?` | Không | `null` | Mã chính sách. Nếu để trống hệ thống sẽ tự sinh sequence `POLICYCODE...`. |
| `..EffDateStart` | `string?` | Có | | Ngày bắt đầu hiệu lực (định dạng `yyyy-MM-dd`). Bắt buộc. |
| `..EffDateEnd` | `string?` | Không | `2100-01-01` | Ngày kết thúc hiệu lực. Mặc định `2100-01-01` nếu để trống. |
| `..Remark` | `string?` | Không | `null` | Ghi chú chung. |
| `Lst_Mst_PolicyMoneyToPointServiceDtl` | `List<DtlReq>` | Không | `[]` | Danh sách cấu hình chi tiết cho từng hạng thẻ. |
| `..[].CardType` | `string` | Có | | Mã hạng thẻ áp dụng (`Mst_CardType`). |
| `..[].ConvertValue` | `double?` | Có | | Số tiền quy đổi (bắt buộc `> 0`). |
| `..[].ConvertPoint` | `double?` | Có | | Số điểm nhận được tương ứng (bắt buộc `> 0`). |
| `..[].DiscountRate` | `double?` | Không | `0` | % chiết khấu dịch vụ (phải nằm trong khoảng `0` đến `100`). |

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Kiểm tra payload: Header không được rỗng; ngày `EffDateStart` phải hợp lệ.
2. Kiểm tra danh sách Detail:
   - Hạng thẻ `CardType` phải tồn tại trong bảng `Mst_CardType`. Nếu không có → lỗi `Mst_CardType_CheckDB_CardTypeNotFound`.
   - Hạng thẻ phải đang hoạt động (`FlagActive = '1'`). Nếu không → lỗi `Mst_CardType_CheckDB_FlagActiveNotMatched`.
   - `ConvertValue` và `ConvertPoint` phải lớn hơn 0. Nếu không → lỗi `Mst_PolicyMoneyToPointService_Save_InvalidValue`.
   - `DiscountRate` phải nằm trong đoạn `[0, 100]`. Nếu không → lỗi `Mst_PolicyMoneyToPointService_Save`.
   - Danh sách không được chứa trùng lặp `CardType`. Nếu trùng → lỗi `Mst_PolicyMoneyToPointService_Save`.
3. Kiểm tra trùng khoảng thời gian hiệu lực: Ngày `EffDateStart` không được trùng với chính sách khác đang có.
4. Ghi dữ liệu vào CSDL trong 1 transaction:
   - Xóa toàn bộ dữ liệu detail và header cũ theo `PolicyCode`.
   - Thêm mới header kèm sequence tự sinh nếu chưa có mã.
   - Thêm mới toàn bộ danh sách detail.
   - Commit transaction và trả về HTTP 200 `{ "Success": true, "Data": null }`.

#### Bảng mã lỗi (Copy nguyên văn từ mã nguồn)

| Mã lỗi (`ErrorCode`) | Tham số `Params` | Điều kiện kích hoạt |
|---|---|---|
| `ErridnLoyalty.Mst_PolicyMoneyToPointService_Save` | `{"Check.strJson": null}` | Request body rỗng hoàn toàn. |
| `ErridnLoyalty.Mst_PolicyMoneyToPointService_Save` | `{"Check.Mst_PolicyMoneyToPointService": null}` | Thiếu đối tượng Header chính sách trong payload. |
| `ErridnLoyalty.Mst_PolicyMoneyToPointService_Save` | `{"Check.EffDateStart": rawVal}` | Ngày bắt đầu hiệu lực `EffDateStart` không đúng định dạng hoặc bị trùng lặp. |
| `ErridnLoyalty.Mst_CardType_CheckDB_CardTypeNotFound` | `{"Check.CardType": strCardType}` | Hạng thẻ trong danh sách detail không tồn tại trong `Mst_CardType`. |
| `ErridnLoyalty.Mst_CardType_CheckDB_FlagActiveNotMatched` | `{"Check.CardType": strCardType, "Check.strFlagActiveListToCheck": "1", "DB.FlagActive": dbFlag}` | Hạng thẻ có cờ trạng thái khác `'1'`. |
| `ErridnLoyalty.Mst_PolicyMoneyToPointService_Save` | `{"Check.DiscountRate": val}` | Chiết khấu `DiscountRate` âm hoặc vượt quá 100%. |
| `ErridnLoyalty.Mst_PolicyMoneyToPointService_Save_InvalidValue` | `{"Check.CardType": cardType, "Check.ConvertValue": val, "Check.ConvertPoint": pt}` | `ConvertValue` hoặc `ConvertPoint` nhỏ hơn hoặc bằng 0. |
| `ErridnLoyalty.Mst_PolicyMoneyToPointService_Save` | `{"Check.DuplicateCardType": null}` | Danh sách detail chứa 2 dòng cùng một `CardType`. |
| `ErridnLoyalty.Mst_PolicyMoneyToPointService_Save` | `{"Exception": ex.Message}` | Lỗi ngoại lệ trong quá trình thực thi transaction. |

---

## 3. Khác biệt so với Nguồn & GAP

1. **Khóa ngoại không ràng buộc cứng**: Khóa ngoại `FK CardType -> Mst_CardType` và `FK PolicyCode -> Mst_PolicyMoneyToPointService` có trong DDL nguồn (`20221026.CreateDatabase.sql:656-670`) nhưng chưa cấu hình navigation properties trong EF Core (chỉ kiểm tra giả lập qua code).
2. **Tên trường trả về Lst_Mst_PolicyMoneyToPointService**: Nguồn đặt tên trường JSON chứa danh sách chi tiết mà không có hậu tố `Dtl`. Hệ thống port bảo lưu 1-1 để tương thích giao tiếp.
3. **Phân quyền & Kiểm soát truy cập**: Bỏ qua các kiểm tra phân quyền `CheckGatewayAuthentication`, `CheckAuthorize`, `Sys_User_CheckAuthorize` và ghi log `RequestLog`/`ReturnLog`.

---

## 4. Nhật ký Thay đổi

| Hạng mục (#) | Action / Endpoint | Commit Hash | Ngày hoàn thành | Ghi chú |
|---|---|---|---|---|
| #194 | `GetAllActive` | `b7a48c3` | 2026-10-16 | Khởi tạo bảng `Mst_PolicyMoneyToPointService` và endpoint lấy danh sách chính sách |
| #195 | `GetByPolicyCode` | `f0df397` | 2026-10-16 | Endpoint tra cứu chi tiết chính sách kèm danh sách hạng thẻ |
| #196 | `Save` | `0364844` | 2026-10-16 | Endpoint lưu/cập nhật chính sách và chi tiết trong transaction |
