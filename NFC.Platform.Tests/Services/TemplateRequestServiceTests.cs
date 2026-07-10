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

        [Fact]
        public async Task CreateRequestAsync_ReturnsUnauthorized_WhenTenantNotAuthenticated()
        {
            // Arrange
            _currentTenant.TenantId.Returns((Guid?)null);

            // Act
            var result = await _sut.CreateRequestAsync(Guid.NewGuid(), new CreateTemplateRequest { TemplateName = "Sales Template", Notes = "Black and gold style" });

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(401, result.StatusCode);
        }

        [Fact]
        public async Task CreateRequestAsync_Success_SavesPendingRequest()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            _currentTenant.TenantId.Returns(tenantId);

            var request = new CreateTemplateRequest { TemplateName = "Sales Template", Notes = "Premium dark theme" };
            
            var savedRequest = new TemplateRequest
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                RequestedByUserId = userId,
                TemplateName = request.TemplateName,
                Notes = request.Notes,
                Status = TemplateRequestStatus.Pending
            };

            var queryable = new List<TemplateRequest> { savedRequest }.BuildMock();
            _templateRequestRepo.GetQueryable().Returns(queryable);

            var dto = new TemplateRequestDto
            {
                Id = savedRequest.Id,
                RequestedByUserId = userId,
                TemplateName = request.TemplateName,
                Notes = request.Notes,
                Status = "Pending"
            };

            _mapper.Map<TemplateRequestDto>(Arg.Any<TemplateRequest>()).Returns(dto);
            _messageService.Get("RecordCreated").Returns("Request submitted.");

            // Act
            var result = await _sut.CreateRequestAsync(userId, request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(200, result.StatusCode);
            Assert.Equal("Pending", result.Data.Status);
            await _templateRequestRepo.Received(1).AddAsync(Arg.Any<TemplateRequest>());
            await _unitOfWork.Received(1).SaveChangesAsync();
        }

        [Fact]
        public async Task UpdateRequestStatusAsync_ReturnsNotFound_WhenRequestDoesNotExist()
        {
            // Arrange
            var queryable = new List<TemplateRequest>().BuildMock();
            _templateRequestRepo.GetQueryable().Returns(queryable);
            _messageService.Get("RecordNotFound").Returns("Request not found.");

            // Act
            var result = await _sut.UpdateRequestStatusAsync(Guid.NewGuid(), TemplateRequestStatus.Completed);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task UpdateRequestStatusAsync_Success_UpdatesStatus()
        {
            // Arrange
            var requestId = Guid.NewGuid();
            var request = new TemplateRequest { Id = requestId, Status = TemplateRequestStatus.Pending };

            var queryable = new List<TemplateRequest> { request }.BuildMock();
            _templateRequestRepo.GetQueryable().Returns(queryable);

            var dto = new TemplateRequestDto { Id = requestId, Status = "Completed" };
            _mapper.Map<TemplateRequestDto>(request).Returns(dto);
            _messageService.Get("RecordUpdated").Returns("Status updated.");

            // Act
            var result = await _sut.UpdateRequestStatusAsync(requestId, TemplateRequestStatus.Completed);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(TemplateRequestStatus.Completed, request.Status);
            _templateRequestRepo.Received(1).Update(request);
            await _unitOfWork.Received(1).SaveChangesAsync();
        }

        [Fact]
        public async Task GetTenantRequestsAsync_ReturnsMappedRequests()
        {
            // Arrange
            var requests = new List<TemplateRequest>
            {
                new TemplateRequest { Id = Guid.NewGuid(), TemplateName = "First", CreatedAt = DateTime.UtcNow.AddMinutes(-10) },
                new TemplateRequest { Id = Guid.NewGuid(), TemplateName = "Second", CreatedAt = DateTime.UtcNow }
            };

            var mock = requests.BuildMock();
            _templateRequestRepo.GetQueryable().Returns(mock);

            var dtos = new List<TemplateRequestDto>
            {
                new TemplateRequestDto { TemplateName = "Second" },
                new TemplateRequestDto { TemplateName = "First" }
            };

            _mapper.Map<IReadOnlyList<TemplateRequestDto>>(Arg.Any<List<TemplateRequest>>())
                .Returns(dtos);

            // Act
            var result = await _sut.GetTenantRequestsAsync();

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Data.Count);
            Assert.Equal("Second", result.Data[0].TemplateName);
        }
    }
}
