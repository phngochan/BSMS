using BSMS.BusinessObjects.Enums;

namespace BSMS.BusinessObjects.Models;
public class Payment
{
    public int PaymentId { get; set; }
    public int UserId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentTime { get; set; }
    public PaymentMethod Method { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public string InvoiceUrl { get; set; }

    public User User { get; set; }
    public ICollection<SwapTransaction> SwapTransactions { get; set; }
}
