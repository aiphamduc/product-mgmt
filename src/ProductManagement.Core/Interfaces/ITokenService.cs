namespace ProductManagement.Core.Interfaces;

public interface ITokenService
{
    Task<string> CreateAccessTokenAsync(Guid userId, CancellationToken cancellationToken = default);
}
