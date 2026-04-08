using ChatarPatar.Common.Enums;
using ChatarPatar.Infrastructure.Entities;
using ChatarPatar.Infrastructure.Persistence;
using ChatarPatar.Infrastructure.RepositoryContracts;

namespace ChatarPatar.Infrastructure.Repositories;

internal class OtpVerificationRepository : IOtpVerificationRepository
{
    private readonly AppDbContext _context;

    public OtpVerificationRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(OtpVerification entity) => await _context.OtpVerifications.AddAsync(entity);

    public IQueryable<OtpVerification> GetActiveOtp(Guid userId, OtpPurposeEnum purpose) =>
        _context.OtpVerifications
            .Where(x => x.UserId == userId
                     && x.Purpose == purpose
                     && !x.IsUsed
                     && x.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(x => x.CreatedAt)
            .Take(1);

    public IQueryable<OtpVerification> GetAllActiveOtps(Guid userId, OtpPurposeEnum purpose) =>
        _context.OtpVerifications
            .Where(x => x.UserId == userId
                     && x.Purpose == purpose
                     && !x.IsUsed
                     && x.ExpiresAt > DateTime.UtcNow);

    public IQueryable<OtpVerification> GetLatestOtp(Guid userId, OtpPurposeEnum purpose) =>
        _context.OtpVerifications
            .Where(x => x.UserId == userId 
                        && x.Purpose == purpose)
            .OrderByDescending(x => x.CreatedAt)
            .Take(1);
}
