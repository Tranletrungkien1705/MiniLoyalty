using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniLoyalty.Data;

namespace MiniLoyalty.Models.Port;

// ===========================================================================================
// PORT 1:1 từ dự án nguồn 2022.7.Loyalty — bảng danh mục loại nghỉ phép Mst_LeaveType (#166).
//
// Nguồn tra cứu (chỉ đọc):
//   - Model cột:  12.Dev.Common/idn.Loyalty.Common/Models/HRM/Mst_LeaveType.cs:11-19 (9 cột)
//   - DDL:        Không có DDL trong repo (source_excerpt.txt:137) -> PK = LeaveTypeCode
//                 (suy từ draft distinct và inner join MasterData.cs:31648, :31691; DDL chưa xác minh)
//   - SQL select: 11.BackEnd/V10/idn.Loyalty.Biz/MasterData.cs (Mst_LeaveType_GetX :31644-31695)
//
// ĐẶC ĐIỂM BẢNG (đúng 9 cột):
//   LeaveTypeCode (PK, string not null), NetworkID (string?), LeaveType (string? - tên loại nghỉ phép),
//   LeaveTypeDesc (string? - mô tả), FlagUnPaid (string? - cờ không lương),
//   FlagInsurance (string? - cờ BH chi trả), FlagActive (string? - trạng thái),
//   LogLUDTimeUTC (DateTime?), LogLUBy (string?).
//   Comment: "khong co DDL — kieu cot can xac nhan".
// ===========================================================================================

/// <summary>
/// Danh mục loại nghỉ phép — port 1:1 bảng <c>Mst_LeaveType</c>.
/// Model nguồn: Mst_LeaveType.cs:11-19. PK = LeaveTypeCode (suy từ draft distinct và inner join MasterData.cs:31648, :31691; DDL chưa xác minh).
/// Không có DDL — kiểu cột cần xác nhận.
/// Đúng 9 cột: LeaveTypeCode, NetworkID, LeaveType, LeaveTypeDesc, FlagUnPaid, FlagInsurance, FlagActive, LogLUDTimeUTC, LogLUBy.
/// </summary>
public class Mst_LeaveType
{
    public string LeaveTypeCode { get; set; } = "";          // Mã loại nghỉ phép — PK (suy từ join, DDL chưa xác minh)
    public string? NetworkID { get; set; }                    // Mã mạng
    public string? LeaveType { get; set; }                    // Tên loại nghỉ phép
    public string? LeaveTypeDesc { get; set; }                // Mô tả
    public string? FlagUnPaid { get; set; }                   // Cờ không lương
    public string? FlagInsurance { get; set; }                // Cờ BH chi trả
    public string? FlagActive { get; set; }                   // Trạng thái / Cờ hoạt động '1'/'0'
    public DateTime? LogLUDTimeUTC { get; set; }              // Thời điểm log cập nhật UTC
    public string? LogLUBy { get; set; }                      // Người cập nhật
}

/// <summary>Cấu hình EF cho <see cref="Mst_LeaveType"/> — tên bảng Mst_LeaveType + khoá chính LeaveTypeCode.</summary>
public class Mst_LeaveTypeConfig : IEntityTypeConfiguration<Mst_LeaveType>
{
    public void Configure(EntityTypeBuilder<Mst_LeaveType> b)
    {
        b.ToTable("Mst_LeaveType");
        b.HasKey(x => x.LeaveTypeCode);                       // PK = LeaveTypeCode (suy từ join MasterData.cs:31648, :31691)
        b.Property(x => x.LeaveTypeCode).IsRequired();
    }
}

/// <summary>
/// Dữ liệu mẫu cho bảng Mst_LeaveType. Idempotent: đã có dữ liệu thì bỏ qua.
/// Thứ tự Order => 41 (danh mục Mst_* dùng 10–99).
/// </summary>
public class Mst_LeaveTypeSeeder : IPortSeeder
{
    public int Order => 41;   // danh mục Mst_* dùng 10–99

    public async Task SeedAsync(AppDbContext db)
    {
        if (await db.Set<Mst_LeaveType>().AnyAsync()) return;

        var seedTime = new DateTime(2026, 10, 10, 10, 0, 0, DateTimeKind.Utc);

        db.Set<Mst_LeaveType>().AddRange(
            // Dữ liệu demo (comment "du lieu demo") — khong co DDL — kieu cot can xac nhan
            new Mst_LeaveType
            {
                LeaveTypeCode = "LT01",
                LeaveType = "Nghi phep nam",
                LeaveTypeDesc = "Mo ta LT01",
                FlagUnPaid = "0",
                FlagInsurance = "0",
                FlagActive = "1",
                NetworkID = "DEMO",
                LogLUBy = "seed",
                LogLUDTimeUTC = seedTime
            },
            new Mst_LeaveType
            {
                LeaveTypeCode = "LT02",
                LeaveType = "Nghi om",
                LeaveTypeDesc = "Mo ta LT02",
                FlagUnPaid = "0",
                FlagInsurance = "1",
                FlagActive = "1",
                NetworkID = "DEMO",
                LogLUBy = "seed",
                LogLUDTimeUTC = seedTime
            },
            new Mst_LeaveType
            {
                LeaveTypeCode = "LT03",
                LeaveType = "Nghi thai san",
                LeaveTypeDesc = "Mo ta LT03",
                FlagUnPaid = "0",
                FlagInsurance = "1",
                FlagActive = "1",
                NetworkID = "DEMO",
                LogLUBy = "seed",
                LogLUDTimeUTC = seedTime
            },
            new Mst_LeaveType
            {
                LeaveTypeCode = "LT04",
                LeaveType = "Nghi khong luong",
                LeaveTypeDesc = "Mo ta LT04",
                FlagUnPaid = "1",
                FlagInsurance = "0",
                FlagActive = "1",
                NetworkID = "DEMO",
                LogLUBy = "seed",
                LogLUDTimeUTC = seedTime
            },
            new Mst_LeaveType
            {
                LeaveTypeCode = "LT05",
                LeaveType = "Nghi phep cuoi",
                LeaveTypeDesc = "Mo ta LT05",
                FlagUnPaid = "0",
                FlagInsurance = "0",
                FlagActive = "1",
                NetworkID = "DEMO",
                LogLUBy = "seed",
                LogLUDTimeUTC = seedTime
            },
            new Mst_LeaveType
            {
                LeaveTypeCode = "LT06",
                LeaveType = "Nghi phep cu (ngung)",
                LeaveTypeDesc = "Mo ta LT06",
                FlagUnPaid = "1",
                FlagInsurance = "0",
                FlagActive = "0",
                NetworkID = "DEMO",
                LogLUBy = "seed",
                LogLUDTimeUTC = seedTime
            }
        );
        await db.SaveChangesAsync();
    }
}
