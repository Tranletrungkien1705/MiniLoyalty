using Microsoft.EntityFrameworkCore;
using MiniLoyalty.Models;

namespace MiniLoyalty.Data;

public static class Seeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        await db.Database.EnsureCreatedAsync();
        await MigratePostgresAsync(db);   // DB cloud cũ: thêm Orgs + cột OrgId nếu thiếu

        // Org mặc định (tenant cho dữ liệu seed + UI không kèm ApiKey).
        if (!await db.Orgs.AnyAsync(o => o.Id == TenantContext.DefaultOrgId))
        {
            db.Orgs.Add(new Org { Id = TenantContext.DefaultOrgId, Name = "Demo Loyalty", ApiKey = TenantContext.DefaultApiKey });
            await db.SaveChangesAsync();
        }

        if (!await db.RankTiers.AnyAsync())
        {
            // PointKeepBegin/QtyVisitKeepBegin = ngưỡng DUY TRÌ hạng trong kỳ (Mst_RankPolicy) — dùng cho job xét hạng cuối kỳ.
            // PointUpBegin/QtyVisitUpBegin = ngưỡng NÂNG hạng trong kỳ — đạt cả hai thì lên hạng kế tiếp (UP).
            db.RankTiers.AddRange(
                new RankTier { Name = "Thành viên", MinLifetimePoints = 0, DiscountPercent = 0, BirthdayPoints = 50, BirthdayVoucherPoints = 0, BirthdayVoucherExpireDays = 0, ColorHex = "#94a3b8", SortOrder = 0, PointKeepBegin = 0, QtyVisitKeepBegin = 0, PointUpBegin = 300, QtyVisitUpBegin = 1 },
                new RankTier { Name = "Bạc", MinLifetimePoints = 500, DiscountPercent = 3, BirthdayPoints = 100, BirthdayVoucherPoints = 100, BirthdayVoucherExpireDays = 30, ColorHex = "#9ca3af", SortOrder = 1, PointKeepBegin = 300, QtyVisitKeepBegin = 1, PointUpBegin = 1000, QtyVisitUpBegin = 2 },
                new RankTier { Name = "Vàng", MinLifetimePoints = 2000, DiscountPercent = 5, BirthdayPoints = 200, BirthdayVoucherPoints = 200, BirthdayVoucherExpireDays = 45, ColorHex = "#f59e0b", SortOrder = 2, PointKeepBegin = 1000, QtyVisitKeepBegin = 2, PointUpBegin = 2500, QtyVisitUpBegin = 3 },
                new RankTier { Name = "Bạch kim", MinLifetimePoints = 5000, DiscountPercent = 8, BirthdayPoints = 300, BirthdayVoucherPoints = 300, BirthdayVoucherExpireDays = 60, ColorHex = "#6366f1", SortOrder = 3, PointKeepBegin = 2500, QtyVisitKeepBegin = 3, PointUpBegin = 5000, QtyVisitUpBegin = 4 },
                new RankTier { Name = "Kim cương", MinLifetimePoints = 10000, DiscountPercent = 10, BirthdayPoints = 500, BirthdayVoucherPoints = 500, BirthdayVoucherExpireDays = 90, ColorHex = "#06b6d4", SortOrder = 4, PointKeepBegin = 5000, QtyVisitKeepBegin = 4, PointUpBegin = 0, QtyVisitUpBegin = 0 });
            await db.SaveChangesAsync();
        }
        if (!await db.ServicePolicies.AnyAsync())
        {
            // Chính sách quy đổi tiền dịch vụ → điểm theo hạng (Mst_PolicyMoneyToPointServiceDtl):
            // hạng càng cao, cứ mỗi ConvertValue đồng doanh thu RO được càng nhiều điểm (ConvertPoint).
            var tiers = await db.RankTiers.OrderBy(t => t.SortOrder).ToListAsync();
            var rates = new (decimal Value, int Point)[] { (1000, 1), (1000, 1), (1000, 2), (1000, 3), (1000, 4) };
            for (var i = 0; i < tiers.Count; i++)
                db.ServicePolicies.Add(new ServicePolicy
                {
                    PolicyCode = "DEFAULT", RankTierId = tiers[i].Id,
                    ConvertValue = rates[i].Value, ConvertPoint = rates[i].Point
                });
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
        if (!await db.Promotions.AnyAsync())
        {
            db.Promotions.AddRange(
                new Promotion { Code = "PR-BAODUONG", Name = "Gói bảo dưỡng nhanh", PointCost = 800, Description = "Miễn phí 1 lần bảo dưỡng định kỳ", Qty = 50 },
                new Promotion { Code = "PR-PHUKIEN", Name = "Phụ kiện chính hãng", PointCost = 1200, Description = "Đổi phụ kiện trị giá 1.200.000đ", Qty = 30 },
                new Promotion { Code = "PR-RUAXE", Name = "Thẻ rửa xe 6 tháng", PointCost = 400, Description = "Rửa xe không giới hạn 6 tháng", Qty = 100 });
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
                    // Kỳ xét hạng đã hết hạn (EffDateEnd quá khứ) → job xét hạng cuối kỳ sẽ xử lý.
                    // PointCardRank/QtyVisitAvail: có người đạt ngưỡng duy trì, có người không (để minh hoạ KEEP/DOWN).
                    EffDateStart = DateTime.Today.AddMonths(-12), EffDateEnd = DateTime.Today.AddDays(-1), CardSourceCode = "NEW",
                    PointCardRank = lp / 2, QtyVisitAvail = n % 4,
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
            // Hội viên đạt ngưỡng NÂNG hạng trong kỳ (Bạc: PointUpBegin=1000, QtyVisitUpBegin=2) → minh hoạ UP.
            var up = M("Đỗ Thị Hoa", "0906666666", 800, 400);
            up.PointCardRank = 1200; up.QtyVisitAvail = 2;
            db.Members.Add(up);
            // Hội viên có sinh nhật đúng hôm nay — để minh hoạ job tặng điểm sinh nhật.
            var bd = db.Members.Local.First();
            bd.Dob = new DateTime(1992, DateTime.Today.Month, DateTime.Today.Day);
            // Hội viên hạng Vàng có sinh nhật hôm nay — để minh hoạ job phát voucher sinh nhật (DealPointType=VOUCHERTSN).
            var bdVch = M("Phan Thị Mai", "0910444444", 2600, 1000);
            bdVch.Dob = new DateTime(1991, DateTime.Today.Month, DateTime.Today.Day);
            db.Members.Add(bdVch);
            // Hội viên mới có khai báo người giới thiệu (MemberNoIntro) + điểm thưởng (PointIntro)
            // — để minh hoạ nghiệp vụ thưởng điểm giới thiệu (DealPointType=INTRODUCTION).
            var intro = M("Hoàng Văn Khoa", "0907777777", 0, 0);
            intro.MemberNoIntro = db.Members.Local.First().Code;   // người giới thiệu = hội viên đầu tiên
            intro.PointIntro = 200;
            db.Members.Add(intro);
            // Hội viên mua xe mới có điểm thưởng (Crd_Member.PointBuyCar) — minh hoạ nghiệp vụ tặng điểm mua xe (DealPointType=SALES).
            var buyCar = M("Trịnh Văn Sơn", "0909990000", 0, 0);
            buyCar.PointBuyCar = 5000;
            db.Members.Add(buyCar);
            // Hội viên mua xe khuyến mại có điểm khuyến mại bán hàng (Crd_Member.PointBuyCreta) — minh hoạ nghiệp vụ HTV tặng điểm (DealPointType=KMBH).
            var kmbh = M("Đặng Quốc Huy", "0910555555", 0, 0);
            kmbh.PointBuyCreta = 3000;
            db.Members.Add(kmbh);
            // Hội viên mới có điểm tặng mở thẻ (Crd_Member.PointOpenCard) — minh hoạ nghiệp vụ tặng điểm mở thẻ (DealPointType=OPENCARD).
            var openCard = M("Nguyễn Thị Bích", "0910666666", 0, 0);
            openCard.PointOpenCard = 150000;
            db.Members.Add(openCard);
            // Ghi nhận lượt dịch vụ (DealPointType=SERVICETURN) — minh hoạ nghiệp vụ cộng lượt vào QtyVisitAvail.
            var svcTurn = M("Bùi Thanh Tùng", "0908888888", 300, 300);
            svcTurn.QtyVisitAvail = 2;
            svcTurn.Transactions.Add(new PointTransaction { Type = PointTxType.ServiceTurn, Points = 0, QtyVisit = 2, BalanceAfter = 300, Note = "Ghi nhận 2 lượt dịch vụ", RefNo = "RO-DEMO-001" });
            db.Members.Add(svcTurn);
            // Giao dịch chiết khấu dịch vụ (DealPointType=DISCOUNTRO) — minh hoạ áp % chiết khấu theo hạng.
            var disc = M("Ngô Thị Lan", "0909999999", 2600, 1200);
            disc.Discounts.Add(new MemberDiscountTransaction
            {
                RefNo = "RO-DEMO-002", CardTypeApplyId = disc.RankTierId,
                PolicyDiscountRate = Rank(2600).DiscountPercent, AmountForDC = 2_000_000,
                DiscountAmount = Math.Round(2_000_000 * Rank(2600).DiscountPercent / 100m, 0, MidpointRounding.AwayFromZero),
                CreatedAt = DateTime.Now.AddDays(-3)
            });
            db.Members.Add(disc);
            // Điểm voucher xe mới (DealPointType=VOUCHERXM/VOUCHERSD) — minh hoạ tặng + sử dụng điểm voucher.
            var vch = M("Đinh Quốc Bảo", "0910111111", 1500, 700);
            vch.PointVoucher = 3000;
            vch.Vouchers.Add(new MemberVoucherTransaction
            {
                Type = VoucherTxType.Award, Points = 5000, BalanceAfter = 5000, VoucherCode = "VCH-NEWCAR-2026",
                RefNo = "VOUCHERXM-DEMO-001", ExpiryDate = DateTime.Today.AddMonths(12),
                Note = "Tặng điểm voucher xe mới (VCH-NEWCAR-2026)", CreatedAt = DateTime.Now.AddDays(-10)
            });
            vch.Vouchers.Add(new MemberVoucherTransaction
            {
                Type = VoucherTxType.Use, Points = -2000, BalanceAfter = 3000, VoucherCode = "VCH-NEWCAR-2026",
                RefNo = "VOUCHERSD-DEMO-001", Note = "Sử dụng điểm voucher (VCH-NEWCAR-2026)", CreatedAt = DateTime.Now.AddDays(-4)
            });
            db.Members.Add(vch);
            // Sử dụng điểm đổi ưu đãi (DealPointType=POINTUSE, Crd_DealUsePromotion) — minh hoạ trừ điểm khả dụng theo ưu đãi.
            var prm = M("Lý Minh Quân", "0911222222", 3000, 2500);
            var pr = db.Promotions.Local.First();
            prm.PromotionUses.Add(new MemberPromotionUse
            {
                PromotionId = pr.Id, PrProgramCode = pr.Code, Points = -pr.PointCost, BalanceAfter = 2500 - pr.PointCost,
                RefNo = "DUP-DEMO-001", Note = $"Sử dụng ưu đãi {pr.Code} ({pr.Name})", CreatedAt = DateTime.Now.AddDays(-2)
            });
            prm.Transactions.Add(new PointTransaction { Type = PointTxType.PointUse, Points = -pr.PointCost, BalanceAfter = 2500 - pr.PointCost, Note = $"Sử dụng ưu đãi: {pr.Name}", RefNo = "DUP-DEMO-001", CreatedAt = DateTime.Now.AddDays(-2) });
            db.Members.Add(prm);
            // Ghi nhận sử dụng ưu đãi KHÔNG trừ điểm (DealPointType=PRPROGRAM, Crd_DealUsePromotion) — minh hoạ tracking số lần dùng ưu đãi.
            var prmRec = M("Trần Quốc Bảo", "0911777777", 1200, 1200);
            var prRec = db.Promotions.Local.First();
            prmRec.PromotionUses.Add(new MemberPromotionUse
            {
                PromotionId = prRec.Id, Kind = PromotionUseKind.PrProgram, PrProgramCode = prRec.Code,
                Points = 0, BalanceAfter = 1200, QtyPrChTotal = 2, QtyPrUsed = 2,
                RefNo = "DUP-DEMO-002", Note = $"Ghi nhận sử dụng ưu đãi {prRec.Code} ({prRec.Name}) x2", CreatedAt = DateTime.Now.AddDays(-1)
            });
            prmRec.Transactions.Add(new PointTransaction { Type = PointTxType.PrProgram, Points = 0, BalanceAfter = 1200, PrProgramCode = prRec.Code, QtyPrChTotal = 2, QtyPrUsed = 2, Note = $"Ghi nhận sử dụng ưu đãi: {prRec.Name} (x2)", RefNo = "DUP-DEMO-002", CreatedAt = DateTime.Now.AddDays(-1) });
            db.Members.Add(prmRec);
            // Tích điểm tiêu dùng dịch vụ (DealPointType=CONSUMPTION) — minh hoạ quy đổi doanh thu RO thành điểm theo hạng.
            var cons = M("Hồ Nhật Nam", "0911333333", 1500, 800);
            cons.PointCardRank = 1500;
            cons.QtyVisitAvail = 1;
            cons.Transactions.Add(new PointTransaction
            {
                Type = PointTxType.Consumption, Points = 1500, BalanceAfter = 800, AmountChTotal = 1_500_000,
                PointChRankTotal = 1500, QtyVisit = 1, RefNo = "RO-DEMO-003",
                Note = "Tích điểm tiêu dùng dịch vụ 1.500.000đ (1.000đ = 1 điểm)", CreatedAt = DateTime.Now.AddDays(-5)
            });
            db.Members.Add(cons);
            // Tích điểm xét hạng nhập tay (DealPointType=POINTINCREASE) — minh hoạ cộng điểm xét hạng (không đổi điểm khả dụng).
            var inc = M("Đinh Văn Tú", "0911444444", 400, 400);
            inc.PointCardRank = 400;
            inc.Transactions.Add(new PointTransaction
            {
                Type = PointTxType.PointIncrease, Points = 0, PointChRankTotal = 600, BalanceAfter = 400,
                RefNo = "INC-DEMO-001", Note = "Tích điểm xét hạng (hỗ trợ) +600 điểm", CreatedAt = DateTime.Now.AddDays(-6)
            });
            inc.PointCardRank += 600;
            db.Members.Add(inc);
            // Hội viên đã bị vô hiệu hoá (Crd_Member_InActiveX) — minh hoạ trạng thái Cancel + thẻ huỷ.
            var inact = M("Trương Văn Lộc", "0911555555", 800, 0);
            inact.Status = MemberStatus.Cancel;
            inact.CardStatus = CardStatus.Cancel;
            inact.InactiveAt = DateTime.Now.AddDays(-7);
            inact.InactiveBy = "HTV";
            inact.Remark = "Khách bán xe cũ cho người khác";
            db.Members.Add(inact);
            // Điều chỉnh điểm hỗ trợ (DealPointType=SUPPORT) — minh hoạ nhân viên hỗ trợ cộng/trừ điểm thủ công.
            var sup = M("Đoàn Thị Ngọc", "0911666666", 1000, 1000);
            sup.Transactions.Add(new PointTransaction
            {
                Type = PointTxType.Support, Points = 500, BalanceAfter = 1500, DLCode = "SUPPORT",
                FunctionRemark = "Bù điểm khiếu nại hóa đơn dịch vụ", RefNo = "SUP-DEMO-001",
                Note = "Điều chỉnh điểm hỗ trợ +500 — Bù điểm khiếu nại hóa đơn dịch vụ", CreatedAt = DateTime.Now.AddDays(-1)
            });
            sup.Points = 1500;
            sup.LifetimePoints = 1500;
            db.Members.Add(sup);
            await db.SaveChangesAsync();
        }
        if (!await db.MemberColumnChanges.AnyAsync())
        {
            // Whitelist cột được phép đề nghị thay đổi (Crd_MemberColumnChange) — dùng cho yêu cầu Crd_MemberChangeInfo.
            db.MemberColumnChanges.AddRange(
                new MemberColumnChange { ColumnCode = "MemberName", ColumnName = "Họ tên" },
                new MemberColumnChange { ColumnCode = "PhoneNo", ColumnName = "Số điện thoại" },
                new MemberColumnChange { ColumnCode = "Email", ColumnName = "Email" },
                new MemberColumnChange { ColumnCode = "DateOfBirth", ColumnName = "Ngày sinh" });
            await db.SaveChangesAsync();
        }
        if (!await db.MemberChangeRequests.AnyAsync())
        {
            // Yêu cầu thay đổi thông tin mẫu (Crd_MemberChangeInfo) — minh hoạ luồng duyệt PENDING → APPROVE → FINISH.
            var m = await db.Members.OrderBy(x => x.Id).FirstAsync();
            var req = new MemberChangeRequest
            {
                RequestNo = $"CRQ.{DateTime.Now:yyyy}.{m.Code}.001",
                MemberId = m.Id, RequestType = ChangeRequestType.ChangeInfo, Status = ChangeRequestStatus.Pending,
                DLCodeRequest = "DL-DEMO-001", Remark = "Khách đổi số điện thoại và email", CreatedAt = DateTime.Now.AddDays(-1)
            };
            req.Details.Add(new MemberChangeRequestDtl { ColumnCode = "PhoneNo", ColumnValueNew = "0988777666" });
            req.Details.Add(new MemberChangeRequestDtl { ColumnCode = "Email", ColumnValueNew = "an.nguyen@example.com" });
            db.MemberChangeRequests.Add(req);
            await db.SaveChangesAsync();
        }
        if (!await db.CardExceptions.AnyAsync())
        {
            // Yêu cầu đặc cách thẻ mẫu (Crd_Card_RequestExceptionX) — minh hoạ luồng duyệt PENDING → APPROVE.
            // Kỳ thẻ đặc cách kế thừa hạng thẻ hiện tại, kèm danh sách đại lý chỉ định (Crd_CardDealerUseException).
            var m = await db.Members.OrderBy(x => x.Id).Skip(1).FirstAsync();
            var ex = new CardException
            {
                MemberId = m.Id, CardNo = $"CEX.{DateTime.Now:yyyy}.{m.Code}.001", CardNoPrev = m.Code,
                CardTypeUseId = m.RankTierId, DLCodeExceptionally = "DL-DEMO-001",
                Status = CardExceptionStatus.Pending, Remark = "Khách yêu cầu dùng thẻ tại đại lý khác tỉnh",
                CreatedAt = DateTime.Now.AddHours(-6)
            };
            ex.Dealers.Add(new CardExceptionDealer { DealerCode = "DL-HN-001" });
            ex.Dealers.Add(new CardExceptionDealer { DealerCode = "DL-HCM-002" });
            db.CardExceptions.Add(ex);
            await db.SaveChangesAsync();
        }
        if (!await db.RankHistories.AnyAsync())
        {
            // Lịch sử xét hạng mẫu (Crd_CardRank, DealPointType=LOYALTY) — minh hoạ audit trail UP/KEEP/DOWN
            // kèm ảnh chụp hạng trước/sau của hội viên.
            var tiers = await db.RankTiers.OrderBy(t => t.SortOrder).ToListAsync();
            var ms = await db.Members.OrderBy(x => x.Id).Take(3).ToListAsync();
            if (ms.Count >= 3)
            {
                var d = DateTime.Today.AddMonths(-12);
                db.RankHistories.AddRange(
                    new RankHistory
                    {
                        CardRankNo = $"CRK.{d:yyyyMMdd}.{ms[0].Code}", MemberId = ms[0].Id, RankPolicyCode = "DEFAULT",
                        Action = RankActionType.Up, CardSourceCode = "UP", DealPointType = "LOYALTY",
                        FunctionName = "RunRankKeepDownJobAsync", FunctionRemark = $"Xét hạng cuối kỳ {d:dd/MM/yyyy}: {tiers[1].Name} → {tiers[2].Name}",
                        RankTierIdBefore = tiers[1].Id, PointCardRankBefore = 1200, QtyVisitBefore = 2,
                        RankTierIdAfter = tiers[2].Id, PointCardRankAfter = 0, QtyVisitAfter = 0,
                        EffDateStart = d, EffDateEnd = d.AddMonths(12), CreatedAt = d, CreatedBy = "SYSTEM"
                    },
                    new RankHistory
                    {
                        CardRankNo = $"CRK.{d:yyyyMMdd}.{ms[1].Code}", MemberId = ms[1].Id, RankPolicyCode = "DEFAULT",
                        Action = RankActionType.Keep, CardSourceCode = "KEEP", DealPointType = "LOYALTY",
                        FunctionName = "RunRankKeepDownJobAsync", FunctionRemark = $"Xét hạng cuối kỳ {d:dd/MM/yyyy}: {tiers[2].Name} → {tiers[2].Name}",
                        RankTierIdBefore = tiers[2].Id, PointCardRankBefore = 1500, QtyVisitBefore = 3,
                        RankTierIdAfter = tiers[2].Id, PointCardRankAfter = 0, QtyVisitAfter = 0,
                        EffDateStart = d, EffDateEnd = d.AddMonths(12), CreatedAt = d, CreatedBy = "SYSTEM"
                    },
                    new RankHistory
                    {
                        CardRankNo = $"CRK.{d:yyyyMMdd}.{ms[2].Code}", MemberId = ms[2].Id, RankPolicyCode = "DEFAULT",
                        Action = RankActionType.Down, CardSourceCode = "DOWN", DealPointType = "LOYALTY",
                        FunctionName = "RunRankKeepDownJobAsync", FunctionRemark = $"Xét hạng cuối kỳ {d:dd/MM/yyyy}: {tiers[2].Name} → {tiers[1].Name}",
                        RankTierIdBefore = tiers[2].Id, PointCardRankBefore = 200, QtyVisitBefore = 0,
                        RankTierIdAfter = tiers[1].Id, PointCardRankAfter = 0, QtyVisitAfter = 0,
                        EffDateStart = d, EffDateEnd = d.AddMonths(12), CreatedAt = d, CreatedBy = "SYSTEM"
                    });
                await db.SaveChangesAsync();
            }
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
        };
        foreach (var s in sql)
            try { await db.Database.ExecuteSqlRawAsync(s); } catch { }
    }
}
