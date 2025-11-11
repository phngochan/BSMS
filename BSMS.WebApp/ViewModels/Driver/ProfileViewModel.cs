namespace BSMS.WebApp.ViewModels.Driver;

public class ProfileViewModel
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    
    public List<VehicleInfoViewModel> Vehicles { get; set; } = new();
    public List<SwapTransactionViewModel> SwapTransactions { get; set; } = new();
    public int TotalSwaps { get; set; }
    public int CompletedSwaps { get; set; }
    public decimal TotalSpent { get; set; }
}

public class VehicleInfoViewModel
{
    public int VehicleId { get; set; }
    public string Vin { get; set; } = string.Empty;
    public string BatteryModel { get; set; } = string.Empty;
    public string BatteryType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class SwapTransactionViewModel
{
    public int TransactionId { get; set; }
    public DateTime SwapTime { get; set; }
    public string StationName { get; set; } = string.Empty;
    public string StationAddress { get; set; } = string.Empty;
    public string VehicleVin { get; set; } = string.Empty;
    public string BatteryTakenModel { get; set; } = string.Empty;
    public string BatteryTakenSerialNumber { get; set; } = string.Empty;
    public string? BatteryReturnedModel { get; set; }
    public string? BatteryReturnedSerialNumber { get; set; }
    public decimal TotalCost { get; set; }
    public string Status { get; set; } = string.Empty;
}

