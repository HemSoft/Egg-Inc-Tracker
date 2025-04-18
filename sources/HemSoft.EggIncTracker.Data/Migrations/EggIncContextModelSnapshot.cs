// <auto-generated />
using System;
using HemSoft.EggIncTracker.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace HemSoft.EggIncTracker.Data.Migrations
{
    [DbContext(typeof(EggIncContext))]
    partial class EggIncContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.5")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("HemSoft.EggIncTracker.Data.Dtos.PlayerDto", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<int>("CraftingLevel")
                        .HasColumnType("int");

                    b.Property<string>("EID")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("EarningsBonusPerHour")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("EarningsBonusPercentage")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<float>("ExpectedLegendaries")
                        .HasColumnType("real");

                    b.Property<float>("ExpectedLegendaryCrafts")
                        .HasColumnType("real");

                    b.Property<float>("ExpectedLegendaryDropsFromShips")
                        .HasColumnType("real");

                    b.Property<float>("HoarderScore")
                        .HasColumnType("real");

                    b.Property<float>("JER")
                        .HasColumnType("real");

                    b.Property<float>("LLC")
                        .HasColumnType("real");

                    b.Property<float>("MER")
                        .HasColumnType("real");

                    b.Property<string>("NextTitle")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("PiggyConsumeValue")
                        .HasColumnType("int");

                    b.Property<float>("PlayerLegendaries")
                        .HasColumnType("real");

                    b.Property<float>("PlayerLegendariesExcludingLunarTotem")
                        .HasColumnType("real");

                    b.Property<string>("PlayerName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("ProjectedTitleChange")
                        .HasColumnType("datetime2");

                    b.Property<int>("ProphecyEggs")
                        .HasColumnType("int");

                    b.Property<float>("ShipLaunchPoints")
                        .HasColumnType("real");

                    b.Property<string>("SoulEggs")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("SoulEggsFull")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<double>("TitleProgress")
                        .HasColumnType("float");

                    b.Property<int>("TotalCraftsThatCanBeLegendary")
                        .HasColumnType("int");

                    b.Property<DateTime>("Updated")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.ToTable("Players");
                });
#pragma warning restore 612, 618
        }
    }
}
