namespace MiniLoyalty.Data;

// ==========================================================================================
// Hạ tầng cho port 1:1 từ 2022.7.Loyalty khi NHIỀU hạng mục được làm song song.
// Mục tiêu: mỗi bảng/màn nằm trong file RIÊNG, không phải sửa file dùng chung (AppDbContext, Seeder) ⇒ hết xung đột merge.
//  - Entity: đặt trong namespace `MiniLoyalty.Models.Port`, cấu hình bằng `IEntityTypeConfiguration<T>`
//    trong cùng file — `AppDbContext` tự quét bằng `ApplyConfigurationsFromAssembly`. Không cần thêm `DbSet`, dùng `db.Set<T>()`.
//  - Bảng/cột: `Seeder.EnsureNewTablesAsync` tự `CREATE TABLE IF NOT EXISTS` và `ADD COLUMN` cột còn thiếu
//    cho mọi entity trong namespace đó (bẫy #28: `EnsureCreated` là no-op khi DB đã tồn tại).
//  - Dữ liệu mẫu: class implement `IPortSeeder` — `Seeder` tự tìm và gọi theo `Order` (danh mục trước, nghiệp vụ sau).
// ==========================================================================================

/// <summary>Seeder dữ liệu mẫu của một module port. Phải idempotent (kiểm tra đã có dữ liệu trước khi thêm).</summary>
public interface IPortSeeder
{
    /// <summary>Thứ tự chạy: danh mục Mst_* dùng 10–99, Map_/Sys_ 100–199, nghiệp vụ Crd_/Prm_ từ 200.</summary>
    int Order => 100;

    Task SeedAsync(AppDbContext db);
}
