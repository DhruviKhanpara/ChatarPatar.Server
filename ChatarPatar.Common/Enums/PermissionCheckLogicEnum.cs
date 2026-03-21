using System.ComponentModel.DataAnnotations;

namespace ChatarPatar.Common.Enums;

public enum PermissionCheckLogicEnum
{
    [Display(Description = "User must have all of the required permissions")]
    All,
    
    [Display(Description = "User must have at least one of the required permissions")]
    Any
}
