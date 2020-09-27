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

        public DbSet<BlobData> Blobs { get; set; }

        public DbSet<EndUserData> EndUsers { get; set; }

        public DbSet<EstimationLogData> EstimationLogs { get; set; }

        public DbSet<FetchAttemptData> FetchAttempts { get; set; }

        public DbSet<PersonalSensitivityData> PersonalSensitivities { get; set; }

        public DbSet<RemoteImageData> RemoteImages { get; set; }

        public DbSet<ThumbnailData> Thumbnails { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<EstimationLogData>().HasIndex(x => x.InsertedAt);

            modelBuilder.Entity<FetchAttemptData>().HasIndex(x => x.RemoteImageId);

            modelBuilder.Entity<PersonalSensitivityData>().HasKey(
                nameof(PersonalSensitivityData.UserId),
                nameof(PersonalSensitivityData.RemoteImageId));

            modelBuilder.Entity<RemoteImageData>().HasIndex(x => x.Url).IsUnique();

            modelBuilder.Entity<ThumbnailData>().HasIndex(x => x.FetchAttemptId);
        }
    }
}
