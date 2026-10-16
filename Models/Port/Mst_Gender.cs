using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniLoyalty.Data;

namespace MiniLoyalty.Models.Port;

// ===========================================================================================
// PORT 1:1 từ dự án nguồn 2022.7.Loyalty — bảng danh mục giới tính Mst_Gender (#154).
//
// Nguồn tra cứu (chỉ đọc):
//   - Model cột:  12.Dev.Common/idn.Loyalty.Common/Models/HRM/Mst_Gender.cs:9-18
//   - DDL:        Không có DDL trong repo — không có FK nào trỏ tới -> PK = GenderCode (khoá draft),
//                 ghi comment "khong co DDL — kieu cot can xac nhan".
//   - SQL select: 11.BackEnd/V10/idn.Loyalty.Biz/MasterData.cs (Mst_Gender_GetX :29942-30095)
//
// ĐẶC ĐIỂM BẢNG (đúng 7 cột):
//   GenderCode (PK, string not null), NetworkID (string?), OrgID (string?), Gender (string?),
//   FlagActive (string?), LogLUDTimeUTC (DateTime?), LogLUBy (string?).
//   LƯU Ý: bảng CÓ cột OrgID (Mst_Gender.cs:13) nhưng hàm lọc nguồn KHÔNG dùng OrgID để lọc.
//   Comment: "khong co DDL — kieu cot can xac nhan".
// ===========================================================================================

/// <summary>
/// Danh mục giới tính — port 1:1 bảng <c>Mst_Gender</c>.
/// Model nguồn: Mst_Gender.cs:9-18. PK = GenderCode.
/// Không có DDL — kiểu cột cần xác nhận.
/// Đúng 7 cột: GenderCode, NetworkID, OrgID, Gender, FlagActive, LogLUDTimeUTC, LogLUBy.
/// </summary>
public class Mst_Gender
{
    public string GenderCode { get; set; } = "";              // Mã giới tính — PK (khong co DDL — kieu cot can xac nhan)
    public string? NetworkID { get; set; }                    // Mã mạng
    public string? OrgID { get; set; }                        // Mã tổ chức (Mst_Gender.cs:13) — có cột nhưng hàm lọc KHÔNG dùng để lọc
    public string? Gender { get; set; }                       // Tên giới tính (cột tên là Gender, KHÔNG phải GenderName — Mst_GenderController.cs:178)
    public string? FlagActive { get; set; }                   // Trạng thái / Cờ hoạt động '1'/'0'
    public DateTime? LogLUDTimeUTC { get; set; }              // Thời điểm log cập nhật UTC
    public string? LogLUBy { get; set; }                      // Người cập nhật
}

/// <summary>Cấu hình EF cho <see cref="Mst_Gender"/> — tên bảng Mst_Gender + khoá chính GenderCode.</summary>
public class Mst_GenderConfig : IEntityTypeConfiguration<Mst_Gender>
{
    public void Configure(EntityTypeBuilder<Mst_Gender> b)
    {
        b.ToTable("Mst_Gender");
        b.HasKey(x => x.GenderCode);                          // PK = GenderCode (khong co DDL — kieu cot can xac nhan)
        b.Property(x => x.GenderCode).IsRequired();
    }
}

/// <summary>
/// Dữ liệu mẫu cho bảng Mst_Gender. Idempotent: đã có dữ liệu thì bỏ qua.
/// Dữ liệu demo (comment "du lieu demo"): GN01 'Nam' '1', GN02 'Nữ' '1', GN03 'Khác' '0'.
/// NetworkID 'DEMO', OrgID '4020320001', LogLUBy 'seed'.
/// Thứ tự Order => 36 (danh mục Mst_* dùng 10–99).
/// </summary>
public class Mst_GenderSeeder : IPortSeeder
{
    public int Order => 36;   // danh mục Mst_* dùng 10–99

    public async Task SeedAsync(AppDbContext db)
    {
        if (await db.Set<Mst_Gender>().AnyAsync()) return;

        db.Set<Mst_Gender>().AddRange(
            // Dữ liệu demo (comment "du lieu demo") — khong co DDL — kieu cot can xac nhan
            new Mst_Gender { GenderCode = "GN01", Gender = "Nam", FlagActive = "1", NetworkID = "DEMO", OrgID = "4020320001", LogLUBy = "seed" },
            new Mst_Gender { GenderCode = "GN02", Gender = "Nữ", FlagActive = "1", NetworkID = "DEMO", OrgID = "4020320001", LogLUBy = "seed" },
            new Mst_Gender { GenderCode = "GN03", Gender = "Khác", FlagActive = "0", NetworkID = "DEMO", OrgID = "4020320001", LogLUBy = "seed" }
        );
        await db.SaveChangesAsync();
    }
}
