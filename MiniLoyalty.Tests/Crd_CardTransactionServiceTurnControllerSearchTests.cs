using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MiniLoyalty.Controllers.Port;
using MiniLoyalty.Data;
using MiniLoyalty.Models;
using MiniLoyalty.Models.Port;
using Xunit;
using Xunit.Abstractions;

namespace MiniLoyalty.Tests;

public class Crd_CardTransactionServiceTurnControllerSearchTests
{
    private readonly ITestOutputHelper _output;

    public Crd_CardTransactionServiceTurnControllerSearchTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private class ApiResp
    {
        public bool Success { get; set; }
        public string? ErrorCode { get; set; }
        public Dictionary<string, JsonElement>? Params { get; set; }
        public JsonElement? Data { get; set; }
    }

    private static async Task<(AppDbContext db, Crd_CardTransactionServiceTurnControllerPort controller, SqliteConnection conn)> CreateContextAsync()
    {
        var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        var opt = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(conn)
            .Options;
        var db = new AppDbContext(opt, new TenantContext { OrgId = TenantContext.DefaultOrgId });
        db.Database.EnsureCreated();
        await Seeder.SeedAsync(db);

        var controller = new Crd_CardTransactionServiceTurnControllerPort(db)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        return (db, controller, conn);
    }

    private static ApiResp ToResp(IActionResult actionResult)
    {
        if (actionResult is ObjectResult obj && obj.Value != null)
        {
            var json = JsonSerializer.Serialize(obj.Value);
            return JsonSerializer.Deserialize<ApiResp>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                   ?? new ApiResp { Success = false };
        }
        return new ApiResp { Success = false };
    }

    [Fact]
    public async Task Search_Ca1_NoFilter_ReturnsAllEligibleRecords()
    {
        var (db, controller, conn) = await CreateContextAsync();
        using (conn)
        {
            var res = await controller.Search(new Crd_CardTransactionServiceTurnSearchReq());
            var resp = ToResp(res);

            Assert.True(resp.Success);
            Assert.NotNull(resp.Data);

            var dataElem = resp.Data.Value;
            Assert.True(dataElem.TryGetProperty("Lst_Crd_CardTransactionServiceTurn", out var listElem));
            Assert.Equal(JsonValueKind.Array, listElem.ValueKind);

            var items = JsonSerializer.Deserialize<List<Crd_CardTransactionServiceTurn>>(listElem.GetRawText(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.NotNull(items);
            // 5 dòng: RO_ST_001 (1 dòng), RO_ST_002 (1 dòng), RO_ST_003 (1 dòng), RO_ST_004 (2 dòng do nhân dòng POINTUSE)
            // RO_ST_005 bị loại (DL_UNKNOWN không có trong Mst_DealerCP)
            // RO_ST_006 bị loại (thời gian trước 2023-06-30 17:00:00 UTC)
            Assert.Equal(5, items.Count);

            // Kiểm tra STT đánh số liên tục từ 1..N và CreateDate được gán định dạng yyyy-MM-dd
            for (int i = 0; i < items.Count; i++)
            {
                Assert.Equal(i + 1, items[i].STT);
                Assert.False(string.IsNullOrWhiteSpace(items[i].CreateDate));
            }
        }
    }

    [Fact]
    public async Task Search_Ca2_FilterByDLCode_And_DateRange()
    {
        var (db, controller, conn) = await CreateContextAsync();
        using (conn)
        {
            // Lọc theo DLCode = "DL_SG"
            var resSG = await controller.Search(new Crd_CardTransactionServiceTurnSearchReq
            {
                DLCode = "DL_SG"
            });
            var respSG = ToResp(resSG);
            Assert.True(respSG.Success);
            Assert.NotNull(respSG.Data);

            var listElemSG = respSG.Data.Value.GetProperty("Lst_Crd_CardTransactionServiceTurn");
            var itemsSG = JsonSerializer.Deserialize<List<Crd_CardTransactionServiceTurn>>(listElemSG.GetRawText(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.NotNull(itemsSG);
            Assert.Single(itemsSG);
            Assert.Equal("DL_SG", itemsSG[0].DLCode);
            Assert.Equal("Đại lý Sài Gòn", itemsSG[0].DLCPName);

            // Lọc theo DLCode = "DL_HN" trong khoảng thời gian tháng 3/2026
            var resHNMarch = await controller.Search(new Crd_CardTransactionServiceTurnSearchReq
            {
                DLCode = "DL_HN",
                CreateDTimeUTCFrom = "2026-03-01",
                CreateDTimeUTCTo = "2026-03-31"
            });
            var respHNMarch = ToResp(resHNMarch);
            Assert.True(respHNMarch.Success);
            Assert.NotNull(respHNMarch.Data);

            var listElemHN = respHNMarch.Data.Value.GetProperty("Lst_Crd_CardTransactionServiceTurn");
            var itemsHN = JsonSerializer.Deserialize<List<Crd_CardTransactionServiceTurn>>(listElemHN.GetRawText(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.NotNull(itemsHN);
            Assert.Single(itemsHN);
            Assert.Equal(2001, itemsHN[0].AutoId);
            Assert.Equal("DL_HN", itemsHN[0].DLCode);
            Assert.Equal(50, itemsHN[0].AmountChTotal); // Điểm tiêu từ POINTUSE
            Assert.Equal(1500000, itemsHN[0].Amount);   // Tiền từ ROREPAIR
        }
    }

    [Fact]
    public async Task Search_Ca3_FilterByPointFlags()
    {
        var (db, controller, conn) = await CreateContextAsync();
        using (conn)
        {
            // IsAbleToUsePointNotUse = "1" -> AmountAvailPrevious > 0, Amount > 0, AmountChTotal == 0 (RO_ST_002)
            var resNotUse = await controller.Search(new Crd_CardTransactionServiceTurnSearchReq
            {
                IsAbleToUsePointNotUse = "1"
            });
            var respNotUse = ToResp(resNotUse);
            Assert.True(respNotUse.Success);
            Assert.NotNull(respNotUse.Data);

            var listElem = respNotUse.Data.Value.GetProperty("Lst_Crd_CardTransactionServiceTurn");
            var itemsNotUse = JsonSerializer.Deserialize<List<Crd_CardTransactionServiceTurn>>(listElem.GetRawText(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.NotNull(itemsNotUse);
            Assert.Single(itemsNotUse);
            Assert.Equal(2003, itemsNotUse[0].AutoId);
            Assert.Equal("RO_ST_002", itemsNotUse[0].RefNo);

            // IsNotAbleToUsePoint = "1" -> AmountAvailPrevious == 0 || Amount == 0 (RO_ST_003)
            var resNotAble = await controller.Search(new Crd_CardTransactionServiceTurnSearchReq
            {
                IsNotAbleToUsePoint = "1"
            });
            var respNotAble = ToResp(resNotAble);
            Assert.True(respNotAble.Success);
            Assert.NotNull(respNotAble.Data);

            var listElemNotAble = respNotAble.Data.Value.GetProperty("Lst_Crd_CardTransactionServiceTurn");
            var itemsNotAble = JsonSerializer.Deserialize<List<Crd_CardTransactionServiceTurn>>(listElemNotAble.GetRawText(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.NotNull(itemsNotAble);
            Assert.Single(itemsNotAble);
            Assert.Equal(2004, itemsNotAble[0].AutoId);
            Assert.Equal("RO_ST_003", itemsNotAble[0].RefNo);
        }
    }

    [Fact]
    public async Task Search_Ca4_EmptyResult_ReturnsNullDataList()
    {
        var (db, controller, conn) = await CreateContextAsync();
        using (conn)
        {
            // Lọc mã đại lý không tồn tại -> Lst_Crd_CardTransactionServiceTurn phải là null (gate :96-105)
            var res = await controller.Search(new Crd_CardTransactionServiceTurnSearchReq
            {
                DLCode = "DL_KHONG_TON_TAI"
            });
            var resp = ToResp(res);
            Assert.True(resp.Success);
            Assert.NotNull(resp.Data);

            var listElem = resp.Data.Value.GetProperty("Lst_Crd_CardTransactionServiceTurn");
            Assert.Equal(JsonValueKind.Null, listElem.ValueKind);
        }
    }

    [Fact]
    public async Task Search_Ca5_MultiplePointUse_MultipliesRows()
    {
        var (db, controller, conn) = await CreateContextAsync();
        using (conn)
        {
            // RO_ST_004 có 2 bản ghi POINTUSE (-30 và -20) -> LEFT JOIN nhân 2 dòng
            var res = await controller.Search(new Crd_CardTransactionServiceTurnSearchReq
            {
                DLCode = "DL_HN",
                CreateDTimeUTCFrom = "2026-06-01",
                CreateDTimeUTCTo = "2026-06-30"
            });
            var resp = ToResp(res);
            Assert.True(resp.Success);
            Assert.NotNull(resp.Data);

            var listElem = resp.Data.Value.GetProperty("Lst_Crd_CardTransactionServiceTurn");
            var items = JsonSerializer.Deserialize<List<Crd_CardTransactionServiceTurn>>(listElem.GetRawText(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.NotNull(items);
            Assert.Equal(2, items.Count);
            Assert.All(items, x => Assert.Equal("RO_ST_004", x.RefNo));
            Assert.Contains(items, x => x.AmountChTotal == 30);
            Assert.Contains(items, x => x.AmountChTotal == 20);
        }
    }

    [Fact]
    public async Task Search_Ca6_ExcludedScenarios_DealerNotInMstDealerCP_And_BeforeCutoffDate()
    {
        var (db, controller, conn) = await CreateContextAsync();
        using (conn)
        {
            var res = await controller.Search(new Crd_CardTransactionServiceTurnSearchReq());
            var resp = ToResp(res);
            Assert.True(resp.Success);
            Assert.NotNull(resp.Data);

            var listElem = resp.Data.Value.GetProperty("Lst_Crd_CardTransactionServiceTurn");
            var items = JsonSerializer.Deserialize<List<Crd_CardTransactionServiceTurn>>(listElem.GetRawText(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.NotNull(items);
            // RO_ST_005 (DL_UNKNOWN) không có trong Mst_DealerCP -> bị loại bởi INNER JOIN mdcp
            Assert.DoesNotContain(items, x => x.RefNo == "RO_ST_005");

            // RO_ST_006 (2023-06-30 16:59:59) trước mốc 2023-06-30 17:00:00 -> bị loại bởi bộ lọc thời gian
            Assert.DoesNotContain(items, x => x.RefNo == "RO_ST_006");
        }
    }

    [Fact]
    public async Task Search_Ca7_UserDLCPCode_Authorization()
    {
        var (db, controller, conn) = await CreateContextAsync();
        using (conn)
        {
            // Phân quyền user DLCPCode = "DL_SG" qua header
            controller.ControllerContext.HttpContext.Request.Headers["DLCPCode"] = "DL_SG";

            var res = await controller.Search(new Crd_CardTransactionServiceTurnSearchReq());
            var resp = ToResp(res);
            Assert.True(resp.Success);
            Assert.NotNull(resp.Data);

            var listElem = resp.Data.Value.GetProperty("Lst_Crd_CardTransactionServiceTurn");
            var items = JsonSerializer.Deserialize<List<Crd_CardTransactionServiceTurn>>(listElem.GetRawText(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.NotNull(items);
            Assert.Single(items);
            Assert.Equal("RO_ST_002", items[0].RefNo);
            Assert.Equal("DL_SG", items[0].DLCode);

            // Phân quyền user DLCPCode = "HTV" -> không lọc theo đại lý
            controller.ControllerContext.HttpContext.Request.Headers["DLCPCode"] = "HTV";
            var resHTV = await controller.Search(new Crd_CardTransactionServiceTurnSearchReq());
            var respHTV = ToResp(resHTV);
            Assert.True(respHTV.Success);
            Assert.NotNull(respHTV.Data);

            var listElemHTV = respHTV.Data.Value.GetProperty("Lst_Crd_CardTransactionServiceTurn");
            var itemsHTV = JsonSerializer.Deserialize<List<Crd_CardTransactionServiceTurn>>(listElemHTV.GetRawText(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.NotNull(itemsHTV);
            Assert.Equal(5, itemsHTV.Count);
        }
    }

    [Fact]
    public async Task Verify_LiveServer_Port8517_P2Cases()
    {
        int port = 8517;
        string tempDir = Environment.GetEnvironmentVariable("TEMP") ?? Path.GetTempPath();
        string dbPath = Path.Combine(tempDir, "v017.db");

        if (File.Exists(dbPath))
        {
            try { File.Delete(dbPath); } catch { }
        }

        string baseDir = AppContext.BaseDirectory;
        string rootDir = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", ".."));
        string dllPath = Path.Combine(rootDir, "bin", "Release", "net8.0", "MiniLoyalty.dll");
        if (!File.Exists(dllPath))
        {
            dllPath = Path.Combine(rootDir, "bin", "Debug", "net8.0", "MiniLoyalty.dll");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"\"{dllPath}\"",
            WorkingDirectory = rootDir,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        startInfo.EnvironmentVariables["PORT"] = port.ToString();
        startInfo.EnvironmentVariables["CONNECTION_STRING"] = $"Data Source={dbPath}";

        using var proc = Process.Start(startInfo);
        Assert.NotNull(proc);

        _output.WriteLine($"Started server PID={proc.Id} on port {port} with db {dbPath}");

        using var http = new HttpClient();
        http.Timeout = TimeSpan.FromSeconds(5);

        try
        {
            // Chờ /healthz
            bool healthy = false;
            for (int i = 0; i < 30; i++)
            {
                await Task.Delay(1000);
                try
                {
                    var hRes = await http.GetAsync($"http://127.0.0.1:{port}/healthz");
                    if (hRes.IsSuccessStatusCode)
                    {
                        healthy = true;
                        _output.WriteLine($"Server is healthy after {i + 1}s");
                        break;
                    }
                }
                catch { }
            }
            Assert.True(healthy, "Server failed to respond to /healthz within 30s");

            async Task<(int status, string json)> PostJsonAsync(string path, object body, Dictionary<string, string>? headers = null)
            {
                var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
                var req = new HttpRequestMessage(HttpMethod.Post, $"http://127.0.0.1:{port}{path}")
                {
                    Content = content
                };
                if (headers != null)
                {
                    foreach (var kv in headers)
                    {
                        req.Headers.Add(kv.Key, kv.Value);
                    }
                }
                var res = await http.SendAsync(req);
                var str = await res.Content.ReadAsStringAsync();
                return ((int)res.StatusCode, str);
            }

            // ==========================================
            // Ca 1: Search cơ bản không bộ lọc
            // ==========================================
            var (s1, j1) = await PostJsonAsync("/api/crd_cardtransactionserviceturn/search", new { });
            _output.WriteLine($"[Ca 1] Status={s1}, Body={j1}");
            Assert.Equal(200, s1);
            Assert.Contains("\"Success\":true", j1);
            Assert.Contains("\"Lst_Crd_CardTransactionServiceTurn\":[", j1);
            Assert.Contains("RO_ST_001", j1);
            Assert.Contains("RO_ST_002", j1);
            Assert.Contains("RO_ST_003", j1);
            Assert.Contains("RO_ST_004", j1);

            // ==========================================
            // Ca 2a: Lọc theo DLCode và khoảng thời gian (DL_HN trong tháng 3/2026)
            // ==========================================
            var (s2a, j2a) = await PostJsonAsync("/api/crd_cardtransactionserviceturn/search", new
            {
                DLCode = "DL_HN",
                CreateDTimeUTCFrom = "2026-03-01",
                CreateDTimeUTCTo = "2026-03-31"
            });
            _output.WriteLine($"[Ca 2a] Status={s2a}, Body={j2a}");
            Assert.Equal(200, s2a);
            Assert.Contains("\"Success\":true", j2a);
            Assert.Contains("RO_ST_001", j2a);
            Assert.DoesNotContain("RO_ST_002", j2a);
            Assert.DoesNotContain("RO_ST_003", j2a);

            // ==========================================
            // Ca 2b: Lọc theo DLCode DL_SG
            // ==========================================
            var (s2b, j2b) = await PostJsonAsync("/api/crd_cardtransactionserviceturn/search", new
            {
                DLCode = "DL_SG"
            });
            _output.WriteLine($"[Ca 2b] Status={s2b}, Body={j2b}");
            Assert.Equal(200, s2b);
            Assert.Contains("\"Success\":true", j2b);
            Assert.Contains("RO_ST_002", j2b);
            Assert.Contains("Đại lý Sài Gòn", j2b);

            // ==========================================
            // Ca 2c: Phân quyền đại lý qua Header DLCPCode = "DL_SG"
            // ==========================================
            var (s2c, j2c) = await PostJsonAsync(
                "/api/crd_cardtransactionserviceturn/search",
                new { },
                new Dictionary<string, string> { { "DLCPCode", "DL_SG" } }
            );
            _output.WriteLine($"[Ca 2c] Status={s2c}, Body={j2c}");
            Assert.Equal(200, s2c);
            Assert.Contains("\"Success\":true", j2c);
            Assert.Contains("RO_ST_002", j2c);
            Assert.DoesNotContain("RO_ST_001", j2c);

            // ==========================================
            // Ca 2d: Lọc DLCode không tồn tại -> Lst_Crd_CardTransactionServiceTurn = null
            // ==========================================
            var (s2d, j2d) = await PostJsonAsync("/api/crd_cardtransactionserviceturn/search", new
            {
                DLCode = "DL_NONEXISTENT"
            });
            _output.WriteLine($"[Ca 2d] Status={s2d}, Body={j2d}");
            Assert.Equal(200, s2d);
            Assert.Contains("\"Success\":true", j2d);
            Assert.Contains("\"Lst_Crd_CardTransactionServiceTurn\":null", j2d);
        }
        finally
        {
            try
            {
                if (!proc.HasExited)
                {
                    proc.Kill(entireProcessTree: true);
                    proc.WaitForExit(3000);
                }
            }
            catch { }

            try
            {
                if (File.Exists(dbPath)) File.Delete(dbPath);
            }
            catch { }
        }
    }
}
