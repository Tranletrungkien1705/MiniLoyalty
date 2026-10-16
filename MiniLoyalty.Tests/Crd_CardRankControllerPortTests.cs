using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MiniLoyalty.Controllers.Port;
using MiniLoyalty.Data;
using MiniLoyalty.Models;
using MiniLoyalty.Models.Port;
using Xunit;

namespace MiniLoyalty.Tests;

public class Crd_CardRankControllerPortTests
{
    private class ApiResp
    {
        public bool Success { get; set; }
        public object? Data { get; set; }
        public string? ErrorCode { get; set; }
        public Dictionary<string, object?>? Params { get; set; }
    }

    private static async Task<(AppDbContext db, Crd_CardRankControllerPort controller, SqliteConnection conn)> CreateContextAsync()
    {
        var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        var opt = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(conn)
            .Options;
        var db = new AppDbContext(opt, new TenantContext { OrgId = TenantContext.DefaultOrgId });
        db.Database.EnsureCreated();
        await Seeder.SeedAsync(db);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Biz_NetworkID"] = "1001"
            })
            .Build();

        var controller = new Crd_CardRankControllerPort(db, config);
        return (db, controller, conn);
    }

    private static ApiResp ToResp(IActionResult res)
    {
        if (res is OkObjectResult okRes)
        {
            var json = JsonSerializer.Serialize(okRes.Value);
            return JsonSerializer.Deserialize<ApiResp>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        }
        if (res is ObjectResult objRes)
        {
            var json = JsonSerializer.Serialize(objRes.Value);
            return JsonSerializer.Deserialize<ApiResp>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        }
        throw new InvalidOperationException($"Unexpected result type: {res.GetType()}");
    }

    [Fact]
    public async Task ApproveRankDown_EmptyBody_ReturnsFailCheckStrJson()
    {
        var (db, controller, conn) = await CreateContextAsync();
        using (conn)
        {
            var res = await controller.ApproveRankDown(null);
            var resp = ToResp(res);
            Assert.False(resp.Success);
            Assert.Equal("ErridnLoyalty.Crd_CardRank_ApproveRankDown", resp.ErrorCode);
            Assert.NotNull(resp.Params);
            Assert.True(resp.Params.ContainsKey("Check.strJson"));
        }
    }

    [Fact]
    public async Task ApproveRankDown_NullStrJson_ReturnsFailCheckStrJson()
    {
        var (db, controller, conn) = await CreateContextAsync();
        using (conn)
        {
            var res = await controller.ApproveRankDown(new Crd_CardRankApproveRankDownReq { strJson = null });
            var resp = ToResp(res);
            Assert.False(resp.Success);
            Assert.Equal("ErridnLoyalty.Crd_CardRank_ApproveRankDown", resp.ErrorCode);
            Assert.NotNull(resp.Params);
            Assert.True(resp.Params.ContainsKey("Check.strJson"));
        }
    }

    [Fact]
    public async Task ApproveRankDown_InvalidJson_ReturnsFailCheckStrJson()
    {
        var (db, controller, conn) = await CreateContextAsync();
        using (conn)
        {
            var res = await controller.ApproveRankDown(new Crd_CardRankApproveRankDownReq { strJson = "{invalid_json" });
            var resp = ToResp(res);
            Assert.False(resp.Success);
            Assert.Equal("ErridnLoyalty.Crd_CardRank_ApproveRankDown", resp.ErrorCode);
            Assert.NotNull(resp.Params);
            Assert.True(resp.Params.ContainsKey("Check.strJson"));
        }
    }

    [Fact]
    public async Task ApproveRankDown_CardNotFound_ReturnsFailCardNotFound()
    {
        var (db, controller, conn) = await CreateContextAsync();
        using (conn)
        {
            var req = new Crd_CardRankApproveRankDownReq
            {
                strJson = "{\"Lst_Crd_Card\": [{\"CardNo\": \"CARD_NONEXISTENT\"}]}"
            };
            var res = await controller.ApproveRankDown(req);
            var resp = ToResp(res);
            Assert.False(resp.Success);
            Assert.Equal("ErridnLoyalty.Crd_Card_CheckDB_CardNotFound", resp.ErrorCode);
            Assert.NotNull(resp.Params);
            Assert.Equal("CARD_NONEXISTENT", resp.Params["Check.strCardNo"]?.ToString());
        }
    }

    [Fact]
    public async Task ApproveRankDown_CardNotExpired_ReturnsFailInvalidIEffDateEndBeforeSysDate()
    {
        var (db, controller, conn) = await CreateContextAsync();
        using (conn)
        {
            // Thẻ HV0000001-2026 có EffDateEnd = 2026-12-31 (chưa hết hạn kỳ xét hạng)
            var req = new Crd_CardRankApproveRankDownReq
            {
                strJson = "{\"Lst_Crd_Card\": [{\"CardNo\": \"HV0000001-2026\", \"CardTypeUseExpect\": \"G\"}]}"
            };
            var res = await controller.ApproveRankDown(req);
            var resp = ToResp(res);
            Assert.False(resp.Success);
            Assert.Equal("ErridnLoyalty.Crd_Card_RankDown_Input_InvalidIEffDateEndBeforeSysDate", resp.ErrorCode);
            Assert.NotNull(resp.Params);
            Assert.Equal("HV0000001-2026", resp.Params["Check.strCardNo"]?.ToString());
        }
    }

    [Fact]
    public async Task ApproveRankDown_SameRank_ReturnsFailInvalidRankCardTypeUse()
    {
        var (db, controller, conn) = await CreateContextAsync();
        using (conn)
        {
            // CARD_DOWN_01 có CardTypeUse = "GOLD"
            var req = new Crd_CardRankApproveRankDownReq
            {
                strJson = "{\"Lst_Crd_Card\": [{\"CardNo\": \"CARD_DOWN_01\", \"CardTypeUseExpect\": \"GOLD\"}]}"
            };
            var res = await controller.ApproveRankDown(req);
            var resp = ToResp(res);
            Assert.False(resp.Success);
            Assert.Equal("ErridnLoyalty.Crd_CardRankPolicy_Perform_InvalidRankCardTypeUse", resp.ErrorCode);
            Assert.NotNull(resp.Params);
            Assert.Equal("CARD_DOWN_01", resp.Params["Check.strCardNo"]?.ToString());
        }
    }

    [Fact]
    public async Task ApproveRankDown_Success_CancelsOldCard_ApprovesNewCard_UpdatesMember_CreatesRankRecords()
    {
        var (db, controller, conn) = await CreateContextAsync();
        using (conn)
        {
            // CARD_DOWN_01 (GOLD) hạ xuống SILVER
            var req = new Crd_CardRankApproveRankDownReq
            {
                strJson = "{\"Lst_Crd_Card\": [{\"CardNo\": \"CARD_DOWN_01\", \"CardTypeUseExpect\": \"SILVER\"}]}"
            };
            var res = await controller.ApproveRankDown(req);
            var resp = ToResp(res);
            Assert.True(resp.Success);
            Assert.Null(resp.Data);

            // Kiểm tra thẻ cũ đã bị CANCEL
            var oldCard = await db.CrdCards.AsNoTracking().FirstOrDefaultAsync(c => c.CardNo == "CARD_DOWN_01");
            Assert.NotNull(oldCard);
            Assert.Equal("CANCEL", oldCard.CardStatus);
            Assert.NotNull(oldCard.CancelDTimeUTC);

            // Kiểm tra thẻ mới đã được tạo với APPROVE, CardSourceCode = DOWN
            var newCard = await db.CrdCards.AsNoTracking().FirstOrDefaultAsync(c => c.CardNoPrev == "CARD_DOWN_01");
            Assert.NotNull(newCard);
            Assert.Equal("APPROVE", newCard.CardStatus);
            Assert.Equal("DOWN", newCard.CardSourceCode);
            Assert.Equal("SILVER", newCard.CardTypeUse);
            Assert.Equal("VS0580MBDOWN0102", newCard.CardNo); // DLCode(5) + "0" + MemberNo + Seq(02 vì thẻ cũ có đuôi 01)
            Assert.NotNull(newCard.EffDateStart);
            Assert.NotNull(newCard.EffDateEnd);

            // Kiểm tra hội viên MBDOWN01 đã được cập nhật hạng và reset điểm tích luỹ
            var member = await db.CrdMembers.AsNoTracking().FirstOrDefaultAsync(m => m.MemberNo == "MBDOWN01");
            Assert.NotNull(member);
            Assert.Equal("SILVER", member.CardTypeUse);
            Assert.Equal(0, member.PointCardRank);
            Assert.Equal(0, member.PointAvail);

            // Kiểm tra Crd_CardRank có bản ghi
            var rankRec = await db.Set<Crd_CardRank>().AsNoTracking().FirstOrDefaultAsync(r => r.CardSourceCode == "DOWN");
            Assert.NotNull(rankRec);
            Assert.Equal("Crd_Card_RankDown", rankRec.FunctionName);
            Assert.Contains("CARDRANKNO", rankRec.CardRankNo);

            // Kiểm tra Crd_CardBefore và Crd_CardAfter
            var cardBefore = await db.Set<Crd_CardBefore>().AsNoTracking().FirstOrDefaultAsync(b => b.CardRankNo == rankRec.CardRankNo);
            Assert.NotNull(cardBefore);
            Assert.Equal("CARD_DOWN_01", cardBefore.CardNo);
            Assert.Equal("GOLD", cardBefore.CardTypeUse);

            var cardAfter = await db.Set<Crd_CardAfter>().AsNoTracking().FirstOrDefaultAsync(a => a.CardRankNo == rankRec.CardRankNo);
            Assert.NotNull(cardAfter);
            Assert.Equal(newCard.CardNo, cardAfter.CardNo);
            Assert.Equal("SILVER", cardAfter.CardTypeUse);
        }
    }
}
