using System;
using AutoMapper;
using NFC.Platform.Application.DTOs.Template;
using NFC.Platform.Application.Mapping;
using NFC.Platform.Domain.Entities;
using NFC.Platform.Domain.Enums;
using Xunit;

namespace NFC.Platform.Tests.Mapping
{
    public class TemplateRequestMappingProfileTests
    {
        private readonly IMapper _mapper;

        public TemplateRequestMappingProfileTests()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<TemplateRequestMappingProfile>();
            });
            _mapper = config.CreateMapper();
        }

        [Fact]
        public void TemplateRequest_To_TemplateRequestDto_PopulatesFullNameEmailAndTenantName()
        {
            var tenantId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var request = new TemplateRequest
            {
                Id = Guid.NewGuid(),
                TemplateName = "Luxury Design",
                Status = TemplateRequestStatus.Pending,
                RequestType = TemplateRequestType.ProfileTemplate,
                Tenant = new Tenant { Id = tenantId, Name = "ACME International" },
                RequestedByUser = new User
                {
                    Id = userId,
                    Username = "john_doe",
                    Email = "john@acme.com",
                    UserProfile = new UserProfile { FullName = "Johnathan Doe" }
                }
            };

            var dto = _mapper.Map<TemplateRequestDto>(request);

            Assert.NotNull(dto);
            Assert.Equal("Johnathan Doe", dto.RequestedByUsername);
            Assert.Equal("john@acme.com", dto.RequestedByEmail);
            Assert.Equal("ACME International", dto.TenantName);
            Assert.Equal("Pending", dto.Status);
            Assert.Equal("ProfileTemplate", dto.RequestType);
        }

        [Fact]
        public void TemplateRequest_To_TemplateRequestDto_FallsBackToUsername_WhenFullNameEmpty()
        {
            var request = new TemplateRequest
            {
                Id = Guid.NewGuid(),
                TemplateName = "Simple Layout",
                RequestedByUser = new User
                {
                    Username = "fallback_user",
                    Email = "user@test.com",
                    UserProfile = new UserProfile { FullName = "" }
                }
            };

            var dto = _mapper.Map<TemplateRequestDto>(request);

            Assert.Equal("fallback_user", dto.RequestedByUsername);
            Assert.Equal("user@test.com", dto.RequestedByEmail);
        }
    }
}
