using System.ComponentModel.DataAnnotations;

namespace BSMS.WebApp.ViewModels;

public class VehicleCreateViewModel
{
    [Required(ErrorMessage = "Chọn chủ xe là bắt buộc")]
    [Display(Name = "Chủ Xe")]
    public int UserId { get; set; }

    [Required(ErrorMessage = "VIN là bắt buộc")]
    [Display(Name = "VIN (Số khung)")]
    public string Vin { get; set; } = string.Empty;

    [Required(ErrorMessage = "Loại pin là bắt buộc")]
    [Display(Name = "Loại Pin")]
    public string BatteryModel { get; set; } = string.Empty;

    [Required(ErrorMessage = "Kiểu pin là bắt buộc")]
    [Display(Name = "Kiểu Pin")]
    public string BatteryType { get; set; } = string.Empty;
}

