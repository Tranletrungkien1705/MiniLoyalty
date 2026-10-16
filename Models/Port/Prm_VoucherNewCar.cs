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
    [NotMapped]
    public double? PointActualVCAllModel { get; set; }
    [NotMapped]
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
