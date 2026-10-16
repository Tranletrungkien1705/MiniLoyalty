using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniLoyalty.Data;

namespace MiniLoyalty.Models.Port;

// ===========================================================================================
// PORT 1:1 từ dự án nguồn 2022.7.Loyalty — bảng danh mục dân tộc Mst_EthnicCategory (#149).
//
// Nguồn tra cứu (chỉ đọc):
//   - Model cột:  12.Dev.Common/idn.Loyalty.Common/Models/HRM/Mst_EthnicCategory.cs:9-18
//   - DDL:        Không có DDL trong repo — không có FK nào trỏ tới -> PK = EthnicCode (khoá draft), ghi comment "khong co DDL — kieu cot can xac nhan"
//   - SQL select: 11.BackEnd/V10/idn.Loyalty.Biz/MasterData.cs (Mst_EthnicCategory_GetX :41521-41676)
//
// ĐẶC ĐIỂM BẢNG (đúng 7 cột):
//   EthnicCode (PK, string not null), NetworkID (string?), EthnicName (string?),
//   EthnicDesc (string?), FlagActive (string?), LogLUDTimeUTC (DateTime?), LogLUBy (string?).
//   Comment: "khong co DDL — kieu cot can xac nhan".
// ===========================================================================================

/// <summary>
/// Danh mục dân tộc — port 1:1 bảng <c>Mst_EthnicCategory</c>.
/// Model nguồn: Mst_EthnicCategory.cs:9-18. PK = EthnicCode.
/// Không có DDL — kiểu cột cần xác nhận.
/// Đúng 7 cột: EthnicCode, NetworkID, EthnicName, EthnicDesc, FlagActive, LogLUDTimeUTC, LogLUBy.
/// </summary>
public class Mst_EthnicCategory
{
    public string EthnicCode { get; set; } = "";             // Mã dân tộc — PK (khong co DDL — kieu cot can xac nhan)
    public string? NetworkID { get; set; }                    // Mã mạng
    public string? EthnicName { get; set; }                   // Tên dân tộc
    public string? EthnicDesc { get; set; }                   // Mô tả
    public string? FlagActive { get; set; }                   // Trạng thái / Cờ hoạt động '1'/'0'
    public DateTime? LogLUDTimeUTC { get; set; }              // Thời điểm log cập nhật UTC
    public string? LogLUBy { get; set; }                      // Người cập nhật
}

/// <summary>Cấu hình EF cho <see cref="Mst_EthnicCategory"/> — tên bảng Mst_EthnicCategory + khoá chính EthnicCode.</summary>
public class Mst_EthnicCategoryConfig : IEntityTypeConfiguration<Mst_EthnicCategory>
{
    public void Configure(EntityTypeBuilder<Mst_EthnicCategory> b)
    {
        b.ToTable("Mst_EthnicCategory");
        b.HasKey(x => x.EthnicCode);                          // PK = EthnicCode (khong co DDL — kieu cot can xac nhan)
        b.Property(x => x.EthnicCode).IsRequired();
    }
}

/// <summary>
/// Dữ liệu mẫu cho bảng Mst_EthnicCategory. Idempotent: đã có dữ liệu thì bỏ qua.
/// Dữ liệu demo (comment "du lieu demo"): DT01 'Kinh' (Desc 'Dân tộc Kinh') '1', DT02 'Tày' '1', DT03 'Khác' '0'.
/// Thứ tự Order => 34 (danh mục Mst_* dùng 10–99).
/// </summary>
public class Mst_EthnicCategorySeeder : IPortSeeder
{
    public int Order => 34;   // danh mục Mst_* dùng 10–99

    public async Task SeedAsync(AppDbContext db)
    {
        if (await db.Set<Mst_EthnicCategory>().AnyAsync()) return;

        db.Set<Mst_EthnicCategory>().AddRange(
            // Dữ liệu demo (comment "du lieu demo") — khong co DDL — kieu cot can xac nhan
            new Mst_EthnicCategory { EthnicCode = "DT01", EthnicName = "Kinh", EthnicDesc = "Dân tộc Kinh", FlagActive = "1", NetworkID = "DEMO", LogLUBy = "seed" },
            new Mst_EthnicCategory { EthnicCode = "DT02", EthnicName = "Tày", FlagActive = "1", NetworkID = "DEMO", LogLUBy = "seed" },
            new Mst_EthnicCategory { EthnicCode = "DT03", EthnicName = "Khác", FlagActive = "0", NetworkID = "DEMO", LogLUBy = "seed" }
        );
        await db.SaveChangesAsync();
    }
}
