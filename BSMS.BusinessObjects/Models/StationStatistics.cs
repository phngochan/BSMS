namespace BSMS.BusinessObjects.Models;
public class StationStatistics
{
    public int StatId { get; set; }
    public int StationId { get; set; }
    public DateTime Date { get; set; }
    public int TotalSwaps { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AvgRating { get; set; }
    public int DefectiveBatteries { get; set; }
    public DateTime CreatedAt { get; set; }

    public ChangingStation Station { get; set; }
}
