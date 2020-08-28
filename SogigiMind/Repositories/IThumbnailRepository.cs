using System.Threading.Tasks;

namespace SogigiMind.Repositories
{
    public interface IThumbnailRepository
    {
        /// <summary>
        /// サムネイルをダウンロードします。 <paramref name="key"/> に該当するデータが存在しない場合は <see langword="null"/> を返します。
        /// </summary>
        Task<byte[]?> DownloadAsBytesAsync(string key);

        Task UploadAsync(string key, byte[] content, string url, string contentType);
    }
}
