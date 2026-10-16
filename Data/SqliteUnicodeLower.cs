using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace MiniLoyalty.Data;

/// <summary>
/// SQLite <c>lower()</c> dựng sẵn CHỈ hạ chữ ASCII (INDEX_CONVERT bẫy #24): 'Đ' không thành 'đ' nên tìm "Đà Nẵng" trên SQLite trả 0 dòng
/// dù khớp tuyệt đối, trong khi nguồn SQL Server (collation CI) và Postgres đều không phân biệt hoa/thường tiếng Việt. Chứng cứ lỗi: QA-qa1-1.
/// Đăng ký MỘT LẦN cho mọi kết nối SQLite (khi mở kết nối) hàm <c>lower()</c> hiểu Unicode. Các port đã hạ chữ phía C# bằng
/// <c>ToLowerInvariant()</c> và phía cột bằng <c>ToLower()</c> (EF dịch thành lower()), nên hai phía khớp nhau; không cần sửa từng controller.
/// Hàm gắn ở mức kết nối hạ tầng, KHÔNG đăng ký trong action (xem docs\LESSONS_AGENTS.md A.1).
/// </summary>
public sealed class SqliteUnicodeLowerInterceptor : DbConnectionInterceptor
{
    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
        => Register(connection);

    public override Task ConnectionOpenedAsync(DbConnection connection, ConnectionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        Register(connection);
        return Task.CompletedTask;
    }

    private static void Register(DbConnection connection)
    {
        if (connection is SqliteConnection sqlite)
            sqlite.CreateFunction<string?, string?>("lower", value => value?.ToLowerInvariant(), isDeterministic: true);
    }
}
