using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NFC.Platform.Application.DTOs;
using NFC.Platform.Application.Extensions;
using NFC.Platform.Application.Interfaces.Repositories;
using NFC.Platform.Application.Interfaces.Services;
using NFC.Platform.BuildingBlocks.Common.Helpers;
using NFC.Platform.BuildingBlocks.Localization;
using NFC.Platform.BuildingBlocks.Results;
using NFC.Platform.Domain.Entities;
using NFC.Platform.Domain.Enums;

namespace NFC.Platform.Application.Services
{
    public class CardOrderService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IMessageService messageService,
        ICurrentTenant currentTenant) : ICardOrderService
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        private readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        private readonly IMessageService _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
        private readonly ICurrentTenant _currentTenant = currentTenant ?? throw new ArgumentNullException(nameof(currentTenant));

        public async Task<ServiceResult<PagedResult<CardOrderDto>>> GetPagedAsync(PaginationRequest request)
        {
            var pagedResult = await _unitOfWork.Repository<CardOrder>()
                .GetQueryable()
                .AsNoTracking()
                .Include(o => o.Items)
                .OrderByDescending(o => o.CreatedAt)
                .ToPagedResultAsync(request, o => _mapper.Map<CardOrderDto>(o));

            return ServiceResult<PagedResult<CardOrderDto>>.Success(pagedResult);
        }

        public async Task<ServiceResult<CardOrderDto>> GetByIdAsync(Guid id)
        {
            var order = await _unitOfWork.Repository<CardOrder>()
                .GetQueryable()
                .AsNoTracking()
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return ServiceResult<CardOrderDto>.NotFound(_messageService.Get("RecordNotFound"));

            return ServiceResult<CardOrderDto>.Success(_mapper.Map<CardOrderDto>(order));
        }

        public async Task<ServiceResult<CardOrderDto>> CreateAsync(CreateCardOrderRequest request)
        {
            var userId = _currentTenant.UserId;

            if (!userId.HasValue)
                return ServiceResult<CardOrderDto>.Unauthorized("User is not authenticated.");

            var order = _mapper.Map<CardOrder>(request);
            order.UserId = userId.Value;

            // Apply defaults for fields not supplied by the simple UI modal
            if (string.IsNullOrWhiteSpace(order.CardName))
            {
                order.CardName = $"طلب كروت - {order.Quantity}";
            }
            if (order.CardType == 0)
            {
                order.CardType = CardType.Plastic;
            }
            if (order.CardDesignType == 0)
            {
                order.CardDesignType = CardDesignType.BuiltInTemplate;
            }

            // TenantId is auto-assigned by DbContext.ApplyTenantRules() on SaveChanges
            // for all ITenantEntity entries with TenantId == Guid.Empty

            await _unitOfWork.Repository<CardOrder>().AddAsync(order);
            await _unitOfWork.SaveChangesAsync();

            // Reload with items for the response
            var created = await _unitOfWork.Repository<CardOrder>()
                .GetQueryable()
                .AsNoTracking()
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == order.Id);

            return ServiceResult<CardOrderDto>.Success(
                _mapper.Map<CardOrderDto>(created),
                _messageService.Get("RecordCreated"));
        }

        public async Task<ServiceResult> UpdateStatusAsync(Guid id, UpdateCardOrderStatusRequest request)
        {
            var repo = _unitOfWork.Repository<CardOrder>();
            var order = await repo.GetByIdAsync(id);

            if (order == null)
                return ServiceResult.NotFound(_messageService.Get("RecordNotFound"));

            order.Status = request.Status;
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.Success(_messageService.Get("RecordUpdated"));
        }

        public async Task<ServiceResult> DeleteAsync(Guid id)
        {
            var repo = _unitOfWork.Repository<CardOrder>();
            var order = await repo.GetByIdAsync(id);

            if (order == null)
                return ServiceResult.NotFound(_messageService.Get("RecordNotFound"));

            repo.Remove(order);
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.Success(_messageService.Get("RecordDeleted"));
        }

        public async Task<ServiceResult> AssignCardsAsync(Guid orderId, AssignCardsRequest request)
        {
            var orderRepo = _unitOfWork.Repository<CardOrder>();
            var order = await orderRepo.GetQueryable()
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
                return ServiceResult.NotFound(_messageService.Get("RecordNotFound") ?? "Card order not found.");

            var cardRepo = _unitOfWork.Repository<Card>();

            // Bulk retrieve existing cards for the given activation codes to avoid N+1 query loop
            var activationCodes = request.Assignments
                .Select(a => a.ActivationCode)
                .Where(code => !string.IsNullOrWhiteSpace(code))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var existingCards = await cardRepo.FindAsync(c => activationCodes.Contains(c.ActivationCode));

            var existingCardsLookup = existingCards
                .GroupBy(c => c.ActivationCode, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                foreach (var assignment in request.Assignments)
                {
                    var item = order.Items.FirstOrDefault(i => i.Id == assignment.OrderItemId);
                    if (item == null)
                        continue;

                    item.ActivationCode = assignment.ActivationCode;

                    if (!existingCardsLookup.TryGetValue(assignment.ActivationCode, out var card))
                    {
                        card = new Card
                        {
                            ActivationCode = assignment.ActivationCode,
                            CardOrderId = order.Id,
                            TenantId = order.TenantId
                        };
                        await cardRepo.AddAsync(card);
                        existingCardsLookup[assignment.ActivationCode] = card;
                    }
                    else
                    {
                        card.CardOrderId = order.Id;
                    }

                    // If order item has UserProfileId, link & activate card immediately
                    if (item.UserProfileId.HasValue)
                    {
                        card.UserProfileId = item.UserProfileId.Value;
                        card.IsActive = true;
                        card.ActivatedAt = DateTime.UtcNow;
                        item.LinkedCardId = card.Id;
                    }
                }

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }

            return ServiceResult.Success(_messageService.Get("RecordUpdated") ?? "Cards assigned successfully.");
        }
    }
}
