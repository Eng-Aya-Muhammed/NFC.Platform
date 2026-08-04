using Xunit;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NFC.Platform.Application.DTOs.CardOrder;
using NFC.Platform.Application.DTOs.Admin;
using NFC.Platform.Application.Interfaces.Repositories;
using NFC.Platform.Application.Services;
using NFC.Platform.BuildingBlocks.Localization;
using NFC.Platform.Domain.Entities;
using NFC.Platform.Domain.Enums;
using NFC.Platform.BuildingBlocks.Common.Interfaces;
using AutoMapper;
using Microsoft.Extensions.Options;
using NFC.Platform.Application.DTOs.Settings;
using FluentValidation;
using Hangfire;
using NFC.Platform.Application.Interfaces.Services;
using MockQueryable.NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NFC.Platform.Tests.Services
{
    public class RaceConditionTests
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessageService _messageService;
        private readonly ICurrentTenant _currentTenant;

        public RaceConditionTests()
        {
            _unitOfWork = Substitute.For<IUnitOfWork>();
            _messageService = Substitute.For<IMessageService>();
            _currentTenant = Substitute.For<ICurrentTenant>();
            _currentTenant.UserId.Returns(Guid.NewGuid());
            _currentTenant.TenantId.Returns(Guid.NewGuid());
            _messageService.Get(Arg.Any<string>()).Returns(callInfo => callInfo.Arg<string>());
            _messageService.Get(Arg.Any<string>(), Arg.Any<object[]>()).Returns(callInfo => callInfo.Arg<string>());
        }

        [Fact]
        public async Task CardOrderService_CreateOrderAsync_WhenDbUpdateConcurrencyExceptionThrown_Returns409Conflict()
        {
            // Arrange
            var designId = Guid.NewGuid();
            var tenantId = _currentTenant.TenantId.Value;
            var userId = _currentTenant.UserId.Value;

            var designRepo = Substitute.For<IGenericRepository<CardDesign>>();
            var cardDesign = new CardDesign 
            { 
                Id = designId, 
                TenantId = tenantId, 
                IsPaid = true, 
                TotalQuantity = 100, 
                UsedQuantity = 0, 
                PendingQuantity = 50 
            };
            
            designRepo.GetQueryable().Returns(new List<CardDesign> { cardDesign }.AsQueryable().BuildMock());
            designRepo.GetByIdAsync(designId).Returns(cardDesign);
            _unitOfWork.Repository<CardDesign>().Returns(designRepo);

            var userRepo = Substitute.For<IGenericRepository<User>>();
            userRepo.GetQueryable().Returns(new List<User> { new User { Id = userId, AccountType = AccountType.Individual } }.AsQueryable().BuildMock());
            _unitOfWork.Repository<User>().Returns(userRepo);

            var orderRepo = Substitute.For<IGenericRepository<CardOrder>>();
            orderRepo.GetQueryable().Returns(new List<CardOrder>().AsQueryable().BuildMock());
            _unitOfWork.Repository<CardOrder>().Returns(orderRepo);

            // Simulate the exception during SaveChangesAsync inside CreateOrderAsync
            _unitOfWork.SaveChangesAsync().Throws(new DbUpdateConcurrencyException());

            var validator = Substitute.For<IValidator<CreateCardOrderRequest>>();
            validator.ValidateAsync(Arg.Any<CreateCardOrderRequest>()).Returns(new FluentValidation.Results.ValidationResult());

            var otpOptions = Substitute.For<IOptions<OtpSettings>>();
            otpOptions.Value.Returns(new OtpSettings());

            var sut = new CardOrderService(
                _unitOfWork,
                Substitute.For<IMapper>(),
                _messageService,
                _currentTenant,
                validator,
                Substitute.For<IValidator<UpdateCardOrderRequest>>(),
                Substitute.For<IBackgroundJobClient>(),
                Substitute.For<IEmployeeService>(),
                otpOptions
            );

            var request = new CreateCardOrderRequest
            {
                CardDesignId = designId,
                Quantity = 10 // 100 - 0 - 50 = 50 available, so this is valid.
            };

            // Act
            var result = await sut.CreateOrderAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(409, result.StatusCode);
            Assert.Equal("ConcurrentUpdateConflict", result.Message);
            
            // Verify Rollback was called
            await _unitOfWork.Received(1).RollbackTransactionAsync();
        }

        [Fact]
        public async Task AdminService_UpdateOrderStatusAsync_WhenApprovingAndConcurrencyExceptionThrown_Returns409Conflict()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var designId = Guid.NewGuid();
            var tenantId = _currentTenant.TenantId.Value;

            var orderRepo = Substitute.For<IGenericRepository<CardOrder>>();
            var order = new CardOrder
            {
                Id = orderId,
                TenantId = tenantId,
                CardDesignId = designId,
                Status = OrderStatus.PendingReview,
                Quantity = 10
            };
            orderRepo.GetQueryable().Returns(new List<CardOrder> { order }.AsQueryable().BuildMock());
            orderRepo.GetByIdAsync(orderId).Returns(order);
            _unitOfWork.Repository<CardOrder>().Returns(orderRepo);

            var designRepo = Substitute.For<IGenericRepository<CardDesign>>();
            var cardDesign = new CardDesign
            {
                Id = designId,
                TotalQuantity = 100,
                UsedQuantity = 10,
                PendingQuantity = 20
            };
            designRepo.GetQueryable().Returns(new List<CardDesign> { cardDesign }.AsQueryable().BuildMock());
            _unitOfWork.Repository<CardDesign>().Returns(designRepo);

            // Simulate the exception during SaveChangesAsync inside UpdateOrderStatusAsync
            _unitOfWork.SaveChangesAsync().Throws(new DbUpdateConcurrencyException());

            var otpOptions = Substitute.For<IOptions<OtpSettings>>();
            otpOptions.Value.Returns(new OtpSettings());

            var sut = new AdminService(
                _unitOfWork,
                Substitute.For<IMapper>(),
                _messageService,
                Substitute.For<IStorageService>(),
                Substitute.For<IBackgroundJobClient>(),
                otpOptions
            );

            var request = new UpdateOrderStatusDto { Status = OrderStatus.Approved };

            // Act
            var result = await sut.UpdateOrderStatusAsync(orderId, request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(409, result.StatusCode);
            Assert.Equal("ConcurrentUpdateConflict", result.Message);
        }

        [Fact]
        public async Task CardOrderService_UpdateOrderAsync_WhenExceedingPendingQuantity_Returns400()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var designId = Guid.NewGuid();
            var tenantId = _currentTenant.TenantId.Value;
            var userId = _currentTenant.UserId.Value;

            var orderRepo = Substitute.For<IGenericRepository<CardOrder>>();
            var order = new CardOrder
            {
                Id = orderId,
                TenantId = tenantId,
                CardDesignId = designId,
                Status = OrderStatus.PendingReview,
                Quantity = 10
            };
            orderRepo.GetQueryable().Returns(new List<CardOrder> { order }.AsQueryable().BuildMock());
            _unitOfWork.Repository<CardOrder>().Returns(orderRepo);

            var designRepo = Substitute.For<IGenericRepository<CardDesign>>();
            var cardDesign = new CardDesign
            {
                Id = designId,
                TotalQuantity = 100,
                UsedQuantity = 70,
                PendingQuantity = 20
            };
            // available = Total - Used - Pending + Order.Quantity
            // available = 100 - 70 - 20 + 10 = 20
            designRepo.GetQueryable().Returns(new List<CardDesign> { cardDesign }.AsQueryable().BuildMock());
            designRepo.GetByIdAsync(designId).Returns(cardDesign);
            _unitOfWork.Repository<CardDesign>().Returns(designRepo);

            var userRepo = Substitute.For<IGenericRepository<User>>();
            userRepo.GetQueryable().Returns(new List<User> { new User { Id = userId, AccountType = AccountType.Individual } }.AsQueryable().BuildMock());
            _unitOfWork.Repository<User>().Returns(userRepo);

            var updateValidator = Substitute.For<IValidator<UpdateCardOrderRequest>>();
            updateValidator.ValidateAsync(Arg.Any<UpdateCardOrderRequest>()).Returns(new FluentValidation.Results.ValidationResult());

            var otpOptions = Substitute.For<IOptions<OtpSettings>>();
            otpOptions.Value.Returns(new OtpSettings());

            var sut = new CardOrderService(
                _unitOfWork,
                Substitute.For<IMapper>(),
                _messageService,
                _currentTenant,
                Substitute.For<IValidator<CreateCardOrderRequest>>(),
                updateValidator,
                Substitute.For<IBackgroundJobClient>(),
                Substitute.For<IEmployeeService>(),
                otpOptions
            );

            // Act - trying to update quantity to 30, which exceeds available 20
            var request = new UpdateCardOrderRequest { Quantity = 30 };
            var result = await sut.UpdateOrderAsync(orderId, request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(400, result.StatusCode);
            Assert.Equal("DesignRemainingQuantityExceeded", result.Message);
            await _unitOfWork.Received(1).RollbackTransactionAsync();
        }

        [Fact]
        public async Task CardOrderService_ResendOrderOtpAsync_WhenConcurrencyException_Returns409Conflict()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var tenantId = _currentTenant.TenantId.Value;

            var orderRepo = Substitute.For<IGenericRepository<CardOrder>>();
            var order = new CardOrder
            {
                Id = orderId,
                TenantId = tenantId,
                Status = OrderStatus.ReadyForDelivery,
                DeliveryOtpResendCount = 0
            };
            orderRepo.GetQueryable().Returns(new List<CardOrder> { order }.AsQueryable().BuildMock());
            _unitOfWork.Repository<CardOrder>().Returns(orderRepo);

            // Simulate the concurrency exception when updating OTP counters
            _unitOfWork.SaveChangesAsync().Throws(new DbUpdateConcurrencyException());

            var otpOptions = Substitute.For<IOptions<OtpSettings>>();
            otpOptions.Value.Returns(new OtpSettings { MaxResendAttempts = 5, CooldownSeconds = 0 });

            var sut = new CardOrderService(
                _unitOfWork,
                Substitute.For<IMapper>(),
                _messageService,
                _currentTenant,
                Substitute.For<IValidator<CreateCardOrderRequest>>(),
                Substitute.For<IValidator<UpdateCardOrderRequest>>(),
                Substitute.For<IBackgroundJobClient>(),
                Substitute.For<IEmployeeService>(),
                otpOptions
            );

            // Act
            var result = await sut.ResendOrderOtpAsync(orderId);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(409, result.StatusCode);
            Assert.Equal("ConcurrentUpdateConflict", result.Message);
        }
    }
}
