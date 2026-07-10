using System;
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
    public class CardOrderService : ICardOrderService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IMessageService _messageService;
        private readonly ICurrentTenant _currentTenant;

        public CardOrderService(
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

        public async Task<ServiceResult<PagedResult<CardOrderDto>>> GetPagedAsync(PaginationRequest request)
        {
            var pagedResult = await _unitOfWork.Repository<CardOrder>()
                .GetQueryable()
                .Include(o => o.Items)
                .OrderByDescending(o => o.CreatedAt)
                .ToPagedResultAsync(request, o => _mapper.Map<CardOrderDto>(o));

            return ServiceResult<PagedResult<CardOrderDto>>.Success(pagedResult);
        }

        public async Task<ServiceResult<CardOrderDto>> GetByIdAsync(Guid id)
        {
            var order = await _unitOfWork.Repository<CardOrder>()
                .GetQueryable()
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
            repo.Update(order);
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
    }
}
