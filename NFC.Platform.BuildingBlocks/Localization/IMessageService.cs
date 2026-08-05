namespace NFC.Platform.BuildingBlocks.Localization
{
    public interface IMessageService
    {
        string Get(string key, params object[]? args);
    }
}
