using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniLoyalty.Data;

namespace MiniLoyalty.Models.Port;

/// <summary>
/// Port 1:1 entity Prm_VoucherNewCar theo DDL 20250912.CreTb.Prm_VoucherNewCar.sql:53-80
/// và idn.Loyalty.Common.Models.Prm_VoucherNewCar.cs:9-38.
/// </summary>
public class Prm_VoucherNewCar
{
    public string PrmVoucherCode { get; set; } = ""; // Mã chương trình (PK)
    public string NetworkID { get; set; } = "";
    public string OrgID { get; set; } = "";
    public string PrmVoucherName { get; set; } = ""; // Tên chương trình
    public string EffDateStart { get; set; } = ""; // Ngày hiệu lực từ (yyyy-MM-dd)
    public string EffDateEnd { get; set; } = ""; // Ngày hiệu lực đến (yyyy-MM-dd)
    public double? QtyDayLimitFDlvDate { get; set; } // Số Ngày giới hạn từ ngày giao xe
    public double? ValidityPeriod { get; set; } // Thời hạn Voucher
    public string? FlagAllModel { get; set; } // Cờ áp dụng tất cả model
    public double? PointVoucherAllModel { get; set; } // Điểm tặng xe mới cho all Model - hạng sử dụng
    public double? PointUseLimitAllModel { get; set; } // Điểm sử dụng tối đa trên lần dịch vụ cho all Model
    public DateTime CreateDTimeUTC { get; set; } // Thời điểm tạo
    public string CreateBy { get; set; } = ""; // Người tạo
    public DateTime? LUDTimeUTC { get; set; }
    public string? LUBy { get; set; }
    public DateTime? ApprDTimeUTC { get; set; } // Thời điểm duyệt A
    public string? ApprBy { get; set; } // Người duyệt A
    public DateTime? FinishDTimeUTC { get; set; } // Thời điểm duyệt F
    public string? FinishBy { get; set; } // Người duyệt F
    public DateTime? CancelDTimeUTC { get; set; } // Thời điểm từ chối
    public string? CancelBy { get; set; } // Người từ chối
    public string? PrmVoucherStatus { get; set; } // Trạng thái phê duyệt
    public string? Remark { get; set; } // Mô tả
    public DateTime? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }

    [NotMapped]
    public long? MyIdxSeq { get; set; }
    [NotMapped]
    public string? FlagShow { get; set; } // Trạng thái chương trình (1/0)
    public double? PointActualVCAllModel { get; set; }
    public double? QtyUseLimitAllModel { get; set; }
    [NotMapped]
    public double? PointVoucherCalc { get; set; }
}

public class Prm_VoucherNewCarConfig : IEntityTypeConfiguration<Prm_VoucherNewCar>
{
    public void Configure(EntityTypeBuilder<Prm_VoucherNewCar> b)
    {
        b.ToTable("Prm_VoucherNewCar");
        b.HasKey(x => x.PrmVoucherCode);
        b.Property(x => x.PrmVoucherCode).IsRequired();
        b.Property(x => x.NetworkID).IsRequired();
        b.Property(x => x.OrgID).IsRequired();
        b.Property(x => x.PrmVoucherName).IsRequired();
        b.Property(x => x.EffDateStart).IsRequired();
        b.Property(x => x.EffDateEnd).IsRequired();
        b.Property(x => x.CreateBy).IsRequired();
    }
}

/// <summary>
/// Seeder dữ liệu mẫu cho Prm_VoucherNewCar.
/// Order => 62 (chọn trong dải 10-99 chưa dùng theo bài học A.4, 45 đã bị Mst_Position chiếm).
/// </summary>
public class Prm_VoucherNewCarSeeder : IPortSeeder
{
    public int Order => 62;

    public async Task SeedAsync(AppDbContext db)
    {
        if (!await db.Set<Prm_VoucherNewCar>().AnyAsync())
        {
            db.Set<Prm_VoucherNewCar>().AddRange(
                new Prm_VoucherNewCar
                {
                    PrmVoucherCode = "PVNC01",
                    NetworkID = "DEMO",
                    OrgID = "ORG01",
                    PrmVoucherName = "Chương trình voucher xe mới 01",
                    EffDateStart = "2026-01-01",
                    EffDateEnd = "2026-12-31",
                    QtyDayLimitFDlvDate = 30,
                    ValidityPeriod = 90,
                    FlagAllModel = "1",
                    PointVoucherAllModel = 1000,
                    PointUseLimitAllModel = 500,
                    CreateDTimeUTC = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
                    CreateBy = "seed",
                    PrmVoucherStatus = "FINISH",
                    LogLUBy = "seed",
                    LogLUDTimeUTC = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Prm_VoucherNewCar
                {
                    PrmVoucherCode = "PVNC02",
                    NetworkID = "DEMO",
                    OrgID = "ORG01",
                    PrmVoucherName = "Chương trình voucher xe mới 02",
                    EffDateStart = "2025-01-01",
                    EffDateEnd = "2025-12-31",
                    QtyDayLimitFDlvDate = 15,
                    ValidityPeriod = 60,
                    FlagAllModel = "1",
                    PointVoucherAllModel = 500,
                    PointUseLimitAllModel = 200,
                    CreateDTimeUTC = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
                    CreateBy = "seed",
                    PrmVoucherStatus = "FINISH",
                    LogLUBy = "seed",
                    LogLUDTimeUTC = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Prm_VoucherNewCar
                {
                    PrmVoucherCode = "PVNC03",
                    NetworkID = "DEMO",
                    OrgID = "ORG01",
                    PrmVoucherName = "Chương trình voucher xe mới 03",
                    EffDateStart = "2026-10-01",
                    EffDateEnd = "2100-01-01",
                    QtyDayLimitFDlvDate = 45,
                    ValidityPeriod = 120,
                    FlagAllModel = "0",
                    PointVoucherAllModel = 2000,
                    PointUseLimitAllModel = 1000,
                    CreateDTimeUTC = new DateTime(2026, 10, 10, 0, 0, 0, DateTimeKind.Utc),
                    CreateBy = "seed",
                    PrmVoucherStatus = "NEW",
                    LogLUBy = "seed",
                    LogLUDTimeUTC = new DateTime(2026, 10, 10, 0, 0, 0, DateTimeKind.Utc)
                }
            );
            await db.SaveChangesAsync();
        }
    }
}

/// <summary>
/// Port 1:1 bảng Prm_VoucherNewCarDtl — DDL 20250912.CreTb.Prm_VoucherNewCar.sql:31-47 (+ PointActualVoucher, QtyUseLimit theo Prm_VoucherNewCarDtl.cs). PK (PrmVoucherCode, Idx).
/// </summary>
public class Prm_VoucherNewCarDtl
{
    public string PrmVoucherCode { get; set; } = "";
    public long Idx { get; set; }
    public string NetworkID { get; set; } = "";
    public double? PointVoucher { get; set; }
    public double? PointUseLimit { get; set; }
    public double? PointActualVoucher { get; set; }
    public double? QtyUseLimit { get; set; }
    public string? Remark { get; set; }
    public DateTime? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }
}

public class Prm_VoucherNewCarDtlConfig : IEntityTypeConfiguration<Prm_VoucherNewCarDtl>
{
    public void Configure(EntityTypeBuilder<Prm_VoucherNewCarDtl> b)
    {
        b.ToTable("Prm_VoucherNewCarDtl");
        b.HasKey(x => new { x.PrmVoucherCode, x.Idx });
    }
}

/// <summary>
/// Port 1:1 bảng Prm_VoucherNewCarSpec — DDL 20250912.CreTb.Prm_VoucherNewCar.sql:11-25. PK (PrmVoucherCode, Idx, ModelCode).
/// </summary>
public class Prm_VoucherNewCarSpec
{
    public string PrmVoucherCode { get; set; } = "";
    public long Idx { get; set; }
    public string ModelCode { get; set; } = "";
    public string NetworkID { get; set; } = "";
    public DateTime? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }
}

public class Prm_VoucherNewCarSpecConfig : IEntityTypeConfiguration<Prm_VoucherNewCarSpec>
{
    public void Configure(EntityTypeBuilder<Prm_VoucherNewCarSpec> b)
    {
        b.ToTable("Prm_VoucherNewCarSpec");
        b.HasKey(x => new { x.PrmVoucherCode, x.Idx, x.ModelCode });
    }
}

/// <summary>Bảng sinh sequence cho PrmVoucherCode — Seq.cs:144 (Seq_PrmVoucherCode, "{0}PRMVC.{3}.{1:00000}{2}").</summary>
public class Seq_PrmVoucherCode
{
    public long AutoId { get; set; }
    public string? Remark { get; set; }
}

public class Seq_PrmVoucherCodeConfig : IEntityTypeConfiguration<Seq_PrmVoucherCode>
{
    public void Configure(EntityTypeBuilder<Seq_PrmVoucherCode> b)
    {
        b.ToTable("Seq_PrmVoucherCode");
        b.HasKey(x => x.AutoId);
        b.Property(x => x.AutoId).ValueGeneratedOnAdd();
    }
}

/// <summary>Seeder cho Save/Approve/Finish/Delete/Cancel của Prm_VoucherNewCar (Order => 70).</summary>
public class Prm_VoucherNewCarStatusSeeder : IPortSeeder
{
    public int Order => 70;

    public async Task SeedAsync(AppDbContext db)
    {
        if (await db.Set<Prm_VoucherNewCar>().AnyAsync(x => x.PrmVoucherCode == "PVC_P1")) return;
        var now = new DateTime(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc);
        Prm_VoucherNewCar Mk(string code, string status, string start) => new Prm_VoucherNewCar
        {
            PrmVoucherCode = code, NetworkID = "DEMO", OrgID = "ORG01", PrmVoucherName = "Voucher " + code,
            EffDateStart = start, EffDateEnd = "2100-01-01", QtyDayLimitFDlvDate = 30, ValidityPeriod = 90, FlagAllModel = "0",
            PointVoucherAllModel = 100, PointUseLimitAllModel = 50, PointActualVCAllModel = 100, QtyUseLimitAllModel = 1,
            PrmVoucherStatus = status, CreateDTimeUTC = now, CreateBy = "seed", LUDTimeUTC = now, LUBy = "seed", LogLUDTimeUTC = now, LogLUBy = "seed"
        };
        db.Set<Prm_VoucherNewCar>().AddRange(Mk("PVC_P1", "PENDING", "2030-01-01"), Mk("PVC_P2", "PENDING", "2030-02-01"), Mk("PVC_A1", "APPROVE", "2030-03-01"), Mk("PVC_F1", "FINISH", "2030-04-01"));
        db.Set<Prm_VoucherNewCarDtl>().Add(new Prm_VoucherNewCarDtl { PrmVoucherCode = "PVC_P2", Idx = 1, NetworkID = "DEMO", PointVoucher = 100, PointUseLimit = 50, PointActualVoucher = 100, QtyUseLimit = 1, LogLUDTimeUTC = now, LogLUBy = "seed" });
        db.Set<Prm_VoucherNewCarSpec>().Add(new Prm_VoucherNewCarSpec { PrmVoucherCode = "PVC_P2", Idx = 1, ModelCode = "M01", NetworkID = "DEMO", LogLUDTimeUTC = now, LogLUBy = "seed" });
        await db.SaveChangesAsync();
    }
}
