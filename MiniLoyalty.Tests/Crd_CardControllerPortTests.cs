using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MiniLoyalty.Controllers.Port;
using MiniLoyalty.Data;
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
}
