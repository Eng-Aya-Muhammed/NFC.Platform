using System.Threading.Tasks;

namespace NFC.Platform.BuildingBlocks.Common.Seeders
{
    /// <summary>
    /// Contract for the default CardTemplate seeder.
    /// </summary>
    public interface IDefaultCardTemplateSeeder
    {
        /// <summary>
        /// Seeds the default digital profile CardTemplate into the database if it does not exist,
        /// and normalizes StyleConfigJson on all existing templates to the canonical shape.
        /// </summary>
        Task SeedAsync();
    }
}
