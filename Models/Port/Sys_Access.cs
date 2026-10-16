using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniLoyalty.Data;

namespace MiniLoyalty.Models.Port;

/// <summary>
/// Entity bảng Sys_Access — phân quyền truy cập chức năng hệ thống theo nhóm người dùng.
/// Nguồn model: idn.Loyalty.Common\Models\Sys_Access.cs:9-32
/// BIZ: idn.Loyalty.Biz\System.cs:290-512 (Sys_Access_Get)
/// </summary>
public class Sys_Access
{
    // 4 cột cơ sở trong bảng DB Sys_Access (Sys_Access.cs:11-17)
    public string GroupCode { get; set; } = "";
    public string ObjectCode { get; set; } = "";
    public DateTime? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }

    // 6 cột mở rộng (join từ Sys_Object) — GAP: Bảng Sys_Object chưa có ở hệ thống đích (Sys_Access.cs:20-30)
    [NotMapped]
    public string? so_ObjectCode { get; set; }

    [NotMapped]
    public string? so_ObjectName { get; set; }

    [NotMapped]
    public string? so_ServiceCode { get; set; }

    [NotMapped]
    public string? so_ObjectType { get; set; }

    [NotMapped]
    public string? so_FlagExecModal { get; set; }

    [NotMapped]
    public string? so_FlagActive { get; set; }
}

/// <summary>
/// Cấu hình Entity Framework cho bảng Sys_Access.
/// Khoá chính gồm 2 cột: GroupCode, ObjectCode (System.cs:414-415).
/// </summary>
public class Sys_AccessConfig : IEntityTypeConfiguration<Sys_Access>
{
    public void Configure(EntityTypeBuilder<Sys_Access> b)
    {
        b.ToTable("Sys_Access");
        b.HasKey(x => new { x.GroupCode, x.ObjectCode });
    }
}

/// <summary>
/// Dữ liệu khởi tạo (seed) cho bảng Sys_Access.
/// Order => 30 theo yêu cầu đề bài #300. Idempotent.
/// </summary>
public class Sys_AccessSeeder : IPortSeeder
{
    public int Order => 48; // DEMO, idempotent theo đề bài #300

    public async Task SeedAsync(AppDbContext db)
    {
        if (await db.Set<Sys_Access>().AnyAsync()) return;

        var now = new DateTime(2026, 10, 10, 10, 0, 0, DateTimeKind.Utc);
        db.Set<Sys_Access>().AddRange(
            new Sys_Access { GroupCode = "G01", ObjectCode = "MNU01", LogLUBy = "seed", LogLUDTimeUTC = now },
            new Sys_Access { GroupCode = "G01", ObjectCode = "MNU02", LogLUBy = "seed", LogLUDTimeUTC = now },
            new Sys_Access { GroupCode = "G02", ObjectCode = "MNU01", LogLUBy = "seed", LogLUDTimeUTC = now },
            new Sys_Access { GroupCode = "G03", ObjectCode = "OBJ01", LogLUBy = "seed", LogLUDTimeUTC = now }
        );
        await db.SaveChangesAsync();
    }
}
