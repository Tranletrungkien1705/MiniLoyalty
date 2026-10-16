using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniLoyalty.Data;

namespace MiniLoyalty.Models.Port;

// ===========================================================================================
// PORT 1:1 từ dự án nguồn 2022.7.Loyalty — bảng Sys_Object (#317, #319, #321).
// Nguồn tra cứu:
//   - Model cột: 12.Dev.Common/idn.Loyalty.Common/Models/Sys_Object.cs:9-26
// ===========================================================================================

/// <summary>
/// Bảng Sys_Object — port 1:1.
/// 8 cột: ObjectCode, ObjectName, ServiceCode, ObjectType, FlagExecModal, FlagActive, LogLUDTimeUTC, LogLUBy.
/// </summary>
public class Sys_Object
{
    public string ObjectCode { get; set; } = "";
    public string? ObjectName { get; set; }
    public string? ServiceCode { get; set; }
    public string? ObjectType { get; set; }
    public string? FlagExecModal { get; set; }
    public string? FlagActive { get; set; }
    public string? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }
}

public class Sys_ObjectConfig : IEntityTypeConfiguration<Sys_Object>
{
    public void Configure(EntityTypeBuilder<Sys_Object> b)
    {
        b.ToTable("Sys_Object");
        b.HasKey(x => x.ObjectCode); // PK = ObjectCode (theo SQL order by)
        b.Property(x => x.ObjectCode).IsRequired();
    }
}

public class Sys_ObjectSeeder : IPortSeeder
{
    public int Order => 85;

    public async Task SeedAsync(AppDbContext db)
    {
        if (await db.Set<Sys_Object>().AnyAsync()) return;

        db.Set<Sys_Object>().AddRange(
            new Sys_Object { ObjectCode = "MNU01", ObjectName = "Menu 1", ObjectType = "MENU", FlagActive = "1", ServiceCode = "SRV01", LogLUBy = "seed" },
            new Sys_Object { ObjectCode = "MNU02", ObjectName = "Menu 2", ObjectType = "MENU", FlagActive = "1", ServiceCode = "SRV01", LogLUBy = "seed" },
            new Sys_Object { ObjectCode = "MNU03", ObjectName = "Menu 3 Inactive", ObjectType = "MENU", FlagActive = "0", ServiceCode = "SRV01", LogLUBy = "seed" },
            new Sys_Object { ObjectCode = "OBJ01", ObjectName = "Not a Menu", ObjectType = "FORM", FlagActive = "1", ServiceCode = "SRV01", LogLUBy = "seed" }
        );
        await db.SaveChangesAsync();
    }
}

public class Sys_ObjectMenuButtonSeeder : IPortSeeder
{
    public int Order => 86;

    public async Task SeedAsync(AppDbContext db)
    {
        // Thêm các button phục vụ test GetAllMenuButtonActive
        if (!await db.Set<Sys_Object>().AnyAsync(x => x.ObjectCode == "BTN01"))
        {
            db.Set<Sys_Object>().AddRange(
                new Sys_Object { ObjectCode = "BTN01", ObjectName = "Button 1", ObjectType = "BUTTON", FlagActive = "1", ServiceCode = "SRV01", LogLUBy = "seed" },
                new Sys_Object { ObjectCode = "BTN02", ObjectName = "Button 2 Inactive", ObjectType = "BUTTON", FlagActive = "0", ServiceCode = "SRV01", LogLUBy = "seed" }
            );
            await db.SaveChangesAsync();
        }
    }
}

public class Sys_ObjectFunctionSeeder : IPortSeeder
{
    public int Order => 87;

    public async Task SeedAsync(AppDbContext db)
    {
        // Thêm bản ghi hàm (FUNC) phục vụ test GetFunctionActive (#319) và GetAllFunctionBy_ObjectCode (#321)
        // Yêu cầu: ít nhất 2 bản ghi ObjectType = "FUNC", FlagActive = "1" và 1 bản ghi ObjectType = "FUNC", FlagActive = "0"
        if (!await db.Set<Sys_Object>().AnyAsync(x => x.ObjectCode == "FUNC_B"))
        {
            db.Set<Sys_Object>().AddRange(
                new Sys_Object { ObjectCode = "Sys_User_Get", ObjectName = "Xem người dùng", ObjectType = "FUNC", FlagActive = "1", ServiceCode = "SRV01", LogLUBy = "seed" },
                new Sys_Object { ObjectCode = "Sys_User_Create", ObjectName = "Tạo người dùng", ObjectType = "FUNC", FlagActive = "1", ServiceCode = "SRV01", LogLUBy = "seed" },
                new Sys_Object { ObjectCode = "Sys_User_Other", ObjectName = "Chức năng khác (không gán vào menu)", ObjectType = "FUNC", FlagActive = "1", ServiceCode = "SRV01", LogLUBy = "seed" },
                new Sys_Object { ObjectCode = "FUNC_B", ObjectName = "Function B Active", ObjectType = "FUNC", FlagActive = "1", ServiceCode = "SRV01", LogLUBy = "seed" },
                new Sys_Object { ObjectCode = "FUNC_A", ObjectName = "Function A Active", ObjectType = "FUNC", FlagActive = "1", ServiceCode = "SRV01", LogLUBy = "seed" },
                new Sys_Object { ObjectCode = "FUNC_C", ObjectName = "Function C Inactive", ObjectType = "FUNC", FlagActive = "0", ServiceCode = "SRV01", LogLUBy = "seed" }
            );
            await db.SaveChangesAsync();
        }
    }
}
