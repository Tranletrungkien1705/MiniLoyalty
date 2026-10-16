using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MiniLoyalty.Data;
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

    public Crd_MemberChangeInfoControllerPort(AppDbContext db)
    {
        _db = db;
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
}
