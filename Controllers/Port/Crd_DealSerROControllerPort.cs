using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MiniLoyalty.Data;
using MiniLoyalty.Models;
using MiniLoyalty.Models.Port;

namespace MiniLoyalty.Controllers.Port;

// ===========================================================================================
// PORT 1:1 từ dự án nguồn 2022.7.Loyalty — Crd_DealSerROController (#019, #020, #021).
//
// Nguồn tra cứu (chỉ đọc):
//   - Gate Search: 13.ClientGate/V10/idn.Loyalty.WebAPI/Controllers/Crd_DealSerROController.cs:29-109
//   - Gate Calc:   13.ClientGate/V10/idn.Loyalty.WebAPI/Controllers/Crd_DealSerROController.cs:113-184
//   - Gate Add:    13.ClientGate/V10/idn.Loyalty.WebAPI/Controllers/Crd_DealSerROController.cs:188-251
//   - Filter:      Crd_DealSerROController.cs:255-270 (strWhereClause_Crd_DealSerRO)
//   - API Add:     11.BackEnd/V10/idn.Loyalty.Biz.Web/Controllers/CrdDealSerROController.cs:96-166
//   - Biz Add:     11.BackEnd/V10/idn.Loyalty.Biz/Card.Deal.cs:14810-15174 (Crd_DealSerRO_Add_New20260813)
//                  và Card.Deal.cs:12009-14441 (Crd_DealSerRO_AddX_New20260813)
// ===========================================================================================

/// <summary>
/// DTO yêu cầu tìm kiếm báo giá dịch vụ Crd_DealSerRO (#019).
/// </summary>
public class Crd_DealSerROSearchReq
{
    public string? MemberNo { get; set; }             // Mã hội viên — gate :29, :58, :260-263
    public string? CardNo { get; set; }               // Số thẻ — gate :29, :59, :264-267
    public string? DLCPCode { get; set; }             // Mã đại lý — gate :29, :60 (không lọc trong strWhereClause)
    public string? OrgID { get; set; }                // Mã tổ chức — gate :41, :71 (không lọc trong SQL WHERE)
    public string? NetworkID { get; set; }            // NetworkID — gate :40, :70 (không lọc trong SQL WHERE)
    public int Ft_PageIndex { get; set; } = 0;        // Vị trí trang 0-based — gate :29, mặc định 0
    public int Ft_PageSize { get; set; } = 10;        // Số bản ghi mỗi trang — gate :29, mặc định 10
}

/// <summary>
/// DTO yêu cầu thêm mới báo giá dịch vụ (#021) — gate:196-200.
/// </summary>
public class RQ_Crd_DealSerRO
{
    public Crd_DealSerROInput? Crd_DealSerRO { get; set; }
    public List<Crd_DealSerRODtlInput>? Lst_Crd_DealSerRODtl { get; set; }
    public List<Crd_DealSerROVoucherInput>? Lst_Crd_DealSerROVoucher { get; set; }
}

/// <summary>
/// DTO yêu cầu tính toán thử báo giá dịch vụ (#020) — gate:121-124.
/// </summary>
public class Crd_DealSerROCalcReq : RQ_Crd_DealSerRO
{
}

public class Crd_DealSerROInput
{
    public string? DealSerRONo { get; set; }
    public string? DLCode { get; set; }
    public string? CardNo { get; set; }
    public string? MemberNo { get; set; }
    public string? DealPointType { get; set; }
    public string? CardType { get; set; }
    public string? CardTypeExpect { get; set; }
    public string? PolicyCode { get; set; }
    public string? ParamCode { get; set; }
    public double? TotalAmountRO { get; set; }
    public double? AmountRO { get; set; }
    public double? AmountUse { get; set; }
    public double? PointTotal { get; set; }
    public double? PolicyPointUnit { get; set; }
    public double? PolicyAmountUnit { get; set; }
    public double? ParamPointUnit { get; set; }
    public double? ParamAmountUnit { get; set; }
    public string? Remark { get; set; }
    public string? FlagOnlyPoint { get; set; }
    public string? PlateNo { get; set; }
    public string? VIN { get; set; }
    public double? PointTotalRank { get; set; }
    public double? PointConsumptionPrm { get; set; }
    public double? PointVoucher { get; set; }
    public double? PointActualVoucher { get; set; }
    public double? TotalDiscount { get; set; }
    public double? TotalAmountForDC { get; set; }
}

public class Crd_DealSerRODtlInput
{
    public string? ExpenseType { get; set; }
    public string? PolicyExpenseTypeNo { get; set; }
    public double? Amount { get; set; }
    public string? FlagPoint { get; set; }
    public double? AmountRate { get; set; }
    public double? FlagDiscount { get; set; }
    public double? DiscountRate { get; set; }
    public double? AmountForDC { get; set; }
    public string? RefCode00 { get; set; }
    public string? RefCode01 { get; set; }
    public string? RefCode02 { get; set; }
    public string? RefCode03 { get; set; }
    public string? RefCode04 { get; set; }
    public string? RefCode05 { get; set; }
}

public class Crd_DealSerROVoucherInput
{
    public string? VoucherCode { get; set; }
    public double? PointVoucher { get; set; }
    public double? PointActualVoucher { get; set; }
}

[ApiController]
[Route("api/crd_dealserro")]
public class Crd_DealSerROControllerPort : ControllerBase
{
    private readonly AppDbContext _db;

    public Crd_DealSerROControllerPort(AppDbContext db)
    {
        _db = db;
    }

    private static string? StdParam(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim().ToUpperInvariant();
    private static string? StdStr(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
    private static string StdFlag(string? s) => s == "1" ? "1" : "0";

    /// <summary>
    /// Port 1:1 action Search — tìm kiếm danh sách báo giá dịch vụ Crd_DealSerRO.
    /// Nguồn: Crd_DealSerROController.cs:29-109 (gate), Card.Deal.cs:99-581 (biz).
    /// </summary>
    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Crd_DealSerROSearchReq? req)
    {
        req ??= new Crd_DealSerROSearchReq();
        int pageIndex = req.Ft_PageIndex;
        int pageSize = req.Ft_PageSize;

        var query = _db.Set<Crd_DealSerRO>().AsNoTracking().AsQueryable();

        // 1. Lọc MemberNo với toán tử '=' (nguyên chuỗi, không phân biệt hoa/thường) — gate :260-263
        if (!string.IsNullOrEmpty(req.MemberNo))
        {
            var p_MemberNo = PortQuery.Equal(req.MemberNo);
            query = query.Where(x => EF.Functions.Like((x.MemberNo ?? "").ToLower(), p_MemberNo, PortQuery.Esc));
        }

        // 2. Lọc CardNo với toán tử '=' (nguyên chuỗi, không phân biệt hoa/thường) — gate :264-267
        if (!string.IsNullOrEmpty(req.CardNo))
        {
            var p_CardNo = PortQuery.Equal(req.CardNo);
            query = query.Where(x => EF.Functions.Like((x.CardNo ?? "").ToLower(), p_CardNo, PortQuery.Esc));
        }

        // 3. DLCPCode, OrgID, NetworkID: nguồn gate truyền vào RQ nhưng strWhereClause_Crd_DealSerRO (gate :255-270)
        // và Crd_DealSerRO_GetX (Card.Deal.cs:166-168) HOÀN TOÀN KHÔNG lọc trong mệnh đề WHERE -> giữ nguyên hành vi nguồn (Bài học A.18).

        // 4. Sắp xếp: order by cdsro.LogLUDTimeUTC asc (Card.Deal.cs:168)
        var ordered = query.OrderBy(x => x.LogLUDTimeUTC).ThenBy(x => x.DealSerRONo);
        var itemCount = await ordered.CountAsync();

        // 5. Phân trang: chống tràn số (Bài học A.12) và xử lý pageSize = 0 (Bài học A.7)
        long recordStart = (long)pageIndex * pageSize;
        long recordCount = pageSize;

        var dataList = (recordCount <= 0 || recordStart < 0)
            ? new List<Crd_DealSerRO>()
            : await ordered
                .Skip((int)Math.Min(recordStart, int.MaxValue))
                .Take((int)Math.Min(recordCount, int.MaxValue))
                .ToListAsync();

        // 6. Tính PageCount: khi dataList rỗng (bao gồm trường hợp pageSize = 0) thì PageCount = 0 (gate :62-70, :84-92)
        int ps = pageSize == 0 ? 10 : pageSize;
        int pageCount = dataList.Count == 0 ? 0 : (itemCount % ps == 0 ? itemCount / ps : itemCount / ps + 1);

        var pageInfo = new PageInfo<Crd_DealSerRO>(dataList, itemCount, pageCount, pageIndex, pageSize);

        return Ok(new { Success = true, Data = pageInfo });
    }

    /// <summary>
    /// Port 1:1 action Add — Thêm mới báo giá dịch vụ (Repair Order) (#021).
    /// Nguồn: Gate:188-251, API:96-166, Biz:14810-15174, Biz Core:12009-14441.
    /// </summary>
    [HttpPost("add")]
    public async Task<IActionResult> Add([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] RQ_Crd_DealSerRO? req)
    {
        return await ExecuteDealSerROAsync(req, isCalc: false);
    }

    /// <summary>
    /// Port 1:1 action Calc — Tính toán báo giá dịch vụ (Repair Order) (#020).
    /// Nguồn: Gate:113-184, API:96-166, Biz:14810-15174, Biz Core:12009-14441.
    /// </summary>
    [HttpPost("calc")]
    public async Task<IActionResult> Calc([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Crd_DealSerROCalcReq? req)
    {
        return await ExecuteDealSerROAsync(req, isCalc: true);
    }

    /// <summary>
    /// Helper dùng chung cho cả Add (#021) và Calc (#020) — đều gọi WA_Crd_DealSerRO_Add ở backend.
    /// </summary>
    private async Task<IActionResult> ExecuteDealSerROAsync(RQ_Crd_DealSerRO? req, bool isCalc)
    {
        var nowUtc = DateTime.UtcNow;
        var userCode = Request?.Headers["WAUserCode"].FirstOrDefault() ?? "SYS";
        var actionName = isCalc ? "Crd_DealSerRO_Calc" : "Crd_DealSerRO_Add";

        // Guard 1: Kiểm tra body null / rỗng (Gate:128, :203)
        var ro = req?.Crd_DealSerRO;
        if (ro == null)
        {
            return PortResult.Fail(actionName, "Check.strJson", null);
        }

        var strDealSerRONo = StdParam(ro.DealSerRONo);
        if (string.IsNullOrEmpty(strDealSerRONo))
        {
            return PortResult.Fail(actionName, "Check.strJson", null);
        }

        var strDLCode = StdParam(ro.DLCode);
        var strCardNo = StdParam(ro.CardNo);
        var strMemberNo = StdParam(ro.MemberNo);
        var strDealPointType = StdParam(ro.DealPointType);
        var strCardType = StdParam(ro.CardType);
        var strCardTypeExpect = StdParam(ro.CardTypeExpect);
        var strPolicyCode = StdParam(ro.PolicyCode);
        var strParamCode = StdParam(ro.ParamCode);
        var strRemark = StdParam(ro.Remark); // Card.Deal.cs:12124 dùng StdParam cho Remark
        var strFlagOnlyPoint = StdFlag(ro.FlagOnlyPoint);
        var strPlateNo = StdParam(ro.PlateNo);
        var strVIN = StdParam(ro.VIN);

        double dblTotalAmountRO = ro.TotalAmountRO ?? 0.0;
        double dblAmountRO = ro.AmountRO ?? 0.0;
        double dblAmountUse = ro.AmountUse ?? 0.0;
        double dblPointTotal = ro.PointTotal ?? 0.0;
        double dblPointTotalRank = ro.PointTotalRank ?? 0.0;
        double dblPointConsumptionPrm = ro.PointConsumptionPrm ?? 0.0;
        double dblPointVoucher = ro.PointVoucher ?? 0.0;
        double dblPointActualVoucher = ro.PointActualVoucher ?? 0.0;

        // Guard 2: Crd_DealSerRO_CheckDB — Kiểm tra mã báo giá đã tồn tại (Card.Deal.cs:12160, :69-80)
        var existsRO = await _db.Set<Crd_DealSerRO>()
            .AsNoTracking()
            .AnyAsync(x => x.DealSerRONo == strDealSerRONo);
        if (existsRO)
        {
            return PortResult.Fail("Crd_DealSerRO_CheckDB_Crd_DealSerROExist", "Check.DealSerRONo", strDealSerRONo);
        }

        // Guard 3 & 4: Mst_DealerCP_CheckDB — Kiểm tra đại lý tồn tại và hoạt động (Master.Loyalty.cs:2896-2930)
        var dealer = await _db.Set<Mst_DealerCP>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.DLCPCode == strDLCode);
        if (dealer == null)
        {
            return PortResult.Fail("Mst_DealerCP_CheckDB_DLNotFound", "Check.DLCPCode", ro.DLCode);
        }
        if (dealer.FlagActive != "1")
        {
            return PortResult.Fail("Mst_DealerCP_CheckDB_FlagActiveNotMatched",
                "Check.DLCPCode", ro.DLCode,
                "Check.strFlagActiveListToCheck", "1",
                "DB.FlagActive", dealer.FlagActive ?? "0");
        }

        // Guard 5: Crd_Member_CheckDB — Kiểm tra hội viên tồn tại và trạng thái (Card.cs:56-109)
        var member = await _db.CrdMembers
            .FirstOrDefaultAsync(x => x.MemberNo == strMemberNo);
        if (member == null)
        {
            return PortResult.Fail("Crd_Member_CheckDB_MemberNotFound", "Check.MemberNo", ro.MemberNo);
        }
        if (!string.Equals(member.RegisStatus, "FINISH", StringComparison.OrdinalIgnoreCase))
        {
            return PortResult.Fail("Crd_Member_CheckDB_RegisStatusNotMatched",
                "Check.MemberNo", ro.MemberNo,
                "Check.strRegisStatusListToCheck", "FINISH",
                "DB.RegisStatus", member.RegisStatus);
        }
        if (!string.Equals(member.MemberStatus, "APPROVE", StringComparison.OrdinalIgnoreCase))
        {
            return PortResult.Fail("Crd_Member_CheckDB_MemberStatusNotMatched",
                "Check.MemberNo", ro.MemberNo,
                "Check.strMemberStatusListToCheck", "APPROVE",
                "DB.MemberStatus", member.MemberStatus);
        }

        // Guard 6: Khớp Biển số xe & VIN với thông tin hội viên (Card.Deal.cs:12198-12213)
        var strPlateNoMember = StdParam(member.CarNo);
        var strVINMember = StdParam(member.VIN);
        if (!string.Equals(strPlateNo, strPlateNoMember, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(strVIN, strVINMember, StringComparison.OrdinalIgnoreCase))
        {
            return PortResult.Fail("Crd_DealSerRO_Add_PlateNoOrVINNotMatchMember",
                "Check.MemberNo", ro.MemberNo,
                "Check.PlateNo_Input", ro.PlateNo,
                "Check.VIN_Input", ro.VIN,
                "Check.PlateNo_Member", member.CarNo,
                "Check.VIN_Member", member.VIN);
        }

        // Guard 7: Crd_Card_CheckDB — Kiểm tra thẻ tồn tại và trạng thái (Card.cs:21255-21301)
        var card = await _db.CrdCards
            .FirstOrDefaultAsync(x => x.CardNo == strCardNo);
        if (card == null)
        {
            return PortResult.Fail("Crd_Card_CheckDB_CardNotFound", "Check.CardNo", ro.CardNo);
        }
        if (!string.Equals(card.CardStatus, "APPROVE", StringComparison.OrdinalIgnoreCase))
        {
            return PortResult.Fail("Crd_Card_CheckDB_CardStatusNotMatched",
                "Check.CardNo", ro.CardNo,
                "Check.strFlagActiveListToCheck", "APPROVE",
                "DB.CardStatus", card.CardStatus);
        }

        strCardType = StdParam(card.CardTypeUse) ?? strCardType;

        // Guard 8: Tiền sử dụng vượt quá tích lũy (Card.Deal.cs:12237-12251)
        double memberAmountEnd = member.AmountEnd ?? 0.0;
        if (dblAmountUse > 0 && dblAmountUse > memberAmountEnd)
        {
            return PortResult.Fail("Crd_DealSerRO_Add_Input_InvalidAmountUse",
                "Check.AmountUse", dblAmountUse,
                "Check.AmountEnd", memberAmountEnd);
        }

        // Lấy PolicyCode theo CardType và hiệu lực thời gian (Card.Deal.cs:12256-12280)
        var policyDtl = await (
            from h in _db.Set<Mst_PolicyMoneyToPointService>().AsNoTracking()
            join d in _db.Set<Mst_PolicyMoneyToPointServiceDtl>().AsNoTracking()
                on h.PolicyCode equals d.PolicyCode
            where h.FlagActive == "1"
               && h.EffDateStart <= nowUtc
               && h.EffDateEnd >= nowUtc
               && d.CardType == strCardType
            select d
        ).FirstOrDefaultAsync();

        // Ở action Calc (gate :159), Lst_Crd_DealSerRODtl bị comment out nên không truyền chi phí sang backend
        var dtlList = isCalc ? null : req?.Lst_Crd_DealSerRODtl;
        var voucherList = isCalc ? null : req?.Lst_Crd_DealSerROVoucher;

        // Guard 9: Kiểm tra danh sách chi tiết Lst_Crd_DealSerRODtl (Card.Deal.cs:12444-12473)
        if (dtlList != null)
        {
            foreach (var dtl in dtlList)
            {
                var expTypeParam = StdParam(dtl.ExpenseType);
                var exp = await _db.Set<Mst_ExpenseType>().AsNoTracking()
                    .FirstOrDefaultAsync(x => x.ExpenseType == expTypeParam);
                if (exp == null)
                {
                    return PortResult.Fail("Mst_ExpenseType_CheckDB_ExpenseTypeNotFound", "Check.ExpenseType", dtl.ExpenseType);
                }
                if (exp.FlagActive != "1")
                {
                    return PortResult.Fail("Mst_ExpenseType_CheckDB_FlagActiveNotMatched",
                        "Check.ExpenseType", dtl.ExpenseType,
                        "Check.strFlagActiveListToCheck", "1",
                        "DB.FlagActive", exp.FlagActive ?? "0");
                }
            }
        }

        // Kiểm tra điểm voucher (Card.Deal.cs:12520-12540)
        if (voucherList != null && voucherList.Count > 0)
        {
            double sumVoucher = voucherList.Sum(v => v.PointVoucher ?? 0.0);
            double sumActual = voucherList.Sum(v => v.PointActualVoucher ?? 0.0);
            if (dblPointVoucher != sumVoucher || dblPointActualVoucher != sumActual)
            {
                return PortResult.Fail("Crd_DealSerRO_Add_InvalidPointVoucher",
                    "Check.PointVoucher", dblPointVoucher,
                    "Check.SumPointVoucher", sumVoucher,
                    "Check.PointActualVoucher", dblPointActualVoucher,
                    "Check.SumPointActualVoucher", sumActual);
            }
        }

        // Thực thi trong Transaction (Card.Deal.cs:14904-15132)
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // Khóa hội viên: cập nhật LogLUDTimeUTC (Card.Deal.cs:14951-14966)
            member.LogLUDTimeUTC = nowUtc;
            member.LogLUBy = userCode;

            // Lấy danh sách Mst_PolicyExpenseType tương ứng với các khoản chi phí (Card.Deal.cs:13056-13058)
            var expenseTypes = dtlList?
                .Select(d => StdParam(d.ExpenseType))
                .Where(et => !string.IsNullOrEmpty(et))
                .Distinct()
                .ToList() ?? new List<string?>();

            var policyExpList = await _db.Set<Mst_PolicyExpenseType>()
                .AsNoTracking()
                .Where(p => expenseTypes.Contains(p.ExpenseType))
                .ToListAsync();

            // Thêm bản ghi Crd_DealSerRO (Card.Deal.cs:12670-12695)
            var dealSerRO = new Crd_DealSerRO
            {
                DealSerRONo = strDealSerRONo,
                NetworkID = member.NetworkID,
                OrgID = member.OrgID,
                DLCode = strDLCode,
                MemberNo = strMemberNo,
                CardNo = strCardNo,
                DealPointType = strDealPointType ?? "SERVICE",
                CardType = strCardType,
                CardTypeExpect = strCardTypeExpect ?? strCardType,
                ParamCode = strParamCode ?? "UNITPOINTTOMONEY",
                PolicyCode = policyDtl?.PolicyCode ?? strPolicyCode,
                TotalAmountRO = dblTotalAmountRO,
                AmountRO = dblAmountRO,
                AmountUse = dblAmountUse,
                PointTotal = dblPointTotal,
                AmountTotal = dblPointTotal,
                PointEnd = (member.PointEnd ?? 0.0) + dblPointTotal,
                AmountEnd = memberAmountEnd - dblAmountUse + dblPointTotal,
                EffDateEnd = card.EffDateEnd,
                PointAvail = card.PointAvail,
                PolicyPointUnit = ro.PolicyPointUnit,
                PolicyAmountUnit = ro.PolicyAmountUnit,
                ParamPointUnit = ro.ParamPointUnit,
                ParamAmountUnit = ro.ParamAmountUnit,
                DealSerROStatus = "PENDING",
                FlagOnlyPoint = strFlagOnlyPoint,
                Remark = strRemark,
                PlateNo = strPlateNo,
                VIN = strVIN,
                PointRankTotal = dblPointTotalRank,
                PointConsumptionPrm = dblPointConsumptionPrm,
                PointVoucher = dblPointVoucher,
                PointActualVoucher = dblPointActualVoucher,
                TotalDiscount = ro.TotalDiscount,
                TotalAmountForDC = ro.TotalAmountForDC,
                CreateDTimeUTC = nowUtc,
                CreateBy = userCode,
                LUDTimeUTC = nowUtc,
                LUBy = userCode,
                LogLUDTimeUTC = nowUtc,
                LogLUBy = userCode
            };
            _db.Set<Crd_DealSerRO>().Add(dealSerRO);

            // Thêm các dòng chi tiết Crd_DealSerRODtl (Card.Deal.cs:12700-12728)
            if (dtlList != null)
            {
                foreach (var dtl in dtlList)
                {
                    var expTypeParam = StdParam(dtl.ExpenseType)!;
                    var mpet = policyExpList.FirstOrDefault(p => p.ExpenseType == expTypeParam);
                    var dtlEntity = new Crd_DealSerRODtl
                    {
                        DealSerRONo = strDealSerRONo,
                        ExpenseType = expTypeParam,
                        NetworkID = member.NetworkID,
                        OrgID = member.OrgID,
                        PolicyExpenseTypeNo = mpet?.PolicyExpenseTypeNo ?? StdParam(dtl.PolicyExpenseTypeNo),
                        Amount = dtl.Amount ?? 0.0,
                        FlagPoint = StdFlag(dtl.FlagPoint),
                        AmountRate = mpet?.AmountRate ?? dtl.AmountRate ?? 0.0,
                        FunctionName = "Crd_DealSerRO_Add",
                        FunctionActionType = "ADD",
                        FunctionRemark = null,
                        RefType = "CRD_DEALSERRO",
                        RefCode00 = dtl.RefCode00,
                        RefCode01 = dtl.RefCode01,
                        RefCode02 = dtl.RefCode02,
                        RefCode03 = dtl.RefCode03,
                        RefCode04 = dtl.RefCode04,
                        RefCode05 = dtl.RefCode05,
                        DealSerROStatusDtl = "PENDING",
                        CreateDTimeUTC = nowUtc,
                        CreateBy = userCode,
                        LogLUDTimeUTC = nowUtc,
                        LogLUBy = userCode,
                        AmountForDC = dtl.AmountForDC ?? dtl.Amount ?? 0.0
                    };
                    _db.Set<Crd_DealSerRODtl>().Add(dtlEntity);
                }
            }

            // Sinh các giao dịch thẻ và thực hiện qua PortCardTransactionEngine (Card.Deal.cs:13229-13677, :14346)
            // Lưu ý 1:1 nguồn: Mst_ParamSys 'UNITPOINTTOMONEY' là INNER JOIN — nếu thiếu thì KHÔNG sinh giao dịch
            // (Card.Deal.cs:13298-13301, :13383-13386, :13470-13473, :13556-13559, :13641-13644).
            double? unitPointToMoney = await PortCardTransactionEngine.GetUnitPointToMoneyAsync(_db);
            if (unitPointToMoney.HasValue)
            {
                double unitPointVal = unitPointToMoney.Value;
                var txList = new List<CrdCardTransaction>();
                DateTime expiryDate = PortCardTransactionEngine.GetDateEndMonthOfNextYear(nowUtc);

                // 1. POINTUSE (Sử dụng tiền/điểm) — Draft0 (Card.Deal.cs:13229-13291)
                if (dblAmountUse > 0)
                {
                    txList.Add(new CrdCardTransaction
                    {
                        RefNo = strDealSerRONo,
                        ProjectCode = "LOYALTY",
                        NetworkID = member.NetworkID,
                        OrgID = member.OrgID,
                        DLCode = strDLCode,
                        MemberNo = member.MemberNo,
                        CardNo = card.CardNo,
                        CardSourceCode = card.CardSourceCode,
                        CardTypeUse = card.CardTypeUse,
                        CardTypeInit = card.CardTypeInit,
                        DealPointType = "POINTUSE",
                        PrProgramCode = null,
                        EffDateStart = card.EffDateStart,
                        EffDateEnd = card.EffDateEnd,
                        UsePrmDTime = null,
                        PointChTotal = 0.0,
                        PointChBlock = 0.0,
                        AmountChTotal = -1.0 * dblAmountUse,
                        AmountChBlock = 0.0,
                        QtyVisitChTotal = 0.0,
                        QtyVisitChBlock = 0.0,
                        QtyPrChTotal = 0.0,
                        QtyPrChBlock = 0.0,
                        QtyPrUsed = 0.0,
                        PointRef = 0.0,
                        AmountRef = 0.0,
                        PolicyPointUnit = ro.PolicyPointUnit,
                        PolicyAmountUnit = ro.PolicyAmountUnit,
                        PolicyCode = "",
                        ParamPointUnit = ro.ParamPointUnit,
                        ParamAmountUnit = unitPointVal,
                        ParamCode = "UNITPOINTTOMONEY",
                        FlagViewUI = "1",
                        FlagPoint = "0",
                        FlagAmount = "1",
                        FlagPr = "0",
                        FunctionName = "Crd_DealSerRO_Add",
                        FunctionActionType = "ADD",
                        FunctionRemark = null,
                        RefType = "CRD_DEALSERRO",
                        RefCode00 = null,
                        RefCode01 = null,
                        RefCode02 = null,
                        RefCode03 = null,
                        RefCode04 = member.MemberStatus,
                        RefCode05 = card.CardStatus,
                        RefCode06 = null,
                        RefCode07 = strDealSerRONo,
                        RefCode08 = null,
                        RefCode09 = "",
                        CreateDTimeUTC = nowUtc,
                        CreateBy = userCode,
                        LUDTimeUTC = nowUtc,
                        LUBy = userCode,
                        LogLUDTimeUTC = nowUtc,
                        LogLUBy = userCode,
                        PointChRankTotal = 0.0,
                        PointExpiryDTime = null,
                        AmountChTotalInit = 0.0,
                        AmountChTotalDiffer = 0.0,
                        AmountAvailPrevious = memberAmountEnd
                    });
                }

                // Tính điểm từ chi tiết theo chính sách (Card.Deal.cs:13016-13224)
                double pointRankTotal_AmountRO = 0.0;
                double pointRankTotal_PointRef = 0.0;
                double pointRankTotal_PointChRankTotal = 0.0;
                double pointRankTotal_AmountRef = 0.0;

                double amountChTotal_AmountRO = 0.0;
                double amountChTotal_PointChTotal = 0.0;
                double amountChTotal_AmountChTotal = 0.0;
                double amountChTotal_PointRef = 0.0;
                double amountChTotal_AmountRef = 0.0;

                double countService_AmountRef = 0.0;

                if (dtlList != null && dtlList.Count > 0)
                {
                    double cv = policyDtl?.ConvertValue ?? 0.0;
                    double cp = policyDtl?.ConvertPoint ?? 0.0;

                    foreach (var dtl in dtlList)
                    {
                        var expTypeParam = StdParam(dtl.ExpenseType);
                        var mpet = policyExpList.FirstOrDefault(p => p.ExpenseType == expTypeParam);

                        double amount = dtl.Amount ?? 0.0;
                        double ar = mpet?.AmountRate ?? 0.0;
                        double maxRank = mpet?.MaxRankReviewPoint ?? 0.0;
                        double maxAccum = mpet?.MaxAccumulationPoint ?? 0.0;
                        string flagPointRank = mpet?.FlagPointRank ?? "0";
                        string flagPoint = mpet?.FlagPoint ?? "0";
                        string flagCountService = mpet?.FlagCountService ?? "0";

                        // PointRank, PointChRank (#tbl_Crd_DealSerRODtl_PointTemp: :13032-13045)
                        double pointRank = 0.0;
                        double pointChRank = 0.0;
                        if (cv > 0.0 && mpet != null)
                        {
                            pointRank = (amount * ar) * (cp / cv);
                            pointChRank = (maxRank > 0.0 && pointRank > maxRank) ? maxRank : pointRank;
                        }

                        // AmountRank (#tbl_Crd_DealSerRODtl_AmountRank: :13076-13086)
                        if (flagPointRank == "1")
                        {
                            double amountRank = (maxRank > 0.0 && pointRank > maxRank && cp > 0.0 && ar > 0.0)
                                ? (pointChRank / cp) * (cv / ar)
                                : amount;

                            pointRankTotal_AmountRO += amount;
                            pointRankTotal_PointRef += pointRank;
                            pointRankTotal_PointChRankTotal += pointChRank;
                            pointRankTotal_AmountRef += amountRank;
                        }

                        // PointCh, AmountCh (#tbl_Crd_DealSerRODtl_PointCh, AmountCh: :13124-13157)
                        if (flagPoint == "1")
                        {
                            double pointCh = (maxAccum > 0.0 && pointChRank > maxAccum) ? maxAccum : pointChRank;
                            double amountCh = (maxAccum > 0.0 && pointRank > maxAccum && cp > 0.0 && ar > 0.0)
                                ? (pointCh / cp) * (cv / ar)
                                : amount;

                            amountChTotal_AmountRO += amount;
                            amountChTotal_PointChTotal += pointCh;
                            amountChTotal_AmountChTotal += pointCh; // Nguồn dòng 13170: Sum(PointCh) AmountChTotal
                            amountChTotal_PointRef += pointRank;
                            amountChTotal_AmountRef += amountCh;
                        }

                        // CountService (#tbl_Crd_DealSerRODtl_CountService: :13187-13204)
                        if (flagCountService == "1")
                        {
                            countService_AmountRef += amount;
                        }
                    }
                }

                // 2. POINTINCREASE (Tích điểm xét hạng) — Draft1 (Card.Deal.cs:13309-13371)
                if (pointRankTotal_PointChRankTotal > 0.0)
                {
                    txList.Add(new CrdCardTransaction
                    {
                        RefNo = strDealSerRONo,
                        ProjectCode = "LOYALTY",
                        NetworkID = member.NetworkID,
                        OrgID = member.OrgID,
                        DLCode = strDLCode,
                        MemberNo = member.MemberNo,
                        CardNo = card.CardNo,
                        CardSourceCode = card.CardSourceCode,
                        CardTypeUse = card.CardTypeUse,
                        CardTypeInit = card.CardTypeInit,
                        DealPointType = "POINTINCREASE",
                        PrProgramCode = null,
                        EffDateStart = card.EffDateStart,
                        EffDateEnd = card.EffDateEnd,
                        UsePrmDTime = null,
                        PointChTotal = 0.0,
                        PointChBlock = 0.0,
                        AmountChTotal = 0.0,
                        AmountChBlock = 0.0,
                        QtyVisitChTotal = 0.0,
                        QtyVisitChBlock = 0.0,
                        QtyPrChTotal = 0.0,
                        QtyPrChBlock = 0.0,
                        QtyPrUsed = 0.0,
                        PointRef = pointRankTotal_PointRef,
                        AmountRef = pointRankTotal_AmountRef,
                        PolicyPointUnit = ro.PolicyPointUnit,
                        PolicyAmountUnit = ro.PolicyAmountUnit,
                        PolicyCode = policyDtl?.PolicyCode ?? strPolicyCode,
                        ParamPointUnit = ro.ParamPointUnit,
                        ParamAmountUnit = unitPointVal,
                        ParamCode = "UNITPOINTTOMONEY",
                        FlagViewUI = "1",
                        FlagPoint = "1",
                        FlagAmount = "1",
                        FlagPr = "0",
                        FunctionName = "Crd_DealSerRO_Add",
                        FunctionActionType = "ADD",
                        FunctionRemark = null,
                        RefType = "CRD_DEALSERRO",
                        RefCode00 = null,
                        RefCode01 = null,
                        RefCode02 = null,
                        RefCode03 = null,
                        RefCode04 = member.MemberStatus,
                        RefCode05 = card.CardStatus,
                        RefCode06 = null,
                        RefCode07 = strDealSerRONo,
                        RefCode08 = null,
                        RefCode09 = pointRankTotal_AmountRO.ToString(),
                        CreateDTimeUTC = nowUtc,
                        CreateBy = userCode,
                        LUDTimeUTC = nowUtc,
                        LUBy = userCode,
                        LogLUDTimeUTC = nowUtc,
                        LogLUBy = userCode,
                        PointChRankTotal = pointRankTotal_PointChRankTotal,
                        PointExpiryDTime = null,
                        AmountChTotalInit = 0.0,
                        AmountChTotalDiffer = 0.0,
                        AmountAvailPrevious = 0.0
                    });
                }

                // 3. CONSUMPTION (Tích điểm tiêu dùng) — Draft2 (Card.Deal.cs:13395-13458)
                if (amountChTotal_PointChTotal > 0.0)
                {
                    txList.Add(new CrdCardTransaction
                    {
                        RefNo = strDealSerRONo,
                        ProjectCode = "LOYALTY",
                        NetworkID = member.NetworkID,
                        OrgID = member.OrgID,
                        DLCode = strDLCode,
                        MemberNo = member.MemberNo,
                        CardNo = card.CardNo,
                        CardSourceCode = card.CardSourceCode,
                        CardTypeUse = card.CardTypeUse,
                        CardTypeInit = card.CardTypeInit,
                        DealPointType = "CONSUMPTION",
                        PrProgramCode = null,
                        EffDateStart = card.EffDateStart,
                        EffDateEnd = card.EffDateEnd,
                        UsePrmDTime = null,
                        PointChTotal = amountChTotal_PointChTotal,
                        PointChBlock = 0.0,
                        AmountChTotal = amountChTotal_AmountChTotal,
                        AmountChBlock = 0.0,
                        QtyVisitChTotal = 0.0,
                        QtyVisitChBlock = 0.0,
                        QtyPrChTotal = 0.0,
                        QtyPrChBlock = 0.0,
                        QtyPrUsed = 0.0,
                        PointRef = amountChTotal_PointRef,
                        AmountRef = amountChTotal_AmountRef,
                        PolicyPointUnit = ro.PolicyPointUnit,
                        PolicyAmountUnit = ro.PolicyAmountUnit,
                        PolicyCode = policyDtl?.PolicyCode ?? strPolicyCode,
                        ParamPointUnit = ro.ParamPointUnit,
                        ParamAmountUnit = unitPointVal,
                        ParamCode = "UNITPOINTTOMONEY",
                        FlagViewUI = "1",
                        FlagPoint = "1",
                        FlagAmount = "1",
                        FlagPr = "0",
                        FunctionName = "Crd_DealSerRO_Add",
                        FunctionActionType = "ADD",
                        FunctionRemark = null,
                        RefType = "CRD_DEALSERRO",
                        RefCode00 = null,
                        RefCode01 = null,
                        RefCode02 = null,
                        RefCode03 = null,
                        RefCode04 = member.MemberStatus,
                        RefCode05 = card.CardStatus,
                        RefCode06 = null,
                        RefCode07 = strDealSerRONo,
                        RefCode08 = null,
                        RefCode09 = amountChTotal_AmountRO.ToString(),
                        CreateDTimeUTC = nowUtc,
                        CreateBy = userCode,
                        LUDTimeUTC = nowUtc,
                        LUBy = userCode,
                        LogLUDTimeUTC = nowUtc,
                        LogLUBy = userCode,
                        PointChRankTotal = 0.0,
                        PointExpiryDTime = expiryDate,
                        AmountChTotalInit = 0.0,
                        AmountChTotalDiffer = 0.0,
                        AmountAvailPrevious = memberAmountEnd - dblAmountUse
                    });
                }

                // 4. SERVICETURN (Lượt dịch vụ) — Draft3 (Card.Deal.cs:13482-13561)
                double valRankCardType = policyDtl?.ValueRankCardType ?? 0.0;
                double qtyVisitChTotal = (valRankCardType > 0.0 && countService_AmountRef >= valRankCardType) ? 1.0 : 0.0;
                if (qtyVisitChTotal > 0.0)
                {
                    txList.Add(new CrdCardTransaction
                    {
                        RefNo = strDealSerRONo,
                        ProjectCode = "LOYALTY",
                        NetworkID = member.NetworkID,
                        OrgID = member.OrgID,
                        DLCode = strDLCode,
                        MemberNo = member.MemberNo,
                        CardNo = card.CardNo,
                        CardSourceCode = card.CardSourceCode,
                        CardTypeUse = card.CardTypeUse,
                        CardTypeInit = card.CardTypeInit,
                        DealPointType = "SERVICETURN",
                        PrProgramCode = null,
                        EffDateStart = card.EffDateStart,
                        EffDateEnd = card.EffDateEnd,
                        UsePrmDTime = null,
                        PointChTotal = 0.0,
                        PointChBlock = 0.0,
                        AmountChTotal = 0.0,
                        AmountChBlock = 0.0,
                        QtyVisitChTotal = qtyVisitChTotal,
                        QtyVisitChBlock = 0.0,
                        QtyPrChTotal = 0.0,
                        QtyPrChBlock = 0.0,
                        QtyPrUsed = 0.0,
                        PointRef = 0.0,
                        AmountRef = countService_AmountRef,
                        PolicyPointUnit = ro.PolicyPointUnit,
                        PolicyAmountUnit = ro.PolicyAmountUnit,
                        PolicyCode = "",
                        ParamPointUnit = ro.ParamPointUnit,
                        ParamAmountUnit = unitPointVal,
                        ParamCode = "UNITPOINTTOMONEY",
                        FlagViewUI = "1",
                        FlagPoint = "0",
                        FlagAmount = "1",
                        FlagPr = "0",
                        FunctionName = "Crd_DealSerRO_Add",
                        FunctionActionType = "ADD",
                        FunctionRemark = null,
                        RefType = "CRD_DEALSERRO",
                        RefCode00 = null,
                        RefCode01 = null,
                        RefCode02 = null,
                        RefCode03 = null,
                        RefCode04 = member.MemberStatus,
                        RefCode05 = card.CardStatus,
                        RefCode06 = null,
                        RefCode07 = strDealSerRONo,
                        RefCode08 = null,
                        RefCode09 = "",
                        CreateDTimeUTC = nowUtc,
                        CreateBy = userCode,
                        LUDTimeUTC = nowUtc,
                        LUBy = userCode,
                        LogLUDTimeUTC = nowUtc,
                        LogLUBy = userCode,
                        PointChRankTotal = 0.0,
                        PointExpiryDTime = null,
                        AmountChTotalInit = 0.0,
                        AmountChTotalDiffer = 0.0,
                        AmountAvailPrevious = memberAmountEnd
                    });
                }

                // 5. CONSUMPTIONPRM (Tích điểm tiêu dùng khuyến mại) — Draft4 (Card.Deal.cs:13567-13647)
                if (amountChTotal_PointChTotal > 0.0 && dblPointConsumptionPrm > 0.0)
                {
                    txList.Add(new CrdCardTransaction
                    {
                        RefNo = strDealSerRONo,
                        ProjectCode = "LOYALTY",
                        NetworkID = member.NetworkID,
                        OrgID = member.OrgID,
                        DLCode = "HTV",
                        MemberNo = member.MemberNo,
                        CardNo = card.CardNo,
                        CardSourceCode = card.CardSourceCode,
                        CardTypeUse = card.CardTypeUse,
                        CardTypeInit = card.CardTypeInit,
                        DealPointType = "CONSUMPTIONPRM",
                        PrProgramCode = null,
                        EffDateStart = card.EffDateStart,
                        EffDateEnd = card.EffDateEnd,
                        UsePrmDTime = null,
                        PointChTotal = dblPointConsumptionPrm,
                        PointChBlock = 0.0,
                        AmountChTotal = dblPointConsumptionPrm,
                        AmountChBlock = 0.0,
                        QtyVisitChTotal = 0.0,
                        QtyVisitChBlock = 0.0,
                        QtyPrChTotal = 0.0,
                        QtyPrChBlock = 0.0,
                        QtyPrUsed = 0.0,
                        PointRef = 0.0,
                        AmountRef = 0.0,
                        PolicyPointUnit = ro.PolicyPointUnit,
                        PolicyAmountUnit = ro.PolicyAmountUnit,
                        PolicyCode = policyDtl?.PolicyCode ?? strPolicyCode,
                        ParamPointUnit = ro.ParamPointUnit,
                        ParamAmountUnit = unitPointVal,
                        ParamCode = "UNITPOINTTOMONEY",
                        FlagViewUI = "1",
                        FlagPoint = "1",
                        FlagAmount = "1",
                        FlagPr = "0",
                        FunctionName = "Crd_DealSerRO_Add",
                        FunctionActionType = "ADD",
                        FunctionRemark = null,
                        RefType = "CRD_DEALSERRO",
                        RefCode00 = null,
                        RefCode01 = null,
                        RefCode02 = null,
                        RefCode03 = null,
                        RefCode04 = member.MemberStatus,
                        RefCode05 = card.CardStatus,
                        RefCode06 = null,
                        RefCode07 = strDealSerRONo,
                        RefCode08 = null,
                        RefCode09 = dblPointConsumptionPrm.ToString(),
                        CreateDTimeUTC = nowUtc,
                        CreateBy = userCode,
                        LUDTimeUTC = nowUtc,
                        LUBy = userCode,
                        LogLUDTimeUTC = nowUtc,
                        LogLUBy = userCode,
                        PointChRankTotal = 0.0,
                        PointExpiryDTime = expiryDate,
                        AmountChTotalInit = 0.0,
                        AmountChTotalDiffer = 0.0,
                        AmountAvailPrevious = memberAmountEnd - dblAmountUse + amountChTotal_AmountChTotal
                    });
                }

                if (txList.Count > 0)
                {
                    await PortCardTransactionEngine.Crd_CardTransaction_PerformAsync(_db, txList, nowUtc, userCode);
                }

                // Xét duyệt tăng hạng thẻ khi phát sinh giao dịch (Card.Deal.cs:15066, CardRank.cs:2304-2652)
                await PerformRankPolicyCheckAsync(_db, member, card, nowUtc, userCode);
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            // Trả kết quả: Calc trả { Success = true, Data = { Crd_DealSerRO = null } }, Add trả { Success = true, Data = null }
            if (isCalc)
            {
                return Ok(new { Success = true, Data = new { Crd_DealSerRO = (Crd_DealSerRO?)null } });
            }
            return Ok(new { Success = true, Data = (object?)null });
        }
        catch (PortThresholdException pte)
        {
            await tx.RollbackAsync();
            return PortResult.Fail("Crd_CardTransaction_Perform_QtyChangeOverThreshold", pte.Params);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return PortResult.Fail(actionName, "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Xét duyệt tăng hạng thẻ — port 1:1 từ Crd_CardRankPolicy_PerformX_New20251118 (CardRank.cs:2304-2652)
    /// và Mst_RankPolicy_GetUp_New20260810 (CardRank.cs:8740-8770).
    /// </summary>
    private static async Task PerformRankPolicyCheckAsync(AppDbContext db, CrdMember member, CrdCard card, DateTime nowUtc, string userCode)
    {
        var currentCardType = StdParam(card.CardTypeUse);
        if (string.IsNullOrEmpty(currentCardType)) return;

        var currentPolicy = await db.Set<Mst_RankPolicy>()
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.CardType == currentCardType && p.FlagActive == "1");

        if (currentPolicy == null) return;

        double qtyVisitAvail = member.QtyVisitAvail ?? 0.0;
        double pointCardRank = member.PointCardRank ?? 0.0;
        double qtyVisitUpBegin = currentPolicy.QtyVisitUpBegin ?? 0.0;
        double pointUpBegin = currentPolicy.PointUpBegin ?? 0.0;

        // Xét điều kiện nâng hạng: QtyVisitUpBegin <= QtyVisitAvail && PointUpBegin <= PointCardRank (CardRank.cs:2397-2399)
        if (qtyVisitUpBegin <= qtyVisitAvail && pointUpBegin <= pointCardRank)
        {
            // Mst_RankPolicy_GetUp_New20260810: Tìm hạng tiếp theo
            double currentValue = currentPolicy.Value ?? 0.0;
            var nextPolicy = await db.Set<Mst_RankPolicy>()
                .AsNoTracking()
                .Where(t => t.CardType != currentCardType
                         && t.Value > currentValue
                         && t.CardType != "N"
                         && t.FlagActive == "1")
                .OrderBy(t => t.Value)
                .FirstOrDefaultAsync();

            if (nextPolicy != null)
            {
                var nextCardType = StdParam(nextPolicy.CardType) ?? nextPolicy.CardType;

                // Cập nhật thẻ và hội viên
                card.CardTypeUse = nextCardType;
                card.CardTypeInit = nextCardType;
                card.CardSourceCode = "UP";
                card.LogLUDTimeUTC = nowUtc;
                card.LogLUBy = userCode;
                card.LUDTimeUTC = nowUtc;
                card.LUBy = userCode;

                member.CardTypeUse = nextCardType;
                member.CardTypeInit = nextCardType;
                member.PointCardRank = 0.0; // CardRank.cs:2649: strFN = "PointCardRank"; drDB[strFN] = 0.0;
                member.LogLUDTimeUTC = nowUtc;
                member.LogLUBy = userCode;
                member.LUDTimeUTC = nowUtc;
                member.LUBy = userCode;

                // Ghi nhận lịch sử xét hạng vào Crd_CardRank (CardRank.cs:2451-2480, :2785-2814)
                var cardRankNo = "CRD_RANK." + nowUtc.ToString("yyyyMMddHHmmssfff");
                db.Set<Crd_CardRank>().Add(new Crd_CardRank
                {
                    CardRankNo = cardRankNo,
                    NetworkID = member.NetworkID ?? "",
                    OrgID = member.OrgID ?? "",
                    RankPolicyCode = currentPolicy.RankPolicyCode,
                    CardSourceCode = "UP",
                    DealPointType = "LOYALTY",
                    FunctionName = "Crd_DealSerRO_Add",
                    FunctionActionType = "NONE",
                    FunctionRemark = "Crd_CardRankPolicy_PerformX",
                    RefType = "ADD",
                    CreateDTimeUTC = nowUtc,
                    CreateBy = userCode,
                    LUDTimeUTC = nowUtc,
                    LUBy = userCode,
                    LogLUDTimeUTC = nowUtc,
                    LogLUBy = userCode
                });
            }
        }
    }
}

