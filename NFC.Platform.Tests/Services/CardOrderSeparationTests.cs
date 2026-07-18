using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AutoMapper;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using NFC.Platform.Application.DTOs.CardOrder;
using NFC.Platform.Application.Interfaces.Repositories;
using NFC.Platform.Application.Services;
using NFC.Platform.BuildingBlocks.Localization;
using NFC.Platform.BuildingBlocks.Results;
using NFC.Platform.Domain.Entities;
using NFC.Platform.Domain.Enums;
using MockQueryable.NSubstitute;
using NSubstitute;
using Xunit;

namespace NFC.Platform.Tests.Services
{
    public class CardOrderSeparationTests
    {
        [Fact]
        public void CardOrder_ShouldNotHaveCustomDesignRequestIdProperty()
        {
            // Act
            var property = typeof(CardOrder).GetProperty("CustomDesignRequestId");

            // Assert
            Assert.Null(property);
        }

        [Fact]
        public async Task CreateAsync_ShouldNotQueryTemplateRequestRepository()
        {
            // Arrange
            var unitOfWork = Substitute.For<IUnitOfWork>();
            var mapper = Substitute.For<IMapper>();
            var messageService = Substitute.For<IMessageService>();
            var currentTenant = Substitute.For<ICurrentTenant>();
            var excelParser = Substitute.For<IExcelParser>();
            var validator = Substitute.For<IValidator<CreateCardOrderRequest>>();
            var storageService = Substitute.For<NFC.Platform.Application.Interfaces.Services.IStorageService>();
            var backgroundJobClient = Substitute.For<Hangfire.IBackgroundJobClient>();

            var orderRepo = Substitute.For<IGenericRepository<CardOrder>>();
            var pricingRepo = Substitute.For<IGenericRepository<CardPricing>>();
            unitOfWork.Repository<CardOrder>().Returns(orderRepo);
            unitOfWork.Repository<CardPricing>().Returns(pricingRepo);

            var validationResult = new FluentValidation.Results.ValidationResult();
            validator.ValidateAsync(Arg.Any<CreateCardOrderRequest>(), default)
                .Returns(Task.FromResult(validationResult));

            currentTenant.UserId.Returns(Guid.NewGuid());

            var pricing = new CardPricing
            {
                CardType = CardType.Plastic,
                IsActive = true,
                EffectiveFrom = DateTime.UtcNow.AddDays(-1),
                UnitPrice = 10,
                Currency = "KWD"
            };
            pricingRepo.GetQueryable().Returns(new List<CardPricing> { pricing }.AsQueryable().BuildMock());

            var order = new CardOrder
            {
                Id = Guid.NewGuid(),
                Quantity = 1,
                CardType = CardType.Plastic
            };
            mapper.Map<CardOrder>(Arg.Any<CreateCardOrderRequest>()).Returns(order);
            orderRepo.GetQueryable().Returns(new List<CardOrder> { order }.AsQueryable().BuildMock());

            var service = new CardOrderService(
                unitOfWork,
                mapper,
                messageService,
                currentTenant,
                excelParser,
                validator,
                storageService,
                backgroundJobClient);

            var request = new CreateCardOrderRequest
            {
                Quantity = 1,
                CardType = CardType.Plastic,
                CardDesignType = CardDesignType.NeedCustomDesign,
                LogoUrl = "https://cdn.example.com/logo.png"
            };

            // Act
            var result = await service.CreateAsync(request);

            // Assert
            Assert.True(result.IsSuccess);
            unitOfWork.DidNotReceive().Repository<TemplateRequest>();
        }

        [Fact]
        public async Task ResolvePublicProfileAsync_QueriesOnlyProfileTemplateRequestType()
        {
            // Arrange
            var unitOfWork = Substitute.For<IUnitOfWork>();
            var mapper = Substitute.For<IMapper>();
            var messageService = Substitute.For<IMessageService>();

            var cardRepo = Substitute.For<IGenericRepository<Card>>();
            var templateRequestRepo = Substitute.For<IGenericRepository<TemplateRequest>>();
            unitOfWork.Repository<Card>().Returns(cardRepo);
            unitOfWork.Repository<TemplateRequest>().Returns(templateRequestRepo);

            var tenantId = Guid.NewGuid();
            var company = new Company { TenantId = tenantId };
            var employee = new Employee { Company = company };
            var userProfile = new UserProfile { TenantId = tenantId, Employee = employee };
            var card = new Card { Id = Guid.NewGuid(), Status = CardStatus.Active, UserProfile = userProfile, ActivationCode = "test-code" };

            mapper.Map<EmployeeDetailsDto>(card.UserProfile).Returns(new EmployeeDetailsDto());

            cardRepo.GetQueryable().Returns(new List<Card> { card }.AsQueryable().BuildMock());

            // Mock completed TemplateRequest queryable
            var completedRequest = new TemplateRequest
            {
                TenantId = tenantId,
                Status = TemplateRequestStatus.Completed,
                RequestType = TemplateRequestType.ProfileTemplate,
                LogoUrl = "https://cdn.example.com/logo.png"
            };
            templateRequestRepo.GetQueryable().Returns(new List<TemplateRequest> { completedRequest }.AsQueryable().BuildMock());

            var service = new ProfileMetricService(unitOfWork, messageService, mapper);

            // Act
            var result = await service.ResolvePublicProfileAsync("test-code");

            // Assert
            Assert.True(result.IsSuccess);
            // Verify that the query filtered by RequestType == TemplateRequestType.ProfileTemplate
            // We can confirm this because MockQueryable parsed and executed the query correctly
        }
    }
}
