using System.ComponentModel.DataAnnotations;

namespace BSMS.BusinessObjects.Enums;
public enum PaymentMethod
{
    [Display(Name = "Tiền mặt")] Cash,
    [Display(Name = "Thẻ tín dụng")] CreditCard,
    [Display(Name = "Chuyển khoản")] BankTransfer,
    [Display(Name = "Ví điện tử")] EWallet
}
