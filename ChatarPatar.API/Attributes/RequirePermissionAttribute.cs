using ChatarPatar.Common.Enums;

namespace ChatarPatar.API.Attributes;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class RequirePermissionAttribute : Attribute
{
    public string[] Permissions { get; }
    public PermissionCheckLogicEnum Logic { get; }

    public RequirePermissionAttribute(PermissionCheckLogicEnum logic = PermissionCheckLogicEnum.Any, params string[] permissions)
    {
        if (permissions == null || permissions.Length == 0)
            throw new ArgumentException("At least one permission must be specified.", nameof(permissions));

        Permissions = permissions;
        Logic = logic;
    }
}