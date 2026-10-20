# Tài liệu API: Crd_MemberController

Phạm vi: Crd_MemberController (POST /api/crd_member/search, POST /api/crd_member/getbymemberno, POST /api/crd_member/getbyphoneno)
Cập nhật: 2026-10-17
Phiên bản mã: 259729c
Trạng thái: Nháp — chờ review

## 1. Tóm tắt

Cung cấp các chức năng tìm kiếm và tra cứu chi tiết thông tin hội viên thẻ (CrdMember) kết hợp thông tin thẻ phát hành lần đầu (CrdCard với CardSourceCode = 'NEW') hoặc thẻ phát hành gần nhất. Hỗ trợ tìm kiếm phân trang đa tiêu chí, tra cứu chi tiết theo mã hội viên và tra cứu danh sách hội viên hoạt động theo số điện thoại kèm đầy đủ thông tin địa giới hành chính, đại lý và hình thức mua hàng.

### Tệp nguồn và tệp đích
- Nguồn Gate:
  - 13.ClientGate/V10/idn.Loyalty.WebAPI/Controllers/Crd_MemberController.cs:30-140 (Search)
  - 13.ClientGate/V10/idn.Loyalty.WebAPI/Controllers/Crd_MemberController.cs:142-217 (GetByMemberNo)
  - 13.ClientGate/V10/idn.Loyalty.WebAPI/Controllers/Crd_MemberController.cs:219-291 (GetByPhoneNo)
  - 13.ClientGate/V10/idn.Loyalty.WebAPI/Controllers/Crd_MemberController.cs:1494-1558 (strWhereClause_Crd_Member)
  - 13.ClientGate/V10/idn.Loyalty.WebAPI/Controllers/Crd_MemberController.cs:1560-1586 (strWhereClause_Crd_MemberFlagShow)
- Nguồn API:
  - 11.BackEnd/V10/idn.Loyalty.Biz.Web/Controllers/CrdMemberController.cs:97-166 (WA_Crd_Member_GetRegister)
  - 11.BackEnd/V10/idn.Loyalty.Biz.Web/Controllers/CrdMemberController.cs:245-314 (WA_Crd_Member_Get)
- Nguồn Biz / SQL:
  - 11.BackEnd/V10/idn.Loyalty.Biz/Card.cs:19760-19858 (WAS_Crd_Member_GetRegister), 19860-20012 (Crd_Member_GetRegister), 20303-20600 (Crd_Member_GetRegisterX_New20231019 — bản LIVE)
  - 11.BackEnd/V10/idn.Loyalty.Biz/Card.cs:11097-11195 (WAS_Crd_Member_Get), 11197-11349 (Crd_Member_Get), 11351-11573 (Crd_Member_GetX)
- Đích Controller: Controllers/Port/Crd_MemberControllerPort.cs:1-665
- Đích Model: Models/Port/Crd_Member.cs:1-508, Models/PortLoyalty.cs (CrdMember)

### Bảng dữ liệu liên quan

#### Bảng chính: Crd_Member (CrdMember)
- Thực thể: CrdMember (Models/PortLoyalty.cs)
- Khóa chính: MemberNo (string, không null)
- Tái sử dụng thực thể CrdMember có sẵn, không định nghĩa thêm entity trùng tên (Models/Port/Crd_Member.cs:11-13).

#### Bảng liên kết: Crd_Card (CrdCard)
- Thực thể: CrdCard (Models/PortLoyalty.cs)
- Khóa chính: CardNo (string, không null)
- Khóa ngoại logic: MemberNo tham chiếu đến CrdMember.MemberNo.

#### Các bảng danh mục tra cứu kèm theo
- Mst_Province: Tra cứu thông tin Tỉnh/Thành phố theo ProvinceCode (Card.cs:20533-20534).
- Mst_District: Tra cứu thông tin Quận/Huyện theo cặp ProvinceCode và DistrictCode (Card.cs:20535-20537).
- Mst_DealerCP: Tra cứu đại lý đăng ký (DLCodeRegis) và đại lý đặc cách (DLCodeExceptionally) (Card.cs:20538-20541).
- Mst_PurchaseType: Tra cứu tên hình thức mua thẻ theo PurchaseType (Card.cs:20544-20545).
- Map_QueryDealer_Member: Bảng phân quyền dữ liệu đại lý theo MemberNo và DLCPCode (Gate:1548-1551, Biz:11417-11420).

## 2. Chi tiết từng endpoint

### 2.1 POST /api/crd_member/search

- Route: /api/crd_member/search (Controllers/Port/Crd_MemberControllerPort.cs:189)
- HTTP Method: POST
- Chức năng: Tìm kiếm phân trang danh sách hội viên kèm thông tin thẻ NEW theo nhiều tiêu chí lọc.

#### Bảng tham số request (Crd_MemberSearchReq)

| Tên trường | Kiểu | Bắt buộc? | Mặc định | Ý nghĩa |
| :--- | :--- | :--- | :--- | :--- |
| OrgID | string? | Không | null | Mã tổ chức (nhận vào nhưng không lọc trong mệnh đề WHERE) (Gate:30, :1494-1558) |
| RegistrationDateFrom | string? | Không | null | Ngày đăng ký hội viên từ (yyyy-MM-dd) (Gate:30) |
| RegistrationDateTo | string? | Không | null | Ngày đăng ký hội viên đến (yyyy-MM-dd) (Gate:30) |
| MemberActiveDateFrom | string? | Không | null | Ngày kích hoạt hội viên từ (yyyy-MM-dd) (Gate:30) |
| MemberActiveDateTo | string? | Không | null | Ngày kích hoạt hội viên đến (yyyy-MM-dd) (Gate:30) |
| DLCodeRegis | string? | Không | null | Mã đại lý đăng ký hội viên (Gate:30) |
| PhoneNo | string? | Không | null | Số điện thoại hội viên (Gate:30) |
| MemberNo | string? | Không | null | Mã hội viên (Gate:30) |
| RegisStatus | string? | Không | null | Trạng thái đăng ký: P, A1, A2, F, C (Gate:31) |
| CarNo | string? | Không | null | Biển số xe (Gate:31) |
| MemberName | string? | Không | null | Tên hội viên (so khớp chính xác =, không dùng LIKE) (Gate:31, :1536-1539) |
| VIN | string? | Không | null | Số khung VIN xe (Gate:31) |
| cm_FlagShow | string? | Không | null | Cờ trạng thái: "1" (APPROVE), "0" (CANCEL), "-1" (PENDING) (Gate:31, :1560-1586) |
| SourceRegisType | string? | Không | null | Nguồn đăng ký mở thẻ: ON/OFF (Gate:31) |
| MemberStatus | string? | Không | null | Trạng thái hội viên: APPROVE, PENDING, CANCEL (Gate:1544) |
| DLCPCode | string? | Không | null | Mã đại lý tra cứu theo Map_QueryDealer_Member (Gate:1548) |
| Ft_PageIndex | int | Không | 0 | Chỉ số trang, bắt đầu từ 0 (Gate:31) |
| Ft_PageSize | int | Không | 10 | Kích thước trang (Gate:31) |

#### Quy tắc xử lý theo thứ tự
1. Khởi tạo đối tượng request mặc định nếu payload gửi lên null (Controllers/Port/Crd_MemberControllerPort.cs:192).
2. Xây dựng truy vấn cơ sở INNER JOIN giữa Crd_Member và Crd_Card với điều kiện cc.CardSourceCode == "NEW" (Card.cs:20526-20530, Controllers/Port/Crd_MemberControllerPort.cs:197-200).
3. Áp dụng các điều kiện lọc theo strWhereClause_Crd_Member (Gate:1494-1558, Controllers/Port/Crd_MemberControllerPort.cs:205-298):
   - MemberNo: so khớp bằng chính xác không phân biệt hoa thường qua PortQuery.Equal.
   - CarNo: so khớp bằng chính xác không phân biệt hoa thường qua PortQuery.Equal.
   - RegistrationDateFrom: lọc RegistrationDate >= regFrom.Date.
   - RegistrationDateTo: lọc RegistrationDate <= cuối ngày (23:59:59).
   - MemberActiveDateFrom: lọc (MemberActiveDate ?? CardActiveDate) >= actFrom.Date.
   - MemberActiveDateTo: lọc (MemberActiveDate ?? CardActiveDate) <= cuối ngày.
   - DLCodeRegis: so khớp bằng chính xác không phân biệt hoa thường qua PortQuery.Equal.
   - PhoneNo: so khớp bằng chính xác không phân biệt hoa thường qua PortQuery.Equal.
   - RegisStatus: so khớp bằng chính xác không phân biệt hoa thường qua PortQuery.Equal.
   - MemberName: so khớp bằng chính xác "=" không phân biệt hoa thường qua PortQuery.Equal (không dùng LIKE).
   - VIN: so khớp bằng chính xác không phân biệt hoa thường qua PortQuery.Equal.
   - MemberStatus: so khớp bằng chính xác không phân biệt hoa thường qua PortQuery.Equal.
   - DLCPCode: kiểm tra tồn tại bản ghi trong Map_QueryDealer_Member có MemberNo trùng khớp và DLCPCode khớp chính xác qua PortQuery.Equal.
   - SourceRegisType: so khớp bằng chính xác không phân biệt hoa thường qua PortQuery.Equal.
   - cm_FlagShow: chuyển đổi giá trị sang lọc MemberStatus: "1" -> "APPROVE", "0" -> "CANCEL", "-1" -> "PENDING".
   - OrgID: không tham gia vào điều kiện lọc WHERE.
4. Đếm tổng số bản ghi thỏa mãn điều kiện lọc (itemCount = await query.CountAsync()) (Card.cs:20373, Controllers/Port/Crd_MemberControllerPort.cs:301).
5. Xử lý phân trang và chống tràn số (Controllers/Port/Crd_MemberControllerPort.cs:304-332):
   - Nếu Ft_PageSize <= 0, Ft_PageIndex < 0 hoặc itemCount == 0: trả về danh sách rỗng, PageCount = 0 khi Ft_PageSize == 0, ItemCount giữ nguyên tổng số bản ghi.
   - Sắp xếp tăng dần theo cm.MemberNo (Card.cs:20369, 20546).
   - Phân trang bằng Skip và Take với giới hạn int.MaxValue.
6. Tra cứu danh mục và ánh xạ chi tiết từng dòng (BuildMemberItemsAsync, Controllers/Port/Crd_MemberControllerPort.cs:351-499):
   - Nạp các danh mục liên quan trong danh sách phân trang: Mst_Province, Mst_District, Mst_DealerCP, Mst_PurchaseType.
   - Tính toán cm_FlagShow: PENDING -> "-1", APPROVE -> "1", CANCEL -> "0" (Card.cs:20505-20511).
   - Chuyển đổi RegisFinishDTimeUTC sang chuỗi ngày giờ địa phương (+7 tiếng, yyyy-MM-dd HH:mm:ss) nếu có giá trị.
   - Gán trường MemberActiveDate lấy từ cc.CardActiveDate (Card.cs:20487).
   - Đánh số thứ tự MyIdxSeq = recordStart + i.
7. Đóng gói kết quả: Trả về HTTP 200 kèm cấu trúc { Success = true, Data = pageInfo } (Controllers/Port/Crd_MemberControllerPort.cs:342-348).

#### Bảng mã lỗi
Endpoint là truy vấn phân trang, không phát sinh mã lỗi ErridnLoyalty.* trong luồng xử lý chuẩn.

| Mã lỗi | Điều kiện phát sinh |
| :--- | :--- |
| Không có | Controller không định nghĩa mã ErridnLoyalty.* cho action này |

#### Ví dụ Request
Tìm kiếm theo số điện thoại và phân trang:

    POST /api/crd_member/search
    Content-Type: application/json

    {
        "OrgID": null,
        "RegistrationDateFrom": null,
        "RegistrationDateTo": null,
        "MemberActiveDateFrom": null,
        "MemberActiveDateTo": null,
        "DLCodeRegis": "DL3901",
        "PhoneNo": "0901000001",
        "MemberNo": null,
        "RegisStatus": null,
        "CarNo": null,
        "MemberName": null,
        "VIN": null,
        "cm_FlagShow": "1",
        "SourceRegisType": null,
        "MemberStatus": null,
        "DLCPCode": null,
        "Ft_PageIndex": 0,
        "Ft_PageSize": 10
    }

Tìm kiếm toàn bộ danh sách mặc định:

    POST /api/crd_member/search
    Content-Type: application/json

    {}

#### Ví dụ Response
Chưa có lần chạy mẫu

---

### 2.2 POST /api/crd_member/getbymemberno

- Route: /api/crd_member/getbymemberno (Controllers/Port/Crd_MemberControllerPort.cs:510)
- HTTP Method: POST
- Chức năng: Lấy thông tin chi tiết một hội viên đang hoạt động (MemberStatus = 'APPROVE') theo mã hội viên.

#### Bảng tham số request (Crd_MemberGetByMemberNoReq)

| Tên trường | Kiểu | Bắt buộc? | Mặc định | Ý nghĩa |
| :--- | :--- | :--- | :--- | :--- |
| MemberNo | string? | Có | null | Mã hội viên cần tra cứu (Gate:142) |
| DLCPCode | string? | Không | null | Mã đại lý phân quyền kiểm tra qua Map_QueryDealer_Member (Gate:142) |

#### Quy tắc xử lý theo thứ tự
1. Kiểm tra đầu vào (Gate:163-164, Controllers/Port/Crd_MemberControllerPort.cs:515-518):
   - Nếu req null hoặc MemberNo rỗng/khoảng trắng, dừng xử lý và trả về HTTP 200 kèm Data = { Crd_Member = null }.
2. Truy vấn hội viên CrdMember (Gate:170, Biz:11421, :11542-11548, Controllers/Port/Crd_MemberControllerPort.cs:524-539):
   - Điều kiện cứng: MemberStatus so khớp bằng chính xác "APPROVE".
   - Điều kiện lọc: MemberNo so khớp bằng chính xác không phân biệt hoa thường qua PortQuery.Equal.
   - Nếu có DLCPCode: kiểm tra tồn tại quyền đại lý trong bảng Map_QueryDealer_Member.
   - Sắp xếp theo MemberNo tăng dần và lấy bản ghi đầu tiên (FirstOrDefaultAsync).
3. Kiểm tra kết quả hội viên (Controllers/Port/Crd_MemberControllerPort.cs:540-543):
   - Nếu không tìm thấy, trả về HTTP 200 kèm Data = { Crd_Member = null }.
4. Lấy thông tin thẻ CrdCard mới nhất (Biz:11459-11467, Controllers/Port/Crd_MemberControllerPort.cs:546-549):
   - Lọc CrdCard theo cc.MemberNo = member.MemberNo và sắp xếp theo cc.CreateDTimeUTC giảm dần (desc), lấy bản ghi đầu tiên.
5. Tra cứu danh mục và chuyển đổi dữ liệu (Biz:11498-11510, Controllers/Port/Crd_MemberControllerPort.cs:552-585):
   - Lookup thông tin Tỉnh/TP (Mst_Province), Quận/Huyện (Mst_District), Đại lý đăng ký và đặc cách (Mst_DealerCP), Loại mua xe (Mst_PurchaseType).
   - Format RegisFinishDTimeUTC sang chuỗi ngày giờ địa phương (+7 tiếng, yyyy-MM-dd HH:mm:ss) nếu có giá trị.
   - Gán cờ cm_FlagShow = "1" (do trạng thái hội viên bắt buộc là APPROVE).
6. Đóng gói kết quả: Trả về HTTP 200 kèm payload { Success = true, Data = { Crd_Member = item } } (Controllers/Port/Crd_MemberControllerPort.cs:660).

#### Bảng mã lỗi

| Mã lỗi | Điều kiện phát sinh |
| :--- | :--- |
| Không có | Controller không định nghĩa mã ErridnLoyalty.* cho action này |

#### Ví dụ Request
Tra cứu theo mã hội viên:

    POST /api/crd_member/getbymemberno
    Content-Type: application/json

    {
        "MemberNo": "MB4001",
        "DLCPCode": "DL4001"
    }

#### Ví dụ Response
Chưa có lần chạy mẫu

---

### 2.3 POST /api/crd_member/getbyphoneno

- Route: /api/crd_member/getbyphoneno (Controllers/Port/Crd_MemberControllerPort.cs:674)
- HTTP Method: POST
- Chức năng: Lấy danh sách hội viên đang hoạt động (MemberStatus = 'APPROVE') theo số điện thoại.

#### Bảng tham số request (Crd_MemberGetByPhoneNoReq)

| Tên trường | Kiểu | Bắt buộc? | Mặc định | Ý nghĩa |
| :--- | :--- | :--- | :--- | :--- |
| PhoneNo | string? | Có | null | Số điện thoại hội viên cần tra cứu (Gate:219) |

#### Quy tắc xử lý theo thứ tự
1. Kiểm tra đầu vào (Gate:242, :274-278, Controllers/Port/Crd_MemberControllerPort.cs:679-682):
   - Nếu req null hoặc PhoneNo rỗng/khoảng trắng, dừng xử lý và trả về HTTP 200 kèm Data = { Lst_Crd_Member = null }.
2. Truy vấn danh sách hội viên CrdMember (Gate:247, :1528-1531, Biz:11421, :11542-11548, Controllers/Port/Crd_MemberControllerPort.cs:688-693):
   - Điều kiện cứng: MemberStatus khớp chính xác "APPROVE".
   - Điều kiện lọc: PhoneNo so khớp bằng chính xác qua PortQuery.Equal (không dùng LIKE, không tách dấu phẩy).
   - Sắp xếp tăng dần theo cm.MemberNo.
3. Kiểm tra kết quả danh sách (Controllers/Port/Crd_MemberControllerPort.cs:696-699):
   - Nếu không có bản ghi nào, trả về HTTP 200 kèm Data = { Lst_Crd_Member = null }.
4. Duyệt danh sách hội viên và bổ sung dữ liệu chi tiết (Controllers/Port/Crd_MemberControllerPort.cs:701-831):
   - Với mỗi hội viên, lấy thẻ CrdCard mới nhất theo CreateDTimeUTC giảm dần.
   - Lookup danh mục Mst_Province, Mst_District, Mst_DealerCP, Mst_PurchaseType.
   - Chuyển đổi RegisFinishDTimeUTC sang chuỗi ngày giờ địa phương (+7 tiếng, yyyy-MM-dd HH:mm:ss).
   - Gán cm_FlagShow = "1" và MyIdxSeq = i (0-based index).
5. Đóng gói kết quả: Trả về HTTP 200 kèm payload { Success = true, Data = { Lst_Crd_Member = dataList } } (Controllers/Port/Crd_MemberControllerPort.cs:833).

#### Bảng mã lỗi

| Mã lỗi | Điều kiện phát sinh |
| :--- | :--- |
| Không có | Controller không định nghĩa mã ErridnLoyalty.* cho action này |

#### Ví dụ Request
Tra cứu hội viên theo số điện thoại:

    POST /api/crd_member/getbyphoneno
    Content-Type: application/json

    {
        "PhoneNo": "0901000041"
    }

#### Ví dụ Response
Chưa có lần chạy mẫu

#### Hành vi đặc biệt
- Toàn bộ toán tử lọc chuỗi (kể cả MemberName) áp dụng toán tử bằng "=" chính xác thông qua PortQuery.Equal, không dùng tìm kiếm gần đúng LIKE (Gate:1536-1539).
- Action Search bắt buộc kết hợp INNER JOIN với thẻ có CardSourceCode = 'NEW' (Card.cs:20528-20530). Do đó hội viên chưa có thẻ loại NEW sẽ không xuất hiện trong kết quả Search.
- Action GetByMemberNo và GetByPhoneNo chỉ tra cứu các hội viên có trạng thái hoạt động MemberStatus = 'APPROVE' (Gate:170, :247, Biz:11421).
- Khi danh sách kết quả rỗng hoặc tham số bắt buộc để trống, GetByMemberNo trả về Data.Crd_Member = null và GetByPhoneNo trả về Data.Lst_Crd_Member = null (không trả mảng rỗng).
- Khi Ft_PageSize = 0 trong Search, DataList trả về mảng rỗng [], PageCount tính bằng 0 và ItemCount vẫn giữ nguyên tổng số lượng bản ghi thỏa mãn điều kiện.
- Cột RegisFinishDTimeUTC tự động chuyển sang giờ Việt Nam (+7 tiếng) định dạng yyyy-MM-dd HH:mm:ss trước khi trả về.

## 3. Khác biệt so với nguồn & GAP

- Tham số OrgID: Gate nhận tham số OrgID nhưng thủ tục SQL và hàm dựng WhereClause của nguồn không áp dụng lọc; controller port giữ nguyên tính tương thích không lọc OrgID.
- Các thực thể chưa có sẵn: Các trường liên kết từ bảng Mst_CardSource (mcs_CardSourceName) và Req_MemberRegister (ReqMemberRegisterCodeOS) được gán giá trị null do dự án hiện tại chưa xây dựng các bảng thực thể này.
- Phân quyền và chứng thực: Chưa áp dụng middleware xác thực phiên đăng nhập người dùng; quyền xem đại lý được giả lập thông qua kiểm tra bảng Map_QueryDealer_Member khi có tham số DLCPCode.
- Ghi log hệ thống: Chưa tích hợp cơ chế ghi nhận log cuộc gọi API của idn.Loyalty cũ.

## 4. Nhật ký thay đổi

- Hạng mục #039: Port 1:1 action Search của Crd_MemberController, seeder Crd_MemberSeeder (commit 259729c).
- Hạng mục #040: Port 1:1 action GetByMemberNo của Crd_MemberController, seeder Crd_MemberGetByMemberNoSeeder (commit 259729c).
- Hạng mục #041: Port 1:1 action GetByPhoneNo của Crd_MemberController, seeder Crd_MemberGetByPhoneNoSeeder (commit 259729c).
