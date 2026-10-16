using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MiniLoyalty.Data;
using MiniLoyalty.Models;

namespace MiniLoyalty.Models.Port;

// ===========================================================================================
// PORT 1:1 từ dự án nguồn 2022.7.Loyalty — Crd_Member (#039).
// Bảng Crd_Member đã được định nghĩa tại Models/PortLoyalty.cs (CrdMember) và ánh xạ trong AppDbContext.cs.
// Tuân thủ Bài học A.24 và PORT_RULES dòng 39: Tái sử dụng CrdMember, KHÔNG tạo entity thứ 2.
// ===========================================================================================

/// <summary>
/// Seeder dữ liệu mẫu cho Crd_Member và Crd_Card tương ứng (port #039).
/// Idempotent, Order => 55 theo đúng đề bài #039.
/// </summary>
public class Crd_MemberSeeder : IPortSeeder
{
    public int Order => 55;

    public async Task SeedAsync(AppDbContext db)
    {
        if (await db.Set<CrdMember>().AnyAsync(x => x.MemberNo == "MB3901"))
        {
            return;
        }

        var now = DateTime.UtcNow;

        // 1. MB3901: OrgID "ORG01", DLCodeRegis "DL3901", MemberName "Nguyen Van An", PhoneNo "0901000001", CarNo "30A-11111", VIN "VIN39000000000001", MemberStatus "APPROVE", RegisStatus "F", SourceRegisType "OFF", RegistrationDate "2026-01-10", CardNo "CD3901", CardActiveDate "2026-01-15".
        db.Set<CrdMember>().Add(new CrdMember
        {
            MemberNo = "MB3901",
            NetworkID = "DEMO",
            OrgID = "ORG01",
            DLCodeRegis = "DL3901",
            MemberName = "Nguyen Van An",
            PhoneNo = "0901000001",
            CarNo = "30A-11111",
            VIN = "VIN39000000000001",
            MemberStatus = "APPROVE",
            RegisStatus = "F",
            SourceRegisType = "OFF",
            RegistrationDate = new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc),
            MemberActiveDate = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc),
            CustomerCode = "CUS3901",
            ProvinceCode = "P01",
            DistrictCode = "D01",
            IDCardNo = "ID3901",
            DateOfBirth = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            MemAddress = "Ha Noi",
            ModelCode = "M01",
            TradeMarkCode = "TM01",
            CardTypeUse = "N",
            CardTypeInit = "N",
            CreateDTimeUTC = now,
            CreateBy = "seed",
            LUDTimeUTC = now,
            LUBy = "seed",
            LogLUDTimeUTC = now,
            LogLUBy = "seed"
        });

        if (!await db.Set<CrdCard>().AnyAsync(x => x.CardNo == "CD3901"))
        {
            db.Set<CrdCard>().Add(new CrdCard
            {
                CardNo = "CD3901",
                MemberNo = "MB3901",
                NetworkID = "DEMO",
                CardSourceCode = "NEW",
                CardActiveDate = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc),
                CardTypeUse = "N",
                CardTypeInit = "N",
                CardStatus = "APPROVE",
                CreateDTimeUTC = now,
                CreateBy = "seed",
                LUDTimeUTC = now,
                LUBy = "seed",
                LogLUDTimeUTC = now,
                LogLUBy = "seed"
            });
        }

        // 2. MB3902: OrgID "ORG01", DLCodeRegis "DL3901", MemberName "Tran Thi Binh", PhoneNo "0901000002", CarNo "30A-22222", VIN "VIN39000000000002", MemberStatus "PENDING", RegisStatus "P", SourceRegisType "ON", RegistrationDate "2026-02-10", CardNo "CD3902", CardActiveDate "2026-02-15".
        db.Set<CrdMember>().Add(new CrdMember
        {
            MemberNo = "MB3902",
            NetworkID = "DEMO",
            OrgID = "ORG01",
            DLCodeRegis = "DL3901",
            MemberName = "Tran Thi Binh",
            PhoneNo = "0901000002",
            CarNo = "30A-22222",
            VIN = "VIN39000000000002",
            MemberStatus = "PENDING",
            RegisStatus = "P",
            SourceRegisType = "ON",
            RegistrationDate = new DateTime(2026, 2, 10, 0, 0, 0, DateTimeKind.Utc),
            MemberActiveDate = new DateTime(2026, 2, 15, 0, 0, 0, DateTimeKind.Utc),
            CustomerCode = "CUS3902",
            ProvinceCode = "P01",
            DistrictCode = "D01",
            IDCardNo = "ID3902",
            DateOfBirth = new DateTime(1992, 2, 2, 0, 0, 0, DateTimeKind.Utc),
            MemAddress = "Ha Noi",
            ModelCode = "M01",
            TradeMarkCode = "TM01",
            CardTypeUse = "N",
            CardTypeInit = "N",
            CreateDTimeUTC = now,
            CreateBy = "seed",
            LUDTimeUTC = now,
            LUBy = "seed",
            LogLUDTimeUTC = now,
            LogLUBy = "seed"
        });

        if (!await db.Set<CrdCard>().AnyAsync(x => x.CardNo == "CD3902"))
        {
            db.Set<CrdCard>().Add(new CrdCard
            {
                CardNo = "CD3902",
                MemberNo = "MB3902",
                NetworkID = "DEMO",
                CardSourceCode = "NEW",
                CardActiveDate = new DateTime(2026, 2, 15, 0, 0, 0, DateTimeKind.Utc),
                CardTypeUse = "N",
                CardTypeInit = "N",
                CardStatus = "APPROVE",
                CreateDTimeUTC = now,
                CreateBy = "seed",
                LUDTimeUTC = now,
                LUBy = "seed",
                LogLUDTimeUTC = now,
                LogLUBy = "seed"
            });
        }

        // 3. MB3903: OrgID "ORG02", DLCodeRegis "DL3902", MemberName "Le Van Cuong", PhoneNo "0901000003", CarNo "15A-33333", VIN "VIN39000000000003", MemberStatus "CANCEL", RegisStatus "C", SourceRegisType "OFF", RegistrationDate "2026-03-10", CardNo "CD3903", CardActiveDate "2026-03-15".
        db.Set<CrdMember>().Add(new CrdMember
        {
            MemberNo = "MB3903",
            NetworkID = "DEMO",
            OrgID = "ORG02",
            DLCodeRegis = "DL3902",
            MemberName = "Le Van Cuong",
            PhoneNo = "0901000003",
            CarNo = "15A-33333",
            VIN = "VIN39000000000003",
            MemberStatus = "CANCEL",
            RegisStatus = "C",
            SourceRegisType = "OFF",
            RegistrationDate = new DateTime(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc),
            MemberActiveDate = new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc),
            CustomerCode = "CUS3903",
            ProvinceCode = "P02",
            DistrictCode = "D02",
            IDCardNo = "ID3903",
            DateOfBirth = new DateTime(1985, 3, 3, 0, 0, 0, DateTimeKind.Utc),
            MemAddress = "Hai Phong",
            ModelCode = "M01",
            TradeMarkCode = "TM01",
            CardTypeUse = "N",
            CardTypeInit = "N",
            CreateDTimeUTC = now,
            CreateBy = "seed",
            LUDTimeUTC = now,
            LUBy = "seed",
            LogLUDTimeUTC = now,
            LogLUBy = "seed"
        });

        if (!await db.Set<CrdCard>().AnyAsync(x => x.CardNo == "CD3903"))
        {
            db.Set<CrdCard>().Add(new CrdCard
            {
                CardNo = "CD3903",
                MemberNo = "MB3903",
                NetworkID = "DEMO",
                CardSourceCode = "NEW",
                CardActiveDate = new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc),
                CardTypeUse = "N",
                CardTypeInit = "N",
                CardStatus = "APPROVE",
                CreateDTimeUTC = now,
                CreateBy = "seed",
                LUDTimeUTC = now,
                LUBy = "seed",
                LogLUDTimeUTC = now,
                LogLUBy = "seed"
            });
        }

        await db.SaveChangesAsync();
    }
}

/// <summary>
/// Seeder bổ sung dữ liệu kiểm thử cho Crd_MemberController/GetByMemberNo (#040).
/// Idempotent, Order => 56 theo đúng đề bài #040.
/// </summary>
public class Crd_MemberGetByMemberNoSeeder : IPortSeeder
{
    public int Order => 56;

    public async Task SeedAsync(AppDbContext db)
    {
        if (await db.Set<CrdMember>().AnyAsync(x => x.MemberNo == "MB4001"))
        {
            return;
        }

        var now = DateTime.UtcNow;

        // MB4001: MemberNo "MB4001", MemberName "Pham Van Dung", MemberStatus "APPROVE", DLCodeRegis "DL4001", PhoneNo "0901000040", CarNo "30A-40001", VIN "VIN40000000000001", CardTypeUse "G", CardTypeInit "G", RegisStatus "F", RegistrationDate "2026-01-01", RegisFinishDTimeUTC DateTime(2026, 1, 5, 3, 0, 0, Utc) -> sau khi cong +7 thanh "2026-01-05 10:00:00".
        db.Set<CrdMember>().Add(new CrdMember
        {
            MemberNo = "MB4001",
            NetworkID = "DEMO",
            OrgID = "ORG01",
            DLCodeRegis = "DL4001",
            MemberName = "Pham Van Dung",
            MemberStatus = "APPROVE",
            PhoneNo = "0901000040",
            CarNo = "30A-40001",
            VIN = "VIN40000000000001",
            CardTypeUse = "G",
            CardTypeInit = "G",
            RegisStatus = "F",
            RegistrationDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            RegisFinishDTimeUTC = new DateTime(2026, 1, 5, 3, 0, 0, DateTimeKind.Utc),
            CustomerCode = "CUS4001",
            ProvinceCode = "P01",
            DistrictCode = "D01",
            IDCardNo = "ID4001",
            DateOfBirth = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            MemAddress = "Ha Noi",
            ModelCode = "M01",
            TradeMarkCode = "TM01",
            CreateDTimeUTC = now,
            CreateBy = "seed",
            LUDTimeUTC = now,
            LUBy = "seed",
            LogLUDTimeUTC = now,
            LogLUBy = "seed"
        });

        // The Crd_Card cho MB4001 gom 2 the:
        // CD4001_OLD: MemberNo "MB4001", CardSourceCode "NEW", CreateDTimeUTC now.AddDays(-10)
        // CD4001_NEW: MemberNo "MB4001", CardSourceCode "REPLACE", CreateDTimeUTC now.AddDays(-1)
        if (!await db.Set<CrdCard>().AnyAsync(x => x.CardNo == "CD4001_OLD"))
        {
            db.Set<CrdCard>().Add(new CrdCard
            {
                CardNo = "CD4001_OLD",
                MemberNo = "MB4001",
                NetworkID = "DEMO",
                CardSourceCode = "NEW",
                CardTypeUse = "G",
                CardTypeInit = "G",
                CardStatus = "APPROVE",
                CreateDTimeUTC = now.AddDays(-10),
                CreateBy = "seed",
                LUDTimeUTC = now,
                LUBy = "seed",
                LogLUDTimeUTC = now,
                LogLUBy = "seed"
            });
        }

        if (!await db.Set<CrdCard>().AnyAsync(x => x.CardNo == "CD4001_NEW"))
        {
            db.Set<CrdCard>().Add(new CrdCard
            {
                CardNo = "CD4001_NEW",
                MemberNo = "MB4001",
                NetworkID = "DEMO",
                CardSourceCode = "REPLACE",
                CardTypeUse = "G",
                CardTypeInit = "G",
                CardStatus = "APPROVE",
                CreateDTimeUTC = now.AddDays(-1),
                CreateBy = "seed",
                LUDTimeUTC = now,
                LUBy = "seed",
                LogLUDTimeUTC = now,
                LogLUBy = "seed"
            });
        }

        // Anh xa Map_QueryDealer_Member cho MB4001: DLCPCode "DL4001", MemberNo "MB4001"
        if (!await db.Set<Map_QueryDealer_Member>().AnyAsync(x => x.DLCPCode == "DL4001" && x.MemberNo == "MB4001"))
        {
            db.Set<Map_QueryDealer_Member>().Add(new Map_QueryDealer_Member
            {
                DLCPCode = "DL4001",
                MemberNo = "MB4001",
                NetworkID = "DEMO",
                QueryDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                FlagActive = "1",
                LogLUDTimeUTC = now,
                LogLUBy = "seed"
            });
        }

        // MB4002: MemberNo "MB4002", MemberName "Hoang Thi Hoa", MemberStatus "PENDING", DLCodeRegis "DL4001"
        db.Set<CrdMember>().Add(new CrdMember
        {
            MemberNo = "MB4002",
            NetworkID = "DEMO",
            OrgID = "ORG01",
            DLCodeRegis = "DL4001",
            MemberName = "Hoang Thi Hoa",
            MemberStatus = "PENDING",
            CustomerCode = "CUS4002",
            ProvinceCode = "P01",
            DistrictCode = "D01",
            IDCardNo = "ID4002",
            PhoneNo = "0901000042",
            CarNo = "30A-40002",
            VIN = "VIN40000000000002",
            CardTypeUse = "N",
            CardTypeInit = "N",
            RegisStatus = "P",
            DateOfBirth = new DateTime(1992, 2, 2, 0, 0, 0, DateTimeKind.Utc),
            MemAddress = "Ha Noi",
            ModelCode = "M01",
            TradeMarkCode = "TM01",
            RegistrationDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            CreateDTimeUTC = now,
            CreateBy = "seed",
            LUDTimeUTC = now,
            LUBy = "seed",
            LogLUDTimeUTC = now,
            LogLUBy = "seed"
        });

        // MB4003: MemberNo "MB4003", MemberName "Do Van Giang", MemberStatus "CANCEL", DLCodeRegis "DL4001"
        db.Set<CrdMember>().Add(new CrdMember
        {
            MemberNo = "MB4003",
            NetworkID = "DEMO",
            OrgID = "ORG01",
            DLCodeRegis = "DL4001",
            MemberName = "Do Van Giang",
            MemberStatus = "CANCEL",
            CustomerCode = "CUS4003",
            ProvinceCode = "P01",
            DistrictCode = "D01",
            IDCardNo = "ID4003",
            PhoneNo = "0901000043",
            CarNo = "30A-40003",
            VIN = "VIN40000000000003",
            CardTypeUse = "N",
            CardTypeInit = "N",
            RegisStatus = "C",
            DateOfBirth = new DateTime(1985, 3, 3, 0, 0, 0, DateTimeKind.Utc),
            MemAddress = "Ha Noi",
            ModelCode = "M01",
            TradeMarkCode = "TM01",
            RegistrationDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            CreateDTimeUTC = now,
            CreateBy = "seed",
            LUDTimeUTC = now,
            LUBy = "seed",
            LogLUDTimeUTC = now,
            LogLUBy = "seed"
        });

        await db.SaveChangesAsync();
    }
}

