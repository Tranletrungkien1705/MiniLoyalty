using Microsoft.EntityFrameworkCore;
using MiniLoyalty.Data;
using MiniLoyalty.Models;
using MiniLoyalty.Services;

// Npgsql: DateTime (Kind Local/Unspecified) '' timestamp without time zone (khong phai timestamptz)
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

var conn = Environment.GetEnvironmentVariable("CONNECTION_STRING")
    ?? builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=miniloyalty.db";
builder.Services.AddDbContext<AppDbContext>(o =>
{
    if (DbUtil.IsPostgres(conn)) o.UseNpgsql(DbUtil.ToNpgsql(conn));
    else o.UseSqlite(conn);
});
builder.Services.AddScoped<ITenantContext, TenantContext>();   // multi-tenant: ngữ cảnh org/request
builder.Services.AddScoped<ILoyaltyService, LoyaltyService>();
builder.Services.AddControllersWithViews();

var app = builder.Build();
using (var scope = app.Services.CreateScope())
    await Seeder.SeedAsync(scope.ServiceProvider.GetRequiredService<AppDbContext>());

// Multi-tenant: X-Api-Key → OrgId (đặt TRƯỚC khi AppDbContext của request được dựng, dùng scope tra cứu riêng).
app.Use(async (ctx, next) =>
{
    var key = ctx.Request.Headers["X-Api-Key"].FirstOrDefault();
    if (!string.IsNullOrWhiteSpace(key))
    {
        using var lookup = app.Services.CreateScope();
        var ldb = lookup.ServiceProvider.GetRequiredService<AppDbContext>();
        var org = await ldb.Orgs.FirstOrDefaultAsync(o => o.ApiKey == key);
        if (org != null) ctx.RequestServices.GetRequiredService<ITenantContext>().OrgId = org.Id;
    }
    await next();
});

app.UseStaticFiles();
app.MapGet("/healthz", () => "ok");

// Đăng ký tổ chức mới (nhận khách) — trả về ApiKey để gọi API với dữ liệu cô lập.
app.MapPost("/api/orgs/register", async (RegisterOrgDto dto, AppDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(dto.Name)) return Results.BadRequest(new { error = "Cần Name." });
    var apiKey = "lyl_" + Guid.NewGuid().ToString("N");
    var org = new Org { Name = dto.Name.Trim(), ApiKey = apiKey };
    db.Orgs.Add(org);
    await db.SaveChangesAsync();
    return Results.Ok(new { orgId = org.Id, apiKey, note = "Gửi header X-Api-Key khi gọi API để dữ liệu cô lập." });
});

// API tích điểm từ đơn hàng (MiniDMS gọi): theo SĐT hoặc mã hội viên.
app.MapPost("/api/earn", async (EarnDto dto, ILoyaltyService svc) =>
{
    Member? m = dto.Phone is { Length: > 0 } p ? await svc.GetByPhoneAsync(p) : null;
    if (m == null && dto.MemberId is { } mid) m = await svc.GetAsync(mid);
    if (m == null) return Results.NotFound(new { error = "Không tìm thấy hội viên" });
    var tx = await svc.EarnFromPurchaseAsync(m.Id, dto.Amount, dto.RefNo);
    var member = await svc.GetAsync(m.Id);
    return Results.Ok(new { memberCode = member!.Code, earned = tx.Points, balance = member.Points, rank = member.RankTier?.Name });
});

// API chạy job hết hạn điểm (điểm cộng quá hạn dùng bị trừ). Idempotent.
app.MapPost("/api/expiry/run", async (ILoyaltyService svc) =>
{
    var r = await svc.RunExpiryJobAsync();
    return Results.Ok(new { date = r.Date, members = r.Members, expiredPoints = r.Points, details = r.Details });
});

// API chạy job phát voucher sinh nhật (DealPointType=VOUCHERTSN): 1 voucher/năm cho hội viên sinh nhật hôm nay. Idempotent.
app.MapPost("/api/birthdayvoucher/run", async (ILoyaltyService svc) =>
{
    var r = await svc.RunBirthdayVoucherJobAsync();
    return Results.Ok(new { date = r.Date, issued = r.Issued, points = r.Points, details = r.Details });
});

// API chạy job xét hạng cuối kỳ (nâng/duy trì/xuống hạng theo ngưỡng Mst_RankPolicy). Idempotent theo kỳ.
app.MapPost("/api/rank/keepdown/run", async (ILoyaltyService svc) =>
{
    var r = await svc.RunRankKeepDownJobAsync();
    return Results.Ok(new { date = r.Date, up = r.Up, kept = r.Kept, down = r.Down, details = r.Details });
});

// API lịch sử xét hạng (Crd_CardRank, DealPointType=LOYALTY): audit trail UP/KEEP/DOWN kèm hạng trước/sau.
app.MapGet("/api/rank/history", async (int? memberId, ILoyaltyService svc) =>
{
    var list = await svc.RankHistoryAsync(memberId);
    return Results.Ok(list.Select(h => new
    {
        h.CardRankNo, memberCode = h.Member?.Code, memberName = h.Member?.Name,
        action = h.Action.ToString(), h.CardSourceCode, h.DealPointType,
        rankBefore = h.RankTierBefore?.Name, rankAfter = h.RankTierAfter?.Name,
        h.PointCardRankBefore, h.QtyVisitBefore, h.FunctionRemark, h.CreatedAt
    }));
});

// API thưởng điểm giới thiệu (DealPointType=INTRODUCTION): cộng điểm cho người giới thiệu của hội viên mới.
app.MapPost("/api/introduction/award", async (IntroDto dto, ILoyaltyService svc) =>
{
    var m = dto.Phone is { Length: > 0 } p ? await svc.GetByPhoneAsync(p) : null;
    if (m == null && dto.MemberId is { } mid) m = await svc.GetAsync(mid);
    if (m == null) return Results.NotFound(new { error = "Không tìm thấy hội viên" });
    var (ok, msg) = await svc.AwardIntroductionAsync(m.Id);
    return ok ? Results.Ok(new { ok, msg }) : Results.BadRequest(new { ok, error = msg });
});

// API ghi nhận lượt dịch vụ (DealPointType=SERVICETURN): cộng lượt vào QtyVisitAvail, không đổi điểm.
app.MapPost("/api/serviceturn", async (ServiceTurnDto dto, ILoyaltyService svc) =>
{
    var m = dto.Phone is { Length: > 0 } p ? await svc.GetByPhoneAsync(p) : null;
    if (m == null && dto.MemberId is { } mid) m = await svc.GetAsync(mid);
    if (m == null) return Results.NotFound(new { error = "Không tìm thấy hội viên" });
    var tx = await svc.RecordServiceTurnAsync(m.Id, dto.Qty, dto.RefNo);
    var member = await svc.GetAsync(m.Id);
    return Results.Ok(new { memberCode = member!.Code, qtyVisit = tx.QtyVisit, qtyVisitAvail = member.QtyVisitAvail, points = member.Points });
});

// API tích điểm tiêu dùng dịch vụ (DealPointType=CONSUMPTION): quy đổi doanh thu RO thành điểm theo chính sách hạng.
app.MapPost("/api/consumption", async (ConsumptionDto dto, ILoyaltyService svc) =>
{
    var m = dto.Phone is { Length: > 0 } p ? await svc.GetByPhoneAsync(p) : null;
    if (m == null && dto.MemberId is { } mid) m = await svc.GetAsync(mid);
    if (m == null) return Results.NotFound(new { error = "Không tìm thấy hội viên" });
    if (dto.Amount <= 0) return Results.BadRequest(new { error = "Doanh thu dịch vụ phải > 0." });
    var tx = await svc.RecordConsumptionAsync(m.Id, dto.Amount, dto.RefNo);
    var member = await svc.GetAsync(m.Id);
    return Results.Ok(new { memberCode = member!.Code, earned = tx.Points, amount = tx.AmountChTotal, balance = member.Points, lifetime = member.LifetimePoints, qtyVisit = member.QtyVisitAvail, rank = member.RankTier?.Name });
});

// API chiết khấu dịch vụ (DealPointType=DISCOUNTRO): áp % chiết khấu theo hạng lên doanh thu dịch vụ.
app.MapPost("/api/discount", async (DiscountDto dto, ILoyaltyService svc) =>
{
    var m = dto.Phone is { Length: > 0 } p ? await svc.GetByPhoneAsync(p) : null;
    if (m == null && dto.MemberId is { } mid) m = await svc.GetAsync(mid);
    if (m == null) return Results.NotFound(new { error = "Không tìm thấy hội viên" });
    if (dto.Amount <= 0) return Results.BadRequest(new { error = "Doanh thu dịch vụ phải > 0." });
    var tx = await svc.ApplyServiceDiscountAsync(m.Id, dto.Amount, dto.RefNo);
    return Results.Ok(new { memberCode = m.Code, rank = tx.CardTypeApply?.Name, rate = tx.PolicyDiscountRate, amount = tx.AmountForDC, discount = tx.DiscountAmount });
});

// API tặng điểm voucher xe mới (DealPointType=VOUCHERXM): cộng điểm voucher cho hội viên mua xe mới.
app.MapPost("/api/voucher/award", async (VoucherAwardDto dto, ILoyaltyService svc) =>
{
    var m = dto.Phone is { Length: > 0 } p ? await svc.GetByPhoneAsync(p) : null;
    if (m == null && dto.MemberId is { } mid) m = await svc.GetAsync(mid);
    if (m == null) return Results.NotFound(new { error = "Không tìm thấy hội viên" });
    if (dto.Points <= 0) return Results.BadRequest(new { error = "Điểm voucher phải > 0." });
    var tx = await svc.AwardVoucherAsync(m.Id, dto.Points, dto.VoucherCode, dto.RefNo, dto.Expiry);
    var member = await svc.GetAsync(m.Id);
    return Results.Ok(new { memberCode = member!.Code, awarded = tx.Points, voucherBalance = member.PointVoucher });
});

// API tặng điểm mua xe mới (DealPointType=SALES): cộng điểm thưởng mua xe cho hội viên.
app.MapPost("/api/sales/award", async (BuyNewCarDto dto, ILoyaltyService svc) =>
{
    var m = dto.Phone is { Length: > 0 } p ? await svc.GetByPhoneAsync(p) : null;
    if (m == null && dto.MemberId is { } mid) m = await svc.GetAsync(mid);
    if (m == null) return Results.NotFound(new { error = "Không tìm thấy hội viên" });
    try
    {
        var tx = await svc.AwardBuyNewCarAsync(m.Id, dto.Points, dto.RefNo);
        var member = await svc.GetAsync(m.Id);
        return Results.Ok(new { memberCode = member!.Code, awarded = tx.Points, balance = member.Points, lifetime = member.LifetimePoints, rank = member.RankTier?.Name });
    }
    catch (Exception ex) { return Results.BadRequest(new { error = ex.Message }); }
});

// API tặng điểm khuyến mại bán hàng (DealPointType=KMBH): HTV tặng thêm điểm cho hội viên mua xe khuyến mại.
app.MapPost("/api/kmbh/award", async (KmbhDto dto, ILoyaltyService svc) =>
{
    var m = dto.Phone is { Length: > 0 } p ? await svc.GetByPhoneAsync(p) : null;
    if (m == null && dto.MemberId is { } mid) m = await svc.GetAsync(mid);
    if (m == null) return Results.NotFound(new { error = "Không tìm thấy hội viên" });
    try
    {
        var tx = await svc.AwardKmbhAsync(m.Id, dto.Points, dto.RefNo);
        var member = await svc.GetAsync(m.Id);
        return Results.Ok(new { memberCode = member!.Code, awarded = tx.Points, balance = member.Points, lifetime = member.LifetimePoints, rank = member.RankTier?.Name });
    }
    catch (Exception ex) { return Results.BadRequest(new { error = ex.Message }); }
});

// API sử dụng điểm voucher (DealPointType=VOUCHERSD): trừ điểm voucher khi hội viên quy đổi tại đại lý.
app.MapPost("/api/voucher/use", async (VoucherUseDto dto, ILoyaltyService svc) =>
{
    var m = dto.Phone is { Length: > 0 } p ? await svc.GetByPhoneAsync(p) : null;
    if (m == null && dto.MemberId is { } mid) m = await svc.GetAsync(mid);
    if (m == null) return Results.NotFound(new { error = "Không tìm thấy hội viên" });
    var (ok, msg) = await svc.UseVoucherAsync(m.Id, dto.Points, dto.VoucherCode, dto.RefNo);
    if (!ok) return Results.BadRequest(new { ok, error = msg });
    var member = await svc.GetAsync(m.Id);
    return Results.Ok(new { ok, msg, memberCode = member!.Code, voucherBalance = member.PointVoucher });
});

// API sử dụng điểm đổi ưu đãi (DealPointType=POINTUSE): trừ điểm khả dụng theo giá điểm của ưu đãi.
app.MapPost("/api/promotion/use", async (PromotionUseDto dto, ILoyaltyService svc) =>
{
    var m = dto.Phone is { Length: > 0 } p ? await svc.GetByPhoneAsync(p) : null;
    if (m == null && dto.MemberId is { } mid) m = await svc.GetAsync(mid);
    if (m == null) return Results.NotFound(new { error = "Không tìm thấy hội viên" });
    var (ok, msg) = await svc.UsePromotionAsync(m.Id, dto.PromotionId, dto.RefNo);
    if (!ok) return Results.BadRequest(new { ok, error = msg });
    var member = await svc.GetAsync(m.Id);
    return Results.Ok(new { ok, msg, memberCode = member!.Code, points = member.Points });
});

// API ghi nhận sử dụng ưu đãi không trừ điểm (DealPointType=PRPROGRAM): tracking số lần dùng ưu đãi, không đổi điểm.
app.MapPost("/api/promotion/record", async (PromotionRecordDto dto, ILoyaltyService svc) =>
{
    var m = dto.Phone is { Length: > 0 } p ? await svc.GetByPhoneAsync(p) : null;
    if (m == null && dto.MemberId is { } mid) m = await svc.GetAsync(mid);
    if (m == null) return Results.NotFound(new { error = "Không tìm thấy hội viên" });
    var (ok, msg) = await svc.RecordPromotionUseAsync(m.Id, dto.PromotionId, dto.Qty, dto.RefNo);
    if (!ok) return Results.BadRequest(new { ok, error = msg });
    var member = await svc.GetAsync(m.Id);
    return Results.Ok(new { ok, msg, memberCode = member!.Code, points = member.Points });
});

// API tích điểm xét hạng nhập tay (DealPointType=POINTINCREASE): cộng điểm xét hạng trong kỳ, không đổi điểm khả dụng.
app.MapPost("/api/pointincrease", async (PointIncreaseDto dto, ILoyaltyService svc) =>
{
    var m = dto.Phone is { Length: > 0 } p ? await svc.GetByPhoneAsync(p) : null;
    if (m == null && dto.MemberId is { } mid) m = await svc.GetAsync(mid);
    if (m == null) return Results.NotFound(new { error = "Không tìm thấy hội viên" });
    if (dto.RankPoints <= 0) return Results.BadRequest(new { error = "Điểm xét hạng phải > 0." });
    var tx = await svc.RecordPointIncreaseAsync(m.Id, dto.RankPoints, dto.RefNo);
    var member = await svc.GetAsync(m.Id);
    return Results.Ok(new { memberCode = member!.Code, rankPoints = tx.PointChRankTotal, pointCardRank = member.PointCardRank, points = member.Points, rank = member.RankTier?.Name });
});

// API tặng điểm mở thẻ mới (DealPointType=OPENCARD): tặng điểm chào mừng khi hội viên hoàn tất đăng ký.
app.MapPost("/api/opencard/award", async (OpenCardDto dto, ILoyaltyService svc) =>
{
    var m = dto.Phone is { Length: > 0 } p ? await svc.GetByPhoneAsync(p) : null;
    if (m == null && dto.MemberId is { } mid) m = await svc.GetAsync(mid);
    if (m == null) return Results.NotFound(new { error = "Không tìm thấy hội viên" });
    try
    {
        var tx = await svc.AwardOpenCardAsync(m.Id, dto.Points, dto.RefNo);
        var member = await svc.GetAsync(m.Id);
        return Results.Ok(new { memberCode = member!.Code, awarded = tx.Points, balance = member.Points, lifetime = member.LifetimePoints, rank = member.RankTier?.Name });
    }
    catch (Exception ex) { return Results.BadRequest(new { error = ex.Message }); }
});

// API vô hiệu hoá hội viên (Crd_Member_InActiveX): đặt MemberStatus=Cancel, huỷ thẻ, đặt điểm còn lại hết hạn cuối tháng.
app.MapPost("/api/member/inactive", async (InactiveDto dto, ILoyaltyService svc) =>
{
    var m = dto.Phone is { Length: > 0 } p ? await svc.GetByPhoneAsync(p) : null;
    if (m == null && dto.MemberId is { } mid) m = await svc.GetAsync(mid);
    if (m == null) return Results.NotFound(new { error = "Không tìm thấy hội viên" });
    var (ok, msg) = await svc.InactivateMemberAsync(m.Id, dto.Remark, dto.By);
    if (!ok) return Results.BadRequest(new { ok, error = msg });
    var member = await svc.GetAsync(m.Id);
    return Results.Ok(new { ok, msg, memberCode = member!.Code, status = member.Status.ToString(), cardStatus = member.CardStatus.ToString() });
});

// API điều chỉnh điểm hỗ trợ (DealPointType=SUPPORT): nhân viên hỗ trợ cộng/trừ điểm thủ công cho hội viên.
app.MapPost("/api/support/adjust", async (SupportAdjustDto dto, ILoyaltyService svc) =>
{
    var m = dto.Phone is { Length: > 0 } p ? await svc.GetByPhoneAsync(p) : null;
    if (m == null && dto.MemberId is { } mid) m = await svc.GetAsync(mid);
    if (m == null) return Results.NotFound(new { error = "Không tìm thấy hội viên" });
    if (dto.Points == 0) return Results.BadRequest(new { error = "Số điểm điều chỉnh phải khác 0." });
    try
    {
        var tx = await svc.AdjustPointsBySupportAsync(m.Id, dto.Points, dto.Reason, dto.RefNo);
        var member = await svc.GetAsync(m.Id);
        return Results.Ok(new { memberCode = member!.Code, adjusted = tx.Points, balance = member.Points, lifetime = member.LifetimePoints, rank = member.RankTier?.Name, reason = tx.FunctionRemark });
    }
    catch (Exception ex) { return Results.BadRequest(new { error = ex.Message }); }
});

// API tạo yêu cầu thay đổi thông tin hội viên (Crd_MemberChangeInfo_SaveX): PENDING, chờ duyệt.
app.MapPost("/api/changerequest/create", async (ChangeRequestCreateDto dto, ILoyaltyService svc) =>
{
    var m = dto.Phone is { Length: > 0 } p ? await svc.GetByPhoneAsync(p) : null;
    if (m == null && dto.MemberId is { } mid) m = await svc.GetAsync(mid);
    if (m == null) return Results.NotFound(new { error = "Không tìm thấy hội viên" });
    var details = (dto.Details ?? []).Select(d => (d.ColumnCode, d.ValueNew)).ToList();
    var (ok, msg, id) = await svc.CreateChangeRequestAsync(m.Id, dto.RequestType, dto.DlCode, dto.Remark, details);
    return ok ? Results.Ok(new { ok, msg, requestId = id }) : Results.BadRequest(new { ok, error = msg });
});

// API duyệt yêu cầu thay đổi (Crd_MemberChangeInfo_ApproveX): PENDING → APPROVE.
// Gate quyền duyệt (Crd_MemberChangeInfo_CheckApproveRight): chỉ HTV hoặc đại lý khớp Đơn vị duyệt đã khóa.
app.MapPost("/api/changerequest/approve", async (ChangeRequestActionDto dto, ILoyaltyService svc) =>
{
    var (ok, msg) = await svc.ApproveChangeRequestAsync(dto.RequestId, dto.RemarkHtv, dto.By, dto.DlcpCode);
    return ok ? Results.Ok(new { ok, msg }) : Results.BadRequest(new { ok, error = msg });
});

// API hoàn tất yêu cầu thay đổi (Crd_MemberChangeInfo_FinishX): APPROVE → FINISH, áp thay đổi vào hội viên.
app.MapPost("/api/changerequest/finish", async (ChangeRequestActionDto dto, ILoyaltyService svc) =>
{
    var (ok, msg) = await svc.FinishChangeRequestAsync(dto.RequestId, dto.RemarkHtv, dto.By, dto.DlcpCode);
    return ok ? Results.Ok(new { ok, msg }) : Results.BadRequest(new { ok, error = msg });
});

// API từ chối yêu cầu thay đổi (Crd_MemberChangeInfo_RejectX): PENDING/APPROVE → CANCEL.
app.MapPost("/api/changerequest/reject", async (ChangeRequestActionDto dto, ILoyaltyService svc) =>
{
    var (ok, msg) = await svc.RejectChangeRequestAsync(dto.RequestId, dto.RemarkHtv, dto.By, dto.DlcpCode);
    return ok ? Results.Ok(new { ok, msg }) : Results.BadRequest(new { ok, error = msg });
});

// API đơn vị duyệt đề nghị (Crd_MemberChangeInfo.ApproveDLCode): trả Đơn vị duyệt đã khóa ("HTV,<đại lý>")
// + cờ FlagApproveAF (user có quyền duyệt không) — dùng để bật/tắt nút Duyệt trên màn chi tiết.
app.MapGet("/api/changerequest/approveunit", async (int requestId, string? dlcpCode, ILoyaltyService svc) =>
{
    var (unit, flag) = await svc.ApproveUnitAsync(requestId, dlcpCode);
    return Results.Ok(new { approveUnit = unit, flagApproveAF = flag });
});

// API tạo yêu cầu đặc cách thẻ (Crd_Card_RequestExceptionX): sinh kỳ thẻ đặc cách PENDING + danh sách đại lý chỉ định.
app.MapPost("/api/cardexception/request", async (CardExceptionRequestDto dto, ILoyaltyService svc) =>
{
    var m = dto.Phone is { Length: > 0 } p ? await svc.GetByPhoneAsync(p) : null;
    if (m == null && dto.MemberId is { } mid) m = await svc.GetAsync(mid);
    if (m == null) return Results.NotFound(new { error = "Không tìm thấy hội viên" });
    var (ok, msg, id) = await svc.RequestCardExceptionAsync(m.Id, dto.DlCodeExceptionally, dto.Remark, dto.DealerCodes ?? []);
    return ok ? Results.Ok(new { ok, msg, cardExceptionId = id }) : Results.BadRequest(new { ok, error = msg });
});

// API duyệt yêu cầu đặc cách thẻ (Crd_Card_ApprExceptionX): huỷ kỳ thẻ cũ, kích hoạt kỳ thẻ đặc cách.
app.MapPost("/api/cardexception/approve", async (CardExceptionActionDto dto, ILoyaltyService svc) =>
{
    var (ok, msg) = await svc.ApproveCardExceptionAsync(dto.Id, dto.RemarkHtv, dto.By);
    return ok ? Results.Ok(new { ok, msg }) : Results.BadRequest(new { ok, error = msg });
});

// API từ chối yêu cầu đặc cách thẻ: PENDING → CANCEL.
app.MapPost("/api/cardexception/reject", async (CardExceptionActionDto dto, ILoyaltyService svc) =>
{
    var (ok, msg) = await svc.RejectCardExceptionAsync(dto.Id, dto.RemarkHtv, dto.By);
    return ok ? Results.Ok(new { ok, msg }) : Results.BadRequest(new { ok, error = msg });
});

// API tạo yêu cầu đăng ký hội viên mới (Req_MemberRegister_SaveX): đại lý gửi thông tin khách hàng + xe, PENDING.
app.MapPost("/api/memberregister/create", async (MemberRegisterCreateDto dto, ILoyaltyService svc) =>
{
    var req = new MemberRegister
    {
        DLCodeRegis = dto.DlCodeRegis, RegisterDate = dto.RegisterDate ?? DateTime.Now,
        VIN = dto.VIN, CarNo = dto.CarNo, TradeMarkName = dto.TradeMarkName, ModelName = dto.ModelName,
        CustomerName = dto.CustomerName ?? "", CustomerPhoneNo = dto.CustomerPhoneNo,
        CustomerDateOfBirth = dto.CustomerDateOfBirth, CustomerIDNo = dto.CustomerIDNo,
        CustomerEmail = dto.CustomerEmail, CustomerAddress = dto.CustomerAddress,
        GenderCode = dto.GenderCode, ProvinceName = dto.ProvinceName, DistrictName = dto.DistrictName,
        MemberNoIntro = dto.MemberNoIntro, Remark = dto.Remark
    };
    var (ok, msg, id) = await svc.CreateMemberRegisterAsync(req);
    return ok ? Results.Ok(new { ok, msg, requestId = id }) : Results.BadRequest(new { ok, error = msg });
});

// API duyệt yêu cầu đăng ký (Req_MemberRegister_ApprX): PENDING → APPROVE, chặn trùng CarNo/VIN.
app.MapPost("/api/memberregister/approve", async (MemberRegisterActionDto dto, ILoyaltyService svc) =>
{
    var (ok, msg) = await svc.ApproveMemberRegisterAsync(dto.Id, dto.Remark, dto.By);
    return ok ? Results.Ok(new { ok, msg }) : Results.BadRequest(new { ok, error = msg });
});

// API hoàn tất yêu cầu đăng ký (Req_MemberRegister_FinishX): APPROVE → FINISH, tạo hội viên mới.
app.MapPost("/api/memberregister/finish", async (MemberRegisterActionDto dto, ILoyaltyService svc) =>
{
    var (ok, msg) = await svc.FinishMemberRegisterAsync(dto.Id, dto.By);
    return ok ? Results.Ok(new { ok, msg }) : Results.BadRequest(new { ok, error = msg });
});

// API huỷ yêu cầu đăng ký (Req_MemberRegister_CancelX): PENDING/APPROVE → CANCEL.
app.MapPost("/api/memberregister/cancel", async (MemberRegisterActionDto dto, ILoyaltyService svc) =>
{
    var (ok, msg) = await svc.CancelMemberRegisterAsync(dto.Id, dto.Remark, dto.By);
    return ok ? Results.Ok(new { ok, msg }) : Results.BadRequest(new { ok, error = msg });
});

// API từ chối yêu cầu đăng ký (Req_MemberRegister_RejectX): APPROVE → REJECT, bắt buộc lý do.
app.MapPost("/api/memberregister/reject", async (MemberRegisterActionDto dto, ILoyaltyService svc) =>
{
    var (ok, msg) = await svc.RejectMemberRegisterAsync(dto.Id, dto.Remark, dto.By);
    return ok ? Results.Ok(new { ok, msg }) : Results.BadRequest(new { ok, error = msg });
});

// API liên kết Đại lý ↔ Hội viên (Map_QueryDealer_Member_Create): ghi nhận đại lý nào đăng ký/tra cứu hội viên nào.
app.MapPost("/api/dealerlink/create", async (DealerLinkDto dto, ILoyaltyService svc) =>
{
    var m = dto.Phone is { Length: > 0 } p ? await svc.GetByPhoneAsync(p) : null;
    if (m == null && dto.MemberId is { } mid) m = await svc.GetAsync(mid);
    if (m == null) return Results.NotFound(new { error = "Không tìm thấy hội viên" });
    var (ok, msg, id) = await svc.LinkDealerMemberAsync(dto.DlcpCode ?? "", m.Id, dto.NetworkId, dto.Remark, dto.By);
    return ok ? Results.Ok(new { ok, msg, linkId = id }) : Results.BadRequest(new { ok, error = msg });
});

// API danh sách liên kết Đại lý ↔ Hội viên (Map_QueryDealer_Member): lọc theo đại lý và/hoặc hội viên.
app.MapGet("/api/dealerlink", async (string? dlcpCode, int? memberId, ILoyaltyService svc) =>
{
    var list = await svc.DealerMemberLinksAsync(dlcpCode, memberId);
    return Results.Ok(list.Select(l => new
    {
        l.Id, l.DLCPCode, memberCode = l.Member?.Code, memberName = l.Member?.Name,
        l.NetworkID, l.QueryDate, l.Remark, l.FlagActive, l.CreatedAt, l.CreatedBy
    }));
});

// API tạo chương trình tặng điểm xe mới (Prm_CarNew_SaveX): đại lý/HTV cấu hình chương trình, PENDING.
app.MapPost("/api/prmcarnew/create", async (PrmCarNewCreateDto dto, ILoyaltyService svc) =>
{
    var prm = new PrmCarNew
    {
        PRMCNName = dto.Name ?? "", DLCPCode = dto.DlcpCode ?? "",
        EffDateStart = dto.EffDateStart ?? DateTime.Today, EffDateEnd = dto.EffDateEnd ?? new DateTime(9999, 12, 31),
        FlagAllModel = dto.FlagAllModel, PointValAllModel = dto.PointValAllModel, Remark = dto.Remark,
        Specs = (dto.Specs ?? []).Select((s, i) => new PrmCarNewSpec { Idx = i + 1, ModelCode = s.ModelCode, PointVal = s.PointVal }).ToList()
    };
    var (ok, msg, id) = await svc.CreatePrmCarNewAsync(prm);
    return ok ? Results.Ok(new { ok, msg, id }) : Results.BadRequest(new { ok, error = msg });
});

// API duyệt chương trình tặng điểm xe mới (Prm_CarNew_ApprX): PENDING → APPROVE.
app.MapPost("/api/prmcarnew/approve", async (PrmCarNewActionDto dto, ILoyaltyService svc) =>
{
    var (ok, msg) = await svc.ApprovePrmCarNewAsync(dto.Id, dto.Remark, dto.By);
    return ok ? Results.Ok(new { ok, msg }) : Results.BadRequest(new { ok, error = msg });
});

// API hoàn tất chương trình tặng điểm xe mới (Prm_CarNew_FinishX): APPROVE → FINISH, chương trình có hiệu lực.
app.MapPost("/api/prmcarnew/finish", async (PrmCarNewActionDto dto, ILoyaltyService svc) =>
{
    var (ok, msg) = await svc.FinishPrmCarNewAsync(dto.Id, dto.Remark, dto.By);
    return ok ? Results.Ok(new { ok, msg }) : Results.BadRequest(new { ok, error = msg });
});

// API huỷ chương trình tặng điểm xe mới (Prm_CarNew_CancelX): PENDING/APPROVE → CANCEL.
app.MapPost("/api/prmcarnew/cancel", async (PrmCarNewActionDto dto, ILoyaltyService svc) =>
{
    var (ok, msg) = await svc.CancelPrmCarNewAsync(dto.Id, dto.Remark, dto.By);
    return ok ? Results.Ok(new { ok, msg }) : Results.BadRequest(new { ok, error = msg });
});

// API tra chương trình tặng điểm xe mới đang hiệu lực (Prm_CarNew_CalcPrmX) theo đại lý + dòng xe.
app.MapGet("/api/prmcarnew/calc", async (string dlcpCode, string? modelCode, ILoyaltyService svc) =>
{
    var p = await svc.CalcPrmCarNewAsync(dlcpCode, modelCode);
    if (p == null) return Results.NotFound(new { error = "Không có chương trình tặng điểm xe mới đang hiệu lực." });
    return Results.Ok(new { p.PRMCNCodeSys, p.PRMCNName, p.DLCPCode, p.FlagAllModel, p.PointValAllModel, p.EffDateStart, p.EffDateEnd });
});

// API tạo chương trình tặng điểm giới thiệu mua xe (Prm_CarRecommend_SaveX): đại lý/HTV cấu hình chương trình, PENDING.
app.MapPost("/api/prmcarrecommend/create", async (PrmCarRecommendCreateDto dto, ILoyaltyService svc) =>
{
    var prm = new PrmCarRecommend
    {
        PRMCRName = dto.Name ?? "", DLCPCode = dto.DlcpCode ?? "",
        EffDateStart = dto.EffDateStart ?? DateTime.Today, EffDateEnd = dto.EffDateEnd ?? new DateTime(9999, 12, 31),
        FlagAllModel = dto.FlagAllModel, PointValAllModel = dto.PointValAllModel, Remark = dto.Remark,
        Specs = (dto.Specs ?? []).Select((s, i) => new PrmCarRecommendSpec { Idx = i + 1, ModelCode = s.ModelCode }).ToList(),
        Details = (dto.Specs ?? []).Select((s, i) => new PrmCarRecommendDtl { Idx = i + 1, PointVal = s.PointVal }).ToList()
    };
    var (ok, msg, id) = await svc.CreatePrmCarRecommendAsync(prm);
    return ok ? Results.Ok(new { ok, msg, id }) : Results.BadRequest(new { ok, error = msg });
});

// API duyệt chương trình tặng điểm giới thiệu mua xe (Prm_CarRecommend_ApprX): PENDING → APPROVE.
app.MapPost("/api/prmcarrecommend/approve", async (PrmCarRecommendActionDto dto, ILoyaltyService svc) =>
{
    var (ok, msg) = await svc.ApprovePrmCarRecommendAsync(dto.Id, dto.Remark, dto.By);
    return ok ? Results.Ok(new { ok, msg }) : Results.BadRequest(new { ok, error = msg });
});

// API hoàn tất chương trình tặng điểm giới thiệu mua xe (Prm_CarRecommend_FinishX): APPROVE → FINISH, chương trình có hiệu lực.
app.MapPost("/api/prmcarrecommend/finish", async (PrmCarRecommendActionDto dto, ILoyaltyService svc) =>
{
    var (ok, msg) = await svc.FinishPrmCarRecommendAsync(dto.Id, dto.Remark, dto.By);
    return ok ? Results.Ok(new { ok, msg }) : Results.BadRequest(new { ok, error = msg });
});

// API huỷ chương trình tặng điểm giới thiệu mua xe (Prm_CarRecommend_CancelX): PENDING/APPROVE → CANCEL.
app.MapPost("/api/prmcarrecommend/cancel", async (PrmCarRecommendActionDto dto, ILoyaltyService svc) =>
{
    var (ok, msg) = await svc.CancelPrmCarRecommendAsync(dto.Id, dto.Remark, dto.By);
    return ok ? Results.Ok(new { ok, msg }) : Results.BadRequest(new { ok, error = msg });
});

// API tra chương trình tặng điểm giới thiệu mua xe đang hiệu lực (Prm_CarRecommend_CalcPrmX) theo đại lý + dòng xe.
app.MapGet("/api/prmcarrecommend/calc", async (string dlcpCode, string? modelCode, ILoyaltyService svc) =>
{
    var p = await svc.CalcPrmCarRecommendAsync(dlcpCode, modelCode);
    if (p == null) return Results.NotFound(new { error = "Không có chương trình tặng điểm giới thiệu mua xe đang hiệu lực." });
    return Results.Ok(new { p.PRMCRCodeSys, p.PRMCRName, p.DLCPCode, p.FlagAllModel, p.PointValAllModel, p.EffDateStart, p.EffDateEnd });
});

// API tính điểm khuyến mại bán hàng Creta (WA_Crd_MemberRegis_CalcPointBuyCreta): kiểm tra điều kiện
// (dòng xe + hạng thẻ + ngày giao xe + CCCD + chưa áp dụng) và trả về số điểm HTV tặng (0 nếu không đủ).
app.MapPost("/api/creta/calc", async (CretaCalcDto dto, ILoyaltyService svc) =>
{
    var r = await svc.CalcPointBuyCretaAsync(dto.ModelCode, dto.CardTypeUse, dto.DeliveryDate, dto.IdCardNo, dto.DealNo, dto.MemberId);
    return Results.Ok(new { eligible = r.Eligible, pointBuyCreta = r.PointBuyCreta, reason = r.Reason });
});

// API tra cứu hội viên cho DMS (Crd_MemberController.GetDetailForDMS): tìm hội viên theo biển số (CarNo)
// hoặc số khung (VIN), kèm FlagIsDLQuery (đại lý đã đăng ký/tra cứu hội viên này chưa).
app.MapGet("/api/member/dms-lookup", async (string? carNo, string? vin, string? dlcpCode, ILoyaltyService svc) =>
{
    var r = await svc.GetDetailForDmsAsync(carNo, vin, dlcpCode);
    if (r.Member == null) return Results.NotFound(new { error = "Không tìm thấy hội viên theo biển số/VIN." });
    var m = r.Member;
    return Results.Ok(new
    {
        memberCode = m.Code, memberName = m.Name, m.Phone, m.CarNo, m.VIN,
        rank = m.RankTier?.Name, points = m.Points, lifetime = m.LifetimePoints,
        qtyVisitAvail = m.QtyVisitAvail, status = m.Status.ToString(),
        flagIsDLQuery = r.FlagIsDLQuery
    });
});

// API liệt kê chính sách đối tượng tích điểm dịch vụ (Mst_PolicyExpenseType): cấu hình cho từng loại chi phí
// dịch vụ (LOCAL/ROINSURANCE/ROREPAIR/ROWARRANTY) xem có tích điểm / tính điểm xét hạng / tính lượt dịch vụ / áp chiết khấu.
app.MapGet("/api/expensetypepolicy", async (ILoyaltyService svc) =>
{
    var list = await svc.ExpenseTypePoliciesAsync();
    return Results.Ok(list.Select(p => new
    {
        p.Id, p.PolicyExpenseTypeNo, p.ExpenseType, p.ExpenseTypeNameActual,
        p.FlagPoint, p.FlagPointRank, p.FlagCountService, p.FlagDiscount,
        p.AmountRate, p.MaxRankReviewPoint, p.MaxAccumulationPoint, p.DiscountRate, p.IsActive, p.Remark
    }));
});

// API lưu chính sách đối tượng tích điểm dịch vụ (Mst_PolicyExpenseType_SaveX): upsert theo ExpenseType,
// validate DiscountRate 0..100 + luật chéo FlagDiscount=0 ⇒ DiscountRate=0.
app.MapPost("/api/expensetypepolicy/save", async (ExpenseTypePolicyDto dto, ILoyaltyService svc) =>
{
    var policy = new ExpenseTypePolicy
    {
        PolicyExpenseTypeNo = dto.PolicyExpenseTypeNo ?? "", ExpenseType = dto.ExpenseType ?? "",
        ExpenseTypeNameActual = dto.ExpenseTypeNameActual ?? "",
        FlagPoint = dto.FlagPoint, FlagPointRank = dto.FlagPointRank, FlagCountService = dto.FlagCountService,
        FlagDiscount = dto.FlagDiscount, AmountRate = dto.AmountRate, MaxRankReviewPoint = dto.MaxRankReviewPoint,
        MaxAccumulationPoint = dto.MaxAccumulationPoint, DiscountRate = dto.DiscountRate, Remark = dto.Remark
    };
    var (ok, msg, id) = await svc.SaveExpenseTypePolicyAsync(policy);
    return ok ? Results.Ok(new { ok, msg, id }) : Results.BadRequest(new { ok, error = msg });
});

// API tạo chương trình tặng điểm voucher xe mới (Prm_VoucherNewCar_SaveX): HTV cấu hình chương trình, PENDING.
app.MapPost("/api/prmvouchernewcar/create", async (PrmVoucherNewCarCreateDto dto, ILoyaltyService svc) =>
{
    var prm = new PrmVoucherNewCar
    {
        PrmVoucherName = dto.Name ?? "",
        QtyDayLimitFDlvDate = dto.QtyDayLimitFDlvDate, ValidityPeriod = dto.ValidityPeriod,
        EffDateStart = dto.EffDateStart ?? DateTime.Today, EffDateEnd = dto.EffDateEnd ?? new DateTime(9999, 12, 31),
        FlagAllModel = dto.FlagAllModel, PointVoucherAllModel = dto.PointVoucherAllModel,
        PointUseLimitAllModel = dto.PointUseLimitAllModel, Remark = dto.Remark,
        Specs = (dto.Specs ?? []).Select((s, i) => new PrmVoucherNewCarSpec { Idx = i + 1, ModelCode = s.ModelCode }).ToList(),
        Details = (dto.Specs ?? []).Select((s, i) => new PrmVoucherNewCarDtl { Idx = i + 1, PointVoucher = s.PointVoucher, PointUseLimit = s.PointUseLimit }).ToList()
    };
    var (ok, msg, id) = await svc.CreatePrmVoucherNewCarAsync(prm);
    return ok ? Results.Ok(new { ok, msg, id }) : Results.BadRequest(new { ok, error = msg });
});

// API duyệt chương trình tặng điểm voucher xe mới (Prm_VoucherNewCar_ApprX): PENDING → APPROVE.
app.MapPost("/api/prmvouchernewcar/approve", async (PrmVoucherNewCarActionDto dto, ILoyaltyService svc) =>
{
    var (ok, msg) = await svc.ApprovePrmVoucherNewCarAsync(dto.Id, dto.Remark, dto.By);
    return ok ? Results.Ok(new { ok, msg }) : Results.BadRequest(new { ok, error = msg });
});

// API hoàn tất chương trình tặng điểm voucher xe mới (Prm_VoucherNewCar_FinishX): APPROVE → FINISH, chương trình có hiệu lực.
app.MapPost("/api/prmvouchernewcar/finish", async (PrmVoucherNewCarActionDto dto, ILoyaltyService svc) =>
{
    var (ok, msg) = await svc.FinishPrmVoucherNewCarAsync(dto.Id, dto.Remark, dto.By);
    return ok ? Results.Ok(new { ok, msg }) : Results.BadRequest(new { ok, error = msg });
});

// API huỷ chương trình tặng điểm voucher xe mới (Prm_VoucherNewCar_CancelX): PENDING/APPROVE → CANCEL.
app.MapPost("/api/prmvouchernewcar/cancel", async (PrmVoucherNewCarActionDto dto, ILoyaltyService svc) =>
{
    var (ok, msg) = await svc.CancelPrmVoucherNewCarAsync(dto.Id, dto.Remark, dto.By);
    return ok ? Results.Ok(new { ok, msg }) : Results.BadRequest(new { ok, error = msg });
});

// API tra chương trình tặng điểm voucher xe mới đang hiệu lực (Prm_VoucherNewCar_CalcPrmX) theo dòng xe.
app.MapGet("/api/prmvouchernewcar/calc", async (string? modelCode, ILoyaltyService svc) =>
{
    var p = await svc.CalcPrmVoucherNewCarAsync(modelCode);
    if (p == null) return Results.NotFound(new { error = "Không có chương trình tặng điểm voucher xe mới đang hiệu lực." });
    return Results.Ok(new { p.PrmVoucherCode, p.PrmVoucherName, p.FlagAllModel, p.PointVoucherAllModel, p.PointUseLimitAllModel, p.ValidityPeriod, p.EffDateStart, p.EffDateEnd });
});

// API danh sách hội viên đang trong vòng đời duyệt ĐĂNG KÝ (Crd_Member.RegisStatus): PENDING → APPROVE1 → APPROVE2 → FINISH.
app.MapGet("/api/memberapproval", async (RegisStatus? status, ILoyaltyService svc) =>
{
    var list = await svc.MemberApprovalsAsync(status);
    return Results.Ok(list.Select(m => new
    {
        m.Id, memberCode = m.Code, memberName = m.Name, m.Phone, m.DLCodeRegis,
        regisStatus = m.RegisStatus.ToString(), memberStatus = m.Status.ToString(),
        rank = m.RankTier?.Name, m.RegisAppr1At, m.RegisAppr1By, m.RegisAppr2At, m.RegisAppr2By,
        m.RegisFinishAt, m.RegisFinishBy, m.MemberActiveDate
    }));
});

// API duyệt đăng ký hội viên bởi ĐẠI LÝ (Crd_Member_ApproveByDealerX): PENDING → APPROVE1.
app.MapPost("/api/memberapproval/approvebydealer", async (MemberApprovalActionDto dto, ILoyaltyService svc) =>
{
    var (ok, msg) = await svc.ApproveMemberByDealerAsync(dto.MemberId, dto.Remark, dto.By);
    return ok ? Results.Ok(new { ok, msg }) : Results.BadRequest(new { ok, error = msg });
});

// API duyệt đăng ký hội viên bởi HTV (Crd_Member_ApproveX): APPROVE1 → APPROVE2.
app.MapPost("/api/memberapproval/approve", async (MemberApprovalActionDto dto, ILoyaltyService svc) =>
{
    var (ok, msg) = await svc.ApproveMemberAsync(dto.MemberId, dto.Remark, dto.By);
    return ok ? Results.Ok(new { ok, msg }) : Results.BadRequest(new { ok, error = msg });
});

// API hoàn tất đăng ký hội viên (Crd_Member_FinishX): APPROVE2 → FINISH, kích hoạt hội viên + thẻ + liên kết đại lý.
app.MapPost("/api/memberapproval/finish", async (MemberApprovalActionDto dto, ILoyaltyService svc) =>
{
    var (ok, msg) = await svc.FinishMemberAsync(dto.MemberId, dto.Remark, dto.By);
    return ok ? Results.Ok(new { ok, msg }) : Results.BadRequest(new { ok, error = msg });
});

// API tính toán thẻ / lộ trình lên hạng (Crd_Card_Calc): hạng hiện tại + còn thiếu gì để lên hạng kế tiếp.
app.MapGet("/api/card/calc", async (int memberId, ILoyaltyService svc) =>
{
    var r = await svc.CardCalcAsync(memberId);
    if (r == null) return Results.NotFound(new { error = "Không tìm thấy hội viên" });
    return Results.Ok(new
    {
        r.MemberCode, r.MemberName, r.RankName, r.RankValue,
        r.PointAvailNow, r.QtyVisitAvail,
        r.NextRankName, r.NextRankValue, r.PointUpBegin, r.QtyVisitUpBegin,
        r.RevenueMiss, r.QtyVisitMiss, r.IsMaxRank
    });
});

app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.Run();

record EarnDto(string? Phone, int? MemberId, decimal Amount, string? RefNo);
record RegisterOrgDto(string Name);
record IntroDto(string? Phone, int? MemberId);
record BuyNewCarDto(string? Phone, int? MemberId, int? Points, string? RefNo);
record KmbhDto(string? Phone, int? MemberId, int? Points, string? RefNo);
record OpenCardDto(string? Phone, int? MemberId, int? Points, string? RefNo);
record ServiceTurnDto(string? Phone, int? MemberId, int Qty, string? RefNo);
record ConsumptionDto(string? Phone, int? MemberId, decimal Amount, string? RefNo);
record DiscountDto(string? Phone, int? MemberId, decimal Amount, string? RefNo);
record VoucherAwardDto(string? Phone, int? MemberId, int Points, string? VoucherCode, string? RefNo, DateTime? Expiry);
record VoucherUseDto(string? Phone, int? MemberId, int Points, string? VoucherCode, string? RefNo);
record PromotionUseDto(string? Phone, int? MemberId, int PromotionId, string? RefNo);
record PromotionRecordDto(string? Phone, int? MemberId, int PromotionId, int Qty, string? RefNo);
record PointIncreaseDto(string? Phone, int? MemberId, int RankPoints, string? RefNo);
record InactiveDto(string? Phone, int? MemberId, string? Remark, string? By);
record SupportAdjustDto(string? Phone, int? MemberId, int Points, string? Reason, string? RefNo);
record ChangeRequestDetailDto(string ColumnCode, string? ValueNew);
record ChangeRequestCreateDto(string? Phone, int? MemberId, ChangeRequestType RequestType, string? DlCode, string? Remark, List<ChangeRequestDetailDto>? Details);
record ChangeRequestActionDto(int RequestId, string? RemarkHtv, string? By, string? DlcpCode);
record CardExceptionRequestDto(string? Phone, int? MemberId, string? DlCodeExceptionally, string? Remark, List<string>? DealerCodes);
record CardExceptionActionDto(int Id, string? RemarkHtv, string? By);
record MemberRegisterCreateDto(string? DlCodeRegis, DateTime? RegisterDate, string? VIN, string? CarNo, string? TradeMarkName, string? ModelName, string? CustomerName, string? CustomerPhoneNo, DateTime? CustomerDateOfBirth, string? CustomerIDNo, string? CustomerEmail, string? CustomerAddress, string? GenderCode, string? ProvinceName, string? DistrictName, string? MemberNoIntro, string? Remark);
record MemberRegisterActionDto(int Id, string? Remark, string? By);
record DealerLinkDto(string? DlcpCode, string? Phone, int? MemberId, int NetworkId, string? Remark, string? By);
record PrmCarNewSpecDto(string ModelCode, int PointVal);
record PrmCarNewCreateDto(string? Name, string? DlcpCode, DateTime? EffDateStart, DateTime? EffDateEnd, bool FlagAllModel, int PointValAllModel, string? Remark, List<PrmCarNewSpecDto>? Specs);
record PrmCarNewActionDto(int Id, string? Remark, string? By);
record PrmCarRecommendSpecDto(string ModelCode, int PointVal);
record PrmCarRecommendCreateDto(string? Name, string? DlcpCode, DateTime? EffDateStart, DateTime? EffDateEnd, bool FlagAllModel, int PointValAllModel, string? Remark, List<PrmCarRecommendSpecDto>? Specs);
record PrmCarRecommendActionDto(int Id, string? Remark, string? By);
record CretaCalcDto(string? ModelCode, string? CardTypeUse, DateTime? DeliveryDate, string? IdCardNo, string? DealNo, int? MemberId);
record ExpenseTypePolicyDto(string? PolicyExpenseTypeNo, string? ExpenseType, string? ExpenseTypeNameActual, bool FlagPoint, bool FlagPointRank, bool FlagCountService, bool FlagDiscount, decimal AmountRate, decimal MaxRankReviewPoint, decimal MaxAccumulationPoint, decimal DiscountRate, string? Remark);
record PrmVoucherNewCarSpecDto(string ModelCode, int PointVoucher, int PointUseLimit);
record PrmVoucherNewCarCreateDto(string? Name, int QtyDayLimitFDlvDate, int ValidityPeriod, DateTime? EffDateStart, DateTime? EffDateEnd, bool FlagAllModel, int PointVoucherAllModel, int PointUseLimitAllModel, string? Remark, List<PrmVoucherNewCarSpecDto>? Specs);
record PrmVoucherNewCarActionDto(int Id, string? Remark, string? By);
record MemberApprovalActionDto(int MemberId, string? Remark, string? By);
