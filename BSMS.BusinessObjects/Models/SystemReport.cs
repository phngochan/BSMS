using BSMS.BusinessObjects.Enums;

namespace BSMS.BusinessObjects.Models;
public class SystemReport
{
    public int ReportId { get; set; }
    public string ReportName { get; set; }
    public ReportType ReportType { get; set; }
    public int GeneratedBy { get; set; }
    public string FilePath { get; set; }
    public DateTime CreatedAt { get; set; }

    public User GeneratedByUser { get; set; }
}

