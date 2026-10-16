using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniLoyalty.Data;

namespace MiniLoyalty.Models.Port;

/// <summary>
/// Model Crd_MemberChangeInfo — port 1:1 từ Crd_MemberChangeInfo.cs:1-50 (#027).
/// Đầy đủ 35 thuộc tính theo mô hình nguồn.
/// Khoá chính: RequestNo (xác định từ Crd_MemberChangeInfo_CheckDB tại Card.cs:27911).
/// </summary>
public class Crd_MemberChangeInfo
{
    public string RequestNo { get; set; } = ""; // Số đề nghị (PK)
    public string? NetworkID { get; set; }
    public string? OrgID { get; set; }
    public string? MemberNo { get; set; } // Mã thẻ hội viên
    public string? DLCodeRequest { get; set; } // Đại lý đề nghị
    public string? RequestType { get; set; } // Loại đề nghị
    public string? RequestStatus { get; set; } // Trạng thái đề nghị
    public string? Remark { get; set; } // Ghi chú
    public string? RemarkHTV { get; set; } // Ghi chú HTV
    public string? AttachFilePath { get; set; } // Đường dẫn file đính kèm
    public string? AttachFileName { get; set; } // Tên file đính kèm
    public string? AttachFileSize { get; set; } // Kích thước file đính kèm
    public string? AttachFileBase64 { get; set; } // Base64
    public string? CreateDTimeUTC { get; set; } // Thời gian tạo
    public string? CreateBy { get; set; } // Người tạo
    public string? LUDTimeUTC { get; set; }
    public string? LUBy { get; set; }
    public string? ApproveDTimeUTC { get; set; } // Thời gian duyệt A
    public string? ApproveBy { get; set; } // Người duyệt A
    public string? FinishDTimeUTC { get; set; } // Thời gian duyệt F
    public string? FinishBy { get; set; } // Người duyệt F
    public string? RejectDTimeUTC { get; set; } // Thời gian từ chối
    public string? RejectBy { get; set; } // Người từ chối
    public string? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }
    public string? FlagFileUpload { get; set; } // 1: là file upload; 0: không là file được upload
    public string? AttFileId { get; set; } // Id file upload

    // Các cột mở rộng từ bảng Crd_Member (Card.cs:28139-28144)
    [NotMapped]
    public string? cm_MemberName { get; set; }
    [NotMapped]
    public string? cm_CarNo { get; set; }
    [NotMapped]
    public string? cm_PhoneNo { get; set; }
    [NotMapped]
    public string? cm_VIN { get; set; }
    [NotMapped]
    public double? cm_AmountEnd { get; set; }
    [NotMapped]
    public string? cm_CardTypeUse { get; set; }

    public string? ApproveDLCode { get; set; } // Đơn vị duyệt (lưu đại lý trong DB, hiển thị thêm prefix 'HTV,')

    [NotMapped]
    public string? FlagApproveAF { get; set; } // 1: user hiện tại được duyệt A/F; 0: không (Card.cs:28145-28151)
}

public class Crd_MemberChangeInfoConfig : IEntityTypeConfiguration<Crd_MemberChangeInfo>
{
    public void Configure(EntityTypeBuilder<Crd_MemberChangeInfo> b)
    {
        b.ToTable("Crd_MemberChangeInfo");
        b.HasKey(x => x.RequestNo);
        b.Property(x => x.RequestNo).IsRequired();

        // Bỏ qua các thuộc tính join và tính toán không có trong bảng DB
        b.Ignore(x => x.cm_MemberName);
        b.Ignore(x => x.cm_CarNo);
        b.Ignore(x => x.cm_PhoneNo);
        b.Ignore(x => x.cm_VIN);
        b.Ignore(x => x.cm_AmountEnd);
        b.Ignore(x => x.cm_CardTypeUse);
        b.Ignore(x => x.FlagApproveAF);
    }
}

/// <summary>
/// Seeder dữ liệu mẫu cho Crd_MemberChangeInfo (Order => 52 theo đề bài #027).
/// Idempotent. Đảm bảo có ít nhất 2 hội viên MB001, MB002 và 4 đề nghị REQ01..REQ04.
/// </summary>
public class Crd_MemberChangeInfoSeeder : IPortSeeder
{
    public int Order => 95; // Đặt sau Order 89 của RptPE_CrdMemberPointExpiryMemberSeeder để kiểm tra xung đột MB001

    public async Task SeedAsync(AppDbContext db)
    {
        // 1. Đảm bảo bảng Crd_Member có ít nhất 2 hội viên: MB001, MB002
        if (!await db.CrdMembers.AnyAsync(m => m.MemberNo == "MB001"))
        {
            db.CrdMembers.Add(new CrdMember
            {
                MemberNo = "MB001",
                NetworkID = "1001",
                OrgID = "ORG01",
                MemberName = "Nguyen Van A",
                CarNo = "30A-12345",
                PhoneNo = "0901234567",
                VIN = "VIN001",
                AmountEnd = 1000,
                CardTypeUse = "STANDARD",
                CreateBy = "seed",
                LUBy = "seed"
            });
        }

        if (!await db.CrdMembers.AnyAsync(m => m.MemberNo == "MB002"))
        {
            db.CrdMembers.Add(new CrdMember
            {
                MemberNo = "MB002",
                NetworkID = "1001",
                OrgID = "ORG01",
                MemberName = "Tran Thi B",
                CarNo = "29B-67890",
                PhoneNo = "0987654321",
                VIN = "VIN002",
                AmountEnd = 2000,
                CardTypeUse = "GOLD",
                CreateBy = "seed",
                LUBy = "seed"
            });
        }

        // 2. Seed 4 bản ghi Crd_MemberChangeInfo
        if (!await db.Set<Crd_MemberChangeInfo>().AnyAsync(r => r.RequestNo == "REQ01"))
        {
            db.Set<Crd_MemberChangeInfo>().AddRange(
                new Crd_MemberChangeInfo
                {
                    RequestNo = "REQ01",
                    MemberNo = "MB001",
                    NetworkID = "1001",
                    OrgID = "ORG01",
                    DLCodeRequest = "VN040",
                    RequestType = "CHANGE_INFO",
                    RequestStatus = "PENDING",
                    ApproveDLCode = "VN040",
                    CreateDTimeUTC = "2026-08-01 08:00:00",
                    FinishDTimeUTC = null,
                    LogLUDTimeUTC = "2026-08-01 08:00:00",
                    CreateBy = "seed",
                    LUBy = "seed"
                },
                new Crd_MemberChangeInfo
                {
                    RequestNo = "REQ02",
                    MemberNo = "MB001",
                    NetworkID = "1001",
                    OrgID = "ORG01",
                    DLCodeRequest = "VN040",
                    RequestType = "CHANGE_INFO",
                    RequestStatus = "APPROVE",
                    ApproveDLCode = "VN040",
                    CreateDTimeUTC = "2026-08-05 09:00:00",
                    FinishDTimeUTC = "2026-08-06 10:00:00",
                    LogLUDTimeUTC = "2026-08-05 09:00:00",
                    CreateBy = "seed",
                    LUBy = "seed"
                },
                new Crd_MemberChangeInfo
                {
                    RequestNo = "REQ03",
                    MemberNo = "MB002",
                    NetworkID = "1001",
                    OrgID = "ORG01",
                    DLCodeRequest = "VN041",
                    RequestType = "CHANGE_INFO",
                    RequestStatus = "FINISH",
                    ApproveDLCode = "VN041",
                    CreateDTimeUTC = "2026-08-10 10:00:00",
                    FinishDTimeUTC = "2026-08-11 15:00:00",
                    LogLUDTimeUTC = "2026-08-10 10:00:00",
                    CreateBy = "seed",
                    LUBy = "seed"
                },
                new Crd_MemberChangeInfo
                {
                    RequestNo = "REQ04",
                    MemberNo = "MB999", // MB999 không tồn tại trong Crd_Member -> kiểm chứng inner join draft loại bỏ
                    NetworkID = "1001",
                    OrgID = "ORG01",
                    DLCodeRequest = "VN040",
                    RequestType = "CHANGE_INFO",
                    RequestStatus = "PENDING",
                    ApproveDLCode = "VN040",
                    CreateDTimeUTC = "2026-08-15 11:00:00",
                    FinishDTimeUTC = null,
                    LogLUDTimeUTC = "2026-08-15 11:00:00",
                    CreateBy = "seed",
                    LUBy = "seed"
                }
            );
        }

        // 3. Seed 5 bản ghi REQ05..REQ09 phục vụ GetCountNewRequest (#028)
        if (!await db.Set<Crd_MemberChangeInfo>().AnyAsync(r => r.RequestNo == "REQ05"))
        {
            var now = DateTime.UtcNow;
            db.Set<Crd_MemberChangeInfo>().AddRange(
                new Crd_MemberChangeInfo
                {
                    RequestNo = "REQ05",
                    MemberNo = "MB001", // tồn tại trong Crd_Member
                    NetworkID = "1001",
                    OrgID = "ORG01",
                    DLCodeRequest = "VN040",
                    RequestType = "CHANGE_INFO",
                    RequestStatus = "PENDING",
                    ApproveDLCode = "VN040",
                    CreateDTimeUTC = now.AddDays(-5).ToString("yyyy-MM-dd HH:mm:ss"),
                    FinishDTimeUTC = null,
                    LogLUDTimeUTC = now.AddDays(-5).ToString("yyyy-MM-dd HH:mm:ss"),
                    CreateBy = "seed",
                    LUBy = "seed"
                },
                new Crd_MemberChangeInfo
                {
                    RequestNo = "REQ06",
                    MemberNo = "MB002", // tồn tại trong Crd_Member
                    NetworkID = "1001",
                    OrgID = "ORG01",
                    DLCodeRequest = "VN041",
                    RequestType = "CHANGE_INFO",
                    RequestStatus = "PENDING",
                    ApproveDLCode = "VN041",
                    CreateDTimeUTC = now.AddDays(-15).ToString("yyyy-MM-dd HH:mm:ss"),
                    FinishDTimeUTC = null,
                    LogLUDTimeUTC = now.AddDays(-15).ToString("yyyy-MM-dd HH:mm:ss"),
                    CreateBy = "seed",
                    LUBy = "seed"
                },
                new Crd_MemberChangeInfo
                {
                    RequestNo = "REQ07",
                    MemberNo = "MB001",
                    NetworkID = "1001",
                    OrgID = "ORG01",
                    DLCodeRequest = "VN040",
                    RequestType = "CHANGE_INFO",
                    RequestStatus = "APPROVE",
                    ApproveDLCode = "VN040",
                    CreateDTimeUTC = now.AddDays(-2).ToString("yyyy-MM-dd HH:mm:ss"),
                    FinishDTimeUTC = null,
                    LogLUDTimeUTC = now.AddDays(-2).ToString("yyyy-MM-dd HH:mm:ss"),
                    CreateBy = "seed",
                    LUBy = "seed"
                },
                new Crd_MemberChangeInfo
                {
                    RequestNo = "REQ08",
                    MemberNo = "MB999", // MB999 không tồn tại trong Crd_Member -> INNER JOIN loại bỏ
                    NetworkID = "1001",
                    OrgID = "ORG01",
                    DLCodeRequest = "VN040",
                    RequestType = "CHANGE_INFO",
                    RequestStatus = "PENDING",
                    ApproveDLCode = "VN040",
                    CreateDTimeUTC = now.AddDays(-1).ToString("yyyy-MM-dd HH:mm:ss"),
                    FinishDTimeUTC = null,
                    LogLUDTimeUTC = now.AddDays(-1).ToString("yyyy-MM-dd HH:mm:ss"),
                    CreateBy = "seed",
                    LUBy = "seed"
                },
                new Crd_MemberChangeInfo
                {
                    RequestNo = "REQ09",
                    MemberNo = "MB001",
                    NetworkID = "1001",
                    OrgID = "ORG01",
                    DLCodeRequest = "VN040",
                    RequestType = "CHANGE_INFO",
                    RequestStatus = "PENDING",
                    ApproveDLCode = "VN040",
                    CreateDTimeUTC = now.AddDays(-45).ToString("yyyy-MM-dd HH:mm:ss"),
                    FinishDTimeUTC = null,
                    LogLUDTimeUTC = now.AddDays(-45).ToString("yyyy-MM-dd HH:mm:ss"),
                    CreateBy = "seed",
                    LUBy = "seed"
                }
            );
        }

        await db.SaveChangesAsync();
    }
}
