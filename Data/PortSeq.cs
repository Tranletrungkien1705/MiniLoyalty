using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace MiniLoyalty.Data;

/// <summary>
/// Tiện ích sinh mã sequence dùng chung cho các màn port 1:1 từ 2022.7.Loyalty (Seq.cs:28-41, 137, 152-185).
/// </summary>
public static class PortSeq
{
    private const string EncryptTable = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const int DefaultRootYear = 2010; // TConst.BizMix.Default_RootYear (Const.Main.BE.cs:15)

    /// <summary>
    /// Định dạng chuỗi sequence theo chuẩn nguồn (Seq.cs:173-180).
    /// Dùng lại cho các sequence sau.
    /// </summary>
    /// <param name="format">Mẫu định dạng, ví dụ: "{0}RANKPOLICYCODE.{3}.{1:00000}{2}"</param>
    /// <param name="prefix">Tiền tố {0}, để trống nếu null</param>
    /// <param name="seq">Số sequence hiện tại (lấy từ AutoId của bảng Seq_*)</param>
    /// <param name="maxSeq">Giới hạn modulo {1}, ví dụ 100000</param>
    /// <param name="postfix">Hậu tố {2}, để trống nếu null</param>
    /// <param name="utcNow">Thời điểm UTC để mã hoá chuỗi 3 ký tự ymd {3}</param>
    /// <returns>Chuỗi mã sequence đã định dạng</returns>
    public static string Build(string format, string? prefix, long seq, long maxSeq, string? postfix, DateTime utcNow)
    {
        // ymd = 3 ký tự: [UtcNow.Year - 2010], [UtcNow.Month], [UtcNow.Day]
        // Bảng mã hoá: "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ" (Seq.cs:173, 179)
        var yIdx = Math.Clamp(utcNow.Year - DefaultRootYear, 0, EncryptTable.Length - 1);
        var mIdx = Math.Clamp(utcNow.Month, 0, EncryptTable.Length - 1);
        var dIdx = Math.Clamp(utcNow.Day, 0, EncryptTable.Length - 1);
        var ymd = $"{EncryptTable[yIdx]}{EncryptTable[mIdx]}{EncryptTable[dIdx]}";

        var effectiveSeq = maxSeq > 0 ? seq % maxSeq : seq;
        return string.Format(format, prefix ?? "", effectiveSeq, postfix ?? "", ymd);
    }

    /// <summary>
    /// Lấy số sequence mới từ bảng Seq_* trong một transaction RIÊNG đã commit độc lập (Seq.cs:28-41).
    /// Nhờ commit độc lập, sequence vẫn tăng kể cả khi nghiệp vụ lưu chính sau đó bị lỗi hoặc rollback.
    /// </summary>
    public static async Task<long> NextAsync<TSeq>(AppDbContext db) where TSeq : class, new()
    {
        await using var tx = await db.Database.BeginTransactionAsync();
        var entity = new TSeq();
        db.Set<TSeq>().Add(entity);
        await db.SaveChangesAsync();

        var entry = db.Entry(entity);
        var pkProp = entry.Metadata.FindPrimaryKey()?.Properties.FirstOrDefault();
        long seq = Convert.ToInt64(pkProp?.PropertyInfo?.GetValue(entity) ?? 0);

        // Xóa dòng tạm sau khi đã lấy AutoId (Seq.cs:33: delete {0} where AutoID = @@Identity)
        db.Set<TSeq>().Remove(entity);
        await db.SaveChangesAsync();

        await tx.CommitAsync();
        return seq;
    }
}
