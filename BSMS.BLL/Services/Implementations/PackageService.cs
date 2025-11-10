using BSMS.BusinessObjects.Models;
using BSMS.DAL.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSMS.BLL.Services.Implementations
{
    public class PackageService : IPackageService
    {
        private readonly IPackageRepository _repo;
        public PackageService(IPackageRepository repo) => _repo = repo;

        public async Task<List<BatteryServicePackage>> GetAllAsync() => await _repo.GetAllAsync();
        public async Task<BatteryServicePackage?> GetByIdAsync(int id) => await _repo.GetByIdAsync(id);

        public async Task CreateAsync(BatteryServicePackage package)
        {
            if (package.Price <= 0 || package.DurationDays <= 0)
                throw new ArgumentException("Giá và thời hạn phải > 0");
            await _repo.AddAsync(package);
        }

        public async Task UpdateAsync(BatteryServicePackage package)
        {
            if (package.Price <= 0 || package.DurationDays <= 0)
                throw new ArgumentException("Giá và thời hạn phải > 0");
            await _repo.UpdateAsync(package);
        }

        public async Task DeleteAsync(int id)
        {
            var pkg = await _repo.GetByIdAsync(id);
            if (pkg == null) throw new KeyNotFoundException("Không tìm thấy gói");
            await _repo.DeleteAsync(id);
        }
    }
}
