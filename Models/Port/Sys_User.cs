namespace MiniLoyalty.Models.Port;

using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniLoyalty.Data;

/// <summary>
/// Thực thể người dùng hệ thống (Sys_User) - port 1:1 từ idn.Loyalty.Common.Models.Sys_User.cs:9-84.
/// PK = UserCode.
/// </summary>
public class Sys_User
{
    /// <summary>Mã người dùng (PK) - Sys_User.cs:12</summary>
    public string UserCode { get; set; } = "";

    /// <summary>Mã Network - Sys_User.cs:14</summary>
    public string? NetworkID { get; set; }

    /// <summary>Tên người dùng - Sys_User.cs:16</summary>
    public string? UserName { get; set; }

    /// <summary>Mật khẩu - Sys_User.cs:18 (truy vấn trả về TConst.BizMix.Default_PasswordMask '*********')</summary>
    public string? UserPassword { get; set; }

    /// <summary>Mật khẩu mới - Sys_User.cs:20</summary>
    public string? UserPasswordNew { get; set; }

    /// <summary>Số điện thoại - Sys_User.cs:22</summary>
    public string? PhoneNo { get; set; }

    /// <summary>Email - Sys_User.cs:24</summary>
    public string? EMail { get; set; }

    /// <summary>Mã số thuế - Sys_User.cs:26</summary>
    public string? MST { get; set; }

    /// <summary>Mã tổ chức - Sys_User.cs:28</summary>
    public string? OrganCode { get; set; }

    /// <summary>Mã phòng ban - Sys_User.cs:30</summary>
    public string? DepartmentCode { get; set; }

    /// <summary>Vị trí - Sys_User.cs:32</summary>
    public string? Position { get; set; }

    /// <summary>Mã xác thực - Sys_User.cs:34</summary>
    public string? VerificationCode { get; set; }

    /// <summary>Ảnh đại diện - Sys_User.cs:36</summary>
    public string? Avatar { get; set; }

    /// <summary>UUID - Sys_User.cs:38</summary>
    public string? UUID { get; set; }

    /// <summary>Cờ quản trị đại lý - Sys_User.cs:40</summary>
    public string? FlagDLAdmin { get; set; }

    /// <summary>Cờ quản trị hệ thống - Sys_User.cs:42</summary>
    public string? FlagSysAdmin { get; set; }

    /// <summary>Cờ quản trị NNT - Sys_User.cs:44</summary>
    public string? FlagNNTAdmin { get; set; }

    /// <summary>Mã tổ chức OrgID - Sys_User.cs:45</summary>
    public string? OrgID { get; set; }

    /// <summary>Mã hệ thống khách hàng CustomerCodeSys - Sys_User.cs:46</summary>
    public string? CustomerCodeSys { get; set; }

    /// <summary>Mã khách hàng - Sys_User.cs:47 (lấy từ Mst_Customer qua left join mc.CustomerCodeSys)</summary>
    public string? CustomerCode { get; set; }

    /// <summary>Tên khách hàng - Sys_User.cs:48 (lấy từ Mst_Customer qua left join mc.CustomerCodeSys)</summary>
    public string? CustomerName { get; set; }

    /// <summary>Trạng thái kích hoạt ('1' = hoạt động, '0' = dừng) - Sys_User.cs:49</summary>
    public string? FlagActive { get; set; }

    /// <summary>Thời gian cập nhật UTC - Sys_User.cs:51</summary>
    public DateTime? LogLUDTimeUTC { get; set; }

    /// <summary>Người cập nhật - Sys_User.cs:53</summary>
    public string? LogLUBy { get; set; }

    /// <summary>ACId tài khoản inos - Sys_User.cs:55</summary>
    public string? ACId { get; set; }

    /// <summary>ACAvatar tài khoản inos - Sys_User.cs:57</summary>
    public string? ACAvatar { get; set; }

    /// <summary>ACEmail tài khoản inos - Sys_User.cs:59</summary>
    public string? ACEmail { get; set; }

    /// <summary>ACLanguage ngôn ngữ inos - Sys_User.cs:61</summary>
    public string? ACLanguage { get; set; }

    /// <summary>ACName tên tài khoản inos - Sys_User.cs:63</summary>
    public string? ACName { get; set; }

    /// <summary>ACPhone điện thoại inos - Sys_User.cs:65</summary>
    public string? ACPhone { get; set; }

    /// <summary>ACTimeZone múi giờ inos - Sys_User.cs:67</summary>
    public string? ACTimeZone { get; set; }

    // Các trường mở rộng / join (Sys_User.cs:70-84):
    public string? mo_OrganCode { get; set; }
    public string? mo_OrganName { get; set; }
    public string? mdept_DepartmentCode { get; set; }
    public string? mdept_DepartmentName { get; set; }
    public string? mnnt_DealerType { get; set; }
    public string? ctitctg_CustomerGrpCode { get; set; }
    public string? mdcpo_DLCPCode { get; set; }
    public string? mdcp_DLCPName { get; set; }
    public string? mdcp_FlagRoot { get; set; }

    /// <summary>Thứ tự dòng trong tập kết quả phân trang (MyIdxSeq - System.cs:8166, Bài học D.50 QA-qa1-6)</summary>
    public long? MyIdxSeq { get; set; }
}

/// <summary>Cấu hình ánh xạ EntityType cho bảng Sys_User.</summary>
public class Sys_UserConfiguration : IEntityTypeConfiguration<Sys_User>
{
    public void Configure(EntityTypeBuilder<Sys_User> builder)
    {
        builder.ToTable("Sys_User");
        builder.HasKey(x => x.UserCode);
        builder.Property(x => x.UserCode).IsRequired();
    }
}

/// <summary>
/// Bảng Mst_Customer (danh mục khách hàng) - phục vụ left join mc.CustomerCodeSys (System.cs:8200-8201).
/// Model nguồn: Mst_Customer.cs:11-72. PK = CustomerCodeSys.
/// </summary>
public class Mst_Customer
{
    /// <summary>Mã hệ thống của khách hàng (PK) - Mst_Customer.cs:14</summary>
    public string CustomerCodeSys { get; set; } = "";

    /// <summary>Mã tổ chức - Mst_Customer.cs:13</summary>
    public string? OrgID { get; set; }

    /// <summary>Mã Network - Mst_Customer.cs:15</summary>
    public string? NetworkID { get; set; }

    /// <summary>Mã hiển thị khách hàng - Mst_Customer.cs:16</summary>
    public string? CustomerCode { get; set; }

    /// <summary>Tên khách hàng - Mst_Customer.cs:20</summary>
    public string? CustomerName { get; set; }

    /// <summary>Loại khách hàng - Mst_Customer.cs:17</summary>
    public string? CustomerType { get; set; }

    /// <summary>Mã nhóm khách hàng - Mst_Customer.cs:18</summary>
    public string? CustomerGrpCode { get; set; }

    /// <summary>Số điện thoại - Mst_Customer.cs:23</summary>
    public string? CustomerPhoneNo { get; set; }

    /// <summary>Số di động - Mst_Customer.cs:24</summary>
    public string? CustomerMobilePhone { get; set; }

    /// <summary>Email khách hàng - Mst_Customer.cs:34</summary>
    public string? CustomerEmail { get; set; }

    /// <summary>Địa chỉ khách hàng - Mst_Customer.cs:33</summary>
    public string? CustomerAddress { get; set; }

    /// <summary>Mã số thuế - Mst_Customer.cs:56</summary>
    public string? MST { get; set; }

    /// <summary>Trạng thái kích hoạt ('1' = hoạt động, '0' = tạm ngừng) - Mst_Customer.cs:68</summary>
    public string? FlagActive { get; set; }

    /// <summary>Thời gian cập nhật UTC - Mst_Customer.cs:71</summary>
    public DateTime? LogLUDTimeUTC { get; set; }

    /// <summary>Người cập nhật - Mst_Customer.cs:72</summary>
    public string? LogLUBy { get; set; }
}

/// <summary>Cấu hình ánh xạ EntityType cho bảng Mst_Customer.</summary>
public class Mst_CustomerConfiguration : IEntityTypeConfiguration<Mst_Customer>
{
    public void Configure(EntityTypeBuilder<Mst_Customer> builder)
    {
        builder.ToTable("Mst_Customer");
        builder.HasKey(x => x.CustomerCodeSys);
        builder.Property(x => x.CustomerCodeSys).IsRequired();
    }
}

/// <summary>
/// Dữ liệu mẫu cho bảng Sys_User và các bảng liên quan theo đề bài #323. Idempotent.
/// Order => 49 (đề bài yêu cầu 42 nhưng 42 đã được Mst_MajorSeeder sử dụng, tuân thủ bài học A.4 không đặt trùng).
/// </summary>
public class Sys_UserSeeder : IPortSeeder
{
    public int Order => 49; // Đề bài #323 yêu cầu Order => 42, theo bài học A.4 chọn 49 chưa dùng trong 10-99

    public async Task SeedAsync(AppDbContext db)
    {
        if (await db.Set<Sys_User>().AnyAsync()) return;

        var now = DateTime.UtcNow;

        // 1. Đồng bộ dữ liệu phụ trợ cho Mst_Customer (phục vụ join mc.CustomerCodeSys)
        if (!await db.Set<Mst_Customer>().AnyAsync())
        {
            await db.Set<Mst_Customer>().AddRangeAsync(
                new Mst_Customer
                {
                    CustomerCodeSys = "CUST01",
                    OrgID = "ORG01",
                    NetworkID = "DEMO",
                    CustomerCode = "KH01",
                    CustomerName = "Khách hàng Doanh Nghiệp 01",
                    FlagActive = "1",
                    LogLUBy = "seed",
                    LogLUDTimeUTC = now
                },
                new Mst_Customer
                {
                    CustomerCodeSys = "CUST02",
                    OrgID = "ORG02",
                    NetworkID = "DEMO",
                    CustomerCode = "KH02",
                    CustomerName = "Khách hàng Cá Nhân 02",
                    FlagActive = "1",
                    LogLUBy = "seed",
                    LogLUDTimeUTC = now
                }
            );
        }

        // 2. Đồng bộ OrgID cho Mst_Org và Map_DealerCP_Org để join hiển thị đầy đủ
        if (!await db.Set<Mst_Org>().AnyAsync(x => x.OrgID == "ORG01"))
        {
            await db.Set<Mst_Org>().AddAsync(new Mst_Org
            {
                OrgID = "ORG01",
                NetworkID = "DEMO",
                Remark = "Tổ chức ORG01",
                FlagActive = "1",
                LogLUBy = "seed",
                LogLUDTimeUTC = now
            });
        }
        if (!await db.Set<Mst_Org>().AnyAsync(x => x.OrgID == "ORG02"))
        {
            await db.Set<Mst_Org>().AddAsync(new Mst_Org
            {
                OrgID = "ORG02",
                NetworkID = "DEMO",
                Remark = "Tổ chức ORG02",
                FlagActive = "1",
                LogLUBy = "seed",
                LogLUDTimeUTC = now
            });
        }

        if (!await db.Set<Map_DealerCP_Org>().AnyAsync(x => x.OrgID == "ORG01"))
        {
            await db.Set<Map_DealerCP_Org>().AddAsync(new Map_DealerCP_Org
            {
                DLCPCode = "DL01",
                OrgID = "ORG01",
                NetworkID = "DEMO",
                FlagActive = "1",
                LogLUBy = "seed",
                LogLUDTimeUTC = now
            });
        }

        // 3. Đảm bảo bảng đại lý có DL01 và phòng ban có KD
        if (!await db.Set<Mst_DealerCP>().AnyAsync(x => x.DLCPCode == "DL01"))
        {
            await db.Set<Mst_DealerCP>().AddAsync(new Mst_DealerCP
            {
                DLCPCode = "DL01",
                NetworkID = "DEMO",
                DLCPName = "Đại lý Hà Nội",
                FlagActive = "1",
                LogLUBy = "seed",
                LogLUDTimeUTC = now
            });
        }

        if (!await db.Set<Mst_Department>().AnyAsync(x => x.DepartmentCode == "KD"))
        {
            await db.Set<Mst_Department>().AddAsync(new Mst_Department
            {
                DepartmentCode = "KD",
                DepartmentName = "Kinh doanh",
                OrgID = "ORG01",
                NetworkID = "DEMO",
                FlagActive = "1",
                LogLUBy = "seed"
            });
        }

        // 4. Seed 3 dòng Sys_User (U01, U02, U03) theo yêu cầu P2 đề bài #323:
        // - U01: OrgID = "ORG01", KeyWord chứa "Admin", FlagActive = "1"
        // - U02: OrgID = "ORG02", KeyWord không chứa "Admin", FlagActive = "1"
        // - U03: OrgID = "ORG02", KeyWord không chứa "Admin", FlagActive = "0"
        var users = new[]
        {
            new Sys_User
            {
                UserCode = "U01",
                NetworkID = "DEMO",
                UserName = "Admin System",
                UserPassword = "Password01",
                PhoneNo = "0901000001",
                EMail = "admin@example.com",
                MST = "0100000001",
                DepartmentCode = "KD",
                Position = "Quản trị viên",
                FlagDLAdmin = "1",
                FlagSysAdmin = "1",
                FlagNNTAdmin = "0",
                OrgID = "ORG01",
                CustomerCodeSys = "CUST01",
                FlagActive = "1",
                ACId = "AC01",
                ACAvatar = "~/avatars/u01.png",
                ACEmail = "admin@example.com",
                ACLanguage = "vi-VN",
                ACName = "Admin System",
                ACPhone = "0901000001",
                ACTimeZone = "SE Asia Standard Time",
                LogLUBy = "seed",
                LogLUDTimeUTC = now
            },
            new Sys_User
            {
                UserCode = "U02",
                NetworkID = "DEMO",
                UserName = "Staff User",
                UserPassword = "Password02",
                PhoneNo = "0902000002",
                EMail = "staff@example.com",
                MST = "0100000002",
                DepartmentCode = "KD",
                Position = "Nhân viên kinh doanh",
                FlagDLAdmin = "0",
                FlagSysAdmin = "0",
                FlagNNTAdmin = "0",
                OrgID = "ORG02",
                CustomerCodeSys = "CUST02",
                FlagActive = "1",
                ACId = "AC02",
                ACAvatar = "~/avatars/u02.png",
                ACEmail = "staff@example.com",
                ACLanguage = "vi-VN",
                ACName = "Staff User",
                ACPhone = "0902000002",
                ACTimeZone = "SE Asia Standard Time",
                LogLUBy = "seed",
                LogLUDTimeUTC = now
            },
            new Sys_User
            {
                UserCode = "U03",
                NetworkID = "DEMO",
                UserName = "Inactive User",
                UserPassword = "Password03",
                PhoneNo = "0903000003",
                EMail = "inactive@example.com",
                MST = "0100000003",
                DepartmentCode = "KD",
                Position = "Cựu nhân viên",
                FlagDLAdmin = "0",
                FlagSysAdmin = "0",
                FlagNNTAdmin = "0",
                OrgID = "ORG02",
                CustomerCodeSys = "CUST02",
                FlagActive = "0",
                ACId = "AC03",
                ACAvatar = null,
                ACEmail = "inactive@example.com",
                ACLanguage = "vi-VN",
                ACName = "Inactive User",
                ACPhone = "0903000003",
                ACTimeZone = "SE Asia Standard Time",
                LogLUBy = "seed",
                LogLUDTimeUTC = now
            }
        };

        await db.Set<Sys_User>().AddRangeAsync(users);
        await db.SaveChangesAsync();
    }
}
