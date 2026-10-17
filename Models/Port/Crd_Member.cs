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

/// <summary>
/// Seeder bổ sung dữ liệu kiểm thử cho Crd_MemberController/GetByPhoneNo (#041).
/// Idempotent, Order => 57 theo đúng đề bài #041.
/// </summary>
public class Crd_MemberGetByPhoneNoSeeder : IPortSeeder
{
    public int Order => 57;

    public async Task SeedAsync(AppDbContext db)
    {
        if (await db.Set<CrdMember>().AnyAsync(x => x.MemberNo == "MB4101"))
        {
            return;
        }

        var now = DateTime.UtcNow;

        // 1. MB4101: PhoneNo "0901000041", MemberStatus "APPROVE", MemberName "Bui Thi Lan", DLCodeRegis "DL4101", ProvinceCode "P01", DistrictCode "D01", CarNo "30A-41001", VIN "VIN41000000000001", RegisStatus "F", CardTypeUse "G", CardTypeInit "G".
        db.Set<CrdMember>().Add(new CrdMember
        {
            MemberNo = "MB4101",
            NetworkID = "DEMO",
            OrgID = "ORG01",
            DLCodeRegis = "DL4101",
            MemberName = "Bui Thi Lan",
            PhoneNo = "0901000041",
            CarNo = "30A-41001",
            VIN = "VIN41000000000001",
            MemberStatus = "APPROVE",
            RegisStatus = "F",
            CardTypeUse = "G",
            CardTypeInit = "G",
            ProvinceCode = "P01",
            DistrictCode = "D01",
            CustomerCode = "CUS4101",
            IDCardNo = "ID4101",
            DateOfBirth = new DateTime(1991, 1, 1, 0, 0, 0, DateTimeKind.Utc),
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

        // The CD4101: MemberNo "MB4101", CardSourceCode "NEW", CardStatus "APPROVE", CardTypeUse "G".
        if (!await db.Set<CrdCard>().AnyAsync(x => x.CardNo == "CD4101"))
        {
            db.Set<CrdCard>().Add(new CrdCard
            {
                CardNo = "CD4101",
                MemberNo = "MB4101",
                NetworkID = "DEMO",
                CardSourceCode = "NEW",
                CardStatus = "APPROVE",
                CardTypeUse = "G",
                CardTypeInit = "G",
                CreateDTimeUTC = now,
                CreateBy = "seed",
                LUDTimeUTC = now,
                LUBy = "seed",
                LogLUDTimeUTC = now,
                LogLUBy = "seed"
            });
        }

        // 2. MB4102: PhoneNo "0901000041" (CUNG so dien thoai voi MB4101), MemberStatus "APPROVE", MemberName "Hoang Van Nam", DLCodeRegis "DL4101", ProvinceCode "P01", DistrictCode "D01", CarNo "30A-41002", VIN "VIN41000000000002", RegisStatus "F", CardTypeUse "S", CardTypeInit "S".
        db.Set<CrdMember>().Add(new CrdMember
        {
            MemberNo = "MB4102",
            NetworkID = "DEMO",
            OrgID = "ORG01",
            DLCodeRegis = "DL4101",
            MemberName = "Hoang Van Nam",
            PhoneNo = "0901000041",
            CarNo = "30A-41002",
            VIN = "VIN41000000000002",
            MemberStatus = "APPROVE",
            RegisStatus = "F",
            CardTypeUse = "S",
            CardTypeInit = "S",
            ProvinceCode = "P01",
            DistrictCode = "D01",
            CustomerCode = "CUS4102",
            IDCardNo = "ID4102",
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

        // The CD4102: MemberNo "MB4102", CardSourceCode "NEW", CardStatus "APPROVE", CardTypeUse "S".
        if (!await db.Set<CrdCard>().AnyAsync(x => x.CardNo == "CD4102"))
        {
            db.Set<CrdCard>().Add(new CrdCard
            {
                CardNo = "CD4102",
                MemberNo = "MB4102",
                NetworkID = "DEMO",
                CardSourceCode = "NEW",
                CardStatus = "APPROVE",
                CardTypeUse = "S",
                CardTypeInit = "S",
                CreateDTimeUTC = now,
                CreateBy = "seed",
                LUDTimeUTC = now,
                LUBy = "seed",
                LogLUDTimeUTC = now,
                LogLUBy = "seed"
            });
        }

        // 3. MB4103: PhoneNo "0901000043", MemberStatus "PENDING", MemberName "Dang Thi Mai", DLCodeRegis "DL4101", ProvinceCode "P01", DistrictCode "D01".
        db.Set<CrdMember>().Add(new CrdMember
        {
            MemberNo = "MB4103",
            NetworkID = "DEMO",
            OrgID = "ORG01",
            DLCodeRegis = "DL4101",
            MemberName = "Dang Thi Mai",
            PhoneNo = "0901000043",
            MemberStatus = "PENDING",
            ProvinceCode = "P01",
            DistrictCode = "D01",
            CustomerCode = "CUS4103",
            IDCardNo = "ID4103",
            CarNo = "30A-41003",
            VIN = "VIN41000000000003",
            RegisStatus = "P",
            CardTypeUse = "N",
            CardTypeInit = "N",
            DateOfBirth = new DateTime(1993, 3, 3, 0, 0, 0, DateTimeKind.Utc),
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

        // 4. MB4104: PhoneNo "0901000044", MemberStatus "CANCEL", MemberName "Ngo Van Phat", DLCodeRegis "DL4101", ProvinceCode "P01", DistrictCode "D01".
        db.Set<CrdMember>().Add(new CrdMember
        {
            MemberNo = "MB4104",
            NetworkID = "DEMO",
            OrgID = "ORG01",
            DLCodeRegis = "DL4101",
            MemberName = "Ngo Van Phat",
            PhoneNo = "0901000044",
            MemberStatus = "CANCEL",
            ProvinceCode = "P01",
            DistrictCode = "D01",
            CustomerCode = "CUS4104",
            IDCardNo = "ID4104",
            CarNo = "30A-41004",
            VIN = "VIN41000000000004",
            RegisStatus = "C",
            CardTypeUse = "N",
            CardTypeInit = "N",
            DateOfBirth = new DateTime(1994, 4, 4, 0, 0, 0, DateTimeKind.Utc),
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

        // 5. MB4105: PhoneNo "0901000045", MemberStatus "APPROVE", MemberName "Vu Thi Quynh", DLCodeRegis "DL4101", ProvinceCode "P01", DistrictCode "D01", CarNo "30A-41005", VIN "VIN41000000000005", RegisStatus "F", CardTypeUse "P", CardTypeInit "P".
        db.Set<CrdMember>().Add(new CrdMember
        {
            MemberNo = "MB4105",
            NetworkID = "DEMO",
            OrgID = "ORG01",
            DLCodeRegis = "DL4101",
            MemberName = "Vu Thi Quynh",
            PhoneNo = "0901000045",
            CarNo = "30A-41005",
            VIN = "VIN41000000000005",
            MemberStatus = "APPROVE",
            RegisStatus = "F",
            CardTypeUse = "P",
            CardTypeInit = "P",
            ProvinceCode = "P01",
            DistrictCode = "D01",
            CustomerCode = "CUS4105",
            IDCardNo = "ID4105",
            DateOfBirth = new DateTime(1995, 5, 5, 0, 0, 0, DateTimeKind.Utc),
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

        // The CD4105: MemberNo "MB4105", CardSourceCode "NEW", CardStatus "APPROVE", CardTypeUse "P".
        if (!await db.Set<CrdCard>().AnyAsync(x => x.CardNo == "CD4105"))
        {
            db.Set<CrdCard>().Add(new CrdCard
            {
                CardNo = "CD4105",
                MemberNo = "MB4105",
                NetworkID = "DEMO",
                CardSourceCode = "NEW",
                CardStatus = "APPROVE",
                CardTypeUse = "P",
                CardTypeInit = "P",
                CreateDTimeUTC = now,
                CreateBy = "seed",
                LUDTimeUTC = now,
                LUBy = "seed",
                LogLUDTimeUTC = now,
                LogLUBy = "seed"
            });
        }

        // 6. MB4106: PhoneNo "0901000046", MemberStatus "APPROVE", MemberName "Duong Van Thang", DLCodeRegis "DL4101", ProvinceCode "P01", DistrictCode "D01", CarNo "30A-41006", VIN "VIN41000000000006", RegisStatus "F".
        db.Set<CrdMember>().Add(new CrdMember
        {
            MemberNo = "MB4106",
            NetworkID = "DEMO",
            OrgID = "ORG01",
            DLCodeRegis = "DL4101",
            MemberName = "Duong Van Thang",
            PhoneNo = "0901000046",
            CarNo = "30A-41006",
            VIN = "VIN41000000000006",
            MemberStatus = "APPROVE",
            RegisStatus = "F",
            CardTypeUse = "N",
            CardTypeInit = "N",
            ProvinceCode = "P01",
            DistrictCode = "D01",
            CustomerCode = "CUS4106",
            IDCardNo = "ID4106",
            DateOfBirth = new DateTime(1996, 6, 6, 0, 0, 0, DateTimeKind.Utc),
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

        // The CD4106: MemberNo "MB4106", CardSourceCode "NEW", CardStatus "APPROVE".
        if (!await db.Set<CrdCard>().AnyAsync(x => x.CardNo == "CD4106"))
        {
            db.Set<CrdCard>().Add(new CrdCard
            {
                CardNo = "CD4106",
                MemberNo = "MB4106",
                NetworkID = "DEMO",
                CardSourceCode = "NEW",
                CardStatus = "APPROVE",
                CardTypeUse = "N",
                CardTypeInit = "N",
                CreateDTimeUTC = now,
                CreateBy = "seed",
                LUDTimeUTC = now,
                LUBy = "seed",
                LogLUDTimeUTC = now,
                LogLUBy = "seed"
            });
        }

        // 7. MB4107: PhoneNo "0901000046" (CUNG so voi MB4106 nhung PENDING), MemberStatus "PENDING", MemberName "Ly Thi Uyen", DLCodeRegis "DL4101", ProvinceCode "P01", DistrictCode "D01".
        db.Set<CrdMember>().Add(new CrdMember
        {
            MemberNo = "MB4107",
            NetworkID = "DEMO",
            OrgID = "ORG01",
            DLCodeRegis = "DL4101",
            MemberName = "Ly Thi Uyen",
            PhoneNo = "0901000046",
            MemberStatus = "PENDING",
            ProvinceCode = "P01",
            DistrictCode = "D01",
            CustomerCode = "CUS4107",
            IDCardNo = "ID4107",
            CarNo = "30A-41007",
            VIN = "VIN41000000000007",
            RegisStatus = "P",
            CardTypeUse = "N",
            CardTypeInit = "N",
            DateOfBirth = new DateTime(1997, 7, 7, 0, 0, 0, DateTimeKind.Utc),
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

/// <summary>
/// Seeder bổ sung dữ liệu kiểm thử cho Crd_MemberController/GetDetailForDMS (#043).
/// Idempotent, Order => 58 theo đúng đề bài #043.
/// </summary>
public class Crd_MemberGetDetailForDMSSeeder : IPortSeeder
{
    public int Order => 59;

    public async Task SeedAsync(AppDbContext db)
    {
        if (await db.Set<CrdMember>().AnyAsync(x => x.MemberNo == "MB4301"))
        {
            return;
        }

        var now = DateTime.UtcNow;

        // 1. MB4301: CarNo '30A-43001', VIN 'VIN43000000000001', MemberStatus 'APPROVE', DLCodeRegis 'DL4301', The CD4301 ('APPROVE', 'NEW'), Map_QueryDealer_Member (DLCPCode 'DL4301', MemberNo 'MB4301').
        db.Set<CrdMember>().Add(new CrdMember
        {
            MemberNo = "MB4301",
            NetworkID = "DEMO",
            OrgID = "ORG01",
            DLCodeRegis = "DL4301",
            MemberName = "Nguyen Van A4301",
            PhoneNo = "0901000431",
            CarNo = "30A-43001",
            VIN = "VIN43000000000001",
            MemberStatus = "APPROVE",
            RegisStatus = "F",
            CardTypeUse = "N",
            CardTypeInit = "N",
            ProvinceCode = "P01",
            DistrictCode = "D01",
            CustomerCode = "CUS4301",
            IDCardNo = "ID4301",
            DateOfBirth = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
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

        if (!await db.Set<CrdCard>().AnyAsync(x => x.CardNo == "CD4301"))
        {
            db.Set<CrdCard>().Add(new CrdCard
            {
                CardNo = "CD4301",
                MemberNo = "MB4301",
                NetworkID = "DEMO",
                CardSourceCode = "NEW",
                CardStatus = "APPROVE",
                CardTypeUse = "N",
                CardTypeInit = "N",
                CreateDTimeUTC = now,
                CreateBy = "seed",
                LUDTimeUTC = now,
                LUBy = "seed",
                LogLUDTimeUTC = now,
                LogLUBy = "seed"
            });
        }

        if (!await db.Set<Map_QueryDealer_Member>().AnyAsync(x => x.DLCPCode == "DL4301" && x.MemberNo == "MB4301"))
        {
            db.Set<Map_QueryDealer_Member>().Add(new Map_QueryDealer_Member
            {
                DLCPCode = "DL4301",
                MemberNo = "MB4301",
                NetworkID = "DEMO",
                QueryDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                FlagActive = "1",
                LogLUDTimeUTC = now,
                LogLUBy = "seed"
            });
        }

        // 2. MB4302: CarNo '30A-43002', VIN 'VIN43000000000002', MemberStatus 'APPROVE', DLCodeRegis 'DL4301', The CD4302 ('APPROVE', 'NEW'), KHONG co ban ghi Map_QueryDealer_Member cho DL4301.
        db.Set<CrdMember>().Add(new CrdMember
        {
            MemberNo = "MB4302",
            NetworkID = "DEMO",
            OrgID = "ORG01",
            DLCodeRegis = "DL4301",
            MemberName = "Tran Thi B4302",
            PhoneNo = "0901000432",
            CarNo = "30A-43002",
            VIN = "VIN43000000000002",
            MemberStatus = "APPROVE",
            RegisStatus = "F",
            CardTypeUse = "N",
            CardTypeInit = "N",
            ProvinceCode = "P01",
            DistrictCode = "D01",
            CustomerCode = "CUS4302",
            IDCardNo = "ID4302",
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

        if (!await db.Set<CrdCard>().AnyAsync(x => x.CardNo == "CD4302"))
        {
            db.Set<CrdCard>().Add(new CrdCard
            {
                CardNo = "CD4302",
                MemberNo = "MB4302",
                NetworkID = "DEMO",
                CardSourceCode = "NEW",
                CardStatus = "APPROVE",
                CardTypeUse = "N",
                CardTypeInit = "N",
                CreateDTimeUTC = now,
                CreateBy = "seed",
                LUDTimeUTC = now,
                LUBy = "seed",
                LogLUDTimeUTC = now,
                LogLUBy = "seed"
            });
        }

        // 3. MB4303: CarNo '30A-43003', VIN 'VIN43000000000003', MemberStatus 'PENDING' (chua duyet), DLCodeRegis 'DL4301'.
        db.Set<CrdMember>().Add(new CrdMember
        {
            MemberNo = "MB4303",
            NetworkID = "DEMO",
            OrgID = "ORG01",
            DLCodeRegis = "DL4301",
            MemberName = "Le Van C4303",
            PhoneNo = "0901000433",
            CarNo = "30A-43003",
            VIN = "VIN43000000000003",
            MemberStatus = "PENDING",
            RegisStatus = "P",
            CardTypeUse = "N",
            CardTypeInit = "N",
            ProvinceCode = "P01",
            DistrictCode = "D01",
            CustomerCode = "CUS4303",
            IDCardNo = "ID4303",
            DateOfBirth = new DateTime(1993, 3, 3, 0, 0, 0, DateTimeKind.Utc),
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

        // 4. MB4304: CarNo '30A-43004', VIN 'VIN43000000000004', MemberStatus 'CANCEL' (da huy), DLCodeRegis 'DL4301'.
        db.Set<CrdMember>().Add(new CrdMember
        {
            MemberNo = "MB4304",
            NetworkID = "DEMO",
            OrgID = "ORG01",
            DLCodeRegis = "DL4301",
            MemberName = "Pham Thi D4304",
            PhoneNo = "0901000434",
            CarNo = "30A-43004",
            VIN = "VIN43000000000004",
            MemberStatus = "CANCEL",
            RegisStatus = "C",
            CardTypeUse = "N",
            CardTypeInit = "N",
            ProvinceCode = "P01",
            DistrictCode = "D01",
            CustomerCode = "CUS4304",
            IDCardNo = "ID4304",
            DateOfBirth = new DateTime(1994, 4, 4, 0, 0, 0, DateTimeKind.Utc),
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

        // 5. MB4305: CarNo '30A-43005', VIN 'VIN43000000000005', MemberStatus 'APPROVE', DLCodeRegis 'DL4301'.
        db.Set<CrdMember>().Add(new CrdMember
        {
            MemberNo = "MB4305",
            NetworkID = "DEMO",
            OrgID = "ORG01",
            DLCodeRegis = "DL4301",
            MemberName = "Hoang Van E4305",
            PhoneNo = "0901000435",
            CarNo = "30A-43005",
            VIN = "VIN43000000000005",
            MemberStatus = "APPROVE",
            RegisStatus = "F",
            CardTypeUse = "N",
            CardTypeInit = "N",
            ProvinceCode = "P01",
            DistrictCode = "D01",
            CustomerCode = "CUS4305",
            IDCardNo = "ID4305",
            DateOfBirth = new DateTime(1995, 5, 5, 0, 0, 0, DateTimeKind.Utc),
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

        // 6. MB4306: CarNo '30A-43005' (CUNG CarNo voi MB4305), VIN 'VIN43000000000006', MemberStatus 'APPROVE', DLCodeRegis 'DL4301'.
        db.Set<CrdMember>().Add(new CrdMember
        {
            MemberNo = "MB4306",
            NetworkID = "DEMO",
            OrgID = "ORG01",
            DLCodeRegis = "DL4301",
            MemberName = "Vu Van F4306",
            PhoneNo = "0901000436",
            CarNo = "30A-43005",
            VIN = "VIN43000000000006",
            MemberStatus = "APPROVE",
            RegisStatus = "F",
            CardTypeUse = "N",
            CardTypeInit = "N",
            ProvinceCode = "P01",
            DistrictCode = "D01",
            CustomerCode = "CUS4306",
            IDCardNo = "ID4306",
            DateOfBirth = new DateTime(1996, 6, 6, 0, 0, 0, DateTimeKind.Utc),
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

/// <summary>
/// Seeder dữ liệu mẫu cho Crd_Member GetRegisterByMemberNo (port #044).
/// Order => 64, idempotent (kiểm tra tồn tại MB4401).
/// </summary>
public class Crd_MemberGetRegisterByMemberNoSeeder : IPortSeeder
{
    public int Order => 65;

    public async Task SeedAsync(AppDbContext db)
    {
        if (await db.Set<CrdMember>().AnyAsync(x => x.MemberNo == "MB4401"))
        {
            return;
        }

        var now = DateTime.UtcNow;

        // 1. MB4401: MemberStatus = "PENDING", RegisStatus = "P", MemberName = "Nguyen Van Dang Ky", MemberFilePath = "upload\\regis\\mb4401.jpg", RegisFinishDTimeUTC = null, Card CD4401 (MemberNo = "MB4401", CardSourceCode = "NEW", CardStatus = "APPROVE").
        db.Set<CrdMember>().Add(new CrdMember
        {
            MemberNo = "MB4401",
            NetworkID = "DEMO",
            OrgID = "ORG01",
            DLCodeRegis = "DL4401",
            MemberName = "Nguyen Van Dang Ky",
            PhoneNo = "0904400001",
            CarNo = "30A-44001",
            VIN = "VIN44000000000001",
            MemberStatus = "PENDING",
            RegisStatus = "P",
            SourceRegisType = "OFF",
            MemberFilePath = "upload\\regis\\mb4401.jpg",
            RegisFinishDTimeUTC = null,
            RegistrationDate = new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc),
            CustomerCode = "CUS4401",
            ProvinceCode = "P01",
            DistrictCode = "D01",
            IDCardNo = "ID4401",
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

        if (!await db.Set<CrdCard>().AnyAsync(x => x.CardNo == "CD4401"))
        {
            db.Set<CrdCard>().Add(new CrdCard
            {
                CardNo = "CD4401",
                MemberNo = "MB4401",
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

        // 2. MB4402: MemberStatus = "APPROVE", RegisStatus = "F", MemberName = "Tran Thi Hoan Tat", MemberFilePath = null, RegisFinishDTimeUTC = new DateTime(2026, 4, 15, 2, 30, 0, DateTimeKind.Utc), Card CD4402 (MemberNo = "MB4402", CardSourceCode = "NEW", CardStatus = "APPROVE").
        db.Set<CrdMember>().Add(new CrdMember
        {
            MemberNo = "MB4402",
            NetworkID = "DEMO",
            OrgID = "ORG01",
            DLCodeRegis = "DL4401",
            MemberName = "Tran Thi Hoan Tat",
            PhoneNo = "0904400002",
            CarNo = "30A-44002",
            VIN = "VIN44000000000002",
            MemberStatus = "APPROVE",
            RegisStatus = "F",
            SourceRegisType = "OFF",
            MemberFilePath = null,
            RegisFinishDTimeUTC = new DateTime(2026, 4, 15, 2, 30, 0, DateTimeKind.Utc),
            RegistrationDate = new DateTime(2026, 2, 10, 0, 0, 0, DateTimeKind.Utc),
            CustomerCode = "CUS4402",
            ProvinceCode = "P01",
            DistrictCode = "D01",
            IDCardNo = "ID4402",
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

        if (!await db.Set<CrdCard>().AnyAsync(x => x.CardNo == "CD4402"))
        {
            db.Set<CrdCard>().Add(new CrdCard
            {
                CardNo = "CD4402",
                MemberNo = "MB4402",
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

        // 3. MB4403: MemberStatus = "CANCEL", RegisStatus = "C", MemberName = "Le Van Da Huy", MemberFilePath = "upload/avatar/mb4403.png", RegisFinishDTimeUTC = null, Card CD4403 (MemberNo = "MB4403", CardSourceCode = "NEW", CardStatus = "APPROVE").
        db.Set<CrdMember>().Add(new CrdMember
        {
            MemberNo = "MB4403",
            NetworkID = "DEMO",
            OrgID = "ORG01",
            DLCodeRegis = "DL4401",
            MemberName = "Le Van Da Huy",
            PhoneNo = "0904400003",
            CarNo = "30A-44003",
            VIN = "VIN44000000000003",
            MemberStatus = "CANCEL",
            RegisStatus = "C",
            SourceRegisType = "OFF",
            MemberFilePath = "upload/avatar/mb4403.png",
            RegisFinishDTimeUTC = null,
            RegistrationDate = new DateTime(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc),
            CustomerCode = "CUS4403",
            ProvinceCode = "P01",
            DistrictCode = "D01",
            IDCardNo = "ID4403",
            DateOfBirth = new DateTime(1985, 3, 3, 0, 0, 0, DateTimeKind.Utc),
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

        if (!await db.Set<CrdCard>().AnyAsync(x => x.CardNo == "CD4403"))
        {
            db.Set<CrdCard>().Add(new CrdCard
            {
                CardNo = "CD4403",
                MemberNo = "MB4403",
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

        // 4. MB4404: MemberStatus = "PENDING", RegisStatus = "P", MemberName = "Pham Van Khong The", KHONG co the NEW nao trong Crd_Card.
        db.Set<CrdMember>().Add(new CrdMember
        {
            MemberNo = "MB4404",
            NetworkID = "DEMO",
            OrgID = "ORG01",
            DLCodeRegis = "DL4401",
            MemberName = "Pham Van Khong The",
            PhoneNo = "0904400004",
            CarNo = "30A-44004",
            VIN = "VIN44000000000004",
            MemberStatus = "PENDING",
            RegisStatus = "P",
            SourceRegisType = "OFF",
            MemberFilePath = null,
            RegisFinishDTimeUTC = null,
            RegistrationDate = new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc),
            CustomerCode = "CUS4404",
            ProvinceCode = "P01",
            DistrictCode = "D01",
            IDCardNo = "ID4404",
            DateOfBirth = new DateTime(1994, 4, 4, 0, 0, 0, DateTimeKind.Utc),
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

        await db.SaveChangesAsync();
    }
}

/// <summary>
/// Seeder dữ liệu mẫu cho Crd_Member/GetCalcPointBuyCreta (#045).
/// Idempotent, Order => 66 theo đúng đề bài #045.
/// </summary>
public class Crd_MemberGetCalcPointBuyCretaSeeder : IPortSeeder
{
    public int Order => 66;

    public async Task SeedAsync(AppDbContext db)
    {
        if (await db.Set<CrdMember>().AnyAsync(x => x.MemberNo == "MB4501"))
        {
            return;
        }

        var now = DateTime.UtcNow;

        // MB4501: MemberNo = "MB4501", NetworkID = "DEMO", OrgID = "ORG01", DLCodeRegis = "DL4501", MemberName = "Tran Creta", PhoneNo = "0904500001", CarNo = "30A-45001", VIN = "VIN45000000000001", DealNo = "DEAL4501", PointBuyCreta = 3000000, MemberStatus = "APPROVE", RegisStatus = "F".
        db.Set<CrdMember>().Add(new CrdMember
        {
            MemberNo = "MB4501",
            NetworkID = "DEMO",
            OrgID = "ORG01",
            DLCodeRegis = "DL4501",
            MemberName = "Tran Creta",
            PhoneNo = "0904500001",
            CarNo = "30A-45001",
            VIN = "VIN45000000000001",
            DealNo = "DEAL4501",
            PointBuyCreta = 3000000,
            MemberStatus = "APPROVE",
            RegisStatus = "F",
            RegistrationDate = new DateTime(2025, 6, 15, 0, 0, 0, DateTimeKind.Utc),
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




/// <summary>Seeder cho Crd_Member Delete/Cancel/Approve/ApproveByDealer (Order => 71).</summary>
public class Crd_MemberStatusSeeder : IPortSeeder
{
    public int Order => 71;

    public async Task SeedAsync(AppDbContext db)
    {
        if (await db.Set<CrdMember>().AnyAsync(x => x.MemberNo == "MB_S1")) return;
        var now = new DateTime(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc);
        CrdMember Mk(string no, string regis, string use, string init) => new CrdMember
        {
            MemberNo = no, NetworkID = "DEMO", OrgID = "ORG01", DLCodeRegis = "DL01", MemberName = "Seed " + no, VIN = "VIN" + no, CarNo = "CAR" + no,
            RegisStatus = regis, MemberStatus = "PENDING", CardTypeUse = use, CardTypeInit = init,
            CreateDTimeUTC = now, CreateBy = "seed", LUDTimeUTC = now, LUBy = "seed", LogLUDTimeUTC = now, LogLUBy = "seed"
        };
        db.Set<CrdMember>().AddRange(Mk("MB_S1", "PENDING", "N", "N"), Mk("MB_S2", "APPROVE1", "N", "P"), Mk("MB_S3", "APPROVE1", "N", "N"), Mk("MB_S4", "APPROVE2", "N", "P"));
        await db.SaveChangesAsync();
    }
}
