using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniLoyalty.Data;

namespace MiniLoyalty.Models.Port;

// ===========================================================================================
// PORT 1:1 từ dự án nguồn 2022.7.Loyalty — bảng danh mục chuyên ngành Mst_Major (#168).
//
// Nguồn tra cứu (chỉ đọc):
//   - Model cột:  12.Dev.Common/idn.Loyalty.Common/Models/HRM/Mst_Major.cs:9-19
//   - DDL:        Không có 'create table Mst_Major' trong .sql của repo (source_excerpt.txt:136)
//                 -> dùng MODEL làm từ điển cột; PK = MajorCode suy từ distinct/join trong SQL biz
//                 (MasterData.cs:40246-40251, :40288-40289); kiểu/độ dài/FK MajorGroupCode chưa xác minh.
//   - SQL select: 11.BackEnd/V10/idn.Loyalty.Biz/MasterData.cs (Mst_Major_GetX :40189-40344)
//
// ĐẶC ĐIỂM BẢNG (đúng 8 cột):
//   MajorCode (PK, string not null), NetworkID (string?), MajorName (string?),
//   MajorDesc (string?), MajorGroupCode (string?), FlagActive (string?),
//   LogLUDTimeUTC (DateTime?), LogLUBy (string?).
//   KHÔNG tạo FK cho MajorGroupCode vì không có DDL chứng minh.
// ===========================================================================================

/// <summary>
/// Danh mục chuyên ngành — port 1:1 bảng <c>Mst_Major</c>.
/// Model nguồn: Mst_Major.cs:9-19. PK = MajorCode (suy từ join, DDL chưa xác minh).
/// Đúng 8 cột: MajorCode, NetworkID, MajorName, MajorDesc, MajorGroupCode, FlagActive, LogLUDTimeUTC, LogLUBy.
/// </summary>
public class Mst_Major
{
    public string MajorCode { get; set; } = "";              // Mã chuyên ngành — PK (suy từ join, DDL chưa xác minh)
    public string? NetworkID { get; set; }                    // Mã mạng
    public string? MajorName { get; set; }                    // Tên chuyên ngành
    public string? MajorDesc { get; set; }                    // Mô tả
    public string? MajorGroupCode { get; set; }               // Nhóm chuyên môn (không tạo FK — DDL chưa xác minh)
    public string? FlagActive { get; set; }                   // Trạng thái / Cờ hoạt động '1'/'0'
    public DateTime? LogLUDTimeUTC { get; set; }              // Thời điểm log cập nhật UTC
    public string? LogLUBy { get; set; }                      // Người cập nhật
}

/// <summary>Cấu hình EF cho <see cref="Mst_Major"/> — tên bảng Mst_Major + khoá chính MajorCode.</summary>
public class Mst_MajorConfig : IEntityTypeConfiguration<Mst_Major>
{
    public void Configure(EntityTypeBuilder<Mst_Major> b)
    {
        b.ToTable("Mst_Major");
        b.HasKey(x => x.MajorCode);                           // PK = MajorCode (suy từ join, DDL chưa xác minh)
        b.Property(x => x.MajorCode).IsRequired();
    }
}

/// <summary>
/// Dữ liệu mẫu cho bảng Mst_Major. Idempotent: đã có dữ liệu thì bỏ qua.
/// Dữ liệu demo (comment "du lieu demo"): 6 dòng MJ01..MJ06 (MJ04 FlagActive '0').
/// Thứ tự Order => 42 (danh mục Mst_* dùng 10–99).
/// </summary>
public class Mst_MajorSeeder : IPortSeeder
{
    public int Order => 42;   // danh mục Mst_* dùng 10–99

    public async Task SeedAsync(AppDbContext db)
    {
        if (await db.Set<Mst_Major>().AnyAsync()) return;

        var logTime = new DateTime(2026, 10, 10, 10, 0, 0, DateTimeKind.Utc);
        db.Set<Mst_Major>().AddRange(
            // Dữ liệu demo (comment "du lieu demo") — DDL chưa xác minh
            new Mst_Major { MajorCode = "MJ01", MajorName = "Cong nghe thong tin", MajorDesc = "Mo ta MJ01", MajorGroupCode = "MG01", FlagActive = "1", NetworkID = "DEMO", LogLUBy = "seed", LogLUDTimeUTC = logTime },
            new Mst_Major { MajorCode = "MJ02", MajorName = "Ky thuat phan mem", MajorDesc = "Mo ta MJ02", MajorGroupCode = "MG01", FlagActive = "1", NetworkID = "DEMO", LogLUBy = "seed", LogLUDTimeUTC = logTime },
            new Mst_Major { MajorCode = "MJ03", MajorName = "Quan tri kinh doanh", MajorDesc = "Mo ta MJ03", MajorGroupCode = "MG02", FlagActive = "1", NetworkID = "DEMO", LogLUBy = "seed", LogLUDTimeUTC = logTime },
            new Mst_Major { MajorCode = "MJ04", MajorName = "Ke toan doanh nghiep", MajorDesc = "Mo ta MJ04", MajorGroupCode = "MG02", FlagActive = "0", NetworkID = "DEMO", LogLUBy = "seed", LogLUDTimeUTC = logTime },
            new Mst_Major { MajorCode = "MJ05", MajorName = "Tai chinh ngan hang", MajorDesc = "Mo ta MJ05", MajorGroupCode = "MG02", FlagActive = "1", NetworkID = "DEMO", LogLUBy = "seed", LogLUDTimeUTC = logTime },
            new Mst_Major { MajorCode = "MJ06", MajorName = "Co khi dong luc", MajorDesc = "Mo ta MJ06", MajorGroupCode = "MG03", FlagActive = "1", NetworkID = "DEMO", LogLUBy = "seed", LogLUDTimeUTC = logTime }
        );
        await db.SaveChangesAsync();
    }
}
