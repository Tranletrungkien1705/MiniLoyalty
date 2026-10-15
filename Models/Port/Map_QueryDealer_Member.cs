using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniLoyalty.Data;

namespace MiniLoyalty.Models.Port;

// ===========================================================================================
// PORT 1:1 từ dự án nguồn 2022.7.Loyalty — bảng liên kết truy vấn hội viên theo đại lý Map_QueryDealer_Member (#66).
//
// Nguồn tra cứu (chỉ đọc):
//   - DDL:        11.BackEnd/V10/05.Refs.Biz/Migrate/Script.HuongTTT/20221108.z11.Crd_Member.Crd_Card.sql:12-21 (và :391-400)
//   - Model cột:  12.Dev.Common/idn.Loyalty.Common/Models/Loyalty/Map_QueryDealer_Member.cs:9-29
//   - SQL select: idn.Loyalty.Biz/Master.Loyalty.cs (Map_QueryDealer_Member_GetX — bản LIVE, SQL excerpt :20933-21140)
//
// ĐẶC ĐIỂM BẢNG (đúng 8 cột theo DDL nguồn):
//   DLCPCode (Varchar50 NOT NULL), MemberNo (Varchar50 NOT NULL), NetworkID (Varchar50 NOT NULL),
//   QueryDate (MyDateTime NOT NULL), Remark (Varchar400), FlagActive (MyStatus),
//   LogLUDTimeUTC (MyDateTime), LogLUBy (Varchar50).
//
// GHI CHÚ QUAN TRỌNG:
//   - Khóa chính: Trong DDL không có khai báo PK — khoá (DLCPCode, MemberNo) suy từ cột join select cuối (cần xác nhận).
//   - Model nguồn Map_QueryDealer_Member.cs có thêm các thuộc tính: DLCPName, CarNo, VIN, PhoneNo, CardTypeUse,
//     MemberName, ModelCode, DLCodeRegis, ProvinceName, ModelName, ModelCodeName -> Đây là các cột tính (enrichment)
//     từ các bảng Crd_Member, Mst_DealerCP,... KHÔNG đưa vào bảng vật lý.
// ===========================================================================================

/// <summary>
/// Đại lý truy vấn thông tin hội viên — port 1:1 bảng <c>Map_QueryDealer_Member</c>.
/// DDL nguồn: 20221108.z11.Crd_Member.Crd_Card.sql:12-21.
/// Đúng 8 cột: DLCPCode, MemberNo, NetworkID, QueryDate, Remark, FlagActive, LogLUDTimeUTC, LogLUBy.
/// Không có PK trong DDL — khoá (DLCPCode, MemberNo) suy từ cột join select cuối (cần xác nhận).
/// </summary>
public class Map_QueryDealer_Member
{
    public string DLCPCode { get; set; } = "";           // Mã đại lý truy vấn — Varchar50 NOT NULL
    public string MemberNo { get; set; } = "";           // Mã hội viên — Varchar50 NOT NULL
    public string NetworkID { get; set; } = "";          // Mã mạng — Varchar50 NOT NULL
    public DateTime QueryDate { get; set; }              // Ngày truy vấn — MyDateTime NOT NULL
    public string? Remark { get; set; }                  // Ghi chú — Varchar400
    public string? FlagActive { get; set; }              // Trạng thái '1'/'0' — MyStatus
    public DateTime? LogLUDTimeUTC { get; set; }         // Thời gian log UTC — MyDateTime
    public string? LogLUBy { get; set; }                 // Người cập nhật — Varchar50
}

/// <summary>Cấu hình EF cho <see cref="Map_QueryDealer_Member"/> — tên bảng Map_QueryDealer_Member + khoá ghép (DLCPCode, MemberNo).</summary>
public class Map_QueryDealer_MemberConfig : IEntityTypeConfiguration<Map_QueryDealer_Member>
{
    public void Configure(EntityTypeBuilder<Map_QueryDealer_Member> b)
    {
        b.ToTable("Map_QueryDealer_Member");
        // Không có PK trong DDL — khoá (DLCPCode, MemberNo) suy từ cột join select cuối (cần xác nhận)
        b.HasKey(x => new { x.DLCPCode, x.MemberNo });
        b.Property(x => x.DLCPCode).HasMaxLength(50).IsRequired();
        b.Property(x => x.MemberNo).HasMaxLength(50).IsRequired();
        b.Property(x => x.NetworkID).HasMaxLength(50).IsRequired();
        b.Property(x => x.QueryDate).IsRequired();
        b.Property(x => x.Remark).HasMaxLength(400);
        b.Property(x => x.FlagActive);
        b.Property(x => x.LogLUDTimeUTC);
        b.Property(x => x.LogLUBy).HasMaxLength(50);
    }
}

/// <summary>
/// Dữ liệu mẫu cho bảng Map_QueryDealer_Member. Idempotent: đã có dữ liệu thì bỏ qua.
/// Dữ liệu demo (Order => 60 theo đề bài):
/// - (DL01, HV0000001, QueryDate 2026-10-01, LogLUDTimeUTC 2026-10-10 10:00)
/// - (DL01, HV0000002, 2026-10-02, 2026-10-10 09:00)
/// - (DL03, HV0000003, 2026-10-03, 2026-10-10 08:00)
/// - (DL01, HV9999999 [không có hội viên], 2026-10-04, 2026-10-10 11:00)
/// </summary>
public class Map_QueryDealer_MemberSeeder : IPortSeeder
{
    public int Order => 60; // Theo yêu cầu đề bài

    public async Task SeedAsync(AppDbContext db)
    {
        if (await db.Set<Map_QueryDealer_Member>().AnyAsync()) return;

        db.Set<Map_QueryDealer_Member>().AddRange(
            new Map_QueryDealer_Member
            {
                DLCPCode = "DL01",
                MemberNo = "HV0000001",
                NetworkID = "1001",
                QueryDate = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
                Remark = "Demo query dealer member 1",
                FlagActive = "1",
                LogLUDTimeUTC = new DateTime(2026, 10, 10, 10, 0, 0, DateTimeKind.Utc),
                LogLUBy = "seed"
            },
            new Map_QueryDealer_Member
            {
                DLCPCode = "DL01",
                MemberNo = "HV0000002",
                NetworkID = "1001",
                QueryDate = new DateTime(2026, 10, 2, 0, 0, 0, DateTimeKind.Utc),
                Remark = "Demo query dealer member 2",
                FlagActive = "1",
                LogLUDTimeUTC = new DateTime(2026, 10, 10, 9, 0, 0, DateTimeKind.Utc),
                LogLUBy = "seed"
            },
            new Map_QueryDealer_Member
            {
                DLCPCode = "DL03",
                MemberNo = "HV0000003",
                NetworkID = "1001",
                QueryDate = new DateTime(2026, 10, 3, 0, 0, 0, DateTimeKind.Utc),
                Remark = "Demo query dealer member 3",
                FlagActive = "1",
                LogLUDTimeUTC = new DateTime(2026, 10, 10, 8, 0, 0, DateTimeKind.Utc),
                LogLUBy = "seed"
            },
            new Map_QueryDealer_Member
            {
                DLCPCode = "DL01",
                MemberNo = "HV9999999", // Hội viên không tồn tại trong Crd_Member để test inner join loại bỏ
                NetworkID = "1001",
                QueryDate = new DateTime(2026, 10, 4, 0, 0, 0, DateTimeKind.Utc),
                Remark = "Demo query dealer member 4 (khong co hoi vien)",
                FlagActive = "1",
                LogLUDTimeUTC = new DateTime(2026, 10, 10, 11, 0, 0, DateTimeKind.Utc),
                LogLUBy = "seed"
            }
        );
        await db.SaveChangesAsync();
    }
}
