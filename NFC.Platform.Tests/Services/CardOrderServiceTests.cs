using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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
    public class CardOrderServiceTests
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IMessageService _messageService;
        private readonly ICurrentTenant _currentTenant;
        private readonly IExcelParser _excelParser;

        private readonly IGenericRepository<CardOrder> _orderRepo;
        private readonly IGenericRepository<Card> _cardRepo;
        private readonly IGenericRepository<CardOrderItem> _orderItemRepo;

        private readonly CardOrderService _sut;

        public CardOrderServiceTests()
        {
            _unitOfWork = Substitute.For<IUnitOfWork>();
            _mapper = Substitute.For<IMapper>();
            _messageService = Substitute.For<IMessageService>();
            _currentTenant = Substitute.For<ICurrentTenant>();
            _excelParser = Substitute.For<IExcelParser>();

            _orderRepo = Substitute.For<IGenericRepository<CardOrder>>();
            _cardRepo = Substitute.For<IGenericRepository<Card>>();
            _orderItemRepo = Substitute.For<IGenericRepository<CardOrderItem>>();

            _unitOfWork.Repository<CardOrder>().Returns(_orderRepo);
            _unitOfWork.Repository<Card>().Returns(_cardRepo);
            _unitOfWork.Repository<CardOrderItem>().Returns(_orderItemRepo);

            var validator = Substitute.For<FluentValidation.IValidator<CreateCardOrderRequest>>();
            var validationResult = new FluentValidation.Results.ValidationResult();
            validator.ValidateAsync(Arg.Any<CreateCardOrderRequest>(), default)
                .Returns(Task.FromResult(validationResult));

            _sut = new CardOrderService(_unitOfWork, _mapper, _messageService, _currentTenant, _excelParser, validator);
        }

        // ── GetByIdAsync ──────────────────────────────────────────────────────────

        [Fact]
        public async Task GetByIdAsync_ReturnsNotFound_WhenOrderDoesNotExist()
        {
            // Arrange
            var id = Guid.NewGuid();
            var emptyQueryable = new List<CardOrder>().AsQueryable().BuildMock();
            _orderRepo.GetQueryable().Returns(emptyQueryable);
            _messageService.Get("RecordNotFound").Returns("Record not found.");

            // Act
            var result = await _sut.GetByIdAsync(id);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsSuccess_WhenOrderExists()
        {
            // Arrange
            var id = Guid.NewGuid();
            var order = new CardOrder { Id = id, Items = [] };
            var queryable = new List<CardOrder> { order }.AsQueryable().BuildMock();
            _orderRepo.GetQueryable().Returns(queryable);
            var dto = new CardOrderDto { Id = id };
            _mapper.Map<CardOrderDto>(order).Returns(dto);

            // Act
            var result = await _sut.GetByIdAsync(id);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(200, result.StatusCode);
            Assert.Equal(id, result.Data!.Id);
        }

        // ── CreateAsync ───────────────────────────────────────────────────────────

        [Fact]
        public async Task CreateAsync_ReturnsUnauthorized_WhenUserNotAuthenticated()
        {
            // Arrange
            _currentTenant.UserId.Returns((Guid?)null);
            var request = new CreateCardOrderRequest { Quantity = 10 };

            // Act
            var result = await _sut.CreateAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(401, result.StatusCode);
        }

        [Fact]
        public async Task CreateAsync_ReturnsSuccess_AndSetsDefaults_WhenNoCardNameOrType()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _currentTenant.UserId.Returns(userId);

            var request = new CreateCardOrderRequest { Quantity = 5 };
            var order = new CardOrder { Id = Guid.NewGuid(), Quantity = 5, Items = [] };
            _mapper.Map<CardOrder>(request).Returns(order);

            var createdQueryable = new List<CardOrder> { order }.AsQueryable().BuildMock();
            _orderRepo.GetQueryable().Returns(createdQueryable);

            var dto = new CardOrderDto { Quantity = 5 };
            _mapper.Map<CardOrderDto>(order).Returns(dto);
            _messageService.Get("RecordCreated").Returns("Record created.");
            _messageService.Get("DefaultCardOrderName", Arg.Any<object[]>()).Returns(x => $"طلب كروت - {((object[])x[1])[0]}");

            // Act
            var result = await _sut.CreateAsync(request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(200, result.StatusCode);
            // Defaults should have been applied to the order object
            Assert.Equal($"طلب كروت - 5", order.CardName);
            Assert.Equal(CardType.Plastic, order.CardType);
            Assert.Equal(CardDesignType.BuiltInTemplate, order.CardDesignType);
            await _orderRepo.Received(1).AddAsync(order);
        }


        // ── DeleteAsync ───────────────────────────────────────────────────────────

        [Fact]
        public async Task DeleteAsync_ReturnsNotFound_WhenOrderDoesNotExist()
        {
            // Arrange
            var id = Guid.NewGuid();
            _orderRepo.GetByIdAsync(id).Returns((CardOrder?)null);
            _messageService.Get("RecordNotFound").Returns("Record not found.");

            // Act
            var result = await _sut.DeleteAsync(id);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task DeleteAsync_ReturnsSuccess_AndRemovesOrder()
        {
            // Arrange
            var id = Guid.NewGuid();
            var order = new CardOrder { Id = id };
            _orderRepo.GetByIdAsync(id).Returns(order);
            _messageService.Get("RecordDeleted").Returns("Record deleted.");

            // Act
            var result = await _sut.DeleteAsync(id);

            // Assert
            Assert.True(result.IsSuccess);
            _orderRepo.Received(1).Remove(order);
            await _unitOfWork.Received(1).SaveChangesAsync();
        }
    }
}
