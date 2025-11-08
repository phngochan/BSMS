using BSMS.BusinessObjects.Models;
using BSMS.DAL.Base;

namespace BSMS.DAL.Repositories;

public interface IConfigRepository : IGenericRepository<Config>
{
    Task<Config?> GetByNameAsync(string name);
}
