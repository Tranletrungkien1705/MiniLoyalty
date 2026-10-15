namespace MiniLoyalty.Data;

/// <summary>
/// Helper dùng chung cho các endpoint port 1:1 — để mọi file `Controllers/Port/*` so chuỗi ĐÚNG một cách.
/// Nguồn chạy SQL Server collation KHÔNG phân biệt hoa/thường (`CmUtils.SqlUtils.BuildWhere`). Ở đích:
///  - `==` / `.Contains()` trên chuỗi PHÂN BIỆT hoa/thường trên SQLite (`instr`) lẫn Postgres ⇒ trả 0 dòng câm (bẫy #18, #31).
///  - Vì vậy hạ chữ CẢ HAI phía rồi `EF.Functions.Like(cot.ToLower(), mau, PortQuery.Esc)`.
/// Cách dùng:
///  - nguồn `like '%x%'`  ⇒ `EF.Functions.Like((x.Cot ?? "").ToLower(), PortQuery.Contains(v), PortQuery.Esc)`
///  - nguồn `= 'x'`       ⇒ `EF.Functions.Like((x.Cot ?? "").ToLower(), PortQuery.Equal(v), PortQuery.Esc)` (KHÔNG wildcard)
/// Ký tự `%` `_` `\` người dùng gõ vào được escape nên không biến thành ký tự đại diện.
/// </summary>
public static class PortQuery
{
    /// <summary>Ký tự escape truyền cho `LIKE … ESCAPE '\'`.</summary>
    public const string Esc = "\\";

    /// <summary>Mẫu "chứa" — port ngữ nghĩa `like '%x%'` của nguồn.</summary>
    public static string Contains(string value) => "%" + EscapeLike(value.Trim().ToLowerInvariant()) + "%";

    /// <summary>Mẫu "bằng" không phân biệt hoa/thường — port ngữ nghĩa `= 'x'` của nguồn.</summary>
    public static string Equal(string value) => EscapeLike(value.Trim().ToLowerInvariant());

    private static string EscapeLike(string s) => s.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}

/// <summary>
/// Khuôn trả về của các action Search ở tầng gate nguồn (`TModelsUI.PageInfo&lt;T&gt;`, vd `Mst_OrgController.cs:48-90`):
/// `DataList` + `ItemCount` (= MyCount của khối "---- Summary:") + `PageCount` + `PageIndex` + `PageSize`.
/// </summary>
public record PageInfo<T>(IReadOnlyList<T> DataList, int ItemCount, int PageCount, int PageIndex, int PageSize);
