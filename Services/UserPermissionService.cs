using Microsoft.Data.SqlClient;
using PlanningTool.NavExtension.Models;

namespace PlanningTool.NavExtension.Services
{
    public class UserPermissionService : IUserPermissionService
    {
        private readonly IConfiguration _configuration;

        public UserPermissionService(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public async Task<UserPermission> GetUserPermissionAsync(string userId)
        {
            var permission = new UserPermission();

            // Simulate fetching user permissions from a data source
            using SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));


            string sql = @"
            SELECT ImPex, Procurement
            FROM [DEDON2019].[dbo].[DEDONPH$User Setup]
            WHERE [User ID] = @UserID";

            using SqlCommand cmd = new SqlCommand (sql, con);
            cmd.Parameters.AddWithValue("@UserID", userId);

            await con.OpenAsync();
            using SqlDataReader reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                permission.ImPex = Convert.ToBoolean(reader["ImPex"]);
                permission.Procurement = Convert.ToBoolean(reader["Procurement"]);
            }

            return permission;
        }
    }
}
