using System.Threading.Tasks;

namespace NFC.Platform.BuildingBlocks.Common.Seeders
{
    public interface IPermissionSeeder
    {
        Task SeedAsync();
    }
}
