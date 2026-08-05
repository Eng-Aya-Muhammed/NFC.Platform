using System.Threading.Tasks;

namespace NFC.Platform.BuildingBlocks.Common.Seeders
{
    public interface ISubscriptionPlanSeeder
    {
        Task SeedAsync();
    }
}
