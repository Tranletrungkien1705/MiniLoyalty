using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniLoyalty.Data;

namespace MiniLoyalty.Models.Port;

// ===========================================================================================
// PORT 1:1 từ dự án nguồn 2022.7.Loyalty — bảng danh mục phòng ban Mst_Department (#122).
//
// Nguồn tra cứu (chỉ đọc):
//   - Model cột:  12.Dev.Common/idn.Loyalty.Common/Models/Mst_Department.cs:8-35 (13 cột DB + 2 cột tính)
//   - DDL:        Không có DDL 'create table Mst_Department' trong .sql của repo nguồn.
//                 FK trỏ tới: Train_CourseDepartment.DepartmentCode (20220219.z11.UpdDB.60.Course.sql:177 — chưa port).
//                 PK = DepartmentCode (khoá chính draft, ghi chú "khong co DDL — kieu cot can xac nhan").
//                 DepartmentLevel -> int? (ghi chú "kieu can xac nhan"), còn lại string.
//   - SQL select: 11.BackEnd/V10/idn.Loyalty.Biz/Master.cs (Mst_Department_GetX :30396-30568)
//
// ĐẶC ĐIỂM BẢNG (đúng 13 cột DB):
//   DepartmentCode (PK, string not null), NetworkID (string?), DepartmentCodeParent (string?),
//   DepartmentBUCode (string?), DepartmentBUPattern (string?), DepartmentLevel (int?),
//   MST (string?), OrgID (string?), DepartmentName (string?), DepartmentDesc (string?),
//   FlagActive (string?), LogLUDTimeUTC (DateTime?), LogLUBy (string?).
//   Cột tính (không lưu bảng): ss_QtyStaff (0 do chưa port Staff_MapDepartment), md_DepartmentNameParent.
// ===========================================================================================

/// <summary>
/// Danh mục phòng ban — port 1:1 bảng <c>Mst_Department</c>.
/// Model nguồn: Mst_Department.cs:8-35. PK = DepartmentCode.
/// Không có DDL — kiểu cột cần xác nhận.
/// Đúng 13 cột: DepartmentCode, NetworkID, DepartmentCodeParent, DepartmentBUCode, DepartmentBUPattern,
/// DepartmentLevel, MST, OrgID, DepartmentName, DepartmentDesc, FlagActive, LogLUDTimeUTC, LogLUBy.
/// </summary>
public class Mst_Department
{
    public string DepartmentCode { get; set; } = "";    // Mã phòng ban — PK (khong co DDL — kieu cot can xac nhan)
    public string? NetworkID { get; set; }              // Mã mạng
    public string? DepartmentCodeParent { get; set; }    // Mã phòng ban cấp trên
    public string? DepartmentBUCode { get; set; }        // Mã đơn vị BU phòng ban
    public string? DepartmentBUPattern { get; set; }     // Mẫu BU phòng ban
    public int? DepartmentLevel { get; set; }           // Cấp phòng ban (kieu can xac nhan)
    public string? MST { get; set; }                    // Mã số thuế
    public string? OrgID { get; set; }                  // Mã tổ chức
    public string? DepartmentName { get; set; }         // Tên phòng ban
    public string? DepartmentDesc { get; set; }         // Mô tả phòng ban
    public string? FlagActive { get; set; }             // Trạng thái hoạt động ('1'/'0')
    public DateTime? LogLUDTimeUTC { get; set; }        // Thời điểm cập nhật UTC
    public string? LogLUBy { get; set; }                // Người cập nhật
}

/// <summary>Cấu hình EF cho <see cref="Mst_Department"/> — tên bảng Mst_Department + khoá chính DepartmentCode.</summary>
public class Mst_DepartmentConfig : IEntityTypeConfiguration<Mst_Department>
{
    public void Configure(EntityTypeBuilder<Mst_Department> b)
    {
        b.ToTable("Mst_Department");
        b.HasKey(x => x.DepartmentCode);                // PK = DepartmentCode (khong co DDL — kieu cot can xac nhan)
        b.Property(x => x.DepartmentCode).IsRequired();
    }
}

/// <summary>
/// Dữ liệu mẫu cho bảng Mst_Department. Idempotent: đã có dữ liệu thì bỏ qua.
/// Dữ liệu demo (comment "du lieu demo") — đúng theo đề bài P2:
///   - HQ: 'Trụ sở chính', parent null, BUCode 'HQ', BUPattern 'HQ%', Level 1, OrgID '4020320001', FlagActive '1'
///   - KD: 'Kinh doanh', parent 'HQ', BUCode 'HQ.KD', BUPattern 'HQ.KD%', Level 2, OrgID '4020320001', FlagActive '1'
///   - KT: 'Kỹ thuật', parent 'HQ', BUCode 'HQ.KT', BUPattern 'HQ.KT%', Level 2, OrgID '4020320002', FlagActive '1'
///   - OLD: 'Phòng cũ', parent null, BUCode 'OLD', BUPattern 'OLD%', Level 1, OrgID '4020320001', FlagActive '0'
/// Thứ tự Order => 32 (danh mục Mst_* dùng 10–99).
/// </summary>
public class Mst_DepartmentSeeder : IPortSeeder
{
    public int Order => 32;   // danh mục Mst_* dùng 10–99

    public async Task SeedAsync(AppDbContext db)
    {
        if (await db.Set<Mst_Department>().AnyAsync()) return;

        db.Set<Mst_Department>().AddRange(
            // Dữ liệu demo (comment "du lieu demo") — khong co DDL — kieu cot can xac nhan
            new Mst_Department
            {
                DepartmentCode = "HQ",
                DepartmentName = "Trụ sở chính",
                DepartmentCodeParent = null,
                DepartmentBUCode = "HQ",
                DepartmentBUPattern = "HQ%",
                DepartmentLevel = 1,
                OrgID = "4020320001",
                FlagActive = "1",
                NetworkID = "DEMO",
                LogLUBy = "seed"
            },
            new Mst_Department
            {
                DepartmentCode = "KD",
                DepartmentName = "Kinh doanh",
                DepartmentCodeParent = "HQ",
                DepartmentBUCode = "HQ.KD",
                DepartmentBUPattern = "HQ.KD%",
                DepartmentLevel = 2,
                OrgID = "4020320001",
                FlagActive = "1",
                NetworkID = "DEMO",
                LogLUBy = "seed"
            },
            new Mst_Department
            {
                DepartmentCode = "KT",
                DepartmentName = "Kỹ thuật",
                DepartmentCodeParent = "HQ",
                DepartmentBUCode = "HQ.KT",
                DepartmentBUPattern = "HQ.KT%",
                DepartmentLevel = 2,
                OrgID = "4020320002",
                FlagActive = "1",
                NetworkID = "DEMO",
                LogLUBy = "seed"
            },
            new Mst_Department
            {
                DepartmentCode = "OLD",
                DepartmentName = "Phòng cũ",
                DepartmentCodeParent = null,
                DepartmentBUCode = "OLD",
                DepartmentBUPattern = "OLD%",
                DepartmentLevel = 1,
                OrgID = "4020320001",
                FlagActive = "0",
                NetworkID = "DEMO",
                LogLUBy = "seed"
            }
        );
        await db.SaveChangesAsync();
    }
}
