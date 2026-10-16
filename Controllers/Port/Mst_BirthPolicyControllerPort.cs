using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MiniLoyalty.Data;
using MiniLoyalty.Models.Port;

namespace MiniLoyalty.Controllers.Port;

// ============================================================================================
// PORT 1:1 — màn danh mục chính sách sinh nhật (Mst_BirthPolicy) của dự án nguồn 2022.7.Loyalty (#70, #71, #72, #73, #74, #75).
//
// Nguồn controller (tầng gate):
//   Mst_BirthPolicyController.cs:23-103    Search -> WA_Mst_BirthPolicy_Get (#70)
//   Mst_BirthPolicyController.cs:104-213   GetAllActive -> WA_Mst_BirthPolicy_Get (#71)
//   Mst_BirthPolicyController.cs:214-290   GetByBirthPolicyNo -> WA_Mst_BirthPolicy_Get (#72)
//   Mst_BirthPolicyController.cs:294-319   strWhereClause_Mst_BirthPolicy (mệnh đề lọc Search / GetAllActive)
//   Mst_BirthPolicyController.cs:320-335   strWhereClause_Mst_BirthPolicy_By_BirthPolicyNo (mệnh đề lọc GetByBirthPolicyNo)
//   Mst_BirthPolicyController.cs:339-434   Save -> WA_Mst_BirthPolicy_Save (#73)
//   Mst_BirthPolicyController.cs:438-530   Update -> WA_Mst_BirthPolicy_Update (#74)
//   Mst_BirthPolicyController.cs:535-600   Delete -> WA_Mst_BirthPolicy_Save (FlagIsDelete = "1", #75)
// Nguồn API:
//   MstBirthPolicyController.cs:168-238    WA_Mst_BirthPolicy_Get
//   MstBirthPolicyController.cs:22-92      WA_Mst_BirthPolicy_Save
//   MstBirthPolicyController.cs:95-165     WA_Mst_BirthPolicy_Update
// Nguồn biz:
//   Master.Loyalty.cs:10606-10719          WAS_Mst_BirthPolicy_Get
//   Master.Loyalty.cs:10720-10876          Mst_BirthPolicy_Get
//   Master.Loyalty.cs:10877-11085          Mst_BirthPolicy_GetX (bản LIVE)
//   Master.Loyalty.cs:10507-10604          WAS_Mst_BirthPolicy_Save
//   Master.Loyalty.cs:10348-10505          Mst_BirthPolicy_Save
//   Master.Loyalty.cs:9675-10346           Mst_BirthPolicy_SaveX (bản LIVE, cả ghi mới #73 và xóa bIsDelete = true #75)
//   Master.Loyalty.cs:11600-11690          WAS_Mst_BirthPolicy_Update
//   Master.Loyalty.cs:11455-11598          Mst_BirthPolicy_Update
//   Master.Loyalty.cs:11093-11453          Mst_BirthPolicy_UpdateX (bản LIVE)
//
// ĐẶC ĐIỂM VÀ QUY TẮC NGHIỆP VỤ NGUỒN:
//   * Endpoint là POST (nguồn gate Mst_BirthPolicyController.cs:23, :104, :214).
//   * Lọc KeyWord: `BirthPolicyNo like '%kw%'` (gate :299-302).
//   * Lọc BirthPolicyNo: `BirthPolicyNo = 'x'` (gate :327).
//   * Lọc FlagActive: danh sách tách dấu phẩy `FlagActive IN ('1','0')` (gate :308-316);
//     dòng `=` (:304-307) BỊ COMMENT ở nguồn nên KHÔNG chạy.
//   * Phân trang: Ft_RecordStart = pageIndex * pageSize, Ft_RecordCount = pageSize (gate :66-67).
//     MyIdxSeq 0-based identity (SQL Master.Loyalty.cs:10937).
//     Thứ tự draft: `order by mrp.LogLUDTimeUTC desc` (:10944). Hòa trùng sắp phụ theo `BirthPolicyNo asc`.
//   * PageSize = 0 -> DataList rỗng (nFilterRecordEnd = -1), PageCount = 0 (gate :78-86).
//   * FlagShow tính lúc select (:10984-10996):
//     - '1' khi `EffDateStart <= @dateSys and @dateSys <= EffDateEnd`
//     - '0' khi ngoài khoảng
//     - NULL nếu ngày null
//     với `@dateSys = DateTime.UtcNow.AddHours(7).ToString("yyyy-MM-dd")` (ngày Việt Nam UTC+7, lúc 00:00:00).
//     Trong C# tính với `var today = DateTime.UtcNow.AddHours(7).Date`.
//   * GetByBirthPolicyNo (#72): [HttpPost("getbybirthpolicyno")], body { BirthPolicyNo }, EmptyBodyBehavior.Allow (gate :214-290).
//     - BirthPolicyNo rỗng -> KHÔNG truy vấn, Data = { Mst_BirthPolicy: null, Lst_Mst_BirthPolicyDtl: null } (:233-234, :273-277).
//     - Ngược lại lọc strWhereClause_Mst_BirthPolicy_By_BirthPolicyNo CHỈ với BirthPolicyNo -> BirthPolicyNo = (:327;
//       nhánh FlagActive :329-332 không truyền -> không lọc FlagActive -> chính sách inactive như BP02 VẪN lấy được).
//       Ft_RecordStart = "0", Ft_RecordCount = "1" (:253-254).
//       Xin CẢ header và Dtl: Rt_Cols_Mst_BirthPolicy = "*", Rt_Cols_Mst_BirthPolicyDtl = "*" (:257-258).
//     - Kết quả (:262-277):
//       Mst_BirthPolicy = header[0] (MyIdxSeq + 12 cột + FlagShow) hoặc null.
//       Lst_Mst_BirthPolicyDtl = danh sách DTL (MyIdxSeq + 10 cột Dtl) hoặc null nếu rỗng.
//     - Biz GetX (:11008-11016): Dtl = t.MyIdxSeq, mrpl.* từ draft inner join Dtl on BirthPolicyNo, order by mrpl.LogLUDTimeUTC desc
//       (hòa trùng: sắp phụ theo CardType asc, ghi comment theo đề bài).
//   * DDL chọn: 20221026.CreateDatabase.sql:399-418 (12 cột, có EffDateEnd, UpdateDTime, UpdateBy)
//     thay vì bản cũ Mst_BirthPolicy.sql:96 thiếu EffDateEnd; khớp với 20260731.z11.Mst_BirthPolicy.Disable.sql.
//   * Mst_BirthPolicyDtl: 10 cột (7 cột gốc DDL :378-394 + 3 cột voucher từ 20260518.z11.RankRN_BirthVoucher.sql:114-130).
//     FK CardType -> Mst_CardType, FK BirthPolicyNo -> Mst_BirthPolicy ghi comment, chưa cấu hình FK navigation EF.
//   * Bỏ qua có chủ đích:
//     - drAbilityOfUser (Master.Loyalty.cs:10920) nguồn gọi nhưng không dùng (:10932 bị comment) -> bỏ qua, ghi comment.
//     - Phân quyền CheckGatewayAuthentication, CheckAuthorize (gate :32-33), Sys_User_CheckAuthorize (:10786-10799):
//       hệ thống đích chưa có hệ phân quyền.
//     - Ghi RequestLog/ReturnLog (:10765-10772, :10866-10873).
// ============================================================================================

/// <summary>
/// Body request của Search (Mst_BirthPolicyController.cs:23).
/// KeyWord, FlagActive, Ft_PageIndex (mặc định 0), Ft_PageSize (mặc định 10).
/// </summary>
public class MstBirthPolicySearchReq
{
    public string? KeyWord { get; set; }
    public string? FlagActive { get; set; }
    public int Ft_PageIndex { get; set; } = 0;
    public int Ft_PageSize { get; set; } = 10;
}

/// <summary>Port 1:1 <c>GetByBirthPolicyNo</c> — body nhận BirthPolicyNo (Mst_BirthPolicyController.cs:214).</summary>
public class MstBirthPolicyGetByBirthPolicyNoReq
{
    public string? BirthPolicyNo { get; set; }
}

/// <summary>
/// DTO header chính sách sinh nhật trong request Save (nguồn gate Mst_BirthPolicyController.cs:339-434, Master.Loyalty.cs:10565-10572).
/// </summary>
public class MstBirthPolicySaveHeaderReq
{
    public string? BirthPolicyNo { get; set; }
    public string? ParamPrType { get; set; }
    public string? BirthPolicyName { get; set; }
    public string? EffDateStart { get; set; }
    public string? FlagActive { get; set; }
    public string? FlagPoint { get; set; }
    public string? FlagVoucher { get; set; }
    public string? Remark { get; set; }
}

/// <summary>
/// DTO chi tiết chính sách sinh nhật trong request Save (nguồn Master.Loyalty.cs:10021-10087).
/// </summary>
public class MstBirthPolicySaveDtlReq
{
    public string? CardType { get; set; }
    public double? Point { get; set; }
    public object? VoucherValue { get; set; }
    public object? VoucherExpireDays { get; set; }
    public string? Remark { get; set; }
}

/// <summary>
/// Body của action Save: { Mst_BirthPolicy: { ... }, Lst_Mst_BirthPolicyDtl: [ ... ] }
/// (nguồn gate Mst_BirthPolicyController.cs:339-434).
/// </summary>
public class MstBirthPolicySaveReq
{
    public MstBirthPolicySaveHeaderReq? Mst_BirthPolicy { get; set; }
    public List<MstBirthPolicySaveDtlReq>? Lst_Mst_BirthPolicyDtl { get; set; }
}

/// <summary>
/// Body của action Update: { Mst_BirthPolicy: { ... }, Lst_Mst_BirthPolicyDtl: [ ... ] }
/// (nguồn gate Mst_BirthPolicyController.cs:438-530).
/// </summary>
public class MstBirthPolicyUpdateReq : MstBirthPolicySaveReq
{
}

/// <summary>Port 1:1 <c>Delete</c> — body nhận BirthPolicyNo (Mst_BirthPolicyController.cs:535, #75).</summary>
public class MstBirthPolicyDeleteReq
{
    public string? BirthPolicyNo { get; set; }
}


[ApiController]
[Route("api/mst_birthpolicy")]
[Produces("application/json")]
public class Mst_BirthPolicyControllerPort : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public Mst_BirthPolicyControllerPort(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    /// <summary>
    /// Port 1:1 <c>Mst_BirthPolicyController.Search</c> (nguồn gate Mst_BirthPolicyController.cs:23-103).
    /// Body JSON nhận KeyWord, FlagActive, Ft_PageIndex, Ft_PageSize, EmptyBodyBehavior.Allow.
    /// </summary>
    [HttpPost("search")]
    public async Task<IActionResult> Search(
        [FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] MstBirthPolicySearchReq? req)
    {
        var r = req ?? new MstBirthPolicySearchReq();
        var pageIndex = Math.Max(0, r.Ft_PageIndex);
        var pageSize = r.Ft_PageSize;

        // Search gọi GetXAsync với start = PageIndex * PageSize, count = PageSize (gate :66-67).
        var start = (long)pageIndex * pageSize;
        var (itemCount, rows) = await GetXAsync(r.KeyWord, r.FlagActive, start, pageSize);

        // PageSize=0 -> DataList rỗng (nguồn: nFilterRecordEnd = start-1, SQL biz + gate :78-86).
        // PageCount = 0 khi DataList rỗng, ngược lại ceil(ItemCount / ps) với ps = PageSize (PageSize=0 thì 10) — gate :78-86.
        var ps = pageSize == 0 ? 10 : pageSize;
        var pageCount = rows.Count == 0 ? 0 : (itemCount % ps == 0 ? itemCount / ps : itemCount / ps + 1);

        return Ok(new
        {
            Success = true,
            Data = new PageInfo<object>(rows, itemCount, pageCount, pageIndex, pageSize)
        });
    }

    /// <summary>
    /// Port 1:1 <c>Mst_BirthPolicyController.GetAllActive</c> (nguồn gate Mst_BirthPolicyController.cs:104-213).
    /// Nguồn gọi <c>strWhereClause_Mst_BirthPolicy</c> với <c>FlagActive = "1"</c> (:130-134) ⇒ lọc
    /// <c>FlagActive IN ('1')</c> (:308-316). <c>OrgID</c> chỉ được gán vào request (:132, :144),
    /// hàm lọc KHÔNG dùng OrgID ⇒ KHÔNG lọc theo OrgID.
    /// Không body: RecordStart = "0", RecordCount = "123456000" (nguồn Ft_RecordStart/Ft_RecordCount :146-147).
    /// Khối Ext/tree (:156-180) bị comment ở nguồn ⇒ KHÔNG port.
    /// Dùng lại GetXAsync của Search (#70): MyIdxSeq + 12 cột + FlagShow.
    /// Trả <c>Ok(new { Success = true, Data = ... })</c> — Data là MẢNG TRẦN, rỗng thì <c>null</c>
    /// (nguồn :127 khởi tạo null, :153-155 chỉ gán khi có dòng, :193-198 trả thẳng listMst_BirthPolicy).
    /// </summary>
    [HttpPost("getallactive")]
    public async Task<IActionResult> GetAllActive()
    {
        // Nguồn: Ft_RecordStart = "0", Ft_RecordCount = "123456000" (gate Mst_BirthPolicyController.cs:146-147).
        // FlagActive = "1" (gate :133) ⇒ lọc IN ('1'); OrgID không lọc.
        var (_, rows) = await GetXAsync(null, "1", 0, 123456000);

        // Nguồn :193-198: Data = listMst_BirthPolicy (mảng trần); rỗng ⇒ null (:127, :153-155).
        return Ok(new { Success = true, Data = rows.Count > 0 ? rows : null });
    }


    /// <summary>
    /// Port 1:1 <c>Mst_BirthPolicyController.GetByBirthPolicyNo</c> (nguồn gate Mst_BirthPolicyController.cs:214-290, #72).
    /// Body JSON nhận BirthPolicyNo, EmptyBodyBehavior.Allow.
    /// BirthPolicyNo rỗng -> KHÔNG truy vấn, Data = { Mst_BirthPolicy: null, Lst_Mst_BirthPolicyDtl: null } (gate :233-234, :273-277).
    /// Ngược lại lọc BirthPolicyNo = (Like + PortQuery.Equal, gate :327 — strWhereClause_Mst_BirthPolicy_By_BirthPolicyNo);
    /// không lọc FlagActive (nhánh FlagActive không truyền ở gate :238-241) -> chính sách inactive (như BP02) VẪN lấy được.
    /// Ft_RecordStart = "0", Ft_RecordCount = "1" (gate :253-254).
    /// Xin cả header và Dtl (Rt_Cols_Mst_BirthPolicy = "*", Rt_Cols_Mst_BirthPolicyDtl = "*", gate :257-258).
    /// Kết quả:
    ///   - Data.Mst_BirthPolicy: header[0] (MyIdxSeq + 12 cột + FlagShow) hoặc null (gate :262-265, :275).
    ///   - Data.Lst_Mst_BirthPolicyDtl: danh sách DTL (MyIdxSeq + 10 cột Dtl gồm VoucherValue, VoucherPointVCLimit, VoucherExpireDays)
    ///     hoặc null nếu rỗng (gate :266-269, :276).
    /// </summary>
    [HttpPost("getbybirthpolicyno")]
    public async Task<IActionResult> GetByBirthPolicyNo(
        [FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] MstBirthPolicyGetByBirthPolicyNoReq? req)
    {
        var birthPolicyNo = req?.BirthPolicyNo;

        // BirthPolicyNo rỗng -> nguồn không truy vấn (:236) và trả cả 2 trường null (:233-234, :273-277).
        if (string.IsNullOrWhiteSpace(birthPolicyNo))
        {
            return Ok(new
            {
                Success = true,
                Data = new
                {
                    Mst_BirthPolicy = (object?)null,
                    Lst_Mst_BirthPolicyDtl = (object?)null
                }
            });
        }

        // Lấy 1 dòng theo BirthPolicyNo (Ft_RecordStart = "0", Ft_RecordCount = "1" — gate :253-254).
        // getDtl = true -> xin thêm danh sách Dtl (Rt_Cols_Mst_BirthPolicyDtl = "*" — gate :258).
        // Nhánh FlagActive không truyền ở gate (:238-241) nên không lọc FlagActive -> chính sách inactive vẫn lấy được.
        var (_, rows, dtlRows) = await GetXAsync(null, null, 0, 1, birthPolicyNoEqual: birthPolicyNo, getDtl: true);

        // Nguồn gate :262-269, :273-277:
        // Mst_BirthPolicy = header[0] hoặc null.
        // Lst_Mst_BirthPolicyDtl = danh sách DTL hoặc null nếu rỗng.
        var objMst_BirthPolicy = rows.Count > 0 ? rows[0] : null;
        var lst_Mst_BirthPolicyDtl = dtlRows.Count > 0 ? dtlRows : null;

        return Ok(new
        {
            Success = true,
            Data = new
            {
                Mst_BirthPolicy = objMst_BirthPolicy,
                Lst_Mst_BirthPolicyDtl = lst_Mst_BirthPolicyDtl
            }
        });
    }

    // ============================================================================================
    // Save — nguồn gate Mst_BirthPolicyController.cs:339-434 (#73)
    //   -> API MstBirthPolicyController.cs:22-92 (WA_Mst_BirthPolicy_Save)
    //   -> BIZ Master.Loyalty.cs:10507-10604 (WAS_Mst_BirthPolicy_Save)
    //   -> :10348-10505 (Mst_BirthPolicy_Save)
    //   -> :9675-10346 (Mst_BirthPolicy_SaveX — bản LIVE).
    //
    // GIỮ ĐÚNG NGUỒN:
    //   * Body JSON: { Mst_BirthPolicy: { ... }, Lst_Mst_BirthPolicyDtl: [ ... ] }
    //   * Body rỗng hoàn toàn -> ServiceException("strJson null") -> mã mặc định Mst_BirthPolicy_Save (KHÔNG tăng seq).
    //   * seq tăng cả khi lỗi: Lấy sequence trong transaction RIÊNG đã commit độc lập (gate Mst_BirthPolicyController.cs:373-397,
    //     SeqType.BirthPolicyNo — Seq.cs:139: bảng Seq_BirthPolicyNo, format "{0}BIRTHPOLICYNO.{3}.{1:00000}{2}", max 100000)
    //     qua bảng Seq_BirthPolicyNo rồi gán vào header (:395). Do đó sequence tăng MỖI LẦN GỌI vượt qua bước body rỗng,
    //     KỂ CẢ KHI LƯU LỖI SAU ĐÓ.
    //     Body {} (thiếu Mst_BirthPolicy) -> NullReference SAU khi lấy seq -> Mst_BirthPolicy_Save (seq ĐÃ tăng).
    //   * Gate ép FlagActive = '1' (:415).
    //   * Chuẩn hóa (Master.Loyalty.cs:9732-9741):
    //     - BirthPolicyNo StdParam (Trim().ToUpper())
    //     - BirthPolicyName StdStr (Trim(), rỗng -> null)
    //     - ParamPrType StdStr (Trim(), rỗng -> null — KHÔNG viết hoa)
    //     - EffDateStart StdDate (yyyy-MM-dd)
    //     - Remark StdStr
    //     - EffDateEnd = 2100-01-01 CỐ ĐỊNH
    //     - FlagActive/FlagPoint/FlagVoucher StdFlag ("1" nếu "1", còn lại "0")
    //     - Convert.ToDateTime(EffDateStart) rồi AddDays(-1) (:9745-9746): thiếu hoặc sai định dạng date -> NÉM lỗi NGAY -> Mst_BirthPolicy_Save.
    //   * Kiểm DÚNG THỨ TỰ:
    //     a. (:9797-9809): (FlagPoint == '1') == (FlagVoucher == '1') (cả hai HOẶC không cái nào) -> Mst_BirthPolicy_SaveX_InvalidFlagPointVoucher.
    //     b. (:9836-9863): tồn tại Mst_BirthPolicy cùng ParamPrType và EffDateStart >= EffDateStart mới -> Mst_BirthPolicy_SaveX_InvalidEffDateStart.
    //     c. (:9867-9873): Mst_ParamPromotionType_CheckDB(ParamPrType, "1", "1") (:13024-13085)
    //        - không có -> Mst_ParamPromotionType_CheckDB_ParamPrTypeNotFound;
    //        - FlagActive không thuộc "1" -> Mst_ParamPromotionType_CheckDB_FlagActiveNotMatched (NULL/rỗng vẫn qua).
    //   * Chi tiết Dtl (:10021-10087):
    //     - Lst Dtl null -> danh sách rỗng (:10542-10543, không lỗi).
    //     - CardType StdParam (Trim().ToUpper()).
    //     - Point/VoucherValue/VoucherExpireDays/Remark giữ nguyên (Remark không trim).
    //     - VoucherValue / VoucherExpireDays khác rỗng -> Convert.ToDouble (sai -> ..._Save);
    //       < 0 -> Mst_BirthPolicy_SaveX_InvalidValue;
    //       VoucherValue > 0 && VoucherExpireDays <= 0 -> Mst_BirthPolicy_SaveX_InvalidValue.
    //     - FK CardType -> Mst_CardType (FK_MstCardTp_MstBirthPolicyDtl) -> GIẢ LẬP: không có -> Mst_BirthPolicy_Save.
    //     - Trùng CardType -> vi phạm PK (BirthPolicyNo, CardType) -> Mst_BirthPolicy_Save (kiểm sau vòng lặp).
    //   * Lưu: 1 transaction duy nhất (:10389, commit :10468), lỗi -> rollback + PortResult.Fail. Thành công -> Data = null.
    //   * Header insert (:10159-10188): BirthPolicyNo, ParamPrType, NetworkID (_config["Biz_NetworkID"] ?? "1001"),
    //     BirthPolicyName, EffDateStart, EffDateEnd, Remark, FlagActive, FlagPoint, FlagVoucher, LogLUDTimeUTC now, LogLUBy "port".
    //     (UpdateDTime/UpdateBy KHÔNG ghi).
    //   * Dtl insert (:10195-10216): BirthPolicyNo, CardType, NetworkID, Point, VoucherValue, VoucherExpireDays, Remark, LogLUDTimeUTC, LogLUBy.
    //     (KHÔNG có VoucherPointVCLimit).
    //   * Cập nhật chương trình TRƯỚC (:10246-10270):
    //     top 1 Mst_BirthPolicy khác BirthPolicyNo mới, CÙNG ParamPrType, order by LogLUDTimeUTC desc
    //     -> EffDateEnd = EffDateStart mới - 1 ngày.
    // ============================================================================================
    [HttpPost("save")]
    public async Task<IActionResult> Save(
        [FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] MstBirthPolicySaveReq? req)
    {
        // 0. Body rỗng / null: gate :349-356 ném ServiceException("strJson null") -> mã mặc định Mst_BirthPolicy_Save.
        // Bước này xảy ra TRƯỚC khi gọi sequence nên KHÔNG tăng seq.
        if (req == null)
        {
            return PortResult.Fail("Mst_BirthPolicy_Save", "Check.strJson", null);
        }

        // 1. Seq tăng cả khi lỗi: Lấy sequence trong transaction RIÊNG đã commit độc lập (gate Mst_BirthPolicyController.cs:373-397,
        // SeqType.BirthPolicyNo — Seq.cs:139: bảng Seq_BirthPolicyNo, format "{0}BIRTHPOLICYNO.{3}.{1:00000}{2}", max 100000).
        // Nhờ transaction riêng, mã sequence đã tăng sẽ không bị rollback kể cả khi bước lưu sau bị lỗi.
        var seq = await PortSeq.NextAsync<Seq_BirthPolicyNo>(_db);
        var birthPolicyNo = PortSeq.Build("{0}BIRTHPOLICYNO.{3}.{1:00000}{2}", "", seq, 100000, "", DateTime.UtcNow);

        // Header null (client gửi `{}` thiếu Mst_BirthPolicy):
        // Nguồn gate :395 gán objMst_BirthPolicy.BirthPolicyNo ném NullReferenceException SAU khi lấy seq -> mã mặc định Mst_BirthPolicy_Save (seq ĐÃ tăng).
        if (req.Mst_BirthPolicy == null)
        {
            return PortResult.Fail("Mst_BirthPolicy_Save", "Check.Mst_BirthPolicy", null);
        }

        var header = req.Mst_BirthPolicy;
        var strBirthPolicyNo = StdParam(birthPolicyNo);
        var strBirthPolicyName = StdStr(header.BirthPolicyName);
        var strParamPrType = StdStr(header.ParamPrType); // StdStr: KHÔNG viết hoa (Master.Loyalty.cs:9737)
        var strRemark = StdStr(header.Remark);
        var strFlagActive = "1"; // Gate ép FlagActive = '1' (:415)
        var strFlagPoint = StdFlag(header.FlagPoint);
        var strFlagVoucher = StdFlag(header.FlagVoucher);

        // 2. Kiểm tra EffDateStart (Master.Loyalty.cs:9745-9746):
        // Dt = Convert.ToDateTime(objEffDateStart);
        // strEffDateEndPre = TUtils.CUtils.StdDate(Dt.AddDays(-1).ToString("yyyy-MM-dd"));
        // Nếu thiếu (Convert.ToDateTime(null) -> MinValue -> AddDays(-1) ném lỗi) hoặc sai định dạng date -> mã mặc định Mst_BirthPolicy_Save.
        DateTime dtEffDateStart;
        try
        {
            if (string.IsNullOrWhiteSpace(header.EffDateStart))
            {
                throw new ArgumentException("EffDateStart is required");
            }
            dtEffDateStart = Convert.ToDateTime(header.EffDateStart);
            var _ = dtEffDateStart.AddDays(-1);
        }
        catch
        {
            return PortResult.Fail("Mst_BirthPolicy_Save", "Check.EffDateStart", header.EffDateStart);
        }

        // 3. Kiểm DÚNG THỨ TỰ:
        // a. (:9797-9809): (FlagPoint == '1') == (FlagVoucher == '1') (cả hai HOẶC không cái nào) -> Mst_BirthPolicy_SaveX_InvalidFlagPointVoucher
        if ((strFlagPoint == "1") == (strFlagVoucher == "1"))
        {
            return PortResult.Fail("Mst_BirthPolicy_SaveX_InvalidFlagPointVoucher",
                "Check.FlagPoint", strFlagPoint,
                "Check.FlagVoucher", strFlagVoucher,
                "Expect", "Chi duoc chon 1 loai tang: FlagPoint hoac FlagVoucher = '1' (khong ca hai, khong ca khong)");
        }

        // b. (:9836-9863): tồn tại Mst_BirthPolicy cùng ParamPrType và EffDateStart >= EffDateStart mới -> Mst_BirthPolicy_SaveX_InvalidEffDateStart
        var existsNewerOrEqual = await _db.Set<Mst_BirthPolicy>().AsNoTracking()
            .AnyAsync(t => EF.Functions.Like((t.ParamPrType ?? "").ToLower(), PortQuery.Equal(strParamPrType ?? ""), PortQuery.Esc)
                           && t.EffDateStart >= dtEffDateStart.Date);
        if (existsNewerOrEqual)
        {
            return PortResult.Fail("Mst_BirthPolicy_SaveX_InvalidEffDateStart",
                "Check.BirthPolicyNo", strBirthPolicyNo,
                "Check.EffDateStart", dtEffDateStart.ToString("yyyy-MM-dd"));
        }

        // c. (:9867-9873): Mst_ParamPromotionType_CheckDB(ParamPrType, "1", "1") (:13024-13085)
        // Tra cứu Mst_ParamPromotionType trong DB (so sánh Like + PortQuery.Equal không phân biệt hoa/thường)
        var promotionType = await _db.Set<Mst_ParamPromotionType>().AsNoTracking()
            .FirstOrDefaultAsync(x => EF.Functions.Like((x.ParamPrType ?? "").ToLower(), PortQuery.Equal(strParamPrType ?? ""), PortQuery.Esc));

        if (promotionType == null)
        {
            return PortResult.Fail("Mst_ParamPromotionType_CheckDB_ParamPrTypeNotFound",
                "Check.ParamPrType", strParamPrType);
        }

        // FlagActive không thuộc "1" (nhưng null hoặc rỗng vẫn qua — như #230)
        if (!"1".Contains(promotionType.FlagActive ?? ""))
        {
            return PortResult.Fail("Mst_ParamPromotionType_CheckDB_FlagActiveNotMatched",
                "Check.ParamPrType", strParamPrType,
                "Check.strFlagActiveListToCheck", "1",
                "DB.FlagActive", promotionType.FlagActive);
        }

        // 4. Kiểm tra Dtl (Master.Loyalty.cs:10021-10087):
        var dtlInputList = req.Lst_Mst_BirthPolicyDtl ?? new List<MstBirthPolicySaveDtlReq>();
        var networkId = _config["Biz_NetworkID"] ?? "1001";
        var now = DateTime.UtcNow;
        var newDtls = new List<Mst_BirthPolicyDtl>();

        for (int nScan = 0; nScan < dtlInputList.Count; nScan++)
        {
            var drScan = dtlInputList[nScan];
            var strCardType = StdParam(drScan.CardType);

            // VoucherValue / VoucherExpireDays khác rỗng -> Convert.ToDouble (sai -> ..._Save)
            double dVoucherValue = 0;
            double? rawVoucherValue = null;
            if (drScan.VoucherValue != null)
            {
                try
                {
                    if (drScan.VoucherValue is System.Text.Json.JsonElement je)
                    {
                        if (je.ValueKind == System.Text.Json.JsonValueKind.Number)
                        {
                            rawVoucherValue = je.GetDouble();
                        }
                        else if (je.ValueKind == System.Text.Json.JsonValueKind.String)
                        {
                            var s = je.GetString();
                            if (!string.IsNullOrWhiteSpace(s)) rawVoucherValue = Convert.ToDouble(s);
                        }
                        else if (je.ValueKind != System.Text.Json.JsonValueKind.Null && je.ValueKind != System.Text.Json.JsonValueKind.Undefined)
                        {
                            rawVoucherValue = Convert.ToDouble(je.ToString());
                        }
                    }
                    else
                    {
                        var s = drScan.VoucherValue.ToString();
                        if (!string.IsNullOrWhiteSpace(s)) rawVoucherValue = Convert.ToDouble(drScan.VoucherValue);
                    }
                }
                catch
                {
                    return PortResult.Fail("Mst_BirthPolicy_Save", "Check.VoucherValue", drScan.VoucherValue);
                }
            }
            if (rawVoucherValue.HasValue)
            {
                dVoucherValue = rawVoucherValue.Value;
            }

            double dVoucherExpireDays = 0;
            int? rawVoucherExpireDays = null;
            if (drScan.VoucherExpireDays != null)
            {
                try
                {
                    if (drScan.VoucherExpireDays is System.Text.Json.JsonElement je)
                    {
                        if (je.ValueKind == System.Text.Json.JsonValueKind.Number)
                        {
                            rawVoucherExpireDays = je.GetInt32();
                            dVoucherExpireDays = rawVoucherExpireDays.Value;
                        }
                        else if (je.ValueKind == System.Text.Json.JsonValueKind.String)
                        {
                            var s = je.GetString();
                            if (!string.IsNullOrWhiteSpace(s))
                            {
                                dVoucherExpireDays = Convert.ToDouble(s);
                                rawVoucherExpireDays = Convert.ToInt32(dVoucherExpireDays);
                            }
                        }
                        else if (je.ValueKind != System.Text.Json.JsonValueKind.Null && je.ValueKind != System.Text.Json.JsonValueKind.Undefined)
                        {
                            dVoucherExpireDays = Convert.ToDouble(je.ToString());
                            rawVoucherExpireDays = Convert.ToInt32(dVoucherExpireDays);
                        }
                    }
                    else
                    {
                        var s = drScan.VoucherExpireDays.ToString();
                        if (!string.IsNullOrWhiteSpace(s))
                        {
                            dVoucherExpireDays = Convert.ToDouble(drScan.VoucherExpireDays);
                            rawVoucherExpireDays = Convert.ToInt32(dVoucherExpireDays);
                        }
                    }
                }
                catch
                {
                    return PortResult.Fail("Mst_BirthPolicy_Save", "Check.VoucherExpireDays", drScan.VoucherExpireDays);
                }
            }

            // < 0 -> Mst_BirthPolicy_SaveX_InvalidValue
            if (dVoucherValue < 0 || dVoucherExpireDays < 0)
            {
                return PortResult.Fail("Mst_BirthPolicy_SaveX_InvalidValue",
                    "Check.CardType", drScan.CardType,
                    "Check.VoucherValue", drScan.VoucherValue,
                    "Check.VoucherExpireDays", drScan.VoucherExpireDays,
                    "Check.ErrCoditional", "VoucherValue < 0 or VoucherExpireDays < 0");
            }

            // VoucherValue > 0 && VoucherExpireDays <= 0 -> Mst_BirthPolicy_SaveX_InvalidValue
            if (dVoucherValue > 0 && dVoucherExpireDays <= 0)
            {
                return PortResult.Fail("Mst_BirthPolicy_SaveX_InvalidValue",
                    "Check.CardType", drScan.CardType,
                    "Check.VoucherValue", drScan.VoucherValue,
                    "Check.VoucherExpireDays", drScan.VoucherExpireDays,
                    "Check.ErrCoditional", "VoucherValue > 0 but VoucherExpireDays <= 0");
            }

            // FK CardType -> Mst_CardType (FK_MstCardTp_MstBirthPolicyDtl) -> GIẢ LẬP: không có -> Mst_BirthPolicy_Save
            var cardTypeExists = await _db.Set<Mst_CardType>().AsNoTracking()
                .AnyAsync(x => EF.Functions.Like((x.CardType ?? "").ToLower(), PortQuery.Equal(strCardType ?? ""), PortQuery.Esc));
            if (!cardTypeExists)
            {
                return PortResult.Fail("Mst_BirthPolicy_Save", "Check.CardType", strCardType);
            }

            // Point, VoucherValue, VoucherExpireDays, Remark giữ nguyên (Remark không trim) (:10021-10028)
            newDtls.Add(new Mst_BirthPolicyDtl
            {
                BirthPolicyNo = birthPolicyNo,
                CardType = strCardType!,
                NetworkID = networkId,
                Point = drScan.Point,
                VoucherValue = rawVoucherValue,
                VoucherExpireDays = rawVoucherExpireDays,
                VoucherPointVCLimit = null, // Nguồn insert không có cột này (:10195-10216)
                Remark = drScan.Remark, // Giữ nguyên
                LogLUBy = "port",
                LogLUDTimeUTC = now
            });
        }

        // Kiểm tra trùng CardType -> PK -> ..._Save (kiểm tra sau vòng lặp)
        var distinctCardTypes = newDtls.Select(x => x.CardType).Distinct(StringComparer.OrdinalIgnoreCase).Count();
        if (distinctCardTypes < newDtls.Count)
        {
            return PortResult.Fail("Mst_BirthPolicy_Save", "Check.DuplicateCardType", null);
        }

        // 5. Lưu dữ liệu — 1 transaction duy nhất (Master.Loyalty.cs:10389, commit :10468)
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // Xóa Dtl + header cùng BirthPolicyNo (:10131-10151)
            var existingDtls = await _db.Set<Mst_BirthPolicyDtl>()
                .Where(x => x.BirthPolicyNo == birthPolicyNo).ToListAsync();
            if (existingDtls.Count > 0)
            {
                _db.Set<Mst_BirthPolicyDtl>().RemoveRange(existingDtls);
            }

            var existingHeader = await _db.Set<Mst_BirthPolicy>()
                .Where(x => x.BirthPolicyNo == birthPolicyNo).FirstOrDefaultAsync();
            if (existingHeader != null)
            {
                _db.Set<Mst_BirthPolicy>().Remove(existingHeader);
            }
            await _db.SaveChangesAsync();

            // Insert header (:10159-10188)
            var newHeader = new Mst_BirthPolicy
            {
                BirthPolicyNo = birthPolicyNo,
                ParamPrType = strParamPrType,
                NetworkID = networkId,
                BirthPolicyName = strBirthPolicyName,
                EffDateStart = DateTime.SpecifyKind(dtEffDateStart.Date, DateTimeKind.Utc),
                EffDateEnd = new DateTime(2100, 1, 1, 0, 0, 0, DateTimeKind.Utc), // Cố định 2100-01-01
                Remark = strRemark,
                FlagActive = strFlagActive,
                FlagPoint = strFlagPoint,
                FlagVoucher = strFlagVoucher,
                LogLUDTimeUTC = now,
                LogLUBy = "port",
                UpdateDTime = null,
                UpdateBy = null,
            };
            _db.Set<Mst_BirthPolicy>().Add(newHeader);

            // Insert Dtl (:10195-10216)
            if (newDtls.Count > 0)
            {
                _db.Set<Mst_BirthPolicyDtl>().AddRange(newDtls);
            }
            await _db.SaveChangesAsync();

            // 6. Cập nhật chương trình TRƯỚC (:10246-10270):
            // top 1 Mst_BirthPolicy khác BirthPolicyNo mới, CÙNG ParamPrType, order by LogLUDTimeUTC desc
            // -> EffDateEnd = EffDateStart mới - 1 ngày
            var preProgram = await _db.Set<Mst_BirthPolicy>()
                .Where(t => t.BirthPolicyNo != birthPolicyNo &&
                            EF.Functions.Like((t.ParamPrType ?? "").ToLower(), PortQuery.Equal(strParamPrType ?? ""), PortQuery.Esc))
                .OrderByDescending(t => t.LogLUDTimeUTC)
                .FirstOrDefaultAsync();

            if (preProgram != null)
            {
                preProgram.EffDateEnd = DateTime.SpecifyKind(dtEffDateStart.Date.AddDays(-1), DateTimeKind.Utc);
                await _db.SaveChangesAsync();
            }

            await tx.CommitAsync();
            return Ok(new { Success = true, Data = (object?)null });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return PortResult.Fail("Mst_BirthPolicy_Save", "Exception", ex.Message);
        }
    }

    // ============================================================================================
    // Update — nguồn gate Mst_BirthPolicyController.cs:438-530 (#74)
    //   -> API MstBirthPolicyController.cs:95-165 (WA_Mst_BirthPolicy_Update)
    //   -> BIZ Master.Loyalty.cs:11600-11690 (WAS_Mst_BirthPolicy_Update)
    //   -> :11455-11598 (Mst_BirthPolicy_Update)
    //   -> :11093-11453 (Mst_BirthPolicy_UpdateX — bản LIVE).
    //
    // GIỮ ĐÚNG NGUỒN:
    //   * Body JSON: { Mst_BirthPolicy: { ... }, Lst_Mst_BirthPolicyDtl: [ ... ] }
    //   * Body rỗng hoàn toàn -> ServiceException("strJson null") -> mã mặc định Mst_BirthPolicy_Update (gate :448-455, :520-523).
    //     KHÔNG tăng sequence (khối :471-498 bị comment ở gate). KHÔNG ép FlagActive = '1'.
    //   * Header null (client gửi `{}` thiếu Mst_BirthPolicy) -> NullReferenceException trong WAS (:11655-11658) -> mã WAS_Mst_BirthPolicy_Update (:11616).
    //   * Chuẩn hóa:
    //     - BirthPolicyNo StdParam (Trim().ToUpper())
    //     - FlagActive/FlagPoint/FlagVoucher StdFlag (thừa/thiếu/null -> '0')
    //     - Tên, ngày, ParamPrType, Remark client gửi trong header BỊ BỎ QUA (:11655-11658).
    //   * Kiểm DÚNG THỨ TỰ:
    //     a. (:11143-11155): (FlagPoint == '1') == (FlagVoucher == '1') (cả hai HOẶC không cái nào)
    //        -> Mst_BirthPolicy_SaveX_InvalidFlagPointVoucher (kiểm TRƯỚC cả kiểm tra tồn tại).
    //     b. (:11160-11167): Mst_BirthPolicy_CheckDB(No, "1") -> không tồn tại BirthPolicyNo
    //        -> Mst_BirthPolicy_CheckDB_BirthPolicyNoNotFound.
    //     c. (:11171-11197): chính sách có EffDateStart < hôm nay (DateTime.UtcNow.Date)
    //        -> Mst_BirthPolicy_UpdateX_InvalidEffDateStart — CHỈ sửa được chính sách CHƯA bắt đầu hiệu lực.
    //   * Chi tiết Dtl (:11264-11329):
    //     - Lst Dtl null -> danh sách rỗng (:11635-11636, không lỗi).
    //     - CardType StdParam (Trim().ToUpper()).
    //     - Point/VoucherValue/VoucherExpireDays giữ nguyên (Remark Dtl KHÔNG cập nhật).
    //     - VoucherValue / VoucherExpireDays khác rỗng -> Convert.ToDouble (sai -> ..._Update);
    //       < 0 -> Mst_BirthPolicy_UpdateX_InvalidValue;
    //       VoucherValue > 0 && VoucherExpireDays <= 0 -> Mst_BirthPolicy_UpdateX_InvalidValue.
    //   * Lưu: 1 transaction duy nhất (:11489, commit :11561), lỗi -> rollback + PortResult.Fail. Thành công -> Data = null.
    //   * Header SET (:11372-11386): FlagActive, FlagPoint, FlagVoucher,
    //     UpdateDTime = now (UTC), UpdateBy = "port" (LogLUDTimeUTC KHÔNG đổi).
    //   * Dtl SET (:11389-11405): Point, VoucherValue, VoucherExpireDays, LogLUDTimeUTC = now, LogLUBy = "port"
    //     CHỈ cho dòng ĐÃ CÓ (khớp BirthPolicyNo, CardType) — dòng mới BỊ BỎ QUA, không insert, không xóa.
    // ============================================================================================
    [HttpPost("update")]
    public async Task<IActionResult> Update(
        [FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] MstBirthPolicyUpdateReq? req)
    {
        // 0. Body rỗng / null: gate :448-455 ném ServiceException("strJson null") -> mã mặc định Mst_BirthPolicy_Update.
        if (req == null)
        {
            return PortResult.Fail("Mst_BirthPolicy_Update", "Check.strJson", null);
        }

        // Header null (client gửi `{}` thiếu Mst_BirthPolicy):
        // WAS :11655-11658 truy cập objRQ_Mst_BirthPolicy.Mst_BirthPolicy.BirthPolicyNo ném NullReferenceException -> mã WAS_Mst_BirthPolicy_Update (:11616).
        if (req.Mst_BirthPolicy == null)
        {
            return PortResult.Fail("WAS_Mst_BirthPolicy_Update", "Check.Mst_BirthPolicy", null);
        }

        var header = req.Mst_BirthPolicy;
        var strBirthPolicyNo = StdParam(header.BirthPolicyNo);
        var strFlagActive = StdFlag(header.FlagActive);
        var strFlagPoint = StdFlag(header.FlagPoint);
        var strFlagVoucher = StdFlag(header.FlagVoucher);

        // 1. Kiểm tra thứ tự a (:11143-11155):
        // (FlagPoint == '1') == (FlagVoucher == '1') -> Mst_BirthPolicy_SaveX_InvalidFlagPointVoucher (kiểm TRƯỚC cả tồn tại trong DB).
        if ((strFlagPoint == "1") == (strFlagVoucher == "1"))
        {
            return PortResult.Fail("Mst_BirthPolicy_SaveX_InvalidFlagPointVoucher",
                "Check.FlagPoint", strFlagPoint,
                "Check.FlagVoucher", strFlagVoucher,
                "Expect", "Chi duoc chon 1 loai tang: FlagPoint hoac FlagVoucher = '1' (khong ca hai, khong ca khong)");
        }

        // 2. Kiểm tra thứ tự b (:11160-11167):
        // Mst_BirthPolicy_CheckDB(No, "1") -> không tồn tại -> Mst_BirthPolicy_CheckDB_BirthPolicyNoNotFound.
        // Chuỗi DB: dùng Like + PortQuery.Equal không phân biệt hoa/thường.
        var existingPolicy = await _db.Set<Mst_BirthPolicy>()
            .FirstOrDefaultAsync(x => EF.Functions.Like((x.BirthPolicyNo ?? "").ToLower(), PortQuery.Equal(strBirthPolicyNo ?? ""), PortQuery.Esc));

        if (existingPolicy == null)
        {
            return PortResult.Fail("Mst_BirthPolicy_CheckDB_BirthPolicyNoNotFound",
                "Check.BirthPolicyNo", strBirthPolicyNo);
        }

        // 3. Kiểm tra thứ tự c (:11171-11197):
        // Chính sách có EffDateStart < hôm nay (DateTime.UtcNow.Date) -> Mst_BirthPolicy_UpdateX_InvalidEffDateStart.
        // CHỈ sửa được chính sách CHƯA bắt đầu hiệu lực (EffDateStart >= hôm nay).
        var today = DateTime.UtcNow.Date;
        if (existingPolicy.EffDateStart < today)
        {
            return PortResult.Fail("Mst_BirthPolicy_UpdateX_InvalidEffDateStart",
                "Check.BirthPolicyNo", strBirthPolicyNo,
                "Check.EffDateEnd", existingPolicy.EffDateEnd);
        }

        // 4. Kiểm tra Dtl (:11264-11329):
        // Lst Dtl null -> danh sách rỗng (:11635-11636).
        var dtlInputList = req.Lst_Mst_BirthPolicyDtl ?? new List<MstBirthPolicySaveDtlReq>();
        var parsedDtls = new List<(string? cardType, double? point, double? rawVoucherValue, int? rawVoucherExpireDays)>();

        for (int nScan = 0; nScan < dtlInputList.Count; nScan++)
        {
            var drScan = dtlInputList[nScan];
            var strCardType = StdParam(drScan.CardType);

            // VoucherValue / VoucherExpireDays khác rỗng -> Convert.ToDouble (sai -> ..._Update)
            double dVoucherValue = 0;
            double? rawVoucherValue = null;
            if (drScan.VoucherValue != null)
            {
                try
                {
                    if (drScan.VoucherValue is System.Text.Json.JsonElement je)
                    {
                        if (je.ValueKind == System.Text.Json.JsonValueKind.Number)
                        {
                            rawVoucherValue = je.GetDouble();
                        }
                        else if (je.ValueKind == System.Text.Json.JsonValueKind.String)
                        {
                            var s = je.GetString();
                            if (!string.IsNullOrWhiteSpace(s)) rawVoucherValue = Convert.ToDouble(s);
                        }
                        else if (je.ValueKind != System.Text.Json.JsonValueKind.Null && je.ValueKind != System.Text.Json.JsonValueKind.Undefined)
                        {
                            rawVoucherValue = Convert.ToDouble(je.ToString());
                        }
                    }
                    else
                    {
                        var s = drScan.VoucherValue.ToString();
                        if (!string.IsNullOrWhiteSpace(s)) rawVoucherValue = Convert.ToDouble(drScan.VoucherValue);
                    }
                }
                catch
                {
                    return PortResult.Fail("Mst_BirthPolicy_Update", "Check.VoucherValue", drScan.VoucherValue);
                }
            }
            if (rawVoucherValue.HasValue)
            {
                dVoucherValue = rawVoucherValue.Value;
            }

            double dVoucherExpireDays = 0;
            int? rawVoucherExpireDays = null;
            if (drScan.VoucherExpireDays != null)
            {
                try
                {
                    if (drScan.VoucherExpireDays is System.Text.Json.JsonElement je)
                    {
                        if (je.ValueKind == System.Text.Json.JsonValueKind.Number)
                        {
                            rawVoucherExpireDays = je.GetInt32();
                            dVoucherExpireDays = rawVoucherExpireDays.Value;
                        }
                        else if (je.ValueKind == System.Text.Json.JsonValueKind.String)
                        {
                            var s = je.GetString();
                            if (!string.IsNullOrWhiteSpace(s))
                            {
                                dVoucherExpireDays = Convert.ToDouble(s);
                                rawVoucherExpireDays = Convert.ToInt32(dVoucherExpireDays);
                            }
                        }
                        else if (je.ValueKind != System.Text.Json.JsonValueKind.Null && je.ValueKind != System.Text.Json.JsonValueKind.Undefined)
                        {
                            dVoucherExpireDays = Convert.ToDouble(je.ToString());
                            rawVoucherExpireDays = Convert.ToInt32(dVoucherExpireDays);
                        }
                    }
                    else
                    {
                        var s = drScan.VoucherExpireDays.ToString();
                        if (!string.IsNullOrWhiteSpace(s))
                        {
                            dVoucherExpireDays = Convert.ToDouble(drScan.VoucherExpireDays);
                            rawVoucherExpireDays = Convert.ToInt32(dVoucherExpireDays);
                        }
                    }
                }
                catch
                {
                    return PortResult.Fail("Mst_BirthPolicy_Update", "Check.VoucherExpireDays", drScan.VoucherExpireDays);
                }
            }

            // VoucherValue < 0 or VoucherExpireDays < 0 -> Mst_BirthPolicy_UpdateX_InvalidValue (:11294-11307)
            if (dVoucherValue < 0 || dVoucherExpireDays < 0)
            {
                return PortResult.Fail("Mst_BirthPolicy_UpdateX_InvalidValue",
                    "Check.CardType", drScan.CardType,
                    "Check.VoucherValue", drScan.VoucherValue,
                    "Check.VoucherExpireDays", drScan.VoucherExpireDays,
                    "Check.ErrCoditional", "VoucherValue < 0 or VoucherExpireDays < 0");
            }

            // VoucherValue > 0 && VoucherExpireDays <= 0 -> Mst_BirthPolicy_UpdateX_InvalidValue (:11310-11323)
            if (dVoucherValue > 0 && dVoucherExpireDays <= 0)
            {
                return PortResult.Fail("Mst_BirthPolicy_UpdateX_InvalidValue",
                    "Check.CardType", drScan.CardType,
                    "Check.VoucherValue", drScan.VoucherValue,
                    "Check.VoucherExpireDays", drScan.VoucherExpireDays,
                    "Check.ErrCoditional", "VoucherValue > 0 but VoucherExpireDays <= 0");
            }

            parsedDtls.Add((strCardType, drScan.Point, rawVoucherValue, rawVoucherExpireDays));
        }

        // 5. Cập nhật dữ liệu — 1 transaction duy nhất (Master.Loyalty.cs:11489, commit :11561)
        var now = DateTime.UtcNow;
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // Cập nhật header (:11372-11386):
            // SET FlagActive, FlagPoint, FlagVoucher, UpdateDTime = now, UpdateBy = "port" (LogLUDTimeUTC KHÔNG đổi).
            // Các trường tên/ngày/ParamPrType/Remark do client gửi BỊ BỎ QUA (:11655-11658).
            existingPolicy.FlagActive = strFlagActive;
            existingPolicy.FlagPoint = strFlagPoint;
            existingPolicy.FlagVoucher = strFlagVoucher;
            existingPolicy.UpdateDTime = now;
            existingPolicy.UpdateBy = "port";

            // Cập nhật Dtl (:11389-11405):
            // SET Point, VoucherValue, VoucherExpireDays, LogLUDTimeUTC = now, LogLUBy = "port"
            // CHỈ cho dòng ĐÃ CÓ (khớp BirthPolicyNo, CardType) — dòng mới BỊ BỎ QUA, không insert, không xóa.
            if (parsedDtls.Count > 0)
            {
                var existingDtls = await _db.Set<Mst_BirthPolicyDtl>()
                    .Where(d => EF.Functions.Like((d.BirthPolicyNo ?? "").ToLower(), PortQuery.Equal(strBirthPolicyNo!), PortQuery.Esc))
                    .ToListAsync();

                foreach (var (cardType, point, voucherValue, voucherExpireDays) in parsedDtls)
                {
                    if (string.IsNullOrWhiteSpace(cardType)) continue;

                    var matchingDtl = existingDtls.FirstOrDefault(d => string.Equals(d.CardType, cardType, StringComparison.OrdinalIgnoreCase));
                    if (matchingDtl != null)
                    {
                        matchingDtl.Point = point;
                        matchingDtl.VoucherValue = voucherValue;
                        matchingDtl.VoucherExpireDays = voucherExpireDays;
                        matchingDtl.LogLUDTimeUTC = now;
                        matchingDtl.LogLUBy = "port";
                    }
                    // Dòng mới (không khớp BirthPolicyNo, CardType) -> BỊ BỎ QUA, không insert (:11399-11402)
                }
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            // Thành công -> Data = null (gate Mst_BirthPolicyController.cs:517-518).
            return Ok(new { Success = true, Data = (object?)null });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return PortResult.Fail("Mst_BirthPolicy_Update", "Exception", ex.Message);
        }
    }

    // ============================================================================================
    // Delete — nguồn gate Mst_BirthPolicyController.cs:535-600 (#75)
    //   -> API MstBirthPolicyController.cs:22-92 (WA_Mst_BirthPolicy_Save với FlagIsDelete = "1")
    //   -> BIZ Master.Loyalty.cs:10507-10604 (WAS_Mst_BirthPolicy_Save)
    //   -> :10348-10505 (Mst_BirthPolicy_Save)
    //   -> :9675-10346 (Mst_BirthPolicy_SaveX — nhánh bIsDelete = true).
    //
    // GIỮ ĐÚNG NGUỒN:
    //   * Body JSON: { BirthPolicyNo } (nguồn gate Mst_BirthPolicyController.cs:535).
    //   * BirthPolicyNo rỗng / null / whitespace:
    //     gate ném ServiceException("invalid_BirthPolicyNo", "invalid_BirthPolicyNo") (:585)
    //     -> HTTP 400 ErridnLoyalty.invalid_BirthPolicyNo.
    //   * Nghiệp vụ sau kiểm tra rỗng được tách thành DeleteXAsync(no) chạy trong 1 transaction duy nhất.
    // ============================================================================================
    [HttpPost("delete")]
    public async Task<IActionResult> Delete(
        [FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] MstBirthPolicyDeleteReq? req)
    {
        // Kiểm tra BirthPolicyNo rỗng / null / whitespace (gate Mst_BirthPolicyController.cs:554, :585)
        if (req == null || string.IsNullOrWhiteSpace(req.BirthPolicyNo))
        {
            return PortResult.Fail("invalid_BirthPolicyNo", "Check.BirthPolicyNo", req?.BirthPolicyNo);
        }

        return await DeleteXAsync(req.BirthPolicyNo);
    }

    /// <summary>
    /// Nghiệp vụ xóa chính sách sinh nhật (Master.Loyalty.cs:9675-10346, bIsDelete = true) — chạy trong 1 transaction.
    /// 1. Chuẩn hóa BirthPolicyNo bằng StdParam (Trim().ToUpperInvariant()).
    ///    CheckDB (:9757-9770): nếu KHÔNG tồn tại -> Thành công, không làm gì cả (bIsDelete -> goto Done, gate :579-580 -> Data null, Success = true).
    /// 2. Kiểm tra EffDateStart (:9879-9906):
    ///    Nếu chính sách có EffDateStart <= hôm nay (DateTime.UtcNow.Date) -> ném Mst_BirthPolicy_SaveX_InvalidEffDateStart
    ///    (CHỈ xóa được chính sách CHƯA bắt đầu hiệu lực: EffDateStart > hôm nay).
    /// 3. TRƯỚC khi xóa: tìm Dl_After (:9908-9941):
    ///    Dl = dòng bị xóa.
    ///    Dl_After = top 1 chính sách CÙNG ParamPrType có LogLUDTimeUTC > LogLUDTimeUTC của dòng bị xóa, order by LogLUDTimeUTC asc.
    ///    LogLUDTimeUTC so sánh nguyên giá trị.
    /// 4. Xóa Dtl + header (:10131-10151):
    ///    FK trỏ tới Mst_BirthPolicy chỉ có Mst_BirthPolicyDtl -> xóa Dtl trước rồi xóa header.
    /// 5. SAU khi xóa: 2 trường hợp sửa chính sách liền kề (:10272-10318):
    ///    - TH1: Có Dl_After -> Dl_After.EffDateStart = Dl.EffDateStart
    ///      (chính sách kế tiếp "lùi" ngày bắt đầu về ngày của dòng bị xóa).
    ///    - TH2: Không có Dl_After -> chính sách MỚI NHẤT còn lại cùng ParamPrType (top 1 LogLUDTimeUTC desc) -> EffDateEnd = 2100-01-01.
    /// </summary>
    private async Task<IActionResult> DeleteXAsync(string no)
    {
        var strBirthPolicyNo = StdParam(no);
        if (strBirthPolicyNo == null)
        {
            return PortResult.Fail("invalid_BirthPolicyNo", "Check.BirthPolicyNo", no);
        }

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // 1. Kiểm tra tồn tại trong DB (Master.Loyalty.cs:9757-9770):
            // Mst_BirthPolicy_CheckDB: nếu KHÔNG tồn tại -> Thành công, không làm gì cả (bIsDelete -> goto Done, gate :579-580 -> Data null, Success = true).
            // Chuỗi DB: Like + PortQuery.Equal không phân biệt hoa/thường.
            var dl = await _db.Set<Mst_BirthPolicy>()
                .FirstOrDefaultAsync(x => EF.Functions.Like((x.BirthPolicyNo ?? "").ToLower(), PortQuery.Equal(strBirthPolicyNo), PortQuery.Esc));

            if (dl == null)
            {
                await tx.CommitAsync();
                return Ok(new { Success = true, Data = (object?)null });
            }

            // 2. Kiểm tra EffDateStart (Master.Loyalty.cs:9879-9906):
            // KHÔNG có dòng BirthPolicyNo = x AND EffDateStart > hôm nay (DateTime.UtcNow.Date) -> Mst_BirthPolicy_SaveX_InvalidEffDateStart
            // CHỈ xóa được chính sách CHƯA bắt đầu hiệu lực (EffDateStart > hôm nay).
            var today = DateTime.UtcNow.Date;
            // Nguồn: giữ lại khi `EffDateStart > 'yyyy-MM-dd'` (00:00 hôm nay) ⇒ so NGUYÊN datetime, KHÔNG cắt .Date
            // (EffDateStart hôm nay 10:00 vẫn xoá được như nguồn).
            if (dl.EffDateStart <= today)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Mst_BirthPolicy_SaveX_InvalidEffDateStart",
                    "Check.BirthPolicyNo", strBirthPolicyNo,
                    "Check.EffDateStart", dl.EffDateStart.ToString("yyyy-MM-dd"));
            }

            // 3. TRƯỚC khi xóa: tìm Dl_After (Master.Loyalty.cs:9908-9941):
            // Dl_After = top 1 chính sách CÙNG ParamPrType có LogLUDTimeUTC > LogLUDTimeUTC của dòng bị xóa, order by LogLUDTimeUTC asc.
            // LogLUDTimeUTC so sánh nguyên giá trị.
            Mst_BirthPolicy? dlAfter = null;
            if (dl.LogLUDTimeUTC.HasValue)
            {
                dlAfter = await _db.Set<Mst_BirthPolicy>()
                    .Where(t => EF.Functions.Like((t.ParamPrType ?? "").ToLower(), PortQuery.Equal(dl.ParamPrType ?? ""), PortQuery.Esc)
                                && t.LogLUDTimeUTC > dl.LogLUDTimeUTC)
                    .OrderBy(t => t.LogLUDTimeUTC)
                    .FirstOrDefaultAsync();
            }

            // Lưu lại ngày bắt đầu và ParamPrType của dòng bị xóa trước khi remove
            var dlEffDateStart = dl.EffDateStart;
            var dlParamPrType = dl.ParamPrType;

            // 4. Xóa Dtl + header (Master.Loyalty.cs:10131-10151):
            // FK trỏ tới Mst_BirthPolicy chỉ có Mst_BirthPolicyDtl -> xóa Dtl trước rồi xóa header.
            var dtls = await _db.Set<Mst_BirthPolicyDtl>()
                .Where(d => EF.Functions.Like((d.BirthPolicyNo ?? "").ToLower(), PortQuery.Equal(strBirthPolicyNo), PortQuery.Esc))
                .ToListAsync();
            if (dtls.Count > 0)
            {
                _db.Set<Mst_BirthPolicyDtl>().RemoveRange(dtls);
            }
            _db.Set<Mst_BirthPolicy>().Remove(dl);
            await _db.SaveChangesAsync();

            // 5. SAU khi xóa: 2 trường hợp sửa chính sách liền kề (Master.Loyalty.cs:10272-10318):
            // - TH1: Có Dl_After -> Dl_After.EffDateStart = Dl.EffDateStart
            //   (chính sách kế tiếp "lùi" ngày bắt đầu về ngày của dòng bị xóa).
            // - TH2: Không có Dl_After -> chính sách MỚI NHẤT còn lại cùng ParamPrType (top 1 LogLUDTimeUTC desc) -> EffDateEnd = 2100-01-01.
            if (dlAfter != null)
            {
                dlAfter.EffDateStart = dlEffDateStart;
                await _db.SaveChangesAsync();
            }
            else
            {
                var latestRemaining = await _db.Set<Mst_BirthPolicy>()
                    .Where(t => EF.Functions.Like((t.ParamPrType ?? "").ToLower(), PortQuery.Equal(dlParamPrType ?? ""), PortQuery.Esc))
                    .OrderByDescending(t => t.LogLUDTimeUTC)
                    .FirstOrDefaultAsync();

                if (latestRemaining != null)
                {
                    latestRemaining.EffDateEnd = new DateTime(2100, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                    await _db.SaveChangesAsync();
                }
            }

            await tx.CommitAsync();
            return Ok(new { Success = true, Data = (object?)null });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return PortResult.Fail("Mst_BirthPolicy_Save", "Exception", ex.Message);
        }
    }


    private static string? StdParam(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim().ToUpperInvariant();
    private static string? StdStr(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
    private static string StdFlag(string? s) => s == "1" ? "1" : "0";

    /// <summary>
    /// Overload giữ nguyên tương thích cho Search (#70) và GetAllActive (#71).
    /// </summary>
    private async Task<(int itemCount, List<object> rows)> GetXAsync(
        string? keyWord,
        string? flagActiveCsv,
        long recordStart,
        long recordCount)
    {
        var (itemCount, rows, _) = await GetXAsync(keyWord, flagActiveCsv, recordStart, recordCount, null, false);
        return (itemCount, rows);
    }

    /// <summary>
    /// Lọc + đếm + chia trang theo LogLUDTimeUTC desc (Master.Loyalty.cs:10877-11085).
    /// Hỗ trợ lọc KeyWord (like '%kw%'), FlagActive (IN), BirthPolicyNo (=), và lấy danh sách chi tiết Dtl.
    /// drAbilityOfUser (Master.Loyalty.cs:10920) nguồn gọi nhưng không dùng (:10932 bị comment) -> bỏ qua, ghi comment.
    /// </summary>
    private async Task<(int itemCount, List<object> rows, List<object> dtlRows)> GetXAsync(
        string? keyWord,
        string? flagActiveCsv,
        long recordStart,
        long recordCount,
        string? birthPolicyNoEqual = null,
        bool getDtl = false)
    {
        // drAbilityOfUser (Master.Loyalty.cs:10920) nguồn gọi nhưng bị comment không dùng (:10932) -> bỏ qua, ghi comment.
        var q = _db.Set<Mst_BirthPolicy>().AsNoTracking().AsQueryable();

        // 1. Lọc KeyWord: like '%kw%' trên BirthPolicyNo (nguồn gate Mst_BirthPolicyController.cs:299-302).
        // Nguồn chạy SQL Server collation không phân biệt hoa/thường -> hạ chữ cả 2 phía rồi LIKE qua PortQuery.
        if (!string.IsNullOrWhiteSpace(keyWord))
        {
            var kw = PortQuery.Contains(keyWord);
            q = q.Where(x => EF.Functions.Like((x.BirthPolicyNo ?? "").ToLower(), kw, PortQuery.Esc));
        }

        // Lọc BirthPolicyNo: `BirthPolicyNo = 'x'` (gate Mst_BirthPolicyController.cs:327 — strWhereClause_Mst_BirthPolicy_By_BirthPolicyNo).
        // Nguồn dùng `=` trên SQL Server collation KHÔNG phân biệt hoa/thường ⇒ Like KHÔNG wildcard, hạ chữ 2 phía (PortQuery.Equal).
        if (!string.IsNullOrWhiteSpace(birthPolicyNoEqual))
        {
            var bp = PortQuery.Equal(birthPolicyNoEqual);
            q = q.Where(x => EF.Functions.Like((x.BirthPolicyNo ?? "").ToLower(), bp, PortQuery.Esc));
        }

        // 2. Lọc FlagActive: danh sách IN ('val1','val2',...) (nguồn gate Mst_BirthPolicyController.cs:308-316).
        // Tách dấu phẩy, bỏ khoảng trắng thừa, hạ chữ để so sánh không phân biệt hoa/thường.
        // Lưu ý: nhánh '=' tại :304-307 bị comment ở nguồn nên không dùng.
        if (!string.IsNullOrWhiteSpace(flagActiveCsv))
        {
            var ds = flagActiveCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => s.ToLowerInvariant())
                .ToList();
            if (ds.Count > 0)
            {
                q = q.Where(x => ds.Contains((x.FlagActive ?? "").ToLower()));
            }
        }

        // 3. Draft filter (#tbl_Mst_BirthPolicy_Filter_Draft — Master.Loyalty.cs:10936-10945):
        // order by mrp.LogLUDTimeUTC desc. Hòa trùng LogLUDTimeUTC: sắp phụ theo BirthPolicyNo asc để kết quả ổn định.
        var ordered = q
            .OrderByDescending(x => x.LogLUDTimeUTC)
            .ThenBy(x => x.BirthPolicyNo);

        // Summary count: select Count(0) MyCount from #tbl_Mst_BirthPolicy_Filter_Draft (:10948)
        var itemCount = await ordered.CountAsync();

        // 4. Phân trang (#tbl_Mst_BirthPolicy_Filter — Master.Loyalty.cs:10952-10959):
        // t.MyIdxSeq >= @nFilterRecordStart and t.MyIdxSeq <= @nFilterRecordEnd
        // với nFilterRecordEnd = recordStart + recordCount - 1. Nếu recordCount <= 0 thì trả danh sách rỗng.
        var page = recordCount <= 0
            ? new List<Mst_BirthPolicy>()
            : await ordered.Skip((int)recordStart).Take((int)recordCount).ToListAsync();

        // 5. Select header + FlagShow (Master.Loyalty.cs:10981-10996):
        // FlagShow = '1' khi EffDateStart <= @dateSys and @dateSys <= EffDateEnd, '0' khi ngoài khoảng
        // với @dateSys = DateTime.UtcNow.AddHours(7).ToString("yyyy-MM-dd") (ngày Việt Nam UTC+7 lúc 00:00:00).
        var today = DateTime.UtcNow.AddHours(7).Date;

        var rows = page
            .OrderByDescending(x => x.LogLUDTimeUTC)
            .ThenBy(x => x.BirthPolicyNo)
            .Select((x, i) =>
            {
                var myIdxSeq = recordStart + i;
                // SQL so sánh cột datetime với chuỗi 'yyyy-MM-dd' = 00:00 hôm nay ⇒ so NGUYÊN giá trị datetime (KHÔNG cắt .Date):
                // EffDateStart hôm nay 10:00 ⇒ 10:00 <= 00:00 sai ⇒ '0' như nguồn.
                var flagShow = (x.EffDateStart <= today && today <= x.EffDateEnd) ? "1" : "0";
                return (object)new
                {
                    MyIdxSeq = myIdxSeq,
                    x.BirthPolicyNo,
                    x.NetworkID,
                    x.ParamPrType,
                    x.BirthPolicyName,
                    x.EffDateStart,
                    x.EffDateEnd,
                    x.FlagActive,
                    x.FlagPoint,
                    x.FlagVoucher,
                    x.Remark,
                    x.UpdateDTime,
                    x.UpdateBy,
                    x.LogLUDTimeUTC,
                    x.LogLUBy,
                    FlagShow = flagShow,
                };
            })
            .ToList();

        // 6. Select Dtl (Master.Loyalty.cs:11008-11016) khi getDtl = true
        // select t.MyIdxSeq, mrpl.*
        // from #tbl_Mst_BirthPolicy_Filter t
        //   inner join Mst_BirthPolicyDtl mrpl on t.BirthPolicyNo = mrpl.BirthPolicyNo
        // order by mrpl.LogLUDTimeUTC desc
        var dtlRows = new List<object>();
        if (getDtl && page.Count > 0)
        {
            var birthPolicyNos = page.Select(x => x.BirthPolicyNo).ToList();
            var policyMap = page
                .Select((x, i) => new { x.BirthPolicyNo, MyIdxSeq = recordStart + i })
                .ToDictionary(x => x.BirthPolicyNo, x => x.MyIdxSeq, StringComparer.OrdinalIgnoreCase);

            List<Mst_BirthPolicyDtl> dtlList;
            if (birthPolicyNos.Count == 1)
            {
                var targetNo = birthPolicyNos[0];
                dtlList = await _db.Set<Mst_BirthPolicyDtl>()
                    .AsNoTracking()
                    .Where(d => EF.Functions.Like((d.BirthPolicyNo ?? "").ToLower(), PortQuery.Equal(targetNo), PortQuery.Esc))
                    .ToListAsync();
            }
            else
            {
                dtlList = await _db.Set<Mst_BirthPolicyDtl>()
                    .AsNoTracking()
                    .Where(d => birthPolicyNos.Contains(d.BirthPolicyNo))
                    .ToListAsync();
            }

            // Sắp xếp Dtl theo mrpl.LogLUDTimeUTC desc (Master.Loyalty.cs:11015).
            // Hòa trùng LogLUDTimeUTC: sắp phụ theo CardType asc, ghi comment theo đề bài (Master.Loyalty.cs:11015).
            dtlRows = dtlList
                .OrderByDescending(d => d.LogLUDTimeUTC)
                .ThenBy(d => d.CardType)
                .Select(d => (object)new
                {
                    MyIdxSeq = policyMap.TryGetValue(d.BirthPolicyNo, out var seq) ? seq : 0L,
                    d.BirthPolicyNo,
                    d.CardType,
                    d.NetworkID,
                    d.Point,
                    d.Remark,
                    d.LogLUDTimeUTC,
                    d.LogLUBy,
                    d.VoucherValue,
                    d.VoucherPointVCLimit,
                    d.VoucherExpireDays,
                })
                .ToList();
        }

        return (itemCount, rows, dtlRows);
    }
}
