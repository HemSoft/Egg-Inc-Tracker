namespace HemSoft.EggIncTracker.Data.Dtos;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;

using Newtonsoft.Json;

public class MajPlayerRankingDto
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int Ranking { get; set; }

    [Required]
    [Column(TypeName = "nvarchar(100)")]
    public string IGN { get; set; }

    [Column(TypeName = "nvarchar(100)")]
    public string DiscordName { get; set; }

    [Column(TypeName = "nvarchar(50)")]
    public string EBString { get; set; }

    [Column(TypeName = "nvarchar(50)")]
    public string Role { get; set; }

    [Column(TypeName = "decimal(38, 2)")]
    public decimal SENumber { get; set; }

    [Column(TypeName = "nvarchar(20)")]
    public string SEString { get; set; }

    [Column("PE", TypeName = "int")]
    public int PE { get; set; }

    [Column("Prestiges", TypeName = "nvarchar(20)")]
    public string? Prestiges { get; set; }

    [Column("MER", TypeName = "decimal(6, 3)")]
    public decimal MER { get; set; }

    [Column("JER", TypeName = "decimal(6, 3)")]
    public decimal JER { get; set; }

    public DateTime Updated { get; set; } = DateTime.UtcNow;

    public static List<MajPlayerRankingDto> ApiToMajPlayerRankings(string apiResponse)
    {
        try
        {
            // Step 1: Deserialize into the Google Sheets response structure
            var sheetsResponse = JsonConvert.DeserializeObject<GoogleSheetsResponse>(apiResponse);

            // Step 2: Skip the header row (first row) and process data rows
            var dataRows = sheetsResponse.Values.Skip(1); // Skip header row

            // Step 3: Map each row to MajPlayerRankingDto
            var rankings = new List<MajPlayerRankingDto>();
            foreach (var row in dataRows)
            {
                // Ensure row has enough elements
                if (row == null || row.Count < 10)
                {
                    // Skip.
                    continue;
                }

                var dto = new MajPlayerRankingDto();
                dto.Ranking = int.Parse(row[0].Split('.')[0]); // Extract number before the dot
                dto.IGN = row[0].Substring(row[0].IndexOf('.') + 2);
                dto.DiscordName = row[1];
                dto.EBString = row[2];
                dto.Role = row[3];
                dto.SENumber = decimal.Parse(row[4], System.Globalization.NumberStyles.Any);
                dto.SEString = row[5];
                dto.PE = int.Parse(row[6]);
                dto.Prestiges = row[7] == "-" ? null : row[7]; // Handle "-" as null or empty
                dto.MER = decimal.Parse(row[8]);
                dto.JER = decimal.Parse(row[9]);
                dto.Updated = DateTime.UtcNow;

                rankings.Add(dto);
            }

            return rankings;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing row. Exception: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            throw;
        }
    }
    public class GoogleSheetsResponse
    {
        public string Range { get; set; }
        public string MajorDimension { get; set; }
        public List<List<string>> Values { get; set; }
    }
}
