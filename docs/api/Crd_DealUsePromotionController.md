# Tài liệu API: Crd_DealUsePromotionController

Phạm vi: Crd_DealUsePromotionController (POST /api/crd_dealusepromotion/save, POST /api/crd_dealusepromotion/search)
Cập nhật: 2026-10-17
Phiên bản mã: e126be3
Trạng thái: Nháp — chờ review

## 1. Tóm tắt

Cung cấp chức năng ghi nhận giao dịch sử dụng ưu đãi chương trình khuyến mại (Save) và tìm kiếm danh sách thẻ hội viên hợp lệ đủ điều kiện áp dụng ưu đãi (Search). Quá trình ghi nhận tự động kiểm tra hạn mức hạng thẻ, quy định số lần sử dụng trong ngày, sinh mã giao dịch theo chuỗi số (sequence) và phát sinh giao dịch thẻ tương ứng.

### Tệp nguồn và tệp đích
- Nguồn Gate:
  - 13.ClientGate/V10/idn.Loyalty.WebAPI/Controllers/Crd_DealUsePromotionController.cs:32-120 (Save)
  - 13.ClientGate/V10/idn.Loyalty.WebAPI/Controllers/Crd_DealUsePromotionController.cs:124-203 (Search), :460-477 (strWhereClause_Crd_Card)
- Nguồn API:
  - 11.BackEnd/V10/idn.Loyalty.Biz.Web/Controllers/CrdDealUsePromotionController.cs:23-93 (Save)
  - 11.BackEnd/V10/idn.Loyalty.Biz.Web/Controllers/CrdDealUsePromotionController.cs:169-239 (Search WA_Crd_Card_GetForPromotion)
- Nguồn Biz / SQL:
  - 11.BackEnd/V10/idn.Loyalty.Biz/Master.Loyalty.cs:16214-16309 (WAS_Crd_DealUsePromotion_Save), 16061-16212 (Crd_DealUsePromotion_Save), 15549-16059 (Crd_DealUsePromotion_SaveX)
  - 11.BackEnd/V10/idn.Loyalty.Biz/Transaction.AddPoint.cs:1072-1484 (Crd_DealUsePromotion_PerformX)
  - 11.BackEnd/V10/idn.Loyalty.Biz/Card.cs:13271-13339 (Crd_Member_UpdateDTime), 21229-21340 (Crd_Card_CheckDB)
  - 11.BackEnd/V10/idn.Loyalty.Biz/Master.Loyalty.cs:2870-2930 (Mst_DealerCP_CheckDB), 13092-13178 (Mst_PromotionProgram_CheckDB)
  - 11.BackEnd/V10/idn.Loyalty.Biz/Master.Loyalty.cs:16790-16903 (WAS_Crd_Card_GetForPromotion), 16904-17062 (Crd_Card_GetForPromotion), 17063-17460 (Crd_Card_GetForPromotionX)
  - 11.BackEnd/V10/idn.Loyalty.Biz/Seq.cs:147 (Seq_DealUsePrmNo)
  - 11.BackEnd/V10/05.Refs.Biz/Migrate/20221006.QUYNHNTN/20221026.CreateDatabase.sql:553-568 (DDL Crd_DealUsePromotionDtl), 574-592 (DDL Crd_DealUsePromotion)
- Đích Controller: Controllers/Port/Crd_DealUsePromotionControllerPort.cs:1-447
- Đích Model: Models/Port/Crd_DealUsePromotion.cs:1-331

### Bảng dữ liệu liên quan

#### Bảng Crd_DealUsePromotion (Giao dịch sử dụng ưu đãi)
- Khóa chính: DealUsePrmNo (string, không null)

| Tên cột | Kiểu EF | Khóa | Cho phép null | Ghi chú |
| :--- | :--- | :--- | :--- | :--- |
| DealUsePrmNo | string | PK | Không | Mã lần giao dịch ưu đãi |
| NetworkID | string? | | Có | Mã mạng lưới |
| OrgID | string? | | Có | Mã tổ chức |
| DLCPCode | string? | | Có | Mã đại lý thực hiện |
| DealSerRONo | string? | | Có | Số RO dịch vụ |
| UsePrmDTime | DateTime? | | Có | Thời gian sử dụng dịch vụ |
| ProjectCode | string? | | Có | Mã dự án (mặc định LOYALTY) |
| CardNo | string? | | Có | Số thẻ hội viên |
| CardType | string? | | Có | Mã hạng thẻ |
| LogLUDTimeUTC | DateTime? | | Có | Thời điểm cập nhật UTC |
| LogLUBy | string? | | Có | Người cập nhật |

#### Bảng Crd_DealUsePromotionDtl (Chi tiết sử dụng ưu đãi)
- Khóa chính kết hợp: (DealUsePrmNo, PrProgramCode)

| Tên cột | Kiểu EF | Khóa | Cho phép null | Ghi chú |
| :--- | :--- | :--- | :--- | :--- |
| DealUsePrmNo | string | PK | Không | Mã lần giao dịch ưu đãi |
| PrProgramCode | string | PK | Không | Mã chương trình ưu đãi |
| NetworkID | string? | | Có | Mã mạng lưới |
| OrgID | string? | | Có | Mã tổ chức |
| QtyPr | double? | | Có | Số lượng ưu đãi |
| QtyPrUsed | double? | | Có | Số lượng ưu đãi đã sử dụng |
| QtyPrChTotal | double? | | Có | Số lượng ưu đãi sử dụng lần này |
| LogLUDTimeUTC | DateTime? | | Có | Thời điểm cập nhật UTC |
| LogLUBy | string? | | Có | Người cập nhật |

#### Bảng Seq_DealUsePrmNo (Sequence sinh mã DealUsePrmNo)
- Khóa chính: AutoId (long, tự tăng).
- Định dạng sinh mã: {0}DEALUSEPRMNO.{3}.{1:00000}{2}, chu kỳ tối đa 100000.

## 2. Chi tiết từng endpoint

### 2.1 POST /api/crd_dealusepromotion/save

- Route: /api/crd_dealusepromotion/save (Controllers/Port/Crd_DealUsePromotionControllerPort.cs:175)
- HTTP Method: POST
- Chức năng: Lưu thông tin giao dịch sử dụng ưu đãi và cập nhật số dư/lịch sử giao dịch thẻ.

#### Bảng tham số request (Crd_DealUsePromotionSaveReq)

| Tên trường | Kiểu | Bắt buộc? | Mặc định | Ý nghĩa |
| :--- | :--- | :--- | :--- | :--- |
| Crd_DealUsePromotion | Crd_DealUsePromotionInput? | Có | null | Đối tượng thông tin chung của giao dịch ưu đãi |
| Crd_DealUsePromotion.DLCPCode | string? | Có | null | Mã đại lý thực hiện giao dịch |
| Crd_DealUsePromotion.CardNo | string? | Có | null | Số thẻ sử dụng ưu đãi |
| Crd_DealUsePromotion.CardType | string? | Không | null | Mã hạng thẻ (nếu để trống lấy từ thông tin thẻ) |
| Crd_DealUsePromotion.UsePrmDTime | string? | Không | null | Thời gian sử dụng định dạng yyyy-MM-dd HH:mm:ss |
| Crd_DealUsePromotion.DealUsePrmNo | string? | Không | null | Mã giao dịch (hệ thống tự sinh qua sequence) |
| Lst_Crd_DealUsePromotionDtl | List<Crd_DealUsePromotionDtlInput>? | Có | null | Danh sách các chương trình ưu đãi áp dụng trong lần này |
| Lst_Crd_DealUsePromotionDtl[].PrProgramCode | string? | Có | null | Mã chương trình ưu đãi |
| Lst_Crd_DealUsePromotionDtl[].QtyPr | double? | Không | null | Số lượng ưu đãi ban đầu |
| Lst_Crd_DealUsePromotionDtl[].QtyPrUsed | double? | Không | null | Số lượng ưu đãi đã dùng trước đó |
| Lst_Crd_DealUsePromotionDtl[].QtyPrChTotal | double? | Có | null | Số lượng ưu đãi sử dụng lần này (phải > 0) |

#### Quy tắc xử lý theo thứ tự
1. Kiểm tra payload (Controllers/Port/Crd_DealUsePromotionControllerPort.cs:178-181):
   - Nếu body rỗng hoặc req.Crd_DealUsePromotion là null, trả về lỗi Crd_DealUsePromotion_Save (Check.strJson).
2. Sinh mã số giao dịch tự động (Controllers/Port/Crd_DealUsePromotionControllerPort.cs:186-188):
   - Sinh số sequence tiếp theo từ bảng Seq_DealUsePrmNo qua hàm PortSeq.NextAsync.
   - Định dạng mã: {0}DEALUSEPRMNO.{3}.{1:00000}{2} và gán vào input.DealUsePrmNo.
3. Chuẩn hóa dữ liệu (Controllers/Port/Crd_DealUsePromotionControllerPort.cs:191-202):
   - Chuẩn hóa chuỗi bằng hàm StdParam (Trim và viết hoa).
   - Chuẩn hóa chuỗi thời gian bằng hàm StdDTime (định dạng yyyy-MM-dd HH:mm:ss).
4. Mở Transaction cơ sở dữ liệu (Controllers/Port/Crd_DealUsePromotionControllerPort.cs:205).
5. Guard 1 - Kiểm tra mã giao dịch chưa tồn tại (Controllers/Port/Crd_DealUsePromotionControllerPort.cs:211-217):
   - Tìm kiếm theo DealUsePrmNo trong Crd_DealUsePromotion. Nếu đã tồn tại, rollback và trả về mã lỗi ErridnLoyalty.Crd_DealUsePromotion_CheckDB_DealUsePrmNoExist.
6. Guard 2 - Kiểm tra thẻ hội viên Crd_Card (Controllers/Port/Crd_DealUsePromotionControllerPort.cs:221-260):
   - Tìm kiếm CardNo trong Crd_Card. Nếu không tìm thấy, trả về ErridnLoyalty.Crd_Card_CheckDB_CardNotFound.
   - Kiểm tra trạng thái CardStatus: nếu khác "APPROVE", trả về ErridnLoyalty.Crd_Card_CheckDB_CardStatusNotMatched.
   - Kiểm tra ngày hiệu lực thẻ: nếu EffDateStart > ngày hiện tại, trả về ErridnLoyalty.Crd_Card_CheckDB_InvalidEffDateStartAfterSysDate.
   - Nếu EffDateEnd < ngày hiện tại, trả về ErridnLoyalty.Crd_Card_CheckDB_InvalidEffDateEndBeforeSysDate.
7. Guard 3 - Kiểm tra đại lý Mst_DealerCP (Controllers/Port/Crd_DealUsePromotionControllerPort.cs:264-280):
   - Tìm kiếm DLCPCode trong Mst_DealerCP. Nếu không tìm thấy, trả về ErridnLoyalty.Mst_DealerCP_CheckDB_DLNotFound.
   - Nếu FlagActive khác "1", trả về ErridnLoyalty.Mst_DealerCP_CheckDB_FlagActiveNotMatched.
8. Guard 4 - Kiểm tra danh sách chi tiết (Controllers/Port/Crd_DealUsePromotionControllerPort.cs:283-287):
   - Nếu req.Lst_Crd_DealUsePromotionDtl là null, trả về ErridnLoyalty.Crd_DealUsePromotion_SaveX_Input_Crd_DealUsePromotionDtlTblNotFound.
9. Guard 5 - Duyệt kiểm tra từng dòng ưu đãi chi tiết (Controllers/Port/Crd_DealUsePromotionControllerPort.cs:296-353):
   - Nếu PrProgramCode rỗng hoặc không tìm thấy trong Mst_PromotionProgram, trả về ErridnLoyalty.Mst_PromotionProgram_CheckDB_PrProgramCodeNotFound.
   - Nếu chương trình có FlagActive khác "1", trả về ErridnLoyalty.Mst_PromotionProgram_CheckDB_FlagActiveNotMatched.
   - Kiểm tra thời hạn hiệu lực chương trình (EffDateStart <= hiện tại <= EffDateEnd). Nếu không hợp lệ, trả về ErridnLoyalty.Mst_BirthPolicy_CheckDB_FlagShowNotMatched (giữ nguyên mã lỗi từ nguồn).
   - Kiểm tra số lượng ưu đãi sử dụng QtyPrChTotal: nếu <= 0, trả về ErridnLoyalty.Crd_DealUsePromotion_Save_Input_InvalidQtyPrChTotal.
10. Lưu dữ liệu giao dịch và chi tiết (Controllers/Port/Crd_DealUsePromotionControllerPort.cs:356-376):
    - Thêm bản ghi Crd_DealUsePromotion và danh sách Crd_DealUsePromotionDtl vào DBContext.
    - Lưu thay đổi tạm thời qua SaveChangesAsync.
11. Guard 6 - Kiểm tra hạn mức theo hạng thẻ (Controllers/Port/Crd_DealUsePromotionControllerPort.cs:380-403):
    - Tính tổng số lượng ưu đãi đã dùng của thẻ cho từng chương trình (tính cả lần lưu hiện tại).
    - Tra cứu hạn mức Qty trong bảng Mst_PromotionProgramDtl theo chương trình và hạng thẻ.
    - Nếu tổng số lượng dùng vượt quá hạn mức Qty quy định, rollback và trả về ErridnLoyalty.Crd_DealUsePromotion_Save_Input_InvalidQtyPrChTotal.
12. Guard 7 - Kiểm tra quy định tần suất sử dụng trong ngày (Controllers/Port/Crd_DealUsePromotionControllerPort.cs:407-440):
    - Tính tổng số lượng ưu đãi của cùng hội viên, cùng chương trình trong cùng một ngày.
    - Nếu tổng số lượng dùng trong ngày > 1, rollback và trả về ErridnLoyalty.Crd_DealUsePromotion_Save_Input_QtyPrChTotalOver.
13. Xử lý nghiệp vụ bổ sung (Side-effects) (Controllers/Port/Crd_DealUsePromotionControllerPort.cs:444-517):
    - Thêm bản ghi lịch sử vào Crd_CardTransaction cho từng dòng chi tiết với DealPointType = "PrProgram", FunctionName = "Crd_DealUsePromotion_PerformX", FlagPr = "1".
    - Lấy tham số UNITPOINTTOMONEY từ Mst_ParamSys gán cho ParamAmountUnit.
    - Cập nhật thời gian LogLUDTimeUTC và người sửa LogLUBy của hội viên tương ứng trong Crd_Member.
14. Commit transaction và hoàn tất (Controllers/Port/Crd_DealUsePromotionControllerPort.cs:519-523):
    - Commit transaction cơ sở dữ liệu.
    - Trả về HTTP 200 OK kèm payload { Success = true, Data = null }.

#### Bảng mã lỗi

| Mã lỗi | Điều kiện phát sinh |
| :--- | :--- |
| Crd_DealUsePromotion_Save | Body rỗng hoặc trường Crd_DealUsePromotion bị null (Controllers/Port/Crd_DealUsePromotionControllerPort.cs:180) |
| Crd_DealUsePromotion_CheckDB_DealUsePrmNoExist | Mã giao dịch tự sinh đã tồn tại trong bảng Crd_DealUsePromotion (Controllers/Port/Crd_DealUsePromotionControllerPort.cs:216) |
| Crd_Card_CheckDB_CardNotFound | Số thẻ CardNo không tìm thấy trong bảng Crd_Card (Controllers/Port/Crd_DealUsePromotionControllerPort.cs:226) |
| Crd_Card_CheckDB_CardStatusNotMatched | Trạng thái thẻ CardStatus khác APPROVE (Controllers/Port/Crd_DealUsePromotionControllerPort.cs:234) |
| Crd_Card_CheckDB_InvalidEffDateStartAfterSysDate | Ngày bắt đầu hiệu lực EffDateStart lớn hơn ngày hiện tại (Controllers/Port/Crd_DealUsePromotionControllerPort.cs:248) |
| Crd_Card_CheckDB_InvalidEffDateEndBeforeSysDate | Ngày kết thúc hiệu lực EffDateEnd nhỏ hơn ngày hiện tại (Controllers/Port/Crd_DealUsePromotionControllerPort.cs:256) |
| Mst_DealerCP_CheckDB_DLNotFound | Mã đại lý DLCPCode không tồn tại trong Mst_DealerCP (Controllers/Port/Crd_DealUsePromotionControllerPort.cs:269) |
| Mst_DealerCP_CheckDB_FlagActiveNotMatched | Đại lý có cờ kích hoạt FlagActive khác 1 (Controllers/Port/Crd_DealUsePromotionControllerPort.cs:276) |
| Crd_DealUsePromotion_SaveX_Input_Crd_DealUsePromotionDtlTblNotFound | Danh sách chi tiết Lst_Crd_DealUsePromotionDtl bị null (Controllers/Port/Crd_DealUsePromotionControllerPort.cs:286) |
| Mst_PromotionProgram_CheckDB_PrProgramCodeNotFound | Mã chương trình PrProgramCode rỗng hoặc không tồn tại (Controllers/Port/Crd_DealUsePromotionControllerPort.cs:302, :310) |
| Mst_PromotionProgram_CheckDB_FlagActiveNotMatched | Chương trình có FlagActive khác 1 (Controllers/Port/Crd_DealUsePromotionControllerPort.cs:317) |
| Mst_BirthPolicy_CheckDB_FlagShowNotMatched | Chương trình hết hiệu lực so với ngày hiện tại (mã lỗi khớp nguồn cũ) (Controllers/Port/Crd_DealUsePromotionControllerPort.cs:329) |
| Crd_DealUsePromotion_Save_Input_InvalidQtyPrChTotal | Số lượng QtyPrChTotal <= 0 hoặc vượt tổng hạn mức quy định theo hạng thẻ (Controllers/Port/Crd_DealUsePromotionControllerPort.cs:340, :399) |
| Crd_DealUsePromotion_Save_Input_QtyPrChTotalOver | Tổng số lượng ưu đãi sử dụng trong ngày của cùng hội viên và chương trình > 1 (Controllers/Port/Crd_DealUsePromotionControllerPort.cs:432) |

#### Ví dụ Request
Request ghi nhận sử dụng ưu đãi cho thẻ:

    POST /api/crd_dealusepromotion/save
    Content-Type: application/json

    {
        "Crd_DealUsePromotion": {
            "DLCPCode": "DL_HN01",
            "CardNo": "CARD_DEMO_01",
            "CardType": "GOLD",
            "UsePrmDTime": "2026-10-17 08:30:00"
        },
        "Lst_Crd_DealUsePromotionDtl": [
            {
                "PrProgramCode": "PRM_DEMO_01",
                "QtyPr": 1.0,
                "QtyPrUsed": 0.0,
                "QtyPrChTotal": 1.0
            }
        ]
    }

#### Ví dụ Response
Chưa có lần chạy mẫu

---

### 2.2 POST /api/crd_dealusepromotion/search

- Route: /api/crd_dealusepromotion/search (Controllers/Port/Crd_DealUsePromotionControllerPort.cs:535)
- HTTP Method: POST
- Chức năng: Tìm kiếm phân trang danh sách thẻ hội viên hợp lệ (CardStatus = APPROVE) kèm thông tin chi tiết hội viên.

#### Bảng tham số request (Crd_DealUsePromotionSearchReq)

| Tên trường | Kiểu | Bắt buộc? | Mặc định | Ý nghĩa |
| :--- | :--- | :--- | :--- | :--- |
| PhoneNo | string? | Không | null | Số điện thoại hội viên cần tìm kiếm |
| MemberNo | string? | Không | null | Mã hội viên cần tìm kiếm |
| Ft_PageIndex | int | Không | 0 | Chỉ số trang (bắt đầu từ 0) |
| Ft_PageSize | int | Không | 10 | Số bản ghi trên mỗi trang |

#### Quy tắc xử lý theo thứ tự
1. Khởi tạo đối tượng request: Nếu body gửi lên là null, tự động khởi tạo mặc định với Ft_PageIndex = 0, Ft_PageSize = 10 (Controllers/Port/Crd_DealUsePromotionControllerPort.cs:537).
2. Kết hợp bảng và điều kiện cứng (Controllers/Port/Crd_DealUsePromotionControllerPort.cs:541-544):
   - Thực hiện INNER JOIN giữa Crd_Card và Crd_Member trên trường MemberNo.
   - Áp dụng điều kiện cứng cc.CardStatus == "APPROVE".
3. Lọc PhoneNo (Controllers/Port/Crd_DealUsePromotionControllerPort.cs:547-551):
   - Nếu PhoneNo có giá trị, lọc trên trường PhoneNo của Crd_Member với toán tử so khớp chính xác chuỗi không phân biệt hoa/thường (PortQuery.Equal).
4. Lọc MemberNo (Controllers/Port/Crd_DealUsePromotionControllerPort.cs:554-558):
   - Nếu MemberNo có giá trị, lọc trên trường MemberNo của Crd_Member với toán tử so khớp chính xác chuỗi không phân biệt hoa/thường (PortQuery.Equal).
5. Sắp xếp kết quả (Controllers/Port/Crd_DealUsePromotionControllerPort.cs:561):
   - Sắp xếp theo cc.LogLUDTimeUTC giảm dần (desc), sau đó cc.CardNo giảm dần (desc).
6. Đếm tổng số bản ghi: Đếm toàn bộ số lượng bản ghi phù hợp điều kiện lọc (Controllers/Port/Crd_DealUsePromotionControllerPort.cs:564).
7. Tính toán phân trang và chống tràn số (Controllers/Port/Crd_DealUsePromotionControllerPort.cs:567-593):
   - Trường hợp Ft_PageSize <= 0, Ft_PageIndex < 0 hoặc tổng số bản ghi bằng 0: DataList trả về danh sách rỗng, PageCount trả về 0.
   - Trường hợp hợp lệ: tính recordStart = pageIndex * pageSize, thực hiện Skip và Take tương ứng trên truy vấn.
8. Ánh xạ dữ liệu trả về (Controllers/Port/Crd_DealUsePromotionControllerPort.cs:592, :604-673):
   - Chuyển đổi dữ liệu sang Crd_DealUsePromotionSearchItem bao gồm đầy đủ các trường của Crd_Card, thông tin mở rộng của Crd_Member và số thứ tự MyIdxSeq.
9. Đóng gói kết quả: Trả về HTTP 200 kèm đối tượng PageInfo trong cấu trúc { Success = true, Data = pageInfo } (Controllers/Port/Crd_DealUsePromotionControllerPort.cs:595-602).

#### Bảng mã lỗi

| Mã lỗi | Điều kiện phát sinh |
| :--- | :--- |
| Không có | Endpoint truy vấn không phát sinh mã lỗi ErridnLoyalty.* trong luồng xử lý chuẩn |

#### Ví dụ Request
Tìm kiếm theo số điện thoại:

    POST /api/crd_dealusepromotion/search
    Content-Type: application/json

    {
        "PhoneNo": "0988000001",
        "MemberNo": null,
        "Ft_PageIndex": 0,
        "Ft_PageSize": 10
    }

Tìm kiếm toàn bộ danh sách thẻ có trạng thái APPROVE:

    POST /api/crd_dealusepromotion/search
    Content-Type: application/json

    {}

#### Ví dụ Response
Chưa có lần chạy mẫu

#### Hành vi đặc biệt
- Tự động sinh mã DealUsePrmNo theo chuỗi số (sequence) tăng dần trước khi mở giao dịch cơ sở dữ liệu.
- Lưu lịch sử và cập nhật đồng thời nhiều bảng liên quan trong một Transaction: Crd_DealUsePromotion, Crd_DealUsePromotionDtl, Crd_CardTransaction và cập nhật thời gian ở Crd_Member.
- Kiểm tra chặt chẽ điều kiện chỉ cho phép sử dụng ưu đãi tối đa 1 lần/ngày đối với mỗi chương trình và mỗi hội viên.
- Khi Ft_PageSize <= 0, danh sách DataList trả về mảng rỗng [] và PageCount tính bằng 0 trong khi ItemCount vẫn giữ nguyên tổng số bản ghi tìm thấy.
- Endpoint Search chỉ lấy các thẻ có CardStatus bằng "APPROVE".

## 3. Khác biệt so với nguồn & GAP

- Nguồn gốc mã lỗi sao chép: Tại bước kiểm tra thời hạn chương trình ưu đãi (Guard 5), hệ thống ném mã lỗi `Mst_BirthPolicy_CheckDB_FlagShowNotMatched` thay vì mã lỗi liên quan đến PromotionProgram. Đây là mã lỗi gốc của hệ thống cũ được giữ nguyên để bảo đảm tính tương thích ngược (Bài học A.13).
- Phân quyền và chứng thực: Chưa kích hoạt phân quyền kiểm tra session người dùng, mã người dùng cập nhật lấy từ header `WAUserCode` hoặc mặc định là "port".
- Ghi log hệ thống: Chưa tích hợp hệ thống lưu vết log tập trung của idn.Loyalty cũ.

## 4. Nhật ký thay đổi

- Hạng mục #022: Triển khai 1:1 action Save ghi nhận sử dụng ưu đãi Crd_DealUsePromotion (commit e126be3).
- Hạng mục #023: Triển khai 1:1 action Search tìm kiếm thẻ áp dụng ưu đãi Crd_DealUsePromotion (commit e126be3).
