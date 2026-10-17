using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
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
using Xunit.Abstractions;

namespace MiniLoyalty.Tests;

public class Crd_MemberChangeInfoFinishTests
{
    private readonly ITestOutputHelper _output;

    public Crd_MemberChangeInfoFinishTests(ITestOutputHelper output)
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

    private static async Task<(AppDbContext db, Crd_MemberChangeInfoControllerPort controller, SqliteConnection conn)> CreateContextAsync()
    {
        var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        var opt = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(conn)
            .Options;
        var db = new AppDbContext(opt, new TenantContext { OrgId = TenantContext.DefaultOrgId });
        db.Database.EnsureCreated();
        await Seeder.SeedAsync(db);
        var controller = new Crd_MemberChangeInfoControllerPort(db);
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
            var res = await controller.Finish(new Crd_MemberChangeInfoFinishReq());
            var resp = ToResp(res);
            Assert.False(resp.Success);
            Assert.Equal("ErridnLoyalty.Crd_MemberChangeInfo_Finish", resp.ErrorCode);
            Assert.True(resp.Params?.ContainsKey("Check.strJson"));
        }
    }

    [Fact]
    public async Task Finish_Ca2_NonExistentRequestNo_ReturnsNotFound()
    {
        var (db, controller, conn) = await CreateContextAsync();
        using (conn)
        {
            var res = await controller.Finish(new Crd_MemberChangeInfoFinishReq
            {
                Crd_MemberChangeInfo = new Crd_MemberChangeInfo { RequestNo = "KHONG_TON_TAI" }
            });
            var resp = ToResp(res);
            Assert.False(resp.Success);
            Assert.Equal("ErridnLoyalty.Crd_MemberChangeInfo_CheckDB_Crd_MemberChangeInfoNotFound", resp.ErrorCode);
            Assert.Equal("KHONG_TON_TAI", resp.Params?["Check.RequestNo"].GetString());
        }
    }

    [Fact]
    public async Task Finish_Ca3_RequestStatusNotMatched_Pending_ReturnsError()
    {
        var (db, controller, conn) = await CreateContextAsync();
        using (conn)
        {
            var res = await controller.Finish(new Crd_MemberChangeInfoFinishReq
            {
                Crd_MemberChangeInfo = new Crd_MemberChangeInfo { RequestNo = "REQ_FIN_STATUS_ERR" }
            });
            var resp = ToResp(res);
            Assert.False(resp.Success);
            Assert.Equal("ErridnLoyalty.Crd_MemberChangeInfo_CheckDB_RequestStatusNotMatched", resp.ErrorCode);
            Assert.Equal("REQ_FIN_STATUS_ERR", resp.Params?["Check.RequestNo"].GetString());
            Assert.Equal("APPROVE", resp.Params?["Check.strRequestStatusListToCheck"].GetString());
            Assert.Equal("PENDING", resp.Params?["DB.RequestStatus"].GetString());
        }
    }

    [Fact]
    public async Task Finish_Ca4_InvalidApproveRight_ReturnsError()
    {
        var (db, controller, conn) = await CreateContextAsync();
        using (conn)
        {
            var res = await controller.Finish(new Crd_MemberChangeInfoFinishReq
            {
                Crd_MemberChangeInfo = new Crd_MemberChangeInfo { RequestNo = "REQ_FIN_RIGHT_ERR" },
                WAUserCode = "port",
                DLCPCode = "VN001"
            });
            var resp = ToResp(res);
            Assert.False(resp.Success);
            Assert.Equal("ErridnLoyalty.Crd_MemberChangeInfo_Finish_InvalidApproveRight", resp.ErrorCode);
            Assert.Equal("VN001", resp.Params?["Check.DLCPCode"].GetString());
            Assert.Equal("VN999", resp.Params?["Check.ApproveDLCode"].GetString());
        }
    }

    [Fact]
    public async Task Finish_Ca5_ValidChangeInfo_UpdatesHeaderAndDetails()
    {
        var (db, controller, conn) = await CreateContextAsync();
        using (conn)
        {
            var res = await controller.Finish(new Crd_MemberChangeInfoFinishReq
            {
                Crd_MemberChangeInfo = new Crd_MemberChangeInfo
                {
                    RequestNo = "REQ_FIN_01",
                    RemarkHTV = "Ghi chu F"
                },
                WAUserCode = "port"
            });
            var resp = ToResp(res);
            Assert.True(resp.Success);

            // Kiểm tra DB
            var header = await db.Set<Crd_MemberChangeInfo>().FirstAsync(x => x.RequestNo == "REQ_FIN_01");
            Assert.Equal("FINISH", header.RequestStatus);
            Assert.Equal("port", header.FinishBy);
            Assert.NotNull(header.FinishDTimeUTC);
            Assert.Equal("Ghi chu F", header.RemarkHTV);
            // LỖI NGUỒN GIỮ NGUYÊN: Remark của header không đổi
            Assert.Equal("Ghi chu goc REQ_FIN_01", header.Remark);

            var dtl = await db.Set<Crd_MemberChangeInfoDtl>().FirstAsync(x => x.RequestNo == "REQ_FIN_01" && x.ColumnCode == "PhoneNo");
            Assert.Null(dtl.Remark); // vì strRemark = null
            Assert.Equal("port", dtl.LogLUBy);

            // Kiểm tra CrdMember PhoneNo được cập nhật
            var member = await db.CrdMembers.FirstAsync(x => x.MemberNo == "MB001");
            Assert.Equal("0987654321", member.PhoneNo);
        }
    }

    [Fact]
    public async Task Finish_Ca6_GetByRequestNo_ReturnsFinishedRecord()
    {
        var (db, controller, conn) = await CreateContextAsync();
        using (conn)
        {
            // Finish trước
            await controller.Finish(new Crd_MemberChangeInfoFinishReq
            {
                Crd_MemberChangeInfo = new Crd_MemberChangeInfo
                {
                    RequestNo = "REQ_FIN_01",
                    RemarkHTV = "Ghi chu F"
                },
                WAUserCode = "port"
            });

            // Đọc lại qua GetByRequestNo
            var res = await controller.GetByRequestNo(new Crd_MemberChangeInfoGetByRequestNoReq
            {
                RequestNo = "REQ_FIN_01"
            });

            var ok = Assert.IsType<OkObjectResult>(res);
            var json = JsonSerializer.Serialize(ok.Value);
            using var doc = JsonDocument.Parse(json);
            var data = doc.RootElement.GetProperty("Data");
            var header = data.GetProperty("Crd_MemberChangeInfo");
            Assert.Equal("FINISH", header.GetProperty("RequestStatus").GetString());
            Assert.False(string.IsNullOrEmpty(header.GetProperty("FinishDTimeUTC").GetString()));
        }
    }

    [Fact]
    public async Task Finish_Ca7_ValidCancelMember_CancelsMemberAndCard()
    {
        var (db, controller, conn) = await CreateContextAsync();
        using (conn)
        {
            var res = await controller.Finish(new Crd_MemberChangeInfoFinishReq
            {
                Crd_MemberChangeInfo = new Crd_MemberChangeInfo
                {
                    RequestNo = "REQ_FIN_CANCEL_01",
                    Remark = "Huy hoi vien"
                },
                WAUserCode = "port"
            });
            var resp = ToResp(res);
            Assert.True(resp.Success);

            // Kiểm tra CrdMember MB001
            var member = await db.CrdMembers.FirstAsync(x => x.MemberNo == "MB001");
            Assert.Equal("CANCEL", member.MemberStatus);
            Assert.Equal("CANCEL", member.CardStatus);
            Assert.Equal("port", member.InactiveBy);
            Assert.NotNull(member.InactiveDTimeUTC);
            Assert.Equal("Huy hoi vien", member.Remark);

            // Kiểm tra CrdCard CD001 bị huỷ
            var card = await db.CrdCards.FirstOrDefaultAsync(c => c.MemberNo == "MB001");
            Assert.NotNull(card);
            Assert.Equal("CANCEL", card!.CardStatus);
            Assert.Equal("port", card.CancelBy);
            Assert.NotNull(card.CancelDTimeUTC);
        }
    }

    [Fact]
    public async Task Finish_Ca8_GuardViolation_RegisStatusNotMatched_ReturnsError()
    {
        var (db, controller, conn) = await CreateContextAsync();
        using (conn)
        {
            var res = await controller.Finish(new Crd_MemberChangeInfoFinishReq
            {
                Crd_MemberChangeInfo = new Crd_MemberChangeInfo { RequestNo = "REQ_FIN_REGIS_ERR" },
                WAUserCode = "port"
            });
            var resp = ToResp(res);
            Assert.False(resp.Success);
            Assert.Equal("ErridnLoyalty.Crd_Member_CheckDB_RegisStatusNotMatched", resp.ErrorCode);
            Assert.Equal("MB_REGIS_ERR", resp.Params?["Check.MemberNo"].GetString());
            Assert.Equal("FINISH", resp.Params?["Check.strRegisStatusListToCheck"].GetString());
            Assert.Equal("PENDING", resp.Params?["DB.RegisStatus"].GetString());
        }
    }

    [Fact]
    public async Task Finish_Ca9_GuardViolation_CarModelNotFound_ReturnsError()
    {
        var (db, controller, conn) = await CreateContextAsync();
        using (conn)
        {
            var res = await controller.Finish(new Crd_MemberChangeInfoFinishReq
            {
                Crd_MemberChangeInfo = new Crd_MemberChangeInfo { RequestNo = "REQ_FIN_MODEL_ERR" },
                WAUserCode = "port"
            });
            var resp = ToResp(res);
            Assert.False(resp.Success);
            Assert.Equal("ErridnLoyalty.Mst_CarModel_CheckDB_CarModelNotFound", resp.ErrorCode);
            Assert.Equal("MODEL_NOT_FOUND", resp.Params?["Check.ModelCode"].GetString());
        }
    }

    [Fact]
    public async Task Finish_LiveApp_All7Cases_OnPort8508()
    {
        var tempDb = Path.Combine(Path.GetTempPath(), "v037.db");
        if (File.Exists(tempDb)) File.Delete(tempDb);

        var dllPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../bin/Release/net8.0/MiniLoyalty.dll"));
        if (!File.Exists(dllPath))
        {
            dllPath = Path.GetFullPath("bin/Release/net8.0/MiniLoyalty.dll");
        }

        var startInfo = new ProcessStartInfo("dotnet", dllPath)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };
        startInfo.EnvironmentVariables["PORT"] = "8508";
        startInfo.EnvironmentVariables["CONNECTION_STRING"] = $"Data Source={tempDb}";

        using var proc = Process.Start(startInfo);
        Assert.NotNull(proc);

        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };

            // Wait for /healthz
            var ready = false;
            for (var i = 0; i < 30; i++)
            {
                await Task.Delay(1000);
                try
                {
                    var healthRes = await http.GetAsync("http://127.0.0.1:8508/healthz");
                    if (healthRes.IsSuccessStatusCode)
                    {
                        ready = true;
                        break;
                    }
                }
                catch { }
            }
            Assert.True(ready, "App failed to start on port 8508 within 30 seconds");

            // Ca 1: Body empty
            var res1 = await http.PostAsync("http://127.0.0.1:8508/api/crd_memberchangeinfo/finish",
                new StringContent("{}", Encoding.UTF8, "application/json"));
            var body1 = await res1.Content.ReadAsStringAsync();
            _output.WriteLine($"[Ca 1] Status: {(int)res1.StatusCode}, Body: {body1}");
            Assert.Equal(400, (int)res1.StatusCode);
            Assert.Contains("Crd_MemberChangeInfo_Finish", body1);
            Assert.Contains("Check.strJson", body1);

            // Ca 2: Non-existent RequestNo
            var res2 = await http.PostAsync("http://127.0.0.1:8508/api/crd_memberchangeinfo/finish",
                new StringContent("{\"Crd_MemberChangeInfo\":{\"RequestNo\":\"KHONG_TON_TAI\"}}", Encoding.UTF8, "application/json"));
            var body2 = await res2.Content.ReadAsStringAsync();
            _output.WriteLine($"[Ca 2] Status: {(int)res2.StatusCode}, Body: {body2}");
            Assert.Equal(400, (int)res2.StatusCode);
            Assert.Contains("Crd_MemberChangeInfo_CheckDB_Crd_MemberChangeInfoNotFound", body2);

            // Ca 3: Status=PENDING
            var res3 = await http.PostAsync("http://127.0.0.1:8508/api/crd_memberchangeinfo/finish",
                new StringContent("{\"Crd_MemberChangeInfo\":{\"RequestNo\":\"REQ_FIN_STATUS_ERR\"}}", Encoding.UTF8, "application/json"));
            var body3 = await res3.Content.ReadAsStringAsync();
            _output.WriteLine($"[Ca 3] Status: {(int)res3.StatusCode}, Body: {body3}");
            Assert.Equal(400, (int)res3.StatusCode);
            Assert.Contains("Crd_MemberChangeInfo_CheckDB_RequestStatusNotMatched", body3);

            // Ca 4: Invalid ApproveDLCode
            var res4 = await http.PostAsync("http://127.0.0.1:8508/api/crd_memberchangeinfo/finish",
                new StringContent("{\"Crd_MemberChangeInfo\":{\"RequestNo\":\"REQ_FIN_RIGHT_ERR\"},\"WAUserCode\":\"port\",\"DLCPCode\":\"VN001\"}", Encoding.UTF8, "application/json"));
            var body4 = await res4.Content.ReadAsStringAsync();
            _output.WriteLine($"[Ca 4] Status: {(int)res4.StatusCode}, Body: {body4}");
            Assert.Equal(400, (int)res4.StatusCode);
            Assert.Contains("Crd_MemberChangeInfo_Finish_InvalidApproveRight", body4);

            // Ca 5: Finish REQ_FIN_01
            var res5 = await http.PostAsync("http://127.0.0.1:8508/api/crd_memberchangeinfo/finish",
                new StringContent("{\"Crd_MemberChangeInfo\":{\"RequestNo\":\"REQ_FIN_01\",\"RemarkHTV\":\"Ghi chu F\"},\"WAUserCode\":\"port\"}", Encoding.UTF8, "application/json"));
            var body5 = await res5.Content.ReadAsStringAsync();
            _output.WriteLine($"[Ca 5] Status: {(int)res5.StatusCode}, Body: {body5}");
            Assert.Equal(200, (int)res5.StatusCode);
            Assert.Contains("\"Success\":true", body5);

            // Ca 6: GetByRequestNo REQ_FIN_01
            var res6 = await http.PostAsync("http://127.0.0.1:8508/api/crd_memberchangeinfo/getbyrequestno",
                new StringContent("{\"RequestNo\":\"REQ_FIN_01\"}", Encoding.UTF8, "application/json"));
            var body6 = await res6.Content.ReadAsStringAsync();
            _output.WriteLine($"[Ca 6] Status: {(int)res6.StatusCode}, Body: {body6}");
            Assert.Equal(200, (int)res6.StatusCode);
            Assert.Contains("\"RequestStatus\":\"FINISH\"", body6);
            Assert.Contains("FinishDTimeUTC", body6);

            // Ca 7: Finish CancelMember REQ_FIN_CANCEL_01
            var res7 = await http.PostAsync("http://127.0.0.1:8508/api/crd_memberchangeinfo/finish",
                new StringContent("{\"Crd_MemberChangeInfo\":{\"RequestNo\":\"REQ_FIN_CANCEL_01\",\"Remark\":\"Huy hoi vien\"},\"WAUserCode\":\"port\"}", Encoding.UTF8, "application/json"));
            var body7 = await res7.Content.ReadAsStringAsync();
            _output.WriteLine($"[Ca 7] Status: {(int)res7.StatusCode}, Body: {body7}");
            Assert.Equal(200, (int)res7.StatusCode);
            Assert.Contains("\"Success\":true", body7);

            // Ca 8: Guard violation RegisStatusNotMatched (RegisStatus=PENDING)
            var res8 = await http.PostAsync("http://127.0.0.1:8508/api/crd_memberchangeinfo/finish",
                new StringContent("{\"Crd_MemberChangeInfo\":{\"RequestNo\":\"REQ_FIN_REGIS_ERR\"},\"WAUserCode\":\"port\"}", Encoding.UTF8, "application/json"));
            var body8 = await res8.Content.ReadAsStringAsync();
            _output.WriteLine($"[Ca 8] Status: {(int)res8.StatusCode}, Body: {body8}");
            Assert.Equal(400, (int)res8.StatusCode);
            Assert.Contains("Crd_Member_CheckDB_RegisStatusNotMatched", body8);

            // Ca 9: Guard violation CarModelNotFound (ModelCode không tồn tại)
            var res9 = await http.PostAsync("http://127.0.0.1:8508/api/crd_memberchangeinfo/finish",
                new StringContent("{\"Crd_MemberChangeInfo\":{\"RequestNo\":\"REQ_FIN_MODEL_ERR\"},\"WAUserCode\":\"port\"}", Encoding.UTF8, "application/json"));
            var body9 = await res9.Content.ReadAsStringAsync();
            _output.WriteLine($"[Ca 9] Status: {(int)res9.StatusCode}, Body: {body9}");
            Assert.Equal(400, (int)res9.StatusCode);
            Assert.Contains("Mst_CarModel_CheckDB_CarModelNotFound", body9);
        }
        finally
        {
            if (!proc.HasExited)
            {
                proc.Kill(entireProcessTree: true);
                proc.WaitForExit(3000);
            }
            if (File.Exists(tempDb))
            {
                try { File.Delete(tempDb); } catch { }
            }
        }
    }
}
