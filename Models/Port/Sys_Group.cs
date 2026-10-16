using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniLoyalty.Data;

namespace MiniLoyalty.Models.Port;

// ===========================================================================================
// PORT 1:1 từ dự án nguồn 2022.7.Loyalty — bảng Sys_Group và Sys_UserInGroup (#307).
//
// Nguồn tra cứu (chỉ đọc):
//   - Model cột Sys_Group: 12.Dev.Common/idn.Loyalty.Common/Models/Sys_Group.cs:9-23 (6 cột)
//   - Model cột Sys_UserInGroup: 12.Dev.Common/idn.Loyalty.Common/Models/Sys_UserInGroup.cs:9-26
//   - Biz join/query: 11.BackEnd/V10/idn.Loyalty.Biz/System.cs:2304-2363
//
// ĐẶC ĐIỂM BẢNG Sys_Group (đúng 6 cột):
//   GroupCode (PK, string not null), GroupName (string?), FlagActive (string?),
//   LogLUDTimeUTC (DateTime?), LogLUBy (string?), MST (string?).
//
// ĐẶC ĐIỂM BẢNG Sys_UserInGroup (phục vụ đếm QtyUserInGroup):
//   UserCode (PK ghép, string not null), GroupCode (PK ghép, string not null).
// ===========================================================================================

/// <summary>
/// Bảng nhóm người dùng hệ thống (Sys_Group) - port 1:1.
/// Model nguồn: Sys_Group.cs:9-23. PK = GroupCode.
/// Gồm 6 cột cơ sở.
/// </summary>
public class Sys_Group
{
    /// <summary>Mã nhóm người dùng (PK) - Sys_Group.cs:11</summary>
    public string GroupCode { get; set; } = "";

    /// <summary>Tên nhóm người dùng - Sys_Group.cs:13</summary>
    public string? GroupName { get; set; }

    /// <summary>Trạng thái hoạt động ('1': hoạt động, '0': không hoạt động) - Sys_Group.cs:15</summary>
    public string? FlagActive { get; set; }

    /// <summary>Thời gian cập nhật cuối UTC - Sys_Group.cs:17</summary>
    public DateTime? LogLUDTimeUTC { get; set; }

    /// <summary>Người cập nhật cuối - Sys_Group.cs:19</summary>
    public string? LogLUBy { get; set; }

    /// <summary>Mã số thuế - Sys_Group.cs:21</summary>
    public string? MST { get; set; }
}

/// <summary>Cấu hình EF cho <see cref="Sys_Group"/>.</summary>
public class Sys_GroupConfig : IEntityTypeConfiguration<Sys_Group>
{
    public void Configure(EntityTypeBuilder<Sys_Group> b)
    {
        b.ToTable("Sys_Group");
        b.HasKey(x => x.GroupCode);
        b.Property(x => x.GroupCode).IsRequired();
    }
}

/// <summary>
/// Bảng người dùng trong nhóm (Sys_UserInGroup) - phục vụ đếm số lượng người dùng trong nhóm.
/// Model nguồn: Sys_UserInGroup.cs:9-26. PK ghép = (UserCode, GroupCode).
/// </summary>
public class Sys_UserInGroup
{
    /// <summary>Mã người dùng (PK ghép) - Sys_UserInGroup.cs:13</summary>
    public string UserCode { get; set; } = "";

    /// <summary>Mã nhóm người dùng (PK ghép) - Sys_UserInGroup.cs:11</summary>
    public string GroupCode { get; set; } = "";
}

/// <summary>Cấu hình EF cho <see cref="Sys_UserInGroup"/>.</summary>
public class Sys_UserInGroupConfig : IEntityTypeConfiguration<Sys_UserInGroup>
{
    public void Configure(EntityTypeBuilder<Sys_UserInGroup> b)
    {
        b.ToTable("Sys_UserInGroup");
        b.HasKey(x => new { x.UserCode, x.GroupCode });
        b.Property(x => x.UserCode).IsRequired();
        b.Property(x => x.GroupCode).IsRequired();
    }
}

/// <summary>
/// Dữ liệu mẫu cho bảng Sys_Group và Sys_UserInGroup theo đề bài #307. Idempotent.
/// Order => 67.
/// </summary>
public class Sys_GroupSeeder : IPortSeeder
{
    public int Order => 67; // Thứ tự chạy seeder theo yêu cầu đề bài #307

    public async Task SeedAsync(AppDbContext db)
    {
        if (await db.Set<Sys_Group>().AnyAsync()) return;

        var logTime = new DateTime(2026, 10, 16, 0, 0, 0, DateTimeKind.Utc);

        // 4 dòng Sys_Group theo đề bài #307
        await db.Set<Sys_Group>().AddRangeAsync(
            new Sys_Group { GroupCode = "G01", GroupName = "Admin", FlagActive = "1", MST = "0101", LogLUBy = "seed", LogLUDTimeUTC = logTime },
            new Sys_Group { GroupCode = "G02", GroupName = "User", FlagActive = "1", MST = "0101", LogLUBy = "seed", LogLUDTimeUTC = logTime },
            new Sys_Group { GroupCode = "G03", GroupName = "Guest", FlagActive = "0", MST = "0101", LogLUBy = "seed", LogLUDTimeUTC = logTime },
            new Sys_Group { GroupCode = "G04", GroupName = "Tester", FlagActive = "1", MST = "0101", LogLUBy = "seed", LogLUDTimeUTC = logTime }
        );

        // 3 dòng Sys_UserInGroup tương ứng để test đếm Qty (U1->G01, U2->G01, U3->G02)
        if (!await db.Set<Sys_UserInGroup>().AnyAsync())
        {
            await db.Set<Sys_UserInGroup>().AddRangeAsync(
                new Sys_UserInGroup { UserCode = "U1", GroupCode = "G01" },
                new Sys_UserInGroup { UserCode = "U2", GroupCode = "G01" },
                new Sys_UserInGroup { UserCode = "U3", GroupCode = "G02" }
            );
        }

        await db.SaveChangesAsync();
    }
}
