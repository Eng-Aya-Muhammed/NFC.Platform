namespace NFC.Platform.Application.Services;

public class CardPricingService(
    IUnitOfWork unitOfWork,
    IMapper mapper,
    IMessageService messageService) : ICardPricingService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    private readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    private readonly IMessageService _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));

    public async Task<ServiceResult<OrderPricingResponseDto>> CalculateOrderPricingAsync(CardType cardType, int quantity)
    {
        var pricing = await _unitOfWork.Repository<CardPricing>().GetQueryable()
            .AsNoTracking()
            .Where(p => p.CardType == cardType && p.IsActive && p.EffectiveFrom <= DateTime.UtcNow && (p.EffectiveTo == null || p.EffectiveTo > DateTime.UtcNow))
            .OrderByDescending(p => p.EffectiveFrom)
            .FirstOrDefaultAsync();

        if (pricing == null)
        {
            return ServiceResult<OrderPricingResponseDto>.Fail(
                _messageService.Get("PricingNotConfigured", cardType.ToString()) ?? $"Pricing is not configured for card type '{cardType}'.",
                500);
        }

        var dto = new OrderPricingResponseDto
        {
            UnitPrice = pricing.UnitPrice,
            TotalPrice = pricing.UnitPrice * quantity,
            Currency = pricing.Currency
        };

        return ServiceResult<OrderPricingResponseDto>.Success(dto);
    }

    public async Task<ServiceResult<IReadOnlyList<CardPricingDto>>> GetActiveCatalogAsync()
    {
        var prices = await _unitOfWork.Repository<CardPricing>().GetQueryable()
            .AsNoTracking()
            .Where(p => p.IsActive && p.EffectiveFrom <= DateTime.UtcNow && (p.EffectiveTo == null || p.EffectiveTo > DateTime.UtcNow))
            .ToListAsync();

        return ServiceResult<IReadOnlyList<CardPricingDto>>.Success(_mapper.Map<IReadOnlyList<CardPricingDto>>(prices));
    }
}
