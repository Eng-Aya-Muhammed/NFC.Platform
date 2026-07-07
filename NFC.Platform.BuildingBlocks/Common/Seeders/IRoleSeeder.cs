using System.Threading.Tasks;

namespace NFC.Platform.BuildingBlocks.Common.Seeders
{
    /// <summary>
    /// Contract for database role seeder.
    /// </summary>
    public interface IRoleSeeder
    {
        /// <summary>
        /// Seeds the default application roles (Admin, Customer) into the database if they do not exist.
        /// </summary>
        /// <returns>A task that represents the asynchronous seed operation.</returns>
        Task SeedAsync();
    }
}
