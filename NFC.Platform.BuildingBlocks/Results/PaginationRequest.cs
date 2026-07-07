using NFC.Platform.BuildingBlocks.Common.Constants;

namespace NFC.Platform.BuildingBlocks.Results
{
    /// <summary>
    /// Model representing a request for paginated data from clients.
    /// </summary>
    public class PaginationRequest
    {
        private int _pageNumber = 1;
        private int _pageSize = GeneralConstants.DefaultPageSize;

        /// <summary>
        /// Gets or sets the requested page number. Defaults to 1.
        /// </summary>
        public int PageNumber
        {
            get => _pageNumber;
            set => _pageNumber = value < 1 ? 1 : value;
        }

        /// <summary>
        /// Gets or sets the requested page size. Capped at <see cref="GeneralConstants.MaxPageSize"/> (100) and defaults to <see cref="GeneralConstants.DefaultPageSize"/> (10).
        /// </summary>
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value < 1 
                ? GeneralConstants.DefaultPageSize 
                : (value > GeneralConstants.MaxPageSize ? GeneralConstants.MaxPageSize : value);
        }
    }
}
