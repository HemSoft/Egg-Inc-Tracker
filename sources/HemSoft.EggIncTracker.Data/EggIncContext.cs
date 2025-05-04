namespace HemSoft.EggIncTracker.Data;

using Microsoft.EntityFrameworkCore;
using HemSoft.EggIncTracker.Data.Dtos;
using Microsoft.Extensions.Configuration;

public class EggIncContext : DbContext
{
    public DbSet<PlayerDto> Players { get; set; } = null!;
    public DbSet<EventDto> Events { get; set; } = null!;
    public DbSet<ContractDto> Contracts { get; set; } = null!;
    public DbSet<PlayerContractDto> PlayerContracts { get; set; } = null!;
    public DbSet<GoalDto> Goals { get; set; } = null!;
    public DbSet<MajPlayerRankingDto> MajPlayerRankings { get; set; } = null!;
    public virtual DbSet<PlayerStatsDto> PlayerRankings { get; set; } = null!;
    public DbSet<MissionDto> Missions { get; set; } = null!;

    // Default constructor for legacy/standalone usage
    public EggIncContext() { }

    // Constructor for DI
    public EggIncContext(DbContextOptions<EggIncContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Mark the PlayerRankingResult as a keyless entity since it's for stored procedure results
        modelBuilder.Entity<PlayerStatsDto>().HasNoKey();
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Skip if options are already configured (means options were passed via constructor)
        if (optionsBuilder.IsConfigured)
            return;

        // Fallback for backwards compatibility
        optionsBuilder.UseSqlServer("Data Source=localhost;Initial Catalog=db-egginc;Integrated Security=True;Encrypt=False;Trust Server Certificate=True");
    }
}
