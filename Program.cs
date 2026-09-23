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
app.MapPost("/api/changerequest/approve", async (ChangeRequestActionDto dto, ILoyaltyService svc) =>
{
    var (ok, msg) = await svc.ApproveChangeRequestAsync(dto.RequestId, dto.RemarkHtv, dto.By);
    return ok ? Results.Ok(new { ok, msg }) : Results.BadRequest(new { ok, error = msg });
});

// API hoàn tất yêu cầu thay đổi (Crd_MemberChangeInfo_FinishX): APPROVE → FINISH, áp thay đổi vào hội viên.
app.MapPost("/api/changerequest/finish", async (ChangeRequestActionDto dto, ILoyaltyService svc) =>
{
    var (ok, msg) = await svc.FinishChangeRequestAsync(dto.RequestId, dto.RemarkHtv, dto.By);
    return ok ? Results.Ok(new { ok, msg }) : Results.BadRequest(new { ok, error = msg });
});

// API từ chối yêu cầu thay đổi (Crd_MemberChangeInfo_RejectX): PENDING/APPROVE → CANCEL.
app.MapPost("/api/changerequest/reject", async (ChangeRequestActionDto dto, ILoyaltyService svc) =>
{
    var (ok, msg) = await svc.RejectChangeRequestAsync(dto.RequestId, dto.RemarkHtv, dto.By);
    return ok ? Results.Ok(new { ok, msg }) : Results.BadRequest(new { ok, error = msg });
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
record ChangeRequestActionDto(int RequestId, string? RemarkHtv, string? By);
