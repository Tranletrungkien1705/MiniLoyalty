using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniLoyalty.Data;

namespace MiniLoyalty.Models.Port;

// ===========================================================================================
// PORT 1:1 từ dự án nguồn 2022.7.Loyalty — thực thể Req_MemberRegister và các bảng đính kèm.
//
// Nguồn tra cứu (chỉ đọc):
//   - Model cột: 12.Dev.Common/idn.Loyalty.Common/Models/Req_MemberRegister.cs
//   - DDL:       11.BackEnd/V10/05.Refs.Biz/Migrate/20231004.UpdDB.60.Member.sql (Req_MemberRegister, Req_MemberRegisterIDAttachFile, Req_MemberRegisterVehicleAttachFile, Req_MemberRegisterAttachFile)
// ===========================================================================================

/// <summary>
/// Yêu cầu đăng ký hội viên — port 1:1 bảng <c>Req_MemberRegister</c>.
/// </summary>
public class Req_MemberRegister
{
    public string ReqMemberRegisterCodeSys { get; set; } = ""; // Mã lượt đăng ký (PK)
    public string NetworkID { get; set; } = "";
    public string OrgID { get; set; } = "";
    public string ReqMemberRegisterCode { get; set; } = "";    // Mã lượt đăng ký User
    public string ReqMemberRegisterCodeOS { get; set; } = "";  // Mã lượt đăng ký OS
    public string DLCodeRegis { get; set; } = "";              // Đại lý đăng ký
    public string? RegisterDate { get; set; }                  // Ngày HV đăng ký (MyDateTime - string/datetime)
    public string VIN { get; set; } = "";                      // Số khung
    public string CarNo { get; set; } = "";                    // Biển số
    public string? TradeMarkName { get; set; }                 // Tên Hiệu xe
    public string? ModelName { get; set; }                     // Tên Model
    public string? CustomerName { get; set; }                  // Tên khách hàng
    public string? CustomerPhoneNo { get; set; }               // Số điện thoại
    public string? CustomerDateOfBirth { get; set; }           // Ngày sinh
    public string? CustomerIDNo { get; set; }                  // MST/CCCD
    public string? CustomerEmail { get; set; }                 // Email
    public string? CustomerAddress { get; set; }               // Địa chỉ
    public string? GenderCode { get; set; }                    // Mã Giới tính
    public string? GenderName { get; set; }                    // Tên Giới tính
    public string? ProvinceCode { get; set; }                  // Mã Tỉnh/TP
    public string? ProvinceName { get; set; }                  // Tên Tỉnh/TP
    public string? DistrictCode { get; set; }                  // Mã Quận/Huyện
    public string? DistrictName { get; set; }                  // Tên Mã Quận/Huyện
    public string? MemberNoIntro { get; set; }                 // Mã giới thiệu
    public string? FlagIsSyncDMS { get; set; }                 // Trạng thái đồng bộ DMS 0/1
    public string? SyncDMSDTime { get; set; }
    public string? SyncDMSError { get; set; }
    public string CreateDTimeUTC { get; set; } = "";
    public string CreateBy { get; set; } = "";
    public string LUDTimeUTC { get; set; } = "";
    public string LUBy { get; set; } = "";
    public string? ApproveDTimeUTC { get; set; }
    public string? ApproveBy { get; set; }
    public string? CancelDTimeUTC { get; set; }
    public string? CancelBy { get; set; }
    public string? RejectDTimeUTC { get; set; }
    public string? RejectBy { get; set; }
    public string? ReqMemberRegisterStatus { get; set; }       // Trạng thái yêu cầu
    public string? Remark { get; set; }
    public string? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }

    // Các cột join bổ sung từ tầng Biz Member.cs:630-640
    public string? cm_RegisFinishDTimeUTC { get; set; }
    public string? mdcp_DLCPCode { get; set; }
    public string? mdcp_DLCPName { get; set; }
    public string? mpv_ProvinceCode { get; set; }
    public string? mpv_ProvinceName { get; set; }
    public string? mdt_DistrictCode { get; set; }
    public string? mdt_DistrictName { get; set; }
}

public class Req_MemberRegisterConfig : IEntityTypeConfiguration<Req_MemberRegister>
{
    public void Configure(EntityTypeBuilder<Req_MemberRegister> b)
    {
        b.ToTable("Req_MemberRegister");
        b.HasKey(x => x.ReqMemberRegisterCodeSys);
        b.Property(x => x.ReqMemberRegisterCodeSys).IsRequired().HasMaxLength(50);
        b.Property(x => x.NetworkID).IsRequired().HasMaxLength(50);
        b.Property(x => x.OrgID).IsRequired().HasMaxLength(50);
        b.Property(x => x.ReqMemberRegisterCode).IsRequired().HasMaxLength(50);
        b.Property(x => x.ReqMemberRegisterCodeOS).IsRequired().HasMaxLength(50);
        b.Property(x => x.DLCodeRegis).IsRequired().HasMaxLength(50);
        b.Property(x => x.VIN).IsRequired().HasMaxLength(50);
        b.Property(x => x.CarNo).IsRequired().HasMaxLength(50);
        b.Property(x => x.CreateDTimeUTC).IsRequired();
        b.Property(x => x.CreateBy).IsRequired().HasMaxLength(50);
        b.Property(x => x.LUDTimeUTC).IsRequired();
        b.Property(x => x.LUBy).IsRequired().HasMaxLength(50);

        // Các thuộc tính join từ bảng khác không map trực tiếp vào cột DB của bảng Req_MemberRegister
        b.Ignore(x => x.cm_RegisFinishDTimeUTC);
        b.Ignore(x => x.mdcp_DLCPCode);
        b.Ignore(x => x.mdcp_DLCPName);
        b.Ignore(x => x.mpv_ProvinceCode);
        b.Ignore(x => x.mpv_ProvinceName);
        b.Ignore(x => x.mdt_DistrictCode);
        b.Ignore(x => x.mdt_DistrictName);
    }
}

/// <summary>
/// File đính kèm giấy tờ tuỳ thân — port 1:1 bảng <c>Req_MemberRegisterIDAttachFile</c>.
/// </summary>
public class Req_MemberRegisterIDAttachFile
{
    public string ReqMemberRegisterCodeSys { get; set; } = "";
    public int? Idx { get; set; }
    public string NetworkID { get; set; } = "";
    public string OrgID { get; set; } = "";
    public string FileType { get; set; } = "";
    public string FileName { get; set; } = "";
    public string? FilePath { get; set; }
    public string? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }
}

public class Req_MemberRegisterIDAttachFileConfig : IEntityTypeConfiguration<Req_MemberRegisterIDAttachFile>
{
    public void Configure(EntityTypeBuilder<Req_MemberRegisterIDAttachFile> b)
    {
        b.ToTable("Req_MemberRegisterIDAttachFile");
        b.HasKey(x => new { x.ReqMemberRegisterCodeSys, x.FileType, x.FileName });
        b.Property(x => x.ReqMemberRegisterCodeSys).IsRequired().HasMaxLength(50);
        b.Property(x => x.NetworkID).IsRequired().HasMaxLength(50);
        b.Property(x => x.OrgID).IsRequired().HasMaxLength(50);
        b.Property(x => x.FileType).IsRequired().HasMaxLength(50);
        b.Property(x => x.FileName).IsRequired().HasMaxLength(200);
    }
}

/// <summary>
/// File đính kèm xe — port 1:1 bảng <c>Req_MemberRegisterVehicleAttachFile</c>.
/// </summary>
public class Req_MemberRegisterVehicleAttachFile
{
    public string ReqMemberRegisterCodeSys { get; set; } = "";
    public int? Idx { get; set; }
    public string NetworkID { get; set; } = "";
    public string OrgID { get; set; } = "";
    public string FileType { get; set; } = "";
    public string FileName { get; set; } = "";
    public string? FilePath { get; set; }
    public string? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }
}

public class Req_MemberRegisterVehicleAttachFileConfig : IEntityTypeConfiguration<Req_MemberRegisterVehicleAttachFile>
{
    public void Configure(EntityTypeBuilder<Req_MemberRegisterVehicleAttachFile> b)
    {
        b.ToTable("Req_MemberRegisterVehicleAttachFile");
        b.HasKey(x => new { x.ReqMemberRegisterCodeSys, x.FileType, x.FileName });
        b.Property(x => x.ReqMemberRegisterCodeSys).IsRequired().HasMaxLength(50);
        b.Property(x => x.NetworkID).IsRequired().HasMaxLength(50);
        b.Property(x => x.OrgID).IsRequired().HasMaxLength(50);
        b.Property(x => x.FileType).IsRequired().HasMaxLength(50);
        b.Property(x => x.FileName).IsRequired().HasMaxLength(200);
    }
}

/// <summary>
/// File đính kèm khác — port 1:1 bảng <c>Req_MemberRegisterAttachFile</c>.
/// </summary>
public class Req_MemberRegisterAttachFile
{
    public string ReqMemberRegisterCodeSys { get; set; } = "";
    public int? Idx { get; set; }
    public string NetworkID { get; set; } = "";
    public string OrgID { get; set; } = "";
    public string FileType { get; set; } = "";
    public string FileName { get; set; } = "";
    public string? FilePath { get; set; }
    public string? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }
}

public class Req_MemberRegisterAttachFileConfig : IEntityTypeConfiguration<Req_MemberRegisterAttachFile>
{
    public void Configure(EntityTypeBuilder<Req_MemberRegisterAttachFile> b)
    {
        b.ToTable("Req_MemberRegisterAttachFile");
        b.HasKey(x => new { x.ReqMemberRegisterCodeSys, x.FileType, x.FileName });
        b.Property(x => x.ReqMemberRegisterCodeSys).IsRequired().HasMaxLength(50);
        b.Property(x => x.NetworkID).IsRequired().HasMaxLength(50);
        b.Property(x => x.OrgID).IsRequired().HasMaxLength(50);
        b.Property(x => x.FileType).IsRequired().HasMaxLength(50);
        b.Property(x => x.FileName).IsRequired().HasMaxLength(200);
    }
}

/// <summary>
/// Seeder dữ liệu mẫu cho bảng Req_MemberRegister và các bảng đính kèm.
/// Order => 75 (74 đã được dùng bởi Crd_DealUsePromotionGetForMemberNoSeeder).
/// </summary>
public class Req_MemberRegisterSeeder : IPortSeeder
{
    public int Order => 75;

    public async Task SeedAsync(AppDbContext db)
    {
        if (await db.Set<Req_MemberRegister>().AnyAsync()) return;

        const string net = "1001";
        const string org = "1001";
        const string user = "seed";

        var reg1 = new Req_MemberRegister
        {
            ReqMemberRegisterCodeSys = "REG20261017001",
            NetworkID = net,
            OrgID = org,
            ReqMemberRegisterCode = "REG001",
            ReqMemberRegisterCodeOS = "OS001",
            DLCodeRegis = "DL01",
            RegisterDate = "2026-10-17 08:00:00",
            VIN = "VIN1234567890",
            CarNo = "30A-12345",
            TradeMarkName = "Hyundai",
            ModelName = "SantaFe",
            CustomerName = "Nguyen Van A",
            CustomerPhoneNo = "0912345678",
            CustomerDateOfBirth = "1990-01-01",
            CustomerIDNo = "001090012345",
            CustomerEmail = "nguyenvana@example.com",
            CustomerAddress = "123 Tran Hung Dao",
            GenderCode = "MALE",
            GenderName = "Nam",
            ProvinceCode = "HN",
            ProvinceName = "Ha Noi",
            DistrictCode = "HN01",
            DistrictName = "Ba Dinh",
            MemberNoIntro = "",
            FlagIsSyncDMS = "0",
            CreateDTimeUTC = "2026-10-17 01:00:00",
            CreateBy = user,
            LUDTimeUTC = "2026-10-17 01:00:00",
            LUBy = user,
            ApproveDTimeUTC = "2026-10-17 02:00:00",
            ApproveBy = user,
            ReqMemberRegisterStatus = "APPROVE",
            Remark = "Test demo",
            LogLUDTimeUTC = "2026-10-17 01:00:00",
            LogLUBy = user
        };

        var idFile1 = new Req_MemberRegisterIDAttachFile
        {
            ReqMemberRegisterCodeSys = "REG20261017001",
            Idx = 1,
            NetworkID = net,
            OrgID = org,
            FileType = "CCCD_FRONT",
            FileName = "cccd_front.jpg",
            FilePath = "/upload/id/cccd_front.jpg",
            LogLUDTimeUTC = "2026-10-17 01:00:00",
            LogLUBy = user
        };

        var vehFile1 = new Req_MemberRegisterVehicleAttachFile
        {
            ReqMemberRegisterCodeSys = "REG20261017001",
            Idx = 1,
            NetworkID = net,
            OrgID = org,
            FileType = "REG_CERT",
            FileName = "dang_ky_xe.jpg",
            FilePath = "/upload/vehicle/dang_ky_xe.jpg",
            LogLUDTimeUTC = "2026-10-17 01:00:00",
            LogLUBy = user
        };

        var attachFile1 = new Req_MemberRegisterAttachFile
        {
            ReqMemberRegisterCodeSys = "REG20261017001",
            Idx = 1,
            NetworkID = net,
            OrgID = org,
            FileType = "OTHER",
            FileName = "hop_dong.pdf",
            FilePath = "/upload/other/hop_dong.pdf",
            LogLUDTimeUTC = "2026-10-17 01:00:00",
            LogLUBy = user
        };

        await db.Set<Req_MemberRegister>().AddAsync(reg1);
        await db.Set<Req_MemberRegisterIDAttachFile>().AddAsync(idFile1);
        await db.Set<Req_MemberRegisterVehicleAttachFile>().AddAsync(vehFile1);
        await db.Set<Req_MemberRegisterAttachFile>().AddAsync(attachFile1);
        await db.SaveChangesAsync();
    }
}

/// <summary>Seeder cho ReqMemberRegister Approve/ApproveOld/Cancel/Reject (Order => 80).</summary>
public class Req_MemberRegisterStatusSeeder : IPortSeeder
{
    public int Order => 80;

    public async Task SeedAsync(AppDbContext db)
    {
        if (await db.Set<Req_MemberRegister>().AnyAsync(x => x.ReqMemberRegisterCodeSys == "REG_S1")) return;
        Req_MemberRegister Mk(string sys, string status, string vin, string car) => new Req_MemberRegister
        {
            ReqMemberRegisterCodeSys = sys, NetworkID = "1001", OrgID = "1001", ReqMemberRegisterCode = sys, ReqMemberRegisterCodeOS = "OS_" + sys,
            DLCodeRegis = "DL01", RegisterDate = "2026-10-17 08:00:00", VIN = vin, CarNo = car, CustomerName = "Seed " + sys, CustomerPhoneNo = "0900000000",
            CreateDTimeUTC = "2026-10-17 01:00:00", CreateBy = "seed", LUDTimeUTC = "2026-10-17 01:00:00", LUBy = "seed",
            ReqMemberRegisterStatus = status, LogLUDTimeUTC = "2026-10-17 01:00:00", LogLUBy = "seed"
        };
        db.Set<Req_MemberRegister>().AddRange(Mk("REG_S1", "PENDING", "VINS1", "CARS1"), Mk("REG_S2", "PENDING", "VINS2", "CARS2"),
            Mk("REG_S3", "PENDING", "VINS3", "CARS3"), Mk("REG_S4", "APPROVE", "VINS4", "CARS4"), Mk("REG_S5", "PENDING", "VIN4301", "CARS5"));
        await db.SaveChangesAsync();
    }
}
