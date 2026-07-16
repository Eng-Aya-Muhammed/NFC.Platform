namespace NFC.Platform.Application.Services;

    public class CardTemplateService(
        IUnitOfWork unitOfWork,
        IMapper mapper) : ICardTemplateService
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        private readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

        public async Task<ServiceResult<IReadOnlyList<CardTemplateDto>>> GetActiveTemplatesAsync()
        {
            var templates = await _unitOfWork.Repository<CardTemplate>()
                .GetQueryable()
                .Where(t => t.IsActive)
                .OrderBy(t => t.DisplayOrder)
                .ToListAsync();

            var dtos = _mapper.Map<IReadOnlyList<CardTemplateDto>>(templates);
            return ServiceResult<IReadOnlyList<CardTemplateDto>>.Success(dtos);
        }
    }
