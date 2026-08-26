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
builder.Services.AddScoped<ILoyaltyService, LoyaltyService>();
builder.Services.AddControllersWithViews();

var app = builder.Build();
using (var scope = app.Services.CreateScope())
    await Seeder.SeedAsync(scope.ServiceProvider.GetRequiredService<AppDbContext>());

app.UseStaticFiles();
app.MapGet("/healthz", () => "ok");

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

app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.Run();

record EarnDto(string? Phone, int? MemberId, decimal Amount, string? RefNo);
