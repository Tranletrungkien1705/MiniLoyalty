using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniLoyalty.Data;

namespace MiniLoyalty.Models.Port;

/// <summary>
/// Model Crd_MemberChangeInfoDtl — port 1:1 từ Crd_MemberChangeInfoDtl.cs:1-62 (#029).
/// Không có DDL trong repo — kiểu cột cần xác nhận.
/// Bảng DB gồm 10 cột, khoá chính ghép: (RequestNo, ColumnCode).
/// Kèm 8 cột tra cứu/join danh mục: ProvinceNameOld, ProvinceNameNew, DistrictNameOld, DistrictNameNew,
/// ModelNameOld, ModelNameNew, BrandNameOld, BrandNameNew (Card.cs:28171-28208).
/// Tổng cộng 18 thuộc tính.
/// </summary>
public class Crd_MemberChangeInfoDtl
{
    public string RequestNo { get; set; } = ""; // Số đề nghị (PK ghép, Card.cs:28186)
    public string? NetworkID { get; set; }
    public string? OrgID { get; set; }
    public string ColumnCode { get; set; } = ""; // Trường thông tin thay đổi (PK ghép)
    public string? ColumnValueOld { get; set; } // Giá trị cũ
    public string? ColumnValueNew { get; set; } // Giá trị mới
    public string? Remark { get; set; } // Ghi chú
    public string? RequestDtlStatus { get; set; } // Trạng thái chi tiết ĐN
    public string? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }

    // 8 cột tra cứu / join danh mục (Card.cs:28176-28203):
    [NotMapped]
    public string? ProvinceNameOld { get; set; } // Tên tỉnh cũ (Card.cs:28176, 28189)
    [NotMapped]
    public string? ProvinceNameNew { get; set; } // Tên tỉnh mới (Card.cs:28177, 28191)
    [NotMapped]
    public string? DistrictNameOld { get; set; } // Tên huyện cũ (Card.cs:28178, 28193)
    [NotMapped]
    public string? DistrictNameNew { get; set; } // Tên huyện mới (Card.cs:28179, 28195)
    [NotMapped]
    public string? ModelNameNew { get; set; } // Tên Model mới (Card.cs:28180, 28197)
    [NotMapped]
    public string? ModelNameOld { get; set; } // Tên Model cũ — lỗi nguồn giữ nguyên: join mcmo trên ColumnValueNew (Card.cs:28181, 28199)
    [NotMapped]
    public string? BrandNameOld { get; set; } // Tên Brand cũ — lỗi nguồn giữ nguyên: join mcbo trên ColumnValueNew (Card.cs:28182, 28201)
    [NotMapped]
    public string? BrandNameNew { get; set; } // Tên Brand mới (Card.cs:28183, 28203)

    // 22 thuộc tính bổ sung từ Crd_MemberChangeInfo (đề nghị) để FE chỉ đọc 1 bảng Dtl (Gate :392-425, Model :38-60)
    [NotMapped]
    public string? MemberNo { get; set; } // Mã hội viên
    [NotMapped]
    public string? DLCodeRequest { get; set; } // Đại lý đề nghị
    [NotMapped]
    public string? RequestType { get; set; } // Loại đề nghị
    [NotMapped]
    public string? RequestStatus { get; set; } // Trạng thái đề nghị
    [NotMapped]
    public string? CreateDTimeUTC { get; set; } // Thời gian tạo
    [NotMapped]
    public string? CreateBy { get; set; } // Người tạo
    [NotMapped]
    public string? LUDTimeUTC { get; set; } // Thời gian cập nhật
    [NotMapped]
    public string? LUBy { get; set; } // Người cập nhật
    [NotMapped]
    public string? ApproveDTimeUTC { get; set; } // Thời gian duyệt A
    [NotMapped]
    public string? ApproveBy { get; set; } // Người duyệt A
    [NotMapped]
    public string? FinishDTimeUTC { get; set; } // Thời gian duyệt F
    [NotMapped]
    public string? FinishBy { get; set; } // Người duyệt F
    [NotMapped]
    public string? RejectDTimeUTC { get; set; } // Thời gian từ chối
    [NotMapped]
    public string? RejectBy { get; set; } // Người từ chối
    [NotMapped]
    public string? ApproveDLCode { get; set; } // Đơn vị duyệt
    [NotMapped]
    public string? FlagApproveAF { get; set; } // Cờ được duyệt A/F (1/0)
    [NotMapped]
    public string? cm_MemberName { get; set; } // Tên hội viên
    [NotMapped]
    public string? cm_CarNo { get; set; } // Biển số xe
    [NotMapped]
    public string? cm_PhoneNo { get; set; } // Số điện thoại
    [NotMapped]
    public string? cm_VIN { get; set; } // Số khung (VIN)
    [NotMapped]
    public double? cm_AmountEnd { get; set; } // Số dư điểm
    [NotMapped]
    public string? cm_CardTypeUse { get; set; } // Hạng thẻ sử dụng
}

/// <summary>
/// Cấu hình EF cho Crd_MemberChangeInfoDtl: tên bảng Crd_MemberChangeInfoDtl,
/// khoá chính ghép (RequestNo, ColumnCode).
/// </summary>
public class Crd_MemberChangeInfoDtlConfig : IEntityTypeConfiguration<Crd_MemberChangeInfoDtl>
{
    public void Configure(EntityTypeBuilder<Crd_MemberChangeInfoDtl> b)
    {
        b.ToTable("Crd_MemberChangeInfoDtl");
        b.HasKey(x => new { x.RequestNo, x.ColumnCode });
        b.Property(x => x.RequestNo).IsRequired();
        b.Property(x => x.ColumnCode).IsRequired();

        // Bỏ qua các cột tra cứu / join không có trong bảng DB
        b.Ignore(x => x.ProvinceNameOld);
        b.Ignore(x => x.ProvinceNameNew);
        b.Ignore(x => x.DistrictNameOld);
        b.Ignore(x => x.DistrictNameNew);
        b.Ignore(x => x.ModelNameOld);
        b.Ignore(x => x.ModelNameNew);
        b.Ignore(x => x.BrandNameOld);
        b.Ignore(x => x.BrandNameNew);

        // Bỏ qua 22 thuộc tính mở rộng từ Crd_MemberChangeInfo
        b.Ignore(x => x.MemberNo);
        b.Ignore(x => x.DLCodeRequest);
        b.Ignore(x => x.RequestType);
        b.Ignore(x => x.RequestStatus);
        b.Ignore(x => x.CreateDTimeUTC);
        b.Ignore(x => x.CreateBy);
        b.Ignore(x => x.LUDTimeUTC);
        b.Ignore(x => x.LUBy);
        b.Ignore(x => x.ApproveDTimeUTC);
        b.Ignore(x => x.ApproveBy);
        b.Ignore(x => x.FinishDTimeUTC);
        b.Ignore(x => x.FinishBy);
        b.Ignore(x => x.RejectDTimeUTC);
        b.Ignore(x => x.RejectBy);
        b.Ignore(x => x.ApproveDLCode);
        b.Ignore(x => x.FlagApproveAF);
        b.Ignore(x => x.cm_MemberName);
        b.Ignore(x => x.cm_CarNo);
        b.Ignore(x => x.cm_PhoneNo);
        b.Ignore(x => x.cm_VIN);
        b.Ignore(x => x.cm_AmountEnd);
        b.Ignore(x => x.cm_CardTypeUse);
    }
}

/// <summary>
/// Seeder dữ liệu mẫu cho Crd_MemberChangeInfoDtl (Order => 96 theo đề bài #029, bổ sung #030).
/// Idempotent, demo.
/// </summary>
public class Crd_MemberChangeInfoDtlSeeder : IPortSeeder
{
    public int Order => 96;

    public async Task SeedAsync(AppDbContext db)
    {
        if (!await db.Set<Crd_MemberChangeInfoDtl>().AnyAsync(d => d.RequestNo == "REQ01"))
        {
            db.Set<Crd_MemberChangeInfoDtl>().AddRange(
                // REQ01 / PhoneNo: ColumnValueOld "0901234567", ColumnValueNew "0912345678", Remark "Thay doi SDT", RequestDtlStatus "PENDING"
                new Crd_MemberChangeInfoDtl
                {
                    RequestNo = "REQ01",
                    ColumnCode = "PhoneNo",
                    ColumnValueOld = "0901234567",
                    ColumnValueNew = "0912345678",
                    Remark = "Thay doi SDT",
                    RequestDtlStatus = "PENDING",
                    NetworkID = "1001",
                    OrgID = "ORG01",
                    LogLUBy = "seed",
                    LogLUDTimeUTC = "2026-08-01 08:00:00"
                },
                // REQ01 / ProvinceCode: ColumnValueOld "HN", ColumnValueNew "HP", Remark "Chuyen tinh", RequestDtlStatus "PENDING"
                new Crd_MemberChangeInfoDtl
                {
                    RequestNo = "REQ01",
                    ColumnCode = "ProvinceCode",
                    ColumnValueOld = "HN",
                    ColumnValueNew = "HP",
                    Remark = "Chuyen tinh",
                    RequestDtlStatus = "PENDING",
                    NetworkID = "1001",
                    OrgID = "ORG01",
                    LogLUBy = "seed",
                    LogLUDTimeUTC = "2026-08-01 08:00:00"
                },
                // REQ02 / DistrictCode: ColumnValueOld "HN01", ColumnValueNew "HN02", Remark "Chuyen quan", RequestDtlStatus "APPROVE"
                new Crd_MemberChangeInfoDtl
                {
                    RequestNo = "REQ02",
                    ColumnCode = "DistrictCode",
                    ColumnValueOld = "HN01",
                    ColumnValueNew = "HN02",
                    Remark = "Chuyen quan",
                    RequestDtlStatus = "APPROVE",
                    NetworkID = "1001",
                    OrgID = "ORG01",
                    LogLUBy = "seed",
                    LogLUDTimeUTC = "2026-08-05 09:00:00"
                }
            );
        }

        // Seed dữ liệu phục vụ GetHistoryByMember (#030)
        if (!await db.Set<Crd_MemberChangeInfoDtl>().AnyAsync(d => d.RequestNo == "REQ10"))
        {
            db.Set<Crd_MemberChangeInfoDtl>().AddRange(
                // REQ10: ColumnCode 'PhoneNo', Old '0901234567', New '0909999999', Remark 'Sua SDT', RequestDtlStatus 'FINISH'
                new Crd_MemberChangeInfoDtl
                {
                    RequestNo = "REQ10",
                    ColumnCode = "PhoneNo",
                    ColumnValueOld = "0901234567",
                    ColumnValueNew = "0909999999",
                    Remark = "Sua SDT",
                    RequestDtlStatus = "FINISH",
                    NetworkID = "1001",
                    OrgID = "ORG01",
                    LogLUBy = "seed",
                    LogLUDTimeUTC = "2026-08-10 10:00:00"
                },
                // REQ10: ColumnCode 'ProvinceCode', Old 'HN', New 'HP', Remark 'Doi tinh', RequestDtlStatus 'FINISH'
                new Crd_MemberChangeInfoDtl
                {
                    RequestNo = "REQ10",
                    ColumnCode = "ProvinceCode",
                    ColumnValueOld = "HN",
                    ColumnValueNew = "HP",
                    Remark = "Doi tinh",
                    RequestDtlStatus = "FINISH",
                    NetworkID = "1001",
                    OrgID = "ORG01",
                    LogLUBy = "seed",
                    LogLUDTimeUTC = "2026-08-10 10:00:00"
                },
                // REQ11: ColumnCode 'CarNo', Old '30A-12345', New '30A-99999', Remark 'Doi bien so', RequestDtlStatus 'FINISH'
                new Crd_MemberChangeInfoDtl
                {
                    RequestNo = "REQ11",
                    ColumnCode = "CarNo",
                    ColumnValueOld = "30A-12345",
                    ColumnValueNew = "30A-99999",
                    Remark = "Doi bien so",
                    RequestDtlStatus = "FINISH",
                    NetworkID = "1001",
                    OrgID = "ORG01",
                    LogLUBy = "seed",
                    LogLUDTimeUTC = "2026-08-11 05:00:00"
                },
                // REQ12: ColumnCode 'DistrictCode', Old 'HN01', New 'HN02', Remark 'Doi quan', RequestDtlStatus 'FINISH'
                new Crd_MemberChangeInfoDtl
                {
                    RequestNo = "REQ12",
                    ColumnCode = "DistrictCode",
                    ColumnValueOld = "HN01",
                    ColumnValueNew = "HN02",
                    Remark = "Doi quan",
                    RequestDtlStatus = "FINISH",
                    NetworkID = "1001",
                    OrgID = "ORG01",
                    LogLUBy = "seed",
                    LogLUDTimeUTC = "2026-08-12 08:00:00"
                }
            );
        }

        await db.SaveChangesAsync();
    }
}
