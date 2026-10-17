using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MiniLoyalty.Controllers.Port;
using MiniLoyalty.Data;
using MiniLoyalty.Models;
using MiniLoyalty.Models.Port;
using Xunit;

namespace MiniLoyalty.Tests;

public class Crd_MemberControllerFinishTests
{
    private class ApiResp
    {
        public bool Success { get; set; }
        public string? ErrorCode { get; set; }
        public Dictionary<string, JsonElement>? Params { get; set; }
    }

    private static async Task<(AppDbContext db, Crd_MemberControllerPort controller, SqliteConnection conn)> CreateContextAsync()
    {
        var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        var opt = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(conn)
            .Options;
        var db = new AppDbContext(opt, new TenantContext { OrgId = TenantContext.DefaultOrgId });
        db.Database.EnsureCreated();
        await Seeder.SeedAsync(db);
        var controller = new Crd_MemberControllerPort(db);
        return (db, controller, conn);
    }

    private static ApiResp ToResp(IActionResult res)
    {
        if (res is OkObjectResult ok)
        {
            var json = JsonSerializer.Serialize(ok.Value);
            return JsonSerializer.Deserialize<ApiResp>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        }
        if (res is BadRequestObjectResult bad)
        {
            var json = JsonSerializer.Serialize(bad.Value);
            return JsonSerializer.Deserialize<ApiResp>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        }
        throw new Exception($"Unexpected action result: {res?.GetType().Name}");
    }

    [Fact]
    public async Task Finish_Ca1_BodyEmpty_ReturnsCheckStrJsonError()
    {
        var (db, controller, conn) = await CreateContextAsync();
        using (conn)
        {
            var res = await controller.Finish(new Crd_MemberControllerPort.Crd_MemberStatusReq());
            var resp = ToResp(res);
            Assert.False(resp.Success);
            Assert.Equal("ErridnLoyalty.Crd_Member_Finish", resp.ErrorCode);
            Assert.True(resp.Params?.ContainsKey("Check.strJson"));
        }
    }

    [Fact]
    public async Task Finish_Ca2_MemberNotFound_ReturnsError()
    {
        var (db, controller, conn) = await CreateContextAsync();
        using (conn)
        {
            var res = await controller.Finish(new Crd_MemberControllerPort.Crd_MemberStatusReq
            {
                Crd_Member = new Crd_MemberControllerPort.Crd_MemberStatusInput { MemberNo = "MB_NOT_FOUND" }
            });
            var resp = ToResp(res);
            Assert.False(resp.Success);
            Assert.Equal("ErridnLoyalty.Crd_Member_CheckDB_MemberNotFound", resp.ErrorCode);
            Assert.Equal("MB_NOT_FOUND", resp.Params?["Check.MemberNo"].GetString());
        }
    }

    [Fact]
    public async Task Finish_Ca3_MemberStatusNotMatched_ReturnsError()
    {
        var (db, controller, conn) = await CreateContextAsync();
        using (conn)
        {
            var res = await controller.Finish(new Crd_MemberControllerPort.Crd_MemberStatusReq
            {
                Crd_Member = new Crd_MemberControllerPort.Crd_MemberStatusInput { MemberNo = "MB_FIN_ERR_STATUS" }
            });
            var resp = ToResp(res);
            Assert.False(resp.Success);
            Assert.Equal("ErridnLoyalty.Crd_Member_CheckDB_MemberStatusNotMatched", resp.ErrorCode);
            Assert.Equal("APPROVE", resp.Params?["DB.MemberStatus"].GetString());
            Assert.Equal("PENDING", resp.Params?["Check.strMemberStatusListToCheck"].GetString());
        }
    }

    [Fact]
    public async Task Finish_Ca4_RegisStatusNotMatched_Pending_ReturnsError()
    {
        var (db, controller, conn) = await CreateContextAsync();
        using (conn)
        {
            var res = await controller.Finish(new Crd_MemberControllerPort.Crd_MemberStatusReq
            {
                Crd_Member = new Crd_MemberControllerPort.Crd_MemberStatusInput { MemberNo = "MB_FIN_ERR_REGIS_P" }
            });
            var resp = ToResp(res);
            Assert.False(resp.Success);
            Assert.Equal("ErridnLoyalty.Crd_Member_CheckDB_RegisStatusNotMatched", resp.ErrorCode);
            Assert.Equal("APPROVE1", resp.Params?["DB.RegisStatus"].GetString());
            Assert.Equal("PENDING", resp.Params?["Check.strRegisStatusListToCheck"].GetString());
        }
    }

    [Fact]
    public async Task Finish_Ca5_RegisStatusNotMatched_Approve2_ReturnsError()
    {
        var (db, controller, conn) = await CreateContextAsync();
        using (conn)
        {
            var res = await controller.Finish(new Crd_MemberControllerPort.Crd_MemberStatusReq
            {
                Crd_Member = new Crd_MemberControllerPort.Crd_MemberStatusInput { MemberNo = "MB_FIN_ERR_REGIS_A2" }
            });
            var resp = ToResp(res);
            Assert.False(resp.Success);
            Assert.Equal("ErridnLoyalty.Crd_Member_CheckDB_RegisStatusNotMatched", resp.ErrorCode);
            Assert.Equal("PENDING", resp.Params?["DB.RegisStatus"].GetString());
            Assert.Equal("APPROVE2", resp.Params?["Check.strRegisStatusListToCheck"].GetString());
        }
    }

    [Fact]
    public async Task Finish_Ca6_MultiCardActive_ReturnsError()
    {
        var (db, controller, conn) = await CreateContextAsync();
        using (conn)
        {
            var res = await controller.Finish(new Crd_MemberControllerPort.Crd_MemberStatusReq
            {
                Crd_Member = new Crd_MemberControllerPort.Crd_MemberStatusInput { MemberNo = "MB_FIN_ERR_MULTICARD" }
            });
            var resp = ToResp(res);
            Assert.False(resp.Success);
            Assert.Equal("ErridnLoyalty.Crd_Member_FinishX_CardNoInvalid", resp.ErrorCode);
            Assert.Equal("MB_FIN_ERR_MULTICARD", resp.Params?["Check.MemberNo"].GetString());
            Assert.Equal("Only 1 Card of Member Active", resp.Params?["Expect"].GetString());
        }
    }

    [Fact]
    public async Task Finish_Ca7_RoundTrip_OK_StandardMember()
    {
        var (db, controller, conn) = await CreateContextAsync();
        using (conn)
        {
            var res = await controller.Finish(new Crd_MemberControllerPort.Crd_MemberStatusReq
            {
                Crd_Member = new Crd_MemberControllerPort.Crd_MemberStatusInput { MemberNo = "MB_FIN_OK_01", Remark = "Ghi chu moi khong duoc luu" }
            });
            var resp = ToResp(res);
            Assert.True(resp.Success);

            // DB Verification: MemberStatus -> APPROVE, RegisStatus -> FINISH (Card.cs:17493-17494)
            var member = await db.CrdMembers.AsNoTracking().FirstOrDefaultAsync(m => m.MemberNo == "MB_FIN_OK_01");
            Assert.NotNull(member);
            Assert.Equal("APPROVE", member.MemberStatus);
            Assert.Equal("FINISH", member.RegisStatus);
            // Source bug preservation: Remark is NOT overwritten
            Assert.Equal("Ghi chu ban dau", member.Remark);

            // Card updated to APPROVE
            var card = await db.CrdCards.AsNoTracking().FirstOrDefaultAsync(c => c.MemberNo == "MB_FIN_OK_01");
            Assert.NotNull(card);
            Assert.Equal("APPROVE", card.CardStatus);

            // Map_QueryDealer_Member created
            var dealerMap = await db.Set<Map_QueryDealer_Member>().AsNoTracking().FirstOrDefaultAsync(m => m.MemberNo == "MB_FIN_OK_01");
            Assert.NotNull(dealerMap);
            Assert.Equal("DL01", dealerMap.DLCPCode);
            Assert.Equal("1", dealerMap.FlagActive);
        }
    }

    [Fact]
    public async Task Finish_Ca8_RoundTrip_OK_ExcepMember()
    {
        var (db, controller, conn) = await CreateContextAsync();
        using (conn)
        {
            var res = await controller.Finish(new Crd_MemberControllerPort.Crd_MemberStatusReq
            {
                Crd_Member = new Crd_MemberControllerPort.Crd_MemberStatusInput { MemberNo = "MB_FIN_OK_EXCEP", Remark = "Dac cach hoan thanh" }
            });
            var resp = ToResp(res);
            Assert.True(resp.Success);

            var member = await db.CrdMembers.AsNoTracking().FirstOrDefaultAsync(m => m.MemberNo == "MB_FIN_OK_EXCEP");
            Assert.NotNull(member);
            Assert.Equal("APPROVE", member.MemberStatus);
            Assert.Equal("FINISH", member.RegisStatus);

            var card = await db.CrdCards.AsNoTracking().FirstOrDefaultAsync(c => c.MemberNo == "MB_FIN_OK_EXCEP");
            Assert.NotNull(card);
            Assert.Equal("APPROVE", card.CardStatus);
        }
    }

    [Fact]
    public async Task Finish_Ca9_AwardPoints_BuyNewCar()
    {
        var (db, controller, conn) = await CreateContextAsync();
        using (conn)
        {
            var res = await controller.Finish(new Crd_MemberControllerPort.Crd_MemberStatusReq
            {
                Crd_Member = new Crd_MemberControllerPort.Crd_MemberStatusInput { MemberNo = "MB_FIN_BUYCAR" }
            });
            var resp = ToResp(res);
            Assert.True(resp.Success);

            // Check Crd_CardTransaction (DealPointType = SALES)
            var tx = await db.CrdCardTransactions.AsNoTracking().FirstOrDefaultAsync(t => t.MemberNo == "MB_FIN_BUYCAR");
            Assert.NotNull(tx);
            Assert.Equal("SALES", tx.DealPointType);
            Assert.Equal(50000, tx.PointChTotal);
            Assert.Equal(50000, tx.AmountChTotal);

            // Check Member points
            var member = await db.CrdMembers.AsNoTracking().FirstOrDefaultAsync(m => m.MemberNo == "MB_FIN_BUYCAR");
            Assert.NotNull(member);
            Assert.Equal(50000, member.PointTotal);
            Assert.Equal(50000, member.PointAvail);
            Assert.Equal(50000, member.PointEnd);
            Assert.Equal(50000, member.AmountTotal);
            Assert.Equal(50000, member.AmountAvail);
            Assert.Equal(50000, member.AmountEnd);

            // Check Card points
            var card = await db.CrdCards.AsNoTracking().FirstOrDefaultAsync(c => c.MemberNo == "MB_FIN_BUYCAR");
            Assert.NotNull(card);
            Assert.Equal(50000, card.PointTotal);
            Assert.Equal(50000, card.PointAvail);
            Assert.Equal(50000, card.AmountTotal);
            Assert.Equal(50000, card.AmountAvail);
        }
    }

    [Fact]
    public async Task Finish_Ca10_AwardPoints_Intro()
    {
        var (db, controller, conn) = await CreateContextAsync();
        using (conn)
        {
            var res = await controller.Finish(new Crd_MemberControllerPort.Crd_MemberStatusReq
            {
                Crd_Member = new Crd_MemberControllerPort.Crd_MemberStatusInput { MemberNo = "MB_FIN_INTRO" }
            });
            var resp = ToResp(res);
            Assert.True(resp.Success);

            // Points awarded to MB_FIN_INTRODUCER (DealPointType = INTRODUCTION)
            var tx = await db.CrdCardTransactions.AsNoTracking().FirstOrDefaultAsync(t => t.MemberNo == "MB_FIN_INTRODUCER");
            Assert.NotNull(tx);
            Assert.Equal("INTRODUCTION", tx.DealPointType);
            Assert.Equal("CD_FIN_INTRODUCER", tx.CardNo);
            Assert.Equal(20000, tx.PointChTotal);
            Assert.Equal(20000, tx.AmountChTotal);

            var introducer = await db.CrdMembers.AsNoTracking().FirstOrDefaultAsync(m => m.MemberNo == "MB_FIN_INTRODUCER");
            Assert.NotNull(introducer);
            Assert.Equal(20000, introducer.PointTotal);
            Assert.Equal(20000, introducer.PointAvail);
            Assert.Equal(20000, introducer.PointEnd);

            var card = await db.CrdCards.AsNoTracking().FirstOrDefaultAsync(c => c.CardNo == "CD_FIN_INTRODUCER");
            Assert.NotNull(card);
            Assert.Equal(20000, card.PointTotal);
            Assert.Equal(20000, card.PointAvail);
        }
    }

    [Fact]
    public async Task Finish_Ca11_AwardPoints_OpenCard()
    {
        var (db, controller, conn) = await CreateContextAsync();
        using (conn)
        {
            var res = await controller.Finish(new Crd_MemberControllerPort.Crd_MemberStatusReq
            {
                Crd_Member = new Crd_MemberControllerPort.Crd_MemberStatusInput { MemberNo = "MB_FIN_OPENCARD" }
            });
            var resp = ToResp(res);
            Assert.True(resp.Success);

            var tx = await db.CrdCardTransactions.AsNoTracking().FirstOrDefaultAsync(t => t.MemberNo == "MB_FIN_OPENCARD");
            Assert.NotNull(tx);
            Assert.Equal("OPENCARD", tx.DealPointType);
            Assert.Equal("HTV", tx.DLCode);
            Assert.Equal(150000, tx.PointChTotal);
            Assert.Equal(150000, tx.AmountChTotal);

            var member = await db.CrdMembers.AsNoTracking().FirstOrDefaultAsync(m => m.MemberNo == "MB_FIN_OPENCARD");
            Assert.NotNull(member);
            Assert.Equal(150000, member.PointOpenCard);
            Assert.Equal(150000, member.PointTotal);
            Assert.Equal(150000, member.PointAvail);
            Assert.Equal(150000, member.PointEnd);

            var card = await db.CrdCards.AsNoTracking().FirstOrDefaultAsync(c => c.MemberNo == "MB_FIN_OPENCARD");
            Assert.NotNull(card);
            Assert.Equal(150000, card.PointTotal);
            Assert.Equal(150000, card.PointAvail);
        }
    }

    [Fact]
    public async Task Finish_Ca12_AwardPoints_BuyCreta()
    {
        var (db, controller, conn) = await CreateContextAsync();
        using (conn)
        {
            var res = await controller.Finish(new Crd_MemberControllerPort.Crd_MemberStatusReq
            {
                Crd_Member = new Crd_MemberControllerPort.Crd_MemberStatusInput { MemberNo = "MB_FIN_CRETA" }
            });
            var resp = ToResp(res);
            Assert.True(resp.Success);

            var tx = await db.CrdCardTransactions.AsNoTracking().FirstOrDefaultAsync(t => t.MemberNo == "MB_FIN_CRETA");
            Assert.NotNull(tx);
            Assert.Equal("KMBH", tx.DealPointType);
            Assert.Equal("HTV", tx.DLCode);
            Assert.Equal(30000, tx.PointChTotal);
            Assert.Equal(30000, tx.AmountChTotal);

            var member = await db.CrdMembers.AsNoTracking().FirstOrDefaultAsync(m => m.MemberNo == "MB_FIN_CRETA");
            Assert.NotNull(member);
            Assert.Equal(30000, member.PointBuyCreta);
            Assert.Equal(30000, member.PointTotal);
            Assert.Equal(30000, member.PointAvail);
            Assert.Equal(30000, member.PointEnd);

            var card = await db.CrdCards.AsNoTracking().FirstOrDefaultAsync(c => c.MemberNo == "MB_FIN_CRETA");
            Assert.NotNull(card);
            Assert.Equal(30000, card.PointTotal);
            Assert.Equal(30000, card.PointAvail);
        }
    }

    [Fact]
    public async Task Finish_Ca13_AwardPoints_VoucherNewCar()
    {
        var (db, controller, conn) = await CreateContextAsync();
        using (conn)
        {
            var res = await controller.Finish(new Crd_MemberControllerPort.Crd_MemberStatusReq
            {
                Crd_Member = new Crd_MemberControllerPort.Crd_MemberStatusInput { MemberNo = "MB_FIN_VOUCHER" }
            });
            var resp = ToResp(res);
            Assert.True(resp.Success);

            // Crd_MemberVoucherTransaction created (DealPointType = VOUCHERXM)
            var vtx = await db.Set<Crd_MemberVoucherTransaction>().AsNoTracking().FirstOrDefaultAsync(t => t.MemberNo == "MB_FIN_VOUCHER");
            Assert.NotNull(vtx);
            Assert.Equal("VOUCHERXM", vtx.DealPointType);
            Assert.Equal("PVC_FIN_01", vtx.PrmVoucherCode);
            Assert.Equal(1000000, vtx.PointChTotal);
            Assert.Equal(1000000, vtx.PointActualChTotal);

            // Crd_MemberVoucher created
            var mv = await db.Set<Crd_MemberVoucher>().AsNoTracking().FirstOrDefaultAsync(v => v.MemberNo == "MB_FIN_VOUCHER");
            Assert.NotNull(mv);
            Assert.StartsWith("VCXM.", mv.VoucherNo);
            Assert.Equal(1000000, mv.PointVCTotal);
            Assert.Equal(1000000, mv.PointVCRemain);
            Assert.Equal(500000, mv.PointVCLimit);
            Assert.Equal("1", mv.FlagActive);

            // Member points updated
            var member = await db.CrdMembers.AsNoTracking().FirstOrDefaultAsync(m => m.MemberNo == "MB_FIN_VOUCHER");
            Assert.NotNull(member);
            Assert.Equal(1000000, member.PointVoucher);
            Assert.Equal(1000000, member.PointActualVoucher);
        }
    }
}
