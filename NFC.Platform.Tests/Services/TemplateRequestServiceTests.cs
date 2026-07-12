using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using MockQueryable.NSubstitute;
using NFC.Platform.Application.DTOs;
using NFC.Platform.Application.Interfaces.Repositories;
using NFC.Platform.Application.Services;
using NFC.Platform.BuildingBlocks.Common.Helpers;
using NFC.Platform.BuildingBlocks.Localization;
using NFC.Platform.Domain.Entities;
using NFC.Platform.Domain.Enums;
using NSubstitute;
using Xunit;

namespace NFC.Platform.Tests.Services
{
    public class TemplateRequestServiceTests
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IMessageService _messageService;
        private readonly ICurrentTenant _currentTenant;

        private readonly IGenericRepository<TemplateRequest> _templateRequestRepo;

        private readonly TemplateRequestService _sut;

        public TemplateRequestServiceTests()
        {
            _unitOfWork = Substitute.For<IUnitOfWork>();
            _mapper = Substitute.For<IMapper>();
            _messageService = Substitute.For<IMessageService>();
            _currentTenant = Substitute.For<ICurrentTenant>();

            _templateRequestRepo = Substitute.For<IGenericRepository<TemplateRequest>>();
            _unitOfWork.Repository<TemplateRequest>().Returns(_templateRequestRepo);

            _sut = new TemplateRequestService(_unitOfWork, _mapper, _messageService, _currentTenant);
        }

        // ── CreateRequestAsync ────────────────────────────────────────────────────

        [Fact]
        public async Task CreateRequestAsync_ReturnsUnauthorized_WhenTenantNotAuthenticated()
        {
            // Arrange
            _currentTenant.TenantId.Returns((Guid?)null);
            _messageService.Get("Unauthorized").Returns("Unauthorized.");

            var request = new CreateTemplateRequest { TemplateName = "Premium Blue" };

            // Act
            var result = await _sut.CreateRequestAsync(Guid.NewGuid(), request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(401, result.StatusCode);
        }

        [Fact]
        public async Task CreateRequestAsync_ReturnsUnauthorized_WithFallbackMessage_WhenMessageIsEmpty()
        {
            // Arrange
            _currentTenant.TenantId.Returns((Guid?)null);
            _messageService.Get("Unauthorized").Returns(string.Empty);

            // Act
            var result = await _sut.CreateRequestAsync(Guid.NewGuid(), new CreateTemplateRequest());

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(401, result.StatusCode);
            Assert.NotEmpty(result.Message!);
        }

        [Fact]
        public async Task CreateRequestAsync_ReturnsSuccess_AndSetsStatusToPending()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tenantId = Guid.NewGuid();
            _currentTenant.TenantId.Returns(tenantId);

            var request = new CreateTemplateRequest
            {
                TemplateName = "Premium Blue",
                LogoUrl = "https://logo.png",
                ReferenceImageUrl = "https://ref.png",
                Notes = "Make it pop"
            };

            var dto = new TemplateRequestDto { TemplateName = "Premium Blue", Status = "Pending" };

            var createdQueryable = new List<TemplateRequest>
            {
                new() { Id = Guid.NewGuid(), Status = TemplateRequestStatus.Pending, RequestedByUser = new User() }
            }.AsQueryable().BuildMock();

            _templateRequestRepo.GetQueryable().Returns(createdQueryable);
            _mapper.Map<TemplateRequest>(request).Returns(new TemplateRequest
            {
                TemplateName = request.TemplateName,
                LogoUrl = request.LogoUrl,
                ReferenceImageUrl = request.ReferenceImageUrl,
                Notes = request.Notes
            });
            _mapper.Map<TemplateRequestDto>(Arg.Any<TemplateRequest>()).Returns(dto);
            _messageService.Get("RecordCreated").Returns("Record created.");

            // Act
            var result = await _sut.CreateRequestAsync(userId, request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(200, result.StatusCode);
            Assert.Equal("Pending", result.Data!.Status);
            await _templateRequestRepo.Received(1).AddAsync(Arg.Is<TemplateRequest>(r =>
                r.RequestedByUserId == userId &&
                r.Status == TemplateRequestStatus.Pending &&
                r.TemplateName == "Premium Blue"));
            await _unitOfWork.Received(1).SaveChangesAsync();
        }

        // ── GetTenantRequestsAsync ────────────────────────────────────────────────

        [Fact]
        public async Task GetTenantRequestsAsync_ReturnsEmptyList_WhenNoRequestsExist()
        {
            // Arrange
            var emptyQueryable = new List<TemplateRequest>().AsQueryable().BuildMock();
            _templateRequestRepo.GetQueryable().Returns(emptyQueryable);
            _mapper.Map<IReadOnlyList<TemplateRequestDto>>(Arg.Any<object>())
                .Returns(new List<TemplateRequestDto>());

            // Act
            var result = await _sut.GetTenantRequestsAsync();

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Empty(result.Data!);
        }

        [Fact]
        public async Task GetTenantRequestsAsync_ReturnsRequestsOrderedByDateDescending()
        {
            // Arrange
            var older = new TemplateRequest { Id = Guid.NewGuid(), TemplateName = "First", CreatedAt = DateTime.UtcNow.AddDays(-2), RequestedByUser = new User() };
            var newer = new TemplateRequest { Id = Guid.NewGuid(), TemplateName = "Second", CreatedAt = DateTime.UtcNow,            RequestedByUser = new User() };

            var queryable = new List<TemplateRequest> { older, newer }.AsQueryable().BuildMock();
            _templateRequestRepo.GetQueryable().Returns(queryable);

            var dtos = new List<TemplateRequestDto>
            {
                new() { TemplateName = "Second" },
                new() { TemplateName = "First" }
            };
            _mapper.Map<IReadOnlyList<TemplateRequestDto>>(Arg.Any<object>()).Returns(dtos);

            // Act
            var result = await _sut.GetTenantRequestsAsync();

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Data!.Count);
            Assert.Equal("Second", result.Data![0].TemplateName);
        }

        // ── UpdateRequestStatusAsync ──────────────────────────────────────────────

        [Fact]
        public async Task UpdateRequestStatusAsync_ReturnsNotFound_WhenRequestDoesNotExist()
        {
            // Arrange
            var id = Guid.NewGuid();
            var emptyQueryable = new List<TemplateRequest>().AsQueryable().BuildMock();
            _templateRequestRepo.GetQueryable().Returns(emptyQueryable);
            _messageService.Get("RecordNotFound").Returns("Not found.");

            // Act
            var result = await _sut.UpdateRequestStatusAsync(id, TemplateRequestStatus.Completed);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task UpdateRequestStatusAsync_ReturnsSuccess_AndUpdatesStatus()
        {
            // Arrange
            var id = Guid.NewGuid();
            var templateRequest = new TemplateRequest
            {
                Id = id,
                Status = TemplateRequestStatus.Pending,
                RequestedByUser = new User()
            };

            var queryable = new List<TemplateRequest> { templateRequest }.AsQueryable().BuildMock();
            _templateRequestRepo.GetQueryable().Returns(queryable);

            var dto = new TemplateRequestDto { Status = "Completed" };
            _mapper.Map<TemplateRequestDto>(templateRequest).Returns(dto);
            _messageService.Get("RecordUpdated").Returns("Updated.");

            // Act
            var result = await _sut.UpdateRequestStatusAsync(id, TemplateRequestStatus.Completed);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(TemplateRequestStatus.Completed, templateRequest.Status);
            _templateRequestRepo.Received(1).Update(templateRequest);
            await _unitOfWork.Received(1).SaveChangesAsync();
            Assert.Equal("Completed", result.Data!.Status);
        }

        [Fact]
        public async Task UpdateRequestStatusAsync_CanSetStatusToRejected()
        {
            // Arrange
            var id = Guid.NewGuid();
            var templateRequest = new TemplateRequest
            {
                Id = id,
                Status = TemplateRequestStatus.Pending,
                RequestedByUser = new User()
            };

            var queryable = new List<TemplateRequest> { templateRequest }.AsQueryable().BuildMock();
            _templateRequestRepo.GetQueryable().Returns(queryable);

            var dto = new TemplateRequestDto { Status = "Rejected" };
            _mapper.Map<TemplateRequestDto>(templateRequest).Returns(dto);
            _messageService.Get("RecordUpdated").Returns("Updated.");

            // Act
            var result = await _sut.UpdateRequestStatusAsync(id, TemplateRequestStatus.Rejected);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(TemplateRequestStatus.Rejected, templateRequest.Status);
        }
    }
}
