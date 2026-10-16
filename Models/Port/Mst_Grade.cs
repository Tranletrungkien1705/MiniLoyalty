using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniLoyalty.Data;

namespace MiniLoyalty.Models.Port;

// ===========================================================================================
// PORT 1:1 từ dự án nguồn 2022.7.Loyalty — bảng danh mục trình độ chuyên môn Mst_Grade (#162).
//
// Nguồn tra cứu (chỉ đọc):
//   - Model cột:  12.Dev.Common/idn.Loyalty.Common/Models/HRM/Mst_Grade.cs:9-18 (7 cột: GradeCode, NetworkID, GradeName, GradeDesc, FlagActive, LogLUDTimeUTC, LogLUBy)
//   - DDL:        Không có DDL trong repo — kiểu/độ dài/NOT NULL: Chưa xác minh.
//                 PK: Chưa xác minh DDL; suy từ join/distinct dùng GradeCode (MasterData.cs:35314, :35357) -> PK = GradeCode (suy từ join, DDL chưa xác minh).
//   - SQL select: 11.BackEnd/V10/idn.Loyalty.Biz/MasterData.cs (Mst_Grade_GetX :35257-35412)
//
// ĐẶC ĐIỂM BẢNG (đúng 7 cột):
//   GradeCode (PK, string not null), NetworkID (string?), GradeName (string?),
//   GradeDesc (string?), FlagActive (string?), LogLUDTimeUTC (DateTime?), LogLUBy (string?).
// ===========================================================================================

/// <summary>
/// Danh mục trình độ chuyên môn — port 1:1 bảng <c>Mst_Grade</c>.
/// Model nguồn: Mst_Grade.cs:9-18.
/// DDL: Chưa xác minh (không có file create table trong .sql).
/// PK: GradeCode (suy từ join/distinct trong MasterData.cs:35314, :35357, DDL chưa xác minh).
/// Đúng 7 cột: GradeCode, NetworkID, GradeName, GradeDesc, FlagActive, LogLUDTimeUTC, LogLUBy.
/// </summary>
public class Mst_Grade
{
    public string GradeCode { get; set; } = "";          // Mã trình độ chuyên môn — PK (suy từ join, DDL chưa xác minh)
    public string? NetworkID { get; set; }               // Mã mạng
    public string? GradeName { get; set; }               // Tên trình độ
    public string? GradeDesc { get; set; }               // Mô tả
    public string? FlagActive { get; set; }              // Trạng thái '1'/'0'
    public DateTime? LogLUDTimeUTC { get; set; }         // Thời điểm cập nhật UTC
    public string? LogLUBy { get; set; }                 // Người cập nhật
}

/// <summary>Cấu hình EF cho <see cref="Mst_Grade"/> — tên bảng Mst_Grade + khoá chính GradeCode.</summary>
public class Mst_GradeConfig : IEntityTypeConfiguration<Mst_Grade>
{
    public void Configure(EntityTypeBuilder<Mst_Grade> b)
    {
        b.ToTable("Mst_Grade");
        b.HasKey(x => x.GradeCode);                      // PK = GradeCode (suy từ join, DDL chưa xác minh)
        b.Property(x => x.GradeCode).IsRequired();
    }
}

/// <summary>
/// Dữ liệu mẫu cho bảng Mst_Grade. Idempotent: đã có dữ liệu thì bỏ qua.
/// Dữ liệu demo (Order => 37 theo đề bài): G01..G06.
/// LogLUDTimeUTC cố định 2026-10-10 10:00:00 UTC, LogLUBy 'seed', NetworkID 'DEMO'.
/// Thứ tự Order => 37 (danh mục Mst_* dùng 10–99).
/// </summary>
public class Mst_GradeSeeder : IPortSeeder
{
    public int Order => 37;   // Danh mục Mst_* dùng 10–99; 36 dành cho Mst_Gender

    public async Task SeedAsync(AppDbContext db)
    {
        if (await db.Set<Mst_Grade>().AnyAsync()) return;

        var fixedTime = DateTime.SpecifyKind(new DateTime(2026, 10, 10, 10, 0, 0), DateTimeKind.Utc);

        db.Set<Mst_Grade>().AddRange(
            // Dữ liệu demo (comment "du lieu demo"): G01-G06
            new Mst_Grade { GradeCode = "G01", GradeName = "Cử nhân", GradeDesc = "Mo ta G01", FlagActive = "1", NetworkID = "DEMO", LogLUDTimeUTC = fixedTime, LogLUBy = "seed" },
            new Mst_Grade { GradeCode = "G02", GradeName = "Kỹ sư", GradeDesc = "Mo ta G02", FlagActive = "1", NetworkID = "DEMO", LogLUDTimeUTC = fixedTime, LogLUBy = "seed" },
            new Mst_Grade { GradeCode = "G03", GradeName = "Thạc sĩ", GradeDesc = "Mo ta G03", FlagActive = "1", NetworkID = "DEMO", LogLUDTimeUTC = fixedTime, LogLUBy = "seed" },
            new Mst_Grade { GradeCode = "G04", GradeName = "Tiến sĩ", GradeDesc = "Mo ta G04", FlagActive = "0", NetworkID = "DEMO", LogLUDTimeUTC = fixedTime, LogLUBy = "seed" },
            new Mst_Grade { GradeCode = "G05", GradeName = "Trung cấp", GradeDesc = "Mo ta G05", FlagActive = "1", NetworkID = "DEMO", LogLUDTimeUTC = fixedTime, LogLUBy = "seed" },
            new Mst_Grade { GradeCode = "G06", GradeName = "Cao đẳng", GradeDesc = "Mo ta G06", FlagActive = "1", NetworkID = "DEMO", LogLUDTimeUTC = fixedTime, LogLUBy = "seed" }
        );
        await db.SaveChangesAsync();
    }
}
