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
/// DTO nhận tham số cho Crd_MemberChangeInfoController/Add (#033).
/// Gate: D:\idocNet\2022.7.Loyalty\13.ClientGate\V10\idn.Loyalty.WebAPI\Controllers\Crd_MemberChangeInfoController.cs:606-702
/// API: D:\idocNet\2022.7.Loyalty\11.BackEnd\V10\idn.Loyalty.Biz.Web\Controllers\CrdMemberChangeInfoController.cs:169-239
/// Biz: D:\idocNet\2022.7.Loyalty\11.BackEnd\V10\idn.Loyalty.Biz\Card.cs:29560-29659 (WAS_), 29399-29558 (Save), 28667-29397 (SaveX)
/// </summary>
public class Crd_MemberChangeInfoAddReq
{
    public Crd_MemberChangeInfo? Crd_MemberChangeInfo { get; set; }
    public List<Crd_MemberChangeInfoDtl>? Lst_Crd_MemberChangeInfoDtl { get; set; }
}

/// <summary>
/// DTO nhận tham số cho Crd_MemberChangeInfoController/Delete (#034).
/// Gate: D:\idocNet\2022.7.Loyalty\13.ClientGate\V10\idn.Loyalty.WebAPI\Controllers\Crd_MemberChangeInfoController.cs:707-770
/// API: D:\idocNet\2022.7.Loyalty\11.BackEnd\V10\idn.Loyalty.Biz.Web\Controllers\CrdMemberChangeInfoController.cs:169-239
/// Biz: D:\idocNet\2022.7.Loyalty\11.BackEnd\V10\idn.Loyalty.Biz\Card.cs:29560-29659 (WAS_), 29399-29558 (Save), 28667-29397 (SaveX)
/// </summary>
public class Crd_MemberChangeInfoDeleteReq
{
    public Crd_MemberChangeInfo? Crd_MemberChangeInfo { get; set; }
}

/// <summary>
/// DTO nhận tham số cho Crd_MemberChangeInfoController/Reject (#035).
/// Gate: D:\idocNet\2022.7.Loyalty\13.ClientGate\V10\idn.Loyalty.WebAPI\Controllers\Crd_MemberChangeInfoController.cs:774-836
/// API: D:\idocNet\2022.7.Loyalty\11.BackEnd\V10\idn.Loyalty.Biz.Web\Controllers\CrdMemberChangeInfoController.cs:388-458
/// Biz: D:\idocNet\2022.7.Loyalty\11.BackEnd\V10\idn.Loyalty.Biz\Card.cs:31510-31588 (WAS_), 31365-31508 (Reject), 31193-31363 (RejectX)
/// </summary>
public class Crd_MemberChangeInfoRejectReq
{
    public Crd_MemberChangeInfo? Crd_MemberChangeInfo { get; set; }
    public string? DLCPCode { get; set; }
    public string? WAUserCode { get; set; }
}

/// <summary>
/// DTO nhận tham số cho Crd_MemberChangeInfoController/Approve (#036).
/// Gate: D:\idocNet\2022.7.Loyalty\13.ClientGate\V10\idn.Loyalty.WebAPI\Controllers\Crd_MemberChangeInfoController.cs:840-904
/// API: D:\idocNet\2022.7.Loyalty\11.BackEnd\V10\idn.Loyalty.Biz.Web\Controllers\CrdMemberChangeInfoController.cs:242-312
/// Biz: D:\idocNet\2022.7.Loyalty\11.BackEnd\V10\idn.Loyalty.Biz\Card.cs:30215-30306 (WAS_), 30071-30213 (Approve), 29667-30005 (ApproveX)
/// </summary>
public class Crd_MemberChangeInfoApproveReq
{
    public Crd_MemberChangeInfo? Crd_MemberChangeInfo { get; set; }
    public List<Crd_MemberChangeInfoDtl>? Lst_Crd_MemberChangeInfoDtl { get; set; }
    public string? DLCPCode { get; set; }
    public string? WAUserCode { get; set; }
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
    private readonly Microsoft.AspNetCore.Hosting.IWebHostEnvironment? _env;

    public Crd_MemberChangeInfoControllerPort(AppDbContext db, IConfiguration? config = null, Microsoft.AspNetCore.Hosting.IWebHostEnvironment? env = null)
    {
        _db = db;
        _config = config;
        _env = env;
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
    /// Export (#038): Gate Crd_MemberChangeInfoController.cs (Export) — cùng bộ lọc với Search, Ft_RecordStart = "0", lấy toàn bộ;
    /// rỗng => ServiceException("Không có dữ liệu"); có dữ liệu => GetImportDicColums() (14 cột), sheet/tiền tố file "Crd_MemberChangeInfo", Data = { Url }.
    /// Lưu ý nguồn: từ điển dùng khoá MemberName/CarNo/PhoneNo trong khi model chỉ có cm_MemberName/cm_CarNo/cm_PhoneNo => 3 cột này không có dữ liệu.
    /// </summary>
    [HttpPost("export")]
    public async Task<IActionResult> Export([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Crd_MemberChangeInfoSearchReq? req)
    {
        var res = await Search(req);
        var okVal = (res as OkObjectResult)?.Value;
        var dataObj = okVal?.GetType().GetProperty("Data")?.GetValue(okVal);
        var rows = dataObj?.GetType().GetProperty("Lst_Crd_MemberChangeInfo")?.GetValue(dataObj) as List<Crd_MemberChangeInfo>;
        if (rows == null || rows.Count == 0)
        {
            return PortResult.Fail("Crd_MemberChangeInfo_Export", "Message", "Không có dữ liệu");
        }

        var webRoot = _env?.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRoot))
        {
            webRoot = _env != null ? System.IO.Path.Combine(_env.ContentRootPath, "wwwroot") : System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "wwwroot");
        }
        var dic = new Dictionary<string, string>
        {
            { "RequestNo", "Mã Đề nghị (*)" },
            { "MemberNo", "Mã thẻ hội viên" },
            { "MemberName", "Tên hội viên" },
            { "DLCodeRequest", "Đại lý đề nghị" },
            { "RequestType", "Loại đề nghị" },
            { "RequestStatus", "Trạng thái" },
            { "Remark", "Ghi chú" },
            { "CreateDTimeUTC", "Ngày tạo" },
            { "CarNo", "Biển số xe" },
            { "PhoneNo", "Số điện thoại" },
            { "ApproveDTimeUTC", "Ngày duyệt" },
            { "ApproveBy", "Người duyệt" },
            { "FinishDTimeUTC", "Ngày hoàn thành" },
            { "FinishBy", "Người hoàn thành" },
        };
        try
        {
            var (physicalPath, virtualPath) = PortExcel.GenExcelExportFilePath(webRoot, "Crd_MemberChangeInfo");
            PortExcel.ExportToExcelFromList(rows, dic, physicalPath, "Crd_MemberChangeInfo");
            return Ok(new { Success = true, Data = new { Url = PortExcel.GetRequestBaseAddress(Request) + virtualPath } });
        }
        catch (Exception ex)
        {
            return PortResult.Fail("Crd_MemberChangeInfo_Export", "Exception", ex.Message);
        }
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
    /// Thêm mới đề nghị thay đổi thông tin hội viên (Action Add, #033).
    /// Gate: D:\idocNet\2022.7.Loyalty\13.ClientGate\V10\idn.Loyalty.WebAPI\Controllers\Crd_MemberChangeInfoController.cs:606-702
    /// API: D:\idocNet\2022.7.Loyalty\11.BackEnd\V10\idn.Loyalty.Biz.Web\Controllers\CrdMemberChangeInfoController.cs:169-239
    /// Biz: D:\idocNet\2022.7.Loyalty\11.BackEnd\V10\idn.Loyalty.Biz\Card.cs:29560-29659 (WAS_), 29399-29558 (Save), 28667-29397 (SaveX)
    /// </summary>
    [HttpPost("add")]
    public async Task<IActionResult> Add([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Crd_MemberChangeInfoAddReq? req)
    {
        // 1. Kiểm tra rỗng / null ở tầng Gate (Gate :617, 626, Ca 1)
        if (req == null || req.Crd_MemberChangeInfo == null)
        {
            return PortResult.Fail("Crd_MemberChangeInfo_Save", "Check.strJson", null);
        }

        var headerInput = req.Crd_MemberChangeInfo;
        var dtlsInput = req.Lst_Crd_MemberChangeInfoDtl;

        // 2. Xử lý file trong hàm phụ Crd_MemberChangeInfo_Return (Gate :579-604, Ca 11)
        var flagFileUpload = (headerInput.FlagFileUpload ?? "").Trim();
        if (string.Equals(flagFileUpload, "1", StringComparison.OrdinalIgnoreCase))
        {
            // Trường hợp upload file (GAP: UploadFileX được ghi nhận trong GAP theo luật)
            headerInput.AttachFilePath = null;
        }
        else
        {
            // Trường hợp không upload file (chuẩn hóa gạch chéo thành gạch chéo ngược)
            if (!string.IsNullOrEmpty(headerInput.AttachFilePath))
            {
                headerInput.AttachFilePath = headerInput.AttachFilePath.Replace("/", "\\");
            }
            headerInput.AttachFileBase64 = null;
        }

        // 3. Sinh mã RequestNo tự động qua Sequence SeqType.RequestNo (Gate :646-668, Seq.cs:148)
        // Sinh số thứ tự trong transaction RIÊNG đã commit độc lập (LESSONS A.15, Ca 12)
        var seq = await PortSeq.NextAsync<Seq_RequestNo>(_db);
        var strRequestNo = PortSeq.Build("{0}REQUESTNO.{3}.{1:00000}{2}", "", seq, 100000, "", DateTime.UtcNow);

        // GHI ĐÈ RequestNo của client bằng RequestNo mới sinh từ sequence (Gate :666, Ca 8)
        headerInput.RequestNo = strRequestNo;

        // 4. Gọi logic lưu dùng chung SaveCoreAsync (LESSONS A.2 & A.3)
        return await SaveCoreAsync(headerInput, dtlsInput, bIsDelete: false);
    }

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

        return await SaveCoreAsync(req.Crd_MemberChangeInfo, null, bIsDelete: false);
    }

    /// <summary>
    /// Xóa đề nghị thay đổi thông tin hội viên (Action Delete, #034).
    /// Gate: D:\idocNet\2022.7.Loyalty\13.ClientGate\V10\idn.Loyalty.WebAPI\Controllers\Crd_MemberChangeInfoController.cs:707-770
    /// API: D:\idocNet\2022.7.Loyalty\11.BackEnd\V10\idn.Loyalty.Biz.Web\Controllers\CrdMemberChangeInfoController.cs:169-239
    /// Biz: D:\idocNet\2022.7.Loyalty\11.BackEnd\V10\idn.Loyalty.Biz\Card.cs:29560-29659 (WAS_), 29399-29558 (Save), 28667-29397 (SaveX)
    /// </summary>
    [HttpPost("delete")]
    public async Task<IActionResult> Delete([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Crd_MemberChangeInfoDeleteReq? req)
    {
        // 1. Kiểm tra rỗng / null ở tầng Gate (Gate :716-723, :759-762)
        if (req == null || req.Crd_MemberChangeInfo == null)
        {
            return PortResult.Fail("Crd_MemberChangeInfo_Save", "Check.strJson", null);
        }

        // 2. Gọi hàm SaveCoreAsync với bIsDelete = true (Gate :751-754, Card.cs:28726)
        return await SaveCoreAsync(req.Crd_MemberChangeInfo, null, bIsDelete: true);
    }

    /// <summary>
    /// Từ chối đề nghị thay đổi thông tin hội viên (Action Reject, #035).
    /// Gate: D:\idocNet\2022.7.Loyalty\13.ClientGate\V10\idn.Loyalty.WebAPI\Controllers\Crd_MemberChangeInfoController.cs:774-836
    /// API: D:\idocNet\2022.7.Loyalty\11.BackEnd\V10\idn.Loyalty.Biz.Web\Controllers\CrdMemberChangeInfoController.cs:388-458
    /// Biz: D:\idocNet\2022.7.Loyalty\11.BackEnd\V10\idn.Loyalty.Biz\Card.cs:31510-31588 (WAS_), 31365-31508 (Reject), 31193-31363 (RejectX)
    /// </summary>
    [HttpPost("reject")]
    public async Task<IActionResult> Reject([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Crd_MemberChangeInfoRejectReq? req)
    {
        // 1. Kiểm tra rỗng / null ở tầng Gate (Gate :783-790, Ca 7)
        if (req == null || req.Crd_MemberChangeInfo == null)
        {
            return PortResult.Fail("Crd_MemberChangeInfo_Reject", "Check.strJson", null);
        }

        var headerInput = req.Crd_MemberChangeInfo;

        // 2. Chuẩn hoá tham số đầu vào (Card.cs:31238-31240)
        var strRequestNo = StdParam(headerInput.RequestNo);
        var strRemarkHTV = StdStr(headerInput.RemarkHTV);

        var dtimeSys = DateTime.UtcNow;
        var strDTimeSys = dtimeSys.ToString("yyyy-MM-dd HH:mm:ss");

        var strWAUserCode = !string.IsNullOrWhiteSpace(req.WAUserCode)
            ? req.WAUserCode.Trim()
            : (Request.Headers.TryGetValue("WAUserCode", out var hdrUser) && !string.IsNullOrWhiteSpace(hdrUser)
                ? hdrUser.ToString().Trim()
                : "port");

        // Lấy DLCPCode của user hiện tại (Card.cs:30020-30021, P3, Ca 5)
        var strDLCPCode = !string.IsNullOrWhiteSpace(req.DLCPCode)
            ? req.DLCPCode.Trim()
            : (Request.Headers.TryGetValue("DLCPCode", out var hdrDlcp) && !string.IsNullOrWhiteSpace(hdrDlcp)
                ? hdrDlcp.ToString().Trim()
                : "HTV");
        strDLCPCode = StdParam(strDLCPCode) ?? "";

        // 3. Khởi tạo transaction an toàn (Card.cs:31398)
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // 4. Kiểm tra DB qua Crd_MemberChangeInfo_CheckDB(strRequestNo, "1", "PENDING, APPROVE") (Card.cs:31252-31258, 27896-27965)
            var p_RequestNo = PortQuery.Equal(strRequestNo ?? "");
            var existingRecord = await _db.Set<Crd_MemberChangeInfo>()
                .FirstOrDefaultAsync(x => EF.Functions.Like((x.RequestNo ?? "").ToLower(), p_RequestNo, PortQuery.Esc));

            if (existingRecord == null)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Crd_MemberChangeInfo_CheckDB_Crd_MemberChangeInfoNotFound",
                    "Check.RequestNo", strRequestNo);
            }

            var currentStatus = existingRecord.RequestStatus ?? "";
            if (!string.Equals(currentStatus, "PENDING", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(currentStatus, "APPROVE", StringComparison.OrdinalIgnoreCase))
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Crd_MemberChangeInfo_CheckDB_RequestStatusNotMatched",
                    "Check.RequestNo", strRequestNo,
                    "Check.strRequestStatusListToCheck", "PENDING, APPROVE",
                    "DB.RequestStatus", existingRecord.RequestStatus);
            }

            // 5. Kiểm tra quyền duyệt qua Crd_MemberChangeInfo_CheckApproveRight (Card.cs:31273-31279, 30012-30070)
            if (!string.Equals(strDLCPCode, "HTV", StringComparison.OrdinalIgnoreCase))
            {
                var strApproveDLCode = StdParam(existingRecord.ApproveDLCode) ?? "";
                if (string.IsNullOrEmpty(strDLCPCode) || !string.Equals(strApproveDLCode, strDLCPCode, StringComparison.OrdinalIgnoreCase))
                {
                    await tx.RollbackAsync();
                    return PortResult.Fail("Crd_MemberChangeInfo_Reject_InvalidApproveRight",
                        "Check.DLCPCode", strDLCPCode,
                        "Check.ApproveDLCode", strApproveDLCode,
                        "Expect", "User phai la HTV hoac dai ly da khoa trong Don vi duyet (ApproveDLCode) luc tao de nghi");
                }
            }

            // 6. Cập nhật DB (Card.cs:31321-31345)
            // LỖI NGUỒN GIỮ NGUYÊN: -- , t.Remark = f.Remark tại Card.cs:31327 bị chú thích, do đó Remark KHÔNG ĐƯỢC CẬP NHẬT (giữ nguyên giá trị cũ trong DB).
            existingRecord.RequestStatus = "CANCEL"; // TConst.RequestStatus.Cancel (Const.Main.BE.cs:1589)
            existingRecord.RejectDTimeUTC = strDTimeSys;
            existingRecord.RejectBy = strWAUserCode;
            existingRecord.RemarkHTV = strRemarkHTV;
            existingRecord.LUDTimeUTC = strDTimeSys;
            existingRecord.LUBy = strWAUserCode;
            existingRecord.LogLUDTimeUTC = strDTimeSys;
            existingRecord.LogLUBy = strWAUserCode;

            await _db.SaveChangesAsync();

            // Commit an toàn (Card.cs:31471)
            await tx.CommitAsync();

            // Trả về Success = true, Data = null (Gate :822-823)
            return Ok(new
            {
                Success = true,
                Data = (object?)null
            });
        }
        catch (Exception)
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Duyệt đề nghị thay đổi thông tin hội viên (Action Approve, #036).
    /// Gate: D:\idocNet\2022.7.Loyalty\13.ClientGate\V10\idn.Loyalty.WebAPI\Controllers\Crd_MemberChangeInfoController.cs:840-904
    /// API: D:\idocNet\2022.7.Loyalty\11.BackEnd\V10\idn.Loyalty.Biz.Web\Controllers\CrdMemberChangeInfoController.cs:242-312
    /// Biz: D:\idocNet\2022.7.Loyalty\11.BackEnd\V10\idn.Loyalty.Biz\Card.cs:30215-30306 (WAS_), 30071-30213 (Approve), 29667-30005 (ApproveX)
    /// </summary>
    [HttpPost("approve")]
    public async Task<IActionResult> Approve([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Crd_MemberChangeInfoApproveReq? req)
    {
        // 1. Kiểm tra rỗng / null ở tầng Gate (Gate :849-856, Ca 1)
        if (req == null || req.Crd_MemberChangeInfo == null)
        {
            return PortResult.Fail("Crd_MemberChangeInfo_Approve", "Check.strJson", null);
        }

        var headerInput = req.Crd_MemberChangeInfo;

        // 2. Chuẩn hoá tham số đầu vào (Card.cs:29711-29719)
        var strRequestNo = StdParam(headerInput.RequestNo);
        var strRemarkHTV = StdStr(headerInput.RemarkHTV);

        var dtimeSys = DateTime.UtcNow;
        var strDTimeSys = dtimeSys.ToString("yyyy-MM-dd HH:mm:ss");

        var strWAUserCode = !string.IsNullOrWhiteSpace(req.WAUserCode)
            ? req.WAUserCode.Trim()
            : (Request.Headers.TryGetValue("WAUserCode", out var hdrUser) && !string.IsNullOrWhiteSpace(hdrUser)
                ? hdrUser.ToString().Trim()
                : "port");

        // Lấy DLCPCode của user hiện tại (Card.cs:30020-30021, P3, Ca 4)
        var strDLCPCode = !string.IsNullOrWhiteSpace(req.DLCPCode)
            ? req.DLCPCode.Trim()
            : (Request.Headers.TryGetValue("DLCPCode", out var hdrDlcp) && !string.IsNullOrWhiteSpace(hdrDlcp)
                ? hdrDlcp.ToString().Trim()
                : "HTV");
        strDLCPCode = StdParam(strDLCPCode) ?? "";

        // 3. Khởi tạo transaction an toàn (Card.cs:30105)
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // 4. Kiểm tra DB qua Crd_MemberChangeInfo_CheckDB(strRequestNo, "1", "PENDING") (Card.cs:29722-29728, 27896-27965)
            var p_RequestNo = PortQuery.Equal(strRequestNo ?? "");
            var existingRecord = await _db.Set<Crd_MemberChangeInfo>()
                .FirstOrDefaultAsync(x => EF.Functions.Like((x.RequestNo ?? "").ToLower(), p_RequestNo, PortQuery.Esc));

            if (existingRecord == null)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Crd_MemberChangeInfo_CheckDB_Crd_MemberChangeInfoNotFound",
                    "Check.RequestNo", strRequestNo);
            }

            var currentStatus = existingRecord.RequestStatus ?? "";
            if (!string.Equals(currentStatus, "PENDING", StringComparison.OrdinalIgnoreCase))
            {
                await tx.RollbackAsync();
                return PortResult.Fail("Crd_MemberChangeInfo_CheckDB_RequestStatusNotMatched",
                    "Check.RequestNo", strRequestNo,
                    "Check.strRequestStatusListToCheck", "PENDING",
                    "DB.RequestStatus", existingRecord.RequestStatus);
            }

            // 5. Kiểm tra quyền duyệt qua Crd_MemberChangeInfo_CheckApproveRight (Card.cs:29736-29741, 30012-30070)
            if (!string.Equals(strDLCPCode, "HTV", StringComparison.OrdinalIgnoreCase))
            {
                var strApproveDLCode = StdParam(existingRecord.ApproveDLCode) ?? "";
                if (string.IsNullOrEmpty(strDLCPCode) || !string.Equals(strApproveDLCode, strDLCPCode, StringComparison.OrdinalIgnoreCase))
                {
                    await tx.RollbackAsync();
                    return PortResult.Fail("Crd_MemberChangeInfo_Approve_InvalidApproveRight",
                        "Check.DLCPCode", strDLCPCode,
                        "Check.ApproveDLCode", strApproveDLCode,
                        "Expect", "User phai la HTV hoac dai ly da khoa trong Don vi duyet (ApproveDLCode) luc tao de nghi");
                }
            }

            // 6. Cập nhật dữ liệu DB (Card.cs:29859-29921)
            // LỖI NGUỒN GIỮ NGUYÊN: Cột Remark được đưa vào #input_Crd_MemberChangeInfo (:554) nhưng bị BỎ SÓT trong mệnh đề SET của lệnh UPDATE Crd_MemberChangeInfo (:29860-29868). Do đó cột Remark của Crd_MemberChangeInfo KHÔNG ĐƯỢC CẬP NHẬT (giữ nguyên giá trị cũ trong DB).
            existingRecord.RequestStatus = "APPROVE";
            existingRecord.ApproveDTimeUTC = strDTimeSys;
            existingRecord.ApproveBy = strWAUserCode;
            existingRecord.RemarkHTV = strRemarkHTV;
            existingRecord.LUDTimeUTC = strDTimeSys;
            existingRecord.LUBy = strWAUserCode;
            existingRecord.LogLUDTimeUTC = strDTimeSys;
            existingRecord.LogLUBy = strWAUserCode;

            // Cập nhật bảng con Crd_MemberChangeInfoDtl (Card.cs:29891-29904)
            if (req.Lst_Crd_MemberChangeInfoDtl != null && req.Lst_Crd_MemberChangeInfoDtl.Count > 0)
            {
                var dtlList = await _db.Set<Crd_MemberChangeInfoDtl>()
                    .Where(x => EF.Functions.Like((x.RequestNo ?? "").ToLower(), p_RequestNo, PortQuery.Esc))
                    .ToListAsync();

                foreach (var dtlInput in req.Lst_Crd_MemberChangeInfoDtl)
                {
                    var colCode = StdParam(dtlInput.ColumnCode);
                    if (string.IsNullOrEmpty(colCode)) continue;

                    var existingDtl = dtlList.FirstOrDefault(x => string.Equals(x.ColumnCode, colCode, StringComparison.OrdinalIgnoreCase));
                    if (existingDtl != null)
                    {
                        existingDtl.RequestDtlStatus = "APPROVE";
                        existingDtl.Remark = dtlInput.Remark;
                        existingDtl.LogLUDTimeUTC = strDTimeSys;
                        existingDtl.LogLUBy = strWAUserCode;
                    }
                }
            }

            // Lưu thay đổi trước khi kiểm tra ràng buộc sau duyệt (Card.cs:29918-29921)
            await _db.SaveChangesAsync();

            // 7. Kiểm tra sau khi duyệt (Check After Approved, Card.cs:29924-30001)
            var strRequestType = existingRecord.RequestType ?? "";
            if (string.Equals(strRequestType, "CANCELMEMBER", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(strRequestType, "CANCELMEMBERNOTQUERY", StringComparison.OrdinalIgnoreCase))
            {
                var strMemberNo = existingRecord.MemberNo ?? "";
                var p_MemberNo = PortQuery.Equal(strMemberNo);
                var p_RequestType = PortQuery.Equal(strRequestType);
                var p_RequestStatus = PortQuery.Equal("APPROVE");

                var approvedCancelList = await _db.Set<Crd_MemberChangeInfo>()
                    .Where(t => EF.Functions.Like((t.MemberNo ?? "").ToLower(), p_MemberNo, PortQuery.Esc) &&
                                EF.Functions.Like((t.RequestType ?? "").ToLower(), p_RequestType, PortQuery.Esc) &&
                                EF.Functions.Like((t.RequestStatus ?? "").ToLower(), p_RequestStatus, PortQuery.Esc))
                    .ToListAsync();

                if (approvedCancelList.Count > 1)
                {
                    await tx.RollbackAsync();
                    return PortResult.Fail("Crd_MemberChangeInfo_ApproveX_InvalidRequestStatus_RequestType",
                        "Check.MemberNo", approvedCancelList[0].MemberNo,
                        "Check.RequestType", approvedCancelList[0].RequestType,
                        "Check.RequestStatus", approvedCancelList[0].RequestStatus,
                        "Expect", "Only 1 RequestStatus P, A in Crd_MemberChangeInfo with Type CancelMember");
                }
            }

            // Commit transaction an toàn (Card.cs:30176)
            await tx.CommitAsync();

            // Trả về Success = true, Data = null (Gate :890-891)
            return Ok(new
            {
                Success = true,
                Data = (object?)null
            });
        }
        catch (Exception)
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Nghiệp vụ lưu cốt lõi dùng chung giữa Save (#032) và Add (#033) (Card.cs:28667-29397 Crd_MemberChangeInfo_SaveX).
    /// </summary>
    private async Task<IActionResult> SaveCoreAsync(Crd_MemberChangeInfo headerInput, List<Crd_MemberChangeInfoDtl>? reqDtl, bool bIsDelete)
    {
        // 2. Chuẩn hoá tham số đầu vào (Card.cs:28726-28737)
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
                    AttachFileBase64 = strAttachFileBase64,
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

/// <summary>
/// Seeder bổ sung cho Crd_MemberChangeInfoController/Delete (#034).
/// Order => 92 (dải 10-99 chưa dùng theo P3).
/// Idempotent. Cung cấp dữ liệu mẫu kiểm thử xóa đề nghị trạng thái PENDING (REQ_DEL_01) kèm bản ghi con Crd_MemberChangeInfoDtl và trạng thái CANCEL (REQ_DEL_CANCEL).
/// </summary>
public class Crd_MemberChangeInfoDeleteSeeder : IPortSeeder
{
    public int Order => 92;

    public async Task SeedAsync(AppDbContext db)
    {
        // 1. Đảm bảo hội viên MB001 tồn tại trong Crd_Member để GetByRequestNo round-trip query không bị loại bỏ
        if (!await db.CrdMembers.AnyAsync(m => m.MemberNo == "MB001"))
        {
            db.CrdMembers.Add(new CrdMember
            {
                MemberNo = "MB001",
                NetworkID = "1001",
                OrgID = "ORG01",
                MemberName = "Nguyen Van A",
                CarNo = "30A-12345",
                PhoneNo = "0901234567",
                VIN = "VIN001",
                AmountEnd = 1000,
                CardTypeUse = "STANDARD",
                CreateBy = "seed",
                LUBy = "seed"
            });
            await db.SaveChangesAsync();
        }

        // 2. Seed đề nghị PENDING (REQ_DEL_01) phục vụ Ca 1
        if (!await db.Set<Crd_MemberChangeInfo>().AnyAsync(r => r.RequestNo == "REQ_DEL_01"))
        {
            db.Set<Crd_MemberChangeInfo>().Add(new Crd_MemberChangeInfo
            {
                RequestNo = "REQ_DEL_01",
                MemberNo = "MB001",
                NetworkID = "1001",
                OrgID = "ORG01",
                DLCodeRequest = "VN040",
                RequestType = "CHANGEINFO",
                RequestStatus = "PENDING",
                ApproveDLCode = "VN040",
                CreateDTimeUTC = "2026-08-01 08:00:00",
                FinishDTimeUTC = null,
                LogLUDTimeUTC = "2026-08-01 08:00:00",
                CreateBy = "seed",
                LUBy = "seed"
            });
            await db.SaveChangesAsync();
        }

        // Seed chi tiết cho REQ_DEL_01 trong Crd_MemberChangeInfoDtl
        if (!await db.Set<Crd_MemberChangeInfoDtl>().AnyAsync(d => d.RequestNo == "REQ_DEL_01"))
        {
            db.Set<Crd_MemberChangeInfoDtl>().Add(new Crd_MemberChangeInfoDtl
            {
                RequestNo = "REQ_DEL_01",
                ColumnCode = "PhoneNo",
                ColumnValueOld = "0901234567",
                ColumnValueNew = "0912345678",
                Remark = "Thay doi SDT cho REQ_DEL_01",
                RequestDtlStatus = "PENDING",
                NetworkID = "1001",
                OrgID = "ORG01",
                LogLUBy = "seed",
                LogLUDTimeUTC = "2026-08-01 08:00:00"
            });
            await db.SaveChangesAsync();
        }

        // 3. Seed đề nghị CANCEL (REQ_DEL_CANCEL) phục vụ Ca 2
        if (!await db.Set<Crd_MemberChangeInfo>().AnyAsync(r => r.RequestNo == "REQ_DEL_CANCEL"))
        {
            db.Set<Crd_MemberChangeInfo>().Add(new Crd_MemberChangeInfo
            {
                RequestNo = "REQ_DEL_CANCEL",
                MemberNo = "MB001",
                NetworkID = "1001",
                OrgID = "ORG01",
                DLCodeRequest = "VN040",
                RequestType = "CHANGEINFO",
                RequestStatus = "CANCEL",
                ApproveDLCode = "VN040",
                CreateDTimeUTC = "2026-08-02 08:00:00",
                FinishDTimeUTC = null,
                LogLUDTimeUTC = "2026-08-02 08:00:00",
                CreateBy = "seed",
                LUBy = "seed"
            });
            await db.SaveChangesAsync();
        }

        // Seed chi tiết cho REQ_DEL_CANCEL trong Crd_MemberChangeInfoDtl
        if (!await db.Set<Crd_MemberChangeInfoDtl>().AnyAsync(d => d.RequestNo == "REQ_DEL_CANCEL"))
        {
            db.Set<Crd_MemberChangeInfoDtl>().Add(new Crd_MemberChangeInfoDtl
            {
                RequestNo = "REQ_DEL_CANCEL",
                ColumnCode = "PhoneNo",
                ColumnValueOld = "0901234567",
                ColumnValueNew = "0988888888",
                Remark = "Thay doi SDT cho REQ_DEL_CANCEL",
                RequestDtlStatus = "CANCEL",
                NetworkID = "1001",
                OrgID = "ORG01",
                LogLUBy = "seed",
                LogLUDTimeUTC = "2026-08-02 08:00:00"
            });
            await db.SaveChangesAsync();
        }
    }
}

/// <summary>
/// Seeder bổ sung cho Crd_MemberChangeInfoController/Reject (#035).
/// Order => 77 (dải 10-99 chưa dùng theo P3).
/// Idempotent. Cung cấp dữ liệu mẫu kiểm thử từ chối đề nghị trạng thái PENDING (REQ_REJ_01), APPROVE (REQ_REJ_02),
/// CANCEL (REQ_REJ_CANCEL), FINISH (REQ_REJ_FINISH).
/// </summary>
public class Crd_MemberChangeInfoRejectSeeder : IPortSeeder
{
    public int Order => 77;

    public async Task SeedAsync(AppDbContext db)
    {
        // 1. Đảm bảo hội viên MB001 tồn tại trong Crd_Member để GetByRequestNo round-trip query không bị loại bỏ do inner join
        if (!await db.CrdMembers.AnyAsync(m => m.MemberNo == "MB001"))
        {
            var now = DateTime.UtcNow;
            const string net = "DEMO";
            db.CrdMembers.AddRange(
                new CrdMember
                {
                    MemberNo = "MB001",
                    NetworkID = net,
                    OrgID = net,
                    DLCodeRegis = "DL01",
                    RegistrationDate = now.AddMonths(-6),
                    CustomerCode = "KH001",
                    ProvinceCode = "HN",
                    DistrictCode = "HN01",
                    MemberName = "Nguyễn Văn MB001",
                    MemberGender = "1",
                    IDCardNo = "001090001001",
                    PhoneNo = "0988111001",
                    DateOfBirth = new DateTime(1990, 1, 1),
                    MemAddress = "Hà Nội",
                    CarNo = "30A-99001",
                    VIN = "VINMB000000000001",
                    ModelCode = "ACC",
                    ModelName = "Accent",
                    TradeMarkCode = "HYU",
                    EffDateStart = now.AddMonths(-6),
                    EffDateEnd = now.AddMonths(6),
                    PointTotal = 1000,
                    PointAvail = 800,
                    PointBegin = 0,
                    PointEnd = 800,
                    AmountTotal = 10000000,
                    AmountAvail = 10000000,
                    AmountBegin = 0,
                    AmountEnd = 10000000,
                    QtyVisitTotal = 2,
                    QtyVisitAvail = 2,
                    PointCardRank = 1000,
                    AmountCardRank = 10000000,
                    CardTypeUse = "P",
                    CardTypeInit = "P",
                    MemberActiveDate = now.AddMonths(-6),
                    FlagExceptionally = "0",
                    FlagExceptionCard = "0",
                    PointVoucher = 50,
                    MemberStatus = "APPROVE",
                    CardStatus = "APPROVE",
                    CardSourceCode = "NEW",
                    PurchaseType = "BUYNEW",
                    CreateDTimeUTC = now.AddMonths(-6),
                    CreateBy = "seed",
                    LUDTimeUTC = now,
                    LUBy = "seed",
                    LogLUDTimeUTC = now,
                    LogLUBy = "seed"
                },
                new CrdMember
                {
                    MemberNo = "MB002",
                    NetworkID = net,
                    OrgID = net,
                    DLCodeRegis = "DL01",
                    RegistrationDate = now.AddMonths(-3),
                    CustomerCode = "KH002",
                    ProvinceCode = "HN",
                    DistrictCode = "HN01",
                    MemberName = "Trần Thị MB002",
                    MemberGender = "0",
                    IDCardNo = "001090001002",
                    PhoneNo = "0988111002",
                    DateOfBirth = new DateTime(1992, 5, 10),
                    MemAddress = "Hà Nội",
                    CarNo = "30A-99002",
                    VIN = "VINMB000000000002",
                    ModelCode = "ACC",
                    ModelName = "Accent",
                    TradeMarkCode = "HYU",
                    EffDateStart = now.AddMonths(-3),
                    EffDateEnd = now.AddMonths(9),
                    PointTotal = 500,
                    PointAvail = 500,
                    PointBegin = 0,
                    PointEnd = 500,
                    AmountTotal = 5000000,
                    AmountAvail = 5000000,
                    AmountBegin = 0,
                    AmountEnd = 5000000,
                    QtyVisitTotal = 1,
                    QtyVisitAvail = 1,
                    PointCardRank = 500,
                    AmountCardRank = 5000000,
                    CardTypeUse = "G",
                    CardTypeInit = "G",
                    MemberActiveDate = now.AddMonths(-3),
                    FlagExceptionally = "0",
                    FlagExceptionCard = "0",
                    PointVoucher = 0,
                    MemberStatus = "PENDING",
                    CardStatus = "PENDING",
                    CardSourceCode = "NEW",
                    PurchaseType = "BUYADD",
                    CreateDTimeUTC = now.AddMonths(-3),
                    CreateBy = "seed",
                    LUDTimeUTC = now,
                    LUBy = "seed",
                    LogLUDTimeUTC = now,
                    LogLUBy = "seed"
                },
                new CrdMember
                {
                    MemberNo = "MB003",
                    NetworkID = net,
                    OrgID = net,
                    DLCodeRegis = "DL01",
                    RegistrationDate = now.AddMonths(-1),
                    CustomerCode = "KH003",
                    ProvinceCode = "HN",
                    DistrictCode = "HN01",
                    MemberName = "Lê Văn MB003",
                    MemberGender = "1",
                    IDCardNo = "001090001003",
                    PhoneNo = "0988111003",
                    DateOfBirth = new DateTime(1988, 8, 20),
                    MemAddress = "Hà Nội",
                    CarNo = "30A-99003",
                    VIN = "VINMB000000000003",
                    ModelCode = "ACC",
                    ModelName = "Accent",
                    TradeMarkCode = "HYU",
                    EffDateStart = now.AddMonths(-1),
                    EffDateEnd = now.AddMonths(11),
                    PointTotal = 200,
                    PointAvail = 200,
                    PointBegin = 0,
                    PointEnd = 200,
                    AmountTotal = 2000000,
                    AmountAvail = 2000000,
                    AmountBegin = 0,
                    AmountEnd = 2000000,
                    QtyVisitTotal = 1,
                    QtyVisitAvail = 1,
                    PointCardRank = 200,
                    AmountCardRank = 2000000,
                    CardTypeUse = "S",
                    CardTypeInit = "S",
                    MemberActiveDate = now.AddMonths(-1),
                    FlagExceptionally = "0",
                    FlagExceptionCard = "0",
                    PointVoucher = 0,
                    MemberStatus = "CANCEL",
                    CardStatus = "CANCEL",
                    CardSourceCode = "NEW",
                    PurchaseType = "BUYADD",
                    CreateDTimeUTC = now.AddMonths(-1),
                    CreateBy = "seed",
                    LUDTimeUTC = now,
                    LUBy = "seed",
                    LogLUDTimeUTC = now,
                    LogLUBy = "seed"
                }
            );
            await db.SaveChangesAsync();
        }

        // 2. Seed các đề nghị: REQ_REJ_01, REQ_REJ_02, REQ_REJ_CANCEL, REQ_REJ_FINISH
        if (!await db.Set<Crd_MemberChangeInfo>().AnyAsync(r => r.RequestNo == "REQ_REJ_01"))
        {
            db.Set<Crd_MemberChangeInfo>().Add(new Crd_MemberChangeInfo
            {
                RequestNo = "REQ_REJ_01",
                MemberNo = "MB001",
                NetworkID = "1001",
                OrgID = "ORG01",
                DLCodeRequest = "VN040",
                ApproveDLCode = "VN040",
                RequestType = "CHANGEINFO",
                RequestStatus = "PENDING",
                Remark = "Ghi chu goc REQ_REJ_01",
                RemarkHTV = null,
                CreateDTimeUTC = "2026-08-01 08:00:00",
                LogLUDTimeUTC = "2026-08-01 08:00:00",
                CreateBy = "seed",
                LUBy = "seed"
            });
        }

        if (!await db.Set<Crd_MemberChangeInfo>().AnyAsync(r => r.RequestNo == "REQ_REJ_02"))
        {
            db.Set<Crd_MemberChangeInfo>().Add(new Crd_MemberChangeInfo
            {
                RequestNo = "REQ_REJ_02",
                MemberNo = "MB001",
                NetworkID = "1001",
                OrgID = "ORG01",
                DLCodeRequest = "VN040",
                ApproveDLCode = "VN040",
                RequestType = "CHANGEINFO",
                RequestStatus = "APPROVE",
                Remark = "Ghi chu goc REQ_REJ_02",
                RemarkHTV = null,
                CreateDTimeUTC = "2026-08-02 08:00:00",
                LogLUDTimeUTC = "2026-08-02 08:00:00",
                CreateBy = "seed",
                LUBy = "seed"
            });
        }

        if (!await db.Set<Crd_MemberChangeInfo>().AnyAsync(r => r.RequestNo == "REQ_REJ_CANCEL"))
        {
            db.Set<Crd_MemberChangeInfo>().Add(new Crd_MemberChangeInfo
            {
                RequestNo = "REQ_REJ_CANCEL",
                MemberNo = "MB001",
                NetworkID = "1001",
                OrgID = "ORG01",
                DLCodeRequest = "VN040",
                ApproveDLCode = "VN040",
                RequestType = "CHANGEINFO",
                RequestStatus = "CANCEL",
                CreateDTimeUTC = "2026-08-03 08:00:00",
                LogLUDTimeUTC = "2026-08-03 08:00:00",
                CreateBy = "seed",
                LUBy = "seed"
            });
        }

        if (!await db.Set<Crd_MemberChangeInfo>().AnyAsync(r => r.RequestNo == "REQ_REJ_FINISH"))
        {
            db.Set<Crd_MemberChangeInfo>().Add(new Crd_MemberChangeInfo
            {
                RequestNo = "REQ_REJ_FINISH",
                MemberNo = "MB001",
                NetworkID = "1001",
                OrgID = "ORG01",
                DLCodeRequest = "VN040",
                ApproveDLCode = "VN040",
                RequestType = "CHANGEINFO",
                RequestStatus = "FINISH",
                CreateDTimeUTC = "2026-08-04 08:00:00",
                LogLUDTimeUTC = "2026-08-04 08:00:00",
                CreateBy = "seed",
                LUBy = "seed"
            });
        }

        await db.SaveChangesAsync();
    }
}

/// <summary>
/// Seeder bổ sung cho Crd_MemberChangeInfoController/Approve (#036).
/// Order => 78 (dải 10-99 chưa dùng theo P3).
/// Idempotent. Cung cấp dữ liệu mẫu kiểm thử duyệt đề nghị trạng thái PENDING (REQ_APP_01, REQ_APP_02, REQ_APP_03, REQ_APP_OK01, REQ_APP_OK02, REQ_APP_OK03),
/// trạng thái APPROVE (REQ_APP_STATUS_ERR), sai quyền duyệt (REQ_APP_RIGHT_ERR), và trùng đề nghị CancelMember (REQ_CANCEL_EXIST, REQ_CANCEL_NEW).
/// </summary>
public class Crd_MemberChangeInfoApproveSeeder : IPortSeeder
{
    public int Order => 78;

    public async Task SeedAsync(AppDbContext db)
    {
        // 1. Đảm bảo hội viên MB001 và MB_CANCEL_01 tồn tại trong CrdMembers
        var now = DateTime.UtcNow;
        const string net = "DEMO";

        if (!await db.CrdMembers.AnyAsync(m => m.MemberNo == "MB001"))
        {
            db.CrdMembers.Add(new CrdMember
            {
                MemberNo = "MB001",
                NetworkID = net,
                OrgID = net,
                DLCodeRegis = "VN040",
                RegistrationDate = now.AddMonths(-6),
                CustomerCode = "KH001",
                ProvinceCode = "HN",
                DistrictCode = "HN01",
                MemberName = "Nguyen Van MB001",
                MemberGender = "1",
                IDCardNo = "001090001001",
                PhoneNo = "0901234567",
                DateOfBirth = new DateTime(1990, 1, 1),
                MemAddress = "Ha Noi",
                CarNo = "30A-99001",
                VIN = "VINMB000000000001",
                ModelCode = "ACC",
                ModelName = "Accent",
                TradeMarkCode = "HYU",
                EffDateStart = now.AddMonths(-6),
                EffDateEnd = now.AddMonths(6),
                PointTotal = 1000,
                PointAvail = 1000,
                PointBegin = 0,
                PointEnd = 1000,
                AmountTotal = 10000000,
                AmountAvail = 10000000,
                AmountBegin = 0,
                AmountEnd = 10000000,
                QtyVisitTotal = 1,
                QtyVisitAvail = 1,
                PointCardRank = 1000,
                AmountCardRank = 10000000,
                CardTypeUse = "P",
                CardTypeInit = "P",
                MemberActiveDate = now.AddMonths(-6),
                FlagExceptionally = "0",
                FlagExceptionCard = "0",
                PointVoucher = 0,
                MemberStatus = "APPROVE",
                CardStatus = "APPROVE",
                CardSourceCode = "NEW",
                PurchaseType = "BUYNEW",
                CreateDTimeUTC = now.AddMonths(-6),
                CreateBy = "seed",
                LUDTimeUTC = now,
                LUBy = "seed",
                LogLUDTimeUTC = now,
                LogLUBy = "seed"
            });
            await db.SaveChangesAsync();
        }

        if (!await db.CrdMembers.AnyAsync(m => m.MemberNo == "MB_CANCEL_01"))
        {
            db.CrdMembers.Add(new CrdMember
            {
                MemberNo = "MB_CANCEL_01",
                NetworkID = net,
                OrgID = net,
                DLCodeRegis = "VN040",
                RegistrationDate = now.AddMonths(-6),
                CustomerCode = "KH_CANCEL_01",
                ProvinceCode = "HN",
                DistrictCode = "HN01",
                MemberName = "Nguyen Van Cancel",
                MemberGender = "1",
                IDCardNo = "001090009999",
                PhoneNo = "0988999999",
                DateOfBirth = new DateTime(1990, 1, 1),
                MemAddress = "Ha Noi",
                CarNo = "30A-99999",
                VIN = "VINCANCEL00000001",
                ModelCode = "ACC",
                ModelName = "Accent",
                TradeMarkCode = "HYU",
                EffDateStart = now.AddMonths(-6),
                EffDateEnd = now.AddMonths(6),
                PointTotal = 1000,
                PointAvail = 1000,
                PointBegin = 0,
                PointEnd = 1000,
                AmountTotal = 10000000,
                AmountAvail = 10000000,
                AmountBegin = 0,
                AmountEnd = 10000000,
                QtyVisitTotal = 1,
                QtyVisitAvail = 1,
                PointCardRank = 1000,
                AmountCardRank = 10000000,
                CardTypeUse = "P",
                CardTypeInit = "P",
                MemberActiveDate = now.AddMonths(-6),
                FlagExceptionally = "0",
                FlagExceptionCard = "0",
                PointVoucher = 0,
                MemberStatus = "APPROVE",
                CardStatus = "APPROVE",
                CardSourceCode = "NEW",
                PurchaseType = "BUYNEW",
                CreateDTimeUTC = now.AddMonths(-6),
                CreateBy = "seed",
                LUDTimeUTC = now,
                LUBy = "seed",
                LogLUDTimeUTC = now,
                LogLUBy = "seed"
            });
            await db.SaveChangesAsync();
        }

        // 2. Seed các đề nghị và chi tiết
        // REQ_APP_01 & REQ_APP_OK01
        foreach (var reqNo in new[] { "REQ_APP_01", "REQ_APP_OK01" })
        {
            if (!await db.Set<Crd_MemberChangeInfo>().AnyAsync(r => r.RequestNo == reqNo))
            {
                db.Set<Crd_MemberChangeInfo>().Add(new Crd_MemberChangeInfo
                {
                    RequestNo = reqNo,
                    MemberNo = "MB001",
                    NetworkID = "1001",
                    OrgID = "ORG01",
                    DLCodeRequest = "VN040",
                    ApproveDLCode = "VN040",
                    RequestType = "CHANGEINFO",
                    RequestStatus = "PENDING",
                    Remark = "Ghi chu goc REQ_APP_01",
                    RemarkHTV = null,
                    CreateDTimeUTC = "2026-08-01 08:00:00",
                    LogLUDTimeUTC = "2026-08-01 08:00:00",
                    CreateBy = "seed",
                    LUBy = "seed"
                });
            }

            if (!await db.Set<Crd_MemberChangeInfoDtl>().AnyAsync(d => d.RequestNo == reqNo && d.ColumnCode == "PhoneNo"))
            {
                db.Set<Crd_MemberChangeInfoDtl>().Add(new Crd_MemberChangeInfoDtl
                {
                    RequestNo = reqNo,
                    ColumnCode = "PhoneNo",
                    ColumnValueOld = "0901234567",
                    ColumnValueNew = "0912345678",
                    Remark = "Chi tiet PhoneNo goc",
                    RequestDtlStatus = "PENDING",
                    NetworkID = "1001",
                    OrgID = "ORG01",
                    LogLUBy = "seed",
                    LogLUDTimeUTC = "2026-08-01 08:00:00"
                });
            }
        }

        // REQ_APP_02 & REQ_APP_OK02
        foreach (var reqNo in new[] { "REQ_APP_02", "REQ_APP_OK02" })
        {
            if (!await db.Set<Crd_MemberChangeInfo>().AnyAsync(r => r.RequestNo == reqNo))
            {
                db.Set<Crd_MemberChangeInfo>().Add(new Crd_MemberChangeInfo
                {
                    RequestNo = reqNo,
                    MemberNo = "MB001",
                    NetworkID = "1001",
                    OrgID = "ORG01",
                    DLCodeRequest = "VN040",
                    ApproveDLCode = "VN040",
                    RequestType = "CHANGEINFO",
                    RequestStatus = "PENDING",
                    Remark = "Ghi chu goc REQ_APP_02",
                    RemarkHTV = null,
                    CreateDTimeUTC = "2026-08-02 08:00:00",
                    LogLUDTimeUTC = "2026-08-02 08:00:00",
                    CreateBy = "seed",
                    LUBy = "seed"
                });
            }

            if (!await db.Set<Crd_MemberChangeInfoDtl>().AnyAsync(d => d.RequestNo == reqNo && d.ColumnCode == "MemAddress"))
            {
                db.Set<Crd_MemberChangeInfoDtl>().Add(new Crd_MemberChangeInfoDtl
                {
                    RequestNo = reqNo,
                    ColumnCode = "MemAddress",
                    ColumnValueOld = "Ha Noi",
                    ColumnValueNew = "Hai Phong",
                    Remark = "Chi tiet Dia chi goc",
                    RequestDtlStatus = "PENDING",
                    NetworkID = "1001",
                    OrgID = "ORG01",
                    LogLUBy = "seed",
                    LogLUDTimeUTC = "2026-08-02 08:00:00"
                });
            }
        }

        // REQ_APP_03 & REQ_APP_OK03
        foreach (var reqNo in new[] { "REQ_APP_03", "REQ_APP_OK03" })
        {
            if (!await db.Set<Crd_MemberChangeInfo>().AnyAsync(r => r.RequestNo == reqNo))
            {
                db.Set<Crd_MemberChangeInfo>().Add(new Crd_MemberChangeInfo
                {
                    RequestNo = reqNo,
                    MemberNo = "MB001",
                    NetworkID = "1001",
                    OrgID = "ORG01",
                    DLCodeRequest = "VN040",
                    ApproveDLCode = "VN040",
                    RequestType = "CHANGEINFO",
                    RequestStatus = "PENDING",
                    Remark = "Ghi chu goc REQ_APP_03",
                    RemarkHTV = null,
                    CreateDTimeUTC = "2026-08-03 08:00:00",
                    LogLUDTimeUTC = "2026-08-03 08:00:00",
                    CreateBy = "seed",
                    LUBy = "seed"
                });
            }
        }

        // 4. REQ_APP_STATUS_ERR
        if (!await db.Set<Crd_MemberChangeInfo>().AnyAsync(r => r.RequestNo == "REQ_APP_STATUS_ERR"))
        {
            db.Set<Crd_MemberChangeInfo>().Add(new Crd_MemberChangeInfo
            {
                RequestNo = "REQ_APP_STATUS_ERR",
                MemberNo = "MB001",
                NetworkID = "1001",
                OrgID = "ORG01",
                DLCodeRequest = "VN040",
                ApproveDLCode = "VN040",
                RequestType = "CHANGEINFO",
                RequestStatus = "APPROVE",
                CreateDTimeUTC = "2026-08-04 08:00:00",
                LogLUDTimeUTC = "2026-08-04 08:00:00",
                CreateBy = "seed",
                LUBy = "seed"
            });
        }

        // 5. REQ_APP_RIGHT_ERR
        if (!await db.Set<Crd_MemberChangeInfo>().AnyAsync(r => r.RequestNo == "REQ_APP_RIGHT_ERR"))
        {
            db.Set<Crd_MemberChangeInfo>().Add(new Crd_MemberChangeInfo
            {
                RequestNo = "REQ_APP_RIGHT_ERR",
                MemberNo = "MB001",
                NetworkID = "1001",
                OrgID = "ORG01",
                DLCodeRequest = "VN040",
                ApproveDLCode = "VN040",
                RequestType = "CHANGEINFO",
                RequestStatus = "PENDING",
                CreateDTimeUTC = "2026-08-05 08:00:00",
                LogLUDTimeUTC = "2026-08-05 08:00:00",
                CreateBy = "seed",
                LUBy = "seed"
            });
        }

        // 6. REQ_CANCEL_EXIST & REQ_CANCEL_NEW
        if (!await db.Set<Crd_MemberChangeInfo>().AnyAsync(r => r.RequestNo == "REQ_CANCEL_EXIST"))
        {
            db.Set<Crd_MemberChangeInfo>().Add(new Crd_MemberChangeInfo
            {
                RequestNo = "REQ_CANCEL_EXIST",
                MemberNo = "MB_CANCEL_01",
                NetworkID = "1001",
                OrgID = "ORG01",
                DLCodeRequest = "VN040",
                ApproveDLCode = "",
                RequestType = "CANCELMEMBER",
                RequestStatus = "APPROVE",
                CreateDTimeUTC = "2026-08-06 08:00:00",
                LogLUDTimeUTC = "2026-08-06 08:00:00",
                CreateBy = "seed",
                LUBy = "seed"
            });
        }

        if (!await db.Set<Crd_MemberChangeInfo>().AnyAsync(r => r.RequestNo == "REQ_CANCEL_NEW"))
        {
            db.Set<Crd_MemberChangeInfo>().Add(new Crd_MemberChangeInfo
            {
                RequestNo = "REQ_CANCEL_NEW",
                MemberNo = "MB_CANCEL_01",
                NetworkID = "1001",
                OrgID = "ORG01",
                DLCodeRequest = "VN040",
                ApproveDLCode = "",
                RequestType = "CANCELMEMBER",
                RequestStatus = "PENDING",
                CreateDTimeUTC = "2026-08-07 08:00:00",
                LogLUDTimeUTC = "2026-08-07 08:00:00",
                CreateBy = "seed",
                LUBy = "seed"
            });
        }

        await db.SaveChangesAsync();
    }
}



