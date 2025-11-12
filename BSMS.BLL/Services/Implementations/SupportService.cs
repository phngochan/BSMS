using BSMS.BusinessObjects.Enums;
using BSMS.BusinessObjects.Models;
using BSMS.DAL.Repositories;

namespace BSMS.BLL.Services.Implementations;

public class SupportService : ISupportService
{
    private readonly ISupportRepository _supportRepository;
    private readonly IUserRepository _userRepository;
    private readonly IChangingStationRepository _stationRepository;

    public SupportService(
        ISupportRepository supportRepository,
        IUserRepository userRepository,
        IChangingStationRepository stationRepository)
    {
        _supportRepository = supportRepository;
        _userRepository = userRepository;
        _stationRepository = stationRepository;
    }

    public async Task<int> CountByStatusAsync(SupportStatus status)
    {
        return await _supportRepository.CountByStatusAsync(status);
    }

    public async Task<Support> CreateSupportAsync(Support support)
    {
        var user = await _userRepository.GetSingleAsync(u => u.UserId == support.UserId);
        if (user == null)
        {
            throw new InvalidOperationException("User not found");
        }

        var station = await _stationRepository.GetSingleAsync(s => s.StationId == support.StationId);
        if (station == null)
        {
            throw new InvalidOperationException("Station not found");
        }

        support.CreatedAt = DateTime.UtcNow;
        return await _supportRepository.CreateAsync(support);
    }

    public async Task DeleteSupportAsync(int supportId)
    {
        var existing = await _supportRepository.GetSingleAsync(s => s.SupportId == supportId);
        if (existing == null)
        {
            throw new InvalidOperationException("Support request not found");
        }

        await _supportRepository.DeleteAsync(existing);
    }

    public async Task<IEnumerable<Support>> GetOpenSupportsAsync()
    {
        return await _supportRepository.GetOpenSupportsAsync();
    }

    public async Task<Support?> GetSupportAsync(int supportId)
    {
        return await _supportRepository.GetSupportWithDetailsAsync(supportId);
    }

    public async Task<IEnumerable<Support>> GetSupportsByStationAsync(int stationId)
    {
        return await _supportRepository.GetSupportsByStationAsync(stationId);
    }

        public async Task UpdateSupportAsync(Support support)
        {
            var existing = await _supportRepository.GetSingleAsync(s => s.SupportId == support.SupportId);
            if (existing == null)
            {
                throw new InvalidOperationException("Support request not found");
            }

            existing.Type = support.Type;
            existing.Description = support.Description;
            existing.Status = support.Status;
            existing.Rating = support.Rating;
            existing.StaffNote = support.StaffNote;

            await _supportRepository.UpdateAsync(existing);
        }

        public async Task UpdateSupportStatusAsync(int supportId, SupportStatus status, int? rating = null, string? staffNote = null)
        {
            var existing = await _supportRepository.GetSingleAsync(s => s.SupportId == supportId);
            if (existing == null)
            {
                throw new InvalidOperationException("Support request not found");
            }

            existing.Status = status;
            existing.Rating = status == SupportStatus.Closed ? rating : null;
            if (staffNote != null)
            {
                existing.StaffNote = staffNote;
            }

            await _supportRepository.UpdateAsync(existing);
        }

        public async Task<IEnumerable<Support>> GetSupportsByUserAsync(int userId)
        {
            return await _supportRepository.GetSupportsByUserAsync(userId);
        }

        public async Task UpdateSupportRatingAsync(int supportId, int rating)
        {
            var existing = await _supportRepository.GetSingleAsync(s => s.SupportId == supportId);
            if (existing == null)
            {
                throw new InvalidOperationException("Support request not found");
            }

            existing.Rating = rating;
            await _supportRepository.UpdateAsync(existing);
        }
}
