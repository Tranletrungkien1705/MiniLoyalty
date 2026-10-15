using Microsoft.EntityFrameworkCore;
using MiniLoyalty.Models;

namespace MiniLoyalty.Data;

public static class Seeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        await db.Database.EnsureCreatedAsync();
        await MigratePostgresAsync(db);   // DB cloud cũ: thêm Orgs + cột OrgId nếu thiếu
        await EnsureNewTablesAsync(db);   // 4 bảng port 1:1 Crd_* thêm SAU khi DB đã tồn tại

        // Org mặc định (tenant cho dữ liệu seed + UI không kèm ApiKey).
        if (!await db.Orgs.AnyAsync(o => o.Id == TenantContext.DefaultOrgId))
        {
            db.Orgs.Add(new Org { Id = TenantContext.DefaultOrgId, Name = "Demo Loyalty", ApiKey = TenantContext.DefaultApiKey });
            await db.SaveChangesAsync();
        }

        if (!await db.RankTiers.AnyAsync())
        {
            db.RankTiers.AddRange(
                new RankTier { Name = "Thành viên", MinLifetimePoints = 0, DiscountPercent = 0, ColorHex = "#94a3b8", SortOrder = 0 },
                new RankTier { Name = "Bạc", MinLifetimePoints = 500, DiscountPercent = 3, ColorHex = "#9ca3af", SortOrder = 1 },
                new RankTier { Name = "Vàng", MinLifetimePoints = 2000, DiscountPercent = 5, ColorHex = "#f59e0b", SortOrder = 2 },
                new RankTier { Name = "Bạch kim", MinLifetimePoints = 5000, DiscountPercent = 8, ColorHex = "#6366f1", SortOrder = 3 },
                new RankTier { Name = "Kim cương", MinLifetimePoints = 10000, DiscountPercent = 10, ColorHex = "#06b6d4", SortOrder = 4 });
            await db.SaveChangesAsync();
        }
        if (!await db.Rewards.AnyAsync())
        {
            db.Rewards.AddRange(
                new Reward { Name = "Voucher giảm 50.000đ", PointCost = 500, Description = "Áp dụng đơn từ 300.000đ" },
                new Reward { Name = "Voucher giảm 100.000đ", PointCost = 900, Description = "Áp dụng đơn từ 500.000đ" },
                new Reward { Name = "Freeship toàn quốc", PointCost = 300, Description = "Miễn phí vận chuyển 1 đơn" },
                new Reward { Name = "Quà sinh nhật đặc biệt", PointCost = 1500, Description = "Set quà tặng thành viên VIP", Stock = 20 });
            await db.SaveChangesAsync();
        }
        if (!await db.Members.AnyAsync())
        {
            var tiers = await db.RankTiers.OrderBy(t => t.SortOrder).ToListAsync();
            RankTier Rank(int lp) => tiers.Last(t => lp >= t.MinLifetimePoints);
            int n = 0;
            Member M(string name, string phone, int lp, int bal)
            {
                n++;
                var r = Rank(lp);
                return new Member
                {
                    Code = $"HV{DateTime.Now:yy}{n:D5}", Name = name, Phone = phone, Points = bal, LifetimePoints = lp,
                    RankTierId = r.Id, JoinedAt = DateTime.Now.AddMonths(-n * 2),
                    Dob = new DateTime(1990 + n, ((n * 3) % 12) + 1, ((n * 5) % 27) + 1),
                    Transactions = [ new PointTransaction { Type = PointTxType.Adjust, Points = lp, BalanceAfter = bal, Note = "Số dư đầu kỳ", CreatedAt = DateTime.Now.AddMonths(-n * 2) } ]
                };
            }
            db.Members.AddRange(
                M("Nguyễn Văn An", "0901111111", 12500, 3200),
                M("Trần Thị Bình", "0902222222", 6200, 1800),
                M("Lê Hoàng Cường", "0903333333", 2400, 900),
                M("Phạm Thu Dung", "0904444444", 700, 350),
                M("Vũ Minh Đức", "0905555555", 120, 120)
            );
            await db.SaveChangesAsync();
        }
    }

    /// <summary>
    /// DB Postgres cloud đã tồn tại (EnsureCreated bỏ qua): tạo bảng Orgs + thêm cột OrgId nếu thiếu,
    /// backfill dữ liệu cũ về Org mặc định. Idempotent (IF NOT EXISTS).
    /// </summary>
    private static async Task MigratePostgresAsync(AppDbContext db)
    {
        if (!db.Database.IsNpgsql()) return;
        var def = TenantContext.DefaultOrgId;
        var sql = new[]
        {
            "CREATE TABLE IF NOT EXISTS miniloyalty.\"Orgs\" (\"Id\" uuid PRIMARY KEY, \"Name\" text NOT NULL DEFAULT '', \"ApiKey\" text NOT NULL DEFAULT '', \"CreatedAt\" timestamp NOT NULL DEFAULT now())",
            "CREATE UNIQUE INDEX IF NOT EXISTS \"IX_Orgs_ApiKey\" ON miniloyalty.\"Orgs\" (\"ApiKey\")",
            $"ALTER TABLE miniloyalty.\"Members\" ADD COLUMN IF NOT EXISTS \"OrgId\" uuid NOT NULL DEFAULT '{def}'",
            $"ALTER TABLE miniloyalty.\"PointTransactions\" ADD COLUMN IF NOT EXISTS \"OrgId\" uuid NOT NULL DEFAULT '{def}'",
            $"ALTER TABLE miniloyalty.\"Rewards\" ADD COLUMN IF NOT EXISTS \"OrgId\" uuid NOT NULL DEFAULT '{def}'",
            // Code trước đây unique TOÀN CỤC (bug: 2 org khác nhau tạo hội viên thứ N đầu tiên bị trùng mã) — đổi sang unique theo (OrgId, Code).
            "DROP INDEX IF EXISTS miniloyalty.\"IX_Members_Code\"",
            "CREATE UNIQUE INDEX IF NOT EXISTS \"IX_Members_OrgId_Code\" ON miniloyalty.\"Members\" (\"OrgId\", \"Code\")",
        };
        foreach (var s in sql)
            try { await db.Database.ExecuteSqlRawAsync(s); } catch { }
    }

    // ==========================================================================================
    // PORT 1:1 2022.7.Loyalty — hạ tầng cho 4 bảng Crd_* mới
    // ==========================================================================================

    /// <summary>
    /// `EnsureCreated` CHỈ tạo bảng khi DB chưa tồn tại — DB SQLite/Postgres đang chạy đã tồn tại nên
    /// 4 bảng Crd_* thêm mới sẽ KHÔNG được tạo. Hàm này sinh DDL từ **metadata EF** (không viết tay
    /// 260 cột × 2 provider) nên tên/kiểu cột luôn khớp entity, và chạy được cả SQLite lẫn Postgres.
    /// Idempotent nhờ `CREATE TABLE IF NOT EXISTS`.
    /// </summary>
    private static async Task EnsureNewTablesAsync(AppDbContext db)
    {
        var pg = db.Database.IsNpgsql();
        // 4 bảng Crd_* port đợt đầu + MỌI entity trong namespace MiniLoyalty.Models.Port (port về sau, xem Data/PortInfra.cs).
        var legacy = new[] { typeof(CrdMember), typeof(CrdCard), typeof(CrdCardTransaction), typeof(CrdMemberDiscountTransaction) };
        var entityTypes = db.Model.GetEntityTypes()
            .Where(e => legacy.Contains(e.ClrType) || (e.ClrType.Namespace ?? "").StartsWith("MiniLoyalty.Models.Port"))
            .ToList();
        foreach (var et in entityTypes)
        {
            var schema = et.GetSchema();
            var table = (string.IsNullOrEmpty(schema) ? "" : Q(schema) + ".") + Q(et.GetTableName()!);
            var pk = et.FindPrimaryKey();
            var pkProps = pk?.Properties.ToList() ?? [];

            var cols = new List<string>();
            foreach (var p in et.GetProperties())
            {
                var name = Q(p.GetColumnName());
                var store = p.GetColumnType() ?? p.GetRelationalTypeMapping().StoreType;
                var solePk = pkProps.Count == 1 && pkProps[0] == p;
                // Khoá chính tự tăng (AutoId IDENTITY(1,1) ở nguồn): mỗi provider một cú pháp riêng.
                if (solePk && p.ValueGenerated == Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAdd
                    && (p.ClrType == typeof(long) || p.ClrType == typeof(int)))
                    cols.Add(pg
                        ? $"{name} bigint GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY"
                        : $"{name} INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT");
                else
                    cols.Add($"{name} {store} {(p.IsNullable ? "NULL" : "NOT NULL")}"
                             + (solePk ? " PRIMARY KEY" : ""));
            }
            // PK nhiều cột: khai riêng (4 bảng hiện tại đều PK 1 cột, giữ nhánh này cho về sau).
            if (pkProps.Count > 1)
                cols.Add("PRIMARY KEY (" + string.Join(", ", pkProps.Select(p => Q(p.GetColumnName()))) + ")");

            // Không có dấu ngoặc nhọn nào trong câu lệnh -> an toàn với ExecuteSqlRaw
            // (bẫy đã biết: `{ }` bị coi là placeholder tham số và nuốt lỗi câm).
            try { await db.Database.ExecuteSqlRawAsync($"CREATE TABLE IF NOT EXISTS {table} (" + string.Join(", ", cols) + ")"); }
            catch { /* bảng đã có với schema khác -> bỏ qua, endpoint sẽ báo lỗi rõ khi truy vấn */ }

            // Bảng đã có từ trước nhưng entity vừa được bổ sung cột (port thêm action) -> thêm cột còn thiếu.
            await AddMissingColumnsAsync(db, et, schema, et.GetTableName()!, table, pg);
        }
        await SeedCrdDemoAsync(db);

        // Dữ liệu mẫu của từng module port (mỗi module 1 class IPortSeeder trong file riêng).
        var seeders = typeof(Seeder).Assembly.GetTypes()
            .Where(t => typeof(IPortSeeder).IsAssignableFrom(t) && t is { IsAbstract: false, IsInterface: false })
            .Select(t => (IPortSeeder)Activator.CreateInstance(t)!)
            .OrderBy(s => s.Order).ThenBy(s => s.GetType().Name);
        foreach (var s in seeders) await s.SeedAsync(db);

        static string Q(string id) => "\"" + id.Replace("\"", "\"\"") + "\"";
    }

    /// <summary>
    /// So cột thật trong DB với metadata EF, `ADD COLUMN` cột còn thiếu. Cột thêm sau luôn cho phép NULL
    /// (SQLite không cho `ADD COLUMN ... NOT NULL` mà không có mặc định — bẫy #47).
    /// </summary>
    private static async Task AddMissingColumnsAsync(AppDbContext db, Microsoft.EntityFrameworkCore.Metadata.IEntityType et,
        string? schema, string tableName, string quotedTable, bool pg)
    {
        var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var conn = db.Database.GetDbConnection();
        var wasOpen = conn.State == System.Data.ConnectionState.Open;
        if (!wasOpen) await conn.OpenAsync();
        try
        {
            await using var cmd = conn.CreateCommand();
            if (pg)
            {
                cmd.CommandText = "select column_name from information_schema.columns where table_name = @t and table_schema = coalesce(@s, current_schema())";
                var pt = cmd.CreateParameter(); pt.ParameterName = "@t"; pt.Value = tableName; cmd.Parameters.Add(pt);
                var ps = cmd.CreateParameter(); ps.ParameterName = "@s"; ps.Value = (object?)schema ?? DBNull.Value; cmd.Parameters.Add(ps);
            }
            else
            {
                cmd.CommandText = "select name from pragma_table_info(@t)";
                var pt = cmd.CreateParameter(); pt.ParameterName = "@t"; pt.Value = tableName; cmd.Parameters.Add(pt);
            }
            await using var rd = await cmd.ExecuteReaderAsync();
            while (await rd.ReadAsync()) existing.Add(rd.GetString(0));
        }
        finally { if (!wasOpen) await conn.CloseAsync(); }
        if (existing.Count == 0) return;   // bảng chưa tạo được -> không ALTER mù

        foreach (var p in et.GetProperties())
        {
            var col = p.GetColumnName();
            if (existing.Contains(col)) continue;
            var store = p.GetColumnType() ?? p.GetRelationalTypeMapping().StoreType;
            try { await db.Database.ExecuteSqlRawAsync($"ALTER TABLE {quotedTable} ADD COLUMN \"{col.Replace("\"", "\"\"")}\" {store} NULL"); }
            catch (Exception ex) when (ex.Message.Contains("duplicate column", StringComparison.OrdinalIgnoreCase)
                                       || ex.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase)) { }
        }
    }

    /// <summary>
    /// Dữ liệu mẫu cho 4 màn port 1:1 — đủ để thấy taxonomy hạng I/N/S/G/P
    /// (Const.Main.BE.cs:1592) và cả 2 loại dòng của màn giao dịch: điểm thường vs chiết khấu DISCOUNTRO
    /// (Master.Loyalty.cs:21735). KHÔNG phải data thật, chỉ để UI có gì mà hiện.
    /// </summary>
    private static async Task SeedCrdDemoAsync(AppDbContext db)
    {
        try { if (await db.CrdMembers.AnyAsync()) return; } catch { return; }

        const string net = "HTV", org = "HTV";
        var now = DateTime.Now;
        // (mã HV, tên, SĐT, biển số, VIN, tỉnh, huyện, hạng sử dụng, hạng thực, trạng thái, đại lý)
        var rows = new (string No, string Name, string Phone, string Car, string Vin, string Pv, string Dt, string Use, string Init, string St, string Dl)[]
        {
            ("HV0000001", "Nguyễn Văn An",  "0901111111", "30A-11111", "RLHJF1234LY000001", "01", "001", LoyaltyConst.CardType.P, LoyaltyConst.CardType.P, LoyaltyConst.MemberStatus.Approve, "VS058"),
            ("HV0000002", "Trần Thị Bình",  "0902222222", "30A-22222", "RLHJF1234LY000002", "01", "002", LoyaltyConst.CardType.G, LoyaltyConst.CardType.S, LoyaltyConst.MemberStatus.Approve, "VS058"),
            ("HV0000003", "Lê Hoàng Cường", "0903333333", "29B-33333", "RLHJF1234LY000003", "02", "010", LoyaltyConst.CardType.S, LoyaltyConst.CardType.S, LoyaltyConst.MemberStatus.Approve, "VS045"),
            ("HV0000004", "Phạm Thu Dung",  "0904444444", "29C-44444", "RLHJF1234LY000004", "02", "011", LoyaltyConst.CardType.N, LoyaltyConst.CardType.N, LoyaltyConst.MemberStatus.Approve, "VS045"),
            ("HV0000005", "Vũ Minh Đức",    "0905555555", "51F-55555", "RLHJF1234LY000005", "03", "020", LoyaltyConst.CardType.I, LoyaltyConst.CardType.I, LoyaltyConst.MemberStatus.Pending, "VS065"),
        };

        int i = 0;
        foreach (var r in rows)
        {
            i++;
            var effStart = new DateTime(now.Year, 1, 1);
            var effEnd = effStart.AddYears(1).AddDays(-1);
            db.CrdMembers.Add(new CrdMember
            {
                MemberNo = r.No, NetworkID = net, OrgID = org, DLCodeRegis = r.Dl,
                RegistrationDate = now.AddMonths(-i * 3), CustomerCode = "KH" + r.No[2..],
                ProvinceCode = r.Pv, DistrictCode = r.Dt, MemberName = r.Name,
                MemberGender = i % 2 == 0 ? "0" : "1", IDCardNo = "0010900000" + i, PhoneNo = r.Phone,
                DateOfBirth = new DateTime(1985 + i, ((i * 3) % 12) + 1, ((i * 5) % 27) + 1),
                MemAddress = $"Số {i * 7} phố Demo, Quận {r.Dt}", CarNo = r.Car, VIN = r.Vin,
                ModelCode = "ACCENT", ModelName = "Accent 1.4AT", TradeMarkCode = "HYUNDAI",
                EffDateStart = effStart, EffDateEnd = effEnd,
                PointTotal = 1000d * i, PointAvail = 800d * i, PointEnd = 800d * i,
                AmountTotal = 12_000_000d * i, AmountAvail = 10_000_000d * i, AmountEnd = 10_000_000d * i,
                QtyVisitTotal = i, QtyVisitAvail = i, QtyVisitEnd = i,
                PointCardRank = 1200d * i, AmountCardRank = 12_000_000d * i,
                CardTypeUse = r.Use, CardTypeInit = r.Init,
                MemberActiveDate = r.St == LoyaltyConst.MemberStatus.Approve ? now.AddMonths(-i * 3) : null,
                FlagExceptionally = "0", PointVoucher = 200d * i, PointActualVoucher = 150d * i,
                MemberStatus = r.St,
                RegisStatus = r.St == LoyaltyConst.MemberStatus.Approve ? LoyaltyConst.RegisStatus.Finish : LoyaltyConst.RegisStatus.Pending,
                SourceRegisType = i % 2 == 0 ? "ON" : "OFF",
                PurchaseType = LoyaltyConst.PurchaseType.BuyNew,
                WarrantyExpiryDate = now.AddYears(2), InvoiceDate = now.AddMonths(-i * 3),
                CardSourceCode = LoyaltyConst.CardSourceCode.New, CardStatus = r.St,
                CreateDTimeUTC = now.AddMonths(-i * 3), CreateBy = "seed",
                LUDTimeUTC = now, LUBy = "seed", LogLUDTimeUTC = now, LogLUBy = "seed",
                Remark = "Dữ liệu mẫu port 1:1 2022.7.Loyalty",
            });

            var cardNo = $"{r.No}-{now.Year}";
            db.CrdCards.Add(new CrdCard
            {
                CardNo = cardNo, MemberNo = r.No, NetworkID = net,
                CardSourceCode = i == 1 ? LoyaltyConst.CardSourceCode.Up : LoyaltyConst.CardSourceCode.New,
                RankPolicyCode = "RP2026", DLCodeExceptionally = null,
                PointBonus = 0, PointPurchaseAddCar = 0,
                EffDateStart = effStart, EffDateEnd = effEnd,
                PointTotal = 1000d * i, PointAvail = 800d * i,
                AmountTotal = 12_000_000d * i, AmountAvail = 10_000_000d * i,
                QtyVisitTotal = i, QtyVisitAvail = i, PointCardRank = 1200d * i,
                TotalAmountPeriod = 12_000_000d * i,
                CardTypeUse = r.Use, CardTypeInit = r.Init,
                CardTypeUsePrev = i == 1 ? LoyaltyConst.CardType.G : r.Use, CardTypeInitPrev = r.Init,
                CardTypeUseExpect = r.Use, CardTypeInitExpect = r.Init,
                RankActionType = i == 1 ? "UP" : "NORMAL",
                CardActiveDate = now.AddMonths(-i * 3), FlagExceptionally = "0",
                CardStatus = r.St,
                ApproveDTimeUTC = r.St == LoyaltyConst.MemberStatus.Approve ? now.AddMonths(-i * 3) : null,
                ApproveBy = r.St == LoyaltyConst.MemberStatus.Approve ? "seed" : null,
                CreateDTimeUTC = now.AddMonths(-i * 3), CreateBy = "seed",
                LUDTimeUTC = now, LUBy = "seed", LogLUDTimeUTC = now, LogLUBy = "seed",
            });

            // 2 giao dịch điểm thường: tích tiêu dùng + tích xét hạng.
            foreach (var (dpt, pt, amt) in new[]
            {
                (LoyaltyConst.DealPointType.Consumption, 500d * i, 5_000_000d * i),
                (LoyaltyConst.DealPointType.PointIncrease, 700d * i, 7_000_000d * i),
            })
                db.CrdCardTransactions.Add(new CrdCardTransaction
                {
                    RefNo = $"RO{now:yyMM}{i:D4}{dpt[..3]}", ProjectCode = "LOYALTY",
                    NetworkID = net, OrgID = org, DLCode = r.Dl, MemberNo = r.No, CardNo = cardNo,
                    CardSourceCode = LoyaltyConst.CardSourceCode.New,
                    CardTypeUse = r.Use, CardTypeInit = r.Init, DealPointType = dpt,
                    EffDateStart = effStart, EffDateEnd = effEnd,
                    PointChTotal = pt, AmountChTotal = pt, PointChRankTotal = pt,
                    PointChTotalInit = pt, AmountChTotalInit = pt,
                    PointChTotalDiffer = 0, AmountChTotalDiffer = 0, AmountAvailPrevious = 0,
                    PointRef = pt, AmountRef = amt,
                    PolicyPointUnit = 1, PolicyAmountUnit = 10_000, PolicyCode = "RP2026",
                    ParamPointUnit = 1, ParamAmountUnit = 1, ParamCode = "STD",
                    FlagViewUI = "1", FlagPoint = "1", FlagAmount = "1", FlagPr = "0",
                    FunctionName = "Crd_CardTransaction_Seed", FunctionActionType = "ADD",
                    RefType = "RO", PointExpiryDTime = effEnd,
                    CreateDTimeUTC = now.AddDays(-i * 10), CreateBy = "seed",
                    LUDTimeUTC = now, LUBy = "seed", LogLUDTimeUTC = now.AddDays(-i * 10), LogLUBy = "seed",
                    DLCodeExceptionally = null,
                });

            // 1 giao dịch chiết khấu dịch vụ (bảng RIÊNG) — để màn giao dịch union được DISCOUNTRO.
            db.CrdMemberDiscountTransactions.Add(new CrdMemberDiscountTransaction
            {
                RefNo = $"RO{now:yyMM}{i:D4}DC", NetworkID = net, OrgID = org, DLCode = r.Dl,
                MemberNo = r.No, CardNo = cardNo,
                CardTypeUse = r.Use, CardTypeInit = r.Init,
                CardTypeApply = LoyaltyConst.CardType.Rank(r.Use) >= LoyaltyConst.CardType.Rank(r.Init) ? r.Use : r.Init,
                DealPointType = LoyaltyConst.DealPointType.DiscountRO,
                PolicyCode = "DC2026", PolicyDiscountRate = i * 2d,
                FlagExceptionally = "0", EffDateStart = effStart, EffDateEnd = effEnd,
                AmountForDC = 3_000_000d * i, PointChTotal = 3_000_000d * i * (i * 2d) / 100d,
                CreateDate = now.AddDays(-i * 5),
                CreateDTimeUTC = now.AddDays(-i * 5), CreateBy = "seed",
                LUDTimeUTC = now, LUBy = "seed", LogLUDTimeUTC = now.AddDays(-i * 5), LogLUBy = "seed",
            });
        }
        await db.SaveChangesAsync();
    }
}
