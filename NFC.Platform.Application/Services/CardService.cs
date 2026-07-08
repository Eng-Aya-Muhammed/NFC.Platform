using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NFC.Platform.Application.DTOs;
using NFC.Platform.Application.Extensions;
using NFC.Platform.Application.Interfaces.Repositories;
using NFC.Platform.Application.Interfaces.Services;
using NFC.Platform.BuildingBlocks.Common.Exceptions;
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

        public CardService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IMessageService messageService)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
        }

        public async Task<ServiceResult<CardDto>> GetByIdAsync(Guid id)
        {
            var card = await _unitOfWork.Repository<Card>().GetByIdAsync(id);
            if (card == null)
            {
                return ServiceResult<CardDto>.NotFound(_messageService.Get("RecordNotFound"));
            }

            var cardDto = _mapper.Map<CardDto>(card);
            return ServiceResult<CardDto>.Success(cardDto);
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

            var existingCards = await repo.FindAsync(c => c.ActivationCode == request.ActivationCode);
            if (existingCards.Any())
            {
                throw new BusinessException("CardAlreadyAssigned");
            }

            var card = _mapper.Map<Card>(request);
            
            await repo.AddAsync(card);
            await _unitOfWork.SaveChangesAsync();

            var cardDto = _mapper.Map<CardDto>(card);
            return ServiceResult<CardDto>.Success(cardDto, _messageService.Get("RecordCreated"));
        }

        public async Task<ServiceResult> DeleteCardAsync(Guid id)
        {
            var repo = _unitOfWork.Repository<Card>();
            var card = await repo.GetByIdAsync(id);
            if (card == null)
            {
                return ServiceResult.NotFound(_messageService.Get("RecordNotFound"));
            }

            repo.Remove(card);
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.Success(_messageService.Get("RecordDeleted"));
        }
    }
}
