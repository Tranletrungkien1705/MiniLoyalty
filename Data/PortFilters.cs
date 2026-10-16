using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace MiniLoyalty.Data;

/// <summary>
/// Hạ tầng dùng chung cho MỌI controller port 1:1 (namespace <c>MiniLoyalty.Controllers.Port</c>) — áp dụng ở MỘT chỗ thay vì sửa lẻ từng controller.
/// (1) Tên trường JSON giữ PascalCase như nguồn: gate nguồn serialize bằng <c>JavaScriptSerializer</c>/Newtonsoft (PascalCase: Success, Data, MyIdxSeq...),
///     trong khi .NET 8 mặc định camelCase. Chỉ áp cho controller port nên SPA và các endpoint cũ (/api/v1/*, /api/earn...) KHÔNG bị ảnh hưởng
///     (cùng cách MiniGate đã làm — labs-audit/AGENT_MiniGate.md:97). Chứng cứ lỗi: QA-qa1-5.
/// (2) Ngoại lệ không bắt: nguồn bắt MỌI ngoại lệ ở gate và trả <c>Success=false</c> (không bao giờ HTTP 500); filter này trả
///     <c>PortResult.Fail("&lt;Controller&gt;_&lt;Action&gt;", "Exception", ex.Message)</c> (400) cho mọi action port chưa tự bắt. Chứng cứ lỗi: QA-qa1-4, QA-qa2-1.
/// </summary>
public static class PortJson
{
    public const string PortNamespace = "MiniLoyalty.Controllers.Port";

    // PropertyNamingPolicy = null => giữ nguyên tên thuộc tính C# (PascalCase). Encoder nới lỏng: không escape ký tự tiếng Việt.
    public static readonly SystemTextJsonOutputFormatter Formatter = new(new JsonSerializerOptions
    {
        PropertyNamingPolicy = null,
        DictionaryKeyPolicy = null,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver(),   // bat buoc khi tu tao options cho formatter
    });

    public static bool IsPort(ActionDescriptor? d) =>
        d is ControllerActionDescriptor cad && cad.ControllerTypeInfo.Namespace == PortNamespace;

    public static void Apply(ObjectResult r)
    {
        r.Formatters.Clear();
        r.Formatters.Add(Formatter);
    }
}

/// <summary>Result filter: ép PascalCase cho ObjectResult của action port.</summary>
public sealed class PortJsonResultFilter : IResultFilter
{
    public void OnResultExecuting(ResultExecutingContext context)
    {
        if (PortJson.IsPort(context.ActionDescriptor) && context.Result is ObjectResult o) PortJson.Apply(o);
    }

    public void OnResultExecuted(ResultExecutedContext context) { }
}

/// <summary>Exception filter: ngoại lệ chưa bắt trong action port → 400 PortResult.Fail (không để thành HTTP 500).</summary>
public sealed class PortExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.ExceptionHandled || !PortJson.IsPort(context.ActionDescriptor)) return;
        var cad = (ControllerActionDescriptor)context.ActionDescriptor;
        // Mã lỗi kiểu nguồn: "<Controller bỏ hậu tố Controller/Port>_<Action>", vd Mst_Department_Search.
        var ctrl = cad.ControllerTypeInfo.Name.Replace("ControllerPort", "").Replace("Controller", "");
        var res = PortResult.Fail($"{ctrl}_{cad.ActionName}", "Exception", context.Exception.Message);
        PortJson.Apply(res);   // result filter KHÔNG chạy khi exception filter xử lý ngoại lệ → tự áp PascalCase
        context.Result = res;
        context.ExceptionHandled = true;
    }
}
