using SogigiMind.Data;

namespace SogigiMind.Services
{
    public class DefaultBlobServiceFactory : IBlobServiceFactory
    {
        public IBlobService CreateBlobService(ApplicationDbContext dbContext)
        {
            return new DefaultBlobService(dbContext);
        }
    }
}
