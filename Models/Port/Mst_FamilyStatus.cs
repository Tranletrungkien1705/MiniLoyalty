using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniLoyalty.Data;

namespace MiniLoyalty.Models.Port;

// ===========================================================================================
// PORT 1:1 từ dự án nguồn 2022.7.Loyalty — bảng danh mục tình trạng gia đình Mst_FamilyStatus (#152).
//
// Nguồn tra cứu (chỉ đọc):
//   - Model cột:  12.Dev.Common/idn.Loyalty.Common/Models/HRM/Mst_FamilyStatus.cs:9-17
//   - DDL:        Không có DDL trong repo — PK = FamilyStatusCode (khoá draft), ghi comment "khong co DDL — kieu cot can xac nhan"
//   - SQL select: 11.BackEnd/V10/idn.Loyalty.Biz/MasterData.cs (Mst_FamilyStatus_GetX :42415-42570)
//
// ĐẶC ĐIỂM BẢNG (đúng 7 cột):
//   FamilyStatusCode (PK, string not null), NetworkID (string?), FamilyStatusName (string?),
//   FamilyStatusDesc (string?), FlagActive (string?), LogLUDTimeUTC (DateTime?), LogLUBy (string?).
//   Comment: "khong co DDL — kieu cot can xac nhan".
// ===========================================================================================

/// <summary>
/// Danh mục tình trạng gia đình — port 1:1 bảng <c>Mst_FamilyStatus</c>.
/// Model nguồn: Mst_FamilyStatus.cs:9-17. PK = FamilyStatusCode.
/// Không có DDL — kiểu cột cần xác nhận.
/// Đúng 7 cột: FamilyStatusCode, NetworkID, FamilyStatusName, FamilyStatusDesc, FlagActive, LogLUDTimeUTC, LogLUBy.
/// </summary>
public class Mst_FamilyStatus
{
    public string FamilyStatusCode { get; set; } = "";    // Mã thành phần gia đình — PK (khong co DDL — kieu cot can xac nhan)
    public string? NetworkID { get; set; }                    // Mã mạng
    public string? FamilyStatusName { get; set; }             // Tên tình trạng gia đình
    public string? FamilyStatusDesc { get; set; }             // Mô tả tình trạng gia đình
    public string? FlagActive { get; set; }                   // Cờ hoạt động '1'/'0'
    public DateTime? LogLUDTimeUTC { get; set; }              // Thời điểm log cập nhật UTC
    public string? LogLUBy { get; set; }                      // Người cập nhật
}

/// <summary>Cấu hình EF cho <see cref="Mst_FamilyStatus"/> — tên bảng Mst_FamilyStatus + khoá chính FamilyStatusCode.</summary>
public class Mst_FamilyStatusConfig : IEntityTypeConfiguration<Mst_FamilyStatus>
{
    public void Configure(EntityTypeBuilder<Mst_FamilyStatus> b)
    {
        b.ToTable("Mst_FamilyStatus");
        b.HasKey(x => x.FamilyStatusCode);                // PK = FamilyStatusCode (khong co DDL — kieu cot can xac nhan)
        b.Property(x => x.FamilyStatusCode).IsRequired();
    }
}

/// <summary>
/// Dữ liệu mẫu cho bảng Mst_FamilyStatus. Idempotent: đã có dữ liệu thì bỏ qua.
/// Dữ liệu demo (comment "du lieu demo"):
/// FS01 'Độc thân' '1' (Desc 'Chưa kết hôn') · FS02 'Đã kết hôn' '1' (Desc 'Có vợ/chồng') · FS03 'Ly hôn' '0' (Desc null).
/// Thứ tự Order => 32 (danh mục Mst_* dùng 10–99).
/// </summary>
public class Mst_FamilyStatusSeeder : IPortSeeder
{
    public int Order => 32;   // danh mục Mst_* dùng 10–99

    public async Task SeedAsync(AppDbContext db)
    {
        if (await db.Set<Mst_FamilyStatus>().AnyAsync()) return;

        db.Set<Mst_FamilyStatus>().AddRange(
            // Dữ liệu demo (comment "du lieu demo") — khong co DDL — kieu cot can xac nhan
            new Mst_FamilyStatus { FamilyStatusCode = "FS01", FamilyStatusName = "Độc thân", FamilyStatusDesc = "Chưa kết hôn", FlagActive = "1", NetworkID = "DEMO", LogLUBy = "seed" },
            new Mst_FamilyStatus { FamilyStatusCode = "FS02", FamilyStatusName = "Đã kết hôn", FamilyStatusDesc = "Có vợ/chồng", FlagActive = "1", NetworkID = "DEMO", LogLUBy = "seed" },
            new Mst_FamilyStatus { FamilyStatusCode = "FS03", FamilyStatusName = "Ly hôn", FamilyStatusDesc = null, FlagActive = "0", NetworkID = "DEMO", LogLUBy = "seed" }
        );
        await db.SaveChangesAsync();
    }
}
