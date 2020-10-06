using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SogigiMind.Services
{
    public class ThumbnailService2 : IThumbnailService
    {
        public Task<IReadOnlyList<ThumbnailInfo>> CreateThumbnailAsync(string url, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
