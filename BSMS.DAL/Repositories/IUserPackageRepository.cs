using BSMS.BusinessObjects.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSMS.DAL.Repositories
{
    public interface IUserPackageRepository
    {
        Task<List<UserPackage>> GetExpiringSoonAsync(int daysBefore = 3);
        Task<List<UserPackage>> GetExpiredAsync();
        Task UpdateAsync(UserPackage package);
    }
}
