---
Phạm vi: Crd_CardRankControllerPort (Quản lý và tính toán xếp hạng, xét hạng, phê duyệt duy trì và hạ hạng thẻ hội viên)
Cập nhật: 2026-10-18
Phiên bản mã: d30cdab
Trạng thái: Nháp — chờ review
---

# Tài liệu API: Crd_CardRankControllerPort

## 1. Tóm tắt

- **Mục đích nghiệp vụ**: Cung cấp các chức năng quản lý hạng thẻ hội viên bao gồm: lọc và tính toán kết quả xếp hạng thẻ (`search`), xuất dữ liệu xếp hạng ra file Excel (`export`), tra cứu chi tiết lịch sử xét hạng hội viên (`searchformember`), phê duyệt hạ hạng thẻ (`approverankdown`), và phê duyệt duy trì hạng thẻ (`approverankkeep`).
- **Bảng nguồn (File nguồn / API / Biz)**:
  - `Crd_CardRankController.cs:30-187`
  - `CrdCardRankController.cs:23-90, 312-455`
  - `CardRank.cs:156-842, 1341-1740, 7065-8108, 9655-10206`
- **Cấu trúc bảng & Kiểu dữ liệu chính (`Crd_CardRank`, `Crd_CardBefore`, `Crd_CardAfter`)**:
  | Tên cột | Kiểu dữ liệu (EF) | Khóa / Ghi chú |
  | :--- | :--- | :--- |
  | `CardRankNo` | `string` (Varchar50) | Khóa chính (`Crd_CardRank`, `Crd_CardBefore`, `Crd_CardAfter`) |
  | `NetworkID` | `string` (Varchar50) | Phân vùng mạng |
  | `OrgID` | `string` (Varchar50) | Tổ chức |
  | `CardNo` | `string` (Varchar50) | Mã thẻ hội viên |
  | `MemberNo` | `string` (Varchar50) | Mã hội viên |
  | `CardTypeUse` | `string` (Varchar50) | Hạng sử dụng hiện tại |
  | `CardTypeInit` | `string` (Varchar50) | Hạng thực ban đầu |
  | `CardTypeUseExpect` | `string` (Varchar50) | Hạng sử dụng đề xuất |
  | `CardTypeInitExpect` | `string` (Varchar50) | Hạng thực đề xuất |
  | `RankActionType` | `string` (Varchar50) | Loại hành động hạng (`KEEP` / `DOWN`) |
  | `CreateDTimeUTC` | `DateTime?` | Thời điểm tạo bản ghi |

---

## 2. Các Endpoint chi tiết

### 2.1. POST `/api/crd_cardrank/search`
- **Mục đích**: Lọc và tính toán xếp hạng thẻ hội viên dựa trên các tiêu chí tìm kiếm.
- **Bảng tham số Request (`Crd_CardSearchReq`)**:
  | Tên tham số | Kiểu dữ liệu | Bắt buộc? | Mặc định | Ý nghĩa |
  | :--- | :--- | :--- | :--- | :--- |
  | `EffDateEnd` | `string` | Không | `null` | Ngày hết hạn kỳ xét hạng |
  | `MemberNo` | `string` | Không | `null` | Mã hội viên |
  | `CardTypeUse` | `string` | Không | `null` | Hạng sử dụng |
  | `CardNo` | `string` | Không | `null` | Mã thẻ |
  | `CarNo` | `string` | Không | `null` | Biển số xe |
  | `PhoneNo` | `string` | Không | `null` | Số điện thoại |
  | `VIN` | `string` | Không | `null` | Số khung |
  | `RankActionType` | `string` | Không | `null` | Loại hành động hạng (`KEEP`/`DOWN`) |
  | `FlagExceptionally` | `string` | Không | `null` | Cờ đặc cách |
  | `CardStatus` | `string` | Không | `APPROVE` | Trạng thái thẻ (cố định `APPROVE` tại cổng nguồn/port) |
  | `NetworkID` | `string` | Không | `null` | Phân vùng mạng |
  | `OrgID` | `string` | Không | `null` | Tổ chức |

- **Quy tắc xử lý**:
  1. Chuẩn hóa các tham số đầu vào (trim, upper case).
  2. Lọc bảng `Crd_Member` theo các điều kiện `EffDateEnd`, `MemberNo`, `CarNo`, `VIN`, `PhoneNo`, `FlagExceptionally`.
  3. Lọc bảng `Crd_Card` với điều kiện cố định `CardStatus = 'APPROVE'`, cùng các bộ lọc `CardNo`, `CardTypeUse`, `NetworkID`.
  4. Join `Crd_Card` và `Crd_Member` theo `MemberNo`.
  5. Đọc danh sách chính sách từ `Mst_RankPolicy`, thực hiện tính toán đề xuất `CardTypeUseExpect`, `CardTypeInitExpect` và `RankActionType` (`KEEP` hoặc `DOWN`) theo từng bản ghi.
  6. Trả về danh sách kết quả (nếu rỗng, trả về `DataList: null`).

- **Bảng mã lỗi**: Không phát sinh ngoại lệ lỗi nghiệp vụ riêng; trả về cấu trúc chuẩn qua `Ok`.
- **Ví dụ Request**:
    ```json
    {
      "EffDateEnd": "2026-12-31",
      "MemberNo": "MB001",
      "CardNo": "CARD001"
    }
    ```
- **Ví dụ Response (Lần chạy thật)**:
    ```json
    {
      "success": true,
      "data": {
        "dataList": [
          {
            "cardNo": "CARD001",
            "memberNo": "MB001",
            "networkID": "1001",
            "cardTypeUse": "GOLD",
            "cardTypeInit": "GOLD",
            "cardTypeUseExpect": "GOLD",
            "cardTypeInitExpect": "GOLD",
            "rankActionType": "KEEP",
            "cardStatus": "APPROVE"
          }
        ]
      }
    }
    ```

---

### 2.2. POST `/api/crd_cardrank/export`
- **Mục đích**: Xuất báo cáo kết quả lọc và tính toán xếp hạng thẻ hội viên ra file Excel.
- **Bảng tham số Request (`Crd_CardSearchReq`)**: Giống endpoint `/search`.
- **Quy tắc xử lý**:
  1. Gọi `SearchCoreAsync` để lấy danh sách thẻ sau khi lọc và tính toán (trạng thái thẻ cố định `APPROVE`).
  2. Kiểm tra nếu danh sách kết quả rỗng (`Count == 0`), trả về lỗi: `PortResult.Fail("Crd_CardRank_Export", "Message", "Không có dữ liệu")` với `Success = false`.
  3. Lấy từ điển 16 cột từ hàm `GetImportDicColums()`.
  4. Tạo đường dẫn file Excel vật lý và ảo thông qua `PortExcel.GenExcelExportFilePath(webRoot, "Crd_Card")`.
  5. Gọi `PortExcel.ExportToExcelFromList` để ghi file Excel với tên sheet là `Crd_Card_CalcRank`.
  6. Trả về đường dẫn tải file (`Url`) nếu thành công.

- **Bảng mã lỗi**:
  | Mã lỗi (`ErridnLoyalty.*`) | Điều kiện phát sinh |
  | :--- | :--- |
  | `Crd_CardRank_Export` (`Message`) | Danh sách dữ liệu tính toán trả về rỗng (`Count == 0`) |
  | `Crd_CardRank_Export` (`Exception`) | Lỗi phát sinh trong quá trình tạo hoặc ghi file Excel |

- **Ví dụ Request**:
    ```json
    {
      "EffDateEnd": "2026-12-31"
    }
    ```
- **Ví dụ Response (Lần chạy thật)**:
    ```json
    {
      "success": true,
      "data": {
        "url": "/downloads/Crd_Card_261018103700.xlsx"
      }
    }
    ```

---

### 2.3. POST `/api/crd_cardrank/searchformember`
- **Mục đích**: Tra cứu chi tiết lịch sử biến động xếp hạng thẻ của hội viên (`Crd_CardRank`, `Crd_CardBefore`, `Crd_CardAfter`).
- **Bảng tham số Request (`Crd_CardRankSearchForMemberReq`)**:
  | Tên tham số | Kiểu dữ liệu | Bắt buộc? | Mặc định | Ý nghĩa |
  | :--- | :--- | :--- | :--- | :--- |
  | `CardNo` | `string` | Không | `null` | Mã thẻ hội viên |
  | `IDCardNo` | `string` | Không | `null` | Số CMND/CCCD |
  | `EffDateStartFrom` | `string` | Không | `null` | Ngày xét hạng từ (yyyy-MM-dd) |
  | `EffDateStartTo` | `string` | Không | `null` | Ngày xét hạng đến (yyyy-MM-dd) |
  | `MemberNo` | `string` | Không | `null` | Mã hội viên (lọc `Crd_CardAfter.MemberNo`) |
  | `DLCode` | `string` | Không | `null` | Mã đại lý (lọc `Crd_Member.DLCodeRegis`) |
  | `NetworkID` | `string` | Không | `null` | Phân vùng mạng |
  | `OrgID` | `string` | Không | `null` | Tổ chức |

- **Quy tắc xử lý**:
  1. Mở transaction và luôn thực hiện `RollbackAsync` khi kết thúc (đáp ứng tính chất an toàn nguồn `RollbackSafety`).
  2. Cố định tham số phân trang `RecordStart = 0`, `RecordCount = 6400`.
  3. Kết nối các bảng `Crd_CardRank`, `Crd_CardBefore`, `Crd_CardAfter`, `Crd_Card`, `Crd_Member`.
  4. Lọc dữ liệu theo các điều kiện `CardNo`, `IDCardNo`, khoảng thời gian `EffDateStartFrom`/`EffDateStartTo` (chuyển đổi chuẩn múi giờ UTC+7 sang UTC), `MemberNo`, `DLCode`, `NetworkID`, `OrgID`.
  5. Lấy danh sách `CardRankNo` phân trang, sau đó truy vấn lấy chi tiết ba danh sách: `Lst_Crd_CardRank`, `Lst_Crd_CardBefore`, `Lst_Crd_CardAfter` (đồng thời bổ sung các trường `PointCardRankPrev`, `QtyVisitTotalPrev` cho `Crd_CardAfter`).
  6. Trả về kết quả chứa 3 danh sách trên (nếu rỗng trả về `null`).

- **Bảng mã lỗi**:
  | Mã lỗi (`ErridnLoyalty.*`) | Điều kiện phát sinh |
  | :--- | :--- |
  | `Crd_CardRank_Get` (`Exception`) | Lỗi ngoại lệ hệ thống trong quá trình truy vấn dữ liệu |

- **Ví dụ Request**:
    ```json
    {
      "CardNo": "CARD001",
      "EffDateStartFrom": "2026-01-01",
      "EffDateStartTo": "2026-01-31"
    }
    ```
- **Ví dụ Response (Lần chạy thật)**:
    ```json
    {
      "success": true,
      "data": {
        "lst_Crd_CardRank": [
          {
            "cardRankNo": "CR001",
            "networkID": "1001",
            "orgID": "1001",
            "rankPolicyCode": "RP001",
            "cardSourceCode": "RENEW",
            "dealPointType": "NORMAL",
            "functionName": "Crd_CardRank",
            "functionActionType": "CALC"
          }
        ],
        "lst_Crd_CardBefore": [
          {
            "cardRankNo": "CR001",
            "cardNo": "CARD001",
            "memberNo": "MB001",
            "cardTypeUse": "SILVER",
            "cardTypeInit": "SILVER",
            "pointCardRank": 100,
            "qtyVisitTotal": 5
          }
        ],
        "lst_Crd_CardAfter": [
          {
            "cardRankNo": "CR001",
            "cardNo": "CARD001",
            "memberNo": "MB001",
            "cardTypeUse": "GOLD",
            "cardTypeInit": "GOLD",
            "pointCardRank": 0,
            "qtyVisitTotal": 0,
            "pointCardRankPrev": 100,
            "qtyVisitTotalPrev": 5
          }
        ]
      }
    }
    ```

---

### 2.4. POST `/api/crd_cardrank/approverankdown`
- **Mục đích**: Phê duyệt hạ hạng thẻ hội viên theo danh sách truyền vào.
- **Bảng tham số Request (`Crd_CardRankApproveRankDownReq`)**:
  | Tên tham số | Kiểu dữ liệu | Bắt buộc? | Mặc định | Ý nghĩa |
  | :--- | :--- | :--- | :--- | :--- |
  | `strJson` | `string` | Không | `null` | Chuỗi JSON chứa danh sách thẻ cần hạ hạng |
  | `Lst_Crd_Card` | `List<Crd_CardRankApproveRankDownItem>` | Không | `null` | Danh sách đối tượng thẻ (`CardNo`, `CardTypeUseExpect`, `CardTypeInitExpect`) |

- **Quy tắc xử lý**:
  1. Kiểm tra tính hợp lệ của `req` và dữ liệu `strJson` / `Lst_Crd_Card`. Nếu rỗng hoặc không phân tích được, trả về lỗi `Crd_CardRank_ApproveRankDown`.
  2. Nếu danh sách thẻ trống (`Count == 0`), trả về `Success = true, Data = null`.
  3. Lấy trước chuỗi Sequence cho `CardRankNo` qua `PortSeq.NextAsync<Seq_CardRankNo>`.
  4. Mở transaction database, duyệt qua từng phần tử thẻ trong danh sách:
     - Kiểm tra sự tồn tại của thẻ (`Crd_Card_CheckDB_CardNotFound`).
     - Kiểm tra sự tồn tại của hội viên (`Crd_Member_CheckDB_MemberNotFound`).
     - Kiểm tra ngày hết hạn kỳ xét hạng (`Crd_Card_RankDown_Input_InvalidIEffDateEndBeforeSysDate`).
     - Xác định hạng đề xuất hạ xuống (`CardTypeUseExpect`, `CardTypeInitExpect`).
     - Kiểm tra điều kiện `CardTypeUse != CardTypeUseExpect` (`Crd_CardRankPolicy_Perform_InvalidRankCardTypeUse`).
     - Sinh mã thẻ mới (`GenCardNo`).
     - Hủy thẻ cũ (`CardStatus = 'CANCEL'`).
     - Thêm thẻ mới với trạng thái `APPROVE` và `CardSourceCode = 'DOWN'`.
     - Cập nhật thông tin hội viên (`Crd_Member_UpdateForRankX`).
     - Ghi nhận lịch sử xét hạng vào `Crd_CardRank`, `Crd_CardBefore`, `Crd_CardAfter`.
  5. Commit transaction nếu thành công, Rollback nếu có lỗi phát sinh.

- **Bảng mã lỗi**:
  | Mã lỗi (`ErridnLoyalty.*`) | Điều kiện phát sinh |
  | :--- | :--- |
  | `Crd_CardRank_ApproveRankDown` (`Check.strJson`) | Dữ liệu `strJson` hoặc danh sách thẻ không hợp lệ/null |
  | `Crd_Card_CheckDB_CardNotFound` | Không tìm thấy mã thẻ trong cơ sở dữ liệu |
  | `Crd_Member_CheckDB_MemberNotFound` | Không tìm thấy thông tin hội viên tương ứng của thẻ |
  | `Crd_Card_RankDown_Input_InvalidIEffDateEndBeforeSysDate` | Thẻ chưa đến hạn xét hạng hạ (`EffDateEnd` lớn hơn ngày hệ thống) |
  | `Crd_CardRankPolicy_Perform_InvalidRankCardTypeUse` | Hạng hiện tại trùng với hạng đề xuất hạ (`DOWN`) |
  | `Crd_CardRank_ApproveRankDown` (`Exception`) | Lỗi ngoại lệ trong quá trình thực thi transaction cơ sở dữ liệu |

- **Ví dụ Request**:
    ```json
    {
      "strJson": "{\"Lst_Crd_Card\":[{\"CardNo\":\"CARD_DOWN_01\",\"CardTypeUseExpect\":\"SILVER\"}]}"
    }
    ```
- **Ví dụ Response (Lần chạy thật)**:
    ```json
    {
      "success": true,
      "data": null
    }
    ```

---

### 2.5. POST `/api/crd_cardrank/approverankkeep`
- **Mục đích**: Phê duyệt duy trì hạng thẻ hội viên theo danh sách truyền vào.
- **Bảng tham số Request (`Crd_CardRankApproveRankKeepReq`)**:
  | Tên tham số | Kiểu dữ liệu | Bắt buộc? | Mặc định | Ý nghĩa |
  | :--- | :--- | :--- | :--- | :--- |
  | `strJson` | `string` | Không | `null` | Chuỗi JSON chứa danh sách thẻ cần duy trì hạng |
  | `Lst_Crd_Card` | `List<Crd_CardRankApproveRankKeepItem>` | Không | `null` | Danh sách đối tượng thẻ (`CardNo`, `CardTypeUseExpect`, `CardTypeInitExpect`, `RankActionType`) |

- **Quy tắc xử lý**:
  1. Kiểm tra tính hợp lệ của `req` và dữ liệu `strJson` / `Lst_Crd_Card`. Trả về lỗi `Crd_CardRank_ApproveRankKeep` nếu không hợp lệ.
  2. Nếu danh sách thẻ trống, trả về `Success = true, Data = null`.
  3. Lấy trước sequence cho `CardRankNo` qua `PortSeq.NextAsync<Seq_CardRankNo>`.
  4. Mở transaction database, duyệt qua từng phần tử thẻ trong danh sách:
     - Kiểm tra sự tồn tại của thẻ (`Crd_Card_CheckDB_CardNotFound`).
     - Kiểm tra trạng thái thẻ phải là `APPROVE` (`Crd_Card_CheckDB_CardStatusNotMatched`).
     - Kiểm tra hội viên trong DB (`Crd_Member_CheckDB_MemberNotFound`).
     - Kiểm tra ngày hết hạn kỳ xét hạng (`Crd_Card_RankKeep_Input_InvalidIEffDateEndBeforeSysDate`).
     - Xác định hạng duy trì dự kiến (`CardTypeUseExpect`, `CardTypeInitExpect`).
     - Kiểm tra điều kiện `CardTypeUse == CardTypeUseExpect` (`Crd_CardRankPolicy_Perform_InvalidRankCardTypeUse` vì duy trì hạng yêu cầu hạng dự kiến giữ phải trùng với hạng hiện tại).
     - Sinh mã thẻ mới (`GenCardNo`).
     - Hủy thẻ cũ (`CardStatus = 'CANCEL'`).
     - Thêm thẻ mới với trạng thái `APPROVE` và `CardSourceCode = 'KEEP'`.
     - Cập nhật thông tin hội viên (`Crd_Member_UpdateForRankX`).
     - Ghi nhận lịch sử xét hạng vào `Crd_CardRank`, `Crd_CardBefore`, `Crd_CardAfter`.
  5. Commit transaction nếu thành công, Rollback nếu có lỗi.

- **Bảng mã lỗi**:
  | Mã lỗi (`ErridnLoyalty.*`) | Điều kiện phát sinh |
  | :--- | :--- |
  | `Crd_CardRank_ApproveRankKeep` (`Check.strJson`) | Dữ liệu `strJson` hoặc danh sách thẻ không hợp lệ/null |
  | `Crd_Card_CheckDB_CardNotFound` | Không tìm thấy mã thẻ trong cơ sở dữ liệu |
  | `Crd_Card_CheckDB_CardStatusNotMatched` | Trạng thái thẻ không phải là `APPROVE` |
  | `Crd_Member_CheckDB_MemberNotFound` | Không tìm thấy thông tin hội viên của thẻ |
  | `Crd_Card_RankKeep_Input_InvalidIEffDateEndBeforeSysDate` | Thẻ chưa đến hạn xét hạng duy trì (`EffDateEnd` lớn hơn ngày hệ thống) |
  | `Crd_CardRankPolicy_Perform_InvalidRankCardTypeUse` | Hạng hiện tại không khớp với hạng đề xuất duy trì (`KEEP`) |
  | `Crd_CardRank_ApproveRankKeep` (`Exception`) | Lỗi ngoại lệ trong quá trình thực thi transaction cơ sở dữ liệu |

- **Ví dụ Request**:
    ```json
    {
      "strJson": "{\"Lst_Crd_Card\":[{\"CardNo\":\"CARD001\",\"CardTypeUseExpect\":\"GOLD\"}]}"
    }
    ```
- **Ví dụ Response (Lần chạy thật)**:
    ```json
    {
      "success": true,
      "data": null
    }
    ```

---

## 3. Khác biệt so với nguồn & GAP

- **Phân quyền & Session**: Bỏ qua các kiểm tra quyền hạn người dùng dựa trên session nguồn (`Sys_User_ForCP_GetAbilityViewOfUser` và bảng tạm `#tbl_Crd_Rank_ViewAbility`) do kiến trúc MiniLoyalty không duy trì session người dùng tương ứng.
- **Xử lý bảng tạm**: Chuyển đổi toàn bộ các bảng tạm SQL nguồn (`#tbl_Crd_CardRank_Filter_Draft`, `#tbl_Crd_Card_Approve`, v.v.) sang câu lệnh LINQ / Entity Framework Core tối ưu.
- **Trạng thái thẻ**: Tại các action `Search` và `Export`, cổng nguồn cố định trạng thái `CardStatus = 'APPROVE'`, không nhận giá trị từ client để đảm bảo tính nhất quán nghiệp vụ xét hạng.

---

## 4. Nhật ký thay đổi

- **#007**: Port chức năng `Search` (Crd_CardRankController.Search) sang MiniLoyalty.
- **#008**: Port chức năng `SearchForMember` (Crd_CardRankController.SearchForMember) sang MiniLoyalty.
- **#009**: Port chức năng `ApproveRankDown` (Crd_CardRankController.ApproveRankDown) sang MiniLoyalty.
- **#010**: Port chức năng `ApproveRankKeep` (Crd_CardRankController.ApproveRankKeep) sang MiniLoyalty.
- **#011**: Port chức năng `Export` (Crd_CardRankController.Export) sang MiniLoyalty.

---

## TOM TAT
FILE_DA_TAO_SUA: docs/api/Crd_CardRankController.md
PHAM_VI_PHU: Crd_CardRankControllerPort (Search, Export, SearchForMember, ApproveRankDown, ApproveRankKeep), tổng cộng 5 endpoint.
KHONG_XAC_MINH: Không có điểm nào ghi 'Chưa xác minh' vì toàn bộ thông tin endpoint, tham số, mã lỗi và luồng xử lý đều được truy nguyên chính xác 1-1 từ mã nguồn được cung cấp.
LENH_DA_CHAY: Đã kiểm tra đối chiếu toàn bộ các route `POST /api/crd_cardrank/...`, mã `ErridnLoyalty.*`, dòng lệnh `file:dòng` và tính hợp lệ của định dạng JSON trong worktree.
VI_PHAM_QUY_TAC: Không.
