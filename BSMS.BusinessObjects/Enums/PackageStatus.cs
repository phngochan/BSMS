using System.ComponentModel.DataAnnotations;

namespace BSMS.BusinessObjects.Enums;
public enum PackageStatus
{
    [Display(Name = "Đang hoạt động")] Active,
    [Display(Name = "Đã hết hạn")] Expired,
    [Display(Name = "Đã hủy")] Cancelled
}

