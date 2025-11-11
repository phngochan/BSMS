using System.ComponentModel.DataAnnotations;

namespace BSMS.BusinessObjects.Enums;
public enum PaymentStatus
{
    [Display(Name = "Đã thanh toán")] Paid,
    [Display(Name = "Chờ xử lý")] Pending,
    [Display(Name = "Thất bại")] Failed
}

