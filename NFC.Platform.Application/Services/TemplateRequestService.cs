namespace NFC.Platform.Application.Services;

    public class TemplateRequestService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IMessageService messageService,
        ICurrentTenant currentTenant) : ITemplateRequestService
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        private readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        private readonly IMessageService _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
        private readonly ICurrentTenant _currentTenant = currentTenant ?? throw new ArgumentNullException(nameof(currentTenant));

        public async Task<ServiceResult<TemplateRequestDto>> CreateRequestAsync(Guid userId, CreateTemplateRequest request)
        {
            var tenantId = _currentTenant.TenantId;
            if (!tenantId.HasValue)
            {
                var msg = _messageService.Get("Unauthorized");
                return ServiceResult<TemplateRequestDto>.Unauthorized(string.IsNullOrWhiteSpace(msg) ? "User is not authenticated." : msg);
            }

            var templateRequest = _mapper.Map<TemplateRequest>(request);
            templateRequest.RequestedByUserId = userId;

            await _unitOfWork.Repository<TemplateRequest>().AddAsync(templateRequest);
            await _unitOfWork.SaveChangesAsync();

            // Fetch with User details to return username
            var createdRequest = await _unitOfWork.Repository<TemplateRequest>()
                .GetQueryable()
                .AsNoTracking()
                .Include(r => r.RequestedByUser)
                .FirstOrDefaultAsync(r => r.Id == templateRequest.Id);

            var dto = _mapper.Map<TemplateRequestDto>(createdRequest);
            return ServiceResult<TemplateRequestDto>.Success(dto, _messageService.Get("RecordCreated") ?? "Template request submitted successfully.");
        }

        public async Task<ServiceResult<IReadOnlyList<TemplateRequestDto>>> GetTenantRequestsAsync()
        {
            var requests = await _unitOfWork.Repository<TemplateRequest>()
                .GetQueryable()
                .AsNoTracking()
                .Include(r => r.RequestedByUser)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var dtos = _mapper.Map<IReadOnlyList<TemplateRequestDto>>(requests);
            return ServiceResult<IReadOnlyList<TemplateRequestDto>>.Success(dtos);
        }


        public async Task<ServiceResult<TemplateRequestDto>> GetRequestByIdAsync(Guid id)
        {
            var request = await _unitOfWork.Repository<TemplateRequest>()
                .GetQueryable()
                .AsNoTracking()
                .Include(r => r.RequestedByUser)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
            {
                return ServiceResult<TemplateRequestDto>.NotFound(_messageService.Get("RecordNotFound") ?? "Template request not found.");
            }

            var dto = _mapper.Map<TemplateRequestDto>(request);
            return ServiceResult<TemplateRequestDto>.Success(dto);
        }
    }
