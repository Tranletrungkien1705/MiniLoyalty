Phạm vi: Controllers/Port/Prm_CarNewControllerPort.cs, Models/Port/Prm_CarNew.cs
Cập nhật: 2026-10-16
Phiên bản mã: eb96793
Trạng thái: Nháp — chờ review

# Prm_CarNewController API

## 1. Tóm tắt
- **Mục đích nghiệp vụ:** Cung cấp chức năng tìm kiếm, phân trang và lọc danh sách các chương trình khuyến mãi xe mới (`Prm_CarNew`) theo tổ chức, mã đại lý, mã hệ thống chương trình, mã chương trình, trạng thái và khoảng thời gian tạo hoặc cờ hiển thị hiệu lực (`FlagShow`).
- **Bảng nguồn:** Gate `Prm_CarNewController.cs` (các hàm tìm kiếm, `strWhereClause_Prm_CarNew` từ dòng 712-786, logic hiển thị `FlagShow` từ dòng 812-813 và biz :301).
- **Bảng cấu trúc / Thực thể liên quan:**
  - `Prm_CarNew`: Bảng chính chứa thông tin chương trình khuyến mãi xe mới.
    - `PRMCNCodeSys` (string, khóa chính, bắt buộc): Mã hệ thống chương trình khuyến mãi.
    - `NetworkID` (string, bắt buộc): Mã mạng lưới.
    - `OrgID` (string, bắt buộc): Mã tổ chức.
    - `DLCPCode` (string, bắt buộc): Mã đại lý / chính sách đại lý.
    - `PRMCNCode` (string, bắt buộc): Mã chương trình khuyến mãi.
    - `PRMCNName` (string, không bắt buộc): Tên chương trình khuyến mãi.
    - `EffDateStart` (string, bắt buộc): Ngày bắt đầu hiệu lực (định dạng chuỗi).
    - `EffDateEnd` (string, bắt buộc): Ngày kết thúc hiệu lực (định dạng chuỗi).
    - `FlagAllModel` (string, không bắt buộc): Cờ áp dụng cho tất cả các model.
    - `PointValAllModel` (double, không bắt buộc): Giá trị điểm áp dụng cho tất cả model.
    - `CreateDTimeUTC` (DateTime, bắt buộc): Thời điểm tạo (UTC).
    - `CreateBy` (string, bắt buộc): Người tạo.
    - `LUDTimeUTC` (DateTime, bắt buộc): Thời điểm cập nhật cuối (UTC).
    - `LUBy` (string, bắt buộc): Người cập nhật cuối.
    - `ApprDTimeUTC` (DateTime, không bắt buộc): Thời điểm duyệt (UTC).
    - `ApprBy` (string, không bắt buộc): Người duyệt.
    - `FinishDTimeUTC` (DateTime, không bắt buộc): Thời điểm kết thúc (UTC).
    - `FinishBy` (string, không bắt buộc): Người kết thúc.
    - `CancelDTimeUTC` (DateTime, không bắt buộc): Thời điểm hủy (UTC).
    - `CancelBy` (string, không bắt buộc): Người hủy.
    - `PRMCNStatus` (string, không bắt buộc): Trạng thái chương trình (VD: `FINISH`, `NEW`).
    - `Remark` (string, không bắt buộc): Ghi chú.
    - `LogLUDTimeUTC` (DateTime, không bắt buộc): Thời điểm log UTC.
    - `LogLUBy` (string, không bắt buộc): Người log.
    - `MyIdxSeq` (long, không ánh xạ cơ sở dữ liệu - NotMapped): Số thứ tự dòng trong trang.
    - `FlagShow` (string, không ánh xạ cơ sở dữ liệu - NotMapped): Cờ hiển thị trạng thái hiệu lực theo thời gian thực.
  - `Prm_CarNewSpec`: Bảng đặc tả chi tiết model theo chương trình khuyến mãi (`PRMCNCodeSys`, `Idx`, `ModelCode` là khóa chính).
  - `Prm_CarNewDtl`: Bảng chi tiết điểm số / điều kiện theo chương trình (`PRMCNCodeSys`, `Idx` là khóa chính).

## 2. Các Endpoint

### POST /api/prm_carnew/search
Tìm kiếm và phân trang danh sách chương trình khuyến mãi xe mới theo các điều kiện lọc.

#### Tham số Request
Body kiểu JSON (`Prm_CarNewSearchReq`):
- `OrgID` (string, không bắt buộc, mặc định: `null`): Lọc theo mã tổ chức.
- `KeyWord` (string, không bắt buộc, mặc định: `null`): Từ khóa tìm kiếm (Lưu ý: Theo gate nguồn `Prm_CarNewController.cs:712-786`, điều kiện `KeyWord` không được sử dụng tại đây nên bị bỏ qua).
- `DLCPCode` (string, không bắt buộc, mặc định: `null`): Lọc theo mã đại lý (`DLCPCode`).
- `PRMCNCodeSys` (string, không bắt buộc, mặc định: `null`): Lọc theo mã hệ thống chương trình (`PRMCNCodeSys`).
- `CreateDTimeUTCFrom` (string, không bắt buộc, mặc định: `null`): Lọc thời gian tạo từ ngày (định dạng chuỗi ngày, ví dụ `yyyy-MM-dd`).
- `CreateDTimeUTCTo` (string, không bắt buộc, mặc định: `null`): Lọc thời gian tạo đến ngày (định dạng chuỗi ngày, ví dụ `yyyy-MM-dd`).
- `PRMCNCode` (string, không bắt buộc, mặc định: `null`): Lọc theo mã chương trình (`PRMCNCode`).
- `PRMCNStatus` (string, không bắt buộc, mặc định: `null`): Lọc theo trạng thái chương trình.
- `FlagShow` (string, không bắt buộc, mặc định: `null`): Lọc theo cờ hiển thị hiệu lực (`"1"` hoặc `"0"`).
- `Ft_PageIndex` (int, không bắt buộc, mặc định: `0`): Chỉ số trang hiện tại (bắt đầu từ 0).
- `Ft_PageSize` (int, không bắt buộc, mặc định: `10`): Số lượng bản ghi trên một trang.

#### Quy tắc xử lý theo thứ tự
1. Khởi tạo đối tượng request nếu giá trị truyền vào là `null` (`req ??= new Prm_CarNewSearchReq()`).
2. Lấy giá trị `Ft_PageIndex` và `Ft_PageSize` từ request.
3. Xây dựng câu truy vấn cơ bản trên bảng `Prm_CarNew`.
4. Áp dụng các điều kiện lọc tùy chọn nếu có giá trị (`OrgID`, `DLCPCode`, `PRMCNCodeSys`, `PRMCNCode`, `PRMCNStatus`) sử dụng phương thức so sánh không phân biệt hoa thường tương đương `LIKE` (`PortQuery.Equal` và `PortQuery.Esc`). Trường `KeyWord` bị bỏ qua theo logic nguồn.
5. Áp dụng bộ lọc thời gian tạo (`CreateDTimeUTCFrom` kết hợp với `00:00:00` và `CreateDTimeUTCTo` kết hợp với `23:59:59`) nếu chuyển đổi ngày tháng hợp lệ bằng `DateTime.TryParse`.
6. Xử lý bộ lọc `FlagShow`:
   - Lấy chuỗi ngày hiện tại theo UTC (`yyyy-MM-dd`) và theo múi giờ lệch +7 (`yyyy-MM-dd`).
   - Nếu `FlagShow == "1"`: lọc các bản ghi có `PRMCNStatus == "FINISH"` và ngày hiện tại nằm trong khoảng từ `EffDateStart` đến `EffDateEnd`.
   - Nếu `FlagShow == "0"`: lọc các bản ghi ngược lại (`PRMCNStatus != "FINISH"` hoặc không trong khoảng hiệu lực).
7. Sắp xếp danh sách kết quả giảm dần theo thời gian tạo (`CreateDTimeUTC`) và thực hiện truy vấn bất đồng bộ lấy toàn bộ danh sách phù hợp (`ToListAsync`).
8. Thực hiện phân trang thủ công trên danh sách dựa vào `pageIndex` và `pageSize`. Nếu `pageSize == 0`, trả về danh sách rỗng.
9. Duyệt qua danh sách các bản ghi trang hiện tại:
   - Tăng số thứ tự `MyIdxSeq`.
   - Cập nhật lại giá trị `FlagShow` cho từng item theo điều kiện thời gian thực tế (`todayStr`).
   - Nếu `EffDateEnd == "2100-01-01"`, gán lại giá trị `EffDateEnd = ""`.
10. Tính toán tổng số trang (`pageCount`) và đóng gói dữ liệu vào đối tượng phân trang `PageInfo<Prm_CarNew>`.
11. Trả về kết quả HTTP 200 kèm theo cấu trúc JSON `{ Success = true, Data = pageInfo }`.

#### Bảng Mã Lỗi
| Mã lỗi | Điều kiện phát sinh | Chi tiết / Tham số trả về |
| :--- | :--- | :--- |
| Chưa xac minh | Endpoint tìm kiếm sử dụng kiểu trả về tiêu chuẩn `Ok(...)` và không định nghĩa tường minh các mã lỗi ngoại lệ `ErridnLoyalty.*` trong thân controller. | Chưa xac minh |

#### Ví dụ Request / Response
- **Request:**
    POST /api/prm_carnew/search
    Content-Type: application/json

    {
      "OrgID": "ORG01",
      "PRMCNStatus": "FINISH",
      "Ft_PageIndex": 0,
      "Ft_PageSize": 10
    }

- **Response (Thành công):**
    Chưa có lần chạy mau

## 3. Khác biệt so với nguồn & GAP
- Theo mã nguồn gate gốc (`Prm_CarNewController.cs:712-786`), tham số `KeyWord` được khai báo nhưng không tham gia vào câu điều kiện `strWhereClause_Prm_CarNew`. Do đó, bản port giữ nguyên logic bỏ qua `KeyWord` này (tuân thủ LESSONS A.18).
- Bộ lọc `FlagShow` tại gate dùng `DateTime.UtcNow`, trong khi cột `FlagShow` trả về trong danh sách sử dụng `UtcNow+7` (biz :301).

## 4. Nhật ký thay đổi
- Port 1-1 controller `Prm_CarNewController` sang MiniLoyalty (phiên bản mã `eb96793`).

TOM TAT
FILE_DA_TAO_SUA: docs/api/Prm_CarNewController.md
PHAM_VI_PHU: Prm_CarNewControllerPort (1 endpoint: POST /api/prm_carnew/search)
KHONG_XAC_MINH: Các mã lỗi chi tiết ErridnLoyalty.* (do controller không định nghĩa mã lỗi tường minh ngoài chuẩn thành công HTTP 200); Ví dụ response thực tế (do môi trường WEB không chạy lệnh/chạy app trực tiếp).
LENH_DA_CHAY: Không chạy lệnh (Theo quy định môi trường WEB: KHÔNG chạy lệnh).
VI_PHAM_QUY_TAC: Khong
