using System.ComponentModel.DataAnnotations;
using BSMS.BusinessObjects.Enums;

namespace BSMS.WebApp.Pages.Admin.Operations;

public class StationInput
{
    public int StationId { get; set; }

    [Required, StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(250)]
    public string Address { get; set; } = string.Empty;

    [Range(-90, 90)]
    public double Latitude { get; set; }

    [Range(-180, 180)]
    public double Longitude { get; set; }

    [Range(1, 1000)]
    public int Capacity { get; set; }

    [Required]
    public StationStatus Status { get; set; } = StationStatus.Active;
}
