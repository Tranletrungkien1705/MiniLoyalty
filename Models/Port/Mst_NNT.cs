using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniLoyalty.Data;

namespace MiniLoyalty.Models.Port;

/// <summary>
/// Bảng Mst_NNT — thông tin Người Nộp Thuế (48 cột theo DDL và model nguồn).
/// DDL nguồn: 20221230.Check_CreateMstDealerCP.sql:265
/// Model nguồn: 12.Dev.Common\idn.Loyalty.Common\Models\Mst_NNT.cs:1-143
/// Khoá chính (PK): MST (Check_CreateMstDealerCP.sql:265, Master.cs:21404)
/// </summary>
public class Mst_NNT
{
    /// <summary>Mã số thuế (khoá chính) - Mst_NNT.cs:11, Check_CreateMstDealerCP.sql:265</summary>
    public string MST { get; set; } = "";

    /// <summary>Mã mạng lưới - Mst_NNT.cs:18, Check_CreateMstDealerCP.sql:265</summary>
    public string? NetworkID { get; set; }

    /// <summary>Tên doanh nghiệp / NNT - Mst_NNT.cs:15, Check_CreateMstDealerCP.sql:265</summary>
    public string? NNTFullName { get; set; }

    /// <summary>Đơn vị trực thuộc / MST cha - Mst_NNT.cs:20, Check_CreateMstDealerCP.sql:265</summary>
    public string? MSTParent { get; set; }

    /// <summary>Mã đơn vị kinh doanh BU - Mst_NNT.cs:22, Check_CreateMstDealerCP.sql:265</summary>
    public string? MSTBUCode { get; set; }

    /// <summary>Mẫu đơn vị kinh doanh BU - Mst_NNT.cs:24, Check_CreateMstDealerCP.sql:265</summary>
    public string? MSTBUPattern { get; set; }

    /// <summary>Cấp bậc MST - Mst_NNT.cs:26, Check_CreateMstDealerCP.sql:265</summary>
    public string? MSTLevel { get; set; }

    /// <summary>Mã tỉnh/thành - Mst_NNT.cs:28, Check_CreateMstDealerCP.sql:265</summary>
    public string? ProvinceCode { get; set; }

    /// <summary>Mã quận/huyện - Mst_NNT.cs:30, Check_CreateMstDealerCP.sql:265</summary>
    public string? DistrictCode { get; set; }

    /// <summary>Mã đại lý - Mst_NNT.cs:34, Check_CreateMstDealerCP.sql:265</summary>
    public string? DLCode { get; set; }

    /// <summary>Địa chỉ người nộp thuế - Mst_NNT.cs:36, Check_CreateMstDealerCP.sql:265</summary>
    public string? NNTAddress { get; set; }

    /// <summary>Điện thoại di động - Mst_NNT.cs:38, Check_CreateMstDealerCP.sql:265</summary>
    public string? NNTMobile { get; set; }

    /// <summary>Tên người liên hệ - Mst_NNT.cs:56, Check_CreateMstDealerCP.sql:265</summary>
    public string? ContactName { get; set; }

    /// <summary>Điện thoại người liên hệ - Mst_NNT.cs:58, Check_CreateMstDealerCP.sql:265</summary>
    public string? ContactPhone { get; set; }

    /// <summary>Email người liên hệ - Mst_NNT.cs:60, Check_CreateMstDealerCP.sql:265</summary>
    public string? ContactEmail { get; set; }

    /// <summary>Website - Mst_NNT.cs:62, Check_CreateMstDealerCP.sql:265</summary>
    public string? Website { get; set; }

    /// <summary>Chứng thư số CA - Mst_NNT.cs:64, Check_CreateMstDealerCP.sql:265</summary>
    public string? CANumber { get; set; }

    /// <summary>Tổ chức cấp chứng thư số CA - Mst_NNT.cs:66, Check_CreateMstDealerCP.sql:265</summary>
    public string? CAOrg { get; set; }

    /// <summary>Thời gian bắt đầu hiệu lực CA - Mst_NNT.cs:68, Check_CreateMstDealerCP.sql:265</summary>
    public string? CAEffDTimeUTCStart { get; set; }

    /// <summary>Thời gian hết hiệu lực CA - Mst_NNT.cs:70, Check_CreateMstDealerCP.sql:265</summary>
    public string? CAEffDTimeUTCEnd { get; set; }

    /// <summary>Mã gói dịch vụ - Mst_NNT.cs:72, Check_CreateMstDealerCP.sql:265</summary>
    public string? PackageCode { get; set; }

    /// <summary>Thời gian tạo UTC - Check_CreateMstDealerCP.sql:265, Mst_NNT.cs:76</summary>
    public string? CreateDTimeUTC { get; set; }

    /// <summary>Người tạo - Mst_NNT.cs:78, Check_CreateMstDealerCP.sql:265</summary>
    public string? CreateBy { get; set; }

    /// <summary>Số tài khoản ngân hàng - Mst_NNT.cs:80, Check_CreateMstDealerCP.sql:265</summary>
    public string? AccNo { get; set; }

    /// <summary>Chủ tài khoản ngân hàng - Mst_NNT.cs:82, Check_CreateMstDealerCP.sql:265</summary>
    public string? AccHolder { get; set; }

    /// <summary>Tên ngân hàng - Mst_NNT.cs:84, Check_CreateMstDealerCP.sql:265</summary>
    public string? BankName { get; set; }

    /// <summary>Trạng thái Tổng cục Thuế - Mst_NNT.cs:97, Check_CreateMstDealerCP.sql:265</summary>
    public string? TCTStatus { get; set; }

    /// <summary>Trạng thái hoạt động ('1'=hoạt động, '0'=ngừng) - Mst_NNT.cs:99, Check_CreateMstDealerCP.sql:265</summary>
    public string? FlagActive { get; set; }

    /// <summary>Thời gian cập nhật cuối cùng UTC - Mst_NNT.cs:103, Check_CreateMstDealerCP.sql:265</summary>
    public DateTime? LogLUDTimeUTC { get; set; }

    /// <summary>Người cập nhật cuối cùng - Mst_NNT.cs:105, Check_CreateMstDealerCP.sql:265</summary>
    public string? LogLUBy { get; set; }

    /// <summary>Loại hình tổ chức - Mst_NNT.cs:86, Check_CreateMstDealerCP.sql:265</summary>
    public string? BizType { get; set; }

    /// <summary>Lĩnh vực hoạt động - Mst_NNT.cs:88, Check_CreateMstDealerCP.sql:265</summary>
    public string? BizFieldCode { get; set; }

    /// <summary>Quy mô tổ chức - Mst_NNT.cs:90, Check_CreateMstDealerCP.sql:265</summary>
    public string? BizSizeCode { get; set; }

    /// <summary>Trạng thái đăng ký - Mst_NNT.cs:95, Check_CreateMstDealerCP.sql:265</summary>
    public string? RegisterStatus { get; set; }

    /// <summary>Mã tổ chức liên kết - Mst_NNT.cs:13, Check_CreateMstDealerCP.sql:265</summary>
    public string? OrgID { get; set; }

    /// <summary>Ghi chú - Mst_NNT.cs:101, Check_CreateMstDealerCP.sql:265</summary>
    public string? Remark { get; set; }

    /// <summary>Ngày tạo - Mst_NNT.cs:74, Check_CreateMstDealerCP.sql:265</summary>
    public string? CreatedDate { get; set; }

    /// <summary>Số giấy phép đăng ký kinh doanh - Mst_NNT.cs:46, Check_CreateMstDealerCP.sql:265</summary>
    public string? BusinessRegNo { get; set; }

    /// <summary>Người đại diện pháp luật - Mst_NNT.cs:44, Check_CreateMstDealerCP.sql:265</summary>
    public string? PresentBy { get; set; }

    /// <summary>Chức vụ người đại diện - Mst_NNT.cs:48, Check_CreateMstDealerCP.sql:265</summary>
    public string? NNTPosition { get; set; }

    /// <summary>Số giấy tờ tuỳ thân người đại diện - Mst_NNT.cs:50, Check_CreateMstDealerCP.sql:265</summary>
    public string? PresentIDNo { get; set; }

    /// <summary>Loại giấy tờ tuỳ thân - Mst_NNT.cs:52, Check_CreateMstDealerCP.sql:265</summary>
    public string? PresentIDType { get; set; }

    /// <summary>Số Fax - Mst_NNT.cs:42, Check_CreateMstDealerCP.sql:265</summary>
    public string? NNTFax { get; set; }

    /// <summary>Cơ quan thuế quản lý - Mst_NNT.cs:54, Check_CreateMstDealerCP.sql:265</summary>
    public string? GovTaxID { get; set; }

    /// <summary>Điện thoại cố định - Mst_NNT.cs:40, Check_CreateMstDealerCP.sql:265</summary>
    public string? NNTPhone { get; set; }

    /// <summary>Loại đại lý (NPP/NCC) - Mst_NNT.cs:92, Check_CreateMstDealerCP.sql:265</summary>
    public string? DealerType { get; set; }

    /// <summary>Vùng thị trường - Mst_NNT.cs:93, Check_CreateMstDealerCP.sql:265</summary>
    public string? AreaCode { get; set; }

    /// <summary>Tên viết tắt doanh nghiệp - Mst_NNT.cs:16, Check_CreateMstDealerCP.sql:265</summary>
    public string? NNTShortName { get; set; }
}

/// <summary>
/// Cấu hình EF cho bảng Mst_NNT — ánh xạ bảng Mst_NNT, khoá chính MST.
/// </summary>
public class Mst_NNTConfiguration : IEntityTypeConfiguration<Mst_NNT>
{
    public void Configure(EntityTypeBuilder<Mst_NNT> builder)
    {
        builder.ToTable("Mst_NNT");
        builder.HasKey(x => x.MST);
    }
}

/// <summary>
/// Seeder khởi tạo dữ liệu mẫu cho bảng Mst_NNT (đảm bảo đủ 6 dòng NNT theo đề bài #172).
/// Thứ tự chạy: 22 (danh mục Mst_* dùng 10–99).
/// Idempotent: chỉ thêm các dòng MST chưa tồn tại trong DB.
/// </summary>
public class Mst_NNTSeeder : IPortSeeder
{
    public int Order => 22;

    public async Task SeedAsync(AppDbContext db)
    {
        var now = DateTime.UtcNow;

        // 6 dòng NNT theo thứ tự MST asc (P2 đề bài #172)
        var candidates = new[]
        {
            new Mst_NNT
            {
                MST = "0100000001",
                OrgID = "4020320001",
                NNTFullName = "Công ty Cổ phần Tập đoàn 01",
                FlagActive = "1",
                NetworkID = "1001",
                ContactEmail = "tapdoan01@demo.vn",
                LogLUDTimeUTC = now,
                LogLUBy = "admin"
            },
            new Mst_NNT
            {
                MST = "0100000002",
                OrgID = "4020320002",
                NNTFullName = "Chi nhánh Công ty 02",
                FlagActive = "1",
                NetworkID = "1001",
                LogLUDTimeUTC = now,
                LogLUBy = "admin"
            },
            new Mst_NNT
            {
                MST = "0100000003",
                OrgID = "4020320001",
                NNTFullName = "Chi nhánh Tập đoàn 03",
                FlagActive = "1",
                NetworkID = "1001",
                ProvinceCode = "HN",
                DistrictCode = "HN01",
                LogLUDTimeUTC = now,
                LogLUBy = "admin"
            },
            new Mst_NNT
            {
                MST = "0100000004",
                OrgID = "4020330004",
                NNTFullName = "Công ty TNHH Dịch vụ 04",
                FlagActive = "0",
                NetworkID = "1001",
                LogLUDTimeUTC = now,
                LogLUBy = "admin"
            },
            new Mst_NNT
            {
                MST = "0100000005",
                OrgID = "4020320005",
                NNTFullName = "Văn phòng Đại diện 05",
                FlagActive = "1",
                NetworkID = "1001",
                ProvinceCode = "SG",
                LogLUDTimeUTC = now,
                LogLUBy = "admin"
            },
            new Mst_NNT
            {
                MST = "0100000006",
                OrgID = "4020320006",
                NNTFullName = "Công ty Cổ phần Dịch vụ 06",
                FlagActive = "0",
                NetworkID = "1001",
                LogLUDTimeUTC = now,
                LogLUBy = "admin"
            }
        };

        var existingMsts = await db.Set<Mst_NNT>().Select(x => x.MST).ToListAsync();
        var toAdd = candidates.Where(c => !existingMsts.Contains(c.MST)).ToList();

        if (toAdd.Count > 0)
        {
            db.Set<Mst_NNT>().AddRange(toAdd);
            await db.SaveChangesAsync();
        }
    }
}
