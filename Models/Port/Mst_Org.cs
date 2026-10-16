using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniLoyalty.Data;

namespace MiniLoyalty.Models.Port;

/// <summary>
/// Không có DDL Mst_Org trong repo nguồn -> từ điển cột lấy từ model; kiểu cột cần xác nhận khi có DB thật.
/// Nguồn: 12.Dev.Common\idn.Loyalty.Common\Models\Mst_Org.cs:11-31 (11 cột DB).
/// </summary>
public class Mst_Org
{
    /// <summary>Mã tổ chức (khoá chính) - Mst_Org.cs:11</summary>
    public string OrgID { get; set; } = "";

    /// <summary>Mã mạng lưới - Mst_Org.cs:13</summary>
    public string? NetworkID { get; set; }

    /// <summary>Mã tổ chức cha - Mst_Org.cs:15</summary>
    public string? OrgParent { get; set; }

    /// <summary>Mã đơn vị kinh doanh BU - Mst_Org.cs:17</summary>
    public string? OrgBUCode { get; set; }

    /// <summary>Mẫu đơn vị kinh doanh BU - Mst_Org.cs:19</summary>
    public string? OrgBUPattern { get; set; }

    /// <summary>Cấp bậc tổ chức - Mst_Org.cs:21</summary>
    public string? OrgLevel { get; set; }

    /// <summary>Ghi chú tổ chức - Mst_Org.cs:23</summary>
    public string? Remark { get; set; }

    /// <summary>Trạng thái kích hoạt ('1' = hoạt động, '0' = tạm ngừng) - Mst_Org.cs:25</summary>
    public string? FlagActive { get; set; }

    /// <summary>Thời gian cập nhật cuối cùng theo chuẩn UTC - Mst_Org.cs:27</summary>
    public DateTime? LogLUDTimeUTC { get; set; }

    /// <summary>Người dùng thực hiện cập nhật cuối cùng - Mst_Org.cs:29</summary>
    public string? LogLUBy { get; set; }

    /// <summary>Mã số thuế liên kết với tổ chức - Mst_Org.cs:31</summary>
    public string? MST { get; set; }
}

/// <summary>Cấu hình ánh xạ EntityType cho bảng Mst_Org (khoá chính OrgID).</summary>
public class Mst_OrgConfiguration : IEntityTypeConfiguration<Mst_Org>
{
    public void Configure(EntityTypeBuilder<Mst_Org> builder)
    {
        builder.ToTable("Mst_Org");
        builder.HasKey(x => x.OrgID);
    }
}

/// <summary>
/// Seeder khởi tạo dữ liệu mẫu cho Mst_Org và Mst_NNT.
/// Thứ tự chạy: 10 (danh mục Mst_* dùng 10-99). Idempotent: bỏ qua nếu đã có dữ liệu.
/// </summary>
public class Mst_OrgSeeder : IPortSeeder
{
    public int Order => 10;

    public async Task SeedAsync(AppDbContext db)
    {
        if (await db.Set<Mst_Org>().AnyAsync()) return;

        var now = DateTime.UtcNow;

        // 4 Org: 3 Org có FlagActive='1', 1 Org có FlagActive='0'.
        // 3 Org có dòng Mst_NNT và 1 Org KHÔNG có (để chứng minh inner join).
        // OrgID dạng chuỗi số 10 ký tự như nguồn (vd '4020320000').
        var orgs = new[]
        {
            new Mst_Org
            {
                OrgID = "4020320001",
                NetworkID = "1001",
                OrgParent = null,
                OrgBUCode = "BU01",
                OrgBUPattern = "PAT01",
                OrgLevel = "1",
                Remark = "Tổ chức trung tâm 01",
                FlagActive = "1",
                LogLUDTimeUTC = now,
                LogLUBy = "admin",
                MST = "0100000001"
            },
            new Mst_Org
            {
                OrgID = "4020320002",
                NetworkID = "1001",
                OrgParent = "4020320001",
                OrgBUCode = "BU02",
                OrgBUPattern = "PAT02",
                OrgLevel = "2",
                Remark = "Chi nhánh 02",
                FlagActive = "1",
                LogLUDTimeUTC = now,
                LogLUBy = "admin",
                MST = "0100000002"
            },
            new Mst_Org
            {
                OrgID = "4020320003",
                NetworkID = "1001",
                OrgParent = "4020320001",
                OrgBUCode = "BU03",
                OrgBUPattern = "PAT03",
                OrgLevel = "2",
                Remark = "Chi nhánh 03 (Không có Mst_NNT)",
                FlagActive = "1",
                LogLUDTimeUTC = now,
                LogLUBy = "admin",
                MST = null
            },
            new Mst_Org
            {
                OrgID = "4020330004",
                NetworkID = "1001",
                OrgParent = null,
                OrgBUCode = "BU04",
                OrgBUPattern = "PAT04",
                OrgLevel = "1",
                Remark = "Tổ chức cũ tạm ngừng hoạt động",
                FlagActive = "0",
                LogLUDTimeUTC = now,
                LogLUBy = "admin",
                MST = "0100000004"
            }
        };

        // 3 dòng Mst_NNT cho 3 Org: 4020320001, 4020320002, 4020330004.
        // Org 4020320003 KHÔNG có dòng Mst_NNT nào.
        var nnts = new[]
        {
            new Mst_NNT
            {
                MST = "0100000001",
                OrgID = "4020320001",
                NNTFullName = "Công ty Cổ phần Tập đoàn 01",
                NetworkID = "1001",
                FlagActive = "1",
                LogLUDTimeUTC = now,
                LogLUBy = "admin",
                ContactEmail = "tapdoan01@demo.vn"
            },
            new Mst_NNT
            {
                MST = "0100000002",
                OrgID = "4020320002",
                NNTFullName = "Chi nhánh Công ty 02",
                NetworkID = "1001",
                FlagActive = "1",
                LogLUDTimeUTC = now,
                LogLUBy = "admin"
            },
            new Mst_NNT
            {
                MST = "0100000004",
                OrgID = "4020330004",
                NNTFullName = "Công ty TNHH Dịch vụ 04",
                NetworkID = "1001",
                FlagActive = "0",
                LogLUDTimeUTC = now,
                LogLUBy = "admin"
            }
        };

        db.Set<Mst_Org>().AddRange(orgs);
        db.Set<Mst_NNT>().AddRange(nnts);
        await db.SaveChangesAsync();
    }
}
