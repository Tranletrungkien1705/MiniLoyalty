using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniLoyalty.Data;

namespace MiniLoyalty.Models.Port;

// ===========================================================================================
// PORT 1:1 từ dự án nguồn 2022.7.Loyalty — bảng danh mục hình thức kỷ luật Mst_Discipline (#132).
//
// Nguồn tra cứu (chỉ đọc):
//   - Model cột:  12.Dev.Common/idn.Loyalty.Common/Models/HRM/Mst_Discipline.cs:9-18
//   - DDL:        Không có DDL trong repo — không có FK nào trỏ tới -> PK = DisciplineCode (khoá draft), ghi comment "khong co DDL — kieu cot can xac nhan"
//   - SQL select: 11.BackEnd/V10/idn.Loyalty.Biz/MasterData.cs (Mst_Discipline_GetX :33954-34109)
//
// ĐẶC ĐIỂM BẢNG (đúng 7 cột):
//   DisciplineCode (PK, string not null), NetworkID (string?), DisciplineName (string?),
//   DisciplineDesc (string?), FlagActive (string?), LogLUDTimeUTC (DateTime?), LogLUBy (string?).
//   Comment: "khong co DDL — kieu cot can xac nhan".
// ===========================================================================================

/// <summary>
/// Danh mục hình thức kỷ luật — port 1:1 bảng <c>Mst_Discipline</c>.
/// Model nguồn: Mst_Discipline.cs:9-18. PK = DisciplineCode.
/// Không có DDL — kiểu cột cần xác nhận.
/// Đúng 7 cột: DisciplineCode, NetworkID, DisciplineName, DisciplineDesc, FlagActive, LogLUDTimeUTC, LogLUBy.
/// </summary>
public class Mst_Discipline
{
    public string DisciplineCode { get; set; } = "";          // Mã hình thức kỷ luật — PK (khong co DDL — kieu cot can xac nhan)
    public string? NetworkID { get; set; }                    // Mã mạng
    public string? DisciplineName { get; set; }               // Hình thức kỷ luật
    public string? DisciplineDesc { get; set; }               // Mô tả
    public string? FlagActive { get; set; }                   // Trạng thái / Cờ hoạt động '1'/'0'
    public DateTime? LogLUDTimeUTC { get; set; }              // Thời điểm log cập nhật UTC
    public string? LogLUBy { get; set; }                      // Người cập nhật
}

/// <summary>Cấu hình EF cho <see cref="Mst_Discipline"/> — tên bảng Mst_Discipline + khoá chính DisciplineCode.</summary>
public class Mst_DisciplineConfig : IEntityTypeConfiguration<Mst_Discipline>
{
    public void Configure(EntityTypeBuilder<Mst_Discipline> b)
    {
        b.ToTable("Mst_Discipline");
        b.HasKey(x => x.DisciplineCode);                      // PK = DisciplineCode (khong co DDL — kieu cot can xac nhan)
        b.Property(x => x.DisciplineCode).IsRequired();
    }
}

/// <summary>
/// Dữ liệu mẫu cho bảng Mst_Discipline. Idempotent: đã có dữ liệu thì bỏ qua.
/// Dữ liệu demo (comment "du lieu demo"): KL01 'Khiển trách' (Desc 'Nhắc nhở bằng văn bản') '1', KL02 'Cảnh cáo' '1', KL03 'Sa thái' '0'.
/// Thứ tự Order => 33 (danh mục Mst_* dùng 10–99).
/// </summary>
public class Mst_DisciplineSeeder : IPortSeeder
{
    public int Order => 33;   // danh mục Mst_* dùng 10–99

    public async Task SeedAsync(AppDbContext db)
    {
        if (await db.Set<Mst_Discipline>().AnyAsync()) return;

        db.Set<Mst_Discipline>().AddRange(
            // Dữ liệu demo (comment "du lieu demo") — khong co DDL — kieu cot can xac nhan
            new Mst_Discipline { DisciplineCode = "KL01", DisciplineName = "Khiển trách", DisciplineDesc = "Nhắc nhở bằng văn bản", FlagActive = "1", NetworkID = "DEMO", LogLUBy = "seed" },
            new Mst_Discipline { DisciplineCode = "KL02", DisciplineName = "Cảnh cáo", FlagActive = "1", NetworkID = "DEMO", LogLUBy = "seed" },
            new Mst_Discipline { DisciplineCode = "KL03", DisciplineName = "Sa thải", FlagActive = "0", NetworkID = "DEMO", LogLUBy = "seed" }
        );
        await db.SaveChangesAsync();
    }
}

/// <summary>
/// Bảng sinh sequence cho mã hình thức kỷ luật — port 1:1 bảng <c>Seq_DisciplineCode</c> (Seq.cs:28-41, SeqType.DisciplineCode).
/// PK = AutoId (bigint IDENTITY(1,1) PK tự tăng).
/// </summary>
public class Seq_DisciplineCode
{
    public long AutoId { get; set; }                       // AutoId — bigint IDENTITY(1,1) PK
    public string? Remark { get; set; }                    // Remark — Varchar50
}

/// <summary>
/// Cấu hình EF cho <see cref="Seq_DisciplineCode"/> — PK AutoId tự tăng.
/// </summary>
public class Seq_DisciplineCodeConfig : IEntityTypeConfiguration<Seq_DisciplineCode>
{
    public void Configure(EntityTypeBuilder<Seq_DisciplineCode> b)
    {
        b.ToTable("Seq_DisciplineCode");
        b.HasKey(x => x.AutoId);
        b.Property(x => x.AutoId).ValueGeneratedOnAdd();
    }
}
