// BLL/Services/Implementations/UserPackageService.cs
using BSMS.BLL.Services;
using BSMS.BusinessObjects.Enums;
using BSMS.BusinessObjects.Models;
using BSMS.DAL.Base;
using BSMS.DAL.Context;
using BSMS.DAL.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

public class UserPackageService : IUserPackageService
{
    private readonly IPackageRepository _pkgRepo;
    private readonly IGenericRepository<UserPackage> _upRepo;
    private readonly BSMSDbContext _context;

    public UserPackageService(
        IPackageRepository pkgRepo,
        IGenericRepository<UserPackage> upRepo,
        BSMSDbContext context)
    {
        _pkgRepo = pkgRepo;
        _upRepo = upRepo;
        _context = context;
    }

    public async Task<List<BatteryServicePackage>> GetAvailablePackagesAsync()
        => await _pkgRepo.GetActivePackagesAsync();

    public async Task<List<BatteryServicePackage>> GetAllPackagesAsync()
     => await _pkgRepo.GetAllPackagesAsync();

    public async Task<UserPackage?> GetActivePackageAsync(int userId)
    {
        return await _upRepo.GetAllAsync(
            filter: up => up.UserId == userId &&
                         up.Status == PackageStatus.Active &&
                         up.EndDate >= DateTime.Today,
            includes: new Expression<Func<UserPackage, object>>[] { up => up.Package! }
        ).ContinueWith(t => t.Result.FirstOrDefault());
    }

    public async Task CreateUserPackageAsync(int userId, int packageId, int paymentId)
    {
        var pkg = await _context.BatteryServicePackages.FindAsync(packageId);

        // 2. Phải kiểm tra kết quả SAU KHI await
        if (pkg == null)
        {
            throw new ArgumentException("Gói dịch vụ không tồn tại");
        }

        var userPkg = new UserPackage
        {
            UserId = userId,
            PackageId = packageId,
            StartDate = DateTime.Now,
            EndDate = DateTime.Now.AddDays(pkg.DurationDays),

            Status = PackageStatus.Active,

        };

        await _upRepo.CreateAsync(userPkg);

        // Cập nhật Payment
        var payment = await _context.Payments.FindAsync(paymentId);
        if (payment != null)
        {
            payment.Status = PaymentStatus.Paid;
            payment.PaymentTime = DateTime.Now;
            await _context.SaveChangesAsync();
        }
    }

    // UserPackageService.cs
    // BLL/Services/Implementations/UserPackageService.cs
    public async Task<bool> CanSwapBatteryAsync(int userId, int vehicleId, int stationId, int batteryTakenId, int batteryReturnedId)
    {
        var activePkg = await GetActivePackageAsync(userId);
        if (activePkg == null) return false;

        // TÍNH SwapLimit = DurationDays / 3
        int swapLimit = activePkg.Package.DurationDays / 3;

        // TẠO GIAO DỊCH → LÚC NÀY MỚI GIẢM LƯỢT (tăng usedSwaps)
        var swap = new SwapTransaction
        {
            UserId = userId,
            VehicleId = vehicleId,
            StationId = stationId,
            BatteryTakenId = batteryTakenId,
            BatteryReturnedId = batteryReturnedId,
            SwapTime = DateTime.Now,
            TotalCost = 0,
            Status = SwapStatus.Completed
        };
        return true;
    }

    public async Task<UserPackage?> GetCurrentPackageAsync(int userId)
    {
        return await _upRepo.GetAllAsync(
            filter: up => up.UserId == userId,
            includes: new Expression<Func<UserPackage, object>>[] { up => up.Package! },
            orderBy: q => q.OrderByDescending(up => up.StartDate)
        ).ContinueWith(t => t.Result.FirstOrDefault());
    }

    public async Task ExpirePackagesAsync()
    {
        var expiredPackages = await _upRepo.GetAllAsync(
            filter: up => up.Status == PackageStatus.Active && up.EndDate < DateTime.Today
        );

        foreach (var up in expiredPackages)
        {
            up.Status = PackageStatus.Expired;
        }

        await Task.WhenAll(expiredPackages.Select(_upRepo.UpdateAsync));
    }

    public async Task<List<BatteryServicePackage>> FilterPackagesAsync(string? search, bool? isActive)
    {
        var query = _context.BatteryServicePackages.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p =>
                p.Name.Contains(search) ||
                p.Description.Contains(search)
            );
        }

        if (isActive != null)
        {
            query = query.Where(p => p.Active == isActive);
        }

        return await query.OrderBy(p => p.PackageId).ToListAsync();
    }


    public async Task<BatteryServicePackage?> GetByIdAsync(int id)
       => await _pkgRepo.GetByIdAsync(id);

    public async Task CreateAsync(BatteryServicePackage pkg)
        => await _pkgRepo.CreateAsync(pkg);

    public async Task UpdateAsync(BatteryServicePackage pkg)
        => await _pkgRepo.UpdateAsync(pkg);

    public async Task<bool> DeleteAsync(int id)
    {
        if (!await _pkgRepo.CanDeleteAsync(id))
            return false;

        return await _pkgRepo.DeleteAsync(id);
    }

    public async Task<List<UserPackage>> GetUserPackagesAsync(int userId)
    {
        return (await _upRepo.GetAllAsync(
            filter: up => up.UserId == userId,
            includes: new Expression<Func<UserPackage, object>>[]
            {
            up => up.Package!
            }
        )).ToList(); // DÒNG NÀY GIẢI QUYẾT HẾT!
    }
}