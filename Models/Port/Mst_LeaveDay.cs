using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniLoyalty.Data;

namespace MiniLoyalty.Models.Port;

// ===========================================================================================
// PORT 1:1 từ dự án nguồn 2022.7.Loyalty — bảng danh mục ngày nghỉ phép Mst_LeaveDay (#164).
//
// Nguồn tra cứu (chỉ đọc):
//   - Model cột:  12.Dev.Common/idn.Loyalty.Common/Models/HRM/Mst_LeaveDay.cs:9-21 (10 cột: LeaveDayCodeSys, NetworkID, NormalLeaveDay, MinDevotedYear, LeaveDaySpecial, LeaveDayJob, FlagActive, Remark, LogLUDTimeUTC, LogLUBy)
//   - DDL:        Không có DDL trong repo (.sql) — kiểu/độ dài/NOT NULL: Chưa xác minh.
//                 PK: Chưa xác minh DDL; suy từ distinct/join mld.LeaveDayCodeSys (MasterData.cs:30223, :30266)
//                     và Mst_LeaveDay_CheckDB (MasterData.cs:30114) -> PK = LeaveDayCodeSys (suy từ join, DDL chưa xác minh).
//   - Kiểu số:    4 cột NormalLeaveDay, MinDevotedYear, LeaveDaySpecial, LeaveDayJob dùng kiểu double?
//                 theo MasterData.cs:30582-30585 (Convert.ToDouble) và PORT_RULES mục 3.
//   - SQL select: 11.BackEnd/V10/idn.Loyalty.Biz/MasterData.cs (Mst_LeaveDay_GetX :30166-30321)
//
// ĐẶC ĐIỂM BẢNG (đúng 10 cột):
//   LeaveDayCodeSys (PK, string not null), NetworkID (string?), NormalLeaveDay (double?),
//   MinDevotedYear (double?), LeaveDaySpecial (double?), LeaveDayJob (double?),
//   FlagActive (string?), Remark (string?), LogLUDTimeUTC (DateTime?), LogLUBy (string?).
// ===========================================================================================

/// <summary>
/// Danh mục ngày nghỉ phép — port 1:1 bảng <c>Mst_LeaveDay</c>.
/// Model nguồn: Mst_LeaveDay.cs:9-21.
/// DDL: Chưa xác minh (không có file create table trong .sql).
/// PK: LeaveDayCodeSys (suy từ distinct/join trong MasterData.cs:30223, :30266 và Mst_LeaveDay_CheckDB :30114, DDL chưa xác minh).
/// Đúng 10 cột: LeaveDayCodeSys, NetworkID, NormalLeaveDay, MinDevotedYear, LeaveDaySpecial, LeaveDayJob, FlagActive, Remark, LogLUDTimeUTC, LogLUBy.
/// </summary>
public class Mst_LeaveDay
{
    public string LeaveDayCodeSys { get; set; } = "";     // Mã số ngày nghỉ phép HT — PK (suy từ join, DDL chưa xác minh)
    public string? NetworkID { get; set; }               // Mã mạng
    public double? NormalLeaveDay { get; set; }          // Số ngày phép thông thường (double? theo PORT_RULES mục 3)
    public double? MinDevotedYear { get; set; }          // Số năm tối thiểu (double? theo PORT_RULES mục 3)
    public double? LeaveDaySpecial { get; set; }         // Số ngày phép đặc biệt (double? theo PORT_RULES mục 3)
    public double? LeaveDayJob { get; set; }             // Số ngày phép theo nghề (double? theo PORT_RULES mục 3)
    public string? FlagActive { get; set; }              // Trạng thái '1'/'0'
    public string? Remark { get; set; }                  // Ghi chú
    public DateTime? LogLUDTimeUTC { get; set; }         // Thời điểm cập nhật UTC
    public string? LogLUBy { get; set; }                 // Người cập nhật
}

/// <summary>Cấu hình EF cho <see cref="Mst_LeaveDay"/> — tên bảng Mst_LeaveDay + khoá chính LeaveDayCodeSys.</summary>
public class Mst_LeaveDayConfig : IEntityTypeConfiguration<Mst_LeaveDay>
{
    public void Configure(EntityTypeBuilder<Mst_LeaveDay> b)
    {
        b.ToTable("Mst_LeaveDay");
        b.HasKey(x => x.LeaveDayCodeSys);                // PK = LeaveDayCodeSys (suy từ join, DDL chưa xác minh)
        b.Property(x => x.LeaveDayCodeSys).IsRequired();
    }
}

/// <summary>
/// Dữ liệu mẫu cho bảng Mst_LeaveDay. Idempotent: đã có dữ liệu thì bỏ qua.
/// Dữ liệu demo (Order => 38 theo đề bài #164): LD01..LD06 (6 dòng).
/// LogLUDTimeUTC cố định 2026-10-10 10:00:00 UTC, LogLUBy 'seed', NetworkID 'DEMO'.
/// Thứ tự Order => 38 (danh mục Mst_* dùng 10–99).
/// </summary>
public class Mst_LeaveDaySeeder : IPortSeeder
{
    public int Order => 38;                              // Danh mục Mst_* dùng 10–99; Order 38 theo đề bài #164

    public async Task SeedAsync(AppDbContext db)
    {
        if (await db.Set<Mst_LeaveDay>().AnyAsync()) return;

        var fixedTime = DateTime.SpecifyKind(new DateTime(2026, 10, 10, 10, 0, 0), DateTimeKind.Utc);

        db.Set<Mst_LeaveDay>().AddRange(
            // Dữ liệu demo (Order => 38 theo đề bài #164, 6 dòng)
            new Mst_LeaveDay { LeaveDayCodeSys = "LD01", NormalLeaveDay = 12, MinDevotedYear = 1, LeaveDaySpecial = 2, LeaveDayJob = 0, FlagActive = "1", Remark = "Phep tieu chuan 1 nam", NetworkID = "DEMO", LogLUDTimeUTC = fixedTime, LogLUBy = "seed" },
            new Mst_LeaveDay { LeaveDayCodeSys = "LD02", NormalLeaveDay = 14, MinDevotedYear = 5, LeaveDaySpecial = 3, LeaveDayJob = 1, FlagActive = "1", Remark = "Phep tham nien 5 nam", NetworkID = "DEMO", LogLUDTimeUTC = fixedTime, LogLUBy = "seed" },
            new Mst_LeaveDay { LeaveDayCodeSys = "LD03", NormalLeaveDay = 15, MinDevotedYear = 10, LeaveDaySpecial = 4, LeaveDayJob = 2, FlagActive = "1", Remark = "Phep tham nien 10 nam", NetworkID = "DEMO", LogLUDTimeUTC = fixedTime, LogLUBy = "seed" },
            new Mst_LeaveDay { LeaveDayCodeSys = "LD04", NormalLeaveDay = 16, MinDevotedYear = 15, LeaveDaySpecial = 5, LeaveDayJob = 2, FlagActive = "0", Remark = "Chinh sach cu ngung ap dung", NetworkID = "DEMO", LogLUDTimeUTC = fixedTime, LogLUBy = "seed" },
            new Mst_LeaveDay { LeaveDayCodeSys = "LD05", NormalLeaveDay = 18, MinDevotedYear = 20, LeaveDaySpecial = 6, LeaveDayJob = 3, FlagActive = "1", Remark = "Phep tham nien 20 nam", NetworkID = "DEMO", LogLUDTimeUTC = fixedTime, LogLUBy = "seed" },
            new Mst_LeaveDay { LeaveDayCodeSys = "LD06", NormalLeaveDay = 24, MinDevotedYear = 25, LeaveDaySpecial = 8, LeaveDayJob = 4, FlagActive = "1", Remark = "Phep dac thu 14", NetworkID = "DEMO", LogLUDTimeUTC = fixedTime, LogLUBy = "seed" }
        );
        await db.SaveChangesAsync();
    }
}
