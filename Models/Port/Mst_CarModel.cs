using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniLoyalty.Data;

namespace MiniLoyalty.Models.Port;

// ===========================================================================================
// PORT 1:1 từ dự án nguồn 2022.7.Loyalty — bảng danh mục dòng xe Mst_CarModel (#97).
//
// Nguồn tra cứu (chỉ đọc):
//   - Model cột:  12.Dev.Common/idn.Loyalty.Common/Models/Loyalty/Mst_CarModel.cs:9-23
//   - DDL:        Không có DDL trong repo — PK suy từ FK REFERENCES Mst_CarModel (ModelCode) (20221026.CreateDatabase.sql:91)
//   - SQL select: idn.Loyalty.Biz/Master.Loyalty.cs (Mst_CarModel_GetX — bản LIVE, SQL excerpt :1739-1791)
//
// ĐẶC ĐIỂM BẢNG (đúng 8 cột):
//   ModelCode (PK, string not null), NetworkID (string?), BrandCode (string?), ModelName (string?),
//   FlagActive (string?), Remark (string?), LogLUDTimeUTC (DateTime?), LogLUBy (string?).
//   Comment: "khong co DDL — PK suy tu FK REFERENCES Mst_CarModel (ModelCode)".
//   KHÔNG giả lập FK BrandCode -> Mst_CarBrand (nguồn không có DDL chứng minh).
// ===========================================================================================

/// <summary>
/// Danh mục dòng xe — port 1:1 bảng <c>Mst_CarModel</c>.
/// Model nguồn: Mst_CarModel.cs:9-23. PK = ModelCode.
/// Không có DDL — PK suy từ FK REFERENCES Mst_CarModel (ModelCode).
/// Đúng 8 cột: ModelCode, NetworkID, BrandCode, ModelName, FlagActive, Remark, LogLUDTimeUTC, LogLUBy.
/// </summary>
public class Mst_CarModel
{
    public string ModelCode { get; set; } = "";           // Mã dòng xe — PK (khong co DDL — PK suy tu FK REFERENCES Mst_CarModel (ModelCode))
    public string? NetworkID { get; set; }                // Mã mạng
    public string? BrandCode { get; set; }                // Mã hãng xe (không tạo FK vì nguồn không có DDL chứng minh)
    public string? ModelName { get; set; }                // Tên dòng xe
    public string? FlagActive { get; set; }               // Cờ hoạt động '1'/'0'
    public string? Remark { get; set; }                   // Ghi chú
    public DateTime? LogLUDTimeUTC { get; set; }          // Thời điểm log cập nhật UTC
    public string? LogLUBy { get; set; }                  // Người cập nhật
}

/// <summary>Cấu hình EF cho <see cref="Mst_CarModel"/> — tên bảng Mst_CarModel + khoá chính ModelCode.</summary>
public class Mst_CarModelConfig : IEntityTypeConfiguration<Mst_CarModel>
{
    public void Configure(EntityTypeBuilder<Mst_CarModel> b)
    {
        b.ToTable("Mst_CarModel");
        b.HasKey(x => x.ModelCode);                       // PK = ModelCode (khong co DDL — PK suy tu FK REFERENCES Mst_CarModel (ModelCode))
        b.Property(x => x.ModelCode).IsRequired();
    }
}

/// <summary>
/// Dữ liệu mẫu cho bảng Mst_CarModel. Idempotent: đã có dữ liệu thì bỏ qua.
/// Dữ liệu demo (comment "du lieu demo"): ACC HYU, MOR KIA, VIO TOY, VF8 VIN (VF8 FlagActive '0').
/// Thứ tự Order => 20 (danh mục Mst_* dùng 10–99).
/// </summary>
public class Mst_CarModelSeeder : IPortSeeder
{
    public int Order => 20;   // danh mục Mst_* dùng 10–99

    public async Task SeedAsync(AppDbContext db)
    {
        if (await db.Set<Mst_CarModel>().AnyAsync()) return;

        db.Set<Mst_CarModel>().AddRange(
            // Dữ liệu demo (comment "du lieu demo") — khong co DDL — PK suy tu FK REFERENCES Mst_CarModel (ModelCode)
            new Mst_CarModel { ModelCode = "ACC", BrandCode = "HYU", ModelName = "Accent", FlagActive = "1", NetworkID = "6403834000", LogLUBy = "seed" },
            new Mst_CarModel { ModelCode = "MOR", BrandCode = "KIA", ModelName = "Morning", FlagActive = "1", NetworkID = "6403834000", LogLUBy = "seed" },
            new Mst_CarModel { ModelCode = "VIO", BrandCode = "TOY", ModelName = "Vios", FlagActive = "1", NetworkID = "6403834000", LogLUBy = "seed" },
            new Mst_CarModel { ModelCode = "VF8", BrandCode = "VIN", ModelName = "VF 8", FlagActive = "0", NetworkID = "6403834000", LogLUBy = "seed" }
        );
        await db.SaveChangesAsync();
    }
}
