using Microsoft.EntityFrameworkCore;
using MiniLoyalty.Data;
using MiniLoyalty.Models;
using MiniLoyalty.Services;
using Serilog;

// Npgsql: DateTime (Kind Local/Unspecified) '' timestamp without time zone (khong phai timestamptz)
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

FleetObs.ConfigureLogger("miniloyalty");
var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();
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
builder.Services.AddFleetObs();
builder.Services.AddControllersWithViews();

var app = builder.Build();
using (var scope = app.Services.CreateScope())
    await Seeder.SeedAsync(scope.ServiceProvider.GetRequiredService<AppDbContext>());

app.UseFleetObs();

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

// API tích hợp: mua xe / sửa xe → tự tạo hội viên nếu chưa có (theo SĐT) rồi tích điểm.
app.MapPost("/api/ext/auto-earn", async (AutoEarnDto dto, ILoyaltyService svc) =>
{
    if (string.IsNullOrWhiteSpace(dto.Phone)) return Results.BadRequest(new { error = "Cần số điện thoại." });
    if (dto.Amount <= 0) return Results.BadRequest(new { error = "Số tiền phải > 0." });
    var m = await svc.GetByPhoneAsync(dto.Phone.Trim());
    var enrolled = false;
    if (m == null)
    {
        var id = await svc.CreateAsync(new Member { Name = dto.Name ?? "Khách hàng", Phone = dto.Phone.Trim() });
        m = await svc.GetAsync(id); enrolled = true;
    }
    var tx = await svc.EarnFromPurchaseAsync(m!.Id, dto.Amount, dto.RefNo);
    var member = await svc.GetAsync(m.Id);
    return Results.Ok(new { memberCode = member!.Code, enrolled, earned = tx.Points, balance = member.Points, rank = member.RankTier?.Name });
});

app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.Run();

record EarnDto(string? Phone, int? MemberId, decimal Amount, string? RefNo);
record AutoEarnDto(string? Phone, string? Name, decimal Amount, string? RefNo);
record RegisterOrgDto(string Name);
