using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MiniLoyalty.Data;
using MiniLoyalty.Models;
using MiniLoyalty.Models.Port;

namespace MiniLoyalty.Controllers.Port;

/// <summary>
/// DTO nhận tham số tìm kiếm cho Crd_MemberChangeInfoController/Search (#027).
/// Khớp danh sách 9 tham số của gate: Crd_MemberChangeInfoController.cs:33.
/// Thêm OrgID tuỳ chọn theo bài học A.14 (không lọc trong WhereClause).
/// </summary>
public class Crd_MemberChangeInfoSearchReq
{
    public string? DLCPCode { get; set; }
    public string? MemberNo { get; set; }
    public string? CreateDTimeUTCFrom { get; set; }
    public string? CreateDTimeUTCTo { get; set; }
    public string? RequestNo { get; set; }
    public string? RequestStatus { get; set; }
    public string? CarNo { get; set; }
    public string? FinishDTimeUTCFrom { get; set; }
    public string? FinishDTimeUTCTo { get; set; }
    public string? OrgID { get; set; }
}

/// <summary>
/// DTO nhận tham số cho Crd_MemberChangeInfoController/GetByRequestNo (#029).
/// Gate: Crd_MemberChangeInfoController.cs:213.
/// </summary>
public class Crd_MemberChangeInfoGetByRequestNoReq
{
    public string? RequestNo { get; set; }
}

/// <summary>
/// DTO nhận tham số cho Crd_MemberChangeInfoController/GetHistoryByMember (#030).
/// Gate: Crd_MemberChangeInfoController.cs:306.
/// </summary>
public class Crd_MemberChangeInfoGetHistoryByMemberReq
{
    public string? MemberNo { get; set; }
    public string? FinishDTimeFrom { get; set; }
    public string? FinishDTimeTo { get; set; }
    public string? OrgID { get; set; }
    public string? DLCPCode { get; set; }
}

/// <summary>
/// DTO nhận tham số cho Crd_MemberChangeInfoController/GetMemberColumnChange (#031).
/// Gate: Crd_MemberChangeInfoController.cs:447.
/// Thêm OrgID tuỳ chọn theo bài học A.14.
/// </summary>
public class Crd_MemberColumnChangeGetReq
{
    public string? OrgID { get; set; }
}

/// <summary>
/// DTO nhận tham số cho Crd_MemberChangeInfoController/Save (#032).
/// Gate: D:\idocNet\2022.7.Loyalty\13.ClientGate\V10\idn.Loyalty.WebAPI\Controllers\Crd_MemberChangeInfoController.cs:516-578
/// API: D:\idocNet\2022.7.Loyalty\11.BackEnd\V10\idn.Loyalty.Biz.Web\Controllers\CrdMemberChangeInfoController.cs:169-239
/// Biz: D:\idocNet\2022.7.Loyalty\11.BackEnd\V10\idn.Loyalty.Biz\Card.cs:29560-29659 (WAS_), 29399-29558 (Save), 28667-29397 (SaveX)
/// </summary>

// Gate :546-561 chỉ chuyển Crd_MemberChangeInfo; FlagIsDelete và Lst_Crd_MemberChangeInfoDtl không bao giờ được gán (=> null).
public class Crd_MemberChangeInfoSaveReq
{

    public Crd_MemberChangeInfo? Crd_MemberChangeInfo { get; set; }
}

/// <summary>
/// Port 1:1 Crd_MemberChangeInfoController — Crd_MemberChangeInfo (#027).
/// Gate: D:\idocNet\2022.7.Loyalty\13.ClientGate\V10\idn.Loyalty.WebAPI\Controllers\Crd_MemberChangeInfoController.cs:33-144
/// API: D:\idocNet\2022.7.Loyalty\11.BackEnd\V10\idn.Loyalty.Biz.Web\Controllers\CrdMemberChangeInfoController.cs:23-93
/// Biz: D:\idocNet\2022.7.Loyalty\11.BackEnd\V10\idn.Loyalty.Biz\Card.cs:28443-28557 (WAS_), 28282-28441 (Get), 27978-28280 (GetX)
/// </summary>
[ApiController]
[Route("api/crd_memberchangeinfo")]
public class Crd_MemberChangeInfoControllerPort : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration? _config;

    public Crd_MemberChangeInfoControllerPort(AppDbContext db, IConfiguration? config = null)
    {
        _db = db;
        _config = config;
    }

    /// <summary>
    /// Tìm kiếm danh sách đề nghị thay đổi thông tin hội viên (Action Search).
    /// Gate: Crd_MemberChangeInfoController.cs:33-144.
    /// </summary>
    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Crd_MemberChangeInfoSearchReq? req)
    {
        req ??= new Crd_MemberChangeInfoSearchReq();

        // 1. Chuẩn hoá khoảng thời gian trước khi lọc (Gate :54-69)
        var createFrom = req.CreateDTimeUTCFrom;
        if (!string.IsNullOrEmpty(createFrom))
        {
            createFrom = createFrom + " 00:00:00";
        }

        var createTo = req.CreateDTimeUTCTo;
        if (!string.IsNullOrEmpty(createTo))
        {
            createTo = createTo + " 23:59:59";
        }

        var finishFrom = req.FinishDTimeUTCFrom;
        if (!string.IsNullOrEmpty(finishFrom))
        {
            finishFrom = finishFrom + " 00:00:00";
        }

        var finishTo = req.FinishDTimeUTCTo;
        if (!string.IsNullOrEmpty(finishTo))
        {
            finishTo = finishTo + " 23:59:59";
        }

        // 2. Truy vấn kết hợp Crd_MemberChangeInfo và Crd_Member (Card.cs:28051-28066)
        // Bảng tạm #tbl_Crd_MemberChangeInfo_Filter_Draft thực hiện INNER JOIN Crd_Member trên MemberNo:
        // Đề nghị có MemberNo không tồn tại trong Crd_Member sẽ bị loại bỏ.
        var query = from cmci in _db.Set<Crd_MemberChangeInfo>()
                    join cm in _db.CrdMembers on cmci.MemberNo equals cm.MemberNo
                    select new { cmci, cm };

        // 3. Mệnh đề điều kiện lọc theo strWhereClause_Crd_MemberChangeInfo (Gate :974-1025)

        // DLCPCode: lọc Crd_MemberChangeInfo.DLCodeRequest với toán tử = (Gate :981-984)
        if (!string.IsNullOrEmpty(req.DLCPCode))
        {
            var p_DLCPCode = PortQuery.Equal(req.DLCPCode);
            query = query.Where(x => EF.Functions.Like((x.cmci.DLCodeRequest ?? "").ToLower(), p_DLCPCode, PortQuery.Esc));
        }

        // MemberNo: lọc Crd_MemberChangeInfo.MemberNo với toán tử = (Gate :985-988)
        if (!string.IsNullOrEmpty(req.MemberNo))
        {
            var p_MemberNo = PortQuery.Equal(req.MemberNo);
            query = query.Where(x => EF.Functions.Like((x.cmci.MemberNo ?? "").ToLower(), p_MemberNo, PortQuery.Esc));
        }

        // RequestNo: lọc Crd_MemberChangeInfo.RequestNo với toán tử = (Gate :989-992)
        if (!string.IsNullOrEmpty(req.RequestNo))
        {
            var p_RequestNo = PortQuery.Equal(req.RequestNo);
            query = query.Where(x => EF.Functions.Like((x.cmci.RequestNo ?? "").ToLower(), p_RequestNo, PortQuery.Esc));
        }

        // CreateDTimeUTCFrom: lọc Crd_MemberChangeInfo.CreateDTimeUTC với toán tử >= (Gate :993-996)
        if (!string.IsNullOrEmpty(createFrom))
        {
            query = query.Where(x => x.cmci.CreateDTimeUTC != null && string.Compare(x.cmci.CreateDTimeUTC, createFrom) >= 0);
        }

        // CreateDTimeUTCTo: lọc Crd_MemberChangeInfo.CreateDTimeUTC với toán tử <= (Gate :997-1000)
        if (!string.IsNullOrEmpty(createTo))
        {
            query = query.Where(x => x.cmci.CreateDTimeUTC != null && string.Compare(x.cmci.CreateDTimeUTC, createTo) <= 0);
        }

        // FinishDTimeUTCFrom: lọc Crd_MemberChangeInfo.FinishDTimeUTC với toán tử >= (Gate :1001-1004)
        if (!string.IsNullOrEmpty(finishFrom))
        {
            query = query.Where(x => x.cmci.FinishDTimeUTC != null && string.Compare(x.cmci.FinishDTimeUTC, finishFrom) >= 0);
        }

        // FinishDTimeUTCTo: lọc Crd_MemberChangeInfo.FinishDTimeUTC với toán tử <= (Gate :1005-1008)
        if (!string.IsNullOrEmpty(finishTo))
        {
            query = query.Where(x => x.cmci.FinishDTimeUTC != null && string.Compare(x.cmci.FinishDTimeUTC, finishTo) <= 0);
        }

        // CarNo: lọc trên bảng Crd_Member.CarNo với toán tử = (Gate :1009-1012)
        if (!string.IsNullOrEmpty(req.CarNo))
        {
            var p_CarNo = PortQuery.Equal(req.CarNo);
            query = query.Where(x => EF.Functions.Like((x.cm.CarNo ?? "").ToLower(), p_CarNo, PortQuery.Esc));
        }

        // RequestStatus: tách mảng bằng dấu phẩy và lọc toán tử IN (Gate :1013-1021)
        if (!string.IsNullOrEmpty(req.RequestStatus))
        {
            var statusList = req.RequestStatus.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                              .Select(p => p.Trim().ToLowerInvariant())
                                              .ToList();
            if (statusList.Count > 0)
            {
                query = query.Where(x => statusList.Contains((x.cmci.RequestStatus ?? "").ToLower()));
            }
        }

        // Không có điều kiện lọc theo OrgID hay KeyWord trong mệnh đề WhereClause (Gate :974-1025)

        // 4. Sắp xếp thứ tự: cmci.LogLUDTimeUTC asc (Card.cs:28065)
        query = query.OrderBy(x => x.cmci.LogLUDTimeUTC);

        var rawList = await query.ToListAsync();

        // 5. Ánh xạ các cột hiển thị và tính toán (Card.cs:28100-28164)
        // Đại lý của user hiện tại (để đánh cờ FlagApproveAF): 'HTV' = HTV, ngược lại = đại lý (Card.cs:28025)
        var userDLCPCode = !string.IsNullOrWhiteSpace(req.DLCPCode) ? req.DLCPCode.Trim().ToUpperInvariant() : "HTV";

        var resultList = new List<Crd_MemberChangeInfo>();
        foreach (var row in rawList)
        {
            var cmci = row.cmci;
            var cm = row.cm;

            // Đơn vị duyệt: prefix 'HTV,' (Card.cs:28138)
            var approveDLCode = "HTV," + (cmci.ApproveDLCode ?? "");

            // FlagApproveAF: 1 nếu trạng thái PENDING/APPROVE và user có quyền duyệt (HTV hoặc khớp ApproveDLCode) (Card.cs:28145-28151)
            var canApprove = (userDLCPCode == "HTV" || string.Equals(cmci.ApproveDLCode, userDLCPCode, StringComparison.OrdinalIgnoreCase));
            var isPendingOrApprove = (cmci.RequestStatus == "PENDING" || cmci.RequestStatus == "APPROVE");
            var flagApproveAF = (isPendingOrApprove && canApprove) ? "1" : "0";

            var item = new Crd_MemberChangeInfo
            {
                RequestNo = cmci.RequestNo,
                NetworkID = cmci.NetworkID,
                OrgID = cmci.OrgID,
                MemberNo = cmci.MemberNo,
                DLCodeRequest = cmci.DLCodeRequest, // strFlagIsLookupByMember = "0" -> giữ nguyên (Card.cs:28115, 28500)
                RequestType = cmci.RequestType,
                RequestStatus = cmci.RequestStatus,
                Remark = cmci.Remark,
                RemarkHTV = cmci.RemarkHTV,
                AttachFilePath = cmci.AttachFilePath,
                AttachFileName = cmci.AttachFileName,
                AttachFileSize = cmci.AttachFileSize,
                AttachFileBase64 = cmci.AttachFileBase64,
                CreateDTimeUTC = cmci.CreateDTimeUTC,
                CreateBy = cmci.CreateBy,
                LUDTimeUTC = cmci.LUDTimeUTC,
                LUBy = cmci.LUBy,
                ApproveDTimeUTC = cmci.ApproveDTimeUTC,
                ApproveBy = cmci.ApproveBy,
                FinishDTimeUTC = cmci.FinishDTimeUTC,
                FinishBy = cmci.FinishBy,
                RejectDTimeUTC = cmci.RejectDTimeUTC,
                RejectBy = cmci.RejectBy,
                LogLUDTimeUTC = cmci.LogLUDTimeUTC,
                LogLUBy = cmci.LogLUBy,
                FlagFileUpload = cmci.FlagFileUpload,
                AttFileId = cmci.AttFileId,

                // Các cột mở rộng từ bảng Crd_Member (Card.cs:28139-28144)
                cm_MemberName = cm.MemberName,
                cm_CarNo = cm.CarNo,
                cm_PhoneNo = cm.PhoneNo,
                cm_VIN = cm.VIN,
                cm_AmountEnd = cm.AmountEnd,
                cm_CardTypeUse = cm.CardTypeUse,

                ApproveDLCode = approveDLCode,
                FlagApproveAF = flagApproveAF
            };

            // 6. Chuyển đổi giờ UTC sang Local Time (+7 giờ VN, format "yyyy-MM-dd HH:mm:ss") cho 4 trường (Gate :105-125):
            // LUDTimeUTC, ApproveDTimeUTC, FinishDTimeUTC, CreateDTimeUTC
            item.LUDTimeUTC = ConvertUtcToLocal(item.LUDTimeUTC);
            item.ApproveDTimeUTC = ConvertUtcToLocal(item.ApproveDTimeUTC);
            item.FinishDTimeUTC = ConvertUtcToLocal(item.FinishDTimeUTC);
            item.CreateDTimeUTC = ConvertUtcToLocal(item.CreateDTimeUTC);

            resultList.Add(item);
        }

        // 7. Kết quả trả về: objJsonResultEntry.Data = new { Lst_Crd_MemberChangeInfo = ... } (Gate :101-104)
        // Nếu danh sách rỗng, Lst_Crd_MemberChangeInfo giữ nguyên giá trị khởi tạo là null (Gate :70)
        return Ok(new
        {
            Success = true,
            Data = new
            {
                Lst_Crd_MemberChangeInfo = resultList.Count > 0 ? resultList : null
            }
        });
    }

    /// <summary>
    /// Đếm số lượng đề nghị mới (RequestStatus = "PENDING" trong vòng 1 tháng gần đây theo UTC) (#028).
    /// Gate: D:\idocNet\2022.7.Loyalty\13.ClientGate\V10\idn.Loyalty.WebAPI\Controllers\Crd_MemberChangeInfoController.cs:147-211
    /// API: D:\idocNet\2022.7.Loyalty\11.BackEnd\V10\idn.Loyalty.Biz.Web\Controllers\CrdMemberChangeInfoController.cs:23-93
    /// Biz: D:\idocNet\2022.7.Loyalty\11.BackEnd\V10\idn.Loyalty.Biz\Card.cs:28443-28557 (WAS_), 28282-28441 (Get), 27978-28280 (GetX)
    /// </summary>
    [HttpPost("getcountnewrequest")]
    public async Task<IActionResult> GetCountNewRequest([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] object? body = null)
    {
        // 1. Mốc thời gian 1 tháng gần đây theo UTC: format "yyyy-MM-dd HH:mm:ss" (Gate :166)
        var strFromDTimeUTC = DateTime.UtcNow.AddMonths(-1).ToString("yyyy-MM-dd HH:mm:ss");

        // 2. Truy vấn kết hợp Crd_MemberChangeInfo và Crd_Member (Card.cs:28051-28066)
        // Bảng draft #tbl_Crd_MemberChangeInfo_Filter_Draft thực hiện INNER JOIN Crd_Member trên MemberNo (Card.cs:28060-28062):
        // Đề nghị có MemberNo không tồn tại trong Crd_Member sẽ bị loại bỏ.
        // Lọc RequestStatus == "PENDING" (Gate :169, 1020) và CreateDTimeUTC >= strFromDTimeUTC (Gate :170, 995).
        // Gate không nhận lọc ngoài từ client (Gate :147, 167-171).
        var query = from cmci in _db.Set<Crd_MemberChangeInfo>()
                    join cm in _db.CrdMembers on cmci.MemberNo equals cm.MemberNo
                    where cmci.RequestStatus != null
                          && cmci.RequestStatus.ToUpper() == "PENDING"
                          && cmci.CreateDTimeUTC != null
                          && string.Compare(cmci.CreateDTimeUTC, strFromDTimeUTC) >= 0
                    select cmci.RequestNo;

        // 3. Đếm số lượng: nCount = objRT_Crd_MemberChangeInfo.Lst_Crd_MemberChangeInfo.Count (Gate :188-192)
        var nCount = await query.CountAsync();

        // 4. Kết quả trả về JSON: objJsonResultEntry.Data = new { Count = nCount } (Gate :194-198)
        return Ok(new
        {
            Success = true,
            Data = new
            {
                Count = nCount
            }
        });
    }

    /// <summary>
    /// Lấy chi tiết đề nghị thay đổi thông tin hội viên theo RequestNo (Action GetByRequestNo, #029).
    /// Gate: D:\idocNet\2022.7.Loyalty\13.ClientGate\V10\idn.Loyalty.WebAPI\Controllers\Crd_MemberChangeInfoController.cs:213-304
    /// API: D:\idocNet\2022.7.Loyalty\11.BackEnd\V10\idn.Loyalty.Biz.Web\Controllers\CrdMemberChangeInfoController.cs:23-93
    /// Biz: D:\idocNet\2022.7.Loyalty\11.BackEnd\V10\idn.Loyalty.Biz\Card.cs:28443-28557 (WAS_), 28282-28441 (Get), 27978-28280 (GetX)
    /// </summary>
    [HttpPost("getbyrequestno")]
    public async Task<IActionResult> GetByRequestNo([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Crd_MemberChangeInfoGetByRequestNoReq? req)
    {
        // 1. Guard: if (RequestNo != null && !TUtils.CUtils.IsNullOrEmpty(RequestNo)) (Gate :238)
        // Nếu null hoặc rỗng -> không gọi backend, trả ngay Data = { Crd_MemberChangeInfo = null, Lst_Crd_MemberChangeInfoDtl = null } (Gate :286-291)
        if (string.IsNullOrWhiteSpace(req?.RequestNo))
        {
            return Ok(new
            {
                Success = true,
                Data = new
                {
                    Crd_MemberChangeInfo = (object?)null,
                    Lst_Crd_MemberChangeInfoDtl = (object?)null
                }
            });
        }

        var requestNo = req.RequestNo.Trim();
        var p_RequestNo = PortQuery.Equal(requestNo);

        // 2. Truy vấn Crd_MemberChangeInfo kết hợp Crd_Member (Card.cs:28051-28066)
        // Bảng tạm #tbl_Crd_MemberChangeInfo_Filter_Draft thực hiện INNER JOIN Crd_Member trên MemberNo (Card.cs:28060-28062):
        // Nếu MemberNo không tồn tại trong Crd_Member thì loại bỏ khỏi kết quả.
        // Hàm lọc strWhereClause_Crd_MemberChangeInfo: chỉ lọc RequestNo với toán tử '=' (Gate :989-992). KHÔNG lọc OrgID (Gate :974-1025).
        var headerQuery = from cmci in _db.Set<Crd_MemberChangeInfo>()
                          join cm in _db.CrdMembers on cmci.MemberNo equals cm.MemberNo
                          where EF.Functions.Like((cmci.RequestNo ?? "").ToLower(), p_RequestNo, PortQuery.Esc)
                          select new { cmci, cm };

        var header = await headerQuery.FirstOrDefaultAsync();
        if (header == null)
        {
            // Không tìm thấy hoặc bị loại bỏ bởi INNER JOIN Crd_Member -> Data = { null, null } (Gate :286-291)
            return Ok(new
            {
                Success = true,
                Data = new
                {
                    Crd_MemberChangeInfo = (object?)null,
                    Lst_Crd_MemberChangeInfoDtl = (object?)null
                }
            });
        }

        var cmciRow = header.cmci;
        var cmRow = header.cm;

        // 3. Tính toán các trường của Crd_MemberChangeInfo (Card.cs:28100-28164)
        // Đơn vị duyệt: prefix 'HTV,' (Card.cs:28138)
        var approveDLCode = "HTV," + (cmciRow.ApproveDLCode ?? "");

        // FlagApproveAF: user hiện tại (mặc định 'HTV') được duyệt khi RequestStatus in ('PENDING','APPROVE') (Card.cs:28145-28151)
        var isPendingOrApprove = (cmciRow.RequestStatus == "PENDING" || cmciRow.RequestStatus == "APPROVE");
        var flagApproveAF = isPendingOrApprove ? "1" : "0";

        var objCrd_MemberChangeInfo = new Crd_MemberChangeInfo
        {
            RequestNo = cmciRow.RequestNo,
            NetworkID = cmciRow.NetworkID,
            OrgID = cmciRow.OrgID,
            MemberNo = cmciRow.MemberNo,
            DLCodeRequest = cmciRow.DLCodeRequest, // strFlagIsLookupByMember = "0" -> giữ nguyên (Card.cs:28115, 28500)
            RequestType = cmciRow.RequestType,
            RequestStatus = cmciRow.RequestStatus,
            Remark = cmciRow.Remark,
            RemarkHTV = cmciRow.RemarkHTV,
            AttachFilePath = cmciRow.AttachFilePath,
            AttachFileName = cmciRow.AttachFileName,
            AttachFileSize = cmciRow.AttachFileSize,
            AttachFileBase64 = cmciRow.AttachFileBase64,
            CreateDTimeUTC = cmciRow.CreateDTimeUTC, // Giữ nguyên UTC (Gate :274 chỉ đổi LUDTimeUTC)
            CreateBy = cmciRow.CreateBy,
            LUDTimeUTC = cmciRow.LUDTimeUTC ?? cmciRow.LogLUDTimeUTC,
            LUBy = cmciRow.LUBy,
            ApproveDTimeUTC = cmciRow.ApproveDTimeUTC,
            ApproveBy = cmciRow.ApproveBy,
            FinishDTimeUTC = cmciRow.FinishDTimeUTC,
            FinishBy = cmciRow.FinishBy,
            RejectDTimeUTC = cmciRow.RejectDTimeUTC,
            RejectBy = cmciRow.RejectBy,
            LogLUDTimeUTC = cmciRow.LogLUDTimeUTC,
            LogLUBy = cmciRow.LogLUBy,
            FlagFileUpload = cmciRow.FlagFileUpload,
            AttFileId = cmciRow.AttFileId,

            // Các cột bổ sung từ Crd_Member (Card.cs:28139-28144)
            cm_MemberName = cmRow.MemberName,
            cm_CarNo = cmRow.CarNo,
            cm_PhoneNo = cmRow.PhoneNo,
            cm_VIN = cmRow.VIN,
            cm_AmountEnd = cmRow.AmountEnd,
            cm_CardTypeUse = cmRow.CardTypeUse,

            ApproveDLCode = approveDLCode,
            FlagApproveAF = flagApproveAF
        };

        // 4. Xử lý sau khi nhận kết quả (Gate :271-279):
        // CHỈ chuyển đổi LUDTimeUTC sang giờ Local (+7 tiếng, format "yyyy-MM-dd HH:mm:ss") (Gate :274)
        // Các trường thời gian khác (CreateDTimeUTC, FinishDTimeUTC...) giữ nguyên UTC.
        objCrd_MemberChangeInfo.LUDTimeUTC = ConvertUtcToLocal(objCrd_MemberChangeInfo.LUDTimeUTC);

        // AttachFilePath: nếu có thì format api/file/{filepath.Replace("\\", "/")}, nếu rỗng/null thì giữ null (Gate :276-277)
        var filepath = objCrd_MemberChangeInfo.AttachFilePath?.Trim();
        if (!string.IsNullOrEmpty(filepath))
        {
            objCrd_MemberChangeInfo.AttachFilePath = $"api/file/{filepath.Replace("\\", "/")}";
        }
        else
        {
            objCrd_MemberChangeInfo.AttachFilePath = null;
        }

        // 5. Truy vấn danh sách chi tiết Crd_MemberChangeInfoDtl (Card.cs:28171-28208)
        var dtlRawList = await _db.Set<Crd_MemberChangeInfoDtl>()
            .Where(x => EF.Functions.Like((x.RequestNo ?? "").ToLower(), p_RequestNo, PortQuery.Esc))
            .ToListAsync();

        List<Crd_MemberChangeInfoDtl>? listCrd_MemberChangeInfoDtl = null;
        if (dtlRawList.Count > 0)
        {
            var provinces = await _db.Set<Mst_Province>().ToListAsync();
            var districts = await _db.Set<Mst_District>().ToListAsync();
            var carModels = await _db.Set<Mst_CarModel>().ToListAsync();
            var carBrands = await _db.Set<Mst_CarBrand>().ToListAsync();

            listCrd_MemberChangeInfoDtl = new List<Crd_MemberChangeInfoDtl>();
            foreach (var dtl in dtlRawList)
            {
                var itemDtl = new Crd_MemberChangeInfoDtl
                {
                    RequestNo = dtl.RequestNo,
                    NetworkID = dtl.NetworkID,
                    OrgID = dtl.OrgID,
                    ColumnCode = dtl.ColumnCode,
                    ColumnValueOld = dtl.ColumnValueOld,
                    ColumnValueNew = dtl.ColumnValueNew,
                    Remark = dtl.Remark,
                    RequestDtlStatus = dtl.RequestDtlStatus,
                    LogLUDTimeUTC = dtl.LogLUDTimeUTC,
                    LogLUBy = dtl.LogLUBy,

                    // Join bảng danh mục để lấy tên (Card.cs:28176-28203):
                    ProvinceNameOld = provinces.FirstOrDefault(p => string.Equals(p.ProvinceCode, dtl.ColumnValueOld, StringComparison.OrdinalIgnoreCase))?.ProvinceName,
                    ProvinceNameNew = provinces.FirstOrDefault(p => string.Equals(p.ProvinceCode, dtl.ColumnValueNew, StringComparison.OrdinalIgnoreCase))?.ProvinceName,
                    DistrictNameOld = districts.FirstOrDefault(d => string.Equals(d.DistrictCode, dtl.ColumnValueOld, StringComparison.OrdinalIgnoreCase))?.DistrictName,
                    DistrictNameNew = districts.FirstOrDefault(d => string.Equals(d.DistrictCode, dtl.ColumnValueNew, StringComparison.OrdinalIgnoreCase))?.DistrictName,
                    ModelNameNew = carModels.FirstOrDefault(m => string.Equals(m.ModelCode, dtl.ColumnValueNew, StringComparison.OrdinalIgnoreCase))?.ModelName,
                    // LỖI NGUỒN GIỮ NGUYÊN: dev nguồn join mcmo trên ColumnValueNew tại Card.cs:28199
                    ModelNameOld = carModels.FirstOrDefault(m => string.Equals(m.ModelCode, dtl.ColumnValueNew, StringComparison.OrdinalIgnoreCase))?.ModelName,
                    // LỖI NGUỒN GIỮ NGUYÊN: dev nguồn join mcbo trên ColumnValueNew tại Card.cs:28201
                    BrandNameOld = carBrands.FirstOrDefault(b => string.Equals(b.BrandCode, dtl.ColumnValueNew, StringComparison.OrdinalIgnoreCase))?.BrandName,
                    BrandNameNew = carBrands.FirstOrDefault(b => string.Equals(b.BrandCode, dtl.ColumnValueNew, StringComparison.OrdinalIgnoreCase))?.BrandName
                };

                listCrd_MemberChangeInfoDtl.Add(itemDtl);
            }
        }

        // 6. Kết quả trả về JSON (Gate :286-291)
        // Nếu không có phần tử chi tiết nào -> giữ null (Gate :280-284)
        return Ok(new
        {
            Success = true,
            Data = new
            {
                Crd_MemberChangeInfo = objCrd_MemberChangeInfo,
                Lst_Crd_MemberChangeInfoDtl = listCrd_MemberChangeInfoDtl
            }
        });
    }

    /// <summary>
    /// Tra cứu lịch sử đề nghị sửa thông tin hội viên (Action GetHistoryByMember, #030).
    /// Gate: D:\idocNet\2022.7.Loyalty\13.ClientGate\V10\idn.Loyalty.WebAPI\Controllers\Crd_MemberChangeInfoController.cs:306-445
    /// API: D:\idocNet\2022.7.Loyalty\11.BackEnd\V10\idn.Loyalty.Biz.Web\Controllers\CrdMemberChangeInfoController.cs:98-166 (WA_Crd_MemberChangeInfo_GetForLookup)
    /// Biz: D:\idocNet\2022.7.Loyalty\11.BackEnd\V10\idn.Loyalty.Biz\Card.cs:28565-28665 (WAS_), 28282-28441 (Get), 27978-28280 (GetX)
    /// </summary>
    [HttpPost("gethistorybymember")]
    public async Task<IActionResult> GetHistoryByMember([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Crd_MemberChangeInfoGetHistoryByMemberReq? req)
    {
        req ??= new Crd_MemberChangeInfoGetHistoryByMemberReq();

        // 1. Chuẩn hoá ngày duyệt sửa từ giờ VN sang UTC (Gate :329-339):
        // FinishDTimeFrom: StdDTimeUTC(StdDTimeBeginDay(FinishDTimeFrom)) -> 00:00:00 VN trừ 7 tiếng
        // FinishDTimeTo: StdDTimeUTC(StdDTimeEndDay(FinishDTimeTo)) -> 23:59:59 VN trừ 7 tiếng
        string? finishFromUtc = null;
        if (!string.IsNullOrWhiteSpace(req.FinishDTimeFrom))
        {
            if (DateTime.TryParse(req.FinishDTimeFrom.Trim(), out var dtFrom))
            {
                finishFromUtc = dtFrom.Date.AddHours(-7).ToString("yyyy-MM-dd HH:mm:ss");
            }
        }

        string? finishToUtc = null;
        if (!string.IsNullOrWhiteSpace(req.FinishDTimeTo))
        {
            if (DateTime.TryParse(req.FinishDTimeTo.Trim(), out var dtTo))
            {
                finishToUtc = dtTo.Date.AddDays(1).AddSeconds(-1).AddHours(-7).ToString("yyyy-MM-dd HH:mm:ss");
            }
        }

        // 2. Truy vấn Crd_MemberChangeInfo kết hợp Crd_Member (Card.cs:28051-28066)
        // Bảng tạm #tbl_Crd_MemberChangeInfo_Filter_Draft thực hiện INNER JOIN Crd_Member trên MemberNo (Card.cs:28060-28062):
        // Loại bỏ đề nghị có MemberNo không tồn tại trong Crd_Member.
        // Lọc cố định: RequestStatus = 'FINISH' (Gate :343, :1020) và RequestType = 'CHANGEINFO' (Gate :348).
        // KHÔNG lọc OrgID (Gate :974-1025); KHÔNG có KeyWord.
        var query = from cmci in _db.Set<Crd_MemberChangeInfo>()
                    join cm in _db.CrdMembers on cmci.MemberNo equals cm.MemberNo
                    where cmci.RequestStatus == "FINISH"
                          && cmci.RequestType == "CHANGEINFO"
                    select new { cmci, cm };

        // Lọc MemberNo với toán tử = nếu có truyền (Gate :342, :985-988)
        if (!string.IsNullOrWhiteSpace(req.MemberNo))
        {
            var p_MemberNo = PortQuery.Equal(req.MemberNo.Trim());
            query = query.Where(x => EF.Functions.Like((x.cmci.MemberNo ?? "").ToLower(), p_MemberNo, PortQuery.Esc));
        }

        // Lọc FinishDTimeUTC >= finishFromUtc (Gate :344, :1001-1004)
        if (!string.IsNullOrEmpty(finishFromUtc))
        {
            query = query.Where(x => x.cmci.FinishDTimeUTC != null && string.Compare(x.cmci.FinishDTimeUTC, finishFromUtc) >= 0);
        }

        // Lọc FinishDTimeUTC <= finishToUtc (Gate :345, :1005-1008)
        if (!string.IsNullOrEmpty(finishToUtc))
        {
            query = query.Where(x => x.cmci.FinishDTimeUTC != null && string.Compare(x.cmci.FinishDTimeUTC, finishToUtc) <= 0);
        }

        // 3. Sắp xếp thứ tự: cmci.LogLUDTimeUTC asc (Card.cs:28065)
        query = query.OrderBy(x => x.cmci.LogLUDTimeUTC);

        var rawList = await query.ToListAsync();

        // Nếu không tìm thấy bản ghi nào, trả Data với 2 mảng null (Gate :326-327, :428-432)
        if (rawList.Count == 0)
        {
            return Ok(new
            {
                Success = true,
                Data = new
                {
                    Lst_Crd_MemberChangeInfo = (object?)null,
                    Lst_Crd_MemberChangeInfoDtl = (object?)null
                }
            });
        }

        // 4. Ánh xạ các cột hiển thị của Crd_MemberChangeInfo (Card.cs:28100-28164)
        var userDLCPCode = !string.IsNullOrWhiteSpace(req.DLCPCode) ? req.DLCPCode.Trim().ToUpperInvariant() : "HTV";

        var resultList = new List<Crd_MemberChangeInfo>();
        foreach (var row in rawList)
        {
            var cmci = row.cmci;
            var cm = row.cm;

            // Cột DLCodeRequest: vì @strFlagIsLookupByMember = '1', nếu @strDLCPCode <> 'HTV' và cmci.DLCodeRequest <> @strDLCPCode thì 'N/A' (Card.cs:28109-28117)
            var dlCodeRequest = cmci.DLCodeRequest;
            if (userDLCPCode != "HTV" && !string.Equals(cmci.DLCodeRequest, userDLCPCode, StringComparison.OrdinalIgnoreCase))
            {
                dlCodeRequest = "N/A";
            }

            // Đơn vị duyệt: prefix 'HTV,' (Card.cs:28138)
            var approveDLCode = "HTV," + (cmci.ApproveDLCode ?? "");

            // FlagApproveAF: vì RequestStatus = 'FINISH' (không thuộc PENDING, APPROVE) nên luôn trả '0' (Card.cs:28145-28151)
            var flagApproveAF = "0";

            var item = new Crd_MemberChangeInfo
            {
                RequestNo = cmci.RequestNo,
                NetworkID = cmci.NetworkID,
                OrgID = cmci.OrgID,
                MemberNo = cmci.MemberNo,
                DLCodeRequest = dlCodeRequest,
                RequestType = cmci.RequestType,
                RequestStatus = cmci.RequestStatus,
                Remark = cmci.Remark,
                RemarkHTV = cmci.RemarkHTV,
                AttachFilePath = cmci.AttachFilePath,
                AttachFileName = cmci.AttachFileName,
                AttachFileSize = cmci.AttachFileSize,
                AttachFileBase64 = cmci.AttachFileBase64,
                CreateDTimeUTC = cmci.CreateDTimeUTC, // Cột CreateDTimeUTC giữ nguyên UTC (Gate :368-385)
                CreateBy = cmci.CreateBy,
                LUDTimeUTC = ConvertUtcToLocal(cmci.LUDTimeUTC ?? cmci.LogLUDTimeUTC), // Đổi sang Local (+7h, Gate :375)
                LUBy = cmci.LUBy,
                ApproveDTimeUTC = ConvertUtcToLocal(cmci.ApproveDTimeUTC), // Đổi sang Local (+7h, Gate :379)
                ApproveBy = cmci.ApproveBy,
                FinishDTimeUTC = ConvertUtcToLocal(cmci.FinishDTimeUTC), // Đổi sang Local (+7h, Gate :383)
                FinishBy = cmci.FinishBy,
                RejectDTimeUTC = cmci.RejectDTimeUTC,
                RejectBy = cmci.RejectBy,
                LogLUDTimeUTC = cmci.LogLUDTimeUTC,
                LogLUBy = cmci.LogLUBy,
                FlagFileUpload = cmci.FlagFileUpload,
                AttFileId = cmci.AttFileId,

                // Các cột mở rộng từ bảng Crd_Member (Card.cs:28139-28144)
                cm_MemberName = cm.MemberName,
                cm_CarNo = cm.CarNo,
                cm_PhoneNo = cm.PhoneNo,
                cm_VIN = cm.VIN,
                cm_AmountEnd = cm.AmountEnd,
                cm_CardTypeUse = cm.CardTypeUse,

                ApproveDLCode = approveDLCode,
                FlagApproveAF = flagApproveAF
            };

            resultList.Add(item);
        }

        // 5. Truy vấn danh sách chi tiết Crd_MemberChangeInfoDtl (Card.cs:28171-28208)
        var requestNos = resultList.Select(x => x.RequestNo).ToList();
        var dtlRawList = await _db.Set<Crd_MemberChangeInfoDtl>()
            .Where(x => requestNos.Contains(x.RequestNo))
            .ToListAsync();

        List<Crd_MemberChangeInfoDtl>? listDtl = null;
        if (dtlRawList.Count > 0)
        {
            var provinces = await _db.Set<Mst_Province>().ToListAsync();
            var districts = await _db.Set<Mst_District>().ToListAsync();
            var carModels = await _db.Set<Mst_CarModel>().ToListAsync();
            var carBrands = await _db.Set<Mst_CarBrand>().ToListAsync();

            // Sắp xếp dtl theo thứ tự của request cha trong resultList (Card.cs:28204: order by t.MyIdxSeq asc)
            var requestOrder = resultList.Select((item, idx) => new { item.RequestNo, idx })
                                         .ToDictionary(x => x.RequestNo, x => x.idx);

            var orderedDtl = dtlRawList.OrderBy(d => requestOrder.TryGetValue(d.RequestNo, out var idx) ? idx : int.MaxValue)
                                       .ThenBy(d => d.ColumnCode);

            // Bổ sung thông tin từ đề nghị cha vào từng dòng Dtl (Gate :392-425)
            var dicHeader = resultList.ToDictionary(h => h.RequestNo, h => h);

            listDtl = new List<Crd_MemberChangeInfoDtl>();
            foreach (var dtl in orderedDtl)
            {
                var itemDtl = new Crd_MemberChangeInfoDtl
                {
                    RequestNo = dtl.RequestNo,
                    NetworkID = dtl.NetworkID,
                    OrgID = dtl.OrgID,
                    ColumnCode = dtl.ColumnCode,
                    ColumnValueOld = dtl.ColumnValueOld,
                    ColumnValueNew = dtl.ColumnValueNew,
                    Remark = dtl.Remark,
                    RequestDtlStatus = dtl.RequestDtlStatus,
                    LogLUDTimeUTC = dtl.LogLUDTimeUTC,
                    LogLUBy = dtl.LogLUBy,

                    // Join bảng danh mục để lấy tên (Card.cs:28176-28203):
                    ProvinceNameOld = provinces.FirstOrDefault(p => string.Equals(p.ProvinceCode, dtl.ColumnValueOld, StringComparison.OrdinalIgnoreCase))?.ProvinceName,
                    ProvinceNameNew = provinces.FirstOrDefault(p => string.Equals(p.ProvinceCode, dtl.ColumnValueNew, StringComparison.OrdinalIgnoreCase))?.ProvinceName,
                    DistrictNameOld = districts.FirstOrDefault(d => string.Equals(d.DistrictCode, dtl.ColumnValueOld, StringComparison.OrdinalIgnoreCase))?.DistrictName,
                    DistrictNameNew = districts.FirstOrDefault(d => string.Equals(d.DistrictCode, dtl.ColumnValueNew, StringComparison.OrdinalIgnoreCase))?.DistrictName,
                    ModelNameNew = carModels.FirstOrDefault(m => string.Equals(m.ModelCode, dtl.ColumnValueNew, StringComparison.OrdinalIgnoreCase))?.ModelName,
                    // LỖI NGUỒN GIỮ NGUYÊN: dev nguồn join mcmo trên ColumnValueNew tại Card.cs:28199
                    ModelNameOld = carModels.FirstOrDefault(m => string.Equals(m.ModelCode, dtl.ColumnValueNew, StringComparison.OrdinalIgnoreCase))?.ModelName,
                    // LỖI NGUỒN GIỮ NGUYÊN: dev nguồn join mcbo trên ColumnValueNew tại Card.cs:28201
                    BrandNameOld = carBrands.FirstOrDefault(b => string.Equals(b.BrandCode, dtl.ColumnValueNew, StringComparison.OrdinalIgnoreCase))?.BrandName,
                    BrandNameNew = carBrands.FirstOrDefault(b => string.Equals(b.BrandCode, dtl.ColumnValueNew, StringComparison.OrdinalIgnoreCase))?.BrandName
                };

                // Bổ sung 22 trường từ Crd_MemberChangeInfo cha vào từng dòng Dtl (Gate :402-423)
                if (dicHeader.TryGetValue(dtl.RequestNo, out var cmci))
                {
                    itemDtl.MemberNo = cmci.MemberNo;
                    itemDtl.DLCodeRequest = cmci.DLCodeRequest; // Đã qua che 'N/A' nếu user khác đại lý đề nghị
                    itemDtl.RequestType = cmci.RequestType;
                    itemDtl.RequestStatus = cmci.RequestStatus;
                    itemDtl.CreateDTimeUTC = cmci.CreateDTimeUTC; // Giữ nguyên UTC
                    itemDtl.CreateBy = cmci.CreateBy;
                    itemDtl.LUDTimeUTC = cmci.LUDTimeUTC; // Giờ local (+7h)
                    itemDtl.LUBy = cmci.LUBy;
                    itemDtl.ApproveDTimeUTC = cmci.ApproveDTimeUTC; // Giờ local (+7h)
                    itemDtl.ApproveBy = cmci.ApproveBy;
                    itemDtl.FinishDTimeUTC = cmci.FinishDTimeUTC; // Giờ local (+7h)
                    itemDtl.FinishBy = cmci.FinishBy;
                    itemDtl.RejectDTimeUTC = cmci.RejectDTimeUTC;
                    itemDtl.RejectBy = cmci.RejectBy;
                    itemDtl.ApproveDLCode = cmci.ApproveDLCode; // Có prefix 'HTV,'
                    itemDtl.FlagApproveAF = cmci.FlagApproveAF; // '0'
                    itemDtl.cm_MemberName = cmci.cm_MemberName;
                    itemDtl.cm_CarNo = cmci.cm_CarNo;
                    itemDtl.cm_PhoneNo = cmci.cm_PhoneNo;
                    itemDtl.cm_VIN = cmci.cm_VIN;
                    itemDtl.cm_AmountEnd = cmci.cm_AmountEnd;
                    itemDtl.cm_CardTypeUse = cmci.cm_CardTypeUse;
                }

                listDtl.Add(itemDtl);
            }
        }

        // 6. Kết quả trả về (Gate :427-432)
        return Ok(new
        {
            Success = true,
            Data = new
            {
                Lst_Crd_MemberChangeInfo = resultList,
                Lst_Crd_MemberChangeInfoDtl = (listDtl != null && listDtl.Count > 0) ? listDtl : null
            }
        });
    }

    /// <summary>
    /// Chuyển đổi chuỗi thời gian UTC sang Local Time (+7 giờ VN) định dạng yyyy-MM-dd HH:mm:ss (Gate :105-125).
    /// </summary>
    private static string? ConvertUtcToLocal(string? utcStr)
    {
        if (string.IsNullOrWhiteSpace(utcStr)) return utcStr;
        if (DateTime.TryParse(utcStr, out var dt))
        {
            return dt.AddHours(7).ToString("yyyy-MM-dd HH:mm:ss");
        }
        return utcStr;
    }

    /// <summary>
    /// Lấy danh sách cấu hình cột thay đổi thông tin hội viên (Action GetMemberColumnChange, #031).
    /// Gate: D:\idocNet\2022.7.Loyalty\13.ClientGate\V10\idn.Loyalty.WebAPI\Controllers\Crd_MemberChangeInfoController.cs:447-512
    /// API: D:\idocNet\2022.7.Loyalty\11.BackEnd\V10\idn.Loyalty.Biz.Web\Controllers\CrdMemberColumnChangeController.cs:23-93
    /// Biz: D:\idocNet\2022.7.Loyalty\11.BackEnd\V10\idn.Loyalty.Biz\Card.cs:31964-32066 (WAS_), 31809-31960 (Get), 31648-31807 (GetX)
    /// </summary>
    [HttpPost("getmembercolumnchange")]
    public async Task<IActionResult> GetMemberColumnChange([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Crd_MemberColumnChangeGetReq? req)
    {
        req ??= new Crd_MemberColumnChangeGetReq();

        // 1. Truy vấn cấu hình cột Crd_MemberColumnChange (Card.cs:31703-31713, 31745-31753)
        var query = _db.Set<Crd_MemberColumnChange>().AsQueryable();

        // Lọc OrgID tuỳ chọn nếu client truyền theo bài học A.14 (Card.cs:31751)
        if (!string.IsNullOrWhiteSpace(req.OrgID))
        {
            var p_OrgID = PortQuery.Equal(req.OrgID.Trim());
            query = query.Where(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), p_OrgID, PortQuery.Esc));
        }

        // Sắp xếp thứ tự: LogLUDTimeUTC asc (Card.cs:31712, 31752)
        query = query.OrderBy(x => x.LogLUDTimeUTC);

        var list = await query.ToListAsync();

        // 2. Kết quả trả về JSON (Gate :489-499)
        // Nếu không có phần tử nào -> trả về null (Gate :468, 489)
        return Ok(new
        {
            Success = true,
            Data = new
            {
                Lst_Crd_MemberColumnChange = (list != null && list.Count > 0) ? list : null
            }
        });
    }

    private static string? StdParam(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim().ToUpperInvariant();
    private static string? StdStr(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim().Normalize();

    /// <summary>
    /// Lưu / Sửa / Xóa đề nghị thay đổi thông tin hội viên (Action Save, #032).
    /// Gate: D:\idocNet\2022.7.Loyalty\13.ClientGate\V10\idn.Loyalty.WebAPI\Controllers\Crd_MemberChangeInfoController.cs:516-578
    /// API: D:\idocNet\2022.7.Loyalty\11.BackEnd\V10\idn.Loyalty.Biz.Web\Controllers\CrdMemberChangeInfoController.cs:169-239
    /// Biz: D:\idocNet\2022.7.Loyalty\11.BackEnd\V10\idn.Loyalty.Biz\Card.cs:29560-29659 (WAS_), 29399-29558 (Save), 28667-29397 (SaveX)
    /// </summary>
    [HttpPost("save")]
    public async Task<IActionResult> Save([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Crd_MemberChangeInfoSaveReq? req)
    {
        // 1. Kiểm tra rỗng / null ở tầng Gate (Gate :525-532, Ca 1)
        if (req == null || req.Crd_MemberChangeInfo == null)
        {
            return PortResult.Fail("Crd_MemberChangeInfo_Save", "Check.strJson", null);
        }

        var headerInput = req.Crd_MemberChangeInfo;

        // 2. Chuẩn hoá tham số đầu vào (Card.cs:28726-28737)
        var bIsDelete = false;

        var strRequestNo = StdParam(headerInput.RequestNo);
        var strMemberNo = StdParam(headerInput.MemberNo);
        var strDLCodeRequest = StdParam(headerInput.DLCodeRequest);
        var strRequestType = StdParam(headerInput.RequestType);
        var strRemark = StdStr(headerInput.Remark);
        var strRemarkHTV = StdStr(headerInput.RemarkHTV);
        var strAttachFilePath = StdStr(headerInput.AttachFilePath);
        var strAttachFileName = StdStr(headerInput.AttachFileName);
        var strAttachFileBase64 = StdStr(headerInput.AttachFileBase64);

        double dbAttachFileSize = 0;
        if (!string.IsNullOrWhiteSpace(headerInput.AttachFileSize))
        {
            if (double.TryParse(headerInput.AttachFileSize, out var fs))
            {
                dbAttachFileSize = fs;
            }
        }

        List<Crd_MemberChangeInfoDtl>? reqDtl = null;
        var dtimeSys = DateTime.UtcNow;
        var strOrgID = headerInput.OrgID ?? "1001";
        var strNetworkID = _config?["Biz_NetworkID"] ?? "1001";
        var strWAUserCode = Request.Headers.TryGetValue("WAUserCode", out var hdrUser) && !string.IsNullOrWhiteSpace(hdrUser)
            ? hdrUser.ToString().Trim()
            : "port";

        // 3. Khởi tạo transaction an toàn (Card.cs:29442)
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // 4. Kiểm tra bản ghi tồn tại qua Crd_MemberChangeInfo_CheckDB (Card.cs:28747-28786)
            var p_RequestNo = PortQuery.Equal(strRequestNo ?? "");
            var existingRecord = await _db.Set<Crd_MemberChangeInfo>()
                .FirstOrDefaultAsync(x => EF.Functions.Like((x.RequestNo ?? "").ToLower(), p_RequestNo, PortQuery.Esc));

            string? strCreateDTimeUTC = null;
            string? strCreateBy = null;

            if (existingRecord == null)
            {
                // Chưa tồn tại trong DB
                if (bIsDelete)
                {
                    // Xóa bản ghi không tồn tại -> thành công ngay (goto MyCodeLabel_Done, Ca 11, Card.cs:28757-28760)
                    await tx.CommitAsync();
                    return Ok(new { Success = true, Data = (object?)null });
                }
            }
            else
            {
                // Đã tồn tại trong DB: chỉ cho phép sửa/xóa khi RequestStatus là PENDING hoặc CANCEL (Card.cs:28768-28779, Ca 2)
                var currentStatus = existingRecord.RequestStatus ?? "";
                if (!string.Equals(currentStatus, "PENDING", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(currentStatus, "CANCEL", StringComparison.OrdinalIgnoreCase))
                {
                    await tx.RollbackAsync();
                    return PortResult.Fail("Crd_MemberChangeInfo_SaveX_InvalidRequestStatus",
                        "Check.DB.RequestStatus", currentStatus,
                        "Check.RequestStatus.Expected", "PENDING");
                }

                // Giữ nguyên CreateDTimeUTC và CreateBy của bản ghi cũ từ DB (Card.cs:28783-28784)
                strCreateDTimeUTC = existingRecord.CreateDTimeUTC;
                strCreateBy = existingRecord.CreateBy;
            }

            strCreateDTimeUTC = string.IsNullOrEmpty(strCreateDTimeUTC) ? dtimeSys.ToString("yyyy-MM-dd HH:mm:ss") : strCreateDTimeUTC;
            strCreateBy = string.IsNullOrEmpty(strCreateBy) ? strWAUserCode : strCreateBy;

            // 5. Kiểm tra guard nghiệp vụ khi !bIsDelete (Card.cs:28793-28872)
            string strApproveDLCode = "";

            if (!bIsDelete)
            {
                // Guard MemberNo rỗng (Card.cs:28796-28805)
                if (string.IsNullOrEmpty(strMemberNo))
                {
                    await tx.RollbackAsync();
                    return PortResult.Fail("Crd_MemberChangeInfo_SaveX_InvalidMemberNo", "Check.MemberNo", strMemberNo);
                }

                // Crd_Member_CheckDB: kiểm tra hội viên tồn tại và MemberStatus = 'APPROVE' (Card.cs:28810-28817, Ca 3)
                var p_MemberNo = PortQuery.Equal(strMemberNo);
                var member = await _db.CrdMembers
                    .FirstOrDefaultAsync(x => EF.Functions.Like((x.MemberNo ?? "").ToLower(), p_MemberNo, PortQuery.Esc));

                if (member == null)
                {
                    await tx.RollbackAsync();
                    return PortResult.Fail("Crd_Member_CheckDB_MemberNotFound", "Check.MemberNo", strMemberNo);
                }

                if (!string.Equals(member.MemberStatus, "APPROVE", StringComparison.OrdinalIgnoreCase))
                {
                    await tx.RollbackAsync();
                    return PortResult.Fail("Crd_Member_CheckDB_MemberStatusNotMatched",
                        "Check.MemberNo", strMemberNo,
                        "Check.strMemberStatusListToCheck", "APPROVE",
                        "DB.MemberStatus", member.MemberStatus);
                }

                // Guard DLCodeRequest rỗng (Card.cs:28819-28828)
                if (string.IsNullOrEmpty(strDLCodeRequest))
                {
                    await tx.RollbackAsync();
                    return PortResult.Fail("Crd_MemberChangeInfo_SaveX_InvalidDLCodeRequest", "Check.DLCodeRequest", strDLCodeRequest);
                }

                // Mst_DealerCP_CheckDB: kiểm tra đại lý tồn tại và FlagActive = '1' (Card.cs:28833-28839, Ca 4)
                var p_DLCodeRequest = PortQuery.Equal(strDLCodeRequest);
                var dealer = await _db.Set<Mst_DealerCP>()
                    .FirstOrDefaultAsync(x => EF.Functions.Like((x.DLCPCode ?? "").ToLower(), p_DLCodeRequest, PortQuery.Esc));

                if (dealer == null)
                {
                    await tx.RollbackAsync();
                    return PortResult.Fail("Mst_DealerCP_CheckDB_DLNotFound", "Check.DLCPCode", strDLCodeRequest);
                }

                if (dealer.FlagActive != "1")
                {
                    await tx.RollbackAsync();
                    return PortResult.Fail("Mst_DealerCP_CheckDB_FlagActiveNotMatched",
                        "Check.DLCPCode", strDLCodeRequest,
                        "Check.strFlagActiveListToCheck", "1",
                        "DB.FlagActive", dealer.FlagActive);
                }

                // Guard RequestType: phải thuộc CHANGEINFO, CANCELMEMBER, CANCELMEMBERNOTQUERY (Card.cs:28841-28856, Ca 5)
                if (string.IsNullOrEmpty(strRequestType) ||
                    (!string.Equals(strRequestType, "CHANGEINFO", StringComparison.OrdinalIgnoreCase) &&
                     !string.Equals(strRequestType, "CANCELMEMBER", StringComparison.OrdinalIgnoreCase) &&
                     !string.Equals(strRequestType, "CANCELMEMBERNOTQUERY", StringComparison.OrdinalIgnoreCase)))
                {
                    await tx.RollbackAsync();
                    return PortResult.Fail("Crd_MemberChangeInfo_SaveX_InvalidRequestType", "Check.RequestType", strRequestType);
                }

                // Nếu RequestType = CHANGEINFO: kiểm tra đại lý tra cứu hội viên Map_QueryDealer_Member_CheckDB (Card.cs:28858-28871)
                if (string.Equals(strRequestType, "CHANGEINFO", StringComparison.OrdinalIgnoreCase))
                {
                    var mapQuery = await _db.Set<Map_QueryDealer_Member>()
                        .FirstOrDefaultAsync(x => EF.Functions.Like((x.DLCPCode ?? "").ToLower(), p_DLCodeRequest, PortQuery.Esc) &&
                                                  EF.Functions.Like((x.MemberNo ?? "").ToLower(), p_MemberNo, PortQuery.Esc));

                    if (mapQuery == null)
                    {
                        await tx.RollbackAsync();
                        return PortResult.Fail("Map_QueryDealer_Member_CheckDB_DLNotFound",
                            "Check.DLCPCode", strDLCodeRequest,
                            "Check.MemberNo", strMemberNo);
                    }

                    if (mapQuery.FlagActive != "1")
                    {
                        await tx.RollbackAsync();
                        return PortResult.Fail("Map_QueryDealer_Member_CheckDB_FlagActiveNotMatched",
                            "Check.DLCPCode", strDLCodeRequest,
                            "Check.MemberNo", strMemberNo,
                            "Check.strFlagActiveListToCheck", "1",
                            "DB.FlagActive", mapQuery.FlagActive);
                    }
                }

                // 6. Khóa đơn vị duyệt ApproveDLCode (Card.cs:28892-28931)
                if (existingRecord == null)
                {
                    if (string.Equals(strRequestType, "CHANGEINFO", StringComparison.OrdinalIgnoreCase))
                    {
                        // Truy vấn Crd_CardTransaction lấy lượt xét hạng gần nhất của hội viên (Card.cs:28904-28925)
                        var lastTx = await _db.Set<CrdCardTransaction>()
                            .Where(cct => EF.Functions.Like((cct.MemberNo ?? "").ToLower(), p_MemberNo, PortQuery.Esc)
                                && (cct.DealPointType == "CONSUMPTION" || cct.DealPointType == "SERVICETURN")
                                && cct.DLCode != "HTV" && cct.DLCode != "SUPPORT")
                            .OrderByDescending(cct => cct.CreateDTimeUTC)
                            .FirstOrDefaultAsync();

                        if (lastTx != null && !string.IsNullOrEmpty(lastTx.DLCode))
                        {
                            strApproveDLCode = StdParam(lastTx.DLCode) ?? "";
                        }
                    }
                    else
                    {
                        // CANCELMEMBER / CANCELMEMBERNOTQUERY: để ApproveDLCode rỗng (Card.cs:28900-28901)
                        strApproveDLCode = "";
                    }
                }
                else
                {
                    // Bản ghi sửa: giữ nguyên ApproveDLCode cũ từ DB (Card.cs:28927-28930)
                    strApproveDLCode = StdParam(existingRecord.ApproveDLCode) ?? "";
                }

                // 7. Kiểm tra bảng chi tiết Crd_MemberChangeInfoDtl (Card.cs:28997-29070)
                var dtlsInput = reqDtl;
                if (string.Equals(strRequestType, "CHANGEINFO", StringComparison.OrdinalIgnoreCase))
                {
                    if (dtlsInput == null || dtlsInput.Count < 1)
                    {
                        await tx.RollbackAsync();
                        return PortResult.Fail("Crd_MemberChangeInfo_Save_Input_Crd_MemberChangeInfoDtlTblInvalid", "Check.TableName", "Crd_MemberChangeInfoDtl");
                    }
                }

                if (dtlsInput != null && dtlsInput.Count > 0)
                {
                    foreach (var dtl in dtlsInput)
                    {
                        var colCode = StdParam(dtl.ColumnCode);
                        if (string.IsNullOrEmpty(colCode))
                        {
                            await tx.RollbackAsync();
                            return PortResult.Fail("Crd_MemberColumnChange_CheckDB_Crd_MemberColumnChangeNotFound", "Check.ColumnCode", dtl.ColumnCode);
                        }

                        // Kiểm tra ColumnCode trong Crd_MemberColumnChange (Card.cs:29053-29060, Ca 7)
                        var p_ColCode = PortQuery.Equal(colCode);
                        var colExists = await _db.Set<Crd_MemberColumnChange>()
                            .AnyAsync(x => EF.Functions.Like((x.ColumnCode ?? "").ToLower(), p_ColCode, PortQuery.Esc));

                        if (!colExists)
                        {
                            await tx.RollbackAsync();
                            return PortResult.Fail("Crd_MemberColumnChange_CheckDB_Crd_MemberColumnChangeNotFound", "Check.ColumnCode", dtl.ColumnCode);
                        }
                    }
                }
            }

            // 8. Thực thi lưu dữ liệu SQL (Card.cs:29109-29249)
            // Xóa cũ: xóa sạch trong Crd_MemberChangeInfoDtl và Crd_MemberChangeInfo theo RequestNo (Card.cs:29112-29134)
            var oldDtls = await _db.Set<Crd_MemberChangeInfoDtl>()
                .Where(x => EF.Functions.Like((x.RequestNo ?? "").ToLower(), p_RequestNo, PortQuery.Esc))
                .ToListAsync();
            if (oldDtls.Count > 0)
            {
                _db.Set<Crd_MemberChangeInfoDtl>().RemoveRange(oldDtls);
            }

            var oldHeaders = await _db.Set<Crd_MemberChangeInfo>()
                .Where(x => EF.Functions.Like((x.RequestNo ?? "").ToLower(), p_RequestNo, PortQuery.Esc))
                .ToListAsync();
            if (oldHeaders.Count > 0)
            {
                _db.Set<Crd_MemberChangeInfo>().RemoveRange(oldHeaders);
            }
            await _db.SaveChangesAsync();

            // Thêm mới (nếu !bIsDelete) (Card.cs:29136-29249)
            var insertedDtls = new List<Crd_MemberChangeInfoDtl>();
            if (!bIsDelete)
            {
                var newHeader = new Crd_MemberChangeInfo
                {
                    RequestNo = strRequestNo ?? "",
                    NetworkID = strNetworkID,
                    OrgID = strOrgID,
                    MemberNo = strMemberNo,
                    DLCodeRequest = strDLCodeRequest,
                    ApproveDLCode = strApproveDLCode,
                    RequestType = strRequestType,
                    RequestStatus = "PENDING",
                    Remark = strRemark,
                    RemarkHTV = strRemarkHTV,
                    AttachFilePath = strAttachFilePath,
                    AttachFileName = strAttachFileName,
                    AttachFileSize = dbAttachFileSize > 0 ? dbAttachFileSize.ToString() : null,
                    CreateDTimeUTC = strCreateDTimeUTC,
                    CreateBy = strCreateBy,
                    LUDTimeUTC = dtimeSys.ToString("yyyy-MM-dd HH:mm:ss"),
                    LUBy = strCreateBy,
                    ApproveDTimeUTC = null,
                    ApproveBy = null,
                    FinishDTimeUTC = null,
                    FinishBy = null,
                    RejectDTimeUTC = null,
                    RejectBy = null,
                    LogLUDTimeUTC = dtimeSys.ToString("yyyy-MM-dd HH:mm:ss"),
                    LogLUBy = strWAUserCode
                };
                _db.Set<Crd_MemberChangeInfo>().Add(newHeader);

                if (reqDtl != null && reqDtl.Count > 0)
                {
                    foreach (var dtl in reqDtl)
                    {
                        var newDtl = new Crd_MemberChangeInfoDtl
                        {
                            RequestNo = strRequestNo ?? "",
                            NetworkID = strNetworkID,
                            OrgID = strOrgID,
                            ColumnCode = StdParam(dtl.ColumnCode) ?? "",
                            ColumnValueOld = StdStr(dtl.ColumnValueOld),
                            ColumnValueNew = StdStr(dtl.ColumnValueNew),
                            Remark = StdStr(dtl.Remark),
                            RequestDtlStatus = "PENDING",
                            LogLUDTimeUTC = dtimeSys.ToString("yyyy-MM-dd HH:mm:ss"),
                            LogLUBy = strWAUserCode
                        };
                        insertedDtls.Add(newDtl);
                        _db.Set<Crd_MemberChangeInfoDtl>().Add(newDtl);
                    }
                }
                await _db.SaveChangesAsync();

                // 9. Kiểm tra sau khi lưu (Check After Save, Card.cs:29252-29375)
                // Ràng buộc CarNo và VIN: gom nhóm theo ColumnCode in ('CarNo', 'VIN') (Card.cs:29260-29294, Ca 8)
                var carNoVinList = insertedDtls
                    .Where(x => string.Equals(x.ColumnCode, "CARNO", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(x.ColumnCode, "VIN", StringComparison.OrdinalIgnoreCase))
                    .GroupBy(x => x.ColumnCode)
                    .Select(g => g.Key)
                    .ToList();

                if (carNoVinList.Count > 1)
                {
                    await tx.RollbackAsync();
                    var firstRaw = reqDtl?.FirstOrDefault(x =>
                        string.Equals(x.ColumnCode, "CARNO", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(x.ColumnCode, "CarNo", StringComparison.OrdinalIgnoreCase))?.ColumnCode ?? "CarNo";
                    return PortResult.Fail("Crd_MemberChangeInfo_SaveX_InvalidCarNo_VIN",
                        "Check.CardNo", firstRaw,
                        "Expect", "Only 1 CarNo or VIN in Crd_MemberChangeInfoDtl");
                }

                // Ràng buộc hủy hội viên: chỉ cho phép tối đa 1 đề nghị PENDING huỷ hội viên (Card.cs:29298-29375)
                if (string.Equals(strRequestType, "CANCELMEMBER", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(strRequestType, "CANCELMEMBERNOTQUERY", StringComparison.OrdinalIgnoreCase))
                {
                    var p_MemberNo = PortQuery.Equal(strMemberNo ?? "");
                    var cancelPending = await _db.Set<Crd_MemberChangeInfo>()
                        .Where(t => EF.Functions.Like((t.MemberNo ?? "").ToLower(), p_MemberNo, PortQuery.Esc) &&
                                    t.RequestType == strRequestType &&
                                    t.RequestStatus == "PENDING")
                        .ToListAsync();

                    if (cancelPending.Count > 1)
                    {
                        await tx.RollbackAsync();
                        return PortResult.Fail("Crd_MemberChangeInfo_SaveX_InvalidRequestStatus_RequestType",
                            "Check.MemberNo", cancelPending[0].MemberNo,
                            "Check.RequestType", cancelPending[0].RequestType,
                            "Check.RequestStatus", cancelPending[0].RequestStatus,
                            "Expect", "Only 1 RequestStatus P, A in Crd_MemberChangeInfo with Type CancelMember");
                    }
                }
            }

            // Commit transaction an toàn (Card.cs:29521)
            await tx.CommitAsync();
            return Ok(new { Success = true, Data = (object?)null });
        }
        catch (Exception)
        {
            await tx.RollbackAsync();
            throw;
        }
    }
}

/// <summary>
/// Seeder bổ sung cho Crd_MemberChangeInfoController/Save (#032).
/// Order => 97 (dải 10-99 chưa dùng theo P3).
/// Idempotent. Đảm bảo cấu hình cột VIN tồn tại trong Crd_MemberColumnChange để ca 8 vượt qua guard cột và kiểm tra ràng buộc sau lưu CarNo_VIN.
/// </summary>
public class Crd_MemberChangeInfoSaveSeeder : IPortSeeder
{
    public int Order => 97;

    public async Task SeedAsync(AppDbContext db)
    {
        if (!await db.Set<Crd_MemberColumnChange>().AnyAsync(x => x.ColumnCode == "VIN"))
        {
            db.Set<Crd_MemberColumnChange>().Add(new Crd_MemberColumnChange
            {
                ColumnCode = "VIN",
                OrgID = "1001",
                NetworkID = "1001",
                ColumnName = "Số khung (VIN)",
                FlagLinkMst = "0",
                MasterName = null,
                Remark = "Cấu hình đổi số khung VIN",
                LogLUBy = "seed",
                LogLUDTimeUTC = "2026-08-01 05:00:00"
            });
            await db.SaveChangesAsync();
        }
    }
}
