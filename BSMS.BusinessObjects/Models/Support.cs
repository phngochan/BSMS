using BSMS.BusinessObjects.Enums;

namespace BSMS.BusinessObjects.Models;
public class Support
{
    public int SupportId { get; set; }
    public int UserId { get; set; }
    public int StationId { get; set; }
    public SupportType Type { get; set; }
    public string Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public SupportStatus Status { get; set; } = SupportStatus.Open;
    public int? Rating { get; set; }
    public string? StaffNote { get; set; }

    public User User { get; set; }
    public ChangingStation Station { get; set; }
}
