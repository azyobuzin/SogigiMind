using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using SogigiMind.Logics;
using SogigiMind.Services;

namespace SogigiMind.UseCases.Thumbnail
{
    public class ThumbnailUseCase
    {
        private readonly IThumbnailQueueProducer _thumbnailQueueProducer;

        public ThumbnailUseCase(IThumbnailQueueProducer thumbnailQueueProducer)
        {
            this._thumbnailQueueProducer = thumbnailQueueProducer ?? throw new ArgumentNullException(nameof(thumbnailQueueProducer));
        }

        public Task<ThumbnailUseCaseOutput> ExecuteAsync(string urlBase64, string? signature)
        {
            var url = UrlNormalizer.NormalizeUrl(Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(urlBase64)));
            // TODO: options でキーを設定できるようにする
            throw new NotImplementedException();
        }
    }

    public class ThumbnailUseCaseOutput
    {
        public ThumbnailUseCaseOutputKind Kind { get; }

        public UploadedBlobInfo? Blob { get; }

        public ThumbnailUseCaseOutput(ThumbnailUseCaseOutputKind kind, UploadedBlobInfo? blob)
        {
            this.Kind = kind;
            this.Blob = blob;
        }
    }

    public enum ThumbnailUseCaseOutputKind
    {
        ReturnThumbnail,
        UseOriginal,
        InvalidSignature,
    }
}
