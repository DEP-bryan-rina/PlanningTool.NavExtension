using PlanningTool.NavExtension.Models;

namespace PlanningTool.NavExtension.Services
{
    public interface IUserPermissionService
    {
        Task<UserPermission> GetUserPermissionAsync(string userId);
    }
}
