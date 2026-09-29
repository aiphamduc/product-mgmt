namespace ProductManagement.Core.Interfaces;

public interface IAuthService
{
    Task<bool> IsAuthorizedAsync(Guid userId, string permission, CancellationToken cancellationToken = default);
}
