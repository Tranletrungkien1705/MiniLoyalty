Phạm vi: Controllers/Port/Crd_CardControllerPort.cs, Models/Port/Crd_Card.cs
Cập nhật: 2026-10-18
Phiên bản mã: d30cdab
Trạng thái: Nháp — chờ review

# Crd_CardController API

## 1. Tóm tắt
- **Mục đích nghiệp vụ:** Quản lý và tra cứu thông tin thẻ hội viên (`Crd_Card`), xem chi tiết thẻ, tính toán nâng/hạ hạng, yêu cầu và xét duyệt đặc cách thẻ hội viên, cũng như xuất báo cáo danh sách thẻ ra file Excel.
- **Bảng nguồn:** 
  - Gate: `Crd_CardController.cs` (Search: :29-131, GetDetailMember: :133-203, GetDetailMember_New: :205-284, RequestException: :289-352, ApprException: :354-416, Export: :421-541, GetImportDicColums: :546-582, strWhereClause_Crd_Card: :586-697).
  - API: `CrdCardController.cs` (`WA_Crd_Card_Get`, `WA_Crd_Card_Calc`, `WA_Crd_Card_RequestException`, `WA_Crd_Card_ApprException`).
  - Biz: `Master.Loyalty.cs` (`WAS_Crd_Card_Get`, `Crd_Card_Get`, `Crd_Card_GetX_New20251229`), `Card.cs` (`Crd_Card_Calc`, `Crd_Card_CalcX`, `Crd_Card_RequestException`, `Crd_Card_RequestExceptionX`, `Crd_Card_ApprException`, `Crd_Card_ApprExceptionX`, `Crd_Card_CancelX`, `Crd_Card_ApproveX`).
- **Bảng cấu trúc / Thực thể liên quan:**
  - `Crd_Card`: Bảng lưu trữ thông tin thẻ hội viên.
    - `CardNo` (string, khóa chính, bắt buộc): Mã thẻ hội viên.
    - `CardNoPrev` (string, không bắt buộc): Mã thẻ kỳ trước.
    - `NetworkID` (string, không bắt buộc): Mã mạng lưới.
    - `MemberNo` (string, bắt buộc): Mã hội viên (khóa ngoại liên kết `Crd_Member`).
    - `CardSourceCode` (string, không bắt buộc): Mã loại thẻ (`DOWN`, `KEEP`, `NEW`, `RENEW`, `UP`).
    - `RankPolicyCode` (string, không bắt buộc): Mã chính sách xếp hạng.
    - `DLCodeExceptionally` (string, không bắt buộc): Đại lý đặc cách.
    - `PointBonus` (double, không bắt buộc): Điểm thưởng.
    - `PointPurchaseAddCar` (double, không bắt buộc): Điểm thưởng mua thêm xe.
    - `EffDateStart` (DateTime, không bắt buộc): Ngày bắt đầu kỳ xét hạng.
    - `EffDateEnd` (DateTime, không bắt buộc): Ngày hết hạn kỳ xét hạng.
    - `PointTotal` (double, không bắt buộc): Điểm tiêu dùng trong kỳ.
    - `PointBlock` (double, không bắt buộc): Điểm block trong kỳ.
    - `PointAvail` (double, không bắt buộc): Điểm tiêu dùng còn lại.
    - `AmountTotal` (double, không bắt buộc): Giá trị tích lũy trong kỳ.
    - `AmountBlock` (double, không bắt buộc): Giá trị block.
    - `AmountAvail` (double, không bắt buộc): Doanh thu xét hạng trong kỳ.
    - `QtyVisitTotal` (double, không bắt buộc): Tổng số lượt dịch vụ.
    - `QtyVisitBlock` (double, không bắt buộc): Số lượt dịch vụ block.
    - `QtyVisitAvail` (double, không bắt buộc): Số lượt dịch vụ khả dụng.
    - `PointCardRank` (double, không bắt buộc): Điểm xét hạng.
    - `TotalAmountPeriod` (double, không bắt buộc): Tổng giá trị trong kỳ.
    - `CardTypeUse` (string, không bắt buộc): Hạng sử dụng.
    - `CardTypeInit` (string, không bắt buộc): Hạng thực.
    - `CardTypeUsePrev` (string, không bắt buộc): Hạng sử dụng kỳ trước.
    - `CardTypeInitPrev` (string, không bắt buộc): Hạng thực kỳ trước.
    - `CardActiveDate` (DateTime, không bắt buộc): Ngày kích hoạt thẻ.
    - `FlagExceptionally` (string, không bắt buộc): Cờ đặc cách.
    - `CardTypeUseExpect` (string, không bắt buộc): Hạng sử dụng dự kiến.
    - `CardTypeInitExpect` (string, không bắt buộc): Hạng thực dự kiến.
    - `RankActionType` (string, không bắt buộc): Loại hành động hạng (`KEEP`, `DOWN`, `UP`).
    - `CreateDTimeUTC` (DateTime, không bắt buộc): Thời điểm tạo thẻ (UTC).
    - `CreateBy` (string, không bắt buộc): Người tạo.
    - `LUDTimeUTC` (DateTime, không bắt buộc): Thời điểm cập nhật cuối (UTC).
    - `LUBy` (string, không bắt buộc): Người cập nhật cuối.
    - `ApproveDTimeUTC` (DateTime, không bắt buộc): Thời điểm duyệt thẻ (UTC).
    - `ApproveBy` (string, không bắt buộc): Người duyệt thẻ.
    - `CancelDTimeUTC` (DateTime, không bắt buộc): Thời điểm hủy thẻ (UTC).
    - `CancelBy` (string, không bắt buộc): Người hủy thẻ.
    - `CardStatus` (string, không bắt buộc): Trạng thái thẻ (`PENDING`, `APPROVE`, `CANCEL`).
    - `Remark` (string, không bắt buộc): Ghi chú.
    - `LogLUDTimeUTC` (DateTime, không bắt buộc): Thời điểm ghi log UTC.
    - `LogLUBy` (string, không bắt buộc): Người ghi log.
  - `Crd_Member`: Thực thể hội viên liên kết qua `MemberNo`.
  - `Crd_CardTransaction`: Bảng giao dịch thẻ hội viên.
  - `Mst_RankPolicy`: Bảng chính sách xếp hạng.
  - `Mst_PolicyMoneyToPointServiceDtl`: Bảng cấu hình quy đổi tiền sang điểm dịch vụ.
  - `Mst_ParamSys`: Bảng tham số hệ thống (`UNITPOINTTOMONEY`).
  - `Crd_CardDealerUseException`: Bảng quản lý đại lý ngoại lệ sử dụng thẻ khi đặc cách.

## 2. Các Endpoint

### 1. POST /api/crd_card/search
Tìm kiếm phân trang danh sách thẻ hội viên dựa trên các điều kiện lọc linh hoạt.

#### Tham số Request
Body kiểu JSON (`Crd_CardControllerSearchReq`):
- `MemberNo` (string, không bắt buộc): Mã hội viên.
- `CarNo` (string, không bắt buộc): Biển số xe.
- `VIN` (string, không bắt buộc): Số khung (`VIN`).
- `PhoneNo` (string, không bắt buộc): Số điện thoại hội viên.
- `DLCodeRegis` (string, không bắt buộc): Mã đại lý đăng ký (phân tách bởi dấu phẩy).
- `DLCPCode` (string, không bắt buộc): Mã đại lý truy vấn.
- `CardStatus` (string, không bắt buộc): Trạng thái thẻ trên hội viên (phân tách bởi dấu phẩy).
- `MemberActiveDateFrom` (string, không bắt buộc): Ngày kích hoạt hội viên từ ngày (`yyyy-MM-dd`).
- `MemberActiveDateTo` (string, không bắt buộc): Ngày kích hoạt hội viên đến ngày (`yyyy-MM-dd`).
- `CardNo` (string, không bắt buộc): Số thẻ hội viên.
- `FlagExceptionally` (string, không bắt buộc): Cờ đặc cách.
- `EffDateStartFrom` (string, không bắt buộc): Ngày hiệu lực từ ngày (`yyyy-MM-dd`).
- `EffDateStartTo` (string, không bắt buộc): Ngày hiệu lực đến ngày (`yyyy-MM-dd`).
- `InactiveDTimeFrom` (string, không bắt buộc): Thời gian dừng từ ngày.
- `InactiveDTimeTo` (string, không bắt buộc): Thời gian dừng đến ngày.
- `CardTypeUse` (string, không bắt buộc): Hạng thẻ sử dụng.
- `MemberStatus` (string, không bắt buộc): Trạng thái hội viên (phân tách bởi dấu phẩy).
- `cm_FlagShow` (string, không bắt buộc): Cờ hiển thị hội viên.
- `cc_FlagShow` (string, không bắt buộc): Cờ hiển thị thẻ.
- `Ft_PageIndex` (int, không bắt buộc, mặc định: `0`): Trang hiện tại.
- `Ft_PageSize` (int, không bắt buộc, mặc định: `10`): Kích thước trang.

#### Quy tắc xử lý theo thứ tự
1. Khởi tạo đối tượng request nếu `null`.
2. Xây dựng câu lệnh truy vấn kết hợp bảng `Crd_Member` và `Crd_Card` với điều kiện lấy thẻ mới nhất (`CardNo`) theo `CreateDTimeUTC desc` cho mỗi hội viên.
3. Áp dụng các điều kiện lọc theo tham số truyền lên (`MemberNo`, `CarNo`, `VIN`, `PhoneNo`, `DLCodeRegis`, `DLCPCode`, `CardStatus`, khoảng thời gian `MemberActiveDate`, `EffDateStart` quy đổi múi giờ, `CardSourceCode`, `FlagExceptionally`, `InactiveDTime`, `CardTypeUse`, `MemberStatus`).
4. Thực hiện đếm tổng số lượng bản ghi thỏa điều kiện (`itemCount`).
5. Xử lý phân trang: nếu `Ft_PageSize <= 0`, trả về danh sách dữ liệu rỗng và giữ nguyên tổng `itemCount`.
6. Sắp xếp danh sách kết quả giảm dần theo `cc.CardNo desc`, bỏ qua các trang trước (`Skip`) và lấy số lượng theo trang (`Take`).
7. Ánh xạ danh sách kết quả sang model `Crd_CardSearchItem` bao gồm việc tính toán các chỉ số bổ sung (`PointExpiry`, `AmountRef`, `PointUse`, `RevenueMiss`, `QtyVisitMiss`, `cm_FlagShow`, `cc_FlagShow`).
8. Trả về HTTP 200 kèm cấu trúc kết quả phân trang `PageInfo<Crd_CardSearchItem>`.

#### Bảng Mã Lỗi
| Mã lỗi | Điều kiện phát sinh | Chi tiết / Tham số trả về |
| :--- | :--- | :--- |
| Không có | Endpoint tìm kiếm sử dụng kết quả thành công chuẩn và trả về danh sách hoặc phân trang rỗng. | Không có |

#### Ví dụ Request / Response
- **Request:**
    POST /api/crd_card/search
    Content-Type: application/json

    {
      "MemberNo": "MEM01",
      "Ft_PageIndex": 0,
      "Ft_PageSize": 10
    }

- **Response (Thành công):**
    Chưa có lần chạy mau

---

### 2. POST /api/crd_card/getdetailmember
Lấy thông tin chi tiết thẻ hội viên dựa trên mã hội viên và mã thẻ.

#### Tham số Request
Body kiểu JSON (`Crd_CardControllerGetDetailMemberReq`):
- `MemberNo` (string, bắt buộc): Mã hội viên.
- `CardNo` (string, bắt buộc): Mã thẻ hội viên.

#### Quy tắc xử lý theo thứ tự
1. Khởi tạo request nếu `null`.
2. Kiểm tra nếu `CardNo` và `MemberNo` đồng thời khác rỗng/null, thực hiện truy vấn kết hợp hội viên và thẻ mới nhất theo điều kiện `MemberNo`.
3. Lấy ra bản ghi đầu tiên, ánh xạ qua hàm gom dữ liệu `BuildCrdCardItemsAsync`.
4. Trả về kết quả đối tượng dữ liệu cấu trúc `{ Success = true, Data = { Crd_Card = objCrd_Card } }`. Nếu thiếu tham số hoặc không tìm thấy, `Crd_Card` có giá trị `null`.

#### Bảng Mã Lỗi
| Mã lỗi | Điều kiện phát sinh | Chi tiết / Tham số trả về |
| :--- | :--- | :--- |
| Không có | Không tìm thấy bản ghi hoặc request thiếu tham số (trả về `null`). | Không có |

#### Ví dụ Request / Response
- **Request:**
    POST /api/crd_card/getdetailmember
    Content-Type: application/json

    {
      "MemberNo": "MEM01",
      "CardNo": "CARD01"
    }

- **Response (Thành công):**
    Chưa có lần chạy mau

---

### 3. POST /api/crd_card/getdetailmember_new
Lấy chi tiết và tính toán thông tin nâng hạng thẻ mới của hội viên.

#### Tham số Request
Body kiểu JSON (`Crd_CardControllerGetDetailMemberReq`):
- `CardNo` (string, bắt buộc): Mã thẻ hội viên.
- `MemberNo` (string, bắt buộc): Mã hội viên.

#### Quy tắc xử lý theo thứ tự
1. Lấy thông tin `CardNo` và `MemberNo` từ Body request hoặc Query string.
2. Kiểm tra `CardNo` rỗng: trả về lỗi `invalid_CardNo` (`Check.CardNo`).
3. Kiểm tra `MemberNo` rỗng: trả về lỗi `invalid_MemberNo` (`Check.MemberNo`).
4. Kiểm tra tồn tại thẻ trong cơ sở dữ liệu: nếu không thấy, trả về lỗi `Crd_Card_CheckDB_CardNotFound` (`Check.CardNo`).
5. Kiểm tra trạng thái thẻ phải là `APPROVE`: nếu khác, trả về lỗi `Crd_Card_CheckDB_CardStatusNotMatched`.
6. Kiểm tra tồn tại hội viên: nếu không thấy, trả về lỗi `Crd_Member_CheckDB_MemberNotFound` (`Check.MemberNo`).
7. Kiểm tra tính hợp lệ của hội viên liên kết với thẻ, tính toán khoảng cách doanh thu/lượt dịch vụ còn thiếu để nâng hạng tiếp theo (`RevenueMiss`, `QtyVisitMiss`) dựa trên tham số hệ thống (`UNITPOINTTOMONEY`) và chính sách hạng (`Mst_RankPolicy`).
8. Trả về thông tin chi tiết thẻ kèm các thông số tính toán mới.

#### Bảng Mã Lỗi
| Mã lỗi | Điều kiện phát sinh | Chi tiết / Tham số trả về |
| :--- | :--- | :--- |
| `invalid_CardNo` | Tham số `CardNo` bị thiếu hoặc rỗng. | `Check.CardNo` |
| `invalid_MemberNo` | Tham số `MemberNo` bị thiếu hoặc rỗng. | `Check.MemberNo` |
| `Crd_Card_CheckDB_CardNotFound` | Không tìm thấy thẻ hội viên theo `CardNo`. | `Check.CardNo` |
| `Crd_Card_CheckDB_CardStatusNotMatched` | Trạng thái thẻ không phải `APPROVE`. | `Check.CardNo`, `Check.strCardStatusListToCheck`, `DB.CardStatus` |
| `Crd_Member_CheckDB_MemberNotFound` | Không tìm thấy hội viên theo `MemberNo`. | `Check.MemberNo` |

#### Ví dụ Request / Response
- **Request:**
    POST /api/crd_card/getdetailmember_new
    Content-Type: application/json

    {
      "CardNo": "CARD01",
      "MemberNo": "MEM01"
    }

- **Response (Thành công):**
    Chưa có lần chạy mau

---

### 4. POST /api/crd_card/requestexception
Yêu cầu xét duyệt đặc cách thẻ hội viên.

#### Tham số Request
Body kiểu JSON (`RQ_Crd_Card`):
- `Crd_Card` (object, bắt buộc): Thông tin yêu cầu đặc cách thẻ.
  - `CardNo` (string, bắt buộc): Mã thẻ hiện tại.
  - `CardTypeUse` (string, không bắt buộc): Hạng sử dụng.
  - `DLCodeExceptionally` (string, không bắt buộc): Mã đại lý đặc cách.
  - `Remark` (string, không bắt buộc): Ghi chú.
- `Lst_Crd_CardDealerUseException` (array, bắt buộc): Danh sách đại lý ngoại lệ được phép sử dụng thẻ.
  - `DealerCode` (string, bắt buộc): Mã đại lý.

#### Quy tắc xử lý theo thứ tự
1. Kiểm tra payload `req` hoặc đối tượng `Crd_Card` rỗng: trả về lỗi `Crd_Card_RequestException` (`Check.strJson`).
2. Chuẩn hóa tham số đầu vào (`CardNo`, `CardTypeUse`, `DLCodeExceptionally`, `Remark`).
3. Kiểm tra thẻ tồn tại trong DB: nếu không thấy, trả về lỗi `Crd_Card_CheckDB_CardNotFound`.
4. Kiểm tra trạng thái thẻ phải là `APPROVE` và nằm trong khoảng thời gian hiệu lực hệ thống.
5. Kiểm tra hội viên tồn tại, trạng thái đăng ký (`RegisStatus == "FINISH"`) và trạng thái hội viên (`MemberStatus == "APPROVE"`).
6. Kiểm tra xem đã tồn tại thẻ đặc cách ở trạng thái `PENDING` hay chưa: nếu có, trả về lỗi `Crd_Card_RequestException_ExistedCardNoException`.
7. Kiểm tra danh sách đại lý ngoại lệ `Lst_Crd_CardDealerUseException`: nếu trống hoặc không hợp lệ, trả về lỗi `Crd_Card_RequestException_CardDealerUseExceptionTblInvalid`.
8. Sinh mã thẻ mới tự động thông qua hàm `GenCardNo`.
9. Bắt đầu transaction, thêm mới bản ghi thẻ với trạng thái `PENDING`, ghi nhận danh sách đại lý ngoại lệ vào `Crd_CardDealerUseException`, cập nhật trạng thái hội viên sang `PENDING` và commit transaction.

#### Bảng Mã Lỗi
| Mã lỗi | Điều kiện phát sinh | Chi tiết / Tham số trả về |
| :--- | :--- | :--- |
| `Crd_Card_RequestException` | Request hoặc thông tin `Crd_Card` bị thiếu/rỗng hoặc xảy ra lỗi exception hệ thống. | `Check.strJson` hoặc `Exception` |
| `Crd_Card_CheckDB_CardNotFound` | Không tìm thấy thẻ yêu cầu đặc cách. | `Check.CardNo` |
| `Crd_Card_CheckDB_CardStatusNotMatched` | Trạng thái thẻ không phải `APPROVE`. | `Check.CardNo`, `Check.strCardStatusListToCheck` |
| `Crd_Member_CheckDB_MemberNotFound` | Không tìm thấy hội viên tương ứng. | `Check.MemberNo` |
| `Crd_Member_CheckDB_RegisStatusNotMatched` | Trạng thái đăng ký của hội viên không phải `FINISH`. | `Check.MemberNo`, `Check.strRegisStatusListToCheck` |
| `Crd_Member_CheckDB_MemberStatusNotMatched` | Trạng thái hội viên không phải `APPROVE`. | `Check.MemberNo`, `Check.strMemberStatusListToCheck` |
| `Crd_Card_RequestException_ExistedCardNoException` | Đã tồn tại thẻ đặc cách ở trạng thái `PENDING` cho thẻ này. | `Check.MemberNo`, `Check.CardNo`, `Check.CardNo_Exception` |
| `Crd_Card_RequestException_CardDealerUseExceptionTblInvalid` | Danh sách đại lý ngoại lệ trống hoặc không hợp lệ. | `Check.TableName` |

#### Ví dụ Request / Response
- **Request:**
    POST /api/crd_card/requestexception
    Content-Type: application/json

    {
      "Crd_Card": {
        "CardNo": "CARD01",
        "CardTypeUse": "VIP",
        "DLCodeExceptionally": "VS058",
        "Remark": "Xin đặc cách nâng cấp thẻ"
      },
      "Lst_Crd_CardDealerUseException": [
        {
          "DealerCode": "VS058"
        }
      ]
    }

- **Response (Thành công):**
    Chưa có lần chạy mau

---

### 5. POST /api/crd_card/apprexception
Duyệt kỳ thẻ đặc cách cho hội viên.

#### Tham số Request
Body kiểu JSON (`RQ_Crd_Card`):
- `Crd_Card` (object, bắt buộc): Thông tin duyệt đặc cách thẻ.
  - `CardNo` (string, bắt buộc): Mã thẻ mới cần duyệt (`CardNoNew`).
  - `Remark` (string, không bắt buộc): Ghi chú duyệt.

#### Quy tắc xử lý theo thứ tự
1. Kiểm tra request hoặc `CardNo` bị rỗng: trả về lỗi `Crd_Card_ApprException` (`Check.strJson`).
2. Kiểm tra thẻ mới tồn tại trong DB và trạng thái phải là `PENDING`: nếu không, trả về lỗi `Crd_Card_CheckDB_CardNotFound` hoặc `Crd_Card_CheckDB_CardStatusNotMatched`.
3. Kiểm tra cờ đặc cách của thẻ mới (`FlagExceptionally != "0"`): nếu không hợp lệ, trả về lỗi `Crd_Card_ApprException_CardNoExceptionInvalid`.
4. Kiểm tra hội viên tồn tại, `RegisStatus == "FINISH"` và `MemberStatus == "APPROVE"`.
5. Kiểm tra hội viên phải đang có thẻ hoạt động ở trạng thái `APPROVE`: nếu không có, trả về lỗi `Crd_Card_ApprException_CardNoActiveInvalid`.
6. Tính toán thời hạn hiệu lực thẻ mới (12 tháng).
7. Bắt đầu transaction: chuyển trạng thái thẻ cũ sang `CANCEL`, chuyển trạng thái thẻ mới sang `APPROVE` kèm thiết lập hiệu lực, cập nhật thông tin tương ứng trên hội viên (`Crd_Member`) và commit transaction.

#### Bảng Mã Lỗi
| Mã lỗi | Điều kiện phát sinh | Chi tiết / Tham số trả về |
| :--- | :--- | :--- |
| `Crd_Card_ApprException` | Payload request hoặc `CardNo` trống hoặc ngoại lệ hệ thống. | `Check.strJson` hoặc `Exception` |
| `Crd_Card_CheckDB_CardNotFound` | Không tìm thấy thẻ mới cần duyệt. | `Check.CardNo` |
| `Crd_Card_CheckDB_CardStatusNotMatched` | Trạng thái thẻ không phải `PENDING`. | `Check.CardNo`, `Check.strCardStatusListToCheck` |
| `Crd_Card_ApprException_CardNoExceptionInvalid` | Cờ đặc cách của thẻ mới không hợp lệ (`FlagExceptionally == "0"`). | `Check.MemberNo`, `Check.CardNoNew`, `Check.FlagExceptionally` |
| `Crd_Member_CheckDB_MemberNotFound` | Không tìm thấy hội viên. | `Check.MemberNo` |
| `Crd_Card_ApprException_CardNoActiveInvalid` | Không tìm thấy thẻ đang hoạt động (`APPROVE`) hợp lệ của hội viên để hủy. | `Check.MemberNo`, `Check.CardNoNew` |

#### Ví dụ Request / Response
- **Request:**
    POST /api/crd_card/apprexception
    Content-Type: application/json

    {
      "Crd_Card": {
        "CardNo": "CARD04",
        "Remark": "Duyệt đặc cách thẻ"
      }
    }

- **Response (Thành công):**
    Chưa có lần chạy mau

---

### 6. POST /api/crd_card/export
Xuất danh sách thẻ hội viên ra file Excel theo bộ lọc tìm kiếm.

#### Tham số Request
Body kiểu JSON (`Crd_CardControllerSearchReq`):
- Sử dụng tương tự các tham số lọc của endpoint `search`.

#### Quy tắc xử lý theo thứ tự
1. Xây dựng truy vấn dữ liệu thẻ và hội viên tương tự endpoint `search`.
2. Lấy toàn bộ danh sách kết quả phù hợp (không áp dụng phân trang `Skip`/`Take`).
3. Nếu danh sách kết quả rỗng, trả về lỗi `Crd_Card_Export` với thông báo `Không có dữ liệu`.
4. Ánh xạ danh sách dữ liệu và chuẩn hóa hiển thị các trường (`MemberGender`: `"1"` thành `"Nam"`, ngược lại `"Nữ"`; `cm_FlagShow`: `"1"` thành `"Active"`, `"0"` thành `"Inactive"`, ngược lại `"None"`).
5. Khởi tạo từ điển 31 cột theo cấu hình `GetImportDicColums()`.
6. Thực hiện tạo file Excel và trả về đường dẫn tải file qua `Url`.

#### Bảng Mã Lỗi
| Mã lỗi | Điều kiện phát sinh | Chi tiết / Tham số trả về |
| :--- | :--- | :--- |
| `Crd_Card_Export` | Không có dữ liệu thỏa mãn điều kiện lọc để xuất file hoặc xảy ra ngoại lệ. | `Message` hoặc `Exception` |

#### Ví dụ Request / Response
- **Request:**
    POST /api/crd_card/export
    Content-Type: application/json

    {
      "MemberNo": "MEM01"
    }

- **Response (Thành công):**
    Chưa có lần chạy mau

## 3. Khác biệt so với nguồn & GAP
- Các toán tử lọc ngày tháng như `EffDateStartFrom` / `EffDateStartTo` thực hiện quy đổi chuẩn hóa thời gian đầu/cuối ngày sang UTC (`UTC+7`) để khớp với lưu trữ `ApproveDTimeUTC`.
- Một số trường dữ liệu mô tả tra cứu mở rộng được ánh xạ thông qua các bảng `NotMapped` trên model để tương thích với cấu trúc hiển thị của tầng Biz nguồn.

## 4. Nhật ký thay đổi
- Hạng mục `#001`, `#003`, `#004`, `#005`, `#006`, `#007` — Port 1:1 các endpoint tìm kiếm, chi tiết, đặc cách, duyệt đặc cách và xuất Excel của `Crd_CardController` sang MiniLoyalty (phiên bản mã `d30cdab`).

TOM TAT
FILE_DA_TAO_SUA: docs/api/Crd_CardController.md
PHAM_VI_PHU: Crd_CardControllerPort (6 endpoint: search, getdetailmember, getdetailmember_new, requestexception, apprexception, export)
KHONG_XAC_MINH: Ví dụ response thực tế (do môi trường WEB không chạy lệnh/chạy app trực tiếp).
LENH_DA_CHAY: Không chạy lệnh (Theo quy định môi trường WEB: KHÔNG chạy lệnh).
VI_PHAM_QUY_TAC: Khong
