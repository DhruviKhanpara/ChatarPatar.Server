using ChatarPatar.Common.Enums;
using ChatarPatar.Common.Models;

namespace ChatarPatar.Application.ServiceContracts;

public interface IPermissionService
{
    Task<bool> HasPermissionAsync(PermissionContext ctx, string[] permission, PermissionCheckLogicEnum logic);
    void InvalidateUserPermissions(Guid userId);
}
