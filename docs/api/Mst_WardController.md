# Tham chiếu API Mst_WardController (Port 1-1)

| Thông tin | Giá trị |
|---|---|
| **Phạm vi** | Tài liệu tham chiếu chi tiết 2 endpoint đã port 1-1 của Mst_WardController (#246, #247) |
| **Cập nhật** | 2026-10-16 |
| **Phiên bản mã** | `ed28f8f` |
| **Trạng thái** | Nháp — chờ review |

> [!NOTE]
> Toàn bộ các endpoint thuộc controller này áp dụng chuẩn hóa JSON PascalCase tự động qua `PortJsonResultFilter` và xử lý ngoại lệ tập trung qua `PortExceptionFilter` (chuyển đổi ngoại lệ chưa bắt thành HTTP 400 `PortResult.Fail`). Chi tiết xem tại [Kiến trúc Hạ tầng Dùng chung](../kien-truc/HA_TANG_CHUNG.md).

---

## 1. Tóm tắt Nghiệp vụ & Cấu trúc Dữ liệu

### 1.1. Mục đích Nghiệp vụ
API `Mst_WardController` quản lý danh mục phường/xã (`Mst_Ward`) trong hệ thống phân cấp hành chính 3 cấp (Tỉnh/Thành phố `Mst_Province` → Quận/Huyện `Mst_District` → Phường/Xã `Mst_Ward`) của MiniLoyalty. Module cung cấp chức năng lấy danh sách toàn bộ các phường/xã đang hoạt động, và tra cứu chi tiết danh sách phường/xã theo tỉnh/thành và quận/huyện.

### 1.2. Bảng Đối chiếu Nguồn Tra cứu (Source Code Mapping)

| Thành phần | Đường dẫn tập tin nguồn (`2022.7.Loyalty`) | Tập tin đích tương ứng (`MiniLoyalty`) |
|---|---|---|
| **Gate Controller** | `13.ClientGate\V10\idn.Loyalty.WebAPI\Controllers\Mst_WardController.cs:24-198` | [`Controllers/Port/Mst_WardControllerPort.cs`](../../Controllers/Port/Mst_WardControllerPort.cs) |
| **API Backend** | `11.BackEnd\V10\idn.Loyalty.Biz.Web\Controllers\MstWardController.cs:21-91` | Tích hợp trực tiếp vào [`Controllers/Port/Mst_WardControllerPort.cs`](../../Controllers/Port/Mst_WardControllerPort.cs) |
| **Nghiệp vụ (Biz)** | `11.BackEnd\V10\idn.Loyalty.Biz\Master.1.cs` (Get `:5848-5998`; GetX `:6000-6154`; WAS `:5755-5847`) | Tích hợp trực tiếp vào [`Controllers/Port/Mst_WardControllerPort.cs`](../../Controllers/Port/Mst_WardControllerPort.cs) |
| **Entity Model** | `12.Dev.Common\idn.Loyalty.Common\Models\CustomerCentrer\Mst_Ward.cs` (8 cột) | [`Models/Port/Mst_Ward.cs:29-39`](../../Models/Port/Mst_Ward.cs#L29-L39) |
| **Cấu hình EF** | Khóa ghép `(ProvinceCode, DistrictCode, WardCode)` | [`Models/Port/Mst_Ward.cs:42-53`](../../Models/Port/Mst_Ward.cs#L42-L53) |
| **Seeder Demo** | `Order => 17` (danh mục `Mst_*` dùng 10–99) | [`Models/Port/Mst_Ward.cs:63-81`](../../Models/Port/Mst_Ward.cs#L63-L81) |

### 1.3. Cấu trúc Bảng CSDL (`Mst_Ward`)

Bảng dữ liệu vật lý `Mst_Ward` gồm đúng 8 cột:

| Tên cột | Kiểu EF Core | Khóa | Cho phép NULL | Ghi chú & Ý nghĩa |
|---|---|---|---|---|
| `ProvinceCode` | `string` | **PK1** | Không | Mã tỉnh/thành phố trực thuộc. Khóa chính phần 1. |
| `DistrictCode` | `string` | **PK2** | Không | Mã quận/huyện trực thuộc. Khóa chính phần 2. |
| `WardCode` | `string` | **PK3** | Không | Mã phường/xã. Khóa chính phần 3. |
| `NetworkID` | `string?` | | Có | Mã mạng nghiệp vụ. Ở đích lấy cấu hình `Biz_NetworkID`. |
| `WardName` | `string?` | | Có | Tên phường/xã (chuẩn hóa qua `StdStr`). |
| `FlagActive` | `string?` | | Có | Cờ trạng thái hoạt động: `"1"` (Đang hoạt động), `"0"` (Ngừng hoạt động). |
| `LogLUDTimeUTC` | `DateTime?` | | Có | Thời điểm cập nhật cuối cùng theo chuẩn UTC. |
| `LogLUBy` | `string?` | | Có | Người thực hiện cập nhật cuối cùng (mặc định gán `"seed"` hoặc `"port"`). |

**Dữ liệu mẫu khởi tạo (Seeder - `Mst_WardSeeder`, Idempotent)**:
- Gồm 5 bản ghi demo khớp dữ liệu tỉnh/huyện:
  - Hà Nội: `HN/HN01/HN0101` (Phúc Xá, FlagActive `'1'`), `HN/HN01/HN0102` (Trúc Bạch, FlagActive `'1'`), `HN/HN02/HN0201` (Hàng Bạc, FlagActive `'1'`).
  - TP. Hồ Chí Minh: `HCM/HCM01/HCM0101` (Bến Nghé, FlagActive `'1'`).
  - Cần Thơ: `CT/CT01/CT0101` (Tân An, FlagActive `'0'`).
- Toàn bộ bản ghi gán `NetworkID = "6403834000"`, `LogLUBy = "seed"`.

---

## 2. Đặc tả Chi tiết Từng Endpoint API

---

### 2.1. GetAllActive (#246)

Lấy toàn bộ danh sách các phường/xã đang hoạt động (`FlagActive = '1'`).

- **Route**: `POST /api/mst_ward/getallactive`
- **Controller Action**: [`Mst_WardControllerPort.GetAllActive`](../../Controllers/Port/Mst_WardControllerPort.cs#L81)
- **Tham chiếu nguồn**: Gate `Mst_WardController.cs:24-84` → API `MstWardController.cs:21-91` → Biz `Master.1.cs:6000-6154`

#### Bảng tham số Request
Endpoint không nhận tham số Body (`gate :24`).

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. Thiết lập tham số phân trang: `Ft_RecordStart = 0`, `Ft_RecordCount = 123456000` (`gate :41-42, :59-60`).
2. Mệnh đề lọc qua `strWhereClause_Mst_Ward` (`gate :162-177`):
   - **FlagActive**: Lọc theo toán tử `=` với giá trị `"1"` (`gate :46, :173`).
   - **OrgID**: Gate nhận tham số nhưng hàm lọc không sử dụng → Không lọc theo `OrgID`.
3. Sắp xếp thứ tự: Sắp xếp theo `mo.WardCode ASC` (`Master.1.cs:6061`), select cuối sắp theo `t.MyIdxSeq ASC` (`Master.1.cs:6095-6100`).
4. Gán `MyIdxSeq = recordStart + i` (0-based identity).
5. Trả về mảng phẳng trần các thực thể `Mst_Ward`. Nếu danh sách rỗng trả về `Data = null` (`gate :43, :66-71`).

#### Bảng mã lỗi
Endpoint luôn trả về HTTP 200.

#### Ví dụ Request / Response thực tế (từ lần chạy VERIFY #246)
- Request: `POST /api/mst_ward/getallactive`
- Response (HTTP 200):
  ```json
  {
    "Success": true,
    "Data": [
      { "MyIdxSeq": 0, "WardCode": "HCM0101", "ProvinceCode": "HCM", "DistrictCode": "HCM01", "WardName": "Bến Nghé", "FlagActive": "1" },
      { "MyIdxSeq": 1, "WardCode": "HN0101", "ProvinceCode": "HN", "DistrictCode": "HN01", "WardName": "Phúc Xá", "FlagActive": "1" },
      { "MyIdxSeq": 2, "WardCode": "HN0102", "ProvinceCode": "HN", "DistrictCode": "HN01", "WardName": "Trúc Bạch", "FlagActive": "1" },
      { "MyIdxSeq": 3, "WardCode": "HN0201", "ProvinceCode": "HN", "DistrictCode": "HN02", "WardName": "Hàng Bạc", "FlagActive": "1" }
    ]
  }
  ```

---

### 2.2. GetWardDetail (#247)

Tra cứu chi tiết danh sách phường/xã theo tỉnh/thành và quận/huyện.

- **Route**: `POST /api/mst_ward/getwarddetail`
- **Controller Action**: [`Mst_WardControllerPort.GetWardDetail`](../../Controllers/Port/Mst_WardControllerPort.cs#L112)
- **Tham chiếu nguồn**: Gate `Mst_WardController.cs:86-158` → API `MstWardController.cs:21-91` → Biz `Master.1.cs:6000-6154`

#### Bảng tham số Request

Payload JSON gửi qua Request Body (`MstWardGetWardDetailReq`):

| Tên tham số | Kiểu dữ liệu | Bắt buộc | Mặc định | Ý nghĩa & Quy tắc |
|---|---|---|---|---|
| `ProvinceCode` | `string?` | Có | `null` | Mã tỉnh/thành phố cần tra cứu (`gate :86, :106, :114`). |
| `DistrictCode` | `string?` | Không | `null` | Mã quận/huyện trực thuộc (tùy chọn; nếu truyền sẽ lọc chính xác theo quận/huyện). |
| `FlagActive` | `string?` | Không | `null` | Cờ hoạt động. **Đặc biệt**: Nguồn gán `"1"` nếu rỗng (`:108-110`) nhưng dòng lọc `FlagActive` trong `strWhereClause_Mst_Ward_Detail` **bị comment** (`:192-195`) → Hoàn toàn không lọc theo `FlagActive`. |

#### Quy tắc xử lý theo thứ tự (Business Rules)
1. **Kiểm tra mã tỉnh rỗng**: Nếu `ProvinceCode` rỗng hoặc null, hệ thống bỏ qua khối truy vấn CSDL (`gate :106`), trả về ngay HTTP 200 với `{ "Success": true, "Data": { "Lst_Mst_Ward": null } }` (`gate :141-145`).
2. Mệnh đề lọc:
   - **ProvinceCode**: So sánh bằng chính xác `x.ProvinceCode == req.ProvinceCode`.
   - **DistrictCode**: Nếu có truyền `DistrictCode` thì lọc so sánh bằng `x.DistrictCode == req.DistrictCode`. Nếu rỗng/null thì không lọc (trả về toàn bộ các phường/xã thuộc tỉnh đó).
   - **FlagActive**: Không lọc (tái hiện trung thực dòng comment của nguồn; phường ngừng hoạt động `FlagActive = '0'` vẫn được lấy).
3. Sắp xếp và định dạng kết quả: Sắp xếp theo `WardCode ASC`, bọc trong đối tượng `{ "Lst_Mst_Ward": rows }`. Nếu không có bản ghi nào thì `Lst_Mst_Ward = null`.

#### Bảng mã lỗi
Endpoint luôn trả về HTTP 200.

#### Ví dụ Request / Response thực tế (từ lần chạy VERIFY #247)
- **Tra cứu theo cả tỉnh và huyện (`HN/HN01`)**:
  - Request: `POST /api/mst_ward/getwarddetail` kèm `{"ProvinceCode": "HN", "DistrictCode": "HN01"}`
  - Response (HTTP 200):
    ```json
    {
      "Success": true,
      "Data": {
        "Lst_Mst_Ward": [
          { "MyIdxSeq": 0, "WardCode": "HN0101", "ProvinceCode": "HN", "DistrictCode": "HN01", "WardName": "Phúc Xá", "FlagActive": "1" },
          { "MyIdxSeq": 1, "WardCode": "HN0102", "ProvinceCode": "HN", "DistrictCode": "HN01", "WardName": "Trúc Bạch", "FlagActive": "1" }
        ]
      }
    }
    ```
- **Tra cứu chỉ theo tỉnh (`HN`, bỏ qua huyện)**:
  - Request: `POST /api/mst_ward/getwarddetail` kèm `{"ProvinceCode": "HN"}`
  - Response (HTTP 200): Trả về cả 3 phường thuộc Hà Nội (`HN0101`, `HN0102`, `HN0201`).
- **Tra cứu bản ghi ngừng hoạt động (`CT/CT01`)**:
  - Request: `POST /api/mst_ward/getwarddetail` kèm `{"ProvinceCode": "CT", "DistrictCode": "CT01"}`
  - Response (HTTP 200): Trả về phường `CT0101` (Tân An) dù có `FlagActive = '0'` (do nguồn comment mệnh đề lọc cờ).
- **Trường hợp thiếu ProvinceCode (`{}`)**:
  - Request: `POST /api/mst_ward/getwarddetail` kèm `{}`
  - Response (HTTP 200):
    ```json
    {
      "Success": true,
      "Data": {
        "Lst_Mst_Ward": null
      }
    }
    ```

---

## 3. Khác biệt so với Nguồn & GAP

1. **Khóa chính ghép**: Bảng `Mst_Ward` sử dụng khóa chính ghép 3 cột `(ProvinceCode, DistrictCode, WardCode)` được suy luận chuẩn xác theo mô hình của `Mst_District`.
2. **Hành vi đặc thù bỏ qua lọc FlagActive của GetWardDetail**: Nguồn gán cờ mặc định `FlagActive = "1"` tại Gate (`:108-110`) nhưng trong hàm tạo điều kiện `strWhereClause_Mst_Ward_Detail` (`:192-195`) dòng lọc này đã bị comment vô hiệu hóa. MiniLoyalty bảo lưu 1-1 hành vi này.
3. **Phân quyền & Kiểm soát truy cập**: Bỏ qua các kiểm tra phân quyền `CheckGatewayAuthentication`, `CheckAuthorize`, `Sys_User_CheckAuthorize` và ghi log `RequestLog`/`ReturnLog`.

---

## 4. Nhật ký Thay đổi

| Hạng mục (#) | Action / Endpoint | Commit Hash | Ngày hoàn thành | Ghi chú |
|---|---|---|---|---|
| #246 | `GetAllActive` | `3aa4d9d` | 2026-10-16 | Khởi tạo bảng `Mst_Ward`, seeder demo và endpoint lấy danh sách phường/xã đang hoạt động |
| #247 | `GetWardDetail` | `4419e9d` | 2026-10-16 | Endpoint tra cứu chi tiết phường/xã theo tỉnh và huyện, tái hiện hành vi bọc Lst_Mst_Ward |
