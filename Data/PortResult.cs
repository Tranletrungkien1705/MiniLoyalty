using Microsoft.AspNetCore.Mvc;

namespace MiniLoyalty.Data;

/// <summary>
/// Khuôn trả LỖI nghiệp vụ dùng chung cho các endpoint port 1:1.
/// Nguồn ném `CmUtils.CMyException.Raise(TError.ErridnLoyalty.&lt;Mã&gt;, null, alParamsCoupleError)` rồi gate trả
/// `Success = false` (vd `Mst_DealerCPController.cs:510-514`). Ở đích: HTTP 400 + đúng MÃ LỖI nguồn + cặp tham số kiểm tra,
/// để client và người kiểm đối chiếu được 1:1 với `TError.ErridnLoyalty.*`.
/// </summary>
public static class PortResult
{
    /// <summary>
    /// Trả 400 với mã lỗi nguồn (không kèm tiền tố, vd "Mst_DealerCP_CheckDB_DLExist") và các cặp "tên", giá trị
    /// giống `alParamsCoupleError` của nguồn (vd "Check.DLCPCode", "DL01").
    /// </summary>
    public static BadRequestObjectResult Fail(string errorCode, params object?[] paramCouples)
    {
        var p = new Dictionary<string, object?>();
        for (var i = 0; i + 1 < paramCouples.Length; i += 2)
            p[Convert.ToString(paramCouples[i]) ?? $"p{i}"] = paramCouples[i + 1];
        return new BadRequestObjectResult(new { Success = false, ErrorCode = "ErridnLoyalty." + errorCode, Params = p });
    }
}
