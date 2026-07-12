using System.Threading.Tasks;

namespace NFC.Platform.BuildingBlocks.Common.Seeders
{
    /// <summary>
    /// Contract for database subscription plan seeder.
    /// </summary>
    public interface ISubscriptionPlanSeeder
    {
        /// <summary>
        /// Seeds default subscription plans into the database if they do not exist.
        /// </summary>
        /// <returns>A task that represents the asynchronous seed operation.</returns>
        Task SeedAsync();
    }
}
