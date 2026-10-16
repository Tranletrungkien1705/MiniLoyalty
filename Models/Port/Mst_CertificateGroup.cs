using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniLoyalty.Data;

namespace MiniLoyalty.Models.Port;

// ===========================================================================================
// PORT 1:1 từ dự án nguồn 2022.7.Loyalty — bảng danh mục nhóm chứng chỉ Mst_CertificateGroup (#107).
//
// Nguồn tra cứu (chỉ đọc):
//   - Model cột:  12.Dev.Common/idn.Loyalty.Common/Models/HRM/Mst_CertificateGroup.cs:9-17
//   - DDL:        Không có DDL trong repo — không có FK nào trỏ tới -> PK = CertificateGroupCode (khoá draft), ghi comment "khong co DDL — kieu cot can xac nhan"
//   - SQL select: 11.BackEnd/V10/idn.Loyalty.Biz/MasterData.cs (Mst_CertificateGroup_GetX :44478-44633)
//
// ĐẶC ĐIỂM BẢNG (đúng 6 cột):
//   CertificateGroupCode (PK, string not null), NetworkID (string?), CertificateGroupName (string?),
//   FlagActive (string?), LogLUDTimeUTC (DateTime?), LogLUBy (string?).
//   Comment: "khong co DDL — kieu cot can xac nhan".
// ===========================================================================================

/// <summary>
/// Danh mục nhóm chứng chỉ — port 1:1 bảng <c>Mst_CertificateGroup</c>.
/// Model nguồn: Mst_CertificateGroup.cs:9-17. PK = CertificateGroupCode.
/// Không có DDL — kiểu cột cần xác nhận.
/// Đúng 6 cột: CertificateGroupCode, NetworkID, CertificateGroupName, FlagActive, LogLUDTimeUTC, LogLUBy.
/// </summary>
public class Mst_CertificateGroup
{
    public string CertificateGroupCode { get; set; } = "";    // Mã nhóm chứng chỉ — PK (khong co DDL — kieu cot can xac nhan)
    public string? NetworkID { get; set; }                    // Mã mạng
    public string? CertificateGroupName { get; set; }         // Tên nhóm chứng chỉ
    public string? FlagActive { get; set; }                   // Cờ hoạt động '1'/'0'
    public DateTime? LogLUDTimeUTC { get; set; }              // Thời điểm log cập nhật UTC
    public string? LogLUBy { get; set; }                      // Người cập nhật
}

/// <summary>Cấu hình EF cho <see cref="Mst_CertificateGroup"/> — tên bảng Mst_CertificateGroup + khoá chính CertificateGroupCode.</summary>
public class Mst_CertificateGroupConfig : IEntityTypeConfiguration<Mst_CertificateGroup>
{
    public void Configure(EntityTypeBuilder<Mst_CertificateGroup> b)
    {
        b.ToTable("Mst_CertificateGroup");
        b.HasKey(x => x.CertificateGroupCode);                // PK = CertificateGroupCode (khong co DDL — kieu cot can xac nhan)
        b.Property(x => x.CertificateGroupCode).IsRequired();
    }
}

/// <summary>
/// Dữ liệu mẫu cho bảng Mst_CertificateGroup. Idempotent: đã có dữ liệu thì bỏ qua.
/// Dữ liệu demo (comment "du lieu demo"): CG01 'Nhóm chứng chỉ A' '1', CG02 'Nhóm chứng chỉ B' '1', CG03 'Kỹ thuật viên' '0'.
/// Thứ tự Order => 31 (danh mục Mst_* dùng 10–99).
/// </summary>
public class Mst_CertificateGroupSeeder : IPortSeeder
{
    public int Order => 31;   // danh mục Mst_* dùng 10–99

    public async Task SeedAsync(AppDbContext db)
    {
        if (await db.Set<Mst_CertificateGroup>().AnyAsync()) return;

        db.Set<Mst_CertificateGroup>().AddRange(
            // Dữ liệu demo (comment "du lieu demo") — khong co DDL — kieu cot can xac nhan
            new Mst_CertificateGroup { CertificateGroupCode = "CG01", CertificateGroupName = "Nhóm chứng chỉ A", FlagActive = "1", NetworkID = "DEMO", LogLUBy = "seed" },
            new Mst_CertificateGroup { CertificateGroupCode = "CG02", CertificateGroupName = "Nhóm chứng chỉ B", FlagActive = "1", NetworkID = "DEMO", LogLUBy = "seed" },
            new Mst_CertificateGroup { CertificateGroupCode = "CG03", CertificateGroupName = "Kỹ thuật viên", FlagActive = "0", NetworkID = "DEMO", LogLUBy = "seed" }
        );
        await db.SaveChangesAsync();
    }
}
