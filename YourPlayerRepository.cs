using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace YourProject.Services
{
    public class YourPlayerRepository
    {
        private readonly YourDbContext _context;

        public YourPlayerRepository(YourDbContext context)
        {
            _context = context;
        }

        public async Task<Player> GetLatestPlayerByNameAsync(string name)
        {
            return await _context.Players
                .Where(p => p.PlayerName == name)
                .OrderByDescending(p => p.Updated)
                .FirstOrDefaultAsync();
        }

        // Add other repository methods here
    }
} 