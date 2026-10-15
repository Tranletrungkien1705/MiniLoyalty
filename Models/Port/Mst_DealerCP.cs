using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniLoyalty.Data;

namespace MiniLoyalty.Models.Port;

/// <summary>
/// Bảng Mst_DealerCP: danh mục đại lý (14 cột DB).
/// Không có DDL trong repo nguồn; khoá chính DLCPCode xác định theo các FK REFERENCES Mst_DealerCP (DLCPCode) (vd 20221026.CreateDatabase.sql:120).
/// Kiểu cột cần xác nhận khi có DB thật.
/// Nguồn: 12.Dev.Common\idn.Loyalty.Common\Models\Loyalty\Mst_DealerCP.cs:11-24.
/// </summary>
public class Mst_DealerCP
{
    /// <summary>Mã đại lý (khoá chính) - Mst_DealerCP.cs:11</summary>
    public string DLCPCode { get; set; } = "";

    /// <summary>Mã mạng lưới - Mst_DealerCP.cs:12</summary>
    public string? NetworkID { get; set; }

    /// <summary>Mã đại lý cha - Mst_DealerCP.cs:13</summary>
    public string? DLCPCodeParent { get; set; }

    /// <summary>Mã đơn vị kinh doanh BU - Mst_DealerCP.cs:14</summary>
    public string? DLCPBUCode { get; set; }

    /// <summary>Mẫu đơn vị kinh doanh BU - Mst_DealerCP.cs:15</summary>
    public string? DLCPBUPattern { get; set; }

    /// <summary>Cấp bậc đại lý - Mst_DealerCP.cs:16</summary>
    public string? DLCPLevel { get; set; }

    /// <summary>Tên đại lý - Mst_DealerCP.cs:17</summary>
    public string? DLCPName { get; set; }

    /// <summary>Số điện thoại đại lý - Mst_DealerCP.cs:18</summary>
    public string? DLCPPhoneNo { get; set; }

    /// <summary>Cờ đại lý gốc ('1' = gốc) - Mst_DealerCP.cs:19</summary>
    public string? FlagRoot { get; set; }

    /// <summary>Trạng thái kích hoạt ('1' = hoạt động, '0' = tạm ngừng) - Mst_DealerCP.cs:20</summary>
    public string? FlagActive { get; set; }

    /// <summary>Cờ đánh dấu đại lý được tặng điểm sinh nhật - Mst_DealerCP.cs:21</summary>
    public string? FlagBirthPoints { get; set; }

    /// <summary>Ghi chú đại lý - Mst_DealerCP.cs:22</summary>
    public string? Remark { get; set; }

    /// <summary>Thời gian cập nhật cuối cùng theo chuẩn UTC - Mst_DealerCP.cs:23</summary>
    public DateTime? LogLUDTimeUTC { get; set; }

    /// <summary>Người dùng thực hiện cập nhật cuối cùng - Mst_DealerCP.cs:24</summary>
    public string? LogLUBy { get; set; }
}

/// <summary>
/// Bảng Map_DealerCP_Org: liên kết giữa đại lý và tổ chức (6 cột DB).
/// Khoá ghép (DLCPCode, OrgID) là SUY LUẬN (không có DDL) — cần xác nhận khi có DB thật.
/// Cột mnnt_MST ở model nguồn là cột enrich khi truy vấn, không phải cột DB.
/// Nguồn: 12.Dev.Common\idn.Loyalty.Common\Models\Loyalty\Map_DealerCP_Org.cs:11-16.
/// </summary>
public class Map_DealerCP_Org
{
    /// <summary>Mã đại lý (khoá chính thành phần - suy luận, cần xác nhận) - Map_DealerCP_Org.cs:11</summary>
    public string DLCPCode { get; set; } = "";

    /// <summary>Mã tổ chức liên kết (khoá chính thành phần - suy luận, cần xác nhận) - Map_DealerCP_Org.cs:12</summary>
    public string OrgID { get; set; } = "";

    /// <summary>Mã mạng lưới - Map_DealerCP_Org.cs:13</summary>
    public string? NetworkID { get; set; }

    /// <summary>Trạng thái kích hoạt ('1' = hoạt động, '0' = tạm ngừng) - Map_DealerCP_Org.cs:14</summary>
    public string? FlagActive { get; set; }

    /// <summary>Thời gian cập nhật cuối cùng theo chuẩn UTC - Map_DealerCP_Org.cs:15</summary>
    public DateTime? LogLUDTimeUTC { get; set; }

    /// <summary>Người dùng thực hiện cập nhật cuối cùng - Map_DealerCP_Org.cs:16</summary>
    public string? LogLUBy { get; set; }
}

/// <summary>Cấu hình ánh xạ EntityType cho bảng Mst_DealerCP (khoá chính DLCPCode).</summary>
public class Mst_DealerCPConfiguration : IEntityTypeConfiguration<Mst_DealerCP>
{
    public void Configure(EntityTypeBuilder<Mst_DealerCP> builder)
    {
        builder.ToTable("Mst_DealerCP");
        builder.HasKey(x => x.DLCPCode);
    }
}

/// <summary>Cấu hình ánh xạ EntityType cho bảng Map_DealerCP_Org (khoá ghép DLCPCode, OrgID - suy luận, cần xác nhận).</summary>
public class Map_DealerCP_OrgConfiguration : IEntityTypeConfiguration<Map_DealerCP_Org>
{
    public void Configure(EntityTypeBuilder<Map_DealerCP_Org> builder)
    {
        builder.ToTable("Map_DealerCP_Org");
        builder.HasKey(x => new { x.DLCPCode, x.OrgID });
    }
}

/// <summary>
/// Seeder khởi tạo dữ liệu mẫu cho Mst_DealerCP và Map_DealerCP_Org.
/// Thứ tự chạy: 12 (chạy sau Mst_OrgSeeder có Order 10). Idempotent: bỏ qua nếu đã có dữ liệu.
/// </summary>
public class Mst_DealerCPSeeder : IPortSeeder
{
    public int Order => 12;

    public async Task SeedAsync(AppDbContext db)
    {
        if (await db.Set<Mst_DealerCP>().AnyAsync()) return;

        var now = DateTime.UtcNow;

        // Mst_DealerCP 4 dòng (NetworkID = '1001'):
        // HTV 'Tổng công ty' (parent null, FlagRoot '1', FlagActive '1')
        // DL01 'Đại lý Hà Nội' (parent HTV, FlagRoot '0', FlagActive '1')
        // DL02 'Đại lý Hải Phòng' (parent HTV, FlagRoot '0', FlagActive '0')
        // DL03 'Đại lý Đà Nẵng' (parent HTV, FlagRoot '0', FlagActive '1')
        var dealers = new[]
        {
            new Mst_DealerCP
            {
                DLCPCode = "HTV",
                NetworkID = "1001",
                DLCPCodeParent = null,
                DLCPBUCode = "BU01",
                DLCPBUPattern = "PAT01",
                DLCPLevel = "1",
                DLCPName = "Tổng công ty",
                DLCPPhoneNo = "0243000000",
                FlagRoot = "1",
                FlagActive = "1",
                FlagBirthPoints = "1",
                Remark = "Tổng công ty HTV",
                LogLUDTimeUTC = now,
                LogLUBy = "admin"
            },
            new Mst_DealerCP
            {
                DLCPCode = "DL01",
                NetworkID = "1001",
                DLCPCodeParent = "HTV",
                DLCPBUCode = "BU01",
                DLCPBUPattern = "PAT01",
                DLCPLevel = "2",
                DLCPName = "Đại lý Hà Nội",
                DLCPPhoneNo = "0243111111",
                FlagRoot = "0",
                FlagActive = "1",
                FlagBirthPoints = "1",
                Remark = "Đại lý Hà Nội",
                LogLUDTimeUTC = now,
                LogLUBy = "admin"
            },
            new Mst_DealerCP
            {
                DLCPCode = "DL02",
                NetworkID = "1001",
                DLCPCodeParent = "HTV",
                DLCPBUCode = "BU01",
                DLCPBUPattern = "PAT01",
                DLCPLevel = "2",
                DLCPName = "Đại lý Hải Phòng",
                DLCPPhoneNo = "0225222222",
                FlagRoot = "0",
                FlagActive = "0",
                FlagBirthPoints = "0",
                Remark = "Đại lý Hải Phòng (tạm ngừng)",
                LogLUDTimeUTC = now,
                LogLUBy = "admin"
            },
            new Mst_DealerCP
            {
                DLCPCode = "DL03",
                NetworkID = "1001",
                DLCPCodeParent = "HTV",
                DLCPBUCode = "BU01",
                DLCPBUPattern = "PAT01",
                DLCPLevel = "2",
                DLCPName = "Đại lý Đà Nẵng",
                DLCPPhoneNo = "0236333333",
                FlagRoot = "0",
                FlagActive = "1",
                FlagBirthPoints = "1",
                Remark = "Đại lý Đà Nẵng",
                LogLUDTimeUTC = now,
                LogLUBy = "admin"
            }
        };

        // Map_DealerCP_Org 3 dòng (NetworkID = '1001'):
        // (DL01, 4020320001) · (DL02, 4020320001) · (DL02, 4020320003) [4020320003 là Org KHÔNG có NNT]
        var maps = new[]
        {
            new Map_DealerCP_Org
            {
                DLCPCode = "DL01",
                OrgID = "4020320001",
                NetworkID = "1001",
                FlagActive = "1",
                LogLUDTimeUTC = now,
                LogLUBy = "admin"
            },
            new Map_DealerCP_Org
            {
                DLCPCode = "DL02",
                OrgID = "4020320001",
                NetworkID = "1001",
                FlagActive = "1",
                LogLUDTimeUTC = now,
                LogLUBy = "admin"
            },
            new Map_DealerCP_Org
            {
                DLCPCode = "DL02",
                OrgID = "4020320003",
                NetworkID = "1001",
                FlagActive = "1",
                LogLUDTimeUTC = now,
                LogLUBy = "admin"
            }
        };

        db.Set<Mst_DealerCP>().AddRange(dealers);
        db.Set<Map_DealerCP_Org>().AddRange(maps);
        await db.SaveChangesAsync();
    }
}
