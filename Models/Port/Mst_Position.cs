using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniLoyalty.Data;

namespace MiniLoyalty.Models.Port;

// ===========================================================================================
// PORT 1:1 từ dự án nguồn 2022.7.Loyalty — bảng chức danh Mst_Position (#197).
//
// Nguồn tra cứu (chỉ đọc):
//   - Model cột:  12.Dev.Common/idn.Loyalty.Common/Models/HRM/Mst_Position.cs:1-22 (9 cột DB + 1 cột mở rộng mnnt_NNTFullName)
//   - DDL:        Không có DDL 'create table Mst_Position' trong các file .sql của repo nguồn.
//                 Tra cứu từ Model idn.Loyalty.Common\Models\HRM\Mst_Position.cs:1-22 và hàm lưu MasterData.cs:23570-23580.
//                 Bảng Mst_Position gồm ĐÚNG 9 cột:
//                   1. OrgID (string, NOT NULL)
//                   2. PositionCode (string, NOT NULL)
//                   3. NetworkID (string?)
//                   4. PositionName (string?)
//                   5. PositionDesc (string?)
//                   6. FlagActive (string?)
//                   7. Remark (string?)
//                   8. LogLUDTimeUTC (DateTime?)
//                   9. LogLUBy (string?)
//   - Khóa chính (PK): Khóa phức hợp (OrgID, PositionCode) theo draft (MasterData.cs:23118-23119),
//                     inner join (MasterData.cs:23165-23166) và Mst_Position_CheckDB (MasterData.cs:22927-22928).
//   - SQL select: 11.BackEnd/V10/idn.Loyalty.Biz/MasterData.cs (Mst_Position_GetX :23056-23224)
//   - Cột mở rộng trả về (không lưu trong bảng Mst_Position):
//       + MyIdxSeq (long?): chỉ số 0-based từ bảng draft (#tbl_Mst_Position_Filter_Draft, MasterData.cs:23117)
//       + mnnt_NNTFullName (string?): tên tổ chức lấy từ Mst_NNT.NNTFullName qua
//         left join Mst_NNT mnnt on t.OrgID = mnnt.OrgID (MasterData.cs:23162, :23167-23168).
// ===========================================================================================

/// <summary>
/// Danh mục chức danh — port 1:1 bảng <c>Mst_Position</c>.
/// Model nguồn: idn.Loyalty.Common\Models\HRM\Mst_Position.cs:1-22.
/// Khóa chính (PK) phức hợp: (OrgID, PositionCode) — MasterData.cs:23118-23119, :23165-23166.
/// Đúng 9 cột DB: OrgID, PositionCode, NetworkID, PositionName, PositionDesc, FlagActive, Remark, LogLUDTimeUTC, LogLUBy.
/// </summary>
public class Mst_Position
{
    public string OrgID { get; set; } = "";             // Mã tổ chức — PK (MasterData.cs:23118, :23165)
    public string PositionCode { get; set; } = "";      // Mã chức danh — PK (MasterData.cs:23119, :23166)
    public string? NetworkID { get; set; }              // Mã mạng lưới (Mst_Position.cs:13)
    public string? PositionName { get; set; }           // Tên chức danh (Mst_Position.cs:14)
    public string? PositionDesc { get; set; }           // Mô tả chức danh (Mst_Position.cs:15)
    public string? FlagActive { get; set; }             // Trạng thái ('1'/'0') (Mst_Position.cs:16)
    public string? Remark { get; set; }                 // Ghi chú (Mst_Position.cs:17)
    public DateTime? LogLUDTimeUTC { get; set; }        // Thời điểm cập nhật UTC (Mst_Position.cs:18)
    public string? LogLUBy { get; set; }                // Người cập nhật (Mst_Position.cs:19)
}

/// <summary>
/// Cấu hình EF cho <see cref="Mst_Position"/> — tên bảng Mst_Position + khoá chính phức hợp (OrgID, PositionCode).
/// </summary>
public class Mst_PositionConfig : IEntityTypeConfiguration<Mst_Position>
{
    public void Configure(EntityTypeBuilder<Mst_Position> b)
    {
        b.ToTable("Mst_Position");
        b.HasKey(x => new { x.OrgID, x.PositionCode });
        b.Property(x => x.OrgID).IsRequired();
        b.Property(x => x.PositionCode).IsRequired();
    }
}

/// <summary>
/// Dữ liệu mẫu cho bảng Mst_Position. Idempotent: đã có dữ liệu thì bỏ qua.
/// Thứ tự Order => 45 (chạy sau Mst_NNT Order=22, danh mục Mst_* dùng 10–99).
/// Dữ liệu demo (Order => 45 theo đề bài #197, 6 dòng):
///   1. OrgID: "4020320001", PositionCode: "POS_DEV", PositionName: "Lap trinh vien", PositionDesc: "Phat trien phan mem", FlagActive: "1", Remark: "Demo dev"
///   2. OrgID: "4020320001", PositionCode: "POS_DIR", PositionName: "Giam doc dieu hanh", PositionDesc: "Dieu hanh chung", FlagActive: "1", Remark: "Demo dir"
///   3. OrgID: "4020320001", PositionCode: "POS_MGR", PositionName: "Truong phong kinh doanh", PositionDesc: "Quan ly kinh doanh", FlagActive: "0", Remark: "Demo mgr"
///   4. OrgID: "4020320002", PositionCode: "POS_ACC", PositionName: "Ke toan tong hop", PositionDesc: "Ke toan tai chinh", FlagActive: "1", Remark: "Demo acc"
///   5. OrgID: "4020320002", PositionCode: "POS_HR", PositionName: "Chuyen vien nhan su", PositionDesc: "Quan ly nhan su", FlagActive: "1", Remark: "Demo hr"
///   6. OrgID: "4020330009", PositionCode: "POS_CONS", PositionName: "Chuyen vien tu van", PositionDesc: "Tu van dich vu", FlagActive: "0", Remark: "Demo cons"
/// </summary>
public class Mst_PositionSeeder : IPortSeeder
{
    public int Order => 45;   // Chạy sau Mst_NNT Order=22, danh mục Mst_* dùng 10–99

    public async Task SeedAsync(AppDbContext db)
    {
        if (await db.Set<Mst_Position>().AnyAsync()) return;

        var fixedTime = DateTime.Parse("2026-10-16T10:00:00Z").ToUniversalTime();

        db.Set<Mst_Position>().AddRange(
            new Mst_Position
            {
                OrgID = "4020320001",
                PositionCode = "POS_DEV",
                PositionName = "Lap trinh vien",
                PositionDesc = "Phat trien phan mem",
                FlagActive = "1",
                Remark = "Demo dev",
                NetworkID = "1001",
                LogLUBy = "seed",
                LogLUDTimeUTC = fixedTime
            },
            new Mst_Position
            {
                OrgID = "4020320001",
                PositionCode = "POS_DIR",
                PositionName = "Giam doc dieu hanh",
                PositionDesc = "Dieu hanh chung",
                FlagActive = "1",
                Remark = "Demo dir",
                NetworkID = "1001",
                LogLUBy = "seed",
                LogLUDTimeUTC = fixedTime
            },
            new Mst_Position
            {
                OrgID = "4020320001",
                PositionCode = "POS_MGR",
                PositionName = "Truong phong kinh doanh",
                PositionDesc = "Quan ly kinh doanh",
                FlagActive = "0",
                Remark = "Demo mgr",
                NetworkID = "1001",
                LogLUBy = "seed",
                LogLUDTimeUTC = fixedTime
            },
            new Mst_Position
            {
                OrgID = "4020320002",
                PositionCode = "POS_ACC",
                PositionName = "Ke toan tong hop",
                PositionDesc = "Ke toan tai chinh",
                FlagActive = "1",
                Remark = "Demo acc",
                NetworkID = "1001",
                LogLUBy = "seed",
                LogLUDTimeUTC = fixedTime
            },
            new Mst_Position
            {
                OrgID = "4020320002",
                PositionCode = "POS_HR",
                PositionName = "Chuyen vien nhan su",
                PositionDesc = "Quan ly nhan su",
                FlagActive = "1",
                Remark = "Demo hr",
                NetworkID = "1001",
                LogLUBy = "seed",
                LogLUDTimeUTC = fixedTime
            },
            new Mst_Position
            {
                OrgID = "4020330009",
                PositionCode = "POS_CONS",
                PositionName = "Chuyen vien tu van",
                PositionDesc = "Tu van dich vu",
                FlagActive = "0",
                Remark = "Demo cons",
                NetworkID = "1001",
                LogLUBy = "seed",
                LogLUDTimeUTC = fixedTime
            }
        );
        await db.SaveChangesAsync();
    }
}
