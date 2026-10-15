using System.Reflection;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;

namespace MiniLoyalty.Data;

/// <summary>
/// Hạ tầng xuất Excel dùng chung cho các action Export/ExportTemplate port 1:1.
/// Nguồn (tầng gate): `BaseController.GenExcelExportFilePath` (`BaseController.cs:772-790`) +
/// `ExcelExport.ExportToExcelFromList(list, dicColNames, filePath, sheetName)` + trả `Url = GetRequestBaseAddress() + url` (`:403-410`).
/// - Đường dẫn ảo giữ ĐÚNG mẫu nguồn: `/TempFiles/yyyy-MM-dd/{prefix}_{yyMMddHHmmss}[_i].xlsx`, vật lý nằm dưới `wwwroot`.
/// - Bảng tính: dòng 1 = tiêu đề theo THỨ TỰ của từ điển cột (giá trị = nhãn tiếng Việt), từ dòng 2 = dữ liệu.
///   `ExcelExport` của nguồn nằm trong DLL không có mã ⇒ định dạng ô (độ rộng, kiểu số/ngày) CẦN XÁC NHẬN khi có file mẫu thật.
/// </summary>
public static class PortExcel
{
    /// <summary>Port `GenExcelExportFilePath(prefix, ref virualPath)` — trả (đường dẫn vật lý, đường dẫn ảo).</summary>
    public static (string physicalPath, string virtualPath) GenExcelExportFilePath(string webRootPath, string prefix)
    {
        var subpath = $"/TempFiles/{DateTime.Now:yyyy-MM-dd}";
        var filename = $"{prefix}_{DateTime.Now:yyMMddHHmmss}";
        var dir = Path.Combine(webRootPath, subpath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(dir);
        var virtualPath = $"{subpath}/{filename}.xlsx";
        var physical = Path.Combine(dir, filename + ".xlsx");
        var i = 0;
        while (File.Exists(physical))
        {
            i++;
            virtualPath = $"{subpath}/{filename}_{i}.xlsx";
            physical = Path.Combine(dir, $"{filename}_{i}.xlsx");
        }
        return (physical, virtualPath);
    }

    /// <summary>
    /// Ghi danh sách ra 1 sheet. <paramref name="dicColNames"/>: khoá = tên thuộc tính (không phân biệt hoa/thường),
    /// giá trị = nhãn cột. Thuộc tính không tồn tại trên phần tử ⇒ ô rỗng (không ném lỗi).
    /// </summary>
    public static void ExportToExcelFromList<T>(IEnumerable<T> rows, IReadOnlyDictionary<string, string> dicColNames, string filePath, string sheetName)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add(string.IsNullOrWhiteSpace(sheetName) ? "Sheet1" : sheetName);
        var cols = dicColNames.ToList();
        for (var c = 0; c < cols.Count; c++) ws.Cell(1, c + 1).Value = cols[c].Value;
        var r = 2;
        foreach (var row in rows)
        {
            if (row is null) continue;
            var t = row.GetType();
            for (var c = 0; c < cols.Count; c++)
            {
                var p = t.GetProperty(cols[c].Key, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                var v = p?.GetValue(row);
                ws.Cell(r, c + 1).Value = v switch
                {
                    null => Blank.Value,
                    DateTime d => d,
                    double d => d,
                    int i => i,
                    long l => l,
                    decimal m => m,
                    _ => Convert.ToString(v) ?? "",
                };
            }
            r++;
        }
        ws.Columns().AdjustToContents();
        wb.SaveAs(filePath);
    }

    /// <summary>
    /// Port `ExcelImportNew.Query(filePath, "A2")` của các action Import (vd `Mst_ProvinceController.cs` Import).
    /// DLL nguồn không có mã ⇒ GIẢ ĐỊNH (cần xác nhận với file mẫu thật): dòng 1 = tiêu đề (đúng file `ExportTemplate` sinh ra),
    /// dữ liệu bắt đầu từ ô bắt đầu (mặc định A2), cột ánh xạ THEO VỊ TRÍ vào khoá của từ điển mẫu Import
    /// (nguồn kiểm `table.Columns.Count != GetImportDicColumsTemplate().Count` rồi đọc `dr[TblX.Cot]`).
    /// Trả về: số cột đọc được ở dòng tiêu đề + danh sách dòng (bỏ qua dòng trống hoàn toàn).
    /// </summary>
    public static (int columnCount, List<Dictionary<string, string?>> rows) ReadSheet(Stream xlsx, IReadOnlyList<string> keysInOrder, int firstDataRow = 2)
    {
        using var wb = new XLWorkbook(xlsx);
        var ws = wb.Worksheets.First();
        var lastCol = ws.Row(Math.Max(1, firstDataRow - 1)).LastCellUsed()?.Address.ColumnNumber ?? 0;
        var rows = new List<Dictionary<string, string?>>();
        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 0;
        for (var r = firstDataRow; r <= lastRow; r++)
        {
            var row = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            var any = false;
            for (var c = 0; c < keysInOrder.Count; c++)
            {
                var s = ws.Cell(r, c + 1).GetString();
                if (!string.IsNullOrWhiteSpace(s)) any = true;
                row[keysInOrder[c]] = string.IsNullOrEmpty(s) ? null : s;
            }
            if (any) rows.Add(row);
        }
        return (lastCol, rows);
    }

    /// <summary>Port `GetRequestBaseAddress()` (`BaseController.cs:403-410`): scheme://host[:port không mặc định].</summary>
    public static string GetRequestBaseAddress(HttpRequest req)
    {
        var defaultPort = req.IsHttps ? 443 : 80;
        var port = req.Host.Port;
        return $"{req.Scheme}://{req.Host.Host}" + (port.HasValue && port.Value != defaultPort ? ":" + port.Value : "");
    }
}
