---
Phạm vi: Controllers/Port/Rpt_PeriodSummaryControllerPort.cs, Models/Port/Rpt_PeriodSummary.cs
Cập nhật: 2026-10-18
Phiên bản mã: d30cdab
Trạng thái: Nháp — chờ review
---

# Tài liệu API: Rpt_PeriodSummaryController

## 1. Tóm tắt

API quản lý báo cáo đối soát tổng hợp kỳ và chi tiết giao dịch liên quan (`Rpt_PeriodSummary`), hỗ trợ các nghiệp vụ tra cứu dữ liệu tổng hợp theo kỳ/đại lý, tra cứu chi tiết đối soát, tra cứu giao dịch thẻ, giao dịch đặc cách, giao dịch giảm điểm, cùng các tính năng xuất báo cáo ra file Excel đơn sheet và multi-sheet.

- **Bảng nguồn**: 
  - Gate: `13.ClientGate\V10\idn.Loyalty.WebAPI\Controllers\Rpt_PeriodSummaryController.cs`
  - Service: `13.ClientGate\V10\idn.Loyalty.ClientService\Services\Loyalty\Rpt_PeriodSummaryService.cs`
  - API: `11.BackEnd\V10\idn.Loyalty.Biz.Web\Controllers\ReportController.cs`, `CrdCardTransactionController.cs`
  - Biz: `11.BackEnd\V10\idn.Loyalty.Biz\Report.cs`, `Master.Loyalty.cs`
  - DDL / SQL: `Rpt_PeriodSummary.sql`
- **Bảng cơ sở dữ liệu (`Rpt_PeriodSummary`)**:
  - `PeriodMonth`: Kiểu `string` (EF), Khóa chính phần 1, Tháng đối soát.
  - `DLCPCode`: Kiểu `string` (EF), Khóa chính phần 2, Mã đại lý giao dịch.
  - `NetworkID`: Kiểu `string?` (EF), Mã Network.
  - `OrgID`: Kiểu `string?` (EF), Mã Org.
  - `ACol`: Kiểu `double?` (EF), Số điểm tiêu dùng Hội viên tích được từ Đại lý trong kỳ.
  - `BCol`: Kiểu `double?` (EF), Số điểm Hội viên sử dụng tại Đại lý trong kỳ.
  - `CCol`: Kiểu `double?` (EF), Số điểm Đại lý chuyển vào quỹ chung vì đặc cách hội viên.
  - `DCol`: Kiểu `double?` (EF), Số điểm Đại lý được nhận từ quỹ chung theo hội viên đặc cách ĐL khác.
  - `ECol`: Kiểu `double?` (EF), Số điểm Đại lý được nhận từ quỹ chung theo hội viên đặc cách từ HTV.
  - `FCol`: Kiểu `double?` (EF), Tổng nộp vào quỹ chung.
  - `GCol`: Kiểu `double?` (EF), Số điểm Quỹ chung chuyển cho ĐL.
  - `HCol`: Kiểu `double?` (EF), Tổng số điểm hết hạn của tháng đối soát (tính lại = HCol + LCol).
  - `ICol`: Kiểu `double?` (EF), Tổng số điểm hết hạn lũy kế tính đến tháng đối soát (tính tất cả).
  - `JCol`: Kiểu `double?` (EF), Tổng số điểm hết hạn lũy kế tính đến tháng đối soát (chỉ tính lũy kế trong 1 năm).
  - `KCol`: Kiểu `double?` (EF), Điểm tiêu dùng của hội viên hủy trong kỳ.
  - `K1Col`: Kiểu `double?` (EF), Số điểm ĐL chuyển vào quỹ hội viên theo Dịch vụ ưu đãi do Bán hạng HV từ ĐL khác.
  - `K2Col`: Kiểu `double?` (EF), Số điểm ĐL nhận được từ quỹ hội viên theo Dịch vụ ưu đãi do Bán hạng HV từ ĐL khác.
  - `LCol`: Kiểu `double?` (EF), Tổng số điểm hết hạn của tháng đối soát.
  - `PCol`: Kiểu `double?` (EF), Tổng số điểm ĐL chuyển vào quỹ HV.
  - `QCol`: Kiểu `double?` (EF), Tổng số điểm quỹ HV chuyển cho ĐL.
  - `LogLUDTimeUTC`: Kiểu `DateTime?` (EF), Thời gian cập nhật cuối UTC.
  - `LogLUBy`: Kiểu `string?` (EF), Người cập nhật cuối.

---

## 2. Các Endpoint

### 2.1. Search (Tra cứu báo cáo đối soát tổng hợp kỳ)
- **Route & Method**: `POST /api/rpt_periodsummary/search`
- **Mã nguồn đích**: `Controllers/Port/Rpt_PeriodSummaryControllerPort.cs:98`

**Bảng tham số Request (`Rpt_PeriodSummarySearchReq`):**
| Tên tham số | Kiểu dữ liệu | Bắt buộc? | Mặc định | Ý nghĩa |
| :--- | :--- | :--- | :--- | :--- |
| `DLCPCode` | string? | Không | null | Mã đại lý giao dịch |
| `PeriodMonth` | string? | Không | null | Tháng đối soát |

**Quy tắc xử lý:**
1. Lọc dữ liệu theo `DLCPCode` và `PeriodMonth` sử dụng toán tử `=` (nguyên chuỗi, không phân biệt hoa thường).
2. Sắp xếp kết quả giảm dần theo `LogLUDTimeUTC`.
3. Tính toán nghiệp vụ cột `HCol = (HCol ?? 0) + (LCol ?? 0)`.
4. Trả về kết quả bọc trong đối tượng dữ liệu; nếu danh sách rỗng, `Lst_Rpt_PeriodSummary` trả về `null`.

**Mã lỗi:**
Chưa xac minh

**Ví dụ Request:**
```json
{
  "DLCPCode": "DL01",
  "PeriodMonth": "2026-03"
}
 {
  "DLCPCode": "DL01",
  "PeriodMonth": "2026-03"
}
 {
  "DLCPCode": "DL01",
  "PeriodMonth": "2026-03"
}
 {
  "DLCPCode": "DL01",
  "PeriodMonth": "2026-03"
}
 {
  "PeriodMonth": "2026-03",
  "DLCodeExceptionally": "DL02"
}
 {
  "DLCode": "DL01",
  "DealPointType": "CONSUMPTION",
  "CreateDTimeUTCFrom": "2026-03-01",
  "CreateDTimeUTCTo": "2026-03-31"
}
 {
  "DLCPCode": "DL01",
  "PeriodMonth": "2026-03"
}
 {
  "DLCPCode": "DL01",
  "PeriodMonth": "2026-03"
}
 {
  "DLCPCode": "DL01",
  "PeriodMonth": "2026-03"
}
 {
  "PeriodMonth": "2026-03",
  "DLCodeExceptionally": "DL02"
}
 {
  "DLCode": "DL01",
  "DealPointType": "CONSUMPTION"
}
 {
  "DLCPCode": "DL01",
  "PeriodMonth": "2026-03"
}
 {
  "DLCPCode": "DL01",
  "PeriodMonth": "2026-03"
}
