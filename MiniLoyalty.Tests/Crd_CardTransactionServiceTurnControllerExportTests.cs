using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MiniLoyalty.Controllers.Port;
using MiniLoyalty.Data;
using MiniLoyalty.Models;
using MiniLoyalty.Models.Port;
using Microsoft.Extensions.FileProviders;
using Xunit;
using Xunit.Abstractions;

namespace MiniLoyalty.Tests;

public class Crd_CardTransactionServiceTurnControllerExportTests
{
    private readonly ITestOutputHelper _output;

    public Crd_CardTransactionServiceTurnControllerExportTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = "";
        public IFileProvider WebRootFileProvider { get; set; } = null!;
        public string ApplicationName { get; set; } = "MiniLoyalty";
        public IFileProvider ContentRootFileProvider { get; set; } = null!;
        public string ContentRootPath { get; set; } = "";
        public string EnvironmentName { get; set; } = "Development";
    }

    private class ApiResp
    {
        public bool Success { get; set; }
        public string? ErrorCode { get; set; }
        public Dictionary<string, JsonElement>? Params { get; set; }
        public JsonElement? Data { get; set; }
    }

    private static async Task<(AppDbContext db, Crd_CardTransactionServiceTurnControllerPort controller, SqliteConnection conn, string tempDir)> CreateContextAsync()
    {
        var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        var opt = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(conn)
            .Options;
        var db = new AppDbContext(opt, new TenantContext { OrgId = TenantContext.DefaultOrgId });
        db.Database.EnsureCreated();
        await Seeder.SeedAsync(db);

        var tempDir = Path.Combine(Path.GetTempPath(), "ExportTest_" + Guid.NewGuid().ToString("N"));
        var wwwroot = Path.Combine(tempDir, "wwwroot");
        Directory.CreateDirectory(wwwroot);

        var env = new TestWebHostEnvironment
        {
            WebRootPath = wwwroot,
            ContentRootPath = tempDir
        };

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "http";
        httpContext.Request.Host = new HostString("127.0.0.1:8518");

        var controller = new Crd_CardTransactionServiceTurnControllerPort(db, env)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            }
        };

        return (db, controller, conn, tempDir);
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
    public async Task Export_Ca1_NoFilter_ReturnsExcelUrl_WithCorrectData()
    {
        var (db, controller, conn, tempDir) = await CreateContextAsync();
        using (conn)
        {
            try
            {
                var res = await controller.Export(new Crd_CardTransactionServiceTurnSearchReq());
                var resp = ToResp(res);

                Assert.True(resp.Success);
                Assert.NotNull(resp.Data);

                var dataElem = resp.Data.Value;
                Assert.True(dataElem.TryGetProperty("Url", out var urlElem));
                var url = urlElem.GetString();
                Assert.NotNull(url);
                Assert.Contains("/TempFiles/", url);
                Assert.EndsWith(".xlsx", url);

                // Kiểm tra file Excel sinh ra trên ổ đĩa
                var relPath = url.Substring(url.IndexOf("/TempFiles/")).TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                var fullPath = Path.Combine(tempDir, "wwwroot", relPath);
                Assert.True(File.Exists(fullPath));

                using var wb = new XLWorkbook(fullPath);
                Assert.True(wb.Worksheets.Contains("Báo cáo lượt dịch vụ"));
                var ws = wb.Worksheet("Báo cáo lượt dịch vụ");

                // 13 cột tiêu đề
                Assert.Equal("STT", ws.Cell(1, 1).GetString());
                Assert.Equal("Mã đại lý", ws.Cell(1, 2).GetString());
                Assert.Equal("Tên đại lý", ws.Cell(1, 3).GetString());
                Assert.Equal("Mã giao dịch", ws.Cell(1, 4).GetString());
                Assert.Equal("Thời gian giao dịch", ws.Cell(1, 5).GetString());
                Assert.Equal("Mã hội viên", ws.Cell(1, 6).GetString());
                Assert.Equal("Mã kỳ xét hạng", ws.Cell(1, 7).GetString());
                Assert.Equal("Hạng sử dụng", ws.Cell(1, 8).GetString());
                Assert.Equal("Số tham chiếu", ws.Cell(1, 9).GetString());
                Assert.Equal("Điểm trước tiêu", ws.Cell(1, 10).GetString());
                Assert.Equal("Điểm tiêu trong lượt dịch vụ", ws.Cell(1, 11).GetString());
                Assert.Equal("Tổng tiền KH", ws.Cell(1, 12).GetString());
                Assert.Equal("Số lượt DV", ws.Cell(1, 13).GetString());

                // 5 dòng dữ liệu: RO_ST_001, RO_ST_002, RO_ST_003, RO_ST_004 (2 dòng)
                var lastRow = ws.LastRowUsed()?.RowNumber() ?? 0;
                Assert.Equal(6, lastRow); // Dòng 1 header + 5 dòng dữ liệu

                // Dòng 1 dữ liệu: RO_ST_001
                Assert.Equal("1", ws.Cell(2, 1).GetString());
                Assert.Equal("DL_HN", ws.Cell(2, 2).GetString());
                Assert.Equal("Đại lý Hà Nội", ws.Cell(2, 3).GetString());
                Assert.Equal("RO_ST_001", ws.Cell(2, 9).GetString());
                Assert.Equal(1500000, ws.Cell(2, 12).GetDouble()); // Đã làm tròn MyRoundVND
            }
            finally
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }
    }

    [Fact]
    public async Task Export_Ca2_FilterByDLCode_ReturnsFilteredExcel()
    {
        var (db, controller, conn, tempDir) = await CreateContextAsync();
        using (conn)
        {
            try
            {
                // Lọc DLCode = "DL_SG"
                var res = await controller.Export(new Crd_CardTransactionServiceTurnSearchReq
                {
                    DLCode = "DL_SG"
                });
                var resp = ToResp(res);

                Assert.True(resp.Success);
                Assert.NotNull(resp.Data);

                var url = resp.Data.Value.GetProperty("Url").GetString()!;
                var relPath = url.Substring(url.IndexOf("/TempFiles/")).TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                var fullPath = Path.Combine(tempDir, "wwwroot", relPath);

                using var wb = new XLWorkbook(fullPath);
                var ws = wb.Worksheet("Báo cáo lượt dịch vụ");
                var lastRow = ws.LastRowUsed()?.RowNumber() ?? 0;
                Assert.Equal(2, lastRow); // Dòng 1 header + 1 dòng dữ liệu (RO_ST_002)

                Assert.Equal("1", ws.Cell(2, 1).GetString());
                Assert.Equal("DL_SG", ws.Cell(2, 2).GetString());
                Assert.Equal("Đại lý Sài Gòn", ws.Cell(2, 3).GetString());
                Assert.Equal("RO_ST_002", ws.Cell(2, 9).GetString());
            }
            finally
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }
    }

    [Fact]
    public async Task Export_Ca3_FilterByPointFlags_IsAbleToUsePointNotUse()
    {
        var (db, controller, conn, tempDir) = await CreateContextAsync();
        using (conn)
        {
            try
            {
                // IsAbleToUsePointNotUse = "1" -> AmountAvailPrevious > 0, Amount > 0, AmountChTotal == 0 (RO_ST_002)
                var res = await controller.Export(new Crd_CardTransactionServiceTurnSearchReq
                {
                    IsAbleToUsePointNotUse = "1"
                });
                var resp = ToResp(res);

                Assert.True(resp.Success);
                Assert.NotNull(resp.Data);

                var url = resp.Data.Value.GetProperty("Url").GetString()!;
                var relPath = url.Substring(url.IndexOf("/TempFiles/")).TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                var fullPath = Path.Combine(tempDir, "wwwroot", relPath);

                using var wb = new XLWorkbook(fullPath);
                var ws = wb.Worksheet("Báo cáo lượt dịch vụ");
                var lastRow = ws.LastRowUsed()?.RowNumber() ?? 0;
                Assert.Equal(2, lastRow); // Header + 1 dòng (RO_ST_002)
                Assert.Equal("RO_ST_002", ws.Cell(2, 9).GetString());
            }
            finally
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }
    }

    [Fact]
    public async Task Export_Ca4_FilterByPointFlags_IsNotAbleToUsePoint()
    {
        var (db, controller, conn, tempDir) = await CreateContextAsync();
        using (conn)
        {
            try
            {
                // IsNotAbleToUsePoint = "1" -> AmountAvailPrevious == 0 || Amount == 0 (RO_ST_003)
                var res = await controller.Export(new Crd_CardTransactionServiceTurnSearchReq
                {
                    IsNotAbleToUsePoint = "1"
                });
                var resp = ToResp(res);

                Assert.True(resp.Success);
                Assert.NotNull(resp.Data);

                var url = resp.Data.Value.GetProperty("Url").GetString()!;
                var relPath = url.Substring(url.IndexOf("/TempFiles/")).TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                var fullPath = Path.Combine(tempDir, "wwwroot", relPath);

                using var wb = new XLWorkbook(fullPath);
                var ws = wb.Worksheet("Báo cáo lượt dịch vụ");
                var lastRow = ws.LastRowUsed()?.RowNumber() ?? 0;
                Assert.Equal(2, lastRow); // Header + 1 dòng (RO_ST_003)
                Assert.Equal("RO_ST_003", ws.Cell(2, 9).GetString());
            }
            finally
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }
    }

    [Fact]
    public async Task Export_Ca5_EmptyResult_ThrowsServiceException_ReturnsFail400()
    {
        var (db, controller, conn, tempDir) = await CreateContextAsync();
        using (conn)
        {
            try
            {
                // Lọc DLCode không tồn tại -> Fail 400 kèm thông điệp "Không có dữ liệu"
                var res = await controller.Export(new Crd_CardTransactionServiceTurnSearchReq
                {
                    DLCode = "DL_KHONG_TON_TAI"
                });

                var badReq = Assert.IsType<BadRequestObjectResult>(res);
                Assert.Equal(400, badReq.StatusCode);

                var resp = ToResp(badReq);
                Assert.False(resp.Success);
                Assert.Equal("ErridnLoyalty.Crd_CardTransactionServiceTurn_Export", resp.ErrorCode);
                Assert.NotNull(resp.Params);
                Assert.True(resp.Params.TryGetValue("Message", out var msgElem));
                Assert.Equal("Không có dữ liệu", msgElem.GetString());
            }
            finally
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }
    }

    [Fact]
    public async Task Verify_LiveServer_Port8518_P2Cases()
    {
        int port = 8518;
        string tempDir = Environment.GetEnvironmentVariable("TEMP") ?? Path.GetTempPath();
        string dbPath = Path.Combine(tempDir, "v018.db");

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
            // Ca 1: Export cơ bản không bộ lọc
            // ==========================================
            var (s1, j1) = await PostJsonAsync("/api/crd_cardtransactionserviceturn/export", new { });
            _output.WriteLine($"[Ca 1] Status={s1}, Body={j1}");
            Assert.Equal(200, s1);
            Assert.Contains("\"Success\":true", j1);
            Assert.Contains("\"Url\":", j1);
            Assert.Contains("/TempFiles/", j1);
            Assert.Contains("Crd_CardTransactionServiceTurn", j1);

            // ==========================================
            // Ca 2: Lọc theo DLCode = "DL_SG"
            // ==========================================
            var (s2, j2) = await PostJsonAsync("/api/crd_cardtransactionserviceturn/export", new
            {
                DLCode = "DL_SG"
            });
            _output.WriteLine($"[Ca 2] Status={s2}, Body={j2}");
            Assert.Equal(200, s2);
            Assert.Contains("\"Success\":true", j2);
            Assert.Contains("\"Url\":", j2);

            // ==========================================
            // Ca 3: Lọc IsAbleToUsePointNotUse = "1"
            // ==========================================
            var (s3, j3) = await PostJsonAsync("/api/crd_cardtransactionserviceturn/export", new
            {
                IsAbleToUsePointNotUse = "1"
            });
            _output.WriteLine($"[Ca 3] Status={s3}, Body={j3}");
            Assert.Equal(200, s3);
            Assert.Contains("\"Success\":true", j3);
            Assert.Contains("\"Url\":", j3);

            // ==========================================
            // Ca 4: Lọc IsNotAbleToUsePoint = "1"
            // ==========================================
            var (s4, j4) = await PostJsonAsync("/api/crd_cardtransactionserviceturn/export", new
            {
                IsNotAbleToUsePoint = "1"
            });
            _output.WriteLine($"[Ca 4] Status={s4}, Body={j4}");
            Assert.Equal(200, s4);
            Assert.Contains("\"Success\":true", j4);
            Assert.Contains("\"Url\":", j4);

            // ==========================================
            // Ca 5: Lọc DLCode không tồn tại -> 400 Bad Request, "Không có dữ liệu"
            // ==========================================
            var (s5, j5) = await PostJsonAsync("/api/crd_cardtransactionserviceturn/export", new
            {
                DLCode = "DL_KHONG_TON_TAI"
            });
            _output.WriteLine($"[Ca 5] Status={s5}, Body={j5}");
            Assert.Equal(400, s5);
            Assert.Contains("\"Success\":false", j5);
            Assert.Contains("Crd_CardTransactionServiceTurn_Export", j5);
            Assert.Contains("Không có dữ liệu", j5);
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
