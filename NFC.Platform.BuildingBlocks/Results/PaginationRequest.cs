using NFC.Platform.BuildingBlocks.Common.Constants;

namespace NFC.Platform.BuildingBlocks.Results
{
    public class PaginationRequest
    {
        private int _pageNumber = 1;
        private int _pageSize = GeneralConstants.DefaultPageSize;

        public int PageNumber
        {
            get => _pageNumber;
            set => _pageNumber = value < 1 ? 1 : value;
        }

        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value < 1
                ? GeneralConstants.DefaultPageSize
                : (value > GeneralConstants.MaxPageSize ? GeneralConstants.MaxPageSize : value);
        }
    }
}
