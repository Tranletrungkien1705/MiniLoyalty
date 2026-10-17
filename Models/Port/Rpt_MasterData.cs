using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniLoyalty.Data;
using MiniLoyalty.Models;

namespace MiniLoyalty.Models.Port;

/// <summary>
/// Bảng Rpt_MasterData: Báo cáo dữ liệu tổng hợp chung (54 cột).
/// Khoá chính PeriodMonth (MyDateTime).
/// Nguồn: 12.Dev.Common\idn.Loyalty.Common\Models\Rpt_MasterData.cs:9-80
/// DDL: 11.BackEnd\V10\05.Refs.Biz\Migrate\20230222.AlterTableRpt_MasterData.sql:51-99
/// </summary>
public class Rpt_MasterData
{
    /// <summary>Tháng báo cáo (Khoá chính) - Rpt_MasterData.cs:11</summary>
    public DateTime? PeriodMonth { get; set; }

    /// <summary>Mã Đại lý - Rpt_MasterData.cs:12</summary>
    public string? DLCode { get; set; }

    /// <summary>Ngày báo cáo - Rpt_MasterData.cs:13</summary>
    public DateTime? RptDate { get; set; }

    /// <summary>Loại báo cáo : Tháng(Month), Quý(Quarter), Năm(Year) - Rpt_MasterData.cs:14</summary>
    public string? RptDateType { get; set; }

    /// <summary>Tháng báo cáo - Rpt_MasterData.cs:15</summary>
    public string? RptDateMonth { get; set; }

    /// <summary>Quý báo cáo - Rpt_MasterData.cs:16</summary>
    public string? RptDateQuarter { get; set; }

    /// <summary>Năm báo cáo - Rpt_MasterData.cs:17</summary>
    public string? RptDateYear { get; set; }

    /// <summary>Mã Network - Rpt_MasterData.cs:18</summary>
    public string? NetworkID { get; set; }

    /// <summary>Mã Org - Rpt_MasterData.cs:19</summary>
    public string? OrgID { get; set; }

    /// <summary>Tổng thẻ hội viên phát hành mới trong kỳ hạng S - Rpt_MasterData.cs:21</summary>
    public double? QtyRegistrationCardS { get; set; }

    /// <summary>Tổng thẻ hội viên phát hành mới đặc cách hạng G - Rpt_MasterData.cs:22</summary>
    public double? QtyRegistrationCardG { get; set; }

    /// <summary>Tổng thẻ hội viên phát hành mới đặc cách hạng P - Rpt_MasterData.cs:23</summary>
    public double? QtyRegistrationCardP { get; set; }

    /// <summary>Tổng lượt tăng hạng trong kỳ - Rpt_MasterData.cs:25</summary>
    public int? QtyCardUp { get; set; }

    /// <summary>Tổng lượt duy trì hạng trong kỳ (DDL QtyCardDown) - Rpt_MasterData.cs:26</summary>
    public double? QtyCardDown { get; set; }

    /// <summary>Tổng lượt xuống hạng trong kỳ (DDL QtyCardKeep) - Rpt_MasterData.cs:27</summary>
    public double? QtyCardKeep { get; set; }

    /// <summary>Tổng hội viên hủy trong kỳ - Rpt_MasterData.cs:28</summary>
    public double? QtyMemberCancel { get; set; }

    /// <summary>Tổng thẻ hội viên lũy kế hạng S - Rpt_MasterData.cs:30</summary>
    public double? QtyCardS { get; set; }

    /// <summary>Tổng thẻ hội viên lũy kế hạng G - Rpt_MasterData.cs:31</summary>
    public double? QtyCardG { get; set; }

    /// <summary>Tổng thẻ hội viên lũy kế hạng P - Rpt_MasterData.cs:32</summary>
    public double? QtyCardP { get; set; }

    /// <summary>Tổng số hội viên lũy kế đặc cách hạng G - Rpt_MasterData.cs:34</summary>
    public double? QtyCardExceptionG { get; set; }

    /// <summary>Tổng số hội viên lũy kế đặc cách hạng P - Rpt_MasterData.cs:35</summary>
    public double? QtyCardExceptionP { get; set; }

    /// <summary>Tổng lượt dịch vụ khách hàng trả tiền trong kỳ (DMS) - Rpt_MasterData.cs:37</summary>
    public double? QtyServiceDMSPaymented { get; set; }

    /// <summary>Tổng lượt dịch vụ trong kỳ(DMS) - Rpt_MasterData.cs:38</summary>
    public double? QtyServiceDMS { get; set; }

    /// <summary>Tổng số Xe làm dịch vụ trong kỳ(DMS) - Rpt_MasterData.cs:39</summary>
    public double? QtyCarUseServiceDMS { get; set; }

    /// <summary>Tổng lượt dịch vụ tích điểm tiêu dùng trong kỳ - Rpt_MasterData.cs:41</summary>
    public double? QtyConsumption { get; set; }

    /// <summary>Tổng lượt dịch vụ tích điểm xét hạng trong kỳ - Rpt_MasterData.cs:42</summary>
    public double? QtyInCrease { get; set; }

    /// <summary>Tổng lượt dịch vụ không thể tiêu điểm - Rpt_MasterData.cs:43</summary>
    public double? QtyServiceTurn { get; set; }

    /// <summary>Tổng doanh thu dịch vụ xét hạng trong kỳ - Rpt_MasterData.cs:45</summary>
    public double? AmountPointInCrease { get; set; }

    /// <summary>Tổng doanh thu dịch vụ tiêu dùng trong kỳ - Rpt_MasterData.cs:46</summary>
    public double? AmountPointUse { get; set; }

    /// <summary>Tổng lượt dịch vụ tiêu điểm trong kỳ - Rpt_MasterData.cs:48</summary>
    public double? QtyPointUse { get; set; }

    /// <summary>Tổng hội viên tiêu điểm dịch vụ trong kỳ - Rpt_MasterData.cs:49</summary>
    public double? QtyMemberPointUse { get; set; }

    /// <summary>Tổng số hội viên sử dụng ưu đãi trong kỳ - Rpt_MasterData.cs:50</summary>
    public double? QtyMemberUsePromotion { get; set; }

    /// <summary>Tổng Doanh thu (tích điểm tiêu dùng) trong kỳ hạng S - Rpt_MasterData.cs:52</summary>
    public double? AmountConsumptionS { get; set; }

    /// <summary>Tổng Doanh thu (tích điểm tiêu dùng) trong kỳ hạng G - Rpt_MasterData.cs:53</summary>
    public double? AmountConsumptionG { get; set; }

    /// <summary>Tổng Doanh thu (tích điểm tiêu dùng) trong kỳ hạng P - Rpt_MasterData.cs:54</summary>
    public double? AmountConsumptionP { get; set; }

    /// <summary>Tổng Doanh thu (tích điểm xét hạng) trong kỳ hạng S - Rpt_MasterData.cs:56</summary>
    public double? AmountPointInCreaseS { get; set; }

    /// <summary>Tổng Doanh thu (tích điểm xét hạng) trong kỳ hạng G - Rpt_MasterData.cs:57</summary>
    public double? AmountPointInCreaseG { get; set; }

    /// <summary>Tổng Doanh thu (tích điểm xét hạng) trong kỳ hạng P - Rpt_MasterData.cs:58</summary>
    public double? AmountPointInCreaseP { get; set; }

    /// <summary>Tổng điểm tiêu dùng tích dịch vụ trong kỳ - Rpt_MasterData.cs:60</summary>
    public double? PointConsumption { get; set; }

    /// <summary>Tổng điểm tiêu dùng tích xe mới trong kỳ - Rpt_MasterData.cs:61</summary>
    public double? PointSales { get; set; }

    /// <summary>Tổng điểm tiêu dùng tích giới thiệu mua trong kỳ - Rpt_MasterData.cs:62</summary>
    public double? PointIntroduction { get; set; }

    /// <summary>Tổng điểm tiêu dùng tích sinh nhật trong kỳ - Rpt_MasterData.cs:63</summary>
    public double? PointBirthday { get; set; }

    /// <summary>Tổng điểm tiêu dùng tiêu trong kỳ - Rpt_MasterData.cs:64</summary>
    public double? PointUse { get; set; }

    /// <summary>Tổng số điểm hết hạn trong kỳ - Rpt_MasterData.cs:65</summary>
    public double? PointExpiry { get; set; }

    /// <summary>Tổng số điểm hết hạn lũy kế - Rpt_MasterData.cs:66</summary>
    public double? PointExpiryAll { get; set; }

    /// <summary>Tổng điểm tiêu dùng lũy kế - Rpt_MasterData.cs:67</summary>
    public double? PointConsumptionAll { get; set; }

    /// <summary>Tổng thẻ hội viên > 6 tháng chưa quay lại làm dịch vụ - Rpt_MasterData.cs:69</summary>
    public double? QtyMemberNotBack6M { get; set; }

    /// <summary>Tổng thẻ hội viên > 12 tháng chưa quay lại làm dịch vụ - Rpt_MasterData.cs:70</summary>
    public double? QtyMemberNotBack12M { get; set; }

    /// <summary>Tổng thẻ hội viên làm dịch vụ 6 tháng gần nhất - Rpt_MasterData.cs:71</summary>
    public double? QtyMemberBack6M { get; set; }

    /// <summary>Tổng lượt DV của HV 12 tháng gần nhất - Rpt_MasterData.cs:72</summary>
    public double? QtyService { get; set; }

    /// <summary>Tổng số điểm Hủy trong kỳ - Rpt_MasterData.cs:74</summary>
    public double? PointCancel { get; set; }

    /// <summary>Tổng số điểm Hủy lũy kế - Rpt_MasterData.cs:75</summary>
    public double? PointCancelAll { get; set; }

    /// <summary>Thời điểm cập nhật UTC - Rpt_MasterData.cs:77</summary>
    public DateTime? LogLUDTimeUTC { get; set; }

    /// <summary>Người cập nhật - Rpt_MasterData.cs:78</summary>
    public string? LogLUBy { get; set; }
}

/// <summary>
/// Cấu hình ánh xạ EntityType cho bảng Rpt_MasterData (khoá chính PeriodMonth).
/// </summary>
public class Rpt_MasterDataConfiguration : IEntityTypeConfiguration<Rpt_MasterData>
{
    public void Configure(EntityTypeBuilder<Rpt_MasterData> builder)
    {
        builder.ToTable("Rpt_MasterData");
        builder.HasKey(x => x.PeriodMonth);
    }
}

/// <summary>
/// Bảng Rpt_MasterDataSpec: Báo cáo dữ liệu đặc thù theo đại lý (37 cột).
/// Khoá chính phức hợp (PeriodMonth MyDateTime, DLCPCode Varchar50).
/// Nguồn: 12.Dev.Common\idn.Loyalty.Common\Models\Rpt_MasterDataSpec.cs:9-60
/// DDL: 11.BackEnd\V10\05.Refs.Biz\Migrate\Script.QuyenNL\20230731.z11.CreateTable.Rpt_MasterDataSpec.sql:4-43
/// </summary>
public class Rpt_MasterDataSpec
{
    /// <summary>Tháng báo cáo (Khoá chính thành phần 1) - Rpt_MasterDataSpec.cs:11</summary>
    public DateTime? PeriodMonth { get; set; }

    /// <summary>Mã Đại lý (Khoá chính thành phần 2) - Rpt_MasterDataSpec.cs:12</summary>
    public string DLCPCode { get; set; } = "";

    /// <summary>Mã Network - Rpt_MasterDataSpec.cs:13</summary>
    public string? NetworkID { get; set; }

    /// <summary>Mã Org - Rpt_MasterDataSpec.cs:14</summary>
    public string? OrgID { get; set; }

    /// <summary>Tổng thẻ hội viên phát hành mới trong kỳ hạng S - Rpt_MasterDataSpec.cs:16</summary>
    public double? QtyRegistrationCardS { get; set; }

    /// <summary>Tổng thẻ hội viên phát hành mới đặc cách hạng G - Rpt_MasterDataSpec.cs:17</summary>
    public double? QtyRegistrationCardG { get; set; }

    /// <summary>Tổng thẻ hội viên phát hành mới đặc cách hạng P - Rpt_MasterDataSpec.cs:18</summary>
    public double? QtyRegistrationCardP { get; set; }

    /// <summary>Tổng lượt dịch vụ khách hàng trả tiền trong kỳ (DMS) - Rpt_MasterDataSpec.cs:20</summary>
    public double? QtyServiceDMSPaymented { get; set; }

    /// <summary>Tổng lượt dịch vụ trong kỳ(DMS) - Rpt_MasterDataSpec.cs:21</summary>
    public double? QtyServiceDMS { get; set; }

    /// <summary>Tổng số Xe làm dịch vụ trong kỳ(DMS) - Rpt_MasterDataSpec.cs:22</summary>
    public double? QtyCarUseServiceDMS { get; set; }

    /// <summary>Tổng lượt dịch vụ tích điểm tiêu dùng trong kỳ - Rpt_MasterDataSpec.cs:24</summary>
    public double? QtyConsumption { get; set; }

    /// <summary>Tổng lượt dịch vụ tích điểm xét hạng trong kỳ - Rpt_MasterDataSpec.cs:25</summary>
    public double? QtyInCrease { get; set; }

    /// <summary>Tổng lượt dịch vụ không thể tiêu điểm - Rpt_MasterDataSpec.cs:26</summary>
    public double? QtyServiceTurn { get; set; }

    /// <summary>Tổng doanh thu dịch vụ xét hạng trong kỳ - Rpt_MasterDataSpec.cs:28</summary>
    public double? AmountPointInCrease { get; set; }

    /// <summary>Tổng doanh thu dịch vụ tiêu dùng trong kỳ - Rpt_MasterDataSpec.cs:29</summary>
    public double? AmountPointUse { get; set; }

    /// <summary>Tổng lượt dịch vụ tiêu điểm trong kỳ - Rpt_MasterDataSpec.cs:31</summary>
    public double? QtyPointUse { get; set; }

    /// <summary>Tổng hội viên tiêu điểm dịch vụ trong kỳ - Rpt_MasterDataSpec.cs:32</summary>
    public double? QtyMemberPointUse { get; set; }

    /// <summary>Tổng số hội viên sử dụng ưu đãi trong kỳ - Rpt_MasterDataSpec.cs:33</summary>
    public double? QtyMemberUsePromotion { get; set; }

    /// <summary>Tổng Doanh thu (tích điểm tiêu dùng) trong kỳ hạng S - Rpt_MasterDataSpec.cs:35</summary>
    public double? AmountConsumptionS { get; set; }

    /// <summary>Tổng Doanh thu (tích điểm tiêu dùng) trong kỳ hạng G - Rpt_MasterDataSpec.cs:36</summary>
    public double? AmountConsumptionG { get; set; }

    /// <summary>Tổng Doanh thu (tích điểm tiêu dùng) trong kỳ hạng P - Rpt_MasterDataSpec.cs:37</summary>
    public double? AmountConsumptionP { get; set; }

    /// <summary>Tổng Doanh thu (tích điểm xét hạng) trong kỳ hạng S - Rpt_MasterDataSpec.cs:39</summary>
    public double? AmountPointInCreaseS { get; set; }

    /// <summary>Tổng Doanh thu (tích điểm xét hạng) trong kỳ hạng G - Rpt_MasterDataSpec.cs:40</summary>
    public double? AmountPointInCreaseG { get; set; }

    /// <summary>Tổng Doanh thu (tích điểm xét hạng) trong kỳ hạng P - Rpt_MasterDataSpec.cs:41</summary>
    public double? AmountPointInCreaseP { get; set; }

    /// <summary>Tổng điểm tiêu dùng tích dịch vụ trong kỳ - Rpt_MasterDataSpec.cs:43</summary>
    public double? PointConsumption { get; set; }

    /// <summary>Tổng điểm tiêu dùng tích xe mới trong kỳ - Rpt_MasterDataSpec.cs:44</summary>
    public double? PointSales { get; set; }

    /// <summary>Tổng điểm tiêu dùng tích giới thiệu mua trong kỳ - Rpt_MasterDataSpec.cs:45</summary>
    public double? PointIntroduction { get; set; }

    /// <summary>Tổng điểm tiêu dùng tích sinh nhật trong kỳ - Rpt_MasterDataSpec.cs:46</summary>
    public double? PointBirthday { get; set; }

    /// <summary>Tổng điểm tiêu dùng tiêu trong kỳ - Rpt_MasterDataSpec.cs:47</summary>
    public double? PointUse { get; set; }

    /// <summary>Tổng số điểm hết hạn trong kỳ - Rpt_MasterDataSpec.cs:48</summary>
    public double? PointExpiry { get; set; }

    /// <summary>Tổng số điểm hết hạn lũy kế - Rpt_MasterDataSpec.cs:49</summary>
    public double? PointExpiryAll { get; set; }

    /// <summary>Tổng điểm tiêu dùng lũy kế - Rpt_MasterDataSpec.cs:51</summary>
    public double? PointConsumptionAll { get; set; }

    /// <summary>Tổng lượt DV của HV 12 tháng gần nhất - Rpt_MasterDataSpec.cs:52</summary>
    public double? QtyService { get; set; }

    /// <summary>Tổng số điểm Hủy trong kỳ - Rpt_MasterDataSpec.cs:54</summary>
    public double? PointCancel { get; set; }

    /// <summary>Tổng số điểm Hủy lũy kế - Rpt_MasterDataSpec.cs:55</summary>
    public double? PointCancelAll { get; set; }

    /// <summary>Thời điểm cập nhật UTC - Rpt_MasterDataSpec.cs:57</summary>
    public DateTime? LogLUDTimeUTC { get; set; }

    /// <summary>Người cập nhật - Rpt_MasterDataSpec.cs:58</summary>
    public string? LogLUBy { get; set; }
}

/// <summary>
/// Cấu hình ánh xạ EntityType cho bảng Rpt_MasterDataSpec (khoá chính phức hợp PeriodMonth, DLCPCode).
/// </summary>
public class Rpt_MasterDataSpecConfiguration : IEntityTypeConfiguration<Rpt_MasterDataSpec>
{
    public void Configure(EntityTypeBuilder<Rpt_MasterDataSpec> builder)
    {
        builder.ToTable("Rpt_MasterDataSpec");
        builder.HasKey(x => new { x.PeriodMonth, x.DLCPCode });
    }
}

/// <summary>
/// Seeder khởi tạo dữ liệu mẫu cho Rpt_MasterData (3 dòng) và Rpt_MasterDataSpec (4 dòng).
/// Thứ tự chạy: 78. Idempotent (kiểm tra AnyAsync trước khi Add).
/// </summary>
public class Rpt_MasterDataSeeder : IPortSeeder
{
    public int Order => 79;

    public async Task SeedAsync(AppDbContext db)
    {
        var hasMaster = await db.Set<Rpt_MasterData>().AnyAsync();
        var hasSpec = await db.Set<Rpt_MasterDataSpec>().AnyAsync();

        var now = DateTime.UtcNow;

        if (!hasMaster)
        {
            var masterRows = new[]
            {
                // Dòng 1: Tháng 2023-07-01
                new Rpt_MasterData
                {
                    PeriodMonth = new DateTime(2023, 7, 1, 0, 0, 0, DateTimeKind.Unspecified),
                    NetworkID = "DEMO",
                    OrgID = "ORG01",
                    QtyRegistrationCardS = 10,
                    QtyCardS = 100,
                    LogLUDTimeUTC = now,
                    LogLUBy = "seed"
                },
                // Dòng 2: Tháng 2023-08-01
                new Rpt_MasterData
                {
                    PeriodMonth = new DateTime(2023, 8, 1, 0, 0, 0, DateTimeKind.Unspecified),
                    NetworkID = "DEMO",
                    OrgID = "ORG01",
                    QtyRegistrationCardS = 20,
                    QtyCardS = 120,
                    LogLUDTimeUTC = now,
                    LogLUBy = "seed"
                },
                // Dòng 3: Tháng 2023-05-01 (trước mốc cứng 2023-06-01 để kiểm chứng điều kiện lọc cứng)
                new Rpt_MasterData
                {
                    PeriodMonth = new DateTime(2023, 5, 1, 0, 0, 0, DateTimeKind.Unspecified),
                    NetworkID = "DEMO",
                    OrgID = "ORG01",
                    QtyRegistrationCardS = 5,
                    QtyCardS = 80,
                    LogLUDTimeUTC = now,
                    LogLUBy = "seed"
                }
            };

            await db.Set<Rpt_MasterData>().AddRangeAsync(masterRows);
        }

        if (!hasSpec)
        {
            var specRows = new[]
            {
                // Dòng S1: Tháng 2023-07-01, DL01
                new Rpt_MasterDataSpec
                {
                    PeriodMonth = new DateTime(2023, 7, 1, 0, 0, 0, DateTimeKind.Unspecified),
                    DLCPCode = "DL01",
                    NetworkID = "DEMO",
                    OrgID = "ORG01",
                    QtyRegistrationCardS = 6,
                    QtyService = 15,
                    LogLUDTimeUTC = now,
                    LogLUBy = "seed"
                },
                // Dòng S2: Tháng 2023-07-01, DL02
                new Rpt_MasterDataSpec
                {
                    PeriodMonth = new DateTime(2023, 7, 1, 0, 0, 0, DateTimeKind.Unspecified),
                    DLCPCode = "DL02",
                    NetworkID = "DEMO",
                    OrgID = "ORG01",
                    QtyRegistrationCardS = 4,
                    QtyService = 10,
                    LogLUDTimeUTC = now,
                    LogLUBy = "seed"
                },
                // Dòng S3: Tháng 2023-08-01, DL01
                new Rpt_MasterDataSpec
                {
                    PeriodMonth = new DateTime(2023, 8, 1, 0, 0, 0, DateTimeKind.Unspecified),
                    DLCPCode = "DL01",
                    NetworkID = "DEMO",
                    OrgID = "ORG01",
                    QtyRegistrationCardS = 12,
                    QtyService = 25,
                    LogLUDTimeUTC = now,
                    LogLUBy = "seed"
                },
                // Dòng S4: Tháng 2023-05-01, DL01 (trước mốc cứng 2023-06-01)
                new Rpt_MasterDataSpec
                {
                    PeriodMonth = new DateTime(2023, 5, 1, 0, 0, 0, DateTimeKind.Unspecified),
                    DLCPCode = "DL01",
                    NetworkID = "DEMO",
                    OrgID = "ORG01",
                    QtyRegistrationCardS = 5,
                    QtyService = 8,
                    LogLUDTimeUTC = now,
                    LogLUBy = "seed"
                }
            };

            await db.Set<Rpt_MasterDataSpec>().AddRangeAsync(specRows);
        }

        // Seed CrdMember và CrdCardTransaction cho POINTUSE và PRPROGRAM để kiểm chứng Quarter/Year (fix_note.md #1)
        var memberSet = db.Set<CrdMember>();
        if (!await memberSet.AnyAsync(x => x.MemberNo == "MB_RPT_01"))
        {
            await memberSet.AddRangeAsync(
                new CrdMember
                {
                    MemberNo = "MB_RPT_01",
                    MemberName = "Hội viên RPT 01",
                    NetworkID = "DEMO",
                    OrgID = "ORG01",
                    DLCodeRegis = "DL01",
                    LogLUBy = "seed",
                    LogLUDTimeUTC = now
                },
                new CrdMember
                {
                    MemberNo = "MB_RPT_02",
                    MemberName = "Hội viên RPT 02",
                    NetworkID = "DEMO",
                    OrgID = "ORG01",
                    DLCodeRegis = "DL01",
                    LogLUBy = "seed",
                    LogLUDTimeUTC = now
                }
            );
        }

        var txSet = db.Set<CrdCardTransaction>();
        if (!await txSet.AnyAsync(x => x.RefNo == "TX_RPT_POINTUSE_01"))
        {
            await txSet.AddRangeAsync(
                new CrdCardTransaction
                {
                    RefNo = "TX_RPT_POINTUSE_01",
                    MemberNo = "MB_RPT_01",
                    CardNo = "CD_RPT_01",
                    DLCode = "DL01",
                    DealPointType = "POINTUSE",
                    PointChTotal = 50,
                    CreateDTimeUTC = new DateTime(2023, 7, 15, 10, 0, 0, DateTimeKind.Utc),
                    LogLUBy = "seed",
                    LogLUDTimeUTC = now
                },
                new CrdCardTransaction
                {
                    RefNo = "TX_RPT_PRPROGRAM_01",
                    MemberNo = "MB_RPT_02",
                    CardNo = "CD_RPT_02",
                    DLCode = "DL01",
                    DealPointType = "PRPROGRAM",
                    QtyPrUsed = 1,
                    CreateDTimeUTC = new DateTime(2023, 8, 20, 10, 0, 0, DateTimeKind.Utc),
                    LogLUBy = "seed",
                    LogLUDTimeUTC = now
                }
            );
        }

        await db.SaveChangesAsync();
    }
}
