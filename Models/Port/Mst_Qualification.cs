using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniLoyalty.Data;

namespace MiniLoyalty.Models.Port;

// ===========================================================================================
// PORT 1:1 từ dự án nguồn 2022.7.Loyalty — bảng danh mục trình độ học vấn Mst_Qualification (#223).
//
// Nguồn tra cứu (chỉ đọc):
//   - Model cột:  12.Dev.Common/idn.Loyalty.Common/Models/HRM/Mst_Qualification.cs:9-18
//   - DDL:        Không có 'create table Mst_Qualification' trong .sql của repo (source_excerpt.txt:135)
//                 -> dùng MODEL làm từ điển cột; PK = QualificationCode suy từ distinct/join trong SQL biz
//                 (MasterData.cs:36614-36621, :36654-36661); kiểu/độ dài chưa xác minh.
//   - SQL select: 11.BackEnd/V10/idn.Loyalty.Biz/MasterData.cs (Mst_Qualification_GetX :36559-36714)
//
// ĐẶC ĐIỂM BẢNG (đúng 7 cột):
//   QualificationCode (PK, string not null), NetworkID (string?), QualificationName (string?),
//   FlagActive (string?), Remark (string?), LogLUDTimeUTC (DateTime?), LogLUBy (string?).
// ===========================================================================================

/// <summary>
/// Danh mục trình độ học vấn — port 1:1 bảng <c>Mst_Qualification</c>.
/// Model nguồn: Mst_Qualification.cs:9-18. PK = QualificationCode (suy từ join, DDL chưa xác minh).
/// Đúng 7 cột: QualificationCode, NetworkID, QualificationName, FlagActive, Remark, LogLUDTimeUTC, LogLUBy.
/// </summary>
public class Mst_Qualification
{
    public string QualificationCode { get; set; } = "";       // Mã trình độ học vấn — PK (suy từ join, DDL chưa xác minh)
    public string? NetworkID { get; set; }                    // Mã mạng
    public string? QualificationName { get; set; }            // Tên
    public string? FlagActive { get; set; }                   // Trạng thái / Cờ hoạt động '1'/'0'
    public string? Remark { get; set; }                       // Ghi chú
    public DateTime? LogLUDTimeUTC { get; set; }              // Thời điểm log cập nhật UTC
    public string? LogLUBy { get; set; }                      // Người cập nhật
}

/// <summary>Cấu hình EF cho <see cref="Mst_Qualification"/> — tên bảng Mst_Qualification + khoá chính QualificationCode.</summary>
public class Mst_QualificationConfig : IEntityTypeConfiguration<Mst_Qualification>
{
    public void Configure(EntityTypeBuilder<Mst_Qualification> b)
    {
        b.ToTable("Mst_Qualification");
        b.HasKey(x => x.QualificationCode);                   // PK = QualificationCode (suy từ join, DDL chưa xác minh)
        b.Property(x => x.QualificationCode).IsRequired();
    }
}

/// <summary>
/// Dữ liệu mẫu cho bảng Mst_Qualification. Idempotent: đã có dữ liệu thì bỏ qua.
/// Dữ liệu demo (đề bài #223 P2): 4 dòng COL, DOC, MAS, UNI.
/// NetworkID 'DEMO', LogLUBy 'seed'.
/// Thứ tự Order => 68 (danh mục Mst_* dùng 10–99).
/// </summary>
public class Mst_QualificationSeeder : IPortSeeder
{
    public int Order => 68;   // danh mục Mst_* dùng 10–99

    public async Task SeedAsync(AppDbContext db)
    {
        if (await db.Set<Mst_Qualification>().AnyAsync()) return;

        var logTime = new DateTime(2026, 10, 10, 10, 0, 0, DateTimeKind.Utc);
        db.Set<Mst_Qualification>().AddRange(
            // Dữ liệu demo (đề bài #223 P2) — DDL chưa xác minh
            new Mst_Qualification { QualificationCode = "COL", QualificationName = "Cao đẳng", FlagActive = "1", NetworkID = "DEMO", LogLUBy = "seed", LogLUDTimeUTC = logTime },
            new Mst_Qualification { QualificationCode = "DOC", QualificationName = "Tiến sĩ", FlagActive = "1", NetworkID = "DEMO", LogLUBy = "seed", LogLUDTimeUTC = logTime },
            new Mst_Qualification { QualificationCode = "MAS", QualificationName = "Thạc sĩ", FlagActive = "0", NetworkID = "DEMO", LogLUBy = "seed", LogLUDTimeUTC = logTime },
            new Mst_Qualification { QualificationCode = "UNI", QualificationName = "Đại học", FlagActive = "1", NetworkID = "DEMO", LogLUBy = "seed", LogLUDTimeUTC = logTime }
        );
        await db.SaveChangesAsync();
    }
}
