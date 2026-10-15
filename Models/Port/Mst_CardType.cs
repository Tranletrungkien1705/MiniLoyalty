using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniLoyalty.Data;

namespace MiniLoyalty.Models.Port;

// ===========================================================================================
// PORT 1:1 từ dự án nguồn 2022.7.Loyalty — bảng danh mục Mst_CardType (loại thẻ/hạng thẻ).
//
// Nguồn tra cứu (chỉ đọc):
//   - Model cột:  12.Dev.Common/idn.Loyalty.Common/Models/Loyalty/Mst_CardType.cs:9-18
//   - DDL thật:   11.BackEnd/V10/05.Refs.Biz/Migrate/20221026.z11.UpdDB.02.MasterData.Loyalty.sql:82-90
//   - SQL select: idn.Loyalty.Biz/Master.Loyalty.cs:5583-5737 (Mst_CardType_GetX — bản LIVE)
//
// QUY ƯỚC KIỂU (ánh xạ user-defined type của SQL Server nguồn):
//   Varchar50/200 -> string?   | MyValue -> double?   | MyStatus -> string?   | MyDateTime -> DateTime?
// ===========================================================================================

/// <summary>
/// Loại thẻ / hạng thẻ — port 1:1 bảng <c>Mst_CardType</c>.
/// DDL: 20221026.z11.UpdDB.02.MasterData.Loyalty.sql:82-90. PK = CardType.
/// </summary>
public class Mst_CardType
{
    public string CardType { get; set; } = "";        // Mã loại thẻ — 20221026.z11.UpdDB.02.MasterData.Loyalty.sql:83 (Varchar50 NOT NULL, PK)
    public string NetworkID { get; set; } = "";       // NetworkID — 20221026.z11.UpdDB.02.MasterData.Loyalty.sql:84 (Varchar50 NOT NULL)
    public string CardTypeName { get; set; } = "";    // Tên loại thẻ — 20221026.z11.UpdDB.02.MasterData.Loyalty.sql:85 (Varchar200 NOT NULL)
    public double? Value { get; set; }                // Giá trị/ngưỡng của hạng — 20221026.z11.UpdDB.02.MasterData.Loyalty.sql:86 (MyValue)
    public string? FlagActive { get; set; }           // Cờ hoạt động '1'/'0' — 20221026.z11.UpdDB.02.MasterData.Loyalty.sql:87 (MyStatus)
    public DateTime? LogLUDTimeUTC { get; set; }      // Thời điểm log cập nhật (UTC) — 20221026.z11.UpdDB.02.MasterData.Loyalty.sql:88 (MyDateTime)
    public string? LogLUBy { get; set; }              // Người log cập nhật — 20221026.z11.UpdDB.02.MasterData.Loyalty.sql:89 (Varchar50)
}

/// <summary>Cấu hình EF cho <see cref="Mst_CardType"/> — tên bảng + khoá + 3 cột NOT NULL đúng DDL nguồn.</summary>
public class Mst_CardTypeConfig : IEntityTypeConfiguration<Mst_CardType>
{
    public void Configure(EntityTypeBuilder<Mst_CardType> b)
    {
        b.ToTable("Mst_CardType");
        b.HasKey(x => x.CardType);                    // DDL :82-90 — PK = CardType
        b.Property(x => x.CardType).IsRequired();     // DDL :83 NOT NULL
        b.Property(x => x.NetworkID).IsRequired();    // DDL :84 NOT NULL
        b.Property(x => x.CardTypeName).IsRequired(); // DDL :85 NOT NULL
    }
}

/// <summary>
/// Dữ liệu mẫu cho màn danh mục loại thẻ. Idempotent: đã có dòng nào thì bỏ qua.
/// 2 dòng R/N lấy NGUYÊN VĂN từ 20260518.z11.RankRN_BirthVoucher.sql:50-57; 4 dòng I/S/G/P là demo.
/// </summary>
public class Mst_CardTypeSeeder : IPortSeeder
{
    public int Order => 11;   // danh mục Mst_* dùng 10–99

    public async Task SeedAsync(AppDbContext db)
    {
        if (await db.Set<Mst_CardType>().AnyAsync()) return;

        db.Set<Mst_CardType>().AddRange(
            // 2 dòng NGUYÊN VĂN từ 20260518.z11.RankRN_BirthVoucher.sql:50-57
            new Mst_CardType { CardType = "R", NetworkID = "DEMO", CardTypeName = "Hạng R (cơ sở)", Value = 0, FlagActive = "1", LogLUBy = "seed" },
            new Mst_CardType { CardType = "N", NetworkID = "DEMO", CardTypeName = "Hạng N (khởi đầu)", Value = 5, FlagActive = "1", LogLUBy = "seed" },
            // 4 dòng DEMO (giá trị demo, cần xác nhận)
            new Mst_CardType { CardType = "I", NetworkID = "DEMO", CardTypeName = "Hạng I", Value = 10, FlagActive = "1", LogLUBy = "seed" },
            new Mst_CardType { CardType = "S", NetworkID = "DEMO", CardTypeName = "Hạng S", Value = 20, FlagActive = "1", LogLUBy = "seed" },
            new Mst_CardType { CardType = "G", NetworkID = "DEMO", CardTypeName = "Hạng G", Value = 30, FlagActive = "1", LogLUBy = "seed" },
            new Mst_CardType { CardType = "P", NetworkID = "DEMO", CardTypeName = "Hạng P", Value = 40, FlagActive = "0", LogLUBy = "seed" }
        );
        await db.SaveChangesAsync();
    }
}
