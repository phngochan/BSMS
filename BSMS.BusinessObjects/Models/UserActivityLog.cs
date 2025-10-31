namespace BSMS.BusinessObjects.Models;
public class UserActivityLog
{
    public int LogId { get; set; }
    public int UserId { get; set; }
    public string Action { get; set; }
    public string IpAddress { get; set; }
    public DateTime CreatedAt { get; set; }

    public User User { get; set; }
}
