namespace HemSoft.EggIncTracker.Data;

using Microsoft.EntityFrameworkCore;
using HemSoft.EggIncTracker.Data.Dtos;

public class EggIncContext : DbContext
{
    public DbSet<PlayerDto> Players { get; set; } = null!;

    override protected void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer("Server=tcp:db-srv-egg.database.windows.net,1433;Initial Catalog=db-egg;Persist Security Info=False;User ID=CloudSA2a0f96c3;Password=YzRAgDcGG3aRE&XRBJ;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;");
    }
}
