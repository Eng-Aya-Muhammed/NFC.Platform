

using NFC.Platform.BuildingBlocks.Common.Helpers;
using NFC.Platform.BuildingBlocks.Common.Interfaces;
using NFC.Platform.BuildingBlocks.Common.Models;

namespace NFC.Platform.Application.Services;

public class AdminService : IAdminService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IMessageService _messageService;
    private readonly IStorageService _storageService;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly OtpSettings _otpSettings;
    private readonly ExportBuilder? _exportBuilder;
    private readonly IExcelExportService? _excelExportService;
    private readonly IPdfExportService? _pdfExportService;

    public AdminService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IMessageService messageService,
        IStorageService storageService,
        IBackgroundJobClient backgroundJobClient,
        IOptions<OtpSettings>? otpSettings = null,
        ExportBuilder? exportBuilder = null,
        IExcelExportService? excelExportService = null,
        IPdfExportService? pdfExportService = null)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
        _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        _backgroundJobClient = backgroundJobClient ?? throw new ArgumentNullException(nameof(backgroundJobClient));
        _otpSettings = otpSettings?.Value ?? new OtpSettings();
        _exportBuilder = exportBuilder;
        _excelExportService = excelExportService;
        _pdfExportService = pdfExportService;
    }

    public async Task<ServiceResult<PagedResult<AdminOrderSummaryDto>>> GetOrdersPagedAsync(PaginationRequest request, OrderStatus? statusFilter, Guid? companyId = null, Guid? tenantId = null, string? search = null, CancellationToken cancellationToken = default)
    {
        var query = _unitOfWork.Repository<CardOrder>()
            .GetQueryable()
            .AsNoTracking()
            .Include(o => o.Tenant)
                .ThenInclude(t => t.Company)
            .Include(o => o.CardDesign)
                .ThenInclude(d => d!.CardType)
            .Include(o => o.CardDesign)
                .ThenInclude(d => d!.CardPackage)
            .OrderByDescending(o => o.CreatedAt)
            .AsQueryable();

        if (statusFilter.HasValue)
        {
            query = query.Where(o => o.Status == statusFilter.Value);
        }

        if (companyId.HasValue)
        {
            query = query.Where(o => o.Tenant.Company != null && o.Tenant.Company.Id == companyId.Value);
        }

        if (tenantId.HasValue)
        {
            query = query.Where(o => o.TenantId == tenantId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(o => o.Id.ToString().Contains(search) ||
                                     (o.TrackingNumber != null && o.TrackingNumber.Contains(search)) ||
                                     (o.Notes != null && o.Notes.Contains(search)) ||
                                     (o.Tenant != null && (o.Tenant.Name.Contains(search) || (o.Tenant.Company != null && o.Tenant.Company.Name.Contains(search)))) ||
                                     (o.CardDesign != null && ((o.CardDesign.Notes != null && o.CardDesign.Notes.Contains(search)) || (o.CardDesign.CardType != null && (o.CardDesign.CardType.NameAr.Contains(search) || o.CardDesign.CardType.NameEn.Contains(search))))) ||
                                     o.Items.Any(i => i.EmployeeName.Contains(search) || (i.Email != null && i.Email.Contains(search)) || (i.Phone != null && i.Phone.Contains(search))));
        }

        var pagedResult = await query.ToPagedResultAsync(request, o => _mapper.Map<AdminOrderSummaryDto>(o), cancellationToken);
        return ServiceResult<PagedResult<AdminOrderSummaryDto>>.Success(pagedResult);
    }

    public async Task<ServiceResult<byte[]>> ExportAdminOrdersAsync(ExportFormat format, OrderStatus? statusFilter, Guid? companyId, string? search = null, CancellationToken cancellationToken = default)
    {
        if (_exportBuilder == null || _excelExportService == null || _pdfExportService == null)
        {
            return ServiceResult<byte[]>.Fail(_messageService.Get("RecordNotFound"), 500);
        }

        var query = _unitOfWork.Repository<CardOrder>()
            .GetQueryable()
            .AsNoTracking()
            .Include(o => o.Tenant)
                .ThenInclude(t => t.Company)
            .Include(o => o.CardDesign)
                .ThenInclude(d => d!.CardType)
            .Include(o => o.CardDesign)
                .ThenInclude(d => d!.CardPackage)
            .OrderByDescending(o => o.CreatedAt)
            .AsQueryable();

        if (statusFilter.HasValue)
        {
            query = query.Where(o => o.Status == statusFilter.Value);
        }

        if (companyId.HasValue)
        {
            query = query.Where(o => o.Tenant.Company != null && o.Tenant.Company.Id == companyId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(o => o.Id.ToString().Contains(search) ||
                                     (o.TrackingNumber != null && o.TrackingNumber.Contains(search)) ||
                                     (o.Notes != null && o.Notes.Contains(search)) ||
                                     (o.Tenant != null && (o.Tenant.Name.Contains(search) || (o.Tenant.Company != null && o.Tenant.Company.Name.Contains(search)))) ||
                                     (o.CardDesign != null && ((o.CardDesign.Notes != null && o.CardDesign.Notes.Contains(search)) || (o.CardDesign.CardType != null && (o.CardDesign.CardType.NameAr.Contains(search) || o.CardDesign.CardType.NameEn.Contains(search))))) ||
                                     o.Items.Any(i => i.EmployeeName.Contains(search) || (i.Email != null && i.Email.Contains(search)) || (i.Phone != null && i.Phone.Contains(search))));
        }

        var orders = await query.ToListAsync(cancellationToken);
        var exportDtos = orders.Select(o => new AdminOrderExportDto
        {
            Id = o.Id,
            CompanyName = o.Tenant?.Company?.Name ?? string.Empty,
            Quantity = o.Quantity,
            TotalAmount = o.TotalPrice,
            Status = o.Status,
            CreatedAt = o.CreatedAt
        }).ToList();

        var dataContainer = _exportBuilder.BuildContainer(exportDtos, "Export_Title_AdminOrders");

        byte[] fileBytes = format switch
        {
            ExportFormat.Excel => _excelExportService.GenerateExcel(dataContainer),
            ExportFormat.Pdf => _pdfExportService.GeneratePdf(dataContainer),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
        };

        return ServiceResult<byte[]>.Success(fileBytes);
    }

    public async Task<ServiceResult<byte[]>> ExportTenantsAsync(ExportFormat format, string? search = null, CancellationToken cancellationToken = default)
    {
        if (_exportBuilder == null || _excelExportService == null || _pdfExportService == null)
        {
            return ServiceResult<byte[]>.Fail(_messageService.Get("RecordNotFound"), 500);
        }

        var query = _unitOfWork.Repository<Tenant>()
            .GetQueryable()
            .AsNoTracking()
            .Include(t => t.Company)
            .OrderByDescending(t => t.CreatedAt)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(t => t.Name.Contains(search) ||
                                     (t.Company != null && (t.Company.Name.Contains(search) || (t.Company.Activity != null && t.Company.Activity.Contains(search)) || (t.Company.CommercialRegistry != null && t.Company.CommercialRegistry.Contains(search)))));
        }

        var tenants = await query.ToListAsync(cancellationToken);
        var exportDtos = tenants.Select(t => new TenantSummaryDto
        {
            Id = t.Id,
            Name = t.Company?.Name ?? t.Name,
            IsActive = t.IsActive,
            AccountType = t.Company != null ? "Company" : "Individual"
        }).ToList();

        var dataContainer = _exportBuilder.BuildContainer(exportDtos, "Export_Title_Tenants");

        byte[] fileBytes = format switch
        {
            ExportFormat.Excel => _excelExportService.GenerateExcel(dataContainer),
            ExportFormat.Pdf => _pdfExportService.GeneratePdf(dataContainer),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
        };

        return ServiceResult<byte[]>.Success(fileBytes);
    }

    public async Task<ServiceResult<AdminOrderDetailDto>> GetOrderByIdAsync(Guid id)
    {
        var order = await _unitOfWork.Repository<CardOrder>()
            .GetQueryable()
            .AsNoTracking()
            .Include(o => o.Tenant)
            .Include(o => o.User)
                .ThenInclude(u => u!.UserProfile)
                    .ThenInclude(p => p!.CustomLinks)
            .Include(o => o.CardDesign)
                .ThenInclude(d => d!.CardType)
            .Include(o => o.CardDesign)
                .ThenInclude(d => d!.CardPackage)
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
            return ServiceResult<AdminOrderDetailDto>.NotFound(_messageService.Get("RecordNotFound"));

        var dto = _mapper.Map<AdminOrderDetailDto>(order);

        if (order.User?.UserProfile?.ProfileTemplateId != null)
        {
            var template = await _unitOfWork.Repository<CardTemplate>()
                .GetByIdAsync(order.User.UserProfile.ProfileTemplateId.Value);
            if (template != null)
            {
                dto.SelectedTemplate = _mapper.Map<CardTemplateAdminDto>(template);
            }
        }

        if (dto.SelectedTemplate == null)
        {
            var latestRequest = await _unitOfWork.Repository<TemplateRequest>()
                .GetQueryable()
                .AsNoTracking()
                .Where(r => r.RequestedByUserId == order.UserId)
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefaultAsync();

            if (latestRequest != null)
            {
                dto.LatestTemplateRequest = _mapper.Map<TemplateRequestDto>(latestRequest);
            }
        }

        return ServiceResult<AdminOrderDetailDto>.Success(dto);
    }

    public async Task<ServiceResult> UpdateOrderStatusAsync(Guid id, UpdateOrderStatusDto dto)
    {
        var orderRepo = _unitOfWork.Repository<CardOrder>();
        var order = await orderRepo.GetQueryable()
            .Include(o => o.Items)
            .Include(o => o.Tenant)
                .ThenInclude(t => t.Company)
                    .ThenInclude(c => c!.AdminUser)
                        .ThenInclude(u => u!.UserProfile)
            .Include(o => o.User)
                .ThenInclude(u => u.UserProfile)
            .Include(o => o.CardDesign)
                .ThenInclude(d => d!.CardType)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
            return ServiceResult.NotFound(_messageService.Get("RecordNotFound"));

        if (!IsValidStatusTransition(order.Status, dto.Status))
            return ServiceResult.Fail(
                _messageService.Get("InvalidStatusTransition", order.Status.ToString(), dto.Status.ToString()), 422);

        if (dto.Status == OrderStatus.Rejected)
        {
            if (string.IsNullOrWhiteSpace(dto.RejectionReason))
                return ServiceResult.Fail(_messageService.Get("RejectionReasonRequired"), 422);

            order.RejectionReason = dto.RejectionReason;
        }

        if (dto.Status == OrderStatus.ReadyForDelivery)
        {
            if (string.IsNullOrWhiteSpace(dto.TrackingNumber))
                return ServiceResult.Fail(_messageService.Get("TrackingNumberRequired"), 422);
        }

        if (!string.IsNullOrWhiteSpace(dto.TrackingNumber))
            order.TrackingNumber = dto.TrackingNumber;

        var oldStatus = order.Status;
        order.Status = dto.Status;

        if (dto.Status == OrderStatus.Approved && oldStatus != OrderStatus.Approved)
        {
            var design = await _unitOfWork.Repository<CardDesign>()
                .GetQueryable()
                .FirstOrDefaultAsync(d => d.Id == order.CardDesignId);

            if (design != null)
            {
                var isCompanyOrder = order.Items != null && order.Items.Count > 0;
                var deducted = isCompanyOrder
                    ? order.Items!.Sum(i => i.NumberOfCardsRequired)
                    : order.Quantity;

                var remainingQty = design.TotalQuantity - design.UsedQuantity;
                if (deducted > remainingQty)
                {
                    return ServiceResult.Fail(
                        _messageService.Get("DesignRemainingQuantityExceeded", deducted.ToString(), Math.Max(0, remainingQty).ToString()),
                        422);
                }

                design.PendingQuantity = Math.Max(0, design.PendingQuantity - deducted);
                design.UsedQuantity += deducted;
            }
        }
        else if ((dto.Status == OrderStatus.Rejected || dto.Status == OrderStatus.Cancelled) && (oldStatus == OrderStatus.PendingReview || oldStatus == OrderStatus.UnderReview || oldStatus == OrderStatus.AwaitingDesign))
        {
            var design = await _unitOfWork.Repository<CardDesign>()
                .GetQueryable()
                .FirstOrDefaultAsync(d => d.Id == order.CardDesignId);

            if (design != null)
            {
                var isCompanyOrder = order.Items != null && order.Items.Count > 0;
                var refundedPending = isCompanyOrder
                    ? order.Items!.Sum(i => i.NumberOfCardsRequired)
                    : order.Quantity;

                design.PendingQuantity = Math.Max(0, design.PendingQuantity - refundedPending);
            }
        }

        if (dto.Status == OrderStatus.Cancelled &&
            (oldStatus is OrderStatus.Approved or OrderStatus.InPrinting or OrderStatus.Encoding or OrderStatus.ReadyForDelivery))
        {
            var design = await _unitOfWork.Repository<CardDesign>()
                .GetQueryable()
                .FirstOrDefaultAsync(d => d.Id == order.CardDesignId);

            if (design != null)
            {
                var isCompanyOrder = order.Items != null && order.Items.Count > 0;
                var refundQty = isCompanyOrder
                    ? order.Items!.Sum(i => i.NumberOfCardsRequired)
                    : order.Quantity;

                design.UsedQuantity = Math.Max(0, design.UsedQuantity - refundQty);
            }
        }

        if (dto.Status == OrderStatus.ReadyForDelivery)
        {
            var recipient = order.Tenant?.Company?.AdminUser ?? order.User;
            if (recipient != null)
            {
                var otp = GenerateOtp();
                order.DeliveryOtpHash = OtpHasher.HashOtp(otp);
                order.DeliveryOtpExpiresAt = DateTime.UtcNow.AddDays(7);
                order.DeliveryOtpLastSentAt = DateTime.UtcNow;
                order.DeliveryOtpResendCount = 0;
                order.DeliveryOtpFailedAttempts = 0;

                var cardName = order.CardDesign?.CardType?.NameAr
                    ?? order.CardDesign?.CardType?.NameEn
                    ?? _messageService.Get("DefaultPhysicalCardName");

                EnqueueOtpNotifications(recipient, otp, cardName, isResend: false);
            }
        }

        try
        {
            await _unitOfWork.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return ServiceResult.Fail(_messageService.Get("ConcurrentUpdateConflict"), 409);
        }

        return ServiceResult.Success(_messageService.Get("RecordUpdated"));
    }

    public async Task<ServiceResult> VerifyDeliveryOtpAsync(Guid orderId, string otp)
    {
        var order = await _unitOfWork.Repository<CardOrder>()
            .GetQueryable()
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
            return ServiceResult.NotFound(_messageService.Get("RecordNotFound"));

        if (order.Status != OrderStatus.ReadyForDelivery)
            return ServiceResult.Fail(_messageService.Get("OrderNotReadyForDelivery"), 422);

        if (string.IsNullOrWhiteSpace(order.DeliveryOtpHash) || !order.DeliveryOtpExpiresAt.HasValue || order.DeliveryOtpExpiresAt.Value < DateTime.UtcNow)
            return ServiceResult.Fail(_messageService.Get("OtpExpired"), 422);

        try
        {
            if (!OtpHasher.VerifyOtp(otp, order.DeliveryOtpHash))
            {
                order.DeliveryOtpFailedAttempts++;
                var maxFailed = _otpSettings.MaxFailedAttempts > 0 ? _otpSettings.MaxFailedAttempts : 5;

                if (order.DeliveryOtpFailedAttempts >= maxFailed)
                {
                    order.DeliveryOtpHash = null;
                    order.DeliveryOtpExpiresAt = null;
                    await _unitOfWork.SaveChangesAsync();
                    return ServiceResult.Fail(_messageService.Get("OtpExpired"), 422);
                }

                await _unitOfWork.SaveChangesAsync();
                return ServiceResult.Fail(_messageService.Get("InvalidOtp"), 422);
            }

            order.Status = OrderStatus.Delivered;
            order.DeliveryOtpHash = null;
            order.DeliveryOtpExpiresAt = null;
            order.DeliveryOtpLastSentAt = null;
            order.DeliveryOtpResendCount = 0;
            order.DeliveryOtpFailedAttempts = 0;
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.Success(_messageService.Get("OrderDelivered"));
        }
        catch (DbUpdateConcurrencyException)
        {
            return ServiceResult.Fail(_messageService.Get("ConcurrentUpdateConflict"), 409);
        }
    }

    public async Task<ServiceResult> ResendDeliveryOtpAsync(Guid orderId)
    {
        var orderRepo = _unitOfWork.Repository<CardOrder>();
        var order = await orderRepo.GetQueryable()
            .Include(o => o.Tenant)
                .ThenInclude(t => t.Company)
                    .ThenInclude(c => c!.AdminUser)
                        .ThenInclude(u => u!.UserProfile)
            .Include(o => o.User)
                .ThenInclude(u => u.UserProfile)
            .Include(o => o.CardDesign)
                .ThenInclude(d => d!.CardType)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
            return ServiceResult.NotFound(_messageService.Get("RecordNotFound"));

        if (order.Status != OrderStatus.ReadyForDelivery)
            return ServiceResult.Fail(_messageService.Get("OrderNotReadyForDelivery"), 422);

        if (order.DeliveryOtpLastSentAt.HasValue &&
            (DateTime.UtcNow - order.DeliveryOtpLastSentAt.Value).TotalSeconds < _otpSettings.CooldownSeconds)
        {
            return ServiceResult.Fail(_messageService.Get("OtpCooldownActive"), 422);
        }

        if (order.DeliveryOtpResendCount >= _otpSettings.MaxResendAttempts)
        {
            return ServiceResult.Fail(_messageService.Get("OtpResendLimitReached"), 422);
        }

        var recipient = order.Tenant?.Company?.AdminUser ?? order.User;

        try
        {
            var newOtp = GenerateOtp();
            order.DeliveryOtpHash = OtpHasher.HashOtp(newOtp);
            order.DeliveryOtpExpiresAt = DateTime.UtcNow.AddDays(7);
            order.DeliveryOtpLastSentAt = DateTime.UtcNow;
            order.DeliveryOtpResendCount++;
            order.DeliveryOtpFailedAttempts = 0;

            await _unitOfWork.SaveChangesAsync();

            if (recipient != null)
            {
                EnqueueOtpNotifications(recipient, newOtp, order.CardDesign?.CardType?.NameAr ?? "Physical Card", isResend: true);
            }

            return ServiceResult.Success(_messageService.Get("OtpResent"));
        }
        catch (DbUpdateConcurrencyException)
        {
            return ServiceResult.Fail(_messageService.Get("ConcurrentUpdateConflict"), 409);
        }
    }

    public async Task<ServiceResult<PagedResult<TemplateRequestDto>>> GetTemplateRequestsPagedAsync(
        PaginationRequest request, TemplateRequestStatus? status = null, string? search = null, CancellationToken cancellationToken = default)
    {
        var query = _unitOfWork.Repository<TemplateRequest>()
            .GetQueryable()
            .AsNoTracking()
            .Include(r => r.Tenant)
            .Include(r => r.RequestedByUser)
                .ThenInclude(u => u.UserProfile)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(r => r.TemplateName.Contains(search) ||
                                     (r.Notes != null && r.Notes.Contains(search)) ||
                                     (r.Tenant != null && r.Tenant.Name.Contains(search)) ||
                                     (r.RequestedByUser != null && (r.RequestedByUser.Email.Contains(search) || r.RequestedByUser.Username.Contains(search))));
        }

        query = query.OrderByDescending(r => r.CreatedAt);

        var pagedResult = await query.ToPagedResultAsync(request, r => _mapper.Map<TemplateRequestDto>(r), cancellationToken);
        return ServiceResult<PagedResult<TemplateRequestDto>>.Success(pagedResult);
    }

    public async Task<ServiceResult> ResolveTemplateRequestAsync(Guid id, ResolveTemplateRequestDto dto)
    {
        var requestRepo = _unitOfWork.Repository<TemplateRequest>();
        var templateRequest = await requestRepo.GetQueryable()
            .Include(r => r.Tenant)
                .ThenInclude(t => t.Company)
                    .ThenInclude(c => c!.AdminUser)
            .Include(r => r.RequestedByUser)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (templateRequest == null)
            return ServiceResult.NotFound(_messageService.Get("RecordNotFound"));

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            templateRequest.Status = dto.Status;
            if (!string.IsNullOrWhiteSpace(dto.Notes))
            {
                templateRequest.Notes = dto.Notes.StartsWith("Admin Notes:", StringComparison.OrdinalIgnoreCase) ? dto.Notes : $"Admin Notes: {dto.Notes}";
            }

            if (dto.Status == TemplateRequestStatus.Completed)
            {
                var customCategory = _unitOfWork.Repository<TemplateCategory>()
                    .GetQueryable()
                    .FirstOrDefault(c => c.NameEn == "Custom");

                if (customCategory == null)
                {
                    customCategory = new TemplateCategory
                    {
                        NameAr = "مخصص",
                        NameEn = "Custom",
                        IsActive = true,
                        DisplayOrder = 99
                    };
                    await _unitOfWork.Repository<TemplateCategory>().AddAsync(customCategory);
                }

                var customTemplate = new CardTemplate
                {
                    NameAr = templateRequest.TemplateName,
                    NameEn = templateRequest.TemplateName,
                    CategoryId = customCategory.Id,
                    PhotoUrl = templateRequest.ReferenceImageUrl ?? templateRequest.LogoUrl ?? "",
                    IsActive = true,
                    DisplayOrder = 1
                };

                await _unitOfWork.Repository<CardTemplate>().AddAsync(customTemplate);
                templateRequest.ProducedTemplateId = customTemplate.Id;

                var company = await _unitOfWork.Repository<Company>()
                    .GetQueryable()
                    .FirstOrDefaultAsync(c => c.TenantId == templateRequest.TenantId);

                if (company != null)
                {
                    company.ProfileTemplateId = customTemplate.Id;
                }
                else
                {
                    var userProfile = await _unitOfWork.Repository<UserProfile>()
                        .GetQueryable()
                        .FirstOrDefaultAsync(p => p.TenantId == templateRequest.TenantId && p.UserId == templateRequest.RequestedByUserId);

                    if (userProfile != null)
                    {
                        userProfile.ProfileTemplateId = customTemplate.Id;
                    }
                }
            }

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            if (dto.Status == TemplateRequestStatus.Completed)
            {
                var recipientEmail = templateRequest.Tenant?.Company?.AdminUser?.Email ?? templateRequest.RequestedByUser?.Email;
                if (!string.IsNullOrWhiteSpace(recipientEmail))
                {
                    var culture = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
                    if (string.IsNullOrWhiteSpace(culture)) culture = "ar";

                    _backgroundJobClient.Enqueue<IEmailService>(x =>
                        x.SendTemplateRequestApprovedEmailAsync(recipientEmail, templateRequest.TemplateName, culture));
                }
            }
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }

        return ServiceResult.Success(_messageService.Get("RecordUpdated"));
    }

    public async Task<ServiceResult<CardTemplateAdminDto>> CreateTemplateAsync(CreateCardTemplateRequest dto)
    {
        var template = _mapper.Map<CardTemplate>(dto);

        await _unitOfWork.Repository<CardTemplate>().AddAsync(template);
        await _unitOfWork.SaveChangesAsync();

        var resultDto = _mapper.Map<CardTemplateAdminDto>(template);
        return ServiceResult<CardTemplateAdminDto>.Success(resultDto);
    }

    public async Task<ServiceResult<CardTemplateAdminDto>> UpdateTemplateAsync(Guid id, UpdateCardTemplateRequest dto)
    {
        var templateRepo = _unitOfWork.Repository<CardTemplate>();
        var template = await templateRepo.GetByIdAsync(id);

        if (template == null)
            return ServiceResult<CardTemplateAdminDto>.NotFound(_messageService.Get("RecordNotFound"));

        _mapper.Map(dto, template);
        _unitOfWork.Repository<CardTemplate>().Update(template);
        await _unitOfWork.SaveChangesAsync();

        var resultDto = _mapper.Map<CardTemplateAdminDto>(template);
        return ServiceResult<CardTemplateAdminDto>.Success(resultDto);
    }

    public async Task<ServiceResult> DeleteTemplateAsync(Guid id)
    {
        var templateRepo = _unitOfWork.Repository<CardTemplate>();
        var template = await templateRepo.GetByIdAsync(id);

        if (template == null)
            return ServiceResult.NotFound(_messageService.Get("RecordNotFound"));

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            template.IsDeleted = true;
            template.IsActive = false;

            var affectedProfiles = await _unitOfWork.Repository<UserProfile>()
                .GetQueryable()
                .Where(p => p.ProfileTemplateId == id)
                .ToListAsync();

            foreach (var profile in affectedProfiles)
                profile.ProfileTemplateId = null;

            var affectedCompanies = await _unitOfWork.Repository<Company>()
                .GetQueryable()
                .Where(c => c.ProfileTemplateId == id)
                .ToListAsync();

            foreach (var company in affectedCompanies)
                company.ProfileTemplateId = null;

            var planAssignments = await _unitOfWork.Repository<SubscriptionPlanTemplate>()
                .GetQueryable()
                .Where(pt => pt.CardTemplateId == id)
                .ToListAsync();

            foreach (var assignment in planAssignments)
                _unitOfWork.Repository<SubscriptionPlanTemplate>().Remove(assignment);

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }

        return ServiceResult.Success(_messageService.Get("TemplateDeletedAndProfilesCleared"));
    }

    public async Task<ServiceResult<PagedResult<TenantSummaryDto>>> GetTenantsPagedAsync(PaginationRequest request, string? search = null, CancellationToken cancellationToken = default)
    {
        var query = _unitOfWork.Repository<Tenant>()
            .GetQueryable()
            .AsNoTracking()
            .Include(t => t.Company)
            .OrderByDescending(t => t.CreatedAt)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(t => t.Name.Contains(search) ||
                                     (t.Company != null && (t.Company.Name.Contains(search) || (t.Company.Activity != null && t.Company.Activity.Contains(search)) || (t.Company.CommercialRegistry != null && t.Company.CommercialRegistry.Contains(search)))));
        }

        var pagedTenants = await query.ToPagedResultAsync(request, t => t, cancellationToken);
        var tenantIds = pagedTenants.Items.Select(t => t.Id).ToList();

        var activeSubscriptions = await _unitOfWork.Repository<UserSubscription>()
            .GetQueryable()
            .AsNoTracking()
            .Include(us => us.SubscriptionPlan)
            .Where(us => tenantIds.Contains(us.TenantId) && us.IsActive)
            .ToListAsync(cancellationToken);

        var activeSubByTenant = activeSubscriptions
            .GroupBy(us => us.TenantId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(us => us.EndDate).FirstOrDefault()
            );

        var tenantSummaryDtos = new List<TenantSummaryDto>();
        foreach (var tenant in pagedTenants.Items)
        {
            var dto = _mapper.Map<TenantSummaryDto>(tenant);
            dto.AccountType = tenant.Company != null
                ? _messageService.Get("AccountTypeCompany")
                : _messageService.Get("AccountTypeIndividual");

            if (activeSubByTenant.TryGetValue(tenant.Id, out var activeSub) && activeSub != null)
            {
                dto.ActivePlanName = activeSub.SubscriptionPlan != null
                    ? (CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar" ? activeSub.SubscriptionPlan.NameAr : activeSub.SubscriptionPlan.NameEn)
                    : null;
                dto.SubscriptionStartDate = activeSub.StartDate;
                dto.SubscriptionExpiry = activeSub.EndDate;
                dto.DaysRemaining = Math.Max(0, (int)(activeSub.EndDate - DateTime.UtcNow).TotalDays);
            }
            else
            {
                dto.ActivePlanName = _messageService.Get("FreeNoActivePlan");
                dto.DaysRemaining = 0;
            }

            tenantSummaryDtos.Add(dto);
        }

        var result = PagedResult<TenantSummaryDto>.Create(
            tenantSummaryDtos,
            pagedTenants.TotalCount,
            pagedTenants.PageNumber,
            pagedTenants.PageSize
        );

        return ServiceResult<PagedResult<TenantSummaryDto>>.Success(result);
    }

    public async Task<ServiceResult> UpdateTenantStatusAsync(Guid id, UpdateTenantStatusDto dto)
    {
        var tenantRepo = _unitOfWork.Repository<Tenant>();
        var tenant = await tenantRepo.GetByIdAsync(id);

        if (tenant == null)
            return ServiceResult.NotFound(_messageService.Get("RecordNotFound"));

        tenant.IsActive = dto.IsActive;
        await _unitOfWork.SaveChangesAsync();

        return ServiceResult.Success(_messageService.Get("RecordUpdated"));
    }

    public async Task<ServiceResult<TenantBasicInfoDto>> GetTenantBasicInfoAsync(Guid tenantId)
    {
        var tenant = await _unitOfWork.Repository<Tenant>()
            .GetQueryable()
            .AsNoTracking()
            .Include(t => t.Company)
                .ThenInclude(c => c!.AdminUser)
            .Include(t => t.Company)
                .ThenInclude(c => c!.ProfileTemplate)
            .FirstOrDefaultAsync(t => t.Id == tenantId);

        if (tenant == null)
            return ServiceResult<TenantBasicInfoDto>.NotFound(_messageService.Get("RecordNotFound"));

        var dto = _mapper.Map<TenantBasicInfoDto>(tenant);
        return ServiceResult<TenantBasicInfoDto>.Success(dto);
    }

    public async Task<ServiceResult<PagedResult<EmployeeDto>>> GetTenantEmployeesPagedAsync(Guid tenantId, PaginationRequest request, string? search = null)
    {
        var query = _unitOfWork.Repository<Employee>()
            .GetQueryable()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(e => e.TenantId == tenantId && !e.IsDeleted)
            .Include(e => e.UserProfile)
            .OrderByDescending(e => e.CreatedAt)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(e => e.FullName.Contains(search) ||
                                     e.Email.Contains(search) ||
                                     e.JobTitle.Contains(search) ||
                                     e.Department.Contains(search) ||
                                     (e.UserProfile != null && (
                                         (e.UserProfile.Phone != null && e.UserProfile.Phone.Contains(search)) ||
                                         (e.UserProfile.Subdomain != null && e.UserProfile.Subdomain.Contains(search))
                                     )));
        }

        var pagedResult = await query.ToPagedResultAsync(request, e => _mapper.Map<EmployeeDto>(e));
        return ServiceResult<PagedResult<EmployeeDto>>.Success(pagedResult);
    }

    public async Task<ServiceResult<EmployeeDetailsDto>> GetTenantEmployeeDetailsAsync(Guid tenantId, Guid employeeId)
    {
        var employee = await _unitOfWork.Repository<Employee>()
            .GetQueryable()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(e => e.TenantId == tenantId && e.Id == employeeId && !e.IsDeleted)
            .Include(e => e.UserProfile)
                .ThenInclude(p => p!.CustomLinks)
            .Include(e => e.Company)
            .FirstOrDefaultAsync();

        if (employee == null)
            return ServiceResult<EmployeeDetailsDto>.NotFound(_messageService.Get("RecordNotFound"));

        var dto = _mapper.Map<EmployeeDetailsDto>(employee);

        if (employee.Company != null)
        {
            dto.CompanyName = employee.Company.Name;
        }

        return ServiceResult<EmployeeDetailsDto>.Success(dto);
    }

    public async Task<ServiceResult<PagedResult<SubscriptionPlanAdminDto>>> GetAllAdminPlansAsync(
        PaginationRequest request, string? search = null, CancellationToken cancellationToken = default)
    {
        var query = _unitOfWork.Repository<SubscriptionPlan>()
            .GetQueryable()
            .AsNoTracking()
            .Include(p => p.PlanTemplates)
                .ThenInclude(pt => pt.CardTemplate)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(p => (p.NameAr != null && p.NameAr.Contains(search)) ||
                                     (p.NameEn != null && p.NameEn.Contains(search)));
        }

        query = query.OrderByDescending(p => p.CreatedAt);

        var pagedResult = await query.ToPagedResultAsync(request, p => _mapper.Map<SubscriptionPlanAdminDto>(p), cancellationToken);
        return ServiceResult<PagedResult<SubscriptionPlanAdminDto>>.Success(pagedResult);
    }

    public async Task<ServiceResult<SubscriptionPlanAdminDto>> GetPlanByIdAsync(Guid id)
    {
        var plan = await _unitOfWork.Repository<SubscriptionPlan>()
            .GetQueryable()
            .AsNoTracking()
            .Include(p => p.PlanTemplates)
                .ThenInclude(pt => pt.CardTemplate)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (plan == null)
            return ServiceResult<SubscriptionPlanAdminDto>.NotFound(_messageService.Get("RecordNotFound"));

        var dto = _mapper.Map<SubscriptionPlanAdminDto>(plan);
        return ServiceResult<SubscriptionPlanAdminDto>.Success(dto);
    }

    public async Task<ServiceResult<SubscriptionPlanAdminDto>> CreatePlanAsync(CreateSubscriptionPlanRequest request)
    {
        var trimmedAr = request.NameAr?.Trim();
        if (!string.IsNullOrWhiteSpace(trimmedAr))
        {
            var nameArExists = await _unitOfWork.Repository<SubscriptionPlan>()
                .GetQueryable()
                .AnyAsync(p => p.NameAr.Trim() == trimmedAr);
            if (nameArExists)
                return ServiceResult<SubscriptionPlanAdminDto>.Fail(_messageService.Get("DuplicateNameAr"), 400);
        }

        var trimmedEn = request.NameEn?.Trim();
        if (!string.IsNullOrWhiteSpace(trimmedEn))
        {
            var nameEnExists = await _unitOfWork.Repository<SubscriptionPlan>()
                .GetQueryable()
                .AnyAsync(p => p.NameEn.Trim() == trimmedEn);
            if (nameEnExists)
                return ServiceResult<SubscriptionPlanAdminDto>.Fail(_messageService.Get("DuplicateNameEn"), 400);
        }

        var plan = _mapper.Map<SubscriptionPlan>(request);
        await _unitOfWork.Repository<SubscriptionPlan>().AddAsync(plan);
        await _unitOfWork.SaveChangesAsync();

        if (request.TemplateIds?.Count > 0)
        {
            foreach (var templateId in request.TemplateIds)
            {
                var template = await _unitOfWork.Repository<CardTemplate>().GetByIdAsync(templateId);
                if (template != null)
                {
                    await _unitOfWork.Repository<SubscriptionPlanTemplate>().AddAsync(new SubscriptionPlanTemplate
                    {
                        SubscriptionPlanId = plan.Id,
                        CardTemplateId = templateId
                    });
                }
            }
            await _unitOfWork.SaveChangesAsync();
        }

        var dto = _mapper.Map<SubscriptionPlanAdminDto>(plan);
        return ServiceResult<SubscriptionPlanAdminDto>.Success(dto, _messageService.Get("RecordCreated"));
    }

    public async Task<ServiceResult<SubscriptionPlanAdminDto>> UpdatePlanAsync(Guid planId, UpdateSubscriptionPlanRequest request)
    {
        var plan = await _unitOfWork.Repository<SubscriptionPlan>().GetByIdAsync(planId);
        if (plan == null)
            return ServiceResult<SubscriptionPlanAdminDto>.NotFound(_messageService.Get("RecordNotFound"));

        if (!string.IsNullOrWhiteSpace(request.NameAr))
        {
            var trimmedAr = request.NameAr.Trim();
            var nameArExists = await _unitOfWork.Repository<SubscriptionPlan>()
                .GetQueryable()
                .AnyAsync(p => p.NameAr.Trim() == trimmedAr && p.Id != planId);
            if (nameArExists)
                return ServiceResult<SubscriptionPlanAdminDto>.Fail(_messageService.Get("DuplicateNameAr"), 400);
        }

        if (!string.IsNullOrWhiteSpace(request.NameEn))
        {
            var trimmedEn = request.NameEn.Trim();
            var nameEnExists = await _unitOfWork.Repository<SubscriptionPlan>()
                .GetQueryable()
                .AnyAsync(p => p.NameEn.Trim() == trimmedEn && p.Id != planId);
            if (nameEnExists)
                return ServiceResult<SubscriptionPlanAdminDto>.Fail(_messageService.Get("DuplicateNameEn"), 400);
        }

        _mapper.Map(request, plan);
        _unitOfWork.Repository<SubscriptionPlan>().Update(plan);

        if (request.TemplateIds != null)
        {
            var existingAssignments = await _unitOfWork.Repository<SubscriptionPlanTemplate>()
                .GetQueryable()
                .Where(pt => pt.SubscriptionPlanId == planId)
                .ToListAsync();

            foreach (var existing in existingAssignments)
            {
                _unitOfWork.Repository<SubscriptionPlanTemplate>().Remove(existing);
            }

            foreach (var templateId in request.TemplateIds)
            {
                await _unitOfWork.Repository<SubscriptionPlanTemplate>().AddAsync(new SubscriptionPlanTemplate
                {
                    SubscriptionPlanId = planId,
                    CardTemplateId = templateId
                });
            }
        }

        await _unitOfWork.SaveChangesAsync();

        var dto = _mapper.Map<SubscriptionPlanAdminDto>(plan);
        return ServiceResult<SubscriptionPlanAdminDto>.Success(dto, _messageService.Get("RecordUpdated"));
    }

    public async Task<ServiceResult> DeletePlanAsync(Guid planId)
    {
        var plan = await _unitOfWork.Repository<SubscriptionPlan>().GetByIdAsync(planId);
        if (plan == null)
            return ServiceResult.NotFound(_messageService.Get("RecordNotFound"));

        var hasSubscriptions = await _unitOfWork.Repository<UserSubscription>()
            .GetQueryable().AnyAsync(s => s.SubscriptionPlanId == planId && s.IsActive);

        if (hasSubscriptions)
            return ServiceResult.Fail(_messageService.Get("PlanHasActiveSubscriptions"), 409);

        _unitOfWork.Repository<SubscriptionPlan>().Remove(plan);
        await _unitOfWork.SaveChangesAsync();

        return ServiceResult.Success(_messageService.Get("RecordDeleted"));
    }

    public async Task<ServiceResult<IReadOnlyList<CardTemplateSummaryDto>>> GetPlanTemplatesAsync(Guid planId)
    {
        var templates = await _unitOfWork.Repository<SubscriptionPlanTemplate>()
            .GetQueryable()
            .AsNoTracking()
            .Include(pt => pt.CardTemplate)
            .Where(pt => pt.SubscriptionPlanId == planId)
            .Select(pt => pt.CardTemplate)
            .ToListAsync();

        var dtos = _mapper.Map<IReadOnlyList<CardTemplateSummaryDto>>(templates);
        return ServiceResult<IReadOnlyList<CardTemplateSummaryDto>>.Success(dtos);
    }

    public async Task<ServiceResult> AssignTemplateAsync(Guid planId, Guid templateId)
    {
        var plan = await _unitOfWork.Repository<SubscriptionPlan>().GetByIdAsync(planId);
        if (plan == null)
            return ServiceResult.NotFound(_messageService.Get("RecordNotFound"));

        var template = await _unitOfWork.Repository<CardTemplate>().GetByIdAsync(templateId);
        if (template == null)
            return ServiceResult.NotFound(_messageService.Get("RecordNotFound"));

        var exists = await _unitOfWork.Repository<SubscriptionPlanTemplate>()
            .GetQueryable()
            .AsNoTracking()
            .AnyAsync(pt => pt.SubscriptionPlanId == planId && pt.CardTemplateId == templateId);

        if (exists)
            return ServiceResult.Fail(_messageService.Get("TemplateAlreadyAssigned"), 409);

        var assignment = new SubscriptionPlanTemplate
        {
            SubscriptionPlanId = planId,
            CardTemplateId = templateId
        };

        await _unitOfWork.Repository<SubscriptionPlanTemplate>().AddAsync(assignment);
        await _unitOfWork.SaveChangesAsync();

        return ServiceResult.Success(_messageService.Get("RecordCreated"));
    }

    public async Task<ServiceResult> UnassignTemplateAsync(Guid planId, Guid templateId)
    {
        var assignment = await _unitOfWork.Repository<SubscriptionPlanTemplate>()
            .GetQueryable()
            .FirstOrDefaultAsync(pt => pt.SubscriptionPlanId == planId && pt.CardTemplateId == templateId);

        if (assignment == null)
            return ServiceResult.NotFound(_messageService.Get("RecordNotFound"));

        _unitOfWork.Repository<SubscriptionPlanTemplate>().Remove(assignment);
        await _unitOfWork.SaveChangesAsync();

        return ServiceResult.Success(_messageService.Get("TemplateUnassigned"));
    }

    private static bool IsValidStatusTransition(OrderStatus current, OrderStatus next)
    {
        if (current == next) return true;

        return current switch
        {
            OrderStatus.PendingReview => next is OrderStatus.UnderReview or OrderStatus.Approved or OrderStatus.Rejected or OrderStatus.Cancelled,
            OrderStatus.UnderReview => next is OrderStatus.Approved or OrderStatus.Rejected or OrderStatus.Cancelled,
            OrderStatus.Approved => next is OrderStatus.InPrinting or OrderStatus.ReadyForDelivery or OrderStatus.Cancelled,
            OrderStatus.InPrinting => next is OrderStatus.Encoding or OrderStatus.ReadyForDelivery or OrderStatus.Cancelled,
            OrderStatus.Encoding => next is OrderStatus.ReadyForDelivery or OrderStatus.Cancelled,
            OrderStatus.ReadyForDelivery => next is OrderStatus.Delivered or OrderStatus.Cancelled,
            OrderStatus.Delivered => false,
            OrderStatus.Rejected => false,
            OrderStatus.Cancelled => false,
            _ => false,
        };
    }

    private static string GenerateOtp()
    {
        var bytes = RandomNumberGenerator.GetBytes(4);
        var value = BitConverter.ToUInt32(bytes, 0) % 1_000_000;
        return value.ToString("D6");
    }

    private void EnqueueOtpNotifications(User recipient, string otp, string cardName, bool isResend)
    {
        var culture = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        if (string.IsNullOrWhiteSpace(culture)) culture = "ar";

        var templateKey = isResend ? "WhatsAppNewOtp" : "WhatsAppOrderReady";

        if (!string.IsNullOrWhiteSpace(recipient.Email))
            _backgroundJobClient.Enqueue<IEmailService>(x =>
                x.SendOrderReadyOtpEmailAsync(recipient.Email, otp, cardName, culture));

        var whatsAppNumber = recipient.UserProfile?.WhatsApp;
        if (!string.IsNullOrWhiteSpace(whatsAppNumber))
            _backgroundJobClient.Enqueue<IWhatsAppService>(x =>
                x.SendWhatsAppMessageAsync(whatsAppNumber, _messageService.Get(templateKey, otp)));
    }


    public async Task<ServiceResult<PagedResult<ProfileSubdomainSummaryDto>>> GetSubdomainsPagedAsync(
        PaginationRequest request, string? search = null, CancellationToken cancellationToken = default)
    {
        var query = _unitOfWork.Repository<UserProfile>()
            .GetQueryable()
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(p => !p.IsDeleted)
            .Include(p => p.Employee)
                .ThenInclude(e => e!.Company)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(p => (p.Subdomain != null && p.Subdomain.Contains(search)) ||
                                     (p.FullName != null && p.FullName.Contains(search)) ||
                                     (p.ContactEmail != null && p.ContactEmail.Contains(search)) ||
                                     (p.Employee != null && (p.Employee.Email.Contains(search) || (p.Employee.Company != null && p.Employee.Company.Name.Contains(search)))));
        }

        query = query.OrderBy(p => p.FullName);

        var paged = await query.ToPagedResultAsync(request, p => _mapper.Map<ProfileSubdomainSummaryDto>(p), cancellationToken);
        return ServiceResult<PagedResult<ProfileSubdomainSummaryDto>>.Success(paged);
    }

    public async Task<ServiceResult> ReassignSubdomainAsync(Guid profileId, ReassignSubdomainDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Subdomain))
            return ServiceResult.Fail(_messageService.Get("InvalidInput"), 400);

        var slugRegex = new System.Text.RegularExpressions.Regex(@"^[a-z0-9][a-z0-9\-]{0,98}[a-z0-9]$");
        if (!slugRegex.IsMatch(dto.Subdomain))
            return ServiceResult.Fail(_messageService.Get("InvalidInput"), 400);

        var reserved = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "admin", "api", "auth", "u", "www", "mail", "support" };
        if (reserved.Contains(dto.Subdomain))
            return ServiceResult.Fail(_messageService.Get("InvalidInput"), 400);

        var profile = await _unitOfWork.Repository<UserProfile>()
            .GetQueryable()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == profileId && !p.IsDeleted);

        if (profile == null)
            return ServiceResult.NotFound(_messageService.Get("RecordNotFound"));

        var taken = await _unitOfWork.Repository<UserProfile>()
            .GetQueryable()
            .IgnoreQueryFilters()
            .AnyAsync(p => p.Subdomain == dto.Subdomain && p.Id != profileId && !p.IsDeleted);

        if (taken)
            return ServiceResult.Fail(_messageService.Get("UserAlreadyExists"), 409);

        profile.Subdomain = dto.Subdomain;
        await _unitOfWork.SaveChangesAsync();

        return ServiceResult.Success(_messageService.Get("RecordUpdated"));
    }
}
