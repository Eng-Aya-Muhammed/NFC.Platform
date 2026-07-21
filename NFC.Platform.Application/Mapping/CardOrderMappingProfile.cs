namespace NFC.Platform.Application.Mapping
{
    public class CardOrderMappingProfile : Profile
    {
        public CardOrderMappingProfile()
        {
            CreateMap<CardOrder, CardOrder>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.TenantId, opt => opt.Ignore())
                .ForMember(dest => dest.Tenant, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.User, opt => opt.Ignore())
                .ForMember(dest => dest.ParentOrderId, opt => opt.Ignore())
                .ForMember(dest => dest.ParentOrder, opt => opt.Ignore())
                .ForMember(dest => dest.Quantity, opt => opt.Ignore())
                .ForMember(dest => dest.ExcelDataUrl, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.RejectionReason, opt => opt.Ignore())
                .ForMember(dest => dest.TotalPrice, opt => opt.Ignore())
                .ForMember(dest => dest.UnitPrice, opt => opt.Ignore())
                .ForMember(dest => dest.Currency, opt => opt.Ignore())
                .ForMember(dest => dest.TrackingNumber, opt => opt.Ignore())
                .ForMember(dest => dest.DeliveryOtp, opt => opt.Ignore())
                .ForMember(dest => dest.DeliveryOtpExpiresAt, opt => opt.Ignore())
                .ForMember(dest => dest.DeliveryOtpLastSentAt, opt => opt.Ignore())
                .ForMember(dest => dest.DeliveryOtpResendCount, opt => opt.Ignore())
                .ForMember(dest => dest.DeliveryMethod, opt => opt.Ignore())
                .ForMember(dest => dest.ShippingAddress, opt => opt.Ignore())
                .ForMember(dest => dest.Items, opt => opt.Ignore());

            CreateMap<CardOrder, CardOrderDto>()
                .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items));

            CreateMap<CardOrderItem, CardOrderItemDto>();

            CreateMap<CardPricing, CardPricingDto>();

            CreateMap<CreateCardOrderRequest, CardOrder>()
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.TotalPrice, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.TenantId, opt => opt.Ignore())
                .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items));

            CreateMap<CreateCardOrderItemRequest, CardOrderItem>()
                .ForMember(dest => dest.TenantId, opt => opt.Ignore())
                .ForMember(dest => dest.CardOrderId, opt => opt.Ignore());

            // Excel import mappings to avoid manual mapping in CardOrderService
            CreateMap<ExcelEmployeeImportDto, Employee>()
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.JobTitle, opt => opt.MapFrom(src => src.JobTitle ?? string.Empty))
                .ForMember(dest => dest.Department, opt => opt.MapFrom(src => src.Department ?? string.Empty))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(_ => UserStatus.Active))
                .ForMember(dest => dest.TenantId, opt => opt.Ignore())
                .ForMember(dest => dest.CompanyId, opt => opt.Ignore())
                .ForMember(dest => dest.UserProfile, opt => opt.Ignore());

            CreateMap<ExcelEmployeeImportDto, UserProfile>()
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.JobTitle, opt => opt.MapFrom(src => src.JobTitle ?? string.Empty))
                .ForMember(dest => dest.Department, opt => opt.MapFrom(src => src.Department))
                .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => src.Phone))
                .ForMember(dest => dest.TenantId, opt => opt.Ignore())
                .ForMember(dest => dest.EmployeeId, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.CompanyName, opt => opt.Ignore())
                .ForMember(dest => dest.CustomLinks, opt => opt.Ignore());

            CreateMap<ExcelEmployeeImportDto, CardOrderItem>()
                .ForMember(dest => dest.EmployeeName, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.UserProfileId, opt => opt.Ignore())
                .ForMember(dest => dest.TenantId, opt => opt.Ignore())
                .ForMember(dest => dest.CardOrderId, opt => opt.Ignore());

            CreateMap<Employee, CardOrderItem>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.EmployeeName, opt => opt.MapFrom(src => src.FullName))
                .ForMember(dest => dest.UserProfileId, opt => opt.MapFrom(src => src.UserProfile != null ? (Guid?)src.UserProfile.Id : null))
                .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => src.UserProfile != null ? src.UserProfile.Phone : null))
                .ForMember(dest => dest.CardOrderId, opt => opt.Ignore())
                .ForMember(dest => dest.TenantId, opt => opt.MapFrom(src => src.TenantId));

            CreateMap<UserProfile, CardOrderItem>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.EmployeeName, opt => opt.MapFrom(src => src.FullName))
                .ForMember(dest => dest.UserProfileId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => src.Phone))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Employee != null ? src.Employee.Email : src.ContactEmail))
                .ForMember(dest => dest.CardOrderId, opt => opt.Ignore())
                .ForMember(dest => dest.TenantId, opt => opt.MapFrom(src => src.TenantId));

            CreateMap<CardOrder, EmployeesImportStatusDto>()
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.TotalRows, opt => opt.MapFrom(src => src.Items.Count))
                .ForMember(dest => dest.Imported, opt => opt.MapFrom(src => src.Items.Count))
                .ForMember(dest => dest.Skipped, opt => opt.MapFrom(_ => 0))
                .ForMember(dest => dest.Errors, opt => opt.MapFrom(_ => new List<string>()));

            CreateMap<EmployeeImportJob, EmployeesImportStatusDto>()
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
                .ForMember(dest => dest.Errors, opt => opt.MapFrom(src => string.IsNullOrWhiteSpace(src.ErrorsJson) 
                    ? new List<string>() 
                    : System.Text.Json.JsonSerializer.Deserialize<List<string>>(src.ErrorsJson, (System.Text.Json.JsonSerializerOptions)null!) ?? new List<string>()));
        }
    }
}
