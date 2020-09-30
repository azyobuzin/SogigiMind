using SogigiMind.Data;

namespace SogigiMind.Services
{
    /// <summary>
    /// すでにインスタンス化されている <see cref="ApplicationDbContext"/> を使用して <see cref="IBlobService"/> を作成するためのファクトリ
    /// </summary>
    public interface IBlobServiceFactory
    {
        IBlobService CreateBlobService(ApplicationDbContext dbContext);
    }
}
