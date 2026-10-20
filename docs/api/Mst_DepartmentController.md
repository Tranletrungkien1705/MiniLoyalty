# Tham chiếu API Mst_DepartmentController (Port 1-1)

| Thông tin | Giá trị |
|---|---|
| **Phạm vi** | Tài liệu tham chiếu chi tiết 10 endpoint đã port 1-1 của Mst_DepartmentController (#122 - #131) |
| **Cập nhật** | 2026-10-16 |
| **Phiên bản mã** | `79657b7` |
| **Trạng thái** | Nháp — chờ review |

---

## 1. Tóm tắt Nghiệp vụ & Cấu trúc Dữ liệu

### 1.1. Mục đích Nghiệp vụ
API `Mst_DepartmentController` quản lý danh mục phòng ban (`Mst_Department`) trong hệ thống MiniLoyalty. Module này cung cấp các chức năng tìm kiếm phân trang theo từ khóa và tổ chức, lấy danh sách phòng ban đang hoạt động, tra cứu chi tiết một phòng ban theo mã, tạo mới phòng ban kèm sinh sequence tự động và tính toán phân cấp BU toàn bảng, cập nhật thông tin phòng ban, xóa đơn lẻ/xóa hàng loạt, nhập dữ liệu danh mục từ bảng tính Excel, xuất danh sách phòng ban ra tập tin Excel chuẩn và xuất tập tin Excel mẫu (ExportTemplate) phục vụ chức năng nhập dữ liệu.

### 1.2. Bảng Đối chiếu Nguồn Tra cứu (Source Code Mapping)

| Thành phần | Đường dẫn tập tin nguồn (`2022.7.Loyalty`) | Tập tin đích tương ứng (`MiniLoyalty`) |
|---|---|---|
| **Gate Controller** | `13.ClientGate\V10\idn.Loyalty.WebAPI\Controllers\Mst_DepartmentController.cs:30-873` | [`Controllers/Port/Mst_DepartmentControllerPort.cs`](../../Controllers/Port/Mst_DepartmentControllerPort.cs) |
| **API Backend** | `11.BackEnd\V10\idn.Loyalty.Biz.Web\Controllers\MstDepartmentController.cs:20-302` | Tích hợp trực tiếp vào [`Controllers/Port/Mst_DepartmentControllerPort.cs`](../../Controllers/Port/Mst_DepartmentControllerPort.cs) |
| **Nghiệp vụ (Biz)** | `11.BackEnd\V10\idn.Loyalty.Biz\Master.cs` (Search `:29450-29701`, `:30396-30568`; Create `:29702-29935`, `:30569-30726`; Update `:29936-30180`, `:30727-30864`; Delete `:30181-30395`, `:30865-30968`; Export `:29450-29701`; UpdBU `:29413-29448`; CheckDB `:29275-29411`, `:7813-7890`) | Tích hợp trực tiếp vào [`Controllers/Port/Mst_DepartmentControllerPort.cs`](../../Controllers/Port/Mst_DepartmentControllerPort.cs) |
| **Entity Model** | `12.Dev.Common\idn.Loyalty.Common\Models\Mst_Department.cs:8-35` | [`Models/Port/Mst_Department.cs:35-50`](../../Models/Port/Mst_Department.cs#L35-L50) |
| **Sequence Entity** | `Seq.cs:Bang Seq_DepartmentCode, SeqType.DepartmentCode` | [`Models/Port/Mst_Department.cs:144-148`](../../Models/Port/Mst_Department.cs#L144-L148) |

### 1.3. Cấu trúc Bảng CSDL (`Mst_Department` & `Seq_DepartmentCode`)

Bảng dữ liệu vật lý `Mst_Department` gồm đúng 13 cột (nguồn không có script DDL tạo bảng `Mst_Department`, cấu trúc cột và kiểu dữ liệu được xác nhận qua model và code nghiệp vụ; khóa chính `DepartmentCode` là khóa draft):

| Tên cột | Kiểu EF Core | Khóa | Cho phép NULL | Ghi chú & Ý nghĩa |
|---|---|---|---|---|
| `DepartmentCode` | `string` | **PK** | Không | Mã phòng ban. Khóa chính (draft - không có DDL nguồn, kiểu cột cần xác nhận). |
| `NetworkID` | `string?` | | Có | Mã mạng nghiệp vụ. Ở đích lấy từ cấu hình `Biz_NetworkID` (mặc định `"1001"`). |
| `DepartmentCodeParent` | `string?` | | Có | Mã phòng ban cấp trên. |
| `DepartmentBUCode` | `string?` | | Có | Mã đơn vị nghiệp vụ phân cấp (BU Code, ví dụ: `HQ.KD`). Tự động sinh bởi `UpdBU`. |
| `DepartmentBUPattern` | `string?` | | Có | Mẫu tìm kiếm tiền tố BU (Pattern, ví dụ: `HQ.KD%`). Tự động sinh bởi `UpdBU`. |
| `DepartmentLevel` | `int?` | | Có | Cấp bậc phòng ban trong cây phân cấp (1, 2, 3...). Tự động tính bởi `UpdBU`. |
| `MST` | `string?` | | Có | Mã số thuế của doanh nghiệp sở hữu phòng ban. |
| `OrgID` | `string?` | | Có | Mã tổ chức quản lý phòng ban. |
| `DepartmentName` | `string?` | | Có | Tên phòng ban. |
| `DepartmentDesc` | `string?` | | Có | Mô tả chức năng, nhiệm vụ phòng ban. |
| `FlagActive` | `string?` | | Có | Cờ trạng thái hoạt động: `"1"` (Đang hoạt động), `"0"` (Ngừng hoạt động). |
| `LogLUDTimeUTC` | `DateTime?` | | Có | Thời điểm cập nhật cuối cùng theo chuẩn UTC. |
| `LogLUBy` | `string?` | | Có | Tài khoản thực hiện cập nhật cuối cùng (mặc định gán `"port"` ở đích). |

**Các cột tính toán (Computed Properties - không lưu trong bảng `Mst_Department`)**:
- `md_DepartmentNameParent`: Tên phòng ban cấp trên, xác định thông qua `LEFT JOIN` chính bảng `Mst_Department` theo điều kiện `t.DepartmentCodeParent = f.DepartmentCode` (nguồn `Master.cs:30512-30513`).
- `ss_QtyStaff`: Số lượng nhân viên trực thuộc phòng ban. Nguồn `Master.cs:30490-30499` thực hiện `LEFT JOIN` và đếm từ bảng `Staff_MapDepartment`. Ở đích, do bảng `Staff_MapDepartment` chưa được port nên tạm thời gán cố định `0` cho mọi bản ghi (ghi nhận GAP).

**Bảng Sequence `Seq_DepartmentCode`**:
- `AutoId`: `long`, Khóa chính tự tăng (`IDENTITY(1,1)`), dùng để sinh số thứ tự sequence cho mã phòng ban.
- `Remark`: `string?`, Nullable, Ghi chú dòng sequence.

---

## 2. Đặc tả Chi tiết Từng Endpoint API

---

### 2.1. Search (#122)

Tìm kiếm danh mục phòng ban phân trang theo từ khóa, tổ chức và trạng thái hoạt động.

- **Route**: `POST /api/mst_department/search`
- **Controller Action**: [`Mst_DepartmentControllerPort.Search`](../../Controllers/Port/Mst_DepartmentControllerPort.cs#L227-L244)
- **Tham chiếu nguồn**: Gate `Mst_DepartmentController.cs:30-109` → API `MstDepartmentController.cs:20-89` → Biz `Master.cs:30396-30568`

#### Bảng tham số Request

Payload JSON gửi qua Request Body (`MstDepartmentSearchReq`), hỗ trợ cả trường hợp body rỗng hoàn toàn:

| Tên tham số | Kiểu dữ liệu | Bắt buộc | Mặc định | Ý nghĩa & Quy tắc |
|---|---|---|---|---|
| `OrgID` | `string?` | Không | `null` | Lọc chính xác theo mã tổ chức (`OrgID = 'x'`, không phân biệt hoa thường). |
| `KeyWord` | `string?` | Không | `null` | Từ khóa tìm kiếm theo tên phòng ban (`DepartmentName LIKE '%kw%'`). |
| `FlagActive` | `string?` | Không | `null` | Danh sách trạng thái phân cách bởi dấu phẩy (ví dụ: `"1"` hoặc `"0,1"`). Chuyển thành toán tử `IN ('0', '1')`. |
| `Ft_PageIndex` | `int` | Không | `0` | Chỉ số trang cần lấy (0-based). Nếu `< 0` tự động gán về `0`. |
| `Ft_PageSize` | `int` | Không | `10` | Số dòng trên mỗi trang. Nếu `<= 0` trả về mảng rỗng `DataList = []`. |

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Chuẩn hóa chỉ số phân trang: `pageIndex = Math.Max(0, r.Ft_PageIndex)`, `pageSize = r.Ft_PageSize`.
2. Tính vị trí bắt đầu bản ghi: `start = (long)pageIndex * pageSize` ([`Mst_DepartmentControllerPort.cs:235`](../../Controllers/Port/Mst_DepartmentControllerPort.cs#L235)).
3. Xây dựng câu truy vấn lọc dữ liệu qua hàm `GetXAsync` ([`Mst_DepartmentControllerPort.cs:1078-1116`](../../Controllers/Port/Mst_DepartmentControllerPort.cs#L1078-L1116)):
   - **KeyWord**: Nếu có giá trị, lọc `EF.Functions.Like((x.DepartmentName ?? "").ToLower(), PortQuery.Contains(keyWord), PortQuery.Esc)` (nguồn gate `:884`).
   - **OrgID**: Nếu có giá trị, lọc `EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(orgIdEqual), PortQuery.Esc)` (nguồn gate `:888`).
   - **FlagActive**: Tách chuỗi theo dấu phẩy, lọc `ds.Contains((x.FlagActive ?? "").ToLower())` (nguồn gate `:894-902`). Lưu ý dòng lọc bằng dấu '=' (`:890-893`) bị comment trong nguồn nên không thực thi.
4. Đếm tổng số bản ghi thỏa mãn điều kiện lọc: `itemCount = await ordered.CountAsync()` (tương ứng `MyCount` trong SQL nguồn `Master.cs:30461`).
5. Sắp xếp thứ tự bản ghi: Bắt buộc sắp theo `md.DepartmentCode ASC` (`Master.cs:30457`).
6. Trích xuất trang dữ liệu:
   - Nếu `pageSize <= 0`: Trả về danh sách rỗng (nguồn gate `:84-94`).
   - Nếu `pageSize > 0`: Áp dụng `.Skip((int)start).Take((int)pageSize)`.
7. Tra cứu thông tin tên phòng ban cấp trên: Quét danh sách `DepartmentCodeParent` của trang hiện tại, tra cứu từ bảng `Mst_Department` để điền giá trị cho thuộc tính `md_DepartmentNameParent` (`Master.cs:30512-30513`).
8. Định dạng dữ liệu trả về:
   - Gán chỉ số `MyIdxSeq = start + i` (0-based) tương ứng với số thứ tự định danh trong SQL nguồn (`Master.cs:30450`).
   - Gán `ss_QtyStaff = 0`.
9. Tính toán số trang `PageCount`:
   - Nếu không có dòng dữ liệu nào (`rows.Count == 0`), `PageCount = 0`.
   - Ngược lại, `PageCount = ceil(itemCount / ps)` với `ps = pageSize == 0 ? 10 : pageSize` (nguồn gate `:87-92`).

#### Bảng mã lỗi
Endpoint này không phát sinh lỗi nghiệp vụ; các tham số không hợp lệ hoặc không tìm thấy dữ liệu đều trả về HTTP 200 kèm danh sách rỗng.

#### Ví dụ Request / Response thực tế (từ lần chạy VERIFY #122)

**Ví dụ 1: Tìm kiếm toàn bộ (Empty Body `{}`)**
- Request:
  ```http
  POST /api/mst_department/search HTTP/1.1
  Content-Type: application/json

  {}
  ```
- Response (HTTP 200):
  ```json
  {
    "Success": true,
    "Data": {
      "DataList": [
        {
          "MyIdxSeq": 0,
          "DepartmentCode": "HQ",
          "NetworkID": "DEMO",
          "DepartmentCodeParent": null,
          "DepartmentBUCode": "HQ",
          "DepartmentBUPattern": "HQ%",
          "DepartmentLevel": 1,
          "MST": null,
          "OrgID": "4020320001",
          "DepartmentName": "Trụ sở chính",
          "DepartmentDesc": null,
          "FlagActive": "1",
          "LogLUDTimeUTC": null,
          "LogLUBy": "seed",
          "md_DepartmentNameParent": null,
          "ss_QtyStaff": 0
        },
        {
          "MyIdxSeq": 1,
          "DepartmentCode": "KD",
          "NetworkID": "DEMO",
          "DepartmentCodeParent": "HQ",
          "DepartmentBUCode": "HQ.KD",
          "DepartmentBUPattern": "HQ.KD%",
          "DepartmentLevel": 2,
          "MST": null,
          "OrgID": "4020320001",
          "DepartmentName": "Kinh doanh",
          "DepartmentDesc": null,
          "FlagActive": "1",
          "LogLUDTimeUTC": null,
          "LogLUBy": "seed",
          "md_DepartmentNameParent": "Trụ sở chính",
          "ss_QtyStaff": 0
        }
      ],
      "ItemCount": 4,
      "PageCount": 1,
      "PageIndex": 0,
      "PageSize": 10
    }
  }
  ```

**Ví dụ 2: Lọc theo từ khóa `KeyWord = "KINH"`**
- Request:
  ```json
  {
    "KeyWord": "KINH"
  }
  ```
- Response: `ItemCount = 1`, `PageCount = 1`, `DataList` chỉ chứa bản ghi phòng ban `"KD"`.

**Ví dụ 3: Đặt `Ft_PageSize = 0`**
- Request:
  ```json
  {
    "Ft_PageSize": 0
  }
  ```
- Response: `ItemCount = 4`, `PageCount = 0`, `PageIndex = 0`, `PageSize = 0`, `DataList = []`.

---

### 2.2. GetAllActive (#123)

Lấy danh sách tất cả các phòng ban đang hoạt động (`FlagActive = '1'`) thuộc tổ chức quản lý.

- **Route**: `POST /api/mst_department/getallactive`
- **Controller Action**: [`Mst_DepartmentControllerPort.GetAllActive`](../../Controllers/Port/Mst_DepartmentControllerPort.cs#L257-L267)
- **Tham chiếu nguồn**: Gate `Mst_DepartmentController.cs:110-218` → API `MstDepartmentController.cs:20-89` → Biz `Master.cs:29450-29701`, `:30396-30568`

#### Bảng tham số Request
Endpoint không yêu cầu tham số request body.

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Thiết lập tham số mặc định của nguồn gate (`Mst_DepartmentController.cs:152-153`):
   - `Ft_RecordStart = 0`
   - `Ft_RecordCount = 123456000` (giá trị cố định của nguồn để lấy toàn bộ dữ liệu)
   - `FlagActive = "1"` (`:139`)
2. Lọc theo tổ chức:
   - Nguồn gate (`:138`) lấy mã tổ chức từ phiên đăng nhập người dùng: `OrgID = orgUserUI.OrgId`.
   - Ở đích chưa có phiên đăng nhập: Đọc mã tổ chức từ cấu hình `_config["Biz_OrgID"]` ([`Mst_DepartmentControllerPort.cs:263`](../../Controllers/Port/Mst_DepartmentControllerPort.cs#L263)).
   - Nếu `Biz_OrgID` không được cấu hình (null hoặc khoảng trắng): Bỏ qua điều kiện lọc theo tổ chức, trả về phòng ban hoạt động của tất cả tổ chức (ghi nhận GAP).
3. Cấu trúc cây (Ext/tree) bị chú thích trong mã nguồn gốc (`:162-193`), do đó API trả về **mảng phẳng (flat list)**.
4. Xử lý kết quả trả về (`Mst_DepartmentController.cs:133, 159-161, 199-204`):
   - Nếu có ít nhất 1 phòng ban: Trả về `Data = rows` (danh sách bản ghi).
   - Nếu không có phòng ban nào thỏa mãn: Trả về `Data = null` (không trả về mảng rỗng `[]`).

#### Bảng mã lỗi
Endpoint luôn trả về HTTP 200.

#### Ví dụ Request / Response thực tế (từ lần chạy VERIFY #123)
- Request:
  ```http
  POST /api/mst_department/getallactive HTTP/1.1
  Content-Type: application/json
  ```
- Khi không cấu hình `Biz_OrgID` (trả về cả 3 phòng ban active `HQ`, `KD`, `KT`):
  ```json
  {
    "Success": true,
    "Data": [
      {
        "MyIdxSeq": 0,
        "DepartmentCode": "HQ",
        "NetworkID": "DEMO",
        "DepartmentCodeParent": null,
        "DepartmentBUCode": "HQ",
        "DepartmentBUPattern": "HQ%",
        "DepartmentLevel": 1,
        "MST": null,
        "OrgID": "4020320001",
        "DepartmentName": "Trụ sở chính",
        "DepartmentDesc": null,
        "FlagActive": "1",
        "LogLUDTimeUTC": null,
        "LogLUBy": "seed",
        "md_DepartmentNameParent": null,
        "ss_QtyStaff": 0
      },
      {
        "MyIdxSeq": 1,
        "DepartmentCode": "KD",
        "NetworkID": "DEMO",
        "DepartmentCodeParent": "HQ",
        "DepartmentBUCode": "HQ.KD",
        "DepartmentBUPattern": "HQ.KD%",
        "DepartmentLevel": 2,
        "MST": null,
        "OrgID": "4020320001",
        "DepartmentName": "Kinh doanh",
        "DepartmentDesc": null,
        "FlagActive": "1",
        "LogLUDTimeUTC": null,
        "LogLUBy": "seed",
        "md_DepartmentNameParent": "Trụ sở chính",
        "ss_QtyStaff": 0
      },
      {
        "MyIdxSeq": 2,
        "DepartmentCode": "KT",
        "NetworkID": "DEMO",
        "DepartmentCodeParent": "HQ",
        "DepartmentBUCode": "HQ.KT",
        "DepartmentBUPattern": "HQ.KT%",
        "DepartmentLevel": 2,
        "MST": null,
        "OrgID": "4020320002",
        "DepartmentName": "Kỹ thuật",
        "DepartmentDesc": null,
        "FlagActive": "1",
        "LogLUDTimeUTC": null,
        "LogLUBy": "seed",
        "md_DepartmentNameParent": "Trụ sở chính",
        "ss_QtyStaff": 0
      }
    ]
  }
  ```
- Khi cấu hình `$env:Biz_OrgID="4020320001"`: `Data` chỉ trả về 2 phòng ban `HQ` và `KD`. Phòng ban ngừng hoạt động `OLD` (`FlagActive = '0'`) bị loại bỏ hoàn toàn.

---

### 2.3. GetByDepartmentCode (#124)

Tra cứu chi tiết một phòng ban duy nhất dựa trên mã phòng ban (`DepartmentCode`).

- **Route**: `POST /api/mst_department/getbydepartmentcode`
- **Controller Action**: [`Mst_DepartmentControllerPort.GetByDepartmentCode`](../../Controllers/Port/Mst_DepartmentControllerPort.cs#L277-L292)
- **Tham chiếu nguồn**: Gate `Mst_DepartmentController.cs:219-282` → API `MstDepartmentController.cs:20-89` → Biz `Master.cs:29450-29701`, `:30396-30568`

#### Bảng tham số Request

Payload JSON gửi qua Request Body (`MstDepartmentGetByDepartmentCodeReq`), hỗ trợ cả body rỗng:

| Tên tham số | Kiểu dữ liệu | Bắt buộc | Mặc định | Ý nghĩa & Quy tắc |
|---|---|---|---|---|
| `DepartmentCode` | `string?` | Không | `null` | Mã phòng ban cần tra cứu. |

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Kiểm tra tham số mã phòng ban:
   - Nếu `string.IsNullOrWhiteSpace(departmentCode)` hoặc body rỗng: Tầng gate của nguồn (`Mst_DepartmentController.cs:239, 269`) không thực hiện truy vấn xuống CSDL và trả về ngay `{ Success = true, Data = null }`.
2. Truy vấn dữ liệu qua `GetXAsync`:
   - Thiết lập `recordStart = 0`, `recordCount = 1` (`:256-257`).
   - Lọc chính xác theo mã: `DepartmentCode = (Like + PortQuery.Equal)` (nguồn `:914`).
   - **Quy tắc đặc biệt**: Nhánh lọc `OrgID` (`:916-919`) và `FlagActive` (`:920-923`) không được truyền từ gate (`:241-244`) nên không chạy. Do đó, phòng ban đã ngừng hoạt động (`FlagActive = '0'`) vẫn được lấy bình thường và không bị giới hạn bởi tổ chức của người dùng.
3. Trả về kết quả:
   - Nếu tìm thấy: Trả về phần tử đầu tiên `rows[0]` (`:263-266`).
   - Nếu không tìm thấy: Trả về `Data = null`.

#### Bảng mã lỗi
Endpoint luôn trả về HTTP 200 kể cả khi không tìm thấy mã phòng ban.

#### Ví dụ Request / Response thực tế (từ lần chạy VERIFY #124)
- **Tra cứu thành công mã `"kd"` (không phân biệt hoa thường)**:
  - Request: `{"DepartmentCode":"kd"}`
  - Response:
    ```json
    {
      "Success": true,
      "Data": {
        "MyIdxSeq": 0,
        "DepartmentCode": "KD",
        "NetworkID": "DEMO",
        "DepartmentCodeParent": "HQ",
        "DepartmentBUCode": "HQ.KD",
        "DepartmentBUPattern": "HQ.KD%",
        "DepartmentLevel": 2,
        "MST": null,
        "OrgID": "4020320001",
        "DepartmentName": "Kinh doanh",
        "DepartmentDesc": null,
        "FlagActive": "1",
        "LogLUDTimeUTC": null,
        "LogLUBy": "seed",
        "md_DepartmentNameParent": "Trụ sở chính",
        "ss_QtyStaff": 0
      }
    }
    ```
- **Tra cứu phòng ban ngừng hoạt động `"OLD"`**: Trả về đúng bản ghi phòng ban `"OLD"` với `FlagActive = "0"`.
- **Tra cứu mã không tồn tại `"K"` hoặc mã rỗng `""` hoặc `{}`**: Trả về `{"Success": true, "Data": null}`.

---

### 2.4. Create (#125)

Tạo mới phòng ban. Hệ thống tự động sinh mã theo sequence, gán tổ chức từ cấu hình và tái cấu trúc cây phân cấp BU toàn bảng.

- **Route**: `POST /api/mst_department/create`
- **Controller Action**: [`Mst_DepartmentControllerPort.Create`](../../Controllers/Port/Mst_DepartmentControllerPort.cs#L302-L345)
- **Tham chiếu nguồn**: Gate `Mst_DepartmentController.cs:286-370` → API `MstDepartmentController.cs:91-160` → Biz `Master.cs:29702-29935`, `:30569-30726`, `:29413-29448`

#### Bảng tham số Request

Payload JSON gửi qua Request Body (`MstDepartmentCreateReq`):

| Tên tham số | Kiểu dữ liệu | Bắt buộc | Mặc định | Ý nghĩa & Quy tắc |
|---|---|---|---|---|
| `DepartmentName` | `string?` | **Có** | | Tên phòng ban mới (không được rỗng sau khi chuẩn hóa `StdStr`). |
| `DepartmentCodeParent` | `string?` | Không | `null` | Mã phòng ban cấp trên. Nếu có, bắt buộc phải tồn tại và đang hoạt động. |
| `DepartmentDesc` | `string?` | Không | `null` | Mô tả phòng ban. |
| `DepartmentCode` | `string?` | Không | `null` | **Bị bỏ qua**: Hệ thống tự động sinh mã từ sequence và ghi đè giá trị client gửi. |
| `OrgID` | `string?` | Không | `null` | **Bị bỏ qua**: Nguồn lấy từ user đăng nhập; ở đích lấy từ cấu hình `Biz_OrgID`. |
| `MST` | `string?` | Không | `null` | **Bị bỏ qua**: Lấy từ bảng `Mst_Org.MST` tương ứng với `OrgID`. |

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. **Kiểm tra body rỗng / null**:
   - Nếu `req == null`: Gate nguồn (`:296-303`) ném `ServiceException("strJson null")`, trả về HTTP 400 kèm mã lỗi `ErridnLoyalty.Mst_Department_Create` (`Check.strJson = null`).
   - Bước này diễn ra trước khi gọi sequence nên **không làm tăng sequence**.
2. **Sinh mã phòng ban tự động**:
   - Gọi [`PortSeq.NextAsync<Seq_DepartmentCode>(_db)`](../../Data/PortSeq.cs#L44-L61) trong một transaction riêng đã commit ngay.
   - Định dạng mã: `PortSeq.Build("{0}DPMCODE.{3}.{1:00000}{2}", "", seq, 100000, "", DateTime.UtcNow)` (ví dụ: `DPMCODE.GAG.00001`). Mã này thay thế hoàn toàn `DepartmentCode` client gửi lên (`Mst_DepartmentController.cs:318-337`).
3. **Mở Transaction nghiệp vụ duy nhất** ([`Master.cs:29827`](../../Controllers/Port/Mst_DepartmentControllerPort.cs#L317)).
4. **Chuẩn hóa chuỗi đầu vào**:
   - Các trường mã: `strDepartmentCode`, `strOrgID_UC`, `strDepartmentCodeParent`, `strMST_UC` qua `StdParam` (`Trim().ToUpper()`, rỗng thành `null`).
   - Các trường văn bản: `strDepartmentName`, `strDepartmentDesc` qua `StdStr` (`Trim().Normalize()`, rỗng thành `null`).
5. **Xác định thông tin tổ chức & thuế**:
   - `strOrgID_UC = StdParam(_config["Biz_OrgID"])`.
   - `strMST_UC`: Tra cứu từ bảng `Mst_Org` theo `OrgID = strOrgID_UC`.
6. **Kiểm tra nghiệp vụ tuần tự (Guards - Master.cs:30620-30689)**:
   - **(a) Kiểm tra mã phòng ban**: Nếu `strDepartmentCode` rỗng → `ErridnLoyalty.Mst_Department_Create_InvalidDepartmentCode`.
   - **(b) Kiểm tra tồn tại mã**: Gọi `Mst_Department_CheckDB(code, "0")` → Nếu đã tồn tại trả về `ErridnLoyalty.Mst_Department_CheckDB_DepartmentExist`.
   - **(c) Kiểm tra người nộp thuế (`Mst_NNT`)**:
     - Nếu `nntDb == null` → Trả về `ErridnLoyalty.Mst_NNT_CheckDB_NNTNotFound` (`Check.MST = strMST_UC`).
     - Nếu `nntDb.FlagActive != "1"` → Trả về `ErridnLoyalty.Mst_NNT_CheckDB_FlagActiveNotMatched`.
   - **(d) Kiểm tra phòng ban cấp trên**: Nếu có `strDepartmentCodeParent`:
     - Tra cứu trong `Mst_Department`: Nếu không tồn tại → Trả về `ErridnLoyalty.Mst_Department_CheckDB_DepartmentNotFound` (`Check.DepartmentCode = strDepartmentCodeParent`).
     - Nếu phòng ban cha có `FlagActive != "1"` → Trả về `ErridnLoyalty.Mst_Department_CheckDB_FlagActiveNotMatched`.
   - **(e) Kiểm tra tên phòng ban**: Nếu `strDepartmentName` rỗng → Trả về `ErridnLoyalty.Mst_Department_Create_InvalidDepartmentName`.
   - **(f) Kiểm tra trùng tên phòng ban**: Tra cứu trong cùng tổ chức (`OrgID = strOrgID_UC` và `DepartmentName LIKE namePattern` không phân biệt hoa thường): Nếu trùng → Trả về `ErridnLoyalty.Mst_Department_CheckDB_DepartmentNameExist`.
7. **Lưu bản ghi vào CSDL**:
   - Thêm bản ghi `Mst_Department`: `NetworkID = _config["Biz_NetworkID"] ?? "1001"`, `OrgID = strOrgID_UC`, `MST = strMST_UC`, `FlagActive = "1"`, `DepartmentBUCode = "X"`, `DepartmentBUPattern = "X"`, `DepartmentLevel = 1`, `LogLUDTimeUTC = DateTime.UtcNow`, `LogLUBy = "port"`.
8. **Tái cấu trúc phân cấp toàn bảng (`UpdBUAsync`)** ([`Master.cs:29413-29448`](../../Controllers/Port/Mst_DepartmentControllerPort.cs#L995-L1048)):
   - **Bước 1**: Tìm bản ghi có mã gốc viết cứng là `"HO"` (`@strDepartmentCode_Root = 'HO'` tại `Master.cs:29416`) → Gán `BUCode = "HO"`, `BUPattern = "HO%"`, `Level = 1`.
   - **Bước 2**: Thực hiện 7 vòng lặp snapshot cho các phòng ban khác `"HO"`. Tại mỗi vòng, đọc thông tin của phòng ban cha từ snapshot vòng trước:
     - `BUCode = (string.IsNullOrEmpty(parentBU) ? "" : parentBU + ".") + Code`
     - `BUPattern = BUCode + "%"`
     - `Level = (parentLevel ?? 0) + 1`
9. **Commit transaction** và trả về `{ Success = true, Data = null }`.

#### Bảng mã lỗi `ErridnLoyalty.*`

| Mã lỗi | Điều kiện kích hoạt | Các tham số trả về trong `Params` |
|---|---|---|
| `ErridnLoyalty.Mst_Department_Create` | Request body rỗng hoặc null (`gate :296-303`) | `Check.strJson = null` |
| `ErridnLoyalty.Mst_Department_Create_InvalidDepartmentCode` | Mã phòng ban rỗng sau chuẩn hóa | `Check.strDepartmentCode = <value>` |
| `ErridnLoyalty.Mst_Department_CheckDB_DepartmentExist` | Mã phòng ban đã tồn tại trong CSDL | `Check.DepartmentCode = <value>` |
| `ErridnLoyalty.Mst_NNT_CheckDB_NNTNotFound` | Không tìm thấy mã số thuế trong bảng `Mst_NNT` (hoặc chưa cấu hình `Biz_OrgID`) | `Check.MST = <value>` |
| `ErridnLoyalty.Mst_NNT_CheckDB_FlagActiveNotMatched` | Bản ghi `Mst_NNT` có `FlagActive != '1'` | `Check.MST`, `Check.strFlagActiveListToCheck = "1"`, `DB.FlagActive` |
| `ErridnLoyalty.Mst_Department_CheckDB_DepartmentNotFound` | Phòng ban cha `DepartmentCodeParent` không tồn tại trong CSDL | `Check.DepartmentCode = <parentCode>` |
| `ErridnLoyalty.Mst_Department_CheckDB_FlagActiveNotMatched` | Phòng ban cha có `FlagActive != '1'` (đã ngừng hoạt động) | `Check.DepartmentCode = <parentCode>`, `Check.strFlagActiveListToCheck = "1"`, `DB.FlagActive` |
| `ErridnLoyalty.Mst_Department_Create_InvalidDepartmentName` | Tên phòng ban rỗng hoặc chỉ có khoảng trắng | `Check.strDepartmentName = null` |
| `ErridnLoyalty.Mst_Department_CheckDB_DepartmentNameExist` | Tên phòng ban bị trùng trong cùng tổ chức `OrgID` | `Check.OrgID = <orgId>`, `Check.DepartmentName = <name>` |

#### Ví dụ Request / Response thực tế (từ lần chạy VERIFY #125)
- **Tạo phòng ban thành công**:
  - Request:
    ```json
    {
      "DepartmentName": " Phòng Mới ",
      "DepartmentCodeParent": "kd",
      "DepartmentDesc": " d ",
      "OrgID": "X",
      "MST": "Y",
      "DepartmentCode": "abc"
    }
    ```
  - Response (HTTP 200):
    ```json
    {
      "Success": true,
      "Data": null
    }
    ```
  - Giá trị lưu thực tế trong CSDL: Mã sinh tự động `DPMCODE.GAG.00001`, `OrgID = '4020320001'`, `MST = '0100000001'`, `DepartmentCodeParent = 'KD'`, `DepartmentName = 'Phòng Mới'`, `DepartmentDesc = 'd'`, `DepartmentBUCode = 'HQ.KD.DPMCODE.GAG.00001'`, `DepartmentBUPattern = 'HQ.KD.DPMCODE.GAG.00001%'`, `DepartmentLevel = 3`.
- **Tạo với phòng ban cha đã ngừng hoạt động (`"OLD"`)**:
  - Response (HTTP 400):
    ```json
    {
      "Success": false,
      "ErrorCode": "ErridnLoyalty.Mst_Department_CheckDB_FlagActiveNotMatched",
      "Params": {
        "Check.DepartmentCode": "OLD",
        "Check.strFlagActiveListToCheck": "1",
        "DB.FlagActive": "0"
      }
    }
    ```

---

### 2.5. Update (#126)

Cập nhật thông tin tên, mô tả và trạng thái hoạt động của một phòng ban hiện có.

- **Route**: `POST /api/mst_department/update`
- **Controller Action**: [`Mst_DepartmentControllerPort.Update`](../../Controllers/Port/Mst_DepartmentControllerPort.cs#L517-L545)
- **Tham chiếu nguồn**: Gate `Mst_DepartmentController.cs:374-443` → API `MstDepartmentController.cs:162-231` → Biz `Master.cs:29936-30180`, `:30727-30864`

#### Bảng tham số Request

Payload JSON gửi qua Request Body (`MstDepartmentUpdateReq`):

| Tên tham số | Kiểu dữ liệu | Bắt buộc | Mặc định | Ý nghĩa & Quy tắc |
|---|---|---|---|---|
| `DepartmentCode` | `string?` | **Có** | | Mã phòng ban cần cập nhật (phải tồn tại trong CSDL). |
| `DepartmentName` | `string?` | **Có** | | Tên phòng ban mới (không được rỗng sau khi chuẩn hóa `StdStr`). |
| `DepartmentDesc` | `string?` | Không | `null` | Mô tả phòng ban. |
| `FlagActive` | `string?` | Không | `"0"` | Trạng thái hoạt động. Nếu thiếu hoặc khác `"1"`, qua `StdFlag` sẽ thành `"0"`. |
| `DepartmentCodeParent` | `string?` | Không | `null` | **Không thay đổi**: Dù client có gửi thì tầng biz cũng không bao giờ cập nhật phòng ban cha. |
| `OrgID` / `MST` | `string?` | Không | `null` | **Không thay đổi**: Không được phép sửa đổi qua hàm Update. |

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. **Kiểm tra body rỗng / null**: Nếu `req == null` → Trả về HTTP 400 kèm mã lỗi `ErridnLoyalty.Mst_Department_Update` (`Check.strJson = null`).
2. **Xác định danh sách cột được phép cập nhật (`Ft_Cols_Upd`)**:
   - Chuỗi cố định từ gate nguồn (`:404-410`): `"Mst_Department.DepartmentCodeParent,Mst_Department.DepartmentName,Mst_Department.DepartmentDesc,Mst_Department.FlagActive,"` (cột `MST` bị comment ở nguồn).
   - **Hành vi đặc biệt về phòng ban cha**: Mặc dù chuỗi `Ft_Cols_Upd` có chứa `DepartmentCodeParent`, nhưng tầng nghiệp vụ `Mst_Department_UpdateX` (`Master.cs:30780-30784, :30848-30855`) **hoàn toàn không kiểm tra cờ này và không có lệnh gán `DepartmentCodeParent` vào DB**. Vì vậy, **phòng ban cha không bao giờ bị thay đổi**.
   - **Không gọi `UpdBU`**: Hàm update không thực thi lại `UpdBU`, cấu trúc BU và Level giữ nguyên.
3. **Chuẩn hóa dữ liệu đầu vào**:
   - `DepartmentCode`: Chuẩn hóa qua `StdParam`.
   - `DepartmentName`, `DepartmentDesc`: Chuẩn hóa qua `StdStr`.
   - `FlagActive`: Chuẩn hóa qua `StdFlag` (khác `"1"` chuyển thành `"0"`).
4. **Kiểm tra nghiệp vụ tuần tự**:
   - **(a) Tra cứu bản ghi**: Nếu `DepartmentCode` rỗng hoặc không tìm thấy trong CSDL → Trả về `ErridnLoyalty.Mst_Department_CheckDB_DepartmentNotFound` (`Check.DepartmentCode = <code >`).
   - **(b) Kiểm tra tên rỗng**: Nếu `DepartmentName` rỗng sau chuẩn hóa → Trả về `ErridnLoyalty.Mst_Department_UpdateX_InvalidDepartmentName`.
   - **(c) Kiểm tra trùng tên phòng ban**:
     - **Chỉ kích hoạt khi tên mới KHÁC tên cũ trong DB theo phép so sánh phân biệt hoa thường chính xác** (`!string.Equals(strDepartmentName, strDepartmentName_DB, StringComparison.Ordinal)` tại `Master.cs:30825`).
     - Khi tên có sự khác biệt, thực hiện tra cứu bằng `LIKE` không phân biệt hoa thường trong cùng `OrgID`.
     - **Hệ quả nghiệp vụ**: Nếu người dùng chỉ thay đổi chữ hoa/chữ thường (ví dụ: đổi `"Kinh doanh 2"` thành `"KINH DOANH 2"`), phép so sánh Ordinal xác nhận có sự thay đổi, nhưng câu truy vấn `LIKE` sau đó lại so sánh không phân biệt hoa thường và vấp phải chính bản ghi hiện tại → Báo lỗi `ErridnLoyalty.Mst_Department_CheckDB_DepartmentNameExist`. Đây là hành vi gốc của hệ thống nguồn được giữ nguyên.
5. **Cập nhật CSDL**: Gán các trường `DepartmentName`, `DepartmentDesc`, `FlagActive`, `LogLUDTimeUTC = DateTime.UtcNow`, `LogLUBy = "port"`. Commit transaction và trả về `{ Success = true, Data = null }`.

#### Bảng mã lỗi `ErridnLoyalty.*`

| Mã lỗi | Điều kiện kích hoạt | Các tham số trả về trong `Params` |
|---|---|---|
| `ErridnLoyalty.Mst_Department_Update` | Request body rỗng hoặc null | `Check.strJson = null` |
| `ErridnLoyalty.Mst_Department_CheckDB_DepartmentNotFound` | Mã phòng ban rỗng hoặc không tồn tại trong CSDL | `Check.DepartmentCode = <code >` |
| `ErridnLoyalty.Mst_Department_UpdateX_InvalidDepartmentName` | Tên phòng ban rỗng hoặc chỉ có khoảng trắng | `Check.strDepartmentName = null` |
| `ErridnLoyalty.Mst_Department_CheckDB_DepartmentNameExist` | Tên phòng ban mới bị trùng với một phòng ban khác cùng `OrgID` | `Check.OrgID = <orgId>`, `Check.DepartmentName = <name>` |

#### Ví dụ Request / Response thực tế (từ lần chạy VERIFY #126)
- **Cập nhật thành công**:
  - Request:
    ```json
    {
      "DepartmentCode": "kd",
      "DepartmentName": "  Kinh doanh 2  ",
      "DepartmentDesc": " mô tả ",
      "FlagActive": "1",
      "DepartmentCodeParent": "KT"
    }
    ```
  - Response: `{"Success": true, "Data": null}`. Kiểm tra CSDL: `DepartmentName = 'Kinh doanh 2'`, `DepartmentDesc = 'mô tả'`, `FlagActive = '1'`, `DepartmentCodeParent` vẫn giữ `'HQ'` (không đổi thành `'KT'`), `DepartmentBUCode` vẫn giữ `'HQ.KD'`.
- **Cập nhật thiếu `FlagActive`**: `{"DepartmentCode":"KD", "DepartmentName":"Kinh doanh 2"}` → Cờ `FlagActive` tự động chuyển về `'0'` (ngừng hoạt động).
- **Lỗi đổi chữ hoa/thường vấp chính nó**:
  - Request: `{"DepartmentCode":"KD", "DepartmentName":"KINH DOANH 2", "FlagActive":"1"}`
  - Response (HTTP 400):
    ```json
    {
      "Success": false,
      "ErrorCode": "ErridnLoyalty.Mst_Department_CheckDB_DepartmentNameExist",
      "Params": {
        "Check.OrgID": "4020320001",
        "Check.DepartmentName": "KINH DOANH 2"
      }
    }
    ```

---

### 2.6. Delete (#127)

Xóa một bản ghi phòng ban đơn lẻ khỏi cơ sở dữ liệu.

- **Route**: `POST /api/mst_department/delete`
- **Controller Action**: [`Mst_DepartmentControllerPort.Delete`](../../Controllers/Port/Mst_DepartmentControllerPort.cs#L660-L691)
- **Tham chiếu nguồn**: Gate `Mst_DepartmentController.cs:446-515` → API `MstDepartmentController.cs:233-302` → Biz `Master.cs:30181-30395`, `:30865-30968`

#### Bảng tham số Request

Payload JSON gửi qua Request Body (`MstDepartmentDeleteReq`):

| Tên tham số | Kiểu dữ liệu | Bắt buộc | Mặc định | Ý nghĩa & Quy tắc |
|---|---|---|---|---|
| `DepartmentCode` | `string?` | **Có** | | Mã phòng ban cần xóa (kiểm tra rỗng đầu tiên tại gate). |
| `OrgID` | `string?` | **Có** | | Mã tổ chức (kiểm tra rỗng thứ hai tại gate; không sử dụng trong logic xóa sau đó). |

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. **Kiểm tra rỗng tại tầng Gate** (`Mst_DepartmentController.cs:465, 467, 495, 500`):
   - Kiểm tra `DepartmentCode`: Nếu rỗng hoặc body rỗng → Ném ngoại lệ `ServiceException("invalid_departmentcode")`, trả về HTTP 400 kèm mã lỗi `ErridnLoyalty.invalid_departmentcode`.
   - Kiểm tra `OrgID`: Nếu rỗng → Ném ngoại lệ `ServiceException("invalid_orgid")`, trả về HTTP 400 kèm mã lỗi `ErridnLoyalty.invalid_orgid` (thực hiện sau kiểm tra `DepartmentCode`).
2. **Thực thi nghiệp vụ xóa qua hàm `DeleteXAsync(code)`**:
   - Mở 1 transaction độc lập cho thao tác xóa.
   - Chuẩn hóa mã qua `StdParam`.
   - Kiểm tra tồn tại trong CSDL (`Mst_Department_CheckDB(ma, "1")` tại `Master.cs:30897`): Nếu không tìm thấy bản ghi → Rollback transaction, trả về HTTP 400 kèm mã lỗi `ErridnLoyalty.Mst_Department_CheckDB_DepartmentNotFound`.
   - **Các hành vi đặc biệt**:
     - **Không kiểm tra phòng ban con**: Nếu phòng ban bị xóa có các phòng ban cấp dưới, hệ thống nguồn không kiểm tra chặn xóa; các phòng ban con vẫn tiếp tục giữ `DepartmentCodeParent` trỏ tới mã đã xóa.
     - **Không gọi `UpdBU`**: Hệ thống nguồn không gọi lại `Mst_Department_UpdBU` khi xóa dòng.
     - **GAP Bảng liên kết**: Nguồn kiểm tra ràng buộc nhân viên trong `Staff_MapDepartment` (`:30909-30953`) và khóa ngoại `Train_CourseDepartment`. Do các bảng này chưa có ở đích nên không thực hiện kiểm tra chặn.
   - Thực hiện xóa dòng khỏi bảng `Mst_Department`, lưu thay đổi và commit transaction.
3. Trả về `{ Success = true, Data = null }` (`:490-491`).

#### Bảng mã lỗi `ErridnLoyalty.*`

| Mã lỗi | Điều kiện kích hoạt | Các tham số trả về trong `Params` |
|---|---|---|
| `ErridnLoyalty.invalid_departmentcode` | `DepartmentCode` rỗng hoặc body rỗng | `Check.DepartmentCode = <value>` |
| `ErridnLoyalty.invalid_orgid` | `OrgID` rỗng trong khi `DepartmentCode` hợp lệ | `Check.OrgID = <value>` |
| `ErridnLoyalty.Mst_Department_CheckDB_DepartmentNotFound` | Phòng ban không tồn tại trong CSDL | `Check.DepartmentCode = <code >` |
| `ErridnLoyalty.Mst_Department_Delete` | Ngoại lệ DB bất ngờ trong quá trình xóa bản ghi (`DeleteXAsync`, commit `1752274`) | `Exception = <thông báo ngoại lệ>` |

> [!NOTE]
> **Xử lý ngoại lệ DB bất ngờ (Commit `1752274`)**: Trước đây, ngoại lệ DB bất ngờ trong `DeleteXAsync` bị ném lại (`throw;`) dẫn tới lỗi máy chủ HTTP 500. Hiện tại, khối `catch (Exception ex)` thực hiện rollback transaction và trả về HTTP 400 kèm mã lỗi `ErridnLoyalty.Mst_Department_Delete` với `Params.Exception = ex.Message`, đảm bảo hành vi khớp 1-1 với tầng gate nguồn (`:503-507`), không còn trả HTTP 500.

#### Ví dụ Request / Response thực tế (từ lần chạy VERIFY #127)
- **Xóa thành công phòng ban `"old"`**:
  - Request: `{"DepartmentCode":"old", "OrgID":"4020320001"}`
  - Response: `{"Success": true, "Data": null}`. Tổng số bản ghi trong bảng giảm từ 4 xuống 3.
- **Xóa phòng ban cha `"HQ"` khi còn phòng ban con `"KD"`**:
  - Request: `{"DepartmentCode":"HQ", "OrgID":"x"}`
  - Response: `{"Success": true, "Data": null}`. Phòng ban con `"KD"` vẫn tồn tại với `DepartmentCodeParent = "HQ"`, tra cứu `GetByDepartmentCode` trên `"KD"` trả về `md_DepartmentNameParent = null`.
- **Lỗi thiếu `OrgID`**: `{"DepartmentCode":"KD"}` → HTTP 400 `ErridnLoyalty.invalid_orgid`.
- **Lỗi thiếu `DepartmentCode`**: `{"OrgID":"x"}` → HTTP 400 `ErridnLoyalty.invalid_departmentcode`.

---

### 2.7. DeleteMultiple (#128)

Xóa danh sách nhiều phòng ban theo cơ chế lặp tuần tự từng phần tử trong các transaction độc lập.

- **Route**: `POST /api/mst_department/deletemultiple`
- **Controller Action**: [`Mst_DepartmentControllerPort.DeleteMultiple`](../../Controllers/Port/Mst_DepartmentControllerPort.cs#L724-L754)
- **Tham chiếu nguồn**: Gate `Mst_DepartmentController.cs:516-580` → API `MstDepartmentController.cs:233-302` → Biz `Master.cs:30181-30395`, `:30865-30968`

#### Bảng tham số Request

Payload JSON gửi qua Request Body là một mảng danh sách các đối tượng: `List<MstDepartmentDeleteReq>`:

| Tên tham số | Kiểu dữ liệu | Bắt buộc | Mặc định | Ý nghĩa & Quy tắc |
|---|---|---|---|---|
| `[].DepartmentCode` | `string?` | Có | | Mã phòng ban cần xóa cho từng phần tử. |

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. **Kiểm tra body rỗng / null**:
   - Nếu `req == null`: Gate nguồn (`:526-533`) ném `ServiceException("strJson null")`, trả về HTTP 400 kèm mã lỗi `ErridnLoyalty.Mst_Department_Delete` (`Check.strJson = null`).
2. **Xử lý danh sách rỗng `[]`**: Vòng lặp không chạy, trả về ngay HTTP 200 `{ Success = true, Data = null }` (`:566-567`).
3. **Lặp tuần tự qua từng phần tử** (`:547-564`):
   - **Khác biệt quan trọng so với Delete đơn**: Tầng gate của `DeleteMultiple` **không kiểm tra rỗng đối với `DepartmentCode` và không kiểm tra `OrgID`** (nguồn lấy `OrgID` từ session người dùng đăng nhập `orgUserUI.OrgId`). Nếu phần tử có mã rỗng, mã rỗng được chuyển thẳng xuống hàm nghiệp vụ và gây ra lỗi `ErridnLoyalty.Mst_Department_CheckDB_DepartmentNotFound`.
   - Mỗi phần tử được thực thi qua hàm `DeleteXAsync` trong một **Transaction riêng biệt đã commit**.
4. **Hành vi Xóa một phần (Partial Delete - Nguồn `:547-573`)**:
   - Nếu phần tử thứ k gặp lỗi (ví dụ: mã không tồn tại, mã rỗng, hoặc lỗi DB bất ngờ): Vòng lặp dừng ngay lập tức, trả về lỗi của phần tử thứ k (kèm `Params.Exception` nếu là ngoại lệ DB bất ngờ theo commit `1752274`).
   - **Các phần tử đứng trước k đã được commit vào CSDL và KHÔNG bị rollback**.
   - **Các phần tử đứng sau k hoàn toàn không được xử lý**.
5. Nếu tất cả phần tử hợp lệ: Trả về HTTP 200 `{ Success = true, Data = null }`.

#### Bảng mã lỗi `ErridnLoyalty.*`

| Mã lỗi | Điều kiện kích hoạt | Các tham số trả về trong `Params` |
|---|---|---|
| `ErridnLoyalty.Mst_Department_Delete` | Request body rỗng hoặc null | `Check.strJson = null` |
| `ErridnLoyalty.Mst_Department_CheckDB_DepartmentNotFound` | Phần tử thứ k có `DepartmentCode` rỗng hoặc không tồn tại trong CSDL | `Check.DepartmentCode = <value>` |
| `ErridnLoyalty.Mst_Department_Delete` | Ngoại lệ DB bất ngờ tại phần tử thứ k (`DeleteXAsync`, commit `1752274`) | `Exception = <thông báo ngoại lệ>` |

#### Ví dụ Request / Response thực tế (từ lần chạy VERIFY #128)
- **Xóa thành công danh sách hợp lệ**:
  - Request: `[{"DepartmentCode":"old"}]`
  - Response: `{"Success": true, "Data": null}`. Số lượng bản ghi giảm từ 4 xuống 3.
- **Minh chứng hành vi Xóa một phần (Partial Delete)**:
  - Trạng thái ban đầu: CSDL có `HQ`, `KD`, `KT`.
  - Request: `[{"DepartmentCode":"KT"}, {"DepartmentCode":"ZZ"}, {"DepartmentCode":"KD"}]`
  - Quá trình xử lý:
    - Phần tử 1 (`"KT"`): Tồn tại → Commit xóa thành công khỏi CSDL.
    - Phần tử 2 (`"ZZ"`): Không tồn tại → Dừng xử lý, ném ngoại lệ.
    - Phần tử 3 (`"KD"`): Không được duyệt tới.
  - Response (HTTP 400):
    ```json
    {
      "Success": false,
      "ErrorCode": "ErridnLoyalty.Mst_Department_CheckDB_DepartmentNotFound",
      "Params": {
        "Check.DepartmentCode": "ZZ"
      }
    }
    ```
  - Trạng thái CSDL sau lệnh: Bản ghi `"KT"` **đã mất vĩnh viễn**, bản ghi `"KD"` **vẫn còn nguyên**. Số lượng bản ghi còn 2 (`HQ` và `KD`).

---

### 2.8. Import (#129)

Nhập danh sách phòng ban hàng loạt từ tập tin Excel `.xlsx`.

- **Route**: `POST /api/mst_department/import`
- **Content-Type**: `multipart/form-data`
- **Controller Action**: [`Mst_DepartmentControllerPort.Import`](../../Controllers/Port/Mst_DepartmentControllerPort.cs#L796-L892)
- **Tham chiếu nguồn**: Gate `Mst_DepartmentController.cs:584-730` → API `MstDepartmentController.cs:91-160` → Biz `Master.cs:29702-29935`, `:30569-30726`

#### Bảng tham số Request

| Tên trường | Kiểu dữ liệu | Bắt buộc | Vị trí | Ý nghĩa & Quy tắc |
|---|---|---|---|---|
| `file` | `IFormFile?` | **Có** | Form data | Tập tin Excel định dạng chuẩn `.xlsx`. |

#### Cấu trúc Tập tin Excel Mẫu Import
Dữ liệu đọc từ sheet đầu tiên, bắt đầu từ dòng dữ liệu số 2 (ô `A2`). Dòng 1 là tiêu đề gồm đúng 3 cột theo thứ tự từ điển mẫu [`GetImportDicColumsTemplate`](../../Controllers/Port/Mst_DepartmentControllerPort.cs#L902-L910) (`gate :864-873`):
1. Cột 1: `DepartmentName` ("Tên phòng ban (*)") — Bắt buộc có dữ liệu.
2. Cột 2: `DepartmentCodeParent` ("Phòng ban cấp trên") — Tùy chọn.
3. Cột 3: `DepartmentDesc` ("Mô tả") — Tùy chọn.
*(Lưu ý: Cột `DepartmentCode` bị comment ở nguồn `:868` nên không có trong file mẫu).*

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. **Kiểm tra file upload** (`Mst_DepartmentController.cs:606-621`):
   - Nếu không có file, file rỗng, hoặc phần mở rộng khác `.xlsx` → Trả về HTTP 400 kèm mã lỗi `ErridnLoyalty.Mst_Department_Import` và message `"File excel import không hợp lệ"`.
2. **Đọc dữ liệu qua `PortExcel.ReadSheet`**:
   - Nếu bảng tính không có dòng dữ liệu nào → Trả về HTTP 400 kèm message `"File excel import không có dữ liệu"` (`:625, :713-716`).
   - Nếu số cột dòng tiêu đề khác 3 → Trả về HTTP 400 kèm message `"File excel import không hợp lệ"` (`:627-631`).
3. **Kiểm tra toàn bộ các dòng TRƯỚC khi tạo (Giai đoạn Validate toàn cục)** (`:635-660`):
   - Duyệt qua từng dòng trong bảng tính: Nếu có bất kỳ dòng nào có `DepartmentName` rỗng hoặc chỉ chứa khoảng trắng → Dừng xử lý ngay lập tức, trả về HTTP 400 kèm message nguyên văn nguồn: `"Tên bộ phận không được trống"`.
   - **Không có bất kỳ bản ghi nào được tạo vào CSDL nếu có lỗi ở bước này**.
4. **Lưu từng dòng tuần tự vào CSDL (Giai đoạn Ghi dữ liệu)** (`:687-708`):
   - Mỗi dòng được xử lý trong một Transaction độc lập.
   - **Sinh mã phòng ban tự động**: Gán `DepartmentCode = Guid.NewGuid().ToString()` (`:689`). Khi đi qua `StdParam` trong `CreateXAsync`, mã chuyển thành **chuỗi GUID in hoa 36 ký tự**. *(Khác với Create đơn sử dụng sequence `DPMCODE...`, Import không qua sequence)*.
   - Xác định `OrgID` và `MST`: Client gửi `"ALL"` bị bỏ qua, thực tế lấy theo cấu hình `Biz_OrgID` giống Create đơn.
    - Gọi `CreateXAsync`: Kiểm tra sự tồn tại và cờ hoạt động của phòng ban cha, kiểm tra trùng tên trong cùng tổ chức, lưu bản ghi và chạy `UpdBUAsync` để tính toán lại phân cấp.
    - **Hành vi Nhập một phần (Partial Import)**:
      - Nếu dòng thứ k gặp lỗi nghiệp vụ (ví dụ: cha không tồn tại, cha ngừng hoạt động, hoặc trùng tên): Transaction của dòng thứ k bị rollback, dừng vòng lặp ngay và trả về lỗi của dòng thứ k.
      - **Tất cả các dòng trước k đã commit thành công và tồn tại trong CSDL**.
      - **Tất cả các dòng sau k không được xử lý**.
    - **Xử lý ngoại lệ DB bất ngờ (Commit `1752274`)**: Nếu xảy ra ngoại lệ DB bất ngờ khi lưu dòng thứ k, khối `catch (Exception ex)` thực hiện rollback transaction của dòng thứ k và trả về HTTP 400 kèm mã lỗi `ErridnLoyalty.Mst_Department_Import` với `Params.Exception = ex.Message` (không còn văng lỗi HTTP 500, khớp nguồn `gate :719-723`). Các dòng trước k đã commit vẫn tồn tại trong CSDL.
5. Trả về HTTP 200 `{ Success = true, Data = null }` (`:710-711`).

#### Bảng mã lỗi

| Mã lỗi (`ErrorCode`) | Message / Điều kiện | Tham số trả về trong `Params` |
|---|---|---|
| `ErridnLoyalty.Mst_Department_Import` | File rỗng, không phải `.xlsx`, lỗi đọc stream, hoặc số cột khác 3 | `Message = "File excel import không hợp lệ"` |
| `ErridnLoyalty.Mst_Department_Import` | File không có dòng dữ liệu nào từ ô A2 trở đi | `Message = "File excel import không có dữ liệu"` |
| `ErridnLoyalty.Mst_Department_Import` | Có dòng trong file bị rỗng tên phòng ban | `Message = "Tên bộ phận không được trống"` |
| `ErridnLoyalty.Mst_Department_CheckDB_DepartmentNotFound` | Dòng k có phòng ban cha không tồn tại | `Check.DepartmentCode = <parentCode>` |
| `ErridnLoyalty.Mst_Department_CheckDB_FlagActiveNotMatched` | Dòng k có phòng ban cha ngừng hoạt động | `Check.DepartmentCode = <parentCode>`, `Check.strFlagActiveListToCheck = "1"`, `DB.FlagActive = "0"` |
| `ErridnLoyalty.Mst_Department_CheckDB_DepartmentNameExist` | Dòng k có tên bị trùng trong cùng tổ chức | `Check.OrgID = <orgId>`, `Check.DepartmentName = <name>` |
| `ErridnLoyalty.Mst_Department_Import` | Ngoại lệ DB bất ngờ trong quá trình lưu dòng thứ k (`commit 1752274`) | `Exception = <thông báo ngoại lệ>` |

#### Ví dụ Request / Response thực tế (từ lần chạy VERIFY #129)
- **Nhập file hợp lệ thành công**:
  - File upload gồm 2 dòng: `[Phòng A, kd, mô tả a]` và `[Phòng B, (trống), (trống)]`.
  - Response (HTTP 200): `{"Success": true, "Data": null}`.
  - CSDL sau nhập: Thêm 2 phòng ban mới có mã GUID in hoa 36 ký tự. `Phòng A` nhận cha `'KD'`, `BUCode = 'HQ.KD.<GUID>'`, `Level = 3`. `Phòng B` không có cha, `BUCode = '<GUID>'`, `Level = 1`.
- **Minh chứng kiểm tra Validate toàn cục trước khi tạo**:
  - File upload gồm dòng 1 rỗng tên, dòng 2 hợp lệ `Phòng G`.
  - Response (HTTP 400):
    ```json
    {
      "Success": false,
      "ErrorCode": "ErridnLoyalty.Mst_Department_Import",
      "Params": {
        "Message": "Tên bộ phận không được trống"
      }
    }
    ```
  - CSDL: `Phòng G` không được tạo, số lượng bản ghi giữ nguyên.
- **Minh chứng hành vi Nhập một phần (Partial Import)**:
  - File upload gồm 3 dòng: Dòng 1 hợp lệ (`Phòng D`), Dòng 2 có cha không tồn tại (`ZZ`), Dòng 3 hợp lệ (`Phòng E`).
  - Response (HTTP 400):
    ```json
    {
      "Success": false,
      "ErrorCode": "ErridnLoyalty.Mst_Department_CheckDB_DepartmentNotFound",
      "Params": {
        "Check.DepartmentCode": "ZZ"
      }
    }
    ```
  - CSDL: `Phòng D` **đã được lưu thành công vào CSDL**, trong khi `Phòng E` hoàn toàn chưa được xử lý.

---

### 2.9. Export (#130)

Xuất danh sách phòng ban ra tập tin Excel `.xlsx`.

- **Route**: `POST /api/mst_department/export`
- **Controller Action**: [`Mst_DepartmentControllerPort.Export`](../../Controllers/Port/Mst_DepartmentControllerPort.cs#L944-L998)
- **Tham chiếu nguồn**: Gate `Mst_DepartmentController.cs:735-812` → API `MstDepartmentController.cs:20-89` (WA_Mst_Department_Get) → Biz `Master.cs:29450-29701`, `30396-30568`

#### Bảng tham số Request

Payload JSON gửi qua Request Body (`MstDepartmentExportReq`), hỗ trợ cả trường hợp body rỗng hoàn toàn hoặc null (`EmptyBodyBehavior.Allow`, gate `:735`):

| Tên tham số | Kiểu dữ liệu | Bắt buộc | Mặc định | Ý nghĩa & Quy tắc |
|---|---|---|---|---|
| `OrgID` | `string?` | Không | `null` | Lọc chính xác theo mã tổ chức (`OrgID = 'x'`). Khác với Search, tham số OrgID ở Export được áp dụng lọc trực tiếp theo client gửi (`gate :886-889`). |
| `KeyWord` | `string?` | Không | `null` | Từ khóa tìm kiếm theo tên phòng ban (`DepartmentName LIKE '%kw%'`, `gate :882-885`). |
| `FlagActive` | `string?` | Không | `null` | Danh sách trạng thái phân cách bởi dấu phẩy (toán tử `IN ('a','b')`, `gate :894-902`). |

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. **Lấy danh sách dữ liệu qua `GetXAsync`** (`gate :774-781`):
   - Truyền phân trang `recordStart = 0`, `recordCount = 123456000` (lấy toàn bộ bản ghi thỏa mãn).
   - Lọc theo `KeyWord` (`LIKE` trên `DepartmentName`), `OrgID` (`=` từ tham số client), `FlagActive` (`IN` tách dấu phẩy).
   - Thứ tự sắp xếp: `md.DepartmentCode ASC` (`Master.cs:30457`), select cuối sắp theo `t.MyIdxSeq ASC` (`Master.cs:30514`).
2. **Kiểm tra dữ liệu rỗng** (`gate :782, :796-798, :801-805`):
   - Nếu danh sách `rows == null || rows.Count == 0`, trả về HTTP 400 kèm mã lỗi `ErridnLoyalty.Mst_Department_Export` và `Message = "Không có dữ liệu"`.
3. **Từ điển cột xuất Excel (`GetImportDicColums()`)**:
   - Tầng gate nguồn sử dụng từ điển Import cho chức năng Export (`gate :786, :851-862`), gồm đúng 4 cột theo thứ tự:
     1. `DepartmentName`: `"Tên phòng ban (*)"`
     2. `DepartmentCodeParent`: `"Phòng ban cấp trên"`
     3. `DepartmentDesc`: `"Mô tả"`
     4. `FlagActive`: `"Trạng thái"`
   - *(Lưu ý: Các cột `md_DepartmentNameParent` và `ss_QtyStaff` có trong danh sách truy vấn nguồn nhưng không nằm trong từ điển nên không được đưa lên file Excel)*.
4. **Sinh tập tin Excel**:
   - Tạo đường dẫn file qua [`PortExcel.GenExcelExportFilePath`](../../Data/PortExcel.cs) với tiền tố `"Mst_Department"`.
   - Ghi dữ liệu ra sheet `"Mst_Department"` qua [`PortExcel.ExportToExcelFromList`](../../Data/PortExcel.cs).
5. **Cơ chế bắt ngoại lệ bất ngờ (Commit `1752274`)**:
   - Toàn bộ quá trình tạo đường dẫn và ghi tập tin Excel được bao bọc trong khối `try/catch (Exception ex)`.
   - Nếu xảy ra lỗi I/O hoặc lỗi hệ thống bất ngờ, hệ thống bắt ngoại lệ và trả về HTTP 400 kèm mã lỗi `ErridnLoyalty.Mst_Department_Export` với `Params.Exception = ex.Message`. Tuyệt đối không để văng ngoại lệ gây lỗi máy chủ HTTP 500, khớp 1-1 với `gate :801-805`.
6. **Trả về kết quả thành công**:
   - Trả về HTTP 200 `{ Success = true, Data = { Url = baseAddress + virtualPath } }` (`gate :790-793`). Không mở transaction, không ghi CSDL (chỉ đọc).

#### Bảng mã lỗi `ErridnLoyalty.*`

| Mã lỗi | Điều kiện kích hoạt | Các tham số trả về trong `Params` |
|---|---|---|
| `ErridnLoyalty.Mst_Department_Export` | Không có bản ghi nào thỏa mãn điều kiện lọc (`rows.Count == 0`) | `Message = "Không có dữ liệu"` |
| `ErridnLoyalty.Mst_Department_Export` | Ngoại lệ hệ thống hoặc lỗi I/O bất ngờ khi tạo tập tin Excel (`commit 1752274`) | `Exception = <thông báo ngoại lệ>` |

#### Ví dụ Request / Response thực tế (từ lần chạy VERIFY #130)
- **Xuất toàn bộ danh sách mặc định (`{}`)**:
  - Request: `POST /api/mst_department/export` kèm body `{}`
  - Response (HTTP 200):
    ```json
    {
      "Success": true,
      "Data": {
        "Url": "http://localhost:5000/TempFiles/2026/10/16/Mst_Department_20261016111530_xxxx.xlsx"
      }
    }
    ```
  - Kết quả kiểm chứng OpenXML tập tin `.xlsx`: Tên sheet `"Mst_Department"`, đúng 4 cột tiêu đề theo thứ tự, đủ 4 dòng dữ liệu demo (`HQ`, `KD`, `KT`, `old`).
- **Lọc theo cờ hoạt động**:
  - Request: `{"FlagActive": "1"}` → HTTP 200, file Excel xuất 3 dòng (`HQ`, `KD`, `KT`).
  - Request: `{"FlagActive": "1,0"}` → HTTP 200, file Excel xuất 4 dòng.
- **Lọc theo tổ chức và từ khóa**:
  - Request: `{"OrgID": "4020320001"}` → HTTP 200, file Excel xuất 3 dòng.
  - Request: `{"KeyWord": "kinh"}` → HTTP 200, file Excel xuất 1 dòng (`KD`).
- **Lỗi không có dữ liệu**:
  - Request: `{"KeyWord": "khongco"}`
  - Response (HTTP 400):
    ```json
    {
      "Success": false,
      "ErrorCode": "ErridnLoyalty.Mst_Department_Export",
      "Params": {
        "Message": "Không có dữ liệu"
      }
    }
    ```

---

### 2.9. ExportTemplate (#131)

Xuất tập tin bảng tính Excel mẫu (chỉ chứa tiêu đề cột, không có dòng dữ liệu) dùng cho chức năng nhập danh mục phòng ban từ Excel.

- **Route**: `POST /api/mst_department/exporttemplate`
- **Controller Action**: [`Mst_DepartmentControllerPort.ExportTemplate`](../../Controllers/Port/Mst_DepartmentControllerPort.cs#L1011)
- **Tham chiếu nguồn**: Gate `Mst_DepartmentController.cs:815-847`, từ điển mẫu `GetImportDicColumsTemplate:864-873`

#### Bảng tham số Request
Endpoint không nhận tham số Body (`gate :815`).

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. **Khởi tạo danh sách thực thể rỗng**: Tạo `new List<Mst_Department>()` (`gate :823`), không truy vấn CSDL, không kiểm tra điều kiện lọc hay phân quyền.
2. **Từ điển cột mẫu (`GetImportDicColumsTemplate()`)**:
   - Tầng gate nguồn quy định đúng 3 cột tiêu đề theo thứ tự (`gate :864-873`):
     1. `DepartmentName`: `"Tên phòng ban (*)"`
     2. `DepartmentCodeParent`: `"Phòng ban cấp trên"`
     3. `DepartmentDesc`: `"Mô tả"`
   - *(Lưu ý: Cột `DepartmentCode` bị comment ở nguồn `:868` nên không xuất hiện trong file mẫu)*.
3. **Sinh tập tin Excel**:
   - Xác định thư mục `webRoot` (hoặc `wwwroot`).
   - Sinh đường dẫn file qua [`PortExcel.GenExcelExportFilePath`](../../Data/PortExcel.cs) với tiền tố `"Mst_Department"`.
   - Ghi tiêu đề ra sheet `"Mst_Department"` qua [`PortExcel.ExportToExcelFromList`](../../Data/PortExcel.cs).
4. **Cơ chế bắt ngoại lệ bất ngờ**:
   - Bọc toàn bộ logic trong `try/catch (Exception ex)`. Nếu xảy ra lỗi I/O hoặc ngoại lệ bất ngờ, trả về HTTP 400 kèm mã lỗi `ErridnLoyalty.Mst_Department_ExportTemplate` với `Params.Exception = ex.Message` (`gate :836-840`).
5. **Trả về kết quả thành công**:
   - Trả về HTTP 200 `{ Success = true, Data = { Url = baseAddress + virtualPath } }` (`gate :829-833`).

#### Bảng mã lỗi `ErridnLoyalty.*`

| Mã lỗi | Điều kiện kích hoạt | Các tham số trả về trong `Params` |
|---|---|---|
| `ErridnLoyalty.Mst_Department_ExportTemplate` | Ngoại lệ hệ thống hoặc lỗi I/O bất ngờ khi tạo tập tin mẫu Excel | `Exception = <thông báo ngoại lệ>` |

#### Ví dụ Request / Response thực tế (từ lần chạy VERIFY #131)
- Request: `POST /api/mst_department/exporttemplate`
- Response (HTTP 200):
  ```json
  {
    "Success": true,
    "Data": {
      "Url": "http://localhost:5000/TempFiles/2026/10/16/Mst_Department_20261016120000_yyyy.xlsx"
    }
  }
  ```
- Kết quả kiểm chứng OpenXML tập tin `.xlsx`: Tên sheet `"Mst_Department"`, đúng 3 cột tiêu đề (`Tên phòng ban (*)`, `Phòng ban cấp trên`, `Mô tả`), không có dòng dữ liệu nào.

---

## 3. Khác biệt So với Nguồn & Danh sách GAP (Known Gaps)

Tuân thủ nguyên tắc trung thực tuyệt đối, dưới đây là bảng liệt kê toàn bộ các điểm khác biệt, giả định và các thành phần chưa port (GAP) giữa hệ thống đích và hệ thống nguồn:

| Hạng mục | Trạng thái ở Nguồn | Triển khai ở Đích | Phân loại & Ghi chú GAP |
|---|---|---|---|
| **Xác thực & Phân quyền** | Gate kiểm tra `CheckGatewayAuthentication`, `CheckAuthorize`; Biz kiểm tra `Sys_User_CheckAuthorize`, `Sys_Access_CheckDenyV30`. | Bỏ qua toàn bộ kiểm tra phân quyền do hệ thống đích chưa có phân hệ User/Role. | Bỏ qua có chủ đích (**Loại Đ**). |
| **Ghi nhật ký hệ thống** | Ghi bản ghi `RequestLog` và `ReturnLog` vào CSDL cho mọi thao tác (`Master.cs`). | Bỏ qua ghi log DB. | Bỏ qua có chủ đích (**Loại Đ**). |
| **Phiên người dùng đăng nhập** | Trích xuất `OrgID`, `MST`, `NetworkID`, `WAUserCode` từ session `orgUserUI` và quyền `drAbilityOfUser`. | Lấy `Biz_OrgID` và `Biz_NetworkID` (mặc định `"1001"`) từ cấu hình `IConfiguration`; `MST` tra cứu từ `Mst_Org`; `LogLUBy` gán cứng `"port"`. | **GAP**: Khi chưa cấu hình `Biz_OrgID`, `GetAllActive` sẽ trả về dữ liệu của mọi tổ chức. |
| **Khai báo DDL nguồn** | Không có tập tin `.sql` tạo bảng `Mst_Department` trong mã nguồn gốc. | Tự suy diễn kiểu cột từ model và code biz. Khóa chính `DepartmentCode` là khóa draft. `DepartmentLevel` đặt kiểu `int?`. | **GAP / Cần xác nhận**: Cần đối chiếu với DDL thực tế trên môi trường production của nguồn. |
| **Đếm nhân viên (`ss_QtyStaff`)** | `LEFT JOIN` với bảng `Staff_MapDepartment` để đếm số nhân viên (`Master.cs:30490-30499`). | Tạm thời gán cố định `ss_QtyStaff = 0` cho mọi bản ghi. | **GAP**: Bảng `Staff_MapDepartment` chưa có trong repo đích. Cần cập nhật hàm đếm khi port bảng này. |
| **Chặn xóa khi có nhân viên** | Kiểm tra nếu phòng ban còn nhân viên trong `Staff_MapDepartment` thì ném lỗi `Mst_Department_DeleteX_ExistStaff_MapDepartment`. | Không chặn xóa khi có nhân viên. | **GAP**: Chưa có bảng `Staff_MapDepartment` để kiểm tra ràng buộc. |
| **Khóa ngoại Train_CourseDepartment** | Có FK từ `Train_CourseDepartment.DepartmentCode` trỏ tới `Mst_Department.DepartmentCode`. | Không kiểm tra ràng buộc khóa ngoại này khi xóa. | **GAP**: Module đào tạo (`Train_CourseDepartment`) chưa được port. |
| **Gốc phân cấp BU ('HO')** | Gốc `'HO'` được viết cứng trong mã nguồn biz (`Master.cs:29416`). | Giữ nguyên gốc `'HO'` viết cứng khi thực hiện `UpdBUAsync`. | Khớp nguồn 1-1. |
| **Không đổi cha khi Update** | Gate gửi `DepartmentCodeParent` trong `Ft_Cols_Upd` nhưng Biz bỏ qua. | Giữ nguyên hành vi nguồn: Không cập nhật trường `DepartmentCodeParent` khi gọi update. | Khớp nguồn 1-1. |
| **Không kiểm tra con khi Xóa** | Nguồn không chặn xóa phòng ban cha khi còn con; không gọi `UpdBU` khi xóa. | Giữ nguyên hành vi nguồn: Phòng ban con vẫn giữ mã cha đã xóa; không gọi `UpdBU`. | Khớp nguồn 1-1. |
| **Định dạng file Excel Import** | Thư viện nguồn `ExcelImportNew` nằm trong DLL đóng gói không có mã nguồn. | Giả định dòng 1 là tiêu đề, dữ liệu bắt đầu từ ô `A2`, thứ tự 3 cột: `DepartmentName`, `DepartmentCodeParent`, `DepartmentDesc`. | **Giả định**: Kế thừa từ hạ tầng [`PortExcel`](../../Data/PortExcel.cs), cần xác nhận với file mẫu thực tế. |

---

## 4. Nhật ký Thay đổi & Đối chiếu Tiến độ (Changelog)

Các hạng mục API của `Mst_DepartmentController` được chuyển đổi tuần tự theo sổ theo dõi tiến độ [`PARITY_PROGRESS.md`](../../PARITY_PROGRESS.md) và lịch sử Git:

| STT Hạng mục | Tên Endpoint & Action | Trạng thái | Mã Commit Git | Mô tả tóm tắt thay đổi |
|---|---|---|---|---|
| **#122** | `Mst_DepartmentController/Search` | Đã hoàn thành | `ec584e6` | Khởi tạo model [`Mst_Department.cs`](../../Models/Port/Mst_Department.cs), cấu hình EF Core, Seeder 4 bản ghi demo và action tìm kiếm phân trang. |
| **#123** | `Mst_DepartmentController/GetAllActive` | Đã hoàn thành | `46644d1` | Bổ sung endpoint lấy toàn bộ danh sách phòng ban hoạt động (`FlagActive = '1'`) theo tổ chức, trả về mảng phẳng. |
| **#124** | `Mst_DepartmentController/GetByDepartmentCode` | Đã hoàn thành | `5c1288c` | Bổ sung endpoint tra cứu chi tiết 1 phòng ban theo mã, bỏ qua lọc tổ chức và trạng thái. |
| **#125** | `Mst_DepartmentController/Create` | Đã hoàn thành | `9b5c7cb` | Tạo mới phòng ban, tích hợp sinh sequence `Seq_DepartmentCode` độc lập, kiểm tra NNT, kiểm tra cha, kiểm tra trùng tên và tự động chạy `UpdBUAsync`. |
| **#126** | `Mst_DepartmentController/Update` | Đã hoàn thành | `7d67b30` | Cập nhật thông tin phòng ban, bảo lưu phòng ban cha, kiểm tra đổi tên phân biệt hoa thường vấp chính nó. |
| **#127** | `Mst_DepartmentController/Delete` | Đã hoàn thành | `d565dda` | Xóa đơn lẻ phòng ban, kiểm tra rỗng tại gate cho cả mã và OrgID, không kiểm tra phòng ban con. |
| **#128** | `Mst_DepartmentController/DeleteMultiple` | Đã hoàn thành | `9de58bd` | Xóa danh sách phòng ban, lặp tuần tự từng transaction riêng biệt, thực thi cơ chế xóa một phần khi có lỗi. |
| **#129** | `Mst_DepartmentController/Import` | Đã hoàn thành | `e3c84ce` | Nhập danh mục từ Excel, validate toàn bộ dòng trước khi tạo, sinh mã GUID tự động và thực thi cơ chế nhập một phần. |
| **#130** | `Mst_DepartmentController/Export` | Đã hoàn thành | `2d13be5` | Xuất danh sách phòng ban ra Excel với từ điển 4 cột theo thứ tự nguồn, lọc OrgID/KeyWord/FlagActive. |
| **#131** | `Mst_DepartmentController/ExportTemplate` | Đã hoàn thành | `a2d6f65` | Xuất tập tin Excel mẫu chứa 3 cột tiêu đề phục vụ nhập dữ liệu (`DepartmentName`, `DepartmentCodeParent`, `DepartmentDesc`). |
| **Fix** | Sửa lỗi ngoại lệ DB bất ngờ | Đã hoàn thành | `1752274` | Import, Delete, DeleteMultiple, Export bắt mọi ngoại lệ và trả về HTTP 400 PortResult.Fail với Params.Exception thay vì HTTP 500, khớp nguồn 1-1. |
