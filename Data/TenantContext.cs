namespace MiniLoyalty.Data;

/// <summary>Ngữ cảnh tenant của request hiện tại. Middleware set OrgId (từ X-Api-Key), DbContext đọc để lọc.</summary>
public interface ITenantContext
{
    Guid OrgId { get; set; }
}

public sealed class TenantContext : ITenantContext
{
    /// <summary>Org mặc định (dữ liệu seed + UI không kèm ApiKey). Cố định để ổn định qua các lần khởi động.</summary>
    public static readonly Guid DefaultOrgId = new("11111111-1111-1111-1111-111111111111");
    public const string DefaultApiKey = "demo-loyalty";

    public Guid OrgId { get; set; } = DefaultOrgId;
}
