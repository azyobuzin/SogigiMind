using Microsoft.EntityFrameworkCore;

namespace SogigiMind.Data
{
    public class ApplicationDbContext : DbContext
    {
#pragma warning disable CS8618 // Null 非許容フィールドは初期化されていません。null 許容として宣言することを検討してください。
        public ApplicationDbContext(DbContextOptions options)
#pragma warning restore CS8618 // Null 非許容フィールドは初期化されていません。null 許容として宣言することを検討してください。
            : base(options)
        {
        }

        public DbSet<Blob> Blobs { get; set; }
    }
}
