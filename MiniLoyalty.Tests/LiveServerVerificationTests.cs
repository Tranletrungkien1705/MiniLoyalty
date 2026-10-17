using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace MiniLoyalty.Tests;

public class LiveServerVerificationTests
{
    private readonly ITestOutputHelper _output;

    public LiveServerVerificationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task Verify_All10Cases_OnLiveServer_Port8572()
    {
        int port = 8572;
        string tempDir = Environment.GetEnvironmentVariable("TEMP") ?? Path.GetTempPath();
        string dbPath = Path.Combine(tempDir, "v021.db");

        if (File.Exists(dbPath))
        {
            try { File.Delete(dbPath); } catch { }
        }

        string baseDir = AppContext.BaseDirectory;
        // Tìm file MiniLoyalty.dll trong bin/Release/net8.0
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

            async Task<(int status, string json)> PostJsonAsync(string path, object body)
            {
                var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
                var res = await http.PostAsync($"http://127.0.0.1:{port}{path}", content);
                var str = await res.Content.ReadAsStringAsync();
                return ((int)res.StatusCode, str);
            }

            // Ca 1: Body null / rỗng
            var (s1, j1) = await PostJsonAsync("/api/crd_dealserro/add", new { });
            _output.WriteLine($"[Ca 1] Status={s1}, Body={j1}");
            Assert.Equal(400, s1);
            Assert.Contains("ErridnLoyalty.Crd_DealSerRO_Add", j1);
            Assert.Contains("Check.strJson", j1);

            // Ca 2: Mã báo giá đã tồn tại
            var (s2, j2) = await PostJsonAsync("/api/crd_dealserro/add", new
            {
                Crd_DealSerRO = new { DealSerRONo = "RO001" }
            });
            _output.WriteLine($"[Ca 2] Status={s2}, Body={j2}");
            Assert.Equal(400, s2);
            Assert.Contains("ErridnLoyalty.Crd_DealSerRO_CheckDB_Crd_DealSerROExist", j2);
            Assert.Contains("RO001", j2);

            // Ca 3: Đại lý không tồn tại
            var (s3, j3) = await PostJsonAsync("/api/crd_dealserro/add", new
            {
                Crd_DealSerRO = new { DealSerRONo = "RO101", DLCode = "DL_KHONG_CO" }
            });
            _output.WriteLine($"[Ca 3] Status={s3}, Body={j3}");
            Assert.Equal(400, s3);
            Assert.Contains("ErridnLoyalty.Mst_DealerCP_CheckDB_DLNotFound", j3);
            Assert.Contains("DL_KHONG_CO", j3);

            // Ca 4: Đại lý bị khóa FlagActive != '1'
            var (s4, j4) = await PostJsonAsync("/api/crd_dealserro/add", new
            {
                Crd_DealSerRO = new { DealSerRONo = "RO101", DLCode = "DL02" }
            });
            _output.WriteLine($"[Ca 4] Status={s4}, Body={j4}");
            Assert.Equal(400, s4);
            Assert.Contains("ErridnLoyalty.Mst_DealerCP_CheckDB_FlagActiveNotMatched", j4);
            Assert.Contains("DL02", j4);
            Assert.Contains("\"0\"", j4);

            // Ca 5: Hội viên không tồn tại
            var (s5, j5) = await PostJsonAsync("/api/crd_dealserro/add", new
            {
                Crd_DealSerRO = new { DealSerRONo = "RO101", DLCode = "DL01", MemberNo = "MEM_KHONG_CO" }
            });
            _output.WriteLine($"[Ca 5] Status={s5}, Body={j5}");
            Assert.Equal(400, s5);
            Assert.Contains("ErridnLoyalty.Crd_Member_CheckDB_MemberNotFound", j5);
            Assert.Contains("MEM_KHONG_CO", j5);

            // Ca 6: Biển số xe hoặc VIN không khớp hội viên
            var (s6, j6) = await PostJsonAsync("/api/crd_dealserro/add", new
            {
                Crd_DealSerRO = new
                {
                    DealSerRONo = "RO101",
                    DLCode = "DL01",
                    MemberNo = "MEM01",
                    PlateNo = "30A-SAI",
                    VIN = "VIN000000001"
                }
            });
            _output.WriteLine($"[Ca 6] Status={s6}, Body={j6}");
            Assert.Equal(400, s6);
            Assert.Contains("ErridnLoyalty.Crd_DealSerRO_Add_PlateNoOrVINNotMatchMember", j6);
            Assert.Contains("30A-SAI", j6);

            // Ca 7: Thẻ không tồn tại
            var (s7, j7) = await PostJsonAsync("/api/crd_dealserro/add", new
            {
                Crd_DealSerRO = new
                {
                    DealSerRONo = "RO101",
                    DLCode = "DL01",
                    MemberNo = "MEM01",
                    PlateNo = "30A-99999",
                    VIN = "VIN000000001",
                    CardNo = "CARD_KHONG_CO"
                }
            });
            _output.WriteLine($"[Ca 7] Status={s7}, Body={j7}");
            Assert.Equal(400, s7);
            Assert.Contains("ErridnLoyalty.Crd_Card_CheckDB_CardNotFound", j7);
            Assert.Contains("CARD_KHONG_CO", j7);

            // Ca 8: Tiền sử dụng vượt quá tích lũy
            var (s8, j8) = await PostJsonAsync("/api/crd_dealserro/add", new
            {
                Crd_DealSerRO = new
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
            _output.WriteLine($"[Ca 8] Status={s8}, Body={j8}");
            Assert.Equal(400, s8);
            Assert.Contains("ErridnLoyalty.Crd_DealSerRO_Add_Input_InvalidAmountUse", j8);
            Assert.Contains("999000000", j8);

            // Ca 9: Khoản mục chi phí không tồn tại trong Lst_Crd_DealSerRODtl
            var (s9, j9) = await PostJsonAsync("/api/crd_dealserro/add", new
            {
                Crd_DealSerRO = new
                {
                    DealSerRONo = "RO101",
                    DLCode = "DL01",
                    MemberNo = "MEM01",
                    PlateNo = "30A-99999",
                    VIN = "VIN000000001",
                    CardNo = "CARD01",
                    AmountUse = 0
                },
                Lst_Crd_DealSerRODtl = new[]
                {
                    new { ExpenseType = "EX_KHONG_CO", Amount = 1000000, FlagPoint = "1" }
                }
            });
            _output.WriteLine($"[Ca 9] Status={s9}, Body={j9}");
            Assert.Equal(400, s9);
            Assert.Contains("ErridnLoyalty.Mst_ExpenseType_CheckDB_ExpenseTypeNotFound", j9);
            Assert.Contains("EX_KHONG_CO", j9);

            // Ca 10: Thêm mới thành công và Round-trip đọc lại
            var (s10, j10) = await PostJsonAsync("/api/crd_dealserro/add", new
            {
                Crd_DealSerRO = new
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
                Lst_Crd_DealSerRODtl = new[]
                {
                    new { ExpenseType = "EX01", Amount = 2000000, FlagPoint = "1" }
                }
            });
            _output.WriteLine($"[Ca 10] Status={s10}, Body={j10}");
            Assert.Equal(200, s10);
            Assert.Contains("\"Success\":true", j10);
            Assert.Contains("\"Data\":null", j10);

            // Round-trip đọc lại qua /api/crd_dealserro/search
            var (sRt, jRt) = await PostJsonAsync("/api/crd_dealserro/search", new
            {
                MemberNo = "MEM01"
            });
            _output.WriteLine($"[Round-trip] Status={sRt}, Body={jRt}");
            Assert.Equal(200, sRt);
            Assert.Contains("RO101", jRt);
            Assert.Contains("PENDING", jRt);
            Assert.Contains("BAO GIA BAO DUONG XE DINH KY", jRt);
        }
        finally
        {
            try
            {
                if (!proc.HasExited)
                {
                    proc.Kill(true);
                    proc.WaitForExit(5000);
                }
            }
            catch { }

            if (File.Exists(dbPath))
            {
                try { File.Delete(dbPath); } catch { }
            }
            _output.WriteLine("Server stopped and temporary database cleaned up.");
        }
    }
}
