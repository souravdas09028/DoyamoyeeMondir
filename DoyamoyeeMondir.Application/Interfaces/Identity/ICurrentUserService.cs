namespace DoyamoyeeMondir.Application.Interfaces.Identity;

public interface ICurrentUserService
{
    string? UserId { get; }

    bool IsAuthenticated { get; }
}