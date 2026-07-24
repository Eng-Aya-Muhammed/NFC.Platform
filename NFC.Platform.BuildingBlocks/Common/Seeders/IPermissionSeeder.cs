using System.Threading.Tasks;

namespace NFC.Platform.BuildingBlocks.Common.Seeders
{
    /// <summary>
    /// Contract for seeding default permissions into system roles.
    /// </summary>
    public interface IPermissionSeeder
    {
        /// <summary>
        /// Seeds default permissions for system roles (CompanyAdmin, Customer)
        /// into the RolePermissions table if they do not already exist.
        /// </summary>
        Task SeedAsync();
    }
}
