namespace NFC.Platform.Application.Services;

    public class CardService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IMessageService messageService,
        ICurrentTenant currentTenant) : ICardService
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        private readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        private readonly IMessageService _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
        private readonly ICurrentTenant _currentTenant = currentTenant ?? throw new ArgumentNullException(nameof(currentTenant));

        // ── Query ──────────────────────────────────────────────────────────────

        public async Task<ServiceResult<CardDto>> GetByIdAsync(Guid id)
        {
            var card = await _unitOfWork.Repository<Card>().GetByIdAsync(id);
            if (card == null)
                return ServiceResult<CardDto>.NotFound(_messageService.Get("RecordNotFound"));

            return ServiceResult<CardDto>.Success(_mapper.Map<CardDto>(card));
        }

        public async Task<ServiceResult<PagedResult<CardDto>>> GetPagedCardsAsync(PaginationRequest request)
        {
            var pagedResult = await _unitOfWork.Repository<Card>()
                .GetQueryable()
                .AsNoTracking()
                .OrderByDescending(c => c.CreatedAt)
                .ToPagedResultAsync(request, c => _mapper.Map<CardDto>(c));

            return ServiceResult<PagedResult<CardDto>>.Success(pagedResult);
        }

        public async Task<ServiceResult<List<CardDto>>> GetCardsForEncodingAsync(Guid orderId, string? statusFilter)
        {
            var query = _unitOfWork.Repository<Card>()
                .GetQueryable()
                .AsNoTracking()
                .Where(c => c.CardOrderId == orderId);

            if (!string.IsNullOrWhiteSpace(statusFilter)
                && Enum.TryParse<CardStatus>(statusFilter, ignoreCase: true, out var parsedStatus))
            {
                query = query.Where(c => c.Status == parsedStatus);
            }

            var cards = await query.OrderBy(c => c.CreatedAt).ToListAsync();
            return ServiceResult<List<CardDto>>.Success(cards.Select(c => _mapper.Map<CardDto>(c)).ToList());
        }

        // ── Create ─────────────────────────────────────────────────────────────

        public async Task<ServiceResult<CardDto>> CreateCardAsync(CreateCardRequest request)
        {
            var repo = _unitOfWork.Repository<Card>();
            var existing = await repo.FindAsync(c => c.UniqueCode == request.ActivationCode);
            if (existing.Count > 0)
                throw new BusinessException("CardAlreadyAssigned");

            var card = _mapper.Map<Card>(request);
            await repo.AddAsync(card);
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult<CardDto>.Success(_mapper.Map<CardDto>(card), _messageService.Get("RecordCreated"));
        }

        // ── User self-activation (via activation code / QR) ───────────────────

        public async Task<ServiceResult> ActivateCardAsync(ActivateCardRequest request)
        {
            var userId = _currentTenant.UserId;
            var tenantId = _currentTenant.TenantId;

            if (!userId.HasValue || !tenantId.HasValue)
                return ServiceResult.Unauthorized(_messageService.Get("UserNotAuthenticated"));

            var cardRepo = _unitOfWork.Repository<Card>();
            var userProfileRepo = _unitOfWork.Repository<UserProfile>();

            var cards = await cardRepo.FindAsync(c => c.UniqueCode == request.ActivationCode);
            var card = cards.Count > 0 ? cards[0] : null;

            if (card == null)
                return ServiceResult.NotFound(_messageService.Get("CardNotFound"));

            if (card.Status == CardStatus.Active)
                return ServiceResult.Fail(_messageService.Get("CardAlreadyActivated"), 400);

            if (card.Status == CardStatus.Deactivated)
                return ServiceResult.Fail(_messageService.Get("CardDeactivated"), 410);

            var userProfile = await GetOrCreateUserProfileAsync(userProfileRepo, userId.Value);
            if (userProfile == null)
                return ServiceResult.Fail(_messageService.Get("RecordNotFound"), 400);

            card.UserProfileId = userProfile.Id;
            card.Status = CardStatus.Active;
            card.ActivatedAt = DateTime.UtcNow;

            var orderItems = await _unitOfWork.Repository<CardOrderItem>().FindAsync(oi => oi.ActivationCode == card.UniqueCode);
            foreach (var item in orderItems)
                item.LinkedCardId = card.Id;

            await _unitOfWork.SaveChangesAsync();
            return ServiceResult.Success(_messageService.Get("CardActivatedSuccessfully"));
        }

        // ── Encoding tool integration ──────────────────────────────────────────

        public async Task<ServiceResult> MarkCardEncodedAsync(Guid cardId)
        {
            var card = await _unitOfWork.Repository<Card>().GetByIdAsync(cardId);
            if (card == null)
                return ServiceResult.NotFound(_messageService.Get("CardNotFound"));

            if (card.Status != CardStatus.UnassignedCode)
                return ServiceResult.Fail(_messageService.Get("CannotMarkEncoded", card.Status.ToString()), 400);

            card.Status = CardStatus.Encoded;
            await _unitOfWork.SaveChangesAsync();

            // Auto-transition order to ReadyForDelivery when all cards are encoded
            if (card.CardOrderId.HasValue)
                await TryTransitionOrderToReadyForDeliveryAsync(card.CardOrderId.Value);

            return ServiceResult.Success(_messageService.Get("RecordUpdated"));
        }

        // ── Admin activation ──────────────────────────────────────────────────

        public async Task<ServiceResult> ActivateCardByIdAsync(Guid cardId)
        {
            var card = await _unitOfWork.Repository<Card>().GetByIdAsync(cardId);
            if (card == null)
                return ServiceResult.NotFound(_messageService.Get("CardNotFound"));

            if (card.Status == CardStatus.Active)
                return ServiceResult.Fail(_messageService.Get("CardAlreadyActivated"), 400);

            card.Status = CardStatus.Active;
            card.ActivatedAt = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.Success(_messageService.Get("CardActivatedSuccessfully"));
        }

        public async Task<ServiceResult> ActivateAllCardsForOrderAsync(Guid orderId)
        {
            var cards = await _unitOfWork.Repository<Card>()
                .GetQueryable()
                .Where(c => c.CardOrderId == orderId && c.Status != CardStatus.Active)
                .ToListAsync();

            if (cards.Count == 0)
                return ServiceResult.Success();

            var now = DateTime.UtcNow;
            foreach (var c in cards)
            {
                c.Status = CardStatus.Active;
                c.ActivatedAt = now;
            }

            await _unitOfWork.SaveChangesAsync();
            return ServiceResult.Success(_messageService.Get("RecordUpdated"));
        }

        public async Task<ServiceResult> DeactivateCardAsync(Guid cardId)
        {
            var card = await _unitOfWork.Repository<Card>().GetByIdAsync(cardId);
            if (card == null)
                return ServiceResult.NotFound(_messageService.Get("CardNotFound"));

            card.Status = CardStatus.Deactivated;
            await _unitOfWork.SaveChangesAsync();
            return ServiceResult.Success(_messageService.Get("RecordUpdated"));
        }

        // ── Delete ─────────────────────────────────────────────────────────────

        public async Task<ServiceResult> DeleteCardAsync(Guid id)
        {
            var repo = _unitOfWork.Repository<Card>();
            var card = await repo.GetByIdAsync(id);

            if (card == null)
                return ServiceResult.NotFound(_messageService.Get("RecordNotFound"));

            repo.Remove(card);
            await _unitOfWork.SaveChangesAsync();
            return ServiceResult.Success(_messageService.Get("RecordDeleted"));
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private async Task TryTransitionOrderToReadyForDeliveryAsync(Guid orderId)
        {
            var orderRepo = _unitOfWork.Repository<CardOrder>();
            var order = await orderRepo.GetByIdAsync(orderId);
            if (order == null || order.Status != OrderStatus.Encoding)
                return;

            var anyStillUnassigned = await _unitOfWork.Repository<Card>()
                .GetQueryable()
                .AnyAsync(c => c.CardOrderId == orderId && c.Status == CardStatus.UnassignedCode);

            if (!anyStillUnassigned)
            {
                order.Status = OrderStatus.ReadyForDelivery;
                await _unitOfWork.SaveChangesAsync();
            }
        }

        private async Task<UserProfile?> GetOrCreateUserProfileAsync(
            IGenericRepository<UserProfile> userProfileRepo, Guid userId)
        {
            var profiles = await userProfileRepo.FindAsync(up => up.UserId == userId);
            if (profiles.Count > 0)
                return profiles[0];

            var user = await _unitOfWork.Repository<User>().GetByIdAsync(userId);
            if (user == null)
                return null;

            var newProfile = _mapper.Map<UserProfile>(user);
            await userProfileRepo.AddAsync(newProfile);
            await _unitOfWork.SaveChangesAsync();
            return newProfile;
        }
    }
