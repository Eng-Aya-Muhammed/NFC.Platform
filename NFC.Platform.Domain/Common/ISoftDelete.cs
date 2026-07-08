namespace NFC.Platform.Domain.Common
{
    /// <summary>
    /// Contract indicating that the implementing entity supports logical (soft) deletion.
    /// </summary>
    public interface ISoftDelete
    {
        /// <summary>
        /// Gets or sets a value indicating whether this entity is logically deleted.
        /// </summary>
        bool IsDeleted { get; set; }
    }
}
