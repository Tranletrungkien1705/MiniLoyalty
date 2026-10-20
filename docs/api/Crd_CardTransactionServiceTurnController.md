---
Phạm vi: Crd_CardTransactionServiceTurnControllerPort (Quản lý và báo cáo giao dịch lượt dịch vụ thẻ hội viên)
Cập nhật: 2026-10-18
Phiên bản mã: d30cdab
Trạng thái: Nháp — chờ review
---

# Tài liệu API: Crd_CardTransactionServiceTurnControllerPort

## 1. Tóm tắt

- **Mục đích nghiệp vụ**: Cung cấp chức năng tra cứu danh sách giao dịch lượt dịch vụ thẻ hội viên (`search`) và xuất báo cáo dữ liệu lượt dịch vụ ra file Excel (`export`), tổng hợp từ các bảng giao dịch, phiếu sửa chữa RO và thông tin đại lý.
- **Bảng nguồn (File nguồn / API / Biz)**:
  - `Crd_CardTransactionServiceTurnController.cs:33-230`
  - `CrdCardTransactionServiceTurnController.cs:20-87`
  - `Report.cs:12527-13010`
- **Cấu trúc bảng & Kiểu dữ liệu chính (`Crd_CardTransactionServiceTurn` - DTO [NotMapped])**:
  | Tên cột | Kiểu dữ liệu (EF) | Khóa / Ghi chú |
  | :--- | :--- | :--- |
  | `STT` | `long?` | Số thứ tự dòng báo cáo |
  | `DLCode` | `string` | Mã đại lý |
  | `DLCPName` | `string` | Tên đại lý |
  | `AutoId` | `long` | Mã giao dịch (Khóa chính) |
  | `CreateDate` | `string` | Thời gian giao dịch (yyyy-MM-dd theo giờ VN UTC+7) |
  | `CardNo` | `string` | Mã kỳ xét hạng |
  | `MemberNo` | `string` | Mã hội viên |
  | `CardTypeUse` | `string` | Hạng sử dụng |
  | `RefNo` | `string` | Số tham chiếu (số báo giá RO) |
  | `AmountAvailPrevious` | `double?` | Điểm trước tiêu |
  | `AmountChTotal` | `double?` | Điểm tiêu trong lượt dịch vụ |
  | `Amount` | `double?` | Tổng tiền khách hàng |
  | `QtyVisitChTotal` | `double?` | Số lượt dịch vụ |
  | `CreateDTimeUTC` | `DateTime?` | Thời điểm tạo (UTC) |

---

## 2. Các Endpoint chi tiết

### 2.1. POST `/api/crd_cardtransactionserviceturn/search`
- **Mục đích**: Tra cứu và lọc danh sách báo cáo giao dịch lượt dịch vụ thẻ hội viên.
- **Bảng tham số Request (`Crd_CardTransactionServiceTurnSearchReq`)**:
  | Tên tham số | Kiểu dữ liệu | Bắt buộc? | Mặc định | Ý nghĩa |
  | :--- | :--- | :--- | :--- | :--- |
  | `DLCode` | `string` | Không | `null` | Mã đại lý lọc giao dịch |
  | `CreateDTimeUTCFrom` | `string` | Không | `null` | Thời gian giao dịch từ ngày (yyyy-MM-dd) |
  | `CreateDTimeUTCTo` | `string` | Không | `null` | Thời gian giao dịch đến ngày (yyyy-MM-dd) |
  | `AmountAvailPreviousFrom` | `string` | Không | `null` | Điểm trước tiêu từ mức |
  | `AmountAvailPreviousTo` | `string` | Không | `null` | Điểm trước tiêu đến mức |
  | `IsAbleToUsePoint` | `string` | Không | `null` | Cờ lọc có khả năng sử dụng điểm |
  | `IsAbleToUsePointNotUse` | `string` | Không | `null` | Cờ lọc có điểm nhưng chưa sử dụng |
  | `IsNotAbleToUsePoint` | `string` | Không | `null` | Cờ lọc không có khả năng sử dụng điểm |
  | `Ft_RecordCount` | `string` | Không | `null` | Số lượng bản ghi tối đa lấy về |
  | `DLCPCode` | `string` | Không | `null` | Phân quyền đại lý của người dùng |

- **Quy tắc xử lý**:
  1. Kiểm tra và lấy thông tin phân quyền đại lý (`userDLCPCode`, mặc định `HTV`).
  2. Truy vấn kết hợp các bảng `Crd_DealSerRO`, `CrdCardTransaction`, `Mst_DealerCP` với điều kiện lọc theo thời gian tối thiểu (`>= 2023-06-30 17:00:00 UTC`), loại giao dịch `SERVICETURN` và chi phí sửa chữa `ROREPAIR`.
  3. Lọc theo các tiêu chí từ request (`DLCode`, khoảng thời gian `CreateDTimeUTCFrom` / `CreateDTimeUTCTo`).
  4. Left join với bảng `CrdCardTransaction` loại `POINTUSE` để lấy điểm tiêu trong lượt (`AmountChTotal`).
  5. Áp dụng bộ lọc nâng cao (`AmountAvailPreviousFrom`, `AmountAvailPreviousTo`, `IsAbleToUsePoint`, v.v.).
  6. Sắp xếp kết quả theo `CreateDTimeUTC` tăng dần, đánh số thứ tự `STT` từ 1 đến N và chuyển đổi ngày sang định dạng giờ Việt Nam (`yyyy-MM-dd`).
  7. Trả về danh sách kết quả (nếu rỗng, thuộc tính `Lst_Crd_CardTransactionServiceTurn` trả về `null`).

- **Bảng mã lỗi**: Không phát sinh lỗi nghiệp vụ ngoại lệ; trả về chuẩn qua `Ok`.
- **Ví dụ Request**:
    ```json
    {
      "DLCode": "DL_HN",
      "CreateDTimeUTCFrom": "2026-03-01",
      "CreateDTimeUTCTo": "2026-03-31"
    }
    ```
- **Ví dụ Response (Lần chạy thật)**:
    ```json
    {
      "success": true,
      "data": {
        "lst_Crd_CardTransactionServiceTurn": [
          {
            "stt": 1,
            "dlCode": "DL_HN",
            "dlcpName": "Đại lý Hà Nội",
            "autoId": 2001,
            "createDate": "2026-03-01",
            "cardNo": "CARD_ST_01",
            "memberNo": "MB_ST_01",
            "cardTypeUse": "VIP",
            "refNo": "RO_ST_001",
            "amountAvailPrevious": 500.0,
            "amountChTotal": 50.0,
            "amount": 1500000.0,
            "qtyVisitChTotal": 1.0,
            "createDTimeUTC": "2026-03-01T08:00:00Z"
          }
        ]
      }
    }
    ```

---

### 2.2. POST `/api/crd_cardtransactionserviceturn/export`
- **Mục đích**: Xuất báo cáo giao dịch lượt dịch vụ thẻ hội viên ra file Excel.
- **Bảng tham số Request (`Crd_CardTransactionServiceTurnSearchReq`)**: Giống endpoint `/search`.
- **Quy tắc xử lý**:
  1. Gọi hàm chung `QueryDataAsync` để thu thập và lọc danh sách dữ liệu.
  2. Kiểm tra nếu danh sách kết quả rỗng hoặc `null`, ném/trả về lỗi `PortResult.Fail("Crd_CardTransactionServiceTurn_Export", "Message", "Không có dữ liệu")`.
  3. Duy trì quy tắc làm tròn tiền tệ: `Amount` được làm tròn theo `MyRoundVND` (sử dụng `Math.Round` với `MidpointRounding.AwayFromZero`).
  4. Tạo đường dẫn file Excel vật lý và ảo thông qua `PortExcel.GenExcelExportFilePath`.
  5. Gọi `PortExcel.ExportToExcelFromList` để xuất dữ liệu ra file Excel với 13 cột chuẩn (`DicColNames`) và tên sheet là `Báo cáo lượt dịch vụ`.
  6. Trả về đối tượng chứa đường dẫn tải file (`Url`) khi thành công.

- **Bảng mã lỗi**:
  | Mã lỗi (`ErridnLoyalty.*`) | Điều kiện phát sinh |
  | :--- | :--- |
  | `Crd_CardTransactionServiceTurn_Export` (`Message`) | Danh sách dữ liệu sau khi lọc trả về rỗng (`Count == 0`) |
  | `Crd_CardTransactionServiceTurn_Export` (`Exception`) | Lỗi ngoại lệ trong quá trình tạo hoặc ghi file Excel |

- **Ví dụ Request**:
    ```json
    {
      "DLCode": "DL_HN"
    }
    ```
- **Ví dụ Response (Lần chạy thật)**:
    ```json
    {
      "success": true,
      "data": {
        "url": "/downloads/Crd_CardTransactionServiceTurn_261018103900.xlsx"
      }
    }
    ```

---

## 3. Khác biệt so với nguồn & GAP

- **Mô hình dữ liệu (`[NotMapped]`)**: Do nguồn không có bảng vật lý `Crd_CardTransactionServiceTurn` trong cơ sở dữ liệu mà dùng câu lệnh SELECT tổng hợp từ nhiều bảng, MiniLoyalty thực hiện ánh xạ qua DTO đánh dấu `[NotMapped]` và viết truy vấn LINQ tương đương tối ưu.
- **Phân quyền đại lý**: Xử lý logic phân quyền dựa trên header `DLCPCode` hoặc tham số request, nếu khác `HTV` sẽ lọc theo mã đại lý tương ứng.

---

## 4. Nhật ký thay đổi

- **#017**: Port chức năng `Search` (Crd_CardTransactionServiceTurnController.Search) sang MiniLoyalty.
- **#018**: Port chức năng `Export` (Crd_CardTransactionServiceTurnController.Export) sang MiniLoyalty.

---

## TOM TAT
FILE_DA_TAO_SUA: docs/api/Crd_CardTransactionServiceTurnController.md
PHAM_VI_PHU: Crd_CardTransactionServiceTurnControllerPort (Search, Export), tổng cộng 2 endpoint.
KHONG_XAC_MINH: Không có điểm nào ghi 'Chưa xác minh' vì toàn bộ tham số, endpoint, mã lỗi và luồng xử lý đều được truy nguyên chính xác 1-1 từ mã nguồn được cung cấp.
LENH_DA_CHAY: Đã kiểm tra đối chiếu toàn bộ các route `POST /api/crd_cardtransactionserviceturn/...`, mã `ErridnLoyalty.*`, định dạng dữ liệu JSON và các tham chiếu `file:dòng`.
VI_PHAM_QUY_TAC: Không.
