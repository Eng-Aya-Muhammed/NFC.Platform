using System;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NFC.Platform.Application.DTOs;
using NFC.Platform.Application.Extensions;
using NFC.Platform.Application.Interfaces.Repositories;
using NFC.Platform.Application.Interfaces.Services;
using NFC.Platform.BuildingBlocks.Common.Exceptions;
using NFC.Platform.BuildingBlocks.Common.Helpers;
using NFC.Platform.BuildingBlocks.Localization;
using NFC.Platform.BuildingBlocks.Results;
using NFC.Platform.Domain.Entities;

namespace NFC.Platform.Application.Services
{
    public class CardService : ICardService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IMessageService _messageService;
        private readonly ICurrentTenant _currentTenant;

        public CardService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IMessageService messageService,
            ICurrentTenant currentTenant)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
            _currentTenant = currentTenant ?? throw new ArgumentNullException(nameof(currentTenant));
        }

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
                .OrderByDescending(c => c.CreatedAt)
                .ToPagedResultAsync(request, c => _mapper.Map<CardDto>(c));

            return ServiceResult<PagedResult<CardDto>>.Success(pagedResult);
        }

        public async Task<ServiceResult<CardDto>> CreateCardAsync(CreateCardRequest request)
        {
            var repo = _unitOfWork.Repository<Card>();

            var existing = await repo.FindAsync(c => c.ActivationCode == request.ActivationCode);
            if (existing.Count > 0)
                throw new BusinessException("CardAlreadyAssigned");

            var card = _mapper.Map<Card>(request);
            await repo.AddAsync(card);
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult<CardDto>.Success(_mapper.Map<CardDto>(card), _messageService.Get("RecordCreated"));
        }

        public async Task<ServiceResult> ActivateCardAsync(ActivateCardRequest request)
        {
            var userId = _currentTenant.UserId;
            var tenantId = _currentTenant.TenantId;

            if (!userId.HasValue || !tenantId.HasValue)
                return ServiceResult.Unauthorized("User is not authenticated.");

            var cardRepo = _unitOfWork.Repository<Card>();
            var userProfileRepo = _unitOfWork.Repository<UserProfile>();

            // Find card by activation code (global query filters apply — card belongs to same tenant)
            var cards = await cardRepo.FindAsync(c => c.ActivationCode == request.ActivationCode);
            var card = cards.Count > 0 ? cards[0] : null;

            if (card == null)
                return ServiceResult.NotFound(_messageService.Get("CardNotFound") ?? "Card not found.");

            if (card.IsActive || card.UserProfileId != null)
                return ServiceResult.Fail("Card already activated", 400);

            // Resolve or create the user's profile
            var userProfile = await GetOrCreateUserProfileAsync(userProfileRepo, userId.Value);
            if (userProfile == null)
                return ServiceResult.Fail("User not found.", 400);

            // Activate the card
            card.UserProfileId = userProfile.Id;
            card.IsActive = true;
            card.ActivatedAt = DateTime.UtcNow;
            cardRepo.Update(card);

            // Link any matching CardOrderItems in the same SaveChanges call for efficiency
            var orderItemRepo = _unitOfWork.Repository<CardOrderItem>();
            var orderItems = await orderItemRepo.FindAsync(oi => oi.ActivationCode == card.ActivationCode);
            foreach (var item in orderItems)
            {
                item.LinkedCardId = card.Id;
                orderItemRepo.Update(item);
            }

            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.Success(_messageService.Get("CardActivatedSuccessfully") ?? "Card activated successfully.");
        }

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

        // ---------------------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------------------

        private async Task<UserProfile?> GetOrCreateUserProfileAsync(
            IGenericRepository<UserProfile> userProfileRepo,
            Guid userId)
        {
            var profiles = await userProfileRepo.FindAsync(up => up.UserId == userId);
            if (profiles.Count > 0)
                return profiles[0];

            var user = await _unitOfWork.Repository<User>().GetByIdAsync(userId);
            if (user == null)
                return null;

            var newProfile = new UserProfile
            {
                UserId = userId,
                FullName = user.Username
            };

            await userProfileRepo.AddAsync(newProfile);
            await _unitOfWork.SaveChangesAsync();

            return newProfile;
        }
    }
}
