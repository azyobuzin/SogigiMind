using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SogigiMind.Data;

namespace SogigiMind.UseCases.Sensitivity
{
    public class DeletePersonalSensitivitiesUseCase
    {
        private readonly ApplicationDbContext _dbContext;

        public DeletePersonalSensitivitiesUseCase(ApplicationDbContext dbContext)
        {
            this._dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task ExecuteAsync(string acct)
        {
            var personalSensitivities = await this._dbContext.PersonalSensitivities
                .Where(x => x.User.Acct == acct)
                .ToListAsync().ConfigureAwait(false);
            this._dbContext.RemoveRange(personalSensitivities);
            await this._dbContext.SaveChangesAsync().ConfigureAwait(false);
        }
    }
}
