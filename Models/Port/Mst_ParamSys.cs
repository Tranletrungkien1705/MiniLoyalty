using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniLoyalty.Data;

namespace MiniLoyalty.Models.Port;

// ===========================================================================================
// PORT 1:1 từ dự án nguồn 2022.7.Loyalty — bảng tham số hệ thống Mst_ParamSys.
//
// Nguồn tra cứu (chỉ đọc):
//   - Model cột:  12.Dev.Common/idn.Loyalty.Common/Models/Loyalty/Mst_ParamSys.cs:9-17
//   - DDL thật:   11.BackEnd/V10/05.Refs.Biz/Migrate/20221006.QUYNHNTN/Mst_Param.sql:4-11
//   - Dữ liệu thật: Mst_Param.sql:20; insert.masterparam.sql:1
//   - SQL select: idn.Loyalty.Biz/Master.Loyalty.cs:12861-13020 (Mst_ParamSys_GetX — bản LIVE)
//
// ĐẶC ĐIỂM BẢNG (đúng 6 cột):
//   ParamCode (PK, Varchar200 NOT NULL), NetworkID (Varchar50 NOT NULL),
//   ParamValue (Varchar400), FlagActive (MyStatus), LogLUDTimeUTC (MyDateTime), LogLUBy (Varchar50).
// ===========================================================================================

/// <summary>
/// Tham số hệ thống — port 1:1 bảng <c>Mst_ParamSys</c>.
/// DDL: 20221006.QUYNHNTN/Mst_Param.sql:4-11. PK = ParamCode.
/// Đúng 6 cột.
/// </summary>
public class Mst_ParamSys
{
    public string ParamCode { get; set; } = "";        // Mã tham số hệ thống — PK (Mst_Param.sql:5, Varchar200 NOT NULL)
    public string NetworkID { get; set; } = "";       // Mã mạng — (Mst_Param.sql:6, Varchar50 NOT NULL)
    public string? ParamValue { get; set; }           // Giá trị tham số — (Mst_Param.sql:7, Varchar400)
    public string? FlagActive { get; set; }           // Cờ hoạt động '1'/'0' — (Mst_Param.sql:8, MyStatus)
    public DateTime? LogLUDTimeUTC { get; set; }      // Thời điểm log cập nhật UTC — (Mst_Param.sql:9, MyDateTime)
    public string? LogLUBy { get; set; }              // Người cập nhật — (Mst_Param.sql:10, Varchar50)
}

/// <summary>Cấu hình EF cho <see cref="Mst_ParamSys"/> — tên bảng + khoá chính + 2 cột NOT NULL đúng DDL nguồn.</summary>
public class Mst_ParamSysConfig : IEntityTypeConfiguration<Mst_ParamSys>
{
    public void Configure(EntityTypeBuilder<Mst_ParamSys> b)
    {
        b.ToTable("Mst_ParamSys");
        b.HasKey(x => x.ParamCode);                    // PK = ParamCode (Mst_Param.sql:4-11)
        b.Property(x => x.ParamCode).IsRequired();     // Mst_Param.sql:5 NOT NULL
        b.Property(x => x.NetworkID).IsRequired();    // Mst_Param.sql:6 NOT NULL
    }
}

/// <summary>
/// Dữ liệu mẫu cho bảng tham số hệ thống Mst_ParamSys. Idempotent: đã có dữ liệu thì bỏ qua.
/// 2 dòng NGUYÊN VĂN từ migration SQL + 1 dòng DEMO 'DEMO_OFF'.
/// Thứ tự Order => 14 (danh mục Mst_* dùng 10–99).
/// </summary>
public class Mst_ParamSysSeeder : IPortSeeder
{
    public int Order => 14;   // danh mục Mst_* dùng 10–99

    public async Task SeedAsync(AppDbContext db)
    {
        if (await db.Set<Mst_ParamSys>().AnyAsync()) return;

        db.Set<Mst_ParamSys>().AddRange(
            // 2 dòng NGUYÊN VĂN từ migration SQL:
            // Mst_Param.sql:20: insert into Mst_ParamSys values ('TS01', '6403834000', '1','1','2022-11-08 00:00:00', 'SYS')
            new Mst_ParamSys
            {
                ParamCode = "TS01",
                NetworkID = "6403834000",
                ParamValue = "1",
                FlagActive = "1",
                LogLUDTimeUTC = DateTime.Parse("2022-11-08 00:00:00"),
                LogLUBy = "SYS"
            },
            // insert.masterparam.sql:1: insert into Mst_ParamSys values('UNITPOINTTOMONEY','6403834000','1','1','2022-11-08 00:00:00', 'SYS')
            new Mst_ParamSys
            {
                ParamCode = "UNITPOINTTOMONEY",
                NetworkID = "6403834000",
                ParamValue = "1",
                FlagActive = "1",
                LogLUDTimeUTC = DateTime.Parse("2022-11-08 00:00:00"),
                LogLUBy = "SYS"
            },
            // 1 dòng DEMO (demo - FlagActive = '0', để verify lọc FlagActive)
            new Mst_ParamSys
            {
                ParamCode = "DEMO_OFF",
                NetworkID = "6403834000",
                ParamValue = "0",
                FlagActive = "0",
                LogLUDTimeUTC = DateTime.Parse("2023-01-01 00:00:00"),
                LogLUBy = "seed"
            }
        );
        await db.SaveChangesAsync();
    }
}
