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

public class Crd_DealSerROControllerAddTests
{
    private class ApiResp
    {
        public bool Success { get; set; }
        public object? Data { get; set; }
        public string? ErrorCode { get; set; }
        public Dictionary<string, JsonElement>? Params { get; set; }
    }

    private static async Task<(AppDbContext db, Crd_DealSerROControllerPort controller, SqliteConnection conn)> CreateContextAsync()
    {
        var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        var opt = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(conn)
            .Options;
        var db = new AppDbContext(opt, new TenantContext { OrgId = TenantContext.DefaultOrgId });
        db.Database.EnsureCreated();
        await Seeder.SeedAsync(db);
        var controller = new Crd_DealSerROControllerPort(db);
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
    public async Task Add_Ca1_BodyNullOrEmpty_ReturnsCheckStrJsonError()
    {
        var (db, controller, conn) = await CreateContextAsync();
        using (conn)
        {
            var res = await controller.Add(new RQ_Crd_DealSerRO());
            var resp = ToResp(res);
            Assert.False(resp.Success);
            Assert.Equal("ErridnLoyalty.Crd_DealSerRO_Add", resp.ErrorCode);
            Assert.True(resp.Params?.ContainsKey("Check.strJson"));
        }
    }

    [Fact]
    public async Task Add_Ca2_DealSerRONoExist_ReturnsError()
    {
        var (db, controller, conn) = await CreateContextAsync();
        using (conn)
        {
            var res = await controller.Add(new RQ_Crd_DealSerRO
            {
                Crd_DealSerRO = new Crd_DealSerROInput { DealSerRONo = "RO001" }
            });
            var resp = ToResp(res);
            Assert.False(resp.Success);
            Assert.Equal("ErridnLoyalty.Crd_DealSerRO_CheckDB_Crd_DealSerROExist", resp.ErrorCode);
            Assert.Equal("RO001", resp.Params?["Check.DealSerRONo"].GetString());
        }
    }

    [Fact]
    public async Task Add_Ca3_DealerNotFound_ReturnsError()
    {
        var (db, controller, conn) = await CreateContextAsync();
        using (conn)
        {
            var res = await controller.Add(new RQ_Crd_DealSerRO
            {
                Crd_DealSerRO = new Crd_DealSerROInput { DealSerRONo = "RO101", DLCode = "DL_KHONG_CO" }
            });
            var resp = ToResp(res);
            Assert.False(resp.Success);
            Assert.Equal("ErridnLoyalty.Mst_DealerCP_CheckDB_DLNotFound", resp.ErrorCode);
            Assert.Equal("DL_KHONG_CO", resp.Params?["Check.DLCPCode"].GetString());
        }
    }

    [Fact]
    public async Task Add_Ca4_DealerInactive_ReturnsError()
    {
        var (db, controller, conn) = await CreateContextAsync();
        using (conn)
        {
            var res = await controller.Add(new RQ_Crd_DealSerRO
            {
                Crd_DealSerRO = new Crd_DealSerROInput { DealSerRONo = "RO101", DLCode = "DL02" }
            });
            var resp = ToResp(res);
            Assert.False(resp.Success);
            Assert.Equal("ErridnLoyalty.Mst_DealerCP_CheckDB_FlagActiveNotMatched", resp.ErrorCode);
            Assert.Equal("DL02", resp.Params?["Check.DLCPCode"].GetString());
            Assert.Equal("0", resp.Params?["DB.FlagActive"].GetString());
        }
    }

    [Fact]
    public async Task Add_Ca5_MemberNotFound_ReturnsError()
    {
        var (db, controller, conn) = await CreateContextAsync();
        using (conn)
        {
            var res = await controller.Add(new RQ_Crd_DealSerRO
            {
                Crd_DealSerRO = new Crd_DealSerROInput { DealSerRONo = "RO101", DLCode = "DL01", MemberNo = "MEM_KHONG_CO" }
            });
            var resp = ToResp(res);
            Assert.False(resp.Success);
            Assert.Equal("ErridnLoyalty.Crd_Member_CheckDB_MemberNotFound", resp.ErrorCode);
            Assert.Equal("MEM_KHONG_CO", resp.Params?["Check.MemberNo"].GetString());
        }
    }

    [Fact]
    public async Task Add_Ca6_PlateNoOrVINNotMatchMember_ReturnsError()
    {
        var (db, controller, conn) = await CreateContextAsync();
        using (conn)
        {
            var res = await controller.Add(new RQ_Crd_DealSerRO
            {
                Crd_DealSerRO = new Crd_DealSerROInput
                {
                    DealSerRONo = "RO101",
                    DLCode = "DL01",
                    MemberNo = "MEM01",
                    PlateNo = "30A-SAI",
                    VIN = "VIN000000001"
                }
            });
            var resp = ToResp(res);
            Assert.False(resp.Success);
            Assert.Equal("ErridnLoyalty.Crd_DealSerRO_Add_PlateNoOrVINNotMatchMember", resp.ErrorCode);
            Assert.Equal("MEM01", resp.Params?["Check.MemberNo"].GetString());
            Assert.Equal("30A-SAI", resp.Params?["Check.PlateNo_Input"].GetString());
        }
    }

    [Fact]
    public async Task Add_Ca7_CardNotFound_ReturnsError()
    {
        var (db, controller, conn) = await CreateContextAsync();
        using (conn)
        {
            var res = await controller.Add(new RQ_Crd_DealSerRO
            {
                Crd_DealSerRO = new Crd_DealSerROInput
                {
                    DealSerRONo = "RO101",
                    DLCode = "DL01",
                    MemberNo = "MEM01",
                    PlateNo = "30A-99999",
                    VIN = "VIN000000001",
                    CardNo = "CARD_KHONG_CO"
                }
            });
            var resp = ToResp(res);
            Assert.False(resp.Success);
            Assert.Equal("ErridnLoyalty.Crd_Card_CheckDB_CardNotFound", resp.ErrorCode);
            Assert.Equal("CARD_KHONG_CO", resp.Params?["Check.CardNo"].GetString());
        }
    }

    [Fact]
    public async Task Add_Ca8_InvalidAmountUse_ReturnsError()
    {
        var (db, controller, conn) = await CreateContextAsync();
        using (conn)
        {
            var res = await controller.Add(new RQ_Crd_DealSerRO
            {
                Crd_DealSerRO = new Crd_DealSerROInput
                {
                    DealSerRONo = "RO101",
                    DLCode = "DL01",
                    MemberNo = "MEM01",
                    PlateNo = "30A-99999",
                    VIN = "VIN000000001",
                    CardNo = "CARD01",
                    AmountUse = 999000000
                }
            });
            var resp = ToResp(res);
            Assert.False(resp.Success);
            Assert.Equal("ErridnLoyalty.Crd_DealSerRO_Add_Input_InvalidAmountUse", resp.ErrorCode);
            Assert.Equal(999000000, resp.Params?["Check.AmountUse"].GetDouble());
        }
    }

    [Fact]
    public async Task Add_Ca9_ExpenseTypeNotFound_ReturnsError()
    {
        var (db, controller, conn) = await CreateContextAsync();
        using (conn)
        {
            var res = await controller.Add(new RQ_Crd_DealSerRO
            {
                Crd_DealSerRO = new Crd_DealSerROInput
                {
                    DealSerRONo = "RO101",
                    DLCode = "DL01",
                    MemberNo = "MEM01",
                    PlateNo = "30A-99999",
                    VIN = "VIN000000001",
                    CardNo = "CARD01",
                    AmountUse = 0
                },
                Lst_Crd_DealSerRODtl = new List<Crd_DealSerRODtlInput>
                {
                    new Crd_DealSerRODtlInput { ExpenseType = "EX_KHONG_CO", Amount = 1000000, FlagPoint = "1" }
                }
            });
            var resp = ToResp(res);
            Assert.False(resp.Success);
            Assert.Equal("ErridnLoyalty.Mst_ExpenseType_CheckDB_ExpenseTypeNotFound", resp.ErrorCode);
            Assert.Equal("EX_KHONG_CO", resp.Params?["Check.ExpenseType"].GetString());
        }
    }

    [Fact]
    public async Task Add_Ca10_Success_AndRoundtrip_AndCardTransactionEngineVerification()
    {
        var (db, controller, conn) = await CreateContextAsync();
        using (conn)
        {
            var res = await controller.Add(new RQ_Crd_DealSerRO
            {
                Crd_DealSerRO = new Crd_DealSerROInput
                {
                    DealSerRONo = "RO101",
                    DLCode = "DL01",
                    MemberNo = "MEM01",
                    PlateNo = "30A-99999",
                    VIN = "VIN000000001",
                    CardNo = "CARD01",
                    DealPointType = "SERVICE",
                    TotalAmountRO = 2000000,
                    AmountRO = 2000000,
                    AmountUse = 0,
                    PointTotal = 200,
                    FlagOnlyPoint = "0",
                    Remark = "Bao gia bao duong xe dinh ky"
                },
                Lst_Crd_DealSerRODtl = new List<Crd_DealSerRODtlInput>
                {
                    new Crd_DealSerRODtlInput { ExpenseType = "EX01", Amount = 2000000, FlagPoint = "1" }
                }
            });

            var resp = ToResp(res);
            Assert.True(resp.Success);
            Assert.Null(resp.Data);

            // Kiểm tra Round-trip qua Search
            var searchRes = await controller.Search(new Crd_DealSerROSearchReq { MemberNo = "MEM01" });
            Assert.IsType<OkObjectResult>(searchRes);
            var searchJson = JsonSerializer.Serialize(((OkObjectResult)searchRes).Value);
            Assert.Contains("RO101", searchJson);
            Assert.Contains("PENDING", searchJson);
            Assert.Contains("BAO GIA BAO DUONG XE DINH KY", searchJson);

            // Kiểm tra Crd_CardTransaction sinh bởi engine thật (không mô phỏng)
            var txList = await db.CrdCardTransactions
                .Where(t => t.RefNo == "RO101")
                .ToListAsync();

            Assert.NotEmpty(txList);
            // 3 giao dịch phát sinh từ chi phí EX01: POINTINCREASE (xét hạng), CONSUMPTION (tích điểm), SERVICETURN (lượt DV)
            Assert.Contains(txList, t => t.DealPointType == "POINTINCREASE");
            Assert.Contains(txList, t => t.DealPointType == "CONSUMPTION");
            Assert.Contains(txList, t => t.DealPointType == "SERVICETURN");

            var txPointIncrease = txList.First(t => t.DealPointType == "POINTINCREASE");
            Assert.Equal(1.0, txPointIncrease.PointChRankTotal);
            Assert.Equal("UNITPOINTTOMONEY", txPointIncrease.ParamCode);
            Assert.Equal("P01", txPointIncrease.PolicyCode);

            var txConsumption = txList.First(t => t.DealPointType == "CONSUMPTION");
            Assert.Equal(1.0, txConsumption.PointChTotal);
            Assert.Equal(1.0, txConsumption.AmountChTotal);
            Assert.NotNull(txConsumption.PointExpiryDTime);

            var txServiceTurn = txList.First(t => t.DealPointType == "SERVICETURN");
            Assert.Equal(1.0, txServiceTurn.QtyVisitChTotal);

            // Kiểm tra số dư thẻ CARD01 và hội viên MEM01 đã được cập nhật qua PortCardTransactionEngine
            var card = await db.CrdCards.FirstAsync(c => c.CardNo == "CARD01");
            Assert.True((card.PointAvail ?? 0.0) >= 1.0);
            Assert.True((card.QtyVisitAvail ?? 0.0) >= 1.0);

            var member = await db.CrdMembers.FirstAsync(m => m.MemberNo == "MEM01");
            Assert.True((member.PointEnd ?? 0.0) >= 1.0);
            Assert.True((member.QtyVisitAvail ?? 0.0) >= 1.0);
        }
    }
}
