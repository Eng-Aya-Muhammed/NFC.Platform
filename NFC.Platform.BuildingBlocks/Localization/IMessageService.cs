namespace NFC.Platform.BuildingBlocks.Localization
{
    /// <summary>
    /// Central messaging service wrapper to retrieve localized strings.
    /// </summary>
    public interface IMessageService
    {
        /// <summary>
        /// Retrieves the localized message for the specified key, formatted with optional arguments.
        /// </summary>
        /// <param name="key">The resource key to search for.</param>
        /// <param name="args">Format arguments to inject into the localized string.</param>
        /// <returns>The localized formatted string, or the key itself if not found.</returns>
        string Get(string key, params object[] args);
    }
}
