using BSMS.BusinessObjects.Models;
using BSMS.DAL.Base;

namespace BSMS.DAL.Repositories;

public interface IVehicleRepository : IGenericRepository<Vehicle>
{
    Task<IEnumerable<Vehicle>> GetVehiclesByUserIdAsync(int userId);
    Task<Vehicle?> GetVehicleByVinAsync(string vin);
    Task<bool> IsVinExistsAsync(string vin, int? excludeVehicleId = null);
}

