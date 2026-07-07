using System.Threading.Tasks;

namespace NFC.Platform.BuildingBlocks.Common.Seeders
{
    /// <summary>
    /// Contract for database administrator user seeder.
    /// </summary>
    public interface IAdminUserSeeder
    {
        /// <summary>
        /// Seeds the default admin user into the database if not present, using configurations from appsettings.
        /// </summary>
        /// <returns>A task that represents the asynchronous seed operation.</returns>
        Task SeedAsync();
    }
}
