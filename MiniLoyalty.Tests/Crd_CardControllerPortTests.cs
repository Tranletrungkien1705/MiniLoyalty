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

public class Crd_CardControllerPortTests
{
    private class ApiResp<T>
    {
        public bool Success { get; set; }
        public PageInfo<T>? Data { get; set; }
    }

    private static async Task<(AppDbContext db, Crd_CardControllerPort controller, SqliteConnection conn)> CreateContextAsync()
    {
        var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        var opt = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(conn)
            .Options;
        var db = new AppDbContext(opt, new TenantContext { OrgId = TenantContext.DefaultOrgId });
        db.Database.EnsureCreated();
        await Seeder.SeedAsync(db);
        var controller = new Crd_CardControllerPort(db);
        return (db, controller, conn);
    }

    private static ApiResp<Crd_CardSearchItem> ToResp(IActionResult res)
    {
        var okRes = Assert.IsType<OkObjectResult>(res);
        var json = JsonSerializer.Serialize(okRes.Value);
        return JsonSerializer.Deserialize<ApiResp<Crd_CardSearchItem>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }

    [Fact]
    public async Task Search_Default_ReturnsPagedResult()
    {
        var (db, controller, conn) = await CreateContextAsync();
        using (conn)
        {
            var res = await controller.Search(new Crd_CardControllerSearchReq());
            var resp = ToResp(res);
            Assert.True(resp.Success);
            Assert.NotNull(resp.Data);
            Assert.True(resp.Data.ItemCount > 0);
            Assert.NotEmpty(resp.Data.DataList);
        }
    }

    [Fact]
    public async Task Search_ByMemberNo_ReturnsMatchingRecord()
    {
        var (db, controller, conn) = await CreateContextAsync();
        using (conn)
        {
            var req = new Crd_CardControllerSearchReq { MemberNo = "MB001" };
            var res = await controller.Search(req);
            var resp = ToResp(res);
            Assert.True(resp.Success);
            Assert.NotNull(resp.Data);
            Assert.Equal(1, resp.Data.ItemCount);
            Assert.Single(resp.Data.DataList);
            var item = resp.Data.DataList[0];
            Assert.Equal("MB001", item.MemberNo);
            Assert.Equal("CD001", item.CardNo);
            Assert.Equal("Nguyễn Văn MB001", item.MemberName);
            Assert.Equal("APPROVE", item.CardStatus);
            Assert.Equal("1", item.cc_FlagShow);
            Assert.Equal("1", item.cm_FlagShow);
            Assert.Equal(50, item.PointExpiry);
            Assert.Equal(5000000, item.AmountRef);
            Assert.Equal(200, item.PointUse);
        }
    }

    [Fact]
    public async Task Search_ByCardStatus_InOperator_FiltersCorrectly()
    {
        var (db, controller, conn) = await CreateContextAsync();
        using (conn)
        {
            var req = new Crd_CardControllerSearchReq { CardStatus = "APPROVE,PENDING" };
            var res = await controller.Search(req);
            var resp = ToResp(res);
            Assert.True(resp.Success);
            Assert.NotNull(resp.Data);
            Assert.True(resp.Data.ItemCount > 0);
            foreach (var item in resp.Data.DataList)
            {
                Assert.Contains(item.CardStatus, new[] { "APPROVE", "PENDING" });
            }
        }
    }

    [Fact]
    public async Task Search_PageSizeOne_ReturnsOneItemPerPageIndex()
    {
        var (db, controller, conn) = await CreateContextAsync();
        using (conn)
        {
            var req = new Crd_CardControllerSearchReq { Ft_PageSize = 1, Ft_PageIndex = 0 };
            var res = await controller.Search(req);
            var resp = ToResp(res);
            Assert.True(resp.Success);
            Assert.NotNull(resp.Data);
            Assert.True(resp.Data.ItemCount > 1);
            Assert.Equal(resp.Data.ItemCount, resp.Data.PageCount);
            Assert.Single(resp.Data.DataList);
        }
    }

    [Fact]
    public async Task Search_PageSizeZero_ReturnsEmptyDataListWithItemCountPreserved()
    {
        var (db, controller, conn) = await CreateContextAsync();
        using (conn)
        {
            var req = new Crd_CardControllerSearchReq { Ft_PageSize = 0 };
            var res = await controller.Search(req);
            var resp = ToResp(res);
            Assert.True(resp.Success);
            Assert.NotNull(resp.Data);
            Assert.True(resp.Data.ItemCount > 0);
            Assert.Equal(0, resp.Data.PageCount);
            Assert.Empty(resp.Data.DataList);
        }
    }

    private class ApiRawResp
    {
        public bool Success { get; set; }
        public object? Data { get; set; }
        public string? ErrorCode { get; set; }
        public Dictionary<string, object?>? Params { get; set; }
    }

    private static ApiRawResp ToRawResp(IActionResult res)
    {
        if (res is ObjectResult obj)
        {
            var json = JsonSerializer.Serialize(obj.Value);
            return JsonSerializer.Deserialize<ApiRawResp>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        }
        var okRes = Assert.IsType<OkObjectResult>(res);
        var jsonOk = JsonSerializer.Serialize(okRes.Value);
        return JsonSerializer.Deserialize<ApiRawResp>(jsonOk, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }

    [Fact]
    public async Task RequestException_EmptyBody_ReturnsFailCheckStrJson()
    {
        var (db, controller, conn) = await CreateContextAsync();
        using (conn)
        {
            var res = await controller.RequestException(null);
            var resp = ToRawResp(res);
            Assert.False(resp.Success);
            Assert.Equal("ErridnLoyalty.Crd_Card_RequestException", resp.ErrorCode);
            Assert.Equal("Check.strJson", resp.Params?.Keys.FirstOrDefault());
        }
    }

    [Fact]
    public async Task RequestException_CardNotFound_ReturnsFailCardNotFound()
    {
        var (db, controller, conn) = await CreateContextAsync();
        using (conn)
        {
            var req = new RQ_Crd_Card
            {
                Crd_Card = new Crd_Card_RequestException_Input { CardNo = "CARD99", CardTypeUse = "VIP" }
            };
            var res = await controller.RequestException(req);
            var resp = ToRawResp(res);
            Assert.False(resp.Success);
            Assert.Equal("ErridnLoyalty.Crd_Card_CheckDB_CardNotFound", resp.ErrorCode);
            Assert.Equal("CARD99", resp.Params?["Check.CardNo"]?.ToString());
        }
    }

    [Fact]
    public async Task RequestException_CardNotApprove_ReturnsFailCardStatusNotMatched()
    {
        var (db, controller, conn) = await CreateContextAsync();
        using (conn)
        {
            var req = new RQ_Crd_Card
            {
                Crd_Card = new Crd_Card_RequestException_Input { CardNo = "CD003", CardTypeUse = "VIP" }
            };
            var res = await controller.RequestException(req);
            var resp = ToRawResp(res);
            Assert.False(resp.Success);
            Assert.Equal("ErridnLoyalty.Crd_Card_CheckDB_CardStatusNotMatched", resp.ErrorCode);
            Assert.Equal("CD003", resp.Params?["Check.CardNo"]?.ToString());
        }
    }

    [Fact]
    public async Task RequestException_MemberNotFound_ReturnsFailMemberNotFound()
    {
        var (db, controller, conn) = await CreateContextAsync();
        using (conn)
        {
            var card = new CrdCard
            {
                CardNo = "CARD_NOMEM",
                MemberNo = "MEM_NONEXIST",
                NetworkID = "1001",
                CardStatus = "APPROVE",
                CardTypeUse = "VIP",
                CardTypeInit = "VIP"
            };
            db.CrdCards.Add(card);
            await db.SaveChangesAsync();

            var req = new RQ_Crd_Card
            {
                Crd_Card = new Crd_Card_RequestException_Input { CardNo = "CARD_NOMEM", CardTypeUse = "VIP" }
            };
            var res = await controller.RequestException(req);
            var resp = ToRawResp(res);
            Assert.False(resp.Success);
            Assert.Equal("ErridnLoyalty.Crd_Member_CheckDB_MemberNotFound", resp.ErrorCode);
            Assert.Equal("MEM_NONEXIST", resp.Params?["Check.MemberNo"]?.ToString());
        }
    }

    [Fact]
    public async Task RequestException_MemberRegisStatusInvalid_ReturnsFailRegisStatusNotMatched()
    {
        var (db, controller, conn) = await CreateContextAsync();
        using (conn)
        {
            var mem = await db.CrdMembers.FirstOrDefaultAsync(m => m.MemberNo == "MEM01");
            Assert.NotNull(mem);
            mem.RegisStatus = "PENDING";
            await db.SaveChangesAsync();

            var req = new RQ_Crd_Card
            {
                Crd_Card = new Crd_Card_RequestException_Input { CardNo = "CARD01", CardTypeUse = "VIP" }
            };
            var res = await controller.RequestException(req);
            var resp = ToRawResp(res);
            Assert.False(resp.Success);
            Assert.Equal("ErridnLoyalty.Crd_Member_CheckDB_RegisStatusNotMatched", resp.ErrorCode);
            Assert.Equal("MEM01", resp.Params?["Check.MemberNo"]?.ToString());
        }
    }

    [Fact]
    public async Task RequestException_MemberStatusInvalid_ReturnsFailMemberStatusNotMatched()
    {
        var (db, controller, conn) = await CreateContextAsync();
        using (conn)
        {
            var mem = await db.CrdMembers.FirstOrDefaultAsync(m => m.MemberNo == "MEM01");
            Assert.NotNull(mem);
            mem.MemberStatus = "PENDING";
            await db.SaveChangesAsync();

            var req = new RQ_Crd_Card
            {
                Crd_Card = new Crd_Card_RequestException_Input { CardNo = "CARD01", CardTypeUse = "VIP" }
            };
            var res = await controller.RequestException(req);
            var resp = ToRawResp(res);
            Assert.False(resp.Success);
            Assert.Equal("ErridnLoyalty.Crd_Member_CheckDB_MemberStatusNotMatched", resp.ErrorCode);
            Assert.Equal("MEM01", resp.Params?["Check.MemberNo"]?.ToString());
        }
    }

    [Fact]
    public async Task RequestException_ExistedPendingCard_ReturnsFailExistedCardNoException()
    {
        var (db, controller, conn) = await CreateContextAsync();
        using (conn)
        {
            db.CrdCards.Add(new CrdCard
            {
                CardNo = "CARD_PENDING_EX",
                CardNoPrev = "CARD01",
                MemberNo = "MEM01",
                NetworkID = "1001",
                CardStatus = "PENDING",
                FlagExceptionally = "1",
                CardTypeUse = "VIP",
                CardTypeInit = "VIP"
            });
            await db.SaveChangesAsync();

            var req = new RQ_Crd_Card
            {
                Crd_Card = new Crd_Card_RequestException_Input { CardNo = "CARD01", CardTypeUse = "VIP" }
            };
            var res = await controller.RequestException(req);
            var resp = ToRawResp(res);
            Assert.False(resp.Success);
            Assert.Equal("ErridnLoyalty.Crd_Card_RequestException_ExistedCardNoException", resp.ErrorCode);
            Assert.Equal("CARD01", resp.Params?["Check.CardNo"]?.ToString());
            Assert.Equal("CARD_PENDING_EX", resp.Params?["Check.CardNo_Exception"]?.ToString());
        }
    }

    [Fact]
    public async Task RequestException_EmptyDealerTable_ReturnsFailDealerTableInvalid()
    {
        var (db, controller, conn) = await CreateContextAsync();
        using (conn)
        {
            var req = new RQ_Crd_Card
            {
                Crd_Card = new Crd_Card_RequestException_Input { CardNo = "CARD01", CardTypeUse = "VIP" },
                Lst_Crd_CardDealerUseException = new List<Crd_CardDealerUseException>()
            };
            var res = await controller.RequestException(req);
            var resp = ToRawResp(res);
            Assert.False(resp.Success);
            Assert.Equal("ErridnLoyalty.Crd_Card_RequestException_CardDealerUseExceptionTblInvalid", resp.ErrorCode);
            Assert.Equal("Crd_CardDealerUseException", resp.Params?["Check.TableName"]?.ToString());
        }
    }

    [Fact]
    public async Task RequestException_DLCodeWrongLength_ReturnsFailInvalidGen6ParamPrefix()
    {
        var (db, controller, conn) = await CreateContextAsync();
        using (conn)
        {
            var mem = await db.CrdMembers.FirstOrDefaultAsync(m => m.MemberNo == "MEM01");
            Assert.NotNull(mem);
            mem.DLCodeRegis = "INVALID_DL";
            await db.SaveChangesAsync();

            var req = new RQ_Crd_Card
            {
                Crd_Card = new Crd_Card_RequestException_Input { CardNo = "CARD01", CardTypeUse = "VIP" },
                Lst_Crd_CardDealerUseException = new List<Crd_CardDealerUseException>
                {
                    new() { DealerCode = "VS058" }
                }
            };
            var res = await controller.RequestException(req);
            var resp = ToRawResp(res);
            Assert.False(resp.Success);
            Assert.Equal("ErridnLoyalty.GenCardNo_InvalidGen6ParamPrefixOfCard", resp.ErrorCode);
            Assert.Equal("INVALID_DL", resp.Params?["Check.DLCode"]?.ToString());
        }
    }

    [Fact]
    public async Task RequestException_Success_CreatesPendingCard_InsertsDealer_UpdatesMember()
    {
        var (db, controller, conn) = await CreateContextAsync();
        using (conn)
        {
            var req = new RQ_Crd_Card
            {
                Crd_Card = new Crd_Card_RequestException_Input
                {
                    CardNo = "CARD01",
                    CardTypeUse = "VIP",
                    DLCodeExceptionally = "VS058",
                    Remark = "Xin đặc cách VIP"
                },
                Lst_Crd_CardDealerUseException = new List<Crd_CardDealerUseException>
                {
                    new() { DealerCode = "VS058" }
                }
            };
            var res = await controller.RequestException(req);
            var resp = ToRawResp(res);
            Assert.True(resp.Success);
            Assert.Null(resp.Data);

            // Kiểm tra thẻ PENDING mới đã được tạo
            var newCard = await db.CrdCards.AsNoTracking().FirstOrDefaultAsync(c => c.CardNoPrev == "CARD01");
            Assert.NotNull(newCard);
            Assert.Equal("PENDING", newCard.CardStatus);
            Assert.Equal("UP", newCard.CardSourceCode);
            Assert.Equal("1", newCard.FlagExceptionally);
            Assert.Equal("VS058", newCard.DLCodeExceptionally);
            Assert.Equal("Xin đặc cách VIP", newCard.Remark);
            Assert.Equal("VS0580MEM0102", newCard.CardNo);

            // Kiểm tra Crd_CardDealerUseException
            var dealerRec = await db.Set<Crd_CardDealerUseException>().AsNoTracking()
                .FirstOrDefaultAsync(d => d.CardNo == newCard.CardNo && d.DealerCode == "VS058");
            Assert.NotNull(dealerRec);

            // Kiểm tra Crd_Member được cập nhật PENDING và UP
            var member = await db.CrdMembers.AsNoTracking().FirstOrDefaultAsync(m => m.MemberNo == "MEM01");
            Assert.NotNull(member);
            Assert.Equal("PENDING", member.CardStatus);
            Assert.Equal("UP", member.CardSourceCode);
        }
    }
}

